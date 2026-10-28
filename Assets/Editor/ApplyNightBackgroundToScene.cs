using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ApplyNightBackgroundToScene
{
    static ApplyNightBackgroundToScene()
    {
        EditorApplication.update += RunOnce;
    }

    [InitializeOnLoadMethod]
    private static void AutoInit()
    {
        EditorApplication.update += RunOnce;
    }

    private static void RunOnce()
    {
        EditorApplication.update -= RunOnce;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        ExecuteUpdate();
    }

    [MenuItem("Tools/Vaelstrider/Update Main Menu Scene In Editor")]
    public static void ExecuteUpdate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            activeScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenuScene.unity");
        }

        // 1. Load WCP_Example sprite
        string spritePath = "Assets/ART/Backgrounds/Night/WCP_Example.png";
        Sprite wcpSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (wcpSprite == null)
        {
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            if (subAssets != null)
            {
                foreach (var a in subAssets)
                {
                    if (a is Sprite s)
                    {
                        wcpSprite = s;
                        break;
                    }
                }
            }
        }

        if (wcpSprite == null)
        {
            Debug.LogWarning("[ApplyNightBackgroundToScene] Could not find WCP_Example sprite in " + spritePath);
            return;
        }

        bool modified = false;

        // 2. Find and update background images
        var allImages = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var img in allImages)
        {
            if (img == null) continue;
            string objName = img.gameObject.name;
            if (objName == "EpicBgArtwork" || objName == "RuinedCityArtwork" || (img.sprite != null && img.sprite.name.Contains("mainmenu_bg_epic")))
            {
                if (img.sprite != wcpSprite || img.color != Color.white)
                {
                    Undo.RecordObject(img, "Set Night Background Sprite");
                    img.sprite = wcpSprite;
                    img.color = Color.white;
                    img.preserveAspect = false;
                    EditorUtility.SetDirty(img);
                    modified = true;
                    Debug.Log($"<color=green>[ApplyNightBackgroundToScene]</color> Updated {objName} sprite to {wcpSprite.name}!");
                }
            }
        }

        // 3. Update MainMenuDynamicBackground component
        var dynBgs = Object.FindObjectsByType<MainMenuDynamicBackground>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var dbg in dynBgs)
        {
            if (dbg == null) continue;
            EditorUtility.SetDirty(dbg);
            modified = true;
        }

        // 4. In BottomNavBar, hide obsolete buttons in Edit Mode (Heroes, Shop, Inventory, Skills)
        var bottomNav = GameObject.Find("BottomNavBar");
        if (bottomNav != null)
        {
            for (int i = 0; i < bottomNav.transform.childCount; i++)
            {
                Transform child = bottomNav.transform.GetChild(i);
                string cName = child.name.ToLower();
                // If it's Hero, Shop, Inventory, Costume, or Skill -> hide it in Edit Mode!
                if (cName.Contains("hero") || cName.Contains("shop") || cName.Contains("inventory") || cName.Contains("bag") || cName.Contains("costume") || cName.Contains("skill"))
                {
                    if (child.gameObject.activeSelf)
                    {
                        Undo.RecordObject(child.gameObject, "Hide Obsolete Button");
                        child.gameObject.SetActive(false);
                        EditorUtility.SetDirty(child.gameObject);
                        modified = true;
                    }
                }
            }
        }

        // 5. Save scene
        if (modified)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=cyan>[ApplyNightBackgroundToScene]</color> Saved MainMenuScene.unity with Night city background and clean UI!");
        }
    }
}
