using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Long-Press Skill Tooltip Popup (Phong cách Đấu Trường Chân Lý TFT / LMHT)
/// 
/// Khi người chơi ấn giữ nút chiêu trên màn hình cảm ứng:
/// - Xuất hiện Popup Thẻ Kỹ Năng chi tiết: Tên chiêu, Icon kỹ năng thật, Loại chiêu, Hồi chiêu.
/// - Sát thương được ghi chung liền mạch với phần mô tả kỹ năng, không còn khung tách biệt thô kệch.
/// - Dòng phân tích tỷ lệ sở hữu ICON THẬT (Sprite Pixel Art thanh kiếm ⚔ cam và ngôi sao ✦ xanh lấy trực tiếp từ Bảng Chỉ Số HUD),
///   triệt tiêu hoàn toàn lỗi font chữ ô vuông [□]!
/// - Đặt ở vị trí cao thoáng (nửa trên màn hình) tránh bị ngón tay và nút Hủy Chiêu che khuất.
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
    private TextMeshProUGUI descText;

    // Scaling Row Elements (Dùng Icon Sprite Thực Tế)
    private GameObject scalingRowObj;
    private Image atkIconImg;
    private TextMeshProUGUI atkValText;
    private TextMeshProUGUI plusText;
    private Image apIconImg;
    private TextMeshProUGUI apValText;

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
        // Neo ở góc trên bên phải màn hình (tránh xa ngón tay cái và nút HỦY CHIÊU ở góc dưới)
        panelRt.anchorMin = new Vector2(1f, 1f);
        panelRt.anchorMax = new Vector2(1f, 1f);
        panelRt.pivot = new Vector2(1f, 1f);
        panelRt.sizeDelta = new Vector2(430f, 215f);
        panelRt.anchoredPosition = new Vector2(-50f, -190f);

        canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 1. Nền thẻ bài Dark Fantasy sang trọng (Màu đá than chì #101217 viền vàng đồng)
        var bgImg = root.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.11f, 0.95f);

        var outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.70f, 0.35f, 0.95f); // Viền vàng hoàng kim TFT
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/VaelstriderFont") 
            ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
            ?? TMP_Settings.defaultFontAsset;

        // 2. Header Container (Icon Chiêu + Tên Chiêu + Hồi Chiêu)
        GameObject headerObj = new GameObject("Header", typeof(RectTransform));
        headerObj.transform.SetParent(root.transform, false);
        var hRt = headerObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.sizeDelta = new Vector2(-24f, 56f);
        hRt.anchoredPosition = new Vector2(0f, -10f);

        // Icon Kỹ Năng
        GameObject iconObj = new GameObject("SkillIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(headerObj.transform, false);
        var iRt = iconObj.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0f, 0.5f);
        iRt.anchorMax = new Vector2(0f, 0.5f);
        iRt.pivot = new Vector2(0f, 0.5f);
        iRt.sizeDelta = new Vector2(48f, 48f);
        iRt.anchoredPosition = new Vector2(4f, 0f);

        iconImg = iconObj.GetComponent<Image>();
        iconImg.preserveAspect = true;
        var iconOutline = iconObj.AddComponent<Outline>();
        iconOutline.effectColor = new Color(0.95f, 0.78f, 0.38f, 0.90f);
        iconOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Tên Kỹ Năng (Vàng kim đậm nét)
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
        titleObj.transform.SetParent(headerObj.transform, false);
        var tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0f, 1f);
        tRt.sizeDelta = new Vector2(-60f, 26f);
        tRt.anchoredPosition = new Vector2(62f, -2f);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (font != null) titleText.font = font;
        titleText.fontSize = 17.5f;
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
        sRt.sizeDelta = new Vector2(-60f, 22f);
        sRt.anchoredPosition = new Vector2(62f, 4f);

        categoryText = subRowObj.AddComponent<TextMeshProUGUI>();
        if (font != null) categoryText.font = font;
        categoryText.fontSize = 13f;
        categoryText.fontStyle = FontStyles.Bold;
        categoryText.color = new Color(0.4f, 0.85f, 1f);
        categoryText.alignment = TextAlignmentOptions.Left;
        categoryText.text = "[KÍCH HOẠT]  •  Hồi chiêu: 0s";

        // 3. Đường kẻ ngang phân cách tinh tế
        GameObject sepObj = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        sepObj.transform.SetParent(root.transform, false);
        var sepRt = sepObj.GetComponent<RectTransform>();
        sepRt.anchorMin = new Vector2(0f, 1f);
        sepRt.anchorMax = new Vector2(1f, 1f);
        sepRt.pivot = new Vector2(0.5f, 1f);
        sepRt.sizeDelta = new Vector2(-24f, 1.5f);
        sepRt.anchoredPosition = new Vector2(0f, -72f);
        sepObj.GetComponent<Image>().color = new Color(0.85f, 0.70f, 0.35f, 0.40f);

        // 4. Mô tả kỹ năng gộp chung với sát thương (Liền mạch và thẩm mỹ)
        GameObject descObj = new GameObject("DescText", typeof(RectTransform));
        descObj.transform.SetParent(root.transform, false);
        var dRt = descObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 1f);
        dRt.anchorMax = new Vector2(1f, 1f);
        dRt.pivot = new Vector2(0f, 1f);
        dRt.sizeDelta = new Vector2(-24f, 85f);
        dRt.anchoredPosition = new Vector2(12f, -80f);

        descText = descObj.AddComponent<TextMeshProUGUI>();
        if (font != null) descText.font = font;
        descText.fontSize = 14f;
        descText.color = new Color(0.88f, 0.90f, 0.94f);
        descText.lineSpacing = 4f;
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.text = "Mô tả chi tiết kỹ năng và lượng sát thương gây ra.";

        // 5. Dòng Tỷ Lệ Sát Thương Thời Gian Thực (Chứa ICON THẬT từ Bảng Chỉ Số)
        scalingRowObj = new GameObject("ScalingRow", typeof(RectTransform));
        scalingRowObj.transform.SetParent(root.transform, false);
        var scRt = scalingRowObj.GetComponent<RectTransform>();
        scRt.anchorMin = new Vector2(0f, 0f);
        scRt.anchorMax = new Vector2(1f, 0f);
        scRt.pivot = new Vector2(0f, 0f);
        scRt.sizeDelta = new Vector2(-24f, 32f);
        scRt.anchoredPosition = new Vector2(12f, 10f);

        // Icon Kiếm (ATK) Thực Tế từ PlayerStatPanelUI
        GameObject atkIconObj = new GameObject("AtkIcon", typeof(RectTransform), typeof(Image));
        atkIconObj.transform.SetParent(scalingRowObj.transform, false);
        var aiRt = atkIconObj.GetComponent<RectTransform>();
        aiRt.anchorMin = new Vector2(0f, 0.5f);
        aiRt.anchorMax = new Vector2(0f, 0.5f);
        aiRt.pivot = new Vector2(0f, 0.5f);
        aiRt.sizeDelta = new Vector2(22f, 22f);
        aiRt.anchoredPosition = new Vector2(0f, 0f);
        atkIconImg = atkIconObj.GetComponent<Image>();
        atkIconImg.sprite = PlayerStatPanelUI.GetPixelIcon("⚔", new Color(1f, 0.55f, 0.20f));
        atkIconImg.preserveAspect = true;

        // Giá trị ATK
        GameObject atkValObj = new GameObject("AtkValText", typeof(RectTransform));
        atkValObj.transform.SetParent(scalingRowObj.transform, false);
        var avRt = atkValObj.GetComponent<RectTransform>();
        avRt.anchorMin = new Vector2(0f, 0.5f);
        avRt.anchorMax = new Vector2(0f, 0.5f);
        avRt.pivot = new Vector2(0f, 0.5f);
        avRt.sizeDelta = new Vector2(110f, 26f);
        avRt.anchoredPosition = new Vector2(26f, -1f);
        atkValText = atkValObj.AddComponent<TextMeshProUGUI>();
        if (font != null) atkValText.font = font;
        atkValText.fontSize = 14f;
        atkValText.fontStyle = FontStyles.Bold;
        atkValText.color = new Color(1f, 0.65f, 0.25f);
        atkValText.alignment = TextAlignmentOptions.Left;
        atkValText.text = "18 (100%)";

        // Dấu "+"
        GameObject plusObj = new GameObject("PlusText", typeof(RectTransform));
        plusObj.transform.SetParent(scalingRowObj.transform, false);
        var pRt = plusObj.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0f, 0.5f);
        pRt.anchorMax = new Vector2(0f, 0.5f);
        pRt.pivot = new Vector2(0f, 0.5f);
        pRt.sizeDelta = new Vector2(20f, 26f);
        pRt.anchoredPosition = new Vector2(145f, -1f);
        plusText = plusObj.AddComponent<TextMeshProUGUI>();
        if (font != null) plusText.font = font;
        plusText.fontSize = 14f;
        plusText.fontStyle = FontStyles.Bold;
        plusText.color = new Color(0.7f, 0.75f, 0.85f);
        plusText.alignment = TextAlignmentOptions.Center;
        plusText.text = "+";

        // Icon Ngôi Sao (AP) Thực Tế từ PlayerStatPanelUI
        GameObject apIconObj = new GameObject("ApIcon", typeof(RectTransform), typeof(Image));
        apIconObj.transform.SetParent(scalingRowObj.transform, false);
        var apiRt = apIconObj.GetComponent<RectTransform>();
        apiRt.anchorMin = new Vector2(0f, 0.5f);
        apiRt.anchorMax = new Vector2(0f, 0.5f);
        apiRt.pivot = new Vector2(0f, 0.5f);
        apiRt.sizeDelta = new Vector2(22f, 22f);
        apiRt.anchoredPosition = new Vector2(170f, 0f);
        apIconImg = apIconObj.GetComponent<Image>();
        apIconImg.sprite = PlayerStatPanelUI.GetPixelIcon("✦", new Color(0.35f, 0.75f, 1f));
        apIconImg.preserveAspect = true;

        // Giá trị AP
        GameObject apValObj = new GameObject("ApValText", typeof(RectTransform));
        apValObj.transform.SetParent(scalingRowObj.transform, false);
        var apvRt = apValObj.GetComponent<RectTransform>();
        apvRt.anchorMin = new Vector2(0f, 0.5f);
        apvRt.anchorMax = new Vector2(0f, 0.5f);
        apvRt.pivot = new Vector2(0f, 0.5f);
        apvRt.sizeDelta = new Vector2(120f, 26f);
        apvRt.anchoredPosition = new Vector2(196f, -1f);
        apValText = apValObj.AddComponent<TextMeshProUGUI>();
        if (font != null) apValText.font = font;
        apValText.fontSize = 14f;
        apValText.fontStyle = FontStyles.Bold;
        apValText.color = new Color(0.40f, 0.85f, 1f);
        apValText.alignment = TextAlignmentOptions.Left;
        apValText.text = "0 (35%)";
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

        // Gán Icon Chiêu Thức Thực Tế
        if (iconImg != null)
        {
            if (info.skillIcon != null)
            {
                iconImg.sprite = info.skillIcon;
                iconImg.color = Color.white;
                iconImg.gameObject.SetActive(true);
            }
            else
            {
                string charId = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
                var fallbackIcon = MobileActionButton.GetHeroActionIcon(charId, type);
                if (fallbackIcon != null)
                {
                    iconImg.sprite = fallbackIcon;
                    iconImg.color = Color.white;
                    iconImg.gameObject.SetActive(true);
                }
            }
        }

        // Cập nhật nội dung Text
        if (titleText != null) titleText.text = info.skillName;
        if (categoryText != null) categoryText.text = $"{info.skillCategory}  •  {info.cooldownText}";
        if (descText != null) descText.text = info.description;

        // Cập nhật dòng phân tích tỷ lệ sở hữu ICON THẬT (Image Sprite từ Bảng Chỉ Số)
        if (atkIconImg != null) atkIconImg.sprite = PlayerStatPanelUI.GetPixelIcon("⚔", new Color(1f, 0.55f, 0.20f));
        if (apIconImg != null) apIconImg.sprite = PlayerStatPanelUI.GetPixelIcon("✦", new Color(0.35f, 0.75f, 1f));

        if (atkValText != null)
        {
            atkValText.text = $"{info.fromAtk} ({Mathf.RoundToInt(info.atkRatio * 100)}%)";
        }

        if (apValText != null)
        {
            apValText.text = $"{info.fromAP} ({Mathf.RoundToInt(info.apRatio * 100)}%)";
        }

        if (panelRt != null)
        {
            // Neo ở góc trên bên phải màn hình (tránh xa ngón tay cái và nút HỦY CHIÊU ở góc dưới)
            panelRt.anchoredPosition = new Vector2(-50f, -190f);
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
