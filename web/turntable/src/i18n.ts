const de = {
  pick: 'Platte wählen',
  hint: 'Arm auflegen · Platte drehen zum Scratchen',
  deck: 'Deck',
  loading: 'lädt …',
  error: 'Laden fehlgeschlagen',
  prev: 'Vorheriger Track',
  next: 'Nächster Track',
  play: 'Abspielen',
  pause: 'Pause',
  open: '⇄ Mischpult öffnen',
  close: '✕ Mischpult schließen',
  pitch: '− Pitch ±8 % +',
  cueSet: 'Cue setzen',
  cueJump: 'Springen · Doppelklick löscht',
  spin: 'Spin',
  upload: 'Eigene Songs öffnen',
  drop: 'Songs hier ablegen',
  myMusic: 'Meine Musik',
  volume: 'Lautstärke',
  crossfader: 'A ◂ Crossfader ▸ B',
  poweredBy: 'powered by',
}

const en: typeof de = {
  pick: 'Pick a record',
  hint: 'Drop the arm · spin the record to scratch',
  deck: 'Deck',
  loading: 'loading …',
  error: 'Could not load',
  prev: 'Previous track',
  next: 'Next track',
  play: 'Play',
  pause: 'Pause',
  open: '⇄ Open mixer',
  close: '✕ Close mixer',
  pitch: '− Pitch ±8 % +',
  cueSet: 'Set cue',
  cueJump: 'Jump · double-click clears',
  spin: 'Spin',
  upload: 'Open your own songs',
  drop: 'Drop songs here',
  myMusic: 'My music',
  volume: 'Volume',
  crossfader: 'A ◂ Crossfader ▸ B',
  poweredBy: 'powered by',
}

export type Strings = typeof de

export const strings = (lang: string | null): Strings =>
  (lang ?? navigator.language).toLowerCase().startsWith('de') ? de : en
