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
    }
}
