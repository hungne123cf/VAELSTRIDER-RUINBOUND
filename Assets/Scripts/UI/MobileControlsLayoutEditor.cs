using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MobileControlsLayoutEditor : MonoBehaviour
{
    public static MobileControlsLayoutEditor Instance { get; private set; }

    private System.Action onCloseCallback;
    private Canvas parentCanvas;
    private RectTransform canvasRect;

    // Draggable Item Rects
    private RectTransform joyItem;
    private RectTransform atkItem;
    private RectTransform dashItem;
    private RectTransform skillItem;
    private RectTransform potionItem;

    // CanvasGroups for live Opacity preview
    private CanvasGroup joyCg;
    private CanvasGroup atkCg;
    private CanvasGroup dashCg;
    private CanvasGroup skillCg;
    private CanvasGroup potionCg;

    private float currentScale = 1.25f;
    private float currentOpacity = 1.0f;

    private TextMeshProUGUI scaleValText;
    private TextMeshProUGUI opacityValText;

    public float GetCurrentScale() => currentScale;
    public float GetCurrentOpacity() => currentOpacity;

    private const float DEFAULT_JOY_X = 250f;
    private const float DEFAULT_JOY_Y = 250f;
    private const float DEFAULT_ATK_X = -220f;
    private const float DEFAULT_ATK_Y = 220f;
    private const float DEFAULT_DASH_X = -380f;
    private const float DEFAULT_DASH_Y = 160f;
    private const float DEFAULT_SKILL_X = -350f;
    private const float DEFAULT_SKILL_Y = 290f;
    private const float DEFAULT_POTION_X = -220f;
    private const float DEFAULT_POTION_Y = 360f;

    public static void OpenEditor(System.Action onClose = null)
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
        }

        Canvas rootCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) rootCanvas = Object.FindAnyObjectByType<Canvas>();

        if (rootCanvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rootCanvas = canvasObj.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        GameObject editorObj = new GameObject("MobileControlsLayoutEditor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        editorObj.layer = 5;
        editorObj.transform.SetParent(rootCanvas.transform, false);

        RectTransform rt = editorObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = editorObj.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.12f); // Clear / transparent backdrop so player sees the live game battlefield
        bg.raycastTarget = true;

        Instance = editorObj.AddComponent<MobileControlsLayoutEditor>();
        Instance.onCloseCallback = onClose;
        Instance.parentCanvas = rootCanvas;
        Instance.canvasRect = rootCanvas.GetComponent<RectTransform>();
        Instance.BuildEditorUI();
    }

    private void BuildEditorUI()
    {
        bool isVi = LocalizationManager.IsVietnamese;

        currentScale = PlayerPrefs.GetFloat("Game_Controls_ButtonScale", 1.25f);
        if (currentScale < 0.6f) currentScale = 1.25f;

        currentOpacity = PlayerPrefs.GetFloat("Game_Controls_Opacity", 1.0f);
        if (currentOpacity < 0.3f) currentOpacity = 1.0f;

        // 1. Sleek Minimal Single-Row Top Bar (No bulky borders, no gothic frame corner brackets)
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topBar.layer = 5;
        topBar.transform.SetParent(transform, false);

        RectTransform tbRt = topBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0.5f, 1f);
        tbRt.anchorMax = new Vector2(0.5f, 1f);
        tbRt.pivot = new Vector2(0.5f, 1f);
        tbRt.anchoredPosition = new Vector2(0f, -14f);
        tbRt.sizeDelta = new Vector2(1040f, 54f);

        Image tbImg = topBar.GetComponent<Image>();
        tbImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        tbImg.type = Image.Type.Sliced;
        tbImg.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);

        // Subtle thin outline for the top bar (clean modern feel, no thick gothic brackets)
        var tbOutline = topBar.AddComponent<Outline>();
        tbOutline.effectColor = new Color(0.20f, 0.40f, 0.55f, 0.60f);
        tbOutline.effectDistance = new Vector2(1f, -1f);

        // Layout container on Top Bar
        float curX = 18f;

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(topBar.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0.5f);
        tRt.anchorMax = new Vector2(0f, 0.5f);
        tRt.pivot = new Vector2(0f, 0.5f);
        tRt.anchoredPosition = new Vector2(curX, 0f);
        tRt.sizeDelta = new Vector2(175f, 32f);

        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = isVi ? "<b>TÙY CHỈNH NÚT</b>" : "<b>CUSTOM CONTROLS</b>";
        titleTmp.fontSize = 15.5f;
        titleTmp.color = new Color(1f, 0.88f, 0.45f);
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        curX += 180f;

        // Separator 1
        curX += CreateSmallDivider(topBar.transform, curX);

        // Cỡ Nút (Scale) Section: Label + [-] + 125% + [+]
        GameObject scaleLblObj = new GameObject("ScaleLabel", typeof(RectTransform), typeof(CanvasRenderer));
        scaleLblObj.transform.SetParent(topBar.transform, false);
        RectTransform slRt = scaleLblObj.GetComponent<RectTransform>();
        slRt.anchorMin = new Vector2(0f, 0.5f);
        slRt.anchorMax = new Vector2(0f, 0.5f);
        slRt.pivot = new Vector2(0f, 0.5f);
        slRt.anchoredPosition = new Vector2(curX, 0f);
        slRt.sizeDelta = new Vector2(65f, 28f);

        var slTmp = scaleLblObj.AddComponent<TextMeshProUGUI>();
        slTmp.text = isVi ? "<b>CỠ NÚT:</b>" : "<b>SIZE:</b>";
        slTmp.fontSize = 13.5f;
        slTmp.color = new Color(0.85f, 0.90f, 0.96f);
        slTmp.alignment = TextAlignmentOptions.MidlineLeft;
        curX += 70f;

        CreateIconButton(topBar.transform, "Btn_ScaleDown", new Vector2(curX, 0f), new Vector2(36f, 36f),
            "<b>-</b>", new Color(0.28f, 0.16f, 0.18f), () => ChangeScale(-0.10f));
        curX += 40f;

        GameObject scaleValObj = new GameObject("ScaleVal", typeof(RectTransform), typeof(CanvasRenderer));
        scaleValObj.transform.SetParent(topBar.transform, false);
        RectTransform svRt = scaleValObj.GetComponent<RectTransform>();
        svRt.anchorMin = new Vector2(0f, 0.5f);
        svRt.anchorMax = new Vector2(0f, 0.5f);
        svRt.pivot = new Vector2(0f, 0.5f);
        svRt.anchoredPosition = new Vector2(curX, 0f);
        svRt.sizeDelta = new Vector2(52f, 28f);

        scaleValText = scaleValObj.AddComponent<TextMeshProUGUI>();
        scaleValText.text = $"<b>{Mathf.RoundToInt(currentScale * 100)}%</b>";
        scaleValText.fontSize = 14.5f;
        scaleValText.alignment = TextAlignmentOptions.Center;
        scaleValText.color = new Color(0.35f, 0.95f, 0.85f);
        curX += 54f;

        CreateIconButton(topBar.transform, "Btn_ScaleUp", new Vector2(curX, 0f), new Vector2(36f, 36f),
            "<b>+</b>", new Color(0.14f, 0.35f, 0.24f), () => ChangeScale(0.10f));
        curX += 44f;

        // Separator 2
        curX += CreateSmallDivider(topBar.transform, curX);

        // Độ Mờ (Opacity) Section: Label + [-] + 100% + [+]
        GameObject opcLblObj = new GameObject("OpacityLabel", typeof(RectTransform), typeof(CanvasRenderer));
        opcLblObj.transform.SetParent(topBar.transform, false);
        RectTransform olRt = opcLblObj.GetComponent<RectTransform>();
        olRt.anchorMin = new Vector2(0f, 0.5f);
        olRt.anchorMax = new Vector2(0f, 0.5f);
        olRt.pivot = new Vector2(0f, 0.5f);
        olRt.anchoredPosition = new Vector2(curX, 0f);
        olRt.sizeDelta = new Vector2(65f, 28f);

        var olTmp = opcLblObj.AddComponent<TextMeshProUGUI>();
        olTmp.text = isVi ? "<b>ĐỘ MỜ:</b>" : "<b>OPACITY:</b>";
        olTmp.fontSize = 13.5f;
        olTmp.color = new Color(0.85f, 0.90f, 0.96f);
        olTmp.alignment = TextAlignmentOptions.MidlineLeft;
        curX += 70f;

        CreateIconButton(topBar.transform, "Btn_OpacityDown", new Vector2(curX, 0f), new Vector2(36f, 36f),
            "<b>-</b>", new Color(0.28f, 0.16f, 0.18f), () => ChangeOpacity(-0.10f));
        curX += 40f;

        GameObject opcValObj = new GameObject("OpacityVal", typeof(RectTransform), typeof(CanvasRenderer));
        opcValObj.transform.SetParent(topBar.transform, false);
        RectTransform ovRt = opcValObj.GetComponent<RectTransform>();
        ovRt.anchorMin = new Vector2(0f, 0.5f);
        ovRt.anchorMax = new Vector2(0f, 0.5f);
        ovRt.pivot = new Vector2(0f, 0.5f);
        ovRt.anchoredPosition = new Vector2(curX, 0f);
        ovRt.sizeDelta = new Vector2(52f, 28f);

        opacityValText = opcValObj.AddComponent<TextMeshProUGUI>();
        opacityValText.text = $"<b>{Mathf.RoundToInt(currentOpacity * 100)}%</b>";
        opacityValText.fontSize = 14.5f;
        opacityValText.alignment = TextAlignmentOptions.Center;
        opacityValText.color = new Color(1f, 0.85f, 0.35f);
        curX += 54f;

        CreateIconButton(topBar.transform, "Btn_OpacityUp", new Vector2(curX, 0f), new Vector2(36f, 36f),
            "<b>+</b>", new Color(0.14f, 0.35f, 0.24f), () => ChangeOpacity(0.10f));
        curX += 48f;

        // Action Buttons on Top Bar: Mặc định, Lưu & Vào trận, Đóng
        CreateTopBarButton(topBar.transform, "Btn_Reset", new Vector2(curX, 0f), new Vector2(105f, 38f),
            isVi ? "MẶC ĐỊNH" : "RESET", "Repeat-Right", new Color(0.18f, 0.24f, 0.35f), OnResetClicked);
        curX += 112f;

        CreateTopBarButton(topBar.transform, "Btn_Save", new Vector2(curX, 0f), new Vector2(150f, 38f),
            isVi ? "LƯU & VÀO TRẬN" : "SAVE & PLAY", "Tick", new Color(0.12f, 0.48f, 0.26f), OnSaveClicked);
        curX += 157f;

        CreateTopBarButton(topBar.transform, "Btn_Back", new Vector2(curX, 0f), new Vector2(75f, 38f),
            isVi ? "ĐÓNG" : "CLOSE", "Close", new Color(0.32f, 0.15f, 0.16f), OnBackClicked);

        // Adjust topBar width precisely based on curX
        tbRt.sizeDelta = new Vector2(curX + 18f, 54f);

        // 2. Bottom Tip Banner (clean and non-intrusive)
        GameObject tipObj = new GameObject("BottomTip", typeof(RectTransform), typeof(CanvasRenderer));
        tipObj.transform.SetParent(transform, false);
        RectTransform tipRt = tipObj.GetComponent<RectTransform>();
        tipRt.anchorMin = new Vector2(0.5f, 0f);
        tipRt.anchorMax = new Vector2(0.5f, 0f);
        tipRt.pivot = new Vector2(0.5f, 0f);
        tipRt.anchoredPosition = new Vector2(0f, 22f);
        tipRt.sizeDelta = new Vector2(800f, 28f);

        var tipTmp = tipObj.AddComponent<TextMeshProUGUI>();
        tipTmp.text = isVi 
            ? "<color=#55EFC4>💡 Chạm vào nút bất kỳ và kéo đến vị trí thuận tay nhất, sau đó nhấn <b>LƯU & VÀO TRẬN</b>.</color>"
            : "<color=#55EFC4>💡 Touch and drag any button to your preferred position, then tap <b>SAVE & PLAY</b>.</color>";
        tipTmp.fontSize = 14f;
        tipTmp.alignment = TextAlignmentOptions.Center;

        // 3. Draggable Items Setup
        string curHero = PlayerPrefs.GetString("SelectedHero", "Hero1");

        // 3.1 Joystick (Cần di chuyển)
        float curJoyX = PlayerPrefs.GetFloat("Game_Controls_Joy_PosX", DEFAULT_JOY_X);
        float curJoyY = PlayerPrefs.GetFloat("Game_Controls_Joy_PosY", DEFAULT_JOY_Y);
        joyItem = CreateDraggableControlItem("Drag_Joystick", isVi ? "CẦN DI CHUYỂN" : "JOYSTICK",
            new Vector2(curJoyX, curJoyY), new Vector2(210f, 210f), new Vector2(0f, 0f),
            null, true, out joyCg);

        // 3.2 Attack (Nút tấn công)
        float curAtkX = PlayerPrefs.GetFloat("Game_Controls_Atk_PosX", DEFAULT_ATK_X);
        float curAtkY = PlayerPrefs.GetFloat("Game_Controls_Atk_PosY", DEFAULT_ATK_Y);
        Sprite atkSpr = MobileActionButton.GetHeroActionIcon(curHero, MobileActionButton.ButtonType.Attack);
        if (atkSpr == null) atkSpr = WhiteIconLoader.GetSprite("Sword");
        atkItem = CreateDraggableControlItem("Drag_Attack", isVi ? "NÚT TẤN CÔNG" : "ATTACK",
            new Vector2(curAtkX, curAtkY), new Vector2(165f, 165f), new Vector2(1f, 0f),
            atkSpr, false, out atkCg);

        // 3.3 Dash (Nút lướt)
        float curDashX = PlayerPrefs.GetFloat("Game_Controls_Dash_PosX", DEFAULT_DASH_X);
        float curDashY = PlayerPrefs.GetFloat("Game_Controls_Dash_PosY", DEFAULT_DASH_Y);
        Sprite dashSpr = MobileActionButton.GetHeroActionIcon(curHero, MobileActionButton.ButtonType.Dash);
        if (dashSpr == null) dashSpr = WhiteIconLoader.GetSprite("Dash") ?? WhiteIconLoader.GetSprite("Footsteps");
        dashItem = CreateDraggableControlItem("Drag_Dash", isVi ? "NÚT LƯỚT" : "DASH",
            new Vector2(curDashX, curDashY), new Vector2(130f, 130f), new Vector2(1f, 0f),
            dashSpr, false, out dashCg);

        // 3.4 Skill (Nút tuyệt kỹ)
        float curSkillX = PlayerPrefs.GetFloat("Game_Controls_Skill_PosX", DEFAULT_SKILL_X);
        float curSkillY = PlayerPrefs.GetFloat("Game_Controls_Skill_PosY", DEFAULT_SKILL_Y);
        Sprite skillSpr = MobileActionButton.GetHeroActionIcon(curHero, MobileActionButton.ButtonType.Skill);
        if (skillSpr == null) skillSpr = WhiteIconLoader.GetSprite("Star") ?? WhiteIconLoader.GetSprite("Flame");
        skillItem = CreateDraggableControlItem("Drag_Skill", isVi ? "NÚT TUYỆT KỸ" : "SKILL",
            new Vector2(curSkillX, curSkillY), new Vector2(130f, 130f), new Vector2(1f, 0f),
            skillSpr, false, out skillCg);

        // 3.5 Potion (Bình máu)
        float curPotionX = PlayerPrefs.GetFloat("Game_Controls_Potion_PosX", DEFAULT_POTION_X);
        float curPotionY = PlayerPrefs.GetFloat("Game_Controls_Potion_PosY", DEFAULT_POTION_Y);
        Sprite potSpr = Resources.Load<Sprite>("Items/health_potion");
#if UNITY_EDITOR
        if (potSpr == null) potSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/health_potion.png");
#endif
        if (potSpr == null) potSpr = WhiteIconLoader.GetSprite("Heart");
        potionItem = CreateDraggableControlItem("Drag_Potion", isVi ? "BÌNH MÁU" : "POTION",
            new Vector2(curPotionX, curPotionY), new Vector2(95f, 95f), new Vector2(1f, 0f),
            potSpr, false, out potionCg);

        ApplyScaleAndOpacityToPreview();
    }

    private float CreateSmallDivider(Transform parent, float curX)
    {
        GameObject divObj = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        divObj.transform.SetParent(parent, false);
        RectTransform dRt = divObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0.5f);
        dRt.anchorMax = new Vector2(0f, 0.5f);
        dRt.pivot = new Vector2(0f, 0.5f);
        dRt.anchoredPosition = new Vector2(curX, 0f);
        dRt.sizeDelta = new Vector2(1.5f, 28f);

        Image img = divObj.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.15f);
        return 14f;
    }

    private Button CreateIconButton(Transform parent, string name, Vector2 pos, Vector2 size, string text, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.layer = 5;
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        img.type = Image.Type.Sliced;
        img.color = color;

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 17f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        btn.onClick.AddListener(() => {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            action?.Invoke();
        });
        return btn;
    }

    private Button CreateTopBarButton(Transform parent, string name, Vector2 pos, Vector2 size, string text, string icon, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.layer = 5;
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        img.type = Image.Type.Sliced;
        img.color = color;

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 13.5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        WhiteIconLoader.SetButtonIconAndText(btn, icon, text, Color.white, Color.white, true);

        btn.onClick.AddListener(action);
        return btn;
    }

    private void ChangeScale(float delta)
    {
        currentScale = Mathf.Clamp(currentScale + delta, 0.75f, 2.00f);
        ApplyScaleAndOpacityToPreview();
    }

    private void ChangeOpacity(float delta)
    {
        currentOpacity = Mathf.Clamp(currentOpacity + delta, 0.35f, 1.0f);
        ApplyScaleAndOpacityToPreview();
    }

    private void ApplyScaleAndOpacityToPreview()
    {
        if (scaleValText != null)
        {
            scaleValText.text = $"<b>{Mathf.RoundToInt(currentScale * 100)}%</b>";
        }
        if (opacityValText != null)
        {
            opacityValText.text = $"<b>{Mathf.RoundToInt(currentOpacity * 100)}%</b>";
        }

        Vector3 sc = Vector3.one * currentScale;
        if (joyItem != null) joyItem.localScale = sc;
        if (atkItem != null) atkItem.localScale = sc;
        if (dashItem != null) dashItem.localScale = sc;
        if (skillItem != null) skillItem.localScale = sc;
        if (potionItem != null) potionItem.localScale = sc;

        if (joyCg != null) joyCg.alpha = currentOpacity;
        if (atkCg != null) atkCg.alpha = currentOpacity;
        if (dashCg != null) dashCg.alpha = currentOpacity;
        if (skillCg != null) skillCg.alpha = currentOpacity;
        if (potionCg != null) potionCg.alpha = currentOpacity;

        PlayerPrefs.SetFloat("Game_Controls_ButtonScale", currentScale);
        PlayerPrefs.SetFloat("Game_Controls_Opacity", currentOpacity);
        SettingsUI.ApplyControlSettingsLive();
    }

    private RectTransform CreateDraggableControlItem(string name, string label, Vector2 pos, Vector2 size, Vector2 anchor, Sprite heroIcon, bool isJoystick, out CanvasGroup outCg)
    {
        GameObject itemObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        itemObj.layer = 5;
        itemObj.transform.SetParent(transform, false);

        outCg = itemObj.GetComponent<CanvasGroup>();
        outCg.alpha = currentOpacity;

        RectTransform rt = itemObj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = itemObj.GetComponent<Image>();

        if (isJoystick)
        {
            Sprite baseSpr = null;
#if UNITY_EDITOR
            baseSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Joystick Base.png");
            if (baseSpr == null) baseSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/joystick_base.png");
#endif
            if (baseSpr == null) baseSpr = Resources.Load<Sprite>("UI/joystick_base");
            if (baseSpr != null) img.sprite = baseSpr;
            img.color = Color.white;
        }
        else
        {
            var circ = SoulslikeHUDStyler.GetActionButtonCoreSprite();
            if (circ == null) circ = Resources.Load<Sprite>("Combat/circle_fill");
#if UNITY_EDITOR
            if (circ == null) circ = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/circle_fill.png");
#endif
            if (circ != null) img.sprite = circ;
            img.color = new Color(0.12f, 0.16f, 0.22f, 0.92f);
        }

        // Circular glow outline for draggable visual feedback
        var outline = itemObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.85f, 1f, 0.80f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        // Decorative Runic Ring Frame on top of button background (matching actual in-game button)
        if (!isJoystick)
        {
            GameObject ringObj = new GameObject("RingFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ringObj.transform.SetParent(itemObj.transform, false);
            RectTransform rRt = ringObj.GetComponent<RectTransform>();
            rRt.anchorMin = Vector2.zero;
            rRt.anchorMax = Vector2.one;
            rRt.sizeDelta = Vector2.zero;

            Image ringImg = ringObj.GetComponent<Image>();
            ringImg.sprite = SoulslikeHUDStyler.GetActionButtonRingSprite();
            ringImg.color = Color.white;
            ringImg.raycastTarget = false;
        }

        // Icon inside button
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(itemObj.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;

        if (isJoystick)
        {
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(85f, 85f);
            iconRt.anchoredPosition = Vector2.zero;

            var joySpr = Resources.Load<Sprite>("UI/joystick_handle");
#if UNITY_EDITOR
            if (joySpr == null) joySpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Joystick Handle.png");
            if (joySpr == null) joySpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/joystick_handle.png");
#endif
            if (joySpr != null) iconImg.sprite = joySpr;
            iconImg.color = Color.white;
            iconImg.enabled = (joySpr != null);
        }
        else
        {
            iconRt.anchorMin = new Vector2(0.18f, 0.18f);
            iconRt.anchorMax = new Vector2(0.82f, 0.82f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            if (heroIcon != null)
            {
                iconImg.sprite = heroIcon;
                iconImg.color = Color.white;
                iconImg.enabled = true;
            }
            else
            {
                // CRUCIAL: Never leave Image enabled with null sprite to avoid white square
                iconImg.enabled = false;
            }
        }

        // Pill badge label above the button
        GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeObj.transform.SetParent(itemObj.transform, false);
        RectTransform bgRt = badgeObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 1f);
        bgRt.anchorMax = new Vector2(0.5f, 1f);
        bgRt.pivot = new Vector2(0.5f, 0f);
        bgRt.anchoredPosition = new Vector2(0f, 6f);
        bgRt.sizeDelta = new Vector2(130f, 24f);

        Image bgImg = badgeObj.GetComponent<Image>();
        bgImg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);
        var bOut = badgeObj.AddComponent<Outline>();
        bOut.effectColor = new Color(0.25f, 0.85f, 1f, 0.60f);
        bOut.effectDistance = new Vector2(1f, -1f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.transform.SetParent(badgeObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = $"<b>{label}</b>";
        tmp.fontSize = 12f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.92f, 0.65f);
        tmp.raycastTarget = false;

        // Add Draggable Controller script
        var dragHandler = itemObj.AddComponent<ControlItemDragHandler>();
        dragHandler.Init(this, rt, anchor, outline);

        return rt;
    }

    public void OnSaveClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();

        if (joyItem != null)
        {
            PlayerPrefs.SetFloat("Game_Controls_Joy_PosX", joyItem.anchoredPosition.x);
            PlayerPrefs.SetFloat("Game_Controls_Joy_PosY", joyItem.anchoredPosition.y);
        }
        if (atkItem != null)
        {
            PlayerPrefs.SetFloat("Game_Controls_Atk_PosX", atkItem.anchoredPosition.x);
            PlayerPrefs.SetFloat("Game_Controls_Atk_PosY", atkItem.anchoredPosition.y);
        }
        if (dashItem != null)
        {
            PlayerPrefs.SetFloat("Game_Controls_Dash_PosX", dashItem.anchoredPosition.x);
            PlayerPrefs.SetFloat("Game_Controls_Dash_PosY", dashItem.anchoredPosition.y);
        }
        if (skillItem != null)
        {
            PlayerPrefs.SetFloat("Game_Controls_Skill_PosX", skillItem.anchoredPosition.x);
            PlayerPrefs.SetFloat("Game_Controls_Skill_PosY", skillItem.anchoredPosition.y);
        }
        if (potionItem != null)
        {
            PlayerPrefs.SetFloat("Game_Controls_Potion_PosX", potionItem.anchoredPosition.x);
            PlayerPrefs.SetFloat("Game_Controls_Potion_PosY", potionItem.anchoredPosition.y);
        }

        PlayerPrefs.SetFloat("Game_Controls_ButtonScale", currentScale);
        PlayerPrefs.SetFloat("Game_Controls_Opacity", currentOpacity);
        PlayerPrefs.Save();
        SettingsUI.ApplyControlSettingsLive();

        // Direct return to match: resume battle immediately so player can fight with the new layout
        Destroy(gameObject);
        if (SettingsUI.Instance != null && SettingsUI.Instance.IsOpen())
        {
            SettingsUI.Instance.CloseSettings();
        }
        else
        {
            Time.timeScale = 1.0f;
            GameAutoBootstrap.SetGameplayHUDVisible(true);
            SettingsUI.ApplyControlSettingsLive();
        }
    }

    public void OnResetClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();

        if (joyItem != null) joyItem.anchoredPosition = new Vector2(DEFAULT_JOY_X, DEFAULT_JOY_Y);
        if (atkItem != null) atkItem.anchoredPosition = new Vector2(DEFAULT_ATK_X, DEFAULT_ATK_Y);
        if (dashItem != null) dashItem.anchoredPosition = new Vector2(DEFAULT_DASH_X, DEFAULT_DASH_Y);
        if (skillItem != null) skillItem.anchoredPosition = new Vector2(DEFAULT_SKILL_X, DEFAULT_SKILL_Y);
        if (potionItem != null) potionItem.anchoredPosition = new Vector2(DEFAULT_POTION_X, DEFAULT_POTION_Y);

        currentScale = 1.25f;
        currentOpacity = 1.0f;
        ApplyScaleAndOpacityToPreview();
    }

    public void OnBackClicked()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        CloseAndReturn();
    }

    private void CloseAndReturn()
    {
        var cb = onCloseCallback;
        Destroy(gameObject);
        cb?.Invoke();
    }

    public Canvas GetParentCanvas() => parentCanvas;
}

public class ControlItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private MobileControlsLayoutEditor editor;
    private RectTransform myRt;
    private Vector2 anchorType;
    private Outline outline;
    private Vector2 startDragPos;

    public void Init(MobileControlsLayoutEditor editor, RectTransform rt, Vector2 anchor, Outline outl)
    {
        this.editor = editor;
        this.myRt = rt;
        this.anchorType = anchor;
        this.outline = outl;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
        startDragPos = myRt.anchoredPosition;
        if (outline != null)
        {
            outline.effectColor = new Color(1f, 0.9f, 0.2f, 1f); // Glow gold on active drag
            outline.effectDistance = new Vector2(3.5f, -3.5f);
        }
        myRt.localScale = Vector3.one * (editor != null ? editor.GetCurrentScale() * 1.06f : 1.25f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Canvas canvas = editor != null ? editor.GetParentCanvas() : null;
        float factor = (canvas != null && canvas.scaleFactor > 0.01f) ? canvas.scaleFactor : 1f;

        Vector2 delta = eventData.delta / factor;
        Vector2 targetPos = myRt.anchoredPosition + delta;

        // Screen boundary clamping based on 1920x1080 design space
        if (anchorType.x < 0.5f) // Bottom-Left (Joystick)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, 80f, 900f);
            targetPos.y = Mathf.Clamp(targetPos.y, 80f, 850f);
        }
        else // Bottom-Right (Action Buttons)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, -900f, -60f);
            targetPos.y = Mathf.Clamp(targetPos.y, 60f, 850f);
        }

        myRt.anchoredPosition = targetPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (outline != null)
        {
            outline.effectColor = new Color(0.25f, 0.85f, 1f, 0.80f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
        }
        myRt.localScale = Vector3.one * (editor != null ? editor.GetCurrentScale() : 1.25f);
    }
}
