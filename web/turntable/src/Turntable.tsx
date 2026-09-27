import type { ComponentChildren } from 'preact'
import { useEffect, useRef, useState } from 'preact/hooks'
import type { Channel, Engine } from './engine'
import type { Strings } from './i18n'
import type { Rec } from './records'
import { createRing, type Viz } from './viz'

// Ein Plattenspieler:
// - Cover anklicken = Platte wechseln (startet Track 1)
// - Tonarm ziehen = Position auf der Platte wählen (außen = Track 1, innen = letzter Track),
//   zurück auf die Ablage = Stopp
// - Platte mit der Maus drehen = Scratchen (Richtung + Tempo folgen der Hand)
// - Pause bremst die Platte hörbar ab
// - Im DJ-Modus zusätzlich: Pitch ±8 %, 3 Hot-Cues (Klick setzt/springt, Doppelklick löscht), Backspin

const DEG_PER_SEC = 200 // 33⅓ rpm
const ARM_REST = -8 // Ablage neben der Platte
const ARM_OUTER = 4 // Nadel auf der äußersten Rille
const ARM_INNER = 27 // Nadel an der Auslaufrille
const PIVOT = { x: 0.98, y: 0.02 } // Tonarm-Lager relativ zur Bühne

const angleAround = (x: number, y: number, cx: number, cy: number) =>
  (Math.atan2(y - cy, x - cx) * 180) / Math.PI
const wrap180 = (d: number) => ((((d + 180) % 360) + 360) % 360) - 180
const pad2 = (n: number) => String(n).padStart(2, '0')

type Cue = { rec: Rec; track: number; pos: number } | null
/** Startpunkt: feste Sekunde oder Anteil der (erst nach dem Laden bekannten) Dauer. */
type At = { sec: number } | { frac: number }

export interface DeckStatus {
  playing: boolean
  rec: Rec | null
}

interface Props {
  side: 'a' | 'b'
  engine: Engine
  channel: Channel
  dj: boolean
  records: Rec[]
  logo: string | null
  uploads: boolean
  t: Strings
  onStatus: (side: 'a' | 'b', s: DeckStatus) => void
  /** Hochgeladene Dateien einsortieren; liefert die Platte, die aufgelegt werden soll. */
  onFiles: (files: File[]) => Promise<Rec | undefined>
  children?: ComponentChildren
}

export default function Turntable(p: Props) {
  const { side, engine, channel, dj, records, t } = p
  const [, setTick] = useState(0)
  const rerender = () => setTick((n) => n + 1)
  const stage = useRef<HTMLDivElement>(null)
  const platter = useRef<HTMLDivElement>(null)
  const arm = useRef<HTMLDivElement>(null)
  const ring = useRef<HTMLDivElement>(null)
  const fileInput = useRef<HTMLInputElement>(null)
  const viz = useRef<Viz | null>(null)
  const vizRec = useRef('')
  const props = useRef(p)
  props.current = p
  // Veränderlicher Zustand in einem Ref, damit rAF-Loop und Pointer-Handler nie veralten.
  const s = useRef({
    rec: null as Rec | null,
    track: 0,
    loaded: '', // "rec/track" des Buffers im Worklet
    needle: false, // Nadel auf der Platte = Ton
    loading: false,
    failed: false,
    pos: 0,
    rate: 1,
    angle: 0,
    armAngle: ARM_REST,
    scratch: null as null | { last: number; t: number; vel: number },
    armDrag: false,
    braking: false,
    pitch: 0, // ±0.08 = ±8 %
    cues: [null, null, null] as Cue[],
    dropping: false,
  }).current

  // Audio erst nach Nutzer-Geste starten; der Ring hängt am Kanalsignal.
  const unlock = () =>
    engine.unlock().then(() => {
      if (!viz.current && ring.current) viz.current = createRing(channel.output!, ring.current)
    })

  const play = (rec: Rec, track: number, at: At = { sec: 0 }) => {
    const key = `${rec.id}/${track}`
    Object.assign(s, { rec, track, needle: true, braking: false, failed: false })
    const dur = rec.tracks[track].duration
    if (s.loaded === key && dur) {
      s.pos = 'sec' in at ? at.sec : at.frac * dur
      channel.seek(s.pos)
    } else {
      s.loading = true
      s.loaded = ''
      s.pos = 0
      channel
        .load(rec.tracks[track].src, (d) => ('sec' in at ? at.sec : at.frac * d))
        .then((d) => {
          if (d === null) return
          rec.tracks[track].duration = d
          s.loaded = key
          s.loading = false
          rerender()
        })
        .catch(() => {
          Object.assign(s, { loading: false, needle: false, failed: true })
          rerender()
        })
    }
    void unlock()
    rerender()
  }

  const stop = () => {
    s.needle = false
    s.braking = false
    rerender()
  }

  const toggle = () => {
    void unlock()
    if (!s.rec) return records[0] && play(records[0], 0)
    if (s.needle && !s.braking) {
      s.braking = true // Motor aus, Platte läuft hörbar aus – Nadel hebt danach ab
    } else {
      if (!s.needle) s.rate = 0 // Anlaufen beim Weiterspielen
      s.needle = true
      s.braking = false
    }
    rerender()
  }

  const step = (d: number) => {
    if (!s.rec) return
    const n = s.track + d
    if (n >= 0 && n < s.rec.tracks.length) play(s.rec, n)
  }

  useEffect(() => {
    channel.onPosition = (pos, ended) => {
      if (s.loading) return
      s.pos = pos
      if (!ended || !s.needle || !s.rec) return
      if (s.track + 1 < s.rec.tracks.length) play(s.rec, s.track + 1)
      else Object.assign(s, { needle: false, track: 0, pos: 0 }), channel.seek(0), rerender()
    }

    let raf = 0
    let last = performance.now()
    const loop = (now: number) => {
      const dt = Math.min(0.05, (now - last) / 1000)
      last = now
      if (s.scratch) {
        // Hand steht still → Platte steht still.
        if (now - s.scratch.t > 50) s.scratch.vel *= 0.6
        s.rate = s.scratch.vel
      } else {
        // Motor zieht die Platte auf Solldrehzahl (inkl. Pitch) bzw. bremst sie ab.
        const target = s.braking ? 0 : 1 + s.pitch
        s.rate += (target - s.rate) * Math.min(1, dt * (s.braking ? 2.2 : 5))
        if (Math.abs(target - s.rate) < 0.002) s.rate = target
        if (s.braking && s.rate < 0.02) stop()
        s.angle += s.rate * DEG_PER_SEC * dt
      }
      channel.setRate(s.needle && !s.loading && !s.armDrag ? s.rate : 0)

      if (!s.armDrag) {
        let target = ARM_REST
        if (s.needle && s.rec) {
          const dur = s.rec.tracks[s.track].duration
          const inTrack = dur ? Math.min(1, s.pos / dur) : 0
          target = ARM_OUTER + ((ARM_INNER - ARM_OUTER) * (s.track + inTrack)) / s.rec.tracks.length
        }
        s.armAngle += (target - s.armAngle) * Math.min(1, dt * 6)
      }
      if (platter.current) platter.current.style.transform = `rotate(${s.angle}deg)`
      if (arm.current) arm.current.style.transform = `rotate(${s.armAngle}deg)`
      raf = requestAnimationFrame(loop)
    }
    raf = requestAnimationFrame(loop)
    return () => cancelAnimationFrame(raf)
  }, [])

  useEffect(() => {
    const playing = s.needle && !s.loading
    if (s.rec && vizRec.current !== s.rec.id) {
      viz.current?.setRecord(s.rec)
      if (viz.current) vizRec.current = s.rec.id
    }
    viz.current?.setActive(playing)
    props.current.onStatus(side, { playing: playing && !s.braking, rec: s.rec })
  })

  // Mischpult zu: Deck B verstummt, Pitch zurück auf 0.
  useEffect(() => {
    if (dj) return
    s.pitch = 0
    if (side === 'b') stop()
  }, [dj])

  // Platte wurde aus der Liste entfernt → Deck leeren.
  useEffect(() => {
    if (s.rec && !records.includes(s.rec)) Object.assign(s, { rec: null, needle: false }), rerender()
  }, [records])

  // --- Scratchen ---
  const center = () => {
    const r = stage.current!.getBoundingClientRect()
    return { cx: r.left + r.width / 2, cy: r.top + r.height / 2, r }
  }
  const onPlatterDown = (e: PointerEvent) => {
    void unlock()
    ;(e.currentTarget as HTMLElement).setPointerCapture(e.pointerId)
    const { cx, cy } = center()
    s.scratch = { last: angleAround(e.clientX, e.clientY, cx, cy), t: performance.now(), vel: 0 }
  }
  const onPlatterMove = (e: PointerEvent) => {
    if (!s.scratch) return
    const { cx, cy } = center()
    const a = angleAround(e.clientX, e.clientY, cx, cy)
    const now = performance.now()
    const delta = wrap180(a - s.scratch.last)
    const dt = Math.max(0.004, (now - s.scratch.t) / 1000)
    s.angle += delta
    const inst = Math.max(-4, Math.min(4, delta / dt / DEG_PER_SEC))
    s.scratch.vel = s.scratch.vel * 0.5 + inst * 0.5
    s.scratch.last = a
    s.scratch.t = now
  }
  const onPlatterUp = () => {
    s.scratch = null
  }

  // --- Tonarm ---
  const armAngleAt = (e: PointerEvent) => {
    const { r } = center()
    const px = r.left + PIVOT.x * r.width
    const py = r.top + PIVOT.y * r.height
    const a = (Math.atan2(-(e.clientX - px), e.clientY - py) * 180) / Math.PI
    return Math.max(ARM_REST, Math.min(ARM_INNER + 2, a))
  }
  const onArmDown = (e: PointerEvent) => {
    void unlock()
    e.stopPropagation()
    ;(e.currentTarget as HTMLElement).setPointerCapture(e.pointerId)
    s.armDrag = true
  }
  const onArmMove = (e: PointerEvent) => {
    if (s.armDrag) s.armAngle = armAngleAt(e)
  }
  const onArmUp = (e: PointerEvent) => {
    if (!s.armDrag) return
    s.armDrag = false
    const a = armAngleAt(e)
    const rec = s.rec ?? records[0]
    if (a < ARM_OUTER - 1.5 || !rec) return stop()
    const pos = Math.min(0.999, Math.max(0, (a - ARM_OUTER) / (ARM_INNER - ARM_OUTER))) * rec.tracks.length
    const track = Math.floor(pos)
    play(rec, track, { frac: pos - track })
  }

  // --- Eigene Songs ---
  const addFiles = async (files: File[]) => {
    void unlock()
    const rec = await props.current.onFiles(files)
    if (rec) play(rec, 0)
  }
  const onDrop = (e: DragEvent) => {
    if (!p.uploads || !e.dataTransfer?.files.length) return
    e.preventDefault()
    e.stopPropagation()
    s.dropping = false
    void addFiles([...e.dataTransfer.files])
  }
  const onDragOver = (e: DragEvent) => {
    if (!p.uploads || !e.dataTransfer?.types.includes('Files')) return
    e.preventDefault()
    if (!s.dropping) (s.dropping = true), rerender()
  }

  // --- DJ-Extras ---
  const hitCue = (i: number) => {
    void unlock()
    const c = s.cues[i]
    if (c) return play(c.rec, c.track, { sec: c.pos })
    if (!s.rec) return
    s.cues[i] = { rec: s.rec, track: s.track, pos: s.pos }
    rerender()
  }
  const clearCue = (i: number) => {
    s.cues[i] = null
    rerender()
  }

  const rec = s.rec
  const playing = s.needle && !s.braking
  const artist = rec && (rec.link ? <a href={rec.link} target="_blank" rel="noopener">{rec.artist}</a> : rec.artist)

  return (
    <div
      class={`at-deck at-deck-${side}${s.dropping ? ' is-dropping' : ''}`}
      style={rec ? { '--deck-accent': rec.accent } : undefined}
      onDragOver={onDragOver}
      onDragLeave={() => s.dropping && ((s.dropping = false), rerender())}
      onDrop={onDrop}
    >
      <div ref={stage} class="at-deck-stage">
        <div ref={ring} class="at-deck-ring" aria-hidden="true" />
        <div
          key={rec?.id ?? 'idle'}
          class="at-deck-disc"
          onPointerDown={onPlatterDown}
          onPointerMove={onPlatterMove}
          onPointerUp={onPlatterUp}
          onPointerCancel={onPlatterUp}
        >
          <div ref={platter} class="at-vinyl">
            {rec && (
              <div class="at-deck-label">
                {rec.cover ? <img src={rec.cover} alt="" draggable={false} /> : <span>{rec.title}</span>}
              </div>
            )}
          </div>
        </div>
        {!rec && side === 'a' && p.logo && <img class="at-vinyl-logo" src={p.logo} alt="" draggable={false} />}
        {s.dropping && <div class="at-deck-drop">{t.drop}</div>}
        <div
          ref={arm}
          class="at-vinyl-arm"
          onPointerDown={onArmDown}
          onPointerMove={onArmMove}
          onPointerUp={onArmUp}
          onPointerCancel={onArmUp}
        />
      </div>

      <div class="at-deck-ui">
        <div class="at-deck-sleeves">
          {records.map((r) => (
            <button
              key={r.id}
              class="at-deck-sleeve"
              style={{ '--sleeve-accent': r.accent }}
              aria-pressed={rec === r}
              title={`${r.artist} – ${r.title}`}
              onClick={() => play(r, 0)}
            >
              {r.cover ? <img src={r.cover} alt={`${r.artist} – ${r.title}`} /> : <span>{r.title}</span>}
            </button>
          ))}
          {p.uploads && (
            <button class="at-deck-sleeve at-deck-upload" title={t.upload} aria-label={t.upload} onClick={() => fileInput.current!.click()}>
              +
              <input
                ref={fileInput}
                type="file"
                accept="audio/*"
                multiple
                hidden
                onChange={(e) => {
                  const el = e.currentTarget as HTMLInputElement
                  void addFiles([...(el.files ?? [])])
                  el.value = ''
                }}
              />
            </button>
          )}
        </div>
        <div class="at-deck-now">
          <div class="at-deck-meta" aria-live="polite">
            {rec ? (
              <>
                <span class="at-deck-band">
                  {artist}
                  {rec.artist && ' · '}
                  {rec.title}
                </span>
                <span class="at-deck-track">
                  {pad2(s.track + 1)} · {rec.tracks[s.track].title}
                  {s.loading && ` · ${t.loading}`}
                  {s.failed && ` · ${t.error}`}
                </span>
              </>
            ) : (
              <>
                <span class="at-deck-band">{dj ? `${t.deck} ${side.toUpperCase()}` : t.pick}</span>
                <span class="at-deck-track">{dj ? t.pick : t.hint}</span>
              </>
            )}
          </div>
          <div class="at-deck-controls">
            <button class="at-deck-btn" onClick={() => step(-1)} aria-label={t.prev}>
              ⏮
            </button>
            <button class="at-deck-btn at-deck-btn-main" onClick={toggle} aria-label={playing ? t.pause : t.play}>
              {playing ? '❚❚' : '▶'}
            </button>
            <button class="at-deck-btn" onClick={() => step(1)} aria-label={t.next}>
              ⏭
            </button>
          </div>
        </div>
        {dj && (
          <div class="at-deck-dj">
            <label class="at-fader">
              <span>{t.pitch}</span>
              <input
                type="range"
                min={-0.08}
                max={0.08}
                step={0.001}
                defaultValue="0"
                onInput={(e) => (s.pitch = +(e.currentTarget as HTMLInputElement).value)}
                onDblClick={(e) => {
                  ;(e.currentTarget as HTMLInputElement).value = '0'
                  s.pitch = 0
                }}
              />
            </label>
            <div class="at-pads">
              {s.cues.map((c, i) => (
                <button
                  key={i}
                  class="at-pad"
                  aria-pressed={!!c}
                  title={c ? t.cueJump : t.cueSet}
                  onClick={() => hitCue(i)}
                  onDblClick={() => clearCue(i)}
                >
                  Cue {i + 1}
                </button>
              ))}
              <button
                class="at-pad"
                onClick={() => {
                  void unlock()
                  if (!s.scratch) s.rate = -4
                }}
              >
                {t.spin}
              </button>
            </div>
          </div>
        )}
        {p.children}
      </div>
    </div>
  )
}
