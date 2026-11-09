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
        LoginIntro,         // Mở đầu từ màn hình Login vào Sảnh
        DeployBattle,       // Xuất trận vào chiến trường
        PortalWarpMap2,     // Bước vào Cổng tới Thung Lũng Tàn Tích (Map 2)
        PortalWarpMap3,     // Bước vào Cổng tới Ký Ức Lãng Quên (Map 3)
        CalamityRealm,      // Bước vào Vết Nứt Ác Mộng & Cõi Thảm Họa Thời Tiết (Mưa Axit, Bão Tuyết, Dung Nham)
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
        _canvas.sortingOrder = 32767; // Tối đa tuyệt đối trong Unity Canvas (short.MaxValue), đảm bảo luôn nằm trên cùng tất cả mọi UI khác
        _canvas.overrideSorting = true;

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
        GameObject containerObj = new GameObject("TransitionContainer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        containerObj.transform.SetParent(transform, false);
        _containerRt = containerObj.GetComponent<RectTransform>();
        _containerRt.anchorMin = Vector2.zero;
        _containerRt.anchorMax = Vector2.one;
        _containerRt.offsetMin = _containerRt.offsetMax = Vector2.zero;
        var containerImg = containerObj.GetComponent<Image>();
        containerImg.color = new Color(0.03f, 0.04f, 0.07f, 1f); // Dark fantasy solid backing chống hở viền 100%
        containerImg.raycastTarget = false;

        // Background Image (Chuẩn Widescreen Landscape, EnvelopeParent bao phủ 100% màn hình mọi tỉ lệ)
        GameObject bgObj = new GameObject("TransitionBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
        bgObj.transform.SetParent(containerObj.transform, false);
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bgRt.localScale = new Vector3(1.10f, 1.10f, 1f); // 10% overscan chống triệt để viền đen trên màn siêu dài 18:9, 19.5:9, 20:9
        _backgroundImage = bgObj.GetComponent<Image>();
        _backgroundImage.color = Color.white;
        _backgroundImage.preserveAspect = false; // Tắt preserveAspect inner-fit để AspectRatioFitter EnvelopeParent lấp đầy 100% khung hình
        _backgroundImage.raycastTarget = false; // Tuyệt đối không chặn raycast của UI khác
        _baseBackgroundPos = Vector2.zero;

        var bgArf = bgObj.GetComponent<AspectRatioFitter>();
        bgArf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        bgArf.aspectRatio = 1.7778f; // Chuẩn 16:9 gốc

        // Breathing Aura Glow Overlay (Ẩn hoàn toàn để loại bỏ lớp phủ màu tím/cam mờ đục theo yêu cầu)
        GameObject auraObj = new GameObject("BreathingAura", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        auraObj.transform.SetParent(containerObj.transform, false);
        _breathingAuraOverlay = auraObj.GetComponent<Image>();
        _breathingAuraOverlay.color = Color.clear;
        _breathingAuraOverlay.raycastTarget = false;
        auraObj.SetActive(false); // Bỏ hiệu ứng màu mè mờ đục

        // Dark Vignette Frame (Nhẹ nhàng ở 4 góc để thanh tiến trình nổi bật)
        GameObject vigObj = new GameObject("VignetteFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        vigObj.transform.SetParent(containerObj.transform, false);
        var vigRt = vigObj.GetComponent<RectTransform>();
        vigRt.anchorMin = Vector2.zero;
        vigRt.anchorMax = Vector2.one;
        vigRt.offsetMin = vigRt.offsetMax = Vector2.zero;
        var vigImg = vigObj.GetComponent<Image>();
        vigImg.color = new Color(0.01f, 0.01f, 0.02f, 0.25f);
        vigImg.raycastTarget = false;

        // Title Text & Subtitle Text (Ẩn hoàn toàn để không đè chữ to lên ngực nhân vật/tranh vẽ)
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(containerObj.transform, false);
        _titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleObj.SetActive(false); // Bỏ chữ to đùng đè lên tranh

        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(containerObj.transform, false);
        _subtitleText = subObj.GetComponent<TextMeshProUGUI>();
        subObj.SetActive(false); // Bỏ subtitle đè tranh

        // Lore Tip Box (Nằm ngay phía trên thanh loading ở đáy màn hình, cách xa trung tâm tranh)
        GameObject tipObj = new GameObject("LoreTipText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        tipObj.transform.SetParent(containerObj.transform, false);
        var tipRt = tipObj.GetComponent<RectTransform>();
        tipRt.anchorMin = new Vector2(0.5f, 0f);
        tipRt.anchorMax = new Vector2(0.5f, 0f);
        tipRt.pivot = new Vector2(0.5f, 0.5f);
        tipRt.anchoredPosition = new Vector2(0f, 95f);
        tipRt.sizeDelta = new Vector2(850f, 28f);
        _loreTipText = tipObj.GetComponent<TextMeshProUGUI>();
        _loreTipText.fontSize = 15f;
        _loreTipText.fontStyle = FontStyles.Bold;
        _loreTipText.alignment = TextAlignmentOptions.Center;
        _loreTipText.color = new Color(1f, 0.88f, 0.55f);
        SoulslikeHUDStyler.SetSafeOutline(_loreTipText, 0.35f, Color.black);

        // ─── Loading Progress Bar: Chuẩn Asset Soulslike Khung Hoàng Kim & Ruột Trắng Phát Sáng ───
        GameObject sliderObj = new GameObject("LoadingBar", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(containerObj.transform, false);
        var sRt = sliderObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.5f, 0f);
        sRt.anchorMax = new Vector2(0.5f, 0f);
        sRt.pivot = new Vector2(0.5f, 0.5f);
        sRt.anchoredPosition = new Vector2(0f, 62f);
        sRt.sizeDelta = new Vector2(680f, 20f); // Thanh mảnh, tinh tế chuẩn Soulslike
        _loadingSlider = sliderObj.GetComponent<Slider>();
        _loadingSlider.interactable = false;

        // Rãnh Đá Obsidian Đen Sâu Thẳm
        var sbgImg = sliderObj.AddComponent<Image>();
        sbgImg.sprite = SoulslikeHUDStyler.GetObsidianTroughSprite();
        sbgImg.type = Image.Type.Sliced;
        sbgImg.color = new Color(0.06f, 0.07f, 0.10f, 0.96f);

        // Khung viền ngoài Boss Bar Gothic Hoàng Kim
        GameObject frameObj = new GameObject("BossFrameDecoration", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameObj.transform.SetParent(sliderObj.transform, false);
        frameObj.transform.SetAsFirstSibling();
        var fDecorRt = frameObj.GetComponent<RectTransform>();
        fDecorRt.anchorMin = Vector2.zero;
        fDecorRt.anchorMax = Vector2.one;
        fDecorRt.offsetMin = new Vector2(-10f, -5f);
        fDecorRt.offsetMax = new Vector2(10f, 5f);
        var frameImg = frameObj.GetComponent<Image>();
        frameImg.sprite = SoulslikeHUDStyler.GetBossBarFrameSprite();
        frameImg.type = Image.Type.Sliced;
        frameImg.color = new Color(1f, 0.95f, 0.85f, 1f);
        frameImg.raycastTarget = false;

        // Slider Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        var faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(2f, 2f);
        faRt.offsetMax = new Vector2(-2f, -2f);

        // Dải nạp chính: MÀU TRẮNG BẠCH KIM PHÁT SÁNG (White Platinum Glow)
        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillObj.transform.SetParent(fillArea.transform, false);
        var fRt = fillObj.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = fRt.offsetMax = Vector2.zero;
        var fImg = fillObj.GetComponent<Image>();
        fImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        fImg.type = Image.Type.Sliced;
        fImg.color = new Color(0.98f, 0.99f, 1.0f, 1f); // Trắng sáng tinh khiết

        // Hào quang tỏa sáng trắng bạc bao quanh dòng năng lượng
        var fillGlow = fillObj.AddComponent<Outline>();
        fillGlow.effectColor = new Color(0.85f, 0.95f, 1f, 0.85f);
        fillGlow.effectDistance = new Vector2(1.5f, 1.5f);

        _loadingSlider.fillRect = fRt;
        _loadingSlider.value = 0f;

        // Progress Percent Text (Nằm ngay bên dưới thanh loading)
        GameObject prgObj = new GameObject("ProgressText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        prgObj.transform.SetParent(containerObj.transform, false);
        var pRt = prgObj.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.5f, 0f);
        pRt.anchorMax = new Vector2(0.5f, 0f);
        pRt.pivot = new Vector2(0.5f, 0.5f);
        pRt.anchoredPosition = new Vector2(0f, 32f);
        pRt.sizeDelta = new Vector2(300f, 26f);
        _progressText = prgObj.GetComponent<TextMeshProUGUI>();
        _progressText.fontSize = 14f;
        _progressText.fontStyle = FontStyles.Bold;
        _progressText.characterSpacing = 2.0f;
        _progressText.alignment = TextAlignmentOptions.Center;
        _progressText.color = new Color(0.96f, 0.98f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(_progressText, 0.35f, Color.black);

        // Mặc định ẩn hoàn toàn Canvas và Container khi chưa có lệnh chuyển cảnh
        if (_containerRt != null) _containerRt.gameObject.SetActive(false);
        if (_canvas != null) _canvas.enabled = false;
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
            if (CorruptedCycleManager.IsCorruptedActive) theme = TransitionTheme.CalamityRealm;
            else if (targetSceneName.Contains("Map1")) theme = TransitionTheme.DeployBattle;
            else if (targetSceneName.Contains("Map2") || targetSceneName.Contains("Valley")) theme = TransitionTheme.PortalWarpMap2;
            else if (targetSceneName.Contains("Map3") || targetSceneName.Contains("Forgotten")) theme = TransitionTheme.PortalWarpMap3;
            else if (targetSceneName.Contains("Login")) theme = TransitionTheme.LoginIntro;
            else if (targetSceneName.Contains("Menu")) theme = TransitionTheme.ReturnSanctuary;
            else theme = TransitionTheme.DeployBattle;
        }

        ApplyThemeVisuals(theme, targetSceneName);

        // Đảm bảo Canvas và Container được bật lên và ĐẶT LÊN LỚP CAO NHẤT (SortingOrder = 32767)
        if (_canvas != null)
        {
            _canvas.enabled = true;
            _canvas.sortingOrder = 32767;
        }
        if (_containerRt != null)
        {
            _containerRt.gameObject.SetActive(true);
            _containerRt.SetAsLastSibling();
        }

        if (_loadingSlider != null) _loadingSlider.value = 0f;
        if (_progressText != null) _progressText.text = "0%";

        // 1. CHE PHỦ 100% NGAY LẬP TỨC (Alpha = 1.0)
        // Lập tức hiển thị màn hình chuyển cảnh full màn hình, chặn toàn bộ raycast, không để lộ 1 frame màn cũ
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        _canvasGroup.alpha = 1f;

        // 2. Bắt đầu Nạp Scene Bất Đồng Bộ
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(targetSceneName);
        asyncOp.allowSceneActivation = false;

        float displayProgress = 0f;
        float kenBurnsElapsed = 0f;

        while (displayProgress < 1f)
        {
            kenBurnsElapsed += Time.unscaledDeltaTime;
            if (_backgroundImage != null)
            {
                float scale = 1.10f + Mathf.PingPong(kenBurnsElapsed * 0.02f, 0.04f);
                _backgroundImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            float targetProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.unscaledDeltaTime * 1.0f);

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

        // 3. KÍCH HOẠT SCENE MỚI TRONG KHI MÀN HÌNH CHUYỂN CẢNH VẪN GIỮ NGUYÊN 100% OPAQUE (alpha = 1.0)
        // Tuyệt đối không fade out ở đây để triệt tiêu 100% lỗi lộ lại màn hình cũ
        asyncOp.allowSceneActivation = true;

        // Chờ đến khi Unity hoàn tất nạp scene mới
        while (!asyncOp.isDone)
        {
            yield return null;
        }

        // Chờ đến khi Scene mới đã thực sự là Active Scene trong Unity
        while (SceneManager.GetActiveScene().name != targetSceneName)
        {
            yield return null;
        }

        // Đợi 3 frame liên tiếp để Camera, Terrain, Spawner, Fog/Lighting và UI của Scene mới thức tỉnh và render xong 100%
        for (int i = 0; i < 3; i++)
        {
            yield return null;
        }
        yield return new WaitForEndOfFrame();

        // 4. BÂY GIỜ SCENE MỚI ĐÃ SẴN SÀNG HOÀN TOÀN PHÍA SAU -> Fade Out Màn Chuyển Cảnh để hé lộ Scene mới
        float fadeDuration = 0.25f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        // Tắt hoàn toàn Container và Canvas để không bao giờ chắn raycast hoặc bị nhầm lẫn
        if (_containerRt != null) _containerRt.gameObject.SetActive(false);
        if (_canvas != null) _canvas.enabled = false;

        _isTransitioning = false;
    }

    private void ApplyThemeVisuals(TransitionTheme theme, string targetSceneName)
    {
        bool isVi = LocalizationManager.IsVietnamese;
        Sprite chosenBg = null;

        // Sử dụng các bức tranh phong cảnh Dark Fantasy Widescreen Landscape sắc nét, bao phủ 100% màn hình
        switch (theme)
        {
            case TransitionTheme.LoginIntro:
            case TransitionTheme.ReturnSanctuary:
                chosenBg = LoadSprite("Backgrounds/transition_sanctuary_return") ?? LoadSprite("Backgrounds/mainmenu_bg_epic");
                if (_loreTipText != null)
                {
                    _loreTipText.text = isVi 
                        ? "« Đang khởi hành vào vương quốc Tàn Tích thượng cổ... »" 
                        : "« Entering the ancient Ruinbound realm... »";
                }
                break;

            case TransitionTheme.DeployBattle:
                chosenBg = LoadSprite("Backgrounds/transition_map1_deploy") ?? LoadSprite("Backgrounds/mainmenu_bg_epic");
                if (_loreTipText != null)
                {
                    _loreTipText.text = isVi 
                        ? "« MẸO CHIẾN ĐẤU: Tấn công trúng đích liên tiếp để tích lũy Nộ Khí và giải phóng Tuyệt Kỹ tối thượng! »" 
                        : "« COMBAT TIP: Chain attacks to build Fury and unleash your devastating Ultimate! »";
                }
                break;

            case TransitionTheme.PortalWarpMap2:
                chosenBg = LoadSprite("Backgrounds/transition_map2_valley") ?? LoadSprite("Backgrounds/mainmenu_bg_epic");
                if (_loreTipText != null)
                {
                    _loreTipText.text = isVi 
                        ? "« THUNG LŨNG TÀN TÍCH: Bọ Cạp Độc và Tộc Goblin xuất hiện với số lượng áp đảo. Hãy cẩn thận các vũng axit! »" 
                        : "« VALLEY RUINS: Toxic Scorpions and Goblin tribes swarm in masses. Avoid toxic pools! »";
                }
                break;

            case TransitionTheme.PortalWarpMap3:
                chosenBg = LoadSprite("Backgrounds/transition_map3_void") ?? LoadSprite("Backgrounds/mainmenu_bg_epic");
                if (_loreTipText != null)
                {
                    _loreTipText.text = isVi 
                        ? "« KÝ ỨC LÃNG QUÊN: Tà khí hư không ngập tràn cõi giới. Chuẩn bị tinh thần cho trận đại chiến sinh tử! »" 
                        : "« FORGOTTEN MEMORIES: The void aether chills your soul. Brace yourself for the ultimate trial! »";
                }
                break;

            case TransitionTheme.CalamityRealm:
                chosenBg = LoadSprite("Backgrounds/transition_calamity_weather") ?? LoadSprite("Backgrounds/mainmenu_bg_epic");
                var currentRealm = CorruptedCycleManager.CurrentRealm;
                string realmName = CorruptedCycleManager.GetRealmName(currentRealm);
                if (_loreTipText != null)
                {
                    switch (currentRealm)
                    {
                        case CalamityRealm.VoidNightmare:
                            _loreTipText.text = isVi 
                                ? "« CÕI ÁC MỘNG HƯ KHÔNG: Tà khí nuốt chửng ánh sáng. Hãy cảnh giác với các quả cầu ác mộng! »" 
                                : "« VOID NIGHTMARE REALM: Shadows consume all light. Beware the orbs of darkness! »";
                            break;
                        case CalamityRealm.AcidDownpour:
                            _loreTipText.text = isVi 
                                ? "« CÕI MƯA AXIT ĂN MÒN: Mưa độc rút máu liên tục. Tấn công quái dồn dập để duy trì trung hòa độc tố! »" 
                                : "« ACID DOWNPOUR REALM: Toxic rain corrodes health. Attack continuously to neutralize venom! »";
                            break;
                        case CalamityRealm.MagmaCalamity:
                            _loreTipText.text = isVi 
                                ? "« CÕI DUNG NHAM RỰC LỬA: Nham thạch sôi trào dưới chân. Tránh xa các cột lửa và vụ nổ địa ngục! »" 
                                : "« MAGMA CALAMITY REALM: Lava boils beneath your feet. Dodge fiery pillars and hellish blasts! »";
                            break;
                        case CalamityRealm.FrostfallBlizzard:
                            _loreTipText.text = isVi 
                                ? "« CÕI BÃO TUYẾT BĂNG GIÁ: Bão tuyết làm chậm và đóng băng linh hồn. Luôn di chuyển và tung chiêu để sinh tồn! »" 
                                : "« FROSTFALL BLIZZARD REALM: Howling blizzards chill your soul. Keep moving to survive the freeze! »";
                            break;
                        default:
                            _loreTipText.text = isVi 
                                ? $"« TIẾN VÀO {realmName.ToUpper()}: Tai ương ập tới, thử thách tối thượng bắt đầu! »" 
                                : $"« ENTERING {realmName.ToUpper()}: Calamity strikes, the ultimate trial begins! »";
                            break;
                    }
                }
                break;

            case TransitionTheme.AscensionGate:
            default:
                chosenBg = LoadSprite("Backgrounds/mainmenu_bg_epic");
                if (_loreTipText != null)
                {
                    _loreTipText.text = isVi 
                        ? "« CHIẾN TÍCH BẤT DIỆT: Ánh sáng thần thánh bao bọc lấy linh hồn bạn, giải thoát vạn vật khỏi bóng đêm. »" 
                        : "« IMMORTAL TRIUMPH: The divine light cradles your soul, delivering the realm from darkness. »";
                }
                break;
        }

        if (chosenBg != null && _backgroundImage != null)
        {
            _backgroundImage.sprite = chosenBg;
            _backgroundImage.color = Color.white;
            _backgroundImage.preserveAspect = false;

            var bgArf = _backgroundImage.GetComponent<AspectRatioFitter>();
            if (bgArf != null && chosenBg.rect.height > 0)
            {
                bgArf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                bgArf.aspectRatio = (float)chosenBg.rect.width / chosenBg.rect.height;
            }
        }
    }

    private Sprite LoadSprite(string path)
    {
        Sprite sp = Resources.Load<Sprite>(path);
        if (sp != null) return sp;
#if UNITY_EDITOR
        sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/" + path + ".png");
        if (sp != null) return sp;
        sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/" + path + ".jpg");
        if (sp != null) return sp;
        sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/" + path + ".jpg");
        if (sp != null) return sp;
        sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/" + path + ".png");
        if (sp != null) return sp;
#endif
        return sp;
    }
}

