using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Long-Press Skill Tooltip Popup (Phong cách Đấu Trường Chân Lý TFT / LMHT)
/// 
/// Khi người chơi ấn giữ nút chiêu trên màn hình cảm ứng:
/// - Xuất hiện Popup Thẻ Kỹ Năng chi tiết: Tên chiêu, Icon kỹ năng thật, Loại chiêu, Tiêu hao năng lượng/máu, Hồi chiêu.
/// - Sát thương và công thức phân rã được gộp chung lên TRÊN cùng hàng với mô tả, không tách riêng 2 hàng ở dưới đáy.
/// - Không còn phần chia cấp độ [ 30 / 45 / 70 ] vì game không có level chiêu.
/// - Tự động hiển thị chính xác tiêu hao cố định (50 Năng lượng), trên giây (15/s), hoặc dùng HP (20% Máu tối đa).
/// - Sử dụng TMP_SpriteAsset pixel art sắc nét: <sprite name="atk">, <sprite name="ap">, <sprite name="mana">, <sprite name="hp">,
///   triệt tiêu 100% lỗi font chữ ô vuông [□]!
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
        if (panelRt == null) panelRt = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildUIStructure(GameObject root, Canvas canvas)
    {
        panelRt = root.GetComponent<RectTransform>();
        // Neo ở giữa phía trên màn hình (đỉnh cao thoáng, tuyệt đối không bị ngón tay cái và nút bánh răng hay nút hủy chiêu che khuất)
        panelRt.anchorMin = new Vector2(0.5f, 1f);
        panelRt.anchorMax = new Vector2(0.5f, 1f);
        panelRt.pivot = new Vector2(0.5f, 1f);
        panelRt.sizeDelta = new Vector2(1100f, 320f);
        panelRt.anchoredPosition = new Vector2(0f, -55f);

        canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 1. Nền thẻ bài Dark Fantasy sang trọng (Màu đá than chì #101217 viền vàng đồng sliced)
        var bgImg = root.AddComponent<Image>();
        bgImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);

        var outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.82f, 0.40f, 0.98f); // Viền vàng hoàng kim TFT
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        TMP_FontAsset font = GameFontManager.BodyFont;
        TMP_FontAsset headerFont = GameFontManager.HeaderFont;

        TMP_SpriteAsset spriteAsset = VaelstriderSpriteAssetFactory.GetOrCreateSpriteAsset();

        // 2. Header Container (Icon Chiêu 96x96 + Tên Chiêu 36pt + Hồi Chiêu 24pt)
        GameObject headerObj = new GameObject("Header", typeof(RectTransform));
        headerObj.transform.SetParent(root.transform, false);
        var hRt = headerObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.sizeDelta = new Vector2(-44f, 105f);
        hRt.anchoredPosition = new Vector2(0f, -14f);

        // Icon Kỹ Năng (Phóng to sắc nét 96x96 cho điện thoại)
        GameObject iconObj = new GameObject("SkillIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(headerObj.transform, false);
        var iRt = iconObj.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0f, 0.5f);
        iRt.anchorMax = new Vector2(0f, 0.5f);
        iRt.pivot = new Vector2(0f, 0.5f);
        iRt.sizeDelta = new Vector2(96f, 96f);
        iRt.anchoredPosition = new Vector2(8f, 0f);

        iconImg = iconObj.GetComponent<Image>();
        iconImg.preserveAspect = true;
        var iconOutline = iconObj.AddComponent<Outline>();
        iconOutline.effectColor = new Color(0.98f, 0.85f, 0.42f, 0.95f);
        iconOutline.effectDistance = new Vector2(2f, -2f);

        // Tên Kỹ Năng (Vàng kim đậm nét, tăng kích cỡ lên 36pt rõ ràng trên mobile)
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
        titleObj.transform.SetParent(headerObj.transform, false);
        var tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0f, 1f);
        tRt.sizeDelta = new Vector2(-128f, 48f);
        tRt.anchoredPosition = new Vector2(120f, -4f);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (headerFont != null) titleText.font = headerFont;
        titleText.fontSize = 36f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.88f, 0.45f);
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.text = "TÊN CHIÊU THỨC";
        SoulslikeHUDStyler.SetSafeOutline(titleText, 0.25f, Color.black);

        // Category & Cooldown Row (Loại Chiêu • Tiêu Hao • Hồi Chiêu, tăng lên 24pt rõ nét)
        GameObject subRowObj = new GameObject("SubRow", typeof(RectTransform));
        subRowObj.transform.SetParent(headerObj.transform, false);
        var sRt = subRowObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(1f, 0f);
        sRt.pivot = new Vector2(0f, 0f);
        sRt.sizeDelta = new Vector2(-128f, 40f);
        sRt.anchoredPosition = new Vector2(120f, 6f);

        categoryText = subRowObj.AddComponent<TextMeshProUGUI>();
        if (font != null) categoryText.font = font;
        if (spriteAsset != null) categoryText.spriteAsset = spriteAsset;
        categoryText.fontSize = 24f;
        categoryText.fontStyle = FontStyles.Bold;
        categoryText.color = new Color(0.4f, 0.85f, 1f);
        categoryText.alignment = TextAlignmentOptions.Left;
        categoryText.text = "◆ KÍCH HOẠT ◆  •  Tiêu hao: 0  •  Hồi chiêu: 0s";
        SoulslikeHUDStyler.SetSafeOutline(categoryText, 0.2f, Color.black);

        // 3. Đường kẻ ngang phân cách tinh tế
        GameObject sepObj = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        sepObj.transform.SetParent(root.transform, false);
        var sepRt = sepObj.GetComponent<RectTransform>();
        sepRt.anchorMin = new Vector2(0f, 1f);
        sepRt.anchorMax = new Vector2(1f, 1f);
        sepRt.pivot = new Vector2(0.5f, 1f);
        sepRt.sizeDelta = new Vector2(-44f, 3f);
        sepRt.anchoredPosition = new Vector2(0f, -126f);
        sepObj.GetComponent<Image>().color = new Color(0.92f, 0.78f, 0.42f, 0.65f);

        // 4. Mô tả kỹ năng gộp chung với sát thương (Tăng font size lên 28pt Bold, khoảng cách dòng thoáng đãng)
        GameObject descObj = new GameObject("DescText", typeof(RectTransform));
        descObj.transform.SetParent(root.transform, false);
        var dRt = descObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 1f);
        dRt.anchorMax = new Vector2(1f, 1f);
        dRt.pivot = new Vector2(0f, 1f);
        dRt.sizeDelta = new Vector2(-50f, 160f);
        dRt.anchoredPosition = new Vector2(25f, -140f);

        descText = descObj.AddComponent<TextMeshProUGUI>();
        if (font != null) descText.font = font;
        if (spriteAsset != null) descText.spriteAsset = spriteAsset;
        descText.fontSize = 28f;
        descText.fontStyle = FontStyles.Bold;
        descText.color = new Color(0.92f, 0.95f, 1f);
        descText.lineSpacing = 14f;
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.enableWordWrapping = true;
        descText.overflowMode = TextOverflowModes.Overflow;
        descText.text = "Mô tả chi tiết kỹ năng và lượng sát thương gây ra.";
        SoulslikeHUDStyler.SetSafeOutline(descText, 0.22f, Color.black);
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

        // Gán Sprite Asset
        var spriteAsset = VaelstriderSpriteAssetFactory.GetOrCreateSpriteAsset();
        if (categoryText != null && categoryText.spriteAsset == null) categoryText.spriteAsset = spriteAsset;
        if (descText != null && descText.spriteAsset == null) descText.spriteAsset = spriteAsset;

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
        if (categoryText != null)
        {
            string catClean = info.skillCategory != null ? info.skillCategory.Replace("[", "").Replace("]", "") : "";
            categoryText.text = $"<color=#38BDF8>◆ {catClean.ToUpper()} ◆</color>  •  {info.energyCostText}  •  {info.cooldownText}";
        }

        if (descText != null)
        {
            // Thay thế dấu [ và ] bằng ( ) để không bao giờ bị font biến thành ký tự Z ô vuông
            string descClean = info.description != null ? info.description.Replace("[", "(").Replace("]", ")") : "";
            descText.text = descClean;

            // Tự động co giãn chiều cao thẻ bài theo độ dài mô tả để layout luôn vừa vặn không thừa thãi
            Canvas.ForceUpdateCanvases();
            float textH = descText.preferredHeight;
            float targetH = Mathf.Max(320f, textH + 165f);
            if (panelRt != null)
            {
                panelRt.sizeDelta = new Vector2(1100f, targetH);
                panelRt.anchoredPosition = new Vector2(0f, -55f);
            }
            if (descText.rectTransform != null)
            {
                descText.rectTransform.sizeDelta = new Vector2(-50f, textH + 20f);
            }
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
