using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 5.0f;

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


        public bool IsStunned => isStunned;
    private bool isStunned = false;

    public void ApplyStun(float duration)
    {
        var ph = GetComponent<PlayerHealth>();
        if (ph != null && ph.isInvulnerable) return;

        var titan = GetComponent<TitanBerserkerSkill>();
        if (titan != null && titan.IsChargingEarthshaker) return;

        var combat = GetComponent<PlayerCombat>();
        if (combat != null && combat.IsParrying) return;

        StartCoroutine(StunRoutine(duration));
    }

    private System.Collections.IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        canMove = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetFloat(SpeedHash, 0f);

        SoundManager.EnsureInstance()?.PlayHitImpact();

        GameObject webTrap = new GameObject("WebTrapVisual");
        webTrap.transform.SetParent(transform, false);
        webTrap.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        var srWeb = webTrap.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
        srWeb.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Bosses/Skeleton King/SkeletonKing_Obstacle_0.png");
#endif
        srWeb.color = new Color(1f, 1f, 1f, 0.8f);
        srWeb.sortingOrder = 3000;
        webTrap.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        yield return new WaitForSeconds(duration);

        if (webTrap != null) Destroy(webTrap);
        isStunned = false;
        canMove = true;
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

        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null && ronin.IsChargingIaido) return true;

        var azure = GetComponent<AzureBladeSkill>();
        if (azure != null && azure.IsAiming) return true;

        var dune = GetComponent<DuneAssassinSkill>();
        if (dune != null && (dune.IsDefending || dune.IsExecutingSkill || dune.IsAttacking)) return true;

        var mage = GetComponent<CelestialMageSkill>();
        if (mage != null && (mage.IsChargingBarrier || mage.IsExecutingSkill)) return true;

        return false;
    }

    void Update()
    {
        var ph = GetComponent<PlayerHealth>();
        if (ph != null && ph.IsDead) return;

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
            rb.linearVelocity = moveInput * moveSpeed;
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
            rb.linearVelocity = moveInput * moveSpeed;
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

            // Kích hoạt Thánh Thuẫn Phản Đòn cho Hiệp Sĩ Vaelstrider (Player 1)
            var combat = GetComponent<PlayerCombat>();
            if (combat != null && combat.enabled && (titan == null || !titan.enabled) && (azureBlade == null || !azureBlade.enabled) && !IsDuneAssassin)
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

        if (box != null && cap == null)
        {
            Vector2 oldOffset = box.offset;
            Vector2 oldSize = box.size;
            Destroy(box);
            cap = gameObject.AddComponent<CapsuleCollider2D>();
            cap.offset = oldOffset;
            cap.size = oldSize;
        }
        else if (cap == null)
        {
            cap = gameObject.AddComponent<CapsuleCollider2D>();
        }

        cap.sharedMaterial = slipMat;
        if (rb != null)
        {
            rb.sharedMaterial = slipMat;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        var sr = GetComponent<SpriteRenderer>();
        string curChar = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        if (curChar == CharacterManager.CHAR_TITAN_BERSERKER || GetComponent<TitanBerserkerSkill>() != null)
        {
            cap.offset = new Vector2(0f, 0.65f);
            cap.size = new Vector2(0.90f, 1.30f);
        }
        else if (curChar == CharacterManager.CHAR_DUNE_ASSASSIN || GetComponent<DuneAssassinSkill>() != null)
        {
            cap.offset = new Vector2(0f, 0f);
            cap.size = new Vector2(0.48f, 0.72f);
        }
        else if (sr != null && sr.sprite != null)
        {
            Bounds b = sr.sprite.bounds;
            float cy = Mathf.Clamp(b.center.y, 0f, 0.55f);
            float h = Mathf.Max(0.75f, b.size.y * 0.75f);
            float w = Mathf.Max(0.45f, b.size.x * 0.55f);
            cap.offset = new Vector2(0f, cy);
            cap.size = new Vector2(w, h);
        }
        else
        {
            cap.offset = new Vector2(0f, 0f);
            cap.size = new Vector2(0.48f, 0.75f);
        }

        var combat = GetComponent<PlayerCombat>();
        if (combat != null) combat.AutoAlignCombat();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        AutoAlignCollider();
    }
#endif
}

