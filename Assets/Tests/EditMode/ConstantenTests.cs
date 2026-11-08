using NUnit.Framework;
using WaterSim.Sim;

namespace WaterSim.Tests
{
    public class ConstantenTests
    {
        [Test]
        public void DichtheidIjsUitRoosterIsOngeveer0917()
        {
            Assert.That(Constanten.DichtheidIjs, Is.EqualTo(0.917).Within(0.004));
        }

        [Test]
        public void WaterIsHetZwaarstBij4Graden()
        {
            double max = Constanten.DichtheidWater(3.98);
            Assert.That(Constanten.DichtheidWater(0), Is.LessThan(max));
            Assert.That(Constanten.DichtheidWater(8), Is.LessThan(max));
            Assert.That(max, Is.EqualTo(1.0).Within(0.0001));
        }

        [Test]
        public void IjsDrijftOpWater()
        {
            Assert.That(Constanten.DichtheidIjs, Is.LessThan(Constanten.DichtheidWater(0)));
        }

        [Test]
        public void VerdampenKostOngeveer7KeerZoveelAlsSmelten()
        {
            Assert.That(Constanten.Verdampingswarmte / Constanten.Smeltwarmte, Is.EqualTo(6.8).Within(0.1));
        }
    }
}
