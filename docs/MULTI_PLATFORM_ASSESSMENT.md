# Avalonia multi-platform assessment

## Decision

The desktop applications can be delivered for Windows, macOS, and Linux while
retaining their feature set and in-app updates. This is a migration, not a
project-file conversion: the current WPF front ends and the Windows-only audio
backend must be replaced behind stable application interfaces.

Avalonia is appropriate for the UI. Its XAML, layout, binding, MVVM, and routed
event concepts are close to WPF, but its styling, property system, event names,
and templates are not source-compatible. Use native Avalonia rather than XPF:
the objective is a maintainable cross-platform app rather than a compatibility
layer around the existing Windows UI.

## Current blockers

| Area | Current implementation | Required change |
| --- | --- | --- |
| Desktop UI | Audiola and Singola target `net10.0-windows`, use WPF, WPF-UI, WPF controls, dialogs, and custom drawing controls. | Create Avalonia desktop hosts and port all XAML, themes, dialogs, input handling, and custom controls to Avalonia. |
| Audio input/output | NAudio `WaveOutEvent`, `WaveInEvent`, and `AudioFileReader` are used throughout both applications. | Introduce audio-device, playback, recording, and decode interfaces; implement each with a backend supported on all three desktop platforms. |
| Audio encoding | MP3/AAC export uses NAudio Media Foundation. | Replace Media Foundation with a bundled cross-platform FFmpeg-based encoder/decoder; retain the managed FLAC and DSP paths where possible. |
| Python models | The managed environment assumes `%LocalAppData%`, `Scripts\python.exe`, and seed-vc's `.venv\Scripts\python.exe`. | Centralize platform paths and use `bin/python` on macOS/Linux. Validate every model's CPU, CUDA, and macOS acceleration path. Do not advertise DirectML outside Windows. |
| OS integration | `.audiola` registration writes the Windows Registry and calls `shell32.dll`. | Keep it as a Windows implementation and add macOS/Linux file-association installers or desktop-entry integration. |
| Embedded preview | WebView2 is a Windows-only dependency. | Use Avalonia's cross-platform browser integration only where required, with a system-browser fallback. |
| Releases | The workflow runs only on `windows-2025`, publishes only `win-x64`, and creates Windows setup executables. | Build and package each app on Windows, macOS, and Linux runners for the supported runtime identifiers. |

`Audiola.Core` is also currently Windows-targeted because it contains NAudio
and Media Foundation. It must be split before the UI migration: keep DSP,
project persistence, model orchestration, and process execution in a
platform-neutral core; move codecs and devices into platform/backend projects.

## Update strategy

Keep Velopack. Its distribution documentation specifies a macOS `.pkg` and a
Linux portable `.AppImage`, alongside Windows setup packages, and uses the same
release-feed model (`releases.{channel}.json`) for installed applications to
discover and apply updates.

The update abstraction should be shared by both apps and retain the existing
behaviour:

1. Run `VelopackApp.Build().Run()` before application initialization.
2. Use a platform-appropriate package and feed for each application and RID.
3. Preserve Audiola's prompted download/restart flow and Singola's background
   download/apply-on-next-exit flow.
4. Publish package assets and their release feeds atomically to GitHub Releases
   so an update never references a missing artifact.
5. Prove update, downgrade protection, and uninstall behaviour in clean VMs
   for Windows, macOS, and two mainstream Linux distributions.

The current `singola-win` channel must become a consistently named,
platform-aware Singola channel. A release-pipeline spike must verify the exact
Velopack channel and GitHub-source naming against the selected Velopack version
before migration, rather than guessing from the existing Windows assets.

## Migration sequence

1. Create a testable `net10.0` platform-neutral domain layer and move pure DSP,
   project bundles, metadata, workspace, model orchestration, and process
   execution into it. Establish regression tests for project loading/export,
   mastering, spatial rendering, and lyrics before replacing backends.
2. Define cross-platform audio contracts and implement a prototype that
   decodes, plays, records, seeks, mixes, and exports WAV/MP3/M4A/FLAC on all
   target systems. Make FFmpeg availability and licensing part of packaging.
3. Create Avalonia hosts for Audiola and Singola, port the common shell and
   themes, then migrate one complete feature slice at a time. Port custom
   waveform, spectrum, EQ, timeline, and pitch controls explicitly; they are
   not portable WPF controls.
4. Port Singola first as the smaller end-to-end vertical slice: loading,
   playback, microphone capture, pitch display, scoring, updates, and install.
   It exercises every high-risk platform capability with a smaller UI surface.
5. Port Audiola feature by feature: project I/O, transport/timeline, editor
   and effects, mastering/export, spatial audio, separation, voices, and
   provenance. Preserve the current `.audiola` format so projects remain
   interchangeable during rollout.
6. Replace the Windows-only workflow with a three-platform release matrix,
   package both products separately, and publish their update feeds. Add smoke
   tests for fresh install, update, and each feature category on every OS.

## Delivery gate

Do not remove the Windows WPF applications until the Avalonia versions pass the
same feature acceptance matrix: import and project round-trip, timeline edits,
recording/playback, all exports, mastering, spatial render, stem separation,
local/cloud voices, transcription, provenance, file opening, and automatic
updates. Windows remains a supported target throughout the staged rollout.

## Sources

- Avalonia get-started documentation: cross-platform .NET UI targeting Windows,
  macOS, Linux, iOS, Android, and WebAssembly.
- Avalonia WPF migration documentation: comparable XAML/MVVM concepts and
  the incompatible styling, property, template, and event areas.
- Velopack distribution documentation: release feeds, macOS `.pkg`, Linux
  `.AppImage`, and hosted update assets.
