"""Maakt een overzichtspagina van alle teksten van Water.sim, om na te lopen.

Gebruik:  python3 Tools/Teksten/overzicht.py
Uitvoer:  Docs/Teksten_overzicht.html

Leest rechtstreeks uit de code (niets met de hand overtypen):
- Assets/Scripts/UI/Teksten.cs      alle teksten (NL en EN), gegroepeerd per onderwerp
- Assets/Scripts/UI/Quizvragen.cs   de quizvragen per level en niveau, met het goede antwoord
- Assets/Scripts/Sim/Klimaat.cs     de klimaatwaarden; regels met * moeten nog worden nagelopen

De pagina zet automatisch een vlag bij: NL en EN met verschillende {0}-plekken, verschillende opmaak (<b> e.d.),
een lege tekst, een punt als decimaalteken in het NL of een komma in het EN, en NL = EN bij lange teksten.
Vinkjes "nagelopen" worden alleen in je eigen browser bewaard.
"""
import html
import os
import re
from datetime import date

HIER = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HIER, "..", ".."))
TEKSTEN = os.path.join(PROJECT, "Assets", "Scripts", "UI", "Teksten.cs")
QUIZ = os.path.join(PROJECT, "Assets", "Scripts", "UI", "Quizvragen.cs")
KLIMAAT = os.path.join(PROJECT, "Assets", "Scripts", "Sim", "Klimaat.cs")
UIT = os.path.join(PROJECT, "Docs", "Teksten_overzicht.html")

GROEPEN = {
    "start": "Algemeen", "binnenkort": "Algemeen", "taal": "Algemeen", "nog": "Algemeen",
    "league": "Niveaus", "level": "Levels (tegels en titels)", "menu": "Menu",
    "info": "Infovak (uitleg per level en niveau, en Model)", "model": "Infovak (uitleg per level en niveau, en Model)",
    "quiz": "Quiz", "mol": "Quiz: namen van moleculen",
    "klimaat": "Level 5: plaatsen", "plaats": "Level 5: plaatsen", "maand": "Level 5: maanden", "streng": "Level 5: meer",
    "meer": "Level 5: meer", "zon": "Level 5: meer", "schaats": "Level 5: meer", "bodem": "Level 5: meer", "dag": "Level 5: meer",
    "lucht": "Level 5: meer", "ijsmassa": "Level 5: meer", "regel": "Level 5: meer",
    "pool": "Level 5: poolkappen", "glas": "Level 5: drijven", "vloeistof": "Level 5: drijven", "gesmolten": "Level 5: drijven",
    "rho": "Level 5: drijven", "inzoom": "Level 5: drijven",
    "ontdek": "Level 6: Ontdek", "fase": "Level 4: smelten", "plateau": "Level 4: smelten", "evenwicht": "Level 4: smelten",
    "per": "Level 4: smelten", "formule": "Formulekaart", "dichtheid": "Level 4: smelten", "ring": "Level 4: smelten",
    "hbruggen": "Knoppen in de levels", "omgeving": "Knoppen in de levels", "oneindig": "Knoppen in de levels",
    "blok": "Knoppen in de levels", "kalotten": "Knoppen in de levels", "beweging": "Knoppen in de levels",
    "ladingen": "Knoppen in de levels", "kanalen": "Knoppen in de levels", "kanaal": "Knoppen in de levels",
    "tab": "Level 5: tabs", "poolkappen": "Level 5: tabs", "jaar": "Level 5: meer", "opnieuw": "Algemeen",
}


# ---------- een klein C#-leesprogramma: strings, getallen, namen en leestekens ----------
def tokens(bron):
    i, n = 0, len(bron)
    while i < n:
        c = bron[i]
        if c.isspace():
            i += 1
        elif bron.startswith("//", i):
            j = bron.find("\n", i); i = n if j < 0 else j
        elif bron.startswith("/*", i):
            i = bron.find("*/", i) + 2
        elif c == '"':
            j, s = i + 1, []
            while bron[j] != '"':
                if bron[j] == "\\":
                    e = bron[j + 1]
                    s.append({"n": "\n", "t": "\t", '"': '"', "\\": "\\"}.get(e, e)); j += 2
                else:
                    s.append(bron[j]); j += 1
            yield ("str", "".join(s)); i = j + 1
        elif c.isdigit() or (c in "-." and i + 1 < n and bron[i + 1].isdigit()):
            j = i + 1
            while j < n and (bron[j].isdigit() or bron[j] in ".eE"): j += 1
            yield ("num", bron[i:j]); i = j
        elif c.isalpha() or c == "_":
            j = i
            while j < n and (bron[j].isalnum() or bron[j] == "_"): j += 1
            yield ("naam", bron[i:j]); i = j
        else:
            yield ("teken", c); i += 1


def lees_teksten():
    """[(sleutel, nl, en)] in de volgorde van het bestand."""
    t = list(tokens(open(TEKSTEN, encoding="utf-8").read()))
    uit = []
    for k in range(len(t) - 8):
        if t[k] == ("teken", "[") and t[k + 1][0] == "str" and t[k + 2] == ("teken", "]") and t[k + 3] == ("teken", "=") and t[k + 4] == ("teken", "("):
            delen, j = [[], []], k + 5
            nr = 0
            while t[j] != ("teken", ")"):
                if t[j] == ("teken", ","): nr += 1
                elif t[j][0] == "str": delen[nr].append(t[j][1])
                j += 1
            uit.append((t[k + 1][1], "".join(delen[0]), "".join(delen[1])))
    return uit


def argumenten(t, k):
    """Leest de argumenten van een aanroep die bij t[k] == '(' begint. Geeft (lijst, index na ')')."""
    args, huidig, diepte, j = [], [], 0, k + 1
    while True:
        tok = t[j]
        if tok == ("teken", "(") :
            diepte += 1; huidig.append(tok)
        elif tok == ("teken", ")"):
            if diepte == 0:
                if huidig: args.append(huidig)
                return args, j + 1
            diepte -= 1; huidig.append(tok)
        elif tok == ("teken", ",") and diepte == 0:
            args.append(huidig); huidig = []
        else:
            huidig.append(tok)
        j += 1


def lees_quiz():
    """{lijstnaam: [vraag]}; een vraag is een dict met nl, en, opties of antwoord."""
    t = list(tokens(open(QUIZ, encoding="utf-8").read()))
    lijsten, huidig = {}, None
    k = 0
    while k < len(t):
        if t[k] == ("naam", "readonly") and t[k + 1] == ("naam", "Quizvraag") and t[k + 2] == ("teken", "["):
            huidig = t[k + 4][1]; lijsten[huidig] = []
        if huidig and t[k][0] == "naam" and t[k][1] in ("MK", "GT") and t[k + 1] == ("teken", "("):
            args, k2 = argumenten(t, k + 1)
            v = {"nl": args[0][0][1], "en": args[1][0][1]}
            if t[k][1] == "MK":
                v["opties"] = [tuple(x[1] for x in a if x[0] == "str") for a in args[2:]]
            else:
                getallen = [a for a in args[2:]]
                def waarde(a): return "".join(x[1] for x in a)
                v["antwoord"] = waarde(getallen[0]); v["marge"] = waarde(getallen[1])
                namen = ["eenheid", "macht", "mindec", "weergave"]
                for naam, a in zip(namen, getallen[2:]):
                    v[naam] = a[0][1] if a and a[0][0] == "str" else waarde(a)
            lijsten[huidig].append(v); k = k2; continue
        k += 1
    return lijsten


def getal(s):
    """Getal uit de code in Nederlandse schrijfwijze: komma en echt minteken."""
    return s.replace(".", ",").replace("-", "−")


def lees_klimaat():
    rijen = []
    for regel in open(KLIMAAT, encoding="utf-8"):
        m = re.match(r'\s*new Klimaat\((.*)\),\s*//\s*(.*)$', regel)
        if not m: continue
        delen = [d.strip().strip('"') for d in m.group(1).split(",")]
        rijen.append({"sleutel": delen[0], "land": delen[1], "breedte": delen[2], "koudst": delen[3], "warmst": delen[4],
                      "dag": delen[5], "streng": delen[6] if len(delen) > 6 else "", "winter": delen[7] if len(delen) > 7 else "",
                      "noot": m.group(2), "ster": "*" in m.group(2)})
    return rijen


# ---------- controles ----------
def plekken(s): return sorted(re.findall(r"\{\d+\}", s))
def opmaak(s): return sorted(re.findall(r"</?(?:b|i|sub|sup)>", s))


def vlaggen(nl, en):
    v = []
    if plekken(nl) != plekken(en): v.append("{n}-plekken verschillen")
    if opmaak(nl) != opmaak(en): v.append("opmaak verschilt")
    if not nl.strip() or not en.strip(): v.append("lege tekst")
    if re.search(r"\d\.\d", re.sub(r"<[^>]+>", "", nl)): v.append("punt als decimaalteken in NL?")
    if re.search(r"\d,\d(?!\d\d)", re.sub(r"<[^>]+>", "", en)): v.append("komma als decimaalteken in EN?")
    if nl == en and len(nl) > 25: v.append("NL = EN")
    return v


# ---------- HTML ----------
def opgemaakt(s):
    """TextMeshPro-opmaak naar HTML: <b>, <i>, <sub>, <sup> blijven; regels breken; {0} gemarkeerd."""
    s = html.escape(s, quote=False)
    for tag in ("b", "i", "sub", "sup"):
        s = s.replace(f"&lt;{tag}&gt;", f"<{tag}>").replace(f"&lt;/{tag}&gt;", f"</{tag}>")
    s = re.sub(r"&lt;/?(?:size|color|alpha)[^&]*&gt;", "", s)
    s = re.sub(r"\{(\d+)\}", r'<span class="ph">{\1}</span>', s)
    return s.replace("\n", "<br>")


def vlagjes(nl, en):
    return "".join('<span class="vlag">' + html.escape(x) + '</span>' for x in vlaggen(nl, en))


def rij_tekst(sleutel, nl, en):
    v = vlaggen(nl, en)
    vl = "".join(f'<span class="vlag">{html.escape(x)}</span>' for x in v)
    return (f'<tr data-k="t:{html.escape(sleutel)}" class="{"gevlagd" if v else ""}">'
            f'<td class="chk"><input type="checkbox"></td><td class="key"><code>{html.escape(sleutel)}</code>{vl}</td>'
            f'<td lang="nl">{opgemaakt(nl)}</td><td lang="en">{opgemaakt(en)}</td></tr>')


NIVEAUS = {"O": "onderbouw", "B": "bovenbouw", "X": "XPRT"}
LEVELS = {"1": "Level 1 · het watermolecuul", "2": "Level 2 · de waterstofbrug"}


def antwoord_tekst(v, taal):
    if "opties" in v:
        goed = v["opties"][0][0 if taal == "nl" else 1]
        rest = " · ".join(o[0 if taal == "nl" else 1] for o in v["opties"][1:])
        return f'<span class="goed">✓ {opgemaakt(goed)}</span><br><span class="rest">{opgemaakt(rest)}</span>'
    w = v.get("weergave", "") or v["antwoord"]
    if taal == "en": w = w.replace(",", ".")
    else: w = w.replace(".", ",") if not v.get("weergave") else w
    extra = []
    if v.get("eenheid") and "10" not in v.get("weergave", ""): extra.append(v["eenheid"])      # zoals in de app: geen dubbele × 10²³
    marge = v["marge"].replace(".", ",") if taal == "nl" else v["marge"]
    m = f' <span class="rest">(marge ±{html.escape(marge)}'
    if v.get("macht") and v["macht"] != "0": m += f", ×10^{html.escape(v['macht'])}"
    if v.get("mindec") and v["mindec"] != "0": m += f", min. {html.escape(v['mindec'])} decimaal"
    m += ")</span>"
    return f'<span class="goed">✓ {opgemaakt(w)} {opgemaakt(" ".join(extra))}</span>{m}'


def maak():
    teksten = lees_teksten()
    quiz = lees_quiz()
    klimaat = lees_klimaat()

    groepen, volgorde = {}, []
    for (k, nl, en) in teksten:
        g = GROEPEN.get(k.split(".")[0], "Overig")
        if g not in groepen: groepen[g] = []; volgorde.append(g)
        groepen[g].append((k, nl, en))
    n_vlag = sum(1 for (k, nl, en) in teksten if vlaggen(nl, en)) + sum(1 for l in quiz.values() for v in l if vlaggen(v["nl"], v["en"]))
    n_quiz = sum(len(v) for v in quiz.values())
    n_ster = sum(1 for r in klimaat if r["ster"])

    delen = []
    inhoud = ['<a href="#klimaat">Klimaatwaarden</a>', '<a href="#quiz">Quizvragen</a>'] + \
             [f'<a href="#g{i}">{html.escape(g)}</a>' for i, g in enumerate(volgorde)]

    # klimaat
    k_rijen = "".join(
        f'<tr data-k="k:{r["sleutel"]}" class="{"ster" if r["ster"] else ""}"><td class="chk"><input type="checkbox"></td>'
        f'<td><b>{r["land"]}</b></td><td>{html.escape(r["noot"])}</td><td class="num">{getal(r["breedte"])}°</td>'
        f'<td class="num">{getal(r["koudst"])}</td><td class="num">{getal(r["warmst"])}</td>'
        f'<td class="num">{r["dag"]}</td><td class="num">{getal(r["streng"])}</td></tr>' for r in klimaat)
    delen.append(f'''<section id="klimaat"><h2>Klimaatwaarden <span class="tel">{n_ster} met * (goedgekeurd)</span></h2>
<p class="uitleg">Uit <code>Klimaat.cs</code>. Normalen 1991–2020, gemiddelde maandtemperatuur. Rijen met * zijn benaderingen uit
secundaire bronnen; nagelopen en goedgekeurd op 11 november 2026.</p>
<div class="tabel"><table><thead><tr><th></th><th>Land</th><th>Plaats en opmerking</th><th>Breedte</th><th>Koudste maand °C</th>
<th>Warmste maand °C</th><th>Dag koudst</th><th>Strenge winter −°C</th></tr></thead><tbody>{k_rijen}</tbody></table></div></section>''')

    # quiz
    q = []
    for naam, vragen in quiz.items():
        lv, nv = naam[1], naam[2]
        rijen = "".join(
            f'<tr data-k="q:{naam}:{i}" class="{"gevlagd" if vlaggen(v["nl"], v["en"]) else ""}"><td class="chk"><input type="checkbox"></td>'
            f'<td class="key">{naam}·{i + 1}{vlagjes(v["nl"], v["en"])}</td>'
            f'<td lang="nl">{opgemaakt(v["nl"])}<div class="antw">{antwoord_tekst(v, "nl")}</div></td>'
            f'<td lang="en">{opgemaakt(v["en"])}<div class="antw">{antwoord_tekst(v, "en")}</div></td></tr>' for i, v in enumerate(vragen))
        q.append(f'<h3>{LEVELS.get(lv, lv)} · {NIVEAUS.get(nv, nv)} <span class="tel">{len(vragen)} vragen</span></h3>'
                 f'<div class="tabel"><table><thead><tr><th></th><th>Nr</th><th>Nederlands</th><th>English</th></tr></thead><tbody>{rijen}</tbody></table></div>')
    delen.append(f'''<section id="quiz"><h2>Quizvragen <span class="tel">{n_quiz}</span></h2>
<p class="uitleg">Uit <code>Quizvragen.cs</code>. Bij meerkeuze staat het goede antwoord met ✓, de andere keuzes eronder
(in de app in willekeurige volgorde). Bij getallen: het goede antwoord, de marge en eventueel de macht van 10.</p>{"".join(q)}</section>''')

    # teksten
    for i, g in enumerate(volgorde):
        rijen = "".join(rij_tekst(*x) for x in groepen[g])
        delen.append(f'<section id="g{i}"><h2>{html.escape(g)} <span class="tel">{len(groepen[g])}</span></h2>'
                     f'<div class="tabel"><table><thead><tr><th></th><th>Sleutel</th><th>Nederlands</th><th>English</th></tr></thead>'
                     f'<tbody>{rijen}</tbody></table></div></section>')

    pagina = f'''<!doctype html>
<html lang="nl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Teksten Water.sim</title>
<style>
:root {{ --bg:#f6f8fa; --kaart:#fff; --tekst:#14202b; --zwak:#5b6b78; --lijn:#dde4ea; --accent:#e2683c; --blauw:#1f7fbf;
        --vlag:#fff1d6; --vlagtekst:#8a5300; --ster:#fff6e0; --goed:#1d7a3e; --klaar:#eef7f0; }}
@media (prefers-color-scheme: dark) {{ :root:not([data-theme="light"]) {{ --bg:#0b141c; --kaart:#122330; --tekst:#e3eef5; --zwak:#93a8b8;
        --lijn:#21384a; --vlag:#3a2c10; --vlagtekst:#ffc56b; --ster:#2c2510; --goed:#7fdc9c; --klaar:#12301e; }} }}
:root[data-theme="dark"] {{ --bg:#0b141c; --kaart:#122330; --tekst:#e3eef5; --zwak:#93a8b8; --lijn:#21384a; --vlag:#3a2c10;
        --vlagtekst:#ffc56b; --ster:#2c2510; --goed:#7fdc9c; --klaar:#12301e; }}
* {{ box-sizing:border-box; }}
body {{ margin:0; background:var(--bg); color:var(--tekst); font:15px/1.5 -apple-system,system-ui,"Segoe UI",sans-serif; }}
header {{ position:sticky; top:0; z-index:5; background:var(--kaart); border-bottom:1px solid var(--lijn); padding:12px 16px; }}
header h1 {{ margin:0 0 6px; font-size:19px; }}
.balk {{ display:flex; flex-wrap:wrap; gap:8px; align-items:center; }}
.balk input[type=search] {{ flex:1 1 220px; padding:7px 10px; border:1px solid var(--lijn); border-radius:8px; background:var(--bg); color:var(--tekst); font-size:15px; }}
.balk label {{ font-size:13px; color:var(--zwak); white-space:nowrap; }}
.voortgang {{ font-size:13px; color:var(--zwak); }}
nav {{ padding:10px 16px 0; display:flex; flex-wrap:wrap; gap:6px; }}
nav a {{ font-size:13px; padding:3px 9px; border:1px solid var(--lijn); border-radius:20px; color:var(--blauw); text-decoration:none; background:var(--kaart); }}
main {{ padding:0 16px 40px; max-width:1200px; margin:0 auto; }}
section, h3 {{ scroll-margin-top:110px; }}
h2 {{ margin:28px 0 6px; font-size:18px; }} h3 {{ margin:18px 0 6px; font-size:15px; }}
.tel {{ font-weight:normal; font-size:13px; color:var(--zwak); margin-left:6px; }}
.uitleg {{ color:var(--zwak); font-size:14px; margin:0 0 8px; max-width:800px; }}
.tabel {{ overflow-x:auto; background:var(--kaart); border:1px solid var(--lijn); border-radius:10px; }}
table {{ width:100%; border-collapse:collapse; }}
th {{ text-align:left; font-size:12px; color:var(--zwak); font-weight:600; padding:7px 10px; border-bottom:1px solid var(--lijn); }}
td {{ padding:8px 10px; border-bottom:1px solid var(--lijn); vertical-align:top; }}
tr:last-child td {{ border-bottom:none; }}
td[lang] {{ width:42%; min-width:220px; }}
td.key {{ font-size:12px; color:var(--zwak); width:150px; word-break:break-all; }}
td.chk {{ width:28px; }} td.num {{ text-align:right; font-variant-numeric:tabular-nums; white-space:nowrap; }}
code {{ font-size:12px; }}
.ph {{ background:rgba(31,127,191,.15); color:var(--blauw); border-radius:4px; padding:0 3px; font-family:ui-monospace,monospace; font-size:13px; }}
.vlag {{ display:inline-block; margin:4px 4px 0 0; font-size:11px; background:var(--vlag); color:var(--vlagtekst); border-radius:4px; padding:1px 6px; word-break:normal; }}
tr.ster td {{ background:var(--ster); }}
tr.klaar td {{ background:var(--klaar); opacity:.75; }}
.antw {{ margin-top:6px; font-size:13px; }} .goed {{ color:var(--goed); font-weight:600; }} .rest {{ color:var(--zwak); }}
.verborgen {{ display:none; }}
@media (max-width:640px) {{ td.key {{ width:auto; }} td[lang] {{ min-width:180px; }} }}
</style></head><body>
<header><h1>Teksten Water.sim</h1>
<div class="balk"><input type="search" id="zoek" placeholder="Zoek in sleutel, NL of EN">
<label><input type="checkbox" id="alleenVlag"> alleen met vlag ({n_vlag})</label>
<label><input type="checkbox" id="verbergKlaar"> nagelopen verbergen</label>
<span class="voortgang" id="voortgang"></span></div></header>
<nav>{"".join(inhoud)}</nav>
<main>
<p class="uitleg" style="margin-top:14px">{len(teksten)} teksten, {n_quiz} quizvragen en {len(klimaat)} plaatsen, gelezen uit de code op {date.today().strftime("%d-%m-%Y")}.
Een <span class="vlag">vlag</span> is een automatische controle (bijvoorbeeld verschillende {{0}}-plekken in NL en EN), geen zekere fout.
<span class="ph">{{0}}</span> wordt in de app ingevuld met een getal of naam. Vink af wat je hebt nagelopen; dat bewaart alleen deze browser.
Opnieuw maken: <code>python3 Tools/Teksten/overzicht.py</code>.</p>
{"".join(delen)}
</main>
<script>
const SLEUTEL = "watersim.teksten.nagelopen";
let klaar = {{}};
try {{ klaar = JSON.parse(localStorage.getItem(SLEUTEL) || "{{}}"); }} catch (e) {{}}
const rijen = [...document.querySelectorAll("tr[data-k]")];
function bewaar() {{ try {{ localStorage.setItem(SLEUTEL, JSON.stringify(klaar)); }} catch (e) {{}} }}
function filter() {{
  const z = document.getElementById("zoek").value.toLowerCase();
  const vlag = document.getElementById("alleenVlag").checked, verberg = document.getElementById("verbergKlaar").checked;
  for (const r of rijen) {{
    const k = r.dataset.k, nk = !!klaar[k];
    r.classList.toggle("klaar", nk);
    const zicht = (!z || r.textContent.toLowerCase().includes(z)) && (!vlag || r.classList.contains("gevlagd")) && !(verberg && nk);
    r.classList.toggle("verborgen", !zicht);
  }}
  const n = rijen.filter(r => klaar[r.dataset.k]).length;
  document.getElementById("voortgang").textContent = n + " van " + rijen.length + " nagelopen";
}}
for (const r of rijen) {{
  const c = r.querySelector("input");
  c.checked = !!klaar[r.dataset.k];
  c.addEventListener("change", () => {{ if (c.checked) klaar[r.dataset.k] = 1; else delete klaar[r.dataset.k]; bewaar(); filter(); }});
}}
for (const id of ["zoek", "alleenVlag", "verbergKlaar"]) document.getElementById(id).addEventListener("input", filter);
filter();
</script></body></html>'''
    open(UIT, "w", encoding="utf-8").write(pagina)
    print(f"{len(teksten)} teksten ({n_vlag} met vlag), {n_quiz} quizvragen, {len(klimaat)} plaatsen ({n_ster} met *) → {UIT}")


if __name__ == "__main__":
    maak()
