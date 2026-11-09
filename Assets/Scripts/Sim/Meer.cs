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
    /// - Een beetje menging (wind, stroming) houdt het verloop geleidelijk.
    /// Eenheden: diepte in m, temperatuur in °C, tijd in s, ijsdikte in m.
    /// Model: geen zon, geen sneeuw, geen grondwater; de bodem laat geen warmte door.
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

        /// <summary>Warmteoverdracht lucht ↔ oppervlak (W/(m²·K)); model, inclusief wind.</summary>
        public double Warmteoverdracht = 15;
        /// <summary>Menging door wind en stroming (m²/s); model.</summary>
        public double Menging = 1e-6;

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
            for (int k = 0; k < n; k++) Deelstap(h, tLucht);
            Tijd += dt;
        }

        void Deelstap(double dt, double tLucht)
        {
            // --- oppervlak ---
            if (Ijsdikte > 0)
            {
                // warmte door het ijs en de lucht (in serie); onder het ijs is het water 0 °C
                double weerstand = Ijsdikte / Constanten.WarmtegeleidingIjs + 1.0 / Warmteoverdracht;
                double qUit = (0 - tLucht) / weerstand;                     // W/m², > 0: warmte naar de lucht
                Ijsdikte += qUit * dt / (RhoIjs * Lf);                      // uit = bevriezen, in = smelten
                // het water onder het ijs geeft warmte af aan het ijs (smelt het van onderen)
                double qWater = C * Menging * (T[0] - 0) / (dz / 2);
                T[0] -= qWater * dt / (C * dz);
                Ijsdikte -= qWater * dt / (RhoIjs * Lf);
                if (Ijsdikte < 0)
                {
                    T[0] += -Ijsdikte * RhoIjs * Lf / (C * dz);             // te veel gesmolten: warmte terug naar het water
                    Ijsdikte = 0;
                }
            }
            else
            {
                double q = Warmteoverdracht * (tLucht - T[0]);
                T[0] += q * dt / (C * dz);
                if (T[0] < 0)
                {
                    Ijsdikte += -T[0] * C * dz / (RhoIjs * Lf);             // onder 0 °C: ijs vormt zich
                    T[0] = 0;
                }
            }

            // --- menging tussen de lagen (bodem laat niets door) ---
            double f = Menging * dt / (dz * dz);
            var oud = (double[])T.Clone();
            for (int i = 0; i < Lagen; i++)
            {
                double boven = i > 0 ? oud[i - 1] : oud[i], onder = i < Lagen - 1 ? oud[i + 1] : oud[i];
                T[i] = oud[i] + f * (boven - 2 * oud[i] + onder);
            }

            Stabiliseer();
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
