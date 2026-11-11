using System;

namespace WaterSim.Sim
{
    /// <summary>
    /// Eenvoudig model van een meer in de winter: een kolom water in lagen (bovenste laag = index 0).
    ///
    /// - Het oppervlak wisselt warmte uit met de lucht (warmteoverdracht, W/(m²·K)).
    /// - Ligt er zwaarder water boven lichter water, dan zakt het: de lagen mengen (convectie).
    ///   Omdat water bij ≈ 4 °C het zwaarst is, koelt het hele meer eerst af tot 4 °C;
    ///   daarna blijft kouder water bovenop liggen en bevriest het oppervlak.
    /// - IJs groeit aan de onderkant: de warmte gaat door het ijs naar de lucht (warmtegeleiding van ijs).
    /// - Bij dooi smelt het ijs aan de bovenkant (warme lucht, zon); er komt smeltwater op te staan.
    ///   Van onderen smelt het alleen door water dat warmer is dan 0 °C (bv. opgewarmd door licht door het ijs).
    /// - Zonlicht dringt in het water en wordt naar de diepte toe minder (uitdoving); wind mengt de bovenste laag.
    /// Eenheden: diepte in m, temperatuur in °C, tijd in s, ijsdikte in m, zon in W/m² (daggemiddelde).
    /// Model: geen sneeuw, geen grondwater, geen in- of uitstroom; de bodem laat geen warmte door.
    /// </summary>
    public class Meer
    {
        public readonly int Lagen;
        public readonly double Diepte;
        /// <summary>Temperatuur per laag (°C), van boven naar beneden.</summary>
        public readonly double[] T;
        /// <summary>Dikte van de ijslaag (m).</summary>
        public double Ijsdikte;
        /// <summary>Verstreken modeltijd (s).</summary>
        public double Tijd;

        /// <summary>Warmteoverdracht lucht ↔ oppervlak (W/(m²·K)); model, inclusief wind, uitstraling en verdamping.</summary>
        public double Warmteoverdracht = Constanten.WarmteoverdrachtMeer;
        /// <summary>Menging door wind en stroming (m²/s); model.</summary>
        public double Menging = 4e-7;
        /// <summary>Zonlicht op het oppervlak (W/m², daggemiddelde). 0 = geen zon.</summary>
        public double Zon;
        /// <summary>Weerkaatsing van zonlicht door ijs en door water (0 … 1); model.</summary>
        public double AlbedoIjs = 0.5, AlbedoWater = 0.07;
        /// <summary>Diepte (m) waarop het licht tot 1/e is uitgedoofd, in ijs en in water; model.</summary>
        public double LichtdiepteIjs = 0.7, LichtdiepteWater = 0.8;
        /// <summary>Dikte van de laag die de wind mengt als er geen ijs ligt (m); model.</summary>
        public double Windlaag = 1.5;
        /// <summary>Warmteoverdracht van water naar de onderkant van het ijs (W/(m²·K)); model.</summary>
        public double OverdrachtOnderIjs = 10;

        /// <summary>Smeltwater dat op het ijs staat (m water); alleen voor de weergave.</summary>
        public double Smeltwater;
        /// <summary>Opgeteld sinds de start (m ijs): aangegroeid aan de onderkant, gesmolten aan boven- en onderkant.</summary>
        public double GegroeidOnder, GesmoltenBoven, GesmoltenOnder;
        /// <summary>Wat het ijs de laatste stap aan de bovenkant deed: +1 groeit (onder), −1 smelt (boven), 0 niets.</summary>
        public int IjsRichting;

        /// <summary>Gemiddelde snelheden over ongeveer de laatste dag (m ijs per dag): aangroei onder, smelten boven en onder.</summary>
        public double GroeiOnderPerDag, SmeltBovenPerDag, SmeltOnderPerDag;
        /// <summary>Netto verandering van de ijsdikte (m per dag, + = dikker).</summary>
        public double NettoPerDag => GroeiOnderPerDag - SmeltBovenPerDag - SmeltOnderPerDag;

        public enum IJstoestand { GeenIJs, Groeit, SmeltBoven, SmeltOnder, SmeltBeide, Gelijk }

        /// <summary>Wat er netto met het ijs gebeurt (gemiddeld over ongeveer een dag).</summary>
        public IJstoestand Toestand
        {
            get
            {
                if (Ijsdikte <= 0) return IJstoestand.GeenIJs;
                const double drempel = 0.0005;                       // 0,5 mm per dag
                double netto = NettoPerDag;
                double smelt = SmeltBovenPerDag + SmeltOnderPerDag;             // een kant telt mee vanaf een kwart van het smelten
                bool boven = SmeltBovenPerDag > Math.Max(drempel, 0.25 * smelt), onder = SmeltOnderPerDag > Math.Max(drempel, 0.25 * smelt);
                if (netto > drempel) return IJstoestand.Groeit;
                if (netto < -drempel)
                {
                    if (boven && onder) return IJstoestand.SmeltBeide;
                    return SmeltOnderPerDag > SmeltBovenPerDag ? IJstoestand.SmeltOnder : IJstoestand.SmeltBoven;
                }
                return IJstoestand.Gelijk;
            }
        }

        readonly double dz;
        static double C => Constanten.SoortelijkeWarmteWater * 1000 * 1000;   // J/(m³·K): 4,18 J/(g·K) · 10⁶ g/m³
        static double RhoIjs => Constanten.DichtheidIjsBinas * 1000;            // kg/m³
        static double Lf => Constanten.Smeltwarmte * 1000;                      // J/kg

        public Meer(int lagen = 24, double diepte = 6, double startTemperatuur = 12)
        {
            Lagen = lagen; Diepte = diepte; dz = diepte / lagen;
            T = new double[lagen];
            for (int i = 0; i < lagen; i++) T[i] = startTemperatuur;
        }

        /// <summary>Temperatuur op het diepste punt.</summary>
        public double Bodem => T[Lagen - 1];

        public void Stap(double dt, double tLucht)
        {
            int n = Math.Max(1, (int)Math.Ceiling(dt / 600.0));
            double h = dt / n;
            double g0 = GegroeidOnder, b0 = GesmoltenBoven, o0 = GesmoltenOnder;
            for (int k = 0; k < n; k++) Deelstap(h, tLucht);
            Tijd += dt;
            // snelheden glad maken over ongeveer een dag (zodat het label niet heen en weer springt)
            if (dt > 0)
            {
                double a = 1 - Math.Exp(-dt / 86400.0), perDag = 86400.0 / dt;
                GroeiOnderPerDag += a * ((GegroeidOnder - g0) * perDag - GroeiOnderPerDag);
                SmeltBovenPerDag += a * ((GesmoltenBoven - b0) * perDag - SmeltBovenPerDag);
                SmeltOnderPerDag += a * ((GesmoltenOnder - o0) * perDag - SmeltOnderPerDag);
                if (Ijsdikte <= 0) GroeiOnderPerDag = SmeltBovenPerDag = SmeltOnderPerDag = 0;
            }
        }

        void Deelstap(double dt, double tLucht)
        {
            if (Ijsdikte > 0)
            {
                // zonlicht: deel weerkaatst, deel blijft in het ijs (aan de bovenkant), deel gaat door het ijs het water in
                double inIjs = Zon * (1 - AlbedoIjs);
                double doorIjs = inIjs * Math.Exp(-Ijsdikte / LichtdiepteIjs);
                double opIjs = inIjs - doorIjs;
                double qBoven = Warmteoverdracht * (tLucht - 0) + opIjs;      // W/m² naar het ijsoppervlak
                if (qBoven > 0)
                {
                    // dooi: de bovenkant is 0 °C en smelt; er komt smeltwater op het ijs
                    double smelt = Math.Min(Ijsdikte, qBoven * dt / (RhoIjs * Lf));
                    Ijsdikte -= smelt; GesmoltenBoven += smelt;
                    Smeltwater += smelt * RhoIjs / 1000.0;
                    IjsRichting = -1;
                }
                else
                {
                    // vorst: warmte stroomt van het water door het ijs naar de lucht; het ijs groeit aan de onderkant
                    double tZonLucht = tLucht + opIjs / Warmteoverdracht;
                    double weerstand = Ijsdikte / Constanten.WarmtegeleidingIjs + 1.0 / Warmteoverdracht;
                    double groei = (0 - tZonLucht) / weerstand * dt / (RhoIjs * Lf);
                    Ijsdikte += groei; GegroeidOnder += groei;
                    Smeltwater = 0;                                            // bevriest weer (vereenvoudigd)
                    IjsRichting = 1;
                }
                Absorbeer(doorIjs, dt);
                // water dat warmer is dan 0 °C smelt het ijs van onderen
                double qWater = OverdrachtOnderIjs * Math.Max(0, T[0]);
                double onder = Math.Min(Ijsdikte, qWater * dt / (RhoIjs * Lf));
                Ijsdikte -= onder; GesmoltenOnder += onder;
                T[0] -= onder * RhoIjs * Lf / (C * dz);
                if (Ijsdikte <= 1e-6) { Ijsdikte = 0; Smeltwater = 0; }
            }
            else
            {
                Smeltwater = 0; IjsRichting = 0;
                double q = Warmteoverdracht * (tLucht - T[0]);
                T[0] += q * dt / (C * dz);
                Absorbeer(Zon * (1 - AlbedoWater), dt);
                if (T[0] < 0)
                {
                    Ijsdikte += -T[0] * C * dz / (RhoIjs * Lf);             // onder 0 °C: ijs vormt zich
                    T[0] = 0;
                    IjsRichting = 1;
                }
                else Windmenging();
            }

            // --- menging tussen de lagen (bodem laat niets door) ---
            double f = Menging * dt / (dz * dz);
            var oud = (double[])T.Clone();
            for (int i = 0; i < Lagen; i++)
            {
                double boven = i > 0 ? oud[i - 1] : oud[i], onderT = i < Lagen - 1 ? oud[i + 1] : oud[i];
                T[i] = oud[i] + f * (boven - 2 * oud[i] + onderT);
            }

            Stabiliseer();
        }

        // licht dat het water in gaat (W/m²) wordt per laag geabsorbeerd: steeds minder naar de diepte;
        // het (kleine) restje dat de bodem bereikt, verdwijnt in de bodem
        void Absorbeer(double q, double dt)
        {
            if (q <= 0) return;
            double vorige = 1;
            for (int i = 0; i < Lagen; i++)
            {
                double z = (i + 1) * dz;
                double nog = Math.Exp(-z / LichtdiepteWater);
                T[i] += (vorige - nog) * q * dt / (C * dz);
                vorige = nog;
            }
        }

        // zonder ijs mengt de wind de bovenste laag tot één temperatuur
        void Windmenging()
        {
            int n = Math.Max(1, Math.Min(Lagen, (int)Math.Round(Windlaag / dz)));
            double som = 0;
            for (int i = 0; i < n; i++) som += T[i];
            for (int i = 0; i < n; i++) T[i] = som / n;
        }

        /// <summary>
        /// Convectie: ligt zwaarder water boven lichter water, dan mengt die hele zone tot één temperatuur.
        /// De zone groeit naar boven en beneden zolang de buren nog instabiel zijn (convectieve aanpassing).
        /// </summary>
        public void Stabiliseer()
        {
            static double Rho(double t) => Constanten.DichtheidWater(t);
            for (int ronde = 0; ronde < Lagen; ronde++)
            {
                bool rustig = true;
                for (int i = 0; i < Lagen - 1; i++)
                {
                    if (Rho(T[i]) <= Rho(T[i + 1]) + 1e-12) continue;
                    rustig = false;
                    int a = i, b = i + 1;
                    double som = T[a] + T[b];
                    while (true)
                    {
                        double m = som / (b - a + 1);
                        if (a > 0 && Rho(T[a - 1]) > Rho(m) + 1e-12) { a--; som += T[a]; continue; }
                        if (b < Lagen - 1 && Rho(m) > Rho(T[b + 1]) + 1e-12) { b++; som += T[b]; continue; }
                        break;
                    }
                    double gem = som / (b - a + 1);
                    for (int k = a; k <= b; k++) T[k] = gem;
                }
                if (rustig) return;
            }
        }
    }
}
