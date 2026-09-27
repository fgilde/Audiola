// Selbsttest für den ID3-Parser: `npm run check` (Node ≥ 22 führt .ts direkt aus).
import assert from 'node:assert/strict'
import { id3Size, parseId3 } from './id3.ts'

const enc = new TextEncoder()
const be32 = (n: number) => [n >>> 24, (n >>> 16) & 255, (n >>> 8) & 255, n & 255]
const safe = (n: number) => [(n >> 21) & 127, (n >> 14) & 127, (n >> 7) & 127, n & 127]

function frame(id: string, body: number[], v4: boolean) {
  return [...enc.encode(id), ...(v4 ? safe(body.length) : be32(body.length)), 0, 0, ...body]
}
function tag(frames: number[][], v4: boolean) {
  const body = [...frames.flat(), 0, 0, 0, 0] // Padding
  return new Uint8Array([0x49, 0x44, 0x33, v4 ? 4 : 3, 0, 0, ...safe(body.length), ...body]).buffer
}

const utf16 = (s: string) => [1, 0xff, 0xfe, ...[...s].flatMap((c) => [c.charCodeAt(0) & 255, c.charCodeAt(0) >> 8]), 0, 0]
const utf8 = (s: string) => [3, ...enc.encode(s)]
const jpg = [0xff, 0xd8, 0xff, 0xe0, 1, 2, 3]

for (const v4 of [false, true]) {
  const buf = tag(
    [
      frame('TIT2', utf16('Groß'), v4),
      frame('TPE1', utf8('Die PapiBaras'), v4),
      frame('TALB', [0, ...enc.encode('Wir haben Ferien')], v4),
      frame('TRCK', utf8('6/11'), v4),
      frame('APIC', [0, ...enc.encode('image/jpeg'), 0, 3, ...enc.encode('cover'), 0, ...jpg], v4),
    ],
    v4,
  )
  const t = parseId3(buf)
  assert.equal(t.title, 'Groß')
  assert.equal(t.artist, 'Die PapiBaras')
  assert.equal(t.album, 'Wir haben Ferien')
  assert.equal(t.track, 6)
  assert.equal(t.picture?.mime, 'image/jpeg')
  assert.deepEqual([...t.picture!.data], jpg)
  assert.equal(id3Size(new Uint8Array(buf)), buf.byteLength)
}
assert.deepEqual(parseId3(new Uint8Array([1, 2, 3]).buffer), {})
console.log('id3 ok')
