using System.IO;
using Audiola.Services;

namespace Singola.Services;

public sealed record LoadedSong(
    string PlayablePath, string Title, string? Lrc, double DurationSeconds, string AudioHash,
    IReadOnlyList<MelodyNote> Melody, string MelodySource)
{
    public bool HasLyrics => !string.IsNullOrWhiteSpace(Lrc);
    public bool HasMelody => Melody.Count > 0;
}

/// <summary>Loads karaoke metadata without depending on a Windows media reader.</summary>
public static class SongLoader
{
    public static readonly string[] AudioExtensions = [".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac", ".wma", ".mp4"];

    public static bool IsSupported(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension == ".audiola" || AudioExtensions.Contains(extension);
    }

    public static async Task<LoadedSong> LoadAsync(string path, IProgress<string>? status, ISettingsService settings)
    {
        string playable = path;
        string title = Path.GetFileNameWithoutExtension(path);
        string? lrc = null;
        string? vocalStemPath = null;
        var isProject = path.EndsWith(".audiola", StringComparison.OrdinalIgnoreCase);
        var apiKey = settings.Current.ElevenLabsApiKey;

        var hash = SongCache.ComputeAudioHash(path);
        var cached = SongCache.Load(hash);
        var cachedPlayable = isProject ? cached?.PlayableWav : path;
        var lyricsSettled = !string.IsNullOrWhiteSpace(cached?.Lrc) || string.IsNullOrWhiteSpace(apiKey);
        if (cached is not null && cached.MelodyJson is not null && cached.DurationSeconds > 1 &&
            lyricsSettled && cachedPlayable is not null && File.Exists(cachedPlayable))
        {
            status?.Report("Sofort bereit — alles aus dem Song-Gedächtnis.");
            return new LoadedSong(cachedPlayable, cached.Title ?? title, cached.Lrc, cached.DurationSeconds,
                hash, MelodyExtractor.FromJson(cached.MelodyJson), "Cache");
        }

        if (isProject)
        {
            status?.Report("Projekt wird geöffnet …");
            var project = new ProjectService();
            var dto = await project.LoadAsync(path);
            lrc = dto.Tracks.Select(track => track.Lrc).FirstOrDefault(LrcParser.HasTimestamps)
                ?? (string.IsNullOrWhiteSpace(dto.Metadata?.Lyrics) ? null : dto.Metadata!.Lyrics)
                ?? dto.Tracks.Select(track => track.Lrc).FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
            if (!string.IsNullOrWhiteSpace(dto.Metadata?.Title)) title = dto.Metadata!.Title!;

            var vocalTrack = dto.Tracks.FirstOrDefault(track =>
                track.Name.Contains("vocal", StringComparison.OrdinalIgnoreCase) ||
                track.Name.Contains("gesang", StringComparison.OrdinalIgnoreCase) ||
                track.Name.Contains("stimme", StringComparison.OrdinalIgnoreCase) ||
                track.Name.Contains("lead", StringComparison.OrdinalIgnoreCase));
            vocalStemPath = vocalTrack?.Clips.FirstOrDefault(clip => File.Exists(clip.Media))?.Media;

            status?.Report("Projekt-Mix wird gerendert …");
            var mixTracks = dto.Tracks
                .SelectMany(track => track.Clips
                    .Where(clip => !string.IsNullOrEmpty(clip.Media) && File.Exists(clip.Media))
                    .Select(clip => new MixTrack(clip.Media, track.Volume, track.Pan,
                        track.IsMuted || !track.IsEnabled, track.IsSolo, clip.TimelineOffsetSeconds)))
                .ToList();
            if (mixTracks.Count == 0)
                throw new InvalidOperationException("Das Projekt enthält keine abspielbaren Spuren.");

            var (samples, rate) = await Task.Run(() => OfflineMixer.Render(mixTracks));
            playable = SongCache.PlayableWavPath(hash);
            AudioExporter.Export(samples, rate, 2, playable);
        }

        if (LrcParser.HasTimestamps(cached?.Lrc))
        {
            lrc ??= cached!.Lrc;
            if (!string.IsNullOrWhiteSpace(cached!.Title)) title = cached.Title!;
            status?.Report("Songtext aus dem Cache übernommen.");
        }

        if (!LrcParser.HasTimestamps(lrc))
        {
            var sidecar = Path.ChangeExtension(path, ".lrc");
            if (File.Exists(sidecar))
            {
                var text = await File.ReadAllTextAsync(sidecar);
                if (LrcParser.HasTimestamps(text))
                {
                    lrc = text;
                    status?.Report("Songtext aus .lrc-Datei geladen.");
                }
            }
        }

        double duration = 0;
        try
        {
            using var tagFile = TagLib.File.Create(playable);
            if (!string.IsNullOrWhiteSpace(tagFile.Tag.Title)) title = tagFile.Tag.Title;
            if (!LrcParser.HasTimestamps(lrc) && !string.IsNullOrWhiteSpace(tagFile.Tag.Lyrics))
            {
                lrc = tagFile.Tag.Lyrics;
                status?.Report("Songtext aus den Datei-Tags geladen.");
            }
            duration = tagFile.Properties.Duration.TotalSeconds;
        }
        catch
        {
            // Playback opens the file independently and still provides transport duration.
        }

        if (string.IsNullOrWhiteSpace(lrc) && !string.IsNullOrWhiteSpace(apiKey))
        {
            status?.Report("Songtext wird erkannt (ElevenLabs) — dauert je nach Länge etwas …");
            try
            {
                lrc = await ElevenLabsTranscriber.TranscribeToLrcAsync(playable, apiKey);
                if (lrc is not null) status?.Report("Songtext erkannt.");
            }
            catch (Exception ex)
            {
                status?.Report("Songtext-Erkennung fehlgeschlagen: " + ex.Message);
            }
        }

        IReadOnlyList<MelodyNote> melody = [];
        var melodySource = "";
        if (cached?.MelodyJson is not null)
        {
            melody = MelodyExtractor.FromJson(cached.MelodyJson);
            melodySource = melody.Count > 0 ? "Cache" : "";
        }
        else
        {
            var analyzePath = vocalStemPath;
            if (analyzePath is null)
            {
                try
                {
                    var demucs = new DemucsStemSeparationService(settings);
                    if (await demucs.IsAvailableAsync())
                    {
                        status?.Report("Gesang wird für das Notenband isoliert (Demucs) — das dauert ein paar Minuten, passiert aber nur einmal pro Song …");
                        var stems = await demucs.SeparateAsync(playable, new Progress<string>(message => status?.Report("Demucs: " + message)));
                        analyzePath = stems.Stems.FirstOrDefault(stem => stem.Kind == Audiola.Models.StemKind.Vocals)?.FilePath;
                        if (analyzePath is not null) melodySource = "Demucs-Gesangsspur";
                    }
                }
                catch
                {
                    // Fall through to the mix estimate.
                }
            }
            else
            {
                melodySource = "Projekt-Gesangsspur";
            }

            if (analyzePath is null)
            {
                analyzePath = playable;
                melodySource = "Mix-Schätzung";
            }

            status?.Report("Melodie wird analysiert …");
            try
            {
                melody = await Task.Run(() => MelodyExtractor.ExtractFromFile(analyzePath));
            }
            catch
            {
                melody = [];
            }
            if (melody.Count == 0) melodySource = "";
        }

        SongCache.Save(hash, new SongCacheEntry
        {
            Title = title,
            Lrc = lrc,
            MelodyJson = MelodyExtractor.ToJson(melody),
            DurationSeconds = duration,
            LastPath = path,
            PlayableWav = isProject ? playable : null,
        });

        return new LoadedSong(playable, title, lrc, duration, hash, melody, melodySource);
    }
}
