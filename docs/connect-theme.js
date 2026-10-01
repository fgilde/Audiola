// Gleicht die Connect-Widgets von gilde.org an das Audiola-Design an.
// Die Widgets rendern in ein offenes Shadow-DOM und setzen ihre Farben dort auf .surface —
// Seiten-CSS kommt da nicht hin. Darum hängen wir jedem Widget ein eigenes Stylesheet an.
// Die Akzentfarbe (accent-Attribut) bleibt die des Widgets; das helle Theme bleibt unberührt.
const css = `
  .surface:not([data-theme=light]):not([data-theme=auto]) {
    --bg: #121318;
    --raised: #20232d;
    --text: #e9ebf2;
    --muted: #a7adc0;
    --line: #2c303b;
    --input: #0c0d12;
  }
  .surface { font-family: "Inter", system-ui, sans-serif; }
  .card { box-shadow: none; }
  h2, h3 { font-family: "Space Grotesk", "Inter", sans-serif; letter-spacing: -.01em; }
  input, textarea, select { font-family: inherit; border-radius: 10px; }
  input:focus, textarea:focus, select:focus { outline: none; border-color: var(--accent); box-shadow: 0 0 0 3px color-mix(in srgb, var(--accent) 22%, transparent); }
  .primary { border-radius: 999px; letter-spacing: .01em; transition: filter .15s, transform .15s; }
  .primary:hover { filter: brightness(1.08); transform: translateY(-1px); }
`;

const sheet = new CSSStyleSheet();
sheet.replaceSync(css);

function theme(el) {
  const root = el.shadowRoot;
  if (root && !root.adoptedStyleSheets.includes(sheet))
    root.adoptedStyleSheets = [...root.adoptedStyleSheets, sheet];
}

for (const tag of ["gilde-contact", "gilde-support"]) {
  customElements.whenDefined(tag).then(() => document.querySelectorAll(tag).forEach(theme));
}

// connect.html legt die Widgets erst per Skript an.
new MutationObserver(records => {
  for (const r of records)
    for (const n of r.addedNodes)
      if (n.nodeType === 1 && /^GILDE-(CONTACT|SUPPORT)$/.test(n.tagName))
        customElements.whenDefined(n.localName).then(() => theme(n));
}).observe(document.documentElement, { childList: true, subtree: true });
