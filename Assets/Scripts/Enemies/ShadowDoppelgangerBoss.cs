using UnityEngine;
using Action = System.Action;
using Random = UnityEngine.Random;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// BÓNG MA BẢN NGÃ (Shadow Doppelgänger Boss) - Ác Mộng Thực Sự Của Cõi Hư Không (Map 3):
/// 1. Chỉ xuất hiện duy nhất ở Map 3 của Màn Ác Mộng (VoidNightmare Mode) khi Skeleton King ngã xuống!
/// 2. Sao chép 100% Model, Sprite, Animation và Hero Class mà người chơi đang điều khiển:
///    - Dune Assassin: Song đao cát, combo 1_atk, 2_atk, 3_atk, lộn nhào roll, Bão Cát Hư Không sp_atk.
///    - Celestial Mage: Scale 0.62x, trượng tinh vân, bắn Cầu Tinh Tú Hư Không đuổi mục tiêu, Thiên Thạch Ma Trận.
///    - Gale Ronin: Chém bán nguyệt 180°, Phong Lôi Trảm, Lướt chém xuyên không gian.
///    - Azure Blade: Song kiếm băng tuyết, Hàn băng trảm, Thế thủ kiếm thuẫn & Phản kích hoàng kim.
///    - Titan Berserker: Vung đại đao/chùy đập đất, Địa chấn tận diệt.
///    - Vaelstrider: Kiếm thuật kỵ sĩ tàn tích, Bão lốc xoáy hư không.
/// 3. HỆ THỐNG MÁU 3 PHASE CÓ SỰ KHÁC BIỆT ĐÁNG KỂ NHÌN RÕ:
///    - Phase 1: Bản Ngã Đối Thủ (1.0x) - Tím nhạt huyền bí, đòn đánh cơ bản + lướt né.
///    - Phase 2: Hư Không Thức Tỉnh (1.25x) - Tím thẫm rực sáng, đôi mắt đỏ rực, vòng ma pháp Hư Không xoay dưới chân, tốc độ +30%, mở khóa Tuyệt Kỹ Class.
///    - Phase 3: Bản Ngã Tối Thượng BERSERK (1.45x) - Đỏ đen Hắc Ám Huyết Nguyệt rực lửa, 4 Quả Cầu Lửa Hư Không xoay quanh người, tốc độ +55%, cuồng nộ liên hoàn chiêu!
/// 4. ĐÃ BỎ TOÀN BỘ CHỮ TEXT DAME (Crit, Độc, Vết Thương Sâu...) chỉ hiển thị số sát thương với màu đặc trưng!
/// 5. Khi tiêu diệt xong Phase 3: Mới chính thức xuất hiện NPC Shop và Cổng Không Gian!
/// </summary>
public class ShadowDoppelgangerBoss : MonoBehaviour, IDamageable
{
    private static ShadowDoppelgangerBoss activeBoss;
    public static ShadowDoppelgangerBoss ActiveBoss => activeBoss;

    [Header("Boss Identity")]
    private string copiedHeroId = "vaelstrider";
    private string heroDisplayName = "Vaelstrider";
    private float baseHeroScale = 1.0f;
    private int currentPhase = 3; // 3 -> 2 -> 1 (Phase I -> Phase II -> Phase III)
    private int maxHealth = 700;
    private int currentHealth = 700;
    private int baseAttackDamage = 18;
    private float baseMoveSpeed = 4.8f;
    private float currentMoveSpeed = 4.8f;
    private bool isDead = false;
    private bool isInvulnerable = false;
    private bool isAttacking = false;
    private bool isParrying = false;
    private bool wasParryTriggered = false;

    [Header("References")]
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D myCol;
    private Animator animator;
    private Action onDefeatedCallback;

    [Header("Visual Effects")]
    private GameObject eyeHolder;
    private GameObject voidAuraRing;
    private GameObject abyssalFlamesHolder;
    private GameObject parryShieldVisual;

    [Header("Combat Timers")]
    private float attackTimer = 0.2f;
    private float attackCooldown = 0.85f;
    private float dashTimer = 1.2f;
    private float dashCooldown = 3.6f;
    private float skillTimer = 3.0f;
    private float skillCooldown = 6.0f;
    private float parryTimer = 5.0f;
    private float parryCooldown = 8.5f;

    // Sprite Animation cho Dune Assassin (Player 5)
    private static Sprite[] duneIdleFrames;
    private static Sprite[] duneRunFrames;
    private static Sprite[] duneAtk1Frames;
    private static Sprite[] duneAtk2Frames;
    private static Sprite[] duneAtk3Frames;
    private static Sprite[] duneSpAtkFrames;
    private static Sprite[] duneRollFrames;

    // Sprite Animation cho Celestial Mage (Player 6)
    private static Sprite[] mageIdleFrames;
    private static Sprite[] mageWalkFrames;
    private static Sprite[] mageAttackFrames;
    private static Sprite[] mageDashFrames;

    private float customAnimTimer = 0f;
    private int customAnimIndex = 0;
    private bool isPlayingCustomAction = false;

    // Hashes for Animator (Hero 1, 2, 3, 4)
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int ComboStepHash = Animator.StringToHash("ComboStep");

    public bool IsDead => isDead;
    public int CurrentPhase => currentPhase;

    public static ShadowDoppelgangerBoss Spawn(Vector3 spawnPos, Action onDefeated = null)
    {
        GameObject go = new GameObject("Boss_ShadowDoppelganger");
        go.transform.position = spawnPos;

        var boss = go.AddComponent<ShadowDoppelgangerBoss>();
        boss.onDefeatedCallback = onDefeated;
        boss.InitializeMirrorBoss();
        return boss;
    }

    private void InitializeMirrorBoss()
    {
        activeBoss = this;
        gameObject.tag = "Enemy";
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) gameObject.layer = enemyLayer;

        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var cap = gameObject.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(1.1f, 1.4f);
        cap.direction = CapsuleDirection2D.Vertical;
        myCol = cap;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5000;

        // 1. Nhận diện Hero Class mà người chơi đang chọn
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.enabled = true;
            player.SetCanMove(true);

            playerTransform = player.transform;
            playerHealth = player.GetComponent<PlayerHealth>();

            var pSr = player.GetComponent<SpriteRenderer>();
            if (pSr != null)
            {
                sr.sprite = pSr.sprite;
                sr.material = pSr.material;
                sr.sortingLayerID = pSr.sortingLayerID;
            }

            DetectHeroClass(player);

            // Nạp hoạt ảnh Sprite frames đặc thù cho các class không dùng Animator
            if (copiedHeroId == "dune")
            {
                LoadDuneSprites();
                baseHeroScale = 1.0f;
                if (duneIdleFrames != null && duneIdleFrames.Length > 0) sr.sprite = duneIdleFrames[0];
            }
            else if (copiedHeroId == "mage")
            {
                LoadMageSprites();
                baseHeroScale = 0.62f; // Tỉ lệ chuẩn của Celestial Mage
                if (mageIdleFrames != null && mageIdleFrames.Length > 0) sr.sprite = mageIdleFrames[0];
            }
            else
            {
                baseHeroScale = 1.0f;
                var pAnim = player.GetComponent<Animator>();
                if (pAnim != null && pAnim.runtimeAnimatorController != null)
                {
                    animator = gameObject.AddComponent<Animator>();
                    animator.runtimeAnimatorController = pAnim.runtimeAnimatorController;
                    animator.applyRootMotion = false;
                }
            }

            // Sao chép các bộ phận đồ họa phụ (Vũ khí, áo choàng, skin) nếu có (LOẠI TRỪ SHIELD)
            for (int i = 0; i < player.transform.childCount; i++)
            {
                var child = player.transform.GetChild(i);
                if (child.name.Contains("Shield") || child.GetComponent<PlayerShieldVisual>() != null) continue;

                if (child.name.Contains("Weapon") || child.name.Contains("Skin") || child.name.Contains("Costume") || child.name.Contains("Armor"))
                {
                    var copyChild = Instantiate(child.gameObject, transform);
                    copyChild.name = child.name;
                    copyChild.transform.localPosition = child.localPosition;
                    copyChild.transform.localRotation = child.localRotation;
                    copyChild.transform.localScale = child.localScale;
                }
            }

            // Chỉ số máu và tốc độ (Cân bằng Boss gấp 5 lần Player cả về HP lẫn Sát thương)
            int pMaxHp = playerHealth != null ? playerHealth.MaxHealth : MapScalingManager.GetPlayerMaxHealth();
            maxHealth = MapScalingManager.ScaleBossHealth(pMaxHp, phases: 3);
            currentHealth = maxHealth;
            baseMoveSpeed = player.MoveSpeed * 1.05f;
            currentMoveSpeed = baseMoveSpeed;
            baseAttackDamage = MapScalingManager.ScaleBossDamage(20, isSpecialSkill: false);
        }

        // Bắt đầu ở Phase 1: Bản Ngã Đối Thủ
        currentPhase = 3; // 3 bars: Phase 1
        ApplyPhase1Aesthetics();

        // Hiển thị thanh máu Boss 3 Phase
        UpdateBossHealthUI();

        // Xuất hiện hào quang bùng nổ
        StartCoroutine(IntroShockwaveRoutine());
    }

    private void DetectHeroClass(PlayerController player)
    {
        string selectedId = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";

        if ((player.GetComponent<DuneAssassinSkill>() != null && player.GetComponent<DuneAssassinSkill>().enabled) || selectedId == CharacterManager.CHAR_DUNE_ASSASSIN)
        {
            copiedHeroId = "dune";
            heroDisplayName = LocalizationManager.T("Sa Mạc Thích Khách", "Dune Assassin");
        }
        else if ((player.GetComponent<CelestialMageSkill>() != null && player.GetComponent<CelestialMageSkill>().enabled) || selectedId == CharacterManager.CHAR_CELESTIAL_MAGE)
        {
            copiedHeroId = "mage";
            heroDisplayName = LocalizationManager.T("Pháp Sư Tinh Tú", "Celestial Mage");
        }
        else if ((player.GetComponent<TitanBerserkerSkill>() != null && player.GetComponent<TitanBerserkerSkill>().enabled) || selectedId == CharacterManager.CHAR_TITAN_BERSERKER)
        {
            copiedHeroId = "titan";
            heroDisplayName = LocalizationManager.T("Đấu Sĩ Titan", "Titan Berserker");
        }
        else if ((player.GetComponent<AzureBladeSkill>() != null && player.GetComponent<AzureBladeSkill>().enabled) || selectedId == CharacterManager.CHAR_AZURE_BLADE)
        {
            copiedHeroId = "azure";
            heroDisplayName = LocalizationManager.T("Lam Kiếm Sĩ", "Azure Blade");
        }
        else if ((player.GetComponent<GaleRoninSkill>() != null && player.GetComponent<GaleRoninSkill>().enabled) || selectedId == CharacterManager.CHAR_GALE_RONIN)
        {
            copiedHeroId = "ronin";
            heroDisplayName = LocalizationManager.T("Phong Lôi Lãng Khách", "Gale Ronin");
        }
        else
        {
            copiedHeroId = "vaelstrider";
            heroDisplayName = LocalizationManager.T("Kỵ Sĩ Tàn Tích", "Vaelstrider");
        }
    }

    private void LoadDuneSprites()
    {
        if (duneIdleFrames != null && duneIdleFrames.Length > 0) return;
        duneIdleFrames = LoadSpritesFromFolder("Assets/ART/Player5/PNG/idle");
        duneRunFrames = LoadSpritesFromFolder("Assets/ART/Player5/PNG/run");
        duneAtk1Frames = LoadSpritesFromFolder("Assets/ART/Player5/PNG/1_atk");
        duneAtk2Frames = LoadSpritesFromFolder("Assets/ART/Player5/PNG/2_atk");
        duneAtk3Frames = LoadSpritesFromFolder("Assets/ART/Player5/PNG/3_atk");
        duneSpAtkFrames = LoadSpritesFromFolder("Assets/ART/Player5/PNG/sp_atk");
        duneRollFrames = LoadSpritesFromFolder("Assets/ART/Player5/PNG/roll");
    }

    private void LoadMageSprites()
    {
        if (mageIdleFrames != null && mageIdleFrames.Length > 0) return;
        mageIdleFrames = LoadSpritesFromFolder("Assets/ART/Player6/sample(idle&walk)/idle");
        if (mageIdleFrames == null || mageIdleFrames.Length == 0)
        {
            mageIdleFrames = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player6/sample(idle&walk)/idle/sprite sheets/idle.png", 46, 55);
        }
        mageWalkFrames = LoadSpritesFromFolder("Assets/ART/Player6/sample(idle&walk)/walk");
        if (mageWalkFrames == null || mageWalkFrames.Length == 0)
        {
            mageWalkFrames = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player6/sample(idle&walk)/walk/sprite sheets/walk.png", 46, 55);
        }
        mageAttackFrames = LoadSpritesFromFolder("Assets/ART/Player6/sample(idle&walk)/attack");
        mageDashFrames = LoadSpritesFromFolder("Assets/ART/Player6/sample(idle&walk)/dash");
    }

    private static Sprite[] LoadSpritesFromFolder(string folderPath)
    {
        List<Sprite> list = new List<Sprite>();
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        System.Array.Sort(guids, (a, b) => {
            string pA = UnityEditor.AssetDatabase.GUIDToAssetPath(a);
            string pB = UnityEditor.AssetDatabase.GUIDToAssetPath(b);
            return string.Compare(pA, pB, System.StringComparison.Ordinal);
        });
        foreach (var guid in guids)
        {
            string p = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (sp != null) list.Add(sp);
        }
#endif
        return list.ToArray();
    }

    private void UpdateBossHealthUI()
    {
        string title = LocalizationManager.T($"BÓNG MA BẢN NGÃ - {heroDisplayName.ToUpper()}", $"SHADOW DOPPELGÄNGER - {heroDisplayName.ToUpper()}");
        BossHealthBarUI.EnsureInstance()?.ShowBoss(title, 3);
        BossHealthBarUI.EnsureInstance()?.UpdateHealth(currentHealth, maxHealth, currentPhase);
    }

    #region Phase Aesthetics & Transformations

    /// <summary>
    /// Phase 1: Bản Ngã Đối Thủ - Tông màu tím nhạt bí ẩn, kích thước chuẩn 1.0x
    /// </summary>
    private void ApplyPhase1Aesthetics()
    {
        transform.localScale = Vector3.one * (baseHeroScale * 1.0f);
        if (sr != null) sr.color = new Color(0.92f, 0.85f, 1f, 1f);
        currentMoveSpeed = baseMoveSpeed;
    }

    /// <summary>
    /// Phase 2: Hư Không Thức Tỉnh - Phóng to 1.25x rõ rệt, tím thẫm hư không rực sáng, mắt quỷ đỏ rực, vòng ma pháp dưới chân
    /// </summary>
    private void ApplyPhase2Aesthetics()
    {
        transform.localScale = Vector3.one * (baseHeroScale * 1.25f);
        if (sr != null) sr.color = new Color(0.55f, 0.20f, 0.95f, 1f);

        currentMoveSpeed = baseMoveSpeed * 1.30f; // Tăng +30% tốc độ chạy
        attackCooldown = 0.60f;
        dashCooldown = 2.4f;
        skillCooldown = 4.2f;

        SpawnCrimsonEyes();
        SpawnVoidAuraUnderFeet();
    }

    /// <summary>
    /// Phase 3: Bản Ngã Tối Thượng (BERSERK) - Phóng to 1.45x khổng lồ áp đảo, đỏ đen Hắc Ám Huyết Nguyệt rực lửa, 4 quả cầu lửa hư không xoay quanh
    /// </summary>
    private void ApplyPhase3Aesthetics()
    {
        transform.localScale = Vector3.one * (baseHeroScale * 1.45f);
        if (sr != null) sr.color = new Color(0.90f, 0.10f, 0.25f, 1.0f);

        currentMoveSpeed = baseMoveSpeed * 1.55f; // Tăng +55% tốc độ chạy
        attackCooldown = 0.42f;
        dashCooldown = 1.6f;
        skillCooldown = 3.2f;

        SpawnCrimsonEyes();
        SpawnVoidAuraUnderFeet();
        SpawnAbyssalFireballs();
    }

    private void SpawnCrimsonEyes()
    {
        if (eyeHolder != null) Destroy(eyeHolder);
        eyeHolder = new GameObject("ShadowEyesFX");
        eyeHolder.transform.SetParent(transform, false);
        eyeHolder.transform.localPosition = new Vector3(0f, 0.55f, 0f);

        for (int i = -1; i <= 1; i += 2)
        {
            GameObject eye = new GameObject($"Eye_{i}");
            eye.transform.SetParent(eyeHolder.transform, false);
            eye.transform.localPosition = new Vector3(i * 0.13f, 0f, 0f);
            var eyeSr = eye.AddComponent<SpriteRenderer>();
            eyeSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
            eyeSr.color = new Color(1.0f, 0.05f, 0.15f, 1.0f); // Bright Glowing Crimson Red
            eyeSr.sortingOrder = 5200;
            eye.transform.localScale = Vector3.one * 0.18f;
        }
    }

    private void SpawnVoidAuraUnderFeet()
    {
        if (voidAuraRing != null) Destroy(voidAuraRing);
        voidAuraRing = new GameObject("VoidAuraRing");
        voidAuraRing.transform.SetParent(transform, false);
        voidAuraRing.transform.localPosition = new Vector3(0f, -0.65f, 0f);

        var aSr = voidAuraRing.AddComponent<SpriteRenderer>();
        aSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        aSr.color = new Color(0.45f, 0.08f, 0.75f, 0.40f);
        aSr.sortingOrder = 4950;
        voidAuraRing.transform.localScale = new Vector3(2.4f, 1.1f, 1f);
    }

    private void SpawnAbyssalFireballs()
    {
        if (abyssalFlamesHolder != null) Destroy(abyssalFlamesHolder);
        abyssalFlamesHolder = new GameObject("AbyssalFireballsHolder");
        abyssalFlamesHolder.transform.SetParent(transform, false);
        abyssalFlamesHolder.transform.localPosition = Vector3.zero;

        for (int i = 0; i < 4; i++)
        {
            float ang = (i / 4f) * 360f;
            GameObject fb = new GameObject($"Fireball_{i}");
            fb.transform.SetParent(abyssalFlamesHolder.transform, false);
            fb.transform.localPosition = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * 1.55f, Mathf.Sin(ang * Mathf.Deg2Rad) * 1.55f, 0f);

            var fSr = fb.AddComponent<SpriteRenderer>();
            fSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
            fSr.color = new Color(1.0f, 0.15f, 0.25f, 0.95f);
            fSr.sortingOrder = 5300;
            fb.transform.localScale = Vector3.one * 0.42f;
        }
    }

    private IEnumerator IntroShockwaveRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(0.6f);

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.4f, 0.6f);

        SoundManager.EnsureInstance()?.PlayBossWarning();

        GameObject wave = new GameObject("IntroShockwave");
        wave.transform.position = transform.position;
        var wSr = wave.AddComponent<SpriteRenderer>();
        wSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        wSr.color = new Color(0.75f, 0.25f, 1f, 0.85f);
        wSr.sortingOrder = 5100;

        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float r = Mathf.Lerp(0.5f, 4.5f, t / 0.35f);
            wave.transform.localScale = new Vector3(r, r, 1f);
            wSr.color = new Color(0.75f, 0.25f, 1f, Mathf.Lerp(0.85f, 0f, t / 0.35f));
            yield return null;
        }
        Destroy(wave);
        isInvulnerable = false;
    }

    #endregion

    void Update()
    {
        if (isDead || isInvulnerable) return;

        // Xoay 4 quả cầu lửa quanh Boss ở Phase 3
        if (abyssalFlamesHolder != null)
        {
            abyssalFlamesHolder.transform.Rotate(0f, 0f, 180f * Time.deltaTime);
        }
        if (voidAuraRing != null)
        {
            voidAuraRing.transform.Rotate(0f, 0f, -60f * Time.deltaTime);
        }

        if (playerTransform == null)
        {
            var p = FindAnyObjectByType<PlayerController>();
            if (p != null) playerTransform = p.transform;
            return;
        }

        Vector2 toPlayer = (playerTransform.position - transform.position);
        float distToPlayer = toPlayer.magnitude;
        Vector2 dirToPlayer = distToPlayer > 0.01f ? (toPlayer / distToPlayer) : Vector2.down;

        if (sr != null)
        {
            if (copiedHeroId == "mage")
            {
                sr.flipX = (dirToPlayer.x > 0);
            }
            else
            {
                sr.flipX = (dirToPlayer.x < 0);
            }
        }

        if (animator != null)
        {
            animator.SetFloat(MoveXHash, dirToPlayer.x);
            animator.SetFloat(MoveYHash, dirToPlayer.y);
        }

        if (isAttacking || isParrying) return;

        attackTimer -= Time.deltaTime;
        dashTimer -= Time.deltaTime;
        skillTimer -= Time.deltaTime;
        parryTimer -= Time.deltaTime;

        // 1. THI TRIỂN TUYỆT KỸ CỦA LỚP HERO
        if (skillTimer <= 0f && distToPlayer <= 4.8f)
        {
            StartCoroutine(ExecuteHeroSignatureSkillRoutine(dirToPlayer));
            return;
        }

        // 2. THẾ THỦ KIẾM THUẪN & PHẢN KÍCH (Chỉ ở Phase 2 và 3 cho Kiếm sĩ)
        if ((copiedHeroId == "vaelstrider" || copiedHeroId == "azure") && currentPhase <= 2 && parryTimer <= 0f && distToPlayer <= 2.5f && Random.value < 0.60f)
        {
            StartCoroutine(PerformDivineParryStanceRoutine(dirToPlayer));
            return;
        }

        // 3. LƯỚT NÉ ĐÒN HOẶC ÁP SÁT NHANH
        if (dashTimer <= 0f && (distToPlayer > 4.5f || (distToPlayer < 1.8f && Random.value < 0.35f)))
        {
            StartCoroutine(PerformBladeDashRoutine(dirToPlayer));
            return;
        }

        // 4. TIẾP CẬN & ĐÁNH THƯỜNG THEO CLASS HERO
        float attackReach = (copiedHeroId == "mage") ? 5.5f : 1.75f;
        if (distToPlayer <= attackReach)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetFloat(SpeedHash, 0f);

            if (attackTimer <= 0f)
            {
                StartCoroutine(PerformHeroBasicAttackComboRoutine(dirToPlayer));
            }
        }
        else
        {
            rb.linearVelocity = dirToPlayer * currentMoveSpeed;
            if (animator != null) animator.SetFloat(SpeedHash, currentMoveSpeed);
        }
    }

    void LateUpdate()
    {
        if (isDead) return;
        if (isPlayingCustomAction) return;

        // Quản lý hoạt ảnh di chuyển/đứng yên cho Dune Assassin & Celestial Mage (không dùng Animator)
        if (copiedHeroId == "dune")
        {
            bool isMoving = (rb != null && rb.linearVelocity.sqrMagnitude > 0.05f);
            Sprite[] frames = isMoving ? duneRunFrames : duneIdleFrames;
            float frameInterval = isMoving ? 0.075f : 0.11f;

            if (frames != null && frames.Length > 0)
            {
                customAnimTimer += Time.deltaTime;
                if (customAnimTimer >= frameInterval)
                {
                    customAnimTimer = 0f;
                    customAnimIndex = (customAnimIndex + 1) % frames.Length;
                }
                if (sr != null) sr.sprite = frames[customAnimIndex % frames.Length];
            }
        }
        else if (copiedHeroId == "mage")
        {
            bool isMoving = (rb != null && rb.linearVelocity.sqrMagnitude > 0.05f);
            Sprite[] frames = isMoving ? mageWalkFrames : mageIdleFrames;
            float frameInterval = isMoving ? 0.075f : 0.10f;

            if (frames != null && frames.Length > 0)
            {
                customAnimTimer += Time.deltaTime;
                if (customAnimTimer >= frameInterval)
                {
                    customAnimTimer = 0f;
                    customAnimIndex = (customAnimIndex + 1) % frames.Length;
                }
                if (sr != null) sr.sprite = frames[customAnimIndex % frames.Length];
            }
        }
    }

    #region Hero-Specific Attack Combos & Skills

    private IEnumerator PerformHeroBasicAttackComboRoutine(Vector2 dir)
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = Vector2.zero;

        int currentDmg = baseAttackDamage;
        if (currentPhase == 2) currentDmg = Mathf.RoundToInt(baseAttackDamage * 1.30f);
        else if (currentPhase == 1) currentDmg = Mathf.RoundToInt(baseAttackDamage * 1.60f);

        if (copiedHeroId == "dune")
        {
            yield return StartCoroutine(PerformDuneAttackComboRoutine(dir, currentDmg));
        }
        else if (copiedHeroId == "mage")
        {
            yield return StartCoroutine(PerformMageAttackRoutine(dir, currentDmg));
        }
        else
        {
            yield return StartCoroutine(PerformStandardMeleeComboRoutine(dir, currentDmg));
        }

        isAttacking = false;
    }

    private IEnumerator PerformDuneAttackComboRoutine(Vector2 dir, int dmg)
    {
        isPlayingCustomAction = true;

        // Đòn 1: Nhích tới, chém dao 1
        if (duneAtk1Frames != null && duneAtk1Frames.Length > 0)
        {
            SoundManager.EnsureInstance()?.PlaySwordSlash(1);
            SpawnHeroSlashFX(transform.position + (Vector3)dir * 0.9f, dir, isHeavy: false);

            rb.linearVelocity = dir * currentMoveSpeed * 1.3f;
            for (int i = 0; i < duneAtk1Frames.Length; i++)
            {
                if (sr != null) sr.sprite = duneAtk1Frames[i];
                if (i == 1 && playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 2.3f)
                {
                    playerHealth?.TakeDamage(dmg);
                    DamagePopup.Create(playerTransform.position + Vector3.up * 0.8f, dmg, isPlayer: true);
                    SoundManager.EnsureInstance()?.PlayHitImpact();
                }
                yield return new WaitForSeconds(0.045f);
            }
            rb.linearVelocity = Vector2.zero;
        }

        yield return new WaitForSeconds(0.12f);

        // Đòn 2: Nhích tới chém dao 2
        if (duneAtk2Frames != null && duneAtk2Frames.Length > 0 && playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 2.6f)
        {
            Vector2 newDir = (playerTransform.position - transform.position).normalized;
            SoundManager.EnsureInstance()?.PlaySwordSlash(2);
            SpawnHeroSlashFX(transform.position + (Vector3)newDir * 1.1f, newDir, isHeavy: true);

            rb.linearVelocity = newDir * currentMoveSpeed * 1.5f;
            for (int i = 0; i < duneAtk2Frames.Length; i++)
            {
                if (sr != null) sr.sprite = duneAtk2Frames[i];
                if (i == 1 && playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 2.4f)
                {
                    int heavyDmg = Mathf.RoundToInt(dmg * 1.35f);
                    playerHealth?.TakeDamage(heavyDmg);
                    DamagePopup.Create(playerTransform.position + Vector3.up * 0.8f, heavyDmg, isPlayer: true);
                    SoundManager.EnsureInstance()?.PlayHitImpact();
                }
                yield return new WaitForSeconds(0.045f);
            }
            rb.linearVelocity = Vector2.zero;
        }

        isPlayingCustomAction = false;
    }

    private IEnumerator PerformMageAttackRoutine(Vector2 dir, int dmg)
    {
        isPlayingCustomAction = true;

        if (mageAttackFrames != null && mageAttackFrames.Length > 0)
        {
            for (int i = 0; i < mageAttackFrames.Length; i++)
            {
                if (sr != null) sr.sprite = mageAttackFrames[i];
                yield return new WaitForSeconds(0.055f);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }

        // Bắn 3 quả Cầu Tinh Tú Hư Không đuổi theo người chơi
        SoundManager.EnsureInstance()?.PlaySwordSlash();
        for (int i = -1; i <= 1; i++)
        {
            float angOffset = i * 22f;
            Vector2 shootDir = Quaternion.Euler(0, 0, angOffset) * dir;
            SpawnMageArcaneOrb(transform.position + (Vector3)shootDir * 0.6f, shootDir, dmg);
        }

        yield return new WaitForSeconds(0.2f);
        isPlayingCustomAction = false;
    }

    private void SpawnMageArcaneOrb(Vector3 pos, Vector2 shootDir, int dmg)
    {
        GameObject orb = new GameObject("Doppelganger_MageOrb");
        orb.transform.position = pos;
        orb.transform.localScale = Vector3.one * 0.45f;

        var oSr = orb.AddComponent<SpriteRenderer>();
        oSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        oSr.color = new Color(0.75f, 0.25f, 1f, 1f);
        oSr.sortingOrder = 5500;

        var col = orb.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;

        var rbOrb = orb.AddComponent<Rigidbody2D>();
        rbOrb.gravityScale = 0f;

        var proj = orb.AddComponent<EnemyProjectile>();
        proj.Initialize(shootDir, dmg, 8.5f);
    }

    private IEnumerator PerformStandardMeleeComboRoutine(Vector2 dir, int dmg)
    {
        if (animator != null)
        {
            animator.SetInteger(ComboStepHash, 1);
            animator.SetTrigger(AttackTriggerHash);
            animator.Play("Attack1", 0, 0f);
        }

        PlayHeroAttackAudio(1);
        SpawnHeroSlashFX(transform.position + (Vector3)dir * 0.9f, dir, isHeavy: false);

        rb.linearVelocity = dir * currentMoveSpeed * 1.3f;
        yield return new WaitForSeconds(0.10f);
        rb.linearVelocity = Vector2.zero;

        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 2.2f)
        {
            playerHealth?.TakeDamage(dmg);
            DamagePopup.Create(playerTransform.position + Vector3.up * 0.8f, dmg, isPlayer: true);
            SoundManager.EnsureInstance()?.PlayHitImpact();
        }

        if (currentPhase == 1)
        {
            SpawnGroundShockwave(transform.position + (Vector3)dir * 1.2f, dir);
        }

        yield return new WaitForSeconds(0.22f);

        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 2.5f)
        {
            Vector2 newDir = (playerTransform.position - transform.position).normalized;

            if (animator != null)
            {
                animator.SetInteger(ComboStepHash, 2);
                animator.SetTrigger(AttackTriggerHash);
                animator.Play("Attack2", 0, 0f);
            }

            PlayHeroAttackAudio(2);
            SpawnHeroSlashFX(transform.position + (Vector3)newDir * 1.1f, newDir, isHeavy: true);

            rb.linearVelocity = newDir * currentMoveSpeed * 1.7f;
            yield return new WaitForSeconds(0.14f);
            rb.linearVelocity = Vector2.zero;

            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 2.4f)
            {
                int heavyDmg = Mathf.RoundToInt(dmg * 1.45f);
                playerHealth?.TakeDamage(heavyDmg);
                DamagePopup.Create(playerTransform.position + Vector3.up * 0.8f, heavyDmg, isPlayer: true);
                SoundManager.EnsureInstance()?.PlayHitImpact();

                var cam = FindAnyObjectByType<CameraFollow>();
                if (cam != null) cam.Shake(0.20f, 0.30f);
            }

            if (currentPhase == 1)
            {
                SpawnGroundShockwave(transform.position + (Vector3)newDir * 1.4f, newDir);
            }

            yield return new WaitForSeconds(0.20f);
        }

        if (animator != null)
        {
            animator.SetInteger(ComboStepHash, 0);
            animator.Play("Idle", 0, 0f);
        }
    }

    private void PlayHeroAttackAudio(int step)
    {
        if (copiedHeroId == "titan")
        {
            SoundManager.EnsureInstance()?.PlayTitanSlam();
        }
        else
        {
            SoundManager.EnsureInstance()?.PlaySwordSlash(step);
        }
    }

    private void SpawnHeroSlashFX(Vector3 pos, Vector2 dir, bool isHeavy)
    {
        GameObject fx = new GameObject("HeroSlashFX");
        fx.transform.position = pos;
        var fSr = fx.AddComponent<SpriteRenderer>();
        fSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();

        Color slashColor = (copiedHeroId == "azure") ? new Color(0.3f, 0.85f, 1f, 0.9f) :
                           (copiedHeroId == "titan") ? new Color(1f, 0.5f, 0.1f, 0.95f) :
                           (copiedHeroId == "ronin") ? new Color(0.2f, 1f, 0.8f, 0.9f) :
                           (copiedHeroId == "mage") ? new Color(0.7f, 0.3f, 1f, 0.9f) :
                           (copiedHeroId == "dune") ? new Color(1f, 0.82f, 0.25f, 0.95f) :
                           new Color(0.85f, 0.25f, 1f, 0.9f);

        if (currentPhase == 1) slashColor = Color.Lerp(slashColor, Color.red, 0.5f);

        fSr.color = slashColor;
        fSr.sortingOrder = 5200;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        fx.transform.rotation = Quaternion.Euler(0, 0, angle);
        fx.transform.localScale = isHeavy ? new Vector3(2.4f, 0.55f, 1f) : new Vector3(1.7f, 0.38f, 1f);

        Destroy(fx, 0.16f);
    }

    private void SpawnGroundShockwave(Vector3 pos, Vector2 dir)
    {
        GameObject shock = new GameObject("GroundShockwave");
        shock.transform.position = pos;
        var sSr = shock.AddComponent<SpriteRenderer>();
        sSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        sSr.color = new Color(1f, 0.1f, 0.3f, 0.75f);
        sSr.sortingOrder = 5150;
        shock.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
        Destroy(shock, 0.25f);
    }

    /// <summary>
    /// Thi triển Tuyệt Kỹ Độc Quyền của từng Class Hero (Ruin Whirlwind, Phantom Gale, Meteor, Sandstorm, etc.)
    /// </summary>
    private IEnumerator ExecuteHeroSignatureSkillRoutine(Vector2 dir)
    {
        isAttacking = true;
        skillTimer = skillCooldown;
        rb.linearVelocity = Vector2.zero;

        int skillDmg = Mathf.RoundToInt(baseAttackDamage * (currentPhase == 1 ? 1.6f : (currentPhase == 2 ? 1.3f : 1.0f)));

        if (copiedHeroId == "dune")
        {
            // === DUNE ASSASSIN: CUỒNG BÃO SA ẢNH HƯ KHÔNG (SANDSTORM X-REND) ===
            isPlayingCustomAction = true;
            SoundManager.EnsureInstance()?.PlayDash();

            if (playerTransform != null)
            {
                // Tốc biến luồn sau lưng người chơi
                Vector3 behind = playerTransform.position + (Vector3)(-dir * 1.35f);
                transform.position = behind;
            }

            if (duneSpAtkFrames != null && duneSpAtkFrames.Length > 0)
            {
                for (int loop = 0; loop < 2; loop++)
                {
                    for (int i = 0; i < duneSpAtkFrames.Length; i++)
                    {
                        if (sr != null) sr.sprite = duneSpAtkFrames[i];
                        yield return new WaitForSeconds(0.04f);
                    }
                }
            }
            else
            {
                yield return new WaitForSeconds(0.35f);
            }

            // Quét bão cát diện rộng
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            SpawnHeroSlashFX(transform.position, dir, isHeavy: true);

            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 4.2f)
            {
                playerHealth?.TakeDamage(skillDmg);
                DamagePopup.Create(playerTransform.position + Vector3.up * 0.8f, skillDmg, isPlayer: true);
                SoundManager.EnsureInstance()?.PlayHitImpact();
            }

            isPlayingCustomAction = false;
        }
        else if (copiedHeroId == "titan")
        {
            // === TITAN: ĐỊA CHẤN TẬN DIỆT ===
            SoundManager.EnsureInstance()?.PlayTitanSlam();
            rb.linearVelocity = dir * currentMoveSpeed * 1.8f;
            yield return new WaitForSeconds(0.25f);
            rb.linearVelocity = Vector2.zero;

            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.45f, 0.7f);

            SpawnGroundShockwave(transform.position, dir);

            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 3.8f)
            {
                int titanDmg = Mathf.RoundToInt(skillDmg * 1.4f);
                playerHealth?.TakeDamage(titanDmg);
                DamagePopup.Create(playerTransform.position + Vector3.up * 0.8f, titanDmg, isPlayer: true);
                SoundManager.EnsureInstance()?.PlayHitImpact();
            }
        }
        else if (copiedHeroId == "ronin")
        {
            // === GALE RONIN: ẢO ẢNH PHONG TRẢM ===
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            Vector3 dashTarget = transform.position + (Vector3)dir * 5.0f;
            transform.position = dashTarget;

            SpawnHeroSlashFX(transform.position, dir, isHeavy: true);

            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 3.5f)
            {
                playerHealth?.TakeDamage(skillDmg);
                DamagePopup.Create(playerTransform.position + Vector3.up * 0.8f, skillDmg, isPlayer: true);
            }
        }
        else if (copiedHeroId == "azure")
        {
            // === AZURE BLADE: BĂNG ĐOẠN HƯ KHÔNG ===
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            SpawnHeroSlashFX(transform.position + (Vector3)dir * 1.2f, dir, isHeavy: true);

            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 3.2f)
            {
                playerHealth?.TakeDamage(skillDmg);
                DamagePopup.Create(playerTransform.position + Vector3.up * 0.8f, skillDmg, isPlayer: true);
                var pc = playerTransform.GetComponent<PlayerController>();
                if (pc != null) pc.ApplyFreeze(0.6f);
            }
        }
        else if (copiedHeroId == "mage")
        {
            // === CELESTIAL MAGE: THIÊN THẠCH MA TRẬN ===
            isPlayingCustomAction = true;
            SoundManager.EnsureInstance()?.PlaySwordSlash();

            for (int i = 0; i < 4; i++)
            {
                Vector3 fallPos = (playerTransform != null) ? playerTransform.position + (Vector3)Random.insideUnitCircle * 1.8f : transform.position;
                SpawnDoppelgangerMeteor(fallPos, skillDmg);
                yield return new WaitForSeconds(0.18f);
            }

            isPlayingCustomAction = false;
        }
        else
        {
            // === VAELSTRIDER: CƠN LỐC TÀN TÍCH HƯ KHÔNG ===
            SoundManager.EnsureInstance()?.PlaySwordSlash();

            float whirlTime = 0f;
            while (whirlTime < 1.4f)
            {
                whirlTime += 0.2f;
                transform.Rotate(0f, 0f, 180f);
                if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 3.8f)
                {
                    int tickDmg = Mathf.RoundToInt(skillDmg * 0.25f);
                    playerHealth?.TakeDamage(tickDmg);
                    DamagePopup.Create(playerTransform.position + Vector3.up * 0.8f, tickDmg, isPlayer: true);
                }
                yield return new WaitForSeconds(0.2f);
            }
            transform.rotation = Quaternion.identity;
        }

        yield return new WaitForSeconds(0.25f);
        isAttacking = false;
    }

    private void SpawnDoppelgangerMeteor(Vector3 targetPos, int dmg)
    {
        GameObject meteor = new GameObject("Doppelganger_Meteor");
        meteor.transform.position = targetPos + Vector3.up * 6.0f;
        var mSr = meteor.AddComponent<SpriteRenderer>();
        mSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        mSr.color = new Color(0.85f, 0.15f, 0.35f, 0.95f);
        mSr.sortingOrder = 5600;
        meteor.transform.localScale = Vector3.one * 0.85f;

        StartCoroutine(MeteorDropRoutine(meteor, targetPos, dmg));
    }

    private IEnumerator MeteorDropRoutine(GameObject meteor, Vector3 targetPos, int dmg)
    {
        float dropSpeed = 16f;
        while (meteor != null && meteor.transform.position.y > targetPos.y)
        {
            meteor.transform.position += Vector3.down * dropSpeed * Time.deltaTime;
            yield return null;
        }

        if (meteor != null)
        {
            Destroy(meteor);
            SoundManager.EnsureInstance()?.PlayHitImpact();

            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.35f, 0.5f);

            GameObject blast = new GameObject("MeteorBlast");
            blast.transform.position = targetPos;
            var bSr = blast.AddComponent<SpriteRenderer>();
            bSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
            bSr.color = new Color(1.0f, 0.25f, 0.1f, 0.85f);
            bSr.sortingOrder = 5550;
            blast.transform.localScale = Vector3.one * 3.2f;
            Destroy(blast, 0.25f);

            if (playerTransform != null && Vector2.Distance(targetPos, playerTransform.position) <= 2.8f)
            {
                playerHealth?.TakeDamage(dmg);
                DamagePopup.Create(playerTransform.position + Vector3.up * 0.8f, dmg, isPlayer: true);
            }
        }
    }

    private IEnumerator PerformBladeDashRoutine(Vector2 dir)
    {
        isAttacking = true;
        dashTimer = dashCooldown;
        SoundManager.EnsureInstance()?.PlayDash();

        if (copiedHeroId == "dune" && duneRollFrames != null && duneRollFrames.Length > 0)
        {
            isPlayingCustomAction = true;
            rb.linearVelocity = dir * currentMoveSpeed * 2.4f;
            for (int i = 0; i < duneRollFrames.Length; i++)
            {
                if (sr != null) sr.sprite = duneRollFrames[i];
                yield return new WaitForSeconds(0.04f);
            }
            rb.linearVelocity = Vector2.zero;
            isPlayingCustomAction = false;
        }
        else
        {
            rb.linearVelocity = dir * currentMoveSpeed * 2.2f;
            yield return new WaitForSeconds(0.20f);
            rb.linearVelocity = Vector2.zero;
        }

        isAttacking = false;
    }

    private IEnumerator PerformDivineParryStanceRoutine(Vector2 dir)
    {
        isParrying = true;
        wasParryTriggered = false;
        parryTimer = parryCooldown;
        rb.linearVelocity = Vector2.zero;

        SpawnParryShieldFX();

        float waitTime = 1.2f;
        float elapsed = 0f;
        while (elapsed < waitTime && !wasParryTriggered)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        DestroyParryShieldFX();

        if (wasParryTriggered)
        {
            yield return StartCoroutine(PerformRiposteAttackRoutine(dir));
        }

        isParrying = false;
    }

    private void SpawnParryShieldFX()
    {
        if (parryShieldVisual != null) Destroy(parryShieldVisual);
        parryShieldVisual = new GameObject("ParryShieldVisual");
        parryShieldVisual.transform.SetParent(transform, false);
        parryShieldVisual.transform.localPosition = new Vector3(0f, 0.2f, 0f);

        var pSr = parryShieldVisual.AddComponent<SpriteRenderer>();
        pSr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        pSr.color = new Color(1f, 0.85f, 0.2f, 0.65f);
        pSr.sortingOrder = 5100;
        parryShieldVisual.transform.localScale = Vector3.one * 1.8f;
    }

    private void DestroyParryShieldFX()
    {
        if (parryShieldVisual != null)
        {
            Destroy(parryShieldVisual);
            parryShieldVisual = null;
        }
    }

    public void TriggerBossParry()
    {
        if (!isParrying || wasParryTriggered) return;
        wasParryTriggered = true;
        SoundManager.EnsureInstance()?.PlayParrySuccess();
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.35f, 0.5f);
    }

    private IEnumerator PerformRiposteAttackRoutine(Vector2 dir)
    {
        SoundManager.EnsureInstance()?.PlaySwordSlash();
        rb.linearVelocity = dir * currentMoveSpeed * 2.6f;
        SpawnHeroSlashFX(transform.position + (Vector3)dir * 1.4f, dir, isHeavy: true);

        yield return new WaitForSeconds(0.18f);
        rb.linearVelocity = Vector2.zero;

        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 3.0f)
        {
            int riposteDmg = Mathf.RoundToInt(baseAttackDamage * 2.2f);
            playerHealth?.TakeDamage(riposteDmg);
            DamagePopup.Create(playerTransform.position + Vector3.up * 0.8f, riposteDmg, isPlayer: true);
        }
    }

    #endregion

    #region IDamageable & Health Management

    public void TakeDamage(int damage)
    {
        if (isDead || isInvulnerable) return;

        if (isParrying)
        {
            TriggerBossParry();
            return;
        }

        currentHealth -= damage;
        StartCoroutine(FlashWhiteRoutine());

        if (currentHealth <= 0)
        {
            if (currentPhase == 3)
            {
                // Hết thanh máu Phase 1 -> Chuyển sang Phase 2
                StartCoroutine(PhaseTransitionRoutine(2));
            }
            else if (currentPhase == 2)
            {
                // Hết thanh máu Phase 2 -> Chuyển sang Phase 3
                StartCoroutine(PhaseTransitionRoutine(1));
            }
            else
            {
                // Hết thanh máu Phase 3 -> Boss chính thức chết!
                Die();
            }
        }
        else
        {
            UpdateBossHealthUI();
        }
    }

    private IEnumerator PhaseTransitionRoutine(int nextPhase)
    {
        isInvulnerable = true;
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        var cam = FindAnyObjectByType<CameraFollow>();

        if (nextPhase == 2)
        {
            currentPhase = 2;
            SoundManager.EnsureInstance()?.PlayBossWarning();
            if (cam != null) cam.Shake(0.6f, 0.8f);

            // Bộc phá sóng đẩy lùi
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 4.5f)
            {
                Vector2 pushDir = (playerTransform.position - transform.position).normalized;
                playerTransform.position += (Vector3)pushDir * 1.4f;
            }

            yield return new WaitForSeconds(0.9f);

            ApplyPhase2Aesthetics();

            // Hồi đầy máu cho Phase 2
            currentHealth = maxHealth;
            UpdateBossHealthUI();
        }
        else if (nextPhase == 1)
        {
            currentPhase = 1;
            SoundManager.EnsureInstance()?.PlayBossDeath();
            yield return new WaitForSeconds(0.35f);
            SoundManager.EnsureInstance()?.PlayBossWarning();

            if (cam != null) cam.Shake(0.9f, 1.2f);

            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= 5.5f)
            {
                Vector2 pushDir = (playerTransform.position - transform.position).normalized;
                playerTransform.position += (Vector3)pushDir * 2.0f;
            }

            yield return new WaitForSeconds(1.1f);

            ApplyPhase3Aesthetics();

            // Hồi đầy máu cho Phase 3
            currentHealth = maxHealth;
            UpdateBossHealthUI();
        }

        yield return new WaitForSeconds(0.3f);
        isInvulnerable = false;
        isAttacking = false;
    }

    private IEnumerator FlashWhiteRoutine()
    {
        Color origColor = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (!isDead && sr != null)
        {
            sr.color = origColor;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        if (myCol != null) myCol.enabled = false;

        BossHealthBarUI.EnsureInstance()?.HideBoss();
        SoundManager.EnsureInstance()?.PlayBossDeath();

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.8f, 1.2f);

        // Rơi phần thưởng cực phẩm
        AethelShard.SpawnShards(transform.position, 60);
        HealthPotion.Spawn(transform.position + Vector3.left * 1.0f, 1);
        HealthPotion.Spawn(transform.position + Vector3.right * 1.0f, 1);
        WorldItemPickup.RollDrop(transform.position, true);
        WorldItemPickup.RollDrop(transform.position + Vector3.down * 0.8f, true);

        StartCoroutine(DeathVanishRoutine());
    }

    private IEnumerator DeathVanishRoutine()
    {
        float vanishDuration = 1.4f;
        float elapsed = 0f;
        while (elapsed < vanishDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1.0f - (elapsed / vanishDuration);
            if (sr != null) sr.color = new Color(1f, 0.85f, 0.2f, alpha);
            yield return null;
        }

        // KÍCH HOẠT CALLBACK: MỞ CỔNG THIÊN GIỚI VÀ XUẤT HIỆN NPC SHOP
        onDefeatedCallback?.Invoke();
        Destroy(gameObject);
    }

    #endregion
}
