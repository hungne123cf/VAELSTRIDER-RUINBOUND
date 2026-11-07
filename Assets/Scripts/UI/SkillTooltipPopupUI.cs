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
        panelRt.sizeDelta = new Vector2(440f, 185f);
        panelRt.anchoredPosition = new Vector2(-50f, -180f);

        canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 1. Nền thẻ bài Dark Fantasy sang trọng (Màu đá than chì #101217 viền vàng đồng)
        var bgImg = root.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.11f, 0.96f);

        var outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.70f, 0.35f, 0.95f); // Viền vàng hoàng kim TFT
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/VaelstriderFont") 
            ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
            ?? TMP_Settings.defaultFontAsset;

        TMP_SpriteAsset spriteAsset = VaelstriderSpriteAssetFactory.GetOrCreateSpriteAsset();

        // 2. Header Container (Icon Chiêu + Tên Chiêu + Hồi Chiêu)
        GameObject headerObj = new GameObject("Header", typeof(RectTransform));
        headerObj.transform.SetParent(root.transform, false);
        var hRt = headerObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.sizeDelta = new Vector2(-24f, 54f);
        hRt.anchoredPosition = new Vector2(0f, -8f);

        // Icon Kỹ Năng
        GameObject iconObj = new GameObject("SkillIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(headerObj.transform, false);
        var iRt = iconObj.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0f, 0.5f);
        iRt.anchorMax = new Vector2(0f, 0.5f);
        iRt.pivot = new Vector2(0f, 0.5f);
        iRt.sizeDelta = new Vector2(46f, 46f);
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
        tRt.sizeDelta = new Vector2(-60f, 25f);
        tRt.anchoredPosition = new Vector2(58f, -2f);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (font != null) titleText.font = font;
        titleText.fontSize = 17.5f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.88f, 0.35f);
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.text = "TÊN CHIÊU THỨC";

        // Category & Cooldown Row (Loại Chiêu • Tiêu Hao • Hồi Chiêu)
        GameObject subRowObj = new GameObject("SubRow", typeof(RectTransform));
        subRowObj.transform.SetParent(headerObj.transform, false);
        var sRt = subRowObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(1f, 0f);
        sRt.pivot = new Vector2(0f, 0f);
        sRt.sizeDelta = new Vector2(-60f, 22f);
        sRt.anchoredPosition = new Vector2(58f, 3f);

        categoryText = subRowObj.AddComponent<TextMeshProUGUI>();
        if (font != null) categoryText.font = font;
        if (spriteAsset != null) categoryText.spriteAsset = spriteAsset;
        categoryText.fontSize = 12.5f;
        categoryText.fontStyle = FontStyles.Bold;
        categoryText.color = new Color(0.4f, 0.85f, 1f);
        categoryText.alignment = TextAlignmentOptions.Left;
        categoryText.text = "[KÍCH HOẠT]  •  Tiêu hao: 0  •  Hồi chiêu: 0s";

        // 3. Đường kẻ ngang phân cách tinh tế
        GameObject sepObj = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        sepObj.transform.SetParent(root.transform, false);
        var sepRt = sepObj.GetComponent<RectTransform>();
        sepRt.anchorMin = new Vector2(0f, 1f);
        sepRt.anchorMax = new Vector2(1f, 1f);
        sepRt.pivot = new Vector2(0.5f, 1f);
        sepRt.sizeDelta = new Vector2(-24f, 1.5f);
        sepRt.anchoredPosition = new Vector2(0f, -68f);
        sepObj.GetComponent<Image>().color = new Color(0.85f, 0.70f, 0.35f, 0.45f);

        // 4. Mô tả kỹ năng gộp chung với sát thương (Liền mạch chuẩn TFT)
        GameObject descObj = new GameObject("DescText", typeof(RectTransform));
        descObj.transform.SetParent(root.transform, false);
        var dRt = descObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 1f);
        dRt.anchorMax = new Vector2(1f, 1f);
        dRt.pivot = new Vector2(0f, 1f);
        dRt.sizeDelta = new Vector2(-28f, 100f);
        dRt.anchoredPosition = new Vector2(14f, -76f);

        descText = descObj.AddComponent<TextMeshProUGUI>();
        if (font != null) descText.font = font;
        if (spriteAsset != null) descText.spriteAsset = spriteAsset;
        descText.fontSize = 13.5f;
        descText.color = new Color(0.88f, 0.90f, 0.94f);
        descText.lineSpacing = 5f;
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.enableWordWrapping = true;
        descText.overflowMode = TextOverflowModes.Overflow;
        descText.text = "Mô tả chi tiết kỹ năng và lượng sát thương gây ra.";
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
            categoryText.text = $"<color=#38BDF8>[{info.skillCategory}]</color>  •  {info.energyCostText}  •  {info.cooldownText}";
        }

        if (descText != null)
        {
            descText.text = info.description;
            // Tự động co giãn chiều cao thẻ bài theo độ dài mô tả để layout luôn vừa vặn không thừa thãi
            Canvas.ForceUpdateCanvases();
            float textH = descText.preferredHeight;
            float targetH = Mathf.Max(180f, textH + 92f);
            if (panelRt != null)
            {
                panelRt.sizeDelta = new Vector2(440f, targetH);
                panelRt.anchoredPosition = new Vector2(-50f, -180f);
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
