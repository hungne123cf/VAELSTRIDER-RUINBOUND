using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hiệu ứng VẾT THƯƠNG SÂU / TRÁI TIM NỨT (Grievous Wounds Debuff)
/// - Áp dụng khi chiêu 1 của Băng Kiếm Sư (Azure Blade) chém trúng quái vật / boss.
/// - Giảm 50% toàn bộ hiệu ứng hồi máu và hút máu của mục tiêu trong 5.0 giây.
/// - Khiến mục tiêu chịu thêm 20% sát thương băng giá từ Băng Kiếm Sư.
/// - Hiển thị biểu tượng TRÁI TIM NỨT (Broken Heart) kinh điển tương tự Liên Quân Mobile
///   lơ lửng trên đỉnh đầu mục tiêu, liên tục đập nhịp và nhấp nháy phát sáng.
/// </summary>
public class GrievousWoundsDebuff : MonoBehaviour
{
    private const float DEFAULT_DURATION = 5.0f;
    private const float DEFAULT_HEAL_REDUCTION = 0.50f;
    private const float DEFAULT_BONUS_DAMAGE = 0.20f;

    [Header("Debuff Parameters")]
    private float durationTimer = DEFAULT_DURATION;
    private float maxDuration = DEFAULT_DURATION;
    private float healReductionPercent = DEFAULT_HEAL_REDUCTION;
    private float bonusDamagePercent = DEFAULT_BONUS_DAMAGE;

    private const float ICON_BASE_SCALE = 0.28f;
    private const float HEAD_CLEARANCE = 0.08f;

    // Visual elements
    private GameObject iconHolder;
    private SpriteRenderer heartRenderer;
    private SpriteRenderer glowRenderer;
    private Collider2D targetCollider;
    private SpriteRenderer targetMainSr;

    private static Sprite cachedBrokenHeartSprite;
    private static Sprite cachedGlowSprite;

    public float RemainingDuration => durationTimer;
    public float HealReductionMultiplier => 1.0f - healReductionPercent;
    public float BonusDamageMultiplier => 1.0f + bonusDamagePercent;

    /// <summary>
    /// Găm hiệu ứng Vết Thương Sâu lên mục tiêu
    /// </summary>
    public static GrievousWoundsDebuff Apply(GameObject target, float duration = DEFAULT_DURATION, float healReduction = DEFAULT_HEAL_REDUCTION, float bonusDamage = DEFAULT_BONUS_DAMAGE, bool allowPlayer = false)
    {
        if (target == null) return null;

        // Bỏ qua Player nếu không có cờ cho phép (như thiên thạch dung nham hoặc quái vật Calamity)
        bool isPlayer = target.CompareTag("Player") || target.GetComponent<PlayerController>() != null;
        if (isPlayer && !allowPlayer) return null;

        // Bỏ qua các vật thể môi trường (thùng gỗ, cỏ cây, rương, trứng quái vỡ)
        if (target.GetComponent<BreakableProp>() != null || target.GetComponentInParent<BreakableProp>() != null) return null;
        if (target.GetComponent<TreasureChest>() != null || target.GetComponentInParent<TreasureChest>() != null) return null;

        // Tìm đúng GameObject gốc mang IDamageable
        GameObject enemyRoot = target;
        var damageable = target.GetComponent<IDamageable>() ?? target.GetComponentInParent<IDamageable>();
        if (damageable != null && (damageable as Component) != null)
        {
            enemyRoot = (damageable as Component).gameObject;
        }

        var debuff = enemyRoot.GetComponent<GrievousWoundsDebuff>();
        bool isNew = (debuff == null);
        if (isNew)
        {
            debuff = enemyRoot.AddComponent<GrievousWoundsDebuff>();
        }

        debuff.RefreshDebuff(duration, healReduction, bonusDamage, isNew);
        return debuff;
    }

    /// <summary>
    /// Kiểm tra mục tiêu có đang chịu Vết Thương Sâu không
    /// </summary>
    public static bool HasDebuff(GameObject target)
    {
        if (target == null) return false;
        var debuff = target.GetComponent<GrievousWoundsDebuff>() ?? target.GetComponentInParent<GrievousWoundsDebuff>();
        return debuff != null && debuff.durationTimer > 0.05f;
    }

    /// <summary>
    /// Lấy hệ số hồi máu (Mặc định 0.5f khi bị dính debuff, 1.0f khi bình thường)
    /// </summary>
    public static float GetHealingMultiplier(GameObject target)
    {
        if (target == null) return 1.0f;
        var debuff = target.GetComponent<GrievousWoundsDebuff>() ?? target.GetComponentInParent<GrievousWoundsDebuff>();
        if (debuff != null && debuff.durationTimer > 0.05f)
        {
            return debuff.HealReductionMultiplier;
        }
        return 1.0f;
    }

    /// <summary>
    /// Lấy hệ số sát thương nhận vào (Mặc định 1.20f = +20% sát thương khi bị Vết Thương Sâu)
    /// </summary>
    public static float GetDamageMultiplier(GameObject target)
    {
        if (target == null) return 1.0f;
        var debuff = target.GetComponent<GrievousWoundsDebuff>() ?? target.GetComponentInParent<GrievousWoundsDebuff>();
        if (debuff != null && debuff.durationTimer > 0.05f)
        {
            return debuff.BonusDamageMultiplier;
        }
        return 1.0f;
    }

    private void Awake()
    {
        CacheTargetRenderers();
    }

    private void CacheTargetRenderers()
    {
        targetCollider = GetComponent<Collider2D>();
        targetMainSr = GetComponent<SpriteRenderer>();

        if (targetMainSr == null)
        {
            var srs = GetComponentsInChildren<SpriteRenderer>();
            foreach (var s in srs)
            {
                if (s == null || s.sprite == null) continue;
                if (iconHolder != null && s.transform.IsChildOf(iconHolder.transform)) continue;
                string nm = s.gameObject.name.ToLower();
                if (nm.Contains("shadow") || nm.Contains("popup") || nm.Contains("cone") || nm.Contains("fx") || nm.Contains("glow")) continue;
                targetMainSr = s;
                break;
            }
        }
    }

    public Vector3 GetTargetHeadPosition()
    {
        if (targetMainSr == null || targetCollider == null)
        {
            CacheTargetRenderers();
        }

        // Nếu là Player: Cố định vị trí trên đầu, không đọc frame bounds để tránh bị rung lắc khi di chuyển
        if (GetComponent<PlayerHealth>() != null || GetComponent<PlayerController>() != null || GetComponentInParent<PlayerController>() != null)
        {
            return transform.position + new Vector3(0f, 1.25f, 0f);
        }

        // Xử lý riêng biệt cho các quái Map 3 & các quái đặc thù
        var slime = GetComponent<SlimeAI>() ?? GetComponentInParent<SlimeAI>();
        if (slime != null)
        {
            float cX = (targetCollider != null && targetCollider.enabled) ? targetCollider.bounds.center.x : transform.position.x;
            float tY = (targetCollider != null && targetCollider.enabled) ? targetCollider.bounds.max.y : (transform.position.y + 0.8f);
            return new Vector3(cX, tY + HEAD_CLEARANCE + 0.12f, 0f);
        }

        var goblin = GetComponent<GoblinAI>() ?? GetComponentInParent<GoblinAI>();
        if (goblin != null)
        {
            float cX = (targetCollider != null && targetCollider.enabled) ? targetCollider.bounds.center.x : transform.position.x;
            float tY = (targetCollider != null && targetCollider.enabled) ? targetCollider.bounds.max.y : (transform.position.y + 1.3f);
            return new Vector3(cX, tY + HEAD_CLEARANCE + 0.15f, 0f);
        }

        var skeleton = GetComponent<SkeletonAI>() ?? GetComponentInParent<SkeletonAI>();
        if (skeleton != null)
        {
            float cX = (targetCollider != null && targetCollider.enabled) ? targetCollider.bounds.center.x : transform.position.x;
            float tY = (targetCollider != null && targetCollider.enabled) ? targetCollider.bounds.max.y : (transform.position.y + 2.0f);
            return new Vector3(cX, tY + HEAD_CLEARANCE + 0.18f, 0f);
        }

        var bossKing = GetComponent<SkeletonKingBossAI>() ?? GetComponentInParent<SkeletonKingBossAI>();
        if (bossKing != null)
        {
            float cX = (targetCollider != null && targetCollider.enabled) ? targetCollider.bounds.center.x : transform.position.x;
            float tY = (targetCollider != null && targetCollider.enabled) ? targetCollider.bounds.max.y : (transform.position.y + 4.8f);
            return new Vector3(cX, tY + HEAD_CLEARANCE + 0.25f, 0f);
        }

        var egg = GetComponent<BoneEggCocoon>() ?? GetComponentInParent<BoneEggCocoon>();
        if (egg != null)
        {
            float cX = (targetCollider != null && targetCollider.enabled) ? targetCollider.bounds.center.x : transform.position.x;
            float tY = (targetCollider != null && targetCollider.enabled) ? targetCollider.bounds.max.y : (transform.position.y + 2.0f);
            return new Vector3(cX, tY + HEAD_CLEARANCE + 0.18f, 0f);
        }

        float topY = transform.position.y;
        float centerX = transform.position.x;

        // Trường hợp chung: Tránh trường hợp SpriteRenderer có viền trong suốt lớn
        float colTop = (targetCollider != null && targetCollider.enabled && targetCollider.bounds.size.y > 0.1f) 
            ? targetCollider.bounds.max.y : float.MinValue;
        float srTop = (targetMainSr != null && targetMainSr.sprite != null && targetMainSr.bounds.size.y > 0.1f) 
            ? targetMainSr.bounds.max.y : float.MinValue;

        if (colTop != float.MinValue && srTop != float.MinValue)
        {
            topY = (srTop > colTop + 0.6f) ? colTop : Mathf.Max(colTop, srTop);
            centerX = (targetCollider != null && targetCollider.enabled) ? targetCollider.bounds.center.x : targetMainSr.bounds.center.x;
        }
        else if (colTop != float.MinValue)
        {
            topY = colTop;
            centerX = targetCollider.bounds.center.x;
        }
        else if (srTop != float.MinValue)
        {
            topY = srTop;
            centerX = targetMainSr.bounds.center.x;
        }
        else
        {
            topY = transform.position.y + 0.9f;
            centerX = transform.position.x;
        }

        // Khoảng cách nhỏ vừa vặn ngay sát trên đỉnh đầu quái/boss
        return new Vector3(centerX, topY + HEAD_CLEARANCE, 0f);
    }

    public void RefreshDebuff(float duration, float healReduction, float bonusDamage, bool isNew)
    {
        durationTimer = Mathf.Max(durationTimer, duration);
        maxDuration = Mathf.Max(maxDuration, duration);
        healReductionPercent = healReduction;
        bonusDamagePercent = bonusDamage;

        EnsureIconCreated();

        if (isNew)
        {
            Vector3 headPos = GetTargetHeadPosition();
            SpawnImpactBurst(headPos);
        }
    }

    private void EnsureIconCreated()
    {
        if (iconHolder == null)
        {
            iconHolder = new GameObject("BrokenHeartDebuff_FX");
            // KHÔNG set parent vào quái để tránh bị nhân tỉ lệ scale hoặc méo hình khi quái lật hướng
            iconHolder.transform.SetParent(null);
            iconHolder.transform.position = GetTargetHeadPosition();
            iconHolder.transform.rotation = Quaternion.identity;

            // 1. Quầng sáng nền mỏng nhẹ
            GameObject glowObj = new GameObject("HeartGlow");
            glowObj.transform.SetParent(iconHolder.transform, false);
            glowRenderer = glowObj.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = GetGlowSprite();
            glowRenderer.color = new Color(0.12f, 0.12f, 0.18f, 0.25f);
            glowRenderer.sortingOrder = 995;
            glowObj.transform.localScale = Vector3.one * (ICON_BASE_SCALE * 1.35f);

            // 2. Icon Trái Tim Nứt ĐEN nhỏ gọn
            GameObject heartObj = new GameObject("HeartSprite");
            heartObj.transform.SetParent(iconHolder.transform, false);
            heartRenderer = heartObj.AddComponent<SpriteRenderer>();
            heartRenderer.sprite = GetBrokenHeartSprite();
            heartRenderer.sortingOrder = 996;
            heartObj.transform.localScale = Vector3.one * ICON_BASE_SCALE;
        }
    }

    private void Update()
    {
        if (durationTimer > 0f)
        {
            durationTimer -= Time.deltaTime;

            if (iconHolder != null)
            {
                // Hiệu ứng nhịp đập tim nhẹ nhàng
                float pulse = 1.0f + 0.05f * Mathf.Sin(Time.time * 5.5f);
                if (heartRenderer != null)
                {
                    heartRenderer.transform.localScale = Vector3.one * (ICON_BASE_SCALE * pulse);
                }
                if (glowRenderer != null)
                {
                    glowRenderer.transform.localScale = Vector3.one * (ICON_BASE_SCALE * 1.35f * pulse);
                }

                // GẦN HẾT THỜI GIAN HIỆU LỰC (Dưới 2.0 giây): Nhấp nháy chớp tắt mờ ảo (Ethereal Blinking)
                if (durationTimer <= 2.0f)
                {
                    float progress = Mathf.Clamp01(durationTimer / 2.0f); // 1.0 -> 0.0
                    float blinkWave = Mathf.Sin((2.0f - durationTimer) * 16f);
                    float blink = 0.45f + 0.55f * Mathf.Abs(blinkWave);
                    float fade = Mathf.Pow(progress, 0.65f);
                    float alpha = Mathf.Clamp01(blink * fade);

                    if (heartRenderer != null)
                    {
                        Color hc = heartRenderer.color;
                        hc.a = alpha;
                        heartRenderer.color = hc;
                    }
                    if (glowRenderer != null)
                    {
                        Color gc = glowRenderer.color;
                        gc.a = alpha * 0.30f;
                        glowRenderer.color = gc;
                    }
                }
                else
                {
                    if (heartRenderer != null && heartRenderer.color.a < 0.99f)
                    {
                        Color hc = heartRenderer.color;
                        hc.a = 1.0f;
                        heartRenderer.color = hc;
                    }
                }
            }

            if (durationTimer <= 0f)
            {
                DestroyDebuff();
            }
        }
    }

    private void LateUpdate()
    {
        // Luôn bám sát chính xác đỉnh đầu của quái vật / boss theo từng frame
        if (iconHolder != null)
        {
            if (this == null || gameObject == null || !gameObject.activeInHierarchy)
            {
                DestroyDebuff();
                return;
            }

            Vector3 headPos = GetTargetHeadPosition();
            iconHolder.transform.position = headPos;
            iconHolder.transform.rotation = Quaternion.identity;
        }
    }

    private void DestroyDebuff()
    {
        if (iconHolder != null)
        {
            Destroy(iconHolder);
            iconHolder = null;
        }
        Destroy(this);
    }

    private void OnDisable()
    {
        DestroyDebuff();
    }

    private void OnDestroy()
    {
        DestroyDebuff();
    }

    private void SpawnImpactBurst(Vector3 position)
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject spark = new GameObject("HeartSpark");
            spark.transform.position = position + new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(-0.08f, 0.08f), 0f);
            var sr = spark.AddComponent<SpriteRenderer>();
            sr.sprite = GetGlowSprite();
            sr.color = new Color(0.20f, 0.22f, 0.28f, 0.85f);
            sr.sortingOrder = 997;
            spark.transform.localScale = Vector3.one * Random.Range(0.05f, 0.10f);

            var rb = spark.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0.25f;
            Vector2 randDir = new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(0.5f, 1.5f));
            rb.linearVelocity = randDir;

            Destroy(spark, Random.Range(0.20f, 0.35f));
        }
    }

    // =========================================================================
    // KHỞI TẠO SPRITE TRÁI TIM NỨT MÀU ĐEN (BLACK BROKEN HEART SPRITE)
    // =========================================================================
    public static Sprite GetBrokenHeartSprite()
    {
        if (cachedBrokenHeartSprite != null) return cachedBrokenHeartSprite;

        int size = 96;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color transparent = new Color(0, 0, 0, 0);
        Color[] pixels = new Color[size * size];

        Color outerBorder = new Color(0.28f, 0.30f, 0.36f, 0.95f);    // Viền kim loại xám sáng nhẹ để nổi bật trên nền tối
        Color innerBorder = new Color(0.02f, 0.02f, 0.03f, 1.0f);     // Viền đen tuyền
        Color crackGlow = new Color(1.0f, 0.95f, 0.70f, 1.0f);         // Vết nứt sáng ánh vàng kim rực rỡ
        Color crackRim = new Color(0.85f, 0.22f, 0.45f, 0.95f);        // Rìa vết nứt ánh đỏ tím huyền bí
        Color heartTop = new Color(0.18f, 0.19f, 0.23f, 1.0f);         // Thân trên màu đen than chì (Obsidian Black)
        Color heartBot = new Color(0.05f, 0.05f, 0.07f, 1.0f);         // Thân dưới màu đen tuyền
        Color highlight = new Color(0.42f, 0.44f, 0.52f, 0.85f);       // Ánh bạc phản quang kim loại đen ở tai trái

        float invSize = 1.0f / size;

        for (int y = 0; y < size; y++)
        {
            float v = ((y + 0.5f) * invSize * 2.2f) - 1.1f;
            float vy = v + 0.12f;

            for (int x = 0; x < size; x++)
            {
                float u = ((x + 0.5f) * invSize * 2.2f) - 1.1f;

                // 1. Dáng quả tim (Two lobes + Tapered lower cone)
                bool insideHeart = false;
                float heartDist = 1.0f;

                if (vy >= 0.20f)
                {
                    float dL = Mathf.Sqrt((u + 0.42f) * (u + 0.42f) + (vy - 0.20f) * (vy - 0.20f));
                    float dR = Mathf.Sqrt((u - 0.42f) * (u - 0.42f) + (vy - 0.20f) * (vy - 0.20f));
                    if (dL <= 0.45f || dR <= 0.45f)
                    {
                        insideHeart = true;
                        heartDist = Mathf.Min(dL, dR) / 0.45f;
                    }
                }
                else if (vy >= -0.85f)
                {
                    float t = (vy - (-0.85f)) / (0.20f - (-0.85f));
                    float maxU = Mathf.Pow(t, 0.82f) * 0.87f;
                    if (Mathf.Abs(u) <= maxU)
                    {
                        insideHeart = true;
                        heartDist = Mathf.Abs(u) / Mathf.Max(0.01f, maxU);
                    }
                }

                if (!insideHeart)
                {
                    pixels[y * size + x] = transparent;
                    continue;
                }

                // 2. Vết nứt tia sét (Lightning Zigzag Fracture)
                float crackX = 0f;
                if (vy > 0.25f)
                {
                    float p = (vy - 0.25f) / 0.35f;
                    crackX = -0.09f * Mathf.Sin(p * Mathf.PI);
                }
                else if (vy > -0.05f)
                {
                    float p = (vy - (-0.05f)) / 0.30f;
                    crackX = Mathf.Lerp(0.12f, -0.02f, p);
                }
                else if (vy > -0.45f)
                {
                    float p = (vy - (-0.45f)) / 0.40f;
                    crackX = Mathf.Lerp(-0.11f, 0.12f, p);
                }
                else
                {
                    float p = (vy - (-0.85f)) / 0.40f;
                    crackX = Mathf.Lerp(0.0f, -0.11f, p);
                }

                float distToCrack = Mathf.Abs(u - crackX);

                // Khe hở vết nứt rỗng xuyên thấu (nhìn rõ quả tim đen bị xẻ đôi)
                if (distToCrack < 0.048f)
                {
                    pixels[y * size + x] = transparent;
                    continue;
                }
                // Rìa vàng kim phát quang
                if (distToCrack < 0.088f)
                {
                    float glowT = (distToCrack - 0.048f) / 0.040f;
                    pixels[y * size + x] = Color.Lerp(crackGlow, crackRim, glowT);
                    continue;
                }
                // Rìa chuyển tiếp đỏ tím
                if (distToCrack < 0.125f)
                {
                    float glowT = (distToCrack - 0.088f) / 0.037f;
                    pixels[y * size + x] = Color.Lerp(crackRim, heartTop, glowT);
                    continue;
                }

                // 3. Viền tối bao quanh (Outer metal rim & inner border)
                if (heartDist >= 0.91f)
                {
                    pixels[y * size + x] = outerBorder;
                    continue;
                }
                if (heartDist >= 0.83f)
                {
                    pixels[y * size + x] = innerBorder;
                    continue;
                }

                // 4. Gradient màu sắc thân tim ĐEN
                float vertGrad = Mathf.Clamp01((vy + 0.85f) / 1.45f);
                Color baseCol = Color.Lerp(heartBot, heartTop, vertGrad);

                // Ánh sáng kim loại bóng trên tai trái
                if (u < -0.22f && vy > 0.28f && distToCrack > 0.15f)
                {
                    float dHi = Mathf.Sqrt((u + 0.45f) * (u + 0.45f) + (vy - 0.42f) * (vy - 0.42f));
                    if (dHi < 0.22f)
                    {
                        baseCol = Color.Lerp(highlight, baseCol, dHi / 0.22f);
                    }
                }

                pixels[y * size + x] = baseCol;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedBrokenHeartSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedBrokenHeartSprite;
    }

    public static Sprite GetGlowSprite()
    {
        if (cachedGlowSprite != null) return cachedGlowSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                if (dist <= 1.0f)
                {
                    float alpha = Mathf.Pow(1.0f - dist, 1.8f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedGlowSprite;
    }
}
