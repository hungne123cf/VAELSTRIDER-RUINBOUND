using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quản lý toàn bộ hiệu ứng Lõi Nâng Cấp (Augments) cho người chơi.
/// Hỗ trợ đầy đủ các cơ chế nâng cao chuẩn Đấu Trường Chân Lý (TFT) & Dark Fantasy:
/// - Chỉ số cộng dồn vĩnh viễn (Soul Harvest, Berserk Stacks)
/// - Hiệu ứng Lướt (Shadow Ambush, Storm Dancer)
/// - Hiệu ứng Sinh tồn (Undying Sovereign bất tử khi sắp chết, Divine Aegis bung khiên)
/// - Hiệu ứng Tinh Tú & Đòn đánh (Celestial Cascade, Death's Touch, Echo Strike, Blood God's Tribute)
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

    // Tích lũy bonus augment
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
    private const int MAX_BERSERKER = 25;
    private bool hasBerserker       = false;

    // Kill-heal & Max HP gain (SoulHarvest)
    private int soulHarvestHp       = 0;
    private bool hasSoulHarvest     = false;

    // Echo Strike
    private float echoStrikeChance  = 0f;
    private bool hasEchoStrike      = false;

    // Combo Mastery
    private float comboResetMult    = 1f;

    // ─── TFT-style Mechanics ───
    private bool hasDemonicPact        = false;
    private bool hasUndyingSovereign    = false;
    private float undyingCooldownTimer = 0f;

    private bool hasCelestialCascade   = false;
    private int cascadeHitCount        = 0;

    private bool hasBloodGodTribute    = false;
    private bool hasStormDancer        = false;

    private bool hasShadowAmbush       = false;
    private bool shadowAmbushActive    = false;
    private float shadowAmbushTimer    = 0f;

    private bool hasDivineAegis        = false;
    private float divineAegisCooldownTimer = 0f;

    private bool hasDeathsTouch        = false;
    private bool hasPoisonTouch        = false;
    private bool hasSurgingPulse       = false;

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
            LevelUpChoiceUI.ResetMatchRerolls();
        }
        else
        {
            // Các Lõi có thời hạn thì qua map mới sẽ bị mất ngay cho dù còn thời hạn
            ClearAllTimedAugmentsOnNewMap();
            UnsubscribeKillEvents();
            SubscribeKillEvents();
            StartCoroutine(DelayedReapplyStats());
        }
    }

    /// <summary>
    /// Qua map mới: tất cả các Lõi Nâng Cấp có thời hạn (duration > 0) sẽ bị hủy ngay lập tức cho dù vẫn còn thời hạn.
    /// </summary>
    public void ClearAllTimedAugmentsOnNewMap()
    {
        if (_timedRegenCoroutines != null && _timedRegenCoroutines.Count > 0)
        {
            foreach (var kvp in _timedRegenCoroutines)
            {
                if (kvp.Value != null) StopCoroutine(kvp.Value);
            }
            _timedRegenCoroutines.Clear();
        }

        if (_timedRemaining != null && _timedRemaining.Count > 0)
        {
            var keys = new List<AugmentDatabase.AugmentData>(_timedRemaining.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                var a = keys[i];
                if (a != null)
                {
                    ExpireTimedAugment(a);
                }
            }
            _timedRemaining.Clear();
            _timedAtkBonus.Clear();
        }

        // Đảm bảo xoá mọi Lõi có thời hạn khỏi danh sách đã sở hữu
        _selectedAugments.RemoveAll(a => a != null && a.duration > 0f);

        FlushStatsToCombat();
        FlushStatsToHealth();
        FlushStatsToController();

        AugmentHUDBar.EnsureExists()?.RefreshBadges();
    }

    private IEnumerator DelayedReapplyStats()
    {
        yield return null;
        yield return null;
        FlushStatsToCombat();
        FlushStatsToHealth();
        FlushStatsToController();
        if (comboResetMult < 1f) ApplyComboMastery();
        AugmentHUDBar.EnsureExists()?.RefreshBadges();
    }

    void Start() { SubscribeKillEvents(); }

    void Update()
    {
        float dt = Time.deltaTime;

        // Timers for TFT mechanics
        if (undyingCooldownTimer > 0f)
        {
            undyingCooldownTimer -= dt;
            if (undyingCooldownTimer <= 0f)
            {
                undyingCooldownTimer = 0f;
                NotifyCooldownReady("BẤT DIỆT CUỒNG VƯƠNG", "UNDYING SOVEREIGN");
            }
        }

        if (divineAegisCooldownTimer > 0f)
        {
            divineAegisCooldownTimer -= dt;
            if (divineAegisCooldownTimer <= 0f)
            {
                divineAegisCooldownTimer = 0f;
                NotifyCooldownReady("THÁNH KHIÊN HỘ MỆNH", "DIVINE AEGIS");
            }
        }

        if (shadowAmbushTimer > 0f)
        {
            shadowAmbushTimer -= dt;
            if (shadowAmbushTimer <= 0f) shadowAmbushActive = false;
        }

        // Timed augments
        if (_timedRemaining.Count == 0) return;

        List<AugmentDatabase.AugmentData> expired = null;
        var keys = new List<AugmentDatabase.AugmentData>(_timedRemaining.Keys);

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
        LevelUpChoiceUI.ResetMatchRerolls();
        WanderingShopUI.ResetShopForNewRun();
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

        hasDemonicPact = false;
        hasUndyingSovereign = false;
        undyingCooldownTimer = 0f;
        hasCelestialCascade = false;
        cascadeHitCount = 0;
        hasBloodGodTribute = false;
        hasStormDancer = false;
        hasShadowAmbush = false;
        shadowAmbushActive = false;
        shadowAmbushTimer = 0f;
        hasDivineAegis = false;
        divineAegisCooldownTimer = 0f;
        hasDeathsTouch = false;
        hasPoisonTouch = false;
        hasSurgingPulse = false;

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

        // Berserker stack (Cuồng bạo bất tận)
        if (hasBerserker && berserkerStacks < MAX_BERSERKER)
        {
            berserkerStacks++;
            FlushStatsToCombat();
        }

        // Soul Harvest (Hấp thụ linh hồn)
        if (hasSoulHarvest && soulHarvestHp > 0)
        {
            var ph = PlayerHealth.Instance;
            if (ph != null)
            {
                ph.HealDirect(soulHarvestHp);
                augBonusHp += 5; // Tích lũy vĩnh viễn +5 HP tối đa mỗi mạng hạ quái!
                ph.ApplyAugmentBonus(augBonusHp);
            }
        }
    }

    // ─── Dash Hook ─────────────────────────────────────────────────────────────
    public void OnPlayerDash(Vector3 playerPos)
    {
        // 1. Nhát chém bóng đêm (Shadow Ambush)
        if (hasShadowAmbush)
        {
            shadowAmbushActive = true;
            shadowAmbushTimer = 3.0f;
        }

        // 2. Vũ điệu bão tố (Storm Dancer): phóng 4 luồng kiếm khí quét xung quanh
        if (hasStormDancer)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, 3.5f);
            foreach (var col in hits)
            {
                if (col.CompareTag("Enemy") || col.CompareTag("Boss"))
                {
                    var dmg = col.GetComponent<IDamageable>();
                    if (dmg != null)
                    {
                        dmg.TakeDamage(200);
                        DamagePopup.Create(col.transform.position + Vector3.up * 0.5f, 200, DamageType.Physical, isCritical: true);
                    }
                }
            }
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            StartCoroutine(TimedSpeedBuff(0.20f, 3.0f));
        }

        // 3. Bát Hướng Cuồng Phong (Eight-Way Tempest - Gale Ronin): Phóng 8 luồng kiếm khí phong lôi tỏa 8 hướng 360 độ
        if (HasAugment(AugmentDatabase.AugmentType.EightWayTempest))
        {
            float[] angles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            foreach (float a in angles)
            {
                Vector2 dir = Quaternion.Euler(0, 0, a) * Vector2.right;
                GaleCrescentWave.Spawn(playerPos, dir, 85, 13.5f, 0.8f, null);
            }
            SoundManager.EnsureInstance()?.PlaySwordSlash();
        }

        // 4. Ảnh Phân Thân Hư Không (Shadow Mirroring - Void Nightmare): Trảm kích phụ quét sạch quái
        if (HasAugment(AugmentDatabase.AugmentType.ShadowMirroring))
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, 4.0f);
            foreach (var col in hits)
            {
                if (col.CompareTag("Enemy") || col.CompareTag("Boss"))
                {
                    var dmg = col.GetComponent<IDamageable>();
                    if (dmg != null)
                    {
                        dmg.TakeDamage(120);
                        DamagePopup.Create(col.transform.position + Vector3.up * 0.5f, 120, DamageType.Magic, isCritical: false);
                    }
                }
            }
        }
    }

    private IEnumerator TimedSpeedBuff(float frac, float dur)
    {
        augBonusSpeed += frac;
        FlushStatsToController();
        yield return new WaitForSeconds(dur);
        augBonusSpeed -= frac;
        if (augBonusSpeed < 0f) augBonusSpeed = 0f;
        FlushStatsToController();
    }

    public bool ConsumeShadowAmbush()
    {
        if (hasShadowAmbush && shadowAmbushActive && shadowAmbushTimer > 0f)
        {
            shadowAmbushActive = false;
            shadowAmbushTimer = 0f;
            return true;
        }
        return false;
    }

    // ─── Undying Sovereign Check ───────────────────────────────────────────────
    public bool CheckUndyingSovereign(PlayerHealth ph, ref int incomingDamage)
    {
        if (hasUndyingSovereign && undyingCooldownTimer <= 0f && ph != null)
        {
            if (ph.CurrentHealth - incomingDamage <= 0)
            {
                incomingDamage = 0;
                undyingCooldownTimer = 90f;
                ph.HealDirect(Mathf.RoundToInt(ph.MaxHealth * 0.25f));
                ph.StartCoroutine(InvulnerabilityRoutine(ph, 3.5f));

                DamagePopup.CreateText(
                    ph.transform.position + Vector3.up * 1.5f,
                    LocalizationManager.IsVietnamese ? "BẤT DIỆT CUỒNG VƯƠNG (3.5s)" : "UNDYING SOVEREIGN (3.5s)",
                    new Color(1f, 0.85f, 0.2f)
                );
                SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
                return true; // Chặn đứng cái chết hoàn toàn!
            }
        }
        return false;
    }

    private IEnumerator InvulnerabilityRoutine(PlayerHealth ph, float duration)
    {
        if (ph == null) yield break;
        ph.isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        if (ph != null) ph.isInvulnerable = false;
    }

    // ─── Divine Aegis Check ────────────────────────────────────────────────────
    public void CheckDivineAegis(PlayerHealth ph)
    {
        if (hasDivineAegis && divineAegisCooldownTimer <= 0f && ph != null)
        {
            if ((float)ph.CurrentHealth / ph.MaxHealth <= 0.35f)
            {
                divineAegisCooldownTimer = 45f;
                ph.AddVirtualHealth(250, 15f);
                ph.HealDirect(Mathf.RoundToInt(ph.MaxHealth * 0.25f));

                DamagePopup.CreateText(
                    ph.transform.position + Vector3.up * 1.2f,
                    LocalizationManager.IsVietnamese ? "THÁNH KHIÊN HỘ MỆNH (CD 45s)" : "DIVINE AEGIS (CD 45s)",
                    new Color(0.35f, 0.75f, 1f)
                );
                SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
            }
        }
    }

    private void NotifyCooldownReady(string nameVN, string nameEN)
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            string title = LocalizationManager.IsVietnamese 
                ? $"★ {nameVN}: ĐÃ HỒI PHỤC! ★" 
                : $"★ {nameEN}: READY! ★";
            DamagePopup.CreateText(player.transform.position + Vector3.up * 1.6f, title, new Color(0.35f, 0.90f, 1f));
        }
        SoundManager.EnsureInstance()?.PlayHeavenlyAscension();
    }

    public bool HasDivineAegis => hasDivineAegis;
    public float DivineAegisCooldownRemaining => Mathf.Max(0f, divineAegisCooldownTimer);
    public float DivineAegisMaxCooldown => 45f;

    public bool HasUndyingSovereign => hasUndyingSovereign;
    public float UndyingSovereignCooldownRemaining => Mathf.Max(0f, undyingCooldownTimer);
    public float UndyingSovereignMaxCooldown => 90f;

    public bool GetAugmentCooldownInfo(AugmentDatabase.AugmentType type, out float remaining, out float maxCooldown)
    {
        if (type == AugmentDatabase.AugmentType.DivineAegis && hasDivineAegis)
        {
            remaining = Mathf.Max(0f, divineAegisCooldownTimer);
            maxCooldown = 45f;
            return true;
        }
        if (type == AugmentDatabase.AugmentType.UndyingSovereign && hasUndyingSovereign)
        {
            remaining = Mathf.Max(0f, undyingCooldownTimer);
            maxCooldown = 90f;
            return true;
        }
        remaining = 0f;
        maxCooldown = 0f;
        return false;
    }

    // ─── Celestial Cascade Check ───────────────────────────────────────────────
    public bool CheckAndTriggerCelestialCascade()
    {
        if (!hasCelestialCascade) return false;
        cascadeHitCount++;
        if (cascadeHitCount >= 3)
        {
            cascadeHitCount = 0;
            return true;
        }
        return false;
    }

    public bool HasBloodGodTribute() => hasBloodGodTribute;
    public bool HasDeathsTouch()     => hasDeathsTouch;
    public bool HasPoisonTouch()     => hasPoisonTouch;
    public bool HasSurgingPulse()    => hasSurgingPulse;

    /// <summary>
    /// <summary>
    /// Khuếch đại sát thương ra đòn dựa trên các hiệu ứng debuff mục tiêu (Độc Ăn Mòn +20%, Vết Thương Sâu, Diệt Khổng Lồ...)
    /// và giảm trừ qua Giáp Vật Lý / Kháng Phép của quái vật & Boss theo DamageType.
    /// Áp dụng nhất quán cho TẤT CẢ TƯỚNG (Kiếm Khí, Chém Thường, Đập Búa, Phép Thuật, Dao Găm).
    /// </summary>
    public static int ProcessOutgoingDamage(GameObject attacker, GameObject targetEnemy, int rawDamage, DamageType damageType = DamageType.Physical)
    {
        if (targetEnemy == null) return rawDamage;
        float mult = 1.0f;

        // 1. Hiệu ứng Nọc Độc Ăn Mòn (PoisonTouch / Corrosive Venom: +20% Sát Thương)
        if (CorrosiveVenomDebuff.HasDebuff(targetEnemy))
        {
            mult *= CorrosiveVenomDebuff.GetDamageMultiplier(targetEnemy);
        }

        // 2. Hiệu ứng Vết Thương Sâu (Grievous Wounds)
        if (GrievousWoundsDebuff.HasDebuff(targetEnemy))
        {
            mult *= GrievousWoundsDebuff.GetDamageMultiplier(targetEnemy);
        }

        // 3. Diệt Khổng Lồ (Giant Slayer: Tăng thêm 25% sát thương lên Boss)
        var augMgr = Instance;
        if (augMgr != null && augMgr.HasAugment(AugmentDatabase.AugmentType.GiantSlayer))
        {
            var bot = targetEnemy.GetComponent<BotWheelAI>();
            var blood = targetEnemy.GetComponent<BloodMonsterAI>();
            bool isBoss = (bot != null && bot.IsBoss) || (blood != null && blood.IsBoss) ||
                          targetEnemy.GetComponent<SkeletonKingBossAI>() != null ||
                          targetEnemy.GetComponent<ValleyGolemBossAI>() != null;
            if (isBoss)
            {
                mult *= 1.25f;
            }
        }

        int augmentedDamage = Mathf.RoundToInt(rawDamage * mult);

        // 4. Giáp & Kháng Phép của Quái Vật / Boss (Bỏ qua vật thể môi trường / rương báu)
        bool isProp = targetEnemy.GetComponent<BreakableProp>() != null || 
                      targetEnemy.GetComponentInParent<BreakableProp>() != null || 
                      targetEnemy.GetComponent<TreasureChest>() != null || 
                      targetEnemy.GetComponentInParent<TreasureChest>() != null;
        if (!isProp)
        {
            var armor = EnemyArmor.EnsureArmor(targetEnemy);
            if (armor != null)
            {
                return armor.MitigateDamage(augmentedDamage, damageType, attacker);
            }
        }

        return augmentedDamage;
    }

    /// <summary>
    /// Kích hoạt toàn bộ hiệu ứng Đòn Đánh của Lõi Nâng Cấp (Áp dụng đồng đều cho TẤT CẢ TƯỚNG)
    /// </summary>
    public static void TriggerOnHitAugments(GameObject attacker, GameObject targetEnemy, int actualDamage, IDamageable damageable)
    {
        if (targetEnemy == null) return;
        var augMgr = Instance;
        if (augMgr == null) return;

        bool isProp = (targetEnemy.GetComponent<BreakableProp>() != null || targetEnemy.GetComponentInParent<BreakableProp>() != null ||
                       targetEnemy.GetComponent<TreasureChest>() != null || targetEnemy.GetComponentInParent<TreasureChest>() != null);
        if (isProp) return;

        // 1. Độc Dược Ăn Mòn (PoisonTouch): Găm nọc độc ăn mòn khiến mục tiêu nhận thêm +20% Sát Thương trong 4s
        if (augMgr.HasPoisonTouch())
        {
            CorrosiveVenomDebuff.Apply(targetEnemy, duration: 4.0f, bonusDamage: 0.20f);
        }

        // 2. Vết Thương Tử Thần (DeathsTouch): Thiêu đốt máu & Vết thương sâu
        if (augMgr.HasDeathsTouch())
        {
            GrievousWoundsDebuff.Apply(targetEnemy, duration: 3.5f, healReduction: 0.40f);
            int burnDmg = Mathf.Max(10, Mathf.RoundToInt(actualDamage * 0.25f));
            damageable?.TakeDamage(burnDmg);
            DamagePopup.Create(targetEnemy.transform.position + Vector3.up * 0.35f, burnDmg, DamageType.TrueDamage, isCritical: false);
        }

        // 3. Lôi Đình Song Trảm (EchoStrike): Cơ hội giáng sấm sét lan
        if (augMgr.HasEchoStrike() && UnityEngine.Random.value <= augMgr.EchoStrikeChance())
        {
            int echoDmg = Mathf.RoundToInt(actualDamage * 0.65f);
            damageable?.TakeDamage(echoDmg);
            DamagePopup.Create(targetEnemy.transform.position + Vector3.up * 0.4f, echoDmg, DamageType.Magic, isCritical: false);
            SoundManager.EnsureInstance()?.PlayChainLightning();
        }

        // 4. Vọng Âm Tinh Tú (CelestialCascade): Đòn thứ 3 gọi Mưa Thiên Thạch
        if (augMgr.CheckAndTriggerCelestialCascade())
        {
            damageable?.TakeDamage(250);
            DamagePopup.Create(targetEnemy.transform.position + Vector3.up * 0.8f, 250, DamageType.Magic, isCritical: true);
            SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
        }

        // 5. Năng Lượng Tuôn Trào (SurgingPulse): Hồi phục thêm mana/nội năng khi đánh trúng quái
        if (attacker != null)
        {
            var energySys = attacker.GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (energySys != null && augMgr.HasSurgingPulse())
            {
                energySys.RestoreEnergy(6f);
            }
        }

        // 6. Hỏa Nham Bộc Phát (MagmaEruption): Gây thêm 40 sát thương thiêu đốt & làm chậm 35%
        if (augMgr.HasAugment(AugmentDatabase.AugmentType.MagmaEruption))
        {
            damageable?.TakeDamage(40);
            DamagePopup.Create(targetEnemy.transform.position + Vector3.up * 0.45f, 40, DamageType.TrueDamage, isCritical: false);
        }

        // 7. Hàn Băng Cực Độ (AbsoluteZeroNova): 30% cơ hội nổ Hàn Băng gây 95 dmg và đông cứng
        if (augMgr.HasAugment(AugmentDatabase.AugmentType.AbsoluteZeroNova) && UnityEngine.Random.value <= 0.30f)
        {
            damageable?.TakeDamage(95);
            DamagePopup.Create(targetEnemy.transform.position + Vector3.up * 0.6f, 95, DamageType.Magic, isCritical: true);
            EnemyFreezeEffect.ApplyFreeze(targetEnemy, 1.2f);
        }

        // 8. Hoa Độc Bùng Nổ (VirulentBloom): Nọc độc cực mạnh & lây lan
        if (augMgr.HasAugment(AugmentDatabase.AugmentType.VirulentBloom))
        {
            CorrosiveVenomDebuff.Apply(targetEnemy, duration: 4.5f, bonusDamage: 0.30f);
        }

        // 9. Tam Trảm Tàn Phong (TripleRuinGale - Vaelstrider): Đòn chém có 35% cơ hội phóng 3 luồng kiếm khí
        if (augMgr.HasAugment(AugmentDatabase.AugmentType.TripleRuinGale) && UnityEngine.Random.value <= 0.35f && attacker != null)
        {
            Vector3 origin = attacker.transform.position;
            Vector2 aimDir = (targetEnemy.transform.position - origin).normalized;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = Vector2.right;

            GaleCrescentWave.Spawn(origin, aimDir, 80, 14f, 0.9f, null);
            GaleCrescentWave.Spawn(origin, Quaternion.Euler(0, 0, 20f) * aimDir, 80, 14f, 0.8f, null);
            GaleCrescentWave.Spawn(origin, Quaternion.Euler(0, 0, -20f) * aimDir, 80, 14f, 0.8f, null);
            SoundManager.EnsureInstance()?.PlaySwordSlash();
        }

        // 10. Song Băng Trụ Thần Cực (TwinFrostNova - Azure Blade): 30% cơ hội nổ cột băng phong ấn 2.5s
        if (augMgr.HasAugment(AugmentDatabase.AugmentType.TwinFrostNova) && UnityEngine.Random.value <= 0.30f && attacker != null)
        {
            Collider2D[] iceHits = Physics2D.OverlapCircleAll(targetEnemy.transform.position, 4.0f);
            foreach (var col in iceHits)
            {
                if (col.CompareTag("Enemy") || col.CompareTag("Boss"))
                {
                    col.GetComponent<IDamageable>()?.TakeDamage(110);
                    EnemyFreezeEffect.ApplyFreeze(col.gameObject, 2.5f);
                    DamagePopup.Create(col.transform.position + Vector3.up * 0.7f, 110, DamageType.Magic, isCritical: true);
                }
            }
            SoundManager.EnsureInstance()?.PlayHitImpact(false, true);
        }

        // 11. Địa Chấn Tam Liên Hoàn (CataclysmicQuake - Titan Berserker): 30% cơ hội nện chấn động làm choáng
        if (augMgr.HasAugment(AugmentDatabase.AugmentType.CataclysmicQuake) && UnityEngine.Random.value <= 0.30f)
        {
            Collider2D[] quakeHits = Physics2D.OverlapCircleAll(targetEnemy.transform.position, 5.0f);
            foreach (var col in quakeHits)
            {
                if (col.CompareTag("Enemy") || col.CompareTag("Boss"))
                {
                    col.GetComponent<IDamageable>()?.TakeDamage(90);
                    EnemyStunEffect.ApplyStun(col.gameObject, 1.5f);
                    DamagePopup.Create(col.transform.position + Vector3.up * 0.5f, 90, DamageType.Physical, isCritical: false);
                }
            }
            var cam = Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.18f, 0.25f);
            SoundManager.EnsureInstance()?.PlayHitImpact(true, true);
        }
    }

    public void ApplyAugment(AugmentDatabase.AugmentData augment)
    {
        if (augment == null) return;
        _selectedAugments.Add(augment);

        // Hiển thị ngay icon Lõi Nâng Cấp tương ứng lên màn hình trận đấu (AugmentHUDBar)
        AugmentHUDBar.EnsureExists()?.OnAugmentAcquired(augment);

        switch (augment.type)
        {
            case AugmentDatabase.AugmentType.BonusAttack:
            case AugmentDatabase.AugmentType.LaceratingEdge:
                augBonusAtk += Mathf.RoundToInt(augment.value);
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.BonusHealth:
                augBonusHp += Mathf.RoundToInt(augment.value);
                FlushStatsToHealth();
                break;

            case AugmentDatabase.AugmentType.AbundantVitality:
                augBonusHp += Mathf.RoundToInt(augment.value);
                FlushStatsToHealth();
                PlayerHealth.Instance?.HealDirect(100);
                break;

            case AugmentDatabase.AugmentType.BonusSpeed:
                augBonusSpeed += augment.value * 0.01f;
                FlushStatsToController();
                break;

            case AugmentDatabase.AugmentType.FleetWind:
                augBonusSpeed += augment.value * 0.01f;
                augBonusDef += 15;
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
            case AugmentDatabase.AugmentType.SurgingPulse:
                augBonusCDR += augment.value * 0.01f;
                hasSurgingPulse = true;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.BonusDefense:
            case AugmentDatabase.AugmentType.IronBastion:
                augBonusDef += Mathf.RoundToInt(augment.value);
                break;

            // ─── TFT-style Mechanics ───
            case AugmentDatabase.AugmentType.DemonicPact:
                hasDemonicPact = true;
                augBonusAtk += 45;
                augBonusSpeed += 0.20f;
                augBonusLifesteal += 0.15f;
                // Hy sinh 15% Máu tối đa
                int sacrificeHp = PlayerHealth.Instance != null ? Mathf.RoundToInt(PlayerHealth.Instance.MaxHealth * 0.15f) : 60;
                augBonusHp -= sacrificeHp;
                FlushStatsToCombat();
                FlushStatsToController();
                FlushStatsToHealth();
                break;

            case AugmentDatabase.AugmentType.UndyingSovereign:
                hasUndyingSovereign = true;
                break;

            case AugmentDatabase.AugmentType.CelestialCascade:
                hasCelestialCascade = true;
                break;

            case AugmentDatabase.AugmentType.BloodGodTribute:
                hasBloodGodTribute = true;
                augBonusLifesteal += augment.value * 0.01f;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.GodforgedArsenal:
                augBonusAtk += 50;
                augBonusDef += 20;
                augBonusCrit += 0.20f;
                augBonusCDR += 0.25f;
                ApplyUltimateCharge(0.35f);
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.StormDancer:
                hasStormDancer = true;
                break;

            case AugmentDatabase.AugmentType.ShadowAmbush:
                hasShadowAmbush = true;
                break;

            case AugmentDatabase.AugmentType.ArcaneOverdrive:
                augBonusAtk += Mathf.RoundToInt(augment.value);
                augBonusSpellVamp += 0.12f;
                augBonusCDR += 0.25f;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.DivineAegis:
                hasDivineAegis = true;
                break;

            case AugmentDatabase.AugmentType.DeathsTouch:
                hasDeathsTouch = true;
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

            // ── 15 Lõi Mới Bổ Sung Chuẩn TFT & Dark Fantasy ────────────────────
            case AugmentDatabase.AugmentType.HeavyImpact:
                augBonusAtk += Mathf.RoundToInt(augment.value);
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.ArcaneShield:
                augBonusDef += 10;
                PlayerHealth.Instance?.AddVirtualHealth(Mathf.RoundToInt(augment.value), 60f);
                FlushStatsToHealth();
                break;

            case AugmentDatabase.AugmentType.RelentlessAssault:
                augBonusAtk += 10;
                augBonusSpeed += 0.05f;
                FlushStatsToCombat();
                FlushStatsToController();
                break;

            case AugmentDatabase.AugmentType.BountyHunter:
                var playerInv = FindAnyObjectByType<PlayerInventory>();
                if (playerInv != null) playerInv.AddShards(Mathf.RoundToInt(augment.value));
                augBonusSpeed += 0.15f;
                FlushStatsToController();
                break;

            case AugmentDatabase.AugmentType.SecondWind:
                augBonusHp += Mathf.RoundToInt(augment.value);
                FlushStatsToHealth();
                PlayerHealth.Instance?.HealDirect(80);
                break;

            case AugmentDatabase.AugmentType.StaticOverload:
                augBonusAtk += Mathf.RoundToInt(augment.value);
                hasEchoStrike = true;
                echoStrikeChance = Mathf.Max(echoStrikeChance, 0.25f);
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.PoisonTouch:
                hasPoisonTouch = true;
                break;

            case AugmentDatabase.AugmentType.GiantSlayer:
                augBonusAtk += Mathf.RoundToInt(augment.value);
                augBonusCrit += 0.15f;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.LifeTransfusion:
                augBonusLifesteal += augment.value * 0.01f;
                hasSoulHarvest = true;
                soulHarvestHp = Mathf.Max(soulHarvestHp, 35);
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.UnwaveringWill:
                augBonusDef += Mathf.RoundToInt(augment.value);
                augBonusHp += 150;
                FlushStatsToHealth();
                break;

            case AugmentDatabase.AugmentType.BattleFrenzy:
                augBonusAtk += Mathf.RoundToInt(augment.value);
                augBonusLifesteal += 0.12f;
                augBonusSpeed += 0.10f;
                FlushStatsToCombat();
                FlushStatsToController();
                break;

            case AugmentDatabase.AugmentType.ChainReaction:
                augBonusAtk += Mathf.RoundToInt(augment.value);
                augBonusCrit += 0.20f;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.AscendedDivinity:
                augBonusHp += Mathf.RoundToInt(augment.value);
                augBonusAtk += 40;
                augBonusDef += 20;
                FlushStatsToHealth();
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.PrismaticTitan:
                augBonusHp += Mathf.RoundToInt(augment.value);
                augBonusDef += 40;
                PlayerHealth.Instance?.AddVirtualHealth(250, 90f);
                FlushStatsToHealth();
                break;

            case AugmentDatabase.AugmentType.EchoingVoid:
                augBonusCDR += augment.value * 0.01f;
                augBonusAtk += 45;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.GlacialSoul:
                PlayerHealth.Instance?.AddVirtualHealth(220, 9999f);
                break;

            case AugmentDatabase.AugmentType.VoidTranscendence:
                augBonusLifesteal += 0.25f;
                augBonusAtk += 40;
                FlushStatsToCombat();
                break;

            // ── Timed buffs (Cộng dồn thời gian nếu nhận lại cùng Lõi) ────────────
            case AugmentDatabase.AugmentType.VampireBlood:
                augBonusLifesteal += augment.value * 0.01f;
                float curVamp = _timedRemaining.ContainsKey(augment) ? _timedRemaining[augment] : 0f;
                _timedRemaining[augment] = curVamp + augment.duration;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.RagingFire:
                var combat = FindAnyObjectByType<PlayerCombat>();
                int extraAtk = combat != null ? Mathf.RoundToInt(combat.GetBaseAttack() * (augment.value * 0.01f)) : Mathf.RoundToInt(20 * (augment.value * 0.01f));
                augBonusAtk += extraAtk;
                _timedAtkBonus[augment] = (_timedAtkBonus.ContainsKey(augment) ? _timedAtkBonus[augment] : 0) + extraAtk;
                float curFire = _timedRemaining.ContainsKey(augment) ? _timedRemaining[augment] : 0f;
                _timedRemaining[augment] = curFire + augment.duration;
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.AncientShield:
                var ph = PlayerHealth.Instance;
                float curShieldTime = _timedRemaining.ContainsKey(augment) ? _timedRemaining[augment] : 0f;
                float totalShieldTime = curShieldTime + augment.duration;
                if (ph != null) ph.AddVirtualHealth(Mathf.RoundToInt(augment.value), totalShieldTime);
                _timedRemaining[augment] = totalShieldTime;
                break;

            case AugmentDatabase.AugmentType.SwiftShadow:
                augBonusSpeed += augment.value * 0.01f;
                float curShadow = _timedRemaining.ContainsKey(augment) ? _timedRemaining[augment] : 0f;
                _timedRemaining[augment] = curShadow + augment.duration;
                FlushStatsToController();
                break;

            case AugmentDatabase.AugmentType.RegenerativeCore:
                if (_timedRegenCoroutines.TryGetValue(augment, out var oldCor) && oldCor != null)
                {
                    StopCoroutine(oldCor);
                }
                float curRegen = _timedRemaining.ContainsKey(augment) ? _timedRemaining[augment] : 0f;
                float totalRegen = curRegen + augment.duration;
                var regenCor = StartCoroutine(TimedBuff_Regen(Mathf.RoundToInt(augment.value), totalRegen));
                _timedRegenCoroutines[augment] = regenCor;
                _timedRemaining[augment] = totalRegen;
                break;
        }
    }

    private void ExpireTimedAugment(AugmentDatabase.AugmentData a)
    {
        if (a == null) return;
        _selectedAugments.RemoveAll(item => item != null && item.type == a.type);

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
                FlushStatsToCombat();
                break;

            case AugmentDatabase.AugmentType.SwiftShadow:
                augBonusSpeed -= a.value * 0.01f;
                if (augBonusSpeed < 0f) augBonusSpeed = 0f;
                FlushStatsToController();
                break;

            case AugmentDatabase.AugmentType.RegenerativeCore:
                if (_timedRegenCoroutines.TryGetValue(a, out var c) && c != null)
                {
                    StopCoroutine(c);
                    _timedRegenCoroutines.Remove(a);
                }
                break;
        }
    }

    public float GetRemainingDuration(AugmentDatabase.AugmentData a)
    {
        if (a != null && _timedRemaining.TryGetValue(a, out float rem))
        {
            return rem;
        }
        return a != null ? a.duration : 0f;
    }

    public float GetRemainingDurationByType(AugmentDatabase.AugmentType type)
    {
        foreach (var kvp in _timedRemaining)
        {
            if (kvp.Key != null && kvp.Key.type == type)
            {
                return kvp.Value;
            }
        }
        return 0f;
    }

    public int GetAugmentRank(AugmentDatabase.AugmentType type)
    {
        int count = 0;
        for (int i = 0; i < _selectedAugments.Count; i++)
        {
            if (_selectedAugments[i] != null && _selectedAugments[i].type == type)
                count++;
        }
        return count;
    }

    public bool HasAugment(AugmentDatabase.AugmentType type)
    {
        for (int i = 0; i < _selectedAugments.Count; i++)
        {
            if (_selectedAugments[i] != null && _selectedAugments[i].type == type)
                return true;
        }
        return false;
    }

    public bool IsTimedAugment(AugmentDatabase.AugmentData a)
    {
        return a != null && a.duration > 0f;
    }

    // ─── Public accessors (read by PlayerCombat / health) ─────────────────────
    public int GetAugBonusAtk()
    {
        int total = augBonusAtk + berserkerStacks * 3;
        // Undying Sovereign: tăng tới +80% sát thương khi máu thấp
        if (hasUndyingSovereign && PlayerHealth.Instance != null)
        {
            float hpPct = (float)PlayerHealth.Instance.CurrentHealth / Mathf.Max(1, PlayerHealth.Instance.MaxHealth);
            float lowHpBonus = Mathf.Clamp01(1f - hpPct) * 0.80f;
            total += Mathf.RoundToInt(total * lowHpBonus);
        }
        return total;
    }

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
    public int   GetBerserkerStacks() => berserkerStacks;
    public int   GetSoulHarvestHp()   => soulHarvestHp;
    public Dictionary<AugmentDatabase.AugmentData, float> GetActiveTimedAugments() => _timedRemaining;
    public bool HasActiveTimedAugments() => _timedRemaining != null && _timedRemaining.Count > 0;

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

    public void ResetAllAugments()
    {
        ResetForNewBattle();
    }
}
