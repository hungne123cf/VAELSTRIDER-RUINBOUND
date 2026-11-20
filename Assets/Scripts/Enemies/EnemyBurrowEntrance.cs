using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Enemy Subterranean Burrow & Emerge Entrance FX
/// Cơ chế quái vật chui từ dưới lòng đất nhô lên khi người chơi bước vào khu vực / phòng quái:
/// 1. Khi vừa xuất hiện, quái vật ở trạng thái chui dưới đất (Subterranean Burrowing).
/// 2. Trong lúc quái đang nhô lên:
///    - Quái vật hoàn toàn BẤT TỬ / Miễn nhiễm sát thương (Collider tắt, IDamageable bị khóa).
///    - Người chơi không thể đánh quái trong thời gian này.
///    - Quái vật cũng chưa thể tấn công người chơi.
/// 3. Hiệu ứng đồ họa:
///    - Đất nứt, mô đất và bụi đất (Dirt Particles) phun trào tại vị trí chân quái.
///    - Mô hình quái vật từ từ nhô lên từ lòng đất (-1.2m -> 0m) trong khoảng 0.85s - 1.1s.
/// 4. Khi quái nhô lên hoàn toàn:
///    - Bụi đất nổ tung, âm thanh gầm rú / va chạm mặt đất.
///    - Kích hoạt lại Collider, mở khóa trạng thái nhận sát thương (Damageable).
///    - Người chơi BẮT ĐẦU ĐÁNH ĐƯỢC QUÁI!
/// </summary>
public class EnemyBurrowEntrance : MonoBehaviour
{
    private static readonly HashSet<GameObject> _emergingEnemies = new HashSet<GameObject>();

    private Collider2D[] _cachedColliders;
    private Rigidbody2D _cachedRb;
    private SpriteRenderer[] _cachedRenderers;
    private Vector3[] _originalLocalPositions;

    private bool _isEmerging = false;
    private Coroutine _emergeCoroutine;

    private static Sprite _proceduralMoundSprite;
    private static Sprite _proceduralDirtSprite;

    /// <summary>
    /// Kiểm tra xem quái vật có đang trong trạng thái chui từ đất lên hay không
    /// </summary>
    public static bool IsEnemyEmerging(GameObject enemy)
    {
        if (enemy == null) return false;
        return _emergingEnemies.Contains(enemy);
    }

    /// <summary>
    /// Gắn cơ chế chui từ đất lên cho quái vật
    /// </summary>
    public static EnemyBurrowEntrance Attach(GameObject enemy, float emergeDuration = 0.95f)
    {
        if (enemy == null) return null;

        // Tránh gắn trùng lặp
        var existing = enemy.GetComponent<EnemyBurrowEntrance>();
        if (existing != null)
        {
            if (existing._isEmerging) return existing;
            Destroy(existing);
        }

        var burrow = enemy.AddComponent<EnemyBurrowEntrance>();
        burrow.StartEmergence(emergeDuration);
        return burrow;
    }

    private void StartEmergence(float emergeDuration)
    {
        _emergeCoroutine = StartCoroutine(EmergeRoutine(emergeDuration));
    }

    private IEnumerator EmergeRoutine(float duration)
    {
        _isEmerging = true;
        _emergingEnemies.Add(gameObject);

        // 1. Tắt toàn bộ Collider của quái vật để người chơi KHÔNG THỂ đánh trúng khi đang chui lên
        _cachedColliders = GetComponentsInChildren<Collider2D>();
        var colliderStates = new bool[_cachedColliders.Length];
        for (int i = 0; i < _cachedColliders.Length; i++)
        {
            if (_cachedColliders[i] != null)
            {
                colliderStates[i] = _cachedColliders[i].enabled;
                _cachedColliders[i].enabled = false;
            }
        }

        _cachedRb = GetComponent<Rigidbody2D>();
        if (_cachedRb != null)
        {
            _cachedRb.linearVelocity = Vector2.zero;
        }

        _cachedRenderers = GetComponentsInChildren<SpriteRenderer>();
        _originalLocalPositions = new Vector3[_cachedRenderers.Length];
        for (int i = 0; i < _cachedRenderers.Length; i++)
        {
            if (_cachedRenderers[i] != null)
            {
                _originalLocalPositions[i] = _cachedRenderers[i].transform.localPosition;
            }
        }

        Vector3 groundBasePos = transform.position;

        // 2. Tạo Mô Đất Nứt (Ground Crack / Soil Mound) tại chân quái
        GameObject moundObj = new GameObject("Burrow_Mound", typeof(SpriteRenderer));
        moundObj.transform.position = groundBasePos + new Vector3(0f, -0.3f, 0f);
        var moundSr = moundObj.GetComponent<SpriteRenderer>();
        moundSr.sprite = GetProceduralMoundSprite();
        moundSr.color = new Color(0.35f, 0.28f, 0.20f, 0.95f);
        moundSr.sortingOrder = (_cachedRenderers.Length > 0 && _cachedRenderers[0] != null) ? _cachedRenderers[0].sortingOrder - 1 : 0;
        moundObj.transform.localScale = new Vector3(0.1f, 0.05f, 1f);

        // Âm thanh đất nứt / rung chuyển
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayDash();
        }

        float subterraneanDepth = 1.35f;
        float elapsed = 0f;

        // Vị trí chìm dưới đất ban đầu
        for (int i = 0; i < _cachedRenderers.Length; i++)
        {
            if (_cachedRenderers[i] != null)
            {
                _cachedRenderers[i].transform.localPosition = _originalLocalPositions[i] + new Vector3(0f, -subterraneanDepth, 0f);
            }
        }

        // Bắn đất đá văng ban đầu
        SpawnDirtDebris(groundBasePos, 6);

        // 3. Tiến trình nhô lên từ từ từ lòng đất
        while (elapsed < duration)
        {
            if (gameObject == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Easing mượt mà: nhô lên nhanh ở giữa, nảy nhẹ khi lên hẳn
            float riseProgress = Mathf.SmoothStep(0f, 1f, t);
            float currentOffsetY = Mathf.Lerp(-subterraneanDepth, 0f, riseProgress);

            for (int i = 0; i < _cachedRenderers.Length; i++)
            {
                if (_cachedRenderers[i] != null)
                {
                    _cachedRenderers[i].transform.localPosition = _originalLocalPositions[i] + new Vector3(0f, currentOffsetY, 0f);
                }
            }

            // Mô đất nở to
            float moundScale = Mathf.Lerp(0.1f, 1.1f, riseProgress);
            if (moundObj != null)
            {
                moundObj.transform.localScale = new Vector3(moundScale, moundScale * 0.45f, 1f);
            }

            // Phun bụi đất rải rác trong lúc chui lên
            if (Random.value < 0.25f)
            {
                SpawnSingleDirtParticle(groundBasePos);
            }

            if (_cachedRb != null) _cachedRb.linearVelocity = Vector2.zero;

            yield return null;
        }

        // 4. QUÁI ĐÃ NHÔ LÊN HOÀN TOÀN (EMERGENCE COMPLETE)
        for (int i = 0; i < _cachedRenderers.Length; i++)
        {
            if (_cachedRenderers[i] != null)
            {
                _cachedRenderers[i].transform.localPosition = _originalLocalPositions[i];
            }
        }

        // Đất nổ tung lần cuối khi quái thoát khỏi lòng đất
        SpawnDirtDebris(groundBasePos, 10);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySwordSlash();
        }

        // Làm mờ dần mô đất
        if (moundObj != null)
        {
            StartCoroutine(FadeOutAndDestroy(moundObj, moundSr, 0.6f));
        }

        // 5. MỞ KHÓA COLLIDER: NGƯỜI CHƠI BẮT ĐẦU ĐÁNH ĐƯỢC QUÁI!
        for (int i = 0; i < _cachedColliders.Length; i++)
        {
            if (_cachedColliders[i] != null)
            {
                _cachedColliders[i].enabled = colliderStates[i];
            }
        }

        _emergingEnemies.Remove(gameObject);
        _isEmerging = false;
        _emergeCoroutine = null;

        // Tự hủy component sau khi hoàn thành để giải phóng tài nguyên
        Destroy(this);
    }

    private void OnDisable()
    {
        CleanupEmergence();
    }

    private void OnDestroy()
    {
        CleanupEmergence();
    }

    private void CleanupEmergence()
    {
        _emergingEnemies.Remove(gameObject);
        if (_cachedColliders != null)
        {
            for (int i = 0; i < _cachedColliders.Length; i++)
            {
                if (_cachedColliders[i] != null)
                {
                    _cachedColliders[i].enabled = true;
                }
            }
        }
        if (_cachedRenderers != null && _originalLocalPositions != null)
        {
            for (int i = 0; i < _cachedRenderers.Length; i++)
            {
                if (_cachedRenderers[i] != null && i < _originalLocalPositions.Length)
                {
                    _cachedRenderers[i].transform.localPosition = _originalLocalPositions[i];
                }
            }
        }
    }

    private void SpawnDirtDebris(Vector3 center, int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnSingleDirtParticle(center);
        }
    }

    private void SpawnSingleDirtParticle(Vector3 center)
    {
        GameObject p = new GameObject("Burrow_DirtParticle", typeof(SpriteRenderer));
        Vector2 offset = new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.35f, -0.15f));
        p.transform.position = center + (Vector3)offset;

        var sr = p.GetComponent<SpriteRenderer>();
        sr.sprite = GetProceduralDirtSprite();
        sr.color = new Color(Random.Range(0.30f, 0.45f), Random.Range(0.22f, 0.32f), Random.Range(0.15f, 0.22f), 0.95f);
        sr.sortingOrder = 6;
        p.transform.localScale = Vector3.one * Random.Range(0.18f, 0.35f);

        Vector2 vel = new Vector2(Random.Range(-1.6f, 1.6f), Random.Range(1.8f, 3.8f));
        StartCoroutine(DirtPhysicsRoutine(p, sr, vel));
    }

    private IEnumerator DirtPhysicsRoutine(GameObject p, SpriteRenderer sr, Vector2 velocity)
    {
        float dur = Random.Range(0.35f, 0.55f);
        float elapsed = 0f;
        float gravity = 9.8f;

        while (elapsed < dur)
        {
            if (p == null) yield break;
            elapsed += Time.deltaTime;
            velocity.y -= gravity * Time.deltaTime;
            p.transform.position += (Vector3)(velocity * Time.deltaTime);

            float a = Mathf.Lerp(0.95f, 0f, elapsed / dur);
            if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, a);

            yield return null;
        }
        if (p != null) Destroy(p);
    }

    private IEnumerator FadeOutAndDestroy(GameObject obj, SpriteRenderer sr, float duration)
    {
        float elapsed = 0f;
        Color c = (sr != null) ? sr.color : Color.white;
        while (elapsed < duration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(c.a, 0f, elapsed / duration);
            if (sr != null) sr.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    // ─── Procedural Dirt Sprites ────────────────────────────────────────────────
    private static Sprite GetProceduralMoundSprite()
    {
        if (_proceduralMoundSprite != null) return _proceduralMoundSprite;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float rX = size * 0.48f;
        float rY = size * 0.24f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center.x) / rX;
                float dy = (y - center.y) / rY;
                float d = dx * dx + dy * dy;
                if (d <= 1f)
                {
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(0.38f, 0.28f, 0.18f, a * 0.95f));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        _proceduralMoundSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _proceduralMoundSprite;
    }

    private static Sprite GetProceduralDirtSprite()
    {
        if (_proceduralDirtSprite != null) return _proceduralDirtSprite;
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(size * 0.5f, size * 0.5f));
                if (d <= size * 0.45f)
                {
                    tex.SetPixel(x, y, Color.white);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        _proceduralDirtSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _proceduralDirtSprite;
    }
}
