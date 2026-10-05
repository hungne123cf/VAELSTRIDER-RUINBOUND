using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class BoneEggObstacle : MonoBehaviour, IDamageable
{
    private SpriteRenderer sr;
    private CircleCollider2D col;
    private int health = 110;
    private bool isDetonated = false;

    private Sprite[] idleSprites;
    private Sprite[] breakSprites;

    public static void Spawn(Vector3 pos)
    {
        GameObject go = new GameObject("BoneEgg_Obelisk");
        go.transform.position = pos;
        go.AddComponent<BoneEggObstacle>();
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();

        col.radius = 0.55f;
        col.offset = new Vector2(0f, 0.2f);

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        gameObject.layer = (enemyLayer != -1) ? enemyLayer : 6;
        gameObject.tag = "Enemy";

        sr.sortingOrder = 1400;

        idleSprites = new Sprite[4];
        breakSprites = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
#if UNITY_EDITOR
            idleSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ART/Bosses/Skeleton King/SkeletonKing_Obstacle_{i}.png");
            breakSprites[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ART/Bosses/Skeleton King/SkeletonKing_Ostacle_Death_{i}.png");
#endif
        }

        if (idleSprites[0] != null) sr.sprite = idleSprites[0];
        transform.localScale = Vector3.one * 1.1f;
    }

    void Start()
    {
        StartCoroutine(ObeliskLifeRoutine());
    }

    private IEnumerator ObeliskLifeRoutine()
    {
        float timer = 2.0f;
        int frame = 0;
        while (timer > 0f && !isDetonated)
        {
            timer -= 0.15f;
            if (idleSprites != null && idleSprites[frame] != null) sr.sprite = idleSprites[frame];
            frame = (frame + 1) % 4;

            if (timer < 0.8f)
            {
                sr.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.time * 8f, 1f));
            }
            yield return new WaitForSeconds(0.15f);
        }

        if (!isDetonated)
        {
            StartCoroutine(ExplodeRoutine());
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDetonated) return;
        health -= damage;
        StartCoroutine(DamageFlash());
        if (health <= 0)
        {
            StartCoroutine(ExplodeRoutine());
        }
    }

    private IEnumerator DamageFlash()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        sr.color = Color.white;
    }

    private IEnumerator ExplodeRoutine()
    {
        isDetonated = true;
        if (col != null) col.enabled = false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.8f);
        foreach (var h in hits)
        {
            if (h.CompareTag("Player"))
            {
                var target = h.GetComponent<IDamageable>();
                if (target != null) target.TakeDamage(22);
            }
        }

        DamagePopup.CreateText(transform.position + Vector3.up * 1f, "EGG HATCH!", new Color(0.4f, 1f, 0.6f));

        // Roi 1-2 linh hon nho
        AethelShard.SpawnShards(transform.position, Random.Range(1, 3));

        // No ra 1 - 2 Nhen Con Mini!
        Vector3 hatchPos = transform.position;
        MiniSpiderMinion.Spawn(hatchPos + Vector3.left * 0.4f);
        if (Random.value > 0.4f)
        {
            MiniSpiderMinion.Spawn(hatchPos + Vector3.right * 0.4f);
        }

        // Hoat anh vo
        for (int i = 0; i < 4; i++)
        {
            if (breakSprites != null && breakSprites[i] != null) sr.sprite = breakSprites[i];
            yield return new WaitForSeconds(0.08f);
        }

        Destroy(gameObject);
    }
}
