using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Giao diện Bàn Thờ Nâng Cấp Cổ Đại (30 Cấp Độ).
/// Gọn gàng, thẩm mỹ chuẩn Dark Fantasy, căn lề chuẩn xác không bị tràn viền.
/// </summary>
public class ShrineUpgradeUI : MonoBehaviour
{
    private static ShrineUpgradeUI instance;

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

    public void RefreshUI()
    {
        var mgr = PlayerUpgradeManager.EnsureInstance();
        if (mgr == null) return;

        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;

        if (shardsCounterText != null)
        {
            string label = isVi ? "MẢNH CỔ TỰ (AETHEL SHARDS)" : "AETHEL SHARDS";
            string curShardsStr = MainMenuManager.FormatCompactNumber(mgr.CurrentShards);
            shardsCounterText.text = $"{label}: {curShardsStr}";
        }

        for (int i = 0; i < 4; i++)
        {
            var type = (PlayerUpgradeManager.UpgradeType)i;
            int lvl = mgr.GetLevel(type);
            int cost = mgr.GetCost(type);
            bool isMax = mgr.IsMaxLevel(type);

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
                    bonusTexts[i].text = isVi 
                        ? $"Cấp kế tiếp: <color=#FFD700>MAX</color>" 
                        : $"Next Level: <color=#FFD700>MAX</color>";
                }
                else
                {
                    bonusTexts[i].text = isVi 
                        ? $"Cấp kế tiếp: <color=#66FF99>{mgr.GetNextBonusString(type)}</color>" 
                        : $"Next Level: <color=#66FF99>{mgr.GetNextBonusString(type)}</color>";
                }
            }

            if (costTexts[i] != null)
            {
                if (isMax)
                {
                    costTexts[i].text = "<size=13>MAX</size>";
                }
                else
                {
                    string costStr = cost >= 10000 ? (cost / 1000.0).ToString("0.#") + "K" : cost.ToString("N0");
                    costTexts[i].text = isVi 
                        ? $"NÂNG CẤP\n<size=11>{costStr} Mảnh</size>" 
                        : $"UPGRADE\n<size=11>{costStr} Shards</size>";
                }
                costTexts[i].enableAutoSizing = true;
                costTexts[i].fontSizeMin = 8.5f;
                costTexts[i].fontSizeMax = 12f;
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
                    bool canAfford = (mgr.CurrentShards >= cost);
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
        pCanvas.sortingOrder = 99990;
        panelObj.AddComponent<GraphicRaycaster>();

        instance = panelObj.AddComponent<ShrineUpgradeUI>();
        instance.canvasGroup = panelObj.GetComponent<CanvasGroup>();

        // 2. Center Dialog Box (Width 960, Height 580)
        GameObject boxObj = new GameObject("DialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxObj.layer = 5;
        boxObj.transform.SetParent(panelObj.transform, false);
        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(960, 580);

        Image boxBg = boxObj.GetComponent<Image>();
        boxBg.color = new Color(0.08f, 0.11f, 0.17f, 0.98f);

        Outline boxOutline = boxObj.AddComponent<Outline>();
        boxOutline.effectColor = new Color(0.25f, 0.65f, 0.95f, 0.85f);
        boxOutline.effectDistance = new Vector2(2f, -2f);

        // Header Title
        GameObject titleObj = new GameObject("HeaderTitle", typeof(RectTransform));
        titleObj.layer = 5;
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 1f);
        tRt.anchorMax = new Vector2(0.5f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0, -20);
        tRt.sizeDelta = new Vector2(760, 36);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "BÀN THỜ NÂNG CẤP CỔ ĐẠI";
        titleTmp.fontSize = 23;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.35f, 0.95f, 1f, 1f);

        // Shards Counter
        GameObject shardObj = new GameObject("ShardCounter", typeof(RectTransform));
        shardObj.layer = 5;
        shardObj.transform.SetParent(boxObj.transform, false);
        RectTransform sRt = shardObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.5f, 1f);
        sRt.anchorMax = new Vector2(0.5f, 1f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0, -56);
        sRt.sizeDelta = new Vector2(650, 26);
        instance.shardsCounterText = shardObj.AddComponent<TextMeshProUGUI>();
        instance.shardsCounterText.fontSize = 16;
        instance.shardsCounterText.fontStyle = FontStyles.Bold;
        instance.shardsCounterText.alignment = TextAlignmentOptions.Center;
        instance.shardsCounterText.color = new Color(1f, 0.88f, 0.35f, 1f);

        // Close Button [X]
        GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtnObj.layer = 5;
        closeBtnObj.transform.SetParent(boxObj.transform, false);
        RectTransform cRt = closeBtnObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-18, -18);
        cRt.sizeDelta = new Vector2(36, 36);
        Image cBg = closeBtnObj.GetComponent<Image>();
        cBg.color = new Color(0.72f, 0.16f, 0.18f, 1f);
        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.targetGraphic = cBg;
        closeBtn.onClick.AddListener(() => instance.Close());

        GameObject cTxtObj = new GameObject("X", typeof(RectTransform));
        cTxtObj.layer = 5;
        cTxtObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform cxRt = cTxtObj.GetComponent<RectTransform>();
        cxRt.anchorMin = Vector2.zero;
        cxRt.anchorMax = Vector2.one;
        cxRt.sizeDelta = Vector2.zero;
        var cxTmp = cTxtObj.AddComponent<TextMeshProUGUI>();
        cxTmp.text = "X";
        cxTmp.fontSize = 20;
        cxTmp.fontStyle = FontStyles.Bold;
        cxTmp.alignment = TextAlignmentOptions.Center;
        cxTmp.color = Color.white;
        cxTmp.raycastTarget = false;

        // Container 4 Thẻ Nâng Cấp (Centrally spaced)
        float startY = -95f;
        float spacingY = 110f;

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
            rowRt.sizeDelta = new Vector2(900, 95);

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
            instance.titleTexts[i].fontSize = 16;
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
            instance.descTexts[i].fontSize = 13;
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
            lRt.sizeDelta = new Vector2(240, 26);
            instance.levelTexts[i] = lObj.AddComponent<TextMeshProUGUI>();
            instance.levelTexts[i].fontSize = 15;
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
            bRt.sizeDelta = new Vector2(260, 24);
            instance.bonusTexts[i] = bObj.AddComponent<TextMeshProUGUI>();
            instance.bonusTexts[i].fontSize = 13;

            // Upgrade Button (Cột phải)
            GameObject uBtnObj = new GameObject("Btn_Upgrade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            uBtnObj.layer = 5;
            uBtnObj.transform.SetParent(rowObj.transform, false);
            RectTransform uBtnRt = uBtnObj.GetComponent<RectTransform>();
            uBtnRt.anchorMin = new Vector2(1f, 0.5f);
            uBtnRt.anchorMax = new Vector2(1f, 0.5f);
            uBtnRt.pivot = new Vector2(1f, 0.5f);
            uBtnRt.anchoredPosition = new Vector2(-22, 0);
            uBtnRt.sizeDelta = new Vector2(190, 58);

            Image uBtnBg = uBtnObj.GetComponent<Image>();
            uBtnBg.color = new Color(0.18f, 0.48f, 0.36f, 1f);

            Button uBtn = uBtnObj.GetComponent<Button>();
            uBtn.targetGraphic = uBtnBg;
            uBtn.onClick.AddListener(() =>
            {
                if (PlayerUpgradeManager.EnsureInstance().TryPurchaseUpgrade(type))
                {
                    SoundManager.EnsureInstance()?.PlayShardPickup();
                    instance.RefreshUI();
                }
            });
            instance.upgradeButtons[i] = uBtn;

            GameObject cLabelObj = new GameObject("CostText", typeof(RectTransform));
            cLabelObj.layer = 5;
            cLabelObj.transform.SetParent(uBtnObj.transform, false);
            RectTransform clRt = cLabelObj.GetComponent<RectTransform>();
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

        instance.Close();
        Debug.Log("<color=cyan>[SHRINE UI]</color> Đã tạo giao diện Bàn Thờ Nâng Cấp Cổ Đại gọn gàng chuẩn xác!");
        return instance;
    }
}
