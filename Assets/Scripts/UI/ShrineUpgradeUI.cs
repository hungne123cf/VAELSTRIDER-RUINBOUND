using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Giao diện Bàn Thờ Nâng Cấp Cổ Đại & Bảng Tra Cứu Chiêu Thức (Attack, Dash, Ulti).
/// Tích hợp 2 Tab chuyển đổi mượt mà, căn chỉnh lề chuẩn xác không bị tràn viền.
/// </summary>
public class ShrineUpgradeUI : MonoBehaviour
{
    private static ShrineUpgradeUI instance;

    [Header("UI Tabs")]
    private GameObject tabUpgradesContent;
    private GameObject tabSkillsContent;
    private Button btnTabUpgrades;
    private Button btnTabSkills;
    private Image imgTabUpgrades;
    private Image imgTabSkills;

    [Header("Upgrade UI References")]
    private TextMeshProUGUI shardsCounterText;
    private GameObject[] cardObjects = new GameObject[4];
    private TextMeshProUGUI[] titleTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] levelTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] descTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] bonusTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] costTexts = new TextMeshProUGUI[4];
    private Button[] upgradeButtons = new Button[4];

    private CanvasGroup canvasGroup;
    private bool isOpen = false;

    public static ShrineUpgradeUI Instance => instance;

    void Awake()
    {
        instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Open()
    {
        isOpen = true;
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        SwitchTab(isUpgrades: true);
        RefreshUI();
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
    }

    public bool IsOpen() => isOpen;

    public void SwitchTab(bool isUpgrades)
    {
        if (tabUpgradesContent != null) tabUpgradesContent.SetActive(isUpgrades);
        if (tabSkillsContent != null) tabSkillsContent.SetActive(!isUpgrades);

        Color activeTabColor = new Color(0.18f, 0.42f, 0.65f, 1f);
        Color inactiveTabColor = new Color(0.12f, 0.16f, 0.22f, 0.85f);

        if (imgTabUpgrades != null) imgTabUpgrades.color = isUpgrades ? activeTabColor : inactiveTabColor;
        if (imgTabSkills != null) imgTabSkills.color = !isUpgrades ? activeTabColor : inactiveTabColor;
    }

    public void RefreshUI()
    {
        var mgr = PlayerUpgradeManager.EnsureInstance();
        if (mgr == null) return;

        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;

        if (shardsCounterText != null)
        {
            string label = isVi ? "MẢNH CỔ TỰ (AETHEL SHARDS)" : "AETHEL SHARDS";
            shardsCounterText.text = $"{label}: {mgr.CurrentShards} ✦";
        }

        for (int i = 0; i < 4; i++)
        {
            var type = (PlayerUpgradeManager.UpgradeType)i;
            int lvl = mgr.GetLevel(type);
            int cost = mgr.GetCost(type);

            if (titleTexts[i] != null) titleTexts[i].text = mgr.GetTitle(type);

            if (levelTexts[i] != null)
            {
                if (lvl >= PlayerUpgradeManager.MAX_LEVEL)
                {
                    levelTexts[i].text = isVi ? "CẤP TỐI ĐA (MAX)" : "MAX LEVEL";
                    levelTexts[i].color = new Color(1f, 0.85f, 0.2f);
                }
                else
                {
                    levelTexts[i].text = (isVi ? "CẤP ĐỘ: " : "LEVEL: ") + $"{lvl} / {PlayerUpgradeManager.MAX_LEVEL}";
                    levelTexts[i].color = new Color(0.4f, 0.9f, 1f);
                }
            }

            if (descTexts[i] != null) descTexts[i].text = mgr.GetDescription(type, lvl);
            if (bonusTexts[i] != null)
            {
                bonusTexts[i].text = mgr.GetNextBonusDescription(type);
                bonusTexts[i].color = (lvl >= PlayerUpgradeManager.MAX_LEVEL) ? new Color(0.55f, 0.85f, 0.55f) : new Color(0.45f, 1f, 0.65f);
            }

            if (costTexts[i] != null)
            {
                if (lvl >= PlayerUpgradeManager.MAX_LEVEL)
                {
                    costTexts[i].text = isVi ? "TỐI ĐA" : "MAX";
                }
                else
                {
                    costTexts[i].text = (isVi ? "NÂNG CẤP (" : "UPGRADE (") + cost + " ✦)";
                }
            }

            if (upgradeButtons[i] != null)
            {
                bool canAfford = mgr.CanUpgrade(type);
                upgradeButtons[i].interactable = canAfford;
                var btnImg = upgradeButtons[i].GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = canAfford ? new Color(0.18f, 0.48f, 0.36f, 1f) : new Color(0.24f, 0.24f, 0.26f, 0.7f);
                }
            }
        }
    }

    public static ShrineUpgradeUI EnsureUIExists()
    {
        if (instance != null) return instance;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("ShrineUpgradePanel");
        if (existing != null)
        {
            instance = existing.GetComponent<ShrineUpgradeUI>();
            if (instance != null) return instance;
        }

        // 1. Full-screen backdrop
        GameObject panelObj = new GameObject("ShrineUpgradePanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.02f, 0.03f, 0.06f, 0.88f);

        instance = panelObj.AddComponent<ShrineUpgradeUI>();

        // 2. Center Dialog Box (Width 980, Height 620)
        GameObject boxObj = new GameObject("DialogBox");
        boxObj.transform.SetParent(panelObj.transform, false);
        RectTransform boxRt = boxObj.AddComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(980, 620);

        Image boxBg = boxObj.AddComponent<Image>();
        boxBg.color = new Color(0.08f, 0.11f, 0.17f, 0.98f);

        Outline boxOutline = boxObj.AddComponent<Outline>();
        boxOutline.effectColor = new Color(0.25f, 0.65f, 0.95f, 0.85f);
        boxOutline.effectDistance = new Vector2(2f, -2f);

        // Header Title
        GameObject titleObj = new GameObject("HeaderTitle");
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform tRt = titleObj.AddComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 1f);
        tRt.anchorMax = new Vector2(0.5f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0, -22);
        tRt.sizeDelta = new Vector2(760, 36);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "✦ BÀN THỜ NÂNG CẤP CỔ ĐẠI ✦";
        titleTmp.fontSize = 24;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.35f, 0.95f, 1f, 1f);

        // Shards Counter
        GameObject shardObj = new GameObject("ShardCounter");
        shardObj.transform.SetParent(boxObj.transform, false);
        RectTransform sRt = shardObj.AddComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.5f, 1f);
        sRt.anchorMax = new Vector2(0.5f, 1f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0, -60);
        sRt.sizeDelta = new Vector2(650, 26);
        instance.shardsCounterText = shardObj.AddComponent<TextMeshProUGUI>();
        instance.shardsCounterText.fontSize = 16;
        instance.shardsCounterText.fontStyle = FontStyles.Bold;
        instance.shardsCounterText.alignment = TextAlignmentOptions.Center;
        instance.shardsCounterText.color = new Color(1f, 0.88f, 0.35f, 1f);

        // Close Button [X]
        GameObject closeBtnObj = new GameObject("Btn_Close");
        closeBtnObj.transform.SetParent(boxObj.transform, false);
        RectTransform cRt = closeBtnObj.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-18, -18);
        cRt.sizeDelta = new Vector2(38, 38);
        Image cBg = closeBtnObj.AddComponent<Image>();
        cBg.color = new Color(0.65f, 0.16f, 0.16f, 1f);
        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => instance.Close());

        GameObject cTxtObj = new GameObject("X");
        cTxtObj.transform.SetParent(closeBtnObj.transform, false);
        var cTmp = cTxtObj.AddComponent<TextMeshProUGUI>();
        cTmp.text = "✕";
        cTmp.fontSize = 20;
        cTmp.alignment = TextAlignmentOptions.Center;
        cTmp.color = Color.white;
        RectTransform cTxtRt = cTxtObj.GetComponent<RectTransform>();
        cTxtRt.anchorMin = Vector2.zero;
        cTxtRt.anchorMax = Vector2.one;
        cTxtRt.sizeDelta = Vector2.zero;

        // 3. TOP TAB BUTTONS (NÂNG CẤP vs CHIÊU THỨC)
        GameObject tabGroupObj = new GameObject("TabGroup");
        tabGroupObj.transform.SetParent(boxObj.transform, false);
        RectTransform tabRt = tabGroupObj.AddComponent<RectTransform>();
        tabRt.anchorMin = new Vector2(0.5f, 1f);
        tabRt.anchorMax = new Vector2(0.5f, 1f);
        tabRt.pivot = new Vector2(0.5f, 1f);
        tabRt.anchoredPosition = new Vector2(0, -96);
        tabRt.sizeDelta = new Vector2(920, 38);

        // Tab 1: Nâng cấp
        GameObject tab1Obj = new GameObject("Tab_Upgrades");
        tab1Obj.transform.SetParent(tabGroupObj.transform, false);
        RectTransform t1Rt = tab1Obj.AddComponent<RectTransform>();
        t1Rt.anchorMin = new Vector2(0.5f, 0.5f);
        t1Rt.anchorMax = new Vector2(0.5f, 0.5f);
        t1Rt.pivot = new Vector2(0.5f, 0.5f);
        t1Rt.anchoredPosition = new Vector2(-225, 0);
        t1Rt.sizeDelta = new Vector2(430, 36);
        instance.imgTabUpgrades = tab1Obj.AddComponent<Image>();
        instance.btnTabUpgrades = tab1Obj.AddComponent<Button>();
        instance.btnTabUpgrades.onClick.AddListener(() => instance.SwitchTab(true));

        GameObject t1TxtObj = new GameObject("Text");
        t1TxtObj.transform.SetParent(tab1Obj.transform, false);
        var t1Tmp = t1TxtObj.AddComponent<TextMeshProUGUI>();
        t1Tmp.text = "⚔️ NÂNG CẤP CHỈ SỐ SỨC MẠNH";
        t1Tmp.fontSize = 15;
        t1Tmp.fontStyle = FontStyles.Bold;
        t1Tmp.alignment = TextAlignmentOptions.Center;
        t1Tmp.color = Color.white;
        RectTransform t1TxtRt = t1TxtObj.GetComponent<RectTransform>();
        t1TxtRt.anchorMin = Vector2.zero;
        t1TxtRt.anchorMax = Vector2.one;
        t1TxtRt.sizeDelta = Vector2.zero;

        // Tab 2: Chiêu thức
        GameObject tab2Obj = new GameObject("Tab_Skills");
        tab2Obj.transform.SetParent(tabGroupObj.transform, false);
        RectTransform t2Rt = tab2Obj.AddComponent<RectTransform>();
        t2Rt.anchorMin = new Vector2(0.5f, 0.5f);
        t2Rt.anchorMax = new Vector2(0.5f, 0.5f);
        t2Rt.pivot = new Vector2(0.5f, 0.5f);
        t2Rt.anchoredPosition = new Vector2(225, 0);
        t2Rt.sizeDelta = new Vector2(430, 36);
        instance.imgTabSkills = tab2Obj.AddComponent<Image>();
        instance.btnTabSkills = tab2Obj.AddComponent<Button>();
        instance.btnTabSkills.onClick.AddListener(() => instance.SwitchTab(false));

        GameObject t2TxtObj = new GameObject("Text");
        t2TxtObj.transform.SetParent(tab2Obj.transform, false);
        var t2Tmp = t2TxtObj.AddComponent<TextMeshProUGUI>();
        t2Tmp.text = "📜 BẢNG MÔ TẢ CHIÊU THỨC (ATTACK / DASH / ULTI)";
        t2Tmp.fontSize = 15;
        t2Tmp.fontStyle = FontStyles.Bold;
        t2Tmp.alignment = TextAlignmentOptions.Center;
        t2Tmp.color = Color.white;
        RectTransform t2TxtRt = t2TxtObj.GetComponent<RectTransform>();
        t2TxtRt.anchorMin = Vector2.zero;
        t2TxtRt.anchorMax = Vector2.one;
        t2TxtRt.sizeDelta = Vector2.zero;

        // 4. TAB 1 CONTENT: 4 NÂNG CẤP
        instance.tabUpgradesContent = new GameObject("UpgradesContent");
        instance.tabUpgradesContent.transform.SetParent(boxObj.transform, false);
        RectTransform upgContentRt = instance.tabUpgradesContent.AddComponent<RectTransform>();
        upgContentRt.anchorMin = new Vector2(0.5f, 0f);
        upgContentRt.anchorMax = new Vector2(0.5f, 1f);
        upgContentRt.pivot = new Vector2(0.5f, 0.5f);
        upgContentRt.sizeDelta = new Vector2(920, 0);
        upgContentRt.anchoredPosition = new Vector2(0, -65);

        float startY = -90f;
        float spacingY = 100f;

        for (int i = 0; i < 4; i++)
        {
            int index = i;
            var type = (PlayerUpgradeManager.UpgradeType)i;

            GameObject rowObj = new GameObject("UpgradeCard_" + i);
            rowObj.transform.SetParent(instance.tabUpgradesContent.transform, false);
            RectTransform rowRt = rowObj.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 1f);
            rowRt.anchorMax = new Vector2(0.5f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.anchoredPosition = new Vector2(0, startY - (i * spacingY));
            rowRt.sizeDelta = new Vector2(920, 88);

            Image rowBg = rowObj.AddComponent<Image>();
            rowBg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            instance.cardObjects[i] = rowObj;

            // Title (CĂN LỀ TRÁI CỐ ĐỊNH pivot = 0, 0.5f) -> KHÔNG BAO GIỜ BỊ LỒI RA NGOÀI
            GameObject tObj = new GameObject("Title");
            tObj.transform.SetParent(rowObj.transform, false);
            RectTransform rowTRt = tObj.AddComponent<RectTransform>();
            rowTRt.anchorMin = new Vector2(0f, 0.5f);
            rowTRt.anchorMax = new Vector2(0f, 0.5f);
            rowTRt.pivot = new Vector2(0f, 0.5f);
            rowTRt.anchoredPosition = new Vector2(25, 20);
            rowTRt.sizeDelta = new Vector2(360, 26);
            instance.titleTexts[i] = tObj.AddComponent<TextMeshProUGUI>();
            instance.titleTexts[i].fontSize = 16;
            instance.titleTexts[i].fontStyle = FontStyles.Bold;
            instance.titleTexts[i].color = new Color(0.92f, 0.96f, 1f);
            instance.titleTexts[i].enableWordWrapping = false;

            // Current Stats (CĂN LỀ TRÁI pivot = 0, 0.5f)
            GameObject dObj = new GameObject("Desc");
            dObj.transform.SetParent(rowObj.transform, false);
            RectTransform dRt = dObj.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0.5f);
            dRt.anchorMax = new Vector2(0f, 0.5f);
            dRt.pivot = new Vector2(0f, 0.5f);
            dRt.anchoredPosition = new Vector2(25, -18);
            dRt.sizeDelta = new Vector2(360, 24);
            instance.descTexts[i] = dObj.AddComponent<TextMeshProUGUI>();
            instance.descTexts[i].fontSize = 13;
            instance.descTexts[i].color = new Color(0.78f, 0.84f, 0.94f);
            instance.descTexts[i].enableWordWrapping = false;

            // Level Text (Cột giữa)
            GameObject lObj = new GameObject("Level");
            lObj.transform.SetParent(rowObj.transform, false);
            RectTransform lRt = lObj.AddComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0.5f);
            lRt.anchorMax = new Vector2(0f, 0.5f);
            lRt.pivot = new Vector2(0f, 0.5f);
            lRt.anchoredPosition = new Vector2(405, 20);
            lRt.sizeDelta = new Vector2(240, 26);
            instance.levelTexts[i] = lObj.AddComponent<TextMeshProUGUI>();
            instance.levelTexts[i].fontSize = 15;
            instance.levelTexts[i].fontStyle = FontStyles.Bold;
            instance.levelTexts[i].enableWordWrapping = false;

            // Next Bonus (Cột giữa bên dưới)
            GameObject bObj = new GameObject("Bonus");
            bObj.transform.SetParent(rowObj.transform, false);
            RectTransform bRt = bObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0.5f);
            bRt.anchorMax = new Vector2(0f, 0.5f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(405, -18);
            bRt.sizeDelta = new Vector2(260, 24);
            instance.bonusTexts[i] = bObj.AddComponent<TextMeshProUGUI>();
            instance.bonusTexts[i].fontSize = 13;
            instance.bonusTexts[i].enableWordWrapping = false;

            // Upgrade Button (Cột phải)
            GameObject uBtnObj = new GameObject("Btn_Upgrade");
            uBtnObj.transform.SetParent(rowObj.transform, false);
            RectTransform uBtnRt = uBtnObj.AddComponent<RectTransform>();
            uBtnRt.anchorMin = new Vector2(1f, 0.5f);
            uBtnRt.anchorMax = new Vector2(1f, 0.5f);
            uBtnRt.pivot = new Vector2(1f, 0.5f);
            uBtnRt.anchoredPosition = new Vector2(-22, 0);
            uBtnRt.sizeDelta = new Vector2(190, 56);

            Image uBtnBg = uBtnObj.AddComponent<Image>();
            uBtnBg.color = new Color(0.18f, 0.48f, 0.36f, 1f);

            Button uBtn = uBtnObj.AddComponent<Button>();
            uBtn.onClick.AddListener(() =>
            {
                if (PlayerUpgradeManager.EnsureInstance().TryPurchaseUpgrade(type))
                {
                    SoundManager.EnsureInstance()?.PlayShardPickup();
                    instance.RefreshUI();
                }
            });
            instance.upgradeButtons[i] = uBtn;

            GameObject cLabelObj = new GameObject("CostText");
            cLabelObj.transform.SetParent(uBtnObj.transform, false);
            RectTransform clRt = cLabelObj.AddComponent<RectTransform>();
            clRt.anchorMin = Vector2.zero;
            clRt.anchorMax = Vector2.one;
            clRt.sizeDelta = Vector2.zero;

            var cTmpTmp = cLabelObj.AddComponent<TextMeshProUGUI>();
            cTmpTmp.fontSize = 14;
            cTmpTmp.fontStyle = FontStyles.Bold;
            cTmpTmp.alignment = TextAlignmentOptions.Center;
            cTmpTmp.color = Color.white;
            instance.costTexts[i] = cTmpTmp;
        }

        // 5. TAB 2 CONTENT: BẢNG MÔ TẢ CHI TIẾT CHIÊU THỨC (ATTACK, DASH, ULTI)
        instance.tabSkillsContent = new GameObject("SkillsContent");
        instance.tabSkillsContent.transform.SetParent(boxObj.transform, false);
        RectTransform skillsContentRt = instance.tabSkillsContent.AddComponent<RectTransform>();
        skillsContentRt.anchorMin = new Vector2(0.5f, 0f);
        skillsContentRt.anchorMax = new Vector2(0.5f, 1f);
        skillsContentRt.pivot = new Vector2(0.5f, 0.5f);
        skillsContentRt.sizeDelta = new Vector2(920, 0);
        skillsContentRt.anchoredPosition = new Vector2(0, -65);

        BuildSkillDescriptionCard(
            parent: instance.tabSkillsContent.transform,
            yPos: -85f,
            height: 125f,
            title: "⚔️ ĐÒN ĐÁNH THƯỜNG (BASIC ATTACK) [Phím J / Chuột Trái]",
            visualDesc: "✦ <color=#FFE266>Hình thức:</color> Vung kiếm chém liên hoàn 2 đòn (Combo 1 chém quét ngang, Combo 2 chém bổ chí mạng đẩy lùi nhẹ).",
            damageDesc: "✦ <color=#66FF99>Sát thương:</color> Combo 1: <b>15 DMG</b> | Combo 2: <b>25 DMG (Crit x1.67)</b>. Tầm đánh quạt bán kính 1.35m.",
            upgradeDesc: "✦ <color=#66CCFF>Khi nâng cấp Kiếm Tàn Tích:</color> Tăng vĩnh viễn <b>+6 Sát thương mỗi cấp</b> cho mọi đòn chém (Cấp Max: +60 DMG)."
        );

        BuildSkillDescriptionCard(
            parent: instance.tabSkillsContent.transform,
            yPos: -220f,
            height: 125f,
            title: "💨 LƯỚT BÓNG MA (SHADOW DASH) [Phím Space / Nút Lướt]",
            visualDesc: "✦ <color=#FFE266>Hình thức:</color> Tốc biến lướt nhanh theo hướng chỉ định, để lại chuỗi tàn ảnh bóng ma mờ ảo. <b>Miễn nhiễm sát thương (Bất tử 0.2s)</b>.",
            damageDesc: "✦ <color=#66FF99>Thông số:</color> Tốc độ lướt cực đại <b>12 m/s</b> (gấp 2.4 lần tốc độ chạy cơ bản). Hồi chiêu cơ bản: <b>0.8 giây</b>.",
            upgradeDesc: "✦ <color=#66CCFF>Khi nâng cấp Thoát Thân Bóng Ma:</color> Giảm hồi chiêu <b>-0.06s/cấp</b> (Tối đa còn <b>0.35s</b>) và tăng tốc độ chạy thường."
        );

        BuildSkillDescriptionCard(
            parent: instance.tabSkillsContent.transform,
            yPos: -355f,
            height: 145f,
            title: "⚡ CHIÊU CUỐI TUYỆT KỸ (ULTIMATE SKILL) [Phím F / Nút Chiêu]",
            visualDesc: "✦ <color=#FFE266>Hình thức:</color> Ngưng đọng thời gian (Slow-mo 0.05x), camera zoom cận cảnh điện ảnh, <b>mắt bừng sáng rực lửa (Eye Flare)</b> cùng câu thoại thức tỉnh!",
            damageDesc: "✦ <color=#66FF99>Sát thương:</color> <b>Vaelstrider:</b> Bão Kiếm 4 đợt hút quái & nổ tung diện rộng (<b>115+ DMG</b>). <b>Maid:</b> Sóng chém trăng khuyết <b>9999 DMG</b> diệt sạch quái thường, 250 DMG & Stun 2.5s Boss!",
            upgradeDesc: "✦ <color=#66CCFF>Khi nâng cấp Cơn Bão Cổ Đại:</color> Giảm hồi chiêu từ <b>12s xuống còn 7s</b> và cộng dồn thêm <b>+150 DMG nổ kết liễu</b> (Tổng DMG đạt 265+)."
        );

        instance.Close();
        Debug.Log("<color=cyan>[SHRINE UI]</color> Đã tạo giao diện Bàn Thờ Nâng Cấp & Bảng Tra Cứu Chiêu Thức hoàn chỉnh!");
        return instance;
    }

    private static void BuildSkillDescriptionCard(Transform parent, float yPos, float height, string title, string visualDesc, string damageDesc, string upgradeDesc)
    {
        GameObject card = new GameObject("SkillCard");
        card.transform.SetParent(parent, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, yPos);
        rt.sizeDelta = new Vector2(920, height);

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.55f, 0.85f, 0.6f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Header Title
        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(card.transform, false);
        RectTransform tRt = tObj.AddComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0f, 1f);
        tRt.anchoredPosition = new Vector2(18, -10);
        tRt.sizeDelta = new Vector2(-36, 26);
        var titleTmp = tObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = title;
        titleTmp.fontSize = 15;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(1f, 0.88f, 0.35f, 1f);

        // Content (Visual, Damage, Upgrade)
        GameObject cObj = new GameObject("Content");
        cObj.transform.SetParent(card.transform, false);
        RectTransform cRt = cObj.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 0f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0f, 1f);
        cRt.anchoredPosition = new Vector2(18, -36);
        cRt.sizeDelta = new Vector2(-36, -42);
        var contentTmp = cObj.AddComponent<TextMeshProUGUI>();
        contentTmp.text = $"{visualDesc}\n{damageDesc}\n{upgradeDesc}";
        contentTmp.fontSize = 13;
        contentTmp.lineSpacing = 16;
        contentTmp.color = new Color(0.88f, 0.92f, 0.98f, 1f);
    }
}
