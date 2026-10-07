using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GaleRoninSkill : MonoBehaviour
{
    [Header("Gale Ronin Attack Settings")]
    [SerializeField] private float slashRadius = 1.30f; // Tăng tầm đánh xa hơn một chút
    [SerializeField] private float slashAngle = 180f; // Góc chém bán nguyệt 180 độ
    [SerializeField] private int baseSlashDamage = 28; // Sát thương uy lực 28 chuẩn 1:1 cho đòn đánh chậm Iaido
    [SerializeField] private float attackCooldown = 1.20f; // Tốc độ chém chậm hơn rõ rệt (1.2s), đầm tay chuẩn Iaido Samurai
    [SerializeField] private LayerMask targetLayers;

    public void SetBaseSlashDamage(int dmg) => baseSlashDamage = dmg;

    [Header("Passive: Wind Mark (Phong Ấn)")]
    [SerializeField] private int maxMarkStacks = 3;
    [SerializeField] private float burstDamageMultiplier = 2.4f;
    [SerializeField] private float vortexDuration = 1.3f;
    [SerializeField] private float vortexRadius = 1.6f; // Thu gọn bán kính lốc xoáy cho vừa vặn, chuẩn xác

    [Header("Special Skill: Bão Phong Trảm (Phantom Cleave)")]
    [SerializeField] private float specialCooldown = 15.0f;
    [SerializeField] private int phantomSlashCount = 4;
    [SerializeField] private int basePhantomDamage = 26;

    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float attackCooldownTimer = 0f;
    private float specialCooldownTimer = 0f;
    private bool isPhantomActive = false;

    // Quản lý tầng Phong Ấn trên từng mục tiêu
    private Dictionary<GameObject, int> windMarks = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, float> markExpiryTimes = new Dictionary<GameObject, float>();

    public bool IsAttackReady => attackCooldownTimer <= 0f;
    public bool IsSpecialReady => specialCooldownTimer <= 0f;
    public float SpecialCooldown => specialCooldown;
    public float SpecialCooldownRatio => Mathf.Clamp01(specialCooldownTimer / specialCooldown);
    public float SpecialCooldownRemaining => Mathf.Max(0f, specialCooldownTimer);

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
        playerHealth = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (targetLayers.value == 0)
        {
            targetLayers = ~0; // Target everything damageable
        }
        RefreshCooldownStats();
    }

    void Start()
    {
        RefreshCooldownStats();
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
            totalCdr += 15.0f * gemCdr;
        }

        specialCooldown = Mathf.Max(5.0f, 15.0f - totalCdr);
    }

    void Update()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (specialCooldownTimer > 0f)
        {
            specialCooldownTimer -= Time.deltaTime;
        }

        // Dọn dẹp các mục tiêu đã hết hạn dấu ấn (sau 6s không bị đánh)
        CleanupExpiredMarks();

        // PC Key test
        if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            TriggerSpecialSkill();
        }
    }

    private void CleanupExpiredMarks()
    {
        if (windMarks.Count == 0) return;

        List<GameObject> toRemove = new List<GameObject>();
        float now = Time.time;
        foreach (var kvp in markExpiryTimes)
        {
            if (kvp.Key == null || now > kvp.Value)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var k in toRemove)
        {
            windMarks.Remove(k);
            markExpiryTimes.Remove(k);
        }
    }

    /// <summary>
    /// Thực hiện đòn đánh thường: Nhát chém bán nguyệt quét 180 độ
    /// </summary>
    public bool PerformAttack(Vector2 facingDir, int bonusAtkDamage)
    {
        if (!IsAttackReady || isPhantomActive) return false;

        attackCooldownTimer = attackCooldown;

        if (facingDir.sqrMagnitude < 0.001f) facingDir = Vector2.down;
        facingDir = facingDir.normalized;

        // Hoạt ảnh chém
        if (animator != null)
        {
            animator.SetFloat("MoveX", facingDir.x);
            animator.SetFloat("MoveY", facingDir.y);
            animator.SetTrigger("AttackTrigger");
        }

        SoundManager.EnsureInstance()?.PlaySwordSlash();

        // Quét hình quạt 180 độ vừa vặn với hoạt ảnh chém với tầm vươn xa hơn
        Vector2 origin = (Vector2)transform.position + facingDir * 0.55f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, slashRadius, targetLayers);

        int totalBaseDamage = baseSlashDamage + bonusAtkDamage;
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

            // Kiểm tra góc nón 180 độ (Dot product >= -0.15f)
            Vector2 toTarget = ((Vector2)col.transform.position - origin).normalized;
            float dot = Vector2.Dot(facingDir, toTarget);

            if (dot >= -0.15f || Vector2.Distance(origin, col.transform.position) <= slashRadius * 0.55f)
            {
                HitTargetWithWindMark(rootObj, damageable, totalBaseDamage, facingDir);
                bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
                if (!isProp)
                {
                    hitEnemyCount++;
                }
            }
        }

        // Hồi năng lượng & Tích lũy 50% Tiến Trình Nội Năng + Giảm 20% Cooldown Tuyệt Kỹ CHỈ KHI chém trúng quái vật thực sự!
        if (hitEnemyCount > 0)
        {
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (energySys != null)
            {
                energySys.RestoreEnergy(15f);
                energySys.AddInnerEnergy(50f); // +50% tiến trình Nội Năng (2 đòn đánh trúng = 100% nộ)
            }

            // Giảm 20% Thời gian hồi Chiêu Cuối (Ultimate CD)
            float cdReduction = specialCooldown * 0.20f;
            if (specialCooldownTimer > 0f)
            {
                specialCooldownTimer = Mathf.Max(0f, specialCooldownTimer - cdReduction);
                DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, LocalizationManager.T("⚡ +50% NỘ | -20% HỒI CHIÊU CUỐI!", "⚡ +50% RAGE | -20% ULT CD!"), new Color(0.1f, 1f, 0.85f));
            }
            else
            {
                DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, LocalizationManager.T("⚡ +50% NỘ!", "⚡ +50% RAGE!"), new Color(0.1f, 1f, 0.85f));
            }

            if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null)
            {
                int healAmount = Mathf.Max(1, Mathf.RoundToInt(totalBaseDamage * playerCombat.LifestealPercent));
                playerHealth.Heal(healAmount);
            }
        }

        return true;
    }

    private void HitTargetWithWindMark(GameObject targetRoot, IDamageable damageable, int damage, Vector2 facingDir)
    {
        // 1. Tích tầng Phong Ấn
        int currentStacks = 0;
        windMarks.TryGetValue(targetRoot, out currentStacks);
        currentStacks++;
        markExpiryTimes[targetRoot] = Time.time + 6.0f;

        SoundManager.EnsureInstance()?.PlayHitImpact();

        // 2. Nếu đạt 3 tầng: PHONG ẤN BỘC PHÁ!
        if (currentStacks >= maxMarkStacks)
        {
            windMarks[targetRoot] = 0;
            int burstDamage = Mathf.RoundToInt(damage * burstDamageMultiplier);
            damageable.TakeDamage(burstDamage);

            Vector3 targetPos = targetRoot.transform.position;
            DamagePopup.Create(targetPos, burstDamage, isCritical: true);
            DamagePopup.CreateText(targetPos + Vector3.up * 1.3f, LocalizationManager.T("💥 PHONG ẤN BỘC PHÁ!", "💥 SEAL BURST!"), new Color(0.1f, 1f, 0.75f));

            // Triệu hồi Lốc Xoáy Phong Lôi (Mini Gale Vortex)
            SpawnMiniGaleVortex(targetPos, Mathf.RoundToInt(damage * 0.6f));
        }
        else
        {
            windMarks[targetRoot] = currentStacks;
            damageable.TakeDamage(damage);
            DamagePopup.Create(targetRoot.transform.position, damage, isCritical: false);

            string markText = (currentStacks == 1) 
                ? LocalizationManager.T("🌀 Ấn I", "🌀 Mark I") 
                : LocalizationManager.T("🌀🌀 Ấn II", "🌀🌀 Mark II");
            DamagePopup.CreateText(targetRoot.transform.position + Vector3.up * 1.1f, markText, new Color(0.4f, 0.9f, 1f));
        }

        // Hơi hất lùi quái vật
        var rb = targetRoot.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(facingDir * 4.5f, ForceMode2D.Impulse);
        }
    }

    /// <summary>
    /// Triệu hồi Lốc Xoáy Mini giữ chân và gây sát thương liên tục
    /// </summary>
    private void SpawnMiniGaleVortex(Vector3 centerPos, int tickDamage)
    {
        GameObject vortex = new GameObject("MiniGaleVortex");
        vortex.transform.position = centerPos;
        StartCoroutine(GaleVortexRoutine(vortex, centerPos, tickDamage));
    }

    private IEnumerator GaleVortexRoutine(GameObject vortex, Vector3 center, int tickDamage)
    {
        float elapsed = 0f;
        float tickTimer = 0f;

        while (elapsed < vortexDuration)
        {
            elapsed += Time.deltaTime;
            tickTimer += Time.deltaTime;

            // Hút các quái vật xung quanh vào tâm
            Collider2D[] cols = Physics2D.OverlapCircleAll(center, vortexRadius, targetLayers);
            foreach (var c in cols)
            {
                if (c == null || c.gameObject == gameObject) continue;
                var dmg = c.GetComponent<IDamageable>();
                if (dmg != null && c.attachedRigidbody != null)
                {
                    Vector2 pullDir = ((Vector2)center - (Vector2)c.transform.position).normalized;
                    c.attachedRigidbody.AddForce(pullDir * 6.5f, ForceMode2D.Force);
                }
            }

            // Gây sát thương mỗi 0.35s
            if (tickTimer >= 0.35f)
            {
                tickTimer = 0f;
                foreach (var c in cols)
                {
                    if (c == null || c.gameObject == gameObject) continue;
                    var dmg = c.GetComponent<IDamageable>();
                    if (dmg != null)
                    {
                        dmg.TakeDamage(tickDamage);
                        DamagePopup.Create(c.transform.position, tickDamage, isCritical: false);
                    }
                }
            }

            yield return null;
        }

        if (vortex != null) Destroy(vortex);
    }

    /// <summary>
    /// Tìm mục tiêu gần nhất hoặc Boss trong bán kính quét
    /// </summary>
    public Transform FindBestTarget(float searchRadius, out IDamageable outDamageable)
    {
        outDamageable = null;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRadius, targetLayers);
        Transform targetTransform = null;
        float closestDist = float.MaxValue;
        bool isBossTarget = false;

        foreach (var col in colliders)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (dmg == null || dmg is PlayerHealth) continue;

            bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
            if (isProp) continue;

            // Lấy chính xác Transform của Enemy (không lấy root vì root có thể là dungeon container)
            Transform enemyT = (dmg as Component)?.transform ?? col.transform;
            float dist = Vector2.Distance(transform.position, enemyT.position);

            bool isBoss = enemyT.GetComponent<ValleyGolemBossAI>() != null || enemyT.GetComponentInParent<ValleyGolemBossAI>() != null || 
                          enemyT.GetComponent<SkeletonKingBossAI>() != null || enemyT.GetComponentInParent<SkeletonKingBossAI>() != null || 
                          enemyT.name.Contains("Boss");
            if (isBoss)
            {
                if (!isBossTarget || dist < closestDist)
                {
                    isBossTarget = true;
                    closestDist = dist;
                    targetTransform = enemyT;
                    outDamageable = dmg;
                }
            }
            else if (!isBossTarget && dist < closestDist)
            {
                closestDist = dist;
                targetTransform = enemyT;
                outDamageable = dmg;
            }
        }

        return targetTransform;
    }

    /// <summary>
    /// Tuyệt Kỹ: Ám Ảnh Trảm (Shadow Execution Strike)
    /// Dịch chuyển ra sau lưng kẻ địch, tung đòn chém chí mạng rồi biến về vị trí ban đầu.
    /// </summary>
    public bool TriggerSpecialSkill()
    {
        if (specialCooldownTimer > 0f || isPhantomActive)
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, LocalizationManager.T("ĐANG HỒI CHIÊU!", "COOLDOWN!"), Color.yellow);
            return false;
        }

        Transform targetT = FindBestTarget(3.5f, out var targetDmg); // Thu gọn tầm khóa mục tiêu về 3.5m (khoảng 3 ô gạch sàn)
        if (targetT == null || targetDmg == null)
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, LocalizationManager.T("KHÔNG CÓ MỤC TIÊU GẦN!", "NO TARGET NEARBY!"), Color.yellow);
            return false;
        }

        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null && !energySys.TryConsumeEnergy(40f))
        {
            return false;
        }

        RefreshCooldownStats();
        specialCooldownTimer = specialCooldown;
        StartCoroutine(PhantomCleaveRoutine(targetT, targetDmg));
        return true;
    }

    private IEnumerator PhantomCleaveRoutine(Transform targetTransform, IDamageable targetDamageable)
    {
        if (targetTransform == null || targetDamageable == null)
        {
            yield break;
        }

        isPhantomActive = true;

        if (playerHealth != null) playerHealth.isInvulnerable = true;
        if (playerController != null) playerController.SetCanMove(false);

        Vector3 startPos = transform.position;
        Vector3 targetPos = targetTransform.position;
        Vector2 dirToTarget = ((Vector2)targetPos - (Vector2)startPos).normalized;
        if (dirToTarget.sqrMagnitude < 0.001f) dirToTarget = Vector2.down;

        // Vị trí sau lưng địch (cách tâm quái 1.15m về phía đối diện)
        Vector3 behindPos = targetPos + (Vector3)(dirToTarget * 1.15f);

        var cam = FindAnyObjectByType<CameraFollow>();

        // Ảo ảnh để lại tại vị trí ban đầu
        SpawnShadowClone(startPos, new Color(0.2f, 0.7f, 1f, 0.75f));
        SoundManager.EnsureInstance()?.PlaySwordSlash();

        // 2. BƯỚC 1: DỊCH CHUYỂN RA SAU LƯNG ĐỊCH
        transform.position = behindPos;

        // Quay mặt vào lưng địch
        Vector2 strikeFacing = -dirToTarget;
        if (animator != null)
        {
            animator.SetFloat("MoveX", strikeFacing.x);
            animator.SetFloat("MoveY", strikeFacing.y);
            animator.Play("Attack1", 0, 0f); // Chuẩn bị tư thế rút kiếm áp sát
        }

        DamagePopup.CreateText(behindPos + Vector3.up * 1.6f, LocalizationManager.T("★ ÁP SÁT LƯNG ĐỊCH! ★", "★ BEHIND ENEMY! ★"), new Color(0.2f, 1f, 0.9f));
        if (cam != null) cam.Shake(0.12f, 0.18f);

        // DỪNG LẠI 0.35s ĐỂ NGƯỜI CHƠI NHÌN THẤY RÕ RÀNG HÀNH ĐỘNG XUẤT HIỆN SAU LƯNG
        yield return new WaitForSeconds(0.35f);

        // 3. BƯỚC 2: TUNG ĐÒN CHÉM KẾT LIỄU CHÍ MẠNG (SHADOW EXECUTION SLICE)
        if (animator != null)
        {
            animator.SetTrigger("AttackTrigger");
            animator.Play("Attack2", 0, 0f);
        }

        SoundManager.EnsureInstance()?.PlaySwordSlash();

        // Đợi 0.12s để hoạt ảnh vung kiếm chém tới điểm va chạm
        yield return new WaitForSeconds(0.12f);

        // Tính sát thương tuyệt kỹ (x3 sát thương cơ bản + bonus nâng cấp)
        int upUltBonus = 0;
        var upMgr = PlayerUpgradeManager.EnsureInstance();
        if (upMgr != null) upUltBonus = upMgr.GetUltiDamageBonus();

        int totalStrikeDmg = (basePhantomDamage * 3) + (playerCombat != null ? playerCombat.BonusAttackDamage * 2 : 0) + upUltBonus;

        bool targetWasAliveBefore = (targetDamageable != null && targetTransform != null);
        if (targetDamageable != null && targetTransform != null)
        {
            targetDamageable.TakeDamage(totalStrikeDmg);
            DamagePopup.Create(targetTransform.position, totalStrikeDmg, isCritical: true);
            DamagePopup.CreateText(targetTransform.position + Vector3.up * 1.8f, LocalizationManager.T("💥 ÁM ẢNH BẠO KÍCH! 💥", "💥 SHADOW CRIT! 💥"), new Color(1f, 0.25f, 0.25f));

            var targetRb = targetTransform.GetComponent<Rigidbody2D>();
            if (targetRb != null)
            {
                targetRb.AddForce(dirToTarget * 8.5f, ForceMode2D.Impulse);
            }
        }

        // Gây sát thương quét phụ (Splash Damage) gọn gàng cho quái vật đứng ngay cạnh mục tiêu chính
        Collider2D[] splashHits = Physics2D.OverlapCircleAll(targetPos, 1.0f, targetLayers);
        foreach (var sh in splashHits)
        {
            if (sh == null || sh.gameObject == gameObject || (targetTransform != null && sh.transform.IsChildOf(targetTransform))) continue;
            var dmg = sh.GetComponent<IDamageable>() ?? sh.GetComponentInParent<IDamageable>();
            if (dmg != null && !(dmg is PlayerHealth))
            {
                bool isProp = (sh.GetComponent<BreakableProp>() != null || sh.GetComponentInParent<BreakableProp>() != null);
                if (!isProp)
                {
                    int splashDmg = Mathf.RoundToInt(totalStrikeDmg * 0.5f);
                    dmg.TakeDamage(splashDmg);
                    DamagePopup.Create(sh.transform.position, splashDmg, isCritical: false);
                }
            }
        }

        // KIỂM TRA NẾU KẾT LIỄU ĐƯỢC MỤC TIÊU: HỒI NGAY CHIÊU CUỐI!
        if (targetWasAliveBefore && IsTargetKilled(targetTransform, targetDamageable))
        {
            specialCooldownTimer = 0f;
            SoundManager.EnsureInstance()?.PlayPortalSpawn();
            DamagePopup.CreateText(transform.position + Vector3.up * 1.8f, LocalizationManager.T("⚡ KẾT LIỄU: HỒI CHIÊU CUỐI! ⚡", "⚡ EXECUTE: ULT RESET! ⚡"), new Color(0.2f, 1f, 0.4f));
        }

        SoundManager.EnsureInstance()?.PlayHitImpact();
        if (cam != null) cam.Shake(0.35f, 0.35f);

        // Hồi năng lượng & Tích lũy Nội Năng lớn cho Player
        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null)
        {
            energySys.RestoreEnergy(15f);
            energySys.AddInnerEnergy(30f);
        }

        if (playerCombat != null && playerCombat.LifestealPercent > 0f && playerHealth != null)
        {
            int healAmount = Mathf.Max(1, Mathf.RoundToInt(totalStrikeDmg * playerCombat.LifestealPercent));
            playerHealth.Heal(healAmount);
        }

        // DỪNG LẠI 0.45s ĐỂ NGƯỜI CHƠI NHÌN THẤY HOẠT ẢNH CHÉM VÀ SÁT THƯƠNG NẢY LÊN
        yield return new WaitForSeconds(0.45f);

        // 4. BƯỚC 3: DỊCH CHUYỂN TRỞ VỀ VỊ TRÍ CŨ
        SpawnShadowClone(behindPos, new Color(0.1f, 0.4f, 0.8f, 0.75f));
        transform.position = startPos;

        if (animator != null)
        {
            animator.SetFloat("MoveX", dirToTarget.x);
            animator.SetFloat("MoveY", dirToTarget.y);
        }

        DamagePopup.CreateText(startPos + Vector3.up * 1.3f, LocalizationManager.T("THU KIẾM!", "SHEATHE!"), new Color(1f, 0.85f, 0.35f));
        SoundManager.EnsureInstance()?.PlaySwordSlash();

        yield return new WaitForSeconds(0.18f);

        if (playerHealth != null) playerHealth.isInvulnerable = false;
        if (playerController != null) playerController.SetCanMove(true);

        isPhantomActive = false;
    }

    private void SpawnShadowClone(Vector3 pos, Color tintColor)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        GameObject ghost = new GameObject("ShadowStrikeGhost");
        ghost.transform.position = pos;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();
        ghostSr.sprite = spriteRenderer.sprite;
        ghostSr.flipX = spriteRenderer.flipX;
        ghostSr.sortingLayerID = spriteRenderer.sortingLayerID;
        ghostSr.sortingOrder = spriteRenderer.sortingOrder - 1;
        ghostSr.color = tintColor;

        StartCoroutine(FadeShadowCloneRoutine(ghost, ghostSr));
    }

    private IEnumerator FadeShadowCloneRoutine(GameObject ghostObj, SpriteRenderer ghostSr)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Color startColor = (ghostSr != null) ? ghostSr.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (ghostSr != null)
            {
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
                ghostSr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }

        if (ghostObj != null) Destroy(ghostObj);
    }

    /// <summary>
    /// Giảm thời gian hồi Tuyệt Kỹ khi Dash (-2.0s)
    /// </summary>
    public void ReduceSpecialCooldown(float seconds)
    {
        if (specialCooldownTimer > 0f)
        {
            specialCooldownTimer = Mathf.Max(0f, specialCooldownTimer - seconds);
            DamagePopup.CreateText(transform.position + Vector3.up * 1.3f, LocalizationManager.T($"⚡ -{seconds:0.#}s HỒI CHIÊU!", $"⚡ -{seconds:0.#}s CD!"), new Color(0.2f, 0.9f, 1f));
        }
    }

    private bool IsTargetKilled(Transform targetT, IDamageable targetDmg)
    {
        if (targetT == null) return true;

        var comp = targetDmg as Component;
        if (comp == null || !comp.gameObject.activeInHierarchy) return true;

        var col = targetT.GetComponent<Collider2D>() ?? comp.GetComponent<Collider2D>();
        if (col == null || !col.enabled) return true;

        return false;
    }

    void OnDrawGizmosSelected()
    {
        Vector2 facing = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
        Vector2 origin = (Vector2)transform.position + facing * 0.45f;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.85f);
        Gizmos.DrawWireSphere(origin, slashRadius);
    }
}
