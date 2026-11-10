using System;
using System.Collections.Generic;

namespace WaterSim.Sim
{
    /// <summary>
    /// Klimaat van een plaats, als jaarverloop van de daggemiddelde luchttemperatuur (een golf tussen de
    /// koudste en de warmste maand) en het zonlicht (uit de breedtegraad). Temperaturen: normalen 1991–2020
    /// (gemiddelde maandtemperatuur, afgerond). Model: geen weersschommelingen van dag tot dag.
    /// </summary>
    public class Klimaat
    {
        /// <summary>Sleutel voor de teksten ("klimaat.nl" …) en de regiocode van het land (ISO 3166, twee letters).</summary>
        public readonly string Sleutel, Land;
        /// <summary>Breedtegraad in graden (noord +, zuid −).</summary>
        public readonly double Breedte;
        /// <summary>Gemiddelde temperatuur van de koudste en de warmste maand (°C).</summary>
        public readonly double Koudst, Warmst;
        /// <summary>Dag van het jaar (0 = 1 januari) in het midden van de koudste maand.</summary>
        public readonly double DagKoudst;
        /// <summary>Strenge winter: zoveel graden kouder in het hart van de winter (0 = geen strenge winter).</summary>
        public readonly double Streng;
        /// <summary>Waar de strenge winter op gebaseerd is (voor de uitleg), of leeg.</summary>
        public readonly string StrengBron;

        public Klimaat(string sleutel, string land, double breedte, double koudst, double warmst, double dagKoudst,
                       double streng = 0, string strengBron = "")
        {
            Sleutel = sleutel; Land = land; Breedte = breedte; Koudst = koudst; Warmst = warmst;
            DagKoudst = dagKoudst; Streng = streng; StrengBron = strengBron;
        }

        public bool HeeftStrengeWinter => Streng > 0;

        /// <summary>Daggemiddelde luchttemperatuur (°C) op deze dag van het jaar.</summary>
        public double Lucht(double dag, bool streng = false)
        {
            double c = Math.Cos(2 * Math.PI * (dag - DagKoudst) / Seizoen.DagenPerJaar);
            double t = (Koudst + Warmst) / 2 - (Warmst - Koudst) / 2 * c;
            if (streng && Streng > 0) t -= Streng * Math.Pow(Math.Max(0, c), 1.5);   // alleen in de winterhelft, het sterkst midden in de winter
            return t;
        }

        /// <summary>
        /// Daggemiddeld zonlicht aan de grond (W/m²): de zonnestraling boven de dampkring (sterrenkundige formule voor
        /// deze breedte en dag) maal een vaste factor voor dampkring en bewolking.
        /// </summary>
        public double Zon(double dag) => Zonlicht(Breedte, dag);

        /// <summary>Deel van de straling boven de dampkring dat gemiddeld de grond bereikt (bewolking, dampkring); model.</summary>
        public const double Doorlating = 0.48;
        const double Zonneconstante = 1361;   // W/m²

        public static double Zonlicht(double breedte, double dag)
        {
            double phi = breedte * Math.PI / 180;
            double delta = 23.44 * Math.PI / 180 * Math.Sin(2 * Math.PI * (284 + dag + 1) / 365);   // declinatie van de zon
            double x = -Math.Tan(phi) * Math.Tan(delta);
            double h0 = x >= 1 ? 0 : x <= -1 ? Math.PI : Math.Acos(x);                            // uurhoek zonsondergang (poolnacht / middernachtzon)
            double afstand = 1 + 0.033 * Math.Cos(2 * Math.PI * (dag + 1) / 365);
            double q = Zonneconstante / Math.PI * afstand * (h0 * Math.Sin(phi) * Math.Sin(delta) + Math.Cos(phi) * Math.Cos(delta) * Math.Sin(h0));
            return Math.Max(0, q) * Doorlating;
        }

        /// <summary>Dag van het jaar waarop het jaar in de app begint: in de herfst, ruim drie maanden voor het hart van de winter.</summary>
        public double StartDag => ((DagKoudst - 107) % Seizoen.DagenPerJaar + Seizoen.DagenPerJaar) % Seizoen.DagenPerJaar;

        // ---------- de plaatsen ----------
        // Normalen 1991–2020 (gemiddelde maandtemperatuur), afgerond. Bronnen: KNMI, KMA, JMA, ECCC, NOAA, FMI,
        // JMA-normalen (Ushuaia), BoM (Cabramurra), NIWA (Lake Tekapo), NEA (Singapore).
        // Waarden met * zijn benaderingen uit secundaire bronnen en moeten nog worden nagelopen.
        public static readonly IReadOnlyList<Klimaat> Alle = new[]
        {
            new Klimaat("nl", "NL",  52.1,  3.6, 18.5,  15, 6.5, "1962/63"),    // De Bilt; strenge winter: 1962/63 (−3,1 °C tegen +2,6 °C normaal)
            new Klimaat("kr", "KR",  37.6, -1.9, 26.1,  15, 6.0, ""),            // Seoul; strenge winter: modelvoorbeeld
            new Klimaat("jp", "JP",  43.1, -3.2, 22.3,  15),                     // Sapporo (*januari)
            new Klimaat("ca", "CA",  45.4, -9.6, 21.3,  15),                     // Ottawa
            new Klimaat("us", "US",  45.0, -9.1, 23.4,  15),                     // Minneapolis (*juli uit gemiddelde max/min)
            new Klimaat("fi", "FI",  60.2, -3.8, 18.0,  45),                     // Helsinki: koudst in februari (*juli)
            new Klimaat("ru", "RU",  55.8, -6.5, 19.4,  15),                     // Moskou (*bronnen wijken ±0,6 °C af)
            new Klimaat("ar", "AR", -54.8,  2.1,  9.6, 196, 6.0, ""),            // Ushuaia: koudst in juli; strenge winter is een modelvoorbeeld
            new Klimaat("au", "AU", -35.9,  1.6, 14.0, 196, 6.0, ""),            // Cabramurra (1488 m): koudst in juli (*januari); strenge winter: modelvoorbeeld
            new Klimaat("nz", "NZ", -44.0,  1.9, 15.4, 196, 6.0, ""),            // Lake Tekapo (Zuidereiland); strenge winter: modelvoorbeeld
            new Klimaat("sg", "SG",   1.35, 26.8, 28.6,  15),                    // Singapore: tropisch, warmst in mei (vereenvoudigd als golf)
        };

        /// <summary>Het klimaat bij een regiocode van het land (bv. "KR"), of Nederland als het land niet in de lijst staat.</summary>
        public static Klimaat VoorLand(string regio)
        {
            foreach (var k in Alle) if (string.Equals(k.Land, regio, StringComparison.OrdinalIgnoreCase)) return k;
            return Alle[0];
        }
    }
}
