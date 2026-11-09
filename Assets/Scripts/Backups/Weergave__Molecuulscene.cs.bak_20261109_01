using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using WaterSim.Sim;

namespace WaterSim.Weergave
{
    /// <summary>
    /// Kleine scènes voor level 1 en 2: één watermolecuul (hoek, δ+ en δ−) of twee moleculen met een waterstofbrug.
    /// Gebruikt de meshes en materialen van <see cref="Kristal"/>, zodat alles er hetzelfde uitziet.
    /// Afstanden en hoeken op schaal (Å, Binas); bij bol-staaf zijn de atomen verkleind, zoals in het kristal.
    /// De labels (δ+, δ−, de hoek) liggen in een plat vlak bovenop het beeld: ze verdwijnen nooit achter een atoom.
    /// </summary>
    public class Molecuulscene : MonoBehaviour
    {
        public enum Soort { Molecuul, Paar }

        public Kristal bron;
        public TMP_FontAsset font;
        public Soort soort = Soort.Molecuul;
        public bool kalotten;
        public bool toonLadingen = true;
        public bool beweging = true;

        /// <summary>Straal (Å) van een bol om het origin waar alles in past, ook de labels. Voor het kiezen van de camera-afstand.</summary>
        public float Straal { get; private set; } = 2f;

        static readonly float OH = (float)Constanten.AfstandOH, OO = (float)Constanten.AfstandOO;
        static readonly float HalveHoek = (float)Constanten.HoekHOH * 0.5f * Mathf.Deg2Rad;
        static readonly Color DeltaMin = new Color(1f, 0.66f, 0.6f), DeltaPlus = new Color(0.78f, 0.92f, 1f);

        readonly List<Matrix4x4> mO = new List<Matrix4x4>(), mH = new List<Matrix4x4>(), mS = new List<Matrix4x4>();
        readonly List<Matrix4x4> mB = new List<Matrix4x4>(), mBK = new List<Matrix4x4>(), mBoog = new List<Matrix4x4>();
        readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
        Material brugMat, kernMat, vlakMat, randMat;
        Color brugKleur, kernKleur;
        Mesh hoekvlak;
        Canvas labelDoek;
        Camera cam;

        // één molecuul: O en de twee H-atomen
        struct Mol { public Vector3 o, h1, h2; }

        void OnEnable()
        {
            if (bron == null) return;
            if (brugMat == null && bron.brug != null)
            {
                brugMat = new Material(bron.brug) { name = "Brug (molecuul)" };
                kernMat = new Material(bron.brug) { name = "Brugkern (molecuul)" };
                brugKleur = bron.brug.GetColor("_BaseColor");
                kernKleur = bron.kernKleur;
                // hoek: licht, doorzichtig vlakje met een heldere rand (geen HDR, dus geen gloed)
                vlakMat = new Material(bron.brug) { name = "Hoekvlak" };
                vlakMat.SetColor("_BaseColor", new Color(0.85f, 0.93f, 1f, 0.22f));
                randMat = new Material(bron.brug) { name = "Hoekrand" };
                randMat.SetColor("_BaseColor", new Color(0.9f, 0.96f, 1f, 0.9f));
            }
            if (hoekvlak == null) hoekvlak = new Mesh { name = "Hoekvlak" };
            if (labelDoek == null)
            {
                var go = new GameObject("Labels", typeof(RectTransform), typeof(Canvas));
                go.transform.SetParent(transform, false);
                labelDoek = go.GetComponent<Canvas>();
                labelDoek.renderMode = RenderMode.ScreenSpaceOverlay;
                labelDoek.sortingOrder = 50;                 // boven het 3D-beeld, onder de knoppen
            }
            labelDoek.gameObject.SetActive(true);
        }

        void OnDisable()
        {
            if (labelDoek != null) labelDoek.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            foreach (var m in new[] { brugMat, kernMat, vlakMat, randMat }) if (m != null) Destroy(m);
            if (hoekvlak != null) Destroy(hoekvlak);
        }

        void LateUpdate()
        {
            if (bron == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            RenderSettings.fog = false;                      // zo dichtbij is nevel alleen maar storend
            cam.backgroundColor = bron.achtergrond;

            float t = Time.time;
            float w = beweging ? 1f : 0f;
            float rO = kalotten ? 1.52f * bron.kalottenSchaal : 0.36f, rH = kalotten ? 1.20f * bron.kalottenSchaal : 0.20f;
            mO.Clear(); mH.Clear(); mS.Clear(); mB.Clear(); mBK.Clear(); mBoog.Clear();
            int lab = 0;
            bool toonHoek = false;

            if (soort == Soort.Molecuul)
            {
                // één molecuul in het xy-vlak, O boven, H-atomen schuin onder; zachte trilling en draai
                var draai = Quaternion.Euler(4f * w * Mathf.Sin(t * 0.7f), 8f * w * Mathf.Sin(t * 0.45f), 3f * w * Mathf.Sin(t * 0.9f));
                var m = Maak(Vector3.zero, draai * Vector3.down, draai * Vector3.right, 1f + 0.015f * w * Mathf.Sin(t * 6f));
                Teken(m);
                if (!kalotten)
                {
                    toonHoek = true;
                    Hoek(m);
                    var bis = ((m.h1 + m.h2) * 0.5f - m.o).normalized;
                    Label(ref lab, m.o + bis * (OH + 0.45f), Hoektekst(), Color.white, 14f);
                }
                if (toonLadingen) Ladingen(ref lab, m, rO, rH);
            }
            else
            {
                // twee moleculen: A geeft een H (donor), B ontvangt met een vrij elektronenpaar (acceptor).
                // O–H···O op één lijn langs x; B staat tetraëdrisch: zijn H-atomen wijzen van de brug af.
                float trek = 0.06f * w * Mathf.Sin(t * 1.3f);                 // de brug "ademt" een beetje
                var oA = new Vector3(-OO * 0.5f - trek * 0.5f, 0f, 0f);
                var oB = new Vector3(OO * 0.5f + trek * 0.5f, 0f, 0f);
                var a = new Mol { o = oA, h1 = oA + Vector3.right * OH };
                float hoek = (float)Constanten.HoekHOH * Mathf.Deg2Rad;
                a.h2 = oA + new Vector3(Mathf.Cos(hoek), Mathf.Sin(hoek), 0f) * OH;
                var draaiB = Quaternion.AngleAxis(10f * w * Mathf.Sin(t * 0.8f), Vector3.right);
                float vp = 0.5f * (float)Constanten.HoekTetraeder * Mathf.Deg2Rad;    // vrije paren: halve tetraëderhoek
                var bisH = draaiB * new Vector3(Mathf.Cos(vp), 0f, -Mathf.Sin(vp));   // tegenover de vrije paren
                var b = Maak(oB, bisH, draaiB * Vector3.up, 1f);
                Teken(a); Teken(b);
                bron.Brug(a.h1, b.o, mB, mBK);
                if (toonLadingen) { Ladingen(ref lab, a, rO, rH); Ladingen(ref lab, b, rO, rH); }
            }
            for (int i = lab; i < labels.Count; i++) labels[i].gameObject.SetActive(false);

            // straal van alles wat in beeld moet (atomen + ruimte voor de labels)
            float straal = 0f;
            foreach (var m in mO) straal = Mathf.Max(straal, ((Vector3)m.GetColumn(3)).magnitude + rO);
            foreach (var m in mH) straal = Mathf.Max(straal, ((Vector3)m.GetColumn(3)).magnitude + rH);
            Straal = straal + 0.45f;

            // tekenen
            Schaal(mO, rO); Schaal(mH, rH);
            if (kalotten && bron.zuurstofGlad != null && bron.waterstofGlad != null)
            {
                Teken(bron.Vierkant, bron.zuurstofGlad, mO);
                Teken(bron.Vierkant, bron.waterstofGlad, mH);
            }
            else
            {
                Teken(bron.bol, bron.zuurstof, mO);
                Teken(bron.bol, bron.waterstof, mH);
                Teken(bron.cilinder, bron.staaf, mS);
                if (brugMat != null)
                {
                    float f = bron.pulseren ? 1f + bron.pulsSterkte * Mathf.Sin(t * bron.pulsSnelheid) : 1f;
                    brugMat.SetColor("_BaseColor", new Color(brugKleur.r * f, brugKleur.g * f, brugKleur.b * f, brugKleur.a));
                    kernMat.SetColor("_BaseColor", new Color(kernKleur.r * f, kernKleur.g * f, kernKleur.b * f, kernKleur.a));
                    var mesh = bron.brugVorm == Kristal.BrugVorm.Stippen ? bron.Stip : bron.capsule;
                    Teken(mesh, brugMat, mB);
                    Teken(mesh, kernMat, mBK);
                }
                if (toonHoek && vlakMat != null)
                {
                    Graphics.RenderMesh(new RenderParams(vlakMat) { shadowCastingMode = ShadowCastingMode.Off }, hoekvlak, 0, Matrix4x4.identity);
                    Teken(bron.cilinder, randMat, mBoog);
                }
            }
        }

        // δ− naast de O (van de H-atomen af), δ+ voorbij elk H-atoom
        void Ladingen(ref int lab, Mol m, float rO, float rH)
        {
            var weg = -((m.h1 - m.o).normalized + (m.h2 - m.o).normalized).normalized;
            Label(ref lab, m.o + weg * (rO + 0.3f), "δ−", DeltaMin, 17f);
            Label(ref lab, m.h1 + (m.h1 - m.o).normalized * (rH + 0.28f), "δ+", DeltaPlus, 17f);
            Label(ref lab, m.h2 + (m.h2 - m.o).normalized * (rH + 0.28f), "δ+", DeltaPlus, 17f);
        }

        // molecuul met O op o; de H-atomen liggen rond 'bisectrice' in het vlak met 'inVlak'
        static Mol Maak(Vector3 o, Vector3 bisectrice, Vector3 inVlak, float rek)
        {
            bisectrice.Normalize();
            var zij = Vector3.Cross(bisectrice, Vector3.Cross(inVlak, bisectrice)).normalized;
            if (zij.sqrMagnitude < 0.5f) zij = Vector3.Cross(bisectrice, Vector3.forward).normalized;
            // 'zij' staat loodrecht op de bisectrice; de H-atomen liggen op ± de halve hoek
            float c = Mathf.Cos(HalveHoek), s = Mathf.Sin(HalveHoek);
            return new Mol
            {
                o = o,
                h1 = o + (bisectrice * c + zij * s) * OH * rek,
                h2 = o + (bisectrice * c - zij * s) * OH * rek,
            };
        }

        void Teken(Mol m)
        {
            mO.Add(Matrix4x4.Translate(m.o));
            mH.Add(Matrix4x4.Translate(m.h1));
            mH.Add(Matrix4x4.Translate(m.h2));
            if (!kalotten)
            {
                mS.Add(Staaf(m.o, m.h1, 0.09f));
                mS.Add(Staaf(m.o, m.h2, 0.09f));
            }
        }

        // taartpunt tussen de twee O–H-bindingen (dubbelzijdig) met een heldere boogrand, zoals in het schoolboek
        void Hoek(Mol m)
        {
            var a = (m.h1 - m.o).normalized; var b = (m.h2 - m.o).normalized;
            const int stukken = 20; const float straal = 0.62f;
            var v = new List<Vector3> { m.o };
            for (int k = 0; k <= stukken; k++) v.Add(m.o + Vector3.Slerp(a, b, k / (float)stukken) * straal);
            var d = new List<int>();
            for (int k = 1; k <= stukken; k++) { d.Add(0); d.Add(k); d.Add(k + 1); d.Add(0); d.Add(k + 1); d.Add(k); }
            hoekvlak.Clear();
            hoekvlak.SetVertices(v);
            hoekvlak.SetTriangles(d, 0);
            hoekvlak.RecalculateBounds();
            for (int k = 1; k <= stukken; k++) mBoog.Add(Staaf(v[k], v[k + 1], 0.022f));
        }

        static string Hoektekst() => ((float)Constanten.HoekHOH).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
            .Replace('.', UI.Teksten.Huidig == UI.Taal.NL ? ',' : '.') + "°";

        static void Schaal(List<Matrix4x4> lijst, float r)
        {
            for (int i = 0; i < lijst.Count; i++)
                lijst[i] = Matrix4x4.TRS(lijst[i].GetColumn(3), Quaternion.identity, Vector3.one * (2f * r));
        }

        static Matrix4x4 Staaf(Vector3 a, Vector3 b, float straal)
        {
            var d = b - a;
            float L = d.magnitude;
            var rot = L > 1e-5f ? Quaternion.FromToRotation(Vector3.up, d / L) : Quaternion.identity;
            return Matrix4x4.TRS((a + b) * 0.5f, rot, new Vector3(2f * straal, L * 0.5f, 2f * straal));
        }

        static void Teken(Mesh mesh, Material mat, List<Matrix4x4> lijst)
        {
            if (mesh == null || mat == null || lijst.Count == 0) return;
            var rp = new RenderParams(mat) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false, worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f) };
            Graphics.RenderMeshInstanced(rp, mesh, 0, lijst);
        }

        // label in het platte vlak bovenop het beeld, op de plek waar dit punt in beeld staat
        void Label(ref int nr, Vector3 p, string tekst, Color kleur, float grootte)
        {
            var s = cam.WorldToScreenPoint(p);
            if (s.z <= 0f) return;
            if (nr >= labels.Count)
            {
                var go = new GameObject("Label", typeof(RectTransform));
                go.transform.SetParent(labelDoek.transform, false);
                var l = go.AddComponent<TextMeshProUGUI>();
                if (font != null) l.font = font;
                l.alignment = TextAlignmentOptions.Center;
                l.textWrappingMode = TextWrappingModes.NoWrap;
                l.raycastTarget = false;
                l.fontStyle = FontStyles.Bold;
                l.outlineWidth = 0.22f;
                l.outlineColor = new Color32(6, 20, 32, 220);
                l.rectTransform.sizeDelta = new Vector2(200f, 60f);
                labels.Add(l);
            }
            var t = labels[nr++];
            t.gameObject.SetActive(true);
            t.text = tekst; t.color = kleur;
            t.fontSize = grootte * Mathf.Min(Screen.width / 390f, Screen.height / 844f);   // zelfde maat als de rest van de interface
            t.rectTransform.position = new Vector3(s.x, s.y, 0f);
        }
    }
}
