using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WaterSim.Besturing;
using WaterSim.Weergave;

namespace WaterSim.Editor
{
    /// <summary>
    /// Menu Water.sim › Maak kristalscène: bouwt de scène Assets/Scenes/Kristal.unity op met camera,
    /// licht, bloom (voor de neon-H-bruggen), nevel en het kristal. Materialen komen in Assets/Materials.
    /// Opnieuw uitvoeren mag: bestaande materialen worden bijgewerkt, de scène wordt opnieuw gemaakt.
    /// </summary>
    public static class Scenemaker
    {
        const string ScenePad = "Assets/Scenes/Kristal.unity";
        const string VolumePad = "Assets/Settings/KristalVolume.asset";
        static readonly Color Achtergrond = new Color(0.039f, 0.082f, 0.125f);

        [MenuItem("Water.sim/Maak kristalscène")]
        public static void Maak()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (lit == null || unlit == null) { Debug.LogError("Water.sim: URP-shaders niet gevonden."); return; }

            var zuurstof = Materiaal("Zuurstof", lit, new Color(0.85f, 0.27f, 0.23f), 0.55f);
            var kanaal = Materiaal("Kanaal", lit, new Color(0.95f, 0.66f, 0.23f), 0.55f);
            var waterstof = Materiaal("Waterstof", lit, new Color(0.86f, 0.88f, 0.90f), 0.45f);
            var staaf = Materiaal("Binding", lit, new Color(0.70f, 0.74f, 0.77f), 0.30f);
            // HDR-blauw (> 1) zodat alleen de H-bruggen door de bloom gaan gloeien
            var brug = Materiaal("Waterstofbrug", unlit, new Color(0.30f, 0.72f, 2.6f, 0.85f), 0f);
            Doorzichtig(brug);

            var profiel = Bloomprofiel();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Achtergrond;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 65f;
            cam.allowHDR = true;
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camGo.AddComponent<Vliegcamera>();
            camGo.transform.position = Vector3.zero;   // midden in een kanaal, kijkend langs de c-as

            var zon = new GameObject("Licht").AddComponent<Light>();
            zon.type = LightType.Directional;
            zon.intensity = 1.1f;
            zon.shadows = LightShadows.None;
            zon.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var vol = new GameObject("Bloom").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profiel;

            var k = new GameObject("Kristal").AddComponent<Kristal>();
            k.bol = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            k.cilinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            k.capsule = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
            k.zuurstof = zuurstof; k.kanaal = kanaal; k.waterstof = waterstof; k.staaf = staaf; k.brug = brug;
            k.achtergrond = Achtergrond;

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.47f, 0.55f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Achtergrond;
            RenderSettings.fogStartDistance = 6f;
            RenderSettings.fogEndDistance = 14f;

            EditorSceneManager.SaveScene(scene, ScenePad);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePad, true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = k.gameObject;
            Debug.Log("Water.sim: kristalscène gemaakt (" + ScenePad + "). Druk op Play om door het ijs te vliegen.");
        }

        static Material Materiaal(string naam, Shader shader, Color kleur, float glad)
        {
            string pad = "Assets/Materials/" + naam + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(pad);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, pad); }
            m.shader = shader;
            m.SetColor("_BaseColor", kleur);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", glad);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Doorzichtig(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
        }

        static VolumeProfile Bloomprofiel()
        {
            var profiel = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePad);
            if (profiel == null)
            {
                profiel = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profiel, VolumePad);
            }
            if (!profiel.TryGet<Bloom>(out var bloom))
            {
                bloom = profiel.Add<Bloom>(true);
                AssetDatabase.AddObjectToAsset(bloom, profiel);
            }
            bloom.active = true;
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(1.6f);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(0.6f, 0.8f, 1f));
            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(profiel);
            return profiel;
        }
    }
}
