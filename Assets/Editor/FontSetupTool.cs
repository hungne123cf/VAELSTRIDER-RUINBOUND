using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

[InitializeOnLoad]
public static class FontSetupTool
{
    static FontSetupTool()
    {
        EditorApplication.delayCall += RunSetupOnce;
    }

    [MenuItem("Tools/Fonts/Generate Dark Fantasy Fonts")]
    public static void RunSetupOnce()
    {
        try
        {
            Debug.Log("<color=cyan>[FontSetupTool]</color> Starting Dark Fantasy Font Asset generation...");

            string resourcesFontsDir = "Assets/Resources/Fonts";
            if (!Directory.Exists(resourcesFontsDir))
            {
                Directory.CreateDirectory(resourcesFontsDir);
                AssetDatabase.Refresh();
            }

            // 1. Load TTF Fonts
            Font cinzelTtf = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Cinzel-Variable.ttf");
            Font beVietnamTtf = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BeVietnamPro-SemiBold.ttf");

            if (cinzelTtf == null || beVietnamTtf == null)
            {
                Debug.LogWarning("<color=yellow>[FontSetupTool]</color> TTF font files not loaded yet. Retrying on next tick...");
                return;
            }

            string beVietnamAssetPath = "Assets/Resources/Fonts/BeVietnamPro SDF.asset";
            string cinzelAssetPath = "Assets/Resources/Fonts/Cinzel SDF.asset";
            string vaelstriderAssetPath = "Assets/Resources/Fonts/VaelstriderFont.asset";

            // Check if already created
            TMP_FontAsset beVietnamSdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(beVietnamAssetPath);
            TMP_FontAsset cinzelSdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(cinzelAssetPath);

            bool createdBeVietnam = false;
            bool createdCinzel = false;

            if (beVietnamSdf == null)
            {
                Debug.Log("<color=cyan>[FontSetupTool]</color> Creating BeVietnamPro Dynamic SDF...");
                beVietnamSdf = TMP_FontAsset.CreateFontAsset(
                    beVietnamTtf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true
                );
                beVietnamSdf.name = "BeVietnamPro SDF";
                AssetDatabase.CreateAsset(beVietnamSdf, beVietnamAssetPath);
                if (beVietnamSdf.material != null)
                {
                    AssetDatabase.AddObjectToAsset(beVietnamSdf.material, beVietnamSdf);
                }
                createdBeVietnam = true;
            }

            if (cinzelSdf == null)
            {
                Debug.Log("<color=cyan>[FontSetupTool]</color> Creating Cinzel Dynamic SDF...");
                cinzelSdf = TMP_FontAsset.CreateFontAsset(
                    cinzelTtf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true
                );
                cinzelSdf.name = "Cinzel SDF";
                AssetDatabase.CreateAsset(cinzelSdf, cinzelAssetPath);
                if (cinzelSdf.material != null)
                {
                    AssetDatabase.AddObjectToAsset(cinzelSdf.material, cinzelSdf);
                }
                createdCinzel = true;
            }

            // Setup fallbacks
            if (cinzelSdf != null && beVietnamSdf != null)
            {
                if (cinzelSdf.fallbackFontAssetTable == null)
                    cinzelSdf.fallbackFontAssetTable = new List<TMP_FontAsset>();

                if (!cinzelSdf.fallbackFontAssetTable.Contains(beVietnamSdf))
                {
                    cinzelSdf.fallbackFontAssetTable.Add(beVietnamSdf);
                    EditorUtility.SetDirty(cinzelSdf);
                }

                // Create or update VaelstriderFont copy so all existing scripts load it instantly
                TMP_FontAsset vaelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(vaelstriderAssetPath);
                if (vaelFont == null)
                {
                    AssetDatabase.CopyAsset(beVietnamAssetPath, vaelstriderAssetPath);
                }

                // Add to LiberationSans fallbacks so any existing UI with LiberationSans SDF uses BeVietnamPro
                TMP_FontAsset libSdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (libSdf != null)
                {
                    if (libSdf.fallbackFontAssetTable == null)
                        libSdf.fallbackFontAssetTable = new List<TMP_FontAsset>();

                    if (!libSdf.fallbackFontAssetTable.Contains(beVietnamSdf))
                    {
                        libSdf.fallbackFontAssetTable.Insert(0, beVietnamSdf);
                        EditorUtility.SetDirty(libSdf);
                    }
                    if (!libSdf.fallbackFontAssetTable.Contains(cinzelSdf))
                    {
                        libSdf.fallbackFontAssetTable.Insert(1, cinzelSdf);
                        EditorUtility.SetDirty(libSdf);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[FontSetupTool] SUCCESS!</color> Dark Fantasy fonts (Cinzel + BeVietnamPro) generated and configured!");
        }
        catch (Exception ex)
        {
            Debug.LogError("[FontSetupTool] Error during font generation: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}
