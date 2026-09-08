using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.8f;

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
    private float baseDashCooldown = 0.8f;
    private float bonusSpeed = 0f;
    private float dashCooldownReduction = 0f;

    public void ApplyUpgradeBonus(float speedBonus, float dashCdReduction)
    {
        bonusSpeed = speedBonus;
        dashCooldownReduction = dashCdReduction;
        moveSpeed = baseMoveSpeed + bonusSpeed;
        dashCooldown = Mathf.Max(0.35f, baseDashCooldown - dashCooldownReduction);
    }

    // Animator Hashes
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        baseMoveSpeed = moveSpeed;
        baseDashCooldown = dashCooldown;
        if (GetComponent<DashGhostTrail>() == null) gameObject.AddComponent<DashGhostTrail>();

        // Khong de Player va Enemy day nhau (loai bo vat ly va cham day nhau gay kho chiu)
        int playerLayer = gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (playerLayer >= 0 && enemyLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        }
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
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }
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
}
