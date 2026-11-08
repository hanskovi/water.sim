using UnityEngine;
using UnityEngine.InputSystem;
using WaterSim.Weergave;

namespace WaterSim.UI
{
    /// <summary>
    /// Kleine meter linksboven voor tijdens het ontwikkelen: beelden per seconde, tijd per beeld,
    /// rekentijd van het kristal en het aantal getekende moleculen.
    /// Aan/uit: toets F, of met drie vingers tikken. Staat standaard alleen aan in de editor
    /// en in development builds.
    /// </summary>
    public class Snelheidsmeter : MonoBehaviour
    {
        public Kristal kristal;
        public bool zichtbaar = true;

        float gemiddeld = 1f / 60f, slechtste;
        float slechtsteTimer;
        GUIStyle stijl;

        void Start()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) zichtbaar = false;
            if (kristal == null) kristal = FindAnyObjectByType<Kristal>();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            gemiddeld = Mathf.Lerp(gemiddeld, dt, 0.05f);
            slechtste = Mathf.Max(slechtste, dt);
            if ((slechtsteTimer += dt) > 2f) { slechtsteTimer = 0f; slechtste = dt; }

            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) zichtbaar = !zichtbaar;
            var scherm = Touchscreen.current;
            if (scherm != null)
            {
                int vingers = 0, nieuw = 0;
                foreach (var t in scherm.touches)
                {
                    if (t.press.isPressed) vingers++;
                    if (t.press.wasPressedThisFrame) nieuw++;
                }
                if (vingers == 3 && nieuw > 0) zichtbaar = !zichtbaar;
            }
        }

        void OnGUI()
        {
            if (!zichtbaar) return;
            if (stijl == null)
            {
                stijl = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true };
                stijl.normal.textColor = Color.white;
            }
            float schaal = Mathf.Max(1f, Screen.dpi / 160f);
            stijl.fontSize = Mathf.RoundToInt(13 * schaal);
            float fps = 1f / Mathf.Max(gemiddeld, 1e-4f);
            string kleur = fps >= 55 ? "#7be07a" : fps >= 30 ? "#f2c64a" : "#ff6b6b";
            string tekst = $"<color={kleur}><b>{fps:0} fps</b></color>  {gemiddeld * 1000f:0.0} ms  (traagst {slechtste * 1000f:0} ms)";
            if (kristal != null)
                tekst += $"\nkristal: {kristal.RekentijdMs:0.0} ms rekenen · {kristal.ZichtbareMoleculen} moleculen";
            var maat = stijl.CalcSize(new GUIContent(tekst.Replace("<b>", "").Replace("</b>", "")));
            float marge = 10f * schaal + Screen.safeArea.xMin;
            GUI.Box(new Rect(marge, 10f * schaal + (Screen.height - Screen.safeArea.yMax), maat.x + 16 * schaal, maat.y + 10 * schaal), tekst, stijl);
        }
    }
}
