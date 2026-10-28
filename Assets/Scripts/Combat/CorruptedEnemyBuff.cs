using UnityEngine;
using System.Collections;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Corrupted Enemy Buff & Void Hazard
/// Tự động áp dụng cho mọi quái vật trong Chế độ Vòng Lặp Tha Hóa (Cycle II):
/// - Phủ ánh sáng tử khí tím tím ma quái (Void Aura)
/// - Tăng 25% tốc độ truy kích người chơi
/// - Khi bị tiêu diệt: Phát nổ để lại một Vệt Tử Khí Axit Hư Không (Void Hazard Pool) tồn tại 3.5 giây.
/// - Đòi hỏi người chơi mobile phải liên tục Dash né tránh linh hoạt chuẩn phong cách Dash Fantasy!
/// </summary>
public class CorruptedEnemyBuff : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color originalColor;
    private bool isEnraged = false;

    void Start()
    {
        if (!CorruptedCycleManager.IsCorruptedActive)
        {
            Destroy(this);
            return;
        }

        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            originalColor = sr.color;
            Color tintColor;
            switch (CorruptedCycleManager.CurrentRealm)
            {
                case CalamityRealm.VoidNightmare:
                    tintColor = new Color(0.85f, 0.45f, 1.0f, 1f); // Amethyst Void
                    break;
                case CalamityRealm.MagmaCalamity:
                    tintColor = new Color(1.0f, 0.45f, 0.25f, 1f); // Molten Magma
                    break;
                case CalamityRealm.AcidDownpour:
                    tintColor = new Color(0.35f, 0.95f, 0.45f, 1f); // Toxic Acid
                    break;
                case CalamityRealm.FrostfallBlizzard:
                    tintColor = new Color(0.45f, 0.85f, 1.0f, 1f); // Glacial Frost
                    break;
                default:
                    tintColor = new Color(0.9f, 0.65f, 1.0f, 1f);
                    break;
            }
            sr.color = tintColor;
        }
    }

    void OnDestroy()
    {
        if (!CorruptedCycleManager.IsCorruptedActive || !gameObject.scene.isLoaded) return;

        Vector3 deathPos = transform.position;
        switch (CorruptedCycleManager.CurrentRealm)
        {
            case CalamityRealm.VoidNightmare:
                // Đã loại bỏ chấm tím dưới chân theo yêu cầu người chơi
                break;
            case CalamityRealm.MagmaCalamity:
                SpawnSmallMagmaPool(deathPos);
                break;
            case CalamityRealm.AcidDownpour:
                // Đã loại bỏ chấm xanh lá rơi ra. Trực tiếp hồi phục khi diệt quái!
                if (PlayerHealth.Instance != null && !PlayerHealth.Instance.IsDead)
                {
                    PlayerHealth.Instance.Heal(Mathf.Max(2, Mathf.RoundToInt(PlayerHealth.Instance.MaxHealth * 0.02f)));
                }
                break;
            case CalamityRealm.FrostfallBlizzard:
                SpawnFrostShardBurst(deathPos);
                break;
            default:
                break;
        }
    }

    public static void AttachIfCorrupted(GameObject enemy)
    {
        if (!CorruptedCycleManager.IsCorruptedActive || enemy == null) return;
        if (enemy.GetComponent<CorruptedEnemyBuff>() == null)
        {
            enemy.AddComponent<CorruptedEnemyBuff>();
        }
    }

    /// <summary>
    /// Vết nổ co thắt hư không (Void Implosion): Hút nhẹ và phát nổ sau 0.75s
    /// </summary>
    private void SpawnVoidImplosion(Vector3 pos)
    {
        GameObject implosion = new GameObject("VoidImplosion");
        implosion.transform.position = pos;
        var comp = implosion.AddComponent<VoidImplosionHazard>();
        comp.Initialize(pos);
    }

    private void SpawnSmallMagmaPool(Vector3 pos)
    {
        GameObject pool = new GameObject("SmallMagmaPool");
        pool.transform.position = pos;
        var srPool = pool.AddComponent<SpriteRenderer>();
        srPool.sprite = CreateCircleSprite();
        srPool.color = new Color(1.0f, 0.35f, 0.05f, 0.55f);
        srPool.sortingOrder = 55;
        pool.transform.localScale = Vector3.one * 1.2f;

        var col = pool.AddComponent<CircleCollider2D>();
        col.radius = 0.65f;
        col.isTrigger = true;
        pool.AddComponent<MagmaHazardPoolTrigger>();
        Destroy(pool, 2.5f);
    }

    private void SpawnNeutralizationDroplet(Vector3 pos)
    {
        GameObject drop = new GameObject("AcidNeutralizationDroplet");
        drop.transform.position = pos;
        var srDrop = drop.AddComponent<SpriteRenderer>();
        srDrop.sprite = CreateCircleSprite();
        srDrop.color = new Color(0.25f, 0.95f, 0.35f, 0.85f);
        srDrop.sortingOrder = 75;
        drop.transform.localScale = Vector3.one * 0.75f;

        var col = drop.AddComponent<CircleCollider2D>();
        col.radius = 0.55f;
        col.isTrigger = true;
        drop.AddComponent<AcidNeutralizationDropletTrigger>();
        Destroy(drop, 8.0f);
    }

    private void SpawnFrostShardBurst(Vector3 pos)
    {
        GameObject burst = new GameObject("FrostShardBurst");
        burst.transform.position = pos;
        var srBurst = burst.AddComponent<SpriteRenderer>();
        srBurst.sprite = CreateCircleSprite();
        srBurst.color = new Color(0.45f, 0.85f, 1f, 0.45f);
        srBurst.sortingOrder = 55;
        burst.transform.localScale = Vector3.one * 1.3f;

        var col = burst.AddComponent<CircleCollider2D>();
        col.radius = 0.7f;
        col.isTrigger = true;
        burst.AddComponent<FrostSlowPoolTrigger>();
        Destroy(burst, 2.5f);
    }

    private void SpawnVoidHazardPool(Vector3 pos)
    {
        GameObject pool = new GameObject("VoidHazardPool");
        pool.transform.position = pos;

        var srPool = pool.AddComponent<SpriteRenderer>();
        srPool.sprite = CreateCircleSprite();
        srPool.color = new Color(0.55f, 0.15f, 0.85f, 0.45f);
        srPool.sortingOrder = 50;
        pool.transform.localScale = Vector3.one * 1.6f;

        var col = pool.AddComponent<CircleCollider2D>();
        col.radius = 0.8f;
        col.isTrigger = true;

        pool.AddComponent<VoidHazardPoolTrigger>();
        Destroy(pool, 3.5f);
    }

    private static Sprite circleSpriteCache;
    private static Sprite CreateCircleSprite()
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
/// Trigger gây sát thương nếu người chơi dẫm vào bãi tử khí hư không
/// </summary>
public class VoidHazardPoolTrigger : MonoBehaviour
{
    private float nextDamageTime = 0f;

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Time.time >= nextDamageTime)
        {
            var health = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                int dmg = Mathf.Max(2, Mathf.RoundToInt(3 * CorruptedCycleManager.DamageMultiplier));
                health.TakeDamage(dmg);
                SoundManager.EnsureInstance()?.PlayPlayerHurt();
                DamagePopup.Create(other.transform.position + Vector3.up * 0.8f, dmg, false);
            }
            nextDamageTime = Time.time + 0.65f;
        }
    }
}

/// <summary>
/// Vết nổ Hư Không: Co thắt hút nhẹ và phát nổ sau 0.75 giây
/// </summary>
public class VoidImplosionHazard : MonoBehaviour
{
    public void Initialize(Vector3 pos)
    {
        StartCoroutine(ImplosionRoutine(pos));
    }

    private IEnumerator ImplosionRoutine(Vector3 pos)
    {
        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        sr.color = new Color(0.75f, 0.25f, 1f, 0.5f);
        sr.sortingOrder = 60;
        transform.localScale = Vector3.one * 1.8f;

        float timer = 0.75f;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            float pulse = 0.5f + Mathf.Sin(timer * 20f) * 0.25f;
            sr.color = new Color(0.85f, 0.35f, 1.0f, pulse);
            transform.localScale = Vector3.Lerp(Vector3.one * 0.4f, Vector3.one * 2.0f, timer / 0.75f);
            yield return null;
        }

        // Bùng nổ Hư Không
        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.25f, 0.35f);
        SoundManager.EnsureInstance()?.PlayHitImpact(true, true);

        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 1.4f);
        foreach (var h in hits)
        {
            if (h.CompareTag("Player") || h.GetComponent<PlayerController>() != null)
            {
                var health = h.GetComponent<PlayerHealth>() ?? h.GetComponentInParent<PlayerHealth>();
                if (health != null)
                {
                    int dmg = Mathf.RoundToInt(5 * CorruptedCycleManager.DamageMultiplier);
                    health.TakeDamage(dmg);
                    DamagePopup.Create(h.transform.position + Vector3.up * 0.8f, dmg, false);
                }
            }
        }

        Destroy(gameObject);
    }
}

/// <summary>
/// Giọt Tinh Chất Trung Hòa Axit rơi ra khi quái chết
/// </summary>
public class AcidNeutralizationDropletTrigger : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            var manager = AcidDownpourHazardManager.Instance;
            var health = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                health.Heal(Mathf.Max(3, Mathf.RoundToInt(health.MaxHealth * 0.04f)));
            }
            DamagePopup.CreateText(transform.position + Vector3.up * 0.8f, LocalizationManager.T("+TRUNG HÒA AXIT", "+ACID SHIELD"), new Color(0.25f, 0.95f, 0.35f));
            SoundManager.EnsureInstance()?.PlayShardPickup();
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Bãi băng giá làm chậm
/// </summary>
public class FrostSlowPoolTrigger : MonoBehaviour
{
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (pc != null && !pc.IsDashing())
            {
                pc.ApplySlow(0.65f, 1.5f);
            }
        }
    }
}
