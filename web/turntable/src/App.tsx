import type { ComponentChildren } from 'preact'
import { useEffect, useRef, useState } from 'preact/hooks'
import { Engine } from './engine'
import { strings } from './i18n'
import Mixer from './Mixer'
import type { Rec } from './records'
import Turntable, { type DeckControl, type DeckStatus } from './Turntable'
import { addFiles } from './uploads'
import { createBars, type Viz } from './viz'

type Side = 'a' | 'b'

const idle: DeckStatus = { playing: false, loading: false, failed: false, rec: null, track: 0 }

export interface AppProps {
  host: HTMLElement
  /** Wird mit der Steuerung beider Decks befüllt (Methoden play/pause/next/previous des Elements). */
  controls: Partial<Record<Side, DeckControl>>
  /** Meldet die Platten inkl. eigener Songs, damit el.play(index) auch diese findet. */
  onRecords: (all: Rec[]) => void
  records: Rec[]
  decks: 1 | 2
  mode: 'single' | 'dj'
  uploads: boolean
  logo: string | null
  lang: string | null
}

export default function App(p: AppProps) {
  const t = strings(p.lang)
  const [engine] = useState(() => new Engine())
  const [dj, setDj] = useState(p.decks === 2 && p.mode === 'dj')
  // Mixer bei jedem Öffnen frisch mounten, damit Regler und (zurückgesetzte) Engine übereinstimmen.
  const [session, setSession] = useState(0)
  const [uploaded, setUploaded] = useState<Rec[]>([])
  const [hasHero, setHasHero] = useState(false)
  const bgEl = useRef<HTMLDivElement>(null)
  const djOnly = useRef<HTMLElement[]>([])
  const bars = useRef<Viz | null>(null)
  const status = useRef<Record<Side, DeckStatus>>({ a: idle, b: idle })
  const sent = useRef<Record<Side, string>>({ a: '', b: '' })
  const records = [...p.records, ...uploaded]
  p.onRecords(records)

  useEffect(() => () => engine.close(), [engine])

  // Deck B + Mixer sind nur im DJ-Modus bedienbar (bleiben für die Animation im DOM).
  useEffect(() => {
    djOnly.current.forEach((el) => (el.inert = !dj))
    p.host.toggleAttribute('data-dj', dj)
    p.host.dispatchEvent(new CustomEvent('modechange', { detail: { mode: dj ? 'dj' : 'single' }, bubbles: true }))
  }, [dj])

  useEffect(() => {
    setDj(p.decks === 2 && p.mode === 'dj')
  }, [p.mode, p.decks])

  const toggleDj = () => {
    if (dj) engine.reset()
    else setSession((n) => n + 1)
    setDj(!dj)
  }

  // Hintergrund-Balken: an, solange ein Deck spielt; Farbe folgt dem hörbaren Pegel je Deck
  // (Fader + Crossfader): nur rechts hörbar = Farbe von Deck B, beide gleich laut = Mischfarbe.
  const onStatus = (side: Side, st: DeckStatus) => {
    status.current[side] = st
    emitDeck(side, st)
    const any = status.current.a.playing || status.current.b.playing
    if (!bars.current && any && engine.output && bgEl.current) bars.current = createBars(engine.output, bgEl.current)
    bars.current?.setActive(any)
  }
  useEffect(() => {
    const w = { a: 0, b: 0 }
    const id = window.setInterval(() => {
      const st = status.current
      if (!bars.current || !(st.a.playing || st.b.playing)) return
      const parts: [Rec, number][] = []
      ;(['a', 'b'] as const).forEach((side, i) => {
        const lvl = st[side].playing ? engine.channels[i].level() : 0
        w[side] = w[side] * 0.75 + lvl * 0.25
        if (st[side].rec) parts.push([st[side].rec!, w[side]])
      })
      bars.current.setMix(parts)
    }, 80)
    return () => window.clearInterval(id)
  }, [engine])

  // "deckchange" nur bei echter Änderung; record.index zählt in el.records, eigene Songs folgen dahinter.
  const emitDeck = (side: Side, st: DeckStatus) => {
    const index = st.rec ? records.indexOf(st.rec) : -1
    const key = `${st.playing}|${st.loading}|${st.failed}|${st.rec?.id}|${st.track}|${index}`
    if (sent.current[side] === key) return
    sent.current[side] = key
    const r = st.rec
    const tr = r?.tracks[st.track]
    p.host.dispatchEvent(
      new CustomEvent('deckchange', {
        bubbles: true,
        detail: {
          deck: side,
          playing: st.playing,
          loading: st.loading,
          failed: st.failed,
          record: r && { index, uploaded: index >= p.records.length, title: r.title, artist: r.artist, cover: r.cover, link: r.link, accent: r.accent },
          track: tr && { index: st.track, title: tr.title, src: tr.src, duration: tr.duration },
        },
      }),
    )
  }

  const onFiles = async (files: File[]) => {
    const { records: next, touched } = await addFiles(files, uploaded, t.myMusic)
    setUploaded(next)
    return touched[0]
  }

  const deck = (side: Side, children?: ComponentChildren) => (
    <Turntable
      side={side}
      engine={engine}
      channel={engine.channels[side === 'a' ? 0 : 1]}
      dj={dj}
      records={records}
      logo={p.logo}
      uploads={p.uploads}
      t={t}
      onStatus={onStatus}
      controls={p.controls}
      onFiles={onFiles}
    >
      {children}
    </Turntable>
  )

  return (
    <div class={`at-root${dj ? ' is-dj' : ''}${hasHero ? ' has-hero' : ''}`}>
      <div ref={bgEl} class="at-bg-viz" aria-hidden="true" />
      <div class="at-inner">
        <div class="at-hero">
          <slot
            ref={(el: HTMLSlotElement | null) => {
              if (!el || (el as any)._watched) return
              ;(el as any)._watched = true
              const check = () =>
                setHasHero(el.assignedNodes().some((n) => n.nodeType === 1 || n.textContent?.trim()))
              el.addEventListener('slotchange', check)
              check()
            }}
          />
        </div>
        {deck(
          'a',
          p.decks === 2 && (
            <button class="at-dj-toggle" onClick={toggleDj} hidden={dj}>
              {t.open}
            </button>
          ),
        )}
        {p.decks === 2 && (
          <>
            <div ref={(el) => void (el && (djOnly.current[0] = el))} class="at-dj-wrap">
              <Mixer key={session} engine={engine} active={dj} t={t} onClose={toggleDj} />
            </div>
            <div ref={(el) => void (el && (djOnly.current[1] = el))} class="at-dj-wrap">
              {deck('b')}
            </div>
          </>
        )}
      </div>
    </div>
  )
}
