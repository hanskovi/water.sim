using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WaterSim.Besturing;
using WaterSim.Weergave;
using static WaterSim.UI.UIBouw;

namespace WaterSim.UI
{
    /// <summary>
    /// De interface van Water.sim in de stijl van C.chem, helemaal in code opgebouwd (uGUI + TextMeshPro):
    ///
    /// - Startscherm: kop (H₂O · Water.sim · taal), 6 levels (3 × 2 tegels), leagues, uitleg van het level,
    ///   startknop en voortgang met sterren. Achter het scherm vliegt de camera langzaam door het ijs.
    /// - Simulatiescherm: bovenbalk (terug · level · sterren) en onderpaneel met de toestand van het water,
    ///   de temperatuurschuif, snelknoppen en weergaveknoppen. Het midden blijft vrij om rond te kijken.
    ///
    /// Opdrachten (tipbalk, antwoord, check) volgen in fase 4 deel B.
    /// </summary>
    public class AppSchil : MonoBehaviour
    {
        public Kristal kristal;
        public Vliegcamera vliegcamera;
        public Font ubuntuRegular, ubuntuVet;

        // ---------- kleuren (zoals C.chem, "naamgeving"-identiteit) ----------
        static readonly Color Achtergrond = Hex("12122A");
        static readonly Color Tegel = new Color(0.11f, 0.125f, 0.31f, 0.92f);   // #1C2050, iets doorzichtig op de lagune
        static readonly Color Pil = Hex("141836");
        static readonly Color Strook = new Color(0.05f, 0.063f, 0.19f, 0.88f);
        static readonly Color PilActief = Hex("4A3430");
        static readonly Color PilActiefTekst = Hex("F2C76A");
        static readonly Color Kaart = new Color(0.106f, 0.13f, 0.26f, 0.92f);
        static readonly Color Paneel = new Color(0.07f, 0.07f, 0.16f, 0.88f);
        static readonly Color Check = Hex("79E07A");
        static readonly Color CheckTekst = Hex("173D18");
        static readonly Color Warm1 = Hex("F2B25C");
        static readonly Color Warm2 = Hex("E05A78");
        static readonly Color Koud = Hex("4A9BFF");
        static readonly Color Grijs = Hex("9FB3C2");
        static readonly Color Zacht = Hex("AAB0D8");

        // startscherm: glas en ijs met complementair oranje als accent (oranje ligt tegenover de lagune op de kleurcirkel)
        static readonly Color OranjeL = Hex("F7884D"), OranjeR = Hex("E8502E");
        static readonly Color GlasRand = new Color(1f, 1f, 1f, 0.35f);
        static readonly Color IJsTekst = Hex("E3F7FC");
        static readonly Color LaguneHex = Hex("0E7C9E");

        struct LevelInfo
        {
            public bool speelbaar, oneindig;
            public float startT;
            public LevelInfo(bool s, bool o = true, float t = -10f) { speelbaar = s; oneindig = o; startT = t; }
        }
        static readonly LevelInfo[] Levels =
        {
            new LevelInfo(false), new LevelInfo(false),
            new LevelInfo(true, true, -10f), new LevelInfo(true, true, -10f), new LevelInfo(true, false, -10f),
            new LevelInfo(false),
        };
        static readonly int[] Snelknoppen = { -20, -5, 5, 20, 80 };

        [Header("Startscherm")]
        [Tooltip("Beelden per seconde op het startscherm (spaart batterij).")]
        public int startschermFps = 30;

        enum Pagina { Start, Sim }
        Pagina pagina = Pagina.Start;
        int level, league;

        Canvas canvas;
        CanvasScaler schaler;
        RectTransform veilig, kolom, startPagina, simPagina;
        RectTransform startAchtergrond;
        TextMeshProUGUI tTemp, tStatus, tOmgeving;
        Slider schuif;
        RectTransform uitklap;
        Image nevel;
        Image uitklapIcoon;
        bool uitgeklapt = true;
        readonly List<(Image img, TextMeshProUGUI t, int waarde)> snel = new List<(Image, TextMeshProUGUI, int)>();
        readonly List<(Image img, TextMeshProUGUI t, Func<bool> aan)> schakelaars = new List<(Image, TextMeshProUGUI, Func<bool>)>();
        readonly List<RaycastResult> raak = new List<RaycastResult>();
        Rect laatsteSafe;
        Vector2 laatsteScherm;
        float statusTimer;
        bool bouwen;          // tijdens het opbouwen geen meldingen van de schuif doorgeven
        bool schuifVast;      // alleen als je de schuif echt vasthoudt, verandert hij de temperatuur

        void Start()
        {
            if (kristal == null) kristal = FindAnyObjectByType<Kristal>();
            if (vliegcamera == null) vliegcamera = FindAnyObjectByType<Vliegcamera>();
            if (UIBouw.Regular == null && ubuntuRegular != null) UIBouw.Regular = TMP_FontAsset.CreateFontAsset(ubuntuRegular);
            if (UIBouw.Vet == null && ubuntuVet != null) UIBouw.Vet = TMP_FontAsset.CreateFontAsset(ubuntuVet);
            level = Mathf.Clamp(PlayerPrefs.GetInt("watersim.level", 2), 0, Levels.Length - 1);
            league = Mathf.Clamp(PlayerPrefs.GetInt("watersim.league", 1), 0, 2);

            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            Bouw();
            Teksten.Gewijzigd += Bouw;
            Vliegcamera.OpUI = OpUI;
            ToonPagina(Pagina.Start);
        }

        void OnDestroy()
        {
            Teksten.Gewijzigd -= Bouw;
            if (Vliegcamera.OpUI == (Predicate<Vector2>)OpUI) Vliegcamera.OpUI = null;
        }

        // ---------- opbouw ----------
        void Bouw()
        {
            bouwen = true;
            if (canvas != null) Destroy(canvas.gameObject);
            snel.Clear(); schakelaars.Clear();

            var go = new GameObject("Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            schaler = go.GetComponent<CanvasScaler>();
            schaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            schaler.referenceResolution = new Vector2(390f, 844f);

            // achtergrond in de stijl van het icoon: water (lagune + golf), ijsrooster, schuim en stoom
            startAchtergrond = Maak("Achtergrond", go.transform);
            Vul(startAchtergrond);
            foreach (IcoonAchtergrond.Laag l in System.Enum.GetValues(typeof(IcoonAchtergrond.Laag)))
            {
                // CanvasRenderer eerst zelf toevoegen: Unity doet dat hier niet vanzelf, en zonder tekent het vlak niets
                var go2 = Maak(l.ToString(), startAchtergrond).gameObject;
                go2.AddComponent<CanvasRenderer>();
                var laag = go2.AddComponent<IcoonAchtergrond>();
                laag.laag = l;
                Vul(laag.rectTransform);
            }

            veilig = Maak("VeiligeZone", go.transform);
            kolom = Maak("Kolom", veilig);
            startPagina = Maak("Startscherm", kolom); Vul(startPagina);
            simPagina = Maak("Simulatie", kolom); Vul(simPagina);
            BouwStart();
            BouwSim();
            PasSchermAan();
            ToonPagina(pagina, false);
            if (kristal != null) schuif.SetValueWithoutNotify(kristal.temperatuurOmgeving);
            bouwen = false;
        }

        void PasSchermAan()
        {
            laatsteSafe = Screen.safeArea;
            laatsteScherm = new Vector2(Screen.width, Screen.height);
            if (veilig == null || Screen.width <= 0 || Screen.height <= 0) return;
            schaler.matchWidthOrHeight = Screen.width > Screen.height ? 1f : 0f;
            Rect s = Screen.safeArea;
            veilig.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
            veilig.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
            veilig.offsetMin = veilig.offsetMax = Vector2.zero;
            // de inhoud blijft een smalle kolom (zoals op een telefoon), ook op een liggend scherm
            kolom.anchorMin = new Vector2(0.5f, 0f);
            kolom.anchorMax = new Vector2(0.5f, 1f);
            kolom.pivot = new Vector2(0.5f, 0.5f);
            float breedte = ((RectTransform)canvas.transform).rect.width * (s.width / Screen.width);
            kolom.sizeDelta = new Vector2(Mathf.Min(breedte, 440f), 0f);
            kolom.anchoredPosition = Vector2.zero;
        }

        VerticalLayoutGroup Kolom(RectTransform rt, int marge, float tussen)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(marge, marge, marge, marge);
            v.spacing = tussen;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        HorizontalLayoutGroup Rij(Transform ouder, string naam, float hoogte, float tussen, int marge = 0)
        {
            var rt = Maak(naam, ouder);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = tussen;
            h.padding = new RectOffset(marge, marge, marge, marge);
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            h.childAlignment = TextAnchor.MiddleLeft;
            LE(rt.gameObject, prefH: hoogte, minH: hoogte);
            return h;
        }

        (Image img, TextMeshProUGUI t) Pilknop(Transform ouder, string tekst, Action actie, float flexW = 1f)
        {
            var img = Vlak(ouder, "Knop " + tekst, Pil, 8f);
            LE(img.gameObject, flexW: flexW);
            var t = Tekst(img.transform, "Tekst", tekst, 13.5f, Color.white, TextAlignmentOptions.Center);
            Vul(t.rectTransform, 4, 0, 4, 0);
            Knop(img, actie);
            return (img, t);
        }

        static void Actief(Image img, TextMeshProUGUI t, bool aan)
        {
            img.color = aan ? PilActief : Pil;
            t.color = aan ? PilActiefTekst : Color.white;
        }

        // ---------- startscherm ----------
        // glazen vlak: doorzichtig wit met een dunne witte rand
        Image Glas(Transform ouder, string naam, float radius, float dekking = 0.16f, bool metRand = true)
        {
            var img = Vlak(ouder, naam, new Color(1f, 1f, 1f, dekking), radius);
            if (metRand)
            {
                var r = Maak("Rand", img.transform).gameObject.AddComponent<Image>();
                r.sprite = RandSprite(); r.type = Image.Type.Sliced; r.pixelsPerUnitMultiplier = 24f / radius;
                r.color = GlasRand; r.raycastTarget = false;
                Vul(r.rectTransform);
                r.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }
            return img;
        }

        static void Oranje(Image img, float radius)
        {
            img.sprite = Verloop(OranjeL, OranjeR);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 24f / radius;
            img.color = Color.white;
        }

        void BouwStart()
        {
            Kolom(startPagina, 14, 10);

            // nevelwolk achter alle knoppen (doet niet mee in de opmaak, ademt langzaam in Update)
            nevel = Maak("Nevel", startPagina).gameObject.AddComponent<Image>();
            nevel.sprite = NevelSprite(); nevel.type = Image.Type.Sliced; nevel.pixelsPerUnitMultiplier = 0.55f;
            nevel.color = new Color(1f, 1f, 1f, 0.30f);
            nevel.raycastTarget = false;
            nevel.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var nr = nevel.rectTransform;
            nr.anchorMin = new Vector2(0f, 0.22f); nr.anchorMax = new Vector2(1f, 0.92f);
            nr.offsetMin = new Vector2(-24f, 0f); nr.offsetMax = new Vector2(24f, 0f);

            // kop: H₂O · Water.sim · taal
            var kop = Rij(startPagina, "Kop", 44f, 10f);
            var merk = Vlak(kop.transform, "H2O", Color.white, 8f);
            LE(merk.gameObject, prefW: 58f);
            var merkTekst = Tekst(merk.transform, "Tekst", "H<sub>2</sub>O", 19f, Lin(Hex("0B3570")), TextAlignmentOptions.Center, true);
            Vul(merkTekst.rectTransform);
            var logo = Tekst(kop.transform, "Logo", "<b>Water.</b><size=72%>sim</size>", 27f, Color.white, TextAlignmentOptions.Left);
            LE(logo.gameObject, flexW: 1f);
            var taal = Glas(kop.transform, "Taal", 8f);
            LE(taal.gameObject, prefW: 52f);
            var tt = Tekst(taal.transform, "Tekst", Teksten.T("taal"), 14f, Color.white, TextAlignmentOptions.Center);
            Vul(tt.rectTransform);
            Knop(taal, () => Teksten.Stel(Teksten.Huidig == Taal.NL ? Taal.EN : Taal.NL));
            var lijn = Vlak(startPagina, "Lijn", new Color(1f, 1f, 1f, 0.35f));
            LE(lijn.gameObject, prefH: 1f);

            // levels: 3 × 2 tegels
            var raster = Maak("Levels", startPagina);
            var g = raster.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(110f, 84f);
            g.spacing = new Vector2(9f, 9f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            g.childAlignment = TextAnchor.UpperCenter;
            LE(raster.gameObject, prefH: 84f * 2 + 9f);
            for (int i = 0; i < Levels.Length; i++) LevelTegel(raster, i);

            // leagues: glazen strook, de gekozen league oranje
            var strook = Glas(startPagina, "Leagues", 9f, 0.10f);
            LE(strook.gameObject, prefH: 42f);
            var lr = strook.gameObject.AddComponent<HorizontalLayoutGroup>();
            lr.padding = new RectOffset(4, 4, 4, 4); lr.spacing = 4;
            lr.childControlWidth = lr.childControlHeight = true; lr.childForceExpandWidth = true; lr.childForceExpandHeight = true;
            for (int i = 0; i < 3; i++)
            {
                int l = i;
                var pil = Vlak(strook.transform, "League" + i, new Color(1f, 1f, 1f, 0.14f), 7f);
                if (i == league) Oranje(pil, 7f);
                var t = Tekst(pil.transform, "Tekst", Teksten.T("league." + i), 13.5f, Color.white, TextAlignmentOptions.Center, i == league);
                Vul(t.rectTransform, 4, 0, 4, 0);
                Knop(pil, () => { league = l; PlayerPrefs.SetInt("watersim.league", l); Bouw(); });
            }

            // uitleg van het gekozen level
            var kaart = Glas(startPagina, "Uitleg", 12f);
            var kv = Kolom((RectTransform)kaart.transform, 14, 6);
            kv.childAlignment = TextAnchor.UpperLeft;
            var titel = Tekst(kaart.transform, "Titel", $"{level + 1} · {Teksten.T($"level.{level + 1}.titel")}", 18f, Color.white, TextAlignmentOptions.Left, true);
            LE(titel.gameObject, prefH: 24f);
            Tekst(kaart.transform, "Uitleg", Teksten.T($"level.{level + 1}.uitleg"), 14f, IJsTekst, TextAlignmentOptions.TopLeft, false, true);

            // startknop: wit, met lagunekleurige tekst
            bool speelbaar = Levels[level].speelbaar;
            var start = speelbaar ? Vlak(startPagina, "Start", Color.white, 12f) : Glas(startPagina, "Start", 12f, 0.12f);
            LE(start.gameObject, prefH: 52f);
            var sr = start.gameObject.AddComponent<HorizontalLayoutGroup>();
            sr.childAlignment = TextAnchor.MiddleCenter; sr.spacing = 8;
            sr.childControlWidth = sr.childControlHeight = false;
            sr.childForceExpandWidth = sr.childForceExpandHeight = false;
            var lagune = Lin(LaguneHex);
            var st = Tekst(start.transform, "Tekst", speelbaar ? Teksten.T("start") : Teksten.T("binnenkort"), 19f, speelbaar ? lagune : IJsTekst, TextAlignmentOptions.Center, true);
            st.rectTransform.sizeDelta = new Vector2(st.preferredWidth, 30f);
            if (speelbaar)
            {
                var pijl = Icoon(start.transform, "Pijl", PijlRechts(), lagune);
                pijl.rectTransform.sizeDelta = new Vector2(18f, 18f);
                Knop(start, () => ToonPagina(Pagina.Sim));
            }

            // voortgang direct onder de startknop (dun)
            Voortgang(startPagina);

            var vrij = Maak("Ruimte", startPagina);
            LE(vrij.gameObject, flexH: 1f);
        }

        void LevelTegel(Transform ouder, int i)
        {
            bool gekozen = i == level, speelbaar = Levels[i].speelbaar;
            var bg = Glas(ouder, "Level" + (i + 1), 10f, 0.16f, !gekozen);
            if (gekozen) Oranje(bg, 10f);
            var tex = Resources.Load<Texture2D>($"interface/level{i + 1}_neon");
            if (tex != null)
            {
                var art = Maak("Beeld", bg.transform).gameObject.AddComponent<Image>();
                art.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                art.raycastTarget = false;
                // Unity rekt plaatjes die geen macht van 2 zijn op tot een vierkant; zet de echte verhouding (4:3) terug
                var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = 4f / 3f;
                art.color = new Color(1f, 1f, 1f, speelbaar ? 1f : 0.45f);
                Vul(art.rectTransform, 2f, 2f, 2f, 2f);
            }
            var nr = Tekst(bg.transform, "Nummer", (i + 1).ToString(), 16f, Color.white, TextAlignmentOptions.TopLeft, true);
            Vul(nr.rectTransform, 8f, 4f, 4f, 5f);
            var naam = Tekst(bg.transform, "Naam", speelbaar ? Teksten.T($"level.{i + 1}.kort") : Teksten.T("binnenkort"), 11f,
                             speelbaar || gekozen ? Color.white : IJsTekst, TextAlignmentOptions.BottomLeft);
            Vul(naam.rectTransform, 8f, 5f, 4f, 4f);
            int idx = i;
            Knop(bg, () => { level = idx; PlayerPrefs.SetInt("watersim.level", idx); Bouw(); });
        }

        void Voortgang(Transform ouder)
        {
            int sterren = PlayerPrefs.GetInt("watersim.sterren", 0);
            var rij = Rij(ouder, "Voortgang", 16f, 8f);
            var houder = Maak("Balkhouder", rij.transform);
            LE(houder.gameObject, flexW: 1f);
            var balk = Glas(houder, "Balk", 6f, 0.18f);
            balk.rectTransform.anchorMin = new Vector2(0f, 0.5f); balk.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            balk.rectTransform.sizeDelta = new Vector2(0f, 12f);
            var vul = Vlak(balk.transform, "Vul", Color.white, 6f);
            Oranje(vul, 6f);
            vul.raycastTarget = false;
            vul.rectTransform.anchorMin = Vector2.zero;
            vul.rectTransform.anchorMax = new Vector2(Mathf.Clamp(sterren / 30f, 0.05f, 1f), 1f);
            vul.rectTransform.offsetMin = vul.rectTransform.offsetMax = Vector2.zero;
            vul.transform.SetSiblingIndex(0);
            var bt = Tekst(balk.transform, "Tekst", $"{Mathf.Min(sterren, 30)}/30", 9.5f, Color.white, TextAlignmentOptions.Center, true);
            Vul(bt.rectTransform);

            var pil = Vlak(rij.transform, "Sterren", Color.white, 8f);
            Oranje(pil, 8f);
            LE(pil.gameObject, prefW: 60f, flexW: 0f);
            var h = pil.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter; h.spacing = 4; h.childControlWidth = h.childControlHeight = false;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            Icoon(pil.transform, "Ster", Ster(), Color.white).rectTransform.sizeDelta = new Vector2(11f, 11f);
            var t = Tekst(pil.transform, "Aantal", sterren.ToString(), 11f, Color.white, TextAlignmentOptions.Center, true);
            t.rectTransform.sizeDelta = new Vector2(t.preferredWidth + 2f, 16f);
        }

        // ---------- simulatiescherm ----------
        void BouwSim()
        {
            Kolom(simPagina, 10, 8);

            // bovenbalk: terug · level · sterren
            var balk = Vlak(simPagina, "Bovenbalk", Paneel, 10f);
            LE(balk.gameObject, prefH: 44f, minH: 44f);
            var bh = balk.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh.padding = new RectOffset(10, 10, 6, 6); bh.spacing = 10;
            bh.childControlWidth = bh.childControlHeight = true; bh.childForceExpandWidth = false; bh.childForceExpandHeight = true;
            bh.childAlignment = TextAnchor.MiddleLeft;
            var terug = Vlak(balk.transform, "Terug", Color.white, 6f);
            LE(terug.gameObject, prefW: 38f);
            var tp = Icoon(terug.transform, "Pijl", PijlLinks(), Achtergrond);
            Vul(tp.rectTransform, 11, 7, 11, 7);
            Knop(terug, () => ToonPagina(Pagina.Start));
            var titel = Tekst(balk.transform, "Titel", $"<b>{level + 1}</b> · {Teksten.T($"level.{level + 1}.titel")}", 15f, Color.white);
            LE(titel.gameObject, flexW: 1f);

            // vrije ruimte in het midden: daar kijk en vlieg je rond
            var vrij = Maak("Vrij", simPagina);
            LE(vrij.gameObject, flexH: 1f);

            // onderpaneel
            var paneel = Vlak(simPagina, "Bediening", Paneel, 14f);
            var pv = Kolom((RectTransform)paneel.transform, 12, 8);
            pv.childAlignment = TextAnchor.LowerCenter;

            // regel 1: temperatuur van het water + toestand + uitklappen
            var r1 = Rij(paneel.transform, "Toestand", 46f, 10f);
            tTemp = Tekst(r1.transform, "Temperatuur", "", 24f, Color.white, TextAlignmentOptions.Left, true);
            LE(tTemp.gameObject, prefW: 118f);
            tStatus = Tekst(r1.transform, "Status", "", 12.5f, Zacht, TextAlignmentOptions.Left, false, true);
            LE(tStatus.gameObject, flexW: 1f);
            var uk = Vlak(r1.transform, "Uitklappen", Pil, 8f);
            LE(uk.gameObject, prefW: 40f);
            uitklapIcoon = Icoon(uk.transform, "Pijl", uitgeklapt ? PijlOmlaag() : PijlOmhoog(), Color.white);
            Vul(uitklapIcoon.rectTransform, 11, 13, 11, 13);
            Knop(uk, () => { uitgeklapt = !uitgeklapt; uitklap.gameObject.SetActive(uitgeklapt); uitklapIcoon.sprite = uitgeklapt ? PijlOmlaag() : PijlOmhoog(); });

            uitklap = Maak("Bediening", paneel.transform);
            Kolom(uitklap, 0, 8);
            uitklap.gameObject.SetActive(uitgeklapt);

            // regel 2: temperatuur van de omgeving
            var r2 = Rij(uitklap, "Omgeving", 34f, 10f);
            tOmgeving = Tekst(r2.transform, "Label", "", 13.5f, Color.white);
            LE(tOmgeving.gameObject, prefW: 128f);
            schuif = Temperatuurschuif(r2.transform);

            // regel 3: snelknoppen
            var r3 = Rij(uitklap, "Snel", 34f, 6f);
            foreach (int w in Snelknoppen)
            {
                int t = w;
                var p = Pilknop(r3.transform, Teksten.Graden(t, 0, true), () => kristal.temperatuurOmgeving = t);
                snel.Add((p.img, p.t, t));
            }

            // regel 4: weergave
            var r4 = Rij(uitklap, "Weergave", 34f, 6f);
            var ruimte = Pilknop(r4.transform, "", WisselRuimte);
            schakelaars.Add((ruimte.img, ruimte.t, () => true));
            var kal = Pilknop(r4.transform, Teksten.T("kalotten"), () => kristal.kalotten = !kristal.kalotten);
            schakelaars.Add((kal.img, kal.t, () => kristal.kalotten));
            var hb = Pilknop(r4.transform, Teksten.T("hbruggen"), () => kristal.toonBruggen = !kristal.toonBruggen);
            schakelaars.Add((hb.img, hb.t, () => kristal.toonBruggen));
            var bw = Pilknop(r4.transform, Teksten.T("beweging"), () => kristal.beweging = !kristal.beweging);
            schakelaars.Add((bw.img, bw.t, () => kristal.beweging));

            var noot = Tekst(uitklap, "Model", Teksten.T("model"), 10.5f, Grijs, TextAlignmentOptions.Left, false, true);
            LE(noot.gameObject, prefH: 28f);
        }

        Slider Temperatuurschuif(Transform ouder)
        {
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.name = "Schuif";
            go.transform.SetParent(ouder, false);
            LE(go, flexW: 1f);
            var s = go.GetComponent<Slider>();
            s.minValue = -100f; s.maxValue = 99f; s.wholeNumbers = true;
            s.navigation = new Navigation { mode = Navigation.Mode.None };

            // spoor: blauw (koud) → wit (0 °C) → oranje-rood (warm)
            var spoor = go.transform.Find("Background").GetComponent<Image>();
            spoor.sprite = Verloop(Koud, Warm2);
            spoor.type = Image.Type.Sliced; spoor.pixelsPerUnitMultiplier = 24f / 4f;
            spoor.color = Color.white;
            var spoorRt = spoor.rectTransform;
            spoorRt.anchorMin = new Vector2(0f, 0.5f); spoorRt.anchorMax = new Vector2(1f, 0.5f);
            spoorRt.sizeDelta = new Vector2(0f, 8f);
            go.transform.Find("Fill Area").gameObject.SetActive(false);
            s.fillRect = null;

            // streepje bij 0 °C
            var nul = Vlak(go.transform, "Nul", Color.white);
            nul.raycastTarget = false;
            float f = Mathf.InverseLerp(s.minValue, s.maxValue, 0f);
            nul.rectTransform.anchorMin = nul.rectTransform.anchorMax = new Vector2(f, 0.5f);
            nul.rectTransform.sizeDelta = new Vector2(2f, 18f);
            nul.transform.SetSiblingIndex(1);

            var duim = s.handleRect.GetComponent<Image>();
            duim.sprite = Cirkel();
            duim.color = Color.white;
            s.handleRect.sizeDelta = new Vector2(24f, 0f);
            if (kristal != null) s.SetValueWithoutNotify(kristal.temperatuurOmgeving);
            s.onValueChanged.AddListener(v => { if (!bouwen && schuifVast) kristal.temperatuurOmgeving = v; });
            var trigger = go.AddComponent<EventTrigger>();
            void Bij(EventTriggerType soort, bool vast)
            {
                var e = new EventTrigger.Entry { eventID = soort };
                e.callback.AddListener(_ => { schuifVast = vast; if (vast) kristal.temperatuurOmgeving = s.value; });
                trigger.triggers.Add(e);
            }
            Bij(EventTriggerType.PointerDown, true);
            Bij(EventTriggerType.PointerUp, false);
            return s;
        }

        // ---------- pagina's ----------
        void ToonPagina(Pagina p, bool opnieuw = true)
        {
            pagina = p;
            if (startPagina == null) return;
            startPagina.gameObject.SetActive(p == Pagina.Start);
            simPagina.gameObject.SetActive(p == Pagina.Sim);
            startAchtergrond.gameObject.SetActive(p == Pagina.Start);
            if (!opnieuw || kristal == null) return;

            Application.targetFrameRate = p == Pagina.Start ? startschermFps : kristal.beeldenPerSeconde;
            // op het startscherm ligt het 3D-kristal stil (de achtergrond dekt alles af): spaart batterij
            kristal.enabled = p == Pagina.Sim;
            if (p == Pagina.Start)
            {
                if (vliegcamera != null) vliegcamera.enabled = false;
            }
            else
            {
                var info = Levels[level];
                kristal.Herstart(info.startT, info.oneindig);
                if (vliegcamera != null)
                {
                    if (info.oneindig) vliegcamera.Zet(Vector3.zero, new Vector3(0f, 0f, 1f));
                    else vliegcamera.Zet(kristal.BlokKijkpunt, Vector3.zero);
                    vliegcamera.enabled = true;
                }
                // titel en knoppen horen bij het gekozen level
                Bouw();
            }
        }

        void WisselRuimte()
        {
            kristal.oneindig = !kristal.oneindig;
            if (vliegcamera == null) return;
            if (kristal.oneindig) vliegcamera.Zet(Vector3.zero, new Vector3(0, 0, 1));
            else vliegcamera.Zet(kristal.BlokKijkpunt, Vector3.zero);
        }

        // ---------- elk beeld ----------
        void Update()
        {
            if (Screen.safeArea != laatsteSafe || Screen.width != (int)laatsteScherm.x || Screen.height != (int)laatsteScherm.y) PasSchermAan();
            if (kristal == null || kristal.Model == null) return;

            if (pagina == Pagina.Start)
            {
                // de nevel ademt heel langzaam (mist die zacht beweegt)
                if (nevel != null) nevel.color = new Color(1f, 1f, 1f, 0.27f + 0.05f * Mathf.Sin(Time.unscaledTime * 0.35f));
                return;
            }

            if ((statusTimer += Time.deltaTime) > 0.12f)
            {
                statusTimer = 0f;
                var m = kristal.Model;
                double T = m.Fase.Temperatuur;
                string fase = m.Fase.Gesmolten <= 0 ? Teksten.T("fase.ijs") : m.Fase.Gesmolten >= 1 ? Teksten.T("fase.water")
                            : kristal.temperatuurOmgeving > 0 ? Teksten.T("fase.smelten") : kristal.temperatuurOmgeving < 0 ? Teksten.T("fase.bevriezen") : Teksten.T("fase.mengsel");
                tTemp.text = Teksten.Graden(T, 1);
                tStatus.text = $"<b><color=#FFFFFF>{fase}</color></b> · {Mathf.RoundToInt((float)m.Fase.Gesmolten * 100)}% {Teksten.T("gesmolten")}\n" +
                               $"{Teksten.Getal(m.BruggenPerMolecuul, "0.0")} {Teksten.T("hbruggen")} · ρ {Teksten.Getal(m.Dichtheid, "0.000")}";
            }
            tOmgeving.text = $"{Teksten.T("omgeving")} <b>{Teksten.Graden(kristal.temperatuurOmgeving, 0, true)}</b>";
            schuif.SetValueWithoutNotify(kristal.temperatuurOmgeving);
            foreach (var k in snel) Actief(k.img, k.t, Mathf.Approximately(kristal.temperatuurOmgeving, k.waarde));
            for (int i = 0; i < schakelaars.Count; i++)
            {
                var k = schakelaars[i];
                if (i == 0) k.t.text = kristal.oneindig ? Teksten.T("oneindig") : Teksten.T("blok");
                Actief(k.img, k.t, k.aan());
            }
        }

        // ligt dit schermpunt (pixels, oorsprong linksonder) op een knop of paneel?
        bool OpUI(Vector2 p)
        {
            if (EventSystem.current == null) return false;
            var ped = new PointerEventData(EventSystem.current) { position = p };
            raak.Clear();
            EventSystem.current.RaycastAll(ped, raak);
            return raak.Count > 0;
        }
    }
}
