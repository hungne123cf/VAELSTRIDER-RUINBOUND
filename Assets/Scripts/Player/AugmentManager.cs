using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Nhận augment từ LevelUpChoiceUI và áp dụng vào player stats.
/// Singleton tồn tại trong scene game.
/// </summary>
public class AugmentManager : MonoBehaviour
{
    private static AugmentManager instance;
    public static AugmentManager Instance
    {
        get => instance != null ? instance : null;
        private set => instance = value;
    }

    public static AugmentManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = FindAnyObjectByType<AugmentManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        var go = new GameObject("AugmentManager");
        Instance = go.AddComponent<AugmentManager>();
        return Instance;
    }

    // ─── Runtime state ─────────────────────────────────────────────────────────
    private readonly List<AugmentDatabase.AugmentData> _selectedAugments = new List<AugmentDatabase.AugmentData>();
    public List<AugmentDatabase.AugmentData> GetSelectedAugments() => _selectedAugments;
    public int SelectedAugmentCount => _selectedAugments.Count;

    // Tích lũy bonus augment (cộng thêm vào các ApplyUpgradeBonus hiện tại)
    private int augBonusAtk         = 0;
    private int augBonusHp          = 0;
    private float augBonusSpeed     = 0f;
    private float augBonusCrit      = 0f;    // fraction (0.08 = 8%)
    private float augBonusCritDmg   = 0f;    // fraction (0.20 = 20%)
    private float augBonusLifesteal = 0f;
    private float augBonusSpellVamp = 0f;
    private float augBonusCDR       = 0f;    // fraction
    private int augBonusDef         = 0;

    // Kill-based stacks (BerserkerMode)
    private int berserkerStacks     = 0;
    private const int MAX_BERSERKER = 20;
    private bool hasBerserker       = false;

    // Kill-heal (SoulHarvest)
    private int soulHarvestHp       = 0;
    private bool hasSoulHarvest     = false;

    // Echo Strike
    private float echoStrikeChance  = 0f;
    private bool hasEchoStrike      = false;

    // Combo Mastery
    private float comboResetMult    = 1f; // 1 = no change, 0.5 = 50% faster

    // Cached delegates
    private System.Action<GoblinAI>           _goblin;
    private System.Action<SkeletonAI>         _skeleton;
    private System.Action<SlimeAI>            _slime;
    private System.Action<BloodMonsterAI>     _blood;
    private System.Action<BotWheelAI>         _bot;
    private System.Action<ValleyBeastAI>      _beast;
    private System.Action<ValleySpitterAI>    _spitter;
    private System.Action<ValleyGolemBossAI>  _golem;
    private System.Action<SkeletonKingBossAI> _king;
    private System.Action<MiniSpiderMinion>   _spider;

    // Timed Augments Real-time tracking
    private readonly Dictionary<AugmentDatabase.AugmentData, float> _timedRemaining = new Dictionary<AugmentDatabase.AugmentData, float>();
    private readonly Dictionary<AugmentDatabase.AugmentData, int> _timedAtkBonus = new Dictionary<AugmentDatabase.AugmentData, int>();
    private readonly Dictionary<AugmentDatabase.AugmentData, Coroutine> _timedRegenCoroutines = new Dictionary<AugmentDatabase.AugmentData, Coroutine>();

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
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
            // Transitioning between dungeon maps (Map1 -> Map2 -> Map3)
            // Preserve accumulated augments and stacks!
            UnsubscribeKillEvents();
            SubscribeKillEvents();
            StartCoroutine(DelayedReapplyStats());
        }
    }

    private IEnumerator DelayedReapplyStats()
    {
        yield return null;
        yield return null;
        FlushStatsToCombat();
        FlushStatsToHealth();
        FlushStatsToController();
        if (comboResetMult < 1f) ApplyComboMastery();
    }

    void Start() { SubscribeKillEvents(); }

    void Update()
    {
        if (_timedRemaining.Count == 0) return;

        List<AugmentDatabase.AugmentData> expired = null;
        var keys = new List<AugmentDatabase.AugmentData>(_timedRemaining.Keys);
        float dt = Time.deltaTime;

        for (int i = 0; i < keys.Count; i++)
        {
            var a = keys[i];
            float rem = _timedRemaining[a] - dt;
            if (rem <= 0f)
            {
                if (expired == null) expired = new List<AugmentDatabase.AugmentData>();
                expired.Add(a);
            }
            else
            {
                _timedRemaining[a] = rem;
            }
        }

        if (expired != null)
        {
            for (int i = 0; i < expired.Count; i++)
            {
                var a = expired[i];
                _timedRemaining.Remove(a);
                ExpireTimedAugment(a);
            }
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        UnsubscribeKillEvents();
    }

    public void ResetForNewBattle()
    {
        _selectedAugments.Clear();
        _timedRemaining.Clear();
        _timedAtkBonus.Clear();
        _timedRegenCoroutines.Clear();
        augBonusAtk = 0;
        augBonusHp = 0;
        augBonusSpeed = 0f;
        augBonusCrit = 0f;
        augBonusCritDmg = 0f;
        augBonusLifesteal = 0f;
        augBonusSpellVamp = 0f;
        augBonusCDR = 0f;
        augBonusDef = 0;
        berserkerStacks = 0;
        hasBerserker = false;
        hasSoulHarvest = false;
        soulHarvestHp = 0;
        hasEchoStrike = false;
        echoStrikeChance = 0f;
        comboResetMult = 1f;
        StopAllCoroutines();
        FlushStatsToCombat();
        FlushStatsToHealth();
        FlushStatsToController();
    }

    private void SubscribeKillEvents()
    {
        _goblin   = _ => OnKill();
        _skeleton = _ => OnKill();
        _slime    = _ => OnKill();
        _blood    = _ => OnKill();
        _bot      = _ => OnKill();
        _beast    = _ => OnKill();
        _spitter  = _ => OnKill();
        _golem    = _ => OnKill();
        _king     = _ => OnKill();
        _spider   = _ => OnKill();

        GoblinAI.OnEnemyDied           += _goblin;
        SkeletonAI.OnEnemyDied         += _skeleton;
        SlimeAI.OnEnemyDied            += _slime;
        BloodMonsterAI.OnEnemyDied     += _blood;
        BotWheelAI.OnEnemyDied        += _bot;
        ValleyBeastAI.OnEnemyDied      += _beast;
        ValleySpitterAI.OnEnemyDied    += _spitter;
        ValleyGolemBossAI.OnEnemyDied  += _golem;
        SkeletonKingBossAI.OnBossDied  += _king;
        MiniSpiderMinion.OnEnemyDied   += _spider;
    }

    private void UnsubscribeKillEvents()
    {
        GoblinAI.OnEnemyDied           -= _goblin;
        SkeletonAI.OnEnemyDied         -= _skeleton;
        SlimeAI.OnEnemyDied            -= _slime;
        BloodMonsterAI.OnEnemyDied     -= _blood;
        BotWheelAI.OnEnemyDied        -= _bot;
        ValleyBeastAI.OnEnemyDied      -= _beast;
        ValleySpitterAI.OnEnemyDied    -= _spitter;
        ValleyGolemBossAI.OnEnemyDied  -= _golem;
        SkeletonKingBossAI.OnBossDied  -= _king;
        MiniSpiderMinion.OnEnemyDied   -= _spider;
    }

    private void OnKill()
    {
        if (PlayerHealth.IsPlayerDead) return;

        // Berserker stack
        if (hasBerserker && berserkerStacks < MAX_BERSERKER)
        {
            berserkerStacks++;
            FlushStatsToCombat();
        }

        // Soul Harvest
        if (hasSoulHarvest && soulHarvestHp > 0)
        {
            var ph = PlayerHealth.Instance;
            if (ph != null) ph.HealDirect(soulHarvestHp);
        }
    }

    // ─── Apply augment (called by LevelUpChoiceUI) ────────────────────────────
    public void ApplyAugment(AugmentDatabase.AugmentData augment)
    {
        if (augment == null) return;
        _selectedAugments.Add(augment);

        switch (augment.type)
        {
            case AugmentDatabase.AugmentType.BonusAttack:
                augBonusAtk += Mathf.RoundToInt(augment.value);
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.BonusHealth:
                augBonusHp += Mathf.RoundToInt(augment.value);
                FlushStatsToHealth();
                break;

            case AugmentDatabase.AugmentType.BonusSpeed:
                augBonusSpeed += augment.value * 0.01f; // store as fraction
                FlushStatsToController();
                break;

            case AugmentDatabase.AugmentType.BonusCritChance:
                augBonusCrit += augment.value * 0.01f;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.BonusCritDamage:
                augBonusCritDmg += augment.value * 0.01f;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.BonusLifesteal:
                augBonusLifesteal += augment.value * 0.01f;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.BonusSpellVamp:
                augBonusSpellVamp += augment.value * 0.01f;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.BonusCooldownReduction:
                augBonusCDR += augment.value * 0.01f;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.BonusDefense:
                augBonusDef += Mathf.RoundToInt(augment.value);
                break;

            // ── Kill-based / stackable ──────────────────────────────────────
            case AugmentDatabase.AugmentType.BerserkerMode:
                hasBerserker = true;
                break;

            case AugmentDatabase.AugmentType.SoulHarvest:
                hasSoulHarvest = true;
                soulHarvestHp = Mathf.RoundToInt(augment.value);
                break;

            case AugmentDatabase.AugmentType.EchoStrike:
                hasEchoStrike = true;
                echoStrikeChance = augment.value * 0.01f;
                break;

            case AugmentDatabase.AugmentType.ComboMastery:
                comboResetMult = 1f - (augment.value * 0.01f);
                ApplyComboMastery();
                break;

            case AugmentDatabase.AugmentType.UltimateCharge:
                ApplyUltimateCharge(augment.value * 0.01f);
                break;

            // ── Timed buffs ─────────────────────────────────────────────────
            case AugmentDatabase.AugmentType.VampireBlood:
                augBonusLifesteal += augment.value * 0.01f;
                _timedRemaining[augment] = augment.duration;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.RagingFire:
                var combat = FindAnyObjectByType<PlayerCombat>();
                int extraAtk = combat != null ? Mathf.RoundToInt(combat.GetBaseAttack() * (augment.value * 0.01f)) : Mathf.RoundToInt(20 * (augment.value * 0.01f));
                augBonusAtk += extraAtk;
                _timedAtkBonus[augment] = extraAtk;
                _timedRemaining[augment] = augment.duration;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.AncientShield:
                var ph = PlayerHealth.Instance;
                if (ph != null) ph.AddVirtualHealth(Mathf.RoundToInt(augment.value), augment.duration);
                _timedRemaining[augment] = augment.duration;
                break;

            case AugmentDatabase.AugmentType.SwiftShadow:
                augBonusSpeed += augment.value * 0.01f;
                _timedRemaining[augment] = augment.duration;
                FlushStatsToController();
                break;

            case AugmentDatabase.AugmentType.RegenerativeCore:
                var regenCor = StartCoroutine(TimedBuff_Regen(Mathf.RoundToInt(augment.value), augment.duration));
                _timedRegenCoroutines[augment] = regenCor;
                _timedRemaining[augment] = augment.duration;
                break;
        }
    }

    private void ExpireTimedAugment(AugmentDatabase.AugmentData a)
    {
        if (a == null) return;
        _selectedAugments.Remove(a);

        switch (a.type)
        {
            case AugmentDatabase.AugmentType.VampireBlood:
                augBonusLifesteal -= a.value * 0.01f;
                if (augBonusLifesteal < 0f) augBonusLifesteal = 0f;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.RagingFire:
                if (_timedAtkBonus.TryGetValue(a, out int extraAtk))
                {
                    augBonusAtk -= extraAtk;
                    if (augBonusAtk < 0) augBonusAtk = 0;
                    _timedAtkBonus.Remove(a);
                }
                else
                {
                    var combat = FindAnyObjectByType<PlayerCombat>();
                    int fallbackAtk = combat != null ? Mathf.RoundToInt(combat.GetBaseAttack() * (a.value * 0.01f)) : 0;
                    augBonusAtk -= fallbackAtk;
                    if (augBonusAtk < 0) augBonusAtk = 0;
                }
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.SwiftShadow:
                augBonusSpeed -= a.value * 0.01f;
                if (augBonusSpeed < 0f) augBonusSpeed = 0f;
                FlushStatsToController();
                break;

            case AugmentDatabase.AugmentType.AncientShield:
                break;

            case AugmentDatabase.AugmentType.RegenerativeCore:
                if (_timedRegenCoroutines.TryGetValue(a, out var c) && c != null)
                {
                    StopCoroutine(c);
                    _timedRegenCoroutines.Remove(a);
                }
                break;
        }

        Debug.Log($"<color=orange>[AUGMENT]</color> Lõi tạm thời '{a.nameVN}' đã hết thời gian ({a.duration}s) và được gỡ bỏ khỏi danh sách!");
    }

    public float GetRemainingDuration(AugmentDatabase.AugmentData a)
    {
        if (a != null && _timedRemaining.TryGetValue(a, out float rem))
        {
            return rem;
        }
        return a != null ? a.duration : 0f;
    }

    public bool IsTimedAugment(AugmentDatabase.AugmentData a)
    {
        return a != null && a.duration > 0f;
    }

    // ─── Public accessors (read by PlayerCombat / health) ─────────────────────
    public int GetAugBonusAtk()       => augBonusAtk + berserkerStacks * 2;
    public int GetAugBonusHp()        => augBonusHp;
    public float GetAugBonusSpeed()   => augBonusSpeed;
    public float GetAugCrit()         => augBonusCrit;
    public float GetAugCritDmg()      => augBonusCritDmg;
    public float GetAugLifesteal()    => augBonusLifesteal;
    public float GetAugSpellVamp()    => augBonusSpellVamp;
    public float GetAugCDR()          => augBonusCDR;
    public int   GetAugDef()          => augBonusDef;
    public bool  HasEchoStrike()      => hasEchoStrike;
    public float EchoStrikeChance()   => echoStrikeChance;

    // ─── Flush to components ──────────────────────────────────────────────────
    private void FlushStatsToCombat()
    {
        var combat = FindAnyObjectByType<PlayerCombat>();
        if (combat == null) return;
        combat.ApplyAugmentBonus(
            GetAugBonusAtk(),
            GetAugCrit(),
            GetAugCritDmg(),
            GetAugLifesteal(),
            GetAugSpellVamp(),
            GetAugCDR()
        );
    }

    private void FlushStatsToHealth()
    {
        var ph = PlayerHealth.Instance;
        if (ph != null) ph.ApplyAugmentBonus(augBonusHp);
    }

    private void FlushStatsToController()
    {
        var ctrl = FindAnyObjectByType<PlayerController>();
        if (ctrl == null) return;
        ctrl.ApplyAugmentBonus(augBonusSpeed);
    }

    private IEnumerator TimedBuff_Regen(int hpPerSec, float dur)
    {
        float elapsed = 0f;
        var ph = PlayerHealth.Instance;
        while (elapsed < dur)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
            if (ph != null && !PlayerHealth.IsPlayerDead)
                ph.HealDirect(hpPerSec);
        }
    }

    private void ApplyComboMastery()
    {
        var combat = FindAnyObjectByType<PlayerCombat>();
        if (combat != null) combat.SetComboResetMultiplier(comboResetMult);
    }

    private void ApplyUltimateCharge(float reductionFrac)
    {
        var combat = FindAnyObjectByType<PlayerCombat>();
        if (combat != null) combat.ReduceSkillCooldownImmediate(reductionFrac);
    }

    // ─── Reset khi thua / thoát trận ─────────────────────────────────────────
    public void ResetAllAugments()
    {
        augBonusAtk      = 0; augBonusHp    = 0; augBonusSpeed   = 0f;
        augBonusCrit     = 0f; augBonusCritDmg = 0f;
        augBonusLifesteal= 0f; augBonusSpellVamp=0f; augBonusCDR = 0f;
        augBonusDef      = 0;
        berserkerStacks  = 0; hasBerserker    = false;
        hasSoulHarvest   = false; soulHarvestHp = 0;
        hasEchoStrike    = false; echoStrikeChance = 0f;
        comboResetMult   = 1f;
        StopAllCoroutines();
    }
}
