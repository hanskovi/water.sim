using System;
using System.Collections.Generic;
using System.Globalization;

namespace WaterSim.Sim
{
    /// <summary>
    /// Rekenwerk van de quiz, los van de weergave: een getal-antwoord nakijken, de streak bijhouden en een
    /// ronde in willekeurige volgorde zetten.
    /// </summary>
    public static class Quiz
    {
        /// <summary>
        /// Leest een getal zoals een leerling het intypt: komma of punt, spaties, een echt minteken (−) of een gewoon (-).
        /// </summary>
        public static bool LeesGetal(string invoer, out double waarde)
        {
            waarde = 0;
            if (string.IsNullOrWhiteSpace(invoer)) return false;
            string s = invoer.Trim().Replace(" ", "").Replace(" ", "").Replace('−', '-').Replace('–', '-').Replace(',', '.');
            if (s.Length > 0 && s[0] == '+') s = s.Substring(1);
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out waarde) && !double.IsNaN(waarde) && !double.IsInfinity(waarde);
        }

        /// <summary>Klopt het ingetypte getal binnen de marge? (een marge van 0 telt als "precies", met een heel kleine speling)</summary>
        public static bool GetalKlopt(string invoer, double antwoord, double marge)
        {
            if (!LeesGetal(invoer, out double w)) return false;
            return Math.Abs(w - antwoord) <= Math.Max(marge, 1e-9 * Math.Max(1, Math.Abs(antwoord)));
        }

        /// <summary>Indices 0 … n−1 in willekeurige volgorde (Fisher–Yates).</summary>
        public static int[] Schud(int n, Random rnd)
        {
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = i;
            for (int i = n - 1; i > 0; i--) { int j = rnd.Next(i + 1); (a[i], a[j]) = (a[j], a[i]); }
            return a;
        }

        /// <summary>Punten voor een goed getekende opdracht (H-bruggen) per niveau: 1, 2 of 4.</summary>
        public static int PuntenTekenen(int niveau) => niveau <= 0 ? 1 : niveau == 1 ? 2 : 4;
    }

    /// <summary>Streak: punten achter elkaar goed; één fout zet hem op 0. Onthoudt de beste.</summary>
    public class Streak
    {
        public int Huidig { get; private set; }
        public int Beste { get; private set; }

        public Streak(int beste = 0) { Beste = Math.Max(0, beste); }

        /// <summary>Goed antwoord: zoveel punten erbij. Geeft true als dit een nieuwe beste streak is.</summary>
        public bool Goed(int punten = 1)
        {
            Huidig += Math.Max(1, punten);
            if (Huidig > Beste) { Beste = Huidig; return true; }
            return false;
        }

        public void Fout() => Huidig = 0;
    }
}
