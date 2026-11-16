using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý toàn bộ vòng đời trận đấu Đấu Trường PvP 1v1 (Online Photon Fusion 2 & AI Rival Practice).
/// - Xây dựng Bản Đồ Đấu Trường Cổ Đại (Grand Colosseum Arena) độc lập hoàn toàn.
/// - Chuẩn hóa chỉ số máu PvP (x8 Base HP) để trận chiến kéo dài kịch tính 30-60 giây.
/// - Hiển thị Thanh Máu + Năng Lượng Đối Kháng Đôi (Top Versus HUD), tự động ẩn khi mở Settings.
/// </summary>
public class PvPMatchManager : MonoBehaviour
{
    public static PvPMatchManager Instance { get; private set; }
    public static bool IsPvPModeActive = false;
    public static bool IsCoopModeActive = false;

    public const float ARENA_HALF_WIDTH = 10.6f;
    public const float ARENA_HALF_HEIGHT = 5.2f;
    public const float ARENA_OCTAGON_SUM = 14.4f;
    public const float MATCH_DURATION_SECONDS = 90f;

    public bool IsCombatActive { get; private set; } = false;
    public bool IsMatchEnded { get; private set; } = false;

    private float _matchTimer = MATCH_DURATION_SECONDS;
    private float _networkSyncTimer = 0f;
    private const float NETWORK_SYNC_INTERVAL = 0.033f; // 30 Hz thời gian thực (33ms/gói) triệt tiêu độ trễ mạng tối đa
    private Vector2 _lastSentPos = Vector2.zero;
    private int _lastSentHp = -1;
    private string _lastSentSpr = "";
    private float _idleHeartbeatTimer = 0f;
    private float _timeSyncTimer = 1.0f;

    private PlayerController _localPlayer;
    private PlayerHealth _localHealth;
    private PlayerCombat _localCombat;
    private PlayerEnergySystem _localEnergy;
    private DuneAssassinSkill _localDuneSkill;
    private SpriteRenderer _localSr;
    private Animator _localAnim;
    private Rigidbody2D _localRb;

    private float _lastRecordedAttackTime = -10f;
    private float _lastUltiSendTime = -10f;

    // UI References
    private Canvas _pvpHudCanvas;
    private GameObject _topBarObj;
    private GameObject _exitBtnObj;
    private bool _externalHudVisible = true;

    private Image _leftHpFill;
    private Image _leftMpFill;
    private Image _leftPortraitImg;
    private TextMeshProUGUI _leftNameText;
    private TextMeshProUGUI _leftHpValueText;

    private Image _rightHpFill;
    private Image _rightPortraitImg;
    private TextMeshProUGUI _rightNameText;
    private TextMeshProUGUI _rightHpValueText;

    private TextMeshProUGUI _timerText;
    private TextMeshProUGUI _modeSubText;
    private TextMeshProUGUI _roundScoreText;
    private TextMeshProUGUI _countdownBannerText;
    private GameObject _resultModalObj;
    private TextMeshProUGUI _resultTitleText;
    private TextMeshProUGUI _resultRewardText;
    private Image _resultLeftHeroImg;
    private TextMeshProUGUI _resultLeftNameText;
    private Image _resultRightHeroImg;
    private TextMeshProUGUI _resultRightNameText;

    // Hệ thống Đa Hiệp Đấu (Round System: Bo1, Bo3, Bo5)
    private int _currentRound = 1;
    private int _localRoundWins = 0;
    private int _remoteRoundWins = 0;
    private int _requiredWins = 2;
    private int _maxRounds = 3;
    private bool _isRoundEnding = false;

    // Quản lý tái đấu đồng thuận 2 chiều trong 10 giây
    private Button _rematchBtn;
    private TextMeshProUGUI _rematchBtnText;
    private Image _rematchBtnBg;
    private bool _localWantsRematch = false;
    private bool _remoteWantsRematch = false;
    private Coroutine _rematchTimerRoutine = null;
    private const float REMATCH_WAIT_SECONDS = 10f;

    public static float GetHeroFootOffsetY(string heroId)
    {
        string norm = CharacterManager.NormalizeHeroId(heroId);
        if (norm == CharacterManager.CHAR_VAELSTRIDER)
        {
            // Adventurer (Player 1): sprite 96x80, chân ở y = +0.72 so với tâm gốc
            return 0.72f;
        }
        return 0f;
    }

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    public static PvPMatchManager EnsureArenaInitialized()
    {
        if (Instance != null) return Instance;
        var existing = FindAnyObjectByType<PvPMatchManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }
        GameObject go = new GameObject("PvPMatchManager");
        Instance = go.AddComponent<PvPMatchManager>();
        return Instance;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        ReleaseAndroidWifiLock();
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (IsPvPModeActive)
        {
            InitializeArenaMatch();
        }
        else if (IsCoopModeActive || (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsRunning))
        {
            InitializeCoopMatch();
        }
    }

    public void InitializeCoopMatch()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.runInBackground = true;

        IsCombatActive = true;
        IsMatchEnded = false;
        Time.timeScale = 1.0f;

        PvPNetworkManager.EnsureInstance();

        // 1. Không phá hủy bản đồ Dungeon! Giữ nguyên Dungeon Map (Map 1/2/3), phòng, quái, rương!
        // 2. Định vị Local Player tại vị trí bắt đầu
        _localPlayer = FindAnyObjectByType<PlayerController>();
        if (_localPlayer != null)
        {
            _localHealth = _localPlayer.GetComponent<PlayerHealth>();
            _localCombat = _localPlayer.GetComponent<PlayerCombat>();
            _localEnergy = _localPlayer.GetComponent<PlayerEnergySystem>();
            _localDuneSkill = _localPlayer.GetComponent<DuneAssassinSkill>();
            _localSr = _localPlayer.GetComponent<SpriteRenderer>();
            _localAnim = _localPlayer.GetComponent<Animator>();
            _localRb = _localPlayer.GetComponent<Rigidbody2D>();

            if (_localHealth != null)
            {
                if (_localHealth.IsDead) _localHealth.Revive();
            }
        }

        // 3. Spawn các đồng đội đã kết nối cạnh Player (khi có 2, 3 hoặc 4 người)
        if (PvPNetworkManager.Instance != null && (PvPNetworkManager.Instance.HasOpponentJoined || PvPNetworkManager.Instance.GetConnectedPlayerCount() >= 2))
        {
            Vector3 basePos = _localPlayer != null ? _localPlayer.transform.position : Vector3.zero;
            string oppName = PvPNetworkManager.Instance.OpponentName;
            string oppHero = PvPNetworkManager.Instance.OpponentHeroId;
            int oppMaxHp = PvPNetworkManager.Instance.OpponentMaxHp;

            Vector3 teammateSpawn = basePos + new Vector3(1.5f, 0f, 0f);
            PvPRemoteOpponent.SpawnOpponent(teammateSpawn, oppName, oppHero, oppMaxHp);
        }

        // 4. Gửi handshake và bắt đầu trận
        PvPNetworkManager.Instance?.SendHandshakePacket();
        CoopPartyListHUD.EnsureUIExists()?.RefreshPartyList();
    }

    public void InitializeArenaMatch()
    {
        // Tối ưu hóa hiệu năng và input trên Mobile: 60 FPS mượt mà, vSync tắt, cấm ngủ màn hình & khóa hiệu năng Wifi cao nhất
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.runInBackground = true;
        AcquireAndroidWifiLock();

        IsCombatActive = false;
        IsMatchEnded = false;
        _matchTimer = MATCH_DURATION_SECONDS;
        _externalHudVisible = true;
        Time.timeScale = 1.0f;

        PvPNetworkManager.EnsureInstance();

        // 1. Xây dựng Bản Đồ Võ Đài Cổ Đại độc lập & Dọn sạch thành phần PvE
        DungeonMapBuilder.BuildPvPColosseumArena();
        HidePvEElements();

        // 2. Định vị Local Player trên Đài Xuất Chiến Tây (-3.8, 0.2) hoặc Đông (+3.8, 0.2)
        // Khoảng cách 7.6m giúp cả 2 đấu sĩ đều hiện diện trọn vẹn trong khung hình Camera ngay từ giây đếm ngược đầu tiên!
        _localPlayer = FindAnyObjectByType<PlayerController>();
        if (_localPlayer != null)
        {
            _localHealth = _localPlayer.GetComponent<PlayerHealth>();
            _localCombat = _localPlayer.GetComponent<PlayerCombat>();
            _localEnergy = _localPlayer.GetComponent<PlayerEnergySystem>();
            _localDuneSkill = _localPlayer.GetComponent<DuneAssassinSkill>();
            _localSr = _localPlayer.GetComponent<SpriteRenderer>();
            _localAnim = _localPlayer.GetComponent<Animator>();
            _localRb = _localPlayer.GetComponent<Rigidbody2D>();

            bool isP1 = PvPNetworkManager.Instance.IsLocalPlayerOne();
            string myHero = CharacterManager.EnsureInstance().GetSelectedCharacterId();
            float myFootOffset = GetHeroFootOffsetY(myHero);
            Vector3 mySpawn = isP1 
                ? new Vector3(-3.8f, 0.2f - myFootOffset, 0f) 
                : new Vector3(3.8f, 0.2f - myFootOffset, 0f);
            _localPlayer.transform.position = mySpawn;
            if (_localRb != null)
            {
                _localRb.position = mySpawn;
                _localRb.linearVelocity = Vector2.zero;
            }
            if (_localSr != null)
            {
                _localSr.flipX = !isP1;
            }

            int pvpMaxHp = PvPNetworkManager.GetNormalizedPvPMaxHealth(myHero);
            if (_localHealth != null)
            {
                if (_localHealth.IsDead)
                {
                    _localHealth.Revive();
                }
                PlayerHealth.ResetRunPreservedHealth();
                _localHealth.SetBaseMaxHealth(pvpMaxHp);
                _localHealth.Heal(pvpMaxHp * 2);
            }
        }

        // 3. Spawn Đối Thủ trên Đài Xuất Chiến đối diện
        bool localIsP1 = PvPNetworkManager.Instance.IsLocalPlayerOne();
        string oppName = PvPNetworkManager.Instance.OpponentName;
        string oppHero = PvPNetworkManager.Instance.OpponentHeroId;
        int oppMaxHp = PvPNetworkManager.Instance.OpponentMaxHp;
        float oppFootOffset = GetHeroFootOffsetY(oppHero);

        Vector3 oppSpawn = localIsP1 
            ? new Vector3(3.8f, 0.2f - oppFootOffset, 0f) 
            : new Vector3(-3.8f, 0.2f - oppFootOffset, 0f);

        var opp = PvPRemoteOpponent.SpawnOpponent(oppSpawn, oppName, oppHero, oppMaxHp);
        var oppSr = opp.GetComponent<SpriteRenderer>();
        if (oppSr != null) oppSr.flipX = localIsP1;

        // 4. Khởi tạo Giao diện Thanh Máu + Năng Lượng Đối Kháng Đôi (Top Versus HUD)
        _currentRound = 1;
        _localRoundWins = 0;
        _remoteRoundWins = 0;
        _requiredWins = PvPNetworkManager.GetRequiredWins(PvPNetworkManager.SelectedRoundFormat);
        _maxRounds = PvPNetworkManager.GetMaxRounds(PvPNetworkManager.SelectedRoundFormat);
        _isRoundEnding = false;
        BuildVersusHUD();

        // 5. Gửi gói tin Handshake & Bắt đầu đếm ngược 3 - 2 - 1 - KHAI CHIẾN!
        PvPNetworkManager.Instance.SendHandshakePacket();
        StopAllCoroutines();
        StartCoroutine(MatchCountdownRoutine());
    }

    public void RestartArenaMatch()
    {
        if (_rematchTimerRoutine != null)
        {
            StopCoroutine(_rematchTimerRoutine);
            _rematchTimerRoutine = null;
        }
        _localWantsRematch = false;
        _remoteWantsRematch = false;
        if (_resultModalObj != null) _resultModalObj.SetActive(false);
        InitializeArenaMatch();
    }

    public void SetHudVisible(bool visible)
    {
        _externalHudVisible = visible;
        RefreshHudVisibility();
    }

    private void RefreshHudVisibility()
    {
        bool settingsOpen = (SettingsUI.Instance != null && SettingsUI.Instance.gameObject != null && SettingsUI.Instance.gameObject.activeInHierarchy);
        bool showTopHud = _externalHudVisible && !settingsOpen;

        if (_topBarObj != null && _topBarObj.activeSelf != showTopHud)
            _topBarObj.SetActive(showTopHud);

        if (_exitBtnObj != null && _exitBtnObj.activeSelf != showTopHud)
            _exitBtnObj.SetActive(showTopHud);
    }

    private void HidePvEElements()
    {
        var roomMgrs = FindObjectsByType<DungeonRoomManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var rm in roomMgrs) if (rm != null) rm.gameObject.SetActive(false);

        var spawners = FindObjectsByType<WaveSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in spawners) if (s != null) s.gameObject.SetActive(false);

        var chests = FindObjectsByType<TreasureChest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in chests) if (c != null) Destroy(c.gameObject);

        var graves = FindObjectsByType<LostShardsGrave>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var g in graves) if (g != null) Destroy(g.gameObject);

        var portals = FindObjectsByType<RuinPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in portals) if (p != null) Destroy(p.gameObject);

        // Ẩn toàn bộ cụm HUD đi ải PvE góc trái để nhường chỗ cho Thanh Máu Đối Kháng Đôi
        if (PlayerAvatarUI.Instance != null && PlayerAvatarUI.Instance.gameObject != null)
            PlayerAvatarUI.Instance.gameObject.SetActive(false);
        if (PlayerEnergyUI.Instance != null && PlayerEnergyUI.Instance.gameObject != null)
            PlayerEnergyUI.Instance.gameObject.SetActive(false);
        if (PlayerExpBarUI.Instance != null && PlayerExpBarUI.Instance.gameObject != null)
            PlayerExpBarUI.Instance.SetVisible(false);
        if (AugmentHUDBar.Instance != null && AugmentHUDBar.Instance.gameObject != null)
            AugmentHUDBar.Instance.gameObject.SetActive(false);
        if (PlayerStatPanelUI.Instance != null && PlayerStatPanelUI.Instance.gameObject != null)
            PlayerStatPanelUI.Instance.gameObject.SetActive(false);
        if (PotionQuickSlotUI.Instance != null && PotionQuickSlotUI.Instance.gameObject != null)
            PotionQuickSlotUI.Instance.gameObject.SetActive(false);

        var shardsUIs = FindObjectsByType<ShardsUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sh in shardsUIs) if (sh != null) sh.gameObject.SetActive(false);

        string[] hideObjNames = new string[]
        {
            "PlayerAvatarHUD", "PlayerHealthBar", "HealthBar", "PlayerHealthUI",
            "Player_Energy_Bars_HUD", "PlayerExpBarHUD", "ExpBarHUD", "ShardsCounter",
            "AugmentHUDBar", "PlayerStatPanel", "Btn_CharacterStats", "Btn_InGameBag", "Btn_Potion",
            "WaveBanner", "MatchTimerPanel", "Minimap_Compass_HUD", "RadarDisc", "DungeonBreakables",
            "RoomWaypointUI", "PvP_Arena_Border"
        };

        var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c == null || c == _pvpHudCanvas) continue;
            DisableTransformsByNameRecursive(c.transform, hideObjNames);
        }

        foreach (var n in hideObjNames)
        {
            var obj = GameObject.Find(n);
            if (obj != null) obj.SetActive(false);
        }
    }

    private void DisableTransformsByNameRecursive(Transform parent, string[] names)
    {
        if (parent == null) return;
        for (int i = 0; i < names.Length; i++)
        {
            if (parent.name == names[i])
            {
                parent.gameObject.SetActive(false);
                return;
            }
        }
        for (int i = 0; i < parent.childCount; i++)
        {
            DisableTransformsByNameRecursive(parent.GetChild(i), names);
        }
    }

    private IEnumerator MatchCountdownRoutine()
    {
        IsCombatActive = false;
        if (_localPlayer != null) _localPlayer.SetCanMove(false);

        if (_countdownBannerText != null)
        {
            _countdownBannerText.gameObject.SetActive(true);

            string[] steps = new string[] { "3", "2", "1", LocalizationManager.T("KHAI CHIẾN!", "FIGHT!") };
            for (int i = 0; i < steps.Length; i++)
            {
                _countdownBannerText.text = steps[i];
                _countdownBannerText.color = (i == steps.Length - 1)
                    ? new Color(1f, 0.35f, 0.2f, 1f)
                    : new Color(1f, 0.88f, 0.3f, 1f);

                SoundManager.EnsureInstance()?.PlayButtonClick();
                yield return new WaitForSecondsRealtime(0.70f);
            }

            _countdownBannerText.gameObject.SetActive(false);
        }

        if (_localPlayer != null) _localPlayer.SetCanMove(true);
        IsCombatActive = true;
    }

    public static Vector2 ClampInsideColosseum(Vector2 pos)
    {
        float cx = Mathf.Clamp(pos.x, -ARENA_HALF_WIDTH, ARENA_HALF_WIDTH);
        float cy = Mathf.Clamp(pos.y, -ARENA_HALF_HEIGHT + 0.4f, ARENA_HALF_HEIGHT);
        float sum = Mathf.Abs(cx) + Mathf.Abs(cy);
        if (sum > ARENA_OCTAGON_SUM)
        {
            float excess = (sum - ARENA_OCTAGON_SUM) * 0.5f;
            cx -= Mathf.Sign(cx) * excess;
            cy -= Mathf.Sign(cy) * excess;
        }
        return new Vector2(cx, cy);
    }

    private void Update()
    {
        if (!IsPvPModeActive && !IsCoopModeActive && (PvPNetworkManager.Instance == null || !PvPNetworkManager.Instance.IsRunning)) return;

        // Tự động tìm lại Local Player nếu chưa kịp nạp tại Awake/Start
        if (_localPlayer == null)
        {
            _localPlayer = FindAnyObjectByType<PlayerController>();
            if (_localPlayer != null)
            {
                _localHealth = _localPlayer.GetComponent<PlayerHealth>();
                _localCombat = _localPlayer.GetComponent<PlayerCombat>();
                _localEnergy = _localPlayer.GetComponent<PlayerEnergySystem>();
                _localDuneSkill = _localPlayer.GetComponent<DuneAssassinSkill>();
                _localSr = _localPlayer.GetComponent<SpriteRenderer>();
                _localAnim = _localPlayer.GetComponent<Animator>();
                _localRb = _localPlayer.GetComponent<Rigidbody2D>();
            }
        }

        if (IsPvPModeActive)
        {
            // Tự động ẩn Thanh Máu PvP khi người chơi đang mở bảng CÀI ĐẶT HỆ THỐNG (SettingsUI)
            RefreshHudVisibility();

            // 1. Giữ Local Player bên trong biên giới Võ Đài Bát Giác
            if (_localPlayer != null)
            {
                Vector3 p = _localPlayer.transform.position;
                Vector2 clamped = ClampInsideColosseum(new Vector2(p.x, p.y));
                if (Mathf.Abs(p.x - clamped.x) > 0.001f || Mathf.Abs(p.y - clamped.y) > 0.001f)
                {
                    _localPlayer.transform.position = new Vector3(clamped.x, clamped.y, 0f);
                    if (_localRb != null) _localRb.position = clamped;
                }
            }

            // 2. Cập nhật HUD Máu + Năng lượng & Đồng hồ
            UpdateVersusHUD();
        }

        // 3. Đồng bộ trạng thái nhân vật qua mạng 20Hz
        _networkSyncTimer -= Time.deltaTime;
        if (_networkSyncTimer <= 0f)
        {
            _networkSyncTimer = NETWORK_SYNC_INTERVAL;
            BroadcastLocalPlayerState();
        }

        // Đồng bộ thời gian trận đấu từ Chủ phòng sang tất cả thành viên 1Hz
        if (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsHost)
        {
            _timeSyncTimer -= Time.deltaTime;
            if (_timeSyncTimer <= 0f)
            {
                _timeSyncTimer = 1.0f;
                int elapsed = Mathf.RoundToInt(MatchTimerUI.CurrentElapsedTime);
                PvPNetworkManager.Instance.SendMatchTimeSync(elapsed);
            }
        }

        // 4. Phát hiện đòn đánh / kỹ năng của Local Player để gửi tín hiệu VFX sang máy đối thủ / đồng đội
        DetectAndBroadcastLocalActions();

        if (IsPvPModeActive)
        {
            if (!IsCombatActive || IsMatchEnded) return;

            // 5. Kiểm tra điều kiện Thắng / Thua hiệp đấu PvP
            if (_localHealth != null && (_localHealth.IsDead || _localHealth.CurrentHealth <= 0))
            {
                PvPNetworkManager.Instance?.SendMatchKoPacket(localPlayerDied: true);
                OnRoundFinished(localPlayerWon: false, "K.O");
                return;
            }

            if (PvPRemoteOpponent.Instance != null && PvPRemoteOpponent.Instance.CurrentHealth <= 0)
            {
                OnRoundFinished(localPlayerWon: true, "K.O");
                return;
            }

            // 6. Đếm ngược 90 giây
            _matchTimer -= Time.deltaTime;
            if (_timerText != null)
            {
                int sec = Mathf.Max(0, Mathf.CeilToInt(_matchTimer));
                _timerText.text = sec.ToString("00");
                if (sec <= 10) _timerText.color = new Color(1f, 0.35f, 0.35f);
            }

            if (_matchTimer <= 0f)
            {
                HandleTimeOut();
            }
        }
    }

    private void HandleTimeOut()
    {
        _matchTimer = 0f;
        float myPct = (_localHealth != null && _localHealth.MaxHealth > 0)
            ? (float)_localHealth.CurrentHealth / _localHealth.MaxHealth : 0f;
        float oppPct = (PvPRemoteOpponent.Instance != null && PvPRemoteOpponent.Instance.MaxHealth > 0)
            ? (float)PvPRemoteOpponent.Instance.CurrentHealth / PvPRemoteOpponent.Instance.MaxHealth : 0f;
        OnRoundFinished(localPlayerWon: myPct >= oppPct, "TIME OUT");
    }

    private void BroadcastLocalPlayerState()
    {
        if (_localPlayer == null || PvPNetworkManager.Instance == null || PvPNetworkManager.Instance.IsBotPracticeMode)
            return;

        Vector2 pos = _localPlayer.transform.position;
        Vector2 vel = _localRb != null ? _localRb.linearVelocity : Vector2.zero;
        bool flipX = _localSr != null && _localSr.flipX;
        Vector2 scale = _localPlayer.transform.localScale;

        float mx = 0f, my = 0f, spd = vel.magnitude;
        if (_localAnim != null && _localAnim.enabled && _localAnim.runtimeAnimatorController != null)
        {
            mx = _localAnim.GetFloat(MoveXHash);
            my = _localAnim.GetFloat(MoveYHash);
            spd = _localAnim.GetFloat(SpeedHash);
        }

        int curHp = _localHealth != null ? _localHealth.CurrentHealth : 800;
        int maxHp = _localHealth != null ? _localHealth.MaxHealth : 800;
        string sprName = (_localSr != null && _localSr.sprite != null) ? _localSr.sprite.name : "";
        string heroId = CharacterManager.EnsureInstance().GetSelectedCharacterId();

        // Kiểm tra xem nhân vật có đang đứng yên hoàn toàn không để tiết kiệm băng thông và chống nghẽn Reliable Queue
        bool isStationary = (vel.sqrMagnitude < 0.0004f) && (Vector2.SqrMagnitude(pos - _lastSentPos) < 0.0001f) && (curHp == _lastSentHp) && (sprName == _lastSentSpr);
        if (isStationary)
        {
            _idleHeartbeatTimer -= NETWORK_SYNC_INTERVAL;
            if (_idleHeartbeatTimer > 0f) return; // Chỉ gửi heartbeat 0.15s một lần khi đứng yên
            _idleHeartbeatTimer = 0.15f;
        }
        else
        {
            _idleHeartbeatTimer = 0f;
        }

        _lastSentPos = pos;
        _lastSentHp = curHp;
        _lastSentSpr = sprName;

        PvPNetworkManager.Instance.SendPlayerStatePacket(
            pos, vel, flipX, scale, mx, my, spd, curHp, maxHp, sprName, heroId);
    }

    public void BroadcastLocalSkillAction(string actionType = "ULTI")
    {
        if (_localPlayer == null || PvPNetworkManager.Instance == null || PvPNetworkManager.Instance.IsBotPracticeMode)
            return;

        if (actionType == "ULTI") _lastUltiSendTime = Time.time;

        Vector2 pos = _localPlayer.transform.position;
        Vector2 facing = (_localSr != null && _localSr.flipX) ? Vector2.left : Vector2.right;
        if (_localPlayer != null)
        {
            Vector2 aim = _localPlayer.GetFacingDirection();
            if (aim.sqrMagnitude > 0.01f) facing = aim.normalized;
        }
        if (!IsCoopModeActive && PvPRemoteOpponent.Instance != null)
        {
            Vector2 toOpp = (Vector2)PvPRemoteOpponent.Instance.transform.position - pos;
            if (toOpp.sqrMagnitude > 0.05f) facing = toOpp.normalized;
        }

        PvPNetworkManager.Instance.SendActionVfxPacket(actionType, facing, pos);
        BroadcastLocalPlayerState();
    }

    private void DetectAndBroadcastLocalActions()
    {
        if (_localPlayer == null || PvPNetworkManager.Instance == null || PvPNetworkManager.Instance.IsBotPracticeMode)
            return;

        Vector2 pos = _localPlayer.transform.position;
        Vector2 facing = (_localSr != null && _localSr.flipX) ? Vector2.left : Vector2.right;
        if (_localPlayer != null)
        {
            Vector2 aim = _localPlayer.GetFacingDirection();
            if (aim.sqrMagnitude > 0.01f) facing = aim.normalized;
        }
        if (!IsCoopModeActive && PvPRemoteOpponent.Instance != null)
        {
            Vector2 toOpp = (Vector2)PvPRemoteOpponent.Instance.transform.position - pos;
            if (toOpp.sqrMagnitude > 0.05f) facing = toOpp.normalized;
        }

        if (_localCombat != null && _localCombat.LastAttackTime > _lastRecordedAttackTime + 0.001f)
        {
            _lastRecordedAttackTime = _localCombat.LastAttackTime;
            PvPNetworkManager.Instance.SendActionVfxPacket("ATK", facing, pos);
            BroadcastLocalPlayerState(); // Phát ngay gói tin trạng thái vị trí tại khung hình ra đòn
        }

        var mech = _localPlayer.GetComponent<AbyssalMechSkill>();
        if (mech != null && mech.enabled && mech.IsAttacking && Time.time > _lastRecordedAttackTime + 0.35f)
        {
            _lastRecordedAttackTime = Time.time;
            PvPNetworkManager.Instance.SendActionVfxPacket("ATK", facing, pos);
            BroadcastLocalPlayerState();
        }

        if (mech != null && mech.enabled && mech.IsExecutingSkill && Time.time > _lastUltiSendTime + 2.0f)
        {
            _lastUltiSendTime = Time.time;
            PvPNetworkManager.Instance.SendActionVfxPacket("ULTI", facing, pos);
            BroadcastLocalPlayerState();
        }
    }

    public void OnLocalPlayerReceivePvPDamage(int damage, bool isCrit, Vector2 knockback, float stunDur)
    {
        // Chế độ Co-op tuyệt đối miễn nhiễm sát thương đối kháng / cấm Friendly Fire từ đồng đội
        if (IsCoopModeActive) return;
        if (!IsCombatActive || IsMatchEnded || _localHealth == null || _localHealth.IsDead) return;

        int safeDmg = Mathf.Clamp(damage, 1, Mathf.RoundToInt(_localHealth.MaxHealth * 0.30f));
        _localHealth.TakeDamage(safeDmg);
        DamagePopup.Create(_localPlayer.transform.position, safeDmg, isCrit ? DamageType.Magic : DamageType.Physical, isCrit);

        if (_localRb != null && knockback.sqrMagnitude > 0.01f)
        {
            _localRb.AddForce(knockback, ForceMode2D.Impulse);
        }
        if (stunDur > 0.05f && _localPlayer != null)
        {
            _localPlayer.ApplyStun(stunDur);
        }

        BroadcastLocalPlayerState();

        if (_localHealth.IsDead || _localHealth.CurrentHealth <= 0)
        {
            PvPNetworkManager.Instance?.SendMatchKoPacket(localPlayerDied: true);
            OnRoundFinished(localPlayerWon: false, "K.O");
        }
    }

    public void OnRemoteMatchEndSignal(bool localWon)
    {
        if (!IsMatchEnded)
        {
            OnRoundFinished(localWon, "K.O");
        }
    }

    public void OnOpponentDisconnectedWin()
    {
        if (!IsMatchEnded)
        {
            DamagePopup.CreateText(
                _localPlayer != null ? _localPlayer.transform.position + Vector3.up * 1.5f : Vector3.zero,
                LocalizationManager.T("ĐỐI THỦ ĐÃ THOÁT TRẬN!", "OPPONENT DISCONNECTED!"),
                new Color(1f, 0.85f, 0.25f));
            EndMatch(localPlayerWon: true);
        }
    }

    public void OnRoundFinished(bool localPlayerWon, string reason)
    {
        if (IsMatchEnded || _isRoundEnding) return;
        _isRoundEnding = true;
        IsCombatActive = false;

        if (localPlayerWon) _localRoundWins++;
        else _remoteRoundWins++;

        RefreshRoundScoreHUD();

        // Kiểm tra xem đã có ai đạt đủ số hiệp thắng để định đoạt cả trận chưa
        if (_localRoundWins >= _requiredWins)
        {
            EndMatch(localPlayerWon: true);
            return;
        }
        if (_remoteRoundWins >= _requiredWins)
        {
            EndMatch(localPlayerWon: false);
            return;
        }

        // Chưa bên nào đủ hiệp thắng -> Chuyển sang Round tiếp theo
        StartCoroutine(NextRoundTransitionRoutine(localPlayerWon, reason));
    }

    private IEnumerator NextRoundTransitionRoutine(bool localWonRound, string reason)
    {
        if (_countdownBannerText != null)
        {
            _countdownBannerText.gameObject.SetActive(true);
            string bannerTitle = localWonRound
                ? LocalizationManager.T($"HIỆP {_currentRound}: CHIẾN THẮNG!", $"ROUND {_currentRound}: VICTORY!")
                : LocalizationManager.T($"HIỆP {_currentRound}: THẤT BẠI!", $"ROUND {_currentRound}: DEFEAT!");
            _countdownBannerText.text = bannerTitle;
            _countdownBannerText.color = localWonRound ? new Color(0.35f, 1f, 0.45f) : new Color(1f, 0.35f, 0.3f);
        }

        yield return new WaitForSecondsRealtime(2.2f);

        _currentRound++;
        _matchTimer = 90f;
        _isRoundEnding = false;

        bool isP1 = PvPNetworkManager.Instance.IsLocalPlayerOne();
        string myHero = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        float myFootOffset = GetHeroFootOffsetY(myHero);
        Vector3 mySpawn = isP1 
            ? new Vector3(-3.8f, 0.2f - myFootOffset, 0f) 
            : new Vector3(3.8f, 0.2f - myFootOffset, 0f);

        if (_localPlayer != null)
        {
            _localPlayer.transform.position = mySpawn;
            if (_localRb != null)
            {
                _localRb.position = mySpawn;
                _localRb.linearVelocity = Vector2.zero;
            }
            if (_localSr != null) _localSr.flipX = !isP1;
        }

        if (_localHealth != null)
        {
            _localHealth.Revive();
            int pvpMaxHp = PvPNetworkManager.GetNormalizedPvPMaxHealth(myHero);
            _localHealth.SetBaseMaxHealth(pvpMaxHp);
            _localHealth.Heal(pvpMaxHp * 2);
        }

        string oppHero = PvPNetworkManager.Instance.OpponentHeroId;
        float oppFootOffset = GetHeroFootOffsetY(oppHero);
        Vector3 oppSpawn = isP1 
            ? new Vector3(3.8f, 0.2f - oppFootOffset, 0f) 
            : new Vector3(-3.8f, 0.2f - oppFootOffset, 0f);

        if (PvPRemoteOpponent.Instance != null)
        {
            PvPRemoteOpponent.Instance.ResetForNewRound(oppSpawn);
        }

        RefreshRoundScoreHUD();

        if (_countdownBannerText != null)
        {
            _countdownBannerText.text = LocalizationManager.T($"HIỆP {_currentRound}", $"ROUND {_currentRound}");
            _countdownBannerText.color = new Color(1f, 0.88f, 0.32f);
            yield return new WaitForSecondsRealtime(1.0f);

            _countdownBannerText.text = LocalizationManager.T("KHAI CHIẾN!", "FIGHT!");
            _countdownBannerText.color = new Color(1f, 0.35f, 0.2f);
            SoundManager.EnsureInstance()?.PlayButtonClick();
            yield return new WaitForSecondsRealtime(0.6f);
            _countdownBannerText.gameObject.SetActive(false);
        }

        IsCombatActive = true;
    }

    private void RefreshRoundScoreHUD()
    {
        if (_roundScoreText != null)
        {
            string fmtName = (PvPNetworkManager.SelectedRoundFormat == PvPNetworkManager.PvPRoundFormat.Bo1_SingleRound)
                ? "BO1"
                : ((PvPNetworkManager.SelectedRoundFormat == PvPNetworkManager.PvPRoundFormat.Bo5_BestOf5) ? "BO5" : "BO3");

            string roundLabel = LocalizationManager.T($"HIỆP {_currentRound}", $"ROUND {_currentRound}");
            _roundScoreText.text = $"<color=#FFE066>{roundLabel}</color>  <size=15><color=#55FF88>{_localRoundWins}</color> - <color=#FF6666>{_remoteRoundWins}</color></size>  <size=12><color=#88AACC>({fmtName})</color></size>";
        }
    }

    public void EndMatch(bool localPlayerWon)
    {
        if (IsMatchEnded) return;
        IsMatchEnded = true;
        IsCombatActive = false;

        int rewardShards = localPlayerWon ? 1500 : 300;
        AccountDatabase.AddAethelShards(rewardShards);

        string myHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? CharacterManager.CHAR_VAELSTRIDER;
        int heroExpReward = localPlayerWon ? 450 : 180;
        AccountDatabase.AddHeroExp(myHero, heroExpReward, out int newHeroLv, out bool heroLvlUp);

        var acc = AccountDatabase.GetActiveAccount();
        if (acc != null)
        {
            if (acc.pvpRating <= 0) acc.pvpRating = 1000;
            if (localPlayerWon)
            {
                acc.pvpWins++;
                acc.pvpRating += 25;
            }
            else
            {
                acc.pvpLosses++;
                acc.pvpRating = Mathf.Max(800, acc.pvpRating - 12);
            }
            PlayFabClientService.EnsureInstance().SubmitPvPScoreToLeaderboard(acc.pvpRating, acc.pvpWins);
        }

        AccountDatabase.SaveActiveSessionToDatabase();

        if (localPlayerWon)
            SoundManager.EnsureInstance()?.PlayHeavenlyAscension();
        else
            SoundManager.EnsureInstance()?.PlayPlayerHurt();

        ShowResultModal(localPlayerWon, rewardShards);
    }

    // =========================================================================
    // GIAO DIỆN THANH MÁU ĐỐI KHÁNG ĐÔI (TOP VERSUS HUD) & KẾT QUẢ TRẬN ĐẤU
    // =========================================================================
    private void BuildVersusHUD()
    {
        if (_pvpHudCanvas != null && _pvpHudCanvas.gameObject != null)
        {
            Destroy(_pvpHudCanvas.gameObject);
        }

        GameObject canvasObj = new GameObject("PvP_VersusHUD_Canvas");
        _pvpHudCanvas = canvasObj.AddComponent<Canvas>();
        _pvpHudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _pvpHudCanvas.sortingOrder = 85; // Nằm dưới các Modal toàn màn hình (Settings, Inventory)

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Top Center Panel
        _topBarObj = new GameObject("TopVersusBar", typeof(RectTransform));
        _topBarObj.transform.SetParent(canvasObj.transform, false);
        RectTransform topRt = _topBarObj.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0.5f, 1f);
        topRt.anchorMax = new Vector2(0.5f, 1f);
        topRt.pivot = new Vector2(0.5f, 1f);
        topRt.anchoredPosition = new Vector2(0f, -12f);
        topRt.sizeDelta = new Vector2(1260f, 96f);

        // Left Fighter HP + Mana Bar + Portrait (BẠN)
        CreateFighterHudBar(
            _topBarObj.transform, isLeft: true, new Vector2(-345f, -44f),
            new Color(0.18f, 0.85f, 0.42f, 1f),
            out _leftHpFill, out _leftMpFill, out _leftPortraitImg, out _leftNameText, out _leftHpValueText);

        // Right Fighter HP Bar + Portrait (ĐỐI THỦ)
        CreateFighterHudBar(
            _topBarObj.transform, isLeft: false, new Vector2(345f, -44f),
            new Color(0.92f, 0.22f, 0.25f, 1f),
            out _rightHpFill, out _, out _rightPortraitImg, out _rightNameText, out _rightHpValueText);

        // Center VS & Timer Box
        GameObject centerBox = new GameObject("CenterTimerBox", typeof(RectTransform), typeof(Image));
        centerBox.transform.SetParent(_topBarObj.transform, false);
        RectTransform cRt = centerBox.GetComponent<RectTransform>();
        cRt.anchoredPosition = new Vector2(0f, -44f);
        cRt.sizeDelta = new Vector2(180f, 94f);
        Image cImg = centerBox.GetComponent<Image>();
        cImg.color = new Color(0.08f, 0.09f, 0.14f, 0.96f);

        _timerText = CreateHudText(centerBox.transform, "90", 28f, new Vector2(0f, 22f), new Vector2(140f, 32f), new Color(1f, 0.88f, 0.32f));
        _roundScoreText = CreateHudText(centerBox.transform, "", 14f, new Vector2(0f, -4f), new Vector2(175f, 22f), Color.white);
        _modeSubText = CreateHudText(centerBox.transform,
            PvPNetworkManager.Instance.IsBotPracticeMode ? "AI RIVAL" : "PVP ASIA",
            12f, new Vector2(0f, -28f), new Vector2(150f, 18f), new Color(0.75f, 0.85f, 1f));
        RefreshRoundScoreHUD();

        // Nút Thoát PvP đặt ở góc trên bên phải (nằm ngay bên trái nút Bánh Răng Cài Đặt)
        _exitBtnObj = new GameObject("Btn_ExitPvP", typeof(RectTransform), typeof(Image), typeof(Button));
        _exitBtnObj.transform.SetParent(canvasObj.transform, false);
        RectTransform exRt = _exitBtnObj.GetComponent<RectTransform>();
        exRt.anchorMin = new Vector2(1f, 1f);
        exRt.anchorMax = new Vector2(1f, 1f);
        exRt.pivot = new Vector2(1f, 1f);
        exRt.anchoredPosition = new Vector2(-118f, -24f);
        exRt.sizeDelta = new Vector2(170f, 56f);
        Image exImg = _exitBtnObj.GetComponent<Image>();
        exImg.color = new Color(0.52f, 0.14f, 0.16f, 0.95f);
        Button exBtn = _exitBtnObj.GetComponent<Button>();
        exBtn.onClick.AddListener(() =>
        {
            SoundManager.EnsureInstance()?.PlayButtonClick();
            PvPNetworkManager.EnsureInstance().LeaveMatchAndReturnToMenu();
        });
        CreateHudText(_exitBtnObj.transform, LocalizationManager.T("THOÁT PVP", "LEAVE PVP"), 19f, Vector2.zero, new Vector2(156f, 48f), Color.white);

        // Banner đếm ngược 3 - 2 - 1 - KHAI CHIẾN giữa màn hình
        _countdownBannerText = CreateHudText(canvasObj.transform, "3", 92f, new Vector2(0f, 80f), new Vector2(900f, 140f), new Color(1f, 0.88f, 0.3f));

        // Modal Kết Quả Thắng / Thua
        BuildResultModal(canvasObj.transform);

        GameFontManager.ApplyFontsGlobally(canvasObj);
    }

    private static Sprite _solidWhiteSprite;
    public static Sprite GetSolidWhiteSprite()
    {
        if (_solidWhiteSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            _solidWhiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        return _solidWhiteSprite;
    }

    private void CreateFighterHudBar(
        Transform parent, bool isLeft, Vector2 anchoredPos, Color barColor,
        out Image hpFillImg, out Image mpFillImg, out Image portraitImg, out TextMeshProUGUI nameTmp, out TextMeshProUGUI hpValTmp)
    {
        GameObject container = new GameObject(isLeft ? "LeftFighterBar" : "RightFighterBar", typeof(RectTransform), typeof(Image));
        container.transform.SetParent(parent, false);
        RectTransform rt = container.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(540f, 78f);
        Image bg = container.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.07f, 0.11f, 0.94f);

        // Khung Avatar Chân Dung Tướng ở mép ngoài (Trái hoặc Phải) giống Bước 8 trong lưu đồ
        GameObject avatarFrame = new GameObject("AvatarFrame", typeof(RectTransform), typeof(Image));
        avatarFrame.transform.SetParent(container.transform, false);
        RectTransform avRt = avatarFrame.GetComponent<RectTransform>();
        avRt.anchoredPosition = new Vector2(isLeft ? -230f : 230f, 0f);
        avRt.sizeDelta = new Vector2(70f, 70f);
        avatarFrame.GetComponent<Image>().color = new Color(0.14f, 0.17f, 0.26f, 1f);

        GameObject portObj = new GameObject("PortraitSprite", typeof(RectTransform), typeof(Image));
        portObj.transform.SetParent(avatarFrame.transform, false);
        RectTransform pRt = portObj.GetComponent<RectTransform>();
        pRt.sizeDelta = new Vector2(62f, 62f);
        pRt.localScale = new Vector3(isLeft ? 1f : -1f, 1f, 1f);
        portraitImg = portObj.GetComponent<Image>();
        portraitImg.preserveAspect = true;
        portraitImg.raycastTarget = false;

        float leftInset = isLeft ? 80f : 8f;
        float rightInset = isLeft ? -8f : -80f;

        // --- KHUNG VIỀN NGOÀI THANH MÁU CHỮ NHẬT CHUẨN ĐỐI KHÁNG ---
        GameObject hpFrame = new GameObject("HP_BarFrame", typeof(RectTransform), typeof(Image));
        hpFrame.transform.SetParent(container.transform, false);
        RectTransform hpFrameRt = hpFrame.GetComponent<RectTransform>();
        hpFrameRt.anchorMin = new Vector2(0f, 0.24f);
        hpFrameRt.anchorMax = new Vector2(1f, 0.68f);
        hpFrameRt.offsetMin = new Vector2(leftInset, 0f);
        hpFrameRt.offsetMax = new Vector2(rightInset, 0f);
        Image hpFrameImg = hpFrame.GetComponent<Image>();
        hpFrameImg.color = new Color(0.20f, 0.24f, 0.34f, 1f); // Viền kim loại sắc nét

        // Nền tối bên trong thanh máu (khi mất máu sẽ thấy khoảng trống tối này)
        GameObject hpTrack = new GameObject("HP_TrackBg", typeof(RectTransform), typeof(Image));
        hpTrack.transform.SetParent(hpFrame.transform, false);
        RectTransform hpTrackRt = hpTrack.GetComponent<RectTransform>();
        hpTrackRt.anchorMin = Vector2.zero;
        hpTrackRt.anchorMax = Vector2.one;
        hpTrackRt.offsetMin = new Vector2(2f, 2f);
        hpTrackRt.offsetMax = new Vector2(-2f, -2f);
        Image hpTrackImg = hpTrack.GetComponent<Image>();
        hpTrackImg.color = new Color(0.12f, 0.04f, 0.05f, 0.95f);

        // Thanh Máu Chữ Nhật Đặc (Solid Rectangle HP Fill - Sử dụng 1x1 flat sprite để Image.Type.Filled co rút chính xác)
        GameObject fillObj = new GameObject("HP_Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(hpTrack.transform, false);
        RectTransform fRt = fillObj.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = Vector2.zero;
        fRt.offsetMax = Vector2.zero;
        hpFillImg = fillObj.GetComponent<Image>();
        hpFillImg.sprite = GetSolidWhiteSprite(); // Sprite 1x1 phẳng đặc sắc nét, fillAmount hoạt động 100%
        hpFillImg.type = Image.Type.Filled;
        hpFillImg.fillMethod = Image.FillMethod.Horizontal;
        hpFillImg.fillOrigin = isLeft ? (int)Image.OriginHorizontal.Left : (int)Image.OriginHorizontal.Right;
        hpFillImg.fillAmount = 1f;
        hpFillImg.color = barColor;

        // Vạch Máu (Segmented Notches / Tick Marks chia đoạn 10 nấc chuẩn game đối kháng)
        GameObject ticksOverlay = new GameObject("HP_TicksOverlay", typeof(RectTransform));
        ticksOverlay.transform.SetParent(hpTrack.transform, false);
        RectTransform ticksRt = ticksOverlay.GetComponent<RectTransform>();
        ticksRt.anchorMin = Vector2.zero;
        ticksRt.anchorMax = Vector2.one;
        ticksRt.offsetMin = Vector2.zero;
        ticksRt.offsetMax = Vector2.zero;

        for (int i = 1; i <= 9; i++)
        {
            float pct = i * 0.1f;
            GameObject tickObj = new GameObject("Tick_" + i, typeof(RectTransform), typeof(Image));
            tickObj.transform.SetParent(ticksOverlay.transform, false);
            RectTransform tRt = tickObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(pct, 0f);
            tRt.anchorMax = new Vector2(pct, 1f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.sizeDelta = new Vector2(2f, 0f);
            tRt.anchoredPosition = Vector2.zero;
            Image tImg = tickObj.GetComponent<Image>();
            tImg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f); // Vạch chia tối sắc cạnh
            tImg.raycastTarget = false;
        }

        // --- KHUNG THANH NĂNG LƯỢNG (MP FILL) CHỮ NHẬT GỌN ĐẸP ---
        GameObject mpFrame = new GameObject("MP_BarFrame", typeof(RectTransform), typeof(Image));
        mpFrame.transform.SetParent(container.transform, false);
        RectTransform mpFrameRt = mpFrame.GetComponent<RectTransform>();
        mpFrameRt.anchorMin = new Vector2(0f, 0.06f);
        mpFrameRt.anchorMax = new Vector2(1f, 0.18f);
        mpFrameRt.offsetMin = new Vector2(leftInset, 0f);
        mpFrameRt.offsetMax = new Vector2(rightInset, 0f);
        mpFrame.GetComponent<Image>().color = new Color(0.14f, 0.18f, 0.26f, 1f);

        GameObject mpObj = new GameObject("MP_Fill", typeof(RectTransform), typeof(Image));
        mpObj.transform.SetParent(mpFrame.transform, false);
        RectTransform mpRt = mpObj.GetComponent<RectTransform>();
        mpRt.anchorMin = Vector2.zero;
        mpRt.anchorMax = Vector2.one;
        mpRt.offsetMin = new Vector2(1f, 1f);
        mpRt.offsetMax = new Vector2(-1f, -1f);
        mpFillImg = mpObj.GetComponent<Image>();
        mpFillImg.sprite = GetSolidWhiteSprite(); // Chuẩn khối chữ nhật, fillAmount co rút chính xác
        mpFillImg.type = Image.Type.Filled;
        mpFillImg.fillMethod = Image.FillMethod.Horizontal;
        mpFillImg.fillOrigin = isLeft ? (int)Image.OriginHorizontal.Left : (int)Image.OriginHorizontal.Right;
        mpFillImg.fillAmount = 1f;
        mpFillImg.color = isLeft ? new Color(0.18f, 0.72f, 1.0f, 1f) : new Color(0.85f, 0.55f, 0.20f, 0.90f);

        float textCenterX = isLeft ? 36f : -36f;
        nameTmp = CreateHudText(container.transform, isLeft ? "BẠN" : "ĐỐI THỦ", 17f, new Vector2(textCenterX, 22f), new Vector2(430f, 24f), new Color(0.98f, 0.92f, 0.72f));
        nameTmp.alignment = isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;

        hpValTmp = CreateHudText(container.transform, "800 / 800 HP", 14.5f, new Vector2(textCenterX, -4f), new Vector2(430f, 22f), Color.white);
        hpValTmp.alignment = isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
    }

    private void BuildResultModal(Transform canvasTransform)
    {
        _resultModalObj = new GameObject("PvP_ResultModal", typeof(RectTransform), typeof(Image));
        _resultModalObj.transform.SetParent(canvasTransform, false);
        RectTransform mRt = _resultModalObj.GetComponent<RectTransform>();
        mRt.anchorMin = Vector2.zero;
        mRt.anchorMax = Vector2.one;
        mRt.offsetMin = Vector2.zero;
        mRt.offsetMax = Vector2.zero;
        Image overlay = _resultModalObj.GetComponent<Image>();
        overlay.color = new Color(0.02f, 0.02f, 0.05f, 0.90f);

        // Khung Kết Quả Trận Đấu (Bước 9: YOU WIN! + 2 Nhân vật Trái/Phải + Bảng Thưởng ở giữa)
        GameObject card = new GameObject("ResultCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(_resultModalObj.transform, false);
        RectTransform cRt = card.GetComponent<RectTransform>();
        cRt.sizeDelta = new Vector2(1180f, 680f);
        card.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.14f, 0.98f);

        _resultTitleText = CreateHudText(card.transform, "YOU WIN!", 68f, new Vector2(0f, 250f), new Vector2(960f, 90f), new Color(1f, 0.86f, 0.25f));

        // Nhân vật bên Trái (BẠN)
        GameObject leftBox = new GameObject("ResultLeftFighter", typeof(RectTransform), typeof(Image));
        leftBox.transform.SetParent(card.transform, false);
        RectTransform lRt = leftBox.GetComponent<RectTransform>();
        lRt.anchoredPosition = new Vector2(-390f, 10f);
        lRt.sizeDelta = new Vector2(310f, 340f);
        leftBox.GetComponent<Image>().color = new Color(0.09f, 0.13f, 0.22f, 0.96f);

        GameObject lSprObj = new GameObject("LeftHeroImg", typeof(RectTransform), typeof(Image));
        lSprObj.transform.SetParent(leftBox.transform, false);
        RectTransform lsRt = lSprObj.GetComponent<RectTransform>();
        lsRt.anchoredPosition = new Vector2(0f, 25f);
        lsRt.sizeDelta = new Vector2(210f, 210f);
        _resultLeftHeroImg = lSprObj.GetComponent<Image>();
        _resultLeftHeroImg.preserveAspect = true;

        _resultLeftNameText = CreateHudText(leftBox.transform, "TruongHung", 26f, new Vector2(0f, -125f), new Vector2(280f, 40f), new Color(0.45f, 1f, 0.65f));

        // Nhân vật bên Phải (ĐỐI THỦ)
        GameObject rightBox = new GameObject("ResultRightFighter", typeof(RectTransform), typeof(Image));
        rightBox.transform.SetParent(card.transform, false);
        RectTransform rBoxRt = rightBox.GetComponent<RectTransform>();
        rBoxRt.anchoredPosition = new Vector2(390f, 10f);
        rBoxRt.sizeDelta = new Vector2(310f, 340f);
        rightBox.GetComponent<Image>().color = new Color(0.18f, 0.09f, 0.12f, 0.96f);

        GameObject rSprObj = new GameObject("RightHeroImg", typeof(RectTransform), typeof(Image));
        rSprObj.transform.SetParent(rightBox.transform, false);
        RectTransform rsRt = rSprObj.GetComponent<RectTransform>();
        rsRt.anchoredPosition = new Vector2(0f, 25f);
        rsRt.sizeDelta = new Vector2(210f, 210f);
        rsRt.localScale = new Vector3(-1f, 1f, 1f);
        _resultRightHeroImg = rSprObj.GetComponent<Image>();
        _resultRightHeroImg.preserveAspect = true;

        _resultRightNameText = CreateHudText(rightBox.transform, "DarkRider", 26f, new Vector2(0f, -125f), new Vector2(280f, 40f), new Color(1f, 0.55f, 0.55f));

        // Hộp Phần Thưởng Chính Giữa (Reward Box)
        GameObject rewardBox = new GameObject("CenterRewardBox", typeof(RectTransform), typeof(Image));
        rewardBox.transform.SetParent(card.transform, false);
        RectTransform rwRt = rewardBox.GetComponent<RectTransform>();
        rwRt.anchoredPosition = new Vector2(0f, 15f);
        rwRt.sizeDelta = new Vector2(410f, 250f);
        rewardBox.GetComponent<Image>().color = new Color(0.11f, 0.13f, 0.20f, 0.98f);

        CreateHudText(rewardBox.transform, LocalizationManager.T("THƯỞNG TRẬN ĐẤU", "MATCH REWARDS"), 26f, new Vector2(0f, 80f), new Vector2(370f, 40f), new Color(1f, 0.88f, 0.35f));
        _resultRewardText = CreateHudText(rewardBox.transform, "+1,500 LINH HỒN\n+120 EXP DANH VỌNG", 24f, new Vector2(0f, -15f), new Vector2(370f, 120f), new Color(0.45f, 1f, 0.68f));

        // Nút Quay Lại (Về Sảnh) bên trái
        GameObject menuBtnObj = new GameObject("Btn_ReturnMenu", typeof(RectTransform), typeof(Image), typeof(Button));
        menuBtnObj.transform.SetParent(card.transform, false);
        RectTransform mnRt = menuBtnObj.GetComponent<RectTransform>();
        mnRt.anchoredPosition = new Vector2(-190f, -245f);
        mnRt.sizeDelta = new Vector2(320f, 76f);
        menuBtnObj.GetComponent<Image>().color = new Color(0.22f, 0.25f, 0.36f, 1f);
        menuBtnObj.GetComponent<Button>().onClick.AddListener(() =>
        {
            SoundManager.EnsureInstance()?.PlayButtonClick();
            PvPNetworkManager.EnsureInstance().LeaveMatchAndReturnToMenu();
        });
        CreateHudText(menuBtnObj.transform, LocalizationManager.T("< QUAY LẠI (SẢNH)", "< RETURN LOBBY"), 24f, Vector2.zero, new Vector2(290f, 60f), Color.white);

        // Nút Tiếp Tục (Tái Đấu) bên phải - Quản lý đồng thuận 2 chiều trong 10 giây
        GameObject rematchBtnObj = new GameObject("Btn_Rematch", typeof(RectTransform), typeof(Image), typeof(Button));
        rematchBtnObj.transform.SetParent(card.transform, false);
        RectTransform rRt = rematchBtnObj.GetComponent<RectTransform>();
        rRt.anchoredPosition = new Vector2(190f, -245f);
        rRt.sizeDelta = new Vector2(340f, 76f);
        _rematchBtnBg = rematchBtnObj.GetComponent<Image>();
        _rematchBtnBg.color = new Color(0.85f, 0.50f, 0.14f, 1f);
        _rematchBtn = rematchBtnObj.GetComponent<Button>();
        _rematchBtn.onClick.AddListener(OnRematchButtonClicked);
        _rematchBtnText = CreateHudText(rematchBtnObj.transform, LocalizationManager.T("TIẾP TỤC (TÁI ĐẤU) >", "CONTINUE (REMATCH) >"), 22f, Vector2.zero, new Vector2(320f, 60f), Color.white);

        _resultModalObj.SetActive(false);
    }

    private void OnRematchButtonClicked()
    {
        SoundManager.EnsureInstance()?.PlayButtonClick();

        if (PvPNetworkManager.Instance != null && PvPNetworkManager.Instance.IsBotPracticeMode)
        {
            // Trong chế độ Đấu Tập Bot: Tái đấu ngay lập tức không cần chờ
            RestartArenaMatch();
            return;
        }

        if (_remoteWantsRematch)
        {
            // Đối thủ đã bấm trước đó -> Đồng ý tái đấu ngay!
            if (_rematchTimerRoutine != null)
            {
                StopCoroutine(_rematchTimerRoutine);
                _rematchTimerRoutine = null;
            }
            PvPNetworkManager.Instance?.SendRematchPacket("ACCEPT");
            RestartArenaMatch();
            return;
        }

        // Người chơi bấm trước -> Gửi yêu cầu tái đấu và đếm ngược 10s chờ đối thủ
        _localWantsRematch = true;
        if (_rematchBtn != null) _rematchBtn.interactable = false;
        PvPNetworkManager.Instance?.SendRematchPacket("WANT");

        if (_rematchTimerRoutine != null) StopCoroutine(_rematchTimerRoutine);
        _rematchTimerRoutine = StartCoroutine(RematchCountdownRoutine());
    }

    private IEnumerator RematchCountdownRoutine()
    {
        float timeLeft = REMATCH_WAIT_SECONDS;
        while (timeLeft > 0f)
        {
            if (_rematchBtnText != null)
            {
                int sec = Mathf.CeilToInt(timeLeft);
                _rematchBtnText.text = LocalizationManager.T($"CHỜ ĐỐI THỦ... ({sec}S)", $"WAITING ({sec}S)...");
            }
            yield return new WaitForSecondsRealtime(1.0f);
            timeLeft -= 1.0f;
        }

        // Hết 10 giây mà đối thủ không bấm -> Hủy và ẩn/mất nút tái đấu
        _localWantsRematch = false;
        PvPNetworkManager.Instance?.SendRematchPacket("CANCEL");
        if (_rematchBtn != null)
        {
            _rematchBtn.gameObject.SetActive(false);
        }
        _rematchTimerRoutine = null;
    }

    public void OnRemoteRematchRequested()
    {
        _remoteWantsRematch = true;

        if (_localWantsRematch)
        {
            // Cả 2 cùng đồng ý tái đấu!
            if (_rematchTimerRoutine != null)
            {
                StopCoroutine(_rematchTimerRoutine);
                _rematchTimerRoutine = null;
            }
            PvPNetworkManager.Instance?.SendRematchPacket("ACCEPT");
            RestartArenaMatch();
        }
        else
        {
            // Đối thủ muốn tái đấu trước -> Đổi màu nút sang xanh lá nổi bật mời người chơi bấm
            if (_rematchBtn != null)
            {
                _rematchBtn.gameObject.SetActive(true);
                _rematchBtn.interactable = true;
            }
            if (_rematchBtnBg != null)
            {
                _rematchBtnBg.color = new Color(0.18f, 0.72f, 0.35f, 1f);
            }
            if (_rematchBtnText != null)
            {
                _rematchBtnText.text = LocalizationManager.T("ĐỐI THỦ XIN TÁI ĐẤU! (BẤM)", "REMATCH REQUESTED! (TAP)");
            }
        }
    }

    public void OnRemoteRematchAccepted()
    {
        if (_rematchTimerRoutine != null)
        {
            StopCoroutine(_rematchTimerRoutine);
            _rematchTimerRoutine = null;
        }
        RestartArenaMatch();
    }

    public void OnRemoteRematchCancelled()
    {
        _remoteWantsRematch = false;
        if (!_localWantsRematch)
        {
            if (_rematchBtnText != null)
            {
                _rematchBtnText.text = LocalizationManager.T("ĐỐI THỦ ĐÃ TỪ CHỐI", "OPPONENT DECLINED");
            }
            if (_rematchBtn != null)
            {
                _rematchBtn.gameObject.SetActive(false);
            }
        }
    }

    private void ShowResultModal(bool localWon, int rewardShards)
    {
        if (_resultModalObj == null) return;
        _localWantsRematch = false;
        _remoteWantsRematch = false;
        if (_rematchTimerRoutine != null)
        {
            StopCoroutine(_rematchTimerRoutine);
            _rematchTimerRoutine = null;
        }
        if (_rematchBtn != null)
        {
            _rematchBtn.gameObject.SetActive(true);
            _rematchBtn.interactable = true;
        }
        if (_rematchBtnBg != null)
        {
            _rematchBtnBg.color = new Color(0.85f, 0.50f, 0.14f, 1f);
        }
        if (_rematchBtnText != null)
        {
            _rematchBtnText.text = LocalizationManager.T("TIẾP TỤC (TÁI ĐẤU) >", "CONTINUE (REMATCH) >");
        }
        _resultModalObj.SetActive(true);

        if (_resultTitleText != null)
        {
            _resultTitleText.text = localWon
                ? LocalizationManager.T("YOU WIN! (CHIẾN THẮNG!)", "YOU WIN!")
                : LocalizationManager.T("DEFEAT (THẤT BẠI!)", "DEFEAT!");
            _resultTitleText.color = localWon ? new Color(1f, 0.86f, 0.25f) : new Color(1f, 0.35f, 0.35f);
        }

        string myHeroId = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        int expGain = localWon ? 450 : 180;
        int heroLv = AccountDatabase.GetHeroLevel(myHeroId);
        string fmtName = (PvPNetworkManager.SelectedRoundFormat == PvPNetworkManager.PvPRoundFormat.Bo1_SingleRound)
            ? "BO1"
            : ((PvPNetworkManager.SelectedRoundFormat == PvPNetworkManager.PvPRoundFormat.Bo5_BestOf5) ? "BO5" : "BO3");

        if (_resultRewardText != null)
        {
            _resultRewardText.text = LocalizationManager.T(
                $"TỈ SỐ HIỆP: {_localRoundWins} - {_remoteRoundWins} ({fmtName})\nLINH HỒN: +{rewardShards:N0}\nEXP TƯỚNG (LV. {heroLv}): +{expGain}",
                $"ROUND SCORE: {_localRoundWins} - {_remoteRoundWins} ({fmtName})\nSOULS: +{rewardShards:N0}\nHERO EXP (LV. {heroLv}): +{expGain}");
        }
        if (_resultLeftHeroImg != null)
            _resultLeftHeroImg.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(myHeroId);
        if (_resultLeftNameText != null)
        {
            var acc = AccountDatabase.GetActiveAccount();
            string myName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName : PlayerPrefs.GetString("Player_Name", "");
            if (string.IsNullOrEmpty(myName) || myName == "Player" || myName == "Wanderer") myName = "Chiến Binh";
            _resultLeftNameText.text = myName;
        }

        if (PvPRemoteOpponent.Instance != null)
        {
            if (_resultRightHeroImg != null)
                _resultRightHeroImg.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(PvPRemoteOpponent.Instance.OpponentHeroId);
            if (_resultRightNameText != null)
                _resultRightNameText.text = PvPRemoteOpponent.Instance.OpponentName.Replace("[", "(").Replace("]", ")");
        }
    }

    private void UpdateVersusHUD()
    {
        if (_timerText != null)
        {
            _timerText.text = Mathf.CeilToInt(Mathf.Max(0f, _matchTimer)).ToString();
        }

        if (_localHealth != null)
        {
            int cur = Mathf.Max(0, _localHealth.CurrentHealth);
            int max = Mathf.Max(1, _localHealth.MaxHealth);
            if (_leftHpFill != null) _leftHpFill.fillAmount = Mathf.Clamp01((float)cur / max);

            float mpRatio = 1f;
            if (_localDuneSkill != null && _localDuneSkill.enabled)
                mpRatio = _localDuneSkill.MaxRage > 0f ? Mathf.Clamp01(_localDuneSkill.CurrentRage / _localDuneSkill.MaxRage) : 0f;
            else if (_localEnergy != null)
                mpRatio = _localEnergy.GetEnergyNormalized();

            if (_leftMpFill != null) _leftMpFill.fillAmount = mpRatio;

            if (_leftHpValueText != null) _leftHpValueText.text = $"{cur} / {max} HP";

            if (_leftPortraitImg != null)
            {
                string myHeroId = CharacterManager.EnsureInstance().GetSelectedCharacterId();
                _leftPortraitImg.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(myHeroId);
            }

            if (_leftNameText != null)
            {
                var cData = CharacterManager.EnsureInstance().GetSelectedCharacter();
                string hName = cData != null ? (LocalizationManager.IsVietnamese ? cData.nameVN : cData.nameEN) : "HIỆP SĨ";
                var acc = AccountDatabase.GetActiveAccount();
                string myName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName : PlayerPrefs.GetString("Player_Name", "");
                if (string.IsNullOrEmpty(myName) || myName == "Player" || myName == "Wanderer") myName = "Chiến Binh";
                _leftNameText.text = $"{myName.ToUpper()} ({hName.ToUpper()})";
            }
        }

        if (PvPRemoteOpponent.Instance != null)
        {
            int cur = Mathf.Max(0, PvPRemoteOpponent.Instance.CurrentHealth);
            int max = Mathf.Max(1, PvPRemoteOpponent.Instance.MaxHealth);
            if (_rightHpFill != null) _rightHpFill.fillAmount = Mathf.Clamp01((float)cur / max);
            if (_rightHpValueText != null) _rightHpValueText.text = $"{cur} / {max} HP";
            if (_rightPortraitImg != null)
            {
                _rightPortraitImg.sprite = PvPRemoteOpponent.GetHeroPortraitSprite(PvPRemoteOpponent.Instance.OpponentHeroId);
            }
            if (_rightNameText != null)
            {
                string cleanOppName = PvPRemoteOpponent.Instance.OpponentName.Replace("[", "(").Replace("]", ")");
                _rightNameText.text = cleanOppName;
            }
        }
    }

    private TextMeshProUGUI CreateHudText(Transform parent, string text, float fontSize, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = fontSize;
        tmp.raycastTarget = false;
        SoulslikeHUDStyler.SetSafeOutline(tmp, 0.3f, Color.black);
        return tmp;
    }

    // =========================================================================
    // ANDROID HIGH PERFORMANCE WIFI LOCK (CHỐNG WIFI POWER SAVE MODE)
    // =========================================================================
    private static void AcquireAndroidWifiLock()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (var wifiManager = context.Call<AndroidJavaObject>("getSystemService", "wifi"))
            {
                if (wifiManager != null)
                {
                    // WIFI_MODE_FULL_HIGH_PERF = 3 (Ép chip Wi-Fi luôn thức ở băng thông tối đa)
                    var lockObj = wifiManager.Call<AndroidJavaObject>("createWifiLock", 3, "VaelstriderPvPLock");
                    if (lockObj != null)
                    {
                        lockObj.Call("setReferenceCounted", false);
                        lockObj.Call("acquire");
                        _cachedWifiLock = lockObj;
                    }
                }
            }
        }
        catch (System.Exception) { }
#endif
    }

    private static void ReleaseAndroidWifiLock()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (_cachedWifiLock != null)
            {
                _cachedWifiLock.Call("release");
                _cachedWifiLock = null;
            }
        }
        catch (System.Exception) { }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject _cachedWifiLock = null;
#endif
}
