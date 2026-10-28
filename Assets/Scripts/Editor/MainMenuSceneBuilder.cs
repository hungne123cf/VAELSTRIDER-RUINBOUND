// ========================================================
// VAELSTRIDER: RUINBOUND - Main Menu Scene Builder
// ========================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.IO;

[InitializeOnLoad]
public class MainMenuSceneBuilder
{
    public const string LOGIN_SCENE_PATH = "Assets/Scenes/LoginScene.unity";
    public const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/MainMenuScene.unity";
    public const string MAP1_SCENE_PATH = "Assets/Scenes/Map1_Thestart.unity";
    public const string MAP2_SCENE_PATH = "Assets/Scenes/Map2_ValleyRuin.unity";
    public const string MAP3_SCENE_PATH = "Assets/Scenes/Map3_ForgottenMemories.unity";

    private const string TRIGGER_FILE = "Temp/build_mainmenu.trigger";

    static MainMenuSceneBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch {}
                BuildMainMenuScene();
            }
            UpdateBuildSettings();
        };
    }

    [MenuItem("Vaelstrider/Scenes/Xay Dung & Luu UI Main Menu Truc Tiep Vao Scene")]
    public static void BuildMainMenuScene()
    {
        Debug.Log("<color=cyan>[MAIN MENU BUILDER=]</color> Dang khoi tao toan bo UI truc tiep vao Hierarchy...");

        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "MainMenuScene")
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            activeScene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
        }

        // 1. Camera
        var cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 5f;

        // 2. EventSystem with InputSystemUIInputModule (BUILT FOR UNITY 6 NEW INPUT SYSTEM)
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
            var module = esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
        else
        {
            var standalone = es.GetComponent<StandaloneInputModule>();
            if (standalone != null) Object.DestroyImmediate(standalone);
            var module = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (module == null)
            {
                module = es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            module.AssignDefaultActions();
        }

        // 3. Ensure Canvas exists and is configured for New Input System
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cObj = new GameObject("Canvas");
            canvas = cObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            cObj.AddComponent<GraphicRaycaster>();
        }

        // Remove old solid background images & HeroStage center relics directly from Canvas
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            var child = canvas.transform.GetChild(i);
            string cName = child.name.ToLower();
            if ((cName == "background" || cName == "bg" || cName == "solidbg") && cName != "dynamicbackgroundroot")
            {
                Object.DestroyImmediate(child.gameObject);
            }
            else if (cName == "herostage" || cName == "heropreview" || cName == "characterpreview" || cName == "previewframe")
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // 4. MainMenuManager
        var mgrObj = GameObject.Find("MainMenuManager");
        if (mgrObj == null)
        {
            mgrObj = new GameObject("MainMenuManager");
        }

        var mgr = mgrObj.GetComponent<MainMenuManager>();
        if (mgr == null) mgr = mgrObj.AddComponent<MainMenuManager>();

        // 5. Build & Bind Complete UI directly into Scene Hierarchy
        mgr.EnsureDynamicBackground();
        mgr.BuildMainMenuUI();
        mgr.HideCenterHeroDisplay();
        mgr.RefreshCurrencies();
        mgr.RefreshAllTexts();

        // 6. Ensure all modals (Inventory, ArmorShop, Settings, Roster, HeroSelectCarousel) exist in Scene Hierarchy
        InventoryUI.EnsureUIExists();
        ArmorShopUI.EnsureUIExists();
        SettingsUI.EnsureUIExists();
        mgr.EnsureHeroSelectCarouselModal();

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        UpdateBuildSettings();

        Debug.Log("<color=green>[MAIN MENU BUILDER]</color> Da tao va luu thanh cong toan bo UI (bao gom Armor Shop da slot, Settings, Map Select, Roster) vao Hierarchy!");
    }

    public static void UpdateBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(LOGIN_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAIN_MENU_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAP1_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAP2_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAP3_SCENE_PATH, true)
        };
        EditorBuildSettings.scenes = scenes;
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Vaelstrider/Scenes/Mo Main Menu Scene")]
    public static void OpenMainMenuScene()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        if (File.Exists(MAIN_MENU_SCENE_PATH))
        {
            EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
        }
        else
        {
            BuildMainMenuScene();
        }
    }
}
