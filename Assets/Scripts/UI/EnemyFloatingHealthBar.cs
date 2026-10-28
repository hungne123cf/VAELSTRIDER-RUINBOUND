using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Thanh máu nổi trên đầu quái nhỏ / quái thường (Overhead Floating Health Bar).
/// - Tự động gắn và kích hoạt khi quái nhận sát thương.
/// - Giao diện Dark Fantasy / Soulslike: Nền đá đen, thanh máu đỏ Ruby, thanh Ghost vàng cam mượt mà.
/// - Hiển thị số máu chính xác (vd: 85/110) giúp người chơi biết rõ lượng sát thương gây ra.
/// - Tự động biến mất sau 3.5s không nhận thêm sát thương hoặc khi quái bị tiêu diệt.
/// - Không áp dụng cho Boss lớn (Boss lớn đã có thanh máu hoành tráng BossHealthBarUI ở mép trên màn hình).
/// </summary>
public class EnemyFloatingHealthBar : MonoBehaviour
{
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform canvasRt;

    private Image healthFill;
    private Image ghostFill;

    private Transform targetTransform;
    private Collider2D cachedCol;
    private SpriteRenderer cachedSr;
    private float headPadding = 0.35f;
    private int maxHp = 100;
    private int currentHp = 100;

    private float visibleTimer = 0f;
    private const float VISIBLE_DURATION = 3.5f;
    private const float FADE_SPEED = 3.5f;

    private float targetFillAmount = 1f;
    private float ghostFillAmount = 1f;
    private bool isInitialized = false;

    private static Sprite solidWhiteSprite;

    public static EnemyFloatingHealthBar Attach(GameObject enemy, int currentHealth, int maxHealth, float offset = -1f)
    {
        if (enemy == null) return null;

        var bar = enemy.GetComponentInChildren<EnemyFloatingHealthBar>();
        if (bar == null)
        {
            GameObject barObj = new GameObject("FloatingHealthBar");
            barObj.transform.SetParent(enemy.transform, false);
            bar = barObj.AddComponent<EnemyFloatingHealthBar>();
            bar.Initialize(enemy, offset);
        }

        bar.UpdateHealth(currentHealth, maxHealth);
        return bar;
    }

    public static void ReportHealth(GameObject enemy, int currentHealth, int maxHealth, float offset = -1f)
    {
        if (enemy == null) return;

        // Bỏ qua nếu là Boss lớn đã có thanh máu Boss riêng biệt
        var bot = enemy.GetComponent<BotWheelAI>();
        if (bot != null && bot.IsBoss) return;

        var blood = enemy.GetComponent<BloodMonsterAI>();
        if (blood != null && blood.IsBoss) return;

        if (enemy.GetComponent<SkeletonKingBossAI>() != null) return;
        if (enemy.GetComponent<ValleyGolemBossAI>() != null) return;

        Attach(enemy, currentHealth, maxHealth, offset);
    }

    public void Initialize(GameObject enemy, float offset)
    {
        targetTransform = enemy.transform;
        cachedCol = enemy.GetComponent<Collider2D>();
        if (cachedCol == null) cachedCol = enemy.GetComponentInChildren<Collider2D>();

        cachedSr = enemy.GetComponent<SpriteRenderer>();
        if (cachedSr == null) cachedSr = enemy.GetComponentInChildren<SpriteRenderer>();

        headPadding = (offset > 0f) ? offset : 0.20f;

        BuildUI();
        isInitialized = true;
    }

    private void BuildUI()
    {
        if (solidWhiteSprite == null)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, Color.white);
            t.SetPixel(0, 1, Color.white);
            t.SetPixel(1, 0, Color.white);
            t.SetPixel(1, 1, Color.white);
            t.Apply();
            solidWhiteSprite = Sprite.Create(t, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100);
        }

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = "UI";
        canvas.sortingOrder = 6000;

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        canvasRt = GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(70f, 6.5f);
        canvasRt.localScale = Vector3.one * 0.009f;
        canvasRt.localPosition = new Vector3(0f, headPadding, 0f);

        // 1. Frame Background
        GameObject bgObj = new GameObject("BgFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        bgObj.transform.SetParent(transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.sprite = solidWhiteSprite;
        bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);

        Outline outline = bgObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.20f, 0.26f, 0.95f);
        outline.effectDistance = new Vector2(1f, -1f);

        // 2. Ghost Fill (Amber delayed drain)
        GameObject ghostObj = new GameObject("GhostFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ghostObj.transform.SetParent(bgObj.transform, false);
        RectTransform ghostRt = ghostObj.GetComponent<RectTransform>();
        ghostRt.anchorMin = Vector2.zero;
        ghostRt.anchorMax = Vector2.one;
        ghostRt.offsetMin = new Vector2(0.8f, 0.8f);
        ghostRt.offsetMax = new Vector2(-0.8f, -0.8f);

        ghostFill = ghostObj.GetComponent<Image>();
        ghostFill.sprite = solidWhiteSprite;
        ghostFill.type = Image.Type.Filled;
        ghostFill.fillMethod = Image.FillMethod.Horizontal;
        ghostFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        ghostFill.color = new Color(1f, 0.82f, 0.28f, 0.85f);
        ghostFill.fillAmount = 1f;

        // 3. Health Fill (Ruby Red)
        GameObject healthObj = new GameObject("HealthFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        healthObj.transform.SetParent(bgObj.transform, false);
        RectTransform healthRt = healthObj.GetComponent<RectTransform>();
        healthRt.anchorMin = Vector2.zero;
        healthRt.anchorMax = Vector2.one;
        healthRt.offsetMin = new Vector2(0.8f, 0.8f);
        healthRt.offsetMax = new Vector2(-0.8f, -0.8f);

        healthFill = healthObj.GetComponent<Image>();
        healthFill.sprite = solidWhiteSprite;
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthFill.color = new Color(0.92f, 0.16f, 0.20f, 1f);
        healthFill.fillAmount = 1f;
    }

    public void UpdateHealth(int current, int max)
    {
        currentHp = Mathf.Max(0, current);
        maxHp = Mathf.Max(1, max);

        targetFillAmount = Mathf.Clamp01((float)currentHp / maxHp);

        if (healthFill != null)
        {
            healthFill.fillAmount = targetFillAmount;
        }

        // Tinh tế hóa hiển thị Giáp: Đổi viền sang ánh thép bạc nếu quái có giáp phòng ngự
        var armor = targetTransform != null ? (targetTransform.GetComponent<EnemyArmor>() ?? targetTransform.GetComponentInParent<EnemyArmor>()) : null;
        var outline = GetComponentInChildren<Outline>();
        if (outline != null)
        {
            if (armor != null && armor.CurrentPhysicalArmor > 0)
            {
                outline.effectColor = new Color(0.60f, 0.65f, 0.78f, 0.95f); // Ánh thép bạc tượng trưng cho giáp
            }
            else
            {
                outline.effectColor = new Color(0.18f, 0.20f, 0.26f, 0.95f);
            }
        }

        // Hiện thanh máu và reset bộ đếm thời gian
        visibleTimer = VISIBLE_DURATION;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        // Nếu quái chết, thu gọn thanh máu và xóa dần
        if (currentHp <= 0)
        {
            visibleTimer = 0.4f;
        }
    }

    void LateUpdate()
    {
        if (targetTransform == null || !targetTransform.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        // Tự động tìm lại collider hoặc sprite renderer nếu chưa có
        if (cachedCol == null)
        {
            cachedCol = targetTransform.GetComponent<Collider2D>();
            if (cachedCol == null) cachedCol = targetTransform.GetComponentInChildren<Collider2D>();
        }
        if (cachedSr == null)
        {
            cachedSr = targetTransform.GetComponent<SpriteRenderer>();
            if (cachedSr == null) cachedSr = targetTransform.GetComponentInChildren<SpriteRenderer>();
        }

        // Căn chuẩn tuyệt đối theo trọng tâm bề ngang (bounds.center.x) và đỉnh đầu (bounds.max.y)
        // Bất kể quái có offset sprite, collider lệch trục hay đang xoay hướng trái/phải
        Vector3 targetPos;

        var spitter = targetTransform.GetComponent<ValleySpitterAI>();
        if (spitter != null)
        {
            // Cục đá ValleySpitter: Canh chuẩn xác theo bounds hình ảnh thực tế của SpriteRenderer
            float centerX = (cachedSr != null && cachedSr.enabled) ? cachedSr.bounds.center.x : (targetTransform.position.x + 1.5f);
            float topY = (cachedSr != null && cachedSr.enabled) ? cachedSr.bounds.max.y : (targetTransform.position.y + 2.8f);
            targetPos = new Vector3(centerX, topY + headPadding, 0f);
        }
        else if (targetTransform.GetComponent<SlimeAI>() != null)
        {
            // Quái Nhớt Map 3: Thân dẹp, chiều cao collider ~0.8m
            float centerX = (cachedCol != null && cachedCol.enabled) ? cachedCol.bounds.center.x : targetTransform.position.x;
            float topY = (cachedCol != null && cachedCol.enabled) ? cachedCol.bounds.max.y : (targetTransform.position.y + 0.8f);
            targetPos = new Vector3(centerX, topY + 0.16f, 0f);
        }
        else if (targetTransform.GetComponent<GoblinAI>() != null)
        {
            // Yêu tinh Goblin Map 3: Thân nhỏ nhanh nhẹn, chiều cao collider ~1.3m
            float centerX = (cachedCol != null && cachedCol.enabled) ? cachedCol.bounds.center.x : targetTransform.position.x;
            float topY = (cachedCol != null && cachedCol.enabled) ? cachedCol.bounds.max.y : (targetTransform.position.y + 1.3f);
            targetPos = new Vector3(centerX, topY + 0.18f, 0f);
        }
        else if (targetTransform.GetComponent<SkeletonAI>() != null)
        {
            // Bộ xương Skeleton Map 3: Dáng cao, chiều cao collider ~2.0m
            float centerX = (cachedCol != null && cachedCol.enabled) ? cachedCol.bounds.center.x : targetTransform.position.x;
            float topY = (cachedCol != null && cachedCol.enabled) ? cachedCol.bounds.max.y : (targetTransform.position.y + 2.0f);
            targetPos = new Vector3(centerX, topY + 0.20f, 0f);
        }
        else if (targetTransform.GetComponent<BoneEggCocoon>() != null)
        {
            // Kén trứng phong ấn Map 3: Bán kính collider 2.0m
            float centerX = (cachedCol != null && cachedCol.enabled) ? cachedCol.bounds.center.x : targetTransform.position.x;
            float topY = (cachedCol != null && cachedCol.enabled) ? cachedCol.bounds.max.y : (targetTransform.position.y + 2.0f);
            targetPos = new Vector3(centerX, topY + 0.20f, 0f);
        }
        else
        {
            float colTop = (cachedCol != null && cachedCol.enabled) ? cachedCol.bounds.max.y : float.MinValue;
            float srTop = (cachedSr != null && cachedSr.enabled) ? cachedSr.bounds.max.y : float.MinValue;

            float topY;
            if (colTop != float.MinValue && srTop != float.MinValue)
            {
                // Nếu SpriteRenderer có viền trong suốt lớn bất thường (> 0.6m so với Collider)
                // Ưu tiên Collider vì Collider được gắn chuẩn theo thân thể thực tế của quái
                if (srTop > colTop + 0.6f)
                {
                    topY = colTop;
                }
                else
                {
                    topY = Mathf.Max(colTop, srTop);
                }
            }
            else if (colTop != float.MinValue)
            {
                topY = colTop;
            }
            else if (srTop != float.MinValue)
            {
                topY = srTop;
            }
            else
            {
                topY = float.MinValue;
            }

            if (topY == float.MinValue)
            {
                var bot = targetTransform.GetComponent<BotWheelAI>();
                if (bot != null)
                {
                    Vector3 center = bot.GetBossVisualCenter();
                    targetPos = new Vector3(center.x, center.y + 0.65f + headPadding, 0f);
                }
                else
                {
                    targetPos = targetTransform.position + Vector3.up * (1.1f + headPadding);
                }
            }
            else
            {
                float centerX = (cachedCol != null && cachedCol.enabled) ? cachedCol.bounds.center.x :
                                ((cachedSr != null && cachedSr.enabled) ? cachedSr.bounds.center.x : targetTransform.position.x);
                targetPos = new Vector3(centerX, topY + headPadding, 0f);
            }
        }

        transform.position = targetPos;
        transform.rotation = Quaternion.identity;

        // Triệt tiêu hoàn toàn scale và lật mặt từ quái cha, giữ thanh máu luôn thẳng, đúng kích thước 0.009f
        Vector3 pScale = targetTransform.lossyScale;
        float absX = Mathf.Abs(pScale.x) > 0.0001f ? Mathf.Abs(pScale.x) : 1f;
        float absY = Mathf.Abs(pScale.y) > 0.0001f ? Mathf.Abs(pScale.y) : 1f;
        float signX = pScale.x < 0f ? -1f : 1f;
        float signY = pScale.y < 0f ? -1f : 1f;
        transform.localScale = new Vector3(signX * (0.009f / absX), signY * (0.009f / absY), 1f);

        // Thanh Ghost vàng cam trôi từ từ theo thanh máu đỏ
        if (ghostFill != null)
        {
            ghostFillAmount = Mathf.MoveTowards(ghostFillAmount, targetFillAmount, Time.deltaTime * 0.85f);
            ghostFill.fillAmount = ghostFillAmount;
        }

        // Đếm ngược tự động ẩn thanh máu sau 3.5 giây không nhận thêm đòn đánh
        if (visibleTimer > 0f)
        {
            visibleTimer -= Time.deltaTime;
        }
        else if (canvasGroup != null && canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime * FADE_SPEED);
            if (currentHp <= 0 && canvasGroup.alpha <= 0.01f)
            {
                Destroy(gameObject);
            }
        }
    }
}
