using UnityEngine;
using System.Collections;

public class SkeletonKingObstacle : MonoBehaviour
{
    [SerializeField] private int contactDamage = 15;
    [SerializeField] private float lifetime = 7.0f;

    private Collider2D col;
    private SpriteRenderer sr;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        if (GetComponent<AutoDepthSort>() == null)
        {
            gameObject.AddComponent<AutoDepthSort>();
        }

        if (col == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.2f, 1.2f);
            box.isTrigger = true;
            col = box;
        }
    }

    void Start()
    {
        StartCoroutine(RiseAndFadeRoutine());
    }

    private IEnumerator RiseAndFadeRoutine()
    {
        // Hiệu ứng nhô lên từ đất
        Vector3 finalScale = transform.localScale;
        transform.localScale = new Vector3(finalScale.x, 0f, finalScale.z);

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            transform.localScale = new Vector3(finalScale.x, Mathf.Lerp(0f, finalScale.y, t / 0.25f), finalScale.z);
            yield return null;
        }
        transform.localScale = finalScale;

        // Tồn tại trong thời gian lifetime
        yield return new WaitForSeconds(lifetime - 0.5f);

        // Chìm dần xuống đất biến mất
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            transform.localScale = new Vector3(finalScale.x, Mathf.Lerp(finalScale.y, 0f, t / 0.5f), finalScale.z);
            yield return null;
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(contactDamage);
            }
        }
    }

    public static SkeletonKingObstacle Spawn(Vector3 position, Sprite spikeSprite)
    {
        GameObject obj = new GameObject("SkeletonKing_Spike");
        obj.transform.position = position;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = spikeSprite;
        sr.sortingOrder = 3000;

        var spike = obj.AddComponent<SkeletonKingObstacle>();
        return spike;
    }
}
