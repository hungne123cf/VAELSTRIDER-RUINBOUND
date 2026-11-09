using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Knight's Mailbox & Notification Hub
/// Giao diện Hòm Thư & Thông Báo chuẩn phong cách Liên Quân Mobile (Arena of Valor) & Dark Fantasy:
/// - Hệ thống 3 Tab danh mục: [THƯ & QUÀ TẶNG] - [THÔNG BÁO HỆ THỐNG] - [MÃ QUÀ TẶNG]
/// - Thiết kế thẻ thư cao cấp (Mail Cards) viền kim đồng chạm khắc runic, wax seal, và red dot notification
/// - Nút "NHẬN TẤT CẢ" (Claim All) ánh kim hoàng gia 1-chạm thu thập toàn bộ linh hồn
/// - Nút "XÓA ĐÃ ĐỌC" (Delete Read) dọn dẹp hòm thư gọn gàng
/// - Khay hiển thị vật phẩm đính kèm (Attached Reward Tray) với khung ngọc Runic Frame
/// - Trạng thái Hòm thư trống (Empty State) với huy hiệu chìm và nút khôi phục thư mẫu
/// - Khu vực Đổi Giftcode chuyên biệt với thẻ mã có sẵn 1-chạm không cần gõ phím
/// </summary>
public class MailboxUI : MonoBehaviour
{
    public static MailboxUI Instance { get; private set; }

    public enum MailTab
    {
        MailsAndGifts = 0,
        SystemNotices = 1,
        GiftcodeHub = 2
    }

    [Serializable]
    public class MailItemData
    {
        public string id;
        public string titleVi;
        public string titleEn;
        public string senderVi;
        public string senderEn;
        public string contentVi;
        public string contentEn;
        public int rewardShards;
        public string date;
        public bool isSystemNotice;
        public string categoryTag;
    }

    private static List<MailItemData> _mails = new List<MailItemData>();
    private static List<MailItemData> _notices = new List<MailItemData>();

    private System.Action onCloseCallback;
    private MailItemData selectedMail;
    private MailTab currentTab = MailTab.MailsAndGifts;

    // Window & Tab Views
    private RectTransform modalWindowRect;
    private GameObject tabContentMails;
    private GameObject tabContentNotices;
    private GameObject tabContentGiftcode;

    // Tab Buttons & Indicators
    private Image tabBtnMailsBg;
    private TextMeshProUGUI tabBtnMailsText;
    private GameObject tabMailsRedDot;

    private Image tabBtnNoticesBg;
    private TextMeshProUGUI tabBtnNoticesText;

    private Image tabBtnGiftcodeBg;
    private TextMeshProUGUI tabBtnGiftcodeText;

    // Tab 1: Mail List & Detail UI
    private Transform mailListContent;
    private GameObject mailEmptyStateObj;
    private GameObject mailDetailPanel;
    private TextMeshProUGUI detailTitleText;
    private TextMeshProUGUI detailSenderText;
    private TextMeshProUGUI detailDateText;
    private TextMeshProUGUI detailBodyText;
    private GameObject detailRewardBox;
    private TextMeshProUGUI detailRewardValText;
    private Button detailClaimButton;
    private TextMeshProUGUI detailClaimBtnText;
    private Button detailDeleteButton;
    private TextMeshProUGUI mailCounterText;

    // Tab 2: System Notices UI
    private Transform noticeListContent;
    private TextMeshProUGUI noticeDetailTitleText;
    private TextMeshProUGUI noticeDetailMetaText;
    private TextMeshProUGUI noticeDetailBodyText;
    private MailItemData selectedNotice;

    // Tab 3: Giftcode Hub UI
    private TMP_InputField giftcodeInput;
    private TouchScreenKeyboard mobileKeyboard;
    private TextMeshProUGUI giftcodeFeedbackText;
    private GameObject virtualKeyboardObj;
    private TextMeshProUGUI keyboardPreviewText;
    private Transform publicCodesContainer;

    // =========================================================================
    // INITIALIZATION & DATA
    // =========================================================================

    public static void InitializeDefaultMails()
    {
        _mails.Clear();
        _notices.Clear();

        // 1. Thư Tân Thủ: 5,000 Linh Hồn
        _mails.Add(new MailItemData
        {
            id = "mail_welcome_5000",
            titleVi = "[QUÀ TÂN THỦ] LỜI HIỆU TRIỆU TỪ VỰC THẲM",
            titleEn = "[NEWBIE GIFT] CALL OF THE RUINS",
            senderVi = "Hội Đồng Vaelstrider",
            senderEn = "Council of Vaelstrider",
            contentVi = "Chào mừng dũng sĩ đã đặt chân đến thế giới Vaelstrider: Ruinbound!\n\nĐể tiếp thêm sinh lực và khai mở sức mạnh ban đầu, Hội Đồng xin gửi tặng bạn 5,000 Mảnh Linh Hồn. Hãy dùng chúng để chiêu mộ chiến binh, mở khóa kỹ năng và sẵn sàng cho những trận chiến sinh tử nơi Vực Thẳm.\n\nChúc hiệp sĩ bách chiến bách thắng!",
            contentEn = "Welcome, brave warrior, to the realm of Vaelstrider: Ruinbound!\n\nTo empower your initial journey, the Council grants you 5,000 Soul Shards. Use them to recruit heroes, unlock perks, and conquer the depths.\n\nFight with honor!",
            rewardShards = 5000,
            date = "04/11/2026",
            categoryTag = "QUÀ TÂN THỦ"
        });

        // 2. Thư Cập Nhật Hệ Thống v1.3
        _mails.Add(new MailItemData
        {
            id = "mail_patch_1_3",
            titleVi = "[CẬP NHẬT v1.3] ĐIỀU KHIỂN & HÒM THƯ HOÀNG GIA",
            titleEn = "[UPDATE v1.3] CONTROLS & ROYAL MAILBOX",
            senderVi = "Đội Ngũ Phát Triển",
            senderEn = "Dev Team",
            contentVi = "Bản cập nhật v1.3 chính thức ra mắt:\n- Giao diện Hòm Thư & Thông Báo chuẩn phong cách Liên Quân Mobile.\n- Hệ thống Tùy chỉnh vị trí nút cảm ứng trực tiếp trong trận đấu.\n- Vòng quay và Điểm danh nhận quà mỗi 07:00 sáng hàng ngày.\n- Tối ưu hóa hiệu năng và hiệu ứng chuyển cảnh Zero-Bug.\n\nXin gửi tặng bạn 1,000 Linh Hồn tri ân!",
            contentEn = "Update v1.3 introduces:\n- High-end Liên Quân style Royal Mailbox & Notification Hub.\n- Live in-match touch controls positioning.\n- 07:00 AM Daily Attendance and rewards.\n- Zero-Bug realm transitions.\n\nEnjoy 1,000 Soul Shards as our gratitude!",
            rewardShards = 1000,
            date = "05/11/2026",
            categoryTag = "CẬP NHẬT"
        });

        // 3. Quà Khai Mở Đấu Trường
        _mails.Add(new MailItemData
        {
            id = "mail_arena_open",
            titleVi = "[SỰ KIỆN] KHAI MỞ ĐẤU TRƯỜNG VỰC THẲM",
            titleEn = "[EVENT] ABYSSAL ARENA OPENING",
            senderVi = "Đại Tướng Quân Vực Thẳm",
            senderEn = "Abyssal Commander",
            contentVi = "Hỡi các hiệp sĩ, Đấu Trường Sinh Tử chính thức mở cửa nghênh chiến!\n\nTham gia các trận huyết chiến, tích lũy điểm chiến công để đổi lấy trang bị và danh hiệu truyền thuyết. Mọi chiến binh vượt qua thử thách đều được khắc tên vào Bảng Vàng Danh Dự.\n\nHãy chuẩn bị lưỡi kiếm sắc bén nhất!",
            contentEn = "Warriors, the Abyssal Arena has opened its gates!\n\nFight in brutal battles, earn glory, and carve your name into the Kingdom's Hall of Fame.\n\nSharpen your blades and claim your destiny!",
            rewardShards = 1500,
            date = "06/11/2026",
            categoryTag = "SỰ KIỆN"
        });

        // 4. Tri Ân Chiến Binh Tiên Phong
        _mails.Add(new MailItemData
        {
            id = "mail_vanguard_hero",
            titleVi = "[TRI ÂN] GÓI QUÀ CHIẾN BINH TIÊN PHONG",
            titleEn = "[TRIBUTE] VANGUARD WARRIOR BUNDLE",
            senderVi = "Hội Đồng Vaelstrider",
            senderEn = "Council of Vaelstrider",
            contentVi = "Hội Đồng Vaelstrider xin gửi lời cảm ơn sâu sắc nhất tới toàn thể người chơi đã nhiệt tình trải nghiệm, thử thách kỹ năng và gửi phản hồi quý giá giúp hoàn thiện tựa game.\n\nPhần thưởng 2,000 Linh Hồn Aethel được trao tặng đến bạn kèm bức thư này.",
            contentEn = "The Council expresses deep gratitude to all players testing and giving feedback.\n\nAttached is 2,000 Aethel Soul Shards as our special tribute.",
            rewardShards = 2000,
            date = "07/11/2026",
            categoryTag = "TRI ÂN"
        });

        // --- CÁC THÔNG BÁO HỆ THỐNG (Tab 2) ---
        _notices.Add(new MailItemData
        {
            id = "notice_rules",
            titleVi = "[QUY TẮC] BẢO VỆ TÀI KHOẢN VÀ CHỐNG GIAN LẬN",
            titleEn = "[RULES] ACCOUNT SAFETY & FAIR PLAY",
            senderVi = "Hệ Thống Giám Sát",
            senderEn = "Security System",
            contentVi = "Nhằm xây dựng một môi trường đối kháng công bằng, minh bạch và lành mạnh cho toàn thể game thủ:\n\n1. Nghiêm cấm sử dụng các phần mềm can thiệp bộ nhớ hoặc mod trái phép.\n2. Mọi hành vi gian lận dữ liệu linh hồn sẽ dẫn đến khóa tài khoản vĩnh viễn.\n3. Luôn đăng nhập trên thiết bị an toàn và không chia sẻ tài khoản.\n\nCảm ơn bạn đã đồng hành giữ gìn danh dự chiến binh!",
            contentEn = "To ensure a fair and balanced battlefield for all warriors:\n\n1. Third-party mods and memory alterations are strictly prohibited.\n2. Exploiting currencies will result in permanent ban.\n3. Keep your account secure at all times.\n\nThank you for upholding the warrior's honor!",
            rewardShards = 0,
            date = "08/11/2026",
            isSystemNotice = true,
            categoryTag = "QUY TẮC"
        });

        _notices.Add(new MailItemData
        {
            id = "notice_schedule",
            titleVi = "[LỊCH TRÌNH] BẢO TRÌ & LÀM MỚI MỖI 07:00 SÁNG",
            titleEn = "[SCHEDULE] DAILY RESET AT 07:00 AM",
            senderVi = "Ban Quản Trị Vương Quốc",
            senderEn = "Realm Administration",
            contentVi = "Thông báo đến toàn thể dũng sĩ:\n\n- Chu kỳ Điểm danh 7 ngày và Phần thưởng đăng nhập được làm mới chính xác vào 07:00 AM mỗi ngày.\n- Thư có đính kèm phần thưởng sẽ được lưu giữ tối đa 30 ngày kể từ ngày gửi.\n- Vui lòng kiểm tra và nhận thưởng trước khi hết hạn để tránh mất vật phẩm quý giá.\n\nChúc hiệp sĩ một ngày chiến đấu thắng lợi!",
            contentEn = "Notice to all warriors:\n\n- Weekly Attendance and Daily Rewards reset strictly at 07:00 AM daily.\n- Mails with attached rewards expire after 30 days.\n- Claim rewards promptly before expiration.\n\nMay victory follow your blade!",
            rewardShards = 0,
            date = "08/11/2026",
            isSystemNotice = true,
            categoryTag = "LỊCH TRÌNH"
        });
    }

    public static bool IsMailDeleted(string id)
    {
        return PlayerPrefs.GetInt("Mail_Deleted_" + id, 0) == 1;
    }

    public static bool HasUnclaimedMail()
    {
        if (_mails.Count == 0) InitializeDefaultMails();

        foreach (var m in _mails)
        {
            if (IsMailDeleted(m.id)) continue;
            if (m.rewardShards > 0 && PlayerPrefs.GetInt("Mail_Claimed_" + m.id, 0) == 0)
            {
                return true;
            }
        }
        return false;
    }

    public static void RestoreDefaultMails()
    {
        if (_mails.Count == 0) InitializeDefaultMails();

        foreach (var m in _mails)
        {
            PlayerPrefs.DeleteKey("Mail_Deleted_" + m.id);
        }
        PlayerPrefs.Save();
    }

    // =========================================================================
    // ENTRY POINT & MODAL CREATION
    // =========================================================================

    public static void OpenMailbox(System.Action onClose = null)
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

        GameObject mbObj = new GameObject("MailboxUI", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        mbObj.layer = 5;
        mbObj.transform.SetParent(rootCanvas.transform, false);

        RectTransform rt = mbObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = mbObj.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f);
        bg.raycastTarget = true;

        Instance = mbObj.AddComponent<MailboxUI>();
        Instance.onCloseCallback = onClose;
        Instance.BuildUI();
    }

    // =========================================================================
    // UI BUILDER
    // =========================================================================

    private void BuildUI()
    {
        if (_mails.Count == 0) InitializeDefaultMails();
        bool isVi = LocalizationManager.IsVietnamese;

        // Auto-restore default mails if all were deleted in past tests, so user is never faced with empty void!
        int visibleMailCount = 0;
        foreach (var m in _mails)
        {
            if (!IsMailDeleted(m.id)) visibleMailCount++;
        }
        if (visibleMailCount == 0)
        {
            RestoreDefaultMails();
        }

        // --- MODAL WINDOW (1160 x 640px) ---
        GameObject win = new GameObject("Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        win.layer = 5;
        win.transform.SetParent(transform, false);

        RectTransform wRt = win.GetComponent<RectTransform>();
        modalWindowRect = wRt;
        wRt.anchorMin = new Vector2(0.5f, 0.5f);
        wRt.anchorMax = new Vector2(0.5f, 0.5f);
        wRt.pivot = new Vector2(0.5f, 0.5f);
        wRt.anchoredPosition = Vector2.zero;
        wRt.sizeDelta = new Vector2(1160f, 640f);

        Image winImg = win.GetComponent<Image>();
        winImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        winImg.type = Image.Type.Sliced;
        winImg.color = new Color(0.04f, 0.05f, 0.09f, 0.98f);

        // Ornate Royal Gilded Double Outline
        var winOut = win.AddComponent<Outline>();
        winOut.effectColor = new Color(0.85f, 0.70f, 0.32f, 0.95f);
        winOut.effectDistance = new Vector2(2f, -2f);

        // --- TOP NAVIGATION HEADER (Height 70px) ---
        BuildTopHeader(win.transform);

        // Filigree Divider under Header
        SoulslikeHUDStyler.AddFiligreeDivider(win.transform, new Vector2(0f, -70f), new Vector2(1120f, 6f));

        // --- TAB 1 CONTAINER: THƯ & QUÀ TẶNG ---
        tabContentMails = new GameObject("TabContent_Mails", typeof(RectTransform));
        tabContentMails.transform.SetParent(win.transform, false);
        RectTransform t1Rt = tabContentMails.GetComponent<RectTransform>();
        t1Rt.anchorMin = Vector2.zero;
        t1Rt.anchorMax = Vector2.one;
        t1Rt.offsetMin = new Vector2(20f, 15f);
        t1Rt.offsetMax = new Vector2(-20f, -76f);

        BuildMailsTab(tabContentMails.transform);

        // --- TAB 2 CONTAINER: THÔNG BÁO HỆ THỐNG ---
        tabContentNotices = new GameObject("TabContent_Notices", typeof(RectTransform));
        tabContentNotices.transform.SetParent(win.transform, false);
        RectTransform t2Rt = tabContentNotices.GetComponent<RectTransform>();
        t2Rt.anchorMin = Vector2.zero;
        t2Rt.anchorMax = Vector2.one;
        t2Rt.offsetMin = new Vector2(20f, 15f);
        t2Rt.offsetMax = new Vector2(-20f, -76f);

        BuildNoticesTab(tabContentNotices.transform);

        // --- TAB 3 CONTAINER: MÃ QUÀ TẶNG (GIFTCODE HUB) ---
        tabContentGiftcode = new GameObject("TabContent_Giftcode", typeof(RectTransform));
        tabContentGiftcode.transform.SetParent(win.transform, false);
        RectTransform t3Rt = tabContentGiftcode.GetComponent<RectTransform>();
        t3Rt.anchorMin = Vector2.zero;
        t3Rt.anchorMax = Vector2.one;
        t3Rt.offsetMin = new Vector2(20f, 15f);
        t3Rt.offsetMax = new Vector2(-20f, -76f);

        BuildGiftcodeTab(tabContentGiftcode.transform);

        // Set Default Tab
        SwitchTab(MailTab.MailsAndGifts);

        // Build Phone Virtual Keyboard
        BuildVirtualKeyboard(transform);
    }

    // =========================================================================
    // TOP HEADER & NAVIGATION TABS
    // =========================================================================

    private void BuildTopHeader(Transform parent)
    {
        bool isVi = LocalizationManager.IsVietnamese;

        // Header Root
        GameObject headObj = new GameObject("TopHeader", typeof(RectTransform));
        headObj.transform.SetParent(parent, false);
        RectTransform hRt = headObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = Vector2.zero;
        hRt.sizeDelta = new Vector2(0f, 70f);

        // Title on the Left: Crest Icon + Text
        GameObject titleGroup = new GameObject("TitleGroup", typeof(RectTransform));
        titleGroup.transform.SetParent(headObj.transform, false);
        RectTransform tgRt = titleGroup.GetComponent<RectTransform>();
        tgRt.anchorMin = new Vector2(0f, 0.5f);
        tgRt.anchorMax = new Vector2(0f, 0.5f);
        tgRt.pivot = new Vector2(0f, 0.5f);
        tgRt.anchoredPosition = new Vector2(24f, 0f);
        tgRt.sizeDelta = new Vector2(300f, 50f);

        // Royal Mail Crest Icon
        GameObject iconObj = new GameObject("CrestIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(titleGroup.transform, false);
        RectTransform iRt = iconObj.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0f, 0.5f);
        iRt.anchorMax = new Vector2(0f, 0.5f);
        iRt.pivot = new Vector2(0f, 0.5f);
        iRt.anchoredPosition = new Vector2(0f, 0f);
        iRt.sizeDelta = new Vector2(38f, 38f);

        Image iImg = iconObj.GetComponent<Image>();
        Sprite mailIcon = Resources.Load<Sprite>("UI/icon_mail");
        if (mailIcon == null) mailIcon = SoulslikeHUDStyler.GetAmberFillSprite();
        iImg.sprite = mailIcon;
        iImg.color = new Color(1f, 0.88f, 0.45f);

        // Title Text
        GameObject tTxtObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer));
        tTxtObj.transform.SetParent(titleGroup.transform, false);
        RectTransform ttRt = tTxtObj.GetComponent<RectTransform>();
        ttRt.anchorMin = new Vector2(0f, 0f);
        ttRt.anchorMax = new Vector2(1f, 1f);
        ttRt.offsetMin = new Vector2(46f, 0f);
        ttRt.offsetMax = Vector2.zero;

        var ttTmp = tTxtObj.AddComponent<TextMeshProUGUI>();
        ttTmp.text = isVi ? "<b>HÒM THƯ HOÀNG GIA</b>" : "<b>ROYAL MAILBOX</b>";
        ttTmp.fontSize = 20f;
        ttTmp.color = new Color(1f, 0.88f, 0.45f);
        ttTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Navigation Tabs in Center: [THƯ & QUÀ TẶNG] [THÔNG BÁO] [MÃ QUÀ TẶNG]
        GameObject tabsGroup = new GameObject("TabsGroup", typeof(RectTransform));
        tabsGroup.transform.SetParent(headObj.transform, false);
        RectTransform tbRt = tabsGroup.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0.5f, 0.5f);
        tbRt.anchorMax = new Vector2(0.5f, 0.5f);
        tbRt.pivot = new Vector2(0.5f, 0.5f);
        tbRt.anchoredPosition = new Vector2(40f, 0f);
        tbRt.sizeDelta = new Vector2(580f, 46f);

        // Tab 1: THƯ & QUÀ TẶNG
        tabBtnMailsBg = CreateTabButton(tabsGroup.transform, "Tab_Mails", 
            isVi ? "<b>THƯ & QUÀ TẶNG</b>" : "<b>MAILS & GIFTS</b>", 
            new Vector2(-190f, 0f), new Vector2(180f, 44f), 
            () => SwitchTab(MailTab.MailsAndGifts), out tabBtnMailsText, out tabMailsRedDot);

        // Tab 2: THÔNG BÁO HỆ THỐNG
        tabBtnNoticesBg = CreateTabButton(tabsGroup.transform, "Tab_Notices", 
            isVi ? "<b>THÔNG BÁO</b>" : "<b>NOTICES</b>", 
            new Vector2(0f, 0f), new Vector2(180f, 44f), 
            () => SwitchTab(MailTab.SystemNotices), out tabBtnNoticesText, out _);

        // Tab 3: MÃ QUÀ TẶNG
        tabBtnGiftcodeBg = CreateTabButton(tabsGroup.transform, "Tab_Giftcode", 
            isVi ? "<b>MÃ QUÀ TẶNG</b>" : "<b>GIFTCODES</b>", 
            new Vector2(190f, 0f), new Vector2(180f, 44f), 
            () => SwitchTab(MailTab.GiftcodeHub), out tabBtnGiftcodeText, out _);

        // Royal Runic Close Button [✕] at Top Right
        GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtnObj.layer = 5;
        closeBtnObj.transform.SetParent(headObj.transform, false);
        RectTransform cRt = closeBtnObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 0.5f);
        cRt.anchorMax = new Vector2(1f, 0.5f);
        cRt.pivot = new Vector2(1f, 0.5f);
        cRt.anchoredPosition = new Vector2(-22f, 0f);
        cRt.sizeDelta = new Vector2(48f, 48f);

        Image cImg = closeBtnObj.GetComponent<Image>();
        cImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        cImg.type = Image.Type.Sliced;
        cImg.color = new Color(0.35f, 0.12f, 0.14f, 0.95f);

        var cOut = closeBtnObj.AddComponent<Outline>();
        cOut.effectColor = new Color(0.95f, 0.40f, 0.40f, 0.85f);
        cOut.effectDistance = new Vector2(1.5f, -1.5f);

        // Circular Runic Ring over close button
        GameObject ringObj = new GameObject("Ring", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ringObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform rRt = ringObj.GetComponent<RectTransform>();
        rRt.anchorMin = Vector2.zero;
        rRt.anchorMax = Vector2.one;
        rRt.sizeDelta = Vector2.zero;
        Image rImg = ringObj.GetComponent<Image>();
        rImg.sprite = SoulslikeHUDStyler.GetActionButtonRingSprite();
        rImg.color = new Color(1f, 0.85f, 0.45f, 0.75f);
        rImg.raycastTarget = false;

        // Golden Cross [✕]
        GameObject xTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        xTxtObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform xtRt = xTxtObj.GetComponent<RectTransform>();
        xtRt.anchorMin = Vector2.zero;
        xtRt.anchorMax = Vector2.one;
        xtRt.sizeDelta = Vector2.zero;
        var xtTmp = xTxtObj.AddComponent<TextMeshProUGUI>();
        xtTmp.text = "<b>✕</b>";
        xtTmp.fontSize = 20f;
        xtTmp.alignment = TextAlignmentOptions.Center;
        xtTmp.color = new Color(1f, 0.92f, 0.70f);

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.targetGraphic = cImg;
        closeBtn.onClick.AddListener(CloseMailbox);
    }

    private Image CreateTabButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action, out TextMeshProUGUI textComp, out GameObject redDotObj)
    {
        GameObject tabObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        tabObj.transform.SetParent(parent, false);

        RectTransform tRt = tabObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 0.5f);
        tRt.anchorMax = new Vector2(0.5f, 0.5f);
        tRt.pivot = new Vector2(0.5f, 0.5f);
        tRt.anchoredPosition = pos;
        tRt.sizeDelta = size;

        Image tabImg = tabObj.GetComponent<Image>();
        tabImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        tabImg.type = Image.Type.Sliced;
        tabImg.color = new Color(0.10f, 0.13f, 0.18f, 0.95f);

        var tabOut = tabObj.AddComponent<Outline>();
        tabOut.effectColor = new Color(0.40f, 0.50f, 0.65f, 0.50f);
        tabOut.effectDistance = new Vector2(1f, -1f);

        GameObject tTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        tTxtObj.transform.SetParent(tabObj.transform, false);
        RectTransform ttRt = tTxtObj.GetComponent<RectTransform>();
        ttRt.anchorMin = Vector2.zero;
        ttRt.anchorMax = Vector2.one;
        ttRt.sizeDelta = Vector2.zero;

        textComp = tTxtObj.AddComponent<TextMeshProUGUI>();
        textComp.text = label;
        textComp.fontSize = 13.5f;
        textComp.alignment = TextAlignmentOptions.Center;
        textComp.color = new Color(0.75f, 0.82f, 0.92f);

        // Red notification dot
        GameObject rdObj = new GameObject("RedDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rdObj.transform.SetParent(tabObj.transform, false);
        RectTransform rdRt = rdObj.GetComponent<RectTransform>();
        rdRt.anchorMin = new Vector2(1f, 1f);
        rdRt.anchorMax = new Vector2(1f, 1f);
        rdRt.pivot = new Vector2(1f, 1f);
        rdRt.anchoredPosition = new Vector2(-4f, -4f);
        rdRt.sizeDelta = new Vector2(12f, 12f);

        Image rdImg = rdObj.GetComponent<Image>();
        rdImg.color = new Color(0.95f, 0.20f, 0.20f, 1f);
        rdObj.SetActive(false);
        redDotObj = rdObj;

        Button btn = tabObj.GetComponent<Button>();
        btn.targetGraphic = tabImg;
        btn.onClick.AddListener(() => {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            action();
        });

        return tabImg;
    }

    private void SwitchTab(MailTab tab)
    {
        currentTab = tab;

        tabContentMails.SetActive(tab == MailTab.MailsAndGifts);
        tabContentNotices.SetActive(tab == MailTab.SystemNotices);
        tabContentGiftcode.SetActive(tab == MailTab.GiftcodeHub);

        // Update Tab visual highlights (Active: Radiant Gold, Inactive: Slate)
        UpdateTabButtonState(tabBtnMailsBg, tabBtnMailsText, tab == MailTab.MailsAndGifts);
        UpdateTabButtonState(tabBtnNoticesBg, tabBtnNoticesText, tab == MailTab.SystemNotices);
        UpdateTabButtonState(tabBtnGiftcodeBg, tabBtnGiftcodeText, tab == MailTab.GiftcodeHub);

        // Update Red Dot on Mails Tab
        if (tabMailsRedDot != null)
        {
            tabMailsRedDot.SetActive(HasUnclaimedMail());
        }

        if (tab == MailTab.MailsAndGifts)
        {
            RefreshMailList();
            SelectFirstAvailableMail();
        }
        else if (tab == MailTab.SystemNotices)
        {
            RefreshNoticeList();
            SelectFirstAvailableNotice();
        }
    }

    private void UpdateTabButtonState(Image bg, TextMeshProUGUI text, bool isActive)
    {
        if (bg == null || text == null) return;

        var outline = bg.GetComponent<Outline>();

        if (isActive)
        {
            bg.color = new Color(0.24f, 0.18f, 0.08f, 0.98f);
            if (outline != null) outline.effectColor = new Color(1f, 0.85f, 0.35f, 0.95f);
            text.color = new Color(1f, 0.92f, 0.50f);
        }
        else
        {
            bg.color = new Color(0.08f, 0.10f, 0.14f, 0.90f);
            if (outline != null) outline.effectColor = new Color(0.30f, 0.40f, 0.55f, 0.40f);
            text.color = new Color(0.65f, 0.75f, 0.85f);
        }
    }

    // =========================================================================
    // TAB 1: THƯ & QUÀ TẶNG (MAILS & REWARDS)
    // =========================================================================

    private void BuildMailsTab(Transform parent)
    {
        bool isVi = LocalizationManager.IsVietnamese;

        // --- LEFT COLUMN: Mail List (Width 420px) ---
        GameObject leftCol = new GameObject("LeftColumn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        leftCol.transform.SetParent(parent, false);
        RectTransform lcRt = leftCol.GetComponent<RectTransform>();
        lcRt.anchorMin = new Vector2(0f, 0f);
        lcRt.anchorMax = new Vector2(0f, 1f);
        lcRt.pivot = new Vector2(0f, 0.5f);
        lcRt.anchoredPosition = new Vector2(0f, 0f);
        lcRt.sizeDelta = new Vector2(420f, 0f);

        Image lcImg = leftCol.GetComponent<Image>();
        lcImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        lcImg.type = Image.Type.Sliced;
        lcImg.color = new Color(0.03f, 0.04f, 0.07f, 0.88f);

        var lcOut = leftCol.AddComponent<Outline>();
        lcOut.effectColor = new Color(0.30f, 0.42f, 0.58f, 0.50f);
        lcOut.effectDistance = new Vector2(1f, -1f);

        // Header of Left Column with Counter
        GameObject listHeadObj = new GameObject("ListHeader", typeof(RectTransform));
        listHeadObj.transform.SetParent(leftCol.transform, false);
        RectTransform lhRt = listHeadObj.GetComponent<RectTransform>();
        lhRt.anchorMin = new Vector2(0f, 1f);
        lhRt.anchorMax = new Vector2(1f, 1f);
        lhRt.pivot = new Vector2(0.5f, 1f);
        lhRt.anchoredPosition = new Vector2(0f, -8f);
        lhRt.sizeDelta = new Vector2(-24f, 32f);

        GameObject lhTxtObj = new GameObject("CounterText", typeof(RectTransform), typeof(CanvasRenderer));
        lhTxtObj.transform.SetParent(listHeadObj.transform, false);
        RectTransform lhtRt = lhTxtObj.GetComponent<RectTransform>();
        lhtRt.anchorMin = Vector2.zero;
        lhtRt.anchorMax = Vector2.one;
        lhtRt.sizeDelta = Vector2.zero;

        mailCounterText = lhTxtObj.AddComponent<TextMeshProUGUI>();
        mailCounterText.fontSize = 12.5f;
        mailCounterText.color = new Color(0.75f, 0.85f, 0.95f);
        mailCounterText.alignment = TextAlignmentOptions.MidlineLeft;

        // Scroll Container for Mail Cards
        GameObject scrollRoot = new GameObject("ScrollRoot", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        scrollRoot.transform.SetParent(leftCol.transform, false);
        RectTransform srRt = scrollRoot.GetComponent<RectTransform>();
        srRt.anchorMin = new Vector2(0f, 0f);
        srRt.anchorMax = new Vector2(1f, 1f);
        srRt.offsetMin = new Vector2(8f, 62f);
        srRt.offsetMax = new Vector2(-8f, -44f);

        ScrollRect sRect = scrollRoot.GetComponent<ScrollRect>();
        sRect.horizontal = false;
        sRect.vertical = true;
        sRect.scrollSensitivity = 28f;

        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollRoot.transform, false);
        RectTransform cntRt = contentObj.GetComponent<RectTransform>();
        cntRt.anchorMin = new Vector2(0f, 1f);
        cntRt.anchorMax = new Vector2(1f, 1f);
        cntRt.pivot = new Vector2(0.5f, 1f);
        cntRt.sizeDelta = new Vector2(0f, 0f);

        var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sRect.content = cntRt;
        sRect.viewport = srRt;
        mailListContent = contentObj.transform;

        // Bottom Action Bar of Left Column: [NHẬN TẤT CẢ] & [XÓA ĐÃ ĐỌC]
        GameObject actionBar = new GameObject("LeftActionBar", typeof(RectTransform));
        actionBar.transform.SetParent(leftCol.transform, false);
        RectTransform abRt = actionBar.GetComponent<RectTransform>();
        abRt.anchorMin = new Vector2(0f, 0f);
        abRt.anchorMax = new Vector2(1f, 0f);
        abRt.pivot = new Vector2(0.5f, 0f);
        abRt.anchoredPosition = new Vector2(0f, 8f);
        abRt.sizeDelta = new Vector2(-16f, 48f);

        // Nút NHẬN TẤT CẢ (Claim All) - Radiant Gold
        GameObject claimAllBtnObj = new GameObject("Btn_ClaimAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        claimAllBtnObj.transform.SetParent(actionBar.transform, false);
        RectTransform caRt = claimAllBtnObj.GetComponent<RectTransform>();
        caRt.anchorMin = new Vector2(0f, 0.5f);
        caRt.anchorMax = new Vector2(0f, 0.5f);
        caRt.pivot = new Vector2(0f, 0.5f);
        caRt.anchoredPosition = new Vector2(0f, 0f);
        caRt.sizeDelta = new Vector2(230f, 44f);

        Image caImg = claimAllBtnObj.GetComponent<Image>();
        caImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        caImg.type = Image.Type.Sliced;
        caImg.color = new Color(0.85f, 0.65f, 0.15f, 1f);

        var caOut = claimAllBtnObj.AddComponent<Outline>();
        caOut.effectColor = new Color(1f, 0.90f, 0.45f, 0.90f);
        caOut.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject catObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        catObj.transform.SetParent(claimAllBtnObj.transform, false);
        RectTransform catRt = catObj.GetComponent<RectTransform>();
        catRt.anchorMin = Vector2.zero;
        catRt.anchorMax = Vector2.one;
        catRt.sizeDelta = Vector2.zero;
        var catTmp = catObj.AddComponent<TextMeshProUGUI>();
        catTmp.text = isVi ? "<b>★ NHẬN TẤT CẢ</b>" : "<b>★ CLAIM ALL</b>";
        catTmp.fontSize = 13.5f;
        catTmp.alignment = TextAlignmentOptions.Center;
        catTmp.color = new Color(0.12f, 0.08f, 0.02f);

        Button claimAllBtn = claimAllBtnObj.GetComponent<Button>();
        claimAllBtn.targetGraphic = caImg;
        claimAllBtn.onClick.AddListener(OnClaimAllMails);

        // Nút XÓA ĐÃ ĐỌC (Delete Read) - Dark Stone
        GameObject delReadBtnObj = new GameObject("Btn_DeleteRead", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        delReadBtnObj.transform.SetParent(actionBar.transform, false);
        RectTransform drRt = delReadBtnObj.GetComponent<RectTransform>();
        drRt.anchorMin = new Vector2(1f, 0.5f);
        drRt.anchorMax = new Vector2(1f, 0.5f);
        drRt.pivot = new Vector2(1f, 0.5f);
        drRt.anchoredPosition = new Vector2(0f, 0f);
        drRt.sizeDelta = new Vector2(160f, 44f);

        Image drImg = delReadBtnObj.GetComponent<Image>();
        drImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        drImg.type = Image.Type.Sliced;
        drImg.color = new Color(0.28f, 0.14f, 0.16f, 0.95f);

        var drOut = delReadBtnObj.AddComponent<Outline>();
        drOut.effectColor = new Color(0.85f, 0.35f, 0.35f, 0.65f);
        drOut.effectDistance = new Vector2(1.2f, -1.2f);

        GameObject drtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        drtObj.transform.SetParent(delReadBtnObj.transform, false);
        RectTransform drtRt = drtObj.GetComponent<RectTransform>();
        drtRt.anchorMin = Vector2.zero;
        drtRt.anchorMax = Vector2.one;
        drtRt.sizeDelta = Vector2.zero;
        var drtTmp = drtObj.AddComponent<TextMeshProUGUI>();
        drtTmp.text = isVi ? "<b>XÓA ĐÃ ĐỌC</b>" : "<b>DELETE READ</b>";
        drtTmp.fontSize = 12.5f;
        drtTmp.alignment = TextAlignmentOptions.Center;
        drtTmp.color = Color.white;

        Button delReadBtn = delReadBtnObj.GetComponent<Button>();
        delReadBtn.targetGraphic = drImg;
        delReadBtn.onClick.AddListener(OnDeleteAllReadMails);

        // --- RIGHT COLUMN: Inspection Panel (Width 685px) ---
        GameObject rightCol = new GameObject("RightColumn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rightCol.transform.SetParent(parent, false);
        RectTransform rcRt = rightCol.GetComponent<RectTransform>();
        rcRt.anchorMin = new Vector2(1f, 0f);
        rcRt.anchorMax = new Vector2(1f, 1f);
        rcRt.pivot = new Vector2(1f, 0.5f);
        rcRt.anchoredPosition = new Vector2(0f, 0f);
        rcRt.sizeDelta = new Vector2(685f, 0f);

        Image rcImg = rightCol.GetComponent<Image>();
        rcImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        rcImg.type = Image.Type.Sliced;
        rcImg.color = new Color(0.04f, 0.05f, 0.09f, 0.92f);

        var rcOut = rightCol.AddComponent<Outline>();
        rcOut.effectColor = new Color(0.30f, 0.42f, 0.58f, 0.50f);
        rcOut.effectDistance = new Vector2(1f, -1f);

        // Container when Mail is Selected
        mailDetailPanel = new GameObject("DetailPanel", typeof(RectTransform));
        mailDetailPanel.transform.SetParent(rightCol.transform, false);
        RectTransform dpRt = mailDetailPanel.GetComponent<RectTransform>();
        dpRt.anchorMin = Vector2.zero;
        dpRt.anchorMax = Vector2.one;
        dpRt.sizeDelta = Vector2.zero;

        // Detail Header: Title
        GameObject dtObj = new GameObject("DetailTitle", typeof(RectTransform), typeof(CanvasRenderer));
        dtObj.transform.SetParent(mailDetailPanel.transform, false);
        RectTransform dtRt = dtObj.GetComponent<RectTransform>();
        dtRt.anchorMin = new Vector2(0f, 1f);
        dtRt.anchorMax = new Vector2(1f, 1f);
        dtRt.offsetMin = new Vector2(24f, -48f);
        dtRt.offsetMax = new Vector2(-24f, -14f);

        detailTitleText = dtObj.AddComponent<TextMeshProUGUI>();
        detailTitleText.fontSize = 17f;
        detailTitleText.fontStyle = FontStyles.Bold;
        detailTitleText.color = new Color(1f, 0.88f, 0.45f);

        // Detail Meta: Sender & Date
        GameObject dmObj = new GameObject("DetailMeta", typeof(RectTransform), typeof(CanvasRenderer));
        dmObj.transform.SetParent(mailDetailPanel.transform, false);
        RectTransform dmRt = dmObj.GetComponent<RectTransform>();
        dmRt.anchorMin = new Vector2(0f, 1f);
        dmRt.anchorMax = new Vector2(1f, 1f);
        dmRt.offsetMin = new Vector2(24f, -74f);
        dmRt.offsetMax = new Vector2(-24f, -48f);

        detailSenderText = dmObj.AddComponent<TextMeshProUGUI>();
        detailSenderText.fontSize = 12.5f;
        detailSenderText.color = new Color(0.55f, 0.72f, 0.92f);

        // Divider
        SoulslikeHUDStyler.AddFiligreeDivider(mailDetailPanel.transform, new Vector2(0f, -80f), new Vector2(635f, 6f));

        // Scrollable Body Text
        GameObject bodyScrollRoot = new GameObject("BodyScrollRoot", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        bodyScrollRoot.transform.SetParent(mailDetailPanel.transform, false);
        RectTransform bsrRt = bodyScrollRoot.GetComponent<RectTransform>();
        bsrRt.anchorMin = new Vector2(0f, 0f);
        bsrRt.anchorMax = new Vector2(1f, 1f);
        bsrRt.offsetMin = new Vector2(24f, 142f);
        bsrRt.offsetMax = new Vector2(-24f, -92f);

        ScrollRect bodyScroll = bodyScrollRoot.GetComponent<ScrollRect>();
        bodyScroll.horizontal = false;
        bodyScroll.vertical = true;
        bodyScroll.scrollSensitivity = 24f;

        GameObject bodyContentObj = new GameObject("BodyContent", typeof(RectTransform), typeof(ContentSizeFitter));
        bodyContentObj.transform.SetParent(bodyScrollRoot.transform, false);
        RectTransform bcRt = bodyContentObj.GetComponent<RectTransform>();
        bcRt.anchorMin = new Vector2(0f, 1f);
        bcRt.anchorMax = new Vector2(1f, 1f);
        bcRt.pivot = new Vector2(0.5f, 1f);
        bcRt.sizeDelta = new Vector2(0f, 0f);

        var bcsf = bodyContentObj.GetComponent<ContentSizeFitter>();
        bcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        detailBodyText = bodyContentObj.AddComponent<TextMeshProUGUI>();
        detailBodyText.fontSize = 14f;
        detailBodyText.lineSpacing = 16f;
        detailBodyText.color = new Color(0.88f, 0.92f, 0.98f);
        detailBodyText.enableWordWrapping = true;

        bodyScroll.content = bcRt;
        bodyScroll.viewport = bsrRt;

        // Attachment Tray (PHẦN THƯỞNG ĐÍNH KÈM)
        detailRewardBox = new GameObject("RewardBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        detailRewardBox.transform.SetParent(mailDetailPanel.transform, false);
        RectTransform rbRt = detailRewardBox.GetComponent<RectTransform>();
        rbRt.anchorMin = new Vector2(0.5f, 0f);
        rbRt.anchorMax = new Vector2(0.5f, 0f);
        rbRt.pivot = new Vector2(0.5f, 0f);
        rbRt.anchoredPosition = new Vector2(0f, 68f);
        rbRt.sizeDelta = new Vector2(640f, 64f);

        Image rbImg = detailRewardBox.GetComponent<Image>();
        rbImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        rbImg.type = Image.Type.Sliced;
        rbImg.color = new Color(0.08f, 0.11f, 0.16f, 0.96f);

        var rbOut = detailRewardBox.AddComponent<Outline>();
        rbOut.effectColor = new Color(0.85f, 0.70f, 0.30f, 0.70f);
        rbOut.effectDistance = new Vector2(1.2f, -1.2f);

        // Reward Icon (Aethel Soul Shard in Slot Frame)
        GameObject rwSlotObj = new GameObject("SlotFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rwSlotObj.transform.SetParent(detailRewardBox.transform, false);
        RectTransform rwsRt = rwSlotObj.GetComponent<RectTransform>();
        rwsRt.anchorMin = new Vector2(0f, 0.5f);
        rwsRt.anchorMax = new Vector2(0f, 0.5f);
        rwsRt.pivot = new Vector2(0f, 0.5f);
        rwsRt.anchoredPosition = new Vector2(12f, 0f);
        rwsRt.sizeDelta = new Vector2(50f, 50f);

        Image rwsImg = rwSlotObj.GetComponent<Image>();
        rwsImg.sprite = SoulslikeHUDStyler.GetPotionSlotFrameSprite();
        rwsImg.color = new Color(1f, 0.85f, 0.40f);

        GameObject rwIconObj = new GameObject("ShardIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rwIconObj.transform.SetParent(rwSlotObj.transform, false);
        RectTransform rwiRt = rwIconObj.GetComponent<RectTransform>();
        rwiRt.anchorMin = new Vector2(0.5f, 0.5f);
        rwiRt.anchorMax = new Vector2(0.5f, 0.5f);
        rwiRt.pivot = new Vector2(0.5f, 0.5f);
        rwiRt.anchoredPosition = Vector2.zero;
        rwiRt.sizeDelta = new Vector2(34f, 34f);

        Image rwiImg = rwIconObj.GetComponent<Image>();
        Sprite shardSprite = Resources.Load<Sprite>("UI/Soulslike/Bar_Amber_Fill");
        if (shardSprite == null) shardSprite = Resources.Load<Sprite>("UI/Augments/blood_drop");
        rwiImg.sprite = shardSprite;
        rwiImg.color = new Color(1f, 0.85f, 0.30f);

        // Reward Value Text
        GameObject rwTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        rwTxtObj.transform.SetParent(detailRewardBox.transform, false);
        RectTransform rwtRt = rwTxtObj.GetComponent<RectTransform>();
        rwtRt.anchorMin = new Vector2(0f, 0f);
        rwtRt.anchorMax = new Vector2(1f, 1f);
        rwtRt.offsetMin = new Vector2(72f, 0f);
        rwtRt.offsetMax = new Vector2(-12f, 0f);

        detailRewardValText = rwTxtObj.AddComponent<TextMeshProUGUI>();
        detailRewardValText.fontSize = 14.5f;
        detailRewardValText.fontStyle = FontStyles.Bold;
        detailRewardValText.alignment = TextAlignmentOptions.MidlineLeft;
        detailRewardValText.color = new Color(1f, 0.90f, 0.55f);

        // Action Buttons at Bottom of Inspection Panel
        GameObject detailActionRow = new GameObject("DetailActionRow", typeof(RectTransform));
        detailActionRow.transform.SetParent(mailDetailPanel.transform, false);
        RectTransform darRt = detailActionRow.GetComponent<RectTransform>();
        darRt.anchorMin = new Vector2(0.5f, 0f);
        darRt.anchorMax = new Vector2(0.5f, 0f);
        darRt.pivot = new Vector2(0.5f, 0f);
        darRt.anchoredPosition = new Vector2(0f, 12f);
        darRt.sizeDelta = new Vector2(640f, 48f);

        // Nút NHẬN PHẦN THƯỞNG (Claim Mail) - Emerald / Radiant Green
        GameObject claimBtnObj = new GameObject("Btn_Claim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        claimBtnObj.layer = 5;
        claimBtnObj.transform.SetParent(detailActionRow.transform, false);
        RectTransform clRt = claimBtnObj.GetComponent<RectTransform>();
        clRt.anchorMin = new Vector2(0.5f, 0.5f);
        clRt.anchorMax = new Vector2(0.5f, 0.5f);
        clRt.pivot = new Vector2(0.5f, 0.5f);
        clRt.anchoredPosition = new Vector2(-80f, 0f);
        clRt.sizeDelta = new Vector2(280f, 46f);

        Image clImg = claimBtnObj.GetComponent<Image>();
        clImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        clImg.type = Image.Type.Sliced;
        clImg.color = new Color(0.10f, 0.52f, 0.28f, 0.98f);

        var clOut = claimBtnObj.AddComponent<Outline>();
        clOut.effectColor = new Color(0.35f, 1f, 0.65f, 0.75f);
        clOut.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject clTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        clTxtObj.transform.SetParent(claimBtnObj.transform, false);
        RectTransform cltRt = clTxtObj.GetComponent<RectTransform>();
        cltRt.anchorMin = Vector2.zero;
        cltRt.anchorMax = Vector2.one;
        cltRt.sizeDelta = Vector2.zero;

        detailClaimBtnText = clTxtObj.AddComponent<TextMeshProUGUI>();
        detailClaimBtnText.text = isVi ? "<b>NHẬN PHẦN THƯỞNG</b>" : "<b>CLAIM REWARD</b>";
        detailClaimBtnText.fontSize = 15f;
        detailClaimBtnText.alignment = TextAlignmentOptions.Center;
        detailClaimBtnText.color = Color.white;

        detailClaimButton = claimBtnObj.GetComponent<Button>();
        detailClaimButton.targetGraphic = clImg;
        detailClaimButton.onClick.AddListener(OnClaimSelectedMail);

        // Nút XÓA THƯ NÀY (Delete This Mail)
        GameObject delOneBtnObj = new GameObject("Btn_DeleteThis", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        delOneBtnObj.layer = 5;
        delOneBtnObj.transform.SetParent(detailActionRow.transform, false);
        RectTransform dotRt = delOneBtnObj.GetComponent<RectTransform>();
        dotRt.anchorMin = new Vector2(0.5f, 0.5f);
        dotRt.anchorMax = new Vector2(0.5f, 0.5f);
        dotRt.pivot = new Vector2(0.5f, 0.5f);
        dotRt.anchoredPosition = new Vector2(180f, 0f);
        dotRt.sizeDelta = new Vector2(180f, 46f);

        Image dotImg = delOneBtnObj.GetComponent<Image>();
        dotImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        dotImg.type = Image.Type.Sliced;
        dotImg.color = new Color(0.35f, 0.15f, 0.16f, 0.95f);

        var dotOut = delOneBtnObj.AddComponent<Outline>();
        dotOut.effectColor = new Color(0.85f, 0.35f, 0.35f, 0.70f);
        dotOut.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject dottObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        dottObj.transform.SetParent(delOneBtnObj.transform, false);
        RectTransform dottRt = dottObj.GetComponent<RectTransform>();
        dottRt.anchorMin = Vector2.zero;
        dottRt.anchorMax = Vector2.one;
        dottRt.sizeDelta = Vector2.zero;

        var dottTmp = dottObj.AddComponent<TextMeshProUGUI>();
        dottTmp.text = isVi ? "<b>XÓA THƯ NÀY</b>" : "<b>DELETE THIS</b>";
        dottTmp.fontSize = 13.5f;
        dottTmp.alignment = TextAlignmentOptions.Center;
        dottTmp.color = Color.white;

        detailDeleteButton = delOneBtnObj.GetComponent<Button>();
        detailDeleteButton.targetGraphic = dotImg;
        detailDeleteButton.onClick.AddListener(OnDeleteSelectedMail);

        // --- EMPTY STATE WATERMARK ILLUSTRATION ---
        mailEmptyStateObj = new GameObject("EmptyStateIllustration", typeof(RectTransform));
        mailEmptyStateObj.transform.SetParent(rightCol.transform, false);
        RectTransform esRt = mailEmptyStateObj.GetComponent<RectTransform>();
        esRt.anchorMin = Vector2.zero;
        esRt.anchorMax = Vector2.one;
        esRt.sizeDelta = Vector2.zero;

        // Big Royal Crest Watermark
        GameObject esIconObj = new GameObject("WatermarkIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        esIconObj.transform.SetParent(mailEmptyStateObj.transform, false);
        RectTransform esiRt = esIconObj.GetComponent<RectTransform>();
        esiRt.anchorMin = new Vector2(0.5f, 0.5f);
        esiRt.anchorMax = new Vector2(0.5f, 0.5f);
        esiRt.pivot = new Vector2(0.5f, 0.5f);
        esiRt.anchoredPosition = new Vector2(0f, 60f);
        esiRt.sizeDelta = new Vector2(80f, 80f);

        Image esiImg = esIconObj.GetComponent<Image>();
        esiImg.sprite = mailIcon;
        esiImg.color = new Color(1f, 0.85f, 0.40f, 0.35f);

        // Empty State Title
        GameObject estObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        estObj.transform.SetParent(mailEmptyStateObj.transform, false);
        RectTransform estRt = estObj.GetComponent<RectTransform>();
        estRt.anchorMin = new Vector2(0.5f, 0.5f);
        estRt.anchorMax = new Vector2(0.5f, 0.5f);
        estRt.pivot = new Vector2(0.5f, 0.5f);
        estRt.anchoredPosition = new Vector2(0f, -8f);
        estRt.sizeDelta = new Vector2(500f, 32f);

        var estTmp = estObj.AddComponent<TextMeshProUGUI>();
        estTmp.text = isVi ? "<b>HÒM THƯ HIỆP SĨ TRỐNG</b>" : "<b>KNIGHT'S MAILBOX IS EMPTY</b>";
        estTmp.fontSize = 17f;
        estTmp.color = new Color(1f, 0.88f, 0.45f, 0.85f);
        estTmp.alignment = TextAlignmentOptions.Center;

        // Empty State Subtitle
        GameObject essObj = new GameObject("SubTitle", typeof(RectTransform), typeof(CanvasRenderer));
        essObj.transform.SetParent(mailEmptyStateObj.transform, false);
        RectTransform essRt = essObj.GetComponent<RectTransform>();
        essRt.anchorMin = new Vector2(0.5f, 0.5f);
        essRt.anchorMax = new Vector2(0.5f, 0.5f);
        essRt.pivot = new Vector2(0.5f, 0.5f);
        essRt.anchoredPosition = new Vector2(0f, -44f);
        essRt.sizeDelta = new Vector2(540f, 48f);

        var essTmp = essObj.AddComponent<TextMeshProUGUI>();
        essTmp.text = isVi 
            ? "Hiện không có thư hoặc quà mới nào từ Hội Đồng Vaelstrider.\nMọi thông báo và phần thưởng tri ân sẽ được gửi tại đây."
            : "No active mails or gifts from the Council of Vaelstrider.\nSystem notices and event rewards will appear here.";
        essTmp.fontSize = 13f;
        essTmp.lineSpacing = 12f;
        essTmp.color = new Color(0.60f, 0.70f, 0.82f, 0.75f);
        essTmp.alignment = TextAlignmentOptions.Center;

        // Button: KHÔI PHỤC THƯ MẪU (Restore Default Mails)
        GameObject rstBtnObj = new GameObject("Btn_RestoreMails", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        rstBtnObj.transform.SetParent(mailEmptyStateObj.transform, false);
        RectTransform rstRt = rstBtnObj.GetComponent<RectTransform>();
        rstRt.anchorMin = new Vector2(0.5f, 0.5f);
        rstRt.anchorMax = new Vector2(0.5f, 0.5f);
        rstRt.pivot = new Vector2(0.5f, 0.5f);
        rstRt.anchoredPosition = new Vector2(0f, -100f);
        rstRt.sizeDelta = new Vector2(240f, 44f);

        Image rstImg = rstBtnObj.GetComponent<Image>();
        rstImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        rstImg.type = Image.Type.Sliced;
        rstImg.color = new Color(0.18f, 0.24f, 0.35f, 0.95f);

        var rstOut = rstBtnObj.AddComponent<Outline>();
        rstOut.effectColor = new Color(0.55f, 0.75f, 1f, 0.65f);
        rstOut.effectDistance = new Vector2(1.2f, -1.2f);

        GameObject rsttObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        rsttObj.transform.SetParent(rstBtnObj.transform, false);
        RectTransform rsttRt = rsttObj.GetComponent<RectTransform>();
        rsttRt.anchorMin = Vector2.zero;
        rsttRt.anchorMax = Vector2.one;
        rsttRt.sizeDelta = Vector2.zero;
        var rsttTmp = rsttObj.AddComponent<TextMeshProUGUI>();
        rsttTmp.text = isVi ? "<b>↺ KHÔI PHỤC THƯ MẪU</b>" : "<b>↺ RESTORE MAILS</b>";
        rsttTmp.fontSize = 13f;
        rsttTmp.alignment = TextAlignmentOptions.Center;
        rsttTmp.color = Color.white;

        Button rstBtn = rstBtnObj.GetComponent<Button>();
        rstBtn.targetGraphic = rstImg;
        rstBtn.onClick.AddListener(() => {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
            RestoreDefaultMails();
            RefreshMailList();
            SelectFirstAvailableMail();
        });

        mailEmptyStateObj.SetActive(false);
    }

    private void RefreshMailList()
    {
        if (mailListContent == null) return;

        // Clear existing children
        for (int i = mailListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(mailListContent.GetChild(i).gameObject);
        }

        bool isVi = LocalizationManager.IsVietnamese;
        int activeMailCount = 0;

        foreach (var mail in _mails)
        {
            if (IsMailDeleted(mail.id)) continue;
            activeMailCount++;

            bool hasReward = mail.rewardShards > 0;
            bool isClaimed = hasReward && PlayerPrefs.GetInt("Mail_Claimed_" + mail.id, 0) == 1;
            bool isSelected = selectedMail != null && selectedMail.id == mail.id;

            // Mail Card (400 x 82px)
            GameObject item = new GameObject("MailItem_" + mail.id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            item.transform.SetParent(mailListContent, false);

            RectTransform itRt = item.GetComponent<RectTransform>();
            itRt.sizeDelta = new Vector2(0f, 82f);

            Image itemImg = item.GetComponent<Image>();
            itemImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
            itemImg.type = Image.Type.Sliced;

            var itemOut = item.AddComponent<Outline>();

            if (isSelected)
            {
                itemImg.color = new Color(0.18f, 0.14f, 0.07f, 0.98f);
                itemOut.effectColor = new Color(1f, 0.85f, 0.35f, 0.95f);
                itemOut.effectDistance = new Vector2(1.5f, -1.5f);

                // Gold selection indicator bar on the left edge
                GameObject selBar = new GameObject("SelBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                selBar.transform.SetParent(item.transform, false);
                RectTransform sbRt = selBar.GetComponent<RectTransform>();
                sbRt.anchorMin = new Vector2(0f, 0f);
                sbRt.anchorMax = new Vector2(0f, 1f);
                sbRt.pivot = new Vector2(0f, 0.5f);
                sbRt.anchoredPosition = new Vector2(2f, 0f);
                sbRt.sizeDelta = new Vector2(4f, -8f);

                Image sbImg = selBar.GetComponent<Image>();
                sbImg.color = new Color(1f, 0.85f, 0.35f);
            }
            else
            {
                itemImg.color = isClaimed 
                    ? new Color(0.04f, 0.06f, 0.09f, 0.85f) 
                    : new Color(0.06f, 0.08f, 0.13f, 0.95f);
                itemOut.effectColor = new Color(0.24f, 0.32f, 0.45f, 0.50f);
                itemOut.effectDistance = new Vector2(1f, -1f);
            }

            // Envelope Icon on the Left (44x44px)
            GameObject iconObj = new GameObject("EnvelopeIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(item.transform, false);
            RectTransform icRt = iconObj.GetComponent<RectTransform>();
            icRt.anchorMin = new Vector2(0f, 0.5f);
            icRt.anchorMax = new Vector2(0f, 0.5f);
            icRt.pivot = new Vector2(0f, 0.5f);
            icRt.anchoredPosition = new Vector2(14f, 0f);
            icRt.sizeDelta = new Vector2(40f, 40f);

            Image icImg = iconObj.GetComponent<Image>();
            Sprite mailIco = Resources.Load<Sprite>("UI/icon_mail");
            if (mailIco == null) mailIco = SoulslikeHUDStyler.GetAmberFillSprite();
            icImg.sprite = mailIco;
            icImg.color = isClaimed ? new Color(0.55f, 0.65f, 0.75f, 0.7f) : new Color(1f, 0.88f, 0.45f);

            // Card Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
            titleObj.transform.SetParent(item.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.offsetMin = new Vector2(62f, -34f);
            tRt.offsetMax = new Vector2(-16f, -10f);

            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = isVi ? mail.titleVi : mail.titleEn;
            tTmp.fontSize = 13.5f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = isClaimed ? new Color(0.65f, 0.72f, 0.82f) : new Color(1f, 0.90f, 0.50f);
            tTmp.overflowMode = TextOverflowModes.Ellipsis;

            // Sender & Meta line
            GameObject subObj = new GameObject("Meta", typeof(RectTransform), typeof(CanvasRenderer));
            subObj.transform.SetParent(item.transform, false);
            RectTransform sRt = subObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(1f, 0f);
            sRt.offsetMin = new Vector2(62f, 10f);
            sRt.offsetMax = new Vector2(-16f, 32f);

            var sTmp = subObj.AddComponent<TextMeshProUGUI>();
            string sender = isVi ? mail.senderVi : mail.senderEn;
            string statusStr = isClaimed 
                ? (isVi ? "<color=#718096>[Đã nhận]</color>" : "<color=#718096>[Claimed]</color>") 
                : $"<color=#55EFC4><b>+{mail.rewardShards:N0} Linh Hồn</b></color>";
            sTmp.text = $"{sender} • {statusStr}";
            sTmp.fontSize = 11.5f;

            // Red Dot if unclaimed reward
            if (hasReward && !isClaimed)
            {
                GameObject rdObj = new GameObject("RedDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                rdObj.transform.SetParent(item.transform, false);
                RectTransform rdRt = rdObj.GetComponent<RectTransform>();
                rdRt.anchorMin = new Vector2(1f, 1f);
                rdRt.anchorMax = new Vector2(1f, 1f);
                rdRt.pivot = new Vector2(1f, 1f);
                rdRt.anchoredPosition = new Vector2(-8f, -8f);
                rdRt.sizeDelta = new Vector2(12f, 12f);

                Image rdImg = rdObj.GetComponent<Image>();
                rdImg.color = new Color(0.95f, 0.20f, 0.20f, 1f);
            }

            // Click Card Action
            MailItemData capturedMail = mail;
            Button btn = item.GetComponent<Button>();
            btn.targetGraphic = itemImg;
            btn.onClick.AddListener(() => {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                SelectMail(capturedMail);
            });
        }

        // Update counter
        if (mailCounterText != null)
        {
            mailCounterText.text = isVi 
                ? $"<b>DANH SÁCH THƯ HỆ THỐNG ({activeMailCount}/50)</b>" 
                : $"<b>SYSTEM MAIL LIST ({activeMailCount}/50)</b>";
        }

        // Handle Empty State
        if (activeMailCount == 0)
        {
            if (mailDetailPanel != null) mailDetailPanel.SetActive(false);
            if (mailEmptyStateObj != null) mailEmptyStateObj.SetActive(true);
        }
        else
        {
            if (mailDetailPanel != null) mailDetailPanel.SetActive(true);
            if (mailEmptyStateObj != null) mailEmptyStateObj.SetActive(false);
        }
    }

    private void SelectFirstAvailableMail()
    {
        foreach (var m in _mails)
        {
            if (!IsMailDeleted(m.id))
            {
                SelectMail(m);
                return;
            }
        }

        // No mail left
        selectedMail = null;
        if (mailDetailPanel != null) mailDetailPanel.SetActive(false);
        if (mailEmptyStateObj != null) mailEmptyStateObj.SetActive(true);
    }

    private void SelectMail(MailItemData mail)
    {
        if (mail == null || IsMailDeleted(mail.id))
        {
            SelectFirstAvailableMail();
            return;
        }

        selectedMail = mail;
        bool isVi = LocalizationManager.IsVietnamese;

        if (mailDetailPanel != null) mailDetailPanel.SetActive(true);
        if (mailEmptyStateObj != null) mailEmptyStateObj.SetActive(false);

        if (detailTitleText != null) detailTitleText.text = isVi ? mail.titleVi : mail.titleEn;
        if (detailSenderText != null)
        {
            string sender = isVi ? mail.senderVi : mail.senderEn;
            detailSenderText.text = isVi 
                ? $"Người gửi: <b>{sender}</b> | Ngày: {mail.date} | Hạn lưu: 30 ngày"
                : $"From: <b>{sender}</b> | Date: {mail.date} | Expires: 30 days";
        }
        if (detailBodyText != null) detailBodyText.text = isVi ? mail.contentVi : mail.contentEn;

        bool hasReward = mail.rewardShards > 0;
        bool isClaimed = hasReward && PlayerPrefs.GetInt("Mail_Claimed_" + mail.id, 0) == 1;

        if (detailRewardBox != null)
        {
            detailRewardBox.SetActive(hasReward);
            if (hasReward)
            {
                detailRewardValText.text = isVi 
                    ? $"PHẦN THƯỞNG ĐÍNH KÈM: <color=#55EFC4><b>+{mail.rewardShards:N0} MẢNH LINH HỒN</b></color>"
                    : $"ATTACHED REWARD: <color=#55EFC4><b>+{mail.rewardShards:N0} SOUL SHARDS</b></color>";
            }
        }

        if (detailClaimButton != null)
        {
            detailClaimButton.gameObject.SetActive(hasReward);
            detailClaimButton.interactable = !isClaimed;

            Image clImg = detailClaimButton.GetComponent<Image>();
            if (isClaimed)
            {
                clImg.color = new Color(0.20f, 0.25f, 0.32f, 0.95f);
                detailClaimBtnText.text = isVi ? "<b>✓ ĐÃ NHẬN</b>" : "<b>✓ CLAIMED</b>";
                detailClaimBtnText.color = new Color(0.65f, 0.72f, 0.82f);
            }
            else
            {
                clImg.color = new Color(0.10f, 0.52f, 0.28f, 0.98f);
                detailClaimBtnText.text = isVi ? "<b>NHẬN PHẦN THƯỞNG</b>" : "<b>CLAIM REWARD</b>";
                detailClaimBtnText.color = Color.white;
            }
        }

        if (detailDeleteButton != null)
        {
            detailDeleteButton.gameObject.SetActive(isClaimed || !hasReward);
        }

        RefreshMailList();
    }

    private void OnClaimSelectedMail()
    {
        if (selectedMail == null || selectedMail.rewardShards <= 0) return;

        string claimKey = "Mail_Claimed_" + selectedMail.id;
        if (PlayerPrefs.GetInt(claimKey, 0) == 1) return;

        AccountDatabase.AddAethelShards(selectedMail.rewardShards);
        PlayerPrefs.SetInt(claimKey, 1);
        PlayerPrefs.Save();

        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();
        if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();

        SelectMail(selectedMail);
        UpdateLobbyBadge();
        if (tabMailsRedDot != null) tabMailsRedDot.SetActive(HasUnclaimedMail());
    }

    private void OnClaimAllMails()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        int totalClaimed = 0;
        int totalShards = 0;

        foreach (var m in _mails)
        {
            if (IsMailDeleted(m.id)) continue;
            string claimKey = "Mail_Claimed_" + m.id;
            if (m.rewardShards > 0 && PlayerPrefs.GetInt(claimKey, 0) == 0)
            {
                PlayerPrefs.SetInt(claimKey, 1);
                totalShards += m.rewardShards;
                totalClaimed++;
            }
        }

        if (totalShards > 0)
        {
            AccountDatabase.AddAethelShards(totalShards);
            PlayerPrefs.Save();

            if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();
            if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();

            UpdateLobbyBadge();
            if (tabMailsRedDot != null) tabMailsRedDot.SetActive(false);
            SelectMail(selectedMail);
            RefreshMailList();
        }
    }

    private void OnDeleteSelectedMail()
    {
        if (selectedMail == null) return;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        PlayerPrefs.SetInt("Mail_Deleted_" + selectedMail.id, 1);
        PlayerPrefs.Save();

        SelectFirstAvailableMail();
        UpdateLobbyBadge();
        if (tabMailsRedDot != null) tabMailsRedDot.SetActive(HasUnclaimedMail());
    }

    private void OnDeleteAllReadMails()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();

        int deletedCount = 0;
        foreach (var m in _mails)
        {
            bool isClaimed = m.rewardShards <= 0 || PlayerPrefs.GetInt("Mail_Claimed_" + m.id, 0) == 1;
            if (isClaimed && !IsMailDeleted(m.id))
            {
                PlayerPrefs.SetInt("Mail_Deleted_" + m.id, 1);
                deletedCount++;
            }
        }

        if (deletedCount > 0)
        {
            PlayerPrefs.Save();
            SelectFirstAvailableMail();
            UpdateLobbyBadge();
            if (tabMailsRedDot != null) tabMailsRedDot.SetActive(HasUnclaimedMail());
        }
    }

    // =========================================================================
    // TAB 2: THÔNG BÁO HỆ THỐNG (SYSTEM NOTICES)
    // =========================================================================

    private void BuildNoticesTab(Transform parent)
    {
        bool isVi = LocalizationManager.IsVietnamese;

        // Left Column: Notices List (Width 420px)
        GameObject leftCol = new GameObject("LeftColumn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        leftCol.transform.SetParent(parent, false);
        RectTransform lcRt = leftCol.GetComponent<RectTransform>();
        lcRt.anchorMin = new Vector2(0f, 0f);
        lcRt.anchorMax = new Vector2(0f, 1f);
        lcRt.pivot = new Vector2(0f, 0.5f);
        lcRt.anchoredPosition = new Vector2(0f, 0f);
        lcRt.sizeDelta = new Vector2(420f, 0f);

        Image lcImg = leftCol.GetComponent<Image>();
        lcImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        lcImg.type = Image.Type.Sliced;
        lcImg.color = new Color(0.03f, 0.04f, 0.07f, 0.88f);

        var lcOut = leftCol.AddComponent<Outline>();
        lcOut.effectColor = new Color(0.30f, 0.42f, 0.58f, 0.50f);
        lcOut.effectDistance = new Vector2(1f, -1f);

        // Header
        GameObject lhObj = new GameObject("NoticeHeader", typeof(RectTransform), typeof(CanvasRenderer));
        lhObj.transform.SetParent(leftCol.transform, false);
        RectTransform lhRt = lhObj.GetComponent<RectTransform>();
        lhRt.anchorMin = new Vector2(0f, 1f);
        lhRt.anchorMax = new Vector2(1f, 1f);
        lhRt.offsetMin = new Vector2(14f, -40f);
        lhRt.offsetMax = new Vector2(-14f, -8f);

        var lhTmp = lhObj.AddComponent<TextMeshProUGUI>();
        lhTmp.text = isVi ? "<b>BẢNG TIN VƯƠNG QUỐC</b>" : "<b>REALM BULLETIN</b>";
        lhTmp.fontSize = 13.5f;
        lhTmp.color = new Color(0.85f, 0.90f, 0.96f);
        lhTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Scroll Container
        GameObject scrollRoot = new GameObject("NoticeScrollRoot", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        scrollRoot.transform.SetParent(leftCol.transform, false);
        RectTransform srRt = scrollRoot.GetComponent<RectTransform>();
        srRt.anchorMin = Vector2.zero;
        srRt.anchorMax = Vector2.one;
        srRt.offsetMin = new Vector2(8f, 10f);
        srRt.offsetMax = new Vector2(-8f, -44f);

        ScrollRect sRect = scrollRoot.GetComponent<ScrollRect>();
        sRect.horizontal = false;
        sRect.vertical = true;
        sRect.scrollSensitivity = 28f;

        GameObject contentObj = new GameObject("NoticeContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollRoot.transform, false);
        RectTransform cntRt = contentObj.GetComponent<RectTransform>();
        cntRt.anchorMin = new Vector2(0f, 1f);
        cntRt.anchorMax = new Vector2(1f, 1f);
        cntRt.pivot = new Vector2(0.5f, 1f);

        var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sRect.content = cntRt;
        sRect.viewport = srRt;
        noticeListContent = contentObj.transform;

        // Right Column: Notice Inspection Panel (Width 685px)
        GameObject rightCol = new GameObject("RightColumn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rightCol.transform.SetParent(parent, false);
        RectTransform rcRt = rightCol.GetComponent<RectTransform>();
        rcRt.anchorMin = new Vector2(1f, 0f);
        rcRt.anchorMax = new Vector2(1f, 1f);
        rcRt.pivot = new Vector2(1f, 0.5f);
        rcRt.anchoredPosition = new Vector2(0f, 0f);
        rcRt.sizeDelta = new Vector2(685f, 0f);

        Image rcImg = rightCol.GetComponent<Image>();
        rcImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        rcImg.type = Image.Type.Sliced;
        rcImg.color = new Color(0.04f, 0.05f, 0.09f, 0.92f);

        var rcOut = rightCol.AddComponent<Outline>();
        rcOut.effectColor = new Color(0.30f, 0.42f, 0.58f, 0.50f);
        rcOut.effectDistance = new Vector2(1f, -1f);

        // Notice Title
        GameObject ntObj = new GameObject("NoticeTitle", typeof(RectTransform), typeof(CanvasRenderer));
        ntObj.transform.SetParent(rightCol.transform, false);
        RectTransform ntRt = ntObj.GetComponent<RectTransform>();
        ntRt.anchorMin = new Vector2(0f, 1f);
        ntRt.anchorMax = new Vector2(1f, 1f);
        ntRt.offsetMin = new Vector2(24f, -48f);
        ntRt.offsetMax = new Vector2(-24f, -14f);

        noticeDetailTitleText = ntObj.AddComponent<TextMeshProUGUI>();
        noticeDetailTitleText.fontSize = 17f;
        noticeDetailTitleText.fontStyle = FontStyles.Bold;
        noticeDetailTitleText.color = new Color(1f, 0.88f, 0.45f);

        // Notice Meta
        GameObject nmObj = new GameObject("NoticeMeta", typeof(RectTransform), typeof(CanvasRenderer));
        nmObj.transform.SetParent(rightCol.transform, false);
        RectTransform nmRt = nmObj.GetComponent<RectTransform>();
        nmRt.anchorMin = new Vector2(0f, 1f);
        nmRt.anchorMax = new Vector2(1f, 1f);
        nmRt.offsetMin = new Vector2(24f, -74f);
        nmRt.offsetMax = new Vector2(-24f, -48f);

        noticeDetailMetaText = nmObj.AddComponent<TextMeshProUGUI>();
        noticeDetailMetaText.fontSize = 12.5f;
        noticeDetailMetaText.color = new Color(0.55f, 0.72f, 0.92f);

        SoulslikeHUDStyler.AddFiligreeDivider(rightCol.transform, new Vector2(0f, -80f), new Vector2(635f, 6f));

        // Notice Scrollable Body Text
        GameObject nBodyScroll = new GameObject("NoticeBodyScroll", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        nBodyScroll.transform.SetParent(rightCol.transform, false);
        RectTransform nbsRt = nBodyScroll.GetComponent<RectTransform>();
        nbsRt.anchorMin = Vector2.zero;
        nbsRt.anchorMax = Vector2.one;
        nbsRt.offsetMin = new Vector2(24f, 20f);
        nbsRt.offsetMax = new Vector2(-24f, -92f);

        ScrollRect nbScroll = nBodyScroll.GetComponent<ScrollRect>();
        nbScroll.horizontal = false;
        nbScroll.vertical = true;
        nbScroll.scrollSensitivity = 24f;

        GameObject nbContent = new GameObject("BodyContent", typeof(RectTransform), typeof(ContentSizeFitter));
        nbContent.transform.SetParent(nBodyScroll.transform, false);
        RectTransform nbcRt = nbContent.GetComponent<RectTransform>();
        nbcRt.anchorMin = new Vector2(0f, 1f);
        nbcRt.anchorMax = new Vector2(1f, 1f);
        nbcRt.pivot = new Vector2(0.5f, 1f);

        var nbcsf = nbContent.GetComponent<ContentSizeFitter>();
        nbcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        noticeDetailBodyText = nbContent.AddComponent<TextMeshProUGUI>();
        noticeDetailBodyText.fontSize = 14f;
        noticeDetailBodyText.lineSpacing = 16f;
        noticeDetailBodyText.color = new Color(0.88f, 0.92f, 0.98f);
        noticeDetailBodyText.enableWordWrapping = true;

        nbScroll.content = nbcRt;
        nbScroll.viewport = nbsRt;
    }

    private void RefreshNoticeList()
    {
        if (noticeListContent == null) return;

        for (int i = noticeListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(noticeListContent.GetChild(i).gameObject);
        }

        bool isVi = LocalizationManager.IsVietnamese;

        foreach (var notice in _notices)
        {
            bool isSelected = selectedNotice != null && selectedNotice.id == notice.id;

            GameObject item = new GameObject("NoticeItem_" + notice.id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            item.transform.SetParent(noticeListContent, false);

            RectTransform itRt = item.GetComponent<RectTransform>();
            itRt.sizeDelta = new Vector2(0f, 76f);

            Image itemImg = item.GetComponent<Image>();
            itemImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
            itemImg.type = Image.Type.Sliced;

            var itemOut = item.AddComponent<Outline>();

            if (isSelected)
            {
                itemImg.color = new Color(0.18f, 0.14f, 0.07f, 0.98f);
                itemOut.effectColor = new Color(1f, 0.85f, 0.35f, 0.95f);
                itemOut.effectDistance = new Vector2(1.5f, -1.5f);
            }
            else
            {
                itemImg.color = new Color(0.06f, 0.08f, 0.13f, 0.95f);
                itemOut.effectColor = new Color(0.24f, 0.32f, 0.45f, 0.50f);
                itemOut.effectDistance = new Vector2(1f, -1f);
            }

            // Tag Pill (QUY TẮC / LỊCH TRÌNH / CẬP NHẬT)
            GameObject tagObj = new GameObject("TagPill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tagObj.transform.SetParent(item.transform, false);
            RectTransform tgRt = tagObj.GetComponent<RectTransform>();
            tgRt.anchorMin = new Vector2(0f, 0.5f);
            tgRt.anchorMax = new Vector2(0f, 0.5f);
            tgRt.pivot = new Vector2(0f, 0.5f);
            tgRt.anchoredPosition = new Vector2(12f, 0f);
            tgRt.sizeDelta = new Vector2(80f, 26f);

            Image tgImg = tagObj.GetComponent<Image>();
            tgImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
            tgImg.type = Image.Type.Sliced;
            tgImg.color = new Color(0.20f, 0.30f, 0.45f, 0.95f);

            GameObject tgtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            tgtObj.transform.SetParent(tagObj.transform, false);
            RectTransform tgttRt = tgtObj.GetComponent<RectTransform>();
            tgttRt.anchorMin = Vector2.zero;
            tgttRt.anchorMax = Vector2.one;
            tgttRt.sizeDelta = Vector2.zero;
            var tgTmp = tgtObj.AddComponent<TextMeshProUGUI>();
            tgTmp.text = $"<b>{notice.categoryTag}</b>";
            tgTmp.fontSize = 10.5f;
            tgTmp.alignment = TextAlignmentOptions.Center;
            tgTmp.color = Color.white;

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
            titleObj.transform.SetParent(item.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.offsetMin = new Vector2(100f, -32f);
            tRt.offsetMax = new Vector2(-12f, -8f);

            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = isVi ? notice.titleVi : notice.titleEn;
            tTmp.fontSize = 12.5f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = isSelected ? new Color(1f, 0.90f, 0.50f) : new Color(0.85f, 0.90f, 0.96f);
            tTmp.overflowMode = TextOverflowModes.Ellipsis;

            // Sender & Date
            GameObject subObj = new GameObject("Meta", typeof(RectTransform), typeof(CanvasRenderer));
            subObj.transform.SetParent(item.transform, false);
            RectTransform sRt = subObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(1f, 0f);
            sRt.offsetMin = new Vector2(100f, 8f);
            sRt.offsetMax = new Vector2(-12f, 28f);

            var sTmp = subObj.AddComponent<TextMeshProUGUI>();
            string sender = isVi ? notice.senderVi : notice.senderEn;
            sTmp.text = $"{sender} • {notice.date}";
            sTmp.fontSize = 11f;
            sTmp.color = new Color(0.55f, 0.65f, 0.78f);

            MailItemData capturedNotice = notice;
            Button btn = item.GetComponent<Button>();
            btn.targetGraphic = itemImg;
            btn.onClick.AddListener(() => {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                SelectNotice(capturedNotice);
            });
        }
    }

    private void SelectFirstAvailableNotice()
    {
        if (_notices.Count > 0)
        {
            SelectNotice(_notices[0]);
        }
    }

    private void SelectNotice(MailItemData notice)
    {
        if (notice == null) return;
        selectedNotice = notice;
        bool isVi = LocalizationManager.IsVietnamese;

        if (noticeDetailTitleText != null) noticeDetailTitleText.text = isVi ? notice.titleVi : notice.titleEn;
        if (noticeDetailMetaText != null)
        {
            string sender = isVi ? notice.senderVi : notice.senderEn;
            noticeDetailMetaText.text = isVi 
                ? $"Đăng bởi: <b>{sender}</b> | Ngày phát hành: {notice.date}" 
                : $"Posted by: <b>{sender}</b> | Release date: {notice.date}";
        }
        if (noticeDetailBodyText != null) noticeDetailBodyText.text = isVi ? notice.contentVi : notice.contentEn;

        RefreshNoticeList();
    }

    // =========================================================================
    // TAB 3: MÃ QUÀ TẶNG (GIFTCODE REDEMPTION HUB)
    // =========================================================================

    private void BuildGiftcodeTab(Transform parent)
    {
        bool isVi = LocalizationManager.IsVietnamese;

        // Container Card
        GameObject mainCard = new GameObject("GiftcodeCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        mainCard.transform.SetParent(parent, false);
        RectTransform mcRt = mainCard.GetComponent<RectTransform>();
        mcRt.anchorMin = Vector2.zero;
        mcRt.anchorMax = Vector2.one;
        mcRt.sizeDelta = Vector2.zero;

        Image mcImg = mainCard.GetComponent<Image>();
        mcImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        mcImg.type = Image.Type.Sliced;
        mcImg.color = new Color(0.04f, 0.05f, 0.09f, 0.95f);

        var mcOut = mainCard.AddComponent<Outline>();
        mcOut.effectColor = new Color(0.30f, 0.42f, 0.58f, 0.50f);
        mcOut.effectDistance = new Vector2(1f, -1f);

        // Header Title & Subtitle
        GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        tObj.transform.SetParent(mainCard.transform, false);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 1f);
        tRt.anchorMax = new Vector2(0.5f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -20f);
        tRt.sizeDelta = new Vector2(800f, 32f);

        var tTmp = tObj.AddComponent<TextMeshProUGUI>();
        tTmp.text = isVi ? "<b>TRUNG TÂM ĐỔI MÃ QUÀ TẶNG VƯƠNG QUỐC</b>" : "<b>KINGDOM GIFTCODE REDEMPTION</b>";
        tTmp.fontSize = 19f;
        tTmp.color = new Color(1f, 0.88f, 0.45f);
        tTmp.alignment = TextAlignmentOptions.Center;

        GameObject stObj = new GameObject("SubTitle", typeof(RectTransform), typeof(CanvasRenderer));
        stObj.transform.SetParent(mainCard.transform, false);
        RectTransform stRt = stObj.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0.5f, 1f);
        stRt.anchorMax = new Vector2(0.5f, 1f);
        stRt.pivot = new Vector2(0.5f, 1f);
        stRt.anchoredPosition = new Vector2(0f, -54f);
        stRt.sizeDelta = new Vector2(900f, 24f);

        var stTmp = stObj.AddComponent<TextMeshProUGUI>();
        stTmp.text = isVi 
            ? "Nhập mã quà tặng từ sự kiện, cộng đồng hoặc nhà phát triển để nhận Linh Hồn và vật phẩm quý giá."
            : "Redeem giftcodes from events, community, and dev milestones to claim Soul Shards.";
        stTmp.fontSize = 13f;
        stTmp.color = new Color(0.65f, 0.75f, 0.88f);
        stTmp.alignment = TextAlignmentOptions.Center;

        // Redemption Input Box (Height 54px)
        GameObject inputBar = new GameObject("InputBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        inputBar.transform.SetParent(mainCard.transform, false);
        RectTransform ibRt = inputBar.GetComponent<RectTransform>();
        ibRt.anchorMin = new Vector2(0.5f, 1f);
        ibRt.anchorMax = new Vector2(0.5f, 1f);
        ibRt.pivot = new Vector2(0.5f, 1f);
        ibRt.anchoredPosition = new Vector2(0f, -94f);
        ibRt.sizeDelta = new Vector2(920f, 60f);

        Image ibImg = inputBar.GetComponent<Image>();
        ibImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        ibImg.type = Image.Type.Sliced;
        ibImg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

        var ibOut = inputBar.AddComponent<Outline>();
        ibOut.effectColor = new Color(0.85f, 0.70f, 0.30f, 0.65f);
        ibOut.effectDistance = new Vector2(1.5f, -1.5f);

        // Input Field Component
        GameObject ifObj = new GameObject("InputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        ifObj.transform.SetParent(inputBar.transform, false);
        RectTransform ifRt = ifObj.GetComponent<RectTransform>();
        ifRt.anchorMin = new Vector2(0f, 0.5f);
        ifRt.anchorMax = new Vector2(0f, 0.5f);
        ifRt.pivot = new Vector2(0f, 0.5f);
        ifRt.anchoredPosition = new Vector2(16f, 0f);
        ifRt.sizeDelta = new Vector2(430f, 44f);

        Image ifBg = ifObj.GetComponent<Image>();
        ifBg.color = new Color(0.04f, 0.06f, 0.09f, 0.95f);

        giftcodeInput = ifObj.GetComponent<TMP_InputField>();
        giftcodeInput.targetGraphic = ifBg;
        giftcodeInput.characterLimit = 22;

        GameObject taObj = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        taObj.transform.SetParent(ifObj.transform, false);
        RectTransform taRt = taObj.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(12f, 2f);
        taRt.offsetMax = new Vector2(-12f, -2f);

        GameObject phObj = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer));
        phObj.transform.SetParent(taObj.transform, false);
        RectTransform phRt = phObj.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.sizeDelta = Vector2.zero;
        var phTmp = phObj.AddComponent<TextMeshProUGUI>();
        phTmp.text = isVi ? "Chạm để nhập mã quà tặng..." : "Tap to enter giftcode...";
        phTmp.fontSize = 13.5f;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.5f, 0.6f, 0.7f, 0.7f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;
        phTmp.raycastTarget = false;

        GameObject itObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        itObj.transform.SetParent(taObj.transform, false);
        RectTransform itRt = itObj.GetComponent<RectTransform>();
        itRt.anchorMin = Vector2.zero;
        itRt.anchorMax = Vector2.one;
        itRt.sizeDelta = Vector2.zero;
        var itTmp = itObj.AddComponent<TextMeshProUGUI>();
        itTmp.fontSize = 14.5f;
        itTmp.fontStyle = FontStyles.Bold;
        itTmp.color = Color.white;
        itTmp.alignment = TextAlignmentOptions.MidlineLeft;
        itTmp.raycastTarget = false;

        giftcodeInput.textViewport = taRt;
        giftcodeInput.textComponent = itTmp;
        giftcodeInput.placeholder = phTmp;

        var trigger = ifObj.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener((eventData) => OpenMobileKeyboard());
        trigger.triggers.Add(entry);

        giftcodeInput.onSelect.AddListener((s) => OpenMobileKeyboard());
        giftcodeInput.onValueChanged.AddListener((s) => {
            AdjustInputFontSize(s);
            UpdateKeyboardPreview();
        });

        // Button DÁN MÃ (Paste)
        GameObject pasteBtnObj = new GameObject("Btn_Paste", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        pasteBtnObj.transform.SetParent(inputBar.transform, false);
        RectTransform pRt = pasteBtnObj.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0f, 0.5f);
        pRt.anchorMax = new Vector2(0f, 0.5f);
        pRt.pivot = new Vector2(0f, 0.5f);
        pRt.anchoredPosition = new Vector2(456f, 0f);
        pRt.sizeDelta = new Vector2(90f, 44f);

        Image pImg = pasteBtnObj.GetComponent<Image>();
        pImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        pImg.type = Image.Type.Sliced;
        pImg.color = new Color(0.20f, 0.28f, 0.40f, 0.95f);

        GameObject ptObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        ptObj.transform.SetParent(pasteBtnObj.transform, false);
        RectTransform ptRt = ptObj.GetComponent<RectTransform>();
        ptRt.anchorMin = Vector2.zero;
        ptRt.anchorMax = Vector2.one;
        ptRt.sizeDelta = Vector2.zero;
        var ptTmp = ptObj.AddComponent<TextMeshProUGUI>();
        ptTmp.text = isVi ? "<b>DÁN MÃ</b>" : "<b>PASTE</b>";
        ptTmp.fontSize = 12.5f;
        ptTmp.alignment = TextAlignmentOptions.Center;
        ptTmp.color = new Color(0.7f, 0.9f, 1f);

        Button pasteBtn = pasteBtnObj.GetComponent<Button>();
        pasteBtn.targetGraphic = pImg;
        pasteBtn.onClick.AddListener(OnPasteFromClipboard);

        // Button BÀN PHÍM ẢO (Virtual Keyboard)
        GameObject kbdBtnObj = new GameObject("Btn_ToggleKeyboard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        kbdBtnObj.transform.SetParent(inputBar.transform, false);
        RectTransform kbRt = kbdBtnObj.GetComponent<RectTransform>();
        kbRt.anchorMin = new Vector2(0f, 0.5f);
        kbRt.anchorMax = new Vector2(0f, 0.5f);
        kbRt.pivot = new Vector2(0f, 0.5f);
        kbRt.anchoredPosition = new Vector2(554f, 0f);
        kbRt.sizeDelta = new Vector2(100f, 44f);

        Image kbImg = kbdBtnObj.GetComponent<Image>();
        kbImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        kbImg.type = Image.Type.Sliced;
        kbImg.color = new Color(0.25f, 0.22f, 0.38f, 0.95f);

        GameObject kbtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        kbtObj.transform.SetParent(kbdBtnObj.transform, false);
        RectTransform kbtRt = kbtObj.GetComponent<RectTransform>();
        kbtRt.anchorMin = Vector2.zero;
        kbtRt.anchorMax = Vector2.one;
        kbtRt.sizeDelta = Vector2.zero;
        var kbtTmp = kbtObj.AddComponent<TextMeshProUGUI>();
        kbtTmp.text = isVi ? "<b>⌨ PHÍM</b>" : "<b>⌨ KEYBD</b>";
        kbtTmp.fontSize = 12.5f;
        kbtTmp.alignment = TextAlignmentOptions.Center;
        kbtTmp.color = new Color(0.9f, 0.85f, 1f);

        Button kbdBtn = kbdBtnObj.GetComponent<Button>();
        kbdBtn.targetGraphic = kbImg;
        kbdBtn.onClick.AddListener(ToggleVirtualKeyboard);

        // Button ĐỔI QUÀ NGAY (Redeem Now) - Radiant Gold / Emerald
        GameObject rdBtnObj = new GameObject("Btn_Redeem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        rdBtnObj.transform.SetParent(inputBar.transform, false);
        RectTransform rdRt = rdBtnObj.GetComponent<RectTransform>();
        rdRt.anchorMin = new Vector2(1f, 0.5f);
        rdRt.anchorMax = new Vector2(1f, 0.5f);
        rdRt.pivot = new Vector2(1f, 0.5f);
        rdRt.anchoredPosition = new Vector2(-16f, 0f);
        rdRt.sizeDelta = new Vector2(230f, 44f);

        Image rdImg = rdBtnObj.GetComponent<Image>();
        rdImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        rdImg.type = Image.Type.Sliced;
        rdImg.color = new Color(0.12f, 0.52f, 0.28f, 0.98f);

        var rdOut = rdBtnObj.AddComponent<Outline>();
        rdOut.effectColor = new Color(0.35f, 1f, 0.65f, 0.75f);
        rdOut.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject rdtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        rdtObj.transform.SetParent(rdBtnObj.transform, false);
        RectTransform rdtRt = rdtObj.GetComponent<RectTransform>();
        rdtRt.anchorMin = Vector2.zero;
        rdtRt.anchorMax = Vector2.one;
        rdtRt.sizeDelta = Vector2.zero;
        var rdtTmp = rdtObj.AddComponent<TextMeshProUGUI>();
        rdtTmp.text = isVi ? "<b>★ ĐỔI QUÀ NGAY</b>" : "<b>★ REDEEM NOW</b>";
        rdtTmp.fontSize = 14f;
        rdtTmp.alignment = TextAlignmentOptions.Center;
        rdtTmp.color = Color.white;

        Button rdBtn = rdBtnObj.GetComponent<Button>();
        rdBtn.targetGraphic = rdImg;
        rdBtn.onClick.AddListener(OnRedeemGiftcode);

        // Feedback Text Line
        GameObject fbObj = new GameObject("FeedbackText", typeof(RectTransform), typeof(CanvasRenderer));
        fbObj.transform.SetParent(mainCard.transform, false);
        RectTransform fbRt = fbObj.GetComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(0.5f, 1f);
        fbRt.anchorMax = new Vector2(0.5f, 1f);
        fbRt.pivot = new Vector2(0.5f, 1f);
        fbRt.anchoredPosition = new Vector2(0f, -160f);
        fbRt.sizeDelta = new Vector2(900f, 26f);

        giftcodeFeedbackText = fbObj.AddComponent<TextMeshProUGUI>();
        giftcodeFeedbackText.fontSize = 13.5f;
        giftcodeFeedbackText.alignment = TextAlignmentOptions.Center;
        giftcodeFeedbackText.color = new Color(0.35f, 0.95f, 0.85f);

        // Filigree Divider
        SoulslikeHUDStyler.AddFiligreeDivider(mainCard.transform, new Vector2(0f, -190f), new Vector2(920f, 6f));

        // Public Codes Section Header
        GameObject pcHeadObj = new GameObject("PublicCodesHeader", typeof(RectTransform), typeof(CanvasRenderer));
        pcHeadObj.transform.SetParent(mainCard.transform, false);
        RectTransform pchRt = pcHeadObj.GetComponent<RectTransform>();
        pchRt.anchorMin = new Vector2(0.5f, 1f);
        pchRt.anchorMax = new Vector2(0.5f, 1f);
        pchRt.pivot = new Vector2(0.5f, 1f);
        pchRt.anchoredPosition = new Vector2(0f, -206f);
        pchRt.sizeDelta = new Vector2(900f, 26f);

        var pchTmp = pcHeadObj.AddComponent<TextMeshProUGUI>();
        pchTmp.text = isVi 
            ? "<b>MÃ QUÀ TẶNG CÔNG KHAI CÓ SẴN (CHẠM ĐỂ NHẬN NHANH)</b>" 
            : "<b>AVAILABLE PUBLIC GIFTCODES (TAP TO INSTANTLY REDEEM)</b>";
        pchTmp.fontSize = 13f;
        pchTmp.color = new Color(1f, 0.88f, 0.45f);
        pchTmp.alignment = TextAlignmentOptions.Center;

        // Container for Public Codes Cards
        GameObject codesContainer = new GameObject("CodesGrid", typeof(RectTransform));
        codesContainer.transform.SetParent(mainCard.transform, false);
        RectTransform cgRt = codesContainer.GetComponent<RectTransform>();
        cgRt.anchorMin = new Vector2(0.5f, 0f);
        cgRt.anchorMax = new Vector2(0.5f, 0f);
        cgRt.pivot = new Vector2(0.5f, 0f);
        cgRt.anchoredPosition = new Vector2(0f, 18f);
        cgRt.sizeDelta = new Vector2(920f, 150f);

        publicCodesContainer = codesContainer.transform;
        PopulatePublicGiftcodeCards();
    }

    private void PopulatePublicGiftcodeCards()
    {
        if (publicCodesContainer == null) return;

        bool isVi = LocalizationManager.IsVietnamese;

        var sampleCodes = new (string code, string descVi, string descEn, int shards)[]
        {
            ("HUNGNE123", "Đặc Quyền Tác Giả", "Author Exclusive", 5000),
            ("VIP888", "Gói Siêu Cấp VIP", "Super VIP Pack", 2000),
            ("DARKFANTASY", "Tri Ân Cộng Đồng", "Community Tribute", 1500),
            ("VAELSTRIDER", "Hào Khí Chiến Binh", "Warrior Spirit", 1000)
        };

        float cardWidth = 220f;
        float cardHeight = 140f;
        float gap = 12f;
        float totalWidth = 4 * cardWidth + 3 * gap;
        float startX = -totalWidth * 0.5f + cardWidth * 0.5f;

        for (int i = 0; i < sampleCodes.Length; i++)
        {
            var item = sampleCodes[i];
            float xPos = startX + i * (cardWidth + gap);

            string usedKey = "Giftcode_Used_" + item.code;
            bool isUsed = PlayerPrefs.GetInt(usedKey, 0) == 1;

            GameObject card = new GameObject("CodeCard_" + item.code, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(publicCodesContainer, false);

            RectTransform cRt = card.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(xPos, 0f);
            cRt.sizeDelta = new Vector2(cardWidth, cardHeight);

            Image cImg = card.GetComponent<Image>();
            cImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
            cImg.type = Image.Type.Sliced;
            cImg.color = isUsed 
                ? new Color(0.05f, 0.07f, 0.10f, 0.85f) 
                : new Color(0.08f, 0.11f, 0.16f, 0.95f);

            var cOut = card.AddComponent<Outline>();
            cOut.effectColor = isUsed ? new Color(0.3f, 0.4f, 0.5f, 0.4f) : new Color(0.85f, 0.70f, 0.32f, 0.70f);
            cOut.effectDistance = new Vector2(1f, -1f);

            // Code Text
            GameObject ctObj = new GameObject("CodeName", typeof(RectTransform), typeof(CanvasRenderer));
            ctObj.transform.SetParent(card.transform, false);
            RectTransform ctRt = ctObj.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0.5f, 1f);
            ctRt.anchorMax = new Vector2(0.5f, 1f);
            ctRt.pivot = new Vector2(0.5f, 1f);
            ctRt.anchoredPosition = new Vector2(0f, -10f);
            ctRt.sizeDelta = new Vector2(cardWidth - 16f, 24f);

            var ctTmp = ctObj.AddComponent<TextMeshProUGUI>();
            ctTmp.text = $"<b>{item.code}</b>";
            ctTmp.fontSize = 14f;
            ctTmp.alignment = TextAlignmentOptions.Center;
            ctTmp.color = isUsed ? new Color(0.6f, 0.7f, 0.8f) : new Color(1f, 0.88f, 0.45f);

            // Description
            GameObject cdObj = new GameObject("Desc", typeof(RectTransform), typeof(CanvasRenderer));
            cdObj.transform.SetParent(card.transform, false);
            RectTransform cdRt = cdObj.GetComponent<RectTransform>();
            cdRt.anchorMin = new Vector2(0.5f, 1f);
            cdRt.anchorMax = new Vector2(0.5f, 1f);
            cdRt.pivot = new Vector2(0.5f, 1f);
            cdRt.anchoredPosition = new Vector2(0f, -34f);
            cdRt.sizeDelta = new Vector2(cardWidth - 16f, 20f);

            var cdTmp = cdObj.AddComponent<TextMeshProUGUI>();
            cdTmp.text = isVi ? item.descVi : item.descEn;
            cdTmp.fontSize = 11f;
            cdTmp.alignment = TextAlignmentOptions.Center;
            cdTmp.color = new Color(0.65f, 0.75f, 0.88f);

            // Shards Value
            GameObject csObj = new GameObject("Shards", typeof(RectTransform), typeof(CanvasRenderer));
            csObj.transform.SetParent(card.transform, false);
            RectTransform csRt = csObj.GetComponent<RectTransform>();
            csRt.anchorMin = new Vector2(0.5f, 1f);
            csRt.anchorMax = new Vector2(0.5f, 1f);
            csRt.pivot = new Vector2(0.5f, 1f);
            csRt.anchoredPosition = new Vector2(0f, -54f);
            csRt.sizeDelta = new Vector2(cardWidth - 16f, 22f);

            var csTmp = csObj.AddComponent<TextMeshProUGUI>();
            csTmp.text = $"<color=#55EFC4><b>+{item.shards:N0} Linh Hồn</b></color>";
            csTmp.fontSize = 12f;
            csTmp.alignment = TextAlignmentOptions.Center;

            // 1-Click Action Button
            GameObject actBtnObj = new GameObject("Btn_UseCode", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            actBtnObj.transform.SetParent(card.transform, false);
            RectTransform abRt = actBtnObj.GetComponent<RectTransform>();
            abRt.anchorMin = new Vector2(0.5f, 0f);
            abRt.anchorMax = new Vector2(0.5f, 0f);
            abRt.pivot = new Vector2(0.5f, 0f);
            abRt.anchoredPosition = new Vector2(0f, 10f);
            abRt.sizeDelta = new Vector2(cardWidth - 24f, 34f);

            Image abImg = actBtnObj.GetComponent<Image>();
            abImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
            abImg.type = Image.Type.Sliced;
            abImg.color = isUsed 
                ? new Color(0.20f, 0.24f, 0.30f, 0.95f) 
                : new Color(0.12f, 0.46f, 0.26f, 0.98f);

            GameObject abtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            abtObj.transform.SetParent(actBtnObj.transform, false);
            RectTransform abtRt = abtObj.GetComponent<RectTransform>();
            abtRt.anchorMin = Vector2.zero;
            abtRt.anchorMax = Vector2.one;
            abtRt.sizeDelta = Vector2.zero;

            var abtTmp = abtObj.AddComponent<TextMeshProUGUI>();
            abtTmp.text = isUsed 
                ? (isVi ? "<b>ĐÃ DÙNG</b>" : "<b>USED</b>") 
                : (isVi ? "<b>SỬ DỤNG</b>" : "<b>USE CODE</b>");
            abtTmp.fontSize = 12f;
            abtTmp.alignment = TextAlignmentOptions.Center;
            abtTmp.color = isUsed ? new Color(0.6f, 0.7f, 0.8f) : Color.white;

            Button actBtn = actBtnObj.GetComponent<Button>();
            actBtn.targetGraphic = abImg;
            actBtn.interactable = !isUsed;

            string targetCode = item.code;
            actBtn.onClick.AddListener(() => {
                if (giftcodeInput != null) giftcodeInput.text = targetCode;
                OnRedeemGiftcode();
                PopulatePublicGiftcodeCards();
            });
        }
    }

    private void OnPasteFromClipboard()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        string clip = GUIUtility.systemCopyBuffer;
        if (!string.IsNullOrEmpty(clip) && giftcodeInput != null)
        {
            giftcodeInput.text = clip.Trim().ToUpperInvariant();
            AdjustInputFontSize(giftcodeInput.text);
            if (giftcodeFeedbackText != null)
            {
                giftcodeFeedbackText.text = LocalizationManager.IsVietnamese 
                    ? "<color=#55EFC4>Đã dán mã quà tặng từ bộ nhớ tạm!</color>" 
                    : "<color=#55EFC4>Pasted giftcode from clipboard!</color>";
            }
        }
        else if (giftcodeFeedbackText != null)
        {
            giftcodeFeedbackText.text = LocalizationManager.IsVietnamese 
                ? "<color=#FF6B6B>Bộ nhớ tạm đang trống!</color>" 
                : "<color=#FF6B6B>Clipboard is empty!</color>";
        }
    }

    private void OnRedeemGiftcode()
    {
        if (giftcodeInput == null) return;
        string rawCode = giftcodeInput.text.Trim().ToUpperInvariant();
        bool isVi = LocalizationManager.IsVietnamese;

        if (string.IsNullOrEmpty(rawCode))
        {
            if (giftcodeFeedbackText != null)
            {
                giftcodeFeedbackText.text = isVi ? "<color=#FF6B6B>Vui lòng nhập mã quà tặng!</color>" : "<color=#FF6B6B>Please enter a giftcode!</color>";
            }
            return;
        }

        string usedKey = "Giftcode_Used_" + rawCode;
        if (PlayerPrefs.GetInt(usedKey, 0) == 1)
        {
            if (giftcodeFeedbackText != null)
            {
                giftcodeFeedbackText.text = isVi ? "<color=#FF6B6B>Mã này đã được sử dụng rồi!</color>" : "<color=#FF6B6B>This code has already been used!</color>";
            }
            return;
        }

        int reward = 0;
        if (rawCode == "HUNGNE123")
        {
            reward = 5000;
        }
        else if (rawCode == "VIP888")
        {
            reward = 2000;
        }
        else if (rawCode == "DARKFANTASY")
        {
            reward = 1500;
        }
        else if (rawCode == "VAELSTRIDER" || rawCode == "RUINBOUND" || rawCode == "DISCORD2026")
        {
            reward = 1000;
        }
        else if (rawCode == "NEWBIE2026" || rawCode == "SHADOWFIGHT")
        {
            reward = 500;
        }
        else
        {
            if (giftcodeFeedbackText != null)
            {
                giftcodeFeedbackText.text = isVi ? "<color=#FF6B6B>Mã không hợp lệ hoặc đã hết hạn!</color>" : "<color=#FF6B6B>Invalid or expired code!</color>";
            }
            return;
        }

        AccountDatabase.AddAethelShards(reward);
        PlayerPrefs.SetInt(usedKey, 1);
        PlayerPrefs.Save();

        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();
        if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();

        if (giftcodeFeedbackText != null)
        {
            giftcodeFeedbackText.text = isVi 
                ? $"<color=#55EFC4><b>Thành công! Đã nhận +{reward:N0} Mảnh Linh Hồn!</b></color>"
                : $"<color=#55EFC4><b>Success! Received +{reward:N0} Soul Shards!</b></color>";
        }

        giftcodeInput.text = "";
        UpdateKeyboardPreview();
        PopulatePublicGiftcodeCards();
    }

    public static void UpdateLobbyBadge()
    {
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.UpdateMailboxBadge();
        }
    }

    // =========================================================================
    // PHONE VIRTUAL KEYBOARD & TOUCH INPUT
    // =========================================================================

    public void OpenMobileKeyboard()
    {
        if (virtualKeyboardObj != null)
        {
            virtualKeyboardObj.SetActive(true);
            UpdateKeyboardPreview();
        }

        if (modalWindowRect != null)
        {
            modalWindowRect.anchoredPosition = new Vector2(0f, 135f);
        }

        if (giftcodeInput != null)
        {
            giftcodeInput.Select();
            giftcodeInput.ActivateInputField();
        }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        if (TouchScreenKeyboard.isSupported)
        {
            try
            {
                bool isVi = LocalizationManager.IsVietnamese;
                string promptTitle = isVi ? "Nhập mã quà tặng (VD: HUNGNE123, VIP888...)" : "Enter Giftcode";

                mobileKeyboard = TouchScreenKeyboard.Open(
                    giftcodeInput != null ? giftcodeInput.text : "",
                    TouchScreenKeyboardType.Default,
                    false,
                    false,
                    false,
                    false,
                    promptTitle
                );
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[MailboxUI] TouchScreenKeyboard warning: " + e.Message);
                mobileKeyboard = null;
            }
        }
#endif
    }

    public void CloseVirtualKeyboard()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (virtualKeyboardObj != null)
        {
            virtualKeyboardObj.SetActive(false);
        }
        if (modalWindowRect != null)
        {
            modalWindowRect.anchoredPosition = Vector2.zero;
        }
    }

    public void ToggleVirtualKeyboard()
    {
        if (virtualKeyboardObj != null && virtualKeyboardObj.activeSelf)
        {
            CloseVirtualKeyboard();
        }
        else
        {
            OpenMobileKeyboard();
        }
    }

    private void AdjustInputFontSize(string s)
    {
        if (giftcodeInput == null || giftcodeInput.textComponent == null) return;
        int len = string.IsNullOrEmpty(s) ? 0 : s.Length;
        if (len >= 18)
        {
            giftcodeInput.textComponent.fontSize = 9f;
        }
        else if (len >= 14)
        {
            giftcodeInput.textComponent.fontSize = 11f;
        }
        else
        {
            giftcodeInput.textComponent.fontSize = 14f;
        }
    }

    private void OnVirtualKeyPress(char c)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (giftcodeInput == null) return;
        if (giftcodeInput.text.Length >= giftcodeInput.characterLimit) return;

        giftcodeInput.text += char.ToUpperInvariant(c);
        AdjustInputFontSize(giftcodeInput.text);
        UpdateKeyboardPreview();
    }

    private void OnVirtualKeyBackspace()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (giftcodeInput == null) return;
        if (!string.IsNullOrEmpty(giftcodeInput.text))
        {
            giftcodeInput.text = giftcodeInput.text.Substring(0, giftcodeInput.text.Length - 1);
            AdjustInputFontSize(giftcodeInput.text);
            UpdateKeyboardPreview();
        }
    }

    private void UpdateKeyboardPreview()
    {
        if (keyboardPreviewText == null) return;
        bool isVi = LocalizationManager.IsVietnamese;
        string current = giftcodeInput != null ? giftcodeInput.text : "";
        string label = isVi ? "MÃ:" : "CODE:";
        if (string.IsNullOrEmpty(current))
        {
            keyboardPreviewText.text = $"<b>{label}</b> <color=#718096><i>{(isVi ? "Chạm các phím bên dưới để nhập..." : "Tap keys below to type...")}</i></color>";
        }
        else
        {
            keyboardPreviewText.text = $"<b>{label}</b> <color=#FFE066><b>{current}</b></color><color=#74B9FF>_</color>";
        }
    }

    private void BuildVirtualKeyboard(Transform parent)
    {
        bool isVi = LocalizationManager.IsVietnamese;

        virtualKeyboardObj = new GameObject("VirtualKeyboard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        virtualKeyboardObj.layer = 5;
        virtualKeyboardObj.transform.SetParent(parent, false);

        RectTransform vkRt = virtualKeyboardObj.GetComponent<RectTransform>();
        vkRt.anchorMin = new Vector2(0.5f, 0f);
        vkRt.anchorMax = new Vector2(0.5f, 0f);
        vkRt.pivot = new Vector2(0.5f, 0f);
        vkRt.anchoredPosition = new Vector2(0f, 10f);
        vkRt.sizeDelta = new Vector2(960f, 320f);

        Image vkImg = virtualKeyboardObj.GetComponent<Image>();
        vkImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        vkImg.type = Image.Type.Sliced;
        vkImg.color = new Color(0.04f, 0.06f, 0.10f, 0.98f);

        var vkOut = virtualKeyboardObj.AddComponent<Outline>();
        vkOut.effectColor = new Color(0.35f, 0.60f, 0.85f, 0.80f);
        vkOut.effectDistance = new Vector2(2f, -2f);

        // Header / Display Bar (Height 40px)
        GameObject dispBar = new GameObject("DisplayBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dispBar.transform.SetParent(virtualKeyboardObj.transform, false);
        RectTransform dbRt = dispBar.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(0f, 1f);
        dbRt.anchorMax = new Vector2(1f, 1f);
        dbRt.pivot = new Vector2(0.5f, 1f);
        dbRt.anchoredPosition = new Vector2(0f, -8f);
        dbRt.sizeDelta = new Vector2(-16f, 40f);

        Image dbImg = dispBar.GetComponent<Image>();
        dbImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        dbImg.type = Image.Type.Sliced;
        dbImg.color = new Color(0.02f, 0.03f, 0.06f, 0.95f);

        GameObject titObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        titObj.transform.SetParent(dispBar.transform, false);
        RectTransform titRt = titObj.GetComponent<RectTransform>();
        titRt.anchorMin = new Vector2(0f, 0.5f);
        titRt.anchorMax = new Vector2(0f, 0.5f);
        titRt.pivot = new Vector2(0f, 0.5f);
        titRt.anchoredPosition = new Vector2(14f, 0f);
        titRt.sizeDelta = new Vector2(150f, 32f);

        var titTmp = titObj.AddComponent<TextMeshProUGUI>();
        titTmp.text = isVi ? "<b>BÀN PHÍM</b>" : "<b>KEYBOARD</b>";
        titTmp.fontSize = 13f;
        titTmp.color = new Color(1f, 0.88f, 0.45f);
        titTmp.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject prevObj = new GameObject("PreviewText", typeof(RectTransform), typeof(RectMask2D));
        prevObj.transform.SetParent(dispBar.transform, false);
        RectTransform prevRt = prevObj.GetComponent<RectTransform>();
        prevRt.anchorMin = new Vector2(0f, 0.5f);
        prevRt.anchorMax = new Vector2(1f, 0.5f);
        prevRt.offsetMin = new Vector2(140f, -16f);
        prevRt.offsetMax = new Vector2(-160f, 16f);

        keyboardPreviewText = prevObj.AddComponent<TextMeshProUGUI>();
        keyboardPreviewText.fontSize = 13.5f;
        keyboardPreviewText.enableAutoSizing = true;
        keyboardPreviewText.fontSizeMin = 8.5f;
        keyboardPreviewText.fontSizeMax = 13.5f;
        keyboardPreviewText.overflowMode = TextOverflowModes.Ellipsis;
        keyboardPreviewText.alignment = TextAlignmentOptions.MidlineLeft;
        keyboardPreviewText.color = Color.white;

        GameObject hideBtnObj = new GameObject("Btn_HideKeyboard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        hideBtnObj.transform.SetParent(dispBar.transform, false);
        RectTransform hbRt = hideBtnObj.GetComponent<RectTransform>();
        hbRt.anchorMin = new Vector2(1f, 0.5f);
        hbRt.anchorMax = new Vector2(1f, 0.5f);
        hbRt.pivot = new Vector2(1f, 0.5f);
        hbRt.anchoredPosition = new Vector2(-8f, 0f);
        hbRt.sizeDelta = new Vector2(105f, 32f);

        Image hbImg = hideBtnObj.GetComponent<Image>();
        hbImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        hbImg.type = Image.Type.Sliced;
        hbImg.color = new Color(0.38f, 0.16f, 0.18f, 0.95f);

        GameObject hbtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        hbtObj.transform.SetParent(hideBtnObj.transform, false);
        RectTransform hbtRt = hbtObj.GetComponent<RectTransform>();
        hbtRt.anchorMin = Vector2.zero;
        hbtRt.anchorMax = Vector2.one;
        hbtRt.sizeDelta = Vector2.zero;
        var hbtTmp = hbtObj.AddComponent<TextMeshProUGUI>();
        hbtTmp.text = isVi ? "<b>THU GỌN</b>" : "<b>HIDE</b>";
        hbtTmp.fontSize = 12f;
        hbtTmp.alignment = TextAlignmentOptions.Center;
        hbtTmp.color = Color.white;

        Button hideBtn = hideBtnObj.GetComponent<Button>();
        hideBtn.targetGraphic = hbImg;
        hideBtn.onClick.AddListener(CloseVirtualKeyboard);

        // Row 1: Numbers
        string[] row1 = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        CreateKeyRow(virtualKeyboardObj.transform, row1, -54f, 86f, 42f, 7f);

        // Row 2: QWERTY
        string[] row2 = new string[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
        CreateKeyRow(virtualKeyboardObj.transform, row2, -102f, 86f, 42f, 7f);

        // Row 3: ASDF
        string[] row3 = new string[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" };
        CreateKeyRow(virtualKeyboardObj.transform, row3, -150f, 94f, 42f, 8f);

        // Row 4: ZXCV + Backspace
        CreateRow4WithBackspace(virtualKeyboardObj.transform, -198f);

        // Row 5: Action Row
        CreateActionRow(virtualKeyboardObj.transform, -248f, isVi);

        virtualKeyboardObj.SetActive(false);
    }

    private void CreateKeyRow(Transform parent, string[] keys, float yPos, float keyWidth, float keyHeight, float gap)
    {
        int count = keys.Length;
        float totalWidth = count * keyWidth + (count - 1) * gap;
        float startX = -totalWidth * 0.5f + keyWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            string keyChar = keys[i];
            float xPos = startX + i * (keyWidth + gap);

            GameObject keyObj = new GameObject("Key_" + keyChar, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            keyObj.transform.SetParent(parent, false);

            RectTransform kRt = keyObj.GetComponent<RectTransform>();
            kRt.anchorMin = new Vector2(0.5f, 1f);
            kRt.anchorMax = new Vector2(0.5f, 1f);
            kRt.pivot = new Vector2(0.5f, 0.5f);
            kRt.anchoredPosition = new Vector2(xPos, yPos - keyHeight * 0.5f);
            kRt.sizeDelta = new Vector2(keyWidth, keyHeight);

            Image kImg = keyObj.GetComponent<Image>();
            kImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
            kImg.type = Image.Type.Sliced;
            kImg.color = new Color(0.16f, 0.22f, 0.32f, 0.96f);

            var kOut = keyObj.AddComponent<Outline>();
            kOut.effectColor = new Color(0.35f, 0.55f, 0.80f, 0.50f);
            kOut.effectDistance = new Vector2(1f, -1f);

            GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            tObj.transform.SetParent(keyObj.transform, false);
            RectTransform tRt = tObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            var tmp = tObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{keyChar}</b>";
            tmp.fontSize = 19f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.92f, 0.95f, 1f);

            Button btn = keyObj.GetComponent<Button>();
            btn.targetGraphic = kImg;
            char c = keyChar[0];
            btn.onClick.AddListener(() => OnVirtualKeyPress(c));
        }
    }

    private void CreateRow4WithBackspace(Transform parent, float yPos)
    {
        string[] letters = new string[] { "Z", "X", "C", "V", "B", "N", "M" };
        float keyWidth = 92f;
        float keyHeight = 42f;
        float gap = 8f;
        float backspaceWidth = 170f;

        float totalWidth = 7 * keyWidth + 6 * gap + gap + backspaceWidth;
        float startX = -totalWidth * 0.5f + keyWidth * 0.5f;

        for (int i = 0; i < 7; i++)
        {
            string keyChar = letters[i];
            float xPos = startX + i * (keyWidth + gap);

            GameObject keyObj = new GameObject("Key_" + keyChar, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            keyObj.transform.SetParent(parent, false);

            RectTransform kRt = keyObj.GetComponent<RectTransform>();
            kRt.anchorMin = new Vector2(0.5f, 1f);
            kRt.anchorMax = new Vector2(0.5f, 1f);
            kRt.pivot = new Vector2(0.5f, 0.5f);
            kRt.anchoredPosition = new Vector2(xPos, yPos - keyHeight * 0.5f);
            kRt.sizeDelta = new Vector2(keyWidth, keyHeight);

            Image kImg = keyObj.GetComponent<Image>();
            kImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
            kImg.type = Image.Type.Sliced;
            kImg.color = new Color(0.16f, 0.22f, 0.32f, 0.96f);

            var kOut = keyObj.AddComponent<Outline>();
            kOut.effectColor = new Color(0.35f, 0.55f, 0.80f, 0.50f);
            kOut.effectDistance = new Vector2(1f, -1f);

            GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            tObj.transform.SetParent(keyObj.transform, false);
            RectTransform tRt = tObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            var tmp = tObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{keyChar}</b>";
            tmp.fontSize = 19f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.92f, 0.95f, 1f);

            Button btn = keyObj.GetComponent<Button>();
            btn.targetGraphic = kImg;
            char c = keyChar[0];
            btn.onClick.AddListener(() => OnVirtualKeyPress(c));
        }

        // Backspace
        float bsX = startX + 6 * (keyWidth + gap) + keyWidth * 0.5f + gap + backspaceWidth * 0.5f;
        GameObject bsObj = new GameObject("Key_Backspace", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        bsObj.transform.SetParent(parent, false);

        RectTransform bsRt = bsObj.GetComponent<RectTransform>();
        bsRt.anchorMin = new Vector2(0.5f, 1f);
        bsRt.anchorMax = new Vector2(0.5f, 1f);
        bsRt.pivot = new Vector2(0.5f, 0.5f);
        bsRt.anchoredPosition = new Vector2(bsX, yPos - keyHeight * 0.5f);
        bsRt.sizeDelta = new Vector2(backspaceWidth, keyHeight);

        Image bsImg = bsObj.GetComponent<Image>();
        bsImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        bsImg.type = Image.Type.Sliced;
        bsImg.color = new Color(0.38f, 0.18f, 0.20f, 0.96f);

        var bsOut = bsObj.AddComponent<Outline>();
        bsOut.effectColor = new Color(0.85f, 0.40f, 0.40f, 0.65f);
        bsOut.effectDistance = new Vector2(1f, -1f);

        GameObject bstObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        bstObj.transform.SetParent(bsObj.transform, false);
        RectTransform bstRt = bstObj.GetComponent<RectTransform>();
        bstRt.anchorMin = Vector2.zero;
        bstRt.anchorMax = Vector2.one;
        bstRt.sizeDelta = Vector2.zero;

        var bstTmp = bstObj.AddComponent<TextMeshProUGUI>();
        bstTmp.text = LocalizationManager.IsVietnamese ? "<b>XÓA</b>" : "<b>BACK</b>";
        bstTmp.fontSize = 14f;
        bstTmp.alignment = TextAlignmentOptions.Center;
        bstTmp.color = Color.white;

        Button bsBtn = bsObj.GetComponent<Button>();
        bsBtn.targetGraphic = bsImg;
        bsBtn.onClick.AddListener(OnVirtualKeyBackspace);
    }

    private void CreateActionRow(Transform parent, float yPos, bool isVi)
    {
        float keyHeight = 44f;
        float gap = 12f;
        float w1 = 180f; // Paste
        float w2 = 220f; // Space
        float w3 = 240f; // Redeem

        float totalWidth = w1 + w2 + w3 + 2 * gap;
        float startX = -totalWidth * 0.5f;

        // 1. Paste Button
        float x1 = startX + w1 * 0.5f;
        CreateSingleActionButton(parent, "Btn_VkPaste", isVi ? "<b>DÁN MÃ</b>" : "<b>PASTE</b>",
            new Vector2(x1, yPos - keyHeight * 0.5f), new Vector2(w1, keyHeight),
            new Color(0.20f, 0.26f, 0.38f, 0.96f), new Color(0.4f, 0.7f, 1f, 0.6f),
            OnPasteFromClipboard);

        // 2. Space Bar
        float x2 = startX + w1 + gap + w2 * 0.5f;
        CreateSingleActionButton(parent, "Btn_VkSpace", isVi ? "<b>DẤU CÁCH</b>" : "<b>SPACE</b>",
            new Vector2(x2, yPos - keyHeight * 0.5f), new Vector2(w2, keyHeight),
            new Color(0.18f, 0.23f, 0.30f, 0.96f), new Color(0.4f, 0.55f, 0.75f, 0.5f),
            () => OnVirtualKeyPress(' '));

        // 3. Redeem Button
        float x3 = startX + w1 + gap + w2 + gap + w3 * 0.5f;
        CreateSingleActionButton(parent, "Btn_VkRedeem", isVi ? "<b>ĐỔI QUÀ NGAY</b>" : "<b>REDEEM NOW</b>",
            new Vector2(x3, yPos - keyHeight * 0.5f), new Vector2(w3, keyHeight),
            new Color(0.10f, 0.46f, 0.24f, 0.98f), new Color(0.35f, 1f, 0.65f, 0.75f),
            OnRedeemGiftcode);
    }

    private void CreateSingleActionButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor, Color outColor, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = obj.GetComponent<Image>();
        img.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        img.type = Image.Type.Sliced;
        img.color = bgColor;

        var outComp = obj.AddComponent<Outline>();
        outComp.effectColor = outColor;
        outComp.effectDistance = new Vector2(1.2f, -1.2f);

        GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        tObj.transform.SetParent(obj.transform, false);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        var tmp = tObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        Button btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);
    }

    void Update()
    {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        if (mobileKeyboard != null)
        {
            try
            {
                if (mobileKeyboard.active && giftcodeInput != null)
                {
                    string kText = mobileKeyboard.text;
                    if (!string.IsNullOrEmpty(kText))
                    {
                        giftcodeInput.text = kText.ToUpperInvariant();
                    }
                }

                if (mobileKeyboard.status == TouchScreenKeyboard.Status.Done)
                {
                    if (giftcodeInput != null && !string.IsNullOrEmpty(giftcodeInput.text))
                    {
                        OnRedeemGiftcode();
                    }
                    mobileKeyboard = null;
                }
                else if (mobileKeyboard.status == TouchScreenKeyboard.Status.Canceled || mobileKeyboard.status == TouchScreenKeyboard.Status.LostFocus)
                {
                    mobileKeyboard = null;
                }
            }
            catch
            {
                mobileKeyboard = null;
            }
        }
#endif
    }

    private void CloseMailbox()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        var cb = onCloseCallback;
        Destroy(gameObject);
        cb?.Invoke();
    }
}
