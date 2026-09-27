// Plattenlisten für <audiola-turntable>.
//
// JSON-Format (Attribut `records="…/records.json"` oder Property `el.records = […]`):
// { "records": [ {
//     "title": "Wir haben Ferien", "artist": "Die PapiBaras",
//     "cover": "papibaras/cover.webp", "link": "https://papibaras.de",
//     "accent": "#3de0ff", "gradient": ["#3de0ff", "#6a7bff", "#c93dff"],
//     "tracks": [ { "title": "Mach die Bahn frei", "src": "papibaras/01.mp3", "duration": 202.2 } ]
// } ] }
// Relative URLs gelten relativ zur JSON-Datei. `gradient`, `duration`, `cover`, `link` sind optional.

export interface Track {
  title: string
  src: string
  /** Sekunden; wird nach dem Laden automatisch ergänzt. */
  duration?: number
}

export interface Rec {
  id: string
  title: string
  artist: string
  cover?: string
  link?: string
  accent: string
  gradient: [string, string, string]
  tracks: Track[]
}

export const DEFAULT_ACCENT = '#c2ff3a'

const HEX = /^#[0-9a-f]{6}$/i

export const rgb = (hex: string) => [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16))
const toHex = (c: number[]) =>
  '#' + c.map((v) => Math.max(0, Math.min(255, Math.round(v))).toString(16).padStart(2, '0')).join('')
/** t > 0 aufhellen Richtung Weiß, t < 0 abdunkeln Richtung Schwarz. */
export const shade = (hex: string, t: number) =>
  toHex(rgb(hex).map((v) => (t > 0 ? v + (255 - v) * t : v * (1 + t))))

export const gradientFor = (accent: string): [string, string, string] => [
  shade(accent, 0.3),
  accent,
  shade(accent, -0.6),
]

let seq = 0
export const newId = () => `r${++seq}`

const str = (v: unknown) => (typeof v === 'string' ? v : '')

export function normalize(input: unknown, base: string): Rec[] {
  const list = Array.isArray(input) ? input : (input as { records?: unknown })?.records
  if (!Array.isArray(list)) return []
  const url = (v: unknown) => (str(v) ? new URL(str(v), base).href : undefined)
  return list.flatMap((r: any): Rec[] => {
    const tracks: Track[] = (Array.isArray(r?.tracks) ? r.tracks : [])
      .filter((t: any) => str(t?.src))
      .map((t: any, i: number) => ({
        title: str(t.title) || `Track ${i + 1}`,
        src: url(t.src)!,
        duration: typeof t.duration === 'number' ? t.duration : undefined,
      }))
    if (!tracks.length) return []
    const accent = HEX.test(str(r.accent)) ? r.accent : DEFAULT_ACCENT
    const g = Array.isArray(r.gradient) && r.gradient.length === 3 && r.gradient.every((c: unknown) => HEX.test(str(c)))
    return [
      {
        id: newId(),
        title: str(r.title) || 'Untitled',
        artist: str(r.artist),
        cover: url(r.cover),
        link: url(r.link),
        accent,
        gradient: g ? r.gradient : gradientFor(accent),
        tracks,
      },
    ]
  })
}
