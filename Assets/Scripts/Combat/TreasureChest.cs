using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class TreasureChest : MonoBehaviour, IDamageable
{
    [Header("Chest Sprites")]
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    [Header("Loot Drop")]
    [SerializeField] private int potionCount = 1;
    [SerializeField] private int shardMin = 6;
    [SerializeField] private int shardMax = 10;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private bool isOpened = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (closedSprite == null)
        {
            closedSprite = Resources.Load<Sprite>("Items/chest_closed");
#if UNITY_EDITOR
            if (closedSprite == null)
                closedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/chest_closed.png");
#endif
        }
        if (openSprite == null)
        {
            openSprite = Resources.Load<Sprite>("Items/chest_open");
#if UNITY_EDITOR
            if (openSprite == null)
                openSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/chest_open.png");
#endif
        }
        if (closedSprite != null && spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = closedSprite;
        }

        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(0.85f, 0.7f);
            boxCollider.offset = new Vector2(0f, 0.35f);
            boxCollider.isTrigger = true; // Cho phep cham hoac chem
        }
    }

    public void TakeDamage(int damage)
    {
        if (isOpened) return;
        OpenChest();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpened) return;
        // Nguoi choi cham vao hoac chem trung
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponent<PlayerCombat>() != null)
        {
            OpenChest();
        }
    }

    [ContextMenu("Open Chest Now")]
    public void OpenChest()
    {
        if (isOpened) return;
        isOpened = true;
        SoundManager.EnsureInstance()?.PlayChestOpen();
        Debug.Log("<color=yellow>[CHEST OPENED]</color> Ban da mo Ruong Kho Bau!");

        if (openSprite != null)
        {
            spriteRenderer.sprite = openSprite;
        }

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.1f, 0.2f);

        StartCoroutine(BounceLidRoutine());

        Vector3 dropPos = transform.position + Vector3.up * 0.4f;

        // 1. Roi binh mau ngau nhien tu 0 den 2 binh
        int droppedPotions = Random.Range(0, 3); // 0, 1 hoac 2 binh
        for (int i = 0; i < droppedPotions; i++)
        {
            HealthPotion.Spawn(dropPos, 35);
        }

        // 2. Roi tinh the Shards
        int shards = Random.Range(shardMin, shardMax + 1);
        AethelShard.SpawnShards(dropPos, shards);

        DamagePopup.CreateText(transform.position, "CHEST OPENED!", new Color(1f, 0.85f, 0.2f));
    }

    private IEnumerator BounceLidRoutine()
    {
        Vector3 orig = transform.localScale;
        transform.localScale = orig * 1.25f;
        yield return new WaitForSeconds(0.08f);
        transform.localScale = orig * 0.95f;
        yield return new WaitForSeconds(0.06f);
        transform.localScale = orig;
    }

    public static TreasureChest SpawnChest(Vector3 pos)
    {
        GameObject chestObj = new GameObject("TreasureChest");
        chestObj.transform.position = pos;

        var sr = chestObj.AddComponent<SpriteRenderer>();
        var sprite = Resources.Load<Sprite>("Items/chest_closed");
#if UNITY_EDITOR
        if (sprite == null) sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/chest_closed.png");
#endif
        sr.sprite = sprite;
        sr.sortingOrder = 5000;

        var col = chestObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.85f, 0.7f);
        col.offset = new Vector2(0f, 0.35f);
        col.isTrigger = true;

        var comp = chestObj.AddComponent<TreasureChest>();
        return comp;
    }
}
