using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Kỹ năng Chiến đấu của Player 5 - Sa Mạc Thích Khách (Dune Assassin / Mirage Nomad)
/// Phân lớp: Sát Thủ Tốc Độ / Phản Đòn & Song Đoản Đao (High Agility, Dual Daggers, Mirage Defend & Sandstorm X-Rend)
/// </summary>
public class DuneAssassinSkill : MonoBehaviour
{
    [Header("--- THÔNG SỐ ĐÒN ĐÁNH SONG ĐAO ---")]
    public int baseDaggerDamage = 18;
    public float attackRange = 1.6f;
    public float attackAngle = 160f;
    public float attackCooldown = 0.22f; // Tốc độ ra đòn siêu nhanh
    private float attackCooldownTimer = 0f;

    [Header("--- CÂN BẰNG LỐC XOÁY COMBO ĐÒN ĐÁNH THƯỜNG ---")]
    public float tornadoComboCooldown = 3.5f; // Thời gian hồi nội tại giữa 2 lần tung lốc ở đòn đánh thường 3_atk
    private float tornadoComboTimer = 0f;

    [Header("--- HỆ THỐNG THANH NỘ (FURY / RAGE SYSTEM) ---")]
    [SerializeField] private float maxFury = 100f;
    [SerializeField] private float currentFury = 0f;
    public float MaxRage => maxFury;
    public float CurrentRage => currentFury;
    public float RageRatio => maxFury > 0f ? Mathf.Clamp01(currentFury / maxFury) : 0f;
    public bool IsRageFull => currentFury >= (maxFury - 0.01f);

    public void AddRage(float amount)
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        bool wasFull = IsRageFull;
        currentFury = Mathf.Clamp(currentFury + amount, 0f, maxFury);

        if (!wasFull && IsRageFull)
        {
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.12f, 0.15f);
        }
    }

    public void ConsumeRage()
    {
        currentFury = 0f;
    }

    public int GetEffectiveAttackDamage(int fallbackBonus = 0)
    {
        if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            return baseDaggerDamage + playerCombat.BonusAttackDamage;
        }
        return baseDaggerDamage + fallbackBonus;
    }

    [Header("--- NỘI TẠI: HỒI 10% HP ĐÃ MẤT MỖI 2 CHIÊU ---")]
    private int skillCastCount = 0;

    public void OnSkillCast()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead) return;

        skillCastCount++;
        if (skillCastCount >= 2)
        {
            skillCastCount = 0;
            int maxHp = playerHealth.MaxHealth;
            int curHp = playerHealth.CurrentHealth;
            int lostHp = Mathf.Max(0, maxHp - curHp);

            if (lostHp > 0)
            {
                int healAmount = Mathf.Max(1, Mathf.RoundToInt(lostHp * 0.10f));
                playerHealth.Heal(healAmount);
            }
        }
    }

    [Header("--- THẾ THỦ ẢO ẢNH & NGẮM TUNG CƠN LỐC (AIMING / VORTEX) ---")]
    [Range(0.5f, 0.9f)]
    public float defendDamageReduction = 0.70f; // Giảm 70% sát thương khi gồng thủ
    public float minChargeDuration = 0.15f;
    public float maxChargeDuration = 1.0f;
    private bool isDefending = false;
    private bool isAimingTornado = false;
    private float defendStartTime = 0f;
    private float currentChargeDuration = 0f;
    private Vector2 currentAimDir = Vector2.down;
    private Coroutine defendAnimRoutine = null;

    [Header("--- TUYỆT KỸ: CUỒNG BÃO SA ẢNH (SANDSTORM X-REND) ---")]
    public float specialCooldown = 13.0f;
    private float specialCooldownTimer = 0f;
    public float ultimateRadius = 5.0f;
    public int baseUltiFlurryDamage = 16;
    public int baseUltiFinisherDamage = 110;
    private bool isExecutingSkill = false;

    [Header("--- TARGETING LAYERS ---")]
    public LayerMask enemyLayers;

    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    private PlayerController playerController;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private int comboStep = 0;
    private float lastComboTime = 0f;
    private Coroutine attackAnimRoutine = null;

    // Cached Sprite Frames from Assets/ART/Player5/PNG/
    private static Sprite[] idleFrames;
    private static Sprite[] runFrames;
    private static Sprite[] atk1Frames;
    private static Sprite[] atk2Frames;
    private static Sprite[] atk3Frames;
    private static Sprite[] defendFrames;
    private static Sprite[] airAtkFrames;
    private static Sprite[] spAtkFrames;
    private static Sprite[] rollFrames;
    private static Sprite[] deathFrames;

    public bool IsDefending => isDefending;
    public bool IsAimingTornado => isAimingTornado;
    public bool IsExecutingSkill => isExecutingSkill;
    public bool IsAttacking => isAttacking;
    public bool IsAttackReady => !isExecutingSkill && !isDefending && !isAimingTornado;
    public bool IsSpecialReady => specialCooldownTimer <= 0f && !isExecutingSkill;
    public float SpecialCooldown => specialCooldown;
    public float SpecialCooldownRemaining => Mathf.Max(0f, specialCooldownTimer);
    public float SpecialCooldownRatio => (specialCooldown > 0f) ? Mathf.Clamp01(specialCooldownTimer / specialCooldown) : 0f;
    public float CurrentChargeRatio => Mathf.Clamp01(currentChargeDuration / maxChargeDuration);

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

        RefreshCooldownStats();
        LoadAllPlayer5Sprites();
    }

    void OnEnable()
    {
        RefreshCooldownStats();
    }

    public void RefreshCooldownStats()
    {
        float totalCdr = 0f;
        var upgradeMgr = PlayerUpgradeManager.EnsureInstance();
        if (upgradeMgr != null)
        {
            totalCdr += upgradeMgr.GetUltiCooldownReduction();
        }

        EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out _, out _, out _, out float gemCdr, out _, out _);
        if (gemCdr > 0f)
        {
            totalCdr += 13.0f * gemCdr;
        }

        specialCooldown = Mathf.Max(5.0f, 13.0f - totalCdr);
    }

    public void ReduceSpecialCooldown(float seconds)
    {
        specialCooldownTimer = Mathf.Max(0f, specialCooldownTimer - seconds);
    }

    private Coroutine rollAnimRoutine = null;
    private bool wasDashing = false;

    void Update()
    {
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;
        if (specialCooldownTimer > 0f) specialCooldownTimer -= Time.deltaTime;
        if (tornadoComboTimer > 0f) tornadoComboTimer -= Time.deltaTime;

        if (isDefending)
        {
            currentChargeDuration += Time.deltaTime;
        }

        // Bắt đầu Dash: Kích hoạt hoạt ảnh lộn nhào (Roll Animation) từ roll_1 đến roll_6
        bool isDashing = (playerController != null && playerController.IsDashing());
        if (isDashing && !wasDashing)
        {
            CancelTornadoAim();
            if (attackAnimRoutine != null) { StopCoroutine(attackAnimRoutine); attackAnimRoutine = null; isAttacking = false; }
            if (defendExitAnimRoutine != null) { StopCoroutine(defendExitAnimRoutine); defendExitAnimRoutine = null; isAttacking = false; }
            if (rollAnimRoutine != null) StopCoroutine(rollAnimRoutine);
            rollAnimRoutine = StartCoroutine(PlayRollAnimationRoutine());
        }
        wasDashing = isDashing;

        // Reset combo nếu người chơi ngừng đánh quá 1.3 giây
        if (comboStep > 0 && Time.time - lastComboTime > 1.3f && attackAnimRoutine == null)
        {
            comboStep = 0;
        }
    }

    private IEnumerator PlayRollAnimationRoutine()
    {
        if (rollFrames != null && rollFrames.Length > 0)
        {
            for (int i = 0; i < rollFrames.Length; i++)
            {
                if (spriteRenderer != null && rollFrames[i] != null)
                {
                    spriteRenderer.sprite = rollFrames[i];
                    if (playerController != null)
                    {
                        Vector2 curDir = playerController.GetFacingDirection();
                        if (Mathf.Abs(curDir.x) > 0.05f)
                        {
                            spriteRenderer.flipX = (curDir.x < 0f);
                        }
                        else if (playerController.GetLastHorizontalFacing() < 0f)
                        {
                            spriteRenderer.flipX = true;
                        }
                        else if (playerController.GetLastHorizontalFacing() > 0f)
                        {
                            spriteRenderer.flipX = false;
                        }
                    }
                }
                yield return new WaitForSeconds(0.040f);
            }
        }

        rollAnimRoutine = null;
        if (!isAttacking && !isDefending && !isExecutingSkill && idleFrames != null && idleFrames.Length > 0 && spriteRenderer != null)
        {
            spriteRenderer.sprite = idleFrames[0];
        }
    }

    private float animTimer = 0f;
    private int animIndex = 0;

    void LateUpdate()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (isDefending || isExecutingSkill || attackAnimRoutine != null || rollAnimRoutine != null || defendExitAnimRoutine != null) return;

        if (animator != null && animator.enabled)
        {
            animator.enabled = false;
        }

        bool isMoving = (playerController != null && playerController.IsMoving);

        Sprite[] frames = isMoving ? runFrames : idleFrames;
        float frameInterval = isMoving ? 0.08f : 0.12f;

        if (frames == null || frames.Length == 0) frames = idleFrames;
        if (frames == null || frames.Length == 0) return;

        animTimer += Time.deltaTime;
        if (animTimer >= frameInterval)
        {
            animTimer = 0f;
            animIndex = (animIndex + 1) % frames.Length;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = frames[animIndex % frames.Length];
            if (playerController != null)
            {
                Vector2 curDir = playerController.GetFacingDirection();
                if (Mathf.Abs(curDir.x) > 0.05f)
                {
                    spriteRenderer.flipX = (curDir.x < 0f);
                }
                else if (playerController.GetLastHorizontalFacing() < 0f)
                {
                    spriteRenderer.flipX = true;
                }
                else if (playerController.GetLastHorizontalFacing() > 0f)
                {
                    spriteRenderer.flipX = false;
                }
            }
        }
    }

    public static void LoadAllPlayer5Sprites()
    {
        if (idleFrames != null && idleFrames.Length > 0) return;

        idleFrames = LoadFramesFromDirectory("Assets/ART/Player5/PNG/idle", "idle");
        runFrames = LoadFramesFromDirectory("Assets/ART/Player5/PNG/run", "run");
        atk1Frames = LoadFramesFromDirectory("Assets/ART/Player5/PNG/1_atk", "1_atk");
        atk2Frames = LoadFramesFromDirectory("Assets/ART/Player5/PNG/2_atk", "2_atk");
        atk3Frames = LoadFramesFromDirectory("Assets/ART/Player5/PNG/3_atk", "3_atk");
        defendFrames = LoadFramesFromDirectory("Assets/ART/Player5/PNG/defend", "defend");
        airAtkFrames = LoadFramesFromDirectory("Assets/ART/Player5/PNG/air_atk", "air_atk");
        spAtkFrames = LoadFramesFromDirectory("Assets/ART/Player5/PNG/sp_atk", "sp_atk");
        rollFrames = LoadFramesFromDirectory("Assets/ART/Player5/PNG/roll", "roll");
        deathFrames = LoadFramesFromDirectory("Assets/ART/Player5/PNG/death", "death");
    }

    private static Sprite[] LoadFramesFromDirectory(string dirPath, string prefix)
    {
        var list = new List<Sprite>();
#if UNITY_EDITOR
        if (System.IO.Directory.Exists(dirPath))
        {
            var files = System.IO.Directory.GetFiles(dirPath, "*.png");
            System.Array.Sort(files, (a, b) =>
            {
                int numA = ExtractNumber(a);
                int numB = ExtractNumber(b);
                return numA.CompareTo(numB);
            });

            foreach (var f in files)
            {
                string assetPath = f.Replace("\\", "/");
                var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
                Sprite selected = null;
                if (allAssets != null && allAssets.Length > 0)
                {
                    foreach (var a in allAssets)
                    {
                        if (a is Sprite s)
                        {
                            if (s.name.EndsWith("_0"))
                            {
                                selected = s;
                                break;
                            }
                            if (selected == null) selected = s;
                        }
                    }
                }
                if (selected != null) list.Add(selected);
            }
        }
#endif
        if (list.Count == 0)
        {
            string cleanDir = dirPath.Replace("Assets/", "");
            string fullPath = System.IO.Path.Combine(Application.dataPath, cleanDir);
            if (System.IO.Directory.Exists(fullPath))
            {
                var files = System.IO.Directory.GetFiles(fullPath, "*.png");
                System.Array.Sort(files, (a, b) => ExtractNumber(a).CompareTo(ExtractNumber(b)));
                foreach (var f in files)
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(f);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Point;
                        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                        list.Add(s);
                    }
                }
            }
        }
        return list.ToArray();
    }

    private static int ExtractNumber(string path)
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        int underscore = fileName.LastIndexOf('_');
        if (underscore >= 0 && int.TryParse(fileName.Substring(underscore + 1), out int n))
        {
            return n;
        }
        return 0;
    }

    private bool isAttacking = false;
    private bool hasBufferedAttack = false;
    private int bufferedBonusAtk = 0;
    private Coroutine defendExitAnimRoutine = null;

    // =========================================================================
    // 1. ĐÒN ĐÁNH THƯỜNG: COMBO 3 NHỊP SONG ĐAO (1_ATK -> 2_ATK -> 3_ATK)
    // =========================================================================
    public bool PerformAttack(Vector2 facingDir, int bonusAtkDamage)
    {
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (isExecutingSkill || isDefending) return false;

        if (defendExitAnimRoutine != null)
        {
            StopCoroutine(defendExitAnimRoutine);
            defendExitAnimRoutine = null;
        }

        // Nếu đang trong nhịp chém của combo trước: Ghi nhớ (buffer) để chạy tiếp combo sau khi chém xong
        if (isAttacking)
        {
            hasBufferedAttack = true;
            bufferedBonusAtk = bonusAtkDamage;
            if (playerController != null && facingDir.sqrMagnitude > 0.01f)
            {
                playerController.SetFacingDirection(facingDir);
            }
            return true;
        }

        // Tự động reset combo về 1 nếu ngừng đánh quá 1.3s hoặc sau khi kết thúc đòn 3
        if (Time.time - lastComboTime > 1.3f || comboStep >= 3 || comboStep <= 0)
        {
            comboStep = 1;
        }
        else
        {
            comboStep++;
        }

        lastComboTime = Time.time;

        if (facingDir.sqrMagnitude < 0.01f)
        {
            facingDir = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
        }
        currentAimDir = facingDir.normalized;

        if (attackAnimRoutine != null)
        {
            StopCoroutine(attackAnimRoutine);
            attackAnimRoutine = null;
        }
        attackAnimRoutine = StartCoroutine(AttackComboAnimationRoutine(comboStep, facingDir, bonusAtkDamage));

        return true;
    }

    private IEnumerator AttackComboAnimationRoutine(int step, Vector2 facingDir, int bonusAtkDamage)
    {
        isAttacking = true;
        hasBufferedAttack = false;

        Sprite[] targetFrames = (step == 1) ? atk1Frames : (step == 2) ? atk2Frames : atk3Frames;
        if (targetFrames == null || targetFrames.Length == 0) targetFrames = atk1Frames;

        SoundManager.EnsureInstance()?.PlaySwordSlash();

        // Tốc độ frame: 
        // 1_atk (8 frames): 0.080s (~0.64s)
        // 2_atk (18 frames): 0.070s (~1.26s)
        // 3_atk (26 frames): 0.070s (~1.82s)
        float frameTime = (step == 1) ? 0.080f : 0.070f;

        // Điểm ra đòn gây sát thương
        int hitIndex1 = (step == 1) ? 3 : ((step == 2) ? 6 : 13);
        int hitIndex2 = (step == 2) ? 12 : ((step == 3) ? 15 : -1);

        try
        {
            if (targetFrames != null && targetFrames.Length > 0)
            {
                for (int i = 0; i < targetFrames.Length; i++)
                {
                    // Cập nhật hướng xoay mặt TRÁI / PHẢI theo Joystick/Bàn phím liên tục trong từng frame
                    if (spriteRenderer != null)
                    {
                        if (playerController != null)
                        {
                            Vector2 curDir = playerController.GetFacingDirection();
                            if (Mathf.Abs(curDir.x) > 0.05f)
                            {
                                spriteRenderer.flipX = (curDir.x < 0f);
                                facingDir = curDir;
                            }
                            else if (playerController.GetLastHorizontalFacing() < 0f)
                            {
                                spriteRenderer.flipX = true;
                            }
                            else if (playerController.GetLastHorizontalFacing() > 0f)
                            {
                                spriteRenderer.flipX = false;
                            }
                        }
                        else if (Mathf.Abs(facingDir.x) > 0.05f)
                        {
                            spriteRenderer.flipX = (facingDir.x < 0f);
                        }

                        if (targetFrames[i] != null)
                        {
                            spriteRenderer.sprite = targetFrames[i];
                        }
                    }

                    // Gây sát thương tại nhịp chém chuẩn
                    if (i == hitIndex1 || (hitIndex2 > 0 && i == hitIndex2))
                    {
                        ExecuteComboHitDamage(step, facingDir, bonusAtkDamage, isSecondaryHit: (i == hitIndex2));
                    }

                    // ĐÒN 3 (3_ATK): HIỆN TIA NGẮM LỐC KHI SẴN SÀNG TUNG LỐC (Hết hồi chiêu 3.5s hoặc Đầy Nộ)
                    bool canSpawnComboVortex = (tornadoComboTimer <= 0f || IsRageFull);
                    if (step == 3 && i < 15 && canSpawnComboVortex)
                    {
                        Color aimColor = IsRageFull 
                            ? new Color(1f, 0.18f, 0.18f, 0.95f) 
                            : new Color(1f, 0.82f, 0.20f, 0.95f);

                        SkillAimIndicator.EnsureExists(gameObject)?.ShowAimLine(
                            (Vector2)transform.position, 
                            facingDir, 
                            8.0f, 
                            aimColor, 
                            IsRageFull ? 0.30f : 0.24f, 
                            0.40f
                        );
                    }

                    // ĐÒN 3 (3_ATK) TẠI FRAME 15 (3_atk_16): PHÓNG RA CƠN LỐC HOẶC TUNG SONG ĐAO TRẢM NẾU ĐANG HỒI CHIÊU LỐC
                    if (step == 3 && i == 15)
                    {
                        SkillAimIndicator.Instance?.Hide();
                        if (canSpawnComboVortex)
                        {
                            tornadoComboTimer = tornadoComboCooldown;
                            SpawnSandBladeVortex((Vector2)transform.position + facingDir * 0.65f, facingDir, bonusAtkDamage);
                        }
                        else
                        {
                            // Đang trong hồi chiêu nội tại lốc: Tung cú chém cận chiến áp sát gây thêm sát thương
                            ExecuteComboHitDamage(3, facingDir, bonusAtkDamage, isSecondaryHit: true);
                        }
                    }

                    yield return new WaitForSeconds(frameTime);
                }
            }
        }
        finally
        {
            isAttacking = false;
            SkillAimIndicator.Instance?.Hide();
        }

        // Nếu người chơi đã bấm tiếp trong lúc chém: Tự động chạy tiếp nhịp combo tiếp theo mượt mà
        if (hasBufferedAttack)
        {
            hasBufferedAttack = false;
            comboStep = (step >= 3) ? 1 : step + 1;
            lastComboTime = Time.time;

            Vector2 nextFacing = (playerController != null) ? playerController.GetFacingDirection() : facingDir;
            attackAnimRoutine = StartCoroutine(AttackComboAnimationRoutine(comboStep, nextFacing, bufferedBonusAtk));
        }
        else
        {
            attackAnimRoutine = null;
            if (idleFrames != null && idleFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
        }
    }

    private void ExecuteComboHitDamage(int step, Vector2 facingDir, int bonusAtkDamage, bool isSecondaryHit = false)
    {
        float curRange = (step == 3 && isSecondaryHit) ? 2.4f : attackRange;
        Vector2 origin = (step == 3 && isSecondaryHit) ? (Vector2)transform.position : (Vector2)transform.position + facingDir * 0.45f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, curRange);

        float dmgMultiplier = (step == 1) ? 1.0f : (step == 2) ? 1.35f : 2.2f;
        int effectiveAtk = GetEffectiveAttackDamage(bonusAtkDamage);
        int totalDamage = Mathf.RoundToInt(effectiveAtk * dmgMultiplier);
        bool isCrit = (step == 3);

        if (step == 3 && isSecondaryHit)
        {
            var cam = Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.18f, 0.22f);
        }

        int hitEnemyCount = 0;
        var damagedSet = new HashSet<IDamageable>();

        foreach (var col in hits)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is PlayerHealth) && !damagedSet.Contains(damageable))
            {
                damagedSet.Add(damageable);
                int actualDmg = AugmentManager.ProcessOutgoingDamage(gameObject, col.gameObject, totalDamage);
                damageable.TakeDamage(actualDmg);
                AugmentManager.TriggerOnHitAugments(gameObject, col.gameObject, actualDmg, damageable);

                bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
                if (!isProp)
                {
                    hitEnemyCount++;
                    SoundManager.EnsureInstance()?.PlayHitImpact();
                    DamagePopup.Create(col.transform.position, actualDmg, (step == 3 ? DamageType.TrueDamage : DamageType.Physical), isCritical: isCrit);

                    // TÍCH NỘ CHO SA MẠC THÍCH KHÁCH KHI GÂY SÁT THƯƠNG
                    AddRage(step == 1 ? 14f : (step == 2 ? 18f : 30f));

                    // TÍCH NỘI NĂNG (INNER ENERGY) ĐỂ KÍCH HOẠT GIÁP HỘ THÂN
                    var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
                    if (energySys != null)
                    {
                        energySys.AddInnerEnergy(step == 1 ? 10f : (step == 2 ? 15f : 25f));
                    }

                    // Đòn thứ 3 hất lùi quái vật
                    if (step == 3)
                    {
                        var rbEnemy = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
                        if (rbEnemy != null)
                        {
                            rbEnemy.AddForce(facingDir * 4.0f, ForceMode2D.Impulse);
                        }
                    }

                    // Lifesteal & Overheal cho Đòn Đánh Thường
                    if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null && !playerHealth.IsDead)
                    {
                        int healAmount = Mathf.Max(1, Mathf.RoundToInt(totalDamage * playerCombat.LifestealPercent));
                        if (!playerHealth.IsFullHealth())
                        {
                            playerHealth.Heal(healAmount);
                        }
                        else
                        {
                            // Khi máu gốc đã đầy: Chuyển % hút máu thành Máu Ảo (Overheal)
                            playerHealth.AddVirtualHealth(healAmount, 15f);
                        }
                    }
                }
            }
        }
    }

    // =========================================================================
    // 2. CƠ CHẾ GỒNG & ĐIỀU KHIỂN VÙNG NGẮM TUNG CƠN LỐC (TORNADO AIMING)
    // =========================================================================
    public void StartTornadoAim(Vector2 aimDir)
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (isExecutingSkill) return;

        isAttacking = false;
        hasBufferedAttack = false;
        isDefending = true;
        isAimingTornado = true;
        defendStartTime = Time.time;
        currentChargeDuration = 0f;
        
        if (aimDir.sqrMagnitude < 0.01f)
        {
            aimDir = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
        }
        currentAimDir = aimDir.normalized;

        if (attackAnimRoutine != null) { StopCoroutine(attackAnimRoutine); attackAnimRoutine = null; }
        if (defendExitAnimRoutine != null) { StopCoroutine(defendExitAnimRoutine); defendExitAnimRoutine = null; }
        if (defendAnimRoutine != null) StopCoroutine(defendAnimRoutine);

        // Hiển thị ngay VÙNG NGẮM TIA LỐC SA MẠC PHÁT SÁNG (MÀU ĐỎ NẾU NỘ ĐẦY)
        Color aimColor = IsRageFull 
            ? new Color(1f, 0.18f, 0.18f, 0.95f) 
            : new Color(1f, 0.82f, 0.20f, 0.95f);

        SkillAimIndicator.EnsureExists(gameObject)?.ShowAimLine(
            (Vector2)transform.position, 
            currentAimDir, 
            8.0f, 
            aimColor, 
            IsRageFull ? 0.30f : 0.24f, 
            0.40f
        );

        defendAnimRoutine = StartCoroutine(DefendLoopRoutine());
    }

    public void StartDefendCharge(Vector2 aimDir)
    {
        StartTornadoAim(aimDir);
    }

    private IEnumerator DefendLoopRoutine()
    {
        if (defendFrames == null || defendFrames.Length == 0) yield break;

        // Chạy frame đầu nếu có
        if (defendFrames.Length > 0 && spriteRenderer != null)
        {
            spriteRenderer.sprite = defendFrames[0];
            if (Mathf.Abs(currentAimDir.x) > 0.05f) spriteRenderer.flipX = (currentAimDir.x < 0f);
        }
        yield return new WaitForSeconds(0.05f);

        // Giữ cố định ở frame Defend_2_0 (index 1) trong suốt thời gian gồng ngắm
        int holdIndex = Mathf.Min(1, defendFrames.Length - 1);
        while (isDefending || isAimingTornado)
        {
            if (spriteRenderer != null && defendFrames.Length > holdIndex)
            {
                spriteRenderer.sprite = defendFrames[holdIndex];
                if (Mathf.Abs(currentAimDir.x) > 0.05f)
                {
                    spriteRenderer.flipX = (currentAimDir.x < 0f);
                }
            }
            yield return null;
        }
    }

    public void UpdateTornadoAim(Vector2 aimDir, float holdDuration = 0f)
    {
        if (!isAimingTornado && !isDefending) return;

        if (aimDir.sqrMagnitude > 0.01f)
        {
            currentAimDir = aimDir.normalized;
        }
        currentChargeDuration = holdDuration;

        if (spriteRenderer != null && Mathf.Abs(currentAimDir.x) > 0.05f)
        {
            spriteRenderer.flipX = (currentAimDir.x < 0f);
        }

        // Cập nhật tia ngắm theo hướng xoay Joystick / Chuột (Màu Đỏ nếu Nộ Đầy)
        Color aimColor = IsRageFull 
            ? new Color(1f, 0.18f, 0.18f, 0.95f) 
            : new Color(1f, 0.82f, 0.20f, 0.95f);

        SkillAimIndicator.EnsureExists(gameObject)?.ShowAimLine(
            (Vector2)transform.position, 
            currentAimDir, 
            8.0f, 
            aimColor, 
            IsRageFull ? 0.30f : 0.24f, 
            0.40f
        );
    }

    public void UpdateDefendCharge(Vector2 aimDir)
    {
        UpdateTornadoAim(aimDir, currentChargeDuration);
    }

    public void CancelTornadoAim()
    {
        isDefending = false;
        isAimingTornado = false;
        isAttacking = false;
        currentChargeDuration = 0f;

        if (defendAnimRoutine != null)
        {
            StopCoroutine(defendAnimRoutine);
            defendAnimRoutine = null;
        }
        if (defendExitAnimRoutine != null)
        {
            StopCoroutine(defendExitAnimRoutine);
            defendExitAnimRoutine = null;
        }

        SkillAimIndicator.Instance?.Hide();

        if (!isAttacking && !isExecutingSkill && idleFrames != null && idleFrames.Length > 0 && spriteRenderer != null)
        {
            spriteRenderer.sprite = idleFrames[0];
        }
    }

    public void ReleaseTornadoAim(Vector2 aimDir, int bonusAtkDamage = 0)
    {
        if (!isDefending && !isAimingTornado) return;
        isDefending = false;
        isAimingTornado = false;

        SkillAimIndicator.Instance?.Hide();

        if (aimDir.sqrMagnitude > 0.01f)
        {
            currentAimDir = aimDir.normalized;
        }

        if (defendAnimRoutine != null)
        {
            StopCoroutine(defendAnimRoutine);
            defendAnimRoutine = null;
        }
        if (defendExitAnimRoutine != null)
        {
            StopCoroutine(defendExitAnimRoutine);
            defendExitAnimRoutine = null;
        }

        // Tung cú chém bão lốc xoáy chuẩn xác vào hướng đã ngắm
        defendExitAnimRoutine = StartCoroutine(ReleaseTornadoRoutine(currentAimDir, bonusAtkDamage));
    }

    public void ReleaseDefendCharge(Vector2 aimDir)
    {
        int bonus = (playerCombat != null) ? playerCombat.BonusAttackDamage : 0;
        ReleaseTornadoAim(aimDir, bonus);
    }

    private IEnumerator ReleaseTornadoRoutine(Vector2 aimDir, int bonusAtkDamage)
    {
        isAttacking = true;
        SoundManager.EnsureInstance()?.PlaySwordSlash();

        try
        {
            Sprite[] frames = (atk3Frames != null && atk3Frames.Length > 0) ? atk3Frames : defendFrames;
            int startIndex = (frames != null && frames == atk3Frames && frames.Length > 12) ? 12 : 2;

            if (frames != null && frames.Length > startIndex)
            {
                for (int i = startIndex; i < frames.Length; i++)
                {
                    if (isExecutingSkill) yield break;

                    if (spriteRenderer != null && frames[i] != null)
                    {
                        spriteRenderer.sprite = frames[i];
                        if (Mathf.Abs(aimDir.x) > 0.05f) spriteRenderer.flipX = (aimDir.x < 0f);
                    }

                    // Điểm phóng ra cơn lốc sa mạc
                    if (i == 15 || (frames != atk3Frames && i == startIndex + 1))
                    {
                        SpawnSandBladeVortex((Vector2)transform.position + aimDir * 0.65f, aimDir, bonusAtkDamage);
                        ExecuteComboHitDamage(3, aimDir, bonusAtkDamage, isSecondaryHit: true);
                        isAttacking = false;
                        if (playerController != null) playerController.RefreshMoveInput();
                    }

                    yield return new WaitForSeconds(0.060f);
                }
            }
            else
            {
                SpawnSandBladeVortex((Vector2)transform.position + aimDir * 0.65f, aimDir, bonusAtkDamage);
            }
        }
        finally
        {
            isAttacking = false;
            defendExitAnimRoutine = null;

            if (playerController != null) playerController.RefreshMoveInput();

            if (!isExecutingSkill && idleFrames != null && idleFrames.Length > 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
        }
    }

    public void OnDefendHitTrigger(int originalDamage)
    {
        // Chặn sát thương trong thế thủ & Tích nộ phản đòn
        SoundManager.EnsureInstance()?.PlaySwordSlash();
        DamagePopup.Create(transform.position + Vector3.up * 0.5f, 0, false); // BLOCKED!
        AddRage(20f);
    }

    public void SpawnSandBladeVortex(Vector2 origin, Vector2 direction, int bonusAtk)
    {
        tornadoComboTimer = tornadoComboCooldown; // Đồng bộ thời gian hồi nội tại lốc

        bool isEmpowered = IsRageFull;
        if (isEmpowered)
        {
            ConsumeRage();
            var cam = Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.22f, 0.30f);
        }
        else
        {
            // Tích 10 nộ một lần duy nhất khi tung lốc thường (thay vì tích nộ vô hạn từng tick lốc)
            AddRage(10f);
        }

        // LÕI NÂNG CẤP ĐẶC QUYỀN SÁT THỦ SA MẠC: TAM SA LỐC XOÁY (Phóng cùng lúc 3 Cơn Lốc Cát)
        bool hasTripleVortex = AugmentManager.Instance != null && AugmentManager.Instance.HasAugment(AugmentDatabase.AugmentType.TripleMirageTornado);

        // CƠ CHẾ HIẾN TẾ MÁU (BLOOD SACRIFICE): Tiêu hao % máu khi tung lốc xoáy (8% thường / 12% nộ)
        // Khi phóng ra 3 cơn lốc cát thì tiêu tốn máu gấp 3 lần để tung ra
        if (playerHealth != null && !playerHealth.IsDead)
        {
            float baseHpPct = isEmpowered ? 0.12f : 0.08f;
            float hpMultiplier = hasTripleVortex ? 3f : 1f;
            playerHealth.ConsumeHealthPercent(baseHpPct * hpMultiplier);
        }

        // Đếm số lần dùng chiêu -> Hồi 10% HP đã mất mỗi 2 chiêu
        OnSkillCast();

        int effectiveAtk = GetEffectiveAttackDamage(bonusAtk);
        int totalDamage = Mathf.RoundToInt(effectiveAtk * (isEmpowered ? 3.3f : 2.2f));
        GameObject vortexObj = new GameObject(isEmpowered ? "Dune_CrimsonSandVortex" : "Dune_SandBladeVortex");
        vortexObj.transform.position = origin;
        var vortex = vortexObj.AddComponent<SandBladeVortexProjectile>();
        vortex.Initialize(direction, totalDamage, effectiveAtk, enemyLayers, gameObject, isEmpowered);

        // Phóng cùng lúc 3 Cơn Lốc Cát (Trái - Giữa - Phải) khi có lõi Tam Sa Lốc Xoáy
        if (hasTripleVortex)
        {
            Vector2 dirLeft = Quaternion.Euler(0, 0, 24f) * direction;
            Vector2 dirRight = Quaternion.Euler(0, 0, -24f) * direction;

            GameObject leftObj = new GameObject(isEmpowered ? "Dune_CrimsonSandVortex_Left" : "Dune_SandBladeVortex_Left");
            leftObj.transform.position = origin;
            var vLeft = leftObj.AddComponent<SandBladeVortexProjectile>();
            vLeft.Initialize(dirLeft, totalDamage, effectiveAtk, enemyLayers, gameObject, isEmpowered);

            GameObject rightObj = new GameObject(isEmpowered ? "Dune_CrimsonSandVortex_Right" : "Dune_SandBladeVortex_Right");
            rightObj.transform.position = origin;
            var vRight = rightObj.AddComponent<SandBladeVortexProjectile>();
            vRight.Initialize(dirRight, totalDamage, effectiveAtk, enemyLayers, gameObject, isEmpowered);
        }
    }

    // =========================================================================
    // 3. TUYỆT KỸ: CUỒNG BÃO SA ẢNH (SANDSTORM X-REND - SP_ATK 30 FRAMES)
    // =========================================================================
    public bool TriggerSpecialSkill()
    {
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (!IsSpecialReady) return false;

        isAttacking = false;
        hasBufferedAttack = false;
        if (attackAnimRoutine != null) { StopCoroutine(attackAnimRoutine); attackAnimRoutine = null; }
        if (defendAnimRoutine != null) { StopCoroutine(defendAnimRoutine); defendAnimRoutine = null; }
        if (defendExitAnimRoutine != null) { StopCoroutine(defendExitAnimRoutine); defendExitAnimRoutine = null; }

        specialCooldownTimer = specialCooldown;

        // CƠ CHẾ HIẾN TẾ MÁU (BLOOD SACRIFICE): Tiêu hao 20% Máu tối đa khi kích hoạt Tuyệt Kỹ
        if (playerHealth != null && !playerHealth.IsDead)
        {
            playerHealth.ConsumeHealthPercent(0.20f); // 20% Max HP
        }

        // Đếm số lần dùng chiêu -> Hồi 10% HP đã mất mỗi 2 chiêu
        OnSkillCast();

        StartCoroutine(ExecuteSandstormXRendRoutine());
        return true;
    }

    private IEnumerator ExecuteSandstormXRendRoutine()
    {
        isExecutingSkill = true;
        if (playerHealth != null) playerHealth.isInvulnerable = true;
        if (playerController != null) playerController.SetCanMove(false);

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.SetCinematicZoom(6.8f); // Zoom out bao quát toàn bộ đấu trường bão cát
        Time.timeScale = 0.15f; // Slow-mo cinematic

        yield return new WaitForSecondsRealtime(0.25f);
        Time.timeScale = 1.0f;

        // 1. HÚT QUÁI VẬT VÀO TÂM CƠN BÃO CÁT (Sand Vortex Pull)
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, ultimateRadius);
        foreach (var col in enemies)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
            var rbEnemy = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
            if (rbEnemy != null)
            {
                Vector2 dirToCenter = ((Vector2)transform.position - (Vector2)col.transform.position).normalized;
                rbEnemy.AddForce(dirToCenter * 6.0f, ForceMode2D.Impulse);
            }
        }

        // 2. CHẠY 30 FRAMES ANIMATION SP_ATK VÀ XẢ SÁT THƯƠNG
        if (spAtkFrames != null && spAtkFrames.Length > 0)
        {
            for (int i = 0; i < spAtkFrames.Length; i++)
            {
                if (spriteRenderer != null && spAtkFrames[i] != null)
                {
                    spriteRenderer.sprite = spAtkFrames[i];
                }

                // Các nhịp chém liên hoàn (Frame 6, 12, 18)
                if (i == 6 || i == 12 || i == 18)
                {
                    SoundManager.EnsureInstance()?.PlaySwordSlash();
                    DealAoEFlurryDamage(baseUltiFlurryDamage);
                }

                // Nhát chém Chữ X Kết Liễu (Finisher Frame 24..26)
                if (i == 24)
                {
                    SoundManager.EnsureInstance()?.PlayHitImpact();
                    DealAoEFinisherDamage(baseUltiFinisherDamage);
                    if (cam != null) cam.Shake(0.35f, 0.45f);
                }

                yield return new WaitForSeconds(0.038f);
            }
        }

        if (cam != null) cam.ResetZoom();
        if (playerHealth != null) playerHealth.isInvulnerable = false;
        if (playerController != null) playerController.SetCanMove(true);
        isExecutingSkill = false;
    }

    private void DealAoEFlurryDamage(int damage)
    {
        int effectiveAtk = GetEffectiveAttackDamage();
        int actualDamage = Mathf.Max(damage, Mathf.RoundToInt(effectiveAtk * 0.45f));
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, ultimateRadius);
        foreach (var col in hits)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is PlayerHealth))
            {
                int procDamage = AugmentManager.ProcessOutgoingDamage(gameObject, col.gameObject, actualDamage);
                damageable.TakeDamage(procDamage);
                bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
                if (!isProp)
                {
                    DamagePopup.Create(col.transform.position, procDamage, DamageType.Physical, isCritical: false);
                    AugmentManager.TriggerOnHitAugments(gameObject, col.gameObject, procDamage, damageable);

                    // Tích nộ và nội năng từ tuyệt kỹ (chỉ tính quái & boss)
                    AddRage(8f);
                    var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
                    if (energySys != null) energySys.AddInnerEnergy(6f);

                    // HỒI 50% SÁT THƯƠNG THÀNH MÁU & OVERHEAL MÁU ẢO TỪ TRANG BỊ/NGỌC
                    if (playerHealth != null && !playerHealth.IsDead)
                    {
                        int healAmount = Mathf.Max(1, Mathf.RoundToInt(actualDamage * 0.50f));
                        playerHealth.Heal(healAmount); // 50% Damage = HP

                        if (playerCombat != null && playerCombat.LifestealPercent > 0f)
                        {
                            int virtualHp = Mathf.Max(1, Mathf.RoundToInt(actualDamage * playerCombat.LifestealPercent));
                            playerHealth.AddVirtualHealth(virtualHp, 15f);
                        }
                    }
                }
            }
        }
    }

    private void DealAoEFinisherDamage(int damage)
    {
        int effectiveAtk = GetEffectiveAttackDamage();
        int actualDamage = Mathf.Max(damage, Mathf.RoundToInt(effectiveAtk * 2.85f));
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, ultimateRadius);
        foreach (var col in hits)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is PlayerHealth))
            {
                int procDamage = AugmentManager.ProcessOutgoingDamage(gameObject, col.gameObject, actualDamage);
                damageable.TakeDamage(procDamage);
                bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
                if (!isProp)
                {
                    DamagePopup.Create(col.transform.position, procDamage, DamageType.TrueDamage, isCritical: true);
                    AugmentManager.TriggerOnHitAugments(gameObject, col.gameObject, procDamage, damageable);

                    // Tích nộ và nội năng đòn kết liễu (chỉ tính quái & boss)
                    AddRage(25f);
                    var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
                    if (energySys != null) energySys.AddInnerEnergy(20f);

                    // HỒI 50% SÁT THƯƠNG ĐÒN KẾT LIỄU & OVERHEAL MÁU ẢO TỪ TRANG BỊ/NGỌC
                    if (playerHealth != null && !playerHealth.IsDead)
                    {
                        int healAmount = Mathf.Max(1, Mathf.RoundToInt(actualDamage * 0.50f));
                        playerHealth.Heal(healAmount); // 50% Damage = HP

                        if (playerCombat != null && playerCombat.LifestealPercent > 0f)
                        {
                            int virtualHp = Mathf.Max(2, Mathf.RoundToInt(actualDamage * playerCombat.LifestealPercent));
                            playerHealth.AddVirtualHealth(virtualHp, 15f);
                        }
                    }

                    // Kèm hiệu ứng Sand Bleed rút máu sau đó
                    StartCoroutine(ApplySandBleedRoutine(damageable, col.transform));
                }
            }
        }
    }

    private IEnumerator ApplySandBleedRoutine(IDamageable target, Transform targetTransform)
    {
        int bleedTicks = 3;
        int effectiveAtk = GetEffectiveAttackDamage();
        int bleedDamage = Mathf.Max(3, Mathf.RoundToInt(effectiveAtk * 0.35f));
        int bleedHeal  = Mathf.RoundToInt(bleedDamage * 0.40f);

        for (int t = 0; t < bleedTicks; t++)
        {
            yield return new WaitForSeconds(0.6f);
            if (target != null && targetTransform != null)
            {
                target.TakeDamage(bleedDamage);
                DamagePopup.Create(targetTransform.position, bleedDamage, isCritical: false);

                // Tích nộ từ độc chảy máu
                AddRage(6f);

                // Hồi máu 40% từ độc chảy máu
                if (bleedHeal > 0 && playerHealth != null && !playerHealth.IsDead)
                {
                    playerHealth.Heal(bleedHeal);
                }
            }
        }
    }
}

/// <summary>
/// Lốc Kiếm Sa Mạc (Sand Blade Vortex Projectile) do Sa Mạc Thích Khách phóng ra.
/// Khi đủ Nộ (100% Rage): Chuyển hóa thành Cơn Lốc Đỏ (Crimson Vortex) uy lực x1.5 và GÂY CHOÁNG KẺ ĐỊCH 2 GIÂY.
/// </summary>
public class SandBladeVortexProjectile : MonoBehaviour
{
    private Vector2 moveDir = Vector2.right;
    private float moveSpeed = 8.5f;
    private float lifeTimer = 0.95f;
    private int baseDamage = 25;
    private int baseDaggerAtk = 18;
    private LayerMask targetMask;
    private GameObject owner;
    private bool isEmpowered = false;

    private SpriteRenderer sr;
    private float tickTimer = 0f;
    private float animTimer = 0f;
    private int animIndex = 0;
    private HashSet<IDamageable> hitThisTick = new HashSet<IDamageable>();
    private HashSet<IDamageable> stunnedEnemies = new HashSet<IDamageable>();

    private static readonly Dictionary<int, float> s_EnemyLastStunTime = new Dictionary<int, float>();
    private const float STUN_IMMUNITY_COOLDOWN = 3.0f; // 3 giây miễn nhiễm choáng tiếp theo từ lốc

    private static Sprite[] vortexRepeatableFrames;
    private static Sprite[] vortexHitFrames;

    public void Initialize(Vector2 dir, int damage, int daggerAtk, LayerMask mask, GameObject ownerObj, bool isEmpoweredRed = false)
    {
        moveDir = (dir.sqrMagnitude > 0.01f) ? dir.normalized : Vector2.down;
        baseDamage = damage;
        baseDaggerAtk = daggerAtk;
        targetMask = mask;
        owner = ownerObj;
        isEmpowered = isEmpoweredRed;

        if (isEmpowered)
        {
            moveSpeed = 9.2f;
            lifeTimer = 1.35f; // Tồn tại lâu hơn, càn quét rộng hơn
        }

        LoadVortexSprites();
        EnsureVisuals();
    }

    private static void LoadVortexSprites()
    {
        if (vortexRepeatableFrames != null && vortexRepeatableFrames.Length > 0) return;

        var repList = new List<Sprite>();
        var hitList = new List<Sprite>();

#if UNITY_EDITOR
        for (int i = 16; i <= 22; i++)
        {
            string path = $"Assets/ART/Player5/PNG/3_atk/3_atk_{i}.png";
            var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            if (allAssets != null && allAssets.Length > 0)
            {
                foreach (var a in allAssets)
                {
                    if (a is Sprite s && !s.name.EndsWith("_0"))
                    {
                        if (i <= 20)
                        {
                            repList.Add(s);
                        }
                        else
                        {
                            hitList.Add(s);
                        }
                    }
                }
            }
        }
#endif

        vortexRepeatableFrames = repList.ToArray();
        vortexHitFrames = hitList.ToArray();
    }

    private void EnsureVisuals()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 5050;

        // CƠN LỐC ĐỎ CUỒNG NỘ vs CƠN LỐC SA MẠC THƯỜNG
        sr.color = isEmpowered ? new Color(1f, 0.22f, 0.22f, 1f) : Color.white;

        if (vortexRepeatableFrames != null && vortexRepeatableFrames.Length > 0)
        {
            sr.sprite = vortexRepeatableFrames[0];
        }

        float scale = isEmpowered ? 3.3f : 2.4f;
        transform.localScale = new Vector3(scale, scale, 1f);
        transform.rotation = Quaternion.identity;

        if (sr != null && moveDir.x < 0f)
        {
            sr.flipX = true;
        }
    }

    void Update()
    {
        // Di chuyển về phía trước theo hướng ném
        transform.position += (Vector3)(moveDir * moveSpeed * Time.deltaTime);

        // Frame animation lốc xoáy xoay tít
        if (vortexRepeatableFrames != null && vortexRepeatableFrames.Length > 0)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= 0.045f)
            {
                animTimer = 0f;
                animIndex = (animIndex + 1) % vortexRepeatableFrames.Length;
                if (sr != null) sr.sprite = vortexRepeatableFrames[animIndex];
            }
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= 0.11f)
        {
            tickTimer = 0f;
            ExecuteVortexSliceHit();
        }

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            if (vortexHitFrames != null && vortexHitFrames.Length > 0)
            {
                SpawnHitBurst(transform.position);
            }
            Destroy(gameObject);
        }
    }

    private void ExecuteVortexSliceHit()
    {
        float hitRadius = isEmpowered ? 2.2f : 1.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);
        hitThisTick.Clear();

        int tickDmg = Mathf.Max(8, Mathf.RoundToInt(baseDamage * 0.45f));

        foreach (var col in hits)
        {
            if (col == null || col.gameObject == owner || (owner != null && col.transform.IsChildOf(owner.transform))) continue;

            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is PlayerHealth) && !hitThisTick.Contains(damageable))
            {
                hitThisTick.Add(damageable);
                damageable.TakeDamage(tickDmg);
                // Kiểm tra xem có phải thùng gỗ, bình gốm, rương kho báu không
                bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
                if (!isProp)
                {
                    DamagePopup.Create(col.transform.position, tickDmg, isEmpowered ? DamageType.TrueDamage : DamageType.Physical, isCritical: isEmpowered);

                    // CÂN BẰNG HIỆU ỨNG KHỐNG CHẾ & CHOÁNG (STUN) CỦA CƠN LỐC ĐỎ
                    bool isBoss = (col.tag == "Boss") ||
                                   (col.GetComponent<ValleyGolemBossAI>() != null || col.GetComponentInParent<ValleyGolemBossAI>() != null) ||
                                   (col.GetComponent<SkeletonKingBossAI>() != null || col.GetComponentInParent<SkeletonKingBossAI>() != null) ||
                                   (col.GetComponent<BotWheelAI>() != null || col.GetComponentInParent<BotWheelAI>() != null) ||
                                   (col.GetComponent<BossGigapackSkills>() != null || col.GetComponentInParent<BossGigapackSkills>() != null) ||
                                   col.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   col.transform.root.name.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   col.name.IndexOf("Titan", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   col.transform.root.name.IndexOf("Titan", System.StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isEmpowered && !stunnedEnemies.Contains(damageable))
                    {
                        stunnedEnemies.Add(damageable);
                        int targetId = col.gameObject.GetInstanceID();
                        float lastStun = 0f;
                        bool hasRecentStun = s_EnemyLastStunTime.TryGetValue(targetId, out lastStun) && (Time.time - lastStun < STUN_IMMUNITY_COOLDOWN);

                        if (isBoss)
                        {
                            // Boss có kháng khống chế: Chỉ bị ngắt chiêu nhẹ (micro-interrupt 0.20s), không bị choáng cứng
                            if (!hasRecentStun)
                            {
                                s_EnemyLastStunTime[targetId] = Time.time;
                                EnemyStunEffect.ApplyStun(col.gameObject, 0.20f);
                                DamagePopup.CreateText(col.transform.position + Vector3.up * 0.45f, "NGẮT CHIÊU!", new Color(1f, 0.75f, 0.2f));
                            }
                        }
                        else if (!hasRecentStun)
                        {
                            // Quái thường: Choáng 0.75s (giảm từ 2.0s) và có thời gian hồi miễn nhiễm 3.0s
                            s_EnemyLastStunTime[targetId] = Time.time;
                            EnemyStunEffect.ApplyStun(col.gameObject, 0.75f);
                            DamagePopup.CreateText(col.transform.position + Vector3.up * 0.45f, "CHOÁNG!", new Color(1f, 0.9f, 0.2f));
                        }
                    }

                    // Hút kẻ địch vào tâm lốc (Boss có lực kháng cự lớn, chỉ bị kéo nhẹ)
                    var rbEnemy = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
                    if (rbEnemy != null)
                    {
                        Vector2 pullDir = ((Vector2)transform.position - (Vector2)col.transform.position).normalized;
                        float pullMagnitude = isBoss ? 0.6f : (isEmpowered ? 4.2f : 2.5f);
                        rbEnemy.AddForce(pullDir * pullMagnitude, ForceMode2D.Impulse);
                    }

                    // CHỈ HÚT MÁU KHI ĐÁNH TRÚNG QUÁI VẬT VÀ BOSS (KHÔNG TÍCH NỘ LIÊN TỤC TRÊN TỪNG TICK LỐC XOÁY)
                    if (owner != null)
                    {
                        // Giảm tích nội năng từ lốc (chỉ 1.5f mỗi tick thay vì spam giáp hộ thân liên tục)
                        var energySys = owner.GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
                        if (energySys != null)
                        {
                            energySys.AddInnerEnergy(isEmpowered ? 1.5f : 1.0f);
                        }

                        var combat = owner.GetComponent<PlayerCombat>();
                        var health = owner.GetComponent<PlayerHealth>();
                        if (health != null && !health.IsDead)
                        {
                            // 1. HỒI 15% SÁT THƯƠNG LỐC GÂY RA THÀNH MÁU GỐC (Cân bằng lại so với mức 50% quá bá đạo)
                            int healAmount = Mathf.Max(1, Mathf.RoundToInt(tickDmg * 0.15f));
                            health.Heal(healAmount);

                            // 2. OVERHEAL: % Hút Máu từ Trang Bị / Ngọc Khảm chuyển hóa thành Máu Ảo (Virtual Health Shield)
                            if (combat != null && combat.LifestealPercent > 0f)
                            {
                                int virtualHpAmount = Mathf.Max(1, Mathf.RoundToInt(tickDmg * combat.LifestealPercent));
                                health.AddVirtualHealth(virtualHpAmount, 15f);
                            }
                        }
                    }

                    SoundManager.EnsureInstance()?.PlaySwordSlash();
                }
            }
        }
    }

    private void SpawnHitBurst(Vector2 pos)
    {
        GameObject burst = new GameObject(isEmpowered ? "Dune_CrimsonVortex_Burst" : "Dune_SandVortex_Burst");
        burst.transform.position = pos;
        burst.transform.rotation = Quaternion.identity;
        var bSr = burst.AddComponent<SpriteRenderer>();
        bSr.sortingLayerName = "Default";
        bSr.sortingOrder = 5060;
        bSr.color = isEmpowered ? new Color(1f, 0.22f, 0.22f, 1f) : Color.white;
        
        float scale = isEmpowered ? 3.3f : 2.4f;
        burst.transform.localScale = new Vector3(scale, scale, 1f);

        var anim = burst.AddComponent<TemporaryFrameAnimator>();
        anim.Play(vortexHitFrames, 0.04f);
    }
}

/// <summary>
/// Helper phát animation 1 lần rồi tự hủy
/// </summary>
public class TemporaryFrameAnimator : MonoBehaviour
{
    public void Play(Sprite[] frames, float interval)
    {
        StartCoroutine(Routine(frames, interval));
    }

    private IEnumerator Routine(Sprite[] frames, float interval)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (frames != null && sr != null)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                sr.sprite = frames[i];
                yield return new WaitForSeconds(interval);
            }
        }
        Destroy(gameObject);
    }
}
