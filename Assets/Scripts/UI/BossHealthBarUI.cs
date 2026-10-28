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
        new Color(0.95f, 0.22f, 0.22f, 1f), // Phase 1: Do ruc
        new Color(0.75f, 0.35f, 0.95f, 1f), // Phase 2: Tim ma thuat
        new Color(0.22f, 0.75f, 0.95f, 1f)  // Phase 3: Xanh ngoc
    };

    private BotWheelAI currentBoss;
    private Coroutine ghostRoutine;
    private bool isVisible = false;
    private int currentPhase = 3;

    public bool IsVisible => isVisible;

    void Awake()
    {
        Instance = this;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (panelRt == null) panelRt = GetComponent<RectTransform>();
        if (panelRt != null) panelRt.anchoredPosition = new Vector2(0f, -44f);
        SoulslikeHUDStyler.StyleBossHealthBar(GetComponentInParent<Canvas>());
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        UpdatePhaseDisplay(currentPhase);
    }

    public static BossHealthBarUI EnsureInstance()
    {
        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null || !canvas.gameObject.activeInHierarchy)
        {
            GameObject cObj = GameObject.Find("BossCanvas");
            if (cObj != null && cObj.GetComponent<Canvas>() != null && cObj.activeInHierarchy)
            {
                canvas = cObj.GetComponent<Canvas>();
            }
            else
            {
                if (cObj == null) cObj = new GameObject("BossCanvas");
                cObj.SetActive(true);
                canvas = cObj.GetComponent<Canvas>() ?? cObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9000;
                var scaler = cObj.GetComponent<CanvasScaler>() ?? cObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                if (cObj.GetComponent<GraphicRaycaster>() == null) cObj.AddComponent<GraphicRaycaster>();
            }
        }

        if (Instance != null)
        {
            if (!Instance.gameObject.activeSelf) Instance.gameObject.SetActive(true);
            if (!Instance.gameObject.activeInHierarchy && canvas != null)
            {
                Instance.transform.SetParent(canvas.transform, false);
                Instance.gameObject.SetActive(true);
            }
            return Instance;
        }

        BossHealthBarUI existingUI = Object.FindAnyObjectByType<BossHealthBarUI>(FindObjectsInactive.Include);
        if (existingUI != null)
        {
            existingUI.gameObject.SetActive(true);
            if (!existingUI.gameObject.activeInHierarchy && canvas != null)
            {
                existingUI.transform.SetParent(canvas.transform, false);
                existingUI.gameObject.SetActive(true);
            }
            Instance = existingUI;
            return Instance;
        }

        Transform existing = canvas.transform.Find("BossHealthBarPanel");
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            var rt = existing.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = new Vector2(0f, -44f);
            Instance = existing.GetComponent<BossHealthBarUI>();
            if (Instance != null) return Instance;
        }

        GameObject root = new GameObject("BossHealthBarPanel");
        root.transform.SetParent(canvas.transform, false);
        root.SetActive(true);

        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 1f);
        rootRt.anchorMax = new Vector2(0.5f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = new Vector2(0f, -44f);
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
        nameTmp.text = "CHUA TE NHEN - SKELETON KING";
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

        // 3. Ghost Slider
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
        pTmp.text = "[ 3 THANH MAU - GIAI DOAN 1 ]";
        pTmp.fontSize = 13;
        pTmp.fontStyle = FontStyles.Bold;
        pTmp.color = new Color(0.5f, 0.85f, 1f);
        pTmp.alignment = TextAlignmentOptions.Center;
        ui.phaseText = pTmp;

        Instance = ui;
        return ui;
    }

    private GameObject trackedBossObject = null;

    public void ShowBoss(string name, int totalPhases = 3)
    {
        ShowBoss((GameObject)null, name, totalPhases);
    }

    public void ShowBoss(GameObject bossObj, string name, int totalPhases = 3)
    {
        trackedBossObject = bossObj;
        ShowBoss((BotWheelAI)null, name);
        UpdatePhaseDisplay(totalPhases);
    }

    public void ShowBoss(BotWheelAI boss, string name)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!gameObject.activeInHierarchy)
        {
            Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
            if (canvas != null && canvas.gameObject.activeInHierarchy)
            {
                transform.SetParent(canvas.transform, false);
            }
            gameObject.SetActive(true);
        }

        currentBoss = boss;
        if (boss != null) trackedBossObject = boss.gameObject;
        if (bossNameText != null) bossNameText.text = name;

        if (healthSlider != null) healthSlider.value = 1f;
        if (ghostSlider != null) ghostSlider.value = 1f;
        UpdatePhaseDisplay(3);

        if (gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(FadeInRoutine());
        }
        else
        {
            isVisible = true;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }
    }

    public void UpdateHealth(int currentHp, int maxHp, int phase)
    {
        if (maxHp <= 0) return;

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!gameObject.activeInHierarchy)
        {
            Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
            if (canvas != null && canvas.gameObject.activeInHierarchy)
            {
                transform.SetParent(canvas.transform, false);
            }
            gameObject.SetActive(true);
        }

        float norm = Mathf.Clamp01((float)currentHp / maxHp);

        if (healthSlider != null) healthSlider.value = norm;
        if (hpPercentText != null) hpPercentText.text = $"{Mathf.CeilToInt(norm * 100f)}%";

        UpdatePhaseDisplay(phase);

        if (gameObject.activeInHierarchy)
        {
            if (ghostRoutine != null) StopCoroutine(ghostRoutine);
            ghostRoutine = StartCoroutine(GhostFillRoutine(norm));
        }
        else
        {
            if (ghostSlider != null) ghostSlider.value = norm;
        }
    }

    public void UpdatePhaseDisplay(int phase)
    {
        currentPhase = phase;
        if (healthFillImage != null)
        {
            if (phase == 3)
            {
                healthFillImage.sprite = SoulslikeHUDStyler.GetRubyFillSprite();
                healthFillImage.type = Image.Type.Sliced;
                healthFillImage.color = Color.white;
            }
            else if (phase == 2)
            {
                healthFillImage.sprite = SoulslikeHUDStyler.GetAmethystFillSprite();
                healthFillImage.type = Image.Type.Sliced;
                healthFillImage.color = Color.white;
            }
            else
            {
                healthFillImage.sprite = SoulslikeHUDStyler.GetRubyFillSprite();
                healthFillImage.type = Image.Type.Sliced;
                healthFillImage.color = new Color(1f, 0.35f, 0.35f, 1f);
            }
        }

        if (phaseText != null)
        {
            string roman = (phase == 3) ? "I" : ((phase == 2) ? "II" : "III");
            string pColor = (phase == 3) ? "#ffd700" : ((phase == 2) ? "#d880ff" : "#ff4040");

            string subTitle;
            if (currentBoss != null)
            {
                subTitle = (phase == 3)
                    ? LocalizationManager.T("KHỞI ĐỘNG CỖ MÁY", "CORE ACTIVATION")
                    : ((phase == 2)
                        ? LocalizationManager.T("BÁNH XE CUỒNG NỘ", "FRENZIED OVERDRIVE")
                        : LocalizationManager.T("TẬN DIỆT TÀN TÍCH", "ANCIENT OVERLOAD"));
            }
            else
            {
                subTitle = (phase == 3)
                    ? LocalizationManager.T("CHÚA TỂ THỨC TỈNH", "LORD AWAKENING")
                    : ((phase == 2)
                        ? LocalizationManager.T("BẦY NHỆN SĂN MỒI", "HUNTING SWARM")
                        : LocalizationManager.T("CUỒNG NỘ HUYẾT NGUYỆT", "BLOOD MOON FRENZY"));
            }

            string armorInfo = "";
            EnemyArmor armor = null;
            if (trackedBossObject != null)
            {
                armor = trackedBossObject.GetComponent<EnemyArmor>() ?? trackedBossObject.GetComponentInParent<EnemyArmor>();
            }
            if (armor == null && currentBoss != null)
            {
                armor = currentBoss.GetComponent<EnemyArmor>() ?? currentBoss.GetComponentInParent<EnemyArmor>();
            }
            if (armor == null)
            {
                armor = Object.FindAnyObjectByType<EnemyArmor>();
            }

            if (armor != null && (armor.CurrentPhysicalArmor > 0 || armor.CurrentMagicArmor > 0))
            {
                string armLabel = LocalizationManager.T("GIÁP", "ARM");
                string resLabel = LocalizationManager.T("KHÁNG", "RES");
                armorInfo = $"  <size=10><color=#F97316>[{armLabel}: {armor.CurrentPhysicalArmor}]</color> <color=#A855F7>[{resLabel}: {armor.CurrentMagicArmor}]</color></size>";
            }

            string phaseLabel = LocalizationManager.T("GIAI ĐOẠN", "PHASE");
            phaseText.text = $"<color={pColor}>[ {roman} ]</color>  <size=11><color=#c4b89f>{phaseLabel} {roman}: {subTitle}</color></size>{armorInfo}";
        }
    }

    public void HideBoss()
    {
        if (gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(FadeOutRoutine());
        }
        else
        {
            isVisible = false;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeInRoutine()
    {
        isVisible = true;
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.35f);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutRoutine()
    {
        isVisible = false;
        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.unscaledDeltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.45f);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
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