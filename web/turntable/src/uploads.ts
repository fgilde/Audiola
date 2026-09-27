import { id3Size, parseId3, type Tags } from './id3'
import { DEFAULT_ACCENT, gradientFor, newId, shade, type Rec } from './records'

// Eigene Songs: Dateien bleiben lokal (Blob-URLs), nichts wird hochgeladen.
// Gruppierung nach Album-Tag → eine Platte pro Album; weitere Dateien desselben Albums
// erweitern dessen Trackliste. Cover + Akzentfarbe kommen aus dem eingebetteten Bild.

async function readTags(file: File): Promise<Tags> {
  try {
    const size = id3Size(new Uint8Array(await file.slice(0, 10).arrayBuffer()))
    return size ? parseId3(await file.slice(0, size).arrayBuffer()) : {}
  } catch {
    return {}
  }
}

/** "03 - Mein_Song.mp3" → [3, "Mein Song"] */
function fromName(name: string): [number | undefined, string] {
  const base = name.replace(/\.[^.]+$/, '').replace(/_/g, ' ')
  const m = base.match(/^\s*(\d{1,3})\s*[-.)_ ]\s*(.+)$/)
  return m ? [parseInt(m[1], 10), m[2].trim()] : [undefined, base.trim()]
}

/** Mittlere Farbe des Covers, aufgehellt, damit sie auf Schwarz leuchtet. */
async function averageColor(url: string): Promise<string> {
  try {
    const img = await createImageBitmap(await (await fetch(url)).blob())
    const c = new OffscreenCanvas(16, 16)
    const g = c.getContext('2d')!
    g.drawImage(img, 0, 0, 16, 16)
    const d = g.getImageData(0, 0, 16, 16).data
    let r = 0, gg = 0, b = 0, n = 0
    for (let i = 0; i < d.length; i += 4) {
      const max = Math.max(d[i], d[i + 1], d[i + 2]), min = Math.min(d[i], d[i + 1], d[i + 2])
      const w = 1 + (max - min) / 32 // bunte Pixel zählen mehr als graue
      r += d[i] * w; gg += d[i + 1] * w; b += d[i + 2] * w; n += w
    }
    const hex = '#' + [r, gg, b].map((v) => Math.round(v / n).toString(16).padStart(2, '0')).join('')
    return shade(hex, 0.35)
  } catch {
    return DEFAULT_ACCENT
  }
}

/** Fügt Dateien hinzu; liefert die neue Plattenliste und die Platten, die neu/erweitert wurden. */
export async function addFiles(
  files: File[],
  records: Rec[],
  fallbackAlbum: string,
): Promise<{ records: Rec[]; touched: Rec[] }> {
  const audio = files.filter((f) => f.type.startsWith('audio/') || /\.(mp3|m4a|aac|wav|ogg|oga|opus|flac|webm)$/i.test(f.name))
  const next = [...records]
  const touched = new Set<Rec>()
  const entries = await Promise.all(audio.map(async (f) => ({ f, tags: await readTags(f) })))
  for (const { f, tags } of entries) {
    const [num, name] = fromName(f.name)
    const album = tags.album || fallbackAlbum
    const artist = tags.artist || ''
    let rec = next.find((r) => r.id.startsWith('u') && r.title === album && r.artist === artist)
    if (!rec) {
      rec = { id: 'u' + newId(), title: album, artist, accent: DEFAULT_ACCENT, gradient: gradientFor(DEFAULT_ACCENT), tracks: [] }
      next.push(rec)
    }
    if (!rec.cover && tags.picture) {
      rec.cover = URL.createObjectURL(new Blob([tags.picture.data as BlobPart], { type: tags.picture.mime }))
      rec.accent = await averageColor(rec.cover)
      rec.gradient = gradientFor(rec.accent)
    }
    const track = { title: tags.title || name, src: URL.createObjectURL(f), n: tags.track ?? num ?? 999 }
    rec.tracks.push(track)
    rec.tracks.sort((a: any, b: any) => (a.n ?? 999) - (b.n ?? 999))
    touched.add(rec)
  }
  return { records: next, touched: [...touched] }
}
