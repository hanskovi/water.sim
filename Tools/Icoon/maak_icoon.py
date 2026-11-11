"""Tekent het app-icoon van Water.sim (ontwerp: Docs/Icoon/watersim_icoon.svg, gekozen 8 nov 2026;
oranje V onder de 2 toegevoegd 11 nov 2026, zie Docs/Mockups/icoon_hex_keuze.png).

Gebruik:  python3 Tools/Icoon/maak_icoon.py
Uitvoer:
  Assets/Icoon/icoon_1024.png          vierkant, zonder doorzichtigheid (iOS, App Store, standaard)
  Assets/Icoon/android_achtergrond.png Android adaptief icoon: achtergrondlaag (1024)
  Assets/Icoon/android_voorgrond.png   Android adaptief icoon: H2O, kleiner, binnen de veilige zone (1024)
  Docs/Icoon/icoon_store_512.png       Play Store (512, vierkant)
  Docs/Icoon/voorbeeld.png             voorbeeld met afgeronde hoeken en kleine maten

Alles wordt op 4× resolutie getekend en daarna verkleind (zachte randen).
Ontwerp in eenheden van 0..120 (zoals de mockups).
"""
import math
import os
from PIL import Image, ImageChops, ImageDraw, ImageFont

HIER = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HIER, "..", ".."))
FONT = os.path.join(PROJECT, "Assets", "Fonts", "Ubuntu", "Ubuntu-Bold.ttf")
UIT_ASSETS = os.path.join(PROJECT, "Assets", "Icoon")
UIT_DOCS = os.path.join(PROJECT, "Docs", "Icoon")

MAAT = 1024
OVER = 4                      # overbemonstering
S = MAAT * OVER
K = S / 120.0                 # pixels per ontwerp-eenheid

GY, AMP = 62.0, 5.0           # golf: hoogte en uitslag
BOVEN = ((0x3C, 0xC0, 0xDC), (0x16, 0x96, 0xB8))
ONDER = ((0x0E, 0x7C, 0x9E), (0x06, 0x4A, 0x63))
SCHUIM = (0xBD, 0xEF, 0xFA)
IJS = (0xC4, 0xF4, 0xFB)
HEX_R, HEX_W, HEX_MAX = 11.0, 1.8, 0.85
MARGE = 7.0                   # ruimte links en rechts van H2O (van 120)
TWEE_OPTISCH = 0.5            # plek van de 2: 0 = midden tussen de buitenranden, 1 = optisch midden op de hoogte van de 2
ORANJE = ((0xF7, 0x88, 0x4D), (0xE8, 0x50, 0x2E))   # verloop van de oranje knoppen in de app (links-boven → rechts-onder)
V_DIKTE = HEX_W * 2.4         # oranje V: de twee onderste zijden van de zeshoek onder de 2 (zoals de streep onder het molecuul
                              # bij de andere scheikunde-iconen; gekozen 11 nov 2026)
TWEE_OVERLAP = 6.1            # zoveel valt de bovenpunt van die zeshoek achter de 2 (eenheden, bij volle lettergrootte)


def p(x, y):
    return (x * K, y * K)


def golf_punten(n=240):
    """Twee kwadratische bogen: M-2,GY Q29,GY-8 60,GY Q91,GY+8 122,GY."""
    def q(p0, c, p1, t):
        return ((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * c[0] + t * t * p1[0],
                (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * c[1] + t * t * p1[1])
    a, b, c = (-2, GY), (60, GY), (122, GY)
    pts = [q(a, (29, GY - AMP * 1.6), b, i / n) for i in range(n)]
    pts += [q(b, (91, GY + AMP * 1.6), c, i / n) for i in range(n + 1)]
    return pts


def verloop(kleuren, y0, y1):
    """Verticaal verloop over het hele beeld, van y0 naar y1 (eenheden)."""
    img = Image.new("RGB", (1, S))
    for y in range(S):
        t = min(1.0, max(0.0, (y / K - y0) / (y1 - y0)))
        img.putpixel((0, y), tuple(int(kleuren[0][i] + (kleuren[1][i] - kleuren[0][i]) * t) for i in range(3)))
    return img.resize((S, S))


def watermasker():
    m = Image.new("L", (S, S), 0)
    pts = [p(x, y) for x, y in golf_punten()] + [p(122, 122), p(-2, 122)]
    ImageDraw.Draw(m).polygon(pts, fill=255)
    return m


def zeshoek(d, cx, cy, r, w, kleur):
    pts = [p(cx + r * math.cos(math.pi / 3 * k + math.pi / 6), cy + r * math.sin(math.pi / 3 * k + math.pi / 6)) for k in range(6)]
    d.line(pts + [pts[0], pts[1]], fill=kleur, width=int(w * K), joint="curve")


def ijsrooster(watervorm, schuif=(0.0, 0.0)):
    laag = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(laag)
    dx = HEX_R * math.sqrt(3)
    for j in range(-1, 9):
        for i in range(-2, int(124 / dx) + 2):
            zeshoek(d, i * dx + (dx / 2 if j % 2 else 0) + schuif[0], GY - 2 + j * HEX_R * 1.5 + schuif[1], HEX_R, HEX_W, IJS + (255,))
    # vriesverloop: onzichtbaar bij de golf, onderin HEX_MAX zichtbaar
    alfa = Image.new("L", (1, S))
    for y in range(S):
        t = (y / K - GY) / (118 - GY)
        if t <= 0:
            a = 0.0
        elif t < 0.25:
            a = HEX_MAX * 0.15 * (t / 0.25)
        else:
            a = HEX_MAX * (0.15 + 0.85 * (t - 0.25) / 0.75)
        alfa.putpixel((0, y), int(255 * min(1.0, a)))
    alfa = alfa.resize((S, S))
    # alleen in het water (onder de golf), en met het vriesverloop
    laag.putalpha(ImageChops.multiply(ImageChops.multiply(laag.getchannel("A"), watervorm), alfa))
    return laag


def lijn_pad(d, pts, w, kleur):
    """Dikke lijn met ronde uiteinden: stempelt rondjes langs het pad (gladde randen, geen hoekpieken)."""
    r = w * K / 2
    stap = max(1.0, r / 4)
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        X0, Y0 = p(x0, y0)
        X1, Y1 = p(x1, y1)
        n = max(1, int(math.hypot(X1 - X0, Y1 - Y0) / stap))
        for i in range(n + 1):
            t = i / n
            X, Y = X0 + (X1 - X0) * t, Y0 + (Y1 - Y0) * t
            d.ellipse([X - r, Y - r, X + r, Y + r], fill=kleur)


def stoompluim(x, y, h):
    """M x y q-4.5 -h/4 0 -h/2 q4.5 -h/4 0 -h/2"""
    pts = []
    for seg, (cx, ey) in enumerate([(-4.5, -h * 0.5), (4.5, -h * 0.5)]):
        sx, sy = x, y - seg * h * 0.5
        for i in range(41):
            t = i / 40
            px = (1 - t) ** 2 * sx + 2 * (1 - t) * t * (sx + cx) + t * t * sx
            py = (1 - t) ** 2 * sy + 2 * (1 - t) * t * (sy - h * 0.25) + t * t * (sy + ey)
            pts.append((px, py))
    return pts


def h2o(breedte_eenheden, midden_y=60.0):
    """Wit H2O (Ubuntu Bold) met een verlaagde, kleinere 2, precies breedte_eenheden breed, gecentreerd."""
    groot = 1000
    f = ImageFont.truetype(FONT, groot)
    fk = ImageFont.truetype(FONT, int(groot * 34 / 60))
    zak = groot * 12 / 60
    delen = [("H", f, 0), ("2", fk, zak), ("O", f, 0)]
    # gewone plaatsing (op voorloopbreedte), en de inktranden van elke letter
    x, boxes = 0, []
    for tekst, font, dy in delen:
        l, _, r, _ = font.getbbox(tekst, anchor="ls")
        boxes.append([x, dy, tekst, font, x + l, x + r])
        x += font.getlength(tekst)
    basislijn = groot * 1.2

    def masker(i):
        m = Image.new("L", (int(x) + 200, groot * 2), 0)
        bx, dy, tekst, font = boxes[i][:4]
        ImageDraw.Draw(m).text((bx + 100, basislijn + dy), tekst, font=font, fill=255, anchor="ls")
        return m

    # de 2 optisch in het midden: meet de ruimte op de hoogte van de 2 zelf
    # (de O buigt daar naar rechts weg, dus alleen de buitenranden meten is niet genoeg)
    mH, m2, mO = masker(0), masker(1), masker(2)
    l2, t2, r2, b2 = m2.getbbox()
    h_rechts = mH.crop((0, t2, mH.width, b2)).getbbox()[2]
    # linkerrand van de O, gemiddeld over het middelste deel van de hoogte van de 2
    randen = []
    for y in range(t2 + (b2 - t2) // 4, b2 - (b2 - t2) // 4, 4):
        rij = mO.crop((0, y, mO.width, y + 1)).getbbox()
        if rij: randen.append(rij[0])
    o_optisch = sum(randen) / len(randen)
    o_buiten = mO.getbbox()[0]                       # buitenrand van de O (breedste punt)
    o_links = o_buiten + TWEE_OPTISCH * (o_optisch - o_buiten)
    gat = (o_links - h_rechts - (r2 - l2)) / 2
    boxes[1][0] += (h_rechts + gat) - l2
    tmp = Image.new("L", (int(x) + 200, groot * 2), 0)
    d = ImageDraw.Draw(tmp)
    for (bx, dy, tekst, font, _, _) in boxes:
        d.text((bx + 100, basislijn + dy), tekst, font=font, fill=255, anchor="ls")
    tmp = tmp.crop(tmp.getbbox())
    schaal = breedte_eenheden * K / tmp.width
    tmp = tmp.resize((int(tmp.width * schaal), int(tmp.height * schaal)), Image.LANCZOS)
    laag = Image.new("RGBA", (S, S), (255, 255, 255, 0))
    wit = Image.new("RGBA", tmp.size, (255, 255, 255, 255))
    laag.paste(wit, (int((S - tmp.width) / 2), int(midden_y * K - tmp.height / 2)), tmp)
    return laag


def twee_plek(letters):
    """Midden (x) en onderkant (y) van de 2 in een H2O-laag, in eenheden. De 2 is het laagste stuk inkt;
    vanuit het midden van zijn onderste balk wordt de hele 2 opgevuld."""
    a = letters.getchannel("A")
    l, t, r_, b = a.getbbox()
    binair = a.point(lambda v: 255 if v > 0 else 0)
    y = b - int(0.2 * K)
    rij = [binair.getpixel((x, y)) for x in range(l, r_)] + [0]
    beste, start = (0, 0), None
    for i, v in enumerate(rij):
        if v == 255 and start is None: start = i
        if v != 255 and start is not None:
            if i - start > beste[1] - beste[0]: beste = (start, i)
            start = None
    ImageDraw.floodfill(binair, (l + (beste[0] + beste[1]) // 2, y), 128)
    l2, _, r2, b2 = binair.point(lambda v: 255 if v == 128 else 0).getbbox()
    return ((l2 + r2) / 2 / K, b2 / K)


def zeshoek_onder_twee(twee, schaal):
    """Midden van de zeshoek onder de 2, en de verschuiving van het raster waarmee daar een zeshoek staat."""
    dx = HEX_R * math.sqrt(3)
    cx, cy = twee[0], twee[1] - TWEE_OVERLAP * schaal + HEX_R
    j = round((cy - (GY - 2)) / (HEX_R * 1.5))
    rij_x = (dx / 2 if j % 2 else 0)
    i = round((cx - rij_x) / dx)
    return (cx, cy), (cx - (i * dx + rij_x), cy - (GY - 2 + j * HEX_R * 1.5))


def oranje_v(midden):
    """De twee onderste zijden van de zeshoek rond midden, dik, in het oranje verloop."""
    cx, cy = midden
    h = [(cx + HEX_R * math.cos(math.pi / 3 * k + math.pi / 6), cy + HEX_R * math.sin(math.pi / 3 * k + math.pi / 6)) for k in range(3)]
    vorm = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    lijn_pad(ImageDraw.Draw(vorm), h, V_DIKTE, (255, 255, 255, 255))
    vlak = Image.new("RGB", (256, 256))
    for y in range(256):
        for x in range(256):
            t = (x + y) / 510
            vlak.putpixel((x, y), tuple(int(ORANJE[0][i] + (ORANJE[1][i] - ORANJE[0][i]) * t) for i in range(3)))
    vlak = vlak.resize((S, S)).convert("RGBA")
    vlak.putalpha(vorm.getchannel("A"))
    return vlak


def achtergrond(met_stoom=True, twee=None, schaal=1.0):
    """twee: plek van de 2 (uit twee_plek); dan schuift het raster zo dat er een zeshoek onder de 2 valt,
    en krijgt die zeshoek een oranje V. schaal: lettergrootte ten opzichte van het gewone icoon."""
    img = verloop(BOVEN, 0, 120).convert("RGBA")
    water = watermasker()
    onder = verloop(ONDER, GY - AMP * 1.6, 122).convert("RGBA")
    img.paste(onder, (0, 0), water)
    midden, schuif = zeshoek_onder_twee(twee, schaal) if twee else (None, (0.0, 0.0))
    img = Image.alpha_composite(img, ijsrooster(water, schuif))
    if midden:
        img = Image.alpha_composite(img, oranje_v(midden))
    schuim = Image.new("RGBA", (S, S), (0, 0, 0, 0))   # aparte laag: anders stapelt de doorzichtigheid op
    lijn_pad(ImageDraw.Draw(schuim), golf_punten(), 2.6, SCHUIM + (int(255 * 0.95),))
    img = Image.alpha_composite(img, schuim)
    if met_stoom:
        stoom = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        ds = ImageDraw.Draw(stoom)
        lijn_pad(ds, stoompluim(20, 29, 21), 3.2, (255, 255, 255, int(255 * 0.6)))
        lijn_pad(ds, stoompluim(31, 25, 16), 3.2, (255, 255, 255, int(255 * 0.45)))
        img = Image.alpha_composite(img, stoom)
    return img


def klein(img):
    return img.resize((MAAT, MAAT), Image.LANCZOS)


def afgerond(img, rx=27):
    m = Image.new("L", img.size, 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, img.size[0] - 1, img.size[1] - 1], radius=rx / 120 * img.size[0], fill=255)
    uit = img.copy()
    uit.putalpha(m)
    return uit


def main():
    os.makedirs(UIT_ASSETS, exist_ok=True)
    os.makedirs(UIT_DOCS, exist_ok=True)
    letters = h2o(120 - 2 * MARGE)
    bg = achtergrond(twee=twee_plek(letters))
    volledig = Image.alpha_composite(bg, letters)
    icoon = klein(volledig).convert("RGB")
    icoon.save(os.path.join(UIT_ASSETS, "icoon_1024.png"))
    icoon.resize((512, 512), Image.LANCZOS).save(os.path.join(UIT_DOCS, "icoon_store_512.png"))

    # Android adaptief: 108 dp laag, veilige zone 66 dp in het midden → H2O op ~58 % van de breedte.
    # De letters staan daar kleiner, dus de zeshoek met de oranje V komt op een eigen plek onder die 2.
    letters_a = h2o(120 * 0.58)
    bg_a = achtergrond(twee=twee_plek(letters_a), schaal=120 * 0.58 / (120 - 2 * MARGE))
    klein(bg_a).convert("RGB").save(os.path.join(UIT_ASSETS, "android_achtergrond.png"))
    klein(letters_a).save(os.path.join(UIT_ASSETS, "android_voorgrond.png"))
    # Android rond icoon (oudere toestellen): de adaptieve lagen samen, rond uitgesneden
    rond1024 = Image.alpha_composite(klein(bg_a), klein(letters_a))
    mr = Image.new("L", (MAAT, MAAT), 0)
    ImageDraw.Draw(mr).ellipse([0, 0, MAAT - 1, MAAT - 1], fill=255)
    rond1024.putalpha(mr)
    rond1024.save(os.path.join(UIT_ASSETS, "android_rond.png"))

    # voorbeeld: afgerond groot + kleine maten, en de Android-cirkel
    vb = Image.new("RGBA", (1024 + 40 + 600, 1024), (233, 238, 242, 255))
    vb.alpha_composite(afgerond(icoon.convert("RGBA")), (0, 0))
    x = 1064
    for m in (180, 120, 80, 58):
        vb.alpha_composite(afgerond(icoon.convert("RGBA").resize((m, m), Image.LANCZOS)), (x, 40))
        x += m + 20
    rond = Image.alpha_composite(klein(bg_a), klein(letters_a)).resize((300, 300), Image.LANCZOS)
    m = Image.new("L", (300, 300), 0)
    ImageDraw.Draw(m).ellipse([0, 0, 299, 299], fill=255)
    rond.putalpha(m)
    vb.alpha_composite(rond, (1064, 300))
    vb.save(os.path.join(UIT_DOCS, "voorbeeld.png"))
    print("klaar")


if __name__ == "__main__":
    main()
