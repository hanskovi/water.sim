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
            ["level.6.kort"] = ("koken", "boiling"),

            ["level.1.titel"] = ("Het watermolecuul", "The water molecule"),
            ["level.2.titel"] = ("De waterstofbrug", "The hydrogen bond"),
            ["level.3.titel"] = ("Het ijskristal", "The ice crystal"),
            ["level.4.titel"] = ("Smelten en bevriezen", "Melting and freezing"),
            ["level.5.titel"] = ("Waarom ijs drijft", "Why ice floats"),
            ["level.6.titel"] = ("Koken", "Boiling"),

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
            ["level.6.uitleg"] = ("Van water naar stoom: belletjes, verdampen en het plateau bij 100 °C.",
                                  "From water to steam: bubbles, evaporation and the plateau at 100 °C."),

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
            ["rho.max"] = ("max", "max"),
            ["omgeving"] = ("Omgeving", "Surroundings"),
            ["oneindig"] = ("Oneindig", "Infinite"),
            ["blok"] = ("Los blok", "Block"),
            ["kalotten"] = ("Kalotten", "Space-fill"),
            ["beweging"] = ("Beweging", "Motion"),
            ["model"] = ("Model, niet op schaal: smelten gaat veel sneller dan echt; trilling 2× vergroot en bij kou extra afgezwakt.",
                         "Model, not to scale: melting is much faster than in reality; vibration enlarged 2× and damped more in the cold."),
        };
    }
}
