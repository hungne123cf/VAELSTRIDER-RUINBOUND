using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }

    [Header("UI References")]
    private CanvasGroup canvasGroup;
    private RectTransform panelRt;
    private TextMeshProUGUI bossNameText;
    private Slider healthSlider;
    private Image healthFillImage;
    private Slider ghostSlider;
    private TextMeshProUGUI phaseText;
    private TextMeshProUGUI hpPercentText;

    private Color[] phaseColors = new Color[]
    {
        new Color(0.95f, 0.22f, 0.22f, 1f), // Phase 1 (Core/Cuối): Đỏ rực
        new Color(0.75f, 0.35f, 0.95f, 1f), // Phase 2: Tím hư không
        new Color(0.22f, 0.75f, 0.95f, 1f)  // Phase 3 (Mở đầu): Xanh ngọc
    };

    private BotWheelAI currentBoss;
    private Coroutine ghostRoutine;
    private bool isVisible = false;

    void Awake()
    {
        Instance = this;
    }

    public static BossHealthBarUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cObj = new GameObject("BossCanvas");
            canvas = cObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9000;
            var scaler = cObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            cObj.AddComponent<GraphicRaycaster>();
        }

        Transform existing = canvas.transform.Find("BossHealthBarPanel");
        if (existing != null)
        {
            Instance = existing.GetComponent<BossHealthBarUI>();
            return Instance;
        }

        GameObject root = new GameObject("BossHealthBarPanel");
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 1f);
        rootRt.anchorMax = new Vector2(0.5f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = new Vector2(0f, -25f);
        rootRt.sizeDelta = new Vector2(780f, 95f);

        CanvasGroup cg = root.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        var ui = root.AddComponent<BossHealthBarUI>();
        ui.canvasGroup = cg;
        ui.panelRt = rootRt;

        // 1. Boss Name Text
        GameObject nameObj = new GameObject("BossNameText");
        nameObj.transform.SetParent(root.transform, false);
        RectTransform nameRt = nameObj.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.5f, 1f);
        nameRt.anchorMax = new Vector2(0.5f, 1f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = new Vector2(0f, 0f);
        nameRt.sizeDelta = new Vector2(760f, 32f);

        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "✦ TITAN CRUSHER • CỖ MÁY TÀN TÍCH ✦";
        nameTmp.fontSize = 20;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = new Color(1f, 0.88f, 0.45f);
        nameTmp.alignment = TextAlignmentOptions.Center;
        ui.bossNameText = nameTmp;

        // 2. Health Bar Background Frame
        GameObject frameObj = new GameObject("FrameBg");
        frameObj.transform.SetParent(root.transform, false);
        RectTransform frameRt = frameObj.AddComponent<RectTransform>();
        frameRt.anchorMin = new Vector2(0.5f, 0f);
        frameRt.anchorMax = new Vector2(0.5f, 0f);
        frameRt.pivot = new Vector2(0.5f, 0f);
        frameRt.anchoredPosition = new Vector2(0f, 26f);
        frameRt.sizeDelta = new Vector2(740f, 26f);

        Image frameImg = frameObj.AddComponent<Image>();
        frameImg.color = new Color(0.06f, 0.07f, 0.10f, 0.95f);

        // 3. Ghost Slider (Vết máu tụt chậm)
        GameObject ghostObj = new GameObject("GhostSlider");
        ghostObj.transform.SetParent(frameObj.transform, false);
        RectTransform ghostRt = ghostObj.AddComponent<RectTransform>();
        ghostRt.anchorMin = Vector2.zero;
        ghostRt.anchorMax = Vector2.one;
        ghostRt.sizeDelta = Vector2.zero;

        Slider gSlider = ghostObj.AddComponent<Slider>();
        gSlider.minValue = 0f;
        gSlider.maxValue = 1f;
        gSlider.value = 1f;

        GameObject gFill = new GameObject("Fill");
        gFill.transform.SetParent(ghostObj.transform, false);
        RectTransform gfRt = gFill.AddComponent<RectTransform>();
        gfRt.anchorMin = Vector2.zero;
        gfRt.anchorMax = Vector2.one;
        gfRt.sizeDelta = Vector2.zero;
        Image gfImg = gFill.AddComponent<Image>();
        gfImg.color = new Color(1f, 0.85f, 0.35f, 0.75f);
        gSlider.fillRect = gfRt;
        ui.ghostSlider = gSlider;

        // 4. Main Health Slider
        GameObject hpObj = new GameObject("HealthSlider");
        hpObj.transform.SetParent(frameObj.transform, false);
        RectTransform hpRt = hpObj.AddComponent<RectTransform>();
        hpRt.anchorMin = Vector2.zero;
        hpRt.anchorMax = Vector2.one;
        hpRt.sizeDelta = Vector2.zero;

        Slider hpSlider = hpObj.AddComponent<Slider>();
        hpSlider.minValue = 0f;
        hpSlider.maxValue = 1f;
        hpSlider.value = 1f;

        GameObject hpFill = new GameObject("Fill");
        hpFill.transform.SetParent(hpObj.transform, false);
        RectTransform hpfRt = hpFill.AddComponent<RectTransform>();
        hpfRt.anchorMin = Vector2.zero;
        hpfRt.anchorMax = Vector2.one;
        hpfRt.sizeDelta = Vector2.zero;
        Image hpfImg = hpFill.AddComponent<Image>();
        hpfImg.color = ui.phaseColors[2];
        hpSlider.fillRect = hpfRt;
        ui.healthSlider = hpSlider;
        ui.healthFillImage = hpfImg;

        // 5. HP Percent Text
        GameObject hpTxtObj = new GameObject("HpPercent");
        hpTxtObj.transform.SetParent(frameObj.transform, false);
        RectTransform hpTxtRt = hpTxtObj.AddComponent<RectTransform>();
        hpTxtRt.anchorMin = Vector2.zero;
        hpTxtRt.anchorMax = Vector2.one;
        hpTxtRt.sizeDelta = Vector2.zero;
        var hpTmp = hpTxtObj.AddComponent<TextMeshProUGUI>();
        hpTmp.text = "100%";
        hpTmp.fontSize = 14;
        hpTmp.fontStyle = FontStyles.Bold;
        hpTmp.color = Color.white;
        hpTmp.alignment = TextAlignmentOptions.Center;
        ui.hpPercentText = hpTmp;

        // 6. Phase Indicators
        GameObject phaseObj = new GameObject("PhaseIndicator");
        phaseObj.transform.SetParent(root.transform, false);
        RectTransform phaseRt = phaseObj.AddComponent<RectTransform>();
        phaseRt.anchorMin = new Vector2(0.5f, 0f);
        phaseRt.anchorMax = new Vector2(0.5f, 0f);
        phaseRt.pivot = new Vector2(0.5f, 0f);
        phaseRt.anchoredPosition = new Vector2(0f, 2f);
        phaseRt.sizeDelta = new Vector2(740f, 20f);

        var pTmp = phaseObj.AddComponent<TextMeshProUGUI>();
        pTmp.text = "◆ ◆ ◆ [ 3 THANH MÁU • PHASE I ]";
        pTmp.fontSize = 13;
        pTmp.fontStyle = FontStyles.Bold;
        pTmp.color = new Color(0.5f, 0.85f, 1f);
        pTmp.alignment = TextAlignmentOptions.Center;
        ui.phaseText = pTmp;

        Instance = ui;
        return ui;
    }

    public void ShowBoss(BotWheelAI boss, string name)
    {
        currentBoss = boss;
        if (bossNameText != null) bossNameText.text = name;

        if (healthSlider != null) healthSlider.value = 1f;
        if (ghostSlider != null) ghostSlider.value = 1f;
        UpdatePhaseDisplay(3);

        StopAllCoroutines();
        StartCoroutine(FadeInRoutine());
    }

    public void UpdateHealth(int currentHp, int maxHp, int phase)
    {
        if (maxHp <= 0) return;
        float norm = Mathf.Clamp01((float)currentHp / maxHp);

        if (healthSlider != null) healthSlider.value = norm;
        if (hpPercentText != null) hpPercentText.text = $"{Mathf.CeilToInt(norm * 100f)}%";

        UpdatePhaseDisplay(phase);

        if (ghostRoutine != null) StopCoroutine(ghostRoutine);
        ghostRoutine = StartCoroutine(GhostFillRoutine(norm));
    }

    public void UpdatePhaseDisplay(int phase)
    {
        int idx = Mathf.Clamp(phase - 1, 0, 2);
        if (healthFillImage != null)
        {
            healthFillImage.color = phaseColors[idx];
        }

        if (phaseText != null)
        {
            if (phase == 3)
            {
                phaseText.text = "<color=#38BDF8>◆ ◆ ◆</color>  [ GIAI ĐOẠN 1: MÁY XAY CỔ ĐẠI ]";
                phaseText.color = new Color(0.4f, 0.8f, 1f);
            }
            else if (phase == 2)
            {
                phaseText.text = "<color=#C084FC>◆ ◆ ◇</color>  [ GIAI ĐOẠN 2: HƯ KHÔNG THỨC TỈNH ]";
                phaseText.color = new Color(0.85f, 0.45f, 1f);
            }
            else
            {
                phaseText.text = "<color=#EF4444>◆ ◇ ◇</color>  [ GIAI ĐOẠN 3: CUỒNG NỘ HUYẾT NGUYỆT ]";
                phaseText.color = new Color(1f, 0.25f, 0.25f);
            }
        }
    }

    public void HideBoss()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        isVisible = true;
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.4f);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutRoutine()
    {
        isVisible = false;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.6f);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    private IEnumerator GhostFillRoutine(float targetVal)
    {
        yield return new WaitForSecondsRealtime(0.2f);
        if (ghostSlider == null) yield break;

        float startVal = ghostSlider.value;
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            ghostSlider.value = Mathf.Lerp(startVal, targetVal, t / 0.35f);
            yield return null;
        }
        ghostSlider.value = targetVal;
    }
}
