using System;
using System.Collections.Generic;

namespace WaterSim.Sim
{
    public enum Element { H, C, N, O, S }

    /// <summary>
    /// Een molecuul als structuurformule in 2D (bindingslengte ≈ 1), voor de tekenopdracht "teken waterstofbruggen".
    /// </summary>
    public class Molecuul2D
    {
        public readonly string Sleutel;
        public readonly Element[] Atomen;
        public readonly double[] X, Y;
        public readonly (int a, int b, int orde)[] Bindingen;

        public Molecuul2D(string sleutel, (Element e, double x, double y)[] atomen, (int a, int b, int orde)[] bindingen)
        {
            Sleutel = sleutel;
            Atomen = new Element[atomen.Length]; X = new double[atomen.Length]; Y = new double[atomen.Length];
            for (int i = 0; i < atomen.Length; i++) { Atomen[i] = atomen[i].e; X[i] = atomen[i].x; Y[i] = atomen[i].y; }
            Bindingen = bindingen;
        }

        /// <summary>Het atoom waar dit (H-)atoom aan vastzit, of −1.</summary>
        public int Buur(int atoom)
        {
            foreach (var (a, b, _) in Bindingen) { if (a == atoom) return b; if (b == atoom) return a; }
            return -1;
        }

        /// <summary>Aantal vrije elektronenparen (O en S: 2, N: 1, C en H: 0).</summary>
        public int VrijeParen(int atoom) => Atomen[atoom] == Element.O || Atomen[atoom] == Element.S ? 2 : Atomen[atoom] == Element.N ? 1 : 0;

        /// <summary>Kan dit molecuul een H-brug geven (een H aan N of O)?</summary>
        public bool IsDonor { get { for (int i = 0; i < Atomen.Length; i++) if (HBrugRegels.IsDonorH(this, i)) return true; return false; } }
        /// <summary>Kan dit molecuul een H-brug ontvangen (een vrij elektronenpaar op N of O)?</summary>
        public bool IsAcceptor { get { foreach (var e in Atomen) if (e == Element.N || e == Element.O) return true; return false; } }
    }

    /// <summary>
    /// Regels voor een waterstofbrug, zoals op school: van een H die aan N of O vastzit naar een vrij elektronenpaar van
    /// N of O van een ánder molecuul. Een H geeft hooguit één brug; O kan er 2 ontvangen, N 1. H aan C doet niet mee;
    /// H₂S vormt (in de schoolnorm) geen waterstofbruggen. (F laten we weg: dat zit niet in de opdracht.)
    /// </summary>
    public static class HBrugRegels
    {
        public enum Oordeel { Goed, HNietAanNOfO, AcceptorNietNOfO, ZelfdeMolecuul, HAlBezet, ParenOp, BeginNietBijH }

        public static bool IsDonorH(Molecuul2D m, int atoom)
        {
            if (m.Atomen[atoom] != Element.H) return false;
            int b = m.Buur(atoom);
            return b >= 0 && (m.Atomen[b] == Element.N || m.Atomen[b] == Element.O);
        }

        /// <summary>Eén getekende brug: van atoom <c>H</c> in molecuul <c>Van</c> naar atoom <c>Naar</c> in molecuul <c>Doel</c>.</summary>
        public struct Brug { public int Van, H, Doel, Naar; }

        /// <summary>
        /// Beoordeelt alle getekende bruggen samen (want een H of een elektronenpaar kan maar één keer gebruikt worden).
        /// Bruggen worden in volgorde bekeken: de eerste die een H of paar gebruikt, telt; een volgende is "al bezet".
        /// </summary>
        public static Oordeel[] Beoordeel(IReadOnlyList<Molecuul2D> moleculen, IReadOnlyList<Brug> bruggen)
        {
            var uit = new Oordeel[bruggen.Count];
            var hGebruikt = new HashSet<(int, int)>();
            var parenGebruikt = new Dictionary<(int, int), int>();
            for (int k = 0; k < bruggen.Count; k++)
            {
                var b = bruggen[k];
                var md = moleculen[b.Van]; var ma = moleculen[b.Doel];
                if (b.Van == b.Doel) { uit[k] = Oordeel.ZelfdeMolecuul; continue; }
                if (md.Atomen[b.H] != Element.H) { uit[k] = Oordeel.BeginNietBijH; continue; }
                if (!IsDonorH(md, b.H)) { uit[k] = Oordeel.HNietAanNOfO; continue; }
                var e = ma.Atomen[b.Naar];
                if (e != Element.N && e != Element.O) { uit[k] = Oordeel.AcceptorNietNOfO; continue; }
                if (hGebruikt.Contains((b.Van, b.H))) { uit[k] = Oordeel.HAlBezet; continue; }
                parenGebruikt.TryGetValue((b.Doel, b.Naar), out int n);
                if (n >= ma.VrijeParen(b.Naar)) { uit[k] = Oordeel.ParenOp; continue; }
                hGebruikt.Add((b.Van, b.H));
                parenGebruikt[(b.Doel, b.Naar)] = n + 1;
                uit[k] = Oordeel.Goed;
            }
            return uit;
        }

        /// <summary>Hoeveel bruggen er minstens getekend moeten worden, per niveau.</summary>
        public static int Nodig(int niveau) => niveau <= 0 ? 2 : niveau == 1 ? 3 : 4;
    }

    /// <summary>
    /// Draait de moleculen van de tekenopdracht zo dat hun H-brugvormers (H aan N of O) en -ontvangers (N of O) naar een
    /// buurmolecuul wijzen dat daar een partner voor heeft. Zo zijn de bruggen goed te tekenen (vooral met aceton en de
    /// zuren). Lange moleculen liggen bij voorkeur ongeveer liggend, dan passen ze in hun vak.
    /// </summary>
    public static class Schikking
    {
        /// <summary>Hoek (graden, tegen de klok in) per molecuul; plekken in dezelfde eenheden in x en y.</summary>
        public static double[] Richt(IReadOnlyList<Molecuul2D> mols, IReadOnlyList<(double x, double y)> plekken, Random rnd)
        {
            int n = mols.Count;
            var hoek = new double[n];
            for (int i = 0; i < n; i++) hoek[i] = rnd.NextDouble() * 360;
            var volgorde = new int[n];
            for (int i = 0; i < n; i++) volgorde[i] = i;
            for (int ronde = 0; ronde < 4; ronde++)
            {
                for (int k = n - 1; k > 0; k--) { int j = rnd.Next(k + 1); (volgorde[k], volgorde[j]) = (volgorde[j], volgorde[k]); }
                foreach (int i in volgorde)
                {
                    double beste = hoek[i], bs = double.MinValue;
                    for (int c = 0; c < 36; c++)
                    {
                        double h = c * 10 + rnd.NextDouble() * 6 - 3;
                        double sc = Score(mols, plekken, hoek, i, h) + rnd.NextDouble() * 0.02;
                        if (sc > bs) { bs = sc; beste = h; }
                    }
                    hoek[i] = beste;
                }
            }
            return hoek;
        }

        // hoe goed molecuul i onder hoek h zijn polaire atomen naar passende buren richt (en of het past in zijn vak)
        static double Score(IReadOnlyList<Molecuul2D> mols, IReadOnlyList<(double x, double y)> plek, double[] hoek, int i, double h)
        {
            var m = mols[i];
            Midden(m, out double mx, out double my);
            double c = Math.Cos(h * Math.PI / 180), s = Math.Sin(h * Math.PI / 180);
            double score = 0, x0 = 0, x1 = 0, y0 = 0, y1 = 0;
            for (int a = 0; a < m.Atomen.Length; a++)
            {
                double vx = m.X[a] - mx, vy = m.Y[a] - my;
                double rx = c * vx - s * vy, ry = s * vx + c * vy;
                x0 = Math.Min(x0, rx); x1 = Math.Max(x1, rx); y0 = Math.Min(y0, ry); y1 = Math.Max(y1, ry);
                bool donor = HBrugRegels.IsDonorH(m, a), acceptor = m.Atomen[a] == Element.N || m.Atomen[a] == Element.O;
                if (!donor && !acceptor) continue;
                double L = Math.Sqrt(rx * rx + ry * ry);
                if (L < 1e-6) continue;
                for (int j = 0; j < mols.Count; j++)
                {
                    if (j == i) continue;
                    bool partner = (donor && mols[j].IsAcceptor) || (acceptor && mols[j].IsDonor);
                    if (!partner) continue;
                    double dx = plek[j].x - plek[i].x, dy = plek[j].y - plek[i].y, d = Math.Sqrt(dx * dx + dy * dy);
                    double cos = (rx * dx + ry * dy) / (L * d);
                    if (cos > 0) score += cos * cos / (d * d);
                }
            }
            // lange moleculen liggend: een staand molecuul moet kleiner getekend worden
            double breed = x1 - x0, hoog = y1 - y0;
            if (m.Atomen.Length > 6) score -= 4.0 * Math.Max(0, hoog - 2.4) / Math.Max(1, breed);
            return score;
        }

        public static void Midden(Molecuul2D m, out double mx, out double my)
        {
            double x0 = double.MaxValue, x1 = double.MinValue, y0 = double.MaxValue, y1 = double.MinValue;
            for (int a = 0; a < m.Atomen.Length; a++) { x0 = Math.Min(x0, m.X[a]); x1 = Math.Max(x1, m.X[a]); y0 = Math.Min(y0, m.Y[a]); y1 = Math.Max(y1, m.Y[a]); }
            mx = (x0 + x1) / 2; my = (y0 + y1) / 2;
        }
    }

    /// <summary>De moleculen voor de tekenopdracht, met de keuze per niveau.</summary>
    public static class Molecuulbibliotheek
    {
        const Element H = Element.H, C = Element.C, N = Element.N, O = Element.O, S = Element.S;

        // methylgroep (C met 3 H) op (x, y), met de H-atomen naar links/boven/onder
        static void CH3(List<(Element, double, double)> at, List<(int, int, int)> bi, double x, double y, int hoekLinks = 1)
        {
            int c = at.Count; at.Add((C, x, y));
            at.Add((H, x - hoekLinks, y)); bi.Add((c, c + 1, 1));
            at.Add((H, x, y + 1)); bi.Add((c, c + 2, 1));
            at.Add((H, x, y - 1)); bi.Add((c, c + 3, 1));
        }
        // CH2 op (x, y), H boven en onder
        static void CH2(List<(Element, double, double)> at, List<(int, int, int)> bi, double x, double y, int vorige)
        {
            int c = at.Count; at.Add((C, x, y)); bi.Add((vorige, c, 1));
            at.Add((H, x, y + 1)); bi.Add((c, c + 1, 1));
            at.Add((H, x, y - 1)); bi.Add((c, c + 2, 1));
        }

        // alcohol met n C-atomen: CH3–(CH2)n−1–O–H
        static Molecuul2D Alcohol(string sleutel, int n)
        {
            var at = new List<(Element, double, double)>(); var bi = new List<(int, int, int)>();
            CH3(at, bi, 0, 0);
            int vorige = 0;
            for (int k = 1; k < n; k++) { int c = at.Count; CH2(at, bi, 1.2 * k, 0, vorige); vorige = c; }
            int o = at.Count; at.Add((O, 1.2 * n, 0)); bi.Add((vorige, o, 1));
            at.Add((H, 1.2 * n + 0.6, 0.8)); bi.Add((o, o + 1, 1));
            return new Molecuul2D(sleutel, at.ToArray(), bi.ToArray());
        }

        // amine met n C-atomen: CH3–(CH2)n−1–NH2
        static Molecuul2D Amine(string sleutel, int n)
        {
            var at = new List<(Element, double, double)>(); var bi = new List<(int, int, int)>();
            CH3(at, bi, 0, 0);
            int vorige = 0;
            for (int k = 1; k < n; k++) { int c = at.Count; CH2(at, bi, 1.2 * k, 0, vorige); vorige = c; }
            int nn = at.Count; at.Add((N, 1.2 * n, 0)); bi.Add((vorige, nn, 1));
            at.Add((H, 1.2 * n + 0.6, 0.8)); bi.Add((nn, nn + 1, 1));
            at.Add((H, 1.2 * n + 0.6, -0.8)); bi.Add((nn, nn + 2, 1));
            return new Molecuul2D(sleutel, at.ToArray(), bi.ToArray());
        }

        static Molecuul2D Alkaan(string sleutel, int n)
        {
            var at = new List<(Element, double, double)>(); var bi = new List<(int, int, int)>();
            if (n == 1)
            {
                at.Add((C, 0, 0)); at.Add((H, -1, 0)); at.Add((H, 1, 0)); at.Add((H, 0, 1)); at.Add((H, 0, -1));
                bi.Add((0, 1, 1)); bi.Add((0, 2, 1)); bi.Add((0, 3, 1)); bi.Add((0, 4, 1));
                return new Molecuul2D(sleutel, at.ToArray(), bi.ToArray());
            }
            CH3(at, bi, 0, 0);
            int vorige = 0;
            for (int k = 1; k < n - 1; k++) { int c = at.Count; CH2(at, bi, 1.2 * k, 0, vorige); vorige = c; }
            int laatste = at.Count; double x = 1.2 * (n - 1);
            at.Add((C, x, 0)); bi.Add((vorige, laatste, 1));
            at.Add((H, x + 1, 0)); bi.Add((laatste, laatste + 1, 1));
            at.Add((H, x, 1)); bi.Add((laatste, laatste + 2, 1));
            at.Add((H, x, -1)); bi.Add((laatste, laatste + 3, 1));
            return new Molecuul2D(sleutel, at.ToArray(), bi.ToArray());
        }

        public static readonly Molecuul2D Water = new Molecuul2D("water",
            new[] { (O, 0.0, 0.0), (H, -0.8, -0.6), (H, 0.8, -0.6) }, new[] { (0, 1, 1), (0, 2, 1) });
        public static readonly Molecuul2D Ammoniak = new Molecuul2D("ammoniak",
            new[] { (N, 0.0, 0.0), (H, -0.95, -0.4), (H, 0.95, -0.4), (H, 0.0, -1.05) }, new[] { (0, 1, 1), (0, 2, 1), (0, 3, 1) });
        public static readonly Molecuul2D Waterstofsulfide = new Molecuul2D("h2s",
            new[] { (S, 0.0, 0.0), (H, -0.9, -0.6), (H, 0.9, -0.6) }, new[] { (0, 1, 1), (0, 2, 1) });
        public static readonly Molecuul2D Aceton = new Molecuul2D("aceton",
            new[] { (C, 0.0, 0.0), (O, 0.0, 1.2), (C, -1.2, -0.6), (H, -2.2, -0.6), (H, -1.2, -1.6), (H, -1.6, 0.3),
                    (C, 1.2, -0.6), (H, 2.2, -0.6), (H, 1.2, -1.6), (H, 1.6, 0.3) },
            new[] { (0, 1, 2), (0, 2, 1), (2, 3, 1), (2, 4, 1), (2, 5, 1), (0, 6, 1), (6, 7, 1), (6, 8, 1), (6, 9, 1) });
        public static readonly Molecuul2D Methanol = Alcohol("methanol", 1);
        public static readonly Molecuul2D Ethanol = Alcohol("ethanol", 2);
        public static readonly Molecuul2D Propanol = Alcohol("propanol", 3);
        public static readonly Molecuul2D Butanol = Alcohol("butanol", 4);
        public static readonly Molecuul2D Methaanamine = Amine("methaanamine", 1);
        public static readonly Molecuul2D Ethaanamine = Amine("ethaanamine", 2);
        public static readonly Molecuul2D Methaan = Alkaan("methaan", 1);
        public static readonly Molecuul2D Butaan = Alkaan("butaan", 4);
        // carbonzuren: H–C(=O)–O–H en CH3–C(=O)–O–H
        public static readonly Molecuul2D Methaanzuur = new Molecuul2D("methaanzuur",
            new[] { (C, 0.0, 0.0), (H, -1.0, 0.0), (O, 0.6, 1.04), (O, 0.6, -1.04), (H, 1.55, -1.04) },
            new[] { (0, 1, 1), (0, 2, 2), (0, 3, 1), (3, 4, 1) });
        public static readonly Molecuul2D Ethaanzuur = Zuur("ethaanzuur");

        static Molecuul2D Zuur(string sleutel)
        {
            var at = new List<(Element, double, double)>(); var bi = new List<(int, int, int)>();
            CH3(at, bi, 0, 0);
            int c = at.Count; at.Add((C, 1.2, 0)); bi.Add((0, c, 1));
            at.Add((O, 1.8, 1.04)); bi.Add((c, c + 1, 2));
            at.Add((O, 1.8, -1.04)); bi.Add((c, c + 2, 1));
            at.Add((H, 2.75, -1.04)); bi.Add((c + 2, c + 3, 1));
            return new Molecuul2D(sleutel, at.ToArray(), bi.ToArray());
        }

        /// <summary>
        /// Kiest de twee soorten moleculen voor een opdracht:
        /// onderbouw: water + methanol of ammoniak; bovenbouw: water + één van methanol, ammoniak, ethanol, methaanamine,
        /// ethaanamine; XPRT: één stof die H-bruggen kan vormen (ook methaanzuur en ethaanzuur) + één apolaire of "lastige" stof
        /// (methaan, butaan, H₂S, aceton).
        /// </summary>
        public static (Molecuul2D a, Molecuul2D b) Kies(int niveau, Random rnd)
        {
            if (niveau <= 0) return (Water, rnd.Next(2) == 0 ? Methanol : Ammoniak);
            if (niveau == 1)
            {
                var keus = new[] { Methanol, Ammoniak, Ethanol, Methaanamine, Ethaanamine };
                return (Water, keus[rnd.Next(keus.Length)]);
            }
            var polair = new[] { Water, Methanol, Ethanol, Propanol, Butanol, Ammoniak, Methaanamine, Ethaanamine, Methaanzuur, Ethaanzuur };
            var anders = new[] { Methaan, Butaan, Waterstofsulfide, Aceton };
            return (polair[rnd.Next(polair.Length)], anders[rnd.Next(anders.Length)]);
        }
    }
}
