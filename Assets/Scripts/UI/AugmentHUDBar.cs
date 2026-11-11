using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// Hộp Popup thu gọn / mở rộng hiển thị các Lõi Nâng Cấp (Augments) trên màn hình trận đấu (Gameplay HUD).
/// - Thiết kế đồng bộ 100% với Bảng Thống Kê Chỉ Số (PlayerStatPanelUI):
///   + Khi thu gọn: Chỉ hiển thị 1 nút Icon tròn Lõi Cổ Ngữ (52x52, nền đá đen, viền vàng đồng, biểu tượng Tinh Thể Lõi) kèm số lượng Lõi đang sở hữu.
///   + Khi bấm mở rộng: Hiện bảng kính mờ Dark Fantasy chứa toàn bộ các Icon Lõi Nâng Cấp đang kích hoạt, bấm lại vào nút Icon góc phải để đóng lại bất cứ lúc nào.
/// - Mỗi Lõi hiển thị viền phẩm chất (Bạc, Vàng, Kim Cương), cấp độ (Lv.2, Lv.3) và vòng xoay hồi chiêu (Radial Cooldown).
/// - Chạm hoặc rê chuột vào từng Icon Lõi sẽ hiện Tooltip giải thích chi tiết, hoặc bấm nút "CHI TIẾT" để mở bảng danh sách đầy đủ.
/// </summary>
public class AugmentHUDBar : MonoBehaviour
{
    private static AugmentHUDBar instance;
    public static AugmentHUDBar Instance => instance;

    private const string PREF_COLLAPSED = "AugmentPanel_Collapsed";

    // Cache sprites
    private static Sprite _obsidianCore;
    private static Sprite _runicRing;
    private static Sprite _panelBg;
    private static Sprite _augmentToggleIconSprite;

    // Collapsible Popup UI Elements
    private RectTransform _rootRt;
    private GameObject _panelContentObj;
    private RectTransform _panelContentRt;
    private Transform _badgesGridContainer;
    private GameObject _emptyStateTextObj;
    private RectTransform _toggleBtnRt;
    private Image _toggleBtnImg;
    private GameObject _countBadgeObj;
    private TextMeshProUGUI _countBadgeText;
    private bool _collapsed = true;

    // Badges active inside Popup Grid
    private class BadgeItem
    {
        public AugmentDatabase.AugmentData data;
        public int starRank = 1;
        public GameObject rootObj;
        public RectTransform rootRt;
        public Image iconImg;
        public Image frameImg;
        public Image cdOverlayImg;
        public TextMeshProUGUI cdText;
        public TextMeshProUGUI stackText;
        public GameObject starRankObj;
        public TextMeshProUGUI starRankText;
        public CanvasGroup cg;
        public float initialDuration;
    }

    private readonly Dictionary<AugmentDatabase.AugmentType, BadgeItem> _activeBadges
        = new Dictionary<AugmentDatabase.AugmentType, BadgeItem>();

    // Tooltip UI
    private GameObject _tooltipObj;
    private RectTransform _tooltipRt;
    private TextMeshProUGUI _tooltipTierText;
    private TextMeshProUGUI _tooltipTitleText;
    private TextMeshProUGUI _tooltipDescText;
    private CanvasGroup _tooltipCg;
    private Coroutine _tooltipFadeCor;

    // Full Overview Modal
    private GameObject _fullOverviewPopupObj;
    private Transform _popupContentContainer;
    private float _prevOverviewTimeScale = 1f;
    private int _lastSelectedCount = -1;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        EnsureCollapsiblePanelBuilt();
        RefreshBadges(true);
    }

    void OnEnable()
    {
        EnsureCollapsiblePanelBuilt();
        RefreshBadges(true);
    }

    // ─── Procedural Augment Core Toggle Icon (Đồng bộ phong cách với Stat Icon) ─
    private static Sprite GetAugmentToggleIconSprite()
    {
        if (_augmentToggleIconSprite != null) return _augmentToggleIconSprite;

        const int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color dark      = new Color(0.07f, 0.09f, 0.13f, 0.95f);
        Color gold      = new Color(0.95f, 0.78f, 0.38f, 1.00f);
        Color goldDim   = new Color(0.62f, 0.50f, 0.26f, 1.00f);
        Color coreCyan  = new Color(0.32f, 0.90f, 1.00f, 1.00f);
        Color coreWhite = new Color(0.96f, 0.98f, 1.00f, 1.00f);

        Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
        float rOut = s * 0.5f - 1f;
        float rRing = rOut - 4f;

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Vector2.Distance(p, c);
                Color col = Color.clear;

                if (d <= rOut)
                {
                    // Nền tròn đá đen & viền đồng vàng giống hệt icon thống kê chỉ số
                    col = d > rRing ? gold : (d > rRing - 1.5f ? goldDim : dark);

                    float dx = Mathf.Abs(p.x - c.x);
                    float dy = Mathf.Abs(p.y - c.y);
                    float diamondDist = dx + dy;
                    float octDist = Mathf.Max(dx, dy) + 0.42f * Mathf.Min(dx, dy);

                    // 1. Vành cổ ngữ bát giác / kim cương bên ngoài (bán kính 14..17)
                    if (octDist >= 13.2f && octDist <= 16.2f)
                    {
                        col = octDist < 14.8f ? gold : goldDim;
                    }

                    // 2. Bốn hạt vệ tinh năng lượng ở 4 đỉnh Đông - Tây - Nam - Bắc
                    if ((dx <= 2.2f && Mathf.Abs(dy - 17.5f) <= 2.2f) ||
                        (dy <= 2.2f && Mathf.Abs(dx - 17.5f) <= 2.2f))
                    {
                        col = gold;
                    }

                    // 3. Tinh thể Lõi Nâng Cấp hình thoi đa diện ở chính giữa
                    if (diamondDist <= 10.5f)
                    {
                        if (diamondDist >= 8.2f)
                        {
                            col = gold; // Viền vàng bọc quanh lõi
                        }
                        else if (diamondDist <= 3.5f)
                        {
                            col = coreWhite; // Tâm sáng trắng kim cương
                        }
                        else
                        {
                            // Mặt cắt tinh thể ánh lam ngọc / hoàng kim
                            bool topFacet = (p.y > c.y && p.x < c.x);
                            col = topFacet ? Color.Lerp(coreCyan, coreWhite, 0.55f) : coreCyan;
                        }
                    }

                    col.a *= Mathf.Clamp01(rOut - d + 0.5f);
                }

                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();
        _augmentToggleIconSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        return _augmentToggleIconSprite;
    }

    public static AugmentHUDBar EnsureExists()
    {
        if (instance != null) return instance;

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("AugmentHUDBar");
        if (existing != null)
        {
            instance = existing.GetComponent<AugmentHUDBar>();
            if (instance != null)
            {
                instance.EnsureCollapsiblePanelBuilt();
                return instance;
            }
        }

        GameObject barObj = new GameObject("AugmentHUDBar", typeof(RectTransform));
        barObj.transform.SetParent(canvas.transform, false);

        instance = barObj.AddComponent<AugmentHUDBar>();
        instance.EnsureCollapsiblePanelBuilt();
        instance.SetupTooltip(canvas.transform);

        return instance;
    }

    private void EnsureCollapsiblePanelBuilt()
    {
        if (_panelContentObj != null && _toggleBtnRt != null) return;

        // Xóa layout cũ trên root nếu có từ phiên trước
        var oldHlg = GetComponent<HorizontalLayoutGroup>();
        if (oldHlg != null) Destroy(oldHlg);

        _rootRt = GetComponent<RectTransform>();
        if (_rootRt == null) _rootRt = gameObject.AddComponent<RectTransform>();
        _rootRt.anchorMin = new Vector2(0f, 1f);
        _rootRt.anchorMax = new Vector2(0f, 1f);
        _rootRt.pivot = new Vector2(0f, 1f);
        // Nằm ngay phía dưới cụm Avatar/Shards và phía trên Bảng Thống Kê Chỉ Số (x = 24, y = -178)
        _rootRt.anchoredPosition = new Vector2(24f, -178f);
        _rootRt.sizeDelta = new Vector2(456f, 122f);

        TMP_FontAsset font = GameFontManager.BodyFont;
        TMP_FontAsset headerFont = GameFontManager.HeaderFont;
        bool isVi = LocalizationManager.IsVietnamese;

        // ── 1. Panel Content (Hộp kính mờ Dark Fantasy đồng bộ với PlayerStatPanelUI) ──
        _panelContentObj = new GameObject("Content", typeof(RectTransform), typeof(Image), typeof(Outline));
        _panelContentObj.transform.SetParent(transform, false);
        _panelContentRt = _panelContentObj.GetComponent<RectTransform>();
        _panelContentRt.anchorMin = Vector2.zero;
        _panelContentRt.anchorMax = Vector2.one;
        _panelContentRt.offsetMin = _panelContentRt.offsetMax = Vector2.zero;

        var bg = _panelContentObj.GetComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.09f, 0.84f);
        bg.raycastTarget = true;

        var outline = _panelContentObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.62f, 0.55f, 0.40f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Tiêu đề hộp Lõi Nâng Cấp
        var headerObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerObj.transform.SetParent(_panelContentObj.transform, false);
        var hRt = headerObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(0f, 1f);
        hRt.pivot = new Vector2(0f, 1f);
        hRt.anchoredPosition = new Vector2(16f, -9f);
        hRt.sizeDelta = new Vector2(260f, 28f);

        var hTmp = headerObj.GetComponent<TextMeshProUGUI>();
        if (headerFont != null) hTmp.font = headerFont;
        hTmp.text = isVi ? "LÕI NÂNG CẤP ĐÃ CHỌN" : "ACTIVE AUGMENTS";
        hTmp.fontSize = 16.5f;
        hTmp.fontStyle = FontStyles.Bold;
        hTmp.color = new Color(0.95f, 0.82f, 0.42f, 1f);
        hTmp.alignment = TextAlignmentOptions.MidlineLeft;
        hTmp.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(hTmp, 0.30f, Color.black);

        // Nút "CHI TIẾT" trên thanh tiêu đề để mở bảng mô tả đầy đủ nếu muốn đọc chữ
        var detailBtnObj = new GameObject("DetailBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        detailBtnObj.transform.SetParent(_panelContentObj.transform, false);
        var dbRt = detailBtnObj.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(1f, 1f);
        dbRt.anchorMax = new Vector2(1f, 1f);
        dbRt.pivot = new Vector2(1f, 1f);
        dbRt.anchoredPosition = new Vector2(-48f, -8f);
        dbRt.sizeDelta = new Vector2(96f, 28f);

        var dbImg = detailBtnObj.GetComponent<Image>();
        dbImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

        var dbOut = detailBtnObj.GetComponent<Outline>();
        dbOut.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.80f);
        dbOut.effectDistance = new Vector2(1f, -1f);

        var dbBtn = detailBtnObj.GetComponent<Button>();
        dbBtn.targetGraphic = dbImg;
        dbBtn.onClick.AddListener(OpenFullOverviewPopup);

        var dbTxtObj = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        dbTxtObj.transform.SetParent(detailBtnObj.transform, false);
        var dbtRt = dbTxtObj.GetComponent<RectTransform>();
        dbtRt.anchorMin = Vector2.zero;
        dbtRt.anchorMax = Vector2.one;
        dbtRt.offsetMin = dbtRt.offsetMax = Vector2.zero;

        var dbTmp = dbTxtObj.GetComponent<TextMeshProUGUI>();
        if (font != null) dbTmp.font = font;
        dbTmp.text = isVi ? "CHI TIẾT" : "DETAILS";
        dbTmp.fontSize = 13.5f;
        dbTmp.fontStyle = FontStyles.Bold;
        dbTmp.alignment = TextAlignmentOptions.Center;
        dbTmp.color = new Color(1f, 0.90f, 0.50f, 1f);
        dbTmp.raycastTarget = false;

        // Grid chứa các Icon Lõi Nâng Cấp (Phóng to 54x54 rõ nét trên điện thoại)
        var gridObj = new GameObject("BadgesGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObj.transform.SetParent(_panelContentObj.transform, false);
        var gRt = gridObj.GetComponent<RectTransform>();
        gRt.anchorMin = new Vector2(0f, 0f);
        gRt.anchorMax = new Vector2(1f, 1f);
        gRt.offsetMin = new Vector2(16f, 12f);
        gRt.offsetMax = new Vector2(-16f, -42f);

        var glg = gridObj.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(54f, 54f);
        glg.spacing = new Vector2(10f, 10f);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 6;
        _badgesGridContainer = gridObj.transform;

        // Dòng chữ khi chưa có Lõi nào
        _emptyStateTextObj = new GameObject("EmptyText", typeof(RectTransform), typeof(TextMeshProUGUI));
        _emptyStateTextObj.transform.SetParent(_panelContentObj.transform, false);
        var empRt = _emptyStateTextObj.GetComponent<RectTransform>();
        empRt.anchorMin = new Vector2(0f, 0f);
        empRt.anchorMax = new Vector2(1f, 1f);
        empRt.offsetMin = new Vector2(16f, 10f);
        empRt.offsetMax = new Vector2(-16f, -38f);

        var empTmp = _emptyStateTextObj.GetComponent<TextMeshProUGUI>();
        if (font != null) empTmp.font = font;
        empTmp.text = isVi ? "Chưa sở hữu Lõi Nâng Cấp nào" : "No Augments acquired yet";
        empTmp.fontSize = 16f;
        empTmp.alignment = TextAlignmentOptions.Center;
        empTmp.color = new Color(0.65f, 0.70f, 0.78f, 0.90f);
        empTmp.raycastTarget = false;

        // ── 2. Nút Toggle Icon Tròn (64x64 - Phóng to đồng bộ với nút Thống Kê Chỉ Số) ────
        var btnObj = new GameObject("ToggleBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(transform, false);
        _toggleBtnRt = btnObj.GetComponent<RectTransform>();
        _toggleBtnRt.sizeDelta = new Vector2(64f, 64f);

        _toggleBtnImg = btnObj.GetComponent<Image>();
        _toggleBtnImg.sprite = GetAugmentToggleIconSprite();
        _toggleBtnImg.color = Color.white;

        var toggleBtn = btnObj.GetComponent<Button>();
        toggleBtn.targetGraphic = _toggleBtnImg;
        toggleBtn.onClick.AddListener(Toggle);

        // Huy hiệu đếm số lượng Lõi ở góc dưới phải của nút Icon
        _countBadgeObj = new GameObject("CountBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
        _countBadgeObj.transform.SetParent(btnObj.transform, false);
        var cbRt = _countBadgeObj.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(1f, 0f);
        cbRt.anchorMax = new Vector2(1f, 0f);
        cbRt.pivot = new Vector2(1f, 0f);
        cbRt.anchoredPosition = new Vector2(2f, -2f);
        cbRt.sizeDelta = new Vector2(28f, 22f);

        var cbImg = _countBadgeObj.GetComponent<Image>();
        cbImg.sprite = GetPanelBg();
        cbImg.type = Image.Type.Sliced;
        cbImg.color = new Color(0.06f, 0.09f, 0.15f, 0.96f);
        cbImg.raycastTarget = false;

        var cbOut = _countBadgeObj.GetComponent<Outline>();
        cbOut.effectColor = new Color(0.95f, 0.80f, 0.35f, 0.95f);
        cbOut.effectDistance = new Vector2(1f, -1f);

        var cbTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        cbTxtObj.transform.SetParent(_countBadgeObj.transform, false);
        var cbtRt = cbTxtObj.GetComponent<RectTransform>();
        cbtRt.anchorMin = Vector2.zero;
        cbtRt.anchorMax = Vector2.one;
        cbtRt.offsetMin = cbtRt.offsetMax = Vector2.zero;

        _countBadgeText = cbTxtObj.GetComponent<TextMeshProUGUI>();
        if (font != null) _countBadgeText.font = font;
        _countBadgeText.fontSize = 14.5f;
        _countBadgeText.fontStyle = FontStyles.Bold;
        _countBadgeText.alignment = TextAlignmentOptions.Center;
        _countBadgeText.color = new Color(0.35f, 0.92f, 1.00f, 1f);
        _countBadgeText.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(_countBadgeText, 0.30f, Color.black);
        _countBadgeObj.SetActive(false);

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) SetupTooltip(canvas.transform);

        _collapsed = PlayerPrefs.GetInt(PREF_COLLAPSED, 0) == 1;
        ApplyCollapsedState();
    }

    public void Toggle()
    {
        _collapsed = !_collapsed;
        PlayerPrefs.SetInt(PREF_COLLAPSED, _collapsed ? 1 : 0);
        PlayerPrefs.Save();
        HideTooltip();
        ApplyCollapsedState();
        if (!_collapsed) RefreshBadges(true);
    }

    private void ApplyCollapsedState()
    {
        if (_panelContentObj != null) _panelContentObj.SetActive(!_collapsed);

        if (_toggleBtnRt != null)
        {
            if (_collapsed)
            {
                // Thu gọn: Nút icon tròn nằm gọn ở góc trên-trái (ngay trên nút Thống Kê Chỉ Số)
                _toggleBtnRt.anchorMin = _toggleBtnRt.anchorMax = new Vector2(0f, 1f);
                _toggleBtnRt.pivot = new Vector2(0f, 1f);
                _toggleBtnRt.anchoredPosition = Vector2.zero;
            }
            else
            {
                // Mở rộng: Nút icon tròn nằm ở góc trên-phải của hộp Popup (giống hệt bảng Thống Kê Chỉ Số)
                _toggleBtnRt.anchorMin = _toggleBtnRt.anchorMax = new Vector2(1f, 1f);
                _toggleBtnRt.pivot = new Vector2(0.5f, 0.5f);
                _toggleBtnRt.anchoredPosition = new Vector2(-4f, -4f);
            }
        }

        if (_toggleBtnImg != null)
        {
            _toggleBtnImg.color = _collapsed ? Color.white : new Color(1f, 1f, 1f, 0.88f);
        }

        SyncStatPanelPosition();
    }

    public void SyncStatPanelPosition()
    {
        if (PlayerStatPanelUI.Instance == null) return;

        if (_collapsed)
        {
            // Khi hộp Lõi thu gọn (cao 64px tại y = -178), bảng Chỉ Số nằm ngay dưới tại y = -250
            PlayerStatPanelUI.Instance.SetTopYOffset(-250f);
        }
        else
        {
            float h = _rootRt != null ? _rootRt.sizeDelta.y : 122f;
            PlayerStatPanelUI.Instance.SetTopYOffset(-178f - h - 12f);
        }
    }

    public void RefreshBadges(bool force = false)
    {
        EnsureCollapsiblePanelBuilt();
        if (AugmentManager.Instance == null) return;
        var selected = AugmentManager.Instance.GetSelectedAugments();
        if (selected == null) return;

        if (!force && selected.Count == _lastSelectedCount)
        {
            return;
        }
        _lastSelectedCount = selected.Count;

        // Đếm số lượng từng loại Lõi theo thứ tự xuất hiện
        List<AugmentDatabase.AugmentType> orderedTypes = new List<AugmentDatabase.AugmentType>();
        Dictionary<AugmentDatabase.AugmentType, int> counts = new Dictionary<AugmentDatabase.AugmentType, int>();
        Dictionary<AugmentDatabase.AugmentType, AugmentDatabase.AugmentData> firstFound = new Dictionary<AugmentDatabase.AugmentType, AugmentDatabase.AugmentData>();

        for (int i = 0; i < selected.Count; i++)
        {
            var aug = selected[i];
            if (aug == null) continue;

            if (!counts.ContainsKey(aug.type))
            {
                counts[aug.type] = 1;
                firstFound[aug.type] = aug;
                orderedTypes.Add(aug.type);
            }
            else
            {
                counts[aug.type]++;
            }
        }

        // Xóa các badge không còn sở hữu
        List<AugmentDatabase.AugmentType> toClean = null;
        foreach (var kvp in _activeBadges)
        {
            if (!counts.ContainsKey(kvp.Key))
            {
                if (toClean == null) toClean = new List<AugmentDatabase.AugmentType>();
                toClean.Add(kvp.Key);
            }
        }
        if (toClean != null)
        {
            for (int i = 0; i < toClean.Count; i++)
            {
                var t = toClean[i];
                if (_activeBadges.TryGetValue(t, out BadgeItem b) && b != null && b.rootObj != null)
                {
                    Destroy(b.rootObj);
                }
                _activeBadges.Remove(t);
            }
        }

        // Cập nhật hoặc tạo mới toàn bộ badge trong hộp Popup
        for (int i = 0; i < orderedTypes.Count; i++)
        {
            var type = orderedTypes[i];
            int rank = counts[type];
            var data = firstFound[type];

            if (!_activeBadges.TryGetValue(type, out BadgeItem badge))
            {
                CreateAugmentBadge(data, rank);
                _activeBadges.TryGetValue(type, out badge);
            }
            else
            {
                badge.starRank = rank;
                if (badge.starRankText != null)
                {
                    badge.starRankText.text = $"Lv.{rank}";
                }
                if (badge.starRankObj != null)
                {
                    badge.starRankObj.SetActive(rank > 1);
                }
            }

            if (badge != null && badge.rootObj != null)
            {
                badge.rootObj.SetActive(true);
                badge.rootObj.transform.SetSiblingIndex(i);
            }
        }

        // Cập nhật huy hiệu số lượng trên nút Toggle Icon
        int distinctCount = orderedTypes.Count;
        if (_countBadgeObj != null)
        {
            _countBadgeObj.SetActive(distinctCount > 0);
            if (_countBadgeText != null) _countBadgeText.text = distinctCount.ToString();
        }

        if (_emptyStateTextObj != null)
        {
            _emptyStateTextObj.SetActive(distinctCount == 0);
        }

        // Tự động co giãn chiều cao hộp Popup theo số hàng Lõi (6 icon / hàng)
        int rows = Mathf.Max(1, Mathf.CeilToInt(distinctCount / 6f));
        float panelHeight = 56f + rows * 54f + (rows - 1) * 10f;
        if (_rootRt != null)
        {
            _rootRt.sizeDelta = new Vector2(456f, panelHeight);
        }
        SyncStatPanelPosition();

        // Nếu popup tổng quan đang mở thì cập nhật lại nội dung danh sách
        if (_fullOverviewPopupObj != null && _fullOverviewPopupObj.activeSelf)
        {
            PopulateOverviewContent(orderedTypes, counts, firstFound);
        }
    }

    public void OnAugmentAcquired(AugmentDatabase.AugmentData aug)
    {
        if (aug == null) return;
        // Tự động mở rộng hiển thị các Icon Lõi đã chọn trên HUD
        _collapsed = false;
        PlayerPrefs.SetInt(PREF_COLLAPSED, 0);
        PlayerPrefs.Save();
        ApplyCollapsedState();
        RefreshBadges(true);
    }

    private void SetupTooltip(Transform canvasRoot)
    {
        if (_tooltipObj != null) return;

        _tooltipObj = new GameObject("AugmentTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(CanvasGroup));
        _tooltipObj.transform.SetParent(canvasRoot, false);

        _tooltipRt = _tooltipObj.GetComponent<RectTransform>();
        _tooltipRt.anchorMin = new Vector2(0f, 1f);
        _tooltipRt.anchorMax = new Vector2(0f, 1f);
        _tooltipRt.pivot = new Vector2(0f, 1f);
        _tooltipRt.sizeDelta = new Vector2(420f, 150f);
        _tooltipRt.anchoredPosition = new Vector2(24f, -245f);

        Image bg = _tooltipObj.GetComponent<Image>();
        bg.sprite = GetPanelBg();
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.05f, 0.065f, 0.095f, 0.98f);

        Outline outline = _tooltipObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.82f, 0.40f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        _tooltipCg = _tooltipObj.GetComponent<CanvasGroup>();
        _tooltipCg.alpha = 0f;
        _tooltipCg.blocksRaycasts = false;

        // Tier text
        var tierObj = new GameObject("Tier", typeof(RectTransform), typeof(TextMeshProUGUI));
        tierObj.transform.SetParent(_tooltipObj.transform, false);
        var tRt = tierObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.sizeDelta = new Vector2(-16f, 22f);
        tRt.anchoredPosition = new Vector2(0f, -8f);

        _tooltipTierText = tierObj.GetComponent<TextMeshProUGUI>();
        _tooltipTierText.fontSize = 20f;
        _tooltipTierText.fontStyle = FontStyles.Bold;
        GameFontManager.ApplyFontToText(_tooltipTierText);
        SoulslikeHUDStyler.SetSafeOutline(_tooltipTierText, 0.0f, Color.black);

        // Title text
        var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(_tooltipObj.transform, false);
        var tiRt = titleObj.GetComponent<RectTransform>();
        tiRt.anchorMin = new Vector2(0f, 1f);
        tiRt.anchorMax = new Vector2(1f, 1f);
        tiRt.pivot = new Vector2(0.5f, 1f);
        tiRt.sizeDelta = new Vector2(-16f, 28f);
        tiRt.anchoredPosition = new Vector2(0f, -32f);

        _tooltipTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        _tooltipTitleText.fontSize = 26f;
        _tooltipTitleText.fontStyle = FontStyles.Bold;
        _tooltipTitleText.color = Color.white;
        GameFontManager.ApplyFontToText(_tooltipTitleText, true);
        SoulslikeHUDStyler.SetSafeOutline(_tooltipTitleText, 0.0f, Color.black);

        // Desc text
        var descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(_tooltipObj.transform, false);
        var dRt = descObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 1f);
        dRt.offsetMin = new Vector2(8f, 6f);
        dRt.offsetMax = new Vector2(-8f, -62f);

        _tooltipDescText = descObj.GetComponent<TextMeshProUGUI>();
        _tooltipDescText.fontSize = 22f;
        _tooltipDescText.color = new Color(0.85f, 0.90f, 0.98f);
        _tooltipDescText.enableWordWrapping = true;
        GameFontManager.ApplyFontToText(_tooltipDescText);
        SoulslikeHUDStyler.SetSafeOutline(_tooltipDescText, 0.0f, Color.black);

        _tooltipObj.SetActive(false);
    }

    void Update()
    {
        if (AugmentManager.Instance == null) return;

        var selected = AugmentManager.Instance.GetSelectedAugments();
        if (selected == null) return;

        RefreshBadges();

        List<AugmentDatabase.AugmentType> toRemove = null;

        foreach (var kvp in _activeBadges)
        {
            var type = kvp.Key;
            var badge = kvp.Value;

            if (badge == null || badge.rootObj == null) continue;
            var aug = badge.data;
            if (aug == null) continue;

            bool stillOwned = false;
            for (int s = 0; s < selected.Count; s++)
            {
                if (selected[s] != null && selected[s].type == type)
                {
                    stillOwned = true;
                    break;
                }
            }

            if (!stillOwned)
            {
                if (toRemove == null) toRemove = new List<AugmentDatabase.AugmentType>();
                toRemove.Add(type);
                continue;
            }

            if (aug.duration > 0f)
            {
                float remaining = AugmentManager.Instance.GetRemainingDurationByType(type);

                if (remaining <= 0f)
                {
                    if (toRemove == null) toRemove = new List<AugmentDatabase.AugmentType>();
                    toRemove.Add(type);
                    continue;
                }

                if (badge.cdOverlayImg != null)
                {
                    if (!badge.cdOverlayImg.gameObject.activeSelf) badge.cdOverlayImg.gameObject.SetActive(true);
                    float totalDur = badge.initialDuration > 0f ? badge.initialDuration : aug.duration;
                    badge.cdOverlayImg.fillAmount = Mathf.Clamp01(remaining / totalDur);
                }

                if (badge.cdText != null)
                {
                    if (!badge.cdText.gameObject.activeSelf) badge.cdText.gameObject.SetActive(true);
                    string secStr = remaining >= 10f
                        ? $"{Mathf.CeilToInt(remaining)}s"
                        : $"{remaining:0.#}s";
                    badge.cdText.text = secStr;
                }
            }
            else if (AugmentManager.Instance.GetAugmentCooldownInfo(type, out float cdRem, out float maxCd))
            {
                if (cdRem > 0f)
                {
                    if (badge.cdOverlayImg != null)
                    {
                        if (!badge.cdOverlayImg.gameObject.activeSelf) badge.cdOverlayImg.gameObject.SetActive(true);
                        badge.cdOverlayImg.fillAmount = Mathf.Clamp01(cdRem / maxCd);
                    }
                    if (badge.cdText != null)
                    {
                        if (!badge.cdText.gameObject.activeSelf) badge.cdText.gameObject.SetActive(true);
                        string secStr = cdRem >= 10f ? $"{Mathf.CeilToInt(cdRem)}s" : $"{cdRem:0.#}s";
                        badge.cdText.text = secStr;
                        badge.cdText.color = new Color(1f, 0.85f, 0.35f);
                    }
                    if (badge.iconImg != null)
                    {
                        badge.iconImg.color = new Color(0.45f, 0.45f, 0.50f, 0.55f);
                    }
                }
                else
                {
                    if (badge.cdOverlayImg != null && badge.cdOverlayImg.gameObject.activeSelf)
                    {
                        badge.cdOverlayImg.gameObject.SetActive(false);
                    }
                    if (badge.cdText != null && badge.cdText.gameObject.activeSelf)
                    {
                        badge.cdText.text = "";
                        badge.cdText.gameObject.SetActive(false);
                    }
                    if (badge.iconImg != null)
                    {
                        badge.iconImg.color = Color.white;
                    }
                }
            }

            if (aug.type == AugmentDatabase.AugmentType.BerserkerMode && badge.stackText != null)
            {
                int stacks = AugmentManager.Instance.GetBerserkerStacks();
                badge.stackText.text = stacks > 0 ? $"x{stacks}" : "";
            }
        }

        if (toRemove != null)
        {
            for (int i = 0; i < toRemove.Count; i++)
            {
                var type = toRemove[i];
                if (_activeBadges.TryGetValue(type, out BadgeItem badge))
                {
                    _activeBadges.Remove(type);
                    if (gameObject.activeInHierarchy && !_collapsed)
                    {
                        StartCoroutine(AnimateFadeOutAndDestroy(badge));
                    }
                    else if (badge.rootObj != null)
                    {
                        Destroy(badge.rootObj);
                    }
                }
            }
            _lastSelectedCount = -1;
            RefreshBadges(true);
        }
    }

    private void CreateAugmentBadge(AugmentDatabase.AugmentData aug, int rank = 1)
    {
        EnsureCollapsiblePanelBuilt();
        Color tierColor = GetTierColor(aug.rarity);
        Color typeColor = LevelUpChoiceUI.GetDamageTypeColor(aug.type);

        Transform parentTransform = _badgesGridContainer != null ? _badgesGridContainer : transform;

        GameObject badgeObj = new GameObject($"Badge_{aug.type}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(CanvasGroup));
        badgeObj.transform.SetParent(parentTransform, false);

        RectTransform rt = badgeObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(54f, 54f);

        Image bg = badgeObj.GetComponent<Image>();
        bg.sprite = GetObsidianCore();
        bg.color = new Color(0.08f, 0.10f, 0.14f, 0.96f);

        Outline outline = badgeObj.GetComponent<Outline>();
        outline.effectColor = tierColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        CanvasGroup cg = badgeObj.GetComponent<CanvasGroup>();

        GameObject ringObj = new GameObject("Ring", typeof(RectTransform), typeof(Image));
        ringObj.transform.SetParent(badgeObj.transform, false);
        var ringRt = ringObj.GetComponent<RectTransform>();
        ringRt.anchorMin = Vector2.zero;
        ringRt.anchorMax = Vector2.one;
        ringRt.offsetMin = ringRt.offsetMax = Vector2.zero;

        Image ringImg = ringObj.GetComponent<Image>();
        ringImg.sprite = GetRunicRing();
        ringImg.color = Color.Lerp(tierColor, Color.white, 0.25f);
        ringImg.raycastTarget = false;

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(badgeObj.transform, false);
        var iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.08f, 0.08f);
        iconRt.anchorMax = new Vector2(0.92f, 0.92f);
        iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = AugmentIconHelper.GetAugmentSprite(aug.type);
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        if (iconImg.sprite != null)
        {
            iconImg.enabled = true;
        }
        else
        {
            iconImg.enabled = false;
            var fbTxtObj = new GameObject("IconFallbackTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
            fbTxtObj.transform.SetParent(badgeObj.transform, false);
            var fbRt = fbTxtObj.GetComponent<RectTransform>();
            fbRt.anchorMin = Vector2.zero;
            fbRt.anchorMax = Vector2.one;
            fbRt.offsetMin = fbRt.offsetMax = Vector2.zero;
            var fbTmp = fbTxtObj.GetComponent<TextMeshProUGUI>();
            fbTmp.text = AugmentIconHelper.GetAugmentShortCode(aug.type);
            fbTmp.fontSize = 11f;
            fbTmp.fontStyle = FontStyles.Bold;
            fbTmp.alignment = TextAlignmentOptions.Center;
            fbTmp.color = typeColor;
            fbTmp.raycastTarget = false;
        }

        bool hasCooldownSupport = (aug.duration > 0f) ||
                                  (aug.type == AugmentDatabase.AugmentType.DivineAegis) ||
                                  (aug.type == AugmentDatabase.AugmentType.UndyingSovereign);

        Image cdOverlay = null;
        TextMeshProUGUI cdTmp = null;

        if (hasCooldownSupport)
        {
            GameObject cdObj = new GameObject("CDOverlay", typeof(RectTransform), typeof(Image));
            cdObj.transform.SetParent(badgeObj.transform, false);
            var cdRt = cdObj.GetComponent<RectTransform>();
            cdRt.anchorMin = Vector2.zero;
            cdRt.anchorMax = Vector2.one;
            cdRt.offsetMin = cdRt.offsetMax = Vector2.zero;

            cdOverlay = cdObj.GetComponent<Image>();
            cdOverlay.sprite = GetObsidianCore();
            cdOverlay.type = Image.Type.Filled;
            cdOverlay.fillMethod = Image.FillMethod.Radial360;
            cdOverlay.fillOrigin = (int)Image.Origin360.Top;
            cdOverlay.fillClockwise = false;
            cdOverlay.color = new Color(0f, 0f, 0f, 0.72f);
            cdOverlay.raycastTarget = false;
            if (aug.duration <= 0f) cdObj.SetActive(false);

            GameObject cdTxtObj = new GameObject("CDText", typeof(RectTransform), typeof(TextMeshProUGUI));
            cdTxtObj.transform.SetParent(badgeObj.transform, false);
            var cdTxtRt = cdTxtObj.GetComponent<RectTransform>();
            cdTxtRt.anchorMin = Vector2.zero;
            cdTxtRt.anchorMax = Vector2.one;
            cdTxtRt.offsetMin = cdTxtRt.offsetMax = Vector2.zero;

            cdTmp = cdTxtObj.GetComponent<TextMeshProUGUI>();
            cdTmp.fontSize = 14f;
            cdTmp.fontStyle = FontStyles.Bold;
            cdTmp.alignment = TextAlignmentOptions.Center;
            cdTmp.color = new Color(1f, 0.90f, 0.35f);
            SoulslikeHUDStyler.SetSafeOutline(cdTmp, 0.45f, Color.black);
            if (aug.duration <= 0f) cdTxtObj.SetActive(false);
        }

        TextMeshProUGUI stackTmp = null;
        if (aug.type == AugmentDatabase.AugmentType.BerserkerMode || aug.type == AugmentDatabase.AugmentType.SoulHarvest)
        {
            GameObject stackObj = new GameObject("StackText", typeof(RectTransform), typeof(TextMeshProUGUI));
            stackObj.transform.SetParent(badgeObj.transform, false);
            var sRt = stackObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.40f, 0.55f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = sRt.offsetMax = Vector2.zero;

            stackTmp = stackObj.GetComponent<TextMeshProUGUI>();
            stackTmp.fontSize = 13f;
            stackTmp.fontStyle = FontStyles.Bold;
            stackTmp.alignment = TextAlignmentOptions.TopRight;
            stackTmp.color = Color.white;
            SoulslikeHUDStyler.SetSafeOutline(stackTmp, 0.40f, Color.black);
        }

        GameObject starObj = new GameObject("StarRankBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
        starObj.transform.SetParent(badgeObj.transform, false);
        var starRt = starObj.GetComponent<RectTransform>();
        starRt.anchorMin = new Vector2(0f, 1f);
        starRt.anchorMax = new Vector2(0f, 1f);
        starRt.pivot = new Vector2(0f, 1f);
        starRt.anchoredPosition = new Vector2(-2f, 2f);
        starRt.sizeDelta = new Vector2(32f, 18f);

        var starImg = starObj.GetComponent<Image>();
        starImg.sprite = GetPanelBg();
        starImg.type = Image.Type.Sliced;
        starImg.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);
        starImg.raycastTarget = false;

        var starOut = starObj.GetComponent<Outline>();
        starOut.effectColor = new Color(0.95f, 0.82f, 0.35f, 0.90f);
        starOut.effectDistance = new Vector2(0.75f, -0.75f);

        GameObject starTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        starTxtObj.transform.SetParent(starObj.transform, false);
        var starTxtRt = starTxtObj.GetComponent<RectTransform>();
        starTxtRt.anchorMin = Vector2.zero;
        starTxtRt.anchorMax = Vector2.one;
        starTxtRt.offsetMin = starTxtRt.offsetMax = Vector2.zero;

        var starTmp = starTxtObj.GetComponent<TextMeshProUGUI>();
        starTmp.fontSize = 12f;
        starTmp.fontStyle = FontStyles.Bold;
        starTmp.alignment = TextAlignmentOptions.Center;
        starTmp.color = new Color(1f, 0.90f, 0.35f, 1f);
        starTmp.enableWordWrapping = false;
        starTmp.overflowMode = TextOverflowModes.Overflow;
        SoulslikeHUDStyler.SetSafeOutline(starTmp, 0.30f, Color.black);
        starTmp.text = $"Lv.{rank}";
        starObj.SetActive(rank > 1);

        var trigger = badgeObj.AddComponent<EventTrigger>();
        var captured = aug;

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowTooltip(captured, badgeObj.transform.position));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideTooltip());
        trigger.triggers.Add(exit);

        var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(_ => ShowTooltip(captured, badgeObj.transform.position, true));
        trigger.triggers.Add(click);

        var item = new BadgeItem
        {
            data = aug,
            starRank = rank,
            rootObj = badgeObj,
            rootRt = rt,
            iconImg = iconImg,
            frameImg = bg,
            cdOverlayImg = cdOverlay,
            cdText = cdTmp,
            stackText = stackTmp,
            starRankObj = starObj,
            starRankText = starTmp,
            cg = cg,
            initialDuration = aug.duration
        };

        _activeBadges[aug.type] = item;

        if (gameObject.activeInHierarchy && !_collapsed)
        {
            StartCoroutine(AnimatePopIn(item));
        }
        else
        {
            if (item.rootRt != null) item.rootRt.localScale = Vector3.one;
            if (item.cg != null) item.cg.alpha = 1f;
        }
    }

    private IEnumerator AnimatePopIn(BadgeItem item)
    {
        if (item == null || item.rootRt == null) yield break;

        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(t / 0.18f);
            if (item.rootRt != null)
            {
                item.rootRt.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.15f, factor);
            }
            if (item.cg != null) item.cg.alpha = factor;
            yield return null;
        }

        t = 0f;
        while (t < 0.10f)
        {
            t += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(t / 0.10f);
            if (item.rootRt != null)
            {
                item.rootRt.localScale = Vector3.one * Mathf.Lerp(1.15f, 1.0f, factor);
            }
            yield return null;
        }

        if (item.rootRt != null) item.rootRt.localScale = Vector3.one;
        if (item.cg != null) item.cg.alpha = 1f;
    }

    private IEnumerator AnimateFadeOutAndDestroy(BadgeItem item)
    {
        if (item == null || item.rootObj == null) yield break;

        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(t / 0.22f);
            if (item.rootRt != null)
            {
                item.rootRt.localScale = Vector3.one * Mathf.Lerp(1.0f, 0.2f, factor);
            }
            if (item.cg != null) item.cg.alpha = 1f - factor;
            yield return null;
        }

        if (item.rootObj != null) Destroy(item.rootObj);
    }

    // ─── Tooltip Display ───────────────────────────────────────────────────────
    public void ShowTooltip(AugmentDatabase.AugmentData aug, Vector3 badgeWorldPos, bool autoDismiss = false)
    {
        if (_tooltipObj == null || aug == null) return;

        bool isVi = LocalizationManager.IsVietnamese;
        Color tierColor = GetTierColor(aug.rarity);
        string tierName = GetTierName(aug.rarity, isVi);
        string typeName = LevelUpChoiceUI.GetDamageTypeName(aug.type, isVi);

        if (_tooltipTierText != null)
        {
            _tooltipTierText.text = $"{tierName} - {typeName}";
            _tooltipTierText.color = tierColor;
        }

        int rank = 1;
        if (_activeBadges != null && _activeBadges.TryGetValue(aug.type, out BadgeItem badge) && badge != null)
        {
            rank = badge.starRank;
        }

        string rankHeader = rank > 1 ? $" <color=#FBBF24><b>Lv.{rank}</b></color>" : "";
        string title = isVi ? aug.nameVN : aug.nameEN;
        if (string.IsNullOrEmpty(title))
        {
            var dbAug = AugmentDatabase.GetByType(aug.type);
            if (dbAug != null) title = isVi ? dbAug.nameVN : dbAug.nameEN;
        }
        if (string.IsNullOrEmpty(title)) title = aug.type.ToString();
        if (_tooltipTitleText != null) _tooltipTitleText.text = title + rankHeader;

        string desc = FormatAugmentDescription(aug, rank, isVi, typeName);

        if (aug.duration > 0f)
        {
            float rem = AugmentManager.Instance != null ? AugmentManager.Instance.GetRemainingDurationByType(aug.type) : aug.duration;
            string remStr = rem >= 10f ? $"{Mathf.CeilToInt(rem)}s" : $"{rem:0.#}s";
            desc += $"\n<color=#55EFC4><b>{(isVi ? "Thời gian còn:" : "Remaining:")} {remStr}</b></color>";
        }
        else if (AugmentManager.Instance != null && AugmentManager.Instance.GetAugmentCooldownInfo(aug.type, out float cdRem, out float maxCd))
        {
            if (cdRem > 0f)
            {
                string remStr = cdRem >= 10f ? $"{Mathf.CeilToInt(cdRem)}s" : $"{cdRem:0.#}s";
                desc += $"\n<color=#FFAA33><b>{(isVi ? "Đang Hồi Chiêu:" : "On Cooldown:")} {remStr} / {maxCd:0}s</b></color>";
            }
            else
            {
                desc += $"\n<color=#52F0FF><b>{(isVi ? "Trạng thái:" : "Status:")} {(isVi ? "SẴN SÀNG KÍCH HOẠT!" : "READY TO PROC!")}</b></color>";
            }
        }

        if (_tooltipDescText != null) _tooltipDescText.text = desc;

        if (_tooltipRt != null)
        {
            float panelH = (_rootRt != null && !_collapsed) ? _rootRt.sizeDelta.y : 52f;
            _tooltipRt.anchoredPosition = new Vector2(24f, -134f - panelH - 6f);
        }

        _tooltipObj.transform.SetAsLastSibling();
        _tooltipObj.SetActive(true);

        if (gameObject.activeInHierarchy)
        {
            if (_tooltipFadeCor != null) StopCoroutine(_tooltipFadeCor);
            _tooltipFadeCor = StartCoroutine(FadeTooltip(1f, 0.12f));

            if (autoDismiss)
            {
                StartCoroutine(AutoDismissTooltip(2.8f));
            }
        }
        else
        {
            if (_tooltipCg != null) _tooltipCg.alpha = 1f;
        }
    }

    private IEnumerator AutoDismissTooltip(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        HideTooltip();
    }

    public void HideTooltip()
    {
        if (_tooltipObj == null || !_tooltipObj.activeSelf) return;

        if (gameObject.activeInHierarchy)
        {
            if (_tooltipFadeCor != null) StopCoroutine(_tooltipFadeCor);
            _tooltipFadeCor = StartCoroutine(FadeTooltip(0f, 0.12f, () => {
                if (_tooltipObj != null) _tooltipObj.SetActive(false);
            }));
        }
        else
        {
            if (_tooltipCg != null) _tooltipCg.alpha = 0f;
            _tooltipObj.SetActive(false);
        }
    }

    private IEnumerator FadeTooltip(float targetAlpha, float dur, System.Action onComplete = null)
    {
        float start = _tooltipCg != null ? _tooltipCg.alpha : 0f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float factor = Mathf.Clamp01(t / dur);
            if (_tooltipCg != null) _tooltipCg.alpha = Mathf.Lerp(start, targetAlpha, factor);
            yield return null;
        }
        if (_tooltipCg != null) _tooltipCg.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    // ─── Full Overview Modal ───────────────────────────────────────────────────
    public void OpenFullOverviewPopup()
    {
        HideTooltip();
        if (_fullOverviewPopupObj == null)
        {
            Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                BuildOverviewPopup(canvas.transform);
            }
        }

        if (_fullOverviewPopupObj == null) return;

        if (AugmentManager.Instance != null)
        {
            var selected = AugmentManager.Instance.GetSelectedAugments();
            List<AugmentDatabase.AugmentType> orderedTypes = new List<AugmentDatabase.AugmentType>();
            Dictionary<AugmentDatabase.AugmentType, int> counts = new Dictionary<AugmentDatabase.AugmentType, int>();
            Dictionary<AugmentDatabase.AugmentType, AugmentDatabase.AugmentData> firstFound = new Dictionary<AugmentDatabase.AugmentType, AugmentDatabase.AugmentData>();

            for (int i = 0; i < selected.Count; i++)
            {
                var aug = selected[i];
                if (aug == null) continue;
                if (!counts.ContainsKey(aug.type))
                {
                    counts[aug.type] = 1;
                    firstFound[aug.type] = aug;
                    orderedTypes.Add(aug.type);
                }
                else
                {
                    counts[aug.type]++;
                }
            }

            PopulateOverviewContent(orderedTypes, counts, firstFound);
        }

        _fullOverviewPopupObj.SetActive(true);
        _prevOverviewTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
    }

    public void CloseFullOverviewPopup()
    {
        if (_fullOverviewPopupObj != null)
        {
            _fullOverviewPopupObj.SetActive(false);
        }
        Time.timeScale = _prevOverviewTimeScale;
    }

    private void BuildOverviewPopup(Transform canvasRoot)
    {
        if (_fullOverviewPopupObj != null) return;

        bool isVi = LocalizationManager.IsVietnamese;

        _fullOverviewPopupObj = new GameObject("AugmentOverviewModal", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        _fullOverviewPopupObj.transform.SetParent(canvasRoot, false);

        var c = _fullOverviewPopupObj.GetComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 26000;

        var rootRt = _fullOverviewPopupObj.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        var backdropObj = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
        backdropObj.transform.SetParent(_fullOverviewPopupObj.transform, false);
        var bRt = backdropObj.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.offsetMin = bRt.offsetMax = Vector2.zero;

        var bImg = backdropObj.GetComponent<Image>();
        bImg.color = new Color(0.015f, 0.02f, 0.035f, 0.85f);
        bImg.raycastTarget = true;

        var bBtn = backdropObj.GetComponent<Button>();
        bBtn.onClick.AddListener(CloseFullOverviewPopup);

        var panelObj = new GameObject("WindowPanel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(backdropObj.transform, false);
        var pRt = panelObj.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.5f, 0.5f);
        pRt.anchorMax = new Vector2(0.5f, 0.5f);
        pRt.pivot = new Vector2(0.5f, 0.5f);
        pRt.sizeDelta = new Vector2(740f, 520f);

        var pImg = panelObj.GetComponent<Image>();
        pImg.sprite = GetPanelBg();
        pImg.type = Image.Type.Sliced;
        pImg.color = new Color(0.05f, 0.065f, 0.095f, 0.98f);
        pImg.raycastTarget = true;

        var borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(panelObj.transform, false);
        var borderRt = borderObj.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = borderRt.offsetMax = Vector2.zero;

        var borderImg = borderObj.GetComponent<Image>();
        borderImg.sprite = SoulslikeHUDStyler.GetFramedPanelBorderSprite();
        if (borderImg.sprite != null)
        {
            borderImg.type = Image.Type.Sliced;
            borderImg.pixelsPerUnitMultiplier = 0.4f;
        }
        borderImg.color = new Color(0.95f, 0.82f, 0.40f, 0.90f);
        borderImg.raycastTarget = false;

        var titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        var tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.06f, 0.89f);
        tRt.anchorMax = new Vector2(0.85f, 0.98f);
        tRt.offsetMin = tRt.offsetMax = Vector2.zero;

        var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = isVi ? "TẤT CẢ LÕI NÂNG CẤP ĐÃ CHỌN" : "ALL ACQUIRED AUGMENTS";
        titleTmp.fontSize = 20f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        titleTmp.color = new Color(1f, 0.90f, 0.50f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(titleTmp, 0.40f, Color.black);

        var closeXObj = new GameObject("CloseXBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        closeXObj.transform.SetParent(panelObj.transform, false);
        var cxRt = closeXObj.GetComponent<RectTransform>();
        cxRt.anchorMin = new Vector2(0.91f, 0.89f);
        cxRt.anchorMax = new Vector2(0.96f, 0.97f);
        cxRt.offsetMin = cxRt.offsetMax = Vector2.zero;

        var cxImg = closeXObj.GetComponent<Image>();
        cxImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        cxImg.type = Image.Type.Sliced;
        cxImg.color = new Color(0.20f, 0.12f, 0.14f, 1f);

        var cxBtn = closeXObj.GetComponent<Button>();
        cxBtn.onClick.AddListener(CloseFullOverviewPopup);

        var cxTextObj = new GameObject("XText", typeof(RectTransform), typeof(TextMeshProUGUI));
        cxTextObj.transform.SetParent(closeXObj.transform, false);
        var cxTextRt = cxTextObj.GetComponent<RectTransform>();
        cxTextRt.anchorMin = Vector2.zero;
        cxTextRt.anchorMax = Vector2.one;
        cxTextRt.offsetMin = cxTextRt.offsetMax = Vector2.zero;
        var cxTmp = cxTextObj.GetComponent<TextMeshProUGUI>();
        cxTmp.text = "X";
        cxTmp.fontSize = 18f;
        cxTmp.fontStyle = FontStyles.Bold;
        cxTmp.alignment = TextAlignmentOptions.Center;
        cxTmp.color = new Color(0.95f, 0.35f, 0.35f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(cxTmp, 0.35f, Color.black);

        var divObj = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divObj.transform.SetParent(panelObj.transform, false);
        var divRt = divObj.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.04f, 0.87f);
        divRt.anchorMax = new Vector2(0.96f, 0.885f);
        divRt.offsetMin = divRt.offsetMax = Vector2.zero;

        var divImg = divObj.GetComponent<Image>();
        divImg.sprite = SoulslikeHUDStyler.GetGothicDividerSprite();
        divImg.color = new Color(0.95f, 0.82f, 0.40f, 0.85f);
        divImg.raycastTarget = false;

        var scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        scrollObj.transform.SetParent(panelObj.transform, false);
        var scRt = scrollObj.GetComponent<RectTransform>();
        scRt.anchorMin = new Vector2(0.04f, 0.12f);
        scRt.anchorMax = new Vector2(0.96f, 0.86f);
        scRt.offsetMin = scRt.offsetMax = Vector2.zero;

        var scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollObj.transform, false);
        var cRt = contentObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.sizeDelta = new Vector2(0f, 0f);

        var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(4, 4, 6, 6);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = cRt;
        _popupContentContainer = contentObj.transform;

        var bottomBtnObj = new GameObject("BottomCloseBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        bottomBtnObj.transform.SetParent(panelObj.transform, false);
        var bbRt = bottomBtnObj.GetComponent<RectTransform>();
        bbRt.anchorMin = new Vector2(0.40f, 0.025f);
        bbRt.anchorMax = new Vector2(0.60f, 0.095f);
        bbRt.offsetMin = bbRt.offsetMax = Vector2.zero;

        var bbImg = bottomBtnObj.GetComponent<Image>();
        bbImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        bbImg.type = Image.Type.Sliced;
        bbImg.color = new Color(0.18f, 0.20f, 0.28f, 1f);

        var bbOut = bottomBtnObj.GetComponent<Outline>();
        bbOut.effectColor = new Color(0.95f, 0.82f, 0.40f, 0.90f);
        bbOut.effectDistance = new Vector2(1.5f, -1.5f);

        var bbBtn = bottomBtnObj.GetComponent<Button>();
        bbBtn.onClick.AddListener(CloseFullOverviewPopup);

        var bbTextObj = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bbTextObj.transform.SetParent(bottomBtnObj.transform, false);
        var bbTextRt = bbTextObj.GetComponent<RectTransform>();
        bbTextRt.anchorMin = Vector2.zero;
        bbTextRt.anchorMax = Vector2.one;
        bbTextRt.offsetMin = bbTextRt.offsetMax = Vector2.zero;

        var bbTmp = bbTextObj.GetComponent<TextMeshProUGUI>();
        bbTmp.text = isVi ? "ĐÓNG" : "CLOSE";
        bbTmp.fontSize = 16f;
        bbTmp.fontStyle = FontStyles.Bold;
        bbTmp.alignment = TextAlignmentOptions.Center;
        bbTmp.color = Color.white;
        SoulslikeHUDStyler.SetSafeOutline(bbTmp, 0.35f, Color.black);
    }

    private void PopulateOverviewContent(
        List<AugmentDatabase.AugmentType> orderedTypes,
        Dictionary<AugmentDatabase.AugmentType, int> counts,
        Dictionary<AugmentDatabase.AugmentType, AugmentDatabase.AugmentData> firstFound)
    {
        if (_popupContentContainer == null) return;

        bool isVi = LocalizationManager.IsVietnamese;

        for (int i = _popupContentContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_popupContentContainer.GetChild(i).gameObject);
        }

        if (orderedTypes.Count == 0)
        {
            var emptyObj = new GameObject("EmptyText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            emptyObj.transform.SetParent(_popupContentContainer, false);
            var le = emptyObj.GetComponent<LayoutElement>();
            le.preferredHeight = 100f;
            var tmp = emptyObj.GetComponent<TextMeshProUGUI>();
            tmp.text = isVi ? "Chưa có Lõi Nâng Cấp nào được chọn." : "No augments acquired yet.";
            tmp.fontSize = 16f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.6f, 0.65f, 0.75f, 1f);
            return;
        }

        for (int i = 0; i < orderedTypes.Count; i++)
        {
            var type = orderedTypes[i];
            int rank = counts[type];
            var aug = firstFound[type];
            if (aug == null) continue;

            Color tierColor = GetTierColor(aug.rarity);
            string tierHex = ColorUtility.ToHtmlStringRGB(tierColor);
            string tierName = GetTierName(aug.rarity, isVi);
            string typeName = LevelUpChoiceUI.GetDamageTypeName(aug.type, isVi);

            string title = isVi ? aug.nameVN : aug.nameEN;
            if (string.IsNullOrEmpty(title))
            {
                var dbAug = AugmentDatabase.GetByType(aug.type);
                if (dbAug != null) title = isVi ? dbAug.nameVN : dbAug.nameEN;
            }
            if (string.IsNullOrEmpty(title)) title = aug.type.ToString();

            string desc = FormatAugmentDescription(aug, rank, isVi, typeName);

            var cardObj = new GameObject($"Card_{type}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(Outline));
            cardObj.transform.SetParent(_popupContentContainer, false);

            var le = cardObj.GetComponent<LayoutElement>();
            le.preferredHeight = 94f;
            le.minHeight = 94f;

            var cardImg = cardObj.GetComponent<Image>();
            cardImg.sprite = GetPanelBg();
            cardImg.type = Image.Type.Sliced;
            cardImg.color = new Color(0.08f, 0.10f, 0.15f, 0.95f);

            var outl = cardObj.GetComponent<Outline>();
            outl.effectColor = tierColor * 0.70f;
            outl.effectDistance = new Vector2(1f, -1f);

            var medalObj = new GameObject("Medallion", typeof(RectTransform), typeof(Image));
            medalObj.transform.SetParent(cardObj.transform, false);
            var medalRt = medalObj.GetComponent<RectTransform>();
            medalRt.anchorMin = new Vector2(0f, 0.5f);
            medalRt.anchorMax = new Vector2(0f, 0.5f);
            medalRt.pivot = new Vector2(0f, 0.5f);
            medalRt.anchoredPosition = new Vector2(12f, 0f);
            medalRt.sizeDelta = new Vector2(52f, 52f);

            var medalImg = medalObj.GetComponent<Image>();
            medalImg.sprite = GetObsidianCore();
            medalImg.color = new Color(0.10f, 0.12f, 0.16f, 1f);

            var ringObj = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ringObj.transform.SetParent(medalObj.transform, false);
            var ringRt = ringObj.GetComponent<RectTransform>();
            ringRt.anchorMin = Vector2.zero;
            ringRt.anchorMax = Vector2.one;
            ringRt.offsetMin = ringRt.offsetMax = Vector2.zero;

            var ringImg = ringObj.GetComponent<Image>();
            ringImg.sprite = GetRunicRing();
            ringImg.color = tierColor;

            var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(medalObj.transform, false);
            var iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.10f, 0.10f);
            iconRt.anchorMax = new Vector2(0.90f, 0.90f);
            iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;

            var iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = AugmentIconHelper.GetAugmentSprite(type);
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            if (iconImg.sprite != null)
            {
                iconImg.enabled = true;
            }
            else
            {
                iconImg.enabled = false;
                var fbTxtObj = new GameObject("IconFallbackTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
                fbTxtObj.transform.SetParent(medalObj.transform, false);
                var fbRt = fbTxtObj.GetComponent<RectTransform>();
                fbRt.anchorMin = Vector2.zero;
                fbRt.anchorMax = Vector2.one;
                fbRt.offsetMin = fbRt.offsetMax = Vector2.zero;
                var fbTmp = fbTxtObj.GetComponent<TextMeshProUGUI>();
                fbTmp.text = AugmentIconHelper.GetAugmentShortCode(type);
                fbTmp.fontSize = 12f;
                fbTmp.fontStyle = FontStyles.Bold;
                fbTmp.alignment = TextAlignmentOptions.Center;
                fbTmp.color = Color.white;
            }

            if (rank > 1)
            {
                var rkObj = new GameObject("RankTag", typeof(RectTransform), typeof(TextMeshProUGUI));
                rkObj.transform.SetParent(medalObj.transform, false);
                var rkRt = rkObj.GetComponent<RectTransform>();
                rkRt.anchorMin = new Vector2(0f, 0.65f);
                rkRt.anchorMax = new Vector2(0.7f, 1f);
                rkRt.offsetMin = rkRt.offsetMax = Vector2.zero;
                var rkTmp = rkObj.GetComponent<TextMeshProUGUI>();
                rkTmp.text = $"Lv.{rank}";
                rkTmp.fontSize = 11f;
                rkTmp.fontStyle = FontStyles.Bold;
                rkTmp.color = new Color(1f, 0.88f, 0.25f, 1f);
                SoulslikeHUDStyler.SetSafeOutline(rkTmp, 0.35f, Color.black);
            }

            var textObj = new GameObject("TextCol", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(cardObj.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.offsetMin = new Vector2(68f, 4f);
            textRt.offsetMax = new Vector2(-155f, -4f);

            var textTmp = textObj.GetComponent<TextMeshProUGUI>();
            string rankStr = rank > 1 ? $" <color=#FBBF24>Lv.{rank}</color>" : "";
            textTmp.text = $"<size=16.5><b><color=#{tierHex}>{tierName} - {typeName}</color>  <color=#FFFFFF>{title}</color>{rankStr}</b></size>\n<size=13.5><color=#CBD5E1>{desc}</color></size>";
            textTmp.enableWordWrapping = true;
            SoulslikeHUDStyler.SetSafeOutline(textTmp, 0.25f, Color.black);

            var statusObj = new GameObject("StatusCol", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusObj.transform.SetParent(cardObj.transform, false);
            var statusRt = statusObj.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(1f, 0f);
            statusRt.anchorMax = new Vector2(1f, 1f);
            statusRt.pivot = new Vector2(1f, 0.5f);
            statusRt.anchoredPosition = new Vector2(-10f, 0f);
            statusRt.sizeDelta = new Vector2(125f, 65f);

            var statusTmp = statusObj.GetComponent<TextMeshProUGUI>();
            statusTmp.alignment = TextAlignmentOptions.Right;

            if (aug.duration > 0f)
            {
                float rem = AugmentManager.Instance != null ? AugmentManager.Instance.GetRemainingDurationByType(type) : aug.duration;
                if (rem <= 0f) rem = aug.duration;
                string remStr = rem >= 10f ? $"{Mathf.CeilToInt(rem)}s" : $"{rem:0.#}s";
                statusTmp.text = $"<size=11><color=#94A3B8>{(isVi ? "Thời gian" : "Duration")}</color></size>\n<size=14><b><color=#55EFC4>{remStr}</color></b></size>";
            }
            else
            {
                statusTmp.text = $"<size=11><color=#94A3B8>{(isVi ? "Hiệu lực" : "Effect")}</color></size>\n<size=13><b><color=#86EFAC>{(isVi ? "Vĩnh viễn" : "Permanent")}</color></b></size>";
            }
            SoulslikeHUDStyler.SetSafeOutline(statusTmp, 0.25f, Color.black);
        }
    }

    private static Color GetTierColor(AugmentDatabase.AugmentData.Rarity rarity)
    {
        switch (rarity)
        {
            case AugmentDatabase.AugmentData.Rarity.Epic:
                return new Color(0.92f, 0.50f, 0.98f, 1f);
            case AugmentDatabase.AugmentData.Rarity.Rare:
                return new Color(1.00f, 0.82f, 0.28f, 1f);
            default:
                return new Color(0.80f, 0.86f, 0.95f, 0.95f);
        }
    }

    private static string GetTierName(AugmentDatabase.AugmentData.Rarity rarity, bool isVi)
    {
        switch (rarity)
        {
            case AugmentDatabase.AugmentData.Rarity.Epic:
                return isVi ? "BẬC KIM CƯƠNG" : "PRISMATIC TIER";
            case AugmentDatabase.AugmentData.Rarity.Rare:
                return isVi ? "BẬC VÀNG" : "GOLD TIER";
            default:
                return isVi ? "BẬC BẠC" : "SILVER TIER";
        }
    }

    // ─── Sprite Caches ─────────────────────────────────────────────────────────
    public static Sprite GetObsidianCore()
    {
        if (_obsidianCore == null)
            _obsidianCore = SoulslikeHUDStyler.GetActionButtonCoreSprite();
        return _obsidianCore;
    }

    public static Sprite GetRunicRing()
    {
        if (_runicRing == null)
            _runicRing = SoulslikeHUDStyler.GetActionButtonRingSprite();
        return _runicRing;
    }

    public static string FormatAugmentDescription(AugmentDatabase.AugmentData aug, int rank, bool isVi, string typeName)
    {
        string desc = isVi ? aug.descVN : aug.descEN;
        if (string.IsNullOrEmpty(desc))
        {
            var dbAug = AugmentDatabase.GetByType(aug.type);
            if (dbAug != null) desc = isVi ? dbAug.descVN : dbAug.descEN;
        }
        if (string.IsNullOrEmpty(desc)) desc = aug.descVN ?? aug.descEN;

        float totalVal = aug.value * rank;
        string valStr = totalVal % 1 == 0 ? ((int)totalVal).ToString() : totalVal.ToString("F1");

        if (!string.IsNullOrEmpty(desc))
        {
            desc = desc.Replace("{v}", $"<b><color=#FFEAA7>{valStr}</color></b>");
        }
        else
        {
            return isVi ? $"Tăng cường chỉ số {typeName} (+{valStr})." : $"Enhanced {typeName} (+{valStr}).";
        }

        if (rank > 1)
        {
            if (aug.type == AugmentDatabase.AugmentType.PercentCurrentHpExecute)
            {
                int mobPercent = 30 * rank;
                int bossPercent = 5 * rank;
                int bossCap = 600 * rank;
                if (isVi)
                {
                    desc = $"<color=#55EFC4><b>[LỢI ÍCH]</b></color> Gây thêm sát thương bằng <color=#FFEAA7><b>{mobPercent}% Máu Hiện Tại</b></color> của quái vật.\n<color=#FDCB6E><b>[HẠN CHẾ]</b></color> Sát thương lên Boss là <color=#FDCB6E><b>{bossPercent}% Máu</b></color> (trần {bossCap} dmg).";
                }
                else
                {
                    desc = $"<color=#55EFC4><b>[BENEFIT]</b></color> Deals bonus damage equal to <color=#FFEAA7><b>{mobPercent}% Target Current HP</b></color> on monsters.\n<color=#FDCB6E><b>[LIMIT]</b></color> Deals <color=#FDCB6E><b>{bossPercent}% HP</b></color> on Bosses (max {bossCap} dmg).";
                }
            }
        }

        return desc;
    }

    public static Sprite GetPanelBg()
    {
        if (_panelBg == null)
            _panelBg = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        return _panelBg;
    }
}
