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

        // QUAN TRỌNG: Luôn ẩn Cinematic Panel và Loading Panel khi Awake
        // Chỉ kích hoạt khi người chơi thực sự bấm "CHẠM ĐỂ BẮT ĐẦU" (Tap To Start)
        if (cinematicPanel != null) cinematicPanel.SetActive(false);
        if (loadingProgressPanel != null) loadingProgressPanel.SetActive(false);
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
        // 1. Tắt vĩnh viễn nút Bỏ Qua thô kệch và dòng chữ Subtitle đè lên tranh
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        if (cinematicSubtitleText != null)
        {
            cinematicSubtitleText.gameObject.SetActive(false);
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
        // Đặt vị trí tranh ở vùng trên (y: 17% -> 96%) để dành trọn vẹn 17% đáy cho thanh tiến trình
        if (cinematicSlideImage != null)
        {
            cinematicSlideImage.gameObject.SetActive(true);
            cinematicSlideImage.preserveAspect = true;
            cinematicSlideImage.color = Color.white; // Màu gốc nguyên bản 100%, không bị ám màu

            _slideRectTransform = cinematicSlideImage.GetComponent<RectTransform>();
            _slideRectTransform.anchorMin = new Vector2(0.12f, 0.17f);
            _slideRectTransform.anchorMax = new Vector2(0.88f, 0.96f);
            _slideRectTransform.offsetMin = _slideRectTransform.offsetMax = Vector2.zero;

            _slideAspectFitter = cinematicSlideImage.GetComponent<AspectRatioFitter>();
            if (_slideAspectFitter == null) _slideAspectFitter = cinematicSlideImage.gameObject.AddComponent<AspectRatioFitter>();
            _slideAspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            _slideAspectFitter.aspectRatio = 0.747f; // Chuẩn tỉ lệ 896x1200 của tranh gốc
        }

        // 4. Đồng bộ thanh tiến trình sang màn hình DUY NHẤT (Cinematic Panel) nằm ngay dưới tranh
        if (cinematicPanel != null)
        {
            if (progressSlider != null && progressSlider.transform.parent != cinematicPanel.transform)
            {
                progressSlider.transform.SetParent(cinematicPanel.transform, false);
            }
            if (progressStatusText != null && progressStatusText.transform.parent != cinematicPanel.transform)
            {
                progressStatusText.transform.SetParent(cinematicPanel.transform, false);
            }
            if (progressPercentText != null && progressPercentText.transform.parent != cinematicPanel.transform)
            {
                progressPercentText.transform.SetParent(cinematicPanel.transform, false);
            }
        }

        // Tắt panel loading riêng biệt nếu có (vì toàn bộ đã tích hợp trực tiếp ngay dưới tranh)
        if (loadingProgressPanel != null)
        {
            loadingProgressPanel.SetActive(false);
        }

        // 5. Định dạng Thanh Tiến Trình chuẩn Bộ Asset Soulslike:
        // - Khung ngoài: Boss_Bar_Frame.png mạ viền vàng hoàng kim Gothic
        // - Rãnh máng: Bar_Obsidian_Trough.png rãnh đá đen sâu thẳm
        // - Ruột fill: Soulslike_Button_Stone.png màu Trắng Bạch Kim phát sáng
        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(true);
            var sRt = progressSlider.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 0f);
            sRt.anchorMax = new Vector2(0.5f, 0f);
            sRt.pivot = new Vector2(0.5f, 0.5f);
            sRt.anchoredPosition = new Vector2(0f, 62f);
            sRt.sizeDelta = new Vector2(680f, 20f);

            // Rãnh đá Obsidian
            var sImg = progressSlider.GetComponent<Image>();
            if (sImg != null)
            {
                sImg.sprite = SoulslikeHUDStyler.GetObsidianTroughSprite();
                sImg.type = Image.Type.Sliced;
                sImg.color = new Color(0.06f, 0.07f, 0.10f, 0.96f);
            }

            // Khung viền ngoài Boss Bar Gothic hoàng kim
            Transform frameTr = progressSlider.transform.Find("BossFrameDecoration");
            Image frameImg = null;
            if (frameTr == null)
            {
                GameObject frameObj = new GameObject("BossFrameDecoration");
                frameObj.transform.SetParent(progressSlider.transform, false);
                frameObj.transform.SetAsFirstSibling();
                RectTransform fRt = frameObj.AddComponent<RectTransform>();
                fRt.anchorMin = Vector2.zero;
                fRt.anchorMax = Vector2.one;
                fRt.offsetMin = new Vector2(-10f, -5f);
                fRt.offsetMax = new Vector2(10f, 5f);
                frameImg = frameObj.AddComponent<Image>();
            }
            else
            {
                frameImg = frameTr.GetComponent<Image>();
            }

            if (frameImg != null)
            {
                frameImg.sprite = SoulslikeHUDStyler.GetBossBarFrameSprite();
                frameImg.type = Image.Type.Sliced;
                frameImg.color = new Color(1f, 0.95f, 0.85f, 1f);
                frameImg.raycastTarget = false;
            }
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

        // Status Text (Tip - Nằm ngay trên thanh slider, cách xa chân tranh)
        if (progressStatusText != null)
        {
            progressStatusText.gameObject.SetActive(true);
            progressStatusText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            progressStatusText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            progressStatusText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            progressStatusText.rectTransform.anchoredPosition = new Vector2(0f, 95f);
            progressStatusText.rectTransform.sizeDelta = new Vector2(800f, 28f);
            progressStatusText.fontSize = 15;
            progressStatusText.fontStyle = FontStyles.Bold;
            progressStatusText.alignment = TextAlignmentOptions.Center;
            progressStatusText.color = new Color(1f, 0.88f, 0.55f);
            SoulslikeHUDStyler.SetSafeOutline(progressStatusText, 0.35f, Color.black);
        }

        // Percent Text (Nằm ngay dưới thanh slider)
        if (progressPercentText != null)
        {
            progressPercentText.gameObject.SetActive(true);
            progressPercentText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            progressPercentText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            progressPercentText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            progressPercentText.rectTransform.anchoredPosition = new Vector2(0f, 32f);
            progressPercentText.rectTransform.sizeDelta = new Vector2(300f, 26f);
            progressPercentText.fontSize = 14;
            progressPercentText.fontStyle = FontStyles.Bold;
            progressPercentText.alignment = TextAlignmentOptions.Center;
            progressPercentText.characterSpacing = 2.0f;
            progressPercentText.color = new Color(0.96f, 0.98f, 1f);
            SoulslikeHUDStyler.SetSafeOutline(progressPercentText, 0.35f, Color.black);
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
        _isLoadingScene = true;
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

        // Tác phẩm Digital Art nguyên bản chuẩn phong cách Dark Fantasy TCG
        Sprite[] slides = new Sprite[]
        {
            _spriteVoidPhantom,
            _spriteInfernoFiend != null ? _spriteInfernoFiend : _spriteVoidPhantom,
            _spriteBladeSaint != null ? _spriteBladeSaint : _spriteVoidPhantom
        };

        int currentSlideIndex = 0;
        if (cinematicSlideImage != null && slides[0] != null)
        {
            cinematicSlideImage.sprite = slides[0];
            cinematicSlideImage.preserveAspect = true;
            if (_slideAspectFitter != null && slides[0].rect.height > 0)
            {
                _slideAspectFitter.aspectRatio = (float)slides[0].rect.width / slides[0].rect.height;
            }
        }

        // Tuyệt đối không hiển thị bất kỳ dòng chữ Subtitle nào đè lên chân tranh
        if (cinematicSubtitleText != null)
        {
            cinematicSubtitleText.gameObject.SetActive(false);
        }

        bool isVi = LocalizationManager.IsVietnamese;
        string[] loadingTips = new string[]
        {
            isVi ? "Đang đánh thức linh hồn tàn tích..." : "Awakening ancient ruin spirits...",
            isVi ? "Đang chuẩn bị kho vũ khí & kỹ năng tướng..." : "Preparing arsenal and combat prowess...",
            isVi ? "Đang nạp địa đồ các cõi tai ương..." : "Loading calamity realms and dungeons...",
            isVi ? "Đang khởi hành vào vương quốc..." : "Entering the Ruinbound realm..."
        };

        // BẮT ĐẦU NẠP BẤT ĐỒNG BỘ NGAY TRÊN MÀN HÌNH NÀY (1 Màn hình duy nhất chạy xuyên suốt)
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync("MainMenuScene");
        asyncOp.allowSceneActivation = false;

        float displayProgress = 0f;
        int tipIndex = 0;
        float tipTimer = 0f;
        float slideTimer = 0f;
        float slideDuration = 4.0f;

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

            // Chu kỳ đổi gợi ý tip
            tipTimer += Time.unscaledDeltaTime;
            if (tipTimer > 1.2f && tipIndex < loadingTips.Length - 1)
            {
                tipTimer = 0f;
                tipIndex++;
            }

            if (progressStatusText != null)
            {
                progressStatusText.text = loadingTips[Mathf.Clamp(tipIndex, 0, loadingTips.Length - 1)];
                SoulslikeHUDStyler.SetSafeOutline(progressStatusText, 0.30f, Color.black);
            }

            // Hiệu ứng "Tranh Động Sống Động" (Living Canvas): Nhịp thở điện ảnh chậm nhẹ (1.00 -> 1.025)
            if (_slideRectTransform != null)
            {
                float breath = 1.0f + 0.022f * Mathf.Sin(Time.unscaledTime * 1.6f);
                _slideRectTransform.localScale = new Vector3(breath, breath, 1f);
            }

            // Chuyển slide tranh nếu thời gian load kéo dài qua tranh tiếp theo
            slideTimer += Time.unscaledDeltaTime;
            if (slideTimer > slideDuration && slides.Length > 1)
            {
                slideTimer = 0f;
                currentSlideIndex = (currentSlideIndex + 1) % slides.Length;
                if (cinematicSlideImage != null && slides[currentSlideIndex] != null)
                {
                    cinematicSlideImage.sprite = slides[currentSlideIndex];
                    if (_slideAspectFitter != null && slides[currentSlideIndex].rect.height > 0)
                    {
                        _slideAspectFitter.aspectRatio = (float)slides[currentSlideIndex].rect.width / slides[currentSlideIndex].rect.height;
                    }
                }
            }

            // Hoàn tất nạp game
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

        // Kích hoạt Scene mới trong khi Canvas vẫn giữ nguyên 100% Opaque (Không bao giờ fade out trước để tránh lộ màn hình cũ)
        asyncOp.allowSceneActivation = true;

        while (SceneManager.GetActiveScene().name != "MainMenuScene" && !asyncOp.isDone)
        {
            yield return null;
        }
        yield return null;
        yield return new WaitForEndOfFrame();

        // Bây giờ MainMenuScene đã sẵn sàng ở phía sau -> Fade out nhẹ nhàng
        float fadeTime = 0.35f;
        float ft = 0f;
        while (ft < fadeTime)
        {
            ft += Time.unscaledDeltaTime;
            if (cinematicCg != null) cinematicCg.alpha = 1f - (ft / fadeTime);
            yield return null;
        }
    }
}
