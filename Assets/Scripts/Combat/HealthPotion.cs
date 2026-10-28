using UnityEngine;
using System.Collections;

public class HealthPotion : MonoBehaviour
{
    [SerializeField] private int potionCount = 1;
    [SerializeField] private float pickupRadius = 1.35f;

    private Transform player;
    private bool isCollected = false;
    private Vector3 initialPos;
    private float floatTimer;

    void Start()
    {
        initialPos = transform.position;
        var p = Object.FindAnyObjectByType<PlayerController>();
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (isCollected) return;

        if (player == null)
        {
            var p = Object.FindAnyObjectByType<PlayerController>();
            if (p != null) player = p.transform;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // Khi nguoi choi buoc vao tam nhan -> Kich hoat hieu ung bay vao UI ngay lap tuc!
        if (dist <= pickupRadius)
        {
            TriggerPickup();
        }
        else if (dist <= 3.8f)
        {
            // Tự động hút nhanh về phía người chơi (tốc độ cao để thu thập ngay lập tức, không lơ lửng)
            transform.position = Vector3.MoveTowards(transform.position, player.position, 14f * Time.deltaTime);
            if (Vector2.Distance(transform.position, player.position) <= pickupRadius)
            {
                TriggerPickup();
            }
        }
        else
        {
            // Bap benh nhe nhang tren san
            floatTimer += Time.deltaTime * 3.5f;
            transform.position = initialPos + new Vector3(0, Mathf.Sin(floatTimer) * 0.08f, 0);
        }
    }

    private void TriggerPickup()
    {
        isCollected = true;

        var health = (player != null) ? player.GetComponent<PlayerHealth>() : Object.FindAnyObjectByType<PlayerHealth>();
        if (health != null && !health.IsDead)
        {
            // Hồi phục trực tiếp 25% máu tối đa (tối thiểu 35 HP) cho mỗi ngọc/bình máu
            int healPerPotion = Mathf.Max(35, Mathf.RoundToInt(health.MaxHealth * 0.25f));
            int totalHeal = healPerPotion * Mathf.Max(1, potionCount);
            health.Heal(totalHeal);
            SoundManager.EnsureInstance()?.PlayPotionDrink();
        }

        // Tự biến mất ngay lập tức
        Destroy(gameObject);
    }

    public static HealthPotion Spawn(Vector3 pos, int count = 1)
    {
        GameObject obj = new GameObject("HealthPotion");
        obj.transform.position = pos;

        var sr = obj.AddComponent<SpriteRenderer>();
        var sprite = Resources.Load<Sprite>("Items/health_potion");
#if UNITY_EDITOR
        if (sprite == null)
        {
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/health_potion.png");
        }
#endif
        sr.sprite = sprite;
        sr.sortingOrder = 5200;

        // Chuan hoa kich thuoc can bang bang dung vien ngoc Aethel Shard (0.42m)
        if (sprite != null)
        {
            float maxDim = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            if (maxDim > 0.001f)
            {
                obj.transform.localScale = Vector3.one * (0.42f / maxDim);
            }
        }
        else
        {
            obj.transform.localScale = Vector3.one * 0.42f;
        }

        var potion = obj.AddComponent<HealthPotion>();
        potion.potionCount = count;

        potion.StartCoroutine(potion.BurstRoutine(pos));
        return potion;
    }

    private IEnumerator BurstRoutine(Vector3 start)
    {
        Vector3 target = start + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.4f, 0.8f), 0);
        float t = 0;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, t / 0.35f) + Vector3.up * Mathf.Sin((t / 0.35f) * Mathf.PI) * 0.4f;
            yield return null;
        }
        initialPos = target;
        transform.position = target;
    }
}
