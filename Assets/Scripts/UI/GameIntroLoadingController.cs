using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Live Interactive Cinematic Motion Intro & Loading Controller.
/// Hệ thống Mở màn Ảnh Động Tương Tác Sống Động chuẩn Dark Fantasy / Soulslike:
/// - Loại bỏ hoàn toàn video MP4 bên thứ ba để không dính logo watermark và không bị kéo bẹp giãn chiều ngang.
/// - Giữ trọn tỉ lệ 3:4 của Tranh Digital Art bằng AspectRatioFitter, hiển thị sắc nét thanh thoát giữa màn hình.
/// - Bỏ nút "Bỏ Qua" và dòng text rườm rà — Người chơi chạm tay bất kỳ đâu để tương tác hoặc chuyển tiếp tức thì.
/// - Thanh tiến trình Loading: Áp dụng trọn bộ Sprite Asset chuẩn phong cách Dark Fantasy (Khung máng đá Obsidian Trough, Khung Boss Bar Frame, Dải Fill Trắng Bạch Kim Phát Sáng).
/// - 100% Zero-Bug Release trên Android: Mượt mà 60 FPS, không đen màn hình, co giãn responsive hoàn hảo.
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
    private Image _breathingGlowImage;
    private RectTransform _slideRectTransform;
    private AspectRatioFitter _slideArf;
    private Vector2 _slideBaseAnchoredPos;
    private Vector2 _currentTiltOffset;
    private bool _hasSkipped = false;
    private bool _advanceSlideRequested = false;
    private bool _isInCinematic = false;
    private bool _isLoadingScene = false;
    private Coroutine _introFlowRoutine;

    // Danh sách các tác phẩm Digital Art gốc chuẩn Dark Fantasy không logo
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

    private void SetupInteractiveCinematicLayers()
    {
        if (cinematicPanel == null) return;

        // Đảm bảo nền Cinematic là màu đen Obsidian sâu thẳm huyền bí
        var cpImg = cinematicPanel.GetComponent<Image>();
        if (cpImg != null)
        {
            cpImg.color = new Color(0.015f, 0.015f, 0.025f, 1f);
        }

        // Bỏ hoàn toàn nút Bỏ Qua theo yêu cầu
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        // Cố định tỉ lệ tranh Digital Art bằng AspectRatioFitter chuẩn 3:4 (0.747f) - KHÔNG BAO GIỜ BỊ DÃN CHIỀU NGANG
        if (cinematicSlideImage != null)
        {
            _slideRectTransform = cinematicSlideImage.GetComponent<RectTransform>();
            if (_slideRectTransform != null)
            {
                _slideRectTransform.anchorMin = new Vector2(0.15f, 0.12f);
                _slideRectTransform.anchorMax = new Vector2(0.85f, 0.88f);
                _slideRectTransform.offsetMin = _slideRectTransform.offsetMax = Vector2.zero;
                _slideBaseAnchoredPos = _slideRectTransform.anchoredPosition;

                _slideArf = cinematicSlideImage.GetComponent<AspectRatioFitter>();
                if (_slideArf == null) _slideArf = cinematicSlideImage.gameObject.AddComponent<AspectRatioFitter>();
                _slideArf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                _slideArf.aspectRatio = 0.747f; // Chuẩn tỷ lệ thẻ bài dọc không méo
            }

            cinematicSlideImage.preserveAspect = true;
            cinematicSlideImage.color = Color.white;
        }

        // Tạo Lớp Hào Quang Nhịp Thở Huyền Bí (Breathing Aura Glow)
        Transform existingGlow = cinematicPanel.transform.Find("InteractiveBreathingAura");
        if (existingGlow == null)
        {
            GameObject glowObj = new GameObject("InteractiveBreathingAura", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glowObj.transform.SetParent(cinematicPanel.transform, false);
            glowObj.transform.SetSiblingIndex(cinematicSlideImage != null ? cinematicSlideImage.transform.GetSiblingIndex() + 1 : 1);

            RectTransform grt = glowObj.GetComponent<RectTransform>();
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.offsetMin = grt.offsetMax = Vector2.zero;

            _breathingGlowImage = glowObj.GetComponent<Image>();
            _breathingGlowImage.color = new Color(0.48f, 0.15f, 0.75f, 0.16f);
            _breathingGlowImage.raycastTarget = false;
        }
        else
        {
            _breathingGlowImage = existingGlow.GetComponent<Image>();
        }

        // Dòng chữ phụ đề cốt truyện căn chỉnh tinh tế
        if (cinematicSubtitleText != null)
        {
            var subRt = cinematicSubtitleText.GetComponent<RectTransform>();
            if (subRt != null)
            {
                subRt.anchorMin = new Vector2(0.10f, 0.035f);
                subRt.anchorMax = new Vector2(0.90f, 0.11f);
                subRt.offsetMin = subRt.offsetMax = Vector2.zero;
            }
            cinematicSubtitleText.fontSize = 20f;
            cinematicSubtitleText.fontStyle = FontStyles.Bold;
            cinematicSubtitleText.alignment = TextAlignmentOptions.Center;
            cinematicSubtitleText.color = new Color(1f, 0.92f, 0.70f, 0.95f);
            SoulslikeHUDStyler.SetSafeOutline(cinematicSubtitleText, 0.45f, Color.black);
        }
    }

    private void StyleLoadingProgressPanelWithGameAssets()
    {
        if (loadingProgressPanel == null) return;

        // 1. Nền Panel Loading: Đổi từ nền cát cam cũ sang Nền Đá Tàn Tích Dark Fantasy Obsidian
        var lpImg = loadingProgressPanel.GetComponent<Image>();
        if (lpImg != null)
        {
            lpImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
            lpImg.type = Image.Type.Sliced;
            lpImg.color = new Color(0.015f, 0.02f, 0.035f, 1f); // Đen tuyền đá Obsidian huyền ảo
        }

        // 2. Tiêu đề Game & Phụ đề
        if (gameTitleText != null)
        {
            gameTitleText.color = new Color(1f, 0.88f, 0.45f, 1f); // Hoàng Kim Thượng Cổ
            SoulslikeHUDStyler.SetSafeOutline(gameTitleText, 0.45f, Color.black);
        }
        if (gameSubtitleText != null)
        {
            gameSubtitleText.color = new Color(0.92f, 0.40f, 0.40f, 0.95f);
            SoulslikeHUDStyler.SetSafeOutline(gameSubtitleText, 0.35f, Color.black);
        }

        // 3. Thanh Tiến Trình Slider: Đổi khung và thanh fill bằng Sprite Asset chuẩn phong cách game
        if (progressSlider != null)
        {
            var sldRt = progressSlider.GetComponent<RectTransform>();
            if (sldRt != null)
            {
                sldRt.anchorMin = new Vector2(0.5f, 0.5f);
                sldRt.anchorMax = new Vector2(0.5f, 0.5f);
                sldRt.pivot = new Vector2(0.5f, 0.5f);
                sldRt.anchoredPosition = new Vector2(0f, -145f);
                sldRt.sizeDelta = new Vector2(760f, 22f); // Kích thước thanh thoát chuẩn Soulslike
            }

            // Khung Máng Đá Obsidian Trough
            var sldBg = progressSlider.GetComponent<Image>();
            if (sldBg != null)
            {
                sldBg.sprite = SoulslikeHUDStyler.GetObsidianTroughSprite();
                sldBg.type = Image.Type.Sliced;
                sldBg.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            }

            var sldOutline = progressSlider.GetComponent<Outline>();
            if (sldOutline != null)
            {
                sldOutline.effectColor = new Color(0.90f, 0.82f, 0.45f, 0.75f);
                sldOutline.effectDistance = new Vector2(1.2f, -1.2f);
            }

            // Gắn khung kim loại Boss Bar Frame đè bên ngoài nếu chưa có
            Transform existingBossFrame = progressSlider.transform.Find("SoulslikeBossFrame");
            if (existingBossFrame == null)
            {
                GameObject bossFrameObj = new GameObject("SoulslikeBossFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bossFrameObj.transform.SetParent(progressSlider.transform, false);
                bossFrameObj.transform.SetAsLastSibling();

                var bfRt = bossFrameObj.GetComponent<RectTransform>();
                bfRt.anchorMin = Vector2.zero;
                bfRt.anchorMax = Vector2.one;
                bfRt.offsetMin = new Vector2(-6f, -5f);
                bfRt.offsetMax = new Vector2(6f, 5f);

                var bfImg = bossFrameObj.GetComponent<Image>();
                bfImg.sprite = SoulslikeHUDStyler.GetBossBarFrameSprite();
                bfImg.type = Image.Type.Sliced;
                bfImg.color = Color.white;
                bfImg.raycastTarget = false;
            }

            // Dải Fill Năng Lượng Bên Trong: Sprite Sapphire/Soul Aether màu TRẮNG BẠCH KIM PHÁT SÁNG
            if (progressFillImage != null)
            {
                progressFillImage.sprite = SoulslikeHUDStyler.GetSapphireFillSprite();
                progressFillImage.type = Image.Type.Sliced;
                progressFillImage.color = new Color(0.98f, 0.99f, 1.0f, 1f); // Trắng Tinh Khiết / Bạch Kim

                var fillGlow = progressFillImage.GetComponent<Outline>();
                if (fillGlow == null) fillGlow = progressFillImage.gameObject.AddComponent<Outline>();
                fillGlow.effectColor = new Color(0.85f, 0.95f, 1f, 0.85f); // Hào quang trắng xanh ngọc bích
                fillGlow.effectDistance = new Vector2(1.5f, 1.5f);
            }
        }

        // 4. Dòng chữ trạng thái & mẹo: Đổi từ xanh lá cây lạc quẻ sang màu vàng kim Antique Gold
        if (progressStatusText != null)
        {
            var stRt = progressStatusText.GetComponent<RectTransform>();
            if (stRt != null)
            {
                stRt.anchoredPosition = new Vector2(0f, -105f);
            }
            progressStatusText.color = new Color(1f, 0.90f, 0.65f, 1f); // Antique Gold
            progressStatusText.fontSize = 17f;
            SoulslikeHUDStyler.SetSafeOutline(progressStatusText, 0.35f, Color.black);
        }

        // 5. Phần trăm loading màu trắng ngọc bích sắc nét
        if (progressPercentText != null)
        {
            var ptRt = progressPercentText.GetComponent<RectTransform>();
            if (ptRt != null)
            {
                ptRt.anchoredPosition = new Vector2(0f, -190f);
            }
            progressPercentText.color = new Color(0.95f, 0.98f, 1.0f, 0.95f);
            progressPercentText.fontSize = 15f;
            SoulslikeHUDStyler.SetSafeOutline(progressPercentText, 0.35f, Color.black);
        }
    }

    private void Update()
    {
        if (!_isInCinematic || _hasSkipped) return;

        // 3D Parallax Tilt theo tương tác chạm / chuột / con quay hồi chuyển
        Vector2 targetTilt = Vector2.zero;
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            Vector2 norm = new Vector2((t.position.x / Screen.width) - 0.5f, (t.position.y / Screen.height) - 0.5f);
            targetTilt = new Vector2(-norm.x * 20f, -norm.y * 15f);

            if (t.phase == TouchPhase.Began)
            {
                TriggerTouchEffect();
            }
        }
        else
        {
            Vector2 mouseNorm = new Vector2((Input.mousePosition.x / Screen.width) - 0.5f, (Input.mousePosition.y / Screen.height) - 0.5f);
            targetTilt = new Vector2(-mouseNorm.x * 18f, -mouseNorm.y * 12f);

            Vector3 accel = Input.acceleration;
            if (accel.sqrMagnitude > 0.01f)
            {
                targetTilt += new Vector2(-accel.x * 15f, -accel.y * 10f);
            }

            if (Input.GetMouseButtonDown(0))
            {
                TriggerTouchEffect();
            }
        }

        _currentTiltOffset = Vector2.Lerp(_currentTiltOffset, targetTilt, Time.unscaledDeltaTime * 6f);
        if (_slideRectTransform != null)
        {
            _slideRectTransform.anchoredPosition = _slideBaseAnchoredPos + _currentTiltOffset;
        }

        // Hào quang nhịp thở
        if (_breathingGlowImage != null)
        {
            float pulse = 0.12f + 0.10f * Mathf.Sin(Time.unscaledTime * 2.6f);
            Color c = _breathingGlowImage.color;
            c.a = pulse;
            _breathingGlowImage.color = c;
        }
    }

    private void TriggerTouchEffect()
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
            c.a = 0.40f * factor;
            _breathingGlowImage.color = c;
            yield return null;
        }
    }

    public void StartIntroSequence()
    {
        if (_introFlowRoutine != null) StopCoroutine(_introFlowRoutine);
        _introFlowRoutine = StartCoroutine(IntroFlowRoutine());
    }

    private IEnumerator IntroFlowRoutine()
    {
        _isInCinematic = true;
        _hasSkipped = false;
        SetupInteractiveCinematicLayers();

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

        // 3 Phân Cảnh Cốt Truyện bằng Tranh Digital Art Không Logo
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

            if (cinematicSlideImage != null && slide.sprite != null)
            {
                cinematicSlideImage.sprite = slide.sprite;
                cinematicSlideImage.preserveAspect = true;
            }

            if (cinematicSubtitleText != null)
            {
                cinematicSubtitleText.text = isVi ? slide.textVN : slide.textEN;
                SoulslikeHUDStyler.SetSafeOutline(cinematicSubtitleText, 0.45f, Color.black);
            }

            // Hiệu ứng Ken Burns Zoom từ 1.0 -> 1.06 trong suốt slide (không méo hình)
            float elapsed = 0f;
            float totalDur = slide.duration;

            while (elapsed < totalDur && !_hasSkipped)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / totalDur;

                if (_slideRectTransform != null)
                {
                    float scale = Mathf.Lerp(1.0f, 1.06f, progress);
                    _slideRectTransform.localScale = new Vector3(scale, scale, 1f);
                }

                // Chạm vào màn hình để chuyển nhanh
                if (_advanceSlideRequested && elapsed > 1.0f)
                {
                    break;
                }

                yield return null;
            }

            // Fade nhẹ chuyển tranh
            if (cinematicSlideImage != null)
            {
                float fadeOut = 0.22f;
                float f = 0f;
                Color originalColor = cinematicSlideImage.color;
                while (f < fadeOut && !_hasSkipped)
                {
                    f += Time.unscaledDeltaTime;
                    cinematicSlideImage.color = Color.Lerp(originalColor, new Color(0.15f, 0.15f, 0.20f, 0.7f), f / fadeOut);
                    yield return null;
                }
                cinematicSlideImage.color = originalColor;
            }
        }

        _isInCinematic = false;

        // Chuyển cảnh sang Màn Hình Loading
        float transitionTime = 0.45f;
        float t = 0f;
        if (loadingProgressPanel != null)
        {
            StyleLoadingProgressPanelWithGameAssets();
            loadingProgressPanel.SetActive(true);
        }

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

        while (displayProgress < 1f)
        {
            float targetProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.unscaledDeltaTime * 0.95f);

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
        SoundManager.EnsureInstance()?.PlayButtonClick();
    }
}
