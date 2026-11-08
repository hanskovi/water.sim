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
    elif n == 5:    # bekerglas met drijvend ijsblokje
        bx, by, bw, bh = cx - 110, cy - 90, 220, 200
        d.rectangle([bx, by + 70, bx + bw, by + bh], fill=(60, 150, 255, 110))
        d.line([bx, by, bx, by + bh, bx + bw, by + bh, bx + bw, by], fill=WIT + (230,), width=6)
        d.rectangle([bx + 70, by + 40, bx + 140, by + 100], fill=(225, 245, 255, 235), outline=CYAAN + (255,), width=4)
        d.line([bx, by + 70, bx + bw, by + 70], fill=CYAAN + (255,), width=4)
        d.text((bx + bw + 14, by + 60), "ρ", font=f_groot, fill=(255, 255, 255, 255))
    elif n == 6:    # koken: dicht water onderin met belletjes; hele moleculen komen los en stijgen op
        opp = cy + 25
        d.line([cx - 175, opp, cx + 175, opp], fill=CYAAN + (220,), width=4)
        for (x, y, r) in [(cx - 40, cy + 95, 24), (cx + 95, cy + 120, 17)]:
            d.ellipse([x - r, y - r, x + r, y + r], outline=CYAAN + (255,), width=4)
        for (x, y, h) in [(cx - 140, cy + 60, 20), (cx - 95, cy + 105, 250), (cx + 20, cy + 60, 130),
                          (cx + 60, cy + 115, 330), (cx + 140, cy + 70, 200), (cx - 150, cy + 130, 90), (cx + 165, cy + 130, 60)]:
            molecuul(d, x, y, 0.6, h, labels=False)
        for (x, y, h) in [(cx - 90, cy - 60, 300), (cx + 60, cy - 105, 210), (cx + 140, cy - 30, 30)]:
            molecuul(d, x, y, 0.62, h, labels=False)
            for k in range(1, 4):                               # spoortje: hij komt uit het water
                rr = 5 - k
                d.ellipse([x - rr, y + 26 + k * 16 - rr, x + rr, y + 26 + k * 16 + rr], fill=CYAAN + (220 - 55 * k,))

    bruggen = gloed(bruggen, 6)
    tek = gloed(tek, 5)
    beeld = Image.alpha_composite(Image.alpha_composite(basis, bruggen), tek)
    os.makedirs(UIT, exist_ok=True)
    beeld.save(os.path.join(UIT, f"level{n}_neon.png"))


if __name__ == "__main__":
    for n in range(1, 7):
        tegel(n)
    print("klaar:", UIT)
