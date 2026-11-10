using System;

namespace WaterSim.Sim
{
    /// <summary>
    /// Een jaar in een gematigd klimaat (zoals Nederland, met een koude winter), als daggemiddelden.
    /// Dag 0 = 1 januari. Model, geen echte metingen: lucht van −6 °C (half januari) tot 20 °C (half juli),
    /// zonlicht van ≈ 20 W/m² (eind december) tot ≈ 230 W/m² (21 juni).
    /// </summary>
    public static class Seizoen
    {
        public const double DagenPerJaar = 365;
        /// <summary>Daggemiddeld zonlicht (W/m²) van een heldere winterdag en voorjaarsdag (ordegrootte, Nederland).</summary>
        public const double ZonWinter = 40, ZonVoorjaar = 150;

        /// <summary>Gemiddelde luchttemperatuur (°C) op deze dag van het jaar.</summary>
        public static double Lucht(double dag) => 7 - 13 * Math.Cos(2 * Math.PI * (dag - 15) / DagenPerJaar);

        /// <summary>Gemiddeld zonlicht (W/m²) op deze dag van het jaar.</summary>
        public static double Zon(double dag) => 125 + 105 * Math.Cos(2 * Math.PI * (dag - 172) / DagenPerJaar);

        /// <summary>Maand (0 = januari … 11 = december) bij deze dag van het jaar.</summary>
        public static int Maand(double dag)
        {
            int[] einde = { 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
            double d = ((dag % DagenPerJaar) + DagenPerJaar) % DagenPerJaar;
            for (int m = 0; m < 12; m++) if (d < einde[m]) return m;
            return 11;
        }
    }
}
