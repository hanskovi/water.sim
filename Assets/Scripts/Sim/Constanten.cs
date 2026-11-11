namespace WaterSim.Sim
{
    /// <summary>
    /// Vaste getallen voor water en ijs. Afstanden in ångström (Å), temperaturen in °C,
    /// warmte in J/g, dichtheid in g/cm³.
    /// </summary>
    public static class Constanten
    {
        // --- IJs Ih (zeshoekig), roosterconstanten bij ongeveer −10 °C ---
        public const double IjsA = 4.52;          // Å, zijde van de zeshoekige cel
        public const double IjsC = 7.36;          // Å, hoogte van de cel langs de c-as
        public const double AfstandOO = 2.76;     // Å, O–O-afstand bij een waterstofbrug
        public const double AfstandOH = 0.96;     // Å, covalente O–H-binding
        public const double TrillingIjs = 0.15;    // Å, uitslag van een O-atoom in ijs bij 0 °C (ordegrootte)
        public const double HoekHOH = 104.5;      // °, hoek H–O–H in een los watermolecuul
        public const double HoekTetraeder = 109.47; // °, hoek tussen de bindingsrichtingen in ijs (tetraëder)
        public const int MoleculenPerRechthoekigeCel = 8;   // cel a × a√3 × c

        // --- Stofgegevens ---
        public const double MolaireMassaWater = 18.015;     // g/mol
        public const double Avogadro = 6.02214076e23;       // 1/mol

        // --- Dichtheid van andere vloeistoffen (g/cm³, Binas, bij ≈ 20 °C) ---
        public const double DichtheidZeewater = 1.025;
        public const double DichtheidEthanol = 0.789;

        // --- Poolkappen: zeespiegelstijging als al het ijs zou smelten (m) ---
        public const double ZeespiegelGroenland = 7.4;      // Groenlandse ijskap (Morlighem e.a. 2017)
        public const double ZeespiegelAntarctica = 58;      // Antarctische ijskap (Fretwell e.a. 2013)

        // --- Warmtegeleiding (Binas) ---
        public const double WarmtegeleidingIjs = 2.2;       // W/(m·K), bij 0 °C
        public const double WarmteoverdrachtMeer = 35;      // W/(m²·K), lucht ↔ oppervlak van het meer; model (wind, uitstraling, verdamping)

        // --- Warmte (Binas-waarden, afgerond) ---
        public const double Smeltwarmte = 334;              // J/g bij 0 °C
        public const double Verdampingswarmte = 2257;       // J/g bij 100 °C
        public const double SoortelijkeWarmteIjs = 2.1;     // J/(g·K)
        public const double SoortelijkeWarmteWater = 4.18;  // J/(g·K)
        public const double SoortelijkeWarmteStoom = 2.0;   // J/(g·K)

        /// <summary>Volume van de rechthoekige ijscel (a × a√3 × c) in Å³.</summary>
        public static double CelvolumeIjs => IjsA * IjsA * System.Math.Sqrt(3) * IjsC;

        /// <summary>Dichtheid van ijs Ih bij 0 °C volgens Binas (g/cm³). Het rooster geeft ≈ 0,919 (bij −10 °C).</summary>
        public const double DichtheidIjsBinas = 0.917;

        /// <summary>Dichtheid van ijs Ih, berekend uit het rooster (≈ 0,92 g/cm³).</summary>
        public static double DichtheidIjs =>
            MoleculenPerRechthoekigeCel * MolaireMassaWater / (Avogadro * CelvolumeIjs * 1e-24);

        /// <summary>Temperatuur (°C) waarbij vloeibaar water de grootste dichtheid heeft.</summary>
        public const double TemperatuurMaxDichtheid = 3.98;

        /// <summary>
        /// Dichtheid van vloeibaar water (g/cm³) tussen 0 en 100 °C, formule van Thiesen.
        /// Maximum bij 3,98 °C.
        /// </summary>
        public static double DichtheidWater(double t)
        {
            return 1.0 - (t - TemperatuurMaxDichtheid) * (t - TemperatuurMaxDichtheid) * (t + 283.0) / (503570.0 * (t + 67.26));
        }
    }
}
