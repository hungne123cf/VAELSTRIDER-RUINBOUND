using UnityEngine;
using System.Collections;

public class AzureBladeSkill : MonoBehaviour
{
    [Header("Azure Blade Combat Settings")]
    [SerializeField] private float attackWindupDelay = 0.08f; // Độ trễ vung kiếm
    [SerializeField] private float waveSpeed = 11.5f;
    [SerializeField] private int baseWaveDamage = 24; // Sát thương sóng kiếm cơ bản = 24 (Tụ lực max x2.25 = 54)
    [SerializeField] private float baseAttackCooldown = 5.0f; // Cooldown đòn đánh thường cơ bản là 5s
    [SerializeField] private float minAttackCooldown = 0.8f; // Cooldown tối thiểu khi nâng cấp max
    [SerializeField] private float specialCooldown = 18.0f;
    [SerializeField] private int ultiWaveCount = 5;
    [SerializeField] private float ultiSpreadAngle = 60f; // Nan quạt 60 độ

    public void SetBaseWaveDamage(int dmg) => baseWaveDamage = dmg;

    [Header("Hold / Aim Settings (0.1s - 1.0s)")]
    [SerializeField] private float minChargeTime = 0.10f; // Ngưỡng bắt đầu tính tụ lực
    [SerializeField] private float maxChargeTime = 1.00f; // Thời gian tụ lực tối đa 1.0s
    [SerializeField] private float maxChargedMultiplier = 2.25f; // Sát thương x2.25 khi tụ đủ 1.0s

    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private RangedAimIndicator aimIndicator;

    private GameObject cryoChargeAuraObj = null;
    private float nextFrostParticleTime = 0f;
    private bool hasPlayedMaxCryoGlint = false;

    private float attackCooldown = 5.0f;
    private float attackCooldownTimer = 0f;
    private float specialCooldownTimer = 0f;
    private float lastAttackTime = 0f;
    private bool isAiming = false;
    private float currentHoldDuration = 0f;
    private Vector2 currentAimDir = Vector2.down;

    public float CurrentAttackCooldown => attackCooldown;
    public bool IsAttackReady => attackCooldownTimer <= 0f;
    public float AttackCooldownRatio => Mathf.Clamp01(attackCooldownTimer / attackCooldown);
    public float AttackCooldownRemaining => Mathf.Max(0f, attackCooldownTimer);
    public bool IsSpecialReady => specialCooldownTimer <= 0f;
    public float SpecialCooldown => specialCooldown;
    public float SpecialCooldownRatio => Mathf.Clamp01(specialCooldownTimer / specialCooldown);
    public float SpecialCooldownRemaining => Mathf.Max(0f, specialCooldownTimer);
    public bool IsAiming => isAiming;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
        playerHealth = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        aimIndicator = RangedAimIndicator.EnsureExists(gameObject);
        RefreshCooldownStats();
    }

    void Start()
    {
        if (aimIndicator == null) aimIndicator = RangedAimIndicator.EnsureExists(gameObject);
        RefreshCooldownStats();
    }

    void OnEnable()
    {
        RefreshCooldownStats();
    }

    [Header("Speed to AP Conversion (Kiếm Khí Quy Hóa)")]
    private int convertedMagicAttack = 0;
    private float totalBonusSpeedScore = 0f;

    public int ConvertedMagicAttack => convertedMagicAttack;
    public float TotalBonusSpeedScore => totalBonusSpeedScore;

    /// <summary>
    /// Tính toán giảm thời gian hồi chiêu đòn đánh thường (5s -> 0.8s) dựa trên Trang bị, Khảm ngọc và Nâng cấp
    /// Đồng thời tự động chuyển hóa toàn bộ Tốc Đánh & Tốc Độ cộng thêm thành Sát Thương Phép (AP) cho Azure Blade.
    /// </summary>
    public void RefreshCooldownStats()
    {
        float totalCdr = 0f;

        // 1. Giảm hồi chiêu từ Nâng Cấp Nhanh Nhẹn (Agility / Speed Upgrade)
        var upgradeMgr = PlayerUpgradeManager.EnsureInstance();
        int agiLvl = 0;
        if (upgradeMgr != null)
        {
            agiLvl = upgradeMgr.GetLevel(PlayerUpgradeManager.UpgradeType.Agility);
            totalCdr += agiLvl * 0.35f; // Mỗi cấp giảm 0.35s hồi chiêu
        }

        // 2. Giảm hồi chiêu từ Trang bị Giáp (Armor Speed Bonus)
        ArmorShopUI.GetEquippedArmorBonus(out int _, out float _, out float armorSpd);
        if (armorSpd > 0)
        {
            totalCdr += armorSpd * 0.45f;
        }

        // 3. Giảm hồi chiêu từ Vũ khí (Weapon Speed Bonus)
        string equippedWpnId = PlayerPrefs.GetString("Equipped_Weapon_Id", "");
        float wpnSpeed = 0f;
        if (!string.IsNullOrEmpty(equippedWpnId))
        {
            var def = InventoryItemDatabase.GetItem(equippedWpnId);
            if (def != null && def.speedBonus > 0f)
            {
                wpnSpeed = def.speedBonus;
                totalCdr += def.speedBonus * 0.45f;
            }
        }

        // 4. Giảm hồi chiêu từ Khảm Ngọc (Socket Gem CDR %)
        EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out _, out _, out _, out float gemCdr, out _, out _);
        if (gemCdr > 0f)
        {
            totalCdr += baseAttackCooldown * gemCdr;
        }

        // 5. Giảm hồi chiêu từ Lõi Nâng Cấp (Augments Speed & CDR)
        float augSpeed = 0f;
        float augCDR = 0f;
        if (AugmentManager.Instance != null)
        {
            augSpeed = AugmentManager.Instance.GetAugBonusSpeed();
            augCDR = AugmentManager.Instance.GetAugCDR();
            totalCdr += baseAttackCooldown * (augCDR + augSpeed * 0.5f);
        }

        attackCooldown = Mathf.Max(minAttackCooldown, baseAttackCooldown - totalCdr);

        // 6. Cập nhật Cooldown Tuyệt Kỹ (18s -> min 5s)
        string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        float ultiUpCd = (upgradeMgr != null) ? upgradeMgr.GetUltiCooldownReduction(curHero) : 0f;
        float ultiGemCd = (gemCdr > 0f) ? 18.0f * gemCdr : 0f;
        float ultiAugCd = (augCDR > 0f) ? 18.0f * augCDR : 0f;
        specialCooldown = Mathf.Max(5.0f, 18.0f - ultiUpCd - ultiGemCd - ultiAugCd);

        // === CƠ CHẾ ĐẶC QUYỀN AZURE BLADE: TỐC ĐÁNH & TỐC ĐỘ CHUYỂN HÓA THÀNH SÁT THƯƠNG PHÉP (AP) ===
        // Mỗi điểm tốc độ / tốc đánh từ Nhanh nhẹn, Giáp, Vũ khí, Khảm ngọc, Lõi Nâng Cấp
        // đều tự động quy đổi thành Sát Thương Phép (AP) cộng thêm cho Sóng Kiếm!
        // - Mỗi 0.1s CDR / tốc đánh đạt được = 1 điểm tốc
        // - Mỗi +1.0 tốc chạy từ Vũ khí, Giáp = 10 điểm tốc
        // - Mỗi % tốc chạy từ Lõi nâng cấp = 0.75 điểm tốc
        float speedScore = totalCdr * 10f;
        speedScore += (wpnSpeed + armorSpd) * 10f;
        speedScore += (augSpeed * 100f) * 0.75f;

        totalBonusSpeedScore = speedScore;
        // Tỷ lệ chuyển hóa hào phóng: 1 điểm tốc quy đổi thành +2.2 Sát Thương Phép (AP)
        convertedMagicAttack = Mathf.Max(0, Mathf.RoundToInt(speedScore * 2.2f));
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

        // Handle PC Input for testing Tuyệt kỹ
        if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            TriggerSpecialSkill();
        }
    }

    /// <summary>
    /// Bắt đầu chế độ ngắm bắn & tụ lực (Hold to Aim)
    /// </summary>
    public bool StartAim(Vector2 initialDir)
    {
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (!IsAttackReady) return false;

        isAiming = true;
        currentHoldDuration = 0f;
        hasPlayedMaxCryoGlint = false;
        currentAimDir = (initialDir.sqrMagnitude > 0.01f) ? initialDir.normalized : GetFacingDirection();
        if (aimIndicator == null) aimIndicator = RangedAimIndicator.EnsureExists(gameObject);

        CreateCryoChargeAura();
        return true;
    }

    /// <summary>
    /// Cập nhật hướng ngắm và thời gian tụ lực từ 0.1s đến 1.0s
    /// </summary>
    public void UpdateAim(Vector2 aimDirection, float holdDuration)
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            CancelAim();
            return;
        }

        isAiming = true;
        currentHoldDuration = holdDuration;
        if (aimDirection.sqrMagnitude > 0.01f)
        {
            currentAimDir = aimDirection.normalized;
        }

        // Xoay nhân vật theo tia ngắm
        if (animator != null)
        {
            animator.SetFloat("MoveX", currentAimDir.x);
            animator.SetFloat("MoveY", currentAimDir.y);
        }

        // Tỷ lệ tụ lực tính trong khoảng [0.1s -> 1.0s]
        float chargeProgress = Mathf.Clamp01((currentHoldDuration - minChargeTime) / (maxChargeTime - minChargeTime));

        // Cập nhật quầng hào quang băng tuyết
        UpdateCryoChargeAura(chargeProgress);

        // Sinh hạt tinh thể băng ngọc hút dồn vào lưỡi kiếm
        if (Time.time >= nextFrostParticleTime)
        {
            nextFrostParticleTime = Time.time + 0.045f;
            SpawnInwardFrostCrystal(transform.position, currentAimDir, chargeProgress);
        }

        // Hiển thị tia ngắm laze định hướng chuẩn xạ thủ
        if (aimIndicator != null)
        {
            Vector2 origin = (Vector2)transform.position + currentAimDir * 0.4f;
            aimIndicator.Show(origin, currentAimDir, chargeProgress);
        }

        // KHI ĐẠT 100% TỤ LỰC (MAX CHARGE 1.0s): Khóa hồng tâm hoàn hảo + Lóe sáng tinh thể băng `✧`!
        if (chargeProgress >= 1.0f && !hasPlayedMaxCryoGlint)
        {
            hasPlayedMaxCryoGlint = true;
            Vector3 glintPos = transform.position + (Vector3)(currentAimDir * 0.45f) + Vector3.up * 0.25f;
            SpawnCryoStarGlint(glintPos);
            SoundManager.EnsureInstance()?.PlayHitImpact();

            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.12f, 0.18f);

            StartCoroutine(FlashSpriteRoutine(new Color(0.4f, 0.95f, 1f, 1f), 0.12f));
        }
    }

    /// <summary>
    /// Hủy ngắm bắn khi kéo vào vùng Hủy Chiêu
    /// </summary>
    public void CancelAim()
    {
        isAiming = false;
        currentHoldDuration = 0f;
        hasPlayedMaxCryoGlint = false;
        DestroyCryoChargeAura();
        if (aimIndicator != null) aimIndicator.Hide();
    }

    /// <summary>
    /// Thả tay (Release) để tính toán sát thương theo thời gian nhấn giữ (0.1s -> 1.0s)
    /// </summary>
    public void ReleaseAimAndFire(int bonusAttackDamage)
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            CancelAim();
            return;
        }

        if (aimIndicator != null) aimIndicator.Hide();
        DestroyCryoChargeAura();
        hasPlayedMaxCryoGlint = false;

        if (!IsAttackReady) return;

        attackCooldownTimer = attackCooldown;
        isAiming = false;

        // Vung kiếm hoạt ảnh
        if (animator != null)
        {
            animator.SetTrigger("AttackTrigger");
            animator.Play("Attack2", 0, 0f);
        }

        SoundManager.EnsureInstance()?.PlaySwordSlash();

        // Tính tỷ lệ tụ lực từ 0.1s đến 1.0s
        float chargeProgress = Mathf.Clamp01((currentHoldDuration - minChargeTime) / (maxChargeTime - minChargeTime));
        bool isFullCharge = chargeProgress >= 0.90f;

        // Tính sát thương tăng tiến theo mức tụ lực
        float damageMultiplier = Mathf.Lerp(1.0f, maxChargedMultiplier, chargeProgress);
        int totalDamage = Mathf.RoundToInt((baseWaveDamage + bonusAttackDamage) * damageMultiplier);

        // Hiển thị hiệu ứng tụ lực
        if (isFullCharge)
        {
            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.16f, 0.22f);
        }

        // Vị trí xuất phát kiếm khí
        Vector3 spawnPos = transform.position + (Vector3)(currentAimDir * 0.50f) + Vector3.up * 0.2f;

        // Hiệu ứng nổ bung tinh thể băng khi bắn ra
        if (chargeProgress > 0.25f)
        {
            SpawnCryoMuzzleBurst(spawnPos, currentAimDir, chargeProgress);
        }

        // Tốc độ và số lượng xuyên thấu tăng theo mức tụ lực
        float speed = Mathf.Lerp(waveSpeed, waveSpeed * 1.45f, chargeProgress);
        int pierce = Mathf.RoundToInt(Mathf.Lerp(4, 9, chargeProgress));

        // Phóng sóng kiếm (kích thước chuẩn với hiệu ứng phát sáng lam băng)
        float waveScale = Mathf.Lerp(0.9f, 1.35f, chargeProgress);
        SwordWaveProjectile.Spawn(spawnPos, currentAimDir, totalDamage, speed, scaleMultiplier: waveScale, isUlti: false, isCharged: (chargeProgress > 0.2f), pierceLimit: pierce, ownerSkill: this);
    }

    /// <summary>
    /// Giảm 50% thời gian hồi chiêu và hồi phục Năng Lượng / Nội Năng khi kiếm khí chém trúng quái vật
    /// </summary>
    public void OnWaveHitEnemy(int hitDamage = 0, bool isCharged = false, bool isUlti = false)
    {
        if (attackCooldownTimer > 0.05f)
        {
            float reduction = attackCooldownTimer * 0.5f;
            attackCooldownTimer = Mathf.Max(0f, attackCooldownTimer - reduction);
        }

        // Tích tụ năng lượng & Nội Năng CHỈ KHI kiếm khí chém trúng quái vật
        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null)
        {
            float gain = isUlti ? 4f : (isCharged ? 16f : 8f);
            energySys.RestoreEnergy(gain);
            energySys.AddInnerEnergy(gain * 1.5f);
        }

        if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null)
        {
            int effDmg = (hitDamage > 0) ? hitDamage : (isCharged ? Mathf.RoundToInt(baseWaveDamage * maxChargedMultiplier) : baseWaveDamage);
            int heal = Mathf.Max(1, Mathf.RoundToInt(effDmg * playerCombat.LifestealPercent));
            playerHealth.Heal(heal);
        }
    }

    /// <summary>
    /// Hồi ngay lập tức đòn đánh thường (khi Dash)
    /// </summary>
    public void ResetAttackCooldown()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer = 0f;
        }
    }

    /// <summary>
    /// Đòn chém thường nhấp nhanh (< 0.1s)
    /// </summary>
    public bool OnPerformAttack(Vector2 facingDirection, int baseAttackDamage)
    {
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (!IsAttackReady) return false;
        attackCooldownTimer = attackCooldown;

        if (aimIndicator != null) aimIndicator.Hide();
        StartCoroutine(ExecuteHeavySlashWithWave(facingDirection, baseAttackDamage));
        return true;
    }

    private IEnumerator ExecuteHeavySlashWithWave(Vector2 facingDirection, int baseAtk)
    {
        if (facingDirection.sqrMagnitude < 0.001f) facingDirection = Vector2.down;
        facingDirection = facingDirection.normalized;

        yield return new WaitForSeconds(attackWindupDelay);

        Vector3 spawnPos = transform.position + (Vector3)(facingDirection * 0.7f) + Vector3.up * 0.2f;
        int totalDamage = baseWaveDamage + baseAtk;

        SoundManager.EnsureInstance()?.PlaySwordSlash();
        SwordWaveProjectile.Spawn(spawnPos, facingDirection, totalDamage, waveSpeed, scaleMultiplier: 1.0f, isUlti: false, isCharged: false, pierceLimit: 4, ownerSkill: this);
    }

    /// <summary>
    /// Kỹ năng Tuyệt Kỹ: Bão Sóng Kiếm (Crescent Tempest Wave)
    /// </summary>
    public bool TriggerSpecialSkill()
    {
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (specialCooldownTimer > 0f)
        {
            return false;
        }

        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null && !energySys.TryConsumeEnergy(50f))
        {
            return false;
        }

        specialCooldownTimer = specialCooldown;
        StartCoroutine(ExecuteCrescentTempest());
        return true;
    }

    private IEnumerator ExecuteCrescentTempest()
    {
        Vector2 facing = playerController != null ? GetFacingDirection() : Vector2.down;
        if (facing.sqrMagnitude < 0.001f) facing = Vector2.down;
        facing = facing.normalized;

        SoundManager.EnsureInstance()?.PlayHitImpact();

        if (animator != null)
        {
            animator.SetTrigger("AttackTrigger");
        }

        yield return new WaitForSeconds(0.1f);

        Vector3 origin = transform.position + (Vector3)(facing * 0.8f) + Vector3.up * 0.2f;
        float baseAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;

        float startAngle = baseAngle - (ultiSpreadAngle / 2f);
        float angleStep = ultiSpreadAngle / (ultiWaveCount - 1);

        int upUltBonus = 0;
        var upMgr = PlayerUpgradeManager.EnsureInstance();
        if (upMgr != null) upUltBonus = upMgr.GetUltiDamageBonus();

        if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();
        int curAtk = (playerCombat != null) ? (playerCombat.Attack1Damage + playerCombat.BonusAttackDamage) : 24;
        int waveDamage = (baseWaveDamage * 2) + curAtk + upUltBonus;

        for (int i = 0; i < ultiWaveCount; i++)
        {
            float curAngle = startAngle + (i * angleStep);
            float rad = curAngle * Mathf.Deg2Rad;
            Vector2 waveDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            SwordWaveProjectile.Spawn(origin, waveDir, waveDamage, waveSpeed * 1.25f, scaleMultiplier: 1.0f, isUlti: true, isCharged: false, pierceLimit: 15, ownerSkill: this);
        }
    }

    public Vector2 GetFacingDirection()
    {
        if (animator == null) return Vector2.down;
        float mx = animator.GetFloat("MoveX");
        float my = animator.GetFloat("MoveY");
        Vector2 dir = new Vector2(mx, my);
        if (dir.sqrMagnitude < 0.01f) return Vector2.down;
        return dir.normalized;
    }

    // =========================================================================
    // HỆ THỐNG HIỆU ỨNG THỊ GIÁC BĂNG LAM XẠ THỦ (CRYO MARKSMAN VFX SYSTEM)
    // =========================================================================

    private void CreateCryoChargeAura()
    {
        if (cryoChargeAuraObj != null) Destroy(cryoChargeAuraObj);

        cryoChargeAuraObj = new GameObject("Azure_CryoChargeAura");
        cryoChargeAuraObj.transform.SetParent(transform, false);
        cryoChargeAuraObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);

        var light = cryoChargeAuraObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = new Color(0.2f, 0.95f, 1f, 1f);
        light.intensity = 1.3f;
        light.pointLightInnerRadius = 0.25f;
        light.pointLightOuterRadius = 1.5f;
    }

    private void UpdateCryoChargeAura(float ratio)
    {
        if (cryoChargeAuraObj == null) return;
        var light = cryoChargeAuraObj.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
        if (light != null)
        {
            light.intensity = Mathf.Lerp(1.3f, 3.0f, ratio);
            light.pointLightOuterRadius = Mathf.Lerp(1.5f, 2.4f, ratio);
            light.color = Color.Lerp(new Color(0.2f, 0.95f, 1f, 1f), new Color(0.9f, 1f, 1f, 1f), ratio);
        }
    }

    private void DestroyCryoChargeAura()
    {
        if (cryoChargeAuraObj != null)
        {
            Destroy(cryoChargeAuraObj);
            cryoChargeAuraObj = null;
        }
    }

    private void SpawnInwardFrostCrystal(Vector3 center, Vector2 facingDir, float ratio)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(1.1f, 1.7f);
        Vector3 spawnPos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

        GameObject p = new GameObject("FrostCrystalMote");
        p.transform.position = spawnPos;
        p.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);

        var sr = p.AddComponent<SpriteRenderer>();
        sr.sprite = GetFrostDiamondSprite();
        sr.color = (ratio >= 0.85f) ? new Color(0.95f, 1f, 1f, 0.95f) : new Color(0.35f, 0.95f, 1f, 0.85f);
        sr.sortingOrder = 3200;

        var rb = p.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        Vector2 target = (Vector2)center + facingDir * 0.35f;
        Vector2 flyDir = (target - (Vector2)spawnPos).normalized;
        rb.linearVelocity = flyDir * Random.Range(6f, 10f);

        Destroy(p, 0.18f);
    }

    private void SpawnCryoStarGlint(Vector3 glintPos)
    {
        GameObject star = new GameObject("CryoLockOnGlint");
        star.transform.position = glintPos;
        star.transform.localScale = Vector3.zero;

        var sr = star.AddComponent<SpriteRenderer>();
        sr.sprite = GetFrostDiamondSprite();
        sr.color = new Color(1f, 1f, 1f, 1f);
        sr.sortingOrder = 4500;

        var light = star.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = new Color(0.4f, 0.95f, 1f, 1f);
        light.intensity = 3.5f;
        light.pointLightInnerRadius = 0.25f;
        light.pointLightOuterRadius = 1.9f;

        StartCoroutine(AnimateCryoStarRoutine(star, light));
    }

    private IEnumerator AnimateCryoStarRoutine(GameObject starObj, UnityEngine.Rendering.Universal.Light2D light)
    {
        float dur = 0.22f;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            float scale = Mathf.Sin(t * Mathf.PI) * 1.5f;
            if (starObj != null) starObj.transform.localScale = Vector3.one * scale;
            if (light != null) light.intensity = (1f - t) * 3.5f;
            yield return null;
        }

        if (starObj != null) Destroy(starObj);
    }

    private void SpawnCryoMuzzleBurst(Vector3 spawnPos, Vector2 direction, float ratio)
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject spk = new GameObject("CryoMuzzleShard");
            spk.transform.position = spawnPos + (Vector3)Random.insideUnitCircle * 0.12f;
            spk.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);

            var sr = spk.AddComponent<SpriteRenderer>();
            sr.sprite = GetFrostDiamondSprite();
            sr.color = new Color(0.6f, 0.95f, 1f, 0.95f);
            sr.sortingOrder = 3600;

            float spreadAngle = Random.Range(-40f, 40f);
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spreadAngle;
            float rad = baseAngle * Mathf.Deg2Rad;
            Vector2 shootDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            var rb = spk.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = shootDir * Random.Range(4f, 8f);

            StartCoroutine(FadeCryoObjectRoutine(spk, sr, 0.18f));
        }
    }

    private IEnumerator FadeCryoObjectRoutine(GameObject obj, SpriteRenderer sr, float duration)
    {
        float elapsed = 0f;
        Color startCol = (sr != null) ? sr.color : Color.white;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (sr != null) sr.color = new Color(startCol.r, startCol.g, startCol.b, (1f - t) * startCol.a);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    private IEnumerator FlashSpriteRoutine(Color flashColor, float duration)
    {
        if (spriteRenderer == null) yield break;
        Color orig = spriteRenderer.color;
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(duration);
        if (spriteRenderer != null) spriteRenderer.color = orig;
    }

    private static Sprite cachedFrostDiamondSprite = null;
    private static Sprite GetFrostDiamondSprite()
    {
        if (cachedFrostDiamondSprite != null) return cachedFrostDiamondSprite;

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

                // Diamond frost crystal formula
                float dist = dx + dy;
                if (dist <= 1.0f)
                {
                    float alpha = 1f - dist;
                    if (dist < 0.35f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 1f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(0.35f, 0.95f, 1f, alpha * 1.5f));
                    }
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        cachedFrostDiamondSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        return cachedFrostDiamondSprite;
    }
}
