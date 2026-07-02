using System.IO;
using Audiola.Services;
using NAudio.Wave;

namespace Singola.Services;

/// <summary>Ein spielbereiter Song: Audio + Metadaten + Lyrics + Referenz-Melodie (Notenband).</summary>
public sealed record LoadedSong(
    string PlayablePath, string Title, string? Lrc, double DurationSeconds, string AudioHash,
    IReadOnlyList<MelodyNote> Melody, string MelodySource)
{
    public bool HasLyrics => !string.IsNullOrWhiteSpace(Lrc);
    public bool HasMelody => Melody.Count > 0;
}

/// <summary>
/// Lädt einen Song für die Karaoke-Bühne und besorgt Lyrics UND Melodie „automatisch":
/// Lyrics: Song-Cache → .audiola-Track-Lyrics → .lrc daneben → Datei-Tags → ElevenLabs.
/// Melodie: Song-Cache → .audiola-Gesangsspur → Demucs-Vocals (wenn eingerichtet) → Mix-Schätzung.
/// Beides landet im inhalts-gehashten Cache — jeder Song wird nur einmal analysiert.
/// </summary>
public static class SongLoader
{
    public static readonly string[] AudioExtensions = [".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac", ".wma", ".mp4"];

    public static bool IsSupported(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".audiola" || AudioExtensions.Contains(ext);
    }

    public static async Task<LoadedSong> LoadAsync(string path, IProgress<string>? status, ISettingsService settings)
    {
        string playable = path;
        string title = Path.GetFileNameWithoutExtension(path);
        string? lrc = null;
        string? vocalStemPath = null;   // isolierte Gesangsquelle, falls das Projekt eine hat

        var isProject = path.EndsWith(".audiola", StringComparison.OrdinalIgnoreCase);
        var apiKey = settings.Current.ElevenLabsApiKey;

        // ---- FAST-PATH: Hash über die ORIGINAL-Datei — ist alles schon analysiert, sind wir sofort fertig
        //      (kein Entpacken, kein Mix-Rendern, keine Tags, kein Demucs, keine Transkription). ----
        var hash = SongCache.ComputeAudioHash(path);
        var cached = SongCache.Load(hash);
        var cachedPlayable = isProject ? cached?.PlayableWav : path;
        var lyricsSettled = !string.IsNullOrWhiteSpace(cached?.Lrc) || string.IsNullOrWhiteSpace(apiKey);
        if (cached is not null && cached.MelodyJson is not null && cached.DurationSeconds > 1
            && lyricsSettled && cachedPlayable is not null && File.Exists(cachedPlayable))
        {
            status?.Report("Sofort bereit — alles aus dem Song-Gedächtnis.");
            return new LoadedSong(cachedPlayable, cached.Title ?? title, cached.Lrc, cached.DurationSeconds,
                hash, MelodyExtractor.FromJson(cached.MelodyJson), "Cache");
        }

        // .audiola: Projekt entpacken — Lyrics + Gesangsspur mitnehmen, Audio wie in Audiola mischen.
        if (isProject)
        {
            status?.Report("Projekt wird geöffnet …");
            var project = new ProjectService();
            var dto = await project.LoadAsync(path);

            // Lyrics aus dem Projekt: Spur-Lyrics (synchronisiert) vor den Song-Metadaten —
            // auch Fließtext zählt (wird verteilt), damit NIE unnötig transkribiert wird.
            lrc = dto.Tracks.Select(t => t.Lrc).FirstOrDefault(LrcParser.HasTimestamps)
                  ?? (string.IsNullOrWhiteSpace(dto.Metadata?.Lyrics) ? null : dto.Metadata!.Lyrics)
                  ?? dto.Tracks.Select(t => t.Lrc).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
            if (!string.IsNullOrWhiteSpace(dto.Metadata?.Title)) title = dto.Metadata!.Title!;

            // Gesangsspur (fürs Notenband) über den Namen erkennen.
            var vocalTrack = dto.Tracks.FirstOrDefault(t =>
                t.Name.Contains("vocal", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("gesang", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("stimme", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("lead", StringComparison.OrdinalIgnoreCase));
            vocalStemPath = vocalTrack?.Clips.FirstOrDefault(c => File.Exists(c.Media))?.Media;

            status?.Report("Projekt-Mix wird gerendert …");
            // Mute/Solo/Aktiv aus dem Projekt übernehmen — es klingt exakt wie in Audiola.
            var mixTracks = dto.Tracks
                .SelectMany(t => t.Clips
                    .Where(c => !string.IsNullOrEmpty(c.Media) && File.Exists(c.Media))
                    .Select(c => new MixTrack(c.Media, t.Volume, t.Pan,
                        t.IsMuted || !t.IsEnabled, t.IsSolo, c.TimelineOffsetSeconds)))
                .ToList();
            if (mixTracks.Count == 0) throw new InvalidOperationException("Das Projekt enthält keine abspielbaren Spuren.");

            // Direkt ins Song-Gedächtnis rendern — beim nächsten Öffnen entfällt das komplett.
            var (samples, rate) = await Task.Run(() => OfflineMixer.Render(mixTracks));
            playable = SongCache.PlayableWavPath(hash);
            AudioExporter.Export(samples, rate, 2, playable);
        }

        // ---- Lyrics ----
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
                if (LrcParser.HasTimestamps(text)) { lrc = text; status?.Report("Songtext aus .lrc-Datei geladen."); }
            }
        }

        try
        {
            using var tagFile = TagLib.File.Create(playable);
            if (!string.IsNullOrWhiteSpace(tagFile.Tag.Title)) title = tagFile.Tag.Title;
            if (!LrcParser.HasTimestamps(lrc) && !string.IsNullOrWhiteSpace(tagFile.Tag.Lyrics))
            {
                // Auch unsynchronisierte Tag-Lyrics übernehmen (werden über die Songdauer verteilt) —
                // vorhandener Text schlägt eine erneute Erkennung.
                lrc = tagFile.Tag.Lyrics;
                status?.Report("Songtext aus den Datei-Tags geladen.");
            }
        }
        catch { /* Format ohne Tags (WAV …) */ }

        if (string.IsNullOrWhiteSpace(lrc) && !string.IsNullOrWhiteSpace(apiKey))
        {
            status?.Report("Songtext wird erkannt (ElevenLabs) — dauert je nach Länge etwas …");
            try
            {
                lrc = await ElevenLabsTranscriber.TranscribeToLrcAsync(playable, apiKey!);
                if (lrc is not null) status?.Report("Songtext erkannt.");
            }
            catch (Exception ex) { status?.Report("Songtext-Erkennung fehlgeschlagen: " + ex.Message); }
        }

        // ---- Melodie (Notenband) — non-null MelodyJson heißt: Analyse ist schon gelaufen ----
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
                // Demucs still mitnutzen, wenn Audiola dafür eingerichtet ist (beste Qualität).
                try
                {
                    var demucs = new DemucsStemSeparationService(settings);
                    if (await demucs.IsAvailableAsync())
                    {
                        status?.Report("Gesang wird für das Notenband isoliert (Demucs) — das dauert ein paar Minuten, passiert aber nur einmal pro Song …");
                        var stems = await demucs.SeparateAsync(playable, new Progress<string>(s => status?.Report("Demucs: " + s)));
                        analyzePath = stems.Stems.FirstOrDefault(s => s.Kind == Audiola.Models.StemKind.Vocals)?.FilePath;
                        if (analyzePath is not null) melodySource = "Demucs-Gesangsspur";
                    }
                }
                catch { /* Demucs nicht nutzbar → Mix-Schätzung */ }
            }
            else melodySource = "Projekt-Gesangsspur";

            if (analyzePath is null) { analyzePath = playable; melodySource = "Mix-Schätzung"; }

            status?.Report("Melodie wird analysiert …");
            try { melody = await Task.Run(() => MelodyExtractor.ExtractFromFile(analyzePath)); }
            catch { melody = []; }
            if (melody.Count == 0) melodySource = "";
        }

        double duration;
        using (var reader = new AudioFileReader(playable)) duration = reader.TotalTime.TotalSeconds;

        SongCache.Save(hash, new SongCacheEntry
        {
            Title = title,
            Lrc = lrc,
            // Immer setzen — auch eine leere Analyse gilt als erledigt und läuft nie wieder.
            MelodyJson = MelodyExtractor.ToJson(melody),
            DurationSeconds = duration,
            LastPath = path,
            PlayableWav = isProject ? playable : null,
        });

        return new LoadedSong(playable, title, lrc, duration, hash, melody, melodySource);
    }
}
