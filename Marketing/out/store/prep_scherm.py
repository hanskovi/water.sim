# Scherm uit de Unity-simulator klaarmaken voor de store: de advertentiestrook onderin weg (opgevuld met het beeld erboven,
# naar onder iets donkerder), en het grijs rond de telefoon doorzichtig. Werkt voor lichte en donkere banners en de testbanner.
# Gebruik: python3 prep_scherm.py bron.png doel.png
import sys
from PIL import Image, ImageDraw, ImageFilter
src, doel = sys.argv[1], sys.argv[2]
im = Image.open(src).convert("RGBA"); W, H = im.size; px = im.load()
def verschil(a, b): return sum(abs(a[i] - b[i]) for i in range(3))
# 1. de strook: kleur links in de strook (net binnen de telefoon), en vanaf daar omhoog tot het beeld verandert
xr = int(W * 0.15); yb = H - 60
band = px[xr, yb]
y = yb
while y > H * 0.75 and verschil(px[xr, y], band) < 12: y -= 1
top = y + 1
# 2. opvullen: per rij het hele scherm (tot de zwarte rand van de telefoon, ook in de afgeronde hoeken), met per kolom
#    de kleur net boven de strook (horizontaal vervaagd), naar onder 45 % donkerder
def zwart(x, y): p = px[x, y]; return p[3] > 0 and p[0] + p[1] + p[2] < 20
rij = im.crop((0, top - 4, W, top - 1)).resize((W, 1)).filter(ImageFilter.BoxBlur(25))
for yy in range(top, H):
    if zwart(W // 2, yy): break                                     # de onderrand van de telefoon
    xl = W // 2
    while xl > 3 and not (zwart(xl - 1, yy) and zwart(xl - 2, yy) and zwart(xl - 3, yy)): xl -= 1
    xh = W // 2
    while xh < W - 4 and not (zwart(xh + 1, yy) and zwart(xh + 2, yy) and zwart(xh + 3, yy)): xh += 1
    f = 1 - 0.45 * (yy - top) / max(1, H - top)
    for x in range(xl, xh + 1):
        k = rij.getpixel((x, 0))
        px[x, yy] = (int(k[0] * f), int(k[1] * f), int(k[2] * f), 255)
print(f"strook weg vanaf y {top}")
# 3. grijs rond de telefoon doorzichtig (vanuit de hoeken)
grijs = px[2, 2][:3]
for (x, y) in [(0, 0), (W - 1, 0), (0, H - 1), (W - 1, H - 1)]:
    ImageDraw.floodfill(im, (x, y), (grijs[0], grijs[1], grijs[2], 0), thresh=10)
im.save(doel)
