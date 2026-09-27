// Minimaler ID3v2.3/2.4-Leser für hochgeladene MP3s: Titel, Künstler, Album, Tracknummer, Cover.
// ponytail: nur ID3v2 (MP3). M4A/FLAC/OGG-Tags werden nicht gelesen – dort greift der Dateiname.

export interface Tags {
  title?: string
  artist?: string
  album?: string
  track?: number
  picture?: { mime: string; data: Uint8Array }
}

const syncsafe = (b: Uint8Array, o: number) =>
  ((b[o] & 0x7f) << 21) | ((b[o + 1] & 0x7f) << 14) | ((b[o + 2] & 0x7f) << 7) | (b[o + 3] & 0x7f)
const u32 = (b: Uint8Array, o: number) => ((b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3]) >>> 0

/** Länge des ID3-Blocks inkl. Header (0 = kein ID3v2). Braucht nur die ersten 10 Bytes. */
export function id3Size(head: Uint8Array): number {
  if (head.length < 10 || head[0] !== 0x49 || head[1] !== 0x44 || head[2] !== 0x33) return 0
  return 10 + syncsafe(head, 6)
}

const DECODERS = ['latin1', 'utf-16', 'utf-16be', 'utf-8'].map((e) => new TextDecoder(e))

/** Text bis zum (encoding-abhängigen) Nullterminator; liefert Text + Folgeposition. */
function readText(b: Uint8Array, start: number, end: number, enc: number): [string, number] {
  const wide = enc === 1 || enc === 2
  let i = start
  if (wide) while (i + 1 < end && !(b[i] === 0 && b[i + 1] === 0)) i += 2
  else while (i < end && b[i] !== 0) i++
  const text = DECODERS[enc] ? DECODERS[enc].decode(b.subarray(start, i)) : ''
  return [text.replace(/^﻿/, '').trim(), Math.min(end, i + (wide ? 2 : 1))]
}

export function parseId3(buf: ArrayBuffer): Tags {
  const b = new Uint8Array(buf)
  const size = id3Size(b)
  if (!size) return {}
  const version = b[3]
  if (version !== 3 && version !== 4) return {}
  const end = Math.min(size, b.length)
  let p = 10
  if (b[5] & 0x40) p += version === 4 ? syncsafe(b, 10) : u32(b, 10) + 4 // Extended Header
  const tags: Tags = {}
  while (p + 10 <= end) {
    const id = String.fromCharCode(b[p], b[p + 1], b[p + 2], b[p + 3])
    if (!/^[A-Z0-9]{4}$/.test(id)) break // Padding erreicht
    const len = version === 4 ? syncsafe(b, p + 4) : u32(b, p + 4)
    const s = p + 10
    const e = Math.min(end, s + len)
    p = e
    if (len <= 0) continue
    const enc = b[s]
    if (id === 'TIT2') tags.title = readText(b, s + 1, e, enc)[0]
    else if (id === 'TPE1') tags.artist = readText(b, s + 1, e, enc)[0]
    else if (id === 'TALB') tags.album = readText(b, s + 1, e, enc)[0]
    else if (id === 'TRCK') tags.track = parseInt(readText(b, s + 1, e, enc)[0], 10) || undefined
    else if (id === 'APIC' && !tags.picture) {
      const [mime, afterMime] = readText(b, s + 1, e, 0)
      const [, afterDesc] = readText(b, afterMime + 1, e, enc) // +1 = Bildtyp-Byte
      tags.picture = { mime: mime.includes('/') ? mime : `image/${mime || 'jpeg'}`, data: b.slice(afterDesc, e) }
    }
  }
  return tags
}
