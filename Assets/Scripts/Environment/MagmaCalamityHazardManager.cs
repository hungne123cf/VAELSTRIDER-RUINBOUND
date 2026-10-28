using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Magma Calamity Hazard Manager (Chu Kỳ 3: Cõi Nham Thạch)
/// Cơ chế môi trường: Mưa thiên thạch & Vũng dung nham thiêu đốt:
/// - Xuất hiện các vòng cảnh báo hồng tâm lửa đỏ rực trên mặt đất (telegraph 0.9s).
/// - Thiên thạch nham thạch rực lửa rơi xuống nổ tung gây sát thương va chạm và rung chấn màn hình.
/// - Để lại bãi dung nham rực cháy trong 3.5s, buộc người chơi phải liên tục lướt (Dash) và né đòn.
/// </summary>
public class MagmaCalamityHazardManager : MonoBehaviour
{
    private static MagmaCalamityHazardManager instance;
    public static MagmaCalamityHazardManager Instance => instance;

    [Header("Settings")]
    private float minMeteorInterval = 2.2f;
    private float maxMeteorInterval = 3.8f;
    private float meteorTimer = 0f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        if (CorruptedCycleManager.CurrentRealm != CalamityRealm.MagmaCalamity)
        {
            Destroy(gameObject);
            return;
        }

        meteorTimer = Random.Range(1.5f, 2.8f);
        ApplyMagmaLighting();
        SpawnPersistentLavaFlows();
    }

    private void SpawnPersistentLavaFlows()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        Vector3 basePos = player != null ? player.transform.position : Vector3.zero;

        // Sinh 4 bãi dung nham nóng chảy cố định trên bản đồ
        Vector3[] poolOffsets = new Vector3[]
        {
            new Vector3(-4.5f, 2.8f, 0f),
            new Vector3(4.8f, -2.5f, 0f),
            new Vector3(-3.2f, -3.6f, 0f),
            new Vector3(3.8f, 3.5f, 0f)
        };

        foreach (var offset in poolOffsets)
        {
            SpawnMagmaPool(basePos + offset, duration: -1f); // Tồn tại vĩnh viễn suốt trận đấu
        }
    }

    void Update()
    {
        if (CorruptedCycleManager.CurrentRealm != CalamityRealm.MagmaCalamity) return;

        meteorTimer -= Time.deltaTime;
        if (meteorTimer <= 0f)
        {
            meteorTimer = Random.Range(minMeteorInterval, maxMeteorInterval);
            SpawnRandomMeteorNearPlayer();
        }
    }

    public static void EnsureManager()
    {
        if (CorruptedCycleManager.CurrentRealm != CalamityRealm.MagmaCalamity) return;

        if (FindAnyObjectByType<MagmaCalamityHazardManager>() == null)
        {
            GameObject obj = new GameObject("MagmaCalamityHazardManager");
            obj.AddComponent<MagmaCalamityHazardManager>();
        }
    }

    private void ApplyMagmaLighting()
    {
        var lights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l != null && l.lightType == Light2D.LightType.Global)
            {
                l.color = new Color(0.32f, 0.14f, 0.08f, 1f); // Smoldering Magma Crimson
                l.intensity = 0.88f;
            }
        }
    }

    private void SpawnRandomMeteorNearPlayer()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        Vector2 offset = Random.insideUnitCircle * Random.Range(1.8f, 5.5f);
        Vector3 targetPos = player.transform.position + new Vector3(offset.x, offset.y, 0f);

        StartCoroutine(MeteorStrikeRoutine(targetPos));
    }

    private IEnumerator MeteorStrikeRoutine(Vector3 targetPos)
    {
        // 1. Telegraph Warning Indicator (Vòng tròn cảnh báo lửa đỏ rực)
        GameObject telegraphObj = new GameObject("MeteorTelegraph");
        telegraphObj.transform.position = targetPos;
        var srTel = telegraphObj.AddComponent<SpriteRenderer>();
        srTel.sprite = GetSoftCircleSprite();
        srTel.color = new Color(1f, 0.25f, 0.1f, 0.45f);
        srTel.sortingOrder = 70;
        telegraphObj.transform.localScale = Vector3.one * 0.4f;

        float telDuration = 0.95f;
        float elapsed = 0f;
        while (elapsed < telDuration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / telDuration;
            telegraphObj.transform.localScale = Vector3.Lerp(Vector3.one * 0.4f, Vector3.one * 2.2f, p);
            float pulse = 0.35f + Mathf.Sin(elapsed * 15f) * 0.15f;
            srTel.color = new Color(1f, 0.2f, 0.05f, pulse);
            yield return null;
        }

        Destroy(telegraphObj);

        // 2. Thiên thạch lao từ trên trời xuống (Khối đá nham thạch bazan rực lửa)
        GameObject meteorObj = new GameObject("FallingMeteor");
        Vector3 startPos = targetPos + new Vector3(Random.Range(-1.5f, 1.5f), 8.5f, 0f);
        meteorObj.transform.position = startPos;

        var srMeteor = meteorObj.AddComponent<SpriteRenderer>();
        srMeteor.sprite = GetVolcanicRockSprite();
        srMeteor.sortingOrder = 5500;
        meteorObj.transform.localScale = Vector3.one * 1.5f;

        float fallDuration = 0.32f;
        float fallTime = 0f;
        while (fallTime < fallDuration)
        {
            fallTime += Time.deltaTime;
            float p = fallTime / fallDuration;
            meteorObj.transform.position = Vector3.Lerp(startPos, targetPos, p * p);
            meteorObj.transform.Rotate(0f, 0f, 480f * Time.deltaTime);

            // Sinh vệt tàn lửa phía sau thiên thạch đang lao xuống
            if (Random.value < 0.45f)
            {
                SpawnMeteorFlameParticle(meteorObj.transform.position);
            }
            yield return null;
        }

        Destroy(meteorObj);

        // 3. Vụ nổ khi va chạm mặt đất
        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.45f, 0.55f);
        SoundManager.EnsureInstance()?.PlayTitanSlam();

        // Gây sát thương va chạm trực tiếp nếu người chơi đứng trong bán kính nổ
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, 1.5f);
        foreach (var h in hits)
        {
            if (h.CompareTag("Player") || h.GetComponent<PlayerController>() != null)
            {
                var health = h.GetComponent<PlayerHealth>() ?? h.GetComponentInParent<PlayerHealth>();
                if (health != null)
                {
                    int dmg = Mathf.RoundToInt(health.MaxHealth * 0.15f * CorruptedCycleManager.DamageMultiplier);
                    health.TakeDamage(dmg);
                    DamagePopup.Create(h.transform.position + Vector3.up * 0.8f, dmg, false);

                    // Dính hiệu ứng Thiêu Đốt theo % máu + Vết Thương Sâu
                    PlayerBurnDebuff.Apply(h.gameObject, 4.0f, 0.04f);
                    GrievousWoundsDebuff.Apply(h.gameObject, 5.0f, 0.50f, 0.20f, allowPlayer: true);
                }
            }
        }

        // 4. Sinh ra Bãi Dung Nham Thiêu Đốt tồn tại 4.5s
        SpawnMagmaPool(targetPos);
    }

    private void SpawnMeteorFlameParticle(Vector3 pos)
    {
        GameObject p = new GameObject("MeteorFlameFX");
        p.transform.position = pos + (Vector3)Random.insideUnitCircle * 0.2f;
        p.transform.localScale = Vector3.one * Random.Range(0.25f, 0.45f);
        var sr = p.AddComponent<SpriteRenderer>();
        sr.sprite = GetSoftCircleSprite();
        sr.color = new Color(1.0f, Random.Range(0.3f, 0.7f), 0.05f, 0.85f);
        sr.sortingOrder = 5490;
        Destroy(p, 0.25f);
    }

    private void SpawnMagmaPool(Vector3 pos, float duration = 4.5f)
    {
        GameObject pool = new GameObject("MagmaHazardPool");
        pool.transform.position = pos;

        var sr = pool.AddComponent<SpriteRenderer>();
        sr.sprite = GetSoftCircleSprite();
        sr.color = new Color(1.0f, 0.28f, 0.02f, 0.75f); // Molten Lava Orange
        sr.sortingOrder = 65;
        pool.transform.localScale = Vector3.one * 2.2f;

        var col = pool.AddComponent<CircleCollider2D>();
        col.radius = 0.95f;
        col.isTrigger = true;

        var light = pool.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.pointLightInnerRadius = 0.4f;
        light.pointLightOuterRadius = 3.0f;
        light.color = new Color(1.0f, 0.45f, 0.10f, 1f);
        light.intensity = 1.8f;

        pool.AddComponent<MagmaHazardPoolTrigger>();
        if (duration > 0f)
        {
            Destroy(pool, duration);
        }
    }

    private static Sprite volcanicRockSpriteCache;
    public static Sprite GetVolcanicRockSprite()
    {
        if (volcanicRockSpriteCache != null) return volcanicRockSpriteCache;

        int res = 48;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(res * 0.5f, res * 0.5f);
        float radius = res * 0.42f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);

                // Tạo hình dạng đá gồ ghề góc cạnh (jagged asteroid rock)
                float angle = Mathf.Atan2(y - center.y, x - center.x);
                float noise = Mathf.Sin(angle * 5f) * 2.5f + Mathf.Cos(angle * 3f) * 2.0f;
                float rockRadius = radius + noise;

                if (dist <= rockRadius)
                {
                    // Vết nứt dung nham rực sáng (Magma Fissures / Cracks)
                    bool isCrack = (Mathf.Abs(x - y + 2) <= 1 && dist < radius * 0.85f) ||
                                   (Mathf.Abs(x + y - res) <= 1 && dist < radius * 0.75f) ||
                                   (Mathf.Abs(x - 24) <= 1 && y > 18 && y < 32);

                    if (isCrack)
                    {
                        // Khe nứt dung nham vàng rực - cam đỏ
                        float crackCore = Random.Range(0.85f, 1.0f);
                        tex.SetPixel(x, y, new Color(1.0f, crackCore * 0.75f, 0.1f, 1.0f));
                    }
                    else if (dist > rockRadius - 2f)
                    {
                        // Viền than hồng ngoài rìa đá
                        tex.SetPixel(x, y, new Color(0.95f, 0.35f, 0.05f, 1.0f));
                    }
                    else
                    {
                        // Thân đá bazan xám đen bóng bẩy
                        float shade = 0.15f + ((x + y) % 3) * 0.04f + (1f - dist / radius) * 0.08f;
                        tex.SetPixel(x, y, new Color(shade * 1.2f, shade, shade * 0.9f, 1.0f));
                    }
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        volcanicRockSpriteCache = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        return volcanicRockSpriteCache;
    }

    private static Sprite circleSpriteCache;
    public static Sprite GetSoftCircleSprite()
    {
        if (circleSpriteCache != null) return circleSpriteCache;

        int res = 64;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(res * 0.5f, res * 0.5f);
        float radius = res * 0.48f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    float alpha = Mathf.SmoothStep(1f, 0f, dist / radius);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        circleSpriteCache = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        return circleSpriteCache;
    }
}

/// <summary>
/// Trigger gây sát thương thiêu đốt 10% máu hiện tại + Vết Thương Sâu cho người chơi trong vũng dung nham
/// </summary>
public class MagmaHazardPoolTrigger : MonoBehaviour
{
    private float nextBurnTime = 0f;

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Time.time >= nextBurnTime)
        {
            // Nếu người chơi đang lướt (Dash), miễn nhiễm sát thương vũng nham!
            var pc = other.GetComponent<PlayerController>();
            if (pc != null && pc.IsDashing()) return;

            var health = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            if (health != null && !health.IsDead)
            {
                // TRỪ 10% MÁU HIỆN TẠI (TỐI THIỂU 2 HP) THEO ĐÚNG YÊU CẦU!
                int currentHp = health.GetCurrentHealth();
                int dmg = Mathf.Max(2, Mathf.RoundToInt(currentHp * 0.10f));
                health.TakeDamage(dmg);
                DamagePopup.Create(other.transform.position + Vector3.up * 0.7f, dmg, false);
                SoundManager.EnsureInstance()?.PlayPlayerHurt();

                // Găm hiệu ứng Thiêu Đốt và Vết Thương Sâu
                PlayerBurnDebuff.Apply(other.gameObject, 3.5f, 0.035f);
                GrievousWoundsDebuff.Apply(other.gameObject, 4.0f, 0.50f, 0.20f, allowPlayer: true);
            }
            nextBurnTime = Time.time + 0.75f;
        }
    }
}
