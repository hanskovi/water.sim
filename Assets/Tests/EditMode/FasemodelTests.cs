using NUnit.Framework;
using WaterSim.Sim;

namespace WaterSim.Tests
{
    public class FasemodelTests
    {
        [TestCase(-50)] [TestCase(-10)] [TestCase(0.5)] [TestCase(20)] [TestCase(90)]
        public void TemperatuurUitEnthalpieKloptTerug(double t)
        {
            Assert.That(Fasemodel.TemperatuurBij(Fasemodel.EnthalpieBij(t)), Is.EqualTo(t).Within(1e-9));
        }

        [Test]
        public void TijdensHetSmeltenBlijftHet0Graden()
        {
            var f = new Fasemodel(-10);
            bool plateauGezien = false;
            for (int i = 0; i < 2000; i++)
            {
                f.Stap(0.05, 20);
                if (f.IsAanHetSmelten)
                {
                    plateauGezien = true;
                    Assert.That(f.Temperatuur, Is.EqualTo(0), "zolang er ijs is, is het 0 °C");
                }
            }
            Assert.That(plateauGezien);
            Assert.That(f.Gesmolten, Is.EqualTo(1));
            Assert.That(f.Temperatuur, Is.GreaterThan(19).And.LessThanOrEqualTo(20));
        }

        [Test]
        public void SmeltenDuurtBij20GradenOngeveer10Seconden()
        {
            var f = new Fasemodel(0);
            double t = 0;
            while (f.Gesmolten < 1 && t < 60) { f.Stap(0.05, 20); t += 0.05; }
            Assert.That(t, Is.EqualTo(10).Within(1.5));
        }

        [Test]
        public void SmeltenKostVeelMeerWarmteDanOpwarmen()
        {
            // ijs van −10 naar 0 °C: 21 J/g; smelten: 334 J/g; water van 0 naar 10 °C: 41,8 J/g
            double opwarmenIjs = Fasemodel.EnthalpieBij(0) - Fasemodel.EnthalpieBij(-10);
            double smelten = Fasemodel.EnthalpieBij(0.0001) - Fasemodel.EnthalpieBij(0);
            double opwarmenWater = Fasemodel.EnthalpieBij(10) - Fasemodel.EnthalpieBij(0.0001);
            Assert.That(opwarmenIjs, Is.EqualTo(21).Within(0.1));
            Assert.That(smelten, Is.EqualTo(334).Within(0.1));
            Assert.That(opwarmenWater, Is.EqualTo(41.8).Within(0.1));
        }

        [Test]
        public void BevriezenGaatOokVia0Graden()
        {
            var f = new Fasemodel(15);
            bool plateauGezien = false;
            for (int i = 0; i < 3000; i++)
            {
                f.Stap(0.05, -20);
                if (f.IsAanHetSmelten) { plateauGezien = true; Assert.That(f.Temperatuur, Is.EqualTo(0)); }
            }
            Assert.That(plateauGezien);
            Assert.That(f.Gesmolten, Is.EqualTo(0));
            Assert.That(f.Temperatuur, Is.LessThan(-19));
        }

        [Test]
        public void WaterBlijftOnder100Graden()
        {
            var f = new Fasemodel(50);
            for (int i = 0; i < 2000; i++) f.Stap(0.05, 150);
            Assert.That(f.Temperatuur, Is.LessThanOrEqualTo(Fasemodel.MaxTemperatuur));
        }
    
        [Test]
        public void ResterendeTijdKloptMetDeWarmtestroom()
        {
            // net begonnen met smelten bij 20 °C: 334 J/g / (1,67 · 20) = 10 s
            var f = new Fasemodel(0) { Enthalpie = 1e-9 };
            Assert.That(f.ResterendeTijd(20), Is.EqualTo(334 / (1.67 * 20)).Within(1e-6));
            // halverwege stollen bij −10 °C: 167 J/g / (1,67 · 10) = 10 s
            f.Enthalpie = 167;
            Assert.That(f.ResterendeTijd(-10), Is.EqualTo(10).Within(1e-6));
            // omgeving 0 °C: evenwicht, er gebeurt netto niets
            Assert.That(double.IsPositiveInfinity(f.ResterendeTijd(0)), Is.True);
            // buiten het plateau geen tijd
            Assert.That(double.IsPositiveInfinity(new Fasemodel(10).ResterendeTijd(20)), Is.True);
        }

        [Test]
        public void ResterendeTijdKomtUitMetDeSimulatie()
        {
            var f = new Fasemodel(0) { Enthalpie = 50 };
            double verwacht = f.ResterendeTijd(20);
            double t = 0;
            while (f.IsAanHetSmelten && t < 100) { f.Stap(0.01, 20); t += 0.01; }
            Assert.That(t, Is.EqualTo(verwacht).Within(0.05));
        }
}
}
