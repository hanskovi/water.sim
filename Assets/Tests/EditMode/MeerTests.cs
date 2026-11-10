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
}
}
