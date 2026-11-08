using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using WaterSim.Sim;

namespace WaterSim.Weergave
{
    /// <summary>
    /// Tekent het ijskristal als een oneindig rooster rond de camera.
    ///
    /// Elk molecuul wordt getekend op de kopie van het (periodieke) blok die het dichtst bij de camera ligt.
    /// Wat verder weg ligt dan de nevel, wordt niet getekend. De nevel loopt af naar de achtergrondkleur,
    /// zodat het rooster in de verte in het donker verdwijnt en je het verspringen nooit ziet.
    ///
    /// Alles wordt met GPU-instancing getekend: één tekenopdracht per soort onderdeel.
    /// </summary>
    public class Kristal : MonoBehaviour
    {
        [Header("Rooster (oneven NX en NY: camera start in een kanaal)")]
        public int nx = 11, ny = 7, nz = 7;
        [Tooltip("Zelfde getal = zelfde verdeling van de H-atomen.")]
        public int zaad = 1;

        [Header("Temperatuur")]
        [Range(-200, 0)] public float temperatuur = -10f;
        [Tooltip("Trilling van de moleculen (2× vergroot, anders zie je haar niet).")]
        public bool beweging = true;

        [Header("Weergave")]
        public bool kalotten = false;
        public bool toonWaterstof = true;
        public bool toonBruggen = true;
        public bool markeerKanaal = false;
        [Tooltip("Bij kalotten: moleculen binnen deze afstand van de camera (Å) worden weggelaten, " +
                 "zodat je vanuit een lege bel naar het rooster kijkt.")]
        [Range(0f, 10f)] public float doorkijkStraal = 5f;

        [Header("Pulseren van de H-bruggen")]
        public bool pulseren = true;
        public float pulsSnelheid = 2.6f;
        [Range(0f, 0.6f)] public float pulsSterkte = 0.35f;

        [Header("Nevel (Å)")]
        public float nevelStart = 6f;
        public float nevelEind = 14f;
        public Color achtergrond = new Color(0.039f, 0.082f, 0.125f);

        [Header("Onderdelen (worden ingevuld door Water.sim › Maak kristalscène)")]
        public Mesh bol;
        public Mesh cilinder;
        public Mesh capsule;
        public Material zuurstof, kanaal, waterstof, staaf, brug;

        public IJsrooster Rooster { get; private set; }
        /// <summary>Aantal moleculen dat dit beeld getekend is.</summary>
        public int ZichtbareMoleculen { get; private set; }
        /// <summary>Tijd die het opbouwen van dit beeld op de processor kostte (ms).</summary>
        public float RekentijdMs { get; private set; }

        const int BrugStreepjes = 4;
        // De bruggen zijn verdeeld over een paar groepen met elk een eigen materiaal; elke groep pulseert
        // in een andere fase. Zo knippert het netwerk zacht, zonder eigen shader.
        const int PulsGroepen = 6;
        const float AfstandOH = (float)Constanten.AfstandOH;

        float lx, ly, lz;
        float[] ox, oy, oz;          // roosterplekken
        float[] hx, hy, hz;          // richtingen van de H-atomen [i*2+k]
        int[] acceptor;              // per H: het O-atoom dat de brug ontvangt
        bool[] inKanaal;
        float[] fase, freq;
        float[] px, py, pz;          // huidige posities (met trilling)

        Matrix4x4[] mO, mK, mH, mS;
        int nO, nK, nH, nS;
        readonly List<Matrix4x4>[] mB = new List<Matrix4x4>[PulsGroepen];
        Material[] brugGroep;
        Color brugKleur;
        Camera cam;
        readonly System.Diagnostics.Stopwatch klok = new System.Diagnostics.Stopwatch();

        void Awake()
        {
            Rooster = new IJsrooster(nx, ny, nz);
            IJsregels.Pas(Rooster, zaad);
            IJsregels.HAtomen(Rooster, out var dhx, out var dhy, out var dhz, out acceptor, out _);

            int n = Rooster.Aantal;
            lx = (float)Rooster.Lx; ly = (float)Rooster.Ly; lz = (float)Rooster.Lz;
            ox = new float[n]; oy = new float[n]; oz = new float[n];
            px = new float[n]; py = new float[n]; pz = new float[n];
            hx = new float[2 * n]; hy = new float[2 * n]; hz = new float[2 * n];
            inKanaal = new bool[n];
            fase = new float[3 * n]; freq = new float[3 * n];
            var rnd = new System.Random(zaad);
            for (int i = 0; i < n; i++)
            {
                ox[i] = (float)Rooster.X[i]; oy[i] = (float)Rooster.Y[i]; oz[i] = (float)Rooster.Z[i];
                inKanaal[i] = Mathf.Sqrt(ox[i] * ox[i] + oy[i] * oy[i]) < 3.2f;
                for (int a = 0; a < 3; a++) { fase[3 * i + a] = (float)(rnd.NextDouble() * 6.283); freq[3 * i + a] = 7f + (float)rnd.NextDouble() * 5f; }
            }
            for (int k = 0; k < 2 * n; k++) { hx[k] = (float)dhx[k]; hy[k] = (float)dhy[k]; hz[k] = (float)dhz[k]; }

            mO = new Matrix4x4[n]; mK = new Matrix4x4[n];
            mH = new Matrix4x4[2 * n]; mS = new Matrix4x4[2 * n];
            for (int g = 0; g < PulsGroepen; g++) mB[g] = new List<Matrix4x4>(2 * n * BrugStreepjes / PulsGroepen + 64);
            if (brug != null)
            {
                brugKleur = brug.GetColor("_BaseColor");
                brugGroep = new Material[PulsGroepen];
                for (int g = 0; g < PulsGroepen; g++) brugGroep[g] = new Material(brug) { name = brug.name + " " + g };
            }
            cam = Camera.main;
        }

        void OnDestroy()
        {
            if (brugGroep == null) return;
            foreach (var m in brugGroep) if (m != null) Destroy(m);
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            klok.Restart();

            // --- nevel: zo instellen dat alles wat we niet tekenen, al helemaal in de nevel zit ---
            float hoek = HoekNaarSchermhoek(cam);
            float halfBlok = Mathf.Min(lx, Mathf.Min(ly, lz)) / 2f;
            float tekenStraal = Mathf.Min(nevelEind * hoek + 1f, halfBlok - 3.5f);
            float eind = Mathf.Min(nevelEind, (tekenStraal - 1f) / hoek);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = achtergrond;
            RenderSettings.fogStartDistance = Mathf.Min(nevelStart, eind - 1f);
            RenderSettings.fogEndDistance = eind;
            cam.backgroundColor = achtergrond;

            // --- trilling ---
            float t = Time.time;
            float amp = beweging ? 2f * 0.15f * Mathf.Sqrt((temperatuur + 273.15f) / 273.15f) : 0f;
            int n = ox.Length;
            for (int i = 0; i < n; i++)
            {
                px[i] = ox[i] + amp * Mathf.Sin(t * freq[3 * i] + fase[3 * i]);
                py[i] = oy[i] + amp * Mathf.Sin(t * freq[3 * i + 1] + fase[3 * i + 1]);
                pz[i] = oz[i] + amp * Mathf.Sin(t * freq[3 * i + 2] + fase[3 * i + 2]);
            }

            // --- matrices voor alles binnen de tekenstraal ---
            Vector3 c = cam.transform.position;
            float r2 = tekenStraal * tekenStraal;
            // Kalotten: iets kleiner dan van der Waals (O 1,52 / H 1,2 Å), anders past de camera niet
            // in een kanaal. Moleculen die de camera raken, laten we weg ("doorkijk").
            float rO = kalotten ? 1.25f : 0.36f, rH = kalotten ? 0.95f : 0.20f;
            float dichtbij = kalotten ? doorkijkStraal : 0f, dichtbij2 = dichtbij * dichtbij;
            nO = nK = nH = nS = 0;
            for (int g = 0; g < PulsGroepen; g++) mB[g].Clear();
            int zichtbaar = 0;
            for (int i = 0; i < n; i++)
            {
                float x = c.x + Kortst(px[i] - c.x, lx);
                float y = c.y + Kortst(py[i] - c.y, ly);
                float z = c.z + Kortst(pz[i] - c.z, lz);
                float dx = x - c.x, dy = y - c.y, dz = z - c.z;
                float d2 = dx * dx + dy * dy + dz * dz;
                if (d2 > r2 || d2 < dichtbij2) continue;
                zichtbaar++;
                var o = new Vector3(x, y, z);

                if (markeerKanaal && inKanaal[i]) mK[nK++] = Bol(o, rO); else mO[nO++] = Bol(o, rO);

                for (int k = 0; k < 2; k++)
                {
                    int h = 2 * i + k;
                    var hp = o + new Vector3(hx[h], hy[h], hz[h]) * AfstandOH;
                    if (toonWaterstof)
                    {
                        mH[nH++] = Bol(hp, rH);
                        if (!kalotten) mS[nS++] = Staaf(o, hp, 0.09f);
                    }
                    if (!toonBruggen || kalotten) continue;
                    int j = acceptor[h];
                    var ap = o + new Vector3(Kortst(px[j] - px[i], lx), Kortst(py[j] - py[i], ly), Kortst(pz[j] - pz[i], lz));
                    Streepjes(hp, ap, mB[(int)((uint)(h * 2654435761u) >> 16) % PulsGroepen]);
                }
            }
            ZichtbareMoleculen = zichtbaar;

            Teken(bol, zuurstof, mO, nO);
            Teken(bol, kanaal, mK, nK);
            Teken(bol, waterstof, mH, nH);
            Teken(cilinder, staaf, mS, nS);
            if (brugGroep != null)
                for (int g = 0; g < PulsGroepen; g++)
                {
                    float f = pulseren ? 1f + pulsSterkte * Mathf.Sin(t * pulsSnelheid + g * (2f * Mathf.PI / PulsGroepen)) : 1f;
                    var kleur = new Color(brugKleur.r * f, brugKleur.g * f, brugKleur.b * f, brugKleur.a);
                    brugGroep[g].SetColor("_BaseColor", kleur);
                    Teken(capsule, brugGroep[g], mB[g]);
                }
            RekentijdMs = (float)klok.Elapsed.TotalMilliseconds;
        }

        // gestippelde H-brug van het H-atoom naar de acceptor: 4 neon-pilletjes
        void Streepjes(Vector3 h, Vector3 a, List<Matrix4x4> lijst)
        {
            var v = a - h;
            float L = v.magnitude;
            if (L < 0.8f) return;
            v /= L;
            float s0 = 0.24f, s1 = L - 0.40f, stuk = (s1 - s0) / BrugStreepjes, lengte = stuk * 0.55f;
            for (int q = 0; q < BrugStreepjes; q++)
            {
                float a0 = s0 + q * stuk + (stuk - lengte) / 2f;
                lijst.Add(Staaf(h + v * a0, h + v * (a0 + lengte), 0.06f));
            }
        }

        static Matrix4x4 Bol(Vector3 p, float straal) => Matrix4x4.TRS(p, Quaternion.identity, Vector3.one * (2f * straal));

        // Unity-cilinder en -capsule: hoogte 2, straal 0,5, langs y
        static Matrix4x4 Staaf(Vector3 a, Vector3 b, float straal)
        {
            var d = b - a;
            float L = d.magnitude;
            var rot = L > 1e-5f ? Quaternion.FromToRotation(Vector3.up, d / L) : Quaternion.identity;
            return Matrix4x4.TRS((a + b) * 0.5f, rot, new Vector3(2f * straal, L * 0.5f, 2f * straal));
        }

        void Teken(Mesh mesh, Material mat, Matrix4x4[] lijst, int aantal)
        {
            if (aantal == 0 || mesh == null || mat == null) return;
            var rp = new RenderParams(mat)
            {
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                worldBounds = new Bounds(cam.transform.position, Vector3.one * 200f),
            };
            for (int s = 0; s < aantal; s += 1023)
                Graphics.RenderMeshInstanced(rp, mesh, 0, lijst, Mathf.Min(1023, aantal - s), s);
        }

        void Teken(Mesh mesh, Material mat, List<Matrix4x4> lijst)
        {
            int aantal = lijst.Count;
            if (aantal == 0 || mesh == null || mat == null) return;
            var rp = new RenderParams(mat)
            {
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                worldBounds = new Bounds(cam.transform.position, Vector3.one * 200f),
            };
            for (int s = 0; s < aantal; s += 1023)
                Graphics.RenderMeshInstanced(rp, mesh, 0, lijst, Mathf.Min(1023, aantal - s), s);
        }

        static float Kortst(float d, float L) => d - L * Mathf.Round(d / L);

        // Hoeveel verder de hoek van het beeld ligt dan het midden: afstand / diepte in de beeldhoek.
        static float HoekNaarSchermhoek(Camera c)
        {
            float tv = Mathf.Tan(c.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float th = tv * c.aspect;
            return Mathf.Sqrt(1f + tv * tv + th * th);
        }
    }
}
