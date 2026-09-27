import { useEffect, useRef, useState } from 'preact/hooks'
import type { Channel, EqBand, Engine } from './engine'
import type { Strings } from './i18n'
import audiolaIcon from './audiola-icon.webp'

// Zwei-Kanal-Mischpult zwischen den Decks: EQ (Hi/Mid/Low bis Kill), Filter, Kanal-Fader mit
// VU-Meter, Crossfader, Master-FX Echo (halten) und Airhorn. Doppelklick setzt Regler zurück.

/** Drehregler -1…1, Mitte = neutral. Ziehen hoch/runter, Pfeiltasten, Doppelklick = Mitte. */
function Knob({ label, onChange }: { label: string; onChange: (v: number) => void }) {
  const [v, setV] = useState(0)
  const drag = useRef<{ y: number; v: number } | null>(null)
  const set = (x: number) => {
    const c = Math.max(-1, Math.min(1, Math.abs(x) < 0.04 ? 0 : x))
    setV(c)
    onChange(c)
  }
  const onKey = (e: KeyboardEvent) => {
    const d = ({ ArrowUp: 0.1, ArrowRight: 0.1, ArrowDown: -0.1, ArrowLeft: -0.1 } as Record<string, number>)[e.key]
    if (d) (e.preventDefault(), set(v + d))
  }
  return (
    <div class="at-knob-wrap">
      <div
        class="at-knob"
        role="slider"
        tabIndex={0}
        aria-label={label}
        aria-valuemin={-1}
        aria-valuemax={1}
        aria-valuenow={Math.round(v * 100) / 100}
        style={{ '--knob': `${v * 135}deg` }}
        onPointerDown={(e) => {
          ;(e.currentTarget as HTMLElement).setPointerCapture(e.pointerId)
          drag.current = { y: e.clientY, v }
        }}
        onPointerMove={(e) => drag.current && set(drag.current.v + (drag.current.y - e.clientY) / 90)}
        onPointerUp={() => (drag.current = null)}
        onPointerCancel={() => (drag.current = null)}
        onDblClick={() => set(0)}
        onKeyDown={onKey}
      />
      <span>{label}</span>
    </div>
  )
}

function Strip(p: { channel: Channel; name: string; t: Strings; meter: (el: HTMLElement | null) => void }) {
  const eq = (band: EqBand) => (v: number) => p.channel.setEq(band, v)
  return (
    <div class="at-mixer-strip">
      <span class="at-mixer-ch">{p.name}</span>
      <Knob label="Hi" onChange={eq('high')} />
      <Knob label="Mid" onChange={eq('mid')} />
      <Knob label="Low" onChange={eq('low')} />
      <Knob label="Filter" onChange={(v) => p.channel.setFilter(v)} />
      <div class="at-mixer-faderbox">
        <div class="at-mixer-vu">
          <div ref={p.meter} class="at-mixer-vu-fill" />
        </div>
        <div class="at-fader at-mixer-vfader">
          <input
            type="range"
            min={0}
            max={1}
            step={0.01}
            defaultValue="1"
            aria-label={`${p.t.volume} ${p.name}`}
            onInput={(e) => p.channel.setVolume(+(e.currentTarget as HTMLInputElement).value)}
          />
        </div>
      </div>
    </div>
  )
}

interface Props {
  engine: Engine
  active: boolean
  t: Strings
  onClose: () => void
}

export default function Mixer({ engine, active, t, onClose }: Props) {
  const meters = useRef<(HTMLElement | null)[]>([])

  // VU-Meter nur bei offenem Mischpult zeichnen.
  useEffect(() => {
    if (!active) return
    let raf = 0
    const tick = () => {
      engine.channels.forEach((c, i) => {
        const el = meters.current[i]
        if (el) el.style.transform = `scaleY(${c.level()})`
      })
      raf = requestAnimationFrame(tick)
    }
    raf = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(raf)
  }, [active, engine])

  const echo = (on: boolean) => void engine.unlock().then(() => engine.echo(on))

  return (
    <div class="at-mixer">
      <button class="at-dj-toggle" onClick={onClose}>
        {t.close}
      </button>
      <div class="at-mixer-strips">
        {engine.channels.map((c, i) => (
          <Strip key={i} channel={c} name={i ? 'B' : 'A'} t={t} meter={(el) => (meters.current[i] = el)} />
        ))}
      </div>
      <label class="at-fader at-mixer-xfader">
        <span>{t.crossfader}</span>
        <input
          type="range"
          min={-1}
          max={1}
          step={0.01}
          defaultValue="0"
          onInput={(e) => engine.setCrossfader(+(e.currentTarget as HTMLInputElement).value)}
          onDblClick={(e) => {
            ;(e.currentTarget as HTMLInputElement).value = '0'
            engine.setCrossfader(0)
          }}
        />
      </label>
      <div class="at-pads">
        <button
          class="at-pad"
          onPointerDown={() => echo(true)}
          onPointerUp={() => echo(false)}
          onPointerLeave={() => echo(false)}
          onPointerCancel={() => echo(false)}
          onKeyDown={(e) => (e.key === ' ' || e.key === 'Enter') && echo(true)}
          onKeyUp={() => echo(false)}
        >
          Echo
        </button>
        <button class="at-pad" onClick={() => void engine.unlock().then(() => engine.horn())}>
          Horn
        </button>
      </div>
      <a class="at-powered" href="https://audiola.de/turntable.html" target="_blank" rel="noopener">
        {t.poweredBy} <img src={audiolaIcon} alt="" /> Audiola
      </a>
    </div>
  )
}
