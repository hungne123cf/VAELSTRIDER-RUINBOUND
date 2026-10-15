using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hieu ung Choang (Stun) ap dung cho tat ca cac loai quai vat va Boss trong game.
/// - Ngung di chuyen, tan cong va lam cham Animator trong thoi gian choang.
/// - Hien thi VFX Vòng Sao Choang (Dizzy Stars) xoay chính xác trên đỉnh đầu quái/Boss.
/// </summary>
public class EnemyStunEffect : MonoBehaviour
{
    private float stunTimer = 0f;
    private Rigidbody2D rb;
    private List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
    private GameObject dizzyVfx;
    private Transform[] starTransforms;
    private float orbitAngle = 0f;
    private static Sprite cachedStarSprite = null;

    private Animator animator;
    private float originalAnimSpeed = 1f;
    private bool hasSavedAnimSpeed = false;

    public static void ApplyStun(GameObject target, float duration = 1.0f)
    {
        if (target == null) return;

        // 1. Tuyệt đối không choáng Player
        if (target.CompareTag("Player") || target.GetComponent<PlayerHealth>() != null || target.GetComponentInParent<PlayerHealth>() != null) return;

        // 2. Không choáng các vật thể môi trường (Props, Chests, Barrels, Eggs, Rocks, Obstacles)
        if (target.GetComponent<BreakableProp>() != null || target.GetComponentInParent<BreakableProp>() != null) return;
        if (target.GetComponent<TreasureChest>() != null || target.GetComponentInParent<TreasureChest>() != null) return;
        if (target.GetComponent<BoneEggObstacle>() != null || target.GetComponentInParent<BoneEggObstacle>() != null) return;

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
        bool isEnemyOrBoss = enemyRoot.CompareTag("Enemy") || (damageable != null && !(damageable is PlayerHealth));
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

        if (!isEnemyOrBoss) return;

        var existing = enemyRoot.GetComponent<EnemyStunEffect>();
        if (existing == null)
        {
            existing = enemyRoot.AddComponent<EnemyStunEffect>();
        }
        existing.SetStunDuration(duration);
    }

    public void SetStunDuration(float duration)
    {
        stunTimer = Mathf.Max(stunTimer, duration);
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Vector3 headPos = CalculateHeadTopWorldPosition();
        SoundManager.EnsureInstance()?.PlayHitImpact();

        EnsureDizzyVFX();
        DisableAIScripts();
        PauseAnimator();
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
        Vector3 headWorldPos = CalculateHeadTopWorldPosition();
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
            // Tự xoay từng ngôi sao quanh trục của nó
            starTransforms[i].localRotation = Quaternion.Euler(0f, 0f, -orbitAngle * 2.5f);

            // Chiều sâu 3D (sao phía trước to hơn phía sau)
            float depthScale = Mathf.Lerp(starBaseScale * 0.82f, starBaseScale * 1.18f, (Mathf.Sin(rad) + 1f) * 0.5f);
            starTransforms[i].localScale = Vector3.one * depthScale;
        }
    }

    private Vector3 CalculateHeadTopWorldPosition()
    {
        // 1. Tìm tâm ngang (X) chính xác của nhân vật
        float headCenterX = transform.position.x;
        bool hasCenterX = false;

        // Ưu tiên Collider2D vì Collider luôn bám theo trọng tâm hitbox thật của quái/Boss (tránh lệch do padding của sprite sheet)
        Collider2D mainCol = GetComponent<Collider2D>();
        if (mainCol == null) mainCol = GetComponentInChildren<Collider2D>();

        if (mainCol != null && !mainCol.isTrigger)
        {
            headCenterX = mainCol.bounds.center.x;
            hasCenterX = true;
        }

        // 2. Tìm điểm cao nhất (Y) của đỉnh đầu
        float maxTopY = transform.position.y + 0.9f;

        if (mainCol != null && !mainCol.isTrigger)
        {
            maxTopY = Mathf.Max(maxTopY, mainCol.bounds.max.y);
        }

        // Kiểm tra các SpriteRenderer hiển thị thân thể
        var allSrs = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in allSrs)
        {
            if (sr == null || sr.sprite == null) continue;
            if (dizzyVfx != null && sr.transform.IsChildOf(dizzyVfx.transform)) continue;

            string nameLower = sr.gameObject.name.ToLower();
            if (nameLower.Contains("popup") || nameLower.Contains("shadow") || nameLower.Contains("text") || 
                nameLower.Contains("shield") || nameLower.Contains("circle") || nameLower.Contains("cone") || 
                nameLower.Contains("indicator") || nameLower.Contains("range")) continue;

            if (!hasCenterX)
            {
                headCenterX = sr.bounds.center.x;
                hasCenterX = true;
            }

            maxTopY = Mathf.Max(maxTopY, sr.bounds.max.y);
        }

        // 3. Đảm bảo chiều cao tối thiểu tương xứng tỉ lệ Scale nếu là Boss khổng lồ
        float scaleY = Mathf.Abs(transform.lossyScale.y);
        if (scaleY > 1.15f)
        {
            maxTopY = Mathf.Max(maxTopY, transform.position.y + (1.25f * scaleY));
        }

        // 4. Vị trí vòng sao: Lơ lửng ngay phía trên chóp đầu/sừng +0.22m
        return new Vector3(headCenterX, maxTopY + 0.22f, transform.position.z - 0.25f);
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
                nameLower.Contains("indicator") || nameLower.Contains("range")) continue;

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
            dizzyVfx.transform.position = CalculateHeadTopWorldPosition();
            dizzyVfx.transform.rotation = Quaternion.identity;
            dizzyVfx.transform.localScale = Vector3.one;

            // Tìm sortingOrder cao nhất của quái để sao luôn hiển thị nổi trên đỉnh đầu
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

                // 4-cánh sao lấp lánh (4-Point Golden Star)
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

    private void RemoveStun()
    {
        ResumeAnimator();

        foreach (var mb in disabledScripts)
        {
            if (mb != null) mb.enabled = true;
        }
        disabledScripts.Clear();

        if (dizzyVfx != null)
        {
            Destroy(dizzyVfx);
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
        if (dizzyVfx != null) Destroy(dizzyVfx);
    }
}
