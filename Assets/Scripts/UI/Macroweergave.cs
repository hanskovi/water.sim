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
        public TextMeshProUGUI lIjs, lBodem;
        public readonly List<(TextMeshProUGUI t, float diepte)> diepteLabels = new List<(TextMeshProUGUI, float)>();
        public Color koud = new Color(0.6f, 0.85f, 0.95f, 1f), vier = new Color(0.04f, 0.22f, 0.4f, 1f), warm = new Color(0.12f, 0.45f, 0.42f, 1f);
        public Color ijsKleur = new Color(0.9f, 0.97f, 1f, 0.95f), bodem = new Color(0.23f, 0.16f, 0.11f, 1f), vis = new Color(1f, 0.6f, 0.4f, 1f);
        public Color smeltwater = new Color(0.45f, 0.78f, 0.95f, 0.95f), zon = new Color(1f, 0.85f, 0.35f, 1f);

        void Update() { SetVerticesDirty(); PlaatsLabels(); }

        void Maten(Rect r, out float boven, out float onder, out float schaal)
        {
            boven = r.yMax - Mathf.Max(r.height * 0.46f, 195f);   // bovenin ruimte voor lucht en datum, zon, ijslabel, schaatsers en formulekaartje
            onder = r.yMin + r.height * 0.05f;
            schaal = meer != null ? (boven - onder) / (float)meer.Diepte : 1f;
        }

        // ---------- schaatsers ----------
        const float Schaatsruimte = 24f;          // hoogte boven het ijs voor de schaatsers (het ijslabel staat erboven)
        const int MaxSchaatsers = 10;
        readonly float[] zicht = new float[MaxSchaatsers];
        float laatsteTijd;

        /// <summary>True als er ijs ligt waar niemand op mag (te dun, of rot door de dooi): dan staat het gevarenbord.</summary>
        public bool Gevaarlijk { get; private set; }

        /// <summary>
        /// Schaatsers volgens de ijsdikte (richtlijn): &lt; 5 cm niemand (gevarenbord), 5–10 cm één voorzichtige schaatser,
        /// 10–15 cm een paar, vanaf 15 cm een toertocht in een lint. Bij dooi is ijs minder sterk: het wordt "rot"
        /// (zie Meer.Rot). Dan gelden de grenzen hoger: 3 cm bij beginnende dooi, tot 15 cm bij helemaal rot ijs.
        /// </summary>
        void Schaatsers(VertexHelper vh, float x0, float x1, float ijs, float t)
        {
            float dt = Mathf.Clamp(t - laatsteTijd, 0f, 0.1f); laatsteTijd = t;
            double cm = meer.Ijsdikte * 100;
            var tst = meer.Toestand;
            bool dooi = meer.Smeltwater > 0.0005 || tst == Meer.IJstoestand.SmeltBoven || tst == Meer.IJstoestand.SmeltOnder || tst == Meer.IJstoestand.SmeltBeide;
            double nodig = System.Math.Max(dooi ? 3 : 0, 15 * meer.Rot);    // extra marge voor zwak (rot) ijs
            bool toertocht = cm >= 15 + nodig;
            int aantal = cm < 5 + nodig ? 0 : cm < 10 + nodig ? 1 : cm < 15 + nodig ? 3 : 9;
            Gevaarlijk = meer.Ijsdikte > 0.003 && aantal == 0;
            if (Gevaarlijk) Gevarenbord(vh, new Vector2(x1 - 22f, ijs));
            Doorzakker(vh, x0, x1, ijs, t);
            float breed = x1 - x0;
            for (int i = 0; i < MaxSchaatsers; i++)
            {
                zicht[i] = Mathf.MoveTowards(zicht[i], i < aantal ? 1f : 0f, dt * 1.5f);   // rustig het ijs op en af
                if (zicht[i] <= 0.01f) continue;
                float x; int kant;
                if (toertocht)
                {
                    // lint achter elkaar, van links naar rechts, rond
                    float lus = breed + 60f;
                    x = x0 - 30f + Mathf.Repeat(t * 26f + (MaxSchaatsers - i) * 17f, lus);
                    kant = 1;
                }
                else
                {
                    // heen en weer, elk in zijn eigen tempo; de eerste (op dun ijs) heel voorzichtig
                    float v = aantal == 1 ? 10f : 16f + 6f * i, L = breed - 60f;
                    float f = Mathf.PingPong(t * v / L + i * 0.37f, 1f);
                    x = x0 + 30f + f * L;
                    kant = Mathf.PingPong(t * v / L + i * 0.37f + 0.001f, 1f) > f ? 1 : -1;
                }
                if (x < x0 + 6f || x > x1 - 6f) continue;
                Schaatser(vh, new Vector2(x, ijs), kant, t * (toertocht ? 3.2f : 2.6f) + i * 1.3f, toertocht, zicht[i], toertocht && i == 0);
            }
        }

        // plat figuurtje in de stijl van de vis: voorovergebogen, om en om afzetten
        void Schaatser(VertexHelper vh, Vector2 voet, int kant, float fase, bool handenOpRug, float zichtbaar, bool vlag)
        {
            var k = vis; k.a *= zichtbaar;
            var ijzer = new Color(0.1f, 0.15f, 0.2f, zichtbaar);
            float s = Mathf.Sin(fase);
            var heup = voet + new Vector2(kant * 1f, 9f);
            var voor = voet + new Vector2(kant * (2f + 3f * s), 0f);
            var achter = voet + new Vector2(-kant * (4f + 3f * s), 1.5f * Mathf.Max(0f, -s) + 0.5f);
            var schouder = heup + new Vector2(kant * 5f, 5.5f);
            Tekenhulp.Lijn(vh, voor + new Vector2(-4f, -0.8f), voor + new Vector2(4f, -0.8f), 1.2f, ijzer);   // ijzers
            Tekenhulp.Lijn(vh, achter + new Vector2(-4f, -0.8f), achter + new Vector2(4f, -0.8f), 1.2f, ijzer);
            Tekenhulp.Lijnstuk(vh, new[] { voor, heup, achter }, 2.2f, k, true);                                   // benen
            Tekenhulp.Lijnstuk(vh, new[] { heup, schouder }, 2.8f, k, true);                                      // romp
            Tekenhulp.Schijf(vh, schouder + new Vector2(kant * 2.4f, 3.2f), 2.6f, k, 12);                        // hoofd
            var hand = handenOpRug ? heup + new Vector2(-kant * 1.5f, 2.5f) : schouder + new Vector2(-kant * 5f * s, -4.5f);
            Tekenhulp.Lijnstuk(vh, new[] { schouder, hand }, 1.8f, k, true);                                      // arm
            if (vlag)
            {
                var stok = schouder + new Vector2(kant * 1f, 0f);
                Tekenhulp.Lijn(vh, stok, stok + new Vector2(0f, 12f), 1f, ijzer);
                var w = new Color(1f, 1f, 1f, zichtbaar);
                int i = vh.currentVertCount;
                vh.AddVert(stok + new Vector2(0f, 12f), w, Vector4.zero); vh.AddVert(stok + new Vector2(-kant * 8f, 10f), w, Vector4.zero); vh.AddVert(stok + new Vector2(0f, 8f), w, Vector4.zero);
                vh.AddTriangle(i, i + 1, i + 2);
            }
        }

        // gevarenbord aan de kant: rode driehoek met wit vlak en een uitroepteken
        void Gevarenbord(VertexHelper vh, Vector2 voet)
        {
            var paal = new Color(0.85f, 0.88f, 0.9f, 1f);
            var rood = new Color(0.85f, 0.2f, 0.17f, 1f);
            Tekenhulp.Lijn(vh, voet, voet + new Vector2(0f, 13f), 1.6f, paal);
            var m = voet + new Vector2(0f, 19f);                       // zwaartepunt van de driehoek
            Driehoek(vh, m + new Vector2(0f, 10f), m + new Vector2(-9f, -5.5f), m + new Vector2(9f, -5.5f), rood);
            Driehoek(vh, m + new Vector2(0f, 6.2f), m + new Vector2(-5.8f, -3.6f), m + new Vector2(5.8f, -3.6f), Color.white);
            var zwart = new Color(0.1f, 0.1f, 0.12f, 1f);
            Tekenhulp.Lijn(vh, m + new Vector2(0f, 3.4f), m + new Vector2(0f, -0.6f), 1.5f, zwart);
            Tekenhulp.Schijf(vh, m + new Vector2(0f, -2.2f), 0.85f, zwart, 8);
        }

        // ---------- af en toe zakt er een schaatser door het gevaarlijke ijs ----------
        // Hij schaatst het ijs op, zakt erdoor tot aan zijn schouders (hoofd boven water), en kruipt op zijn buik terug
        // over het spoor waarlangs hij kwam (daar hield het ijs hem): het advies als je door het ijs zakt.
        const float WakEerst = 10f, WakTussen = 55f;            // seconden gevaarlijk ijs tot de eerste, en daartussen (gemiddeld)
        float wakStart = -1f, volgendeWak = -1f, wakX;

        void Doorzakker(VertexHelper vh, float x0, float x1, float ijs, float t)
        {
            bool kan = Gevaarlijk && meer.Ijsdikte >= 0.01;
            if (!kan && wakStart < 0f) { volgendeWak = -1f; return; }
            if (volgendeWak < 0f) volgendeWak = t + WakEerst + Random.Range(0f, 6f);
            if (wakStart < 0f && t >= volgendeWak)
            {
                wakStart = t;
                wakX = Mathf.Lerp(x0, x1, Random.Range(0.35f, 0.6f));
            }
            if (wakStart < 0f) return;
            float f = t - wakStart;
            const float Aan = 4f, Val = 0.4f, Water = 3.6f, Kruip = 4f, Weg = 4f, Na = 5f;
            if (f > Aan + Val + Water + Kruip + Weg + Na || meer.Ijsdikte <= 0.003)
            {
                wakStart = -1f; volgendeWak = t + WakTussen * Random.Range(0.75f, 1.3f);
                return;
            }
            // het wak: een gat in het ijs met water erin, en een paar scheuren; verdwijnt langzaam weer
            if (f >= Aan)
            {
                float a = Mathf.Clamp01((Aan + Val + Water + Kruip + Weg + Na - f) / Na);
                var water = Kleur(meer.T[0]) * 0.75f; water.a = a;
                Blok(vh, wakX - 9f, ijs - 5f, wakX + 9f, ijs + 0.5f, water);
                var scheur = new Color(0.55f, 0.7f, 0.8f, a);
                Tekenhulp.Lijn(vh, new Vector2(wakX - 9f, ijs), new Vector2(wakX - 17f, ijs + 1.2f), 0.9f, scheur);
                Tekenhulp.Lijn(vh, new Vector2(wakX + 9f, ijs), new Vector2(wakX + 15f, ijs - 0.8f), 0.9f, scheur);
                Tekenhulp.Lijn(vh, new Vector2(wakX + 9f, ijs), new Vector2(wakX + 19f, ijs + 0.6f), 0.9f, scheur);
            }
            var k = vis;
            if (f < Aan)                                            // schaatst het ijs op, naar de plek van het wak
            {
                float x = Mathf.Lerp(x0 - 10f, wakX, f / Aan);
                Schaatser(vh, new Vector2(x, ijs), 1, t * 2.6f, false, 1f, false);
            }
            else if (f < Aan + Val + Water)                         // in het wak: zakt tot zijn schouders, zwaait met zijn armen
            {
                float zak = Mathf.Clamp01((f - Aan) / Val);
                float hoofd = Mathf.Lerp(16f, 4f, zak);
                var schouder = new Vector2(wakX, ijs + hoofd - 4f);
                Tekenhulp.Schijf(vh, new Vector2(wakX, ijs + hoofd), 2.6f, k, 12);
                float zwaai = Mathf.Sin(t * 9f) * 2f;
                Tekenhulp.Lijnstuk(vh, new[] { schouder, new Vector2(wakX - 11f, ijs + 1.5f + zwaai) }, 1.8f, k, true);
                Tekenhulp.Lijnstuk(vh, new[] { schouder, new Vector2(wakX + 11f, ijs + 1.5f - zwaai) }, 1.8f, k, true);
            }
            else if (f < Aan + Val + Water + Kruip)                 // kruipt op zijn buik terug, de kant op waar hij vandaan kwam
            {
                float g = (f - Aan - Val - Water) / Kruip;
                float x = Mathf.Lerp(wakX - 2f, wakX - 34f, g);         // hoofd links (vooraan)
                float kr = Mathf.Sin(t * 7f);
                var schouder = new Vector2(x + 3f, ijs + 2f);
                var heup = new Vector2(x + 11f, ijs + 1.8f);
                Tekenhulp.Schijf(vh, new Vector2(x, ijs + 2.8f), 2.4f, k, 12);
                Tekenhulp.Lijnstuk(vh, new[] { schouder, heup }, 2.6f, k, true);
                Tekenhulp.Lijnstuk(vh, new[] { schouder, new Vector2(x - 5f + 2f * kr, ijs + 0.8f) }, 1.6f, k, true);
                Tekenhulp.Lijnstuk(vh, new[] { heup, new Vector2(heup.x + 8f, ijs + 0.8f + Mathf.Max(0f, kr)) }, 2f, k, true);
            }
            else if (f < Aan + Val + Water + Kruip + Weg)           // staat op en loopt voorzichtig weg
            {
                float g = (f - Aan - Val - Water - Kruip) / Weg;
                float x = Mathf.Lerp(wakX - 34f, x0 - 10f, g);
                Schaatser(vh, new Vector2(x, ijs), -1, t * 1.6f, false, 1f - g * 0.3f, false);
            }
        }

        // bovenkant van het ijs plus het smeltwater (waar het ijslabel op staat)
        float IjsTop(float boven, float schaal)
        {
            float sw = meer.Smeltwater > 0.0005 ? Mathf.Clamp((float)meer.Smeltwater * schaal * 3f, 2f, 7f) : 0f;
            return boven + 0.08f * (float)meer.Ijsdikte * schaal + sw;
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
            // zonlicht in het water: zachte gele gloed die naar de diepte toe uitdooft (zoals het model rekent)
            if (meer.Zon > 1)
            {
                bool metIjs = meer.Ijsdikte > 0;
                double licht = meer.Zon * (1 - (metIjs ? meer.AlbedoIjs : meer.AlbedoWater)) * (metIjs ? System.Math.Exp(-meer.Ijsdikte / meer.LichtdiepteIjs) : 1);
                float sterkte = Mathf.Clamp01((float)(licht / 150.0));
                for (int i = 0; i < meer.Lagen; i++)
                {
                    float z = (i + 0.5f) * (float)(meer.Diepte / meer.Lagen);
                    float a = 0.3f * sterkte * Mathf.Exp(-z / (float)meer.LichtdiepteWater);
                    if (a < 0.01f) break;
                    Blok(vh, x0, boven - (i + 1) * dz, x1, boven - i * dz, new Color(1f, 0.88f, 0.45f, a));
                }
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
            Schaatsers(vh, x0, x1, IjsTop(boven, schaal), Time.time);
            float t = Time.time;
            // zon: groter en feller bij meer zonlicht
            if (meer.Zon > 1)
            {
                // tussen de tekst linksboven (lucht, datum) en het ijslabel boven het water
                float sterkte = Mathf.Clamp01((float)meer.Zon / 230f);
                float bovengrens = r.yMax - 54f, ondergrens = IjsTop(boven, schaal) + Schaatsruimte + 42f;
                float ruimte = Mathf.Max(20f, (bovengrens - ondergrens) * 0.5f);
                var zm = new Vector2(x0 + 56f, (bovengrens + ondergrens) * 0.5f);
                float schijf = Mathf.Clamp(ruimte * 0.32f, 8f, 16f) * Mathf.Lerp(0.75f, 1f, sterkte);
                Zonnetje.Teken(vh, zm, schijf, (ruimte - schijf) * Mathf.Lerp(0.7f, 1f, sterkte), Mathf.Lerp(0.55f, 1f, sterkte), t);
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
            // dieptelabels: wit op een donker pilletje (altijd leesbaar); nooit over het ijs, dan net eronder
            float ijsOnder = boven - 0.92f * (float)meer.Ijsdikte * schaal;
            float vorige = float.MaxValue;
            foreach (var (t, diepte) in diepteLabels)
            {
                if (t == null) continue;
                var pil = t.transform.parent as RectTransform;
                if (pil == null) continue;
                float y = Mathf.Min(boven - diepte * schaal, ijsOnder - 13f);
                y = Mathf.Min(y, vorige - 22f);                   // niet op elkaar
                vorige = y;
                pil.anchorMin = pil.anchorMax = Vector2.zero;
                pil.pivot = new Vector2(1f, 0.5f);
                pil.sizeDelta = new Vector2(t.preferredWidth + 14f, 19f);
                pil.anchoredPosition = new Vector2(r.width - 10f, y - r.yMin);
            }
            if (lBodem != null)
            {
                var pil = lBodem.transform.parent as RectTransform;
                if (pil != null)
                {
                    pil.anchorMin = pil.anchorMax = Vector2.zero;
                    pil.pivot = new Vector2(0.5f, 0.5f);
                    pil.sizeDelta = new Vector2(lBodem.preferredWidth + 14f, 19f);
                    pil.anchoredPosition = new Vector2(r.width * 0.5f, onder - r.yMin - 2f);
                }
            }
            if (lIjs != null)
            {
                lIjs.rectTransform.anchorMin = lIjs.rectTransform.anchorMax = Vector2.zero;
                lIjs.rectTransform.pivot = new Vector2(0f, 0f);
                lIjs.rectTransform.sizeDelta = new Vector2(Mathf.Max(60f, r.width - 24f), 40f);
                lIjs.rectTransform.anchoredPosition = new Vector2(12f, IjsTop(boven, schaal) + Schaatsruimte + 4f - r.yMin);
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
    /// Level 5, poolkappen: twee plaatjes onder elkaar, elk apart te laten smelten.
    /// Boven: landijs op bergen (zoals Groenland) — smelt het, dan stijgt de zeespiegel.
    /// Onder: zee-ijs dat drijft (Noordpool) — smelt het, dan blijft de zeespiegel (bijna) gelijk.
    /// Vlakke stijl met lichte lucht, turquoise bergen en witte ijsschotsen. Hoogtes niet op schaal.
    /// </summary>
    public class Poolweergave : MaskableGraphic
    {
        [Range(0f, 1f)] public float gesmoltenLand, gesmoltenZee;
        public Color lucht = new Color(0.8f, 0.92f, 0.95f, 1f), berg = new Color(0.46f, 0.77f, 0.84f, 1f), sneeuw = Color.white;
        public Color zee = new Color(0.06f, 0.38f, 0.66f, 1f), zeeDonker = new Color(0.04f, 0.3f, 0.55f, 1f), ijsZij = new Color(0.66f, 0.87f, 0.92f, 1f);
        public TextMeshProUGUI lTitelLand, lTitelZee, lLand, lZee;
        /// <summary>−1: beide plaatjes onder elkaar; 0: alleen landijs; 1: alleen zee-ijs (het hele vlak, in een afgerond masker).</summary>
        public int deel = -1;
        /// <summary>Dunne oranje rand om het plaatje (als dit ijs smelt of gesmolten is).</summary>
        public bool rand;
        public Color randKleur = new Color(0.95f, 0.45f, 0.25f, 1f);
        public float randStraal = 12f, randDikte = 2.5f;

        /// <summary>De twee plaatjes (lokale coördinaten), zodat de labels erbij kunnen.</summary>
        public Rect Land { get; private set; }
        public Rect Zee { get; private set; }

        void Update() { SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (r.width < 80f || r.height < 120f) return;
            float t = Time.time;
            if (deel >= 0)
            {
                if (deel == 0) { Land = r; TekenLand(vh, r, t); Plaats(lTitelLand, new Vector2(r.xMin + 10f, r.yMax - 12f), r, 0f); }
                else { Zee = r; TekenZee(vh, r, t); Plaats(lTitelZee, new Vector2(r.xMin + 10f, r.yMax - 12f), r, 0f); }
                if (rand) RondeRand(vh, r);
                return;
            }
            float gat = 8f, h = (r.height - gat) * 0.5f;
            Land = new Rect(r.xMin, r.yMin + h + gat, r.width, h);
            Zee = new Rect(r.xMin, r.yMin, r.width, h);
            TekenLand(vh, Land, t);
            TekenZee(vh, Zee, t);
            Plaats(lTitelLand, new Vector2(Land.xMin + 10f, Land.yMax - 12f), r, 0f);
            Plaats(lTitelZee, new Vector2(Zee.xMin + 10f, Zee.yMax - 12f), r, 0f);
        }

        // dunne rand langs de afgeronde hoeken van het masker (net binnen de rand, zodat het masker hem niet afsnijdt)
        void RondeRand(VertexHelper vh, Rect r)
        {
            float d = randDikte * 0.5f, R = Mathf.Max(0f, randStraal - d);
            var a = new Rect(r.xMin + d, r.yMin + d, r.width - 2f * d, r.height - 2f * d);
            var pad = new List<Vector2>();
            Vector2[] middens = { new Vector2(a.xMax - R, a.yMax - R), new Vector2(a.xMin + R, a.yMax - R), new Vector2(a.xMin + R, a.yMin + R), new Vector2(a.xMax - R, a.yMin + R) };
            for (int h = 0; h < 4; h++)
                for (int k = 0; k <= 8; k++)
                {
                    float hoek = (h * 90f + k * 90f / 8f) * Mathf.Deg2Rad;
                    pad.Add(middens[h] + new Vector2(Mathf.Cos(hoek), Mathf.Sin(hoek)) * R);
                }
            pad.Add(pad[0]);
            Tekenhulp.Lijnstuk(vh, pad, randDikte, randKleur, true);
        }

        void TekenLand(VertexHelper vh, Rect P, float t)
        {
            Blok(vh, P.xMin, P.yMin, P.xMax, P.yMax, lucht);
            float zee0 = P.yMin + 0.3f * P.height;
            float zeeNu = zee0 + 0.16f * P.height * gesmoltenLand;     // sterk overdreven
            // bergen (turquoise), van links tot ver in beeld; het land loopt onder water door
            float[] fx = { 0f, 0.1f, 0.2f, 0.3f, 0.42f, 0.54f, 0.64f, 0.72f, 0.78f };
            float[] fy = { 0.42f, 0.62f, 0.8f, 0.6f, 0.86f, 0.64f, 0.5f, 0.32f, 0.12f };
            var bergLijn = new List<Vector2>();
            for (int k = 0; k < fx.Length; k++) bergLijn.Add(new Vector2(P.xMin + fx[k] * P.width, P.yMin + fy[k] * P.height));
            Strook(vh, bergLijn, P.yMin, berg);
            // ijskap: wit pak op de bergen, dunner naarmate het smelt
            var ijsLijn = new List<Vector2>();
            for (int k = 0; k <= 40; k++)
            {
                float f = k / 40f, x = P.xMin + Mathf.Lerp(0f, 0.74f, f) * P.width;
                float yb = Bergh(bergLijn, x);
                float dik = 0.13f * P.height * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(f * Mathf.PI)), 0.5f) * (1f - gesmoltenLand);
                ijsLijn.Add(new Vector2(x, yb + dik));
            }
            for (int k = 0; k < ijsLijn.Count - 1; k++)
            {
                var a = ijsLijn[k]; var b = ijsLijn[k + 1];
                Vierhoek(vh, new Vector2(a.x, Bergh(bergLijn, a.x) - 1f), new Vector2(b.x, Bergh(bergLijn, b.x) - 1f), b, a, sneeuw);
            }
            // smeltwater dat van het ijs in zee loopt
            if (gesmoltenLand > 0f && gesmoltenLand < 1f)
                for (int k = 0; k < 4; k++)
                {
                    float f = Mathf.Repeat(t * 0.7f + k / 4f, 1f);
                    float x = P.xMin + Mathf.Lerp(0.62f, 0.8f, f) * P.width;
                    Tekenhulp.Schijf(vh, new Vector2(x, Mathf.Max(zeeNu, Bergh(bergLijn, x) + 2f)), 2.2f, new Color(0.3f, 0.6f, 0.95f, 1f), 8);
                }
            // zee, met donkere strepen
            Blok(vh, P.xMin, P.yMin, P.xMax, zeeNu, zee);
            Strepen(vh, P, zeeNu);
            // oude zeespiegel als stippellijn
            if (gesmoltenLand > 0f) Stippellijn(vh, P.xMin + 0.55f * P.width, P.xMax, zee0);
            Plaats(lLand, new Vector2(P.xMax - 10f, zeeNu - 14f), rectTransform.rect, 1f);
        }

        void TekenZee(VertexHelper vh, Rect P, float t)
        {
            Blok(vh, P.xMin, P.yMin, P.xMax, P.yMax, lucht);
            // verre bergen aan de horizon (decor)
            var verte = new List<Vector2>();
            float[] fx = { 0f, 0.15f, 0.3f, 0.48f, 0.62f, 0.8f, 1f };
            float[] fy = { 0.62f, 0.72f, 0.66f, 0.76f, 0.68f, 0.74f, 0.64f };
            for (int k = 0; k < fx.Length; k++) verte.Add(new Vector2(P.xMin + fx[k] * P.width, P.yMin + fy[k] * P.height));
            Strook(vh, verte, P.yMin, new Color(berg.r, berg.g, berg.b, 0.55f));
            float zeeY = P.yMin + 0.56f * P.height;                     // de zeespiegel blijft gelijk
            Blok(vh, P.xMin, P.yMin, P.xMax, zeeY, zee);
            Strepen(vh, P, zeeY);
            // drie schotsen: 10 % boven water; het deel onder water schemert door het water heen
            float[,] s = { { 0.05f, 0.42f, 1f }, { 0.48f, 0.9f, 0.85f }, { 0.22f, 0.66f, 0.7f } };
            float[] diep = { 0f, -0.16f, -0.32f };
            for (int k = 0; k < 3; k++)
            {
                float krimp = 1f - 0.6f * gesmoltenZee;
                float dik = 0.11f * P.height * s[k, 2] * (1f - gesmoltenZee);
                if (dik < 0.5f) continue;
                float y = zeeY + diep[k] * P.height * 0.5f;           // lager in beeld = dichterbij (vlakke stijl, zonder echt perspectief)
                float mid = P.xMin + (s[k, 0] + s[k, 1]) * 0.5f * P.width, half = (s[k, 1] - s[k, 0]) * 0.5f * P.width * krimp;
                float top = y + 0.1f * dik, bodem = y - 0.9f * dik, diepte = 0.035f * P.height * krimp;
                // onderwaterdeel (lichtblauw, half doorzichtig) en zijkant boven water
                Vierhoek(vh, new Vector2(mid - half, bodem), new Vector2(mid + half, bodem), new Vector2(mid + half, y), new Vector2(mid - half, y), new Color(ijsZij.r, ijsZij.g, ijsZij.b, 0.45f));
                Vierhoek(vh, new Vector2(mid - half, y), new Vector2(mid + half, y), new Vector2(mid + half, top), new Vector2(mid - half, top), ijsZij);
                // bovenvlak (wit, in perspectief)
                Vierhoek(vh, new Vector2(mid - half, top), new Vector2(mid + half, top), new Vector2(mid + half - 14f, top + diepte), new Vector2(mid - half + 22f, top + diepte), sneeuw);
            }
            Plaats(lZee, new Vector2(P.xMax - 10f, zeeY + 14f), rectTransform.rect, 1f);
        }

        // hoogte van de berglijn op x (lineair tussen de punten)
        static float Bergh(List<Vector2> lijn, float x)
        {
            if (x <= lijn[0].x) return lijn[0].y;
            for (int k = 0; k < lijn.Count - 1; k++)
                if (x <= lijn[k + 1].x) return Mathf.Lerp(lijn[k].y, lijn[k + 1].y, Mathf.InverseLerp(lijn[k].x, lijn[k + 1].x, x));
            return lijn[lijn.Count - 1].y;
        }

        // gevuld vlak tussen een lijn en een horizontale onderkant
        static void Strook(VertexHelper vh, List<Vector2> lijn, float onder, Color k)
        {
            for (int i = 0; i < lijn.Count - 1; i++)
                Vierhoek(vh, new Vector2(lijn[i].x, onder), new Vector2(lijn[i + 1].x, onder), lijn[i + 1], lijn[i], k);
        }

        void Strepen(VertexHelper vh, Rect P, float zeeY)
        {
            for (int k = 1; k <= 5; k++)
            {
                float y = zeeY - k * (zeeY - P.yMin) / 6.5f;
                float a = P.xMin + ((k * 37) % 50) / 100f * P.width, b = a + P.width * 0.32f;
                Tekenhulp.Lijn(vh, new Vector2(a, y), new Vector2(Mathf.Min(b, P.xMax - 4f), y), 2f, zeeDonker);
            }
        }

        static void Stippellijn(VertexHelper vh, float a, float b, float y)
        {
            for (float x = a; x < b; x += 8f) Tekenhulp.Lijn(vh, new Vector2(x, y), new Vector2(Mathf.Min(x + 4f, b), y), 1.4f, new Color(1f, 1f, 1f, 0.85f));
        }

        static void Plaats(TextMeshProUGUI l, Vector2 p, Rect r, float pivotX)
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

        static void Vierhoek(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color k)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, k, Vector4.zero); vh.AddVert(b, k, Vector4.zero); vh.AddVert(c, k, Vector4.zero); vh.AddVert(d, k, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
