using System.Collections.ObjectModel;
using System.Windows;
using Audiola.Helper;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Audiola.ViewModels;

public sealed partial class TimelineViewModel
{
    /// <summary>
    /// Ersetzt die Stimme des ausgewählten Clips (oder des markierten Bereichs darin) per
    /// Speech-to-Speech mit explizit gewählter Zielstimme (aus dem Stimmtausch-Dialog —
    /// lokal über seed-vc oder über ElevenLabs). Performance/Timing/Betonung bleiben erhalten;
    /// löscht die Stimme danach, wenn sie nur temporär (geklont) war.
    /// </summary>
    public async Task ChangeSelectedClipVoiceAsync(VoiceChoice choice)
    {
        var clip = SelectedClip;
        if (clip is null || IsVoiceChanging || choice is null) return;

        var voiceId = choice.ElevenVoiceId ?? "";
        var deleteAfter = choice.TemporaryEleven;
        if (choice.IsLocal) { if (choice.LocalProfile is null) return; }
        else if (string.IsNullOrWhiteSpace(voiceId)) return;

        var path = clip.SourcePath;
        var srcStart = clip.SourceStartSeconds;
        var len = clip.LengthSeconds;
        var region = ClipRegionSeconds(clip);

        IsVoiceChanging = true;
        try
        {
            // Fortschritt (Bereitstellung + Konvertierung) sichtbar in der Statuszeile.
            var prog = choice.IsLocal ? new Progress<string>(s => SeparationStatus = s) : null;
            if (choice.IsLocal)
            {
                IsSeparating = true;
                SeparationStatus = "Bereite lokalen Stimmtausch vor (seed-vc — erster Lauf lädt das Modell) …";
                await _localVoice.DownloadModelAsync("seed-vc", prog);
                SeparationStatus = "Stimmtausch läuft (seed-vc) … das kann auf CPU lange dauern.";
            }

            var prep = await Task.Run(() =>
            {
                var (all, rate) = AudioProcessingHelper.ReadStereo(path);
                var startS = (int)Math.Clamp((long)(srcStart * rate) * 2, 0, all.Length);
                var lenS = (int)Math.Clamp((long)(len * rate) * 2, 0, all.Length - startS);
                var seg = new float[lenS];
                Array.Copy(all, startS, seg, 0, lenS);

                var lenFrames = lenS / 2;
                var a = region is null ? 0 : (int)Math.Clamp((long)(region.Value.aSec * rate), 0, lenFrames);
                var b = region is null ? lenFrames : (int)Math.Clamp((long)(region.Value.bSec * rate), 0, lenFrames);
                var aS = a * 2;
                var bS = b * 2;

                var sub = new float[bS - aS];
                Array.Copy(seg, aS, sub, 0, sub.Length);

                // Spitzenpegel des Originalausschnitts merken, um die laute STS-Ausgabe
                // darauf zurückzuskalieren (sonst „doppelt so laut“ / Clipping).
                var inPeak = 0f;
                foreach (var v in sub) inPeak = Math.Max(inPeak, Math.Abs(v));

                var t = TempDir.File("voice", ".wav", "in");
                AudioEdits.WriteWav(t, sub, rate);
                return (temp: t, seg, aS, bS, rate, inPeak, whole: a == 0 && b == lenFrames);
            });

            var (outSamples, outSr) = choice.IsLocal
                ? await _localVoice.ChangeVoiceAsync(prep.temp, choice.LocalProfile!, choice.DiffusionSteps, choice.AutoF0Adjust, prog)
                : await _voiceChange.ChangeAsync(prep.temp, voiceId);

            // STS-Ausgabe auf den Originalpegel angleichen (verhindert Übersteuern/zu laut).
            MatchPeak(outSamples, prep.inPeak);

            if (prep.whole)
            {
                ReplaceClipFromBuffer(clip, outSamples, outSr);
            }
            else
            {
                var outRes = AudioProcessingHelper.Resample(outSamples, outSr, prep.rate);
                var newBuf = AudioProcessingHelper.SpliceStereo(prep.seg, prep.aS, prep.bS, outRes);
                ReplaceClipFromBuffer(clip, newBuf, prep.rate);
            }

            _snackbar.Success("Stimme getauscht",
                prep.whole ? "Der Clip wurde ersetzt." : "Der markierte Bereich wurde ersetzt.");
        }
        catch (Exception ex)
        {
            Audiola.Services.UiError.Show("Stimmtausch fehlgeschlagen", ex.Message);
        }
        finally
        {
            IsVoiceChanging = false;
            IsSeparating = false;
            SeparationStatus = "";
            if (deleteAfter) try { await _voiceChange.DeleteVoiceAsync(voiceId); } catch { /* Aufräumen */ }
        }
    }

    /// <summary>Erzeugt aus Text Audio (lokal oder ElevenLabs) und legt es als neue Spur an.</summary>
    public async Task AddTextToSpeechTrackAsync(string text, VoiceChoice choice,
        double speed, double stability, double similarity)
    {
        if (string.IsNullOrWhiteSpace(text) || choice is null || IsVoiceChanging) return;

        IsVoiceChanging = true;
        _snackbar.Show("Sprache wird erzeugt …",
            choice.IsLocal ? "Lokales Modell — beim ersten Mal kann das einige Zeit dauern." : "Über ElevenLabs …",
            ControlAppearance.Info, new SymbolIcon(SymbolRegular.TextField24), TimeSpan.FromSeconds(6));
        try
        {
            float[] samples; int sr;
            if (choice.IsLocal)
            {
                if (choice.LocalProfile is null) { return; }
                (samples, sr) = await _localVoice.SpeakAsync(text, choice.LocalProfile, speed);
            }
            else
            {
                var voiceId = choice.ElevenVoiceId ?? "";
                if (string.IsNullOrWhiteSpace(voiceId)) return;
                try { (samples, sr) = await _voiceChange.SpeakAsync(text, voiceId, speed, stability, similarity); }
                finally { if (choice.TemporaryEleven) try { await _voiceChange.DeleteVoiceAsync(voiceId); } catch { } }
            }

            var temp = TempDir.File("voice", ".wav", "tts");
            AudioEdits.WriteWav(temp, samples, sr);

            await AddAudioFileAsync(temp, -1, 0);

            _snackbar.Success("Sprache erzeugt", "Neue Spur aus Text hinzugefügt.");
        }
        catch (Exception ex)
        {
            Audiola.Services.UiError.Show("Text-zu-Sprache fehlgeschlagen", ex.Message);
        }
        finally
        {
            IsVoiceChanging = false;
        }
    }

    /// <summary>Transkribiert den ausgewählten Clip (Whisper) und speichert das Ergebnis als LRC.</summary>
    public async Task TranscribeSelectedClipAsync(bool useElevenLabs = false)
    {
        var clip = SelectedClip;
        if (clip is null || string.IsNullOrEmpty(clip.SourcePath) || IsTranscribing) return;

        IsTranscribing = true;
        try
        {
            var segments = useElevenLabs
                ? await _voiceChange.TranscribeAsync(clip.SourcePath)
                : await _localVoice.TranscribeAsync(clip.SourcePath,
                    string.IsNullOrWhiteSpace(_settings.Current.WhisperModel) ? "base" : _settings.Current.WhisperModel);
            if (segments.Count == 0)
            {
                _snackbar.Warning("Keine Sprache erkannt", "Das Transkript ist leer.", 4);
                return;
            }

            // Am Projekt speichern (wird beim Export der Spur als Lyrics eingebettet).
            var lrc = LrcWriter.ToLrc(segments, clip.Track.Name);
            clip.Track.Lrc = lrc;

            // Optional zusätzlich als Datei sichern.
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Transkript zusätzlich als Datei speichern (optional)",
                Filter = "LRC-Lyrics (*.lrc)|*.lrc|Text (*.txt)|*.txt",
                FileName = System.IO.Path.GetFileNameWithoutExtension(clip.SourcePath) + ".lrc"
            };
            if (dialog.ShowDialog() == true)
            {
                var content = dialog.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                    ? LrcWriter.ToPlainText(segments) : lrc;
                await System.IO.File.WriteAllTextAsync(dialog.FileName, content);
            }

            _snackbar.Success("Transkribiert", $"{segments.Count} Segmente — wird beim Export der Spur eingebettet.", 4);
        }
        catch (Exception ex)
        {
            Audiola.Services.UiError.Show("Transkription fehlgeschlagen", ex.Message);
        }
        finally { IsTranscribing = false; }
    }

    /// <summary>
    /// Transkribiert eine Audiodatei per Whisper und gibt das Ergebnis als LRC zurück
    /// (null = keine Sprache erkannt). Wiederverwendbar für Tag-Editor und Export-Dialog.
    /// </summary>
    public async Task<string?> TranscribeFileToLrcAsync(string audioPath, string? title = null, bool useElevenLabs = false)
    {
        if (string.IsNullOrWhiteSpace(audioPath) || !System.IO.File.Exists(audioPath)) return null;
        IReadOnlyList<TranscriptSegment> segments;
        if (useElevenLabs)
        {
            segments = await _voiceChange.TranscribeAsync(audioPath);
        }
        else
        {
            var model = string.IsNullOrWhiteSpace(_settings.Current.WhisperModel) ? "base" : _settings.Current.WhisperModel;
            segments = await _localVoice.TranscribeAsync(audioPath, model);
        }
        return segments.Count == 0 ? null : LrcWriter.ToLrc(segments, title);
    }

    /// <summary>Skaliert <paramref name="samples"/> so, dass ihr Spitzenpegel dem Original entspricht.</summary>
    private static void MatchPeak(float[] samples, float targetPeak)
    {
        if (targetPeak <= 1e-6f) return;
        var peak = 0f;
        foreach (var v in samples) peak = Math.Max(peak, Math.Abs(v));
        if (peak <= 1e-6f) return;
        var gain = Math.Clamp(targetPeak / peak, 0.05f, 4f);
        for (var i = 0; i < samples.Length; i++) samples[i] *= gain;
    }
}
