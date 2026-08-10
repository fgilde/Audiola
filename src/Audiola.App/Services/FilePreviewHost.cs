using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Audiola.Services;

/// <summary>
/// Hosts files on a tiny local HTTP server (loopback only) and builds URLs to the
/// MudBlazor.Extensions "preview-file" page, so any file can be previewed in a browser
/// or an embedded web control (e.g. WebView2).
///
/// The preview page loads the file via fetch/XHR, so every response carries permissive
/// CORS headers. Browsers treat http://localhost as a secure context, so this works even
/// when the preview page itself is served over https (no mixed-content blocking).
///
/// Keep one instance alive for the lifetime of your app (it owns the listener) and Dispose it on exit.
/// </summary>
public sealed class FilePreviewHost : IDisposable
{
    private readonly HttpListener _listener;
    private readonly ConcurrentDictionary<string, PreviewItem> _items = new();
    private readonly string _origin;
    private readonly Uri _previewPage;
    private readonly TimeSpan _itemTtl;

    /// <param name="previewPageUrl">Absolute URL of the hosted preview page, e.g. "https://www.mudex.org/preview-file".</param>
    /// <param name="port">Loopback port to listen on. 0 picks a free port automatically.</param>
    /// <param name="itemTtl">How long a registered file stays available. Default 30 minutes.</param>
    public FilePreviewHost(string previewPageUrl, int port = 0, TimeSpan? itemTtl = null)
    {
        _previewPage = new Uri(previewPageUrl, UriKind.Absolute);
        _itemTtl = itemTtl ?? TimeSpan.FromMinutes(30);

        if (port == 0) port = GetFreeLoopbackPort();
        _origin = $"http://localhost:{port}";

        _listener = new HttpListener();
        _listener.Prefixes.Add($"{_origin}/");   // loopback -> no admin / urlacl needed on Windows
        _listener.Start();
        _ = Task.Run(AcceptLoopAsync);
    }

    // ---- URL builders (use these to feed your own WebView / iframe) ----

    public string GetPreviewUrl(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
        if (!File.Exists(filePath)) throw new FileNotFoundException("File not found.", filePath);

        var fileName = Path.GetFileName(filePath);
        return Register(new PreviewItem
        {
            FileName = fileName,
            ContentType = GuessContentType(fileName),
            FilePath = filePath,         // streamed on demand, not buffered
        });
    }

    public string GetPreviewUrl(byte[] bytes, string fileName, string? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return Register(new PreviewItem
        {
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? GuessContentType(fileName) : contentType!,
            Bytes = bytes,
        });
    }

    public string GetPreviewUrl(Stream stream, string fileName, string? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);              // materialize so it can be served on multiple requests
        return GetPreviewUrl(ms.ToArray(), fileName, contentType);
    }

    // ---- open directly in the default browser --------------------------

    public void Show(string filePath) => OpenInBrowser(GetPreviewUrl(filePath));
    public void Show(byte[] bytes, string fileName, string? contentType = null) => OpenInBrowser(GetPreviewUrl(bytes, fileName, contentType));
    public void Show(Stream stream, string fileName, string? contentType = null) => OpenInBrowser(GetPreviewUrl(stream, fileName, contentType));

    // ---- internals -----------------------------------------------------

    private string Register(PreviewItem item)
    {
        SweepExpired();
        var id = Guid.NewGuid().ToString("N");
        item.CreatedUtc = DateTime.UtcNow;
        _items[id] = item;

        var fileUrl = $"{_origin}/{id}";
        var query =
            $"url={Uri.EscapeDataString(fileUrl)}" +
            $"&name={Uri.EscapeDataString(item.FileName ?? string.Empty)}" +
            $"&contentType={Uri.EscapeDataString(item.ContentType ?? string.Empty)}";

        return $"{_previewPage.GetLeftPart(UriPartial.Path)}?{query}";
    }

    private async Task AcceptLoopAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync().ConfigureAwait(false); }
            catch { break; } // listener stopped/disposed
            _ = Task.Run(() => HandleRequestAsync(ctx));
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        try
        {
            // CORS - the WASM page fetches cross-origin.
            res.AddHeader("Access-Control-Allow-Origin", "*");
            res.AddHeader("Access-Control-Allow-Methods", "GET, HEAD, OPTIONS");
            res.AddHeader("Access-Control-Allow-Headers", "*");
            // Media elements (audio/video) need byte ranges + these headers exposed to read them.
            res.AddHeader("Access-Control-Expose-Headers", "Content-Length, Content-Range, Accept-Ranges");
            res.AddHeader("Accept-Ranges", "bytes");

            if (req.HttpMethod == "OPTIONS") { res.StatusCode = 204; return; }

            var id = req.Url!.AbsolutePath.Trim('/');
            if (!_items.TryGetValue(id, out var item)) { res.StatusCode = 404; return; }

            res.ContentType = item.ContentType ?? "application/octet-stream";
            res.AddHeader("Content-Disposition", $"inline; filename=\"{item.FileName}\"");

            var total = item.Length;
            var isHead = req.HttpMethod == "HEAD";

            // HTML5-Audio/Video sendet Range-Requests zum Streamen/Seeken → 206 beantworten.
            if (TryParseRange(req.Headers["Range"], total, out var from, out var to))
            {
                res.StatusCode = 206;
                res.AddHeader("Content-Range", $"bytes {from}-{to}/{total}");
                res.ContentLength64 = to - from + 1;
                if (isHead) return;
                await item.WriteToAsync(res.OutputStream, from, to).ConfigureAwait(false);
            }
            else
            {
                res.ContentLength64 = total;
                if (isHead) return;
                await item.WriteToAsync(res.OutputStream, 0, total - 1).ConfigureAwait(false);
            }
        }
        catch { try { res.StatusCode = 500; } catch { /* ignore */ } }
        finally { try { res.Close(); } catch { /* ignore */ } }
    }

    /// <summary>Parst einen einfachen "bytes=from-to"-Range-Header. Liefert false, wenn keiner/ungültig.</summary>
    private static bool TryParseRange(string? header, long total, out long from, out long to)
    {
        from = 0; to = total - 1;
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase) || total <= 0)
            return false;

        var spec = header["bytes=".Length..].Split(',')[0].Trim();   // nur den ersten Bereich bedienen
        var dash = spec.IndexOf('-');
        if (dash < 0) return false;

        var startStr = spec[..dash];
        var endStr = spec[(dash + 1)..];

        if (startStr.Length == 0)
        {
            // Suffix-Range "bytes=-N" → letzte N Bytes.
            if (!long.TryParse(endStr, out var lastN) || lastN <= 0) return false;
            from = Math.Max(0, total - lastN);
            to = total - 1;
        }
        else
        {
            if (!long.TryParse(startStr, out from)) return false;
            to = endStr.Length == 0 ? total - 1 : (long.TryParse(endStr, out var e) ? e : total - 1);
        }

        from = Math.Clamp(from, 0, total - 1);
        to = Math.Clamp(to, from, total - 1);
        return true;
    }

    private void SweepExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _items)
            if (now - kvp.Value.CreatedUtc > _itemTtl)
                _items.TryRemove(kvp.Key, out _);
    }

    private static int GetFreeLoopbackPort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        try { return ((IPEndPoint)l.LocalEndpoint).Port; }
        finally { l.Stop(); }
    }

    private static void OpenInBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
        else
            Process.Start("xdg-open", url);
    }

    public void Dispose()
    {
        try { _listener.Stop(); _listener.Close(); } catch { /* ignore */ }
        _items.Clear();
    }

    private sealed class PreviewItem
    {
        public string? FileName;
        public string? ContentType;
        public byte[]? Bytes;
        public string? FilePath;
        public DateTime CreatedUtc;

        public long Length => Bytes?.Length ?? (FilePath != null ? new FileInfo(FilePath).Length : 0);

        /// <summary>Schreibt den Byte-Bereich [from..to] (inklusiv) in den Ausgabestrom.</summary>
        public async Task WriteToAsync(Stream output, long from, long to)
        {
            var count = to - from + 1;
            if (count <= 0) return;

            if (Bytes != null)
            {
                await output.WriteAsync(Bytes.AsMemory((int)from, (int)count)).ConfigureAwait(false);
            }
            else if (FilePath != null)
            {
                using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs.Seek(from, SeekOrigin.Begin);
                var buffer = new byte[81920];
                var remaining = count;
                while (remaining > 0)
                {
                    var read = await fs.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining))).ConfigureAwait(false);
                    if (read <= 0) break;
                    await output.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
                    remaining -= read;
                }
            }
        }
    }

    private static string GuessContentType(string? fileName) => Path.GetExtension(fileName)?.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".svg" => "image/svg+xml",
        ".bmp" => "image/bmp",
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".flac" => "audio/flac",
        ".m4a" or ".aac" => "audio/mp4",
        ".ogg" => "audio/ogg",
        ".txt" => "text/plain",
        ".csv" => "text/csv",
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".html" or ".htm" => "text/html",
        ".md" => "text/markdown",
        ".zip" => "application/zip",
        _ => "application/octet-stream",
    };
}
