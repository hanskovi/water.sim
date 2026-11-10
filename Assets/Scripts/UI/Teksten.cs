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
            ["level.5.uitleg"] = ("IJs drijft in een glas, en een meer bevriest van boven: water van 4 °C zakt naar de bodem.",
                                  "Ice floats in a glass, and a lake freezes from the top: water at 4 °C sinks to the bottom."),
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
            ["glas"] = ("Glas", "Glass"),
            ["zon"] = ("Zon", "Sun"),
            ["jaar"] = ("Jaar", "Year"),
            ["glas.drijvend"] = ("Drijvend", "Floating"),
            ["glas.rots"] = ("Op een rots", "On a rock"),
            ["glas.smelt"] = ("Smelt", "Melt"),
            ["glas.status.rots"] = ("Het ijs ligt op een rots, boven het water: zo ligt landijs op Groenland en Antarctica. Laat het smelten.", "The ice lies on a rock, above the water: like land ice on Greenland and Antarctica. Let it melt."),
            ["glas.na.zoet"] = ("Gesmolten, en het water staat precies even hoog: drijvend ijs verplaatste al evenveel water als het smeltwater inneemt.", "Melted, and the water is exactly as high: floating ice already displaced as much water as the meltwater takes up."),
            ["glas.na.zee"] = ("In zeewater stijgt het niveau een heel klein beetje: het smeltwater is zoet en lichter dan zeewater.", "In sea water the level rises a tiny bit: the meltwater is fresh and lighter than sea water."),
            ["glas.na.rots"] = ("Het water stijgt flink: al het smeltwater komt erbij. Zo stijgt de zeespiegel als landijs smelt; drijvend zee-ijs doet dat (bijna) niet.", "The water rises a lot: all the meltwater is added. That is how sea level rises when land ice melts; floating sea ice (almost) does not."),
            ["glas.na.zinkt"] = ("Het gezonken ijs is gesmolten en het niveau staat iets lager: het smeltwater neemt minder ruimte in dan het ijs (vereenvoudigd, zonder mengen).", "The sunken ice has melted and the level is a little lower: the meltwater takes up less room than the ice (simplified, without mixing)."),
            ["meer.groeit"] = ("groeit aan de onderkant", "grows at the bottom"),
            ["meer.smelt"] = ("smelt aan de bovenkant", "melts at the top"),
            ["meer.status.jaar"] = ("{0} · bodem {1}", "{0} · bottom {1}"),
            ["schaats.0"] = ("Niet op het ijs!", "Stay off the ice!"),
            ["schaats.1"] = ("Eén persoon, voorzichtig", "One person, carefully"),
            ["schaats.2"] = ("Schaatsen kan", "Skating is OK"),
            ["schaats.3"] = ("Toertocht!", "Ice tour!"),
            ["maand.0"] = ("jan", "Jan"),
            ["maand.1"] = ("feb", "Feb"),
            ["maand.2"] = ("mrt", "Mar"),
            ["maand.3"] = ("apr", "Apr"),
            ["maand.4"] = ("mei", "May"),
            ["maand.5"] = ("jun", "Jun"),
            ["maand.6"] = ("jul", "Jul"),
            ["maand.7"] = ("aug", "Aug"),
            ["maand.8"] = ("sep", "Sep"),
            ["maand.9"] = ("okt", "Oct"),
            ["maand.10"] = ("nov", "Nov"),
            ["maand.11"] = ("dec", "Dec"),
            ["meer"] = ("Meer", "Lake"),
            ["vloeistof.0"] = ("Zoet water", "Fresh water"),
            ["vloeistof.1"] = ("Zeewater", "Sea water"),
            ["vloeistof.2"] = ("Alcohol", "Alcohol"),
            ["lucht"] = ("Lucht", "Air"),
            ["opnieuw"] = ("Opnieuw", "Restart"),
            ["inzoom.ijs"] = ("ijs", "ice"),
            ["inzoom.water"] = ("water", "water"),
            ["glas.boven"] = ("{0}% boven", "{0}% above"),
            ["glas.onder"] = ("{0}% onder", "{0}% below"),
            ["glas.zinkt"] = ("zinkt!", "sinks!"),
            ["glas.status.drijft"] = ("Het ijsblokje drijft: het grootste deel zit onder water.", "The ice cube floats: most of it is under water."),
            ["glas.status.zee"] = ("In zout water drijft ijs net iets hoger: zeewater is zwaarder dan zoet water.", "In salt water ice floats a little higher: sea water is denser than fresh water."),
            ["glas.status.zinkt"] = ("In alcohol zinkt ijs: alcohol is lichter dan ijs.", "In alcohol ice sinks: alcohol is less dense than ice."),
            ["meer.ijs"] = ("ijs {0} cm", "ice {0} cm"),
            ["meer.status"] = ("Dag {0} · bodem {1}", "Day {0} · bottom {1}"),
            ["level.5.zie.glas"] = ("Links zie je hoe het ijs er van dichtbij uitziet, rechts het water. In ijs zitten de moleculen in open zeshoeken; in water zitten ze dichter op elkaar. Daarom drijft ijs. Kies een andere vloeistof en kijk wat er gebeurt.",
                                    "On the left you see ice up close, on the right water. In ice the molecules sit in open hexagons; in water they are closer together. That is why ice floats. Choose another liquid and see what happens."),
            ["level.5.zie.meer"] = ("Een meer koelt af in de winter. Eerst wordt al het water kouder. Bij 4 °C is water het zwaarst en zakt het naar de bodem. Kouder water blijft bovenop liggen en bevriest. Onder het ijs kunnen de vissen blijven leven. Zet Jaar aan en kijk een heel jaar mee.",
                                    "A lake cools down in winter. First all the water gets colder. At 4 °C water is heaviest and sinks to the bottom. Colder water stays on top and freezes. Under the ice the fish can stay alive. Turn on Year and watch a whole year."),
            ["model.glas"] = ("Model, niet op schaal: dichtheden uit Binas (ijs 0,917; zeewater 1,025; alcohol 0,789 g/cm³).",
                              "Model, not to scale: densities from Binas (ice 0.917; sea water 1.025; alcohol 0.789 g/cm³)."),
            ["model.meer"] = ("Model: meer van 6 m diep; 1 s = 1 dag (jaar: 3 dagen); geen sneeuw of grondwater; wind en zon vereenvoudigd. Schaatsmeter is een richtlijn, geen garantie.",
                              "Model: lake 6 m deep; 1 s = 1 day (year: 3 days); no snow or groundwater; wind and sun simplified. The skating meter is a guideline, not a guarantee."),
            ["level.1.zie"] = ("Je ziet één watermolecuul: een zuurstofatoom (rood) met twee waterstofatomen (wit). Het molecuul is geknikt. De kant van de zuurstof is een beetje negatief (δ−), de kant van de waterstof een beetje positief (δ+).",
                       "You see one water molecule: an oxygen atom (red) with two hydrogen atoms (white). The molecule is bent. The oxygen side is slightly negative (δ−), the hydrogen side slightly positive (δ+)."),
            ["level.2.zie"] = ("Je ziet twee watermoleculen. Het waterstofatoom (δ+) van het ene molecuul trekt aan de zuurstof (δ−) van het andere. Die aantrekking is de waterstofbrug: de blauwe stippellijn. Hij is veel zwakker dan de binding binnen een molecuul.",
                       "You see two water molecules. The hydrogen atom (δ+) of one molecule is attracted to the oxygen (δ−) of the other. That attraction is the hydrogen bond: the blue dotted line. It is much weaker than the bond inside a molecule."),
            ["level.3.zie"] = ("Je vliegt door ijs. Elk molecuul zit met vier waterstofbruggen vast aan vier buren. Samen vormen ze zeshoeken, als een honingraat, met lege kanalen ertussen. Daardoor neemt ijs meer ruimte in dan water.",
                       "You are flying through ice. Each molecule is held by four neighbours through four hydrogen bonds. Together they form hexagons, like a honeycomb, with empty channels in between. That is why ice takes up more space than water."),
            ["info.titel"] = ("Meer weten",
                       "Learn more"),
            ["info.1.0"] = ("Alles om je heen is gemaakt van atomen. Een watermolecuul bestaat uit drie atomen: één zuurstofatoom (O, rood) en twee waterstofatomen (H, wit). Daarom heet water <b>H<sub>2</sub>O</b>.\n\nEen molecuul is ontzettend klein. In één druppel water zitten ongeveer 1 700 000 000 000 000 000 000 moleculen. Het model laat ze enorm vergroot zien.\n\nZuurstof trekt harder aan de elektronen die de atomen delen. Daardoor is de zuurstofkant een beetje negatief (δ−) en zijn de waterstofatomen een beetje positief (δ+).",
                       "Everything around you is made of atoms. A water molecule consists of three atoms: one oxygen atom (O, red) and two hydrogen atoms (H, white). That is why water is called <b>H<sub>2</sub>O</b>.\n\nA molecule is extremely small. One drop of water contains about 1 700 000 000 000 000 000 000 molecules. The model shows them hugely enlarged.\n\nOxygen pulls harder on the electrons the atoms share. So the oxygen side is slightly negative (δ−) and the hydrogen atoms are slightly positive (δ+)."),
            ["info.1.1"] = ("De O–H-binding is een <b>atoombinding</b>: de atomen delen een elektronenpaar. Zuurstof is elektronegatiever dan waterstof en trekt de gedeelde elektronen naar zich toe: O krijgt een kleine negatieve lading (δ−), de H-atomen een kleine positieve (δ+).\n\nO heeft ook twee <b>vrije elektronenparen</b>. Die duwen de bindingen naar elkaar toe, zodat de hoek H–O–H {3}° is in plaats van 180°. Door die knik vallen de ladingen niet tegen elkaar weg: water is een <b>dipool</b>.\n\nEen O–H-binding is {0} Å lang. <b>1 Å (ångström) = 10<sup>−10</sup> m = 0,1 nm</b>, dus {0} Å = {11} nm: ongeveer een tienmiljoenste millimeter.",
                       "The O–H bond is a <b>covalent bond</b>: the atoms share an electron pair. Oxygen is more electronegative than hydrogen and pulls the shared electrons towards itself: O gets a small negative charge (δ−), the H atoms a small positive one (δ+).\n\nO also has two <b>lone pairs</b>. They push the bonds towards each other, so the H–O–H angle is {3}° instead of 180°. Because of this bend the charges do not cancel out: water is a <b>dipole</b>.\n\nAn O–H bond is {0} Å long. <b>1 Å (ångström) = 10<sup>−10</sup> m = 0.1 nm</b>, so {0} Å = {11} nm: about a ten-millionth of a millimetre."),
            ["info.1.2"] = ("Volgens het VSEPR-model zijn de vier elektronenparen rond O tetraëdrisch verdeeld (ideaal {4}°). Vrije paren nemen meer ruimte in dan bindende paren; daardoor wordt de H–O–H-hoek kleiner: {3}°.\n\nHet <b>dipoolmoment</b> van water is 1,85 D (6,2·10<sup>−30</sup> C·m). Een H<sub>2</sub>O-molecuul heeft 3N − 6 = 3 <b>trillingswijzen</b>: symmetrische strek, asymmetrische strek en buiging. Die zie je terug in het IR-spectrum.\n\n<i>Model:</i> atomen zijn hier bollen. In werkelijkheid is een molecuul een elektronenwolk zonder scherpe rand.",
                       "According to the VSEPR model the four electron pairs around O are arranged tetrahedrally (ideal {4}°). Lone pairs take up more space than bonding pairs, which makes the H–O–H angle smaller: {3}°.\n\nThe <b>dipole moment</b> of water is 1.85 D (6.2·10<sup>−30</sup> C·m). An H<sub>2</sub>O molecule has 3N − 6 = 3 <b>vibrational modes</b>: symmetric stretch, asymmetric stretch and bend. You see them in the IR spectrum.\n\n<i>Model:</i> atoms are spheres here. In reality a molecule is an electron cloud without a sharp edge."),
            ["info.2.0"] = ("Watermoleculen trekken elkaar aan. De positieve kant (H) van het ene molecuul en de negatieve kant (O) van het andere trekken naar elkaar toe. Die aantrekking heet een <b>waterstofbrug</b>.\n\nEen waterstofbrug is veel zwakker dan de binding binnen een molecuul. Maar er zijn er ontzettend veel, en samen houden ze water stevig bij elkaar. Daardoor kookt water pas bij 100 °C, terwijl een watermolecuul heel licht is.",
                       "Water molecules attract each other. The positive side (H) of one molecule and the negative side (O) of another pull towards each other. That attraction is called a <b>hydrogen bond</b>.\n\nA hydrogen bond is much weaker than the bond inside a molecule. But there are enormous numbers of them, and together they hold water firmly together. That is why water only boils at 100 °C, although a water molecule is very light."),
            ["info.2.1"] = ("Een waterstofbrug ontstaat tussen een H die vastzit aan een sterk elektronegatief atoom (O, N of F) en een <b>vrij elektronenpaar</b> van een ander O-, N- of F-atoom.\n\nDe afstand H···O is {2} Å, bijna twee keer zo lang als de O–H-binding ({0} Å). De twee O-atomen liggen {1} Å uit elkaar (1 Å = 10<sup>−10</sup> m).\n\nOm een H-brug te verbreken is ongeveer 20 kJ/mol nodig, voor een O–H-binding 463 kJ/mol: ruim 20 keer zoveel. Vergelijk H<sub>2</sub>S: dat vormt nauwelijks H-bruggen en kookt al bij −60 °C.",
                       "A hydrogen bond forms between an H attached to a strongly electronegative atom (O, N or F) and a <b>lone pair</b> on another O, N or F atom.\n\nThe H···O distance is {2} Å, almost twice as long as the O–H bond ({0} Å). The two O atoms are {1} Å apart (1 Å = 10<sup>−10</sup> m).\n\nBreaking a hydrogen bond takes about 20 kJ/mol, an O–H bond 463 kJ/mol: more than 20 times as much. Compare H<sub>2</sub>S: it hardly forms hydrogen bonds and boils at −60 °C."),
            ["info.2.2"] = ("O–H···O staat bijna recht (≈ 180°): de aantrekking tussen de δ+ H en het vrije elektronenpaar is dan het sterkst. Een H-brug is grotendeels elektrostatisch, met een kleine covalente bijdrage (ladingsoverdracht).\n\nIn vloeibaar water leeft een H-brug maar ongeveer een <b>picoseconde</b> (10<sup>−12</sup> s): bruggen breken en vormen zich voortdurend.\n\nSchatting uit de smeltwarmte: in ijs heeft elk molecuul 4 H-bruggen, maar elke brug hoort bij 2 moleculen, dus 1 mol ijs bevat 2 mol bruggen. Bij smelten breekt ongeveer een achtste daarvan (van 4 naar ≈ 3,5 per molecuul): 2 × 1/8 = 0,25 mol bruggen per mol water. De smeltwarmte is {7} J/g × {12} g/mol ≈ {8} kJ/mol, dus {8} kJ/mol ÷ 0,25 ≈ 24 kJ/mol per brug. In het IR-spectrum schuift de O–H-strekband door H-bruggen naar lagere golfgetallen.",
                       "O–H···O is almost straight (≈ 180°): the attraction between the δ+ H and the lone pair is strongest then. A hydrogen bond is mostly electrostatic, with a small covalent contribution (charge transfer).\n\nIn liquid water a hydrogen bond lives only about a <b>picosecond</b> (10<sup>−12</sup> s): bonds break and form all the time.\n\nEstimate from the heat of fusion: in ice each molecule has 4 hydrogen bonds, but each bond belongs to 2 molecules, so 1 mol of ice contains 2 mol of bonds. On melting about one eighth of them break (from 4 to ≈ 3.5 per molecule): 2 × 1/8 = 0.25 mol of bonds per mol of water. The heat of fusion is {7} J/g × {12} g/mol ≈ {8} kJ/mol, so {8} kJ/mol ÷ 0.25 ≈ 24 kJ/mol per bond. In the IR spectrum hydrogen bonding shifts the O–H stretch band to lower wavenumbers."),
            ["info.3.0"] = ("In ijs zitten de watermoleculen op vaste plekken. Elk molecuul houdt vier buren vast met waterstofbruggen. Zo ontstaan zeshoeken, net als in een honingraat, met lege kanalen ertussen.\n\nOmdat er zoveel lege ruimte is, neemt ijs meer plaats in dan water. Daarom kan een volle fles water in de vriezer knappen, en drijft ijs.\n\nDe moleculen staan niet stil: ze trillen op hun plek.",
                       "In ice the water molecules sit in fixed places. Each molecule holds four neighbours with hydrogen bonds. This makes hexagons, just like a honeycomb, with empty channels in between.\n\nBecause there is so much empty space, ice takes up more room than water. That is why a full bottle of water can burst in the freezer, and why ice floats.\n\nThe molecules are not standing still: they vibrate in place."),
            ["info.3.1"] = ("Gewoon ijs heet <b>ijs Ih</b> (h van hexagonaal). Elk O-atoom heeft vier buren in de vorm van een <b>tetraëder</b> (hoek ≈ {4}°), op {1} Å afstand (1 Å = 10<sup>−10</sup> m).\n\nVan de vier H-bruggen rond een molecuul geeft het er twee (met zijn eigen H-atomen) en ontvangt het er twee (op zijn vrije elektronenparen).\n\nDoor de open honingraat is de dichtheid van ijs {5} g/cm³, kleiner dan die van water (≈ 1,000 g/cm³). Daarom drijft ijs.",
                       "Ordinary ice is called <b>ice Ih</b> (h for hexagonal). Each O atom has four neighbours arranged as a <b>tetrahedron</b> (angle ≈ {4}°), {1} Å away (1 Å = 10<sup>−10</sup> m).\n\nOf the four hydrogen bonds around a molecule it donates two (with its own H atoms) and accepts two (on its lone pairs).\n\nBecause of the open honeycomb the density of ice is {5} g/cm³, lower than that of water (≈ 1.000 g/cm³). That is why ice floats."),
            ["info.3.2"] = ("De <b>ijsregels</b> (Bernal-Fowler): elke O heeft precies 2 H dichtbij, en op elke O···O-lijn zit precies één H. De H-atomen kunnen op heel veel manieren zo verdeeld zijn; Pauling schatte ongeveer 1,5 mogelijkheden per molecuul. Daardoor houdt ijs ook bij 0 K entropie over: S = R ln 1,5 ≈ 3,4 J/(mol·K), wat klopt met metingen.\n\nEen hexagonale eenheidscel van ijs Ih (a = {14} Å, c = {15} Å) bevat 4 moleculen.\n\nOnder hoge druk ontstaan andere ijssoorten, zoals ijs VII. Die zijn wél dichter dan water.",
                       "The <b>ice rules</b> (Bernal-Fowler): each O has exactly 2 H close by, and each O···O line carries exactly one H. The H atoms can be arranged like this in very many ways; Pauling estimated about 1.5 possibilities per molecule. So ice keeps entropy even at 0 K: S = R ln 1.5 ≈ 3.4 J/(mol·K), in agreement with measurements.\n\nA hexagonal unit cell of ice Ih (a = {14} Å, c = {15} Å) contains 4 molecules.\n\nUnder high pressure other kinds of ice form, such as ice VII. Those are denser than water."),
            ["info.4.0"] = ("Als je ijs verwarmt, gaan de moleculen harder trillen tot ze loskomen: het ijs <b>smelt</b>. Zolang er nog ijs is, blijft de temperatuur 0 °C. Alle warmte gaat dan naar het losmaken van de moleculen, niet naar het warmer maken.\n\nPas als alles gesmolten is, stijgt de temperatuur weer. Dat zie je in de grafiek als een vlak stuk: het <b>smeltplateau</b>.\n\nBij <b>stollen</b> gebeurt het omgekeerde: de moleculen komen weer vast te zitten en er komt warmte vrij.",
                       "When you heat ice, the molecules vibrate harder until they break loose: the ice <b>melts</b>. As long as there is ice left, the temperature stays at 0 °C. All the heat goes into freeing the molecules, not into warming up.\n\nOnly when everything has melted does the temperature rise again. In the graph you see that as a flat part: the <b>melting plateau</b>.\n\nWhen water <b>freezes</b> the opposite happens: the molecules get fixed again and heat is released."),
            ["info.4.1"] = ("<b>Smeltwarmte</b>: om 1 g ijs van 0 °C te smelten is {7} J nodig (Binas). Met dezelfde warmte verwarm je 1 g water ongeveer 80 °C. De soortelijke warmte van ijs is {10} J/(g·K), van water {9} J/(g·K).\n\nPer mol: {7} J/g × {12} g/mol (molaire massa) ≈ {8} kJ/mol. Smelten: ΔH = +{8} kJ/mol (endotherm), stollen: ΔH = −{8} kJ/mol (exotherm).\n\nIn water van ongeveer 1 °C heeft een molecuul gemiddeld nog ongeveer 3,5 H-bruggen, in ijs 4. Bij smelten breekt dus maar een klein deel van de bruggen.",
                       "<b>Heat of fusion</b>: melting 1 g of ice at 0 °C takes {7} J (Binas). With the same heat you warm 1 g of water by about 80 °C. The specific heat of ice is {10} J/(g·K), of water {9} J/(g·K).\n\nPer mole: {7} J/g × {12} g/mol (molar mass) ≈ {8} kJ/mol. Melting: ΔH = +{8} kJ/mol (endothermic), freezing: ΔH = −{8} kJ/mol (exothermic).\n\nIn water at about 1 °C a molecule still has about 3.5 hydrogen bonds on average, in ice 4. So on melting only a small part of the bonds breaks."),
            ["info.4.2"] = ("Bij 0 °C en 1 bar staan ijs en water in evenwicht: ΔG = ΔH − TΔS = 0, dus ΔS = ΔH/T ≈ 22 J/(mol·K). Onder 0 °C is ΔG van smelten positief: ijs smelt dan niet vanzelf.\n\nDe smeltlijn in het fasediagram helt naar links (Clausius-Clapeyron), omdat ijs een groter volume heeft dan water: ongeveer 135 bar extra druk verlaagt het smeltpunt 1 K.\n\nZuiver water kan <b>onderkoeld</b> raken tot ongeveer −40 °C: ijs moet eerst een kiem vormen, en dat is een kansproces.\n\n<i>Model:</i> smelten gaat hier veel sneller dan in werkelijkheid.",
                       "At 0 °C and 1 bar ice and water are in equilibrium: ΔG = ΔH − TΔS = 0, so ΔS = ΔH/T ≈ 22 J/(mol·K). Below 0 °C ΔG of melting is positive: ice does not melt by itself.\n\nThe melting line in the phase diagram leans to the left (Clausius-Clapeyron), because ice has a larger volume than water: about 135 bar of extra pressure lowers the melting point by 1 K.\n\nPure water can be <b>supercooled</b> to about −40 °C: ice first has to form a nucleus, and that is a matter of chance.\n\n<i>Model:</i> melting is much faster here than in reality."),
            ["info.5.0"] = ("<b>Dichtheid</b> zegt hoe zwaar iets is voor zijn grootte. IJs is lichter dan hetzelfde volume water, daarom drijft ijs.\n\nIn het glas zie je het: het ijsblokje drijft, en bijna alles zit onder water. In het meer zie je dat water van 4 °C naar de bodem zakt.\n\nDat is bijzonder: bij bijna alle stoffen is de vaste stof juist zwaarder dan de vloeistof.",
                       "<b>Density</b> tells you how heavy something is for its size. Ice is lighter than the same volume of water, so ice floats.\n\nYou can see it in the glass: the ice cube floats, with almost all of it under water. In the lake you see that water at 4 °C sinks to the bottom.\n\nThat is special: for almost all substances the solid is heavier than the liquid."),
            ["info.5.1"] = ("ρ = m / V. IJs: {5} g/cm³, water bij 0 °C ≈ 1,000 g/cm³. Water zet bij bevriezen dus ongeveer 9% uit. Van een ijsblok in zoet water zit ongeveer 92% onder water.\n\nWater heeft zijn grootste dichtheid bij {13} °C. Tussen 0 en 4 °C krimpt water nog als je het verwarmt, omdat ijsachtige stukjes netwerk afbreken. Daarom is het water op de bodem van een meer in de winter ongeveer 4 °C, en vriest een meer van boven dicht.\n\nIn de zomer ligt warm, licht water als een deken bovenop: de <b>spronglaag</b>. In het voorjaar en de herfst is het meer overal even zwaar en mengt het helemaal (omkering).\n\nSmelt drijvend ijs, dan blijft het waterniveau gelijk. Smelt <b>landijs</b> (Groenland, Antarctica), dan stijgt de zeespiegel.",
                       "ρ = m / V. Ice: {5} g/cm³, water at 0 °C ≈ 1.000 g/cm³. So water expands by about 9% on freezing. About 92% of an ice block in fresh water is under water.\n\nWater has its highest density at {13} °C. Between 0 and 4 °C water still shrinks when you heat it, because ice-like bits of network break down. That is why the water at the bottom of a lake in winter is about 4 °C, and a lake freezes from the top.\n\nIn summer warm, light water lies on top like a blanket: the <b>thermocline</b>. In spring and autumn the lake has the same density everywhere and mixes completely (turnover).\n\nWhen floating ice melts, the water level stays the same. When <b>land ice</b> melts (Greenland, Antarctica), sea level rises."),
            ["info.5.2"] = ("Volumeverandering bij smelten, per mol: 18,0/1,000 − 18,0/{5} ≈ −1,6 cm³.\n\nHet dichtheidsmaximum ontstaat door twee effecten die tegen elkaar in werken: de warmtebeweging zet de vloeistof uit, het afbreken van het open H-brugnetwerk laat haar krimpen. Bij {13} °C heffen ze elkaar precies op.\n\nOpgelost zout verschuift het maximum: in zeewater (3,5% zout) ligt het onder het vriespunt.\n\n<i>Model:</i> de dichtheid van water volgt de formule van Thiesen.",
                       "Volume change on melting, per mole: 18.0/1.000 − 18.0/{5} ≈ −1.6 cm³.\n\nThe density maximum comes from two opposing effects: thermal motion makes the liquid expand, the breakdown of the open hydrogen-bond network makes it shrink. At {13} °C they cancel exactly.\n\nDissolved salt shifts the maximum: in sea water (3.5% salt) it lies below the freezing point.\n\n<i>Model:</i> the density of water follows the Thiesen formula."),
            ["formule"] = ("Formule", "Formula"),
            ["kanaal"] = ("kanaal", "channel"),
            ["formule.4.onder"] = ("{0} J/g · bij 0 °C", "{0} J/g · at 0 °C"),
            ["formule.5"] = ("ρ<sub>ijs</sub> {0} <noparse><</noparse> ρ<sub>water</sub> {1}", "ρ<sub>ice</sub> {0} <noparse><</noparse> ρ<sub>water</sub> {1}"),
            ["formule.5.onder"] = ("g/cm³ · ijs drijft", "g/cm³ · ice floats"),
            ["model.ijs"] = ("Model: ijs Ih bij −10 °C; trilling op ware grootte (≈ 0,15 Å), maar sterk vertraagd (echt: biljoenen keren per seconde).",
                             "Model: ice Ih at −10 °C; vibration to scale (≈ 0.15 Å), but greatly slowed down (really: trillions of times per second)."),
            ["model.molecuul"] = ("Model: afstanden en hoeken op schaal; bij bol-staaf zijn de atomen verkleind, de trilling is vergroot.",
                                  "Model: distances and angles to scale; in ball-and-stick the atoms are shrunk, the vibration is enlarged."),
            ["model"] = ("Model, niet op schaal: smelten gaat veel sneller dan echt (ook de resterende tijd is modeltijd); trilling 2× vergroot en bij kou extra afgezwakt.",
                         "Model, not to scale: melting is much faster than in reality (the time left is model time too); vibration enlarged 2× and damped more in the cold."),
        };
    }
}
