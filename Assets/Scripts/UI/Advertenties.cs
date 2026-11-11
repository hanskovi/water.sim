using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID || UNITY_IOS
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace WaterSim.UI
{
    /// <summary>
    /// Advertenties: een banner onderaan elke pagina en een interstitial na een afgeronde quizronde.
    ///
    /// - In de editor (doel Android of iOS): de nep-advertenties van de AdMob-SDK zelf (banner en interstitial).
    /// - In development builds: Google's TEST-advertenties ("Test Ad"); eigen tikken tellen niet mee.
    /// - In test- en releasebuilds (Android/iOS): ECHTE advertenties via Google AdMob, na het toestemmingsvenster (UMP)
    ///   waar dat nodig is (EER, VK, Zwitserland). Registreer je eigen toestellen in AdMob als testtoestel, anders kan
    ///   een eigen tik als ongeldig verkeer tellen.
    /// - Zonder SDK (ander platform): de grijze strook van AppSchil en een eigen scherm "advertentie (nep)".
    /// - De app is voor scholieren vanaf 14 jaar: alle gebruikers krijgen de tienerbehandeling (geen persoonlijke
    ///   advertenties, inhoud hooguit rating T).
    /// - Een interstitial komt hooguit eens per <see cref="MinTussenInterstitials"/> seconden, en alleen tussen rondes.
    ///
    /// Gebaseerd op de AdManager van C.chem (toestemming eerst, callbacks op de Unity-thread).
    /// </summary>
    public partial class Advertenties : MonoBehaviour
    {
        public static Advertenties Instance { get; private set; }

        /// <summary>Hoogte van de banner in pixels zodra die bekend is (voor de vrije strook onderaan).</summary>
        public static event Action<float> BannerHoogteBekend;
        /// <summary>Hoogte van de geladen banner in pixels, of −1 zolang er geen is (voor wie zich pas later aanmeldt).</summary>
        public static float BannerPixels { get; private set; } = -1f;
        /// <summary>Wordt aangeroepen als bekend is of de knop "Privacykeuzes" nodig is.</summary>
        public static event Action PrivacyKeuzesGewijzigd;

        /// <summary>Minimaal aantal seconden tussen twee interstitials.</summary>
        public const float MinTussenInterstitials = 60f;
        float laatsteInterstitial = -1000f;

#if UNITY_ANDROID || UNITY_IOS
        /// <summary>Eigen nep-advertenties (alleen als de AdMob-SDK er niet is, bv. de editor met Standalone als doel).</summary>
        public static bool Nep => false;
#else
        public static bool Nep => true;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Editor en development builds: Google's test-ID's (in de editor nep-advertenties van de SDK, op een toestel "Test Ad").</summary>
        static readonly bool TestIds = true;
#else
        static readonly bool TestIds = false;
#endif

        // Ad-unit-ID's van Water.sim (AdMob). Laat je er een leeg, dan gebruikt de app daar Google's test-ID.
#if UNITY_ANDROID
        const string EchteBanner = "ca-app-pub-6923727980694793/7703937959", EchteInterstitial = "ca-app-pub-6923727980694793/2991647265";
        const string TestBanner = "ca-app-pub-3940256099942544/6300978111", TestInterstitial = "ca-app-pub-3940256099942544/1033173712";
#elif UNITY_IOS
        const string EchteBanner = "ca-app-pub-6923727980694793/9727466065", EchteInterstitial = "ca-app-pub-6923727980694793/2451611271";
        const string TestBanner = "ca-app-pub-3940256099942544/2934735716", TestInterstitial = "ca-app-pub-3940256099942544/4411468910";
#else
        const string EchteBanner = "", EchteInterstitial = "", TestBanner = "", TestInterstitial = "";
#endif
        static string BannerId => TestIds || EchteBanner == "" ? TestBanner : EchteBanner;
        static string InterstitialId => TestIds || EchteInterstitial == "" ? TestInterstitial : EchteInterstitial;

        readonly Queue<Action> hoofdThread = new Queue<Action>();
        void OpHoofdThread(Action a) { lock (hoofdThread) hoofdThread.Enqueue(a); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void MaakAan()
        {
            if (Instance != null) return;
            new GameObject("Advertenties").AddComponent<Advertenties>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
#if UNITY_ANDROID || UNITY_IOS
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            VraagToestemmingDanStart();
#endif
        }

        void Update()
        {
            lock (hoofdThread) while (hoofdThread.Count > 0) hoofdThread.Dequeue()?.Invoke();
#if UNITY_EDITOR
            NepBannerBovenop();
            EigenBannersEditor();
#endif
        }

#if UNITY_EDITOR
        // De nep-banner van de AdMob-SDK in de editor is een Canvas met sorteervolgorde 0; de interface van de app (100)
        // ligt er dan overheen. Op een toestel is de banner een systeemvenster en ligt hij altijd bovenop; hier doen we
        // dat na: boven de interface, onder de interstitials (500 en 32767).
        const int BannerVolgorde = 400;
        float volgendeBannerCheck;

        void NepBannerBovenop()
        {
            if (Time.unscaledTime < volgendeBannerCheck) return;
            volgendeBannerCheck = Time.unscaledTime + 0.5f;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.isRootCanvas && c.sortingOrder < BannerVolgorde && c.name.Contains("ADAPTIVE"))
                    c.sortingOrder = BannerVolgorde;
        }
#endif

        // ---------- interstitial ----------

        /// <summary>
        /// Toon een interstitial (als dat mag volgens de frequentiegrens en er een klaarstaat) en roep daarna
        /// <paramref name="daarna"/> aan. Zonder advertentie wordt <paramref name="daarna"/> meteen aangeroepen.
        /// </summary>
        public void ToonInterstitial(Action daarna = null)
        {
            if (Time.realtimeSinceStartup - laatsteInterstitial < MinTussenInterstitials) { daarna?.Invoke(); return; }
            laatsteInterstitial = Time.realtimeSinceStartup;
            if (Nep) { NepInterstitial(daarna); return; }
#if UNITY_ANDROID || UNITY_IOS
            if (gestart && interstitial != null && interstitial.CanShowAd())
            {
                naInterstitial = daarna;
                interstitial.Show();
                return;
            }
            LaadInterstitial();
#endif
            daarna?.Invoke();
        }

        // nep-interstitial: donker scherm met "advertentie (nep)", tik om te sluiten
        void NepInterstitial(Action daarna)
        {
            var go = new GameObject("Nep-interstitial", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 500;
            var bg = new GameObject("Achtergrond", typeof(RectTransform)).AddComponent<Image>();
            bg.transform.SetParent(go.transform, false);
            bg.color = new Color(0.02f, 0.03f, 0.05f, 0.96f);
            var rt = bg.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = new GameObject("Tekst", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.transform.SetParent(bg.transform, false);
            if (UIBouw.Regular != null) t.font = UIBouw.Regular;
            t.text = "advertentie (nep)\n<size=60%>alleen in editor en testbuilds · tik om te sluiten</size>";
            t.alignment = TextAlignmentOptions.Center; t.color = new Color(1f, 1f, 1f, 0.8f);
            t.fontSize = 28f * Mathf.Min(Screen.width / 390f, Screen.height / 844f);
            var trt = t.rectTransform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            var knop = bg.gameObject.AddComponent<Button>();
            knop.onClick.AddListener(() => { Destroy(go); daarna?.Invoke(); });
        }

        // ---------- privacykeuzes ----------

        /// <summary>True als de gebruiker zijn advertentie-privacykeuzes moet kunnen wijzigen (EER e.d.).</summary>
        public static bool PrivacyKeuzesNodig
        {
            get
            {
#if UNITY_ANDROID || UNITY_IOS
                try { return ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required; }
                catch (Exception) { return false; }
#else
                return Nep;                                       // in de editor de knop laten zien, om de opmaak te testen
#endif
            }
        }

        /// <summary>Toont Google's venster met privacykeuzes voor advertenties.</summary>
        public static void ToonPrivacyKeuzes()
        {
#if UNITY_ANDROID || UNITY_IOS
            ConsentForm.ShowPrivacyOptionsForm(fout =>
            {
                var ik = Instance;
                if (ik == null) return;
                ik.OpHoofdThread(() => { if (fout != null) Debug.LogWarning("[Advertenties] privacykeuzes: " + fout.Message); ik.StartAdvertenties(false); });
            });
#else
            Debug.Log("[Advertenties] privacykeuzes (nep): in een echte build opent hier Google's venster");
#endif
        }

#if UNITY_ANDROID || UNITY_IOS
        // ---------- echte advertenties (AdMob) ----------
        const float ToestemmingTimeout = 8f;
        /// <summary>Gebruikers gelden als jonger dan de leeftijd van toestemming (16 in NL): alleen niet-persoonlijke advertenties.</summary>
        const bool OnderLeeftijdVanToestemming = true;
        bool gestart;
        BannerView banner;
        InterstitialAd interstitial;
        Action naInterstitial;

        void VraagToestemmingDanStart()
        {
            StartAdvertenties(false);                                         // toestemming uit een eerdere sessie: meteen starten
            Invoke(nameof(TimeoutToestemming), ToestemmingTimeout);
            try
            {
                ConsentInformation.Update(new ConsentRequestParameters { TagForUnderAgeOfConsent = OnderLeeftijdVanToestemming }, fout => OpHoofdThread(() =>
                {
                    if (fout != null) { Debug.LogWarning("[Advertenties] toestemming bijwerken mislukt: " + fout.Message); StartAdvertenties(true); return; }
                    ConsentForm.LoadAndShowConsentFormIfRequired(formFout => OpHoofdThread(() =>
                    {
                        if (formFout != null) Debug.LogWarning("[Advertenties] toestemmingsvenster: " + formFout.Message);
                        StartAdvertenties(formFout != null);
                        PrivacyKeuzesGewijzigd?.Invoke();
                    }));
                }));
            }
            catch (Exception e) { Debug.LogWarning("[Advertenties] toestemming: " + e.Message); StartAdvertenties(true); }
        }

        void TimeoutToestemming() { if (!gestart) StartAdvertenties(true); }

        void StartAdvertenties(bool zonderControle)
        {
            if (gestart) return;
            if (!zonderControle && !ConsentInformation.CanRequestAds()) return;     // eerst het toestemmingsvenster
            gestart = true;
            CancelInvoke(nameof(TimeoutToestemming));
            // de app is voor scholieren vanaf 14 jaar (in NL is de leeftijd van toestemming 16): tienerbehandeling, dus
            // geen persoonlijke advertenties, en alleen advertenties die geschikt zijn voor tieners
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                AgeRestrictedTreatment = AgeRestrictedTreatment.Teen,
                MaxAdContentRating = MaxAdContentRating.T,
            });
            MobileAds.Initialize(_ => { MaakGoogleBanner(); LaadInterstitial(); });
        }

        void MaakGoogleBanner()
        {
            VernietigGoogleBanner();
            gestartBanner = true;
            var maat = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            var b = new BannerView(BannerId, maat, AdPosition.Bottom);
            banner = b;
            b.OnBannerAdLoaded += () => OpHoofdThread(() =>
            {
                if (banner != b) return;                                       // al vervangen door een eigen banner
                BannerPixels = b.GetHeightInPixels();
                BannerHoogteBekend?.Invoke(BannerPixels);
                GoogleBannerGeladen();                                         // telt mee voor de eigen banners
            });
            b.LoadAd(new AdRequest());
        }

        void VernietigGoogleBanner()
        {
            if (banner == null) return;
            banner.Destroy();
            banner = null;
        }

        void LaadInterstitial()
        {
            interstitial?.Destroy(); interstitial = null;
            InterstitialAd.Load(InterstitialId, new AdRequest(), (ad, fout) =>
            {
                if (fout != null || ad == null) { Debug.LogWarning("[Advertenties] interstitial laden mislukt: " + fout); return; }
                interstitial = ad;
                interstitial.OnAdFullScreenContentClosed += () => { var d = naInterstitial; naInterstitial = null; d?.Invoke(); LaadInterstitial(); };
                interstitial.OnAdFullScreenContentFailed += _ => { var d = naInterstitial; naInterstitial = null; d?.Invoke(); LaadInterstitial(); };
            });
        }
#else
        void MaakGoogleBanner() { }
        void VernietigGoogleBanner() { }
#endif
    }
}
