# Header (Google Play feature graphic 1024×500) en zoekresultaat-mockups (Play + App Store), NL en EN.
# Teksten uit Docs/Store_WaterSim.md. Gebruik: python3 maak_header.py (na maak_screenshots.py)
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import math, os
HIER = os.path.dirname(os.path.abspath(__file__))
FM = os.path.join(HIER, "../../../Assets/Fonts/Ubuntu/Ubuntu-Medium.ttf")
FR = os.path.join(HIER, "../../../Assets/Fonts/Ubuntu/Ubuntu-Regular.ttf")
FB = os.path.join(HIER, "../../../Assets/Fonts/Ubuntu/Ubuntu-Bold.ttf")
icoon = Image.open(os.path.join(HIER, "../../../Assets/Icoon/icoon_1024.png")).convert("RGBA")
VLIEG, MEER = os.path.join(HIER, "schermen/scherm_1.png"), os.path.join(HIER, "schermen/scherm_3.png")
BLAUW, TEAL, ORANJE = (91, 194, 255), (90, 210, 200), (242, 138, 85)
LICHT = (185, 220, 245)


def rond(im, r):
    m = Image.new("L", im.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, im.width - 1, im.height - 1], radius=r, fill=255)
    o = im.copy(); o.putalpha(m); return o


def ster(d, cx, cy, r, k):
    d.polygon([(cx + (r if i % 2 == 0 else r * 0.45) * math.cos(-math.pi / 2 + i * math.pi / 5),
                cy + (r if i % 2 == 0 else r * 0.45) * math.sin(-math.pi / 2 + i * math.pi / 5)) for i in range(10)], fill=k)


def tekst(d, xy, s, font, fill, anchor="la"):
    """Tekst met een echte lage 2 (₂): Ubuntu heeft dat teken niet, dus kleiner en lager zelf tekenen."""
    x, y = xy
    if "₂" not in s:
        d.text((x, y), s, font=font, fill=fill, anchor=anchor); return
    klein = ImageFont.truetype(font.path, int(font.size * 0.62))
    delen = s.split("₂")
    breed = sum(d.textlength(p, font=font) for p in delen) + (len(delen) - 1) * d.textlength("2", font=klein)
    if anchor[0] == "m": x -= breed / 2
    elif anchor[0] == "r": x -= breed
    a = "l" + anchor[1]
    for i, p in enumerate(delen):
        d.text((x, y), p, font=font, fill=fill, anchor=a); x += d.textlength(p, font=font)
        if i < len(delen) - 1:
            basis = y + font.getmetrics()[0] if a[1] == "a" else y      # basislijn van de grote letters
            d.text((x, basis + font.size * 0.16), "2", font=klein, fill=fill, anchor="ls"); x += d.textlength("2", font=klein)


def lengte(s): return len(s)


T = {"nl": dict(tag=["Water op molecuulniveau,", "voor scheikunde en natuurkunde"],
                pijlers=[("Moleculen in 3D", "watermolecuul, H-brug, ijskristal"), ("Smelten, stollen, drijven", "opwarmkromme, dichtheid, 4 °C"),
                         ("Quiz en tekenopdracht", "3 niveaus · streaks")],
                extra="6 levels · model, niet op schaal · zonder account",
                as_naam="Water.sim – H₂O in 3D", as_sub="Water op molecuulniveau", gp_titel="Water.sim – H₂O in 3D",
                gp_kort="Water op molecuulniveau: H-bruggen, ijs, smelten en waarom ijs drijft.", cat="Onderwijs", krijg="Download",
                inst="Installeren", gratis="Gratis", noot1="De eerste 3 screenshots verschijnen in het zoekresultaat.",
                noot2="Naam max 30 · ondertitel max 30 tekens", kort="(korte beschrijving, max 80 tekens)", zoek="water.sim"),
     "en": dict(tag=["Water at the molecular level,", "for chemistry and physics"],
                pijlers=[("Molecules in 3D", "water molecule, H-bond, ice crystal"), ("Melt, freeze, float", "heating curve, density, 4 °C"),
                         ("Quiz and drawing task", "3 levels · streaks")],
                extra="6 levels · model, not to scale · no account",
                as_naam="Water.sim – H₂O in 3D", as_sub="Water at the molecular level", gp_titel="Water.sim – H₂O in 3D",
                gp_kort="Water at the molecular level: hydrogen bonds, ice, melting and why ice floats.", cat="Education", krijg="Get",
                inst="Install", gratis="Free", noot1="The first 3 screenshots appear in search results.",
                noot2="Name max 30 · subtitle max 30 characters", kort="(short description, max 80 characters)", zoek="water.sim")}

for taal, t in T.items():
    for k in ("as_naam", "as_sub", "gp_titel"): assert lengte(t[k]) <= 30, (taal, k)
    assert lengte(t["gp_kort"]) <= 80, taal
    # ---------- header (Google Play feature graphic) ----------
    W, H = 1024, 500; im = Image.new("RGBA", (W, H)); d = ImageDraw.Draw(im)
    for x in range(W):
        u = x / W; d.line([(x, 0), (x, H)], fill=(int(6 + 4 * u), int(20 + 20 * u), int(34 + 36 * u), 255))
    gl = Image.new("RGBA", (W, H), (0, 0, 0, 0)); g = ImageDraw.Draw(gl)
    g.ellipse([480, 40, 780, 520], fill=BLAUW + (55,)); g.ellipse([660, -60, 1110, 540], fill=TEAL + (45,))
    im.alpha_composite(gl.filter(ImageFilter.GaussianBlur(80)))
    ic = rond(icoon.resize((92, 92), Image.LANCZOS), 21); im.alpha_composite(ic, (44, 40))
    f = ImageFont.truetype(FB, 52); d.text((150, 42), "Water", font=f, fill=(255, 255, 255))
    d.text((150 + d.textlength("Water", font=f), 57), ".sim", font=ImageFont.truetype(FR, 37), fill=(255, 255, 255))
    ft = ImageFont.truetype(FR, 17); d.text((150, 102), t["tag"][0], font=ft, fill=LICHT); d.text((150, 123), t["tag"][1], font=ft, fill=LICHT)
    y = 168
    for i, (kop, sub) in enumerate(t["pijlers"]):
        k = (BLAUW, TEAL, ORANJE)[i]
        d.rounded_rectangle([44, y, 470, y + 58], radius=14, fill=(10, 36, 56), outline=k, width=2)
        d.ellipse([58, y + 19, 78, y + 39], fill=k)
        d.text((92, y + 18), kop, font=ImageFont.truetype(FM, 19), fill=(255, 255, 255), anchor="lm")
        d.text((92, y + 40), sub, font=ImageFont.truetype(FR, 14), fill=LICHT, anchor="lm"); y += 68
    d.text((44, y + 10), t["extra"], font=ImageFont.truetype(FR, 14), fill=LICHT)
    fl = ImageFont.truetype(FM, 14); x = 44; yy = y + 38
    for tl in ["NL", "EN"]:
        w = d.textlength(tl, font=fl) + 20; d.rounded_rectangle([x, yy, x + w, yy + 24], radius=12, outline=LICHT, width=1)
        d.text((x + w / 2, yy + 12), tl, font=fl, fill=(232, 242, 250), anchor="mm"); x += w + 6

    def tel(pad, sx, sy, s):
        p = Image.open(pad).convert("RGBA"); p = p.resize((int(p.width * s), int(p.height * s)), Image.LANCZOS)
        sch = Image.new("RGBA", (W, H), (0, 0, 0, 0)); m = Image.new("L", p.size, 0); m.paste(p.getchannel("A"))
        sch.paste((0, 0, 0, 150), (sx + 6, sy + 10), m)
        im.alpha_composite(sch.filter(ImageFilter.GaussianBlur(10))); im.alpha_composite(p.crop((0, 0, p.width, min(p.height, H - sy))), (sx, sy))
    tel(MEER, 520, 80, 0.40); tel(VLIEG, 720, 36, 0.40)
    os.makedirs(os.path.join(HIER, "header"), exist_ok=True)
    im.convert("RGB").save(os.path.join(HIER, f"header/googleplay_feature_graphic_1024x500_{taal}.png"))

    # ---------- zoekresultaat Google Play ----------
    os.makedirs(os.path.join(HIER, "zoekresultaat"), exist_ok=True)
    W2, H2 = 1080, 980; z = Image.new("RGB", (W2, H2), (255, 255, 255)); d = ImageDraw.Draw(z)
    d.rounded_rectangle([40, 30, W2 - 40, 110], radius=40, fill=(240, 242, 245)); d.text((90, 70), t["zoek"], font=ImageFont.truetype(FR, 34), fill=(60, 64, 67), anchor="lm")
    ic = rond(icoon.resize((150, 150), Image.LANCZOS), 34); z.paste(ic, (50, 150), ic)
    tekst(d, (230, 158), t["gp_titel"], ImageFont.truetype(FM, 38), (32, 33, 36))
    d.text((230, 210), "ConceptCraft", font=ImageFont.truetype(FR, 30), fill=(24, 128, 56))
    fr = ImageFont.truetype(FR, 28); x = 230; s1 = f"{t['cat']} · 4,7"
    d.text((x, 270), s1, font=fr, fill=(95, 99, 104), anchor="lm"); x += d.textlength(s1, font=fr) + 18
    ster(d, x, 268, 12, (95, 99, 104)); d.text((x + 20, 270), f" · {t['gratis']}", font=fr, fill=(95, 99, 104), anchor="lm")
    d.rounded_rectangle([W2 - 280, 170, W2 - 50, 240], radius=35, fill=(1, 135, 95))
    d.text((W2 - 165, 205), t["inst"], font=ImageFont.truetype(FM, 30), fill=(255, 255, 255), anchor="mm")
    fg = rond(im.resize((1000, 488), Image.LANCZOS), 24); z.paste(fg, (40, 330), fg)
    d.text((40, 850), t["gp_kort"], font=ImageFont.truetype(FR, 28 if taal == "nl" else 26), fill=(60, 64, 67))
    d.text((40, 900), t["kort"], font=ImageFont.truetype(FR, 22), fill=(150, 150, 150))
    z.save(os.path.join(HIER, f"zoekresultaat/googleplay_zoekresultaat_{taal}.png"))

    # ---------- zoekresultaat App Store (alleen als de screenshots in deze taal er zijn) ----------
    eerste = [os.path.join(HIER, f"appstore/appstore_6.9_{taal}_{k}.png") for k in (1, 2, 3)]
    if not all(os.path.exists(p) for p in eerste): print(f"{taal}: geen App Store-screenshots, zoekresultaat App Store overgeslagen"); continue
    W3, H3 = 1179, 1320; z = Image.new("RGB", (W3, H3), (255, 255, 255)); d = ImageDraw.Draw(z)
    d.rounded_rectangle([40, 40, W3 - 40, 130], radius=24, fill=(238, 238, 240)); d.text((90, 85), t["zoek"], font=ImageFont.truetype(FR, 40), fill=(60, 60, 67), anchor="lm")
    ic = rond(icoon.resize((190, 190), Image.LANCZOS), 42); z.paste(ic, (50, 180), ic)
    tekst(d, (270, 195), t["as_naam"], ImageFont.truetype(FM, 44), (0, 0, 0))
    d.text((270, 255), t["as_sub"], font=ImageFont.truetype(FR, 36), fill=(110, 110, 115))
    for i in range(5): ster(d, 288 + i * 36, 329, 15, (140, 140, 145))
    d.text((288 + 5 * 36, 329), "128", font=ImageFont.truetype(FR, 30), fill=(140, 140, 145), anchor="lm")
    d.rounded_rectangle([W3 - 230, 215, W3 - 60, 290], radius=38, fill=(236, 236, 240))
    d.text((W3 - 145, 252), t["krijg"], font=ImageFont.truetype(FM, 34), fill=(0, 122, 255), anchor="mm")
    for i, p in enumerate(eerste):
        s = rond(Image.open(p).convert("RGBA").resize((345, 750), Image.LANCZOS), 30); z.paste(s, (50 + i * 367, 410), s)
    d.text((50, 1200), t["noot1"], font=ImageFont.truetype(FR, 30), fill=(120, 120, 125))
    d.text((50, 1250), t["noot2"], font=ImageFont.truetype(FR, 30), fill=(120, 120, 125))
    z.save(os.path.join(HIER, f"zoekresultaat/appstore_zoekresultaat_{taal}.png"))
print("klaar")
