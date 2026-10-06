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
    private RangedAimIndicator aimIndicator;

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

    /// <summary>
    /// Tính toán giảm thời gian hồi chiêu đòn đánh thường (5s -> 0.8s) dựa trên Trang bị, Khảm ngọc và Nâng cấp
    /// </summary>
    public void RefreshCooldownStats()
    {
        float totalCdr = 0f;

        // 1. Giảm hồi chiêu từ Nâng Cấp Nhanh Nhẹn (Agility / Speed Upgrade)
        var upgradeMgr = PlayerUpgradeManager.EnsureInstance();
        if (upgradeMgr != null)
        {
            int agiLvl = upgradeMgr.GetLevel(PlayerUpgradeManager.UpgradeType.Agility);
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
        if (!string.IsNullOrEmpty(equippedWpnId))
        {
            var def = InventoryItemDatabase.GetItem(equippedWpnId);
            if (def != null && def.speedBonus > 0f)
            {
                totalCdr += def.speedBonus * 0.45f;
            }
        }

        // 4. Giảm hồi chiêu từ Khảm Ngọc (Socket Gem CDR %)
        EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out _, out _, out _, out float gemCdr, out _, out _);
        if (gemCdr > 0f)
        {
            totalCdr += baseAttackCooldown * gemCdr;
        }

        attackCooldown = Mathf.Max(minAttackCooldown, baseAttackCooldown - totalCdr);

        // 5. Cập nhật Cooldown Tuyệt Kỹ (18s -> min 5s)
        float ultiUpCd = (upgradeMgr != null) ? upgradeMgr.GetUltiCooldownReduction() : 0f;
        float ultiGemCd = (gemCdr > 0f) ? 18.0f * gemCdr : 0f;
        specialCooldown = Mathf.Max(5.0f, 18.0f - ultiUpCd - ultiGemCd);
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
        if (!IsAttackReady) return false;

        isAiming = true;
        currentHoldDuration = 0f;
        currentAimDir = (initialDir.sqrMagnitude > 0.01f) ? initialDir.normalized : GetFacingDirection();
        if (aimIndicator == null) aimIndicator = RangedAimIndicator.EnsureExists(gameObject);
        return true;
    }

    /// <summary>
    /// Cập nhật hướng ngắm và thời gian tụ lực từ 0.1s đến 1.0s
    /// </summary>
    public void UpdateAim(Vector2 aimDirection, float holdDuration)
    {
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

        // Hiển thị tia ngắm laze định hướng
        if (aimIndicator != null)
        {
            Vector2 origin = (Vector2)transform.position + currentAimDir * 0.4f;
            aimIndicator.Show(origin, currentAimDir, chargeProgress);
        }
    }

    /// <summary>
    /// Thả tay (Release) để tính toán sát thương theo thời gian nhấn giữ (0.1s -> 1.0s)
    /// </summary>
    public void ReleaseAimAndFire(int bonusAttackDamage)
    {
        if (aimIndicator != null) aimIndicator.Hide();
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

        // Hiển thị popup hiệu ứng tụ lực
        if (isFullCharge)
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, "★ MAX CHARGE (1.0s)!", new Color(1f, 0.9f, 0.2f));
        }
        else if (chargeProgress > 0.25f)
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, $"CHARGED +{Mathf.RoundToInt(chargeProgress * 100)}%!", new Color(0.3f, 0.95f, 1f));
        }

        // Vị trí xuất phát kiếm khí
        Vector3 spawnPos = transform.position + (Vector3)(currentAimDir * 0.7f) + Vector3.up * 0.2f;

        // Tốc độ và số lượng xuyên thấu tăng theo mức tụ lực
        float speed = Mathf.Lerp(waveSpeed, waveSpeed * 1.45f, chargeProgress);
        int pierce = Mathf.RoundToInt(Mathf.Lerp(4, 9, chargeProgress));

        // Phóng sóng kiếm (kích thước chuẩn 1.0 với hình ảnh vẽ tay Twin_sword)
        SwordWaveProjectile.Spawn(spawnPos, currentAimDir, totalDamage, speed, scaleMultiplier: 1.0f, isUlti: false, isCharged: (chargeProgress > 0.2f), pierceLimit: pierce, ownerSkill: this);
    }

    /// <summary>
    /// Giảm 50% thời gian hồi chiêu và hồi phục Năng Lượng / Nội Năng khi kiếm khí chém trúng quái vật
    /// </summary>
    public void OnWaveHitEnemy(bool isCharged = false, bool isUlti = false)
    {
        if (attackCooldownTimer > 0.05f)
        {
            float reduction = attackCooldownTimer * 0.5f;
            attackCooldownTimer = Mathf.Max(0f, attackCooldownTimer - reduction);
            DamagePopup.CreateText(transform.position + Vector3.up * 1.3f, "⚡ CD -50%!", new Color(0.2f, 1f, 0.85f));
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
            int heal = Mathf.Max(1, Mathf.RoundToInt(baseWaveDamage * playerCombat.LifestealPercent));
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
            DamagePopup.CreateText(transform.position + Vector3.up * 1.3f, "⚡ CD RESET!", new Color(1f, 0.9f, 0.2f));
        }
    }

    /// <summary>
    /// Đòn chém thường nhấp nhanh (< 0.1s)
    /// </summary>
    public bool OnPerformAttack(Vector2 facingDirection, int baseAttackDamage)
    {
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
        if (specialCooldownTimer > 0f)
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, "COOLDOWN!", Color.yellow);
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

        DamagePopup.CreateText(transform.position + Vector3.up * 1.8f, "AZURE TEMPEST!", new Color(0.2f, 0.9f, 1f));
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

        for (int i = 0; i < ultiWaveCount; i++)
        {
            float curAngle = startAngle + (i * angleStep);
            float rad = curAngle * Mathf.Deg2Rad;
            Vector2 waveDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            SwordWaveProjectile.Spawn(origin, waveDir, baseWaveDamage * 2 + 10, waveSpeed * 1.25f, scaleMultiplier: 1.0f, isUlti: true, isCharged: false, pierceLimit: 15);
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
}
