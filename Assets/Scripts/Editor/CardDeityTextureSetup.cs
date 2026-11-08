#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CardDeityTextureSetup
{
    static CardDeityTextureSetup()
    {
        EditorApplication.delayCall += EnsureCardDeitySprites;
    }

    [MenuItem("Tools/Setup Card Deity Sprites")]
    public static void EnsureCardDeitySprites()
    {
        string[] deityFiles = new string[]
        {
            "Assets/Resources/UI/CardDeities/CardDeity_BladeSaint.png",
            "Assets/Resources/UI/CardDeities/CardDeity_BloodBeast.png",
            "Assets/Resources/UI/CardDeities/CardDeity_CelestialAstral.png",
            "Assets/Resources/UI/CardDeities/CardDeity_IronTitan.png",
            "Assets/Resources/UI/CardDeities/CardDeity_VoidPhantom.png"
        };

        bool anyModified = false;
        foreach (var path in deityFiles)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool needReimport = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    needReimport = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    needReimport = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    needReimport = true;
                }
                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    needReimport = true;
                }
                if (importer.maxTextureSize < 1024)
                {
                    importer.maxTextureSize = 1024;
                    needReimport = true;
                }

                if (needReimport)
                {
                    importer.SaveAndReimport();
                    anyModified = true;
                    Debug.Log($"[CardDeitySetup] Converted {path} to Sprite (2D and UI)");
                }
            }
        }

        if (anyModified)
        {
            AssetDatabase.Refresh();
        }
    }
}
#endif
