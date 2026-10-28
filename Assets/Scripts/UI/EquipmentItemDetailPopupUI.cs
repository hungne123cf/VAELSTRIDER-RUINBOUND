// ========================================================
// VAELSTRIDER: RUINBOUND - Equipment & Gem Stat Detail Popup UI
// Hiển thị popup chi tiết chỉ số hiện tại (scaled by level) khi người chơi chạm/click vào ô trang bị/ngọc
// Thiết kế chuẩn Dark Fantasy Gothic, chống tràn viền và hiển thị sắc nét trên Mobile & PC
// ========================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

public class EquipmentItemDetailPopupUI : MonoBehaviour
{
    private static EquipmentItemDetailPopupUI _instance;
    public static EquipmentItemDetailPopupUI Instance => _instance;

    private CanvasGroup canvasGroup;
    private GameObject dialogBox;
    private RectTransform dialogBoxRt;

    // UI References
    private TextMeshProUGUI slotHeaderText;
    private Image itemIconImg;
    private GameObject itemIconFrameObj;
    private TextMeshProUGUI itemNameText;
    private TextMeshProUGUI itemSubInfoText;
    private TextMeshProUGUI itemCategoryText;
    private TextMeshProUGUI statsBodyText;
    private ScrollRect statsScrollRect;
    private RectTransform statsContentRt;
    private TextMeshProUGUI itemDescText;
    private Button unsocketBtn;
    private TextMeshProUGUI unsocketBtnText;
    private Button closeBtn;
    private TextMeshProUGUI closeBtnText;
    private Button backdropDismissBtn;
    private Image dividerImg;

    // State
    private int currentSlotIdx = -1;
    private string currentGemId = "";
    private int currentLevel = 1;
    private string currentHeroId = "";
    private Action onUnsocketCallback;
    private Coroutine popAnimCor;

    void Awake()
    {
        _instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public static EquipmentItemDetailPopupUI EnsureUIExists(Transform parentCanvas = null)
    {
        Canvas targetCanvas = null;
        if (parentCanvas != null)
        {
            targetCanvas = parentCanvas.GetComponent<Canvas>();
            if (targetCanvas == null) targetCanvas = parentCanvas.GetComponentInParent<Canvas>();
            if (targetCanvas != null && targetCanvas.rootCanvas != null) targetCanvas = targetCanvas.rootCanvas;
        }
        if (targetCanvas == null)
        {
            var inGameModal = InGameInventoryUI.Instance;
            if (inGameModal != null && inGameModal.gameObject.activeInHierarchy)
            {
                targetCanvas = inGameModal.GetComponent<Canvas>() ?? inGameModal.GetComponentInParent<Canvas>();
                if (targetCanvas != null && targetCanvas.rootCanvas != null) targetCanvas = targetCanvas.rootCanvas;
            }
        }
        if (targetCanvas == null)
        {
            targetCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        }
        if (targetCanvas == null)
        {
            targetCanvas = Object.FindAnyObjectByType<Canvas>();
        }

        if (targetCanvas == null)
        {
            GameObject cObj = new GameObject("PopupCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            targetCanvas = cObj.GetComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 999;

            var scaler = cObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (_instance != null && _instance.gameObject != null)
        {
            if (targetCanvas != null && _instance.transform.parent != targetCanvas.transform)
            {
                _instance.transform.SetParent(targetCanvas.transform, false);
            }
            _instance.transform.SetAsLastSibling();
            RectTransform mRt = _instance.GetComponent<RectTransform>();
            if (mRt != null)
            {
                mRt.anchorMin = Vector2.zero;
                mRt.anchorMax = Vector2.one;
                mRt.offsetMin = Vector2.zero;
                mRt.offsetMax = Vector2.zero;
                mRt.localScale = Vector3.one;
            }
            var cv = _instance.GetComponent<Canvas>();
            if (cv == null) cv = _instance.gameObject.AddComponent<Canvas>();
            cv.overrideSorting = true;
            cv.sortingOrder = 32000;
            if (_instance.GetComponent<GraphicRaycaster>() == null) _instance.gameObject.AddComponent<GraphicRaycaster>();
            return _instance;
        }

        Transform existing = targetCanvas.transform.Find("EquipmentItemDetailPopupModal");
        if (existing != null)
        {
            _instance = existing.GetComponent<EquipmentItemDetailPopupUI>();
            if (_instance != null)
            {
                _instance.transform.SetAsLastSibling();
                return _instance;
            }
        }

        // Tạo Modal Root
        GameObject modalObj = new GameObject("EquipmentItemDetailPopupModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(Image), typeof(Button));
        modalObj.layer = 5;
        modalObj.transform.SetParent(targetCanvas.transform, false);
        modalObj.transform.SetAsLastSibling();

        Canvas modalCanvas = modalObj.AddComponent<Canvas>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = 32000;
        modalObj.AddComponent<GraphicRaycaster>();

        RectTransform rt = modalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bgDim = modalObj.GetComponent<Image>();
        bgDim.color = new Color(0.02f, 0.03f, 0.05f, 0.82f);
        bgDim.raycastTarget = true;

        Button bgBtn = modalObj.GetComponent<Button>();
        bgBtn.transition = Selectable.Transition.None;

        _instance = modalObj.AddComponent<EquipmentItemDetailPopupUI>();
        _instance.backdropDismissBtn = bgBtn;
        _instance.BuildUI(modalObj.transform);

        modalObj.SetActive(false);
        return _instance;
    }

    private void BuildUI(Transform root)
    {
        backdropDismissBtn.onClick.RemoveAllListeners();
        backdropDismissBtn.onClick.AddListener(Hide);

        // 1. Center Dialog Box (470 x 550)
        dialogBox = new GameObject("ItemDetailDialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogBox.layer = 5;
        dialogBox.transform.SetParent(root, false);

        dialogBoxRt = dialogBox.GetComponent<RectTransform>();
        dialogBoxRt.anchorMin = new Vector2(0.5f, 0.5f);
        dialogBoxRt.anchorMax = new Vector2(0.5f, 0.5f);
        dialogBoxRt.pivot = new Vector2(0.5f, 0.5f);
        dialogBoxRt.anchoredPosition = Vector2.zero;
        dialogBoxRt.sizeDelta = new Vector2(470f, 550f);

        Image boxImg = dialogBox.GetComponent<Image>();
        boxImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        boxImg.type = Image.Type.Sliced;
        boxImg.color = Color.white;
        boxImg.raycastTarget = true;
        WhiteIconLoader.ApplyGothicFrame(dialogBox, Color.white, 0.40f);

        // 2. Header Bar (Slot Category + Top-Right Close Button)
        GameObject headerObj = new GameObject("HeaderBar", typeof(RectTransform));
        headerObj.transform.SetParent(dialogBox.transform, false);
        RectTransform hRt = headerObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = new Vector2(0f, -14f);
        hRt.sizeDelta = new Vector2(-36f, 32f);

        GameObject slotTxtObj = new GameObject("SlotHeaderText", typeof(RectTransform), typeof(CanvasRenderer));
        slotTxtObj.transform.SetParent(headerObj.transform, false);
        RectTransform stRt = slotTxtObj.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 0f);
        stRt.anchorMax = new Vector2(1f, 1f);
        stRt.offsetMin = new Vector2(4f, 0f);
        stRt.offsetMax = new Vector2(-44f, 0f);
        slotHeaderText = slotTxtObj.AddComponent<TextMeshProUGUI>();
        slotHeaderText.fontSize = 15f;
        slotHeaderText.fontStyle = FontStyles.Bold;
        slotHeaderText.color = new Color(0.98f, 0.88f, 0.55f);
        slotHeaderText.alignment = TextAlignmentOptions.MidlineLeft;
        slotHeaderText.raycastTarget = false;

        // Top-Right Close Button [X]
        GameObject xBtnObj = new GameObject("Btn_CloseTopRight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        xBtnObj.transform.SetParent(headerObj.transform, false);
        RectTransform xRt = xBtnObj.GetComponent<RectTransform>();
        xRt.anchorMin = new Vector2(1f, 0.5f);
        xRt.anchorMax = new Vector2(1f, 0.5f);
        xRt.pivot = new Vector2(1f, 0.5f);
        xRt.anchoredPosition = Vector2.zero;
        xRt.sizeDelta = new Vector2(32f, 32f);
        Button xBtn = xBtnObj.GetComponent<Button>();
        xBtn.onClick.AddListener(Hide);
        SoulslikeHUDStyler.StyleCloseButton(xBtn);

        // 3. Identity Section (Icon on Left + Texts explicitly positioned on Right)
        GameObject idSection = new GameObject("IdentitySection", typeof(RectTransform));
        idSection.transform.SetParent(dialogBox.transform, false);
        RectTransform idsRt = idSection.GetComponent<RectTransform>();
        idsRt.anchorMin = new Vector2(0f, 1f);
        idsRt.anchorMax = new Vector2(1f, 1f);
        idsRt.pivot = new Vector2(0.5f, 1f);
        idsRt.anchoredPosition = new Vector2(0f, -50f);
        idsRt.sizeDelta = new Vector2(-36f, 76f);

        // Item Icon Box (72 x 72)
        GameObject iconBox = new GameObject("ItemIconBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconBox.transform.SetParent(idSection.transform, false);
        RectTransform ibRt = iconBox.GetComponent<RectTransform>();
        ibRt.anchorMin = new Vector2(0f, 0.5f);
        ibRt.anchorMax = new Vector2(0f, 0.5f);
        ibRt.pivot = new Vector2(0f, 0.5f);
        ibRt.anchoredPosition = new Vector2(2f, 0f);
        ibRt.sizeDelta = new Vector2(72f, 72f);
        Image ibImg = iconBox.GetComponent<Image>();
        ibImg.color = new Color(0.04f, 0.05f, 0.09f, 1f);
        itemIconFrameObj = iconBox;
        WhiteIconLoader.ApplyGothicFrame(iconBox, new Color(0.85f, 0.72f, 0.38f, 0.75f), 0.40f);

        GameObject icObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        icObj.transform.SetParent(iconBox.transform, false);
        RectTransform icRt = icObj.GetComponent<RectTransform>();
        icRt.anchorMin = Vector2.zero;
        icRt.anchorMax = Vector2.one;
        icRt.sizeDelta = new Vector2(-8f, -8f);
        itemIconImg = icObj.GetComponent<Image>();
        itemIconImg.preserveAspect = true;
        itemIconImg.raycastTarget = false;

        // Item Name (Row 1 on Right)
        GameObject nameObj = new GameObject("ItemNameText", typeof(RectTransform), typeof(CanvasRenderer));
        nameObj.transform.SetParent(idSection.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0f, 1f);
        nameRt.anchoredPosition = new Vector2(88f, -4f);
        nameRt.sizeDelta = new Vector2(-92f, 24f);
        itemNameText = nameObj.AddComponent<TextMeshProUGUI>();
        itemNameText.fontSize = 18f;
        itemNameText.fontStyle = FontStyles.Bold;
        itemNameText.color = Color.white;
        itemNameText.alignment = TextAlignmentOptions.MidlineLeft;
        itemNameText.enableWordWrapping = false;
        itemNameText.overflowMode = TextOverflowModes.Ellipsis;
        itemNameText.raycastTarget = false;

        // Sub Info (Level + Multiplier + Absorbed count in ONE clean single line - Row 2 on Right)
        GameObject subInfoObj = new GameObject("ItemSubInfoText", typeof(RectTransform), typeof(CanvasRenderer));
        subInfoObj.transform.SetParent(idSection.transform, false);
        RectTransform siRt = subInfoObj.GetComponent<RectTransform>();
        siRt.anchorMin = new Vector2(0f, 1f);
        siRt.anchorMax = new Vector2(1f, 1f);
        siRt.pivot = new Vector2(0f, 1f);
        siRt.anchoredPosition = new Vector2(88f, -30f);
        siRt.sizeDelta = new Vector2(-92f, 20f);
        itemSubInfoText = subInfoObj.AddComponent<TextMeshProUGUI>();
        itemSubInfoText.fontSize = 13.5f;
        itemSubInfoText.alignment = TextAlignmentOptions.MidlineLeft;
        itemSubInfoText.enableWordWrapping = false;
        itemSubInfoText.raycastTarget = false;

        // Category Tag (Row 3 on Right)
        GameObject catObj = new GameObject("CategoryText", typeof(RectTransform), typeof(CanvasRenderer));
        catObj.transform.SetParent(idSection.transform, false);
        RectTransform catRt = catObj.GetComponent<RectTransform>();
        catRt.anchorMin = new Vector2(0f, 1f);
        catRt.anchorMax = new Vector2(1f, 1f);
        catRt.pivot = new Vector2(0f, 1f);
        catRt.anchoredPosition = new Vector2(88f, -52f);
        catRt.sizeDelta = new Vector2(-92f, 18f);
        itemCategoryText = catObj.AddComponent<TextMeshProUGUI>();
        itemCategoryText.fontSize = 11.5f;
        itemCategoryText.color = new Color(0.6f, 0.7f, 0.82f);
        itemCategoryText.alignment = TextAlignmentOptions.MidlineLeft;
        itemCategoryText.enableWordWrapping = false;
        itemCategoryText.raycastTarget = false;

        // 4. Decorative Divider Line
        GameObject divObj = new GameObject("DividerLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        divObj.transform.SetParent(dialogBox.transform, false);
        RectTransform divRt = divObj.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0f, 1f);
        divRt.anchorMax = new Vector2(1f, 1f);
        divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0f, -132f);
        divRt.sizeDelta = new Vector2(-36f, 2f);
        dividerImg = divObj.GetComponent<Image>();
        dividerImg.color = new Color(0.85f, 0.72f, 0.38f, 0.40f);
        Sprite divSpr = WhiteIconLoader.GetDividerFadeSprite();
        if (divSpr != null) { dividerImg.sprite = divSpr; dividerImg.type = Image.Type.Simple; }

        // 5. Stats Container Box (Sleek dark panel with clean border, NO clunky heavy frame)
        GameObject statsBox = new GameObject("StatsBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        statsBox.transform.SetParent(dialogBox.transform, false);
        RectTransform sbRt = statsBox.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0f, 1f);
        sbRt.anchorMax = new Vector2(1f, 1f);
        sbRt.pivot = new Vector2(0.5f, 1f);
        sbRt.anchoredPosition = new Vector2(0f, -140f);
        sbRt.sizeDelta = new Vector2(-36f, 246f);

        Image sbImg = statsBox.GetComponent<Image>();
        sbImg.color = new Color(0.03f, 0.04f, 0.07f, 0.92f);
        var sbOutline = statsBox.AddComponent<Outline>();
        sbOutline.effectColor = new Color(0.35f, 0.45f, 0.60f, 0.45f);
        sbOutline.effectDistance = new Vector2(1f, -1f);

        // Header label inside stats box
        GameObject statsHeadObj = new GameObject("StatsHeadLabel", typeof(RectTransform), typeof(CanvasRenderer));
        statsHeadObj.transform.SetParent(statsBox.transform, false);
        RectTransform shRt = statsHeadObj.GetComponent<RectTransform>();
        shRt.anchorMin = new Vector2(0f, 1f);
        shRt.anchorMax = new Vector2(1f, 1f);
        shRt.pivot = new Vector2(0.5f, 1f);
        shRt.anchoredPosition = new Vector2(0f, -8f);
        shRt.sizeDelta = new Vector2(-24f, 22f);
        var shTmp = statsHeadObj.AddComponent<TextMeshProUGUI>();
        shTmp.fontSize = 12.5f;
        shTmp.fontStyle = FontStyles.Bold;
        shTmp.color = new Color(1f, 0.85f, 0.40f);
        shTmp.text = LocalizationManager.T("HIỆU QUẢ CƯỜNG HÓA HIỆN TẠI:", "CURRENT ENHANCED STATS:");
        shTmp.raycastTarget = false;

        // Scrollable Stats Viewport (with RectMask2D)
        GameObject scrollObj = new GameObject("StatsScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        scrollObj.transform.SetParent(statsBox.transform, false);
        RectTransform svRt = scrollObj.GetComponent<RectTransform>();
        svRt.anchorMin = Vector2.zero;
        svRt.anchorMax = Vector2.one;
        svRt.offsetMin = new Vector2(14f, 8f);
        svRt.offsetMax = new Vector2(-14f, -32f);

        statsScrollRect = scrollObj.GetComponent<ScrollRect>();
        statsScrollRect.horizontal = false;
        statsScrollRect.vertical = true;
        statsScrollRect.movementType = ScrollRect.MovementType.Clamped;
        statsScrollRect.scrollSensitivity = 25f;

        // Stats Content Container
        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(scrollObj.transform, false);
        statsContentRt = contentObj.GetComponent<RectTransform>();
        statsContentRt.anchorMin = new Vector2(0f, 1f);
        statsContentRt.anchorMax = new Vector2(1f, 1f);
        statsContentRt.pivot = new Vector2(0.5f, 1f);
        statsContentRt.anchoredPosition = Vector2.zero;
        statsContentRt.sizeDelta = new Vector2(0f, 300f);

        statsScrollRect.content = statsContentRt;
        statsScrollRect.viewport = svRt;

        // Single TextMeshPro for Stats + Base Comparison (Zero overlap guaranteed!)
        GameObject statsTextObj = new GameObject("StatsText", typeof(RectTransform), typeof(CanvasRenderer));
        statsTextObj.transform.SetParent(contentObj.transform, false);
        RectTransform stextRt = statsTextObj.GetComponent<RectTransform>();
        stextRt.anchorMin = new Vector2(0f, 1f);
        stextRt.anchorMax = new Vector2(1f, 1f);
        stextRt.pivot = new Vector2(0.5f, 1f);
        stextRt.anchoredPosition = Vector2.zero;
        stextRt.sizeDelta = new Vector2(0f, 300f);

        statsBodyText = statsTextObj.AddComponent<TextMeshProUGUI>();
        statsBodyText.fontSize = 13.5f;
        statsBodyText.lineSpacing = 1.3f;
        statsBodyText.color = Color.white;
        statsBodyText.enableWordWrapping = true;
        statsBodyText.raycastTarget = false;

        // 6. Item Lore / Description Box
        GameObject descBox = new GameObject("DescBox", typeof(RectTransform), typeof(CanvasRenderer));
        descBox.transform.SetParent(dialogBox.transform, false);
        RectTransform dbRt = descBox.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(0f, 0f);
        dbRt.anchorMax = new Vector2(1f, 0f);
        dbRt.pivot = new Vector2(0.5f, 0f);
        dbRt.anchoredPosition = new Vector2(0f, 72f);
        dbRt.sizeDelta = new Vector2(-40f, 44f);

        itemDescText = descBox.AddComponent<TextMeshProUGUI>();
        itemDescText.fontSize = 12f;
        itemDescText.fontStyle = FontStyles.Italic;
        itemDescText.color = new Color(0.70f, 0.75f, 0.85f, 0.90f);
        itemDescText.alignment = TextAlignmentOptions.TopLeft;
        itemDescText.enableWordWrapping = true;
        itemDescText.overflowMode = TextOverflowModes.Ellipsis;
        itemDescText.raycastTarget = false;

        // 7. Bottom Action Buttons Bar (Unsocket + Close)
        GameObject btnBar = new GameObject("ButtonBar", typeof(RectTransform));
        btnBar.transform.SetParent(dialogBox.transform, false);
        RectTransform bbRt = btnBar.GetComponent<RectTransform>();
        bbRt.anchorMin = new Vector2(0f, 0f);
        bbRt.anchorMax = new Vector2(1f, 0f);
        bbRt.pivot = new Vector2(0.5f, 0f);
        bbRt.anchoredPosition = new Vector2(0f, 16f);
        bbRt.sizeDelta = new Vector2(-36f, 44f);

        var hlgBtns = btnBar.AddComponent<HorizontalLayoutGroup>();
        hlgBtns.childControlWidth = true;
        hlgBtns.childControlHeight = true;
        hlgBtns.childForceExpandWidth = true;
        hlgBtns.childForceExpandHeight = true;
        hlgBtns.spacing = 14f;

        // Unsocket Button (Soulslike Stone Button)
        GameObject unBtnObj = new GameObject("Btn_Unsocket", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        unBtnObj.transform.SetParent(btnBar.transform, false);
        unsocketBtn = unBtnObj.GetComponent<Button>();
        unsocketBtn.onClick.AddListener(OnUnsocketClicked);

        GameObject unTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        unTxtObj.transform.SetParent(unBtnObj.transform, false);
        RectTransform utRt = unTxtObj.GetComponent<RectTransform>();
        utRt.anchorMin = Vector2.zero; utRt.anchorMax = Vector2.one; utRt.sizeDelta = Vector2.zero;
        unsocketBtnText = unTxtObj.AddComponent<TextMeshProUGUI>();
        unsocketBtnText.fontSize = 14.5f;
        unsocketBtnText.fontStyle = FontStyles.Bold;
        unsocketBtnText.alignment = TextAlignmentOptions.Center;
        unsocketBtnText.color = new Color(1f, 0.65f, 0.65f);
        unsocketBtnText.raycastTarget = false;
        SoulslikeHUDStyler.StyleButton(unsocketBtn, null, new Color(1f, 0.65f, 0.65f));

        // Close Button (Soulslike Stone Button)
        GameObject cBtnObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        cBtnObj.transform.SetParent(btnBar.transform, false);
        closeBtn = cBtnObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(Hide);

        GameObject cTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        cTxtObj.transform.SetParent(cBtnObj.transform, false);
        RectTransform ctRt = cTxtObj.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero; ctRt.anchorMax = Vector2.one; ctRt.sizeDelta = Vector2.zero;
        closeBtnText = cTxtObj.AddComponent<TextMeshProUGUI>();
        closeBtnText.fontSize = 14.5f;
        closeBtnText.fontStyle = FontStyles.Bold;
        closeBtnText.alignment = TextAlignmentOptions.Center;
        closeBtnText.color = new Color(0.96f, 0.88f, 0.65f);
        closeBtnText.raycastTarget = false;
        SoulslikeHUDStyler.StyleButton(closeBtn, null, new Color(0.96f, 0.88f, 0.65f));
    }

    /// <summary>
    /// Hiển thị Popup chi tiết chỉ số hiện tại của ngọc/trang bị trong ô socket.
    /// </summary>
    public static void Show(int slotIndex, string gemId, int itemLevel, string heroId, Action onUnsocket = null, Transform parentCanvas = null)
    {
        // Guard: If clicking a socket slot, verify that the socket is truly occupied in EquipmentSocketManager
        if (slotIndex >= 0)
        {
            if (!EquipmentSocketManager.GetHeroSocketGem(slotIndex, out string realGem, out int realLvl, heroId) || string.IsNullOrEmpty(realGem))
            {
                // Slot is empty - do not show stale gem popup!
                Hide();
                return;
            }
            gemId = realGem;
            itemLevel = realLvl;
        }

        var popup = EnsureUIExists(parentCanvas);
        if (popup == null) return;
        popup.transform.SetAsLastSibling();
        var cv = popup.GetComponent<Canvas>();
        if (cv == null) cv = popup.gameObject.AddComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingOrder = 32000;
        if (popup.GetComponent<GraphicRaycaster>() == null) popup.gameObject.AddComponent<GraphicRaycaster>();
        popup.Populate(slotIndex, gemId, itemLevel, heroId, onUnsocket);
    }

    public void Populate(int slotIndex, string gemId, int itemLevel, string heroId, Action onUnsocket)
    {
        currentSlotIdx = slotIndex;
        currentGemId = gemId;
        currentLevel = Mathf.Max(1, itemLevel);
        currentHeroId = heroId;
        onUnsocketCallback = onUnsocket;

        bool isVi = LocalizationManager.IsVietnamese;
        var itemDef = InventoryItemDatabase.GetItem(gemId);
        var gemEff = EquipmentSocketManager.GetGemEffect(gemId);
        Color gemCol = gemEff.HasValue ? gemEff.Value.gemColor : new Color(0.85f, 0.72f, 0.38f, 1f);

        // 1. Header Slot Name
        if (slotIndex >= 0 && slotIndex < EquipmentSocketManager.HERO_SOCKET_COUNT)
        {
            if (slotHeaderText != null)
            {
                slotHeaderText.text = $"<color=#FFDF66><b>{(isVi ? $"Ô TRANG BỊ #{slotIndex + 1}" : $"EQUIPMENT SLOT #{slotIndex + 1}")}</b></color>";
            }
        }
        else
        {
            if (slotHeaderText != null)
            {
                slotHeaderText.text = $"<color=#FFDF66><b>{(isVi ? "VẬT PHẨM TRONG TÚI" : "ITEM IN BAG")}</b></color>";
            }
        }

        // 2. Icon & Glow Frame on Icon Box only
        if (itemIconImg != null)
        {
            itemIconImg.sprite = (itemDef != null) ? itemDef.Icon : null;
            itemIconImg.enabled = (itemIconImg.sprite != null);
        }
        if (itemIconFrameObj != null)
        {
            WhiteIconLoader.ApplyGothicFrame(itemIconFrameObj, gemCol, 0.40f);
        }
        if (dividerImg != null)
        {
            dividerImg.color = new Color(gemCol.r, gemCol.g, gemCol.b, 0.50f);
        }

        // 3. Name & Sub Information
        string displayName = (gemEff.HasValue && !string.IsNullOrEmpty(gemEff.Value.nameVi))
            ? (isVi ? gemEff.Value.nameVi : gemEff.Value.nameEn)
            : (itemDef != null ? (isVi ? itemDef.nameVi : itemDef.nameEn) : gemId);

        string hexColor = ColorUtility.ToHtmlStringRGB(gemCol);
        if (itemNameText != null)
        {
            itemNameText.text = $"<color=#{hexColor}><b>{displayName}</b></color>";
        }

        float mult = EquipmentSocketManager.GetLevelMultiplier(currentLevel);
        if (itemSubInfoText != null)
        {
            if (slotIndex >= 0)
            {
                string absorbedMsg = isVi ? $"Đã hợp nhất {currentLevel} viên" : $"Absorbed {currentLevel} gems";
                itemSubInfoText.text = $"<color=#22C55E><b>Lv.{currentLevel}</b></color>   <color=#00E5FF><b>(x{mult:F2})</b></color>   <color=#94A3B8>• {absorbedMsg}</color>";
            }
            else
            {
                int bagQty = AccountDatabase.GetActiveUserItemCount(gemId);
                if (bagQty <= 0) bagQty = currentLevel;
                itemSubInfoText.text = $"<color=#00FF88><b>{(isVi ? $"Số lượng trong túi: x{bagQty}" : $"Bag Quantity: x{bagQty}")}</b></color>   <color=#94A3B8>• {(isVi ? "Chưa trang bị" : "Unsocketed")}</color>";
            }
        }

        if (itemCategoryText != null)
        {
            itemCategoryText.text = isVi ? "Khoáng Thạch / Đá Quý Hấp Thu" : "Socketed Ore & Gemstone";
        }

        // 4. Calculate Current Scaled Stats
        int bAtk = 0, bPhysAtk = 0, bMagicAtk = 0, bTrueAtk = 0, bHp = 0, bDef = 0;
        float lifestealPct = 0f, cdReductionPct = 0f, shardBonusPct = 0f, hpRegenPerSec = 0f;
        float bonusMaxEnergy = 0f, energyRegenPerSec = 0f, physLifePct = 0f, spellVampPct = 0f, critChancePct = 0f;

        if (slotIndex >= 0)
        {
            EquipmentSocketManager.GetHeroSocketBonus(
                slotIndex,
                out bAtk,
                out bPhysAtk,
                out bMagicAtk,
                out bTrueAtk,
                out bHp,
                out bDef,
                out lifestealPct,
                out cdReductionPct,
                out shardBonusPct,
                out hpRegenPerSec,
                out bonusMaxEnergy,
                out energyRegenPerSec,
                out physLifePct,
                out spellVampPct,
                out critChancePct,
                heroId
            );
        }
        else if (gemEff.HasValue)
        {
            var effect = gemEff.Value;
            float m = EquipmentSocketManager.GetLevelMultiplier(currentLevel);
            bAtk = Mathf.RoundToInt(effect.bonusAtk * m);
            bPhysAtk = Mathf.RoundToInt(effect.bonusPhysAtk * m);
            bMagicAtk = Mathf.RoundToInt(effect.bonusMagicAtk * m);
            bTrueAtk = Mathf.RoundToInt(effect.bonusTrueAtk * m);
            bHp = Mathf.RoundToInt(effect.bonusHp * m);
            bDef = Mathf.RoundToInt(effect.bonusDef * m);
            lifestealPct = effect.lifestealPct * m;
            cdReductionPct = effect.cdReductionPct * m;
            shardBonusPct = effect.shardBonusPct * m;
            hpRegenPerSec = effect.hpRegenPerSec * m;
            bonusMaxEnergy = effect.bonusMaxEnergy * m;
            energyRegenPerSec = effect.energyRegenPerSec * m;
            physLifePct = effect.physLifestealPct * m;
            spellVampPct = effect.spellVampPct * m;
            critChancePct = effect.critChancePct * m;
        }

        List<string> statLines = new List<string>();

        if (bPhysAtk > 0)
            statLines.Add($"<color=#FF3B30>• {(isVi ? "Sát Thương Vật Lý:" : "Physical ATK:")} <b>+{bPhysAtk} ATK</b></color>");
        if (bMagicAtk > 0)
            statLines.Add($"<color=#B84DFF>• {(isVi ? "Sát Thương Phép:" : "Magic ATK:")} <b>+{bMagicAtk} AP</b></color>");
        if (bTrueAtk > 0)
            statLines.Add($"<color=#E2E8F0>• {(isVi ? "Sát Thương Chuẩn:" : "True Damage:")} <b>+{bTrueAtk} TD</b></color>");
        if (bPhysAtk == 0 && bMagicAtk == 0 && bTrueAtk == 0 && bAtk > 0)
            statLines.Add($"<color=#FF3B30>• {(isVi ? "Công Kích:" : "Total ATK:")} <b>+{bAtk} ATK</b></color>");

        if (bHp > 0)
            statLines.Add($"<color=#22C55E>• {(isVi ? "Máu Tối Đa:" : "Max HP:")} <b>+{bHp} HP</b></color>");
        if (bDef > 0)
            statLines.Add($"<color=#38BDF8>• {(isVi ? "Giáp Phòng Thủ:" : "Armor DEF:")} <b>+{bDef} DEF</b></color>");

        if (critChancePct > 0.001f)
            statLines.Add($"<color=#F59E0B>• {(isVi ? "Tỉ Lệ Chí Mạng:" : "CRIT Rate:")} <b>+{Mathf.RoundToInt(critChancePct * 100f)}% CRIT</b></color>");

        if (physLifePct > 0.001f)
            statLines.Add($"<color=#F43F5E>• {(isVi ? "Hút Máu Vật Lý:" : "Phys Lifesteal:")} <b>+{Mathf.RoundToInt(physLifePct * 100f)}%</b></color>");
        if (spellVampPct > 0.001f)
            statLines.Add($"<color=#E879F9>• {(isVi ? "Hút Máu Phép:" : "Spell Vamp:")} <b>+{Mathf.RoundToInt(spellVampPct * 100f)}%</b></color>");
        if (physLifePct <= 0.001f && spellVampPct <= 0.001f && lifestealPct > 0.001f)
            statLines.Add($"<color=#F43F5E>• {(isVi ? "Hút Máu:" : "Lifesteal:")} <b>+{Mathf.RoundToInt(lifestealPct * 100f)}%</b></color>");

        if (cdReductionPct > 0.001f)
            statLines.Add($"<color=#C084FC>• {(isVi ? "Giảm Hồi Chiêu:" : "CD Reduction:")} <b>-{Mathf.RoundToInt(cdReductionPct * 100f)}% CD</b></color>");

        if (shardBonusPct > 0.001f)
            statLines.Add($"<color=#FBBF24>• {(isVi ? "Thưởng Shards:" : "Aethel Shards:")} <b>+{Mathf.RoundToInt(shardBonusPct * 100f)}% Shards</b></color>");

        if (hpRegenPerSec > 0.01f)
            statLines.Add($"<color=#10B981>• {(isVi ? "Hồi Máu Tự Nhiên:" : "HP Regeneration:")} <b>+{hpRegenPerSec:F1} HP/s</b></color>");

        if (bonusMaxEnergy > 0.01f)
            statLines.Add($"<color=#00E5FF>• {(isVi ? "Năng Lượng Tối Đa:" : "Max Mana:")} <b>+{Mathf.RoundToInt(bonusMaxEnergy)} Mana</b></color>");

        if (energyRegenPerSec > 0.01f)
            statLines.Add($"<color=#06B6D4>• {(isVi ? "Hồi Năng Lượng:" : "Mana Regeneration:")} <b>+{energyRegenPerSec:F1} Mana/s</b></color>");

        if (statLines.Count == 0)
        {
            statLines.Add($"<color=#94A3B8><i>{(isVi ? "Không có chỉ số kích hoạt." : "No active bonus stats.")}</i></color>");
        }

        // Base Stat summary
        string baseSummary = gemEff.HasValue 
            ? (isVi ? gemEff.Value.effectSummaryVi : gemEff.Value.effectSummaryEn) 
            : "";
        
        string fullStatsBody = string.Join("\n", statLines);
        if (!string.IsNullOrEmpty(baseSummary))
        {
            fullStatsBody += $"\n\n<size=11.5><color=#64748B>{(isVi ? "Chỉ số gốc (Lv.1):" : "Base (Lv.1):")}</color> {EquipmentSocketManager.FormatColoredStatSummary(baseSummary)}</size>";
        }

        if (statsBodyText != null)
        {
            statsBodyText.text = fullStatsBody;
            // Update content height based on line count so ScrollRect functions smoothly
            float estHeight = Mathf.Max(200f, (statLines.Count + 3) * 22f);
            if (statsContentRt != null)
            {
                statsContentRt.sizeDelta = new Vector2(statsContentRt.sizeDelta.x, estHeight);
                statsBodyText.rectTransform.sizeDelta = new Vector2(statsBodyText.rectTransform.sizeDelta.x, estHeight);
            }
        }

        if (statsScrollRect != null)
        {
            statsScrollRect.verticalNormalizedPosition = 1f; // Reset scroll to top
        }

        // Lore / Description
        string desc = itemDef != null ? (isVi ? itemDef.descVi : itemDef.descEn) : "";
        if (itemDescText != null)
        {
            itemDescText.text = !string.IsNullOrEmpty(desc) ? $"\"{desc}\"" : "";
        }

        // Action Buttons
        if (unsocketBtnText != null && unsocketBtn != null)
        {
            var uImg = unsocketBtn.GetComponent<Image>();
            if (slotIndex >= 0)
            {
                unsocketBtnText.text = isVi ? "THÁO TRANG BỊ" : "UNSOCKET";
                if (uImg != null) uImg.color = new Color(0.70f, 0.18f, 0.22f, 1f);
            }
            else
            {
                unsocketBtnText.text = isVi ? "TRANG BỊ NGAY" : "EQUIP NOW";
                if (uImg != null) uImg.color = new Color(0.12f, 0.65f, 0.35f, 1f);
            }
        }
        if (closeBtnText != null)
        {
            closeBtnText.text = isVi ? "ĐÓNG" : "CLOSE";
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (popAnimCor != null) StopCoroutine(popAnimCor);
        popAnimCor = StartCoroutine(AnimateOpen());
    }

    private IEnumerator AnimateOpen()
    {
        canvasGroup.alpha = 0f;
        if (dialogBoxRt != null) dialogBoxRt.localScale = new Vector3(0.90f, 0.90f, 1f);

        float dur = 0.12f;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float tBack = 1f + 1.2f * Mathf.Pow(t - 1f, 3f) + 0.4f * Mathf.Pow(t - 1f, 2f);

            canvasGroup.alpha = t;
            if (dialogBoxRt != null) dialogBoxRt.localScale = new Vector3(tBack, tBack, 1f);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (dialogBoxRt != null) dialogBoxRt.localScale = Vector3.one;
    }

    private void OnUnsocketClicked()
    {
        if (currentSlotIdx >= 0)
        {
            EquipmentSocketManager.UnsocketHeroSlot(currentSlotIdx, currentHeroId);
            SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
            onUnsocketCallback?.Invoke();
        }
        else
        {
            // Equip from Bag into selected empty socket or first available socket
            int targetSlot = -1;
            if (InGameInventoryUI.Instance != null)
            {
                targetSlot = InGameInventoryUI.Instance.GetSelectedEmptySocketIndex();
            }

            if (EquipmentSocketManager.SocketGemToHeroSlot(targetSlot, currentGemId, out _, out _, currentHeroId))
            {
                SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                onUnsocketCallback?.Invoke();
            }
        }
        Hide();
    }

    public static void Hide()
    {
        if (_instance != null && _instance.gameObject != null)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            _instance.gameObject.SetActive(false);
        }
    }
}
