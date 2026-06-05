# Audiola

Ein modernes Windows-Audio-Tool mit **WPF Fluent UI** (WPF-UI / Mica) zum Bearbeiten von
Audiodateien, **Trennen in Stems** (Vocals / Drums / Bass / Other) und **Neumischen**.

## Funktionen

- 🎵 Audiodateien laden & abspielen (WAV, MP3, FLAC, AIFF, M4A, OGG) via NAudio
- 📈 Wellenform-Anzeige mit Klick-zum-Springen
- 🎚️ Transport (Play / Pause / Stop), Lautstärke, Positionsanzeige
- 🧩 **Stems & Mix** (eine Arbeitsfläche): lokale Stem-Trennung mit **Demucs** (Meta),
  dann schicke Stem-Liste mit An/Aus-Checkbox, Lautstärke, Panorama, Mute, Solo —
  **Live-Wiedergabe nur der aktiven Stems** (sample-synchron) mit Transport & Seek
- 💾 Export als **WAV (Standard), MP3 oder AAC/M4A** — Format einfach im Windows-
  Speichern-Dialog wählbar (MP3/AAC via Windows Media Foundation)
- 🎚️ **Mastering**: EQ (HP/Low-Shelf/Peak/High-Shelf), Kompressor, LUFS-Messung &
  Normalisierung (BS.1770 / EBU R128)
- ✨ **Klangvariation**: erzeugt songnahe Varianten mit subtiler Klangneigung,
  Praesenz-, Stereo-, Saettigungs- und Textur-Anpassung sowie reproduzierbarem Seed
- 🔎 **Provenienz & KI-Erkennung**: liest eingebettete Herkunfts-/Wasserzeichen-Daten
  (C2PA, XMP, ID3, Generator-Spuren) aus und erklärt, warum ein Track als KI erkannt
  werden kann — reine Analyse, optional mit `c2patool` für volle C2PA-Manifeste
- 🌙 Fluent-Design mit Dark/Light-Theme und Mica-Hintergrund

## Voraussetzungen

- **.NET 9 SDK** (Windows)
- Für die Stem-Trennung: **Python** mit Demucs

```powershell
# Demucs + WAV-Schreib-Backend installieren
pip install -U demucs soundfile

# Falls die Trennung am Ende mit "Couldn't find appropriate backend" abbricht,
# passt die torchaudio-Version nicht zur torch-Version. Passendes CPU-Paar erzwingen:
pip install "torchaudio==2.8.0" --index-url https://download.pytorch.org/whl/cpu
```

> `soundfile` ist nötig, weil torchaudio 2.8 auf Windows sonst kein Backend zum
> Speichern der Stem-WAVs findet (die Trennung rechnet sonst durch und scheitert erst
> am Schreiben mit Exit-Code 1).

In den **Einstellungen** der App den Python-Pfad eintragen (z. B. `python`, `py`
oder ein venv-Pfad) und über „Demucs prüfen" die Verfügbarkeit testen.

> Hinweis: Die erste Trennung lädt das Demucs-Modell herunter und kann auf CPU
> mehrere Minuten dauern. Mit CUDA-fähiger GPU geht es deutlich schneller.

## Bauen & Starten

```powershell
dotnet build Audiola.sln -c Debug
dotnet run --project src/Audiola/Audiola.csproj
```

## Projektstruktur

```
src/Audiola/
├─ Models/        AudioTrack, Stem, StemSet
├─ Services/      Wiedergabe (NAudio), Wellenform, Demucs, Mixdown, Settings
├─ ViewModels/    MVVM (CommunityToolkit.Mvvm), geteilter SessionState
├─ Views/         FluentWindow + Seiten (Home, Editor, Stems, Mixer, Settings)
├─ Controls/      WaveformControl (eigene Render-Logik)
└─ Converters/    Visibility-Converter
```

## Architektur

- **MVVM** mit `CommunityToolkit.Mvvm` (Source-Generatoren für `ObservableProperty` / `RelayCommand`).
- **Dependency Injection** über `Microsoft.Extensions.Hosting`; Navigation, Snackbar
  und Dialoge kommen aus WPF-UI 3.0.5.
- **Stem-Trennung** ruft Demucs als externen Prozess (`python -m demucs`) auf und
  liest die erzeugten WAV-Dateien aus `<Ausgabe>/<modell>/<track>/`.
