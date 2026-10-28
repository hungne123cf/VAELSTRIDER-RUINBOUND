using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Giao diện Popup Mở Rương Trong Trận (In-Game Treasure Chest Loot Modal)
/// Thiết kế chuẩn Dark Fantasy, tối ưu hóa cho màn hình cảm ứng Mobile (CH Play) & PC:
/// - Chống tràn viền (ScrollRect có Mask)
/// - Đầy đủ CanvasRenderer đảm bảo hiển thị 100% rõ nét trên mọi màn hình
/// - Cho phép nhặt từng vật phẩm riêng lẻ hoặc Nhận Tất Cả (Claim All)
/// </summary>
public class ChestLootPopupUI : MonoBehaviour
{
    private static ChestLootPopupUI instance;
    public static ChestLootPopupUI Instance => instance;

    [Header("UI State")]
    private CanvasGroup canvasGroup;
    private TreasureChest currentChest;
    private Transform contentContainer;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private Image chestHeaderIcon;
    private Button claimAllBtn;
    private Button closeBtn;
    private TextMeshProUGUI claimAllBtnText;
    private TextMeshProUGUI closeBtnText;

    private List<GameObject> activeItemRows = new List<GameObject>();
    private bool isOpen = false;

    public bool IsOpen => isOpen;

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

    private void OnLanguageChanged()
    {
        if (titleText != null) titleText.text = LocalizationManager.T("RƯƠNG BÁU TÀN TÍCH", "ANCIENT RUIN CHEST");
        if (claimAllBtnText != null) claimAllBtnText.text = LocalizationManager.T("LẤY TẤT CẢ", "CLAIM ALL");
        if (closeBtnText != null) closeBtnText.text = LocalizationManager.T("ĐÓNG", "CLOSE");
        if (isOpen) RefreshLootItems();
    }

    public static ChestLootPopupUI EnsureUIExists()
    {
        if (instance != null && instance.gameObject != null) return instance;

        Canvas targetCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (targetCanvas == null) targetCanvas = Object.FindAnyObjectByType<Canvas>();
        if (targetCanvas == null)
        {
            GameObject cObj = new GameObject("InGameUICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            targetCanvas = cObj.GetComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 950;

            var scaler = cObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform existing = targetCanvas.transform.Find("ChestLootPopupModal");
        if (existing != null)
        {
            instance = existing.GetComponent<ChestLootPopupUI>();
            if (instance != null)
            {
                if (instance.contentContainer == null) instance.BuildUI(existing);
                return instance;
            }
        }

        // Tạo Root Modal
        GameObject modalObj = new GameObject("ChestLootPopupModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(Image));
        modalObj.layer = 5;
        modalObj.transform.SetParent(targetCanvas.transform, false);

        RectTransform rt = modalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bgDim = modalObj.GetComponent<Image>();
        bgDim.color = new Color(0.03f, 0.03f, 0.06f, 0.88f);

        instance = modalObj.AddComponent<ChestLootPopupUI>();
        instance.BuildUI(modalObj.transform);

        modalObj.SetActive(false);
        return instance;
    }

    private void BuildUI(Transform root)
    {
        // 1. Dialog Box
        GameObject boxObj = new GameObject("LootDialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.layer = 5;
        boxObj.transform.SetParent(root, false);

        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.sizeDelta = new Vector2(660f, 490f);
        boxRt.anchoredPosition = Vector2.zero;

        Image boxImg = boxObj.GetComponent<Image>();
        boxImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        boxImg.type = Image.Type.Sliced;
        boxImg.color = Color.white;
        WhiteIconLoader.ApplyGothicFrame(boxObj, Color.white, 0.40f);

        // 2. Header Icon
        GameObject iconObj = new GameObject("HeaderChestIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.layer = 5;
        iconObj.transform.SetParent(boxObj.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchoredPosition = new Vector2(0f, 195f);
        iconRt.sizeDelta = new Vector2(56f, 56f);

        chestHeaderIcon = iconObj.GetComponent<Image>();
        chestHeaderIcon.preserveAspect = true;
        Sprite spr = Resources.Load<Sprite>("Items/chest_open");
#if UNITY_EDITOR
        if (spr == null) spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/chest_open.png");
#endif
        if (spr != null) chestHeaderIcon.sprite = spr;

        // 3. Title Text
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.layer = 5;
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 150f);
        titleRt.sizeDelta = new Vector2(580f, 30f);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = LocalizationManager.T("RƯƠNG BÁU TÀN TÍCH", "ANCIENT RUIN CHEST");
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.98f, 0.88f, 0.55f);
        titleText.alignment = TextAlignmentOptions.Center;

        // 4. Subtitle Text
        GameObject subObj = new GameObject("SubText", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.layer = 5;
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0f, 122f);
        subRt.sizeDelta = new Vector2(580f, 22f);

        subtitleText = subObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = LocalizationManager.T("Chọn vật phẩm muốn nhặt hoặc nhấn Lấy Tất Cả:", "Select items to take or tap Claim All:");
        subtitleText.fontSize = 14.5f;
        subtitleText.color = new Color(0.80f, 0.85f, 0.95f, 0.85f);
        SoulslikeHUDStyler.StyleHeader(titleText, subtitleText);
        SoulslikeHUDStyler.AddFiligreeDivider(boxObj.transform, new Vector2(0f, 106f), new Vector2(460f, 12f));
        subtitleText.alignment = TextAlignmentOptions.Center;

        // 5. Scroll View (Anti-Overflow, Mobile Touch Swipe)
        GameObject scrollObj = new GameObject("LootScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(ScrollRect), typeof(RectMask2D));
        scrollObj.layer = 5;
        scrollObj.transform.SetParent(boxObj.transform, false);
        RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchoredPosition = new Vector2(0f, -5f);
        scrollRt.sizeDelta = new Vector2(600f, 240f);

        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 25f;

        // Content
        GameObject contentObj = new GameObject("LootContent", typeof(RectTransform), typeof(CanvasRenderer), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.layer = 5;
        contentObj.transform.SetParent(scrollObj.transform, false);
        RectTransform cRt = contentObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta = new Vector2(0f, 0f);

        var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = cRt;
        scrollRect.viewport = scrollRt;

        contentContainer = contentObj.transform;

        // 6. Action Buttons Bar
        GameObject btnBarObj = new GameObject("ActionBar", typeof(RectTransform), typeof(CanvasRenderer));
        btnBarObj.layer = 5;
        btnBarObj.transform.SetParent(boxObj.transform, false);
        RectTransform barRt = btnBarObj.GetComponent<RectTransform>();
        barRt.anchoredPosition = new Vector2(0f, -188f);
        barRt.sizeDelta = new Vector2(580f, 50f);

        // Claim All Button (Green)
        claimAllBtn = CreateActionButton(btnBarObj.transform, "Btn_ClaimAll",
            LocalizationManager.T("LẤY TẤT CẢ", "CLAIM ALL"),
            new Vector2(95f, 0f), new Vector2(255f, 48f), new Color(0.18f, 0.65f, 0.38f, 1f));
        claimAllBtnText = claimAllBtn.GetComponentInChildren<TextMeshProUGUI>();
        claimAllBtn.onClick.AddListener(OnClaimAllClicked);

        // Close Button (Dark Slate)
        closeBtn = CreateActionButton(btnBarObj.transform, "Btn_Close",
            LocalizationManager.T("ĐÓNG", "CLOSE"),
            new Vector2(-155f, 0f), new Vector2(170f, 48f), new Color(0.24f, 0.28f, 0.38f, 1f));
        closeBtnText = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
        closeBtn.onClick.AddListener(Close);
    }

    private Button CreateActionButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.layer = 5;
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Button btn = btnObj.GetComponent<Button>();

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
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.96f, 0.88f, 0.65f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        SoulslikeHUDStyler.StyleButton(btn, text);

        return btn;
    }

    public void Open(TreasureChest chest)
    {
        if (chest == null) return;
        currentChest = chest;
        isOpen = true;

        Canvas rootCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) rootCanvas = Object.FindAnyObjectByType<Canvas>();
        if (rootCanvas != null && transform.parent != rootCanvas.transform)
        {
            transform.SetParent(rootCanvas.transform, false);
        }

        if (contentContainer == null)
        {
            BuildUI(transform);
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        Time.timeScale = 0f;
        SoundManager.EnsureInstance()?.PlayChestOpen();

        RefreshLootItems();
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        Time.timeScale = 1f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
        SoundManager.EnsureInstance()?.PlayButtonClick();

        if (currentChest != null)
        {
            currentChest.OnModalClosed();
        }
    }

    private void RefreshLootItems()
    {
        foreach (var r in activeItemRows)
        {
            if (r != null) Destroy(r);
        }
        activeItemRows.Clear();

        if (currentChest == null) return;

        var items = currentChest.GetLootItems();
        if (items == null || items.Count == 0)
        {
            // Rương trống rỗng
            if (subtitleText != null)
            {
                subtitleText.text = LocalizationManager.T("Rương này đã được thu thập hết chiến lợi phẩm!", "All treasures from this chest have been collected!");
            }
            if (claimAllBtn != null) claimAllBtn.gameObject.SetActive(false);
            return;
        }

        if (subtitleText != null)
        {
            subtitleText.text = LocalizationManager.T("Chọn vật phẩm muốn nhặt hoặc nhấn Lấy Tất Cả:", "Select items to take or tap Claim All:");
        }
        if (claimAllBtn != null) claimAllBtn.gameObject.SetActive(true);

        foreach (var item in items)
        {
            GameObject rowObj = CreateItemRow(item);
            activeItemRows.Add(rowObj);
        }

        Canvas.ForceUpdateCanvases();
        if (contentContainer != null)
        {
            var sr = contentContainer.GetComponentInParent<ScrollRect>();
            if (sr != null) sr.verticalNormalizedPosition = 1f;
        }
    }

    private GameObject CreateItemRow(TreasureChest.ChestLootItem item)
    {
        GameObject rowObj = new GameObject("LootRow_" + item.id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        rowObj.layer = 5;
        rowObj.transform.SetParent(contentContainer, false);

        RectTransform rt = rowObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(570f, 74f);

        var le = rowObj.GetComponent<LayoutElement>();
        le.preferredWidth = 570f;
        le.preferredHeight = 74f;
        le.minHeight = 74f;

        Image bg = rowObj.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.13f, 0.95f);

        Color rarityCol = GetRarityColor(item.rarity);
        WhiteIconLoader.ApplyGothicFrame(rowObj, rarityCol, 0.65f);

        // Icon Box
        GameObject ibObj = new GameObject("IconBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ibObj.layer = 5;
        ibObj.transform.SetParent(rowObj.transform, false);
        RectTransform ibRt = ibObj.GetComponent<RectTransform>();
        ibRt.anchorMin = new Vector2(0f, 0.5f);
        ibRt.anchorMax = new Vector2(0f, 0.5f);
        ibRt.pivot = new Vector2(0f, 0.5f);
        ibRt.anchoredPosition = new Vector2(12f, 0f);
        ibRt.sizeDelta = new Vector2(50f, 50f);

        Image ibImg = ibObj.GetComponent<Image>();
        ibImg.color = new Color(0.18f, 0.20f, 0.30f, 1f);

        GameObject icObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        icObj.layer = 5;
        icObj.transform.SetParent(ibObj.transform, false);
        RectTransform icRt = icObj.GetComponent<RectTransform>();
        icRt.anchorMin = Vector2.zero;
        icRt.anchorMax = Vector2.one;
        icRt.sizeDelta = new Vector2(-6f, -6f);

        Image icImg = icObj.GetComponent<Image>();
        Sprite iconSprite = item.icon;
        if (iconSprite == null)
        {
            if (item.id == "potion")
            {
                iconSprite = Resources.Load<Sprite>("Items/health_potion");
#if UNITY_EDITOR
                if (iconSprite == null) iconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/health_potion.png");
#endif
            }
            else if (item.id == "shards")
            {
                iconSprite = Resources.Load<Sprite>("Items/aethel_shard");
#if UNITY_EDITOR
                if (iconSprite == null) iconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/aethel_shard.png");
#endif
            }
            else
            {
                var def = InventoryItemDatabase.GetItem(item.id);
                if (def != null) iconSprite = def.Icon;
            }
        }

        if (iconSprite != null)
        {
            icImg.sprite = iconSprite;
            icImg.color = Color.white;
            icImg.preserveAspect = true;
        }
        else
        {
            icImg.color = new Color(0f, 0f, 0f, 0f);
        }

        // Details (Name + Quantity)
        GameObject infoObj = new GameObject("Info", typeof(RectTransform), typeof(CanvasRenderer), typeof(VerticalLayoutGroup));
        infoObj.layer = 5;
        infoObj.transform.SetParent(rowObj.transform, false);
        RectTransform infoRt = infoObj.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0f, 0.5f);
        infoRt.anchorMax = new Vector2(1f, 0.5f);
        infoRt.pivot = new Vector2(0f, 0.5f);
        infoRt.anchoredPosition = new Vector2(72f, 0f);
        infoRt.sizeDelta = new Vector2(-195f, 50f);

        var vlg = infoObj.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.spacing = 3f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        // Item Name
        GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(LayoutElement));
        nameObj.layer = 5;
        nameObj.transform.SetParent(infoObj.transform, false);
        var nLe = nameObj.GetComponent<LayoutElement>();
        nLe.preferredHeight = 24f;
        nLe.minHeight = 24f;
        var nTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nTmp.text = LocalizationManager.IsVietnamese ? item.nameVi : item.nameEn;
        nTmp.fontSize = 16f;
        nTmp.fontStyle = FontStyles.Bold;
        nTmp.color = Color.white;
        nTmp.alignment = TextAlignmentOptions.Left;
        nTmp.overflowMode = TextOverflowModes.Ellipsis;
        nTmp.enableWordWrapping = false;

        // Rarity + Quantity Tag
        GameObject tagObj = new GameObject("Tag", typeof(RectTransform), typeof(CanvasRenderer), typeof(LayoutElement));
        tagObj.layer = 5;
        tagObj.transform.SetParent(infoObj.transform, false);
        var tLe = tagObj.GetComponent<LayoutElement>();
        tLe.preferredHeight = 20f;
        tLe.minHeight = 20f;
        var tTmp = tagObj.AddComponent<TextMeshProUGUI>();
        string qtyStr = item.quantity > 1 ? $" x{item.quantity}" : "";
        if (item.id == "shards") qtyStr = $" +{item.quantity:N0} {LocalizationManager.T("Linh Hồn", "Shards")}";
        tTmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(rarityCol)}>[ {GetRarityLabel(item.rarity)} ]</color>{qtyStr}";
        tTmp.fontSize = 13.5f;
        tTmp.fontStyle = FontStyles.Bold;
        tTmp.color = new Color(0.85f, 0.88f, 0.95f);
        tTmp.alignment = TextAlignmentOptions.Left;
        tTmp.enableWordWrapping = false;

        // "NHẬN" / "LOOT" Button
        GameObject btnObj = new GameObject("Btn_Take", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.layer = 5;
        btnObj.transform.SetParent(rowObj.transform, false);
        RectTransform bRt = btnObj.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(1f, 0.5f);
        bRt.anchorMax = new Vector2(1f, 0.5f);
        bRt.pivot = new Vector2(1f, 0.5f);
        bRt.anchoredPosition = new Vector2(-10f, 0f);
        bRt.sizeDelta = new Vector2(105f, 42f);

        Image bImg = btnObj.GetComponent<Image>();
        bImg.color = new Color(0.18f, 0.65f, 0.38f, 1f);

        Button btn = btnObj.GetComponent<Button>();

        GameObject bTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        bTxtObj.layer = 5;
        bTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform btRt = bTxtObj.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.offsetMin = Vector2.zero;
        btRt.offsetMax = Vector2.zero;

        var bTmp = bTxtObj.AddComponent<TextMeshProUGUI>();
        bTmp.text = LocalizationManager.T("NHẬN", "LOOT");
        bTmp.fontSize = 15f;
        bTmp.fontStyle = FontStyles.Bold;
        bTmp.color = Color.white;
        bTmp.alignment = TextAlignmentOptions.Center;
        bTmp.raycastTarget = false;

        btn.targetGraphic = bImg;
        btn.onClick.AddListener(() => TakeSingleItem(item, rowObj));

        return rowObj;
    }

    private void TakeSingleItem(TreasureChest.ChestLootItem item, GameObject rowObj)
    {
        if (item == null || currentChest == null) return;

        currentChest.EjectSingleLoot(item);

        if (rowObj != null)
        {
            activeItemRows.Remove(rowObj);
            Destroy(rowObj);
        }

        if (currentChest.GetLootItems().Count == 0)
        {
            StartCoroutine(CloseDelayRoutine());
        }
    }

    private void OnClaimAllClicked()
    {
        if (currentChest == null) return;

        currentChest.EjectAndSpawnAllLoot();
        Close();
    }

    private IEnumerator CloseDelayRoutine()
    {
        if (subtitleText != null)
        {
            subtitleText.text = LocalizationManager.T("ĐÃ THU THẬP TẤT CẢ VẬT PHẨM!", "ALL TREASURES COLLECTED!");
            subtitleText.color = new Color(0.2f, 1f, 0.5f);
        }
        if (claimAllBtn != null) claimAllBtn.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(0.45f);
        Close();
    }

    private Color GetRarityColor(string rarity)
    {
        switch (rarity)
        {
            case "Currency": return new Color(0f, 0.85f, 1f);
            case "Legendary": return new Color(1f, 0.75f, 0.15f);
            case "Epic": return new Color(0.75f, 0.35f, 0.95f);
            case "Rare": return new Color(0.25f, 0.65f, 1f);
            default: return new Color(0.75f, 0.8f, 0.85f);
        }
    }

    private string GetRarityLabel(string rarity)
    {
        switch (rarity)
        {
            case "Currency": return LocalizationManager.T("TIỀN TỆ", "CURRENCY");
            case "Legendary": return LocalizationManager.T("THẦN THOẠI", "MYTHIC");
            case "Epic": return LocalizationManager.T("SỬ THI", "EPIC");
            case "Rare": return LocalizationManager.T("HIẾM", "RARE");
            default: return LocalizationManager.T("THƯỜNG", "COMMON");
        }
    }
}
