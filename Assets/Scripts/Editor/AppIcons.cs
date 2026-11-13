using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CoopGame.EditorTools
{
    /// <summary>
    /// Uygulama ikonu (Assets/Icons/AppIcon.png, 1024x1024 opak): Default Icon (Android + iOS) ve iOS App Store (Marketing 1024).
    /// Editör yüklenince eksikse kendiliğinden uygular ve ProjectSettings'e yazar (bulut build ProjectSettings'i kullanır).
    /// </summary>
    [InitializeOnLoad]
    public static class AppIcons
    {
        const string IconPath = "Assets/Icons/AppIcon.png";

        static AppIcons() => EditorApplication.delayCall += () => Apply(false);

        [MenuItem("CoopGame/App İkonunu Ayarla")]
        static void Menu() => Apply(true);

        public static void Apply(bool force)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (tex == null) return;

            var imp = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (imp != null && (imp.mipmapEnabled || imp.npotScale != TextureImporterNPOTScale.None || imp.alphaSource != TextureImporterAlphaSource.None))
            {
                imp.textureType = TextureImporterType.Default;
                imp.mipmapEnabled = false;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.alphaSource = TextureImporterAlphaSource.None;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            }

            bool changed = false;
            var def = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            if (force || def.Length == 0 || def[0] != tex)
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { tex }, IconKind.Any);
                changed = true;
            }

            // iOS App Store 1024 (Marketing). Tür adıyla bulunur: iOS modülüne derleme bağımlılığı olmasın.
            var marketing = PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS).FirstOrDefault(k => k.ToString().Contains("Marketing"));
            if (marketing != null)
            {
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, marketing);
                foreach (var ic in icons)
                {
                    var cur = ic.GetTextures();
                    if (!force && cur.Length > 0 && cur[0] == tex) continue;
                    ic.SetTextures(tex);
                    changed = true;
                }
                if (changed) PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, marketing, icons);
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[AppIcons] İkon ayarlandı: " + IconPath + " (" + tex.width + "x" + tex.height + ")");
            }
        }
    }
}
