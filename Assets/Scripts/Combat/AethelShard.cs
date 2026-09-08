using UnityEngine;

public class AethelShard : MonoBehaviour
{
    [Header("Loot Settings")]
    [SerializeField] private int shardValue = 1;
    [SerializeField] private float magnetRadius = 4.5f;
    [SerializeField] private float pickupRadius = 0.45f;
    [SerializeField] private float initialFlySpeed = 2f;

    private Transform player;
    private PlayerInventory playerInventory;
    private Vector3 scatterVelocity;
    private float scatterTimer = 0.35f;
    private float currentFlySpeed = 2f;
    private bool isAttracted = false;

    public static void SpawnShards(Vector3 spawnPos, int count)
    {
        Sprite shardSprite = null;
#if UNITY_EDITOR
        shardSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/aethel_shard.png");
#endif

        for (int i = 0; i < count; i++)
        {
            GameObject shardObj = new GameObject("AethelShard");
            shardObj.transform.position = spawnPos + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0);

            SpriteRenderer sr = shardObj.AddComponent<SpriteRenderer>();
            if (shardSprite != null) sr.sprite = shardSprite;
            sr.sortingLayerName = "Entities";
            sr.sortingOrder = 50;

            // Them hieu ung chieu sau
            var depth = shardObj.AddComponent<AutoDepthSort>();

            AethelShard shard = shardObj.AddComponent<AethelShard>();

            // Luc vang ngau nhien khi quai no
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            shard.scatterVelocity = new Vector3(randomDir.x, randomDir.y, 0) * Random.Range(3f, 5.5f);
        }
    }

    void Start()
    {
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            player = pc.transform;
            playerInventory = pc.GetComponent<PlayerInventory>();
        }
    }

    void Update()
    {
        // 1. Giai doan vang ra xung quanh
        if (scatterTimer > 0)
        {
            transform.position += scatterVelocity * Time.deltaTime;
            scatterVelocity = Vector3.Lerp(scatterVelocity, Vector3.zero, 8f * Time.deltaTime);
            scatterTimer -= Time.deltaTime;
            return;
        }

        // Bap benh nhe tren mat dat
        transform.position += new Vector3(0, Mathf.Sin(Time.time * 6f) * 0.0015f, 0);

        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position + Vector3.up * 0.6f);

        // 2. Giai doan nam cham hut ve phia Player
        if (distance <= magnetRadius)
        {
            isAttracted = true;
        }

        if (isAttracted)
        {
            currentFlySpeed += 14f * Time.deltaTime;
            Vector3 targetPos = player.position + Vector3.up * 0.6f;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, currentFlySpeed * Time.deltaTime);

            // 3. Nhat thanh cong
            if (distance <= pickupRadius)
            {
                if (playerInventory != null)
                {
                    playerInventory.AddShards(shardValue);
                SoundManager.EnsureInstance()?.PlayShardPickup();
                }
                Destroy(gameObject);
            }
        }
    }
}
