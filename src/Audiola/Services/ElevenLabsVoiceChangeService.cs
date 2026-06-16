using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audiola.Services;

/// <summary>
/// Voice-Changer über die ElevenLabs Speech-to-Speech-API
/// (POST /v1/speech-to-speech/{voice_id}). Lädt den Clip hoch, holt das
/// konvertierte Audio (MP3) und dekodiert es in einen Stereo-Float-Puffer.
/// </summary>
public sealed class ElevenLabsVoiceChangeService : IVoiceChangeService
{
    private readonly ISettingsService _settings;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };

    public ElevenLabsVoiceChangeService(ISettingsService settings) => _settings = settings;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Current.ElevenLabsApiKey) &&
        !string.IsNullOrWhiteSpace(_settings.Current.ElevenLabsVoiceId);

    public bool HasApiKey => !string.IsNullOrWhiteSpace(_settings.Current.ElevenLabsApiKey);

    public Task<(float[] Samples, int SampleRate)> ChangeAsync(string inputWavPath, CancellationToken ct = default)
    {
        var voiceId = _settings.Current.ElevenLabsVoiceId;
        if (string.IsNullOrWhiteSpace(voiceId))
            throw new InvalidOperationException("ElevenLabs Voice-ID fehlt in den Einstellungen.");
        return ChangeAsync(inputWavPath, voiceId, ct);
    }

    public async Task<(float[] Samples, int SampleRate)> ChangeAsync(string inputWavPath, string voiceId, CancellationToken ct = default)
    {
        var s = _settings.Current;
        RequireApiKey();

        var model = string.IsNullOrWhiteSpace(s.ElevenLabsModelId)
            ? "eleven_multilingual_sts_v2"
            : s.ElevenLabsModelId;
        var url = $"https://api.elevenlabs.io/v1/speech-to-speech/{voiceId}?output_format=mp3_44100_128";

        using var form = new MultipartFormDataContent();
        var audioBytes = await File.ReadAllBytesAsync(inputWavPath, ct);
        var fileContent = new ByteArrayContent(audioBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        form.Add(fileContent, "audio", "input.wav");
        form.Add(new StringContent(model), "model_id");
        // Niedrige Stability erhält die originale Performance (Timing/Betonung/Gesang),
        // hohe Similarity überträgt die Ziel-Klangfarbe deutlicher.
        var voiceSettings = JsonSerializer.Serialize(new
        {
            stability = 0.35,
            similarity_boost = 0.9,
            style = 0.0,
            use_speaker_boost = true
        });
        form.Add(new StringContent(voiceSettings), "voice_settings");
        form.Add(new StringContent("false"), "remove_background_noise");

        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        req.Headers.Add("xi-api-key", s.ElevenLabsApiKey);

        using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"ElevenLabs-Fehler {(int)resp.StatusCode}: {Shorten(body)}");
        }

        var dir = Path.Combine(Path.GetTempPath(), "Audiola", "voice");
        Directory.CreateDirectory(dir);
        var mp3 = Path.Combine(dir, $"sts_{Guid.NewGuid():N}.mp3");
        await using (var fs = File.Create(mp3))
            await resp.Content.CopyToAsync(fs, ct);

        return DecodeToStereo(mp3);
    }

    public async Task<(float[] Samples, int SampleRate)> SpeakAsync(string text, string voiceId,
        double speed, double stability, double similarity, CancellationToken ct = default)
    {
        RequireApiKey();
        if (string.IsNullOrWhiteSpace(voiceId))
            throw new InvalidOperationException("Keine Zielstimme gewählt.");
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Kein Text angegeben.");

        var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}?output_format=mp3_44100_128";
        var payload = new
        {
            text,
            model_id = "eleven_multilingual_v2",
            voice_settings = new
            {
                stability = Math.Clamp(stability, 0, 1),
                similarity_boost = Math.Clamp(similarity, 0, 1),
                speed = Math.Clamp(speed, 0.7, 1.2)
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Add("xi-api-key", _settings.Current.ElevenLabsApiKey);

        using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"ElevenLabs-Fehler {(int)resp.StatusCode}: {Shorten(body)}");
        }

        var dir = Path.Combine(Path.GetTempPath(), "Audiola", "voice");
        Directory.CreateDirectory(dir);
        var mp3 = Path.Combine(dir, $"tts_{Guid.NewGuid():N}.mp3");
        await using (var fs = File.Create(mp3))
            await resp.Content.CopyToAsync(fs, ct);

        return DecodeToStereo(mp3);
    }

    public async Task DeleteVoiceAsync(string voiceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(voiceId) || !HasApiKey) return;
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"https://api.elevenlabs.io/v1/voices/{voiceId}");
        req.Headers.Add("xi-api-key", _settings.Current.ElevenLabsApiKey);
        using var resp = await Http.SendAsync(req, ct); // Fehler hier sind unkritisch (Aufräumen)
    }

    private void RequireApiKey()
    {
        if (!HasApiKey)
            throw new InvalidOperationException("Bitte zuerst den ElevenLabs API-Key in den Einstellungen eintragen.");
    }

    public async Task<IReadOnlyList<VoiceInfo>> GetVoicesAsync(CancellationToken ct = default)
    {
        var key = _settings.Current.ElevenLabsApiKey;
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Bitte zuerst den ElevenLabs API-Key eintragen.");

        using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.elevenlabs.io/v1/voices");
        req.Headers.Add("xi-api-key", key);
        using var resp = await Http.SendAsync(req, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"ElevenLabs-Fehler {(int)resp.StatusCode}: {Shorten(json)}");

        using var doc = JsonDocument.Parse(json);
        var list = new List<VoiceInfo>();
        if (doc.RootElement.TryGetProperty("voices", out var voices))
            foreach (var v in voices.EnumerateArray())
            {
                var id = v.TryGetProperty("voice_id", out var i) ? i.GetString() ?? "" : "";
                var name = v.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var cat = v.TryGetProperty("category", out var c) ? c.GetString() ?? "" : "";
                if (id.Length > 0) list.Add(new VoiceInfo(id, name, cat));
            }
        return list;
    }

    public async Task<string> CreateVoiceFromSamplesAsync(string name, IReadOnlyList<string> samplePaths, CancellationToken ct = default)
    {
        var key = _settings.Current.ElevenLabsApiKey;
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Bitte zuerst den ElevenLabs API-Key eintragen.");
        if (samplePaths.Count == 0)
            throw new InvalidOperationException("Keine Audiodatei für die Stimme angegeben.");

        using var form = new MultipartFormDataContent
        {
            { new StringContent(string.IsNullOrWhiteSpace(name) ? "Audiola-Stimme" : name), "name" }
        };
        foreach (var p in samplePaths)
        {
            var bytes = await File.ReadAllBytesAsync(p, ct);
            var fc = new ByteArrayContent(bytes);
            fc.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(fc, "files", Path.GetFileName(p));
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.elevenlabs.io/v1/voices/add") { Content = form };
        req.Headers.Add("xi-api-key", key);
        using var resp = await Http.SendAsync(req, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"ElevenLabs-Fehler {(int)resp.StatusCode}: {Shorten(json)}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("voice_id", out var vid) ? vid.GetString() ?? "" : "";
    }

    private static (float[] Samples, int SampleRate) DecodeToStereo(string path)
    {
        using var reader = new AudioFileReader(path);
        ISampleProvider sp = reader;
        if (sp.WaveFormat.Channels == 1)
            sp = new MonoToStereoSampleProvider(sp);
        var sr = sp.WaveFormat.SampleRate;

        var list = new List<float>(sr * 4);
        var buf = new float[sr * 2];
        int read;
        while ((read = sp.Read(buf, 0, buf.Length)) > 0)
            for (var i = 0; i < read; i++) list.Add(buf[i]);

        return (list.ToArray(), sr);
    }

    private static string Shorten(string s) =>
        string.IsNullOrEmpty(s) ? "(keine Antwort)" : s.Length > 300 ? s[..300] : s;
}
