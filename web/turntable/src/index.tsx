import { render } from 'preact'
import App from './App'
import { palette } from './palette'
import { fillColors, normalize, type Rec } from './records'
import css from './styles.css?inline'

// <audiola-turntable> – freie Web-Component: Plattenspieler & DJ-Mischpult im Browser.
// https://audiola.de/turntable.html
//
//   <script type="module" src="https://audiola.de/widgets/turntable/v1.js"></script>
//   <audiola-turntable records="records.json" decks="2" uploads lang="de">
//     …optionaler Hero-Inhalt: steht links, fährt beim Öffnen des Mischpults raus…
//   </audiola-turntable>
//
// Attribute: records (URL zu JSON), decks ("1" | "2", Standard 2), mode ("single" | "dj"),
//            uploads (eigene Songs erlauben), logo (Bild auf der leeren Platte), lang ("de" | "en").
// Property:  el.records = [...]  (gleiches Format wie die JSON-Datei, siehe records.ts)
// Event:     "modechange" { detail: { mode: "single" | "dj" } }
// Styling:   CSS-Variablen --at-accent, --at-text, --at-muted, --at-font, --at-font-display,
//            --at-font-mono, --at-padding, --at-min-height

class AudiolaTurntable extends HTMLElement {
  static observedAttributes = ['records', 'decks', 'mode', 'uploads', 'logo', 'lang']
  private mount?: HTMLDivElement
  private list: Rec[] = []
  private loadId = 0

  get records(): Rec[] {
    return this.list
  }
  set records(v: unknown) {
    this.loadId++ // späteres Laden per Attribut soll die Property nicht überschreiben
    this.list = normalize(v, document.baseURI)
    this.update()
    void this.colorize()
  }

  connectedCallback() {
    if (!this.shadowRoot) {
      const root = this.attachShadow({ mode: 'open' })
      const style = document.createElement('style')
      style.textContent = css
      this.mount = document.createElement('div')
      root.append(style, this.mount)
    }
    this.update()
  }

  disconnectedCallback() {
    if (this.mount) render(null, this.mount)
  }

  attributeChangedCallback(name: string, old: string | null, value: string | null) {
    if (name === 'records' && value && value !== old) void this.fetchRecords(value)
    else this.update()
  }

  private async fetchRecords(src: string) {
    const id = ++this.loadId
    try {
      const url = new URL(src, document.baseURI).href
      const data = await (await fetch(url)).json()
      if (id !== this.loadId) return
      this.list = normalize(data, url)
      this.update()
      void this.colorize()
    } catch (e) {
      console.error('[audiola-turntable] records konnten nicht geladen werden:', e)
    }
  }

  /** Fehlende Plattenfarben aus den Covern berechnen und neu zeichnen. */
  private async colorize() {
    const list = this.list
    if ((await fillColors(list, palette)) && list === this.list) {
      this.list = [...list]
      this.update()
    }
  }

  private update() {
    if (!this.mount || !this.isConnected) return
    const logo = this.getAttribute('logo')
    render(
      <App
        host={this}
        records={this.list}
        decks={this.getAttribute('decks') === '1' ? 1 : 2}
        mode={this.getAttribute('mode') === 'dj' ? 'dj' : 'single'}
        uploads={this.hasAttribute('uploads')}
        logo={logo ? new URL(logo, document.baseURI).href : null}
        lang={this.getAttribute('lang')}
      />,
      this.mount,
    )
  }
}

if (!customElements.get('audiola-turntable')) customElements.define('audiola-turntable', AudiolaTurntable)

declare global {
  interface HTMLElementTagNameMap {
    'audiola-turntable': AudiolaTurntable
  }
}
