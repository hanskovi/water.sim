using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !DEVELOPMENT_BUILD
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace WaterSim.UI
{
    /// <summary>
    /// Advertenties: een banner onderaan elke pagina en een interstitial na een afgeronde quizronde.
    ///
    /// - In de editor en in development builds: NEP-advertenties. De banner is de grijze strook die AppSchil al toont,
    ///   de interstitial een eigen scherm "advertentie (nep)". Er wordt niets bij Google opgevraagd.
    /// - In test- en releasebuilds (Android/iOS): ECHTE advertenties via Google AdMob, na het toestemmingsvenster (UMP)
    ///   waar dat nodig is (EER, VK, Zwitserland). Registreer je eigen toestellen in AdMob als testtoestel, anders kan
    ///   een eigen tik als ongeldig verkeer tellen.
    /// - Een interstitial komt hooguit eens per <see cref="MinTussenInterstitials"/> seconden, en alleen tussen rondes.
    ///
    /// Gebaseerd op de AdManager van C.chem (toestemming eerst, callbacks op de Unity-thread).
    /// </summary>
    public class Advertenties : MonoBehaviour
    {
        public static Advertenties Instance { get; private set; }

        /// <summary>Hoogte van de banner in pixels zodra die bekend is (voor de vrije strook onderaan).</summary>
        public static event Action<float> BannerHoogteBekend;
        /// <summary>Wordt aangeroepen als bekend is of de knop "Privacykeuzes" nodig is.</summary>
        public static event Action PrivacyKeuzesGewijzigd;

        /// <summary>Minimaal aantal seconden tussen twee interstitials.</summary>
        public const float MinTussenInterstitials = 180f;
        float laatsteInterstitial = -1000f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool Nep => true;
#else
        public static bool Nep => false;
#endif

        // Ad-unit-ID's van Water.sim (aanmaken in AdMob). Zolang ze leeg zijn, gebruikt de app Google's test-ID's.
#if UNITY_ANDROID
        const string EchteBanner = "", EchteInterstitial = "";
        const string TestBanner = "ca-app-pub-3940256099942544/6300978111", TestInterstitial = "ca-app-pub-3940256099942544/1033173712";
#elif UNITY_IOS
        const string EchteBanner = "", EchteInterstitial = "";
        const string TestBanner = "ca-app-pub-3940256099942544/2934735716", TestInterstitial = "ca-app-pub-3940256099942544/4411468910";
#else
        const string EchteBanner = "", EchteInterstitial = "", TestBanner = "", TestInterstitial = "";
#endif
        static string BannerId => EchteBanner != "" ? EchteBanner : TestBanner;
        static string InterstitialId => EchteInterstitial != "" ? EchteInterstitial : TestInterstitial;

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
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !DEVELOPMENT_BUILD
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            VraagToestemmingDanStart();
#endif
        }

        void Update()
        {
            lock (hoofdThread) while (hoofdThread.Count > 0) hoofdThread.Dequeue()?.Invoke();
        }

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
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !DEVELOPMENT_BUILD
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
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !DEVELOPMENT_BUILD
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
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !DEVELOPMENT_BUILD
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

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !DEVELOPMENT_BUILD
        // ---------- echte advertenties (AdMob) ----------
        const float ToestemmingTimeout = 8f;
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
                ConsentInformation.Update(new ConsentRequestParameters(), fout => OpHoofdThread(() =>
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
            MobileAds.Initialize(_ => { ToonBanner(); LaadInterstitial(); });
        }

        void ToonBanner()
        {
            banner?.Destroy();
            var maat = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            banner = new BannerView(BannerId, maat, AdPosition.Bottom);
            banner.OnBannerAdLoaded += () => BannerHoogteBekend?.Invoke(banner.GetHeightInPixels());
            banner.LoadAd(new AdRequest());
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
#endif
    }
}
