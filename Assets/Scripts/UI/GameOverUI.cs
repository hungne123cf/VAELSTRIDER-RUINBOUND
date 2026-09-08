using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button retryButton;

    private bool isShown = false;

    void Awake()
    {
        Instance = this;

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RestartGame);
        }
    }

    public static void Show()
    {
        if (Instance != null)
        {
            Instance.TriggerShow();
        }
        else
        {
            CreateRuntimeGameOverUI();
        }
    }

    public void TriggerShow()
    {
        if (isShown) return;
        isShown = true;
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        yield return new WaitForSeconds(0.6f);

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        float t = 0f;
        while (t < 0.7f)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.7f);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void CreateRuntimeGameOverUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("GameOverPanel");
        if (existing != null)
        {
            var go = existing.GetComponent<GameOverUI>();
            if (go != null) { go.TriggerShow(); return; }
        }

        GameObject panelObj = new GameObject("GameOverPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.02f, 0.02f, 0.92f); // Dark red death vignette

        CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
        var gameOverComp = panelObj.AddComponent<GameOverUI>();

        // Center Box
        GameObject boxObj = new GameObject("CenterBox");
        boxObj.transform.SetParent(panelObj.transform, false);
        RectTransform boxRect = boxObj.AddComponent<RectTransform>();
        boxRect.sizeDelta = new Vector2(480, 320);

        // Title: YOU DIED
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 70);
        titleRect.sizeDelta = new Vector2(450, 70);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "YOU DIED";
        titleTmp.fontSize = 54;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.95f, 0.15f, 0.15f);
        titleTmp.alignment = TextAlignmentOptions.Center;

        // Subtitle
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform subRect = subObj.AddComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, 15);
        subRect.sizeDelta = new Vector2(400, 40);
        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = "Your journey in the ruins has ended.";
        subTmp.fontSize = 17;
        subTmp.color = new Color(0.85f, 0.75f, 0.75f);
        subTmp.alignment = TextAlignmentOptions.Center;

        // Button: RESPAWN
        GameObject btnObj = new GameObject("Btn_Respawn");
        btnObj.transform.SetParent(boxObj.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -65);
        btnRect.sizeDelta = new Vector2(220, 56);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.7f, 0.18f, 0.18f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(gameOverComp.RestartGame);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        var btnTmp = txtObj.AddComponent<TextMeshProUGUI>();
        btnTmp.text = "RESPAWN";
        btnTmp.fontSize = 20;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.color = Color.white;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.raycastTarget = false;

        gameOverComp.retryButton = btn;
        gameOverComp.canvasGroup = cg;

        gameOverComp.TriggerShow();
    }
}
