using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaterSim.UI
{
    public enum Taal { NL = 0, EN = 1 }

    /// <summary>
    /// Alle zichtbare teksten van de app, per taal. Nooit tekst hard in de code: altijd via <see cref="T"/>.
    /// Eerst Nederlands en Engels; Duits, Frans en Spaans kunnen later als extra kolom worden toegevoegd.
    /// </summary>
    public static class Teksten
    {
        const string Bewaarsleutel = "watersim.taal";
        static Taal huidig = (Taal)PlayerPrefs.GetInt(Bewaarsleutel, (int)Taal.NL);

        public static Taal Huidig => huidig;
        public static event Action Gewijzigd;

        public static void Stel(Taal taal)
        {
            if (taal == huidig) return;
            huidig = taal;
            PlayerPrefs.SetInt(Bewaarsleutel, (int)taal);
            Gewijzigd?.Invoke();
        }

        public static string T(string sleutel)
        {
            if (!Lijst.TryGetValue(sleutel, out var v)) return sleutel;
            return huidig == Taal.EN ? v.en : v.nl;
        }

        /// <summary>Decimale komma in het Nederlands, punt in het Engels.</summary>
        public static string Getal(double v, string formaat)
        {
            string s = v.ToString(formaat, System.Globalization.CultureInfo.InvariantCulture);
            return huidig == Taal.NL ? s.Replace('.', ',') : s;
        }

        /// <summary>Temperatuur met echt minteken (−) en eventueel een plusteken.</summary>
        public static string Graden(double t, int decimalen, bool plus = false)
        {
            string s = Getal(Math.Abs(t), decimalen == 0 ? "0" : "0.0");
            string teken = t < -0.05 ? "−" : (plus && t > 0.05 ? "+" : "");
            return teken + s + " °C";
        }

        static readonly Dictionary<string, (string nl, string en)> Lijst = new Dictionary<string, (string, string)>
        {
            // algemeen
            ["start"] = ("Start", "Start"),
            ["binnenkort"] = ("binnenkort", "coming soon"),
            ["taal"] = ("NL", "EN"),

            // leagues
            ["league.0"] = ("Onderbouw", "Junior"),
            ["league.1"] = ("Bovenbouw", "Senior"),
            ["league.2"] = ("XPRT League", "XPRT League"),

            // levels: kort (op de tegel), titel en uitleg
            ["level.1.kort"] = ("molecuul", "molecule"),
            ["level.2.kort"] = ("H-brug", "H-bond"),
            ["level.3.kort"] = ("ijskristal", "ice crystal"),
            ["level.4.kort"] = ("smelten", "melting"),
            ["level.5.kort"] = ("dichtheid", "density"),
            ["level.6.kort"] = ("ontdek", "discover"),

            ["level.1.titel"] = ("Het watermolecuul", "The water molecule"),
            ["level.2.titel"] = ("De waterstofbrug", "The hydrogen bond"),
            ["level.3.titel"] = ("Het ijskristal", "The ice crystal"),
            ["level.4.titel"] = ("Smelten en stollen", "Melting and freezing"),
            ["level.5.titel"] = ("Waarom ijs drijft", "Why ice floats"),
            ["level.6.titel"] = ("Ontdek", "Discover"),

            ["level.1.uitleg"] = ("Eén watermolecuul: de hoek, de lading δ+ en δ−, en waarom water een dipool is.",
                                  "One water molecule: its angle, the δ+ and δ− charges, and why water is a dipole."),
            ["level.2.uitleg"] = ("Hoe twee watermoleculen elkaar aantrekken via een waterstofbrug.",
                                  "How two water molecules attract each other through a hydrogen bond."),
            ["level.3.uitleg"] = ("Vlieg door ijs. Elk molecuul zit vast aan 4 buren in een honingraat met lege kanalen.",
                                  "Fly through ice. Each molecule is held by 4 neighbours in a honeycomb with empty channels."),
            ["level.4.uitleg"] = ("Warm het ijs op. Zolang er ijs is, blijft het 0 °C: de warmte breekt H-bruggen.",
                                  "Heat the ice. While there is ice left, it stays at 0 °C: the heat breaks hydrogen bonds."),
            ["level.5.uitleg"] = ("Een los blok ijs smelt en krimpt: water is dichter dan ijs, dus ijs drijft.",
                                  "A block of ice melts and shrinks: water is denser than ice, so ice floats."),
            ["level.6.uitleg"] = ("Buiten de quiz: VR, het Mpemba-effect, het fasediagram en andere soorten ijs.",
                                  "Outside the quiz: VR, the Mpemba effect, the phase diagram and other kinds of ice."),

            // simulatiescherm
            ["fase.ijs"] = ("ijs", "ice"),
            ["fase.water"] = ("water", "water"),
            ["fase.smelten"] = ("smelten", "melting"),
            ["fase.bevriezen"] = ("bevriezen", "freezing"),
            ["fase.mengsel"] = ("ijs + water", "ice + water"),
            ["gesmolten"] = ("gesmolten", "melted"),
            ["hbruggen"] = ("H-bruggen", "H-bonds"),
            ["per.molecuul"] = ("per molecuul", "per molecule"),
            ["dichtheid"] = ("dichtheid", "density"),
            ["rho.max"] = ("grootste dichtheid", "highest density"),
            ["fase.IJs"] = ("IJs", "Ice"),
            ["fase.Water"] = ("Water", "Water"),
            ["fase.mengsel.stol"] = ("water + ijs", "water + ice"),
            ["plateau.smelt"] = ("smeltplateau", "melting plateau"),
            ["plateau.stol"] = ("stolplateau", "freezing plateau"),
            ["evenwicht"] = ("evenwicht", "equilibrium"),
            ["ring.ijs"] = ("{0}% ijs", "{0}% ice"),
            ["regel.smelten"] = ("Smelten", "Melting"),
            ["regel.stollen"] = ("Stollen", "Freezing"),
            ["regel.evenwicht"] = ("Smelten en stollen heffen elkaar op", "Melting and freezing cancel out"),
            ["regel.warmer"] = ("Omgeving warmer: T stijgt", "Surroundings warmer: T rises"),
            ["regel.kouder"] = ("Omgeving kouder: T daalt", "Surroundings colder: T falls"),
            ["regel.gelijk"] = ("Even warm als de omgeving", "Same temperature as surroundings"),
            ["nog"] = ("nog ± {0}", "about {0} left"),
            ["omgeving"] = ("Omgeving", "Surroundings"),
            ["oneindig"] = ("Oneindig", "Infinite"),
            ["blok"] = ("Los blok", "Block"),
            ["kalotten"] = ("Kalotten", "Space-fill"),
            ["beweging"] = ("Beweging", "Motion"),
            ["ladingen"] = ("δ+ δ−", "δ+ δ−"),
            ["kanalen"] = ("Kanalen", "Channels"),
            ["level.3.feiten"] = ("Elk molecuul zit via 4 H-bruggen vast aan 4 buren (tetraëder, {1}°) · O–O <b>{0} Å</b> · ρ <b>{2} g/cm³</b>\nZes moleculen vormen een zeshoek; samen een honingraat met lege kanalen. Daarom is ijs minder dicht dan water.",
                                  "Each molecule is held by 4 neighbours through 4 H-bonds (tetrahedron, {1}°) · O–O <b>{0} Å</b> · ρ <b>{2} g/cm³</b>\nSix molecules form a hexagon; together a honeycomb with empty channels. That is why ice is less dense than water."),
            ["model.ijs"] = ("Model, niet op schaal: ijs Ih bij −10 °C; trilling 2× vergroot.",
                             "Model, not to scale: ice Ih at −10 °C; vibration enlarged 2×."),
            ["level.1.feiten"] = ("Hoek H–O–H <b>{0}°</b> · O–H <b>{1} Å</b>\nO trekt harder aan de gedeelde elektronen dan H: O wordt δ−, de H-atomen δ+. Door de gebogen vorm is water een dipool.",
                                  "H–O–H angle <b>{0}°</b> · O–H <b>{1} Å</b>\nO pulls harder on the shared electrons than H: O becomes δ−, the H atoms δ+. Because the molecule is bent, water is a dipole."),
            ["level.2.feiten"] = ("H···O <b>{0} Å</b> · O–O <b>{1} Å</b> · O–H <b>{2} Å</b>\nDe δ+ H van het ene molecuul trekt aan een vrij elektronenpaar van de δ− O van het andere. Een waterstofbrug is ongeveer 20× zwakker dan een O–H-binding.",
                                  "H···O <b>{0} Å</b> · O–O <b>{1} Å</b> · O–H <b>{2} Å</b>\nThe δ+ H of one molecule is attracted to a lone pair on the δ− O of the other. A hydrogen bond is about 20× weaker than an O–H bond."),
            ["model.molecuul"] = ("Model: afstanden en hoeken op schaal; bij bol-staaf zijn de atomen verkleind, de trilling is vergroot.",
                                  "Model: distances and angles to scale; in ball-and-stick the atoms are shrunk, the vibration is enlarged."),
            ["model"] = ("Model, niet op schaal: smelten gaat veel sneller dan echt (ook de resterende tijd is modeltijd); trilling 2× vergroot en bij kou extra afgezwakt.",
                         "Model, not to scale: melting is much faster than in reality (the time left is model time too); vibration enlarged 2× and damped more in the cold."),
        };
    }
}
