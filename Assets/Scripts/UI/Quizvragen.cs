using System.Collections.Generic;

namespace WaterSim.UI
{
    /// <summary>Eén quizvraag: meerkeuze (het eerste antwoord is goed) of een getal met een marge.</summary>
    public class Quizvraag
    {
        public string VraagNL, VraagEN;
        public (string nl, string en)[] Opties;          // null bij een getalvraag; Opties[0] is het goede antwoord
        public double Antwoord, Marge;
        public string Eenheid = "";
        /// <summary>Het antwoord staat in eenheden van 10^Macht (de leerling mag ook "6,022e23" typen).</summary>
        public int Macht;
        /// <summary>Minimaal aantal cijfers achter de komma in het antwoord.</summary>
        public int MinDecimalen;
        /// <summary>Hoe het goede antwoord wordt getoond (leeg: het getal zelf).</summary>
        public string Weergave = "";

        public bool IsGetal => Opties == null;
        public string Vraag => Teksten.Kies(VraagNL, VraagEN);
        public string Optie(int i) => Teksten.Kies(Opties[i].nl, Opties[i].en);
    }

    /// <summary>
    /// De quizvragen per level en niveau (0 = onderbouw, 1 = bovenbouw, 2 = XPRT), in NL en EN.
    /// Bron: Docs/Quiz_opgaven.md; getallen nagerekend met Binas en Constanten.cs.
    /// </summary>
    public static class Quizvragen
    {
        static Quizvraag MK(string nl, string en, params (string, string)[] opties) => new Quizvraag { VraagNL = nl, VraagEN = en, Opties = opties };
        static Quizvraag GT(string nl, string en, double antwoord, double marge, string eenheid = "", int macht = 0, int minDecimalen = 0, string weergave = "") =>
            new Quizvraag { VraagNL = nl, VraagEN = en, Antwoord = antwoord, Marge = marge, Eenheid = eenheid, Macht = macht, MinDecimalen = minDecimalen, Weergave = weergave };

        /// <summary>De vragen van dit level (1 of 2) en niveau, of een lege lijst.</summary>
        public static IReadOnlyList<Quizvraag> Voor(int level, int niveau)
        {
            if (level == 1) return niveau == 0 ? L1O : niveau == 1 ? L1B : L1X;
            if (level == 2) return niveau == 0 ? L2O : niveau == 1 ? L2B : L2X;
            return new Quizvraag[0];
        }

        public static bool Heeft(int level) => level == 1 || level == 2;

        // ---------- level 1 · het watermolecuul ----------
        static readonly Quizvraag[] L1O =
        {
            MK("Uit welke atomen bestaat een watermolecuul?", "Which atoms make up a water molecule?",
               ("1 O en 2 H", "1 O and 2 H"), ("2 O en 1 H", "2 O and 1 H"), ("1 O en 1 H", "1 O and 1 H"), ("2 O en 2 H", "2 O and 2 H")),
            MK("Welke kleur heeft zuurstof in het model?", "What colour is oxygen in the model?",
               ("rood", "red"), ("wit", "white"), ("blauw", "blue"), ("zwart", "black")),
            MK("Is een watermolecuul recht of geknikt?", "Is a water molecule straight or bent?",
               ("geknikt", "bent"), ("recht", "straight"), ("ringvormig", "ring-shaped"), ("plat vierkant", "flat square")),
            MK("Wat is H₂O in gewone woorden?", "What is H₂O in everyday words?",
               ("water", "water"), ("waterstof", "hydrogen"), ("zuurstof", "oxygen"), ("zout", "salt")),
            MK("Wat gebeurt er met de moleculen zelf als ijs smelt?", "What happens to the molecules themselves when ice melts?",
               ("niets, ze blijven H₂O", "nothing, they stay H₂O"), ("ze worden groter", "they get bigger"), ("ze splitsen in H en O", "they split into H and O"), ("ze verdwijnen", "they disappear")),
            GT("Uit hoeveel atomen bestaat één watermolecuul?", "How many atoms does one water molecule consist of?", 3, 0),
            MK("Wat zit er tussen de moleculen in?", "What is between the molecules?",
               ("niets (lege ruimte)", "nothing (empty space)"), ("lucht", "air"), ("water", "water"), ("stof", "dust")),
            MK("Kun je moleculen met het blote oog zien?", "Can you see molecules with the naked eye?",
               ("nee, ze zijn veel te klein", "no, they are far too small"), ("ja, als stipjes", "yes, as dots"), ("alleen met een vergrootglas", "only with a magnifying glass"), ("alleen in ijs", "only in ice")),
            MK("Staan de moleculen in een glas water stil?", "Are the molecules in a glass of water standing still?",
               ("nee, ze bewegen altijd", "no, they always move"), ("ja", "yes"), ("alleen als je roert", "only when you stir"), ("alleen als het kookt", "only when it boils")),
            MK("Wat is in het model niet op schaal?", "What is not to scale in the model?",
               ("de grootte van de atomen en de trilling", "the size of the atoms and the vibration"), ("het aantal atomen in een molecuul", "the number of atoms in a molecule"), ("de hoek van het molecuul", "the angle of the molecule"), ("niets", "nothing")),
        };

        static readonly Quizvraag[] L1B =
        {
            GT("Hoe groot is de hoek H–O–H?", "How large is the H–O–H angle?", 104.5, 1, "°"),
            MK("Welk atoom trekt het hardst aan de gedeelde elektronen?", "Which atom pulls hardest on the shared electrons?",
               ("O", "O"), ("H", "H"), ("allebei even hard", "both equally"), ("geen van beide", "neither")),
            MK("Welke lading hebben de H-atomen?", "What charge do the H atoms have?",
               ("δ+", "δ+"), ("δ−", "δ−"), ("neutraal", "neutral"), ("1+", "1+")),
            MK("Waarom is water een dipool?", "Why is water a dipole?",
               ("door de polaire O–H-bindingen en de geknikte vorm", "because of the polar O–H bonds and the bent shape"), ("omdat het vloeibaar is", "because it is liquid"), ("omdat het ionen bevat", "because it contains ions"), ("omdat het zuurstof bevat", "because it contains oxygen")),
            GT("Hoeveel vrije elektronenparen heeft het O-atoom in water?", "How many lone pairs does the O atom in water have?", 2, 0),
            GT("Wat is de molecuulmassa van H₂O? Geef minstens 1 cijfer achter de komma.", "What is the molecular mass of H₂O? Give at least 1 decimal.", 18.015, 0.02, "u", 0, 1, "18,0 (18,015)"),
            MK("Stel dat water recht was, zoals CO₂. Dan zou water…", "Suppose water were straight, like CO₂. Then water would…",
               ("geen dipool zijn", "not be a dipole"), ("zwaarder zijn", "be heavier"), ("niet bestaan", "not exist"), ("sterker polair zijn", "be more polar")),
            GT("Hoeveel moleculen zitten er in 1 mol water? (Je mag ook 6,0e23 typen.)", "How many molecules are in 1 mol of water? (You may also type 6.0e23.)", 6.022, 0.03, "× 10²³", 23, 0, "6,022 × 10²³"),
            MK("Welke binding zit binnen een watermolecuul?", "Which bond is inside a water molecule?",
               ("atoombinding (covalent)", "covalent bond"), ("ionbinding", "ionic bond"), ("metaalbinding", "metallic bond"), ("waterstofbrug", "hydrogen bond")),
            GT("Hoe lang is de O–H-binding?", "How long is the O–H bond?", 0.96, 0.02, "Å"),
        };

        static readonly Quizvraag[] L1X =
        {
            MK("Waarom is de hoek H–O–H 104,5° en niet 109,5° (tetraëder)?", "Why is the H–O–H angle 104.5° and not 109.5° (tetrahedron)?",
               ("vrije elektronenparen stoten sterker af dan bindende paren", "lone pairs repel more strongly than bonding pairs"), ("de H-atomen trekken elkaar aan", "the H atoms attract each other"), ("door de waterstofbruggen", "because of hydrogen bonds"), ("door de warmtebeweging", "because of thermal motion")),
            GT("Hoeveel trillingswijzen (normaalmodi) heeft een H₂O-molecuul? (3N − 6)", "How many vibrational modes (normal modes) does an H₂O molecule have? (3N − 6)", 3, 0),
            MK("Welke trillingswijzen zijn dat?", "Which vibrational modes are these?",
               ("symmetrische strek, asymmetrische strek, buiging", "symmetric stretch, asymmetric stretch, bend"), ("drie rotaties", "three rotations"), ("drie translaties", "three translations"), ("twee strek, één torsie", "two stretches, one torsion")),
            GT("Wat is het dipoolmoment van water?", "What is the dipole moment of water?", 1.85, 0.05, "D"),
            MK("Zwaar water (D₂O) smelt bij…", "Heavy water (D₂O) melts at…",
               ("3,8 °C", "3.8 °C"), ("0 °C", "0 °C"), ("−3,8 °C", "−3.8 °C"), ("100 °C", "100 °C")),
            MK("Waarom is D₂O dichter dan H₂O (1,11 tegen 1,00 g/cm³)?", "Why is D₂O denser than H₂O (1.11 versus 1.00 g/cm³)?",
               ("zwaardere kernen in een vrijwel even groot molecuul", "heavier nuclei in an almost equally large molecule"), ("de moleculen zijn groter", "the molecules are larger"), ("de covalente binding is sterker", "the covalent bond is stronger"), ("D₂O is geen water", "D₂O is not water")),
            GT("Hoeveel elektronen heeft een H₂O-molecuul in totaal?", "How many electrons does an H₂O molecule have in total?", 10, 0),
            MK("Welke hybridisatie krijgt O in water meestal (vereenvoudigd model)?", "Which hybridisation is usually given to O in water (simplified model)?",
               ("sp³", "sp³"), ("sp²", "sp²"), ("sp", "sp"), ("geen", "none")),
            GT("Wat is de bindingsenergie van O–H (Binas)?", "What is the bond energy of O–H (Binas)?", 463, 10, "kJ/mol"),
            GT("Hoeveel moleculen zitten er in 1,0 g water? (Je mag ook 3,3e22 typen.)", "How many molecules are in 1.0 g of water? (You may also type 3.3e22.)", 3.34, 0.05, "× 10²²", 22, 0, "3,34 × 10²²"),
            MK("Waarom geleidt zuiver water slecht, maar toch wél een heel klein beetje?", "Why does pure water conduct poorly, yet just a tiny bit?",
               ("autoprotolyse: een klein deel vormt H₃O⁺ en OH⁻", "autoionisation: a small part forms H₃O⁺ and OH⁻"), ("er zit altijd zout in", "there is always salt in it"), ("elektronen bewegen vrij", "electrons move freely"), ("door de dipool", "because of the dipole")),
        };

        // ---------- level 2 · de waterstofbrug ----------
        static readonly Quizvraag[] L2O =
        {
            MK("Wat houdt watermoleculen bij elkaar?", "What holds water molecules together?",
               ("een aantrekkende kracht tussen de moleculen", "an attractive force between the molecules"), ("lijm", "glue"), ("ijzerdraadjes", "tiny wires"), ("niets", "nothing")),
            MK("Een waterstofbrug zit…", "A hydrogen bond is…",
               ("tussen twee moleculen", "between two molecules"), ("binnen één molecuul", "inside one molecule"), ("tussen twee H-atomen van hetzelfde molecuul", "between two H atoms of the same molecule"), ("in de kern van een atoom", "in the nucleus of an atom")),
            MK("Wat is sterker?", "What is stronger?",
               ("de binding binnen het molecuul", "the bond inside the molecule"), ("de waterstofbrug", "the hydrogen bond"), ("ze zijn even sterk", "they are equally strong"), ("dat hangt van de kleur af", "that depends on the colour")),
            MK("Waarom kookt water pas bij 100 °C, terwijl het molecuul zo licht is?", "Why does water only boil at 100 °C, although the molecule is so light?",
               ("de moleculen houden elkaar stevig vast", "the molecules hold on to each other firmly"), ("water is zwaar", "water is heavy"), ("door de lucht", "because of the air"), ("door het zout", "because of the salt")),
            MK("Waarom kan een schaatsenrijder (of een ander insect) op water lopen?", "Why can a pond skater (or another insect) walk on water?",
               ("oppervlaktespanning: de moleculen trekken elkaar aan", "surface tension: the molecules attract each other"), ("water is hard", "water is hard"), ("het insect weegt niets", "the insect weighs nothing"), ("de lucht duwt het omhoog", "the air pushes it up")),
            MK("Waarom vormt water ronde druppels?", "Why does water form round drops?",
               ("de moleculen trekken elkaar naar binnen", "the molecules pull each other inwards"), ("door de zwaartekracht", "because of gravity"), ("door de wind", "because of the wind"), ("water is plakkerig", "water is sticky")),
            MK("Wat gebeurt er met de waterstofbruggen als water warmer wordt?", "What happens to the hydrogen bonds when water gets warmer?",
               ("er breken er meer", "more of them break"), ("ze worden sterker", "they get stronger"), ("er komen er meer bij", "more are formed"), ("niets", "nothing")),
            GT("Met hoeveel buren is een molecuul in ijs via waterstofbruggen verbonden?", "With how many neighbours is a molecule in ice connected by hydrogen bonds?", 4, 0),
            MK("Hoe zie je de waterstofbruggen in het model?", "How do you see the hydrogen bonds in the model?",
               ("als blauwe stippellijntjes", "as blue dotted lines"), ("als rode bollen", "as red spheres"), ("als witte staafjes", "as white sticks"), ("je ziet ze niet", "you cannot see them")),
            MK("Blijft een waterstofbrug altijd bestaan?", "Does a hydrogen bond last forever?",
               ("nee, ze breken en vormen zich steeds opnieuw", "no, they break and form again all the time"), ("ja", "yes"), ("alleen in ijs", "only in ice"), ("alleen bij koken", "only when boiling")),
        };

        static readonly Quizvraag[] L2B =
        {
            MK("Een waterstofbrug loopt van…", "A hydrogen bond runs from…",
               ("H (δ+) van het ene molecuul naar O (δ−) van een ander", "H (δ+) of one molecule to O (δ−) of another"), ("O naar O", "O to O"), ("H naar H", "H to H"), ("O naar H in hetzelfde molecuul", "O to H in the same molecule")),
            GT("Hoeveel waterstofbruggen kan één watermolecuul maximaal vormen?", "What is the maximum number of hydrogen bonds one water molecule can form?", 4, 0),
            MK("H₂S heeft dezelfde vorm als water en is zwaarder. Bij welke temperatuur kookt het?", "H₂S has the same shape as water and is heavier. At what temperature does it boil?",
               ("−60 °C", "−60 °C"), ("100 °C", "100 °C"), ("200 °C", "200 °C"), ("0 °C", "0 °C")),
            MK("H₂S kookt bij −60 °C, water bij 100 °C. Waarom?", "H₂S boils at −60 °C, water at 100 °C. Why?",
               ("H₂S vormt nauwelijks waterstofbruggen", "H₂S hardly forms hydrogen bonds"), ("H₂S is lichter", "H₂S is lighter"), ("H₂S is een ion", "H₂S is an ion"), ("zwavel is kleiner dan zuurstof", "sulfur is smaller than oxygen")),
            GT("Een H-brug kost ≈ 20 kJ/mol, een O–H-binding 463 kJ/mol. Hoeveel keer zwakker is de H-brug?", "A hydrogen bond takes ≈ 20 kJ/mol, an O–H bond 463 kJ/mol. How many times weaker is the hydrogen bond?", 23, 3, "×"),
            GT("De afstand O–O is 2,76 Å en O–H is 0,96 Å. Hoe lang is de afstand H···O in de brug?", "The O–O distance is 2.76 Å and O–H is 0.96 Å. How long is the H···O distance in the bond?", 1.80, 0.02, "Å"),
            MK("Welke stof mengt goed met water dankzij waterstofbruggen?", "Which substance mixes well with water thanks to hydrogen bonds?",
               ("ethanol", "ethanol"), ("hexaan", "hexane"), ("olie", "oil"), ("tetra", "tetrachloromethane")),
            MK("Waarom heeft water een grote soortelijke warmte (4,18 J/(g·K))?", "Why does water have a large specific heat (4.18 J/(g·K))?",
               ("een deel van de warmte gaat naar het verbreken van waterstofbruggen", "part of the heat goes into breaking hydrogen bonds"), ("water is zwaar", "water is heavy"), ("water geleidt goed", "water conducts well"), ("door opgeloste lucht", "because of dissolved air")),
            GT("Zet in level 4 de omgeving op 90 °C en lees af: hoeveel waterstofbruggen heeft een molecuul gemiddeld?", "In level 4, set the surroundings to 90 °C and read off: how many hydrogen bonds does a molecule have on average?", 2.9, 0.2),
            MK("Welke stoffen vormen ook waterstofbruggen?", "Which substances also form hydrogen bonds?",
               ("NH₃ en HF", "NH₃ and HF"), ("CH₄ en CO₂", "CH₄ and CO₂"), ("NaCl", "NaCl"), ("O₂ en N₂", "O₂ and N₂")),
        };

        static readonly Quizvraag[] L2X =
        {
            GT("Bij smelten breekt ≈ 1/8 van de waterstofbruggen: 0,25 mol bruggen per mol water. De smeltwarmte is 6,0 kJ/mol. Hoeveel energie kost één mol bruggen?", "On melting ≈ 1/8 of the hydrogen bonds break: 0.25 mol of bonds per mol of water. The heat of fusion is 6.0 kJ/mol. How much energy does one mole of bonds take?", 24, 2, "kJ/mol"),
            GT("Bij verdampen breken alle ≈ 1,75 bruggen per molecuul. De verdampingswarmte is 40,7 kJ/mol. Hoeveel kost één mol bruggen? (vereenvoudigd)", "On evaporation all ≈ 1.75 bonds per molecule break. The heat of vaporisation is 40.7 kJ/mol. How much does one mole of bonds take? (simplified)", 23, 2, "kJ/mol"),
            MK("Schattingen uit de smeltwarmte en uit de verdampingswarmte geven allebei ≈ 24 kJ/mol per H-brug. Wat leer je daaruit?", "Estimates from the heat of fusion and from the heat of vaporisation both give ≈ 24 kJ/mol per hydrogen bond. What do you learn from that?",
               ("het brugmodel is consistent", "the bond model is consistent"), ("smelten kost meer per brug", "melting costs more per bond"), ("bij verdampen breken geen bruggen", "no bonds break on evaporation"), ("de schatting klopt niet", "the estimate is wrong")),
            GT("RT = R × T is de warmte-energie per mol (R = 8,314 J/(mol·K)). Bij 25 °C (T = 298 K) is RT = 2,48 kJ/mol. Een H-brug is ≈ 20 kJ/mol. Hoeveel keer RT is dat?", "RT = R × T is the thermal energy per mole (R = 8.314 J/(mol·K)). At 25 °C (T = 298 K) RT = 2.48 kJ/mol. A hydrogen bond is ≈ 20 kJ/mol. How many times RT is that?", 8, 0.5, "× RT"),
            MK("Waarom is een waterstofbrug bijna recht (O–H···O ≈ 180°)?", "Why is a hydrogen bond almost straight (O–H···O ≈ 180°)?",
               ("de aantrekking tussen δ+ en het vrije elektronenpaar is dan het sterkst", "the attraction between δ+ and the lone pair is strongest then"), ("door de zwaartekracht", "because of gravity"), ("toeval", "coincidence"), ("door de roosterconstante", "because of the lattice constant")),
            MK("Hoe lang leeft een waterstofbrug in vloeibaar water?", "How long does a hydrogen bond live in liquid water?",
               ("≈ picoseconden (10⁻¹² s)", "≈ picoseconds (10⁻¹² s)"), ("seconden", "seconds"), ("uren", "hours"), ("milliseconden", "milliseconds")),
            MK("Een waterstofbrug leeft ≈ 1 ps. Wat betekent dat voor het 'geheugen' van water?", "A hydrogen bond lives ≈ 1 ps. What does that mean for the 'memory' of water?",
               ("de structuur past zich heel snel aan de temperatuur aan: geen geheugen van eerder", "the structure adapts to the temperature very quickly: no memory of before"), ("water onthoudt vormen", "water remembers shapes"), ("water onthoudt opgeloste stoffen", "water remembers dissolved substances"), ("de bruggen zijn blijvend", "the bonds are permanent")),
            MK("Waarom heeft ijs een open structuur?", "Why does ice have an open structure?",
               ("waterstofbruggen leggen 4 buren vast in een tetraëder", "hydrogen bonds fix 4 neighbours in a tetrahedron"), ("er zit lucht in", "there is air in it"), ("de moleculen zijn groter", "the molecules are larger"), ("door de druk", "because of pressure")),
            GT("Hoe groot is de oppervlaktespanning van water bij 25 °C?", "What is the surface tension of water at 25 °C?", 72, 2, "mN/m"),
            MK("Welk effect in het IR-spectrum wijst op waterstofbruggen?", "Which effect in the IR spectrum indicates hydrogen bonds?",
               ("de O–H-strekband schuift naar lagere golfgetallen en wordt breder", "the O–H stretch band shifts to lower wavenumbers and broadens"), ("de massa stijgt", "the mass increases"), ("de kleur wordt blauw", "the colour turns blue"), ("geen", "none")),
        };
    }
}
