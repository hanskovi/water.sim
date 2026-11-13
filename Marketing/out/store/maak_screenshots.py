# Store-screenshots Water.sim: schermen/scherm_N.png (telefoon in kader, doorzichtige achtergrond) → App Store 6,9" en Google Play.
# Gebruik: python3 maak_screenshots.py   (koppen per taal hieronder aanpassen)
# Nieuw scherm: python3 prep_scherm.py ../../in/naam_nl.png schermen/scherm_N.png
# Engelse reeks: zet de app op EN, maak dezelfde schermen, en zet ze in schermen_en/ (dezelfde nummers).
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import os
HIER = os.path.dirname(os.path.abspath(__file__))
FM = os.path.join(HIER, "../../../Assets/Fonts/Ubuntu/Ubuntu-Medium.ttf")
FR = os.path.join(HIER, "../../../Assets/Fonts/Ubuntu/Ubuntu-Regular.ttf")
TEKSTEN = {
 "nl": [("Vlieg door ijs", "molecuul voor molecuul, met elke waterstofbrug"),
        ("Waarom drijft ijs?", "de open honingraat van ijs: 0,917 g/cm³"),
        ("Een meertje door het jaar", "11 plaatsen op aarde · wanneer kun je schaatsen?"),
        ("Smelten en stollen", "bij 4 °C is water het zwaarst"),
        ("De waterstofbrug", "δ+ trekt aan δ−, in 3D"),
        ("Teken zelf waterstofbruggen", "quiz met streaks · 3 niveaus"),
        ("En er komt meer", "Mpemba, onderkoeling, sneeuwvlokken …")],
 # Engelse reeks: deels andere schermen dan de Nederlandse (level 1 en kalotten in plaats van water bij 4 °C en Ontdek)
 "en": [("Fly through ice", "molecule by molecule, with every hydrogen bond"),
        ("Why does ice float?", "the open honeycomb of ice: 0.917 g/cm³"),
        ("A pond through the year", "11 places on Earth · when can you go skating?"),
        ("The water molecule", "104.5°, δ+ and δ−: why water is a dipole"),
        ("The hydrogen bond", "δ+ attracts δ−, in 3D"),
        ("Draw hydrogen bonds yourself", "with instant feedback · quiz with streaks"),
        ("Space-filling or ball-and-stick", "from −20 to +80 °C")]}
IJS = {1, 2, 4}          # ijs en water: koele blauwe gloed
MEER = {3}               # meertje: blauw-groen
QUIZ = {6}               # quiz: warme oranje gloed (de oranje knoppen van de app)
# App Store Connect (nov 2026): "iPhone with Dynamic Island (medium display)" vraagt 1206 × 2622 (of 1179 × 2556);
# de 6,9"-reeks blijft erbij voor als dat vak terugkomt
FORMATEN = {"appstore_6.3": (1206, 2622), "appstore_6.9": (1320, 2868), "googleplay": (1080, 1920)}


def kleuren(k):
    if k in QUIZ: return (8, 22, 34), (40, 30, 30), (242, 138, 85, 60), (255, 200, 160)
    if k in MEER: return (6, 24, 34), (10, 52, 60), (90, 210, 200, 55), (170, 230, 225)
    if k in IJS:  return (6, 18, 34), (10, 44, 78), (91, 194, 255, 65), (185, 220, 245)
    return (8, 20, 32), (14, 40, 62), (120, 170, 255, 55), (190, 215, 240)


def achtergrond(W, H, k):
    top, bot, gloed, _ = kleuren(k)
    bg = Image.new("RGBA", (W, H)); d = ImageDraw.Draw(bg)
    for y in range(H):
        t = y / H; d.line([(0, y), (W, y)], fill=tuple(int(top[i] + (bot[i] - top[i]) * t) for i in range(3)) + (255,))
    gl = Image.new("RGBA", (W, H), (0, 0, 0, 0)); ImageDraw.Draw(gl).ellipse([W * 0.1, H * 0.25, W * 0.9, H * 0.95], fill=gloed)
    bg.alpha_composite(gl.filter(ImageFilter.GaussianBlur(W * 0.12)))
    return bg


def maak(taal, naam, W, H, k, kop, sub, map_schermen):
    im = achtergrond(W, H, k); d = ImageDraw.Draw(im)
    fk = ImageFont.truetype(FM, int(W * 0.072)); fs = ImageFont.truetype(FR, int(W * 0.04))
    while d.textlength(kop, font=fk) > W * 0.9: fk = ImageFont.truetype(FM, fk.size - 2)
    while d.textlength(sub, font=fs) > W * 0.92: fs = ImageFont.truetype(FR, fs.size - 2)
    y0 = int(H * 0.055); d.text((W / 2, y0), kop, font=fk, fill=(255, 255, 255), anchor="mt")
    d.text((W / 2, y0 + fk.size * 1.25), sub, font=fs, fill=kleuren(k)[3], anchor="mt")
    tel = Image.open(os.path.join(map_schermen, f"scherm_{k}.png")).convert("RGBA")
    top = y0 + fk.size * 1.25 + fs.size * 1.6; s = min((H - top - H * 0.03) / tel.height, W * 0.86 / tel.width)
    tel = tel.resize((int(tel.width * s), int(tel.height * s)), Image.LANCZOS)
    x = (W - tel.width) // 2; y = int(top)
    sch = Image.new("RGBA", (W, H), (0, 0, 0, 0)); m = Image.new("L", tel.size, 0); m.paste(tel.getchannel("A"))
    sch.paste((0, 0, 0, 150), (x + int(W * 0.01), y + int(W * 0.02)), m); im.alpha_composite(sch.filter(ImageFilter.GaussianBlur(W * 0.02)))
    im.alpha_composite(tel, (x, y))
    map_ = "appstore" if naam.startswith("app") else "googleplay"; os.makedirs(os.path.join(HIER, map_), exist_ok=True)
    im.convert("RGB").save(os.path.join(HIER, map_, f"{naam}_{taal}_{k}.png"))


def reeks(taal, naam, W, H):
    """Overzicht van de hele reeks op één vel (om te bekijken, niet om te uploaden)."""
    map_ = "appstore" if naam.startswith("app") else "googleplay"
    stukken = [Image.open(os.path.join(HIER, map_, f"{naam}_{taal}_{k}.png")) for k in range(1, 8)
               if os.path.exists(os.path.join(HIER, map_, f"{naam}_{taal}_{k}.png"))]
    if not stukken: return
    h = 700; w = int(W * h / H); v = Image.new("RGB", (len(stukken) * (w + 20) + 20, h + 40), (230, 234, 238))
    for i, s in enumerate(stukken): v.paste(s.resize((w, h), Image.LANCZOS), (20 + i * (w + 20), 20))
    v.save(os.path.join(HIER, map_, f"{naam}_{taal}_reeks.png"))


if __name__ == "__main__":
    for taal, lijst in TEKSTEN.items():
        schermen = os.path.join(HIER, "schermen" if taal == "nl" else "schermen_" + taal)
        if not os.path.isdir(schermen): print(f"{taal}: geen map {os.path.basename(schermen)}, overgeslagen"); continue
        for naam, (W, H) in FORMATEN.items():
            for k, (kop, sub) in enumerate(lijst, 1):
                if os.path.exists(os.path.join(schermen, f"scherm_{k}.png")): maak(taal, naam, W, H, k, kop, sub, schermen)
            reeks(taal, naam, W, H)
    print("klaar")
