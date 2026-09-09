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

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (legacyWaveText == null) legacyWaveText = GetComponentInChildren<Text>();
        if (tmpWaveText == null) tmpWaveText = GetComponentInChildren<TextMeshProUGUI>();

        canvasGroup.alpha = 0f;
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

    private void DisplayWaveStart(int current, int total, string title)
    {
        ShowBanner(title, 2.5f);
    }

    private void DisplayWaveMessage(string message)
    {
        ShowBanner(message, 2.5f);
    }

    
    public void ShowBanner(string title, string subtitle, float duration = 2.5f)
    {
        string full = string.IsNullOrEmpty(subtitle) ? title : $"{title}\n<size=70%>{subtitle}</size>";
        ShowBanner(full, duration);
    }

    public void ShowBanner(string content, float duration = 2.5f)
    {
        if (legacyWaveText != null) legacyWaveText.text = content;
        if (tmpWaveText != null) tmpWaveText.text = content;

        StopAllCoroutines();
        StartCoroutine(BannerRoutine(duration));
    }

    private IEnumerator BannerRoutine(float duration)
    {
        // Fade in
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.3f);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(duration);

        // Fade out
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.4f);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    public static WaveUI EnsureUIExists()
    {
        return GameAutoBootstrap.EnsureWaveUI();
    }
}
