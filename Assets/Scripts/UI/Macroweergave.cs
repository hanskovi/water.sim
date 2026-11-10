using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WaterSim.Sim;

namespace WaterSim.UI
{
    /// <summary>
    /// Level 5, het glas: een ijsblokje in een vloeistof, drijvend of op een rots (landijs). Het deel onder het
    /// oppervlak komt uit <see cref="Drijven.DeelOnder"/>; bij smelten volgt het niveau <see cref="Drijven.Niveaustijging"/>.
    /// De stippellijn toont het niveau van vóór het smelten.
    /// </summary>
    public class Glasweergave : MaskableGraphic
    {
        public double rhoIjs = Constanten.DichtheidIjsBinas, rhoVloeistof = 1, rhoSmeltwater = 1;
        public bool opRots;
        [Range(0f, 1f)] public float gesmolten;
        public Color vloeistof = new Color(0.12f, 0.5f, 0.66f, 0.55f);
        public Color ijs = new Color(0.86f, 0.95f, 1f, 0.9f);
        public Color rots = new Color(0.42f, 0.38f, 0.34f, 1f);
        public TextMeshProUGUI lBoven, lOnder;

        void Update() { SetVerticesDirty(); PlaatsLabels(); }

        // vaste maten van het glas, het startniveau en het blokje
        void Maten(Rect r, out float gw, out float gh, out Vector2 voet, out float niveau0, out float s0)
        {
            gw = Mathf.Min(r.width * 0.46f, r.height * 0.5f);
            gh = gw * 1.35f;
            voet = new Vector2(r.center.x, r.yMin + Mathf.Max(10f, (r.height - gh) * 0.35f));
            niveau0 = voet.y + gh * (opRots ? 0.5f : 0.62f);
            s0 = gw * (opRots ? 0.34f : 0.4f);
        }

        float Breedte(float gw, float gh, Vector2 voet, float y) => 2f * Mathf.Lerp(gw * 0.42f, gw * 0.52f, (y - voet.y) / gh);

        float Niveau(float gw, float gh, Vector2 voet, float niveau0, float s0) =>
            niveau0 + (float)Drijven.Niveaustijging(s0 * s0, Breedte(gw, gh, voet, niveau0), rhoIjs, rhoVloeistof, rhoSmeltwater, !opRots, gesmolten);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (r.width < 40f || r.height < 60f) return;
            Maten(r, out float gw, out float gh, out var voet, out float niveau0, out float s0);
            float onderB = gw * 0.42f, bovenB = gw * 0.52f;
            Vector2 lo = voet + new Vector2(-onderB, 0f), ro = voet + new Vector2(onderB, 0f);
            Vector2 lb = voet + new Vector2(-bovenB, gh), rb = voet + new Vector2(bovenB, gh);
            float niveau = Niveau(gw, gh, voet, niveau0, s0);
            float t = Time.time;
            double deel = Drijven.DeelOnder(rhoIjs, rhoVloeistof);
            bool zinkt = !Drijven.Drijft(rhoIjs, rhoVloeistof);

            // rots (landijs): steekt boven het water uit
            float rotsTop = niveau0 + gh * 0.08f;
            if (opRots)
            {
                var rp = new[] { voet + new Vector2(-onderB * 0.75f, 2f), voet + new Vector2(onderB * 0.75f, 2f),
                                 new Vector2(voet.x + s0 * 0.6f, rotsTop), new Vector2(voet.x - s0 * 0.6f, rotsTop) };
                Vierhoek(vh, rp[0], rp[1], rp[2], rp[3], rots);
            }

            // ijsblokje (krimpt bij smelten; eerst tekenen, zodat de vloeistof er doorschijnend overheen valt)
            float s = s0 * Mathf.Sqrt(Mathf.Max(0f, 1f - gesmolten));
            if (s > 1f)
            {
                float y0 = opRots ? rotsTop : zinkt ? voet.y + 3f : niveau - (float)deel * s + 1.2f * Mathf.Sin(t * 1.3f);
                var m = new Vector2(voet.x + (opRots ? 0f : 2f * Mathf.Sin(t * 0.6f)), y0 + s * 0.5f);
                var rot = Quaternion.Euler(0f, 0f, opRots ? 0f : zinkt ? -3f : -5f + 2f * Mathf.Sin(t * 0.9f));
                var hoeken = new Vector2[4];
                for (int k = 0; k < 4; k++)
                {
                    var h = new Vector2((k == 0 || k == 3 ? -1 : 1) * s * 0.5f, (k < 2 ? -1 : 1) * s * 0.5f);
                    hoeken[k] = m + (Vector2)(rot * h);
                }
                Vierhoek(vh, hoeken[0], hoeken[1], hoeken[2], hoeken[3], ijs);
                Tekenhulp.Lijnstuk(vh, new[] { hoeken[0], hoeken[1], hoeken[2], hoeken[3], hoeken[0] }, 1.6f, new Color(1f, 1f, 1f, 0.9f), false);
                // smeltdruppels die van het blokje op de rots naar het water lopen
                if (opRots && gesmolten > 0f && gesmolten < 1f)
                    for (int k = 0; k < 3; k++)
                    {
                        float f = Mathf.Repeat(t * 0.8f + k / 3f, 1f);
                        Tekenhulp.Schijf(vh, new Vector2(voet.x + s0 * 0.62f, Mathf.Lerp(rotsTop, niveau, f)), 2f, new Color(0.6f, 0.85f, 1f, 1f), 8);
                    }
            }

            // vloeistof tot het (nieuwe) niveau, en een stippellijn op het oude niveau
            float wn = Breedte(gw, gh, voet, niveau) * 0.5f;
            Vierhoek(vh, lo + new Vector2(2f, 2f), ro + new Vector2(-2f, 2f), new Vector2(voet.x + wn - 2f, niveau), new Vector2(voet.x - wn + 2f, niveau), vloeistof);
            if (opRots) Vierhoek(vh, voet + new Vector2(-onderB * 0.75f, 2f), voet + new Vector2(onderB * 0.75f, 2f),
                                 new Vector2(voet.x + s0 * 0.6f, Mathf.Min(rotsTop, niveau)), new Vector2(voet.x - s0 * 0.6f, Mathf.Min(rotsTop, niveau)), rots);
            Tekenhulp.Lijn(vh, new Vector2(voet.x - wn + 2f, niveau), new Vector2(voet.x + wn - 2f, niveau), 2f, new Color(0.65f, 0.9f, 0.97f, 1f));
            if (gesmolten > 0f)
            {
                float w0 = Breedte(gw, gh, voet, niveau0) * 0.5f + 10f;
                for (float x = voet.x - w0; x < voet.x + w0; x += 8f)
                    Tekenhulp.Lijn(vh, new Vector2(x, niveau0), new Vector2(Mathf.Min(x + 4f, voet.x + w0), niveau0), 1.2f, new Color(1f, 1f, 1f, 0.6f));
            }

            // glas
            Tekenhulp.Lijnstuk(vh, new[] { lb, lo, ro, rb }, 2.5f, new Color(1f, 1f, 1f, 0.6f), true);
        }

        void PlaatsLabels()
        {
            var r = rectTransform.rect;
            if (r.width < 40f) return;
            Maten(r, out float gw, out float gh, out var voet, out float niveau0, out float s0);
            float niveau = Niveau(gw, gh, voet, niveau0, s0), x = voet.x + gw * 0.56f;
            float s = s0 * Mathf.Sqrt(Mathf.Max(0f, 1f - gesmolten));
            double deel = Drijven.DeelOnder(rhoIjs, rhoVloeistof);
            bool zinkt = !Drijven.Drijft(rhoIjs, rhoVloeistof);
            void Zet(TextMeshProUGUI l, float y)
            {
                if (l == null) return;
                l.rectTransform.anchorMin = l.rectTransform.anchorMax = Vector2.zero;
                l.rectTransform.pivot = new Vector2(0f, 0.5f);
                l.rectTransform.anchoredPosition = new Vector2(x - r.xMin, y - r.yMin);
            }
            Zet(lBoven, niveau + Mathf.Max(8f, (1f - (float)deel) * s * 0.5f) + 4f);
            Zet(lOnder, zinkt ? voet.y + s * 0.5f : niveau - (float)deel * s * 0.5f);
        }

        static void Vierhoek(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color k)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, k, Vector4.zero); vh.AddVert(b, k, Vector4.zero); vh.AddVert(c, k, Vector4.zero); vh.AddVert(d, k, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    /// <summary>
    /// Rond inzoomvenster: ijs (moleculen in zeshoeken, open) of vloeibaar water (moleculen door elkaar).
    /// IJs in bovenaanzicht langs de c-as: elk molecuul heeft 3 buren in het vlak en 1 erboven of eronder.
    /// Elke O heeft 2 H; op elke binding in het vlak zit precies één H. Bij de helft van de moleculen wijst
    /// één H naar de kijker (de binding langs de c-as): die H ligt over de O heen.
    /// </summary>
    public class Inzoom : MaskableGraphic
    {
        public bool water;
        public Color rand = new Color(0.95f, 0.54f, 0.33f, 1f);
        public Color achter = new Color(0.03f, 0.08f, 0.13f, 0.95f);
        public Color zuurstof = new Color(0.88f, 0.35f, 0.29f, 1f);
        public Color brug = new Color(0.36f, 0.76f, 1f, 1f);

        struct Mol { public Vector2 p; public float hoek, snel; }
        List<Mol> mols;

        void Update() { if (water) SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float R = Mathf.Min(r.width, r.height) * 0.5f - 2f;
            if (R < 10f) return;
            var m = r.center;
            Tekenhulp.Schijf(vh, m, R, achter, 40);
            if (water) Water(vh, m, R); else IJs(vh, m, R);
            // rand (ring)
            var punten = new List<Vector2>();
            for (int k = 0; k <= 48; k++) { float a = k * Mathf.PI * 2f / 48f; punten.Add(m + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R); }
            Tekenhulp.Lijnstuk(vh, punten, 2.2f, rand, false);
        }

        void IJs(VertexHelper vh, Vector2 m, float R)
        {
            float b = R * 0.26f, rO = b * 0.24f, rH = b * 0.13f;
            float wx = Mathf.Sqrt(3f) * b;
            var dA = new Vector2(0f, b);                  // A → B recht erboven
            var dB = new Vector2(wx * 0.5f, -b * 0.5f);   // B-buren van A schuin onder
            var dC = new Vector2(-wx * 0.5f, -b * 0.5f);
            bool Binnen(Vector2 p) => (p - m).sqrMagnitude < (R - rO - 1f) * (R - rO - 1f);
            var wit = Color.white;
            var aa = new List<Vector2>();
            for (int n = -6; n <= 6; n++)
                for (int k = -6; k <= 6; k++) aa.Add(m + new Vector2(n * wx + k * wx * 0.5f, k * 1.5f * b));

            // 1. bindingen: streepje van O naar zijn H, stippels van H naar de ontvangende O
            foreach (var a in aa)
            {
                var bo = a + dA;
                if (Binnen(a)) Donor(vh, a, bo, Binnen(bo), wit, rH);                 // A geeft één H aan B erboven
                if (Binnen(bo))
                {
                    Donor(vh, bo, bo - dB, Binnen(bo - dB), wit, rH);                    // B geeft twee H schuin omhoog
                    Donor(vh, bo, bo - dC, Binnen(bo - dC), wit, rH);
                }
            }
            // 2. atomen
            foreach (var a in aa)
            {
                var bo = a + dA;
                if (Binnen(a))
                {
                    Tekenhulp.Schijf(vh, a, rO, zuurstof, 14);
                    Tekenhulp.Schijf(vh, a + dA * 0.36f, rH, wit, 10);
                    Tekenhulp.Schijf(vh, a + new Vector2(rO * 0.45f, rO * 0.45f), rH, wit, 10);   // tweede H wijst naar de kijker
                }
                if (Binnen(bo))
                {
                    Tekenhulp.Schijf(vh, bo, rO, zuurstof, 14);
                    Tekenhulp.Schijf(vh, bo - dB * 0.36f, rH, wit, 10);
                    Tekenhulp.Schijf(vh, bo - dC * 0.36f, rH, wit, 10);
                }
            }
        }

        void Donor(VertexHelper vh, Vector2 o, Vector2 ontvanger, bool brugZichtbaar, Color wit, float rH)
        {
            var h = Vector2.Lerp(o, ontvanger, 0.36f);
            Tekenhulp.Lijn(vh, o, h, 1.4f, wit);
            if (brugZichtbaar) Stippen(vh, h, ontvanger, rH * 0.7f);
        }

        void Stippen(VertexHelper vh, Vector2 a, Vector2 b, float r)
        {
            for (int k = 1; k <= 3; k++) Tekenhulp.Schijf(vh, Vector2.Lerp(a, b, k / 4f), r, brug, 8);
        }

        void Water(VertexHelper vh, Vector2 m, float R)
        {
            float b = R * 0.26f, rO = b * 0.24f, rH = b * 0.13f;
            if (mols == null)
            {
                mols = new List<Mol>();
                var rnd = new System.Random(7);
                float stap = b * 1.12f;                                // iets dichter op elkaar dan in ijs
                for (float y = -R; y <= R; y += stap)
                    for (float x = -R; x <= R; x += stap)
                    {
                        var p = new Vector2(x + (float)(rnd.NextDouble() - 0.5) * stap * 0.7f, y + (float)(rnd.NextDouble() - 0.5) * stap * 0.7f);
                        if (p.magnitude > R - rO - 2f) continue;
                        mols.Add(new Mol { p = p, hoek = (float)rnd.NextDouble() * 360f, snel = (float)(rnd.NextDouble() - 0.5) * 60f });
                    }
            }
            float t = Time.time, half = (float)(Constanten.HoekHOH * 0.5) * Mathf.Deg2Rad;
            foreach (var mo in mols)
            {
                var p = m + mo.p + new Vector2(Mathf.Sin(t * 1.7f + mo.hoek), Mathf.Cos(t * 1.3f + mo.snel)) * b * 0.08f;
                float a = (mo.hoek + mo.snel * t) * Mathf.Deg2Rad;
                for (int k = -1; k <= 1; k += 2)
                {
                    float ah = a + k * half;
                    var h = p + new Vector2(Mathf.Cos(ah), Mathf.Sin(ah)) * b * 0.36f;
                    Tekenhulp.Lijn(vh, p, h, 1.4f, Color.white);
                    Tekenhulp.Schijf(vh, h, rH, Color.white, 10);
                }
                Tekenhulp.Schijf(vh, p, rO, zuurstof, 14);
            }
        }
    }

    /// <summary>
    /// Level 5, het meer: een doorsnede met de temperatuur per laag (kleur), de ijslaag bovenop en een vis
    /// op de bodem. Leest alleen uit <see cref="Meer"/>.
    /// </summary>
    public class Meerweergave : MaskableGraphic
    {
        public Meer meer;
        public TextMeshProUGUI lIjs;
        public readonly List<(TextMeshProUGUI t, float diepte)> diepteLabels = new List<(TextMeshProUGUI, float)>();
        public Color koud = new Color(0.6f, 0.85f, 0.95f, 1f), vier = new Color(0.04f, 0.22f, 0.4f, 1f), warm = new Color(0.12f, 0.45f, 0.42f, 1f);
        public Color ijsKleur = new Color(0.9f, 0.97f, 1f, 0.95f), bodem = new Color(0.23f, 0.16f, 0.11f, 1f), vis = new Color(1f, 0.6f, 0.4f, 1f);
        public Color smeltwater = new Color(0.45f, 0.78f, 0.95f, 0.95f), zon = new Color(1f, 0.85f, 0.35f, 1f);

        void Update() { SetVerticesDirty(); PlaatsLabels(); }

        void Maten(Rect r, out float boven, out float onder, out float schaal)
        {
            boven = r.yMax - Mathf.Max(r.height * 0.36f, 150f);   // bovenin ruimte voor lucht, zon en het formulekaartje
            onder = r.yMin + r.height * 0.05f;
            schaal = meer != null ? (boven - onder) / (float)meer.Diepte : 1f;
        }

        public Color Kleur(double t)
        {
            if (t <= 4) return Color.Lerp(koud, vier, (float)(t / 4));
            return Color.Lerp(vier, warm, Mathf.Clamp01((float)(t - 4) / 12f));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (meer == null || r.width < 40f || r.height < 60f) return;
            Maten(r, out float boven, out float onder, out float schaal);
            float x0 = r.xMin + 6f, x1 = r.xMax - 6f;
            float dz = (float)(meer.Diepte / meer.Lagen) * schaal;
            for (int i = 0; i < meer.Lagen; i++)
            {
                float y1 = boven - i * dz, y2 = boven - (i + 1) * dz - 0.5f;
                Blok(vh, x0, y2, x1, y1, Kleur(meer.T[i]));
            }
            // ijs: 92 % onder het waterniveau, 8 % erboven
            float d = (float)meer.Ijsdikte * schaal;
            if (d > 0.3f)
            {
                Blok(vh, x0, boven - 0.92f * d, x1, boven + 0.08f * d, ijsKleur);
                Tekenhulp.Lijn(vh, new Vector2(x0, boven - 0.92f * d), new Vector2(x1, boven - 0.92f * d), 1f, new Color(0.7f, 0.85f, 0.95f, 1f));
                // smeltwater op het ijs (dooi): dun blauw laagje, iets aangedikt zodat je het ziet
                if (meer.Smeltwater > 0.0005)
                {
                    float sw = Mathf.Clamp((float)meer.Smeltwater * schaal * 3f, 2f, 7f);
                    Blok(vh, x0, boven + 0.08f * d, x1, boven + 0.08f * d + sw, smeltwater);
                }
            }
            else Tekenhulp.Lijn(vh, new Vector2(x0, boven), new Vector2(x1, boven), 2f, new Color(0.65f, 0.9f, 0.97f, 1f));
            float t = Time.time;
            // zon: groter en feller bij meer zonlicht
            if (meer.Zon > 1)
            {
                float sterkte = Mathf.Clamp01((float)meer.Zon / 230f);
                float ruimte = r.yMax - boven;
                var zm = new Vector2(x0 + 52f, boven + ruimte * 0.5f);
                Zonnetje.Teken(vh, zm, Mathf.Lerp(11f, 19f, sterkte), Mathf.Min(ruimte * 0.46f, 60f) * Mathf.Lerp(0.7f, 1f, sterkte), Mathf.Lerp(0.55f, 1f, sterkte), t);
            }

            // bodem (golvend) en een vis vlak boven de bodem
            var golf = new List<Vector2>();
            for (int k = 0; k <= 24; k++)
            {
                float x = Mathf.Lerp(x0, x1, k / 24f);
                golf.Add(new Vector2(x, onder + 4f * Mathf.Sin(k * 0.9f)));
            }
            for (int k = 0; k < golf.Count - 1; k++) Blok(vh, golf[k].x, r.yMin, golf[k + 1].x, Mathf.Max(golf[k].y, golf[k + 1].y), bodem);
            var v = new Vector2(Mathf.Lerp(x0 + 30f, x1 - 70f, 0.5f + 0.5f * Mathf.Sin(t * 0.25f)), onder + 0.06f * (boven - onder) + 3f * Mathf.Sin(t));
            int kant = Mathf.Cos(t * 0.25f) >= 0 ? 1 : -1;      // de vis zwemt de kant op waar hij heen gaat
            var neus = v + new Vector2(9f * kant, 0f);
            Driehoek(vh, v + new Vector2(-4f * kant, 4f), v + new Vector2(-4f * kant, -4f), neus, vis);
            Driehoek(vh, v + new Vector2(-4f * kant, 0f), v + new Vector2(-10f * kant, 4f), v + new Vector2(-10f * kant, -4f), vis);
        }

        void PlaatsLabels()
        {
            var r = rectTransform.rect;
            if (meer == null || r.width < 40f) return;
            Maten(r, out float boven, out float onder, out float schaal);
            foreach (var (t, diepte) in diepteLabels)
            {
                if (t == null) continue;
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = Vector2.zero;
                t.rectTransform.pivot = new Vector2(1f, 0.5f);
                t.rectTransform.anchoredPosition = new Vector2(r.width - 12f, boven - diepte * schaal - r.yMin);
            }
            if (lIjs != null)
            {
                lIjs.rectTransform.anchorMin = lIjs.rectTransform.anchorMax = Vector2.zero;
                lIjs.rectTransform.pivot = new Vector2(0f, 0f);
                float sw = meer.Smeltwater > 0.0005 ? Mathf.Clamp((float)meer.Smeltwater * schaal * 3f, 2f, 7f) : 0f;
                lIjs.rectTransform.anchoredPosition = new Vector2(12f, boven + 0.08f * (float)meer.Ijsdikte * schaal + sw + 5f - r.yMin);
            }
        }

        static void Blok(VertexHelper vh, float xa, float ya, float xb, float yb, Color k)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(xa, ya), k, Vector4.zero); vh.AddVert(new Vector3(xb, ya), k, Vector4.zero);
            vh.AddVert(new Vector3(xb, yb), k, Vector4.zero); vh.AddVert(new Vector3(xa, yb), k, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        static void Driehoek(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color k)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, k, Vector4.zero); vh.AddVert(b, k, Vector4.zero); vh.AddVert(c, k, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2);
        }
    }

    /// <summary>Zon met een gloeiende schijf (geel midden, oranje rand) en veel dunne stralen die langzaam draaien.</summary>
    static class Zonnetje
    {
        public static void Teken(VertexHelper vh, Vector2 m, float straal, float straalLengte, float dekking, float tijd)
        {
            var geel = new Color(1f, 0.93f, 0.35f, dekking);
            var oranje = new Color(1f, 0.67f, 0.25f, dekking);
            var weg = new Color(1f, 0.85f, 0.3f, 0f);
            // stralen: smalle driehoekjes, om en om lang en kort, naar buiten toe doorzichtig
            int n = 64;
            for (int k = 0; k < n; k++)
            {
                float a = (k + tijd * 0.4f) * Mathf.PI * 2f / n;
                float lang = (k % 2 == 0 ? 1f : 0.72f) * straalLengte;
                float breed = Mathf.PI * 2f / n * 0.32f;
                var basis1 = m + new Vector2(Mathf.Cos(a - breed), Mathf.Sin(a - breed)) * straal * 1.02f;
                var basis2 = m + new Vector2(Mathf.Cos(a + breed), Mathf.Sin(a + breed)) * straal * 1.02f;
                var punt = m + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (straal + lang);
                int i = vh.currentVertCount;
                vh.AddVert(basis1, geel, Vector4.zero); vh.AddVert(basis2, geel, Vector4.zero); vh.AddVert(punt, weg, Vector4.zero);
                vh.AddTriangle(i, i + 1, i + 2);
            }
            // schijf: geel in het midden, oranje aan de rand
            int start = vh.currentVertCount;
            vh.AddVert(m, geel, Vector4.zero);
            for (int k = 0; k <= 40; k++)
            {
                float a = k * Mathf.PI * 2f / 40f;
                vh.AddVert(m + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * straal, oranje, Vector4.zero);
                if (k > 0) vh.AddTriangle(start, start + k, start + k + 1);
            }
        }
    }

    /// <summary>
    /// Level 5, poolkappen: doorsnede met landijs op een continent (zoals Groenland) en drijvend zee-ijs (Noordpool).
    /// Bij smelten stijgt de zeespiegel alleen door het landijs. Hoogtes niet op schaal.
    /// </summary>
    public class Poolweergave : MaskableGraphic
    {
        [Range(0f, 1f)] public float gesmolten;
        public Color zee = new Color(0.08f, 0.35f, 0.55f, 0.85f), land = new Color(0.36f, 0.3f, 0.24f, 1f), ijs = new Color(0.9f, 0.97f, 1f, 0.95f);
        public TextMeshProUGUI lLand, lZee, lStijging;

        void Update() { SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (r.width < 60f || r.height < 80f) return;
            float x0 = r.xMin + 6f, x1 = r.xMax - 6f, bodem = r.yMin + 6f;
            float zee0 = r.yMin + r.height * 0.34f;
            float stijg = r.height * 0.07f * gesmolten;                 // overdreven: alleen landijs laat het stijgen
            float zeeNu = zee0 + stijg;
            float landRand = Mathf.Lerp(x0, x1, 0.46f), landTop = zee0 + r.height * 0.08f;

            // zee
            Blok(vh, x0, bodem, x1, zeeNu, zee);
            // land (continent) met een grillige kust
            var kust = new List<Vector2> { new Vector2(x0, bodem), new Vector2(landRand + 14f, bodem), new Vector2(landRand, landTop - 6f), new Vector2(landRand - 10f, landTop) , new Vector2(x0, landTop + 4f) };
            Veelhoek(vh, kust, land);
            // landijs: koepel, wordt lager bij smelten
            float koepel = r.height * 0.36f * (1f - gesmolten);
            if (koepel > 1f)
            {
                var k = new List<Vector2>();
                float a = x0 + 6f, b = landRand - 14f;
                for (int i = 0; i <= 24; i++)
                {
                    float f = i / 24f;
                    k.Add(new Vector2(Mathf.Lerp(a, b, f), landTop + koepel * Mathf.Pow(Mathf.Sin(f * Mathf.PI), 0.6f)));
                }
                k.Add(new Vector2(b, landTop)); k.Add(new Vector2(a, landTop));
                Veelhoek(vh, k, ijs);
            }
            // zee-ijs: drijvende schots (92 % onder water), wordt dunner maar het niveau verandert er niet door
            float dik = 14f * (1f - gesmolten);
            if (dik > 0.5f)
            {
                float a = Mathf.Lerp(x0, x1, 0.6f), b = Mathf.Lerp(x0, x1, 0.94f);
                Blok(vh, a, zeeNu - 0.92f * dik, b, zeeNu + 0.08f * dik, ijs);
            }
            // oude zeespiegel als stippellijn
            if (gesmolten > 0f)
                for (float x = x0; x < x1; x += 8f)
                    Tekenhulp.Lijn(vh, new Vector2(x, zee0), new Vector2(Mathf.Min(x + 4f, x1), zee0), 1.2f, new Color(1f, 1f, 1f, 0.6f));
            Tekenhulp.Lijn(vh, new Vector2(landRand - 4f, zeeNu), new Vector2(x1, zeeNu), 2f, new Color(0.65f, 0.9f, 0.97f, 1f));

            // labels
            Zet(lLand, new Vector2(Mathf.Lerp(x0, landRand, 0.45f), landTop + koepel + 14f), r, 0.5f);
            Zet(lZee, new Vector2(Mathf.Lerp(x0, x1, 0.77f), zeeNu + 22f), r, 0.5f);
            Zet(lStijging, new Vector2(x1 - 4f, zee0 - 16f), r, 1f);
        }

        static void Zet(TextMeshProUGUI l, Vector2 p, Rect r, float pivotX)
        {
            if (l == null) return;
            l.rectTransform.anchorMin = l.rectTransform.anchorMax = Vector2.zero;
            l.rectTransform.pivot = new Vector2(pivotX, 0.5f);
            l.rectTransform.anchoredPosition = p - r.min;
        }

        static void Blok(VertexHelper vh, float xa, float ya, float xb, float yb, Color k)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(xa, ya), k, Vector4.zero); vh.AddVert(new Vector3(xb, ya), k, Vector4.zero);
            vh.AddVert(new Vector3(xb, yb), k, Vector4.zero); vh.AddVert(new Vector3(xa, yb), k, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        // bolle veelhoek als waaier vanuit het zwaartepunt
        static void Veelhoek(VertexHelper vh, List<Vector2> p, Color k)
        {
            var m = Vector2.zero; foreach (var q in p) m += q; m /= p.Count;
            int s = vh.currentVertCount;
            vh.AddVert(m, k, Vector4.zero);
            foreach (var q in p) vh.AddVert(q, k, Vector4.zero);
            for (int i = 0; i < p.Count; i++) vh.AddTriangle(s, s + 1 + i, s + 1 + (i + 1) % p.Count);
        }
    }
}
