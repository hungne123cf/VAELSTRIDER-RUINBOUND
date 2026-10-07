using UnityEngine;
using System.Collections;

/// <summary>
/// Hien thi hieu ung Lop Giap Ao Ho Than (Protective Energy Shield VFX) bao boc tron ven nguoi choi.
/// Tu dong tinh toan toa do tam co the (body center) va chieu cao nhan vat de khiem khong bao gio bi lech xuong chan.
/// </summary>
public class PlayerShieldVisual : MonoBehaviour
{
    private SpriteRenderer shieldRenderer;
    private static Sprite proceduralShieldSprite;

    private float baseScale = 2.4f;
    private Color shieldColor = new Color(1f, 0.86f, 0.28f, 0.68f); // Vang kim quang phat sang
    private Color hitColor = new Color(1f, 1f, 1f, 0.95f);
    private bool isVisible = false;

    void Awake()
    {
        shieldRenderer = gameObject.AddComponent<SpriteRenderer>();
        shieldRenderer.sprite = GetOrCreateShieldSprite();
        shieldRenderer.color = shieldColor;
        shieldRenderer.sortingOrder = 5005; // Luon hien thi phia tren Sprite nhan vat (Player la 5000)
        shieldRenderer.enabled = false;

        UpdatePositionAndScale();
    }

    void Start()
    {
        UpdatePositionAndScale();
    }

    public void UpdatePositionAndScale()
    {
        string curChar = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        float centerY = 0.1f;
        float height = 1.0f;

        if (curChar == CharacterManager.CHAR_TITAN_BERSERKER || GetComponentInParent<TitanBerserkerSkill>() != null)
        {
            centerY = 0.65f;
            height = 1.45f;
        }
        else
        {
            var col = GetComponentInParent<Collider2D>();
            var sr = GetComponentInParent<SpriteRenderer>();

            if (col != null)
            {
                centerY = col.offset.y;
                if (col is CapsuleCollider2D cap)
                {
                    height = Mathf.Max(cap.size.y, cap.size.x);
                }
                else if (col is BoxCollider2D box)
                {
                    height = Mathf.Max(box.size.y, box.size.x);
                }
            }
            else if (sr != null && sr.sprite != null)
            {
                centerY = sr.bounds.center.y - (transform.parent != null ? transform.parent.position.y : 0f);
                height = sr.bounds.size.y;
            }
        }

        // Can giua dung tam nguc nhan vat
        transform.localPosition = new Vector3(0f, centerY, 0f);

        // Kich thuoc du lon de bao boc tu dinh dau den qua chan
        baseScale = Mathf.Max(2.2f, height * 1.55f);
        transform.localScale = Vector3.one * baseScale;
    }

    void Update()
    {
        if (!isVisible || shieldRenderer == null || !shieldRenderer.enabled) return;

        // Xoay nhe lop giap tao cam giac dong nang luong luu chuyen
        transform.Rotate(Vector3.forward * (50f * Time.deltaTime));

        // Nhip tho xung nhan (Pulsing breathe)
        float pulse = 1.0f + Mathf.Sin(Time.time * 3.5f) * 0.04f;
        transform.localScale = Vector3.one * (baseScale * pulse);
    }

    public void ShowShield(bool show)
    {
        isVisible = show;
        if (show)
        {
            UpdatePositionAndScale();
        }

        if (shieldRenderer != null)
        {
            shieldRenderer.enabled = show;
            shieldRenderer.color = shieldColor;
        }

        if (show)
        {
            transform.localScale = Vector3.one * (baseScale * 1.25f);
            StopAllCoroutines();
            StartCoroutine(ScaleToNormalRoutine());
        }
    }

    public void OnHit()
    {
        if (!isVisible || shieldRenderer == null) return;
        StopAllCoroutines();
        StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        shieldRenderer.color = hitColor;
        transform.localScale = Vector3.one * (baseScale * 1.15f);
        yield return new WaitForSeconds(0.08f);
        if (isVisible && shieldRenderer != null)
        {
            shieldRenderer.color = shieldColor;
            transform.localScale = Vector3.one * baseScale;
        }
    }

    private IEnumerator ScaleToNormalRoutine()
    {
        float t = 0f;
        Vector3 startScale = Vector3.one * (baseScale * 1.25f);
        Vector3 endScale = Vector3.one * baseScale;

        while (t < 1f)
        {
            t += Time.deltaTime * 6f;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
    }

    public void PlayBreakVFX()
    {
        // Tao 8 manh vo nang luong bung no vang vang ra xung quanh
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            CreateShard(transform.position, dir);
        }
    }

    private void CreateShard(Vector3 origin, Vector3 dir)
    {
        GameObject shardObj = new GameObject("Shield_Shard");
        shardObj.transform.position = origin + dir * 0.4f;
        var sr = shardObj.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreateShieldSprite();
        sr.color = new Color(1f, 0.75f, 0.2f, 0.85f);
        sr.sortingOrder = 5006;
        shardObj.transform.localScale = Vector3.one * 0.35f;

        var anim = shardObj.AddComponent<ShardAnimator>();
        anim.Init(dir);
    }

    private static Sprite GetOrCreateShieldSprite()
    {
        if (proceduralShieldSprite != null) return proceduralShieldSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size * 0.46f;
        float ringWidth = size * 0.14f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > radius + 2f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float outerAlpha = Mathf.Clamp01(1f - Mathf.Abs(dist - radius) / ringWidth);
                    float innerGlow = Mathf.Clamp01(1f - dist / radius) * 0.32f;
                    float finalAlpha = Mathf.Clamp01(outerAlpha * 0.85f + innerGlow);

                    Color c = new Color(1f, 0.90f, 0.45f, finalAlpha);
                    tex.SetPixel(x, y, c);
                }
            }
        }
        tex.Apply();

        proceduralShieldSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return proceduralShieldSprite;
    }

    private class ShardAnimator : MonoBehaviour
    {
        private Vector3 moveDir;
        private float speed = 4f;
        private float life = 0.45f;
        private SpriteRenderer sr;

        public void Init(Vector3 dir)
        {
            moveDir = dir;
            sr = GetComponent<SpriteRenderer>();
            Destroy(gameObject, life);
        }

        void Update()
        {
            transform.position += moveDir * (speed * Time.deltaTime);
            speed = Mathf.Max(0.5f, speed - Time.deltaTime * 6f);
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Max(0f, c.a - Time.deltaTime * 2.5f);
                sr.color = c;
            }
        }
    }
}
