using System;
using NUnit.Framework;
using WaterSim.Sim;

namespace WaterSim.Tests
{
    public class MeerTests
    {
        const double Dag = 86400;

        [Test]
        public void IJsDrijftOpWaterEnZinktInAlcohol()
        {
            Assert.That(Drijven.Drijft(Constanten.DichtheidIjsBinas, Constanten.DichtheidWater(0)), Is.True);
            Assert.That(Drijven.DeelOnder(Constanten.DichtheidIjsBinas, Constanten.DichtheidWater(0)), Is.EqualTo(0.917).Within(0.002));
            Assert.That(Drijven.DeelOnder(Constanten.DichtheidIjsBinas, Constanten.DichtheidZeewater), Is.EqualTo(0.895).Within(0.002));
            Assert.That(Drijven.Drijft(Constanten.DichtheidIjsBinas, Constanten.DichtheidEthanol), Is.False);
            Assert.That(Drijven.DeelOnder(Constanten.DichtheidIjsBinas, Constanten.DichtheidEthanol), Is.EqualTo(1));
        }

        [Test]
        public void BovenVierGradenMengtHetHeleMeer()
        {
            var m = new Meer(24, 6, 12);
            for (int d = 0; d < 3; d++) m.Stap(Dag, -5);
            Assert.That(m.T[0], Is.GreaterThan(4.2), "nog niet onder 4 °C");
            Assert.That(m.Bodem - m.T[0], Is.LessThan(0.5), "boven 4 °C is het meer goed gemengd");
        }

        [Test]
        public void InDeWinterIsDeBodemVierGradenEnLigtErIJsBovenop()
        {
            var m = new Meer(24, 6, 12);
            for (int d = 0; d < 40; d++) m.Stap(Dag, -10);
            Assert.That(m.Ijsdikte, Is.GreaterThan(0.1), "er ligt ijs");
            Assert.That(m.Bodem, Is.EqualTo(Constanten.TemperatuurMaxDichtheid).Within(0.3), "bodem ≈ 4 °C");
            Assert.That(m.T[0], Is.LessThan(1.5), "vlak onder het ijs is het bijna 0 °C");
        }

        [Test]
        public void ZwaarWaterLigtNooitBovenLichtWater()
        {
            var m = new Meer(24, 6, 12);
            for (int s = 0; s < 200; s++)
            {
                m.Stap(3 * 3600, s < 120 ? -8 : 15);
                for (int i = 0; i < m.Lagen - 1; i++)
                    Assert.That(Constanten.DichtheidWater(m.T[i]), Is.LessThanOrEqualTo(Constanten.DichtheidWater(m.T[i + 1]) + 1e-9));
            }
        }

        [Test]
        public void IJsSmeltAlsHetWarmWordt()
        {
            var m = new Meer(24, 6, 4);
            for (int d = 0; d < 20; d++) m.Stap(Dag, -10);
            Assert.That(m.Ijsdikte, Is.GreaterThan(0));
            for (int d = 0; d < 40; d++) m.Stap(Dag, 15);
            Assert.That(m.Ijsdikte, Is.EqualTo(0));
            Assert.That(m.T[0], Is.GreaterThan(4));
        }
    
        [Test]
        public void BijDooiSmeltHetIJsVanBoven()
        {
            var m = new Meer(24, 6, 4);
            for (int d = 0; d < 25; d++) m.Stap(Dag, -10);
            double voor = m.Ijsdikte, boven0 = m.GesmoltenBoven, onder0 = m.GesmoltenOnder;
            for (int d = 0; d < 3; d++) m.Stap(Dag, 10);
            Assert.That(m.Ijsdikte, Is.LessThan(voor));
            Assert.That(m.GesmoltenBoven - boven0, Is.GreaterThan(5 * (m.GesmoltenOnder - onder0)), "meeste smelt aan de bovenkant");
            Assert.That(m.Smeltwater, Is.GreaterThan(0), "smeltwater op het ijs");
        }

        [Test]
        public void EenJaarGeeftWinterijsEnZomerspronglaag()
        {
            var m = new Meer(24, 6, 12);
            double start = 274;                          // 1 oktober
            double winterIjs = 0, winterBodem = 0, zomerVerschil = 0;
            for (int d = 0; d < 365; d++)
            {
                double dag = (start + d) % Seizoen.DagenPerJaar;
                m.Zon = Seizoen.Zon(dag);
                m.Stap(Dag, Seizoen.Lucht(dag));
                if ((int)dag == 40) { winterIjs = m.Ijsdikte; winterBodem = m.Bodem; }
                if ((int)dag == 200) zomerVerschil = m.T[0] - m.Bodem;
            }
            Assert.That(winterIjs, Is.GreaterThan(0.05), "ijs in februari");
            Assert.That(winterBodem, Is.EqualTo(4).Within(0.6), "bodem ≈ 4 °C onder het ijs");
            Assert.That(zomerVerschil, Is.GreaterThan(4), "zomer: warm water bovenop (spronglaag)");
            Assert.That(m.Ijsdikte, Is.EqualTo(0), "op 1 oktober geen ijs");
        }

        [Test]
        public void SmeltendIJsInZoetWaterLaatHetNiveauGelijk()
        {
            double rw = Constanten.DichtheidWater(0), ri = Constanten.DichtheidIjsBinas;
            Assert.That(Drijven.Niveaustijging(100, 50, ri, rw, rw, true, 1), Is.EqualTo(0).Within(1e-9));
            Assert.That(Drijven.Niveaustijging(100, 50, ri, Constanten.DichtheidZeewater, rw, true, 1), Is.GreaterThan(0).And.LessThan(0.1));
            Assert.That(Drijven.Niveaustijging(100, 50, ri, rw, rw, false, 1), Is.EqualTo(100 * ri / rw / 50).Within(1e-9), "landijs: alles erbij");
            Assert.That(Drijven.Niveaustijging(100, 50, ri, Constanten.DichtheidEthanol, rw, true, 1), Is.LessThan(0), "gezonken ijs in alcohol: niveau daalt");
        }

        [Test]
        public void ToestandVanHetIJsVolgtDeEnergiebalans()
        {
            var m = new Meer(24, 6, 4);
            for (int d = 0; d < 10; d++) m.Stap(Dag, -10);
            Assert.That(m.Toestand, Is.EqualTo(Meer.IJstoestand.Groeit), "vorst zonder zon: groeit");

            var dooi = new Meer(24, 6, 4);
            for (int d = 0; d < 20; d++) dooi.Stap(Dag, -10);
            for (int d = 0; d < 2; d++) dooi.Stap(Dag, 5);
            Assert.That(dooi.Toestand, Is.EqualTo(Meer.IJstoestand.SmeltBoven), "dooi zonder zon: smelt aan de bovenkant");

            // lichte vorst met voorjaarszon: het licht verwarmt het water, het ijs groeit netto niet meer
            var zon = new Meer(24, 6, 4);
            for (int d = 0; d < 6; d++) zon.Stap(Dag, -6);
            zon.Zon = Seizoen.ZonVoorjaar;
            for (int d = 0; d < 15; d++) zon.Stap(Dag, -1);
            Assert.That(zon.NettoPerDag, Is.LessThanOrEqualTo(0.0005), "geen netto groei bij −1 °C met zon");
            Assert.That(zon.Toestand, Is.Not.EqualTo(Meer.IJstoestand.Groeit));
        }

        // een jaar in dit klimaat, vanaf de herfst; geeft de dikste ijslaag en de laagste bodemtemperatuur onder ijs
        static double DiksteIJs(Klimaat k, bool streng)
        {
            var m = new Meer(24, 6, Math.Max(4, k.Lucht(k.StartDag) + 2));
            double max = 0;
            for (int d = 0; d < 365; d++)
            {
                double dag = (k.StartDag + d) % Seizoen.DagenPerJaar;
                m.Zon = k.Zon(dag);
                m.Stap(Dag, k.Lucht(dag, streng));
                max = Math.Max(max, m.Ijsdikte);
            }
            return max;
        }

        [Test]
        public void KlimaatVolgtHetHalfrond()
        {
            var nl = Klimaat.VoorLand("NL"); var nz = Klimaat.VoorLand("NZ");
            Assert.That(nl.Lucht(15), Is.LessThan(nl.Lucht(196)), "noorden: januari kouder dan juli");
            Assert.That(nz.Lucht(196), Is.LessThan(nz.Lucht(15)), "zuiden: juli kouder dan januari");
            Assert.That(nl.Zon(171), Is.GreaterThan(4 * nl.Zon(354)), "NL: juni veel meer zon dan december");
            Assert.That(nz.Zon(354), Is.GreaterThan(nz.Zon(171)), "NZ: december meer zon dan juni");
            Assert.That(Klimaat.Zonlicht(52, 171), Is.EqualTo(230).Within(30), "NL juni ≈ 230 W/m² aan de grond");
            Assert.That(Klimaat.VoorLand("XX"), Is.SameAs(nl), "onbekend land: Nederland");
        }

        [Test]
        public void IJsHangtAfVanHetKlimaat()
        {
            Assert.That(DiksteIJs(Klimaat.VoorLand("NL"), false), Is.LessThan(0.02), "gewone Nederlandse winter: (bijna) geen ijs");
            Assert.That(DiksteIJs(Klimaat.VoorLand("NL"), true), Is.GreaterThan(0.10), "strenge winter (1963): schaatsijs");
            Assert.That(DiksteIJs(Klimaat.VoorLand("CA"), false), Is.GreaterThan(0.3), "Ottawa: dik ijs");
            Assert.That(DiksteIJs(Klimaat.VoorLand("SG"), false), Is.EqualTo(0), "Singapore: nooit ijs");
        }
}
}
