using System;
using System.Linq;
using NUnit.Framework;
using WaterSim.Sim;

namespace WaterSim.Tests
{
    public class IJsroosterTests
    {
        // 9 × 5 × 5 is het blok uit het prototype (1800 moleculen); 6 × 4 × 6 heeft even NX en NY.
        static readonly int[][] Blokken = { new[] { 9, 5, 5 }, new[] { 6, 4, 6 }, new[] { 2, 1, 1 } };

        static IJsrooster Maak(int[] b, int zaad = 1)
        {
            var r = new IJsrooster(b[0], b[1], b[2]);
            IJsregels.Pas(r, zaad);
            return r;
        }

        [TestCaseSource(nameof(Blokken))]
        public void AantalMoleculenIs8PerCel(int[] b)
        {
            var r = new IJsrooster(b[0], b[1], b[2]);
            Assert.That(r.Aantal, Is.EqualTo(8 * b[0] * b[1] * b[2]));
        }

        [TestCaseSource(nameof(Blokken))]
        public void ElkOAtoomHeeft4BurenOp276Angstrom(int[] b)
        {
            var r = new IJsrooster(b[0], b[1], b[2]);
            Assert.That(r.Verbindingen.Length, Is.EqualTo(2 * r.Aantal), "4 verbindingen per O, elk gedeeld door 2");
            Assert.That(r.VerbindingenVan.All(l => l.Length == 4), "elk O-atoom heeft precies 4 buren");
            foreach (var v in r.Verbindingen)
                Assert.That(Math.Sqrt(v.Dx * v.Dx + v.Dy * v.Dy + v.Dz * v.Dz), Is.EqualTo(Constanten.AfstandOO).Within(0.05));
        }

        [TestCaseSource(nameof(Blokken))]
        public void IJsregelsElkOHeeft2EigenHEn2Bruggen(int[] b)
        {
            var r = Maak(b);
            var donor = new int[r.Aantal];
            var acceptor = new int[r.Aantal];
            foreach (var v in r.Verbindingen)
            {
                Assert.That(v.Donor == v.A || v.Donor == v.B, "donor hoort bij de verbinding");
                donor[v.Donor]++;
                acceptor[v.Acceptor]++;
            }
            Assert.That(donor.All(d => d == 2), "elk O-atoom levert 2 H-atomen");
            Assert.That(acceptor.All(a => a == 2), "elk O-atoom ontvangt 2 H-bruggen");
        }

        [Test]
        public void HAtomenStaanInEenTetraederhoek()
        {
            var r = Maak(Blokken[0]);
            IJsregels.HAtomen(r, out var hx, out var hy, out var hz, out _, out _);
            for (int i = 0; i < r.Aantal; i++)
            {
                double cos = hx[2 * i] * hx[2 * i + 1] + hy[2 * i] * hy[2 * i + 1] + hz[2 * i] * hz[2 * i + 1];
                double hoek = Math.Acos(cos) * 180 / Math.PI;
                Assert.That(hoek, Is.EqualTo(109.47).Within(1.5), $"molecuul {i}");
                Assert.That(Math.Sqrt(hx[2 * i] * hx[2 * i] + hy[2 * i] * hy[2 * i] + hz[2 * i] * hz[2 * i]), Is.EqualTo(1).Within(1e-9));
            }
        }

        [Test]
        public void ZelfdeZaadGeeftZelfdeVerdelingAnderZaadEenAndere()
        {
            var r1 = Maak(Blokken[0], 7);
            var r2 = Maak(Blokken[0], 7);
            var r3 = Maak(Blokken[0], 8);
            Assert.That(r1.Verbindingen.Select(v => v.Donor), Is.EqualTo(r2.Verbindingen.Select(v => v.Donor)));
            Assert.That(r1.Verbindingen.Select(v => v.Donor), Is.Not.EqualTo(r3.Verbindingen.Select(v => v.Donor)));
        }

        [Test]
        public void HetBlokSluitNaadloosAanOverDeRand()
        {
            var r = Maak(Blokken[0]);
            int overRand = r.Verbindingen.Count(v => v.OverRand);
            Assert.That(overRand, Is.GreaterThan(0), "er lopen verbindingen over de rand");
            // een verbinding over de rand is in het echte (onverschoven) blok veel langer dan 2,76 Å
            foreach (var v in r.Verbindingen.Where(v => v.OverRand))
            {
                double rx = r.X[v.B] - r.X[v.A], ry = r.Y[v.B] - r.Y[v.A], rz = r.Z[v.B] - r.Z[v.A];
                Assert.That(Math.Sqrt(rx * rx + ry * ry + rz * rz), Is.GreaterThan(4));
            }
        }

        [TestCaseSource(nameof(Blokken))]
        public void DichtheidVanHetRoosterIs0917(int[] b)
        {
            var r = new IJsrooster(b[0], b[1], b[2]);
            Assert.That(r.Dichtheid, Is.EqualTo(Constanten.DichtheidIjs).Within(1e-9));
            Assert.That(r.Dichtheid, Is.EqualTo(0.917).Within(0.004));
        }

        [TestCaseSource(nameof(Blokken))]
        public void KanaalasIsLeeg(int[] b)
        {
            var r = new IJsrooster(b[0], b[1], b[2]);
            // een paar willekeurige plekken: de dichtstbijzijnde kanaalas moet ≥ a/√3 ≈ 2,61 Å (in xy) van elk O-atoom liggen
            var rnd = new Random(3);
            for (int t = 0; t < 20; t++)
            {
                r.DichtstbijzijndKanaal((rnd.NextDouble() - 0.5) * r.Lx, (rnd.NextDouble() - 0.5) * r.Ly, out var kx, out var ky);
                double min = double.MaxValue;
                for (int i = 0; i < r.Aantal; i++)
                {
                    double dx = IJsrooster.KortsteVerschil(r.X[i] - kx, r.Lx), dy = IJsrooster.KortsteVerschil(r.Y[i] - ky, r.Ly);
                    min = Math.Min(min, Math.Sqrt(dx * dx + dy * dy));
                }
                Assert.That(min, Is.EqualTo(r.A / Math.Sqrt(3)).Within(0.01), $"kanaal bij ({kx:F2}, {ky:F2})");
            }
        }

        [Test]
        public void BijOnevenNXenNYLigtDeOorsprongInEenKanaal()
        {
            var r = new IJsrooster(9, 5, 5);
            r.DichtstbijzijndKanaal(0.1, -0.2, out var kx, out var ky);
            Assert.That(kx, Is.EqualTo(0).Within(1e-9));
            Assert.That(ky, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void TeKleinBlokGeeftEenDuidelijkeFout()
        {
            Assert.Throws<ArgumentException>(() => new IJsrooster(1, 5, 5));
        }

        [Test]
        public void GrootBlokWerktOok()
        {
            var r = Maak(new[] { 15, 9, 9 });   // 9720 moleculen
            Assert.That(r.Aantal, Is.EqualTo(9720));
            Assert.That(r.VerbindingenVan.All(l => l.Length == 4));
        }
    }
}
