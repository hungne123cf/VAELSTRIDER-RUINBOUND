using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý kết nối Online PvP 1v1 qua Photon Fusion 2 (Chuẩn Android Zero-Bug Release).
/// - Khóa cứng FixedRegion = "asia" để PC Test và Android APK luôn nhìn thấy phòng của nhau 100%.
/// - Hỗ trợ: Tìm Trận Nhanh (Quick Match), Tạo Phòng Mã PIN 4 số, Vào Phòng bằng Mã PIN, và Đấu Tập AI.
/// - Truyền nhận gói tin thời gian thực qua SendReliableDataToPlayer / OnReliableDataReceived (Không phụ thuộc NetworkPrefab baking).
/// </summary>
public class PvPNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static PvPNetworkManager Instance { get; private set; }

    public enum PvPRoundFormat
    {
        Bo1_SingleRound = 0, // 1 Hiệp sinh tử (1 Round KO)
        Bo3_BestOf3 = 1,     // Chạm 2 thắng (Bo3 - Đấu tối đa 3 hiệp)
        Bo5_BestOf5 = 2      // Chạm 3 thắng (Bo5 - Đấu tối đa 5 hiệp)
    }

    public static PvPRoundFormat SelectedRoundFormat { get; set; } = PvPRoundFormat.Bo3_BestOf3;

    public static int GetRequiredWins(PvPRoundFormat format)
    {
        switch (format)
        {
            case PvPRoundFormat.Bo1_SingleRound: return 1;
            case PvPRoundFormat.Bo3_BestOf3: return 2;
            case PvPRoundFormat.Bo5_BestOf5: return 3;
            default: return 2;
        }
    }

    public static int GetMaxRounds(PvPRoundFormat format)
    {
        switch (format)
        {
            case PvPRoundFormat.Bo1_SingleRound: return 1;
            case PvPRoundFormat.Bo3_BestOf3: return 3;
            case PvPRoundFormat.Bo5_BestOf5: return 5;
            default: return 3;
        }
    }

    public const string DEFAULT_FUSION_APP_ID = "aebb059d-f96c-4dbf-85d0-b61543ad1be7";
    public const string FIXED_REGION = "asia";
    public const string QUICK_MATCH_SESSION = "VAEL_PVP_QUICK_V1";
    public const string ROOM_PREFIX = "VAEL_PVP_PIN_";

    private NetworkRunner _runner;
    public NetworkRunner Runner => _runner;

    private int _packetSeq = 1;
    private uint _localStateSeq = 0;
    private uint _lastReceivedStateSeq = 0;
    private PlayerRef _remotePlayerRef = PlayerRef.None;
    private float _handshakeTimer = 0f;
    private bool _hasTriggeredArenaLoad = false;

    public bool IsBotPracticeMode { get; private set; } = false;
    public bool IsCustomRoomSession { get; private set; } = false;
    public bool IsConnectingOrConnected => (_runner != null && _runner.IsRunning) || _isStartingGame;
    public bool IsRunning => _runner != null && _runner.IsRunning;
    private bool _isStartingGame = false;

    public string CurrentRoomCode { get; private set; } = "";
    public string StatusMessage { get; private set; } = "";

    // Thông tin đối thủ đồng bộ qua mạng
    public string OpponentName { get; set; } = "ĐỐI THỦ BÍ ẨN";
    public string OpponentHeroId { get; set; } = CharacterManager.CHAR_VAELSTRIDER;
    public int OpponentLevel { get; set; } = 1;
    public int OpponentMaxHp { get; set; } = 800;
    public int OpponentCurrentHp { get; set; } = 800;
    public bool HasOpponentJoined { get; private set; } = false;

    // Trạng thái Sẵn Sàng (Ready) chuẩn Multiplayer
    public bool IsLocalClientReady { get; private set; } = false;
    public bool IsOpponentReady { get; private set; } = false;
    public event Action OnOpponentReadyChanged;

    // Hệ thống Chat Đội (Lobby & In-Game)
    public event Action<string, string> OnChatMessageReceived;

    public void SendFriendRequestPacket(string targetPlayerName)
    {
        var acc = AccountDatabase.GetActiveAccount();
        string myName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName : "Hiệp Sĩ";
        string myHero = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        int myLv = AccountDatabase.GetHeroLevel(CharacterManager.NormalizeHeroId(myHero));
        SendRawPacket($"FREQ|{targetPlayerName}|{myName}|{myHero}|{myLv}", 1);
    }

    public void SendFriendAcceptPacket(string targetPlayerName)
    {
        var acc = AccountDatabase.GetActiveAccount();
        string myName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName : "Hiệp Sĩ";
        string myHero = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        int myLv = AccountDatabase.GetHeroLevel(CharacterManager.NormalizeHeroId(myHero));
        SendRawPacket($"FACC|{targetPlayerName}|{myName}|{myHero}|{myLv}", 1);
    }

    public void SendRoomInvitePacket(string friendName, string roomPin)
    {
        var acc = AccountDatabase.GetActiveAccount();
        string myName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName : "Hiệp Sĩ";
        SendRawPacket($"INVITE|{friendName}|{myName}|{roomPin}", 1);
    }

    public void SendChatMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        string sanitizedMsg = message.Replace("[", "").Replace("]", "").Trim();
        string myName = LocalizationManager.IsVietnamese ? PlayerPrefs.GetString("Player_Name", "Hiệp Sĩ") : "Knight";
        myName = myName.Replace("[", "").Replace("]", "").Trim();
        SendRawPacket($"CHAT|{myName}|{sanitizedMsg}", 1);
        OnChatMessageReceived?.Invoke(myName, sanitizedMsg);
    }

    public void SendHeroChange(string newHeroId)
    {
        string norm = CharacterManager.NormalizeHeroId(newHeroId);
        SendRawPacket($"HERO|{norm}", 1);
        SendHandshakePacket();
    }

    public void SendReadyPacket(bool isReady)
    {
        IsLocalClientReady = isReady;
        SendRawPacket("READY|" + (isReady ? "1" : "0"), 1);
        OnOpponentReadyChanged?.Invoke();
    }

    public void ResetReadyStates()
    {
        IsLocalClientReady = false;
        IsOpponentReady = false;
        OnOpponentReadyChanged?.Invoke();
    }

    public event Action<string> OnStatusChanged;
    public event Action OnMatchReadyToStart;
    public event Action OnOpponentIdentityUpdated;

    public static PvPNetworkManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = FindAnyObjectByType<PvPNetworkManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }
        GameObject go = new GameObject("PvPNetworkManager");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<PvPNetworkManager>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep; // Ngăn chặn Android tự khóa màn hình hoặc tắt Wi-Fi gây Disconnect 104
    }

    private void Update()
    {
        if (_runner != null && _runner.IsRunning && !IsBotPracticeMode)
        {
            int playerCount = GetConnectedPlayerCount();
            if (playerCount >= 2)
            {
                HasOpponentJoined = true;
                _handshakeTimer -= Time.unscaledDeltaTime;
                if (_handshakeTimer <= 0f)
                {
                    _handshakeTimer = 1.0f;
                    SendHandshakePacket();
                }

                if (!IsCustomRoomSession && !_hasTriggeredArenaLoad && SceneManager.GetActiveScene().name == "MainMenuScene")
                {
                    BeginVsCountdownAndEnterArena();
                }
            }
            else
            {
                HasOpponentJoined = false;
            }
        }
    }

    public void BeginVsCountdownAndEnterArena()
    {
        if (_hasTriggeredArenaLoad) return;
        _hasTriggeredArenaLoad = true;
        EnterPvPArenaScene();
    }

    public void TriggerCustomRoomStart()
    {
        // 1, 2, 3 hoặc 4 người chơi đều có thể xuất trận ngay lập tức (Chuẩn Shape of Dreams)!
        _hasTriggeredArenaLoad = false;
        try
        {
            SendRawPacket("G|START", 7);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[PVP NETWORK] Send start packet warning: " + ex);
        }
        EnterPvPArenaScene();
    }

    private System.Collections.IEnumerator EnterArenaDelayedRoutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        EnterPvPArenaScene();
    }

    public int GetConnectedPlayerCount()
    {
        if (IsBotPracticeMode) return 2;
        if (_runner == null || !_runner.IsRunning) return 0;
        int count = 0;
        foreach (var _ in _runner.ActivePlayers) count++;
        return count;
    }

    /// <summary>
    /// Xác định xem máy hiện tại là Player 1 (bên trái võ đài) hay Player 2 (bên phải võ đài).
    /// Trong chế độ AutoHostOrClient: Máy tạo phòng (Host/Server) luôn là Player 1, máy vào sau (Client) luôn là Player 2.
    /// </summary>
    public bool IsLocalPlayerOne()
    {
        if (IsBotPracticeMode) return true;
        if (_runner == null || !_runner.IsRunning) return true;
        if (_runner.GameMode != GameMode.Shared)
        {
            return _runner.IsServer;
        }

        PlayerRef local = _runner.LocalPlayer;
        foreach (var p in _runner.ActivePlayers)
        {
            if (p != local)
            {
                return local.PlayerId < p.PlayerId;
            }
        }
        return true;
    }

    private void SetStatus(string msg)
    {
        StatusMessage = msg;
        Debug.Log($"<color=cyan>[PVP NETWORK]</color> {msg}");
        OnStatusChanged?.Invoke(msg);
    }

    public bool IsHost => _runner != null && _runner.IsServer;

    public async void StartQuickMatch()
    {
        if (_isStartingGame) return;
        IsBotPracticeMode = false;
        IsCustomRoomSession = false;
        CurrentRoomCode = "QUICK";
        await StartFusionSessionAsync(QUICK_MATCH_SESSION, isQuickMatch: true, mode: GameMode.AutoHostOrClient);
    }

    public async void CreateRoomWithPin(string pinCode = "")
    {
        if (_isStartingGame) return;
        IsBotPracticeMode = false;
        IsCustomRoomSession = true;
        if (string.IsNullOrEmpty(pinCode))
        {
            pinCode = UnityEngine.Random.Range(1000, 9999).ToString();
        }
        CurrentRoomCode = pinCode;
        await StartFusionSessionAsync(ROOM_PREFIX + pinCode, isQuickMatch: false, mode: GameMode.AutoHostOrClient);
    }

    public async void JoinRoomWithPin(string pinCode, Action<bool, string> onComplete = null)
    {
        if (_isStartingGame) return;
        string cleanPin = (pinCode ?? "").Trim();
        if (string.IsNullOrEmpty(cleanPin))
        {
            string err = LocalizationManager.T("VUI LÒNG NHẬP MÃ PHÒNG 4 SỐ!", "PLEASE ENTER A 4-DIGIT ROOM PIN!");
            SetStatus(err);
            onComplete?.Invoke(false, err);
            return;
        }
        IsBotPracticeMode = false;
        IsCustomRoomSession = true;
        CurrentRoomCode = cleanPin;
        bool ok = await StartFusionSessionAsync(ROOM_PREFIX + cleanPin, isQuickMatch: false, mode: GameMode.AutoHostOrClient);
        if (!ok)
        {
            string err = LocalizationManager.T($"PHÒNG #{cleanPin} KHÔNG TỒN TẠI HOẶC KHÔNG THỂ KẾT NỐI!", $"ROOM #{cleanPin} NOT FOUND OR CANNOT CONNECT!");
            onComplete?.Invoke(false, err);
            return;
        }

        // Kiểm tra số lượng người chơi: Nếu phòng đã full 4 người thì không cho connect lại
        int currentPlayers = GetConnectedPlayerCount();
        if (currentPlayers > 4)
        {
            ShutdownRunnerOnly();
            string fullErr = LocalizationManager.T($"PHÒNG #{cleanPin} ĐÃ ĐẦY ĐỦ 4 NGƯỜI! KHÔNG THỂ THAM GIA.", $"ROOM #{cleanPin} IS FULL (4/4 PLAYERS)! CANNOT JOIN.");
            SetStatus(fullErr);
            if (PvPLobbyUI.IsLobbyModalOpen)
            {
                PvPLobbyUI.Instance.ShowLobbyToast(fullErr, new Color(1f, 0.35f, 0.35f));
            }
            onComplete?.Invoke(false, fullErr);
            return;
        }

        // Phòng còn chỗ (< 4 người): Kết nối lại thành công!
        onComplete?.Invoke(true, "");
    }

    public void PrepareBotPracticeMatch()
    {
        ShutdownRunnerOnly();
        IsBotPracticeMode = true;
        IsCustomRoomSession = false;
        HasOpponentJoined = true;
        CurrentRoomCode = "BOT";

        // Chọn ngẫu nhiên 1 trong 7 tướng làm Đối Thủ AI
        string[] allHeroes = new string[]
        {
            CharacterManager.CHAR_VAELSTRIDER,
            CharacterManager.CHAR_GALE_RONIN,
            CharacterManager.CHAR_AZURE_BLADE,
            CharacterManager.CHAR_TITAN_BERSERKER,
            CharacterManager.CHAR_DUNE_ASSASSIN,
            CharacterManager.CHAR_CELESTIAL_MAGE,
            CharacterManager.CHAR_ABYSSAL_MECH
        };
        string localHero = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        List<string> candidates = new List<string>();
        foreach (var h in allHeroes)
        {
            if (h != localHero) candidates.Add(h);
        }
        OpponentHeroId = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        var oppData = CharacterManager.EnsureInstance().GetCharacter(OpponentHeroId);
        string heroName = oppData != null ? (LocalizationManager.IsVietnamese ? oppData.nameVN : oppData.nameEN) : OpponentHeroId;
        OpponentName = LocalizationManager.T($"AI CHIẾN THẦN ({heroName})", $"AI RIVAL ({heroName})");
        OpponentMaxHp = GetNormalizedPvPMaxHealth(OpponentHeroId);
        OpponentCurrentHp = OpponentMaxHp;
        OnOpponentIdentityUpdated?.Invoke();
    }

    public void StartBotPractice()
    {
        PrepareBotPracticeMatch();
        SetStatus(LocalizationManager.T("ĐANG VÀO VÕ ĐÀI ĐẤU TẬP...", "ENTERING PRACTICE ARENA..."));
        EnterPvPArenaScene();
    }

    public static int GetNormalizedPvPMaxHealth(string heroId)
    {
        var charMgr = CharacterManager.EnsureInstance();
        var data = charMgr != null ? charMgr.GetCharacter(heroId) : null;
        int baseHp = data != null && data.baseHealth > 0 ? data.baseHealth : 110;
        // Nhân x8 máu cơ bản trong Đấu Trường PvP (từ ~720 đến 1600 HP) để trận đấu kịch tính 30-60 giây
        return Mathf.Clamp(baseHp * 8, 720, 1800);
    }

    private async Task CleanOldRunnerAsync()
    {
        if (_runner != null)
        {
            var oldRunner = _runner;
            _runner = null;
            try
            {
                oldRunner.RemoveCallbacks(this);
                if (oldRunner.IsRunning)
                {
                    await oldRunner.Shutdown(destroyGameObject: true);
                }
                else if (oldRunner.gameObject != null)
                {
                    Destroy(oldRunner.gameObject);
                }
            }
            catch (Exception)
            {
                if (oldRunner != null && oldRunner.gameObject != null)
                {
                    Destroy(oldRunner.gameObject);
                }
            }

            // Chờ 200ms để hệ điều hành giải phóng hoàn toàn cổng UDP socket & DNS cache
            await Task.Delay(200);
        }
    }

    private async Task<bool> StartFusionSessionAsync(string sessionName, bool isQuickMatch, GameMode mode = GameMode.AutoHostOrClient)
    {
        if (_runner != null && _runner.IsRunning && CurrentRoomCode == sessionName)
        {
            Debug.Log($"[PvP] Already running in session {sessionName}");
            return true;
        }

        _hasTriggeredArenaLoad = false;
        HasOpponentJoined = false;
        Application.runInBackground = true;

        int maxAttempts = 2;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await CleanOldRunnerAsync();

            _isStartingGame = true;

            string attemptStr = attempt > 1 ? $" (Thử lại {attempt}/{maxAttempts})" : "";
            SetStatus(isQuickMatch
                ? LocalizationManager.T($"ĐANG KẾT NỐI MÁY CHỦ PHOTON (ASIA)...{attemptStr}", $"CONNECTING TO PHOTON CLOUD (ASIA)...{attemptStr}")
                : LocalizationManager.T($"ĐANG MỞ PHÒNG #{CurrentRoomCode} (ASIA)...{attemptStr}", $"CONNECTING TO ROOM #{CurrentRoomCode} (ASIA)...{attemptStr}"));

            GameObject runnerObj = new GameObject("PvP_FusionRunner");
            DontDestroyOnLoad(runnerObj);
            _runner = runnerObj.AddComponent<NetworkRunner>();
            var sceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>();
            var objectProvider = runnerObj.AddComponent<NetworkObjectProviderDefault>();
            _runner.ProvideInput = false;
            _runner.AddCallbacks(this);

            // Khóa cứng FixedRegion = "asia" để PC và Android APK luôn gặp nhau 100%
            FusionAppSettings customAppSettings;
            if (PhotonAppSettings.TryGetGlobal(out var globalSettings) && globalSettings != null && globalSettings.AppSettings != null)
            {
                customAppSettings = globalSettings.AppSettings.GetCopy();
            }
            else
            {
                customAppSettings = new FusionAppSettings();
            }
            if (string.IsNullOrEmpty(customAppSettings.AppIdFusion))
            {
                customAppSettings.AppIdFusion = DEFAULT_FUSION_APP_ID;
            }
            customAppSettings.FixedRegion = FIXED_REGION;
            customAppSettings.AppVersion = "1.0";
            customAppSettings.UseNameServer = true;
            customAppSettings.EnableProtocolFallback = true;

            var startArgs = new StartGameArgs
            {
                GameMode = mode,
                SessionName = sessionName,
                PlayerCount = 4, // Hỗ trợ tối đa 4 người chơi cùng vượt ải
                CustomPhotonAppSettings = customAppSettings,
                SceneManager = sceneManager,
                ObjectProvider = objectProvider
            };

            var currentRunner = _runner;
            if (currentRunner == null)
            {
                _isStartingGame = false;
                return false;
            }

            StartGameResult result;
            try
            {
                result = await currentRunner.StartGame(startArgs);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PvP] StartGame exception (lần {attempt}): " + ex.Message);
                if (attempt < maxAttempts)
                {
                    SetStatus(LocalizationManager.T("MẠNG GIÁN ĐOẠN, ĐANG TỰ ĐỘNG THỬ LẠI...", "NETWORK BLIP, RETRYING..."));
                    await Task.Delay(1000);
                    continue;
                }
                _isStartingGame = false;
                ShutdownRunnerOnly();
                SetStatus(LocalizationManager.T("LỖI KẾT NỐI MÁY CHỦ PHOTON (DNS / TIMEOUT). VUI LÒNG THỬ LẠI!", "PHOTON CONNECTION ERROR (DNS / TIMEOUT). PLEASE RETRY!"));
                return false;
            }

            _isStartingGame = false;

            // Nếu Runner đã bị Shutdown hoặc người chơi đã nhấn Hủy/Quay lại trong lúc đang kết nối
            if (currentRunner == null || _runner != currentRunner || !_runner.IsRunning)
            {
                return false;
            }

            if (result.Ok)
            {
                // Reset chuỗi Sequence về 0 đảm bảo phiên đấu mới bắt đầu sạch sẽ, không bị drop nhầm packet
                ResetStateSequences();

                int count = GetConnectedPlayerCount();
                if (count >= 2)
                {
                    SendHandshakePacket();
                    SetStatus(LocalizationManager.T("ĐÃ GHÉP ĐỦ 2 ĐẤU SĨ! ĐANG VÀO VÕ ĐÀI...", "2 FIGHTERS MATCHED! ENTERING ARENA..."));
                }
                else
                {
                    if (isQuickMatch)
                    {
                        SetStatus(LocalizationManager.T("ĐANG CHỜ ĐỐI THỦ VÀO TRẬN (1/2)... (SERVER: ASIA)", "WAITING FOR OPPONENT (1/2)... (SERVER: ASIA)"));
                    }
                    else
                    {
                        SetStatus(LocalizationManager.T($"PHÒNG #{CurrentRoomCode} ĐÃ MỞ! HÃY NHẬP MÃ #{CurrentRoomCode} Ở MÁY KIA (1/2)", $"ROOM #{CurrentRoomCode} READY! ENTER PIN #{CurrentRoomCode} ON OTHER DEVICE (1/2)"));
                    }
                }
                return true;
            }
            else
            {
                // Tự động retry nếu là lỗi DNS lookup hoặc lỗi tạm thời của mạng
                bool isNetworkTemporaryError = result.ErrorMessage != null && 
                    (result.ErrorMessage.Contains("Dns") || result.ErrorMessage.Contains("Timeout") || result.ShutdownReason == ShutdownReason.Error);

                if (isNetworkTemporaryError && attempt < maxAttempts)
                {
                    Debug.LogWarning($"[PvP] StartGame failed: {result.ErrorMessage}, retrying attempt {attempt + 1}...");
                    SetStatus(LocalizationManager.T("KẾT NỐI MẠNG GIÁN ĐOẠN, ĐANG THỬ LẠI KẾT NỐI...", "NETWORK INTERRUPTION, RETRYING CONNECTION..."));
                    await Task.Delay(1200);
                    continue;
                }

                // Nếu người tham gia Client tìm phòng không tồn tại
                if (mode == GameMode.Client)
                {
                    SetStatus(LocalizationManager.T($"PHÒNG #{CurrentRoomCode} KHÔNG TỒN TẠI HOẶC ĐÃ ĐẦY!", $"ROOM #{CurrentRoomCode} NOT FOUND OR FULL!"));
                }
                else if (result.ShutdownReason != ShutdownReason.Ok)
                {
                    SetStatus(LocalizationManager.T(
                        $"LỖI KẾT NỐI MẠNG: {result.ShutdownReason} (KIỂM TRA MẠNG / APP ID)",
                        $"CONNECTION FAILED: {result.ShutdownReason}"));
                }
                ShutdownRunnerOnly();
                return false;
            }
        }

        return false;
    }

    public void EnterPvPArenaScene()
    {
        // Khóa chặt nhân vật đã chọn trước khi chuyển scene, đảm bảo 100% không bị lệch tướng
        string myHero = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        if (!string.IsNullOrEmpty(myHero))
        {
            CharacterManager.EnsureInstance().SelectCharacter(myHero, true);
            AccountDatabase.SetSelectedHero(myHero);
            AccountDatabase.SaveActiveSessionToDatabase();
        }

        PvPMatchManager.IsPvPModeActive = false;
        PvPMatchManager.IsCoopModeActive = true;
        CorruptedCycleManager.ResetToNormalCycle();
        PlayerHealth.ResetRunPreservedHealth();
        MatchTimerUI.ResetRunTimer();
        Time.timeScale = 1.0f;

        PvPLobbyUI.CloseLobbyModal();

        // Kích hoạt Màn Chuyển Cảnh Cõi Giới Sống Động (Realm Transition) chuẩn AAA
        RealmTransitionManager.LoadSceneWithTransition("Map1_Thestart", RealmTransitionManager.TransitionTheme.DeployBattle);
    }

    public void ShutdownRunnerOnly()
    {
        _isStartingGame = false;
        _hasTriggeredArenaLoad = false;
        HasOpponentJoined = false;
        IsLocalClientReady = false;
        IsOpponentReady = false;
        if (_runner != null)
        {
            var r = _runner;
            _runner = null;
            try
            {
                r.RemoveCallbacks(this);
                if (r.IsRunning)
                {
                    r.Shutdown(destroyGameObject: true);
                }
                else if (r.gameObject != null)
                {
                    Destroy(r.gameObject);
                }
            }
            catch (Exception)
            {
                if (r != null && r.gameObject != null)
                {
                    Destroy(r.gameObject);
                }
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            // Khi người chơi focus lại vào game sau khi tab ra ngoài
            if (_runner != null && !_runner.IsRunning && !_isStartingGame)
            {
                Debug.LogWarning("[PvP] Socket Runner đã bị ngắt kết nối do ứng dụng mất focus quá lâu!");
                SetStatus(LocalizationManager.T("KẾT NỐI ĐÃ HẾT HẠN DO RỜI ỨNG DỤNG. VUI LÒNG VÀO LẠI PHÒNG!", "CONNECTION EXPIRED WHILE AWAY. PLEASE REJOIN!"));
                ShutdownRunnerOnly();
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            // Khi ứng dụng resume lại từ background
            if (_runner != null && !_runner.IsRunning && !_isStartingGame)
            {
                Debug.LogWarning("[PvP] Socket Runner đã bị ngắt kết nối do ứng dụng tạm dừng quá lâu!");
                SetStatus(LocalizationManager.T("KẾT NỐI ĐÃ HẾT HẠN DO RỜI ỨNG DỤNG. VUI LÒNG VÀO LẠI PHÒNG!", "CONNECTION EXPIRED WHILE AWAY. PLEASE REJOIN!"));
                ShutdownRunnerOnly();
            }
        }
    }

    public void LeaveMatchAndReturnToMenu()
    {
        PvPMatchManager.IsPvPModeActive = false;
        PvPMatchManager.IsCoopModeActive = false;
        IsBotPracticeMode = false;
        ShutdownRunnerOnly();
        Time.timeScale = 1.0f;
        RealmTransitionManager.LoadSceneWithTransition("MainMenuScene", RealmTransitionManager.TransitionTheme.ReturnSanctuary);
    }

    public void ResetStateSequences()
    {
        _localStateSeq = 0;
        _lastReceivedStateSeq = 0;
        _packetSeq = 0;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    private struct FloatUnion
    {
        [System.Runtime.InteropServices.FieldOffset(0)] public float f;
        [System.Runtime.InteropServices.FieldOffset(0)] public int i;
    }

    private static int FloatToInt(float val)
    {
        FloatUnion u = default;
        u.f = val;
        return u.i;
    }

    private static float IntToFloat(int val)
    {
        FloatUnion u = default;
        u.i = val;
        return u.f;
    }

    private static byte GetHeroCode(string heroId)
    {
        string norm = CharacterManager.NormalizeHeroId(heroId);
        if (norm == CharacterManager.CHAR_VAELSTRIDER) return 0;
        if (norm == CharacterManager.CHAR_GALE_RONIN) return 1;
        if (norm == CharacterManager.CHAR_AZURE_BLADE) return 2;
        if (norm == CharacterManager.CHAR_TITAN_BERSERKER) return 3;
        if (norm == CharacterManager.CHAR_DUNE_ASSASSIN) return 4;
        if (norm == CharacterManager.CHAR_CELESTIAL_MAGE) return 5;
        if (norm == CharacterManager.CHAR_ABYSSAL_MECH) return 6;
        return 0;
    }

    private static string GetHeroIdFromCode(byte code)
    {
        switch (code)
        {
            case 0: return CharacterManager.CHAR_VAELSTRIDER;
            case 1: return CharacterManager.CHAR_GALE_RONIN;
            case 2: return CharacterManager.CHAR_AZURE_BLADE;
            case 3: return CharacterManager.CHAR_TITAN_BERSERKER;
            case 4: return CharacterManager.CHAR_DUNE_ASSASSIN;
            case 5: return CharacterManager.CHAR_CELESTIAL_MAGE;
            case 6: return CharacterManager.CHAR_ABYSSAL_MECH;
            default: return CharacterManager.CHAR_VAELSTRIDER;
        }
    }

    // =========================================================================
    // TRUYỀN NHẬN GÓI TIN ĐỒNG BỘ PVP REAL-TIME (TỐI ƯU ZERO-GC BINARY STREAM)
    // =========================================================================
    public void SendRawBytes(byte[] bytes, int channelId = 1)
    {
        if (IsBotPracticeMode || _runner == null || !_runner.IsRunning) return;
        // Chỉ gửi gói tin khi đã có đủ 2 người chơi trong phòng để tránh gửi vào phiên rỗng
        if (GetConnectedPlayerCount() < 2) return;

        try
        {
            // Kênh 2 (State di chuyển): Xoay vòng 1024 slot chống nghẽn hàng đợi ReliableData trên Android/PC
            int subKey = (channelId == 2) ? (int)((++_packetSeq) & 0x3FF) : (int)((++_packetSeq) & 0x7FFFFFFF);
            ReliableKey key = ReliableKey.FromInts(channelId, subKey, 0, 0);

            if (_runner.IsServer)
            {
                foreach (var p in _runner.ActivePlayers)
                {
                    if (p != _runner.LocalPlayer)
                    {
                        try
                        {
                            _runner.SendReliableDataToPlayer(p, key, bytes);
                        }
                        catch (Exception)
                        {
                            // Bỏ qua nếu người chơi này vừa ngắt kết nối hoặc socket đang đóng (chống AssertException)
                        }
                    }
                }
            }
            else
            {
                try
                {
                    // Máy Client gửi thẳng lên Host/Server qua SendReliableDataToServer
                    _runner.SendReliableDataToServer(key, bytes);
                }
                catch (Exception)
                {
                    // Bỏ qua nếu Server đang ngắt kết nối
                }
            }
        }
        catch (Exception ex)
        {
            // Bọc an toàn tuyệt đối, không bao giờ để AssertException của Fusion làm gián đoạn game loop
            Debug.LogWarning("[PVP NETWORK] Safe SendRawBytes catch: " + ex.Message);
        }
    }

    private void SendRawPacket(string payload, int channelId = 1)
    {
        if (IsBotPracticeMode || _runner == null || !_runner.IsRunning) return;
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            SendRawBytes(bytes, channelId);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[PVP NETWORK] Safe SendRawPacket catch: " + ex.Message);
        }
    }

    public void SendHandshakePacket()
    {
        ResetStateSequences();
        var acc = AccountDatabase.GetActiveAccount();
        string myName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName :
                        (acc != null && !string.IsNullOrEmpty(acc.username)) ? acc.username :
                        PlayerPrefs.GetString(AccountDatabase.PREF_CURRENT_USER, "HIỆP SĨ");
        if (string.IsNullOrEmpty(myName)) myName = "HIỆP SĨ";
        myName = myName.Replace("|", " ");

        string myHero = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        string normHero = CharacterManager.NormalizeHeroId(myHero);
        int myHeroLv = AccountDatabase.GetHeroLevel(normHero);
        if (myHeroLv <= 0) myHeroLv = 1;

        int maxHp = 100;
        if (PvPMatchManager.IsPvPModeActive)
        {
            maxHp = GetNormalizedPvPMaxHealth(myHero);
        }
        else
        {
            maxHp = PlayerHealth.Instance != null ? PlayerHealth.Instance.MaxHealth : CharacterManager.GetBaseHealth(myHero);
        }
        if (maxHp <= 0) maxHp = 100;
        int curHp = PlayerHealth.Instance != null ? PlayerHealth.Instance.CurrentHealth : maxHp;

        string pkt = string.Format(CultureInfo.InvariantCulture, "H|{0}|{1}|{2}|{3}|{4}|{5}", myName, myHero, maxHp, curHp, (int)SelectedRoundFormat, myHeroLv);
        SendRawPacket(pkt, 1);
    }

    public void KickPlayer(string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return;
        SendRawPacket($"KICK|{targetName}", 1);

        if (_runner != null && _runner.IsServer && _remotePlayerRef != PlayerRef.None)
        {
            try
            {
                _runner.Disconnect(_remotePlayerRef);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PVP NETWORK] Disconnect player warning: " + ex.Message);
            }
        }

        // Xóa bỏ trạng thái đối thủ tức thì trên máy Chủ phòng
        HasOpponentJoined = false;
        OpponentName = "";
        OpponentHeroId = "";
        IsOpponentReady = false;
        _remotePlayerRef = PlayerRef.None;

        if (PvPLobbyUI.Instance != null)
        {
            PvPLobbyUI.Instance.RefreshStep5WaitingRoomUI();
        }
        if (CoopPartyListHUD.Instance != null)
        {
            CoopPartyListHUD.Instance.RefreshPartyList();
        }
        if (PvPRemoteOpponent.Instance != null)
        {
            Destroy(PvPRemoteOpponent.Instance.gameObject);
        }
    }

    public void SendMatchTimeSync(int elapsedSeconds)
    {
        SendRawPacket($"T|{elapsedSeconds}", 1);
    }

    public void BroadcastRoomClosed()
    {
        SendRawPacket("ROOM_CLOSED", 1);
    }

    public float GetEstimatedLatencySeconds()
    {
        if (_runner != null && _runner.IsRunning)
        {
            foreach (var p in _runner.ActivePlayers)
            {
                if (p != _runner.LocalPlayer)
                {
                    double rtt = _runner.GetPlayerRtt(p);
                    if (rtt > 0.005 && rtt < 1.0)
                    {
                        // 1-way latency ≈ RTT / 2 (Dao động từ 35ms đến 160ms trên mạng Mobile)
                        return Mathf.Clamp((float)(rtt * 0.5), 0.035f, 0.160f);
                    }
                }
            }
        }
        return 0.075f; // Mặc định 75ms cho mạng di động 4G / Wi-Fi
    }

    /// <summary>
    /// Lấy độ trễ khứ hồi thực tế Round-Trip Time (RTT Ping) tính bằng milliseconds
    /// </summary>
    public int GetPingMilliseconds()
    {
        if (_runner != null && _runner.IsRunning)
        {
            foreach (var p in _runner.ActivePlayers)
            {
                if (p != _runner.LocalPlayer)
                {
                    double rtt = _runner.GetPlayerRtt(p);
                    if (rtt > 0.001 && rtt < 2.0)
                    {
                        return Mathf.Clamp(Mathf.RoundToInt((float)(rtt * 1000.0)), 15, 999);
                    }
                }
            }
            double selfRtt = _runner.GetPlayerRtt(_runner.LocalPlayer);
            if (selfRtt > 0.001 && selfRtt < 2.0)
            {
                return Mathf.Clamp(Mathf.RoundToInt((float)(selfRtt * 1000.0)), 15, 999);
            }
        }
        return -1;
    }

    private readonly byte[] _stateBuffer = new byte[80];

    public void SendPlayerStatePacket(
        Vector2 pos, Vector2 vel, bool flipX, Vector2 scale,
        float moveX, float moveY, float speed,
        int curHp, int maxHp, string spriteName, string heroId)
    {
        uint seq = ++_localStateSeq;

        // GÓI TIN NHỊ PHÂN SIÊU GỌN (Zero-GC Binary Stream):
        // [0]: Magic byte 0xFE
        // [1]: Type 0x01 (State)
        // [2..5]: uint seq
        // [6..9]: float posX
        // [10..13]: float posY
        // [14..17]: float velX
        // [18..21]: float velY
        // [22]: flags (bit 0 = flipX)
        // [23..24]: ushort curHp
        // [25..26]: ushort maxHp
        // [27]: byte heroCode
        // [28]: byte spriteLen
        // [29..]: sprite bytes (tối đa 32 bytes)

        _stateBuffer[0] = 0xFE;
        _stateBuffer[1] = 0x01;

        _stateBuffer[2] = (byte)(seq);
        _stateBuffer[3] = (byte)(seq >> 8);
        _stateBuffer[4] = (byte)(seq >> 16);
        _stateBuffer[5] = (byte)(seq >> 24);

        int pxBits = FloatToInt(pos.x);
        _stateBuffer[6] = (byte)(pxBits);
        _stateBuffer[7] = (byte)(pxBits >> 8);
        _stateBuffer[8] = (byte)(pxBits >> 16);
        _stateBuffer[9] = (byte)(pxBits >> 24);

        int pyBits = FloatToInt(pos.y);
        _stateBuffer[10] = (byte)(pyBits);
        _stateBuffer[11] = (byte)(pyBits >> 8);
        _stateBuffer[12] = (byte)(pyBits >> 16);
        _stateBuffer[13] = (byte)(pyBits >> 24);

        int vxBits = FloatToInt(vel.x);
        _stateBuffer[14] = (byte)(vxBits);
        _stateBuffer[15] = (byte)(vxBits >> 8);
        _stateBuffer[16] = (byte)(vxBits >> 16);
        _stateBuffer[17] = (byte)(vxBits >> 24);

        int vyBits = FloatToInt(vel.y);
        _stateBuffer[18] = (byte)(vyBits);
        _stateBuffer[19] = (byte)(vyBits >> 8);
        _stateBuffer[20] = (byte)(vyBits >> 16);
        _stateBuffer[21] = (byte)(vyBits >> 24);

        _stateBuffer[22] = (byte)(flipX ? 1 : 0);

        ushort safeCurHp = (ushort)Mathf.Clamp(curHp, 0, 65535);
        _stateBuffer[23] = (byte)(safeCurHp);
        _stateBuffer[24] = (byte)(safeCurHp >> 8);

        ushort safeMaxHp = (ushort)Mathf.Clamp(maxHp, 1, 65535);
        _stateBuffer[25] = (byte)(safeMaxHp);
        _stateBuffer[26] = (byte)(safeMaxHp >> 8);

        _stateBuffer[27] = GetHeroCode(heroId);

        int sprLen = 0;
        if (!string.IsNullOrEmpty(spriteName))
        {
            sprLen = Encoding.ASCII.GetBytes(spriteName, 0, Mathf.Min(spriteName.Length, 32), _stateBuffer, 29);
        }
        _stateBuffer[28] = (byte)sprLen;

        int totalLen = 29 + sprLen;
        byte[] toSend = new byte[totalLen];
        Buffer.BlockCopy(_stateBuffer, 0, toSend, 0, totalLen);

        SendRawBytes(toSend, 2);
    }

    public void SendActionVfxPacket(string actionType, Vector2 dir, Vector2 pos)
    {
        string pkt = string.Format(
            CultureInfo.InvariantCulture,
            "A|{0}|{1:F2}|{2:F2}|{3:F2}|{4:F2}",
            actionType, dir.x, dir.y, pos.x, pos.y);
        SendRawPacket(pkt, 3);
    }

    public void SendDamageToOpponentPacket(int damage, bool isCrit, Vector2 knockback, float stunDur = 0f)
    {
        if (damage <= 0) return;
        string pkt = string.Format(
            CultureInfo.InvariantCulture,
            "D|{0}|{1}|{2:F2}|{3:F2}|{4:F2}",
            damage, isCrit ? 1 : 0, knockback.x, knockback.y, stunDur);
        SendRawPacket(pkt, 4);
    }

    public void SendMatchKoPacket(bool localPlayerDied)
    {
        string pkt = "K|" + (localPlayerDied ? "LOCAL_DIED" : "OPPONENT_DIED");
        SendRawPacket(pkt, 5);
    }

    public void SendRematchPacket(string subType = "WANT")
    {
        SendRawPacket("R|" + subType, 6);
    }

    private void ParseIncomingPacket(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return;
        string[] parts = raw.Split('|');
        if (parts.Length < 2) return;

        string type = parts[0];
        if (type == "H" && parts.Length >= 5)
        {
            ResetStateSequences();
            OpponentName = parts[1];
            OpponentHeroId = CharacterManager.NormalizeHeroId(parts[2]);
            if (int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mHp))
                OpponentMaxHp = Mathf.Max(1, mHp);
            if (int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int cHp))
                OpponentCurrentHp = Mathf.Clamp(cHp, 0, OpponentMaxHp);

            if (parts.Length >= 6 && int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fmtInt))
            {
                SelectedRoundFormat = (PvPRoundFormat)Mathf.Clamp(fmtInt, 0, 2);
            }
            if (parts.Length >= 7 && int.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int oppLv))
            {
                OpponentLevel = Mathf.Max(1, oppLv);
            }

            OnOpponentIdentityUpdated?.Invoke();
            if (PvPRemoteOpponent.Instance == null && (PvPMatchManager.IsPvPModeActive || PvPMatchManager.IsCoopModeActive || IsRunning))
            {
                var localPl = GameObject.FindWithTag("Player");
                Vector3 spawnPos = localPl != null ? localPl.transform.position + new Vector3(1.5f, 0f, 0f) : Vector3.zero;
                PvPRemoteOpponent.SpawnOpponent(spawnPos, OpponentName, OpponentHeroId, OpponentMaxHp);
            }
            else if (PvPRemoteOpponent.Instance != null)
            {
                PvPRemoteOpponent.Instance.ApplyOpponentIdentity(OpponentName, OpponentHeroId, OpponentMaxHp, OpponentCurrentHp);
            }
        }
        else if (type == "S" && parts.Length >= 8)
        {
            if (uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint seq))
            {
                // Chống giật lùi (Rubberbanding): Drop bỏ gói tin đến muộn / cũ hơn gói tin mới nhất đã nhận
                if (_lastReceivedStateSeq > 0 && seq <= _lastReceivedStateSeq && (_lastReceivedStateSeq - seq < 500))
                {
                    return; // Drop packet cũ lập tức
                }
                _lastReceivedStateSeq = seq;
            }

            if (parts.Length < 15)
            {
                // Định dạng Compact thời gian thực siêu mượt cho Mobile ↔ Mobile
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float px);
                float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float py);
                float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float vx);
                float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float vy);
                bool flipX = parts[6] == "1";
                int.TryParse(parts[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out int curHp);

                int maxHp = OpponentMaxHp;
                string spriteName = "";
                string heroId = OpponentHeroId;

                if (parts.Length >= 11)
                {
                    // Format mới: S|seq|x|y|vx|vy|flipX|curHp|maxHp|sprite|heroId
                    if (int.TryParse(parts[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedMax) && parsedMax > 0)
                    {
                        maxHp = parsedMax;
                    }
                    spriteName = parts[9];
                    if (!string.IsNullOrEmpty(parts[10])) heroId = CharacterManager.NormalizeHeroId(parts[10]);
                }
                else if (parts.Length >= 9)
                {
                    spriteName = parts[8];
                }

                if (maxHp > 0) OpponentMaxHp = maxHp;
                OpponentCurrentHp = Mathf.Clamp(curHp, 0, OpponentMaxHp);
                if (!string.IsNullOrEmpty(heroId)) OpponentHeroId = heroId;

                Vector2 syncScale = (OpponentHeroId == CharacterManager.CHAR_CELESTIAL_MAGE) ? new Vector2(0.62f, 0.62f) : Vector2.one;

                if (PvPRemoteOpponent.Instance == null && (PvPMatchManager.IsPvPModeActive || PvPMatchManager.IsCoopModeActive || IsRunning))
                {
                    PvPRemoteOpponent.SpawnOpponent(new Vector3(px, py, 0f), OpponentName, OpponentHeroId, OpponentMaxHp);
                }
                if (PvPRemoteOpponent.Instance != null)
                {
                    float spd = Mathf.Sqrt(vx * vx + vy * vy);
                    PvPRemoteOpponent.Instance.OnReceiveNetworkState(
                        new Vector2(px, py), new Vector2(vx, vy), flipX, syncScale,
                        vx, vy, spd, OpponentCurrentHp, OpponentMaxHp, spriteName, OpponentHeroId);
                }
            }
            else
            {
                // Định dạng đầy đủ (Legacy Fallback)
                int offset = 1;
                float.TryParse(parts[offset + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float px);
                float.TryParse(parts[offset + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out float py);
                float.TryParse(parts[offset + 3], NumberStyles.Float, CultureInfo.InvariantCulture, out float vx);
                float.TryParse(parts[offset + 4], NumberStyles.Float, CultureInfo.InvariantCulture, out float vy);
                bool flipX = parts[offset + 5] == "1";
                float.TryParse(parts[offset + 6], NumberStyles.Float, CultureInfo.InvariantCulture, out float sx);
                float.TryParse(parts[offset + 7], NumberStyles.Float, CultureInfo.InvariantCulture, out float sy);
                float.TryParse(parts[offset + 8], NumberStyles.Float, CultureInfo.InvariantCulture, out float mx);
                float.TryParse(parts[offset + 9], NumberStyles.Float, CultureInfo.InvariantCulture, out float my);
                float.TryParse(parts[offset + 10], NumberStyles.Float, CultureInfo.InvariantCulture, out float spd);
                int.TryParse(parts[offset + 11], NumberStyles.Integer, CultureInfo.InvariantCulture, out int curHp);
                int.TryParse(parts[offset + 12], NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxHp);
                string spriteName = parts[offset + 13];
                string heroId = CharacterManager.NormalizeHeroId(parts[offset + 14]);

                OpponentHeroId = heroId;
                if (maxHp > 0) OpponentMaxHp = maxHp;
                OpponentCurrentHp = Mathf.Clamp(curHp, 0, OpponentMaxHp);

                if (PvPRemoteOpponent.Instance == null && (PvPMatchManager.IsPvPModeActive || PvPMatchManager.IsCoopModeActive || IsRunning))
                {
                    PvPRemoteOpponent.SpawnOpponent(new Vector3(px, py, 0f), OpponentName, heroId, OpponentMaxHp);
                }
                if (PvPRemoteOpponent.Instance != null)
                {
                    PvPRemoteOpponent.Instance.OnReceiveNetworkState(
                        new Vector2(px, py), new Vector2(vx, vy), flipX, new Vector2(sx, sy),
                        mx, my, spd, OpponentCurrentHp, OpponentMaxHp, spriteName, heroId);
                }
            }
        }
        else if (type == "A" && parts.Length >= 6)
        {
            string act = parts[1];
            float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float dx);
            float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float dy);
            float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float px);
            float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float py);

            if (PvPRemoteOpponent.Instance != null)
            {
                PvPRemoteOpponent.Instance.PlayRemoteActionVFX(act, new Vector2(dx, dy), new Vector2(px, py));
            }
        }
        else if (type == "D" && parts.Length >= 6)
        {
            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int dmg);
            bool isCrit = parts[2] == "1";
            float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float kx);
            float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float ky);
            float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float stun);

            if (PvPMatchManager.Instance != null)
            {
                PvPMatchManager.Instance.OnLocalPlayerReceivePvPDamage(dmg, isCrit, new Vector2(kx, ky), stun);
            }
        }
        else if (type == "K" && parts.Length >= 2)
        {
            bool remoteDied = parts[1] == "LOCAL_DIED";
            if (PvPMatchManager.Instance != null)
            {
                PvPMatchManager.Instance.OnRemoteMatchEndSignal(localWon: remoteDied);
            }
        }
        else if (type == "R")
        {
            string sub = parts.Length > 1 ? parts[1] : "WANT";
            if (PvPMatchManager.Instance != null)
            {
                if (sub == "ACCEPT")
                {
                    PvPMatchManager.Instance.OnRemoteRematchAccepted();
                }
                else if (sub == "CANCEL")
                {
                    PvPMatchManager.Instance.OnRemoteRematchCancelled();
                }
                else // "WANT" hoặc "REMATCH"
                {
                    PvPMatchManager.Instance.OnRemoteRematchRequested();
                }
            }
        }
        else if (type == "READY" && parts.Length >= 2)
        {
            IsOpponentReady = (parts[1] == "1");
            OnOpponentReadyChanged?.Invoke();
        }
        else if (type == "CHAT" && parts.Length >= 3)
        {
            string sender = parts[1].Replace("[", "").Replace("]", "").Trim();
            string msg = string.Join("|", parts, 2, parts.Length - 2).Replace("[", "").Replace("]", "").Trim();
            OnChatMessageReceived?.Invoke(sender, msg);
        }
        else if (type == "HERO" && parts.Length >= 2)
        {
            OpponentHeroId = CharacterManager.NormalizeHeroId(parts[1]);
            OnOpponentIdentityUpdated?.Invoke();
            if (PvPRemoteOpponent.Instance == null && (PvPMatchManager.IsPvPModeActive || PvPMatchManager.IsCoopModeActive || IsRunning))
            {
                var localPl = GameObject.FindWithTag("Player");
                Vector3 spawnPos = localPl != null ? localPl.transform.position + new Vector3(1.5f, 0f, 0f) : Vector3.zero;
                PvPRemoteOpponent.SpawnOpponent(spawnPos, OpponentName, OpponentHeroId, OpponentMaxHp);
            }
            else if (PvPRemoteOpponent.Instance != null)
            {
                PvPRemoteOpponent.Instance.ApplyOpponentIdentity(OpponentName, OpponentHeroId, OpponentMaxHp, OpponentCurrentHp);
            }
        }
        else if (type == "FREQ" && parts.Length >= 5)
        {
            string target = parts[1];
            string sender = parts[2];
            string hero = parts[3];
            int.TryParse(parts[4], out int lvl);

            var acc = AccountDatabase.GetActiveAccount();
            string myName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName :
                            (acc != null && !string.IsNullOrEmpty(acc.username)) ? acc.username : "Hiệp Sĩ";

            bool isForMe = (target == "*") ||
                           string.Equals(target, myName, StringComparison.OrdinalIgnoreCase) ||
                           (acc != null && !string.IsNullOrEmpty(acc.username) && string.Equals(target, acc.username, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(sender) && !string.Equals(sender, myName, StringComparison.OrdinalIgnoreCase));

            if (isForMe)
            {
                FriendManager.EnsureInstance().ReceiveFriendRequest(sender, hero, lvl);
                if (PvPLobbyUI.IsLobbyModalOpen && PvPLobbyUI.Instance != null)
                {
                    PvPLobbyUI.Instance.ShowLobbyToast(
                        LocalizationManager.T($"{sender} đã gửi lời mời kết bạn!", $"{sender} sent a friend request!"),
                        new Color(0.45f, 0.92f, 1f));
                }
                InGameTeamChatUI.ShowFriendRequestNotification(sender);
                MainMenuManager mm = UnityEngine.Object.FindAnyObjectByType<MainMenuManager>();
                if (mm != null)
                {
                    mm.UpdateMailboxBadge();
                    mm.UpdateFriendsBadge();
                }
            }
        }
        else if (type == "FACC" && parts.Length >= 3)
        {
            string target = parts[1];
            string sender = parts[2];
            string hero = parts.Length >= 4 ? parts[3] : CharacterManager.CHAR_VAELSTRIDER;

            var acc = AccountDatabase.GetActiveAccount();
            string myName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName :
                            (acc != null && !string.IsNullOrEmpty(acc.username)) ? acc.username : "Hiệp Sĩ";

            bool isForMe = (target == "*") ||
                           string.Equals(target, myName, StringComparison.OrdinalIgnoreCase) ||
                           (acc != null && !string.IsNullOrEmpty(acc.username) && string.Equals(target, acc.username, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(sender) && !string.Equals(sender, myName, StringComparison.OrdinalIgnoreCase));

            if (isForMe)
            {
                FriendManager.EnsureInstance().AddFriend(sender, "", hero);
                if (PvPLobbyUI.IsLobbyModalOpen && PvPLobbyUI.Instance != null)
                {
                    PvPLobbyUI.Instance.ShowLobbyToast(
                        LocalizationManager.T($"{sender} đã chấp nhận lời mời kết bạn! Hai bạn hiện đã là bạn bè.", $"{sender} accepted your friend request! You are now friends."),
                        new Color(0.35f, 1f, 0.55f));
                }
                InGameTeamChatUI.Instance?.HandleInGameChatMessage("BẠN BÈ", LocalizationManager.T($"{sender} đã đồng ý kết bạn!", $"{sender} accepted your friend request!"));
                MainMenuManager mm = UnityEngine.Object.FindAnyObjectByType<MainMenuManager>();
                if (mm != null)
                {
                    mm.UpdateMailboxBadge();
                    mm.UpdateFriendsBadge();
                }
            }
        }
        else if (type == "INVITE" && parts.Length >= 4)
        {
            string target = parts[1];
            string sender = parts[2];
            string pin = parts[3];

            var acc = AccountDatabase.GetActiveAccount();
            string myName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName : "Hiệp Sĩ";

            if (target == "*" || string.Equals(target, myName, StringComparison.OrdinalIgnoreCase))
            {
                FriendManager.EnsureInstance().ReceiveRoomInvite(sender, pin);
                PvPLobbyUI.ShowRoomInviteDialog(sender, pin);
                InGameTeamChatUI.ShowInteractiveInviteToast(sender, pin);
                MainMenuManager mm = UnityEngine.Object.FindAnyObjectByType<MainMenuManager>();
                if (mm != null)
                {
                    mm.UpdateMailboxBadge();
                    mm.UpdateFriendsBadge();
                }
            }
        }
        else if (type == "G")
        {
            BeginVsCountdownAndEnterArena();
        }
        else if (type == "KICK" && parts.Length >= 2)
        {
            string target = parts[1];
            var acc = AccountDatabase.GetActiveAccount();
            string myDisplayName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName : "";
            string myUsername = (acc != null && !string.IsNullOrEmpty(acc.username)) ? acc.username : "";
            string myPref = PlayerPrefs.GetString(AccountDatabase.PREF_CURRENT_USER, "Hiệp Sĩ");

            bool isMe = (!IsHost) && (
                string.Equals(target, myDisplayName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(target, myUsername, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(target, myPref, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(target) || target == "Đồng đội"
            );

            if (isMe)
            {
                // Bản thân bị Chủ phòng kích khỏi phòng -> Lập tức thoát về sảnh chính
                LeaveMatchAndReturnToMenu();
                string kickMsg = LocalizationManager.T("BẠN ĐÃ BỊ CHỦ PHÒNG KÍCH KHỎI PHÒNG!", "YOU HAVE BEEN KICKED BY THE ROOM HOST!");
                if (InGameTeamChatUI.Instance != null)
                {
                    InGameTeamChatUI.Instance.HandleInGameChatMessage("HỆ THỐNG", kickMsg);
                }
                if (PvPLobbyUI.Instance != null)
                {
                    PvPLobbyUI.Instance.ShowLobbyToast(kickMsg, new Color(1f, 0.35f, 0.35f));
                }
            }
            else
            {
                string kickInfo = LocalizationManager.T($"CHỦ PHÒNG ĐÃ KÍCH {target.ToUpper()} KHỎI PHÒNG!", $"HOST KICKED {target.ToUpper()} FROM THE ROOM!");
                if (InGameTeamChatUI.Instance != null)
                {
                    InGameTeamChatUI.Instance.HandleInGameChatMessage("HỆ THỐNG", kickInfo);
                }
                if (PvPLobbyUI.Instance != null)
                {
                    PvPLobbyUI.Instance.ShowLobbyToast(kickInfo, new Color(1f, 0.55f, 0.35f));
                    PvPLobbyUI.Instance.RefreshStep5WaitingRoomUI();
                }
            }
        }
        else if (type == "ROOM_CLOSED")
        {
            // Chủ phòng đã đóng phòng hoặc rời phòng -> Cưỡng chế tất cả thành viên ra sảnh
            if (!IsHost)
            {
                LeaveMatchAndReturnToMenu();
                string closeMsg = LocalizationManager.T("CHỦ PHÒNG ĐÃ RỜI PHÒNG. PHÒNG ĐÃ ĐÓNG!", "HOST HAS LEFT. ROOM CLOSED!");
                if (InGameTeamChatUI.Instance != null)
                {
                    InGameTeamChatUI.Instance.HandleInGameChatMessage("HỆ THỐNG", closeMsg);
                }
                if (PvPLobbyUI.Instance != null)
                {
                    PvPLobbyUI.Instance.ShowLobbyToast(closeMsg, new Color(1f, 0.35f, 0.35f));
                }
            }
        }
        else if (type == "T" && parts.Length >= 2)
        {
            if (int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int remoteTime))
            {
                MatchTimerUI.SyncFromHost(remoteTime);
            }
        }
    }

    // =========================================================================
    // PHOTON FUSION 2 CALLBACKS
    // =========================================================================
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player != runner.LocalPlayer)
        {
            _remotePlayerRef = player;
        }

        int count = GetConnectedPlayerCount();
        if (count >= 2)
        {
            HasOpponentJoined = true;
            SendHandshakePacket();
            SetStatus(LocalizationManager.T("ĐỒNG ĐỘI ĐÃ VÀO PHÒNG! CHỜ SẴN SÀNG...", "TEAMMATE JOINED! WAITING FOR READY..."));
        }

        // CHỈ gửi G|START reconnect khi Host đang thực sự trong trận đấu gameplay (không phải ở MainMenuScene và modal lobby không mở)
        string curScene = SceneManager.GetActiveScene().name;
        bool isActualGameplay = curScene != "MainMenuScene" && curScene != "LoginScene" && !PvPLobbyUI.IsLobbyModalOpen;
        if (isActualGameplay && (curScene == "Map1_Thestart" || PvPMatchManager.IsCoopModeActive))
        {
            SendRawPacket("G|START", 7);
            SendHandshakePacket();

            string reconMsg = LocalizationManager.T("MỘT ĐỒNG ĐỘI ĐÃ KẾT NỐI LẠI VÀO HẦM NGỤC!", "A TEAMMATE HAS RECONNECTED TO THE DUNGEON!");
            if (PvPLobbyUI.IsLobbyModalOpen && PvPLobbyUI.Instance != null)
            {
                PvPLobbyUI.Instance.ShowLobbyToast(reconMsg, new Color(0.35f, 1f, 0.55f));
            }
            if (InGameTeamChatUI.Instance != null)
            {
                InGameTeamChatUI.Instance.HandleInGameChatMessage("HỆ THỐNG", reconMsg);
            }
        }

        OnOpponentReadyChanged?.Invoke();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (player == _remotePlayerRef)
        {
            _remotePlayerRef = PlayerRef.None;
        }

        int remaining = GetConnectedPlayerCount();
        HasOpponentJoined = (remaining >= 2);
        ResetReadyStates();

        // Nếu bản thân là Khách mà Host thoát -> Cưỡng chế văng ra ngoài sảnh và đóng phòng
        if (!IsHost)
        {
            LeaveMatchAndReturnToMenu();
            string hostLeftMsg = LocalizationManager.T("CHỦ PHÒNG ĐÃ RỜI TRẬN. PHÒNG ĐÃ ĐÓNG!", "HOST HAS LEFT. ROOM CLOSED!");
            if (InGameTeamChatUI.Instance != null)
            {
                InGameTeamChatUI.Instance.HandleInGameChatMessage("HỆ THỐNG", hostLeftMsg);
            }
            if (PvPLobbyUI.Instance != null)
            {
                PvPLobbyUI.Instance.ShowLobbyToast(hostLeftMsg, new Color(1f, 0.35f, 0.35f));
            }
            return;
        }

        string leftPlayerName = !string.IsNullOrEmpty(OpponentName) ? OpponentName : "Đồng đội";
        string msg = LocalizationManager.T(
            $"NGƯỜI CHƠI {leftPlayerName.ToUpper()} ĐÃ THOÁT TRẬN! (MÃ PHÒNG #{CurrentRoomCode})",
            $"PLAYER {leftPlayerName.ToUpper()} HAS LEFT THE MATCH! (ROOM #{CurrentRoomCode})");

        if (PvPMatchManager.IsPvPModeActive && PvPMatchManager.Instance != null)
        {
            PvPMatchManager.Instance.OnOpponentDisconnectedWin();
        }
        else
        {
            SetStatus(msg);
            if (PvPLobbyUI.IsLobbyModalOpen && PvPLobbyUI.Instance != null)
            {
                PvPLobbyUI.Instance.ShowLobbyToast(msg, new Color(1f, 0.45f, 0.35f));
                PvPLobbyUI.Instance.RefreshStep5WaitingRoomUI();
            }
            if (InGameTeamChatUI.Instance != null)
            {
                InGameTeamChatUI.Instance.HandleInGameChatMessage("HỆ THỐNG", msg);
            }

            if (PvPRemoteOpponent.Instance != null)
            {
                Destroy(PvPRemoteOpponent.Instance.gameObject);
            }
        }
        OnOpponentReadyChanged?.Invoke();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        if (data.Array == null || data.Count <= 0) return;

        // Xử lý gói tin nhị phân siêu tốc (Zero-GC Binary Stream)
        if (data.Array[data.Offset] == 0xFE && data.Count >= 28)
        {
            ParseBinaryPacket(data.Array, data.Offset, data.Count);
            return;
        }

        string raw = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
        ParseIncomingPacket(raw);
    }

    private void ParseBinaryPacket(byte[] buffer, int offset, int count)
    {
        byte type = buffer[offset + 1];
        if (type == 0x01 && count >= 28) // State Packet
        {
            uint seq = (uint)(buffer[offset + 2] 
                           | (buffer[offset + 3] << 8) 
                           | (buffer[offset + 4] << 16) 
                           | (buffer[offset + 5] << 24));

            // CHỐNG TRIỆT ĐỂ DÂY CAO SU (RUBBERBANDING):
            // Luôn drop bỏ gói tin đến muộn / cũ hơn gói tin mới nhất đã nhận
            if (_lastReceivedStateSeq > 0)
            {
                if (seq <= _lastReceivedStateSeq)
                {
                    if (_lastReceivedStateSeq - seq < 500)
                    {
                        return; // Gói tin cũ -> bỏ qua ngay lập tức!
                    }
                }
            }
            _lastReceivedStateSeq = seq;

            int pxBits = buffer[offset + 6] | (buffer[offset + 7] << 8) | (buffer[offset + 8] << 16) | (buffer[offset + 9] << 24);
            int pyBits = buffer[offset + 10] | (buffer[offset + 11] << 8) | (buffer[offset + 12] << 16) | (buffer[offset + 13] << 24);
            int vxBits = buffer[offset + 14] | (buffer[offset + 15] << 8) | (buffer[offset + 16] << 16) | (buffer[offset + 17] << 24);
            int vyBits = buffer[offset + 18] | (buffer[offset + 19] << 8) | (buffer[offset + 20] << 16) | (buffer[offset + 21] << 24);

            float px = IntToFloat(pxBits);
            float py = IntToFloat(pyBits);
            float vx = IntToFloat(vxBits);
            float vy = IntToFloat(vyBits);

            bool flipX = (buffer[offset + 22] & 0x01) != 0;
            int curHp = (ushort)(buffer[offset + 23] | (buffer[offset + 24] << 8));
            int maxHp = (ushort)(buffer[offset + 25] | (buffer[offset + 26] << 8));
            byte heroCode = buffer[offset + 27];
            string heroId = GetHeroIdFromCode(heroCode);

            string spriteName = "";
            int sprLen = buffer[offset + 28];
            if (sprLen > 0 && offset + 29 + sprLen <= offset + count)
            {
                spriteName = Encoding.ASCII.GetString(buffer, offset + 29, sprLen);
            }

            if (maxHp > 0) OpponentMaxHp = maxHp;
            OpponentCurrentHp = Mathf.Clamp(curHp, 0, OpponentMaxHp);
            if (!string.IsNullOrEmpty(heroId)) OpponentHeroId = heroId;

            Vector2 syncScale = (OpponentHeroId == CharacterManager.CHAR_CELESTIAL_MAGE) ? new Vector2(0.62f, 0.62f) : Vector2.one;

            if (PvPRemoteOpponent.Instance == null && (PvPMatchManager.IsPvPModeActive || PvPMatchManager.IsCoopModeActive || IsRunning))
            {
                var localPl = GameObject.FindWithTag("Player");
                Vector3 spawnPos = localPl != null ? localPl.transform.position + new Vector3(1.5f, 0f, 0f) : Vector3.zero;
                PvPRemoteOpponent.SpawnOpponent(spawnPos, OpponentName, OpponentHeroId, OpponentMaxHp);
            }
            if (PvPRemoteOpponent.Instance != null)
            {
                float spd = Mathf.Sqrt(vx * vx + vy * vy);
                PvPRemoteOpponent.Instance.OnReceiveNetworkState(
                    new Vector2(px, py), new Vector2(vx, vy), flipX, syncScale,
                    vx, vy, spd, OpponentCurrentHp, OpponentMaxHp, spriteName, OpponentHeroId);
            }
        }
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (shutdownReason != ShutdownReason.Ok)
        {
            SetStatus(LocalizationManager.T(
                $"PHIÊN KẾT NỐI ĐÃ ĐÓNG ({shutdownReason}).",
                $"SESSION CLOSED ({shutdownReason})."));
            if ((PvPMatchManager.IsPvPModeActive || PvPMatchManager.IsCoopModeActive) && !IsBotPracticeMode)
            {
                LeaveMatchAndReturnToMenu();
            }
            else
            {
                ShutdownRunnerOnly();
            }
        }
    }
    public void OnConnectedToServer(NetworkRunner runner)
    {
        if (GetConnectedPlayerCount() >= 2)
        {
            SendHandshakePacket();
        }
    }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        SetStatus(LocalizationManager.T($"MẤT KẾT NỐI MÁY CHỦ: {reason}", $"DISCONNECTED: {reason}"));
        if ((PvPMatchManager.IsPvPModeActive || PvPMatchManager.IsCoopModeActive) && !IsBotPracticeMode)
        {
            LeaveMatchAndReturnToMenu();
        }
        else
        {
            ShutdownRunnerOnly();
        }
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        request.Accept();
    }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        SetStatus(LocalizationManager.T($"KHÔNG THỂ KẾT NỐI: {reason}", $"CONNECT FAILED: {reason}"));
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
