using UnityEngine;
using UnityEngine.InputSystem;

namespace WaterSim.Besturing
{
    /// <summary>
    /// Twee standen:
    /// - Vrij vliegen door het kristal (oneindig).
    ///   Muis: slepen = rondkijken, scrollen = vooruit/achteruit. Toetsen: W/S of ↑/↓ vooruit/achteruit,
    ///   A/D of ←/→ opzij, Q/E omlaag/omhoog, Shift = sneller. Aanraken: één vinger = rondkijken,
    ///   twee vingers omhoog/omlaag = vooruit/achteruit.
    /// - Draaien om een punt (los blok, <see cref="DraaiOm"/>): slepen = om het blok heen draaien,
    ///   scrollen / twee vingers / W en S = dichterbij of verder weg, A/D en Q/E = draaien.
    /// Afstanden in Å.
    /// </summary>
    public class Vliegcamera : MonoBehaviour
    {
        public float snelheid = 5f;
        public float snelSnelheid = 14f;
        [Tooltip("Graden per pixel slepen.")]
        public float kijkGevoeligheid = 0.15f;
        [Tooltip("Å per pixel bij schuiven met twee vingers.")]
        public float vingerVlieg = 0.03f;
        [Tooltip("Å per scrollstap.")]
        public float scrollStap = 0.6f;

        /// <summary>Geeft true als een schermpunt (pixels, oorsprong linksonder) op een knop of paneel ligt;
        /// aanrakingen die daar beginnen, sturen de camera niet.</summary>
        public static System.Predicate<Vector2> OpUI;

        [Header("Draaien om een punt")]
        public Vector3 draaiPunt = Vector3.zero;
        public float minAfstand = 10f, maxAfstand = 160f;
        [Tooltip("Graden per seconde bij A/D en Q/E.")]
        public float draaiToets = 70f;

        [Tooltip("Verschuift het draaipunt omhoog (+) of omlaag (−) in beeld, als deel van de schermhoogte; zo staat het molecuul midden in het vrije stuk boven het paneel.")]
        public float beeldVerschuiving;

        float yaw, pitch, afstand = 60f, doelAfstand = -1f;

        /// <summary>Huidige afstand tot het draaipunt (Å).</summary>
        public float Afstand => afstand;

        /// <summary>Laat de camera vloeiend naar deze afstand gaan (stopt zodra de gebruiker zelf zoomt).</summary>
        public void PasAfstand(float doel) => doelAfstand = Mathf.Clamp(doel, minAfstand, maxAfstand);

        /// <summary>Knop + (richting 1) of − (richting −1): dichterbij of verder weg.</summary>
        public void Zoom(int richting)
        {
            doelAfstand = -1f;
            if (draaiOm) afstand = Mathf.Clamp(afstand * (richting > 0 ? 0.8f : 1.25f), minAfstand, maxAfstand);
            else transform.position += transform.forward * (1.5f * richting);
        }
        bool muisOpUI, draaiOm;

        /// <summary>Aan: de camera draait om <see cref="draaiPunt"/> (los blok). Uit: vrij vliegen.</summary>
        public bool DraaiOm
        {
            get => draaiOm;
            set
            {
                if (value == draaiOm) return;
                draaiOm = value;
                if (draaiOm) Zet(transform.position, draaiPunt);
            }
        }

        /// <summary>Zet de camera op <paramref name="positie"/>, kijkend naar <paramref name="kijkNaar"/>.</summary>
        public void Zet(Vector3 positie, Vector3 kijkNaar)
        {
            transform.position = positie;
            var r = Quaternion.LookRotation(kijkNaar - positie, Vector3.up).eulerAngles;
            yaw = r.y;
            pitch = r.x > 180f ? r.x - 360f : r.x;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            if (draaiOm) afstand = Mathf.Clamp(Vector3.Distance(positie, draaiPunt), minAfstand, maxAfstand);
        }

        void Start()
        {
            var e = transform.eulerAngles;
            yaw = e.y;
            pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector2 kijk = Vector2.zero;
            Vector3 beweeg = Vector3.zero;        // x = opzij, y = omhoog, z = vooruit (in Å)
            bool aanraking = false;

            var scherm = Touchscreen.current;
            if (scherm != null)
            {
                int vingers = 0;
                Vector2 som = Vector2.zero;
                foreach (var t in scherm.touches)
                {
                    if (!t.press.isPressed) continue;
                    if (OpUI != null && OpUI(t.startPosition.ReadValue())) { aanraking = true; continue; }
                    vingers++;
                    som += t.delta.ReadValue();
                }
                if (vingers == 1) kijk += som;
                else if (vingers >= 2)
                {
                    beweeg.z += som.y / vingers * vingerVlieg;
                    // knijpen: vingers uit elkaar = dichterbij
                    Vector2 p0 = Vector2.zero, p1 = Vector2.zero, d0 = Vector2.zero, d1 = Vector2.zero; int n = 0;
                    foreach (var t in scherm.touches)
                    {
                        if (!t.press.isPressed || (OpUI != null && OpUI(t.startPosition.ReadValue()))) continue;
                        if (n == 0) { p0 = t.position.ReadValue(); d0 = t.delta.ReadValue(); }
                        else if (n == 1) { p1 = t.position.ReadValue(); d1 = t.delta.ReadValue(); }
                        n++;
                    }
                    if (n >= 2) beweeg.z += (Vector2.Distance(p0, p1) - Vector2.Distance(p0 - d0, p1 - d1)) * vingerVlieg * 1.5f;
                }
                aanraking |= vingers > 0;
            }

            var muis = Mouse.current;
            if (muis != null && !aanraking)
            {
                if (muis.leftButton.wasPressedThisFrame || muis.rightButton.wasPressedThisFrame)
                    muisOpUI = OpUI != null && OpUI(muis.position.ReadValue());
                if ((muis.leftButton.isPressed || muis.rightButton.isPressed) && !muisOpUI) kijk += muis.delta.ReadValue();
                float scroll = muis.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f) beweeg.z += Mathf.Sign(scroll) * scrollStap;
            }

            var tb = Keyboard.current;
            if (tb != null)
            {
                float v = (tb.leftShiftKey.isPressed || tb.rightShiftKey.isPressed ? snelSnelheid : snelheid) * dt;
                if (tb.wKey.isPressed || tb.upArrowKey.isPressed) beweeg.z += v;
                if (tb.sKey.isPressed || tb.downArrowKey.isPressed) beweeg.z -= v;
                if (tb.dKey.isPressed || tb.rightArrowKey.isPressed) beweeg.x += v;
                if (tb.aKey.isPressed || tb.leftArrowKey.isPressed) beweeg.x -= v;
                if (tb.eKey.isPressed) beweeg.y += v;
                if (tb.qKey.isPressed) beweeg.y -= v;
            }

            if (draaiOm)
            {
                // slepen pakt het blok vast: naar rechts slepen draait het blok naar rechts
                yaw += -kijk.x * kijkGevoeligheid + Mathf.Sign(beweeg.x) * (beweeg.x != 0f ? draaiToets * dt : 0f);
                pitch = Mathf.Clamp(pitch - kijk.y * kijkGevoeligheid + Mathf.Sign(beweeg.y) * (beweeg.y != 0f ? draaiToets * dt : 0f), -85f, 85f);
                if (beweeg.z != 0f) doelAfstand = -1f;
                afstand = Mathf.Clamp(afstand * Mathf.Exp(-beweeg.z * 0.08f), minAfstand, maxAfstand);
                if (doelAfstand > 0f)
                {
                    afstand = Mathf.Lerp(afstand, doelAfstand, 1f - Mathf.Exp(-6f * dt));
                    if (Mathf.Abs(afstand - doelAfstand) < 0.01f) doelAfstand = -1f;
                }
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                // camera iets omlaag schuiven zodat het draaipunt hoger in beeld staat (boven het paneel)
                var c = GetComponent<Camera>();
                float halfHoogte = afstand * Mathf.Tan((c != null ? c.fieldOfView : 60f) * 0.5f * Mathf.Deg2Rad);
                transform.position = draaiPunt - transform.forward * afstand - transform.up * (2f * halfHoogte * beeldVerschuiving);
                return;
            }
            yaw += kijk.x * kijkGevoeligheid;
            pitch = Mathf.Clamp(pitch - kijk.y * kijkGevoeligheid, -85f, 85f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position += transform.right * beweeg.x + Vector3.up * beweeg.y + transform.forward * beweeg.z;
        }
    }
}
