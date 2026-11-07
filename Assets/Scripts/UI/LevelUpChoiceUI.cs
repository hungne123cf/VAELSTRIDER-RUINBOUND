using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;

/// <summary>
/// Giao diện chọn Lõi Nâng Cấp (Augments) thế hệ mới - Phong cách Hades / Đấu Trường Chân Lý (TFT) & Dark Fantasy:
/// - Thiết kế Thẻ bài (Tarot Relic Card) phân tầng rõ rệt theo Bậc: BẠC (Silver), VÀNG (Gold), KIM CƯƠNG (Prismatic chuyển sắc cầu vồng động).
/// - Huy hiệu trung tâm cỡ lớn kèm Hào Quang Nguyên Tố (Elemental Halo Glow) thở nhịp nhàng theo loại chỉ số.
/// - Huy hiệu trạng thái thông minh: Báo hiệu "MỚI" (New) hoặc "CẤP X -> CẤP Y" kèm so sánh chỉ số trước/sau khi nâng cấp.
/// - Tự động tô sáng các con số / phần trăm quan trọng trong mô tả để đọc nhanh trong 1 giây.
/// - Hiệu ứng lật thẻ xuất hiện tuần tự (Staggered Flip-in), phóng to khi chạm/rê chuột và lóe sáng khi chọn (dùng unscaledDeltaTime).
/// </summary>
public class LevelUpChoiceUI : MonoBehaviour
{
    // ─── Singleton ─────────────────────────────────────────────────────────────
    private static LevelUpChoiceUI _instance;

    // ─── Runtime ───────────────────────────────────────────────────────────────
    private GameObject _root;
    private bool _isShowing = false;
    private bool _isPicking = false;
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
    private Image _rerollBtnBg;
    private Transform _cardsContainer;
    private readonly List<CanvasGroup> _spawnedCardGroups = new List<CanvasGroup>();
    private readonly List<RectTransform> _spawnedCardRects = new List<RectTransform>();

    // Danh sách lõi đã xuất hiện trong phiên lên cấp này (chống trùng lặp khi roll lại - Anti-Repeat chuẩn TFT)
    private readonly HashSet<AugmentDatabase.AugmentType> _sessionSeenAugments = new HashSet<AugmentDatabase.AugmentType>();

    // Queue lên cấp liên tiếp (khi giết Boss nhận nhiều EXP)
    private static readonly Queue<int> _pendingLevelUps = new Queue<int>();

    // ─── Procedural & Loaded Sprite Cache ──────────────────────────────────────
    private static Sprite _cardBodySprite;
    private static Sprite _cardFrameSprite;
    private static Sprite _radialGlowSprite;
    private static Sprite _pillBadgeSprite;
    private static Sprite _diamondGemSprite;
    private static Sprite _dividerSprite;
    private static Sprite _runicRingSprite;
    private static Sprite _obsidianCoreSprite;
    private static Sprite _cachedAttackIcon;

    public static void ResetUI()
    {
        _pendingLevelUps.Clear();
        if (_instance != null)
        {
            _instance.StopAllCoroutines();
            _instance._isShowing = false;
            _instance._isPicking = false;
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

    // ─── Core UI Coroutine ─────────────────────────────────────────────────────
    private IEnumerator ShowRoutine(int level)
    {
        _isShowing = true;
        _isPicking = false;
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

    // ─── Procedural High-Contrast UI Sprites ───────────────────────────────────
    private static Sprite GetCardBodySprite()
    {
        if (_cardBodySprite != null) return _cardBodySprite;

        int w = 96, h = 144;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] px = new Color[w * h];
        float cornerR = 10f;

        for (int y = 0; y < h; y++)
        {
            float yf = (float)y / (h - 1);
            // Vertical gradient: slightly brighter at top arch, deep velvet obsidian in middle/bottom
            float vShade = Mathf.Lerp(0.72f, 1.0f, Mathf.Pow(yf, 0.85f));

            for (int x = 0; x < w; x++)
            {
                // Rounded corner distance check
                float dx = 0f, dy = 0f;
                if (x < cornerR) dx = cornerR - x;
                else if (x > w - 1 - cornerR) dx = x - (w - 1 - cornerR);

                if (y < cornerR) dy = cornerR - y;
                else if (y > h - 1 - cornerR) dy = y - (h - 1 - cornerR);

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > cornerR + 0.8f)
                {
                    px[y * w + x] = Color.clear;
                    continue;
                }

                float alpha = Mathf.Clamp01(cornerR + 0.8f - dist);
                // Subtle center-horizontal highlight
                float xf = Mathf.Abs((x / (float)(w - 1)) - 0.5f) * 2f;
                float hShade = Mathf.Lerp(1.06f, 0.88f, xf * xf);

                float val = Mathf.Clamp01(vShade * hShade);
                px[y * w + x] = new Color(val, val, val, alpha);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        _cardBodySprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        return _cardBodySprite;
    }

    private static Sprite GetOrnateCardFrameSprite()
    {
        if (_cardFrameSprite != null) return _cardFrameSprite;

        int w = 96, h = 144;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] px = new Color[w * h];
        float cornerR = 10f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = 0f, dy = 0f;
                if (x < cornerR) dx = cornerR - x;
                else if (x > w - 1 - cornerR) dx = x - (w - 1 - cornerR);

                if (y < cornerR) dy = cornerR - y;
                else if (y > h - 1 - cornerR) dy = y - (h - 1 - cornerR);

                float cornerDist = Mathf.Sqrt(dx * dx + dy * dy);
                if (cornerDist > cornerR + 0.8f)
                {
                    px[y * w + x] = Color.clear;
                    continue;
                }

                int edgeDist = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                if (dx > 0f && dy > 0f)
                {
                    edgeDist = Mathf.RoundToInt(cornerR - cornerDist);
                }

                float a = 0f;
                float b = 1f;

                // Outer thick metallic bevel (0..3px)
                if (edgeDist >= 0 && edgeDist <= 3)
                {
                    a = edgeDist == 0 ? 0.85f : 1.0f;
                    b = edgeDist == 1 ? 1.0f : 0.88f;
                }
                // Inner filigree hairline (7..8px)
                else if (edgeDist == 7 || edgeDist == 8)
                {
                    a = 0.58f;
                    b = 0.92f;
                }
                // Corner ornate diagonal filigree accent
                bool inCornerZone = (x < 22 || x > w - 23) && (y < 22 || y > h - 23);
                if (inCornerZone && (edgeDist >= 4 && edgeDist <= 10))
                {
                    int cx = x < 22 ? 12 : w - 13;
                    int cy = y < 22 ? 12 : h - 13;
                    int manhattan = Mathf.Abs(x - cx) + Mathf.Abs(y - cy);
                    if (manhattan <= 6)
                    {
                        a = Mathf.Max(a, 0.95f);
                        b = 1.0f;
                    }
                }

                px[y * w + x] = new Color(b, b, b, a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        _cardFrameSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
        return _cardFrameSprite;
    }

    private static Sprite GetRadialGlowSprite()
    {
        if (_radialGlowSprite != null) return _radialGlowSprite;

        int sz = 96;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] px = new Color[sz * sz];
        float c = (sz - 1) * 0.5f;
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a); // Smoothstep falloff
                px[y * sz + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        _radialGlowSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
        return _radialGlowSprite;
    }

    private static Sprite GetPillBadgeSprite()
    {
        if (_pillBadgeSprite != null) return _pillBadgeSprite;

        int w = 64, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] px = new Color[w * h];
        float r = 10f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = 0f, dy = 0f;
                if (x < r) dx = r - x;
                else if (x > w - 1 - r) dx = x - (w - 1 - r);
                if (y < r) dy = r - y;
                else if (y > h - 1 - r) dy = y - (h - 1 - r);

                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > r + 0.6f)
                {
                    px[y * w + x] = Color.clear;
                }
                else
                {
                    float a = Mathf.Clamp01(r + 0.6f - d);
                    bool isBorder = (d >= r - 1.8f) || x <= 1 || x >= w - 2 || y <= 1 || y >= h - 2;
                    float b = isBorder ? 1.0f : Mathf.Lerp(0.78f, 0.94f, (float)y / (h - 1));
                    px[y * w + x] = new Color(b, b, b, a);
                }
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        _pillBadgeSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
        return _pillBadgeSprite;
    }

    private static Sprite GetDiamondGemSprite()
    {
        if (_diamondGemSprite != null) return _diamondGemSprite;

        int sz = 32;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] px = new Color[sz * sz];
        float c = (sz - 1) * 0.5f;
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float md = (Mathf.Abs(x - c) + Mathf.Abs(y - c)) / (c * 0.90f);
                if (md > 1.05f)
                {
                    px[y * sz + x] = Color.clear;
                }
                else
                {
                    float a = Mathf.Clamp01((1.05f - md) * 6f);
                    float facet = (x < c && y > c) ? 1.0f : ((x > c && y < c) ? 0.78f : 0.90f);
                    if (md < 0.35f) facet = 1.0f;
                    px[y * sz + x] = new Color(facet, facet, facet, a);
                }
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        _diamondGemSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), 100f);
        return _diamondGemSprite;
    }

    private static Sprite GetDividerSprite()
    {
        if (_dividerSprite != null) return _dividerSprite;
        _dividerSprite = SoulslikeHUDStyler.GetGothicDividerSprite();
        if (_dividerSprite != null) return _dividerSprite;
        _dividerSprite = Resources.Load<Sprite>("UI/divider_fade_004");
        return _dividerSprite;
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
        Sprite s = AugmentIconHelper.GetAugmentSprite(type);
        if (s != null) return s;

        if (_cachedAttackIcon == null)
            _cachedAttackIcon = Resources.Load<Sprite>("UI/Augments/sword_slash");
        return _cachedAttackIcon;
    }

    // ─── Build UI (Atmosphere + Header + 3 Tarot Cards + Crystal Reroll) ──────
    private void BuildUI(int level)
    {
        if (_root != null) Destroy(_root);

        AugmentIconHelper.ClearCache();

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

        // ── 1. Atmospheric Dark Fantasy Vignette Backdrop ─────────────────────
        var overlay = CreatePanel(_root.transform, new Color(0.015f, 0.020f, 0.035f, 0.86f), Vector2.zero, Vector2.one);
        overlay.name = "OverlayBackdrop";

        // Soft Ambient Sanctuary Glow behind cards
        var centerAuraObj = new GameObject("SanctuaryAmbientGlow", typeof(RectTransform), typeof(Image));
        centerAuraObj.transform.SetParent(overlay.transform, false);
        var caRT = centerAuraObj.GetComponent<RectTransform>();
        caRT.anchorMin = new Vector2(0.08f, 0.08f);
        caRT.anchorMax = new Vector2(0.92f, 0.92f);
        caRT.offsetMin = caRT.offsetMax = Vector2.zero;
        var caImg = centerAuraObj.GetComponent<Image>();
        caImg.sprite = GetRadialGlowSprite();
        caImg.color = new Color(0.28f, 0.20f, 0.45f, 0.28f);
        caImg.raycastTarget = false;

        // ── 2. Royal Floating Header Banner ───────────────────────────────────
        var headerBanner = new GameObject("HeaderBanner", typeof(RectTransform), typeof(Image));
        headerBanner.transform.SetParent(overlay.transform, false);
        var hbRT = headerBanner.GetComponent<RectTransform>();
        hbRT.anchorMin = new Vector2(0.18f, 0.865f);
        hbRT.anchorMax = new Vector2(0.82f, 0.978f);
        hbRT.offsetMin = hbRT.offsetMax = Vector2.zero;
        var hbImg = headerBanner.GetComponent<Image>();
        hbImg.sprite = GetPillBadgeSprite();
        hbImg.type = Image.Type.Sliced;
        hbImg.color = new Color(0.05f, 0.07f, 0.12f, 0.92f);
        hbImg.raycastTarget = false;

        var hbOutline = headerBanner.AddComponent<Outline>();
        hbOutline.effectColor = new Color(0.96f, 0.80f, 0.35f, 0.70f);
        hbOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Title Text
        var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(headerBanner.transform, false);
        var titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.04f, 0.06f);
        titleRT.anchorMax = new Vector2(0.96f, 0.94f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        var titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        string titleStr = isVietnamese ? $"THỨC TỈNH LÕI TÀN TÍCH  -  CẤP {level}" : $"AWAKEN RUINBOUND AUGMENT  -  LV.{level}";
        string subStr   = isVietnamese
            ? "Chọn 1 Lõi Nâng Cấp để khắc ấn sức mạnh vĩnh cửu hoặc thăng cấp Lõi đã sở hữu"
            : "Select 1 Augment to engrave eternal power or upgrade an owned Augment";
        titleTMP.text = $"<size=32><b><color=#FFEAA7>{titleStr}</color></b></size>\n<size=15><color=#CBD5E1>{subStr}</color></size>";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;
        titleTMP.enableWordWrapping = true;
        titleTMP.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(titleTMP, 0.42f, new Color(0.02f, 0.02f, 0.02f, 0.98f));

        // Flanking gold gems on header
        CreateGemIcon(headerBanner.transform, "LeftCrestGem", new Vector2(0.035f, 0.5f), 22f, new Color(1f, 0.84f, 0.32f, 0.95f));
        CreateGemIcon(headerBanner.transform, "RightCrestGem", new Vector2(0.965f, 0.5f), 22f, new Color(1f, 0.84f, 0.32f, 0.95f));

        // Ornamental divider under header
        CreateOrnamentDivider(overlay.transform, "HeaderDivider",
            new Vector2(0.22f, 0.848f), new Vector2(0.78f, 0.862f),
            new Color(0.98f, 0.82f, 0.38f, 0.85f));

        // ── 3. Cards Container ─────────────────────────────────────────────────
        var cardsContainerObj = new GameObject("CardsContainer", typeof(RectTransform));
        cardsContainerObj.transform.SetParent(overlay.transform, false);
        var ccRT = cardsContainerObj.GetComponent<RectTransform>();
        ccRT.anchorMin = new Vector2(0.05f, 0.115f);
        ccRT.anchorMax = new Vector2(0.95f, 0.840f);
        ccRT.offsetMin = ccRT.offsetMax = Vector2.zero;
        _cardsContainer = cardsContainerObj.transform;

        BuildCards(isVietnamese);

        // ── 4. Sculpted Crystal Reroll Button ─────────────────────────────────
        BuildRerollButton(overlay.transform, isVietnamese);
    }

    private void CreateGemIcon(Transform parent, string name, Vector2 anchorPos, float size, Color color)
    {
        var gemObj = new GameObject(name, typeof(RectTransform), typeof(Image));
        gemObj.transform.SetParent(parent, false);
        var rt = gemObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        var img = gemObj.GetComponent<Image>();
        img.sprite = GetDiamondGemSprite();
        img.color = color;
        img.raycastTarget = false;
    }

    private void BuildCards(bool isVietnamese)
    {
        _spawnedCardGroups.Clear();
        _spawnedCardRects.Clear();

        for (int i = _cardsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_cardsContainer.GetChild(i).gameObject);
        }

        if (_currentChoices == null || _currentChoices.Count == 0) return;

        float cardWidth = 0.296f;
        float startX    = 0.024f;
        float gap       = (1f - startX * 2f - cardWidth * 3f) / 2f;

        for (int i = 0; i < _currentChoices.Count; i++)
        {
            var aug = _currentChoices[i];
            if (aug == null) continue;
            float xMin = startX + i * (cardWidth + gap);
            float xMax = xMin + cardWidth;
            BuildCard(_cardsContainer, aug, i, isVietnamese,
                new Vector2(xMin, 0.015f), new Vector2(xMax, 0.985f));
        }
    }

    private void BuildRerollButton(Transform parent, bool isVN)
    {
        var btnObj = new GameObject("RerollButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        var btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.32f, 0.018f);
        btnRT.anchorMax = new Vector2(0.68f, 0.095f);
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

        _rerollBtnBg = btnObj.GetComponent<Image>();
        _rerollBtnBg.sprite = GetPillBadgeSprite();
        _rerollBtnBg.type = Image.Type.Sliced;
        _rerollBtnBg.color = new Color(0.10f, 0.14f, 0.24f, 0.98f);

        _rerollBtn = btnObj.GetComponent<Button>();
        _rerollBtn.targetGraphic = _rerollBtnBg;

        var outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(1.00f, 0.82f, 0.32f, 0.95f);
        outline.effectDistance = new Vector2(1.8f, -1.8f);

        // Left Crystal/Repeat Icon
        Sprite rerollIcon = WhiteIconLoader.GetSprite("Repeat-Right");
        if (rerollIcon == null) rerollIcon = GetDiamondGemSprite();

        var iconObj = new GameObject("RerollIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(btnObj.transform, false);
        var iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.06f, 0.20f);
        iconRT.anchorMax = new Vector2(0.16f, 0.80f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

        var iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = rerollIcon;
        iconImg.color = new Color(0.35f, 0.92f, 1.00f, 0.98f);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        var txtObj = new GameObject("RerollText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        var txtRT = txtObj.GetComponent<RectTransform>();
        txtRT.anchorMin = new Vector2(0.15f, 0f);
        txtRT.anchorMax = new Vector2(0.96f, 1f);
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;

        _rerollBtnText = txtObj.GetComponent<TextMeshProUGUI>();
        _rerollBtnText.fontSize = 21f;
        _rerollBtnText.fontStyle = FontStyles.Bold;
        _rerollBtnText.alignment = TextAlignmentOptions.Center;
        _rerollBtnText.raycastTarget = false;
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
            if (_rerollBtnBg != null) _rerollBtnBg.color = new Color(0.11f, 0.16f, 0.28f, 0.98f);
            string label = isVN
                ? $"ĐỔI LẠI LÕI  <color=#52F0FF>({_matchRemainingRerolls}/{MAX_MATCH_REROLLS} LƯỢT)</color>"
                : $"REROLL AUGMENTS  <color=#52F0FF>({_matchRemainingRerolls}/{MAX_MATCH_REROLLS} LEFT)</color>";
            _rerollBtnText.text = label;
            _rerollBtnText.color = Color.white;
        }
        else
        {
            _rerollBtn.interactable = false;
            if (_rerollBtnBg != null) _rerollBtnBg.color = new Color(0.08f, 0.09f, 0.12f, 0.85f);
            string label = isVN ? $"ĐÃ HẾT LƯỢT ĐỔI (0/{MAX_MATCH_REROLLS})" : $"NO REROLLS LEFT (0/{MAX_MATCH_REROLLS})";
            _rerollBtnText.text = label;
            _rerollBtnText.color = new Color(0.55f, 0.58f, 0.65f, 1f);
        }
    }

    private void OnRerollClicked(bool isVN)
    {
        if (_isPicking || _matchRemainingRerolls <= 0)
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
            new Color(0.35f, 0.92f, 1.0f)
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
                return new Color(0.98f, 0.30f, 0.25f); // Sát thương Vật Lý: Đỏ Chiến Binh
            case AugmentDatabase.AugmentType.BonusSpellVamp:
            case AugmentDatabase.AugmentType.EchoStrike:
            case AugmentDatabase.AugmentType.ArcaneOverdrive:
            case AugmentDatabase.AugmentType.CelestialCascade:
            case AugmentDatabase.AugmentType.StaticOverload:
            case AugmentDatabase.AugmentType.EchoingVoid:
                return new Color(0.76f, 0.38f, 1.00f); // Phép Thuật / Tinh Tú: Tím Ma Pháp
            case AugmentDatabase.AugmentType.BonusCritChance:
            case AugmentDatabase.AugmentType.BonusCritDamage:
            case AugmentDatabase.AugmentType.ShadowAmbush:
            case AugmentDatabase.AugmentType.ChainReaction:
                return new Color(1.00f, 0.84f, 0.16f); // Chí Mạng: Vàng Kim Hoàng Triều
            case AugmentDatabase.AugmentType.BonusLifesteal:
            case AugmentDatabase.AugmentType.VampireBlood:
            case AugmentDatabase.AugmentType.BloodGodTribute:
            case AugmentDatabase.AugmentType.DeathsTouch:
            case AugmentDatabase.AugmentType.LifeTransfusion:
                return new Color(0.96f, 0.18f, 0.30f); // Huyết Tế / Hút Máu: Đỏ Huyết
            case AugmentDatabase.AugmentType.PoisonTouch:
                return new Color(0.16f, 0.92f, 0.46f); // Độc Dược / Ăn Mòn: Lục Độc Acid
            case AugmentDatabase.AugmentType.BonusHealth:
            case AugmentDatabase.AugmentType.RegenerativeCore:
            case AugmentDatabase.AugmentType.SoulHarvest:
            case AugmentDatabase.AugmentType.AbundantVitality:
            case AugmentDatabase.AugmentType.SecondWind:
                return new Color(0.22f, 0.92f, 0.50f); // Sinh Lực: Lục Bảo
            case AugmentDatabase.AugmentType.BonusSpeed:
            case AugmentDatabase.AugmentType.SwiftShadow:
            case AugmentDatabase.AugmentType.FleetWind:
            case AugmentDatabase.AugmentType.StormDancer:
                return new Color(0.25f, 0.85f, 1.00f); // Nhanh Nhẹn / Phong Lôi: Băng Lam
            case AugmentDatabase.AugmentType.BonusDefense:
            case AugmentDatabase.AugmentType.AncientShield:
            case AugmentDatabase.AugmentType.IronBastion:
            case AugmentDatabase.AugmentType.DivineAegis:
            case AugmentDatabase.AugmentType.ArcaneShield:
            case AugmentDatabase.AugmentType.UnwaveringWill:
            case AugmentDatabase.AugmentType.PrismaticTitan:
                return new Color(0.38f, 0.70f, 1.00f); // Phòng Thủ: Lam Khiên
            case AugmentDatabase.AugmentType.BountyHunter:
                return new Color(1.00f, 0.84f, 0.24f); // Tài Phú / Tinh Thể
            case AugmentDatabase.AugmentType.BonusCooldownReduction:
            case AugmentDatabase.AugmentType.UltimateCharge:
            case AugmentDatabase.AugmentType.ComboMastery:
            case AugmentDatabase.AugmentType.SurgingPulse:
            case AugmentDatabase.AugmentType.BonusMaxEnergy:
                return new Color(1.00f, 0.68f, 0.22f); // Hồi Chiêu / Nội Năng: Hổ Phách
            case AugmentDatabase.AugmentType.DemonicPact:
            case AugmentDatabase.AugmentType.UndyingSovereign:
            case AugmentDatabase.AugmentType.GodforgedArsenal:
            case AugmentDatabase.AugmentType.AscendedDivinity:
            case AugmentDatabase.AugmentType.PercentCurrentHpExecute:
                return new Color(0.98f, 0.28f, 0.24f);
            case AugmentDatabase.AugmentType.GoldenSanctuary:
                return new Color(1.00f, 0.85f, 0.22f);
            case AugmentDatabase.AugmentType.CursedMight:
                return new Color(0.96f, 0.18f, 0.28f);
            case AugmentDatabase.AugmentType.IroncladPact:
                return new Color(0.38f, 0.70f, 1.00f);
            case AugmentDatabase.AugmentType.ChronoStasis:
                return new Color(0.15f, 0.90f, 0.90f);
            case AugmentDatabase.AugmentType.BloodTitheBlade:
                return new Color(0.96f, 0.18f, 0.24f);
            case AugmentDatabase.AugmentType.GlassCannonTitan:
                return new Color(1.00f, 0.82f, 0.16f);
            case AugmentDatabase.AugmentType.PhantomRush:
                return new Color(0.72f, 0.40f, 0.98f);
            default:
                return new Color(0.96f, 0.48f, 0.92f);
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
            case AugmentDatabase.AugmentType.PercentCurrentHpExecute:
                return isVN ? "TRẢM SÁT" : "EXECUTE";
            case AugmentDatabase.AugmentType.GoldenSanctuary:
                return isVN ? "KIM THÂN" : "SANCTUARY";
            case AugmentDatabase.AugmentType.CursedMight:
                return isVN ? "NGUYỀN RỦA" : "CURSED";
            case AugmentDatabase.AugmentType.IroncladPact:
                return isVN ? "MINH ƯỚC" : "IRONCLAD";
            case AugmentDatabase.AugmentType.ChronoStasis:
                return isVN ? "THỜI KHÔNG" : "CHRONO";
            case AugmentDatabase.AugmentType.BloodTitheBlade:
                return isVN ? "HUYẾT TẾ" : "BLOOD TITHE";
            case AugmentDatabase.AugmentType.GlassCannonTitan:
                return isVN ? "THỦY TINH" : "GLASS CANNON";
            case AugmentDatabase.AugmentType.PhantomRush:
                return isVN ? "BÓNG MA" : "PHANTOM";
            default:
                return isVN ? "THẦN THOẠI" : "MYTHIC";
        }
    }

    public static string GetStatShortCode(AugmentDatabase.AugmentType type)
    {
        return AugmentIconHelper.GetAugmentShortCode(type);
    }

    // ─── Build Individual Tarot / Hades Augment Card ───────────────────────────
    private void BuildCard(Transform parent, AugmentDatabase.AugmentData aug, int index,
        bool isVN, Vector2 anchorMin, Vector2 anchorMax)
    {
        Color typeColor = GetDamageTypeColor(aug.type);
        string typeName = GetDamageTypeName(aug.type, isVN);

        int currentRank = AugmentManager.Instance != null
            ? AugmentManager.Instance.GetAugmentRank(aug.type) : 0;
        bool isUpgrade = currentRank > 0;

        // ── 1. Tier Visual Spec (Silver / Gold / Prismatic) ───────────────────
        string tierTitle;
        Color tierFrameColor;
        Color tierBodyTint;
        Color tierTopArchColor;
        Color tierRibbonBg;
        int tierGemCount;
        bool isPrismatic = (aug.rarity == AugmentDatabase.AugmentData.Rarity.Epic);

        switch (aug.rarity)
        {
            case AugmentDatabase.AugmentData.Rarity.Epic:
                tierTitle        = isVN ? "BẬC KIM CƯƠNG" : "PRISMATIC TIER";
                tierFrameColor   = new Color(0.95f, 0.52f, 1.00f, 1.00f); // Lăng kính Tinh Vân
                tierBodyTint     = new Color(0.11f, 0.08f, 0.19f, 0.98f); // Tím Hắc Ám sâu
                tierTopArchColor = new Color(0.55f, 0.22f, 0.78f, 0.45f);
                tierRibbonBg     = new Color(0.26f, 0.09f, 0.38f, 0.98f);
                tierGemCount     = 3;
                break;

            case AugmentDatabase.AugmentData.Rarity.Rare:
                tierTitle        = isVN ? "BẬC VÀNG" : "GOLD TIER";
                tierFrameColor   = new Color(1.00f, 0.82f, 0.26f, 1.00f); // Hoàng Kim Vương Giả
                tierBodyTint     = new Color(0.12f, 0.095f, 0.065f, 0.98f); // Đen Hổ Phách
                tierTopArchColor = new Color(0.75f, 0.50f, 0.12f, 0.40f);
                tierRibbonBg     = new Color(0.32f, 0.20f, 0.05f, 0.98f);
                tierGemCount     = 2;
                break;

            default:
                tierTitle        = isVN ? "BẬC BẠC" : "SILVER TIER";
                tierFrameColor   = new Color(0.82f, 0.90f, 1.00f, 0.98f); // Bạch Kim Băng Giá
                tierBodyTint     = new Color(0.075f, 0.105f, 0.155f, 0.98f); // Xanh Đá Phiến
                tierTopArchColor = new Color(0.32f, 0.52f, 0.78f, 0.36f);
                tierRibbonBg     = new Color(0.12f, 0.18f, 0.28f, 0.98f);
                tierGemCount     = 1;
                break;
        }

        // ── 2. Card Root + CanvasGroup + Whole-Card Clickable Button ──────────
        var card = new GameObject($"Card_{index}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button));
        card.transform.SetParent(parent, false);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = anchorMin;
        cardRT.anchorMax = anchorMax;
        cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;
        cardRT.pivot = new Vector2(0.5f, 0.5f);

        var cardCG = card.GetComponent<CanvasGroup>();
        _spawnedCardGroups.Add(cardCG);
        _spawnedCardRects.Add(cardRT);

        // Outer Ambient Glow behind card
        var outerGlowObj = new GameObject("OuterGlow", typeof(RectTransform), typeof(Image));
        outerGlowObj.transform.SetParent(card.transform, false);
        var ogRT = outerGlowObj.GetComponent<RectTransform>();
        ogRT.anchorMin = new Vector2(-0.08f, -0.06f);
        ogRT.anchorMax = new Vector2(1.08f, 1.06f);
        ogRT.offsetMin = ogRT.offsetMax = Vector2.zero;
        var outerGlowImg = outerGlowObj.GetComponent<Image>();
        outerGlowImg.sprite = GetRadialGlowSprite();
        outerGlowImg.color = new Color(tierFrameColor.r, tierFrameColor.g, tierFrameColor.b, isPrismatic ? 0.42f : 0.20f);
        outerGlowImg.raycastTarget = false;

        // Card Body Image
        var cardBodyImg = card.GetComponent<Image>();
        cardBodyImg.sprite = GetCardBodySprite();
        cardBodyImg.type = Image.Type.Sliced;
        cardBodyImg.color = tierBodyTint;
        cardBodyImg.raycastTarget = true;

        // Top Arch Inner Glow inside card
        var topArchObj = new GameObject("TopHeaderArchGlow", typeof(RectTransform), typeof(Image));
        topArchObj.transform.SetParent(card.transform, false);
        var taRT = topArchObj.GetComponent<RectTransform>();
        taRT.anchorMin = new Vector2(0.03f, 0.52f);
        taRT.anchorMax = new Vector2(0.97f, 0.97f);
        taRT.offsetMin = taRT.offsetMax = Vector2.zero;
        var topArchImg = topArchObj.GetComponent<Image>();
        topArchImg.sprite = GetRadialGlowSprite();
        topArchImg.color = tierTopArchColor;
        topArchImg.raycastTarget = false;

        // Ornate Metallic Double Frame
        var frameObj = new GameObject("FrameBorder", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(card.transform, false);
        var frameRT = frameObj.GetComponent<RectTransform>();
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.offsetMin = frameRT.offsetMax = Vector2.zero;
        var cardFrameImg = frameObj.GetComponent<Image>();
        cardFrameImg.sprite = GetOrnateCardFrameSprite();
        cardFrameImg.type = Image.Type.Sliced;
        cardFrameImg.color = tierFrameColor;
        cardFrameImg.raycastTarget = false;

        // ── 3. Top Tier Ribbon ("BẬC BẠC / VÀNG / KIM CƯƠNG" + Gems) ─────────
        var tierBannerObj = new GameObject("TierBanner", typeof(RectTransform), typeof(Image), typeof(Outline));
        tierBannerObj.transform.SetParent(card.transform, false);
        var tbRT = tierBannerObj.GetComponent<RectTransform>();
        tbRT.anchorMin = new Vector2(0.06f, 0.922f);
        tbRT.anchorMax = new Vector2(0.94f, 0.978f);
        tbRT.offsetMin = tbRT.offsetMax = Vector2.zero;

        var tbImg = tierBannerObj.GetComponent<Image>();
        tbImg.sprite = GetPillBadgeSprite();
        tbImg.type = Image.Type.Sliced;
        tbImg.color = tierRibbonBg;
        tbImg.raycastTarget = false;

        var tbOutline = tierBannerObj.GetComponent<Outline>();
        tbOutline.effectColor = tierFrameColor;
        tbOutline.effectDistance = new Vector2(1.2f, -1.2f);

        var tierTxtObj = new GameObject("TierText", typeof(RectTransform), typeof(TextMeshProUGUI));
        tierTxtObj.transform.SetParent(tierBannerObj.transform, false);
        var ttRT = tierTxtObj.GetComponent<RectTransform>();
        ttRT.anchorMin = Vector2.zero;
        ttRT.anchorMax = Vector2.one;
        ttRT.offsetMin = ttRT.offsetMax = Vector2.zero;

        var tierTxt = tierTxtObj.GetComponent<TextMeshProUGUI>();
        tierTxt.text = tierTitle;
        tierTxt.color = tierFrameColor;
        tierTxt.fontSize = 13.5f;
        tierTxt.alignment = TextAlignmentOptions.Center;
        tierTxt.fontStyle = FontStyles.Bold;
        tierTxt.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(tierTxt, 0.35f, Color.black);

        // Tier Gems on left & right of TierBanner
        for (int g = 0; g < tierGemCount; g++)
        {
            float offset = 0.065f + g * 0.042f;
            CreateGemIcon(tierBannerObj.transform, $"GemL_{g}", new Vector2(offset, 0.5f), 13f, tierFrameColor);
            CreateGemIcon(tierBannerObj.transform, $"GemR_{g}", new Vector2(1f - offset, 0.5f), 13f, tierFrameColor);
        }

        // ── 4. Rank-Up / New Badge (Idea 3: "MỚI" hoặc "CẤP X -> CẤP Y") ─────
        var rankBadgeObj = new GameObject("RankStatusBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
        rankBadgeObj.transform.SetParent(card.transform, false);
        var rbRT = rankBadgeObj.GetComponent<RectTransform>();
        rbRT.anchorMin = new Vector2(0.20f, 0.862f);
        rbRT.anchorMax = new Vector2(0.80f, 0.912f);
        rbRT.offsetMin = rbRT.offsetMax = Vector2.zero;

        Color badgeBorderCol = isUpgrade
            ? new Color(1.00f, 0.86f, 0.28f, 1.0f)   // Vàng kim thăng cấp
            : new Color(0.22f, 0.95f, 0.62f, 0.95f); // Lục bảo lõi mới
        Color badgeBgCol = isUpgrade
            ? new Color(0.26f, 0.18f, 0.04f, 0.96f)
            : new Color(0.05f, 0.22f, 0.15f, 0.96f);

        var rbImg = rankBadgeObj.GetComponent<Image>();
        rbImg.sprite = GetPillBadgeSprite();
        rbImg.type = Image.Type.Sliced;
        rbImg.color = badgeBgCol;
        rbImg.raycastTarget = false;

        var rbOut = rankBadgeObj.GetComponent<Outline>();
        rbOut.effectColor = badgeBorderCol;
        rbOut.effectDistance = new Vector2(1.2f, -1.2f);

        var rbTxtObj = new GameObject("RankStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        rbTxtObj.transform.SetParent(rankBadgeObj.transform, false);
        var rbtRT = rbTxtObj.GetComponent<RectTransform>();
        rbtRT.anchorMin = Vector2.zero;
        rbtRT.anchorMax = Vector2.one;
        rbtRT.offsetMin = rbtRT.offsetMax = Vector2.zero;

        var rbTxt = rbTxtObj.GetComponent<TextMeshProUGUI>();
        rbTxt.text = isUpgrade
            ? (isVN ? $"THĂNG CẤP:  CẤP {currentRank}  ->  CẤP {currentRank + 1}" : $"UPGRADE:  LV.{currentRank}  ->  LV.{currentRank + 1}")
            : (isVN ? "LÕI MỚI  -  CẤP 1" : "NEW AUGMENT  -  LV.1");
        rbTxt.color = isUpgrade ? new Color(1f, 0.94f, 0.55f) : new Color(0.55f, 1f, 0.80f);
        rbTxt.fontSize = 12.5f;
        rbTxt.fontStyle = FontStyles.Bold;
        rbTxt.alignment = TextAlignmentOptions.Center;
        rbTxt.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(rbTxt, 0.35f, Color.black);

        // ── 5. Central Medallion + Breathing Elemental Halo (Idea 1) ──────────
        var haloObj = new GameObject("ElementalHalo", typeof(RectTransform), typeof(Image));
        haloObj.transform.SetParent(card.transform, false);
        var haloRT = haloObj.GetComponent<RectTransform>();
        haloRT.anchorMin = new Vector2(0.5f, 0.720f);
        haloRT.anchorMax = new Vector2(0.5f, 0.720f);
        haloRT.sizeDelta = new Vector2(154f, 154f);
        haloRT.pivot = new Vector2(0.5f, 0.5f);
        var haloImg = haloObj.GetComponent<Image>();
        haloImg.sprite = GetRadialGlowSprite();
        haloImg.color = new Color(typeColor.r, typeColor.g, typeColor.b, 0.48f);
        haloImg.raycastTarget = false;

        var medallionObj = new GameObject("Medallion", typeof(RectTransform), typeof(Image), typeof(Outline));
        medallionObj.transform.SetParent(card.transform, false);
        var medRT = medallionObj.GetComponent<RectTransform>();
        medRT.anchorMin = new Vector2(0.5f, 0.720f);
        medRT.anchorMax = new Vector2(0.5f, 0.720f);
        medRT.sizeDelta = new Vector2(106f, 106f);
        medRT.pivot = new Vector2(0.5f, 0.5f);

        var coreImg = medallionObj.GetComponent<Image>();
        coreImg.sprite = GetObsidianCoreSprite();
        coreImg.color = new Color(0.07f, 0.09f, 0.14f, 1f);
        coreImg.raycastTarget = false;

        var medOutline = medallionObj.GetComponent<Outline>();
        medOutline.effectColor = tierFrameColor;
        medOutline.effectDistance = new Vector2(1.8f, -1.8f);

        // Runic Ring
        var ringObj = new GameObject("RunicRing", typeof(RectTransform), typeof(Image));
        ringObj.transform.SetParent(medallionObj.transform, false);
        var ringRT = ringObj.GetComponent<RectTransform>();
        ringRT.anchorMin = Vector2.zero;
        ringRT.anchorMax = Vector2.one;
        ringRT.offsetMin = ringRT.offsetMax = Vector2.zero;
        var ringImg = ringObj.GetComponent<Image>();
        ringImg.sprite = GetRunicRingSprite();
        ringImg.color = Color.Lerp(tierFrameColor, Color.white, 0.35f);
        ringImg.raycastTarget = false;

        // Full-Color High-Detail Augment Icon (Color.white preserves full shading & gold/gem highlights!)
        Sprite iconSprite = GetMedallionSprite(aug.type);
        if (iconSprite != null)
        {
            var iconImgObj = new GameObject("IconImg", typeof(RectTransform), typeof(Image));
            iconImgObj.transform.SetParent(medallionObj.transform, false);
            var iconRT = iconImgObj.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.08f, 0.08f);
            iconRT.anchorMax = new Vector2(0.92f, 0.92f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

            var iconImg = iconImgObj.GetComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }
        else
        {
            var iconTxtObj = new GameObject("IconText", typeof(RectTransform), typeof(TextMeshProUGUI));
            iconTxtObj.transform.SetParent(medallionObj.transform, false);
            var iconRT = iconTxtObj.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

            var iconTMP = iconTxtObj.AddComponent<TextMeshProUGUI>();
            iconTMP.text = GetStatShortCode(aug.type);
            iconTMP.fontSize = 20f;
            iconTMP.fontStyle = FontStyles.Bold;
            iconTMP.alignment = TextAlignmentOptions.Center;
            iconTMP.color = typeColor;
            iconTMP.raycastTarget = false;
            SoulslikeHUDStyler.SetSafeOutline(iconTMP, 0.35f, Color.black);
        }

        // ── 6. Elemental Category Pill Tag (Below Medallion) ──────────────────
        var statTagObj = new GameObject("StatTag", typeof(RectTransform), typeof(Image), typeof(Outline));
        statTagObj.transform.SetParent(card.transform, false);
        var stRT = statTagObj.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0.25f, 0.565f);
        stRT.anchorMax = new Vector2(0.75f, 0.612f);
        stRT.offsetMin = stRT.offsetMax = Vector2.zero;

        var stImg = statTagObj.GetComponent<Image>();
        stImg.sprite = GetPillBadgeSprite();
        stImg.type = Image.Type.Sliced;
        stImg.color = new Color(0.04f, 0.06f, 0.10f, 0.96f);
        stImg.raycastTarget = false;

        var stOutline = statTagObj.GetComponent<Outline>();
        stOutline.effectColor = new Color(typeColor.r, typeColor.g, typeColor.b, 0.90f);
        stOutline.effectDistance = new Vector2(1.2f, -1.2f);

        var stTxtObj = new GameObject("StatTagText", typeof(RectTransform), typeof(TextMeshProUGUI));
        stTxtObj.transform.SetParent(statTagObj.transform, false);
        var sttRT = stTxtObj.GetComponent<RectTransform>();
        sttRT.anchorMin = Vector2.zero;
        sttRT.anchorMax = Vector2.one;
        sttRT.offsetMin = sttRT.offsetMax = Vector2.zero;

        var stTxt = stTxtObj.GetComponent<TextMeshProUGUI>();
        stTxt.text = typeName;
        stTxt.color = typeColor;
        stTxt.fontSize = 12.5f;
        stTxt.alignment = TextAlignmentOptions.Center;
        stTxt.fontStyle = FontStyles.Bold;
        stTxt.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(stTxt, 0.30f, Color.black);

        // ── 7. Augment Name (Tên Lõi Nâng Cấp) ───────────────────────────────
        var nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(card.transform, false);
        var nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.05f, 0.465f);
        nameRT.anchorMax = new Vector2(0.95f, 0.558f);
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

        var nameTMP = nameObj.GetComponent<TextMeshProUGUI>();
        nameTMP.text = isVN ? aug.nameVN : aug.nameEN;
        nameTMP.enableAutoSizing = true;
        nameTMP.fontSizeMin = 17f;
        nameTMP.fontSizeMax = 22f;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.color = new Color(1f, 0.97f, 0.90f);
        nameTMP.enableWordWrapping = true;
        nameTMP.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(nameTMP, 0.42f, new Color(0.03f, 0.03f, 0.05f, 0.98f));

        // Ornamental Divider 1
        CreateOrnamentDivider(card.transform, "Divider1",
            new Vector2(0.08f, 0.448f), new Vector2(0.92f, 0.460f),
            tierFrameColor * 0.85f);

        // ── 8. Augment Description with Auto-Highlighted Numbers & Rank Scaling
        string fullDesc = FormatCardDescription(aug, currentRank, isVN);

        var descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(card.transform, false);
        var descRT = descObj.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0.06f, 0.175f);
        descRT.anchorMax = new Vector2(0.94f, 0.442f);
        descRT.offsetMin = descRT.offsetMax = Vector2.zero;

        var descTMP = descObj.GetComponent<TextMeshProUGUI>();
        descTMP.text = fullDesc;
        descTMP.enableAutoSizing = true;
        descTMP.fontSizeMin = 12.5f;
        descTMP.fontSizeMax = 16.5f;
        descTMP.alignment = TextAlignmentOptions.Center;
        descTMP.color = new Color(0.92f, 0.95f, 1.0f);
        descTMP.enableWordWrapping = true;
        descTMP.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(descTMP, 0.28f, new Color(0f, 0f, 0f, 0.90f));

        // Ornamental Divider 2
        CreateOrnamentDivider(card.transform, "Divider2",
            new Vector2(0.08f, 0.158f), new Vector2(0.92f, 0.170f),
            tierFrameColor * 0.85f);

        // ── 9. Bottom Action Button ("CHỌN LÕI" / "NÂNG CẤP") ─────────────────
        var btnObj = new GameObject("PickButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        btnObj.transform.SetParent(card.transform, false);
        var btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.07f, 0.032f);
        btnRT.anchorMax = new Vector2(0.93f, 0.145f);
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

        var btnImg = btnObj.GetComponent<Image>();
        btnImg.sprite = GetPillBadgeSprite();
        btnImg.type = Image.Type.Sliced;
        btnImg.color = Color.Lerp(new Color(0.10f, 0.15f, 0.24f, 1f), tierFrameColor * 0.45f, 0.42f);

        var bOut = btnObj.GetComponent<Outline>();
        bOut.effectColor = tierFrameColor;
        bOut.effectDistance = new Vector2(1.5f, -1.5f);

        var btnTxtObj = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        var btnTxtRT = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero;
        btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.offsetMin = btnTxtRT.offsetMax = Vector2.zero;

        var btnTMP = btnTxtObj.GetComponent<TextMeshProUGUI>();
        btnTMP.text = isUpgrade
            ? (isVN ? $"NÂNG LÊN CẤP {currentRank + 1}" : $"UPGRADE TO LV.{currentRank + 1}")
            : (isVN ? "KHẮC ẤN LÕI NÀY" : "ACQUIRE AUGMENT");
        btnTMP.fontSize = 18.5f;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = Color.white;
        btnTMP.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(btnTMP, 0.40f, new Color(0f, 0f, 0f, 0.95f));

        var captured = aug;
        int capturedIndex = index;

        var btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(() => OnCardSelected(captured, capturedIndex));

        var cardBtn = card.GetComponent<Button>();
        cardBtn.targetGraphic = cardBodyImg;
        cardBtn.onClick.AddListener(() => OnCardSelected(captured, capturedIndex));

        // ── 10. Animations: Entrance Flip-in, Hover Scale, & Ambient/Prismatic
        SetupCardHover(card, cardRT, cardFrameImg, outerGlowImg, btnImg, tierFrameColor, isPrismatic);
        StartCoroutine(AnimateCardEntranceRoutine(cardRT, cardCG, index));
        StartCoroutine(AnimateCardAmbientRoutine(cardRT, haloRT, haloImg, cardFrameImg, tbOutline, outerGlowImg, tierTxt, isPrismatic, index));
    }

    private string FormatCardDescription(AugmentDatabase.AugmentData aug, int currentRank, bool isVN)
    {
        string desc = isVN ? aug.descVN : aug.descEN;
        if (string.IsNullOrEmpty(desc)) desc = aug.descVN ?? aug.descEN ?? "";

        float baseVal = aug.value;
        string valStr = baseVal % 1 == 0 ? ((int)baseVal).ToString() : baseVal.ToString("F1");

        if (currentRank > 0)
        {
            float curTotal = baseVal * currentRank;
            float nextTotal = baseVal * (currentRank + 1);
            string curStr = curTotal % 1 == 0 ? ((int)curTotal).ToString() : curTotal.ToString("F1");
            string nextStr = nextTotal % 1 == 0 ? ((int)nextTotal).ToString() : nextTotal.ToString("F1");
            desc = desc.Replace("{v}", $"<b><color=#94A3B8>{curStr}</color> -> <color=#55EFC4>{nextStr}</color></b>");
        }
        else
        {
            desc = desc.Replace("{v}", $"<b><color=#FFEAA7>{valStr}</color></b>");
        }

        if (aug.duration > 0)
        {
            desc = desc.Replace("{d}", $"<b><color=#52F0FF>{(int)aug.duration}s</color></b>");
        }

        // Auto-highlight standalone numbers/percentages not already wrapped in rich text tags
        if (!desc.Contains("<color="))
        {
            desc = Regex.Replace(desc, @"(\+?\d+(?:[\.,]\d+)?%?s?)", "<color=#FFEAA7><b>$1</b></color>");
        }

        string flavor = isVN ? aug.flavorVN : aug.flavorEN;
        if (!string.IsNullOrEmpty(flavor))
        {
            desc += $"\n\n<size=80%><color=#94A3B8><i>\"{flavor}\"</i></color></size>";
        }

        return desc;
    }

    // ─── Entrance, Ambient Prismatic & Hover Animations (unscaledDeltaTime) ────
    private IEnumerator AnimateCardEntranceRoutine(RectTransform cardRT, CanvasGroup cardCG, int index)
    {
        if (cardRT == null || cardCG == null) yield break;

        cardCG.alpha = 0f;
        cardRT.localScale = new Vector3(0.06f, 0.90f, 1f);

        float delay = index * 0.09f;
        float tDelay = 0f;
        while (tDelay < delay)
        {
            if (cardRT == null) yield break;
            tDelay += Time.unscaledDeltaTime;
            yield return null;
        }

        float duration = 0.24f;
        float t = 0f;
        while (t < duration)
        {
            if (cardRT == null) yield break;
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            // EaseOutBack for crisp card flip-in feel
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float easeOutBack = 1f + c3 * Mathf.Pow(p - 1f, 3f) + c1 * Mathf.Pow(p - 1f, 2f);

            cardCG.alpha = Mathf.Clamp01(p * 1.6f);
            float sx = Mathf.LerpUnclamped(0.06f, 1.0f, easeOutBack);
            float sy = Mathf.Lerp(0.90f, 1.0f, p);
            cardRT.localScale = new Vector3(sx, sy, 1f);
            yield return null;
        }

        if (cardRT != null) cardRT.localScale = Vector3.one;
        if (cardCG != null) cardCG.alpha = 1f;
    }

    private IEnumerator AnimateCardAmbientRoutine(
        RectTransform cardRT,
        RectTransform haloRT,
        Image haloImg,
        Image frameImg,
        Outline bannerOutline,
        Image outerGlowImg,
        TextMeshProUGUI tierTxt,
        bool isPrismatic,
        int index)
    {
        float timeOffset = index * 0.85f;
        Color baseHaloCol = haloImg != null ? haloImg.color : Color.white;

        while (cardRT != null && _isShowing)
        {
            float uTime = Time.unscaledTime + timeOffset;

            // 1. Breathing Elemental Medallion Halo
            if (haloRT != null)
            {
                float pulse = 1.0f + 0.08f * Mathf.Sin(uTime * 3.2f);
                haloRT.localScale = new Vector3(pulse, pulse, 1f);
                if (haloImg != null)
                {
                    float a = Mathf.Clamp01(baseHaloCol.a + 0.12f * Mathf.Sin(uTime * 3.2f));
                    haloImg.color = new Color(baseHaloCol.r, baseHaloCol.g, baseHaloCol.b, a);
                }
            }

            // 2. Live Prismatic Shimmer for Epic/Prismatic Tier Cards
            if (isPrismatic)
            {
                // Cycle smoothly between Magenta-Rose, Celestial Cyan, and Imperial Gold
                float hueWave = (Mathf.Sin(uTime * 2.4f) + 1f) * 0.5f;
                Color cA = new Color(1.00f, 0.45f, 0.96f, 1f); // Rose Magenta
                Color cB = new Color(0.35f, 0.95f, 1.00f, 1f); // Astral Cyan
                Color cC = new Color(1.00f, 0.88f, 0.42f, 1f); // Imperial Gold

                Color prisCol = hueWave < 0.5f
                    ? Color.Lerp(cA, cB, hueWave * 2f)
                    : Color.Lerp(cB, cC, (hueWave - 0.5f) * 2f);

                if (frameImg != null) frameImg.color = prisCol;
                if (bannerOutline != null) bannerOutline.effectColor = prisCol;
                if (tierTxt != null) tierTxt.color = prisCol;
                if (outerGlowImg != null)
                {
                    outerGlowImg.color = new Color(prisCol.r, prisCol.g, prisCol.b, 0.42f + 0.12f * Mathf.Sin(uTime * 4f));
                }
            }

            yield return null;
        }
    }

    private void SetupCardHover(GameObject cardObj, RectTransform cardRT, Image cardFrame,
        Image outerGlow, Image btnImg, Color normalBorderColor, bool isPrismatic)
    {
        var trigger = cardObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = cardObj.AddComponent<EventTrigger>();

        Color normalBtnCol = btnImg != null ? btnImg.color : Color.white;
        Color hoverBtnCol = Color.Lerp(normalBtnCol, Color.white, 0.28f);

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => {
            if (cardRT != null && !_isPicking)
            {
                if (btnImg != null) btnImg.color = hoverBtnCol;
                StartCoroutine(AnimateHover(cardRT, cardFrame, outerGlow, 1.052f, Color.white, 0.52f, 0.11f, isPrismatic));
            }
        });

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => {
            if (cardRT != null && !_isPicking)
            {
                if (btnImg != null) btnImg.color = normalBtnCol;
                StartCoroutine(AnimateHover(cardRT, cardFrame, outerGlow, 1.000f, normalBorderColor, isPrismatic ? 0.42f : 0.20f, 0.12f, isPrismatic));
            }
        });

        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
    }

    private IEnumerator AnimateHover(RectTransform rt, Image cardFrame, Image outerGlow,
        float targetScale, Color targetFrameColor, float targetGlowAlpha, float duration, bool isPrismatic)
    {
        if (rt == null) yield break;

        Vector3 startScale = rt.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        Color startCardColor = cardFrame != null ? cardFrame.color : targetFrameColor;
        float startGlowA = outerGlow != null ? outerGlow.color.a : targetGlowAlpha;

        float t = 0f;
        while (t < duration)
        {
            if (rt == null || _isPicking) yield break;
            t += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(t / duration);

            rt.localScale = Vector3.Lerp(startScale, endScale, factor);
            if (!isPrismatic && cardFrame != null)
            {
                cardFrame.color = Color.Lerp(startCardColor, targetFrameColor, factor);
            }
            if (outerGlow != null)
            {
                Color gc = outerGlow.color;
                gc.a = Mathf.Lerp(startGlowA, targetGlowAlpha, factor);
                outerGlow.color = gc;
            }

            yield return null;
        }

        if (rt != null && !_isPicking) rt.localScale = endScale;
    }

    // ─── Card Selection Flash & Acquire ────────────────────────────────────────
    private void OnCardSelected(AugmentDatabase.AugmentData aug, int chosenIndex)
    {
        if (_isPicking || aug == null) return;
        _isPicking = true;
        StartCoroutine(PickChoiceRoutine(aug, chosenIndex));
    }

    private IEnumerator PickChoiceRoutine(AugmentDatabase.AugmentData aug, int chosenIndex)
    {
        SoundManager.EnsureInstance()?.PlayUpgradeSuccess();

        // Animate chosen card pulsing up & flashing while other 2 cards shrink/fade out
        float dur = 0.16f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);

            for (int i = 0; i < _spawnedCardRects.Count; i++)
            {
                var rt = _spawnedCardRects[i];
                var cg = i < _spawnedCardGroups.Count ? _spawnedCardGroups[i] : null;
                if (rt == null) continue;

                if (i == chosenIndex)
                {
                    float s = Mathf.Lerp(1.04f, 1.09f, Mathf.Sin(p * Mathf.PI));
                    rt.localScale = new Vector3(s, s, 1f);
                }
                else
                {
                    rt.localScale = Vector3.one * Mathf.Lerp(1.0f, 0.90f, p);
                    if (cg != null) cg.alpha = 1f - p;
                }
            }
            yield return null;
        }

        PickChoice(aug);
    }

    private void PickChoice(AugmentDatabase.AugmentData aug)
    {
        if (AugmentManager.Instance != null)
            AugmentManager.Instance.ApplyAugment(aug);

        // Hiển thị ngay biểu tượng Lõi Nâng Cấp lên màn hình trận đấu (AugmentHUDBar)
        AugmentHUDBar.EnsureExists()?.OnAugmentAcquired(aug);

        DamagePopup.CreateText(
            Camera.main != null ? Camera.main.transform.position + Vector3.up * 1.5f : Vector3.up * 2f,
            LocalizationManager.IsVietnamese
                ? $"{aug.nameVN}"
                : $"{aug.nameEN}",
            new Color(1.0f, 0.92f, 0.45f)
        );

        _isShowing = false;
        _isPicking = false;
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
        var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        return go;
    }

    private GameObject CreateOrnamentDivider(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var left = new GameObject("Left", typeof(RectTransform), typeof(Image));
        left.transform.SetParent(go.transform, false);
        var leftRT = left.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0f, 0f);
        leftRT.anchorMax = new Vector2(0.5f, 1f);
        leftRT.offsetMin = leftRT.offsetMax = Vector2.zero;
        var leftImg = left.GetComponent<Image>();
        leftImg.sprite = GetDividerSprite();
        leftImg.color = color;
        leftImg.raycastTarget = false;

        var right = new GameObject("Right", typeof(RectTransform), typeof(Image));
        right.transform.SetParent(go.transform, false);
        var rightRT = right.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.5f, 0f);
        rightRT.anchorMax = new Vector2(1f, 1f);
        rightRT.offsetMin = rightRT.offsetMax = Vector2.zero;
        rightRT.localScale = new Vector3(-1f, 1f, 1f);
        var rightImg = right.GetComponent<Image>();
        rightImg.sprite = GetDividerSprite();
        rightImg.color = color;
        rightImg.raycastTarget = false;

        return go;
    }
}
