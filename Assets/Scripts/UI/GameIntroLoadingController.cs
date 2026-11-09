using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Live Interactive Cinematic Motion Intro & Loading Controller.
/// Hệ thống Mở màn Ảnh Động Tương Tác Trực Tiếp (Interactive Motion Graphics & Cinemagraph Video) chuẩn Dark Fantasy:
/// - Tự động phát hiện và trình chiếu Video Cinemagraph chất lượng cao từ Kling AI (kling_20261109_VIDEO_Cinemagrap_2987_0.mp4).
/// - Cơ chế Bảo Vệ Android Zero-Bug: Tự động Fallback sang hệ thống Motion Graphics bằng Digital Art nếu VideoPlayer không tương thích hoặc lỗi codec trên điện thoại cũ.
/// - 3D Interactive Parallax Tilt: Khung hình nghiêng tự nhiên theo con trỏ chuột (PC) hoặc cảm ứng / con quay hồi chuyển Gyroscope (Mobile).
/// - Touch Shockwave & Haptic: Chạm tay vào màn hình sẽ bùng sáng sóng ma thuật tím, rung nhẹ điện thoại và phát âm thanh Soulslike.
/// - Breathing Aura Glow: Ánh sáng tím huyền bí phập phồng nhịp thở sinh động.
/// - Responsive đa tỉ lệ (16:9, 18:9, 20:9), hỗ trợ Chạm bất kỳ đâu để tương tác hoặc Bỏ qua tức thì.
/// </summary>
public class GameIntroLoadingController : MonoBehaviour
{
    public static GameIntroLoadingController Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject cinematicPanel;
    [SerializeField] private GameObject loadingProgressPanel;

    [Header("Cinematic Elements")]
    [SerializeField] private CanvasGroup cinematicCg;
    [SerializeField] private Image cinematicSlideImage;
    [SerializeField] private TextMeshProUGUI cinematicSubtitleText;
    [SerializeField] private Button skipButton;
    [SerializeField] private CanvasGroup skipButtonCg;

    [Header("Loading Progress Elements")]
    [SerializeField] private CanvasGroup loadingProgressCg;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TextMeshProUGUI progressPercentText;
    [SerializeField] private TextMeshProUGUI progressStatusText;
    [SerializeField] private TextMeshProUGUI gameTitleText;
    [SerializeField] private TextMeshProUGUI gameSubtitleText;

    // Runtime Interactive Elements
    private RawImage _cinematicVideoRawImage;
    private VideoPlayer _videoPlayer;
    private RenderTexture _videoRenderTexture;
    private Image _breathingGlowImage;
    private TextMeshProUGUI _tapHintText;
    private RectTransform _slideRectTransform;
    private Vector2 _slideBaseAnchoredPos;
    private Vector2 _currentTiltOffset;
    private bool _hasSkipped = false;
    private bool _advanceSlideRequested = false;
    private bool _isInCinematic = false;
    private bool _isLoadingScene = false;
    private Coroutine _introFlowRoutine;

    // Danh sách 3 tác phẩm Digital Art đại diện cho 3 giai đoạn cốt truyện
    private Sprite _spriteVoidPhantom;
    private Sprite _spriteInfernoFiend;
    private Sprite _spriteBladeSaint;

    private void Awake()
    {
        Instance = this;
        LoadDeityArtwork();
    }

    private void LoadDeityArtwork()
    {
        if (_spriteVoidPhantom == null) _spriteVoidPhantom = Resources.Load<Sprite>("UI/CardDeities/CardDeity_VoidPhantom");
        if (_spriteInfernoFiend == null) _spriteInfernoFiend = Resources.Load<Sprite>("UI/CardDeities/CardDeity_InfernoFiend");
        if (_spriteBladeSaint == null) _spriteBladeSaint = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BladeSaint");

#if UNITY_EDITOR
        if (_spriteVoidPhantom == null) _spriteVoidPhantom = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/CardDeities/CardDeity_VoidPhantom.png");
        if (_spriteInfernoFiend == null) _spriteInfernoFiend = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/CardDeities/CardDeity_InfernoFiend.png");
        if (_spriteBladeSaint == null) _spriteBladeSaint = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/CardDeities/CardDeity_BladeSaint.png");
#endif
    }

    private void SetupInteractiveLayers()
    {
        if (cinematicPanel == null) return;

        if (cinematicSlideImage != null)
        {
            _slideRectTransform = cinematicSlideImage.GetComponent<RectTransform>();
            if (_slideRectTransform != null)
            {
                _slideBaseAnchoredPos = _slideRectTransform.anchoredPosition;
            }
        }

        // 1. Kiểm tra và thiết lập VideoPlayer nếu có video Kling Cinemagraph trong project
        SetupVideoPlayer();

        // 2. Tạo Lớp Hào Quang Nhịp Thở Huyền Bí (Breathing Aura Glow Overlay)
        Transform existingGlow = cinematicPanel.transform.Find("InteractiveBreathingAura");
        if (existingGlow == null)
        {
            GameObject glowObj = new GameObject("InteractiveBreathingAura", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glowObj.transform.SetParent(cinematicPanel.transform, false);

            RectTransform grt = glowObj.GetComponent<RectTransform>();
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.offsetMin = grt.offsetMax = Vector2.zero;

            _breathingGlowImage = glowObj.GetComponent<Image>();
            _breathingGlowImage.color = new Color(0.55f, 0.18f, 0.85f, 0.16f);
            _breathingGlowImage.raycastTarget = false;
        }
        else
        {
            _breathingGlowImage = existingGlow.GetComponent<Image>();
        }

        // 3. Tạo Dòng Chữ Nhắc Nhở Tương Tác Sống Động (Tap Hint)
        Transform existingHint = cinematicPanel.transform.Find("InteractiveTapHint");
        if (existingHint == null)
        {
            GameObject hintObj = new GameObject("InteractiveTapHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            hintObj.transform.SetParent(cinematicPanel.transform, false);

            RectTransform hrt = hintObj.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.10f, 0.02f);
            hrt.anchorMax = new Vector2(0.90f, 0.06f);
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;

            _tapHintText = hintObj.GetComponent<TextMeshProUGUI>();
            _tapHintText.fontSize = 14f;
            _tapHintText.fontStyle = FontStyles.Bold;
            _tapHintText.alignment = TextAlignmentOptions.Center;
            _tapHintText.color = new Color(0.85f, 0.88f, 0.95f, 0.70f);
            _tapHintText.raycastTarget = false;
            SoulslikeHUDStyler.SetSafeOutline(_tapHintText, 0.35f, Color.black);
        }
        else
        {
            _tapHintText = existingHint.GetComponent<TextMeshProUGUI>();
        }
    }

    private void SetupVideoPlayer()
    {
        string videoPath = System.IO.Path.Combine(Application.dataPath, "Resources/UI/kling_20261109_VIDEO_Cinemagrap_2987_0.mp4");
        if (!System.IO.File.Exists(videoPath)) return;

        Transform existingRaw = cinematicPanel.transform.Find("CinematicVideoSurface");
        if (existingRaw == null)
        {
            GameObject rawObj = new GameObject("CinematicVideoSurface", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(VideoPlayer));
            rawObj.transform.SetParent(cinematicPanel.transform, false);
            rawObj.transform.SetSiblingIndex(cinematicSlideImage != null ? cinematicSlideImage.transform.GetSiblingIndex() + 1 : 0);

            RectTransform rrt = rawObj.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.05f, 0.12f);
            rrt.anchorMax = new Vector2(0.95f, 0.92f);
            rrt.offsetMin = rrt.offsetMax = Vector2.zero;

            _cinematicVideoRawImage = rawObj.GetComponent<RawImage>();
            _videoPlayer = rawObj.GetComponent<VideoPlayer>();

            _videoRenderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
            _videoRenderTexture.Create();

            _videoPlayer.playOnAwake = false;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _videoRenderTexture;
            _videoPlayer.isLooping = true;
            _videoPlayer.url = videoPath;

            _cinematicVideoRawImage.texture = _videoRenderTexture;
            _cinematicVideoRawImage.color = Color.white;
            _cinematicVideoRawImage.raycastTarget = false;

            _slideRectTransform = rrt;
            _slideBaseAnchoredPos = rrt.anchoredPosition;

            _videoPlayer.errorReceived += (source, message) =>
            {
                // Fallback an toàn sang Digital Art nếu máy không đọc được codec
                if (_cinematicVideoRawImage != null) _cinematicVideoRawImage.gameObject.SetActive(false);
                if (cinematicSlideImage != null) cinematicSlideImage.gameObject.SetActive(true);
            };
        }
    }

    private void Update()
    {
        if (!_isInCinematic || _hasSkipped) return;

        // ── 1. Tính toán 3D Parallax Tilt theo tương tác chạm / chuột / con quay hồi chuyển ──
        Vector2 targetTilt = Vector2.zero;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            Vector2 norm = new Vector2((t.position.x / Screen.width) - 0.5f, (t.position.y / Screen.height) - 0.5f);
            targetTilt = new Vector2(-norm.x * 24f, -norm.y * 18f);

            if (t.phase == TouchPhase.Began)
            {
                TriggerTouchEffect(t.position);
            }
        }
        else
        {
            // Rê chuột trên PC
            Vector2 mouseNorm = new Vector2((Input.mousePosition.x / Screen.width) - 0.5f, (Input.mousePosition.y / Screen.height) - 0.5f);
            targetTilt = new Vector2(-mouseNorm.x * 20f, -mouseNorm.y * 15f);

            // Cảm biến gia tốc lắc máy điện thoại Android (Accelerometer)
            Vector3 accel = Input.acceleration;
            if (accel.sqrMagnitude > 0.01f)
            {
                targetTilt += new Vector2(-accel.x * 16f, -accel.y * 12f);
            }

            if (Input.GetMouseButtonDown(0))
            {
                TriggerTouchEffect(Input.mousePosition);
            }
        }

        // Làm mượt chuyển động nghiêng Parallax
        _currentTiltOffset = Vector2.Lerp(_currentTiltOffset, targetTilt, Time.unscaledDeltaTime * 6f);
        if (_slideRectTransform != null)
        {
            _slideRectTransform.anchoredPosition = _slideBaseAnchoredPos + _currentTiltOffset;
        }

        // ── 2. Hào quang nhịp thở huyền bí (Breathing Aura Glow) ──
        if (_breathingGlowImage != null)
        {
            float pulse = 0.12f + 0.10f * Mathf.Sin(Time.unscaledTime * 2.6f);
            Color c = _breathingGlowImage.color;
            c.a = pulse;
            _breathingGlowImage.color = c;
        }

        // ── 3. Dòng chữ tương tác nhấp nháy êm dịu ──
        if (_tapHintText != null)
        {
            float hintAlpha = 0.40f + 0.35f * Mathf.Sin(Time.unscaledTime * 3.2f);
            _tapHintText.alpha = hintAlpha;
            _tapHintText.text = LocalizationManager.IsVietnamese 
                ? "« Chạm vào màn hình để tương tác ma thuật • Bấm Bỏ qua để nạp game »" 
                : "« Touch screen to unleash aether • Tap Skip to proceed »";
        }
    }

    /// <summary>
    /// Hiệu ứng chạm ngón tay: Bùng sáng ma thuật, rung điện thoại và phát âm thanh Soulslike
    /// </summary>
    private void TriggerTouchEffect(Vector2 screenPos)
    {
        _advanceSlideRequested = true;
        SoundManager.EnsureInstance()?.PlayButtonClick();

#if UNITY_ANDROID && !UNITY_EDITOR
        try { Handheld.Vibrate(); } catch { }
#endif

        if (_breathingGlowImage != null)
        {
            StopCoroutine("FlashTouchAuraRoutine");
            StartCoroutine("FlashTouchAuraRoutine");
        }
    }

    private IEnumerator FlashTouchAuraRoutine()
    {
        if (_breathingGlowImage == null) yield break;
        float elapsed = 0f;
        while (elapsed < 0.28f)
        {
            elapsed += Time.unscaledDeltaTime;
            float factor = 1f - (elapsed / 0.28f);
            Color c = _breathingGlowImage.color;
            c.a = 0.45f * factor;
            _breathingGlowImage.color = c;
            yield return null;
        }
    }

    /// <summary>
    /// Khởi chạy chuỗi Intro Cinematic Động & Loading nạp game
    /// </summary>
    public void StartIntroSequence()
    {
        if (_introFlowRoutine != null) StopCoroutine(_introFlowRoutine);
        _introFlowRoutine = StartCoroutine(IntroFlowRoutine());
    }

    private IEnumerator IntroFlowRoutine()
    {
        _isInCinematic = true;
        _hasSkipped = false;
        SetupInteractiveLayers();

        if (cinematicPanel != null)
        {
            cinematicPanel.SetActive(true);
            if (cinematicCg != null) cinematicCg.alpha = 1f;
        }

        if (loadingProgressPanel != null)
        {
            loadingProgressPanel.SetActive(false);
            if (loadingProgressCg != null) loadingProgressCg.alpha = 0f;
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipClicked);
        }

        // Bắt đầu chạy video nếu có
        if (_videoPlayer != null && _cinematicVideoRawImage != null)
        {
            _cinematicVideoRawImage.gameObject.SetActive(true);
            if (cinematicSlideImage != null) cinematicSlideImage.gameObject.SetActive(false);
            _videoPlayer.Play();
        }

        // 3 Phân Cảnh Cốt Truyện dẫn dắt người chơi
        var slides = new[]
        {
            new {
                sprite = _spriteVoidPhantom,
                textVN = "Vương quốc cổ đại chìm sâu dưới tro tàn của đại lục Tàn Tích...",
                textEN = "An ancient kingdom lies submerged in the ashes of the Ruinbound realm...",
                duration = 3.6f
            },
            new {
                sprite = _spriteInfernoFiend != null ? _spriteInfernoFiend : _spriteVoidPhantom,
                textVN = "Lời nguyền Hắc Ám trỗi dậy, những sinh linh sa đọa gầm thét trong bóng tối...",
                textEN = "The Dark Curse stirs, fallen horrors howling from within the abyss...",
                duration = 3.6f
            },
            new {
                sprite = _spriteBladeSaint != null ? _spriteBladeSaint : _spriteVoidPhantom,
                textVN = "Chỉ có kẻ thức tỉnh Lõi Sức Mạnh mới có thể cứu rỗi vạn vật...",
                textEN = "Only the awakened wielders of Ancient Augments can pierce the oblivion...",
                duration = 3.8f
            }
        };

        bool isVi = LocalizationManager.IsVietnamese;

        for (int i = 0; i < slides.Length; i++)
        {
            if (_hasSkipped) break;

            var slide = slides[i];
            _advanceSlideRequested = false;

            // Nếu không dùng video hoặc video lỗi thì dùng tranh Digital Art chất lượng cao
            if ((_videoPlayer == null || !_videoPlayer.isPlaying) && cinematicSlideImage != null && slide.sprite != null)
            {
                cinematicSlideImage.gameObject.SetActive(true);
                cinematicSlideImage.sprite = slide.sprite;
                cinematicSlideImage.preserveAspect = true;
            }

            if (cinematicSubtitleText != null)
            {
                cinematicSubtitleText.text = isVi ? slide.textVN : slide.textEN;
                SoulslikeHUDStyler.SetSafeOutline(cinematicSubtitleText, 0.45f, new Color(0.01f, 0.01f, 0.02f, 0.98f));
            }

            // Hiệu ứng Ken Burns Zoom từ 1.0 -> 1.08 trong suốt slide
            float elapsed = 0f;
            float totalDur = slide.duration;

            while (elapsed < totalDur && !_hasSkipped)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / totalDur;

                if (_slideRectTransform != null)
                {
                    float scale = Mathf.Lerp(1.0f, 1.08f, progress);
                    _slideRectTransform.localScale = new Vector3(scale, scale, 1f);
                }

                if (_advanceSlideRequested && elapsed > 1.2f)
                {
                    break;
                }

                yield return null;
            }
        }

        if (_videoPlayer != null && _videoPlayer.isPlaying)
        {
            _videoPlayer.Stop();
        }

        _isInCinematic = false;

        // Chuyển cảnh mượt mà sang Màn Hình Loading
        float transitionTime = 0.5f;
        float t = 0f;
        if (loadingProgressPanel != null) loadingProgressPanel.SetActive(true);

        while (t < transitionTime)
        {
            t += Time.unscaledDeltaTime;
            float ratio = t / transitionTime;
            if (cinematicCg != null) cinematicCg.alpha = 1f - ratio;
            if (loadingProgressCg != null) loadingProgressCg.alpha = ratio;
            yield return null;
        }

        if (cinematicPanel != null) cinematicPanel.SetActive(false);
        if (loadingProgressCg != null) loadingProgressCg.alpha = 1f;

        // Giai đoạn 2: Thanh Tiến Trình Loading Nạp Game
        yield return StartCoroutine(RunProgressLoadingRoutine());
    }

    private IEnumerator RunProgressLoadingRoutine()
    {
        _isLoadingScene = true;
        bool isVi = LocalizationManager.IsVietnamese;

        string[] loadingTips = new string[]
        {
            isVi ? "Đang đánh thức linh hồn tàn tích..." : "Awakening ancient ruin spirits...",
            isVi ? "Đang chuẩn bị kho vũ khí & kỹ năng tướng..." : "Preparing arsenal and combat prowess...",
            isVi ? "Đang nạp địa đồ các cõi tai ương..." : "Loading calamity realms and dungeons...",
            isVi ? "Đang khởi hành vào vương quốc..." : "Entering the Ruinbound realm..."
        };

        AsyncOperation asyncOp = SceneManager.LoadSceneAsync("MainMenuScene");
        asyncOp.allowSceneActivation = false;

        float displayProgress = 0f;
        int tipIndex = 0;
        float tipTimer = 0f;

        // Cập nhật phong cách thanh loading sang màu Trắng Bạch Kim Phát Sáng chuẩn Dark Fantasy
        if (progressFillImage != null)
        {
            progressFillImage.color = new Color(0.98f, 0.99f, 1.0f, 1f);
            var glow = progressFillImage.GetComponent<Outline>();
            if (glow == null) glow = progressFillImage.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(0.85f, 0.95f, 1f, 0.80f);
            glow.effectDistance = new Vector2(1.5f, 1.5f);
        }

        while (displayProgress < 1f)
        {
            float targetProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.unscaledDeltaTime * 0.85f);

            if (progressSlider != null) progressSlider.value = displayProgress;
            if (progressPercentText != null)
            {
                progressPercentText.text = $"{(int)(displayProgress * 100f)}%";
                SoulslikeHUDStyler.SetSafeOutline(progressPercentText, 0.35f, Color.black);
            }

            tipTimer += Time.unscaledDeltaTime;
            if (tipTimer > 0.8f && tipIndex < loadingTips.Length - 1)
            {
                tipTimer = 0f;
                tipIndex++;
            }

            if (progressStatusText != null)
            {
                progressStatusText.text = loadingTips[Mathf.Clamp(tipIndex, 0, loadingTips.Length - 1)];
                SoulslikeHUDStyler.SetSafeOutline(progressStatusText, 0.30f, Color.black);
            }

            if (displayProgress >= 0.999f && asyncOp.progress >= 0.9f)
            {
                displayProgress = 1f;
                if (progressSlider != null) progressSlider.value = 1f;
                if (progressPercentText != null) progressPercentText.text = "100%";
                if (progressStatusText != null)
                {
                    progressStatusText.text = isVi ? "Khởi hành hoàn tất! Đang bước vào..." : "Ready! Entering realm...";
                }
                yield return new WaitForSecondsRealtime(0.35f);
                break;
            }

            yield return null;
        }

        asyncOp.allowSceneActivation = true;
    }

    public void OnSkipClicked()
    {
        if (_hasSkipped) return;
        _hasSkipped = true;
        _isInCinematic = false;
        if (_videoPlayer != null && _videoPlayer.isPlaying)
        {
            _videoPlayer.Stop();
        }
        SoundManager.EnsureInstance()?.PlayButtonClick();
    }

    private void OnDestroy()
    {
        if (_videoRenderTexture != null)
        {
            _videoRenderTexture.Release();
            Destroy(_videoRenderTexture);
            _videoRenderTexture = null;
        }
    }
}
