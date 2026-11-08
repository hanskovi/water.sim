using System;

namespace WaterSim.Sim
{
    /// <summary>
    /// Energiemodel voor 1 gram water. Houdt de "warme-inhoud" (enthalpie, J/g) bij in plaats van de temperatuur.
    /// Nulpunt: ijs van 0 °C.
    ///
    ///   h &lt; 0                  → ijs,          T = h / c_ijs
    ///   0 ≤ h ≤ 334            → ijs + water,  T = 0 °C (smelten; de warmte breekt H-bruggen)
    ///   h &gt; 334                → water,        T = (h − 334) / c_water
    ///
    /// Warmte stroomt van de omgeving naar het water: dh/dt = k · (T_omgeving − T).
    /// Daardoor ontstaat de opwarmcurve met het plateau bij 0 °C vanzelf, met de juiste verhoudingen.
    /// Koken (100 °C) komt in fase 5; tot die tijd blijft het water onder <see cref="MaxTemperatuur"/>.
    /// </summary>
    public class Fasemodel
    {
        /// <summary>Hoogste temperatuur tot koken is ingebouwd (°C).</summary>
        public const double MaxTemperatuur = 99;
        public const double MinTemperatuur = -273;

        /// <summary>Enthalpie in J/g, ten opzichte van ijs bij 0 °C.</summary>
        public double Enthalpie;

        /// <summary>
        /// Warmte-overdracht in J/(g·K) per seconde modeltijd. Hoger = alles gaat sneller.
        /// Standaard: smelten bij een omgeving van 20 °C duurt ongeveer 10 seconden.
        /// </summary>
        public double Overdracht = 1.67;

        public Fasemodel(double startTemperatuur) { Enthalpie = EnthalpieBij(startTemperatuur); }

        public double Temperatuur => TemperatuurBij(Enthalpie);

        /// <summary>Deel van het water dat vloeibaar is (0 … 1).</summary>
        public double Gesmolten => Math.Max(0, Math.Min(1, Enthalpie / Constanten.Smeltwarmte));

        public bool IsAanHetSmelten => Enthalpie > 0 && Enthalpie < Constanten.Smeltwarmte;

        public static double EnthalpieBij(double t) =>
            t <= 0 ? Constanten.SoortelijkeWarmteIjs * t
                   : Constanten.Smeltwarmte + Constanten.SoortelijkeWarmteWater * t;

        public static double TemperatuurBij(double h)
        {
            if (h < 0) return h / Constanten.SoortelijkeWarmteIjs;
            if (h <= Constanten.Smeltwarmte) return 0;
            return (h - Constanten.Smeltwarmte) / Constanten.SoortelijkeWarmteWater;
        }

        /// <summary>Laat warmte stromen gedurende <paramref name="dt"/> seconden (in kleine stapjes, dus stabiel).</summary>
        public void Stap(double dt, double tOmgeving)
        {
            tOmgeving = Math.Max(MinTemperatuur, Math.Min(MaxTemperatuur, tOmgeving));
            int stappen = Math.Max(1, (int)Math.Ceiling(dt / 0.02));
            double h = dt / stappen;
            for (int s = 0; s < stappen; s++)
            {
                Enthalpie += Overdracht * (tOmgeving - Temperatuur) * h;
                Enthalpie = Math.Max(EnthalpieBij(MinTemperatuur), Math.Min(EnthalpieBij(MaxTemperatuur), Enthalpie));
            }
        }
    }
}
