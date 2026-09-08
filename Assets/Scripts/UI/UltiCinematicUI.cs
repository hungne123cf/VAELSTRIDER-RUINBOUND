using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UltiCinematicUI : MonoBehaviour
{
    public static UltiCinematicUI Instance { get; private set; }

    private GameObject letterboxTop;
    private GameObject letterboxBottom;
    private GameObject quoteContainer;
    private TextMeshProUGUI quoteText;
    private TextMeshProUGUI speakerText;
    private CanvasGroup canvasGroup;

    private readonly string[] battleQuotes = new string[]
    {
        "Bão Lốc Tàn Tích... Biến mất vào hư không!",
        "Ruin awakens... Disappear into the abyss!",
        "Lưỡi Kiếm Tàn Tích... Diệt Tuyệt!",
        "Face the storm of the Ruinbound!",
        "Ngươi không thể thoát khỏi bão lốc!"
    };

    private int quoteIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildCinematicUI();
    }

    private void BuildCinematicUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null) transform.SetParent(canvas.transform, false);
        }

        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // 1. Top Letterbox Bar
        letterboxTop = new GameObject("Letterbox_Top", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        letterboxTop.transform.SetParent(transform, false);
        RectTransform topRT = letterboxTop.GetComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0f, 1f);
        topRT.anchorMax = new Vector2(1f, 1f);
        topRT.pivot = new Vector2(0.5f, 1f);
        topRT.sizeDelta = new Vector2(0f, 110f);
        topRT.anchoredPosition = Vector2.zero;
        Image topImg = letterboxTop.GetComponent<Image>();
        topImg.color = new Color(0.02f, 0.02f, 0.04f, 0.96f);
        topImg.raycastTarget = false;

        // 2. Bottom Letterbox Bar
        letterboxBottom = new GameObject("Letterbox_Bottom", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        letterboxBottom.transform.SetParent(transform, false);
        RectTransform botRT = letterboxBottom.GetComponent<RectTransform>();
        botRT.anchorMin = new Vector2(0f, 0f);
        botRT.anchorMax = new Vector2(1f, 0f);
        botRT.pivot = new Vector2(0.5f, 0f);
        botRT.sizeDelta = new Vector2(0f, 110f);
        botRT.anchoredPosition = Vector2.zero;
        Image botImg = letterboxBottom.GetComponent<Image>();
        botImg.color = new Color(0.02f, 0.02f, 0.04f, 0.96f);
        botImg.raycastTarget = false;

        // 3. Quote Banner Container
        quoteContainer = new GameObject("Quote_Container", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        quoteContainer.transform.SetParent(transform, false);
        RectTransform qcRT = quoteContainer.GetComponent<RectTransform>();
        qcRT.anchorMin = new Vector2(0.5f, 0.22f);
        qcRT.anchorMax = new Vector2(0.5f, 0.22f);
        qcRT.pivot = new Vector2(0.5f, 0.5f);
        qcRT.sizeDelta = new Vector2(760f, 95f);
        qcRT.anchoredPosition = Vector2.zero;
        Image qcImg = quoteContainer.GetComponent<Image>();
        qcImg.color = new Color(0.04f, 0.03f, 0.08f, 0.88f);
        qcImg.raycastTarget = false;

        // Speaker Header
        GameObject speakerObj = new GameObject("SpeakerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        speakerObj.transform.SetParent(quoteContainer.transform, false);
        RectTransform spkRT = speakerObj.GetComponent<RectTransform>();
        spkRT.anchorMin = new Vector2(0f, 1f);
        spkRT.anchorMax = new Vector2(1f, 1f);
        spkRT.pivot = new Vector2(0.5f, 1f);
        spkRT.sizeDelta = new Vector2(-40f, 28f);
        spkRT.anchoredPosition = new Vector2(0f, -8f);
        speakerText = speakerObj.GetComponent<TextMeshProUGUI>();
        speakerText.text = "✦ VAELSTRIDER ✦";
        speakerText.fontSize = 18;
        speakerText.fontStyle = FontStyles.Bold;
        speakerText.alignment = TextAlignmentOptions.Center;
        speakerText.color = new Color(0.95f, 0.78f, 0.35f, 1f);
        speakerText.raycastTarget = false;

        // Quote Text
        GameObject quoteTextObj = new GameObject("QuoteText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        quoteTextObj.transform.SetParent(quoteContainer.transform, false);
        RectTransform txtRT = quoteTextObj.GetComponent<RectTransform>();
        txtRT.anchorMin = new Vector2(0f, 0f);
        txtRT.anchorMax = new Vector2(1f, 1f);
        txtRT.pivot = new Vector2(0.5f, 0.5f);
        txtRT.sizeDelta = new Vector2(-40f, -32f);
        txtRT.anchoredPosition = new Vector2(0f, -10f);
        quoteText = quoteTextObj.GetComponent<TextMeshProUGUI>();
        quoteText.text = battleQuotes[0];
        quoteText.fontSize = 24;
        quoteText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        quoteText.alignment = TextAlignmentOptions.Center;
        quoteText.color = new Color(0.85f, 0.95f, 1f, 1f);
        quoteText.raycastTarget = false;
    }

    public void ShowCutscene(float durationRealtime)
    {
        StopAllCoroutines();
        StartCoroutine(CutsceneRoutine(durationRealtime));
    }

    private IEnumerator CutsceneRoutine(float durationRealtime)
    {
        if (quoteText != null)
        {
            quoteText.text = "“" + battleQuotes[quoteIndex % battleQuotes.Length] + "”";
            quoteIndex++;
        }

        // Phat giong noi cau thoai ngau cua Player
        SoundManager.EnsureInstance()?.PlayUltiVoice();

        if (quoteContainer != null)
        {
            quoteContainer.transform.localScale = Vector3.one * 1.15f;
        }

        float elapsed = 0f;
        float fadeInTime = 0.12f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInTime);
            if (canvasGroup != null) canvasGroup.alpha = t;
            if (quoteContainer != null) quoteContainer.transform.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, t);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (quoteContainer != null) quoteContainer.transform.localScale = Vector3.one;

        float holdTime = Mathf.Max(0.1f, durationRealtime - fadeInTime - 0.15f);
        yield return new WaitForSecondsRealtime(holdTime);

        elapsed = 0f;
        float fadeOutTime = 0.15f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutTime);
            if (canvasGroup != null) canvasGroup.alpha = 1f - t;
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public static UltiCinematicUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("UltiCinematicUI");
        if (existing != null)
        {
            Instance = existing.GetComponent<UltiCinematicUI>();
            if (Instance != null) return Instance;
        }

        GameObject go = new GameObject("UltiCinematicUI");
        go.transform.SetParent(canvas.transform, false);
        Instance = go.AddComponent<UltiCinematicUI>();
        return Instance;
    }
}
