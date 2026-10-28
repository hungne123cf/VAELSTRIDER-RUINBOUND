using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Frostfall Blizzard Hazard Manager (Chu Kỳ 5: Bão Tuyết Băng Giá)
/// Cơ chế môi trường: Mưa hoa tuyết băng giá:
/// - Các bông tuyết băng giá rơi lơ lửng ngẫu nhiên trên đấu trường.
/// - Chạm phải bông tuyết:
///   1) Làm chậm -45% Tốc độ di chuyển và tốc đánh trong 2.0 giây.
///   2) 35% tỉ lệ kích hoạt [ĐÓNG BĂNG & KHÓA CHIÊU] đúng 0.5 giây!
/// - Người chơi có thể chém kiếm hoặc Dash để phá hủy bông tuyết trước khi chạm vào!
/// </summary>
public class FrostfallBlizzardHazardManager : MonoBehaviour
{
    private static FrostfallBlizzardHazardManager instance;
    public static FrostfallBlizzardHazardManager Instance => instance;

    [Header("Settings")]
    private float spawnInterval = 1.8f;
    private float spawnTimer = 0f;

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
        if (CorruptedCycleManager.CurrentRealm != CalamityRealm.FrostfallBlizzard)
        {
            Destroy(gameObject);
            return;
        }

        spawnTimer = 1.2f;
        ApplyBlizzardLighting();
    }

    void Update()
    {
        if (CorruptedCycleManager.CurrentRealm != CalamityRealm.FrostfallBlizzard) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = Random.Range(0.45f, 0.85f); // Rơi thường xuyên liên tục
            SpawnSnowflakeCluster();
        }
    }

    public static void EnsureManager()
    {
        if (CorruptedCycleManager.CurrentRealm != CalamityRealm.FrostfallBlizzard) return;

        if (FindAnyObjectByType<FrostfallBlizzardHazardManager>() == null)
        {
            GameObject obj = new GameObject("FrostfallBlizzardHazardManager");
            obj.AddComponent<FrostfallBlizzardHazardManager>();
        }
    }

    private void ApplyBlizzardLighting()
    {
        var lights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l != null && l.lightType == Light2D.LightType.Global)
            {
                l.color = new Color(0.14f, 0.22f, 0.35f, 1f); // Glacial Arctic Cyan
                l.intensity = 0.88f;
            }
        }
    }

    private void SpawnSnowflakeCluster()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        int count = Random.Range(4, 8); // Tăng mật độ bông tuyết dày đặc
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * Random.Range(2.5f, 7.5f);
            Vector3 spawnPos = player.transform.position + new Vector3(offset.x, offset.y + 5.5f, 0f);
            SpawnSingleSnowflake(spawnPos);
        }
    }

    private void SpawnSingleSnowflake(Vector3 pos)
    {
        GameObject flake = new GameObject("GlacialSnowflake");
        flake.transform.position = pos;

        var sr = flake.AddComponent<SpriteRenderer>();
        sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        sr.color = new Color(0.65f, 0.90f, 1.0f, 0.9f); // Crystal Ice Cyan
        sr.sortingOrder = 5400;
        flake.transform.localScale = Vector3.one * Random.Range(0.45f, 0.7f);

        var col = flake.AddComponent<CircleCollider2D>();
        col.radius = 0.45f;
        col.isTrigger = true;

        var light = flake.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.pointLightInnerRadius = 0.1f;
        light.pointLightOuterRadius = 0.8f;
        light.color = new Color(0.45f, 0.85f, 1f, 1f);
        light.intensity = 1.1f;

        flake.AddComponent<FrostflakeDrift>();
        Destroy(flake, 7.5f);
    }
}

/// <summary>
/// Hành vi lơ lửng rơi chậm và va chạm của hạt tuyết băng giá
/// </summary>
public class FrostflakeDrift : MonoBehaviour, IDamageable
{
    private float driftSpeed;
    private float swaySpeed;
    private float swayAmount;
    private float startX;
    private float aliveTime = 0f;

    void Start()
    {
        driftSpeed = Random.Range(2.8f, 4.6f); // Rơi nhanh hơn hẳn theo bão tuyết cuồn cuộn
        swaySpeed = Random.Range(3.5f, 6.0f);
        swayAmount = Random.Range(0.4f, 1.1f);
        startX = transform.position.x;
    }

    void Update()
    {
        aliveTime += Time.deltaTime;
        float x = startX + Mathf.Sin(aliveTime * swaySpeed) * swayAmount;
        float y = transform.position.y - (driftSpeed * Time.deltaTime);
        transform.position = new Vector3(x, y, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                // Nếu đang lướt (Dash), đâm vỡ hạt tuyết an toàn!
                if (pc.IsDashing())
                {
                    Shatter();
                    return;
                }

                // 1. Áp dụng Làm Chậm (-45% tốc chạy trong 2.0s)
                pc.ApplySlow(0.55f, 2.0f);

                // 2. 35% tỉ lệ [ĐÓNG BĂNG & KHÓA CHIÊU 0.5 GIÂY]
                if (Random.value <= 0.35f)
                {
                    pc.ApplyFreeze(0.5f);
                }
            }

            Shatter();
        }
    }

    // Người chơi có thể chém kiếm để phá vỡ hạt tuyết an toàn
    public void TakeDamage(int damage)
    {
        Shatter();
    }

    public void TakeDamage(int damage, DamageType type)
    {
        Shatter();
    }

    private void Shatter()
    {
        SoundManager.EnsureInstance()?.PlayHitImpact();
        Destroy(gameObject);
    }
}
