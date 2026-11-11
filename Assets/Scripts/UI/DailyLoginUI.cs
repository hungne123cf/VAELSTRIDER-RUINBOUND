using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyLoginUI : MonoBehaviour
{
    public static DailyLoginUI Instance { get; private set; }

    private static readonly int[] REWARDS = new int[] {
        500,   // Thứ 2 (Ngày 1)
        750,   // Thứ 3 (Ngày 2)
        1000,  // Thứ 4 (Ngày 3)
        1500,  // Thứ 5 (Ngày 4)
        2000,  // Thứ 6 (Ngày 5)
        2500,  // Thứ 7 (Ngày 6)
        4000   // Chủ Nhật (Ngày 7 - Quà khủng kết tuần)
    };

    private static readonly string[] DAY_NAMES_VI = new string[] {
        "THỨ 2", "THỨ 3", "THỨ 4", "THỨ 5", "THỨ 6", "THỨ 7", "CHỦ NHẬT"
    };

    private static readonly string[] DAY_NAMES_EN = new string[] {
        "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN"
    };

    private System.Action onCloseCallback;

    // UI References
    private Transform cardsContainer;
    private TextMeshProUGUI timerText;
    private Button claimButton;
    private TextMeshProUGUI claimBtnText;

    /// <summary>
    /// Giờ logic trong game: lùi 7 tiếng để 07:00 Sáng là thời điểm bắt đầu ngày mới
    /// </summary>
    public static DateTime GetLogicalNow()
    {
        return DateTime.Now.AddHours(-7);
    }

    /// <summary>
    /// Vị trí ngày trong tuần hiện tại: 0 = Thứ 2 (Day 1) ... 6 = Chủ Nhật (Day 7)
    /// </summary>
    public static int GetTodayDayIndex()
    {
        DayOfWeek dow = GetLogicalNow().DayOfWeek;
        // DayOfWeek: Sunday = 0, Monday = 1, Tuesday = 2 ... Saturday = 6
        return ((int)dow + 6) % 7;
    }

    /// <summary>
    /// Ngày Thứ 2 đầu tuần của tuần hiện tại (theo mốc 07:00 sáng)
    /// </summary>
    public static DateTime GetMondayOfCurrentWeek()
    {
        DateTime logicalNow = GetLogicalNow();
        int dayIndex = GetTodayDayIndex();
        return logicalNow.Date.AddDays(-dayIndex);
    }

    /// <summary>
    /// Khóa lưu trữ của ngày hôm nay (yyyyMMdd)
    /// </summary>
    public static string GetTodayKey()
    {
        return GetLogicalNow().ToString("yyyyMMdd");
    }

    /// <summary>
    /// Kiểm tra hôm nay người chơi đã điểm danh hay chưa
    /// </summary>
    public static bool CanClaimToday()
    {
        string todayKey = "DailyClaimed_" + GetTodayKey();
        return PlayerPrefs.GetInt(todayKey, 0) == 0;
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
                ? $"<color=#88A0C0>Làm mới lúc 07:00 sáng | Còn lại: </color><color=#55EFC4><b>{GetTimeToNextReset()}</b></color>"
                : $"<color=#88A0C0>Resets at 07:00 AM | Next in: </color><color=#55EFC4><b>{GetTimeToNextReset()}</b></color>";
        }
    }

    private void BuildUI()
    {
        bool isVi = LocalizationManager.IsVietnamese;

        // Modal Window - Phóng to toàn màn hình (Fullscreen Responsive sát viền an toàn)
        GameObject win = new GameObject("Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        win.layer = 5;
        win.transform.SetParent(transform, false);

        RectTransform wRt = win.GetComponent<RectTransform>();
        wRt.anchorMin = Vector2.zero;
        wRt.anchorMax = Vector2.one;
        wRt.pivot = new Vector2(0.5f, 0.5f);
        wRt.offsetMin = new Vector2(18f, 14f);
        wRt.offsetMax = new Vector2(-18f, -14f);
        wRt.sizeDelta = Vector2.zero;

        Image winImg = win.GetComponent<Image>();
        winImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        winImg.type = Image.Type.Sliced;
        winImg.color = new Color(0.04f, 0.05f, 0.07f, 0.98f);

        // Viền Hoàng Kim Cổ Điển Soulslike
        var winOut = win.AddComponent<Outline>();
        winOut.effectColor = new Color(0.85f, 0.72f, 0.38f, 0.90f);
        winOut.effectDistance = new Vector2(2f, -2f);

        // Header Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(win.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 1f);
        tRt.anchorMax = new Vector2(0.5f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -18f);
        tRt.sizeDelta = new Vector2(1300f, 50f);

        var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
        tTmp.text = isVi ? "<b>ĐIỂM DANH TUẦN NHẬN LINH HỒN</b>" : "<b>WEEKLY SOUL ATTENDANCE</b>";
        tTmp.fontSize = 42f;
        tTmp.fontStyle = FontStyles.Bold;
        tTmp.color = new Color(1f, 0.88f, 0.45f);
        tTmp.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(tTmp, 0.35f, Color.black);

        // Subtitle
        GameObject subObj = new GameObject("SubTitle", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.transform.SetParent(win.transform, false);
        RectTransform stRt = subObj.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0.5f, 1f);
        stRt.anchorMax = new Vector2(0.5f, 1f);
        stRt.pivot = new Vector2(0.5f, 1f);
        stRt.anchoredPosition = new Vector2(0f, -68f);
        stRt.sizeDelta = new Vector2(1500f, 38f);

        var stTmp = subObj.AddComponent<TextMeshProUGUI>();
        stTmp.text = isVi 
            ? "Đăng nhập ngày nào nhận ngày đó (Bỏ lỡ sẽ không nhận lại được). Tự động làm mới vào <b>07:00 Sáng</b> mỗi ngày!" 
            : "Claim on the day you login (Missed days cannot be reclaimed). Resets at <b>07:00 AM</b> daily!";
        stTmp.fontSize = 24f;
        stTmp.color = new Color(0.75f, 0.85f, 0.95f, 0.95f);
        stTmp.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(stTmp, 0.25f, Color.black);

        // Nút Đóng [X] - Huy Hiệu Tròn Runic Ruby Quý Phái
        GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtnObj.layer = 5;
        closeBtnObj.transform.SetParent(win.transform, false);
        RectTransform cRt = closeBtnObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-22f, -18f);
        cRt.sizeDelta = new Vector2(72f, 72f);

        Image cImg = closeBtnObj.GetComponent<Image>();
        Sprite coreSpr = SoulslikeHUDStyler.GetActionButtonCoreSprite();
        if (coreSpr != null) cImg.sprite = coreSpr;
        cImg.color = new Color(0.22f, 0.08f, 0.10f, 0.98f);

        // Viền Runic Ring vàng cổ cho nút Đóng
        GameObject cRingObj = new GameObject("CloseRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cRingObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform crRt = cRingObj.GetComponent<RectTransform>();
        crRt.anchorMin = Vector2.zero;
        crRt.anchorMax = Vector2.one;
        crRt.pivot = new Vector2(0.5f, 0.5f);
        crRt.anchoredPosition = Vector2.zero;
        crRt.sizeDelta = Vector2.zero;
        Image crImg = cRingObj.GetComponent<Image>();
        crImg.sprite = SoulslikeHUDStyler.GetActionButtonRingSprite();
        crImg.color = new Color(0.92f, 0.78f, 0.42f, 0.95f);
        crImg.raycastTarget = false;

        // Icon X bên trong
        GameObject cIconObj = new GameObject("CrossIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cIconObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform ciRt = cIconObj.GetComponent<RectTransform>();
        ciRt.anchorMin = new Vector2(0.5f, 0.5f);
        ciRt.anchorMax = new Vector2(0.5f, 0.5f);
        ciRt.pivot = new Vector2(0.5f, 0.5f);
        ciRt.anchoredPosition = Vector2.zero;
        ciRt.sizeDelta = new Vector2(36f, 36f);
        Image ciImg = cIconObj.GetComponent<Image>();
        ciImg.sprite = WhiteIconLoader.GetSprite("Cross");
        ciImg.color = new Color(1f, 0.95f, 0.88f);
        ciImg.preserveAspect = true;
        ciImg.raycastTarget = false;

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.targetGraphic = cImg;
        closeBtn.onClick.AddListener(CloseUI);

        // Divider
        SoulslikeHUDStyler.AddFiligreeDivider(win.transform, new Vector2(0f, -100f), new Vector2(1600f, 8f));

        // Cards Container - Rộng rãi toàn màn hình
        GameObject cardsObj = new GameObject("CardsContainer", typeof(RectTransform));
        cardsObj.transform.SetParent(win.transform, false);
        RectTransform cdRt = cardsObj.GetComponent<RectTransform>();
        cdRt.anchorMin = new Vector2(0.5f, 0.5f);
        cdRt.anchorMax = new Vector2(0.5f, 0.5f);
        cdRt.pivot = new Vector2(0.5f, 0.5f);
        cdRt.anchoredPosition = new Vector2(0f, 32f);
        cdRt.sizeDelta = new Vector2(1820f, 620f);

        cardsContainer = cardsObj.transform;

        // Bottom Timer
        GameObject tmObj = new GameObject("TimerText", typeof(RectTransform), typeof(CanvasRenderer));
        tmObj.transform.SetParent(win.transform, false);
        RectTransform tmRt = tmObj.GetComponent<RectTransform>();
        tmRt.anchorMin = new Vector2(0.5f, 0f);
        tmRt.anchorMax = new Vector2(0.5f, 0f);
        tmRt.pivot = new Vector2(0.5f, 0f);
        tmRt.anchoredPosition = new Vector2(0f, 118f);
        tmRt.sizeDelta = new Vector2(1500f, 42f);

        timerText = tmObj.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 28f;
        timerText.fontStyle = FontStyles.Bold;
        timerText.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(timerText, 0.30f, Color.black);

        // Big Claim Button at Bottom
        GameObject claimBtnObj = new GameObject("Btn_ClaimDaily", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        claimBtnObj.layer = 5;
        claimBtnObj.transform.SetParent(win.transform, false);
        RectTransform clRt = claimBtnObj.GetComponent<RectTransform>();
        clRt.anchorMin = new Vector2(0.5f, 0f);
        clRt.anchorMax = new Vector2(0.5f, 0f);
        clRt.pivot = new Vector2(0.5f, 0f);
        clRt.anchoredPosition = new Vector2(0f, 18f);
        clRt.sizeDelta = new Vector2(980f, 92f);

        Image clImg = claimBtnObj.GetComponent<Image>();
        clImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        clImg.type = Image.Type.Sliced;

        var clOut = claimBtnObj.AddComponent<Outline>();
        clOut.effectDistance = new Vector2(2f, -2f);

        GameObject clTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        clTxtObj.transform.SetParent(claimBtnObj.transform, false);
        RectTransform cltRt = clTxtObj.GetComponent<RectTransform>();
        cltRt.anchorMin = Vector2.zero;
        cltRt.anchorMax = Vector2.one;
        cltRt.sizeDelta = Vector2.zero;

        claimBtnText = clTxtObj.AddComponent<TextMeshProUGUI>();
        claimBtnText.fontSize = 36f;
        claimBtnText.fontStyle = FontStyles.Bold;
        claimBtnText.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(claimBtnText, 0.30f, Color.black);

        claimButton = claimBtnObj.GetComponent<Button>();
        claimButton.targetGraphic = clImg;
        claimButton.onClick.AddListener(OnClaimToday);

        RefreshCards();
    }

    private void RefreshCards()
    {
        bool isVi = LocalizationManager.IsVietnamese;
        bool canClaim = CanClaimToday();
        int todayIndex = GetTodayDayIndex();
        DateTime monday = GetMondayOfCurrentWeek();
        DateTime logicalToday = GetLogicalNow().Date;

        // Clear children
        for (int i = cardsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(cardsContainer.GetChild(i).gameObject);
        }

        float cardW = 240f;
        float cardH = 590f;
        float spacing = 18f;
        float startX = -((cardW * 6 + 265f + spacing * 6) * 0.5f) + cardW * 0.5f;

        for (int i = 0; i < 7; i++)
        {
            DateTime cardDate = monday.AddDays(i);
            string cardKey = "DailyClaimed_" + cardDate.ToString("yyyyMMdd");
            bool isClaimed = PlayerPrefs.GetInt(cardKey, 0) == 1;

            bool isDay7 = (i == 6);
            bool isPastDay = cardDate.Date < logicalToday;
            bool isToday = (i == todayIndex);
            bool isFutureDay = cardDate.Date > logicalToday;

            bool isMissed = isPastDay && !isClaimed;
            bool isAvailableToday = isToday && canClaim;

            float curW = isDay7 ? 265f : cardW;
            float curH = isDay7 ? 610f : cardH;
            float posX = startX + i * (cardW + spacing) + (isDay7 ? 10f : 0f);

            GameObject card = new GameObject("Card_Day" + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(cardsContainer, false);

            RectTransform cRt = card.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(posX, 0f);
            cRt.sizeDelta = new Vector2(curW, curH);

            Image cardImg = card.GetComponent<Image>();
            cardImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
            cardImg.type = Image.Type.Sliced;

            var outline = card.AddComponent<Outline>();

            // Visual Styling dựa trên trạng thái
            if (isAvailableToday)
            {
                cardImg.color = new Color(0.08f, 0.18f, 0.24f, 0.98f);
                outline.effectColor = new Color(0.35f, 0.95f, 0.85f, 0.95f);
                outline.effectDistance = new Vector2(2f, -2f);
                card.transform.localScale = Vector3.one * 1.03f;
            }
            else if (isClaimed)
            {
                cardImg.color = new Color(0.04f, 0.07f, 0.05f, 0.88f);
                outline.effectColor = new Color(0.20f, 0.55f, 0.35f, 0.65f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
            else if (isMissed)
            {
                cardImg.color = new Color(0.10f, 0.04f, 0.05f, 0.80f);
                outline.effectColor = new Color(0.55f, 0.18f, 0.20f, 0.50f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
            else if (isDay7)
            {
                // Thẻ Chủ Nhật - Hoàng Kim Tối Thượng
                cardImg.color = new Color(0.18f, 0.12f, 0.04f, 0.98f);
                outline.effectColor = new Color(1f, 0.85f, 0.30f, 0.95f);
                outline.effectDistance = new Vector2(2.5f, -2.5f);
            }
            else // Future day bình thường
            {
                cardImg.color = new Color(0.05f, 0.06f, 0.09f, 0.88f);
                outline.effectColor = new Color(0.25f, 0.32f, 0.45f, 0.50f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            // 1. Top Header: THỨ 2..CHỦ NHẬT (N1..N7)
            GameObject topObj = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer));
            topObj.transform.SetParent(card.transform, false);
            RectTransform thRt = topObj.GetComponent<RectTransform>();
            thRt.anchorMin = new Vector2(0.5f, 1f);
            thRt.anchorMax = new Vector2(0.5f, 1f);
            thRt.pivot = new Vector2(0.5f, 1f);
            thRt.anchoredPosition = new Vector2(0f, -14f);
            thRt.sizeDelta = new Vector2(curW - 10f, 44f);

            var thTmp = topObj.AddComponent<TextMeshProUGUI>();
            string dayLabel = isVi ? DAY_NAMES_VI[i] : DAY_NAMES_EN[i];
            thTmp.text = $"<b>{dayLabel} (N{i + 1})</b>";
            thTmp.fontSize = isDay7 ? 30f : 27f;
            thTmp.fontStyle = FontStyles.Bold;
            thTmp.alignment = TextAlignmentOptions.Center;
            thTmp.color = isDay7 ? new Color(1f, 0.90f, 0.45f) : (isAvailableToday ? new Color(0.4f, 0.95f, 1f) : new Color(0.85f, 0.90f, 0.98f));
            SoulslikeHUDStyler.SetSafeOutline(thTmp, 0.30f, Color.black);

            // 2. Thẻ Card Nghệ Thuật Digital Art TCG Đẳng Cấp Thần Thoại Cho 7 Ngày
            Sprite rewardSpr = null;
            switch (i)
            {
                case 0:
                    rewardSpr = Resources.Load<Sprite>("UI/CardDeities/CardDeity_BonusHealth");
                    break;
                case 1:
                    rewardSpr = Resources.Load<Sprite>("UI/CardDeities/CardDeity_CelestialCascade");
                    break;
                case 2:
                    rewardSpr = Resources.Load<Sprite>("UI/CardDeities/CardDeity_DivineAegis");
                    break;
                case 3:
                    rewardSpr = Resources.Load<Sprite>("UI/CardDeities/CardDeity_ChronoStasis");
                    break;
                case 4:
                    rewardSpr = Resources.Load<Sprite>("UI/CardDeities/CardDeity_GoldenSanctuary");
                    break;
                case 5:
                    rewardSpr = Resources.Load<Sprite>("UI/CardDeities/CardDeity_GodforgedArsenal");
                    break;
                case 6: // Ngày 7 Chủ Nhật: Vương Miện Hoàng Đế Bất Diệt
                    rewardSpr = Resources.Load<Sprite>("UI/CardDeities/CardDeity_UndyingSovereign");
                    break;
            }
            if (rewardSpr == null) rewardSpr = Resources.Load<Sprite>("UI/CardDeities/CardDeity_AscendedDivinity");

            // Khung Icon Slot Thẻ Bài TCG Nghệ Thuật - Siêu To Lớn & Sắc Nét
            GameObject iconBoxObj = new GameObject("RewardSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconBoxObj.transform.SetParent(card.transform, false);
            RectTransform ibRt = iconBoxObj.GetComponent<RectTransform>();
            ibRt.anchorMin = new Vector2(0.5f, 0.5f);
            ibRt.anchorMax = new Vector2(0.5f, 0.5f);
            ibRt.pivot = new Vector2(0.5f, 0.5f);
            ibRt.anchoredPosition = new Vector2(0f, 75f);
            float ibSize = isDay7 ? 190f : 172f;
            ibRt.sizeDelta = new Vector2(ibSize, ibSize);

            Image ibImg = iconBoxObj.GetComponent<Image>();
            Sprite slotSpr = SoulslikeHUDStyler.GetPotionSlotFrameSprite();
            if (slotSpr != null) ibImg.sprite = slotSpr;
            ibImg.color = isDay7 ? new Color(1f, 0.88f, 0.40f, 1f) : (isAvailableToday ? new Color(0.35f, 0.95f, 0.85f, 0.95f) : new Color(0.65f, 0.72f, 0.85f, 0.80f));

            // Tranh Nghệ Thuật bên trong Slot
            GameObject icObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icObj.transform.SetParent(iconBoxObj.transform, false);
            RectTransform icRt = icObj.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0.5f, 0.5f);
            icRt.anchorMax = new Vector2(0.5f, 0.5f);
            icRt.pivot = new Vector2(0.5f, 0.5f);
            icRt.anchoredPosition = Vector2.zero;
            float icSize = isDay7 ? 172f : 154f;
            icRt.sizeDelta = new Vector2(icSize, icSize);

            Image icImg = icObj.GetComponent<Image>();
            icImg.raycastTarget = false;
            icImg.preserveAspect = true;
            if (rewardSpr != null) icImg.sprite = rewardSpr;
            icImg.color = isMissed ? new Color(0.45f, 0.45f, 0.45f, 0.60f) : Color.white;

            // 3. Số Lượng Linh Hồn ở Giữa (To Rõ Nhìn Xa Vẫn Đọc Được Cực Nét)
            GameObject rwObj = new GameObject("RewardText", typeof(RectTransform), typeof(CanvasRenderer));
            rwObj.transform.SetParent(card.transform, false);
            RectTransform rwRt = rwObj.GetComponent<RectTransform>();
            rwRt.anchorMin = new Vector2(0.5f, 0.5f);
            rwRt.anchorMax = new Vector2(0.5f, 0.5f);
            rwRt.pivot = new Vector2(0.5f, 0.5f);
            rwRt.anchoredPosition = new Vector2(0f, -48f);
            rwRt.sizeDelta = new Vector2(curW - 10f, 96f);

            var rwTmp = rwObj.AddComponent<TextMeshProUGUI>();
            rwTmp.text = $"<b>+{REWARDS[i]:N0}</b>\n<size=26>{(isVi ? "LINH HỒN" : "SOULS")}</size>";
            rwTmp.fontSize = isDay7 ? 40f : 34f;
            rwTmp.fontStyle = FontStyles.Bold;
            rwTmp.alignment = TextAlignmentOptions.Center;
            rwTmp.color = isDay7 ? new Color(1f, 0.88f, 0.35f) : (isMissed ? new Color(0.6f, 0.5f, 0.5f) : Color.white);
            SoulslikeHUDStyler.SetSafeOutline(rwTmp, 0.30f, Color.black);

            // 4. Huy Hiệu Trạng Thái ở Chân Thẻ (Đầy Đặn, Rõ Nét, Triệt Tiêu Lỗi z)
            GameObject stObj = new GameObject("StatusBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            stObj.transform.SetParent(card.transform, false);
            RectTransform stRt = stObj.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.5f, 0f);
            stRt.anchorMax = new Vector2(0.5f, 0f);
            stRt.pivot = new Vector2(0.5f, 0f);
            stRt.anchoredPosition = new Vector2(0f, 18f);
            stRt.sizeDelta = new Vector2(curW - 14f, 64f);

            Image stImg = stObj.GetComponent<Image>();
            Sprite stBgSpr = SoulslikeHUDStyler.GetStoneButtonSprite();
            if (stBgSpr != null) stImg.sprite = stBgSpr;
            stImg.type = Image.Type.Sliced;

            GameObject stTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            stTxtObj.transform.SetParent(stObj.transform, false);
            RectTransform sttRt = stTxtObj.GetComponent<RectTransform>();
            sttRt.anchorMin = Vector2.zero;
            sttRt.anchorMax = Vector2.one;
            sttRt.sizeDelta = Vector2.zero;

            var stTmp = stTxtObj.AddComponent<TextMeshProUGUI>();
            stTmp.fontSize = 26f;
            stTmp.fontStyle = FontStyles.Bold;
            stTmp.alignment = TextAlignmentOptions.Center;
            SoulslikeHUDStyler.SetSafeOutline(stTmp, 0.30f, Color.black);

            if (isClaimed)
            {
                stImg.color = new Color(0.12f, 0.28f, 0.18f, 0.90f);
                stTmp.text = isVi ? "<color=#2ECC71><b>✔ ĐÃ NHẬN</b></color>" : "<color=#2ECC71><b>✔ CLAIMED</b></color>";
            }
            else if (isMissed)
            {
                stImg.color = new Color(0.24f, 0.10f, 0.12f, 0.80f);
                stTmp.text = isVi ? "<color=#E74C3C><b>BỎ LỠ</b></color>" : "<color=#E74C3C><b>MISSED</b></color>";
            }
            else if (isAvailableToday)
            {
                stImg.color = new Color(0.12f, 0.40f, 0.35f, 0.95f);
                stTmp.text = isVi ? "<color=#55EFC4><b>NHẬN NGAY</b></color>" : "<color=#55EFC4><b>CLAIM NOW</b></color>";
            }
            else
            {
                stImg.color = new Color(0.10f, 0.12f, 0.16f, 0.85f);
                stTmp.text = isVi ? "<color=#ECEFF1><b>CHƯA ĐẾN</b></color>" : "<color=#ECEFF1><b>LOCKED</b></color>";
            }
        }

        // 5. Cập Nhật Nút Nhận Lớn ở Đáy
        Image bImg = claimButton.GetComponent<Image>();
        Outline bOut = claimButton.GetComponent<Outline>();

        if (canClaim)
        {
            claimButton.interactable = true;
            bImg.color = new Color(0.12f, 0.45f, 0.28f, 0.98f);
            bOut.effectColor = new Color(1f, 0.88f, 0.45f, 0.95f);
            string todayName = isVi ? DAY_NAMES_VI[todayIndex] : DAY_NAMES_EN[todayIndex];
            claimBtnText.text = isVi 
                ? $"<b>ĐIỂM DANH {todayName} (+{REWARDS[todayIndex]:N0} LINH HỒN)</b>"
                : $"<b>CLAIM {todayName} (+{REWARDS[todayIndex]:N0} SOULS)</b>";
            claimBtnText.color = new Color(1f, 0.95f, 0.70f);
            SoulslikeHUDStyler.SetSafeOutline(claimBtnText, 0.35f, Color.black);
        }
        else
        {
            claimButton.interactable = false;
            bImg.color = new Color(0.10f, 0.12f, 0.16f, 0.92f);
            bOut.effectColor = new Color(0.35f, 0.40f, 0.50f, 0.60f);
            claimBtnText.text = isVi 
                ? "<b>HÔM NAY ĐÃ ĐIỂM DANH (LÀM MỚI LÚC 07:00 SÁNG)</b>"
                : "<b>CLAIMED TODAY (RESETS AT 07:00 AM)</b>";
            claimBtnText.color = new Color(0.65f, 0.72f, 0.82f);
        }
    }

    private void OnClaimToday()
    {
        if (!CanClaimToday()) return;

        int todayIndex = GetTodayDayIndex();
        int reward = REWARDS[todayIndex];

        // 1. Add Shards to account
        AccountDatabase.AddAethelShards(reward);

        // 2. Mark today as claimed
        string todayKey = "DailyClaimed_" + GetTodayKey();
        PlayerPrefs.SetInt(todayKey, 1);
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
