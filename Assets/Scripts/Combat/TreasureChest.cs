using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class TreasureChest : MonoBehaviour, IDamageable
{
    [System.Serializable]
    public class ChestLootItem
    {
        public string id;
        public string nameVi;
        public string nameEn;
        public int quantity;
        public string rarity; // "Common", "Rare", "Epic", "Legendary", "Currency"
        public Sprite icon;
    }

    [Header("Chest Sprites")]
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    [Header("Loot Drop Configuration")]
    [SerializeField] private int potionCount = 1;
    [SerializeField] private int shardMin = 300;
    [SerializeField] private int shardMax = 650;
    [SerializeField] private float interactRadius = 2.2f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private bool isOpened = false;
    private bool isLootGenerated = false;
    private List<ChestLootItem> lootItems = new List<ChestLootItem>();

    private Vector3 baseScale = Vector3.one;
    private Coroutine bounceRoutine = null;

    // Mobile & In-Game Interaction Arrow (Giống trụ nâng cấp Ancient Shrine)
    private GameObject arrowIndicatorObj;
    private TextMeshPro arrowTextMesh;
    private Color arrowColor = new Color(1f, 0.85f, 0.35f, 1f);
    private Transform playerTransform;

    void Awake()
    {
        baseScale = transform.localScale;
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
            boxCollider.size = new Vector2(1.1f, 0.65f);
            boxCollider.offset = new Vector2(0f, 0.25f);
            boxCollider.isTrigger = false;
        }

        if (GetComponent<AutoDepthSort>() == null)
        {
            gameObject.AddComponent<AutoDepthSort>();
        }

        CreateArrowIndicator();
    }

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (isOpened)
        {
            if (arrowIndicatorObj != null && arrowIndicatorObj.activeSelf)
                arrowIndicatorObj.SetActive(false);
            return;
        }

        if (playerTransform == null) FindPlayer();
        if (playerTransform == null) return;

        float dist = Vector2.Distance(transform.position + Vector3.up * 0.35f, playerTransform.position);
        bool inRange = dist <= interactRadius;

        // Kiem tra popup loot hoac cac menu khac co dang mo khong
        bool popupOpen = ChestLootPopupUI.Instance != null && ChestLootPopupUI.Instance.IsOpen;
        bool shouldShow = inRange && !popupOpen && !isOpened;

        if (arrowIndicatorObj != null && arrowIndicatorObj.activeSelf != shouldShow)
        {
            arrowIndicatorObj.SetActive(shouldShow);
        }

        // Hieu ung mui ten vang bay bong, nhap nhay tren noc ruong giong tru nang cap
        if (arrowIndicatorObj != null && arrowIndicatorObj.activeSelf)
        {
            float bobbing = Mathf.Sin(Time.time * 4.5f) * 0.12f;
            arrowIndicatorObj.transform.localPosition = new Vector3(0f, 1.15f + bobbing, 0f);

            if (arrowTextMesh != null)
            {
                float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
                arrowTextMesh.color = Color.Lerp(new Color(1f, 0.82f, 0.3f, 0.85f), new Color(1f, 1f, 0.7f, 1f), pulse);
            }

            if (inRange && !popupOpen && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            {
                OpenChest();
            }
        }
    }

    private void FindPlayer()
    {
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;
    }

    private void CreateArrowIndicator()
    {
        if (arrowIndicatorObj != null) return;

        arrowIndicatorObj = new GameObject("Chest_ArrowIndicator");
        arrowIndicatorObj.transform.SetParent(transform, false);
        arrowIndicatorObj.transform.localPosition = new Vector3(0f, 1.15f, 0f);

        arrowTextMesh = arrowIndicatorObj.AddComponent<TextMeshPro>();
        arrowTextMesh.text = "▼";
        arrowTextMesh.fontSize = 4.2f;
        arrowTextMesh.fontStyle = FontStyles.Bold;
        arrowTextMesh.alignment = TextAlignmentOptions.Center;
        arrowTextMesh.color = arrowColor;
        arrowTextMesh.sortingLayerID = SortingLayer.NameToID("UI");
        arrowTextMesh.sortingOrder = 500;
        arrowTextMesh.rectTransform.sizeDelta = new Vector2(1.5f, 1.5f);
        arrowTextMesh.raycastTarget = false;
        arrowTextMesh.maskable = false;

        arrowIndicatorObj.SetActive(false);
    }

    public void GenerateLootIfEmpty()
    {
        if (isLootGenerated) return;
        isLootGenerated = true;
        lootItems.Clear();

        // 1. Aethel Shards
        int shards = Random.Range(shardMin, shardMax + 1);
        Sprite shardSpr = Resources.Load<Sprite>("Items/aethel_shard");
#if UNITY_EDITOR
        if (shardSpr == null) shardSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/aethel_shard.png");
#endif
        lootItems.Add(new ChestLootItem
        {
            id = "shards",
            nameVi = "Mảnh Linh Hồn",
            nameEn = "Aethel Shards",
            quantity = shards,
            rarity = "Currency",
            icon = shardSpr
        });

        // 2. Health Potions
        int pCount = Random.Range(1, 3);
        Sprite potSpr = Resources.Load<Sprite>("Items/health_potion");
#if UNITY_EDITOR
        if (potSpr == null) potSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/health_potion.png");
#endif
        lootItems.Add(new ChestLootItem
        {
            id = "potion",
            nameVi = "Bình Hồi Máu",
            nameEn = "Health Potion",
            quantity = pCount,
            rarity = "Rare",
            icon = potSpr
        });

        // (Cơ chế đá/khoáng sản/item socket đã được loại bỏ hoàn toàn theo thiết kế mới, chỉ giữ lại Shards và Potion)
    }

    public List<ChestLootItem> GetLootItems()
    {
        GenerateLootIfEmpty();
        return lootItems;
    }

    public void RemoveLootItem(ChestLootItem item)
    {
        if (lootItems.Contains(item))
        {
            lootItems.Remove(item);
        }
        if (lootItems.Count == 0)
        {
            MarkFullyOpened();
        }
    }

    public void ClearAllLoot()
    {
        lootItems.Clear();
        MarkFullyOpened();
    }

    public void EjectAndSpawnAllLoot()
    {
        GenerateLootIfEmpty();
        var items = new List<ChestLootItem>(lootItems);
        lootItems.Clear();

        Vector3 spawnOrigin = transform.position + Vector3.up * 0.45f;
        int total = items.Count;
        for (int i = 0; i < total; i++)
        {
            var item = items[i];
            float angle = (i / (float)Mathf.Max(1, total)) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            WorldItemPickup.SpawnArc(spawnOrigin, item.id, item.quantity, item.icon, item.rarity, dir, LocalizationManager.IsVietnamese ? item.nameVi : item.nameEn);
        }

        MarkFullyOpened();
        SoundManager.EnsureInstance()?.PlayUpgradeSuccess();
    }

    public void EjectSingleLoot(ChestLootItem item)
    {
        if (item == null) return;
        Vector3 spawnOrigin = transform.position + Vector3.up * 0.45f;
        Vector2 dir = Random.insideUnitCircle.normalized;
        WorldItemPickup.SpawnArc(spawnOrigin, item.id, item.quantity, item.icon, item.rarity, dir, LocalizationManager.IsVietnamese ? item.nameVi : item.nameEn);

        RemoveLootItem(item);
    }

    public void MarkFullyOpened()
    {
        isOpened = true;
        if (openSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = openSprite;
        }

        if (arrowIndicatorObj != null)
        {
            arrowIndicatorObj.SetActive(false);
            Destroy(arrowIndicatorObj);
            arrowIndicatorObj = null;
        }

        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
        }

        PlayBounceAnimation();
    }

    public void OnModalClosed()
    {
        if (lootItems.Count == 0)
        {
            MarkFullyOpened();
        }
    }

    public void OpenChest()
    {
        if (isOpened) return;

        GenerateLootIfEmpty();

        if (openSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = openSprite;
        }

        PlayBounceAnimation();

        var popup = ChestLootPopupUI.EnsureUIExists();
        if (popup != null)
        {
            popup.Open(this);
        }
    }

    // Cham hoac Click tren thiet bi Mobile / PC
    void OnMouseDown()
    {
        if (isOpened) return;
        if (playerTransform == null) FindPlayer();
        float dist = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : 99f;
        if (dist <= interactRadius + 1.2f)
        {
            OpenChest();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isOpened) return;
        OpenChest();
    }

    private void PlayBounceAnimation()
    {
        if (bounceRoutine != null)
        {
            StopCoroutine(bounceRoutine);
        }
        bounceRoutine = StartCoroutine(BounceLidRoutine());
    }

    private IEnumerator BounceLidRoutine()
    {
        transform.localScale = baseScale * 1.15f;
        yield return new WaitForSecondsRealtime(0.08f);
        transform.localScale = baseScale * 0.95f;
        yield return new WaitForSecondsRealtime(0.06f);
        transform.localScale = baseScale;
        bounceRoutine = null;
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
        col.size = new Vector2(1.1f, 0.65f);
        col.offset = new Vector2(0f, 0.25f);
        col.isTrigger = false;

        chestObj.AddComponent<AutoDepthSort>();

        var comp = chestObj.AddComponent<TreasureChest>();
        return comp;
    }
}
