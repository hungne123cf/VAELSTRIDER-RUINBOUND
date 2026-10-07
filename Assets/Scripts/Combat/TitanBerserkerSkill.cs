using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Kỹ năng Chiến đấu của Player 4 - Cuồng Chiến Binh Đại Chùy (Titan Berserker)
/// Phân lớp: Tanker / Đấu Sĩ Càn Quét (High HP, Giáp Chống Chịu, Hút Quái & Chấn Địa Dung Nham)
/// </summary>
public class TitanBerserkerSkill : MonoBehaviour
{
    [Header("--- THÔNG SỐ ĐÒN ĐÁNH ĐẠI CHÙY ---")]
    public int baseHammerDamage = 32;
    public float attackRange = 2.4f;
    public float attackAngle = 140f;
    public float attackCooldown = 0.55f;
    private float attackCooldownTimer = 0f;

    [Header("--- NỘI TẠI THỂ PHÁCH TITAN (PASSIVE) ---")]
    [Range(0f, 0.5f)]
    public float innateDamageReduction = 0.20f; // Giảm 20% sát thương nhận vào
    public float ragePerHit = 25f;
    private float currentBerserkRage = 0f;
    private const float MAX_BERSERK_RAGE = 100f;

    [Header("--- TUYỆT KỸ: CUỒNG NỘ CHẤN ĐỊA (CATACLYSM SLAM) ---")]
    public float specialCooldown = 12.0f;
    private float specialCooldownTimer = 0f;
    public float ultimateRadius = 5.5f;
    public int baseCataclysmDamage = 95;
    private bool isExecutingCataclysm = false;

    [Header("--- TARGETING LAYERS ---")]
    public LayerMask enemyLayers;

    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    private PlayerController playerController;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private int comboStep = 0;
    private float lastComboTime = 0f;
    private bool isSpinning = false;
    public bool IsSpinning => isSpinning;

    public bool IsSpecialReady => specialCooldownTimer <= 0f && !isExecutingCataclysm;
    public float SpecialCooldownRatio => (specialCooldown > 0f) ? Mathf.Clamp01(specialCooldownTimer / specialCooldown) : 0f;
    public float SpecialCooldownRemaining => Mathf.Max(0f, specialCooldownTimer);
    public float AttackCooldownRatio => (attackCooldown > 0f) ? Mathf.Clamp01(attackCooldownTimer / attackCooldown) : 0f;
    public float AttackCooldownRemaining => Mathf.Max(0f, attackCooldownTimer);
    public float CooldownProgress => Mathf.Clamp01(specialCooldownTimer / specialCooldown);
    public float BerserkRagePercent => Mathf.Clamp01(currentBerserkRage / MAX_BERSERK_RAGE);

    void Awake()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerHealth = GetComponent<PlayerHealth>();
        playerController = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (enemyLayers.value == 0)
        {
            enemyLayers = LayerMask.GetMask("Enemy", "Default", "Obstacle");
        }
    }

    void Update()
    {
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;
        if (specialCooldownTimer > 0f) specialCooldownTimer -= Time.deltaTime;
    }

    public bool IsAttackReady => attackCooldownTimer <= 0f && !isExecutingCataclysm && !isSpinning;

    /// <summary>
    /// Đòn đánh thường: Combo 4 Đòn (Chém 1 -> Chém 2 -> Húc Búa 3 -> Xoay Búa Lốc Xoáy 4 Tự Do Di Chuyển)
    /// </summary>
    public bool OnPerformAttack(Vector2 facingDir, int bonusAtkDamage)
    {
        if (!IsAttackReady) return false;

        if (Time.time - lastComboTime > 1.25f)
        {
            comboStep = 0;
        }

        comboStep++;
        lastComboTime = Time.time;

        if (facingDir.sqrMagnitude < 0.001f) facingDir = Vector2.down;
        facingDir = facingDir.normalized;

        if (comboStep < 4)
        {
            attackCooldownTimer = attackCooldown;
            if (animator != null)
            {
                animator.SetInteger("ComboStep", comboStep);
                animator.SetTrigger("AttackTrigger");
                string stateName = (comboStep == 3) ? "Attack2" : "Attack1";
                animator.Play(stateName, 0, 0f);
            }

            StartCoroutine(HammerSmashRoutine(facingDir, bonusAtkDamage, isBash: (comboStep == 3)));
        }
        else
        {
            // Đòn thứ 4: CƠN LỐC CUỒNG NỘ (SPIN WHIRLWIND 360°) - VỪA XOAY VỪA TỰ DO DI CHUYỂN
            comboStep = 0;
            attackCooldownTimer = 3.35f;
            StartCoroutine(SpinWhirlwindRoutine(bonusAtkDamage));
        }

        return true;
    }

    private IEnumerator HammerSmashRoutine(Vector2 facingDir, int bonusAtk, bool isBash = false)
    {
        SoundManager.EnsureInstance()?.PlayHitImpact();

        yield return new WaitForSeconds(0.16f);

        Vector3 center = (playerCombat != null) ? playerCombat.GetBodyCenterWorld() : (transform.position + Vector3.up * 0.65f);
        Vector3 origin = center + (Vector3)(facingDir * (isBash ? 1.0f : 0.85f));

        int totalDmg = baseHammerDamage + bonusAtk + (isBash ? 12 : 0);
        float smashRadius = isBash ? 1.45f : 1.35f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, smashRadius, enemyLayers);
        int hitCount = 0;

        foreach (var col in hits)
        {
            if (col == null || col.gameObject == gameObject) continue;

            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is PlayerHealth))
            {
                hitCount++;
                damageable.TakeDamage(totalDmg);
                DamagePopup.Create(col.transform.position, totalDmg, isCritical: isBash || Random.value < 0.25f);

                // Hất lùi quái mạnh
                var rb = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 pushDir = ((Vector2)col.transform.position - (Vector2)center).normalized;
                    if (pushDir.sqrMagnitude < 0.01f) pushDir = facingDir;
                    rb.AddForce(pushDir * (isBash ? 11.0f : 7.5f), ForceMode2D.Impulse);
                }
            }
        }

        if (hitCount > 0)
        {
            SoundManager.EnsureInstance()?.PlayHitImpact();
            var cam = Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(isBash ? 0.22f : 0.16f, 0.20f);

            AddBerserkRage(ragePerHit * hitCount);

            if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null)
            {
                int heal = Mathf.Max(1, Mathf.RoundToInt(totalDmg * playerCombat.LifestealPercent));
                playerHealth.Heal(heal);
            }
        }
    }

    /// <summary>
    /// Đòn thứ 4: Cơn Lốc Cuồng Nộ Xoay Búa 360° - Vừa Xoay Vừa Di Chuyển Càn Quét
    /// </summary>
    private IEnumerator SpinWhirlwindRoutine(int bonusAtk)
    {
        isSpinning = true;

        if (animator != null)
        {
            animator.SetTrigger("SpinTrigger");
            animator.Play("Spin", 0, 0f);
        }

        DamagePopup.CreateText(transform.position + Vector3.up * 1.8f, LocalizationManager.T("⚔ CUỒNG NỘ LỐC XOÁY! ⚔", "⚔ TITAN WHIRLWIND! ⚔"), new Color(1f, 0.55f, 0.1f));
        SoundManager.EnsureInstance()?.PlaySwordSlash();

        float spinDuration = 3.2f;
        float tickInterval = 0.22f;
        float elapsed = 0f;
        float spinRadius = 2.4f;

        while (elapsed < spinDuration)
        {
            elapsed += tickInterval;
            SoundManager.EnsureInstance()?.PlaySwordSlash();

            Vector3 center = (playerCombat != null) ? playerCombat.GetBodyCenterWorld() : (transform.position + Vector3.up * 0.65f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, spinRadius, enemyLayers);

            bool isLastTick = elapsed >= spinDuration;
            int tickDmg = Mathf.RoundToInt((baseHammerDamage * 0.55f) + (bonusAtk * 0.4f));
            if (isLastTick) tickDmg = Mathf.RoundToInt(baseHammerDamage * 1.35f + bonusAtk);

            int hitCount = 0;
            foreach (var col in hits)
            {
                if (col == null || col.gameObject == gameObject) continue;

                var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (damageable != null && !(damageable is PlayerHealth))
                {
                    hitCount++;
                    damageable.TakeDamage(tickDmg);
                    DamagePopup.Create(col.transform.position, tickDmg, isCritical: isLastTick || Random.value < 0.3f);

                    var rb = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
                    if (rb != null)
                    {
                        if (!isLastTick)
                        {
                            // Hút nhẹ quái vào tâm lốc xoáy
                            Vector2 pullDir = ((Vector2)center - (Vector2)col.transform.position).normalized;
                            rb.AddForce(pullDir * 4.5f, ForceMode2D.Impulse);
                        }
                        else
                        {
                            // Cú chốt: Hất tung tất cả quái văng ra xa!
                            Vector2 blastDir = ((Vector2)col.transform.position - (Vector2)center).normalized;
                            rb.AddForce(blastDir * 12.0f, ForceMode2D.Impulse);
                        }
                    }
                }
            }

            if (hitCount > 0)
            {
                AddBerserkRage(10f * hitCount);
                var cam = Object.FindAnyObjectByType<CameraFollow>();
                if (cam != null) cam.Shake(isLastTick ? 0.25f : 0.10f, 0.12f);
            }

            yield return new WaitForSeconds(tickInterval);
        }

        isSpinning = false;
        if (animator != null)
        {
            animator.Play("Idle", 0, 0f);
        }
    }

    /// <summary>
    /// Tích lũy Nộ Cuồng Chiến Binh & Kích hoạt Khiên Chấn Địa khi đầy Nộ
    /// </summary>
    public void AddBerserkRage(float amount)
    {
        currentBerserkRage = Mathf.Clamp(currentBerserkRage + amount, 0f, MAX_BERSERK_RAGE);
        if (currentBerserkRage >= MAX_BERSERK_RAGE)
        {
            TriggerTitanShield();
            currentBerserkRage = 0f;
        }
    }

    private void TriggerTitanShield()
    {
        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null)
        {
            energySys.ActivateShield();
        }

        SoundManager.EnsureInstance()?.PlayHitImpact();
        DamagePopup.CreateText(transform.position + Vector3.up * 1.8f, LocalizationManager.T("★ THỂ PHÁCH TITAN: KHIÊN ĐẤT ĐÁ! ★", "★ TITAN BULWARK ACTIVATED! ★"), new Color(1f, 0.8f, 0.2f));

        // Hất văng nhẹ kẻ địch xung quanh
        Collider2D[] burstHits = Physics2D.OverlapCircleAll(transform.position, 2.5f, enemyLayers);
        foreach (var col in burstHits)
        {
            if (col == null || col.gameObject == gameObject) continue;
            var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (dmg != null && !(dmg is PlayerHealth))
            {
                dmg.TakeDamage(18);
                DamagePopup.Create(col.transform.position, 18, isCritical: false);
                var rb = col.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 pushDir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
                    rb.AddForce(pushDir * 5f, ForceMode2D.Impulse);
                }
            }
        }
    }

    /// <summary>
    /// Tuyệt Kỹ: Cuồng Nộ Chấn Địa (Titan Cataclysm Slam)
    /// Hút toàn bộ kẻ địch trong 5.5m vào tâm chấn rồi nện búa dung nham hất tung và gây choáng!
    /// </summary>
    public bool TriggerSpecialSkill()
    {
        if (specialCooldownTimer > 0f || isExecutingCataclysm)
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, LocalizationManager.T("ĐANG HỒI CHIÊU!", "COOLDOWN!"), Color.yellow);
            return false;
        }

        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null && !energySys.TryConsumeEnergy(45f))
        {
            return false;
        }

        specialCooldownTimer = specialCooldown;
        StartCoroutine(CataclysmRoutine());
        return true;
    }

    private IEnumerator CataclysmRoutine()
    {
        isExecutingCataclysm = true;

        if (playerHealth != null) playerHealth.isInvulnerable = true;
        if (playerController != null) playerController.SetCanMove(false);

        DamagePopup.CreateText(transform.position + Vector3.up * 2.0f, LocalizationManager.T("CUỒNG NỘ CHẤN ĐỊA!", "TITAN CATACLYSM!"), new Color(1f, 0.4f, 0.1f));
        SoundManager.EnsureInstance()?.PlayHitImpact();

        // 1. TÂM CHẤN HÚT KẺ ĐỊCH VÀO TÂM TRONG 0.4s
        float pullDuration = 0.45f;
        float elapsed = 0f;
        Vector3 slamCenter = transform.position;

        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;
            Collider2D[] pullHits = Physics2D.OverlapCircleAll(slamCenter, ultimateRadius, enemyLayers);
            foreach (var col in pullHits)
            {
                if (col == null || col.gameObject == gameObject) continue;
                var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (dmg != null && !(dmg is PlayerHealth))
                {
                    Vector2 pullDir = ((Vector2)slamCenter - (Vector2)col.transform.position).normalized;
                    col.transform.position = Vector3.MoveTowards(col.transform.position, slamCenter, 6.5f * Time.deltaTime);
                }
            }
            yield return null;
        }

        // 2. NỆN ĐẠI CHÙY DUNG NHAM BỘC PHÁ DIỆN RỘNG (AREA SLAM)
        if (animator != null)
        {
            animator.SetTrigger("SkillTrigger");
        }

        SoundManager.EnsureInstance()?.PlayHitImpact();
        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.45f, 0.6f);

        int upUltBonus = 0;
        var upMgr = PlayerUpgradeManager.EnsureInstance();
        if (upMgr != null) upUltBonus = upMgr.GetUltiDamageBonus();

        int cataclysmDamage = baseCataclysmDamage + (playerCombat != null ? playerCombat.BonusAttackDamage * 2 : 0) + upUltBonus;

        Collider2D[] finalHits = Physics2D.OverlapCircleAll(slamCenter, ultimateRadius * 1.15f, enemyLayers);
        foreach (var col in finalHits)
        {
            if (col == null || col.gameObject == gameObject) continue;
            var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (dmg != null && !(dmg is PlayerHealth))
            {
                dmg.TakeDamage(cataclysmDamage);
                DamagePopup.Create(col.transform.position, cataclysmDamage, isCritical: true);

                // Choáng & Hất tung
                var rb = col.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 burstDir = ((Vector2)col.transform.position - (Vector2)slamCenter).normalized;
                    if (burstDir.sqrMagnitude < 0.01f) burstDir = Vector2.up;
                    rb.AddForce(burstDir * 11.0f, ForceMode2D.Impulse);
                }
            }
        }

        AddBerserkRage(MAX_BERSERK_RAGE); // Kích hoạt ngay khiên hộ thân

        yield return new WaitForSeconds(0.25f);

        if (playerHealth != null) playerHealth.isInvulnerable = false;
        if (playerController != null) playerController.SetCanMove(true);

        isExecutingCataclysm = false;
    }

    /// <summary>
    /// Giảm thời gian hồi Tuyệt Kỹ khi Dash (-2.0s)
    /// </summary>
    public void ReduceSpecialCooldown(float seconds)
    {
        if (specialCooldownTimer > 0f)
        {
            specialCooldownTimer = Mathf.Max(0f, specialCooldownTimer - seconds);
            DamagePopup.CreateText(transform.position + Vector3.up * 1.3f, LocalizationManager.T($"⚡ -{seconds:0.#}s HỒI CHIÊU!", $"⚡ -{seconds:0.#}s CD!"), new Color(1f, 0.6f, 0.2f));
        }
    }
}
