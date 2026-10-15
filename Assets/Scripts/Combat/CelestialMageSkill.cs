using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Kỹ năng Chiến đấu và Quản lý Hoạt ảnh của Player 6 - Pháp Sư Tinh Tú (Celestial Mage)
/// Phân lớp: Pháp Sư / Tinh Vân & Thiên Thạch (Arcane Missiles, Astral Aegis, Void Blink & Meteor Apocalypse)
/// </summary>
public class CelestialMageSkill : MonoBehaviour
{
    [Header("--- THÔNG SỐ CẦU MA PHÁP (ARCANE STAR ORB) ---")]
    public int baseOrbDamage = 65; // Đòn bắn tự tìm mục tiêu uy lực cao
    public float orbSpeed = 12.0f; // Tốc độ bay thẳng thần tốc chính xác (không bị cong)
    public float attackCooldown = 0.75f; // Tốc độ đánh chậm (0.75s) bù đắp cho khả năng tự khóa mục tiêu
    private float attackCooldownTimer = 0f;

    [Header("--- KHIÊN HỘ THỂ TINH TÚ & THIÊN QUANG (ASTRAL AEGIS) ---")]
    [Range(0.4f, 0.8f)]
    public float barrierDamageReduction = 0.60f;
    public float maxChargeDuration = 1.2f;
    private bool isChargingBarrier = false;
    private float currentChargeDuration = 0f;
    private Vector2 currentAimDir = Vector2.down;
    private GameObject activeBarrierVFX = null;

    [Header("--- TUYỆT KỸ: THIÊN THẠCH MA TRẬN (METEOR APOCALYPSE) ---")]
    public float specialCooldown = 15.0f;
    private float specialCooldownTimer = 0f;
    public int meteorCount = 5;
    public int baseMeteorDamage = 95;
    private bool isExecutingSkill = false;

    [Header("--- TARGETING LAYERS ---")]
    public LayerMask enemyLayers;

    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    private PlayerController playerController;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Cached Sprite Frames from Assets/ART/Player6/sample(idle&walk)/
    private static Sprite[] idleFrames;
    private static Sprite[] walkFrames;
    private static Sprite[] attackFrames;
    private static Sprite[] dashFrames;
    private static Sprite[] hurtFrames;
    private static Sprite[] deathFrames;
    private static Sprite[] ultiFrames;

    // VFX Animation Sequences from Assets/ART/Player6/VFX/
    private static Sprite[] vfxCastMuzzleFlashFrames;
    private static Sprite[] vfxHitImpactFrames;
    private static Sprite[] vfxAstralAegisFrames;
    private static Sprite[] vfxNovaBurstFrames;
    private static Sprite[] vfxBlinkWarpFrames;
    private static Sprite[] vfxMeteorRuneFrames;
    private static Sprite[] vfxMeteorExplosionFrames;

    public enum MageAnimState { Idle, Walk, Attack, Dash, Hurt, Ulti, Death }
    private MageAnimState currentActionState = MageAnimState.Idle;
    private Coroutine actionAnimCoroutine = null;

    private static Texture2D orbTex;
    private static Sprite orbSprite;
    private static Texture2D meteorTex;
    private static Sprite meteorSprite;
    private static Texture2D runeCircleTex;
    private static Sprite runeCircleSprite;

    [Header("--- THẦN NỘ TINH TÚ (CELESTIAL OVERDRIVE) ---")]
    private bool isOverdriveActive = false;
    private float overdriveDuration = 8.0f; // Duy trì 8 giây hồi chiêu cuối 1s liên tục để spam
    private float overdriveTimer = 0f;
    private GameObject overdriveVFX = null;

    public bool IsOverdriveActive => isOverdriveActive;
    public float OverdriveRatio => (overdriveDuration > 0f) ? Mathf.Clamp01(overdriveTimer / overdriveDuration) : 0f;

    public bool IsChargingBarrier => isChargingBarrier;
    public bool IsExecutingSkill => isExecutingSkill;
    public bool IsAttackReady => attackCooldownTimer <= 0f && !isExecutingSkill && !isChargingBarrier;
    
    public bool HasEnoughSpecialEnergy()
    {
        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        float cost = isOverdriveActive ? 25f : 35f;
        return (energySys == null) || (energySys.GetEnergy() >= cost);
    }

    public bool IsSpecialReady => specialCooldownTimer <= 0f && !isExecutingSkill && HasEnoughSpecialEnergy();
    public float AttackCooldownRemaining => Mathf.Max(0f, attackCooldownTimer);
    public float AttackCooldownRatio => (attackCooldown > 0f) ? Mathf.Clamp01(attackCooldownTimer / attackCooldown) : 0f;
    public float SpecialCooldown => specialCooldown;
    public float SpecialCooldownRemaining => Mathf.Max(0f, specialCooldownTimer);
    public float SpecialCooldownRatio => (specialCooldown > 0f) ? Mathf.Clamp01(specialCooldownTimer / (isOverdriveActive ? 1.0f : specialCooldown)) : 0f;
    public float CurrentChargeRatio => Mathf.Clamp01(currentChargeDuration / maxChargeDuration);

    void Awake()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerHealth = GetComponent<PlayerHealth>();
        playerController = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Thu nhỏ kích thước nhân vật về chuẩn cân đối
        transform.localScale = new Vector3(0.62f, 0.62f, 1f);

        if (animator != null && animator.enabled)
        {
            animator.enabled = false;
        }

        if (enemyLayers.value == 0)
        {
            enemyLayers = LayerMask.GetMask("Enemy", "Default", "Obstacle");
            if (enemyLayers.value == 0) enemyLayers = ~0;
        }

        RefreshCooldownStats();
        LoadAllPlayer6Sprites();
        EnsureProceduralSprites();
    }

    void OnEnable()
    {
        transform.localScale = new Vector3(0.62f, 0.62f, 1f);
        RefreshCooldownStats();
        if (animator != null && animator.enabled)
        {
            animator.enabled = false;
        }
    }

    public void RefreshCooldownStats()
    {
        float totalCdr = 0f;
        var upgradeMgr = PlayerUpgradeManager.EnsureInstance();
        if (upgradeMgr != null)
        {
            totalCdr += upgradeMgr.GetUltiCooldownReduction();
        }

        EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out _, out _, out _, out float gemCdr, out _, out _);
        if (gemCdr > 0f)
        {
            totalCdr += 15.0f * gemCdr;
        }

        specialCooldown = Mathf.Max(5.0f, 15.0f - totalCdr);
    }

    public void ReduceSpecialCooldown(float seconds)
    {
        specialCooldownTimer = Mathf.Max(0f, specialCooldownTimer - seconds);
    }

    void Update()
    {
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;
        if (specialCooldownTimer > 0f) specialCooldownTimer -= Time.deltaTime;

        if (isChargingBarrier)
        {
            currentChargeDuration += Time.deltaTime;
        }

        // Quản lý Trạng thái Bùng Nổ Thiên Quang (Celestial Overdrive)
        if (isOverdriveActive)
        {
            overdriveTimer -= Time.deltaTime;

            // Đồng bộ rút dần thanh nội năng về 0 trong suốt 8 giây
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (energySys != null)
            {
                energySys.SetInnerEnergy((overdriveTimer / overdriveDuration) * energySys.GetMaxInnerEnergy());
            }

            if (overdriveTimer <= 0f)
            {
                EndCelestialOverdrive();
            }
        }
    }

    /// <summary>
    /// Kích hoạt trạng thái Bùng Nổ Thiên Quang khi thanh nội năng đạt 100%.
    /// Giảm cooldown chiêu cuối về 1 giây để người chơi spam thiên thạch liên tục!
    /// </summary>
    public void ActivateCelestialOverdrive()
    {
        if (isOverdriveActive) return;
        isOverdriveActive = true;
        overdriveTimer = overdriveDuration;

        // Reset hồi chiêu cuối ngay lập tức để người chơi thi triển được luôn!
        specialCooldownTimer = 0f;

        // Hồi nhẹ 25 năng lượng khi thức tỉnh nộ
        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null)
        {
            energySys.RestoreEnergy(25f);
        }

        // Tạo hào quang Tinh Tú bừng sáng quanh nhân vật
        if (vfxAstralAegisFrames != null && vfxAstralAegisFrames.Length > 0)
        {
            overdriveVFX = SpawnAnimatedVFX("OverdriveAegisAura", transform.position, vfxAstralAegisFrames, 0.03f, scale: 1.8f, tint: new Color(0.95f, 0.45f, 1f, 0.9f), sortingOrder: 15, loop: true);
            if (overdriveVFX != null)
            {
                overdriveVFX.transform.SetParent(transform, true);
                overdriveVFX.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            }
        }

        SoundManager.EnsureInstance()?.PlayHitImpact();
        SoundManager.EnsureInstance()?.PlayPortalSpawn();

        Debug.Log("<color=magenta>[CELESTIAL OVERDRIVE]</color> Nội Năng đầy! Bùng nổ trạng thái Hồi Chiêu Cuối 1s liên tục!");
    }

    public void EndCelestialOverdrive()
    {
        isOverdriveActive = false;
        overdriveTimer = 0f;
        if (overdriveVFX != null)
        {
            Destroy(overdriveVFX);
            overdriveVFX = null;
        }

        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null)
        {
            energySys.SetInnerEnergy(0f);
        }

        Debug.Log("<color=purple>[CELESTIAL OVERDRIVE]</color> Kết thúc trạng thái Overdrive!");
    }

    /// <summary>
    /// Kiểm tra mục tiêu có phải là quái Boss không.
    /// Theo yêu cầu, chỉ khi gây sát thương lên quái Boss mới được tích lũy Nội Năng cho Player 6.
    /// Bao gồm:
    /// - Titan Wheel (BotWheelAI) tại Map 1 (toàn bộ BotWheel đều là quái máy cơ giới / Boss của map)
    /// - Valley Golem (ValleyGolemBossAI) tại Map 2
    /// - Skeleton King (SkeletonKingBossAI) tại Map 3
    /// - DummyEnemy (Bù nhìn luyện tập)
    /// - BossGigapackSkills hoặc bất kỳ kẻ địch nào có cờ isBoss / IsBoss
    /// - Mọi quái vật trong phòng Boss (TitanSanctum)
    /// </summary>
    public static bool IsBossTarget(GameObject obj)
    {
        if (obj == null) return false;

        // 1. Kiểm tra các component Boss cụ thể (tìm cả trên object, parent, và root)
        if (obj.GetComponent<ValleyGolemBossAI>() != null || obj.GetComponentInParent<ValleyGolemBossAI>() != null || (obj.transform.root != null && obj.transform.root.GetComponent<ValleyGolemBossAI>() != null)) return true;
        if (obj.GetComponent<SkeletonKingBossAI>() != null || obj.GetComponentInParent<SkeletonKingBossAI>() != null || (obj.transform.root != null && obj.transform.root.GetComponent<SkeletonKingBossAI>() != null)) return true;
        if (obj.GetComponent<BossGigapackSkills>() != null || obj.GetComponentInParent<BossGigapackSkills>() != null || (obj.transform.root != null && obj.transform.root.GetComponent<BossGigapackSkills>() != null)) return true;
        if (obj.GetComponent<DummyEnemy>() != null || obj.GetComponentInParent<DummyEnemy>() != null || (obj.transform.root != null && obj.transform.root.GetComponent<DummyEnemy>() != null)) return true;

        // 2. BotWheelAI (Titan Wheel của Map 1): Toàn bộ BotWheel trong Map 1 là quái cơ giới / Boss của map này
        var bot = obj.GetComponent<BotWheelAI>() ?? obj.GetComponentInParent<BotWheelAI>() ?? (obj.transform.root != null ? obj.transform.root.GetComponent<BotWheelAI>() : null);
        if (bot != null) return true;

        // 3. BloodMonsterAI (Boss Quái Máu)
        var blood = obj.GetComponent<BloodMonsterAI>() ?? obj.GetComponentInParent<BloodMonsterAI>() ?? (obj.transform.root != null ? obj.transform.root.GetComponent<BloodMonsterAI>() : null);
        if (blood != null && (blood.isBoss || blood.IsBoss)) return true;

        // 4. Kiểm tra tên GameObject (kiểm tra cả obj.name và obj.transform.root.name)
        string n1 = obj.name.ToLower();
        string n2 = (obj.transform.root != null) ? obj.transform.root.name.ToLower() : "";
        if (n1.Contains("boss") || n1.Contains("king") || n1.Contains("golem") || n1.Contains("titan") || n1.Contains("cocoon") || n1.Contains("wheel") || n1.Contains("bot") || n1.Contains("dummy")) return true;
        if (n2.Contains("boss") || n2.Contains("king") || n2.Contains("golem") || n2.Contains("titan") || n2.Contains("cocoon") || n2.Contains("wheel") || n2.Contains("bot") || n2.Contains("dummy")) return true;

        // 5. Kiểm tra cờ isBoss thông qua reflection nếu có
        var mbs = obj.GetComponents<MonoBehaviour>();
        if (mbs != null)
        {
            foreach (var mb in mbs)
            {
                if (mb == null) continue;
                var prop = mb.GetType().GetProperty("IsBoss");
                if (prop != null && prop.PropertyType == typeof(bool) && (bool)prop.GetValue(mb)) return true;
                var field = mb.GetType().GetField("isBoss");
                if (field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(mb)) return true;
            }
        }

        // 6. Nếu đang ở trong phòng Boss TitanSanctum, tất cả quái xuất hiện đều tính là Boss
        var roomMgr = DungeonRoomManager.Instance;
        if (roomMgr != null && roomMgr.currentRoom != null && roomMgr.currentRoom.roomType == DungeonRoom.RoomType.TitanSanctum)
        {
            return true;
        }

        return false;
    }

    private float animTimer = 0f;
    private int animIndex = 0;

    void LateUpdate()
    {
        if (playerHealth != null && playerHealth.IsDead) return;

        // Vô hiệu hóa Animator để toàn quyền điều khiển Sprite theo mẫu Player6
        if (animator != null && animator.enabled)
        {
            animator.enabled = false;
        }

        // Cập nhật hướng xoay mặt flipX
        if (spriteRenderer != null && playerController != null)
        {
            Vector2 curDir = playerController.GetFacingDirection();
            // Ảnh gốc Player 6 nhìn sang TRÁI (Left-facing)
            // Di chuyển sang Phải (curDir.x > 0) -> lật flipX = true
            // Di chuyển sang Trái (curDir.x < 0) -> giữ flipX = false
            if (curDir.x > 0.05f)
            {
                spriteRenderer.flipX = true;
            }
            else if (curDir.x < -0.05f)
            {
                spriteRenderer.flipX = false;
            }
            else if (playerController.GetLastHorizontalFacing() > 0f)
            {
                spriteRenderer.flipX = true;
            }
            else if (playerController.GetLastHorizontalFacing() < 0f)
            {
                spriteRenderer.flipX = false;
            }
        }

        // Nếu đang trong hoạt ảnh hành động riêng (Attack, Dash, Hurt, Ulti), không ghi đè Sprite
        if (currentActionState != MageAnimState.Idle) return;

        bool isMoving = (playerController != null && playerController.IsMoving);
        Sprite[] frames = isMoving ? walkFrames : idleFrames;
        float frameInterval = isMoving ? 0.075f : 0.10f;

        if (frames == null || frames.Length == 0) frames = idleFrames;
        if (frames == null || frames.Length == 0) return;

        animTimer += Time.deltaTime;
        if (animTimer >= frameInterval)
        {
            animTimer = 0f;
            animIndex = (animIndex + 1) % frames.Length;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = frames[animIndex % frames.Length];
        }
    }

    public void PlayActionAnimation(MageAnimState state, Sprite[] frames, float frameDuration, System.Action onComplete = null)
    {
        if (playerHealth != null && playerHealth.IsDead && state != MageAnimState.Death) return;
        if (frames == null || frames.Length == 0) return;

        if (actionAnimCoroutine != null)
        {
            StopCoroutine(actionAnimCoroutine);
        }
        actionAnimCoroutine = StartCoroutine(ActionAnimationRoutine(state, frames, frameDuration, onComplete));
    }

    private IEnumerator ActionAnimationRoutine(MageAnimState state, Sprite[] frames, float frameDuration, System.Action onComplete)
    {
        currentActionState = state;
        for (int i = 0; i < frames.Length; i++)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = frames[i];
            }
            yield return new WaitForSeconds(frameDuration);
        }
        currentActionState = MageAnimState.Idle;
        actionAnimCoroutine = null;
        onComplete?.Invoke();
    }

    public void OnTakeDamageHurt()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (isExecutingSkill) return; // Super armor trong lúc cast Ulti
        if (hurtFrames != null && hurtFrames.Length > 0)
        {
            PlayActionAnimation(MageAnimState.Hurt, hurtFrames, 0.055f);
        }
    }

    // =========================================================================
    // 1. ĐÒN ĐÁNH CƠ BẢN: 1 CẦU TINH TÚ THẦN QUANG (SINGLE CELESTIAL STAR ORB)
    // =========================================================================
    public void PerformAttack(Vector2 direction, int effectiveDamage)
    {
        if (attackCooldownTimer > 0f || isExecutingSkill || isChargingBarrier) return;
        attackCooldownTimer = attackCooldown;

        if (direction.sqrMagnitude < 0.01f) direction = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
        direction = direction.normalized;

        // TỰ ĐỘNG TÌM VÀ KHÓA MỤC TIÊU BẮN THẲNG (AUTO-AIM STRAIGHT SHOT):
        // Tìm kẻ địch thích hợp nhất trong bán kính để đường đạn bắn thẳng tắp tới đích
        Transform target = FindBestTarget(direction, 11.5f);
        if (target != null)
        {
            Vector2 toTarget = ((Vector2)target.position + new Vector2(0f, 0.2f) - (Vector2)transform.position);
            if (toTarget.sqrMagnitude > 0.01f)
            {
                direction = toTarget.normalized;
            }
        }

        if (playerController != null) playerController.SetFacingDirection(direction);

        // Kích hoạt Hoạt ảnh Vung Gậy Bắn Cầu Phép (Attack Animation)
        if (attackFrames != null && attackFrames.Length > 0)
        {
            PlayActionAnimation(MageAnimState.Attack, attackFrames, 0.048f);
        }

        Vector3 spawnPos = transform.position + (Vector3)(direction * 0.45f) + new Vector3(0f, 0.15f, 0f);

        // Hiệu ứng Chớp Sáng Đầu Gậy (Cast Muzzle Flash VFX)
        if (vfxCastMuzzleFlashFrames != null && vfxCastMuzzleFlashFrames.Length > 0)
        {
            SpawnAnimatedVFX("CastMuzzleFlashVFX", spawnPos, vfxCastMuzzleFlashFrames, 0.035f, scale: 1.15f, tint: new Color(0.9f, 0.5f, 1f, 0.95f));
        }

        int finalDamage = (effectiveDamage > 0) ? (baseOrbDamage + (effectiveDamage - 10)) : baseOrbDamage;
        SpawnArcaneOrb(spawnPos, direction, finalDamage);

        SoundManager.EnsureInstance()?.PlayEnemyShoot();
    }

    /// <summary>
    /// Tìm mục tiêu kẻ địch thông minh để khóa mục tiêu bắn thẳng (Auto-Aim Target Lock)
    /// </summary>
    public Transform FindBestTarget(Vector2 aimDir, float searchRadius = 11.5f)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius, enemyLayers);
        Transform bestTarget = null;
        float bestScore = -9999f;
        float closestDist = float.MaxValue;
        Transform closestEnemy = null;

        foreach (var col in colliders)
        {
            if (col == null || col.isTrigger || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (dmg == null || dmg is PlayerHealth) continue;

            // Bỏ qua hòm kho báu hoặc đồ đạc đập vỡ
            bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
            if (isProp) continue;

            // Bỏ qua đối tượng đã bị tiêu diệt hoặc vô hiệu hóa
            var comp = dmg as Component;
            if (comp == null || !comp.gameObject.activeInHierarchy) continue;
            if (!col.enabled) continue;

            Transform enemyT = (dmg as Component)?.transform ?? col.transform;
            Vector2 toEnemy = (Vector2)enemyT.position - (Vector2)transform.position;
            float dist = toEnemy.magnitude;
            if (dist < 0.1f || dist > searchRadius) continue;

            if (dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = enemyT;
            }

            Vector2 dirToEnemy = toEnemy / dist;
            float dot = (aimDir.sqrMagnitude > 0.01f) ? Vector2.Dot(aimDir.normalized, dirToEnemy) : 1f;

            // Ưu tiên kẻ địch trước mặt hướng bắn và ở gần
            float score = (dot * 3.5f) - (dist * 0.25f);
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = enemyT;
            }
        }

        return bestTarget ?? closestEnemy;
    }

    private void SpawnArcaneOrb(Vector3 pos, Vector2 dir, int dmg)
    {
        GameObject orbObj = new GameObject("CelestialStarOrb", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D));
        orbObj.transform.position = pos;

        var sr = orbObj.GetComponent<SpriteRenderer>();
        if (vfxNovaBurstFrames != null && vfxNovaBurstFrames.Length > 1)
        {
            sr.sprite = vfxNovaBurstFrames[1]; // Phi tiêu năng lượng hình thoi sắc bén
        }
        else if (vfxNovaBurstFrames != null && vfxNovaBurstFrames.Length > 0)
        {
            sr.sprite = vfxNovaBurstFrames[0];
        }
        else
        {
            sr.sprite = orbSprite;
        }

        sr.sortingOrder = 20;
        sr.color = Color.white;
        orbObj.transform.localScale = Vector3.one * 2.2f;

        // Xoay đầu đạn hướng thẳng theo chiều bắn
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        orbObj.transform.rotation = Quaternion.Euler(0, 0, angle);

        var col = orbObj.GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;

        var rb = orbObj.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = dir * orbSpeed;

        var script = orbObj.AddComponent<ArcaneOrbProjectile>();
        // Truyền vfxAstralAegisFrames (trọn bộ 31 frame) làm hiệu ứng nổ va chạm khi trúng đích
        script.Init(dir, dmg, orbSpeed, enemyLayers, playerCombat, playerHealth, vfxNovaBurstFrames, vfxAstralAegisFrames, runeCircleSprite);
    }

    // =========================================================================
    // 2. GỒNG THẾ THỦ & VỤ NỔ THIÊN QUANG (ASTRAL AEGIS & NOVA)
    // =========================================================================
    public void StartBarrierCharge(Vector2 aimDir)
    {
        if (isExecutingSkill) return;

        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null && energySys.GetEnergy() < 15f)
        {
            SoundManager.EnsureInstance()?.PlayMenuHover();
            return;
        }

        isChargingBarrier = true;
        currentChargeDuration = 0f;
        currentAimDir = aimDir;

        if (activeBarrierVFX == null)
        {
            if (vfxAstralAegisFrames != null && vfxAstralAegisFrames.Length > 0)
            {
                activeBarrierVFX = SpawnAnimatedVFX("CelestialBarrierVFX", transform.position, vfxAstralAegisFrames, 0.045f, scale: 1.55f, tint: new Color(0.9f, 0.45f, 1f, 0.92f), sortingOrder: 18, loop: true);
                activeBarrierVFX.transform.SetParent(transform, true);
                activeBarrierVFX.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            }
            else
            {
                activeBarrierVFX = new GameObject("CelestialBarrierVFX", typeof(SpriteRenderer));
                activeBarrierVFX.transform.SetParent(transform, false);
                activeBarrierVFX.transform.localPosition = new Vector3(0f, 0.15f, 0f);

                var sr = activeBarrierVFX.GetComponent<SpriteRenderer>();
                sr.sprite = runeCircleSprite;
                sr.sortingOrder = 18;
                sr.color = new Color(0.7f, 0.25f, 1f, 0.75f);
                activeBarrierVFX.transform.localScale = Vector3.one * 1.55f;
            }
        }

        SoundManager.EnsureInstance()?.PlayPortalSpawn();
    }

    public void UpdateBarrierCharge(Vector2 aimDir, float holdDuration)
    {
        currentAimDir = aimDir;
        currentChargeDuration = holdDuration;

        if (activeBarrierVFX != null)
        {
            float chargeProgress = Mathf.Clamp01(holdDuration / maxChargeDuration);
            float baseScale = 1.45f + chargeProgress * 0.45f;
            float pulse = baseScale + Mathf.Sin(Time.time * 10f) * 0.12f;
            activeBarrierVFX.transform.localScale = Vector3.one * pulse;
        }
    }

    public void ReleaseBarrierCharge(Vector2 aimDir, int bonusDamage)
    {
        if (!isChargingBarrier) return;
        isChargingBarrier = false;

        if (activeBarrierVFX != null)
        {
            Destroy(activeBarrierVFX);
            activeBarrierVFX = null;
        }

        float chargeRatio = Mathf.Clamp01(currentChargeDuration / maxChargeDuration);
        if (chargeRatio >= 0.18f)
        {
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (energySys != null && !energySys.TryConsumeEnergy(20f))
            {
                SoundManager.EnsureInstance()?.PlayMenuHover();
                currentChargeDuration = 0f;
                return;
            }

            // Kích hoạt Vụ Nổ Thiên Quang (Arcane Nova)
            StartCoroutine(TriggerArcaneNovaRoutine(chargeRatio, bonusDamage));
        }
        currentChargeDuration = 0f;
        GetComponent<PlayerController>()?.RefreshMoveInput();
    }

    private IEnumerator TriggerArcaneNovaRoutine(float chargeRatio, int bonusDamage)
    {
        Vector3 center = transform.position;
        float radius = 2.6f + chargeRatio * 1.6f;
        int damage = Mathf.RoundToInt((baseOrbDamage * 2.0f + bonusDamage) * (1f + chargeRatio * 0.85f));

        SoundManager.EnsureInstance()?.PlayHitImpact();
        SoundManager.EnsureInstance()?.PlayPortalSpawn();

        // 1. Tầng lõi: Vụ nổ hủy diệt Tinh Thể Vũ Trụ (Meteor Explosion - 13 frame chi tiết cực nét, không bị bể hình)
        if (vfxMeteorExplosionFrames != null && vfxMeteorExplosionFrames.Length > 0)
        {
            SpawnAnimatedVFX("NovaMeteorBlastVFX", center + new Vector3(0f, 0.15f, 0f), vfxMeteorExplosionFrames, 0.032f, scale: 2.6f + chargeRatio * 0.8f, tint: new Color(0.95f, 0.55f, 1f, 1f), sortingOrder: 25);
        }

        // 2. Tầng sóng xung kích: Vòng ma trận Tinh Tú mở rộng thần tốc (Astral Shockwave Ring - 31 frame quét nhanh)
        if (vfxAstralAegisFrames != null && vfxAstralAegisFrames.Length > 0)
        {
            SpawnAnimatedVFX("NovaShockwaveRingVFX", center, vfxAstralAegisFrames, 0.022f, scale: 2.8f + chargeRatio * 1.0f, tint: new Color(0.85f, 0.4f, 1f, 0.85f), sortingOrder: 21);
        }

        // 3. Tầng sàn: Vòng pháp trận Thiên Hà bừng sáng dưới chân (Meteor Rune - 18 frame)
        if (vfxMeteorRuneFrames != null && vfxMeteorRuneFrames.Length > 0)
        {
            SpawnAnimatedVFX("NovaGroundRuneVFX", center, vfxMeteorRuneFrames, 0.035f, scale: 2.2f + chargeRatio * 0.6f, tint: new Color(0.9f, 0.45f, 1f, 0.8f), sortingOrder: 4);
        }

        // Gây sát thương diện rộng và hất văng quái vật xung quanh
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayers);
        HashSet<IDamageable> damagedSet = new HashSet<IDamageable>();
        int bossHitCount = 0;

        foreach (var h in hits)
        {
            if (h == null || h.gameObject == gameObject) continue;
            var dmg = h.GetComponent<IDamageable>() ?? h.GetComponentInParent<IDamageable>();
            if (dmg != null && !(dmg is PlayerHealth) && !damagedSet.Contains(dmg))
            {
                damagedSet.Add(dmg);

                // Tính tỉ lệ nổ chí mạng cho Arcane Nova (Ưu đãi +15% crit chance)
                bool isCrit = (playerCombat != null) 
                    ? playerCombat.RollCritical(0.15f, out float critMult) 
                    : (UnityEngine.Random.value < 0.35f);
                float mult = isCrit ? 1.8f : 1.0f;
                int finalDamage = Mathf.RoundToInt(damage * mult);

                dmg.TakeDamage(finalDamage);
                DamagePopup.Create(h.transform.position, finalDamage, DamageType.Magic, isCritical: isCrit);

                GameObject hitTargetObj = (dmg as Component)?.gameObject ?? h.gameObject;
                if (IsBossTarget(hitTargetObj))
                {
                    bossHitCount++;
                }

                // Chớp lửa va chạm bùng lên trên người quái bị trúng chiêu
                if (vfxHitImpactFrames != null && vfxHitImpactFrames.Length > 0)
                {
                    SpawnAnimatedVFX("NovaTargetHitVFX", h.transform.position + new Vector3(0f, 0.2f, 0f), vfxHitImpactFrames, 0.035f, scale: 1.6f, tint: new Color(1f, 0.7f, 1f, 1f), sortingOrder: 26);
                }

                // Đẩy lùi mạnh mẽ kẻ địch ra xa
                var rb = h.GetComponent<Rigidbody2D>() ?? h.GetComponentInParent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 pushDir = ((Vector2)h.transform.position - (Vector2)center).normalized;
                    if (pushDir.sqrMagnitude < 0.01f) pushDir = Vector2.up;
                    rb.linearVelocity = pushDir * (7.5f + chargeRatio * 4.0f);
                }

                if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null)
                {
                    int healAmount = Mathf.Max(1, Mathf.RoundToInt(damage * playerCombat.LifestealPercent));
                    playerHealth.Heal(healAmount);
                }
            }
        }

        // TÍCH NỘI NĂNG KHI NỔ TRÚNG QUÁI VẬT
        if (damagedSet.Count > 0)
        {
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance ?? Object.FindAnyObjectByType<PlayerEnergySystem>();
            if (energySys != null)
            {
                float gain = Mathf.Min(35f, 10f + (damagedSet.Count - 1) * 4f + bossHitCount * 10f);
                energySys.AddInnerEnergy(gain);
                Debug.Log($"<color=magenta>[CELESTIAL INNER ENERGY]</color> Arcane Nova nổ trúng {damagedSet.Count} quái! +{gain} Nội Năng.");
            }
        }

        yield return null;
    }

    // =========================================================================
    // 3. TỐC BIẾN HƯ KHÔNG (ARCANE BLINK)
    // =========================================================================
    public void OnPerformBlink(Vector2 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.01f) moveDir = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
        moveDir = moveDir.normalized;

        // Kích hoạt Hoạt ảnh Tốc Biến Hư Không (Dash Animation)
        if (dashFrames != null && dashFrames.Length > 0)
        {
            PlayActionAnimation(MageAnimState.Dash, dashFrames, 0.045f);
        }

        Vector3 startPos = transform.position;
        float maxBlinkDistance = 3.2f;
        float actualDist = maxBlinkDistance;

        // Quét chướng ngại vật, tường và bờ vực bằng CircleCast để không bao giờ tốc biến lọt ra ngoài map
        RaycastHit2D[] hits = Physics2D.CircleCastAll(startPos, 0.35f, moveDir, maxBlinkDistance);
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger || h.collider.gameObject == gameObject) continue;
            if (h.collider.CompareTag("Player") || h.collider.GetComponent<PlayerHealth>() != null) continue;
            // Cho phép Pháp Sư lướt xuyên người quái vật
            if (h.collider.CompareTag("Enemy") || h.collider.GetComponent<IDamageable>() != null) continue;

            // Bị tường hoặc vật cản chặn lại: dừng ngay trước bề mặt vật cản an toàn
            float safeDist = Mathf.Max(0f, h.distance - 0.35f);
            if (safeDist < actualDist)
            {
                actualDist = safeDist;
            }
        }

        Vector3 targetPos = startPos + (Vector3)(moveDir * actualDist);

        // Kiểm tra an toàn: Nếu điểm đến vẫn bị đè lên tường/vật cản, lùi lại từng bước nhỏ cho đến khi thông thoáng
        float checkRadius = 0.35f;
        int maxRetries = 10;
        while (actualDist > 0.1f && maxRetries > 0)
        {
            maxRetries--;
            Collider2D overlap = Physics2D.OverlapCircle(targetPos, checkRadius);
            if (overlap != null && !overlap.isTrigger && overlap.gameObject != gameObject && !overlap.CompareTag("Enemy") && overlap.GetComponent<IDamageable>() == null)
            {
                actualDist = Mathf.Max(0f, actualDist - 0.3f);
                targetPos = startPos + (Vector3)(moveDir * actualDist);
            }
            else
            {
                break;
            }
        }

        // Khóa chặt biên bản đồ theo CameraFollow để không bao giờ vượt ra ngoài phạm vi màn chơi
        var camFollow = CameraFollow.Instance ?? Object.FindAnyObjectByType<CameraFollow>();
        if (camFollow != null && camFollow.EnableBoundsClamp)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, camFollow.MinBounds.x + 1.0f, camFollow.MaxBounds.x - 1.0f);
            targetPos.y = Mathf.Clamp(targetPos.y, camFollow.MinBounds.y + 1.0f, camFollow.MaxBounds.y - 1.0f);
        }

        // Triệt tiêu quán tính vật lý
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Hiệu ứng Cổng Tốc Biến Hư Không (Blink Warp VFX) tại điểm đi và điểm đến
        if (vfxBlinkWarpFrames != null && vfxBlinkWarpFrames.Length > 0)
        {
            SpawnAnimatedVFX("BlinkWarpStartVFX", startPos, vfxBlinkWarpFrames, 0.038f, scale: 1.3f, tint: new Color(0.85f, 0.4f, 1f, 0.95f));
            SpawnAnimatedVFX("BlinkWarpEndVFX", targetPos, vfxBlinkWarpFrames, 0.038f, scale: 1.3f, tint: new Color(0.85f, 0.4f, 1f, 0.95f));
        }

        // Tạo Vết Rách Hư Không (Warp Singularity) tại vị trí cũ
        SpawnSingularityRift(startPos);

        // Dịch chuyển tức thời an toàn
        transform.position = targetPos;

        // Giảm hồi chiêu Tuyệt Kỹ 1.5s
        ReduceSpecialCooldown(1.5f);
        attackCooldownTimer = 0f;

        SoundManager.EnsureInstance()?.PlayPortalSpawn();
    }

    private void SpawnSingularityRift(Vector3 pos)
    {
        GameObject rift = new GameObject("SingularityRift", typeof(SpriteRenderer));
        rift.transform.position = pos;
        var sr = rift.GetComponent<SpriteRenderer>();
        sr.sprite = runeCircleSprite;
        sr.sortingOrder = 14;
        sr.color = new Color(0.7f, 0.2f, 1f, 0.75f);
        rift.transform.localScale = Vector3.one * 1.8f;

        StartCoroutine(SingularityRiftRoutine(rift, pos));
    }

    private IEnumerator SingularityRiftRoutine(GameObject rift, Vector3 pos)
    {
        float timer = 0f;
        float duration = 1.6f;
        int tickDmg = Mathf.Max(8, baseOrbDamage / 4);

        while (timer < duration && rift != null)
        {
            timer += 0.35f;
            rift.transform.Rotate(0, 0, -240f * 0.35f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 1.6f, enemyLayers);
            HashSet<IDamageable> damaged = new HashSet<IDamageable>();
            foreach (var h in hits)
            {
                if (h == null || h.gameObject == gameObject) continue;
                var dmg = h.GetComponent<IDamageable>() ?? h.GetComponentInParent<IDamageable>();
                if (dmg != null && !(dmg is PlayerHealth) && !damaged.Contains(dmg))
                {
                    damaged.Add(dmg);
                    dmg.TakeDamage(tickDmg);
                    DamagePopup.Create(h.transform.position, tickDmg, DamageType.Magic);
                }
            }
            yield return new WaitForSeconds(0.35f);
        }

        if (rift != null) Destroy(rift);
    }

    // =========================================================================
    // 4. TUYỆT KỸ: THIÊN THẠCH MA TRẬN (METEOR APOCALYPSE)
    // =========================================================================
    // =========================================================================
    // 4. TUYỆT KỸ: THIÊN THẠCH MA TRẬN (METEOR APOCALYPSE)
    // =========================================================================
    public bool TriggerSpecialSkill()
    {
        if (!IsSpecialReady) return false;

        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        float cost = isOverdriveActive ? 25f : 35f;
        if (energySys != null && !energySys.TryConsumeEnergy(cost))
        {
            SoundManager.EnsureInstance()?.PlayMenuHover();
            return false;
        }

        StartCoroutine(ExecuteMeteorApocalypseRoutine());
        return true;
    }

    public IEnumerator ExecuteMeteorApocalypseRoutine()
    {
        isExecutingSkill = true;
        // Trong trạng thái Bùng Nổ Thiên Quang (Overdrive): Cooldown chỉ 1 giây để người chơi spam liên tục!
        specialCooldownTimer = isOverdriveActive ? 1.0f : specialCooldown;

        var cam = FindAnyObjectByType<CameraFollow>();
        var health = GetComponent<PlayerHealth>();

        // Kích hoạt Hoạt ảnh Niệm Đại Tuyệt Kỹ (Ulti Animation)
        if (ultiFrames != null && ultiFrames.Length > 0)
        {
            PlayActionAnimation(MageAnimState.Ulti, ultiFrames, 0.08f);
        }

        try
        {
            if (health != null) health.isInvulnerable = true;
            if (playerController != null) playerController.SetCanMove(false);

            // Zoom OUT camera rộng toàn cảnh chiến trường (Wide Arena View)
            if (cam != null) cam.SetCinematicZoom(7.2f);

            SoundManager.EnsureInstance()?.PlayPortalSpawn();
            SoundManager.EnsureInstance()?.PlaySwordSlash();

            yield return new WaitForSeconds(0.2f);
        }
        finally
        {
            if (health != null) health.isInvulnerable = false;
            if (playerController != null) playerController.SetCanMove(true);
        }

        // Bắt đầu triệu hồi mưa thiên thạch dội xuống toàn bản đồ
        StartCoroutine(SummonMeteorStormRoutine());

        // Trong trạng thái Overdrive, giải phóng trạng thái thi triển sau 0.5s để người chơi chuẩn bị bấm tiếp sau 1s!
        yield return new WaitForSeconds(isOverdriveActive ? 0.5f : 1.8f);
        if (cam != null) cam.ResetZoom();
        isExecutingSkill = false;
    }

    private IEnumerator SummonMeteorStormRoutine()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 12f, enemyLayers);
        List<Vector3> targetPositions = new List<Vector3>();

        foreach (var n in nearby)
        {
            if (n == null || n.gameObject == gameObject) continue;
            var dmg = n.GetComponent<IDamageable>() ?? n.GetComponentInParent<IDamageable>();
            if (dmg != null && !(dmg is PlayerHealth))
            {
                targetPositions.Add(n.transform.position);
            }
        }

        for (int i = 0; i < meteorCount; i++)
        {
            Vector3 impactPos = transform.position;
            if (targetPositions.Count > 0)
            {
                impactPos = targetPositions[i % targetPositions.Count] + (Vector3)Random.insideUnitCircle * 1.2f;
            }
            else
            {
                impactPos = transform.position + (Vector3)Random.insideUnitCircle * 4.5f;
            }

            StartCoroutine(SpawnSingleMeteor(impactPos));
            yield return new WaitForSeconds(0.28f);
        }
    }

    private IEnumerator SpawnSingleMeteor(Vector3 targetPos)
    {
        // 1. Cảnh báo vòng ấn chú ma pháp dưới đất (Meteor Warning Rune VFX)
        GameObject runeObj = null;
        if (vfxMeteorRuneFrames != null && vfxMeteorRuneFrames.Length > 0)
        {
            runeObj = SpawnAnimatedVFX("MeteorWarningRuneVFX", targetPos, vfxMeteorRuneFrames, 0.05f, scale: 2.2f, tint: new Color(1f, 0.35f, 0.85f, 0.9f), sortingOrder: 12, loop: true);
        }
        else
        {
            runeObj = new GameObject("MeteorWarningRune", typeof(SpriteRenderer));
            runeObj.transform.position = targetPos;
            var rSr = runeObj.GetComponent<SpriteRenderer>();
            rSr.sprite = runeCircleSprite;
            rSr.sortingOrder = 12;
            rSr.color = new Color(1f, 0.2f, 0.8f, 0.4f);
            runeObj.transform.localScale = Vector3.one * 2.6f;
        }

        // 2. Thiên thạch lao từ góc trên cao xuống
        Vector3 spawnOffset = new Vector3(-2.5f, 6.0f, 0f);
        Vector3 meteorStart = targetPos + spawnOffset;

        GameObject meteorObj = new GameObject("FallingMeteor", typeof(SpriteRenderer));
        meteorObj.transform.position = meteorStart;
        var mSr = meteorObj.GetComponent<SpriteRenderer>();
        mSr.sprite = meteorSprite;
        mSr.sortingOrder = 20;
        mSr.color = new Color(1f, 0.5f, 0.2f, 1f);
        meteorObj.transform.localScale = Vector3.one * 1.6f;

        float fallDuration = 0.45f;
        float elapsed = 0f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;
            if (meteorObj != null)
            {
                meteorObj.transform.position = Vector3.Lerp(meteorStart, targetPos, t * t);
                meteorObj.transform.Rotate(0, 0, 360f * Time.deltaTime);
            }
            yield return null;
        }

        if (meteorObj != null) Destroy(meteorObj);
        if (runeObj != null) Destroy(runeObj);

        // 3. Vụ Nổ Thiên Thạch (Impact Explosion)
        ExecuteMeteorImpact(targetPos);
    }

    private void ExecuteMeteorImpact(Vector3 impactPos)
    {
        SoundManager.EnsureInstance()?.PlayHitImpact();
        SoundManager.EnsureInstance()?.PlaySwordSlash();

        // Hiệu ứng Vụ Nổ Thiên Thạch (Meteor Explosion VFX)
        if (vfxMeteorExplosionFrames != null && vfxMeteorExplosionFrames.Length > 0)
        {
            SpawnAnimatedVFX("MeteorExplosionVFX", impactPos, vfxMeteorExplosionFrames, 0.04f, scale: 2.8f, tint: new Color(1f, 0.55f, 0.25f, 0.98f), sortingOrder: 22);
        }
        else
        {
            GameObject impactRing = new GameObject("MeteorImpactRing", typeof(SpriteRenderer));
            impactRing.transform.position = impactPos;
            var sr = impactRing.GetComponent<SpriteRenderer>();
            sr.sprite = runeCircleSprite;
            sr.sortingOrder = 16;
            sr.color = new Color(1f, 0.4f, 0.9f, 0.95f);
            StartCoroutine(AnimateImpactRing(impactRing, 3.2f));
        }

        // Gây sát thương nổ
        int totalDamage = baseMeteorDamage + (playerCombat != null ? playerCombat.BonusAttackDamage : 0);
        Collider2D[] hits = Physics2D.OverlapCircleAll(impactPos, 2.5f, enemyLayers);
        HashSet<IDamageable> damagedSet = new HashSet<IDamageable>();
        int bossHitCount = 0;

        foreach (var h in hits)
        {
            if (h == null || h.gameObject == gameObject) continue;
            var dmg = h.GetComponent<IDamageable>() ?? h.GetComponentInParent<IDamageable>();
            if (dmg != null && !(dmg is PlayerHealth) && !damagedSet.Contains(dmg))
            {
                damagedSet.Add(dmg);

                // Thiên Thạch Rơi Ulti: Tỉ lệ Đại Chí Mạng cực cao (Ưu đãi +25% crit chance)
                bool isCrit = (playerCombat != null) 
                    ? playerCombat.RollCritical(0.25f, out float critMult) 
                    : (UnityEngine.Random.value < 0.45f);
                float mult = isCrit ? 2.0f : 1.0f; // Sát thương gấp đôi khi nổ đại chí mạng!
                int finalDmg = Mathf.RoundToInt(totalDamage * mult);

                dmg.TakeDamage(finalDmg);
                DamagePopup.Create(h.transform.position, finalDmg, DamageType.Magic, isCritical: isCrit);
                if (playerCombat != null && playerCombat.BonusTrueDamage > 0)
                {
                    int finalTrue = Mathf.RoundToInt(playerCombat.BonusTrueDamage * 2 * mult);
                    DamagePopup.Create(h.transform.position + Vector3.up * 0.4f, finalTrue, DamageType.TrueDamage, isCritical: isCrit);
                }

                GameObject hitTargetObj = (dmg as Component)?.gameObject ?? h.gameObject;
                if (IsBossTarget(hitTargetObj))
                {
                    bossHitCount++;
                }

                if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null)
                {
                    int healAmount = Mathf.Max(1, Mathf.RoundToInt(totalDamage * playerCombat.LifestealPercent));
                    playerHealth.Heal(healAmount);
                }
            }
        }

        // TÍCH NỘI NĂNG KHI NỔ TRÚNG QUÁI VẬT
        if (damagedSet.Count > 0)
        {
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance ?? Object.FindAnyObjectByType<PlayerEnergySystem>();
            if (energySys != null)
            {
                float gain = Mathf.Min(35f, 6f * damagedSet.Count + bossHitCount * 10f);
                energySys.AddInnerEnergy(gain);
                Debug.Log($"<color=magenta>[CELESTIAL INNER ENERGY]</color> Thiên thạch nổ trúng {damagedSet.Count} quái! +{gain} Nội Năng.");
            }
        }
    }

    private IEnumerator AnimateImpactRing(GameObject ring, float maxRadius)
    {
        float dur = 0.35f;
        float elapsed = 0f;
        var sr = ring.GetComponent<SpriteRenderer>();

        while (elapsed < dur && ring != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            ring.transform.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one * (maxRadius * 2f), t);
            if (sr != null) sr.color = new Color(1f, 0.4f, 0.9f, 1f - t);
            yield return null;
        }
        if (ring != null) Destroy(ring);
    }

    // =========================================================================
    // 5. TẢI TÀI NGUYÊN SPRITES VÀ TẠO HÌNH NỀN PROCEDURAL HD
    // =========================================================================
    public static Sprite[] GetDeathFrames()
    {
        LoadAllPlayer6Sprites();
        return deathFrames;
    }

    public static void LoadAllPlayer6Sprites()
    {
        if (idleFrames == null || idleFrames.Length == 0)
        {
#if UNITY_EDITOR
            var idleAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player6/sample(idle&walk)/idle/sprite sheets/idle.png");
            if (idleAssets != null && idleAssets.Length > 0)
            {
                List<Sprite> list = new List<Sprite>();
                foreach (var a in idleAssets) if (a is Sprite s) list.Add(s);
                if (list.Count > 0) idleFrames = list.ToArray();
            }
#endif
            if (idleFrames == null || idleFrames.Length == 0)
            {
                idleFrames = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player6/sample(idle&walk)/idle/sprite sheets/idle.png", 46, 55);
            }
        }

        if (walkFrames == null || walkFrames.Length == 0)
        {
#if UNITY_EDITOR
            var walkAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player6/sample(idle&walk)/walk/sprite sheets/walk.png");
            if (walkAssets != null && walkAssets.Length > 0)
            {
                List<Sprite> list = new List<Sprite>();
                foreach (var a in walkAssets) if (a is Sprite s) list.Add(s);
                if (list.Count > 0) walkFrames = list.ToArray();
            }
#endif
            if (walkFrames == null || walkFrames.Length == 0)
            {
                walkFrames = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player6/sample(idle&walk)/walk/sprite sheets/walk.png", 46, 55);
            }
        }

        if (attackFrames == null || attackFrames.Length == 0)
        {
            attackFrames = LoadFramesFromFolder("Assets/ART/Player6/sample(idle&walk)/attack", "attack");
        }

        if (dashFrames == null || dashFrames.Length == 0)
        {
            dashFrames = LoadFramesFromFolder("Assets/ART/Player6/sample(idle&walk)/dash", "dash");
        }

        if (hurtFrames == null || hurtFrames.Length == 0)
        {
            hurtFrames = LoadFramesFromFolder("Assets/ART/Player6/sample(idle&walk)/hurt", "hurt");
        }

        if (deathFrames == null || deathFrames.Length == 0)
        {
            deathFrames = LoadFramesFromFolder("Assets/ART/Player6/sample(idle&walk)/death", "death");
        }

        if (ultiFrames == null || ultiFrames.Length == 0)
        {
            ultiFrames = LoadFramesFromFolder("Assets/ART/Player6/sample(idle&walk)/ulti", "ulti");
        }

        // Tải 7 Bộ Hiệu Ứng Pixel VFX Chuyên Nghiệp từ Assets/ART/Player6/VFX/
        if (vfxCastMuzzleFlashFrames == null || vfxCastMuzzleFlashFrames.Length == 0)
        {
            vfxCastMuzzleFlashFrames = LoadFramesFromFolder("Assets/ART/Player6/VFX/CastMuzzleFlash", "frame");
        }
        if (vfxHitImpactFrames == null || vfxHitImpactFrames.Length == 0)
        {
            vfxHitImpactFrames = LoadFramesFromFolder("Assets/ART/Player6/VFX/HitImpact", "frame");
        }
        if (vfxAstralAegisFrames == null || vfxAstralAegisFrames.Length == 0)
        {
            vfxAstralAegisFrames = LoadFramesFromFolder("Assets/ART/Player6/VFX/AstralAegis", "frame");
        }
        if (vfxNovaBurstFrames == null || vfxNovaBurstFrames.Length == 0)
        {
            vfxNovaBurstFrames = LoadFramesFromFolder("Assets/ART/Player6/VFX/NovaBurst", "frame");
        }
        if (vfxBlinkWarpFrames == null || vfxBlinkWarpFrames.Length == 0)
        {
            vfxBlinkWarpFrames = LoadFramesFromFolder("Assets/ART/Player6/VFX/BlinkWarp", "frame");
        }
        if (vfxMeteorRuneFrames == null || vfxMeteorRuneFrames.Length == 0)
        {
            vfxMeteorRuneFrames = LoadFramesFromFolder("Assets/ART/Player6/VFX/MeteorRune", "frame");
        }
        if (vfxMeteorExplosionFrames == null || vfxMeteorExplosionFrames.Length == 0)
        {
            vfxMeteorExplosionFrames = LoadFramesFromFolder("Assets/ART/Player6/VFX/MeteorExplosion", "frame");
        }
    }

    public static GameObject SpawnAnimatedVFX(string name, Vector3 position, Sprite[] frames, float frameDuration, float scale = 1.0f, Color? tint = null, int sortingOrder = 22, bool loop = false)
    {
        if (frames == null || frames.Length == 0) return null;
        GameObject vfxObj = new GameObject(name, typeof(SpriteRenderer), typeof(AnimatedVFXPlayer));
        vfxObj.transform.position = position;
        vfxObj.transform.localScale = Vector3.one * scale;

        var sr = vfxObj.GetComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingOrder = sortingOrder;
        if (tint.HasValue) sr.color = tint.Value;

        var player = vfxObj.GetComponent<AnimatedVFXPlayer>();
        player.Play(frames, frameDuration, loop);
        return vfxObj;
    }

    public static Sprite[] LoadFramesFromFolder(string folderPath, string prefix)
    {
        List<Sprite> list = new List<Sprite>();
#if UNITY_EDITOR
        if (System.IO.Directory.Exists(folderPath))
        {
            var files = System.IO.Directory.GetFiles(folderPath, prefix + "*.png");
            System.Array.Sort(files, (a, b) => {
                string fA = System.IO.Path.GetFileNameWithoutExtension(a);
                string fB = System.IO.Path.GetFileNameWithoutExtension(b);
                return string.Compare(fA, fB, System.StringComparison.Ordinal);
            });

            foreach (var f in files)
            {
                var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(f.Replace("\\", "/"));
                if (s != null) list.Add(s);
            }
        }
#endif
        if (list.Count == 0)
        {
            string fullDir = System.IO.Path.Combine(Application.dataPath, folderPath.Replace("Assets/", ""));
            if (System.IO.Directory.Exists(fullDir))
            {
                var files = System.IO.Directory.GetFiles(fullDir, prefix + "*.png");
                System.Array.Sort(files, (a, b) => {
                    string fA = System.IO.Path.GetFileNameWithoutExtension(a);
                    string fB = System.IO.Path.GetFileNameWithoutExtension(b);
                    return string.Compare(fA, fB, System.StringComparison.Ordinal);
                });

                foreach (var f in files)
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(f);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Point;
                        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16);
                        list.Add(s);
                    }
                }
            }
        }
        return list.ToArray();
    }

    private static void EnsureProceduralSprites()
    {
        if (orbSprite == null)
        {
            int size = 64;
            orbTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            orbTex.filterMode = FilterMode.Bilinear;
            Vector2 c = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            Color cyanWhite = new Color(0.92f, 0.98f, 1f, 1f);
            Color deepViolet = new Color(0.85f, 0.25f, 1f, 1f);
            Color darkAstral = new Color(0.45f, 0.05f, 0.8f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), c);
                    float dx = Mathf.Abs(x - c.x);
                    float dy = Mathf.Abs(y - c.y);

                    // 4-pointed glowing astral star cross
                    bool isCrossCore = (dx <= 2f && dy <= 24f) || (dy <= 2f && dx <= 24f);
                    bool isCrossOuter = (dx <= 4.5f && dy <= 20f) || (dy <= 4.5f && dx <= 20f);

                    if (dist <= 7f)
                    {
                        // White brilliant core
                        float t = dist / 7f;
                        orbTex.SetPixel(x, y, Color.Lerp(cyanWhite, deepViolet, t));
                    }
                    else if (dist <= 16f)
                    {
                        // Violet-Magenta coronal plasma
                        float t = (dist - 7f) / 9f;
                        float alpha = Mathf.Clamp01(1f - t);
                        orbTex.SetPixel(x, y, new Color(deepViolet.r, deepViolet.g, deepViolet.b, alpha * 0.95f));
                    }
                    else if (isCrossCore)
                    {
                        float crossT = Mathf.Max(dx, dy) / 24f;
                        orbTex.SetPixel(x, y, new Color(cyanWhite.r, cyanWhite.g, cyanWhite.b, 1f - crossT));
                    }
                    else if (isCrossOuter)
                    {
                        float crossT = Mathf.Max(dx, dy) / 20f;
                        orbTex.SetPixel(x, y, new Color(deepViolet.r, deepViolet.g, deepViolet.b, (1f - crossT) * 0.6f));
                    }
                    else if (dist <= 26f)
                    {
                        // Outer stardust aura ring
                        float t = (dist - 16f) / 10f;
                        float alpha = Mathf.Clamp01(1f - t) * 0.35f;
                        orbTex.SetPixel(x, y, new Color(darkAstral.r, darkAstral.g, darkAstral.b, alpha));
                    }
                    else
                    {
                        orbTex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            orbTex.Apply();
            orbSprite = Sprite.Create(orbTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        }

        if (runeCircleSprite == null)
        {
            runeCircleTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(31.5f, 31.5f);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), c);
                    if (dist >= 26f && dist <= 31f)
                    {
                        runeCircleTex.SetPixel(x, y, new Color(0.85f, 0.35f, 1f, 0.95f));
                    }
                    else if (dist < 26f && dist >= 22f)
                    {
                        runeCircleTex.SetPixel(x, y, new Color(0.6f, 0.2f, 0.9f, 0.4f));
                    }
                    else
                    {
                        runeCircleTex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            runeCircleTex.Apply();
            runeCircleSprite = Sprite.Create(runeCircleTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32);
        }

        if (meteorSprite == null)
        {
            meteorTex = new Texture2D(48, 48, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(23.5f, 23.5f);
            for (int y = 0; y < 48; y++)
            {
                for (int x = 0; x < 48; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), c);
                    if (dist < 22f)
                    {
                        float alpha = Mathf.Clamp01(1f - (dist / 22f));
                        meteorTex.SetPixel(x, y, new Color(1f, 0.45f, 0.15f, alpha));
                    }
                    else
                    {
                        meteorTex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            meteorTex.Apply();
            meteorSprite = Sprite.Create(meteorTex, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 32);
        }
    }
}

/// <summary>
/// Đạn Cầu Tinh Tú Thần Quang (Radiant Celestial Star Orb Projectile)
/// Khóa mục tiêu chuẩn xác khi bắn và phóng theo một đường thẳng thần tốc (Straight-line Trajectory, không uốn cong)
/// </summary>
public class ArcaneOrbProjectile : MonoBehaviour
{
    private Vector2 direction;
    private int damage;
    private float speed;
    private LayerMask enemyLayers;
    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    private Sprite[] orbFrames;
    private Sprite[] hitVFXFrames;
    private Sprite runeSprite;
    private SpriteRenderer sr;
    private float lifetime = 2.5f;
    private float trailTimer = 0f;

    public void Init(Vector2 dir, int dmg, float spd, LayerMask layers, PlayerCombat pc, PlayerHealth ph, Sprite[] projFrames, Sprite[] hitFrames, Sprite runeSpr)
    {
        direction = dir.normalized;
        damage = dmg;
        speed = spd;
        enemyLayers = layers;
        playerCombat = pc;
        playerHealth = ph;
        orbFrames = projFrames;
        hitVFXFrames = hitFrames;
        runeSprite = runeSpr;
        sr = GetComponent<SpriteRenderer>();

        // Xoay đầu mũi phi tiêu thẳng theo vector hướng bắn
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
    }

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // 1. BAY THẲNG TUYỆT ĐỐI - KHÔNG XOAY HAY UỐN LƯỢN BỊ CONG
        // Giữ nguyên vector hướng đã khóa mục tiêu ban đầu
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // 2. Thả vệt bụi sao lấp lánh thẳng hàng sau đuôi (Stardust Trail)
        trailTimer += Time.deltaTime;
        if (trailTimer >= 0.04f)
        {
            trailTimer = 0f;
            SpawnStardustSpark();
        }
    }

    private void SpawnStardustSpark()
    {
        GameObject spark = new GameObject("StardustTrail", typeof(SpriteRenderer));
        spark.transform.position = transform.position + (Vector3)Random.insideUnitCircle * 0.12f;
        spark.transform.localScale = Vector3.one * Random.Range(0.4f, 0.7f);

        var sparkSr = spark.GetComponent<SpriteRenderer>();
        if (orbFrames != null && orbFrames.Length > 0)
        {
            sparkSr.sprite = orbFrames[0];
        }
        sparkSr.color = new Color(0.9f, 0.45f, 1f, 0.75f);
        sparkSr.sortingOrder = 19;

        StartCoroutine(FadeSparkRoutine(spark, sparkSr));
    }

    private IEnumerator FadeSparkRoutine(GameObject obj, SpriteRenderer sparkSr)
    {
        float dur = 0.2f;
        float elapsed = 0f;
        Color c = (sparkSr != null) ? sparkSr.color : Color.white;
        while (elapsed < dur && obj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            if (sparkSr != null)
            {
                sparkSr.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, t));
                obj.transform.localScale = Vector3.Lerp(obj.transform.localScale, Vector3.zero, t);
            }
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col == null) return;
        var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
        if (dmg != null && !(dmg is PlayerHealth))
        {
            // TÍNH TOÁN TỈ LỆ NỔ CHÍ MẠNG (Roll Crit với tỉ lệ cơ bản 25% hoặc lấy từ PlayerCombat)
            bool isCrit = (playerCombat != null) 
                ? playerCombat.RollCritical(0.05f, out float critMult) 
                : (UnityEngine.Random.value < 0.25f);
            float mult = (playerCombat != null) ? (isCrit ? playerCombat.CritMultiplier : 1.0f) : (isCrit ? 1.75f : 1.0f);
            int finalDamage = Mathf.RoundToInt(damage * mult);

            dmg.TakeDamage(finalDamage);
            DamagePopup.Create(col.transform.position, finalDamage, DamageType.Magic, isCritical: isCrit);

            float vamp = (playerCombat != null) ? Mathf.Max(playerCombat.SpellVampPercent, playerCombat.LifestealPercent) : 0f;
            if (vamp > 0f && playerHealth != null)
            {
                int healAmount = Mathf.Max(1, Mathf.RoundToInt(finalDamage * vamp));
                playerHealth.Heal(healAmount);
            }

            // TÍCH LŨY NỘI NĂNG KHI ĐÁNH TRÚNG QUÁI (Quái thường +8, Boss +16)
            GameObject hitTargetObj = (dmg as Component)?.gameObject ?? col.gameObject;
            var energySys = PlayerEnergySystem.Instance 
                ?? (playerCombat != null ? playerCombat.GetComponent<PlayerEnergySystem>() : null) 
                ?? Object.FindAnyObjectByType<PlayerEnergySystem>();

            if (energySys != null)
            {
                bool isBoss = CelestialMageSkill.IsBossTarget(hitTargetObj);
                float gain = isBoss ? 16f : 8f;
                energySys.AddInnerEnergy(gain);
                Debug.Log($"<color=magenta>[CELESTIAL INNER ENERGY]</color> Cầu tinh tú đánh trúng {(isBoss ? "Boss" : "quái")} {hitTargetObj.name}! +{gain} Nội Năng.");
            }

            // Kích hoạt vụ nổ ma trận Tinh Tú Astral Aegis 31 frame hoành tráng khi trúng đích
            SpawnImpactBurst(transform.position);
            Destroy(gameObject);
        }
        else if (col.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            SpawnImpactBurst(transform.position);
            Destroy(gameObject);
        }
    }

    private void SpawnImpactBurst(Vector3 pos)
    {
        if (hitVFXFrames != null && hitVFXFrames.Length > 0)
        {
            // 1. Kích hoạt vụ nổ ma trận Tinh Tú Astral Aegis 31 frame hoành tráng
            CelestialMageSkill.SpawnAnimatedVFX("AstralAegisExplosionVFX", pos, hitVFXFrames, 0.035f, scale: 2.35f, tint: new Color(0.95f, 0.6f, 1f, 1f), sortingOrder: 22);
        }

        // 2. Kích hoạt Vực Tinh Vân lưu lại trên mặt đất (Astral Residue Field)
        SpawnAstralResidueField(pos);
    }

    private void SpawnAstralResidueField(Vector3 pos)
    {
        GameObject fieldObj = new GameObject("AstralResidueField", typeof(SpriteRenderer), typeof(AstralResidueField));
        fieldObj.transform.position = pos;
        fieldObj.transform.localScale = Vector3.one * 1.85f;

        var sr = fieldObj.GetComponent<SpriteRenderer>();
        if (hitVFXFrames != null && hitVFXFrames.Length > 0)
        {
            sr.sprite = hitVFXFrames[0]; // Vệt bụi sao tím dạ quang lưu trên mặt sàn
        }
        sr.sortingOrder = 3; // Sát mặt gạch dưới chân nhân vật và quái vật
        sr.color = new Color(0.88f, 0.45f, 1f, 0.82f);

        var field = fieldObj.GetComponent<AstralResidueField>();
        field.Init(14, 3.5f, 0.5f, playerCombat, playerHealth);
    }
}

/// <summary>
/// Component tự quản lý vòng đời hoạt ảnh VFX độc lập, không bị hủy khi nguồn bắn biến mất
/// </summary>
public class AnimatedVFXPlayer : MonoBehaviour
{
    private SpriteRenderer sr;
    private Sprite[] frames;
    private float frameDuration;
    private bool loop;

    public void Play(Sprite[] f, float dur, bool l)
    {
        sr = GetComponent<SpriteRenderer>();
        frames = f;
        frameDuration = dur;
        loop = l;
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        int idx = 0;
        while (true)
        {
            if (sr != null && idx < frames.Length)
            {
                sr.sprite = frames[idx];
            }
            idx++;
            if (idx >= frames.Length)
            {
                if (loop) idx = 0;
                else break;
            }
            yield return new WaitForSeconds(frameDuration);
        }
        if (!loop)
        {
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Vực Tinh Vân / Vệt Tàn Dư Ma Pháp (Astral Residue Field)
/// Tồn tại 3.5s trên mặt đất:
/// - Làm chậm kẻ địch 50% khi bước qua.
/// - Gây sát thương thiêu đốt ma pháp liên tục mỗi 0.5s.
/// - Nhấp nháy dạ quang tím và tự động Fade Out biến mất sạch sẽ sau 3.5s.
/// </summary>
public class AstralResidueField : MonoBehaviour
{
    private float lifetime = 3.5f;
    private float dotTimer = 0f;
    private float dotInterval = 0.5f;
    private int damagePerTick = 14;
    private float slowRatio = 0.5f;
    private SpriteRenderer sr;
    private Color originalColor;
    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;

    public void Init(int damagePerTick, float lifeTime, float slowRatio, PlayerCombat pc, PlayerHealth ph)
    {
        this.damagePerTick = damagePerTick;
        lifetime = lifeTime;
        this.slowRatio = slowRatio;
        playerCombat = pc;
        playerHealth = ph;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
    }

    void Update()
    {
        lifetime -= Time.deltaTime;
        dotTimer -= Time.deltaTime;

        // Hiệu ứng nhấp nháy dạ quang tinh vân nhẹ nhàng
        if (sr != null)
        {
            float pulse = 0.72f + 0.16f * Mathf.Sin(Time.time * 6f);
            if (lifetime > 0.8f)
            {
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * pulse);
            }
            else
            {
                // Mờ dần (Fade out) ở 0.8s cuối trước khi biến mất
                float alpha = Mathf.Clamp01(lifetime / 0.8f);
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * alpha);
            }
        }

        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Kích hoạt hiệu ứng diện rộng lên kẻ địch trong vùng
        if (dotTimer <= 0f)
        {
            dotTimer = dotInterval;
            ApplyFieldEffects();
        }
    }

    private void ApplyFieldEffects()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1.15f);
        foreach (var col in colliders)
        {
            if (col == null || col.CompareTag("Player") || col.GetComponent<PlayerHealth>() != null) continue;

            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is PlayerHealth))
            {
                damageable.TakeDamage(damagePerTick);
                DamagePopup.Create(col.transform.position + new Vector3(Random.Range(-0.2f, 0.2f), 0.35f, 0f), damagePerTick, DamageType.Magic, isCritical: false);

                if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null)
                {
                    int heal = Mathf.Max(1, Mathf.RoundToInt(damagePerTick * playerCombat.LifestealPercent));
                    playerHealth.Heal(heal);
                }

                // TÍCH LŨY NỘI NĂNG KHI THIÊU ĐỐT TRÚNG QUÁI (Quái thường +1.0f, Boss +2.5f)
                GameObject hitTargetObj = (damageable as Component)?.gameObject ?? col.gameObject;
                var energySys = PlayerEnergySystem.Instance 
                    ?? (playerCombat != null ? playerCombat.GetComponent<PlayerEnergySystem>() : null) 
                    ?? Object.FindAnyObjectByType<PlayerEnergySystem>();

                if (energySys != null)
                {
                    bool isBoss = CelestialMageSkill.IsBossTarget(hitTargetObj);
                    float gain = isBoss ? 2.5f : 1.0f;
                    energySys.AddInnerEnergy(gain);
                }
            }

            // Gắn debuff làm chậm 50% cho quái
            var enemyObj = col.gameObject;
            var debuff = enemyObj.GetComponent<EnemySlowDebuff>() ?? enemyObj.AddComponent<EnemySlowDebuff>();
            debuff.ApplySlow(slowRatio, 0.75f);
        }
    }
}

/// <summary>
/// Debuff làm chậm tốc độ di chuyển của kẻ địch kèm hiệu ứng nhuộm tím ma pháp
/// </summary>
public class EnemySlowDebuff : MonoBehaviour
{
    private float slowTimer = 0f;
    private float slowFactor = 0.5f;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Color origColor;
    private bool hasColor = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            origColor = sr.color;
            hasColor = true;
        }
    }

    public void ApplySlow(float factor, float duration)
    {
        slowFactor = factor;
        slowTimer = Mathf.Max(slowTimer, duration);

        if (sr != null && hasColor)
        {
            sr.color = new Color(0.85f, 0.5f, 1f, 1f); // Nhuộm tím tinh vân
        }
    }

    void Update()
    {
        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (rb != null && rb.bodyType != RigidbodyType2D.Static)
            {
                rb.linearVelocity *= slowFactor;
            }

            if (slowTimer <= 0f)
            {
                if (sr != null && hasColor) sr.color = origColor;
                Destroy(this);
            }
        }
    }

    void OnDisable()
    {
        if (sr != null && hasColor) sr.color = origColor;
    }
}
