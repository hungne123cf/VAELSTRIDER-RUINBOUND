using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hệ thống Chat Tổ Đội & Lời Mời Kết Bạn In-Game:
/// Nút Chat nổi, Banner thông báo toast, Popup Quick Ping / Text Chat,
/// cùng Tab Lời Mời Kết Bạn với nút [Chấp Nhận] / [Từ Chối] trực tiếp trong trận.
/// Đồng thời hiển thị Toast Tương Tác Lời Mời Vào Phòng (Accept/Decline).
/// </summary>
public class InGameTeamChatUI : MonoBehaviour
{
    public static InGameTeamChatUI Instance { get; private set; }

    private Canvas _canvas;
    private GameObject _rootPanel;
    private GameObject _quickChatModal;
    private GameObject _toastBannerObj;
    private TextMeshProUGUI _toastBannerText;
    private Coroutine _toastCoroutine;

    // Toast tương tác mời phòng (Interactive Room Invite Toast)
    private GameObject _inviteToastObj;
    private TextMeshProUGUI _inviteToastText;
    private string _pendingInviteRoomPin = "";
    private Coroutine _inviteToastCoroutine;

    // Lịch sử tin nhắn trò chuyện (Chat History Log)
    private Transform _chatHistoryContent;
    private ScrollRect _chatHistoryScroll;
    private readonly List<string> _chatHistoryLog = new List<string>();
    private const int MAX_CHAT_LOG = 30;

    // Các thành phần trong Quick Chat Modal
    private GameObject _panelChatTab;
    private GameObject _panelRequestsTab;
    private Button _tabChatBtn;
    private Button _tabRequestsBtn;
    private TextMeshProUGUI _tabChatBtnText;
    private TextMeshProUGUI _tabRequestsBtnText;
    private Transform _requestListContent;

    private readonly string[] _quickPings = new string[]
    {
        "TẬP TRUNG LẠI!",
        "CỨU TUI VỚI!",
        "TẬP TRUNG ĐÁNH BOSS!",
        "LÙI LẠI HỒI MÁU!",
        "ĐÒN ĐÁNH TỐT LẮM!",
        "CỐ LÊN ANH EM!"
    };

    private TMP_InputField _inGameChatInput;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            var go = new GameObject("InGameTeamChatUI_AutoInit");
            Instance = go.AddComponent<InGameTeamChatUI>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        BuildUI();
    }

    private void OnEnable()
    {
        HookNetworkChat();
    }

    private void OnDisable()
    {
        UnhookNetworkChat();
    }

    private void Start()
    {
        HookNetworkChat();
    }

    private void HookNetworkChat()
    {
        if (PvPNetworkManager.Instance != null)
        {
            PvPNetworkManager.Instance.OnChatMessageReceived -= HandleInGameChatMessage;
            PvPNetworkManager.Instance.OnChatMessageReceived += HandleInGameChatMessage;
        }
    }

    private void UnhookNetworkChat()
    {
        if (PvPNetworkManager.Instance != null)
        {
            PvPNetworkManager.Instance.OnChatMessageReceived -= HandleInGameChatMessage;
        }
    }

    private void Update()
    {
        // Tuyệt đối không hiển thị nút chat trong trận khi người chơi đang ở ngoài Sảnh Chính (MainMenuScene) hoặc Đăng Nhập (LoginScene)
        string curScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isMenuScene = curScene == "MainMenuScene" || curScene == "LoginScene";

        // Không hiển thị nút chat trong trận khi đang ở phòng chờ, modal lobby hoặc đang mở Cài Đặt (Settings)
        bool isLobbyActive = PvPLobbyUI.IsLobbyModalOpen;
        bool isSettingsActive = IsAnySettingsModalActive();
        bool isInCombat = !isMenuScene && !isLobbyActive && !isSettingsActive && (PvPMatchManager.IsPvPModeActive || (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsRunning));

        if (_rootPanel != null)
        {
            if (_rootPanel.activeSelf != isInCombat)
            {
                _rootPanel.SetActive(isInCombat);
                if (isInCombat)
                {
                    HookNetworkChat();
                }
                else
                {
                    if (_quickChatModal != null) _quickChatModal.SetActive(false);
                }
            }
        }

        // Tự động né bàn phím ảo trên điện thoại (Mobile Keyboard Avoidance):
        // Khi người chơi chạm vào ô gõ tin nhắn, modal chat tự động nâng lên cao để không bị bàn phím Android che khuất!
        if (_quickChatModal != null && _quickChatModal.activeSelf)
        {
            bool isKbOpen = (_inGameChatInput != null && _inGameChatInput.isFocused);
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            if (TouchScreenKeyboard.visible) isKbOpen = true;
#endif
            Vector2 targetPos = isKbOpen ? new Vector2(560f, 290f) : new Vector2(560f, 110f);
            RectTransform modalRt = _quickChatModal.GetComponent<RectTransform>();
            if (modalRt != null)
            {
                modalRt.anchoredPosition = Vector2.Lerp(modalRt.anchoredPosition, targetPos, Time.unscaledDeltaTime * 14f);
            }
        }
    }

    private bool IsAnySettingsModalActive()
    {
        if (SettingsUI.Instance != null && SettingsUI.Instance.IsOpen()) return true;
        var allSettings = FindObjectsByType<SettingsUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        return allSettings != null && allSettings.Length > 0;
    }

    private void BuildUI()
    {
        if (_canvas != null) return;

        GameObject canvasGo = new GameObject("InGameTeamChat_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 950; // Dưới modal settings nhưng trên gameplay

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _rootPanel = new GameObject("ChatRoot", typeof(RectTransform));
        _rootPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform rootRt = _rootPanel.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        // Container riêng cho Toast thông báo (Luôn luôn sẵn sàng nhận và hiển thị toast ở bất kỳ Scene nào)
        GameObject toastGo = new GameObject("ToastContainer", typeof(RectTransform));
        toastGo.transform.SetParent(canvasGo.transform, false);
        RectTransform toastRt = toastGo.GetComponent<RectTransform>();
        toastRt.anchorMin = Vector2.zero;
        toastRt.anchorMax = Vector2.one;
        toastRt.sizeDelta = Vector2.zero;

        // 1. NÚT CHAT NỔI (Neo góc trên bên phải, đặt ngay dưới nút Cài Đặt bánh răng để không bao giờ đè lên Bảng Tổ Đội)
        GameObject chatBtnObj = CreateRoundedButton(_rootPanel.transform, "Btn_InGameChat",
            "CHAT", Vector2.zero, new Vector2(88f, 42f),
            new Color(0.24f, 0.42f, 0.82f, 0.95f), 16f, ToggleQuickChatModal);
        RectTransform chatRt = chatBtnObj.GetComponent<RectTransform>();
        if (chatRt != null)
        {
            chatRt.anchorMin = new Vector2(1f, 1f);
            chatRt.anchorMax = new Vector2(1f, 1f);
            chatRt.pivot = new Vector2(1f, 1f);
            chatRt.anchoredPosition = new Vector2(-20f, -125f);
            chatRt.sizeDelta = new Vector2(88f, 42f);
        }

        // 2. BANNER TOAST NỔI TIN NHẮN (Hiển thị tin nhắn tức thời trên đầu màn hình trong 3.8s)
        _toastBannerObj = CreateRoundedBox(toastGo.transform, "InGameChatToast",
            new Vector2(0f, 420f), new Vector2(680f, 58f),
            new Color(0.08f, 0.08f, 0.16f, 0.95f));
        CreateBorderBox(_toastBannerObj.transform, "ToastBord", Vector2.zero, new Vector2(680f, 58f), new Color(0.35f, 0.55f, 0.95f, 0.8f));

        _toastBannerText = CreateText(_toastBannerObj.transform, "", 18f, Vector2.zero, new Vector2(650f, 46f), Color.white);
        _toastBannerText.alignment = TextAlignmentOptions.Center;
        _toastBannerObj.SetActive(false);

        // 3. TOAST TƯƠNG TÁC LỜI MỜI PHÒNG (Interactive Room Invite Toast với nút Vào Phòng / Từ Chối)
        BuildInteractiveInviteToast(toastGo.transform);

        // 4. POPUP MODAL CHAT & LỜI MỜI BẠN BÈ
        BuildQuickChatModal(_rootPanel.transform);

        _rootPanel.SetActive(false);
    }

    private void BuildInteractiveInviteToast(Transform parent)
    {
        _inviteToastObj = CreateRoundedBox(parent, "InteractiveInviteToast",
            new Vector2(0f, 350f), new Vector2(740f, 72f),
            new Color(0.12f, 0.14f, 0.24f, 0.98f));
        CreateBorderBox(_inviteToastObj.transform, "InvBord", Vector2.zero, new Vector2(740f, 72f), new Color(0.25f, 0.85f, 0.95f, 0.9f));

        _inviteToastText = CreateText(_inviteToastObj.transform, "", 16f, new Vector2(-130f, 0f), new Vector2(440f, 60f), Color.white);
        _inviteToastText.alignment = TextAlignmentOptions.Left;

        // Nút Vào Phòng Ngay
        CreateRoundedButton(_inviteToastObj.transform, "Btn_JoinInvite",
            LocalizationManager.T("VÀO PHÒNG", "JOIN"),
            new Vector2(170f, 0f), new Vector2(120f, 44f),
            new Color(0.18f, 0.65f, 0.28f, 1f), 15f, OnAcceptInviteClicked);

        // Nút Từ Chối
        CreateRoundedButton(_inviteToastObj.transform, "Btn_DeclineInvite",
            LocalizationManager.T("TỪ CHỐI", "DECLINE"),
            new Vector2(305f, 0f), new Vector2(110f, 44f),
            new Color(0.65f, 0.22f, 0.25f, 1f), 15f, OnDeclineInviteClicked);

        _inviteToastObj.SetActive(false);
    }

    private void OnAcceptInviteClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_inviteToastObj != null) _inviteToastObj.SetActive(false);
        if (!string.IsNullOrEmpty(_pendingInviteRoomPin))
        {
            PvPLobbyUI.AcceptRoomInviteAndJoin(_pendingInviteRoomPin);
        }
    }

    private void OnDeclineInviteClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_inviteToastObj != null) _inviteToastObj.SetActive(false);
    }

    public static void ShowInteractiveInviteToast(string sender, string roomPin)
    {
        if (Instance == null) AutoInit();
        if (Instance != null)
        {
            Instance.TriggerInviteToast(sender, roomPin);
        }
    }

    public void TriggerInviteToast(string sender, string roomPin)
    {
        if (_canvas == null) BuildUI();
        _pendingInviteRoomPin = roomPin;
        if (_inviteToastObj != null && _inviteToastText != null)
        {
            _inviteToastText.text = LocalizationManager.T(
                $"<color=#FFD54F><b>{sender}</b></color> mời bạn vào phòng Co-op <color=#00E5FF>#{roomPin}</color>!",
                $"<color=#FFD54F><b>{sender}</b></color> invited you to Co-op Room <color=#00E5FF>#{roomPin}</color>!");
            _inviteToastObj.transform.SetAsLastSibling();
            _inviteToastObj.SetActive(true);

            if (_inviteToastCoroutine != null) StopCoroutine(_inviteToastCoroutine);
            _inviteToastCoroutine = StartCoroutine(HideInviteToastAfterDelay(10f));
        }
    }

    private IEnumerator HideInviteToastAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_inviteToastObj != null) _inviteToastObj.SetActive(false);
    }

    public static void ShowFriendRequestNotification(string sender)
    {
        if (Instance != null)
        {
            Instance.HandleInGameChatMessage("HỆ THỐNG", LocalizationManager.T(
                $"Hiệp sĩ {sender} vừa gửi cho bạn một lời mời kết bạn! Nhấn CHAT để duyệt.",
                $"Knight {sender} sent you a friend request! Tap CHAT to review."));
        }
    }

    private void BuildQuickChatModal(Transform parent)
    {
        _quickChatModal = CreateRoundedBox(parent, "QuickChatModal", new Vector2(560f, 110f), new Vector2(500f, 490f), new Color(0.10f, 0.09f, 0.18f, 0.98f));
        CreateBorderBox(_quickChatModal.transform, "ModalBord", Vector2.zero, new Vector2(500f, 490f), new Color(0.32f, 0.40f, 0.65f, 1f));

        // Nút Đóng 'X'
        CreateRoundedButton(_quickChatModal.transform, "Btn_Close", "X",
            new Vector2(215f, 210f), new Vector2(38f, 38f),
            new Color(0.45f, 0.20f, 0.25f, 1f), 16f, () => _quickChatModal.SetActive(false));

        // 2 Nút Tab: [CHAT TỔ ĐỘI] và [LỜI MỜI (X)]
        GameObject tabChatObj = CreateRoundedButton(_quickChatModal.transform, "Btn_TabChat",
            LocalizationManager.T("CHAT TỔ ĐỘI", "TEAM CHAT"),
            new Vector2(-105f, 210f), new Vector2(180f, 38f),
            new Color(0.24f, 0.45f, 0.85f, 1f), 14f, () => SwitchModalTab(true));
        _tabChatBtn = tabChatObj.GetComponent<Button>();
        _tabChatBtnText = tabChatObj.GetComponentInChildren<TextMeshProUGUI>();

        GameObject tabReqObj = CreateRoundedButton(_quickChatModal.transform, "Btn_TabReqs",
            LocalizationManager.T("LỜI MỜI (0)", "REQUESTS (0)"),
            new Vector2(85f, 210f), new Vector2(180f, 38f),
            new Color(0.18f, 0.22f, 0.32f, 1f), 14f, () => SwitchModalTab(false));
        _tabRequestsBtn = tabReqObj.GetComponent<Button>();
        _tabRequestsBtnText = tabReqObj.GetComponentInChildren<TextMeshProUGUI>();

        // PANEL 1: CHAT TỔ ĐỘI & LỊCH SỬ TIN NHẮN
        _panelChatTab = new GameObject("Panel_ChatTab", typeof(RectTransform));
        _panelChatTab.transform.SetParent(_quickChatModal.transform, false);
        RectTransform pcRt = _panelChatTab.GetComponent<RectTransform>();
        pcRt.anchorMin = Vector2.zero;
        pcRt.anchorMax = Vector2.one;
        pcRt.sizeDelta = Vector2.zero;

        // 1. Khung hiển thị LỊCH SỬ TIN NHẮN (Chat History View)
        GameObject logBox = CreateRoundedBox(_panelChatTab.transform, "ChatLogBox",
            new Vector2(0f, 65f), new Vector2(460f, 220f),
            new Color(0.05f, 0.05f, 0.10f, 0.95f));
        CreateBorderBox(logBox.transform, "LogBord", Vector2.zero, new Vector2(460f, 220f), new Color(0.25f, 0.35f, 0.55f, 0.7f));

        GameObject scrollRoot = new GameObject("ChatLogScroll", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        scrollRoot.transform.SetParent(logBox.transform, false);
        RectTransform srRt = scrollRoot.GetComponent<RectTransform>();
        srRt.anchorMin = Vector2.zero;
        srRt.anchorMax = Vector2.one;
        srRt.offsetMin = new Vector2(8f, 8f);
        srRt.offsetMax = new Vector2(-8f, -8f);

        _chatHistoryScroll = scrollRoot.GetComponent<ScrollRect>();
        _chatHistoryScroll.horizontal = false;
        _chatHistoryScroll.vertical = true;
        _chatHistoryScroll.scrollSensitivity = 25f;

        GameObject contentObj = new GameObject("ChatLogContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollRoot.transform, false);
        RectTransform cntRt = contentObj.GetComponent<RectTransform>();
        cntRt.anchorMin = new Vector2(0f, 1f);
        cntRt.anchorMax = new Vector2(1f, 1f);
        cntRt.pivot = new Vector2(0.5f, 1f);

        var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _chatHistoryScroll.content = cntRt;
        _chatHistoryScroll.viewport = srRt;
        _chatHistoryContent = contentObj.transform;

        // Nạp lại các tin nhắn đã lưu trước đó nếu có
        RefreshChatHistoryView();

        // 2. Các nút Ping Nhanh (2 hàng x 2 cột gọn gàng)
        CreateRoundedButton(_panelChatTab.transform, "Btn_Ping_0", "TẬP TRUNG LẠI!",
            new Vector2(-115f, -72f), new Vector2(220f, 34f),
            new Color(0.18f, 0.24f, 0.42f, 1f), 13f, () => SendQuickPing("TẬP TRUNG LẠI!"));

        CreateRoundedButton(_panelChatTab.transform, "Btn_Ping_1", "CỨU TUI VỚI!",
            new Vector2(115f, -72f), new Vector2(220f, 34f),
            new Color(0.18f, 0.24f, 0.42f, 1f), 13f, () => SendQuickPing("CỨU TUI VỚI!"));

        CreateRoundedButton(_panelChatTab.transform, "Btn_Ping_2", "ĐÁNH BOSS ĐI!",
            new Vector2(-115f, -112f), new Vector2(220f, 34f),
            new Color(0.18f, 0.24f, 0.42f, 1f), 13f, () => SendQuickPing("ĐÁNH BOSS ĐI!"));

        CreateRoundedButton(_panelChatTab.transform, "Btn_Ping_3", "CỐ LÊN ANH EM!",
            new Vector2(115f, -112f), new Vector2(220f, 34f),
            new Color(0.18f, 0.24f, 0.42f, 1f), 13f, () => SendQuickPing("CỐ LÊN ANH EM!"));

        // 3. Hàng nhập tin nhắn tự do (ở dưới cùng)
        GameObject inputBg = CreateRoundedBox(_panelChatTab.transform, "InputBg",
            new Vector2(-55f, -165f), new Vector2(330f, 44f),
            new Color(0.06f, 0.05f, 0.10f, 1f));
        CreateBorderBox(inputBg.transform, "InBord", Vector2.zero, new Vector2(330f, 44f), new Color(0.28f, 0.35f, 0.55f, 1f));

        GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputBg.transform, false);
        RectTransform taRt = textArea.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(10f, 4f);
        taRt.offsetMax = new Vector2(-10f, -4f);

        TextMeshProUGUI placeholder = CreateText(textArea.transform, LocalizationManager.T("Gõ tin nhắn...", "Type message..."), 14f, Vector2.zero, new Vector2(300f, 32f), new Color(0.5f, 0.55f, 0.65f, 0.65f));
        placeholder.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI inputText = CreateText(textArea.transform, "", 16f, Vector2.zero, new Vector2(300f, 32f), Color.white);
        inputText.alignment = TextAlignmentOptions.Left;

        _inGameChatInput = inputBg.AddComponent<TMP_InputField>();
        _inGameChatInput.textViewport = taRt;
        _inGameChatInput.textComponent = inputText;
        _inGameChatInput.placeholder = placeholder;
        _inGameChatInput.characterLimit = 40;
        _inGameChatInput.shouldHideMobileInput = false; // Luôn hiển thị chữ đang gõ trên màn hình

        CreateRoundedButton(_panelChatTab.transform, "Btn_Send",
            LocalizationManager.T("GỬI", "SEND"),
            new Vector2(170f, -165f), new Vector2(95f, 44f),
            new Color(0.22f, 0.65f, 0.35f, 1f), 15f, SendInputMessage);

        // PANEL 2: LỜI MỜI KẾT BẠN (FRIEND REQUESTS TAB)
        _panelRequestsTab = new GameObject("Panel_RequestsTab", typeof(RectTransform));
        _panelRequestsTab.transform.SetParent(_quickChatModal.transform, false);
        RectTransform prRt = _panelRequestsTab.GetComponent<RectTransform>();
        prRt.anchorMin = Vector2.zero;
        prRt.anchorMax = Vector2.one;
        prRt.sizeDelta = Vector2.zero;

        // Scrollview chứa danh sách yêu cầu kết bạn
        GameObject reqScrollRoot = new GameObject("ReqsScroll", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        reqScrollRoot.transform.SetParent(_panelRequestsTab.transform, false);
        RectTransform reqSrRt = reqScrollRoot.GetComponent<RectTransform>();
        reqSrRt.anchorMin = Vector2.zero;
        reqSrRt.anchorMax = Vector2.one;
        reqSrRt.offsetMin = new Vector2(20f, 20f);
        reqSrRt.offsetMax = new Vector2(-20f, -65f);

        ScrollRect sRect = reqScrollRoot.GetComponent<ScrollRect>();
        sRect.horizontal = false;
        sRect.vertical = true;
        sRect.scrollSensitivity = 28f;

        GameObject reqContentObj = new GameObject("ReqContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        reqContentObj.transform.SetParent(reqScrollRoot.transform, false);
        RectTransform reqCntRt = reqContentObj.GetComponent<RectTransform>();
        reqCntRt.anchorMin = new Vector2(0f, 1f);
        reqCntRt.anchorMax = new Vector2(1f, 1f);
        reqCntRt.pivot = new Vector2(0.5f, 1f);

        var reqVlg = reqContentObj.GetComponent<VerticalLayoutGroup>();
        reqVlg.spacing = 8f;
        reqVlg.padding = new RectOffset(4, 4, 4, 4);
        reqVlg.childControlHeight = false;
        reqVlg.childControlWidth = true;
        reqVlg.childForceExpandHeight = false;
        reqVlg.childForceExpandWidth = true;

        var reqCsf = reqContentObj.GetComponent<ContentSizeFitter>();
        reqCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sRect.content = reqCntRt;
        sRect.viewport = reqSrRt;
        _requestListContent = reqContentObj.transform;

        _panelRequestsTab.SetActive(false);
        _quickChatModal.SetActive(false);
    }

    private void SwitchModalTab(bool isChat)
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_panelChatTab != null) _panelChatTab.SetActive(isChat);
        if (_panelRequestsTab != null) _panelRequestsTab.SetActive(!isChat);

        if (_tabChatBtn != null)
        {
            var img = _tabChatBtn.GetComponent<Image>();
            if (img != null) img.color = isChat ? new Color(0.24f, 0.45f, 0.85f, 1f) : new Color(0.18f, 0.22f, 0.32f, 1f);
        }
        if (_tabRequestsBtn != null)
        {
            var img = _tabRequestsBtn.GetComponent<Image>();
            if (img != null) img.color = !isChat ? new Color(0.24f, 0.45f, 0.85f, 1f) : new Color(0.18f, 0.22f, 0.32f, 1f);
        }

        if (!isChat)
        {
            RefreshPendingRequestsList();
        }
    }

    private void RefreshPendingRequestsList()
    {
        if (_requestListContent == null) return;

        for (int i = _requestListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(_requestListContent.GetChild(i).gameObject);
        }

        var requests = FriendManager.EnsureInstance()?.GetPendingRequests();
        int count = requests != null ? requests.Count : 0;

        if (_tabRequestsBtnText != null)
        {
            _tabRequestsBtnText.text = LocalizationManager.T($"LỜI MỜI ({count})", $"REQUESTS ({count})");
        }

        if (count == 0)
        {
            GameObject emptyItem = new GameObject("EmptyReqText", typeof(RectTransform));
            emptyItem.transform.SetParent(_requestListContent, false);
            RectTransform eRt = emptyItem.GetComponent<RectTransform>();
            eRt.sizeDelta = new Vector2(0f, 60f);
            var emptyTxt = CreateText(emptyItem.transform, LocalizationManager.T("Không có lời mời kết bạn nào.", "No friend requests found."), 15f, Vector2.zero, new Vector2(380f, 50f), new Color(0.6f, 0.65f, 0.75f));
            emptyTxt.alignment = TextAlignmentOptions.Center;
            return;
        }

        foreach (var req in requests)
        {
            string sender = req.senderName;
            GameObject itemObj = CreateRoundedBox(_requestListContent, "ReqItem_" + sender, Vector2.zero, new Vector2(0f, 65f), new Color(0.14f, 0.16f, 0.26f, 0.95f));
            CreateBorderBox(itemObj.transform, "ReqItemBord", Vector2.zero, new Vector2(0f, 65f), new Color(0.35f, 0.45f, 0.65f, 0.6f));

            // Tên và thông tin hiệp sĩ
            TextMeshProUGUI nameTxt = CreateText(itemObj.transform, $"<b>{sender}</b>\n<size=12><color=#A0B0D0>{req.senderHeroId} (Lv.{req.senderLevel})</color></size>", 15f, new Vector2(-75f, 0f), new Vector2(230f, 50f), Color.white);
            nameTxt.alignment = TextAlignmentOptions.Left;

            // Nút Chấp Nhận
            CreateRoundedButton(itemObj.transform, "Btn_Acc",
                LocalizationManager.T("ĐỒNG Ý", "ACCEPT"),
                new Vector2(95f, 0f), new Vector2(75f, 38f),
                new Color(0.18f, 0.65f, 0.28f, 1f), 13f, () =>
                {
                    SoundManager.EnsureInstance()?.PlayButtonClick();
                    FriendManager.EnsureInstance().AcceptFriendRequest(sender);
                    RefreshPendingRequestsList();
                    HandleInGameChatMessage("BẠN BÈ", LocalizationManager.T($"Đã kết bạn thành công với {sender}!", $"Added {sender} as friend!"));
                });

            // Nút Từ Chối
            CreateRoundedButton(itemObj.transform, "Btn_Dec",
                LocalizationManager.T("TỪ CHỐI", "DECLINE"),
                new Vector2(175f, 0f), new Vector2(75f, 38f),
                new Color(0.65f, 0.22f, 0.25f, 1f), 13f, () =>
                {
                    SoundManager.EnsureInstance()?.PlayButtonClick();
                    FriendManager.EnsureInstance().DeclineFriendRequest(sender);
                    RefreshPendingRequestsList();
                });
        }
    }

    private void ToggleQuickChatModal()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_quickChatModal != null)
        {
            bool willOpen = !_quickChatModal.activeSelf;
            _quickChatModal.SetActive(willOpen);
            if (willOpen)
            {
                int count = FriendManager.EnsureInstance()?.GetPendingRequestCount() ?? 0;
                if (_tabRequestsBtnText != null)
                {
                    _tabRequestsBtnText.text = LocalizationManager.T($"LỜI MỜI ({count})", $"REQUESTS ({count})");
                }
                SwitchModalTab(true);
            }
        }
    }

    private void SendQuickPing(string text)
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        string myName = AccountDatabase.GetActiveAccount() != null
            ? (!string.IsNullOrEmpty(AccountDatabase.GetActiveAccount().displayName) ? AccountDatabase.GetActiveAccount().displayName : AccountDatabase.GetActiveAccount().username)
            : PlayerPrefs.GetString("SAVED_LOGIN_USERNAME", "Tôi");

        AddMessageToChatHistory(myName, text);

        if (PvPNetworkManager.Instance != null)
        {
            PvPNetworkManager.Instance.SendChatMessage(text);
        }
    }

    private void SendInputMessage()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_inGameChatInput != null && !string.IsNullOrEmpty(_inGameChatInput.text.Trim()))
        {
            string msg = _inGameChatInput.text.Trim();
            string myName = AccountDatabase.GetActiveAccount() != null
                ? (!string.IsNullOrEmpty(AccountDatabase.GetActiveAccount().displayName) ? AccountDatabase.GetActiveAccount().displayName : AccountDatabase.GetActiveAccount().username)
                : PlayerPrefs.GetString("SAVED_LOGIN_USERNAME", "Tôi");

            AddMessageToChatHistory(myName, msg);

            if (PvPNetworkManager.Instance != null)
            {
                PvPNetworkManager.Instance.SendChatMessage(msg);
            }
            _inGameChatInput.text = "";
        }
    }

    public void HandleInGameChatMessage(string sender, string message)
    {
        AddMessageToChatHistory(sender, message);

        // Hiển thị toast nổi trên đầu màn hình (Tuyệt đối loại bỏ triệt để ký tự [ và ] vì font pixel map [ thành chữ z)
        if (_toastBannerObj != null && _toastBannerText != null)
        {
            string cleanSender = string.IsNullOrEmpty(sender) ? "" : sender.Replace("[", "(").Replace("]", ")").Replace("zTRỐNG", "TRỐNG").Trim();
            string cleanMsg = string.IsNullOrEmpty(message) ? "" : message.Replace("[", "(").Replace("]", ")").Trim();
            _toastBannerText.text = $"<color=#FFD54F>{cleanSender}:</color> {cleanMsg}";
            _toastBannerObj.SetActive(true);

            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(HideToastAfterDelay(3.8f));
        }
    }

    public void AddMessageToChatHistory(string sender, string message)
    {
        string cleanSender = string.IsNullOrEmpty(sender) ? "" : sender.Replace("[", "(").Replace("]", ")").Replace("zTRỐNG", "TRỐNG").Trim();
        string cleanMsg = string.IsNullOrEmpty(message) ? "" : message.Replace("[", "(").Replace("]", ")").Trim();
        string timeStr = System.DateTime.Now.ToString("HH:mm");

        string line = $"<color=#8FA0C0>({timeStr})</color> <color=#FFD54F><b>{cleanSender}:</b></color> <color=#FFFFFF>{cleanMsg}</color>";
        _chatHistoryLog.Add(line);
        if (_chatHistoryLog.Count > MAX_CHAT_LOG)
        {
            _chatHistoryLog.RemoveAt(0);
        }

        if (_chatHistoryContent != null)
        {
            CreateChatLogRow(line);
            StartCoroutine(ScrollToBottomNextFrame());
        }
    }

    private void CreateChatLogRow(string line)
    {
        if (_chatHistoryContent == null) return;
        GameObject row = new GameObject("ChatMsgRow", typeof(RectTransform));
        row.transform.SetParent(_chatHistoryContent, false);
        RectTransform rRt = row.GetComponent<RectTransform>();
        rRt.sizeDelta = new Vector2(0f, 24f);

        var tmp = row.AddComponent<TextMeshProUGUI>();
        tmp.text = line;
        tmp.fontSize = 13.5f;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
    }

    private void RefreshChatHistoryView()
    {
        if (_chatHistoryContent == null) return;
        for (int i = _chatHistoryContent.childCount - 1; i >= 0; i--)
        {
            Destroy(_chatHistoryContent.GetChild(i).gameObject);
        }
        foreach (var line in _chatHistoryLog)
        {
            CreateChatLogRow(line);
        }
        StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        if (_chatHistoryScroll != null)
        {
            _chatHistoryScroll.normalizedPosition = new Vector2(0f, 0f);
        }
    }

    private IEnumerator HideToastAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_toastBannerObj != null) _toastBannerObj.SetActive(false);
    }

    // Helper UI
    private GameObject CreateRoundedBox(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.color = color;
        return go;
    }

    private void CreateBorderBox(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.color = Color.clear;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2f, 2f);
    }

    private GameObject CreateRoundedButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color, float fontSize, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.color = color;
        Button btn = go.GetComponent<Button>();
        if (onClick != null) btn.onClick.AddListener(onClick);

        CreateText(go.transform, label, fontSize, Vector2.zero, size, Color.white);
        return go;
    }

    private TextMeshProUGUI CreateText(Transform parent, string content, float fontSize, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        return tmp;
    }
}
