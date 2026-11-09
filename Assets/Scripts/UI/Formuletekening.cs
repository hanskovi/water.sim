using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WaterSim.UI
{
    /// <summary>
    /// Eenvoudige lijntekening voor het formulekaartje: bindingsstreepjes, gestippelde H-bruggen, puntjes
    /// (vrije elektronenparen), rondjes en een hoekboogje. Coördinaten in eenheden met de oorsprong linksboven
    /// (y naar beneden), zoals in een tekening op papier.
    /// </summary>
    public class Formuletekening : MaskableGraphic
    {
        struct Lijn { public Vector2 a, b; public float dikte, stip; public Color kleur; }
        struct Rondje { public Vector2 m; public float r; public Color kleur; }
        struct Boog { public Vector2 m; public float r, van, tot, dikte; public Color kleur; }

        readonly List<Lijn> lijnen = new List<Lijn>();
        readonly List<Rondje> rondjes = new List<Rondje>();
        readonly List<Boog> bogen = new List<Boog>();

        public void Wis() { lijnen.Clear(); rondjes.Clear(); bogen.Clear(); SetVerticesDirty(); }

        /// <summary>Lijn; met <paramref name="stip"/> &gt; 0 een stippellijn met die afstand tussen de stippen.</summary>
        public void Streep(Vector2 a, Vector2 b, Color kleur, float dikte = 2f, float stip = 0f)
        { lijnen.Add(new Lijn { a = a, b = b, kleur = kleur, dikte = dikte, stip = stip }); SetVerticesDirty(); }

        public void Punt(Vector2 m, float r, Color kleur)
        { rondjes.Add(new Rondje { m = m, r = r, kleur = kleur }); SetVerticesDirty(); }

        /// <summary>Boog rond m (hoeken in graden, 0 = rechts, met de klok mee want y wijst omlaag).</summary>
        public void Hoekboog(Vector2 m, float r, float van, float tot, Color kleur, float dikte = 1.5f)
        { bogen.Add(new Boog { m = m, r = r, van = van, tot = tot, kleur = kleur, dikte = dikte }); SetVerticesDirty(); }

        Vector2 P(Rect r, Vector2 p) => new Vector2(r.xMin + p.x, r.yMax - p.y);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            foreach (var l in lijnen)
            {
                Vector2 a = P(r, l.a), b = P(r, l.b);
                if (l.stip <= 0f) { Tekenhulp.Lijnstuk(vh, new[] { a, b }, l.dikte, l.kleur, true); continue; }
                float L = Vector2.Distance(a, b);
                int n = Mathf.Max(2, Mathf.RoundToInt(L / l.stip) + 1);
                for (int k = 0; k < n; k++) Tekenhulp.Schijf(vh, Vector2.Lerp(a, b, k / (float)(n - 1)), l.dikte * 0.5f, l.kleur, 10);
            }
            foreach (var b in bogen)
            {
                var punten = new List<Vector2>();
                for (int k = 0; k <= 16; k++)
                {
                    float h = Mathf.Lerp(b.van, b.tot, k / 16f) * Mathf.Deg2Rad;
                    punten.Add(P(r, b.m + new Vector2(Mathf.Cos(h), Mathf.Sin(h)) * b.r));
                }
                Tekenhulp.Lijnstuk(vh, punten, b.dikte, b.kleur, true);
            }
            foreach (var o in rondjes) Tekenhulp.Schijf(vh, P(r, o.m), o.r, o.kleur, 18);
        }
    }
}
