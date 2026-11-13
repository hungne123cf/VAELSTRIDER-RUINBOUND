using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 5.0f;

    public float MoveSpeed => moveSpeed;

    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 moveInput;
    private Vector2 lastFacingDirection = Vector2.right;
    private float lastHorizontalFacing = 1f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;

    // Hệ thống tích trữ lần lướt (3 Stored Dash Charges) cho Player 5
    private int currentDashCharges = 3;
    private float dashChargeRechargeTimer = 0f;
    private float dashIntervalTimer = 0f;

    private bool isMobileInputActive = false;
    private Vector2 rawMobileInput = Vector2.zero;
    private bool canMove = true;
    private float baseMoveSpeed = 5f;
    private float baseDashCooldown = 5.0f;
    private float bonusSpeed = 0f;
    private float dashCooldownReduction = 0f;

    public float DashDuration => dashDuration;
    public bool IsDuneAssassin => (GetComponent<DuneAssassinSkill>() != null && GetComponent<DuneAssassinSkill>().enabled);
    public int GetCurrentDashCharges() => IsDuneAssassin ? currentDashCharges : (dashCooldownTimer <= 0 ? 1 : 0);
    public int GetMaxDashCharges() => IsDuneAssassin ? 3 : 1;
    public float GetDashChargeRechargeProgress()
    {
        if (!IsDuneAssassin) return (dashCooldownTimer <= 0) ? 1f : (1f - (dashCooldownTimer / dashCooldown));
        if (currentDashCharges >= 3) return 1f;
        float chargeCd = Mathf.Max(1.5f, 3.5f - dashCooldownReduction);
        return Mathf.Clamp01(dashChargeRechargeTimer / chargeCd);
    }

    public void SetBaseSpeed(float newBaseSpeed)
    {
        baseMoveSpeed = newBaseSpeed;
        moveSpeed = baseMoveSpeed + bonusSpeed;
    }

    public void ApplyUpgradeBonus(float speedBonus, float dashCdReduction)
    {
        bonusSpeed = speedBonus;
        dashCooldownReduction = dashCdReduction;
        moveSpeed = baseMoveSpeed + bonusSpeed + augSpeedBonus;
        dashCooldown = Mathf.Max(1.0f, baseDashCooldown - dashCooldownReduction);
    }

    // ─── Augment speed bonus ───────────────────────────────────────────────────
    private float augSpeedBonus = 0f;

    public void ApplyAugmentBonus(float speedBonusFraction)
    {
        augSpeedBonus = baseMoveSpeed * speedBonusFraction;
        moveSpeed = baseMoveSpeed + bonusSpeed + augSpeedBonus;
    }


    public bool IsStunned => isStunned || stunTimerRemaining > 0f;
    public float StunTimerRemaining => Mathf.Max(0f, stunTimerRemaining);
    private bool isStunned = false;
    private float stunTimerRemaining = 0f;
    private float lastStunEndTime = -10f;

    [Header("Golden Sanctuary (Lõi Kim Thân Bất Hoại - Miễn Khống Chế)")]
    private bool hasGoldenSanctuary = false;
    private bool isGoldenImmunityReady = false;
    private float goldenImmunityCooldownTimer = 0f;
    private const float GOLDEN_IMMUNITY_COOLDOWN = 10f;
    private GameObject goldenAuraObject = null;
    public const float STUN_IMMUNITY_GRACE_PERIOD = 3.5f;

    public void EnableGoldenSanctuary()
    {
        hasGoldenSanctuary = true;
        isGoldenImmunityReady = true;
        goldenImmunityCooldownTimer = 0f;
        UpdateGoldenAuraVisual();
    }

    public bool TryConsumeGoldenCCImmunity()
    {
        if (hasGoldenSanctuary && isGoldenImmunityReady)
        {
            isGoldenImmunityReady = false;
            goldenImmunityCooldownTimer = GOLDEN_IMMUNITY_COOLDOWN;
            UpdateGoldenAuraVisual();

            DamagePopup.CreateText(
                transform.position + Vector3.up * 1.5f,
                LocalizationManager.T("KIM THÂN: MIỄN KHỐNG CHẾ!", "GOLDEN IMMUNITY: CC BLOCKED!"),
                new Color(1f, 0.88f, 0.20f)
            );
            SoundManager.EnsureInstance()?.PlayHeavenlyAscension();
            return true;
        }
        return false;
    }

    public Vector3 GetBodyCenterWorld()
    {
        var combat = GetComponent<PlayerCombat>();
        if (combat != null) return combat.GetBodyCenterWorld();
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null) return sr.bounds.center;
        var col = GetComponent<Collider2D>();
        if (col != null) return col.bounds.center;
        return transform.position + Vector3.up * 1.0f;
    }

    private void UpdateGoldenAuraVisual()
    {
        if (!hasGoldenSanctuary)
        {
            if (goldenAuraObject != null) goldenAuraObject.SetActive(false);
            return;
        }

        if (isGoldenImmunityReady)
        {
            if (goldenAuraObject == null)
            {
                goldenAuraObject = new GameObject("GoldenSanctuaryAura");
                goldenAuraObject.transform.SetParent(transform, false);
                var sr = goldenAuraObject.AddComponent<SpriteRenderer>();
                sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
                sr.color = new Color(1f, 0.85f, 0.18f, 0.40f);
                sr.sortingOrder = 5400;
            }
            Vector3 localCenter = GetBodyCenterWorld() - transform.position;
            goldenAuraObject.transform.localPosition = localCenter;
            goldenAuraObject.SetActive(true);
        }
        else
        {
            if (goldenAuraObject != null) goldenAuraObject.SetActive(false);
        }
    } // Thời gian miễn nhiễm choáng sau khi hồi phục để chống perma-stun

    public void ApplyStun(float duration)
    {
        if (TryConsumeGoldenCCImmunity()) return;
        var ph = GetComponent<PlayerHealth>();
        if (ph != null && ph.isInvulnerable) return;

        var titan = GetComponent<TitanBerserkerSkill>();
        if (titan != null && titan.IsChargingEarthshaker) return;

        var mech = GetComponent<AbyssalMechSkill>();
        if (mech != null && mech.IsChargingVortex) return; // Miễn nhiễm choáng khi cơ giáp đang cắm neo gồng bọt nước

        var combat = GetComponent<PlayerCombat>();
        if (combat != null && combat.IsParrying) return;

        // Cơ chế Kháng Choáng & Chống Choáng Liên Tục (Anti-Perma-Stun)
        if (Time.time < lastStunEndTime + STUN_IMMUNITY_GRACE_PERIOD)
        {
            ApplySlow(0.75f, 0.8f);
            DamagePopup.CreateText(transform.position + Vector3.up * 1.4f, 
                LocalizationManager.T("KHÁNG CHOÁNG!", "STUN RESIST!"), 
                new Color(0.35f, 0.85f, 1f));
            return;
        }

        // Giới hạn thời gian 1 lần choáng tối đa 0.85s để người chơi luôn có cửa phản xạ
        duration = Mathf.Min(duration, 0.85f);
        StartCoroutine(StunRoutine(duration));
    }

    private System.Collections.IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        stunTimerRemaining = Mathf.Max(stunTimerRemaining, duration);
        canMove = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetFloat(SpeedHash, 0f);

        SoundManager.EnsureInstance()?.PlayHitImpact();

        GameObject webTrap = new GameObject("WebTrapVisual");
        webTrap.transform.SetParent(transform, false);
        webTrap.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        var srWeb = webTrap.AddComponent<SpriteRenderer>();

        var trapSprites = SpiderWebSpriteFactory.GetTrapSprites();
        Sprite obsSpr = (trapSprites != null && trapSprites.Length > 0) ? trapSprites[0] : null;
        if (obsSpr == null) obsSpr = Resources.Load<Sprite>("SpiderWeb/SpiderWeb_Trap_0");
        if (obsSpr == null) obsSpr = Resources.Load<Sprite>("Enemies/Map3/SkeletonKing_Obstacle_0");
#if UNITY_EDITOR
        if (obsSpr == null)
        {
            obsSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/SpiderWeb/SpiderWeb_Trap_0.png");
            if (obsSpr == null) obsSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Bosses/Skeleton King/SkeletonKing_Obstacle_0.png");
        }
#endif
        if (obsSpr == null) obsSpr = MagmaCalamityHazardManager.GetSoftCircleSprite();

        srWeb.sprite = obsSpr;
        srWeb.color = Color.white;
        srWeb.sortingOrder = 3200;
        webTrap.transform.localScale = new Vector3(1.25f, 1.25f, 1f);

        float animTimer = 0f;
        int frameIdx = 0;

        while (stunTimerRemaining > 0f)
        {
            stunTimerRemaining -= Time.deltaTime;
            animTimer += Time.deltaTime;
            if (animTimer >= 0.12f && trapSprites != null && trapSprites.Length > 0)
            {
                animTimer = 0f;
                frameIdx = (frameIdx + 1) % trapSprites.Length;
                if (srWeb != null && trapSprites[frameIdx] != null)
                {
                    srWeb.sprite = trapSprites[frameIdx];
                }
            }
            yield return null;
        }

        // Vỡ kén nhện khi hết choáng
        SpiderWebSpriteFactory.SpawnWebBreakVFX(transform.position);

        if (webTrap != null) Destroy(webTrap);
        isStunned = false;
        canMove = true;
        lastStunEndTime = Time.time;
    }

    public void ApplySlow(float slowMultiplier, float duration)
    {
        StartCoroutine(SlowRoutine(slowMultiplier, duration));
    }

    private System.Collections.IEnumerator SlowRoutine(float multiplier, float duration)
    {
        moveSpeed = (baseMoveSpeed + bonusSpeed) * multiplier;
        yield return new WaitForSeconds(duration);
        moveSpeed = baseMoveSpeed + bonusSpeed;
    }

    public void ApplyFreeze(float duration)
    {
        if (TryConsumeGoldenCCImmunity()) return;
        if (isStunned || Time.time < lastStunEndTime + STUN_IMMUNITY_GRACE_PERIOD)
        {
            ApplySlow(0.6f, 1.0f);
            return;
        }
        duration = Mathf.Min(duration, 0.85f);
        StartCoroutine(FreezeRoutine(duration));
    }

    private System.Collections.IEnumerator FreezeRoutine(float duration)
    {
        isStunned = true;
        stunTimerRemaining = Mathf.Max(stunTimerRemaining, duration);
        canMove = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetFloat(SpeedHash, 0f);

        GameObject iceBlock = new GameObject("GlacialFreezeVisual");
        iceBlock.transform.SetParent(transform, false);
        iceBlock.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        var srIce = iceBlock.AddComponent<SpriteRenderer>();
        srIce.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        srIce.color = new Color(0.35f, 0.85f, 1f, 0.75f);
        srIce.sortingOrder = 5500;
        iceBlock.transform.localScale = new Vector3(1.2f, 1.4f, 1f);

        DamagePopup.CreateText(transform.position + Vector3.up * 1.2f, LocalizationManager.T("ĐÓNG BĂNG! (0.5s)", "FROZEN! (0.5s)"), new Color(0.35f, 0.85f, 1f));
        SoundManager.EnsureInstance()?.PlayHitImpact();

        while (stunTimerRemaining > 0f)
        {
            stunTimerRemaining -= Time.deltaTime;
            yield return null;
        }

        if (iceBlock != null) Destroy(iceBlock);
        isStunned = false;
        canMove = true;
        lastStunEndTime = Time.time;
    }

    // Animator Hashes
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        animator = GetComponent<Animator>();

        // Tự động nạp tốc độ cơ bản chuẩn theo CharacterData của tướng đã chọn (Dune: 4.8, Ronin: 5.4, Titan: 4.0, ...)
        var charMgr = CharacterManager.EnsureInstance();
        var charData = charMgr != null ? charMgr.GetSelectedCharacter() : null;
        if (charData != null && charData.baseSpeed > 0f)
        {
            moveSpeed = charData.baseSpeed;
        }

        baseMoveSpeed = moveSpeed;

        ArmorShopUI.GetEquippedArmorBonus(out int _, out float _, out float armorSpd);
        if (armorSpd > 0)
        {
            baseMoveSpeed += armorSpd;
            moveSpeed += armorSpd;
        }

        string equippedWpnId = PlayerPrefs.GetString("Equipped_Weapon_Id", "");
        if (!string.IsNullOrEmpty(equippedWpnId))
        {
            var def = InventoryItemDatabase.GetItem(equippedWpnId);
            if (def != null && def.speedBonus > 0f)
            {
                baseMoveSpeed += def.speedBonus;
                moveSpeed += def.speedBonus;
            }
        }

        HeroCostumeManager.GetEquippedCostumeBonus(CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "", out _, out _, out _, out float costumeSpd);
        if (costumeSpd > 0)
        {
            baseMoveSpeed += costumeSpd;
            moveSpeed += costumeSpd;
        }

        // Bat buoc dash cooldown mo dau la 5s
        dashCooldown = 5.0f;
        baseDashCooldown = 5.0f;
        if (GetComponent<DashGhostTrail>() == null) gameObject.AddComponent<DashGhostTrail>();
        if (GetComponent<AutoDepthSort>() == null) gameObject.AddComponent<AutoDepthSort>();
        PlayerEquipmentVisual.EnsureExists(gameObject);
        AutoAlignCollider();

        // Khong de Player va Enemy day nhau (loai bo vat ly va cham day nhau gay kho chiu)
        int playerLayer = gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (playerLayer >= 0 && enemyLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        }
    }

    void Start()
    {
        GameAutoBootstrap.ApplySelectedCharacterToPlayer();
        if (AugmentManager.Instance != null && AugmentManager.Instance.HasAugment(AugmentDatabase.AugmentType.GoldenSanctuary))
        {
            EnableGoldenSanctuary();
        }
    }

    public bool IsMovementBlocked()
    {
        var ph = GetComponent<PlayerHealth>();
        if (ph != null && ph.IsDead) return true;

        if (!canMove || isStunned) return true;

        var combat = GetComponent<PlayerCombat>();
        if (combat != null && (combat.IsParrying || combat.IsAttackHolding)) return true;

        var titan = GetComponent<TitanBerserkerSkill>();
        if (titan != null && (titan.IsChargingEarthshaker || titan.IsExecutingCataclysm)) return true;

        // Gale Ronin cho phép vừa di chuyển chậm vừa gồng tụ lực Iaido để tăng tính cơ động, không bị chôn chân

        var azure = GetComponent<AzureBladeSkill>();
        if (azure != null && azure.IsAiming) return true;

        var dune = GetComponent<DuneAssassinSkill>();
        if (dune != null && (dune.IsDefending || dune.IsExecutingSkill || dune.IsAttacking)) return true;

        var mage = GetComponent<CelestialMageSkill>();
        if (mage != null && (mage.IsChargingBarrier || mage.IsExecutingSkill)) return true;

        // Riêng Hải Thần Cơ Giáp (Player 7): Khi đang gồng cắm mỏ neo hoặc đang vung mỏ neo thì đứng tấn bất động, không được di chuyển
        var mech = GetComponent<AbyssalMechSkill>();
        if (mech != null && mech.enabled && (mech.IsChargingVortex || mech.IsAttacking)) return true;

        return false;
    }

    void Update()
    {
        var ph = GetComponent<PlayerHealth>();
        if (ph != null && ph.IsDead) return;

        // Golden Sanctuary (Kim Thân Bất Hoại: đếm ngược 10s & pulse hào quang vàng)
        if (hasGoldenSanctuary && !isGoldenImmunityReady)
        {
            goldenImmunityCooldownTimer -= Time.deltaTime;
            if (goldenImmunityCooldownTimer <= 0f)
            {
                goldenImmunityCooldownTimer = 0f;
                isGoldenImmunityReady = true;
                UpdateGoldenAuraVisual();
                DamagePopup.CreateText(
                    transform.position + Vector3.up * 1.5f,
                    LocalizationManager.T("KIM THÂN SẴN SÀNG!", "GOLDEN SANCTUARY READY!"),
                    new Color(1f, 0.88f, 0.20f)
                );
                SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
            }
        }
        if (goldenAuraObject != null && goldenAuraObject.activeSelf)
        {
            Vector3 localCenter = GetBodyCenterWorld() - transform.position;
            goldenAuraObject.transform.localPosition = localCenter;
            float pulse = 1f + Mathf.PingPong(Time.time * 2f, 0.15f);
            goldenAuraObject.transform.localScale = new Vector3(pulse * 1.8f, pulse * 1.8f, 1f);
        }

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (dashIntervalTimer > 0)
        {
            dashIntervalTimer -= Time.deltaTime;
        }

        if (IsDuneAssassin)
        {
            float chargeCd = Mathf.Max(1.5f, 3.5f - dashCooldownReduction);
            if (currentDashCharges < 3)
            {
                dashChargeRechargeTimer += Time.deltaTime;
                if (dashChargeRechargeTimer >= chargeCd)
                {
                    dashChargeRechargeTimer = 0f;
                    currentDashCharges = Mathf.Min(3, currentDashCharges + 1);
                }
            }
            else
            {
                dashChargeRechargeTimer = 0f;
            }
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
            }
            return;
        }

        bool isBlocked = IsMovementBlocked();

        if (isMobileInputActive && rawMobileInput.sqrMagnitude > 0.01f)
        {
            lastFacingDirection = rawMobileInput.normalized;
            if (Mathf.Abs(rawMobileInput.x) > 0.05f)
            {
                lastHorizontalFacing = Mathf.Sign(rawMobileInput.x);
            }
            moveInput = isBlocked ? Vector2.zero : rawMobileInput.normalized;
        }
        else
        {
            // Ban phim PC WASD / Mui ten
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 rawKbd = new Vector2(h, v);
            if (rawKbd.sqrMagnitude > 0.01f)
            {
                lastFacingDirection = rawKbd.normalized;
                if (Mathf.Abs(rawKbd.x) > 0.05f)
                {
                    lastHorizontalFacing = Mathf.Sign(rawKbd.x);
                }
            }
            moveInput = isBlocked ? Vector2.zero : rawKbd.normalized;
        }

        // Bam Space de Luot (Dash tren PC)
        if (Input.GetKeyDown(KeyCode.Space) && !isBlocked)
        {
            PerformDash();
        }

        // Bam 1, Q, H, E de uong binh mau
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.H))
        {
            var inv = GetComponent<PlayerInventory>();
            if (inv != null) inv.UsePotion();
        }

        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (IsMovementBlocked())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (isDashing)
        {
            var titan = GetComponent<TitanBerserkerSkill>();
            float speed = (titan != null && titan.enabled) ? 13.5f : dashSpeed;
            rb.linearVelocity = lastFacingDirection * speed;
        }
        else
        {
            float curSpeed = moveSpeed;
            var ronin = GetComponent<GaleRoninSkill>();
            if (ronin != null && ronin.IsChargingIaido)
            {
                if (!ronin.CanUseIaidoCharge)
                {
                    ronin.CancelIaidoCharge();
                }
                else
                {
                    curSpeed *= 0.5f;
                }
            }
            rb.linearVelocity = moveInput * curSpeed;
        }
    }

    private SpriteRenderer cachedPlayerSr;

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        Vector3 clamped = SurroundingWaterOceanSystem.ClampToWalkableLand(pos, 0.55f);
        if (Mathf.Abs(pos.x - clamped.x) > 0.005f || Mathf.Abs(pos.y - clamped.y) > 0.005f || Mathf.Abs(pos.z) > 0.001f)
        {
            transform.position = new Vector3(clamped.x, clamped.y, 0f);
            if (rb != null)
            {
                rb.position = new Vector2(clamped.x, clamped.y);
            }
        }

        if (cachedPlayerSr == null) cachedPlayerSr = GetComponent<SpriteRenderer>();
        if (cachedPlayerSr != null && cachedPlayerSr.sortingOrder < 1500)
        {
            cachedPlayerSr.sortingOrder = AutoDepthSort.GetSortingOrderForY(transform.position.y);
        }
    }

    public void RefreshMoveInput()
    {
        if (IsMovementBlocked())
        {
            moveInput = Vector2.zero;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetFloat(SpeedHash, 0f);
            return;
        }

        if (isMobileInputActive && rawMobileInput.sqrMagnitude > 0.01f)
        {
            moveInput = rawMobileInput.normalized;
            lastFacingDirection = rawMobileInput.normalized;
            if (Mathf.Abs(rawMobileInput.x) > 0.05f)
            {
                lastHorizontalFacing = Mathf.Sign(rawMobileInput.x);
            }
        }
        else
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 rawKbd = new Vector2(h, v);
            if (rawKbd.sqrMagnitude > 0.01f)
            {
                moveInput = rawKbd.normalized;
                lastFacingDirection = rawKbd.normalized;
                if (Mathf.Abs(rawKbd.x) > 0.05f)
                {
                    lastHorizontalFacing = Mathf.Sign(rawKbd.x);
                }
            }
            else
            {
                moveInput = Vector2.zero;
            }
        }

        if (rb != null)
        {
            float curSpeed = moveSpeed;
            var ronin = GetComponent<GaleRoninSkill>();
            if (ronin != null && ronin.IsChargingIaido)
            {
                if (!ronin.CanUseIaidoCharge)
                {
                    ronin.CancelIaidoCharge();
                }
                else
                {
                    curSpeed *= 0.5f;
                }
            }
            rb.linearVelocity = moveInput * curSpeed;
        }
        UpdateAnimations();
    }

    public void SetCanMove(bool enable)
    {
        canMove = enable;
        if (!enable)
        {
            moveInput = Vector2.zero;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetFloat(SpeedHash, 0f);
        }
        else
        {
            RefreshMoveInput();
        }
    }

    public void TriggerDashCooldown()
    {
        dashCooldownTimer = Mathf.Max(1.0f, dashCooldown - dashCooldownReduction);
    }

    public void PerformDash()
    {
        // 1. Cơ chế Tàn Ảnh Tốc Biến & Thu Hồi Bóng (Murad S1 Style) cho Phong Lôi Lãng Khách (Player 2)
        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null && ronin.enabled)
        {
            if (ronin.HasActiveShadowClone)
            {
                // Nhịp 2: Bấm lại là lập tức Teleport quay ngược về vị trí chiếc bóng ban đầu (miễn phí năng lượng!)
                ronin.RecallToShadowClone();
                TriggerDashCooldown();
                return;
            }
            else
            {
                // Nhịp 1: Tốc biến 4.8m tới trước & cắm lại bóng tại vị trí xuất phát
                if (dashCooldownTimer > 0f || isDashing) return;
                var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
                if (energySys != null && !energySys.TryConsumeEnergy(15f)) return;

                ronin.PerformTeleportDash(lastFacingDirection);
                return;
            }
        }

        // Cơ chế Tốc Biến Hư Không (Void Blink) cho Pháp Sư Tinh Tú (Player 6)
        var mage = GetComponent<CelestialMageSkill>();
        if (mage != null && mage.enabled)
        {
            if (dashCooldownTimer > 0f || isDashing) return;
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (energySys != null && !energySys.TryConsumeEnergy(15f)) return;

            mage.OnPerformBlink(lastFacingDirection);
            TriggerDashCooldown();
            return;
        }

        bool canDash = false;
        if (IsDuneAssassin)
        {
            canDash = (currentDashCharges > 0 && dashIntervalTimer <= 0f && lastFacingDirection != Vector2.zero && !isDashing);
        }
        else
        {
            canDash = (dashCooldownTimer <= 0 && lastFacingDirection != Vector2.zero && !isDashing);
        }

        if (canDash)
        {
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (!IsDuneAssassin && energySys != null && !energySys.TryConsumeEnergy(20f))
            {
                return; // Khong du nang luong de luot
            }

            isDashing = true;
            SoundManager.EnsureInstance()?.PlayDash();
            AugmentManager.Instance?.OnPlayerDash(transform.position);
            var titan = GetComponent<TitanBerserkerSkill>();
            dashTimer = (titan != null && titan.enabled) ? 0.38f : dashDuration;

            if (IsDuneAssassin)
            {
                currentDashCharges = Mathf.Max(0, currentDashCharges - 1);
                dashIntervalTimer = 0.22f; // Giãn cách ngắn giữa 2 lần lướt liên hoàn

                // TIÊU HAO 4% MÁU TỐI ĐA KHI LƯỚT CHO PLAYER 5 (DUNE ASSASSIN)
                var pHealth = GetComponent<PlayerHealth>();
                if (pHealth != null && !pHealth.IsDead)
                {
                    pHealth.ConsumeHealthPercent(0.04f); // 4% Max HP
                }

                var duneSkill = GetComponent<DuneAssassinSkill>();
                if (duneSkill != null)
                {
                    duneSkill.OnSkillCast();
                    duneSkill.ReduceSpecialCooldown(1.5f);
                }
            }
            else
            {
                dashCooldownTimer = dashCooldown;
            }

            // Hồi ngay đòn đánh thường cho Lam Kiếm Sĩ khi Dash
            var azureBlade = GetComponent<AzureBladeSkill>();
            if (azureBlade != null)
            {
                azureBlade.ResetAttackCooldown();
            }

            // Kích hoạt lốc xoáy chém xoay 360 độ khi Dash cho Cuồng Chiến Binh Titan (Player 4)
            if (titan != null && titan.enabled)
            {
                titan.TriggerDashSpin();
            }

            // Kích hoạt Lướt Sóng Thâm Hải & Vệt Nước Trơn Trượt cho Hải Thần Cơ Giáp (Player 7)
            var mechSkill = GetComponent<AbyssalMechSkill>();
            if (mechSkill != null && mechSkill.enabled)
            {
                mechSkill.OnPerformHydroDash(lastFacingDirection);
            }

            // Kích hoạt Thánh Thuẫn Phản Đòn CHỈ RIÊNG cho Hiệp Sĩ Vaelstrider (Player 1)
            var combat = GetComponent<PlayerCombat>();
            if (combat != null && combat.enabled && (titan == null || !titan.enabled) && (azureBlade == null || !azureBlade.enabled) && (mechSkill == null || !mechSkill.enabled) && !IsDuneAssassin && (mage == null || !mage.enabled))
            {
                combat.TriggerRadiantRiposteDash(lastFacingDirection);
            }
        }
    }

    public void ReduceDashCooldown(float factor)
    {
        dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer * (1f - factor));
    }

    public void PerformDashInDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            SetFacingDirection(direction);
        }
        PerformDash();
    }

    public float GetDashCooldownNormalized()
    {
        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null && ronin.enabled && ronin.HasActiveShadowClone)
        {
            return 1f - ronin.ShadowCloneRatio; // Vòng tròn đếm ngược thời gian hồi bóng 4.5s
        }
        if (IsDuneAssassin)
        {
            if (currentDashCharges >= 3) return 0f;
            float chargeCd = Mathf.Max(1.5f, 3.5f - dashCooldownReduction);
            return (currentDashCharges == 0) ? (1f - (dashChargeRechargeTimer / chargeCd)) : 0f;
        }
        if (dashCooldown <= 0f) return 0f;
        return Mathf.Clamp01(dashCooldownTimer / dashCooldown);
    }

    public float GetDashCooldownRemaining()
    {
        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null && ronin.enabled && ronin.HasActiveShadowClone)
        {
            return ronin.ShadowCloneTimeRemaining;
        }
        if (IsDuneAssassin)
        {
            if (currentDashCharges > 0) return 0f;
            float chargeCd = Mathf.Max(1.5f, 3.5f - dashCooldownReduction);
            return Mathf.Max(0f, chargeCd - dashChargeRechargeTimer);
        }
        return Mathf.Max(0f, dashCooldownTimer);
    }

    public bool IsDashReady()
    {
        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null && ronin.enabled && ronin.HasActiveShadowClone)
        {
            return true; // Sẵn sàng bấm nhịp 2 để thu hồi về bóng
        }
        if (IsDuneAssassin)
        {
            return currentDashCharges > 0 && dashIntervalTimer <= 0f && !isDashing;
        }
        return dashCooldownTimer <= 0f && !isDashing;
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        var titan = GetComponent<TitanBerserkerSkill>();
        if (titan != null && (titan.IsSpinning || titan.IsExecutingCataclysm || titan.IsChargingEarthshaker))
        {
            return;
        }

        bool isBlocked = IsMovementBlocked();

        bool isTitan = (titan != null);
        if (isTitan)
        {
            animator.SetFloat(MoveXHash, lastHorizontalFacing);
            animator.SetFloat(MoveYHash, 0f);
        }
        else
        {
            animator.SetFloat(MoveXHash, lastFacingDirection.x);
            animator.SetFloat(MoveYHash, lastFacingDirection.y);
        }

        float animSpeed = isBlocked ? 0f : moveInput.sqrMagnitude;
        animator.SetFloat(SpeedHash, animSpeed);

        // ĐỒNG BỘ TỐC ĐỘ HOẠT ẢNH CHẠY TƯƠNG THÍCH 100% VỚI TỐC ĐỘ DI CHUYỂN HIỆN TẠI (Player 1, 2, 3, 4)
        if (animSpeed > 0.01f)
        {
            float speedRatio = moveSpeed / Mathf.Max(3f, baseMoveSpeed);
            animator.speed = Mathf.Clamp(speedRatio, 0.5f, 3.0f);
        }
        else
        {
            var pcCombat = GetComponent<PlayerCombat>();
            if (pcCombat != null && Time.time - pcCombat.LastAttackTime < 0.45f)
            {
                animator.speed = pcCombat.AttackSpeedMultiplier; // Giữ tốc độ chém nhanh mượt mà cho đòn tấn công
            }
            else
            {
                animator.speed = 1.0f; // Trả về tốc độ 1.0x khi đứng yên để hoạt ảnh thở/idle tự nhiên
            }
        }
    }

    public void SetFacingDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.01f)
        {
            lastFacingDirection = dir.normalized;
            if (Mathf.Abs(dir.x) > 0.05f)
            {
                lastHorizontalFacing = Mathf.Sign(dir.x);
            }

            var titan = GetComponent<TitanBerserkerSkill>();
            if (titan != null && titan.IsChargingEarthshaker) return;

            if (animator != null)
            {
                bool isTitan = (titan != null);
                if (isTitan)
                {
                    animator.SetFloat(MoveXHash, lastHorizontalFacing);
                    animator.SetFloat(MoveYHash, 0f);
                }
                else
                {
                    animator.SetFloat(MoveXHash, lastFacingDirection.x);
                    animator.SetFloat(MoveYHash, lastFacingDirection.y);
                }
            }
        }
    }

    public void SetMobileInput(Vector2 input)
    {
        rawMobileInput = input;
        if (input.sqrMagnitude > 0.01f)
        {
            isMobileInputActive = true;
            lastFacingDirection = input.normalized;
            if (Mathf.Abs(input.x) > 0.05f)
            {
                lastHorizontalFacing = Mathf.Sign(input.x);
            }
            bool isBlocked = IsMovementBlocked();
            moveInput = isBlocked ? Vector2.zero : input.normalized;
        }
        else
        {
            isMobileInputActive = false;
            rawMobileInput = Vector2.zero;
            moveInput = Vector2.zero;
        }
    }

    public Vector2 GetFacingDirection() => lastFacingDirection;
    public float GetLastHorizontalFacing() => lastHorizontalFacing;
    public bool IsDashing() => isDashing;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    public Vector2 MoveInput => moveInput;

    public void AutoAlignCollider()
    {
        PhysicsMaterial2D slipMat = new PhysicsMaterial2D("PlayerSlipperyMat");
        slipMat.friction = 0f;
        slipMat.bounciness = 0f;

        var box = GetComponent<BoxCollider2D>();
        var cap = GetComponent<CapsuleCollider2D>();

        // Chuẩn hóa kích thước Collider cho nhân vật Top-Down 2D:
        // Đặt Collider ở vùng bàn chân (footprint: offset.y ~ 0.25, size ~ 0.48 x 0.40)
        // thay vì bao phủ cả ngực/đầu (offset.y = 1.25).
        // Nhờ vậy, bàn chân nhân vật có thể đi chạm sát chân tường và mép cầu/nước tự nhiên,
        // trong khi đầu và ngực tự nhiên đè lên lớp tường theo phối cảnh 2D chân thực.
        Vector2 feetOffset = new Vector2(0f, 0.35f);
        Vector2 feetSize = new Vector2(0.50f, 0.50f);

        string curChar = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        if (curChar == CharacterManager.CHAR_TITAN_BERSERKER || GetComponent<TitanBerserkerSkill>() != null)
        {
            feetOffset = new Vector2(0f, 0.35f);
            feetSize = new Vector2(0.65f, 0.50f);
        }

        if (box != null)
        {
            box.offset = feetOffset;
            box.size = feetSize;
            box.sharedMaterial = slipMat;
        }

        if (cap != null)
        {
            cap.offset = feetOffset;
            cap.size = feetSize;
            cap.sharedMaterial = slipMat;
        }

        if (box == null && cap == null)
        {
            box = gameObject.AddComponent<BoxCollider2D>();
            box.offset = feetOffset;
            box.size = feetSize;
            box.sharedMaterial = slipMat;
        }

        if (rb != null)
        {
            rb.sharedMaterial = slipMat;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        var combat = GetComponent<PlayerCombat>();
        if (combat != null) combat.AutoAlignCombat();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.BuildPipeline.isBuildingPlayer) return;
        AutoAlignCollider();
    }
#endif
}

