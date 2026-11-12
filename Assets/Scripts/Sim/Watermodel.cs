using System;

namespace WaterSim.Sim
{
    /// <summary>
    /// Alle watermoleculen samen: ijs dat trilt, smelt, vloeibaar water met H-bruggen die breken en opnieuw
    /// vormen, en weer bevriest. De weergave leest alleen de openbare arrays.
    ///
    /// Twee ruimtes:
    /// - Oneindig: het blok herhaalt zich in alle richtingen (periodiek). Smelten begint bij een paar punten.
    /// - Los blok: wanden rondom. Smelten gaat van buiten naar binnen en het water zakt in
    ///   (<see cref="Bovenkant"/>), omdat water dichter is dan ijs.
    ///
    /// Model, geen echte moleculaire dynamica: zachte bollen met warmtebeweging, H-atomen die naar een buur
    /// draaien, en een H-brug als de hoek en de afstand kloppen. Afgesteld op ≈ 3,5 bruggen per molecuul
    /// bij 0 °C en ≈ 2,9 bij 90 °C.
    /// </summary>
    public class Watermodel
    {
        public readonly IJsrooster Rooster;
        public readonly int N;
        public readonly Fasemodel Fase;
        public readonly float Lx, Ly, Lz;

        /// <summary>Oneindig (periodiek) of een los blok met wanden.</summary>
        public bool Oneindig = true;
        /// <summary>Trilling en warmtebeweging aan/uit (uit = stilstaand beeld).</summary>
        public bool Beweging = true;
        /// <summary>Hoeveel keer de trilling van ijs vergroot wordt (1 = ware grootte; 2 = beter zichtbaar).</summary>
        public float TrillingVergroting = 2f;
        /// <summary>Rekenwerk van de vloeistof verdelen over alle processorkernen.</summary>
        public bool Parallel = true;

        // ---------- uitvoer voor de weergave ----------
        /// <summary>Posities van de O-atomen (Å), binnen het blok.</summary>
        public readonly float[] X, Y, Z;
        /// <summary>Richting van de H-atomen: [i*6 + k*3 + (0,1,2)], eenheidsvectoren.</summary>
        public readonly float[] H;
        /// <summary>Per H [i*2 + k]: het O-atoom dat de H-brug ontvangt, of −1.</summary>
        public readonly int[] Acceptor;
        public readonly bool[] Vloeibaar;
        /// <summary>Bovenkant van het water in een los blok (y, Å). Daalt bij smelten.</summary>
        public float Bovenkant { get; private set; }
        /// <summary>Gemiddeld aantal H-bruggen per molecuul (binnenin gemeten, uitgevlakt).</summary>
        public float BruggenPerMolecuul { get; private set; } = 4f;
        /// <summary>Dichtheid in g/cm³ van het mengsel ijs + water.</summary>
        public double Dichtheid => Constanten.DichtheidIjsBinas + Fase.Gesmolten * (Constanten.DichtheidWater(Math.Max(0, Fase.Temperatuur)) - Constanten.DichtheidIjsBinas);

        // ---------- afstelling ----------
        public float Draairuis = 0.20f, Uitlijnen = 8f, BrugCos = 0.80f, BrugAfstand = 3.5f, Veer = 6f, HeetSchaal = 70f;
        const float D0 = 2.72f, Zoekstraal = 3.5f, Marge = 0.9f;

        // ---------- rooster en toestand ----------
        readonly float[] ox, oy, oz, latH;
        readonly int[] latAcc;
        readonly bool[] latRand;
        readonly int[] rangBlok, rangOneindig;      // volgorde van smelten
        readonly int[] opRangBlok, opRangOneindig;  // omgekeerd: welke plek heeft rang r
        readonly int[] latBuren;                    // de 4 roosterburen van elke plek (−1 = over de rand van een los blok)
        readonly int[] kopV, volgendeV;             // celrooster met alleen de vloeibare moleculen (voor het stollen)
        readonly int[] kopG, volgendeG;             // celrooster met de plekken die nog ijs moeten worden
        readonly int[] koppel;                      // eindfase van het stollen: welk molecuul hoort bij welke vrije plek (−1 = geen)
        bool eindfase;
        int aantalVloeibaar;
        readonly float[] fase, freq;
        readonly float[] lx, ly, lz, vx, vy, vz;      // vloeistof
        readonly float[] terug;                       // 0 → 1: overgang van vloeistof terug naar roosterplek
        readonly float[] reis;                        // afstand (Å) van die overgang: ver weg gaat het rustiger
        readonly byte[] tel;
        readonly uint[] rndI;                         // eigen toevalsgenerator per molecuul (veilig bij meerdere kernen)
        float tijd;
        uint rnd = 2463534242u;

        // celrooster voor buren
        readonly int gx, gy, gz;
        readonly float cx, cy, cz;
        readonly int[] kop, volgende, buren;
        readonly byte[] aantalBuren;
        const int MaxBuren = 10;

        public Watermodel(int nx, int ny, int nz, int zaad = 1, double startTemperatuur = -10)
        {
            Rooster = new IJsrooster(nx, ny, nz);
            IJsregels.Pas(Rooster, zaad);
            IJsregels.HAtomen(Rooster, out var hx, out var hy, out var hz, out var acc, out var rand);
            N = Rooster.Aantal;
            Lx = (float)Rooster.Lx; Ly = (float)Rooster.Ly; Lz = (float)Rooster.Lz;
            Fase = new Fasemodel(startTemperatuur) { MaxPlateauDuur = Constanten.MaxPlateauSeconden };

            X = new float[N]; Y = new float[N]; Z = new float[N];
            H = new float[6 * N]; Acceptor = new int[2 * N]; Vloeibaar = new bool[N];
            ox = new float[N]; oy = new float[N]; oz = new float[N];
            latH = new float[6 * N]; latAcc = acc; latRand = rand;
            fase = new float[3 * N]; freq = new float[3 * N];
            lx = new float[N]; ly = new float[N]; lz = new float[N];
            vx = new float[N]; vy = new float[N]; vz = new float[N];
            terug = new float[N]; reis = new float[N]; tel = new byte[N]; rndI = new uint[N];
            rnd ^= (uint)(zaad * 7919);

            for (int i = 0; i < N; i++)
            {
                ox[i] = X[i] = (float)Rooster.X[i]; oy[i] = Y[i] = (float)Rooster.Y[i]; oz[i] = Z[i] = (float)Rooster.Z[i];
                terug[i] = 1f;
                rndI[i] = (uint)(i * 2654435761u) ^ (uint)(zaad * 40503) | 1u;
                for (int a = 0; a < 3; a++) { fase[3 * i + a] = Rnd() * 6.2832f; freq[3 * i + a] = 7f + Rnd() * 5f; }
                for (int k = 0; k < 2; k++)
                {
                    latH[6 * i + 3 * k] = (float)hx[2 * i + k];
                    latH[6 * i + 3 * k + 1] = (float)hy[2 * i + k];
                    latH[6 * i + 3 * k + 2] = (float)hz[2 * i + k];
                }
            }
            Array.Copy(latH, H, 6 * N);

            // smeltvolgorde: blok van buiten naar binnen; oneindig vanuit 3 punten (met wat ruis voor een grillig front)
            var dB = new float[N]; var dI = new float[N];
            float hxB = Lx / 2, hyB = Ly / 2, hzB = Lz / 2;
            var punten = new[] { new[] { 0f, 0f, 0f }, new[] { hxB * 0.9f, -hyB * 0.5f, hzB * 0.6f }, new[] { -hxB * 0.5f, hyB * 0.7f, -hzB * 0.8f } };
            for (int i = 0; i < N; i++)
            {
                float r = Rnd() * 1.2f;
                dB[i] = Math.Min(hxB - Math.Abs(ox[i]), Math.Min(hyB - Math.Abs(oy[i]), hzB - Math.Abs(oz[i]))) + r;
                float m = float.MaxValue;
                foreach (var p in punten)
                {
                    float dx = Kortst(ox[i] - p[0], Lx), dy = Kortst(oy[i] - p[1], Ly), dz = Kortst(oz[i] - p[2], Lz);
                    m = Math.Min(m, (float)Math.Sqrt(dx * dx + dy * dy + dz * dz));
                }
                dI[i] = m + r;
            }
            rangBlok = Rangen(dB); rangOneindig = Rangen(dI);
            opRangBlok = new int[N]; opRangOneindig = new int[N];
            for (int i = 0; i < N; i++) { opRangBlok[rangBlok[i]] = i; opRangOneindig[rangOneindig[i]] = i; }
            // roosterburen: de 2 plekken waar i een H-brug naar geeft, en de 2 die er een naar i geven
            latBuren = new int[4 * N]; var tellerB = new int[N];
            for (int i = 0; i < 4 * N; i++) latBuren[i] = -1;
            for (int i = 0; i < N; i++)
                for (int k = 0; k < 2; k++)
                {
                    int j = latAcc[2 * i + k];
                    if (j < 0 || rand[2 * i + k]) continue;            // over de rand: alleen een buur in oneindig ijs (zie Buur)
                    if (tellerB[i] < 4) latBuren[4 * i + tellerB[i]++] = j;
                    if (tellerB[j] < 4) latBuren[4 * j + tellerB[j]++] = i;
                }
            randBuren = new int[4 * N]; var tellerR = new int[N];
            for (int i = 0; i < 4 * N; i++) randBuren[i] = -1;
            for (int i = 0; i < N; i++)
                for (int k = 0; k < 2; k++)
                {
                    int j = latAcc[2 * i + k];
                    if (j < 0 || !rand[2 * i + k]) continue;
                    if (tellerR[i] < 4) randBuren[4 * i + tellerR[i]++] = j;
                    if (tellerR[j] < 4) randBuren[4 * j + tellerR[j]++] = i;
                }

            gx = Math.Max(3, (int)(Lx / 3.6f)); gy = Math.Max(3, (int)(Ly / 3.6f)); gz = Math.Max(3, (int)(Lz / 3.6f));
            cx = Lx / gx; cy = Ly / gy; cz = Lz / gz;
            kop = new int[gx * gy * gz]; volgende = new int[N];
            kopV = new int[gx * gy * gz]; volgendeV = new int[N];
            kopG = new int[gx * gy * gz]; volgendeG = new int[N];
            koppel = new int[N]; doel = new int[N];
            for (int i = 0; i < N; i++) doel[i] = -1;
            buren = new int[N * MaxBuren]; aantalBuren = new byte[N];
            Bovenkant = Ly / 2;
            Stap(0, (float)startTemperatuur);
        }

        /// <summary>Eén tijdstap van <paramref name="dt"/> seconden met de omgeving op <paramref name="tOmgeving"/> °C.</summary>
        public void Stap(float dt, float tOmgeving)
        {
            dt = Math.Min(dt, 0.05f);
            Fase.Stap(dt, tOmgeving);
            float T = (float)Fase.Temperatuur;
            float bewegingsDt = Beweging ? dt : 0f;
            tijd += bewegingsDt;

            // --- wie is vloeibaar? ---
            // Smelten volgt het smeltfront (vaste volgorde). Stollen gebeurt waar het water is: telkens wordt de vrije
            // roosterplek ijs die naast bestaand ijs ligt en een vloeibaar molecuul vlak bij zich heeft. Zo groeit het ijs
            // geleidelijk vanaf een kiem, en vormen de kanalen zich tijdens het stollen (niet pas aan het eind).
            int nVloeibaar = (int)Math.Round(Fase.Gesmolten * N);
            if (aantalVloeibaar < nVloeibaar)
            {
                var opRang = Oneindig ? opRangOneindig : opRangBlok;
                for (int r = 0; r < N && aantalVloeibaar < nVloeibaar; r++)
                {
                    int i = opRang[r];
                    if (Vloeibaar[i]) continue;
                    Vloeibaar[i] = true; aantalVloeibaar++;
                    lx[i] = X[i]; ly[i] = Y[i]; lz[i] = Z[i];
                    vx[i] = vy[i] = vz[i] = 0;
                }
            }
            else if (aantalVloeibaar > nVloeibaar)
            {
                stolWacht += dt;
                eindfase = aantalVloeibaar <= Math.Max(24, N / 33);
                if (eindfase) Koppel();
                // niet te veel tegelijk (gelijkmatig rekenwerk): hooguit 3× zo snel als een plateau van 15 s nodig heeft
                int perBeeld = Math.Max(4, (int)Math.Ceiling(N * dt / 5f));
                while (aantalVloeibaar > nVloeibaar && perBeeld-- > 0)
                {
                    VloeistofRaster();
                    int i = KiesStolplek(out int m);
                    if (i < 0) break;
                    // pas bevriezen als het molecuul er vlakbij is; anders even wachten tot het (vloeiend) aankomt
                    // (loopt het ijs al MaxStolWacht achter, dan niet meer wachten: de achterblijvers schuiven vloeiend naar hun plek)
                    if (m >= 0 && Afstand2(m, i) > MaxStolAfstand * MaxStolAfstand && stolWacht < MaxStolWacht) break;
                    // Moleculen zijn niet te onderscheiden: de plek wordt ingenomen door het vloeibare molecuul dat er het
                    // dichtst bij is. Daarvoor ruilen i en dat molecuul hun vloeistoftoestand: niemand verspringt.
                    if (m >= 0 && m != i)
                    {
                        RuilVloeistof(i, m);
                        // de vloeistoftoestand van i woont nu in m: koppelingen naar i gaan naar m
                        if (eindfase) for (int p = 0; p < N; p++) if (koppel[p] == i) koppel[p] = m;
                    }
                    if (eindfase) koppel[i] = -1;
                    doel[i] = -1;
                    reis[i] = (float)Math.Sqrt(Afstand2(i, i));
                    Vloeibaar[i] = false; terug[i] = 0f; aantalVloeibaar--;
                }
            }
            float rhoW = (float)Constanten.DichtheidWater(Math.Max(0, T));
            Bovenkant = Oneindig ? Ly / 2 : -Ly / 2 + Ly * (1f - (float)Fase.Gesmolten * (1f - (float)Constanten.DichtheidIjsBinas / rhoW));

            if (nVloeibaar > 0) Vloeistof(bewegingsDt, T);
            if (aantalVloeibaar > 0 && tOmgeving < 0 && (Fase.IsAanHetSmelten || aantalVloeibaar > nVloeibaar)) VulGaten(dt);
            if (aantalVloeibaar <= nVloeibaar) stolWacht = 0f;
            IJs(dt, bewegingsDt, T);
            if (geschoven) { BreekTeLangeBruggen(); geschoven = false; }
            TelBruggen(dt);
        }

        // ---------- ijs: trillen rond de roosterplek; teruggaan na bevriezen ----------
        void IJs(float dt, float bdt, float T)
        {
            // Trilling: 0,15 Å bij 0 °C, maal TrillingVergroting. Natuurkundig schaalt de uitslag met √T; dat geeft bij −100 °C
            // nog 80 %, te weinig om te zien. Didactisch versterkt naar T^1,5: −100 °C → 50 %, −200 °C → 14 %.
            float amp = Beweging ? TrillingVergroting * (float)Constanten.TrillingIjs * (float)Math.Pow((Math.Min(T, 0f) + 273.15f) / 273.15f, 1.5) : 0f;
            for (int i = 0; i < N; i++)
            {
                if (Vloeibaar[i]) { X[i] = lx[i]; Y[i] = ly[i]; Z[i] = lz[i]; continue; }
                float sx = ox[i] + amp * (float)Math.Sin(tijd * freq[3 * i] + fase[3 * i]);
                float sy = oy[i] + amp * (float)Math.Sin(tijd * freq[3 * i + 1] + fase[3 * i + 1]);
                float sz = oz[i] + amp * (float)Math.Sin(tijd * freq[3 * i + 2] + fase[3 * i + 2]);
                int o = 6 * i;
                if (terug[i] < 1f)
                {
                    terug[i] = Math.Min(1f, terug[i] + dt * Math.Min(1.2f, MaxReisSnelheid / Math.Max(0.1f, reis[i])));
                    float w = terug[i] * terug[i] * (3f - 2f * terug[i]);
                    float ex = sx - lx[i], ey = sy - ly[i], ez = sz - lz[i];
                    if (Oneindig) { ex = Kortst(ex, Lx); ey = Kortst(ey, Ly); ez = Kortst(ez, Lz); }
                    X[i] = lx[i] + ex * w; Y[i] = ly[i] + ey * w; Z[i] = lz[i] + ez * w;
                    for (int q = 0; q < 6; q++) H[o + q] += (latH[o + q] - H[o + q]) * w;
                    Normeer(o); Normeer(o + 3);
                    for (int k = 0; k < 2; k++) Acceptor[2 * i + k] = terug[i] > 0.95f ? IJsAcceptor(i, k) : -1;
                }
                else
                {
                    X[i] = sx; Y[i] = sy; Z[i] = sz;
                    Array.Copy(latH, o, H, o, 6);
                    for (int k = 0; k < 2; k++) Acceptor[2 * i + k] = IJsAcceptor(i, k);
                }
            }
        }

        int IJsAcceptor(int i, int k)
        {
            int j = latAcc[2 * i + k];
            if (!Oneindig && latRand[2 * i + k]) return -1;   // over de rand bestaat in een los blok niet
            if (Vloeibaar[j]) return -1;
            if (terug[j] < 0.95f) return -1;                  // de buur is nog niet op zijn plek: nog geen brug
            return j;
        }

        readonly int[] randBuren;                   // roosterburen over de rand (alleen in oneindig ijs buren)

        int IJsBuren(int i)
        {
            int n = 0;
            for (int k = 0; k < 4; k++) { int j = latBuren[4 * i + k]; if (j >= 0 && !Vloeibaar[j]) n++; }
            if (Oneindig) for (int k = 0; k < 4; k++) { int j = randBuren[4 * i + k]; if (j >= 0 && !Vloeibaar[j]) n++; }
            return n;
        }

        // celrooster met de vloeibare moleculen op hun huidige plek
        void VloeistofRaster()
        {
            Array.Fill(kopV, -1);
            for (int m = 0; m < N; m++)
            {
                if (!Vloeibaar[m]) continue;
                int g = Vak(Cel(lx[m], Lx, cx, gx), Cel(ly[m], Ly, cy, gy), Cel(lz[m], Lz, cz, gz));
                volgendeV[m] = kopV[g]; kopV[g] = m;
            }
        }

        // het vloeibare molecuul dat het dichtst bij (px, py, pz) is, binnen ±straal cellen; −1 als er geen is
        int NaasteVloeistof(float px, float py, float pz, int straal, out float d2)
        {
            int beste = -1; d2 = float.MaxValue;
            int ci = Cel(px, Lx, cx, gx), cj = Cel(py, Ly, cy, gy), ck = Cel(pz, Lz, cz, gz);
            for (int a = -straal; a <= straal; a++)
            {
                int A = ci + a; if (A < 0 || A >= gx) { if (!Oneindig) continue; A = ((A % gx) + gx) % gx; }
                for (int b = -straal; b <= straal; b++)
                {
                    int B = cj + b; if (B < 0 || B >= gy) { if (!Oneindig) continue; B = ((B % gy) + gy) % gy; }
                    for (int c = -straal; c <= straal; c++)
                    {
                        int C = ck + c; if (C < 0 || C >= gz) { if (!Oneindig) continue; C = ((C % gz) + gz) % gz; }
                        for (int m = kopV[Vak(A, B, C)]; m >= 0; m = volgendeV[m])
                        {
                            float dx = lx[m] - px, dy = ly[m] - py, dz = lz[m] - pz;
                            if (Oneindig) { dx = Kortst(dx, Lx); dy = Kortst(dy, Ly); dz = Kortst(dz, Lz); }
                            float d = dx * dx + dy * dy + dz * dz;
                            if (d < d2) { d2 = d; beste = m; }
                        }
                    }
                }
            }
            return beste;
        }

        /// <summary>
        /// De vrije roosterplek die nu het best kan bevriezen: naast bestaand ijs (het ijsfront groeit), met een vloeibaar
        /// molecuul zo dicht mogelijk bij (dat neemt de plek in, <paramref name="molecuul"/>). Is er nog geen ijs, dan
        /// ontstaat er een kiem waar een molecuul het dichtst bij een roosterplek is.
        /// </summary>
        const float StolBuurVoorkeur = 4f;           // Å² voorrang per ijsbuur
        const float GatStraal = 1.8f, GatSnelheid = 2.5f;   // Å en Å/s
        const float GatVersnelling = 0.6f;                   // per s: een molecuul op 10 Å schuift 2,5 + 6 Å/s
        const float MaxStolAfstand = 2.5f, MaxStolWacht = 1f;   // Å; s (daarna bevriest de plek toch)
        float stolWacht;
        const float MaxReisSnelheid = 6f;            // Å/s: een lange weg naar de roosterplek wordt een rustige glijbeweging

        float Afstand2(int m, int s)
        {
            float dx = ox[s] - lx[m], dy = oy[s] - ly[m], dz = oz[s] - lz[m];
            if (Oneindig) { dx = Kortst(dx, Lx); dy = Kortst(dy, Ly); dz = Kortst(dz, Lz); }
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// Tijdens het stollen: is er bij een plek die nog ijs moet worden geen watermolecuul in de buurt (een leeg gat,
        /// omdat vloeibaar water dichter is en klontert), dan schuift het dichtstbijzijnde watermolecuul er rustig naartoe.
        /// Zo blijft het water verdeeld over de plekken die nog bevriezen, en hoeft aan het eind niemand ver te reizen.
        /// </summary>
        void VulGaten(float dt)
        {
            // de doelen 10 keer per seconde bepalen (dat zoeken is het dure deel); elk beeld een stapje ernaartoe
            doelKlok -= dt;
            if (doelKlok <= 0f) { doelKlok = 0.1f; BepaalDoelen(); }
            float stap = GatSnelheid * dt;
            for (int m = 0; m < N; m++)
            {
                int s = doel[m];
                if (s < 0 || !Vloeibaar[m] || !Vloeibaar[s]) continue;
                float d = (float)Math.Sqrt(Afstand2(m, s));
                if (d > (eindfase ? 0.3f : GatStraal)) Schuif(m, s, d, stap + GatVersnelling * d * dt);   // ver weg: sneller, maar vloeiend
            }
        }

        readonly int[] doel;                         // stollen: naar welke vrije plek een watermolecuul schuift (−1 = geen)
        float doelKlok;
        int doelRonde;                               // per ronde zoekt een derde van de moleculen een nieuw doel (zuinig)

        void BepaalDoelen()
        {
            // eindfase (de laatste paar procent): elke vrije plek krijgt een eigen molecuul
            eindfase = aantalVloeibaar <= Math.Max(24, N / 33);
            if (eindfase)
            {
                for (int m = 0; m < N; m++) doel[m] = -1;
                Koppel(); for (int i = 0; i < N; i++) if (Vloeibaar[i] && koppel[i] >= 0) doel[koppel[i]] = i;
                return;
            }
            doelRonde = (doelRonde + 1) % 3;
            VloeistofRaster();
            // 1. watermoleculen die ver van elke vrije plek zitten (bv. in een kanaal van het ijs), schuiven erheen
            Array.Fill(kopG, -1);
            for (int i = 0; i < N; i++)
            {
                if (!Vloeibaar[i]) continue;
                int g = Vak(Cel(ox[i], Lx, cx, gx), Cel(oy[i], Ly, cy, gy), Cel(oz[i], Lz, cz, gz));
                volgendeG[i] = kopG[g]; kopG[g] = i;
            }
            void Zoek(int m)
            {
                if (m % 3 != doelRonde) return;                       // deze ronde niet aan de beurt: houdt zijn doel
                doel[m] = -1;
                if (!Vloeibaar[m]) return;
                int s = NaastePlek(lx[m], ly[m], lz[m], 1, out float d2);
                if (s < 0) s = NaastePlek(lx[m], ly[m], lz[m], 3, out d2);
                // verder weg zoeken we niet (te duur): zo'n molecuul krijgt in de eindfase een eigen plek
                if (s >= 0 && d2 > GatStraal * GatStraal) doel[m] = s;
            }
            if (Parallel && N > 512)
            {
                int blokken = Math.Max(1, Environment.ProcessorCount) * 4, stuk = (N + blokken - 1) / blokken;
                System.Threading.Tasks.Parallel.For(0, blokken, b => { for (int m = b * stuk, e = Math.Min(N, m + stuk); m < e; m++) Zoek(m); });
            }
            else for (int m = 0; m < N; m++) Zoek(m);
            // 2. vrije plekken zonder water in de buurt (het water klontert): het dichtstbijzijnde molecuul schuift erheen
            for (int i = 0; i < N; i++)
            {
                if (!Vloeibaar[i]) continue;
                int m = NaasteVloeistof(ox[i], oy[i], oz[i], 1, out float d2);
                if (m < 0) m = NaasteVloeistof(ox[i], oy[i], oz[i], 2, out d2);
                if (m < 0 || d2 < GatStraal * GatStraal || doel[m] >= 0) continue;
                doel[m] = i;
            }
        }

        // vrije plekken en vloeibare moleculen eerlijk koppelen: kortste paren eerst (gulzig), elk molecuul één keer
        readonly System.Collections.Generic.List<(float d, int plek, int mol)> paren = new System.Collections.Generic.List<(float, int, int)>();
        void Koppel()
        {
            paren.Clear();
            for (int i = 0; i < N; i++)
            {
                koppel[i] = -1;
                if (!Vloeibaar[i]) continue;
                for (int m = 0; m < N; m++)
                {
                    if (!Vloeibaar[m]) continue;
                    paren.Add((Afstand2(m, i), i, m));
                }
            }
            paren.Sort((a, b) => a.d.CompareTo(b.d));
            var bezet = new System.Collections.Generic.HashSet<int>();
            foreach (var (d, plek, mol) in paren)
            {
                if (koppel[plek] >= 0 || bezet.Contains(mol)) continue;
                koppel[plek] = mol; bezet.Add(mol);
            }
        }

        bool geschoven;

        // na het verschuiven kan een vloeistofbrug te lang worden: die breekt
        void BreekTeLangeBruggen()
        {
            float max2 = BrugAfstand * BrugAfstand;
            for (int i = 0; i < N; i++)
                for (int k = 0; k < 2; k++)
                {
                    int j = Acceptor[2 * i + k];
                    if (j < 0 || (!Vloeibaar[i] && !Vloeibaar[j])) continue;    // ijsbruggen blijven
                    float dx = X[j] - X[i], dy = Y[j] - Y[i], dz = Z[j] - Z[i];
                    if (Oneindig) { dx = Kortst(dx, Lx); dy = Kortst(dy, Ly); dz = Kortst(dz, Lz); }
                    if (dx * dx + dy * dy + dz * dz > max2) Acceptor[2 * i + k] = -1;
                }
        }

        // molecuul m een stapje richting roosterplek s
        void Schuif(int m, int s, float d, float stap)
        {
            geschoven = true;
            float dx = ox[s] - lx[m], dy = oy[s] - ly[m], dz = oz[s] - lz[m];
            if (Oneindig) { dx = Kortst(dx, Lx); dy = Kortst(dy, Ly); dz = Kortst(dz, Lz); }
            float f = Math.Min(1f, stap / Math.Max(1e-4f, d));
            lx[m] += dx * f; ly[m] += dy * f; lz[m] += dz * f;
            if (Oneindig) { lx[m] = Terug(lx[m], Lx); ly[m] = Terug(ly[m], Ly); lz[m] = Terug(lz[m], Lz); }
        }

        // de vrije roosterplek die het dichtst bij (px, py, pz) is, binnen ±straal cellen; −1 als er geen is
        int NaastePlek(float px, float py, float pz, int straal, out float d2)
        {
            int beste = -1; d2 = float.MaxValue;
            int ci = Cel(px, Lx, cx, gx), cj = Cel(py, Ly, cy, gy), ck = Cel(pz, Lz, cz, gz);
            for (int a = -straal; a <= straal; a++)
            {
                int A = ci + a; if (A < 0 || A >= gx) { if (!Oneindig) continue; A = ((A % gx) + gx) % gx; }
                for (int b = -straal; b <= straal; b++)
                {
                    int B = cj + b; if (B < 0 || B >= gy) { if (!Oneindig) continue; B = ((B % gy) + gy) % gy; }
                    for (int c = -straal; c <= straal; c++)
                    {
                        int C = ck + c; if (C < 0 || C >= gz) { if (!Oneindig) continue; C = ((C % gz) + gz) % gz; }
                        for (int s = kopG[Vak(A, B, C)]; s >= 0; s = volgendeG[s])
                        {
                            float dx = ox[s] - px, dy = oy[s] - py, dz = oz[s] - pz;
                            if (Oneindig) { dx = Kortst(dx, Lx); dy = Kortst(dy, Ly); dz = Kortst(dz, Lz); }
                            float d = dx * dx + dy * dy + dz * dz;
                            if (d < d2) { d2 = d; beste = s; }
                        }
                    }
                }
            }
            return beste;
        }

        static float Terug(float v, float L) { float h = L * 0.5f; while (v > h) v -= L; while (v < -h) v += L; return v; }

        int KiesStolplek(out int molecuul)
        {
            molecuul = -1;
            if (eindfase)
            {
                int b = -1; float bd = float.MaxValue;
                for (int i = 0; i < N; i++)
                {
                    if (!Vloeibaar[i] || koppel[i] < 0) continue;
                    int m = koppel[i];
                    float dx = ox[i] - lx[m], dy = oy[i] - ly[m], dz = oz[i] - lz[m];
                    if (Oneindig) { dx = Kortst(dx, Lx); dy = Kortst(dy, Ly); dz = Kortst(dz, Lz); }
                    float d = dx * dx + dy * dy + dz * dz;
                    if (d < bd) { bd = d; b = i; }
                }
                if (b >= 0) { molecuul = koppel[b]; return b; }
            }
            bool erIsIJs = aantalVloeibaar < N;
            int beste = -1; float score = float.MaxValue;
            for (int i = 0; i < N; i++)
            {
                if (!Vloeibaar[i]) continue;
                int buren = IJsBuren(i);
                if (erIsIJs && buren == 0) continue;
                int m = NaasteVloeistof(ox[i], oy[i], oz[i], 1, out float d2);
                if (m < 0) d2 = 1e6f;                                  // geen water vlakbij: alleen als het niet anders kan
                // plekken met meer ijsburen eerst: dan blijven er geen gaten achter in het ijs (compact front)
                float sc = d2 - StolBuurVoorkeur * buren;
                if (sc < score) { score = sc; beste = i; molecuul = m; }
            }
            if (beste < 0)                                             // geen plek aan het front (komt bijna niet voor)
                for (int i = 0; i < N; i++) if (Vloeibaar[i]) { beste = i; break; }
            if (beste >= 0 && molecuul < 0)
            {
                // ver weg zoeken (het hele rooster)
                molecuul = NaasteVloeistof(ox[beste], oy[beste], oz[beste], Math.Max(gx, Math.Max(gy, gz)), out _);
            }
            return beste;
        }

        // ruil de vloeistoftoestand (plaats, snelheid, richting van de H-atomen) van twee moleculen
        void RuilVloeistof(int a, int b)
        {
            (lx[a], lx[b]) = (lx[b], lx[a]); (ly[a], ly[b]) = (ly[b], ly[a]); (lz[a], lz[b]) = (lz[b], lz[a]);
            (vx[a], vx[b]) = (vx[b], vx[a]); (vy[a], vy[b]) = (vy[b], vy[a]); (vz[a], vz[b]) = (vz[b], vz[a]);
            for (int q = 0; q < 6; q++) (H[6 * a + q], H[6 * b + q]) = (H[6 * b + q], H[6 * a + q]);
            (X[a], X[b]) = (X[b], X[a]); (Y[a], Y[b]) = (Y[b], Y[a]); (Z[a], Z[b]) = (Z[b], Z[a]);
            (tel[a], tel[b]) = (tel[b], tel[a]);
            (doel[a], doel[b]) = (doel[b], doel[a]);
        }

        // ---------- vloeistof ----------
        void Vloeistof(float dt, float T)
        {
            Array.Fill(kop, -1);
            for (int i = 0; i < N; i++)
            {
                int g = Vak(Cel(X[i], Lx, cx, gx), Cel(Y[i], Ly, cy, gy), Cel(Z[i], Lz, cz, gz));
                volgende[i] = kop[g]; kop[g] = i;
            }

            TK = Math.Max(0f, T) + 273.15f;
            sig = 2.2f * (float)Math.Sqrt(TK / 273.15f); demp = (float)Math.Exp(-3f * dt); sq = (float)Math.Sqrt(dt) * 3f;
            draai = Draairuis * (float)Math.Sqrt(TK / 273.15f) * (1f + Math.Max(0f, T) / HeetSchaal) * (float)Math.Sqrt(dt) * 2.2f;
            R2 = Zoekstraal * Zoekstraal;

            stapDt = dt; per = Oneindig;
            if (Parallel && N > 512)
            {
                int blokken = Math.Max(1, Environment.ProcessorCount) * 4, stuk = (N + blokken - 1) / blokken;
                System.Threading.Tasks.Parallel.For(0, blokken, b => { for (int i = b * stuk, e = Math.Min(N, i + stuk); i < e; i++) if (Vloeibaar[i]) Krachten(i); });
                System.Threading.Tasks.Parallel.For(0, blokken, b => { for (int i = b * stuk, e = Math.Min(N, i + stuk); i < e; i++) if (Vloeibaar[i]) BeweegEnDraai(i); });
            }
            else
            {
                for (int i = 0; i < N; i++) if (Vloeibaar[i]) Krachten(i);
                for (int i = 0; i < N; i++) if (Vloeibaar[i]) BeweegEnDraai(i);
            }
        }

        // stapgegevens, gedeeld door de rekenkernen (alleen gelezen)
        float TK, sig, demp, sq, draai, R2, stapDt;
        bool per;

        // stap 1: krachten van de buren, snelheid bijwerken (schrijft alleen in molecuul i)
        void Krachten(int i)
        {
            float dt = stapDt;
                float fx = 0, fy = 0, fz = 0;
                int n = 0;
                int ci = Cel(X[i], Lx, cx, gx), cj = Cel(Y[i], Ly, cy, gy), ck = Cel(Z[i], Lz, cz, gz);
                for (int a = -1; a <= 1; a++)
                {
                    int A = ci + a; if (A < 0 || A >= gx) { if (!per) continue; A = (A + gx) % gx; }
                    for (int b = -1; b <= 1; b++)
                    {
                        int B = cj + b; if (B < 0 || B >= gy) { if (!per) continue; B = (B + gy) % gy; }
                        for (int c = -1; c <= 1; c++)
                        {
                            int C = ck + c; if (C < 0 || C >= gz) { if (!per) continue; C = (C + gz) % gz; }
                            for (int j = kop[(A * gy + B) * gz + C]; j >= 0; j = volgende[j])
                            {
                                if (j == i) continue;
                                float dx = X[j] - X[i], dy = Y[j] - Y[i], dz = Z[j] - Z[i];
                                if (per) { dx = Kortst(dx, Lx); dy = Kortst(dy, Ly); dz = Kortst(dz, Lz); }
                                float d2 = dx * dx + dy * dy + dz * dz;
                                if (d2 > R2) continue;
                                float d = (float)Math.Sqrt(d2) + 1e-6f;
                                if (d < D0) { float f = (D0 - d) / d * 14f; fx -= dx * f; fy -= dy * f; fz -= dz * f; }
                                if (Acceptor[2 * i] == j || Acceptor[2 * i + 1] == j || Acceptor[2 * j] == i || Acceptor[2 * j + 1] == i)
                                { float f = (d - 2.8f) / d * Veer; fx += dx * f; fy += dy * f; fz += dz * f; }
                                if (n < MaxBuren) buren[i * MaxBuren + n++] = j;
                            }
                        }
                    }
                }
                aantalBuren[i] = (byte)n;
                vx[i] = (vx[i] + fx * dt + Gauss(i) * sig * sq) * demp;
                vy[i] = (vy[i] + fy * dt + Gauss(i) * sig * sq) * demp;
                vz[i] = (vz[i] + fz * dt + Gauss(i) * sig * sq) * demp;
        }

        // stap 2: verplaatsen en H-atomen laten draaien (schrijft alleen in molecuul i)
        void BeweegEnDraai(int i)
        {
            float dt = stapDt;
                lx[i] += vx[i] * dt; ly[i] += vy[i] * dt; lz[i] += vz[i] * dt;
                if (per) { lx[i] = Kortst(lx[i], Lx); ly[i] = Kortst(ly[i], Ly); lz[i] = Kortst(lz[i], Lz); }
                else
                {
                    Wand(ref lx[i], ref vx[i], -Lx / 2 + Marge, Lx / 2 - Marge);
                    Wand(ref ly[i], ref vy[i], -Ly / 2 + Marge, Bovenkant - Marge);
                    Wand(ref lz[i], ref vz[i], -Lz / 2 + Marge, Lz / 2 - Marge);
                }

                // H-atomen draaien naar de buur waar ze het meest naar wijzen, plus warmteruis
                int o = 6 * i, bezet = -1;
                for (int k = 0; k < 2; k++)
                {
                    float hx = H[o + 3 * k], hy = H[o + 3 * k + 1], hz = H[o + 3 * k + 2];
                    float beste = -2f, bx = 0, by = 0, bz = 0, bd = 9f; int bj = -1;
                    for (int q = 0; q < aantalBuren[i]; q++)
                    {
                        int j = buren[i * MaxBuren + q]; if (j == bezet) continue;
                        float dx = X[j] - X[i], dy = Y[j] - Y[i], dz = Z[j] - Z[i];
                        if (per) { dx = Kortst(dx, Lx); dy = Kortst(dy, Ly); dz = Kortst(dz, Lz); }
                        float d = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz) + 1e-6f;
                        float cs = (hx * dx + hy * dy + hz * dz) / d;
                        if (cs > beste) { beste = cs; bj = j; bx = dx / d; by = dy / d; bz = dz / d; bd = d; }
                    }
                    if (bj >= 0 && beste > 0.2f) { float r = Math.Min(1f, Uitlijnen * dt); hx += (bx - hx) * r; hy += (by - hy) * r; hz += (bz - hz) * r; }
                    hx += Gauss(i) * draai; hy += Gauss(i) * draai; hz += Gauss(i) * draai;
                    H[o + 3 * k] = hx; H[o + 3 * k + 1] = hy; H[o + 3 * k + 2] = hz;
                    Acceptor[2 * i + k] = (bj >= 0 && beste > BrugCos && bd < BrugAfstand) ? bj : -1;
                    if (Acceptor[2 * i + k] >= 0) bezet = bj;
                }
                Tetraeder(o);
        }

        // ---------- H-bruggen tellen (alleen binnenin, niet bij wanden of oppervlak) ----------
        void TelBruggen(float dt)
        {
            Array.Clear(tel, 0, N);
            for (int i = 0; i < N; i++)
                for (int k = 0; k < 2; k++)
                {
                    int j = Acceptor[2 * i + k];
                    if (j < 0) continue;
                    tel[i]++; tel[j]++;
                }
            int som = 0, n = 0;
            for (int i = 0; i < N; i++)
            {
                if (!Oneindig)
                {
                    float w = Math.Min(Lx / 2 - Math.Abs(X[i]), Math.Min(Ly / 2 - Math.Abs(Y[i]), Lz / 2 - Math.Abs(Z[i])));
                    if (Vloeibaar[i]) w = Math.Min(w, Bovenkant - Y[i]);
                    if (w < 3.6f) continue;
                }
                som += tel[i]; n++;
            }
            if (n == 0) return;
            float nu = (float)som / n;
            BruggenPerMolecuul = dt <= 0 ? nu : BruggenPerMolecuul + (nu - BruggenPerMolecuul) * Math.Min(1f, dt * 1.5f);
        }

        /// <summary>Huidig aantal H-bruggen per molecuul zonder uitvlakken (voor tests).</summary>
        public float BruggenNu() { TelBruggen(0); return BruggenPerMolecuul; }

        // ---------- hulpjes ----------
        static readonly float CT = (float)Math.Cos(54.74 * Math.PI / 180), ST = (float)Math.Sin(54.74 * Math.PI / 180);

        // zet de twee H-richtingen weer in een tetraëderhoek (109,5°) rond hun gemiddelde richting
        void Tetraeder(int o)
        {
            float bx = H[o] + H[o + 3], by = H[o + 1] + H[o + 4], bz = H[o + 2] + H[o + 5];
            float bl = (float)Math.Sqrt(bx * bx + by * by + bz * bz); if (bl < 1e-6f) bl = 1; bx /= bl; by /= bl; bz /= bl;
            float nx = H[o] - H[o + 3], ny = H[o + 1] - H[o + 4], nz = H[o + 2] - H[o + 5];
            float p = nx * bx + ny * by + nz * bz; nx -= p * bx; ny -= p * by; nz -= p * bz;
            float nl = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (nl < 1e-4f) { nx = by; ny = -bx; nz = 0; nl = (float)Math.Sqrt(nx * nx + ny * ny); if (nl < 1e-6f) { nx = 1; nl = 1; } }
            nx /= nl; ny /= nl; nz /= nl;
            H[o] = bx * CT + nx * ST; H[o + 1] = by * CT + ny * ST; H[o + 2] = bz * CT + nz * ST;
            H[o + 3] = bx * CT - nx * ST; H[o + 4] = by * CT - ny * ST; H[o + 5] = bz * CT - nz * ST;
        }

        void Normeer(int o)
        {
            float l = (float)Math.Sqrt(H[o] * H[o] + H[o + 1] * H[o + 1] + H[o + 2] * H[o + 2]);
            if (l < 1e-6f) return;
            H[o] /= l; H[o + 1] /= l; H[o + 2] /= l;
        }

        static void Wand(ref float p, ref float v, float min, float max)
        {
            if (p < min) { p = min; v = Math.Abs(v); }
            if (p > max) { p = max; v = -Math.Abs(v); }
        }

        int Vak(int a, int b, int c) => (a * gy + b) * gz + c;
        static int Cel(float v, float L, float c, int g) { int k = (int)((v + L / 2) / c); return k < 0 ? 0 : k >= g ? g - 1 : k; }
        public static float Kortst(float d, float L)
        {
            float h = L * 0.5f;
            if (d > h) { d -= L; if (d > h) d -= L * (float)Math.Round(d / L); }
            else if (d < -h) { d += L; if (d < -h) d -= L * (float)Math.Round(d / L); }
            return d;
        }

        static int[] Rangen(float[] waarde)
        {
            int n = waarde.Length;
            var idx = new int[n];
            for (int i = 0; i < n; i++) idx[i] = i;
            var kopie = (float[])waarde.Clone();
            Array.Sort(kopie, idx);
            var rang = new int[n];
            for (int r = 0; r < n; r++) rang[idx[r]] = r;
            return rang;
        }

        float Rnd()
        {
            rnd ^= rnd << 13; rnd ^= rnd >> 17; rnd ^= rnd << 5;
            return (rnd & 0xFFFFFF) / 16777216f;
        }

        // Ruis met gemiddelde 0 en spreiding 1 (som van 3 gelijkverdeelde getallen ≈ normaal). Veel sneller dan
        // log/cos, en voor warmtebeweging in beeld niet van echt "normaal" te onderscheiden.
        float Gauss(int i)
        {
            uint r = rndI[i];
            float som = 0f;
            for (int k = 0; k < 3; k++) { r ^= r << 13; r ^= r >> 17; r ^= r << 5; som += (r & 0xFFFFFF) / 16777216f; }
            rndI[i] = r;
            return (som - 1.5f) * 2f;
        }
    }
}
