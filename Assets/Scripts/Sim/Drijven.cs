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

        /// <summary>
        /// Hoeveel het vloeistofniveau stijgt als een deel <paramref name="gesmolten"/> (0 … 1) van een ijsblok smelt.
        /// Plat (per breedte): ijsoppervlak <paramref name="ijsOppervlak"/>, breedte van het glas <paramref name="glasBreedte"/>,
        /// uitkomst in dezelfde lengte-eenheid.
        /// - Drijvend (of gezonken) in de vloeistof: het ijs verplaatste al vloeistof; alleen het verschil telt.
        ///   In zoet water is dat precies nul.
        /// - Op een rots, boven de vloeistof (landijs): al het smeltwater komt erbij.
        /// Vereenvoudiging: smeltwater en vloeistof mengen zonder volumeverandering.
        /// </summary>
        public static double Niveaustijging(double ijsOppervlak, double glasBreedte, double rhoIjs, double rhoVloeistof,
                                           double rhoSmeltwater, bool inVloeistof, double gesmolten)
        {
            double smeltwater = ijsOppervlak * gesmolten * rhoIjs / rhoSmeltwater;
            double eerderVerplaatst = inVloeistof ? ijsOppervlak * gesmolten * DeelOnder(rhoIjs, rhoVloeistof) : 0;
            return (smeltwater - eerderVerplaatst) / glasBreedte;
        }
    }
}
