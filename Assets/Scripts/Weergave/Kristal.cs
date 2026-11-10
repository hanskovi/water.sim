using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using WaterSim.Sim;

namespace WaterSim.Weergave
{
    /// <summary>
    /// Laat het <see cref="Watermodel"/> zien: ijs, smelten, vloeibaar water en weer bevriezen.
    ///
    /// Oneindig: elk molecuul wordt getekend op de kopie van het periodieke blok die het dichtst bij de camera
    /// ligt; alles buiten de nevel wordt overgeslagen. Los blok: het blok staat los in de ruimte, met een
    /// stippellijn-kader voor het volume van het ijs en een blauwe lijn voor het waterniveau.
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
        [Tooltip("Temperatuur van de omgeving. Het water warmt op of koelt af naar deze temperatuur.")]
        [Range(-100, 99)] public float temperatuurOmgeving = -10f;
        public float startTemperatuur = -10f;
        [Tooltip("Trilling en warmtebeweging (trilling van ijs 2× vergroot).")]
        public bool beweging = true;
        [Tooltip("1 = trilling van ijs op ware grootte, 2 = twee keer zo groot (beter te zien).")]
        [Range(1f, 3f)] public float trillingVergroting = 2f;

        [Header("Ruimte")]
        [Tooltip("Aan: het rooster herhaalt zich in alle richtingen. Uit: een los blok, waarin je ziet dat water krimpt.")]
        public bool oneindig = true;

        [Header("Weergave")]
        public bool kalotten = false;
        public bool toonWaterstof = true;
        public bool toonBruggen = true;
        public bool markeerKanaal = false;
        [Tooltip("Bij kalotten: moleculen binnen deze afstand van de camera (Å) worden weggelaten.")]
        [Range(0f, 10f)] public float doorkijkStraal = 5f;
        [Tooltip("Kalotten: van-der-Waalsstralen (Bondi: O 1,52 Å, H 1,20 Å) maal deze factor; verhouding H : O blijft juist.")]
        [Range(0.5f, 1f)] public float kalottenSchaal = 0.85f;

        [Header("Vorm van de H-bruggen (bol-staaf)")]
        [Tooltip("Stippen: rij bolletjes met een lichte kern. Streepjes: neon-pilletjes.")]
        public BrugVorm brugVorm = BrugVorm.Stippen;
        [Range(1, 16)] public int brugDelen = 7;
        [Tooltip("Streepjes: deel van elk stuk dat gevuld is (de rest is gat).")]
        [Range(0.15f, 0.95f)] public float brugVulling = 0.4f;
        [Tooltip("Straal van een stip of streepje in Å.")]
        [Range(0.01f, 0.12f)] public float brugStraal = 0.042f;
        [Tooltip("Lichte kern in elke stip: straal t.o.v. de stip, en kleur (HDR: boven 1 gloeit hij mee).")]
        [Range(0f, 0.9f)] public float kernSchaal = 0.45f;
        [ColorUsage(true, true)] public Color kernKleur = new Color(0.75f, 0.92f, 1.3f, 0.85f);

        [Header("Pulseren van de H-bruggen")]
        public bool pulseren = true;
        public float pulsSnelheid = 2.6f;
        [Range(0f, 0.6f)] public float pulsSterkte = 0.35f;

        [Header("Beeldsnelheid")]
        [Tooltip("Op Android en iOS staat Unity standaard op 30 beelden per seconde. 60 maakt vliegen veel vloeiender.")]
        public int beeldenPerSeconde = 60;

        [Header("Nevel (Å)")]
        public float nevelStart = 6f;
        public float nevelEind = 14f;
        public Color achtergrond = new Color(0.039f, 0.082f, 0.125f);

        [Header("Onderdelen (worden ingevuld door Water.sim › Maak kristalscène)")]
        public Mesh bol;
        public Mesh cilinder;
        public Mesh capsule;
        public Material zuurstof, kanaal, waterstof, staaf, brug, rand, niveau;
        [Tooltip("Kalotten met berekende bollen (impostors): perfect rond op elke afstand, snijlijnen O–H zijn echte cirkels, en sneller. Uit = gewone bollen uit driehoekjes. Bol-staaf gebruikt altijd gewone bollen.")]
        public bool gladdeBollen = true;
        public Material zuurstofGlad, kanaalGlad, waterstofGlad;

        public Watermodel Model { get; private set; }
        /// <summary>Aantal moleculen dat dit beeld getekend is.</summary>
        public int ZichtbareMoleculen { get; private set; }
        /// <summary>Tijd voor tekenen (matrices) dit beeld, in ms.</summary>
        public float RekentijdMs { get; private set; }
        /// <summary>Tijd voor de simulatie dit beeld, in ms.</summary>
        public float SimulatieMs { get; private set; }

        public enum BrugVorm { Stippen, Streepjes }
        /// <summary>Langste H···O die als waterstofbrug getekend wordt (Å): de O–O-grens van het model min de O–H-binding, plus marge.</summary>
        const float MaxBrugLengte = 2.9f;
        const int MaxBrugDelen = 16;
        const int PulsGroepen = 6;
        const float AfstandOH = (float)Constanten.AfstandOH;

        bool[] inKanaal;
        Matrix4x4[] mO, mK, mH, mS;
        int nO, nK, nH, nS;
        readonly List<Matrix4x4>[] mB = new List<Matrix4x4>[PulsGroepen];
        readonly List<Matrix4x4>[] mBK = new List<Matrix4x4>[PulsGroepen];      // de lichte kernen
        readonly List<Matrix4x4> mRand = new List<Matrix4x4>(), mNiveau = new List<Matrix4x4>();
        Material[] brugGroep, kernGroep;
        Mesh stip;
        Color brugKleur;
        Camera cam;
        readonly System.Diagnostics.Stopwatch klok = new System.Diagnostics.Stopwatch();

        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = beeldenPerSeconde;

            Model = new Watermodel(nx, ny, nz, zaad, startTemperatuur) { Oneindig = oneindig };
            int n = Model.N;
            inKanaal = new bool[n];
            for (int i = 0; i < n; i++) inKanaal[i] = Mathf.Sqrt(Model.X[i] * Model.X[i] + Model.Y[i] * Model.Y[i]) < 3.2f;

            mO = new Matrix4x4[n]; mK = new Matrix4x4[n];
            mH = new Matrix4x4[2 * n]; mS = new Matrix4x4[2 * n];
            for (int g = 0; g < PulsGroepen; g++)
            {
                mB[g] = new List<Matrix4x4>(2 * n * 8 / PulsGroepen + 64);
                mBK[g] = new List<Matrix4x4>(2 * n * 8 / PulsGroepen + 64);
            }
            if (brug != null)
            {
                brugKleur = brug.GetColor("_BaseColor");
                brugGroep = new Material[PulsGroepen];
                kernGroep = new Material[PulsGroepen];
                for (int g = 0; g < PulsGroepen; g++)
                {
                    brugGroep[g] = new Material(brug) { name = brug.name + " " + g };
                    kernGroep[g] = new Material(brug) { name = brug.name + " kern " + g };
                }
            }
            cam = Camera.main;
            bolLaag = Icosfeer(2);
            stip = Icosfeer(1);           // 80 driehoekjes: rond genoeg voor zo'n klein bolletje
            vierkant = new Mesh { name = "Bolvierkant" };
            vierkant.SetVertices(new List<Vector3> { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) });
            vierkant.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            vierkant.bounds = new Bounds(Vector3.zero, Vector3.one * 4f);
        }

        Mesh bolLaag, vierkant;
        /// <summary>Klein bolletje voor de stippen van de H-bruggen (ook gebruikt door de molecuulscènes).</summary>
        public Mesh Stip => stip;
        /// <summary>Vierkantje voor de berekende bollen (kalotten).</summary>
        public Mesh Vierkant => vierkant;

        // bol uit driehoeken (icosaëder, n keer verfijnd); straal 0,5 zoals de Unity-bol
        static Mesh Icosfeer(int verfijn)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new List<Vector3> {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
            var f = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                                    3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            for (int r = 0; r < verfijn; r++)
            {
                var nieuw = new List<int>();
                var midden = new Dictionary<long, int>();
                int Mid(int a, int b)
                {
                    long k = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (midden.TryGetValue(k, out int m)) return m;
                    v.Add((v[a] + v[b]) * 0.5f); midden[k] = v.Count - 1; return v.Count - 1;
                }
                for (int i = 0; i < f.Count; i += 3)
                {
                    int a = f[i], b = f[i + 1], c = f[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    nieuw.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                f = nieuw;
            }
            var normalen = new Vector3[v.Count];
            for (int i = 0; i < v.Count; i++) { normalen[i] = v[i].normalized; v[i] = normalen[i] * 0.5f; }
            var m2 = new Mesh { name = "Icosfeer" };
            m2.SetVertices(v); m2.SetNormals(normalen); m2.SetTriangles(f, 0);
            m2.RecalculateBounds();
            return m2;
        }

        /// <summary>Begint opnieuw: vers ijs (of water) op <paramref name="startT"/> °C, met de omgeving op dezelfde temperatuur.</summary>
        public void Herstart(float startT, bool alsOneindig)
        {
            startTemperatuur = startT;
            temperatuurOmgeving = startT;
            oneindig = alsOneindig;
            Model = new Watermodel(nx, ny, nz, zaad, startT) { Oneindig = alsOneindig };
        }

        void OnDestroy()
        {
            if (brugGroep != null) foreach (var m in brugGroep) if (m != null) Destroy(m);
            if (kernGroep != null) foreach (var m in kernGroep) if (m != null) Destroy(m);
        }

        void Update()
        {
            klok.Restart();
            Model.Oneindig = oneindig;
            Model.Beweging = beweging;
            Model.TrillingVergroting = trillingVergroting;
            Model.Stap(Time.deltaTime, temperatuurOmgeving);
            SimulatieMs = (float)klok.Elapsed.TotalMilliseconds;
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            klok.Restart();

            Vector3 c = cam.transform.position;
            float lx = Model.Lx, ly = Model.Ly, lz = Model.Lz;

            // --- nevel ---
            float tekenStraal;
            if (oneindig)
            {
                // zo instellen dat alles wat we niet tekenen, al helemaal in de nevel zit (ook in de hoeken)
                float hoek = HoekNaarSchermhoek(cam);
                float halfBlok = Mathf.Min(lx, Mathf.Min(ly, lz)) / 2f;
                tekenStraal = Mathf.Min(nevelEind * hoek + 1f, halfBlok - 3.5f);
                float eind = Mathf.Min(nevelEind, (tekenStraal - 1f) / hoek);
                RenderSettings.fogStartDistance = Mathf.Min(nevelStart, eind - 1f);
                RenderSettings.fogEndDistance = eind;
            }
            else
            {
                float afstand = c.magnitude;
                tekenStraal = float.MaxValue;
                RenderSettings.fogStartDistance = Mathf.Max(12f, afstand - 25f);
                RenderSettings.fogEndDistance = Mathf.Max(70f, afstand + 45f);
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = achtergrond;
            cam.backgroundColor = achtergrond;

            // --- matrices ---
            float t = Time.time;
            float r2 = tekenStraal < float.MaxValue ? tekenStraal * tekenStraal : float.MaxValue;
            float rO = kalotten ? 1.52f * kalottenSchaal : 0.36f, rH = kalotten ? 1.20f * kalottenSchaal : 0.20f;
            float dichtbij = kalotten && oneindig ? doorkijkStraal : 0f, dichtbij2 = dichtbij * dichtbij;
            var X = Model.X; var Y = Model.Y; var Z = Model.Z; var H = Model.H; var acc = Model.Acceptor;
            nO = nK = nH = nS = 0;
            for (int g = 0; g < PulsGroepen; g++) { mB[g].Clear(); mBK[g].Clear(); }
            int zichtbaar = 0, n = Model.N;
            for (int i = 0; i < n; i++)
            {
                float x = X[i], y = Y[i], z = Z[i];
                if (oneindig) { x = c.x + Watermodel.Kortst(x - c.x, lx); y = c.y + Watermodel.Kortst(y - c.y, ly); z = c.z + Watermodel.Kortst(z - c.z, lz); }
                float dx = x - c.x, dy = y - c.y, dz = z - c.z;
                float d2 = dx * dx + dy * dy + dz * dz;
                if (d2 > r2 || d2 < dichtbij2) continue;
                zichtbaar++;
                var o = new Vector3(x, y, z);

                if (markeerKanaal && inKanaal[i] && !Model.Vloeibaar[i]) mK[nK++] = Bol(o, rO); else mO[nO++] = Bol(o, rO);

                for (int k = 0; k < 2; k++)
                {
                    int h = 6 * i + 3 * k;
                    var hp = o + new Vector3(H[h], H[h + 1], H[h + 2]) * AfstandOH;
                    if (toonWaterstof)
                    {
                        mH[nH++] = Bol(hp, rH);
                        if (!kalotten) mS[nS++] = Staaf(o, hp, 0.09f);
                    }
                    if (!toonBruggen || kalotten) continue;
                    int j = acc[2 * i + k];
                    if (j < 0) continue;
                    float ax = X[j] - X[i], ay = Y[j] - Y[i], az = Z[j] - Z[i];
                    if (oneindig) { ax = Watermodel.Kortst(ax, lx); ay = Watermodel.Kortst(ay, ly); az = Watermodel.Kortst(az, lz); }
                    int brugNr = 2 * i + k;
                    int groep = (int)((uint)(brugNr * 2654435761u) >> 16) % PulsGroepen;
                    Brug(hp, o + new Vector3(ax, ay, az), mB[groep], mBK[groep]);
                }
            }
            ZichtbareMoleculen = zichtbaar;

            // --- kader van het ijsvolume en het waterniveau (alleen los blok) ---
            mRand.Clear(); mNiveau.Clear();
            if (!oneindig)
            {
                Kader(mRand, new Vector3(lx, ly, lz) * 0.5f, 0.08f);
                if (Model.Fase.Gesmolten > 0.02) Niveau(mNiveau, lx * 0.5f, lz * 0.5f, Model.Bovenkant, 0.1f);
            }

            if (kalotten && gladdeBollen && zuurstofGlad != null && waterstofGlad != null)
            {
                // kalotten: berekende bollen, één vierkantje per atoom; de shader rekent de echte bol uit
                Teken(vierkant, zuurstofGlad, mO, nO);
                Teken(vierkant, kanaalGlad != null ? kanaalGlad : zuurstofGlad, mK, nK);
                Teken(vierkant, waterstofGlad, mH, nH);
            }
            else
            {
                // bol-staaf: gewone bollen; in een los blok met kalotten (zonder gladde bollen) met minder vlakjes
                var bolNu = kalotten && !oneindig && bolLaag != null ? bolLaag : bol;
                Teken(bolNu, zuurstof, mO, nO);
                Teken(bolNu, kanaal, mK, nK);
                Teken(bolNu, waterstof, mH, nH);
            }
            Teken(cilinder, staaf, mS, nS);
            Teken(cilinder, rand, mRand);
            Teken(cilinder, niveau, mNiveau);
            if (brugGroep != null)
                for (int g = 0; g < PulsGroepen; g++)
                {
                    float f = pulseren ? 1f + pulsSterkte * Mathf.Sin(t * pulsSnelheid + g * (2f * Mathf.PI / PulsGroepen)) : 1f;
                    brugGroep[g].SetColor("_BaseColor", new Color(brugKleur.r * f, brugKleur.g * f, brugKleur.b * f, brugKleur.a));
                    kernGroep[g].SetColor("_BaseColor", new Color(kernKleur.r * f, kernKleur.g * f, kernKleur.b * f, kernKleur.a));
                    bool stippen = brugVorm == BrugVorm.Stippen;
                    Teken(stippen ? stip : capsule, brugGroep[g], mB[g]);
                    Teken(stippen ? stip : capsule, kernGroep[g], mBK[g]);   // na de buitenkant: de kern ligt erover
                }
            RekentijdMs = (float)klok.Elapsed.TotalMilliseconds;
        }

        /// <summary>Waar de camera in een los blok goed staat om het hele blok te zien.</summary>
        public Vector3 BlokKijkpunt => new Vector3(Model.Lx * 0.5f + 30f, 16f, Model.Lz * 0.5f + 34f);

        // gestippelde H-brug van het H-atoom naar de acceptor: stippen of neon-pilletjes, elk met een lichte kern
        public void Brug(Vector3 h, Vector3 a, List<Matrix4x4> lijst, List<Matrix4x4> kernen)
        {
            var v = a - h;
            float L = v.magnitude;
            if (L < 0.8f || L > MaxBrugLengte) return;      // langer dan een echte H-brug (H···O): niet tekenen
            v /= L;
            int delen = Mathf.Clamp(brugDelen, 1, MaxBrugDelen);
            float s0 = 0.24f, s1 = L - 0.40f, stuk = (s1 - s0) / delen;
            for (int q = 0; q < delen; q++)
            {
                float m = s0 + (q + 0.5f) * stuk;
                if (brugVorm == BrugVorm.Stippen)
                {
                    var p = h + v * m;
                    lijst.Add(Bol(p, brugStraal));
                    if (kernSchaal > 0f) kernen.Add(Bol(p, brugStraal * kernSchaal));
                }
                else
                {
                    float half = stuk * brugVulling * 0.5f;
                    lijst.Add(Staaf(h + v * (m - half), h + v * (m + half), brugStraal));
                    if (kernSchaal > 0f) kernen.Add(Staaf(h + v * (m - half), h + v * (m + half), brugStraal * kernSchaal));
                }
            }
        }

        // 12 ribben van een balk rond de oorsprong (halve maten hm)
        static void Kader(List<Matrix4x4> lijst, Vector3 hm, float r)
        {
            for (int a = 0; a < 3; a++)
                for (int s1 = -1; s1 <= 1; s1 += 2)
                    for (int s2 = -1; s2 <= 1; s2 += 2)
                    {
                        Vector3 p = Vector3.zero, q = Vector3.zero;
                        int b = (a + 1) % 3, c = (a + 2) % 3;
                        p[a] = -hm[a]; q[a] = hm[a];
                        p[b] = q[b] = s1 * hm[b];
                        p[c] = q[c] = s2 * hm[c];
                        lijst.Add(Staaf(p, q, r));
                    }
        }

        static void Niveau(List<Matrix4x4> lijst, float hx, float hz, float y, float r)
        {
            var a = new Vector3(-hx, y, -hz); var b = new Vector3(hx, y, -hz);
            var c = new Vector3(hx, y, hz); var d = new Vector3(-hx, y, hz);
            lijst.Add(Staaf(a, b, r)); lijst.Add(Staaf(b, c, r)); lijst.Add(Staaf(c, d, r)); lijst.Add(Staaf(d, a, r));
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

        RenderParams Params(Material mat) => new RenderParams(mat)
        {
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false,
            worldBounds = new Bounds(Vector3.zero, Vector3.one * 100000f),
        };

        void Teken(Mesh mesh, Material mat, Matrix4x4[] lijst, int aantal)
        {
            if (aantal == 0 || mesh == null || mat == null) return;
            var rp = Params(mat);
            for (int s = 0; s < aantal; s += 1023)
                Graphics.RenderMeshInstanced(rp, mesh, 0, lijst, Mathf.Min(1023, aantal - s), s);
        }

        void Teken(Mesh mesh, Material mat, List<Matrix4x4> lijst)
        {
            int aantal = lijst.Count;
            if (aantal == 0 || mesh == null || mat == null) return;
            var rp = Params(mat);
            for (int s = 0; s < aantal; s += 1023)
                Graphics.RenderMeshInstanced(rp, mesh, 0, lijst, Mathf.Min(1023, aantal - s), s);
        }

        // Hoeveel verder de hoek van het beeld ligt dan het midden: afstand / diepte in de beeldhoek.
        static float HoekNaarSchermhoek(Camera c)
        {
            float tv = Mathf.Tan(c.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float th = tv * c.aspect;
            return Mathf.Sqrt(1f + tv * tv + th * th);
        }
    }
}
