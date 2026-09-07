using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1.0f;
    [SerializeField] private float attackOffsetDistance = 0.9f; // Khoảng cách vung kiếm ra trước mặt
    [SerializeField] private Vector2 bodyCenterOffset = new Vector2(0f, 1.0f); // Tâm người nhân vật (cao hơn chân)
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private int attack1Damage = 20;
    [SerializeField] private int attack2Damage = 35;
    [SerializeField] private float comboResetTime = 0.9f;

    private Animator animator;
    private PlayerController playerController;

    private int comboStep = 0;
    private float lastAttackTime = 0f;

    // Hashes
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int ComboStepHash = Animator.StringToHash("ComboStep");

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();

        if (attackPoint == null)
        {
            Transform existing = transform.Find("AttackPoint");
            if (existing != null)
            {
                attackPoint = existing;
            }
            else
            {
                GameObject point = new GameObject("AttackPoint");
                point.transform.parent = transform;
                attackPoint = point.transform;
            }
        }
        UpdateAttackPointPosition();
    }

    void Update()
    {
        if (Time.time - lastAttackTime > comboResetTime)
        {
            comboStep = 0;
            if (animator != null) animator.SetInteger(ComboStepHash, 0);
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
        {
            PerformAttack();
        }

        UpdateAttackPointPosition();
    }

    private void UpdateAttackPointPosition()
    {
        if (attackPoint != null && playerController != null)
        {
            Vector2 dir = playerController.GetFacingDirection();
            // Đặt tâm chém xuất phát từ thắt lưng (bodyCenterOffset) rồi vươn ra theo hướng mặt
            attackPoint.localPosition = new Vector3(
                bodyCenterOffset.x + dir.x * attackOffsetDistance,
                bodyCenterOffset.y + dir.y * attackOffsetDistance,
                0
            );
        }
    }

    public void PerformAttack()
    {
        if (playerController != null && playerController.IsDashing()) return;

        lastAttackTime = Time.time;
        comboStep++;
        if (comboStep > 2) comboStep = 1;

        if (animator != null)
        {
            animator.SetInteger(ComboStepHash, comboStep);
            animator.SetTrigger(AttackTriggerHash);

            string attackState = (comboStep == 1) ? "Attack1" : "Attack2";
            animator.Play(attackState, 0, 0f);
        }

        ExecuteHitDamage();
    }

    private void ExecuteHitDamage()
    {
        Vector3 origin = (attackPoint != null) ? attackPoint.position : transform.position;
        int damage = (comboStep == 1) ? attack1Damage : attack2Damage;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(origin, attackRadius, enemyLayers);

        Debug.Log($"<color=cyan>[ATTACK]</color> Vung kiem (Combo {comboStep})! Tim thay: {hitEnemies.Length} ke dich trong tam chem.");

        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log($"<color=red>[HIT ENEMY]</color> Chem trung: {enemy.name} | Sat thuong: {damage}");
            
            var damageable = enemy.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = enemy.GetComponentInParent<IDamageable>();
            }

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                bool isCrit = (comboStep == 2);
                DamagePopup.Create(enemy.transform.position, damage, isCritical: isCrit);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (attackPoint != null)
        {
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
        else
        {
            Vector3 origin = transform.position + new Vector3(bodyCenterOffset.x, bodyCenterOffset.y, 0) + Vector3.down * attackOffsetDistance;
            Gizmos.DrawWireSphere(origin, attackRadius);
        }
    }
}
