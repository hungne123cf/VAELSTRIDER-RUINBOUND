using UnityEngine.EventSystems;
using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 1.35f;
    [SerializeField] private float attackOffsetDistance = 1.3f;
    [SerializeField] private Vector2 bodyCenterOffset = new Vector2(0f, 0.1f);
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private int attack1Damage = 5;
    [SerializeField] private int attack2Damage = 5;
    [SerializeField] private float comboResetTime = 0.9f;

    [Header("Special Skill - Ruin Whirlwind")]
    [SerializeField] private float skillCooldown = 30.0f;
    [SerializeField] private float skillRadius = 2.8f;
    [SerializeField] private float skillKnockbackForce = 11.5f;

    private Animator animator;
    private PlayerController playerController;
    private PlayerHealth playerHealth;

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    private float skillCooldownTimer = 0f;
    private int lastAttackFrame = -1;
    private int lastSpecialSkillFrame = -1;
    private bool isUltiActive = false;
    private int bonusAttackDamage = 0;
    private float ultiCooldownReduction = 0f;
    private int bonusUltiDamage = 0;

    public void ApplyUpgradeBonus(int bonusAtk, float ultiCdReduction, int bonusUltiDmg)
    {
        bonusAttackDamage = bonusAtk;
        ultiCooldownReduction = ultiCdReduction;
        bonusUltiDamage = bonusUltiDmg;
    }

    // Hashes
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int ComboStepHash = Animator.StringToHash("ComboStep");

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();

        // Bat buoc ap dung chi so can ban Hardcore chuan
        attack1Damage = 5;
        attack2Damage = 5;
        skillCooldown = 30.0f;

        AutoAlignCombat();

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
        if (skillCooldownTimer > 0f)
        {
            skillCooldownTimer -= Time.deltaTime;
        }

        if (Time.time - lastAttackTime > comboResetTime)
        {
            comboStep = 0;
            if (animator != null) animator.SetInteger(ComboStepHash, 0);
        }

        // Tuyet ky Ruin Whirlwind (Phim F tren PC)
        if (Input.GetKeyDown(KeyCode.F))
        {
            PerformSpecialSkill();
        }

        // Danh thuong (Chuot trai hoac phim J)
        if (Time.frameCount != lastSpecialSkillFrame && !isUltiActive)
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                PerformAttack();
            }
            else if (Input.GetMouseButtonDown(0))
            {
                if (!IsPointerOverUI())
                {
                    PerformAttack();
                }
            }
        }

        UpdateAttackPointPosition();
    }

    private void UpdateAttackPointPosition()
    {
        if (attackPoint != null && playerController != null)
        {
            Vector2 dir = playerController.GetFacingDirection();
            attackPoint.localPosition = new Vector3(
                bodyCenterOffset.x + dir.x * attackOffsetDistance,
                bodyCenterOffset.y + dir.y * attackOffsetDistance,
                0
            );
        }
    }

    public void PerformAttack()
    {
        if (isUltiActive) return;
        if (playerController != null && playerController.IsDashing()) return;

        if (Time.frameCount == lastAttackFrame) return;
        lastAttackFrame = Time.frameCount;

        lastAttackTime = Time.time;
        comboStep++;
        if (comboStep > 2) comboStep = 1;
        SoundManager.EnsureInstance()?.PlaySwordSlash();

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
        int damage = ((comboStep == 1) ? attack1Damage : attack2Damage) + bonusAttackDamage;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(origin, attackRadius, enemyLayers);

        Debug.Log($"<color=cyan>[ATTACK]</color> Vung kiem (Combo {comboStep})! Tim thay: {hitEnemies.Length} ke dich.");

        foreach (Collider2D enemy in hitEnemies)
        {
            var damageable = enemy.GetComponent<IDamageable>() ?? enemy.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                SoundManager.EnsureInstance()?.PlayHitImpact();
                bool isCrit = (comboStep == 2);
                DamagePopup.Create(enemy.transform.position, damage, isCritical: isCrit);
            }
        }
    }

    public bool PerformSpecialSkill()
    {
        if (skillCooldownTimer > 0f || isUltiActive) return false;
        float effectiveCooldown = Mathf.Max(5.0f, skillCooldown - ultiCooldownReduction);
        skillCooldownTimer = effectiveCooldown;
        lastSpecialSkillFrame = Time.frameCount;

        // Kích hoạt Quét Tan Kẻ Yếu nếu nhân vật là Maid
        var sweepSkill = GetComponent<SweepTheWeakSkill>();
        if (sweepSkill != null)
        {
            StartCoroutine(sweepSkill.ExecuteSkillRoutine());
            return true;
        }

        StartCoroutine(CinematicSpecialSkillRoutine());
        return true;
    }

    private IEnumerator CinematicSpecialSkillRoutine()
    {
        isUltiActive = true;
        if (playerHealth != null) playerHealth.isInvulnerable = true;
        if (playerController != null) playerController.SetCanMove(false);

        var cam = FindAnyObjectByType<CameraFollow>();
        float originalTimeScale = Time.timeScale;

        try
        {
            // ====================================================
            // GIAI ĐOẠN 1: CUTSCENE ZOOM-IN & NGƯNG ĐỌNG THỜI GIAN
            // ====================================================
            Time.timeScale = 0.05f; // Ngưng đọng thời gian (Hit-stop / Cinematic slow-mo)

            // Camera zoom cận cảnh vào người chơi
            if (cam != null) cam.SetCinematicZoom(2.8f);

            // Phát âm thanh thức tỉnh sức mạnh thần bí
            SoundManager.EnsureInstance()?.PlayPortalSpawn();

            // Bật Hiệu Ứng Ánh Mắt Rực Lửa (Eye Flare VFX) trên mặt nhân vật
            var eyeFlare = GetComponent<EyeFlareFX>();
            if (eyeFlare == null) eyeFlare = gameObject.AddComponent<EyeFlareFX>();
            eyeFlare.TriggerEyeFlare(1.0f, isMaid: false);

            // Bật Cutscene UI: 2 vệt đen điện ảnh + Dải ánh mắt anime + Câu thoại ngầu lòi
            var cutsceneUI = UltiCinematicUI.EnsureInstance();
            if (cutsceneUI != null) cutsceneUI.ShowCutscene(1.0f, isMaid: false);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ULTI] Error in cinematic phase: " + ex);
        }

        // Chờ 1.0 giây thời gian thực (Realtime)
        yield return new WaitForSecondsRealtime(1.0f);

        // ====================================================
        // GIAI ĐOẠN 2: THỜI GIAN TRỞ LẠI & BÙNG NỔ BÃO LỐC
        // ====================================================
        Time.timeScale = 1.0f; // Luôn đảm bảo khôi phục thời gian bình thường!
        if (cam != null)
        {
            cam.ResetZoom();
            cam.Shake(0.25f, 0.35f);
        }

        Vector3 center = transform.position + new Vector3(bodyCenterOffset.x, bodyCenterOffset.y, 0f);

        // ĐỢT 1 (t = 0.0s): Chém mở màn & Hút nhẹ quái vào tâm bão
        if (animator != null) animator.Play("Player_Ulti", 0, 0f);
        SoundManager.EnsureInstance()?.PlaySwordSlash();
        ExecuteCleaveWave(center, damage: 20, isFinal: false, pullInward: true);
        yield return new WaitForSeconds(0.32f);

        // ĐỢT 2 (t = 0.32s): Bão kiếm cuộn xoay liên kích 2
        if (animator != null) animator.Play("Player_Ulti", 0, 0.15f);
        SoundManager.EnsureInstance()?.PlaySwordSlash();
        if (cam != null) cam.Shake(0.12f, 0.2f);
        ExecuteCleaveWave(center, damage: 20, isFinal: false, pullInward: false);
        yield return new WaitForSeconds(0.32f);

        // ĐỢT 3 (t = 0.64s): Bão kiếm cuộn xoay liên kích 3
        if (animator != null) animator.Play("Player_Ulti", 0, 0.3f);
        SoundManager.EnsureInstance()?.PlaySwordSlash();
        ExecuteCleaveWave(center, damage: 25, isFinal: false, pullInward: false);
        yield return new WaitForSeconds(0.32f);

        // ĐỢT 4 - ĐÒN KẾT LIỄU CỰC ĐẠI (t = 0.96s): NỔ TUNG HẤT VĂNG TOÀN BỘ QUÁI
        if (animator != null) animator.Play("Player_Ulti", 0, 0.45f);
        SoundManager.EnsureInstance()?.PlaySwordSlash();
        SoundManager.EnsureInstance()?.PlayHitImpact();
        if (cam != null) cam.Shake(0.4f, 0.5f);
        ExecuteCleaveWave(center, damage: 50 + bonusUltiDamage, isFinal: true, pullInward: false);
        yield return new WaitForSeconds(0.35f);

        // ====================================================
        // KẾT THÚC TUYỆT KỸ
        // ====================================================
        if (playerHealth != null) playerHealth.isInvulnerable = false;
        if (playerController != null) playerController.SetCanMove(true);
        isUltiActive = false;

        Debug.Log("<color=cyan>[SPECIAL SKILL]</color> Hoàn tất chuỗi Bão Lốc Tàn Tích!");
    }

    private void ExecuteCleaveWave(Vector3 center, int damage, bool isFinal, bool pullInward)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(center, skillRadius, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            var damageable = enemy.GetComponent<IDamageable>() ?? enemy.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                SoundManager.EnsureInstance()?.PlayHitImpact();

                if (isFinal)
                {
                    DamagePopup.CreateText(enemy.transform.position, $"✦ RUIN BURST! {damage} ✦", new Color(1f, 0.85f, 0.2f));
                }
                else
                {
                    DamagePopup.CreateText(enemy.transform.position, $"{damage}", new Color(0.3f, 0.9f, 1f));
                }
            }

            var enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                Vector2 dir = ((Vector2)enemy.transform.position - (Vector2)center).normalized;
                if (dir == Vector2.zero) dir = Vector2.up;

                if (pullInward)
                {
                    // Hút nhẹ vào tâm bão để đợt chém sau trúng trọn vẹn
                    enemyRb.linearVelocity = -dir * 3.5f;
                }
                else if (isFinal)
                {
                    // Đòn cuối hất văng cực mạnh ra xa
                    enemyRb.linearVelocity = dir * skillKnockbackForce;
                }
                else
                {
                    // Đẩy nhẹ giữ khoảng cách trong bão
                    enemyRb.linearVelocity = dir * 2.5f;
                }
            }
        }
    }

    public float GetSkillCooldownNormalized() => (skillCooldown > 0f) ? Mathf.Clamp01(skillCooldownTimer / skillCooldown) : 0f;
    public float GetSkillCooldownRemaining() => Mathf.Max(0f, skillCooldownTimer);
    public bool IsSkillReady() => skillCooldownTimer <= 0f && !isUltiActive;

    
    public void BlockAttackThisFrame()
    {
        lastSpecialSkillFrame = Time.frameCount;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (EventSystem.current.IsPointerOverGameObject()) return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return true;
            }
        }

        return false;
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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + new Vector3(bodyCenterOffset.x, bodyCenterOffset.y, 0), skillRadius);
    }

    public void AutoAlignCombat()
    {
        var sr = GetComponent<SpriteRenderer>();
        bool isAdventurer = (sr != null && sr.sprite != null && (sr.sprite.rect.height >= 60 || sr.sprite.name.ToLower().Contains("idle") || sr.sprite.name.ToLower().Contains("adventurer")));
        if (isAdventurer)
        {
            bodyCenterOffset = new Vector2(0f, 1.25f);
            attackOffsetDistance = 0.85f;
            attackRadius = 1.15f;
        }
        else
        {
            bodyCenterOffset = new Vector2(0f, 0.05f);
            attackOffsetDistance = 0.65f;
            attackRadius = 0.95f;
        }

        if (attackPoint != null)
        {
            UpdateAttackPointPosition();
        }
    }

    private void EnsureAttackPoint()
    {
        if (attackPoint == null)
        {
            Transform existing = transform.Find("AttackPoint");
            if (existing != null)
            {
                attackPoint = existing;
            }
            else if (Application.isPlaying)
            {
                GameObject point = new GameObject("AttackPoint");
                point.transform.SetParent(transform, false);
                attackPoint = point.transform;
            }
        }

        if (attackPoint != null)
        {
            UpdateAttackPointPosition();
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        AutoAlignCombat();
    }
#endif
}
