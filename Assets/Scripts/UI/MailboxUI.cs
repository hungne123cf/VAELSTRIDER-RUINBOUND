using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class MailboxUI : MonoBehaviour
{
    public static MailboxUI Instance { get; private set; }

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
    }

    private static List<MailItemData> _mails = new List<MailItemData>();

    private System.Action onCloseCallback;
    private MailItemData selectedMail;

    // UI References
    private Transform mailListContent;
    private TextMeshProUGUI emptyMailNoticeText;
    private TextMeshProUGUI detailTitleText;
    private TextMeshProUGUI detailSenderText;
    private TextMeshProUGUI detailDateText;
    private TextMeshProUGUI detailBodyText;
    private GameObject detailRewardBox;
    private TextMeshProUGUI detailRewardValText;
    private Button detailClaimButton;
    private TextMeshProUGUI detailClaimBtnText;
    private Button detailDeleteButton;

    private TMP_InputField giftcodeInput;
    private TouchScreenKeyboard mobileKeyboard;
    private TextMeshProUGUI giftcodeFeedbackText;
    private GameObject virtualKeyboardObj;
    private TextMeshProUGUI keyboardPreviewText;
    private RectTransform modalWindowRect;

    public static void InitializeDefaultMails()
    {
        _mails.Clear();

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
            date = "04/11/2026"
        });

        // 2. Thư Cập Nhật Hệ Thống
        _mails.Add(new MailItemData
        {
            id = "mail_patch_1_3",
            titleVi = "[CẬP NHẬT v1.3] ĐIỀU KHIỂN & ĐIỂM DANH MỚI",
            titleEn = "[UPDATE v1.3] NEW CONTROLS & ATTENDANCE",
            senderVi = "Đội Ngũ Phát Triển",
            senderEn = "Dev Team",
            contentVi = "Bản cập nhật v1.3 chính thức ra mắt hệ thống Tùy chỉnh nút cảm ứng trực tiếp trong trận đấu, Điểm danh nhận quà mỗi 07:00 sáng và Hòm thư hiệp sĩ.\n\nCảm ơn bạn đã luôn đồng hành cùng Vaelstrider!",
            contentEn = "Update v1.3 introduces live in-match controls customization, 07:00 AM Daily Attendance, and the Knight's Mailbox.\n\nThank you for supporting Vaelstrider!",
            rewardShards = 500,
            date = "04/11/2026"
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
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        bg.raycastTarget = true;

        Instance = mbObj.AddComponent<MailboxUI>();
        Instance.onCloseCallback = onClose;
        Instance.BuildUI();
    }

    private void BuildUI()
    {
        if (_mails.Count == 0) InitializeDefaultMails();
        bool isVi = LocalizationManager.IsVietnamese;

        // Modal Window
        GameObject win = new GameObject("Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        win.layer = 5;
        win.transform.SetParent(transform, false);

        RectTransform wRt = win.GetComponent<RectTransform>();
        modalWindowRect = wRt;
        wRt.anchorMin = new Vector2(0.5f, 0.5f);
        wRt.anchorMax = new Vector2(0.5f, 0.5f);
        wRt.pivot = new Vector2(0.5f, 0.5f);
        wRt.anchoredPosition = Vector2.zero;
        wRt.sizeDelta = new Vector2(980f, 620f);

        Image winImg = win.GetComponent<Image>();
        winImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        winImg.type = Image.Type.Sliced;
        winImg.color = new Color(0.05f, 0.07f, 0.12f, 0.98f);

        var winOut = win.AddComponent<Outline>();
        winOut.effectColor = new Color(0.35f, 0.60f, 0.85f, 0.70f);
        winOut.effectDistance = new Vector2(2f, -2f);

        // Header Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        titleObj.transform.SetParent(win.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 1f);
        tRt.anchorMax = new Vector2(0.5f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -20f);
        tRt.sizeDelta = new Vector2(900f, 36f);

        var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
        tTmp.text = isVi ? "<b>HÒM THƯ HIỆP SĨ</b>" : "<b>KNIGHT'S MAILBOX</b>";
        tTmp.fontSize = 24f;
        tTmp.color = new Color(1f, 0.88f, 0.45f);
        tTmp.alignment = TextAlignmentOptions.Center;

        // Subtitle
        GameObject subObj = new GameObject("SubTitle", typeof(RectTransform), typeof(CanvasRenderer));
        subObj.transform.SetParent(win.transform, false);
        RectTransform stRt = subObj.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0.5f, 1f);
        stRt.anchorMax = new Vector2(0.5f, 1f);
        stRt.pivot = new Vector2(0.5f, 1f);
        stRt.anchoredPosition = new Vector2(0f, -54f);
        stRt.sizeDelta = new Vector2(900f, 24f);

        var stTmp = subObj.AddComponent<TextMeshProUGUI>();
        stTmp.text = isVi ? "Tin nhắn, Quà tặng & Mã phần thưởng từ Hội Đồng Vaelstrider" : "Messages, Gifts & Rewards from the Council of Vaelstrider";
        stTmp.fontSize = 13.5f;
        stTmp.color = new Color(0.70f, 0.80f, 0.92f, 0.8f);
        stTmp.alignment = TextAlignmentOptions.Center;

        // Close Button (ASCII X)
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
        ctTmp.text = "<b>X</b>";
        ctTmp.fontSize = 24f;
        ctTmp.fontStyle = FontStyles.Bold;
        ctTmp.alignment = TextAlignmentOptions.Center;
        ctTmp.color = Color.white;

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.targetGraphic = cImg;
        closeBtn.onClick.AddListener(CloseMailbox);

        // --- LEFT COLUMN: Mail List (width 380px) ---
        GameObject leftCol = new GameObject("LeftColumn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        leftCol.transform.SetParent(win.transform, false);
        RectTransform lcRt = leftCol.GetComponent<RectTransform>();
        lcRt.anchorMin = new Vector2(0f, 0f);
        lcRt.anchorMax = new Vector2(0f, 1f);
        lcRt.pivot = new Vector2(0f, 0.5f);
        lcRt.anchoredPosition = new Vector2(25f, -20f);
        lcRt.sizeDelta = new Vector2(380f, -145f);

        Image lcImg = leftCol.GetComponent<Image>();
        lcImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        lcImg.type = Image.Type.Sliced;
        lcImg.color = new Color(0.03f, 0.05f, 0.08f, 0.85f);

        // Header inside Left Col with "XÓA ĐÃ ĐỌC" button
        GameObject listHeadObj = new GameObject("ListHeader", typeof(RectTransform));
        listHeadObj.transform.SetParent(leftCol.transform, false);
        RectTransform lhRt = listHeadObj.GetComponent<RectTransform>();
        lhRt.anchorMin = new Vector2(0f, 1f);
        lhRt.anchorMax = new Vector2(1f, 1f);
        lhRt.offsetMin = new Vector2(8f, -42f);
        lhRt.offsetMax = new Vector2(-8f, -8f);

        GameObject lhTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        lhTxtObj.transform.SetParent(listHeadObj.transform, false);
        RectTransform lhtRt = lhTxtObj.GetComponent<RectTransform>();
        lhtRt.anchorMin = new Vector2(0f, 0f);
        lhtRt.anchorMax = new Vector2(0f, 1f);
        lhtRt.pivot = new Vector2(0f, 0.5f);
        lhtRt.anchoredPosition = new Vector2(4f, 0f);
        lhtRt.sizeDelta = new Vector2(185f, 0f);

        var lhtTmp = lhTxtObj.AddComponent<TextMeshProUGUI>();
        lhtTmp.text = isVi ? "<b>DANH SÁCH THƯ</b>" : "<b>MAIL LIST</b>";
        lhtTmp.fontSize = 13.5f;
        lhtTmp.color = new Color(0.85f, 0.90f, 0.96f);
        lhtTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Button "XÓA ĐÃ ĐỌC" (Delete Read Mails)
        GameObject delAllBtnObj = new GameObject("Btn_DeleteRead", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        delAllBtnObj.transform.SetParent(listHeadObj.transform, false);
        RectTransform daRt = delAllBtnObj.GetComponent<RectTransform>();
        daRt.anchorMin = new Vector2(1f, 0.5f);
        daRt.anchorMax = new Vector2(1f, 0.5f);
        daRt.pivot = new Vector2(1f, 0.5f);
        daRt.anchoredPosition = new Vector2(-2f, 0f);
        daRt.sizeDelta = new Vector2(138f, 30f);

        Image daImg = delAllBtnObj.GetComponent<Image>();
        daImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        daImg.type = Image.Type.Sliced;
        daImg.color = new Color(0.32f, 0.14f, 0.16f, 0.95f);

        var daOut = delAllBtnObj.AddComponent<Outline>();
        daOut.effectColor = new Color(0.85f, 0.3f, 0.3f, 0.6f);
        daOut.effectDistance = new Vector2(1f, -1f);

        GameObject datObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        datObj.transform.SetParent(delAllBtnObj.transform, false);
        RectTransform datRt = datObj.GetComponent<RectTransform>();
        datRt.anchorMin = Vector2.zero;
        datRt.anchorMax = Vector2.one;
        datRt.sizeDelta = Vector2.zero;

        var datTmp = datObj.AddComponent<TextMeshProUGUI>();
        datTmp.text = isVi ? "<b>XÓA ĐÃ ĐỌC</b>" : "<b>DELETE READ</b>";
        datTmp.fontSize = 12f;
        datTmp.alignment = TextAlignmentOptions.Center;
        datTmp.color = Color.white;

        Button delAllBtn = delAllBtnObj.GetComponent<Button>();
        delAllBtn.targetGraphic = daImg;
        delAllBtn.onClick.AddListener(OnDeleteAllReadMails);

        // Scroll Container for mail items
        GameObject itemsContainer = new GameObject("ItemsScrollContent", typeof(RectTransform));
        itemsContainer.transform.SetParent(leftCol.transform, false);
        RectTransform icRt = itemsContainer.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0f, 0f);
        icRt.anchorMax = new Vector2(1f, 1f);
        icRt.offsetMin = new Vector2(6f, 8f);
        icRt.offsetMax = new Vector2(-6f, -48f);

        mailListContent = itemsContainer.transform;

        // Empty state notice
        GameObject empObj = new GameObject("EmptyNotice", typeof(RectTransform), typeof(CanvasRenderer));
        empObj.transform.SetParent(leftCol.transform, false);
        RectTransform empRt = empObj.GetComponent<RectTransform>();
        empRt.anchorMin = Vector2.zero;
        empRt.anchorMax = Vector2.one;
        empRt.sizeDelta = Vector2.zero;

        emptyMailNoticeText = empObj.AddComponent<TextMeshProUGUI>();
        emptyMailNoticeText.text = isVi ? "<i>Hòm thư hiện đang trống</i>" : "<i>Mailbox is empty</i>";
        emptyMailNoticeText.fontSize = 14f;
        emptyMailNoticeText.color = new Color(0.6f, 0.7f, 0.8f, 0.6f);
        emptyMailNoticeText.alignment = TextAlignmentOptions.Center;
        emptyMailNoticeText.gameObject.SetActive(false);

        // --- RIGHT COLUMN: Mail Detail (width 515px) ---
        GameObject rightCol = new GameObject("RightColumn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rightCol.transform.SetParent(win.transform, false);
        RectTransform rcRt = rightCol.GetComponent<RectTransform>();
        rcRt.anchorMin = new Vector2(1f, 0f);
        rcRt.anchorMax = new Vector2(1f, 1f);
        rcRt.pivot = new Vector2(1f, 0.5f);
        rcRt.anchoredPosition = new Vector2(-25f, -20f);
        rcRt.sizeDelta = new Vector2(515f, -145f);

        Image rcImg = rightCol.GetComponent<Image>();
        rcImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        rcImg.type = Image.Type.Sliced;
        rcImg.color = new Color(0.04f, 0.06f, 0.10f, 0.88f);

        // Inside Right Column: Title
        GameObject dtObj = new GameObject("DetailTitle", typeof(RectTransform), typeof(CanvasRenderer));
        dtObj.transform.SetParent(rightCol.transform, false);
        RectTransform dtRt = dtObj.GetComponent<RectTransform>();
        dtRt.anchorMin = new Vector2(0f, 1f);
        dtRt.anchorMax = new Vector2(1f, 1f);
        dtRt.offsetMin = new Vector2(20f, -44f);
        dtRt.offsetMax = new Vector2(-20f, -14f);

        detailTitleText = dtObj.AddComponent<TextMeshProUGUI>();
        detailTitleText.fontSize = 16f;
        detailTitleText.fontStyle = FontStyles.Bold;
        detailTitleText.color = new Color(1f, 0.88f, 0.45f);

        // Sender & Date
        GameObject dsObj = new GameObject("DetailMeta", typeof(RectTransform), typeof(CanvasRenderer));
        dsObj.transform.SetParent(rightCol.transform, false);
        RectTransform dsRt = dsObj.GetComponent<RectTransform>();
        dsRt.anchorMin = new Vector2(0f, 1f);
        dsRt.anchorMax = new Vector2(1f, 1f);
        dsRt.offsetMin = new Vector2(20f, -68f);
        dsRt.offsetMax = new Vector2(-20f, -46f);

        detailSenderText = dsObj.AddComponent<TextMeshProUGUI>();
        detailSenderText.fontSize = 12.5f;
        detailSenderText.color = new Color(0.60f, 0.75f, 0.90f);

        // Divider
        SoulslikeHUDStyler.AddFiligreeDivider(rightCol.transform, new Vector2(0f, -74f), new Vector2(470f, 6f));

        // Content Body
        GameObject dbObj = new GameObject("DetailBody", typeof(RectTransform), typeof(CanvasRenderer));
        dbObj.transform.SetParent(rightCol.transform, false);
        RectTransform dbRt = dbObj.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(0f, 0f);
        dbRt.anchorMax = new Vector2(1f, 1f);
        dbRt.offsetMin = new Vector2(22f, 130f);
        dbRt.offsetMax = new Vector2(-22f, -86f);

        detailBodyText = dbObj.AddComponent<TextMeshProUGUI>();
        detailBodyText.fontSize = 14f;
        detailBodyText.lineSpacing = 15f;
        detailBodyText.color = new Color(0.85f, 0.90f, 0.96f);
        detailBodyText.enableWordWrapping = true;

        // Attachment Box
        detailRewardBox = new GameObject("RewardBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        detailRewardBox.transform.SetParent(rightCol.transform, false);
        RectTransform rbRt = detailRewardBox.GetComponent<RectTransform>();
        rbRt.anchorMin = new Vector2(0.5f, 0f);
        rbRt.anchorMax = new Vector2(0.5f, 0f);
        rbRt.pivot = new Vector2(0.5f, 0f);
        rbRt.anchoredPosition = new Vector2(0f, 68f);
        rbRt.sizeDelta = new Vector2(470f, 52f);

        Image rbImg = detailRewardBox.GetComponent<Image>();
        rbImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        rbImg.type = Image.Type.Sliced;
        rbImg.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);

        var rbOut = detailRewardBox.AddComponent<Outline>();
        rbOut.effectColor = new Color(0.95f, 0.80f, 0.30f, 0.60f);
        rbOut.effectDistance = new Vector2(1f, -1f);

        GameObject rwTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        rwTxtObj.transform.SetParent(detailRewardBox.transform, false);
        RectTransform rwRt = rwTxtObj.GetComponent<RectTransform>();
        rwRt.anchorMin = Vector2.zero;
        rwRt.anchorMax = Vector2.one;
        rwRt.sizeDelta = Vector2.zero;

        detailRewardValText = rwTxtObj.AddComponent<TextMeshProUGUI>();
        detailRewardValText.fontSize = 15f;
        detailRewardValText.fontStyle = FontStyles.Bold;
        detailRewardValText.alignment = TextAlignmentOptions.Center;
        detailRewardValText.color = new Color(1f, 0.92f, 0.55f);

        // Claim Button (Centered)
        GameObject claimBtnObj = new GameObject("Btn_Claim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        claimBtnObj.layer = 5;
        claimBtnObj.transform.SetParent(rightCol.transform, false);
        RectTransform clRt = claimBtnObj.GetComponent<RectTransform>();
        clRt.anchorMin = new Vector2(0.5f, 0f);
        clRt.anchorMax = new Vector2(0.5f, 0f);
        clRt.pivot = new Vector2(0.5f, 0f);
        clRt.anchoredPosition = new Vector2(-60f, 14f);
        clRt.sizeDelta = new Vector2(250f, 46f);

        Image clImg = claimBtnObj.GetComponent<Image>();
        clImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        clImg.type = Image.Type.Sliced;
        clImg.color = new Color(0.10f, 0.48f, 0.25f, 0.98f);

        var clOut = claimBtnObj.AddComponent<Outline>();
        clOut.effectColor = new Color(0.35f, 1f, 0.65f, 0.70f);
        clOut.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject clTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        clTxtObj.transform.SetParent(claimBtnObj.transform, false);
        RectTransform cltRt = clTxtObj.GetComponent<RectTransform>();
        cltRt.anchorMin = Vector2.zero;
        cltRt.anchorMax = Vector2.one;
        cltRt.sizeDelta = Vector2.zero;

        detailClaimBtnText = clTxtObj.AddComponent<TextMeshProUGUI>();
        detailClaimBtnText.text = isVi ? "<b>NHẬN QUÀ</b>" : "<b>CLAIM GIFT</b>";
        detailClaimBtnText.fontSize = 15f;
        detailClaimBtnText.alignment = TextAlignmentOptions.Center;
        detailClaimBtnText.color = Color.white;

        detailClaimButton = claimBtnObj.GetComponent<Button>();
        detailClaimButton.targetGraphic = clImg;
        detailClaimButton.onClick.AddListener(OnClaimSelectedMail);

        // Delete Single Mail Button
        GameObject delOneBtnObj = new GameObject("Btn_DeleteThis", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        delOneBtnObj.layer = 5;
        delOneBtnObj.transform.SetParent(rightCol.transform, false);
        RectTransform dotRt = delOneBtnObj.GetComponent<RectTransform>();
        dotRt.anchorMin = new Vector2(0.5f, 0f);
        dotRt.anchorMax = new Vector2(0.5f, 0f);
        dotRt.pivot = new Vector2(0.5f, 0f);
        dotRt.anchoredPosition = new Vector2(160f, 14f);
        dotRt.sizeDelta = new Vector2(150f, 46f);

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
        dottTmp.text = isVi ? "<b>XÓA THƯ</b>" : "<b>DELETE</b>";
        dottTmp.fontSize = 14f;
        dottTmp.alignment = TextAlignmentOptions.Center;
        dottTmp.color = Color.white;

        detailDeleteButton = delOneBtnObj.GetComponent<Button>();
        detailDeleteButton.targetGraphic = dotImg;
        detailDeleteButton.onClick.AddListener(OnDeleteSelectedMail);

        // --- BOTTOM SECTION: Giftcode Bar (height 48px) ---
        BuildGiftcodeBar(win.transform);

        // Populate List
        RefreshMailList();

        // Select first valid mail
        SelectFirstAvailableMail();

        // Build Phone Virtual Keyboard
        BuildVirtualKeyboard(transform);
    }

    private void BuildGiftcodeBar(Transform parent)
    {
        bool isVi = LocalizationManager.IsVietnamese;

        GameObject gcBar = new GameObject("GiftcodeBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gcBar.transform.SetParent(parent, false);
        RectTransform gcRt = gcBar.GetComponent<RectTransform>();
        gcRt.anchorMin = new Vector2(0.5f, 0f);
        gcRt.anchorMax = new Vector2(0.5f, 0f);
        gcRt.pivot = new Vector2(0.5f, 0f);
        gcRt.anchoredPosition = new Vector2(0f, 16f);
        gcRt.sizeDelta = new Vector2(930f, 48f);

        Image gcImg = gcBar.GetComponent<Image>();
        gcImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        gcImg.type = Image.Type.Sliced;
        gcImg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);

        // Label
        GameObject lblObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        lblObj.transform.SetParent(gcBar.transform, false);
        RectTransform lRt = lblObj.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 0.5f);
        lRt.anchorMax = new Vector2(0f, 0.5f);
        lRt.pivot = new Vector2(0f, 0.5f);
        lRt.anchoredPosition = new Vector2(14f, 0f);
        lRt.sizeDelta = new Vector2(120f, 32f);

        var lTmp = lblObj.AddComponent<TextMeshProUGUI>();
        lTmp.text = isVi ? "<b>MÃ QUÀ:</b>" : "<b>CODE:</b>";
        lTmp.fontSize = 13.5f;
        lTmp.color = new Color(1f, 0.88f, 0.45f);
        lTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Input Field - Optimized with large touch target for mobile (height 40px)
        GameObject inputObj = new GameObject("InputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        inputObj.transform.SetParent(gcBar.transform, false);
        RectTransform inRt = inputObj.GetComponent<RectTransform>();
        inRt.anchorMin = new Vector2(0f, 0.5f);
        inRt.anchorMax = new Vector2(0f, 0.5f);
        inRt.pivot = new Vector2(0f, 0.5f);
        inRt.anchoredPosition = new Vector2(128f, 0f);
        inRt.sizeDelta = new Vector2(210f, 40f);

        // Button [⌨ PHÍM] - Mở bàn phím ảo điện thoại trực tiếp
        GameObject kbdBtnObj = new GameObject("Btn_ToggleKeyboard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        kbdBtnObj.transform.SetParent(gcBar.transform, false);
        RectTransform kbRt = kbdBtnObj.GetComponent<RectTransform>();
        kbRt.anchorMin = new Vector2(0f, 0.5f);
        kbRt.anchorMax = new Vector2(0f, 0.5f);
        kbRt.pivot = new Vector2(0f, 0.5f);
        kbRt.anchoredPosition = new Vector2(344f, 0f);
        kbRt.sizeDelta = new Vector2(80f, 40f);

        Image kbImg = kbdBtnObj.GetComponent<Image>();
        kbImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        kbImg.type = Image.Type.Sliced;
        kbImg.color = new Color(0.24f, 0.22f, 0.38f, 0.95f);

        var kbOut = kbdBtnObj.AddComponent<Outline>();
        kbOut.effectColor = new Color(0.70f, 0.60f, 1f, 0.60f);
        kbOut.effectDistance = new Vector2(1f, -1f);

        GameObject kbtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        kbtObj.transform.SetParent(kbdBtnObj.transform, false);
        RectTransform kbtRt = kbtObj.GetComponent<RectTransform>();
        kbtRt.anchorMin = Vector2.zero;
        kbtRt.anchorMax = Vector2.one;
        kbtRt.sizeDelta = Vector2.zero;
        var kbtTmp = kbtObj.AddComponent<TextMeshProUGUI>();
        kbtTmp.text = isVi ? "<b>PHÍM</b>" : "<b>KEYBD</b>";
        kbtTmp.fontSize = 12f;
        kbtTmp.alignment = TextAlignmentOptions.Center;
        kbtTmp.color = new Color(0.9f, 0.85f, 1f);

        Button kbdBtn = kbdBtnObj.GetComponent<Button>();
        kbdBtn.targetGraphic = kbImg;
        kbdBtn.onClick.AddListener(ToggleVirtualKeyboard);

        Image inBg = inputObj.GetComponent<Image>();
        inBg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);
        inBg.raycastTarget = true;

        var inOutline = inputObj.AddComponent<Outline>();
        inOutline.effectColor = new Color(0.25f, 0.45f, 0.65f, 0.60f);
        inOutline.effectDistance = new Vector2(1f, -1f);

        giftcodeInput = inputObj.GetComponent<TMP_InputField>();
        giftcodeInput.targetGraphic = inBg;
        giftcodeInput.keyboardType = TouchScreenKeyboardType.Default;
        giftcodeInput.shouldHideMobileInput = false;
        giftcodeInput.characterLimit = 22;

        // Text area WITH RECTMASK2D - Ngăn 100% chữ tràn hoặc lem ra ngoài ô nhập!
        GameObject taObj = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        taObj.transform.SetParent(inputObj.transform, false);
        RectTransform taRt = taObj.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(10f, 2f);
        taRt.offsetMax = new Vector2(-10f, -2f);

        // Placeholder Text
        GameObject phObj = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer));
        phObj.transform.SetParent(taObj.transform, false);
        RectTransform phRt = phObj.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.sizeDelta = Vector2.zero;
        var phTmp = phObj.AddComponent<TextMeshProUGUI>();
        phTmp.text = isVi ? "Chạm để nhập mã..." : "Tap to enter code...";
        phTmp.fontSize = 12.5f;
        phTmp.enableAutoSizing = true;
        phTmp.fontSizeMin = 8.5f;
        phTmp.fontSizeMax = 12.5f;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.5f, 0.6f, 0.7f, 0.7f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;
        phTmp.raycastTarget = false; // CRITICAL: Never block touches to InputField on mobile

        // Input Text with Auto-Sizing (Tự động co nhỏ chữ khi chuỗi dài, không bao giờ lem ra ngoài)
        GameObject itObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        itObj.transform.SetParent(taObj.transform, false);
        RectTransform itRt = itObj.GetComponent<RectTransform>();
        itRt.anchorMin = Vector2.zero;
        itRt.anchorMax = Vector2.one;
        itRt.sizeDelta = Vector2.zero;
        var itTmp = itObj.AddComponent<TextMeshProUGUI>();
        itTmp.fontSize = 13.5f;
        itTmp.enableAutoSizing = true;
        itTmp.fontSizeMin = 8.5f;
        itTmp.fontSizeMax = 13.5f;
        itTmp.overflowMode = TextOverflowModes.Truncate;
        itTmp.fontStyle = FontStyles.Bold;
        itTmp.color = Color.white;
        itTmp.alignment = TextAlignmentOptions.MidlineLeft;
        itTmp.raycastTarget = false; // CRITICAL: Never block touches to InputField on mobile

        giftcodeInput.textViewport = taRt;
        giftcodeInput.textComponent = itTmp;
        giftcodeInput.placeholder = phTmp;

        // Click on InputField automatically summons native Android on-screen keyboard
        var trigger = inputObj.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener((eventData) => OpenMobileKeyboard());
        trigger.triggers.Add(entry);

        giftcodeInput.onSelect.AddListener((s) => OpenMobileKeyboard());
        giftcodeInput.onValueChanged.AddListener((s) => {
            AdjustInputFontSize(s);
            UpdateKeyboardPreview();
        });

        // 1-Click Paste Button (DÁN MÃ TỪ BỘ NHỚ TẠM) - Cực tiện lợi cho điện thoại!
        GameObject pasteBtnObj = new GameObject("Btn_Paste", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        pasteBtnObj.transform.SetParent(gcBar.transform, false);
        RectTransform pRt = pasteBtnObj.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0f, 0.5f);
        pRt.anchorMax = new Vector2(0f, 0.5f);
        pRt.pivot = new Vector2(0f, 0.5f);
        pRt.anchoredPosition = new Vector2(430f, 0f);
        pRt.sizeDelta = new Vector2(65f, 40f);

        Image pImg = pasteBtnObj.GetComponent<Image>();
        pImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        pImg.type = Image.Type.Sliced;
        pImg.color = new Color(0.22f, 0.28f, 0.38f, 0.95f);

        GameObject ptObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        ptObj.transform.SetParent(pasteBtnObj.transform, false);
        RectTransform ptRt = ptObj.GetComponent<RectTransform>();
        ptRt.anchorMin = Vector2.zero;
        ptRt.anchorMax = Vector2.one;
        ptRt.sizeDelta = Vector2.zero;
        var ptTmp = ptObj.AddComponent<TextMeshProUGUI>();
        ptTmp.text = isVi ? "<b>DÁN</b>" : "<b>PASTE</b>";
        ptTmp.fontSize = 12.5f;
        ptTmp.alignment = TextAlignmentOptions.Center;
        ptTmp.color = new Color(0.7f, 0.9f, 1f);

        Button pasteBtn = pasteBtnObj.GetComponent<Button>();
        pasteBtn.targetGraphic = pImg;
        pasteBtn.onClick.AddListener(OnPasteFromClipboard);

        // 1-Click Code List Picker Button (MÃ CÓ SẴN) - Không cần gõ phím!
        GameObject listBtnObj = new GameObject("Btn_CodeList", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        listBtnObj.transform.SetParent(gcBar.transform, false);
        RectTransform clRt = listBtnObj.GetComponent<RectTransform>();
        clRt.anchorMin = new Vector2(0f, 0.5f);
        clRt.anchorMax = new Vector2(0f, 0.5f);
        clRt.pivot = new Vector2(0f, 0.5f);
        clRt.anchoredPosition = new Vector2(501f, 0f);
        clRt.sizeDelta = new Vector2(95f, 40f);

        Image clImg = listBtnObj.GetComponent<Image>();
        clImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        clImg.type = Image.Type.Sliced;
        clImg.color = new Color(0.28f, 0.22f, 0.12f, 0.95f);

        var clOut = listBtnObj.AddComponent<Outline>();
        clOut.effectColor = new Color(1f, 0.82f, 0.35f, 0.65f);
        clOut.effectDistance = new Vector2(1f, -1f);

        GameObject cltObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        cltObj.transform.SetParent(listBtnObj.transform, false);
        RectTransform cltRt = cltObj.GetComponent<RectTransform>();
        cltRt.anchorMin = Vector2.zero;
        cltRt.anchorMax = Vector2.one;
        cltRt.sizeDelta = Vector2.zero;
        var cltTmp = cltObj.AddComponent<TextMeshProUGUI>();
        cltTmp.text = isVi ? "<b>MÃ CÓ SẴN</b>" : "<b>CODE LIST</b>";
        cltTmp.fontSize = 12.5f;
        cltTmp.alignment = TextAlignmentOptions.Center;
        cltTmp.color = new Color(1f, 0.88f, 0.45f);

        Button listBtn = listBtnObj.GetComponent<Button>();
        listBtn.targetGraphic = clImg;
        listBtn.onClick.AddListener(OpenCodeListModal);

        // Redeem Button (ĐỔI CODE)
        GameObject rdBtnObj = new GameObject("Btn_Redeem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        rdBtnObj.transform.SetParent(gcBar.transform, false);
        RectTransform rdRt = rdBtnObj.GetComponent<RectTransform>();
        rdRt.anchorMin = new Vector2(0f, 0.5f);
        rdRt.anchorMax = new Vector2(0f, 0.5f);
        rdRt.pivot = new Vector2(0f, 0.5f);
        rdRt.anchoredPosition = new Vector2(602f, 0f);
        rdRt.sizeDelta = new Vector2(120f, 40f);

        Image rdImg = rdBtnObj.GetComponent<Image>();
        rdImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        rdImg.type = Image.Type.Sliced;
        rdImg.color = new Color(0.12f, 0.46f, 0.26f, 0.98f);

        var rdOut = rdBtnObj.AddComponent<Outline>();
        rdOut.effectColor = new Color(0.35f, 1f, 0.65f, 0.70f);
        rdOut.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject rdtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        rdtObj.transform.SetParent(rdBtnObj.transform, false);
        RectTransform rdtRt = rdtObj.GetComponent<RectTransform>();
        rdtRt.anchorMin = Vector2.zero;
        rdtRt.anchorMax = Vector2.one;
        rdtRt.sizeDelta = Vector2.zero;
        var rdtTmp = rdtObj.AddComponent<TextMeshProUGUI>();
        rdtTmp.text = isVi ? "<b>ĐỔI CODE</b>" : "<b>REDEEM</b>";
        rdtTmp.fontSize = 13.5f;
        rdtTmp.alignment = TextAlignmentOptions.Center;
        rdtTmp.color = Color.white;

        Button rdBtn = rdBtnObj.GetComponent<Button>();
        rdBtn.targetGraphic = rdImg;
        rdBtn.onClick.AddListener(OnRedeemGiftcode);

        // Feedback Text
        GameObject fbObj = new GameObject("Feedback", typeof(RectTransform), typeof(CanvasRenderer));
        fbObj.transform.SetParent(gcBar.transform, false);
        RectTransform fbRt = fbObj.GetComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(0f, 0.5f);
        fbRt.anchorMax = new Vector2(1f, 0.5f);
        fbRt.offsetMin = new Vector2(730f, -18f);
        fbRt.offsetMax = new Vector2(-12f, 18f);

        giftcodeFeedbackText = fbObj.AddComponent<TextMeshProUGUI>();
        giftcodeFeedbackText.fontSize = 12f;
        giftcodeFeedbackText.alignment = TextAlignmentOptions.MidlineLeft;
        giftcodeFeedbackText.color = new Color(0.35f, 0.95f, 0.85f);
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
                    ? "<color=#55EFC4>Đã dán mã từ bộ nhớ tạm!</color>" 
                    : "<color=#55EFC4>Pasted from clipboard!</color>";
            }
        }
        else if (giftcodeFeedbackText != null)
        {
            giftcodeFeedbackText.text = LocalizationManager.IsVietnamese 
                ? "<color=#FF6B6B>Bộ nhớ tạm đang trống!</color>" 
                : "<color=#FF6B6B>Clipboard is empty!</color>";
        }
    }

    private void OpenCodeListModal()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();

        // Build a sleek code picker popup
        GameObject modalObj = new GameObject("CodeListModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalObj.transform.SetParent(transform, false);
        RectTransform mRt = modalObj.GetComponent<RectTransform>();
        mRt.anchorMin = Vector2.zero;
        mRt.anchorMax = Vector2.one;
        mRt.offsetMin = mRt.offsetMax = Vector2.zero;

        Image mBg = modalObj.GetComponent<Image>();
        mBg.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(modalObj.transform, false);
        RectTransform cRt = card.GetComponent<RectTransform>();
        cRt.anchorMin = cRt.anchorMax = cRt.pivot = new Vector2(0.5f, 0.5f);
        cRt.sizeDelta = new Vector2(620f, 440f);

        Image cardImg = card.GetComponent<Image>();
        cardImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.05f, 0.07f, 0.12f, 0.98f);

        var outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.82f, 0.35f, 0.75f);
        outline.effectDistance = new Vector2(2f, -2f);

        bool isVi = LocalizationManager.IsVietnamese;

        // Title
        GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        tObj.transform.SetParent(card.transform, false);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = tRt.anchorMax = tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -16f);
        tRt.sizeDelta = new Vector2(560f, 32f);
        var tTmp = tObj.GetComponent<TextMeshProUGUI>();
        tTmp.text = isVi ? "<b>DANH SÁCH MÃ QUÀ TẶNG CÓ SẴN</b>" : "<b>AVAILABLE GIFTCODES</b>";
        tTmp.fontSize = 18f;
        tTmp.alignment = TextAlignmentOptions.Center;
        tTmp.color = new Color(1f, 0.88f, 0.45f);

        // Subtitle
        GameObject sObj = new GameObject("Sub", typeof(RectTransform), typeof(TextMeshProUGUI));
        sObj.transform.SetParent(card.transform, false);
        RectTransform sRt = sObj.GetComponent<RectTransform>();
        sRt.anchorMin = sRt.anchorMax = sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0f, -44f);
        sRt.sizeDelta = new Vector2(560f, 22f);
        var sTmp = sObj.GetComponent<TextMeshProUGUI>();
        sTmp.text = isVi ? "Chạm vào mã bất kỳ để tự động điền và kích hoạt:" : "Tap any code to automatically apply and redeem:";
        sTmp.fontSize = 12f;
        sTmp.alignment = TextAlignmentOptions.Center;
        sTmp.color = new Color(0.75f, 0.82f, 0.90f);

        // Close button
        GameObject closeBtn = new GameObject("Btn_Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeBtn.transform.SetParent(card.transform, false);
        RectTransform cbRt = closeBtn.GetComponent<RectTransform>();
        cbRt.anchorMin = cbRt.anchorMax = cbRt.pivot = new Vector2(1f, 1f);
        cbRt.anchoredPosition = new Vector2(-12f, -12f);
        cbRt.sizeDelta = new Vector2(36f, 36f);
        Image cbImg = closeBtn.GetComponent<Image>();
        cbImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
        cbImg.color = new Color(0.38f, 0.15f, 0.16f);
        GameObject cbtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        cbtObj.transform.SetParent(closeBtn.transform, false);
        var cbtTmp = cbtObj.GetComponent<TextMeshProUGUI>();
        cbtTmp.text = "X";
        cbtTmp.alignment = TextAlignmentOptions.Center;
        cbtTmp.color = Color.white;
        closeBtn.GetComponent<Button>().onClick.AddListener(() => Destroy(modalObj));

        // Code entries
        string[] codes = new string[] { "HUNGNE123", "VIP888", "DARKFANTASY", "VAELSTRIDER", "RUINBOUND", "DISCORD2026", "NEWBIE2026", "SHADOWFIGHT" };
        int[] rewards = new int[] { 5000, 2000, 1500, 1000, 1000, 1000, 500, 500 };
        string[] descs = new string[] {
            isVi ? "Mã Tác Giả Game" : "Author Dev Code",
            isVi ? "Mã Siêu Cấp VIP" : "Super VIP Code",
            isVi ? "Tri Ân Dark Fantasy" : "Dark Fantasy Gift",
            isVi ? "Mã Khởi Đầu" : "Release Code",
            isVi ? "Mã Vực Thẳm" : "Ruinbound Code",
            isVi ? "Quà Hiệp Sĩ" : "Community Code",
            isVi ? "Quà Tân Thủ" : "Newbie Bonus",
            isVi ? "Tri Ân Đối Kháng" : "Combat Gift"
        };

        float startY = -78f;
        float itemH = 38f;
        float gap = 6f;

        for (int i = 0; i < codes.Length; i++)
        {
            string cCode = codes[i];
            int rAmount = rewards[i];
            string dNote = descs[i];
            bool isUsed = PlayerPrefs.GetInt("Giftcode_Used_" + cCode, 0) == 1;

            GameObject row = new GameObject("Row_" + cCode, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            row.transform.SetParent(card.transform, false);
            RectTransform rRt = row.GetComponent<RectTransform>();
            rRt.anchorMin = rRt.anchorMax = rRt.pivot = new Vector2(0.5f, 1f);
            rRt.anchoredPosition = new Vector2(0f, startY - i * (itemH + gap));
            rRt.sizeDelta = new Vector2(560f, itemH);

            Image rImg = row.GetComponent<Image>();
            rImg.color = isUsed ? new Color(0.08f, 0.10f, 0.12f, 0.60f) : new Color(0.10f, 0.15f, 0.22f, 0.85f);

            // Code Text
            GameObject cObj = new GameObject("Code", typeof(RectTransform), typeof(TextMeshProUGUI));
            cObj.transform.SetParent(row.transform, false);
            RectTransform cTxtRt = cObj.GetComponent<RectTransform>();
            cTxtRt.anchorMin = cTxtRt.anchorMax = cTxtRt.pivot = new Vector2(0f, 0.5f);
            cTxtRt.anchoredPosition = new Vector2(16f, 0f);
            cTxtRt.sizeDelta = new Vector2(160f, 30f);
            var cTmp = cObj.GetComponent<TextMeshProUGUI>();
            cTmp.text = $"<b>{cCode}</b>";
            cTmp.fontSize = 14f;
            cTmp.alignment = TextAlignmentOptions.MidlineLeft;
            cTmp.color = isUsed ? new Color(0.5f, 0.55f, 0.60f) : new Color(1f, 0.90f, 0.45f);

            // Reward & Note
            GameObject nObj = new GameObject("Note", typeof(RectTransform), typeof(TextMeshProUGUI));
            nObj.transform.SetParent(row.transform, false);
            RectTransform nRt = nObj.GetComponent<RectTransform>();
            nRt.anchorMin = nRt.anchorMax = nRt.pivot = new Vector2(0f, 0.5f);
            nRt.anchoredPosition = new Vector2(185f, 0f);
            nRt.sizeDelta = new Vector2(250f, 30f);
            var nTmp = nObj.GetComponent<TextMeshProUGUI>();
            nTmp.text = $"<color=#55EFC4>+{rAmount:N0} Linh Hồn</color> | <size=11>{dNote}</size>";
            nTmp.fontSize = 12.5f;
            nTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nTmp.color = isUsed ? new Color(0.5f, 0.55f, 0.60f) : Color.white;

            // Pick / Status Button
            GameObject actBtn = new GameObject("Btn_Act", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            actBtn.transform.SetParent(row.transform, false);
            RectTransform aRt = actBtn.GetComponent<RectTransform>();
            aRt.anchorMin = aRt.anchorMax = aRt.pivot = new Vector2(1f, 0.5f);
            aRt.anchoredPosition = new Vector2(-10f, 0f);
            aRt.sizeDelta = new Vector2(100f, 30f);

            Image aImg = actBtn.GetComponent<Image>();
            aImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
            aImg.type = Image.Type.Sliced;
            aImg.color = isUsed ? new Color(0.20f, 0.22f, 0.25f, 0.8f) : new Color(0.12f, 0.48f, 0.26f, 0.95f);

            GameObject atObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            atObj.transform.SetParent(actBtn.transform, false);
            var atTmp = atObj.GetComponent<TextMeshProUGUI>();
            atTmp.text = isUsed ? (isVi ? "ĐÃ DÙNG" : "USED") : (isVi ? "<b>DÙNG NGAY</b>" : "<b>APPLY</b>");
            atTmp.fontSize = 11.5f;
            atTmp.alignment = TextAlignmentOptions.Center;
            atTmp.color = isUsed ? new Color(0.6f, 0.6f, 0.6f) : Color.white;

            Button btn = actBtn.GetComponent<Button>();
            btn.targetGraphic = aImg;
            btn.interactable = !isUsed;
            if (!isUsed)
            {
                btn.onClick.AddListener(() => {
                    Destroy(modalObj);
                    if (giftcodeInput != null)
                    {
                        giftcodeInput.text = cCode;
                        OnRedeemGiftcode();
                    }
                });
            }
        }
    }

    private void RefreshMailList()
    {
        bool isVi = LocalizationManager.IsVietnamese;

        // Clear existing children
        for (int i = mailListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(mailListContent.GetChild(i).gameObject);
        }

        List<MailItemData> visibleMails = new List<MailItemData>();
        foreach (var m in _mails)
        {
            if (!IsMailDeleted(m.id)) visibleMails.Add(m);
        }

        if (emptyMailNoticeText != null)
        {
            emptyMailNoticeText.gameObject.SetActive(visibleMails.Count == 0);
        }

        float itemHeight = 78f;
        float gap = 8f;
        float startY = -4f;

        for (int i = 0; i < visibleMails.Count; i++)
        {
            var mail = visibleMails[i];
            bool isClaimed = mail.rewardShards <= 0 || PlayerPrefs.GetInt("Mail_Claimed_" + mail.id, 0) == 1;

            GameObject item = new GameObject("MailItem_" + mail.id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            item.transform.SetParent(mailListContent, false);

            RectTransform iRt = item.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.5f, 1f);
            iRt.anchorMax = new Vector2(0.5f, 1f);
            iRt.pivot = new Vector2(0.5f, 1f);
            iRt.anchoredPosition = new Vector2(0f, startY - i * (itemHeight + gap));
            iRt.sizeDelta = new Vector2(356f, itemHeight);

            Image itemImg = item.GetComponent<Image>();
            itemImg.sprite = SoulslikeHUDStyler.GetStoneButtonSprite();
            itemImg.type = Image.Type.Sliced;
            itemImg.color = (selectedMail == mail) ? new Color(0.18f, 0.28f, 0.42f, 0.95f) : new Color(0.08f, 0.11f, 0.16f, 0.92f);

            var outline = item.AddComponent<Outline>();
            outline.effectColor = (selectedMail == mail) ? new Color(0.4f, 0.9f, 1f, 0.8f) : new Color(0.2f, 0.35f, 0.5f, 0.4f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Mail Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
            titleObj.transform.SetParent(item.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.offsetMin = new Vector2(12f, -30f);
            tRt.offsetMax = new Vector2(-24f, -8f);

            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = isVi ? mail.titleVi : mail.titleEn;
            tTmp.fontSize = 12.5f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = isClaimed ? new Color(0.65f, 0.72f, 0.82f) : new Color(1f, 0.88f, 0.45f);
            tTmp.overflowMode = TextOverflowModes.Ellipsis;

            // Sender & Status
            GameObject subObj = new GameObject("Meta", typeof(RectTransform), typeof(CanvasRenderer));
            subObj.transform.SetParent(item.transform, false);
            RectTransform sRt = subObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(1f, 0f);
            sRt.offsetMin = new Vector2(12f, 8f);
            sRt.offsetMax = new Vector2(-12f, 26f);

            var sTmp = subObj.AddComponent<TextMeshProUGUI>();
            string sender = isVi ? mail.senderVi : mail.senderEn;
            string statusStr = isClaimed 
                ? (isVi ? "<color=#88A0C0>[Đã nhận]</color>" : "<color=#88A0C0>[Claimed]</color>") 
                : $"<color=#55EFC4><b>+{mail.rewardShards:N0} Linh Hồn</b></color>";
            sTmp.text = $"{sender} | {statusStr}";
            sTmp.fontSize = 11.5f;

            // Red Dot if unclaimed
            if (!isClaimed)
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

            Button btn = item.GetComponent<Button>();
            btn.targetGraphic = itemImg;
            btn.onClick.AddListener(() => {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                SelectMail(mail);
            });
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
        ClearDetailDisplay();
    }

    private void ClearDetailDisplay()
    {
        selectedMail = null;
        bool isVi = LocalizationManager.IsVietnamese;
        if (detailTitleText != null) detailTitleText.text = isVi ? "Không có thư nào được chọn" : "No mail selected";
        if (detailSenderText != null) detailSenderText.text = "";
        if (detailBodyText != null) detailBodyText.text = isVi ? "Hòm thư của bạn hiện đang trống." : "Your mailbox is currently empty.";
        if (detailRewardBox != null) detailRewardBox.SetActive(false);
        if (detailClaimButton != null) detailClaimButton.gameObject.SetActive(false);
        if (detailDeleteButton != null) detailDeleteButton.gameObject.SetActive(false);
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

        if (detailTitleText != null) detailTitleText.text = isVi ? mail.titleVi : mail.titleEn;
        if (detailSenderText != null)
        {
            string sender = isVi ? mail.senderVi : mail.senderEn;
            detailSenderText.text = isVi 
                ? $"Người gửi: <b>{sender}</b> | Ngày: {mail.date}"
                : $"From: <b>{sender}</b> | Date: {mail.date}";
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
                    ? $"PHẦN THƯỞNG: <b>+{mail.rewardShards:N0} MẢNH LINH HỒN</b>"
                    : $"REWARD: <b>+{mail.rewardShards:N0} SOUL SHARDS</b>";
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
                detailClaimBtnText.text = isVi ? "<b>ĐÃ NHẬN</b>" : "<b>CLAIMED</b>";
                detailClaimBtnText.color = new Color(0.65f, 0.72f, 0.82f);
            }
            else
            {
                clImg.color = new Color(0.10f, 0.48f, 0.25f, 0.98f);
                detailClaimBtnText.text = isVi ? "<b>NHẬN QUÀ</b>" : "<b>CLAIM GIFT</b>";
                detailClaimBtnText.color = Color.white;
            }
        }

        if (detailDeleteButton != null)
        {
            // Only allow deleting mail if it has been claimed or has no reward
            detailDeleteButton.gameObject.SetActive(isClaimed || !hasReward);
        }

        RefreshMailList();
    }

    private void OnClaimSelectedMail()
    {
        if (selectedMail == null || selectedMail.rewardShards <= 0) return;

        string claimKey = "Mail_Claimed_" + selectedMail.id;
        if (PlayerPrefs.GetInt(claimKey, 0) == 1) return;

        // 1. Add Shards to player's account
        AccountDatabase.AddAethelShards(selectedMail.rewardShards);
        PlayerPrefs.SetInt(claimKey, 1);
        PlayerPrefs.Save();

        // 2. Play Audio & FX
        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();

        // 3. Refresh HUD
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.RefreshCurrencies();
        }

        // 4. Update UI
        SelectMail(selectedMail);
        UpdateLobbyBadge();
    }

    private void OnDeleteSelectedMail()
    {
        if (selectedMail == null) return;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        PlayerPrefs.SetInt("Mail_Deleted_" + selectedMail.id, 1);
        PlayerPrefs.Save();

        SelectFirstAvailableMail();
        UpdateLobbyBadge();
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
        }
    }

    private void OnRedeemGiftcode()
    {
        if (giftcodeInput == null) return;
        string rawCode = giftcodeInput.text.Trim().ToUpperInvariant();
        bool isVi = LocalizationManager.IsVietnamese;

        if (string.IsNullOrEmpty(rawCode))
        {
            giftcodeFeedbackText.text = isVi ? "<color=#FF6B6B>Vui lòng nhập mã quà tặng!</color>" : "<color=#FF6B6B>Please enter a code!</color>";
            return;
        }

        string usedKey = "Giftcode_Used_" + rawCode;
        if (PlayerPrefs.GetInt(usedKey, 0) == 1)
        {
            giftcodeFeedbackText.text = isVi ? "<color=#FF6B6B>Mã này đã được sử dụng rồi!</color>" : "<color=#FF6B6B>This code has already been used!</color>";
            return;
        }

        int reward = 0;
        if (rawCode == "HUNGNE123")
        {
            reward = 5000; // Ma Dac Quyen Tac Gia Game
        }
        else if (rawCode == "VIP888")
        {
            reward = 2000; // Ma Sieu Cap VIP
        }
        else if (rawCode == "DARKFANTASY")
        {
            reward = 1500; // Ma Tri An Cong Dong Dark Fantasy
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
            giftcodeFeedbackText.text = isVi ? "<color=#FF6B6B>Mã không hợp lệ hoặc đã hết hạn!</color>" : "<color=#FF6B6B>Invalid or expired code!</color>";
            return;
        }

        // Apply giftcode reward
        AccountDatabase.AddAethelShards(reward);
        PlayerPrefs.SetInt(usedKey, 1);
        PlayerPrefs.Save();

        if (SoundManager.Instance != null) SoundManager.Instance.PlayUpgradeSuccess();
        if (MainMenuManager.Instance != null) MainMenuManager.Instance.RefreshCurrencies();

        giftcodeFeedbackText.text = isVi 
            ? $"<color=#55EFC4><b>Thành công! Nhận +{reward:N0} Linh Hồn!</b></color>"
            : $"<color=#55EFC4><b>Success! Received +{reward:N0} Souls!</b></color>";

        giftcodeInput.text = "";
        UpdateKeyboardPreview();
    }

    public static void UpdateLobbyBadge()
    {
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.UpdateMailboxBadge();
        }
    }

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
                    false, // autocorrection
                    false, // multiline
                    false, // secure
                    false, // alert
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
            giftcodeInput.textComponent.fontSize = 8.5f;
        }
        else if (len >= 14)
        {
            giftcodeInput.textComponent.fontSize = 10f;
        }
        else if (len >= 10)
        {
            giftcodeInput.textComponent.fontSize = 11.5f;
        }
        else
        {
            giftcodeInput.textComponent.fontSize = 13.5f;
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

        // Header / Display Bar (Height 42px)
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

        // Title on Display Bar
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

        // Preview Text with RectMask2D and auto-sizing
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

        // Close / Minimize Keyboard Button
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

        // Row 1: Numbers (10 keys)
        string[] row1 = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        CreateKeyRow(virtualKeyboardObj.transform, row1, -54f, 86f, 42f, 7f);

        // Row 2: QWERTY (10 keys)
        string[] row2 = new string[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
        CreateKeyRow(virtualKeyboardObj.transform, row2, -102f, 86f, 42f, 7f);

        // Row 3: ASDF (9 keys)
        string[] row3 = new string[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" };
        CreateKeyRow(virtualKeyboardObj.transform, row3, -150f, 94f, 42f, 8f);

        // Row 4: ZXCV (7 keys) + Backspace (1 key)
        CreateRow4WithBackspace(virtualKeyboardObj.transform, -198f);

        // Row 5: Action Row (Dán, Mã Sẵn, Dấu Cách, Đổi Code)
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

        // Backspace key
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
        float w1 = 150f; // Paste
        float w2 = 170f; // Available codes
        float w3 = 190f; // Space
        float w4 = 210f; // Redeem

        float totalWidth = w1 + w2 + w3 + w4 + 3 * gap;
        float startX = -totalWidth * 0.5f;

        // 1. Paste Button
        float x1 = startX + w1 * 0.5f;
        CreateSingleActionButton(parent, "Btn_VkPaste", isVi ? "<b>DÁN MÃ</b>" : "<b>PASTE</b>",
            new Vector2(x1, yPos - keyHeight * 0.5f), new Vector2(w1, keyHeight),
            new Color(0.20f, 0.26f, 0.38f, 0.96f), new Color(0.4f, 0.7f, 1f, 0.6f),
            OnPasteFromClipboard);

        // 2. Code List Modal Button
        float x2 = startX + w1 + gap + w2 * 0.5f;
        CreateSingleActionButton(parent, "Btn_VkCodeList", isVi ? "<b>MÃ CÓ SẴN</b>" : "<b>GIFTCODES</b>",
            new Vector2(x2, yPos - keyHeight * 0.5f), new Vector2(w2, keyHeight),
            new Color(0.32f, 0.24f, 0.12f, 0.96f), new Color(1f, 0.85f, 0.35f, 0.7f),
            OpenCodeListModal);

        // 3. Space Bar
        float x3 = startX + w1 + gap + w2 + gap + w3 * 0.5f;
        CreateSingleActionButton(parent, "Btn_VkSpace", isVi ? "<b>DẤU CÁCH</b>" : "<b>SPACE</b>",
            new Vector2(x3, yPos - keyHeight * 0.5f), new Vector2(w3, keyHeight),
            new Color(0.18f, 0.23f, 0.30f, 0.96f), new Color(0.4f, 0.55f, 0.75f, 0.5f),
            () => OnVirtualKeyPress(' '));

        // 4. Redeem Button
        float x4 = startX + w1 + gap + w2 + gap + w3 + gap + w4 * 0.5f;
        CreateSingleActionButton(parent, "Btn_VkRedeem", isVi ? "<b>ĐỔI CODE</b>" : "<b>REDEEM</b>",
            new Vector2(x4, yPos - keyHeight * 0.5f), new Vector2(w4, keyHeight),
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
        // Live stream text from phone keyboard into InputField on physical mobile device
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
