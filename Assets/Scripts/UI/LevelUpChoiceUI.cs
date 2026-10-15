using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// UI hiển thị 3 lựa chọn Lõi Nâng Cấp (Augments) khi nhân vật lên cấp.
/// Thiết kế phong cách Dark Neutral / Gothic RPG với khung kim loại chạm khắc cổ điển và dải phân cách chạm khắc.
/// Tự tạo toàn bộ UI bằng code kết hợp 9-sliced frame & divider asset.
/// </summary>
public class LevelUpChoiceUI : MonoBehaviour
{
    // ─── Singleton ─────────────────────────────────────────────────────────────
    private static LevelUpChoiceUI _instance;

    // ─── Runtime ───────────────────────────────────────────────────────────────
    private GameObject _root;
    private bool _isShowing = false;
    private List<AugmentDatabase.AugmentData> _currentChoices;

    // Queue lên cấp liên tiếp (khi giết Boss nhận nhiều EXP)
    private static readonly Queue<int> _pendingLevelUps = new Queue<int>();

    // Cache sprites
    private static Sprite _frameSprite;
    private static Sprite _dividerSprite;

    // ─── Neutral Dark Color Palette ────────────────────────────────────────────
    private static readonly Color ColorOverlayBg      = new Color(0.015f, 0.018f, 0.024f, 0.94f);
    private static readonly Color ColorMainPanelBg    = new Color(0.060f, 0.065f, 0.075f, 0.98f);
    private static readonly Color ColorMainPanelFrame = new Color(0.550f, 0.600f, 0.700f, 0.85f);

    private static readonly Color ColorCardBaseBg     = new Color(0.085f, 0.090f, 0.105f, 0.98f);
    private static readonly Color ColorCardFrameNorm  = new Color(0.600f, 0.650f, 0.750f, 0.85f);
    private static readonly Color ColorCardFrameHover = new Color(1.000f, 1.000f, 1.000f, 1.00f);

    private static readonly Color ColorDividerLine    = new Color(0.500f, 0.550f, 0.650f, 0.55f);

    private static readonly Color ColorBtnNormBg      = new Color(0.140f, 0.150f, 0.180f, 1.00f);
    private static readonly Color ColorBtnHoverBg     = new Color(0.220f, 0.240f, 0.280f, 1.00f);
    private static readonly Color ColorBtnPressedBg   = new Color(0.080f, 0.090f, 0.110f, 1.00f);

    public static void ResetUI()
    {
        _pendingLevelUps.Clear();
        if (_instance != null)
        {
            _instance.StopAllCoroutines();
            _instance._isShowing = false;
            if (_instance._root != null)
            {
                Destroy(_instance._root);
                _instance._root = null;
            }
        }
        GameAutoBootstrap.SetGameplayHUDVisible(true);
    }

    // ─── Static Entry Point ────────────────────────────────────────────────────
    public static void Show(int newLevel)
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("LevelUpChoiceUI");
            _instance = go.AddComponent<LevelUpChoiceUI>();
        }
        _pendingLevelUps.Enqueue(newLevel);
        _instance.ProcessQueue();
    }

    private void ProcessQueue()
    {
        if (_isShowing || _pendingLevelUps.Count == 0) return;
        int level = _pendingLevelUps.Dequeue();
        StartCoroutine(ShowRoutine(level));
    }

    // ─── Core UI coroutine ─────────────────────────────────────────────────────
    private IEnumerator ShowRoutine(int level)
    {
        _isShowing = true;

        // Ẩn toàn bộ Gameplay HUD để không chồng lấn
        GameAutoBootstrap.SetGameplayHUDVisible(false);

        // Pause game time
        float prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // Build UI
        BuildUI(level);
        if (_root != null) _root.SetActive(true);

        SoundManager.EnsureInstance()?.PlayUpgradeSuccess();

        // Chờ người chơi bấm chọn
        while (_isShowing && _root != null && _root.activeSelf)
        {
            yield return null;
        }

        // Khôi phục thời gian
        Time.timeScale = prevTimeScale;

        // Nếu không còn lượt lên cấp chờ nào, hiện lại Gameplay HUD
        if (_pendingLevelUps.Count == 0)
        {
            GameAutoBootstrap.SetGameplayHUDVisible(true);
        }

        // Xử lý lần lên cấp tiếp theo nếu có
        yield return new WaitForSecondsRealtime(0.2f);
        ProcessQueue();
    }

    // ─── Asset Loaders ─────────────────────────────────────────────────────────
    private static Sprite GetFrameSprite()
    {
        if (_frameSprite != null) return _frameSprite;

#if UNITY_EDITOR
        _frameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/frame_gothic_border.png");
        if (_frameSprite != null) return _frameSprite;
#endif

        _frameSprite = Resources.Load<Sprite>("UI/frame_gothic_border");
        if (_frameSprite != null) return _frameSprite;

        string[] candidates = new string[]
        {
            System.IO.Path.Combine(Application.dataPath, "Sprites/UI/frame_gothic_border.png"),
            System.IO.Path.Combine(Application.dataPath, "Resources/UI/frame_gothic_border.png"),
            System.IO.Path.Combine(Application.dataPath, "ART/UI/Default/Border/panel-border-004.png")
        };

        foreach (var path in candidates)
        {
            if (System.IO.File.Exists(path))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    _frameSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(14, 14, 14, 14));
                    break;
                }
            }
        }

        return _frameSprite;
    }

    private static Sprite GetDividerSprite()
    {
        if (_dividerSprite != null) return _dividerSprite;

#if UNITY_EDITOR
        _dividerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/divider_fade_004.png");
        if (_dividerSprite != null) return _dividerSprite;
#endif

        _dividerSprite = Resources.Load<Sprite>("UI/divider_fade_004");
        if (_dividerSprite != null) return _dividerSprite;

        string[] candidates = new string[]
        {
            System.IO.Path.Combine(Application.dataPath, "Sprites/UI/divider_fade_004.png"),
            System.IO.Path.Combine(Application.dataPath, "Resources/UI/divider_fade_004.png"),
            System.IO.Path.Combine(Application.dataPath, "ART/UI/Default/Divider Fade/divider-fade-004.png")
        };

        foreach (var path in candidates)
        {
            if (System.IO.File.Exists(path))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    _dividerSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
                    break;
                }
            }
        }

        return _dividerSprite;
    }

    // ─── Build UI ──────────────────────────────────────────────────────────────
    private void BuildUI(int level)
    {
        if (_root != null) Destroy(_root);

        bool isVietnamese = LocalizationManager.IsVietnamese;
        string heroId = PlayerUpgradeManager.Instance != null
            ? PlayerUpgradeManager.Instance.GetCurrentHeroKey() : "Vaelstrider";

        _currentChoices = AugmentDatabase.GetRandomChoices(heroId, 3);

        // ── Canvas ─────────────────────────────────────────────────────────────
        _root = new GameObject("LevelUpRoot");
        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 25000;
        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        _root.AddComponent<GraphicRaycaster>();
        var cg = _root.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable = true;

        // ── Dark Overlay (Neutral Vignette) ────────────────────────────────────
        var overlay = CreatePanel(_root.transform, ColorOverlayBg, Vector2.zero, Vector2.one);
        overlay.name = "Overlay";

        // ── Main Panel Window (Framed with Gothic Asset) ───────────────────────
        var panel = CreateFramedPanel(overlay.transform, "MainPanel",
            ColorMainPanelBg, ColorMainPanelFrame,
            new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.94f),
            0.35f, out Image panelFrameImg);

        // ── Title Section ──────────────────────────────────────────────────────
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.10f, 0.84f);
        titleRT.anchorMax = new Vector2(0.90f, 0.98f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        string titleStr = isVietnamese ? $"LÊN CẤP {level}" : $"LEVEL UP {level}";
        string subStr   = isVietnamese ? "Chọn 1 nâng cấp để tiếp tục chiến đấu" : "Choose 1 augment to continue";
        titleTMP.text = $"<size=34><b><color=#ECEFF4>{titleStr}</color></b></size>\n<size=17><color=#7E889B>{subStr}</color></size>";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;
        titleTMP.enableWordWrapping = true;

        // Title flanking decorative divider wings
        var tlw = CreateDividerWing(panel.transform, "TitleLeftWing",
            new Vector2(0.24f, 0.912f), new Vector2(0.36f, 0.932f),
            new Color(0.70f, 0.75f, 0.85f, 0.80f), false);

        var trw = CreateDividerWing(panel.transform, "TitleRightWing",
            new Vector2(0.64f, 0.912f), new Vector2(0.76f, 0.932f),
            new Color(0.70f, 0.75f, 0.85f, 0.80f), true);

        // Title bottom symmetric ornamental divider
        CreateOrnamentDivider(panel.transform, "TitleDivider",
            new Vector2(0.18f, 0.825f), new Vector2(0.82f, 0.840f),
            new Color(0.55f, 0.60f, 0.70f, 0.65f));

        // ── 3 Augment Cards (Neutral Dark + Gothic Frame) ──────────────────────
        float cardWidth = 0.285f;
        float startX    = 0.045f;
        float gap       = (1f - startX * 2f - cardWidth * 3f) / 2f;

        for (int i = 0; i < _currentChoices.Count; i++)
        {
            var aug = _currentChoices[i];
            float xMin = startX + i * (cardWidth + gap);
            float xMax = xMin + cardWidth;
            BuildCard(panel.transform, aug, i, isVietnamese,
                new Vector2(xMin, 0.06f), new Vector2(xMax, 0.80f));
        }

        // Đảm bảo viền ngoài của MainPanel luôn hiển thị trên cùng không bị đè
        if (panelFrameImg != null)
        {
            panelFrameImg.transform.SetAsLastSibling();
        }
    }

    private void BuildCard(Transform parent, AugmentDatabase.AugmentData aug, int index,
        bool isVN, Vector2 anchorMin, Vector2 anchorMax)
    {
        // 1. Card Container & Outer Frame
        var card = CreateFramedPanel(parent, $"Card_{index}",
            ColorCardBaseBg, ColorCardFrameNorm,
            anchorMin, anchorMax,
            0.42f, out Image cardFrameImg);

        var cardRT = card.GetComponent<RectTransform>();

        // 2. Rarity Header (No black block overlay, no broken unicode characters!)
        string rarityLabel;
        Color rarityColor;

        switch (aug.rarity)
        {
            case AugmentDatabase.AugmentData.Rarity.Epic:
                rarityLabel = isVN ? "SỬ THI" : "EPIC";
                rarityColor = new Color(0.92f, 0.78f, 0.50f); // Soft amber gold
                break;
            case AugmentDatabase.AugmentData.Rarity.Rare:
                rarityLabel = isVN ? "QUÝ HIẾM" : "RARE";
                rarityColor = new Color(0.55f, 0.80f, 0.98f); // Soft ice silver
                break;
            default:
                rarityLabel = isVN ? "THƯỜNG" : "COMMON";
                rarityColor = new Color(0.72f, 0.77f, 0.85f); // Slate silver
                break;
        }

        var rarityHeader = new GameObject("RarityHeader");
        rarityHeader.transform.SetParent(card.transform, false);
        var rhRT = rarityHeader.AddComponent<RectTransform>();
        rhRT.anchorMin = new Vector2(0.06f, 0.900f);
        rhRT.anchorMax = new Vector2(0.94f, 0.965f);
        rhRT.offsetMin = rhRT.offsetMax = Vector2.zero;

        // Left wing (pointing inward)
        CreateDividerWing(rarityHeader.transform, "LeftWing",
            new Vector2(0f, 0.20f), new Vector2(0.30f, 0.80f),
            new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.75f), false);

        // Center text (clean, no unicode glyph tofu boxes)
        var rarityTxtObj = new GameObject("RarityText");
        rarityTxtObj.transform.SetParent(rarityHeader.transform, false);
        var rarityTxtRT = rarityTxtObj.AddComponent<RectTransform>();
        rarityTxtRT.anchorMin = new Vector2(0.30f, 0f);
        rarityTxtRT.anchorMax = new Vector2(0.70f, 1f);
        rarityTxtRT.offsetMin = rarityTxtRT.offsetMax = Vector2.zero;

        var rarityTxt = rarityTxtObj.AddComponent<TextMeshProUGUI>();
        rarityTxt.text = rarityLabel;
        rarityTxt.color = rarityColor;
        rarityTxt.fontSize = 15;
        rarityTxt.alignment = TextAlignmentOptions.Center;
        rarityTxt.fontStyle = FontStyles.Bold;

        // Right wing (mirrored, pointing inward)
        CreateDividerWing(rarityHeader.transform, "RightWing",
            new Vector2(0.70f, 0.20f), new Vector2(1f, 0.80f),
            new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.75f), true);

        // 3. Stat Emblem Badge (Gothic Framed)
        var emblemObj = CreateFramedPanel(card.transform, "Emblem",
            new Color(0.04f, 0.045f, 0.055f, 1f),
            new Color(0.60f, 0.65f, 0.75f, 0.95f),
            new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f),
            0.72f, out _);

        var emblemRT = emblemObj.GetComponent<RectTransform>();
        emblemRT.sizeDelta = new Vector2(86f, 72f);
        emblemRT.pivot = new Vector2(0.5f, 0.5f);

        var emblemTxtObj = new GameObject("EmblemText");
        emblemTxtObj.transform.SetParent(emblemObj.transform, false);
        var etRT = emblemTxtObj.AddComponent<RectTransform>();
        etRT.anchorMin = Vector2.zero;
        etRT.anchorMax = Vector2.one;
        etRT.offsetMin = new Vector2(4f, 2f);
        etRT.offsetMax = new Vector2(-4f, -2f);

        var etTMP = emblemTxtObj.AddComponent<TextMeshProUGUI>();
        etTMP.text = GetTypeBadgeText(aug.type);
        etTMP.fontSize = 17f;
        etTMP.fontStyle = FontStyles.Bold;
        etTMP.alignment = TextAlignmentOptions.Center;
        etTMP.color = new Color(0.95f, 0.97f, 1f);
        etTMP.enableWordWrapping = false;
        etTMP.enableAutoSizing = true;
        etTMP.fontSizeMin = 11f;
        etTMP.fontSizeMax = 18f;
        etTMP.overflowMode = TextOverflowModes.Ellipsis;

        // 4. Augment Name
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(card.transform, false);
        var nameRT = nameObj.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.06f, 0.57f);
        nameRT.anchorMax = new Vector2(0.94f, 0.69f);
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

        var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = isVN ? aug.nameVN : aug.nameEN;
        nameTMP.fontSize = 20;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color = new Color(0.96f, 0.98f, 1f);
        nameTMP.enableWordWrapping = true;

        // 5. Symmetric Ornamental Divider 1 (Replaces plain flat bar)
        CreateOrnamentDivider(card.transform, "Divider1",
            new Vector2(0.10f, 0.550f), new Vector2(0.90f, 0.564f),
            ColorDividerLine);

        // 6. Augment Description
        string desc = isVN ? aug.descVN : aug.descEN;
        string valStr = aug.value % 1 == 0 ? ((int)aug.value).ToString() : aug.value.ToString("F1");
        desc = desc.Replace("{v}", $"<b><color=#FFFFFF>{valStr}</color></b>");
        if (aug.duration > 0)
        {
            desc = desc.Replace("{d}", $"<b><color=#FFFFFF>{(int)aug.duration}</color></b>");
        }

        var descObj = new GameObject("Desc");
        descObj.transform.SetParent(card.transform, false);
        var descRT = descObj.AddComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0.06f, 0.28f);
        descRT.anchorMax = new Vector2(0.94f, 0.54f);
        descRT.offsetMin = descRT.offsetMax = Vector2.zero;

        var descTMP = descObj.AddComponent<TextMeshProUGUI>();
        descTMP.text = desc;
        descTMP.fontSize = 16;
        descTMP.alignment = TextAlignmentOptions.Center;
        descTMP.color = new Color(0.78f, 0.82f, 0.88f);
        descTMP.enableWordWrapping = true;

        // 7. Symmetric Ornamental Divider 2 (Replaces plain flat bar)
        CreateOrnamentDivider(card.transform, "Divider2",
            new Vector2(0.10f, 0.245f), new Vector2(0.90f, 0.259f),
            ColorDividerLine);

        // 8. Pick Button (Framed Gothic Dark Plate)
        var btnObj = CreateFramedPanel(card.transform, "PickButton",
            ColorBtnNormBg, ColorCardFrameNorm,
            new Vector2(0.10f, 0.06f), new Vector2(0.90f, 0.21f),
            0.65f, out Image btnFrameImg);

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = ColorBtnNormBg;
        colors.highlightedColor = ColorBtnHoverBg;
        colors.pressedColor = ColorBtnPressedBg;
        colors.selectedColor = ColorBtnHoverBg;
        btn.colors = colors;

        var btnTxtObj = new GameObject("BtnText");
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        var btnTxtRT = btnTxtObj.AddComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero;
        btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.offsetMin = btnTxtRT.offsetMax = Vector2.zero;

        var btnTMP = btnTxtObj.AddComponent<TextMeshProUGUI>();
        btnTMP.text = isVN ? "CHỌN" : "PICK";
        btnTMP.fontSize = 21;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = new Color(0.95f, 0.97f, 1f);

        // Click actions: clicking either the button or card executes selection
        var captured = aug;
        btn.onClick.AddListener(() => PickChoice(captured));

        // QUAN TRỌNG: Đưa khung của card lên làm Sibling cuối cùng để KHÔNG BAO GIỜ bị bất kỳ phần tử con nào đè lên
        if (cardFrameImg != null)
        {
            cardFrameImg.transform.SetAsLastSibling();
        }

        // Add interactive hover states
        SetupCardHover(card, cardRT, cardFrameImg, btnFrameImg);
    }

    private void SetupCardHover(GameObject cardObj, RectTransform cardRT, Image cardFrame, Image btnFrame)
    {
        var trigger = cardObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = cardObj.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => {
            if (cardRT != null)
            {
                StartCoroutine(AnimateHover(cardRT, cardFrame, btnFrame, 1.035f, ColorCardFrameHover, 0.12f));
            }
        });

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => {
            if (cardRT != null)
            {
                StartCoroutine(AnimateHover(cardRT, cardFrame, btnFrame, 1.000f, ColorCardFrameNorm, 0.12f));
            }
        });

        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
    }

    private IEnumerator AnimateHover(RectTransform rt, Image cardFrame, Image btnFrame,
        float targetScale, Color targetFrameColor, float duration)
    {
        if (rt == null) yield break;

        Vector3 startScale = rt.localScale;
        Vector3 endScale = Vector3.one * targetScale;

        Color startCardColor = cardFrame != null ? cardFrame.color : targetFrameColor;
        Color startBtnColor  = btnFrame != null ? btnFrame.color : targetFrameColor;

        float t = 0f;
        while (t < duration)
        {
            if (rt == null) yield break;
            t += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(t / duration);

            rt.localScale = Vector3.Lerp(startScale, endScale, factor);
            if (cardFrame != null) cardFrame.color = Color.Lerp(startCardColor, targetFrameColor, factor);
            if (btnFrame != null) btnFrame.color = Color.Lerp(startBtnColor, targetFrameColor, factor);

            yield return null;
        }

        if (rt != null) rt.localScale = endScale;
        if (cardFrame != null) cardFrame.color = targetFrameColor;
        if (btnFrame != null) btnFrame.color = targetFrameColor;
    }

    private string GetTypeBadgeText(AugmentDatabase.AugmentType type)
    {
        switch (type)
        {
            case AugmentDatabase.AugmentType.BonusAttack:
            case AugmentDatabase.AugmentType.RagingFire:
            case AugmentDatabase.AugmentType.BerserkerMode:
                return "ATK";
            case AugmentDatabase.AugmentType.BonusHealth:
            case AugmentDatabase.AugmentType.RegenerativeCore:
            case AugmentDatabase.AugmentType.SoulHarvest:
                return "HP";
            case AugmentDatabase.AugmentType.BonusSpeed:
            case AugmentDatabase.AugmentType.SwiftShadow:
                return "SPD";
            case AugmentDatabase.AugmentType.BonusCritChance:
            case AugmentDatabase.AugmentType.BonusCritDamage:
                return "CRIT";
            case AugmentDatabase.AugmentType.BonusLifesteal:
            case AugmentDatabase.AugmentType.BonusSpellVamp:
            case AugmentDatabase.AugmentType.VampireBlood:
                return "VAMP";
            case AugmentDatabase.AugmentType.BonusDefense:
            case AugmentDatabase.AugmentType.AncientShield:
                return "DEF";
            case AugmentDatabase.AugmentType.BonusCooldownReduction:
            case AugmentDatabase.AugmentType.EchoStrike:
            case AugmentDatabase.AugmentType.UltimateCharge:
                return "CDR";
            case AugmentDatabase.AugmentType.ComboMastery:
            case AugmentDatabase.AugmentType.BonusMaxEnergy:
                return "COMBO";
            default:
                return "BUFF";
        }
    }

    // ─── Pick choice ───────────────────────────────────────────────────────────
    private void PickChoice(AugmentDatabase.AugmentData aug)
    {
        if (AugmentManager.Instance != null)
            AugmentManager.Instance.ApplyAugment(aug);

        // Show floating text
        DamagePopup.CreateText(
            Camera.main != null ? Camera.main.transform.position + Vector3.up * 1.5f : Vector3.up * 2f,
            LocalizationManager.IsVietnamese
                ? $"[ {aug.nameVN} ]"
                : $"[ {aug.nameEN} ]",
            new Color(0.95f, 0.95f, 0.98f)
        );

        SoundManager.EnsureInstance()?.PlayUpgradeSuccess();

        _isShowing = false;
        if (_root != null)
        {
            Destroy(_root);
            _root = null;
        }

        if (_pendingLevelUps.Count == 0)
        {
            GameAutoBootstrap.SetGameplayHUDVisible(true);
        }
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────
    private GameObject CreatePanel(Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        return go;
    }

    private GameObject CreateFramedPanel(Transform parent, string name, Color bgColor, Color frameColor,
        Vector2 anchorMin, Vector2 anchorMax, float framePixelsMultiplier, out Image frameImg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var bgImg = go.AddComponent<Image>();
        bgImg.color = bgColor;
        bgImg.raycastTarget = true;

        var frameObj = new GameObject("FrameBorder");
        frameObj.transform.SetParent(go.transform, false);
        var frameRT = frameObj.AddComponent<RectTransform>();
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.offsetMin = frameRT.offsetMax = Vector2.zero;

        frameImg = frameObj.AddComponent<Image>();
        frameImg.sprite = GetFrameSprite();
        frameImg.type = Image.Type.Sliced;
        frameImg.pixelsPerUnitMultiplier = framePixelsMultiplier;
        frameImg.color = frameColor;
        frameImg.raycastTarget = false;

        return go;
    }

    /// <summary>
    /// Tạo cánh trang trí một bên (divider-fade-004) với tuỳ chọn lật gương horizontally.
    /// </summary>
    private GameObject CreateDividerWing(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color, bool flipHorizontal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        if (flipHorizontal)
        {
            rt.localScale = new Vector3(-1f, 1f, 1f);
        }

        var img = go.AddComponent<Image>();
        img.sprite = GetDividerSprite();
        img.color = color;
        img.raycastTarget = false;
        img.preserveAspect = false;

        return go;
    }

    /// <summary>
    /// Tạo thanh phân cách đối xứng trung tâm từ 2 nửa ghép lại (chữ thập ở giữa, mờ dần sang 2 bên).
    /// </summary>
    private GameObject CreateOrnamentDivider(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Left half (mờ từ mép trái tới tâm)
        var left = new GameObject("Left");
        left.transform.SetParent(go.transform, false);
        var leftRT = left.AddComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0f, 0f);
        leftRT.anchorMax = new Vector2(0.5f, 1f);
        leftRT.offsetMin = leftRT.offsetMax = Vector2.zero;
        var leftImg = left.AddComponent<Image>();
        leftImg.sprite = GetDividerSprite();
        leftImg.color = color;
        leftImg.raycastTarget = false;
        leftImg.preserveAspect = false;

        // Right half (lật ngược, từ tâm mờ dần ra mép phải)
        var right = new GameObject("Right");
        right.transform.SetParent(go.transform, false);
        var rightRT = right.AddComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.5f, 0f);
        rightRT.anchorMax = new Vector2(1f, 1f);
        rightRT.offsetMin = rightRT.offsetMax = Vector2.zero;
        rightRT.localScale = new Vector3(-1f, 1f, 1f);
        var rightImg = right.AddComponent<Image>();
        rightImg.sprite = GetDividerSprite();
        rightImg.color = color;
        rightImg.raycastTarget = false;
        rightImg.preserveAspect = false;

        return go;
    }
}
