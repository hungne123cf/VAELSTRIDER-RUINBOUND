using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Long-Press Skill Tooltip Popup (Phong cách Đấu Trường Chân Lý TFT / LMHT)
/// 
/// Khi người chơi ấn giữ nút chiêu trên màn hình cảm ứng:
/// - Xuất hiện Popup Thẻ Kỹ Năng chi tiết: Tên chiêu, Loại chiêu, Hồi chiêu, Mô tả cơ chế.
/// - Phân rã Công thức Sát Thương thời gian thực với Icon trực quan:
///   Sát thương: {tổng} ( ⚔ {fromAtk} + ✦ {fromAP} )
///   Trong đó:
///   ⚔ Kiếm (Vàng/Cam): Điểm sát thương đóng góp từ Công Vật Lý (ATK).
///   ✦ Ngôi sao (Xanh lam ngọc): Điểm sát thương đóng góp từ Sức Mạnh Phép (AP).
/// - Khi người chơi nhấc ngón tay ra, Popup tự động đóng lại mượt mà.
/// </summary>
public class SkillTooltipPopupUI : MonoBehaviour
{
    public static SkillTooltipPopupUI Instance { get; private set; }

    private CanvasGroup canvasGroup;
    private RectTransform panelRt;

    // UI Elements
    private Image iconImg;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI categoryText;
    private TextMeshProUGUI cooldownText;
    private TextMeshProUGUI descText;
    private TextMeshProUGUI damageFormulaText;
    private TextMeshProUGUI scalingBreakdownText;

    private Coroutine fadeRoutine;
    private bool isVisible = false;

    public static SkillTooltipPopupUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        var existing = Object.FindAnyObjectByType<SkillTooltipPopupUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null)
        {
            GameObject cObj = GameObject.Find("Canvas");
            if (cObj != null) canvas = cObj.GetComponent<Canvas>();
        }

        if (canvas == null) return null;

        GameObject root = new GameObject("SkillTooltipPopupUI", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);

        var ui = root.AddComponent<SkillTooltipPopupUI>();
        ui.BuildUIStructure(root, canvas);
        Instance = ui;
        return ui;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildUIStructure(GameObject root, Canvas canvas)
    {
        panelRt = root.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1f, 0f);
        panelRt.anchorMax = new Vector2(1f, 0f);
        panelRt.pivot = new Vector2(1f, 0f);
        panelRt.sizeDelta = new Vector2(430f, 270f);
        panelRt.anchoredPosition = new Vector2(-40f, 380f); // Nằm ngay phía trên cụm nút bên phải

        canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 1. Nền thẻ bài Dark Fantasy sang trọng (Màu đá than chì #12141A, viền vàng đồng)
        var bgImg = root.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.11f, 0.96f);

        var outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.70f, 0.35f, 0.95f); // Viền vàng hoàng kim TFT
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/VaelstriderFont") 
            ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
            ?? TMP_Settings.defaultFontAsset;

        // 2. Header Container (Icon + Tên + Hồi Chiêu)
        GameObject headerObj = new GameObject("Header", typeof(RectTransform));
        headerObj.transform.SetParent(root.transform, false);
        var hRt = headerObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.sizeDelta = new Vector2(-20f, 65f);
        hRt.anchoredPosition = new Vector2(0f, -10f);

        // Icon Kỹ Năng
        GameObject iconObj = new GameObject("SkillIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(headerObj.transform, false);
        var iRt = iconObj.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0f, 0.5f);
        iRt.anchorMax = new Vector2(0f, 0.5f);
        iRt.pivot = new Vector2(0f, 0.5f);
        iRt.sizeDelta = new Vector2(50f, 50f);
        iRt.anchoredPosition = new Vector2(5f, 0f);

        iconImg = iconObj.GetComponent<Image>();
        iconImg.preserveAspect = true;
        var iconOutline = iconObj.AddComponent<Outline>();
        iconOutline.effectColor = new Color(0.95f, 0.78f, 0.38f, 0.85f);
        iconOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Tên Kỹ Năng (Vàng kim đậm nét)
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
        titleObj.transform.SetParent(headerObj.transform, false);
        var tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0f, 1f);
        tRt.sizeDelta = new Vector2(-65f, 28f);
        tRt.anchoredPosition = new Vector2(65f, 0f);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (font != null) titleText.font = font;
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.88f, 0.35f);
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.text = "TÊN CHIÊU THỨC";

        // Category & Cooldown Row
        GameObject subRowObj = new GameObject("SubRow", typeof(RectTransform));
        subRowObj.transform.SetParent(headerObj.transform, false);
        var sRt = subRowObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(1f, 0f);
        sRt.pivot = new Vector2(0f, 0f);
        sRt.sizeDelta = new Vector2(-65f, 22f);
        sRt.anchoredPosition = new Vector2(65f, 5f);

        categoryText = subRowObj.AddComponent<TextMeshProUGUI>();
        if (font != null) categoryText.font = font;
        categoryText.fontSize = 13;
        categoryText.fontStyle = FontStyles.Bold;
        categoryText.color = new Color(0.4f, 0.85f, 1f);
        categoryText.alignment = TextAlignmentOptions.Left;
        categoryText.text = "[KÍCH HOẠT]";

        // 3. Đường kẻ ngang ngăn cách (Gold separator line)
        GameObject sepObj = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        sepObj.transform.SetParent(root.transform, false);
        var sepRt = sepObj.GetComponent<RectTransform>();
        sepRt.anchorMin = new Vector2(0f, 1f);
        sepRt.anchorMax = new Vector2(1f, 1f);
        sepRt.pivot = new Vector2(0.5f, 1f);
        sepRt.sizeDelta = new Vector2(-24f, 1.5f);
        sepRt.anchoredPosition = new Vector2(0f, -80f);
        sepObj.GetComponent<Image>().color = new Color(0.85f, 0.70f, 0.35f, 0.45f);

        // 4. Mô tả kỹ năng
        GameObject descObj = new GameObject("DescText", typeof(RectTransform));
        descObj.transform.SetParent(root.transform, false);
        var dRt = descObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 1f);
        dRt.anchorMax = new Vector2(1f, 1f);
        dRt.pivot = new Vector2(0f, 1f);
        dRt.sizeDelta = new Vector2(-24f, 75f);
        dRt.anchoredPosition = new Vector2(12f, -90f);

        descText = descObj.AddComponent<TextMeshProUGUI>();
        if (font != null) descText.font = font;
        descText.fontSize = 13.5f;
        descText.color = new Color(0.85f, 0.87f, 0.90f);
        descText.lineSpacing = 4f;
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.text = "Mô tả chi tiết hiệu ứng của kỹ năng.";

        // 5. Khung Sát Thương Phân Rã Thời Gian Thực (Dual-Scaling Realtime Box)
        GameObject formulaBoxObj = new GameObject("FormulaBox", typeof(RectTransform), typeof(Image));
        formulaBoxObj.transform.SetParent(root.transform, false);
        var fRt = formulaBoxObj.GetComponent<RectTransform>();
        fRt.anchorMin = new Vector2(0f, 0f);
        fRt.anchorMax = new Vector2(1f, 0f);
        fRt.pivot = new Vector2(0.5f, 0f);
        fRt.sizeDelta = new Vector2(-20f, 75f);
        fRt.anchoredPosition = new Vector2(0f, 10f);

        var fBg = formulaBoxObj.GetComponent<Image>();
        fBg.color = new Color(0.03f, 0.04f, 0.06f, 0.85f);

        var fOutline = formulaBoxObj.AddComponent<Outline>();
        fOutline.effectColor = new Color(0.40f, 0.35f, 0.25f, 0.65f);
        fOutline.effectDistance = new Vector2(1f, -1f);

        // Dòng Tổng Sát Thương
        GameObject dmgTotalObj = new GameObject("TotalDamageText", typeof(RectTransform));
        dmgTotalObj.transform.SetParent(formulaBoxObj.transform, false);
        var dtRt = dmgTotalObj.GetComponent<RectTransform>();
        dtRt.anchorMin = new Vector2(0f, 1f);
        dtRt.anchorMax = new Vector2(1f, 1f);
        dtRt.pivot = new Vector2(0f, 1f);
        dtRt.sizeDelta = new Vector2(-16f, 26f);
        dtRt.anchoredPosition = new Vector2(10f, -4f);

        damageFormulaText = dmgTotalObj.AddComponent<TextMeshProUGUI>();
        if (font != null) damageFormulaText.font = font;
        damageFormulaText.fontSize = 15;
        damageFormulaText.fontStyle = FontStyles.Bold;
        damageFormulaText.alignment = TextAlignmentOptions.Left;
        damageFormulaText.text = "Sát thương: <color=#FFFFFF>45</color>";

        // Dòng Chi Tiết Phân Rã Icon (⚔ ATK + ✦ AP)
        GameObject breakdownObj = new GameObject("BreakdownText", typeof(RectTransform));
        breakdownObj.transform.SetParent(formulaBoxObj.transform, false);
        var bdRt = breakdownObj.GetComponent<RectTransform>();
        bdRt.anchorMin = new Vector2(0f, 0f);
        bdRt.anchorMax = new Vector2(1f, 0f);
        bdRt.pivot = new Vector2(0f, 0f);
        bdRt.sizeDelta = new Vector2(-16f, 38f);
        bdRt.anchoredPosition = new Vector2(10f, 6f);

        scalingBreakdownText = breakdownObj.AddComponent<TextMeshProUGUI>();
        if (font != null) scalingBreakdownText.font = font;
        scalingBreakdownText.fontSize = 13.5f;
        scalingBreakdownText.fontStyle = FontStyles.Bold;
        scalingBreakdownText.alignment = TextAlignmentOptions.Left;
        scalingBreakdownText.text = "<color=#FFA500>⚔ 30</color> (100% ATK) + <color=#4DEEEA>✦ 15</color> (35% AP)";
    }

    /// <summary>
    /// Hiển thị Tooltip Popup tương ứng với nút kỹ năng được nhấn giữ.
    /// </summary>
    public void ShowTooltip(MobileActionButton.ButtonType type, Vector2 buttonScreenPos)
    {
        var playerCtrl = Object.FindAnyObjectByType<PlayerController>();
        var combat = (playerCtrl != null) ? playerCtrl.GetComponent<PlayerCombat>() : Object.FindAnyObjectByType<PlayerCombat>();
        if (combat == null) return;

        HeroSkillDamageCalculator.SkillInfo info = null;
        if (type == MobileActionButton.ButtonType.Attack)
        {
            info = HeroSkillDamageCalculator.GetAttackSkillInfo(combat);
        }
        else if (type == MobileActionButton.ButtonType.Dash)
        {
            info = HeroSkillDamageCalculator.GetDashSkillInfo(playerCtrl, combat);
        }
        else if (type == MobileActionButton.ButtonType.Skill)
        {
            info = HeroSkillDamageCalculator.GetSpecialSkillInfo(combat);
        }

        if (info == null) return;

        // Cập nhật nội dung Text
        if (titleText != null) titleText.text = info.skillName;
        if (categoryText != null) categoryText.text = $"{info.skillCategory}  •  {info.cooldownText}";
        if (descText != null) descText.text = info.description;

        string dmgTypeStr = info.damageType == DamageType.Magic 
            ? LocalizationManager.T("Sát thương Phép", "Magic Damage")
            : LocalizationManager.T("Sát thương Vật Lý", "Physical Damage");

        string colorHex = info.damageType == DamageType.Magic ? "#4DEEEA" : "#FFA500";

        if (damageFormulaText != null)
        {
            damageFormulaText.text = $"{dmgTypeStr}: <color={colorHex}><b>{info.totalDamage:N0}</b></color>";
        }

        if (scalingBreakdownText != null)
        {
            string atkPart = $"<color=#FFA500>⚔ {info.fromAtk}</color> ({Mathf.RoundToInt(info.atkRatio * 100)}%)";
            string apPart = $"<color=#4DEEEA>✦ {info.fromAP}</color> ({Mathf.RoundToInt(info.apRatio * 100)}%)";
            scalingBreakdownText.text = $"{LocalizationManager.T("Cấu thành", "Scaling")}: Base {info.baseDamage} + {atkPart} + {apPart}";
        }

        // Lấy Sprite icon của nút hành động
        var mobileBtns = Object.FindObjectsByType<MobileActionButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var mb in mobileBtns)
        {
            if (mb != null && mb.GetButtonType() == type)
            {
                var img = mb.GetComponent<Image>();
                if (img != null && img.sprite != null && iconImg != null)
                {
                    iconImg.sprite = img.sprite;
                    break;
                }
            }
        }

        // Điều chỉnh vị trí popup linh hoạt theo vị trí nút
        if (panelRt != null)
        {
            // Mặc định nằm ở góc phải phía trên cụm nút để ngón tay không bị che
            panelRt.anchoredPosition = new Vector2(-35f, 400f);
        }

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(1f));
        isVisible = true;
    }

    /// <summary>
    /// Đóng Popup khi người chơi nhấc tay ra khỏi nút.
    /// </summary>
    public void HideTooltip()
    {
        if (!isVisible) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(0f));
        isVisible = false;
    }

    private System.Collections.IEnumerator FadeRoutine(float targetAlpha)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        float duration = 0.12f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        fadeRoutine = null;
    }
}
