using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Portal Confirmation Popup Modal
/// Thiết kế chuẩn mỹ thuật Dark Fantasy / Soulslike tinh tế:
/// - Khung đá Obsidian Dark_Fantasy_Panel_Bg với viền vàng kim thanh mảnh
/// - Rãnh Runic Plaque thể hiện tên cõi giới tiếp theo sắc nét
/// - Không sử dụng ký tự lạ unicode gây lỗi ô vuông (chống triệt để ô vuông [ ] hay ?)
/// - Nút bấm đá rèn Soulslike_Button_Stone với ánh kim Amethyst & Obsidian
/// - Hỗ trợ mượt mà cả PC (Esc / Enter) và cảm ứng Mobile Android
/// </summary>
public class PortalConfirmationPopupUI : MonoBehaviour
{
    private static PortalConfirmationPopupUI instance;
    public static PortalConfirmationPopupUI Instance => instance;

    [Header("UI State")]
    private CanvasGroup canvasGroup;
    private RuinPortal currentPortal;
    private CorruptedVoidPortal currentCorruptedPortal;
    private Transform playerTransform;

    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI destTagText;
    private TextMeshProUGUI destinationNameText;
    private TextMeshProUGUI destinationDescText;
    private TextMeshProUGUI warningNoticeText;

    private Button proceedBtn;
    private Button cancelBtn;
    private TextMeshProUGUI proceedBtnText;
    private TextMeshProUGUI cancelBtnText;

    private bool isOpen = false;
    public bool IsOpen => isOpen;

    public static float LastClosedTime { get; private set; } = -99f;

    void Awake()
    {
        instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void Update()
    {
        if (!isOpen) return;

        // Phím tắt bàn phím PC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmProceed();
        }
    }

    private void OnLanguageChanged()
    {
        if (titleText != null)
            titleText.text = LocalizationManager.T("DỊCH CHUYỂN CÕI GIỚI", "REALM WARP GATE");
        if (subtitleText != null)
            subtitleText.text = LocalizationManager.T("Vết nứt không gian kết nối tới cõi tiếp theo", "Traverse the spatial rift into the next realm");
        if (destTagText != null)
            destTagText.text = LocalizationManager.T("ĐÍCH ĐẾN TIẾP THEO", "DESTINATION REALM");
        if (cancelBtnText != null)
            cancelBtnText.text = LocalizationManager.T("Ở LẠI", "STAY");
        if (proceedBtnText != null)
            proceedBtnText.text = LocalizationManager.T("TIẾN VÀO", "ENTER");

        if (isOpen && currentPortal != null)
        {
            UpdateDestinationInfo();
        }
    }

    public static PortalConfirmationPopupUI EnsureUIExists()
    {
        if (instance != null && instance.gameObject != null) return instance;

        Canvas targetCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (targetCanvas == null) targetCanvas = Object.FindAnyObjectByType<Canvas>();
        if (targetCanvas == null)
        {
            GameObject cObj = new GameObject("InGameUICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            targetCanvas = cObj.GetComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 955;

            var scaler = cObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform existing = targetCanvas.transform.Find("PortalConfirmationModal");
        if (existing != null)
        {
            instance = existing.GetComponent<PortalConfirmationPopupUI>();
            if (instance != null)
            {
                if (instance.titleText == null) instance.BuildUI(existing);
                return instance;
            }
        }

        // Tạo Root Modal
        GameObject modalObj = new GameObject("PortalConfirmationModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(Image));
        modalObj.layer = 5;
        modalObj.transform.SetParent(targetCanvas.transform, false);

        RectTransform rt = modalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bgDim = modalObj.GetComponent<Image>();
        bgDim.color = new Color(0.015f, 0.02f, 0.035f, 0.82f);

        instance = modalObj.AddComponent<PortalConfirmationPopupUI>();
        instance.BuildUI(modalObj.transform);

        modalObj.SetActive(false);
        return instance;
    }

    private void BuildUI(Transform root)
    {
        // 1. Dialog Box Frame (Tỉ lệ chuẩn 530x390 rộng rãi, không chồng chữ)
        GameObject boxObj = new GameObject("DialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.layer = 5;
        boxObj.transform.SetParent(root, false);

        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.sizeDelta = new Vector2(530f, 390f);
        boxRt.anchoredPosition = Vector2.zero;

        Image boxImg = boxObj.GetComponent<Image>();
        boxImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        boxImg.type = Image.Type.Sliced;
        boxImg.color = Color.white;

        var boxOutline = boxObj.AddComponent<Outline>();
        boxOutline.effectColor = new Color(0.95f, 0.80f, 0.35f, 0.35f);
        boxOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // 2. Title Section
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.layer = 5;
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 150f);
        titleRt.sizeDelta = new Vector2(490f, 28f);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = LocalizationManager.T("DỊCH CHUYỂN CÕI GIỚI", "REALM WARP GATE");
        titleText.fontSize = 21f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.92f, 0.65f, 1f); // Antique Gold
        titleText.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(titleText, 0.35f, Color.black);

        // Subtitle
        GameObject subObj = new GameObject("SubText", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.layer = 5;
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0f, 124f);
        subRt.sizeDelta = new Vector2(490f, 20f);

        subtitleText = subObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = LocalizationManager.T("Vết nứt không gian kết nối tới cõi tiếp theo", "Traverse the spatial rift into the next realm");
        subtitleText.fontSize = 12.5f;
        subtitleText.fontStyle = FontStyles.Normal;
        subtitleText.color = new Color(0.75f, 0.80f, 0.90f, 0.85f);
        subtitleText.alignment = TextAlignmentOptions.Center;

        // Filigree Divider
        SoulslikeHUDStyler.AddFiligreeDivider(boxObj.transform, new Vector2(0f, 110f), new Vector2(420f, 8f));

        // 3. Destination Plaque Card
        GameObject plaqueObj = new GameObject("DestinationCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        plaqueObj.layer = 5;
        plaqueObj.transform.SetParent(boxObj.transform, false);
        RectTransform plaqueRt = plaqueObj.GetComponent<RectTransform>();
        plaqueRt.anchoredPosition = new Vector2(0f, 36f);
        plaqueRt.sizeDelta = new Vector2(480f, 122f);

        Image pImg = plaqueObj.GetComponent<Image>();
        pImg.sprite = SoulslikeHUDStyler.GetRunicPlaqueSprite();
        pImg.type = Image.Type.Sliced;
        pImg.color = new Color(0.12f, 0.14f, 0.22f, 0.95f);

        var pOutline = plaqueObj.AddComponent<Outline>();
        pOutline.effectColor = new Color(0.35f, 0.40f, 0.55f, 0.55f);
        pOutline.effectDistance = new Vector2(1f, -1f);

        // Destination Tag
        GameObject tagObj = new GameObject("DestTag", typeof(RectTransform), typeof(CanvasRenderer));
        tagObj.layer = 5;
        tagObj.transform.SetParent(plaqueObj.transform, false);
        RectTransform tagRt = tagObj.GetComponent<RectTransform>();
        tagRt.anchoredPosition = new Vector2(0f, 44f);
        tagRt.sizeDelta = new Vector2(460f, 18f);
        destTagText = tagObj.AddComponent<TextMeshProUGUI>();
        destTagText.text = LocalizationManager.T("ĐÍCH ĐẾN TIẾP THEO", "DESTINATION REALM");
        destTagText.fontSize = 11f;
        destTagText.fontStyle = FontStyles.Bold;
        destTagText.color = new Color(0.65f, 0.75f, 0.90f, 0.9f);
        destTagText.alignment = TextAlignmentOptions.Center;

        // Destination Name (Không ký tự lạ, chữ to rõ ràng)
        GameObject destNameObj = new GameObject("DestName", typeof(RectTransform), typeof(CanvasRenderer));
        destNameObj.layer = 5;
        destNameObj.transform.SetParent(plaqueObj.transform, false);
        RectTransform dnRt = destNameObj.GetComponent<RectTransform>();
        dnRt.anchoredPosition = new Vector2(0f, 22f);
        dnRt.sizeDelta = new Vector2(460f, 24f);
        destinationNameText = destNameObj.AddComponent<TextMeshProUGUI>();
        destinationNameText.fontSize = 16f;
        destinationNameText.fontStyle = FontStyles.Bold;
        destinationNameText.color = new Color(0.35f, 0.85f, 1f);
        destinationNameText.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(destinationNameText, 0.35f, Color.black);

        // Destination Description (Có chiều cao rộng rãi cho 3 dòng bullet points)
        GameObject destDescObj = new GameObject("DestDesc", typeof(RectTransform), typeof(CanvasRenderer));
        destDescObj.layer = 5;
        destDescObj.transform.SetParent(plaqueObj.transform, false);
        RectTransform ddRt = destDescObj.GetComponent<RectTransform>();
        ddRt.anchoredPosition = new Vector2(0f, -20f);
        ddRt.sizeDelta = new Vector2(450f, 54f);
        destinationDescText = destDescObj.AddComponent<TextMeshProUGUI>();
        destinationDescText.fontSize = 11f;
        destinationDescText.lineSpacing = 1.15f;
        destinationDescText.fontStyle = FontStyles.Normal;
        destinationDescText.color = new Color(0.80f, 0.85f, 0.92f, 0.90f);
        destinationDescText.alignment = TextAlignmentOptions.Center;
        destinationDescText.enableWordWrapping = true;

        // 4. Warning Notice Box
        GameObject warnObj = new GameObject("WarningText", typeof(RectTransform), typeof(CanvasRenderer));
        warnObj.layer = 5;
        warnObj.transform.SetParent(boxObj.transform, false);
        RectTransform warnRt = warnObj.GetComponent<RectTransform>();
        warnRt.anchoredPosition = new Vector2(0f, -54f);
        warnRt.sizeDelta = new Vector2(470f, 38f);

        warningNoticeText = warnObj.AddComponent<TextMeshProUGUI>();
        warningNoticeText.fontSize = 12f;
        warningNoticeText.lineSpacing = 2f;
        warningNoticeText.alignment = TextAlignmentOptions.Center;
        warningNoticeText.text = LocalizationManager.T(
            "Bạn có muốn bước qua cánh cổng không gian?\n<color=#F87171>Lưu ý: Mọi chiến lợi phẩm chưa nhặt trên bản đồ sẽ biến mất!</color>",
            "Do you wish to enter the realm rift?\n<color=#F87171>Notice: Any uncollected loot on this map will be lost!</color>"
        );

        // 5. Action Buttons Bar
        GameObject btnBarObj = new GameObject("ActionBar", typeof(RectTransform), typeof(CanvasRenderer));
        btnBarObj.layer = 5;
        btnBarObj.transform.SetParent(boxObj.transform, false);
        RectTransform barRt = btnBarObj.GetComponent<RectTransform>();
        barRt.anchoredPosition = new Vector2(0f, -128f);
        barRt.sizeDelta = new Vector2(460f, 48f);

        // Button 1: CANCEL [ Ở LẠI ]
        cancelBtn = CreateActionButton(btnBarObj.transform, "Btn_Cancel",
            LocalizationManager.T("Ở LẠI", "STAY"),
            new Vector2(-120f, 0f), new Vector2(185f, 44f),
            new Color(0.25f, 0.28f, 0.38f, 1f),
            new Color(0.85f, 0.88f, 0.95f),
            new Color(0.40f, 0.45f, 0.58f, 0.7f));
        cancelBtnText = cancelBtn.GetComponentInChildren<TextMeshProUGUI>();
        cancelBtn.onClick.AddListener(Cancel);

        // Button 2: PROCEED [ TIẾN VÀO ]
        proceedBtn = CreateActionButton(btnBarObj.transform, "Btn_Proceed",
            LocalizationManager.T("TIẾN VÀO", "ENTER"),
            new Vector2(120f, 0f), new Vector2(185f, 44f),
            new Color(0.50f, 0.18f, 0.78f, 1f),
            new Color(1f, 0.96f, 0.65f),
            new Color(1f, 0.85f, 0.35f, 0.9f));
        proceedBtnText = proceedBtn.GetComponentInChildren<TextMeshProUGUI>();
        proceedBtn.onClick.AddListener(ConfirmProceed);
    }

    private Button CreateActionButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color bgColor, Color textColor, Color outlineColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.layer = 5;
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image bg = btnObj.GetComponent<Image>();
        bg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        bg.type = Image.Type.Sliced;
        bg.color = bgColor;

        var outline = btnObj.AddComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(1.2f, -1.2f);

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = bg;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        btn.colors = colors;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.layer = 5;
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform tRt = txtObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 15f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(tmp, 0.35f, Color.black);

        return btn;
    }

    public void Open(RuinPortal portal, Transform player)
    {
        currentPortal = portal;
        currentCorruptedPortal = null;
        playerTransform = player;
        isOpen = true;

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        Time.timeScale = 0f; // Tạm dừng trận chiến để chọn an toàn
        SoundManager.EnsureInstance()?.PlayButtonClick();

        string currentScene = SceneManager.GetActiveScene().name;
        bool isHeavenly = (portal != null && portal.IsFinalRealm) || currentScene.Contains("Map3") || currentScene.Contains("Forgotten");

        if (isHeavenly)
        {
            if (titleText != null)
            {
                titleText.text = LocalizationManager.T("CỔNG THĂNG HOA: KẾT THÚC HÀNH TRÌNH", "HEAVENLY GATE: ASCENSION");
                titleText.color = new Color(1f, 0.92f, 0.65f, 1f); // Celestial Gold
            }
            if (subtitleText != null)
            {
                subtitleText.text = LocalizationManager.T("Bạn đã thanh tẩy cõi giới và đánh bại Skeleton King", "You have purified the realm and vanquished the Skeleton King");
            }
            if (destTagText != null)
            {
                destTagText.text = LocalizationManager.T("KẾT THÚC HÀNH TRÌNH", "RUN COMPLETION");
            }
            if (destinationNameText != null)
            {
                destinationNameText.text = LocalizationManager.T("CÕI THIÊN ĐƯỜNG (HOÀN THÀNH)", "HEAVENLY GATE (VICTORY)");
                destinationNameText.color = new Color(1f, 0.90f, 0.40f);
            }
            if (destinationDescText != null)
            {
                destinationDescText.text = LocalizationManager.T(
                    "Thanh tẩy hoàn toàn cõi tàn tích • Xem bảng vàng chiến tích anh hùng",
                    "Total realm purification • Conclude your heroic journey"
                );
            }
            if (warningNoticeText != null)
            {
                warningNoticeText.text = LocalizationManager.T(
                    "Bước vào Cổng Thăng Hoa để hoàn thành hành trình và khắc tên lên bảng vàng!",
                    "Ascend through the Heavenly Gate to complete your journey and seal your victory!"
                );
            }
            if (proceedBtnText != null)
            {
                proceedBtnText.text = LocalizationManager.T("THĂNG HOA", "ASCEND");
            }
        }
        else
        {
            if (titleText != null)
            {
                titleText.text = LocalizationManager.T("DỊCH CHUYỂN CÕI GIỚI", "REALM WARP GATE");
                titleText.color = new Color(1f, 0.92f, 0.65f, 1f); // Antique Gold
            }
            if (subtitleText != null)
            {
                subtitleText.text = LocalizationManager.T("Vết nứt không gian kết nối tới cõi tiếp theo", "Traverse the spatial rift into the next realm");
            }
            if (destTagText != null)
            {
                destTagText.text = LocalizationManager.T("ĐÍCH ĐẾN TIẾP THEO", "DESTINATION REALM");
            }
            if (warningNoticeText != null)
            {
                warningNoticeText.text = LocalizationManager.T(
                    "Bạn có muốn bước qua cánh cổng không gian?\n<color=#F87171>Lưu ý: Mọi chiến lợi phẩm chưa nhặt trên bản đồ sẽ biến mất!</color>",
                    "Do you wish to enter the realm rift?\n<color=#F87171>Notice: Any uncollected loot on this map will be lost!</color>"
                );
            }
            if (proceedBtnText != null)
            {
                proceedBtnText.text = LocalizationManager.T("TIẾN VÀO", "ENTER");
            }

            UpdateDestinationInfo();
        }
    }

    public void OpenForCorruptedPortal(CorruptedVoidPortal portal)
    {
        currentCorruptedPortal = portal;
        currentPortal = null;
        isOpen = true;

        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        Time.timeScale = 0f;
        SoundManager.EnsureInstance()?.PlayButtonClick();

        var nextRealm = CorruptedCycleManager.GetNextRealm();
        int nextCycle = (int)nextRealm;
        Color realmColor = CorruptedCycleManager.GetRealmColor(nextRealm);
        string realmName = CorruptedCycleManager.GetRealmName(nextRealm);

        if (titleText != null)
        {
            titleText.text = LocalizationManager.T($"CỔNG THẢM HỌA: {realmName}", $"CALAMITY GATE: {realmName}");
            titleText.color = realmColor;
        }
        if (subtitleText != null)
        {
            subtitleText.text = LocalizationManager.T($"Vết nứt không gian kết nối tới Chu Kỳ {nextCycle} (Calamity Realm)", $"Dimensional rift connecting into Calamity Cycle {nextCycle}");
        }
        if (destTagText != null)
        {
            destTagText.text = LocalizationManager.T($"CÕI GIỚI THẢM HỌA (CHU KỲ {nextCycle})", $"CALAMITY REALM (CYCLE {nextCycle})");
        }
        if (destinationNameText != null)
        {
            destinationNameText.text = LocalizationManager.T($"MAP 1: KHỞI ĐẦU ({realmName})", $"MAP 1: THESTART ({realmName})");
            destinationNameText.color = realmColor;
        }
        if (destinationDescText != null)
        {
            destinationDescText.text = CorruptedCycleManager.GetRealmDescription(nextRealm);
        }
        if (warningNoticeText != null)
        {
            warningNoticeText.text = LocalizationManager.T(
                "Bạn có muốn dấn thân vào Cõi Thảm Họa tiếp theo?\n<color=#F43F5E>CẢNH BÁO: Môi trường biến dị khốc liệt & Quái vật, Boss tăng vọt sức mạnh!</color>",
                "Do you dare to venture into the next Calamity Realm?\n<color=#F43F5E>WARNING: Harsh environmental hazards & Heavily empowered enemies!</color>"
            );
        }
        if (proceedBtnText != null)
        {
            proceedBtnText.text = LocalizationManager.T($"TIẾN VÀO CHU KỲ {nextCycle}", $"ENTER CYCLE {nextCycle}");
        }
    }

    private void UpdateDestinationInfo()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "SampleScene" || currentScene.Contains("Map1"))
        {
            if (destinationNameText != null)
            {
                destinationNameText.text = LocalizationManager.T("THUNG LŨNG TÀN TÍCH (MAP 2)", "VALLEY OF RUIN (MAP 2)");
                destinationNameText.color = new Color(0.35f, 0.85f, 1f); // Sapphire Cyan
            }
            if (destinationDescText != null)
            {
                destinationDescText.text = LocalizationManager.T(
                    "Vùng đất hoang tàn • Xuất hiện Bọ Cạp Độc và Tộc Goblin",
                    "Perilous canyon • Poison Spitters and fierce Goblins"
                );
            }
        }
        else if (currentScene.Contains("Map2") || currentScene.Contains("Valley"))
        {
            if (destinationNameText != null)
            {
                destinationNameText.text = LocalizationManager.T("KÝ ỨC LÃNG QUÊN (MAP 3)", "FORGOTTEN MEMORIES (MAP 3)");
                destinationNameText.color = new Color(0.80f, 0.45f, 1f); // Amethyst Purple
            }
            if (destinationDescText != null)
            {
                destinationDescText.text = LocalizationManager.T(
                    "Cõi tâm thức cổ xưa • Nơi ngự trị của Skeleton King",
                    "Ancient sanctum • Realm of the dread Skeleton King"
                );
            }
        }
        else if (currentScene.Contains("Map3") || currentScene.Contains("Forgotten"))
        {
            if (destinationNameText != null)
            {
                destinationNameText.text = LocalizationManager.T("CÕI THIÊN ĐƯỜNG (HOÀN THÀNH)", "HEAVENLY GATE (ASCENSION)");
                destinationNameText.color = new Color(1f, 0.90f, 0.40f); // Celestial Gold
            }
            if (destinationDescText != null)
            {
                destinationDescText.text = LocalizationManager.T(
                    "Thanh tẩy hoàn toàn tàn tích • Ghi danh chiến công sử thi",
                    "Total realm purification • Complete your legendary run"
                );
            }
        }
    }

    public void Cancel()
    {
        if (!isOpen) return;
        isOpen = false;
        LastClosedTime = Time.unscaledTime;

        Time.timeScale = 1f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
        SoundManager.EnsureInstance()?.PlayButtonClick();

        if (currentPortal != null)
        {
            currentPortal.OnConfirmationClosed();
        }
    }

    public void ConfirmProceed()
    {
        if (!isOpen) return;
        isOpen = false;
        LastClosedTime = Time.unscaledTime;

        Time.timeScale = 1f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
        SoundManager.EnsureInstance()?.PlayButtonClick();

        if (currentCorruptedPortal != null)
        {
            currentCorruptedPortal.EnterCorruptedCycle();
        }
        else if (currentPortal != null)
        {
            currentPortal.ConfirmProceed(playerTransform);
        }
    }
}
