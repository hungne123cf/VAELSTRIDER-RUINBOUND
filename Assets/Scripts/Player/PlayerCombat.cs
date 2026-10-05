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
    private float lifestealPercent = 0f;

    private int upgradeBonusAtk = 0;
    private int weaponBonusAtk = 0;
    private int socketBonusAtk = 0;

    public void ApplyUpgradeBonus(int bonusAtk, float ultiCdReduction, int bonusUltiDmg)
    {
        upgradeBonusAtk = bonusAtk;
        bonusUltiDamage = bonusUltiDmg;
        RecalculateCombatStats();
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
        if (enemyLayers.value == 0)
        {
            enemyLayers = ~0; // Chem tat ca cac layer co IDamageable
        }

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

        RefreshEquipmentBonuses();
    }

    void Start()
    {
        RefreshEquipmentBonuses();
    }

    public void RefreshEquipmentBonuses()
    {
        var upgradeMgr = PlayerUpgradeManager.EnsureInstance();
        if (upgradeMgr != null)
        {
            upgradeBonusAtk = upgradeMgr.GetAttackBonus();
            bonusUltiDamage = upgradeMgr.GetUltiDamageBonus();
        }

        string equippedWpnId = PlayerPrefs.GetString("Equipped_Weapon_Id", "");
        weaponBonusAtk = 0;
        if (!string.IsNullOrEmpty(equippedWpnId))
        {
            var def = InventoryItemDatabase.GetItem(equippedWpnId);
            if (def != null && def.atkBonus > 0)
            {
                weaponBonusAtk = def.atkBonus;
            }
        }

        EquipmentSocketManager.GetActiveEquippedSocketBonus(out socketBonusAtk, out _, out _, out lifestealPercent, out _, out _, out _);
        RecalculateCombatStats();
    }

    public void RecalculateCombatStats()
    {
        bonusAttackDamage = upgradeBonusAtk + weaponBonusAtk + socketBonusAtk;

        EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out _, out _, out lifestealPercent, out float cdReduc, out _, out _);
        float upUltiCd = 0f;
        var upgradeMgr = PlayerUpgradeManager.EnsureInstance();
        if (upgradeMgr != null)
        {
            upUltiCd = upgradeMgr.GetUltiCooldownReduction();
        }
        ultiCooldownReduction = upUltiCd + (30.0f * cdReduc);

        var azureBlade = GetComponent<AzureBladeSkill>();
        if (azureBlade != null)
        {
            azureBlade.RefreshCooldownStats();
        }
    }

    public int BonusAttackDamage => bonusAttackDamage;
    public float LifestealPercent => lifestealPercent;

    private bool isAttackHolding = false;
    private float attackHoldDuration = 0f;

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

        // Danh thuong & Tu luc ngam ban (Chuot trai hoac phim J)
        if (Time.frameCount != lastSpecialSkillFrame && !isUltiActive)
        {
            var azureBlade = GetComponent<AzureBladeSkill>();

            if (Input.GetKeyDown(KeyCode.J) || (Input.GetMouseButtonDown(0) && !IsPointerOverUI()))
            {
                if (azureBlade == null || azureBlade.IsAttackReady)
                {
                    isAttackHolding = true;
                    attackHoldDuration = 0f;
                    if (azureBlade != null)
                    {
                        Vector2 initialAim = GetAimDirectionFromInput();
                        azureBlade.StartAim(initialAim);
                    }
                }
            }

            if (isAttackHolding)
            {
                attackHoldDuration += Time.deltaTime;
                if (azureBlade != null && attackHoldDuration >= 0.10f)
                {
                    Vector2 aimDir = GetAimDirectionFromInput();
                    azureBlade.UpdateAim(aimDir, attackHoldDuration);
                }
            }

            if (isAttackHolding && (Input.GetKeyUp(KeyCode.J) || Input.GetMouseButtonUp(0)))
            {
                if (azureBlade != null && attackHoldDuration >= 0.10f)
                {
                    azureBlade.ReleaseAimAndFire(bonusAttackDamage);
                }
                else
                {
                    PerformAttack();
                }
                isAttackHolding = false;
                attackHoldDuration = 0f;
            }
        }

        UpdateAttackPointPosition();
    }

    public Vector2 GetAimDirectionFromInput()
    {
        if (Camera.main != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 diff = (Vector2)mouseWorld - (Vector2)transform.position;
            if (diff.sqrMagnitude > 0.04f) return diff.normalized;
        }
        if (playerController != null)
        {
            Vector2 f = playerController.GetFacingDirection();
            if (f.sqrMagnitude > 0.01f) return f.normalized;
        }
        return Vector2.down;
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
        if (playerController != null && (playerController.IsDashing() || playerController.IsStunned)) return;

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

        // Kich hoat Phong Loi Lang Khach cho Nhan vat 2 (Gale Ronin)
        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null && ronin.enabled)
        {
            Vector2 facing = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
            int curAtk = ((comboStep == 1) ? attack1Damage : attack2Damage) + bonusAttackDamage;
            ronin.PerformAttack(facing, curAtk);
            return;
        }

        // Kich hoat Song Kiem cho Nhan vat 3 (Azure Blade)
        var azureBlade = GetComponent<AzureBladeSkill>();
        if (azureBlade != null && azureBlade.enabled)
        {
            Vector2 facing = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
            int curAtk = ((comboStep == 1) ? attack1Damage : attack2Damage) + bonusAttackDamage;
            azureBlade.OnPerformAttack(facing, curAtk);
            return;
        }

        ExecuteHitDamage();
    }

    private void ExecuteHitDamage()
    {
        Vector3 origin = (attackPoint != null) ? attackPoint.position : transform.position;
        int damage = ((comboStep == 1) ? attack1Damage : attack2Damage) + bonusAttackDamage;

        // Quet toan bo muc tieu trong tam chem (Bao gom ca Quai vat va Vat the chem be BreakableProp)
        Collider2D[] allHits = Physics2D.OverlapCircleAll(origin, attackRadius + 0.45f);
        var damagedSet = new System.Collections.Generic.HashSet<IDamageable>();
        int enemyCount = 0;

        foreach (var col in allHits)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is PlayerHealth) && !damagedSet.Contains(damageable))
            {
                damagedSet.Add(damageable);
                damageable.TakeDamage(damage);

                // Neu la Quai vat (khong phai binh gom / hoa co BreakableProp hoac TreasureChest)
                bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
                if (!isProp)
                {
                    enemyCount++;
                    SoundManager.EnsureInstance()?.PlayHitImpact();
                    bool isCrit = (comboStep == 2);
                    DamagePopup.Create(col.transform.position, damage, isCritical: isCrit);

                    // Hoi nang luong va tich luy Noi Nang CHỈ KHI chem trung quai vat
                    var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
                    if (energySys != null)
                    {
                        energySys.RestoreEnergy(8f);
                        energySys.AddInnerEnergy(15f);
                    }

                    // Hồi phục Máu từ Ngọc Huyết Thạch (Lifesteal)
                    if (lifestealPercent > 0f && playerHealth != null)
                    {
                        int healAmount = Mathf.Max(1, Mathf.RoundToInt(damage * lifestealPercent));
                        playerHealth.Heal(healAmount);
                    }
                }
            }
        }

        Debug.Log($"<color=cyan>[ATTACK]</color> Vung kiếm (Combo {comboStep})! Đánh trúng: {damagedSet.Count} mục tiêu ({enemyCount} quái vật).");
    }

    public bool PerformSpecialSkill()
    {
        if (skillCooldownTimer > 0f || isUltiActive || (playerController != null && playerController.IsStunned)) return false;

        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null && !energySys.TryConsumeEnergy(50f))
        {
            return false; // Khong du nang luong de thi trien Tuyet ky
        }

        float effectiveCooldown = Mathf.Max(5.0f, skillCooldown - ultiCooldownReduction);
        skillCooldownTimer = effectiveCooldown;
        lastSpecialSkillFrame = Time.frameCount;

        // Kích hoạt Bão Phong Trảm nếu nhân vật là Gale Ronin (Player 2)
        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null)
        {
            ronin.TriggerSpecialSkill();
            return true;
        }

        // Kích hoạt Bão Sóng Kiếm nếu nhân vật là Azure Blade (Player 3)
        var azureBlade = GetComponent<AzureBladeSkill>();
        if (azureBlade != null)
        {
            azureBlade.TriggerSpecialSkill();
            return true;
        }

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

        try
        {
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
            ExecuteCleaveWave(center, damage: 20 + Mathf.RoundToInt(bonusAttackDamage * 0.4f), isFinal: false, pullInward: true);
            yield return new WaitForSecondsRealtime(0.28f);

            // ĐỢT 2 (t = 0.28s): Bão kiếm cuộn xoay liên kích 2
            if (animator != null) animator.Play("Player_Ulti", 0, 0.15f);
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            if (cam != null) cam.Shake(0.12f, 0.2f);
            ExecuteCleaveWave(center, damage: 20 + Mathf.RoundToInt(bonusAttackDamage * 0.4f), isFinal: false, pullInward: false);
            yield return new WaitForSecondsRealtime(0.28f);

            // ĐỢT 3 (t = 0.56s): Bão kiếm cuộn xoay liên kích 3
            if (animator != null) animator.Play("Player_Ulti", 0, 0.3f);
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            ExecuteCleaveWave(center, damage: 25 + Mathf.RoundToInt(bonusAttackDamage * 0.5f), isFinal: false, pullInward: false);
            yield return new WaitForSecondsRealtime(0.28f);

            // ĐỢT 4 - ĐÒN KẾT LIỄU CỰC ĐẠI (t = 0.84s): NỔ TUNG HẤT VĂNG TOÀN BỘ QUÁI
            if (animator != null) animator.Play("Player_Ulti", 0, 0.45f);
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            SoundManager.EnsureInstance()?.PlayHitImpact();
            if (cam != null) cam.Shake(0.4f, 0.5f);
            ExecuteCleaveWave(center, damage: 50 + bonusUltiDamage + bonusAttackDamage, isFinal: true, pullInward: false);
            yield return new WaitForSecondsRealtime(0.30f);
        }
        finally
        {
            // ====================================================
            // KẾT THÚC TUYỆT KỸ - BẢO ĐẢM 100% LUÔN PHỤC HỒI DI CHUYỂN
            // ====================================================
            Time.timeScale = 1.0f;
            if (cam != null) cam.ResetZoom();
            if (playerHealth != null) playerHealth.isInvulnerable = false;
            if (playerController != null) playerController.SetCanMove(true);
            isUltiActive = false;
            if (animator != null) animator.Play("Idle", 0, 0f);

            Debug.Log("<color=cyan>[SPECIAL SKILL]</color> Hoàn tất chuỗi Bão Lốc Tàn Tích!");
        }
    }

    private void ExecuteCleaveWave(Vector3 center, int damage, bool isFinal, bool pullInward)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(center, skillRadius, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy == null || enemy.gameObject == gameObject || enemy.transform.IsChildOf(transform)) continue;

            var damageable = enemy.GetComponent<IDamageable>() ?? enemy.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is PlayerHealth))
            {
                damageable.TakeDamage(damage);
                SoundManager.EnsureInstance()?.PlayHitImpact();

                if (isFinal)
                {
                    DamagePopup.CreateText(enemy.transform.position, $"RUIN BURST! {damage}", new Color(1f, 0.85f, 0.2f));
                }
                else
                {
                    DamagePopup.CreateText(enemy.transform.position, $"{damage}", new Color(0.3f, 0.9f, 1f));
                }

                bool isProp = (enemy.GetComponent<BreakableProp>() != null || enemy.GetComponentInParent<BreakableProp>() != null || enemy.GetComponent<TreasureChest>() != null || enemy.GetComponentInParent<TreasureChest>() != null);
                if (!isProp)
                {
                    // Tích lũy Nội Năng khi Tuyệt kỹ đánh trúng quái
                    var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
                    if (energySys != null)
                    {
                        energySys.AddInnerEnergy(isFinal ? 12f : 5f);
                    }

                    if (lifestealPercent > 0f && playerHealth != null)
                    {
                        int healAmount = Mathf.Max(1, Mathf.RoundToInt(damage * lifestealPercent));
                        playerHealth.Heal(healAmount);
                    }
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
        string curChar = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        bool isVaelstrider = (curChar == CharacterManager.CHAR_VAELSTRIDER);
        bool isRonin = (curChar == CharacterManager.CHAR_GALE_RONIN || GetComponent<GaleRoninSkill>() != null);
        bool isAzure = (curChar == CharacterManager.CHAR_AZURE_BLADE || GetComponent<AzureBladeSkill>() != null);

        if (isRonin)
        {
            bodyCenterOffset = new Vector2(0f, 0.0f);
            attackOffsetDistance = 0.45f;
            attackRadius = 0.85f;
            skillRadius = 1.8f;
        }
        else if (isAzure)
        {
            bodyCenterOffset = new Vector2(0f, 0.05f);
            attackOffsetDistance = 0.55f;
            attackRadius = 0.95f;
            skillRadius = 2.0f;
        }
        else if (isVaelstrider)
        {
            bodyCenterOffset = new Vector2(0f, 0.65f);
            attackOffsetDistance = 0.75f;
            attackRadius = 1.05f;
            skillRadius = 2.4f;
        }
        else
        {
            bodyCenterOffset = new Vector2(0f, 0.05f);
            attackOffsetDistance = 0.45f;
            attackRadius = 0.85f;
            skillRadius = 1.8f;
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
