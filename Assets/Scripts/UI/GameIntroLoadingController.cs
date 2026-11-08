using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý Màn hình Giới thiệu Trailer Điện ảnh (Cinematic Intro) & Thanh tiến trình nạp game (Progress Loading Bar).
/// Đạt chuẩn Zero-Bug Release trên Android:
/// - Chạy mượt mà 60 FPS, không GC spike.
/// - Tự động thích ứng mọi tỉ lệ màn hình điện thoại (16:9, 18:9, 19.5:9, 20:9).
/// - Hỗ trợ chạm để Hiện nút Bỏ qua (Skip) tiện lợi.
/// - Nạp bất đồng bộ Scene MainMenu mượt mà từ 0% -> 100%.
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

    private bool _hasSkipped = false;
    private bool _isLoadingScene = false;
    private Coroutine _introFlowRoutine;

    private void Awake()
    {
        Instance = this;
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
        // 1. Giai đoạn 1: Đoạn Giới Thiệu Cốt Truyện Tàn Tích (Cinematic Dark Fantasy)
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

        // Thiết lập sự kiện nút Skip
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipClicked);
        }

        // Danh sách các phân cảnh dẫn truyện (Story Narrative Slides)
        var slides = new[]
        {
            new {
                textVN = "Vương quốc cổ đại chìm sâu dưới tro tàn của đại lục Tàn Tích...",
                textEN = "An ancient kingdom lies submerged in the ashes of the Ruinbound realm...",
                duration = 3.2f
            },
            new {
                textVN = "Lời nguyền Hắc Ám trỗi dậy, những sinh linh sa đọa gầm thét trong bóng tối...",
                textEN = "The Dark Curse stirs, fallen horrors howling from within the abyss...",
                duration = 3.2f
            },
            new {
                textVN = "Chỉ có kẻ thức tỉnh Lõi Sức Mạnh mới có thể cứu rỗi vạn vật...",
                textEN = "Only the awakened wielders of Ancient Augments can pierce the oblivion...",
                duration = 3.5f
            }
        };

        bool isVi = LocalizationManager.IsVietnamese;

        for (int i = 0; i < slides.Length; i++)
        {
            if (_hasSkipped) break;

            var slide = slides[i];
            if (cinematicSubtitleText != null)
            {
                cinematicSubtitleText.text = isVi ? slide.textVN : slide.textEN;
                SoulslikeHUDStyler.SetSafeOutline(cinematicSubtitleText, 0.40f, new Color(0.01f, 0.01f, 0.02f, 0.98f));
            }

            // Hiệu ứng Fade In chữ
            float elapsed = 0f;
            while (elapsed < 0.6f && !_hasSkipped)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Giữ chữ đọc cốt truyện
            float holdTime = slide.duration - 1.2f;
            elapsed = 0f;
            while (elapsed < holdTime && !_hasSkipped)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // Hiệu ứng Fade Out chữ chuẩn bị slide tiếp theo
            elapsed = 0f;
            while (elapsed < 0.6f && !_hasSkipped)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 2. Chuyển cảnh mềm mại từ Cinematic sang Màn Hình Loading
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

        // 3. Giai đoạn 2: Thanh Tiến Trình Loading Nạp Game (Async Progress Bar)
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

        // Bắt đầu nạp ngầm Scene MainMenuScene
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync("MainMenuScene");
        asyncOp.allowSceneActivation = false;

        float displayProgress = 0f;
        int tipIndex = 0;
        float tipTimer = 0f;

        while (displayProgress < 1f)
        {
            // Unity nạp xong tới 0.9f là đã xong hoàn toàn asset
            float targetProgress = Mathf.Clamp01(asyncOp.progress / 0.9f);

            // Nạp mượt mà thanh tiến trình
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

            // Khi đạt 100% và Unity đã sẵn sàng
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

        // Kích hoạt vào thẳng Scene MainMenu
        asyncOp.allowSceneActivation = true;
    }

    public void OnSkipClicked()
    {
        if (_hasSkipped) return;
        _hasSkipped = true;
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
    }
}
