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
    private Vector2 lastFacingDirection = Vector2.down;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
        private bool isMobileInputActive = false;
    private bool canMove = true;
    private float baseMoveSpeed = 5f;
    private float baseDashCooldown = 5.0f;
    private float bonusSpeed = 0f;
    private float dashCooldownReduction = 0f;

    public void ApplyUpgradeBonus(float speedBonus, float dashCdReduction)
    {
        bonusSpeed = speedBonus;
        dashCooldownReduction = dashCdReduction;
        moveSpeed = baseMoveSpeed + bonusSpeed;
        dashCooldown = Mathf.Max(1.0f, baseDashCooldown - dashCooldownReduction);
    }

        public bool IsStunned => isStunned;
    private bool isStunned = false;

    public void ApplyStun(float duration)
    {
        StartCoroutine(StunRoutine(duration));
    }

    private System.Collections.IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        canMove = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetFloat(SpeedHash, 0f);

        DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, "STUNNED! (2s)", new Color(1f, 0.9f, 0.3f));
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

    void Update()
    {
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
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

        // Ban phim PC WASD / Mui ten
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        if (!isMobileInputActive)
        {
            moveInput = new Vector2(h, v).normalized;
        }

        if (moveInput != Vector2.zero)
        {
            lastFacingDirection = moveInput;
        }

        // Bam Space de Luot (Dash tren PC)
        if (Input.GetKeyDown(KeyCode.Space))
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
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (isDashing)
        {
            rb.linearVelocity = lastFacingDirection * dashSpeed;
        }
        else
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
    }

    public void SetCanMove(bool enable)
    {
        canMove = enable;
        if (!enable)
        {
            moveInput = Vector2.zero;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    public void PerformDash()
    {
        if (dashCooldownTimer <= 0 && lastFacingDirection != Vector2.zero && !isDashing)
        {
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (energySys != null && !energySys.TryConsumeEnergy(20f))
            {
                return; // Khong du nang luong de luot
            }

            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            // Hồi ngay đòn đánh thường cho Lam Kiếm Sĩ khi Dash
            var azureBlade = GetComponent<AzureBladeSkill>();
            if (azureBlade != null)
            {
                azureBlade.ResetAttackCooldown();
            }
        }
    }

    public float GetDashCooldownNormalized()
    {
        if (dashCooldown <= 0f) return 0f;
        return Mathf.Clamp01(dashCooldownTimer / dashCooldown);
    }

    public float GetDashCooldownRemaining()
    {
        return Mathf.Max(0f, dashCooldownTimer);
    }

    public bool IsDashReady()
    {
        return dashCooldownTimer <= 0f && !isDashing;
    }

    private void UpdateAnimations()
    {
        animator.SetFloat(MoveXHash, lastFacingDirection.x);
        animator.SetFloat(MoveYHash, lastFacingDirection.y);
        animator.SetFloat(SpeedHash, moveInput.sqrMagnitude);
    }

    public void SetMobileInput(Vector2 input)
    {
        if (input.sqrMagnitude > 0.01f)
        {
            isMobileInputActive = true;
            moveInput = input.normalized;
            lastFacingDirection = moveInput;
        }
        else
        {
            isMobileInputActive = false;
            moveInput = Vector2.zero;
        }
    }

    public Vector2 GetFacingDirection() => lastFacingDirection;
    public bool IsDashing() => isDashing;

    public void AutoAlignCollider()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box == null) box = gameObject.AddComponent<BoxCollider2D>();
        var sr = GetComponent<SpriteRenderer>();

        if (sr != null && sr.sprite != null)
        {
            Bounds b = sr.sprite.bounds;
            float cy = Mathf.Clamp(b.center.y, 0.35f, 0.55f);
            float h = Mathf.Max(0.9f, b.size.y * 0.85f);
            float w = Mathf.Max(0.55f, b.size.x * 0.6f);
            box.offset = new Vector2(0f, cy);
            box.size = new Vector2(w, h);
        }
        else
        {
            box.offset = new Vector2(0f, 0.45f);
            box.size = new Vector2(0.6f, 0.95f);
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

