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
