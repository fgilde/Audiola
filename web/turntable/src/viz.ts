import AudioMotionAnalyzer, { type ConstructorOptions } from 'audiomotion-analyzer'
import { rgb, type Rec } from './records'

// Spektrum-Balken im Hintergrund (Master) und ein radialer Ring um jede Platte (Kanalsignal).
// Farbverläufe kommen von den Platten; die Balken mischen sie nach hörbarem Pegel je Deck.

const COMMON: ConstructorOptions = {
  connectSpeakers: false,
  overlay: true,
  showBgColor: false,
  showScaleX: false,
  showPeaks: false,
  alphaBars: true,
  frequencyScale: 'log',
  smoothing: 0.72,
  minDecibels: -85,
  maxDecibels: -22,
  start: false,
}

export interface Viz {
  setRecord(rec: Rec): void
  /** Mischt die Verläufe der Platten nach Gewicht (z. B. hörbarer Pegel je Deck). */
  setMix(parts: [Rec, number][]): void
  setActive(on: boolean): void
}

function create(el: HTMLElement, options: ConstructorOptions): Viz {
  const a = new AudioMotionAnalyzer(el, { ...COMMON, ...options })
  const use = (name: string, stops: string[]) => {
    a.registerGradient(name, { bgColor: 'transparent', colorStops: [...stops] }) // mutiert das Array
    if (a.gradient !== name) a.gradient = name
  }
  let stopTimer = 0
  return {
    setRecord: (rec) => use(rec.id, rec.gradient),
    setMix(parts) {
      const total = parts.reduce((t, [, w]) => t + w, 0)
      if (total < 1e-4) return
      const stops = [0, 1, 2].map((i) => {
        const c = [0, 0, 0]
        for (const [rec, w] of parts) rgb(rec.gradient[i]).forEach((v, k) => (c[k] += (v * w) / total))
        return `rgb(${c.map(Math.round).join(',')})`
      })
      use('mix', stops)
    },
    setActive(on) {
      window.clearTimeout(stopTimer)
      el.classList.toggle('is-on', on)
      // Erst nach dem Ausblenden anhalten, damit die Balken sichtbar abklingen.
      if (on) !a.isOn && a.start()
      else stopTimer = window.setTimeout(() => a.stop(), 1200)
    },
  }
}

export const createBars = (source: AudioNode, el: HTMLElement) =>
  create(el, { source, mode: 3, barSpace: 0.3 })

export const createRing = (source: AudioNode, el: HTMLElement) =>
  create(el, { source, mode: 4, barSpace: 0.35, radial: true, radius: 0.7, spinSpeed: 2 })
