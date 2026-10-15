using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hiệu ứng ĐÓNG BĂNG (Freeze Effect) áp dụng cho Quái vật & Boss khi trúng đòn Địa Chấn Băng của Titan Berserker
/// - Quái bị hóa xanh băng giá toàn thân (Icy Blue Tint)
/// - Bị bất động / ngưng chuyển động hoàn toàn trong suốt thời gian đóng băng
/// - Có hiệu ứng tinh thể băng và ánh sáng hàn khí phát ra từ thân quái
/// </summary>
public class EnemyFreezeEffect : MonoBehaviour
{
    private float freezeTimer = 0f;
    private float totalFreezeDuration = 1.2f;
    private Rigidbody2D rb;
    private Animator animator;
    private float originalAnimSpeed = 1f;
    private bool hasSavedAnimSpeed = false;
    private List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();

    private List<SpriteRenderer> enemySrs = new List<SpriteRenderer>();
    private Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();

    private GameObject frostAura;
    private Color freezeTint = new Color(0.38f, 0.82f, 1f, 1f); // Màu xanh dương băng tuyết lạnh giá

    public static void ApplyFreeze(GameObject target, float duration = 1.2f)
    {
        if (target == null) return;

        // 1. Tuyệt đối không đóng băng Player
        if (target.CompareTag("Player") || target.GetComponent<PlayerHealth>() != null || target.GetComponentInParent<PlayerHealth>() != null) return;

        // 2. Không đóng băng vật thể môi trường (Props, Chests, Eggs, Obstacles)
        if (target.GetComponent<BreakableProp>() != null || target.GetComponentInParent<BreakableProp>() != null) return;
        if (target.GetComponent<TreasureChest>() != null || target.GetComponentInParent<TreasureChest>() != null) return;
        if (target.GetComponent<BoneEggObstacle>() != null || target.GetComponentInParent<BoneEggObstacle>() != null) return;

        // 3. Tìm đúng GameObject gốc của Quái / Boss
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

        var existing = enemyRoot.GetComponent<EnemyFreezeEffect>();
        if (existing == null)
        {
            existing = enemyRoot.AddComponent<EnemyFreezeEffect>();
        }
        existing.SetFreezeDuration(duration);
    }

    public void SetFreezeDuration(float duration)
    {
        freezeTimer = Mathf.Max(freezeTimer, duration);
        totalFreezeDuration = Mathf.Max(totalFreezeDuration, duration);

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Thu thập tất cả SpriteRenderer của quái
        CacheSpriteRenderers();

        // Áp dụng màu xanh băng ngay lập tức
        ApplyFreezeColor(freezeTint);

        EnsureFrostAura();
        DisableAIScripts();
        PauseAnimator();
    }

    private void CacheSpriteRenderers()
    {
        var allSrs = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in allSrs)
        {
            if (sr == null) continue;
            if (frostAura != null && sr.transform.IsChildOf(frostAura.transform)) continue;

            string nameLower = sr.gameObject.name.ToLower();
            if (nameLower.Contains("popup") || nameLower.Contains("shadow") || nameLower.Contains("text") || 
                nameLower.Contains("shield") || nameLower.Contains("indicator")) continue;

            if (!enemySrs.Contains(sr))
            {
                enemySrs.Add(sr);
                if (!originalColors.ContainsKey(sr))
                {
                    originalColors[sr] = sr.color;
                }
            }
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        CacheSpriteRenderers();
    }

    void Update()
    {
        if (freezeTimer > 0f)
        {
            freezeTimer -= Time.deltaTime;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            if (freezeTimer <= 0f)
            {
                StartCoroutine(UnfreezeRoutine());
            }
        }
    }

    void LateUpdate()
    {
        // Duy trì màu xanh băng trong suốt thời gian bị đóng băng (chống bị script sát thương / nhấp nháy đỏ ghi đè)
        if (freezeTimer > 0f)
        {
            ApplyFreezeColor(freezeTint);
        }
    }

    private void ApplyFreezeColor(Color color)
    {
        for (int i = enemySrs.Count - 1; i >= 0; i--)
        {
            var sr = enemySrs[i];
            if (sr == null)
            {
                enemySrs.RemoveAt(i);
                continue;
            }
            sr.color = color;
        }
    }

    private void EnsureFrostAura()
    {
        if (frostAura == null)
        {
            frostAura = new GameObject("Freeze_FrostAura");
            frostAura.transform.SetParent(transform, false);
            frostAura.transform.localPosition = new Vector3(0f, 0.25f, 0f);

            var light = frostAura.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            light.color = new Color(0.4f, 0.9f, 1f, 1f);
            light.intensity = 1.6f;
            light.pointLightInnerRadius = 0.3f;
            light.pointLightOuterRadius = 1.4f;
        }
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
            animator.speed = 0f; // Bị đông cứng hoàn toàn
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

    private IEnumerator UnfreezeRoutine()
    {
        float fadeDuration = 0.25f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            foreach (var kvp in originalColors)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.color = Color.Lerp(freezeTint, kvp.Value, t);
                }
            }

            yield return null;
        }

        RemoveFreeze();
    }

    private void RemoveFreeze()
    {
        ResumeAnimator();

        foreach (var kvp in originalColors)
        {
            if (kvp.Key != null)
            {
                kvp.Key.color = kvp.Value;
            }
        }

        foreach (var mb in disabledScripts)
        {
            if (mb != null) mb.enabled = true;
        }
        disabledScripts.Clear();

        if (frostAura != null)
        {
            Destroy(frostAura);
        }

        Destroy(this);
    }

    void OnDestroy()
    {
        ResumeAnimator();
        foreach (var mb in disabledScripts)
        {
            if (mb != null) mb.enabled = true;
        }
        if (frostAura != null) Destroy(frostAura);
    }
}
