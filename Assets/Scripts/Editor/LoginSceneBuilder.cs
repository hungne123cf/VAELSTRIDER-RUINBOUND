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
    private const string MAP1_SCENE_PATH = "Assets/Scenes/Map1_Thestart.unity";
    private const string MAP2_SCENE_PATH = "Assets/Scenes/Map2_ValleyRuin.unity";
    private const string MAP3_SCENE_PATH = "Assets/Scenes/Map3_ForgottenMemories.unity";
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

    [MenuItem("Vaelstrider/Tài Khoản/Xóa Sạch Tất Cả Tài Khoản (Reset Toàn Bộ)", false, 1)]
    public static void ClearAllAccountsMenu()
    {
        AccountDatabase.ClearAllAccounts();
        EditorUtility.DisplayDialog("Đã Xóa Sạch Tài Khoản", "Toàn bộ tài khoản và tiến trình đã được xóa hoàn toàn về trạng thái ban đầu!", "OK");
    }

    [MenuItem("Vaelstrider/Tài Khoản/Xóa Sạch Trang Phục Đã Mua (Reset Costumes)", false, 2)]
    public static void ResetCostumesMenu()
    {
        HeroCostumeManager.ResetAllCostumes();
        EditorUtility.DisplayDialog("Đã Reset Trang Phục", "Toàn bộ trang phục đã mua đã được xóa sạch và đưa về mặc định!", "OK");
    }

    [MenuItem("Vaelstrider/Scenes/Build Login Scene (LoginScene.unity)", false, 0)]
    public static void BuildLoginScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // 1. Create fresh scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Load divider sprites if available
        Sprite dividerGold = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Divider/divider-000.png");
        Sprite dividerSilver = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Divider/divider-003.png");

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

        // 3. Ambient Lighting & Mystic Embers
        GameObject lightObj = new GameObject("GlobalLight");
        var light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color(0.70f, 0.78f, 0.98f, 1f);
        light.intensity = 0.90f;

        // Floating Golden Embers
        GameObject particlesObj = new GameObject("MysticEmbers");
        var ps = particlesObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 7f;
        main.startSpeed = 0.7f;
        main.startSize = 0.14f;
        main.startColor = new Color(1f, 0.82f, 0.35f, 0.75f);
        main.maxParticles = 80;
        var emission = ps.emission;
        emission.rateOverTime = 12f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(18f, 1f, 1f);
        particlesObj.transform.position = new Vector3(0f, -6f, 0f);
        var psRenderer = particlesObj.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null) psRenderer.material = HeavenlyPortalFX.GetOrCreateParticleMaterial();

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

        // 5. Background Vignette / Backdrop
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        Sprite sandBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Backgrounds/Sand/Demo.png");
        if (sandBgSprite != null)
        {
            bgImg.sprite = sandBgSprite;
            bgImg.color = new Color(0.72f, 0.68f, 0.64f, 1f);
        }
        else
        {
            bgImg.color = new Color(0.12f, 0.10f, 0.08f, 1f);
        }

        // 6. LoginManager Controller
        var loginMgr = canvasObj.AddComponent<LoginManager>();

        // 7. HEADER GAME LOGO / TITLE
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.5f, 0.5f);
        headerRt.anchorMax = new Vector2(0.5f, 0.5f);
        headerRt.pivot = new Vector2(0.5f, 0.5f);
        headerRt.anchoredPosition = new Vector2(0f, 325f);
        headerRt.sizeDelta = new Vector2(1100f, 190f);

        // Title "VAELSTRIDER"
        GameObject titleObj = new GameObject("GameTitle");
        titleObj.transform.SetParent(headerObj.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 35f);
        titleRt.sizeDelta = new Vector2(1000f, 110f);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "VAELSTRIDER:\nRUINBOUND";
        titleTmp.fontSize = 54;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(1f, 0.85f, 0.25f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.raycastTarget = false;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(headerObj.transform, false);
        RectTransform subRt = subObj.AddComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0f, -50f);
        subRt.sizeDelta = new Vector2(1000f, 35f);
        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = "— JOURNEY TO THE ANCIENT RUINS —";
        subTmp.fontSize = 18;
        subTmp.fontStyle = FontStyles.Bold;
        subTmp.color = new Color(0.92f, 0.45f, 0.45f);
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.raycastTarget = false;

        // Divider under header
        if (dividerGold != null)
        {
            CreateDividerImage(headerObj.transform, dividerGold, new Vector2(0f, -80f), new Vector2(600f, 18f));
        }

        // 8. MAIN LOGIN CARD PANEL
        GameObject loginCardObj = new GameObject("LoginCardPanel");
        loginCardObj.transform.SetParent(canvasObj.transform, false);
        RectTransform cardRt = loginCardObj.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = new Vector2(0f, -70f);
        cardRt.sizeDelta = new Vector2(620f, 500f);
        Image cardBg = loginCardObj.AddComponent<Image>();
        cardBg.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);
        var cardOutline = loginCardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.40f, 0.55f, 0.80f, 0.9f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        // 8A. TABS: ĐĂNG NHẬP / ĐĂNG KÝ
        GameObject tabGroup = new GameObject("TabGroup");
        tabGroup.transform.SetParent(loginCardObj.transform, false);
        RectTransform tabGroupRt = tabGroup.AddComponent<RectTransform>();
        tabGroupRt.anchoredPosition = new Vector2(0f, 215f);
        tabGroupRt.sizeDelta = new Vector2(520f, 44f);

        // Tab Login Button
        var tabLoginBtn = CreateTabButton(tabGroup.transform, "Tab_Login", "ĐĂNG NHẬP", new Vector2(-130f, 0f), new Vector2(250f, 44f), out Image tabLoginBg, out TextMeshProUGUI tabLoginTxt);
        // Tab Register Button
        var tabRegBtn = CreateTabButton(tabGroup.transform, "Tab_Register", "ĐĂNG KÝ MỚI", new Vector2(130f, 0f), new Vector2(250f, 44f), out Image tabRegBg, out TextMeshProUGUI tabRegTxt);

        // Divider below tabs
        if (dividerSilver != null)
        {
            CreateDividerImage(loginCardObj.transform, dividerSilver, new Vector2(0f, 180f), new Vector2(520f, 14f));
        }

        // 8B. Username Input Group
        CreateFieldLabel(loginCardObj.transform, "Tên Đăng Nhập:", new Vector2(0f, 145f));
        var userInput = CreateInputField(loginCardObj.transform, "UsernameInput", "", new Vector2(0f, 105f), new Vector2(480f, 46f), false);

        // 8C. Password Input Group + Eye Button (Clean "HIỆN" / "ẨN" text, No square emojis)
        CreateFieldLabel(loginCardObj.transform, "Mật Khẩu:", new Vector2(0f, 60f));
        var passInput = CreateInputField(loginCardObj.transform, "PasswordInput", "123456", new Vector2(-35f, 20f), new Vector2(410f, 46f), true);

        // Toggle Eye Button (Icon Eye Open / Closed)
        GameObject eyeBtnObj = new GameObject("Btn_TogglePassword");
        eyeBtnObj.transform.SetParent(loginCardObj.transform, false);
        RectTransform eyeRt = eyeBtnObj.AddComponent<RectTransform>();
        eyeRt.anchoredPosition = new Vector2(212f, 20f);
        eyeRt.sizeDelta = new Vector2(46f, 46f);
        Image eyeImg = eyeBtnObj.AddComponent<Image>();
        eyeImg.color = new Color(0.14f, 0.18f, 0.28f);
        eyeImg.raycastTarget = true;
        var eyeOutline = eyeBtnObj.AddComponent<Outline>();
        eyeOutline.effectColor = new Color(0.40f, 0.55f, 0.75f, 0.8f);
        Button eyeBtn = eyeBtnObj.AddComponent<Button>();
        eyeBtn.targetGraphic = eyeImg;

        GameObject eyeIconObj = new GameObject("Icon");
        eyeIconObj.transform.SetParent(eyeBtnObj.transform, false);
        RectTransform eyeIconRt = eyeIconObj.AddComponent<RectTransform>();
        eyeIconRt.anchorMin = new Vector2(0.5f, 0.5f);
        eyeIconRt.anchorMax = new Vector2(0.5f, 0.5f);
        eyeIconRt.pivot = new Vector2(0.5f, 0.5f);
        eyeIconRt.sizeDelta = new Vector2(26f, 26f);
        Image eyeIconImg = eyeIconObj.AddComponent<Image>();
        Sprite eyeOpenSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/UI/eye_open.png");
        Sprite eyeClosedSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/UI/eye_closed.png");
        eyeIconImg.sprite = eyeClosedSpr;
        eyeIconImg.color = new Color(0.75f, 0.85f, 0.95f, 0.95f);
        eyeIconImg.raycastTarget = false;

        // 8D. Remember Me Toggle (Solid High-Contrast Box)
        GameObject remObj = new GameObject("RememberMeToggle");
        remObj.transform.SetParent(loginCardObj.transform, false);
        RectTransform remRt = remObj.AddComponent<RectTransform>();
        remRt.anchoredPosition = new Vector2(-120f, -28f);
        remRt.sizeDelta = new Vector2(230f, 32f);
        Toggle remToggle = remObj.AddComponent<Toggle>();

        GameObject chkBoxObj = new GameObject("Background");
        chkBoxObj.transform.SetParent(remObj.transform, false);
        RectTransform chkBoxRt = chkBoxObj.AddComponent<RectTransform>();
        chkBoxRt.anchoredPosition = new Vector2(-95f, 0f);
        chkBoxRt.sizeDelta = new Vector2(24f, 24f);
        Image chkBoxImg = chkBoxObj.AddComponent<Image>();
        chkBoxImg.color = new Color(0.14f, 0.18f, 0.28f);
        var chkBoxOutline = chkBoxObj.AddComponent<Outline>();
        chkBoxOutline.effectColor = new Color(0.35f, 0.50f, 0.70f, 0.8f);

        GameObject chkMarkObj = new GameObject("Checkmark");
        chkMarkObj.transform.SetParent(chkBoxObj.transform, false);
        RectTransform chkMarkRt = chkMarkObj.AddComponent<RectTransform>();
        chkMarkRt.anchorMin = Vector2.zero;
        chkMarkRt.anchorMax = Vector2.one;
        chkMarkRt.offsetMin = new Vector2(4f, 4f);
        chkMarkRt.offsetMax = new Vector2(-4f, -4f);
        Image chkMarkImg = chkMarkObj.AddComponent<Image>();
        chkMarkImg.color = new Color(0f, 0.85f, 0.95f, 1f);

        remToggle.graphic = chkMarkImg;
        remToggle.targetGraphic = chkBoxImg;
        remToggle.isOn = true;

        GameObject remLblObj = new GameObject("Label");
        remLblObj.transform.SetParent(remObj.transform, false);
        RectTransform remLblRt = remLblObj.AddComponent<RectTransform>();
        remLblRt.anchoredPosition = new Vector2(20f, 0f);
        remLblRt.sizeDelta = new Vector2(190f, 26f);
        var remLblTmp = remLblObj.AddComponent<TextMeshProUGUI>();
        remLblTmp.text = "Ghi nhớ tài khoản";
        remLblTmp.fontSize = 15;
        remLblTmp.color = new Color(0.85f, 0.90f, 0.98f);
        remLblTmp.alignment = TextAlignmentOptions.Left;
        remLblTmp.raycastTarget = false;

        // 8E. Action Submit Button (ĐĂNG NHẬP VÀO GAME)
        var submitBtn = CreateBigButton(loginCardObj.transform, "Btn_Submit", "ĐĂNG NHẬP VÀO GAME", new Vector2(0f, -80f), new Color(0.24f, 0.50f, 0.90f), out TextMeshProUGUI submitTxt);

        // 8F. Quit Button (THOÁT GAME)
        var quitBtn = CreateSmallButton(loginCardObj.transform, "Btn_Quit", "THOÁT GAME", new Vector2(0f, -140f), new Color(0.55f, 0.20f, 0.20f), out _);

        // 8G. Status Feedback Text
        GameObject statusObj = new GameObject("StatusFeedbackText");
        statusObj.transform.SetParent(loginCardObj.transform, false);
        RectTransform statusRt = statusObj.AddComponent<RectTransform>();
        statusRt.anchoredPosition = new Vector2(0f, -195f);
        statusRt.sizeDelta = new Vector2(560f, 30f);
        var statusTmp = statusObj.AddComponent<TextMeshProUGUI>();
        statusTmp.text = "";
        statusTmp.fontSize = 14;
        statusTmp.fontStyle = FontStyles.Bold;
        statusTmp.color = new Color(0.4f, 0.9f, 1f);
        statusTmp.alignment = TextAlignmentOptions.Center;
        statusTmp.raycastTarget = false;

        // 9. TAP TO START PANEL (Activated only after successful login/registration)
        GameObject tapPanel = new GameObject("TapToStartPanel");
        tapPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform tapPanelRt = tapPanel.AddComponent<RectTransform>();
        tapPanelRt.anchorMin = Vector2.zero;
        tapPanelRt.anchorMax = Vector2.one;
        tapPanelRt.sizeDelta = Vector2.zero;

        // Fullscreen Tap Trigger
        GameObject tapBtnObj = new GameObject("Btn_FullScreenTap");
        tapBtnObj.transform.SetParent(tapPanel.transform, false);
        RectTransform tapBtnRt = tapBtnObj.AddComponent<RectTransform>();
        tapBtnRt.anchorMin = Vector2.zero;
        tapBtnRt.anchorMax = Vector2.one;
        tapBtnRt.sizeDelta = Vector2.zero;
        Image tapImg = tapBtnObj.AddComponent<Image>();
        tapImg.color = new Color(0f, 0f, 0f, 0.001f);
        tapImg.raycastTarget = true;
        Button fullTapBtn = tapBtnObj.AddComponent<Button>();

        // Welcome User Banner Capsule at Top
        GameObject userBannerObj = new GameObject("UserBanner");
        userBannerObj.transform.SetParent(tapPanel.transform, false);
        RectTransform userBannerRt = userBannerObj.AddComponent<RectTransform>();
        userBannerRt.anchorMin = new Vector2(0.5f, 1f);
        userBannerRt.anchorMax = new Vector2(0.5f, 1f);
        userBannerRt.pivot = new Vector2(0.5f, 1f);
        userBannerRt.anchoredPosition = new Vector2(0f, -40f);
        userBannerRt.sizeDelta = new Vector2(560f, 52f);
        Image userBannerBg = userBannerObj.AddComponent<Image>();
        userBannerBg.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);
        var userOutline = userBannerObj.AddComponent<Outline>();
        userOutline.effectColor = new Color(0.18f, 0.70f, 0.40f, 0.9f);
        userOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject userTxtObj = new GameObject("Text");
        userTxtObj.transform.SetParent(userBannerObj.transform, false);
        RectTransform userTxtRt = userTxtObj.AddComponent<RectTransform>();
        userTxtRt.anchorMin = Vector2.zero;
        userTxtRt.anchorMax = Vector2.one;
        userTxtRt.offsetMin = new Vector2(16f, 0f);
        userTxtRt.offsetMax = new Vector2(-120f, 0f);
        var userTmp = userTxtObj.AddComponent<TextMeshProUGUI>();
        userTmp.text = "";
        userTmp.fontSize = 16;
        userTmp.fontStyle = FontStyles.Bold;
        userTmp.color = new Color(0.85f, 0.95f, 0.90f);
        userTmp.alignment = TextAlignmentOptions.Left;
        userTmp.raycastTarget = false;
        userBannerObj.SetActive(false); // Offline game: hide account capsule

        // Switch Account Button inside banner
        GameObject switchAccObj = new GameObject("Btn_SwitchAccount");
        switchAccObj.transform.SetParent(userBannerObj.transform, false);
        RectTransform switchAccRt = switchAccObj.AddComponent<RectTransform>();
        switchAccRt.anchorMin = new Vector2(1f, 0.5f);
        switchAccRt.anchorMax = new Vector2(1f, 0.5f);
        switchAccRt.pivot = new Vector2(1f, 0.5f);
        switchAccRt.anchoredPosition = new Vector2(-12f, 0f);
        switchAccRt.sizeDelta = new Vector2(95f, 32f);
        Image switchAccImg = switchAccObj.AddComponent<Image>();
        switchAccImg.color = new Color(0.24f, 0.28f, 0.38f);
        Button switchAccBtn = switchAccObj.AddComponent<Button>();

        GameObject swTxtObj = new GameObject("Text");
        swTxtObj.transform.SetParent(switchAccObj.transform, false);
        RectTransform swTxtRt = swTxtObj.AddComponent<RectTransform>();
        swTxtRt.anchorMin = Vector2.zero;
        swTxtRt.anchorMax = Vector2.one;
        swTxtRt.sizeDelta = Vector2.zero;
        var swTmp = swTxtObj.AddComponent<TextMeshProUGUI>();
        swTmp.text = "Đổi TK";
        swTmp.fontSize = 13;
        swTmp.fontStyle = FontStyles.Bold;
        swTmp.color = Color.white;
        swTmp.alignment = TextAlignmentOptions.Center;
        swTmp.raycastTarget = false;

        switchAccObj.SetActive(false); // Game offline, khong can doi tai khoan

        // Pulsing "TAP TO START" Text
        GameObject tapTextObj = new GameObject("TapToStartText");
        tapTextObj.transform.SetParent(tapPanel.transform, false);
        RectTransform tapTxtRt = tapTextObj.AddComponent<RectTransform>();
        tapTxtRt.anchorMin = new Vector2(0.5f, 0.5f);
        tapTxtRt.anchorMax = new Vector2(0.5f, 0.5f);
        tapTxtRt.pivot = new Vector2(0.5f, 0.5f);
        tapTxtRt.anchoredPosition = new Vector2(0f, -200f);
        tapTxtRt.sizeDelta = new Vector2(900f, 100f);
        var tapTxtTmp = tapTextObj.AddComponent<TextMeshProUGUI>();
        tapTxtTmp.text = "— BẮT ĐẦU TRÒ CHƠI —\n<size=18><color=#E6C875>Chạm vào màn hình để bắt đầu</color></size>";
        tapTxtTmp.fontSize = 26;
        tapTxtTmp.fontStyle = FontStyles.Bold;
        tapTxtTmp.color = new Color(1f, 0.95f, 0.80f, 1f);
        tapTxtTmp.alignment = TextAlignmentOptions.Center;
        tapTxtTmp.raycastTarget = false;

        tapPanel.SetActive(true);
        loginCardObj.SetActive(false);

        // 10. Link all references into LoginManager
        SerializedObject so = new SerializedObject(loginMgr);
        so.FindProperty("loginCardPanel").objectReferenceValue = loginCardObj;
        so.FindProperty("tapToStartPanel").objectReferenceValue = tapPanel;

        so.FindProperty("tabLoginBtn").objectReferenceValue = tabLoginBtn;
        so.FindProperty("tabRegisterBtn").objectReferenceValue = tabRegBtn;
        so.FindProperty("tabLoginBg").objectReferenceValue = tabLoginBg;
        so.FindProperty("tabRegisterBg").objectReferenceValue = tabRegBg;
        so.FindProperty("tabLoginTxt").objectReferenceValue = tabLoginTxt;
        so.FindProperty("tabRegisterTxt").objectReferenceValue = tabRegTxt;
        so.FindProperty("usernameInputField").objectReferenceValue = userInput;
        so.FindProperty("passwordInputField").objectReferenceValue = passInput;
        so.FindProperty("togglePasswordVisibilityBtn").objectReferenceValue = eyeBtn;
        so.FindProperty("passwordEyeIconImg").objectReferenceValue = eyeIconImg;
        so.FindProperty("eyeOpenSprite").objectReferenceValue = eyeOpenSpr;
        so.FindProperty("eyeClosedSprite").objectReferenceValue = eyeClosedSpr;
        so.FindProperty("rememberMeToggle").objectReferenceValue = remToggle;
        so.FindProperty("actionSubmitButton").objectReferenceValue = submitBtn;
        so.FindProperty("actionSubmitButtonText").objectReferenceValue = submitTxt;
        so.FindProperty("quitButton").objectReferenceValue = quitBtn;

        so.FindProperty("fullScreenTapButton").objectReferenceValue = fullTapBtn;
        so.FindProperty("tapToStartText").objectReferenceValue = tapTxtTmp;
        so.FindProperty("welcomeUserBannerText").objectReferenceValue = userTmp;
        so.FindProperty("switchAccountBtn").objectReferenceValue = switchAccBtn;

        so.FindProperty("titleText").objectReferenceValue = titleTmp;
        so.FindProperty("subtitleText").objectReferenceValue = subTmp;
        so.FindProperty("statusFeedbackText").objectReferenceValue = statusTmp;
        so.ApplyModifiedProperties();

        // 11. Register all 5 scenes in Build Settings
        EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(LOGIN_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAIN_MENU_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAP1_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAP2_SCENE_PATH, true),
            new EditorBuildSettingsScene(MAP3_SCENE_PATH, true)
        };
        EditorBuildSettings.scenes = buildScenes;

        // 12. Save the scene
        string dir = Path.GetDirectoryName(LOGIN_SCENE_PATH);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        EditorSceneManager.SaveScene(scene, LOGIN_SCENE_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[LOGIN BUILDER]</color> Đã nâng cấp LoginScene với bộ UI Divider, Tab Đăng Nhập / Đăng Ký, chữ HIỆN/ẨN sắc nét và Tap To Start hoàn hảo 100%!");
    }

    private static void CreateDividerImage(Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject divObj = new GameObject("Divider");
        divObj.transform.SetParent(parent, false);
        var rt = divObj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = divObj.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
    }

    private static void CreateFieldLabel(Transform parent, string labelText, Vector2 pos)
    {
        GameObject lblObj = new GameObject("Label_" + labelText);
        lblObj.transform.SetParent(parent, false);
        var lblRt = lblObj.AddComponent<RectTransform>();
        lblRt.anchoredPosition = pos;
        lblRt.sizeDelta = new Vector2(480f, 22f);
        var lblTmp = lblObj.AddComponent<TextMeshProUGUI>();
        lblTmp.text = labelText;
        lblTmp.fontSize = 14;
        lblTmp.fontStyle = FontStyles.Bold;
        lblTmp.color = new Color(0.85f, 0.90f, 0.98f);
        lblTmp.alignment = TextAlignmentOptions.Left;
        lblTmp.raycastTarget = false;
    }

    private static Button CreateTabButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, out Image bg, out TextMeshProUGUI txt)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        bg = obj.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.17f, 0.25f);
        bg.raycastTarget = true;

        var btn = obj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(obj.transform, false);
        var tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = 16;
        txt.fontStyle = FontStyles.Bold;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false;

        return btn;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, string defaultText, Vector2 pos, Vector2 size, bool isPassword)
    {
        GameObject inObj = new GameObject(name);
        inObj.transform.SetParent(parent, false);
        var inRt = inObj.AddComponent<RectTransform>();
        inRt.anchoredPosition = pos;
        inRt.sizeDelta = size;

        var inBg = inObj.AddComponent<Image>();
        inBg.color = new Color(0.13f, 0.16f, 0.24f);

        var inOutline = inObj.AddComponent<Outline>();
        inOutline.effectColor = new Color(0.35f, 0.50f, 0.70f, 0.7f);
        inOutline.effectDistance = new Vector2(1.5f, -1.5f);

        var inputField = inObj.AddComponent<TMP_InputField>();
        inputField.targetGraphic = inBg;
        if (isPassword)
        {
            inputField.contentType = TMP_InputField.ContentType.Password;
        }

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(inObj.transform, false);
        var tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(14f, 0f);
        tRt.offsetMax = new Vector2(-14f, 0f);
        var txtTmp = txtObj.AddComponent<TextMeshProUGUI>();
        txtTmp.text = defaultText;
        txtTmp.fontSize = 17;
        txtTmp.fontStyle = FontStyles.Bold;
        txtTmp.color = Color.white;
        txtTmp.alignment = TextAlignmentOptions.Left;

        inputField.textViewport = tRt;
        inputField.textComponent = txtTmp;
        inputField.text = defaultText;
        inputField.interactable = true;

        return inputField;
    }

    private static Button CreateBigButton(Transform parent, string name, string text, Vector2 pos, Color bgColor, out TextMeshProUGUI btnTxt)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(480f, 50f);

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
        btnTxt.fontSize = 18;
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
        rt.sizeDelta = new Vector2(480f, 44f);

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
        btnTxt.fontSize = 16;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.raycastTarget = false;

        return btn;
    }
}
