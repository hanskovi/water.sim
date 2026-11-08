using UnityEngine;
using UnityEngine.UI;

// Let op: voeg een CanvasRenderer toe vóór dit onderdeel (AddComponent<CanvasRenderer>()), anders tekent het niets.

namespace WaterSim.UI
{
    /// <summary>
    /// Achtergrond van het startscherm in de stijl van het app-icoon, als UI-vlakken die zichzelf tekenen
    /// (geen plaatjes, dus scherp op elk scherm). Drie lagen, elk een eigen object, in deze volgorde:
    ///   Water   lagune-verloop bovenin, twee sinusgolven (voor en ver weg, een halve golf verschoven), donker water
    ///   IJs     zeshoekrooster in het water: onzichtbaar bij de golf, onderin goed zichtbaar ("bevriezen")
    ///   Schuim  lichte schuimrand op de golf en stoom als slangetjes die omhoog kruipen en vervagen
    /// Kleuren en verhoudingen zoals Tools/Icoon/maak_icoon.py.
    /// </summary>
    public class IcoonAchtergrond : MaskableGraphic
    {
        public enum Laag { Water, IJs, Schuim }
        public Laag laag;

        [Header("Golven")]
        [Tooltip("Hoogte van het water als deel van de schermhoogte (vanaf onder).")]
        public float golfHoogte = 0.30f;
        [Tooltip("Hoogte van de top (en diepte van het dal) van de voorste golf.")]
        public float golfUitslag = 15f;
        [Tooltip("Golflengte als deel van de schermbreedte.")]
        public float golfLengte = 1f;
        [Tooltip("Seconden waarin de golf twee schermbreedtes opschuift (rustig tempo).")]
        public float oversteekTijd = 26f;
        [Tooltip("Achterste golf: grootte t.o.v. de voorste.")]
        public float achterSchaal = 0.65f;

        [Header("Stoom")]
        [Tooltip("Hoogste zichtbaarheid van een stoomslangetje (0..1).")]
        public float stoomZicht = 0.32f;

        // Het project rekent in lineaire kleuren: zonder omrekenen zou de achtergrond bleker zijn dan het icoon.
        Color Boven1, Boven2, Onder1, Onder2, SchuimKleur, IJsKleur, Ver1, VerSchuim;

        void Kleuren()
        {
            Boven1 = Echt(Hex(0x3CC0DC)); Boven2 = Echt(Hex(0x1696B8));
            Onder1 = Echt(Hex(0x0E7C9E)); Onder2 = Echt(Hex(0x064A63));
            SchuimKleur = Echt(new Color(0xBD / 255f, 0xEF / 255f, 0xFA / 255f, 0.95f));
            IJsKleur = Echt(new Color(0xC4 / 255f, 0xF4 / 255f, 0xFB / 255f, 1f));
            Ver1 = Echt(Hex(0x1F8FB4));                                           // verre golf: lichter, wazig
            VerSchuim = Echt(new Color(0xBD / 255f, 0xEF / 255f, 0xFA / 255f, 0.45f));
        }

        static Color Echt(Color c)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear) return c;
            var l = c.linear;
            return new Color(l.r, l.g, l.b, c.a);
        }

        const int Stappen = 64;
        const float HexR = 30f, HexLijn = 2.4f, HexMax = 0.6f;
        const int Pluimen = 5;

        float tijd;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            Kleuren();
        }

        void Update()
        {
            if (laag == Laag.IJs) return;          // het rooster staat stil
            tijd += Time.unscaledDeltaTime;
            SetVerticesDirty();
        }

        float Basis(Rect r) => r.yMin + r.height * golfHoogte;

        // Twee doorlopende sinusgolven die met dezelfde snelheid opschuiven. De achterste ligt een halve golf
        // verschoven: waar de voorste in zijn dal zakt, komt de top van de achterste omhoog (altijd opvulling).
        float Fase(Rect r, float x)
        {
            float lambda = r.width * golfLengte;
            float snelheid = 2f * r.width / oversteekTijd;
            return (x - r.xMin - snelheid * tijd) / lambda * Mathf.PI * 2f;
        }

        float VoorY(Rect r, float x) => Basis(r) + golfUitslag * Mathf.Sin(Fase(r, x));

        // iets lager en kleiner (verder weg); zijn top steekt uit boven het dal van de voorste golf
        float AchterY(Rect r, float x) => Basis(r) - 3f + golfUitslag * achterSchaal * Mathf.Sin(Fase(r, x) + Mathf.PI);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (r.width <= 0 || r.height <= 0) return;
            switch (laag)
            {
                case Laag.Water: Water(vh, r); break;
                case Laag.IJs: IJs(vh, r); break;
                case Laag.Schuim: Schuim(vh, r); break;
            }
        }

        void Water(VertexHelper vh, Rect r)
        {
            // lagune: verloop over het hele vlak
            Vierhoek(vh, new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin), new Vector2(r.xMin, r.yMin),
                     Boven1, Boven1, Boven2, Boven2);

            // verre golf (achter): lichter water met een vage schuimrand; meestal verstopt achter het voorste water
            var achter = new Vector2[Stappen + 1];
            var achterKl = new Color[Stappen + 1];
            for (int i = 0; i <= Stappen; i++)
            {
                float x = Mathf.Lerp(r.xMin, r.xMax, i / (float)Stappen);
                achter[i] = new Vector2(x, AchterY(r, x));
                achterKl[i] = VerSchuim;
            }
            for (int i = 0; i < Stappen; i++)
                Vierhoek(vh, achter[i], achter[i + 1], new Vector2(achter[i + 1].x, r.yMin), new Vector2(achter[i].x, r.yMin), Ver1, Ver1, Onder1, Onder1);
            Band(vh, achter, achterKl, 2.5f);

            // voorste water
            float top = Basis(r) + golfUitslag;
            for (int i = 0; i < Stappen; i++)
            {
                float x0 = Mathf.Lerp(r.xMin, r.xMax, i / (float)Stappen), x1 = Mathf.Lerp(r.xMin, r.xMax, (i + 1) / (float)Stappen);
                float y0 = VoorY(r, x0), y1 = VoorY(r, x1);
                Color k0 = Color.Lerp(Onder1, Onder2, Mathf.InverseLerp(top, r.yMin, y0));
                Color k1 = Color.Lerp(Onder1, Onder2, Mathf.InverseLerp(top, r.yMin, y1));
                Vierhoek(vh, new Vector2(x0, y0), new Vector2(x1, y1), new Vector2(x1, r.yMin), new Vector2(x0, r.yMin), k0, k1, Onder2, Onder2);
            }
        }

        void IJs(VertexHelper vh, Rect r)
        {
            float golf = Basis(r) - golfUitslag * 1.1f;   // net onder het diepste dal van de golf
            float dx = HexR * Mathf.Sqrt(3f);
            int rijen = Mathf.CeilToInt((golf - r.yMin) / (HexR * 1.5f)) + 1;
            for (int j = 0; j < rijen; j++)
                for (float x = r.xMin - dx; x < r.xMax + dx; x += dx)
                {
                    float cx = x + (j % 2 == 1 ? dx / 2f : 0f), cy = r.yMin + j * HexR * 1.5f;
                    for (int k = 0; k < 6; k++)
                    {
                        float a0 = Mathf.PI / 3f * k + Mathf.PI / 6f, a1 = a0 + Mathf.PI / 3f;
                        var p0 = new Vector2(cx + HexR * Mathf.Cos(a0), cy + HexR * Mathf.Sin(a0));
                        var p1 = new Vector2(cx + HexR * Mathf.Cos(a1), cy + HexR * Mathf.Sin(a1));
                        Color c0 = IJsKleur, c1 = IJsKleur;
                        c0.a = Bevroren(p0.y, golf, r.yMin); c1.a = Bevroren(p1.y, golf, r.yMin);
                        if (c0.a <= 0.001f && c1.a <= 0.001f) continue;
                        Lijn(vh, p0, p1, HexLijn, c0, c1);
                    }
                }
        }

        // vriesverloop: 0 bij de golf, onderin HexMax zichtbaar (zoals in het icoon)
        static float Bevroren(float y, float golf, float onder)
        {
            float t = Mathf.InverseLerp(golf, onder, y);
            if (y > golf) return 0f;
            return t < 0.25f ? HexMax * 0.15f * (t / 0.25f) : HexMax * (0.15f + 0.85f * (t - 0.25f) / 0.75f);
        }

        void Schuim(VertexHelper vh, Rect r)
        {
            // schuimrand op de voorste golf, als één doorlopende band
            var golf = new Vector2[Stappen + 1];
            var kleuren = new Color[Stappen + 1];
            for (int i = 0; i <= Stappen; i++)
            {
                float x = Mathf.Lerp(r.xMin, r.xMax, i / (float)Stappen);
                golf[i] = new Vector2(x, VoorY(r, x));
                kleuren[i] = SchuimKleur;
            }
            Band(vh, golf, kleuren, 4f);

            for (int p = 0; p < Pluimen; p++) Slangetje(vh, r, p);
        }

        // Stoom als slangetje: kruipt langs een vaste kronkelbaan omhoog. De kop komt geleidelijk uit het niets,
        // de staart vervaagt, en hoe hoger, hoe vager het geheel.
        void Slangetje(VertexHelper vh, Rect r, int p)
        {
            float x0 = r.xMin + r.width * ((0.12f + p * 0.19f + 0.05f * Mathf.Sin(p * 2.1f)) % 1f);
            float voet = Basis(r) + golfUitslag + 10f;               // net boven het water
            float reis = r.height * 0.40f;                           // zo hoog kruipt hij
            float lengte = 70f + 12f * Mathf.Sin(p * 1.3f);
            float snelheid = 10f + 2.2f * p;                         // eenheden per seconde
            float pauze = 30f + 25f * ((p * 0.37f) % 1f);            // even niets tussen twee slangetjes
            float kop = (tijd * snelheid + p * 131f) % (reis + lengte + pauze);
            if (kop > reis + lengte) return;

            const int n = 28;
            var pts = new Vector2[n + 1];
            var kl = new Color[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n;                              // 0 = staart, 1 = kop
                float afstand = kop - lengte + u * lengte;           // langs de baan, vanaf het water
                float s = Mathf.Max(0f, afstand);
                pts[i] = new Vector2(x0 + 8f * Mathf.Sin(s / 48f * Mathf.PI * 2f + p * 1.7f), voet + s);
                float vorm = Mathf.SmoothStep(0f, 1f, u / 0.45f) * Mathf.SmoothStep(0f, 1f, (1f - u) / 0.25f);
                float hoogte = 1f - 0.8f * Mathf.Clamp01(s / reis);
                float uitHetWater = Mathf.Clamp01(afstand / 12f);   // nog in het water: niet zichtbaar
                kl[i] = new Color(1f, 1f, 1f, stoomZicht * vorm * hoogte * uitHetWater);
            }
            Band(vh, pts, kl, 3.2f);
        }

        // doorlopende band langs een lijn: gedeelde hoekpunten, dus geen overlap (geen "kraaltjes" bij doorzichtigheid)
        static void Band(VertexHelper vh, Vector2[] pts, Color[] kleur, float dikte)
        {
            int start = vh.currentVertCount;
            for (int i = 0; i < pts.Length; i++)
            {
                Vector2 richting = i == 0 ? pts[1] - pts[0] : i == pts.Length - 1 ? pts[i] - pts[i - 1] : pts[i + 1] - pts[i - 1];
                var n = new Vector2(-richting.y, richting.x).normalized * (dikte * 0.5f);
                vh.AddVert(pts[i] + n, kleur[i], Vector2.zero);
                vh.AddVert(pts[i] - n, kleur[i], Vector2.zero);
            }
            for (int i = 0; i < pts.Length - 1; i++)
            {
                int a = start + 2 * i;
                vh.AddTriangle(a, a + 2, a + 3);
                vh.AddTriangle(a, a + 3, a + 1);
            }
        }

        static void Lijn(VertexHelper vh, Vector2 a, Vector2 b, float dikte, Color ka, Color kb)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-d.y, d.x).normalized * (dikte * 0.5f);
            // iets langer, zodat de stukjes naadloos op elkaar aansluiten
            var e = d.normalized * (dikte * 0.25f);
            Vierhoek(vh, a - e + n, b + e + n, b + e - n, a - e - n, ka, kb, kb, ka);
        }

        static void Vierhoek(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ka, Color kb, Color kc, Color kd)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, ka, Vector2.zero);
            vh.AddVert(b, kb, Vector2.zero);
            vh.AddVert(c, kc, Vector2.zero);
            vh.AddVert(d, kd, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}
