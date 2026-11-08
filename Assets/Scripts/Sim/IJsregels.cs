using System;
using System.Collections.Generic;

namespace WaterSim.Sim
{
    /// <summary>
    /// Verdeelt de H-atomen over het rooster volgens de ijsregels (Bernal–Fowler):
    /// op elke verbinding precies één H, en elk O-atoom heeft precies twee eigen H-atomen.
    ///
    /// Werkwijze: elk O-atoom heeft 4 verbindingen. Een Euler-rondgang over dit netwerk geeft
    /// elke verbinding een richting zó dat bij elk O-atoom 2 verbindingen vertrekken en 2 aankomen.
    /// "Vertrekken" = dit O-atoom is de donor (de H hoort bij hem). Door de volgorde te schudden
    /// staan de H-atomen wanordelijk, zoals in echt ijs Ih. Omdat het rooster periodiek is,
    /// gelden de regels ook over de randen: het blok kan naadloos herhaald worden.
    /// </summary>
    public static class IJsregels
    {
        /// <summary>Past de ijsregels toe op <paramref name="rooster"/>. Zelfde zaadgetal = zelfde verdeling.</summary>
        public static void Pas(IJsrooster rooster, int zaad = 1)
        {
            var rnd = new Random(zaad);
            int n = rooster.Aantal;
            var v = rooster.Verbindingen;

            foreach (var lijst in rooster.VerbindingenVan)
                if (lijst.Length % 2 != 0)
                    throw new InvalidOperationException("Elk O-atoom moet een even aantal verbindingen hebben.");

            // geschudde kopie van de verbindingslijsten
            var buren = new int[n][];
            for (int i = 0; i < n; i++)
            {
                var l = (int[])rooster.VerbindingenVan[i].Clone();
                for (int k = l.Length - 1; k > 0; k--) { int j = rnd.Next(k + 1); (l[k], l[j]) = (l[j], l[k]); }
                buren[i] = l;
            }

            var gebruikt = new bool[v.Length];
            var wijzer = new int[n];
            var stapel = new List<int>();
            for (int start = 0; start < n; start++)
            {
                stapel.Add(start);
                while (stapel.Count > 0)
                {
                    int o = stapel[stapel.Count - 1];
                    while (wijzer[o] < buren[o].Length && gebruikt[buren[o][wijzer[o]]]) wijzer[o]++;
                    if (wijzer[o] < buren[o].Length)
                    {
                        int e = buren[o][wijzer[o]];
                        gebruikt[e] = true;
                        v[e].Donor = o;
                        stapel.Add(v[e].A == o ? v[e].B : v[e].A);
                    }
                    else stapel.RemoveAt(stapel.Count - 1);
                }
            }
        }

        /// <summary>
        /// Richtingen van de twee H-atomen van elk O-atoom (eenheidsvectoren langs de O–O-verbinding)
        /// en de bijbehorende acceptor. Index: molecuul i, H k (0 of 1) → [i*2 + k].
        /// </summary>
        public static void HAtomen(IJsrooster rooster, out double[] hx, out double[] hy, out double[] hz,
                                   out int[] acceptor, out bool[] overRand)
        {
            int n = rooster.Aantal;
            hx = new double[2 * n]; hy = new double[2 * n]; hz = new double[2 * n];
            acceptor = new int[2 * n]; overRand = new bool[2 * n];
            var teller = new int[n];
            foreach (var e in rooster.Verbindingen)
            {
                if (e.Donor < 0) throw new InvalidOperationException("Pas eerst de ijsregels toe.");
                int d = e.Donor, k = teller[d]++;
                if (k > 1) throw new InvalidOperationException("Een O-atoom heeft meer dan 2 H-atomen.");
                double s = d == e.A ? 1 : -1, L = Math.Sqrt(e.Dx * e.Dx + e.Dy * e.Dy + e.Dz * e.Dz);
                hx[d * 2 + k] = s * e.Dx / L; hy[d * 2 + k] = s * e.Dy / L; hz[d * 2 + k] = s * e.Dz / L;
                acceptor[d * 2 + k] = e.Acceptor; overRand[d * 2 + k] = e.OverRand;
            }
        }
    }
}
