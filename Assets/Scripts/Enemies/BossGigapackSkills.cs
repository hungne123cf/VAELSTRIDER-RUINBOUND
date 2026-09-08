using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BossGigapackSkills : MonoBehaviour
{
    private class SkillDef
    {
        public string nameVN;
        public string relativePath; // Relative to Assets/ART/BotWheel/Super Pixel Effects Gigapack (Free Version)/PNG/
        public int damage;
        public float radius;
        public float speed;
        public float animFps;
        public bool targetPlayerPos;
        public Sprite[] cachedFrames;
    }

    private List<SkillDef> activeSkills = new List<SkillDef>();
    private BotWheelAI bossAI;
    private Transform playerTransform;
    private float skillCooldownTimer = 4.0f;
    private bool isCasting = false;
    private bool isMap2 = false;

    private static readonly string GigapackBase = "Assets/ART/BotWheel/Super Pixel Effects Gigapack (Free Version)/PNG/";

    void Awake()
    {
        bossAI = GetComponent<BotWheelAI>();
        string scene = SceneManager.GetActiveScene().name;
        isMap2 = scene.Contains("Map2") || scene.Contains("Valley");
        InitializeSkillList();
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    private void InitializeSkillList()
    {
        activeSkills.Clear();

        if (!isMap2)
        {
            // ==========================================
            // BOSS MAP 1: 5 SKILL ĐẶC BIỆT ĐỘC BẢN
            // ==========================================
            activeSkills.Add(new SkillDef {
                nameVN = "BÃO NỔ CỔ ĐẠI",
                relativePath = "Explosions/epic_explosion_001/epic_explosion_001_large_orange",
                damage = 22, radius = 2.8f, speed = 0f, animFps = 16f, targetPlayerPos = true
            });
            activeSkills.Add(new SkillDef {
                nameVN = "TỨ PHƯƠNG HỦY DIỆT",
                relativePath = "Explosions/symmetrical_explosion_001/symmetrical_explosion_001_large_orange",
                damage = 18, radius = 2.4f, speed = 0f, animFps = 14f, targetPlayerPos = false
            });
            activeSkills.Add(new SkillDef {
                nameVN = "HỐ ĐEN HÚT HỒN",
                relativePath = "Fantasy Spells/spell_absorb_001/spell_absorb_001_large_violet",
                damage = 15, radius = 2.2f, speed = 0f, animFps = 20f, targetPlayerPos = true
            });
            activeSkills.Add(new SkillDef {
                nameVN = "XUNG KÍCH TRẢM",
                relativePath = "Impacts/directional_impact_001/directional_impact_001_large_blue",
                damage = 16, radius = 1.8f, speed = 6.5f, animFps = 16f, targetPlayerPos = false
            });
            activeSkills.Add(new SkillDef {
                nameVN = "TÁI TẠO NĂNG LƯỢNG",
                relativePath = "Fantasy Spells/spell_heal_001/spell_heal_001_large_red",
                damage = 12, radius = 2.0f, speed = 0f, animFps = 15f, targetPlayerPos = false
            });
        }
        else
        {
            // ==========================================
            // BOSS MAP 2: 20 SKILL ĐẶC BIỆT (KHÔNG TRÙNG)
            // ==========================================
            activeSkills.Add(new SkillDef { nameVN = "THẦN THỤ BỘC PHÁ", relativePath = "Explosions/epic_explosion_002/epic_explosion_002_large_yellow", damage = 30, radius = 3.2f, animFps = 16f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "HOÀNG KIM THỂ NỔ", relativePath = "Explosions/stylized_explosion_001/stylized_explosion_001_large_yellow", damage = 25, radius = 2.4f, animFps = 15f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "TỬ THẦN HƯ KHÔNG", relativePath = "Explosions/stylized_explosion_002/stylized_explosion_002_large_violet", damage = 26, radius = 2.5f, animFps = 15f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "HỎA VÂN TÀN PHÁ", relativePath = "Explosions/stylized_explosion_003/stylized_explosion_003_large_yellow", damage = 28, radius = 2.6f, animFps = 16f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "BÁT PHƯƠNG CHẤN NỘ", relativePath = "Explosions/symmetrical_explosion_002/symmetrical_explosion_002_large_orange", damage = 22, radius = 2.5f, animFps = 15f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "TỨ TRẬN HỦY DIỆT", relativePath = "Explosions/symmetrical_explosion_004/symmetrical_explosion_004_large_orange", damage = 24, radius = 2.6f, animFps = 15f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "ĐẦU LÂU HẮC ÁM", relativePath = "Fantasy Spells/spell_death_001/spell_death_001_large_red", damage = 35, radius = 3.0f, animFps = 25f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "ĐẦM LẦY ĐỘC DƯỢC", relativePath = "Fantasy Spells/spell_poison_001/spell_poison_001_large_green", damage = 18, radius = 2.5f, animFps = 16f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "CUỒNG PHONG ẢO ẢNH", relativePath = "Fantasy Spells/spell_haste_001/spell_haste_001_large_green", damage = 20, radius = 2.2f, animFps = 22f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "CHIẾN Ý CUỒNG BẠO", relativePath = "Fantasy Spells/spell_attack_up_001/spell_attack_up_001_large_red", damage = 22, radius = 2.0f, animFps = 18f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "KHIÊN CỔ THỤ", relativePath = "Fantasy Spells/spell_defense_up_001/spell_defense_up_001_large_blue", damage = 15, radius = 2.2f, animFps = 18f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "MƯA ĐỘC ĂN MÒN", relativePath = "Fantasy Spells/status_poison_001/status_poison_001_large_green", damage = 16, radius = 2.4f, animFps = 20f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "BỤI SAO TÊ LIỆT", relativePath = "Fantasy Spells/status_sparkling_001/status_sparkling_001_large_yellow", damage = 14, radius = 2.5f, animFps = 20f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "BĂNG THẠCH ĐAO KHÍ", relativePath = "Impacts/directional_impact_002/directional_impact_002_large_white", damage = 24, radius = 1.8f, speed = 6.0f, animFps = 16f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "HƯ KHÔNG ĐẠI TRẢM", relativePath = "Impacts/directional_impact_003/directional_impact_003_large_violet", damage = 26, radius = 2.0f, speed = 6.5f, animFps = 16f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "HOÀNG KIM XUNG KÍCH", relativePath = "Impacts/directional_impact_004/directional_impact_004_large_yellow", damage = 22, radius = 1.8f, speed = 7.0f, animFps = 16f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "ĐỊA CHẤN VẠN TRỌNG", relativePath = "Impacts/symmetrical_impact_001/symmetrical_impact_001_large_yellow", damage = 26, radius = 2.6f, animFps = 16f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "LỐC XOÁY LAM QUANG", relativePath = "Impacts/symmetrical_impact_002/symmetrical_impact_002_large_blue", damage = 22, radius = 2.4f, animFps = 16f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "HUYẾT TRẬN TẬN CÙNG", relativePath = "Splatters/burst_splatter_001/burst_splatter_001_large_red", damage = 28, radius = 2.8f, animFps = 16f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "ĐỘC DỊCH VUNG VÃI", relativePath = "Splatters/burst_splatter_003/burst_splatter_003_large_green", damage = 20, radius = 2.5f, animFps = 16f, targetPlayerPos = true });
        }
    }

    void Update()
    {
        if (bossAI == null || !bossAI.IsBoss) return;

        if (playerTransform == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            return;
        }

        if (skillCooldownTimer > 0f)
        {
            skillCooldownTimer -= Time.deltaTime;
        }
        else if (!isCasting && activeSkills.Count > 0)
        {
            // Cooldown giam dan theo phase cua boss (Phase 3 spam skill nhanh hon!)
            float baseCd = isMap2 ? 3.5f : 4.5f;
            int phase = bossAI.CurrentBossPhase;
            if (phase == 2) baseCd *= 0.8f;
            else if (phase == 1) baseCd *= 0.65f; // Phase cuoi cuong no!

            skillCooldownTimer = baseCd;
            StartCoroutine(CastRandomSkillRoutine());
        }
    }

    private IEnumerator CastRandomSkillRoutine()
    {
        isCasting = true;
        int idx = Random.Range(0, activeSkills.Count);
        SkillDef skill = activeSkills[idx];

        // 1. Bao ten skill len dau Boss
        Vector3 popupPos = transform.position + Vector3.up * 1.6f;
        Color nameCol = isMap2 ? new Color(1f, 0.45f, 0.25f) : new Color(0.95f, 0.85f, 0.35f);
        DamagePopup.CreateText(popupPos, $"✦ {skill.nameVN} ✦", nameCol);

        // Sound gio hú / bao dong
        SoundManager.EnsureInstance()?.PlaySwordSlash();

        // 2. Chuan bi vi tri thi trien
        Vector3 targetPos = skill.targetPlayerPos && playerTransform != null 
            ? playerTransform.position 
            : transform.position;

        Vector2 shootDir = Vector2.down;
        if (playerTransform != null)
        {
            shootDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        }

        // Cho nua giay tap trung nang luong
        yield return new WaitForSeconds(0.4f);

        // 3. Load frames neu chua cache
        if (skill.cachedFrames == null || skill.cachedFrames.Length == 0)
        {
            skill.cachedFrames = LoadFramesFromDisk(skill.relativePath);
        }

        if (skill.cachedFrames != null && skill.cachedFrames.Length > 0)
        {
            StartCoroutine(ExecuteSkillFX(skill, targetPos, shootDir));
        }

        isCasting = false;
    }

    private IEnumerator ExecuteSkillFX(SkillDef skill, Vector3 spawnPos, Vector2 direction)
    {
        GameObject fxObj = new GameObject("BossSkillFX_" + skill.nameVN);
        fxObj.transform.position = spawnPos;

        var sr = fxObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4500;
        sr.sprite = skill.cachedFrames[0];

        // Phim dan / bay ve phia truoc neu la directional
        bool isProjectile = skill.speed > 0f;
        if (isProjectile)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            fxObj.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            fxObj.transform.position = transform.position + (Vector3)(direction * 0.8f);
        }
        else
        {
            fxObj.transform.localScale = Vector3.one * 1.6f;
        }

        // Rung man hinh nhe
        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.2f, 0.35f);

        float frameDelay = 1f / skill.animFps;
        bool hasDealtDamage = false;

        for (int i = 0; i < skill.cachedFrames.Length; i++)
        {
            if (fxObj == null) yield break;
            sr.sprite = skill.cachedFrames[i];

            if (isProjectile)
            {
                fxObj.transform.position += (Vector3)(direction * skill.speed * frameDelay);
            }

            // Gay dame o giua hoat anh
            if (!hasDealtDamage && i >= skill.cachedFrames.Length / 3)
            {
                hasDealtDamage = true;
                CheckAndDealDamage(fxObj.transform.position, skill.radius, skill.damage);
            }

            yield return new WaitForSeconds(frameDelay);
        }

        if (fxObj != null) Destroy(fxObj);
    }

    private void CheckAndDealDamage(Vector3 center, float radius, int damage)
    {
        if (playerTransform == null) return;

        float dist = Vector2.Distance(center, playerTransform.position);
        if (dist <= radius)
        {
            var pHealth = playerTransform.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.TakeDamage(damage);
                DamagePopup.Create(playerTransform.position, damage, isCritical: true);
                SoundManager.EnsureInstance()?.PlayHitImpact();
            }
        }
    }

    private Sprite[] LoadFramesFromDisk(string relativePath)
    {
        string fullPath = Path.Combine(Application.dataPath, "ART/BotWheel/Super Pixel Effects Gigapack (Free Version)/PNG", relativePath);
        if (!Directory.Exists(fullPath))
        {
            Debug.LogWarning("[BOSS SKILL] Khong tim thay thu muc: " + fullPath);
            return null;
        }

        string[] files = Directory.GetFiles(fullPath, "*.png");
        System.Array.Sort(files);

        List<Sprite> sprites = new List<Sprite>();
        foreach (var file in files)
        {
#if UNITY_EDITOR
            string assetPath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
            Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sp != null)
            {
                sprites.Add(sp);
                continue;
            }
#endif
            byte[] bytes = File.ReadAllBytes(file);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            if (tex.LoadImage(bytes))
            {
                Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
                sprites.Add(sp);
            }
        }

        return sprites.ToArray();
    }
}
