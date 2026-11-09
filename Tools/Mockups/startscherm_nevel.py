"""Mockup van het startscherm (glas en ijs, oranje accent), zonder en met nevelwolk, met de echte tegelplaatjes.

Gebruik:  python3 Tools/Mockups/startscherm_nevel.py
Uitvoer:  Docs/Mockups/startscherm_nevel.png
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HIER = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HIER, "..", ".."))
FONT_B = os.path.join(PROJECT, "Assets", "Fonts", "Ubuntu", "Ubuntu-Bold.ttf")
FONT_R = os.path.join(PROJECT, "Assets", "Fonts", "Ubuntu", "Ubuntu-Regular.ttf")
TEGELS = os.path.join(PROJECT, "Assets", "Resources", "interface")
UIT = os.path.join(PROJECT, "Docs", "Mockups")

Z = 2                         # pixels per eenheid (2× scherpte)
B, H = 390, 844               # schermmaat in eenheden (zoals de app: 390 breed)


def hx(c):
    c = c.lstrip("#")
    return tuple(int(c[i:i + 2], 16) for i in (0, 2, 4))


def f(eenh, vet=False):
    return ImageFont.truetype(FONT_B if vet else FONT_R, int(eenh * Z))


def verloop_v(w, h, a, b):
    img = Image.new("RGBA", (1, h))
    for y in range(h):
        t = y / max(1, h - 1)
        img.putpixel((0, y), tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3)) + (255,))
    return img.resize((w, h))


def verloop_h(w, h, a, b):
    img = Image.new("RGBA", (w, 1))
    for x in range(w):
        t = x / max(1, w - 1)
        img.putpixel((x, 0), tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3)) + (255,))
    return img.resize((w, h))


def afgerond(w, h, r):
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, w - 1, h - 1], radius=r, fill=255)
    return m


def plak_vlak(doek, x, y, w, h, r, vulling=None, kleur=None, alfa=255, rand=None):
    """Afgerond vlak: vulling is een beeld (verloop) of een kleur met doorzichtigheid; optioneel een rand."""
    X, Y, W, Hh, R = int(x * Z), int(y * Z), int(w * Z), int(h * Z), int(r * Z)
    m = afgerond(W, Hh, R)
    if vulling is None:
        vulling = Image.new("RGBA", (W, Hh), hx(kleur) + (alfa,))
        m = Image.eval(m, lambda v: v * alfa // 255)
    laag = Image.new("RGBA", doek.size, (0, 0, 0, 0))
    laag.paste(vulling.resize((W, Hh)), (X, Y), m)
    doek.alpha_composite(laag)
    if rand:
        d = ImageDraw.Draw(doek, "RGBA")
        d.rounded_rectangle([X, Y, X + W - 1, Y + Hh - 1], radius=R, outline=rand, width=max(1, Z))


def tekst(doek, x, y, s, grootte, kleur, vet=False, anker="ls"):
    ImageDraw.Draw(doek, "RGBA").text((x * Z, y * Z), s, font=f(grootte, vet), fill=kleur, anchor=anker)


def achtergrond(nevel):
    doek = verloop_v(B * Z, H * Z, hx("3CC0DC"), hx("1696B8"))
    basis = H * 0.70
    golf = [(x, basis + 15 * math.sin(x / B * 2 * math.pi + 0.6)) for x in range(0, B + 1, 2)]
    water = Image.new("L", doek.size, 0)
    ImageDraw.Draw(water).polygon([(x * Z, y * Z) for x, y in golf] + [(B * Z, H * Z), (0, H * Z)], fill=255)
    doek.paste(verloop_v(B * Z, H * Z, hx("0E7C9E"), hx("064A63")), (0, 0), water)
    # ijsrooster: onder de golf, naar onderen zichtbaarder
    ijs = Image.new("RGBA", doek.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(ijs)
    r, dx = 30, 30 * math.sqrt(3)
    for j in range(10):
        for i in range(-1, 9):
            cx, cy = i * dx + (dx / 2 if j % 2 else 0), H - j * r * 1.5
            if cy > basis - 10: continue
            pts = [((cx + r * math.cos(math.pi / 3 * k + math.pi / 6)) * Z, (cy + r * math.sin(math.pi / 3 * k + math.pi / 6)) * Z) for k in range(7)]
            d.line(pts, fill=hx("C4F4FB") + (255,), width=int(2.4 * Z))
    for j in range(10):
        for i in range(-1, 9):
            cx, cy = i * dx + (dx / 2 if j % 2 else 0), H - j * r * 1.5
            if cy <= basis - 10: continue
            pts = [((cx + r * math.cos(math.pi / 3 * k + math.pi / 6)) * Z, (cy + r * math.sin(math.pi / 3 * k + math.pi / 6)) * Z) for k in range(7)]
            d.line(pts, fill=hx("C4F4FB") + (255,), width=int(2.4 * Z))
    alfa = Image.new("L", (1, H * Z))
    for y in range(H * Z):
        t = (y / Z - basis) / (H - basis)
        alfa.putpixel((0, y), int(255 * 0.6 * max(0, min(1, t)) ** 1.1) if t > 0 else 0)
    alfa = alfa.resize(doek.size)
    from PIL import ImageChops
    ijs.putalpha(ImageChops.multiply(ImageChops.multiply(ijs.getchannel("A"), water), alfa))
    doek.alpha_composite(ijs)
    ImageDraw.Draw(doek, "RGBA").line([(x * Z, y * Z) for x, y in golf], fill=hx("BDEFFA") + (240,), width=4 * Z)
    if nevel:
        wolk = Image.new("RGBA", doek.size, (0, 0, 0, 0))
        ImageDraw.Draw(wolk).rounded_rectangle([14 * Z, 110 * Z, (B - 14) * Z, 600 * Z], radius=80 * Z, fill=(255, 255, 255, 80))
        doek.alpha_composite(wolk.filter(ImageFilter.GaussianBlur(28 * Z)))
    return doek


def scherm(nevel):
    doek = achtergrond(nevel)
    oranje = verloop_h(200, 50, hx("F7884D"), hx("E8502E"))
    wit = (255, 255, 255)
    glas_rand = (255, 255, 255, 90)
    top = 60
    # kop
    plak_vlak(doek, 20, top, 64, 44, 9, kleur="FFFFFF")
    d = ImageDraw.Draw(doek, "RGBA")
    d.text((52 * Z - 18 * Z, (top + 32) * Z), "H", font=f(22, True), fill=hx("0B3570"), anchor="ls")
    d.text((52 * Z - 1 * Z, (top + 36) * Z), "2", font=f(13, True), fill=hx("0B3570"), anchor="ls")
    d.text((52 * Z + 7 * Z, (top + 32) * Z), "O", font=f(22, True), fill=hx("0B3570"), anchor="ls")
    tekst(doek, 96, top + 33, "Water.", 30, wit, True)
    breed = f(30, True).getlength("Water.") / Z
    tekst(doek, 96 + breed + 1, top + 33, "sim", 21, wit)
    plak_vlak(doek, B - 20 - 52, top, 52, 44, 9, kleur="FFFFFF", alfa=40, rand=glas_rand)
    tekst(doek, B - 20 - 26, top + 28, "NL", 15, wit, anker="ms")
    ImageDraw.Draw(doek, "RGBA").rectangle([20 * Z, (top + 56) * Z, (B - 20) * Z, (top + 56) * Z + Z], fill=(255, 255, 255, 90))
    # tegels 3 × 2
    namen = ["binnenkort", "binnenkort", "ijskristal", "smelten", "dichtheid", "binnenkort"]
    tw, th, gx, gy = 110, 84, 9, 9
    x0 = (B - (3 * tw + 2 * gx)) / 2
    y0 = top + 68
    for i in range(6):
        x, y = x0 + (i % 3) * (tw + gx), y0 + (i // 3) * (th + gy)
        if i == 2: plak_vlak(doek, x, y, tw, th, 10, vulling=oranje)
        else: plak_vlak(doek, x, y, tw, th, 10, kleur="FFFFFF", alfa=40, rand=glas_rand)
        art = Image.open(os.path.join(TEGELS, f"level{i + 1}_neon.png")).convert("RGBA")
        aw = int((tw - 4) * Z); ah = int(aw * 3 / 4)
        if ah > (th - 4) * Z: ah = int((th - 4) * Z); aw = int(ah * 4 / 3)
        art = art.resize((aw, ah), Image.LANCZOS)
        speelbaar = i in (2, 3, 4)
        if not speelbaar:
            art.putalpha(Image.eval(art.getchannel("A"), lambda v: int(v * 0.45)))
        doek.alpha_composite(art, (int((x + tw / 2) * Z - aw / 2), int((y + th / 2) * Z - ah / 2)))
        tekst(doek, x + 8, y + 20, str(i + 1), 16, wit, True)
        tekst(doek, x + 8, y + th - 7, namen[i], 11, wit if speelbaar else (227, 247, 252, 230))
    # leagues
    ly = y0 + 2 * th + gy + 10
    plak_vlak(doek, 20, ly, B - 40, 42, 9, kleur="FFFFFF", alfa=26, rand=glas_rand)
    pw = (B - 40 - 8 - 8) / 3
    for i, t in enumerate(["Onderbouw", "Bovenbouw", "XPRT League"]):
        px = 24 + i * (pw + 4)
        if i == 1: plak_vlak(doek, px, ly + 4, pw, 34, 7, vulling=oranje)
        else: plak_vlak(doek, px, ly + 4, pw, 34, 7, kleur="FFFFFF", alfa=36)
        tekst(doek, px + pw / 2, ly + 26, t, 13.5, wit, i == 1, anker="ms")
    # uitlegkaart
    ky = ly + 52
    plak_vlak(doek, 20, ky, B - 40, 86, 12, kleur="FFFFFF", alfa=40, rand=glas_rand)
    tekst(doek, 34, ky + 28, "3 · Het ijskristal", 18, wit, True)
    tekst(doek, 34, ky + 52, "Vlieg door ijs. Elk molecuul zit vast aan 4", 13.5, (227, 247, 252, 255))
    tekst(doek, 34, ky + 70, "buren in een honingraat met lege kanalen.", 13.5, (227, 247, 252, 255))
    # start
    sy = ky + 96
    plak_vlak(doek, 20, sy, B - 40, 52, 12, kleur="FFFFFF")
    tekst(doek, B / 2 - 8, sy + 34, "Start", 19, hx("0E7C9E"), True, anker="ms")
    d = ImageDraw.Draw(doek, "RGBA")
    ax, ay = (B / 2 + 30) * Z, (sy + 26) * Z
    d.polygon([(ax - 6 * Z, ay - 8 * Z), (ax - 6 * Z, ay + 8 * Z), (ax + 7 * Z, ay)], fill=hx("0E7C9E"))
    # dunne voortgangsbalk + pil
    vy = sy + 64
    pil, gat = 60, 8
    bb = B - 40 - pil - gat
    plak_vlak(doek, 20, vy, bb, 12, 6, kleur="FFFFFF", alfa=46, rand=(255, 255, 255, 100))
    plak_vlak(doek, 20, vy, bb * 8 / 30, 12, 6, vulling=oranje)
    tekst(doek, 20 + bb / 2, vy + 9.5, "8/30", 9.5, wit, True, anker="ms")
    plak_vlak(doek, 20 + bb + gat, vy - 2, pil, 16, 8, vulling=oranje)
    sx, sy2, sr = 20 + bb + gat + 17, vy + 6, 5.5
    ster = [((sx + (sr if k % 2 == 0 else sr * 0.42) * math.cos(-math.pi / 2 + k * math.pi / 5)) * Z,
             (sy2 + (sr if k % 2 == 0 else sr * 0.42) * math.sin(-math.pi / 2 + k * math.pi / 5)) * Z) for k in range(10)]
    ImageDraw.Draw(doek, "RGBA").polygon(ster, fill=wit)
    tekst(doek, 20 + bb + gat + 36, vy + 10.5, "12", 11, wit, True, anker="ms")
    return doek


def telefoon(doek):
    rand = 12 * Z
    W, Hh = doek.size
    t = Image.new("RGBA", (W + 2 * rand, Hh + 2 * rand), (0, 0, 0, 0))
    ImageDraw.Draw(t).rounded_rectangle([0, 0, t.size[0] - 1, t.size[1] - 1], radius=58 * Z, fill=(5, 5, 8, 255))
    m = afgerond(W, Hh, 48 * Z)
    t.paste(doek, (rand, rand), m)
    ImageDraw.Draw(t).rounded_rectangle([t.size[0] / 2 - 62 * Z, rand, t.size[0] / 2 + 62 * Z, rand + 30 * Z], radius=15 * Z, fill=(5, 5, 8, 255))
    return t


def main():
    os.makedirs(UIT, exist_ok=True)
    a, b = telefoon(scherm(False)), telefoon(scherm(True))
    gat = 40 * Z
    vel = Image.new("RGBA", (a.size[0] * 2 + gat * 3, a.size[1] + 90 * Z), (233, 238, 242, 255))
    vel.alpha_composite(a, (gat, gat))
    vel.alpha_composite(b, (a.size[0] + 2 * gat, gat))
    d = ImageDraw.Draw(vel)
    d.text((gat + a.size[0] / 2, a.size[1] + gat + 28 * Z), "1 · Geen nevel", font=f(18, True), fill=(40, 50, 60), anchor="ms")
    d.text((2 * gat + a.size[0] * 1.5, a.size[1] + gat + 28 * Z), "2 · Nevelwolk achter de kolom", font=f(18, True), fill=(40, 50, 60), anchor="ms")
    vel = vel.convert("RGB")
    vel.save(os.path.join(UIT, "startscherm_nevel.png"))
    print("klaar")


if __name__ == "__main__":
    main()
