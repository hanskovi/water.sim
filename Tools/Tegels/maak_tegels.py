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


def molecuul(d, x, y, schaal, hoek, font=None, labels=True):
    """H₂O: O in het midden, twee H op 104,5°."""
    for s in (-1, 1):
        a = math.radians(hoek + s * 52.25)
        hx, hy = x + math.cos(a) * 46 * schaal, y + math.sin(a) * 46 * schaal
        d.line([x, y, hx, hy], fill=WIT + (230,), width=max(2, int(8 * schaal)))
        atoom(d, hx, hy, 13 * schaal, WIT, "H" if labels else None, font)
    atoom(d, x, y, 22 * schaal, ROOD, "O" if labels else None, font)


def brug(d, a, b, dikte=5, streepjes=5):
    (x1, y1), (x2, y2) = a, b
    for i in range(streepjes):
        t0, t1 = (i + 0.2) / streepjes, (i + 0.7) / streepjes
        d.line([x1 + (x2 - x1) * t0, y1 + (y2 - y1) * t0, x1 + (x2 - x1) * t1, y1 + (y2 - y1) * t1],
               fill=CYAAN + (255,), width=dikte)


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
    elif n == 2:    # twee moleculen met een H-brug
        molecuul(d, cx - 120, cy + 10, 1.3, 20, f_klein)
        molecuul(d, cx + 110, cy - 20, 1.3, 200, f_klein)
        brug(db, (cx - 70, cy + 30), (cx + 85, cy - 20), 7, 6)
    elif n == 3:    # zeshoek uit het ijsrooster
        pts = [(cx + math.cos(math.radians(60 * k + 30)) * 120, cy + math.sin(math.radians(60 * k + 30)) * 120) for k in range(6)]
        for k in range(6):
            brug(db, pts[k], pts[(k + 1) % 6], 6, 4)
        for (x, y) in pts:
            atoom(d, x, y, 16, ROOD)
    elif n == 4:    # rooster dat uit elkaar valt
        pts = [(cx - 60 + math.cos(math.radians(60 * k + 30)) * 90, cy + math.sin(math.radians(60 * k + 30)) * 90) for k in range(6)]
        for k in range(3):
            brug(db, pts[k], pts[(k + 1) % 6], 6, 3)
        for (x, y) in pts:
            atoom(d, x, y, 13, ROOD)
        for i, (x, y, h) in enumerate([(cx + 120, cy - 70, 20), (cx + 150, cy + 40, 140), (cx + 70, cy + 120, 260)]):
            molecuul(d, x, y, 0.8, h, labels=False)
    elif n == 5:    # bekerglas met drijvend ijsblokje
        bx, by, bw, bh = cx - 110, cy - 90, 220, 200
        d.rectangle([bx, by + 70, bx + bw, by + bh], fill=(60, 150, 255, 110))
        d.line([bx, by, bx, by + bh, bx + bw, by + bh, bx + bw, by], fill=WIT + (230,), width=6)
        d.rectangle([bx + 70, by + 40, bx + 140, by + 100], fill=(225, 245, 255, 235), outline=CYAAN + (255,), width=4)
        d.line([bx, by + 70, bx + bw, by + 70], fill=CYAAN + (255,), width=4)
        d.text((bx + bw + 14, by + 60), "ρ", font=f_groot, fill=(255, 255, 255, 255))
    elif n == 6:    # koken: belletjes en losse moleculen
        for (x, y, r) in [(cx - 90, cy + 80, 26), (cx + 20, cy + 40, 18), (cx + 100, cy + 100, 30), (cx - 20, cy - 40, 14)]:
            d.ellipse([x - r, y - r, x + r, y + r], outline=CYAAN + (255,), width=4)
        for (x, y, h) in [(cx - 120, cy - 80, 30), (cx + 110, cy - 60, 200), (cx + 30, cy - 130, 110)]:
            molecuul(d, x, y, 0.75, h, labels=False)

    bruggen = gloed(bruggen, 6)
    tek = gloed(tek, 5)
    beeld = Image.alpha_composite(Image.alpha_composite(basis, bruggen), tek)
    os.makedirs(UIT, exist_ok=True)
    beeld.save(os.path.join(UIT, f"level{n}_neon.png"))


if __name__ == "__main__":
    for n in range(1, 7):
        tegel(n)
    print("klaar:", UIT)
