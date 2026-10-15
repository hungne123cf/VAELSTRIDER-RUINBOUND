using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Hệ thống cấp độ nhân vật trong trận.
/// - Mỗi quái chết tặng EXP (boss cho nhiều hơn)
/// - Khi đủ EXP sẽ lên cấp và hiện UI chọn 3 augment
/// - Reset về cấp 1 khi bắt đầu trận mới
/// </summary>
public class PlayerLevelSystem : MonoBehaviour
{
    private static PlayerLevelSystem instance;
    public static PlayerLevelSystem Instance
    {
        get => instance != null ? instance : null;
        private set => instance = value;
    }

    public static PlayerLevelSystem EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = FindAnyObjectByType<PlayerLevelSystem>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        var go = new GameObject("PlayerLevelSystem");
        Instance = go.AddComponent<PlayerLevelSystem>();
        return Instance;
    }

    // ─── Events ────────────────────────────────────────────────────────────────
    public static event Action<int> OnLevelUp;
    public static event Action<int, int> OnExpChanged; // (current, required)

    // ─── Config ────────────────────────────────────────────────────────────────
    [Header("EXP Config")]
    [SerializeField] private int baseExpRequired = 4;
    [SerializeField] private float expGrowthFactor = 1.35f;

    [Header("EXP per Kill")]
    [SerializeField] private int expNormalEnemy = 1;
    [SerializeField] private int expBossEnemy   = 10;

    // ─── State ─────────────────────────────────────────────────────────────────
    private int currentLevel = 1;
    private int currentExp   = 0;
    private int expRequired  = 4;

    public int CurrentLevel => currentLevel;
    public int CurrentExp   => currentExp;
    public int ExpRequired  => expRequired;

    // Cached delegates để có thể unsubscribe chính xác
    private Action<GoblinAI>           _onGoblinDied;
    private Action<SkeletonAI>         _onSkeletonDied;
    private Action<SlimeAI>            _onSlimeDied;
    private Action<BloodMonsterAI>     _onBloodDied;
    private Action<BotWheelAI>         _onBotDied;
    private Action<ValleyBeastAI>      _onBeastDied;
    private Action<ValleySpitterAI>    _onSpitterDied;
    private Action<ValleyGolemBossAI>  _onGolemDied;
    private Action<SkeletonKingBossAI> _onSkeletonKingDied;
    private Action<MiniSpiderMinion>   _onSpiderDied;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        baseExpRequired = 4;
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        string s = scene.name.ToLower();
        bool isMenuOrLobby = s.Contains("mainmenu") || s.Contains("login") || s.Contains("lobby") || s.Contains("ending");

        if (isMenuOrLobby)
        {
            ResetForNewBattle();
        }
        else
        {
            // Transitioning between dungeon maps (e.g. Map1 -> Map2 -> Map3)
            // Preserve accumulated level & EXP! Re-subscribe to enemy death events in new scene.
            UnsubscribeFromEnemyDeaths();
            SubscribeToEnemyDeaths();

            // Broadcast current stats to newly spawned HUDs in the new scene
            StartCoroutine(DelayedBroadcastStats());
        }

        LevelUpChoiceUI.ResetUI();
    }

    private IEnumerator DelayedBroadcastStats()
    {
        yield return null;
        yield return null;
        OnLevelUp?.Invoke(currentLevel);
        OnExpChanged?.Invoke(currentExp, expRequired);
    }

    void Start()
    {
        if (currentLevel <= 1 && currentExp == 0)
        {
            ResetForNewBattle();
        }
        UnsubscribeFromEnemyDeaths();
        SubscribeToEnemyDeaths();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        UnsubscribeFromEnemyDeaths();
    }

    private void SubscribeToEnemyDeaths()
    {
        int n = expNormalEnemy;
        int b = expBossEnemy;

        _onGoblinDied          = _ => AddExp(n);
        _onSkeletonDied        = _ => AddExp(n);
        _onSlimeDied           = _ => AddExp(n);
        _onBloodDied           = _ => AddExp(n);
        _onBotDied             = _ => AddExp(n);
        _onBeastDied           = _ => AddExp(n);
        _onSpitterDied         = _ => AddExp(n);
        _onGolemDied           = _ => AddExp(b);
        _onSkeletonKingDied    = _ => AddExp(b);
        _onSpiderDied          = _ => AddExp(n);

        GoblinAI.OnEnemyDied           += _onGoblinDied;
        SkeletonAI.OnEnemyDied         += _onSkeletonDied;
        SlimeAI.OnEnemyDied            += _onSlimeDied;
        BloodMonsterAI.OnEnemyDied     += _onBloodDied;
        BotWheelAI.OnEnemyDied        += _onBotDied;
        ValleyBeastAI.OnEnemyDied      += _onBeastDied;
        ValleySpitterAI.OnEnemyDied    += _onSpitterDied;
        ValleyGolemBossAI.OnEnemyDied  += _onGolemDied;
        SkeletonKingBossAI.OnBossDied  += _onSkeletonKingDied;
        MiniSpiderMinion.OnEnemyDied   += _onSpiderDied;
    }

    private void UnsubscribeFromEnemyDeaths()
    {
        GoblinAI.OnEnemyDied           -= _onGoblinDied;
        SkeletonAI.OnEnemyDied         -= _onSkeletonDied;
        SlimeAI.OnEnemyDied            -= _onSlimeDied;
        BloodMonsterAI.OnEnemyDied     -= _onBloodDied;
        BotWheelAI.OnEnemyDied        -= _onBotDied;
        ValleyBeastAI.OnEnemyDied      -= _onBeastDied;
        ValleySpitterAI.OnEnemyDied    -= _onSpitterDied;
        ValleyGolemBossAI.OnEnemyDied  -= _onGolemDied;
        SkeletonKingBossAI.OnBossDied  -= _onSkeletonKingDied;
        MiniSpiderMinion.OnEnemyDied   -= _onSpiderDied;
    }

    // ─── Core Logic ────────────────────────────────────────────────────────────
    public void ResetForNewBattle()
    {
        currentLevel = 1;
        currentExp   = 0;
        expRequired  = baseExpRequired;
        OnExpChanged?.Invoke(currentExp, expRequired);
        OnLevelUp?.Invoke(currentLevel);
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;
        if (PlayerHealth.IsPlayerDead) return;

        currentExp += amount;
        OnExpChanged?.Invoke(currentExp, expRequired);

        while (currentExp >= expRequired)
        {
            currentExp -= expRequired;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        expRequired = Mathf.RoundToInt(baseExpRequired * Mathf.Pow(expGrowthFactor, currentLevel - 1));
        OnLevelUp?.Invoke(currentLevel);
        OnExpChanged?.Invoke(currentExp, expRequired);

        SoundManager.EnsureInstance()?.PlayUpgradeSuccess();

        StartCoroutine(ShowAugmentUIDelayed(0.2f));
    }

    private System.Collections.IEnumerator ShowAugmentUIDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        LevelUpChoiceUI.Show(currentLevel);
    }

    public float GetExpProgress() => expRequired > 0 ? (float)currentExp / expRequired : 0f;
}
