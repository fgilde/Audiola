import { shade } from './records'

// Farben aus einem Cover: Akzent = häufigster leuchtender Farbton, dazu ein
// heller leuchtender Stop und ein dunkler Stop im zweitstärksten Farbton → Verlauf für die Visuals.
// Fremde Cover brauchen CORS; ohne CORS bleibt es bei den Standardfarben.

const hex = (c: number[]) => '#' + c.map((v) => Math.round(Math.max(0, Math.min(255, v))).toString(16).padStart(2, '0')).join('')

function hsv(r: number, g: number, b: number) {
  const max = Math.max(r, g, b), min = Math.min(r, g, b), d = max - min
  let h = 0
  if (d) h = max === r ? ((g - b) / d) % 6 : max === g ? (b - r) / d + 2 : (r - g) / d + 4
  return { h: (h * 60 + 360) % 360, s: max ? d / max : 0, v: max / 255 }
}

/** Macht eine Farbe kräftig genug, um auf Schwarz zu leuchten (Mindest-Sättigung und -Helligkeit). */
function lift(c: number[], minV: number, minS = 0.8) {
  const { h, s, v } = hsv(c[0], c[1], c[2])
  const S = Math.max(s, minS), V = Math.max(v, minV / 255)
  const f = (n: number) => {
    const k = (n + h / 60) % 6
    return 255 * V * (1 - S * Math.max(0, Math.min(k, 4 - k, 1)))
  }
  return [f(5), f(3), f(1)]
}

export async function palette(url: string): Promise<{ accent: string; gradient: [string, string, string] } | null> {
  try {
    const res = await fetch(url)
    if (!res.ok) return null
    const img = await createImageBitmap(await res.blob())
    const size = 24
    const canvas = new OffscreenCanvas(size, size)
    const g = canvas.getContext('2d')!
    g.drawImage(img, 0, 0, size, size)
    const d = g.getImageData(0, 0, size, size).data

    // 12 Farbton-Eimer à 30°. Zuerst zählen nur leuchtende Pixel (Meer, Lichter, Feuer) – große
    // gedeckte Flächen wie Haut, Sand, Fell oder Holz würden sonst fast jedes Cover braun machen.
    // Gibt es kaum leuchtende Pixel, entscheiden alle farbigen Pixel.
    const rank = (vivid: boolean) => {
      const buckets = Array.from({ length: 12 }, () => ({ w: 0, r: 0, g: 0, b: 0, best: [0, 0, 0], bestScore: 0 }))
      for (let i = 0; i < d.length; i += 4) {
        const [r, gg, b] = [d[i], d[i + 1], d[i + 2]]
        const { h, s, v } = hsv(r, gg, b)
        if (vivid ? s < 0.5 || v < 0.6 : s < 0.2 || v < 0.15) continue
        const w = s * v
        const k = buckets[Math.floor(h / 30) % 12]
        k.w += w; k.r += r * w; k.g += gg * w; k.b += b * w
        const score = s * v * v
        if (score > k.bestScore) (k.bestScore = score), (k.best = [r, gg, b])
      }
      // Nachbar-Eimer zählen halb mit, damit ein Farbton an einer 30°-Grenze nicht zerfällt
      const score = (i: number) => buckets[i].w + 0.5 * (buckets[(i + 11) % 12].w + buckets[(i + 1) % 12].w)
      return buckets
        .map((k, i) => ({ ...k, i, score: score(i) }))
        .filter((k) => k.w > 0)
        .sort((a, b) => b.score - a.score)
    }
    const vivid = rank(true)
    const ranked = vivid.reduce((t, k) => t + k.w, 0) > size * size * 0.04 ? vivid : rank(false)
    if (!ranked.length) return null
    const main = ranked[0]
    // zweiter Farbton mit Abstand ≥ 60°, sonst der Hauptton selbst
    const second = ranked.find((k) => Math.min(Math.abs(k.i - main.i), 12 - Math.abs(k.i - main.i)) >= 2) ?? main
    const avg = (k: typeof main) => [k.r / k.w, k.g / k.w, k.b / k.w]

    const accent = hex(lift(avg(main), 215))
    return {
      accent,
      gradient: [shade(hex(lift(main.best, 235)), 0.2), accent, shade(hex(lift(avg(second), 200)), -0.45)],
    }
  } catch {
    return null
  }
}
