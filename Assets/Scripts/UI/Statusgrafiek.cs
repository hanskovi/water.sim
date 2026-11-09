using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WaterSim.Sim;

namespace WaterSim.UI
{
    /// <summary>Hulpjes om lijnen en schijfjes in een UI-mesh te tekenen.</summary>
    static class Tekenhulp
    {
        public static void Lijn(VertexHelper vh, Vector2 a, Vector2 b, float dikte, Color kleur)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-d.y, d.x).normalized * (dikte * 0.5f);
            int i = vh.currentVertCount;
            vh.AddVert(a - n, kleur, Vector4.zero); vh.AddVert(a + n, kleur, Vector4.zero);
            vh.AddVert(b + n, kleur, Vector4.zero); vh.AddVert(b - n, kleur, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        public static void Schijf(VertexHelper vh, Vector2 m, float r, Color kleur, int stukken = 16)
        {
            int i = vh.currentVertCount;
            vh.AddVert(m, kleur, Vector4.zero);
            for (int k = 0; k <= stukken; k++)
            {
                float a = k * Mathf.PI * 2f / stukken;
                vh.AddVert(m + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, kleur, Vector4.zero);
                if (k > 0) vh.AddTriangle(i, i + k, i + k + 1);
            }
        }

        /// <summary>Doorlopende lijn met ronde knikken.</summary>
        public static void Lijnstuk(VertexHelper vh, System.Collections.Generic.IList<Vector2> p, float dikte, Color kleur, bool rondeEinden)
        {
            for (int k = 0; k + 1 < p.Count; k++) Lijn(vh, p[k], p[k + 1], dikte, kleur);
            for (int k = rondeEinden ? 0 : 1; k < (rondeEinden ? p.Count : p.Count - 1); k++) Schijf(vh, p[k], dikte * 0.5f, kleur, 10);
        }
    }

    /// <summary>
    /// Ring rond de temperatuur: hoeveel er al gesmolten (of bevroren) is. Begint bovenaan, loopt met de klok mee.
    /// </summary>
    public class Voortgangsring : MaskableGraphic
    {
        [Range(0f, 1f)] public float deel;
        public float dikte = 7f;
        public Color achter = new Color(1f, 1f, 1f, 0.16f);
        public Color voor = Color.white;

        public void Zet(float nieuwDeel, Color kleur)
        {
            nieuwDeel = Mathf.Clamp01(nieuwDeel);
            if (Mathf.Abs(nieuwDeel - deel) < 0.0005f && kleur == voor) return;
            deel = nieuwDeel; voor = kleur;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float straal = Mathf.Min(r.width, r.height) * 0.5f - dikte * 0.5f - 1f;
            if (straal <= 0f) return;
            var m = r.center;
            Boog(vh, m, straal, 0f, 1f, achter, 72);
            if (deel <= 0.001f) return;
            Boog(vh, m, straal, 0f, deel, voor, Mathf.Max(4, Mathf.CeilToInt(72 * deel)));
            Tekenhulp.Schijf(vh, Punt(m, straal, 0f), dikte * 0.5f, voor, 12);      // ronde uiteinden
            Tekenhulp.Schijf(vh, Punt(m, straal, deel), dikte * 0.5f, voor, 12);
        }

        static Vector2 Punt(Vector2 m, float r, float f)
        {
            float a = Mathf.PI * 0.5f - f * Mathf.PI * 2f;      // bovenaan beginnen, met de klok mee
            return m + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        void Boog(VertexHelper vh, Vector2 m, float r, float van, float tot, Color kleur, int stukken)
        {
            float bi = r - dikte * 0.5f, bu = r + dikte * 0.5f;
            int start = vh.currentVertCount;
            for (int k = 0; k <= stukken; k++)
            {
                float f = Mathf.Lerp(van, tot, k / (float)stukken);
                float a = Mathf.PI * 0.5f - f * Mathf.PI * 2f;
                var u = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                vh.AddVert(m + u * bi, kleur, Vector4.zero);
                vh.AddVert(m + u * bu, kleur, Vector4.zero);
                if (k > 0)
                {
                    int i = start + 2 * k;
                    vh.AddTriangle(i - 2, i - 1, i + 1);
                    vh.AddTriangle(i - 2, i + 1, i);
                }
            }
        }
    }

    /// <summary>
    /// Kleine opwarmkromme: temperatuur tegen toegevoerde warmte (J/g), met het smeltplateau bij 0 °C, de huidige
    /// toestand als stip en een pijl in de looprichting. Bij stollen gespiegeld: links hoog (water), rechts laag (ijs),
    /// zodat je altijd van links naar rechts leest. Verhoudingen volgen Binas (c_ijs, c_water, smeltwarmte).
    /// </summary>
    public class Opwarmkromme : MaskableGraphic
    {
        public float enthalpie;                 // J/g, nulpunt ijs van 0 °C (zoals Fasemodel)
        public float tLaag = -20f, tHoog = 20f;
        public int richting;                    // +1 warmte erin, −1 warmte eruit, 0 niets
        public bool spiegel, plateau;
        public Color accent = Color.white;
        public Color lijn = new Color(1f, 1f, 1f, 0.45f);
        public Color stippel = new Color(1f, 1f, 1f, 0.16f);
        public TextMeshProUGUI lNul, lIjs, lMengsel, lWater;

        const float Links = 3f, Rechts = 3f, Onder = 13f, Boven = 12f;
        static float L => (float)Constanten.Smeltwarmte;
        float HL => (float)Constanten.SoortelijkeWarmteIjs * tLaag;
        float HH => L + (float)Constanten.SoortelijkeWarmteWater * tHoog;

        public void Zet(float h, float tWater, int nieuweRichting, bool opPlateau, Color kleur)
        {
            enthalpie = h;
            tLaag = Mathf.Min(-20f, tWater);
            tHoog = Mathf.Max(20f, tWater);
            richting = nieuweRichting;
            spiegel = nieuweRichting < 0;
            plateau = opPlateau;
            accent = kleur;
            SetVerticesDirty();
            PlaatsLabels();
        }

        Vector2 P(Rect r, float h)
        {
            float t = (float)Fasemodel.TemperatuurBij(h);
            float x = Mathf.InverseLerp(HL, HH, h);
            if (spiegel) x = 1f - x;
            float y = Mathf.InverseLerp(tLaag, tHoog, t);
            return new Vector2(r.xMin + Links + x * (r.width - Links - Rechts), r.yMin + Onder + y * (r.height - Onder - Boven));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (r.width < 20f || r.height < 30f) return;

            // stippellijn op 0 °C
            float y0 = P(r, 0f).y;
            for (float x = r.xMin; x < r.xMax; x += 7f)
                Tekenhulp.Lijn(vh, new Vector2(x, y0), new Vector2(Mathf.Min(x + 3f, r.xMax), y0), 1f, stippel);

            // de kromme zelf, en het plateau in de accentkleur
            Tekenhulp.Lijnstuk(vh, new[] { P(r, HL), P(r, 0f), P(r, L), P(r, HH) }, 2f, lijn, false);
            if (plateau) Tekenhulp.Lijnstuk(vh, new[] { P(r, 0f), P(r, L) }, 4f, accent, true);

            float h = Mathf.Clamp(enthalpie, HL, HH);
            if (richting != 0)
            {
                // pijl net boven de kromme, vanaf de stip in de looprichting
                float totaal = HH - HL, van = h + richting * 0.05f * totaal, tot = h + richting * 0.24f * totaal;
                var pijl = new System.Collections.Generic.List<Vector2>();
                for (int k = 0; k <= 16; k++)
                {
                    float e = Mathf.Lerp(van, tot, k / 16f);
                    if (e < HL || e > HH) break;
                    pijl.Add(P(r, e) + new Vector2(0f, 9f));
                }
                if (pijl.Count >= 2)
                {
                    Tekenhulp.Lijnstuk(vh, pijl, 2f, accent, true);
                    var eind = pijl[pijl.Count - 1];
                    var dir = (eind - pijl[pijl.Count - 2]).normalized;
                    var terug = -dir * 5.5f;
                    Tekenhulp.Lijnstuk(vh, new[] { eind + (Vector2)(Quaternion.Euler(0, 0, 38f) * terug), eind }, 2f, accent, true);
                    Tekenhulp.Lijnstuk(vh, new[] { eind + (Vector2)(Quaternion.Euler(0, 0, -38f) * terug), eind }, 2f, accent, true);
                }
            }

            var stip = P(r, h);
            Tekenhulp.Schijf(vh, stip, 6.5f, Color.white, 18);
            Tekenhulp.Schijf(vh, stip, 5f, accent, 18);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            PlaatsLabels();
        }

        // labels onder elk stuk van de kromme, en "0 °C" boven de stippellijn aan de beginkant
        void PlaatsLabels()
        {
            var r = rectTransform.rect;
            if (r.width < 20f) return;
            void Zet(TextMeshProUGUI t, float x, float y, float pivotX)
            {
                if (t == null) return;
                var rt = t.rectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(pivotX, 0f);
                rt.anchoredPosition = new Vector2(x - r.xMin, y - r.yMin);
            }
            float onder = r.yMin - 1f;
            Zet(lIjs, (P(r, HL).x + P(r, 0f).x) * 0.5f, onder, 0.5f);
            Zet(lMengsel, (P(r, 0f).x + P(r, L).x) * 0.5f, onder, 0.5f);
            Zet(lWater, (P(r, L).x + P(r, HH).x) * 0.5f, onder, 0.5f);
            Zet(lNul, spiegel ? r.xMax - 1f : r.xMin + 1f, P(r, 0f).y + 2f, spiegel ? 1f : 0f);
            if (lNul != null) lNul.alignment = spiegel ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.BottomLeft;
        }
    }
}
