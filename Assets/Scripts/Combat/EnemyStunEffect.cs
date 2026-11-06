using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Enemy & Boss Stun Effect System
/// Hiệu ứng Choáng (Stun) áp dụng cho tất cả quái vật và Boss trong game:
/// 1. QUY TẮC KHÔNG CỘNG DỒN CHOÁNG (NO STUN STACKING):
///    - Khi quái/Boss đang bị choáng, mọi đòn stun tiếp theo từ player đều bị vô hiệu hóa (không cộng dồn thời gian).
/// 2. THỜI GIAN HỒI MIỄN NHIỄM CHOÁNG (ANTI-PERMA-STUNLOCK GRACE PERIOD):
///    - Sau khi hết choáng, Boss được miễn nhiễm choáng 4.0s, Quái thường được miễn nhiễm 2.5s.
///    - Đòn stun đánh vào lúc này sẽ hiện "KHÁNG CHOÁNG!" và bị triệt tiêu hoàn toàn.
/// 3. KHẢ NĂNG MIỄN CHOÁNG LÊN ĐẾN 50% (50% STUN IMMUNITY):
///    - 50% tỷ lệ né/miễn hoàn toàn đòn choáng (hiện "MIỄN CHOÁNG (50%)!").
///    - 50% giảm thời lượng choáng nếu bị dính (Boss tối đa 0.35s ngắt chiêu, Quái thường tối đa 0.75s).
/// 4. MIỄN CHOÁNG KHI CHUYỂN PHASE / BẤT TỬ (BOSS SUPER ARMOR):
///    - Tuyệt đối không choáng khi Boss đang đổi phase hoặc bất tử.
/// </summary>
public class EnemyStunEffect : MonoBehaviour
{
    // Cooldown thời gian miễn nhiễm choáng sau khi hồi phục để chống perma-stun
    public const float BOSS_IMMUNITY_COOLDOWN = 4.0f; // Boss: 4.0s miễn nhiễm choáng sau khi hết choáng
    public const float MONSTER_IMMUNITY_COOLDOWN = 2.5f; // Quái thường: 2.5s miễn nhiễm choáng sau khi hết choáng

    // Khả năng miễn choáng lên đến 50%
    public const float STUN_IMMUNITY_CHANCE = 0.50f; // 50% tỷ lệ né / miễn choáng hoàn toàn
    public const float STUN_DURATION_REDUCTION = 0.50f; // Giảm 50% thời gian choáng nếu dính

    public const float BOSS_MAX_STUN_DURATION = 0.35f; // Trần thời gian choáng tối đa của Boss (ngắt chiêu)
    public const float MONSTER_MAX_STUN_DURATION = 0.75f; // Trần thời gian choáng tối đa của Quái thường

    private static readonly Dictionary<int, float> s_LastStunEndTimes = new Dictionary<int, float>();

    private float stunTimer = 0f;
    private float lastStunEndTime = -999f;
    private Rigidbody2D rb;
    private List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
    private GameObject dizzyVfx;
    private Transform[] starTransforms;
    private float orbitAngle = 0f;
    private static Sprite cachedStarSprite = null;

    private Animator animator;
    private float originalAnimSpeed = 1f;
    private bool hasSavedAnimSpeed = false;

    public bool IsStunned => stunTimer > 0f;

    public static bool ApplyStun(GameObject target, float duration = 1.0f)
    {
        if (target == null) return false;

        // 1. Tuyệt đối không choáng Player
        if (target.CompareTag("Player") || target.GetComponent<PlayerHealth>() != null || target.GetComponentInParent<PlayerHealth>() != null) return false;

        // 2. Không choáng các vật thể môi trường (Props, Chests, Barrels, Eggs, Rocks, Obstacles)
        if (target.GetComponent<BreakableProp>() != null || target.GetComponentInParent<BreakableProp>() != null) return false;
        if (target.GetComponent<TreasureChest>() != null || target.GetComponentInParent<TreasureChest>() != null) return false;
        if (target.GetComponent<BoneEggObstacle>() != null || target.GetComponentInParent<BoneEggObstacle>() != null) return false;

        // 3. Tìm đúng GameObject gốc của Quái / Boss (phòng trường hợp va chạm trúng child collider / hitbox)
        GameObject enemyRoot = target;
        var damageable = target.GetComponent<IDamageable>() ?? target.GetComponentInParent<IDamageable>();
        if (damageable != null && (damageable as Component) != null)
        {
            enemyRoot = (damageable as Component).gameObject;
        }
        else if (target.transform.parent != null && target.GetComponent<Rigidbody2D>() == null && target.transform.parent.GetComponent<Rigidbody2D>() != null)
        {
            enemyRoot = target.transform.parent.gameObject;
        }

        // Kiểm tra xác thực là Enemy hoặc Boss
        bool isEnemyOrBoss = enemyRoot.CompareTag("Enemy") || enemyRoot.CompareTag("Boss") || (damageable != null && !(damageable is PlayerHealth));
        if (!isEnemyOrBoss)
        {
            var behaviours = enemyRoot.GetComponentsInChildren<MonoBehaviour>();
            foreach (var mb in behaviours)
            {
                if (mb == null) continue;
                string tName = mb.GetType().Name;
                if (tName.EndsWith("AI") || tName.EndsWith("Enemy") || tName.Contains("Boss") || tName.Contains("Monster") || tName.Contains("EnemyHealth"))
                {
                    isEnemyOrBoss = true;
                    break;
                }
            }
        }

        if (!isEnemyOrBoss) return false;

        bool isBoss = IsBoss(enemyRoot);

        // 4. KIỂM TRA ĐANG TRONG GIAI ĐOẠN BẤT TỬ HOẶC CHUYỂN PHASE (BOSS SUPER ARMOR)
        if (IsInvulnerableOrTransitioning(enemyRoot))
        {
            Vector3 headPos = GetHeadTopWorldPosition(enemyRoot);
            DamagePopup.CreateText(headPos, LocalizationManager.T("BẤT KHẢ XÂM PHẠM!", "INVULNERABLE!"), new Color(1f, 0.85f, 0.2f));
            return false;
        }

        // Cơ chế Cleanse 3s & Miễn nhiễm khống chế
        if (EnemyCCCleanseSystem.IsCCImmune(enemyRoot))
        {
            Vector3 headPos = GetHeadTopWorldPosition(enemyRoot);
            DamagePopup.CreateText(headPos, LocalizationManager.T("MIỄN KHỐNG CHẾ!", "CC IMMUNE!"), new Color(1f, 0.9f, 0.35f));
            return false;
        }
        EnemyCCCleanseSystem.EnsureAttached(enemyRoot);

        // 5. QUY TẮC KHÔNG CỘNG DỒN CHOÁNG (NO STUN STACKING):
        // Nếu Quái hoặc Boss đang trong trạng thái Choáng, tuyệt đối không cộng dồn thời gian!
        var existing = enemyRoot.GetComponent<EnemyStunEffect>();
        if (existing != null && existing.IsStunned)
        {
            // Bỏ qua hoàn toàn đòn gây choáng tiếp theo của Player
            return false;
        }

        // 6. KIỂM TRA THỜI GIAN HỒI MIỄN NHIỄM CHOÁNG (STUN IMMUNITY GRACE PERIOD)
        int targetId = enemyRoot.GetInstanceID();
        float lastEndTime = -999f;
        if (existing != null) lastEndTime = existing.lastStunEndTime;
        if (s_LastStunEndTimes.TryGetValue(targetId, out float recordedTime))
        {
            lastEndTime = Mathf.Max(lastEndTime, recordedTime);
        }

        float immunityCooldown = isBoss ? BOSS_IMMUNITY_COOLDOWN : MONSTER_IMMUNITY_COOLDOWN;
        if (Time.time < lastEndTime + immunityCooldown)
        {
            // Đang trong thời gian miễn nhiễm phục hồi -> Kháng choáng 100%!
            Vector3 headPos = GetHeadTopWorldPosition(enemyRoot);
            DamagePopup.CreateText(headPos, LocalizationManager.T("KHÁNG CHOÁNG!", "STUN RESIST!"), new Color(0.35f, 0.85f, 1f));
            return false;
        }

        // 7. KHẢ NĂNG MIỄN CHOÁNG LÊN ĐẾN 50% (50% STUN IMMUNITY CHANCE)
        // Cả Boss và Quái có 50% cơ hội miễn nhiễm hoàn toàn đòn choáng
        if (Random.value < STUN_IMMUNITY_CHANCE)
        {
            Vector3 headPos = GetHeadTopWorldPosition(enemyRoot);
            DamagePopup.CreateText(headPos, LocalizationManager.T("MIỄN CHOÁNG (50%)!", "STUN IMMUNE (50%)!"), new Color(1f, 0.75f, 0.2f));
            return false;
        }

        // 8. GIẢM 50% THỜI LƯỢNG CHOÁNG (50% DURATION REDUCTION)
        float effectiveDuration = duration * STUN_DURATION_REDUCTION;
        if (isBoss)
        {
            effectiveDuration = Mathf.Min(effectiveDuration, BOSS_MAX_STUN_DURATION);
        }
        else
        {
            effectiveDuration = Mathf.Min(effectiveDuration, MONSTER_MAX_STUN_DURATION);
        }

        // 9. ÁP DỤNG CHOÁNG
        if (existing == null)
        {
            existing = enemyRoot.AddComponent<EnemyStunEffect>();
        }
        existing.enabled = true;
        existing.SetStunDuration(effectiveDuration, isBoss);

        CleanExpiredImmunities();
        return true;
    }

    public static bool IsBoss(GameObject enemy)
    {
        if (enemy == null) return false;
        if (enemy.CompareTag("Boss")) return true;

        var eh = enemy.GetComponent<IEnemyHealth>() ?? enemy.GetComponentInParent<IEnemyHealth>();
        if (eh != null && eh.IsBoss) return true;

        if (enemy.GetComponent<ValleyGolemBossAI>() != null || enemy.GetComponentInParent<ValleyGolemBossAI>() != null) return true;
        if (enemy.GetComponent<SkeletonKingBossAI>() != null || enemy.GetComponentInParent<SkeletonKingBossAI>() != null) return true;
        if (enemy.GetComponent<BotWheelAI>() != null || enemy.GetComponentInParent<BotWheelAI>() != null) return true;
        if (enemy.GetComponent<BloodMonsterAI>() != null || enemy.GetComponentInParent<BloodMonsterAI>() != null) return true;
        if (enemy.GetComponent<ShadowDoppelgangerBoss>() != null || enemy.GetComponentInParent<ShadowDoppelgangerBoss>() != null) return true;
        if (enemy.GetComponent<BossGigapackSkills>() != null || enemy.GetComponentInParent<BossGigapackSkills>() != null) return true;

        string nameLower = enemy.name.ToLower();
        if (nameLower.Contains("boss") || nameLower.Contains("titan") || nameLower.Contains("gormok") || 
            nameLower.Contains("valthor") || nameLower.Contains("valenthus")) return true;

        return false;
    }

    public static bool IsInvulnerableOrTransitioning(GameObject enemyRoot)
    {
        if (enemyRoot == null) return false;

        // BotWheel
        var bot = enemyRoot.GetComponent<BotWheelAI>() ?? enemyRoot.GetComponentInParent<BotWheelAI>();
        if (bot != null && (bot.IsTransitioningPhase || bot.IsDead)) return true;

        // Skeleton King
        var sk = enemyRoot.GetComponent<SkeletonKingBossAI>() ?? enemyRoot.GetComponentInParent<SkeletonKingBossAI>();
        if (sk != null && (sk.IsPhaseTransitioning || sk.IsDead)) return true;

        // Blood Monster
        var bm = enemyRoot.GetComponent<BloodMonsterAI>() ?? enemyRoot.GetComponentInParent<BloodMonsterAI>();
        if (bm != null && (bm.IsTransitioningPhase || bm.IsDead)) return true;

        // Shadow Doppelganger
        var sd = enemyRoot.GetComponent<ShadowDoppelgangerBoss>() ?? enemyRoot.GetComponentInParent<ShadowDoppelgangerBoss>();
        if (sd != null && (sd.IsInvulnerable || sd.IsDead)) return true;

        // Valley Golem
        var vg = enemyRoot.GetComponent<ValleyGolemBossAI>() ?? enemyRoot.GetComponentInParent<ValleyGolemBossAI>();
        if (vg != null && vg.IsDead) return true;

        // General IEnemyHealth dead check
        var eh = enemyRoot.GetComponent<IEnemyHealth>() ?? enemyRoot.GetComponentInParent<IEnemyHealth>();
        if (eh != null && eh.CurrentHealth <= 0) return true;

        return false;
    }

    public void SetStunDuration(float duration, bool isBossTarget)
    {
        // Gán thời gian choáng chính xác (KHÔNG cộng dồn)
        stunTimer = duration;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        SoundManager.EnsureInstance()?.PlayHitImpact();

        EnsureDizzyVFX();
        if (dizzyVfx != null) dizzyVfx.SetActive(true);

        DisableAIScripts();
        PauseAnimator();
    }

    // Tương thích ngược với các hàm gọi cũ
    public void SetStunDuration(float duration)
    {
        SetStunDuration(duration, IsBoss(gameObject));
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            UpdateStarOrbit();

            if (stunTimer <= 0f)
            {
                RemoveStun();
            }
        }
    }

    private void UpdateStarOrbit()
    {
        if (dizzyVfx == null || starTransforms == null) return;

        // Tính vị trí tâm thực tế của đỉnh đầu quái/Boss trong World Space từng frame
        Vector3 headWorldPos = GetHeadTopWorldPosition(gameObject);
        dizzyVfx.transform.position = headWorldPos;
        dizzyVfx.transform.rotation = Quaternion.identity;
        dizzyVfx.transform.localScale = Vector3.one;

        // Xoay vòng sao quanh đỉnh đầu theo hình elip vừa vặn
        orbitAngle += 360f * Time.deltaTime * 1.5f;

        float enemyWidth = GetEnemyWidth();
        float radiusX = Mathf.Clamp(enemyWidth * 0.32f, 0.30f, 0.85f);
        float radiusY = radiusX * 0.35f;

        float scaleY = Mathf.Abs(transform.lossyScale.y);
        float starBaseScale = Mathf.Clamp(0.42f * Mathf.Sqrt(scaleY), 0.38f, 0.65f);

        for (int i = 0; i < starTransforms.Length; i++)
        {
            if (starTransforms[i] == null) continue;
            float angleDeg = orbitAngle + (i * 120f);
            float rad = angleDeg * Mathf.Deg2Rad;

            float x = Mathf.Cos(rad) * radiusX;
            float y = Mathf.Sin(rad) * radiusY + (Mathf.Sin(Time.time * 5f + i) * 0.025f);

            starTransforms[i].localPosition = new Vector3(x, y, 0f);
            starTransforms[i].localRotation = Quaternion.Euler(0f, 0f, -orbitAngle * 2.5f);

            float depthScale = Mathf.Lerp(starBaseScale * 0.82f, starBaseScale * 1.18f, (Mathf.Sin(rad) + 1f) * 0.5f);
            starTransforms[i].localScale = Vector3.one * depthScale;
        }
    }

    public static Vector3 GetHeadTopWorldPosition(GameObject target)
    {
        if (target == null) return Vector3.zero;

        float headCenterX = target.transform.position.x;
        bool hasCenterX = false;

        Collider2D mainCol = target.GetComponent<Collider2D>() ?? target.GetComponentInChildren<Collider2D>();
        if (mainCol != null && !mainCol.isTrigger)
        {
            headCenterX = mainCol.bounds.center.x;
            hasCenterX = true;
        }

        float maxTopY = target.transform.position.y + 0.9f;
        if (mainCol != null && !mainCol.isTrigger)
        {
            maxTopY = Mathf.Max(maxTopY, mainCol.bounds.max.y);
        }

        var allSrs = target.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in allSrs)
        {
            if (sr == null || sr.sprite == null) continue;
            string nameLower = sr.gameObject.name.ToLower();
            if (nameLower.Contains("popup") || nameLower.Contains("shadow") || nameLower.Contains("text") || 
                nameLower.Contains("shield") || nameLower.Contains("circle") || nameLower.Contains("cone") || 
                nameLower.Contains("indicator") || nameLower.Contains("range") || nameLower.Contains("dizzy") || nameLower.Contains("star")) continue;

            if (!hasCenterX)
            {
                headCenterX = sr.bounds.center.x;
                hasCenterX = true;
            }

            if (mainCol != null && !mainCol.isTrigger && sr.bounds.max.y > mainCol.bounds.max.y + 0.6f)
            {
                continue;
            }

            maxTopY = Mathf.Max(maxTopY, sr.bounds.max.y);
        }

        float scaleY = Mathf.Abs(target.transform.lossyScale.y);
        if (scaleY > 1.15f)
        {
            maxTopY = Mathf.Max(maxTopY, target.transform.position.y + (1.25f * scaleY));
        }

        return new Vector3(headCenterX, maxTopY + 0.22f, target.transform.position.z - 0.25f);
    }

    private float GetEnemyWidth()
    {
        float width = 1.0f;
        Collider2D mainCol = GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>();
        if (mainCol != null && !mainCol.isTrigger)
        {
            width = Mathf.Max(width, mainCol.bounds.size.x);
        }

        var allSrs = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in allSrs)
        {
            if (sr == null || sr.sprite == null) continue;
            if (dizzyVfx != null && sr.transform.IsChildOf(dizzyVfx.transform)) continue;
            string nameLower = sr.gameObject.name.ToLower();
            if (nameLower.Contains("popup") || nameLower.Contains("shadow") || nameLower.Contains("text") || 
                nameLower.Contains("shield") || nameLower.Contains("circle") || nameLower.Contains("cone") || 
                nameLower.Contains("indicator") || nameLower.Contains("range") || nameLower.Contains("dizzy") || nameLower.Contains("star")) continue;

            width = Mathf.Max(width, sr.bounds.size.x);
        }

        float scaleX = Mathf.Abs(transform.lossyScale.x);
        return Mathf.Max(width, 0.9f * scaleX);
    }

    private void DisableAIScripts()
    {
        disabledScripts.Clear();
        var allBehaviours = GetComponents<MonoBehaviour>();
        foreach (var mb in allBehaviours)
        {
            if (mb == null || mb == this) continue;
            string typeName = mb.GetType().Name;
            if (typeName.EndsWith("AI") || typeName.EndsWith("Enemy") || typeName.Contains("Boss") || typeName.Contains("Spawner"))
            {
                if (mb.enabled)
                {
                    mb.enabled = false;
                    disabledScripts.Add(mb);
                }
            }
        }
    }

    private void PauseAnimator()
    {
        if (animator == null) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (animator != null && !hasSavedAnimSpeed)
        {
            originalAnimSpeed = animator.speed;
            animator.speed = 0.05f; // Đứng hình trong lúc bị choáng
            hasSavedAnimSpeed = true;
        }
    }

    private void ResumeAnimator()
    {
        if (hasSavedAnimSpeed && animator != null)
        {
            animator.speed = originalAnimSpeed;
            hasSavedAnimSpeed = false;
        }
    }

    private void EnsureDizzyVFX()
    {
        if (dizzyVfx == null)
        {
            dizzyVfx = new GameObject("Stun_Dizzy_Stars");
            dizzyVfx.transform.position = GetHeadTopWorldPosition(gameObject);
            dizzyVfx.transform.rotation = Quaternion.identity;
            dizzyVfx.transform.localScale = Vector3.one;

            int highestOrder = 5000;
            string targetLayer = "Default";
            var allSrs = GetComponentsInChildren<SpriteRenderer>();
            foreach (var s in allSrs)
            {
                if (s == null) continue;
                if (s.sortingOrder > highestOrder) highestOrder = s.sortingOrder;
                targetLayer = s.sortingLayerName;
            }

            var light = dizzyVfx.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            light.color = new Color(1f, 0.88f, 0.2f, 1f);
            light.intensity = 1.0f;
            light.pointLightInnerRadius = 0.2f;
            light.pointLightOuterRadius = 0.85f;

            if (cachedStarSprite == null)
            {
                cachedStarSprite = GenerateDizzyStarSprite();
            }

            starTransforms = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var starGo = new GameObject($"Star_{i}");
                starGo.transform.SetParent(dizzyVfx.transform, false);
                var starSr = starGo.AddComponent<SpriteRenderer>();
                starSr.sprite = cachedStarSprite;
                starSr.sortingLayerName = targetLayer;
                starSr.sortingOrder = highestOrder + 50;
                starGo.transform.localScale = Vector3.one * 0.45f;
                starTransforms[i] = starGo.transform;
            }
        }
    }

    private static Sprite GenerateDizzyStarSprite()
    {
        int size = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color starGold = new Color(1f, 0.92f, 0.25f, 1f);
        Color starCore = new Color(1f, 1f, 0.85f, 1f);
        Color starOutline = new Color(0.85f, 0.55f, 0.05f, 1f);

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerR = size * 0.46f;
        float innerR = size * 0.16f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                Vector2 diff = pos - center;
                float dist = diff.magnitude;
                float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg + 90f;
                if (angle < 0f) angle += 360f;

                float modAngle = Mathf.Repeat(angle, 90f);
                float normAngle = Mathf.Abs(modAngle - 45f) / 45f;
                float expectedR = Mathf.Lerp(innerR, outerR, Mathf.Pow(normAngle, 2.2f));

                if (dist <= expectedR)
                {
                    if (dist <= expectedR * 0.35f)
                    {
                        tex.SetPixel(x, y, starCore);
                    }
                    else if (dist >= expectedR * 0.78f)
                    {
                        tex.SetPixel(x, y, starOutline);
                    }
                    else
                    {
                        tex.SetPixel(x, y, starGold);
                    }
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 24f);
    }

    public void RemoveStun()
    {
        ResumeAnimator();

        foreach (var mb in disabledScripts)
        {
            if (mb != null) mb.enabled = true;
        }
        disabledScripts.Clear();

        if (dizzyVfx != null)
        {
            dizzyVfx.SetActive(false);
        }

        stunTimer = 0f;
        lastStunEndTime = Time.time;
        s_LastStunEndTimes[gameObject.GetInstanceID()] = Time.time;

        enabled = false;
    }

    private static void CleanExpiredImmunities()
    {
        if (s_LastStunEndTimes.Count > 150)
        {
            float now = Time.time;
            var expiredKeys = new List<int>();
            foreach (var kvp in s_LastStunEndTimes)
            {
                if (now - kvp.Value > 10f) expiredKeys.Add(kvp.Key);
            }
            for (int i = 0; i < expiredKeys.Count; i++)
            {
                s_LastStunEndTimes.Remove(expiredKeys[i]);
            }
        }
    }

    void OnDestroy()
    {
        ResumeAnimator();

        foreach (var mb in disabledScripts)
        {
            if (mb != null) mb.enabled = true;
        }
        if (dizzyVfx != null) Destroy(dizzyVfx);
    }
}
