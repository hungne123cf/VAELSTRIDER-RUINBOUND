using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bảng chỉ số chi tiết nhân vật & kỹ năng (Character Stats & Skill Details).
/// Hỗ trợ chuyển đổi ngôn ngữ linh hoạt 100% Tiếng Việt / English.
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
    [SerializeField] private TextMeshProUGUI attackCardTitle;
    [SerializeField] private TextMeshProUGUI attackCardDesc;
    [SerializeField] private TextMeshProUGUI dashCardTitle;
    [SerializeField] private TextMeshProUGUI dashCardDesc;
    [SerializeField] private TextMeshProUGUI ultiCardTitle;
    [SerializeField] private TextMeshProUGUI ultiCardDesc;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        LocalizationManager.OnLanguageChanged += RefreshData;
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshData;
    }

    void Update()
    {
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
        bool isVi = LocalizationManager.IsVietnamese;

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
                ? (isVi ? "HẦU GÁI CHIẾN BINH (ELYSIA)" : "WARRIOR MAID (ELYSIA)")
                : (isVi ? "HIỆP SĨ VAELSTRIDER" : "KNIGHT VAELSTRIDER");
        }
        if (characterSubtitleText != null)
        {
            characterSubtitleText.text = isMaid
                ? (isVi ? "Phong cách: Tuyệt kỹ Trăng Khuyết | Trảm sát tức thì & Khống chế Boss" : "Style: Crescent Slash | Instant Execute & Boss Stun")
                : (isVi ? "Phong cách: Kiếm Sĩ Cổ Tộc | Đột kích liên hoàn & Bão kiếm quét rộng" : "Style: Ancient Blade | Multi-slash Combo & Area Storm");
        }

        // 4 Quick Stats
        if (hpStatText != null)
        {
            string label = isVi ? "MÁU (HP)" : "HEALTH (HP)";
            hpStatText.text = $"{label}\n<color=#55FF88>{totalHp}</color> <size=12>(+ {hpBonus})</size>";
        }
        if (atkStatText != null)
        {
            string label = isVi ? "TẤN CÔNG (ATK)" : "ATTACK (ATK)";
            atkStatText.text = $"{label}\n<color=#FF5566>{totalAtk}</color> <size=12>(+ {atkBonus})</size>";
        }
        if (dashCdStatText != null)
        {
            string label = isVi ? "LƯỚT (DASH CD)" : "DASH (CD)";
            dashCdStatText.text = $"{label}\n<color=#44DDFF>{finalDashCd:F1}s</color> <size=12>(- {dashCdRed:F1}s)</size>";
        }
        if (ultiCdStatText != null)
        {
            string label = isVi ? "TUYỆT KỸ (ULTI CD)" : "ULTIMATE (CD)";
            ultiCdStatText.text = $"{label}\n<color=#FFAA33>{finalUltiCd:F1}s</color> <size=12>(- {ultiCdRed:F1}s)</size>";
        }

        // Card 1: Attack
        if (attackCardTitle != null)
        {
            attackCardTitle.text = isVi ? "1. ĐÒN ĐÁNH CƠ BẢN (BASIC ATTACK)" : "1. BASIC ATTACK";
        }
        if (attackCardDesc != null)
        {
            attackCardDesc.text = isVi
                ? $"- <b>Sát thương:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> (Gốc 5 + Cấp kiếm +{atkBonus}). Tầm đánh: 1.35m.\n" +
                  $"- <b>Hình thức:</b> Combo 2 nhịp kiếm liên hoàn [Phím J / Chuột trái / Nút Tấn công].\n" +
                  $"- <b>Hồi phục:</b> Mỗi đòn chém trúng hồi <color=#38BDF8><b>+8 NL</b></color> & tích <color=#FBBF24><b>+15 Nội Năng</b></color>."
                : $"- <b>Damage:</b> <color=#FF6677><b>{totalAtk} ATK</b></color> (Base 5 + Sword +{atkBonus}). Range: 1.35m.\n" +
                  $"- <b>Pattern:</b> 2-Hit Strike Combo [Key J / Left Click / Attack Button].\n" +
                  $"- <b>Regen:</b> Hits restore <color=#38BDF8><b>+8 Energy</b></color> & gain <color=#FBBF24><b>+15 Inner Power</b></color>.";
        }

        // Card 2: Dash
        if (dashCardTitle != null)
        {
            dashCardTitle.text = isVi ? "2. LƯỚT TỐC BIẾN & NÉ ĐÒN (DASH)" : "2. DASH & DODGE";
        }
        if (dashCardDesc != null)
        {
            dashCardDesc.text = isVi
                ? $"- <b>Tiêu hao:</b> <color=#38BDF8><b>20 NL</b></color> | <b>Hồi chiêu:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color> [Phím Space / Nút Lướt].\n" +
                  $"- <b>Sát thương:</b> <color=#FFFF66><b>0 DMG</b></color> (Kỹ năng né đòn thuần túy).\n" +
                  $"- <b>Đặc tính:</b> Lướt tốc biến 12 m/s theo hướng di chuyển. <color=#55FF88><b>Bất tử hoàn toàn trong 0.2s</b></color> để né mọi đòn đánh."
                : $"- <b>Cost:</b> <color=#38BDF8><b>20 Energy</b></color> | <b>Cooldown:</b> <color=#55DDFF><b>{finalDashCd:F1}s</b></color> [Space / Dash Button].\n" +
                  $"- <b>Damage:</b> <color=#FFFF66><b>0 DMG</b></color> (Pure evasion skill).\n" +
                  $"- <b>Trait:</b> Dash at 12 m/s in movement direction. <color=#55FF88><b>Invincible for 0.2s</b></color> dodging all attacks.";
        }

        // Card 3: Ulti
        if (ultiCardTitle != null)
        {
            ultiCardTitle.text = isMaid 
                ? (isVi ? "3. TUYỆT KỸ: QUÉT TẬN KẺ YẾU" : "3. ULTIMATE: SWEEP THE WEAK") 
                : (isVi ? "3. TUYỆT KỸ: CƠN LỐC TÀN TÍCH" : "3. ULTIMATE: RUIN WHIRLWIND");
        }
        if (ultiCardDesc != null)
        {
            if (isMaid)
            {
                ultiCardDesc.text = isVi
                    ? $"- <b>Tiêu hao:</b> <color=#38BDF8><b>50 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color> [Phím F / Nút Ulti].\n" +
                      $"- <b>Điện ảnh:</b> Ngưng đọng thời gian slow-mo, camera cận cảnh rực rỡ!\n" +
                      $"- <b>Hiệu ứng:</b> Vung đao khí trăng khuyết góc rộng 140 độ bán kính 4.8m:\n" +
                      $"  - <color=#FF4455><b>Quái thường:</b> KẾT LIỄU NGAY LẬP TỨC (Insta-Kill)</color>.\n" +
                      $"  - <color=#FFCC33><b>Boss:</b> Gây 250 DMG (+{ultiDmgBonus}) và LÀM CHOÁNG BOSS 2.5 GIÂY</color>!"
                    : $"- <b>Cost:</b> <color=#38BDF8><b>50 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color> [Key F / Ulti Button].\n" +
                      $"- <b>Cinematic:</b> Slow-motion camera zoom with glowing flash!\n" +
                      $"- <b>Effect:</b> Slashes wide crescent energy blade across 4.8m:\n" +
                      $"  - <color=#FF4455><b>Normal Mobs:</b> INSTANT EXECUTION (Insta-Kill)</color>.\n" +
                      $"  - <color=#FFCC33><b>Boss:</b> Deals 250 DMG (+{ultiDmgBonus}) & STUNS FOR 2.5s</color>!";
            }
            else
            {
                ultiCardDesc.text = isVi
                    ? $"- <b>Tiêu hao:</b> <color=#38BDF8><b>50 NL</b></color> | <b>Hồi chiêu:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color> [Phím F / Nút Ulti].\n" +
                      $"- <b>Hiệu ứng:</b> Vận nội lực triệu hồi Cơn Bão Kiếm bán kính 2.8m quét 4 đợt liên tục.\n" +
                      $"- <b>Sát thương:</b> Hút quái xung quanh vào tâm bão, dồn sát thương diện rộng uy lực!"
                    : $"- <b>Cost:</b> <color=#38BDF8><b>50 Energy</b></color> | <b>Cooldown:</b> <color=#FFAA33><b>{finalUltiCd:F1}s</b></color> [Key F / Ulti Button].\n" +
                      $"- <b>Effect:</b> Summons ancient Ruin Whirlwind in 2.8m radius striking 4 times.\n" +
                      $"- <b>Damage:</b> Pulls nearby enemies to storm center dealing massive AOE damage!";
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

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "INFO";
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.4f, 0.8f, 1f);
            tmp.raycastTarget = false;
        }

        Transform existingPanel = rootCanvas.transform.Find("CharacterStatsPanel");
        if (existingPanel != null)
        {
            var comp = existingPanel.GetComponent<CharacterStatsUI>();
            if (comp != null) return comp;
        }

        // Tạo Panel Character Stats
        GameObject panelObj = CreateUIObject("CharacterStatsPanel", rootCanvas.transform);
        RectTransform pRt = panelObj.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.5f, 0.5f);
        pRt.anchorMax = new Vector2(0.5f, 0.5f);
        pRt.pivot = new Vector2(0.5f, 0.5f);
        pRt.anchoredPosition = Vector2.zero;
        pRt.sizeDelta = new Vector2(850f, 540f);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.08f, 0.13f, 0.98f);

        var pOutline = panelObj.AddComponent<Outline>();
        pOutline.effectColor = new Color(0.35f, 0.75f, 1f, 0.85f);
        pOutline.effectDistance = new Vector2(2f, -2f);

        var uiComp = panelObj.AddComponent<CharacterStatsUI>();

        // Header Title
        GameObject headerObj = CreateUIObject("HeaderTitle", panelObj.transform);
        RectTransform hRt = headerObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = new Vector2(0f, -15f);
        hRt.sizeDelta = new Vector2(-40f, 35f);

        uiComp.characterTitleText = headerObj.AddComponent<TextMeshProUGUI>();
        uiComp.characterTitleText.fontSize = 22;
        uiComp.characterTitleText.fontStyle = FontStyles.Bold;
        uiComp.characterTitleText.alignment = TextAlignmentOptions.Center;
        uiComp.characterTitleText.color = new Color(1f, 0.85f, 0.35f);

        // Subtitle
        GameObject subObj = CreateUIObject("HeaderSubtitle", panelObj.transform);
        RectTransform sRt = subObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 1f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0f, -48f);
        sRt.sizeDelta = new Vector2(-40f, 24f);

        uiComp.characterSubtitleText = subObj.AddComponent<TextMeshProUGUI>();
        uiComp.characterSubtitleText.fontSize = 13;
        uiComp.characterSubtitleText.alignment = TextAlignmentOptions.Center;
        uiComp.characterSubtitleText.color = new Color(0.6f, 0.85f, 1f);

        // 4 Badges Container
        GameObject badgesRow = CreateUIObject("BadgesRow", panelObj.transform);
        RectTransform bRowRt = badgesRow.GetComponent<RectTransform>();
        bRowRt.anchorMin = new Vector2(0.5f, 1f);
        bRowRt.anchorMax = new Vector2(0.5f, 1f);
        bRowRt.pivot = new Vector2(0.5f, 1f);
        bRowRt.anchoredPosition = new Vector2(0f, -80f);
        bRowRt.sizeDelta = new Vector2(810f, 50f);

        float bWidth = 195f;
        float bSpace = 10f;
        uiComp.hpStatText = CreateMiniBadge(badgesRow.transform, 0, bWidth, bSpace, new Color(0.12f, 0.28f, 0.18f, 0.95f));
        uiComp.atkStatText = CreateMiniBadge(badgesRow.transform, 1, bWidth, bSpace, new Color(0.32f, 0.14f, 0.16f, 0.95f));
        uiComp.dashCdStatText = CreateMiniBadge(badgesRow.transform, 2, bWidth, bSpace, new Color(0.12f, 0.22f, 0.32f, 0.95f));
        uiComp.ultiCdStatText = CreateMiniBadge(badgesRow.transform, 3, bWidth, bSpace, new Color(0.32f, 0.24f, 0.12f, 0.95f));

        // 3 Cards
        CreateSkillCard(panelObj.transform, "Card_Attack", -145f, 110f, "1. BASIC ATTACK", out uiComp.attackCardDesc, out uiComp.attackCardTitle);
        CreateSkillCard(panelObj.transform, "Card_Dash", -265f, 105f, "2. DASH", out uiComp.dashCardDesc, out uiComp.dashCardTitle);
        CreateSkillCard(panelObj.transform, "Card_Ulti", -380f, 115f, "3. ULTIMATE", out uiComp.ultiCardDesc, out uiComp.ultiCardTitle);

        // Close Button
        GameObject closeBtnObj = CreateUIObject("Btn_Close", panelObj.transform);
        RectTransform cRt = closeBtnObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-10f, -10f);
        cRt.sizeDelta = new Vector2(35f, 35f);

        Image cImg = closeBtnObj.AddComponent<Image>();
        cImg.color = new Color(0.3f, 0.12f, 0.14f, 0.9f);

        Button cBtn = closeBtnObj.AddComponent<Button>();
        cBtn.targetGraphic = cImg;
        cBtn.onClick.AddListener(uiComp.Close);

        GameObject cTxtObj = CreateUIObject("Text", closeBtnObj.transform);
        RectTransform ctRt = cTxtObj.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero;
        ctRt.anchorMax = Vector2.one;
        ctRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI cTmp = cTxtObj.AddComponent<TextMeshProUGUI>();
        cTmp.text = "X";
        cTmp.fontSize = 18;
        cTmp.fontStyle = FontStyles.Bold;
        cTmp.alignment = TextAlignmentOptions.Center;
        cTmp.color = Color.white;

        panelObj.SetActive(false);
        return uiComp;
    }

    private static TextMeshProUGUI CreateMiniBadge(Transform parent, int index, float width, float spacing, Color bgColor)
    {
        GameObject badgeObj = CreateUIObject($"Badge_{index}", parent);
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
