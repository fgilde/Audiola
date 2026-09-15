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

## Stand der Umsetzung

Die Migration ist durchgeführt. Beide Oberflächen teilen dieselbe Logik; die
Avalonia-Fassung wird auf Windows, macOS und Linux ausgeliefert.

| Bereich | Umsetzung |
| --- | --- |
| Geteilte Schicht | `Audiola.App` — alle ViewModels und Dienste, host-neutral über `INotifier`, `IFileDialogs`, `IAppDialogs`, `IShellNavigation`, `IAppTheme`, `DispatcherHelper`, `UiTimer` |
| Oberfläche | Alle 13 Seiten, 11 Steuerelemente und 9 Dialoge portiert; Theme über Avalonias `ThemeDictionaries` (Hell/Dunkel ohne Brush-Ersetzung) |
| Wiedergabe / Aufnahme | `PortableWaveOut` / `PortableWaveIn`: unter Windows NAudio, sonst miniaudio (CoreAudio, ALSA/PulseAudio) |
| Decode | `PortableAudioFile`: unter Windows `AudioFileReader`, sonst FFmpeg-Vorstufe nach WAV mit Zwischenspeicher |
| Encode | MP3/AAC unter Windows über Media Foundation, sonst FFmpeg; FLAC durchgängig über FLAKE |
| Geräte | `AudioDevices.InputNames` statt `WaveInEvent.DeviceCount`; die Mikrofonauswahl fragt `IAudioPlatform` |
| Releases | Ein Produkt für alle Plattformen aus `src/Audiola.Avalonia` |

## Singola nach dem Umbau

Singola war beim Umbau unfertig geblieben; diese Lücken sind geschlossen:

| Befund | Behandlung |
| --- | --- |
| `SingolaHostViewModel` — toter Demo-Rest, von nichts referenziert | entfernt |
| Song per Drag & Drop ließ sich nicht ablegen, obwohl die Oberfläche es anbot | `DragDrop.AllowDrop` samt Handler nachgezogen |
| Song als Startargument („Öffnen mit …“) wurde ignoriert | `PendingStartupFile` wie in der WPF-Fassung |
| Mikrofone wurden nur beim Programmstart gelesen — ein später angestecktes Mikro blieb unsichtbar | lebende Geräteliste plus „Aktualisieren“; beim Zurück ins Setup automatisch |
| Ein klemmendes Mikrofon fiel erst am Punktestand von 0 auf | Mikrofon-Test mit Pegelbalken, erkanntem Ton und Fehlertext pro Spieler |
| Ein fehlgeschlagenes Mikrofon riss die ganze Runde ab | Runde läuft weiter, der betroffene Platz nennt den Grund |
| Updates wurden still geladen und beim Beenden eingespielt | `SingolaUpdates` meldet die Version und bietet den Neustart an |
| Fehlermeldungen der Audio-Schicht waren englisch | eingedeutscht (gilt auch für Audiola, das dieselben Dateien mitkompiliert) |

Neu dazugekommen: **Mitschnitt der Runde herunterladen.** `KaraokeEngine`
schreibt pro Spieler ein WAV mit, `KaraokeExport` mischt es über die Musik des
Songs (Song als Leitspur, Stimmen geteilter Kopfraum, Normalisierung gegen
Clipping) und schreibt WAV, MP3, M4A oder FLAC über den vorhandenen
`AudioExporter`. Im Ergebnis gibt es dafür je Spieler „Aufnahme“ und, sobald
mehrere Mitschnitte vorliegen, „Alle zusammen als Duett“.

## Bedienoberfläche nach dem Umbau (Durchgang September 2026)

Alle Seiten und Dialoge wurden mit Bild aufgenommen und gegen das Studio als Vorbild
sowie gegen die Konventionen gängiger Audio-Programme abgeglichen. Ergebnis:

| Bereich | Befund | Behandlung |
| --- | --- | --- |
| Theme | Werkzeugleisten-Gruppen, Abschnitts-Überschriften und Leerzustände waren nur lokal (Studio, Stimmen) definiert | ins Theme gehoben: `toolbar`, `toolGroup`, `toolBtn` (nimmt keinen Fokus an), `sectionCaption`, `emptyState`, `sliderRow` |
| Editor | Seite ließ sich seit v1.2.9 nicht öffnen (`InputGesture="Esc"` — Avalonia kennt nur `Escape`; die Ausnahme fing der globale Fehlerfang still ab) | behoben; Kopfzeile mit primären Aktionen rechts, Werkzeugleiste in Gruppen mit Kürzel-Tooltips, Regler mit Wertanzeige, Kontextmenü mit Icons, Leerzustand |
| Klangvariation | ASCII-Umlaute im Text, Presets als nackte Textknöpfe, Regler ohne Wertanzeige | Umlaute, Presets als erklärte Karten, zwei Spalten, Export in der Kopfzeile |
| Equalizer | lose Werkzeugzeile | Gruppenmuster, Export in der Kopfzeile, Legende mit Bedienhinweis |
| Evaluation, Provenienz, Spatial | ohne Daten große leere Flächen | Leerzustände mit Erklärung und nächstem Schritt |
| Dialoge | Primärknöpfe ohne Icon/Tooltip, Emoji im Knopftext | Icons und Tooltips (Stimmtausch, Text-zu-Sprache, Variationen, Einsingen) |
| Einstellungen, Metadaten | nutzten nur ~600 px Breite | Breite angehoben |
| Mastering | Transport ohne Tooltips | ergänzt, Fokus abgeschaltet |

Entwicklungs-Hilfe: `Audiola.exe <datei> --page <Seite>` öffnet direkt eine Seite — für
Screenshots und Tests, ohne sich durch die Oberfläche zu klicken.

### Studio-Arbeitsfläche (zweiter Durchgang)

Rückmeldung nach dem ersten Durchgang: die Leiste zerfiel in sieben Kästen
unterschiedlicher Höhe, der Clip-Inspektor sprang als zweite breite Zeile auf,
der Spurkopf war gedrängt, und bei kleinem Zoom stand die Timeline zentriert
statt am Spurkopf.

| Stelle | vorher | jetzt |
| --- | --- | --- |
| Werkzeugleiste | sieben Kästen mit Überschriften, Kippschalter und Combos dazwischen, zwei Zeilen hoch | eine Zeile: gleich große, flache Icon-Knöpfe, Sinngruppen durch dünne Striche getrennt, Schalter als ToggleButton mit Akzent im An-Zustand (Muster jetzt auch im Editor und Equalizer) |
| Clip-Regler | eigene Karte, sprang beim Auswählen auf und verschob das Layout | Zeile in derselben Karte, Regler mit Wert daneben, Aktionen rechts |
| Spurkopf | Checkbox, zwei Icon-Knöpfe, zwei unbeschriftete Regler, M/S | Farbstreifen, Name mit „…"-Menü (öffnet dasselbe Menü wie der Rechtsklick), M/S/Aktiv in Bedeutungsfarben, Fader und Panorama mit Wert, Pegelbalken am Rand |
| Zoom | passte der Song in die Breite, zentrierte der ScrollViewer den Inhalt — Zeitachse und Spurkopf standen versetzt | Inhalt linksbündig; das Raster endet unter der letzten Spur |
| Clip | nur Wellenform, große weiße Fade-Griffe | Name im Clip, Griffe treten erst beim Überfahren hervor |
| Mixer | Kanalzüge wuchsen mit der freien Fläche mit | feste, kompakte Streifen mit Fader, Pegel, Wert und M/S |
| Spurköpfe/Lineal | „Hidden" zeichnete weiter eine Laufspur — ein Strich neben den Spurköpfen | Balken ausgeblendet |

### Offene Punkte

- Die Abnahmematrix ist unter Windows durchgelaufen (Projekt-Roundtrip, Timeline,
  Wiedergabe, Export, Seitenwechsel). Auf macOS und Linux fehlt der Durchlauf auf
  echter Hardware — insbesondere Mikrofon-Auswahl, Aufnahme-Latenz und die
  FFmpeg-Decode-Strecke.
- `src/Audiola` (WPF) bleibt als Windows-Rückfallebene im Repo und baut weiter,
  wird aber nicht mehr veröffentlicht. Nach bestätigtem Durchlauf auf macOS und
  Linux kann es entfernt werden.
- Singolas Mitschnitt ist auf dieser Maschine nicht mit echtem Mikrofon
  geprüft: die Sitzung ist eine Remote-Sitzung, in der Windows keine
  Aufnahmegeräte durchreicht (`WaveInEvent.DeviceCount` = 0). Geprüft sind der
  Mix-Weg mit erzeugten Aufnahmen (WAV/MP3/FLAC, Resampling 48 → 44,1 kHz,
  kürzere Stimme als Song, Normalisierung) und die Fehlerbehandlung ohne
  Mikrofon. Offen bleibt eine Runde mit echtem Mikrofon.
- Die Dateiverknüpfung für `.audiola` ist weiter Windows-spezifisch
  (`FileAssociation`, Registry). macOS/Linux brauchen `Info.plist` bzw. eine
  `.desktop`-Datei mit MIME-Typ.
