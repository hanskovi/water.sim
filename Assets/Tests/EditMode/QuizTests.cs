using NUnit.Framework;
using WaterSim.Sim;

namespace WaterSim.Tests
{
    public class QuizTests
    {
        [Test]
        public void GetallenWordenGelezenZoalsLeerlingenTypen()
        {
            Assert.That(Quiz.GetalKlopt("104,5", 104.5, 1), Is.True);
            Assert.That(Quiz.GetalKlopt(" 104.5 ", 104.5, 1), Is.True);
            Assert.That(Quiz.GetalKlopt("−60", -60, 0), Is.True);
            Assert.That(Quiz.GetalKlopt("-60", -60, 0), Is.True);
            Assert.That(Quiz.GetalKlopt("3 760", 3760, 40), Is.True);
            Assert.That(Quiz.GetalKlopt("106", 104.5, 1), Is.False);
            Assert.That(Quiz.GetalKlopt("abc", 3, 0), Is.False);
            Assert.That(Quiz.GetalKlopt("", 3, 0), Is.False);
            Assert.That(Quiz.GetalKlopt("4", 4, 0), Is.True);
            Assert.That(Quiz.GetalKlopt("4,01", 4, 0), Is.False);
        }

        [Test]
        public void StreakTeltPuntenEnOnthoudtDeBeste()
        {
            var s = new Streak(3);
            s.Goed(); s.Goed();
            Assert.That(s.Huidig, Is.EqualTo(2)); Assert.That(s.Beste, Is.EqualTo(3));
            Assert.That(s.Goed(Quiz.PuntenTekenen(2)), Is.True, "4 punten voor XPRT-tekenen: nieuwe beste");
            Assert.That(s.Beste, Is.EqualTo(6));
            s.Fout();
            Assert.That(s.Huidig, Is.EqualTo(0)); Assert.That(s.Beste, Is.EqualTo(6));
        }

        [Test]
        public void SchuddenGeeftAlleIndicesPreciesEenKeer()
        {
            var a = Quiz.Schud(10, new System.Random(1));
            var gezien = new bool[10];
            foreach (int i in a) { Assert.That(gezien[i], Is.False); gezien[i] = true; }
        }
    
        [Test]
        public void MachtenVanTienEnDecimalen()
        {
            // 6,022·10²³ moleculen per mol, antwoord in eenheden van 10²³
            foreach (var inv in new[] { "6,022", "6.022e23", "6,022E23", "6,022·10^23", "6,022 × 10²³", "6,022x10^23", "60,22e22", "6.022*10^23" })
                Assert.That(Quiz.GetalKlopt(inv, 6.022, 0.01, 23), Is.True, inv);
            Assert.That(Quiz.GetalKlopt("6,022e22", 6.022, 0.01, 23), Is.False, "factor 10 te klein");
            Assert.That(Quiz.GetalKlopt("3,34·10^22", 3.34, 0.1, 22), Is.True);
            // molecuulmassa: minstens 1 cijfer achter de komma
            Assert.That(Quiz.GetalKlopt("18", 18.015, 0.02, 0, 1), Is.False, "zonder decimaal");
            Assert.That(Quiz.GetalKlopt("18,0", 18.015, 0.02, 0, 1), Is.True);
            Assert.That(Quiz.GetalKlopt("18,015", 18.015, 0.02, 0, 1), Is.True);
            Assert.That(Quiz.GetalKlopt("18,1", 18.015, 0.02, 0, 1), Is.False);
        }
}
}
