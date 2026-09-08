using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using System.IO;

[InitializeOnLoad]
public class MainMenuSceneBuilder
{
    public const string LOGIN_SCENE_PATH = "Assets/Scenes/LoginScene.unity";
    public const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/MainMenuScene.unity";
    public const string MAP1_SCENE_PATH = "Assets/Scenes/SampleScene.unity";
    public const string MAP2_SCENE_PATH = "Assets/Scenes/Map2_ValleyRuin.unity";

    static MainMenuSceneBuilder()
    {
        EditorApplication.delayCall += CheckAndBuildMainMenuScene;
    }

    [MenuItem("Vaelstrider/Scenes/Create or Update Main Menu Scene")]
    public static void CheckAndBuildMainMenuScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(MAIN_MENU_SCENE_PATH))
        {
            BuildMainMenuScene();
        }
        else
        {
            UpdateBuildSettings();
        }
    }

    public static void BuildMainMenuScene()
    {
        Debug.Log("<color=cyan>[MAIN MENU]</color> Đang khởi tạo Scene: " + MAIN_MENU_SCENE_PATH);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Camera
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camObj.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();

        // 2. EventSystem
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<StandaloneInputModule>();

        // 3. MainMenuManager
        GameObject mgrObj = new GameObject("MainMenuManager");
        mgrObj.AddComponent<MainMenuManager>();

        EditorSceneManager.SaveScene(scene, MAIN_MENU_SCENE_PATH);
        UpdateBuildSettings();

        Debug.Log("<color=green>[MAIN MENU]</color> Đã tạo và cấu hình thành công MainMenuScene!");
    }

    public static void UpdateBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(LOGIN_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAIN_MENU_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAP1_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAP2_SCENE_PATH, true)
        };
        EditorBuildSettings.scenes = scenes;
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Vaelstrider/Scenes/Open Main Menu Scene")]
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
