using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WaterSim.Sim;
using static WaterSim.UI.UIBouw;

namespace WaterSim.UI
{
    /// <summary>
    /// De quiz bij level 1 en 2: per keer 3 vragen (meerkeuze of getal) uit het gekozen niveau, met een streak
    /// (punten achter elkaar goed, loopt door over de keren heen) en de beste streak per level en niveau.
    /// Bij level 2 komt na de 3 vragen één keer de opdracht "teken waterstofbruggen".
    /// Interstitial: alleen als vraag 3 fout is, en dan met 50 % kans.
    /// Het scherm wordt bij elke stap opnieuw opgebouwd (Bouw), net als de rest van de interface.
    /// </summary>
    public partial class AppSchil
    {
        bool quizOpen, quizBeantwoord, quizLaatsteGoed, quizNieuweBeste, quizTekenen;
        const int VragenPerKeer = 3;
        const float KansInterstitialBijFout = 0.5f;
        int quizNiveau = -1, quizIndex, quizAantalGoed, quizGekozen = -1;
        int[] quizVolgorde, quizOptieVolgorde;
        string quizInvoer = "";
        Streak quizStreak;
        readonly System.Random quizRnd = new System.Random();

        int QuizLevel => level + 1;
        string StreakSleutel => $"watersim.streak.{QuizLevel}.{quizNiveau}";
        bool QuizKlaar => quizVolgorde != null && quizIndex >= quizVolgorde.Length && !quizTekenen;

        // knop in het paneel van level 1 en 2
        void Quizknop(Transform ouder)
        {
            var p = Pilknop(ouder, Teksten.T("quiz.start"), OpenQuiz);
            Oranje(p.img, 8f);
            p.t.fontStyle = FontStyles.Bold;
        }

        void OpenQuiz()
        {
            if (quizNiveau < 0) quizNiveau = Mathf.Clamp(PlayerPrefs.GetInt("watersim.quizniveau", league), 0, 2);
            quizOpen = true; infoOpen = false;
            NieuweRonde();
            Bouw();
        }

        void SluitQuiz()
        {
            quizOpen = false;
            Bouw();
        }

        void NieuweRonde()
        {
            var vragen = Quizvragen.Voor(QuizLevel, quizNiveau);
            var alle = Quiz.Schud(vragen.Count, quizRnd);
            quizVolgorde = new int[Mathf.Min(VragenPerKeer, alle.Length)];
            System.Array.Copy(alle, quizVolgorde, quizVolgorde.Length);
            quizIndex = 0; quizAantalGoed = 0; quizTekenen = false;
            quizStreak = new Streak(PlayerPrefs.GetInt(StreakSleutel, 0), PlayerPrefs.GetInt(StreakSleutel + ".nu", 0));
            NieuweVraag();
        }

        void NieuweVraag()
        {
            quizBeantwoord = false; quizGekozen = -1; quizInvoer = ""; quizNieuweBeste = false;
            var v = HuidigeVraag();
            quizOptieVolgorde = v != null && !v.IsGetal ? Quiz.Schud(v.Opties.Length, quizRnd) : null;
        }

        Quizvraag HuidigeVraag()
        {
            if (quizVolgorde == null || QuizKlaar) return null;
            return Quizvragen.Voor(QuizLevel, quizNiveau)[quizVolgorde[quizIndex]];
        }

        void Beantwoord(bool goed)
        {
            quizBeantwoord = true; quizLaatsteGoed = goed;
            if (goed) { quizAantalGoed++; quizNieuweBeste = quizStreak.Goed(1); }
            else quizStreak.Fout();
            PlayerPrefs.SetInt(StreakSleutel, quizStreak.Beste);
            PlayerPrefs.SetInt(StreakSleutel + ".nu", quizStreak.Huidig);       // de streak loopt door naar de volgende keer
            Bouw();
        }

        // na de laatste vraag: alleen bij een fout antwoord, en dan met 50 % kans, eerst een interstitial
        void NaLaatsteVraag(Action verder)
        {
            if (!quizLaatsteGoed && Advertenties.Instance != null && UnityEngine.Random.value < KansInterstitialBijFout)
                Advertenties.Instance.ToonInterstitial(verder);
            else verder();
        }

        // naar de volgende stap: volgende vraag, (level 2) de tekenopdracht, of het einde
        void VolgendeStap()
        {
            bool laatste = quizIndex + 1 >= quizVolgorde.Length;
            void Verder()
            {
                quizIndex++;
                if (laatste && QuizLevel == 2) { quizTekenen = true; NieuweTekenopdracht(); }
                else NieuweVraag();
                Bouw();
            }
            if (laatste) NaLaatsteVraag(Verder); else Verder();
        }

        void BouwQuiz(RectTransform vrij)
        {
            var kleur = Lin(Hex("05293A")); kleur.a = 0.94f;
            var vak = Vlak(vrij, "Quiz", kleur, 14f);
            Vul(vak.rectTransform, 0f, 6f, 0f, 6f);
            var kv = Kolom((RectTransform)vak.transform, 14, 10);
            kv.childAlignment = TextAnchor.UpperLeft;

            // kop: titel en sluiten
            var kop = Rij(vak.transform, "Kop", 28f, 8f);
            var titel = Tekst(kop.transform, "Titel", $"<b>{Teksten.T("quiz.titel")}</b> · {QuizLevel} · {Teksten.T($"level.{QuizLevel}.titel")}", 14.5f, Color.white);
            LE(titel.gameObject, flexW: 1f);
            var sluit = Vlak(kop.transform, "Sluiten", KnopGlas, 8f);
            LE(sluit.gameObject, prefW: 30f);
            var x = Tekst(sluit.transform, "x", "×", 20f, Color.white, TextAlignmentOptions.Center);
            Vul(x.rectTransform, 0, 2, 0, 0);
            Knop(sluit, SluitQuiz);

            // niveau (een andere keuze begint een nieuwe ronde)
            var tabs = Rij(vak.transform, "Niveaus", 30f, 6f);
            for (int i = 0; i < 3; i++)
            {
                int n = i;
                var p = Pilknop(tabs.transform, Teksten.T("league." + i), () =>
                {
                    if (n == quizNiveau) return;
                    quizNiveau = n; PlayerPrefs.SetInt("watersim.quizniveau", n);
                    NieuweRonde(); Bouw();
                });
                p.t.fontSize = 12.5f;
                Actief(p.img, p.t, quizNiveau == i);
            }

            // stand: vraagnummer en streak
            var stand = Rij(vak.transform, "Stand", 22f, 8f);
            int totaal = quizVolgorde?.Length ?? 0;
            var nr = Tekst(stand.transform, "Nummer", QuizKlaar || quizTekenen ? "" : string.Format(Teksten.T("quiz.vraag"), quizIndex + 1, totaal), 12.5f, IJsTekst);
            LE(nr.gameObject, flexW: 1f);
            var sp = Vlak(stand.transform, "Streak", Color.white, 8f);
            Oranje(sp, 8f); sp.raycastTarget = false;
            var st = Tekst(sp.transform, "Tekst", string.Format(Teksten.T("quiz.streak"), quizStreak.Huidig, quizStreak.Beste), 12f, Color.white, TextAlignmentOptions.Center, true);
            Vul(st.rectTransform, 8, 0, 8, 0);
            LE(sp.gameObject, prefW: st.preferredWidth + 18f);

            if (quizTekenen) { BouwTekenopdracht(vak.transform); return; }
            if (QuizKlaar) { BouwQuizEinde(vak.transform, totaal); return; }

            var v = HuidigeVraag();
            Tekst(vak.transform, "Vraag", v.Vraag, 16f, Color.white, TextAlignmentOptions.TopLeft, true, true);

            if (v.IsGetal) BouwGetalvraag(vak.transform, v);
            else BouwMeerkeuze(vak.transform, v);

            if (quizBeantwoord)
            {
                string goedAntwoord = !v.IsGetal ? v.Optie(0) : (v.Weergave != "" ? Teksten.Kies(v.Weergave, v.Weergave.Replace(',', '.')) : Teksten.Getal(v.Antwoord, "0.###")) + (v.Eenheid != "" && !v.Weergave.Contains("10") ? " " + v.Eenheid : "");
                var fb = Tekst(vak.transform, "Uitslag", quizLaatsteGoed ? Teksten.T(quizNieuweBeste ? "quiz.nieuwbeste" : "quiz.goed") + " +1"
                                                                         : string.Format(Teksten.T("quiz.fout"), goedAntwoord),
                               14f, quizLaatsteGoed ? Lin(Hex("8BE0A4")) : Lin(Hex("FFB48A")), TextAlignmentOptions.TopLeft, true, true);
                var vg = Pilknop(vak.transform, Teksten.T("quiz.volgende"), VolgendeStap);
                Oranje(vg.img, 8f); vg.t.fontStyle = FontStyles.Bold;
                LE(vg.img.gameObject, prefH: 40f);
            }
        }

        void BouwMeerkeuze(Transform ouder, Quizvraag v)
        {
            for (int k = 0; k < quizOptieVolgorde.Length; k++)
            {
                int optie = quizOptieVolgorde[k];
                var p = Pilknop(ouder, v.Optie(optie), () =>
                {
                    if (quizBeantwoord) return;
                    quizGekozen = optie;
                    Beantwoord(optie == 0);
                });
                LE(p.img.gameObject, prefH: 42f);
                p.t.textWrappingMode = TextWrappingModes.Normal;
                p.t.enableAutoSizing = true; p.t.fontSizeMin = 10f; p.t.fontSizeMax = 13.5f;
                Vul(p.t.rectTransform, 10, 2, 10, 2);
                if (quizBeantwoord)
                {
                    if (optie == 0) Oranje(p.img, 8f);                                           // het goede antwoord
                    else if (optie == quizGekozen) p.img.color = new Color(0.85f, 0.25f, 0.2f, 0.55f); // jouw (foute) keuze
                    else p.t.color = new Color(1f, 1f, 1f, 0.5f);
                }
            }
        }

        void BouwGetalvraag(Transform ouder, Quizvraag v)
        {
            var rij = Rij(ouder, "Invoer", 42f, 8f);
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = "Getal"; go.transform.SetParent(rij.transform, false);
            LE(go, flexW: 1f);
            var veld = go.GetComponent<TMP_InputField>();
            var achter = go.GetComponent<Image>();
            achter.sprite = RondSprite(); achter.type = Image.Type.Sliced; achter.pixelsPerUnitMultiplier = 24f / 8f;
            achter.color = new Color(1f, 1f, 1f, 0.92f);
            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
            {
                if (Regular != null) t.font = Regular;
                t.fontSize = 17f; t.alignment = TextAlignmentOptions.MidlineLeft;
            }
            veld.textComponent.color = Lin(Hex("0B2230"));
            if (veld.placeholder is TMP_Text ph) { ph.text = Teksten.T("quiz.invoer"); ph.color = new Color(0.3f, 0.4f, 0.45f, 0.7f); }
            veld.contentType = TMP_InputField.ContentType.Custom;
            veld.inputType = TMP_InputField.InputType.Standard;
            veld.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
            veld.characterValidation = TMP_InputField.CharacterValidation.None;
            veld.text = quizInvoer;
            veld.interactable = !quizBeantwoord;
            veld.onValueChanged.AddListener(s => quizInvoer = s);
            veld.onSubmit.AddListener(_ => Controleer(v));
            if (v.Eenheid != "")
            {
                var e = Tekst(rij.transform, "Eenheid", v.Eenheid, 15f, Color.white, TextAlignmentOptions.MidlineLeft);
                LE(e.gameObject, prefW: Mathf.Max(24f, e.preferredWidth + 4f));
            }
            if (!quizBeantwoord)
            {
                var c = Pilknop(rij.transform, Teksten.T("quiz.controleer"), () => Controleer(v), 0f);
                Oranje(c.img, 8f); c.t.fontStyle = FontStyles.Bold;
                LE(c.img.gameObject, prefW: 110f);
            }
        }

        void Controleer(Quizvraag v)
        {
            if (quizBeantwoord || !Quiz.LeesGetal(quizInvoer, out _)) return;     // eerst een getal intypen
            Beantwoord(Quiz.GetalKlopt(quizInvoer, v.Antwoord, v.Marge, v.Macht, v.MinDecimalen));
        }

        void BouwQuizEinde(Transform ouder, int totaal)
        {
            Tekst(ouder, "Klaar", string.Format(Teksten.T("quiz.klaar"), quizAantalGoed, totaal), 18f, Color.white, TextAlignmentOptions.TopLeft, true, true);
            var rij = Rij(ouder, "Knoppen", 42f, 8f);
            var nog = Pilknop(rij.transform, Teksten.T("quiz.nogeen"), () => { NieuweRonde(); Bouw(); });
            Oranje(nog.img, 8f); nog.t.fontStyle = FontStyles.Bold;
            Pilknop(rij.transform, Teksten.T("quiz.sluiten"), SluitQuiz);
        }

        // ---------- level 2: tekenopdracht "teken waterstofbruggen" (na de 3 vragen, één keer) ----------

        readonly System.Collections.Generic.List<Tekenveld.Plaatsing> tekenMoleculen = new System.Collections.Generic.List<Tekenveld.Plaatsing>();
        readonly System.Collections.Generic.List<HBrugRegels.Brug> tekenBruggen = new System.Collections.Generic.List<HBrugRegels.Brug>();
        HBrugRegels.Oordeel[] tekenOordelen;
        bool tekenRegelsOpen, tekenGelukt;
        int tekenPunten;
        Molecuul2D tekenA, tekenB;

        // 6 moleculen (3 van elke soort) in 2 kolommen × 3 rijen, om en om, met een kleine draai
        void NieuweTekenopdracht()
        {
            (tekenA, tekenB) = Molecuulbibliotheek.Kies(quizNiveau, quizRnd);
            tekenMoleculen.Clear(); tekenBruggen.Clear(); tekenOordelen = null; tekenRegelsOpen = false; tekenGelukt = false;
            for (int rij = 0; rij < 3; rij++)
                for (int kol = 0; kol < 2; kol++)
                {
                    var m = (rij + kol) % 2 == 0 ? tekenA : tekenB;
                    bool klein = m.Atomen.Length <= 5;
                    float hoek = klein ? (float)quizRnd.NextDouble() * 360f : ((float)quizRnd.NextDouble() - 0.5f) * 40f + (quizRnd.Next(2) == 0 ? 0f : 180f);
                    var plek = new Vector2(0.25f + kol * 0.5f + ((float)quizRnd.NextDouble() - 0.5f) * 0.06f,
                                           1f - (rij + 0.5f) / 3f + ((float)quizRnd.NextDouble() - 0.5f) * 0.04f);
                    tekenMoleculen.Add(new Tekenveld.Plaatsing { Mol = m, Plek = plek, Hoek = hoek });
                }
        }

        static string MolNaam(Molecuul2D m) => Teksten.T("mol." + m.Sleutel);

        void BouwTekenopdracht(Transform ouder)
        {
            if (tekenA == null || tekenMoleculen.Count == 0) NieuweTekenopdracht();
            int nodig = HBrugRegels.Nodig(quizNiveau);
            bool gecontroleerd = tekenOordelen != null;

            Tekst(ouder, "Opdracht", string.Format(Teksten.T("quiz.teken.opdracht"), nodig, MolNaam(tekenA), MolNaam(tekenB)),
                  13.5f, Color.white, TextAlignmentOptions.TopLeft, false, true);

            if (tekenRegelsOpen)
            {
                var rv = Vlak(ouder, "Regels", new Color(1f, 1f, 1f, 0.06f), 10f);
                LE(rv.gameObject, flexH: 1f, minH: 200f);
                var rt = Tekst(rv.transform, "Tekst", Teksten.T("quiz.teken.regeltekst"), 13f, IJsTekst, TextAlignmentOptions.TopLeft, false, true);
                Vul(rt.rectTransform, 12, 10, 12, 10);
                rt.enableAutoSizing = true; rt.fontSizeMin = 9f; rt.fontSizeMax = 13f;
            }
            else
            {
                // dichte achtergrond, zodat de 3D-scène er niet doorheen schijnt
                var achter = Lin(Hex("0A3446"));
                var vlak = Vlak(ouder, "Tekenvlak", achter, 10f);
                LE(vlak.gameObject, flexH: 1f, minH: 220f);
                var go = new GameObject("Tekenveld", typeof(RectTransform));
                go.transform.SetParent(vlak.transform, false);
                Vul((RectTransform)go.transform, 0, 0, 0, 0);
                go.AddComponent<CanvasRenderer>();                   // eerst zelf toevoegen, anders tekent het veld niets
                var veld = go.AddComponent<Tekenveld>();
                veld.color = Color.white;
                veld.Font = Vet;
                veld.Achter = achter;
                veld.Moleculen = tekenMoleculen; veld.Bruggen = tekenBruggen; veld.Oordelen = tekenOordelen;
                veld.Vergrendeld = gecontroleerd;
                veld.Ververs();

                var model = Tekst(ouder, "Model", Teksten.T("quiz.teken.model"), 10.5f, new Color(1f, 1f, 1f, 0.55f), TextAlignmentOptions.Center);
                LE(model.gameObject, prefH: 14f);
                TextMeshProUGUI teller = null;
                veld.Gewijzigd = () => { if (teller != null) teller.text = string.Format(Teksten.T("quiz.teken.geteld"), tekenBruggen.Count); };

                if (!gecontroleerd)
                {
                    var rij = Rij(ouder, "Knoppen", 40f, 8f);
                    teller = Tekst(rij.transform, "Teller", string.Format(Teksten.T("quiz.teken.geteld"), tekenBruggen.Count), 12.5f, IJsTekst);
                    LE(teller.gameObject, prefW: 84f);
                    Pilknop(rij.transform, Teksten.T("quiz.teken.wis"), () => { tekenBruggen.Clear(); veld.Ververs(); veld.Gewijzigd(); });
                    Pilknop(rij.transform, Teksten.T("quiz.teken.regels"), () => { tekenRegelsOpen = true; Bouw(); });
                    var c = Pilknop(rij.transform, Teksten.T("quiz.controleer"), ControleerTekening, 1.4f);
                    Oranje(c.img, 8f); c.t.fontStyle = FontStyles.Bold;
                    return;
                }
            }

            if (tekenRegelsOpen)
            {
                var terug = Pilknop(ouder, Teksten.T("quiz.teken.tekening"), () => { tekenRegelsOpen = false; Bouw(); });
                LE(terug.img.gameObject, prefH: 40f);
                return;
            }

            // uitslag met uitleg per soort fout
            int goed = 0; var soorten = new System.Collections.Generic.List<HBrugRegels.Oordeel>();
            foreach (var o in tekenOordelen) { if (o == HBrugRegels.Oordeel.Goed) goed++; else if (!soorten.Contains(o)) soorten.Add(o); }
            string uitslag = tekenGelukt ? Teksten.T("quiz.teken.goed").Replace("{0}", goed.ToString()) + (quizNieuweBeste ? " " + Teksten.T("quiz.teken.beste") : "") + $" +{tekenPunten}"
                           : goed < nodig ? string.Format(Teksten.T("quiz.teken.tekort"), goed, nodig) : "";
            foreach (var o in soorten) uitslag += (uitslag != "" ? "\n" : "") + "• " + Teksten.T("quiz.teken." + o);
            var fb = Tekst(ouder, "Uitslag", uitslag, 12.5f, tekenGelukt ? Lin(Hex("8BE0A4")) : Lin(Hex("FFB48A")), TextAlignmentOptions.TopLeft, true, true);
            fb.enableAutoSizing = true; fb.fontSizeMin = 9.5f; fb.fontSizeMax = 12.5f;
            var vg = Pilknop(ouder, Teksten.T("quiz.volgende"), () => { quizTekenen = false; tekenA = null; tekenMoleculen.Clear(); Bouw(); });
            Oranje(vg.img, 8f); vg.t.fontStyle = FontStyles.Bold;
            LE(vg.img.gameObject, prefH: 40f);
        }

        void ControleerTekening()
        {
            if (tekenOordelen != null) return;
            var mols = new System.Collections.Generic.List<Molecuul2D>();
            foreach (var p in tekenMoleculen) mols.Add(p.Mol);
            tekenOordelen = HBrugRegels.Beoordeel(mols, tekenBruggen);
            int goed = 0; bool fout = false;
            foreach (var o in tekenOordelen) { if (o == HBrugRegels.Oordeel.Goed) goed++; else fout = true; }
            tekenGelukt = goed >= HBrugRegels.Nodig(quizNiveau) && !fout;
            quizNieuweBeste = false; tekenPunten = 0;
            if (tekenGelukt) { tekenPunten = Quiz.PuntenTekenen(quizNiveau); quizNieuweBeste = quizStreak.Goed(tekenPunten); }
            else quizStreak.Fout();
            PlayerPrefs.SetInt(StreakSleutel, quizStreak.Beste);
            PlayerPrefs.SetInt(StreakSleutel + ".nu", quizStreak.Huidig);
            Bouw();
        }
    }
}
