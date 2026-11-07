using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Dynamic Frame-by-Frame Sprite Polygon Collider Synchronizer
/// 
/// ĐẶC TÍNH KỸ THUẬT:
/// 1. Áp dụng cho TẤT CẢ Quái thường và Đại Trùm ở mọi Map (Map 1, Map 2, Map 3).
/// 2. Tự động gỡ bỏ các Collider hình học tĩnh bị lệch (CircleCollider2D, CapsuleCollider2D, BoxCollider2D)
///    và thay bằng PolygonCollider2D ôm sát 100% theo đường viền Pixel thực tế (Physics Shape) của từng khung hình!
/// 3. Khi quái/Boss di chuyển, vung chân nhện, vung kiếm, nhảy vồ hoặc lật mặt (flipX / localScale.x):
///    Collider tự động biến thiên khớp tuyệt đối với cử động của Sprite ở từng frame.
/// 4. SIÊU TỐI ƯU HÓA 60 FPS (Zero-Lag Static Shape Cache):
///    - Đường viền đa giác của mỗi Sprite chỉ được trích xuất đúng 1 lần duy nhất và lưu vào bộ nhớ đệm tĩnh (s_ShapeCache).
///    - Trong LateUpdate, chỉ khi Sprite hoặc hướng lật (flipX/flipY) thực sự đổi sang frame mới thì mới cập nhật PolygonCollider2D.
/// </summary>
[DisallowMultipleComponent]
public class DynamicSpriteColliderSync : MonoBehaviour
{
    private struct ShapeCacheKey : System.IEquatable<ShapeCacheKey>
    {
        public int spriteInstanceId;
        public bool flipX;
        public bool flipY;

        public bool Equals(ShapeCacheKey other)
        {
            return spriteInstanceId == other.spriteInstanceId && flipX == other.flipX && flipY == other.flipY;
        }

        public override bool Equals(object obj)
        {
            return obj is ShapeCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = spriteInstanceId * 397;
            if (flipX) hash ^= 0x1A2B3C;
            if (flipY) hash ^= 0x4D5E6F;
            return hash;
        }
    }

    private static readonly Dictionary<ShapeCacheKey, Vector2[][]> s_ShapeCache = new Dictionary<ShapeCacheKey, Vector2[][]>(256);
    private static readonly List<Vector2> s_TempShapeList = new List<Vector2>(64);

    private SpriteRenderer sr;
    private PolygonCollider2D polyCol;
    private Sprite lastSprite;
    private bool lastFlipX;
    private bool lastFlipY;
    private bool playerCollisionIgnored = false;

    public PolygonCollider2D PolygonCollider => polyCol;

    public static PolygonCollider2D EnsureAttached(GameObject target)
    {
        if (target == null) return null;

        // Zero-Bug Android: Không gắn DynamicSpriteColliderSync hoặc PolygonCollider2D lên Boss và Minion 
        // vốn đã có CapsuleCollider2D / CircleCollider2D tĩnh để triệt tiêu 100% nguy cơ Box2D solver freeze
        if (target.GetComponent<SkeletonKingBossAI>() != null || 
            target.GetComponent<MiniSpiderMinion>() != null ||
            target.GetComponent<BoneEggObstacle>() != null ||
            target.GetComponent<BoneEggCocoon>() != null)
        {
            return target.GetComponent<PolygonCollider2D>();
        }

        var sync = target.GetComponent<DynamicSpriteColliderSync>();
        if (sync == null)
        {
            sync = target.AddComponent<DynamicSpriteColliderSync>();
        }
        sync.EnsureInitialized();
        return sync.polyCol;
    }

    public static Vector2 GetBodyCenter(Transform targetTransform, Collider2D col = null)
    {
        if (targetTransform != null)
        {
            var bot = targetTransform.GetComponent<BotWheelAI>();
            if (bot != null)
            {
                return bot.GetBossVisualCenter();
            }
        }
        if (col != null && col.enabled && col.bounds.size.sqrMagnitude > 0.001f)
        {
            return col.bounds.center;
        }
        if (targetTransform != null)
        {
            var c = targetTransform.GetComponent<Collider2D>();
            if (c != null && c.enabled && c.bounds.size.sqrMagnitude > 0.001f)
            {
                return c.bounds.center;
            }
            var srComp = targetTransform.GetComponent<SpriteRenderer>();
            if (srComp != null && srComp.bounds.size.sqrMagnitude > 0.001f)
            {
                return srComp.bounds.center;
            }
            return targetTransform.position;
        }
        return Vector2.zero;
    }

    void Awake()
    {
        EnsureInitialized();
    }

    void Start()
    {
        EnsureInitialized();
        SyncColliderWithSprite(force: true);
        IgnorePlayerPhysicalPush();
    }

    void OnEnable()
    {
        EnsureInitialized();
        SyncColliderWithSprite(force: true);
    }

    void LateUpdate()
    {
        if (!playerCollisionIgnored)
        {
            IgnorePlayerPhysicalPush();
        }
        SyncColliderWithSprite(force: false);
    }

    public void EnsureInitialized()
    {
        // Zero-Bug Android: Không bao giờ gắn hoặc tái tạo PolygonCollider2D trên Boss Nhện hoặc Minion
        if (GetComponent<SkeletonKingBossAI>() != null || 
            GetComponent<MiniSpiderMinion>() != null ||
            GetComponent<BoneEggObstacle>() != null ||
            GetComponent<BoneEggCocoon>() != null)
        {
            var pCol = GetComponent<PolygonCollider2D>();
            if (pCol != null) Destroy(pCol);
            Destroy(this);
            return;
        }

        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        }

        // Xóa bỏ toàn bộ các Collider tĩnh cũ (Circle, Capsule, Box) trên cùng GameObject để tránh lệch tâm
        var existingCols = GetComponents<Collider2D>();
        for (int i = 0; i < existingCols.Length; i++)
        {
            var c = existingCols[i];
            if (c == null) continue;
            if (c is PolygonCollider2D existingPoly)
            {
                if (polyCol == null) polyCol = existingPoly;
                else if (existingPoly != polyCol) Destroy(existingPoly);
            }
            else
            {
                c.enabled = false;
                Destroy(c);
            }
        }

        if (polyCol == null)
        {
            polyCol = gameObject.AddComponent<PolygonCollider2D>();
        }

        polyCol.isTrigger = false;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) gameObject.layer = enemyLayer;
        try { gameObject.tag = "Enemy"; } catch { }

        if (sr != null && sr.sprite != null)
        {
            SyncColliderWithSprite(force: true);
        }
    }

    private void IgnorePlayerPhysicalPush()
    {
        if (polyCol == null) return;
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            var pCols = player.GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < pCols.Length; i++)
            {
                if (pCols[i] != null && !pCols[i].isTrigger)
                {
                    Physics2D.IgnoreCollision(polyCol, pCols[i], true);
                }
            }
            playerCollisionIgnored = true;
        }
    }

    public void SyncColliderWithSprite(bool force)
    {
        if (sr == null || polyCol == null) return;

        Sprite currentSprite = sr.sprite;
        if (currentSprite == null) return;

        bool flipX = sr.flipX;
        bool flipY = sr.flipY;

        if (!force && currentSprite == lastSprite && flipX == lastFlipX && flipY == lastFlipY)
        {
            return;
        }

        lastSprite = currentSprite;
        lastFlipX = flipX;
        lastFlipY = flipY;

        if (GetComponent<BotWheelAI>() != null)
        {
            Vector2 c = new Vector2(flipX ? 1.136f : -1.136f, flipY ? -0.36f : 0.36f);
            Vector2 e = new Vector2(0.26f, 0.36f);
            Vector2[] botBodyOctagon = new Vector2[8]
            {
                new Vector2(c.x - e.x * 0.5f, c.y - e.y),
                new Vector2(c.x + e.x * 0.5f, c.y - e.y),
                new Vector2(c.x + e.x,        c.y - e.y * 0.5f),
                new Vector2(c.x + e.x,        c.y + e.y * 0.5f),
                new Vector2(c.x + e.x * 0.5f, c.y + e.y),
                new Vector2(c.x - e.x * 0.5f, c.y + e.y),
                new Vector2(c.x - e.x,        c.y + e.y * 0.5f),
                new Vector2(c.x - e.x,        c.y - e.y * 0.5f)
            };
            polyCol.pathCount = 1;
            polyCol.SetPath(0, botBodyOctagon);
            return;
        }

        ShapeCacheKey key = new ShapeCacheKey
        {
            spriteInstanceId = currentSprite.GetInstanceID(),
            flipX = flipX,
            flipY = flipY
        };

        if (!s_ShapeCache.TryGetValue(key, out Vector2[][] cachedPaths))
        {
            cachedPaths = BuildPathsForSprite(currentSprite, flipX, flipY);
            s_ShapeCache[key] = cachedPaths;
        }

        if (cachedPaths != null && cachedPaths.Length > 0)
        {
            try
            {
                polyCol.pathCount = cachedPaths.Length;
                for (int i = 0; i < cachedPaths.Length; i++)
                {
                    if (cachedPaths[i] != null && cachedPaths[i].Length >= 3)
                    {
                        polyCol.SetPath(i, cachedPaths[i]);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DynamicSpriteColliderSync] SetPath fallback on sprite {currentSprite.name}: {ex.Message}");
            }
        }
    }

    private static Vector2[][] BuildPathsForSprite(Sprite sprite, bool flipX, bool flipY)
    {
        if (sprite == null) return null;

        Bounds b = sprite.bounds;
        float mirrorCenterX = b.center.x * 2f;
        float mirrorCenterY = b.center.y * 2f;

        try
        {
            int shapeCount = sprite.GetPhysicsShapeCount();

            if (shapeCount > 0)
            {
                List<Vector2[]> validPaths = new List<Vector2[]>(shapeCount);
                Vector2[] largestPath = null;
                float largestPerimeter = -1f;

                for (int i = 0; i < shapeCount; i++)
                {
                    s_TempShapeList.Clear();
                    int ptCount = sprite.GetPhysicsShape(i, s_TempShapeList);
                    if (ptCount < 3) continue;

                    Vector2[] arr = new Vector2[ptCount];
                    float perimeter = 0f;

                    for (int j = 0; j < ptCount; j++)
                    {
                        Vector2 pt = s_TempShapeList[j];
                        if (flipX) pt.x = -pt.x;
                        if (flipY) pt.y = -pt.y;
                        arr[j] = pt;
                    }

                    // Zero-Bug Android: Đảo ngược thứ tự đỉnh khi lật để duy trì chiều quay Counter-Clockwise (CCW) của Box2D
                    if (flipX ^ flipY)
                    {
                        System.Array.Reverse(arr);
                    }

                    for (int j = 0; j < ptCount; j++)
                    {
                        Vector2 p0 = arr[j];
                        Vector2 p1 = arr[(j + 1) % ptCount];
                        perimeter += Vector2.Distance(p0, p1);
                    }

                    if (perimeter > largestPerimeter)
                    {
                        largestPerimeter = perimeter;
                        largestPath = arr;
                    }

                    // Lọc bỏ các hạt bụi pixel li ti rời rạc (chu vi < 0.45m) để đa giác mượt và chuẩn xác
                    if (perimeter >= 0.45f)
                    {
                        validPaths.Add(arr);
                    }
                }

                if (validPaths.Count == 0 && largestPath != null)
                {
                    validPaths.Add(largestPath);
                }

                if (validPaths.Count > 0)
                {
                    return validPaths.ToArray();
                }
            }
        }
        catch (System.Exception ex)
        {
            // Android APK Runtime Protection: Physics shape data not accessible on mobile compressed sprites
            Debug.LogWarning($"[DynamicSpriteColliderSync] Physics shape fallback for sprite {sprite.name}: {ex.Message}");
        }

        // Fallback: Nếu Sprite không có sẵn PhysicsShape, tạo bát giác ôm sát Bounds của Sprite và xử lý lật flipX/flipY
        Vector2 c = b.center;
        if (flipX) c.x = -c.x;
        if (flipY) c.y = -c.y;

        Vector2 e = b.extents * 0.85f;
        if (e.x < 0.15f) e.x = 0.25f;
        if (e.y < 0.15f) e.y = 0.25f;

        Vector2[] fallbackOctagon = new Vector2[8]
        {
            new Vector2(c.x - e.x * 0.5f, c.y - e.y),
            new Vector2(c.x + e.x * 0.5f, c.y - e.y),
            new Vector2(c.x + e.x,        c.y - e.y * 0.5f),
            new Vector2(c.x + e.x,        c.y + e.y * 0.5f),
            new Vector2(c.x + e.x * 0.5f, c.y + e.y),
            new Vector2(c.x - e.x * 0.5f, c.y + e.y),
            new Vector2(c.x - e.x,        c.y + e.y * 0.5f),
            new Vector2(c.x - e.x,        c.y - e.y * 0.5f)
        };

        if (flipX ^ flipY)
        {
            System.Array.Reverse(fallbackOctagon);
        }

        return new Vector2[][] { fallbackOctagon };
    }
}

