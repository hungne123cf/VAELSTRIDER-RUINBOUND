using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShrineUpgradeUI : MonoBehaviour
{
    private static ShrineUpgradeUI instance;

    [Header("UI Elements")]
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI shardsCounterText;

    private GameObject[] cardObjects = new GameObject[4];
    private TextMeshProUGUI[] titleTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] levelTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] descTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] bonusTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] costTexts = new TextMeshProUGUI[4];
    private Button[] upgradeButtons = new Button[4];

    private bool isOpen = false;

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Awake()
    {
        instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        Close();
        if (PlayerUpgradeManager.Instance != null)
        {
            PlayerUpgradeManager.Instance.OnUpgradesChanged += RefreshUI;
        }

        var inv = FindAnyObjectByType<PlayerInventory>();
        if (inv != null)
        {
            inv.OnShardsChanged += (s) => RefreshUI();
        }
    }

    void Update()
    {
        if (isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)))
        {
            Close();
        }
    }

    public bool IsOpen() => isOpen;

    public void Open()
    {
        isOpen = true;
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.SetCanMove(false);

        RefreshUI();
    }

    public void Close()
    {
        isOpen = false;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        gameObject.SetActive(false);

        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.SetCanMove(true);
    }

    public void RefreshUI()
    {
        var mgr = PlayerUpgradeManager.EnsureInstance();
        var inv = FindAnyObjectByType<PlayerInventory>();
        int curShards = inv != null ? inv.GetShards() : 0;

        if (shardsCounterText != null)
        {
            shardsCounterText.text = "MẢNH CỔ TỰ (AETHEL SHARDS): <color=#55FFFF>" + curShards + "</color> <color=#00FFFF>◆</color>";
        }

        for (int i = 0; i < 4; i++)
        {
            var type = (PlayerUpgradeManager.UpgradeType)i;
            int lvl = mgr.GetLevel(type);
            int cost = mgr.GetCost(type);

            if (titleTexts[i] != null) titleTexts[i].text = mgr.GetTitle(type);
            if (levelTexts[i] != null)
            {
                levelTexts[i].text = lvl >= PlayerUpgradeManager.MAX_LEVEL 
                    ? "<color=#FFD700>CẤP TỐI ĐA (MAX)</color>" 
                    : "CẤP ĐỘ: <color=#55FFFF>" + lvl + " / " + PlayerUpgradeManager.MAX_LEVEL + "</color>";
            }

            if (descTexts[i] != null)
            {
                descTexts[i].text = "Hiện tại: <color=#DDDDDD>" + (lvl == 0 ? "Chưa mở khóa" : mgr.GetDescription(type, lvl)) + "</color>";
            }

            if (bonusTexts[i] != null)
            {
                bonusTexts[i].text = "Kế tiếp: <color=#77FF77>" + mgr.GetNextBonusDescription(type) + "</color>";
            }

            if (costTexts[i] != null)
            {
                if (lvl >= PlayerUpgradeManager.MAX_LEVEL)
                {
                    costTexts[i].text = "TỐI ĐA (MAX)";
                }
                else
                {
                    costTexts[i].text = "NÂNG CẤP (" + cost + " ◆)";
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

        // Create Panel
        GameObject panelObj = new GameObject("ShrineUpgradePanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.05f, 0.09f, 0.90f);

        instance = panelObj.AddComponent<ShrineUpgradeUI>();

        // Center Container
        GameObject boxObj = new GameObject("DialogBox");
        boxObj.transform.SetParent(panelObj.transform, false);
        RectTransform boxRt = boxObj.AddComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(900, 560);
        Image boxBg = boxObj.AddComponent<Image>();
        boxBg.color = new Color(0.09f, 0.12f, 0.18f, 0.98f);

        // Header Title
        GameObject titleObj = new GameObject("HeaderTitle");
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform tRt = titleObj.AddComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 1f);
        tRt.anchorMax = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0, -35);
        tRt.sizeDelta = new Vector2(760, 45);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "✦ BÀN THỜ NÂNG CẤP CỔ ĐẠI ✦";
        titleTmp.fontSize = 25;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.35f, 0.95f, 1f, 1f);

        // Shards Counter
        GameObject shardObj = new GameObject("ShardCounter");
        shardObj.transform.SetParent(boxObj.transform, false);
        RectTransform sRt = shardObj.AddComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.5f, 1f);
        sRt.anchorMax = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0, -75);
        sRt.sizeDelta = new Vector2(650, 30);
        instance.shardsCounterText = shardObj.AddComponent<TextMeshProUGUI>();
        instance.shardsCounterText.fontSize = 17;
        instance.shardsCounterText.fontStyle = FontStyles.Bold;
        instance.shardsCounterText.alignment = TextAlignmentOptions.Center;
        instance.shardsCounterText.color = new Color(1f, 0.9f, 0.4f, 1f);

        // Close Button [X]
        GameObject closeBtnObj = new GameObject("Btn_Close");
        closeBtnObj.transform.SetParent(boxObj.transform, false);
        RectTransform cRt = closeBtnObj.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-25, -25);
        cRt.sizeDelta = new Vector2(40, 40);
        Image cBg = closeBtnObj.AddComponent<Image>();
        cBg.color = new Color(0.55f, 0.16f, 0.16f, 1f);
        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => instance.Close());

        GameObject cTxtObj = new GameObject("X");
        cTxtObj.transform.SetParent(closeBtnObj.transform, false);
        var cTmp = cTxtObj.AddComponent<TextMeshProUGUI>();
        cTmp.text = "✕";
        cTmp.fontSize = 22;
        cTmp.alignment = TextAlignmentOptions.Center;
        cTmp.color = Color.white;
        RectTransform cTxtRt = cTxtObj.GetComponent<RectTransform>();
        cTxtRt.anchorMin = Vector2.zero;
        cTxtRt.anchorMax = Vector2.one;
        cTxtRt.sizeDelta = Vector2.zero;

        // 4 Upgrade Cards (Vertical Layout)
        float startY = -140f;
        float spacingY = 92f;

        for (int i = 0; i < 4; i++)
        {
            int index = i;
            var type = (PlayerUpgradeManager.UpgradeType)i;

            GameObject rowObj = new GameObject("UpgradeCard_" + i);
            rowObj.transform.SetParent(boxObj.transform, false);
            RectTransform rowRt = rowObj.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 1f);
            rowRt.anchorMax = new Vector2(0.5f, 1f);
            rowRt.anchoredPosition = new Vector2(0, startY - (i * spacingY));
            rowRt.sizeDelta = new Vector2(840, 80);

            Image rowBg = rowObj.AddComponent<Image>();
            rowBg.color = new Color(0.13f, 0.17f, 0.25f, 0.90f);
            instance.cardObjects[i] = rowObj;

            // Title (Width 360, single line, no wrapping)
            GameObject tObj = new GameObject("Title");
            tObj.transform.SetParent(rowObj.transform, false);
            RectTransform rowTRt = tObj.AddComponent<RectTransform>();
            rowTRt.anchorMin = new Vector2(0, 0.5f);
            rowTRt.anchorMax = new Vector2(0, 0.5f);
            rowTRt.anchoredPosition = new Vector2(22, 17);
            rowTRt.sizeDelta = new Vector2(360, 26);
            instance.titleTexts[i] = tObj.AddComponent<TextMeshProUGUI>();
            instance.titleTexts[i].fontSize = 16;
            instance.titleTexts[i].fontStyle = FontStyles.Bold;
            instance.titleTexts[i].color = new Color(0.92f, 0.96f, 1f);
            instance.titleTexts[i].enableWordWrapping = false;

            // Level Text (Width 220, position 400)
            GameObject lObj = new GameObject("Level");
            lObj.transform.SetParent(rowObj.transform, false);
            RectTransform lRt = lObj.AddComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0, 0.5f);
            lRt.anchorMax = new Vector2(0, 0.5f);
            lRt.anchoredPosition = new Vector2(400, 17);
            lRt.sizeDelta = new Vector2(220, 26);
            instance.levelTexts[i] = lObj.AddComponent<TextMeshProUGUI>();
            instance.levelTexts[i].fontSize = 15;
            instance.levelTexts[i].fontStyle = FontStyles.Bold;
            instance.levelTexts[i].enableWordWrapping = false;

            // Current Stats (Width 360, position y = -17)
            GameObject dObj = new GameObject("Desc");
            dObj.transform.SetParent(rowObj.transform, false);
            RectTransform dRt = dObj.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0, 0.5f);
            dRt.anchorMax = new Vector2(0, 0.5f);
            dRt.anchoredPosition = new Vector2(22, -17);
            dRt.sizeDelta = new Vector2(360, 24);
            instance.descTexts[i] = dObj.AddComponent<TextMeshProUGUI>();
            instance.descTexts[i].fontSize = 13;
            instance.descTexts[i].color = new Color(0.78f, 0.82f, 0.92f);
            instance.descTexts[i].enableWordWrapping = false;

            // Next Bonus (Width 230, position 400, y = -17)
            GameObject bObj = new GameObject("Bonus");
            bObj.transform.SetParent(rowObj.transform, false);
            RectTransform bRt = bObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0, 0.5f);
            bRt.anchorMax = new Vector2(0, 0.5f);
            bRt.anchoredPosition = new Vector2(400, -17);
            bRt.sizeDelta = new Vector2(240, 24);
            instance.bonusTexts[i] = bObj.AddComponent<TextMeshProUGUI>();
            instance.bonusTexts[i].fontSize = 13;
            instance.bonusTexts[i].enableWordWrapping = false;

            // Upgrade Button (Width 175, height 54, position -105)
            GameObject uBtnObj = new GameObject("Btn_Upgrade");
            uBtnObj.transform.SetParent(rowObj.transform, false);
            RectTransform uBtnRt = uBtnObj.AddComponent<RectTransform>();
            uBtnRt.anchorMin = new Vector2(1f, 0.5f);
            uBtnRt.anchorMax = new Vector2(1f, 0.5f);
            uBtnRt.anchoredPosition = new Vector2(-105, 0);
            uBtnRt.sizeDelta = new Vector2(175, 54);

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

        instance.Close();
        Debug.Log("<color=cyan>[SHRINE UI]</color> Đã tạo giao diện Bàn Thờ Nâng Cấp tinh chỉnh đẹp mắt!");
        return instance;
    }
}
