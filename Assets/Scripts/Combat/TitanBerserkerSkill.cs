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
    public float attackCooldown = 0.42f;
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
    public bool IsExecutingCataclysm => isExecutingCataclysm;

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

    // Earthshaker Charging State (Bất tử & Choáng 1s)
    private bool isChargingEarthshaker = false;
    private float currentChargeDuration = 0f;
    private Vector2 currentAimDir = Vector2.down;
    private GameObject chargeAuraObj = null;
    private const float MAX_CHARGE_TIME = 0.65f;
    private const float MIN_CHARGE_TIME = 0.22f;

    [Header("--- EARTHSHAKER CHARGE COOLDOWN ---")]
    [SerializeField] private float minChargeCooldown = 5.0f; // CD 5s khi gồng ngắn
    [SerializeField] private float maxChargeCooldown = 10.0f; // CD 10s khi gồng max công lực
    private float chargeCooldownTimer = 0f;
    private float lastChargeCooldownMax = 5.0f;

    public bool CanUseEarthshakerCharge => chargeCooldownTimer <= 0f;
    public float ChargeCooldownRemaining => Mathf.Max(0f, chargeCooldownTimer);
    public float ChargeCooldownRatio => (lastChargeCooldownMax > 0f) ? Mathf.Clamp01(chargeCooldownTimer / lastChargeCooldownMax) : 0f;

    public bool IsChargingEarthshaker => isChargingEarthshaker;
    public float CurrentChargeRatio => Mathf.Clamp01((currentChargeDuration - MIN_CHARGE_TIME) / (MAX_CHARGE_TIME - MIN_CHARGE_TIME));

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

        IceSlamVFX.EnsureFramesLoaded();
    }

    [Header("--- CHARGE POSE SPRITE ---")]
    public Sprite chargePoseSprite = null;

    private Sprite GetChargeSprite()
    {
        if (chargePoseSprite != null) return chargePoseSprite;

#if UNITY_EDITOR
        var objs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player4/Hammer_Attack.png");
        if (objs != null)
        {
            foreach (var o in objs)
            {
                if (o is Sprite s && s.name == "Hammer_Attack_0_0")
                {
                    chargePoseSprite = s;
                    return s;
                }
            }
            System.Collections.Generic.List<Sprite> list = new System.Collections.Generic.List<Sprite>();
            foreach (var o in objs) { if (o is Sprite s) list.Add(s); }
            var sorted = System.Linq.Enumerable.ToList(System.Linq.Enumerable.ThenBy(System.Linq.Enumerable.OrderByDescending(list, s => s.rect.y), s => s.rect.x));
            if (sorted.Count > 0)
            {
                chargePoseSprite = sorted[0];
                return chargePoseSprite;
            }
        }
#endif
        return chargePoseSprite;
    }

    void Update()
    {
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;
        if (specialCooldownTimer > 0f) specialCooldownTimer -= Time.deltaTime;
        if (chargeCooldownTimer > 0f) chargeCooldownTimer -= Time.deltaTime;
    }

    void LateUpdate()
    {
        if (isChargingEarthshaker && spriteRenderer != null)
        {
            var s = GetChargeSprite();
            if (s != null)
            {
                spriteRenderer.sprite = s;
                spriteRenderer.flipX = (currentAimDir.x < -0.05f);
            }
        }
    }

    public bool IsAttackReady => attackCooldownTimer <= 0f && !isExecutingCataclysm && !isSpinning;

    public Vector3 GetAimCenter()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            return new Vector3(transform.position.x, col.bounds.center.y, transform.position.z);
        }
        return transform.position + new Vector3(0f, 0.45f, 0f);
    }

    // =========================================================================
    // HỆ THỐNG GỒNG ĐỊA CHẤN: MIỄN NHIỄM SÁT THƯƠNG & CHOÁNG + NỆN ĐẤT CHOÁNG 1s
    // =========================================================================

    public void StartEarthshakerCharge(Vector2 initialAim)
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (isExecutingCataclysm || isSpinning || !CanUseEarthshakerCharge) return;

        isChargingEarthshaker = true;
        currentChargeDuration = 0f;
        currentAimDir = (initialAim.sqrMagnitude > 0.001f) ? initialAim.normalized : Vector2.down;

        if (playerController != null)
        {
            playerController.SetFacingDirection(currentAimDir);
            playerController.SetCanMove(false);
        }

        // BẬT TRẠNG THÁI BẤT TỬ & MIỄN NHIỄM TOÀN BỘ SÁT THƯƠNG + CHOÁNG
        if (playerHealth != null)
        {
            playerHealth.isInvulnerable = true;
        }

        // TẮT TẠM THỜI ANIMATOR ĐỂ KHÔNG BỊ VỆT CHÉM ĐÈ LÊN, GÁN THẲNG SPRITE GIƠ BÚA
        if (animator != null)
        {
            animator.enabled = false;
        }

        var s = GetChargeSprite();
        if (spriteRenderer != null && s != null)
        {
            spriteRenderer.sprite = s;
            spriteRenderer.flipX = (currentAimDir.x < -0.05f);
        }

        CreateChargeAura();
        SoundManager.EnsureInstance()?.PlayHitImpact();
    }

    public void UpdateEarthshakerCharge(Vector2 aimDir, float holdDuration)
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            CancelEarthshakerCharge();
            return;
        }

        if (!isChargingEarthshaker)
        {
            if (!IsAttackReady) return;
            if (!CanUseEarthshakerCharge)
            {
                // Đang trong thời gian hồi chiêu gồng (5s - 10s) -> chuyển sang combo đánh thường
                int bonusAtk = (playerCombat != null) ? playerCombat.BonusAttackDamage : 0;
                OnPerformAttack(aimDir, bonusAtk);
                return;
            }
            var energyCheck = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (energyCheck != null && energyCheck.GetEnergy() < 3f)
            {
                // Không đủ năng lượng để gồng -> đánh thường
                int bonusAtk = (playerCombat != null) ? playerCombat.BonusAttackDamage : 0;
                OnPerformAttack(aimDir, bonusAtk);
                return;
            }
            StartEarthshakerCharge(aimDir);
        }

        if (!isChargingEarthshaker) return;

        currentChargeDuration = holdDuration;
        if (aimDir.sqrMagnitude > 0.001f)
        {
            currentAimDir = aimDir.normalized;
            if (playerController != null) playerController.SetFacingDirection(currentAimDir);
        }

        var s = GetChargeSprite();
        if (spriteRenderer != null && s != null)
        {
            spriteRenderer.sprite = s;
            spriteRenderer.flipX = (currentAimDir.x < -0.05f);
        }

        float chargeSpan = MAX_CHARGE_TIME - MIN_CHARGE_TIME;
        float ratio = Mathf.Clamp01((currentChargeDuration - MIN_CHARGE_TIME) / (chargeSpan > 0.01f ? chargeSpan : 0.43f));

        // TIÊU HAO NĂNG LƯỢNG LIÊN TỤC KHI GỒNG: Gồng càng lâu tốc độ trừ năng lượng càng tăng!
        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null)
        {
            float drainRate = Mathf.Lerp(15f, 38f, ratio);
            float drainAmount = drainRate * Time.deltaTime;

            bool hasEnergy = energySys.TryConsumeEnergy(drainAmount);
            if (!hasEnergy || energySys.GetEnergy() <= 0.5f)
            {
                // HẾT NĂNG LƯỢNG KHI ĐANG GỒNG -> TỰ ĐỘNG NỆN BÚA XUỐNG NGAY LẬP TỨC!
                int bonusAtk = (playerCombat != null) ? playerCombat.BonusAttackDamage : 0;
                ReleaseEarthshakerCharge(currentAimDir, bonusAtk);
                return;
            }
        }

        // CHỈ HIỂN THỊ VÙNG NGẮM & AURA KHI GIỮ ĐỦ LÂU (TRÁNH HIỆN VÙNG NGẮM KHI TAP / DOUBLE TAP NHANH)
        if (currentChargeDuration >= MIN_CHARGE_TIME)
        {
            Color aimColor = Color.Lerp(new Color(0.2f, 0.9f, 1f, 0.95f), new Color(0.9f, 1f, 1f, 1f), ratio);
            float lineLength = Mathf.Lerp(3.5f, 5.2f, ratio);
            float lineWidth = Mathf.Lerp(0.20f, 0.36f, ratio);
            float startOffset = 0.48f; // Xuất phát chuẩn từ rìa thân nhân vật Minotaur
            Vector3 aimCenter = GetAimCenter();
            SkillAimIndicator.EnsureExists(gameObject)?.ShowAimLine((Vector2)aimCenter, currentAimDir, lineLength, aimColor, lineWidth, startOffset);

            UpdateChargeAura(ratio);
        }
        else
        {
            SkillAimIndicator.Instance?.Hide();
        }

        // Rung nhẹ màn hình tăng dần khi gồng lâu
        if (ratio >= 0.5f && Random.value > 0.7f)
        {
            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.04f, 0.05f);
        }
    }

    public void CancelEarthshakerCharge()
    {
        if (!isChargingEarthshaker) return;
        isChargingEarthshaker = false;
        currentChargeDuration = 0f;
        if (playerHealth != null) playerHealth.isInvulnerable = false;
        if (playerController != null) playerController.SetCanMove(true);

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool("IsCharging", false);
        }

        DestroyChargeAura();
        SkillAimIndicator.Instance?.Hide();
    }

    public void ReleaseEarthshakerCharge(Vector2 aimDir, int bonusAtkDamage)
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            CancelEarthshakerCharge();
            return;
        }

        if (SkillAimIndicator.Instance != null) SkillAimIndicator.Instance.Hide();
        DestroyChargeAura();

        if (aimDir.sqrMagnitude > 0.001f) currentAimDir = aimDir.normalized;

        float holdTime = currentChargeDuration;
        bool wasCharging = isChargingEarthshaker;
        isChargingEarthshaker = false;
        currentChargeDuration = 0f;

        // Tắt bất tử & hồi lại di chuyển
        if (playerHealth != null) playerHealth.isInvulnerable = false;
        if (playerController != null) playerController.SetCanMove(true);

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool("IsCharging", false);
        }

        if (!wasCharging || holdTime < MIN_CHARGE_TIME)
        {
            OnPerformAttack(currentAimDir, bonusAtkDamage);
            return;
        }

        ExecuteEarthshakerSmash(currentAimDir, holdTime, bonusAtkDamage);
    }

    private void ExecuteEarthshakerSmash(Vector2 facingDir, float chargeTime, int bonusAtkDamage)
    {
        attackCooldownTimer = 0.95f;
        float ratio = Mathf.Clamp01(chargeTime / MAX_CHARGE_TIME);

        // Đặt Cooldown cho đòn gồng: Gồng càng lâu CD càng dài (5s -> 10s)
        lastChargeCooldownMax = Mathf.Lerp(minChargeCooldown, maxChargeCooldown, ratio);
        chargeCooldownTimer = lastChargeCooldownMax;

        if (facingDir.sqrMagnitude < 0.001f) facingDir = Vector2.down;
        facingDir = facingDir.normalized;

        if (playerController != null) playerController.SetFacingDirection(facingDir);

        if (animator != null)
        {
            animator.SetBool("IsCharging", false);
            animator.SetTrigger("AttackTrigger");
            animator.Play("Attack2", 0, 0f);
        }

        SoundManager.EnsureInstance()?.PlayTitanSlam();

        // Rung màn hình cực mạnh
        float shakeAmt = Mathf.Lerp(0.22f, 0.45f, ratio);
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(shakeAmt, 0.35f);

        // Vùng nện đất hình nón / chấn địa phía trước
        float range = Mathf.Lerp(3.0f, 4.6f, ratio);
        Vector3 smashCenter = GetAimCenter();
        Vector2 origin = (Vector2)smashCenter + facingDir * 0.8f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, range, enemyLayers);

        float dmgMult = Mathf.Lerp(1.8f, 2.85f, ratio);
        int totalDamage = Mathf.RoundToInt((baseHammerDamage + bonusAtkDamage) * dmgMult);

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

            Vector2 toTarget = ((Vector2)col.transform.position - (Vector2)smashCenter).normalized;
            float dot = Vector2.Dot(facingDir, toTarget);

            if (dot >= 0.15f || Vector2.Distance(smashCenter, col.transform.position) <= 1.5f)
            {
                int actualDmg = AugmentManager.ProcessOutgoingDamage(gameObject, col.gameObject, totalDamage);
                damageable.TakeDamage(actualDmg);
                bool isMaxCharge = ratio >= 0.90f;
                DamagePopup.Create(col.transform.position, actualDmg, isMaxCharge ? DamageType.TrueDamage : DamageType.Physical, isCritical: true);
                AugmentManager.TriggerOnHitAugments(gameObject, col.gameObject, actualDmg, damageable);

                bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null || col.GetComponent<BoneEggObstacle>() != null || col.GetComponentInParent<BoneEggObstacle>() != null);
                
                if (!isProp)
                {
                    // GÂY ĐÓNG BĂNG HÓA XANH TOÀN THÂN CHO QUÁI VÀ BOSS (Đứng hình, không dùng vòng sao choáng)
                    float freezeDuration = Mathf.Lerp(1.2f, 1.8f, ratio);
                    EnemyFreezeEffect.ApplyFreeze(rootObj, freezeDuration);

                    // Hất tung nhẹ kẻ địch
                    var rb = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 pushDir = ((Vector2)col.transform.position - (Vector2)smashCenter).normalized;
                        rb.AddForce(pushDir * 10f, ForceMode2D.Impulse);
                    }

                    hitEnemyCount++;
                }
            }
        }

        // Tăng Nộ Berserk Rage & Hồi phục Năng Lượng / Nội Năng khi nện trúng quái
        if (hitEnemyCount > 0)
        {
            AddBerserkRage(35f + hitEnemyCount * 5f);
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (energySys != null)
            {
                energySys.RestoreEnergy(hitEnemyCount * 10f);
                energySys.AddInnerEnergy(hitEnemyCount * 12f);
            }

            // Hồi phục Máu từ Ngọc Huyết Thạch / Lifesteal khi nện đòn gồng trúng quái
            if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null)
            {
                int heal = Mathf.Max(1, Mathf.RoundToInt(totalDamage * playerCombat.LifestealPercent));
                playerHealth.Heal(heal);
            }
        }

        // TẠO CÁC CỌC BĂNG ĐỊA CHẤN LAN TỎA (Ice VFX thay cho vết nứt cũ)
        IceSlamVFX.Spawn(smashCenter, facingDir, range, ratio);
    }

    private void SpawnGroundFissure(Vector3 origin, Vector2 dir, float length, float duration)
    {
        StartCoroutine(GroundFissureRoutine(origin, dir, length, duration));
    }

    private IEnumerator GroundFissureRoutine(Vector3 origin, Vector2 dir, float length, float duration)
    {
        GameObject fissureObj = new GameObject("Titan_GroundFissure");
        fissureObj.transform.position = origin;

        var light = fissureObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = new Color(1f, 0.45f, 0.1f, 1f);
        light.intensity = 2.4f;
        light.pointLightInnerRadius = 0.5f;
        light.pointLightOuterRadius = length * 0.9f;

        // Vẽ các nhánh đường nứt đất bằng LineRenderer
        List<LineRenderer> crackLines = new List<LineRenderer>();
        Material lineMat = new Material(Shader.Find("Sprites/Default"));

        // Nhánh chính giữa
        crackLines.Add(CreateCrackLine(fissureObj, lineMat, origin, dir, length, 0.22f));
        // Nhánh nứt rẽ trái
        Vector2 leftDir = Quaternion.Euler(0, 0, 28f) * dir;
        crackLines.Add(CreateCrackLine(fissureObj, lineMat, origin + (Vector3)dir * 0.4f, leftDir, length * 0.7f, 0.14f));
        // Nhánh nứt rẽ phải
        Vector2 rightDir = Quaternion.Euler(0, 0, -28f) * dir;
        crackLines.Add(CreateCrackLine(fissureObj, lineMat, origin + (Vector3)dir * 0.4f, rightDir, length * 0.7f, 0.14f));

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float lifeRatio = elapsed / duration;

            // Nhấp nháy dung nham phát sáng trong lòng đất
            float pulse = 1f + Mathf.Sin(elapsed * 18f) * 0.15f;
            float alpha = (lifeRatio < 0.7f) ? 1f : Mathf.Lerp(1f, 0f, (lifeRatio - 0.7f) / 0.3f);

            light.intensity = 2.4f * pulse * alpha;

            Color coreColor = new Color(1f, 0.88f, 0.25f, alpha);
            Color edgeColor = new Color(0.95f, 0.28f, 0.05f, alpha * 0.85f);

            foreach (var lr in crackLines)
            {
                if (lr != null)
                {
                    lr.startColor = coreColor;
                    lr.endColor = edgeColor;
                }
            }

            yield return null;
        }

        Destroy(fissureObj);
    }

    private LineRenderer CreateCrackLine(GameObject parent, Material mat, Vector3 start, Vector2 dir, float length, float width)
    {
        GameObject lineGo = new GameObject("CrackBranch");
        lineGo.transform.SetParent(parent.transform, true);

        var lr = lineGo.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.useWorldSpace = true;
        lr.startWidth = width;
        lr.endWidth = width * 0.3f;
        lr.sortingLayerName = "Default";
        lr.sortingOrder = -5; // Nằm sát mặt đất dưới chân nhân vật và quái

        // Tạo 6 điểm gãy khúc ziczac ngẫu nhiên mô phỏng vết nứt đá tự nhiên
        int segments = 6;
        lr.positionCount = segments;
        Vector3 currentPoint = start;
        lr.SetPosition(0, currentPoint);

        Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
        float segLen = length / (segments - 1);

        for (int i = 1; i < segments; i++)
        {
            float jitter = (i == segments - 1) ? 0f : Random.Range(-0.25f, 0.25f);
            currentPoint = start + (Vector3)(dir * (segLen * i)) + (Vector3)(perp * jitter);
            lr.SetPosition(i, currentPoint);
        }

        return lr;
    }

    private void CreateChargeAura()
    {
        if (chargeAuraObj != null) Destroy(chargeAuraObj);

        chargeAuraObj = new GameObject("Titan_ChargeAura");
        chargeAuraObj.transform.SetParent(transform, false);
        chargeAuraObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);

        var light = chargeAuraObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = new Color(1f, 0.45f, 0.1f, 1f);
        light.intensity = 1.6f;
        light.pointLightInnerRadius = 0.4f;
        light.pointLightOuterRadius = 2.0f;
    }

    private void UpdateChargeAura(float ratio)
    {
        if (chargeAuraObj == null) return;
        var light = chargeAuraObj.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
        if (light != null)
        {
            light.intensity = Mathf.Lerp(1.6f, 3.8f, ratio);
            light.pointLightOuterRadius = Mathf.Lerp(2.0f, 3.2f, ratio);
            light.color = Color.Lerp(new Color(1f, 0.55f, 0.15f, 1f), new Color(1f, 0.2f, 0.05f, 1f), ratio);
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

    /// <summary>
    /// Đòn đánh thường: Combo 4 Đòn (Chém 1 -> Chém 2 -> Húc Búa 3 -> Xoay Búa Lốc Xoáy 4 Tự Do Di Chuyển)
    /// </summary>
    public bool OnPerformAttack(Vector2 facingDir, int bonusAtkDamage)
    {
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (!IsAttackReady) return false;

        if (Time.time - lastComboTime > 1.25f)
        {
            comboStep = 0;
        }

        comboStep++;
        lastComboTime = Time.time;

        if (facingDir.sqrMagnitude < 0.001f) facingDir = Vector2.down;
        facingDir = facingDir.normalized;

        if (playerController != null)
        {
            playerController.SetFacingDirection(facingDir);
        }

        if (comboStep < 4)
        {
            attackCooldownTimer = attackCooldown;
            if (animator != null)
            {
                animator.enabled = true;
                animator.SetFloat("MoveX", facingDir.x);
                animator.SetFloat("MoveY", facingDir.y);
                animator.SetInteger("ComboStep", comboStep);
                animator.SetTrigger("AttackTrigger");
                string stateName = (comboStep == 3) ? "Attack2" : "Attack1";
                animator.Play(stateName, 0, 0f);
            }

            StartCoroutine(HammerSmashRoutine(facingDir, bonusAtkDamage, isBash: (comboStep == 3)));
        }
        else
        {
            // Đòn thứ 4: CƠN LỐC CUỒNG NỘ (SPIN WHIRLWIND 360°) - XOAY 1 VÒNG CÀN QUÉT KHỚP SOUND (0.75s)
            comboStep = 0;
            attackCooldownTimer = 0.85f;
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
                int actualDmg = AugmentManager.ProcessOutgoingDamage(gameObject, col.gameObject, totalDmg);
                damageable.TakeDamage(actualDmg);
                DamagePopup.Create(col.transform.position, actualDmg, DamageType.Physical, isCritical: isBash || Random.value < 0.25f);
                AugmentManager.TriggerOnHitAugments(gameObject, col.gameObject, actualDmg, damageable);

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
    /// Đòn thứ 4: Cơn Lốc Cuồng Nộ Xoay Búa 360° - Xoay quét dứt khoát khớp chuẩn âm thanh (0.75s)
    /// </summary>
    private IEnumerator SpinWhirlwindRoutine(int bonusAtk)
    {
        isSpinning = true;

        if (animator != null)
        {
            animator.SetTrigger("SpinTrigger");
            animator.Play("Spin", 0, 0f);
        }

        SoundManager.EnsureInstance()?.PlaySwordSlash();

        float spinDuration = 0.75f;
        float tickInterval = 0.25f;
        float elapsed = 0f;
        float spinRadius = 2.4f;

        while (elapsed < spinDuration)
        {
            elapsed += tickInterval;

            Vector3 center = (playerCombat != null) ? playerCombat.GetBodyCenterWorld() : (transform.position + Vector3.up * 0.65f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, spinRadius, enemyLayers);

            bool isLastTick = elapsed >= spinDuration;
            int tickDmg = Mathf.RoundToInt((baseHammerDamage * 0.55f) + (bonusAtk * 0.4f));
            if (isLastTick)
            {
                tickDmg = Mathf.RoundToInt(baseHammerDamage * 1.35f + bonusAtk);
                SoundManager.EnsureInstance()?.PlayHitImpact();
            }

            int hitCount = 0;
            foreach (var col in hits)
            {
                if (col == null || col.gameObject == gameObject) continue;

                var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (damageable != null && !(damageable is PlayerHealth))
                {
                    hitCount++;
                    damageable.TakeDamage(tickDmg);
                    DamagePopup.Create(col.transform.position, tickDmg, DamageType.Physical, isCritical: isLastTick || Random.value < 0.3f);

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
                if (cam != null) cam.Shake(isLastTick ? 0.22f : 0.08f, 0.10f);
            }

            yield return new WaitForSeconds(tickInterval);
        }

        // Hết vòng xoay & hết sound -> Ngắt ngay hoạt ảnh và trả về trạng thái bình thường
        isSpinning = false;
        if (animator != null)
        {
            animator.ResetTrigger("SpinTrigger");
            animator.Play("Idle", 0, 0f);
        }
    }

    [Header("--- SPIN SPRITES ---")]
    private Sprite[] spinSpritesCache = null;

    private Sprite[] GetSpinSprites()
    {
        if (spinSpritesCache != null && spinSpritesCache.Length > 0) return spinSpritesCache;

#if UNITY_EDITOR
        var objs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player4/Hammer_Spin.png");
        if (objs != null)
        {
            var list = new System.Collections.Generic.List<Sprite>();
            foreach (var o in objs)
            {
                if (o is Sprite s) list.Add(s);
            }
            var sorted = System.Linq.Enumerable.ToList(System.Linq.Enumerable.ThenBy(System.Linq.Enumerable.OrderByDescending(list, s => s.rect.y), s => s.rect.x));
            if (sorted.Count > 0)
            {
                spinSpritesCache = sorted.ToArray();
                return spinSpritesCache;
            }
        }
#endif
        return spinSpritesCache;
    }

    /// <summary>
    /// Kích hoạt Cuồng Phong Xoay Rìu 360° khi Lướt (Dash Spin như chiêu E của Tryndamere)
    /// Cho phép vừa lướt tốc độ cao vừa xoay tròn chém càn quét toàn bộ quái vật trên đường lướt!
    /// </summary>
    public void TriggerDashSpin()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        StartCoroutine(DashSpinRoutine());
    }

    private IEnumerator DashSpinRoutine()
    {
        isSpinning = true;

        // Tạm thời ngắt Animator để điều khiển trực tiếp từng frame xoay tròn 360 độ từ Hammer_Spin
        if (animator != null)
        {
            animator.enabled = false;
        }

        SoundManager.EnsureInstance()?.PlaySwordSlash();

        float spinRadius = 2.4f;
        int bonusAtk = (playerCombat != null) ? playerCombat.BonusAttackDamage : 0;
        int spinDmg = Mathf.RoundToInt(baseHammerDamage * 0.95f + bonusAtk);

        HashSet<Collider2D> hitEnemies = new HashSet<Collider2D>();

        // Hàm quét và gây sát thương lập tức cho kẻ địch
        System.Action DoHitScan = () =>
        {
            Vector3 center = (playerCombat != null) ? playerCombat.GetBodyCenterWorld() : (transform.position + Vector3.up * 0.65f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, spinRadius, enemyLayers);

            foreach (var col in hits)
            {
                if (col == null || col.gameObject == gameObject || hitEnemies.Contains(col)) continue;

                var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (damageable != null && !(damageable is PlayerHealth))
                {
                    hitEnemies.Add(col);
                    damageable.TakeDamage(spinDmg);
                    DamagePopup.Create(col.transform.position, spinDmg, isCritical: Random.value < 0.40f);

                    var rb = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 pushDir = ((Vector2)col.transform.position - (Vector2)center).normalized;
                        if (pushDir.sqrMagnitude < 0.01f) pushDir = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
                        rb.AddForce(pushDir * 9.0f, ForceMode2D.Impulse);
                    }

                    AddBerserkRage(15f);
                    SoundManager.EnsureInstance()?.PlayHitImpact();

                    var cam = Object.FindAnyObjectByType<CameraFollow>();
                    if (cam != null) cam.Shake(0.12f, 0.15f);
                }
            }
        };

        // Lấy danh sách 9 sprite xoay tròn 360 độ theo hướng lướt
        var allSpinSprites = GetSpinSprites();
        Sprite[] activeSpinFrames = null;
        if (allSpinSprites != null && allSpinSprites.Length >= 9)
        {
            Vector2 faceDir = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
            int rowIndex = 0; // 0: Down, 1: Left, 2: Right, 3: Up
            if (Mathf.Abs(faceDir.x) > Mathf.Abs(faceDir.y))
            {
                rowIndex = (faceDir.x < 0) ? 1 : 2;
            }
            else
            {
                rowIndex = (faceDir.y > 0) ? 3 : 0;
            }

            int startIdx = rowIndex * 9;
            if (startIdx + 9 <= allSpinSprites.Length)
            {
                activeSpinFrames = new Sprite[9];
                System.Array.Copy(allSpinSprites, startIdx, activeSpinFrames, 0, 9);
            }
            else
            {
                activeSpinFrames = new Sprite[9];
                System.Array.Copy(allSpinSprites, 0, activeSpinFrames, 0, 9);
            }
        }

        // 1. GÂY SÁT THƯƠNG NGAY LẬP TỨC TRÊN FRAME ĐẦU TIÊN (Frame 0 Instant Hit)
        DoHitScan();

        // 2. XOAY 360 ĐỘ RÕ NÉT & UY LỰC TRONG SUỐT QUÃNG ĐƯỜNG LƯỚT
        float frameDuration = 0.042f; // ~24 FPS (Mỗi frame kéo dài 0.042s giúp thấy rõ từng góc xoay đại chùy)
        float frameTimer = 0f;
        int currentFrameIdx = 0;

        while (playerController != null && playerController.IsDashing())
        {
            if (activeSpinFrames != null && activeSpinFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = activeSpinFrames[currentFrameIdx % activeSpinFrames.Length];
                frameTimer += Time.deltaTime;
                if (frameTimer >= frameDuration)
                {
                    frameTimer = 0f;
                    currentFrameIdx++;
                }
            }

            DoHitScan();
            yield return null;
        }

        // 3. KẾT THÚC LƯỚT: BẬT LẠI ANIMATOR VÀ TRẢ VỀ HOẠT ẢNH DI CHUYỂN / ĐỨNG
        isSpinning = false;
        if (animator != null)
        {
            animator.enabled = true;
            animator.ResetTrigger("SpinTrigger");
            if (playerController != null && playerController.IsMoving)
            {
                animator.Play("Move", 0, 0f);
            }
            else
            {
                animator.Play("Idle", 0, 0f);
            }
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

        // Hất văng nhẹ kẻ địch xung quanh
        Collider2D[] burstHits = Physics2D.OverlapCircleAll(transform.position, 2.5f, enemyLayers);
        foreach (var col in burstHits)
        {
            if (col == null || col.gameObject == gameObject) continue;
            var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (dmg != null && !(dmg is PlayerHealth))
            {
                dmg.TakeDamage(18);
                DamagePopup.Create(col.transform.position, 18, DamageType.Physical, isCritical: false);
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
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (specialCooldownTimer > 0f || isExecutingCataclysm)
        {
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

        // Kích hoạt ngay toàn bộ chuỗi hoạt ảnh Hammer_Skill (Vung đại chùy tích năng lượng -> Nhảy lên -> Nện đất bộc phá)
        if (animator != null)
        {
            animator.SetTrigger("SkillTrigger");
            animator.Play("Skill", 0, 0f);
        }

        SoundManager.EnsureInstance()?.PlayHitImpact();

        // 1. GIAI ĐOẠN TÍCH TỤ & VUNG BÚA LÊN CAO: TÂM CHẤN HÚT KẺ ĐỊCH VÀO TRONG 0.48s
        float pullDuration = 0.48f;
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
                    col.transform.position = Vector3.MoveTowards(col.transform.position, slamCenter, 7.5f * Time.deltaTime);
                }
            }
            yield return null;
        }

        // 2. THỜI ĐIỂM BÚA CHẠM ĐẤT: NỆN ĐẠI CHÙY DUNG NHAM BỘC PHÁ DIỆN RỘNG (CATACLYSM EXPLOSION IMPACT)
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
                DamagePopup.Create(col.transform.position, cataclysmDamage, DamageType.Physical, isCritical: true);
                if (playerCombat != null && playerCombat.BonusTrueDamage > 0)
                {
                    DamagePopup.Create(col.transform.position + Vector3.up * 0.4f, playerCombat.BonusTrueDamage * 2, DamageType.TrueDamage, isCritical: true);
                }

                // Choáng & Hất tung
                var rb = col.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 burstDir = ((Vector2)col.transform.position - (Vector2)slamCenter).normalized;
                    if (burstDir.sqrMagnitude < 0.01f) burstDir = Vector2.up;
                    rb.AddForce(burstDir * 12.0f, ForceMode2D.Impulse);
                }
            }
        }

        AddBerserkRage(MAX_BERSERK_RAGE); // Kích hoạt ngay khiên hộ thân

        // Cơ chế mới: Nhận thêm 50% Máu hiện tại thành 1 lớp Máu Ảo (Barrier) tồn tại trong suốt thời gian của khiên nội năng
        if (playerHealth != null)
        {
            int curHp = playerHealth.CurrentHealth;
            int bonusVirtualHp = Mathf.Max(1, Mathf.RoundToInt(curHp * 0.50f));
            playerHealth.AddVirtualHealth(bonusVirtualHp, duration: 15.0f);
        }

        // Chờ hoạt ảnh vung nện kết thúc trọn vẹn (frame 5 recovery)
        yield return new WaitForSeconds(0.35f);

        if (playerHealth != null) playerHealth.isInvulnerable = false;
        if (playerController != null) playerController.SetCanMove(true);

        isExecutingCataclysm = false;
        if (animator != null)
        {
            animator.Play("Idle", 0, 0f);
        }
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
}
