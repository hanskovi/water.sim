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
        static readonly System.Text.RegularExpressions.Regex Vorm = new System.Text.RegularExpressions.Regex(
            @"^([+-]?\d+(?:\.(\d+))?)(?:(?:[x*·]10\^?([+-]?\d+))|(?:e([+-]?\d+)))?$");

        /// <summary>
        /// Leest een getal zoals een leerling het intypt: komma of punt, spaties, een echt minteken (−), en eventueel een
        /// macht van 10 (6,022e23 · 6,022·10^23 · 6,022 × 10²³ · 6,022x10^23).
        /// </summary>
        /// <param name="macht">de ingetypte macht van 10, of null als die er niet bij staat</param>
        /// <param name="decimalen">aantal cijfers achter de komma (van het getal vóór de macht)</param>
        public static bool LeesGetal(string invoer, out double waarde, out int? macht, out int decimalen)
        {
            waarde = 0; macht = null; decimalen = 0;
            if (string.IsNullOrWhiteSpace(invoer)) return false;
            var sb = new System.Text.StringBuilder();
            foreach (char c in invoer.Trim())
            {
                int sup = "⁰¹²³⁴⁵⁶⁷⁸⁹".IndexOf(c);
                if (sup >= 0) { if (sb.Length == 0 || sb[sb.Length - 1] != '^' && !char.IsDigit(sb[sb.Length - 1]) && sb[sb.Length - 1] != '-') sb.Append('^'); sb.Append((char)('0' + sup)); continue; }
                if (c == '⁻') { sb.Append("^-"); continue; }
                if (char.IsWhiteSpace(c)) continue;
                switch (c)
                {
                    case '−': case '–': sb.Append('-'); break;
                    case ',': sb.Append('.'); break;
                    case '×': case 'X': case '*': case '·': case '⋅': sb.Append('x'); break;
                    case 'E': sb.Append('e'); break;
                    default: sb.Append(c); break;
                }
            }
            string s = sb.ToString().Replace("^^", "^").Replace("10^-", "10^-");
            if (s.StartsWith("+")) s = s.Substring(1);
            // "10²³" zonder ^ komt als "10^23" binnen; "6.022x10^23" en "6.022e23" passen in het patroon
            var m = Vorm.Match(s);
            if (!m.Success) return false;
            if (!double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double basis)) return false;
            decimalen = m.Groups[2].Success ? m.Groups[2].Value.Length : 0;
            string e = m.Groups[3].Success ? m.Groups[3].Value : m.Groups[4].Success ? m.Groups[4].Value : null;
            if (e != null)
            {
                if (!int.TryParse(e, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mm)) return false;
                macht = mm;
                waarde = basis * Math.Pow(10, mm);
            }
            else waarde = basis;
            return !double.IsNaN(waarde) && !double.IsInfinity(waarde);
        }

        public static bool LeesGetal(string invoer, out double waarde) => LeesGetal(invoer, out waarde, out _, out _);

        /// <summary>
        /// Klopt het ingetypte getal binnen de marge? Het antwoord staat in eenheden van 10^<paramref name="eenheidsmacht"/>
        /// (bv. 6,022 met macht 23): een leerling mag het getal in die eenheid typen ("6,022") of met de macht erbij
        /// ("6,022e23"). Met <paramref name="minDecimalen"/> moet het minstens zoveel cijfers achter de komma hebben.
        /// </summary>
        public static bool GetalKlopt(string invoer, double antwoord, double marge, int eenheidsmacht = 0, int minDecimalen = 0)
        {
            if (!LeesGetal(invoer, out double w, out int? macht, out int dec)) return false;
            if (dec < minDecimalen) return false;
            if (macht.HasValue) w /= Math.Pow(10, eenheidsmacht);
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

        public Streak(int beste = 0, int huidig = 0) { Huidig = Math.Max(0, huidig); Beste = Math.Max(Math.Max(0, beste), Huidig); }

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
