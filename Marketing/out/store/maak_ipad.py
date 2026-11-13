# App Store-screenshots voor iPad 13": schermen_ipad/scherm_N.png (iPad in kader, doorzichtige achtergrond) →
# staand 2064 × 2752 of liggend 2752 × 2064 (Apple accepteert beide in één reeks). Zelfde stijl als maak_screenshots.py.
# Gebruik: python3 maak_ipad.py   (nieuw scherm: python3 prep_scherm.py ../../in/naam_nl.png schermen_ipad/scherm_N.png)
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import os
import maak_screenshots as M

HIER = os.path.dirname(os.path.abspath(__file__))
TEKSTEN = {
 "nl": [("Vlieg door ijs", "molecuul voor molecuul, met elke waterstofbrug", 1),
        ("Het ijskristal", "elk molecuul aan vier buren · een honingraat met lege kanalen", 2),
        ("Een meertje door het jaar", "11 plaatsen op aarde · wanneer kun je schaatsen?", 3),
        ("De waterstofbrug", "δ+ trekt aan δ−, in 3D", 5)],
 # Engelse reeks: andere schermen dan de Nederlandse (level 3 vliegen, level 4 ijsblok, meertje, H-brug)
 "en": [("Fly through ice", "molecule by molecule, with every hydrogen bond", 1),
        ("Why does ice float?", "the open honeycomb of ice: 0.917 g/cm³", 2),
        ("A pond through the year", "11 places on Earth · when can you go skating?", 3),
        ("The hydrogen bond", "δ+ attracts δ−, in 3D", 5)]}
STAAND, LIGGEND = (2064, 2752), (2752, 2064)


def maak(taal, k, kop, sub, kleur, schermen):
    tab = Image.open(os.path.join(schermen, f"scherm_{k}.png")).convert("RGBA")
    W, H = LIGGEND if tab.width > tab.height else STAAND
    im = M.achtergrond(W, H, kleur); d = ImageDraw.Draw(im)
    basis = min(W, H)
    fk = ImageFont.truetype(M.FM, int(basis * 0.06)); fs = ImageFont.truetype(M.FR, int(basis * 0.032))
    while d.textlength(kop, font=fk) > W * 0.9: fk = ImageFont.truetype(M.FM, fk.size - 2)
    while d.textlength(sub, font=fs) > W * 0.92: fs = ImageFont.truetype(M.FR, fs.size - 2)
    y0 = int(H * 0.05); d.text((W / 2, y0), kop, font=fk, fill=(255, 255, 255), anchor="mt")
    d.text((W / 2, y0 + fk.size * 1.25), sub, font=fs, fill=M.kleuren(kleur)[3], anchor="mt")
    top = y0 + fk.size * 1.25 + fs.size * 1.7
    s = min((H - top - H * 0.035) / tab.height, W * 0.88 / tab.width)
    tab = tab.resize((int(tab.width * s), int(tab.height * s)), Image.LANCZOS)
    x = (W - tab.width) // 2; y = int(top)
    sch = Image.new("RGBA", (W, H), (0, 0, 0, 0)); m = Image.new("L", tab.size, 0); m.paste(tab.getchannel("A"))
    sch.paste((0, 0, 0, 150), (x + int(basis * 0.01), y + int(basis * 0.02)), m)
    im.alpha_composite(sch.filter(ImageFilter.GaussianBlur(basis * 0.02)))
    im.alpha_composite(tab, (x, y))
    os.makedirs(os.path.join(HIER, "appstore"), exist_ok=True)
    pad = os.path.join(HIER, "appstore", f"appstore_ipad13_{taal}_{k}.png")
    im.convert("RGB").save(pad)
    return pad


if __name__ == "__main__":
    for taal, lijst in TEKSTEN.items():
        schermen = os.path.join(HIER, "schermen_ipad" if taal == "nl" else "schermen_ipad_" + taal)
        if not os.path.isdir(schermen): print(f"{taal}: geen map {os.path.basename(schermen)}, overgeslagen"); continue
        paden = [maak(taal, k, kop, sub, kleur, schermen) for k, (kop, sub, kleur) in enumerate(lijst, 1)
                 if os.path.exists(os.path.join(schermen, f"scherm_{k}.png"))]
        # overzicht (niet uploaden)
        h = 700; stukken = [Image.open(p) for p in paden]
        v = Image.new("RGB", (sum(int(s.width * h / s.height) + 20 for s in stukken) + 20, h + 40), (230, 234, 238)); x = 20
        for s in stukken:
            w = int(s.width * h / s.height); v.paste(s.resize((w, h), Image.LANCZOS), (x, 20)); x += w + 20
        v.save(os.path.join(HIER, "appstore", f"appstore_ipad13_{taal}_reeks.png"))
    print("klaar")
