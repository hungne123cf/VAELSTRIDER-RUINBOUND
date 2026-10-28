using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Giao diện Bàn Thờ Nâng Cấp Cổ Đại (Ancient Upgrade Altar).
/// Tích hợp bộ chọn số lượng (x1, x10, x50, MAX), nút "Nâng Tất Cả (MAX)" 
/// và tính năng Nhấn Giữ Chuột / Ngón tay để tự động nâng cấp siêu tốc liên tục!
/// </summary>
public class ShrineUpgradeUI : MonoBehaviour
{
    private static ShrineUpgradeUI instance;

    [Header("Upgrade UI References")]
    private TextMeshProUGUI headerTitleText;
    private TextMeshProUGUI shardsCounterText;
    private GameObject[] cardObjects = new GameObject[4];
    private TextMeshProUGUI[] titleTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] levelTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] descTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] bonusTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] costTexts = new TextMeshProUGUI[4];
    private Button[] upgradeButtons = new Button[4];

    // Multiplier Controls (x1, x10, x50, MAX)
    private static int currentMultiplier = 1; // 1, 10, 50, -1 (MAX)
    private Button[] multButtons = new Button[4];
    private Image[] multButtonBgs = new Image[4];
    private TextMeshProUGUI[] multButtonTexts = new TextMeshProUGUI[4];
    private static readonly int[] MultiplierValues = new int[] { 1, 10, 50, -1 };

    // Upgrade All Button
    private Button upgradeAllBtn;
    private TextMeshProUGUI upgradeAllBtnText;

    private CanvasGroup canvasGroup;
    private bool isOpen = false;

    public static ShrineUpgradeUI Instance => instance;

    void Awake()
    {
        instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        LocalizationManager.OnLanguageChanged += RefreshUI;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshUI;
    }

    public void Open()
    {
        isOpen = true;
        GameAutoBootstrap.SetGameplayHUDVisible(false);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        RefreshUI();
        SoundManager.EnsureInstance()?.PlayMenuHover();
    }

    public void Close()
    {
        isOpen = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
        GameAutoBootstrap.SetGameplayHUDVisible(true);
        SoundManager.EnsureInstance()?.PlayMenuHover();
    }

    public bool IsOpen() => isOpen;

    public void SetMultiplier(int multiplier)
    {
        currentMultiplier = multiplier;
        SoundManager.EnsureInstance()?.PlayMenuHover();
        RefreshUI();
    }

    public void RefreshUI()
    {
        var mgr = PlayerUpgradeManager.EnsureInstance();
        if (mgr == null) return;

        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;

        if (headerTitleText != null)
        {
            headerTitleText.text = isVi ? "BÀN THỜ NÂNG CẤP CỔ ĐẠI" : "ANCIENT UPGRADE ALTAR";
        }

        if (shardsCounterText != null)
        {
            string curShardsStr = MainMenuManager.FormatCompactNumber(mgr.CurrentShards);
            shardsCounterText.text = isVi
                ? $"<color=#64748B>MẢNH CỔ TỰ:</color> <color=#FBBF24><b>{curShardsStr}</b></color>"
                : $"<color=#64748B>AETHEL SHARDS:</color> <color=#FBBF24><b>{curShardsStr}</b></color>";
        }

        // Cập nhật giao diện 4 nút Multiplier
        for (int m = 0; m < MultiplierValues.Length; m++)
        {
            int val = MultiplierValues[m];
            bool isSelected = (currentMultiplier == val);
            if (multButtonBgs[m] != null)
            {
                multButtonBgs[m].color = isSelected 
                    ? new Color(0.15f, 0.45f, 0.75f, 1f) 
                    : new Color(0.10f, 0.14f, 0.22f, 0.9f);
                var outline = multButtonBgs[m].GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = isSelected ? new Color(0.35f, 0.85f, 1f, 0.9f) : new Color(0.2f, 0.35f, 0.5f, 0.4f);
                }
            }
            if (multButtonTexts[m] != null)
            {
                multButtonTexts[m].color = isSelected ? new Color(1f, 0.9f, 0.4f) : new Color(0.75f, 0.85f, 0.95f);
                multButtonTexts[m].fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        // Cập nhật nút Upgrade All
        if (upgradeAllBtnText != null)
        {
            upgradeAllBtnText.text = isVi ? "⚡ NÂNG TẤT CẢ (MAX)" : "⚡ UPGRADE ALL (MAX)";
        }
        if (upgradeAllBtn != null)
        {
            upgradeAllBtn.interactable = (mgr.CurrentShards > 0);
        }

        // Cập nhật từng dòng nâng cấp
        for (int i = 0; i < 4; i++)
        {
            var type = (PlayerUpgradeManager.UpgradeType)i;
            int lvl = mgr.GetLevel(type);
            bool isMax = mgr.IsMaxLevel(type);

            mgr.GetCostAndLevelsForMultiplier(type, currentMultiplier, out int buyCount, out long totalCost);

            if (titleTexts[i] != null) titleTexts[i].text = mgr.GetUpgradeName(type);
            if (descTexts[i] != null) descTexts[i].text = mgr.GetUpgradeDescription(type);

            if (levelTexts[i] != null)
            {
                if (isMax)
                {
                    levelTexts[i].text = isVi 
                        ? $"CẤP HIỆN TẠI: <color=#FFD700>Lv.{lvl} (MAX)</color>" 
                        : $"CURRENT LEVEL: <color=#FFD700>Lv.{lvl} (MAX)</color>";
                }
                else
                {
                    levelTexts[i].text = isVi 
                        ? $"CẤP HIỆN TẠI: <color=#66CCFF>Lv.{lvl}</color>" 
                        : $"CURRENT LEVEL: <color=#66CCFF>Lv.{lvl}</color>";
                }
            }

            if (bonusTexts[i] != null)
            {
                if (isMax)
                {
                    bonusTexts[i].text = isVi ? "Cấp kế tiếp: <color=#FFD700>MAX</color>" : "Next Level: <color=#FFD700>MAX</color>";
                }
                else
                {
                    string bonusStr = mgr.GetNextBonusDescription(type, buyCount);
                    string levelPrefix = buyCount > 1 ? $"+{buyCount} Cấp: " : (isVi ? "Cấp kế tiếp: " : "Next Level: ");
                    if (!isVi && buyCount > 1) levelPrefix = $"+{buyCount} Lvs: ";

                    bonusTexts[i].text = $"{levelPrefix}<color=#66FF99>{bonusStr}</color>";
                }
            }

            if (costTexts[i] != null)
            {
                if (isMax)
                {
                    costTexts[i].text = "<size=15>MAX</size>";
                }
                else
                {
                    string costStr = MainMenuManager.FormatCompactNumber(totalCost);
                    string countStr = buyCount > 1 ? $" (+{buyCount})" : "";

                    costTexts[i].text = isVi 
                        ? $"NÂNG CẤP{countStr}\n<size=13>{costStr} Mảnh</size>" 
                        : $"UPGRADE{countStr}\n<size=13>{costStr} Shards</size>";
                }
                costTexts[i].enableAutoSizing = true;
                costTexts[i].fontSizeMin = 11f;
                costTexts[i].fontSizeMax = 15f;
            }

            if (upgradeButtons[i] != null)
            {
                if (isMax)
                {
                    upgradeButtons[i].interactable = false;
                    var img = upgradeButtons[i].GetComponent<Image>();
                    if (img != null) img.color = new Color(0.22f, 0.25f, 0.28f, 0.65f);
                }
                else
                {
                    bool canAfford = (mgr.CurrentShards >= totalCost && totalCost > 0);
                    upgradeButtons[i].interactable = canAfford;
                    var img = upgradeButtons[i].GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = canAfford ? new Color(0.18f, 0.55f, 0.38f, 1f) : new Color(0.35f, 0.18f, 0.20f, 0.75f);
                    }
                }
            }
        }
    }

    private void DoUpgrade(PlayerUpgradeManager.UpgradeType type)
    {
        var mgr = PlayerUpgradeManager.EnsureInstance();
        if (mgr == null) return;

        mgr.GetCostAndLevelsForMultiplier(type, currentMultiplier, out int buyCount, out _);
        if (mgr.TryPurchaseUpgradeMulti(type, buyCount))
        {
            SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
            RefreshUI();
        }
    }

    private void OnClickUpgradeAll()
    {
        var mgr = PlayerUpgradeManager.EnsureInstance();
        if (mgr == null) return;

        int bought = mgr.UpgradeAllMax();
        if (bought > 0)
        {
            SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
            RefreshUI();

            bool isVi = LocalizationManager.IsVietnamese;
            string msg = isVi ? $"Đã nâng cấp thành công {bought} Cấp độ!" : $"Successfully upgraded {bought} Levels!";
            Vector3 pos = upgradeAllBtn != null ? upgradeAllBtn.transform.position : transform.position;
            DamagePopup.CreateText(pos, msg, new Color(0.2f, 1f, 0.45f));
        }
    }

    public static ShrineUpgradeUI EnsureUIExists()
    {
        if (instance != null) return instance;

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("ShrineUpgradePanel");
        if (existing != null)
        {
            instance = existing.GetComponent<ShrineUpgradeUI>();
            if (instance != null) return instance;
        }

        // 1. Full-screen backdrop
        GameObject panelObj = new GameObject("ShrineUpgradePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panelObj.layer = 5;
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = new Color(0.02f, 0.03f, 0.06f, 0.88f);
        panelBg.raycastTarget = true;

        Canvas pCanvas = panelObj.AddComponent<Canvas>();
        pCanvas.overrideSorting = true;
        pCanvas.sortingOrder = 28000;
        panelObj.AddComponent<GraphicRaycaster>();

        instance = panelObj.AddComponent<ShrineUpgradeUI>();
        instance.canvasGroup = panelObj.GetComponent<CanvasGroup>();

        // 2. Center Dialog Box (Width 980, Height 590)
        GameObject boxObj = new GameObject("DialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.layer = 5;
        boxObj.transform.SetParent(panelObj.transform, false);
        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(980, 590);

        Image boxBg = boxObj.GetComponent<Image>();
        boxBg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        boxBg.type = Image.Type.Sliced;
        boxBg.color = Color.white;
        WhiteIconLoader.ApplyGothicFrame(boxObj, Color.white, 0.40f);

        // Header Title
        GameObject titleObj = new GameObject("HeaderTitle", typeof(RectTransform));
        titleObj.layer = 5;
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 1f);
        tRt.anchorMax = new Vector2(0.5f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0, -18);
        tRt.sizeDelta = new Vector2(760, 32);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = LocalizationManager.T("BÀN THỜ NÂNG CẤP CỔ ĐẠI", "ANCIENT UPGRADE ALTAR");
        titleTmp.fontSize = 25;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.98f, 0.88f, 0.55f, 1f);
        instance.headerTitleText = titleTmp;
        SoulslikeHUDStyler.StyleHeader(instance.headerTitleText);
        SoulslikeHUDStyler.AddFiligreeDivider(boxObj.transform, new Vector2(0f, -46f), new Vector2(500f, 12f));

        // Close Button [X]
        GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtnObj.layer = 5;
        closeBtnObj.transform.SetParent(boxObj.transform, false);
        RectTransform cRt = closeBtnObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-16, -14);
        cRt.sizeDelta = new Vector2(36, 36);
        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(() => instance.Close());
        SoulslikeHUDStyler.StyleCloseButton(closeBtn);

        // 3. Toolbar (Shards Counter on Left, Multipliers in Middle, Upgrade All on Right)
        float toolbarY = -54f;

        // Shards Counter (Left)
        GameObject shardObj = new GameObject("ShardCounter", typeof(RectTransform));
        shardObj.layer = 5;
        shardObj.transform.SetParent(boxObj.transform, false);
        RectTransform sRt = shardObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 1f);
        sRt.anchorMax = new Vector2(0f, 1f);
        sRt.pivot = new Vector2(0f, 0.5f);
        sRt.anchoredPosition = new Vector2(28f, toolbarY);
        sRt.sizeDelta = new Vector2(300, 30);
        instance.shardsCounterText = shardObj.AddComponent<TextMeshProUGUI>();
        instance.shardsCounterText.fontSize = 16;
        instance.shardsCounterText.fontStyle = FontStyles.Bold;
        instance.shardsCounterText.alignment = TextAlignmentOptions.MidlineLeft;
        instance.shardsCounterText.color = new Color(1f, 0.88f, 0.35f, 1f);

        // Multiplier Group (Middle) [x1] [x10] [x50] [MAX]
        GameObject multGroup = new GameObject("MultiplierGroup", typeof(RectTransform));
        multGroup.layer = 5;
        multGroup.transform.SetParent(boxObj.transform, false);
        RectTransform mgRt = multGroup.GetComponent<RectTransform>();
        mgRt.anchorMin = new Vector2(0.5f, 1f);
        mgRt.anchorMax = new Vector2(0.5f, 1f);
        mgRt.pivot = new Vector2(0.5f, 0.5f);
        mgRt.anchoredPosition = new Vector2(-20f, toolbarY);
        mgRt.sizeDelta = new Vector2(250, 32);

        string[] multLabels = new string[] { "x1", "x10", "x50", "MAX" };
        float btnW = 56f;
        float btnGap = 6f;
        float startX = -( (multLabels.Length * btnW + (multLabels.Length - 1) * btnGap) / 2f ) + (btnW / 2f);

        for (int m = 0; m < MultiplierValues.Length; m++)
        {
            int mIdx = m;
            int mVal = MultiplierValues[mIdx];

            GameObject mBtnObj = new GameObject("Btn_Mult_" + multLabels[mIdx], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            mBtnObj.layer = 5;
            mBtnObj.transform.SetParent(multGroup.transform, false);
            RectTransform mbRt = mBtnObj.GetComponent<RectTransform>();
            mbRt.anchorMin = new Vector2(0.5f, 0.5f);
            mbRt.anchorMax = new Vector2(0.5f, 0.5f);
            mbRt.pivot = new Vector2(0.5f, 0.5f);
            mbRt.anchoredPosition = new Vector2(startX + mIdx * (btnW + btnGap), 0);
            mbRt.sizeDelta = new Vector2(btnW, 30);

            Image mbImg = mBtnObj.GetComponent<Image>();
            mbImg.color = new Color(0.10f, 0.14f, 0.22f, 0.9f);
            instance.multButtonBgs[mIdx] = mbImg;

            var mbOutline = mBtnObj.AddComponent<Outline>();
            mbOutline.effectColor = new Color(0.25f, 0.45f, 0.65f, 0.5f);
            mbOutline.effectDistance = new Vector2(1f, -1f);

            Button mBtn = mBtnObj.GetComponent<Button>();
            mBtn.targetGraphic = mbImg;
            mBtn.onClick.AddListener(() => instance.SetMultiplier(mVal));
            instance.multButtons[mIdx] = mBtn;

            GameObject mTxtObj = new GameObject("Text", typeof(RectTransform));
            mTxtObj.layer = 5;
            mTxtObj.transform.SetParent(mBtnObj.transform, false);
            RectTransform mtRt = mTxtObj.GetComponent<RectTransform>();
            mtRt.anchorMin = Vector2.zero;
            mtRt.anchorMax = Vector2.one;
            mtRt.sizeDelta = Vector2.zero;
            var mtTmp = mTxtObj.AddComponent<TextMeshProUGUI>();
            mtTmp.text = multLabels[mIdx];
            mtTmp.fontSize = 14f;
            mtTmp.fontStyle = FontStyles.Bold;
            mtTmp.alignment = TextAlignmentOptions.Center;
            mtTmp.color = Color.white;
            instance.multButtonTexts[mIdx] = mtTmp;
        }

        // Upgrade All Button (Right)
        GameObject allBtnObj = new GameObject("Btn_UpgradeAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        allBtnObj.layer = 5;
        allBtnObj.transform.SetParent(boxObj.transform, false);
        RectTransform abRt = allBtnObj.GetComponent<RectTransform>();
        abRt.anchorMin = new Vector2(1f, 1f);
        abRt.anchorMax = new Vector2(1f, 1f);
        abRt.pivot = new Vector2(1f, 0.5f);
        abRt.anchoredPosition = new Vector2(-28f, toolbarY);
        abRt.sizeDelta = new Vector2(230, 34);

        Image abImg = allBtnObj.GetComponent<Image>();
        abImg.color = new Color(0.85f, 0.50f, 0.05f, 1f); // Vàng cam nổi bật

        var abOutline = allBtnObj.AddComponent<Outline>();
        abOutline.effectColor = new Color(1f, 0.9f, 0.4f, 0.9f);
        abOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button abBtn = allBtnObj.GetComponent<Button>();
        abBtn.targetGraphic = abImg;
        abBtn.onClick.AddListener(() => instance.OnClickUpgradeAll());
        instance.upgradeAllBtn = abBtn;

        GameObject abTxtObj = new GameObject("Text", typeof(RectTransform));
        abTxtObj.layer = 5;
        abTxtObj.transform.SetParent(allBtnObj.transform, false);
        RectTransform abtRt = abTxtObj.GetComponent<RectTransform>();
        abtRt.anchorMin = Vector2.zero;
        abtRt.anchorMax = Vector2.one;
        abtRt.sizeDelta = Vector2.zero;
        var abtTmp = abTxtObj.AddComponent<TextMeshProUGUI>();
        abtTmp.text = LocalizationManager.T("⚡ NÂNG TẤT CẢ (MAX)", "⚡ UPGRADE ALL (MAX)");
        abtTmp.fontSize = 14f;
        abtTmp.fontStyle = FontStyles.Bold;
        abtTmp.alignment = TextAlignmentOptions.Center;
        abtTmp.color = Color.white;
        instance.upgradeAllBtnText = abtTmp;

        // Container 4 Thẻ Nâng Cấp (Centrally spaced)
        float startY = -98f;
        float spacingY = 114f;

        for (int i = 0; i < 4; i++)
        {
            var type = (PlayerUpgradeManager.UpgradeType)i;

            GameObject rowObj = new GameObject("UpgradeCard_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rowObj.layer = 5;
            rowObj.transform.SetParent(boxObj.transform, false);
            RectTransform rowRt = rowObj.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 1f);
            rowRt.anchorMax = new Vector2(0.5f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.anchoredPosition = new Vector2(0, startY - (i * spacingY));
            rowRt.sizeDelta = new Vector2(924, 98);

            Image rowBg = rowObj.GetComponent<Image>();
            rowBg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            instance.cardObjects[i] = rowObj;

            var rowOutline = rowObj.AddComponent<Outline>();
            rowOutline.effectColor = new Color(0.25f, 0.5f, 0.8f, 0.5f);
            rowOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Title
            GameObject tObj = new GameObject("Title", typeof(RectTransform));
            tObj.layer = 5;
            tObj.transform.SetParent(rowObj.transform, false);
            RectTransform rowTRt = tObj.GetComponent<RectTransform>();
            rowTRt.anchorMin = new Vector2(0f, 0.5f);
            rowTRt.anchorMax = new Vector2(0f, 0.5f);
            rowTRt.pivot = new Vector2(0f, 0.5f);
            rowTRt.anchoredPosition = new Vector2(25, 22);
            rowTRt.sizeDelta = new Vector2(360, 26);
            instance.titleTexts[i] = tObj.AddComponent<TextMeshProUGUI>();
            instance.titleTexts[i].fontSize = 17;
            instance.titleTexts[i].fontStyle = FontStyles.Bold;
            instance.titleTexts[i].color = new Color(0.92f, 0.96f, 1f);

            // Current Stats
            GameObject dObj = new GameObject("Desc", typeof(RectTransform));
            dObj.layer = 5;
            dObj.transform.SetParent(rowObj.transform, false);
            RectTransform dRt = dObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0.5f);
            dRt.anchorMax = new Vector2(0f, 0.5f);
            dRt.pivot = new Vector2(0f, 0.5f);
            dRt.anchoredPosition = new Vector2(25, -18);
            dRt.sizeDelta = new Vector2(360, 24);
            instance.descTexts[i] = dObj.AddComponent<TextMeshProUGUI>();
            instance.descTexts[i].fontSize = 14.5f;
            instance.descTexts[i].color = new Color(0.78f, 0.84f, 0.94f);

            // Level Text (Cột giữa)
            GameObject lObj = new GameObject("Level", typeof(RectTransform));
            lObj.layer = 5;
            lObj.transform.SetParent(rowObj.transform, false);
            RectTransform lRt = lObj.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0.5f);
            lRt.anchorMax = new Vector2(0f, 0.5f);
            lRt.pivot = new Vector2(0f, 0.5f);
            lRt.anchoredPosition = new Vector2(405, 22);
            lRt.sizeDelta = new Vector2(270, 26);
            instance.levelTexts[i] = lObj.AddComponent<TextMeshProUGUI>();
            instance.levelTexts[i].fontSize = 16.5f;
            instance.levelTexts[i].fontStyle = FontStyles.Bold;

            // Next Bonus (Cột giữa bên dưới)
            GameObject bObj = new GameObject("Bonus", typeof(RectTransform));
            bObj.layer = 5;
            bObj.transform.SetParent(rowObj.transform, false);
            RectTransform bRt = bObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0.5f);
            bRt.anchorMax = new Vector2(0f, 0.5f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(405, -18);
            bRt.sizeDelta = new Vector2(270, 24);
            instance.bonusTexts[i] = bObj.AddComponent<TextMeshProUGUI>();
            instance.bonusTexts[i].fontSize = 14.5f;

            // Upgrade Button (Cột phải)
            GameObject uBtnObj = new GameObject("Btn_Upgrade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            uBtnObj.layer = 5;
            uBtnObj.transform.SetParent(rowObj.transform, false);
            RectTransform uBtnRt = uBtnObj.GetComponent<RectTransform>();
            uBtnRt.anchorMin = new Vector2(1f, 0.5f);
            uBtnRt.anchorMax = new Vector2(1f, 0.5f);
            uBtnRt.pivot = new Vector2(1f, 0.5f);
            uBtnRt.anchoredPosition = new Vector2(-22, 0);
            uBtnRt.sizeDelta = new Vector2(200, 62);

            Image uBtnBg = uBtnObj.GetComponent<Image>();
            uBtnBg.color = new Color(0.18f, 0.48f, 0.36f, 1f);

            Button uBtn = uBtnObj.GetComponent<Button>();
            uBtn.targetGraphic = uBtnBg;
            uBtn.onClick.AddListener(() => instance.DoUpgrade(type));
            instance.upgradeButtons[i] = uBtn;

            // Tính năng Nhấn Giữ để tự động nâng cấp nhanh
            var holdHandler = uBtnObj.AddComponent<UIHoldUpgradeButton>();
            holdHandler.onTick = () => instance.DoUpgrade(type);

            GameObject cLabelObj = new GameObject("CostText", typeof(RectTransform));
            cLabelObj.layer = 5;
            cLabelObj.transform.SetParent(uBtnObj.transform, false);
            RectTransform clRt = cLabelObj.GetComponent<RectTransform>();
            clRt.anchorMin = Vector2.zero;
            clRt.anchorMax = Vector2.one;
            clRt.sizeDelta = Vector2.zero;

            var cTmpTmp = cLabelObj.AddComponent<TextMeshProUGUI>();
            cTmpTmp.fontSize = 15.5f;
            cTmpTmp.fontStyle = FontStyles.Bold;
            cTmpTmp.alignment = TextAlignmentOptions.Center;
            cTmpTmp.color = Color.white;
            instance.costTexts[i] = cTmpTmp;
        }

        instance.Close();
        Debug.Log("<color=cyan>[SHRINE UI]</color> Đã tạo giao diện Bàn Thờ Nâng Cấp Cổ Đại tích hợp Multiplier & Nâng Nhanh!");
        return instance;
    }
}

/// <summary>
/// Component xử lý Nhấn Giữ (Hold) trên nút bấm để tự động lặp lại hành động với tốc độ tăng dần.
/// </summary>
public class UIHoldUpgradeButton : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    public System.Action onTick;
    private bool isDown = false;
    private float holdDuration = 0f;
    private float tickTimer = 0f;
    private float currentInterval = 0.12f;

    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left) return;
        isDown = true;
        holdDuration = 0f;
        tickTimer = 0f;
        currentInterval = 0.12f;
    }

    public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
    {
        isDown = false;
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
    {
        isDown = false;
    }

    void Update()
    {
        if (!isDown || onTick == null) return;

        holdDuration += Time.unscaledDeltaTime;
        if (holdDuration >= 0.30f) // Sau 0.3s nhấn giữ sẽ bắt đầu kích hoạt lặp lại
        {
            tickTimer += Time.unscaledDeltaTime;
            if (tickTimer >= currentInterval)
            {
                tickTimer = 0f;
                onTick.Invoke();
                // Tăng tốc dần theo thời gian giữ
                currentInterval = Mathf.Max(0.04f, currentInterval * 0.92f);
            }
        }
    }
}
