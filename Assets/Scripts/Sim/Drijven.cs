namespace WaterSim.Sim
{
    /// <summary>
    /// Drijven volgens Archimedes: een voorwerp drijft als zijn dichtheid kleiner is dan die van de vloeistof.
    /// Het deel onder het oppervlak is dan ρ(voorwerp) / ρ(vloeistof).
    /// </summary>
    public static class Drijven
    {
        /// <summary>Drijft het voorwerp (dichtheden in g/cm³)?</summary>
        public static bool Drijft(double rhoVoorwerp, double rhoVloeistof) => rhoVoorwerp < rhoVloeistof;

        /// <summary>Deel van het voorwerp onder het oppervlak (0 … 1); 1 als het zinkt.</summary>
        public static double DeelOnder(double rhoVoorwerp, double rhoVloeistof) =>
            rhoVloeistof <= 0 ? 1 : System.Math.Min(1.0, rhoVoorwerp / rhoVloeistof);
    }
}
