using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bang chi so chi tiet nhan vat & ky nang (Character Stats & Skill Details).
/// Cho phep mo bat cu luc nao qua nut bam [STATS (C)] tren HUD hoac phim tat 'C'.
/// </summary>
public class CharacterStatsUI : MonoBehaviour
{
    public static CharacterStatsUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI characterTitleText;
    [SerializeField] private TextMeshProUGUI characterSubtitleText;

    // 4 Mini Stat Badges
    [SerializeField] private TextMeshProUGUI hpStatText;
    [SerializeField] private TextMeshProUGUI atkStatText;
    [SerializeField] private TextMeshProUGUI dashCdStatText;
    [SerializeField] private TextMeshProUGUI ultiCdStatText;

    // 3 Skill Detail Cards
    [SerializeField] private TextMeshProUGUI attackCardDesc;
    [SerializeField] private TextMeshProUGUI dashCardDesc;
    [SerializeField] private TextMeshProUGUI ultiCardTitle;
    [SerializeField] private TextMeshProUGUI ultiCardDesc;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Phim tat C de bat/tat bang chi so chi tiet
        if (Input.GetKeyDown(KeyCode.C))
        {
            Toggle();
        }
        else if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Toggle()
    {
        if (gameObject.activeSelf) Close();
        else Open();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        RefreshData();
        Time.timeScale = 0f;
        SoundManager.EnsureInstance()?.PlayMenuHover();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        SoundManager.EnsureInstance()?.PlayMenuHover();
    }

    public void RefreshData()
    {
        var upgradeMgr = PlayerUpgradeManager.EnsureInstance();
        var charMgr = CharacterManager.EnsureInstance();

        string selectedCharId = charMgr != null ? charMgr.GetSelectedCharacterId() : "Vaelstrider";
        bool isMaid = (selectedCharId == CharacterManager.CHAR_MAID);

        int atkBonus = upgradeMgr != null ? upgradeMgr.GetAttackBonus() : 0;
        int hpBonus = upgradeMgr != null ? upgradeMgr.GetHealthBonus() : 0;
        float dashCdRed = upgradeMgr != null ? upgradeMgr.GetDashCooldownReduction() : 0f;
        float ultiCdRed = upgradeMgr != null ? upgradeMgr.GetUltiCooldownReduction() : 0f;
        int ultiDmgBonus = upgradeMgr != null ? upgradeMgr.GetUltiDamageBonus() : 0;

        int totalAtk = 5 + atkBonus;
        int totalHp = 100 + hpBonus;
        float finalDashCd = Mathf.Max(1.0f, 5.0f - dashCdRed);
        float finalUltiCd = Mathf.Max(5.0f, 30.0f - ultiCdRed);

        // Header Title
        if (characterTitleText != null)
        {
            characterTitleText.text = isMaid 
                ? "CHIEN BINH MAID (WARRIOR MAID)" 
                : "HIEP SI VAELSTRIDER";
        }
        if (characterSubtitleText != null)
        {
            characterSubtitleText.text = isMaid
                ? "Phong cach: Tuyet ky Trang Khuyet | Tram sat tuc thi & Khong che Boss"
                : "Phong cach: Kiem Si Co Tu | Dot kich lien hoan & Bao kiem quet rong";
        }

        // 4 Quick Stats
        if (hpStatText != null) hpStatText.text = $"MAU (HP)\n<color=#55FF88>{totalHp}</color> <size=12>(+ {hpBonus})</size>";
        if (atkStatText != null) atkStatText.text = $"TAN CONG (ATK)\n<color=#FF5566>{totalAtk}</color> <size=12>(+ {atkBonus})</size>";
        if (dashCdStatText != null) dashCdStatText.text = $"LUOT (DASH CD)\n<color=#44DDFF>{finalDashCd:F1}s</color> <size=12>(- {dashCdRed:F1}s)</size>";
        if (ultiCdStatText != null) ultiCdStatText.text = $"TUYET KY (ULTI CD)\n<color=#FFAA33>{finalUltiCd:F1}s</color> <size=12>(- {ultiCdRed:F1}s)</size>";

        // Card 1: Attack
        if (attackCardDesc != null)
        {
            attackCardDesc.text = 
                $"- <b>Sat thuong:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> (Goc 5 + Cap kiem +{atkBonus}). Tam danh: 1.35m.\n" +
                $"- <b>Hinh thuc:</b> Combo 2 nhip kiem lien hoan [Phim J / Chuot trai / Nut Tan cong].\n" +
                $"- <b>Hoi phuc:</b> Moi don chem trung hoi <color=#38BDF8><b>+8 NL</b></color> & tich <color=#FBBF24><b>+15 Noi Nang</b></color>.\n" +
                $"  - <i>Nhip 1:</i> Vung kiem quet ngang nhanh, lam cham nhe quai.\n" +
                $"  - <i>Nhip 2:</i> Bo kiem chi mang don sat thuong ket hop day lui doi thu.";
        }

        // Card 2: Dash
        if (dashCardDesc != null)
        {
            dashCardDesc.text = 
                $"- <b>Tieu hao:</b> <color=#38BDF8><b>20 NL</b></color> | <b>Hoi chieu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color> [Phim Space / Nut Luot].\n" +
                $"- <b>Sat thuong:</b> <color=#FFFF66><b>0 DMG</b></color> (Ky nang ne don thuan tuy, tuyet doi khong gay sat thuong).\n" +
                $"- <b>Dac tinh:</b> Luot toc bien 12 m/s theo huong di chuyen, de lai tan anh bong ma. <color=#55FF88><b>Bat tu hoan toan trong 0.2s</b></color> de ne moi don danh va dan cua quai.";
        }

        // Card 3: Ulti
        if (ultiCardTitle != null)
        {
            ultiCardTitle.text = isMaid 
                ? "TUYET KY: QUET TAN KE YEU (SWEEP THE WEAK)" 
                : "TUYET KY: CON LOC TAN TICH (RUIN WHIRLWIND)";
        }
        if (ultiCardDesc != null)
        {
            if (isMaid)
            {
                ultiCardDesc.text = 
                    $"- <b>Tieu hao:</b> <color=#38BDF8><b>50 NL</b></color> | <b>Hoi chieu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color> (Goc 30.0s - Giam tru {ultiCdRed:F1}s) [Phim F / Nut Ulti].\n" +
                    $"- <b>Dien anh:</b> Ngung dong thoi gian slow-mo, camera can canh, mat loe sang ruc ro va phat loi thoai don dep phe tich!\n" +
                    $"- <b>Hieu ung uy luc:</b> Vung dao khi trang khuyet goc rong 140 do ban kinh 4.8m:\n" +
                    $"  - <color=#FF4455><b>Quai thuong:</b> KET LIEU NGAY LAP TUC (Insta-Kill/Execute)</color> toan bo quai trong tam quet.\n" +
                    $"  - <color=#FFCC33><b>Boss:</b> Gay 250 DMG (+{ultiDmgBonus}) va LAM CHOANG BOSS TRONG 2.5 GIAY</color>!";
            }
            else
            {
                ultiCardDesc.text = 
                    $"- <b>Tieu hao:</b> <color=#38BDF8><b>50 NL</b></color> | <b>Hoi chieu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color> (Goc 30.0s - Giam tru {ultiCdRed:F1}s) [Phim F / Nut Ulti].\n" +
                    $"- <b>Hieu ung uy luc:</b> Van noi luc trieu hoi Con Bao Kiem Co Tu ban kinh 2.8m quet 4 dot lien tuc.\n" +
                    $"- <b>Sat thuong:</b> Hut toan bo quai xung quanh vao tam bao, don sat thuong dien rong uy luc va phat no hat vang moi ke dich can duong!";
            }
        }
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = 5;
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    public static CharacterStatsUI EnsureUIExists()
    {
        Canvas rootCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) return null;

        // 1. Tao Nut Bam [STATS] tren HUD (-78, -20) ben canh nut Setting (-20, -20)
        Transform existingBtn = rootCanvas.transform.Find("Btn_CharacterStats");
        if (existingBtn == null)
        {
            GameObject btnObj = CreateUIObject("Btn_CharacterStats", rootCanvas.transform);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 1f);
            btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.pivot = new Vector2(1f, 1f);
            btnRt.anchoredPosition = new Vector2(-78f, -20f);
            btnRt.sizeDelta = new Vector2(50f, 50f);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.12f, 0.16f, 0.24f, 0.94f);

            var outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.65f, 0.95f, 0.75f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() =>
            {
                var ui = Instance != null ? Instance : EnsureUIExists();
                if (ui != null) ui.Toggle();
            });

            GameObject txtObj = CreateUIObject("IconText", btnObj.transform);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "STATS";
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.4f, 0.92f, 1f, 1f);
            tmp.raycastTarget = false;
        }

        // 2. Tao Cua So Modal CharacterStatsPanel
        Transform existingModal = rootCanvas.transform.Find("CharacterStatsPanel");
        if (existingModal != null)
        {
            Instance = existingModal.GetComponent<CharacterStatsUI>();
            if (Instance != null) return Instance;
        }

        GameObject modalObj = CreateUIObject("CharacterStatsPanel", rootCanvas.transform);
        RectTransform modalRt = modalObj.GetComponent<RectTransform>();
        modalRt.anchorMin = Vector2.zero;
        modalRt.anchorMax = Vector2.one;
        modalRt.offsetMin = Vector2.zero;
        modalRt.offsetMax = Vector2.zero;

        Image modalBg = modalObj.AddComponent<Image>();
        modalBg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f); // Backdrop mo sang trong

        Instance = modalObj.AddComponent<CharacterStatsUI>();

        // Card Box chinh giua man hinh (860 x 580)
        GameObject boxObj = CreateUIObject("StatsBox", modalObj.transform);
        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.sizeDelta = new Vector2(860f, 580f);
        boxRt.anchoredPosition = Vector2.zero;

        Image boxBg = boxObj.AddComponent<Image>();
        boxBg.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);

        var boxOutline = boxObj.AddComponent<Outline>();
        boxOutline.effectColor = new Color(0.28f, 0.65f, 0.95f, 0.85f);
        boxOutline.effectDistance = new Vector2(2f, -2f);

        // Header: Tieu de
        GameObject titleObj = CreateUIObject("HeaderTitle", boxObj.transform);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -14f);
        titleRt.sizeDelta = new Vector2(0f, 32f);

        Instance.characterTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        Instance.characterTitleText.fontSize = 22;
        Instance.characterTitleText.fontStyle = FontStyles.Bold;
        Instance.characterTitleText.alignment = TextAlignmentOptions.Center;
        Instance.characterTitleText.color = new Color(1f, 0.85f, 0.35f);

        // Header: Phu de
        GameObject subObj = CreateUIObject("HeaderSubtitle", boxObj.transform);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -44f);
        subRt.sizeDelta = new Vector2(0f, 22f);

        Instance.characterSubtitleText = subObj.AddComponent<TextMeshProUGUI>();
        Instance.characterSubtitleText.fontSize = 12;
        Instance.characterSubtitleText.fontStyle = FontStyles.Italic;
        Instance.characterSubtitleText.alignment = TextAlignmentOptions.Center;
        Instance.characterSubtitleText.color = new Color(0.75f, 0.85f, 0.95f);

        // Close Button [X]
        GameObject closeBtnObj = CreateUIObject("Btn_Close", boxObj.transform);
        RectTransform cRt = closeBtnObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-16f, -16f);
        cRt.sizeDelta = new Vector2(36f, 36f);

        Image cImg = closeBtnObj.AddComponent<Image>();
        cImg.color = new Color(0.75f, 0.18f, 0.22f, 1f);

        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.targetGraphic = cImg;
        closeBtn.onClick.AddListener(() => Instance.Close());

        GameObject cTxtObj = CreateUIObject("X", closeBtnObj.transform);
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

        // 4 Quick Stat Badges Strip
        GameObject statsStrip = CreateUIObject("StatsStrip", boxObj.transform);
        RectTransform ssRt = statsStrip.GetComponent<RectTransform>();
        ssRt.anchorMin = new Vector2(0.5f, 1f);
        ssRt.anchorMax = new Vector2(0.5f, 1f);
        ssRt.pivot = new Vector2(0.5f, 1f);
        ssRt.anchoredPosition = new Vector2(0f, -80f);
        ssRt.sizeDelta = new Vector2(810f, 52f);

        float badgeWidth = 195f;
        float badgeSpacing = 10f;
        Instance.hpStatText = CreateStatBadge(statsStrip.transform, "Badge_HP", 0, badgeWidth, badgeSpacing, new Color(0.12f, 0.24f, 0.18f, 0.9f));
        Instance.atkStatText = CreateStatBadge(statsStrip.transform, "Badge_ATK", 1, badgeWidth, badgeSpacing, new Color(0.26f, 0.14f, 0.16f, 0.9f));
        Instance.dashCdStatText = CreateStatBadge(statsStrip.transform, "Badge_DashCD", 2, badgeWidth, badgeSpacing, new Color(0.12f, 0.20f, 0.28f, 0.9f));
        Instance.ultiCdStatText = CreateStatBadge(statsStrip.transform, "Badge_UltiCD", 3, badgeWidth, badgeSpacing, new Color(0.28f, 0.20f, 0.12f, 0.9f));

        // 3 Detail Cards (Attack, Dash, Ulti)
        float cardY = -145f;
        float cardHeight = 125f;
        float cardSpacing = 135f;

        // Card 1: Attack
        CreateSkillCard(boxObj.transform, "Card_Attack", cardY, cardHeight, 
            "DON DANH THUONG (BASIC ATTACK) [J / Chuot trai / Nut Danh]",
            out Instance.attackCardDesc);

        // Card 2: Dash
        CreateSkillCard(boxObj.transform, "Card_Dash", cardY - cardSpacing, cardHeight, 
            "LUOT BONG MA (SHADOW DASH) [Space / Nut Luot]",
            out Instance.dashCardDesc);

        // Card 3: Ulti
        CreateSkillCard(boxObj.transform, "Card_Ulti", cardY - (cardSpacing * 2), cardHeight + 25f, 
            "CHIEU CUOI TUYET KY (ULTIMATE SKILL) [F / Nut Tuyet Ky]",
            out Instance.ultiCardDesc, out Instance.ultiCardTitle);

        modalObj.SetActive(false);
        return Instance;
    }

    private static TextMeshProUGUI CreateStatBadge(Transform parent, string name, int index, float width, float spacing, Color bgColor)
    {
        GameObject badgeObj = CreateUIObject(name, parent);
        RectTransform bRt = badgeObj.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0f, 0.5f);
        bRt.anchorMax = new Vector2(0f, 0.5f);
        bRt.pivot = new Vector2(0f, 0.5f);
        bRt.anchoredPosition = new Vector2(index * (width + spacing), 0f);
        bRt.sizeDelta = new Vector2(width, 50f);

        Image bg = badgeObj.AddComponent<Image>();
        bg.color = bgColor;

        var outline = badgeObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.7f, 1f, 0.45f);
        outline.effectDistance = new Vector2(1f, -1f);

        GameObject txtObj = CreateUIObject("Text", badgeObj.transform);
        RectTransform tRt = txtObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 11;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.lineSpacing = 12;
        return tmp;
    }

    private static void CreateSkillCard(Transform parent, string name, float yPos, float height, string title, out TextMeshProUGUI descTmp)
    {
        TextMeshProUGUI unusedTitleTmp;
        CreateSkillCard(parent, name, yPos, height, title, out descTmp, out unusedTitleTmp);
    }

    private static void CreateSkillCard(Transform parent, string name, float yPos, float height, string title, out TextMeshProUGUI descTmp, out TextMeshProUGUI titleTmp)
    {
        GameObject cardObj = CreateUIObject(name, parent);
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(810f, height);

        Image bg = cardObj.AddComponent<Image>();
        bg.color = new Color(0.11f, 0.14f, 0.22f, 0.95f);

        var outline = cardObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.55f, 0.85f, 0.65f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Header Title Text
        GameObject tObj = CreateUIObject("Title", cardObj.transform);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0f, 1f);
        tRt.anchoredPosition = new Vector2(18f, -8f);
        tRt.sizeDelta = new Vector2(-36f, 24f);

        titleTmp = tObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = title;
        titleTmp.fontSize = 14;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(1f, 0.88f, 0.38f);

        // Description Body Text
        GameObject dObj = CreateUIObject("Desc", cardObj.transform);
        RectTransform dRt = dObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 1f);
        dRt.pivot = new Vector2(0f, 1f);
        dRt.anchoredPosition = new Vector2(18f, -32f);
        dRt.sizeDelta = new Vector2(-36f, -38f);

        descTmp = dObj.AddComponent<TextMeshProUGUI>();
        descTmp.fontSize = 12.5f;
        descTmp.lineSpacing = 16;
        descTmp.color = new Color(0.88f, 0.92f, 0.98f);
        descTmp.enableWordWrapping = true;
    }
}
