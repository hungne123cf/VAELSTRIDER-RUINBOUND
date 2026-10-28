using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hiệu ứng NỌC ĐỘC ĂN MÒN (Corrosive Venom Debuff) từ Lõi Nâng Cấp "Độc Dược Ăn Mòn" (PoisonTouch)
/// - Không dùng icon lơ lửng trên đầu để tránh che khuất tầm nhìn.
/// - BIẾN TOÀN THÂN KẺ ĐỊCH THÀNH MÀU XANH LÁ ĐỘC DƯỢC (Toxic Emerald/Venom Green) rực rỡ và nhấp nháy theo nhịp độc ăn mòn.
/// - Kẻ địch nhận thêm +20% SÁT THƯƠNG trong 4.0 giây từ mọi nguồn công kích.
/// - Phát ra các bong bóng / khói acid độc bay lên từ thân thể quái vật.
/// </summary>
public class CorrosiveVenomDebuff : MonoBehaviour
{
    private const float DEFAULT_DURATION = 4.0f;
    private const float DEFAULT_BONUS_DAMAGE = 0.20f;

    [Header("Debuff Parameters")]
    private float durationTimer = DEFAULT_DURATION;
    private float maxDuration = DEFAULT_DURATION;
    private float bonusDamagePercent = DEFAULT_BONUS_DAMAGE;

    // Quản lý toàn bộ SpriteRenderer của cơ thể kẻ địch
    private List<SpriteRenderer> targetRenderers = new List<SpriteRenderer>();
    private Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();

    private Collider2D targetCollider;
    private float nextBubbleTime = 0f;
    private static Sprite cachedGlowSprite;

    // Màu xanh lá độc dược rực rỡ chuẩn Fantasy
    private static readonly Color ToxicGreenA = new Color(0.20f, 0.95f, 0.32f, 1f);
    private static readonly Color ToxicGreenB = new Color(0.42f, 1.00f, 0.50f, 1f);

    public float RemainingDuration => durationTimer;
    public float BonusDamageMultiplier => 1.0f + bonusDamagePercent;

    /// <summary>
    /// Găm hiệu ứng Nọc Độc Ăn Mòn lên mục tiêu
    /// </summary>
    public static CorrosiveVenomDebuff Apply(GameObject target, float duration = DEFAULT_DURATION, float bonusDamage = DEFAULT_BONUS_DAMAGE)
    {
        if (target == null) return null;

        // Bỏ qua Player
        if (target.CompareTag("Player") && target.GetComponent<PlayerController>() != null) return null;

        // Bỏ qua các vật thể môi trường vỡ (thùng gỗ, cỏ cây, rương)
        if (target.GetComponent<BreakableProp>() != null || target.GetComponentInParent<BreakableProp>() != null) return null;
        if (target.GetComponent<TreasureChest>() != null || target.GetComponentInParent<TreasureChest>() != null) return null;

        // Tìm đúng GameObject gốc mang IDamageable
        GameObject enemyRoot = target;
        var damageable = target.GetComponent<IDamageable>() ?? target.GetComponentInParent<IDamageable>();
        if (damageable != null && (damageable as Component) != null)
        {
            enemyRoot = (damageable as Component).gameObject;
        }

        var debuff = enemyRoot.GetComponent<CorrosiveVenomDebuff>();
        bool isNew = (debuff == null);
        if (isNew)
        {
            debuff = enemyRoot.AddComponent<CorrosiveVenomDebuff>();
        }

        debuff.RefreshDebuff(duration, bonusDamage, isNew);
        return debuff;
    }

    /// <summary>
    /// Kiểm tra mục tiêu có đang chịu Nọc Độc Ăn Mòn không
    /// </summary>
    public static bool HasDebuff(GameObject target)
    {
        if (target == null) return false;
        var debuff = target.GetComponent<CorrosiveVenomDebuff>() ?? target.GetComponentInParent<CorrosiveVenomDebuff>();
        return debuff != null && debuff.durationTimer > 0.05f;
    }

    /// <summary>
    /// Lấy hệ số sát thương nhận vào (+20% sát thương khi bị dính Độc Ăn Mòn)
    /// </summary>
    public static float GetDamageMultiplier(GameObject target)
    {
        if (target == null) return 1.0f;
        var debuff = target.GetComponent<CorrosiveVenomDebuff>() ?? target.GetComponentInParent<CorrosiveVenomDebuff>();
        if (debuff != null && debuff.durationTimer > 0.05f)
        {
            return debuff.BonusDamageMultiplier;
        }
        return 1.0f;
    }

    private void Awake()
    {
        targetCollider = GetComponent<Collider2D>();
        CacheEnemyRenderers();
    }

    /// <summary>
    /// Thu thập toàn bộ SpriteRenderer trên thân thể quái để nhuộm xanh độc dược
    /// Loại trừ bóng dưới chân (shadow), thanh máu, popup chữ...
    /// </summary>
    private void CacheEnemyRenderers()
    {
        var allSrs = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in allSrs)
        {
            if (sr == null || sr.sprite == null) continue;

            string nm = sr.gameObject.name.ToLower();
            if (nm.Contains("shadow") || nm.Contains("popup") || nm.Contains("text") || 
                nm.Contains("cone") || nm.Contains("healthbar") || nm.Contains("hp") || 
                nm.Contains("canvas") || nm.Contains("indicator"))
            {
                continue;
            }

            if (!targetRenderers.Contains(sr))
            {
                targetRenderers.Add(sr);
                if (!originalColors.ContainsKey(sr))
                {
                    originalColors[sr] = sr.color;
                }
            }
        }
    }

    public void RefreshDebuff(float duration, float bonusDamage, bool isNew)
    {
        durationTimer = Mathf.Max(durationTimer, duration);
        maxDuration = Mathf.Max(maxDuration, duration);
        bonusDamagePercent = bonusDamage;

        CacheEnemyRenderers();

        // Xóa bất kỳ icon thừa nào nếu trước đó còn sót lại
        var oldIcon = transform.Find("CorrosiveVenom_FX");
        if (oldIcon != null) Destroy(oldIcon.gameObject);

        // Nhuộm ngay lập tức cơ thể sang màu xanh lá độc dược
        ApplyToxicGreenColor(ToxicGreenA);

        if (isNew)
        {
            SpawnAcidBurst(transform.position);
        }
    }

    public Vector3 GetTargetHeadPosition()
    {
        float topY = transform.position.y;
        float centerX = transform.position.x;

        if (targetCollider != null && targetCollider.bounds.size.y > 0.1f)
        {
            Bounds b = targetCollider.bounds;
            topY = b.max.y;
            centerX = b.center.x;
        }
        else if (targetRenderers.Count > 0 && targetRenderers[0] != null)
        {
            Bounds b = targetRenderers[0].bounds;
            topY = b.max.y;
            centerX = b.center.x;
        }
        else
        {
            topY = transform.position.y + 0.6f;
        }

        return new Vector3(centerX, topY + 0.10f, 0f);
    }

    void Update()
    {
        if (durationTimer <= 0f)
        {
            RemoveDebuff();
            return;
        }

        durationTimer -= Time.deltaTime;

        // Biến thân thể quái thành màu xanh lá độc dược nhấp nháy sống động
        float pulse = Mathf.PingPong(Time.time * 5.0f, 1f);
        Color currentToxicColor = Color.Lerp(ToxicGreenA, ToxicGreenB, pulse);
        ApplyToxicGreenColor(currentToxicColor);

        // Sinh bọt khí / tàn khói acid độc bay lên từ thân thể quái
        if (Time.time >= nextBubbleTime)
        {
            nextBubbleTime = Time.time + Random.Range(0.18f, 0.32f);
            SpawnSingleBubble(transform.position + (Vector3)Random.insideUnitCircle * 0.4f);
        }
    }

    private void ApplyToxicGreenColor(Color color)
    {
        for (int i = targetRenderers.Count - 1; i >= 0; i--)
        {
            var sr = targetRenderers[i];
            if (sr == null)
            {
                targetRenderers.RemoveAt(i);
                continue;
            }
            sr.color = color;
        }
    }

    private void RestoreOriginalColors()
    {
        foreach (var kvp in originalColors)
        {
            if (kvp.Key != null)
            {
                kvp.Key.color = kvp.Value;
            }
        }
    }

    private void SpawnAcidBurst(Vector3 center)
    {
        for (int i = 0; i < 6; i++)
        {
            SpawnSingleBubble(center + (Vector3)Random.insideUnitCircle * 0.3f);
        }
    }

    private void SpawnSingleBubble(Vector3 pos)
    {
        GameObject bubble = new GameObject("AcidBubble_Particle");
        bubble.transform.position = pos;
        var sr = bubble.AddComponent<SpriteRenderer>();
        sr.sprite = GetCachedGlowSprite();
        sr.color = new Color(0.20f, 0.95f, 0.35f, 0.85f);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 5500;
        bubble.transform.localScale = Vector3.one * Random.Range(0.06f, 0.13f);

        StartCoroutine(BubbleFloatRoutine(bubble));
    }

    private IEnumerator BubbleFloatRoutine(GameObject b)
    {
        if (b == null) yield break;
        float elapsed = 0f;
        float life = Random.Range(0.35f, 0.60f);
        Vector3 startScale = b.transform.localScale;
        float riseSpeed = Random.Range(0.6f, 1.2f);
        float wobbleFreq = Random.Range(6f, 12f);
        float startX = b.transform.position.x;

        while (elapsed < life && b != null)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / life;
            float wobble = Mathf.Sin(elapsed * wobbleFreq) * 0.08f;
            b.transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
            b.transform.position = new Vector3(startX + wobble, b.transform.position.y, b.transform.position.z);
            b.transform.localScale = Vector3.Lerp(startScale, startScale * 0.2f, p);

            var sr = b.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(0.20f, 0.95f, 0.35f, (1f - p) * 0.85f);
            }
            yield return null;
        }

        if (b != null) Destroy(b);
    }

    private void RemoveDebuff()
    {
        RestoreOriginalColors();
        Destroy(this);
    }

    private void OnDisable()
    {
        RestoreOriginalColors();
    }

    private void OnDestroy()
    {
        RestoreOriginalColors();
    }

    private static Sprite GetCachedGlowSprite()
    {
        if (cachedGlowSprite != null) return cachedGlowSprite;

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
        float radius = (size - 1) / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float norm = Mathf.Clamp01(dist / radius);
                float alpha = Mathf.Pow(1f - norm, 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        cachedGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedGlowSprite;
    }
}
