using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace WaterSim.Editor
{
    /// <summary>
    /// Menu Water.sim › Zet app-iconen: stelt de iconen uit Assets/Icoon in voor alle platforms.
    /// - Standaard en iOS: icoon_1024.png (vierkant, zonder doorzichtigheid; iOS rondt zelf af).
    /// - Android adaptief: android_achtergrond.png + android_voorgrond.png.
    /// - Android rond: android_rond.png; Android gewoon: icoon_1024.png.
    /// De PNG's maak je met: python3 Tools/Icoon/maak_icoon.py
    /// </summary>
    public static class Iconen
    {
        const string Map = "Assets/Icoon/";

        [MenuItem("Water.sim/Zet app-iconen")]
        public static void Zet()
        {
            var vierkant = Laad("icoon_1024.png");
            var achter = Laad("android_achtergrond.png");
            var voor = Laad("android_voorgrond.png");
            var rond = Laad("android_rond.png");
            if (vierkant == null) return;

            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { vierkant }, IconKind.Any);

            int gezet = 0;
            foreach (var soort in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                var iconen = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, soort);
                string naam = soort.ToString().ToLowerInvariant();
                foreach (var ic in iconen)
                {
                    if (naam.Contains("adaptive") && achter != null && voor != null) ic.SetTextures(achter, voor);
                    else if (naam.Contains("round") && rond != null) ic.SetTexture(rond);
                    else ic.SetTexture(vierkant);
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, soort, iconen);
                gezet += iconen.Length;
            }

            var ios = PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS);
            foreach (var soort in ios)
            {
                var iconen = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, soort);
                foreach (var ic in iconen) ic.SetTexture(vierkant);
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, soort, iconen);
                gezet += iconen.Length;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Water.sim: app-iconen ingesteld ({gezet} plekken voor Android en iOS, plus het standaardicoon).");
        }

        static Texture2D Laad(string bestand)
        {
            string pad = Map + bestand;
            var imp = AssetImporter.GetAtPath(pad) as TextureImporter;
            if (imp == null) { Debug.LogError("Water.sim: icoon niet gevonden: " + pad + " (draai Tools/Icoon/maak_icoon.py)"); return null; }
            // iconen: geen compressie, geen mipmaps, volle maat
            bool aangepast = imp.textureCompression != TextureImporterCompression.Uncompressed || imp.mipmapEnabled || imp.maxTextureSize < 1024 || imp.npotScale != TextureImporterNPOTScale.None;
            if (aangepast)
            {
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = 1024;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(pad);
        }
    }
}
