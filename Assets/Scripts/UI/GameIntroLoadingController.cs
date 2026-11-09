using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Clean High-End Dark Fantasy Intro & Loading Controller.
/// Thiết kế tối giản, sang trọng chuẩn mỹ thuật Dark Fantasy / Soulslike:
/// - GIỮ TRỌN VẸN NGUYÊN VỊ TRANH GỐC: Loại bỏ hoàn toàn lớp phủ màu tím mờ đục và video watermark AI.
/// - CHUẨN TỈ LỆ TRANH GỐC: Sử dụng AspectRatioFitter FitInParent (tỉ lệ 3:4 chuẩn 896x1200), không bị kéo giãn phồng chiều ngang.
/// - LOẠI BỎ NÚT BỎ QUA THÔ KỆCH: Người chơi chỉ cần chạm nhẹ vào màn hình là tự động chuyển tiếp mượt mà.
/// - THANH TIẾN TRÌNH CHUẨN ASSET GAME: Khung rãnh đá Runic Plaque đen obsidian và thanh Fill rèn đá màu trắng sáng tinh tế.
/// - NỀN ĐÁ TÀN TÍCH THUẦN TÚY: Loại bỏ nền cát pixel thô ráp, thay bằng màn đêm phế tích sâu thẳm.
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

    private RectTransform _slideRectTransform;
    private AspectRatioFitter _slideAspectFitter;
    private bool _hasSkipped = false;
    private bool _advanceSlideRequested = false;
    private bool _isInCinematic = false;
    private bool _isLoadingScene = false;
    private Coroutine _introFlowRoutine;

    // Danh sách 3 tác phẩm Digital Art độc bản chuẩn 3:4
    private Sprite _spriteVoidPhantom;
    private Sprite _spriteInfernoFiend;
    private Sprite _spriteBladeSaint;

    private void Awake()
    {
        Instance = this;
        LoadDeityArtwork();
        FormatCleanVisuals();
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

    /// <summary>
    /// Định dạng mỹ thuật tinh gọn: Giữ nguyên vẹn vẻ đẹp tranh vẽ gốc, dẹp bỏ logo, lớp phủ tím và nút thô
    /// </summary>
    private void FormatCleanVisuals()
    {
        // 1. Tắt vĩnh viễn nút Bỏ Qua thô kệch
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        // 2. Xóa sạch lớp phủ màu tím mờ đục và bề mặt video nếu còn tồn tại
        if (cinematicPanel != null)
        {
            Transform glow = cinematicPanel.transform.Find("InteractiveBreathingAura");
            if (glow != null) Destroy(glow.gameObject);

            Transform raw = cinematicPanel.transform.Find("CinematicVideoSurface");
            if (raw != null) Destroy(raw.gameObject);

            Transform hint = cinematicPanel.transform.Find("InteractiveTapHint");
            if (hint != null) Destroy(hint.gameObject);

            // Nền Cinematic Panel là màu đen tuyền thuần túy sâu thẳm
            var cpImg = cinematicPanel.GetComponent<Image>();
            if (cpImg != null)
            {
                cpImg.sprite = null;
                cpImg.color = new Color(0.015f, 0.015f, 0.025f, 1f);
            }
        }

        // 3. Khóa chuẩn tỉ lệ dọc 3:4 cho tranh vẽ, triệt tiêu 100% hiện tượng phình ngang
        if (cinematicSlideImage != null)
        {
            cinematicSlideImage.gameObject.SetActive(true);
            cinematicSlideImage.preserveAspect = true;
            cinematicSlideImage.color = Color.white; // Màu gốc nguyên bản, không ám màu

            _slideRectTransform = cinematicSlideImage.GetComponent<RectTransform>();
            _slideRectTransform.anchorMin = new Vector2(0.15f, 0.12f);
            _slideRectTransform.anchorMax = new Vector2(0.85f, 0.90f);
            _slideRectTransform.offsetMin = _slideRectTransform.offsetMax = Vector2.zero;

            _slideAspectFitter = cinematicSlideImage.GetComponent<AspectRatioFitter>();
            if (_slideAspectFitter == null) _slideAspectFitter = cinematicSlideImage.gameObject.AddComponent<AspectRatioFitter>();
            _slideAspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            _slideAspectFitter.aspectRatio = 0.747f; // Chuẩn tỉ lệ 896x1200 của tranh gốc
        }

        // 4. Định dạng lại Màn Hình Loading Progress Panel theo đúng Asset Soulslike
        if (loadingProgressPanel != null)
        {
            // Bỏ nền cát pixel nâu vỡ hạt, dùng nền đen đá tàn tích Dark Fantasy
            var lpImg = loadingProgressPanel.GetComponent<Image>();
            if (lpImg != null)
            {
                lpImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
                lpImg.type = Image.Type.Sliced;
                lpImg.color = new Color(0.02f, 0.02f, 0.035f, 1f);
            }

            // Định dạng lại thanh Slider theo Asset Game
            if (progressSlider != null)
            {
                var sRt = progressSlider.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.5f, 0.5f);
                sRt.anchorMax = new Vector2(0.5f, 0.5f);
                sRt.pivot = new Vector2(0.5f, 0.5f);
                sRt.anchoredPosition = new Vector2(0f, -145f);
                sRt.sizeDelta = new Vector2(800f, 18f); // Thanh mảnh, tinh tế

                // Rãnh đá Runic Plaque
                var sImg = progressSlider.GetComponent<Image>();
                if (sImg != null)
                {
                    sImg.sprite = SoulslikeHUDStyler.GetRunicPlaqueSprite();
                    sImg.type = Image.Type.Sliced;
                    sImg.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);
                }

                var sOutline = progressSlider.GetComponent<Outline>();
                if (sOutline == null) sOutline = progressSlider.gameObject.AddComponent<Outline>();
                sOutline.effectColor = new Color(0.85f, 0.75f, 0.35f, 0.75f); // Viền vàng kim tinh tế
                sOutline.effectDistance = new Vector2(1.2f, -1.2f);
            }

            // Thanh Fill bên trong: Trắng Bạch Kim Phát Sáng theo Asset đá rèn
            if (progressFillImage != null)
            {
                progressFillImage.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
                progressFillImage.type = Image.Type.Sliced;
                progressFillImage.color = new Color(0.98f, 0.99f, 1.0f, 1f); // Trắng sáng tinh khiết

                var fOutline = progressFillImage.GetComponent<Outline>();
                if (fOutline == null) fOutline = progressFillImage.gameObject.AddComponent<Outline>();
                fOutline.effectColor = new Color(0.85f, 0.95f, 1f, 0.85f);
                fOutline.effectDistance = new Vector2(1.5f, 1.5f);
            }

            // Status Text (Tip)
            if (progressStatusText != null)
            {
                progressStatusText.rectTransform.anchoredPosition = new Vector2(0f, -105f);
                progressStatusText.fontSize = 16;
                progressStatusText.fontStyle = FontStyles.Bold;
                progressStatusText.color = new Color(1f, 0.88f, 0.55f);
                SoulslikeHUDStyler.SetSafeOutline(progressStatusText, 0.35f, Color.black);
            }

            // Percent Text
            if (progressPercentText != null)
            {
                progressPercentText.rectTransform.anchoredPosition = new Vector2(0f, -185f);
                progressPercentText.fontSize = 15;
                progressPercentText.fontStyle = FontStyles.Bold;
                progressPercentText.characterSpacing = 2.0f;
                progressPercentText.color = new Color(0.96f, 0.98f, 1f);
                SoulslikeHUDStyler.SetSafeOutline(progressPercentText, 0.35f, Color.black);
            }
        }
    }

    private void Update()
    {
        if (!_isInCinematic || _hasSkipped) return;

        // Chạm ngón tay hoặc click chuột để lướt nhanh sang phân cảnh tiếp theo
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            _advanceSlideRequested = true;
            SoundManager.EnsureInstance()?.PlayButtonClick();
        }
    }

    /// <summary>
    /// Khởi chạy chuỗi Intro Cinematic & Loading nạp game
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
        FormatCleanVisuals();

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

        // 3 Phân Cảnh Cốt Truyện sử dụng trực tiếp tranh Digital Art Dark Fantasy nguyên bản
        var slides = new[]
        {
            new {
                sprite = _spriteVoidPhantom,
                textVN = "Vương quốc cổ đại chìm sâu dưới tro tàn của đại lục Tàn Tích...",
                textEN = "An ancient kingdom lies submerged in the ashes of the Ruinbound realm...",
                duration = 3.5f
            },
            new {
                sprite = _spriteInfernoFiend != null ? _spriteInfernoFiend : _spriteVoidPhantom,
                textVN = "Lời nguyền Hắc Ám trỗi dậy, những sinh linh sa đọa gầm thét trong bóng tối...",
                textEN = "The Dark Curse stirs, fallen horrors howling from within the abyss...",
                duration = 3.5f
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
                if (_slideAspectFitter != null && slide.sprite.rect.height > 0)
                {
                    _slideAspectFitter.aspectRatio = (float)slide.sprite.rect.width / slide.sprite.rect.height;
                }
            }

            if (cinematicSubtitleText != null)
            {
                cinematicSubtitleText.text = isVi ? slide.textVN : slide.textEN;
                SoulslikeHUDStyler.SetSafeOutline(cinematicSubtitleText, 0.45f, new Color(0.01f, 0.01f, 0.02f, 0.98f));
            }

            // Chuyển động Zoom điện ảnh chậm nhẹ (từ 1.0 -> 1.04) giữ trọn độ nét
            float elapsed = 0f;
            float totalDur = slide.duration;

            while (elapsed < totalDur && !_hasSkipped)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / totalDur;

                if (_slideRectTransform != null)
                {
                    float scale = Mathf.Lerp(1.0f, 1.04f, progress);
                    _slideRectTransform.localScale = new Vector3(scale, scale, 1f);
                }

                if (_advanceSlideRequested && elapsed > 0.8f)
                {
                    break;
                }

                yield return null;
            }

            // Fade nhẹ nhàng chuyển tranh
            if (cinematicSlideImage != null)
            {
                float fadeOut = 0.20f;
                float f = 0f;
                Color originalColor = cinematicSlideImage.color;
                while (f < fadeOut && !_hasSkipped)
                {
                    f += Time.unscaledDeltaTime;
                    cinematicSlideImage.color = Color.Lerp(originalColor, new Color(0.1f, 0.1f, 0.12f, 0.7f), f / fadeOut);
                    yield return null;
                }
                cinematicSlideImage.color = originalColor;
            }
        }

        _isInCinematic = false;

        // Chuyển cảnh mềm mại sang Màn Hình Loading
        float transitionTime = 0.4f;
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
                yield return new WaitForSecondsRealtime(0.25f);
                break;
            }

            yield return null;
        }

        asyncOp.allowSceneActivation = true;
    }
}
