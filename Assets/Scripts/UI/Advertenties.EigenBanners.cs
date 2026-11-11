using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WaterSim.UI
{
    /// <summary>
    /// Eigen banners (house ads) van ConceptCraft, afwisselend met de AdMob-banner: na elke 3 geladen Google-banners
    /// staat er 30 seconden een eigen banner op dezelfde plek, om de beurt atom.sim, Chemistry Formulas &amp; Names
    /// (chemformula) en C.chem. Een tik opent de app in de Play Store of de App Store.
    ///
    /// Zoals in C.chem (HouseBannerAd): de laatste Google-banner krijgt eerst nog zijn eigen tijd in beeld; tijdens de
    /// eigen banner wordt de Google-banner vernietigd (niet verborgen), zodat er geen onzichtbare vertoningen geteld
    /// worden; daarna komt er een nieuwe Google-banner en begint het tellen opnieuw.
    /// Vereist dat "automatisch vernieuwen" aan staat voor de banner-unit in AdMob (elke vernieuwing telt als geladen).
    /// </summary>
    public partial class Advertenties
    {
        public class EigenApp
        {
            public readonly string Sprite, AndroidPakket, AppleId;
            public EigenApp(string sprite, string androidPakket, string appleId) { Sprite = sprite; AndroidPakket = androidPakket; AppleId = appleId; }

            public void OpenStore()
            {
                Debug.Log("[Advertenties] eigen banner aangetikt: " + Sprite);
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    // market:// opent direct de Play Store-app; lukt dat niet, dan de webversie
                    using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activiteit = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var uriKlasse = new AndroidJavaClass("android.net.Uri"))
                    using (var uri = uriKlasse.CallStatic<AndroidJavaObject>("parse", "market://details?id=" + AndroidPakket))
                    using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW", uri))
                        activiteit.Call("startActivity", intent);
                }
                catch (System.Exception) { Application.OpenURL("https://play.google.com/store/apps/details?id=" + AndroidPakket); }
#elif UNITY_ANDROID
                Application.OpenURL("https://play.google.com/store/apps/details?id=" + AndroidPakket);
#elif UNITY_IOS
                Application.OpenURL("https://apps.apple.com/app/id" + AppleId);
#else
                Application.OpenURL("https://conceptcraft.nl/apps/");
#endif
            }
        }

        // volgorde van de eigen banners (gegevens zoals in C.chem)
        static readonly EigenApp[] EigenApps =
        {
            new EigenApp("eigenbanners/banner_atomsim",   "com.conceptcraft.atomapp",            "1493527466"),
            new EigenApp("eigenbanners/banner_chemistry", "com.conceptcraft.chemformula_EN.sim", "1535630246"),
            new EigenApp("eigenbanners/banner_cchem",     "com.ConceptCraft.C.chem",             "6804392281"),
        };

        const int GoogleBannersPerEigen = 3;          // na zoveel geladen Google-banners een eigen banner
        const float EigenBannerSeconden = 30f;        // zo lang staat een eigen banner (en de laatste Google-banner ervoor)
        const float EigenBannerDp = 50f;              // hoogte als de Google-banner nog geen hoogte heeft gegeven
        const int EigenBannerVolgorde = 450;          // boven de interface (100) en de nep-banner (400), onder interstitials
        static readonly Color EigenBannerAchter = new Color(0.043f, 0.133f, 0.188f, 1f);   // donkerblauw van de app

        int googleGeladen, volgendeEigen;
        bool eigenBezig, gestartBanner;
        RectTransform eigenStrook;
        Image eigenBeeld;
        EigenApp eigenGetoond;

        /// <summary>Wordt aangeroepen bij elke geladen (of ververste) Google-banner.</summary>
        void GoogleBannerGeladen()
        {
            if (eigenBezig) return;
            googleGeladen++;
            Debug.Log($"[Advertenties] Google-banner geladen ({googleGeladen}/{GoogleBannersPerEigen})");
            if (googleGeladen >= GoogleBannersPerEigen) StartCoroutine(EigenBannerRonde(false));
        }

        IEnumerator EigenBannerRonde(bool direct)
        {
            eigenBezig = true;
            if (!direct) yield return new WaitForSecondsRealtime(EigenBannerSeconden);     // laatste Google-banner zijn tijd geven
            VernietigGoogleBanner();
            ToonEigenBanner();
            yield return new WaitForSecondsRealtime(EigenBannerSeconden);
            if (eigenStrook != null) eigenStrook.gameObject.SetActive(false);
            googleGeladen = 0;
            eigenBezig = false;
            MaakGoogleBanner();
        }

        void ToonEigenBanner()
        {
            if (eigenStrook == null) BouwEigenStrook();
            eigenGetoond = EigenApps[volgendeEigen % EigenApps.Length];
            volgendeEigen = (volgendeEigen + 1) % EigenApps.Length;
            var sprite = Resources.Load<Sprite>(eigenGetoond.Sprite);
            if (sprite == null) { Debug.LogWarning("[Advertenties] eigen banner niet gevonden: " + eigenGetoond.Sprite); return; }
            eigenBeeld.sprite = sprite;
            PlaatsEigenStrook();
            eigenStrook.gameObject.SetActive(true);
            Debug.Log("[Advertenties] eigen banner: " + eigenGetoond.Sprite);
        }

        // eigen canvas boven de interface, op de strook die de app voor de banner vrijhoudt
        void BouwEigenStrook()
        {
            var cgo = new GameObject("EigenBanner", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            var c = cgo.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = EigenBannerVolgorde;

            var achter = new GameObject("Strook", typeof(RectTransform), typeof(Image), typeof(Button));
            achter.transform.SetParent(cgo.transform, false);
            eigenStrook = (RectTransform)achter.transform;
            eigenStrook.anchorMin = Vector2.zero; eigenStrook.anchorMax = new Vector2(1f, 0f); eigenStrook.pivot = new Vector2(0.5f, 0f);
            achter.GetComponent<Image>().color = EigenBannerAchter;
            var knop = achter.GetComponent<Button>();
            knop.transition = Selectable.Transition.None;
            knop.onClick.AddListener(() => eigenGetoond?.OpenStore());

            var beeld = new GameObject("Beeld", typeof(RectTransform), typeof(Image));
            beeld.transform.SetParent(achter.transform, false);
            var brt = (RectTransform)beeld.transform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
            eigenBeeld = beeld.GetComponent<Image>();
            eigenBeeld.preserveAspect = true; eigenBeeld.raycastTarget = false;
            achter.SetActive(false);
        }

        void PlaatsEigenStrook()
        {
            float hoogte = BannerPixels > 0f ? BannerPixels : EigenBannerDp * (Screen.dpi > 0f ? Screen.dpi / 160f : 1f);
#if UNITY_EDITOR
            float onder = 0f;                                  // de nep-banner van de SDK staat tegen de onderrand
#else
            float onder = Screen.safeArea.yMin;                // op een toestel: boven de thuisbalk, waar de app de strook vrijhoudt
#endif
            eigenStrook.anchoredPosition = new Vector2(0f, onder);
            eigenStrook.sizeDelta = new Vector2(0f, hoogte);
        }

#if UNITY_EDITOR
        // In de editor ververst de nep-banner van de SDK niet: doe alsof er elke 5 s een nieuwe Google-banner laadt.
        // Toets H toont meteen een eigen banner.
        const float EditorVerversing = 5f;
        float editorTimer;

        void EigenBannersEditor()
        {
            if (!gestartBanner) return;
            if (!eigenBezig && (editorTimer += Time.unscaledDeltaTime) >= EditorVerversing) { editorTimer = 0f; GoogleBannerGeladen(); }
            var t = UnityEngine.InputSystem.Keyboard.current;
            if (t != null && t.hKey.wasPressedThisFrame && !eigenBezig) { StopAllCoroutines(); StartCoroutine(EigenBannerRonde(true)); }
        }
#endif
    }
}
