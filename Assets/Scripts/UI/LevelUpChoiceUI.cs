using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// UI hiển thị 3 lựa chọn Lõi Nâng Cấp (Augments) phong cách Đấu Trường Chân Lý (TFT) & Dark Fantasy.
/// - Thể hiện rõ BẬC NÂNG CẤP (Tier / Rarity): BẬC BẠC (SILVER TIER), BẬC VÀNG (GOLD TIER), BẬC KIM CƯƠNG (PRISMATIC TIER)
/// - Giữ màu sắc đặc trưng của Loại Chỉ Số (Vật Lý, Phép, Chí Mạng, Hút Máu, Sinh Lực, Tốc Độ, Phòng Thủ, Nội Năng)
/// - Loại bỏ hoàn toàn các ký tự gây lỗi ô vuông (emojis, unicode stars) và các dấu ngoặc vuông "[ ]"
/// - Huy hiệu trung tâm dùng Sprite đồ họa chất lượng cao kết hợp mã chữ viết tắt rõ nét (ATK, HP, SPD...)
/// - Cơ chế Đổi Lại (Reroll) với số lượt ngẫu nhiên từ 2 đến 4 lượt tùy vào độ may rủi khi lên cấp
/// </summary>
public class LevelUpChoiceUI : MonoBehaviour
{
    // ─── Singleton ─────────────────────────────────────────────────────────────
    private static LevelUpChoiceUI _instance;

    // ─── Runtime ───────────────────────────────────────────────────────────────
    private GameObject _root;
    private bool _isShowing = false;
    public static bool IsShowing => _instance != null && _instance._isShowing;
    private List<AugmentDatabase.AugmentData> _currentChoices;

    // Reroll system: 1 trận có tối đa 5 lượt đổi lại (Match-wide limit)
    public const int MAX_MATCH_REROLLS = 5;
    private static int _matchRemainingRerolls = MAX_MATCH_REROLLS;
    public static void ResetMatchRerolls() => _matchRemainingRerolls = MAX_MATCH_REROLLS;
    public static int RemainingMatchRerolls => _matchRemainingRerolls;

    private int _remainingRerolls = MAX_MATCH_REROLLS;
    private int _totalRerolls = MAX_MATCH_REROLLS;
    private int _currentLevel = 1;
    private string _currentHeroId = "Vaelstrider";
    private TextMeshProUGUI _rerollBtnText;
    private Button _rerollBtn;
    private Transform _cardsContainer;

    // Danh sách lõi đã xuất hiện trong phiên lên cấp này (chống trùng lặp khi roll lại - Anti-Repeat chuẩn TFT)
    private readonly HashSet<AugmentDatabase.AugmentType> _sessionSeenAugments = new HashSet<AugmentDatabase.AugmentType>();

    // Queue lên cấp liên tiếp (khi giết Boss nhận nhiều EXP)
    private static readonly Queue<int> _pendingLevelUps = new Queue<int>();

    // Cache sprites
    private static Sprite _frameSprite;
    private static Sprite _dividerSprite;
    private static Sprite _stoneBtnSprite;
    private static Sprite _panelBgSprite;
    private static Sprite _runicRingSprite;
    private static Sprite _obsidianCoreSprite;

    private static Sprite _cachedAttackIcon;
    private static Sprite _cachedDashIcon;
    private static Sprite _cachedUltiIcon;
    private static Sprite _cachedStarIcon;

    // ─── Luminous Dark Fantasy Color Palette ──────────────────────────────────
    private static readonly Color ColorOverlayBg      = new Color(0.015f, 0.020f, 0.030f, 0.75f);
    private static readonly Color ColorMainPanelBg    = new Color(0.050f, 0.065f, 0.095f, 0.98f);
    private static readonly Color ColorMainPanelFrame = new Color(0.950f, 0.820f, 0.400f, 0.90f);

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
        var ph = PlayerHealth.Instance ?? FindAnyObjectByType<PlayerHealth>();
        if (ph != null) ph.isInvulnerable = false;
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.SetCanMove(true);
        Time.timeScale = 1.0f;
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
        _currentLevel = level;

        // Kích hoạt trạng thái Miễn Sát Thương tuyệt đối cho Người chơi trong lúc chọn Lõi Nâng Cấp
        var ph = PlayerHealth.Instance ?? FindAnyObjectByType<PlayerHealth>();
        bool prevInvul = false;
        if (ph != null)
        {
            prevInvul = ph.isInvulnerable;
            ph.isInvulnerable = true;
        }

        // Tạm dừng nhận input di chuyển
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.SetCanMove(false);

        // Xóa sạch danh sách lõi đã thấy ở các lượt roll trước của phiên này
        _sessionSeenAugments.Clear();

        // Đồng bộ số lượt đổi lại còn lại của trận đấu (tối đa 5 lượt/trận)
        _remainingRerolls = _matchRemainingRerolls;
        _totalRerolls = MAX_MATCH_REROLLS;

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

        // Xoá UI trước khi mở lại timeScale để tránh spike lag 1 frame
        if (_root != null)
        {
            Destroy(_root);
            _root = null;
        }

        // Chờ 1 frame để tất cả enemy AI Update() nhận được IsShowing=false đúng thứ tự
        yield return new WaitForEndOfFrame();

        // Khôi phục thời gian và trạng thái nhân vật
        Time.timeScale = (prevTimeScale > 0f) ? prevTimeScale : 1.0f;
        if (ph != null) ph.isInvulnerable = prevInvul;
        if (pc != null) pc.SetCanMove(true);

        // Nếu không còn lượt lên cấp chờ nào, hiện lại Gameplay HUD
        if (_pendingLevelUps.Count == 0)
        {
            GameAutoBootstrap.SetGameplayHUDVisible(true);
        }

        yield return new WaitForSecondsRealtime(0.15f);
        ProcessQueue();
    }

    // ─── Asset Loaders ─────────────────────────────────────────────────────────
    private static Sprite LoadSpriteFromDiskOrEditor(string relativePath)
    {
#if UNITY_EDITOR
        Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
        if (s != null) return s;
#endif
        string diskPath = System.IO.Path.Combine(Application.dataPath, relativePath.Replace("Assets/", ""));
        if (System.IO.File.Exists(diskPath))
        {
            byte[] bytes = System.IO.File.ReadAllBytes(diskPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(bytes))
            {
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }
        return null;
    }

    private static Sprite GetFrameSprite()
    {
        if (_frameSprite != null) return _frameSprite;
        _frameSprite = Resources.Load<Sprite>("UI/frame_gothic_border");
        if (_frameSprite != null) return _frameSprite;

#if UNITY_EDITOR
        _frameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/frame_gothic_border.png");
        if (_frameSprite != null) return _frameSprite;
#endif

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
        _dividerSprite = SoulslikeHUDStyler.GetGothicDividerSprite();
        if (_dividerSprite != null) return _dividerSprite;

#if UNITY_EDITOR
        _dividerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/divider_fade_004.png");
        if (_dividerSprite != null) return _dividerSprite;
#endif

        _dividerSprite = Resources.Load<Sprite>("UI/divider_fade_004");
        return _dividerSprite;
    }

    private static Sprite GetStoneBtnSprite()
    {
        if (_stoneBtnSprite != null) return _stoneBtnSprite;
        _stoneBtnSprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        return _stoneBtnSprite;
    }

    private static Sprite GetPanelBgSprite()
    {
        if (_panelBgSprite != null) return _panelBgSprite;
        _panelBgSprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        return _panelBgSprite;
    }

    private static Sprite GetRunicRingSprite()
    {
        if (_runicRingSprite != null) return _runicRingSprite;
        _runicRingSprite = SoulslikeHUDStyler.GetActionButtonRingSprite();
        return _runicRingSprite;
    }

    private static Sprite GetObsidianCoreSprite()
    {
        if (_obsidianCoreSprite != null) return _obsidianCoreSprite;
        _obsidianCoreSprite = SoulslikeHUDStyler.GetActionButtonCoreSprite();
        return _obsidianCoreSprite;
    }

    private static Sprite GetMedallionSprite(AugmentDatabase.AugmentType type)
    {
        // Sử dụng Icon đồ họa riêng biệt độc nhất từ AugmentIconHelper
        Sprite s = AugmentIconHelper.GetAugmentSprite(type);
        if (s != null) return s;

        if (_cachedAttackIcon == null)
            _cachedAttackIcon = LoadSpriteFromDiskOrEditor("Assets/Sprites/UI/Attack.png");
        return _cachedAttackIcon;
    }

    // ─── Build UI ──────────────────────────────────────────────────────────────
    private void BuildUI(int level)
    {
        if (_root != null) Destroy(_root);

        bool isVietnamese = LocalizationManager.IsVietnamese;
        _currentHeroId = PlayerUpgradeManager.Instance != null
            ? PlayerUpgradeManager.Instance.GetCurrentHeroKey() : "Vaelstrider";

        _currentChoices = AugmentDatabase.GetRandomChoices(_currentHeroId, 3, level, _sessionSeenAugments);
        if (_currentChoices != null)
        {
            foreach (var c in _currentChoices)
            {
                if (c != null) _sessionSeenAugments.Add(c.type);
            }
        }

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

        // ── Dark Overlay Vignette ──────────────────────────────────────────────
        var overlay = CreatePanel(_root.transform, ColorOverlayBg, Vector2.zero, Vector2.one);
        overlay.name = "Overlay";

        // ── Main Panel Window (Framed with Gothic Ornate Asset) ────────────────
        var panel = CreateFramedPanel(overlay.transform, "MainPanel",
            ColorMainPanelBg, ColorMainPanelFrame,
            new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f),
            0.35f, out Image panelFrameImg);

        // ── Title Section ──────────────────────────────────────────────────────
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.10f, 0.850f);
        titleRT.anchorMax = new Vector2(0.90f, 0.985f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        string titleStr = isVietnamese ? $"LÊN CẤP {level}" : $"LEVEL UP {level}";
        string subStr   = isVietnamese ? "Chọn 1 Lõi Nâng Cấp để tiếp nhận ân sủng tàn tích" : "Choose 1 Augment to embrace the Ruinbound grace";
        titleTMP.text = $"<size=38><b><color=#FFEAA7>{titleStr}</color></b></size>\n<size=16><color=#CBD5E1>{subStr}</color></size>";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;
        titleTMP.enableWordWrapping = true;
        SoulslikeHUDStyler.SetSafeOutline(titleTMP, 0.45f, new Color(0.02f, 0.02f, 0.02f, 0.98f));

        // Title flanking decorative divider wings
        CreateDividerWing(panel.transform, "TitleLeftWing",
            new Vector2(0.20f, 0.915f), new Vector2(0.34f, 0.938f),
            new Color(1f, 0.85f, 0.40f, 0.90f), false);

        CreateDividerWing(panel.transform, "TitleRightWing",
            new Vector2(0.66f, 0.915f), new Vector2(0.80f, 0.938f),
            new Color(1f, 0.85f, 0.40f, 0.90f), true);

        // Title bottom symmetric ornamental divider
        CreateOrnamentDivider(panel.transform, "TitleDivider",
            new Vector2(0.15f, 0.835f), new Vector2(0.85f, 0.850f),
            new Color(0.95f, 0.82f, 0.40f, 0.85f));

        // ── Cards Container ────────────────────────────────────────────────────
        var cardsContainerObj = new GameObject("CardsContainer");
        cardsContainerObj.transform.SetParent(panel.transform, false);
        var ccRT = cardsContainerObj.AddComponent<RectTransform>();
        ccRT.anchorMin = new Vector2(0f, 0.09f);
        ccRT.anchorMax = new Vector2(1f, 0.83f);
        ccRT.offsetMin = ccRT.offsetMax = Vector2.zero;
        _cardsContainer = cardsContainerObj.transform;

        BuildCards(isVietnamese);

        // ── Reroll Button (Đổi lại may rủi 2 đến 4 lượt) ───────────────────────
        BuildRerollButton(panel.transform, isVietnamese);
    }

    private void BuildCards(bool isVietnamese)
    {
        for (int i = _cardsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_cardsContainer.GetChild(i).gameObject);
        }

        float cardWidth = 0.290f;
        float startX    = 0.045f;
        float gap       = (1f - startX * 2f - cardWidth * 3f) / 2f;

        for (int i = 0; i < _currentChoices.Count; i++)
        {
            var aug = _currentChoices[i];
            float xMin = startX + i * (cardWidth + gap);
            float xMax = xMin + cardWidth;
            BuildCard(_cardsContainer, aug, i, isVietnamese,
                new Vector2(xMin, 0.02f), new Vector2(xMax, 0.98f));
        }
    }

    private void BuildRerollButton(Transform parent, bool isVN)
    {
        var btnObj = new GameObject("RerollButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        var btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.35f, 0.015f);
        btnRT.anchorMax = new Vector2(0.65f, 0.078f);
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

        var btnImg = btnObj.GetComponent<Image>();
        btnImg.sprite = GetStoneBtnSprite();
        btnImg.type = Image.Type.Sliced;
        btnImg.color = new Color(0.18f, 0.20f, 0.28f, 1f);

        _rerollBtn = btnObj.GetComponent<Button>();
        _rerollBtn.targetGraphic = btnImg;

        var outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.75f, 0.25f, 0.95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Icon đồ họa xoay vòng lặp lại (Repeat-Right) không bao giờ bị lỗi ô vuông
        Sprite rerollIcon = WhiteIconLoader.GetSprite("Repeat-Right");
        if (rerollIcon != null)
        {
            var iconObj = new GameObject("RerollIcon");
            iconObj.transform.SetParent(btnObj.transform, false);
            var iconRT = iconObj.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.06f, 0.20f);
            iconRT.anchorMax = new Vector2(0.18f, 0.80f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

            var iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = rerollIcon;
            iconImg.color = new Color(1f, 0.85f, 0.35f, 0.95f);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }

        var txtObj = new GameObject("RerollText");
        txtObj.transform.SetParent(btnObj.transform, false);
        var txtRT = txtObj.AddComponent<RectTransform>();
        txtRT.anchorMin = new Vector2(0.16f, 0f);
        txtRT.anchorMax = new Vector2(0.96f, 1f);
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;

        _rerollBtnText = txtObj.AddComponent<TextMeshProUGUI>();
        _rerollBtnText.fontSize = 17f;
        _rerollBtnText.fontStyle = FontStyles.Bold;
        _rerollBtnText.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(_rerollBtnText, 0.40f, new Color(0f, 0f, 0f, 0.95f));

        UpdateRerollButtonUI(isVN);

        _rerollBtn.onClick.AddListener(() => OnRerollClicked(isVN));
    }

    private void UpdateRerollButtonUI(bool isVN)
    {
        if (_rerollBtnText == null || _rerollBtn == null) return;

        if (_matchRemainingRerolls > 0)
        {
            _rerollBtn.interactable = true;
            string label = isVN
                ? $"ĐỔI LẠI <color=#FFD700>({_matchRemainingRerolls}/{MAX_MATCH_REROLLS} LƯỢT)</color>"
                : $"REROLL <color=#FFD700>({_matchRemainingRerolls}/{MAX_MATCH_REROLLS} LEFT)</color>";
            _rerollBtnText.text = label;
            _rerollBtnText.color = Color.white;
        }
        else
        {
            _rerollBtn.interactable = false;
            string label = isVN ? $"HẾT LƯỢT ĐỔI (0/{MAX_MATCH_REROLLS})" : $"NO REROLLS (0/{MAX_MATCH_REROLLS})";
            _rerollBtnText.text = label;
            _rerollBtnText.color = new Color(0.6f, 0.6f, 0.65f, 1f);
        }
    }

    private void OnRerollClicked(bool isVN)
    {
        if (_matchRemainingRerolls <= 0)
        {
            UpdateRerollButtonUI(isVN);
            return;
        }

        _matchRemainingRerolls--;
        _remainingRerolls = _matchRemainingRerolls;
        UpdateRerollButtonUI(isVN);
        SoundManager.EnsureInstance()?.PlayUpgradeSuccess();

        _currentChoices = AugmentDatabase.GetRandomChoices(_currentHeroId, 3, _currentLevel, _sessionSeenAugments);
        if (_currentChoices != null)
        {
            foreach (var c in _currentChoices)
            {
                if (c != null) _sessionSeenAugments.Add(c.type);
            }
        }

        DamagePopup.CreateText(
            Camera.main != null ? Camera.main.transform.position + Vector3.up * 1.5f : Vector3.up * 2f,
            isVN ? "LÀM MỚI LÕI NÂNG CẤP" : "AUGMENTS RE-ROLLED",
            new Color(1f, 0.85f, 0.25f)
        );

        BuildCards(isVN);
    }

    // ─── Stat Color & Naming Helpers ───────────────────────────────────────────
    public static Color GetDamageTypeColor(AugmentDatabase.AugmentType type)
    {
        switch (type)
        {
            case AugmentDatabase.AugmentType.BonusAttack:
            case AugmentDatabase.AugmentType.LaceratingEdge:
            case AugmentDatabase.AugmentType.RagingFire:
            case AugmentDatabase.AugmentType.BerserkerMode:
            case AugmentDatabase.AugmentType.HeavyImpact:
            case AugmentDatabase.AugmentType.RelentlessAssault:
            case AugmentDatabase.AugmentType.GiantSlayer:
            case AugmentDatabase.AugmentType.BattleFrenzy:
                return new Color(0.96f, 0.26f, 0.21f); // Sát thương Vật Lý: Đỏ Chiến Binh (#F44336)
            case AugmentDatabase.AugmentType.BonusSpellVamp:
            case AugmentDatabase.AugmentType.EchoStrike:
            case AugmentDatabase.AugmentType.ArcaneOverdrive:
            case AugmentDatabase.AugmentType.CelestialCascade:
            case AugmentDatabase.AugmentType.StaticOverload:
            case AugmentDatabase.AugmentType.EchoingVoid:
                return new Color(0.72f, 0.33f, 1f);    // Phép Thuật / Tinh Tú: Tím Ma Pháp (#B854FF)
            case AugmentDatabase.AugmentType.BonusCritChance:
            case AugmentDatabase.AugmentType.BonusCritDamage:
            case AugmentDatabase.AugmentType.ShadowAmbush:
            case AugmentDatabase.AugmentType.ChainReaction:
                return new Color(1f, 0.82f, 0.12f);    // Chí Mạng: Vàng Kim Hoàng Triều (#FFD21E)
            case AugmentDatabase.AugmentType.BonusLifesteal:
            case AugmentDatabase.AugmentType.VampireBlood:
            case AugmentDatabase.AugmentType.BloodGodTribute:
            case AugmentDatabase.AugmentType.DeathsTouch:
            case AugmentDatabase.AugmentType.LifeTransfusion:
                return new Color(0.95f, 0.15f, 0.25f); // Huyết Tế / Hút Máu: Đỏ Huyết (#F42640)
            case AugmentDatabase.AugmentType.PoisonTouch:
                return new Color(0.12f, 0.90f, 0.42f); // Độc Dược / Ăn Mòn: Lục Độc Acid (#1FE66B)
            case AugmentDatabase.AugmentType.BonusHealth:
            case AugmentDatabase.AugmentType.RegenerativeCore:
            case AugmentDatabase.AugmentType.SoulHarvest:
            case AugmentDatabase.AugmentType.AbundantVitality:
            case AugmentDatabase.AugmentType.SecondWind:
                return new Color(0.18f, 0.88f, 0.45f); // Sinh Lực: Lục Bảo (#2EE073)
            case AugmentDatabase.AugmentType.BonusSpeed:
            case AugmentDatabase.AugmentType.SwiftShadow:
            case AugmentDatabase.AugmentType.FleetWind:
            case AugmentDatabase.AugmentType.StormDancer:
                return new Color(0.20f, 0.80f, 1f);    // Nhanh Nhẹn / Phong Lôi: Băng Lam (#33CCFF)
            case AugmentDatabase.AugmentType.BonusDefense:
            case AugmentDatabase.AugmentType.AncientShield:
            case AugmentDatabase.AugmentType.IronBastion:
            case AugmentDatabase.AugmentType.DivineAegis:
            case AugmentDatabase.AugmentType.ArcaneShield:
            case AugmentDatabase.AugmentType.UnwaveringWill:
            case AugmentDatabase.AugmentType.PrismaticTitan:
                return new Color(0.35f, 0.65f, 1f);    // Phòng Thủ: Lam Khiên (#59A6FF)
            case AugmentDatabase.AugmentType.BountyHunter:
                return new Color(1.00f, 0.82f, 0.20f); // Tài Phú / Tinh Thể (#FFD133)
            case AugmentDatabase.AugmentType.BonusCooldownReduction:
            case AugmentDatabase.AugmentType.UltimateCharge:
            case AugmentDatabase.AugmentType.ComboMastery:
            case AugmentDatabase.AugmentType.SurgingPulse:
            case AugmentDatabase.AugmentType.BonusMaxEnergy:
                return new Color(0.98f, 0.65f, 0.18f); // Hồi Chiêu / Nội Năng: Hổ Phách (#FA962E)
            case AugmentDatabase.AugmentType.DemonicPact:
            case AugmentDatabase.AugmentType.UndyingSovereign:
            case AugmentDatabase.AugmentType.GodforgedArsenal:
            case AugmentDatabase.AugmentType.AscendedDivinity:
            default:
                return new Color(0.95f, 0.45f, 0.90f); // Thần Thoại / Đa Năng: Hồng Tinh Vân (#F273E6)
        }
    }

    public static string GetDamageTypeName(AugmentDatabase.AugmentType type, bool isVN)
    {
        switch (type)
        {
            case AugmentDatabase.AugmentType.BonusAttack:
            case AugmentDatabase.AugmentType.LaceratingEdge:
            case AugmentDatabase.AugmentType.RagingFire:
            case AugmentDatabase.AugmentType.BerserkerMode:
            case AugmentDatabase.AugmentType.HeavyImpact:
            case AugmentDatabase.AugmentType.RelentlessAssault:
            case AugmentDatabase.AugmentType.GiantSlayer:
            case AugmentDatabase.AugmentType.BattleFrenzy:
                return isVN ? "VẬT LÝ" : "PHYSICAL";
            case AugmentDatabase.AugmentType.BonusSpellVamp:
            case AugmentDatabase.AugmentType.EchoStrike:
            case AugmentDatabase.AugmentType.ArcaneOverdrive:
            case AugmentDatabase.AugmentType.CelestialCascade:
            case AugmentDatabase.AugmentType.StaticOverload:
            case AugmentDatabase.AugmentType.EchoingVoid:
                return isVN ? "PHÉP THUẬT" : "MAGIC";
            case AugmentDatabase.AugmentType.BonusCritChance:
            case AugmentDatabase.AugmentType.BonusCritDamage:
            case AugmentDatabase.AugmentType.ShadowAmbush:
            case AugmentDatabase.AugmentType.ChainReaction:
                return isVN ? "BẠO KÍCH" : "CRITICAL";
            case AugmentDatabase.AugmentType.BonusLifesteal:
            case AugmentDatabase.AugmentType.VampireBlood:
            case AugmentDatabase.AugmentType.BloodGodTribute:
            case AugmentDatabase.AugmentType.DeathsTouch:
            case AugmentDatabase.AugmentType.LifeTransfusion:
                return isVN ? "HÚT MÁU" : "LIFESTEAL";
            case AugmentDatabase.AugmentType.PoisonTouch:
                return isVN ? "ĐỘC DƯỢC" : "VENOM";
            case AugmentDatabase.AugmentType.BonusHealth:
            case AugmentDatabase.AugmentType.RegenerativeCore:
            case AugmentDatabase.AugmentType.SoulHarvest:
            case AugmentDatabase.AugmentType.AbundantVitality:
            case AugmentDatabase.AugmentType.SecondWind:
                return isVN ? "SINH LỰC" : "VITALITY";
            case AugmentDatabase.AugmentType.BonusSpeed:
            case AugmentDatabase.AugmentType.SwiftShadow:
            case AugmentDatabase.AugmentType.FleetWind:
            case AugmentDatabase.AugmentType.StormDancer:
                return isVN ? "TỐC ĐỘ" : "SPEED";
            case AugmentDatabase.AugmentType.BonusDefense:
            case AugmentDatabase.AugmentType.AncientShield:
            case AugmentDatabase.AugmentType.IronBastion:
            case AugmentDatabase.AugmentType.DivineAegis:
            case AugmentDatabase.AugmentType.ArcaneShield:
            case AugmentDatabase.AugmentType.UnwaveringWill:
            case AugmentDatabase.AugmentType.PrismaticTitan:
                return isVN ? "PHÒNG THỦ" : "DEFENSE";
            case AugmentDatabase.AugmentType.BountyHunter:
                return isVN ? "TÀI PHÚ" : "FORTUNE";
            case AugmentDatabase.AugmentType.BonusCooldownReduction:
            case AugmentDatabase.AugmentType.UltimateCharge:
            case AugmentDatabase.AugmentType.ComboMastery:
            case AugmentDatabase.AugmentType.SurgingPulse:
            case AugmentDatabase.AugmentType.BonusMaxEnergy:
                return isVN ? "NỘI NĂNG" : "ENERGY";
            case AugmentDatabase.AugmentType.DemonicPact:
            case AugmentDatabase.AugmentType.UndyingSovereign:
            case AugmentDatabase.AugmentType.GodforgedArsenal:
            case AugmentDatabase.AugmentType.AscendedDivinity:
            default:
                return isVN ? "TOÀN NĂNG" : "MYTHIC";
        }
    }

    public static string GetStatShortCode(AugmentDatabase.AugmentType type)
    {
        switch (type)
        {
            case AugmentDatabase.AugmentType.BonusAttack:
            case AugmentDatabase.AugmentType.LaceratingEdge:
            case AugmentDatabase.AugmentType.RagingFire:
            case AugmentDatabase.AugmentType.BerserkerMode:
            case AugmentDatabase.AugmentType.HeavyImpact:
            case AugmentDatabase.AugmentType.RelentlessAssault:
            case AugmentDatabase.AugmentType.GiantSlayer:
            case AugmentDatabase.AugmentType.BattleFrenzy:
                return "ATK";
            case AugmentDatabase.AugmentType.BonusSpellVamp:
            case AugmentDatabase.AugmentType.EchoStrike:
            case AugmentDatabase.AugmentType.ArcaneOverdrive:
            case AugmentDatabase.AugmentType.CelestialCascade:
            case AugmentDatabase.AugmentType.StaticOverload:
            case AugmentDatabase.AugmentType.EchoingVoid:
                return "AP";
            case AugmentDatabase.AugmentType.BonusCritChance:
            case AugmentDatabase.AugmentType.BonusCritDamage:
            case AugmentDatabase.AugmentType.ShadowAmbush:
            case AugmentDatabase.AugmentType.ChainReaction:
                return "CRIT";
            case AugmentDatabase.AugmentType.BonusLifesteal:
            case AugmentDatabase.AugmentType.VampireBlood:
            case AugmentDatabase.AugmentType.BloodGodTribute:
            case AugmentDatabase.AugmentType.DeathsTouch:
            case AugmentDatabase.AugmentType.LifeTransfusion:
                return "VAMP";
            case AugmentDatabase.AugmentType.PoisonTouch:
                return "TOXIN";
            case AugmentDatabase.AugmentType.BonusHealth:
            case AugmentDatabase.AugmentType.RegenerativeCore:
            case AugmentDatabase.AugmentType.SoulHarvest:
            case AugmentDatabase.AugmentType.AbundantVitality:
            case AugmentDatabase.AugmentType.SecondWind:
                return "HP";
            case AugmentDatabase.AugmentType.BonusSpeed:
            case AugmentDatabase.AugmentType.SwiftShadow:
            case AugmentDatabase.AugmentType.FleetWind:
            case AugmentDatabase.AugmentType.StormDancer:
                return "SPD";
            case AugmentDatabase.AugmentType.BonusDefense:
            case AugmentDatabase.AugmentType.AncientShield:
            case AugmentDatabase.AugmentType.IronBastion:
            case AugmentDatabase.AugmentType.DivineAegis:
            case AugmentDatabase.AugmentType.ArcaneShield:
            case AugmentDatabase.AugmentType.UnwaveringWill:
            case AugmentDatabase.AugmentType.PrismaticTitan:
                return "DEF";
            case AugmentDatabase.AugmentType.BountyHunter:
                return "SHARD";
            case AugmentDatabase.AugmentType.BonusCooldownReduction:
            case AugmentDatabase.AugmentType.UltimateCharge:
            case AugmentDatabase.AugmentType.ComboMastery:
            case AugmentDatabase.AugmentType.SurgingPulse:
            case AugmentDatabase.AugmentType.BonusMaxEnergy:
                return "CDR";
            case AugmentDatabase.AugmentType.DemonicPact:
            case AugmentDatabase.AugmentType.UndyingSovereign:
            case AugmentDatabase.AugmentType.GodforgedArsenal:
            case AugmentDatabase.AugmentType.AscendedDivinity:
            default:
                return "MYTH";
        }
    }

    // ─── Build Individual Card (Dark Fantasy & TFT Tier Aesthetic) ─────────────
    private void BuildCard(Transform parent, AugmentDatabase.AugmentData aug, int index,
        bool isVN, Vector2 anchorMin, Vector2 anchorMax)
    {
        Color typeColor = GetDamageTypeColor(aug.type);
        string typeName = GetDamageTypeName(aug.type, isVN);

        // 1. Phân biệt BẬC NÂNG CẤP (TIER / RARITY) rõ rệt:
        string tierTitle;
        Color tierFrameColor;
        Color tierGlowColor;
        Color tierBannerBg;

        switch (aug.rarity)
        {
            case AugmentDatabase.AugmentData.Rarity.Epic:
                tierTitle = isVN ? "BẬC KIM CƯƠNG" : "PRISMATIC TIER";
                tierFrameColor = new Color(0.92f, 0.50f, 0.98f, 1f); // Tím Lăng Kính Ánh Tinh Vân
                tierGlowColor  = new Color(0.70f, 0.35f, 0.95f, 0.40f);
                tierBannerBg   = new Color(0.32f, 0.08f, 0.42f, 0.95f);
                break;

            case AugmentDatabase.AugmentData.Rarity.Rare:
                tierTitle = isVN ? "BẬC VÀNG" : "GOLD TIER";
                tierFrameColor = new Color(1.00f, 0.82f, 0.28f, 1f); // Hoàng Kim Vương Giả
                tierGlowColor  = new Color(0.95f, 0.60f, 0.12f, 0.35f);
                tierBannerBg   = new Color(0.38f, 0.22f, 0.05f, 0.95f);
                break;

            default:
                tierTitle = isVN ? "BẬC BẠC" : "SILVER TIER";
                tierFrameColor = new Color(0.80f, 0.86f, 0.95f, 0.95f); // Thép Bạc Cổ Điển
                tierGlowColor  = new Color(0.55f, 0.65f, 0.80f, 0.30f);
                tierBannerBg   = new Color(0.12f, 0.16f, 0.24f, 0.95f);
                break;
        }

        // Card Container với viền phẩm chất rõ ràng
        var card = CreateFramedPanel(parent, $"Card_{index}",
            new Color(0.065f, 0.080f, 0.115f, 0.98f), tierFrameColor,
            anchorMin, anchorMax,
            0.40f, out Image cardFrameImg);

        var cardRT = card.GetComponent<RectTransform>();

        // 2. Banner Bậc Nâng Cấp (Top Tier Banner) - Không ký tự sao gây lỗi ô vuông
        var tierBannerObj = new GameObject("TierBanner");
        tierBannerObj.transform.SetParent(card.transform, false);
        var tbRT = tierBannerObj.AddComponent<RectTransform>();
        tbRT.anchorMin = new Vector2(0.04f, 0.925f);
        tbRT.anchorMax = new Vector2(0.96f, 0.985f);
        tbRT.offsetMin = tbRT.offsetMax = Vector2.zero;

        var tbImg = tierBannerObj.AddComponent<Image>();
        tbImg.sprite = GetPanelBgSprite();
        tbImg.type = Image.Type.Sliced;
        tbImg.color = tierBannerBg;

        var tbOutline = tierBannerObj.AddComponent<Outline>();
        tbOutline.effectColor = tierFrameColor;
        tbOutline.effectDistance = new Vector2(1f, -1f);

        var tierTxtObj = new GameObject("TierText");
        tierTxtObj.transform.SetParent(tierBannerObj.transform, false);
        var ttRT = tierTxtObj.AddComponent<RectTransform>();
        ttRT.anchorMin = Vector2.zero;
        ttRT.anchorMax = Vector2.one;
        ttRT.offsetMin = ttRT.offsetMax = Vector2.zero;

        var tierTxt = tierTxtObj.AddComponent<TextMeshProUGUI>();
        tierTxt.text = tierTitle;
        tierTxt.color = tierFrameColor;
        tierTxt.fontSize = 14f;
        tierTxt.alignment = TextAlignmentOptions.Center;
        tierTxt.fontStyle = FontStyles.Bold;
        SoulslikeHUDStyler.SetSafeOutline(tierTxt, 0.35f, new Color(0.02f, 0.02f, 0.02f, 0.95f));

        // 3. Stat Type Pill Tag - Bỏ hoàn toàn dấu ngoặc vuông "[ ]" và emoji
        var statTagObj = new GameObject("StatTag");
        statTagObj.transform.SetParent(card.transform, false);
        var stRT = statTagObj.AddComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0.20f, 0.855f);
        stRT.anchorMax = new Vector2(0.80f, 0.912f);
        stRT.offsetMin = stRT.offsetMax = Vector2.zero;

        var stImg = statTagObj.AddComponent<Image>();
        stImg.sprite = GetStoneBtnSprite();
        stImg.type = Image.Type.Sliced;
        stImg.color = new Color(0.04f, 0.05f, 0.07f, 0.95f);

        var stOutline = statTagObj.AddComponent<Outline>();
        stOutline.effectColor = new Color(typeColor.r, typeColor.g, typeColor.b, 0.90f);
        stOutline.effectDistance = new Vector2(1.2f, -1.2f);

        var stTxtObj = new GameObject("StatTagText");
        stTxtObj.transform.SetParent(statTagObj.transform, false);
        var sttRT = stTxtObj.AddComponent<RectTransform>();
        sttRT.anchorMin = Vector2.zero;
        sttRT.anchorMax = Vector2.one;
        sttRT.offsetMin = sttRT.offsetMax = Vector2.zero;

        var stTxt = stTxtObj.AddComponent<TextMeshProUGUI>();
        stTxt.text = typeName; // Chỉ hiển thị tên loại chỉ số rõ ràng, không có "[ ]" hay emoji
        stTxt.color = typeColor;
        stTxt.fontSize = 13.5f;
        stTxt.alignment = TextAlignmentOptions.Center;
        stTxt.fontStyle = FontStyles.Bold;
        SoulslikeHUDStyler.SetSafeOutline(stTxt, 0.30f, Color.black);

        // 4. Central Runic Medallion (Huy hiệu đá quý cổ điển Dark Fantasy)
        var medallionObj = new GameObject("Medallion");
        medallionObj.transform.SetParent(card.transform, false);
        var medRT = medallionObj.AddComponent<RectTransform>();
        medRT.anchorMin = new Vector2(0.5f, 0.730f);
        medRT.anchorMax = new Vector2(0.5f, 0.730f);
        medRT.sizeDelta = new Vector2(88f, 88f);
        medRT.pivot = new Vector2(0.5f, 0.5f);

        // Obsidian Core (Nền đá tròn)
        var coreImg = medallionObj.AddComponent<Image>();
        coreImg.sprite = GetObsidianCoreSprite();
        coreImg.color = new Color(0.08f, 0.10f, 0.14f, 1f);

        // Runic Ring (Vành kim loại chạm khắc cổ)
        var ringObj = new GameObject("RunicRing");
        ringObj.transform.SetParent(medallionObj.transform, false);
        var ringRT = ringObj.AddComponent<RectTransform>();
        ringRT.anchorMin = Vector2.zero;
        ringRT.anchorMax = Vector2.one;
        ringRT.offsetMin = ringRT.offsetMax = Vector2.zero;
        var ringImg = ringObj.AddComponent<Image>();
        ringImg.sprite = GetRunicRingSprite();
        ringImg.color = Color.Lerp(tierFrameColor, typeColor, 0.40f);

        // Biểu tượng icon ở giữa: Dùng Sprite Image đồ họa thật để KHÔNG BAO GIỜ bị ô vuông
        Sprite iconSprite = GetMedallionSprite(aug.type);
        if (iconSprite != null)
        {
            var iconImgObj = new GameObject("IconImg");
            iconImgObj.transform.SetParent(medallionObj.transform, false);
            var iconRT = iconImgObj.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.24f, 0.24f);
            iconRT.anchorMax = new Vector2(0.76f, 0.76f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

            var iconImg = iconImgObj.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.color = typeColor;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }
        else
        {
            var iconTxtObj = new GameObject("IconText");
            iconTxtObj.transform.SetParent(medallionObj.transform, false);
            var iconRT = iconTxtObj.AddComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

            var iconTMP = iconTxtObj.AddComponent<TextMeshProUGUI>();
            iconTMP.text = GetStatShortCode(aug.type);
            iconTMP.fontSize = 20f;
            iconTMP.fontStyle = FontStyles.Bold;
            iconTMP.alignment = TextAlignmentOptions.Center;
            iconTMP.color = typeColor;
            SoulslikeHUDStyler.SetSafeOutline(iconTMP, 0.35f, Color.black);
        }

        // 5. Augment Name (Tên Lõi Nâng Cấp)
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(card.transform, false);
        var nameRT = nameObj.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.05f, 0.540f);
        nameRT.anchorMax = new Vector2(0.95f, 0.640f);
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

        var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = isVN ? aug.nameVN : aug.nameEN;
        nameTMP.fontSize = 20.5f;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color = new Color(1f, 0.96f, 0.88f);
        nameTMP.enableWordWrapping = true;
        SoulslikeHUDStyler.SetSafeOutline(nameTMP, 0.40f, new Color(0.05f, 0.05f, 0.05f, 0.95f));

        // 6. Ornamental Divider 1
        CreateOrnamentDivider(card.transform, "Divider1",
            new Vector2(0.08f, 0.520f), new Vector2(0.92f, 0.535f),
            tierFrameColor * 0.85f);

        // 7. Augment Description (Mô tả chi tiết chuẩn TFT)
        string desc = isVN ? aug.descVN : aug.descEN;
        string valStr = aug.value % 1 == 0 ? ((int)aug.value).ToString() : aug.value.ToString("F1");
        desc = desc.Replace("{v}", $"<b><color=#FFEAA7>{valStr}</color></b>");
        if (aug.duration > 0)
        {
            desc = desc.Replace("{d}", $"<b><color=#55EFC4>{(int)aug.duration}s</color></b>");
        }

        // Kèm câu trích dẫn tàn tích Dark Fantasy (flavor text)
        string flavor = isVN ? aug.flavorVN : aug.flavorEN;
        string fullDesc = desc;
        if (!string.IsNullOrEmpty(flavor))
        {
            fullDesc += $"\n\n<size=82%><color=#94A3B8><i>{flavor}</i></color></size>";
        }

        var descObj = new GameObject("Desc");
        descObj.transform.SetParent(card.transform, false);
        var descRT = descObj.AddComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0.06f, 0.255f);
        descRT.anchorMax = new Vector2(0.94f, 0.510f);
        descRT.offsetMin = descRT.offsetMax = Vector2.zero;

        var descTMP = descObj.AddComponent<TextMeshProUGUI>();
        descTMP.text = fullDesc;
        descTMP.fontSize = 15.5f;
        descTMP.alignment = TextAlignmentOptions.Center;
        descTMP.color = new Color(0.92f, 0.95f, 1f);
        descTMP.enableWordWrapping = true;
        SoulslikeHUDStyler.SetSafeOutline(descTMP, 0.25f, new Color(0f, 0f, 0f, 0.85f));

        // 8. Ornamental Divider 2
        CreateOrnamentDivider(card.transform, "Divider2",
            new Vector2(0.08f, 0.235f), new Vector2(0.92f, 0.250f),
            tierFrameColor * 0.85f);

        // 9. Acquire Button (Nút Đá Phiến Chạm Khắc Cổ Điển)
        var btnObj = new GameObject("PickButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(card.transform, false);
        var btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.08f, 0.050f);
        btnRT.anchorMax = new Vector2(0.92f, 0.210f);
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

        var btnImg = btnObj.GetComponent<Image>();
        btnImg.sprite = GetStoneBtnSprite();
        btnImg.type = Image.Type.Sliced;
        btnImg.color = Color.Lerp(new Color(0.12f, 0.16f, 0.22f, 1f), tierFrameColor * 0.35f, 0.35f);

        var btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = btnImg;

        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.selectedColor = colors.highlightedColor;
        btn.colors = colors;

        var bOut = btnObj.AddComponent<Outline>();
        bOut.effectColor = tierFrameColor;
        bOut.effectDistance = new Vector2(1.5f, -1.5f);

        var btnTxtObj = new GameObject("BtnText");
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        var btnTxtRT = btnTxtObj.AddComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero;
        btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.offsetMin = btnTxtRT.offsetMax = Vector2.zero;

        var btnTMP = btnTxtObj.AddComponent<TextMeshProUGUI>();
        btnTMP.text = isVN ? "CHỌN NÂNG CẤP" : "ACQUIRE AUGMENT";
        btnTMP.fontSize = 17f;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = Color.white;
        SoulslikeHUDStyler.SetSafeOutline(btnTMP, 0.40f, new Color(0f, 0f, 0f, 0.95f));

        var captured = aug;
        btn.onClick.AddListener(() => PickChoice(captured));

        // Interactive hover states
        SetupCardHover(card, cardRT, cardFrameImg, tierFrameColor);
    }

    private void SetupCardHover(GameObject cardObj, RectTransform cardRT, Image cardFrame, Color normalBorderColor)
    {
        var trigger = cardObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = cardObj.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => {
            if (cardRT != null)
            {
                StartCoroutine(AnimateHover(cardRT, cardFrame, 1.035f, Color.white, 0.12f));
            }
        });

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => {
            if (cardRT != null)
            {
                StartCoroutine(AnimateHover(cardRT, cardFrame, 1.000f, normalBorderColor, 0.12f));
            }
        });

        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
    }

    private IEnumerator AnimateHover(RectTransform rt, Image cardFrame,
        float targetScale, Color targetFrameColor, float duration)
    {
        if (rt == null) yield break;

        Vector3 startScale = rt.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        Color startCardColor = cardFrame != null ? cardFrame.color : targetFrameColor;

        float t = 0f;
        while (t < duration)
        {
            if (rt == null) yield break;
            t += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(t / duration);

            rt.localScale = Vector3.Lerp(startScale, endScale, factor);
            if (cardFrame != null) cardFrame.color = Color.Lerp(startCardColor, targetFrameColor, factor);

            yield return null;
        }

        if (rt != null) rt.localScale = endScale;
        if (cardFrame != null) cardFrame.color = targetFrameColor;
    }

    // ─── Pick choice ───────────────────────────────────────────────────────────
    private void PickChoice(AugmentDatabase.AugmentData aug)
    {
        if (AugmentManager.Instance != null)
            AugmentManager.Instance.ApplyAugment(aug);

        // Hiển thị ngay biểu tượng Lõi Nâng Cấp lên màn hình trận đấu (AugmentHUDBar)
        AugmentHUDBar.EnsureExists()?.OnAugmentAcquired(aug);

        // Show floating text không có dấu ngoặc vuông "[ ]"
        DamagePopup.CreateText(
            Camera.main != null ? Camera.main.transform.position + Vector3.up * 1.5f : Vector3.up * 2f,
            LocalizationManager.IsVietnamese
                ? $"{aug.nameVN}"
                : $"{aug.nameEN}",
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

    // ─── Visual Helpers ────────────────────────────────────────────────────────
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
        bgImg.sprite = GetPanelBgSprite();
        bgImg.type = Image.Type.Sliced;
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

    private GameObject CreateOrnamentDivider(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Left half
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

        // Right half
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
