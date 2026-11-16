using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hệ thống Giao diện Sảnh Đấu Trường PvP Đối Kháng 1v1 theo chuẩn Lưu Đồ 9 Bước (Fighters Arena Flow):
/// - Bước 1: Main Menu -> Nút "PvP Đối Kháng"
/// - Bước 2: Tạo Trận / Chọn Chế Độ (Đấu Thường Quick Match, Tạo Phòng Custom Room, Tìm Phòng Mã PIN, Đấu Tập AI)
/// - Bước 3: Chọn Nhân Vật (Lưới 7 Tướng + Khung Chi Tiết Tướng & Chỉ Số Lôi Đài + Nút Xác Nhận)
/// - Bước 4: Tạo Phòng Custom Room (Chế độ, Số người, Map, Mã PIN) & Tìm Phòng bằng Bàn Phím Số
/// - Bước 5: Phòng Chờ Người Chơi (Phòng #PIN - 2 ô Đấu Sĩ Trái/Phải + Trạng thái Sẵn Sàng + Rời Phòng / Bắt Đầu Trận)
/// - Bước 6: Đang Ghép Trận Quick Match (Vòng Radar tìm trận + Bộ đếm thời gian + Hủy Tìm Trận)
/// - Bước 7: Ghép Thành Công - Màn Hình VS (Chân dung 2 Đấu Sĩ đối đầu VS + Đếm ngược "Chuẩn bị... 3, 2, 1")
/// (Bước 8: HUD Trận Đấu & Bước 9: Màn Hình Kết Quả YOU WIN! được hiển thị trong PvPMatchManager).
/// </summary>
public class PvPLobbyUI : MonoBehaviour
{
    public static PvPLobbyUI Instance { get; private set; }

    public enum LobbyStep
    {
        Step2_ModeSelect,
        Step3_HeroSelect,
        Step4_CreateRoom,
        Step4B_JoinRoomPin,
        Step5_WaitingRoom,
        Step6_QuickMatchSearching,
        Step7_VsMatchFound
    }

    public enum SelectedPvPFlowMode
    {
        QuickMatch = 0,
        CreateCustomRoom = 1,
        JoinRoomByPin = 2,
        BotPractice = 3
    }

    private GameObject _modalRoot;
    private LobbyStep _currentStep = LobbyStep.Step2_ModeSelect;
    private SelectedPvPFlowMode _selectedMode = SelectedPvPFlowMode.QuickMatch;
    private bool _openedDirectlyFromMainMenu = false;
    private TextMeshProUGUI _step3HeaderTitleText;
    private TextMeshProUGUI _step3ConfirmBtnText;
    private Image _step3ConfirmBtnImg;
    private GameObject _lobbyToastObj;
    private TextMeshProUGUI _lobbyToastText;
    private Coroutine _lobbyToastRoutine;

    // Các Panel tương ứng từng bước
    private GameObject _step2ModePanel;
    private GameObject _step3HeroPanel;
    private GameObject _step4CreateRoomPanel;
    private GameObject _step4BJoinPinPanel;
    private GameObject _step5WaitingRoomPanel;
    private GameObject _step6SearchingPanel;
    private GameObject _step7VsPanel;

    // Tham chiếu UI Bước 2 (Chọn Chế Độ)
    private readonly List<Image> _modeTabBackgrounds = new List<Image>();
    private TextMeshProUGUI _modePreviewTitleText;
    private TextMeshProUGUI _modePreviewDescText;
    private Image _modePreviewLeftHeroImg;
    private Image _modePreviewRightHeroImg;

    // Tham chiếu UI Bước 3 (Chọn Nhân Vật)
    private string _previewHeroId = CharacterManager.CHAR_VAELSTRIDER;
    private readonly Dictionary<string, Image> _heroGridBorders = new Dictionary<string, Image>();
    private Image _heroShowcaseImg;
    private TextMeshProUGUI _heroShowcaseNameText;
    private TextMeshProUGUI _heroShowcaseRoleText;
    private TextMeshProUGUI _heroShowcaseStatsText;
    private TextMeshProUGUI _heroShowcaseSkillText;

    // Tham chiếu UI Bước 4 (Tạo Phòng Custom Room)
    private int _customRuleIndex = 0;
    private int _customRoundFormatIndex = 1; // 0 = Bo1 (1 Hiệp), 1 = Bo3 (Chạm 2 Thắng), 2 = Bo5 (Chạm 3 Thắng)
    private int _customMapIndex = 0;
    private string _pendingCustomPin = "4829";
    private TextMeshProUGUI _customRuleValText;
    private TextMeshProUGUI _customRoundFormatValText;
    private TextMeshProUGUI _customMapValText;
    private TextMeshProUGUI _customPinValText;
    private TextMeshProUGUI _step4PreviewInfoText;

    // Tham chiếu UI Bước 4B (Tìm Phòng bằng Mã PIN)
    private TMP_InputField _pinInputField;
    private string _enteredPin = "";
    private TextMeshProUGUI _joinPinStatusText;

    // Tham chiếu UI Bước 5 (Phòng Chờ Co-op 2-4 Người)
    private TextMeshProUGUI _roomHeaderTitleText;
    private TextMeshProUGUI _roomPlayerCountText;
    private Image _roomLeftAvatarImg;
    private TextMeshProUGUI _roomLeftNameText;
    private TextMeshProUGUI _roomLeftHeroText;
    private TextMeshProUGUI _roomLeftReadyText;
    private Image _roomRightAvatarImg;
    private TextMeshProUGUI _roomRightMysteryText;
    private TextMeshProUGUI _roomRightNameText;
    private TextMeshProUGUI _roomRightHeroText;
    private TextMeshProUGUI _roomRightReadyText;
    private Button _roomStartMatchBtn;
    private Image _roomStartMatchBtnImg;
    private TextMeshProUGUI _roomStartMatchBtnText;
    private Button _roomClientReadyBtn;
    private Image _roomClientReadyBtnImg;
    private TextMeshProUGUI _roomClientReadyBtnText;
    private TextMeshProUGUI _roomStatusFooterText;
    private TMP_InputField _headerPinInput;

    private struct CoopCardUI
    {
        public GameObject cardObj;
        public Image avatarImg;
        public GameObject silhouetteObj;
        public GameObject plusBtnObj;
        public GameObject onlineDotObj;
        public GameObject crownBadgeObj;
        public TextMeshProUGUI roleTitleText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI heroText;
        public TextMeshProUGUI readyText;
        public GameObject readyPillObj;
        public Image readyPillBg;
        public GameObject invitePromptObj;
        public GameObject changeHeroBtnObj;
        public GameObject kickBtnObj;
        public GameObject addFriendBtnObj;
    }
    private readonly CoopCardUI[] _coopCardSlots = new CoopCardUI[4];

    // Hộp thoại hướng dẫn mời bạn bè
    private GameObject _inviteGuideDialogObj;
    private TextMeshProUGUI _inviteGuideText;

    // Bảng chọn tướng trực tiếp tại phòng chờ
    private GameObject _heroSelectionModalObj;
    private class HeroCardSlotUI
    {
        public string heroId;
        public GameObject cardObj;
        public Image cardBg;
        public Image cardBord;
        public Image avBord;
        public Image portraitImg;
        public GameObject lockOverlayObj;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI subText;
        public Button cardBtn;
    }
    private readonly System.Collections.Generic.List<HeroCardSlotUI> _heroCardSlots = new System.Collections.Generic.List<HeroCardSlotUI>();

    // Hệ thống Chat Phòng Chờ
    private GameObject _lobbyChatModalObj;
    private TextMeshProUGUI _lobbyChatHistoryText;
    private TMP_InputField _lobbyChatInputField;
    private TextMeshProUGUI _lobbyChatBtnBadgeText;
    private int _unreadChatCount = 0;
    private readonly System.Collections.Generic.List<string> _chatMessages = new System.Collections.Generic.List<string>();

    // Hệ thống Quản Lý Bạn Bè
    // Hệ thống Quản Lý Bạn Bè (3 Tabs)
    private GameObject _friendsModalObj;
    private GameObject _friendTabFriendsPanel;
    private GameObject _friendTabRequestsPanel;
    private GameObject _friendTabAddPanel;
    private Image _friendTabFriendsBtnImg;
    private Image _friendTabRequestsBtnImg;
    private Image _friendTabAddBtnImg;
    private TextMeshProUGUI _friendsTabCountText;
    private TextMeshProUGUI _requestsTabCountText;
    private Transform _friendsListContainer;
    private Transform _friendRequestsListContainer;
    private TMP_InputField _addFriendNameInput;
    private bool _openedFromMainMenuOnly = false;

    // Modal Lời Mời Vào Phòng Co-op (Invite Popup Dialog)
    private GameObject _roomInviteDialogObj;
    private TextMeshProUGUI _roomInvitePromptText;
    private string _pendingInvitePin = "";

    // Modal Đặt / Sửa Tên Ghi Nhớ ()
    private GameObject _editNicknameModalObj;
    private TMP_InputField _editNicknameInput;
    private string _editingFriendId = "";

    // Tham chiếu UI Bước 6 (Đang Ghép Trận Quick Match)
    private float _searchElapsedSeconds = 0f;
    private RectTransform _searchRadarRingRt;
    private TextMeshProUGUI _searchTimerText;
    private TextMeshProUGUI _searchStatusSubText;

    // Tham chiếu UI Bước 7 (Ghép Thành Công - VS Screen)
    private float _vsCountdownTimer = 3.0f;
    private bool _vsCountdownActive = false;
    private Image _vsLeftHeroImg;
    private TextMeshProUGUI _vsLeftNameText;
    private TextMeshProUGUI _vsLeftHeroTitleText;
    private Image _vsRightHeroImg;
    private TextMeshProUGUI _vsRightNameText;
    private TextMeshProUGUI _vsRightHeroTitleText;
    private TextMeshProUGUI _vsCountdownText;

    private static readonly string[] ALL_HERO_IDS = new string[]
    {
        CharacterManager.CHAR_VAELSTRIDER,
        CharacterManager.CHAR_GALE_RONIN,
        CharacterManager.CHAR_AZURE_BLADE,
        CharacterManager.CHAR_TITAN_BERSERKER,
        CharacterManager.CHAR_DUNE_ASSASSIN,
        CharacterManager.CHAR_CELESTIAL_MAGE,
        CharacterManager.CHAR_ABYSSAL_MECH
    };

    public static PvPLobbyUI EnsureInstance(Transform canvasTransform = null)
    {
        if (Instance != null && Instance._modalRoot != null) return Instance;

        if (canvasTransform == null)
        {
            var canvas = GameAutoBootstrap.GetMainGameplayCanvas();
            if (canvas != null) canvasTransform = canvas.transform;
        }
        if (canvasTransform == null) return null;

        GameObject go = new GameObject("PvPLobbyModalRoot", typeof(RectTransform));
        go.transform.SetParent(canvasTransform, false);
        Instance = go.AddComponent<PvPLobbyUI>();
        Instance.BuildAllStepsUI(go.transform);
        return Instance;
    }

    public static void OpenLobby()
    {
        var inst = EnsureInstance();
        if (inst == null) return;

        inst._modalRoot.SetActive(true);
        inst._modalRoot.transform.SetAsLastSibling();
        inst._previewHeroId = CharacterManager.EnsureInstance().GetSelectedCharacterId();

        var net = PvPNetworkManager.EnsureInstance();
        net.OnStatusChanged -= inst.HandleNetworkStatusChanged;
        net.OnStatusChanged += inst.HandleNetworkStatusChanged;
        net.OnMatchReadyToStart -= inst.HandleMatchReadyToStart;
        net.OnMatchReadyToStart += inst.HandleMatchReadyToStart;
        net.OnOpponentIdentityUpdated -= inst.HandleOpponentIdentityUpdated;
        net.OnOpponentIdentityUpdated += inst.HandleOpponentIdentityUpdated;
        net.OnOpponentReadyChanged -= inst.HandleOpponentReadyChanged;
        net.OnOpponentReadyChanged += inst.HandleOpponentReadyChanged;
        net.OnChatMessageReceived -= inst.HandleChatMessageReceived;
        net.OnChatMessageReceived += inst.HandleChatMessageReceived;

        inst.ShowStep(LobbyStep.Step2_ModeSelect);
    }

    public static void CloseLobbyModal()
    {
        if (Instance != null) Instance.CloseLobby();
    }

    public static bool IsLobbyModalOpen => Instance != null && Instance._modalRoot != null && Instance._modalRoot.activeSelf;

    public void CloseLobby()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        _vsCountdownActive = false;
        if (PvPNetworkManager.Instance != null)
        {
            PvPNetworkManager.Instance.OnStatusChanged -= HandleNetworkStatusChanged;
            PvPNetworkManager.Instance.OnMatchReadyToStart -= HandleMatchReadyToStart;
            PvPNetworkManager.Instance.OnOpponentIdentityUpdated -= HandleOpponentIdentityUpdated;
            PvPNetworkManager.Instance.OnOpponentReadyChanged -= HandleOpponentReadyChanged;
            PvPNetworkManager.Instance.OnChatMessageReceived -= HandleChatMessageReceived;
            if (!PvPMatchManager.IsPvPModeActive && !PvPMatchManager.IsCoopModeActive)
            {
                PvPNetworkManager.Instance.ShutdownRunnerOnly();
            }
        }
        if (_modalRoot != null) _modalRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (PvPNetworkManager.Instance != null)
        {
            PvPNetworkManager.Instance.OnStatusChanged -= HandleNetworkStatusChanged;
            PvPNetworkManager.Instance.OnMatchReadyToStart -= HandleMatchReadyToStart;
            PvPNetworkManager.Instance.OnOpponentIdentityUpdated -= HandleOpponentIdentityUpdated;
            PvPNetworkManager.Instance.OnOpponentReadyChanged -= HandleOpponentReadyChanged;
            PvPNetworkManager.Instance.OnChatMessageReceived -= HandleChatMessageReceived;
        }
        if (Instance == this) Instance = null;
    }

    private void HandleChatMessageReceived(string sender, string msg)
    {
        string cleanSender = string.IsNullOrEmpty(sender) ? "" : sender.Replace("[", "").Replace("]", "").Trim();
        string cleanMsg = string.IsNullOrEmpty(msg) ? "" : msg.Replace("[", "").Replace("]", "").Trim();
        string formatted = $"<color=#FFD54F>{cleanSender}:</color> {cleanMsg}";
        _chatMessages.Add(formatted);
        if (_chatMessages.Count > 30) _chatMessages.RemoveAt(0);

        if (_lobbyChatModalObj != null && _lobbyChatModalObj.activeSelf)
        {
            UpdateLobbyChatHistoryUI();
        }
        else
        {
            _unreadChatCount++;
            if (_lobbyChatBtnBadgeText != null)
            {
                _lobbyChatBtnBadgeText.text = $"CHAT ({_unreadChatCount})";
                _lobbyChatBtnBadgeText.color = new Color(1f, 0.90f, 0.40f);
            }
            ShowLobbyToast(formatted, new Color(0.45f, 0.85f, 1f));
        }
    }

    private void HandleOpponentReadyChanged()
    {
        if (_currentStep == LobbyStep.Step5_WaitingRoom)
        {
            RefreshStep5WaitingRoomUI();
        }
    }

    private void Update()
    {
        if (_modalRoot == null || !_modalRoot.activeSelf) return;

        // Cập nhật Bước 5: Phòng Chờ (1/2 -> 2/2)
        if (_currentStep == LobbyStep.Step5_WaitingRoom)
        {
            RefreshStep5WaitingRoomUI();
        }
        // Cập nhật Bước 6: Đang Ghép Trận Quick Match
        else if (_currentStep == LobbyStep.Step6_QuickMatchSearching)
        {
            _searchElapsedSeconds += Time.unscaledDeltaTime;
            if (_searchRadarRingRt != null)
            {
                _searchRadarRingRt.Rotate(0f, 0f, -140f * Time.unscaledDeltaTime);
            }
            if (_searchTimerText != null)
            {
                int mins = Mathf.FloorToInt(_searchElapsedSeconds / 60f);
                int secs = Mathf.FloorToInt(_searchElapsedSeconds % 60f);
                _searchTimerText.text = LocalizationManager.T(
                    $"THỜI GIAN TÌM: {mins:00}:{secs:00}   |   ƯỚC TÍNH: 00:15",
                    $"ELAPSED: {mins:00}:{secs:00}   |   ESTIMATED: 00:15");
            }
        }
        // Cập nhật Bước 7: Màn Hình VS Đếm Ngược 3 - 2 - 1
        else if (_currentStep == LobbyStep.Step7_VsMatchFound && _vsCountdownActive)
        {
            _vsCountdownTimer -= Time.unscaledDeltaTime;
            int ceilSec = Mathf.Clamp(Mathf.CeilToInt(_vsCountdownTimer), 1, 3);
            if (_vsCountdownText != null)
            {
                _vsCountdownText.text = LocalizationManager.T(
                    $"CHUẨN BỊ VÀO TRẬN... {ceilSec}",
                    $"PREPARING FOR BATTLE... {ceilSec}");
            }

            if (_vsCountdownTimer <= 0f)
            {
                _vsCountdownActive = false;
                if (_modalRoot != null) _modalRoot.SetActive(false);
                if (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsBotPracticeMode)
                {
                    PvPNetworkManager.Instance.EnterPvPArenaScene();
                }
            }
        }
    }

    private void HandleNetworkStatusChanged(string msg)
    {
        if (msg == null) msg = "";
        msg = msg.Replace("[", "(").Replace("]", ")");
        if (_roomStatusFooterText != null) _roomStatusFooterText.text = msg;
        if (_searchStatusSubText != null) _searchStatusSubText.text = msg;
        if (_joinPinStatusText != null) _joinPinStatusText.text = msg;
        if (_currentStep == LobbyStep.Step5_WaitingRoom)
        {
            RefreshStep5WaitingRoomUI();
        }
    }

    private void HandleOpponentIdentityUpdated()
    {
        if (_currentStep == LobbyStep.Step5_WaitingRoom)
        {
            RefreshStep5WaitingRoomUI();
        }
        else if (_currentStep == LobbyStep.Step7_VsMatchFound)
        {
            RefreshStep7VsScreenUI();
        }
    }

    private void HandleMatchReadyToStart()
    {
        if (_modalRoot != null)
        {
            _modalRoot.SetActive(false);
        }
    }

    public void ShowStep(LobbyStep step)
    {
        _currentStep = step;
        if (_step2ModePanel != null) _step2ModePanel.SetActive(step == LobbyStep.Step2_ModeSelect);
        if (_step3HeroPanel != null) _step3HeroPanel.SetActive(step == LobbyStep.Step3_HeroSelect);
        if (_step4CreateRoomPanel != null) _step4CreateRoomPanel.SetActive(step == LobbyStep.Step4_CreateRoom);
        if (_step4BJoinPinPanel != null) _step4BJoinPinPanel.SetActive(step == LobbyStep.Step4B_JoinRoomPin);
        if (_step5WaitingRoomPanel != null) _step5WaitingRoomPanel.SetActive(step == LobbyStep.Step5_WaitingRoom);
        if (_step6SearchingPanel != null) _step6SearchingPanel.SetActive(step == LobbyStep.Step6_QuickMatchSearching);
        if (_step7VsPanel != null) _step7VsPanel.SetActive(step == LobbyStep.Step7_VsMatchFound);

        switch (step)
        {
            case LobbyStep.Step2_ModeSelect:
                RefreshStep2ModeSelectUI();
                break;
            case LobbyStep.Step3_HeroSelect:
                _previewHeroId = CharacterManager.EnsureInstance().GetSelectedCharacterId();
                RefreshStep3HeroSelectUI();
                break;
            case LobbyStep.Step4_CreateRoom:
                _pendingCustomPin = UnityEngine.Random.Range(1000, 9999).ToString();
                RefreshStep4CreateRoomUI();
                break;
            case LobbyStep.Step4B_JoinRoomPin:
                _enteredPin = "";
                if (_pinInputField != null) _pinInputField.text = "";
                break;
            case LobbyStep.Step5_WaitingRoom:
                RefreshStep5WaitingRoomUI();
                break;
            case LobbyStep.Step6_QuickMatchSearching:
                _searchElapsedSeconds = 0f;
                break;
            case LobbyStep.Step7_VsMatchFound:
                _vsCountdownTimer = 3.0f;
                _vsCountdownActive = true;
                SoundManager.EnsureInstance()?.PlayHeavenlyAscension();
                RefreshStep7VsScreenUI();
                break;
        }
    }

    // =========================================================================
    // XÂY DỰNG TOÀN BỘ CÁC MÀN HÌNH THEO LƯU ĐỒ 9 BƯỚC
    // =========================================================================
    private void BuildAllStepsUI(Transform root)
    {
        _modalRoot = root.gameObject;
        RectTransform rootRt = _modalRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image backdrop = _modalRoot.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.03f, 0.06f, 0.95f);

        BuildStep2ModeSelectPanel(root);
        BuildStep3HeroSelectPanel(root);
        BuildStep4CreateRoomPanel(root);
        BuildStep4BJoinRoomPinPanel(root);
        BuildStep5WaitingRoomPanel(root);
        BuildStep6SearchingPanel(root);
        BuildStep7VsMatchFoundPanel(root);
        BuildFriendsModal(root);
        BuildEditNicknameModal(root);

        GameFontManager.ApplyFontsGlobally(_modalRoot);
    }

    public static void OpenCoopLobbyFromMainMenu()
    {
        if (PlayFabClientService.IsOfflineMode)
        {
            ShowOfflineLockPrompt();
            return;
        }
        var inst = EnsureInstance();
        if (inst == null) return;
        OpenLobby();
        inst._selectedMode = SelectedPvPFlowMode.CreateCustomRoom;
        inst._openedDirectlyFromMainMenu = true;

        var net = PvPNetworkManager.EnsureInstance();
        if (!net.IsConnectingOrConnected)
        {
            string newPin = UnityEngine.Random.Range(1000, 9999).ToString();
            inst._pendingCustomPin = newPin;
            net.CreateRoomWithPin(newPin);
        }
        inst.ShowStep(LobbyStep.Step5_WaitingRoom);
    }

    public static void StartQuickMatchFromMainMenu()
    {
        OpenCoopLobbyFromMainMenu();
    }

    public static void OpenCreateRoomFromMainMenu()
    {
        OpenCoopLobbyFromMainMenu();
    }

    public static void OpenJoinRoomPinFromMainMenu()
    {
        OpenCoopLobbyFromMainMenu();
    }

    public static void StartBotPracticeFromMainMenu()
    {
        var inst = EnsureInstance();
        if (inst == null) return;
        OpenLobby();
        inst._selectedMode = SelectedPvPFlowMode.BotPractice;
        inst._openedDirectlyFromMainMenu = true;
        inst.ShowStep(LobbyStep.Step3_HeroSelect);
    }

    public static void ShowOfflineLockPrompt()
    {
        PlayFabAuthLoginUI.OpenWizardForStartGame(() =>
        {
            MainMenuManager.EnsureInstance()?.RefreshTopLeftPlayerProfileBadge();
            MainMenuManager.EnsureInstance()?.RefreshPvPModeLockState();
        });
    }

    // -------------------------------------------------------------------------
    // BƯỚC 2: TẠO TRẬN / CHỌN CHẾ ĐỘ (PVP ĐỐI KHÁNG)
    // -------------------------------------------------------------------------
    private void BuildStep2ModeSelectPanel(Transform root)
    {
        _step2ModePanel = CreateFrameWindow(root, "Step2_ModeSelectPanel",
            LocalizationManager.T("2. PVP ĐỐI KHÁNG - CHỌN CHẾ ĐỘ", "2. PVP ARENA - SELECT MODE"),
            CloseLobby);

        // Khung căn giữa: Danh sách 4 chế độ gọn gàng (bỏ hoàn toàn ảnh, VS và mô tả rườm rà)
        GameObject modeBox = CreateBox(_step2ModePanel.transform, "CenterModeList", new Vector2(0f, -10f), new Vector2(740f, 520f), new Color(0.08f, 0.10f, 0.16f, 0.96f));

        string[] modeNamesVN = new string[]
        {
            "ĐẤU THƯỜNG (QUICK MATCH)",
            "TẠO PHÒNG (CUSTOM ROOM)",
            "TÌM PHÒNG (NHẬP MÃ PIN)",
            "ĐẤU TẬP AI (BOT RIVAL)"
        };
        string[] modeNamesEN = new string[]
        {
            "QUICK MATCH (1V1 ONLINE)",
            "CREATE ROOM (CUSTOM PIN)",
            "FIND ROOM (ENTER PIN)",
            "AI RIVAL PRACTICE (OFFLINE)"
        };

        Color[] btnColors = new Color[]
        {
            new Color(0.88f, 0.50f, 0.14f, 1f),
            new Color(0.18f, 0.45f, 0.82f, 1f),
            new Color(0.24f, 0.32f, 0.55f, 1f),
            new Color(0.18f, 0.58f, 0.36f, 1f)
        };

        _modeTabBackgrounds.Clear();
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            float y = 160f - i * 110f;
            GameObject btnObj = CreateButton(modeBox.transform, "ModeTab_" + i,
                LocalizationManager.T(modeNamesVN[i], modeNamesEN[i]),
                new Vector2(0f, y), new Vector2(640f, 86f),
                btnColors[i], 24f, () =>
                {
                    SoundManager.EnsureInstance()?.PlayButtonClick();
                    _selectedMode = (SelectedPvPFlowMode)idx;
                    TriggerSelectedModeAction();
                });
            _modeTabBackgrounds.Add(btnObj.GetComponent<Image>());
        }
    }

    private void TriggerSelectedModeAction()
    {
        if (PlayFabClientService.IsOfflineMode && _selectedMode != SelectedPvPFlowMode.BotPractice)
        {
            ShowOfflineLockPrompt();
            return;
        }

        // Bắt buộc chuyển sang bước Chọn Chiến Binh trước khi vào bất kỳ chế độ nào
        _openedDirectlyFromMainMenu = false;
        ShowStep(LobbyStep.Step3_HeroSelect);
    }

    private void RefreshStep2ModeSelectUI()
    {
        for (int i = 0; i < _modeTabBackgrounds.Count; i++)
        {
            if (_modeTabBackgrounds[i] == null) continue;
            bool active = (i == (int)_selectedMode);
            _modeTabBackgrounds[i].color = active
                ? new Color(0.95f, 0.58f, 0.16f, 1f)
                : new Color(0.20f, 0.24f, 0.34f, 1f);
        }
    }

    private void OnBackFromStep3HeroSelect()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_selectedMode == SelectedPvPFlowMode.BotPractice)
        {
            CloseLobby();
        }
        else
        {
            ShowStep(LobbyStep.Step5_WaitingRoom);
        }
    }

    // -------------------------------------------------------------------------
    // BƯỚC 3: CHỌN NHÂN VẬT (HERO SELECTION)
    // -------------------------------------------------------------------------
    private void BuildStep3HeroSelectPanel(Transform root)
    {
        _step3HeroPanel = CreateFrameWindow(root, "Step3_HeroSelectPanel",
            LocalizationManager.T("3. CHỌN NHÂN VẬT XUẤT CHIẾN", "3. SELECT YOUR FIGHTER"),
            OnBackFromStep3HeroSelect);

        var headerTmp = _step3HeroPanel.transform.Find("HeaderBar")?.GetComponentInChildren<TextMeshProUGUI>();
        if (headerTmp != null) _step3HeaderTitleText = headerTmp;

        // Cột Trái: Lưới 7 Tướng (3 cột x 3 hàng)
        GameObject gridBox = CreateBox(_step3HeroPanel.transform, "HeroGridBox", new Vector2(-335f, -20f), new Vector2(590f, 620f), new Color(0.08f, 0.10f, 0.16f, 0.96f));
        CreateText(gridBox.transform,
            LocalizationManager.T("DANH SÁCH CHIẾN BINH (7 TƯỚNG)", "FIGHTER ROSTER (7 HEROES)"),
            22f, new Vector2(0f, 265f), new Vector2(540f, 38f), new Color(0.95f, 0.85f, 0.40f));

        _heroGridBorders.Clear();
        float cellW = 165f;
        float cellH = 155f;
        float gapX = 18f;
        float gapY = 18f;

        for (int i = 0; i < ALL_HERO_IDS.Length; i++)
        {
            string hId = ALL_HERO_IDS[i];
            int row = i / 3;
            int col = i % 3;
            float x = (col - 1) * (cellW + gapX);
            float y = 135f - row * (cellH + gapY);

            GameObject cardObj = new GameObject("HeroCard_" + hId, typeof(RectTransform), typeof(Image), typeof(Button));
            cardObj.transform.SetParent(gridBox.transform, false);
            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = new Vector2(x, y);
            cRt.sizeDelta = new Vector2(cellW, cellH);
            Image borderImg = cardObj.GetComponent<Image>();
            borderImg.color = new Color(0.16f, 0.20f, 0.30f, 1f);
            _heroGridBorders[hId] = borderImg;

            GameObject inner = CreateBox(cardObj.transform, "Inner", new Vector2(0f, 10f), new Vector2(cellW - 12f, cellH - 36f), new Color(0.06f, 0.07f, 0.12f, 1f));
            Image icon = CreateHeroPortraitImage(inner.transform, "Portrait", Vector2.zero, new Vector2(105f, 105f), false);
            icon.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(hId);

            var cData = CharacterManager.EnsureInstance().GetCharacter(hId);
            string shortName = cData != null ? (LocalizationManager.IsVietnamese ? cData.nameVN : cData.nameEN) : hId;
            CreateText(cardObj.transform, shortName, 15f, new Vector2(0f, -60f), new Vector2(cellW - 8f, 24f), Color.white);

            cardObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                _previewHeroId = hId;
                if (CharacterManager.EnsureInstance().IsCharacterUnlocked(hId))
                {
                    CharacterManager.EnsureInstance().SelectCharacter(hId, force: false);
                }
                RefreshStep3HeroSelectUI();
            });
        }

        // Cột Phải: Khung Chi Tiết Nhân Vật + Nút Hành Động Theo Chế Độ
        GameObject showcaseBox = CreateBox(_step3HeroPanel.transform, "HeroShowcaseBox", new Vector2(315f, -20f), new Vector2(640f, 620f), new Color(0.08f, 0.10f, 0.16f, 0.96f));

        // Khung Avatar Chân Dung Tướng (Đồng nhất Avatar khuôn mặt chuẩn, không dùng hình toàn thân)
        GameObject avatarBorder = CreateBox(showcaseBox.transform, "AvatarBorder", new Vector2(0f, 150f), new Vector2(190f, 190f), new Color(0.85f, 0.65f, 0.25f, 1f));
        GameObject avatarBg = CreateBox(avatarBorder.transform, "AvatarBg", Vector2.zero, new Vector2(178f, 178f), new Color(0.05f, 0.06f, 0.10f, 1f));
        _heroShowcaseImg = CreateHeroPortraitImage(avatarBg.transform, "ShowcaseAvatar", Vector2.zero, new Vector2(165f, 165f), false);

        // Tên Tướng
        _heroShowcaseNameText = CreateText(showcaseBox.transform, "", 28f, new Vector2(0f, 35f), new Vector2(580f, 38f), new Color(1f, 0.86f, 0.28f));

        // Vai Trò / Phong Cách Chiến Đấu
        _heroShowcaseRoleText = CreateText(showcaseBox.transform, "", 18f, new Vector2(0f, 3f), new Vector2(580f, 28f), new Color(0.55f, 0.90f, 1f));

        // Card Thông Số & Tuyệt Kỹ (Căn giữa, viền Soulslike, cách biệt hoàn toàn không bao giờ chạm nút)
        GameObject infoCard = CreateBox(showcaseBox.transform, "HeroInfoCard", new Vector2(0f, -80f), new Vector2(580f, 115f), new Color(0.05f, 0.07f, 0.12f, 0.95f));
        CreateBox(infoCard.transform, "CardBorder", Vector2.zero, new Vector2(580f, 115f), new Color(0.18f, 0.24f, 0.36f, 0.65f));

        _heroShowcaseStatsText = CreateText(infoCard.transform, "", 18f, new Vector2(0f, 20f), new Vector2(550f, 34f), new Color(0.55f, 1f, 0.72f));
        _heroShowcaseStatsText.alignment = TextAlignmentOptions.Center;

        _heroShowcaseSkillText = CreateText(infoCard.transform, "", 17f, new Vector2(0f, -20f), new Vector2(550f, 34f), new Color(1f, 0.88f, 0.45f));
        _heroShowcaseSkillText.alignment = TextAlignmentOptions.Center;

        // Nút Xác Nhận Hành Động Chính (Căn giữa hoàn hảo phía dưới, rộng rãi, rõ ràng)
        GameObject confirmBtnObj = CreateButton(showcaseBox.transform, "Btn_ConfirmHero",
            LocalizationManager.T("XÁC NHẬN >", "CONFIRM >"),
            new Vector2(0f, -225f), new Vector2(460f, 72f),
            new Color(0.18f, 0.65f, 0.34f, 1f), 24f, OnConfirmHeroStep3);
        _step3ConfirmBtnText = confirmBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        _step3ConfirmBtnImg = confirmBtnObj.GetComponent<Image>();
    }

    public void ShowLobbyToast(string message, Color textColor)
    {
        if (this == null || !gameObject.activeInHierarchy || !isActiveAndEnabled || _modalRoot == null || !_modalRoot.activeInHierarchy) return;
        if (_lobbyToastObj == null)
        {
            _lobbyToastObj = new GameObject("LobbyToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(Image));
            _lobbyToastObj.layer = 5;
            _lobbyToastObj.transform.SetParent(_modalRoot.transform, false);
            _lobbyToastObj.transform.SetAsLastSibling();

            RectTransform rt = _lobbyToastObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 300f);
            rt.sizeDelta = new Vector2(720f, 85f);

            Image bg = _lobbyToastObj.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.04f, 0.05f, 0.96f);
            bg.raycastTarget = false; // Tuyệt đối không chặn touch / raycast tới các nút bấm phía dưới

            CanvasGroup toastCg = _lobbyToastObj.GetComponent<CanvasGroup>();
            if (toastCg != null)
            {
                toastCg.blocksRaycasts = false;
                toastCg.interactable = false;
            }

            var outline = _lobbyToastObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.35f, 0.25f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(_lobbyToastObj.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            _lobbyToastText = txtObj.GetComponent<TextMeshProUGUI>();
            _lobbyToastText.font = GameFontManager.HeaderFont;
            _lobbyToastText.fontSize = 24f;
            _lobbyToastText.alignment = TextAlignmentOptions.Center;
            _lobbyToastText.enableWordWrapping = true;
            _lobbyToastText.raycastTarget = false;
        }

        if (_lobbyToastObj != null)
        {
            _lobbyToastObj.SetActive(true);
            _lobbyToastObj.transform.SetAsLastSibling();
            if (_lobbyToastText != null)
            {
                _lobbyToastText.text = message;
                _lobbyToastText.color = textColor;
            }
            if (_lobbyToastRoutine != null)
            {
                StopCoroutine(_lobbyToastRoutine);
                _lobbyToastRoutine = null;
            }
            if (isActiveAndEnabled && gameObject.activeInHierarchy)
            {
                _lobbyToastRoutine = StartCoroutine(AnimateLobbyToastRoutine());
            }
        }
    }

    private System.Collections.IEnumerator AnimateLobbyToastRoutine()
    {
        if (_lobbyToastObj == null) yield break;
        CanvasGroup cg = _lobbyToastObj.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        float timer = 2.4f;
        while (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            yield return null;
        }

        float fade = 0.3f;
        while (fade > 0f)
        {
            fade -= Time.unscaledDeltaTime;
            if (cg != null) cg.alpha = fade / 0.3f;
            yield return null;
        }

        if (_lobbyToastObj != null) _lobbyToastObj.SetActive(false);
        _lobbyToastRoutine = null;
    }

    private void RefreshStep3HeroSelectUI()
    {
        if (_step3HeaderTitleText != null)
        {
            switch (_selectedMode)
            {
                case SelectedPvPFlowMode.QuickMatch:
                    _step3HeaderTitleText.text = LocalizationManager.T("CHỌN CHIẾN BINH - ĐẤU THƯỜNG (QUICK MATCH)", "SELECT FIGHTER - QUICK MATCH");
                    break;
                case SelectedPvPFlowMode.CreateCustomRoom:
                    _step3HeaderTitleText.text = LocalizationManager.T("CHỌN CHIẾN BINH - TẠO PHÒNG (CUSTOM ROOM)", "SELECT FIGHTER - CREATE ROOM");
                    break;
                case SelectedPvPFlowMode.JoinRoomByPin:
                    _step3HeaderTitleText.text = LocalizationManager.T("CHỌN CHIẾN BINH - TÌM PHÒNG (NHẬP MÃ PIN)", "SELECT FIGHTER - ENTER PIN");
                    break;
                case SelectedPvPFlowMode.BotPractice:
                    _step3HeaderTitleText.text = LocalizationManager.T("CHỌN CHIẾN BINH - ĐẤU TẬP AI (BOT RIVAL)", "SELECT FIGHTER - AI PRACTICE");
                    break;
            }
        }

        var charMgr = CharacterManager.EnsureInstance();
        bool isUnlocked = charMgr.IsCharacterUnlocked(_previewHeroId);

        if (_step3ConfirmBtnText != null)
        {
            if (!isUnlocked)
            {
                _step3ConfirmBtnText.text = LocalizationManager.T("CHƯA MỞ KHÓA (CẦN MUA)", "LOCKED (PURCHASE FIRST)");
                if (_step3ConfirmBtnImg != null) _step3ConfirmBtnImg.color = new Color(0.65f, 0.22f, 0.22f, 1f);
            }
            else
            {
                if (_step3ConfirmBtnImg != null) _step3ConfirmBtnImg.color = new Color(0.18f, 0.65f, 0.34f, 1f);
                switch (_selectedMode)
                {
                    case SelectedPvPFlowMode.QuickMatch:
                        _step3ConfirmBtnText.text = LocalizationManager.T("TÌM TRẬN NGAY >", "FIND MATCH >");
                        break;
                    case SelectedPvPFlowMode.CreateCustomRoom:
                        _step3ConfirmBtnText.text = LocalizationManager.T("TIẾP TỤC TẠO PHÒNG >", "CREATE ROOM >");
                        break;
                    case SelectedPvPFlowMode.JoinRoomByPin:
                        _step3ConfirmBtnText.text = LocalizationManager.T("TIẾP TỤC NHẬP PIN >", "ENTER PIN >");
                        break;
                    case SelectedPvPFlowMode.BotPractice:
                        _step3ConfirmBtnText.text = LocalizationManager.T("VÀO ĐẤU TẬP >", "START BATTLE >");
                        break;
                }
            }
        }

        foreach (var kv in _heroGridBorders)
        {
            if (kv.Value == null) continue;
            bool isSelected = (kv.Key == _previewHeroId);
            bool unlockedCard = charMgr.IsCharacterUnlocked(kv.Key);
            if (isSelected)
                kv.Value.color = new Color(1f, 0.82f, 0.22f, 1f);
            else if (unlockedCard)
                kv.Value.color = new Color(0.18f, 0.24f, 0.36f, 1f);
            else
                kv.Value.color = new Color(0.35f, 0.14f, 0.16f, 0.85f);
        }

        var data = charMgr.GetCharacter(_previewHeroId);
        if (_heroShowcaseImg != null)
            _heroShowcaseImg.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(_previewHeroId);

        string hName = data != null ? (LocalizationManager.IsVietnamese ? data.nameVN : data.nameEN) : _previewHeroId;
        string hRole = data != null ? (LocalizationManager.IsVietnamese ? data.roleVN : data.roleEN) : "";
        string sName = data != null ? (LocalizationManager.IsVietnamese ? data.skillNameVN : data.skillNameEN) : "";
        int pvpHp = PvPNetworkManager.GetNormalizedPvPMaxHealth(_previewHeroId);
        int atk = data != null ? data.baseAttack : 25;
        int def = data != null ? data.baseDefense : 10;
        float spd = data != null ? data.baseSpeed : 4.8f;

        if (_heroShowcaseNameText != null)
        {
            _heroShowcaseNameText.text = isUnlocked ? hName.ToUpper() : hName.ToUpper() + LocalizationManager.T(" (CHƯA MỞ KHÓA)", " (LOCKED)");
        }
        if (_heroShowcaseRoleText != null)
        {
            _heroShowcaseRoleText.text = hRole;
        }
        if (_heroShowcaseStatsText != null)
        {
            _heroShowcaseStatsText.text = LocalizationManager.T(
                $"HP LÔI ĐÀI: {pvpHp}   •   ATK: {atk}   •   DEF: {def}   •   TỐC ĐỘ: {spd:F1}",
                $"ARENA HP: {pvpHp}   •   ATK: {atk}   •   DEF: {def}   •   SPEED: {spd:F1}");
        }
        if (_heroShowcaseSkillText != null)
        {
            _heroShowcaseSkillText.text = LocalizationManager.T(
                $"TUYỆT KỸ: {sName.ToUpper()}",
                $"ULTIMATE: {sName.ToUpper()}");
        }
    }

    private void OnConfirmHeroStep3()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        var charMgr = CharacterManager.EnsureInstance();
        if (!charMgr.IsCharacterUnlocked(_previewHeroId))
        {
            var cData = charMgr.GetCharacter(_previewHeroId);
            string hName = cData != null ? (LocalizationManager.IsVietnamese ? cData.nameVN : cData.nameEN) : _previewHeroId;
            SoundManager.EnsureInstance()?.PlayHitImpact(false, false);
            ShowLobbyToast(LocalizationManager.T(
                $"VUI LÒNG MỞ KHÓA TƯỚNG {hName.ToUpper()} ĐỂ SỬ DỤNG!",
                $"PLEASE UNLOCK HERO {hName.ToUpper()} TO USE IN BATTLE!"),
                new Color(1f, 0.45f, 0.45f, 1f));
            RefreshStep3HeroSelectUI();
            return;
        }

        charMgr.SelectCharacter(_previewHeroId, force: false);
        PlayerPrefs.SetString("SelectedHero", _previewHeroId);
        PlayerPrefs.Save();
        AccountDatabase.SaveActiveSessionToDatabase();

        if (_selectedMode == SelectedPvPFlowMode.BotPractice)
        {
            PvPNetworkManager.EnsureInstance().PrepareBotPracticeMatch();
            if (_modalRoot != null) _modalRoot.SetActive(false);
            PvPNetworkManager.EnsureInstance().EnterPvPArenaScene();
        }
        else
        {
            ShowStep(LobbyStep.Step5_WaitingRoom);
            PvPNetworkManager.Instance?.SendHandshakePacket();
        }
    }

    // -------------------------------------------------------------------------
    // BƯỚC 4: TẠO PHÒNG (CUSTOM ROOM SETTINGS)
    // -------------------------------------------------------------------------
    private void BuildStep4CreateRoomPanel(Transform root)
    {
        _step4CreateRoomPanel = CreateFrameWindow(root, "Step4_CreateRoomPanel",
            LocalizationManager.T("4. TẠO PHÒNG (CUSTOM ROOM)", "4. CREATE CUSTOM ROOM"),
            () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                ShowStep(LobbyStep.Step3_HeroSelect);
            });

        // Cột Trái: Các dòng cấu hình phòng (Chế độ, Thể thức, Số người, Map, Mã Phòng)
        GameObject formBox = CreateBox(_step4CreateRoomPanel.transform, "RoomSettingsForm", new Vector2(-280f, 20f), new Vector2(680f, 500f), new Color(0.08f, 0.10f, 0.16f, 0.96f));

        // 1. Chế độ đấu
        CreateText(formBox.transform, LocalizationManager.T("CHẾ ĐỘ ĐẤU:", "GAME MODE:"), 20f, new Vector2(-190f, 170f), new Vector2(240f, 40f), Color.white).alignment = TextAlignmentOptions.Left;
        _customRuleValText = CreateText(formBox.transform, "", 18.5f, new Vector2(110f, 170f), new Vector2(250f, 40f), new Color(1f, 0.88f, 0.35f));
        CreateButton(formBox.transform, "Btn_PrevRule", "<", new Vector2(-40f, 170f), new Vector2(46f, 42f), new Color(0.18f, 0.22f, 0.32f, 1f), 20f, () =>
        {
            _customRuleIndex = (_customRuleIndex + 1) % 2;
            RefreshStep4CreateRoomUI();
        });
        CreateButton(formBox.transform, "Btn_NextRule", ">", new Vector2(260f, 170f), new Vector2(46f, 42f), new Color(0.18f, 0.22f, 0.32f, 1f), 20f, () =>
        {
            _customRuleIndex = (_customRuleIndex + 1) % 2;
            RefreshStep4CreateRoomUI();
        });

        // 2. Thể thức đấu (Round Format: Bo1, Bo3, Bo5)
        CreateText(formBox.transform, LocalizationManager.T("THỂ THỨC ĐẤU:", "ROUND FORMAT:"), 20f, new Vector2(-190f, 95f), new Vector2(240f, 40f), Color.white).alignment = TextAlignmentOptions.Left;
        _customRoundFormatValText = CreateText(formBox.transform, "", 18f, new Vector2(110f, 95f), new Vector2(250f, 40f), new Color(0.45f, 0.95f, 1f));
        CreateButton(formBox.transform, "Btn_PrevFormat", "<", new Vector2(-40f, 95f), new Vector2(46f, 42f), new Color(0.18f, 0.22f, 0.32f, 1f), 20f, () =>
        {
            _customRoundFormatIndex = (_customRoundFormatIndex + 2) % 3;
            RefreshStep4CreateRoomUI();
        });
        CreateButton(formBox.transform, "Btn_NextFormat", ">", new Vector2(260f, 95f), new Vector2(46f, 42f), new Color(0.18f, 0.22f, 0.32f, 1f), 20f, () =>
        {
            _customRoundFormatIndex = (_customRoundFormatIndex + 1) % 3;
            RefreshStep4CreateRoomUI();
        });

        // 3. Số người
        CreateText(formBox.transform, LocalizationManager.T("SỐ NGƯỜI:", "PLAYERS:"), 20f, new Vector2(-190f, 20f), new Vector2(240f, 40f), Color.white).alignment = TextAlignmentOptions.Left;
        CreateText(formBox.transform, LocalizationManager.T("2 NGƯỜI (1V1)", "2 PLAYERS (1V1)"), 20f, new Vector2(110f, 20f), new Vector2(260f, 40f), new Color(0.55f, 0.95f, 0.68f));

        // 4. Map
        CreateText(formBox.transform, LocalizationManager.T("BẢN ĐỒ (MAP):", "ARENA MAP:"), 20f, new Vector2(-190f, -55f), new Vector2(240f, 40f), Color.white).alignment = TextAlignmentOptions.Left;
        _customMapValText = CreateText(formBox.transform, "", 18.5f, new Vector2(110f, -55f), new Vector2(250f, 40f), new Color(1f, 0.88f, 0.35f));
        CreateButton(formBox.transform, "Btn_PrevMap", "<", new Vector2(-40f, -55f), new Vector2(46f, 42f), new Color(0.18f, 0.22f, 0.32f, 1f), 20f, () =>
        {
            _customMapIndex = (_customMapIndex + 1) % 2;
            RefreshStep4CreateRoomUI();
        });
        CreateButton(formBox.transform, "Btn_NextMap", ">", new Vector2(260f, -55f), new Vector2(46f, 42f), new Color(0.18f, 0.22f, 0.32f, 1f), 20f, () =>
        {
            _customMapIndex = (_customMapIndex + 1) % 2;
            RefreshStep4CreateRoomUI();
        });

        // 5. Mã Phòng (PIN)
        CreateText(formBox.transform, LocalizationManager.T("MÃ PHÒNG (PIN):", "ROOM PIN:"), 20f, new Vector2(-190f, -130f), new Vector2(240f, 40f), Color.white).alignment = TextAlignmentOptions.Left;
        _customPinValText = CreateText(formBox.transform, "#4829", 28f, new Vector2(60f, -130f), new Vector2(190f, 44f), new Color(0.45f, 0.95f, 1f));
        CreateButton(formBox.transform, "Btn_RerollPin", LocalizationManager.T("ĐỔI MÃ", "NEW PIN"), new Vector2(225f, -130f), new Vector2(115f, 44f), new Color(0.22f, 0.38f, 0.62f, 1f), 17f, () =>
        {
            SoundManager.EnsureInstance()?.PlayButtonClick();
            _pendingCustomPin = UnityEngine.Random.Range(1000, 9999).ToString();
            RefreshStep4CreateRoomUI();
        });

        // Cột Phải: Preview Bản Đồ Đấu Trường Cổ Đại
        GameObject mapPreviewBox = CreateBox(_step4CreateRoomPanel.transform, "MapPreviewBox", new Vector2(360f, 20f), new Vector2(520f, 500f), new Color(0.08f, 0.10f, 0.16f, 0.96f));
        CreateText(mapPreviewBox.transform, LocalizationManager.T("ĐẤU TRƯỜNG BÁT GIÁC CỔ ĐẠI", "ANCIENT OCTAGON COLOSSEUM"), 24f, new Vector2(0f, 195f), new Vector2(480f, 42f), new Color(1f, 0.86f, 0.32f));

        GameObject arenaThumb = CreateBox(mapPreviewBox.transform, "ArenaVisualThumb", new Vector2(0f, 25f), new Vector2(440f, 240f), new Color(0.14f, 0.12f, 0.18f, 1f));
        CreateBox(arenaThumb.transform, "OctagonRing", Vector2.zero, new Vector2(360f, 180f), new Color(0.22f, 0.19f, 0.28f, 1f));
        CreateText(arenaThumb.transform, LocalizationManager.T("VÕ ĐÀI SINH TỬ 1V1\n(KHÔNG QUÁI VẬT - CHUẨN HÓA HP)", "1V1 DUEL COLOSSEUM\n(PURE PVP - NORMALIZED HP)"), 19f, Vector2.zero, new Vector2(340f, 90f), new Color(1f, 0.92f, 0.65f));

        _step4PreviewInfoText = CreateText(mapPreviewBox.transform, "",
            17f, new Vector2(0f, -155f), new Vector2(480f, 95f), new Color(0.75f, 0.88f, 0.98f));

        // Nút "TẠO PHÒNG" lớn ở giữa dưới đáy
        CreateButton(_step4CreateRoomPanel.transform, "Btn_ConfirmCreateRoom",
            LocalizationManager.T("TẠO PHÒNG NGAY >", "CREATE ROOM >"),
            new Vector2(0f, -295f), new Vector2(420f, 82f),
            new Color(0.18f, 0.62f, 0.34f, 1f), 28f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                PvPNetworkManager.SelectedRoundFormat = (PvPNetworkManager.PvPRoundFormat)_customRoundFormatIndex;
                PlayerPrefs.SetInt("PvP_Round_Format_Index", _customRoundFormatIndex);
                PlayerPrefs.Save();
                ShowStep(LobbyStep.Step5_WaitingRoom);
                PvPNetworkManager.EnsureInstance().CreateRoomWithPin(_pendingCustomPin);
            });
    }

    private void RefreshStep4CreateRoomUI()
    {
        if (_customRuleValText != null)
        {
            _customRuleValText.text = (_customRuleIndex == 0)
                ? LocalizationManager.T("ĐẤU THƯỜNG (CHUẨN HÓA)", "STANDARD DUEL")
                : LocalizationManager.T("SINH TỬ (QUYẾT ĐẤU)", "DEATHMATCH 1V1");
        }
        if (_customRoundFormatValText != null)
        {
            switch (_customRoundFormatIndex)
            {
                case 0:
                    _customRoundFormatValText.text = LocalizationManager.T("1 HIỆP SINH TỬ (BO1)", "1 ROUND KO (BO1)");
                    break;
                case 2:
                    _customRoundFormatValText.text = LocalizationManager.T("CHẠM 3 THẮNG (BO5)", "FIRST TO 3 (BO5)");
                    break;
                case 1:
                default:
                    _customRoundFormatValText.text = LocalizationManager.T("CHẠM 2 THẮNG (BO3)", "FIRST TO 2 (BO3)");
                    break;
            }
        }
        if (_customMapValText != null)
        {
            _customMapValText.text = (_customMapIndex == 0)
                ? LocalizationManager.T("ĐẤU TRƯỜNG CỔ ĐẠI", "ANCIENT COLOSSEUM")
                : LocalizationManager.T("NGẪU NHIÊN (RANDOM)", "RANDOM ARENA");
        }
        if (_customPinValText != null)
        {
            _customPinValText.text = "#" + _pendingCustomPin;
        }

        if (_step4PreviewInfoText != null)
        {
            string fmtDesc = (_customRoundFormatIndex == 0)
                ? LocalizationManager.T("1 Hiệp Sinh Tử (Bo1)", "Single Round KO (Bo1)")
                : ((_customRoundFormatIndex == 2)
                    ? LocalizationManager.T("Chạm 3 Thắng (Bo5 - Tối đa 5 hiệp)", "Best of 5 (Bo5 - Up to 5 rounds)")
                    : LocalizationManager.T("Chạm 2 Thắng (Bo3 - Tối đa 3 hiệp)", "Best of 3 (Bo3 - Up to 3 rounds)"));

            _step4PreviewInfoText.text = LocalizationManager.T(
                $"• Máy chủ: Photon Cloud (Asia)\n• Thể thức: {fmtDesc}\n• Thời gian: 90 Giây / Hiệp  •  Tự động đồng bộ PC & Android",
                $"• Server: Photon Cloud (Asia)\n• Format: {fmtDesc}\n• Time: 90s / Round  •  Cross-play PC & Android");
        }
    }

    // -------------------------------------------------------------------------
    // BƯỚC 4B: TÌM PHÒNG (NHẬP MÃ PIN 4 SỐ BẰNG BÀN PHÍM CẢM ỨNG)
    // -------------------------------------------------------------------------
    private void BuildStep4BJoinRoomPinPanel(Transform root)
    {
        _step4BJoinPinPanel = CreateFrameWindow(root, "Step4B_JoinRoomPinPanel",
            LocalizationManager.T("4. TÌM PHÒNG ĐẤU (NHẬP MÃ PIN 4 SỐ)", "4. FIND ROOM (ENTER 4-DIGIT PIN)"),
            () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                ShowStep(LobbyStep.Step3_HeroSelect);
            });

        GameObject centerBox = CreateBox(_step4BJoinPinPanel.transform, "KeypadContainer", new Vector2(0f, -15f), new Vector2(760f, 610f), new Color(0.08f, 0.10f, 0.16f, 0.96f));

        CreateText(centerBox.transform,
            LocalizationManager.T("NHẬP MÃ PHÒNG #PIN CỦA BẠN BÈ ĐỂ VÀO PHÒNG CHỜ", "ENTER YOUR FRIEND'S 4-DIGIT ROOM PIN"),
            23f, new Vector2(0f, 245f), new Vector2(700f, 40f), new Color(1f, 0.88f, 0.35f));

        BuildPinInputField(centerBox.transform, new Vector2(0f, 165f));
        BuildNumericKeypad(centerBox.transform, new Vector2(0f, -45f));

        _joinPinStatusText = CreateText(centerBox.transform,
            LocalizationManager.T("MÁY CHỦ PHOTON ASIA - SẴN SÀNG KẾT NỐI", "PHOTON ASIA SERVER - READY"),
            19f, new Vector2(0f, -255f), new Vector2(700f, 36f), new Color(0.45f, 0.92f, 1f));
    }

    // -------------------------------------------------------------------------
    // BƯỚC 5: PHÒNG CHỜ CO-OP 1-4 NGƯỜI (CHUẨN CHUYÊN NGHIỆP MOBILE ARPG)
    // -------------------------------------------------------------------------
    private void BuildStep5WaitingRoomPanel(Transform root)
    {
        _step5WaitingRoomPanel = new GameObject("Step5_WaitingRoomPanel", typeof(RectTransform), typeof(Image));
        _step5WaitingRoomPanel.transform.SetParent(root, false);
        RectTransform panelRt = _step5WaitingRoomPanel.GetComponent<RectTransform>();
        panelRt.sizeDelta = new Vector2(1920f, 1080f);
        _step5WaitingRoomPanel.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.15f, 0.94f);

        // 1. TOP BAR HEADER (y = 435f) - Căn chỉnh layout chuẩn xác, chống tràn & chồng đè
        // Nút Quay Lại "<"
        CreateRoundedButton(_step5WaitingRoomPanel.transform, "Btn_Back", "<",
            new Vector2(-590f, 435f), new Vector2(64f, 54f),
            new Color(0.38f, 0.28f, 0.72f, 1f), 32f, LeaveWaitingRoomToStep2);

        // Badge tiêu đề "PHÒNG CO-OP"
        GameObject headerPill = CreateRoundedBox(_step5WaitingRoomPanel.transform, "HeaderPill",
            new Vector2(-435f, 435f), new Vector2(210f, 54f),
            new Color(0.24f, 0.18f, 0.46f, 1f));
        CreateText(headerPill.transform, LocalizationManager.T("PHÒNG CO-OP", "CO-OP ROOM"),
            19f, Vector2.zero, new Vector2(200f, 44f), Color.white);

        // Chip Mã phòng + Nút Copy PIN
        GameObject pinChip = CreateRoundedBox(_step5WaitingRoomPanel.transform, "PinChip",
            new Vector2(-215f, 435f), new Vector2(190f, 52f),
            new Color(0.12f, 0.10f, 0.22f, 0.95f));
        CreateBorderBox(pinChip.transform, "PinChipBorder", Vector2.zero, new Vector2(190f, 52f), new Color(0.32f, 0.28f, 0.50f, 1f));

        _roomHeaderTitleText = CreateText(pinChip.transform, "Mã: #0000", 17f, new Vector2(-25f, 0f), new Vector2(115f, 40f), Color.white);
        _roomHeaderTitleText.alignment = TextAlignmentOptions.Left;

        CreateRoundedButton(pinChip.transform, "Btn_CopyChip", "COPY",
            new Vector2(60f, 0f), new Vector2(54f, 34f),
            new Color(0.26f, 0.22f, 0.42f, 1f), 12.5f, OnCopyRoomPinClicked);

        Button chipBtn = pinChip.AddComponent<Button>();
        chipBtn.onClick.AddListener(OnCopyRoomPinClicked);

        // Ô NHẬP MÃ PHÒNG (PIN) TRỰC TIẾP TRÊN HEADER (x = 50f, width = 280f)
        BuildHeaderPinInputField(_step5WaitingRoomPanel.transform);

        // NÚT BẠN BÈ & NÚT CHAT TỔ ĐỘI (Dàn đều sang phải, không chồng lấn)
        CreateRoundedButton(_step5WaitingRoomPanel.transform, "Btn_LobbyFriends",
            LocalizationManager.T("BẠN BÈ", "FRIENDS"),
            new Vector2(280f, 435f), new Vector2(130f, 52f),
            new Color(0.20f, 0.52f, 0.45f, 1f), 17f, OpenFriendsModal);

        GameObject chatBtnObj = CreateRoundedButton(_step5WaitingRoomPanel.transform, "Btn_LobbyChat", "CHAT",
            new Vector2(435f, 435f), new Vector2(130f, 52f),
            new Color(0.25f, 0.40f, 0.85f, 1f), 18f, OpenLobbyChatModal);
        _lobbyChatBtnBadgeText = chatBtnObj.GetComponentInChildren<TextMeshProUGUI>();

        // 2. KHUNG CHÍNH (MAIN CARD CONTAINER) Ở TRUNG TÂM
        GameObject mainCard = CreateRoundedBox(_step5WaitingRoomPanel.transform, "MainCardContainer",
            new Vector2(0f, -5f), new Vector2(1380f, 690f),
            new Color(0.16f, 0.15f, 0.28f, 0.98f));
        CreateBorderBox(mainCard.transform, "MainCardBorder", Vector2.zero, new Vector2(1380f, 690f), new Color(0.28f, 0.25f, 0.45f, 1f));

        // Sub-Header trong khung
        var leftSubText = CreateText(mainCard.transform,
            LocalizationManager.T("Sảnh chờ tổ đội (Co-op 1 - 4 người)", "Squad Lobby (Co-op 1 - 4 players)"),
            20f, new Vector2(-360f, 305f), new Vector2(550f, 40f), new Color(0.85f, 0.88f, 0.98f));
        leftSubText.alignment = TextAlignmentOptions.Left;

        _roomPlayerCountText = CreateText(mainCard.transform,
            LocalizationManager.T("Đồng đội: 1/4", "Teammates: 1/4"),
            20f, new Vector2(510f, 305f), new Vector2(240f, 40f), new Color(1f, 0.86f, 0.32f));
        _roomPlayerCountText.alignment = TextAlignmentOptions.Right;

        CreateBox(mainCard.transform, "Divider", new Vector2(0f, 280f), new Vector2(1320f, 2f), new Color(0.24f, 0.22f, 0.38f, 1f));

        // 3. 4 THẺ SLOT NGƯỜI CHƠI (HÀNG NGANG)
        float[] slotXs = new float[] { -465f, -155f, 155f, 465f };

        for (int i = 0; i < 4; i++)
        {
            float posX = slotXs[i];
            bool isHostSlot = (i == 0);

            Color cardBgColor = isHostSlot
                ? new Color(0.13f, 0.13f, 0.24f, 1f)
                : new Color(0.11f, 0.10f, 0.19f, 0.92f);

            GameObject card = CreateRoundedBox(mainCard.transform, $"CoopSlot_{i}",
                new Vector2(posX, 80f), new Vector2(295f, 360f), cardBgColor);

            Color borderColor = isHostSlot
                ? new Color(0.22f, 0.65f, 1f, 1f)
                : new Color(0.20f, 0.20f, 0.32f, 1f);
            CreateBorderBox(card.transform, "CardBorder", Vector2.zero, new Vector2(295f, 360f), borderColor);

            // Badge "CHỦ PHÒNG" (Slot 0)
            GameObject crownObj = CreateRoundedBox(card.transform, "CrownBadge",
                new Vector2(0f, 152f), new Vector2(160f, 34f),
                new Color(0.98f, 0.75f, 0.18f, 1f));
            CreateText(crownObj.transform, LocalizationManager.T("CHỦ PHÒNG", "HOST"), 16.5f,
                Vector2.zero, new Vector2(150f, 30f), new Color(0.22f, 0.14f, 0.02f));
            crownObj.SetActive(isHostSlot);

            // Tiêu đề vị trí (Vị trí 2, 3, 4)
            string roleTitle = isHostSlot ? "" : LocalizationManager.T($"Vị trí {i + 1}", $"Slot {i + 1}");
            var roleTitleTmp = CreateText(card.transform, roleTitle, 17f,
                new Vector2(0f, 152f), new Vector2(240f, 30f), new Color(0.65f, 0.70f, 0.82f));
            roleTitleTmp.gameObject.SetActive(!isHostSlot);

            // Khung Avatar vuông bo góc - Cho phép click đổi tướng
            GameObject avBox = CreateRoundedBox(card.transform, "AvatarBox",
                new Vector2(0f, 48f), new Vector2(124f, 124f),
                new Color(0.08f, 0.09f, 0.16f, 1f));
            Color avBorderColor = isHostSlot ? new Color(0.35f, 0.85f, 1f, 1f) : new Color(0.22f, 0.22f, 0.36f, 1f);
            CreateBorderBox(avBox.transform, "AvBorder", Vector2.zero, new Vector2(124f, 124f), avBorderColor);

            Image avImg = CreateHeroPortraitImage(avBox.transform, "AvatarImg", Vector2.zero, new Vector2(112f, 112f), false);

            GameObject onlineDot = CreateCircle(avBox.transform, "OnlineDot",
                new Vector2(48f, -48f), new Vector2(18f, 18f),
                new Color(0.25f, 0.95f, 0.40f, 1f));
            onlineDot.SetActive(isHostSlot);

            // Silhouette bóng người xám (khi slot trống)
            GameObject silBox = new GameObject("Silhouette", typeof(RectTransform));
            silBox.transform.SetParent(avBox.transform, false);
            CreateCircle(silBox.transform, "SilHead", new Vector2(0f, 15f), new Vector2(40f, 40f), new Color(0.22f, 0.24f, 0.34f, 1f));
            CreateRoundedBox(silBox.transform, "SilShoulders", new Vector2(0f, -24f), new Vector2(72f, 38f), new Color(0.22f, 0.24f, 0.34f, 1f));
            silBox.SetActive(!isHostSlot);

            // Nút (+) Mời bạn bè ở giữa thẻ trống
            GameObject plusBtn = CreateRoundedButton(card.transform, "PlusBtn", "+",
                new Vector2(0f, -36f), new Vector2(46f, 46f),
                new Color(0.22f, 0.24f, 0.38f, 1f), 26f, OpenInviteGuideDialog);
            plusBtn.SetActive(!isHostSlot);

            // Dòng chữ "Mời bạn bè hoặc nhập PIN"
            GameObject invitePrompt = new GameObject("InvitePrompt", typeof(RectTransform));
            invitePrompt.transform.SetParent(card.transform, false);
            CreateText(invitePrompt.transform,
                LocalizationManager.T("Mời bạn bè\nhoặc nhập PIN", "Invite friends\nor enter PIN"),
                14.5f, new Vector2(0f, -92f), new Vector2(240f, 44f),
                new Color(0.55f, 0.58f, 0.72f));
            invitePrompt.SetActive(!isHostSlot);

            // NÚT CHỌN TƯỚNG (Mở Popup chọn tướng riêng biệt, to rõ, dễ bấm)
            GameObject changeHeroBtn = CreateRoundedButton(card.transform, "Btn_ChangeHero",
                LocalizationManager.T("CHỌN TƯỚNG", "PICK HERO"),
                new Vector2(0f, -24f), new Vector2(150f, 38f),
                new Color(0.95f, 0.55f, 0.10f, 1f), 15f, OpenHeroSelectionModal);
            CreateBorderBox(changeHeroBtn.transform, "ChangeHeroBord", Vector2.zero, new Vector2(150f, 38f), new Color(1f, 0.85f, 0.40f, 1f));
            changeHeroBtn.SetActive(isHostSlot);

            // Tên người chơi thật
            string initName = isHostSlot ? GetLocalPlayerDisplayName() : "---";
            var nameTxt = CreateText(card.transform, initName,
                20f, new Vector2(0f, -60f), new Vector2(260f, 30f), Color.white);

            // Cấp độ thật từ hệ thống Hero Mastery
            string curHero = CharacterManager.EnsureInstance().GetSelectedCharacterId();
            int curHeroLv = AccountDatabase.GetHeroLevel(CharacterManager.NormalizeHeroId(curHero));
            if (curHeroLv <= 0) curHeroLv = 1;
            string initLvlStr = isHostSlot ? $"Lv. {curHeroLv}" : "---";
            var levelTxt = CreateText(card.transform, initLvlStr,
                15.5f, new Vector2(0f, -86f), new Vector2(260f, 24f), new Color(1f, 0.85f, 0.35f));

            // Badge / Pill Trạng Thái
            GameObject readyPill = CreateRoundedBox(card.transform, "ReadyPill",
                new Vector2(0f, -130f), new Vector2(190f, 38f),
                new Color(0.12f, 0.35f, 0.22f, 1f));
            CreateBorderBox(readyPill.transform, "PillBorder", Vector2.zero, new Vector2(190f, 38f), new Color(0.35f, 0.95f, 0.55f, 1f));
            Image pillBgImg = readyPill.GetComponent<Image>();
            var readyTxt = CreateText(readyPill.transform, LocalizationManager.T("CHỦ PHÒNG", "HOST"),
                16f, Vector2.zero, new Vector2(180f, 32f), new Color(0.35f, 1f, 0.55f));
            readyPill.SetActive(isHostSlot);

            // Nút KÍCH (Kick) - Dành riêng cho Chủ phòng
            int slotIdx = i;
            GameObject kickBtn = CreateRoundedButton(card.transform, $"Btn_KickSlot_{i}",
                LocalizationManager.T("KÍCH", "KICK"),
                new Vector2(85f, 155f), new Vector2(65f, 32f),
                new Color(0.75f, 0.20f, 0.22f, 1f), 13f, () => OnKickSlotClicked(slotIdx));
            kickBtn.SetActive(false);

            // Nút KẾT BẠN (+) - Gửi lời mời kết bạn tức thì
            GameObject addFrBtn = CreateRoundedButton(card.transform, $"Btn_AddFrSlot_{i}",
                "+ BẠN",
                new Vector2(-85f, 155f), new Vector2(72f, 32f),
                new Color(0.20f, 0.45f, 0.85f, 1f), 12.5f, () => OnAddFriendSlotClicked(slotIdx));
            addFrBtn.SetActive(false);

            _coopCardSlots[i] = new CoopCardUI
            {
                cardObj = card,
                avatarImg = avImg,
                silhouetteObj = silBox,
                plusBtnObj = plusBtn,
                onlineDotObj = onlineDot,
                crownBadgeObj = crownObj,
                roleTitleText = roleTitleTmp,
                nameText = nameTxt,
                heroText = levelTxt,
                readyText = readyTxt,
                readyPillObj = readyPill,
                readyPillBg = pillBgImg,
                invitePromptObj = invitePrompt,
                changeHeroBtnObj = changeHeroBtn,
                kickBtnObj = kickBtn,
                addFriendBtnObj = addFrBtn
            };
        }

        // 4. BẢNG THÔNG TIN HỆ THỐNG / HƯỚNG DẪN TRẬN ĐẤU
        GameObject infoBar = CreateRoundedBox(mainCard.transform, "MatchInfoBanner",
            new Vector2(0f, -175f), new Vector2(1250f, 80f),
            new Color(0.11f, 0.10f, 0.20f, 0.95f));
        CreateBorderBox(infoBar.transform, "InfoBorder", Vector2.zero, new Vector2(1250f, 80f), new Color(0.24f, 0.22f, 0.38f, 1f));

        CreateText(infoBar.transform,
            LocalizationManager.T("CHẾ ĐỘ: HẦM NGỤC CO-OP LIÊN THỦ (1 - 4 NGƯỜI)", "MODE: ALLIED CO-OP DUNGEON (1 - 4 PLAYERS)"),
            18f, new Vector2(-280f, 15f), new Vector2(620f, 30f), new Color(1f, 0.88f, 0.42f));

        CreateText(infoBar.transform,
            LocalizationManager.T("• Bấm 'ĐỔI TƯỚNG' để đổi nhân vật. Chủ phòng xuất trận khi tất cả đã bấm 'SẴN SÀNG'.",
                                  "• Tap 'CHANGE HERO' to switch heroes. Host starts once all teammates are 'READY'."),
            14.5f, new Vector2(-150f, -16f), new Vector2(880f, 26f), new Color(0.72f, 0.76f, 0.88f));

        // 5. THANH NÚT HÀNH ĐỘNG DƯỚI ĐÁY (y = -430f)
        // 5.1. Nút Rời Phòng (Bên trái)
        CreateRoundedButton(_step5WaitingRoomPanel.transform, "Btn_LeaveWaitingRoom",
            LocalizationManager.T("RỜI PHÒNG", "LEAVE ROOM"),
            new Vector2(-465f, -430f), new Vector2(230f, 74f),
            new Color(0.32f, 0.22f, 0.52f, 1f), 21f, LeaveWaitingRoomToStep2);

        // 5.2. Text Trạng Thái Realtime (Ở giữa)
        _roomStatusFooterText = CreateText(_step5WaitingRoomPanel.transform,
            LocalizationManager.T("ĐANG CHỜ ĐỒNG ĐỘI...", "WAITING FOR TEAMMATES..."),
            17f, new Vector2(-30f, -430f), new Vector2(480f, 50f), new Color(0.75f, 0.78f, 0.90f));

        // 5.3. Nút BẮT ĐẦU (Chỉ hiển thị cho Chủ Phòng)
        GameObject hostStartBtnObj = CreateRoundedButton(_step5WaitingRoomPanel.transform, "Btn_StartHostMatch",
            LocalizationManager.T("BẮT ĐẦU TRẬN >", "START MATCH >"),
            new Vector2(430f, -430f), new Vector2(340f, 82f),
            new Color(0.18f, 0.65f, 0.32f, 1f), 24f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                try
                {
                    string curHero = CharacterManager.EnsureInstance().GetSelectedCharacterId();
                    if (!string.IsNullOrEmpty(curHero))
                    {
                        CharacterManager.EnsureInstance().SelectCharacter(curHero, true);
                        AccountDatabase.SetSelectedHero(curHero);
                        AccountDatabase.SaveActiveSessionToDatabase();
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[PVP LOBBY] Hero commit on match start warning: " + ex);
                }
                PvPNetworkManager.EnsureInstance().TriggerCustomRoomStart();
            });
        _roomStartMatchBtn = hostStartBtnObj.GetComponent<Button>();
        _roomStartMatchBtnImg = hostStartBtnObj.GetComponent<Image>();
        _roomStartMatchBtnText = hostStartBtnObj.GetComponentInChildren<TextMeshProUGUI>();

        // 5.4. Nút SẴN SÀNG (Chỉ hiển thị cho Khách / Client)
        GameObject clientReadyBtnObj = CreateRoundedButton(_step5WaitingRoomPanel.transform, "Btn_ClientReady",
            LocalizationManager.T("SẴN SÀNG", "READY"),
            new Vector2(430f, -430f), new Vector2(340f, 82f),
            new Color(0.98f, 0.76f, 0.18f, 1f), 26f, OnClientToggleReadyClicked);
        _roomClientReadyBtn = clientReadyBtnObj.GetComponent<Button>();
        _roomClientReadyBtnImg = clientReadyBtnObj.GetComponent<Image>();
        _roomClientReadyBtnText = clientReadyBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (_roomClientReadyBtnText != null)
        {
            _roomClientReadyBtnText.color = new Color(0.22f, 0.14f, 0.02f);
        }
        clientReadyBtnObj.SetActive(false);

        // 6. POPUPS: HƯỚNG DẪN MỜI BẠN, CHỌN TƯỚNG, CHAT VÀ BẠN BÈ
        BuildInviteGuideDialog(_step5WaitingRoomPanel.transform);
        BuildHeroSelectionModal(_step5WaitingRoomPanel.transform);
        BuildLobbyChatModal(_step5WaitingRoomPanel.transform);
    }

    private void BuildHeaderPinInputField(Transform parent)
    {
        // Khung nhập PIN tại x = 50f, rộng 280f, không đè lên Nút BẠN BÈ (x = 280f)
        GameObject pinBox = CreateRoundedBox(parent, "HeaderPinJoinBox",
            new Vector2(50f, 435f), new Vector2(280f, 52f),
            new Color(0.10f, 0.09f, 0.18f, 0.98f));
        CreateBorderBox(pinBox.transform, "PinBoxBorder", Vector2.zero, new Vector2(280f, 52f), new Color(0.28f, 0.25f, 0.45f, 1f));

        GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(pinBox.transform, false);
        RectTransform taRt = textArea.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(10f, 6f);
        taRt.offsetMax = new Vector2(-95f, -6f); // Chừa 95px mép phải cho nút VÀO >

        TextMeshProUGUI placeholder = CreateText(textArea.transform, LocalizationManager.T("Nhập mã PIN...", "Enter PIN..."), 14.5f, Vector2.zero, new Vector2(165f, 38f), new Color(0.5f, 0.55f, 0.65f, 0.65f));
        placeholder.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI inputText = CreateText(textArea.transform, "", 18f, Vector2.zero, new Vector2(165f, 38f), new Color(1f, 0.92f, 0.42f));
        inputText.alignment = TextAlignmentOptions.Left;

        _headerPinInput = pinBox.AddComponent<TMP_InputField>();
        _headerPinInput.textViewport = taRt;
        _headerPinInput.textComponent = inputText;
        _headerPinInput.placeholder = placeholder;
        _headerPinInput.characterLimit = 6;
        _headerPinInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        CreateRoundedButton(pinBox.transform, "Btn_HeaderJoinPin",
            LocalizationManager.T("VÀO >", "JOIN >"),
            new Vector2(90f, 0f), new Vector2(82f, 40f),
            new Color(0.22f, 0.65f, 0.35f, 1f), 14.5f, OnHeaderJoinPinClicked);
    }

    private void OnHeaderJoinPinClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        string pin = _headerPinInput != null ? _headerPinInput.text.Trim() : "";
        if (string.IsNullOrEmpty(pin))
        {
            ShowLobbyToast(LocalizationManager.T("VUI LÒNG NHẬP MÃ PHÒNG (PIN) ĐỂ VÀO!", "PLEASE ENTER A ROOM PIN!"), new Color(1f, 0.45f, 0.45f));
            return;
        }

        ShowLobbyToast(LocalizationManager.T($"ĐANG TÌM PHÒNG #{pin}...", $"SEARCHING FOR ROOM #{pin}..."), new Color(0.45f, 0.92f, 1f));

        PvPNetworkManager.EnsureInstance().JoinRoomWithPin(pin, (success, errorMsg) =>
        {
            if (success)
            {
                _pendingCustomPin = pin;
                ShowLobbyToast(LocalizationManager.T($"ĐÃ VÀO PHÒNG #{pin} THÀNH CÔNG!", $"JOINED ROOM #{pin} SUCCESSFULLY!"), new Color(0.35f, 1f, 0.55f));
                RefreshStep5WaitingRoomUI();
            }
            else
            {
                ShowLobbyToast(errorMsg, new Color(1f, 0.45f, 0.45f));
                SoundManager.EnsureInstance()?.PlayMenuHover();
            }
        });
    }

    private void OnClientToggleReadyClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        var net = PvPNetworkManager.EnsureInstance();
        bool newReadyState = !net.IsLocalClientReady;

        string curHero = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        if (!string.IsNullOrEmpty(curHero))
        {
            CharacterManager.EnsureInstance().SelectCharacter(curHero, true);
            AccountDatabase.SetSelectedHero(curHero);
            AccountDatabase.SaveActiveSessionToDatabase();
            net.SendHeroChange(curHero);
        }

        net.SendReadyPacket(newReadyState);

        if (_roomClientReadyBtnImg != null)
        {
            _roomClientReadyBtnImg.color = newReadyState
                ? new Color(0.15f, 0.65f, 0.32f, 1f)
                : new Color(0.98f, 0.76f, 0.18f, 1f);
        }
        if (_roomClientReadyBtnText != null)
        {
            _roomClientReadyBtnText.text = newReadyState
                ? LocalizationManager.T("ĐÃ SẴN SÀNG (HỦY)", "READY (CANCEL)")
                : LocalizationManager.T("SẴN SÀNG", "READY");
            _roomClientReadyBtnText.color = newReadyState ? Color.white : new Color(0.22f, 0.14f, 0.02f);
        }

        ShowLobbyToast(newReadyState
            ? LocalizationManager.T("BẠN ĐÃ SẴN SÀNG! ĐANG CHỜ CHỦ PHÒNG BẮT ĐẦU...", "YOU ARE READY! WAITING FOR HOST TO START...")
            : LocalizationManager.T("ĐÃ HỦY SẴN SÀNG!", "CANCELLED READY!"),
            newReadyState ? new Color(0.35f, 1f, 0.55f) : new Color(1f, 0.75f, 0.35f));
    }

    // =========================================================================
    // MODAL CHỌN TƯỚNG TRỰC TIẾP TẠI PHÒNG CHỜ (HERO SELECTION DRAWER)
    // =========================================================================
    private void BuildHeroSelectionModal(Transform parent)
    {
        _heroSelectionModalObj = CreateRoundedBox(parent, "HeroSelectionModal", Vector2.zero, new Vector2(1300f, 620f), new Color(0.10f, 0.09f, 0.18f, 0.99f));
        CreateBorderBox(_heroSelectionModalObj.transform, "ModalBord", Vector2.zero, new Vector2(1300f, 620f), new Color(0.35f, 0.45f, 0.75f, 1f));

        CreateText(_heroSelectionModalObj.transform,
            LocalizationManager.T("CHỌN HIỆP SĨ XUẤT TRẬN", "CHOOSE YOUR CHAMPION"),
            24f, new Vector2(0f, 255f), new Vector2(800f, 40f), new Color(1f, 0.88f, 0.35f));

        CreateText(_heroSelectionModalObj.transform,
            LocalizationManager.T("Bấm trực tiếp vào ảnh đại diện hiệp sĩ để chọn. Tướng chưa sở hữu sẽ bị làm tối.", "Tap directly on hero portrait to select. Locked heroes are dimmed."),
            16f, new Vector2(0f, 215f), new Vector2(800f, 30f), new Color(0.75f, 0.78f, 0.90f));

        // Nút Đóng
        CreateRoundedButton(_heroSelectionModalObj.transform, "Btn_CloseHeroModal", "X",
            new Vector2(610f, 260f), new Vector2(46f, 46f),
            new Color(0.45f, 0.20f, 0.25f, 1f), 20f, () => _heroSelectionModalObj.SetActive(false));

        // 7 TƯỚNG XẾP HÀNG NGANG
        string[] heroIds = new string[]
        {
            CharacterManager.CHAR_VAELSTRIDER,
            CharacterManager.CHAR_GALE_RONIN,
            CharacterManager.CHAR_AZURE_BLADE,
            CharacterManager.CHAR_TITAN_BERSERKER,
            CharacterManager.CHAR_DUNE_ASSASSIN,
            CharacterManager.CHAR_CELESTIAL_MAGE,
            CharacterManager.CHAR_ABYSSAL_MECH
        };

        float startX = -510f;
        float spacingX = 170f;

        _heroCardSlots.Clear();

        for (int i = 0; i < heroIds.Length; i++)
        {
            string hid = heroIds[i];
            float px = startX + i * spacingX;

            var cData = CharacterManager.EnsureInstance().GetCharacter(hid);
            string hName = cData != null ? (LocalizationManager.IsVietnamese ? cData.nameVN : cData.nameEN) : hid;

            GameObject card = CreateRoundedBox(_heroSelectionModalObj.transform, $"HeroCard_{hid}",
                new Vector2(px, 15f), new Vector2(160f, 330f), new Color(0.14f, 0.13f, 0.25f, 1f));
            Image cardBg = card.GetComponent<Image>();
            GameObject cardBordObj = CreateBorderBox(card.transform, "CardBord", Vector2.zero, new Vector2(160f, 330f), new Color(0.28f, 0.30f, 0.48f, 1f));
            Image cardBord = cardBordObj.GetComponent<Image>();

            // Nút bấm trên toàn bộ Card (Chạm vào Avatar hoặc bất kỳ vị trí nào trên thẻ đều nhận thao tác)
            Button cardBtn = card.AddComponent<Button>();
            cardBtn.transition = Selectable.Transition.ColorTint;

            // Avatar Portrait Box
            GameObject avBox = CreateRoundedBox(card.transform, "AvBox", new Vector2(0f, 65f), new Vector2(120f, 120f), new Color(0.08f, 0.08f, 0.15f, 1f));
            GameObject avBordObj = CreateBorderBox(avBox.transform, "AvBord", Vector2.zero, new Vector2(120f, 120f), new Color(0.35f, 0.65f, 0.95f, 1f));
            Image avBord = avBordObj.GetComponent<Image>();

            Image portrait = CreateHeroPortraitImage(avBox.transform, "Portrait", Vector2.zero, new Vector2(110f, 110f), false);
            portrait.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(hid);

            // Icon Ổ Khóa che mờ trên Avatar nếu chưa mua
            GameObject lockOverlay = new GameObject("LockOverlay", typeof(RectTransform));
            lockOverlay.transform.SetParent(avBox.transform, false);
            RectTransform lockRt = lockOverlay.GetComponent<RectTransform>();
            lockRt.anchoredPosition = Vector2.zero;
            lockRt.sizeDelta = new Vector2(110f, 110f);
            var lockTxt = CreateText(lockOverlay.transform, "🔒", 38f, Vector2.zero, new Vector2(90f, 90f), new Color(1f, 0.35f, 0.35f, 0.95f));
            lockTxt.raycastTarget = false;
            lockOverlay.SetActive(false);

            // Tên tướng
            var nameTmp = CreateText(card.transform, hName, 17f, new Vector2(0f, -25f), new Vector2(150f, 44f), new Color(1f, 0.88f, 0.45f, 1f));
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.enableWordWrapping = true;
            nameTmp.alignment = TextAlignmentOptions.Center;

            // Dòng trạng thái (ĐÃ SỞ HỮU / ★ ĐANG CHỌN / 🔒 CHƯA SỞ HỮU)
            var statusTmp = CreateText(card.transform, "", 14f, new Vector2(0f, -68f), new Vector2(150f, 32f), Color.white);
            statusTmp.fontStyle = FontStyles.Bold;

            // Dòng phụ (CHẠM ĐỂ CHỌN / GIÁ LINH HỒN)
            var subTmp = CreateText(card.transform, "", 13f, new Vector2(0f, -102f), new Vector2(150f, 30f), Color.white);

            var slot = new HeroCardSlotUI
            {
                heroId = hid,
                cardObj = card,
                cardBg = cardBg,
                cardBord = cardBord,
                avBord = avBord,
                portraitImg = portrait,
                lockOverlayObj = lockOverlay,
                nameText = nameTmp,
                statusText = statusTmp,
                subText = subTmp,
                cardBtn = cardBtn
            };

            string capturedId = hid;
            cardBtn.onClick.AddListener(() => OnHeroCardTapped(capturedId));

            _heroCardSlots.Add(slot);
        }

        // Nút Đóng phía dưới
        CreateRoundedButton(_heroSelectionModalObj.transform, "Btn_BottomClose",
            LocalizationManager.T("HOÀN TẤT", "DONE"),
            new Vector2(0f, -235f), new Vector2(220f, 54f),
            new Color(0.32f, 0.25f, 0.55f, 1f), 18f, () => _heroSelectionModalObj.SetActive(false));

        _heroSelectionModalObj.SetActive(false);
    }

    private bool IsHeroOwned(string heroId)
    {
        string normId = CharacterManager.NormalizeHeroId(heroId);
        if (normId == CharacterManager.CHAR_VAELSTRIDER) return true;

        if (PlayerPrefs.GetInt("Char_Unlocked_" + normId, 0) == 1) return true;

        var acc = AccountDatabase.GetActiveAccount();
        if (acc != null && acc.unlockedHeroes != null)
        {
            string alias = CharacterManager.GetHeroAliasStatic(normId);
            if (acc.unlockedHeroes.Contains(normId) || acc.unlockedHeroes.Contains(alias) || acc.unlockedHeroes.Contains(heroId))
            {
                return true;
            }
        }

        return CharacterManager.EnsureInstance().IsCharacterPurchased(normId);
    }

    private void RefreshHeroSelectionModalUI()
    {
        string currentSelected = CharacterManager.EnsureInstance().GetSelectedCharacterId();

        for (int i = 0; i < _heroCardSlots.Count; i++)
        {
            var slot = _heroCardSlots[i];
            if (slot == null || slot.cardObj == null) continue;

            string hid = slot.heroId;
            bool isOwned = IsHeroOwned(hid);
            bool isSelected = (hid == currentSelected);

            var cData = CharacterManager.EnsureInstance().GetCharacter(hid);
            int price = cData != null ? cData.unlockPrice : 0;

            if (isOwned)
            {
                // TƯỚNG ĐÃ MUA: SÁNG RỰC RỠ, VIỀN NỔI BẬT
                slot.cardBg.color = isSelected 
                    ? new Color(0.18f, 0.22f, 0.38f, 0.98f) 
                    : new Color(0.12f, 0.14f, 0.24f, 0.98f);

                slot.cardBord.color = isSelected 
                    ? new Color(1f, 0.85f, 0.25f, 1f) 
                    : new Color(0.35f, 0.65f, 0.95f, 0.9f);

                slot.avBord.color = isSelected 
                    ? new Color(1f, 0.85f, 0.25f, 1f) 
                    : new Color(0.40f, 0.80f, 1f, 1f);

                slot.portraitImg.color = Color.white; // Avatar sáng rõ 100%
                if (slot.lockOverlayObj != null) slot.lockOverlayObj.SetActive(false);

                slot.nameText.color = isSelected ? new Color(1f, 0.95f, 0.55f, 1f) : new Color(0.92f, 0.95f, 1f, 1f);

                if (isSelected)
                {
                    slot.statusText.text = LocalizationManager.T("<color=#55FF88>★ ĐANG CHỌN</color>", "<color=#55FF88>★ SELECTED</color>");
                    slot.subText.text = LocalizationManager.T("<color=#FFDD66>HIỆP SĨ XUẤT TRẬN</color>", "<color=#FFDD66>ACTIVE HERO</color>");
                }
                else
                {
                    slot.statusText.text = LocalizationManager.T("<color=#66DDFF>ĐÃ SỞ HỮU</color>", "<color=#66DDFF>OWNED</color>");
                    slot.subText.text = LocalizationManager.T("<color=#AAFFAA>CHẠM ĐỂ CHỌN</color>", "<color=#AAFFAA>TAP TO PICK</color>");
                }
            }
            else
            {
                // TƯỚNG CHƯA MUA: TỐI SẪM, Ổ KHÓA CẢNH BÁO
                slot.cardBg.color = new Color(0.06f, 0.06f, 0.08f, 0.92f);
                slot.cardBord.color = new Color(0.20f, 0.20f, 0.25f, 0.8f);
                slot.avBord.color = new Color(0.25f, 0.25f, 0.30f, 0.8f);

                slot.portraitImg.color = new Color(0.24f, 0.24f, 0.28f, 0.65f); // Avatar tối chìm
                if (slot.lockOverlayObj != null) slot.lockOverlayObj.SetActive(true);

                slot.nameText.color = new Color(0.50f, 0.50f, 0.55f, 1f);
                slot.statusText.text = LocalizationManager.T("<color=#FF6666>🔒 CHƯA SỞ HỮU</color>", "<color=#FF6666>🔒 LOCKED</color>");
                slot.subText.text = LocalizationManager.T($"<color=#FFAA55>{price:N0} LINH HỒN</color>", $"<color=#FFAA55>{price:N0} SOULS</color>");
            }
        }
    }

    private void OnHeroCardTapped(string heroId)
    {
        string normId = CharacterManager.NormalizeHeroId(heroId);
        bool isOwned = IsHeroOwned(normId);

        var cData = CharacterManager.EnsureInstance().GetCharacter(normId);
        string hName = cData != null ? (LocalizationManager.IsVietnamese ? cData.nameVN : cData.nameEN) : normId;
        int price = cData != null ? cData.unlockPrice : 0;

        if (isOwned)
        {
            SelectHeroFromModal(normId);
            RefreshHeroSelectionModalUI();
        }
        else
        {
            // Báo cảnh báo ngay khi chọn vào tướng chưa mua
            SoundManager.EnsureInstance()?.PlayButtonClick();
            ShowLobbyToast(
                LocalizationManager.T($"TƯỚNG {hName.ToUpper()} CHƯA MỞ KHÓA! HÃY MUA BẰNG {price:N0} LINH HỒN TRONG CỬA HÀNG.",
                                      $"HERO {hName.ToUpper()} IS LOCKED! PURCHASE FOR {price:N0} SOULS IN SHOP."),
                new Color(1f, 0.35f, 0.35f, 1f));
        }
    }

    private void OpenHeroSelectionModal()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_heroSelectionModalObj != null)
        {
            RefreshHeroSelectionModalUI();
            _heroSelectionModalObj.SetActive(true);
            _heroSelectionModalObj.transform.SetAsLastSibling();
        }
    }

    private void SelectHeroFromModal(string heroId)
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        string normId = CharacterManager.NormalizeHeroId(heroId);
        string heroAlias = CharacterManager.GetHeroAliasStatic(normId);

        _previewHeroId = normId;

        // 1. Áp dụng ngay vào CharacterManager
        var charMgr = CharacterManager.EnsureInstance();
        charMgr.SelectCharacter(normId, true);

        // 2. Ghi đè tuyệt đối vào PlayerPrefs
        PlayerPrefs.SetString("Selected_Character_ID", normId);
        PlayerPrefs.SetString("SelectedHero", heroAlias);
        PlayerPrefs.Save();

        // 3. Đồng bộ vào AccountDatabase của user hiện tại
        AccountDatabase.SetSelectedHero(normId);
        AccountDatabase.SaveActiveSessionToDatabase();

        // 4. Bắn gói tin đồng bộ mạng sang các đồng đội trong phòng
        PvPNetworkManager.EnsureInstance().SendHeroChange(normId);

        var cData = charMgr.GetCharacter(normId);
        string heroName = cData != null ? (LocalizationManager.IsVietnamese ? cData.nameVN : cData.nameEN) : normId;

        ShowLobbyToast(LocalizationManager.T($"ĐÃ CHỌN TƯỚNG: {heroName.ToUpper()}", $"SELECTED HERO: {heroName.ToUpper()}"), new Color(0.35f, 1f, 0.55f));
        RefreshStep5WaitingRoomUI();

        if (_heroSelectionModalObj != null) _heroSelectionModalObj.SetActive(false);
    }

    // =========================================================================
    // MODAL CHAT PHÒNG CHỜ (LOBBY TEAM CHAT MODAL)
    // =========================================================================
    private void BuildLobbyChatModal(Transform parent)
    {
        _lobbyChatModalObj = CreateRoundedBox(parent, "LobbyChatModal", Vector2.zero, new Vector2(740f, 650f), new Color(0.10f, 0.09f, 0.18f, 0.99f));
        CreateBorderBox(_lobbyChatModalObj.transform, "ChatModalBord", Vector2.zero, new Vector2(740f, 650f), new Color(0.35f, 0.45f, 0.75f, 1f));

        CreateText(_lobbyChatModalObj.transform,
            LocalizationManager.T("CHAT TỔ ĐỘI CO-OP", "CO-OP TEAM CHAT"),
            22f, new Vector2(0f, 280f), new Vector2(500f, 36f), new Color(1f, 0.88f, 0.35f));

        CreateRoundedButton(_lobbyChatModalObj.transform, "Btn_CloseChat", "X",
            new Vector2(330f, 285f), new Vector2(40f, 40f),
            new Color(0.45f, 0.20f, 0.25f, 1f), 18f, () => _lobbyChatModalObj.SetActive(false));

        // Khung Nhật Ký Tin Nhắn
        GameObject chatLogBox = CreateRoundedBox(_lobbyChatModalObj.transform, "ChatLogBox",
            new Vector2(0f, 65f), new Vector2(670f, 360f),
            new Color(0.06f, 0.05f, 0.11f, 1f));
        CreateBorderBox(chatLogBox.transform, "LogBord", Vector2.zero, new Vector2(670f, 360f), new Color(0.24f, 0.26f, 0.40f, 1f));

        _lobbyChatHistoryText = CreateText(chatLogBox.transform,
            LocalizationManager.T("Chào mừng bạn vào sảnh chat tổ đội! Hãy cùng thảo luận chiến thuật.", "Welcome to team chat! Discuss your strategy here."),
            17f, Vector2.zero, new Vector2(640f, 335f), Color.white);
        _lobbyChatHistoryText.alignment = TextAlignmentOptions.BottomLeft;
        _lobbyChatHistoryText.enableWordWrapping = true;

        // Dải 5 nút Chat Nhanh (Quick Chat Buttons)
        string[] quickPhrases = new string[]
        {
            "Sẵn sàng đi!",
            "Chờ tui đổi tướng",
            "Chiến thôi!",
            "OK!",
            "Cố lên anh em!"
        };

        float startQx = -240f;
        float spaceQx = 120f;
        for (int i = 0; i < quickPhrases.Length; i++)
        {
            string phrase = quickPhrases[i];
            CreateRoundedButton(_lobbyChatModalObj.transform, $"Btn_Quick_{i}", phrase,
                new Vector2(startQx + i * spaceQx, -150f), new Vector2(112f, 40f),
                new Color(0.18f, 0.24f, 0.42f, 1f), 13f, () => SendLobbyQuickChat(phrase));
        }

        // Hàng Nhập Text Tự Do
        GameObject inputBg = CreateRoundedBox(_lobbyChatModalObj.transform, "ChatInputBg",
            new Vector2(-65f, -225f), new Vector2(510f, 54f),
            new Color(0.06f, 0.05f, 0.10f, 1f));
        CreateBorderBox(inputBg.transform, "InBord", Vector2.zero, new Vector2(510f, 54f), new Color(0.28f, 0.35f, 0.55f, 1f));

        GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputBg.transform, false);
        RectTransform taRt = textArea.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(10f, 6f);
        taRt.offsetMax = new Vector2(-10f, -6f);

        TextMeshProUGUI placeholder = CreateText(textArea.transform, LocalizationManager.T("Nhập tin nhắn tổ đội...", "Type team message..."), 16f, Vector2.zero, new Vector2(480f, 40f), new Color(0.5f, 0.55f, 0.65f, 0.65f));
        placeholder.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI inputText = CreateText(textArea.transform, "", 18f, Vector2.zero, new Vector2(480f, 40f), Color.white);
        inputText.alignment = TextAlignmentOptions.Left;

        _lobbyChatInputField = inputBg.AddComponent<TMP_InputField>();
        _lobbyChatInputField.textViewport = taRt;
        _lobbyChatInputField.textComponent = inputText;
        _lobbyChatInputField.placeholder = placeholder;
        _lobbyChatInputField.characterLimit = 50;

        CreateRoundedButton(_lobbyChatModalObj.transform, "Btn_SendChat",
            LocalizationManager.T("GỬI", "SEND"),
            new Vector2(245f, -225f), new Vector2(120f, 54f),
            new Color(0.22f, 0.65f, 0.35f, 1f), 17f, SendLobbyChatFromInput);

        _lobbyChatModalObj.SetActive(false);
    }

    private void OpenLobbyChatModal()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_lobbyChatModalObj != null)
        {
            _lobbyChatModalObj.SetActive(true);
            _lobbyChatModalObj.transform.SetAsLastSibling();
            _unreadChatCount = 0;
            if (_lobbyChatBtnBadgeText != null)
            {
                _lobbyChatBtnBadgeText.text = "CHAT";
                _lobbyChatBtnBadgeText.color = Color.white;
            }
            UpdateLobbyChatHistoryUI();
        }
    }

    private void SendLobbyQuickChat(string phrase)
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        PvPNetworkManager.EnsureInstance().SendChatMessage(phrase);
    }

    private void SendLobbyChatFromInput()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_lobbyChatInputField != null && !string.IsNullOrEmpty(_lobbyChatInputField.text.Trim()))
        {
            string msg = _lobbyChatInputField.text.Trim();
            PvPNetworkManager.EnsureInstance().SendChatMessage(msg);
            _lobbyChatInputField.text = "";
        }
    }

    private void UpdateLobbyChatHistoryUI()
    {
        if (_lobbyChatHistoryText == null) return;
        if (_chatMessages.Count == 0)
        {
            _lobbyChatHistoryText.text = LocalizationManager.T("Chưa có tin nhắn nào. Hãy gửi lời chào đồng đội!", "No messages yet. Say hello to teammates!");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var m in _chatMessages)
        {
            sb.AppendLine(m);
        }
        _lobbyChatHistoryText.text = sb.ToString();
    }

    private void BuildInviteGuideDialog(Transform parent)
    {
        _inviteGuideDialogObj = CreateRoundedBox(parent, "InviteGuideDialogModal", Vector2.zero, new Vector2(660f, 480f), new Color(0.12f, 0.11f, 0.22f, 0.99f));
        CreateBorderBox(_inviteGuideDialogObj.transform, "DialogBorder", Vector2.zero, new Vector2(660f, 480f), new Color(0.38f, 0.32f, 0.60f, 1f));

        CreateText(_inviteGuideDialogObj.transform, LocalizationManager.T("MỜI BẠN BÈ VÀO PHÒNG CO-OP", "INVITE FRIENDS TO CO-OP"),
            22f, new Vector2(0f, 180f), new Vector2(580f, 40f), new Color(1f, 0.86f, 0.32f));

        _inviteGuideText = CreateText(_inviteGuideDialogObj.transform,
            LocalizationManager.T("Chia sẻ mã PIN phòng này cho bạn bè:\n\n#0000\n\nBạn bè có thể nhập mã này ở ô trên cùng góc phải để vào phòng!",
                                  "Share this Room PIN with friends:\n\n#0000\n\nFriends can enter this PIN at the top-right header to join!"),
            17f, new Vector2(0f, 80f), new Vector2(580f, 130f), new Color(0.85f, 0.88f, 0.98f));

        // Nút Sao Chép Mã Phòng
        CreateRoundedButton(_inviteGuideDialogObj.transform, "Btn_CopyPinGuide",
            LocalizationManager.T("SAO CHÉP MÃ PHÒNG", "COPY ROOM PIN"),
            new Vector2(-155f, -30f), new Vector2(280f, 52f),
            new Color(0.28f, 0.48f, 0.85f, 1f), 16f, () =>
            {
                OnCopyRoomPinClicked();
                if (_inviteGuideDialogObj != null) _inviteGuideDialogObj.SetActive(false);
            });

        // Nút Mở Danh Sách Bạn Bè để mời
        CreateRoundedButton(_inviteGuideDialogObj.transform, "Btn_OpenFriendsFromGuide",
            LocalizationManager.T("CHỌN TỪ BẠN BÈ", "INVITE FRIENDS"),
            new Vector2(155f, -30f), new Vector2(280f, 52f),
            new Color(0.20f, 0.52f, 0.45f, 1f), 16f, () =>
            {
                if (_inviteGuideDialogObj != null) _inviteGuideDialogObj.SetActive(false);
                OpenFriendsModal();
            });

        // Nút Đóng
        CreateRoundedButton(_inviteGuideDialogObj.transform, "Btn_CloseGuide",
            LocalizationManager.T("ĐÓNG", "CLOSE"),
            new Vector2(0f, -120f), new Vector2(180f, 48f),
            new Color(0.35f, 0.22f, 0.38f, 1f), 17f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                if (_inviteGuideDialogObj != null) _inviteGuideDialogObj.SetActive(false);
            });

        _inviteGuideDialogObj.SetActive(false);
    }

    private void OpenInviteGuideDialog()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_inviteGuideDialogObj != null)
        {
            var net = PvPNetworkManager.EnsureInstance();
            string pin = string.IsNullOrEmpty(net.CurrentRoomCode) ? _pendingCustomPin : net.CurrentRoomCode;
            if (_inviteGuideText != null)
            {
                _inviteGuideText.text = LocalizationManager.T(
                    $"Chia sẻ mã PIN phòng này cho bạn bè:\n\n<color=#FFD54F><size=32>#{pin}</size></color>\n\nBạn bè có thể nhập mã này ở ô trên cùng góc phải để vào phòng!",
                    $"Share this Room PIN with friends:\n\n<color=#FFD54F><size=32>#{pin}</size></color>\n\nFriends can enter this PIN at the top-right header to join!");
            }
            _inviteGuideDialogObj.SetActive(true);
        }
    }

    private void OnCopyRoomPinClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        var net = PvPNetworkManager.EnsureInstance();
        string pin = string.IsNullOrEmpty(net.CurrentRoomCode) ? _pendingCustomPin : net.CurrentRoomCode;
        GUIUtility.systemCopyBuffer = pin;
        ShowLobbyToast(LocalizationManager.T($"ĐÃ SAO CHÉP MÃ PHÒNG #{pin} ĐỂ GỬI BẠN BÈ!", $"COPIED ROOM PIN #{pin} TO CLIPBOARD!"), new Color(0.35f, 1f, 0.55f));
    }

    private void LeaveWaitingRoomToStep2()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsHost)
        {
            PvPNetworkManager.Instance.BroadcastRoomClosed();
        }
        PvPNetworkManager.Instance?.ShutdownRunnerOnly();
        CloseLobby();
    }

    private void OnKickSlotClicked(int slotIndex)
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        var net = PvPNetworkManager.EnsureInstance();
        if (!net.IsHost) return;
        string target = !string.IsNullOrEmpty(net.OpponentName) ? net.OpponentName : "Đồng đội";
        net.KickPlayer(target);
        ShowLobbyToast(LocalizationManager.T($"ĐÃ KÍCH {target.ToUpper()} KHỎI PHÒNG!", $"KICKED {target.ToUpper()} FROM ROOM!"), new Color(1f, 0.45f, 0.45f));
    }

    private void OnAddFriendSlotClicked(int slotIndex)
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        var net = PvPNetworkManager.EnsureInstance();
        string target = !string.IsNullOrEmpty(net.OpponentName) ? net.OpponentName : "";
        if (string.IsNullOrEmpty(target)) return;
        FriendManager.EnsureInstance()?.SendFriendRequest(target);
        ShowLobbyToast(LocalizationManager.T($"ĐÃ GỬI LỜI MỜI KẾT BẠN TỚI {target.ToUpper()}!", $"SENT FRIEND REQUEST TO {target.ToUpper()}!"), new Color(0.35f, 1f, 0.55f));
    }

    public static void ShowGlobalLobbyNotification(string msg, Color col)
    {
        if (Instance != null && Instance.gameObject.activeInHierarchy)
        {
            Instance.ShowLobbyToast(msg, col);
        }
    }

    // =========================================================================
    // HỆ THỐNG BẠN BÈ (FRIEND SYSTEM MODAL & EDIT NICKNAME)
    // =========================================================================
    private void BuildFriendsModal(Transform parent)
    {
        _friendsModalObj = CreateRoundedBox(parent, "FriendsManagerModal", Vector2.zero, new Vector2(920f, 680f), new Color(0.10f, 0.09f, 0.18f, 0.99f));
        CreateBorderBox(_friendsModalObj.transform, "FriendsModalBord", Vector2.zero, new Vector2(920f, 680f), new Color(0.35f, 0.45f, 0.75f, 1f));

        CreateText(_friendsModalObj.transform,
            LocalizationManager.T("HỆ THỐNG BẠN BÈ", "FRIENDS SYSTEM"),
            24f, new Vector2(0f, 290f), new Vector2(600f, 38f), new Color(1f, 0.88f, 0.35f));

        CreateRoundedButton(_friendsModalObj.transform, "Btn_CloseFriends", "X",
            new Vector2(420f, 295f), new Vector2(40f, 40f),
            new Color(0.45f, 0.20f, 0.25f, 1f), 18f, CloseFriendsModal);

        // 3 Nút Tab chuyển đổi
        GameObject tabFriendsBtn = CreateRoundedButton(_friendsModalObj.transform, "Btn_TabFriends",
            LocalizationManager.T("DANH SÁCH BẠN BÈ", "FRIENDS LIST"),
            new Vector2(-260f, 230f), new Vector2(230f, 48f),
            new Color(0.25f, 0.45f, 0.85f, 1f), 15f, () => SwitchFriendTab(0));
        _friendTabFriendsBtnImg = tabFriendsBtn.GetComponent<Image>();
        _friendsTabCountText = tabFriendsBtn.GetComponentInChildren<TextMeshProUGUI>();

        GameObject tabReqsBtn = CreateRoundedButton(_friendsModalObj.transform, "Btn_TabReqs",
            LocalizationManager.T("LỜI MỜI (0)", "REQUESTS (0)"),
            new Vector2(0f, 230f), new Vector2(230f, 48f),
            new Color(0.18f, 0.18f, 0.30f, 1f), 15f, () => SwitchFriendTab(1));
        _friendTabRequestsBtnImg = tabReqsBtn.GetComponent<Image>();
        _requestsTabCountText = tabReqsBtn.GetComponentInChildren<TextMeshProUGUI>();

        GameObject tabAddBtn = CreateRoundedButton(_friendsModalObj.transform, "Btn_TabAdd",
            LocalizationManager.T("+ THÊM BẠN MỚI", "+ ADD FRIEND"),
            new Vector2(260f, 230f), new Vector2(230f, 48f),
            new Color(0.18f, 0.18f, 0.30f, 1f), 15f, () => SwitchFriendTab(2));
        _friendTabAddBtnImg = tabAddBtn.GetComponent<Image>();

        // TAB 1: DANH SÁCH BẠN BÈ
        _friendTabFriendsPanel = new GameObject("TabFriendsPanel", typeof(RectTransform));
        _friendTabFriendsPanel.transform.SetParent(_friendsModalObj.transform, false);
        RectTransform tfRt = _friendTabFriendsPanel.GetComponent<RectTransform>();
        tfRt.anchoredPosition = new Vector2(0f, -30f);
        tfRt.sizeDelta = new Vector2(880f, 440f);

        GameObject listContainer = new GameObject("FriendsListContainer", typeof(RectTransform));
        listContainer.transform.SetParent(_friendTabFriendsPanel.transform, false);
        RectTransform lcRt = listContainer.GetComponent<RectTransform>();
        lcRt.anchorMin = Vector2.zero;
        lcRt.anchorMax = Vector2.one;
        lcRt.sizeDelta = Vector2.zero;
        _friendsListContainer = listContainer.transform;

        // TAB 2: LỜI MỜI KẾT BẠN
        _friendTabRequestsPanel = new GameObject("TabRequestsPanel", typeof(RectTransform));
        _friendTabRequestsPanel.transform.SetParent(_friendsModalObj.transform, false);
        RectTransform trRt = _friendTabRequestsPanel.GetComponent<RectTransform>();
        trRt.anchoredPosition = new Vector2(0f, -30f);
        trRt.sizeDelta = new Vector2(880f, 440f);

        GameObject reqContainer = new GameObject("FriendRequestsListContainer", typeof(RectTransform));
        reqContainer.transform.SetParent(_friendTabRequestsPanel.transform, false);
        RectTransform rcRt = reqContainer.GetComponent<RectTransform>();
        rcRt.anchorMin = Vector2.zero;
        rcRt.anchorMax = Vector2.one;
        rcRt.sizeDelta = Vector2.zero;
        _friendRequestsListContainer = reqContainer.transform;

        // TAB 3: THÊM BẠN BÈ
        _friendTabAddPanel = new GameObject("TabAddPanel", typeof(RectTransform));
        _friendTabAddPanel.transform.SetParent(_friendsModalObj.transform, false);
        RectTransform taRt = _friendTabAddPanel.GetComponent<RectTransform>();
        taRt.anchoredPosition = new Vector2(0f, -30f);
        taRt.sizeDelta = new Vector2(880f, 440f);

        CreateText(_friendTabAddPanel.transform,
            LocalizationManager.T("KẾT BẠN THEO TÊN NGƯỜI CHƠI", "ADD FRIEND BY PLAYER NAME"),
            21f, new Vector2(0f, 120f), new Vector2(800f, 36f), new Color(1f, 0.86f, 0.32f));

        CreateText(_friendTabAddPanel.transform,
            LocalizationManager.T("Nhập chính xác tên nhân vật của bạn bè:", "Enter friend's character name:"),
            15.5f, new Vector2(-120f, 50f), new Vector2(360f, 26f), new Color(0.75f, 0.78f, 0.90f)).alignment = TextAlignmentOptions.Left;

        GameObject nameBox = CreateRoundedBox(_friendTabAddPanel.transform, "AddFriendNameBox", new Vector2(0f, 0f), new Vector2(620f, 52f), new Color(0.06f, 0.05f, 0.11f, 1f));
        CreateBorderBox(nameBox.transform, "NBord", Vector2.zero, new Vector2(620f, 52f), new Color(0.28f, 0.35f, 0.55f, 1f));
        _addFriendNameInput = SetupSimpleInputField(nameBox, "VD: HoangNam, Tuấn, ShadowBlade...", 24);

        CreateRoundedButton(_friendTabAddPanel.transform, "Btn_DoAddFriend",
            LocalizationManager.T("+ GỬI LỜI MỜI KẾT BẠN", "+ SEND FRIEND REQUEST"),
            new Vector2(0f, -80f), new Vector2(340f, 56f),
            new Color(0.20f, 0.68f, 0.35f, 1f), 17f, OnAddFriendClicked);

        CreateText(_friendTabAddPanel.transform,
            LocalizationManager.T("Gợi ý: Nhập tên hiệp sĩ để gửi lời mời. Hai bạn sẽ chính thức trở thành bạn bè khi đối phương bấm 'Chấp Nhận'.", "Tip: Enter a player name to send a request. You become friends once they accept."),
            14f, new Vector2(0f, -145f), new Vector2(760f, 26f), new Color(0.65f, 0.70f, 0.85f));

        _friendTabRequestsPanel.SetActive(false);
        _friendTabAddPanel.SetActive(false);
        _friendsModalObj.SetActive(false);
    }

    public void HideAllStepPanels()
    {
        if (_step2ModePanel != null) _step2ModePanel.SetActive(false);
        if (_step3HeroPanel != null) _step3HeroPanel.SetActive(false);
        if (_step4CreateRoomPanel != null) _step4CreateRoomPanel.SetActive(false);
        if (_step4BJoinPinPanel != null) _step4BJoinPinPanel.SetActive(false);
        if (_step5WaitingRoomPanel != null) _step5WaitingRoomPanel.SetActive(false);
        if (_step6SearchingPanel != null) _step6SearchingPanel.SetActive(false);
        if (_step7VsPanel != null) _step7VsPanel.SetActive(false);
    }

    private void CloseFriendsModal()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_friendsModalObj != null) _friendsModalObj.SetActive(false);
        if (_openedFromMainMenuOnly)
        {
            _openedFromMainMenuOnly = false;
            if (_modalRoot != null) _modalRoot.SetActive(false);
            CloseLobby();
        }
    }

    public static void OpenFriendsModalFromMainMenu()
    {
        var lobby = EnsureInstance();
        if (lobby != null)
        {
            lobby._openedFromMainMenuOnly = true;
            if (lobby._modalRoot != null)
            {
                lobby._modalRoot.SetActive(true);
                lobby._modalRoot.transform.SetAsLastSibling();
            }
            lobby.HideAllStepPanels();
            lobby.OpenFriendsModal();
        }
    }

    private TMP_InputField SetupSimpleInputField(GameObject box, string placeholderText, int charLimit)
    {
        GameObject ta = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        ta.transform.SetParent(box.transform, false);
        RectTransform taRt = ta.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(10f, 4f);
        taRt.offsetMax = new Vector2(-10f, -4f);

        TextMeshProUGUI ph = CreateText(ta.transform, placeholderText, 15f, Vector2.zero, new Vector2(560f, 36f), new Color(0.5f, 0.55f, 0.65f, 0.65f));
        ph.alignment = TextAlignmentOptions.Left;

        TextMeshProUGUI inputTxt = CreateText(ta.transform, "", 17f, Vector2.zero, new Vector2(560f, 36f), Color.white);
        inputTxt.alignment = TextAlignmentOptions.Left;

        TMP_InputField inf = box.AddComponent<TMP_InputField>();
        inf.textViewport = taRt;
        inf.textComponent = inputTxt;
        inf.placeholder = ph;
        inf.characterLimit = charLimit;
        return inf;
    }

    private void OpenFriendsModal()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_friendsModalObj != null)
        {
            _friendsModalObj.SetActive(true);
            _friendsModalObj.transform.SetAsLastSibling();
            SwitchFriendTab(0);
            RefreshFriendsListUI();
        }
    }

    private void SwitchFriendTab(int tabIndex)
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_friendTabFriendsPanel != null) _friendTabFriendsPanel.SetActive(tabIndex == 0);
        if (_friendTabRequestsPanel != null) _friendTabRequestsPanel.SetActive(tabIndex == 1);
        if (_friendTabAddPanel != null) _friendTabAddPanel.SetActive(tabIndex == 2);

        Color activeCol = new Color(0.25f, 0.45f, 0.85f, 1f);
        Color inactiveCol = new Color(0.18f, 0.18f, 0.30f, 1f);

        if (_friendTabFriendsBtnImg != null) _friendTabFriendsBtnImg.color = (tabIndex == 0) ? activeCol : inactiveCol;
        if (_friendTabRequestsBtnImg != null) _friendTabRequestsBtnImg.color = (tabIndex == 1) ? activeCol : inactiveCol;
        if (_friendTabAddBtnImg != null) _friendTabAddBtnImg.color = (tabIndex == 2) ? activeCol : inactiveCol;

        if (tabIndex == 0) RefreshFriendsListUI();
        else if (tabIndex == 1) RefreshFriendRequestsListUI();
    }

    private void RefreshFriendRequestsListUI()
    {
        if (_friendRequestsListContainer == null) return;

        for (int i = _friendRequestsListContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_friendRequestsListContainer.GetChild(i).gameObject);
        }

        var reqs = FriendManager.EnsureInstance().GetPendingRequests();
        if (_requestsTabCountText != null)
        {
            string countStr = reqs.Count > 99 ? "99+" : reqs.Count.ToString();
            _requestsTabCountText.text = LocalizationManager.T($"LỜI MỜI ({countStr})", $"REQUESTS ({countStr})");
            _requestsTabCountText.color = reqs.Count > 0 ? new Color(1f, 0.85f, 0.35f) : Color.white;
        }

        if (reqs.Count == 0)
        {
            CreateText(_friendRequestsListContainer,
                LocalizationManager.T("Hiện không có lời mời kết bạn nào.", "No pending friend requests."),
                17f, Vector2.zero, new Vector2(700f, 60f), new Color(0.70f, 0.72f, 0.85f));
            return;
        }

        float startY = 160f;
        float spacingY = 100f;

        for (int i = 0; i < reqs.Count; i++)
        {
            if (i >= 4) break;
            var req = reqs[i];
            float py = startY - i * spacingY;

            GameObject row = CreateRoundedBox(_friendRequestsListContainer, $"ReqRow_{req.requestId}",
                new Vector2(0f, py), new Vector2(850f, 88f),
                new Color(0.13f, 0.12f, 0.23f, 0.98f));
            CreateBorderBox(row.transform, "RowBord", Vector2.zero, new Vector2(850f, 88f), new Color(0.26f, 0.28f, 0.44f, 1f));

            GameObject avBox = CreateRoundedBox(row.transform, "AvBox", new Vector2(-375f, 0f), new Vector2(68f, 68f), new Color(0.08f, 0.08f, 0.15f, 1f));
            CreateBorderBox(avBox.transform, "AvBord", Vector2.zero, new Vector2(68f, 68f), new Color(0.35f, 0.65f, 0.95f, 1f));
            CreateHeroPortraitImage(avBox.transform, "Portrait", Vector2.zero, new Vector2(60f, 60f), false).sprite = PvPRemoteOpponent.GetHeroPortraitSprite(req.senderHeroId);

            var nameTxt = CreateText(row.transform, req.senderName, 18f, new Vector2(-150f, 14f), new Vector2(360f, 30f), Color.white);
            nameTxt.alignment = TextAlignmentOptions.Left;

            var cData = CharacterManager.EnsureInstance().GetCharacter(req.senderHeroId);
            string hName = cData != null ? (LocalizationManager.IsVietnamese ? cData.nameVN : cData.nameEN) : req.senderHeroId;
            var subTxt = CreateText(row.transform, $"Lv. {req.senderLevel} • {hName}  |  {req.timestamp}", 14f, new Vector2(-150f, -16f), new Vector2(360f, 26f), new Color(0.70f, 0.75f, 0.88f));
            subTxt.alignment = TextAlignmentOptions.Left;

            string capturedReqId = req.requestId;
            string capturedSender = req.senderName;

            // Nút Từ Chối
            CreateRoundedButton(row.transform, "Btn_Decline",
                LocalizationManager.T("TỪ CHỐI", "DECLINE"),
                new Vector2(350f, 0f), new Vector2(100f, 40f),
                new Color(0.50f, 0.18f, 0.22f, 1f), 13.5f, () =>
                {
                    FriendManager.EnsureInstance().DeclineFriendRequest(capturedReqId);
                    ShowLobbyToast(LocalizationManager.T($"ĐÃ TỪ CHỐI LỜI MỜI TỪ {capturedSender}!", $"DECLINED REQUEST FROM {capturedSender}!"), new Color(1f, 0.65f, 0.45f));
                    RefreshFriendRequestsListUI();
                });

            // Nút Chấp Nhận
            CreateRoundedButton(row.transform, "Btn_Accept",
                LocalizationManager.T("CHẤP NHẬN", "ACCEPT"),
                new Vector2(230f, 0f), new Vector2(120f, 40f),
                new Color(0.18f, 0.65f, 0.32f, 1f), 14f, () =>
                {
                    FriendManager.EnsureInstance().AcceptFriendRequest(capturedReqId);
                    ShowLobbyToast(LocalizationManager.T($"ĐÃ KẾT BẠN VỚI {capturedSender} THÀNH CÔNG!", $"ACCEPTED FRIEND: {capturedSender}!"), new Color(0.35f, 1f, 0.55f));
                    RefreshFriendRequestsListUI();
                    RefreshFriendsListUI();
                });
        }
    }

    private void RefreshFriendsListUI()
    {
        if (_friendsListContainer == null) return;

        for (int i = _friendsListContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_friendsListContainer.GetChild(i).gameObject);
        }

        var friends = FriendManager.EnsureInstance().GetFriends();
        if (_friendsTabCountText != null)
        {
            _friendsTabCountText.text = LocalizationManager.T($"DANH SÁCH BẠN BÈ ({friends.Count})", $"FRIENDS ({friends.Count})");
        }

        if (friends.Count == 0)
        {
            CreateText(_friendsListContainer,
                LocalizationManager.T("Bạn chưa có bạn bè nào. Hãy chuyển sang tab 'Thêm Bạn Mới' để kết bạn!",
                                      "No friends yet. Switch to '+ Add Friend' tab to add buddies!"),
                17f, Vector2.zero, new Vector2(700f, 60f), new Color(0.70f, 0.72f, 0.85f));
            return;
        }

        float startY = 160f;
        float spacingY = 100f;

        for (int i = 0; i < friends.Count; i++)
        {
            if (i >= 4) break;
            var f = friends[i];
            float py = startY - i * spacingY;

            GameObject row = CreateRoundedBox(_friendsListContainer, $"FriendRow_{f.friendId}",
                new Vector2(0f, py), new Vector2(850f, 88f),
                new Color(0.13f, 0.12f, 0.23f, 0.98f));
            CreateBorderBox(row.transform, "RowBord", Vector2.zero, new Vector2(850f, 88f), new Color(0.26f, 0.28f, 0.44f, 1f));

            GameObject avBox = CreateRoundedBox(row.transform, "AvBox", new Vector2(-375f, 0f), new Vector2(68f, 68f), new Color(0.08f, 0.08f, 0.15f, 1f));
            CreateBorderBox(avBox.transform, "AvBord", Vector2.zero, new Vector2(68f, 68f), new Color(0.35f, 0.65f, 0.95f, 1f));
            CreateHeroPortraitImage(avBox.transform, "Portrait", Vector2.zero, new Vector2(60f, 60f), false).sprite = PvPRemoteOpponent.GetHeroPortraitSprite(f.heroId);

            CreateCircle(avBox.transform, "Dot", new Vector2(25f, -25f), new Vector2(14f, 14f),
                f.isOnline ? new Color(0.25f, 0.95f, 0.40f, 1f) : new Color(0.45f, 0.45f, 0.55f, 1f));

            string displayName = string.IsNullOrEmpty(f.nickname)
                ? f.originalName
                : $"{f.originalName} <color=#FFD54F>({f.nickname})</color>";

            var nameTxt = CreateText(row.transform, displayName, 18f, new Vector2(-150f, 14f), new Vector2(360f, 30f), Color.white);
            nameTxt.alignment = TextAlignmentOptions.Left;

            var cData = CharacterManager.EnsureInstance().GetCharacter(f.heroId);
            string hName = cData != null ? (LocalizationManager.IsVietnamese ? cData.nameVN : cData.nameEN) : f.heroId;
            string statusStr = f.isOnline
                ? (!string.IsNullOrEmpty(f.currentRoomPin) ? $"<color=#00E676>Online</color> • Phòng #{f.currentRoomPin}" : "<color=#00E676>Online</color> • Sẵn sàng")
                : "<color=#888888>Offline</color>";

            var subTxt = CreateText(row.transform, $"Lv. {f.level} • {hName}  |  {statusStr}", 14f, new Vector2(-150f, -16f), new Vector2(360f, 26f), new Color(0.70f, 0.75f, 0.88f));
            subTxt.alignment = TextAlignmentOptions.Left;

            float btnX = 360f;

            string capturedId = f.friendId;
            CreateRoundedButton(row.transform, "Btn_Del",
                LocalizationManager.T("XÓA", "DEL"),
                new Vector2(btnX, 0f), new Vector2(70f, 40f),
                new Color(0.50f, 0.18f, 0.22f, 1f), 13.5f, () => OnRemoveFriendClicked(capturedId));

            btnX -= 95f;
            CreateRoundedButton(row.transform, "Btn_EditNick",
                LocalizationManager.T("SỬA TÊN", "NOTE"),
                new Vector2(btnX, 0f), new Vector2(100f, 40f),
                new Color(0.28f, 0.40f, 0.75f, 1f), 13.5f, () => OpenEditNicknameModal(capturedId));

            // Nút MỜI VÀO PHÒNG (Xuất hiện khi người chơi đang ở trong phòng Co-op)
            string currentPin = (PvPNetworkManager.Instance != null && !string.IsNullOrEmpty(PvPNetworkManager.Instance.CurrentRoomCode)) 
                ? PvPNetworkManager.Instance.CurrentRoomCode 
                : _pendingCustomPin;
            if (!string.IsNullOrEmpty(currentPin))
            {
                btnX -= 95f;
                string sendPin = currentPin;
                string friendName = f.originalName;
                CreateRoundedButton(row.transform, "Btn_InviteRoom",
                    LocalizationManager.T("MỜI", "INVITE"),
                    new Vector2(btnX, 0f), new Vector2(85f, 40f),
                    new Color(0.18f, 0.65f, 0.32f, 1f), 13.5f, () =>
                    {
                        SoundManager.EnsureInstance()?.PlayButtonClick();
                        FriendManager.EnsureInstance().SendRoomInvite(friendName, sendPin);
                        ShowLobbyToast(LocalizationManager.T($"ĐÃ GỬI LỜI MỜI VÀO PHÒNG #{sendPin} TỚI {friendName.ToUpper()}!", $"SENT ROOM #{sendPin} INVITE TO {friendName.ToUpper()}!"), new Color(0.35f, 1f, 0.55f));
                    });
            }

            if (f.isOnline && !string.IsNullOrEmpty(f.currentRoomPin))
            {
                btnX -= 125f;
                string capturedPin = f.currentRoomPin;
                CreateRoundedButton(row.transform, "Btn_JoinRoom",
                    LocalizationManager.T("VÀO PHÒNG", "JOIN"),
                    new Vector2(btnX, 0f), new Vector2(130f, 40f),
                    new Color(0.18f, 0.65f, 0.32f, 1f), 14f, () => OnJoinFriendRoom(capturedPin));
            }
        }
    }

    private void OnAddFriendClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        string name = _addFriendNameInput != null ? _addFriendNameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(name))
        {
            ShowLobbyToast(LocalizationManager.T("VUI LÒNG NHẬP TÊN NGƯỜI CHƠI MUỐN KẾT BẠN!", "PLEASE ENTER FRIEND'S PLAYER NAME!"), new Color(1f, 0.45f, 0.45f));
            return;
        }

        string myName = GetLocalPlayerDisplayName();
        if (name.Equals(myName, StringComparison.OrdinalIgnoreCase))
        {
            ShowLobbyToast(LocalizationManager.T("BẠN KHÔNG THỂ TỰ GỬI LỜI MỜI CHO CHÍNH MÌNH!", "YOU CANNOT SEND A REQUEST TO YOURSELF!"), new Color(1f, 0.65f, 0.35f));
            return;
        }

        if (FriendManager.EnsureInstance().IsFriend(name))
        {
            ShowLobbyToast(LocalizationManager.T("NGƯỜI CHƠI NÀY ĐÃ LÀ BẠN BÈ CỦA BẠN RỒI!", "ALREADY IN YOUR FRIENDS LIST!"), new Color(1f, 0.75f, 0.35f));
            return;
        }

        // Gửi gói tin yêu cầu kết bạn đến đối phương
        PvPNetworkManager.EnsureInstance().SendFriendRequestPacket(name);

        ShowLobbyToast(LocalizationManager.T(
            $"ĐÃ GỬI LỜI MỜI KẾT BẠN ĐẾN: {name.ToUpper()}!\nĐANG CHỜ ĐỐI PHƯƠNG CHẤP NHẬN...", 
            $"FRIEND REQUEST SENT TO: {name.ToUpper()}!\nWAITING FOR ACCEPTANCE..."), 
            new Color(0.35f, 0.95f, 1f));

        if (_addFriendNameInput != null) _addFriendNameInput.text = "";
    }

    private void OnRemoveFriendClicked(string friendId)
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        var f = FriendManager.EnsureInstance().GetFriend(friendId);
        string fName = f != null ? f.GetFormattedDisplayName() : friendId;

        FriendManager.EnsureInstance().RemoveFriend(friendId);
        ShowLobbyToast(LocalizationManager.T($"ĐÃ XÓA {fName} KHỎI DANH SÁCH BẠN BÈ!", $"REMOVED {fName} FROM FRIENDS!"), new Color(1f, 0.55f, 0.55f));
        RefreshFriendsListUI();
    }

    private void OnJoinFriendRoom(string pin)
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (_friendsModalObj != null) _friendsModalObj.SetActive(false);
        if (_headerPinInput != null) _headerPinInput.text = pin;

        ShowLobbyToast(LocalizationManager.T($"ĐANG VÀO PHÒNG CỦA BẠN BÈ #{pin}...", $"JOINING FRIEND'S ROOM #{pin}..."), new Color(0.45f, 0.92f, 1f));

        PvPNetworkManager.EnsureInstance().JoinRoomWithPin(pin, (success, errorMsg) =>
        {
            if (success)
            {
                _pendingCustomPin = pin;
                ShowLobbyToast(LocalizationManager.T($"ĐÃ VÀO PHÒNG #{pin} THÀNH CÔNG!", $"JOINED ROOM #{pin} SUCCESSFULLY!"), new Color(0.35f, 1f, 0.55f));
                RefreshStep5WaitingRoomUI();
            }
            else
            {
                ShowLobbyToast(errorMsg, new Color(1f, 0.45f, 0.45f));
                SoundManager.EnsureInstance()?.PlayMenuHover();
            }
        });
    }

    // =========================================================================
    // MODAL SỬA TÊN GHI NHỚ (NICKNAME IN PARENTHESES)
    // =========================================================================
    private void BuildEditNicknameModal(Transform parent)
    {
        _editNicknameModalObj = CreateRoundedBox(parent, "EditNicknameModal", Vector2.zero, new Vector2(600f, 340f), new Color(0.10f, 0.09f, 0.18f, 0.99f));
        CreateBorderBox(_editNicknameModalObj.transform, "EditBord", Vector2.zero, new Vector2(600f, 340f), new Color(0.35f, 0.45f, 0.75f, 1f));

        CreateText(_editNicknameModalObj.transform,
            LocalizationManager.T("ĐẶT TÊN GHI NHỚ CHO BẠN BÈ", "SET FRIEND NICKNAME"),
            21f, new Vector2(0f, 115f), new Vector2(540f, 36f), new Color(1f, 0.88f, 0.35f));

        CreateText(_editNicknameModalObj.transform,
            LocalizationManager.T("Tên ghi nhớ sẽ được đóng trong ngoặc đơn sau tên bạn bè.\nVí dụ: Minh (Bạn Thân)",
                                  "Nickname will appear in parentheses after name.\nExample: Alex (Best Friend)"),
            14.5f, new Vector2(0f, 55f), new Vector2(540f, 44f), new Color(0.80f, 0.85f, 0.95f));

        GameObject inputBg = CreateRoundedBox(_editNicknameModalObj.transform, "NickInputBg", new Vector2(0f, -15f), new Vector2(460f, 48f), new Color(0.06f, 0.05f, 0.11f, 1f));
        CreateBorderBox(inputBg.transform, "InBord", Vector2.zero, new Vector2(460f, 48f), new Color(0.28f, 0.35f, 0.55f, 1f));
        _editNicknameInput = SetupSimpleInputField(inputBg, "Nhập tên ghi nhớ mới...", 20);

        CreateRoundedButton(_editNicknameModalObj.transform, "Btn_SaveNick",
            LocalizationManager.T("LƯU TÊN", "SAVE"),
            new Vector2(120f, -95f), new Vector2(200f, 50f),
            new Color(0.20f, 0.65f, 0.35f, 1f), 17f, OnConfirmEditNickname);

        CreateRoundedButton(_editNicknameModalObj.transform, "Btn_CancelNick",
            LocalizationManager.T("HỦY", "CANCEL"),
            new Vector2(-120f, -95f), new Vector2(180f, 50f),
            new Color(0.45f, 0.20f, 0.25f, 1f), 17f, () => _editNicknameModalObj.SetActive(false));

        _editNicknameModalObj.SetActive(false);
    }

    // =========================================================================
    // MODAL LỜI MỜI VÀO PHÒNG CO-OP (POPUP DIALOG CHẤP NHẬN / TỪ CHỐI)
    // =========================================================================
    private void BuildRoomInviteDialog(Transform parent)
    {
        _roomInviteDialogObj = CreateRoundedBox(parent, "RoomInviteDialogModal", Vector2.zero, new Vector2(640f, 360f), new Color(0.11f, 0.10f, 0.20f, 0.99f));
        CreateBorderBox(_roomInviteDialogObj.transform, "InviteBord", Vector2.zero, new Vector2(640f, 360f), new Color(0.40f, 0.55f, 0.95f, 1f));

        CreateText(_roomInviteDialogObj.transform,
            LocalizationManager.T("LỜI MỜI VÀO PHÒNG CO-OP", "CO-OP ROOM INVITATION"),
            22f, new Vector2(0f, 120f), new Vector2(580f, 38f), new Color(1f, 0.88f, 0.35f));

        _roomInvitePromptText = CreateText(_roomInviteDialogObj.transform,
            "",
            17f, new Vector2(0f, 35f), new Vector2(560f, 90f), new Color(0.88f, 0.92f, 1f));

        // Nút Tham Gia Ngay (Màu Xanh Lá Rực Rỡ)
        CreateRoundedButton(_roomInviteDialogObj.transform, "Btn_AcceptRoomInvite",
            LocalizationManager.T("VÀO PHÒNG NGAY", "JOIN ROOM"),
            new Vector2(-120f, -70f), new Vector2(210f, 52f),
            new Color(0.18f, 0.68f, 0.35f, 1f), 16f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                if (_roomInviteDialogObj != null) _roomInviteDialogObj.SetActive(false);
                if (!string.IsNullOrEmpty(_pendingInvitePin))
                {
                    if (_headerPinInput != null) _headerPinInput.text = _pendingInvitePin;
                    ShowLobbyToast(LocalizationManager.T($"ĐANG VÀO PHÒNG #{_pendingInvitePin}...", $"JOINING ROOM #{_pendingInvitePin}..."), new Color(0.45f, 0.92f, 1f));
                    PvPNetworkManager.EnsureInstance().JoinRoomWithPin(_pendingInvitePin, (success, errorMsg) =>
                    {
                        if (success)
                        {
                            ShowStep(LobbyStep.Step5_WaitingRoom);
                            RefreshStep5WaitingRoomUI();
                        }
                        else
                        {
                            ShowLobbyToast(LocalizationManager.T($"LỖI VÀO PHÒNG: {errorMsg}", $"JOIN ERROR: {errorMsg}"), new Color(1f, 0.45f, 0.45f));
                        }
                    });
                }
            });

        // Nút Từ Chối (Màu Đỏ)
        CreateRoundedButton(_roomInviteDialogObj.transform, "Btn_DeclineRoomInvite",
            LocalizationManager.T("TỪ CHỐI", "DECLINE"),
            new Vector2(120f, -70f), new Vector2(160f, 52f),
            new Color(0.50f, 0.20f, 0.25f, 1f), 16f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                if (_roomInviteDialogObj != null) _roomInviteDialogObj.SetActive(false);
            });

        _roomInviteDialogObj.SetActive(false);
    }

    public void OpenRoomInvitePopup(string senderName, string roomPin)
    {
        _pendingInvitePin = roomPin;
        if (_roomInviteDialogObj == null)
        {
            BuildRoomInviteDialog(_modalRoot != null ? _modalRoot.transform : transform);
        }
        if (_roomInvitePromptText != null)
        {
            _roomInvitePromptText.text = LocalizationManager.T(
                $"<color=#FFD54F>{senderName}</color> đang mời bạn cùng xuất trận Co-op tại Phòng <color=#00E5FF>#{roomPin}</color>!\nBạn có muốn tham gia ngay không?",
                $"<color=#FFD54F>{senderName}</color> invited you to Co-op Room <color=#00E5FF>#{roomPin}</color>!\nDo you want to join now?");
        }
        if (_roomInviteDialogObj != null)
        {
            if (_modalRoot != null && !_modalRoot.activeSelf) _modalRoot.SetActive(true);
            _roomInviteDialogObj.SetActive(true);
            _roomInviteDialogObj.transform.SetAsLastSibling();
        }
    }

    public static void ShowRoomInviteDialog(string senderName, string roomPin)
    {
        var lobby = EnsureInstance();
        if (lobby != null)
        {
            lobby.OpenRoomInvitePopup(senderName, roomPin);
        }
    }

    /// <summary>
    /// Chấp nhận lời mời phòng: Tự động mở Sảnh Co-op và kết nối thẳng vào phòng chứa mã PIN.
    /// </summary>
    public static void AcceptRoomInviteAndJoin(string roomPin)
    {
        if (string.IsNullOrEmpty(roomPin)) return;
        var lobby = EnsureInstance();
        if (lobby != null)
        {
            OpenCoopLobbyFromMainMenu();
            if (lobby._headerPinInput != null) lobby._headerPinInput.text = roomPin;
            lobby.ShowLobbyToast(LocalizationManager.T($"ĐANG THAM GIA PHÒNG #{roomPin}...", $"JOINING ROOM #{roomPin}..."), new Color(0.45f, 0.92f, 1f));
            PvPNetworkManager.EnsureInstance().JoinRoomWithPin(roomPin, (success, errorMsg) =>
            {
                if (success)
                {
                    lobby.ShowStep(LobbyStep.Step5_WaitingRoom);
                    lobby.RefreshStep5WaitingRoomUI();
                    FriendManager.EnsureInstance()?.RemoveRoomInvite(roomPin);
                }
                else
                {
                    lobby.ShowLobbyToast(LocalizationManager.T($"LỖI VÀO PHÒNG: {errorMsg}", $"JOIN ERROR: {errorMsg}"), new Color(1f, 0.45f, 0.45f));
                }
            });
        }
    }

    private void OpenEditNicknameModal(string friendId)
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        _editingFriendId = friendId;
        var f = FriendManager.EnsureInstance().GetFriend(friendId);
        if (_editNicknameInput != null)
        {
            _editNicknameInput.text = f != null ? f.nickname : "";
        }
        if (_editNicknameModalObj != null)
        {
            _editNicknameModalObj.SetActive(true);
            _editNicknameModalObj.transform.SetAsLastSibling();
        }
    }

    private void OnConfirmEditNickname()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        if (string.IsNullOrEmpty(_editingFriendId)) return;

        string newNick = _editNicknameInput != null ? _editNicknameInput.text.Trim() : "";
        FriendManager.EnsureInstance().EditNickname(_editingFriendId, newNick);

        var f = FriendManager.EnsureInstance().GetFriend(_editingFriendId);
        string formatted = f != null ? f.GetFormattedDisplayName() : "";

        ShowLobbyToast(LocalizationManager.T($"ĐÃ CẬP NHẬT TÊN GHI NHỚ: {formatted}!", $"UPDATED NICKNAME: {formatted}!"), new Color(0.35f, 1f, 0.55f));

        if (_editNicknameModalObj != null) _editNicknameModalObj.SetActive(false);
        RefreshFriendsListUI();
    }

    public void RefreshStep5WaitingRoomUI()
    {
        var net = PvPNetworkManager.EnsureInstance();
        string pin = string.IsNullOrEmpty(net.CurrentRoomCode) ? _pendingCustomPin : net.CurrentRoomCode;
        int count = Mathf.Clamp(net.GetConnectedPlayerCount(), 1, 4);
        bool hasAtLeastTwo = count >= 2 || net.HasOpponentJoined;
        bool isHost = net.IsHost || net.GetConnectedPlayerCount() <= 1;

        if (_roomHeaderTitleText != null)
            _roomHeaderTitleText.text = LocalizationManager.T($"Mã: #{pin}", $"PIN: #{pin}");
        if (_roomPlayerCountText != null)
            _roomPlayerCountText.text = LocalizationManager.T($"Đồng đội: {count}/4", $"Teammates: {count}/4");

        string myName = GetLocalPlayerDisplayName();
        string myHeroId = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        string normHero = CharacterManager.NormalizeHeroId(myHeroId);
        int myLevel = AccountDatabase.GetHeroLevel(normHero);
        if (myLevel <= 0) myLevel = 1;

        // 1. Slot 0: Chủ Phòng (Nếu isHost thì là Mình, ngược lại là Đối Thủ Host)
        string slot0Name = isHost ? myName : net.OpponentName;
        string slot0Hero = isHost ? myHeroId : net.OpponentHeroId;
        int slot0Lvl = isHost ? myLevel : (net.OpponentLevel > 0 ? net.OpponentLevel : 1);

        if (_coopCardSlots[0].avatarImg != null)
        {
            _coopCardSlots[0].avatarImg.gameObject.SetActive(true);
            _coopCardSlots[0].avatarImg.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(slot0Hero);
        }
        if (_coopCardSlots[0].nameText != null) _coopCardSlots[0].nameText.text = slot0Name;
        if (_coopCardSlots[0].heroText != null) _coopCardSlots[0].heroText.text = $"Lv. {slot0Lvl}";
        if (_coopCardSlots[0].onlineDotObj != null) _coopCardSlots[0].onlineDotObj.SetActive(true);
        if (_coopCardSlots[0].crownBadgeObj != null) _coopCardSlots[0].crownBadgeObj.SetActive(true);
        // Tắt hoàn toàn roleTitleText của Slot 0 để KHÔNG đè lên CrownBadge màu vàng
        if (_coopCardSlots[0].roleTitleText != null)
        {
            _coopCardSlots[0].roleTitleText.gameObject.SetActive(false);
        }
        if (_coopCardSlots[0].changeHeroBtnObj != null) _coopCardSlots[0].changeHeroBtnObj.SetActive(isHost);
        if (_coopCardSlots[0].kickBtnObj != null) _coopCardSlots[0].kickBtnObj.SetActive(false);
        if (_coopCardSlots[0].addFriendBtnObj != null) _coopCardSlots[0].addFriendBtnObj.SetActive(false);
        if (_coopCardSlots[0].readyPillObj != null)
        {
            _coopCardSlots[0].readyPillObj.SetActive(true);
            if (_coopCardSlots[0].readyText != null)
            {
                _coopCardSlots[0].readyText.text = LocalizationManager.T("ĐÃ SẴN SÀNG", "READY");
                _coopCardSlots[0].readyText.color = new Color(0.35f, 1f, 0.55f);
            }
            if (_coopCardSlots[0].readyPillBg != null)
            {
                _coopCardSlots[0].readyPillBg.color = new Color(0.12f, 0.35f, 0.22f, 1f);
            }
        }

        // 2. Slot 1: Khách / Đồng Đội (Nếu isHost thì là Đối Thủ, ngược lại là Mình)
        if (hasAtLeastTwo)
        {
            string slot1Name = isHost ? net.OpponentName : myName;
            string slot1Hero = isHost ? net.OpponentHeroId : myHeroId;
            int slot1Lvl = isHost ? (net.OpponentLevel > 0 ? net.OpponentLevel : 1) : myLevel;

            if (_coopCardSlots[1].silhouetteObj != null) _coopCardSlots[1].silhouetteObj.SetActive(false);
            if (_coopCardSlots[1].plusBtnObj != null) _coopCardSlots[1].plusBtnObj.SetActive(false);
            if (_coopCardSlots[1].invitePromptObj != null) _coopCardSlots[1].invitePromptObj.SetActive(false);

            if (_coopCardSlots[1].avatarImg != null)
            {
                _coopCardSlots[1].avatarImg.gameObject.SetActive(true);
                _coopCardSlots[1].avatarImg.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(slot1Hero);
            }
            if (_coopCardSlots[1].onlineDotObj != null) _coopCardSlots[1].onlineDotObj.SetActive(true);
            if (_coopCardSlots[1].nameText != null)
            {
                _coopCardSlots[1].nameText.gameObject.SetActive(true);
                _coopCardSlots[1].nameText.text = slot1Name;
            }
            if (_coopCardSlots[1].heroText != null)
            {
                _coopCardSlots[1].heroText.gameObject.SetActive(true);
                _coopCardSlots[1].heroText.text = $"Lv. {slot1Lvl}";
            }
            if (_coopCardSlots[1].roleTitleText != null)
            {
                _coopCardSlots[1].roleTitleText.gameObject.SetActive(true);
                _coopCardSlots[1].roleTitleText.text = isHost ? LocalizationManager.T("THÀNH VIÊN", "MEMBER") : LocalizationManager.T("BẢN THÂN", "YOU");
                _coopCardSlots[1].roleTitleText.color = new Color(0.65f, 0.70f, 0.82f);
            }
            if (_coopCardSlots[1].changeHeroBtnObj != null)
            {
                _coopCardSlots[1].changeHeroBtnObj.SetActive(!isHost);
            }
            // Nút Kích: Chỉ hiện cho Chủ phòng
            if (_coopCardSlots[1].kickBtnObj != null)
            {
                _coopCardSlots[1].kickBtnObj.SetActive(isHost);
            }
            // Nút Kết bạn: Chỉ hiện cho Chủ phòng nếu người chơi này chưa là bạn bè
            bool isSlot1Friend = FriendManager.EnsureInstance()?.IsFriend(slot1Name) ?? false;
            if (_coopCardSlots[1].addFriendBtnObj != null)
            {
                _coopCardSlots[1].addFriendBtnObj.SetActive(isHost && !isSlot1Friend);
            }
            if (_coopCardSlots[1].readyPillObj != null)
            {
                _coopCardSlots[1].readyPillObj.SetActive(true);
                bool slot1Ready = isHost ? net.IsOpponentReady : net.IsLocalClientReady;
                if (_coopCardSlots[1].readyText != null)
                {
                    _coopCardSlots[1].readyText.text = slot1Ready
                        ? LocalizationManager.T("SẴN SÀNG", "READY")
                        : LocalizationManager.T("CHƯA SẴN SÀNG", "NOT READY");
                    _coopCardSlots[1].readyText.color = slot1Ready ? new Color(0.35f, 1f, 0.55f) : new Color(1f, 0.85f, 0.35f);
                }
                if (_coopCardSlots[1].readyPillBg != null)
                {
                    _coopCardSlots[1].readyPillBg.color = slot1Ready
                        ? new Color(0.12f, 0.35f, 0.22f, 1f)
                        : new Color(0.35f, 0.30f, 0.12f, 1f);
                }
            }
        }
        else
        {
            ShowEmptySlotCard(1);
        }

        // 3. Slot 2 & Slot 3 (Đồng Đội 3 & 4)
        for (int i = 2; i < 4; i++)
        {
            bool hasTeammate = count > i;
            if (hasTeammate)
            {
                if (_coopCardSlots[i].silhouetteObj != null) _coopCardSlots[i].silhouetteObj.SetActive(false);
                if (_coopCardSlots[i].plusBtnObj != null) _coopCardSlots[i].plusBtnObj.SetActive(false);
                if (_coopCardSlots[i].invitePromptObj != null) _coopCardSlots[i].invitePromptObj.SetActive(false);
                if (_coopCardSlots[i].changeHeroBtnObj != null) _coopCardSlots[i].changeHeroBtnObj.SetActive(false);

                if (_coopCardSlots[i].avatarImg != null)
                {
                    _coopCardSlots[i].avatarImg.gameObject.SetActive(true);
                    _coopCardSlots[i].avatarImg.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(CharacterManager.CHAR_GALE_RONIN);
                }
                if (_coopCardSlots[i].onlineDotObj != null) _coopCardSlots[i].onlineDotObj.SetActive(true);
                if (_coopCardSlots[i].nameText != null)
                {
                    _coopCardSlots[i].nameText.gameObject.SetActive(true);
                    _coopCardSlots[i].nameText.text = $"Hiệp Sĩ {i + 1}";
                }
                if (_coopCardSlots[i].heroText != null)
                {
                    _coopCardSlots[i].heroText.gameObject.SetActive(true);
                    _coopCardSlots[i].heroText.text = "Lv. 28";
                }
                if (_coopCardSlots[i].readyPillObj != null)
                {
                    _coopCardSlots[i].readyPillObj.SetActive(true);
                    if (_coopCardSlots[i].readyText != null)
                    {
                        _coopCardSlots[i].readyText.text = LocalizationManager.T("SẴN SÀNG", "READY");
                        _coopCardSlots[i].readyText.color = new Color(0.35f, 1f, 0.55f);
                    }
                }
            }
            else
            {
                ShowEmptySlotCard(i);
            }
        }

        // 4. ĐIỀU KHIỂN NÚT HÀNH ĐỘNG DỰA TRÊN VAI TRÒ (HOST vs CLIENT)
        if (isHost)
        {
            // CHỦ PHÒNG: Chỉ hiển thị nút "BẮT ĐẦU", Ẩn nút "SẴN SÀNG"
            if (_roomClientReadyBtn != null) _roomClientReadyBtn.gameObject.SetActive(false);
            if (_roomStartMatchBtn != null)
            {
                _roomStartMatchBtn.gameObject.SetActive(true);
                _roomStartMatchBtn.transform.SetAsLastSibling();
            }

            if (count == 1)
            {
                // Solo 1 người: Bấm Bắt đầu ngay
                if (_roomStartMatchBtn != null) _roomStartMatchBtn.interactable = true;
                if (_roomStartMatchBtnImg != null) _roomStartMatchBtnImg.color = new Color(0.18f, 0.65f, 0.32f, 1f);
                if (_roomStartMatchBtnText != null)
                {
                    _roomStartMatchBtnText.text = LocalizationManager.T("BẮT ĐẦU (1 NGƯỜI) >", "START SOLO >");
                    _roomStartMatchBtnText.color = Color.white;
                }
                if (_roomStatusFooterText != null)
                {
                    _roomStatusFooterText.text = LocalizationManager.T("Có thể xuất trận 1 mình hoặc chờ đồng đội nhập mã PIN.", "You can launch solo or wait for teammates to join via PIN.");
                }
            }
            else
            {
                // Có đồng đội: Chỉ bắt đầu được khi đồng đội đã sẵn sàng
                bool allReady = net.IsOpponentReady;
                if (_roomStartMatchBtn != null) _roomStartMatchBtn.interactable = allReady;
                if (_roomStartMatchBtnImg != null)
                {
                    _roomStartMatchBtnImg.color = allReady
                        ? new Color(0.18f, 0.72f, 0.35f, 1f)
                        : new Color(0.35f, 0.35f, 0.42f, 1f);
                }
                if (_roomStartMatchBtnText != null)
                {
                    _roomStartMatchBtnText.text = allReady
                        ? LocalizationManager.T("BẮT ĐẦU TRẬN >", "START MATCH >")
                        : LocalizationManager.T("CHỜ ĐỒNG ĐỘI SẴN SÀNG...", "WAITING FOR READY...");
                    _roomStartMatchBtnText.color = allReady ? Color.white : new Color(0.70f, 0.70f, 0.75f);
                }
                if (_roomStatusFooterText != null)
                {
                    _roomStatusFooterText.text = allReady
                        ? LocalizationManager.T("Tất cả đồng đội đã sẵn sàng! Chủ phòng có thể xuất phát.", "All teammates ready! Host can start.")
                        : LocalizationManager.T("Đang chờ đồng đội bấm 'SẴN SÀNG'...", "Waiting for teammates to press 'READY'...");
                }
            }
        }
        else
        {
            // KHÁCH (CLIENT): Chỉ hiển thị nút "SẴN SÀNG", Ẩn nút "BẮT ĐẦU"
            if (_roomStartMatchBtn != null) _roomStartMatchBtn.gameObject.SetActive(false);
            if (_roomClientReadyBtn != null)
            {
                _roomClientReadyBtn.gameObject.SetActive(true);
                bool myReady = net.IsLocalClientReady;
                if (_roomClientReadyBtnImg != null)
                {
                    _roomClientReadyBtnImg.color = myReady
                        ? new Color(0.18f, 0.65f, 0.32f, 1f)
                        : new Color(0.98f, 0.76f, 0.18f, 1f);
                }
                if (_roomClientReadyBtnText != null)
                {
                    _roomClientReadyBtnText.text = myReady
                        ? LocalizationManager.T("ĐÃ SẴN SÀNG (HỦY)", "READY (CANCEL)")
                        : LocalizationManager.T("SẴN SÀNG", "READY");
                    _roomClientReadyBtnText.color = myReady ? Color.white : new Color(0.22f, 0.14f, 0.02f);
                }
            }
            if (_roomStatusFooterText != null)
            {
                _roomStatusFooterText.text = net.IsLocalClientReady
                    ? LocalizationManager.T("Bạn đã sẵn sàng! Đang chờ chủ phòng xuất trận...", "You are ready! Waiting for host to launch...")
                    : LocalizationManager.T("Hãy bấm 'SẴN SÀNG' để chủ phòng có thể bắt đầu trận đấu.", "Press 'READY' so host can start the match.");
            }
        }
    }

    private void ShowEmptySlotCard(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _coopCardSlots.Length) return;
        var s = _coopCardSlots[slotIndex];
        if (s.silhouetteObj != null) s.silhouetteObj.SetActive(true);
        if (s.plusBtnObj != null) s.plusBtnObj.SetActive(true);
        if (s.invitePromptObj != null) s.invitePromptObj.SetActive(true);
        if (s.avatarImg != null) s.avatarImg.gameObject.SetActive(false);
        if (s.onlineDotObj != null) s.onlineDotObj.SetActive(false);
        if (s.nameText != null) s.nameText.gameObject.SetActive(false);
        if (s.heroText != null) s.heroText.gameObject.SetActive(false);
        if (s.readyPillObj != null) s.readyPillObj.SetActive(false);
        if (s.changeHeroBtnObj != null) s.changeHeroBtnObj.SetActive(false);
        if (s.kickBtnObj != null) s.kickBtnObj.SetActive(false);
        if (s.addFriendBtnObj != null) s.addFriendBtnObj.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // BƯỚC 6: ĐANG GHÉP TRẬN (QUICK MATCH SEARCHING)
    // -------------------------------------------------------------------------
    private void BuildStep6SearchingPanel(Transform root)
    {
        _step6SearchingPanel = CreateFrameWindow(root, "Step6_SearchingPanel",
            LocalizationManager.T("6. ĐANG GHÉP TRẬN (QUICK MATCH)", "6. MATCHMAKING (QUICK MATCH)"),
            CancelQuickMatchToStep2);

        GameObject radarBox = CreateBox(_step6SearchingPanel.transform, "RadarContainer", new Vector2(0f, 35f), new Vector2(820f, 440f), new Color(0.07f, 0.09f, 0.15f, 0.96f));

        // Vòng tròn Radar xoay
        GameObject outerRing = new GameObject("RadarOuterRing", typeof(RectTransform), typeof(Image));
        outerRing.transform.SetParent(radarBox.transform, false);
        RectTransform outRt = outerRing.GetComponent<RectTransform>();
        outRt.anchoredPosition = new Vector2(0f, 35f);
        outRt.sizeDelta = new Vector2(210f, 210f);
        Image outImg = outerRing.GetComponent<Image>();
        outImg.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        outImg.color = new Color(0.20f, 0.65f, 1f, 0.22f);

        GameObject pulseRing = new GameObject("RadarRotatingBar", typeof(RectTransform), typeof(Image));
        pulseRing.transform.SetParent(outerRing.transform, false);
        _searchRadarRingRt = pulseRing.GetComponent<RectTransform>();
        _searchRadarRingRt.sizeDelta = new Vector2(160f, 24f);
        Image pulseImg = pulseRing.GetComponent<Image>();
        pulseImg.color = new Color(0.35f, 0.85f, 1f, 0.65f);

        CreateText(outerRing.transform, "1V1", 42f, Vector2.zero, new Vector2(140f, 60f), new Color(1f, 0.88f, 0.32f));

        CreateText(radarBox.transform,
            LocalizationManager.T("ĐANG TÌM ĐỐI THỦ TRỰC TUYẾN...", "SEARCHING FOR OPPONENT..."),
            32f, new Vector2(0f, -105f), new Vector2(760f, 48f), Color.white);

        _searchTimerText = CreateText(radarBox.transform,
            "THỜI GIAN TÌM: 00:00   |   ƯỚC TÍNH: 00:15",
            22f, new Vector2(0f, -155f), new Vector2(740f, 36f), new Color(1f, 0.86f, 0.35f));

        _searchStatusSubText = CreateText(radarBox.transform,
            LocalizationManager.T("MÁY CHỦ PHOTON ASIA - ĐANG QUÉT PHÒNG...", "PHOTON ASIA SERVER - SCANNING..."),
            18f, new Vector2(0f, -195f), new Vector2(760f, 32f), new Color(0.45f, 0.92f, 1f));

        // Nút Hủy Tìm Trận (Căn giữa rộng rãi, rõ ràng)
        CreateButton(_step6SearchingPanel.transform, "Btn_CancelSearch",
            LocalizationManager.T("HỦY TÌM TRẬN", "CANCEL SEARCH"),
            new Vector2(0f, -285f), new Vector2(460f, 76f),
            new Color(0.55f, 0.16f, 0.18f, 1f), 25f, CancelQuickMatchToStep2);
    }

    private void CancelQuickMatchToStep2()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();
        PvPNetworkManager.Instance?.ShutdownRunnerOnly();
        ShowStep(LobbyStep.Step3_HeroSelect);
    }

    // -------------------------------------------------------------------------
    // BƯỚC 7: GHÉP THÀNH CÔNG - MÀN HÌNH VS (ĐÃ TÌM THẤY ĐỐI THỦ!)
    // -------------------------------------------------------------------------
    private void BuildStep7VsMatchFoundPanel(Transform root)
    {
        _step7VsPanel = CreateFrameWindow(root, "Step7_VsMatchFoundPanel",
            LocalizationManager.T("7. ĐÃ TÌM THẤY ĐỐI THỦ!", "7. OPPONENT FOUND!"),
            null);

        // Khung Chiến Binh Trái (BẠN)
        GameObject leftFighterBox = CreateBox(_step7VsPanel.transform, "VsLeftFighter", new Vector2(-340f, 20f), new Vector2(520f, 490f), new Color(0.08f, 0.12f, 0.22f, 0.98f));
        _vsLeftHeroImg = CreateHeroPortraitImage(leftFighterBox.transform, "VsLeftSprite", new Vector2(0f, 55f), new Vector2(260f, 260f), false);
        _vsLeftNameText = CreateText(leftFighterBox.transform, GetLocalPlayerDisplayName(), 32f, new Vector2(0f, -115f), new Vector2(480f, 44f), new Color(0.45f, 1f, 0.65f));
        _vsLeftHeroTitleText = CreateText(leftFighterBox.transform, "", 20f, new Vector2(0f, -165f), new Vector2(480f, 34f), new Color(0.85f, 0.92f, 1f));

        // Biểu tượng VS khổng lồ ở giữa
        GameObject vsCenterBadge = CreateBox(_step7VsPanel.transform, "VsCenterBadge", new Vector2(0f, 20f), new Vector2(130f, 130f), new Color(0.16f, 0.12f, 0.06f, 0.98f));
        CreateText(vsCenterBadge.transform, "VS", 68f, Vector2.zero, new Vector2(140f, 90f), new Color(1f, 0.85f, 0.22f));

        // Khung Chiến Binh Phải (ĐỐI THỦ)
        GameObject rightFighterBox = CreateBox(_step7VsPanel.transform, "VsRightFighter", new Vector2(340f, 20f), new Vector2(520f, 490f), new Color(0.20f, 0.08f, 0.12f, 0.98f));
        _vsRightHeroImg = CreateHeroPortraitImage(rightFighterBox.transform, "VsRightSprite", new Vector2(0f, 55f), new Vector2(260f, 260f), true);
        _vsRightNameText = CreateText(rightFighterBox.transform, LocalizationManager.T("ĐỐI THỦ", "OPPONENT"), 32f, new Vector2(0f, -115f), new Vector2(480f, 44f), new Color(1f, 0.45f, 0.45f));
        _vsRightHeroTitleText = CreateText(rightFighterBox.transform, "", 20f, new Vector2(0f, -165f), new Vector2(480f, 34f), new Color(1f, 0.85f, 0.85f));

        // Đếm ngược "Chuẩn bị... 3" dưới đáy
        _vsCountdownText = CreateText(_step7VsPanel.transform,
            LocalizationManager.T("CHUẨN BỊ VÀO TRẬN... 3", "PREPARING FOR BATTLE... 3"),
            36f, new Vector2(0f, -295f), new Vector2(820f, 60f), new Color(1f, 0.88f, 0.30f));
    }

    private void RefreshStep7VsScreenUI()
    {
        var charMgr = CharacterManager.EnsureInstance();
        var net = PvPNetworkManager.EnsureInstance();

        string myName = GetLocalPlayerDisplayName();
        string myHeroId = charMgr.GetSelectedCharacterId();
        string normHero = CharacterManager.NormalizeHeroId(myHeroId);
        int myHeroLv = AccountDatabase.GetHeroLevel(normHero);
        if (myHeroLv <= 0) myHeroLv = 1;

        var acc = AccountDatabase.GetActiveAccount();
        int rating = acc != null && acc.pvpRating > 0 ? acc.pvpRating : 1000;
        string rankTier = PlayFabAuthLoginUI.GetRankTierName(rating);

        var myData = charMgr.GetCharacter(myHeroId);
        string myHeroName = myData != null ? (LocalizationManager.IsVietnamese ? myData.nameVN : myData.nameEN) : myHeroId;

        if (_vsLeftHeroImg != null)
            _vsLeftHeroImg.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(myHeroId);
        if (_vsLeftNameText != null)
            _vsLeftNameText.text = myName;
        if (_vsLeftHeroTitleText != null)
            _vsLeftHeroTitleText.text = $"{myHeroName} (Lv.{myHeroLv}) • {rankTier}";

        if (!net.HasOpponentJoined && !net.IsBotPracticeMode)
        {
            if (_vsRightHeroImg != null)
                _vsRightHeroImg.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(myHeroId);
            if (_vsRightNameText != null)
                _vsRightNameText.text = LocalizationManager.T("CHƠI ĐƠN HẦM NGỤC", "SOLO EXPEDITION");
            if (_vsRightHeroTitleText != null)
                _vsRightHeroTitleText.text = LocalizationManager.T("Khám phá hầm ngục một mình (1/4)", "Explore dungeon alone (1/4)");
        }
        else
        {
            string oppHeroId = net.OpponentHeroId;
            var oppData = charMgr.GetCharacter(oppHeroId);
            string oppHeroName = oppData != null ? (LocalizationManager.IsVietnamese ? oppData.nameVN : oppData.nameEN) : oppHeroId;
            int oppHeroLv = net.OpponentLevel > 0 ? net.OpponentLevel : 1;

            if (_vsRightHeroImg != null)
                _vsRightHeroImg.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(oppHeroId);
            if (_vsRightNameText != null)
                _vsRightNameText.text = net.OpponentName;
            if (_vsRightHeroTitleText != null)
                _vsRightHeroTitleText.text = $"{oppHeroName} (Lv.{oppHeroLv}) • {rankTier}";
        }
    }

    // =========================================================================
    // CÁC HÀM TIỆN ÍCH XÂY DỰNG UI CHUẨN ZERO-TOFU
    // =========================================================================
    private string GetLocalPlayerDisplayName()
    {
        var acc = AccountDatabase.GetActiveAccount();
        string myName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName :
                        PlayerPrefs.GetString("Player_Name", "");
        if (string.IsNullOrEmpty(myName) || myName == "Player" || myName == "Wanderer")
        {
            myName = acc != null && !string.IsNullOrEmpty(acc.username) ? acc.username : "Chiến Binh";
        }
        return myName;
    }

    private GameObject CreateFrameWindow(Transform root, string name, string headerTitle, UnityEngine.Events.UnityAction onBackClick)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root, false);
        RectTransform pRt = panel.GetComponent<RectTransform>();
        pRt.sizeDelta = new Vector2(1380f, 840f);
        panel.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.10f, 0.98f);

        // Thanh Header trên đỉnh
        GameObject headerBar = CreateBox(panel.transform, "HeaderBar", new Vector2(0f, 368f), new Vector2(1340f, 76f), new Color(0.09f, 0.11f, 0.18f, 1f));
        CreateText(headerBar.transform, headerTitle, 30f, new Vector2(-40f, 0f), new Vector2(780f, 54f), new Color(1f, 0.86f, 0.32f));

        if (onBackClick != null)
        {
            CreateButton(headerBar.transform, "Btn_Back",
                LocalizationManager.T("< QUAY LẠI", "< BACK"),
                new Vector2(-565f, 0f), new Vector2(170f, 54f),
                new Color(0.22f, 0.26f, 0.38f, 1f), 20f, onBackClick);
        }

        return panel;
    }

    private static Sprite _cachedRoundedBoxSprite;
    private static Sprite _cachedBorderBoxSprite;
    private static Sprite _cachedCircleSprite;

    public static Sprite GetRoundedBoxSprite()
    {
        if (_cachedRoundedBoxSprite != null) return _cachedRoundedBoxSprite;
        int size = 64;
        int radius = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        _cachedRoundedBoxSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return _cachedRoundedBoxSprite;
    }

    public static Sprite GetBorderBoxSprite()
    {
        if (_cachedBorderBoxSprite != null) return _cachedBorderBoxSprite;
        int size = 64;
        int radius = 16;
        int border = 4;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float outerAlpha = Mathf.Clamp01(radius + 0.5f - dist);

                int idx = Mathf.Max(0, Mathf.Max(radius - border - x, x - (size - 1 - (radius - border))));
                int idy = Mathf.Max(0, Mathf.Max(radius - border - y, y - (size - 1 - (radius - border))));
                float innerDist = Mathf.Sqrt(idx * idx + idy * idy);
                float innerAlpha = (x >= border && x < size - border && y >= border && y < size - border)
                    ? Mathf.Clamp01((radius - border) + 0.5f - innerDist)
                    : 0f;

                float borderAlpha = Mathf.Clamp01(outerAlpha - innerAlpha);
                cols[y * size + x] = new Color(1f, 1f, 1f, borderAlpha);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        _cachedBorderBoxSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return _cachedBorderBoxSprite;
    }

    public static Sprite GetCircleSprite()
    {
        if (_cachedCircleSprite != null) return _cachedCircleSprite;
        int size = 64;
        float r = 30f;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[size * size];
        Vector2 center = new Vector2(31.5f, 31.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(r + 0.5f - d);
                cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        _cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _cachedCircleSprite;
    }

    private GameObject CreateRoundedBox(Transform parent, string name, Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject box = new GameObject(name, typeof(RectTransform), typeof(Image));
        box.transform.SetParent(parent, false);
        RectTransform rt = box.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = box.GetComponent<Image>();
        img.sprite = GetRoundedBoxSprite();
        img.type = Image.Type.Sliced;
        img.color = bgColor;
        return box;
    }

    private GameObject CreateBorderBox(Transform parent, string name, Vector2 pos, Vector2 size, Color borderColor)
    {
        GameObject box = new GameObject(name, typeof(RectTransform), typeof(Image));
        box.transform.SetParent(parent, false);
        RectTransform rt = box.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = box.GetComponent<Image>();
        img.sprite = GetBorderBoxSprite();
        img.type = Image.Type.Sliced;
        img.color = borderColor;
        img.raycastTarget = false;
        return box;
    }

    private GameObject CreateCircle(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.sprite = GetCircleSprite();
        img.color = color;
        return go;
    }

    private GameObject CreateRoundedButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor, float fontSize, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.sprite = GetRoundedBoxSprite();
        img.type = Image.Type.Sliced;
        img.color = bgColor;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        CreateText(btnObj.transform, label, fontSize, Vector2.zero, size - new Vector2(16f, 8f), Color.white);
        return btnObj;
    }

    private GameObject CreateBox(Transform parent, string name, Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject box = new GameObject(name, typeof(RectTransform), typeof(Image));
        box.transform.SetParent(parent, false);
        RectTransform rt = box.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        box.GetComponent<Image>().color = bgColor;
        return box;
    }

    private Image CreateHeroPortraitImage(Transform parent, string name, Vector2 pos, Vector2 size, bool flipX)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = new Vector3(flipX ? -1f : 1f, 1f, 1f);
        Image img = go.GetComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private void BuildPinInputField(Transform parent, Vector2 pos)
    {
        GameObject inputObj = new GameObject("PinInputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputObj.transform.SetParent(parent, false);
        RectTransform rt = inputObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(460f, 68f);
        inputObj.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 1f);

        GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform taRt = textArea.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(14f, 6f);
        taRt.offsetMax = new Vector2(-14f, -6f);

        TextMeshProUGUI placeholder = CreateText(textArea.transform,
            LocalizationManager.T("NHẬP MÃ PIN (VD: 4829)...", "ENTER PIN (E.G. 4829)..."),
            24f, Vector2.zero, new Vector2(420f, 52f), new Color(0.5f, 0.55f, 0.65f, 0.65f));

        TextMeshProUGUI inputText = CreateText(textArea.transform, "", 32f, Vector2.zero, new Vector2(420f, 52f), new Color(1f, 0.92f, 0.42f));
        inputText.enableAutoSizing = true;
        inputText.fontSizeMin = 14f;
        inputText.fontSizeMax = 32f;
        inputText.overflowMode = TextOverflowModes.Truncate;

        _pinInputField = inputObj.GetComponent<TMP_InputField>();
        _pinInputField.textViewport = taRt;
        _pinInputField.textComponent = inputText;
        _pinInputField.placeholder = placeholder;
        _pinInputField.characterLimit = 8;
        _pinInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        _pinInputField.onValueChanged.AddListener((val) =>
        {
            _enteredPin = val;
        });
    }

    private void BuildNumericKeypad(Transform parent, Vector2 centerPos)
    {
        GameObject padRoot = new GameObject("NumericKeypad", typeof(RectTransform));
        padRoot.transform.SetParent(parent, false);
        RectTransform rt = padRoot.GetComponent<RectTransform>();
        rt.anchoredPosition = centerPos;
        rt.sizeDelta = new Vector2(540f, 280f);

        string[] keys = new string[]
        {
            "1", "2", "3",
            "4", "5", "6",
            "7", "8", "9",
            "XÓA", "0", "VÀO PHÒNG"
        };

        float cellW = 166f;
        float cellH = 58f;
        float gapX = 12f;
        float gapY = 10f;

        for (int i = 0; i < keys.Length; i++)
        {
            int row = i / 3;
            int col = i % 3;
            float x = (col - 1) * (cellW + gapX);
            float y = (1.5f - row) * (cellH + gapY);

            string k = keys[i];
            Color btnCol = new Color(0.16f, 0.19f, 0.28f, 1f);
            if (k == "XÓA") btnCol = new Color(0.55f, 0.18f, 0.20f, 1f);
            else if (k == "VÀO PHÒNG") btnCol = new Color(0.18f, 0.58f, 0.32f, 1f);

            string label = k;
            if (k == "XÓA") label = LocalizationManager.T("XÓA", "CLEAR");
            else if (k == "VÀO PHÒNG") label = LocalizationManager.T("VÀO PHÒNG", "JOIN");

            CreateButton(padRoot.transform, "Key_" + k, label, new Vector2(x, y), new Vector2(cellW, cellH), btnCol, 22f, () =>
            {
                SoundManager.EnsureInstance()?.PlayButtonClick();
                if (k == "XÓA")
                {
                    _enteredPin = "";
                    if (_pinInputField != null) _pinInputField.text = "";
                }
                else if (k == "VÀO PHÒNG")
                {
                    string pin = (_pinInputField != null && !string.IsNullOrEmpty(_pinInputField.text))
                        ? _pinInputField.text.Trim()
                        : _enteredPin.Trim();
                    if (string.IsNullOrEmpty(pin))
                    {
                        if (_joinPinStatusText != null)
                            _joinPinStatusText.text = LocalizationManager.T("VUI LÒNG NHẬP MÃ PHÒNG 4 SỐ!", "PLEASE ENTER A 4-DIGIT PIN!");
                        return;
                    }

                    if (_joinPinStatusText != null)
                    {
                        _joinPinStatusText.text = LocalizationManager.T($"ĐANG TÌM KIẾM PHÒNG #{pin} TRÊN MÁY CHỦ ASIA...", $"SEARCHING FOR ROOM #{pin} (ASIA)...");
                    }

                    PvPNetworkManager.EnsureInstance().JoinRoomWithPin(pin, (success, errorMsg) =>
                    {
                        if (success)
                        {
                            _pendingCustomPin = pin;
                            ShowStep(LobbyStep.Step5_WaitingRoom);
                        }
                        else
                        {
                            if (_joinPinStatusText != null)
                            {
                                _joinPinStatusText.text = $"<color=#FF5555>{errorMsg}</color>";
                            }
                            SoundManager.EnsureInstance()?.PlayMenuHover();
                        }
                    });
                }
                else
                {
                    if (_enteredPin.Length < 8)
                    {
                        _enteredPin += k;
                        if (_pinInputField != null) _pinInputField.text = _enteredPin;
                    }
                }
            });
        }
    }

    private TextMeshProUGUI CreateText(Transform parent, string content, float fontSize, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 11f;
        tmp.fontSizeMax = fontSize;
        tmp.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(tmp, 0.28f, Color.black);
        return tmp;
    }

    private GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor, float fontSize, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        CreateText(btnObj.transform, label, fontSize, Vector2.zero, size - new Vector2(16f, 8f), Color.white);
        return btnObj;
    }
}
