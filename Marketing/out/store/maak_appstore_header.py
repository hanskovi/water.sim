# App Store-productpagina: header (21:9, 3840 × 1646) en zoekresultaat-beeld (3:2, 3840 × 2560), NL en EN.
# Zonder doorzichtigheid. Apple snijdt de header per apparaat bij: alles wat belangrijk is staat in het midden.
# Gebruik: python3 maak_appstore_header.py   (na prep_scherm.py; gebruikt scherm 1, 3 en 5 per taal)
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import math, os
HIER = os.path.dirname(os.path.abspath(__file__))
FM = os.path.join(HIER, "../../../Assets/Fonts/Ubuntu/Ubuntu-Medium.ttf")
FR = os.path.join(HIER, "../../../Assets/Fonts/Ubuntu/Ubuntu-Regular.ttf")
FB = os.path.join(HIER, "../../../Assets/Fonts/Ubuntu/Ubuntu-Bold.ttf")
icoon = Image.open(os.path.join(HIER, "../../../Assets/Icoon/icoon_1024.png")).convert("RGBA")
BLAUW, TEAL, ORANJE, LICHT = (91, 194, 255), (90, 210, 200), (242, 138, 85), (185, 220, 245)
T = {"nl": dict(tag="Water op molecuulniveau", sub="voor scheikunde en natuurkunde · 6 levels · 3 niveaus"),
     "en": dict(tag="Water at the molecular level", sub="for chemistry and physics · 6 levels · 3 difficulties")}


def rond(im, r):
    m = Image.new("L", im.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, im.width - 1, im.height - 1], radius=r, fill=255)
    o = im.copy(); o.putalpha(m); return o


def achtergrond(W, H):
    im = Image.new("RGBA", (W, H)); d = ImageDraw.Draw(im)
    for y in range(H):
        t = y / H; d.line([(0, y), (W, y)], fill=(int(6 + 6 * t), int(20 + 26 * t), int(34 + 44 * t), 255))
    gl = Image.new("RGBA", (W, H), (0, 0, 0, 0)); g = ImageDraw.Draw(gl)
    g.ellipse([W * 0.45, -H * 0.2, W * 1.05, H * 1.1], fill=BLAUW + (60,)); g.ellipse([W * 0.1, H * 0.4, W * 0.6, H * 1.4], fill=TEAL + (40,))
    im.alpha_composite(gl.filter(ImageFilter.GaussianBlur(min(W, H) * 0.15)))
    # vage honingraat onderin (zoals in het icoon)
    hx = Image.new("RGBA", (W, H), (0, 0, 0, 0)); dh = ImageDraw.Draw(hx)
    r = H * 0.075; dx = r * math.sqrt(3)
    for j in range(-1, int(H / (r * 1.5)) + 2):
        for i in range(-1, int(W / dx) + 2):
            cx, cy = i * dx + (dx / 2 if j % 2 else 0), j * r * 1.5
            a = max(0, min(1, (cy / H - 0.45) / 0.55))
            if a <= 0: continue
            pts = [(cx + r * math.cos(math.pi / 3 * k + math.pi / 6), cy + r * math.sin(math.pi / 3 * k + math.pi / 6)) for k in range(7)]
            dh.line(pts, fill=(196, 244, 251, int(38 * a)), width=max(2, int(r * 0.06)))
    im.alpha_composite(hx)
    return im


def telefoon(im, pad, cx, top, hoogte):
    p = Image.open(pad).convert("RGBA"); s = hoogte / p.height
    p = p.resize((int(p.width * s), int(p.height * s)), Image.LANCZOS)
    x = int(cx - p.width / 2)
    W, H = im.size
    sch = Image.new("RGBA", (W, H), (0, 0, 0, 0)); m = Image.new("L", p.size, 0); m.paste(p.getchannel("A"))
    sch.paste((0, 0, 0, 160), (x + int(hoogte * 0.012), int(top + hoogte * 0.02)), m)
    im.alpha_composite(sch.filter(ImageFilter.GaussianBlur(hoogte * 0.02)))
    im.alpha_composite(p.crop((0, 0, p.width, min(p.height, H - int(top)))), (x, int(top)))


def titel(im, x, y, schaal, t, midden=False, met_sub=True):
    """Icoon + "Water.sim" + regel eronder. Geeft de breedte terug."""
    d = ImageDraw.Draw(im)
    ic = int(220 * schaal); fT = ImageFont.truetype(FB, int(150 * schaal)); fS = ImageFont.truetype(FR, int(104 * schaal))
    fG = ImageFont.truetype(FM, int(62 * schaal)); fK = ImageFont.truetype(FR, int(44 * schaal))
    bW = d.textlength("Water", font=fT) + d.textlength(".sim", font=fS)
    breed = ic + int(40 * schaal) + max(bW, d.textlength(t["tag"], font=fG), d.textlength(t["sub"], font=fK) if met_sub else 0)
    if midden: x -= breed / 2
    im.alpha_composite(rond(icoon.resize((ic, ic), Image.LANCZOS), int(ic * 0.225)), (int(x), int(y)))
    tx = x + ic + 40 * schaal
    d.text((tx, y - 6 * schaal), "Water", font=fT, fill=(255, 255, 255))
    d.text((tx + d.textlength("Water", font=fT), y + 36 * schaal), ".sim", font=fS, fill=(255, 255, 255))
    d.text((tx, y + 175 * schaal), t["tag"], font=fG, fill=LICHT)
    if met_sub: d.text((tx, y + 255 * schaal), t["sub"], font=fK, fill=(150, 185, 210))
    return breed


def schermen(taal):
    m = os.path.join(HIER, "schermen" if taal == "nl" else "schermen_" + taal)
    return [os.path.join(m, f"scherm_{k}.png") for k in (3, 1, 5)]      # meertje, ijskristal, H-brug


os.makedirs(os.path.join(HIER, "header"), exist_ok=True)
for taal, t in T.items():
    if not all(os.path.exists(p) for p in schermen(taal)): print(f"{taal}: schermen ontbreken, overgeslagen"); continue
    meer, vlieg, brug = schermen(taal)
    # ---------- header 21:9 ----------
    W, H = 3840, 1646; im = achtergrond(W, H)
    titel(im, W * 0.10, H * 0.38, 1.25, t, met_sub=False)          # in de header geen derde regel: die loopt achter de telefoons
    h = H * 0.86
    telefoon(im, meer, W * 0.585, H * 0.20, h * 0.92); telefoon(im, brug, W * 0.835, H * 0.20, h * 0.92)
    telefoon(im, vlieg, W * 0.71, H * 0.10, h)
    im.convert("RGB").save(os.path.join(HIER, f"header/appstore_header_3840x1646_{taal}.png"))
    # ---------- zoekresultaten 3:2 ----------
    W, H = 3840, 2560; im = achtergrond(W, H)
    titel(im, W / 2, H * 0.06, 1.35, t, midden=True)
    h = H * 0.70
    telefoon(im, meer, W * 0.25, H * 0.33, h * 0.94); telefoon(im, brug, W * 0.75, H * 0.33, h * 0.94)
    telefoon(im, vlieg, W * 0.5, H * 0.29, h)
    im.convert("RGB").save(os.path.join(HIER, f"header/appstore_zoekresultaten_3840x2560_{taal}.png"))
print("klaar")
