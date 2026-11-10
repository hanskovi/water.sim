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
            public int scene;                // 0 = kristal met temperatuur, 1 = één molecuul, 2 = twee moleculen met H-brug, 3 = ijsstructuur, 4 = glas en meer
            public LevelInfo(bool s, bool o = true, float t = -10f, int sc = 0) { speelbaar = s; oneindig = o; startT = t; scene = sc; }
        }
        static readonly LevelInfo[] Levels =
        {
            new LevelInfo(true, sc: 1), new LevelInfo(true, sc: 2),
            new LevelInfo(true, true, -10f, 3), new LevelInfo(true, true, -10f), new LevelInfo(true, sc: 4),
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
        bool MolecuulLevel => Levels[level].scene == 1 || Levels[level].scene == 2;
        bool IjsLevel => Levels[level].scene == 3;
        bool MacroLevel => Levels[level].scene == 4;
        RectTransform vrijRt;
        int pasCameraAan;              // over zoveel beelden de camera-afstand aanpassen aan het molecuul
        bool laatsteKalotten;
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
            snel.Clear(); schakelaars.Clear(); infoKnoppen.Clear();
            schuif = null; ring = null; kromme = null;      // alleen level 4 bouwt deze opnieuw
            zoomHouder = null; glasW = null; meerW = null; poolW = null; luchtSchuif = null;

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
            if (kristal != null && schuif != null) schuif.SetValueWithoutNotify(kristal.temperatuurOmgeving);   // level 1-3 hebben geen schuif
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
            if (veilig == null || kolom == null || schaler == null || Screen.width <= 0 || Screen.height <= 0) return;
            // breedte van de veilige zone in canvas-eenheden, zelf uitgerekend zoals de CanvasScaler dat doet
            // (logaritmisch mengen van breedte en hoogte). Unity schaalt het canvas pas een beeld later; met
            // veilig.rect zou het eerste beeld in pixels rekenen en de kolom even verkeerd staan.
            var r = schaler.referenceResolution;
            float schaal = Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(Screen.width / r.x, 2f), Mathf.Log(Screen.height / r.y, 2f), schaler.matchWidthOrHeight));
            float w = Screen.safeArea.width / schaal;
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
            // vorige / volgende model, zonder terug naar het startscherm
            for (int i = 0; i < 2; i++)
            {
                int stap = i == 0 ? -1 : 1;
                var k = Vlak(balk.transform, i == 0 ? "Vorige" : "Volgende", KnopGlas, 8f);
                LE(k.gameObject, prefW: 34f);
                var pijl = Icoon(k.transform, "Pijl", i == 0 ? PijlLinks() : PijlRechts(), Color.white);
                Vul(pijl.rectTransform, 9, 9, 9, 9);
                Knop(k, () => WisselLevel(stap));
            }
            if (!verkennen) Sterpil(balk.transform);

            // vrije ruimte in het midden: daar kijk en vlieg je rond
            var vrij = Maak("Vrij", simPagina);
            LE(vrij.gameObject, flexH: 1f);
            vrijRt = vrij;
            if (MacroLevel) BouwMacro(vrij); else Zoomknoppen(vrij);
            Formulekaart(vrij);
            Infovak(vrij);
            if (MacroLevel) { BouwMacropaneel(); return; }
            if (MolecuulLevel) { BouwMolecuulpaneel(); return; }
            if (IjsLevel) { BouwIjspaneel(); return; }
            BouwKristalpaneel();
        }

        // ---------- formulekaartje: de structuurformule of vergelijking bij elk level ----------
        bool toonFormule = true;
        Image formuleKaart, formuleDicht;
        string FormuleSleutel => "watersim.formule." + level;      // per level onthouden: open of ingeklapt
        void ZetFormule(bool open) { toonFormule = open; PlayerPrefs.SetInt(FormuleSleutel, open ? 1 : 0); }
        TextMeshProUGUI tF4a, tF4c, tF4b;      // links · rechts van de pijl, en ΔH
        Formuletekening formuleTek;
        int formuleRichting = 99;

        void Formulekaart(RectTransform vrij)
        {
            toonFormule = PlayerPrefs.GetInt(FormuleSleutel, 1) == 1;
            // ingeklapt: klein knopje "Formule ▾" op dezelfde plek
            formuleDicht = Waterglas(vrij, "Formule dicht", 10f);
            Hoek(formuleDicht.rectTransform, new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(104f, 32f));
            var fdt = Tekst(formuleDicht.transform, "Tekst", Teksten.T("formule"), 13f, Color.white, TextAlignmentOptions.Left);
            Vul(fdt.rectTransform, 12, 0, 26, 0);
            var fdp = Icoon(formuleDicht.transform, "Pijl", PijlOmlaag(), Color.white);
            Hoek(fdp.rectTransform, new Vector2(1f, 0.5f), new Vector2(-9f, 0f), new Vector2(12f, 12f));
            Knop(formuleDicht, () => ZetFormule(true));

            formuleKaart = Waterglas(vrij, "Formulekaart", 12f);
            Hoek(formuleKaart.rectTransform, new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(196f, 126f));
            // inklappen: driehoekje rechtsboven in het kaartje (met ruim tikvlak)
            var klap = Vlak(formuleKaart.transform, "Inklappen", new Color(1f, 1f, 1f, 0.001f));
            Hoek(klap.rectTransform, new Vector2(1f, 1f), Vector2.zero, new Vector2(36f, 32f));
            var klapPijl = Icoon(klap.transform, "Pijl", PijlOmhoog(), new Color(1f, 1f, 1f, 0.85f));
            Hoek(klapPijl.rectTransform, new Vector2(1f, 1f), new Vector2(-9f, -9f), new Vector2(12f, 12f));
            Knop(klap, () => ZetFormule(false));
            var vlak = Maak("Tekening", formuleKaart.transform);
            Hoek(vlak, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 110f));
            vlak.pivot = new Vector2(0.5f, 0.5f); vlak.anchoredPosition = Vector2.zero;
            vlak.gameObject.AddComponent<CanvasRenderer>();
            var tek = vlak.gameObject.AddComponent<Formuletekening>();
            tek.raycastTarget = false;
            formuleKaart.raycastTarget = false;

            Color wit = Color.white, blauw = Lin(Hex("5BC2FF")), licht = Lin(Hex("9FD8FF")), rood = Lin(Hex("E05A4A"));
            Color dMin = Lin(Hex("FFA99B")), dPlus = Lin(Hex("C7EBFF")), oranje = Lin(Hex("FFB48A")), grijs = Lin(KrommeTekst);
            TextMeshProUGUI L(string tekst, float x, float y, float grootte, Color kleur, bool vet = false)
            {
                var t = Tekst(vlak, "T", tekst, grootte, kleur, TextAlignmentOptions.Center, vet);
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0f, 1f);
                t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                t.rectTransform.sizeDelta = new Vector2(176f, grootte + 6f);
                t.rectTransform.anchoredPosition = new Vector2(x, -y);
                return t;
            }
            string G(double v, string f) => Teksten.Getal(v, f);
            switch (level)                 // level 1 t/m 5 (index 0 t/m 4)
            {
                case 0:   // lewisstructuur van H2O
                    L("O", 90, 44, 20, wit); L("H", 40, 90, 20, wit); L("H", 140, 90, 20, wit);
                    tek.Streep(new Vector2(81, 53), new Vector2(49, 81), wit); tek.Streep(new Vector2(99, 53), new Vector2(131, 81), wit);
                    foreach (var p in new[] { new Vector2(77, 25), new Vector2(83, 21), new Vector2(97, 21), new Vector2(103, 25) }) tek.Punt(p, 2f, wit);
                    tek.Hoekboog(new Vector2(90, 46), 20, 42, 138, licht);
                    L(G(WaterSim.Sim.Constanten.HoekHOH, "0.0") + "°", 90, 78, 11, licht);
                    L("δ−", 124, 30, 12, dMin); L("δ+", 20, 102, 12, dPlus); L("δ+", 160, 102, 12, dPlus);
                    break;
                case 1:   // twee moleculen met de waterstofbrug
                    L("O", 34, 56, 17, wit); L("H", 72, 56, 17, wit); L("H", 20, 92, 17, wit);
                    L("O", 136, 56, 17, wit); L("H", 160, 24, 17, wit); L("H", 160, 92, 17, wit);
                    tek.Streep(new Vector2(43, 56), new Vector2(64, 56), wit); tek.Streep(new Vector2(30, 65), new Vector2(23, 84), wit);
                    tek.Streep(new Vector2(143, 49), new Vector2(154, 32), wit); tek.Streep(new Vector2(143, 63), new Vector2(154, 84), wit);
                    tek.Streep(new Vector2(82, 56), new Vector2(126, 56), blauw, 3.5f, 6f);
                    L(G(WaterSim.Sim.Constanten.AfstandOO - WaterSim.Sim.Constanten.AfstandOH, "0.00") + " Å", 104, 44, 10, licht);
                    L("O–H···O ≈ 180°", 92, 80, 10, licht);
                    break;
                case 2:   // zeshoek uit ijs Ih
                    {
                        var hoek = new Vector2[6];
                        var midden = new Vector2(90, 55);
                        for (int i = 0; i < 6; i++) { float a = (60f * i - 90f) * Mathf.Deg2Rad; hoek[i] = midden + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 34f; }
                        for (int i = 0; i < 6; i++)
                        {
                            // elke O heeft 2 H: één in de ring (naar de volgende O, met de H-brug gestippeld) en één naar buiten
                            var a = hoek[i]; var b = hoek[(i + 1) % 6]; var h = Vector2.Lerp(a, b, 0.32f);
                            tek.Streep(a, h, wit); tek.Streep(h, b, blauw, 2.5f, 4.5f); tek.Punt(h, 3.5f, wit);
                            var buiten = a + (a - midden).normalized * 12f;
                            tek.Streep(a, buiten, wit); tek.Punt(buiten, 3.5f, wit);
                        }
                        foreach (var o in hoek) { tek.Punt(o, 7f, rood); L("O", o.x, o.y, 9, wit, true); }
                        L(Teksten.T("kanaal"), 90, 55, 9, licht);
                    }
                    break;
                case 3:   // smelten / stollen als vergelijking (pijl en teken volgen de richting)
                    tF4a = L("", 45, 30, 17, wit);
                    tF4c = L("", 135, 30, 17, wit);
                    formuleTek = tek;
                    tF4b = L("", 90, 58, 13, oranje);
                    L(string.Format(Teksten.T("formule.4.onder"), G(WaterSim.Sim.Constanten.Smeltwarmte, "0")), 90, 86, 11, grijs);
                    formuleRichting = 99;
                    break;
                case 4:   // dichtheid
                    L("ρ = m / V", 90, 32, 19, wit);
                    L(string.Format(Teksten.T("formule.5"), G(WaterSim.Sim.Constanten.DichtheidIjsBinas, "0.000"), G(WaterSim.Sim.Constanten.DichtheidWater(0), "0.000")), 90, 62, 13, wit);
                    L(Teksten.T("formule.5.onder"), 90, 88, 11, grijs);
                    break;
                default:
                    formuleKaart.gameObject.SetActive(false); formuleDicht.gameObject.SetActive(false);
                    formuleKaart = null;
                    return;
            }
            formuleKaart.gameObject.SetActive(toonFormule);
            formuleDicht.gameObject.SetActive(!toonFormule);
        }

        // vergelijking bij level 4: smelten (oranje, ΔH +) of stollen (blauw, ΔH −)
        void WerkFormuleBij()
        {
            if (tF4a == null || formuleRichting == statusRichting) return;
            formuleRichting = statusRichting;
            bool stol = statusRichting < 0;
            double dh = WaterSim.Sim.Constanten.Smeltwarmte * WaterSim.Sim.Constanten.MolaireMassaWater / 1000.0;
            // de pijl zelf tekenen (het →-teken zit niet in het lettertype)
            var kleur = Lin(Hex(stol ? "5BC2FF" : "F28A55"));
            formuleTek.Wis();
            formuleTek.Streep(new Vector2(77, 30), new Vector2(103, 30), kleur, 2.2f);
            formuleTek.Streep(new Vector2(97, 25), new Vector2(103, 30), kleur, 2.2f);
            formuleTek.Streep(new Vector2(97, 35), new Vector2(103, 30), kleur, 2.2f);
            tF4a.text = stol ? "H<sub>2</sub>O(l)" : "H<sub>2</sub>O(s)";
            tF4c.text = stol ? "H<sub>2</sub>O(s)" : "H<sub>2</sub>O(l)";
            tF4b.text = $"ΔH = {(stol ? "−" : "+")}{Teksten.Getal(dh, "0.0")} kJ/mol";
            tF4b.color = Lin(Hex(stol ? "9FD8FF" : "FFB48A"));
        }

        // ---------- uitleg: korte beschrijving in het paneel, meer wetenschap in het infovak ----------

        // "wat je ziet", zonder getallen, met rechts het infobolletje
        void Beschrijving(Image paneel, string sleutel)
        {
            var t = Tekst(paneel.transform, "Beschrijving", Teksten.T(sleutel), 13.5f, IJsTekst, TextAlignmentOptions.TopLeft, false, true);
            t.margin = new Vector4(0f, 0f, 40f, 0f);
            var ik = Infoknop(paneel.transform);
            ik.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Hoek(ik.rectTransform, new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(32f, 32f));
        }

        // rond "i"-knopje dat het infovak opent en sluit
        Image Infoknop(Transform ouder)
        {
            var k = Vlak(ouder, "Info", Color.white, 16f);
            k.sprite = Cirkel(); k.type = Image.Type.Simple; k.color = new Color(1f, 1f, 1f, 0.16f);
            var rand = Maak("Rand", k.transform).gameObject.AddComponent<Image>();
            rand.sprite = Cirkel(); rand.color = new Color(1f, 1f, 1f, 0.0f); rand.raycastTarget = false;
            var t = Tekst(k.transform, "i", "i", 18f, Color.white, TextAlignmentOptions.Center, true);
            Vul(t.rectTransform, 0, 1, 0, 0);
            Knop(k, () => { infoOpen = !infoOpen; });
            infoKnoppen.Add(k);
            return k;
        }

        bool infoOpen;
        int infoNiveau;
        Image infoVak;
        TextMeshProUGUI infoTekst;
        readonly List<(Image img, TextMeshProUGUI t, int n)> infoTabs = new List<(Image, TextMeshProUGUI, int)>();
        readonly List<Image> infoKnoppen = new List<Image>();

        // getallen voor de infoteksten (allemaal uit Constanten), zodat de teksten geen losse waarden bevatten
        static object[] InfoGetallen()
        {
            string G(double v, string f) => Teksten.Getal(v, f);
            double oh = WaterSim.Sim.Constanten.AfstandOH, oo = WaterSim.Sim.Constanten.AfstandOO;
            return new object[]
            {
                G(oh, "0.00"), G(oo, "0.00"), G(oo - oh, "0.00"),                                         // {0} O–H, {1} O–O, {2} H···O (Å)
                G(WaterSim.Sim.Constanten.HoekHOH, "0.0"), G(WaterSim.Sim.Constanten.HoekTetraeder, "0.0"),     // {3} H–O–H, {4} tetraëder (°)
                G(WaterSim.Sim.Constanten.DichtheidIjsBinas, "0.000"), G(WaterSim.Sim.Constanten.DichtheidWater(WaterSim.Sim.Constanten.TemperatuurMaxDichtheid), "0.000"),  // {5} ρ ijs, {6} ρ water max
                G(WaterSim.Sim.Constanten.Smeltwarmte, "0"),                                                    // {7} J/g
                G(WaterSim.Sim.Constanten.Smeltwarmte * WaterSim.Sim.Constanten.MolaireMassaWater / 1000.0, "0.0"),  // {8} kJ/mol
                G(WaterSim.Sim.Constanten.SoortelijkeWarmteWater, "0.00"), G(WaterSim.Sim.Constanten.SoortelijkeWarmteIjs, "0.0"),  // {9} c water, {10} c ijs
                G(oh / 10.0, "0.000"),                                                                          // {11} O–H in nm
                G(WaterSim.Sim.Constanten.MolaireMassaWater, "0.0"),                                            // {12} M
                G(WaterSim.Sim.Constanten.TemperatuurMaxDichtheid, "0"),                                        // {13} °C
                G(WaterSim.Sim.Constanten.IjsA, "0.00"), G(WaterSim.Sim.Constanten.IjsC, "0.00"),               // {14} a, {15} c (Å)
            };
        }

        // infovak boven het paneel: meer uitleg per niveau (onderbouw · bovenbouw · XPRT), half doorzichtig over het beeld
        void Infovak(RectTransform vrij)
        {
            infoTabs.Clear();
            infoNiveau = Mathf.Clamp(PlayerPrefs.GetInt("watersim.infoniveau", league), 0, 2);
            var kleur = Lin(Hex("05293A")); kleur.a = 0.9f;
            infoVak = Vlak(vrij, "Infovak", kleur, 14f);
            Vul(infoVak.rectTransform, 0f, 6f, 0f, 6f);
            var rand = Maak("Rand", infoVak.transform).gameObject.AddComponent<Image>();
            rand.sprite = RandSprite(); rand.type = Image.Type.Sliced; rand.pixelsPerUnitMultiplier = 24f / 14f;
            rand.color = new Color(1f, 1f, 1f, 0.3f); rand.raycastTarget = false;
            Vul(rand.rectTransform);
            rand.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var kv = Kolom((RectTransform)infoVak.transform, 12, 8);
            kv.childAlignment = TextAnchor.UpperLeft;

            // kop: titel en sluiten
            var kop = Rij(infoVak.transform, "Kop", 28f, 8f);
            var titel = Tekst(kop.transform, "Titel", $"<b>{Teksten.T("info.titel")}</b> · {level + 1} · {Teksten.T($"level.{level + 1}.titel")}", 14.5f, Color.white);
            LE(titel.gameObject, flexW: 1f);
            var sluit = Vlak(kop.transform, "Sluiten", KnopGlas, 8f);
            LE(sluit.gameObject, prefW: 30f);
            var x = Tekst(sluit.transform, "x", "×", 20f, Color.white, TextAlignmentOptions.Center);
            Vul(x.rectTransform, 0, 2, 0, 0);
            Knop(sluit, () => infoOpen = false);

            // niveaus
            var tabs = Rij(infoVak.transform, "Niveaus", 30f, 6f);
            for (int i = 0; i < 3; i++)
            {
                int n = i;
                var p = Pilknop(tabs.transform, Teksten.T("league." + i), () => { infoNiveau = n; PlayerPrefs.SetInt("watersim.infoniveau", n); VulInfo(); });
                p.t.fontSize = 12.5f;
                infoTabs.Add((p.img, p.t, n));
            }

            // tekst in een schuifvlak (lange uitleg past dan altijd)
            var venster = Maak("Venster", infoVak.transform);
            LE(venster.gameObject, flexH: 1f);
            venster.gameObject.AddComponent<RectMask2D>();
            var inhoud = Maak("Inhoud", venster);
            inhoud.anchorMin = new Vector2(0f, 1f); inhoud.anchorMax = new Vector2(1f, 1f); inhoud.pivot = new Vector2(0.5f, 1f);
            inhoud.offsetMin = inhoud.offsetMax = Vector2.zero;
            var iv = Kolom(inhoud, 0, 0);
            iv.childAlignment = TextAnchor.UpperLeft;
            inhoud.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            infoTekst = Tekst(inhoud, "Tekst", "", 13.5f, IJsTekst, TextAlignmentOptions.TopLeft, false, true);
            var scroll = venster.gameObject.AddComponent<ScrollRect>();
            scroll.content = inhoud; scroll.viewport = venster; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 20f;
            var vangVlak = venster.gameObject.AddComponent<Image>();      // zodat slepen op de tekst het vlak laat schuiven
            vangVlak.color = new Color(0f, 0f, 0f, 0.001f);

            VulInfo();
            infoVak.gameObject.SetActive(infoOpen);
        }

        void VulInfo()
        {
            if (infoTekst == null) return;
            infoTekst.text = string.Format(Teksten.T($"info.{level + 1}.{infoNiveau}"), InfoGetallen());
            foreach (var t in infoTabs) Actief(t.img, t.t, t.n == infoNiveau);
        }

        RectTransform zoomHouder;

        // + en − rechts in beeld: dichterbij / verder weg (ook in de editor, waar knijpen niet kan)
        void Zoomknoppen(RectTransform vrij)
        {
            var houder = Maak("Zoom", vrij);
            zoomHouder = houder;
            Hoek(houder, new Vector2(1f, 0f), new Vector2(0f, 4f), new Vector2(42f, 92f));
            for (int i = 0; i < 2; i++)
            {
                int richting = i == 0 ? 1 : -1;
                var k = Waterglas(houder, i == 0 ? "Plus" : "Min", 10f);
                Hoek(k.rectTransform, new Vector2(0.5f, i == 0 ? 1f : 0f), Vector2.zero, new Vector2(42f, 42f));
                var t = Tekst(k.transform, "Teken", i == 0 ? "+" : "−", 24f, Color.white, TextAlignmentOptions.Center, true);
                Vul(t.rectTransform, 0, 2, 0, 0);
                Knop(k, () => { if (vliegcamera != null) vliegcamera.Zoom(richting); });
            }
        }

        // level 3: alleen de structuur van ijs, zonder temperatuur
        void BouwIjspaneel()
        {
            var paneel = Waterglas(simPagina, "Bediening", 14f);
            var pv = Kolom((RectTransform)paneel.transform, 12, 8);
            pv.childAlignment = TextAnchor.LowerCenter;
            Beschrijving(paneel, "level.3.zie");
            var r = Rij(paneel.transform, "Weergave", 34f, 6f);
            var kal = Pilknop(r.transform, Teksten.T("kalotten"), WisselKalotten);
            schakelaars.Add((kal.img, kal.t, () => kristal.kalotten));
            var hb = Pilknop(r.transform, Teksten.T("hbruggen"), () => kristal.toonBruggen = !kristal.toonBruggen);
            schakelaars.Add((hb.img, hb.t, () => kristal.toonBruggen));
            var kn = Pilknop(r.transform, Teksten.T("kanalen"), () => kristal.markeerKanaal = !kristal.markeerKanaal);
            schakelaars.Add((kn.img, kn.t, () => kristal.markeerKanaal));
            var bw = Pilknop(r.transform, Teksten.T("beweging"), () => kristal.beweging = !kristal.beweging);
            schakelaars.Add((bw.img, bw.t, () => kristal.beweging));
            var noot = Tekst(paneel.transform, "Model", Teksten.T("model.ijs"), 10.5f, new Color(IJsTekst.r, IJsTekst.g, IJsTekst.b, 0.7f), TextAlignmentOptions.Left, false, true);
            LE(noot.gameObject, prefH: 28f);
        }

        // naar het vorige of volgende speelbare level (rondlopend), meteen in het simulatiescherm
        void WisselLevel(int stap)
        {
            int nieuw = level;
            for (int n = 0; n < Levels.Length; n++)
            {
                nieuw = (nieuw + stap + Levels.Length) % Levels.Length;
                if (Levels[nieuw].speelbaar) break;
            }
            if (nieuw == level) return;
            level = nieuw;
            PlayerPrefs.SetInt("watersim.level", level);
            infoOpen = false;
            if (kristal != null) kristal.kalotten = false;     // elk level begint in zijn eigen standaardweergave
            kalottenZetteBlok = false;
            ToonPagina(Pagina.Sim);
        }

        // ---------- level 5: glas en meer ----------
        WaterSim.Sim.Meer meer;
        int macroModus;              // 0 = glas, 1 = meer, 2 = poolkappen (glas en poolkappen vallen onder de tab IJsmassa)
        Poolweergave poolW;
        float gesmoltenLand, gesmoltenZee;
        bool smeltLand, smeltZee;
        int vloeistofKeuze;          // 0 = zoet water, 1 = zeewater, 2 = alcohol
        float luchtT = -5f;
        int zonStand;                              // 0 = geen zon, 1 = winterzon, 2 = voorjaarszon
        bool jaarModus, opRots, aanHetSmelten;
        float gesmolten;
        double startDag;                           // dag van het jaar bij de start (jaar-modus: 1 oktober)
        TextMeshProUGUI tSmeltKnop, tZonKnop;
        Glasweergave glasW;
        Meerweergave meerW;
        Slider luchtSchuif;
        TextMeshProUGUI tLucht, tGlasStatus;
        const float DagenPerSeconde = 1f;          // model: 1 seconde = 1 dag
        const float DagenPerSecondeJaar = 3f;      // jaar-modus: 1 seconde = 3 dagen (een jaar in ruim 2 minuten)

        static double DichtheidVloeistof(int k) => k == 1 ? WaterSim.Sim.Constanten.DichtheidZeewater
                                                 : k == 2 ? WaterSim.Sim.Constanten.DichtheidEthanol
                                                 : WaterSim.Sim.Constanten.DichtheidWater(0);

        void BouwMacro(RectTransform vrij)
        {
            macroModus = PlayerPrefs.GetInt("watersim.macro", 0);
            vloeistofKeuze = PlayerPrefs.GetInt("watersim.vloeistof", 0);
            var vlak = Maak("Scene", vrij);
            Vul(vlak, 4, 4, 4, 4);
            vlak.gameObject.AddComponent<CanvasRenderer>();
            var licht = Lin(KrommeTekst);
            if (macroModus == 0)
            {
                glasW = vlak.gameObject.AddComponent<Glasweergave>();
                glasW.raycastTarget = false;
                glasW.lBoven = Tekst(vlak, "Boven", "", 12.5f, Lin(Hex("FFB48A")), TextAlignmentOptions.Left, true);
                glasW.lOnder = Tekst(vlak, "Onder", "", 12.5f, Lin(Hex("9FD8FF")), TextAlignmentOptions.Left, true);
                foreach (var l in new[] { glasW.lBoven, glasW.lOnder }) l.rectTransform.sizeDelta = new Vector2(110f, 18f);
                // inzoomvensters links en rechts onderin
                for (int i = 0; i < 2; i++)
                {
                    var z = Maak(i == 0 ? "Inzoom ijs" : "Inzoom water", vlak);
                    Hoek(z, new Vector2(i == 0 ? 0f : 1f, 0f), new Vector2(i == 0 ? 6f : -6f, 22f), new Vector2(92f, 92f));
                    z.gameObject.AddComponent<CanvasRenderer>();
                    var g = z.gameObject.AddComponent<Inzoom>();
                    g.water = i == 1; g.raycastTarget = false;
                    g.rand = Lin(Hex(i == 0 ? "5BC2FF" : "F28A55"));
                    var lab = Tekst(z, "Naam", Teksten.T(i == 0 ? "inzoom.ijs" : "inzoom.water"), 11f, licht, TextAlignmentOptions.Top);
                    lab.rectTransform.anchorMin = new Vector2(0f, 0f); lab.rectTransform.anchorMax = new Vector2(1f, 0f);
                    lab.rectTransform.pivot = new Vector2(0.5f, 1f); lab.rectTransform.sizeDelta = new Vector2(40f, 16f);
                    lab.rectTransform.anchoredPosition = new Vector2(0f, -3f);
                }
            }
            else if (macroModus == 2)
            {
                poolW = vlak.gameObject.AddComponent<Poolweergave>();
                poolW.raycastTarget = false;
                poolW.lucht = Lin(Hex("CDEBF3")); poolW.berg = Lin(Hex("77C5D7")); poolW.sneeuw = Color.white;
                poolW.zee = Lin(Hex("1262A8")); poolW.zeeDonker = Lin(Hex("0E4F8C")); poolW.ijsZij = Lin(Hex("A8DDEB"));
                var donker = Lin(Hex("0B2230"));
                poolW.lTitelLand = Tekst(vlak, "Titel land", Teksten.T("pool.land"), 13f, donker, TextAlignmentOptions.Left, true);
                poolW.lTitelZee = Tekst(vlak, "Titel zee", Teksten.T("pool.zee"), 13f, donker, TextAlignmentOptions.Left, true);
                poolW.lLand = Tekst(vlak, "Stijging land", "", 13f, Color.white, TextAlignmentOptions.Right, true);
                poolW.lZee = Tekst(vlak, "Stijging zee", "", 13f, donker, TextAlignmentOptions.Right, true);
                foreach (var l in new[] { poolW.lTitelLand, poolW.lTitelZee, poolW.lLand, poolW.lZee }) l.rectTransform.sizeDelta = new Vector2(200f, 18f);
            }
            else
            {
                meerW = vlak.gameObject.AddComponent<Meerweergave>();
                meerW.raycastTarget = false;
                meerW.meer = meer;
                meerW.koud = Lin(Hex("9ED9F0")); meerW.vier = Lin(Hex("0E3F66")); meerW.warm = Lin(Hex("2C7F78"));
                meerW.bodem = Lin(Hex("3B2A1C")); meerW.vis = Lin(Hex("FFB48A"));
                // labels op een donker pilletje: altijd leesbaar, hoe licht of donker het water ook is
                TextMeshProUGUI Pillabel(string naam)
                {
                    var pil = Vlak(vlak, naam, new Color(0.02f, 0.07f, 0.12f, 0.72f), 9f);
                    pil.raycastTarget = false;
                    var t = Tekst(pil.transform, "Tekst", "", 11.5f, Color.white, TextAlignmentOptions.Center);
                    Vul(t.rectTransform, 7, 0, 7, 0);
                    return t;
                }
                foreach (float d in new[] { 0.5f, 2f, 4f }) meerW.diepteLabels.Add((Pillabel("Diepte"), d));
                meerW.lBodem = Pillabel("Bodem");
                meerW.lIjs = Tekst(vlak, "IJs", "", 12.5f, Color.white, TextAlignmentOptions.BottomLeft, false, true);
                meerW.lIjs.outlineWidth = 0.2f; meerW.lIjs.outlineColor = new Color32(4, 16, 26, 230);
                // lucht en datum linksboven, groot
                tLucht = Tekst(vlak, "Lucht", "", 18f, Color.white, TextAlignmentOptions.TopLeft, true);
                Hoek(tLucht.rectTransform, new Vector2(0f, 1f), new Vector2(12f, -6f), new Vector2(170f, 46f));
            }
        }

        void BouwMacropaneel()
        {
            var paneel = Waterglas(simPagina, "Bediening", 14f);
            var pv = Kolom((RectTransform)paneel.transform, 12, 8);
            pv.childAlignment = TextAnchor.LowerCenter;
            Beschrijving(paneel, macroModus == 0 ? "level.5.zie.glas" : macroModus == 2 ? "level.5.zie.pool" : "level.5.zie.meer");

            void ZetModus(int m) { macroModus = m; PlayerPrefs.SetInt("watersim.macro", m); gesmolten = 0f; aanHetSmelten = false; Bouw(); }
            var tabs = Rij(paneel.transform, "Modus", 34f, 6f);
            var tIjs = Pilknop(tabs.transform, Teksten.T("ijsmassa"), () => { if (macroModus == 1) ZetModus(PlayerPrefs.GetInt("watersim.ijsmassa", 0)); });
            schakelaars.Add((tIjs.img, tIjs.t, () => macroModus != 1));
            var tMeer = Pilknop(tabs.transform, Teksten.T("meer"), () => ZetModus(1));
            schakelaars.Add((tMeer.img, tMeer.t, () => macroModus == 1));
            if (macroModus != 1)
            {
                var sub = Rij(paneel.transform, "IJsmassa", 30f, 6f);
                for (int i = 0; i < 2; i++)
                {
                    int m = i == 0 ? 0 : 2;
                    var p = Pilknop(sub.transform, Teksten.T(i == 0 ? "glas" : "poolkappen"), () => { PlayerPrefs.SetInt("watersim.ijsmassa", m); ZetModus(m); });
                    p.t.fontSize = 12.5f;
                    schakelaars.Add((p.img, p.t, () => macroModus == m));
                }
            }
            if (macroModus == 2)
            {
                var rp = Rij(paneel.transform, "Smelten", 34f, 6f);
                var sl = Pilknop(rp.transform, Teksten.T("pool.smeltland"), () => smeltLand = true);
                sl.t.fontSize = 12.5f;
                schakelaars.Add((sl.img, sl.t, () => smeltLand || gesmoltenLand >= 1f));
                var sz = Pilknop(rp.transform, Teksten.T("pool.smeltzee"), () => smeltZee = true);
                sz.t.fontSize = 12.5f;
                schakelaars.Add((sz.img, sz.t, () => smeltZee || gesmoltenZee >= 1f));
                var op = Pilknop(rp.transform, Teksten.T("opnieuw"), () => { gesmoltenLand = gesmoltenZee = 0f; smeltLand = smeltZee = false; }, 0.7f);
                op.t.fontSize = 12.5f;
                tGlasStatus = Tekst(paneel.transform, "Status", "", 13f, IJsTekst, TextAlignmentOptions.Left, false, true);
            }
            else if (macroModus == 0)
            {
                var r = Rij(paneel.transform, "Vloeistof", 34f, 6f);
                for (int i = 0; i < 3; i++)
                {
                    int k = i;
                    var p = Pilknop(r.transform, Teksten.T("vloeistof." + i), () => { vloeistofKeuze = k; PlayerPrefs.SetInt("watersim.vloeistof", k); gesmolten = 0f; aanHetSmelten = false; });
                    p.t.fontSize = 12.5f;
                    schakelaars.Add((p.img, p.t, () => vloeistofKeuze == k));
                }
                var r2 = Rij(paneel.transform, "IJs", 34f, 6f);
                for (int i = 0; i < 2; i++)
                {
                    bool rots = i == 1;
                    var p = Pilknop(r2.transform, Teksten.T(rots ? "glas.rots" : "glas.drijvend"), () => { opRots = rots; gesmolten = 0f; aanHetSmelten = false; });
                    p.t.fontSize = 12.5f;
                    schakelaars.Add((p.img, p.t, () => opRots == rots));
                }
                var sm = Pilknop(r2.transform, "", () =>
                {
                    if (gesmolten >= 1f || aanHetSmelten) { gesmolten = 0f; aanHetSmelten = false; }
                    else aanHetSmelten = true;
                });
                sm.t.fontSize = 12.5f;
                tSmeltKnop = sm.t;
                schakelaars.Add((sm.img, sm.t, () => aanHetSmelten));
                tGlasStatus = Tekst(paneel.transform, "Status", "", 13f, IJsTekst, TextAlignmentOptions.Left, false, true);
            }
            else
            {
                var rk = Rij(paneel.transform, "Knoppen", 34f, 6f);
                var zon = Pilknop(rk.transform, "", () => zonStand = (zonStand + 1) % 3);   // uit → winterzon → voorjaarszon
                tZonKnop = zon.t;
                schakelaars.Add((zon.img, zon.t, () => zonStand > 0));
                var jaar = Pilknop(rk.transform, Teksten.T("jaar"), () =>
                {
                    jaarModus = !jaarModus;
                    meer = new WaterSim.Sim.Meer(24, 6, 12);
                    startDag = jaarModus ? 274 : 0;                  // jaar begint op 1 oktober
                    if (jaarModus && zonStand == 0) zonStand = 2;
                    if (meerW != null) meerW.meer = meer;
                });
                schakelaars.Add((jaar.img, jaar.t, () => jaarModus));
                var opn = Pilknop(rk.transform, Teksten.T("opnieuw"), () => { meer = new WaterSim.Sim.Meer(24, 6, 12); if (meerW != null) meerW.meer = meer; });

                var r = Rij(paneel.transform, "Lucht", 34f, 10f);
                var lab = Tekst(r.transform, "Label", Teksten.T("lucht"), 13.5f, Color.white);
                LE(lab.gameObject, prefW: 52f);
                var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
                go.name = "Luchtschuif"; go.transform.SetParent(r.transform, false);
                LE(go, flexW: 1f);
                luchtSchuif = go.GetComponent<Slider>();
                luchtSchuif.minValue = -15f; luchtSchuif.maxValue = 25f; luchtSchuif.wholeNumbers = true;
                luchtSchuif.navigation = new Navigation { mode = Navigation.Mode.None };
                var spoor = go.transform.Find("Background").GetComponent<Image>();
                spoor.sprite = Verloop3(Hex("4AB8FF"), Color.white, OranjeR, Mathf.InverseLerp(-15f, 25f, 0f));
                spoor.type = Image.Type.Sliced; spoor.pixelsPerUnitMultiplier = 24f / 4f; spoor.color = Color.white;
                spoor.rectTransform.anchorMin = new Vector2(0f, 0.5f); spoor.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                spoor.rectTransform.sizeDelta = new Vector2(0f, 8f);
                go.transform.Find("Fill Area").gameObject.SetActive(false);
                luchtSchuif.fillRect = null;
                var duim = luchtSchuif.handleRect.GetComponent<Image>();
                duim.sprite = Cirkel(); duim.color = Color.white;
                luchtSchuif.handleRect.sizeDelta = new Vector2(24f, 0f);
                luchtSchuif.SetValueWithoutNotify(luchtT);
                luchtSchuif.onValueChanged.AddListener(v => luchtT = v);
                var w = Tekst(r.transform, "Waarde", "", 13.5f, Color.white, TextAlignmentOptions.Right, true);
                LE(w.gameObject, prefW: 58f);
                tLuchtWaarde = w;

                // de ijsdikte en de schaatsers staan in beeld; geen aparte statusregel meer
            }
            var noot = Tekst(paneel.transform, "Model", Teksten.T(macroModus == 0 ? "model.glas" : macroModus == 2 ? "model.pool" : "model.meer"), 10.5f, new Color(IJsTekst.r, IJsTekst.g, IJsTekst.b, 0.7f), TextAlignmentOptions.Left, false, true);
            LE(noot.gameObject, prefH: 28f);
        }

        TextMeshProUGUI tLuchtWaarde;

        // "15 jan" uit de dag van het jaar (0 = 1 januari)
        static string Datum(double dag)
        {
            int m = WaterSim.Sim.Seizoen.Maand(dag);
            int[] begin = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };
            int d = (int)(dag - begin[m]) + 1;
            return d + " " + Teksten.T("maand." + m);
        }

        void WerkMacroBij()
        {
            if (macroModus == 0 && glasW != null)
            {
                double rhoIjs = WaterSim.Sim.Constanten.DichtheidIjsBinas, rhoV = DichtheidVloeistof(vloeistofKeuze);
                bool drijft = WaterSim.Sim.Drijven.Drijft(rhoIjs, rhoV);
                if (aanHetSmelten) { gesmolten = Mathf.Min(1f, gesmolten + Time.deltaTime / 8f); if (gesmolten >= 1f) aanHetSmelten = false; }
                glasW.rhoIjs = rhoIjs; glasW.rhoVloeistof = rhoV; glasW.rhoSmeltwater = WaterSim.Sim.Constanten.DichtheidWater(0);
                glasW.opRots = opRots; glasW.gesmolten = gesmolten;
                glasW.vloeistof = vloeistofKeuze == 2 ? new Color(0.75f, 0.82f, 0.86f, 0.32f) : Lin(Hex(vloeistofKeuze == 1 ? "1A6A8C" : "1E7FA8")) * new Color(1f, 1f, 1f, 0.6f);
                int pOnder = Mathf.RoundToInt((float)WaterSim.Sim.Drijven.DeelOnder(rhoIjs, rhoV) * 100f);
                bool labels = !opRots && gesmolten < 0.95f;
                glasW.lBoven.text = labels && drijft ? string.Format(Teksten.T("glas.boven"), 100 - pOnder) : "";
                glasW.lOnder.text = !labels ? "" : drijft ? string.Format(Teksten.T("glas.onder"), pOnder) : Teksten.T("glas.zinkt");
                if (tSmeltKnop != null) tSmeltKnop.text = Teksten.T(gesmolten > 0f ? "opnieuw" : "glas.smelt");
                if (tGlasStatus != null)
                {
                    string sleutel = gesmolten < 1f
                        ? (opRots ? "glas.status.rots" : drijft ? (vloeistofKeuze == 1 ? "glas.status.zee" : "glas.status.drijft") : "glas.status.zinkt")
                        : (opRots ? "glas.na.rots" : !drijft ? "glas.na.zinkt" : vloeistofKeuze == 1 ? "glas.na.zee" : "glas.na.zoet");
                    tGlasStatus.text = Teksten.T(sleutel);
                }
            }
            else if (macroModus == 2 && poolW != null)
            {
                // landijs en zee-ijs smelten elk apart (in ongeveer 8 seconden)
                if (smeltLand) { gesmoltenLand = Mathf.Min(1f, gesmoltenLand + Time.deltaTime / 8f); if (gesmoltenLand >= 1f) smeltLand = false; }
                if (smeltZee) { gesmoltenZee = Mathf.Min(1f, gesmoltenZee + Time.deltaTime / 8f); if (gesmoltenZee >= 1f) smeltZee = false; }
                poolW.gesmoltenLand = gesmoltenLand; poolW.gesmoltenZee = gesmoltenZee;
                poolW.lLand.text = gesmoltenLand > 0f ? string.Format(Teksten.T("pool.stijging"), Teksten.Getal(gesmoltenLand * WaterSim.Sim.Constanten.ZeespiegelGroenland, "0.0")) : "";
                poolW.lZee.text = gesmoltenZee > 0f ? Teksten.T("pool.gelijk") : "";
                if (tGlasStatus != null)
                {
                    bool land = gesmoltenLand >= 1f, zee = gesmoltenZee >= 1f;
                    tGlasStatus.text = land && zee
                        ? string.Format(Teksten.T("pool.na"), Teksten.Getal(WaterSim.Sim.Constanten.ZeespiegelGroenland, "0.0"), Teksten.Getal(WaterSim.Sim.Constanten.ZeespiegelAntarctica, "0"))
                        : land ? string.Format(Teksten.T("pool.na.land"), Teksten.Getal(WaterSim.Sim.Constanten.ZeespiegelGroenland, "0.0"))
                        : zee ? Teksten.T("pool.na.zee") : Teksten.T("pool.status");
                }
            }
            else if (macroModus == 1 && meerW != null && meer != null)
            {
                double dag = (startDag + meer.Tijd / 86400.0) % WaterSim.Sim.Seizoen.DagenPerJaar;
                if (jaarModus) luchtT = (float)WaterSim.Sim.Seizoen.Lucht(dag);
                meer.Zon = zonStand == 0 ? 0 : jaarModus ? WaterSim.Sim.Seizoen.Zon(dag)
                         : zonStand == 1 ? WaterSim.Sim.Seizoen.ZonWinter : WaterSim.Sim.Seizoen.ZonVoorjaar;
                if (tZonKnop != null) tZonKnop.text = Teksten.T(zonStand == 0 ? "zon.uit" : jaarModus ? "zon.jaar" : zonStand == 1 ? "zon.winter" : "zon.voorjaar");
                if (luchtSchuif != null)
                {
                    luchtSchuif.interactable = !jaarModus;
                    luchtSchuif.SetValueWithoutNotify(luchtT);
                }
                meer.Stap(Time.deltaTime * (jaarModus ? DagenPerSecondeJaar : DagenPerSeconde) * 86400.0, luchtT);
                meerW.meer = meer;
                for (int i = 0; i < meerW.diepteLabels.Count; i++)
                {
                    var (t, d) = meerW.diepteLabels[i];
                    int laag = Mathf.Clamp((int)(d / meer.Diepte * meer.Lagen), 0, meer.Lagen - 1);
                    t.text = $"{Teksten.Getal(d, "0.#")} m: <b>{Teksten.Graden(meer.T[laag], 1)}</b>";
                }
                // wat er netto met het ijs gebeurt (gemiddeld over een dag), met de snelheid
                var tst = meer.Toestand;
                string waar = tst == WaterSim.Sim.Meer.IJstoestand.Groeit ? "meer.groeit" : tst == WaterSim.Sim.Meer.IJstoestand.SmeltBoven ? "meer.smelt"
                            : tst == WaterSim.Sim.Meer.IJstoestand.SmeltOnder ? "meer.smeltonder" : tst == WaterSim.Sim.Meer.IJstoestand.SmeltBeide ? "meer.smeltbeide"
                            : tst == WaterSim.Sim.Meer.IJstoestand.Gelijk ? "meer.gelijk" : "";
                double cmPerDag = meer.NettoPerDag * 100;
                string snelheid = System.Math.Abs(cmPerDag) >= 0.05 ? $" ({(cmPerDag > 0 ? "+" : "−")}{Teksten.Getal(System.Math.Abs(cmPerDag), "0.0")} cm/{Teksten.T("dag")})" : "";
                meerW.lIjs.text = meer.Ijsdikte > 0.003
                    ? "<b>" + string.Format(Teksten.T("meer.ijs"), Teksten.Getal(meer.Ijsdikte * 100, "0")) + "</b>" + (waar != "" ? "\n" + Teksten.T(waar) + snelheid : "")
                    : "";

                if (tLucht != null)
                    tLucht.text = $"{Teksten.T("lucht")} {Teksten.Graden(luchtT, 0, true)}\n<size=75%><color=#B9DCEA>" +
                                  (jaarModus ? Datum(dag) : $"{Teksten.T("dag.hoofd")} {(int)(meer.Tijd / 86400.0)}") + "</color></size>";
                if (meerW.lBodem != null) meerW.lBodem.text = $"{Teksten.T("bodem")}: <b>{Teksten.Graden(meer.Bodem, 1)}</b>";
                if (tLuchtWaarde != null) tLuchtWaarde.text = Teksten.Graden(luchtT, 0, true);

            }
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
            Beschrijving(paneel, Levels[level].scene == 1 ? "level.1.zie" : "level.2.zie");
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
            var ik = Infoknop(ukHouder);
            Hoek(ik.rectTransform, new Vector2(1f, 0f), new Vector2(-3f, 2f), new Vector2(34f, 34f));
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
            kristal.enabled = p == Pagina.Sim && !MolecuulLevel && !MacroLevel;
            if (molecuul != null) molecuul.enabled = p == Pagina.Sim && MolecuulLevel;
            if (p == Pagina.Start)
            {
                if (vliegcamera != null) vliegcamera.enabled = false;
            }
            else
            {
                var info = Levels[level];
                if (info.scene == 4)
                {
                    // level 5: glas en meer (plat getekend), geen 3D-camera nodig
                    if (vliegcamera != null) vliegcamera.enabled = false;
                    if (meer == null) meer = new WaterSim.Sim.Meer(24, 6, 12);
                    Bouw();
                    return;
                }
                if (info.scene == 1 || info.scene == 2)
                {
                    // level 1 en 2: dichtbij om één of twee moleculen heen draaien
                    molecuul.soort = info.scene == 1 ? Molecuulscene.Soort.Molecuul : Molecuulscene.Soort.Paar;
                    laatsteKalotten = molecuul.kalotten;
                    pasCameraAan = 3;               // na de opbouw van het scherm: passend inzoomen
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
                kristal.trillingVergroting = info.scene == 3 ? 1f : 2f;   // level 3 (structuur): ware grootte
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
            if (vliegcamera != null && vliegcamera.DraaiOm) PasMolecuulcamera(false);
            if (zoomHouder != null && zoomHouder.gameObject.activeSelf == infoOpen) zoomHouder.gameObject.SetActive(!infoOpen);   // niet over het infovak
            if (infoVak != null && infoVak.gameObject.activeSelf != infoOpen)
            {
                infoVak.gameObject.SetActive(infoOpen);
                if (infoOpen) VulInfo();
            }
            foreach (var k in infoKnoppen) if (k != null) k.color = infoOpen ? Lin(OranjeL) : new Color(1f, 1f, 1f, 0.16f);
            if (formuleKaart != null)
            {
                bool kaartZichtbaar = toonFormule && !infoOpen, knopZichtbaar = !toonFormule && !infoOpen;
                if (formuleKaart.gameObject.activeSelf != kaartZichtbaar || formuleDicht.gameObject.activeSelf != knopZichtbaar)
                {
                    formuleKaart.gameObject.SetActive(kaartZichtbaar);
                    formuleDicht.gameObject.SetActive(knopZichtbaar);
                }
                WerkFormuleBij();
            }
            if (MacroLevel)
            {
                foreach (var k in schakelaars) Actief(k.img, k.t, k.aan());
                WerkMacroBij();
                return;
            }
            if (MolecuulLevel || IjsLevel)
            {
                foreach (var k in schakelaars) Actief(k.img, k.t, k.aan());
                if (MolecuulLevel)
                {
                    if (molecuul.kalotten != laatsteKalotten) { laatsteKalotten = molecuul.kalotten; pasCameraAan = 2; }
                    if (pasCameraAan > 0 && --pasCameraAan == 0) PasMolecuulcamera(true);
                }
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
        readonly Vector3[] vrijHoeken = new Vector3[4];

        // het draaipunt midden in het vrije stuk tussen bovenbalk en paneel; met 'afstand' ook passend inzoomen
        void PasMolecuulcamera(bool afstand)
        {
            if (vliegcamera == null || vrijRt == null) return;
            vrijRt.GetWorldCorners(vrijHoeken);            // overlay-canvas: wereld = schermpixels
            float onder = vrijHoeken[0].y, boven = vrijHoeken[1].y, H = Screen.height;
            if (boven - onder < 10f || H <= 0f) return;
            vliegcamera.beeldVerschuiving = (onder + boven) * 0.5f / H - 0.5f;
            if (!afstand || !MolecuulLevel) return;
            var c = vliegcamera.GetComponent<Camera>();
            if (c == null) return;
            float tanV = Mathf.Tan(c.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float dV = molecuul.Straal / (tanV * (boven - onder) / H);
            float dH = molecuul.Straal / (tanV * c.aspect * 0.9f);
            vliegcamera.PasAfstand(Mathf.Max(dV, dH) * 1.05f);
        }

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
