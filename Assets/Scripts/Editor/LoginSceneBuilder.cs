using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class LoginSceneBuilder
{
    private const string LOGIN_SCENE_PATH = "Assets/Scenes/LoginScene.unity";
    private const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/MainMenuScene.unity";
    private const string MAP1_SCENE_PATH = "Assets/Scenes/SampleScene.unity";
    private const string MAP2_SCENE_PATH = "Assets/Scenes/Map2_ValleyRuin.unity";
    private const string TRIGGER_FILE = "Temp/build_login.trigger";

    static LoginSceneBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch {}
                BuildLoginScene();
            }
        };
    }

    [MenuItem("Vaelstrider/Scenes/Build Login Scene (LoginScene.unity)", false, 0)]
    public static void BuildLoginScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // 1. Create a fresh scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Camera Setup
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camObj.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();
        camObj.AddComponent<UniversalAdditionalCameraData>();

        // 3. Ambient Lighting & Subtle Embers
        GameObject lightObj = new GameObject("GlobalLight");
        var light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color(0.65f, 0.72f, 0.95f, 1f);
        light.intensity = 0.85f;

        // Floating Embers
        GameObject particlesObj = new GameObject("MysticEmbers");
        var ps = particlesObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 6f;
        main.startSpeed = 0.6f;
        main.startSize = 0.12f;
        main.startColor = new Color(1f, 0.85f, 0.4f, 0.7f);
        main.maxParticles = 60;
        var emission = ps.emission;
        emission.rateOverTime = 8f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(16f, 1f, 1f);
        particlesObj.transform.position = new Vector3(0f, -6f, 0f);
        var psRenderer = particlesObj.GetComponent<ParticleSystemRenderer>(); if (psRenderer != null) psRenderer.material = HeavenlyPortalFX.GetOrCreateParticleMaterial();

        // 4. UI Canvas & EventSystem
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 5. Background Vignette / Gradient
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.05f, 0.08f, 1f);

        // 6. LoginManager Controller
        var loginMgr = canvasObj.AddComponent<LoginManager>();

        // 7. Header Game Logo / Title
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.5f, 0.5f);
        headerRt.anchorMax = new Vector2(0.5f, 0.5f);
        headerRt.pivot = new Vector2(0.5f, 0.5f);
        headerRt.anchoredPosition = new Vector2(0f, 230f);
        headerRt.sizeDelta = new Vector2(900f, 130f);

        // Title text "VAELSTRIDER"
        GameObject titleObj = new GameObject("GameTitle");
        titleObj.transform.SetParent(headerObj.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 25f);
        titleRt.sizeDelta = new Vector2(850f, 80f);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "VAELSTRIDER";
        titleTmp.fontSize = 68;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(1f, 0.85f, 0.25f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.raycastTarget = false;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(headerObj.transform, false);
        RectTransform subRt = subObj.AddComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0f, -32f);
        subRt.sizeDelta = new Vector2(850f, 35f);
        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = "- RUINBOUND: KE DU HANH CO TICH -";
        subTmp.fontSize = 19;
        subTmp.fontStyle = FontStyles.Bold;
        subTmp.color = new Color(0.85f, 0.40f, 0.40f);
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.raycastTarget = false;

        // 8. Warrior Login Card (Central Box)
        GameObject cardObj = new GameObject("LoginCard");
        cardObj.transform.SetParent(canvasObj.transform, false);
        RectTransform cardRt = cardObj.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = new Vector2(0f, -40f);
        cardRt.sizeDelta = new Vector2(620f, 360f);
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);

        var outline = cardObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.42f, 0.55f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        // 8A. Warrior Name Label
        GameObject inputLblObj = new GameObject("InputLabel");
        inputLblObj.transform.SetParent(cardObj.transform, false);
        RectTransform inLblRt = inputLblObj.AddComponent<RectTransform>();
        inLblRt.anchoredPosition = new Vector2(0f, 115f);
        inLblRt.sizeDelta = new Vector2(460f, 30f);
        var inLblTmp = inputLblObj.AddComponent<TextMeshProUGUI>();
        inLblTmp.text = "TEN CHIEN BINH:";
        inLblTmp.fontSize = 17;
        inLblTmp.fontStyle = FontStyles.Bold;
        inLblTmp.color = new Color(0.85f, 0.90f, 0.98f);
        inLblTmp.alignment = TextAlignmentOptions.Center;
        inLblTmp.raycastTarget = false;

        // Input Field Box (460x54)
        GameObject inputObj = new GameObject("NameInputField");
        inputObj.transform.SetParent(cardObj.transform, false);
        RectTransform inRt = inputObj.AddComponent<RectTransform>();
        inRt.anchoredPosition = new Vector2(0f, 65f);
        inRt.sizeDelta = new Vector2(460f, 54f);
        Image inBg = inputObj.AddComponent<Image>();
        inBg.color = new Color(0.14f, 0.16f, 0.23f);
        inBg.raycastTarget = true;

        var inOutline = inputObj.AddComponent<Outline>();
        inOutline.effectColor = new Color(0.4f, 0.55f, 0.75f, 0.8f);
        inOutline.effectDistance = new Vector2(1.5f, -1.5f);

        var inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.targetGraphic = inBg;

        GameObject inTextObj = new GameObject("Text");
        inTextObj.transform.SetParent(inputObj.transform, false);
        RectTransform inTxtRt = inTextObj.AddComponent<RectTransform>();
        inTxtRt.anchorMin = Vector2.zero;
        inTxtRt.anchorMax = Vector2.one;
        inTxtRt.offsetMin = new Vector2(16f, 0f);
        inTxtRt.offsetMax = new Vector2(-16f, 0f);
        var inTxtTmp = inTextObj.AddComponent<TextMeshProUGUI>();
        inTxtTmp.text = "Vaelstrider";
        inTxtTmp.fontSize = 20;
        inTxtTmp.fontStyle = FontStyles.Bold;
        inTxtTmp.color = Color.white;
        inTxtTmp.alignment = TextAlignmentOptions.Center;
        inTxtTmp.raycastTarget = false;

        inputField.textViewport = inTxtRt;
        inputField.textComponent = inTxtTmp;
        inputField.interactable = true;

        // 8B. Main Play Button "BAT DAU HANH TRINH"
        var playBtn = CreateBigButton(cardObj.transform, "Btn_Play", "BAT DAU HANH TRINH", new Vector2(0f, -25f), new Color(0.85f, 0.50f, 0.15f), out TextMeshProUGUI playTxt);

        // 8C. Secondary Button: "THOAT GAME"
        var quitBtn = CreateSmallButton(cardObj.transform, "Btn_Quit", "THOAT GAME", new Vector2(0f, -100f), new Color(0.55f, 0.20f, 0.20f), out TextMeshProUGUI quitTxt);

        // 9. Link all references into LoginManager
        SerializedObject so = new SerializedObject(loginMgr);
        so.FindProperty("nameInput").objectReferenceValue = inputField;
        so.FindProperty("playButton").objectReferenceValue = playBtn;
        so.FindProperty("quitButton").objectReferenceValue = quitBtn;
        so.FindProperty("titleText").objectReferenceValue = titleTmp;
        so.FindProperty("subtitleText").objectReferenceValue = subTmp;
        so.FindProperty("playButtonText").objectReferenceValue = playTxt;
        so.FindProperty("quitButtonText").objectReferenceValue = quitTxt;
        so.FindProperty("warriorNameLabel").objectReferenceValue = inLblTmp;
        so.ApplyModifiedProperties();

        // 10. Register all 4 scenes in Build Settings (0: Login, 1: MainMenu, 2: SampleScene, 3: Map2)
        EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(LOGIN_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAIN_MENU_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAP1_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAP2_SCENE_PATH, true)
        };
        EditorBuildSettings.scenes = buildScenes;

        // 11. Save the scene
        string dir = Path.GetDirectoryName(LOGIN_SCENE_PATH);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        EditorSceneManager.SaveScene(scene, LOGIN_SCENE_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[LOGIN BUILDER]</color> Da tao thanh cong LoginScene (LoginScene.unity) sach se, khong bi khuat va hoan toan hoat dong 100%!");
    }

    private static Button CreateBigButton(Transform parent, string name, string text, Vector2 pos, Color bgColor, out TextMeshProUGUI btnTxt)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(460f, 60f);

        var img = obj.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        var btn = obj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(obj.transform, false);
        var tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        btnTxt = txtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = text;
        btnTxt.fontSize = 22;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.raycastTarget = false;

        return btn;
    }

    private static Button CreateSmallButton(Transform parent, string name, string text, Vector2 pos, Color bgColor, out TextMeshProUGUI btnTxt)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(460f, 48f);

        var img = obj.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        var btn = obj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(obj.transform, false);
        var tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        btnTxt = txtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = text;
        btnTxt.fontSize = 17;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.raycastTarget = false;

        return btn;
    }
}
