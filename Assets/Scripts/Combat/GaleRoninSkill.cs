using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GaleRoninSkill : MonoBehaviour
{
    [Header("Gale Ronin Attack Settings")]
    [SerializeField] private float slashRadius = 1.30f; // Tăng tầm đánh xa hơn một chút
    [SerializeField] private float slashAngle = 180f; // Góc chém bán nguyệt 180 độ
    [SerializeField] private int baseSlashDamage = 28; // Sát thương uy lực 28 chuẩn 1:1 cho đòn đánh chậm Iaido
    [SerializeField] private float attackCooldown = 1.20f; // Tốc độ chém chậm hơn rõ rệt (1.2s), đầm tay chuẩn Iaido Samurai
    [SerializeField] private LayerMask targetLayers;

    public void SetBaseSlashDamage(int dmg) => baseSlashDamage = dmg;

    [Header("Passive: Wind Mark (Phong Ấn)")]
    [SerializeField] private int maxMarkStacks = 3;
    [SerializeField] private float burstDamageMultiplier = 2.4f;
    [SerializeField] private float vortexDuration = 1.3f;
    [SerializeField] private float vortexRadius = 1.6f; // Thu gọn bán kính lốc xoáy cho vừa vặn, chuẩn xác

    [Header("Special Skill: Bão Phong Trảm (Phantom Cleave)")]
    [SerializeField] private float specialCooldown = 15.0f;
    [SerializeField] private int phantomSlashCount = 4;
    [SerializeField] private int basePhantomDamage = 26;

    [Header("Iaido Charge Settings")]
    [SerializeField] private float minChargeDuration = 0.12f;
    [SerializeField] private float maxChargeDuration = 0.65f;
    [SerializeField] private float maxChargeMultiplier = 2.85f;

    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float attackCooldownTimer = 0f;
    private float specialCooldownTimer = 0f;
    private bool isPhantomActive = false;

    // Iaido Charging state
    private bool isChargingIaido = false;
    private float currentChargeDuration = 0f;
    private Vector2 currentAimDir = Vector2.down;
    private bool hasPlayedMaxGlint = false;
    private float nextParticleTime = 0f;
    private float nextGhostTime = 0f;
    private GameObject chargeAuraObj = null;

    // Quản lý tầng Phong Ấn trên từng mục tiêu
    private Dictionary<GameObject, int> windMarks = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, float> markExpiryTimes = new Dictionary<GameObject, float>();

    [Header("--- TÀN ẢNH TỐC BIẾN (SHADOW PHANTOM WARP - MURAD STYLE) ---")]
    [SerializeField] private float phantomTeleportDistance = 4.8f;
    [SerializeField] private float shadowCloneDuration = 4.5f;
    private bool hasActiveShadowClone = false;
    private Vector2 shadowClonePosition = Vector2.zero;
    private GameObject activeShadowCloneObj = null;
    private float shadowCloneTimer = 0f;

    public bool HasActiveShadowClone => hasActiveShadowClone && activeShadowCloneObj != null;
    public float ShadowCloneTimeRemaining => Mathf.Max(0f, shadowCloneTimer);
    public float ShadowCloneRatio => (shadowCloneDuration > 0f) ? Mathf.Clamp01(shadowCloneTimer / shadowCloneDuration) : 0f;

    public float SlashRadius => slashRadius;
    public bool CanUseIaidoCharge
    {
        get
        {
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            return energySys != null && energySys.IsShieldActive();
        }
    }

    public bool IsAttackReady => attackCooldownTimer <= 0f;
    public float AttackCooldownRatio => Mathf.Clamp01(attackCooldownTimer / attackCooldown);
    public float AttackCooldownRemaining => Mathf.Max(0f, attackCooldownTimer);
    public bool IsSpecialReady => specialCooldownTimer <= 0f;
    public float SpecialCooldown => specialCooldown;
    public float SpecialCooldownRatio => Mathf.Clamp01(specialCooldownTimer / specialCooldown);
    public float SpecialCooldownRemaining => Mathf.Max(0f, specialCooldownTimer);
    public bool IsChargingIaido => isChargingIaido;
    public float CurrentChargeRatio => Mathf.Clamp01(currentChargeDuration / maxChargeDuration);

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
        playerHealth = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (targetLayers.value == 0)
        {
            targetLayers = ~0; // Target everything damageable
        }
        RefreshCooldownStats();
    }

    void Start()
    {
        RefreshCooldownStats();
    }

    void OnEnable()
    {
        RefreshCooldownStats();
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

    void Update()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (specialCooldownTimer > 0f)
        {
            specialCooldownTimer -= Time.deltaTime;
        }

        // Đếm ngược thời gian tồn tại của Tàn Ảnh Bóng Ma (Murad S1)
        if (hasActiveShadowClone)
        {
            shadowCloneTimer -= Time.deltaTime;
            if (shadowCloneTimer <= 0f)
            {
                CancelShadowClone();
                if (playerController != null)
                {
                    playerController.TriggerDashCooldown();
                }
            }
        }

        // Dọn dẹp các mục tiêu đã hết hạn dấu ấn (sau 6s không bị đánh)
        CleanupExpiredMarks();

        // PC Key test
        if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            TriggerSpecialSkill();
        }
    }

    private void CleanupExpiredMarks()
    {
        if (windMarks.Count == 0) return;

        List<GameObject> toRemove = new List<GameObject>();
        float now = Time.time;
        foreach (var kvp in markExpiryTimes)
        {
            if (kvp.Key == null || now > kvp.Value)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var k in toRemove)
        {
            windMarks.Remove(k);
            markExpiryTimes.Remove(k);
        }
    }

    /// <summary>
    /// Thực hiện đòn đánh thường: Nhát chém bán nguyệt quét 180 độ
    /// </summary>
    public bool PerformAttack(Vector2 facingDir, int bonusAtkDamage)
    {
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (!IsAttackReady || isPhantomActive) return false;

        attackCooldownTimer = attackCooldown;

        if (facingDir.sqrMagnitude < 0.001f) facingDir = Vector2.down;
        facingDir = facingDir.normalized;

        // Hoạt ảnh chém
        if (animator != null)
        {
            animator.SetFloat("MoveX", facingDir.x);
            animator.SetFloat("MoveY", facingDir.y);
            animator.SetTrigger("AttackTrigger");
        }

        SoundManager.EnsureInstance()?.PlaySwordSlash();

        // Quét hình quạt 180 độ vừa vặn với hoạt ảnh chém với tầm vươn xa hơn
        Vector2 origin = (Vector2)transform.position + facingDir * 0.55f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, slashRadius, targetLayers);

        int totalBaseDamage = baseSlashDamage + bonusAtkDamage;
        HashSet<GameObject> hitRoots = new HashSet<GameObject>();
        int hitEnemyCount = 0;
        foreach (var col in colliders)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable is PlayerHealth) continue;

            GameObject rootObj = (damageable as Component)?.gameObject ?? col.gameObject;
            if (hitRoots.Contains(rootObj)) continue;
            hitRoots.Add(rootObj);

            // Kiểm tra góc nón 180 độ (Dot product >= -0.15f)
            Vector2 toTarget = ((Vector2)col.transform.position - origin).normalized;
            float dot = Vector2.Dot(facingDir, toTarget);

            if (dot >= -0.15f || Vector2.Distance(origin, col.transform.position) <= slashRadius * 0.55f)
            {
                HitTargetWithWindMark(rootObj, damageable, totalBaseDamage, facingDir);
                bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
                if (!isProp)
                {
                    hitEnemyCount++;
                }
            }
        }

        // Hồi năng lượng & Tích lũy 50% Tiến Trình Nội Năng + Giảm 20% Cooldown Tuyệt Kỹ CHỈ KHI chém trúng quái vật thực sự!
        if (hitEnemyCount > 0)
        {
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (energySys != null)
            {
                energySys.RestoreEnergy(15f);
                energySys.AddInnerEnergy(50f); // +50% tiến trình Nội Năng (2 đòn đánh trúng = 100% nộ)
            }

            // Giảm 20% Thời gian hồi Chiêu Cuối (Ultimate CD)
            float cdReduction = specialCooldown * 0.20f;
            if (specialCooldownTimer > 0f)
            {
                specialCooldownTimer = Mathf.Max(0f, specialCooldownTimer - cdReduction);
                }
            else
            {
                }

            if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null)
            {
                int healAmount = Mathf.Max(1, Mathf.RoundToInt(totalBaseDamage * playerCombat.LifestealPercent));
                playerHealth.Heal(healAmount);
            }
        }

        return true;
    }

    // =========================================================================
    // HỆ THỐNG TỤ LỰC IAIDO SIÊU NGẦU (ANIME CHARGED IAIDO SYSTEM)
    // =========================================================================

    public void StartIaidoCharge(Vector2 initialAim)
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (!IsAttackReady || isPhantomActive || !CanUseIaidoCharge) return;
        isChargingIaido = true;
        currentChargeDuration = 0f;
        hasPlayedMaxGlint = false;
        currentAimDir = (initialAim.sqrMagnitude > 0.001f) ? initialAim.normalized : Vector2.down;

        if (playerController != null)
        {
            playerController.SetFacingDirection(currentAimDir);
        }

        CreateChargeAura();
        SoundManager.EnsureInstance()?.PlaySwordSlash();
    }

    public void UpdateIaidoCharge(Vector2 aimDir, float holdDuration)
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            CancelIaidoCharge();
            return;
        }

        if (!CanUseIaidoCharge)
        {
            if (isChargingIaido) CancelIaidoCharge();
            return;
        }

        if (!isChargingIaido && IsAttackReady)
        {
            StartIaidoCharge(aimDir);
        }

        if (!isChargingIaido) return;

        currentChargeDuration = holdDuration;
        if (aimDir.sqrMagnitude > 0.001f)
        {
            currentAimDir = aimDir.normalized;
            if (playerController != null) playerController.SetFacingDirection(currentAimDir);
        }

        float ratio = Mathf.Clamp01(currentChargeDuration / maxChargeDuration);

        // Biến đổi vùng ngắm linh hoạt:
        // - Trạng thái đánh thường / mới chạm (holdDuration < 0.18s): Hiện vùng quạt chém thường 180°
        // - Trạng thái gồng tụ lực (holdDuration >= 0.18s): Biến đổi thành ĐƯỜNG THẲNG định hướng kiếm khí bắn xa 7.5m
        Color aimColor = Color.Lerp(new Color(0.15f, 0.95f, 0.85f, 0.85f), new Color(1f, 0.95f, 0.35f, 0.95f), ratio);
        if (currentChargeDuration < 0.18f)
        {
            SkillAimIndicator.EnsureExists(gameObject)?.ShowAimArc((Vector2)transform.position, currentAimDir, slashRadius, 180f, aimColor);
        }
        else
        {
            float lineLength = Mathf.Lerp(4.5f, 7.5f, ratio);
            float lineWidth = Mathf.Lerp(0.14f, 0.26f, ratio);
            SkillAimIndicator.EnsureExists(gameObject)?.ShowAimLine((Vector2)transform.position, currentAimDir, lineLength, aimColor, lineWidth);
        }

        // Cập nhật hiệu ứng hào quang cuộn xoáy dưới chân
        UpdateChargeAura(ratio);

        // Sinh hạt kiếm khí phong lôi xoáy tụ vào thân kiếm
        if (Time.time >= nextParticleTime)
        {
            nextParticleTime = Time.time + 0.045f;
            SpawnInwardWindParticle(transform.position, currentAimDir, ratio);
        }

        // Sinh bóng mờ phân thân rung giật chuẩn tốc độ cao
        if (Time.time >= nextGhostTime)
        {
            nextGhostTime = Time.time + 0.09f;
            Vector3 jitter = (Vector3)(Random.insideUnitCircle * Mathf.Lerp(0.03f, 0.09f, ratio));
            Color ghostCol = (ratio >= 0.9f) 
                ? new Color(0.95f, 1f, 0.5f, 0.65f) 
                : new Color(0.1f, 0.85f, 1f, 0.55f);
            SpawnShadowClone(transform.position + jitter, ghostCol);
        }

        // Rung nhẹ camera khi tích lực cao
        if (ratio >= 0.75f && Random.value > 0.65f)
        {
            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.04f, 0.06f);
        }

        // KHI ĐẠT 100% TỤ LỰC (MAX CHARGE): Lóe sáng chói lòa + âm thanh KENG!
        if (ratio >= 1.0f && !hasPlayedMaxGlint)
        {
            hasPlayedMaxGlint = true;
            SpawnSheathStarGlint(transform.position + (Vector3)(currentAimDir * 0.45f) + Vector3.up * 0.45f);
            SoundManager.EnsureInstance()?.PlayHitImpact();

            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.14f, 0.22f);

            // Chớp sáng nhân vật
            StartCoroutine(FlashSpriteRoutine(new Color(1f, 1f, 1f, 1f), 0.12f));
        }
    }

    public void CancelIaidoCharge()
    {
        isChargingIaido = false;
        currentChargeDuration = 0f;
        hasPlayedMaxGlint = false;
        DestroyChargeAura();
        SkillAimIndicator.Instance?.Hide();
    }

    public void ReleaseIaidoCharge(Vector2 aimDir, int bonusAtkDamage)
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            CancelIaidoCharge();
            return;
        }

        if (SkillAimIndicator.Instance != null) SkillAimIndicator.Instance.Hide();
        DestroyChargeAura();

        if (aimDir.sqrMagnitude > 0.001f) currentAimDir = aimDir.normalized;

        float holdTime = currentChargeDuration;
        bool wasCharging = isChargingIaido;
        isChargingIaido = false;
        currentChargeDuration = 0f;
        hasPlayedMaxGlint = false;

        if (!CanUseIaidoCharge || !wasCharging || holdTime < minChargeDuration)
        {
            PerformAttack(currentAimDir, bonusAtkDamage);
            return;
        }

        ExecuteChargedIaidoSlash(currentAimDir, holdTime, bonusAtkDamage);
        if (playerController != null) playerController.RefreshMoveInput();
    }

    private void ExecuteChargedIaidoSlash(Vector2 facingDir, float chargeTime, int bonusAtkDamage)
    {
        if (!IsAttackReady || isPhantomActive) return;

        attackCooldownTimer = attackCooldown;
        float ratio = Mathf.Clamp01(chargeTime / maxChargeDuration);
        bool isMaxCharge = ratio >= 0.90f;

        if (facingDir.sqrMagnitude < 0.001f) facingDir = Vector2.down;
        facingDir = facingDir.normalized;

        if (playerController != null) playerController.SetFacingDirection(facingDir);

        if (animator != null)
        {
            animator.SetFloat("MoveX", facingDir.x);
            animator.SetFloat("MoveY", facingDir.y);
            animator.SetTrigger("AttackTrigger");
            animator.Play("Attack2", 0, 0f);
        }

        SoundManager.EnsureInstance()?.PlaySwordSlash();

        // Rung màn hình uy lực
        float shakeAmt = Mathf.Lerp(0.18f, 0.45f, ratio);
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(shakeAmt, 0.25f);

        // Vết chém không gian rực rỡ (Spatial Slash Rift)
        SpawnSpatialSlashRift(transform.position + (Vector3)(facingDir * 0.6f), facingDir, Mathf.Lerp(1.5f, 2.6f, ratio));

        // Tính sát thương
        float dmgMult = Mathf.Lerp(1.4f, maxChargeMultiplier, ratio);
        int totalDamage = Mathf.RoundToInt((baseSlashDamage + bonusAtkDamage) * dmgMult);
        float currentRadius = Mathf.Lerp(slashRadius, slashRadius * 1.85f, ratio);

        Vector2 origin = (Vector2)transform.position + facingDir * 0.6f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, currentRadius, targetLayers);

        HashSet<GameObject> hitRoots = new HashSet<GameObject>();
        int hitEnemyCount = 0;

        foreach (var col in colliders)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable is PlayerHealth) continue;

            GameObject rootObj = (damageable as Component)?.gameObject ?? col.gameObject;
            if (hitRoots.Contains(rootObj)) continue;
            hitRoots.Add(rootObj);

            Vector2 toTarget = ((Vector2)col.transform.position - origin).normalized;
            float dot = Vector2.Dot(facingDir, toTarget);

            if (dot >= -0.25f || Vector2.Distance(origin, col.transform.position) <= currentRadius * 0.65f)
            {
                if (isMaxCharge)
                {
                    // Max Charge: Kích nổ ngay lập tức 3 tầng Phong Ấn!
                    windMarks[rootObj] = 0;
                    int burstDmg = Mathf.RoundToInt(totalDamage * burstDamageMultiplier);
                    damageable.TakeDamage(burstDmg);
                    DamagePopup.Create(col.transform.position, burstDmg, DamageType.Magic, isCritical: true);
                    SpawnMiniGaleVortex(col.transform.position, Mathf.RoundToInt(totalDamage * 0.7f));
                }
                else
                {
                    HitTargetWithWindMark(rootObj, damageable, totalDamage, facingDir);
                }

                bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
                if (!isProp) hitEnemyCount++;
            }
        }

        // Khi tụ lực >= 35% VÀ đang trong trạng thái Khiên Nội Năng: Phóng thêm Nguyệt Kiếm Khí (Gale Crescent Wave) sắc bén xuyên thấu phía trước!
        if (ratio >= 0.35f && CanUseIaidoCharge)
        {
            Vector3 waveSpawn = transform.position + (Vector3)(facingDir * 0.45f) + Vector3.up * 0.15f;
            int waveDmg = Mathf.RoundToInt(totalDamage * (isMaxCharge ? 0.9f : 0.65f));
            float waveScale = Mathf.Lerp(0.65f, 0.95f, ratio);
            GaleCrescentWave.Spawn(waveSpawn, facingDir, waveDmg, speed: 14.5f, scale: waveScale, owner: this);
        }

        // Hiệu ứng khựng hình Hit-Stop 0.05s chuẩn Anime khi chém trúng quái ở Max Charge!
        if (hitEnemyCount > 0 && isMaxCharge)
        {
            StartCoroutine(HitStopRoutine(0.06f));
        }

        // Hồi năng lượng & Tích lũy Nộ cực lớn
        if (hitEnemyCount > 0)
        {
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (energySys != null)
            {
                energySys.RestoreEnergy(isMaxCharge ? 25f : 18f);
                energySys.AddInnerEnergy(isMaxCharge ? 65f : 50f);
            }

            float cdReduction = specialCooldown * (isMaxCharge ? 0.35f : 0.20f);
            if (specialCooldownTimer > 0f)
            {
                specialCooldownTimer = Mathf.Max(0f, specialCooldownTimer - cdReduction);
            }

            if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null)
            {
                int healAmount = Mathf.Max(1, Mathf.RoundToInt(totalDamage * playerCombat.LifestealPercent));
                playerHealth.Heal(healAmount);
            }
        }
    }

    public void OnCrescentWaveHit(GameObject enemyObj, IDamageable damageable, int damage)
    {
        if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null)
        {
            int healAmount = Mathf.Max(1, Mathf.RoundToInt(damage * playerCombat.LifestealPercent));
            playerHealth.Heal(healAmount);
        }

        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null)
        {
            energySys.RestoreEnergy(6f);
            energySys.AddInnerEnergy(15f);
        }
    }

    // =========================================================================
    // CÁC HIỆU ỨNG VFX TỤ LỰC & XUẤT CHIÊU ĐẬM CHẤT ANIME
    // =========================================================================

    private void CreateChargeAura()
    {
        if (chargeAuraObj != null) Destroy(chargeAuraObj);

        chargeAuraObj = new GameObject("Ronin_ChargeAura");
        chargeAuraObj.transform.SetParent(transform, false);
        chargeAuraObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);

        var light = chargeAuraObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = new Color(0.1f, 0.95f, 0.85f, 1f);
        light.intensity = 1.4f;
        light.pointLightInnerRadius = 0.3f;
        light.pointLightOuterRadius = 1.6f;
    }

    private void UpdateChargeAura(float ratio)
    {
        if (chargeAuraObj == null) return;
        var light = chargeAuraObj.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
        if (light != null)
        {
            light.intensity = Mathf.Lerp(1.4f, 3.2f, ratio);
            light.pointLightOuterRadius = Mathf.Lerp(1.6f, 2.5f, ratio);
            light.color = Color.Lerp(new Color(0.1f, 0.95f, 0.85f, 1f), new Color(1f, 0.95f, 0.35f, 1f), ratio);
        }
    }

    private void DestroyChargeAura()
    {
        if (chargeAuraObj != null)
        {
            Destroy(chargeAuraObj);
            chargeAuraObj = null;
        }
    }

    private void SpawnInwardWindParticle(Vector3 center, Vector2 facingDir, float ratio)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(1.2f, 1.8f);
        Vector3 spawnPos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

        GameObject p = new GameObject("InwardWindParticle");
        p.transform.position = spawnPos;
        p.transform.localScale = Vector3.one * Random.Range(0.12f, 0.24f);

        var sr = p.AddComponent<SpriteRenderer>();
        sr.sprite = GetStarGlintSprite();
        sr.color = (ratio >= 0.8f) ? new Color(1f, 0.95f, 0.4f, 0.9f) : new Color(0.2f, 1f, 0.85f, 0.85f);
        sr.sortingOrder = 3200;

        var rb = p.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        Vector2 target = (Vector2)center + facingDir * 0.35f;
        Vector2 flyDir = (target - (Vector2)spawnPos).normalized;
        rb.linearVelocity = flyDir * Random.Range(6.5f, 11f);

        Destroy(p, 0.18f);
    }

    private void SpawnSheathStarGlint(Vector3 glintPos)
    {
        GameObject star = new GameObject("IaidoStarGlint");
        star.transform.position = glintPos;
        star.transform.localScale = Vector3.zero;

        var sr = star.AddComponent<SpriteRenderer>();
        sr.sprite = GetStarGlintSprite();
        sr.color = new Color(1f, 1f, 1f, 1f);
        sr.sortingOrder = 4500;

        var light = star.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = new Color(1f, 0.95f, 0.5f, 1f);
        light.intensity = 3.5f;
        light.pointLightInnerRadius = 0.3f;
        light.pointLightOuterRadius = 2.0f;

        StartCoroutine(AnimateStarGlintRoutine(star, light));
    }

    private IEnumerator AnimateStarGlintRoutine(GameObject starObj, UnityEngine.Rendering.Universal.Light2D light)
    {
        float dur = 0.22f;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            float scale = Mathf.Sin(t * Mathf.PI) * 1.6f;
            if (starObj != null) starObj.transform.localScale = Vector3.one * scale;
            if (light != null) light.intensity = (1f - t) * 3.5f;
            yield return null;
        }

        if (starObj != null) Destroy(starObj);
    }

    private void SpawnSpatialSlashRift(Vector3 center, Vector2 direction, float length)
    {
        GameObject rift = new GameObject("SpatialSlashRift");
        rift.transform.position = center;
        float rotZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rift.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        rift.transform.localScale = new Vector3(length, length * 0.4f, 1f);

        var sr = rift.AddComponent<SpriteRenderer>();
        sr.sprite = GetStarGlintSprite();
        sr.color = new Color(1f, 1f, 1f, 0.95f);
        sr.sortingOrder = 4000;

        StartCoroutine(FadeRiftRoutine(rift, sr));
    }

    private IEnumerator FadeRiftRoutine(GameObject riftObj, SpriteRenderer sr)
    {
        float dur = 0.20f;
        float elapsed = 0f;
        Color startCol = new Color(0.2f, 1f, 0.9f, 0.95f);

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            if (sr != null)
            {
                sr.color = new Color(startCol.r, startCol.g, startCol.b, 1f - t);
                if (riftObj != null) riftObj.transform.localScale += new Vector3(Time.deltaTime * 1.5f, -Time.deltaTime * 0.5f, 0f);
            }
            yield return null;
        }

        if (riftObj != null) Destroy(riftObj);
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
    }

    private IEnumerator FlashSpriteRoutine(Color flashColor, float duration)
    {
        if (spriteRenderer == null) yield break;
        Color orig = spriteRenderer.color;
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(duration);
        if (spriteRenderer != null) spriteRenderer.color = orig;
    }

    private static Sprite cachedStarGlintSprite = null;
    private static Sprite GetStarGlintSprite()
    {
        if (cachedStarGlintSprite != null) return cachedStarGlintSprite;

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = (size - 1) / 2.0f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center) / center;
                float dy = Mathf.Abs(y - center) / center;

                // 4-point star formula: 1 - (dx^0.5 + dy^0.5)
                float val = 1f - (Mathf.Pow(dx, 0.45f) + Mathf.Pow(dy, 0.45f));
                if (val > 0f)
                {
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(val * 1.6f)));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        cachedStarGlintSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        return cachedStarGlintSprite;
    }

    private void HitTargetWithWindMark(GameObject targetRoot, IDamageable damageable, int damage, Vector2 facingDir)
    {
        // 1. Tích tầng Phong Ấn
        int currentStacks = 0;
        windMarks.TryGetValue(targetRoot, out currentStacks);
        currentStacks++;
        markExpiryTimes[targetRoot] = Time.time + 6.0f;

        SoundManager.EnsureInstance()?.PlayHitImpact();

        // 2. Nếu đạt 3 tầng: PHONG ẤN BỘC PHÁ!
        if (currentStacks >= maxMarkStacks)
        {
            windMarks[targetRoot] = 0;
            int burstDamage = Mathf.RoundToInt(damage * burstDamageMultiplier);
            damageable.TakeDamage(burstDamage);

            Vector3 targetPos = targetRoot.transform.position;
            DamagePopup.Create(targetPos, burstDamage, DamageType.Magic, isCritical: true);
            // Triệu hồi Lốc Xoáy Phong Lôi (Mini Gale Vortex)
            SpawnMiniGaleVortex(targetPos, Mathf.RoundToInt(damage * 0.6f));
        }
        else
        {
            windMarks[targetRoot] = currentStacks;
            damageable.TakeDamage(damage);
            DamagePopup.Create(targetRoot.transform.position, damage, DamageType.Physical, isCritical: false);

            string markText = (currentStacks == 1) 
                ? LocalizationManager.T("🌀 Ấn I", "🌀 Mark I") 
                : LocalizationManager.T("🌀🌀 Ấn II", "🌀🌀 Mark II");
            }

        // Hơi hất lùi quái vật
        var rb = targetRoot.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(facingDir * 4.5f, ForceMode2D.Impulse);
        }
    }

    /// <summary>
    /// Triệu hồi Lốc Xoáy Mini giữ chân và gây sát thương liên tục
    /// </summary>
    private void SpawnMiniGaleVortex(Vector3 centerPos, int tickDamage)
    {
        GameObject vortex = new GameObject("MiniGaleVortex");
        vortex.transform.position = centerPos;
        StartCoroutine(GaleVortexRoutine(vortex, centerPos, tickDamage));
    }

    private IEnumerator GaleVortexRoutine(GameObject vortex, Vector3 center, int tickDamage)
    {
        float elapsed = 0f;
        float tickTimer = 0f;

        while (elapsed < vortexDuration)
        {
            elapsed += Time.deltaTime;
            tickTimer += Time.deltaTime;

            // Hút các quái vật xung quanh vào tâm
            Collider2D[] cols = Physics2D.OverlapCircleAll(center, vortexRadius, targetLayers);
            foreach (var c in cols)
            {
                if (c == null || c.gameObject == gameObject) continue;
                var dmg = c.GetComponent<IDamageable>();
                if (dmg != null && c.attachedRigidbody != null)
                {
                    Vector2 pullDir = ((Vector2)center - (Vector2)c.transform.position).normalized;
                    c.attachedRigidbody.AddForce(pullDir * 6.5f, ForceMode2D.Force);
                }
            }

            // Gây sát thương mỗi 0.35s
            if (tickTimer >= 0.35f)
            {
                tickTimer = 0f;
                foreach (var c in cols)
                {
                    if (c == null || c.gameObject == gameObject) continue;
                    var dmg = c.GetComponent<IDamageable>();
                    if (dmg != null)
                    {
                        dmg.TakeDamage(tickDamage);
                        DamagePopup.Create(c.transform.position, tickDamage, DamageType.Magic, isCritical: false);
                    }
                }
            }

            yield return null;
        }

        if (vortex != null) Destroy(vortex);
    }

    /// <summary>
    /// Tìm mục tiêu gần nhất hoặc Boss trong bán kính quét
    /// </summary>
    public Transform FindBestTarget(float searchRadius, out IDamageable outDamageable)
    {
        outDamageable = null;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius, targetLayers);
        Transform targetTransform = null;
        float closestDist = float.MaxValue;
        bool isBossTarget = false;

        foreach (var col in colliders)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (dmg == null || dmg is PlayerHealth) continue;

            bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
            if (isProp) continue;

            // Lấy chính xác Transform của Enemy (không lấy root vì root có thể là dungeon container)
            Transform enemyT = (dmg as Component)?.transform ?? col.transform;
            float dist = Vector2.Distance(transform.position, enemyT.position);

            bool isBoss = enemyT.GetComponent<ValleyGolemBossAI>() != null || enemyT.GetComponentInParent<ValleyGolemBossAI>() != null || 
                          enemyT.GetComponent<SkeletonKingBossAI>() != null || enemyT.GetComponentInParent<SkeletonKingBossAI>() != null || 
                          enemyT.name.Contains("Boss");
            if (isBoss)
            {
                if (!isBossTarget || dist < closestDist)
                {
                    isBossTarget = true;
                    closestDist = dist;
                    targetTransform = enemyT;
                    outDamageable = dmg;
                }
            }
            else if (!isBossTarget && dist < closestDist)
            {
                closestDist = dist;
                targetTransform = enemyT;
                outDamageable = dmg;
            }
        }

        return targetTransform;
    }

    /// <summary>
    /// Tuyệt Kỹ: Ám Ảnh Trảm (Shadow Execution Strike)
    /// Dịch chuyển ra sau lưng kẻ địch, tung đòn chém chí mạng rồi biến về vị trí ban đầu.
    /// </summary>
    public bool TriggerSpecialSkill()
    {
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (specialCooldownTimer > 0f || isPhantomActive)
        {
            return false;
        }

        Transform targetT = FindBestTarget(3.5f, out var targetDmg); // Thu gọn tầm khóa mục tiêu về 3.5m (khoảng 3 ô gạch sàn)
        if (targetT == null || targetDmg == null)
        {
            return false;
        }

        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null && !energySys.TryConsumeEnergy(40f))
        {
            return false;
        }

        RefreshCooldownStats();
        specialCooldownTimer = specialCooldown;
        StartCoroutine(PhantomCleaveRoutine(targetT, targetDmg));
        return true;
    }

    private IEnumerator PhantomCleaveRoutine(Transform targetTransform, IDamageable targetDamageable)
    {
        if (targetTransform == null || targetDamageable == null)
        {
            yield break;
        }

        isPhantomActive = true;

        if (playerHealth != null) playerHealth.isInvulnerable = true;
        if (playerController != null) playerController.SetCanMove(false);

        Vector3 startPos = transform.position;
        Vector3 targetPos = targetTransform.position;
        Vector2 dirToTarget = ((Vector2)targetPos - (Vector2)startPos).normalized;
        if (dirToTarget.sqrMagnitude < 0.001f) dirToTarget = Vector2.down;

        // Vị trí sau lưng địch (cách tâm quái 1.15m về phía đối diện)
        Vector3 behindPos = targetPos + (Vector3)(dirToTarget * 1.15f);

        var cam = FindAnyObjectByType<CameraFollow>();

        // Ảo ảnh để lại tại vị trí ban đầu
        SpawnShadowClone(startPos, new Color(0.2f, 0.7f, 1f, 0.75f));
        SoundManager.EnsureInstance()?.PlaySwordSlash();

        // 2. BƯỚC 1: DỊCH CHUYỂN RA SAU LƯNG ĐỊCH
        transform.position = behindPos;

        // Quay mặt vào lưng địch
        Vector2 strikeFacing = -dirToTarget;
        if (animator != null)
        {
            animator.SetFloat("MoveX", strikeFacing.x);
            animator.SetFloat("MoveY", strikeFacing.y);
            animator.Play("Attack1", 0, 0f); // Chuẩn bị tư thế rút kiếm áp sát
        }

        if (cam != null) cam.Shake(0.12f, 0.18f);

        // DỪNG LẠI 0.35s ĐỂ NGƯỜI CHƠI NHÌN THẤY RÕ RÀNG HÀNH ĐỘNG XUẤT HIỆN SAU LƯNG
        yield return new WaitForSeconds(0.35f);

        // 3. BƯỚC 2: TUNG ĐÒN CHÉM KẾT LIỄU CHÍ MẠNG (SHADOW EXECUTION SLICE)
        if (animator != null)
        {
            animator.SetTrigger("AttackTrigger");
            animator.Play("Attack2", 0, 0f);
        }

        SoundManager.EnsureInstance()?.PlaySwordSlash();

        // Đợi 0.12s để hoạt ảnh vung kiếm chém tới điểm va chạm
        yield return new WaitForSeconds(0.12f);

        // Tính sát thương tuyệt kỹ (x3 sát thương cơ bản + bonus nâng cấp)
        int upUltBonus = 0;
        var upMgr = PlayerUpgradeManager.EnsureInstance();
        if (upMgr != null) upUltBonus = upMgr.GetUltiDamageBonus();

        int totalStrikeDmg = (basePhantomDamage * 3) + (playerCombat != null ? playerCombat.BonusAttackDamage * 2 : 0) + upUltBonus;

        bool targetWasAliveBefore = (targetDamageable != null && targetTransform != null);
        if (targetDamageable != null && targetTransform != null)
        {
            targetDamageable.TakeDamage(totalStrikeDmg);
            DamagePopup.Create(targetTransform.position, totalStrikeDmg, DamageType.TrueDamage, isCritical: true);
            var targetRb = targetTransform.GetComponent<Rigidbody2D>();
            if (targetRb != null)
            {
                targetRb.AddForce(dirToTarget * 8.5f, ForceMode2D.Impulse);
            }
        }

        // Gây sát thương quét phụ (Splash Damage) gọn gàng cho quái vật đứng ngay cạnh mục tiêu chính
        Collider2D[] splashHits = Physics2D.OverlapCircleAll(targetPos, 1.0f, targetLayers);
        foreach (var sh in splashHits)
        {
            if (sh == null || sh.gameObject == gameObject || (targetTransform != null && sh.transform.IsChildOf(targetTransform))) continue;
            var dmg = sh.GetComponent<IDamageable>() ?? sh.GetComponentInParent<IDamageable>();
            if (dmg != null && !(dmg is PlayerHealth))
            {
                bool isProp = (sh.GetComponent<BreakableProp>() != null || sh.GetComponentInParent<BreakableProp>() != null);
                if (!isProp)
                {
                    int splashDmg = Mathf.RoundToInt(totalStrikeDmg * 0.5f);
                    dmg.TakeDamage(splashDmg);
                    DamagePopup.Create(sh.transform.position, splashDmg, DamageType.Physical, isCritical: false);
                }
            }
        }

        // KIỂM TRA NẾU KẾT LIỄU ĐƯỢC MỤC TIÊU: HỒI NGAY CHIÊU CUỐI!
        if (targetWasAliveBefore && IsTargetKilled(targetTransform, targetDamageable))
        {
            specialCooldownTimer = 0f;
            SoundManager.EnsureInstance()?.PlayPortalSpawn();
            }

        SoundManager.EnsureInstance()?.PlayHitImpact();
        if (cam != null) cam.Shake(0.35f, 0.35f);

        // Hồi năng lượng & Tích lũy Nội Năng lớn cho Player
        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null)
        {
            energySys.RestoreEnergy(15f);
            energySys.AddInnerEnergy(30f);
        }

        if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null)
        {
            int healAmount = Mathf.Max(1, Mathf.RoundToInt(totalStrikeDmg * playerCombat.LifestealPercent));
            playerHealth.Heal(healAmount);
        }

        // DỪNG LẠI 0.45s ĐỂ NGƯỜI CHƠI NHÌN THẤY HOẠT ẢNH CHÉM VÀ SÁT THƯƠNG NẢY LÊN
        yield return new WaitForSeconds(0.45f);

        // 4. BƯỚC 3: DỊCH CHUYỂN TRỞ VỀ VỊ TRÍ CŨ
        SpawnShadowClone(behindPos, new Color(0.1f, 0.4f, 0.8f, 0.75f));
        transform.position = startPos;

        if (animator != null)
        {
            animator.SetFloat("MoveX", dirToTarget.x);
            animator.SetFloat("MoveY", dirToTarget.y);
        }

        SoundManager.EnsureInstance()?.PlaySwordSlash();

        yield return new WaitForSeconds(0.18f);

        if (playerHealth != null) playerHealth.isInvulnerable = false;
        if (playerController != null) playerController.SetCanMove(true);

        isPhantomActive = false;
    }

    private void SpawnShadowClone(Vector3 pos, Color tintColor)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        GameObject ghost = new GameObject("ShadowStrikeGhost");
        ghost.transform.position = pos;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();
        ghostSr.sprite = spriteRenderer.sprite;
        ghostSr.flipX = spriteRenderer.flipX;
        ghostSr.sortingLayerID = spriteRenderer.sortingLayerID;
        ghostSr.sortingOrder = spriteRenderer.sortingOrder - 1;
        ghostSr.color = tintColor;

        StartCoroutine(FadeShadowCloneRoutine(ghost, ghostSr));
    }

    private IEnumerator FadeShadowCloneRoutine(GameObject ghostObj, SpriteRenderer ghostSr)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Color startColor = (ghostSr != null) ? ghostSr.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (ghostSr != null)
            {
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
                ghostSr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }

        if (ghostObj != null) Destroy(ghostObj);
    }

    /// <summary>
    /// Giảm thời gian hồi Tuyệt Kỹ khi Dash (-2.0s)
    /// </summary>
    public void ReduceSpecialCooldown(float seconds)
    {
        if (specialCooldownTimer > 0f)
        {
            specialCooldownTimer = Mathf.Max(0f, specialCooldownTimer - seconds);
        }
    }

    // =========================================================================
    // HỆ THỐNG TÀN ẢNH TỐC BIẾN & THU HỒI BÓNG (MURAD S1 SHADOW PHANTOM WARP)
    // =========================================================================

    public bool PerformTeleportDash(Vector2 moveDir)
    {
        if (playerHealth != null && playerHealth.IsDead) return false;

        // Nếu đã có bóng đang kích hoạt -> Thu hồi về bóng ngay lập tức!
        if (HasActiveShadowClone)
        {
            return RecallToShadowClone();
        }

        Vector2 startPos = transform.position;
        Vector2 facing = (moveDir.sqrMagnitude > 0.01f) ? moveDir.normalized : (playerController != null ? playerController.GetFacingDirection() : Vector2.down);
        if (facing.sqrMagnitude < 0.001f) facing = Vector2.down;

        // Quét chướng ngại vật để tốc biến mượt mà không bị kẹt vào tường
        float actualDist = phantomTeleportDistance;
        RaycastHit2D hit = Physics2D.CircleCast(startPos, 0.35f, facing, phantomTeleportDistance, LayerMask.GetMask("Obstacle", "Default"));
        if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject)
        {
            actualDist = Mathf.Max(0.5f, hit.distance - 0.35f);
        }

        Vector2 targetPos = startPos + facing * actualDist;

        // 1. Sinh Tàn Ảnh Bóng Ma tại vị trí xuất phát (tồn tại tối đa 4.5s)
        SpawnPersistentShadowClone(startPos);

        // 2. Bất tử hoàn toàn trong khoảnh khắc tốc biến
        StartCoroutine(TemporaryInvulnerableRoutine(0.25f));

        // 3. Hiệu ứng Tốc Biến & Kiếm Khí Phong Lôi
        SpawnTeleportVFX(startPos, targetPos, facing);

        // 4. Dịch chuyển nhân vật tức thì tới đích
        transform.position = targetPos;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 5. Gây sát thương & gắn Phong Ấn dọc đường tốc biến
        DealTeleportSlashDamage(startPos, targetPos, facing);

        // 6. Kích hoạt trạng thái bóng ma
        hasActiveShadowClone = true;
        shadowClonePosition = startPos;
        shadowCloneTimer = shadowCloneDuration;

        ReduceSpecialCooldown(2.0f);
        SoundManager.EnsureInstance()?.PlaySwordSlash();

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.12f, 0.18f);

        return true;
    }

    public bool RecallToShadowClone()
    {
        if (!hasActiveShadowClone) return false;

        Vector2 recallPos = shadowClonePosition;
        Vector2 curPos = transform.position;

        // Bất tử trong khoảnh khắc biến về
        StartCoroutine(TemporaryInvulnerableRoutine(0.30f));

        // Hiệu ứng dịch chuyển ngược về bóng
        SpawnRecallVFX(curPos, recallPos);

        // Dịch chuyển nhân vật về đúng vị trí bóng ban đầu
        transform.position = recallPos;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Xóa bóng
        CancelShadowClone();

        ReduceSpecialCooldown(2.0f);
        SoundManager.EnsureInstance()?.PlayHitImpact();

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.16f, 0.22f);

        return true;
    }

    private void SpawnPersistentShadowClone(Vector2 position)
    {
        if (activeShadowCloneObj != null) Destroy(activeShadowCloneObj);

        activeShadowCloneObj = new GameObject("GaleRonin_ShadowClone");
        activeShadowCloneObj.transform.position = position;
        activeShadowCloneObj.transform.localScale = transform.localScale;

        var sr = activeShadowCloneObj.AddComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            sr.sprite = spriteRenderer.sprite;
            sr.flipX = spriteRenderer.flipX;
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder = spriteRenderer.sortingOrder - 1;
        }
        sr.color = new Color(0.3f, 0.95f, 1f, 0.85f);

        StartCoroutine(ShadowClonePulseRoutine(activeShadowCloneObj, sr));
    }

    private IEnumerator ShadowClonePulseRoutine(GameObject cloneObj, SpriteRenderer sr)
    {
        while (cloneObj != null)
        {
            float ratio = (shadowCloneDuration > 0f) ? Mathf.Clamp01(shadowCloneTimer / shadowCloneDuration) : 0f;
            float pulse = 0.65f + 0.25f * Mathf.Sin(Time.time * 7f);
            if (sr != null)
            {
                sr.color = new Color(0.3f, 0.95f, 1f, pulse * ratio);
            }
            yield return null;
        }
    }

    public void CancelShadowClone()
    {
        hasActiveShadowClone = false;
        if (activeShadowCloneObj != null)
        {
            StartCoroutine(FadeShadowCloneRoutine(activeShadowCloneObj, activeShadowCloneObj.GetComponent<SpriteRenderer>()));
            activeShadowCloneObj = null;
        }
    }

    private void DealTeleportSlashDamage(Vector2 startPos, Vector2 endPos, Vector2 dir)
    {
        float dist = Vector2.Distance(startPos, endPos);
        Vector2 midPoint = (startPos + endPos) * 0.5f;
        Collider2D[] hits = Physics2D.OverlapBoxAll(midPoint, new Vector2(dist + 1.2f, 1.6f), Vector2.SignedAngle(Vector2.right, dir), targetLayers);

        int bonusAtk = (playerCombat != null) ? playerCombat.BonusAttackDamage : 0;
        int dashSlashDmg = Mathf.RoundToInt(baseSlashDamage * 0.85f + bonusAtk);
        HashSet<GameObject> hitRoots = new HashSet<GameObject>();

        foreach (var col in hits)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable is PlayerHealth) continue;

            GameObject root = (damageable as Component)?.gameObject ?? col.gameObject;
            if (hitRoots.Contains(root)) continue;
            hitRoots.Add(root);

            HitTargetWithWindMark(root, damageable, dashSlashDmg, dir);
        }
    }

    private void SpawnTeleportVFX(Vector2 startPos, Vector2 endPos, Vector2 dir)
    {
        // 3 bóng mờ vút theo quỹ đạo tốc biến
        for (int i = 1; i <= 3; i++)
        {
            Vector2 ghostPos = Vector2.Lerp(startPos, endPos, i / 3.5f);
            SpawnShadowClone(ghostPos, new Color(0.2f, 0.95f, 1f, 0.55f));
        }
    }

    private void SpawnRecallVFX(Vector2 fromPos, Vector2 toPos)
    {
        // Vệt sáng lôi biến ngược về bóng
        for (int i = 1; i <= 3; i++)
        {
            Vector2 ghostPos = Vector2.Lerp(fromPos, toPos, i / 3.5f);
            SpawnShadowClone(ghostPos, new Color(0.4f, 1f, 0.8f, 0.65f));
        }
    }

    private IEnumerator TemporaryInvulnerableRoutine(float duration)
    {
        if (playerHealth != null)
        {
            playerHealth.isInvulnerable = true;
            yield return new WaitForSeconds(duration);
            if (!isChargingIaido && !isPhantomActive)
            {
                playerHealth.isInvulnerable = false;
            }
        }
    }

    private bool IsTargetKilled(Transform targetT, IDamageable targetDmg)
    {
        if (targetT == null) return true;

        var comp = targetDmg as Component;
        if (comp == null || !comp.gameObject.activeInHierarchy) return true;

        var col = targetT.GetComponent<Collider2D>() ?? comp.GetComponent<Collider2D>();
        if (col == null || !col.enabled) return true;

        return false;
    }

    void OnDrawGizmosSelected()
    {
        Vector2 facing = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
        Vector2 origin = (Vector2)transform.position + facing * 0.45f;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.85f);
        Gizmos.DrawWireSphere(origin, slashRadius);
    }
}
