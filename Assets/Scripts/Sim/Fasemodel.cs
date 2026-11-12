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

        /// <summary>
        /// Langste duur van smelten of stollen (s modeltijd). Bij een kleine temperatuurafstand tot 0 °C zou het plateau
        /// heel lang duren (bij −4 °C ruim 50 s); de warmte-overdracht wordt dan zoveel groter dat het plateau hooguit deze
        /// duur heeft. Alles in dat traject gaat even veel sneller, dus de verhoudingen blijven gelijk. Oneindig = uit.
        /// </summary>
        public double MaxPlateauDuur = double.PositiveInfinity;

        /// <summary>De warmte-overdracht die bij deze omgeving geldt (zie <see cref="MaxPlateauDuur"/>).</summary>
        public double OverdrachtBij(double tOmgeving) =>
            Math.Max(Overdracht, Constanten.Smeltwarmte / (MaxPlateauDuur * Math.Max(1.0, Math.Abs(tOmgeving))));

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

        /// <summary>
        /// Geschatte tijd (s) tot het smelten of stollen klaar is, bij deze omgevingstemperatuur.
        /// Op het plateau is T = 0 °C, dus de warmtestroom is constant: k · T_omgeving.
        /// Buiten het plateau, of als er netto niets gebeurt (omgeving 0 °C): oneindig.
        /// </summary>
        public double ResterendeTijd(double tOmgeving)
        {
            if (!IsAanHetSmelten) return double.PositiveInfinity;
            tOmgeving = Math.Max(MinTemperatuur, Math.Min(MaxTemperatuur, tOmgeving));
            double stroom = OverdrachtBij(tOmgeving) * tOmgeving;      // J/(g·s); > 0 smelten, < 0 stollen
            if (stroom > 0) return (Constanten.Smeltwarmte - Enthalpie) / stroom;
            if (stroom < 0) return Enthalpie / -stroom;
            return double.PositiveInfinity;
        }

        /// <summary>Laat warmte stromen gedurende <paramref name="dt"/> seconden (in kleine stapjes, dus stabiel).</summary>
        public void Stap(double dt, double tOmgeving)
        {
            tOmgeving = Math.Max(MinTemperatuur, Math.Min(MaxTemperatuur, tOmgeving));
            int stappen = Math.Max(1, (int)Math.Ceiling(dt / 0.02));
            double h = dt / stappen, k = OverdrachtBij(tOmgeving);
            for (int s = 0; s < stappen; s++)
            {
                Enthalpie += k * (tOmgeving - Temperatuur) * h;
                Enthalpie = Math.Max(EnthalpieBij(MinTemperatuur), Math.Min(EnthalpieBij(MaxTemperatuur), Enthalpie));
            }
        }
    }
}
