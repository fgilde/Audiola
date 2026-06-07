<div align="center">
  <img src="assets/logo.png" alt="Audiola" width="220" />

  <h1>Audiola</h1>

  <p>A modern Windows audio studio — edit audio, separate stems, remix and master.<br/>
  Built with .NET 9, WPF and a Fluent (WPF-UI / Mica) interface.</p>
</div>

---

## Features

- 🎛️ **Multitrack Studio** — a timeline workspace: import audio, cut/split/trim clips, move clips between tracks, fade-in/out handles, per-track region selection, resizable track height.
- 🧩 **AI stem separation (Demucs)** — automatic content detection and up to **6 stems** (vocals / drums / bass / guitar / piano / other), added straight to the timeline.
- 🎙️ **Vocal cleanup** — de-esser, harshness taming and gentle compression for cleaner vocals.
- 🗣️ **Voice changer** — ElevenLabs speech-to-speech (spoken voice; bring your own / licensed voice).
- 🎚️ **Mastering** — EQ → compressor → LUFS loudness (BS.1770 / EBU R128) with live **A/B preview**, presets, savable custom profiles and **batch/bulk processing** of many files.
- 📊 **Real-time spectrum** visualization in the header, master EQ, mixer panel and VU meters.
- 🧪 **Variation providers** — a pluggable effect framework (`IAudioVariationProvider`) applied to a clip, track or the whole mix.
- ↩️ **Full undo/redo** with a visual history you can jump to, and a native **project format** (`.audiola`) that bundles stems, clips, effects and settings — no re-extraction needed.
- 💾 **Export** as WAV / MP3 / AAC-M4A, plus per-track export.
- 🌙 Fluent design with a Dark theme and Mica backdrop.

## Install

Download the latest **`Audiola-win-Setup.exe`** from the [**Releases**](https://github.com/fgilde/Audiola/releases) page and run it. Audiola **updates itself automatically** on launch (powered by [Velopack](https://velopack.io)).

## Stem separation setup (optional)

Stem separation runs the local **Demucs** model via Python:

```powershell
# Demucs + a WAV writer backend
pip install -U demucs soundfile

# If separation finishes computing but fails with "Couldn't find appropriate backend",
# the torchaudio version doesn't match torch. Force a matching CPU pair:
pip install "torchaudio==2.8.0" --index-url https://download.pytorch.org/whl/cpu
```

In the app's **Settings**, set the Python path (e.g. `python`, `py` or a venv path) and use **“Check Demucs”** to verify. The first separation downloads the model and can take a few minutes on CPU (much faster with a CUDA GPU).

## Build from source

Requires the **.NET 9 SDK** on Windows.

```powershell
dotnet run --project src/Audiola/Audiola.csproj
```

## Releasing

Pushing a tag triggers the GitHub Actions workflow, which publishes the app, builds the **Velopack** installer and attaches it (plus the update feed) to a GitHub release:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

## Tech stack

.NET 9 · WPF · [WPF-UI](https://github.com/lepoco/wpfui) (Fluent) · [NAudio](https://github.com/naudio/NAudio) · CommunityToolkit.Mvvm · [Velopack](https://velopack.io) · [Demucs](https://github.com/facebookresearch/demucs)
