using System;
using System.Collections.Generic;

namespace WaterSim.Sim
{
    /// <summary>
    /// Eén waterstofbrug-verbinding tussen twee O-atomen in het rooster.
    /// (Dx, Dy, Dz) is de kortste vector van A naar B, ook als B aan de overkant van de rand ligt.
    /// </summary>
    public struct Verbinding
    {
        public int A, B;
        public double Dx, Dy, Dz;
        /// <summary>True als de verbinding over de rand van het blok loopt (alleen zichtbaar in het oneindige kristal).</summary>
        public bool OverRand;
        /// <summary>Het O-atoom dat de H levert (A of B). −1 zolang de ijsregels nog niet zijn toegepast.</summary>
        public int Donor;

        public int Acceptor => Donor == A ? B : A;
    }

    /// <summary>
    /// Periodiek blok ijs Ih: alleen de O-atomen en hun verbindingen. Het blok sluit naadloos op
    /// zichzelf aan, zodat het oneindig herhaald kan worden.
    ///
    /// Opbouw: rechthoekige cel a × a√3 × c met 8 O-atomen, NX × NY × NZ keer herhaald.
    /// De c-as (kanalen) ligt langs z. Het blok is gecentreerd rond (0, 0, 0); bij oneven NX en NY
    /// ligt de oorsprong precies op de as van een zeshoekig kanaal.
    /// </summary>
    public class IJsrooster
    {
        public readonly int NX, NY, NZ;
        public readonly double A, B, C;          // celmaten: a, a√3, c (Å)
        public readonly double Lx, Ly, Lz;       // afmetingen van het blok (Å)
        public readonly int Aantal;              // aantal O-atomen
        public readonly double[] X, Y, Z;        // posities van de O-atomen (Å), rond de oorsprong
        public readonly Verbinding[] Verbindingen;
        /// <summary>Per O-atoom de 4 indexen in <see cref="Verbindingen"/>.</summary>
        public readonly int[][] VerbindingenVan;

        public IJsrooster(int nx, int ny, int nz)
        {
            if (nx < 2 || ny < 1 || nz < 1)
                throw new ArgumentException("Blok te klein: NX ≥ 2, NY ≥ 1 en NZ ≥ 1 nodig voor een eenduidig periodiek rooster.");
            NX = nx; NY = ny; NZ = nz;
            A = Constanten.IjsA; B = A * Math.Sqrt(3); C = Constanten.IjsC;
            Lx = NX * A; Ly = NY * B; Lz = NZ * C;

            // 8 O-atomen in de rechthoekige cel: 4 posities van de zeshoekige cel (Wyckoff 4f, z = 1/16)
            // plus dezelfde 4 verschoven over (a/2, b/2).
            double[,] hex = { { 1.0 / 3, 2.0 / 3, 1.0 / 16 }, { 2.0 / 3, 1.0 / 3, 9.0 / 16 },
                              { 2.0 / 3, 1.0 / 3, 15.0 / 16 }, { 1.0 / 3, 2.0 / 3, 7.0 / 16 } };
            var basis = new List<double[]>();
            foreach (var t in new[] { new[] { 0.0, 0.0 }, new[] { A / 2, B / 2 } })
                for (int k = 0; k < 4; k++)
                {
                    double x = hex[k, 0] * A - hex[k, 1] * A / 2 + t[0];
                    double y = hex[k, 1] * B / 2 + t[1];
                    basis.Add(new[] { Mod(x, A), Mod(y, B), hex[k, 2] * C });
                }

            Aantal = basis.Count * NX * NY * NZ;
            X = new double[Aantal]; Y = new double[Aantal]; Z = new double[Aantal];
            int n = 0;
            for (int i = 0; i < NX; i++)
                for (int j = 0; j < NY; j++)
                    for (int k = 0; k < NZ; k++)
                        foreach (var p in basis)
                        {
                            X[n] = p[0] + i * A - Lx / 2;
                            Y[n] = p[1] + j * B - Ly / 2;
                            Z[n] = p[2] + k * C - Lz / 2;
                            n++;
                        }

            Verbindingen = ZoekVerbindingen();
            VerbindingenVan = new int[Aantal][];
            var tel = new int[Aantal];
            foreach (var v in Verbindingen) { tel[v.A]++; tel[v.B]++; }
            for (int i = 0; i < Aantal; i++) VerbindingenVan[i] = new int[tel[i]];
            Array.Clear(tel, 0, Aantal);
            for (int e = 0; e < Verbindingen.Length; e++)
            {
                var v = Verbindingen[e];
                VerbindingenVan[v.A][tel[v.A]++] = e;
                VerbindingenVan[v.B][tel[v.B]++] = e;
            }
        }

        /// <summary>Dichtheid van dit rooster in g/cm³ (zou ≈ 0,92 moeten zijn).</summary>
        public double Dichtheid => Aantal * Constanten.MolaireMassaWater / (Constanten.Avogadro * Lx * Ly * Lz * 1e-24);

        /// <summary>Kortste verschil langs een as met periodieke randen (minimum-image).</summary>
        public static double KortsteVerschil(double d, double L) => d - L * Math.Round(d / L);

        /// <summary>Zet een positie terug in het blok (−L/2 … L/2).</summary>
        public void TerugInBlok(ref double x, ref double y, ref double z)
        {
            x = KortsteVerschil(x, Lx); y = KortsteVerschil(y, Ly); z = KortsteVerschil(z, Lz);
        }

        /// <summary>
        /// De as van het zeshoekige kanaal (langs z) die het dichtst bij (x, y) ligt.
        /// Kanalen liggen op (i·a, j·b) en op (a/2 + i·a, b/2 + j·b), gemeten vanaf het midden van het blok
        /// (bij oneven NX en NY).
        /// </summary>
        public void DichtstbijzijndKanaal(double x, double y, out double kx, out double ky)
        {
            double ox = (NX % 2 == 1) ? 0 : A / 2, oy = (NY % 2 == 1) ? 0 : B / 2;
            double x1 = ox + Math.Round((x - ox) / A) * A, y1 = oy + Math.Round((y - oy) / B) * B;
            double x2 = ox + A / 2 + Math.Round((x - ox - A / 2) / A) * A, y2 = oy + B / 2 + Math.Round((y - oy - B / 2) / B) * B;
            bool eerste = (x - x1) * (x - x1) + (y - y1) * (y - y1) <= (x - x2) * (x - x2) + (y - y2) * (y - y2);
            kx = eerste ? x1 : x2; ky = eerste ? y1 : y2;
        }

        // Zoekt alle O–O-paren op ≈ 2,76 Å met een celrooster (snel, ook voor grote blokken).
        Verbinding[] ZoekVerbindingen()
        {
            const double grens = 0.15;
            double maxD = Constanten.AfstandOO + grens;
            int gx = Math.Max(1, (int)(Lx / maxD)), gy = Math.Max(1, (int)(Ly / maxD)), gz = Math.Max(1, (int)(Lz / maxD));
            var vakken = new List<int>[gx * gy * gz];
            for (int i = 0; i < vakken.Length; i++) vakken[i] = new List<int>();
            int Vak(int a, int b, int c) => (((a % gx) + gx) % gx * gy + ((b % gy) + gy) % gy) * gz + ((c % gz) + gz) % gz;
            int Cel(double v, double L, int g) => Math.Min(g - 1, (int)((v + L / 2) / L * g));
            for (int i = 0; i < Aantal; i++) vakken[Vak(Cel(X[i], Lx, gx), Cel(Y[i], Ly, gy), Cel(Z[i], Lz, gz))].Add(i);

            var lijst = new List<Verbinding>(Aantal * 2);
            var gezien = new HashSet<long>();
            for (int i = 0; i < Aantal; i++)
            {
                int ci = Cel(X[i], Lx, gx), cj = Cel(Y[i], Ly, gy), ck = Cel(Z[i], Lz, gz);
                var bekeken = new HashSet<int>();
                for (int a = -1; a <= 1; a++)
                    for (int b = -1; b <= 1; b++)
                        for (int c = -1; c <= 1; c++)
                        {
                            int vak = Vak(ci + a, cj + b, ck + c);
                            if (!bekeken.Add(vak)) continue;     // bij kleine blokken kan een vak dubbel voorkomen
                            foreach (int j in vakken[vak])
                            {
                                if (j <= i) continue;
                                double dx = KortsteVerschil(X[j] - X[i], Lx), dy = KortsteVerschil(Y[j] - Y[i], Ly), dz = KortsteVerschil(Z[j] - Z[i], Lz);
                                double d = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                                if (Math.Abs(d - Constanten.AfstandOO) > grens) continue;
                                if (!gezien.Add((long)i * Aantal + j)) continue;
                                double rx = X[j] - X[i], ry = Y[j] - Y[i], rz = Z[j] - Z[i];
                                bool overRand = Math.Abs(Math.Sqrt(rx * rx + ry * ry + rz * rz) - d) > 1e-6;
                                lijst.Add(new Verbinding { A = i, B = j, Dx = dx, Dy = dy, Dz = dz, OverRand = overRand, Donor = -1 });
                            }
                        }
            }
            return lijst.ToArray();
        }

        static double Mod(double v, double m) => ((v % m) + m) % m;
    }
}
