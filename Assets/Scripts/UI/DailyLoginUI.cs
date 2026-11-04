using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyLoginUI : MonoBehaviour
{
    public static DailyLoginUI Instance { get; private set; }

    private static readonly int[] REWARDS = new int[] {
        500,   // Ngày 1
        750,   // Ngày 2
        1000,  // Ngày 3
        1500,  // Ngày 4
        2000,  // Ngày 5
        2500,  // Ngày 6
        4000   // Ngày 7 (Quà khủng kết tuần)
    };

    private System.Action onCloseCallback;

    // UI References
    private Transform cardsContainer;
    private TextMeshProUGUI timerText;
    private Button claimButton;
    private TextMeshProUGUI claimBtnText;

    /// <summary>
    /// Tính ngày logic theo mốc 07:00 AM (Trừ 7 tiếng so với giờ hiện tại)
    /// </summary>
    public static DateTime GetLogicalDate()
    {
        return DateTime.Now.AddHours(-7).Date;
    }

    public static string GetLogicalDateKey()
    {
        return GetLogicalDate().ToString("yyyyMMdd");
    }

    public static bool CanClaimToday()
    {
        string todayKey = GetLogicalDateKey();
        string lastClaim = PlayerPrefs.GetString("DailyLogin_LastClaimDate", "");
        return todayKey != lastClaim;
    }

    public static string GetTimeToNextReset()
    {
        DateTime now = DateTime.Now;
        DateTime nextReset;
        if (now.Hour >= 7)
        {
            nextReset = now.Date.AddDays(1).AddHours(7);
        }
        else
        {
            nextReset = now.Date.AddHours(7);
        }
        TimeSpan rem = nextReset - now;
        return $"{rem.Hours:D2}h {rem.Minutes:D2}m {rem.Seconds:D2}s";
    }

    public static void OpenUI(System.Action onClose = null)
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
        }

        Canvas rootCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (rootCanvas == null) rootCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();

        if (rootCanvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rootCanvas = canvasObj.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        GameObject loginObj = new GameObject("DailyLoginUI", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        loginObj.layer = 5;
        loginObj.transform.SetParent(rootCanvas.transform, false);

        RectTransform rt = loginObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = loginObj.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        bg.raycastTarget = true;

        Instance = loginObj.AddComponent<DailyLoginUI>();
        Instance.onCloseCallback = onClose;
        Instance.BuildUI();
    }

    private void Update()
    {
        if (timerText != null)
        {
            bool isVi = LocalizationManager.IsVietnamese;
            timerText.text = isVi 
                ? $"<color=#88A0C0>Làm mới lúc 07:00 sáng • Còn lại: </color><color=#55EFC4><b>{GetTimeToNextReset()}</b></color>"
                : $"<color=#88A0C0>Resets at 07:00 AM • Next in: </color><color=#55EFC4><b>{GetTimeToNextReset()}</b></color>";
        }
    }

    private void BuildUI()
    {
        bool isVi = LocalizationManager.IsVietnamese;

        // Modal Window
        GameObject win = new GameObject("Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        win.layer = 5;
        win.transform.SetParent(transform, false);

        RectTransform wRt = win.GetComponent<RectTransform>();
        wRt.anchorMin = new Vector2(0.5f, 0.5f);
        wRt.anchorMax = new Vector2(0.5f, 0.5f);
        wRt.pivot = new Vector2(0.5f, 0.5f);
        wRt.anchoredPosition = Vector2.zero;
        wRt.sizeDelta = new Vector2(1040f, 580f);

        Image winImg = win.GetComponent<Image>();
        winImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        winImg.type = Image.Type.Sliced;
        winImg.color = new Color(0.04f, 0.06f, 0.10f, 0.98f);

        var winOut = win.AddComponent<Outline>();
        winOut.effectColor = new Color(0.20f, 0.85f, 0.65f, 0.75f);
        winOut.effectDistance = new Vector2(2f, -2f);

        // Header Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(win.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -20f);
        tRt.sizeDelta = new Vector2(900f, 36f);

        var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
        tTmp.text = isVi ? "<b>ĐIỂM DANH 7 NGÀY NHẬN LINH HỒN</b>" : "<b>7-DAY SOUL ATTENDANCE</b>";
        tTmp.fontSize = 24f;
        tTmp.color = new Color(1f, 0.88f, 0.45f);
        tTmp.alignment = TextAlignmentOptions.Center;

        // Subtitle
        GameObject subObj = new GameObject("SubTitle", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.transform.SetParent(win.transform, false);
        RectTransform stRt = subObj.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0f, 1f);
        stRt.anchorMax = new Vector2(1f, 1f);
        stRt.pivot = new Vector2(0.5f, 1f);
        stRt.anchoredPosition = new Vector2(0f, -54f);
        stRt.sizeDelta = new Vector2(900f, 24f);

        var stTmp = subObj.AddComponent<TextMeshProUGUI>();
        stTmp.text = isVi 
            ? "Đăng nhập mỗi ngày để nhận Mảnh Linh Hồn. Tự động làm mới vào <b>07:00 Sáng</b> hàng ngày!" 
            : "Login daily to claim Soul Shards. Automatically resets at <b>07:00 AM</b> daily!";
        stTmp.fontSize = 13.5f;
        stTmp.color = new Color(0.70f, 0.80f, 0.92f, 0.85f);
        stTmp.alignment = TextAlignmentOptions.Center;

        // Close Button
        GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtnObj.layer = 5;
        closeBtnObj.transform.SetParent(win.transform, false);
        RectTransform cRt = closeBtnObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-18f, -18f);
        cRt.sizeDelta = new Vector2(50f, 50f);

        Image cImg = closeBtnObj.GetComponent<Image>();
        cImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        cImg.type = Image.Type.Sliced;
        cImg.color = new Color(0.38f, 0.15f, 0.16f, 0.95f);

        GameObject cTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        cTxtObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform ctRt = cTxtObj.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero;
        ctRt.anchorMax = Vector2.one;
        ctRt.sizeDelta = Vector2.zero;
        var ctTmp = cTxtObj.AddComponent<TextMeshProUGUI>();
        ctTmp.text = "<b>✕</b>";
        ctTmp.fontSize = 24f;
        ctTmp.alignment = TextAlignmentOptions.Center;
        ctTmp.color = Color.white;

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.targetGraphic = cImg;
        closeBtn.onClick.AddListener(CloseUI);

        // Divider
        SoulslikeHUDStyler.AddFiligreeDivider(win.transform, new Vector2(0f, -80f), new Vector2(960f, 6f));

        // Cards Container
        GameObject cardsObj = new GameObject("CardsContainer", typeof(RectTransform));
        cardsObj.transform.SetParent(win.transform, false);
        RectTransform cdRt = cardsObj.GetComponent<RectTransform>();
        cdRt.anchorMin = new Vector2(0.5f, 0.5f);
        cdRt.anchorMax = new Vector2(0.5f, 0.5f);
        cdRt.pivot = new Vector2(0.5f, 0.5f);
        cdRt.anchoredPosition = new Vector2(0f, 15f);
        cdRt.sizeDelta = new Vector2(980f, 260f);

        cardsContainer = cardsObj.transform;

        // Bottom Timer
        GameObject tmObj = new GameObject("TimerText", typeof(RectTransform), typeof(CanvasRenderer));
        tmObj.transform.SetParent(win.transform, false);
        RectTransform tmRt = tmObj.GetComponent<RectTransform>();
        tmRt.anchorMin = new Vector2(0.5f, 0f);
        tmRt.anchorMax = new Vector2(0.5f, 0f);
        tmRt.pivot = new Vector2(0.5f, 0f);
        tmRt.anchoredPosition = new Vector2(0f, 84f);
        tmRt.sizeDelta = new Vector2(700f, 26f);

        timerText = tmObj.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 13.5f;
        timerText.alignment = TextAlignmentOptions.Center;

        // Big Claim Button at Bottom
        GameObject claimBtnObj = new GameObject("Btn_ClaimDaily", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        claimBtnObj.layer = 5;
        claimBtnObj.transform.SetParent(win.transform, false);
        RectTransform clRt = claimBtnObj.GetComponent<RectTransform>();
        clRt.anchorMin = new Vector2(0.5f, 0f);
        clRt.anchorMax = new Vector2(0.5f, 0f);
        clRt.pivot = new Vector2(0.5f, 0f);
        clRt.anchoredPosition = new Vector2(0f, 20f);
        clRt.sizeDelta = new Vector2(460f, 54f);

        Image clImg = claimBtnObj.GetComponent<Image>();
        clImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        clImg.type = Image.Type.Sliced;

        var clOut = claimBtnObj.AddComponent<Outline>();
        clOut.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject clTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        clTxtObj.transform.SetParent(claimBtnObj.transform, false);
        RectTransform cltRt = clTxtObj.GetComponent<RectTransform>();
        cltRt.anchorMin = Vector2.zero;
        cltRt.anchorMax = Vector2.one;
        cltRt.sizeDelta = Vector2.zero;

        claimBtnText = clTxtObj.AddComponent<TextMeshProUGUI>();
        claimBtnText.fontSize = 16f;
        claimBtnText.fontStyle = FontStyles.Bold;
        claimBtnText.alignment = TextAlignmentOptions.Center;

        claimButton = claimBtnObj.GetComponent<Button>();
        claimButton.targetGraphic = clImg;
        claimButton.onClick.AddListener(OnClaimToday);

        RefreshCards();
    }

    private void RefreshCards()
    {
        bool isVi = LocalizationManager.IsVietnamese;
        bool canClaim = CanClaimToday();
        int curDayIndex = PlayerPrefs.GetInt("DailyLogin_DayIndex", 0);
        if (curDayIndex < 0 || curDayIndex >= 7) curDayIndex = 0;

        // Clear children
        for (int i = cardsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(cardsContainer.GetChild(i).gameObject);
        }

        float totalW = 980f;
        float cardW = 126f;
        float cardH = 240f;
        float spacing = 12f;
        float startX = -((cardW * 7 + spacing * 6) * 0.5f) + cardW * 0.5f;

        for (int i = 0; i < 7; i++)
        {
            bool isDay7 = (i == 6);
            bool isPastClaimed = (i < curDayIndex);
            bool isToday = (i == curDayIndex);
            bool isClaimedToday = isToday && !canClaim;
            bool isAvailableToday = isToday && canClaim;
            bool isLockedFuture = (i > curDayIndex);

            GameObject card = new GameObject("Card_Day" + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(cardsContainer, false);

            RectTransform cRt = card.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(startX + i * (cardW + spacing), 0f);
            cRt.sizeDelta = new Vector2(cardW, cardH);

            Image cardImg = card.GetComponent<Image>();
            cardImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
            cardImg.type = Image.Type.Sliced;

            var outline = card.AddComponent<Outline>();

            // Colors based on state
            if (isAvailableToday)
            {
                cardImg.color = new Color(0.10f, 0.22f, 0.28f, 0.98f);
                outline.effectColor = new Color(0.35f, 0.95f, 0.85f, 0.95f);
                outline.effectDistance = new Vector2(2f, -2f);
                card.transform.localScale = Vector3.one * 1.04f;
            }
            else if (isPastClaimed || isClaimedToday)
            {
                cardImg.color = new Color(0.04f, 0.06f, 0.08f, 0.85f);
                outline.effectColor = new Color(0.20f, 0.40f, 0.30f, 0.50f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
            else // Future
            {
                cardImg.color = new Color(0.05f, 0.07f, 0.10f, 0.88f);
                outline.effectColor = isDay7 ? new Color(0.95f, 0.80f, 0.30f, 0.70f) : new Color(0.20f, 0.30f, 0.45f, 0.40f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            // Top Header: NGÀY 1..7
            GameObject topObj = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer));
            topObj.transform.SetParent(card.transform, false);
            RectTransform thRt = topObj.GetComponent<RectTransform>();
            thRt.anchorMin = new Vector2(0f, 1f);
            thRt.anchorMax = new Vector2(1f, 1f);
            thRt.pivot = new Vector2(0.5f, 1f);
            thRt.anchoredPosition = new Vector2(0f, -10f);
            thRt.sizeDelta = new Vector2(cardW - 10f, 24f);

            var thTmp = topObj.AddComponent<TextMeshProUGUI>();
            thTmp.text = isVi ? $"<b>NGÀY {i + 1}</b>" : $"<b>DAY {i + 1}</b>";
            thTmp.fontSize = 13.5f;
            thTmp.alignment = TextAlignmentOptions.Center;
            thTmp.color = isDay7 ? new Color(1f, 0.90f, 0.45f) : (isAvailableToday ? new Color(0.4f, 0.95f, 1f) : new Color(0.75f, 0.82f, 0.90f));

            // Reward Amount in Center
            GameObject rwObj = new GameObject("RewardText", typeof(RectTransform), typeof(CanvasRenderer));
            rwObj.transform.SetParent(card.transform, false);
            RectTransform rwRt = rwObj.GetComponent<RectTransform>();
            rwRt.anchorMin = new Vector2(0f, 0.5f);
            rwRt.anchorMax = new Vector2(1f, 0.5f);
            rwRt.pivot = new Vector2(0.5f, 0.5f);
            rwRt.anchoredPosition = new Vector2(0f, 10f);
            rwRt.sizeDelta = new Vector2(cardW - 10f, 40f);

            var rwTmp = rwObj.AddComponent<TextMeshProUGUI>();
            rwTmp.text = $"<b>+{REWARDS[i]:N0}</b>\n<size=11>{(isVi ? "LINH HỒN" : "SOULS")}</size>";
            rwTmp.fontSize = 15f;
            rwTmp.alignment = TextAlignmentOptions.Center;
            rwTmp.color = isDay7 ? new Color(1f, 0.85f, 0.35f) : Color.white;

            // Status Badge at Bottom of Card
            GameObject stObj = new GameObject("StatusBadge", typeof(RectTransform), typeof(CanvasRenderer));
            stObj.transform.SetParent(card.transform, false);
            RectTransform stRt = stObj.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0f, 0f);
            stRt.anchorMax = new Vector2(1f, 0f);
            stRt.pivot = new Vector2(0.5f, 0f);
            stRt.anchoredPosition = new Vector2(0f, 12f);
            stRt.sizeDelta = new Vector2(cardW - 16f, 28f);

            var stTmp = stObj.AddComponent<TextMeshProUGUI>();
            stTmp.fontSize = 12f;
            stTmp.fontStyle = FontStyles.Bold;
            stTmp.alignment = TextAlignmentOptions.Center;

            if (isPastClaimed || isClaimedToday)
            {
                stTmp.text = isVi ? "<color=#2ECC71>✔ ĐÃ NHẬN</color>" : "<color=#2ECC71>✔ CLAIMED</color>";
            }
            else if (isAvailableToday)
            {
                stTmp.text = isVi ? "<color=#55EFC4>★ HÔM NAY</color>" : "<color=#55EFC4>★ TODAY</color>";
            }
            else
            {
                stTmp.text = isVi ? "<color=#636E72>🔒 CHƯA ĐẾN</color>" : "<color=#636E72>🔒 LOCKED</color>";
            }
        }

        // Update Bottom Claim Button
        Image bImg = claimButton.GetComponent<Image>();
        Outline bOut = claimButton.GetComponent<Outline>();

        if (canClaim)
        {
            claimButton.interactable = true;
            bImg.color = new Color(0.10f, 0.50f, 0.28f, 0.98f);
            bOut.effectColor = new Color(0.35f, 1f, 0.65f, 0.85f);
            claimBtnText.text = isVi 
                ? $"<b>ĐIỂM DANH NGÀY {curDayIndex + 1} (+{REWARDS[curDayIndex]:N0} LINH HỒN)</b>"
                : $"<b>CLAIM DAY {curDayIndex + 1} (+{REWARDS[curDayIndex]:N0} SOULS)</b>";
            claimBtnText.color = Color.white;
        }
        else
        {
            claimButton.interactable = false;
            bImg.color = new Color(0.18f, 0.22f, 0.28f, 0.90f);
            bOut.effectColor = new Color(0.30f, 0.35f, 0.45f, 0.50f);
            claimBtnText.text = isVi 
                ? "<b>ĐÃ ĐIỂM DANH HÔM NAY (LÀM MỚI LÚC 07:00 SÁNG)</b>"
                : "<b>CLAIMED TODAY (RESETS AT 07:00 AM)</b>";
            claimBtnText.color = new Color(0.65f, 0.72f, 0.82f);
        }
    }

    private void OnClaimToday()
    {
        if (!CanClaimToday()) return;

        int curDayIndex = PlayerPrefs.GetInt("DailyLogin_DayIndex", 0);
        if (curDayIndex < 0 || curDayIndex >= 7) curDayIndex = 0;

        int reward = REWARDS[curDayIndex];

        // 1. Add Shards to account
        AccountDatabase.AddAethelShards(reward);

        // 2. Advance streak & save date
        string todayKey = GetLogicalDateKey();
        PlayerPrefs.SetString("DailyLogin_LastClaimDate", todayKey);
        PlayerPrefs.SetInt("DailyLogin_DayIndex", (curDayIndex + 1) % 7);
        PlayerPrefs.Save();

        // 3. Audio & FX
        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();

        // 4. Refresh HUD & Badges
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.RefreshCurrencies();
            MainMenuManager.Instance.UpdateDailyLoginBadge();
        }

        RefreshCards();
    }

    private void CloseUI()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        var cb = onCloseCallback;
        Destroy(gameObject);
        cb?.Invoke();
    }
}
