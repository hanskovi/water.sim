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

        static readonly float OH = (float)Constanten.AfstandOH, OO = (float)Constanten.AfstandOO;
        static readonly float HalveHoek = (float)Constanten.HoekHOH * 0.5f * Mathf.Deg2Rad;

        readonly List<Matrix4x4> mO = new List<Matrix4x4>(), mH = new List<Matrix4x4>(), mS = new List<Matrix4x4>();
        readonly List<Matrix4x4> mB = new List<Matrix4x4>(), mBK = new List<Matrix4x4>(), mBoog = new List<Matrix4x4>();
        readonly List<TextMeshPro> labels = new List<TextMeshPro>();
        Material brugMat, kernMat;
        Color brugKleur, kernKleur;
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
            }
        }

        void OnDisable()
        {
            foreach (var l in labels) if (l != null) l.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (brugMat != null) Destroy(brugMat);
            if (kernMat != null) Destroy(kernMat);
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
            mO.Clear(); mH.Clear(); mS.Clear(); mB.Clear(); mBK.Clear(); mBoog.Clear();
            int lab = 0;

            if (soort == Soort.Molecuul)
            {
                // één molecuul in het xy-vlak, O boven, H-atomen schuin onder; zachte trilling en draai
                var draai = Quaternion.Euler(4f * w * Mathf.Sin(t * 0.7f), 8f * w * Mathf.Sin(t * 0.45f), 3f * w * Mathf.Sin(t * 0.9f));
                var m = Maak(Vector3.zero, draai * Vector3.down, draai * Vector3.right, 1f + 0.015f * w * Mathf.Sin(t * 6f));
                Teken(m);
                Hoekboog(m);
                if (toonLadingen)
                {
                    var bis = ((m.h1 + m.h2) * 0.5f - m.o).normalized;
                    Label(ref lab, m.o - bis * (kalotten ? 1.75f : 0.75f), "δ−", new Color(1f, 0.62f, 0.55f));
                    Label(ref lab, m.h1 + (m.h1 - m.o).normalized * (kalotten ? 1.25f : 0.45f), "δ+", new Color(0.75f, 0.9f, 1f));
                    Label(ref lab, m.h2 + (m.h2 - m.o).normalized * (kalotten ? 1.25f : 0.45f), "δ+", new Color(0.75f, 0.9f, 1f));
                }
                Label(ref lab, m.o + ((m.h1 + m.h2) * 0.5f - m.o).normalized * (kalotten ? 1.9f : 0.95f),
                      Hoektekst(), new Color(1f, 1f, 1f, 0.9f), 2.6f, !kalotten);
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
                if (toonLadingen)
                {
                    Label(ref lab, a.o + new Vector3(-0.35f, -0.65f, 0f) * (kalotten ? 2.2f : 1f), "δ−", new Color(1f, 0.62f, 0.55f));
                    Label(ref lab, a.h1 + Vector3.up * (kalotten ? 1.25f : 0.42f), "δ+", new Color(0.75f, 0.9f, 1f));
                    Label(ref lab, a.h2 + (a.h2 - a.o).normalized * (kalotten ? 1.25f : 0.42f), "δ+", new Color(0.75f, 0.9f, 1f));
                    Label(ref lab, b.o + new Vector3(-0.1f, -0.75f, 0f) * (kalotten ? 2.2f : 1f), "δ−", new Color(1f, 0.62f, 0.55f));
                    Label(ref lab, b.h1 + (b.h1 - b.o).normalized * (kalotten ? 1.25f : 0.42f), "δ+", new Color(0.75f, 0.9f, 1f));
                    Label(ref lab, b.h2 + (b.h2 - b.o).normalized * (kalotten ? 1.25f : 0.42f), "δ+", new Color(0.75f, 0.9f, 1f));
                }
            }
            for (int i = lab; i < labels.Count; i++) labels[i].gameObject.SetActive(false);

            // tekenen
            float rO = kalotten ? 1.52f * bron.kalottenSchaal : 0.36f, rH = kalotten ? 1.20f * bron.kalottenSchaal : 0.20f;
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
                Teken(bron.cilinder, bron.staaf, mBoog);
                if (brugMat != null)
                {
                    float f = bron.pulseren ? 1f + bron.pulsSterkte * Mathf.Sin(t * bron.pulsSnelheid) : 1f;
                    brugMat.SetColor("_BaseColor", new Color(brugKleur.r * f, brugKleur.g * f, brugKleur.b * f, brugKleur.a));
                    kernMat.SetColor("_BaseColor", new Color(kernKleur.r * f, kernKleur.g * f, kernKleur.b * f, kernKleur.a));
                    var mesh = bron.brugVorm == Kristal.BrugVorm.Stippen ? bron.Stip : bron.capsule;
                    Teken(mesh, brugMat, mB);
                    Teken(mesh, kernMat, mBK);
                }
            }
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

        // boogje tussen de twee O–H-bindingen, om de hoek te laten zien (alleen bij bol-staaf)
        void Hoekboog(Mol m)
        {
            if (kalotten) return;
            var a = (m.h1 - m.o).normalized; var b = (m.h2 - m.o).normalized;
            const int stukken = 14; const float straal = 0.55f;
            var vorige = m.o + a * straal;
            for (int k = 1; k <= stukken; k++)
            {
                var p = m.o + Vector3.Slerp(a, b, k / (float)stukken) * straal;
                mBoog.Add(Staaf(vorige, p, 0.012f));
                vorige = p;
            }
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

        // tekstje in de ruimte dat altijd naar de camera kijkt
        void Label(ref int nr, Vector3 p, string tekst, Color kleur, float grootte = 3.2f, bool zichtbaar = true)
        {
            if (!zichtbaar) return;
            if (nr >= labels.Count)
            {
                var go = new GameObject("Label", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var t = go.AddComponent<TextMeshPro>();
                if (font != null) t.font = font;
                t.alignment = TextAlignmentOptions.Center;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.rectTransform.sizeDelta = new Vector2(2f, 0.6f);
                labels.Add(t);
            }
            var l = labels[nr++];
            l.gameObject.SetActive(true);
            l.text = tekst; l.color = kleur; l.fontSize = grootte;
            l.transform.position = p;
            l.transform.rotation = cam.transform.rotation;
        }
    }
}
