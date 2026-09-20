using UnityEngine;
using System.Collections;

/// <summary>
/// Tao hieu ung manh vo gom (Ceramic Shards) khi chem be binh va la rung (Leaves & Petals) khi chem hoa co.
/// Tu dong sinh hat vat ly bay ra 4 phia kem xoay nhe va mo dan, khong can prefab ngoai.
/// </summary>
public class BreakableDebrisFX : MonoBehaviour
{
    private static Sprite ceramicShardSprite;
    private static Sprite leafShardSprite;

    public static void SpawnPotShatter(Vector3 origin)
    {
        EnsureSprites();
        int count = Random.Range(5, 8);
        Color[] shardColors = new Color[]
        {
            new Color(0.72f, 0.45f, 0.28f, 1f), // Terracotta clay
            new Color(0.58f, 0.32f, 0.18f, 1f), // Dark pottery
            new Color(0.85f, 0.60f, 0.38f, 1f), // Highlight clay
            new Color(0.40f, 0.22f, 0.12f, 1f)  // Shadow clay
        };

        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * 360f + Random.Range(-25f, 25f);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
            float speed = Random.Range(2.2f, 4.2f);
            Color col = shardColors[Random.Range(0, shardColors.Length)];
            float size = Random.Range(0.18f, 0.32f);

            CreateDebrisPiece(origin, dir * speed, col, size, 0.5f, ceramicShardSprite, isLeaf: false);
        }
    }

    public static void SpawnFoliageCut(Vector3 origin)
    {
        EnsureSprites();
        int count = Random.Range(6, 10);
        Color[] leafColors = new Color[]
        {
            new Color(0.15f, 0.72f, 0.25f, 1f), // Fresh green
            new Color(0.28f, 0.85f, 0.38f, 1f), // Light green
            new Color(0.08f, 0.48f, 0.18f, 1f), // Forest green
            new Color(1.0f, 0.92f, 0.30f, 1f)   // Yellow petal
        };

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
            float speed = Random.Range(1.2f, 2.8f);
            Color col = leafColors[Random.Range(0, leafColors.Length)];
            float size = Random.Range(0.14f, 0.26f);

            CreateDebrisPiece(origin, dir * speed, col, size, 0.65f, leafShardSprite, isLeaf: true);
        }
    }

    private static void CreateDebrisPiece(Vector3 pos, Vector2 velocity, Color color, float scale, float lifetime, Sprite sprite, bool isLeaf)
    {
        GameObject shard = new GameObject(isLeaf ? "Leaf_Debris" : "Pot_Shard");
        shard.transform.position = pos + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0);
        shard.transform.localScale = Vector3.one * scale;

        var sr = shard.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 5300;

        var fx = shard.AddComponent<BreakableDebrisPiece>();
        fx.Init(velocity, lifetime, isLeaf);
    }

    private static void EnsureSprites()
    {
        if (!ceramicShardSprite)
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixels = new Color[]
            {
                Color.clear, Color.white, Color.white, Color.clear,
                Color.white, Color.white, Color.white, Color.white,
                Color.white, Color.white, Color.white, Color.clear,
                Color.clear, Color.white, Color.clear, Color.clear
            };
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            ceramicShardSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16);
        }

        if (!leafShardSprite)
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixels = new Color[]
            {
                Color.clear, Color.white, Color.clear, Color.clear,
                Color.white, Color.white, Color.white, Color.clear,
                Color.white, Color.white, Color.white, Color.white,
                Color.clear, Color.white, Color.clear, Color.clear
            };
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            leafShardSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16);
        }
    }
}

public class BreakableDebrisPiece : MonoBehaviour
{
    private Vector2 velocity;
    private float lifetime;
    private float rotSpeed;
    private bool isLeaf;
    private SpriteRenderer sr;

    public void Init(Vector2 initialVelocity, float duration, bool leaf)
    {
        velocity = initialVelocity;
        lifetime = duration;
        isLeaf = leaf;
        rotSpeed = Random.Range(-360f, 360f);
        sr = GetComponent<SpriteRenderer>();
        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        float elapsed = 0f;
        Color baseCol = (sr != null) ? sr.color : Color.white;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            transform.position += (Vector3)(velocity * Time.deltaTime);
            transform.Rotate(0, 0, rotSpeed * Time.deltaTime);

            if (isLeaf)
            {
                velocity.y -= 1.8f * Time.deltaTime;
                velocity.x *= 0.94f;
            }
            else
            {
                velocity *= 0.90f;
            }

            if (sr != null)
            {
                Color c = baseCol;
                c.a = Mathf.Lerp(1f, 0f, t);
                sr.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}

