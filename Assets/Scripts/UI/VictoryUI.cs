using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class VictoryUI : MonoBehaviour
{
    public static VictoryUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI enemiesText;
    [SerializeField] private TextMeshProUGUI shardsText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button nextRealmButton;

    private float runStartTime;
    private int enemiesKilled = 0;
    private bool isEnded = false;

    void Awake()
    {
        Instance = this;

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // An ban dau
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (playAgainButton != null) playAgainButton.onClick.AddListener(RestartRun);
        if (nextRealmButton != null) nextRealmButton.onClick.AddListener(RestartRun);
    }

    void OnEnable()
    {
        Instance = this;
        BotWheelAI.OnEnemyDied += HandleEnemyDied;
    }

    void OnDisable()
    {
        BotWheelAI.OnEnemyDied -= HandleEnemyDied;
    }

    void Start()
    {
        runStartTime = Time.time;
    }

    private void HandleEnemyDied(BotWheelAI enemy)
    {
        enemiesKilled++;
    }

    public void ShowVictoryScreen()
    {
        if (isEnded) return;
        isEnded = true;

        float elapsed = Time.time - runStartTime;
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        string timeStr = string.Format("{0:00}:{1:00}", minutes, seconds);

        int totalShards = 0;
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            var inv = player.GetComponent<PlayerInventory>();
            if (inv != null) totalShards = inv.GetShards();
        }

        // Tinh rank: Diet het quai duoi 2 phut = S, duoi 3 phut = A, con lai = B
        string rank = "S RANK";
        if (elapsed > 180f) rank = "B RANK";
        else if (elapsed > 120f) rank = "A RANK";

        if (timeText != null) timeText.text = timeStr;
        if (enemiesText != null) enemiesText.text = $"{enemiesKilled} Slain";
        if (shardsText != null) shardsText.text = $"{totalShards} Shards";
        if (rankText != null) rankText.text = rank;

        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        yield return new WaitForSeconds(0.3f);

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        float t = 0;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.6f);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
