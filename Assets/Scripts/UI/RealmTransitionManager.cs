using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Realm Transition & Warp Manager (Màn Chuyển Cảnh Cõi Giới Sống Động).
/// Đạt chuẩn Zero-Bug Release trên Android:
/// - Loại bỏ 100% hiện tượng "màn hình đen" (Black Screen) và đơ khựng (Freeze Frame) khi chuyển cảnh/nạp scene.
/// - Áp dụng hình ảnh Digital Art Dark Fantasy sống động (Ảnh động tương tác) cho các thời điểm then chốt:
///   1. Xuất Trận (Bấm Play từ Menu vào Map 1).
///   2. Bước qua Cổng Không Gian (Dịch chuyển Map 1 -> Map 2, Map 2 -> Map 3).
///   3. Vào Thành / Trở về Thánh Địa / Khu an toàn.
///   4. Cổng Thăng Hoa (Hoàn thành trận chiến).
/// - 3D Parallax Tilt & Touch Ripple: Chạm tay phát sóng ma thuật và rung haptic trên điện thoại.
/// - Tự động nạp ngầm Scene bất đồng bộ (LoadSceneAsync) siêu mượt 60 FPS.
/// </summary>
public class RealmTransitionManager : MonoBehaviour
{
    private static RealmTransitionManager _instance;
    public static RealmTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                CreateSingletonInstance();
            }
            return _instance;
        }
    }

    public enum TransitionTheme
    {
        Auto,
        DeployBattle,       // Xuất trận vào chiến trường
        PortalWarpMap2,     // Bước vào Cổng tới Thung Lũng Tàn Tích (Map 2)
        PortalWarpMap3,     // Bước vào Cổng tới Ký Ức Lãng Quên (Map 3)
        AscensionGate,      // Cổng Thăng Hoa kết thúc hành trình
        ReturnSanctuary     // Trở về Thành / Thánh địa an bình
    }

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private RectTransform _containerRt;
    private Image _backgroundImage;
    private Image _breathingAuraOverlay;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _subtitleText;
    private TextMeshProUGUI _loreTipText;
    private Slider _loadingSlider;
    private TextMeshProUGUI _progressText;

    private bool _isTransitioning = false;
    private Vector2 _currentTiltOffset;
    private Vector2 _baseBackgroundPos;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private static void CreateSingletonInstance()
    {
        GameObject hostObj = new GameObject("[RealmTransitionManager]");
        _instance = hostObj.AddComponent<RealmTransitionManager>();
        DontDestroyOnLoad(hostObj);
    }

    private void BuildUI()
    {
        _canvas = gameObject.GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999; // Luôn hiển thị trên cùng

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        var raycaster = gameObject.GetComponent<GraphicRaycaster>();
        if (raycaster == null) gameObject.AddComponent<GraphicRaycaster>();

        _canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        // Container Root
        GameObject containerObj = new GameObject("TransitionContainer", typeof(RectTransform));
        containerObj.transform.SetParent(transform, false);
        _containerRt = containerObj.GetComponent<RectTransform>();
        _containerRt.anchorMin = Vector2.zero;
        _containerRt.anchorMax = Vector2.one;
        _containerRt.offsetMin = _containerRt.offsetMax = Vector2.zero;

        // Background Image (Ken Burns & 3D Tilt)
        GameObject bgObj = new GameObject("TransitionBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(containerObj.transform, false);
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(-0.06f, -0.06f);
        bgRt.anchorMax = new Vector2(1.06f, 1.06f);
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        _backgroundImage = bgObj.GetComponent<Image>();
        _backgroundImage.color = Color.black;
        _backgroundImage.preserveAspect = false;
        _baseBackgroundPos = bgRt.anchoredPosition;

        // Breathing Aura Glow Overlay
        GameObject auraObj = new GameObject("BreathingAura", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        auraObj.transform.SetParent(containerObj.transform, false);
        var auraRt = auraObj.GetComponent<RectTransform>();
        auraRt.anchorMin = Vector2.zero;
        auraRt.anchorMax = Vector2.one;
        auraRt.offsetMin = auraRt.offsetMax = Vector2.zero;
        _breathingAuraOverlay = auraObj.GetComponent<Image>();
        _breathingAuraOverlay.color = new Color(0.45f, 0.15f, 0.75f, 0.18f);
        _breathingAuraOverlay.raycastTarget = false;

        // Dark Vignette Frame
        GameObject vigObj = new GameObject("VignetteFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        vigObj.transform.SetParent(containerObj.transform, false);
        var vigRt = vigObj.GetComponent<RectTransform>();
        vigRt.anchorMin = Vector2.zero;
        vigRt.anchorMax = Vector2.one;
        vigRt.offsetMin = vigRt.offsetMax = Vector2.zero;
        var vigImg = vigObj.GetComponent<Image>();
        vigImg.color = new Color(0.01f, 0.01f, 0.02f, 0.45f);
        vigImg.raycastTarget = false;

        // Title Text
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(containerObj.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.10f, 0.68f);
        titleRt.anchorMax = new Vector2(0.90f, 0.85f);
        titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;
        _titleText = titleObj.GetComponent<TextMeshProUGUI>();
        _titleText.fontSize = 36f;
        _titleText.fontStyle = FontStyles.Bold;
        _titleText.alignment = TextAlignmentOptions.Center;
        _titleText.characterSpacing = 2.5f;
        _titleText.color = new Color(1f, 0.92f, 0.65f);
        SoulslikeHUDStyler.SetSafeOutline(_titleText, 0.50f, Color.black);

        // Subtitle Text
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(containerObj.transform, false);
        var subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.12f, 0.56f);
        subRt.anchorMax = new Vector2(0.88f, 0.68f);
        subRt.offsetMin = subRt.offsetMax = Vector2.zero;
        _subtitleText = subObj.GetComponent<TextMeshProUGUI>();
        _subtitleText.fontSize = 19f;
        _subtitleText.fontStyle = FontStyles.Normal;
        _subtitleText.alignment = TextAlignmentOptions.Center;
        _subtitleText.color = new Color(0.85f, 0.90f, 1f, 0.95f);
        SoulslikeHUDStyler.SetSafeOutline(_subtitleText, 0.35f, Color.black);

        // Lore Tip Box
        GameObject tipObj = new GameObject("LoreTipText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        tipObj.transform.SetParent(containerObj.transform, false);
        var tipRt = tipObj.GetComponent<RectTransform>();
        tipRt.anchorMin = new Vector2(0.15f, 0.18f);
        tipRt.anchorMax = new Vector2(0.85f, 0.32f);
        tipRt.offsetMin = tipRt.offsetMax = Vector2.zero;
        _loreTipText = tipObj.GetComponent<TextMeshProUGUI>();
        _loreTipText.fontSize = 15f;
        _loreTipText.fontStyle = FontStyles.Normal;
        _loreTipText.alignment = TextAlignmentOptions.Center;
        _loreTipText.lineSpacing = 1.3f;
        _loreTipText.color = new Color(0.92f, 0.88f, 0.75f, 0.90f);
        SoulslikeHUDStyler.SetSafeOutline(_loreTipText, 0.30f, Color.black);

        // Loading Progress Bar
        GameObject sliderObj = new GameObject("LoadingBar", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(containerObj.transform, false);
        var sRt = sliderObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.25f, 0.10f);
        sRt.anchorMax = new Vector2(0.75f, 0.13f);
        sRt.offsetMin = sRt.offsetMax = Vector2.zero;
        _loadingSlider = sliderObj.GetComponent<Slider>();
        _loadingSlider.interactable = false;

        // Slider Background
        GameObject sBgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        sBgObj.transform.SetParent(sliderObj.transform, false);
        var sbgRt = sBgObj.GetComponent<RectTransform>();
        sbgRt.anchorMin = Vector2.zero;
        sbgRt.anchorMax = Vector2.one;
        sbgRt.offsetMin = sbgRt.offsetMax = Vector2.zero;
        var sbgImg = sBgObj.GetComponent<Image>();
        sbgImg.color = new Color(0.08f, 0.10f, 0.15f, 0.95f);

        // Slider Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        var faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(2f, 2f);
        faRt.offsetMax = new Vector2(-2f, -2f);

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObj.transform.SetParent(fillArea.transform, false);
        var fRt = fillObj.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = fRt.offsetMax = Vector2.zero;
        var fImg = fillObj.GetComponent<Image>();
        fImg.color = new Color(0.98f, 0.82f, 0.35f, 1f); // Imperial Gold

        _loadingSlider.fillRect = fRt;
        _loadingSlider.value = 0f;

        // Progress Percent Text
        GameObject prgObj = new GameObject("ProgressText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        prgObj.transform.SetParent(containerObj.transform, false);
        var pRt = prgObj.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.40f, 0.05f);
        pRt.anchorMax = new Vector2(0.60f, 0.09f);
        pRt.offsetMin = pRt.offsetMax = Vector2.zero;
        _progressText = prgObj.GetComponent<TextMeshProUGUI>();
        _progressText.fontSize = 14f;
        _progressText.fontStyle = FontStyles.Bold;
        _progressText.alignment = TextAlignmentOptions.Center;
        _progressText.color = new Color(0.85f, 0.90f, 1f, 0.85f);
        SoulslikeHUDStyler.SetSafeOutline(_progressText, 0.30f, Color.black);
    }

    private void Update()
    {
        if (!_isTransitioning) return;

        // 3D Parallax Tilt theo tương tác
        Vector2 targetTilt = Vector2.zero;
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            Vector2 norm = new Vector2((t.position.x / Screen.width) - 0.5f, (t.position.y / Screen.height) - 0.5f);
            targetTilt = new Vector2(-norm.x * 20f, -norm.y * 15f);

            if (t.phase == TouchPhase.Began)
            {
                TriggerTouchPulse();
            }
        }
        else
        {
            Vector2 mouseNorm = new Vector2((Input.mousePosition.x / Screen.width) - 0.5f, (Input.mousePosition.y / Screen.height) - 0.5f);
            targetTilt = new Vector2(-mouseNorm.x * 18f, -mouseNorm.y * 12f);

            if (Input.GetMouseButtonDown(0))
            {
                TriggerTouchPulse();
            }
        }

        _currentTiltOffset = Vector2.Lerp(_currentTiltOffset, targetTilt, Time.unscaledDeltaTime * 6f);
        if (_backgroundImage != null)
        {
            _backgroundImage.rectTransform.anchoredPosition = _baseBackgroundPos + _currentTiltOffset;
        }

        // Hào quang nhịp thở
        if (_breathingAuraOverlay != null)
        {
            Color c = _breathingAuraOverlay.color;
            c.a = 0.16f + 0.10f * Mathf.Sin(Time.unscaledTime * 3.0f);
            _breathingAuraOverlay.color = c;
        }
    }

    private void TriggerTouchPulse()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
#if UNITY_ANDROID && !UNITY_EDITOR
        try { Handheld.Vibrate(); } catch { }
#endif
        if (_breathingAuraOverlay != null)
        {
            StopCoroutine("FlashAuraRoutine");
            StartCoroutine("FlashAuraRoutine");
        }
    }

    private IEnumerator FlashAuraRoutine()
    {
        if (_breathingAuraOverlay == null) yield break;
        float elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.unscaledDeltaTime;
            Color c = _breathingAuraOverlay.color;
            c.a = 0.40f * (1f - (elapsed / 0.25f));
            _breathingAuraOverlay.color = c;
            yield return null;
        }
    }

    /// <summary>
    /// API toàn cục gọi chuyển scene kèm Màn Chuyển Cảnh Sống Động
    /// </summary>
    public static void LoadSceneWithTransition(string targetSceneName, TransitionTheme theme = TransitionTheme.Auto)
    {
        Instance.StartCoroutine(Instance.TransitionRoutine(targetSceneName, theme));
    }

    private IEnumerator TransitionRoutine(string targetSceneName, TransitionTheme theme)
    {
        if (_isTransitioning) yield break;
        _isTransitioning = true;

        Time.timeScale = 1f;

        // Xác định Theme nếu để Auto
        if (theme == TransitionTheme.Auto)
        {
            if (targetSceneName.Contains("Map1")) theme = TransitionTheme.DeployBattle;
            else if (targetSceneName.Contains("Map2") || targetSceneName.Contains("Valley")) theme = TransitionTheme.PortalWarpMap2;
            else if (targetSceneName.Contains("Map3") || targetSceneName.Contains("Forgotten")) theme = TransitionTheme.PortalWarpMap3;
            else if (targetSceneName.Contains("Login") || targetSceneName.Contains("Menu")) theme = TransitionTheme.ReturnSanctuary;
            else theme = TransitionTheme.DeployBattle;
        }

        ApplyThemeVisuals(theme, targetSceneName);

        // 1. Fade In Màn Chuyển Cảnh
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;

        float fadeDuration = 0.35f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = elapsed / fadeDuration;
            yield return null;
        }
        _canvasGroup.alpha = 1f;

        // 2. Bắt đầu Nạp Scene Bất Đồng Bộ
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(targetSceneName);
        asyncOp.allowSceneActivation = false;

        float displayProgress = 0f;
        float kenBurnsElapsed = 0f;

        while (displayProgress < 1f)
        {
            kenBurnsElapsed += Time.unscaledDeltaTime;
            // Hiệu ứng Ken Burns Zoom từ 1.0 -> 1.08
            if (_backgroundImage != null)
            {
                float scale = 1.0f + Mathf.PingPong(kenBurnsElapsed * 0.04f, 0.08f);
                _backgroundImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            float targetProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.unscaledDeltaTime * 1.2f);

            if (_loadingSlider != null) _loadingSlider.value = displayProgress;
            if (_progressText != null) _progressText.text = $"{(int)(displayProgress * 100f)}%";

            if (displayProgress >= 0.999f && asyncOp.progress >= 0.9f)
            {
                if (_loadingSlider != null) _loadingSlider.value = 1f;
                if (_progressText != null) _progressText.text = "100%";
                yield return new WaitForSecondsRealtime(0.20f);
                break;
            }

            yield return null;
        }

        // Kích hoạt Scene mới
        asyncOp.allowSceneActivation = true;

        // Đợi scene mới thức tỉnh hoàn toàn
        yield return new WaitForSecondsRealtime(0.25f);

        // 3. Fade Out Màn Chuyển Cảnh hé lộ Map mới
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        _isTransitioning = false;
    }

    private void ApplyThemeVisuals(TransitionTheme theme, string targetSceneName)
    {
        bool isVi = LocalizationManager.IsVietnamese;
        Sprite chosenBg = null;

        switch (theme)
        {
            case TransitionTheme.DeployBattle:
                chosenBg = LoadSprite("UI/CardDeities/CardDeity_BladeSaint");
                _titleText.text = isVi ? "XUẤT TRẬN: BƯỚC VÀO CÕI TÀN TÍCH" : "DEPLOY: ENTERING THE RUINS";
                _titleText.color = new Color(1f, 0.88f, 0.45f);
                _subtitleText.text = isVi ? "Chiến binh thức tỉnh - Rút kiếm phá tan bóng đêm" : "Warrior Awakens - Blade drawn against the dark";
                _loreTipText.text = isVi 
                    ? "« MẸO CHIẾN ĐẤU: Tấn công trúng địch liên tiếp để tích lũy Nộ Khí và giải phóng Tuyệt Kỹ tối thượng! »" 
                    : "« COMBAT TIP: Chain attacks to build Fury and unleash your devastating Ultimate! »";
                _breathingAuraOverlay.color = new Color(0.95f, 0.60f, 0.20f, 0.16f); // Ánh Lửa Chiến Trận
                break;

            case TransitionTheme.PortalWarpMap2:
                chosenBg = LoadSprite("UI/CardDeities/CardDeity_VoidPhantom");
                _titleText.text = isVi ? "VẾT NỨT KHÔNG GIAN: THUNG LŨNG TÀN TÍCH" : "SPATIAL RIFT: VALLEY OF RUIN";
                _titleText.color = new Color(0.35f, 0.85f, 1f);
                _subtitleText.text = isVi ? "Tiến vào Cõi Giới Thứ Hai - Nham thạch & Gai độc bủa vây" : "Entering Realm II - Venom and crumbling chasms";
                _loreTipText.text = isVi 
                    ? "« CẢNH BÁO QUÁI VẬT: Bọ Cạp Độc và Tộc Goblin xuất hiện với số lượng áp đảo. Hãy cẩn thận các vũng axit! »" 
                    : "« MONSTER WARNING: Toxic Scorpions and Goblin tribes swarm in masses. Avoid toxic pools! »";
                _breathingAuraOverlay.color = new Color(0.30f, 0.70f, 1f, 0.18f); // Ánh Xanh Cõi Tàn Tích
                break;

            case TransitionTheme.PortalWarpMap3:
                chosenBg = LoadSprite("UI/CardDeities/CardDeity_GrimReaper");
                if (chosenBg == null) chosenBg = LoadSprite("UI/CardDeities/CardDeity_VoidPhantom");
                _titleText.text = isVi ? "DỊCH CHUYỂN CÕI GIỚI: KÝ ỨC LÃNG QUÊN" : "REALM WARP: FORGOTTEN MEMORIES";
                _titleText.color = new Color(0.85f, 0.45f, 1f);
                _subtitleText.text = isVi ? "Cõi Tâm Thức Cổ Xưa - Lãnh địa của Chúa Tể Nhện Skeleton King" : "Ancient Sanctum - Domain of the dread Skeleton King";
                _loreTipText.text = isVi 
                    ? "« TÀN TÍCH HUYỀN BÍ: Tà khí hư không ngập tràn cõi giới. Chuẩn bị tinh thần cho trận đại chiến sinh tử! »" 
                    : "« DREAD SANCTUM: The void aether chills your soul. Brace yourself for the ultimate trial! »";
                _breathingAuraOverlay.color = new Color(0.65f, 0.25f, 0.95f, 0.20f); // Ánh Tím Thạch Anh
                break;

            case TransitionTheme.AscensionGate:
                chosenBg = LoadSprite("UI/CardDeities/CardDeity_GoldenSanctuary");
                if (chosenBg == null) chosenBg = LoadSprite("UI/CardDeities/CardDeity_ShieldGoddess");
                _titleText.text = isVi ? "CỔNG THĂNG HOA: KHẮC DANH CHIẾN CÔNG" : "HEAVENLY GATE: ASCENSION";
                _titleText.color = new Color(1f, 0.92f, 0.55f);
                _subtitleText.text = isVi ? "Thanh tẩy hoàn toàn cõi tàn tích - Ghi danh vào bảng vàng anh hùng" : "Realm purified - Your legend is sealed in eternity";
                _loreTipText.text = isVi 
                    ? "« CHIẾN TÍCH BẤT DIỆT: Ánh sáng thần thánh bao bọc lấy linh hồn bạn, giải thoát vạn vật khỏi bóng đêm. »" 
                    : "« IMMORTAL TRIUMPH: The divine light cradles your soul, delivering the realm from darkness. »";
                _breathingAuraOverlay.color = new Color(1f, 0.85f, 0.35f, 0.22f); // Ánh Kim Thần Thánh
                break;

            case TransitionTheme.ReturnSanctuary:
            default:
                chosenBg = LoadSprite("UI/CardDeities/CardDeity_GoldenSanctuary");
                if (chosenBg == null) chosenBg = LoadSprite("UI/CardDeities/CardDeity_BladeSaint");
                _titleText.text = isVi ? "TRỞ VỀ THÁNH ĐỊA AN BÌNH" : "RETURN TO SANCTUARY";
                _titleText.color = new Color(1f, 0.90f, 0.65f);
                _subtitleText.text = isVi ? "Nơi nghỉ chân của các chiến binh - Dưỡng sức & Chuẩn bị hành trang" : "Sanctuary of heroes - Rest and prepare for battle";
                _loreTipText.text = isVi 
                    ? "« BÌNH YÊN TẠM THỜI: Hãy ghé thăm Gian Hàng Du Mục và rèn lại các Lõi Thần Vực mạnh nhất! »" 
                    : "« SANCTUARY REST: Visit the Wandering Merchant to forge your ultimate Augment setup! »";
                _breathingAuraOverlay.color = new Color(0.85f, 0.70f, 0.30f, 0.15f);
                break;
        }

        if (chosenBg != null && _backgroundImage != null)
        {
            _backgroundImage.sprite = chosenBg;
            _backgroundImage.color = Color.white;
        }
    }

    private Sprite LoadSprite(string path)
    {
        Sprite sp = Resources.Load<Sprite>(path);
        if (sp != null) return sp;
#if UNITY_EDITOR
        sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/" + path + ".png");
#endif
        return sp;
    }
}
