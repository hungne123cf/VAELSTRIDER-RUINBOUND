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

    private const float DEFAULT_JOY_X = 250f;
    private const float DEFAULT_JOY_Y = 250f;
    private const float DEFAULT_ATK_X = -220f;
    private const float DEFAULT_ATK_Y = 220f;
    private const float DEFAULT_DASH_X = -380f;
    private const float DEFAULT_DASH_Y = 160f;
    private const float DEFAULT_SKILL_X = -350f;
    private const float DEFAULT_SKILL_Y = 290f;

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
        bg.color = new Color(0f, 0f, 0f, 0.06f); // Clear / transparent overlay so player sees the live game battlefield
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

        // 1. Top Bar Panel
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topBar.layer = 5;
        topBar.transform.SetParent(transform, false);

        RectTransform tbRt = topBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0.5f, 1f);
        tbRt.anchorMax = new Vector2(0.5f, 1f);
        tbRt.pivot = new Vector2(0.5f, 1f);
        tbRt.anchoredPosition = new Vector2(0f, -18f);
        tbRt.sizeDelta = new Vector2(980f, 68f);

        Image tbImg = topBar.GetComponent<Image>();
        tbImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        tbImg.type = Image.Type.Sliced;
        tbImg.color = new Color(0.06f, 0.08f, 0.13f, 0.98f);
        WhiteIconLoader.ApplyGothicFrame(topBar, new Color(0.95f, 0.82f, 0.40f, 0.85f), 0.45f);

        // Title on Top Bar
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(topBar.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0f);
        tRt.anchorMax = new Vector2(0f, 1f);
        tRt.pivot = new Vector2(0f, 0.5f);
        tRt.anchoredPosition = new Vector2(25f, 0f);
        tRt.sizeDelta = new Vector2(420f, 36f);

        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = isVi ? "<b>CHỈNH PHÍM TRỰC TIẾP TRONG TRẬN</b>" : "<b>DIRECT IN-MATCH CONTROLS</b>";
        titleTmp.fontSize = 17.5f;
        titleTmp.color = new Color(1f, 0.92f, 0.55f);
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Buttons on Top Bar (Save & Play, Reset Default, Settings Back)
        CreateTopBarButton(topBar.transform, "Btn_Save", new Vector2(460f, 0f), new Vector2(180f, 44f),
            isVi ? "LƯU & VÀO TRẬN" : "SAVE & PLAY", "Tick", new Color(0.12f, 0.52f, 0.28f), OnSaveClicked);

        CreateTopBarButton(topBar.transform, "Btn_Reset", new Vector2(655f, 0f), new Vector2(150f, 44f),
            isVi ? "MẶC ĐỊNH" : "RESET", "Repeat-Right", new Color(0.18f, 0.28f, 0.42f), OnResetClicked);

        CreateTopBarButton(topBar.transform, "Btn_Back", new Vector2(820f, 0f), new Vector2(140f, 44f),
            isVi ? "CÀI ĐẶT" : "SETTINGS", "SolidArrow-Left", new Color(0.38f, 0.16f, 0.18f), OnBackClicked);

        // 2. Bottom Tip Banner
        GameObject tipObj = new GameObject("BottomTip", typeof(RectTransform), typeof(CanvasRenderer));
        tipObj.transform.SetParent(transform, false);
        RectTransform tipRt = tipObj.GetComponent<RectTransform>();
        tipRt.anchorMin = new Vector2(0.5f, 0f);
        tipRt.anchorMax = new Vector2(0.5f, 0f);
        tipRt.pivot = new Vector2(0.5f, 0f);
        tipRt.anchoredPosition = new Vector2(0f, 25f);
        tipRt.sizeDelta = new Vector2(850f, 32f);

        var tipTmp = tipObj.AddComponent<TextMeshProUGUI>();
        tipTmp.text = isVi 
            ? "<color=#55EFC4>💡 Chạm vào phím bất kỳ và kéo đến vị trí thuận tay nhất, sau đó nhấn <b>LƯU & VÀO TRẬN</b>.</color>"
            : "<color=#55EFC4>💡 Touch and drag any button to your preferred position, then tap <b>SAVE & PLAY</b>.</color>";
        tipTmp.fontSize = 15;
        tipTmp.alignment = TextAlignmentOptions.Center;

        // 3. Spawn Draggable Items
        float curJoyX = PlayerPrefs.GetFloat("Game_Controls_Joy_PosX", DEFAULT_JOY_X);
        float curJoyY = PlayerPrefs.GetFloat("Game_Controls_Joy_PosY", DEFAULT_JOY_Y);
        joyItem = CreateDraggableControlItem("Drag_Joystick", isVi ? "CẦN DI CHUYỂN" : "JOYSTICK",
            new Vector2(curJoyX, curJoyY), new Vector2(180f, 180f), new Vector2(0f, 0f), "Sound-None", true);

        float curAtkX = PlayerPrefs.GetFloat("Game_Controls_Atk_PosX", DEFAULT_ATK_X);
        float curAtkY = PlayerPrefs.GetFloat("Game_Controls_Atk_PosY", DEFAULT_ATK_Y);
        atkItem = CreateDraggableControlItem("Drag_Attack", isVi ? "TẤN CÔNG" : "ATTACK",
            new Vector2(curAtkX, curAtkY), new Vector2(145f, 145f), new Vector2(1f, 0f), "icon_attack", false);

        float curDashX = PlayerPrefs.GetFloat("Game_Controls_Dash_PosX", DEFAULT_DASH_X);
        float curDashY = PlayerPrefs.GetFloat("Game_Controls_Dash_PosY", DEFAULT_DASH_Y);
        dashItem = CreateDraggableControlItem("Drag_Dash", isVi ? "LƯỚT DASH" : "DASH",
            new Vector2(curDashX, curDashY), new Vector2(115f, 115f), new Vector2(1f, 0f), "icon_dash", false);

        float curSkillX = PlayerPrefs.GetFloat("Game_Controls_Skill_PosX", DEFAULT_SKILL_X);
        float curSkillY = PlayerPrefs.GetFloat("Game_Controls_Skill_PosY", DEFAULT_SKILL_Y);
        skillItem = CreateDraggableControlItem("Drag_Skill", isVi ? "TUYỆT KỸ" : "SKILL",
            new Vector2(curSkillX, curSkillY), new Vector2(115f, 115f), new Vector2(1f, 0f), "icon_ulti", false);
    }

    private RectTransform CreateDraggableControlItem(string name, string label, Vector2 pos, Vector2 size, Vector2 anchor, string iconName, bool isJoystick)
    {
        GameObject itemObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        itemObj.layer = 5;
        itemObj.transform.SetParent(transform, false);

        RectTransform rt = itemObj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = itemObj.GetComponent<Image>();
        img.color = isJoystick ? Color.white : new Color(0.12f, 0.16f, 0.24f, 0.85f);
        if (isJoystick)
        {
            Sprite baseSpr = null;
#if UNITY_EDITOR
            baseSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Joystick Base.png");
            if (baseSpr == null) baseSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/joystick_base.png");
#endif
            if (baseSpr == null) baseSpr = Resources.Load<Sprite>("UI/joystick_base");
            if (baseSpr != null) img.sprite = baseSpr;
        }
        else
        {
            var circ = Resources.Load<Sprite>("Combat/circle_fill");
#if UNITY_EDITOR
            if (circ == null) circ = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/circle_fill.png");
#endif
            if (circ != null) img.sprite = circ;
        }

        // Glowing outline for draggable state
        var outline = itemObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.9f, 1f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);

        // Icon inside button
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(itemObj.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.raycastTarget = false;
        iconImg.color = Color.white;

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
        }
        else
        {
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(18f, 18f);
            iconRt.offsetMax = new Vector2(-18f, -18f);

            Sprite s = WhiteIconLoader.GetSprite(iconName);
            if (s != null) iconImg.sprite = s;
        }

        // Badge pill label above the button
        GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeObj.transform.SetParent(itemObj.transform, false);
        RectTransform bgRt = badgeObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 1f);
        bgRt.anchorMax = new Vector2(0.5f, 1f);
        bgRt.pivot = new Vector2(0.5f, 0f);
        bgRt.anchoredPosition = new Vector2(0f, 6f);
        bgRt.sizeDelta = new Vector2(120f, 24f);

        Image bgImg = badgeObj.GetComponent<Image>();
        bgImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        var bOut = badgeObj.AddComponent<Outline>();
        bOut.effectColor = new Color(0.2f, 0.9f, 1f, 0.7f);
        bOut.effectDistance = new Vector2(1f, -1f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.transform.SetParent(badgeObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = $"<b>{label}</b>";
        tmp.fontSize = 12.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.92f, 0.65f);
        tmp.raycastTarget = false;

        // Add Draggable Controller script
        var dragHandler = itemObj.AddComponent<ControlItemDragHandler>();
        dragHandler.Init(this, rt, anchor, outline);

        return rt;
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
        img.color = color;
        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;

        SoulslikeHUDStyler.StyleButton(btn);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        WhiteIconLoader.SetButtonIconAndText(btn, icon, text, Color.white, Color.white, true);

        btn.onClick.AddListener(action);
        return btn;
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
            outline.effectDistance = new Vector2(4f, -4f);
        }
        myRt.localScale = Vector3.one * 1.08f;
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
            targetPos.x = Mathf.Clamp(targetPos.x, 100f, 900f);
            targetPos.y = Mathf.Clamp(targetPos.y, 100f, 850f);
        }
        else // Bottom-Right (Action Buttons)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, -900f, -80f);
            targetPos.y = Mathf.Clamp(targetPos.y, 80f, 850f);
        }

        myRt.anchoredPosition = targetPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (outline != null)
        {
            outline.effectColor = new Color(0.2f, 0.9f, 1f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);
        }
        myRt.localScale = Vector3.one;
    }
}
