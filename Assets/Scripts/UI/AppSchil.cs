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
            public int scene;                // 0 = kristal, 1 = één molecuul, 2 = twee moleculen met H-brug
            public LevelInfo(bool s, bool o = true, float t = -10f, int sc = 0) { speelbaar = s; oneindig = o; startT = t; scene = sc; }
        }
        static readonly LevelInfo[] Levels =
        {
            new LevelInfo(true, sc: 1), new LevelInfo(true, sc: 2),
            new LevelInfo(true, true, -10f), new LevelInfo(true, true, -10f), new LevelInfo(true, false, -10f),
            new LevelInfo(false),
        };
        static readonly int[] Snelknoppen = { -20, -5, 4, 20, 80 };   // 4 °C: water heeft dan de grootste dichtheid

        [Header("Demo")]
        [Tooltip("Verkennen: alleen de simulaties, zonder leagues, sterren en voortgang (die komen terug met de quiz).")]
        public bool verkennen = true;

        [Header("Startscherm")]
        [Tooltip("Beelden per seconde op het startscherm (spaart batterij).")]
        public int startschermFps = 30;

        enum Pagina { Start, Sim }
        Pagina pagina = Pagina.Start;
        int level, league;

        Molecuulscene molecuul;
        bool MolecuulLevel => Levels[level].scene != 0;
        Canvas canvas;
        CanvasScaler schaler;
        RectTransform veilig, kolom, startPagina, simPagina;
        RectTransform startAchtergrond;
        TextMeshProUGUI tTemp, tOmgeving;
        // statusblok: ring om de temperatuur, plateaulabel, opwarmkromme, regel, en H-bruggen + ρ
        Voortgangsring ring;
        Opwarmkromme kromme;
        TextMeshProUGUI tRingSub, tLabel, tFase, tRegel, tBruggen, tRho, tMax;
        Image labelPil, maxPil;
        static readonly Color AccentWarm = Hex("F28A55"), AccentKoud = Hex("5BC2FF"), AccentRust = Hex("E8E3D8"), PilTekst = Hex("0B2230"), KrommeTekst = Hex("B9DCEA");
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
            molecuul = gameObject.AddComponent<Molecuulscene>();
            molecuul.bron = kristal; molecuul.font = UIBouw.Regular;
            molecuul.enabled = false;
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
            achtergrondLagen.Clear();
            foreach (IcoonAchtergrond.Laag l in System.Enum.GetValues(typeof(IcoonAchtergrond.Laag)))
            {
                // CanvasRenderer eerst zelf toevoegen: Unity doet dat hier niet vanzelf, en zonder tekent het vlak niets
                var go2 = Maak(l.ToString(), startAchtergrond).gameObject;
                go2.AddComponent<CanvasRenderer>();
                var laag = go2.AddComponent<IcoonAchtergrond>();
                laag.laag = l;
                Vul(laag.rectTransform);
                achtergrondLagen.Add(laag);
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
            // Schalen op de breedte (smalle telefoons) of op de hoogte (tablets, liggend). Daartussen een vloeiende
            // overgang, zodat toestellen rond de grens (vouwtelefoons) niet verspringen. Zo heeft elk scherm
            // ongeveer de hoogte van een telefoon (844) en past de inhoud boven de golf.
            float verhouding = (float)Screen.width / Screen.height;
            schaler.matchWidthOrHeight = Mathf.InverseLerp(SchaalBreedteTot, SchaalHoogteVanaf, verhouding);
            Rect s = Screen.safeArea;
            veilig.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
            veilig.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
            veilig.offsetMin = veilig.offsetMax = Vector2.zero;
            // de kolom vult de veilige zone; de breedte wordt in PasKolomAan op de echte, geschaalde maat gezet
            kolom.anchorMin = Vector2.zero;
            kolom.anchorMax = Vector2.one;
            kolom.pivot = new Vector2(0.5f, 0.5f);
            kolomBreedte = -1f;
            PasKolomAan();
        }

        // Kolombreedte op de echte (geschaalde) breedte van de veilige zone. Bij het opstarten heeft Unity het canvas
        // nog niet geschaald (dan meldt het pixels i.p.v. eenheden); daarom wordt dit elk beeld gecontroleerd.
        const float SchaalBreedteTot = 0.5f, SchaalHoogteVanaf = 0.62f;
        // Op tablets wordt de kolom breder (max 580) zodat het lijkt op de telefoon; de tegels worden breder,
        // maar niet hoger dan TegelMaxHoogte, anders past de inhoud niet meer boven de golf.
        const float MaxKolom = 580f, MinZij = 6f, Binnenmarge = 14f, TegelGat = 9f, TegelMaxHoogte = 92f;
        float kolomBreedte = -1f;
        GridLayoutGroup levelRaster;
        void PasKolomAan()
        {
            if (veilig == null || kolom == null) return;
            float w = veilig.rect.width;
            if (w <= 0f || Mathf.Abs(w - kolomBreedte) < 0.5f) return;
            kolomBreedte = w;
            float zij = Mathf.Max(MinZij, (w - MaxKolom) / 2f);         // smal scherm: kleine marge; breed: kolom van 580
            kolom.offsetMin = new Vector2(zij, 0f);
            kolom.offsetMax = new Vector2(-zij, 0f);
            if (levelRaster != null)
            {
                float binnen = w - 2f * zij - 2f * Binnenmarge;
                float cel = (binnen - 2f * TegelGat) / 3f;                       // tegels vullen de breedte
                levelRaster.cellSize = new Vector2(cel, Mathf.Min(cel * 84f / 110f, TegelMaxHoogte));
                var le = levelRaster.GetComponent<LayoutElement>();
                if (le != null) le.preferredHeight = 2f * levelRaster.cellSize.y + TegelGat;
            }
        }

        // Vangnet: de golf ligt op 30% van de schermhoogte. Is de inhoud toch te hoog (bv. lange tekst of een
        // ongewoon scherm), dan zakt de golf tot net onder de puntenbalk, zodat hij niet door de knoppen loopt.
        const float GolfStandaard = 0.30f, GolfMinimum = 0.06f, GolfMarge = 22f;
        readonly System.Collections.Generic.List<IcoonAchtergrond> achtergrondLagen = new System.Collections.Generic.List<IcoonAchtergrond>();
        RectTransform voortgangRij;
        readonly Vector3[] hoeken = new Vector3[4];
        void PasGolfAan()
        {
            if (voortgangRij == null || startAchtergrond == null || achtergrondLagen.Count == 0) return;
            Rect r = startAchtergrond.rect;
            if (r.height <= 0f) return;
            voortgangRij.GetWorldCorners(hoeken);
            float onder = startAchtergrond.InverseTransformPoint(hoeken[0]).y;       // onderkant van de puntenbalk
            float uitslag = achtergrondLagen[0].golfUitslag;
            float h = Mathf.Clamp((onder - r.yMin - uitslag - GolfMarge) / r.height, GolfMinimum, GolfStandaard);
            foreach (var l in achtergrondLagen)
            {
                if (Mathf.Abs(l.golfHoogte - h) < 0.001f) continue;
                l.golfHoogte = h;
                l.SetVerticesDirty();
            }
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

        static readonly Color KnopGlas = new Color(1f, 1f, 1f, 0.12f);

        (Image img, TextMeshProUGUI t) Pilknop(Transform ouder, string tekst, Action actie, float flexW = 1f)
        {
            var img = Vlak(ouder, "Knop " + tekst, KnopGlas, 8f);
            LE(img.gameObject, flexW: flexW);
            var t = Tekst(img.transform, "Tekst", tekst, 13.5f, Color.white, TextAlignmentOptions.Center);
            Vul(t.rectTransform, 4, 0, 4, 0);
            Knop(img, actie);
            return (img, t);
        }

        // actief = oranje (zoals op het startscherm), anders licht glas; alleen wisselen als het verandert
        static void Actief(Image img, TextMeshProUGUI t, bool aan)
        {
            bool nuOranje = img.sprite != RondSprite();
            if (aan != nuOranje)
            {
                if (aan) Oranje(img, 8f);
                else { img.sprite = RondSprite(); img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 24f / 8f; img.color = KnopGlas; }
            }
            t.color = Color.white;
            t.fontStyle = aan ? FontStyles.Bold : FontStyles.Normal;
        }

        // donker waterglas voor panelen boven het 3D-kristal (wit glas wordt daar grijzig)
        Image Waterglas(Transform ouder, string naam, float radius)
        {
            var kleur = Lin(Hex("064A63")); kleur.a = 0.74f;
            var img = Vlak(ouder, naam, kleur, radius);
            var r = Maak("Rand", img.transform).gameObject.AddComponent<Image>();
            r.sprite = RandSprite(); r.type = Image.Type.Sliced; r.pixelsPerUnitMultiplier = 24f / radius;
            r.color = new Color(1f, 1f, 1f, 0.28f); r.raycastTarget = false;
            Vul(r.rectTransform);
            r.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return img;
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
            LE(taal.gameObject, prefW: 62f);
            var tt = Tekst(taal.transform, "Tekst", Teksten.T("taal"), 14f, Color.white, TextAlignmentOptions.Center);
            Vul(tt.rectTransform, 0, 0, 16, 0);
            // klein driehoekje: hier valt iets te kiezen
            var taalPijl = Icoon(taal.transform, "Pijl", PijlOmlaag(), new Color(1f, 1f, 1f, 0.85f));
            Hoek(taalPijl.rectTransform, new Vector2(1f, 0.5f), new Vector2(-9f, -1f), new Vector2(11f, 11f));
            Knop(taal, () => Teksten.Stel(Teksten.Huidig == Taal.NL ? Taal.EN : Taal.NL));
            var lijn = Vlak(startPagina, "Lijn", new Color(1f, 1f, 1f, 0.35f));
            LE(lijn.gameObject, prefH: 1f);

            // levels: 3 × 2 tegels
            var raster = Maak("Levels", startPagina);
            var g = raster.gameObject.AddComponent<GridLayoutGroup>();
            levelRaster = g;
            g.cellSize = new Vector2(110f, 84f);
            g.spacing = new Vector2(9f, 9f);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            g.childAlignment = TextAnchor.UpperCenter;
            LE(raster.gameObject, prefH: 84f * 2 + 9f);
            for (int i = 0; i < Levels.Length; i++) LevelTegel(raster, i);

            // leagues: glazen strook, de gekozen league oranje (niet in de verkenversie)
            if (!verkennen) Leagues();

            void Leagues()
            {
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
            if (!verkennen) Voortgang(startPagina);

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
            voortgangRij = (RectTransform)rij.transform;
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
            var balk = Waterglas(simPagina, "Bovenbalk", 10f);
            LE(balk.gameObject, prefH: 44f, minH: 44f);
            var bh = balk.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh.padding = new RectOffset(10, 10, 6, 6); bh.spacing = 10;
            bh.childControlWidth = bh.childControlHeight = true; bh.childForceExpandWidth = false; bh.childForceExpandHeight = true;
            bh.childAlignment = TextAnchor.MiddleLeft;
            var terug = Vlak(balk.transform, "Terug", Color.white, 6f);
            LE(terug.gameObject, prefW: 38f);
            var tp = Icoon(terug.transform, "Pijl", PijlLinks(), Lin(LaguneHex));
            Vul(tp.rectTransform, 11, 7, 11, 7);
            Knop(terug, () => ToonPagina(Pagina.Start));
            var titel = Tekst(balk.transform, "Titel", $"<b>{level + 1}</b> · {Teksten.T($"level.{level + 1}.titel")}", 15f, Color.white);
            LE(titel.gameObject, flexW: 1f);
            if (!verkennen) Sterpil(balk.transform);

            // vrije ruimte in het midden: daar kijk en vlieg je rond
            var vrij = Maak("Vrij", simPagina);
            LE(vrij.gameObject, flexH: 1f);
            if (MolecuulLevel) { BouwMolecuulpaneel(); return; }
            BouwKristalpaneel();
        }

        void Sterpil(Transform balk)
        {
            var sterPil = Vlak(balk, "Sterren", Color.white, 8f);
            Oranje(sterPil, 8f);
            LE(sterPil.gameObject, prefW: 58f);
            var sh = sterPil.gameObject.AddComponent<HorizontalLayoutGroup>();
            sh.childAlignment = TextAnchor.MiddleCenter; sh.spacing = 4; sh.childControlWidth = sh.childControlHeight = false;
            sh.childForceExpandWidth = sh.childForceExpandHeight = false;
            Icoon(sterPil.transform, "Ster", Ster(), Color.white).rectTransform.sizeDelta = new Vector2(12f, 12f);
            var sa = Tekst(sterPil.transform, "Aantal", PlayerPrefs.GetInt("watersim.sterren", 0).ToString(), 12f, Color.white, TextAlignmentOptions.Center, true);
            sa.rectTransform.sizeDelta = new Vector2(sa.preferredWidth + 2f, 18f);
        }

        // onderpaneel voor level 1 en 2: feiten over het molecuul en drie weergaveknoppen
        void BouwMolecuulpaneel()
        {
            var paneel = Waterglas(simPagina, "Bediening", 14f);
            var pv = Kolom((RectTransform)paneel.transform, 12, 8);
            pv.childAlignment = TextAnchor.LowerCenter;
            string G(double v, string f) => Teksten.Getal(v, f);
            string feiten = Levels[level].scene == 1
                ? string.Format(Teksten.T("level.1.feiten"), G(WaterSim.Sim.Constanten.HoekHOH, "0.0"), G(WaterSim.Sim.Constanten.AfstandOH, "0.00"))
                : string.Format(Teksten.T("level.2.feiten"), G(WaterSim.Sim.Constanten.AfstandOO - WaterSim.Sim.Constanten.AfstandOH, "0.00"),
                                G(WaterSim.Sim.Constanten.AfstandOO, "0.00"), G(WaterSim.Sim.Constanten.AfstandOH, "0.00"));
            Tekst(paneel.transform, "Feiten", feiten, 13.5f, IJsTekst, TextAlignmentOptions.TopLeft, false, true);
            var r = Rij(paneel.transform, "Weergave", 34f, 6f);
            var kal = Pilknop(r.transform, Teksten.T("kalotten"), () => molecuul.kalotten = !molecuul.kalotten);
            schakelaars.Add((kal.img, kal.t, () => molecuul.kalotten));
            var lad = Pilknop(r.transform, Teksten.T("ladingen"), () => molecuul.toonLadingen = !molecuul.toonLadingen);
            schakelaars.Add((lad.img, lad.t, () => molecuul.toonLadingen));
            var bw = Pilknop(r.transform, Teksten.T("beweging"), () => molecuul.beweging = !molecuul.beweging);
            schakelaars.Add((bw.img, bw.t, () => molecuul.beweging));
            var noot = Tekst(paneel.transform, "Model", Teksten.T("model.molecuul"), 10.5f, new Color(IJsTekst.r, IJsTekst.g, IJsTekst.b, 0.7f), TextAlignmentOptions.Left, false, true);
            LE(noot.gameObject, prefH: 28f);
        }

        void BouwKristalpaneel()
        {

            // onderpaneel
            var paneel = Waterglas(simPagina, "Bediening", 14f);
            var pv = Kolom((RectTransform)paneel.transform, 12, 8);
            pv.childAlignment = TextAnchor.LowerCenter;

            // regel 1: ring met de temperatuur · plateaulabel + opwarmkromme + regel · uitklappen
            var r1 = Rij(paneel.transform, "Toestand", 96f, 12f);
            var ringRt = Maak("Ring", r1.transform);
            LE(ringRt.gameObject, prefW: 96f);
            ringRt.gameObject.AddComponent<CanvasRenderer>();
            ring = ringRt.gameObject.AddComponent<Voortgangsring>();
            ring.raycastTarget = false; ring.dikte = 7f; ring.achter = new Color(1f, 1f, 1f, 0.16f);
            tTemp = Tekst(ringRt, "Temperatuur", "", 20f, Color.white, TextAlignmentOptions.Center, true);
            Vul(tTemp.rectTransform, 0, 8, 0, 0);
            tRingSub = Tekst(ringRt, "Onder", "", 11.5f, Lin(KrommeTekst), TextAlignmentOptions.Center);
            Vul(tRingSub.rectTransform, 0, 0, 0, 0); tRingSub.rectTransform.anchoredPosition = new Vector2(0f, -17f);

            var rechts = Maak("Toestand", r1.transform);
            LE(rechts.gameObject, flexW: 1f);
            labelPil = Vlak(rechts, "Plateau", Color.white, 6f);
            labelPil.raycastTarget = false;
            Hoek(labelPil.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(90f, 19f));
            tLabel = Tekst(labelPil.transform, "Tekst", "", 11.5f, Lin(PilTekst), TextAlignmentOptions.Center, true);
            Vul(tLabel.rectTransform);
            tFase = Tekst(rechts, "Fase", "", 15f, Color.white, TextAlignmentOptions.TopLeft, true);
            Hoek(tFase.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(160f, 20f));
            var kr = Maak("Kromme", rechts);
            kr.anchorMin = new Vector2(0f, 0f); kr.anchorMax = new Vector2(1f, 1f);
            kr.offsetMin = new Vector2(0f, 17f); kr.offsetMax = new Vector2(0f, -22f);
            kr.gameObject.AddComponent<CanvasRenderer>();
            kromme = kr.gameObject.AddComponent<Opwarmkromme>();
            kromme.raycastTarget = false;
            TextMeshProUGUI KLabel(string naam)
            {
                var t = Tekst(kr, naam, "", 9f, Lin(KrommeTekst), TextAlignmentOptions.Bottom);
                t.rectTransform.sizeDelta = new Vector2(70f, 11f);
                return t;
            }
            kromme.lNul = KLabel("Nul"); kromme.lIjs = KLabel("IJs"); kromme.lMengsel = KLabel("Mengsel"); kromme.lWater = KLabel("Water");
            tRegel = Tekst(rechts, "Regel", "", 12.5f, Lin(KrommeTekst), TextAlignmentOptions.BottomLeft);
            Hoek(tRegel.rectTransform, Vector2.zero, Vector2.zero, new Vector2(220f, 16f));

            var ukHouder = Maak("UitklapHouder", r1.transform);
            LE(ukHouder.gameObject, prefW: 40f);
            var uk = Vlak(ukHouder, "Uitklappen", KnopGlas, 8f);
            Hoek(uk.rectTransform, new Vector2(1f, 1f), Vector2.zero, new Vector2(40f, 38f));
            uitklapIcoon = Icoon(uk.transform, "Pijl", uitgeklapt ? PijlOmlaag() : PijlOmhoog(), Color.white);
            Vul(uitklapIcoon.rectTransform, 11, 13, 11, 13);
            Knop(uk, () => { uitgeklapt = !uitgeklapt; uitklap.gameObject.SetActive(uitgeklapt); uitklapIcoon.sprite = uitgeklapt ? PijlOmlaag() : PijlOmhoog(); });

            // regel 2: H-bruggen en dichtheid (met label bij het dichtheidsmaximum)
            var rRho = Rij(paneel.transform, "Dichtheid", 22f, 12f);
            tBruggen = Tekst(rRho.transform, "Bruggen", "", 12.5f, Lin(KrommeTekst));
            tRho = Tekst(rRho.transform, "Rho", "", 12.5f, Lin(KrommeTekst));
            maxPil = Vlak(rRho.transform, "Max", Color.white, 10f);
            Oranje(maxPil, 10f);
            maxPil.raycastTarget = false;
            var maxRij = maxPil.gameObject.AddComponent<HorizontalLayoutGroup>();
            maxRij.padding = new RectOffset(8, 9, 3, 3); maxRij.spacing = 4f; maxRij.childAlignment = TextAnchor.MiddleCenter;
            maxRij.childControlWidth = maxRij.childControlHeight = true; maxRij.childForceExpandWidth = maxRij.childForceExpandHeight = false;
            var maxSter = Icoon(maxPil.transform, "Ster", Ster(), Color.white);
            LE(maxSter.gameObject, prefW: 11f, prefH: 11f);
            tMax = Tekst(maxPil.transform, "Tekst", Teksten.T("rho.max"), 11.5f, Color.white, TextAlignmentOptions.Center, true);
            maxPil.gameObject.SetActive(false);
            var rhoVrij = Maak("Ruimte", rRho.transform);
            LE(rhoVrij.gameObject, flexW: 1f);

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
            var kal = Pilknop(r4.transform, Teksten.T("kalotten"), WisselKalotten);
            schakelaars.Add((kal.img, kal.t, () => kristal.kalotten));
            var hb = Pilknop(r4.transform, Teksten.T("hbruggen"), () => kristal.toonBruggen = !kristal.toonBruggen);
            schakelaars.Add((hb.img, hb.t, () => kristal.toonBruggen));
            var bw = Pilknop(r4.transform, Teksten.T("beweging"), () => kristal.beweging = !kristal.beweging);
            schakelaars.Add((bw.img, bw.t, () => kristal.beweging));

            var noot = Tekst(uitklap, "Model", Teksten.T("model"), 10.5f, new Color(IJsTekst.r, IJsTekst.g, IJsTekst.b, 0.7f), TextAlignmentOptions.Left, false, true);
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

            // spoor: ijsblauw (koud) → wit (0 °C) → oranje (warm); wit ligt precies op 0 °C
            var spoor = go.transform.Find("Background").GetComponent<Image>();
            spoor.sprite = Verloop3(Hex("4AB8FF"), Color.white, OranjeR, Mathf.InverseLerp(-100f, 99f, 0f));
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

            // streepje bij 4 °C (grootste dichtheid van water); "max" staat in de statusregel (onder de duim zou het wegvallen)
            float f4 = Mathf.InverseLerp(s.minValue, s.maxValue, (float)WaterSim.Sim.Constanten.TemperatuurMaxDichtheid);
            var vier = Vlak(go.transform, "Vier", new Color(1f, 1f, 1f, 0.75f));
            vier.raycastTarget = false;
            vier.rectTransform.anchorMin = vier.rectTransform.anchorMax = new Vector2(f4, 0.5f);
            vier.rectTransform.sizeDelta = new Vector2(1.5f, 12f);
            vier.transform.SetSiblingIndex(2);

            var duim = s.handleRect.GetComponent<Image>();
            duim.sprite = Cirkel();
            duim.color = Color.white;
            s.handleRect.sizeDelta = new Vector2(24f, 0f);
            var duimRand = Maak("Rand", s.handleRect).gameObject.AddComponent<Image>();   // dun lagune-randje, zodat de witte duim op het witte midden zichtbaar blijft
            duimRand.sprite = RandSprite(); duimRand.type = Image.Type.Sliced; duimRand.pixelsPerUnitMultiplier = 2f;
            duimRand.color = Lin(LaguneHex); duimRand.raycastTarget = false;
            Vul(duimRand.rectTransform);
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
            kristal.enabled = p == Pagina.Sim && !MolecuulLevel;
            if (molecuul != null) molecuul.enabled = p == Pagina.Sim && MolecuulLevel;
            if (p == Pagina.Start)
            {
                if (vliegcamera != null) vliegcamera.enabled = false;
            }
            else
            {
                var info = Levels[level];
                if (info.scene != 0)
                {
                    // level 1 en 2: dichtbij om één of twee moleculen heen draaien
                    molecuul.soort = info.scene == 1 ? Molecuulscene.Soort.Molecuul : Molecuulscene.Soort.Paar;
                    if (vliegcamera != null)
                    {
                        vliegcamera.minAfstand = 2.5f; vliegcamera.maxAfstand = 25f;
                        vliegcamera.DraaiOm = true;
                        vliegcamera.Zet(info.scene == 1 ? new Vector3(0f, 0.4f, 5f) : new Vector3(0f, 1.6f, 8f), Vector3.zero);
                        vliegcamera.enabled = true;
                    }
                    Bouw();
                    return;
                }
                kristal.Herstart(info.startT, info.oneindig);
                if (vliegcamera != null)
                {
                    vliegcamera.minAfstand = 10f; vliegcamera.maxAfstand = 160f;
                    vliegcamera.DraaiOm = !info.oneindig;
                    if (info.oneindig) vliegcamera.Zet(Vector3.zero, new Vector3(0f, 0f, 1f));
                    else vliegcamera.Zet(kristal.BlokKijkpunt, Vector3.zero);
                    vliegcamera.enabled = true;
                }
                // titel en knoppen horen bij het gekozen level
                Bouw();
            }
        }

        // Kalotten bekijk je het best van een afstand: zet het losse blok in beeld; daarna weer terug naar waar je was.
        bool kalottenZetteBlok;
        Vector3 kalottenTerugPos, kalottenTerugKijk;
        void WisselKalotten()
        {
            kristal.kalotten = !kristal.kalotten;
            if (vliegcamera == null) return;
            var cam = vliegcamera.transform;
            if (kristal.kalotten)
            {
                kalottenTerugPos = cam.position; kalottenTerugKijk = cam.position + cam.forward;
                kalottenZetteBlok = kristal.oneindig;
                if (kristal.oneindig) kristal.oneindig = false;
                vliegcamera.DraaiOm = true;
                vliegcamera.Zet(kristal.BlokKijkpunt, Vector3.zero);
            }
            else if (kalottenZetteBlok)
            {
                kristal.oneindig = true;
                vliegcamera.DraaiOm = false;
                vliegcamera.Zet(kalottenTerugPos, kalottenTerugKijk);
                kalottenZetteBlok = false;
            }
        }

        void WisselRuimte()
        {
            kristal.oneindig = !kristal.oneindig;
            if (vliegcamera == null) return;
            vliegcamera.DraaiOm = !kristal.oneindig;
            if (kristal.oneindig) vliegcamera.Zet(Vector3.zero, new Vector3(0, 0, 1));
            else vliegcamera.Zet(kristal.BlokKijkpunt, Vector3.zero);
        }

        // ---------- elk beeld ----------
        void Update()
        {
            if (Screen.safeArea != laatsteSafe || Screen.width != (int)laatsteScherm.x || Screen.height != (int)laatsteScherm.y) PasSchermAan();
            PasKolomAan();
            if (pagina == Pagina.Start) PasGolfAan();
            if (kristal == null || kristal.Model == null) return;

            if (pagina == Pagina.Start)
            {
                // de nevel ademt heel langzaam (mist die zacht beweegt)
                if (nevel != null) nevel.color = new Color(1f, 1f, 1f, 0.27f + 0.05f * Mathf.Sin(Time.unscaledTime * 0.35f));
                return;
            }
            if (MolecuulLevel)
            {
                foreach (var k in schakelaars) Actief(k.img, k.t, k.aan());
                return;
            }

            if ((statusTimer += Time.deltaTime) > 0.12f)
            {
                statusTimer = 0f;
                var m = kristal.Model;
                double T = m.Fase.Temperatuur;
                float omg = kristal.temperatuurOmgeving;
                bool opPlateau = m.Fase.IsAanHetSmelten;
                int richting = opPlateau ? (omg > 0f ? 1 : omg < 0f ? -1 : 0) : (omg > T + 0.05 ? 1 : omg < T - 0.05 ? -1 : 0);
                statusRichting = richting; statusPlateau = opPlateau;
                int procWater = Mathf.RoundToInt((float)m.Fase.Gesmolten * 100f);
                tTemp.text = Teksten.Graden(T, 1);

                if (opPlateau)
                {
                    tRingSub.text = richting < 0 ? string.Format(Teksten.T("ring.ijs"), 100 - procWater) : $"{procWater}%";
                    tLabel.text = Teksten.T(richting > 0 ? "plateau.smelt" : richting < 0 ? "plateau.stol" : "evenwicht");
                    labelPil.rectTransform.sizeDelta = new Vector2(tLabel.preferredWidth + 16f, 19f);
                    double rest = m.Fase.ResterendeTijd(omg);
                    tRegel.text = richting == 0 ? Teksten.T("regel.evenwicht")
                        : $"<b><color=#FFFFFF>{Teksten.T(richting > 0 ? "regel.smelten" : "regel.stollen")}</color></b> · {string.Format(Teksten.T("nog"), Duur(rest))}";
                }
                else
                {
                    tRingSub.text = "";
                    tFase.text = Teksten.T(m.Fase.Gesmolten <= 0 ? "fase.IJs" : "fase.Water");
                    tRegel.text = Teksten.T(richting > 0 ? "regel.warmer" : richting < 0 ? "regel.kouder" : "regel.gelijk");
                }
                labelPil.gameObject.SetActive(opPlateau);
                tFase.gameObject.SetActive(!opPlateau);
                kromme.lIjs.text = Teksten.T("fase.ijs");
                kromme.lWater.text = Teksten.T("fase.water");
                kromme.lMengsel.text = Teksten.T(richting < 0 ? "fase.mengsel.stol" : "fase.mengsel");
                kromme.lNul.text = Teksten.Graden(0, 0);

                bool water = m.Fase.Gesmolten >= 1;
                tBruggen.text = $"{Teksten.Getal(m.BruggenPerMolecuul, "0.0")} {Teksten.T("hbruggen")}";
                tRho.text = $"ρ <b><color=#FFFFFF>{Teksten.Getal(m.Dichtheid, water ? "0.00000" : "0.000")}</color></b> g/cm³";   // water: 5 decimalen, anders zie je het maximum bij 4 °C niet
                bool max = water && Math.Abs(T - WaterSim.Sim.Constanten.TemperatuurMaxDichtheid) < 0.5;
                if (max != maxPil.gameObject.activeSelf) maxPil.gameObject.SetActive(max);
            }
            // ring en kromme elk beeld bijwerken: dan lopen ze vloeiend mee
            {
                var f = kristal.Model.Fase;
                var accent = Lin(statusRichting > 0 ? AccentWarm : statusRichting < 0 ? AccentKoud : AccentRust);
                float deel = !statusPlateau ? 0f : statusRichting < 0 ? 1f - (float)f.Gesmolten : (float)f.Gesmolten;
                ring.Zet(deel, accent);
                labelPil.color = accent;
                kromme.Zet((float)f.Enthalpie, (float)f.Temperatuur, statusRichting, statusPlateau, accent);
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

        int statusRichting; bool statusPlateau;

        // resterende tijd als "6 s" of "2 min"
        static string Duur(double s)
        {
            if (double.IsInfinity(s) || double.IsNaN(s)) return "–";
            if (s < 59.5) return $"{Math.Max(1, (int)Math.Round(s))} s";
            return $"{(int)Math.Round(s / 60.0)} min";
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
