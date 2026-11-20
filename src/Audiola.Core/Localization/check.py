# Sammelt alle Übersetzungsschlüssel (L.T/L.F in C#, {l:T '…'} und l:Fmt in XAML)
# und meldet fehlende Einträge in en/fr.json. Exit 1, wenn etwas fehlt.
# Aufruf aus src/: python Audiola.Core/Localization/check.py . [--dump keys.json]
import re, sys, os, json, html, glob

src = sys.argv[1]
# Werte, die als Schlüssel dienen und erst an der Anzeige übersetzt werden (Fmt-Converter ohne Parameter).
keys = {'Neutral (aus)', 'Wie Original', 'Podcast / Sprache', 'Streaming −14 LUFS'}

def cs_unescape(s):
    return re.sub(r'\\(u[0-9a-fA-F]{4}|.)', lambda m: chr(int(m.group(1)[1:], 16)) if m.group(1)[0] == 'u' and len(m.group(1)) == 5 else {'n': '\n', 't': '\t', 'r': '\r', '0': '\0'}.get(m.group(1), m.group(1)), s)

for p in glob.glob(os.path.join(src, '**', '*.cs'), recursive=True):
    if os.sep + 'obj' + os.sep in p or os.sep + 'bin' + os.sep in p: continue
    s = open(p, encoding='utf-8-sig').read()
    LIT = r'"(?:[^"\\]|\\.)*"'
    for m in re.finditer(r'\bL\.[TF]\(\s*(' + LIT + r'(?:\s*\+\s*' + LIT + r')*)\s*(\+?)', s):
        if m.group(2): print('NON-CONST', p, m.group(1)[:60])
        keys.add(''.join(cs_unescape(x[1:-1]) for x in re.findall(LIT, m.group(1))))
    for m in re.finditer(r'\bL\.[TF]\(\s*@"((?:[^"]|"")*)"', s):
        keys.add(m.group(1).replace('""', '"'))

for p in glob.glob(os.path.join(src, '**', '*.axaml'), recursive=True):
    if os.sep + 'obj' + os.sep in p or os.sep + 'bin' + os.sep in p: continue
    s = open(p, encoding='utf-8-sig').read()
    for m in re.finditer(r"\{l:T '([^']*)'\}", s):
        # Im Markup-Argument ist "\" ein Escape-Zeichen: "\\" ergibt einen Backslash.
        keys.add(re.sub(r'\\(.)', r'\1', html.unescape(m.group(1))))
    for m in re.finditer(r"l:Fmt\.Instance\}, ConverterParameter='([^']*)'", s):
        v = html.unescape(m.group(1))
        keys.add(v[2:] if v.startswith('{}') else v)

loc = os.path.join(src, 'Audiola.Core', 'Localization')
missing = {}
for lang in ('en', 'fr'):
    d = json.load(open(os.path.join(loc, lang + '.json'), encoding='utf-8'))
    miss = sorted(k for k in keys if k not in d)
    unused = [k for k in d if k not in keys]
    missing[lang] = miss
    print(f'{lang}: {len(keys)} keys, {len(miss)} missing, {len(unused)} unused')
    for k in miss: print('  -', k)
if '--dump' in sys.argv:
    out = sys.argv[sys.argv.index('--dump') + 1]
    json.dump(sorted(keys), open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
sys.exit(1 if any(missing.values()) else 0)
