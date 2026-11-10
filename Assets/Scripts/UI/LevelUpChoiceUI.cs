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
        if (_root != null)
        {
            _root.SetActive(true);
            GameFontManager.ApplyFontsGlobally(_root);
        }

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
    public static Sprite GetCardBodySprite()
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

    public static Sprite GetOrnateCardFrameSprite()
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

    public static Sprite GetRadialGlowSprite()
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

    public static Sprite GetPillBadgeSprite()
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

    public static Sprite GetDiamondGemSprite()
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
        hbRT.anchorMin = new Vector2(0.12f, 0.855f);
        hbRT.anchorMax = new Vector2(0.88f, 0.985f);
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
        string titleStr = isVietnamese ? "THỨC TỈNH LÕI TÀN TÍCH" : "AWAKEN RUINBOUND AUGMENT";
        string subStr   = isVietnamese
            ? "Chọn 1 Lõi Nâng Cấp để khắc ấn sức mạnh vĩnh cửu hoặc thăng cấp Lõi đã sở hữu"
            : "Select 1 Augment to engrave eternal power or upgrade an owned Augment";
        titleTMP.text = $"<size=44><b><color=#FFEAA7>{titleStr}</color></b></size>\n<size=22><color=#CBD5E1>{subStr}</color></size>";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;
        titleTMP.enableWordWrapping = true;
        titleTMP.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(titleTMP, 0.14f, new Color(0.02f, 0.02f, 0.02f, 0.98f));

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

        // Tập hợp theo dõi sprite đã xuất hiện trong lượt roll này (Triệt tiêu 100% trùng hình)
        var usedSpritesInRoll = new HashSet<Sprite>();

        for (int i = 0; i < _currentChoices.Count; i++)
        {
            var aug = _currentChoices[i];
            if (aug == null) continue;
            float xMin = startX + i * (cardWidth + gap);
            float xMax = xMin + cardWidth;
            BuildCard(_cardsContainer, aug, i, isVietnamese,
                new Vector2(xMin, 0.015f), new Vector2(xMax, 0.985f), usedSpritesInRoll);
        }

        Canvas.ForceUpdateCanvases();
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
        _rerollBtnText.fontSize = 32f;
        _rerollBtnText.fontStyle = FontStyles.Bold;
        _rerollBtnText.alignment = TextAlignmentOptions.Center;
        _rerollBtnText.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(_rerollBtnText, 0.0f, Color.black);

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
        CloseAugmentConfirmPopup();

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

    // ─── Tác Phẩm Nghệ Thuật Dark Fantasy Độc Bản & Player Avatars ─────────
    private static Sprite _spriteDeityBloodBeast;
    private static Sprite _spriteDeityBerserkerWarlord;
    private static Sprite _spriteDeityCelestial;
    private static Sprite _spriteDeityBladeSaint;
    private static Sprite _spriteDeityIronTitan;
    private static Sprite _spriteDeityVoidPhantom;
    private static Sprite _spriteDeityFrostEmpress;
    private static Sprite _spriteDeityInfernoFiend;
    private static Sprite _spriteDeityVenomQueen;
    private static Sprite _spriteDeityThunderGod;
    private static Sprite _spriteDeityGrimReaper;
    private static Sprite _spriteDeityCursedDemon;
    private static Sprite _spriteDeityShieldGoddess;

    // Tác phẩm độc bản mới tạo cho từng Lõi Cụ Thể (Dedicated Augment Artworks)
    private static Sprite _spritePercentCurrentHpExecute;
    private static Sprite _spriteGoldenSanctuary;
    private static Sprite _spriteCursedMight;
    private static Sprite _spriteIroncladPact;
    private static Sprite _spriteChronoStasis;
    private static Sprite _spriteBloodTitheBlade;
    private static Sprite _spriteGlassCannonTitan;
    private static Sprite _spritePhantomRush;
    private static Sprite _spriteSoulHarvesterMultiplier;
    private static Sprite _spriteSkillSoulSurge;
    private static Sprite _spriteFlameAdaptation;
    private static Sprite _spriteGlacialSoul;
    private static Sprite _spriteAbsoluteZeroNova;

    // ─── ĐỢT 2: 15 TRANH ĐỘC BẢN HỆ CHIẾN BINH, HUYẾT TẾ & NGUYÊN TỐ ───
    private static Sprite _spriteBonusCritChance;
    private static Sprite _spriteBonusCritDamage;
    private static Sprite _spriteBonusLifesteal;
    private static Sprite _spriteVampireBlood;
    private static Sprite _spriteBloodGodTribute;
    private static Sprite _spriteBattleFrenzy;
    private static Sprite _spriteBerserkerMode;
    private static Sprite _spriteHeavyImpact;
    private static Sprite _spriteDemonicPact;
    private static Sprite _spriteStaticOverload;
    private static Sprite _spriteEchoStrike;
    private static Sprite _spriteStormDancer;
    private static Sprite _spriteMagmaEruption;
    private static Sprite _spritePoisonTouch;
    private static Sprite _spriteVirulentBloom;

    // ─── ĐỢT 3: 17 TRANH ĐỘC BẢN HỆ KIẾN TẠO, KIẾM THÁNH, THÁNH KHIÊN & TỬ THẦN ───
    private static Sprite _spriteRagingFire;
    private static Sprite _spriteAcidMembrane;
    private static Sprite _spriteSoulHarvest;
    private static Sprite _spriteDeathsTouch;
    private static Sprite _spriteDivineAegis;
    private static Sprite _spriteIronBastion;
    private static Sprite _spriteArcaneShield;
    private static Sprite _spriteBonusAttack;
    private static Sprite _spriteBonusSpeed;
    private static Sprite _spriteSwiftShadow;
    private static Sprite _spriteComboMastery;
    private static Sprite _spriteGodforgedArsenal;
    private static Sprite _spriteShadowAmbush;
    private static Sprite _spriteLaceratingEdge;
    private static Sprite _spriteFleetWind;
    private static Sprite _spriteRelentlessAssault;
    private static Sprite _spriteGiantSlayer;

    // ─── ĐỢT 4: 26 TRANH ĐỘC BẢN CUỐI CÙNG (TINH TÚ, CỰ THẦN TITAN & HƯ KHÔNG) ───
    private static Sprite _spriteBonusAbilityPower;
    private static Sprite _spriteArcaneWand;
    private static Sprite _spriteRunicEmpowerment;
    private static Sprite _spriteBonusSpellVamp;
    private static Sprite _spriteBonusCooldownReduction;
    private static Sprite _spriteBonusMaxEnergy;
    private static Sprite _spriteUltimateCharge;
    private static Sprite _spriteCelestialCascade;
    private static Sprite _spriteArcaneOverdrive;
    private static Sprite _spriteSurgingPulse;
    private static Sprite _spriteChainReaction;
    private static Sprite _spriteAscendedDivinity;
    private static Sprite _spriteEchoingVoid;

    private static Sprite _spriteBonusHealth;
    private static Sprite _spriteBonusDefense;
    private static Sprite _spriteAncientShield;
    private static Sprite _spriteRegenerativeCore;
    private static Sprite _spriteUndyingSovereign;
    private static Sprite _spriteAbundantVitality;
    private static Sprite _spriteSecondWind;
    private static Sprite _spriteLifeTransfusion;
    private static Sprite _spriteUnwaveringWill;
    private static Sprite _spritePrismaticTitan;

    private static Sprite _spriteBountyHunter;
    private static Sprite _spriteVoidTranscendence;
    private static Sprite _spriteShadowMirroring;

    private static void EnsureDeitySpritesLoaded()
    {
        if (_spriteDeityBloodBeast == null) _spriteDeityBloodBeast = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BloodBeast");
        if (_spriteDeityBerserkerWarlord == null) _spriteDeityBerserkerWarlord = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BerserkerWarlord");
        if (_spriteDeityCelestial == null) _spriteDeityCelestial = Resources.Load<Sprite>("UI/CardDeities/CardDeity_CelestialAstral");
        if (_spriteDeityBladeSaint == null) _spriteDeityBladeSaint = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BladeSaint");
        if (_spriteDeityIronTitan == null) _spriteDeityIronTitan = Resources.Load<Sprite>("UI/CardDeities/CardDeity_IronTitan");
        if (_spriteDeityVoidPhantom == null) _spriteDeityVoidPhantom = Resources.Load<Sprite>("UI/CardDeities/CardDeity_VoidPhantom");
        if (_spriteDeityFrostEmpress == null) _spriteDeityFrostEmpress = Resources.Load<Sprite>("UI/CardDeities/CardDeity_FrostEmpress");
        if (_spriteDeityInfernoFiend == null) _spriteDeityInfernoFiend = Resources.Load<Sprite>("UI/CardDeities/CardDeity_InfernoFiend");
        if (_spriteDeityVenomQueen == null) _spriteDeityVenomQueen = Resources.Load<Sprite>("UI/CardDeities/CardDeity_VenomQueen");
        if (_spriteDeityThunderGod == null) _spriteDeityThunderGod = Resources.Load<Sprite>("UI/CardDeities/CardDeity_ThunderGod");
        if (_spriteDeityGrimReaper == null) _spriteDeityGrimReaper = Resources.Load<Sprite>("UI/CardDeities/CardDeity_GrimReaper");
        if (_spriteDeityCursedDemon == null) _spriteDeityCursedDemon = Resources.Load<Sprite>("UI/CardDeities/CardDeity_CursedDemon");
        if (_spriteDeityShieldGoddess == null) _spriteDeityShieldGoddess = Resources.Load<Sprite>("UI/CardDeities/CardDeity_ShieldGoddess");

        if (_spritePercentCurrentHpExecute == null) _spritePercentCurrentHpExecute = Resources.Load<Sprite>("UI/CardDeities/CardDeity_PercentCurrentHpExecute");
        if (_spriteGoldenSanctuary == null) _spriteGoldenSanctuary = Resources.Load<Sprite>("UI/CardDeities/CardDeity_GoldenSanctuary");
        if (_spriteCursedMight == null) _spriteCursedMight = Resources.Load<Sprite>("UI/CardDeities/CardDeity_CursedMight");
        if (_spriteIroncladPact == null) _spriteIroncladPact = Resources.Load<Sprite>("UI/CardDeities/CardDeity_IroncladPact");
        if (_spriteChronoStasis == null) _spriteChronoStasis = Resources.Load<Sprite>("UI/CardDeities/CardDeity_ChronoStasis");
        if (_spriteBloodTitheBlade == null) _spriteBloodTitheBlade = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BloodTitheBlade");
        if (_spriteGlassCannonTitan == null) _spriteGlassCannonTitan = Resources.Load<Sprite>("UI/CardDeities/CardDeity_GlassCannonTitan");
        if (_spritePhantomRush == null) _spritePhantomRush = Resources.Load<Sprite>("UI/CardDeities/CardDeity_PhantomRush");
        if (_spriteSoulHarvesterMultiplier == null) _spriteSoulHarvesterMultiplier = Resources.Load<Sprite>("UI/CardDeities/CardDeity_SoulHarvesterMultiplier");
        if (_spriteSkillSoulSurge == null) _spriteSkillSoulSurge = Resources.Load<Sprite>("UI/CardDeities/CardDeity_SkillSoulSurge");
        if (_spriteFlameAdaptation == null) _spriteFlameAdaptation = Resources.Load<Sprite>("UI/CardDeities/CardDeity_FlameAdaptation");
        if (_spriteGlacialSoul == null) _spriteGlacialSoul = Resources.Load<Sprite>("UI/CardDeities/CardDeity_GlacialSoul");
        if (_spriteAbsoluteZeroNova == null) _spriteAbsoluteZeroNova = Resources.Load<Sprite>("UI/CardDeities/CardDeity_AbsoluteZeroNova");

        // Nạp Đợt 2: 15 tranh độc bản
        if (_spriteBonusCritChance == null) _spriteBonusCritChance = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BonusCritChance");
        if (_spriteBonusCritDamage == null) _spriteBonusCritDamage = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BonusCritDamage");
        if (_spriteBonusLifesteal == null) _spriteBonusLifesteal = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BonusLifesteal");
        if (_spriteVampireBlood == null) _spriteVampireBlood = Resources.Load<Sprite>("UI/CardDeities/CardDeity_VampireBlood");
        if (_spriteBloodGodTribute == null) _spriteBloodGodTribute = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BloodGodTribute");
        if (_spriteBattleFrenzy == null) _spriteBattleFrenzy = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BattleFrenzy");
        if (_spriteBerserkerMode == null) _spriteBerserkerMode = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BerserkerMode");
        if (_spriteHeavyImpact == null) _spriteHeavyImpact = Resources.Load<Sprite>("UI/CardDeities/CardDeity_HeavyImpact");
        if (_spriteDemonicPact == null) _spriteDemonicPact = Resources.Load<Sprite>("UI/CardDeities/CardDeity_DemonicPact");
        if (_spriteStaticOverload == null) _spriteStaticOverload = Resources.Load<Sprite>("UI/CardDeities/CardDeity_StaticOverload");
        if (_spriteEchoStrike == null) _spriteEchoStrike = Resources.Load<Sprite>("UI/CardDeities/CardDeity_EchoStrike");
        if (_spriteStormDancer == null) _spriteStormDancer = Resources.Load<Sprite>("UI/CardDeities/CardDeity_StormDancer");
        if (_spriteMagmaEruption == null) _spriteMagmaEruption = Resources.Load<Sprite>("UI/CardDeities/CardDeity_MagmaEruption");
        if (_spritePoisonTouch == null) _spritePoisonTouch = Resources.Load<Sprite>("UI/CardDeities/CardDeity_PoisonTouch");
        if (_spriteVirulentBloom == null) _spriteVirulentBloom = Resources.Load<Sprite>("UI/CardDeities/CardDeity_VirulentBloom");

        // Nạp Đợt 3: 17 tranh độc bản
        if (_spriteRagingFire == null) _spriteRagingFire = Resources.Load<Sprite>("UI/CardDeities/CardDeity_RagingFire");
        if (_spriteAcidMembrane == null) _spriteAcidMembrane = Resources.Load<Sprite>("UI/CardDeities/CardDeity_AcidMembrane");
        if (_spriteSoulHarvest == null) _spriteSoulHarvest = Resources.Load<Sprite>("UI/CardDeities/CardDeity_SoulHarvest");
        if (_spriteDeathsTouch == null) _spriteDeathsTouch = Resources.Load<Sprite>("UI/CardDeities/CardDeity_DeathsTouch");
        if (_spriteDivineAegis == null) _spriteDivineAegis = Resources.Load<Sprite>("UI/CardDeities/CardDeity_DivineAegis");
        if (_spriteIronBastion == null) _spriteIronBastion = Resources.Load<Sprite>("UI/CardDeities/CardDeity_IronBastion");
        if (_spriteArcaneShield == null) _spriteArcaneShield = Resources.Load<Sprite>("UI/CardDeities/CardDeity_ArcaneShield");
        if (_spriteBonusAttack == null) _spriteBonusAttack = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BonusAttack");
        if (_spriteBonusSpeed == null) _spriteBonusSpeed = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BonusSpeed");
        if (_spriteSwiftShadow == null) _spriteSwiftShadow = Resources.Load<Sprite>("UI/CardDeities/CardDeity_SwiftShadow");
        if (_spriteComboMastery == null) _spriteComboMastery = Resources.Load<Sprite>("UI/CardDeities/CardDeity_ComboMastery");
        if (_spriteGodforgedArsenal == null) _spriteGodforgedArsenal = Resources.Load<Sprite>("UI/CardDeities/CardDeity_GodforgedArsenal");
        if (_spriteShadowAmbush == null) _spriteShadowAmbush = Resources.Load<Sprite>("UI/CardDeities/CardDeity_ShadowAmbush");
        if (_spriteLaceratingEdge == null) _spriteLaceratingEdge = Resources.Load<Sprite>("UI/CardDeities/CardDeity_LaceratingEdge");
        if (_spriteFleetWind == null) _spriteFleetWind = Resources.Load<Sprite>("UI/CardDeities/CardDeity_FleetWind");
        if (_spriteRelentlessAssault == null) _spriteRelentlessAssault = Resources.Load<Sprite>("UI/CardDeities/CardDeity_RelentlessAssault");
        if (_spriteGiantSlayer == null) _spriteGiantSlayer = Resources.Load<Sprite>("UI/CardDeities/CardDeity_GiantSlayer");

        // Nạp Đợt 4: 26 tranh độc bản cuối cùng
        if (_spriteBonusAbilityPower == null) _spriteBonusAbilityPower = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BonusAbilityPower");
        if (_spriteArcaneWand == null) _spriteArcaneWand = Resources.Load<Sprite>("UI/CardDeities/CardDeity_ArcaneWand");
        if (_spriteRunicEmpowerment == null) _spriteRunicEmpowerment = Resources.Load<Sprite>("UI/CardDeities/CardDeity_RunicEmpowerment");
        if (_spriteBonusSpellVamp == null) _spriteBonusSpellVamp = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BonusSpellVamp");
        if (_spriteBonusCooldownReduction == null) _spriteBonusCooldownReduction = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BonusCooldownReduction");
        if (_spriteBonusMaxEnergy == null) _spriteBonusMaxEnergy = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BonusMaxEnergy");
        if (_spriteUltimateCharge == null) _spriteUltimateCharge = Resources.Load<Sprite>("UI/CardDeities/CardDeity_UltimateCharge");
        if (_spriteCelestialCascade == null) _spriteCelestialCascade = Resources.Load<Sprite>("UI/CardDeities/CardDeity_CelestialCascade");
        if (_spriteArcaneOverdrive == null) _spriteArcaneOverdrive = Resources.Load<Sprite>("UI/CardDeities/CardDeity_ArcaneOverdrive");
        if (_spriteSurgingPulse == null) _spriteSurgingPulse = Resources.Load<Sprite>("UI/CardDeities/CardDeity_SurgingPulse");
        if (_spriteChainReaction == null) _spriteChainReaction = Resources.Load<Sprite>("UI/CardDeities/CardDeity_ChainReaction");
        if (_spriteAscendedDivinity == null) _spriteAscendedDivinity = Resources.Load<Sprite>("UI/CardDeities/CardDeity_AscendedDivinity");
        if (_spriteEchoingVoid == null) _spriteEchoingVoid = Resources.Load<Sprite>("UI/CardDeities/CardDeity_EchoingVoid");

        if (_spriteBonusHealth == null) _spriteBonusHealth = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BonusHealth");
        if (_spriteBonusDefense == null) _spriteBonusDefense = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BonusDefense");
        if (_spriteAncientShield == null) _spriteAncientShield = Resources.Load<Sprite>("UI/CardDeities/CardDeity_AncientShield");
        if (_spriteRegenerativeCore == null) _spriteRegenerativeCore = Resources.Load<Sprite>("UI/CardDeities/CardDeity_RegenerativeCore");
        if (_spriteUndyingSovereign == null) _spriteUndyingSovereign = Resources.Load<Sprite>("UI/CardDeities/CardDeity_UndyingSovereign");
        if (_spriteAbundantVitality == null) _spriteAbundantVitality = Resources.Load<Sprite>("UI/CardDeities/CardDeity_AbundantVitality");
        if (_spriteSecondWind == null) _spriteSecondWind = Resources.Load<Sprite>("UI/CardDeities/CardDeity_SecondWind");
        if (_spriteLifeTransfusion == null) _spriteLifeTransfusion = Resources.Load<Sprite>("UI/CardDeities/CardDeity_LifeTransfusion");
        if (_spriteUnwaveringWill == null) _spriteUnwaveringWill = Resources.Load<Sprite>("UI/CardDeities/CardDeity_UnwaveringWill");
        if (_spritePrismaticTitan == null) _spritePrismaticTitan = Resources.Load<Sprite>("UI/CardDeities/CardDeity_PrismaticTitan");

        if (_spriteBountyHunter == null) _spriteBountyHunter = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BountyHunter");
        if (_spriteVoidTranscendence == null) _spriteVoidTranscendence = Resources.Load<Sprite>("UI/CardDeities/CardDeity_VoidTranscendence");
        if (_spriteShadowMirroring == null) _spriteShadowMirroring = Resources.Load<Sprite>("UI/CardDeities/CardDeity_ShadowMirroring");
    }

    public static Sprite GetAugmentCardIllustrationSprite(AugmentDatabase.AugmentData aug, HashSet<Sprite> usedSprites = null)
    {
        if (aug == null) return null;
        EnsureDeitySpritesLoaded();

        Sprite chosenSprite = null;

        // 1. Trường hợp đặc biệt: Lõi riêng của chính Player -> Hiển thị avatar/chân dung của Player đó
        if (aug.heroFilter != null && aug.heroFilter.Length > 0 && !string.IsNullOrEmpty(aug.heroFilter[0]))
        {
            chosenSprite = PlayerAvatarUI.GetHeroFaceSprite(aug.heroFilter[0]);
        }

        if (chosenSprite == null)
        {
            switch (aug.type)
            {
                // Lõi đặc quyền của 6 Tướng người chơi
                case AugmentDatabase.AugmentType.TripleMirageTornado:
                    chosenSprite = PlayerAvatarUI.GetHeroFaceSprite(CharacterManager.CHAR_DUNE_ASSASSIN); break;
                case AugmentDatabase.AugmentType.TripleRuinGale:
                    chosenSprite = PlayerAvatarUI.GetHeroFaceSprite(CharacterManager.CHAR_VAELSTRIDER); break;
                case AugmentDatabase.AugmentType.EightWayTempest:
                    chosenSprite = PlayerAvatarUI.GetHeroFaceSprite(CharacterManager.CHAR_GALE_RONIN); break;
                case AugmentDatabase.AugmentType.TwinFrostNova:
                    chosenSprite = PlayerAvatarUI.GetHeroFaceSprite(CharacterManager.CHAR_AZURE_BLADE); break;
                case AugmentDatabase.AugmentType.CataclysmicQuake:
                    chosenSprite = PlayerAvatarUI.GetHeroFaceSprite(CharacterManager.CHAR_TITAN_BERSERKER); break;
                case AugmentDatabase.AugmentType.MeteorSupercluster:
                    chosenSprite = PlayerAvatarUI.GetHeroFaceSprite(CharacterManager.CHAR_CELESTIAL_MAGE); break;

                // ─── 10 LÕI KHẾ ƯỚC & LỜI NGUYÊN (Hình vẽ độc bản riêng biệt 100%) ───
                case AugmentDatabase.AugmentType.PercentCurrentHpExecute:
                    chosenSprite = _spritePercentCurrentHpExecute; break;
                case AugmentDatabase.AugmentType.GoldenSanctuary:
                    chosenSprite = _spriteGoldenSanctuary; break;
                case AugmentDatabase.AugmentType.CursedMight:
                    chosenSprite = _spriteCursedMight; break;
                case AugmentDatabase.AugmentType.IroncladPact:
                    chosenSprite = _spriteIroncladPact; break;
                case AugmentDatabase.AugmentType.ChronoStasis:
                    chosenSprite = _spriteChronoStasis; break;
                case AugmentDatabase.AugmentType.BloodTitheBlade:
                    chosenSprite = _spriteBloodTitheBlade; break;
                case AugmentDatabase.AugmentType.GlassCannonTitan:
                    chosenSprite = _spriteGlassCannonTitan; break;
                case AugmentDatabase.AugmentType.PhantomRush:
                    chosenSprite = _spritePhantomRush; break;
                case AugmentDatabase.AugmentType.SoulHarvesterMultiplier:
                    chosenSprite = _spriteSoulHarvesterMultiplier; break;
                case AugmentDatabase.AugmentType.SkillSoulSurge:
                    chosenSprite = _spriteSkillSoulSurge; break;

                // ─── CÁC LÕI NGUYÊN TỐ CÕI TAI ƯƠNG CÓ TRANH MỚI ───
                case AugmentDatabase.AugmentType.FlameAdaptation:
                    chosenSprite = _spriteFlameAdaptation; break;
                case AugmentDatabase.AugmentType.GlacialSoul:
                    chosenSprite = _spriteGlacialSoul; break;
                case AugmentDatabase.AugmentType.AbsoluteZeroNova:
                    chosenSprite = _spriteAbsoluteZeroNova; break;

                // ─── ĐỢT 2: 15 LÕI HUYẾT TẾ, CUỒNG CHIẾN & NGUYÊN TỐ (Độc bản 100%) ───
                case AugmentDatabase.AugmentType.BonusCritChance:
                    chosenSprite = _spriteBonusCritChance; break;
                case AugmentDatabase.AugmentType.BonusCritDamage:
                    chosenSprite = _spriteBonusCritDamage; break;
                case AugmentDatabase.AugmentType.BonusLifesteal:
                    chosenSprite = _spriteBonusLifesteal; break;
                case AugmentDatabase.AugmentType.VampireBlood:
                    chosenSprite = _spriteVampireBlood; break;
                case AugmentDatabase.AugmentType.BloodGodTribute:
                    chosenSprite = _spriteBloodGodTribute; break;

                case AugmentDatabase.AugmentType.BattleFrenzy:
                    chosenSprite = _spriteBattleFrenzy; break;
                case AugmentDatabase.AugmentType.BerserkerMode:
                    chosenSprite = _spriteBerserkerMode; break;
                case AugmentDatabase.AugmentType.HeavyImpact:
                    chosenSprite = _spriteHeavyImpact; break;
                case AugmentDatabase.AugmentType.DemonicPact:
                    chosenSprite = _spriteDemonicPact; break;

                case AugmentDatabase.AugmentType.StaticOverload:
                    chosenSprite = _spriteStaticOverload; break;
                case AugmentDatabase.AugmentType.EchoStrike:
                    chosenSprite = _spriteEchoStrike; break;
                case AugmentDatabase.AugmentType.StormDancer:
                    chosenSprite = _spriteStormDancer; break;

                case AugmentDatabase.AugmentType.MagmaEruption:
                    chosenSprite = _spriteMagmaEruption; break;

                case AugmentDatabase.AugmentType.PoisonTouch:
                    chosenSprite = _spritePoisonTouch; break;
                case AugmentDatabase.AugmentType.VirulentBloom:
                    chosenSprite = _spriteVirulentBloom; break;

                // ─── ĐỢT 3: 17 LÕI HỎA NGỤC, ĐỘC TỐ, TỬ THẦN, THÁNH KHIÊN & KIẾM THÁNH (Độc bản 100%) ───
                case AugmentDatabase.AugmentType.RagingFire:
                    chosenSprite = _spriteRagingFire; break;
                case AugmentDatabase.AugmentType.AcidMembrane:
                    chosenSprite = _spriteAcidMembrane; break;

                case AugmentDatabase.AugmentType.SoulHarvest:
                    chosenSprite = _spriteSoulHarvest; break;
                case AugmentDatabase.AugmentType.DeathsTouch:
                    chosenSprite = _spriteDeathsTouch; break;

                case AugmentDatabase.AugmentType.DivineAegis:
                    chosenSprite = _spriteDivineAegis; break;
                case AugmentDatabase.AugmentType.IronBastion:
                    chosenSprite = _spriteIronBastion; break;
                case AugmentDatabase.AugmentType.ArcaneShield:
                    chosenSprite = _spriteArcaneShield; break;

                case AugmentDatabase.AugmentType.BonusAttack:
                    chosenSprite = _spriteBonusAttack; break;
                case AugmentDatabase.AugmentType.BonusSpeed:
                    chosenSprite = _spriteBonusSpeed; break;
                case AugmentDatabase.AugmentType.SwiftShadow:
                    chosenSprite = _spriteSwiftShadow; break;
                case AugmentDatabase.AugmentType.ComboMastery:
                    chosenSprite = _spriteComboMastery; break;
                case AugmentDatabase.AugmentType.GodforgedArsenal:
                    chosenSprite = _spriteGodforgedArsenal; break;
                case AugmentDatabase.AugmentType.ShadowAmbush:
                    chosenSprite = _spriteShadowAmbush; break;
                case AugmentDatabase.AugmentType.LaceratingEdge:
                    chosenSprite = _spriteLaceratingEdge; break;
                case AugmentDatabase.AugmentType.FleetWind:
                    chosenSprite = _spriteFleetWind; break;
                case AugmentDatabase.AugmentType.RelentlessAssault:
                    chosenSprite = _spriteRelentlessAssault; break;
                case AugmentDatabase.AugmentType.GiantSlayer:
                    chosenSprite = _spriteGiantSlayer; break;

                // ─── ĐỢT 4: 26 LÕI TINH TÚ, CỰ THẦN TITAN, HƯ KHÔNG & VẬN MAY (Độc bản 100% 68/68 Lõi) ───
                // Nhóm Cổ Thần Tinh Tú: AP, Hồi Chiêu, Tinh Vân & Thần Chú
                case AugmentDatabase.AugmentType.BonusAbilityPower:
                    chosenSprite = _spriteBonusAbilityPower; break;
                case AugmentDatabase.AugmentType.ArcaneWand:
                    chosenSprite = _spriteArcaneWand; break;
                case AugmentDatabase.AugmentType.RunicEmpowerment:
                    chosenSprite = _spriteRunicEmpowerment; break;
                case AugmentDatabase.AugmentType.BonusSpellVamp:
                    chosenSprite = _spriteBonusSpellVamp; break;
                case AugmentDatabase.AugmentType.BonusCooldownReduction:
                    chosenSprite = _spriteBonusCooldownReduction; break;
                case AugmentDatabase.AugmentType.BonusMaxEnergy:
                    chosenSprite = _spriteBonusMaxEnergy; break;
                case AugmentDatabase.AugmentType.UltimateCharge:
                    chosenSprite = _spriteUltimateCharge; break;
                case AugmentDatabase.AugmentType.CelestialCascade:
                    chosenSprite = _spriteCelestialCascade; break;
                case AugmentDatabase.AugmentType.ArcaneOverdrive:
                    chosenSprite = _spriteArcaneOverdrive; break;
                case AugmentDatabase.AugmentType.SurgingPulse:
                    chosenSprite = _spriteSurgingPulse; break;
                case AugmentDatabase.AugmentType.ChainReaction:
                    chosenSprite = _spriteChainReaction; break;
                case AugmentDatabase.AugmentType.AscendedDivinity:
                    chosenSprite = _spriteAscendedDivinity; break;
                case AugmentDatabase.AugmentType.EchoingVoid:
                    chosenSprite = _spriteEchoingVoid; break;

                // Nhóm Cự Thần Titan: Máu, Giáp Thủ, Hồi Phục & Thể Trọng
                case AugmentDatabase.AugmentType.BonusHealth:
                    chosenSprite = _spriteBonusHealth; break;
                case AugmentDatabase.AugmentType.BonusDefense:
                    chosenSprite = _spriteBonusDefense; break;
                case AugmentDatabase.AugmentType.AncientShield:
                    chosenSprite = _spriteAncientShield; break;
                case AugmentDatabase.AugmentType.RegenerativeCore:
                    chosenSprite = _spriteRegenerativeCore; break;
                case AugmentDatabase.AugmentType.UndyingSovereign:
                    chosenSprite = _spriteUndyingSovereign; break;
                case AugmentDatabase.AugmentType.AbundantVitality:
                    chosenSprite = _spriteAbundantVitality; break;
                case AugmentDatabase.AugmentType.SecondWind:
                    chosenSprite = _spriteSecondWind; break;
                case AugmentDatabase.AugmentType.LifeTransfusion:
                    chosenSprite = _spriteLifeTransfusion; break;
                case AugmentDatabase.AugmentType.UnwaveringWill:
                    chosenSprite = _spriteUnwaveringWill; break;
                case AugmentDatabase.AugmentType.PrismaticTitan:
                    chosenSprite = _spritePrismaticTitan; break;

                // Nhóm Vận May & Hư Không
                case AugmentDatabase.AugmentType.BountyHunter:
                    chosenSprite = _spriteBountyHunter; break;
                case AugmentDatabase.AugmentType.VoidTranscendence:
                    chosenSprite = _spriteVoidTranscendence; break;
                case AugmentDatabase.AugmentType.ShadowMirroring:
                    chosenSprite = _spriteShadowMirroring; break;
                default:
                    chosenSprite = _spriteDeityVoidPhantom; break;
            }
        }

        // ── 2. Deduplication: Triệt tiêu 100% trùng hình trong cùng 1 lần Roll ──
        if (usedSprites != null && chosenSprite != null && usedSprites.Contains(chosenSprite))
        {
            // Danh sách các ứng viên dự phòng ưu tiên theo nhánh liên quan
            Sprite[] fallbacks = new Sprite[]
            {
                _spriteDeityBerserkerWarlord,
                _spriteDeityBladeSaint,
                _spriteDeityBloodBeast,
                _spriteDeityInfernoFiend,
                _spriteDeityCursedDemon,
                _spriteDeityThunderGod,
                _spriteDeityCelestial,
                _spriteDeityIronTitan,
                _spriteDeityShieldGoddess,
                _spriteDeityFrostEmpress,
                _spriteDeityGrimReaper,
                _spriteDeityVenomQueen,
                _spriteDeityVoidPhantom
            };

            foreach (var fb in fallbacks)
            {
                if (fb != null && !usedSprites.Contains(fb))
                {
                    chosenSprite = fb;
                    break;
                }
            }
        }

        if (usedSprites != null && chosenSprite != null)
        {
            usedSprites.Add(chosenSprite);
        }

        return chosenSprite != null ? chosenSprite : _spriteDeityVoidPhantom;
    }

    // ─── Build Individual Yu-Gi-Oh Style Dark Fantasy Augment Card ────────────
    private void BuildCard(Transform parent, AugmentDatabase.AugmentData aug, int index,
        bool isVN, Vector2 anchorMin, Vector2 anchorMax, HashSet<Sprite> usedSprites = null)
    {
        Color typeColor = GetDamageTypeColor(aug.type);
        string typeName = GetDamageTypeName(aug.type, isVN);

        int currentRank = AugmentManager.Instance != null
            ? AugmentManager.Instance.GetOwnedAugmentCount(aug.type) : 0;
        bool isUpgrade = currentRank > 0;

        // Lưu giữ chính xác Sprite gốc đã gán cho thẻ bài này (tránh lệch hình khi mở popup)
        Sprite illustrationSprite = GetAugmentCardIllustrationSprite(aug, usedSprites);

        // ── 1. Tier Visual Spec (Silver / Gold / Prismatic) ───────────────────
        string tierTitle;
        Color tierFrameColor;
        Color tierBodyTint;
        Color tierRibbonBg;
        int tierGemCount;
        bool isPrismatic = (aug.rarity == AugmentDatabase.AugmentData.Rarity.Epic);

        switch (aug.rarity)
        {
            case AugmentDatabase.AugmentData.Rarity.Epic:
                tierTitle        = isVN ? "BẬC KIM CƯƠNG" : "PRISMATIC TIER";
                tierFrameColor   = new Color(0.95f, 0.52f, 1.00f, 1.00f); // Lăng kính Tinh Vân
                tierBodyTint     = new Color(0.10f, 0.07f, 0.16f, 0.98f); // Tím Hắc Ám sâu
                tierRibbonBg     = new Color(0.26f, 0.09f, 0.38f, 0.98f);
                tierGemCount     = 3;
                break;

            case AugmentDatabase.AugmentData.Rarity.Rare:
                tierTitle        = isVN ? "BẬC VÀNG" : "GOLD TIER";
                tierFrameColor   = new Color(1.00f, 0.82f, 0.26f, 1.00f); // Hoàng Kim Vương Giả
                tierBodyTint     = new Color(0.11f, 0.085f, 0.055f, 0.98f); // Đen Hổ Phách
                tierRibbonBg     = new Color(0.32f, 0.20f, 0.05f, 0.98f);
                tierGemCount     = 2;
                break;

            default:
                tierTitle        = isVN ? "BẬC BẠC" : "SILVER TIER";
                tierFrameColor   = new Color(0.82f, 0.90f, 1.00f, 0.98f); // Bạch Kim Băng Giá
                tierBodyTint     = new Color(0.065f, 0.085f, 0.125f, 0.98f); // Xanh Đá Phiến
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

        // Outer Ambient Glow behind card (Dịu nhẹ, không lóa mắt)
        var outerGlowObj = new GameObject("OuterGlow", typeof(RectTransform), typeof(Image));
        outerGlowObj.transform.SetParent(card.transform, false);
        var ogRT = outerGlowObj.GetComponent<RectTransform>();
        ogRT.anchorMin = new Vector2(-0.04f, -0.03f);
        ogRT.anchorMax = new Vector2(1.04f, 1.03f);
        ogRT.offsetMin = ogRT.offsetMax = Vector2.zero;
        var outerGlowImg = outerGlowObj.GetComponent<Image>();
        outerGlowImg.sprite = GetRadialGlowSprite();
        outerGlowImg.color = new Color(tierFrameColor.r, tierFrameColor.g, tierFrameColor.b, isPrismatic ? 0.16f : 0.08f);
        outerGlowImg.raycastTarget = false;

        // Card Body Image: Tông màu đen Obsidian sâu thẳm
        var cardBodyImg = card.GetComponent<Image>();
        cardBodyImg.sprite = GetCardBodySprite();
        cardBodyImg.type = Image.Type.Sliced;
        cardBodyImg.color = tierBodyTint;
        cardBodyImg.raycastTarget = true;

        // Ornate Metallic Double Outer Frame
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

        // ── 3. Top Header Bar (Tên Thẻ Bài + Icon Thuộc Tính ở góc trên bên phải) ──
        var headerBarObj = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
        headerBarObj.transform.SetParent(card.transform, false);
        var hbRT = headerBarObj.GetComponent<RectTransform>();
        hbRT.anchorMin = new Vector2(0.045f, 0.910f);
        hbRT.anchorMax = new Vector2(0.955f, 0.985f);
        hbRT.offsetMin = hbRT.offsetMax = Vector2.zero;
        var hbImg = headerBarObj.GetComponent<Image>();
        hbImg.sprite = GetPillBadgeSprite();
        hbImg.type = Image.Type.Sliced;
        hbImg.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);
        hbImg.raycastTarget = false;

        var hbOutline = headerBarObj.AddComponent<Outline>();
        hbOutline.effectColor = tierFrameColor * 0.85f;
        hbOutline.effectDistance = new Vector2(1f, -1f);

        // Tên Lõi Nâng Cấp
        var nameObj = new GameObject("CardName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(headerBarObj.transform, false);
        var nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.04f, 0f);
        nameRT.anchorMax = new Vector2(0.82f, 1f);
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;

        var nameTMP = nameObj.GetComponent<TextMeshProUGUI>();
        nameTMP.text = isVN ? aug.nameVN : aug.nameEN;
        nameTMP.enableAutoSizing = true;
        nameTMP.fontSizeMin = 26f;
        nameTMP.fontSizeMax = 38f;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.alignment = TextAlignmentOptions.MidlineLeft;
        nameTMP.color = new Color(1f, 0.95f, 0.82f);
        nameTMP.raycastTarget = false;
        GameFontManager.ApplyFontToText(nameTMP, true);
        SoulslikeHUDStyler.SetSafeOutline(nameTMP, 0.0f, Color.black);

        // Icon Thuộc Tính / Kỹ Năng ở góc trên bên phải thẻ bài (Chuẩn Yu-Gi-Oh Attribute Element)
        var attrBadgeObj = new GameObject("AttributeElementBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
        attrBadgeObj.transform.SetParent(headerBarObj.transform, false);
        var attrRT = attrBadgeObj.GetComponent<RectTransform>();
        attrRT.anchorMin = new Vector2(0.84f, 0.08f);
        attrRT.anchorMax = new Vector2(0.98f, 0.92f);
        attrRT.offsetMin = attrRT.offsetMax = Vector2.zero;
        var attrImg = attrBadgeObj.GetComponent<Image>();
        attrImg.sprite = GetObsidianCoreSprite();
        attrImg.color = new Color(0.05f, 0.07f, 0.12f, 1f);
        attrImg.raycastTarget = false;

        var attrOutline = attrBadgeObj.GetComponent<Outline>();
        attrOutline.effectColor = typeColor;
        attrOutline.effectDistance = new Vector2(1.2f, -1.2f);

        Sprite iconSprite = GetMedallionSprite(aug.type);
        if (iconSprite != null)
        {
            var attrIconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            attrIconObj.transform.SetParent(attrBadgeObj.transform, false);
            var aicRT = attrIconObj.GetComponent<RectTransform>();
            aicRT.anchorMin = new Vector2(0.12f, 0.12f);
            aicRT.anchorMax = new Vector2(0.88f, 0.88f);
            aicRT.offsetMin = aicRT.offsetMax = Vector2.zero;
            var aicImg = attrIconObj.GetComponent<Image>();
            aicImg.sprite = iconSprite;
            aicImg.color = Color.white;
            aicImg.preserveAspect = true;
            aicImg.raycastTarget = false;
        }

        // ── 4. Tier Ribbon & Rank Status Row (Ngay Dưới Header) ────────────────
        var statusRowObj = new GameObject("StatusRow", typeof(RectTransform));
        statusRowObj.transform.SetParent(card.transform, false);
        var srRT = statusRowObj.GetComponent<RectTransform>();
        srRT.anchorMin = new Vector2(0.05f, 0.860f);
        srRT.anchorMax = new Vector2(0.95f, 0.900f);
        srRT.offsetMin = srRT.offsetMax = Vector2.zero;

        // Tier Title Badge
        var tierBannerObj = new GameObject("TierBanner", typeof(RectTransform), typeof(Image));
        tierBannerObj.transform.SetParent(statusRowObj.transform, false);
        var tbRT = tierBannerObj.GetComponent<RectTransform>();
        tbRT.anchorMin = new Vector2(0f, 0f);
        tbRT.anchorMax = new Vector2(0.48f, 1f);
        tbRT.offsetMin = tbRT.offsetMax = Vector2.zero;
        var tbImg = tierBannerObj.GetComponent<Image>();
        tbImg.sprite = GetPillBadgeSprite();
        tbImg.type = Image.Type.Sliced;
        tbImg.color = tierRibbonBg;
        tbImg.raycastTarget = false;

        var tierTxtObj = new GameObject("TierText", typeof(RectTransform), typeof(TextMeshProUGUI));
        tierTxtObj.transform.SetParent(tierBannerObj.transform, false);
        var ttRT = tierTxtObj.GetComponent<RectTransform>();
        ttRT.anchorMin = Vector2.zero;
        ttRT.anchorMax = Vector2.one;
        ttRT.offsetMin = ttRT.offsetMax = Vector2.zero;
        var tierTxt = tierTxtObj.GetComponent<TextMeshProUGUI>();
        tierTxt.text = tierTitle;
        tierTxt.color = tierFrameColor;
        tierTxt.fontSize = 26f;
        tierTxt.alignment = TextAlignmentOptions.Center;
        tierTxt.fontStyle = FontStyles.Bold;
        tierTxt.raycastTarget = false;
        GameFontManager.ApplyFontToText(tierTxt);
        SoulslikeHUDStyler.SetSafeOutline(tierTxt, 0.0f, Color.black);

        // Rank Status Badge (LÕI MỚI hoặc THĂNG CẤP)
        var rankBadgeObj = new GameObject("RankBadge", typeof(RectTransform), typeof(Image));
        rankBadgeObj.transform.SetParent(statusRowObj.transform, false);
        var rbRT = rankBadgeObj.GetComponent<RectTransform>();
        rbRT.anchorMin = new Vector2(0.52f, 0f);
        rbRT.anchorMax = new Vector2(1f, 1f);
        rbRT.offsetMin = rbRT.offsetMax = Vector2.zero;
        var rbImg = rankBadgeObj.GetComponent<Image>();
        rbImg.sprite = GetPillBadgeSprite();
        rbImg.type = Image.Type.Sliced;
        rbImg.color = isUpgrade ? new Color(0.24f, 0.16f, 0.04f, 0.96f) : new Color(0.04f, 0.20f, 0.12f, 0.96f);
        rbImg.raycastTarget = false;

        var rbTxtObj = new GameObject("RankText", typeof(RectTransform), typeof(TextMeshProUGUI));
        rbTxtObj.transform.SetParent(rankBadgeObj.transform, false);
        var rbtRT = rbTxtObj.GetComponent<RectTransform>();
        rbtRT.anchorMin = Vector2.zero;
        rbtRT.anchorMax = Vector2.one;
        rbtRT.offsetMin = rbtRT.offsetMax = Vector2.zero;
        var rbTxt = rbTxtObj.GetComponent<TextMeshProUGUI>();
        rbTxt.text = isUpgrade
            ? (isVN ? $"CẤP {currentRank} -> {currentRank + 1}" : $"LV.{currentRank} -> {currentRank + 1}")
            : (isVN ? "LÕI MỚI" : "NEW");
        rbTxt.color = isUpgrade ? new Color(1f, 0.92f, 0.50f) : new Color(0.45f, 1f, 0.75f);
        rbTxt.fontSize = 26f;
        rbTxt.fontStyle = FontStyles.Bold;
        rbTxt.alignment = TextAlignmentOptions.Center;
        rbTxt.raycastTarget = false;
        GameFontManager.ApplyFontToText(rbTxt);
        SoulslikeHUDStyler.SetSafeOutline(rbTxt, 0.0f, Color.black);

        // ── 5. Vùng Chứa Tranh (PictureArea Container) - Mở rộng tối đa xuống dưới ──
        var picAreaObj = new GameObject("PictureArea", typeof(RectTransform));
        picAreaObj.transform.SetParent(card.transform, false);
        var paRT = picAreaObj.GetComponent<RectTransform>();
        paRT.anchorMin = new Vector2(0.05f, 0.270f);
        paRT.anchorMax = new Vector2(0.95f, 0.850f);
        paRT.offsetMin = paRT.offsetMax = Vector2.zero;

        float artRatio = 0.747f; // Mặc định tỷ lệ chuẩn 3:4 của Card Deities
        if (illustrationSprite != null && illustrationSprite.rect.height > 0)
        {
            artRatio = (float)illustrationSprite.rect.width / illustrationSprite.rect.height;
        }

        // Khung Tranh Fit Trong PictureArea: Tự động co giãn theo artRatio và ÔM KHÍT VỪA VẶN
        var picFrameObj = new GameObject("PictureFrame", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(AspectRatioFitter));
        picFrameObj.transform.SetParent(picAreaObj.transform, false);
        var pfRT = picFrameObj.GetComponent<RectTransform>();
        pfRT.anchorMin = Vector2.zero;
        pfRT.anchorMax = Vector2.one;
        pfRT.offsetMin = pfRT.offsetMax = Vector2.zero;
        pfRT.pivot = new Vector2(0.5f, 0.5f);

        var arf = picFrameObj.GetComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        arf.aspectRatio = artRatio;

        var pfImg = picFrameObj.GetComponent<Image>();
        pfImg.sprite = GetCardBodySprite();
        pfImg.type = Image.Type.Sliced;
        pfImg.color = new Color(0.03f, 0.04f, 0.06f, 1f);
        pfImg.raycastTarget = false;

        // Bức tranh nghệ thuật minh họa Lõi / Player (Hiển thị sắc nét, HỖ TRỢ PARALLAX 2.5D)
        RectTransform phantomRT = null;
        if (illustrationSprite != null)
        {
            var artObj = new GameObject("CardIllustration", typeof(RectTransform), typeof(Image));
            artObj.transform.SetParent(picFrameObj.transform, false);
            phantomRT = artObj.GetComponent<RectTransform>();
            phantomRT.anchorMin = Vector2.zero;
            phantomRT.anchorMax = Vector2.one;
            phantomRT.offsetMin = phantomRT.offsetMax = Vector2.zero;
            phantomRT.pivot = new Vector2(0.5f, 0.5f);

            var artImg = artObj.GetComponent<Image>();
            artImg.sprite = illustrationSprite;
            artImg.color = Color.white;
            artImg.preserveAspect = false; // Phủ khít khung tranh vì khung đã ôm chuẩn tỷ lệ tranh gốc bằng FitInParent trong PictureArea
            artImg.raycastTarget = false;
        }

        // Lớp Phản Quang Kim Tuyến Cầu Vồng 2.5D (Holographic Foil Shimmer)
        var holoFoilObj = new GameObject("HolographicFoil", typeof(RectTransform), typeof(Image));
        holoFoilObj.transform.SetParent(picFrameObj.transform, false);
        var holoRT = holoFoilObj.GetComponent<RectTransform>();
        holoRT.anchorMin = Vector2.zero;
        holoRT.anchorMax = Vector2.one;
        holoRT.offsetMin = holoRT.offsetMax = Vector2.zero;
        holoRT.pivot = new Vector2(0.5f, 0.5f);

        var holoImg = holoFoilObj.GetComponent<Image>();
        holoImg.sprite = Card3DParallaxTilt.GetHolographicFoilSprite();
        holoImg.color = new Color(1f, 1f, 1f, 0.12f);
        holoImg.raycastTarget = false;

        // Khung viền kim loại kép mạ vàng bao quanh bức tranh (Ôm vừa khít mép tranh từng pixel)
        var picBorderObj = new GameObject("PictureBorder", typeof(RectTransform), typeof(Image));
        picBorderObj.transform.SetParent(picFrameObj.transform, false);
        var pbRT = picBorderObj.GetComponent<RectTransform>();
        pbRT.anchorMin = Vector2.zero;
        pbRT.anchorMax = Vector2.one;
        pbRT.offsetMin = pbRT.offsetMax = Vector2.zero;
        var pbImg = picBorderObj.GetComponent<Image>();
        pbImg.sprite = GetOrnateCardFrameSprite();
        pbImg.type = Image.Type.Sliced;
        pbImg.color = tierFrameColor;
        pbImg.raycastTarget = false;
        picBorderObj.transform.SetAsLastSibling();

        // Gắn hệ thống Hiệu ứng Con quay hồi chuyển (Gyroscopic Tilt) & Parallax 2.5D cao cấp
        var tiltComp = card.AddComponent<Card3DParallaxTilt>();
        tiltComp.Setup(cardRT, phantomRT, holoImg);

        // ── 6. Hộp Mô Tả Văn Bản Riêng Biệt (Card Description Text Box - Tối ưu gọn khít sát đáy) ──
        var descBoxObj = new GameObject("DescriptionBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        descBoxObj.transform.SetParent(card.transform, false);
        var dbRT = descBoxObj.GetComponent<RectTransform>();
        dbRT.anchorMin = new Vector2(0.045f, 0.016f);
        dbRT.anchorMax = new Vector2(0.955f, 0.300f);
        dbRT.offsetMin = dbRT.offsetMax = Vector2.zero;

        var dbImg = descBoxObj.GetComponent<Image>();
        dbImg.sprite = GetCardBodySprite();
        dbImg.type = Image.Type.Sliced;
        dbImg.color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
        dbImg.raycastTarget = false;

        var dbOut = descBoxObj.GetComponent<Outline>();
        dbOut.effectColor = tierFrameColor * 0.75f;
        dbOut.effectDistance = new Vector2(1.2f, -1.2f);

        // Dòng phân loại Tag hệ kỹ năng / Đặc quyền tướng trên đầu hộp mô tả (Dùng ◆ ◆ thay dấu [ ] để không bao giờ bị chữ z)
        bool isHeroSignature = (aug.heroFilter != null && aug.heroFilter.Length > 0);
        string heroNameHeader = "";
        if (isHeroSignature)
        {
            var charData = CharacterManager.EnsureInstance()?.GetCharacter(aug.heroFilter[0]);
            heroNameHeader = charData != null ? (isVN ? charData.nameVN : charData.nameEN) : aug.heroFilter[0];
        }

        string headerTagText = isHeroSignature
            ? (isVN ? $"◆ ĐẶC QUYỀN: {heroNameHeader.ToUpper()} ◆" : $"◆ SIGNATURE: {heroNameHeader.ToUpper()} ◆")
            : (isVN ? $"◆ {typeName.ToUpper()} ◆" : $"◆ {typeName.ToUpper()} ◆");

        var tagTxtObj = new GameObject("CategoryTagText", typeof(RectTransform), typeof(TextMeshProUGUI));
        tagTxtObj.transform.SetParent(descBoxObj.transform, false);
        var ttTagRT = tagTxtObj.GetComponent<RectTransform>();
        ttTagRT.anchorMin = new Vector2(0.04f, 0.78f);
        ttTagRT.anchorMax = new Vector2(0.96f, 0.96f);
        ttTagRT.offsetMin = ttTagRT.offsetMax = Vector2.zero;

        var tagTMP = tagTxtObj.GetComponent<TextMeshProUGUI>();
        tagTMP.text = headerTagText;
        tagTMP.fontSize = 22f;
        tagTMP.fontStyle = FontStyles.Bold;
        tagTMP.alignment = TextAlignmentOptions.MidlineLeft;
        tagTMP.color = isHeroSignature ? new Color(1f, 0.85f, 0.35f) : typeColor;
        tagTMP.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(tagTMP, 0.0f, Color.black);

        // Đường kẻ trang trí trong hộp mô tả
        CreateOrnamentDivider(descBoxObj.transform, "DescInnerDivider",
            new Vector2(0.04f, 0.74f), new Vector2(0.96f, 0.76f),
            tierFrameColor * 0.65f);

        // Nội dung mô tả chỉ số + Scaling thăng cấp
        string fullDesc = FormatCardDescription(aug, currentRank, isVN);

        var descObj = new GameObject("DescContent", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(descBoxObj.transform, false);
        var descRT = descObj.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0.04f, 0.04f);
        descRT.anchorMax = new Vector2(0.96f, 0.72f);
        descRT.offsetMin = descRT.offsetMax = Vector2.zero;

        var descTMP = descObj.GetComponent<TextMeshProUGUI>();
        descTMP.text = fullDesc;
        descTMP.enableAutoSizing = true;
        descTMP.fontSizeMin = 24f;
        descTMP.fontSizeMax = 32f;
        descTMP.alignment = TextAlignmentOptions.TopLeft;
        descTMP.color = new Color(0.94f, 0.96f, 1.0f);
        descTMP.enableWordWrapping = true;
        descTMP.raycastTarget = false;
        GameFontManager.ApplyFontToText(descTMP);
        SoulslikeHUDStyler.SetSafeOutline(descTMP, 0.0f, Color.black);

        // ── 7. Toàn bộ thẻ bài là nút bấm tương tác mở Popup Xác Nhận ─────────
        var captured = aug;
        int capturedIndex = index;
        Sprite capturedSprite = illustrationSprite;

        var cardBtn = card.GetComponent<Button>();
        cardBtn.targetGraphic = cardBodyImg;
        cardBtn.onClick.AddListener(() => ShowAugmentConfirmPopup(captured, capturedIndex, isVN, capturedSprite));

        // ── 8. Animations: Entrance Flip-in, Hover Scale, & Prismatic Shimmer
        SetupCardHover(card, cardRT, cardFrameImg, outerGlowImg, null, tierFrameColor, isPrismatic);
        StartCoroutine(AnimateCardEntranceRoutine(cardRT, cardCG, index));
        StartCoroutine(AnimateCardAmbientRoutine(cardRT, phantomRT, cardFrameImg, null, outerGlowImg, tierTxt, isPrismatic, index));
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
        RectTransform phantomRT,
        Image frameImg,
        Outline bannerOutline,
        Image outerGlowImg,
        TextMeshProUGUI tierTxt,
        bool isPrismatic,
        int index)
    {
        float timeOffset = index * 0.85f;

        // Bức tranh nghệ thuật luôn đứng yên tĩnh tuyệt đối, không nhấp nhô
        if (phantomRT != null)
        {
            phantomRT.localScale = Vector3.one;
        }

        while (cardRT != null && _isShowing)
        {
            float uTime = Time.unscaledTime + timeOffset;

            // 1. Live Prismatic Shimmer for Epic/Prismatic Tier Cards (Viền lăng kính nhẹ nhàng)
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
                    outerGlowImg.color = new Color(prisCol.r, prisCol.g, prisCol.b, 0.14f + 0.04f * Mathf.Sin(uTime * 3f));
                }
            }

            yield return null;
        }
    }

    // ─── Popup Xác Nhận Lõi Nâng Cấp (Dark Fantasy Confirmation Modal) ─────────
    private GameObject _confirmPopupObj = null;

    private void ShowAugmentConfirmPopup(AugmentDatabase.AugmentData aug, int cardIndex, bool isVN, Sprite originalSprite = null)
    {
        if (_isPicking || aug == null) return;
        SoundManager.EnsureInstance()?.PlayMenuHover();

        CloseAugmentConfirmPopup();

        // 1. Root Backdrop (Chặn click ra ngoài)
        _confirmPopupObj = new GameObject("AugmentConfirmPopup", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        _confirmPopupObj.transform.SetParent(_root != null ? _root.transform : transform, false);
        _confirmPopupObj.transform.SetAsLastSibling();

        var bgRT = _confirmPopupObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        var bgImg = _confirmPopupObj.GetComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.04f, 0.88f); // Nền tối mờ huyền bí
        bgImg.raycastTarget = true;

        var cg = _confirmPopupObj.GetComponent<CanvasGroup>();

        // 2. Modal Window Container (Mở rộng kích thước cho Mobile hiển thị chữ to rõ nét)
        var modal = new GameObject("ModalWindow", typeof(RectTransform), typeof(Image), typeof(Outline));
        modal.transform.SetParent(_confirmPopupObj.transform, false);
        var mRT = modal.GetComponent<RectTransform>();
        mRT.anchorMin = new Vector2(0.5f, 0.5f);
        mRT.anchorMax = new Vector2(0.5f, 0.5f);
        mRT.sizeDelta = new Vector2(920f, 580f);
        mRT.pivot = new Vector2(0.5f, 0.5f);

        Color tierColor = aug.rarity == AugmentDatabase.AugmentData.Rarity.Epic
            ? new Color(0.95f, 0.52f, 1.00f)
            : (aug.rarity == AugmentDatabase.AugmentData.Rarity.Rare
                ? new Color(1.00f, 0.82f, 0.26f)
                : new Color(0.82f, 0.90f, 1.00f));

        var mImg = modal.GetComponent<Image>();
        mImg.sprite = GetCardBodySprite();
        mImg.type = Image.Type.Sliced;
        mImg.color = new Color(0.08f, 0.09f, 0.13f, 0.98f);
        mImg.raycastTarget = true;

        var mOut = modal.GetComponent<Outline>();
        mOut.effectColor = tierColor;
        mOut.effectDistance = new Vector2(2f, -2f);

        // Header Title (Phóng to 38pt Bold)
        var headerObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerObj.transform.SetParent(modal.transform, false);
        var hRT = headerObj.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0.05f, 0.88f);
        hRT.anchorMax = new Vector2(0.95f, 0.97f);
        hRT.offsetMin = hRT.offsetMax = Vector2.zero;

        var hTMP = headerObj.GetComponent<TextMeshProUGUI>();
        hTMP.text = isVN ? "XÁC NHẬN KHẮC ẤN LÕI" : "CONFIRM AUGMENT ENGRAVING";
        hTMP.fontSize = 38f;
        hTMP.fontStyle = FontStyles.Bold;
        hTMP.alignment = TextAlignmentOptions.Center;
        hTMP.color = new Color(1f, 0.90f, 0.45f);
        SoulslikeHUDStyler.SetSafeOutline(hTMP, 0.35f, Color.black);

        CreateOrnamentDivider(modal.transform, "ModalDivider",
            new Vector2(0.06f, 0.865f), new Vector2(0.94f, 0.875f),
            tierColor * 0.85f);

        // Bên Trái: Khung Tranh Thu Nhỏ
        var leftThumbArea = new GameObject("LeftThumbArea", typeof(RectTransform));
        leftThumbArea.transform.SetParent(modal.transform, false);
        var ltaRT = leftThumbArea.GetComponent<RectTransform>();
        ltaRT.anchorMin = new Vector2(0.05f, 0.18f);
        ltaRT.anchorMax = new Vector2(0.38f, 0.84f);
        ltaRT.offsetMin = ltaRT.offsetMax = Vector2.zero;

        // Dùng đúng Sprite ban đầu của thẻ bài (hoặc lấy trực tiếp theo aug)
        Sprite thumbSprite = originalSprite != null ? originalSprite : GetAugmentCardIllustrationSprite(aug);
        float thumbRatio = 0.747f;
        if (thumbSprite != null && thumbSprite.rect.height > 0)
        {
            thumbRatio = (float)thumbSprite.rect.width / thumbSprite.rect.height;
        }

        var leftThumb = new GameObject("LeftThumbnail", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter), typeof(Outline));
        leftThumb.transform.SetParent(leftThumbArea.transform, false);
        var ltRT = leftThumb.GetComponent<RectTransform>();
        ltRT.anchorMin = Vector2.zero;
        ltRT.anchorMax = Vector2.one;
        ltRT.offsetMin = ltRT.offsetMax = Vector2.zero;
        ltRT.pivot = new Vector2(0.5f, 0.5f);

        var ltArf = leftThumb.GetComponent<AspectRatioFitter>();
        ltArf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        ltArf.aspectRatio = thumbRatio;

        var ltImg = leftThumb.GetComponent<Image>();
        ltImg.sprite = thumbSprite;
        ltImg.preserveAspect = false;
        ltImg.color = Color.white;

        var ltOut = leftThumb.GetComponent<Outline>();
        ltOut.effectColor = tierColor;
        ltOut.effectDistance = new Vector2(1.8f, -1.8f);

        // Bên Phải: Chi Tiết Lõi (Phóng to kích thước chữ tối đa)
        var rightDetail = new GameObject("RightDetail", typeof(RectTransform));
        rightDetail.transform.SetParent(modal.transform, false);
        var rdRT = rightDetail.GetComponent<RectTransform>();
        rdRT.anchorMin = new Vector2(0.40f, 0.20f);
        rdRT.anchorMax = new Vector2(0.95f, 0.84f);
        rdRT.offsetMin = rdRT.offsetMax = Vector2.zero;

        int curRank = AugmentManager.Instance != null ? AugmentManager.Instance.GetOwnedAugmentCount(aug.type) : 0;
        bool isUp = curRank > 0;

        string detailTxt = $"<size=30><b><color=#FFEAA7>{(isVN ? aug.nameVN : aug.nameEN)}</color></b></size>\n" +
            $"<size=22><color={(isUp ? "#FBBF24" : "#55EFC4")}><b>{(isUp ? (isVN ? $"THĂNG CẤP: CẤP {curRank} -> CẤP {curRank + 1}" : $"UPGRADE: LV.{curRank} -> LV.{curRank + 1}") : (isVN ? "LÕI MỚI" : "NEW AUGMENT"))}</b></color></size>\n\n" +
            $"<size=24>{FormatCardDescription(aug, curRank, isVN)}</size>";

        var dTMPObj = new GameObject("DetailText", typeof(RectTransform), typeof(TextMeshProUGUI));
        dTMPObj.transform.SetParent(rightDetail.transform, false);
        var dtRT = dTMPObj.GetComponent<RectTransform>();
        dtRT.anchorMin = Vector2.zero;
        dtRT.anchorMax = Vector2.one;
        dtRT.offsetMin = dtRT.offsetMax = Vector2.zero;

        var dTMP = dTMPObj.GetComponent<TextMeshProUGUI>();
        dTMP.text = detailTxt;
        dTMP.fontSize = 24f;
        dTMP.enableWordWrapping = true;
        dTMP.color = Color.white;
        SoulslikeHUDStyler.SetSafeOutline(dTMP, 0.35f, Color.black);

        // Bottom 2 Buttons: QUAY LẠI (Hủy) & XÁC NHẬN (Phóng to 30pt Bold)
        // Nút Quay Lại
        var btnBack = new GameObject("BtnBack", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        btnBack.transform.SetParent(modal.transform, false);
        var bbRT = btnBack.GetComponent<RectTransform>();
        bbRT.anchorMin = new Vector2(0.10f, 0.04f);
        bbRT.anchorMax = new Vector2(0.46f, 0.16f);
        bbRT.offsetMin = bbRT.offsetMax = Vector2.zero;

        var bbImg = btnBack.GetComponent<Image>();
        bbImg.sprite = GetPillBadgeSprite();
        bbImg.type = Image.Type.Sliced;
        bbImg.color = new Color(0.18f, 0.20f, 0.25f, 1f);

        var bbOut = btnBack.GetComponent<Outline>();
        bbOut.effectColor = new Color(0.60f, 0.65f, 0.72f);
        bbOut.effectDistance = new Vector2(1.2f, -1.2f);

        var bbTxt = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        bbTxt.transform.SetParent(btnBack.transform, false);
        var bbtRT = bbTxt.GetComponent<RectTransform>();
        bbtRT.anchorMin = Vector2.zero;
        bbtRT.anchorMax = Vector2.one;
        bbtRT.offsetMin = bbtRT.offsetMax = Vector2.zero;
        var bbtTMP = bbTxt.GetComponent<TextMeshProUGUI>();
        bbtTMP.text = isVN ? "QUAY LẠI" : "BACK";
        bbtTMP.fontSize = 30f;
        bbtTMP.fontStyle = FontStyles.Bold;
        bbtTMP.alignment = TextAlignmentOptions.Center;
        bbtTMP.color = new Color(0.85f, 0.88f, 0.95f);
        SoulslikeHUDStyler.SetSafeOutline(bbtTMP, 0.35f, Color.black);

        var bBackBtn = btnBack.GetComponent<Button>();
        bBackBtn.onClick.AddListener(() => {
            SoundManager.EnsureInstance()?.PlayMenuHover();
            CloseAugmentConfirmPopup();
        });

        // Nút Xác Nhận
        var btnConfirm = new GameObject("BtnConfirm", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        btnConfirm.transform.SetParent(modal.transform, false);
        var bcRT = btnConfirm.GetComponent<RectTransform>();
        bcRT.anchorMin = new Vector2(0.54f, 0.04f);
        bcRT.anchorMax = new Vector2(0.90f, 0.16f);
        bcRT.offsetMin = bcRT.offsetMax = Vector2.zero;

        var bcImg = btnConfirm.GetComponent<Image>();
        bcImg.sprite = GetPillBadgeSprite();
        bcImg.type = Image.Type.Sliced;
        bcImg.color = Color.Lerp(new Color(0.12f, 0.22f, 0.36f, 1f), tierColor * 0.65f, 0.45f);

        var bcOut = btnConfirm.GetComponent<Outline>();
        bcOut.effectColor = tierColor;
        bcOut.effectDistance = new Vector2(1.5f, -1.5f);

        var bcTxt = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        bcTxt.transform.SetParent(btnConfirm.transform, false);
        var bctRT = bcTxt.GetComponent<RectTransform>();
        bctRT.anchorMin = Vector2.zero;
        bctRT.anchorMax = Vector2.one;
        bctRT.offsetMin = bctRT.offsetMax = Vector2.zero;
        var bctTMP = bcTxt.GetComponent<TextMeshProUGUI>();
        bctTMP.text = isVN ? "XÁC NHẬN KHẮC ẤN" : "CONFIRM";
        bctTMP.fontSize = 30f;
        bctTMP.fontStyle = FontStyles.Bold;
        bctTMP.alignment = TextAlignmentOptions.Center;
        bctTMP.color = Color.white;
        SoulslikeHUDStyler.SetSafeOutline(bctTMP, 0.35f, Color.black);

        var bConfBtn = btnConfirm.GetComponent<Button>();
        bConfBtn.onClick.AddListener(() => {
            CloseAugmentConfirmPopup();
            OnCardSelected(aug, cardIndex);
        });

        GameFontManager.ApplyFontsGlobally(modal);

        // Entrance scale
        StartCoroutine(AnimateModalEntrance(mRT, cg));
    }

    private void CloseAugmentConfirmPopup()
    {
        if (_confirmPopupObj != null)
        {
            Destroy(_confirmPopupObj);
            _confirmPopupObj = null;
        }
    }

    private IEnumerator AnimateModalEntrance(RectTransform mRT, CanvasGroup cg)
    {
        if (mRT == null) yield break;
        float dur = 0.18f;
        float elapsed = 0f;
        Vector3 fromScale = new Vector3(0.88f, 0.88f, 1f);
        while (elapsed < dur)
        {
            if (mRT == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            mRT.localScale = Vector3.Lerp(fromScale, Vector3.one, ease);
            if (cg != null) cg.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        if (mRT != null) mRT.localScale = Vector3.one;
        if (cg != null) cg.alpha = 1f;
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
        CloseAugmentConfirmPopup();

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
