using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndingCreditsUI : MonoBehaviour
{
    public static EndingCreditsUI Instance { get; private set; }

    private CanvasGroup canvasGroup;
    private Image flashOverlay;
    private RectTransform scrollContent;
    private RectTransform statsWindow;
    private TextMeshProUGUI statsText;
    private Button btnSpeedUp;
    private Button btnSkip;
    private TextMeshProUGUI speedUpText;

    private float scrollSpeed = 46f;
    private float currentSpeedMultiplier = 1f;
    private bool isScrolling = false;
    private bool isFinished = false;
    private float maxScrollY = 1650f;

    public static void ShowEnding()
    {
        HideAllGameplayButtons();
        BossGigapackSkills.ClearAllWarningZonesAndEffects();
        if (Instance == null)
        {
            CreateRuntimeEndingUI();
        }
        Instance.StartEndingSequence();
    }

    public static void HideAllGameplayButtons()
    {
        // 1. Tắt toàn bộ nút Mobile Action (Attack, Dash, Skill, Potion,...)
        var actionButtons = Object.FindObjectsByType<MobileActionButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in actionButtons)
        {
            if (btn != null && btn.gameObject != null)
            {
                btn.gameObject.SetActive(false);
            }
        }

        // 2. Tắt Virtual Joystick và cụm Joystick điều khiển
        var joysticks = Object.FindObjectsByType<VirtualJoystick>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var joy in joysticks)
        {
            if (joy != null && joy.gameObject != null)
            {
                joy.gameObject.SetActive(false);
            }
        }
        var joyBg = GameObject.Find("JoystickBackground");
        if (joyBg != null) joyBg.SetActive(false);

        // 3. Tắt các nút và UI chức năng gameplay khác
        string[] uiNamesToHide = new string[]
        {
            "Btn_Attack", "Btn_Dash", "Btn_Special", "Btn_Potion", 
            "Btn_Settings", "Btn_CharacterStats", 
            "PlayerHealthBar", "ShardsCounter", "WaveBanner", 
            "VictoryPanel", "BossHealthBarPanel", "ShrineUpgradePanel", "GameOverPanel"
        };
        foreach (var name in uiNamesToHide)
        {
            var go = GameObject.Find(name);
            if (go != null) go.SetActive(false);
        }

        // 4. Tìm Main Gameplay Canvas và ẩn toàn bộ các con của nó (đảm bảo không còn nút nào sót lại)
        Canvas mainCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (mainCanvas != null)
        {
            for (int i = 0; i < mainCanvas.transform.childCount; i++)
            {
                Transform child = mainCanvas.transform.GetChild(i);
                if (child.name != "EndingCreditsCanvas" && child.name != "EndingCreditsPanel" && child.name != "TopControls")
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    void Awake()
    {
        Instance = this;
    }

    public void StartEndingSequence()
    {
        gameObject.SetActive(true);
        Time.timeScale = 1.0f; // Bỏ pause game nếu có
        HideAllGameplayButtons();
        StartCoroutine(EndingSequenceRoutine());
    }

    private IEnumerator EndingSequenceRoutine()
    {
        // 1. Khóa điều khiển người chơi
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null) player.SetCanMove(false);

        // 2. Chớp sáng thiêng liêng White-Gold (Divine Flash)
        SoundManager.EnsureInstance()?.PlayHeavenlyAscension();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (flashOverlay != null)
        {
            flashOverlay.color = new Color(1f, 0.96f, 0.85f, 1f);
            flashOverlay.gameObject.SetActive(true);

            float flashTimer = 0f;
            while (flashTimer < 1.4f)
            {
                flashTimer += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, flashTimer / 1.4f);
                flashOverlay.color = new Color(1f, 0.96f, 0.85f, alpha);
                yield return null;
            }
            flashOverlay.gameObject.SetActive(false);
        }

        // 3. Bắt đầu cuộn chữ Credits
        isScrolling = true;
        isFinished = false;
        currentSpeedMultiplier = 1f;

        if (scrollContent != null)
        {
            scrollContent.anchoredPosition = new Vector2(0f, -550f);
        }

        while (isScrolling && scrollContent != null && scrollContent.anchoredPosition.y < maxScrollY)
        {
            float step = scrollSpeed * currentSpeedMultiplier * Time.deltaTime;
            scrollContent.anchoredPosition += new Vector2(0f, step);
            yield return null;
        }

        ShowFinalStatsCard();
    }

    public void ToggleSpeedUp()
    {
        if (currentSpeedMultiplier == 1f)
        {
            currentSpeedMultiplier = 3.5f;
            if (speedUpText != null) speedUpText.text = "SPEED 3X";
        }
        else
        {
            currentSpeedMultiplier = 1f;
            if (speedUpText != null) speedUpText.text = "SPEED UP";
        }
    }

    public void SkipCredits()
    {
        if (isFinished) return;
        isScrolling = false;
        if (flashOverlay != null) flashOverlay.gameObject.SetActive(false);
        ShowFinalStatsCard();
    }

    private void ShowFinalStatsCard()
    {
        isScrolling = false;
        isFinished = true;

        if (scrollContent != null)
        {
            scrollContent.gameObject.SetActive(false);
        }

        if (btnSpeedUp != null) btnSpeedUp.gameObject.SetActive(false);
        if (btnSkip != null) btnSkip.gameObject.SetActive(false);

        if (statsWindow != null)
        {
            statsWindow.gameObject.SetActive(true);
            statsWindow.localScale = Vector3.one * 0.8f;
            StartCoroutine(PopWindowRoutine(statsWindow));
        }

        // Cập nhật số liệu hành trình
        int shards = 0;
        var inv = Object.FindAnyObjectByType<PlayerInventory>();
        if (inv != null) shards = inv.GetShards();

        if (statsText != null)
        {
            bool isVi = LocalizationManager.IsVietnamese;
            statsText.text = isVi
                ? $"<color=#FFE066>ẢI ĐÃ HOÀN THÀNH: 2 / 2</color>\n" +
                  $"<color=#E0E6ED>Chương 1:</color> Tàn Tích Thung Lũng  <color=#52F0FF>[VƯỢT QUA]</color>\n" +
                  $"<color=#E0E6ED>Chương 2:</color> Ngục Tối Rực Lửa  <color=#52F0FF>[THANH TẨY]</color>\n\n" +
                  $"<color=#FFE066>TỔNG LINH HỒN:</color> {shards}\n" +
                  $"<color=#FFE066>XẾP HẠNG TỔNG KẾT:</color> <color=#FF5577>S+ ĐẠI CHIẾN TƯỚNG</color>\n\n" +
                  $"<i>\"Vòng lặp diệt vong đã bị phá vỡ. Vương quốc Vaelstrider đã tìm lại thái bình.\"</i>"
                : $"<color=#FFE066>REALMS PURIFIED: 2 / 2</color>\n" +
                  $"<color=#E0E6ED>Chapter 1:</color> Sunken Sepulcher  <color=#52F0FF>[CLEARED]</color>\n" +
                  $"<color=#E0E6ED>Chapter 2:</color> Valley Ruin of Ancients  <color=#52F0FF>[PURIFIED]</color>\n\n" +
                  $"<color=#FFE066>TOTAL AETHEL SHARDS:</color> {shards}\n" +
                  $"<color=#FFE066>CLEAR RANK:</color> <color=#FF5577>S+ GRAND CHAMPION</color>\n\n" +
                  $"<i>\"The cycle of decay is broken. The world of Vaelstrider is at peace.\"</i>";
        }
    }

    private IEnumerator PopWindowRoutine(RectTransform target)
    {
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float p = t / 0.35f;
            target.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, Mathf.SmoothStep(0f, 1f, p));
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    public void OnClickPlayAgain()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("SampleScene"))
        {
            SceneManager.LoadScene("SampleScene");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    public void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("MainMenuScene"))
        {
            SceneManager.LoadScene("MainMenuScene");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    public static void CreateRuntimeEndingUI()
    {
        GameObject canvasObj = GameObject.Find("EndingCreditsCanvas");
        Canvas canvas = null;
        if (canvasObj == null)
        {
            canvasObj = new GameObject("EndingCreditsCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99998;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99998;
        }

        Transform existing = canvas.transform.Find("EndingCreditsPanel");
        if (existing != null)
        {
            Instance = existing.GetComponent<EndingCreditsUI>();
            return;
        }

        GameObject root = new GameObject("EndingCreditsPanel");
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.04f, 0.98f);

        CanvasGroup cg = root.AddComponent<CanvasGroup>();
        var ui = root.AddComponent<EndingCreditsUI>();
        ui.canvasGroup = cg;

        // Flash Overlay
        GameObject flashObj = new GameObject("FlashOverlay");
        flashObj.transform.SetParent(root.transform, false);
        RectTransform flashRt = flashObj.AddComponent<RectTransform>();
        flashRt.anchorMin = Vector2.zero;
        flashRt.anchorMax = Vector2.one;
        flashRt.offsetMin = Vector2.zero;
        flashRt.offsetMax = Vector2.zero;
        Image flashImg = flashObj.AddComponent<Image>();
        flashImg.color = Color.white;
        ui.flashOverlay = flashImg;

        // Scroll Content
        GameObject scrollObj = new GameObject("ScrollContent");
        scrollObj.transform.SetParent(root.transform, false);
        RectTransform scrollRt = scrollObj.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRt.pivot = new Vector2(0.5f, 0.5f);
        scrollRt.sizeDelta = new Vector2(900f, 2200f);
        scrollRt.anchoredPosition = new Vector2(0f, -550f);
        ui.scrollContent = scrollRt;

        var creditsTmp = scrollObj.AddComponent<TextMeshProUGUI>();
        creditsTmp.richText = true;
        creditsTmp.alignment = TextAlignmentOptions.Center;
        creditsTmp.text = GetCreditsFormattedText();
        creditsTmp.fontSize = 24;
        creditsTmp.lineSpacing = 16f;

        // Buttons Container (Speed Up / Skip)
        GameObject topBar = new GameObject("TopControls");
        topBar.transform.SetParent(root.transform, false);
        RectTransform topRt = topBar.AddComponent<RectTransform>();
        topRt.anchorMin = new Vector2(1f, 1f);
        topRt.anchorMax = new Vector2(1f, 1f);
        topRt.pivot = new Vector2(1f, 1f);
        topRt.anchoredPosition = new Vector2(-30f, -30f);
        topRt.sizeDelta = new Vector2(340f, 50f);

        ui.btnSpeedUp = CreateSmallButton(topBar.transform, new Vector2(-170f, 0f), "SPEED UP", new Color(0.18f, 0.28f, 0.45f, 0.95f), ui.ToggleSpeedUp);
        ui.speedUpText = ui.btnSpeedUp.GetComponentInChildren<TextMeshProUGUI>();
        ui.btnSkip = CreateSmallButton(topBar.transform, new Vector2(0f, 0f), "SKIP", new Color(0.45f, 0.15f, 0.18f, 0.95f), ui.SkipCredits);

        // Stats Window
        GameObject statsObj = new GameObject("FinalStatsCard");
        statsObj.transform.SetParent(root.transform, false);
        RectTransform sRt = statsObj.AddComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.5f, 0.5f);
        sRt.anchorMax = new Vector2(0.5f, 0.5f);
        sRt.pivot = new Vector2(0.5f, 0.5f);
        sRt.sizeDelta = new Vector2(680f, 480f);
        sRt.anchoredPosition = Vector2.zero;

        Image sBg = statsObj.AddComponent<Image>();
        sBg.color = new Color(0.08f, 0.08f, 0.14f, 0.98f);
        ui.statsWindow = sRt;
        statsObj.SetActive(false);

        // Title on stats card
        GameObject sTitleObj = new GameObject("Title");
        sTitleObj.transform.SetParent(statsObj.transform, false);
        RectTransform sTitleRt = sTitleObj.AddComponent<RectTransform>();
        sTitleRt.anchoredPosition = new Vector2(0f, 180f);
        sTitleRt.sizeDelta = new Vector2(600f, 50f);
        var sTitleTmp = sTitleObj.AddComponent<TextMeshProUGUI>();
        var sTitleLoc = sTitleObj.AddComponent<LocalizedText>();
        sTitleLoc.SetTexts("HOÀN THÀNH HÀNH TRÌNH", "JOURNEY COMPLETED");
        sTitleTmp.fontSize = 32;
        sTitleTmp.fontStyle = FontStyles.Bold;
        sTitleTmp.color = new Color(1f, 0.88f, 0.35f);
        sTitleTmp.alignment = TextAlignmentOptions.Center;

        // Content on stats card
        GameObject sBodyObj = new GameObject("Body");
        sBodyObj.transform.SetParent(statsObj.transform, false);
        RectTransform sBodyRt = sBodyObj.AddComponent<RectTransform>();
        sBodyRt.anchoredPosition = new Vector2(0f, 25f);
        sBodyRt.sizeDelta = new Vector2(600f, 220f);
        var sBodyTmp = sBodyObj.AddComponent<TextMeshProUGUI>();
        sBodyTmp.fontSize = 20;
        sBodyTmp.lineSpacing = 12f;
        sBodyTmp.alignment = TextAlignmentOptions.Center;
        ui.statsText = sBodyTmp;

        // Bottom action buttons on card
        GameObject sBtnContainer = new GameObject("ButtonsContainer");
        sBtnContainer.transform.SetParent(statsObj.transform, false);
        RectTransform sBtnRt = sBtnContainer.AddComponent<RectTransform>();
        sBtnRt.anchoredPosition = new Vector2(0f, -170f);
        sBtnRt.sizeDelta = new Vector2(560f, 60f);

        CreateActionButton(sBtnContainer.transform, new Vector2(-140f, 0f), LocalizationManager.T("CHƠI LẠI", "PLAY AGAIN"), new Color(0.2f, 0.38f, 0.72f, 1f), ui.OnClickPlayAgain);
        CreateActionButton(sBtnContainer.transform, new Vector2(140f, 0f), LocalizationManager.T("TRANG CHỦ", "MAIN MENU"), new Color(0.24f, 0.58f, 0.35f, 1f), ui.OnClickMainMenu);

        // Đảm bảo TopControls luôn ở lớp trên cùng
        topBar.transform.SetAsLastSibling();

        root.SetActive(false);
    }

    private static string GetCreditsFormattedText()
    {
        return
            "<size=150%><color=#FFE066><b>VAELSTRIDER: RUINBOUND</b></color></size>\n" +
            "<color=#52F0FF>THE ANCIENT CYCLE RESTORED</color>\n\n" +
            "---------------------------------\n\n" +
            "<color=#94A3B8><size=80%>PRODUCED & DIRECTED BY</size></color>\n" +
            "<size=120%><color=#FFFFFF><b>Trương Hùng</b></color></size>\n\n\n" +
            "<color=#94A3B8><size=80%>SYSTEM ARCHITECT & AI PAIR PROGRAMMER</size></color>\n" +
            "<size=120%><color=#52F0FF><b>Antigravity</b></color></size>\n" +
            "<size=85%><color=#CBD5E1>Google DeepMind Advanced Agentic Coding</color></size>\n\n\n" +
            "<color=#94A3B8><size=80%>PIXEL ART & VISUAL ASSETS</size></color>\n" +
            "<color=#FFFFFF><b>Valley Ruin Environment Pack</b></color>\n" +
            "<color=#CBD5E1>TX Tileset Stone Ground Architecture</color>\n" +
            "<color=#CBD5E1>Adventurer & Maid Warrior Spritesheets</color>\n" +
            "<color=#CBD5E1>Universal Render Pipeline 2D Lighting & Glow</color>\n\n\n" +
            "<color=#94A3B8><size=80%>SOUNDTRACK & AUDIO DESIGN</size></color>\n" +
            "<color=#FFFFFF><b>Coastal Forest Natural Ambience</b></color>\n" +
            "<color=#CBD5E1>Rune Pillar Sacred Resonance</color>\n" +
            "<color=#CBD5E1>Harmonic Heavenly Ascension Chime</color>\n" +
            "<color=#CBD5E1>Custom 16-Bit Retro Synthesizer Engine</color>\n\n\n" +
            "<color=#94A3B8><size=80%>GAMEPLAY SYSTEMS & MECHANICS</size></color>\n" +
            "<color=#CBD5E1>Aethel Shards Soul Economy</color>\n" +
            "<color=#CBD5E1>Ancient Shrine 4-Branch Upgrade Matrix</color>\n" +
            "<color=#CBD5E1>Dark Souls Lost Shards Grave Retrieval</color>\n" +
            "<color=#CBD5E1>Cinematic Anime Boss Eye-Flare Finisher</color>\n\n\n" +
            "<color=#94A3B8><size=80%>LEVEL & WORLD DESIGN</size></color>\n" +
            "<color=#FFFFFF><b>Realm 1: Sunken Sepulcher</b></color>\n" +
            "<color=#FFFFFF><b>Realm 2: Valley Ruin of the Ancients</b></color>\n\n\n" +
            "---------------------------------\n\n" +
            "<color=#94A3B8><size=80%>SPECIAL DEDICATION</size></color>\n" +
            "<color=#E2E8F0>To every warrior who walked through the darkness,</color>\n" +
            "<color=#E2E8F0>and held fast until the first light of dawn.</color>\n\n" +
            "<size=130%><color=#FFE066><b>THANK YOU FOR PLAYING!</b></color></size>\n";
    }

    private static Button CreateSmallButton(Transform parent, Vector2 pos, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(150f, 44f);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = color;

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.4f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private static Button CreateActionButton(Transform parent, Vector2 pos, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(230f, 54f);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = color;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }
}
