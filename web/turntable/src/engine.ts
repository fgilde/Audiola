// Audio-Engine von <audiola-turntable>. Pro Deck liest ein AudioWorklet den dekodierten Track mit
// frei wählbarer Geschwindigkeit ('rate', auch negativ) – das ergibt Scratchen, Anlaufen und
// Pitch beim Abbremsen wie bei echtem Vinyl. rate 1 = normale Wiedergabe, 0 = Stillstand.
//
// Kette je Kanal:  Deck → EQ (Low/Mid/High) → Filter → Kanal-Fader → Crossfader → Bus
// Master:          Bus → Out (+ Echo-Send mit Feedback) → Lautsprecher; Airhorn direkt auf Out

const WORKLET = `
class Deck extends AudioWorkletProcessor {
  static get parameterDescriptors() { return [{ name: 'rate', defaultValue: 0 }] }
  constructor() {
    super()
    this.L = null; this.R = null; this.pos = 0; this.gen = 0; this.tick = 0
    this.port.onmessage = (e) => {
      const d = e.data
      if (d.L) { this.L = d.L; this.R = d.R; this.gen = d.gen }
      if (d.seek !== undefined) this.pos = d.seek * sampleRate
    }
  }
  process(_, outputs, params) {
    const oL = outputs[0][0], oR = outputs[0][1], r = params.rate, L = this.L, R = this.R
    if (!L) return true
    const n = L.length
    for (let i = 0; i < oL.length; i++) {
      const p = this.pos
      if (p >= 0 && p < n - 1) {
        const i0 = p | 0, f = p - i0
        oL[i] = L[i0] + (L[i0 + 1] - L[i0]) * f
        oR[i] = R[i0] + (R[i0 + 1] - R[i0]) * f
      } else { oL[i] = 0; oR[i] = 0 }
      this.pos = Math.max(0, p + (r.length > 1 ? r[i] : r[0]))
    }
    if (++this.tick % 8 === 0) this.port.postMessage({ gen: this.gen, pos: this.pos / sampleRate, ended: this.pos >= n - 1 })
    return true
  }
}
registerProcessor('dr-deck', Deck)
`

export type EqBand = 'low' | 'mid' | 'high'

export class Channel {
  private ctx?: AudioContext
  private node?: AudioWorkletNode
  private eq?: Record<EqBand, BiquadFilterNode>
  private filter?: BiquadFilterNode
  private fader?: GainNode
  private xf?: GainNode
  private analyser?: AnalyserNode
  private meterBuf = new Float32Array(512)
  private gen = 0
  onPosition: (sec: number, ended: boolean) => void = () => {}

  constructor(private engine: Engine) {}

  /** Baut die Kanal-Kette auf; ruft die Engine nach dem Laden des Worklets auf. */
  attach(ctx: AudioContext, bus: AudioNode) {
    this.ctx = ctx
    const node = (this.node = new AudioWorkletNode(ctx, 'dr-deck', { outputChannelCount: [2] }))
    node.port.onmessage = (e) => e.data.gen === this.gen && this.onPosition(e.data.pos, e.data.ended)
    this.eq = {
      low: new BiquadFilterNode(ctx, { type: 'lowshelf', frequency: 220 }),
      mid: new BiquadFilterNode(ctx, { type: 'peaking', frequency: 1000, Q: 0.9 }),
      high: new BiquadFilterNode(ctx, { type: 'highshelf', frequency: 3500 }),
    }
    this.filter = new BiquadFilterNode(ctx, { type: 'allpass' })
    this.fader = new GainNode(ctx)
    this.xf = new GainNode(ctx)
    this.analyser = new AnalyserNode(ctx, { fftSize: 512 })
    node
      .connect(this.eq.low)
      .connect(this.eq.mid)
      .connect(this.eq.high)
      .connect(this.filter)
      .connect(this.fader)
      .connect(this.xf)
      .connect(bus)
    this.xf.connect(this.analyser)
  }

  /** Kanalsignal nach Fader + Crossfader (für Ring-Visual); erst nach `unlock()` gesetzt. */
  get output(): AudioNode | undefined {
    return this.xf
  }

  /**
   * Lädt einen Track und springt auf `offset(dauer)` Sekunden. Liefert die Dauer oder null,
   * wenn inzwischen ein anderer Track angefordert wurde.
   */
  async load(url: string, offset: (duration: number) => number): Promise<number | null> {
    const gen = ++this.gen
    await this.engine.unlock()
    const res = await fetch(url)
    if (!res.ok) throw new Error(`${res.status} ${url}`)
    const buf = await this.ctx!.decodeAudioData(await res.arrayBuffer())
    if (gen !== this.gen) return null
    const L = buf.getChannelData(0).slice()
    const R = buf.getChannelData(buf.numberOfChannels > 1 ? 1 : 0).slice()
    this.node!.port.postMessage({ L, R, gen, seek: offset(buf.duration) }, [L.buffer, R.buffer])
    return buf.duration
  }

  seek(sec: number) {
    this.node?.port.postMessage({ seek: sec })
  }

  setRate(rate: number) {
    if (this.node) this.node.parameters.get('rate')!.setTargetAtTime(rate, this.ctx!.currentTime, 0.01)
  }

  /** -1 = Kill, 0 = neutral, 1 = +6 dB. */
  setEq(band: EqBand, v: number) {
    if (this.eq) this.eq[band].gain.value = v < 0 ? v * 26 : v * 6
  }

  /** DJ-Filter: -1 = Lowpass zu, 0 = neutral, 1 = Highpass zu. */
  setFilter(v: number) {
    const f = this.filter
    if (!f) return
    if (Math.abs(v) < 0.03) {
      f.type = 'allpass'
      return
    }
    f.type = v < 0 ? 'lowpass' : 'highpass'
    f.frequency.value = v < 0 ? 20000 * 2 ** (v * 10) : 20 * 2 ** (v * 10)
    f.Q.value = 1 + Math.abs(v) * 6
  }

  setVolume(v: number) {
    if (this.fader) this.fader.gain.setTargetAtTime(v * v, this.ctx!.currentTime, 0.01)
  }

  setCrossGain(v: number) {
    if (this.xf) this.xf.gain.setTargetAtTime(v, this.ctx!.currentTime, 0.01)
  }

  /** Pegel 0…1 für VU-Meter. */
  level(): number {
    if (!this.analyser) return 0
    this.analyser.getFloatTimeDomainData(this.meterBuf)
    let sum = 0
    for (const x of this.meterBuf) sum += x * x
    return Math.min(1, Math.sqrt(sum / this.meterBuf.length) * 3.2)
  }

  /** Alles auf neutral (beim Schließen des Mischpults). */
  reset() {
    for (const b of ['low', 'mid', 'high'] as const) this.setEq(b, 0)
    this.setFilter(0)
    this.setVolume(1)
  }
}

export class Engine {
  private ctx?: AudioContext
  private out?: GainNode
  private echoSend?: GainNode
  private ready?: Promise<void>
  readonly channels = [new Channel(this), new Channel(this)] as const

  /** Erster Aufruf muss aus einer Nutzer-Geste kommen (Autoplay-Policy). */
  unlock(): Promise<void> {
    if (!this.ready) {
      const ctx = (this.ctx = new AudioContext())
      const url = URL.createObjectURL(new Blob([WORKLET], { type: 'text/javascript' }))
      this.ready = ctx.audioWorklet.addModule(url).then(() => {
        const bus = new GainNode(ctx)
        const out = (this.out = new GainNode(ctx))
        const send = (this.echoSend = new GainNode(ctx, { gain: 0 }))
        const delay = new DelayNode(ctx, { delayTime: 0.375 })
        const feedback = new GainNode(ctx, { gain: 0.55 })
        bus.connect(out).connect(ctx.destination)
        bus.connect(send).connect(delay).connect(feedback).connect(delay)
        delay.connect(out)
        this.channels.forEach((c) => c.attach(ctx, bus))
      })
    }
    void this.ctx!.resume()
    return this.ready
  }

  /** Master-Ausgang für Analyzer; erst nach `unlock()` gesetzt. */
  get output(): AudioNode | undefined {
    return this.out
  }

  /** -1 = nur A, 0 = beide voll, 1 = nur B (DJ-Kurve ohne Pegelloch in der Mitte). */
  setCrossfader(x: number) {
    const [a, b] = this.channels
    a.setCrossGain(x <= 0 ? 1 : Math.cos((x * Math.PI) / 2))
    b.setCrossGain(x >= 0 ? 1 : Math.cos((-x * Math.PI) / 2))
  }

  /** Echo-Send auf/zu; der Nachhall läuft nach dem Loslassen über das Feedback aus. */
  echo(on: boolean) {
    if (this.echoSend) this.echoSend.gain.setTargetAtTime(on ? 0.8 : 0, this.ctx!.currentTime, 0.02)
  }

  /** Synthetisches Airhorn: gestapelte Sägezähne im klassischen „BAAP-BAP-BAP-BAAAP“-Rhythmus. */
  horn() {
    const ctx = this.ctx
    if (!ctx || !this.out) return
    const t0 = ctx.currentTime + 0.01
    const tone = new BiquadFilterNode(ctx, { type: 'lowpass', frequency: 2800, Q: 2 })
    const amp = new GainNode(ctx, { gain: 0 })
    tone.connect(amp).connect(this.out)
    const hits: [number, number][] = [[0, 0.16], [0.2, 0.1], [0.34, 0.1], [0.48, 0.55]]
    for (const [at, len] of hits) {
      amp.gain.setTargetAtTime(0.28, t0 + at, 0.008)
      amp.gain.setTargetAtTime(0, t0 + at + len, 0.02)
    }
    for (const f of [466, 470, 700]) {
      const o = new OscillatorNode(ctx, { type: 'sawtooth', frequency: f })
      o.frequency.setValueAtTime(f, t0 + 0.7)
      o.frequency.linearRampToValueAtTime(f * 0.94, t0 + 1.05)
      o.connect(tone)
      o.start(t0)
      o.stop(t0 + 1.2)
    }
  }

  /** Mischpult geschlossen: Crossfader Mitte, Kanäle neutral. */
  reset() {
    this.setCrossfader(0)
    this.channels.forEach((c) => c.reset())
  }

  close() {
    void this.ctx?.close()
  }
}
