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
        public string nameEN;
        public string relativePath; // Relative to Assets/ART/BotWheel/Super Pixel Effects Gigapack (Free Version)/PNG/
        public int damage;
        public float radius;
        public float speed;
        public float animFps;
        public bool targetPlayerPos;
        public Sprite[] cachedFrames;

        public string GetLocalizedName() => LocalizationManager.T(nameVN, string.IsNullOrEmpty(nameEN) ? nameVN : nameEN);
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
                nameVN = "BÃO NỔ CỔ ĐẠI", nameEN = "ANCIENT BLAST",
                relativePath = "Explosions/epic_explosion_001/epic_explosion_001_large_orange",
                damage = 25, radius = 2.8f, speed = 0f, animFps = 16f, targetPlayerPos = true
            });
            activeSkills.Add(new SkillDef {
                nameVN = "TỨ PHƯƠNG HỦY DIỆT", nameEN = "FOUR-WAY DESTRUCTION",
                relativePath = "Explosions/symmetrical_explosion_001/symmetrical_explosion_001_large_orange",
                damage = 22, radius = 2.6f, speed = 0f, animFps = 14f, targetPlayerPos = false
            });
            activeSkills.Add(new SkillDef {
                nameVN = "HỐ ĐEN THÔN PHỆ", nameEN = "VOID BLACK HOLE",
                relativePath = "Fantasy Spells/spell_absorb_001/spell_absorb_001_large_violet",
                damage = 20, radius = 2.5f, speed = 0f, animFps = 20f, targetPlayerPos = true
            });
            activeSkills.Add(new SkillDef {
                nameVN = "HƯ KHÔNG ĐẠI TRẢM", nameEN = "VOID CLEAVE",
                relativePath = "Impacts/directional_impact_003/directional_impact_003_large_violet",
                damage = 22, radius = 2.2f, speed = 7.0f, animFps = 16f, targetPlayerPos = false
            });
            activeSkills.Add(new SkillDef {
                nameVN = "LINH HỒN ĐOẠT MỆNH", nameEN = "SOUL REAPER",
                relativePath = "Smoke Bursts/stylized_skull_smoke_burst_001/stylized_skull_smoke_burst_001_large_white",
                damage = 26, radius = 3.0f, speed = 0f, animFps = 16f, targetPlayerPos = true
            });
        }
        else
        {
            // ==========================================
            // BOSS MAP 2: 20 SKILL ĐẶC BIỆT HẮC ÁM & HỦY DIỆT
            // ==========================================
            activeSkills.Add(new SkillDef { nameVN = "THẦN THỤ BỘC PHÁ", nameEN = "SACRED DETONATION", relativePath = "Explosions/epic_explosion_002/epic_explosion_002_large_yellow", damage = 34, radius = 3.2f, animFps = 16f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "HOÀNG KIM THỂ NỔ", nameEN = "GOLDEN BURST", relativePath = "Explosions/stylized_explosion_001/stylized_explosion_001_large_yellow", damage = 26, radius = 2.6f, animFps = 15f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "TỬ THẦN HƯ KHÔNG", nameEN = "VOID REAPER", relativePath = "Explosions/stylized_explosion_002/stylized_explosion_002_large_violet", damage = 28, radius = 2.8f, animFps = 15f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "HỎA VÂN TÀN PHÁ", nameEN = "FIRE CLOUD HAVOC", relativePath = "Explosions/stylized_explosion_003/stylized_explosion_003_large_yellow", damage = 30, radius = 2.8f, animFps = 16f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "BÁT PHƯƠNG CHẤN NỘ", nameEN = "EIGHT-WAY WRATH", relativePath = "Explosions/symmetrical_explosion_002/symmetrical_explosion_002_large_orange", damage = 24, radius = 2.6f, animFps = 15f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "TỨ TRẬN HỦY DIỆT", nameEN = "QUADRUPLE HAVOC", relativePath = "Explosions/symmetrical_explosion_004/symmetrical_explosion_004_large_orange", damage = 26, radius = 2.8f, animFps = 15f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "TỬ THẦN HUYẾT LÂU", nameEN = "BLOOD SKULL REAPER", relativePath = "Fantasy Spells/spell_death_001/spell_death_001_large_red", damage = 38, radius = 3.2f, animFps = 25f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "ĐẦM LẦY ĂN MÒN", nameEN = "CORROSIVE MIRE", relativePath = "Fantasy Spells/spell_poison_001/spell_poison_001_large_green", damage = 22, radius = 2.8f, animFps = 16f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "HỐ ĐEN THÔN PHỆ", nameEN = "VOID BLACK HOLE", relativePath = "Fantasy Spells/spell_absorb_001/spell_absorb_001_large_violet", damage = 25, radius = 2.8f, animFps = 20f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "HUYẾT MA BỘC PHÁ", nameEN = "BLOOD BURST", relativePath = "Splatters/burst_splatter_001/burst_splatter_001_large_red", damage = 32, radius = 3.0f, animFps = 18f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "LÔI ĐIỆN TỬ VONG", nameEN = "DEATH LIGHTNING", relativePath = "Lightning/lightning_strike_001/lightning_strike_001_large_violet", damage = 35, radius = 2.8f, animFps = 18f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "LÔI CẦU TẬN THẾ", nameEN = "DOOM LIGHTNING ORB", relativePath = "Lightning/lightning_burst_001/lightning_burst_001_large_violet", damage = 30, radius = 2.8f, animFps = 18f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "LÔI BẠO HẮC ÁM", nameEN = "DARK THUNDERSTORM", relativePath = "Lightning/lightning_burst_002/lightning_burst_002_large_violet", damage = 32, radius = 2.9f, animFps = 18f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "LINH HỒN ĐOẠT MỆNH", nameEN = "SOUL REAPER", relativePath = "Smoke Bursts/stylized_skull_smoke_burst_001/stylized_skull_smoke_burst_001_large_white", damage = 34, radius = 3.2f, animFps = 16f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "MƯA ĐỘC ĂN MÒN", nameEN = "CORROSIVE POISON RAIN", relativePath = "Fantasy Spells/status_poison_001/status_poison_001_large_green", damage = 20, radius = 2.6f, animFps = 20f, targetPlayerPos = true });
            activeSkills.Add(new SkillDef { nameVN = "BĂNG THẠCH ĐAO KHÍ", nameEN = "FROST SLASH", relativePath = "Impacts/directional_impact_002/directional_impact_002_large_white", damage = 26, radius = 2.0f, speed = 6.5f, animFps = 16f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "HƯ KHÔNG ĐẠI TRẢM", nameEN = "VOID CLEAVE", relativePath = "Impacts/directional_impact_003/directional_impact_003_large_violet", damage = 28, radius = 2.2f, speed = 7.0f, animFps = 16f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "HUYẾT ĐAO PHI TIỄN", nameEN = "CRIMSON BLADE DART", relativePath = "Splatters/directional_splatter_001/directional_splatter_001_large_red", damage = 28, radius = 2.0f, speed = 7.5f, animFps = 18f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "ĐỊA CHẤN VẠN TRỌNG", nameEN = "SEISMIC QUAKE", relativePath = "Impacts/symmetrical_impact_001/symmetrical_impact_001_large_yellow", damage = 28, radius = 2.8f, animFps = 16f, targetPlayerPos = false });
            activeSkills.Add(new SkillDef { nameVN = "ĐỘC DỊCH VUNG VÃI", nameEN = "VENOM SPLATTER", relativePath = "Splatters/burst_splatter_003/burst_splatter_003_large_green", damage = 24, radius = 2.8f, animFps = 16f, targetPlayerPos = true });
        }
    }

    void Update()
    {
        if (bossAI == null || !bossAI.IsBoss || bossAI.IsDead) return;
        if (bossAI.IsPurpleRingActive) return; // Đang tụ đòn ma trận đạn tím thì không tung chiêu khác!

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
        if (bossAI != null && (bossAI.IsDead || bossAI.IsPurpleRingActive)) yield break;
        isCasting = true;
        int idx = Random.Range(0, activeSkills.Count);
        SkillDef skill = activeSkills[idx];

        Vector3 bossCenter = bossAI != null ? bossAI.GetBossVisualCenter() : transform.position;

        // 1. Bao ten skill len dau Boss
        Vector3 popupPos = bossCenter + Vector3.up * 1.6f;
        Color nameCol = isMap2 ? new Color(1f, 0.45f, 0.25f) : new Color(0.95f, 0.85f, 0.35f);
        DamagePopup.CreateText(popupPos, $"{skill.GetLocalizedName()}", nameCol);

        // 2. Chuan bi vi tri thi trien (khoa vi tri nguoi choi tai thoi diem bao dong)
        Vector3 targetPos = skill.targetPlayerPos && playerTransform != null 
            ? playerTransform.position 
            : bossCenter;

        Vector2 shootDir = Vector2.down;
        if (playerTransform != null)
        {
            shootDir = ((Vector2)playerTransform.position - (Vector2)bossCenter).normalized;
        }

        // ========================================================
        // VÙNG CẢNH BÁO MÀU ĐỎ VÀ ÂM THANH BÁO ĐỘNG TRƯỚC 2 GIÂY
        // ========================================================
        GameObject warningArea = CreateWarningZone(targetPos, skill.radius, 2.0f);
        SoundManager.EnsureInstance()?.PlayBossWarning();

        // Dem nguoc 2 giay de nguoi choi nhin thay vung do va kip luot/ne tranh
        float elapsed = 0f;
        while (elapsed < 2.0f)
        {
            if (bossAI != null && bossAI.IsDead)
            {
                if (warningArea != null) Destroy(warningArea);
                isCasting = false;
                yield break;
            }

            elapsed += Time.deltaTime;
            // Phat them tieng am thanh canh bao don dap o giay thu 1
            if (elapsed >= 1.0f && elapsed - Time.deltaTime < 1.0f)
            {
                SoundManager.EnsureInstance()?.PlayBossWarning();
            }
            yield return null;
        }

        if (warningArea != null) Destroy(warningArea);

        if (bossAI != null && bossAI.IsDead)
        {
            isCasting = false;
            yield break;
        }

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

    private static Sprite warningCircleSprite = null;

    private static Sprite GetWarningCircleSprite()
    {
        if (warningCircleSprite != null) return warningCircleSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = (size - 1) / 2.0f;
        float maxR = center - 1.5f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float norm = d / maxR;
                if (norm <= 0.88f)
                {
                    // Vung do ben trong mo ao canh bao nguy hiem
                    tex.SetPixel(x, y, new Color(1f, 0.06f, 0.06f, 0.32f));
                }
                else if (norm <= 0.98f)
                {
                    // Vanh do sac net canh bao ro rang ranh gioi
                    tex.SetPixel(x, y, new Color(1f, 0.15f, 0.15f, 0.92f));
                }
                else if (norm <= 1.0f)
                {
                    float alpha = (1f - norm) / 0.02f * 0.92f;
                    tex.SetPixel(x, y, new Color(1f, 0.15f, 0.15f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        warningCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        return warningCircleSprite;
    }

    private GameObject CreateWarningZone(Vector3 pos, float radius, float duration)
    {
        GameObject zone = new GameObject("Boss_RedWarningZone");
        zone.transform.position = pos;

        // Duong kinh = radius * 2 unit -> Scale = (radius * 2f) / 1.28f
        float scale = (radius * 2f) / 1.28f;
        zone.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = zone.AddComponent<SpriteRenderer>();
        sr.sprite = GetWarningCircleSprite();
        sr.sortingOrder = 1800; // Nam tren mat dat, duoi chan nhan vat

        var telegraph = zone.AddComponent<BossWarningTelegraph>();
        telegraph.Initialize(duration);

        return zone;
    }

    private IEnumerator ExecuteSkillFX(SkillDef skill, Vector3 spawnPos, Vector2 direction)
    {
        GameObject fxObj = new GameObject("BossSkillFX_" + skill.nameVN);
        fxObj.transform.position = spawnPos;

        var sr = fxObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4500;
        sr.sprite = skill.cachedFrames[0];

        // Phóng to chiêu thức vừa vặn toàn bộ vòng tròn đỏ cảnh báo!
        bool isProjectile = skill.speed > 0f;
        if (isProjectile)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            fxObj.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            fxObj.transform.position = transform.position + (Vector3)(direction * 0.8f);
            fxObj.transform.localScale = Vector3.one * (skill.radius * 2.2f);
        }
        else
        {
            // Vòng tròn đỏ có đường kính = skill.radius * 2.
            // Phóng to scale = skill.radius * 2.2f để vụ nổ, cột khói, hố đen bung to vừa khít toàn bộ vòng đỏ!
            float fxScale = skill.radius * 2.2f;
            fxObj.transform.localScale = Vector3.one * fxScale;
        }

        // Rung man hinh nhe
        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.2f, 0.35f);

        float frameDelay = 1f / skill.animFps;
        bool hasDealtDamage = false;

        for (int i = 0; i < skill.cachedFrames.Length; i++)
        {
            if (fxObj == null) yield break;
            if (bossAI != null && bossAI.IsDead)
            {
                if (fxObj != null) Destroy(fxObj);
                yield break;
            }
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
            Sprite editorSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (editorSp != null)
            {
                sprites.Add(editorSp);
                continue;
            }
#endif
            byte[] bytes = File.ReadAllBytes(file);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            if (tex.LoadImage(bytes))
            {
                Sprite runtimeSp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
                sprites.Add(runtimeSp);
            }
        }

        return sprites.ToArray();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        isCasting = false;
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        ClearAllWarningZonesAndEffects();
    }

    public static void ClearAllWarningZonesAndEffects()
    {
        var warnings = Object.FindObjectsByType<BossWarningTelegraph>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var w in warnings)
        {
            if (w != null && w.gameObject != null) Destroy(w.gameObject);
        }

        var allObjs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in allObjs)
        {
            if (obj != null && (obj.name.StartsWith("Boss_RedWarningZone") || obj.name.StartsWith("BossSkillFX_") || obj.name.StartsWith("Boss_EnergyBullet_Orbit")))
            {
                Destroy(obj);
            }
        }
    }
}


public class BossWarningTelegraph : MonoBehaviour
{
    private SpriteRenderer sr;
    private float totalDuration = 2.0f;
    private float elapsed = 0f;
    private Color baseColor = new Color(1f, 0.15f, 0.15f, 0.85f);

    void Awake()
    {
        // Tự động tiêu hủy sau tối đa 2.5 giây, đảm bảo không bao giờ tồn tại vĩnh viễn
        Destroy(gameObject, 2.5f);
    }

    public void Initialize(float duration)
    {
        totalDuration = duration;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (sr == null) return;
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / totalDuration);

        // Nhip nhay do bao dong tang toc dan khi sap cham nguong 2 giay
        float flashSpeed = Mathf.Lerp(8f, 22f, progress);
        float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * flashSpeed);

        Color c = baseColor;
        c.a = Mathf.Lerp(0.5f, 0.95f, pulse);
        sr.color = c;
    }
}
