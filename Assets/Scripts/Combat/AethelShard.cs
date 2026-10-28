using UnityEngine;

public class AethelShard : MonoBehaviour
{
    [Header("Loot Settings")]
    [SerializeField] private int shardValue = 1;
    [SerializeField] private float magnetRadius = 4.5f;
    [SerializeField] private float pickupRadius = 0.45f;

    private Transform player;
    private PlayerInventory playerInventory;
    private Vector3 scatterVelocity;
    private float scatterTimer = 0.35f;
    private float currentFlySpeed = 2f;
    private bool isAttracted = false;

    private static Sprite _cachedSprite;
    private static Sprite GetShardSprite()
    {
        if (_cachedSprite != null) return _cachedSprite;

        // 1. Uu tien load tu Resources
        _cachedSprite = Resources.Load<Sprite>("Items/aethel_shard");
        if (_cachedSprite != null) return _cachedSprite;

#if UNITY_EDITOR
        // 2. Load tu AssetDatabase trong Editor
        _cachedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/aethel_shard.png");
        if (_cachedSprite != null) return _cachedSprite;
#endif

        // 3. Fallback tao sprite tinh the kim cuong xanh lam phat quang bang code
        _cachedSprite = CreateDiamondSprite();
        return _cachedSprite;
    }

    private static Sprite CreateDiamondSprite()
    {
        const int SIZE = 16;
        var tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        float half = SIZE * 0.5f;
        for (int x = 0; x < SIZE; x++)
        {
            for (int y = 0; y < SIZE; y++)
            {
                float dx = Mathf.Abs(x + 0.5f - half) / half;
                float dy = Mathf.Abs(y + 0.5f - half) / half;
                float d  = dx + dy;

                Color c;
                if      (d < 0.28f) c = new Color(0.92f, 1.00f, 1.00f, 1f);   // Loi sang trang kim
                else if (d < 0.58f) c = new Color(0.20f, 0.85f, 1.00f, 1f);   // Xanh ngoc cyan ruc ro
                else if (d < 0.82f) c = new Color(0.06f, 0.50f, 0.92f, 1f);   // Xanh lam sang
                else if (d < 1.00f) c = new Color(0.02f, 0.22f, 0.60f, 0.85f); // Vien tinh the
                else                c = Color.clear;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex,
            new Rect(0, 0, SIZE, SIZE),
            new Vector2(0.5f, 0.5f),
            16f);
    }

    public static int ActiveShardCount { get; private set; } = 0;
    private static Transform cachedPlayer;
    private static PlayerInventory cachedInventory;

    public static void EnsurePlayerCached()
    {
        if (cachedPlayer == null)
        {
            var pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                cachedPlayer = pc.transform;
                cachedInventory = pc.GetComponent<PlayerInventory>();
            }
        }
    }

    public static void SpawnShards(Vector3 spawnPos, int count)
    {
        if (count <= 0) return;
        EnsurePlayerCached();
        Sprite sprite = GetShardSprite();

        // Tối ưu hóa: Nếu trên màn hình đã có quá nhiều mảnh rơi (> 30), gom cụm lại thành 1-2 viên giá trị cao
        int spawnCount = count;
        int valuePerShard = 1;

        if (ActiveShardCount > 30)
        {
            spawnCount = 1;
            valuePerShard = count;
        }
        else if (count > 4)
        {
            spawnCount = 2;
            valuePerShard = Mathf.CeilToInt(count / 2f);
        }

        for (int i = 0; i < spawnCount; i++)
        {
            var go = new GameObject("AethelShard");
            go.transform.position = spawnPos + new Vector3(
                Random.Range(-0.25f, 0.25f),
                Random.Range(-0.25f, 0.25f), 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 50;
            sr.color = Color.white;

            if (sprite != null)
            {
                float maxDim = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maxDim > 0.001f)
                {
                    float scaleFactor = (valuePerShard > 1) ? 0.52f : 0.42f;
                    go.transform.localScale = Vector3.one * (scaleFactor / maxDim);
                }
            }
            else
            {
                go.transform.localScale = Vector3.one * 0.42f;
            }

            go.AddComponent<AutoDepthSort>();

            var shard = go.AddComponent<AethelShard>();
            shard.shardValue = valuePerShard;
            var dir2d = Random.insideUnitCircle.normalized;
            shard.scatterVelocity = new Vector3(dir2d.x, dir2d.y, 0) * Random.Range(3f, 5.5f);

            ActiveShardCount++;
        }
    }

    void Start()
    {
        if (cachedPlayer == null) EnsurePlayerCached();
        player = cachedPlayer;
        playerInventory = cachedInventory;
    }

    void OnDestroy()
    {
        ActiveShardCount = Mathf.Max(0, ActiveShardCount - 1);
    }

    void Update()
    {
        if (scatterTimer > 0)
        {
            transform.position += scatterVelocity * Time.deltaTime;
            scatterVelocity = Vector3.Lerp(scatterVelocity, Vector3.zero, 8f * Time.deltaTime);
            scatterTimer -= Time.deltaTime;
            return;
        }

        transform.position += new Vector3(0, Mathf.Sin(Time.time * 6f) * 0.0015f, 0);
        if (player == null)
        {
            EnsurePlayerCached();
            player = cachedPlayer;
            playerInventory = cachedInventory;
            if (player == null) return;
        }

        float distance = Vector2.Distance(transform.position, player.position + Vector3.up * 0.6f);
        if (distance <= magnetRadius) isAttracted = true;

        if (isAttracted)
        {
            currentFlySpeed += 14f * Time.deltaTime;
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position + Vector3.up * 0.6f,
                currentFlySpeed * Time.deltaTime);

            if (distance <= 0.45f)
            {
                EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out _, out _, out _, out _, out float shardBonusPct, out _);
                int finalShards = shardValue;
                if (shardBonusPct > 0f)
                {
                    finalShards = Mathf.Max(1, Mathf.RoundToInt(shardValue * (1f + shardBonusPct)));
                }

                playerInventory?.AddShards(finalShards);
                SoundManager.EnsureInstance()?.PlayShardPickup();
                Destroy(gameObject);
            }
        }
    }
}