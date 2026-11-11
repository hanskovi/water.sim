using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WaterSim.Sim;

namespace WaterSim.UI
{
    /// <summary>
    /// Tekenveld voor de opdracht "teken waterstofbruggen": structuurformules (zonder vrije elektronenparen).
    /// Sleep van een H-atoom naar een atoom van een ander molecuul om een brug te tekenen; tik op een brug om hem weg
    /// te halen. Na het controleren kleuren goede bruggen blauw en foute rood. Leest en schrijft alleen de lijst met
    /// bruggen die de quiz bijhoudt; de beoordeling zit in <see cref="HBrugRegels"/>.
    /// </summary>
    public class Tekenveld : MaskableGraphic, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public struct Plaatsing { public Molecuul2D Mol; public Vector2 Plek; public float Hoek; }   // plek 0..1 in het veld

        public List<Plaatsing> Moleculen = new List<Plaatsing>();
        public List<HBrugRegels.Brug> Bruggen = new List<HBrugRegels.Brug>();
        public HBrugRegels.Oordeel[] Oordelen;                // na controleren, anders null
        public bool Vergrendeld;
        public Action Gewijzigd;
        public TMP_FontAsset Font;

        public Color Binding = new Color(0.85f, 0.92f, 0.96f, 1f), Achter = new Color(0.02f, 0.16f, 0.23f, 1f);
        public Color BrugKleur = new Color(0.36f, 0.76f, 1f, 1f), FoutKleur = new Color(1f, 0.42f, 0.36f, 1f), Kies = new Color(1f, 0.6f, 0.35f, 1f);
        public Color KleurO = new Color(1f, 0.5f, 0.45f, 1f), KleurN = new Color(0.55f, 0.65f, 1f, 1f), KleurS = new Color(1f, 0.85f, 0.35f, 1f);

        // berekende posities (lokaal) per molecuul en atoom
        readonly List<Vector2[]> pos = new List<Vector2[]>();
        readonly List<TextMeshProUGUI> letters = new List<TextMeshProUGUI>();
        float schaal = 20f;
        int sleepMol = -1, sleepAtoom = -1;
        Vector2 sleepNaar;
        bool gesleept;
        Rect laatsteRect;

        void Update()
        {
            if (rectTransform.rect != laatsteRect) { laatsteRect = rectTransform.rect; Indeling(); SetVerticesDirty(); }
        }

        /// <summary>Opnieuw indelen en tekenen (na het zetten van de moleculen of bruggen).</summary>
        public void Ververs() { Indeling(); SetVerticesDirty(); }

        void Indeling()
        {
            var r = rectTransform.rect;
            pos.Clear();
            if (r.width < 20f || Moleculen.Count == 0) return;
            // één schaal voor alle moleculen: elk past in zijn vak (2 kolommen × 3 rijen)
            float vakB = r.width / 2f, vakH = r.height / 3f;
            schaal = float.MaxValue;
            foreach (var p in Moleculen)
            {
                Afmeting(p, out float b, out float h);
                schaal = Mathf.Min(schaal, (vakB - 10f) / (b + 1.1f), (vakH - 8f) / (h + 1.1f));
            }
            schaal = Mathf.Clamp(schaal, 8f, 34f);
            foreach (var p in Moleculen)
            {
                var m = p.Mol; var a = new Vector2[m.Atomen.Length];
                Vector2 midden = Midden(m);
                float c = Mathf.Cos(p.Hoek * Mathf.Deg2Rad), s = Mathf.Sin(p.Hoek * Mathf.Deg2Rad);
                var centrum = new Vector2(r.xMin + p.Plek.x * r.width, r.yMin + p.Plek.y * r.height);
                for (int i = 0; i < a.Length; i++)
                {
                    var v = new Vector2((float)m.X[i], (float)m.Y[i]) - midden;
                    a[i] = centrum + new Vector2(c * v.x - s * v.y, s * v.x + c * v.y) * schaal;
                }
                pos.Add(a);
            }
            Letters();
        }

        static Vector2 Midden(Molecuul2D m)
        {
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            for (int i = 0; i < m.Atomen.Length; i++) { x0 = Mathf.Min(x0, (float)m.X[i]); x1 = Mathf.Max(x1, (float)m.X[i]); y0 = Mathf.Min(y0, (float)m.Y[i]); y1 = Mathf.Max(y1, (float)m.Y[i]); }
            return new Vector2((x0 + x1) / 2f, (y0 + y1) / 2f);
        }

        // breedte en hoogte na draaien (in bindingslengtes)
        static void Afmeting(Plaatsing p, out float b, out float h)
        {
            var m = p.Mol; var mid = Midden(m);
            float c = Mathf.Cos(p.Hoek * Mathf.Deg2Rad), s = Mathf.Sin(p.Hoek * Mathf.Deg2Rad);
            float x0 = 0, x1 = 0, y0 = 0, y1 = 0;
            for (int i = 0; i < m.Atomen.Length; i++)
            {
                var v = new Vector2((float)m.X[i], (float)m.Y[i]) - mid;
                float x = c * v.x - s * v.y, y = s * v.x + c * v.y;
                x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
            b = x1 - x0; h = y1 - y0;
        }

        // een letter per atoom (O, N, S, C, H), boven op de tekening
        void Letters()
        {
            int nodig = 0;
            foreach (var p in Moleculen) nodig += p.Mol.Atomen.Length;
            while (letters.Count < nodig)
            {
                var go = new GameObject("Letter", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var t = go.AddComponent<TextMeshProUGUI>();
                if (Font != null) t.font = Font;
                t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.fontStyle = FontStyles.Bold;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                t.rectTransform.sizeDelta = new Vector2(30f, 30f);
                letters.Add(t);
            }
            var r = rectTransform.rect; int k = 0;
            for (int mi = 0; mi < Moleculen.Count; mi++)
            {
                var m = Moleculen[mi].Mol;
                for (int i = 0; i < m.Atomen.Length; i++, k++)
                {
                    var t = letters[k]; t.gameObject.SetActive(true);
                    var e = m.Atomen[i];
                    t.text = e.ToString();
                    t.fontSize = Mathf.Clamp(schaal * (e == Element.H ? 0.62f : 0.78f), 9f, 22f);
                    t.color = e == Element.O ? KleurO : e == Element.N ? KleurN : e == Element.S ? KleurS : Color.white;
                    t.rectTransform.anchoredPosition = pos[mi][i] - r.center;
                }
            }
            for (; k < letters.Count; k++) letters[k].gameObject.SetActive(false);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (pos.Count != Moleculen.Count) return;
            float dik = Mathf.Max(1.4f, schaal * 0.07f), straalAtoom = schaal * 0.36f;
            // bindingen
            for (int mi = 0; mi < Moleculen.Count; mi++)
            {
                var m = Moleculen[mi].Mol; var a = pos[mi];
                foreach (var (i, j, orde) in m.Bindingen)
                {
                    var d = (a[j] - a[i]).normalized; var n = new Vector2(-d.y, d.x);
                    var p0 = a[i] + d * straalAtoom; var p1 = a[j] - d * straalAtoom;
                    if (orde == 2) { Tekenhulp.Lijn(vh, p0 + n * dik * 1.3f, p1 + n * dik * 1.3f, dik, Binding); Tekenhulp.Lijn(vh, p0 - n * dik * 1.3f, p1 - n * dik * 1.3f, dik, Binding); }
                    else Tekenhulp.Lijn(vh, p0, p1, dik, Binding);
                }
                // vrije elektronenparen tekenen we bewust niet: leerlingen leren alleen dat O 2 en N 1 waterstofbrug kan ontvangen
            }
            // bruggen (stippellijn); rood als ze fout zijn
            for (int k = 0; k < Bruggen.Count; k++)
            {
                var b = Bruggen[k];
                if (b.Van >= pos.Count || b.Doel >= pos.Count) continue;
                bool fout = Oordelen != null && k < Oordelen.Length && Oordelen[k] != HBrugRegels.Oordeel.Goed;
                Stippel(vh, pos[b.Van][b.H], pos[b.Doel][b.Naar], fout ? FoutKleur : BrugKleur, straalAtoom);
            }
            // achtergrondrondje onder elke letter (zodat lijnen niet door de letter lopen)
            for (int mi = 0; mi < Moleculen.Count; mi++)
                for (int i = 0; i < pos[mi].Length; i++)
                {
                    bool gekozen = mi == sleepMol && i == sleepAtoom;
                    Tekenhulp.Schijf(vh, pos[mi][i], straalAtoom * (Moleculen[mi].Mol.Atomen[i] == Element.H ? 0.8f : 1f), gekozen ? Kies : Achter, 16);
                }
            // de brug die nu getekend wordt
            if (sleepMol >= 0 && gesleept) Stippel(vh, pos[sleepMol][sleepAtoom], sleepNaar, Kies, straalAtoom);
        }

        void Stippel(VertexHelper vh, Vector2 a, Vector2 b, Color k, float marge)
        {
            float L = Vector2.Distance(a, b);
            if (L < 1f) return;
            var d = (b - a) / L;
            float stap = Mathf.Max(5f, schaal * 0.28f), r = Mathf.Max(1.8f, schaal * 0.09f);
            for (float t = marge; t <= L - marge * 0.5f; t += stap) Tekenhulp.Schijf(vh, a + d * t, r, k, 8);
        }

        // ---------- aanraken ----------

        bool Atoom(Vector2 lokaal, out int mol, out int atoom)
        {
            mol = atoom = -1; float beste = schaal * 0.6f;
            for (int mi = 0; mi < pos.Count; mi++)
                for (int i = 0; i < pos[mi].Length; i++)
                {
                    float d = Vector2.Distance(pos[mi][i], lokaal);
                    if (d < beste) { beste = d; mol = mi; atoom = i; }
                }
            return mol >= 0;
        }

        Vector2 Lokaal(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out var p);
            return p;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (Vergrendeld) return;
            gesleept = false;
            var p = Lokaal(e);
            if (Atoom(p, out int mi, out int ai) && Moleculen[mi].Mol.Atomen[ai] == Element.H)
            {
                sleepMol = mi; sleepAtoom = ai; sleepNaar = p;
                SetVerticesDirty();
            }
            else sleepMol = -1;
        }

        public void OnDrag(PointerEventData e)
        {
            if (sleepMol < 0) return;
            gesleept = true; sleepNaar = Lokaal(e);
            SetVerticesDirty();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (Vergrendeld) return;
            var p = Lokaal(e);
            if (sleepMol >= 0 && gesleept)
            {
                if (Atoom(p, out int mi, out int ai) && mi != sleepMol)
                {
                    Bruggen.Add(new HBrugRegels.Brug { Van = sleepMol, H = sleepAtoom, Doel = mi, Naar = ai });
                    Gewijzigd?.Invoke();
                }
            }
            else if (!gesleept)
            {
                // tik op een brug: weghalen
                int weg = -1; float beste = Mathf.Max(10f, schaal * 0.4f);
                for (int k = 0; k < Bruggen.Count; k++)
                {
                    var b = Bruggen[k];
                    float d = AfstandTotLijn(p, pos[b.Van][b.H], pos[b.Doel][b.Naar]);
                    if (d < beste) { beste = d; weg = k; }
                }
                if (weg >= 0) { Bruggen.RemoveAt(weg); Gewijzigd?.Invoke(); }
            }
            sleepMol = -1; gesleept = false;
            SetVerticesDirty();
        }

        static float AfstandTotLijn(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
