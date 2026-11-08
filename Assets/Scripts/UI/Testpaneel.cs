using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using WaterSim.Besturing;
using WaterSim.Weergave;

namespace WaterSim.UI
{
    /// <summary>
    /// Eenvoudig bedieningspaneel onderin beeld om te testen (ook op de telefoon): temperatuur van de omgeving,
    /// oneindig/los blok, kalotten en H-bruggen, plus de toestand van het water.
    ///
    /// Tekenen gaat met OnGUI, maar klikken en slepen lezen we zelf uit via het Input System
    /// (IMGUI-knoppen krijgen in de Device Simulator geen aanrakingen door).
    /// Tijdelijk: in fase 4 komt de echte interface in de stijl van C.chem.
    /// </summary>
    public class Testpaneel : MonoBehaviour
    {
        public Kristal kristal;
        public Vliegcamera vliegcamera;
        public bool open = true;

        static readonly int[] Snelknoppen = { -20, -5, 5, 20, 80 };
        const float Tmin = -100f, Tmax = 99f;

        Rect paneel, schuif;                  // GUI-coördinaten (oorsprong linksboven)
        readonly List<(Rect vak, Action actie)> knoppen = new List<(Rect, Action)>();
        bool slepen;
        float schaal = -1f;
        GUIStyle tekst, knop, knopAan;

        void Start()
        {
            if (kristal == null) kristal = FindAnyObjectByType<Kristal>();
            if (vliegcamera == null) vliegcamera = FindAnyObjectByType<Vliegcamera>();
            Vliegcamera.OpUI = p => paneel.Contains(new Vector2(p.x, Screen.height - p.y));
        }

        void OnDestroy() { Vliegcamera.OpUI = null; }

        // ---------- invoer ----------
        void Update()
        {
            if (kristal == null) return;
            bool ingedrukt = false, begon = false;
            Vector2 pos = default;
            var scherm = Touchscreen.current;
            if (scherm != null && scherm.primaryTouch.press.isPressed)
            {
                ingedrukt = true;
                pos = scherm.primaryTouch.position.ReadValue();
                begon = scherm.primaryTouch.press.wasPressedThisFrame;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                ingedrukt = true;
                pos = Mouse.current.position.ReadValue();
                begon = Mouse.current.leftButton.wasPressedThisFrame;
            }
            var p = new Vector2(pos.x, Screen.height - pos.y);

            if (begon)
            {
                if (open && schuif.Contains(p)) slepen = true;
                else foreach (var k in knoppen) if (k.vak.Contains(p)) { k.actie(); break; }
            }
            if (!ingedrukt) slepen = false;
            if (slepen)
                kristal.temperatuurOmgeving = Mathf.Round(Mathf.Lerp(Tmin, Tmax, Mathf.InverseLerp(schuif.xMin, schuif.xMax, p.x)));
        }

        // ---------- tekenen ----------
        void Stijlen()
        {
            schaal = Screen.dpi > 0 ? Mathf.Clamp(Screen.dpi / 160f, 1f, 4f) : Mathf.Max(1f, Screen.height / 900f);
            int f = Mathf.RoundToInt(14 * schaal);
            tekst = new GUIStyle(GUI.skin.label) { fontSize = f, richText = true, wordWrap = true };
            tekst.normal.textColor = Color.white;
            knop = new GUIStyle(GUI.skin.box) { fontSize = f, alignment = TextAnchor.MiddleCenter };
            knop.normal.textColor = Color.white;
            knopAan = new GUIStyle(knop);
            knopAan.normal.textColor = new Color(0.95f, 0.78f, 0.42f);
        }

        void Knop(Rect vak, string label, bool aan, Action actie)
        {
            GUI.Box(vak, label, aan ? knopAan : knop);
            knoppen.Add((vak, actie));
        }

        void OnGUI()
        {
            if (kristal == null || kristal.Model == null) return;
            if (Event.current.type != EventType.Repaint) return;
            if (tekst == null) Stijlen();
            knoppen.Clear();

            var veilig = Screen.safeArea;
            float marge = 8f * schaal;
            float rij = 42f * schaal;
            float hoogte = open ? rij * 5.3f : rij * 1.3f;
            float onder = Screen.height - veilig.yMin;
            paneel = new Rect(veilig.xMin + marge, onder - hoogte - marge, veilig.width - 2 * marge, hoogte);

            GUI.color = new Color(0.04f, 0.06f, 0.12f, 0.85f);
            GUI.DrawTexture(paneel, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var m = kristal.Model;
            float x = paneel.x + marge, y = paneel.y + marge * 0.6f, w = paneel.width - 2 * marge;

            // regel 1: toestand
            double T = m.Fase.Temperatuur;
            string fase = m.Fase.Gesmolten <= 0 ? "ijs" : m.Fase.Gesmolten >= 1 ? "water" : (kristal.temperatuurOmgeving >= 0 ? "smelten" : "bevriezen");
            string status = $"<b>{Graden(T, 1)}</b> · {fase} · {Mathf.RoundToInt((float)m.Fase.Gesmolten * 100)}% gesmolten · " +
                            $"{Komma(m.BruggenPerMolecuul, "0.0")} H-bruggen · ρ {Komma(m.Dichtheid, "0.000")}";
            Knop(new Rect(x + w - rij * 1.4f, y, rij * 1.4f, rij * 0.9f), open ? "▼" : "▲", false, () => open = !open);
            GUI.Label(new Rect(x, y, w - rij * 1.6f, rij * 1.2f), status, tekst);
            if (!open) { schuif = Rect.zero; return; }
            y += rij * 1.2f;

            // regel 2: temperatuur van de omgeving (schuif)
            GUI.Label(new Rect(x, y, w * 0.34f, rij), "Omgeving <b>" + Graden(kristal.temperatuurOmgeving, 0) + "</b>", tekst);
            schuif = new Rect(x + w * 0.36f, y, w * 0.64f, rij * 0.9f);
            float spoorY = schuif.y + schuif.height / 2f;
            GUI.color = new Color(1f, 1f, 1f, 0.25f);
            GUI.DrawTexture(new Rect(schuif.x, spoorY - 2f * schaal, schuif.width, 4f * schaal), Texture2D.whiteTexture);
            float nul = Mathf.InverseLerp(Tmin, Tmax, 0f) * schuif.width + schuif.x;
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            GUI.DrawTexture(new Rect(nul - 1f * schaal, spoorY - 8f * schaal, 2f * schaal, 16f * schaal), Texture2D.whiteTexture);
            float duim = Mathf.InverseLerp(Tmin, Tmax, kristal.temperatuurOmgeving) * schuif.width + schuif.x;
            GUI.color = kristal.temperatuurOmgeving < 0 ? new Color(0.45f, 0.75f, 1f) : new Color(1f, 0.6f, 0.4f);
            GUI.DrawTexture(new Rect(duim - 9f * schaal, spoorY - 9f * schaal, 18f * schaal, 18f * schaal), Texture2D.whiteTexture);
            GUI.color = Color.white;
            y += rij;

            // regel 3: snelknoppen
            float kb = (w - marge * (Snelknoppen.Length - 1)) / Snelknoppen.Length;
            for (int i = 0; i < Snelknoppen.Length; i++)
            {
                int t = Snelknoppen[i];
                Knop(new Rect(x + i * (kb + marge), y, kb, rij * 0.9f), Graden(t, 0), Mathf.Approximately(kristal.temperatuurOmgeving, t),
                     () => kristal.temperatuurOmgeving = t);
            }
            y += rij;

            // regel 4: weergave
            float bb = (w - marge * 3) / 4;
            Knop(new Rect(x, y, bb, rij * 0.9f), kristal.oneindig ? "Oneindig" : "Los blok", true, Wissel);
            Knop(new Rect(x + (bb + marge), y, bb, rij * 0.9f), "Kalotten", kristal.kalotten, () => kristal.kalotten = !kristal.kalotten);
            Knop(new Rect(x + 2 * (bb + marge), y, bb, rij * 0.9f), "H-bruggen", kristal.toonBruggen, () => kristal.toonBruggen = !kristal.toonBruggen);
            Knop(new Rect(x + 3 * (bb + marge), y, bb, rij * 0.9f), "Beweging", kristal.beweging, () => kristal.beweging = !kristal.beweging);
            y += rij;

            GUI.Label(new Rect(x, y, w, rij), "<size=" + Mathf.RoundToInt(11 * schaal) + "><color=#9fb3c2>Model, niet op schaal: smelten gaat veel sneller dan echt; trilling 2× vergroot.</color></size>", tekst);
        }

        void Wissel()
        {
            kristal.oneindig = !kristal.oneindig;
            if (vliegcamera == null) return;
            if (kristal.oneindig) vliegcamera.Zet(Vector3.zero, new Vector3(0, 0, 1));
            else vliegcamera.Zet(kristal.BlokKijkpunt, Vector3.zero);
        }

        static string Komma(double v, string formaat) => v.ToString(formaat, System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

        static string Graden(double t, int decimalen)
        {
            string s = Komma(Math.Abs(t), decimalen == 0 ? "0" : "0.0");
            return (t < -0.05 ? "−" : t > 0.05 && decimalen == 0 ? "+" : "") + s + " °C";
        }
    }
}
