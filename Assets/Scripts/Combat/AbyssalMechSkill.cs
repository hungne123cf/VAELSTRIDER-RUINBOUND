using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Kỹ năng Chiến đấu của Player 7 - Hải Thần Cơ Giáp (Abyssal Leviathan / Goldfish Mech)
/// Phân lớp: Đấu Sĩ Đại Dương / Mỏ Neo Thủy Triều & Tuyệt Kỹ Đại Hồng Thủy Cá Mập (Shark Tsunami Apocalypse)
/// </summary>
public class AbyssalMechSkill : MonoBehaviour
{
    [Header("--- THÔNG SỐ ĐÒN ĐÁNH MỎ NEO ĐẠI DƯƠNG ---")]
    public int baseAnchorDamage = 26;
    public float attackRange = 2.35f;
    public float attackAngle = 155f;
    public float attackCooldown = 0.34f;
    private float attackCooldownTimer = 0f;

    [Header("--- NỘI TẠI: GIÁP LẶN THÂM HẢI (ABYSSAL ARMOR) ---")]
    [Range(0f, 0.5f)]
    public float innateDamageReduction = 0.18f; // Giảm 18% sát thương nhận vào
    private float bubbleShieldTimer = 0f;
    public bool HasBubbleShield => bubbleShieldTimer > 0f;

    [Header("--- CHIÊU GỒNG: XOÁY THỦY TRIỀU & ĐỊA CHẤN MỎ NEO (HOLD CHARGE) ---")]
    public float minChargeDuration = 0.20f;
    public float maxChargeDuration = 1.0f;
    private bool isChargingVortex = false;
    private float currentChargeDuration = 0f;
    private Vector2 currentAimDir = Vector2.right;
    private GameObject vortexPullAuraObj = null;

    public bool IsChargingVortex => isChargingVortex;
    public float CurrentChargeRatio => Mathf.Clamp01(currentChargeDuration / maxChargeDuration);

    [Header("--- CHIÊU LƯỚT: LƯỚT SÓNG THÂM HẢI (HYDRO-THRUST SURGE) ---")]
    public float dashSpeedBoost = 1.30f; // Tăng 30% tốc chạy trên vệt nước
    private static readonly List<Vector3> activeWaterTrailPoints = new List<Vector3>();

    [Header("--- HỆ THỐNG NỘ ĐẠI DƯƠNG (TIDAL RAGE) ---")]
    [SerializeField] private float maxRage = 100f;
    [SerializeField] private float currentRage = 0f;
    public float MaxRage => maxRage;
    public float CurrentRage => currentRage;
    public float RageRatio => maxRage > 0f ? Mathf.Clamp01(currentRage / maxRage) : 0f;
    public bool IsRageFull => currentRage >= (maxRage - 0.01f);

    [Header("--- KỸ NĂNG 1: XOÁY NƯỚC MỎ NEO (ABYSSAL MAELSTROM) ---")]
    public float maelstromCooldown = 6.5f;
    private float maelstromCooldownTimer = 0f;
    public float maelstromRadius = 2.8f;
    public float maelstromDuration = 2.8f;

    [Header("--- TUYỆT KỸ: ĐẠI HỒNG THỦY CÁ MẬP (SHARK TSUNAMI) ---")]
    public float specialCooldown = 12.0f;
    private float specialCooldownTimer = 0f;
    public int baseTsunamiDamage = 42;
    public int baseSharkFinisherDamage = 125;

    [Header("--- TARGETING LAYERS ---")]
    public LayerMask enemyLayers;

    private PlayerCombat playerCombat;
    private PlayerHealth playerHealth;
    private PlayerController playerController;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private int comboStep = 0;
    private float lastComboTime = 0f;
    private bool isAttacking = false;
    private bool isExecutingSkill = false;
    private bool isPlayingHurt = false;
    private Coroutine activeActionRoutine = null;
    private GameObject bubbleShieldVisualObj = null;

    // 70 Frames từ goldfish_mech_atlas.png (128x128 mỗi frame)
    private static Sprite[] allFrames;
    private static Sprite[] idleFrames;   // 0..10 (11 frames)
    private static Sprite[] walkFrames;   // 11..14 (4 frames loop bước đi)
    private static Sprite[] atk1Frames;   // 16..25 (10 frames: Quất mỏ neo & nổ hào quang nước)
    private static Sprite[] atk2Frames;   // 26..35 (10 frames: Bổ & quét mỏ neo vòng cung qua đầu)
    private static Sprite[] atk3Frames;   // 36..39 (4 frames: Trảm thủy bổ dọc)
    private static Sprite[] hurtFrames;   // 40..44 (5 frames: Trúng đòn văng nước bình cá)
    private static Sprite[] deathFrames;  // 45..52 (8 frames: Cá vàng nhảy ra, giáp đổ sập)
    private static Sprite[] ultiFrames;   // 53..69 (17 frames: Tụ khí, phóng đại hồng thủy & cá mập)

    // Pixel Art VFX Spritesheets cho Sóng Thần + Cá Mập Bơi Trên Sóng + Xoáy Nước
    private static Sprite[] sharkTsunamiFrames; // 8 frames (160x128) Sóng Thần + Cá Mập cưỡi đỉnh sóng
    private static Sprite[] sharkOnlyFrames;    // 6 frames (96x64) Cá Mập Đại Dương bơi lướt
    private static Sprite[] whirlpoolFrames;    // 8 frames (128x128) Xoáy Nước Biển Sâu

    private float animTimer = 0f;
    private int animIndex = 0;

    public bool IsAttacking => isAttacking;
    public bool IsExecutingSkill => isExecutingSkill;
    public bool IsAttackReady => attackCooldownTimer <= 0f && !isAttacking && !isExecutingSkill && !isChargingVortex;
    public bool IsMaelstromReady => maelstromCooldownTimer <= 0f && !isExecutingSkill;
    public float MaelstromCooldownRemaining => Mathf.Max(0f, maelstromCooldownTimer);
    public float MaelstromCooldownRatio => maelstromCooldown > 0f ? Mathf.Clamp01(maelstromCooldownTimer / maelstromCooldown) : 0f;

    public bool IsSpecialReady => specialCooldownTimer <= 0f && !isExecutingSkill;
    public float SpecialCooldown => specialCooldown;
    public float SpecialCooldownRemaining => Mathf.Max(0f, specialCooldownTimer);
    public float SpecialCooldownRatio => specialCooldown > 0f ? Mathf.Clamp01(specialCooldownTimer / specialCooldown) : 0f;

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

        EnsureAllSpritesLoaded();
    }

    public static void EnsureAllSpritesLoaded()
    {
        if (idleFrames != null && idleFrames.Length > 0 && sharkTsunamiFrames != null && sharkTsunamiFrames.Length > 0)
            return;

        // 1. Nạp 70 frames từ goldfish_mech_atlas (10x7 grid = 1280x896, chuẩn Zero-Bug Android < 2048px)
        Texture2D atlasTex = Resources.Load<Texture2D>("Sprites/AbyssalMech/goldfish_mech_atlas");
        if (atlasTex != null)
        {
            atlasTex.filterMode = FilterMode.Point;
            allFrames = new Sprite[70];
            int cols = 10;
            int rows = 7;
            int cell = 128;
            for (int i = 0; i < 70; i++)
            {
                int r = i / cols;
                int c = i % cols;
                // Trong Unity Texture2D, gốc y=0 nằm ở đáy ảnh, hàng 0 của PNG nằm ở đỉnh ảnh (rows - 1 - r)
                int y = (rows - 1 - r) * cell;
                int x = c * cell;
                // Pivot (0.5f, 0.45f) và PPU = 36f giúp cỗ máy cao lớn uy vũ và chân đứng đúng mặt đất
                allFrames[i] = Sprite.Create(atlasTex, new Rect(x, y, cell, cell), new Vector2(0.50f, 0.44f), 36f);
                allFrames[i].name = $"GoldfishMech_{i}";
            }
        }
        else
        {
            // Fallback sang strip ngang nếu cần
            Texture2D stripTex = Resources.Load<Texture2D>("Sprites/AbyssalMech/goldfish_mech");
            if (stripTex != null)
            {
                stripTex.filterMode = FilterMode.Point;
                int cell = stripTex.height;
                int count = Mathf.Min(70, stripTex.width / Mathf.Max(1, cell));
                allFrames = new Sprite[count];
                for (int i = 0; i < count; i++)
                {
                    allFrames[i] = Sprite.Create(stripTex, new Rect(i * cell, 0, cell, cell), new Vector2(0.50f, 0.44f), 36f);
                    allFrames[i].name = $"GoldfishMech_{i}";
                }
            }
        }

        if (allFrames != null && allFrames.Length >= 70)
        {
            idleFrames  = SliceRange(allFrames, 0, 10);  // #0..#10 (11 frames)
            walkFrames  = SliceRange(allFrames, 11, 14); // #11..#14 (4 frames loop bước đi)
            atk1Frames  = SliceRange(allFrames, 16, 25); // #16..#25 (10 frames)
            atk2Frames  = SliceRange(allFrames, 26, 35); // #26..#35 (10 frames)
            atk3Frames  = SliceRange(allFrames, 36, 39); // #36..#39 (4 frames)
            hurtFrames  = SliceRange(allFrames, 40, 44); // #40..#44 (5 frames)
            deathFrames = SliceRange(allFrames, 45, 52); // #45..#52 (8 frames)
            ultiFrames  = SliceRange(allFrames, 53, 69); // #53..#69 (17 frames)
        }

        // 2. Nạp Spritesheet Sóng Thần + Cá Mập Bơi Trên Đỉnh Sóng (8 frames, 160x128)
        Texture2D tsunamiTex = Resources.Load<Texture2D>("Sprites/AbyssalMech/shark_tsunami_sheet");
        if (tsunamiTex != null)
        {
            tsunamiTex.filterMode = FilterMode.Point;
            int fw = 160;
            int fh = tsunamiTex.height;
            int count = Mathf.Max(1, tsunamiTex.width / fw);
            sharkTsunamiFrames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                sharkTsunamiFrames[i] = Sprite.Create(tsunamiTex, new Rect(i * fw, 0, fw, fh), new Vector2(0.5f, 0.38f), 28f);
                sharkTsunamiFrames[i].name = $"SharkTsunami_{i}";
            }
        }

        // 3. Nạp Spritesheet Cá Mập Đại Dương riêng lẻ (6 frames, 96x64)
        Texture2D sharkTex = Resources.Load<Texture2D>("Sprites/AbyssalMech/abyssal_shark_sheet");
        if (sharkTex != null)
        {
            sharkTex.filterMode = FilterMode.Point;
            int fw = 96;
            int fh = sharkTex.height;
            int count = Mathf.Max(1, sharkTex.width / fw);
            sharkOnlyFrames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                sharkOnlyFrames[i] = Sprite.Create(sharkTex, new Rect(i * fw, 0, fw, fh), new Vector2(0.5f, 0.5f), 32f);
                sharkOnlyFrames[i].name = $"AbyssalShark_{i}";
            }
        }

        // 4. Nạp Spritesheet Xoáy Nước Biển Sâu (8 frames, 128x128)
        Texture2D whirlTex = Resources.Load<Texture2D>("Sprites/AbyssalMech/abyssal_whirlpool_sheet");
        if (whirlTex != null)
        {
            whirlTex.filterMode = FilterMode.Point;
            int fw = 128;
            int fh = whirlTex.height;
            int count = Mathf.Max(1, whirlTex.width / fw);
            whirlpoolFrames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                whirlpoolFrames[i] = Sprite.Create(whirlTex, new Rect(i * fw, 0, fw, fh), new Vector2(0.5f, 0.5f), 36f);
                whirlpoolFrames[i].name = $"AbyssalWhirlpool_{i}";
            }
        }
    }

    private static Sprite[] SliceRange(Sprite[] src, int startInclusive, int endInclusive)
    {
        if (src == null || startInclusive < 0 || endInclusive >= src.Length || startInclusive > endInclusive)
            return new Sprite[0];
        int len = endInclusive - startInclusive + 1;
        Sprite[] res = new Sprite[len];
        for (int i = 0; i < len; i++)
        {
            res[i] = src[startInclusive + i];
        }
        return res;
    }

    public static Sprite[] GetIdleFrames()
    {
        EnsureAllSpritesLoaded();
        return idleFrames;
    }

    public static Sprite[] GetDeathFrames()
    {
        EnsureAllSpritesLoaded();
        return deathFrames;
    }

    public static Sprite GetActionIcon(MobileActionButton.ButtonType btnType)
    {
        EnsureAllSpritesLoaded();
        if (btnType == MobileActionButton.ButtonType.Attack)
        {
            if (atk1Frames != null && atk1Frames.Length > 3) return atk1Frames[3];
        }
        else if (btnType == MobileActionButton.ButtonType.Dash)
        {
            if (walkFrames != null && walkFrames.Length > 1) return walkFrames[1];
        }
        else if (btnType == MobileActionButton.ButtonType.Skill)
        {
            if (sharkTsunamiFrames != null && sharkTsunamiFrames.Length > 3) return sharkTsunamiFrames[3];
            if (ultiFrames != null && ultiFrames.Length > 13) return ultiFrames[13];
        }
        if (idleFrames != null && idleFrames.Length > 0) return idleFrames[0];
        return null;
    }

    public void AddRage(float amount)
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        bool wasFull = IsRageFull;
        currentRage = Mathf.Clamp(currentRage + amount, 0f, maxRage);
        if (!wasFull && IsRageFull)
        {
            SoundManager.EnsureInstance()?.PlayHeavenlyAscension();
            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.12f, 0.16f);
        }
    }

    public void ConsumeRage()
    {
        currentRage = 0f;
    }

    public int GetEffectiveAttackDamage()
    {
        if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();
        int bonus = playerCombat != null ? playerCombat.BonusAttackDamage : 0;
        return baseAnchorDamage + bonus;
    }

    void Update()
    {
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;
        if (maelstromCooldownTimer > 0f) maelstromCooldownTimer -= Time.deltaTime;
        if (specialCooldownTimer > 0f) specialCooldownTimer -= Time.deltaTime;

        if (bubbleShieldTimer > 0f)
        {
            bubbleShieldTimer -= Time.deltaTime;
            if (bubbleShieldTimer <= 0f && bubbleShieldVisualObj != null)
            {
                Destroy(bubbleShieldVisualObj);
                bubbleShieldVisualObj = null;
            }
        }
    }

    void LateUpdate()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (isAttacking || isExecutingSkill || isPlayingHurt || isChargingVortex || activeActionRoutine != null) return;

        if (animator != null && animator.enabled)
        {
            animator.enabled = false;
        }

        EnsureAllSpritesLoaded();
        bool isMoving = (playerController != null && playerController.IsMoving);
        Sprite[] frames = isMoving ? walkFrames : idleFrames;
        if (frames == null || frames.Length == 0) frames = idleFrames;
        if (frames == null || frames.Length == 0) return;

        float speedRatio = (playerController != null && playerController.MoveSpeed > 0.1f)
            ? (playerController.MoveSpeed / 4.6f)
            : 1.0f;
        float frameInterval = isMoving ? (0.10f / Mathf.Clamp(speedRatio, 0.6f, 2.5f)) : 0.11f;

        animTimer += Time.deltaTime;
        if (animTimer >= frameInterval)
        {
            animTimer = 0f;
            animIndex = (animIndex + 1) % frames.Length;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = frames[animIndex % frames.Length];
            UpdateFacingFlip();
        }
    }

    private void UpdateFacingFlip(Vector2? overrideDir = null)
    {
        if (spriteRenderer == null) return;
        if (overrideDir.HasValue && Mathf.Abs(overrideDir.Value.x) > 0.05f)
        {
            spriteRenderer.flipX = (overrideDir.Value.x < 0f);
            return;
        }
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

    public Vector2 GetFacingDirection()
    {
        if (playerController != null)
        {
            Vector2 dir = playerController.GetFacingDirection();
            if (dir.sqrMagnitude > 0.01f) return dir.normalized;
            float lastX = playerController.GetLastHorizontalFacing();
            if (Mathf.Abs(lastX) > 0.01f) return new Vector2(Mathf.Sign(lastX), 0f);
        }
        if (spriteRenderer != null && spriteRenderer.flipX) return Vector2.left;
        return Vector2.right;
    }

    public void PlayHurtVisual()
    {
        if (isExecutingSkill || isAttacking || (playerHealth != null && playerHealth.IsDead)) return;
        if (hurtFrames != null && hurtFrames.Length > 0 && gameObject.activeInHierarchy)
        {
            StartCoroutine(HurtVisualRoutine());
        }
    }

    private IEnumerator HurtVisualRoutine()
    {
        isPlayingHurt = true;
        for (int i = 0; i < hurtFrames.Length; i++)
        {
            if (isAttacking || isExecutingSkill || (playerHealth != null && playerHealth.IsDead)) break;
            if (spriteRenderer != null && hurtFrames[i] != null)
            {
                spriteRenderer.sprite = hurtFrames[i];
            }
            yield return new WaitForSeconds(0.055f);
        }
        isPlayingHurt = false;
    }

    // =========================================================================
    // 1.5. CHIÊU GỒNG: XOÁY THỦY TRIỀU & ĐỊA CHẤN MỎ NEO (HOLD CHARGE)
    // =========================================================================
    public void StartVortexCharge(Vector2 aimDir)
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        EnsureAllSpritesLoaded();

        isChargingVortex = true;
        currentChargeDuration = 0f;
        currentAimDir = (aimDir.sqrMagnitude > 0.01f) ? aimDir.normalized : GetFacingDirection();
        UpdateFacingFlip(currentAimDir);

        if (atk1Frames != null && atk1Frames.Length > 2 && spriteRenderer != null)
        {
            spriteRenderer.sprite = atk1Frames[2]; // Pose cắm neo gồng bọt nước
        }

        CreateVortexPullAura();
        SoundManager.EnsureInstance()?.PlaySwordSlash();
    }

    public void UpdateVortexCharge(Vector2 aimDir, float duration)
    {
        if (!isChargingVortex) return;
        currentChargeDuration = Mathf.Clamp(duration, 0f, maxChargeDuration);
        if (aimDir.sqrMagnitude > 0.01f)
        {
            currentAimDir = aimDir.normalized;
            UpdateFacingFlip(currentAimDir);
        }

        // Tạo vòng xoáy kéo hút quái vật trong bán kính 3.5m vào chân cơ giáp
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, 3.5f);
        foreach (var c in cols)
        {
            if (c == null || c.gameObject == gameObject || c.transform.IsChildOf(transform)) continue;
            var damageable = c.GetComponent<IDamageable>() ?? c.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable is PlayerHealth) continue;

            var rb = c.GetComponent<Rigidbody2D>() ?? c.GetComponentInParent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 pull = ((Vector2)transform.position - (Vector2)c.transform.position);
                if (pull.magnitude > 0.4f)
                {
                    rb.linearVelocity = pull.normalized * 2.8f;
                }
            }
        }

        if (vortexPullAuraObj != null)
        {
            vortexPullAuraObj.transform.Rotate(0f, 0f, -360f * Time.deltaTime);
            float scale = Mathf.Lerp(1.2f, 2.2f, CurrentChargeRatio);
            vortexPullAuraObj.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public void CancelVortexCharge()
    {
        isChargingVortex = false;
        currentChargeDuration = 0f;
        if (vortexPullAuraObj != null) Destroy(vortexPullAuraObj);
    }

    public void ReleaseVortexCharge(Vector2 aimDir, int bonusAtk)
    {
        if (!isChargingVortex) return;
        float ratio = CurrentChargeRatio;
        isChargingVortex = false;
        if (vortexPullAuraObj != null) Destroy(vortexPullAuraObj);

        if (aimDir.sqrMagnitude < 0.01f) aimDir = currentAimDir;
        else aimDir.Normalize();

        if (activeActionRoutine != null) StopCoroutine(activeActionRoutine);
        activeActionRoutine = StartCoroutine(ReleaseTitanicSmashRoutine(aimDir, ratio, bonusAtk));
    }

    private IEnumerator ReleaseTitanicSmashRoutine(Vector2 aimDir, float ratio, int bonusAtk)
    {
        isAttacking = true;
        UpdateFacingFlip(aimDir);

        // Chạy frame quất và bổ mỏ neo chấn địa
        if (atk2Frames != null && atk2Frames.Length > 0)
        {
            for (int i = 0; i < Mathf.Min(5, atk2Frames.Length); i++)
            {
                if (spriteRenderer != null) spriteRenderer.sprite = atk2Frames[i];
                yield return new WaitForSeconds(0.04f);
            }
        }

        // Kích hoạt nện bộc phá địa chấn nước
        SoundManager.EnsureInstance()?.PlayHitImpact();
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.18f + ratio * 0.15f, 0.22f);

        Vector3 smashCenter = transform.position + (Vector3)(aimDir * 1.5f);
        float smashRadius = Mathf.Lerp(2.2f, 3.4f, ratio);
        int damage = Mathf.RoundToInt((GetEffectiveAttackDamage() + bonusAtk) * Mathf.Lerp(1.6f, 2.8f, ratio));

        Collider2D[] hits = Physics2D.OverlapCircleAll(smashCenter, smashRadius);
        HashSet<GameObject> hitRoots = new HashSet<GameObject>();
        int hitCount = 0;

        foreach (var col in hits)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable is PlayerHealth) continue;

            GameObject rootObj = (damageable as Component)?.gameObject ?? col.gameObject;
            if (hitRoots.Contains(rootObj)) continue;
            hitRoots.Add(rootObj);

            int finalDmg = AugmentManager.ProcessOutgoingDamage(gameObject, rootObj, damage);
            damageable.TakeDamage(finalDmg);
            DamagePopup.Create(rootObj.transform.position, finalDmg, DamageType.TrueDamage, isCritical: (ratio >= 0.7f));
            AugmentManager.TriggerOnHitAugments(gameObject, rootObj, finalDmg, damageable);
            playerCombat?.ApplyLifesteal(finalDmg, DamageType.TrueDamage);

            // Hất tung kẻ địch lên cao
            var rb = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 push = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
                rb.AddForce((push + Vector2.up * 1.5f).normalized * (8f + ratio * 6f), ForceMode2D.Impulse);
            }

            hitCount++;
        }

        // Hồi 8% lượng máu đã mất nếu trúng từ 2 kẻ địch trở lên
        if (hitCount >= 2 && playerHealth != null && !playerHealth.IsDead)
        {
            int lostHp = Mathf.Max(0, playerHealth.MaxHealth - playerHealth.CurrentHealth);
            if (lostHp > 0) playerHealth.Heal(Mathf.Max(3, Mathf.RoundToInt(lostHp * 0.08f)));
            ActivateBubbleShield(3.0f);
        }

        AddRage(20f + hitCount * 6f);
        attackCooldownTimer = attackCooldown;
        isAttacking = false;
        activeActionRoutine = null;
    }

    private void CreateVortexPullAura()
    {
        if (vortexPullAuraObj != null) Destroy(vortexPullAuraObj);
        vortexPullAuraObj = new GameObject("AbyssalVortexPullAura");
        vortexPullAuraObj.transform.SetParent(transform, false);
        vortexPullAuraObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);

        var sr = vortexPullAuraObj.AddComponent<SpriteRenderer>();
        if (whirlpoolFrames != null && whirlpoolFrames.Length > 0)
        {
            sr.sprite = whirlpoolFrames[0];
        }
        sr.color = new Color(0.2f, 0.85f, 1f, 0.65f);
        sr.sortingOrder = 8;
    }

    // =========================================================================
    // 1.6. CHIÊU LƯỚT: LƯỚT SÓNG THÂM HẢI & VỆT NƯỚC TRƠN TRƯỢT (DASH)
    // =========================================================================
    public void OnPerformHydroDash(Vector2 dashDir)
    {
        if (dashDir.sqrMagnitude < 0.01f) dashDir = GetFacingDirection();
        dashDir.Normalize();

        ActivateBubbleShield(1.8f);
        AddRage(15f);

        // Tạo vệt nước trơn trượt tại vị trí lướt
        StartCoroutine(SpawnWaterTrailRoutine(transform.position, dashDir));
    }

    private IEnumerator SpawnWaterTrailRoutine(Vector3 startPos, Vector2 dir)
    {
        GameObject trailObj = new GameObject("AbyssalWaterTrail");
        trailObj.transform.position = startPos;
        var sr = trailObj.AddComponent<SpriteRenderer>();
        if (whirlpoolFrames != null && whirlpoolFrames.Length > 0)
        {
            sr.sprite = whirlpoolFrames[1];
        }
        sr.color = new Color(0.3f, 0.9f, 1f, 0.55f);
        sr.sortingOrder = 3; // Nằm sát sàn
        trailObj.transform.localScale = new Vector3(2.2f, 1.1f, 1f);

        float dur = 2.5f;
        float el = 0f;
        while (el < dur)
        {
            el += Time.deltaTime;
            // Làm chậm 40% quái vật dẫm lên vệt nước
            Collider2D[] cols = Physics2D.OverlapCircleAll(trailObj.transform.position, 1.8f);
            foreach (var c in cols)
            {
                if (c == null || c.gameObject == gameObject) continue;
                var damageable = c.GetComponent<IDamageable>() ?? c.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable is PlayerHealth) continue;

                GameObject rootObj = (damageable as Component)?.gameObject ?? c.gameObject;
                var debuff = rootObj.GetComponent<EnemySlowDebuff>();
                if (debuff == null) debuff = rootObj.AddComponent<EnemySlowDebuff>();
                if (debuff != null) debuff.ApplySlow(0.40f, 0.6f);
            }
            yield return new WaitForSeconds(0.2f);
        }

        if (trailObj != null) Destroy(trailObj);
    }

    // =========================================================================
    // 1. ĐÒN ĐÁNH THƯỜNG: CHUỖI COMBO 3 THỨC MỎ NEO ĐẠI DƯƠNG (#16..#39)
    // =========================================================================
    public bool TryExecuteAnchorAttack(Vector2 aimDir)
    {
        if (!IsAttackReady) return false;
        if (playerHealth != null && playerHealth.IsDead) return false;

        EnsureAllSpritesLoaded();
        if (Time.time - lastComboTime > 1.35f)
        {
            comboStep = 0;
        }

        int currentStep = comboStep;
        comboStep = (comboStep + 1) % 3;
        lastComboTime = Time.time;

        if (activeActionRoutine != null) StopCoroutine(activeActionRoutine);
        activeActionRoutine = StartCoroutine(AnchorAttackRoutine(currentStep, aimDir));
        return true;
    }

    private IEnumerator AnchorAttackRoutine(int step, Vector2 aimDir)
    {
        isAttacking = true;
        isPlayingHurt = false;
        if (animator != null) animator.enabled = false;

        if (aimDir.sqrMagnitude < 0.01f) aimDir = GetFacingDirection();
        else aimDir.Normalize();
        UpdateFacingFlip(aimDir);

        Sprite[] frames = (step == 0) ? atk1Frames : ((step == 1) ? atk2Frames : atk3Frames);
        float dmgMult = (step == 0) ? 1.0f : ((step == 1) ? 1.25f : 1.55f);
        int hitFrame = (step == 0) ? 3 : ((step == 1) ? 4 : 1);
        float frameTime = (step == 2) ? 0.055f : 0.042f;

        SoundManager.EnsureInstance()?.PlaySwordSlash();

        bool hasDealtDamage = false;
        if (frames != null && frames.Length > 0)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (spriteRenderer != null && frames[i] != null)
                {
                    spriteRenderer.sprite = frames[i];
                    UpdateFacingFlip(aimDir);
                }

                if (!hasDealtDamage && i >= hitFrame)
                {
                    hasDealtDamage = true;
                    PerformAnchorHitboxDamage(aimDir, dmgMult, is360Sweep: (step == 1), isHeavyCleave: (step == 2));
                }

                yield return new WaitForSeconds(frameTime);
            }
        }
        else
        {
            PerformAnchorHitboxDamage(aimDir, dmgMult, is360Sweep: (step == 1), isHeavyCleave: (step == 2));
            yield return new WaitForSeconds(0.25f);
        }

        attackCooldownTimer = attackCooldown;
        isAttacking = false;
        activeActionRoutine = null;
    }

    private void PerformAnchorHitboxDamage(Vector2 aimDir, float dmgMultiplier, bool is360Sweep, bool isHeavyCleave)
    {
        int rawDmg = Mathf.RoundToInt(GetEffectiveAttackDamage() * dmgMultiplier);
        float range = is360Sweep ? (attackRange * 1.15f) : attackRange;
        Vector3 center = transform.position + (Vector3)(aimDir * (is360Sweep ? 0.2f : 0.55f));

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, range);
        HashSet<GameObject> hitRoots = new HashSet<GameObject>();
        int hitCount = 0;

        foreach (var col in hits)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable is PlayerHealth) continue;

            GameObject rootObj = (damageable as Component)?.gameObject ?? col.gameObject;
            if (hitRoots.Contains(rootObj)) continue;

            if (!is360Sweep)
            {
                Vector2 toEnemy = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
                float angle = Vector2.Angle(aimDir, toEnemy);
                if (angle > attackAngle * 0.55f && Vector2.Distance(transform.position, col.transform.position) > 1.25f)
                    continue;
            }

            hitRoots.Add(rootObj);
            int finalDmg = AugmentManager.ProcessOutgoingDamage(gameObject, rootObj, rawDmg);
            damageable.TakeDamage(finalDmg);
            DamagePopup.Create(col.transform.position, finalDmg, isHeavyCleave ? DamageType.Magic : DamageType.Physical, isCritical: isHeavyCleave);
            AugmentManager.TriggerOnHitAugments(gameObject, rootObj, finalDmg, damageable);
            playerCombat?.ApplyLifesteal(finalDmg, DamageType.Physical);

            var rb = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 push = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
                rb.AddForce(push * (isHeavyCleave ? 7.5f : 4.5f), ForceMode2D.Impulse);
            }

            hitCount++;
        }

        if (hitCount > 0)
        {
            AddRage(15f + hitCount * 4f);
            var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (energySys != null)
            {
                energySys.RestoreEnergy(hitCount * 6f);
                energySys.AddInnerEnergy(hitCount * 8f);
            }

            // Đòn 3 hồi 4% máu đã mất khi bổ trúng kẻ địch
            if (isHeavyCleave && playerHealth != null && !playerHealth.IsDead)
            {
                int lostHp = Mathf.Max(0, playerHealth.MaxHealth - playerHealth.CurrentHealth);
                if (lostHp > 0) playerHealth.Heal(Mathf.Max(2, Mathf.RoundToInt(lostHp * 0.04f)));
            }

            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.08f, isHeavyCleave ? 0.14f : 0.08f);
        }
    }

    // =========================================================================
    // 2. KỸ NĂNG 1: XOÁY NƯỚC MỎ NEO & KHIÊN BONG BÓNG (ABYSSAL MAELSTROM)
    // =========================================================================
    public bool TryCastWhirlpoolAnchor(Vector2 aimDir)
    {
        if (!IsMaelstromReady) return false;
        if (playerHealth != null && playerHealth.IsDead) return false;

        EnsureAllSpritesLoaded();
        if (activeActionRoutine != null) StopCoroutine(activeActionRoutine);
        activeActionRoutine = StartCoroutine(WhirlpoolAnchorRoutine(aimDir));
        return true;
    }

    private IEnumerator WhirlpoolAnchorRoutine(Vector2 aimDir)
    {
        isExecutingSkill = true;
        isAttacking = false;
        maelstromCooldownTimer = maelstromCooldown;

        if (aimDir.sqrMagnitude < 0.01f) aimDir = GetFacingDirection();
        else aimDir.Normalize();
        UpdateFacingFlip(aimDir);

        SoundManager.EnsureInstance()?.PlaySwordSlash();
        ActivateBubbleShield(2.8f);
        AddRage(25f);

        Vector3 spawnPos = transform.position + (Vector3)(aimDir * 2.2f);
        StartCoroutine(SpawnWhirlpoolZoneRoutine(spawnPos));

        // Chạy hoạt ảnh tung mỏ neo nổ sóng nước (#16..#24)
        if (atk1Frames != null && atk1Frames.Length > 0)
        {
            for (int i = 0; i < atk1Frames.Length; i++)
            {
                if (spriteRenderer != null && atk1Frames[i] != null)
                {
                    spriteRenderer.sprite = atk1Frames[i];
                }
                if (i == 3)
                {
                    PerformAnchorHitboxDamage(aimDir, 1.35f, is360Sweep: true, isHeavyCleave: true);
                }
                yield return new WaitForSeconds(0.038f);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }

        isExecutingSkill = false;
        activeActionRoutine = null;
    }

    private void ActivateBubbleShield(float duration)
    {
        bubbleShieldTimer = duration;
        if (bubbleShieldVisualObj != null) Destroy(bubbleShieldVisualObj);

        bubbleShieldVisualObj = new GameObject("AbyssalBubbleShield");
        bubbleShieldVisualObj.transform.SetParent(transform, false);
        bubbleShieldVisualObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        bubbleShieldVisualObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

        var sr = bubbleShieldVisualObj.AddComponent<SpriteRenderer>();
        if (whirlpoolFrames != null && whirlpoolFrames.Length > 0)
        {
            sr.sprite = whirlpoolFrames[0];
        }
        sr.color = new Color(0.35f, 0.88f, 1f, 0.45f);
        sr.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 10) + 1;
    }

    private IEnumerator SpawnWhirlpoolZoneRoutine(Vector3 centerPos)
    {
        GameObject whirlObj = new GameObject("AbyssalWhirlpoolZone");
        whirlObj.transform.position = centerPos;
        whirlObj.transform.localScale = new Vector3(1.75f, 1.35f, 1f);

        var sr = whirlObj.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.55f, 0.95f, 1f, 0.90f);
        sr.sortingOrder = 4;

        float elapsed = 0f;
        float tickTimer = 0f;
        float frameTimer = 0f;
        int fIdx = 0;

        int tickDamage = Mathf.Max(8, Mathf.RoundToInt(GetEffectiveAttackDamage() * 0.48f));

        while (elapsed < maelstromDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            tickTimer += dt;
            frameTimer += dt;

            if (whirlpoolFrames != null && whirlpoolFrames.Length > 0 && frameTimer >= 0.055f)
            {
                frameTimer = 0f;
                fIdx = (fIdx + 1) % whirlpoolFrames.Length;
                sr.sprite = whirlpoolFrames[fIdx];
            }

            whirlObj.transform.Rotate(0f, 0f, -260f * dt);

            if (tickTimer >= 0.35f)
            {
                tickTimer = 0f;
                Collider2D[] cols = Physics2D.OverlapCircleAll(centerPos, maelstromRadius);
                HashSet<GameObject> hitRoots = new HashSet<GameObject>();
                foreach (var col in cols)
                {
                    if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
                    var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                    if (damageable == null || damageable is PlayerHealth) continue;

                    GameObject rootObj = (damageable as Component)?.gameObject ?? col.gameObject;
                    if (hitRoots.Contains(rootObj)) continue;
                    hitRoots.Add(rootObj);

                    // Kéo hút nhẹ kẻ địch vào tâm xoáy nước
                    Vector3 pullDir = (centerPos - rootObj.transform.position);
                    if (pullDir.magnitude > 0.35f)
                    {
                        rootObj.transform.position += pullDir.normalized * 0.38f;
                    }

                    int actualDmg = AugmentManager.ProcessOutgoingDamage(gameObject, rootObj, tickDamage);
                    damageable.TakeDamage(actualDmg);
                    DamagePopup.Create(rootObj.transform.position, actualDmg, DamageType.Magic, isCritical: false);
                }
            }

            yield return null;
        }

        if (whirlObj != null) Destroy(whirlObj);
    }

    // =========================================================================
    // 3. TUYỆT KỸ ULTIMATE: ĐẠI HỒNG THỦY CÁ MẬP (SHARK TSUNAMI APOCALYPSE)
    // Sử dụng trọn bộ 17 frames (#53..#69) + Sóng Thần Pixel & Cá Mập Bơi Trên Sóng
    // =========================================================================
    public bool TryExecuteSharkTsunami(Vector2 aimDir)
    {
        if (!IsSpecialReady) return false;
        if (playerHealth != null && playerHealth.IsDead) return false;

        EnsureAllSpritesLoaded();
        ConsumeRage();
        specialCooldownTimer = specialCooldown;

        if (activeActionRoutine != null) StopCoroutine(activeActionRoutine);
        activeActionRoutine = StartCoroutine(SharkTsunamiUltimateRoutine(aimDir));
        return true;
    }

    private IEnumerator SharkTsunamiUltimateRoutine(Vector2 aimDir)
    {
        isExecutingSkill = true;
        isAttacking = false;
        if (animator != null) animator.enabled = false;

        if (aimDir.sqrMagnitude < 0.01f) aimDir = GetFacingDirection();
        else aimDir.Normalize();
        UpdateFacingFlip(aimDir);

        // Bật khiên bong bóng bảo vệ trong suốt quá trình tung Ultimate
        ActivateBubbleShield(3.5f);
        SoundManager.EnsureInstance()?.PlayHeavenlyAscension();

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.25f, 0.22f);

        bool spawnedTsunami = false;

        // Diễn hoạt trọn vẹn 17 frames Ultimate (#53..#69) của Goldfish Mech
        if (ultiFrames != null && ultiFrames.Length > 0)
        {
            for (int i = 0; i < ultiFrames.Length; i++)
            {
                if (spriteRenderer != null && ultiFrames[i] != null)
                {
                    spriteRenderer.sprite = ultiFrames[i];
                    UpdateFacingFlip(aimDir);
                }

                // Tại frame thứ 7 (#59 -> #60: khi bộ giáp giơ tay phóng đại hồng thủy),
                // lập tức phóng Cơn Sóng Thần Pixel Art khổng lồ có Cá Mập bơi trên đỉnh sóng!
                if (!spawnedTsunami && i >= 7)
                {
                    spawnedTsunami = true;
                    StartCoroutine(SpawnSharkTsunamiWaveRoutine(transform.position, aimDir));
                }

                yield return new WaitForSeconds(0.055f);
            }
        }
        else
        {
            StartCoroutine(SpawnSharkTsunamiWaveRoutine(transform.position, aimDir));
            yield return new WaitForSeconds(0.8f);
        }

        isExecutingSkill = false;
        activeActionRoutine = null;
    }

    /// <summary>
    /// Triệu hồi Cơn Sóng Thần Pixel Art khổng lồ kèm Cá Mập Đại Dương bơi cưỡi ngay trên ngọn sóng,
    /// cuốn phăng kẻ địch theo hướng chỉ định và kết thúc bằng cú đớp hàm cá mập chí mạng!
    /// </summary>
    private IEnumerator SpawnSharkTsunamiWaveRoutine(Vector3 originPos, Vector2 waveDir)
    {
        if (waveDir.sqrMagnitude < 0.01f) waveDir = Vector2.right;
        waveDir.Normalize();

        bool flipLeft = (waveDir.x < -0.05f);
        float rotAngle = 0f;
        if (Mathf.Abs(waveDir.y) > 0.45f && Mathf.Abs(waveDir.x) < 0.85f)
        {
            rotAngle = Mathf.Atan2(waveDir.y, Mathf.Abs(waveDir.x) > 0.1f ? Mathf.Abs(waveDir.x) : 1f) * Mathf.Rad2Deg * 0.45f;
            if (flipLeft) rotAngle = -rotAngle;
        }

        // 1. Tạo Object Chính: ĐẠI HỒNG THỦY + CÁ MẬP CƯỠI ĐỈNH SÓNG (sharkTsunamiFrames)
        GameObject tsunamiRoot = new GameObject("PixelSharkTsunamiWave");
        tsunamiRoot.transform.position = originPos + (Vector3)(waveDir * 0.8f);
        tsunamiRoot.transform.rotation = Quaternion.Euler(0f, 0f, rotAngle);
        tsunamiRoot.transform.localScale = new Vector3(1.55f, 1.55f, 1f);

        var mainSr = tsunamiRoot.AddComponent<SpriteRenderer>();
        mainSr.sortingOrder = 25;
        mainSr.flipX = flipLeft;
        if (sharkTsunamiFrames != null && sharkTsunamiFrames.Length > 0)
        {
            mainSr.sprite = sharkTsunamiFrames[0];
        }

        // 2. Tạo 2 Cá Mập Hộ Vệ Pixel Art (sharkOnlyFrames) bơi lướt kèm theo ở 2 bên sườn ngọn sóng thần!
        Vector2 perp = new Vector2(-waveDir.y, waveDir.x);
        GameObject[] escortSharks = new GameObject[2];
        SpriteRenderer[] escortSrs = new SpriteRenderer[2];
        for (int s = 0; s < 2; s++)
        {
            escortSharks[s] = new GameObject($"EscortPixelShark_{s}");
            escortSharks[s].transform.SetParent(tsunamiRoot.transform, false);
            float sideOffset = (s == 0) ? 0.85f : -0.85f;
            escortSharks[s].transform.localPosition = new Vector3(s == 0 ? 0.35f : -0.25f, sideOffset, 0f);
            escortSharks[s].transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            escortSrs[s] = escortSharks[s].AddComponent<SpriteRenderer>();
            escortSrs[s].sortingOrder = 26 + s;
            escortSrs[s].flipX = flipLeft;
            if (sharkOnlyFrames != null && sharkOnlyFrames.Length > 0)
            {
                escortSrs[s].sprite = sharkOnlyFrames[s % sharkOnlyFrames.Length];
            }
        }

        float travelDuration = 1.45f;
        float travelSpeed = 7.8f;
        float elapsed = 0f;
        float animTick = 0f;
        float damageTick = 0f;
        int frameCursor = 0;

        int waveHitDamage = Mathf.Max(22, baseTsunamiDamage + Mathf.RoundToInt(GetEffectiveAttackDamage() * 0.85f));
        int sharkBiteFinisher = Mathf.Max(80, baseSharkFinisherDamage + Mathf.RoundToInt(GetEffectiveAttackDamage() * 2.2f));

        while (elapsed < travelDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            animTick += dt;
            damageTick += dt;

            // Di chuyển cả khối Sóng Thần + Đàn Cá Mập băng qua màn hình
            tsunamiRoot.transform.position += (Vector3)(waveDir * (travelSpeed * dt));

            // Cập nhật hoạt ảnh Pixel Art Sóng Thần & Cá Mập bơi nhấp nhô trên sóng
            if (animTick >= 0.055f)
            {
                animTick = 0f;
                frameCursor++;

                if (sharkTsunamiFrames != null && sharkTsunamiFrames.Length > 0)
                {
                    mainSr.sprite = sharkTsunamiFrames[frameCursor % sharkTsunamiFrames.Length];
                }

                for (int s = 0; s < 2; s++)
                {
                    if (escortSrs[s] != null && sharkOnlyFrames != null && sharkOnlyFrames.Length > 0)
                    {
                        escortSrs[s].sprite = sharkOnlyFrames[(frameCursor + s * 2) % sharkOnlyFrames.Length];
                        float bob = Mathf.Sin((elapsed * 10f) + s * 2.5f) * 0.18f;
                        float baseSide = (s == 0) ? 0.85f : -0.85f;
                        escortSharks[s].transform.localPosition = new Vector3(s == 0 ? 0.35f : -0.25f, baseSide + bob, 0f);
                    }
                }
            }

            // Gây sát thương sóng cuốn + cá mập cắn xé và cuốn kẻ địch trôi theo ngọn sóng
            if (damageTick >= 0.22f)
            {
                damageTick = 0f;
                Vector3 waveCenter = tsunamiRoot.transform.position;
                Collider2D[] cols = Physics2D.OverlapCircleAll(waveCenter, 2.65f);
                HashSet<GameObject> hitRoots = new HashSet<GameObject>();

                foreach (var col in cols)
                {
                    if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
                    var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                    if (damageable == null || damageable is PlayerHealth) continue;

                    GameObject rootObj = (damageable as Component)?.gameObject ?? col.gameObject;
                    if (hitRoots.Contains(rootObj)) continue;
                    hitRoots.Add(rootObj);

                    int actualDmg = AugmentManager.ProcessOutgoingDamage(gameObject, rootObj, waveHitDamage);
                    damageable.TakeDamage(actualDmg);
                    DamagePopup.Create(rootObj.transform.position, actualDmg, DamageType.Magic, isCritical: true);
                    AugmentManager.TriggerOnHitAugments(gameObject, rootObj, actualDmg, damageable);
                    playerCombat?.ApplyLifesteal(actualDmg, DamageType.Magic);

                    // Cuốn kẻ địch trôi theo ngọn sóng thần (không đẩy xuyên tường)
                    bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null);
                    if (!isProp)
                    {
                        rootObj.transform.position += (Vector3)(waveDir * 0.55f);
                    }
                }
            }

            yield return null;
        }

        // Đòn kết liễu: CÁ MẬP ĐẠI DƯƠNG ĐỚP CHÍ MẠNG & NỔ SÓNG THẦN CUỐI HÀNH TRÌNH
        Vector3 finalPos = tsunamiRoot.transform.position;
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.28f, 0.26f);
        SoundManager.EnsureInstance()?.PlaySwordSlash();

        Collider2D[] finalHits = Physics2D.OverlapCircleAll(finalPos, 3.2f);
        HashSet<GameObject> finalRoots = new HashSet<GameObject>();
        foreach (var col in finalHits)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable is PlayerHealth) continue;

            GameObject rootObj = (damageable as Component)?.gameObject ?? col.gameObject;
            if (finalRoots.Contains(rootObj)) continue;
            finalRoots.Add(rootObj);

            int actualDmg = AugmentManager.ProcessOutgoingDamage(gameObject, rootObj, sharkBiteFinisher);
            damageable.TakeDamage(actualDmg);
            DamagePopup.Create(rootObj.transform.position, actualDmg, DamageType.TrueDamage, isCritical: true);
            AugmentManager.TriggerOnHitAugments(gameObject, rootObj, actualDmg, damageable);
            playerCombat?.ApplyLifesteal(actualDmg, DamageType.TrueDamage);
        }

        // Fade out mượt mà trong 0.2s
        float fade = 0f;
        while (fade < 0.2f)
        {
            fade += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (fade / 0.2f));
            if (mainSr != null) mainSr.color = new Color(1f, 1f, 1f, alpha);
            for (int s = 0; s < 2; s++)
            {
                if (escortSrs[s] != null) escortSrs[s].color = new Color(1f, 1f, 1f, alpha);
            }
            yield return null;
        }

        if (tsunamiRoot != null) Destroy(tsunamiRoot);
    }
}
