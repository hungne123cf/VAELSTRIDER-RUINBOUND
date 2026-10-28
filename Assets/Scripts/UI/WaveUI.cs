using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WaveUI : MonoBehaviour
{
    [Header("UI Text (Supports both TextMeshPro and Legacy Text)")]
    [SerializeField] private Text legacyWaveText;
    [SerializeField] private TextMeshProUGUI tmpWaveText;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = GetComponent<RectTransform>();

        if (legacyWaveText == null) legacyWaveText = GetComponentInChildren<Text>();
        if (tmpWaveText == null) tmpWaveText = GetComponentInChildren<TextMeshProUGUI>();

        canvasGroup.alpha = 0f;
        UpdateBannerPosition();

        // Tắt WaveBanner hoàn toàn theo yêu cầu người chơi trên tất cả các map
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        WaveSpawner.OnWaveStarted += DisplayWaveStart;
        WaveSpawner.OnWaveMessage += DisplayWaveMessage;
    }

    void OnDisable()
    {
        WaveSpawner.OnWaveStarted -= DisplayWaveStart;
        WaveSpawner.OnWaveMessage -= DisplayWaveMessage;
    }

    void Update()
    {
        if (canvasGroup != null && canvasGroup.alpha > 0.01f)
        {
            UpdateBannerPosition();
        }
    }

    private void UpdateBannerPosition()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // Kiểm tra xem Thanh Máu Trùm (Boss Health Bar) có đang hiển thị không
        bool isBossBarVisible = (BossHealthBarUI.Instance != null &&
                                 BossHealthBarUI.Instance.IsVisible &&
                                 BossHealthBarUI.Instance.gameObject.activeInHierarchy);

        // Nếu Trùm đang xuất hiện: Đặt Banner Wave ở -152f (nằm ngay dưới thanh máu Boss, tuyệt đối không đè)
        // Nếu không có Trùm: Đặt Banner ở -95f (dưới thanh thời gian MatchTimer)
        float targetY = isBossBarVisible ? -152f : -95f;
        if (Mathf.Abs(rectTransform.anchoredPosition.y - targetY) > 0.5f)
        {
            rectTransform.anchoredPosition = new Vector2(0f, targetY);
        }
    }

    private void DisplayWaveStart(int current, int total, string title)
    {
        // Đã tắt toàn bộ banner chữ vàng wave
    }

    private void DisplayWaveMessage(string message)
    {
        // Đã tắt toàn bộ banner chữ vàng wave
    }

    public void ShowBanner(string title, string subtitle, float duration = 2.5f)
    {
        // Đã tắt toàn bộ banner chữ vàng wave theo yêu cầu trên tất cả các map
    }

    public void ShowBanner(string content, float duration = 2.5f)
    {
        // Đã tắt toàn bộ banner chữ vàng wave theo yêu cầu trên tất cả các map
    }

    private IEnumerator BannerRoutine(float duration)
    {
        yield break;
    }

    public static WaveUI EnsureUIExists()
    {
        return GameAutoBootstrap.EnsureWaveUI();
    }
}
