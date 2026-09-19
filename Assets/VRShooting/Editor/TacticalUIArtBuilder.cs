using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using VRShooting.Unity.UI;

namespace VRShooting.Editor
{
    public static class TacticalUIArtBuilder
    {
        const string Folder = "Assets/Resources/UI/Tactical";
        [MenuItem("Tools/VR Shooting/Rebuild Tactical UI Art")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var theme = AssetDatabase.LoadAssetAtPath<TacticalUITheme>(Folder + "/Theme.asset");
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<TacticalUITheme>();
                AssetDatabase.CreateAsset(theme, Folder + "/Theme.asset");
            }
            theme.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "/TrainingChinese.asset");
            if (theme.Font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/VRShooting/Art/Fonts/MSYH.TTC");
                if (source == null) throw new System.InvalidOperationException("Missing project Chinese source font.");
                theme.Font = TMP_FontAsset.CreateFontAsset(source, 48, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                theme.Font.name = "TrainingChinese";
                AssetDatabase.CreateAsset(theme.Font, Folder + "/TrainingChinese.asset");
                AssetDatabase.AddObjectToAsset(theme.Font.material, theme.Font);
                foreach (var texture in theme.Font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, theme.Font);
            }
            theme.Hall = ImportSprite(Folder + "/hall-background.png");
            theme.Trench = ImportSprite("Assets/SimulatedShooting/Art/Combat/trench-a.png");
            theme.Urban = ImportSprite("Assets/SimulatedShooting/Art/Combat/urban-a.png");
            theme.Floors = new Sprite[3];
            for (var i = 0; i < 3; i++) theme.Floors[i] = ImportSprite("Assets/SimulatedShooting/Art/Combat/urban-" + (i + 1) + "f.png");
            EditorUtility.SetDirty(theme);
            EditorUtility.SetDirty(theme.Font);
            AssetDatabase.SaveAssets();
        }
        static Sprite ImportSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new System.InvalidOperationException("Missing UI artwork: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
