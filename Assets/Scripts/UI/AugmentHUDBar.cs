using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// Thanh hiển thị các Lõi Nâng Cấp (Augments) đang kích hoạt trực tiếp trên màn hình trận đấu (Gameplay HUD).
/// - Mỗi Lõi Nâng Cấp sở hữu biểu tượng (Icon) riêng biệt theo từng cơ chế
/// - Hiển thị viền màu theo phẩm chất (Bạc, Vàng, Kim Cương / Prismatic)
/// - Các Lõi có thời hạn sẽ hiển thị vòng tròn hồi chiêu (Radial Cooldown) & đếm ngược số giây (12s, 4.5s...)
/// - Khi hết thời hạn sẽ tự động thu nhỏ và biến mất khỏi màn hình trận đấu
/// - Bấm hoặc rê chuột vào Icon sẽ hiện Tooltip giải thích chi tiết hiệu ứng của Lõi
/// </summary>
public class AugmentHUDBar : MonoBehaviour
{
    private static AugmentHUDBar instance;
    public static AugmentHUDBar Instance => instance;

    // Cache sprites
    private static Sprite _obsidianCore;
    private static Sprite _runicRing;
    private static Sprite _cooldownMask;
    private static Sprite _panelBg;

    // Badges active on HUD
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

    // More Button (+) & Overview Modal
    private GameObject _moreButtonObj;
    private TextMeshProUGUI _moreBtnText;
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
        RefreshBadges(true);
    }

    void OnEnable()
    {
        RefreshBadges(true);
    }

    public void RefreshBadges(bool force = false)
    {
        if (AugmentManager.Instance == null) return;
        var selected = AugmentManager.Instance.GetSelectedAugments();
        if (selected == null) return;

        if (!force && selected.Count == _lastSelectedCount)
        {
            return;
        }
        _lastSelectedCount = selected.Count;

        // Đếm số lượng từng loại Lõi theo thứ tự xuất hiện cũ nhất (chronological)
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

        // Xóa các badge không còn sở hữu khỏi _activeBadges
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

        // Cập nhật hoặc tạo mới badge
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
                // Người dùng yêu cầu: chỉ hiện lên 3 lỗi cũ nhất
                bool showOnBar = (i < 3);
                badge.rootObj.SetActive(showOnBar);
                if (showOnBar)
                {
                    badge.rootObj.transform.SetSiblingIndex(i);
                }
            }
        }

        // Quản lý nút Dấu Cộng (+)
        UpdateMoreButton(orderedTypes.Count);

        // Nếu popup đang mở thì cập nhật lại nội dung danh sách
        if (_fullOverviewPopupObj != null && _fullOverviewPopupObj.activeSelf)
        {
            PopulateOverviewContent(orderedTypes, counts, firstFound);
        }
    }

    public void OnAugmentAcquired(AugmentDatabase.AugmentData aug)
    {
        if (aug == null) return;
        RefreshBadges(true);
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
                RectTransform extRt = existing.GetComponent<RectTransform>();
                if (extRt != null) extRt.anchoredPosition = new Vector2(68f, -148f);
                return instance;
            }
        }

        GameObject barObj = new GameObject("AugmentHUDBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        barObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = barObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        // Vị trí nằm ngay dưới hộp ShardsCounter (-118f, height 28) tránh hoàn toàn không chồng đè
        rt.anchoredPosition = new Vector2(68f, -148f);
        rt.sizeDelta = new Vector2(560f, 44f);

        HorizontalLayoutGroup hlg = barObj.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 7f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        instance = barObj.AddComponent<AugmentHUDBar>();
        instance.SetupTooltip(canvas.transform);

        return instance;
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
        _tooltipRt.sizeDelta = new Vector2(300f, 110f);
        _tooltipRt.anchoredPosition = new Vector2(68f, -196f);

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
        tRt.sizeDelta = new Vector2(-16f, 18f);
        tRt.anchoredPosition = new Vector2(0f, -8f);

        _tooltipTierText = tierObj.GetComponent<TextMeshProUGUI>();
        _tooltipTierText.fontSize = 11f;
        _tooltipTierText.fontStyle = FontStyles.Bold;
        SoulslikeHUDStyler.SetSafeOutline(_tooltipTierText, 0.35f, Color.black);

        // Title text
        var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(_tooltipObj.transform, false);
        var tiRt = titleObj.GetComponent<RectTransform>();
        tiRt.anchorMin = new Vector2(0f, 1f);
        tiRt.anchorMax = new Vector2(1f, 1f);
        tiRt.pivot = new Vector2(0.5f, 1f);
        tiRt.sizeDelta = new Vector2(-16f, 22f);
        tiRt.anchoredPosition = new Vector2(0f, -26f);

        _tooltipTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        _tooltipTitleText.fontSize = 14f;
        _tooltipTitleText.fontStyle = FontStyles.Bold;
        _tooltipTitleText.color = Color.white;
        SoulslikeHUDStyler.SetSafeOutline(_tooltipTitleText, 0.35f, Color.black);

        // Desc text
        var descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(_tooltipObj.transform, false);
        var dRt = descObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 1f);
        dRt.offsetMin = new Vector2(8f, 6f);
        dRt.offsetMax = new Vector2(-8f, -48f);

        _tooltipDescText = descObj.GetComponent<TextMeshProUGUI>();
        _tooltipDescText.fontSize = 11.5f;
        _tooltipDescText.color = new Color(0.85f, 0.90f, 0.98f);
        _tooltipDescText.enableWordWrapping = true;
        SoulslikeHUDStyler.SetSafeOutline(_tooltipDescText, 0.25f, Color.black);

        _tooltipObj.SetActive(false);
    }

    void Update()
    {
        if (AugmentManager.Instance == null) return;

        var selected = AugmentManager.Instance.GetSelectedAugments();
        if (selected == null) return;

        // 1. Kiểm tra thêm Lõi Nâng Cấp mới được chọn vào HUD hoặc thăng cấp sao
        RefreshBadges();

        // 2. Cập nhật thời gian hồi chiêu / đếm ngược và kiểm tra hết hạn
        List<AugmentDatabase.AugmentType> toRemove = null;

        foreach (var kvp in _activeBadges)
        {
            var type = kvp.Key;
            var badge = kvp.Value;

            if (badge == null || badge.rootObj == null) continue;
            var aug = badge.data;
            if (aug == null) continue;

            // Kiểm tra xem còn sở hữu loại Lõi này không
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

            // Nếu là Lõi tạm thời có thời hạn (duration > 0)
            if (aug.duration > 0f)
            {
                float remaining = AugmentManager.Instance.GetRemainingDurationByType(type);

                if (remaining <= 0f)
                {
                    if (toRemove == null) toRemove = new List<AugmentDatabase.AugmentType>();
                    toRemove.Add(type);
                    continue;
                }

                // Cập nhật Radial Cooldown Overlay (vòng quét tròn)
                if (badge.cdOverlayImg != null)
                {
                    if (!badge.cdOverlayImg.gameObject.activeSelf) badge.cdOverlayImg.gameObject.SetActive(true);
                    float totalDur = badge.initialDuration > 0f ? badge.initialDuration : aug.duration;
                    badge.cdOverlayImg.fillAmount = Mathf.Clamp01(remaining / totalDur);
                }

                // Cập nhật văn bản đếm ngược số giây
                if (badge.cdText != null)
                {
                    if (!badge.cdText.gameObject.activeSelf) badge.cdText.gameObject.SetActive(true);
                    string secStr = remaining >= 10f
                        ? $"{Mathf.CeilToInt(remaining)}s"
                        : $"{remaining:0.#}s";
                    badge.cdText.text = secStr;
                }
            }
            // Nếu là Lõi vĩnh viễn có hồi chiêu nội tại (Thánh Khiên Hộ Mệnh 45s, Bất Diệt Cuồng Vương 90s)
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
                        badge.iconImg.color = new Color(0.40f, 0.40f, 0.45f, 0.50f); // Làm tối mờ khi đang hồi
                    }
                }
                else
                {
                    // Sẵn sàng kích hoạt (READY)
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
                        badge.iconImg.color = LevelUpChoiceUI.GetDamageTypeColor(aug.type);
                    }
                }
            }

            // Cập nhật số stack cộng dồn (Berserker / SoulHarvest)
            if (aug.type == AugmentDatabase.AugmentType.BerserkerMode && badge.stackText != null)
            {
                int stacks = AugmentManager.Instance.GetBerserkerStacks();
                badge.stackText.text = stacks > 0 ? $"x{stacks}" : "";
            }
        }

        // 3. Xoá các Lõi đã hết thời hạn với hiệu ứng mờ dần
        if (toRemove != null)
        {
            for (int i = 0; i < toRemove.Count; i++)
            {
                var type = toRemove[i];
                if (_activeBadges.TryGetValue(type, out BadgeItem badge))
                {
                    _activeBadges.Remove(type);
                    if (gameObject.activeInHierarchy)
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
        Color tierColor;
        switch (aug.rarity)
        {
            case AugmentDatabase.AugmentData.Rarity.Epic:
                tierColor = new Color(0.92f, 0.50f, 0.98f, 1f); // Tím Lăng Kính (Prismatic)
                break;
            case AugmentDatabase.AugmentData.Rarity.Rare:
                tierColor = new Color(1.00f, 0.82f, 0.28f, 1f); // Hoàng Kim (Gold)
                break;
            default:
                tierColor = new Color(0.80f, 0.86f, 0.95f, 0.95f); // Thép Bạc (Silver)
                break;
        }

        Color typeColor = LevelUpChoiceUI.GetDamageTypeColor(aug.type);

        // Khung huy hiệu gốc (Size 38x38)
        GameObject badgeObj = new GameObject($"Badge_{aug.type}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(CanvasGroup));
        badgeObj.transform.SetParent(transform, false);

        RectTransform rt = badgeObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(38f, 38f);

        Image bg = badgeObj.GetComponent<Image>();
        bg.sprite = GetObsidianCore();
        bg.color = new Color(0.08f, 0.10f, 0.14f, 0.96f);

        Outline outline = badgeObj.GetComponent<Outline>();
        outline.effectColor = tierColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        CanvasGroup cg = badgeObj.GetComponent<CanvasGroup>();

        // Vành kim loại Runic Ring
        GameObject ringObj = new GameObject("Ring", typeof(RectTransform), typeof(Image));
        ringObj.transform.SetParent(badgeObj.transform, false);
        var ringRt = ringObj.GetComponent<RectTransform>();
        ringRt.anchorMin = Vector2.zero;
        ringRt.anchorMax = Vector2.one;
        ringRt.offsetMin = ringRt.offsetMax = Vector2.zero;

        Image ringImg = ringObj.GetComponent<Image>();
        ringImg.sprite = GetRunicRing();
        ringImg.color = Color.Lerp(tierColor, typeColor, 0.35f);
        ringImg.raycastTarget = false;

        // Icon đồ họa độc nhất riêng biệt cho từng Lõi Nâng Cấp
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(badgeObj.transform, false);
        var iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.20f, 0.20f);
        iconRt.anchorMax = new Vector2(0.80f, 0.80f);
        iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;

        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = AugmentIconHelper.GetAugmentSprite(aug.type);
        iconImg.color = typeColor;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Lớp phủ Cooldown Radial & Chữ đếm giây (Hỗ trợ cả Lõi có thời hạn VÀ Lõi có hồi chiêu nội tại như Thánh Khiên Hộ Mệnh)
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
            cdOverlay.sprite = GetObsidianCore(); // Mask hình tròn
            cdOverlay.type = Image.Type.Filled;
            cdOverlay.fillMethod = Image.FillMethod.Radial360;
            cdOverlay.fillOrigin = (int)Image.Origin360.Top;
            cdOverlay.fillClockwise = false;
            cdOverlay.color = new Color(0f, 0f, 0f, 0.72f);
            cdOverlay.raycastTarget = false;
            if (aug.duration <= 0f) cdObj.SetActive(false);

            // Chữ đếm giây còn lại ở giữa Icon (ví dụ 45s, 12s, 4.2s)
            GameObject cdTxtObj = new GameObject("CDText", typeof(RectTransform), typeof(TextMeshProUGUI));
            cdTxtObj.transform.SetParent(badgeObj.transform, false);
            var cdTxtRt = cdTxtObj.GetComponent<RectTransform>();
            cdTxtRt.anchorMin = Vector2.zero;
            cdTxtRt.anchorMax = Vector2.one;
            cdTxtRt.offsetMin = cdTxtRt.offsetMax = Vector2.zero;

            cdTmp = cdTxtObj.GetComponent<TextMeshProUGUI>();
            cdTmp.fontSize = 11.5f;
            cdTmp.fontStyle = FontStyles.Bold;
            cdTmp.alignment = TextAlignmentOptions.Center;
            cdTmp.color = new Color(1f, 0.90f, 0.35f);
            SoulslikeHUDStyler.SetSafeOutline(cdTmp, 0.45f, Color.black);
            if (aug.duration <= 0f) cdTxtObj.SetActive(false);
        }

        // Chữ đếm số tầng stack (Berserker / SoulHarvest)
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
            stackTmp.fontSize = 10.5f;
            stackTmp.fontStyle = FontStyles.Bold;
            stackTmp.alignment = TextAlignmentOptions.TopRight;
            stackTmp.color = Color.white;
            SoulslikeHUDStyler.SetSafeOutline(stackTmp, 0.40f, Color.black);
        }

        // Huy hiệu cấp độ (Lv.2, Lv.3) ở góc trên bên trái huy hiệu, trên 1 dòng duy nhất không xuống dòng
        GameObject starObj = new GameObject("StarRankBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
        starObj.transform.SetParent(badgeObj.transform, false);
        var starRt = starObj.GetComponent<RectTransform>();
        starRt.anchorMin = new Vector2(0f, 1f);
        starRt.anchorMax = new Vector2(0f, 1f);
        starRt.pivot = new Vector2(0f, 1f);
        starRt.anchoredPosition = new Vector2(-2f, 2f);
        starRt.sizeDelta = new Vector2(25f, 13f);

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
        starTmp.fontSize = 9.5f;
        starTmp.fontStyle = FontStyles.Bold;
        starTmp.alignment = TextAlignmentOptions.Center;
        starTmp.color = new Color(1f, 0.90f, 0.35f, 1f); // Vàng Hoàng Kim
        starTmp.enableWordWrapping = false;
        starTmp.overflowMode = TextOverflowModes.Overflow;
        SoulslikeHUDStyler.SetSafeOutline(starTmp, 0.30f, Color.black);
        starTmp.text = $"Lv.{rank}";
        starObj.SetActive(rank > 1);

        // Tương tác chạm / rê chuột để xem Tooltip giải thích Lõi
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

        // Hiệu ứng Pop-in khi nhận Lõi mới (chỉ chạy coroutine nếu GameObject đang active)
        if (gameObject.activeInHierarchy)
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

        Color tierColor;
        string tierName;
        switch (aug.rarity)
        {
            case AugmentDatabase.AugmentData.Rarity.Epic:
                tierColor = new Color(0.92f, 0.50f, 0.98f, 1f);
                tierName = isVi ? "BẬC KIM CƯƠNG" : "PRISMATIC TIER";
                break;
            case AugmentDatabase.AugmentData.Rarity.Rare:
                tierColor = new Color(1.00f, 0.82f, 0.28f, 1f);
                tierName = isVi ? "BẬC VÀNG" : "GOLD TIER";
                break;
            default:
                tierColor = new Color(0.80f, 0.86f, 0.95f, 0.95f);
                tierName = isVi ? "BẬC BẠC" : "SILVER TIER";
                break;
        }

        string typeName = LevelUpChoiceUI.GetDamageTypeName(aug.type, isVi);
        if (_tooltipTierText != null)
        {
            _tooltipTierText.text = $"{tierName} • {typeName}";
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

        string desc = isVi ? aug.descVN : aug.descEN;
        if (string.IsNullOrEmpty(desc))
        {
            var dbAug = AugmentDatabase.GetByType(aug.type);
            if (dbAug != null) desc = isVi ? dbAug.descVN : dbAug.descEN;
        }
        if (string.IsNullOrEmpty(desc))
        {
            desc = aug.descVN ?? aug.descEN;
        }

        float totalVal = aug.value * rank;
        string valStr = totalVal % 1 == 0 ? ((int)totalVal).ToString() : totalVal.ToString("F1");

        if (!string.IsNullOrEmpty(desc))
        {
            desc = desc.Replace("{v}", $"<b><color=#FFEAA7>{valStr}</color></b>");
        }
        else
        {
            desc = isVi ? $"Tăng cường chỉ số {typeName} (+{valStr})." : $"Enhanced {typeName} (+{valStr}).";
        }

        if (rank > 1)
        {
            desc += $"\n<color=#FBBF24><b>{(isVi ? $"+ Cấp {rank}: Chỉ số được cộng dồn x{rank}!" : $"+ Rank {rank}: Stats multiplied by x{rank}!")}</b></color>";
        }

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

    // ─── More Button (+) & Full Overview Modal ────────────────────────────────
    private void UpdateMoreButton(int totalDistinctCount)
    {
        if (totalDistinctCount > 3)
        {
            EnsureMoreButtonCreated();
            if (_moreButtonObj != null)
            {
                _moreButtonObj.SetActive(true);
                _moreButtonObj.transform.SetAsLastSibling();
                if (_moreBtnText != null)
                {
                    int extra = totalDistinctCount - 3;
                    _moreBtnText.text = $"+{extra}";
                }
            }
        }
        else
        {
            if (_moreButtonObj != null)
            {
                _moreButtonObj.SetActive(false);
            }
        }
    }

    private void EnsureMoreButtonCreated()
    {
        if (_moreButtonObj != null) return;

        _moreButtonObj = new GameObject("MoreAugmentsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        _moreButtonObj.transform.SetParent(transform, false);

        var le = _moreButtonObj.GetComponent<LayoutElement>();
        le.preferredWidth = 38f;
        le.preferredHeight = 38f;
        le.minWidth = 38f;
        le.minHeight = 38f;

        var rt = _moreButtonObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(38f, 38f);

        var bg = _moreButtonObj.GetComponent<Image>();
        bg.sprite = GetObsidianCore();
        bg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

        // Vòng viền hào quang Hoàng Kim
        var ringObj = new GameObject("Ring", typeof(RectTransform), typeof(Image));
        ringObj.transform.SetParent(_moreButtonObj.transform, false);
        var ringRt = ringObj.GetComponent<RectTransform>();
        ringRt.anchorMin = Vector2.zero;
        ringRt.anchorMax = Vector2.one;
        ringRt.offsetMin = ringRt.offsetMax = Vector2.zero;

        var ringImg = ringObj.GetComponent<Image>();
        ringImg.sprite = GetRunicRing();
        ringImg.color = new Color(0.95f, 0.82f, 0.40f, 0.95f);
        ringImg.raycastTarget = false;

        // Dấu cộng Text
        var txtObj = new GameObject("PlusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(_moreButtonObj.transform, false);
        var txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;

        _moreBtnText = txtObj.GetComponent<TextMeshProUGUI>();
        _moreBtnText.fontSize = 17f;
        _moreBtnText.fontStyle = FontStyles.Bold;
        _moreBtnText.alignment = TextAlignmentOptions.Center;
        _moreBtnText.color = new Color(1f, 0.90f, 0.45f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(_moreBtnText, 0.40f, Color.black);
        _moreBtnText.text = "+";

        var btn = _moreButtonObj.GetComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(OpenFullOverviewPopup);

        // Tooltip hover
        var trigger = _moreButtonObj.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => {
            bool isVi = LocalizationManager.IsVietnamese;
            string t = isVi ? "Xem Tất Cả Lõi" : "View All Augments";
            string d = isVi ? "Bấm vào để mở danh sách chi tiết tất cả các Lõi Nâng Cấp đã chọn." : "Click to view full list and stats of all acquired augments.";
            ShowCustomTooltip(t, d, _moreButtonObj.transform.position);
        });
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideTooltip());
        trigger.triggers.Add(exit);
    }

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

        // Query augments
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

        // Dark background overlay (click to close)
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

        // Center window panel
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

        // Panel Border
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

        // Title text
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

        // Close X button
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

        // Gothic Divider
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

        // Scrollable container (ScrollRect + RectMask2D)
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

        // Bottom Close Button
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
                desc = isVi ? $"Tăng cường chỉ số {typeName} (+{valStr})." : $"Enhanced {typeName} (+{valStr}).";
            }

            if (rank > 1)
            {
                desc += $"  <color=#FBBF24><b>{(isVi ? $"+ Cấp {rank}: Chỉ số x{rank}!" : $"+ Rank {rank}: Stats x{rank}!")}</b></color>";
            }

            // Card item
            var cardObj = new GameObject($"Card_{type}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(Outline));
            cardObj.transform.SetParent(_popupContentContainer, false);

            var le = cardObj.GetComponent<LayoutElement>();
            le.preferredHeight = 72f;
            le.minHeight = 72f;

            var cardImg = cardObj.GetComponent<Image>();
            cardImg.sprite = GetPanelBg();
            cardImg.type = Image.Type.Sliced;
            cardImg.color = new Color(0.08f, 0.10f, 0.15f, 0.95f);

            var outl = cardObj.GetComponent<Outline>();
            outl.effectColor = tierColor * 0.70f;
            outl.effectDistance = new Vector2(1f, -1f);

            // Left medallion (44x44)
            var medalObj = new GameObject("Medallion", typeof(RectTransform), typeof(Image));
            medalObj.transform.SetParent(cardObj.transform, false);
            var medalRt = medalObj.GetComponent<RectTransform>();
            medalRt.anchorMin = new Vector2(0f, 0.5f);
            medalRt.anchorMax = new Vector2(0f, 0.5f);
            medalRt.pivot = new Vector2(0f, 0.5f);
            medalRt.anchoredPosition = new Vector2(12f, 0f);
            medalRt.sizeDelta = new Vector2(46f, 46f);

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
            iconRt.anchorMin = new Vector2(0.15f, 0.15f);
            iconRt.anchorMax = new Vector2(0.85f, 0.85f);
            iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;

            var iconImg = iconObj.GetComponent<Image>();
            iconImg.sprite = AugmentIconHelper.GetAugmentSprite(type);
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;

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

            // Text column
            var textObj = new GameObject("TextCol", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(cardObj.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.offsetMin = new Vector2(68f, 4f);
            textRt.offsetMax = new Vector2(-120f, -4f);

            var textTmp = textObj.GetComponent<TextMeshProUGUI>();
            string rankStr = rank > 1 ? $" <color=#FBBF24>Lv.{rank}</color>" : "";
            textTmp.text = $"<size=15><b><color=#{tierHex}>{tierName} • {typeName}</color>  <color=#FFFFFF>{title}</color>{rankStr}</b></size>\n<size=12><color=#CBD5E1>{desc}</color></size>";
            textTmp.enableWordWrapping = true;
            SoulslikeHUDStyler.SetSafeOutline(textTmp, 0.25f, Color.black);

            // Right status column
            var statusObj = new GameObject("StatusCol", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusObj.transform.SetParent(cardObj.transform, false);
            var statusRt = statusObj.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(1f, 0f);
            statusRt.anchorMax = new Vector2(1f, 1f);
            statusRt.pivot = new Vector2(1f, 0.5f);
            statusRt.anchoredPosition = new Vector2(-10f, 0f);
            statusRt.sizeDelta = new Vector2(110f, 60f);

            var statusTmp = statusObj.GetComponent<TextMeshProUGUI>();
            statusTmp.alignment = TextAlignmentOptions.Right;

            if (aug.duration > 0f)
            {
                float rem = AugmentManager.Instance != null ? AugmentManager.Instance.GetRemainingDurationByType(type) : aug.duration;
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

    private void ShowCustomTooltip(string title, string desc, Vector3 badgePos)
    {
        if (_tooltipObj == null) return;
        if (_tooltipTierText != null) _tooltipTierText.text = "";
        if (_tooltipTitleText != null) _tooltipTitleText.text = title;
        if (_tooltipDescText != null) _tooltipDescText.text = desc;

        if (_tooltipRt != null)
        {
            _tooltipRt.anchoredPosition = new Vector2(68f, -196f);
        }
        _tooltipObj.SetActive(true);
        if (_tooltipCg != null) _tooltipCg.alpha = 1f;
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

    public static Sprite GetPanelBg()
    {
        if (_panelBg == null)
            _panelBg = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        return _panelBg;
    }
}
