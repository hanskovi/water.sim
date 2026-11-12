using System;
using System.Collections.Generic;
using NUnit.Framework;
using WaterSim.Sim;
using B = WaterSim.Sim.HBrugRegels.Brug;
using O = WaterSim.Sim.HBrugRegels.Oordeel;

namespace WaterSim.Tests
{
    public class HBrugTests
    {
        static int Eerste(Molecuul2D m, Element e, bool donor = false)
        {
            for (int i = 0; i < m.Atomen.Length; i++)
                if (m.Atomen[i] == e && (!donor || HBrugRegels.IsDonorH(m, i))) return i;
            return -1;
        }
        static int EersteHAanC(Molecuul2D m)
        {
            for (int i = 0; i < m.Atomen.Length; i++)
                if (m.Atomen[i] == Element.H && m.Atomen[m.Buur(i)] == Element.C) return i;
            return -1;
        }

        [Test]
        public void WaterNaarWaterIsGoed()
        {
            var w = Molecuulbibliotheek.Water;
            var mol = new[] { w, w };
            var r = HBrugRegels.Beoordeel(mol, new[] { new B { Van = 0, H = 1, Doel = 1, Naar = 0 } });
            Assert.That(r[0], Is.EqualTo(O.Goed));
        }

        [Test]
        public void FoutenWordenHerkend()
        {
            var w = Molecuulbibliotheek.Water; var me = Molecuulbibliotheek.Methanol; var s = Molecuulbibliotheek.Waterstofsulfide;
            var mol = new[] { w, me, s, w };
            var r = HBrugRegels.Beoordeel(mol, new[]
            {
                new B { Van = 1, H = EersteHAanC(me), Doel = 0, Naar = 0 },          // H aan C
                new B { Van = 0, H = 1, Doel = 2, Naar = 0 },                         // naar S
                new B { Van = 0, H = 1, Doel = 0, Naar = 0 },                         // zelfde molecuul
                new B { Van = 0, H = 0, Doel = 3, Naar = 0 },                         // begint niet bij H
                new B { Van = 2, H = 1, Doel = 0, Naar = 0 },                         // H aan S
            });
            Assert.That(r, Is.EqualTo(new[] { O.HNietAanNOfO, O.AcceptorNietNOfO, O.ZelfdeMolecuul, O.BeginNietBijH, O.HNietAanNOfO }));
        }

        [Test]
        public void EenHEenKeerEnNMaarEenPaar()
        {
            var w = Molecuulbibliotheek.Water; var nh3 = Molecuulbibliotheek.Ammoniak;
            var mol = new[] { w, w, nh3, w };
            var r = HBrugRegels.Beoordeel(mol, new[]
            {
                new B { Van = 0, H = 1, Doel = 2, Naar = 0 },    // goed: naar N
                new B { Van = 1, H = 1, Doel = 2, Naar = 0 },    // N heeft maar 1 vrij paar
                new B { Van = 0, H = 1, Doel = 3, Naar = 0 },    // deze H heeft al een brug
                new B { Van = 1, H = 2, Doel = 3, Naar = 0 },    // goed
                new B { Van = 2, H = 1, Doel = 3, Naar = 0 },    // goed: tweede paar van O
                new B { Van = 2, H = 2, Doel = 3, Naar = 0 },    // O heeft maar 2 vrije paren
            });
            Assert.That(r, Is.EqualTo(new[] { O.Goed, O.ParenOp, O.HAlBezet, O.Goed, O.Goed, O.ParenOp }));
        }

        [Test]
        public void AcetonOntvangtMaarGeeftNiet()
        {
            var ac = Molecuulbibliotheek.Aceton; var w = Molecuulbibliotheek.Water;
            Assert.That(ac.IsAcceptor, Is.True); Assert.That(ac.IsDonor, Is.False);
            Assert.That(Molecuulbibliotheek.Methaan.IsAcceptor || Molecuulbibliotheek.Methaan.IsDonor, Is.False);
            Assert.That(Molecuulbibliotheek.Waterstofsulfide.IsDonor, Is.False, "H₂S: schoolnorm geen H-brug");
            var r = HBrugRegels.Beoordeel(new[] { w, ac }, new[] { new B { Van = 0, H = 1, Doel = 1, Naar = Eerste(ac, Element.O) } });
            Assert.That(r[0], Is.EqualTo(O.Goed));
        }

        [Test]
        public void ZurenGevenEnOntvangen()
        {
            foreach (var z in new[] { Molecuulbibliotheek.Methaanzuur, Molecuulbibliotheek.Ethaanzuur })
            {
                Assert.That(z.IsDonor && z.IsAcceptor, Is.True, z.Sleutel);
                var mols = new[] { z, z };
                int oh = -1, oDubbel = -1;
                for (int a = 0; a < z.Atomen.Length; a++) if (HBrugRegels.IsDonorH(z, a)) oh = a;
                foreach (var (a, b, orde) in z.Bindingen) if (orde == 2) oDubbel = z.Atomen[a] == Element.O ? a : b;
                var o = HBrugRegels.Beoordeel(mols, new[] { new HBrugRegels.Brug { Van = 0, H = oh, Doel = 1, Naar = oDubbel } });
                Assert.That(o[0], Is.EqualTo(HBrugRegels.Oordeel.Goed), z.Sleutel + ": O–H naar C=O");
            }
        }

        [Test]
        public void PolaireAtomenWijzenNaarEenPartner()
        {
            // 6 moleculen in 2 kolommen × 3 rijen, om en om water en aceton: de O van aceton wijst naar een watermolecuul
            var rnd = new System.Random(5);
            var w = Molecuulbibliotheek.Water; var ac = Molecuulbibliotheek.Aceton;
            var mols = new List<Molecuul2D>(); var plek = new List<(double, double)>();
            for (int rij = 0; rij < 3; rij++) for (int kol = 0; kol < 2; kol++) { mols.Add((rij + kol) % 2 == 0 ? w : ac); plek.Add((0.25 + kol * 0.5, 1 - (rij + 0.5) / 3)); }
            var hoek = Schikking.Richt(mols, plek, rnd);
            int o = Array.IndexOf(ac.Atomen, Element.O);
            for (int i = 0; i < 6; i++)
            {
                if (mols[i] != ac) continue;
                Schikking.Midden(ac, out double mx, out double my);
                double c = Math.Cos(hoek[i] * Math.PI / 180), s = Math.Sin(hoek[i] * Math.PI / 180);
                double vx = ac.X[o] - mx, vy = ac.Y[o] - my, rx = c * vx - s * vy, ry = s * vx + c * vy;
                double besteCos = -1;
                for (int j = 0; j < 6; j++)
                {
                    if (mols[j] != w) continue;
                    double dx = plek[j].Item1 - plek[i].Item1, dy = plek[j].Item2 - plek[i].Item2;
                    besteCos = Math.Max(besteCos, (rx * dx + ry * dy) / (Math.Sqrt(rx * rx + ry * ry) * Math.Sqrt(dx * dx + dy * dy)));
                }
                Assert.That(besteCos, Is.GreaterThan(0.8), "de O van aceton wijst naar een watermolecuul");
            }
        }

        [Test]
        public void ErKunnenAltijdGenoegBruggenGetekendWorden()
        {
            var rnd = new System.Random(11);
            for (int niv = 0; niv < 3; niv++)
                for (int k = 0; k < 300; k++)
                {
                    var m = Molecuulbibliotheek.Opdracht(niv, rnd);
                    Assert.That(Molecuulbibliotheek.MaxBruggen(m), Is.GreaterThanOrEqualTo(HBrugRegels.Nodig(niv)), string.Join(", ", Array.ConvertAll(m, x => x.Sleutel)));
                }
            // het voorbeeld uit de test: methanol en aceton kan maar 3, met water erbij wel 4
            var ma = new[] { Molecuulbibliotheek.Methanol, Molecuulbibliotheek.Aceton, Molecuulbibliotheek.Aceton, Molecuulbibliotheek.Methanol, Molecuulbibliotheek.Methanol, Molecuulbibliotheek.Aceton };
            Assert.That(Molecuulbibliotheek.MaxBruggen(ma), Is.EqualTo(3));
        }

        [Test]
        public void ElkeOpdrachtIsTeDoen()
        {
            var rnd = new System.Random(3);
            for (int niv = 0; niv < 3; niv++)
                for (int k = 0; k < 50; k++)
                {
                    var (a, b) = Molecuulbibliotheek.Kies(niv, rnd);
                    Assert.That(a.IsDonor && a.IsAcceptor, Is.True, "de eerste stof kan altijd met zichzelf H-bruggen vormen");
                    if (niv < 2) Assert.That(a, Is.SameAs(Molecuulbibliotheek.Water));
                    else Assert.That(b.IsDonor, Is.False, "XPRT: de tweede stof geeft geen H-bruggen");
                }
        }
    }
}
