# `<audiola-turntable>`

Freie Web-Component: Plattenspieler mit Scratchen und Tonarm, zwei Decks, DJ-Mischpult
(EQ, Filter, Fader, Crossfader, Hot-Cues, Echo, Airhorn) und Audio-Visuals. Eigene Songs per
Drag & Drop, Cover und Trackliste aus den ID3-Tags. Demo, Doku und Playground:
**https://audiola.de/turntable.html**

```html
<script type="module" src="https://audiola.de/widgets/turntable/v1.js"></script>
<audiola-turntable records="records.json" uploads></audiola-turntable>
```

## Entwickeln

```sh
npm install
npm run dev     # Dev-Server mit index.html
npm run check   # Selbsttest ID3-Parser
npm run build   # → ../../docs/widgets/turntable/v1.js (GitHub Pages)
```

`v1.js` ist die stabile URL. Brechende Änderungen an Attributen oder am JSON-Format → neue Datei `v2.js`.

| Datei | Inhalt |
|---|---|
| `src/index.tsx` | Custom Element, Attribute, Laden der Plattenliste |
| `src/App.tsx` | Layout, DJ-Modus, Hintergrund-Visual, Uploads |
| `src/Turntable.tsx` | ein Deck: Platte, Tonarm, Scratchen, Cues |
| `src/Mixer.tsx` | Mischpult |
| `src/engine.ts` | Web-Audio-Engine (AudioWorklet mit variabler Abspielrate) |
| `src/records.ts` | JSON-Format der Plattenliste |
| `src/id3.ts` / `src/uploads.ts` | Tags lesen, eigene Songs einsortieren |
