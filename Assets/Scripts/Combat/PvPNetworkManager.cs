using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý hệ thống mạng PvP (Chế độ Offline / Đã loại bỏ hoàn toàn Photon Fusion).
/// Toàn bộ SDK Photon đã được gỡ bỏ khỏi dự án theo yêu cầu.
/// Giữ nguyên 100% API surface để code game (Story, Dungeon, Combat, Boss, Bot Practice) biên dịch sạch 0 lỗi.
/// </summary>
public class PvPNetworkManager : MonoBehaviour
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

    public const string QUICK_MATCH_SESSION = "OFFLINE_PVP";
    public const string ROOM_PREFIX = "OFFLINE_PIN_";

    public bool IsBotPracticeMode { get; set; } = false;
    public bool IsCustomRoomSession { get; set; } = false;
    public bool IsConnectingOrConnected => false;
    public bool IsRunning => false;
    public bool IsConnected => false;
    public bool IsHost => false;
    public bool IsOnlineMatch => false;
    public bool IsLocalPlayerOne() => true;
    public int LocalPlayerIndex => 0;

    public string CurrentRoomCode { get; set; } = "";
    public string StatusMessage { get; set; } = "";

    public string OpponentName { get; set; } = "BOT AI";
    public string OpponentHeroId { get; set; } = CharacterManager.CHAR_VAELSTRIDER;
    public int OpponentLevel { get; set; } = 1;
    public int OpponentMaxHp { get; set; } = 800;
    public int OpponentCurrentHp { get; set; } = 800;
    public bool HasOpponentJoined { get; set; } = false;

    public bool IsLocalClientReady { get; set; } = false;
    public bool IsOpponentReady { get; set; } = false;

    public event Action OnOpponentReadyChanged;
    public event Action<string, string> OnChatMessageReceived;
    public static event Action<int> OnNetworkRoomEntered;
    public static event Action<int> OnNetworkRoomCleared;
    public static event Action<string> OnNetworkRoomSync;
    public static event Action<int, int, string> OnNetworkWaveStarted;
    public static event Action OnPlayerConnectedInGame;
    public event Action<string> OnStatusChanged;
    public event Action OnMatchReadyToStart;
    public event Action OnOpponentIdentityUpdated;
    public event Action OnConnectionFailed;
    public event Action OnRoomClosed;
    public event Action<string> OnPlayerKicked;

    public static PvPNetworkManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = UnityEngine.Object.FindAnyObjectByType<PvPNetworkManager>();
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
    }

    public int GetConnectedPlayerCount()
    {
        return IsBotPracticeMode ? 2 : 1;
    }

    public static int GetNormalizedPvPMaxHealth(string heroId)
    {
        var charMgr = CharacterManager.EnsureInstance();
        var data = charMgr != null ? charMgr.GetCharacter(heroId) : null;
        int baseHp = data != null && data.baseHealth > 0 ? data.baseHealth : 110;
        return Mathf.Clamp(baseHp * 8, 720, 1800);
    }

    public void PrepareBotPracticeMatch()
    {
        IsBotPracticeMode = true;
        IsCustomRoomSession = false;
        HasOpponentJoined = true;
        CurrentRoomCode = "BOT";

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
        if (candidates.Count > 0)
        {
            OpponentHeroId = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
        else
        {
            OpponentHeroId = CharacterManager.CHAR_VAELSTRIDER;
        }

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

    public void EnterPvPArenaScene()
    {
        string myHero = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        if (!string.IsNullOrEmpty(myHero))
        {
            CharacterManager.EnsureInstance().SelectCharacter(myHero, true);
            AccountDatabase.SetSelectedHero(myHero);
            AccountDatabase.SaveActiveSessionToDatabase();
        }

        PvPMatchManager.IsPvPModeActive = false;
        PvPMatchManager.IsCoopModeActive = false;
        CorruptedCycleManager.ResetToNormalCycle();
        PlayerHealth.ResetRunPreservedHealth();
        MatchTimerUI.ResetRunTimer();
        Time.timeScale = 1.0f;

        PvPLobbyUI.CloseLobbyModal();
        RealmTransitionManager.LoadSceneWithTransition("Map1_Thestart", RealmTransitionManager.TransitionTheme.DeployBattle);
    }

    public void LeaveMatchAndReturnToMenu()
    {
        PvPMatchManager.IsPvPModeActive = false;
        PvPMatchManager.IsCoopModeActive = false;
        IsBotPracticeMode = false;
        Time.timeScale = 1.0f;
        RealmTransitionManager.LoadSceneWithTransition("MainMenuScene", RealmTransitionManager.TransitionTheme.ReturnSanctuary);
    }

    public void ShutdownRunnerOnly() {}
    public void Disconnect() {}
    public void ResetReadyStates()
    {
        IsLocalClientReady = false;
        IsOpponentReady = false;
    }

    public void SetStatus(string msg)
    {
        StatusMessage = msg;
        OnStatusChanged?.Invoke(msg);
    }

    public void BeginVsCountdownAndEnterArena()
    {
        EnterPvPArenaScene();
    }

    public void TriggerCustomRoomStart()
    {
        EnterPvPArenaScene();
    }

    public void StartQuickMatch()
    {
        SetStatus(LocalizationManager.T("CHẾ ĐỘ ONLINE ĐÃ TẮT. ĐANG CHUYỂN SANG ĐẤU TẬP...", "ONLINE MODE DISABLED. SWITCHING TO PRACTICE..."));
        StartBotPractice();
    }

    public void CreateRoomWithPin(string pinCode = "")
    {
        SetStatus(LocalizationManager.T("CHẾ ĐỘ ONLINE ĐÃ TẮT. ĐANG CHUYỂN SANG ĐẤU TẬP...", "ONLINE MODE DISABLED. SWITCHING TO PRACTICE..."));
        StartBotPractice();
    }

    public void JoinRoomWithPin(string pinCode, Action<bool, string> onComplete = null)
    {
        onComplete?.Invoke(false, LocalizationManager.T("Tính năng Online hiện đã bị loại bỏ.", "Online multiplayer is currently removed."));
    }

    public float GetEstimatedLatencySeconds() => 0f;
    public int GetPingMilliseconds() => 0;

    public void SendFriendRequestPacket(string targetPlayerName) {}
    public void SendFriendAcceptPacket(string targetPlayerName) {}
    public void SendRoomInvitePacket(string friendName, string roomPin) {}
    public void SendChatMessage(string message) {}
    public void SendHeroChange(string newHeroId) {}
    public void SendReadyPacket(bool isReady) {}
    public void KickPlayer(string targetName) {}
    public void SendHandshakePacket() {}
    public void SendMatchTimeSync(int elapsedSeconds) {}
    public void BroadcastRoomClosed() {}
    public void SendRawPacket(string payload, int channelId = 7) {}
    public void BroadcastRawPacket(string payload, int channelId = 7) {}

    public void SendPlayerStatePacket(
        Vector2 pos, Vector2 vel, bool flipX, Vector2 scale,
        float moveX, float moveY, float speed,
        int curHp, int maxHp, string spriteName, string heroId) {}

    public void SendActionVfxPacket(string actionType, Vector2 dir, Vector2 pos) {}
    public void SendDamageToOpponentPacket(int damage, bool isCrit, Vector2 knockback, float stunDur = 0f) {}
    public void SendMatchKoPacket(bool localPlayerDied) {}
    public void SendRematchPacket(string subType = "WANT") {}
    public void SendRoomEnterPacket(int roomIndex) {}
    public void SendRoomClearPacket(int roomIndex) {}
    public void SendRoomSyncPacket(string clearedRooms) {}
    public void SendWaveStartPacket(int waveNumber, int totalWaves, string title) {}
    public void SendCoopClearRevivePacket() {}
    public void SendEnemySpawnPacket(int netId, float x, float y, bool isBoss, int maxHp, int curHp, int enemyType = 0) {}
    public void SendEnemyHitPacket(int netId, int damage) {}
    public void SendEnemyDiePacket(int netId) {}
    public void SendWaveClearPacket(int waveNumber, int totalWaves) {}
    public void SendAllWavesClearedPacket() {}
}
