"""Maakt de neon level-tegels van Water.sim (tijdelijk beeld, in de stijl van C.chem).

Gebruik:  python3 Tools/Tegels/maak_tegels.py
Uitvoer:  Assets/Resources/interface/level{1..6}_neon.png  (640 × 480, doorzichtige achtergrond)

Elke tegel: een gloeiende kern met ringen in ijsblauw/violet, en een tekening die bij het level hoort.
De app zet de tegel op een donkere ondergrond; de geselecteerde tegel krijgt het warme verloop van C.chem.
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont

W, H = 640, 480
HIER = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HIER, "..", ".."))
UIT = os.path.join(PROJECT, "Assets", "Resources", "interface")
FONT = os.path.join(PROJECT, "Assets", "Fonts", "Ubuntu", "Ubuntu-Bold.ttf")

ROOD = (235, 90, 90)
WIT = (245, 248, 255)
BLAUW = (80, 170, 255)
CYAAN = (120, 230, 255)


def laag():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def gloed(img, straal):
    """Voegt een zachte gloed toe: vervaagde kopie onder het origineel."""
    zacht = img.filter(ImageFilter.GaussianBlur(straal))
    return Image.alpha_composite(Image.alpha_composite(laag(), zacht), img)


def achtergrondgloed(cx, cy, r, kleur_kern=(255, 255, 255), kleur_rand=(60, 140, 255)):
    img = laag()
    d = ImageDraw.Draw(img)
    for i in range(60, 0, -1):
        f = i / 60
        rr = r * f
        t = 1 - f
        kleur = tuple(int(kleur_rand[k] * (1 - t) + kleur_kern[k] * t) for k in range(3))
        alpha = int(255 * (t ** 1.6) * 0.9)
        d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=kleur + (alpha,))
    img = img.filter(ImageFilter.GaussianBlur(12))
    # neonringen
    ringen = laag()
    dr = ImageDraw.Draw(ringen)
    for i, (k, w) in enumerate([((60, 120, 255), 10), ((150, 80, 255), 7), ((90, 220, 255), 5)]):
        rr = r * (1.05 + i * 0.08)
        dr.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], outline=k + (200,), width=w)
    ringen = gloed(ringen, 8)
    # kruisvormige lichtstreep (lensflare zoals in C.chem)
    flare = laag()
    df = ImageDraw.Draw(flare)
    df.line([cx - r * 1.2, cy, cx + r * 1.2, cy], fill=(200, 230, 255, 140), width=3)
    df.line([cx, cy - r * 1.0, cx, cy + r * 1.0], fill=(200, 230, 255, 110), width=3)
    flare = flare.filter(ImageFilter.GaussianBlur(2))
    return Image.alpha_composite(Image.alpha_composite(ringen, img), flare)


def atoom(d, x, y, r, kleur, label=None, font=None):
    d.ellipse([x - r, y - r, x + r, y + r], fill=kleur + (255,))
    d.ellipse([x - r * 0.45, y - r * 0.6, x - r * 0.05, y - r * 0.2], fill=(255, 255, 255, 120))
    if label and font:
        d.text((x, y), label, font=font, fill=(255, 255, 255, 255), anchor="mm")


def teken_molecuul(d, o, hs, schaal, font=None, labels=False):
    """Watermolecuul: O-atoom op o, H-atomen op de plekken hs (met O–H-staafjes)."""
    for (hx, hy) in hs:
        d.line([o[0], o[1], hx, hy], fill=WIT + (235,), width=max(2, int(8 * schaal)))
        atoom(d, hx, hy, 13 * schaal, WIT, "H" if labels else None, font)
    atoom(d, o[0], o[1], 22 * schaal, ROOD, "O" if labels else None, font)


def molecuul(d, x, y, schaal, hoek, font=None, labels=True):
    """H2O: O op (x, y), twee H op 104,5°; 'hoek' is de richting midden tussen de twee H-atomen.
    Geeft de plek van O en van beide H-atomen terug."""
    hs = []
    for s in (-1, 1):
        a = math.radians(hoek + s * 52.25)
        hs.append((x + math.cos(a) * 46 * schaal, y + math.sin(a) * 46 * schaal))
    teken_molecuul(d, (x, y), hs, schaal, font, labels)
    return (x, y), hs


def brug(d, a, b, dikte=5, streepjes=5):
    (x1, y1), (x2, y2) = a, b
    for i in range(streepjes):
        t0, t1 = (i + 0.2) / streepjes, (i + 0.7) / streepjes
        d.line([x1 + (x2 - x1) * t0, y1 + (y2 - y1) * t0, x1 + (x2 - x1) * t1, y1 + (y2 - y1) * t1],
               fill=CYAAN + (255,), width=dikte)


def h_brug(d, h, o, rh, ro, dikte=6, streepjes=5):
    """Waterstofbrug van het H-atoom h naar het O-atoom o (stippellijn tussen de randen van de bolletjes)."""
    dx, dy = o[0] - h[0], o[1] - h[1]
    L = math.hypot(dx, dy)
    ux, uy = dx / L, dy / L
    brug(d, (h[0] + ux * (rh + 3), h[1] + uy * (rh + 3)), (o[0] - ux * (ro + 3), o[1] - uy * (ro + 3)), dikte, streepjes)


def ijsring(d, db, cx, cy, R, schaal, hoeken):
    """Stuk ijsrooster van hele watermoleculen (ijsregels): elk O-atoom levert één H langs de ring
    naar het volgende O-atoom (met een H-brug), en het tweede H wijst naar buiten."""
    pts = [(cx + math.cos(math.radians(a)) * R, cy + math.sin(math.radians(a)) * R) for a in hoeken]
    oh = 46 * schaal
    for k, o in enumerate(pts):
        if k + 1 < len(pts) or len(pts) == 6:
            nxt = pts[(k + 1) % len(pts)]
            ex, ey = nxt[0] - o[0], nxt[1] - o[1]
        else:                                          # laatste van een open stuk: H wijst naar waar de ring verder zou gaan
            prv = pts[k - 1]
            ex, ey = o[0] - prv[0], o[1] - prv[1]
            a = math.atan2(ey, ex) + math.radians(60)
            ex, ey = math.cos(a), math.sin(a)
            nxt = None
        L = math.hypot(ex, ey); ex, ey = ex / L, ey / L
        h1 = (o[0] + ex * oh, o[1] + ey * oh)
        # tweede H: 104,5° gedraaid, naar buiten de ring
        bx, by = o[0] - cx, o[1] - cy
        beste = None
        for teken in (1, -1):
            a = math.atan2(ey, ex) + teken * math.radians(104.5)
            kand = (math.cos(a), math.sin(a))
            if beste is None or kand[0] * bx + kand[1] * by > beste[0] * bx + beste[1] * by:
                beste = kand
        h2 = (o[0] + beste[0] * oh, o[1] + beste[1] * oh)
        teken_molecuul(d, o, [h1, h2], schaal)
        if nxt is not None:
            h_brug(db, h1, nxt, 13 * schaal, 22 * schaal, max(4, int(7 * schaal)), 3)


BINNEN_R = 178   # alles (moleculen, atomen, bruggen) blijft binnen deze straal; de binnenste ring ligt op ~200


def binnen_cirkel(lagen, cx, cy, straal):
    """Schaalt de tekening rond het midden kleiner tot alles binnen de cirkel valt (zoekt de grootste passende schaal)."""
    from PIL import ImageChops
    buiten = Image.new("L", (W, H), 255)
    ImageDraw.Draw(buiten).ellipse([cx - straal, cy - straal, cx + straal, cy + straal], fill=0)

    def geschaald(img, s):
        return img.transform((W, H), Image.AFFINE, (1 / s, 0, cx - cx / s, 0, 1 / s, cy - cy / s), resample=Image.BICUBIC)

    def past(s):
        for l in lagen:
            a = geschaald(l, s).getchannel("A").point(lambda v: 255 if v > 24 else 0)
            if ImageChops.multiply(a, buiten).getbbox():
                return False
        return True

    if past(1.0):
        return lagen
    laag_s, hoog_s = 0.4, 1.0
    for _ in range(12):
        mid = (laag_s + hoog_s) / 2
        if past(mid): laag_s = mid
        else: hoog_s = mid
    return [geschaald(l, laag_s) for l in lagen]


def golfpad(y0, amp, x0, x1, n=80):
    return [(x0 + (x1 - x0) * i / n, y0 + amp * math.sin(i / n * 2 * math.pi * 1.2 + 0.4)) for i in range(n + 1)]


def vloeistof(d, db, punten, schaal):
    """Losse watermoleculen; korte H-bruggen waar een H recht naar een buur wijst."""
    mols = [molecuul(d, x, y, schaal, h, labels=False) for (x, y, h) in punten]
    for i, (o, hs) in enumerate(mols):
        for hp in hs:
            for j, (o2, _) in enumerate(mols):
                if i == j: continue
                vx, vy = o2[0] - hp[0], o2[1] - hp[1]
                a = math.hypot(vx, vy)
                rx, ry = hp[0] - o[0], hp[1] - o[1]
                if a < 60 * schaal / 0.55 and (vx * rx + vy * ry) / (a * math.hypot(rx, ry)) > 0.8:
                    h_brug(db, hp, o2, 13 * schaal, 22 * schaal, 4, 2)



GRIJS = (70, 74, 84)


def _draai(p, ax, ay):
    x, y, z = p
    c, s = math.cos(ay), math.sin(ay); x, z = c * x + s * z, -s * x + c * z
    c, s = math.cos(ax), math.sin(ax); y, z = c * y - s * z, s * y + c * z
    return (x, y, z)


def _dempen(kleur, t):
    """Achterste atomen donkerder (t = 0 achter, 1 voor), voor diepte."""
    m = 0.3 + 0.7 * t ** 1.3
    return tuple(int(c * m) for c in kleur)


def gashydraat(tek, db, cx, cy, R=92, schaal=0.42):
    """Kooi van gashydraat (dodecaëder, 5^12): 20 watermoleculen op de hoekpunten, H-bruggen langs de 30 ribben
    (ijsregels: elke rib één H), met een methaanmolecuul in het midden. Model, niet op schaal."""
    phi = (1 + 5 ** 0.5) / 2
    v = [(a, b, c) for a in (-1, 1) for b in (-1, 1) for c in (-1, 1)]
    for a in (-1, 1):
        for b in (-1, 1):
            v += [(0, a / phi, b * phi), (a / phi, b * phi, 0), (a * phi, 0, b / phi)]
    ribben = [(i, j) for i in range(20) for j in range(i + 1, 20) if abs(math.dist(v[i], v[j]) - 2 / phi) < 1e-6]
    v = [_draai(p, 0.35, 0.5) for p in v]
    P = [(cx + x * R * 6 / (6 - z), cy + y * R * 6 / (6 - z), z, 6 / (6 - z)) for (x, y, z) in v]
    zs = [p[2] for p in P]; z0, z1 = min(zs), max(zs)
    t = [(z - z0) / (z1 - z0) for z in zs]
    gegeven = [0] * 20; donor = {}
    for (i, j) in sorted(ribben, key=lambda e: (e[0] * 7 + e[1] * 3) % 11):
        dd = i if gegeven[i] <= gegeven[j] else j
        donor[(i, j)] = dd; gegeven[dd] += 1
    items = [((zs[i] + zs[j]) / 2, "rib", donor[(i, j)], j if donor[(i, j)] == i else i) for (i, j) in ribben]
    items += [(zs[i] + 0.01, "O", i, None) for i in range(20)] + [(0.2, "CH4", None, None)]
    items.sort(key=lambda x: x[0])
    for z, soort, i, j in items:
        d = ImageDraw.Draw(tek)
        if soort == "rib":
            o, a = P[i], P[j]; tt = (t[i] + t[j]) / 2; s = schaal * (o[3] + a[3]) / 2
            ux, uy = a[0] - o[0], a[1] - o[1]; L = math.hypot(ux, uy); ux, uy = ux / L, uy / L
            h = (o[0] + ux * 46 * s, o[1] + uy * 46 * s)
            d.line([o[0], o[1], h[0], h[1]], fill=_dempen(WIT, tt) + (235,), width=max(2, int(8 * s)))
            atoom(d, h[0], h[1], 13 * s, _dempen(WIT, tt))
            if L > 81 * s + 8:
                h_brug(db, h, (a[0], a[1]), 13 * s, 22 * s, max(3, int(6 * s)), 3)
        elif soort == "O":
            p = P[i]; s = schaal * p[3]
            if gegeven[i] < 2:                         # tweede H wijst naar buiten de kooi
                ux, uy = p[0] - cx, p[1] - cy; L = math.hypot(ux, uy) or 1
                h = (p[0] + ux / L * 46 * s, p[1] + uy / L * 46 * s)
                d.line([p[0], p[1], h[0], h[1]], fill=_dempen(WIT, t[i]) + (235,), width=max(2, int(8 * s)))
                atoom(d, h[0], h[1], 13 * s, _dempen(WIT, t[i]))
            atoom(d, p[0], p[1], 22 * s, _dempen(ROOD, t[i]))
        else:                                           # methaan (tetraëder) in het midden
            hs = sorted((_draai(q, 0.6, 0.4) for q in [(1, 1, 1), (1, -1, -1), (-1, 1, -1), (-1, -1, 1)]), key=lambda q: q[2])
            for q in [q for q in hs if q[2] <= 0]:
                hx, hy = cx + q[0] * 30, cy + q[1] * 30
                d.line([cx, cy, hx, hy], fill=WIT + (235,), width=6); atoom(d, hx, hy, 11, (200, 205, 215))
            atoom(d, cx, cy, 20, GRIJS)
            d.ellipse([cx - 20, cy - 20, cx + 20, cy + 20], outline=(150, 155, 170, 255), width=2)
            for q in [q for q in hs if q[2] > 0]:
                hx, hy = cx + q[0] * 30, cy + q[1] * 30
                d.line([cx, cy, hx, hy], fill=WIT + (235,), width=6); atoom(d, hx, hy, 11, WIT)
    return tek


def tegel(n):
    f_groot = ImageFont.truetype(FONT, 26)
    f_klein = ImageFont.truetype(FONT, 18)
    cx, cy = W * 0.6, H * 0.55
    basis = achtergrondgloed(cx, cy, 190)
    tek = laag()
    d = ImageDraw.Draw(tek)
    bruggen = laag()
    db = ImageDraw.Draw(bruggen)

    if n == 1:      # één watermolecuul, met δ+ en δ−
        molecuul(d, cx, cy - 10, 2.0, 90, f_groot)
        d.text((cx + 58, cy - 48), "δ−", font=f_klein, fill=(255, 190, 190, 255))
        d.text((cx - 140, cy + 92), "δ+", font=f_klein, fill=(200, 230, 255, 255))
        d.text((cx + 115, cy + 92), "δ+", font=f_klein, fill=(200, 230, 255, 255))
    elif n == 2:    # H-brug: van een H-atoom van het ene molecuul naar het O-atoom van het andere
        s = 1.3
        o1, hs1 = molecuul(d, cx - 160, cy + 40, s, 37, f_klein)
        h = hs1[0]                                         # dit H wijst naar het andere molecuul
        ux, uy = h[0] - o1[0], h[1] - o1[1]
        L = math.hypot(ux, uy); ux, uy = ux / L, uy / L
        o2 = (h[0] + ux * 150, h[1] + uy * 150)
        molecuul(d, o2[0], o2[1], s, math.degrees(math.atan2(uy, ux)), f_klein)
        h_brug(db, h, o2, 13 * s, 22 * s, 10, 6)
    elif n == 3:    # zeshoek uit het ijsrooster, van hele watermoleculen
        ijsring(d, db, cx, cy, 112, 0.72, [60 * k + 30 for k in range(6)])
    elif n == 4:    # smelten: links nog ijs, rechts losgekomen, dicht opeengepakte watermoleculen
        ijsring(d, db, cx - 70, cy - 5, 78, 0.55, [90, 150, 210, 270])
        los = [(cx + 70, cy - 70, 200), (cx + 125, cy - 15, 300), (cx + 60, cy + 10, 40),
               (cx + 120, cy + 60, 160), (cx + 55, cy + 85, 250), (cx + 5, cy + 60, 110), (cx + 165, cy + 25, 20)]
        mols = [molecuul(d, x, y, 0.55, h, labels=False) for (x, y, h) in los]
        for i, (o, hs) in enumerate(mols):                  # een paar korte H-bruggen in de vloeistof
            for hp in hs:
                for j, (o2, _) in enumerate(mols):
                    if i == j: continue
                    vx, vy = o2[0] - hp[0], o2[1] - hp[1]
                    afst = math.hypot(vx, vy)
                    rx, ry = hp[0] - o[0], hp[1] - o[1]
                    if afst < 58 and (vx * rx + vy * ry) / (afst * math.hypot(rx, ry)) > 0.8:
                        h_brug(db, hp, o2, 13 * 0.55, 22 * 0.55, 4, 2)
    elif n == 5:    # dichtheid: ijsblokje (honingraat van hele moleculen) drijft, een klein stukje boven het water
        wl = cy + 5
        db.line(golfpad(wl, 5, cx - 170, cx + 170), fill=CYAAN + (235,), width=5)
        z = 150
        bx, by = cx - z / 2, wl - z * 0.12
        blok = laag()
        ImageDraw.Draw(blok).rounded_rectangle([bx, by, bx + z, by + z * 0.82], radius=14, fill=(200, 240, 255, 55), outline=(235, 250, 255, 255), width=5)
        tek = Image.alpha_composite(tek, blok)
        d = ImageDraw.Draw(tek)
        ijsring(d, db, cx, by + z * 0.41, 38, 0.36, [60 * k + 30 for k in range(6)])
        vloeistof(d, db, [(cx - 140, wl + 40, 20), (cx + 140, wl + 45, 200), (cx - 125, wl + 95, 300), (cx + 125, wl + 100, 110),
                          (cx - 60, wl + 140, 60), (cx + 20, wl + 150, 250), (cx + 90, wl + 145, 330)], 0.45)
    elif n == 6:    # Ontdek: gashydraat, een kooi van watermoleculen (H-bruggen) met een methaanmolecuul erin
        tek = gashydraat(tek, db, cx, cy)
        d = ImageDraw.Draw(tek)

    tek, bruggen = binnen_cirkel([tek, bruggen], cx, cy, BINNEN_R)
    bruggen = gloed(bruggen, 6)
    tek = gloed(tek, 5)
    beeld = Image.alpha_composite(Image.alpha_composite(basis, bruggen), tek)
    os.makedirs(UIT, exist_ok=True)
    beeld.save(os.path.join(UIT, f"level{n}_neon.png"))


if __name__ == "__main__":
    import sys
    for n in ([int(a) for a in sys.argv[1:]] or range(1, 7)):     # bv. "python3 maak_tegels.py 6" voor alleen tegel 6
        tegel(n)
    print("klaar:", UIT)
