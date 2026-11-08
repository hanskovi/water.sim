using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WaterSim.UI
{
    /// <summary>
    /// Hulpfuncties om de interface in code op te bouwen (uGUI + TextMeshPro), overgenomen uit C.chem
    /// (CChemMenu): Maak, Vul, Hoek, LE, Vlak (afgeronde hoeken), Tekst en Knop, plus zelf getekende
    /// plaatjes (afgerond vlak, verloop, cirkel, ster, pijltjes, vinkje).
    /// </summary>
    public static class UIBouw
    {
        public static TMP_FontAsset Regular, Vet;

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static RectTransform Maak(string naam, Transform ouder)
        {
            var go = new GameObject(naam, typeof(RectTransform));
            go.transform.SetParent(ouder, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Vult de ouder, met marges (links, onder, rechts, boven).</summary>
        public static void Vul(RectTransform rt, float links = 0f, float onder = 0f, float rechts = 0f, float boven = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(links, onder);
            rt.offsetMax = new Vector2(-rechts, -boven);
        }

        public static void Hoek(RectTransform rt, Vector2 anker, Vector2 positie, Vector2 maat)
        {
            rt.anchorMin = rt.anchorMax = anker;
            rt.pivot = anker;
            rt.anchoredPosition = positie;
            rt.sizeDelta = maat;
        }

        public static LayoutElement LE(GameObject go, float prefW = -1f, float prefH = -1f, float flexW = -1f, float flexH = 0f, float minH = -1f)
        {
            if (!go.TryGetComponent<LayoutElement>(out var le)) le = go.AddComponent<LayoutElement>();
            le.preferredWidth = prefW;
            le.preferredHeight = prefH;
            le.flexibleWidth = flexW;
            le.flexibleHeight = flexH;
            le.minHeight = minH;
            return le;
        }

        public static Image Vlak(Transform ouder, string naam, Color kleur, float radius = 0f)
        {
            var img = Maak(naam, ouder).gameObject.AddComponent<Image>();
            img.color = kleur;
            if (radius > 0f)
            {
                img.sprite = RondSprite();
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = SpriteRadius / radius;
            }
            return img;
        }

        public static TextMeshProUGUI Tekst(Transform ouder, string naam, string tekst, float grootte, Color kleur,
                                            TextAlignmentOptions uitlijning = TextAlignmentOptions.Left, bool vet = false, bool afbreken = false)
        {
            var t = Maak(naam, ouder).gameObject.AddComponent<TextMeshProUGUI>();
            if ((vet ? Vet : Regular) != null) t.font = vet ? Vet : Regular;
            t.text = tekst;
            t.fontSize = grootte;
            t.color = kleur;
            t.alignment = uitlijning;
            t.richText = true;
            t.textWrappingMode = afbreken ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Knop(Image img, Action actie)
        {
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var kleuren = b.colors;
            kleuren.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            kleuren.highlightedColor = Color.white;
            kleuren.selectedColor = Color.white;
            b.colors = kleuren;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            if (actie != null) b.onClick.AddListener(() => actie());
            return b;
        }

        public static Image Icoon(Transform ouder, string naam, Sprite sprite, Color kleur)
        {
            var img = Maak(naam, ouder).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = kleur;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        // ---------- zelf getekende plaatjes ----------
        const int SpriteMaat = 64, SpriteRadius = 24;
        static Sprite rond, cirkel, ster, pijlLinks, pijlRechts, pijlOmlaag, pijlOmhoog, vink;
        static readonly Dictionary<string, Sprite> verlopen = new Dictionary<string, Sprite>();

        /// <summary>Wit afgerond vierkant (9-slice); kleur via Image.color, straal via pixelsPerUnitMultiplier.</summary>
        public static Sprite RondSprite()
        {
            if (rond != null) return rond;
            const int n = SpriteMaat, r = SpriteRadius;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float cx = x < r ? r : x >= n - r ? n - r : x + 0.5f;
                    float cy = y < r ? r : y >= n - r ? n - r : y + 0.5f;
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            var tex = Textuur(n, n, px);
            rond = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return rond;
        }

        /// <summary>Afgerond vlak met een horizontaal kleurverloop (zoals de geselecteerde tegel in C.chem).</summary>
        public static Sprite Verloop(Color links, Color rechts)
        {
            string sleutel = ColorUtility.ToHtmlStringRGBA(links) + ColorUtility.ToHtmlStringRGBA(rechts);
            if (verlopen.TryGetValue(sleutel, out var s) && s != null) return s;
            const int w = 128, h = 64, r = SpriteRadius;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float cx = x < r ? r : x >= w - r ? w - r : x + 0.5f;
                    float cy = y < r ? r : y >= h - r ? h - r : y + 0.5f;
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    var k = Color.Lerp(links, rechts, (x + 0.5f) / w);
                    k.a *= a;
                    px[y * w + x] = k;
                }
            s = Sprite.Create(Textuur(w, h, px), new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            verlopen[sleutel] = s;
            return s;
        }

        public static Sprite Cirkel() => cirkel != null ? cirkel : cirkel = Veelhoek(Rondje(40));
        public static Sprite Ster() => ster != null ? ster : ster = Veelhoek(Stervorm());
        public static Sprite PijlLinks() => pijlLinks != null ? pijlLinks : pijlLinks = Veelhoek(new[] { new Vector2(0.72f, 0.12f), new Vector2(0.72f, 0.88f), new Vector2(0.22f, 0.5f) });
        public static Sprite PijlRechts() => pijlRechts != null ? pijlRechts : pijlRechts = Veelhoek(new[] { new Vector2(0.28f, 0.12f), new Vector2(0.78f, 0.5f), new Vector2(0.28f, 0.88f) });
        public static Sprite PijlOmlaag() => pijlOmlaag != null ? pijlOmlaag : pijlOmlaag = Veelhoek(new[] { new Vector2(0.12f, 0.72f), new Vector2(0.88f, 0.72f), new Vector2(0.5f, 0.24f) });
        public static Sprite PijlOmhoog() => pijlOmhoog != null ? pijlOmhoog : pijlOmhoog = Veelhoek(new[] { new Vector2(0.12f, 0.28f), new Vector2(0.5f, 0.76f), new Vector2(0.88f, 0.28f) });
        public static Sprite Vink() => vink != null ? vink : vink = Lijn(0.13f, new Vector2(0.12f, 0.52f), new Vector2(0.40f, 0.22f), new Vector2(0.90f, 0.82f));

        static Vector2[] Rondje(int n)
        {
            var p = new Vector2[n];
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2f / n; p[i] = new Vector2(0.5f + 0.48f * Mathf.Cos(a), 0.5f + 0.48f * Mathf.Sin(a)); }
            return p;
        }

        static Vector2[] Stervorm()
        {
            var p = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f, r = i % 2 == 0 ? 0.48f : 0.2f;
                p[i] = new Vector2(0.5f + r * Mathf.Cos(a), 0.47f + r * Mathf.Sin(a));
            }
            return p;
        }

        // gevulde veelhoek (punten in 0..1), 4× overbemonsterd voor zachte randen
        static Sprite Veelhoek(Vector2[] pts)
        {
            const int n = 96;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int raak = 0;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                            if (Binnen(pts, new Vector2((x + (sx + 0.5f) / 4f) / n, (y + (sy + 0.5f) / 4f) / n))) raak++;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(raak * 255 / 16));
                }
            return Sprite.Create(Textuur(n, n, px), new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        static bool Binnen(Vector2[] p, Vector2 q)
        {
            bool binnen = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                if ((p[i].y > q.y) != (p[j].y > q.y) && q.x < (p[j].x - p[i].x) * (q.y - p[i].y) / (p[j].y - p[i].y) + p[i].x) binnen = !binnen;
            return binnen;
        }

        // witte polylijn met ronde uiteinden (punten in 0..1)
        static Sprite Lijn(float dikte, params Vector2[] punten)
        {
            const int n = 128;
            var px = new Color32[n * n];
            float half = dikte * n / 2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = float.MaxValue;
                    for (int i = 0; i + 1 < punten.Length; i++)
                    {
                        Vector2 a = punten[i] * n, b = punten[i + 1] * n, ab = b - a;
                        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                        d = Mathf.Min(d, Vector2.Distance(p, a + ab * t));
                    }
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(half - d + 0.5f) * 255f));
                }
            return Sprite.Create(Textuur(n, n, px), new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        static Texture2D Textuur(int w, int h, Color32[] px)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
