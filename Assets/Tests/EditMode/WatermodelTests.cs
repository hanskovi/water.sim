using System;
using System.Linq;
using NUnit.Framework;
using WaterSim.Sim;

namespace WaterSim.Tests
{
    public class WatermodelTests
    {
        const float Dt = 1f / 30f;

        static Watermodel Maak(bool oneindig, double start = -10)
        {
            return new Watermodel(9, 5, 5, zaad: 3, startTemperatuur: start) { Oneindig = oneindig };
        }

        static void Loop(Watermodel m, float seconden, float tOmgeving)
        {
            for (int s = 0; s < seconden / Dt; s++) m.Stap(Dt, tOmgeving);
        }

        // gemiddeld aantal bruggen over een stukje tijd (de vloeistof schommelt)
        static float GemiddeldeBruggen(Watermodel m, float seconden, float tOmgeving)
        {
            float som = 0; int n = 0;
            for (int s = 0; s < seconden / Dt; s++) { m.Stap(Dt, tOmgeving); som += m.BruggenNu(); n++; }
            return som / n;
        }

        static Watermodel Vloeibaar(bool oneindig, float t)
        {
            var m = Maak(oneindig);
            m.Fase.Enthalpie = Fasemodel.EnthalpieBij(t);
            Loop(m, 6f, t);
            return m;
        }

        [Test]
        public void StartAlsIJsMet4BruggenPerMolecuul()
        {
            var m = Maak(true);
            Assert.That(m.Vloeibaar.Any(v => v), Is.False);
            Assert.That(m.BruggenNu(), Is.EqualTo(4f).Within(1e-6));
        }

        [Test]
        public void TijdensSmeltenBlijft0GradenEnSmeltHetStapVoorStap()
        {
            var m = Maak(true, -5);
            int vorige = 0; bool plateau = false;
            for (int s = 0; s < 30f / Dt; s++)
            {
                m.Stap(Dt, 20f);
                int nu = m.Vloeibaar.Count(v => v);
                if (m.Fase.IsAanHetSmelten)
                {
                    plateau = true;
                    Assert.That(m.Fase.Temperatuur, Is.EqualTo(0));
                    Assert.That(nu, Is.GreaterThanOrEqualTo(vorige), "smelten gaat één kant op");
                }
                vorige = nu;
            }
            Assert.That(plateau);
            Assert.That(m.Vloeibaar.All(v => v), "na 30 s bij 20 °C is alles gesmolten");
        }

        [Test]
        public void VloeibaarWaterHeeftMinderBruggenDanIJsEnMinderAlsHetWarmerIs()
        {
            float koud = GemiddeldeBruggen(Vloeibaar(false, 1f), 2f, 1f);
            float heet = GemiddeldeBruggen(Vloeibaar(false, 90f), 2f, 90f);
            string meting = $"gemeten: 1 °C = {koud:F2}, 90 °C = {heet:F2} bruggen per molecuul";
            Assert.That(koud, Is.InRange(3.1f, 3.8f), "≈ 3,5 bij 1 °C; " + meting);
            Assert.That(heet, Is.InRange(2.5f, 3.2f), "≈ 2,9 bij 90 °C; " + meting);
            Assert.That(heet, Is.LessThan(koud - 0.25f), meting);
        }

        [Test]
        public void LosBlokKrimptBijSmelten()
        {
            var m = Vloeibaar(false, 4f);
            float verwacht = -m.Ly / 2 + m.Ly * 0.917f / (float)Constanten.DichtheidWater(4);
            Assert.That(m.Bovenkant, Is.EqualTo(verwacht).Within(0.05f));
            Assert.That(m.Bovenkant, Is.LessThan(m.Ly / 2 - 2f), "het water staat lager dan het ijs stond");
            for (int i = 0; i < m.N; i++) Assert.That(m.Y[i], Is.LessThanOrEqualTo(m.Bovenkant));
            Assert.That(m.Dichtheid, Is.EqualTo(1.0).Within(0.001));
        }

        [Test]
        public void HAtomenBlijvenInEenTetraederhoekInDeVloeistof()
        {
            var m = Vloeibaar(true, 20f);
            for (int i = 0; i < m.N; i++)
            {
                int o = 6 * i;
                float l0 = (float)Math.Sqrt(m.H[o] * m.H[o] + m.H[o + 1] * m.H[o + 1] + m.H[o + 2] * m.H[o + 2]);
                float cos = m.H[o] * m.H[o + 3] + m.H[o + 1] * m.H[o + 4] + m.H[o + 2] * m.H[o + 5];
                Assert.That(l0, Is.EqualTo(1f).Within(1e-3));
                Assert.That(Math.Acos(cos) * 180 / Math.PI, Is.EqualTo(109.47).Within(0.5));
            }
        }

        [Test]
        public void BevriezenGeeftWeerPerfectIJs()
        {
            var m = Vloeibaar(true, 5f);
            Loop(m, 45f, -20f);
            Assert.That(m.Vloeibaar.Any(v => v), Is.False, "alles weer ijs");
            Assert.That(m.Fase.Temperatuur, Is.LessThan(0));
            Assert.That(m.BruggenNu(), Is.EqualTo(4f).Within(1e-6), "weer 4 bruggen per molecuul");
        }

        [Test]
        public void VloeistofBlijftBinnenHetBlokOfWordtPeriodiekTeruggezet()
        {
            var m = Vloeibaar(true, 30f);
            for (int i = 0; i < m.N; i++)
            {
                Assert.That(Math.Abs(m.X[i]), Is.LessThanOrEqualTo(m.Lx / 2 + 0.01f));
                Assert.That(Math.Abs(m.Y[i]), Is.LessThanOrEqualTo(m.Ly / 2 + 0.01f));
                Assert.That(Math.Abs(m.Z[i]), Is.LessThanOrEqualTo(m.Lz / 2 + 0.01f));
            }
        }
    
        [Test]
        public void BijStollenGroeitHetIJsGeleidelijkEnReistNiemandVer()
        {
            // traject +5 → −4 °C: het ijs groeit waar het water is (vanaf een kiem, aan het front), dus bijna geen molecuul
            // hoeft ver naar zijn roosterplek, ook niet aan het eind van het traject
            var m = Vloeibaar(true, 5f);
            var was = m.Vloeibaar.ToArray();
            int ver = 0, halverwege = 0; double plateau = 0, achter = 0;
            for (int s = 0; s < 1600 && (m.Fase.Gesmolten > 0 || m.Vloeibaar.Any(v => v)); s++)
            {
                m.Stap(0.05f, -4f);
                if (m.Fase.IsAanHetSmelten) plateau += 0.05; else if (m.Fase.Gesmolten <= 0) achter += 0.05;
                for (int i = 0; i < m.N; i++)
                {
                    if (!was[i] || m.Vloeibaar[i]) continue;                     // net bevroren: hoe ver is de roosterplek?
                    float dx = Watermodel.Kortst(m.X[i] - (float)m.Rooster.X[i], m.Lx), dy = Watermodel.Kortst(m.Y[i] - (float)m.Rooster.Y[i], m.Ly), dz = Watermodel.Kortst(m.Z[i] - (float)m.Rooster.Z[i], m.Lz);
                    if (Math.Sqrt(dx * dx + dy * dy + dz * dz) > 3.5) ver++;
                }
                was = m.Vloeibaar.ToArray();
                if (halverwege == 0 && m.Fase.Gesmolten < 0.5) halverwege = m.Vloeibaar.Count(v => !v);
            }
            Assert.That(m.Vloeibaar.Any(v => v), Is.False, "alles bevroren");
            Assert.That(plateau, Is.LessThanOrEqualTo(Constanten.MaxPlateauSeconden + 1), "stollen duurt hooguit ~15 s");
            Assert.That(achter, Is.LessThan(2), "het ijs loopt hooguit even achter op het plateau");
            Assert.That(halverwege, Is.GreaterThan(m.N / 3), "halverwege het plateau is al een flink deel ijs");
            // de enkele moleculen die toch van verder komen (uit een kanaal van het ijs) glijden rustig naar hun plek
            Assert.That(ver, Is.LessThan(m.N / 50), $"hooguit 2 % komt van verder dan 3,5 Å ({ver} van {m.N})");
        }

        [Test]
        public void BijBevriezenReistGeenMolecuulVerEnZijnErGeenLangeBruggen()
        {
            var m = new Watermodel(5, 3, 3, 1, -5) { Parallel = false };
            for (int s = 0; s < 400 && m.Fase.Gesmolten < 1; s++) m.Stap(0.05f, 30);    // smelten
            for (int s = 0; s < 100; s++) m.Stap(0.05f, 30);                              // even als vloeistof bewegen
            var px = (float[])m.X.Clone(); var py = (float[])m.Y.Clone(); var pz = (float[])m.Z.Clone();
            float maxSprong = 0, maxBrug = 0;
            for (int s = 0; s < 600 && m.Fase.Gesmolten > 0; s++)                          // bevriezen
            {
                m.Stap(0.05f, -30);
                for (int i = 0; i < m.N; i++)
                {
                    // moleculen zijn niet te onderscheiden (bij bevriezen ruilen ze van nummer): kijk naar de dichtstbijzijnde
                    // positie van de vorige stap, welk nummer die ook had
                    float kort = float.MaxValue;
                    for (int q = 0; q < m.N; q++)
                    {
                        float dx = Watermodel.Kortst(m.X[i] - px[q], m.Lx), dy = Watermodel.Kortst(m.Y[i] - py[q], m.Ly), dz = Watermodel.Kortst(m.Z[i] - pz[q], m.Lz);
                        kort = System.Math.Min(kort, dx * dx + dy * dy + dz * dz);
                    }
                    maxSprong = System.Math.Max(maxSprong, (float)System.Math.Sqrt(kort));
                    for (int k = 0; k < 2; k++)
                    {
                        int j = m.Acceptor[2 * i + k];
                        if (j < 0) continue;
                        float bx = Watermodel.Kortst(m.X[j] - m.X[i], m.Lx), by = Watermodel.Kortst(m.Y[j] - m.Y[i], m.Ly), bz = Watermodel.Kortst(m.Z[j] - m.Z[i], m.Lz);
                        maxBrug = System.Math.Max(maxBrug, (float)System.Math.Sqrt(bx * bx + by * by + bz * bz));
                    }
                }
                System.Array.Copy(m.X, px, m.N); System.Array.Copy(m.Y, py, m.N); System.Array.Copy(m.Z, pz, m.N);
            }
            Assert.That(m.Fase.Gesmolten, Is.EqualTo(0), "alles bevroren");
            Assert.That(maxSprong, Is.LessThan(1.0f), "geen molecuul verspringt of racet (per stap van 0,05 s)");
            Assert.That(maxBrug, Is.LessThan(m.BrugAfstand + 1.0f), "geen lange H-bruggen (O–O; de vloeistof beweegt na het zoeken nog een stapje)");
        }
}
}
