using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

public static class FontSetupTool
{
    [MenuItem("Tools/Fonts/Generate Dark Fantasy Fonts")]
    public static void GenerateFontAssets()
    {
        try
        {
            Debug.Log("<color=cyan>[FontSetupTool]</color> Generating Dark Fantasy Font Assets with proper Atlas Textures...");

            string resourcesFontsDir = "Assets/Resources/Fonts";
            if (!Directory.Exists(resourcesFontsDir))
            {
                Directory.CreateDirectory(resourcesFontsDir);
                AssetDatabase.Refresh();
            }

            // 1. Load TTF Fonts
            Font cinzelTtf = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/Cinzel-Variable.ttf");
            Font beVietnamTtf = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/BeVietnamPro-SemiBold.ttf");

            if (cinzelTtf == null || beVietnamTtf == null)
            {
                Debug.LogWarning("<color=yellow>[FontSetupTool]</color> TTF font files not loaded yet.");
                return;
            }

            string beVietnamAssetPath = "Assets/Resources/Fonts/BeVietnamPro SDF.asset";
            string cinzelAssetPath = "Assets/Resources/Fonts/Cinzel SDF.asset";
            string vaelstriderAssetPath = "Assets/Resources/Fonts/VaelstriderFont.asset";

            // Delete old broken assets if any
            AssetDatabase.DeleteAsset(beVietnamAssetPath);
            AssetDatabase.DeleteAsset(cinzelAssetPath);
            AssetDatabase.DeleteAsset(vaelstriderAssetPath);
            AssetDatabase.Refresh();

            // Create BeVietnamPro
            TMP_FontAsset beVietnamSdf = CreateAndSaveFontAsset(beVietnamTtf, beVietnamAssetPath, "BeVietnamPro SDF");

            // Create Cinzel
            TMP_FontAsset cinzelSdf = CreateAndSaveFontAsset(cinzelTtf, cinzelAssetPath, "Cinzel SDF");

            if (cinzelSdf != null && beVietnamSdf != null)
            {
                // Fallback: Cinzel -> BeVietnamPro
                cinzelSdf.fallbackFontAssetTable = new List<TMP_FontAsset> { beVietnamSdf };
                EditorUtility.SetDirty(cinzelSdf);

                // Copy BeVietnamPro as VaelstriderFont for backward compatibility
                AssetDatabase.CopyAsset(beVietnamAssetPath, vaelstriderAssetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[FontSetupTool] SUCCESS!</color> Clean Dark Fantasy font assets created without touching engine fallbacks!");
        }
        catch (Exception ex)
        {
            Debug.LogError("[FontSetupTool] Error during font generation: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    private static TMP_FontAsset CreateAndSaveFontAsset(Font ttfFont, string assetPath, string name)
    {
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            ttfFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true
        );
        fontAsset.name = name;

        // 1. Create main asset
        AssetDatabase.CreateAsset(fontAsset, assetPath);

        // 2. Persist all atlas textures as sub-assets
        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                var tex = fontAsset.atlasTextures[i];
                if (tex != null)
                {
                    tex.name = name + " Atlas " + i;
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
                }
            }
        }

        // 3. Persist Material as sub-asset with valid _MainTex texture binding
        if (fontAsset.material != null)
        {
            fontAsset.material.name = name + " Material";
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
            {
                fontAsset.material.SetTexture(ShaderUtilities.ID_MainTex, fontAsset.atlasTextures[0]);
            }
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        return fontAsset;
    }
}
