using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quản lý vật phẩm rơi thực tế trong thế giới game (Physical Item Drops).
/// Có hiệu ứng quỹ đạo bay hình parabol (Arc), trọng lực (Gravity), nảy đất (Bounce),
/// bóng đổ (Shadow), quầng sáng theo phẩm chất (Rarity Glow), và tự động hút về phía người chơi (Magnet Vacuum).
/// </summary>
public class WorldItemPickup : MonoBehaviour
{
    [Header("Item Info")]
    public string itemId;
    public int quantity = 1;
    public string rarityName = "Common";
    public string customName = "";

    [Header("Pickup Settings")]
    [SerializeField] private float magnetRadius = 4.2f;
    [SerializeField] private float pickupRadius = 0.65f;

    private Transform player;
    private bool isCollected = false;

    // Physics Arc Simulation
    private Vector3 groundPos;
    private float currentHeight = 0f;
    private Vector3 horizontalVelocity;
    private float heightVelocity = 0f;
    private float gravity = -22f;
    private int bouncesLeft = 2;
    private bool isAirborne = true;
    private float airborneTimer = 0f;
    private float minAirborneDuration = 0.35f;
    private float rotationSpeed;

    private Transform visualTransform;
    private Transform shadowTransform;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer glowRenderer;
    private SpriteRenderer shadowRenderer;
    private Color baseGlowColor;

    private float currentFlySpeed = 2.5f;

    private static Sprite _cachedShadowSprite;
    public static Sprite GetShadowSprite()
    {
        if (_cachedShadowSprite != null) return _cachedShadowSprite;

        const int W = 32;
        const int H = 16;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float rx = (W - 2) * 0.5f;
        float ry = (H - 2) * 0.5f;
        Vector2 center = new Vector2(W * 0.5f, H * 0.5f);

        for (int x = 0; x < W; x++)
        {
            for (int y = 0; y < H; y++)
            {
                float dx = (x - center.x) / rx;
                float dy = (y - center.y) / ry;
                float distSq = dx * dx + dy * dy;
                if (distSq <= 1f)
                {
                    float alpha = Mathf.SmoothStep(0.55f, 0f, distSq);
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        _cachedShadowSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 24f);
        return _cachedShadowSprite;
    }

    void Start()
    {
        if (player == null)
        {
            var p = Object.FindAnyObjectByType<PlayerController>();
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (isCollected) return;

        if (player == null)
        {
            var p = Object.FindAnyObjectByType<PlayerController>();
            if (p != null) player = p.transform;
        }

        // 1. Giai đoạn bay hình vòng cung, chịu trọng lực và nảy đất
        if (isAirborne)
        {
            airborneTimer += Time.deltaTime;
            groundPos += horizontalVelocity * Time.deltaTime;
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, Time.deltaTime * 3.2f);

            heightVelocity += gravity * Time.deltaTime;
            currentHeight += heightVelocity * Time.deltaTime;

            if (visualTransform != null)
            {
                visualTransform.localRotation = Quaternion.identity;
            }

            if (currentHeight <= 0f)
            {
                currentHeight = 0f;
                if (bouncesLeft > 0 && Mathf.Abs(heightVelocity) > 1.5f)
                {
                    bouncesLeft--;
                    heightVelocity = -heightVelocity * 0.42f;
                    horizontalVelocity *= 0.55f;
                }
                else
                {
                    isAirborne = false;
                    currentHeight = 0f;
                    horizontalVelocity = Vector3.zero;
                }
            }

            transform.position = groundPos;
            if (visualTransform != null)
            {
                visualTransform.localPosition = new Vector3(0f, currentHeight, 0f);
            }

            if (shadowTransform != null && shadowRenderer != null)
            {
                float t = Mathf.Clamp01(currentHeight / 3f);
                shadowTransform.localScale = Vector3.Lerp(new Vector3(1.1f, 0.55f, 1f), new Vector3(0.5f, 0.25f, 1f), t);
                shadowRenderer.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.5f, 0.15f, t));
            }
            return;
        }

        // 2. Khi đã tiếp đất an toàn:
        if (visualTransform != null)
        {
            visualTransform.localRotation = Quaternion.Lerp(visualTransform.localRotation, Quaternion.identity, Time.deltaTime * 10f);
            visualTransform.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 4.5f) * 0.08f, 0f);
        }

        if (glowRenderer != null)
        {
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
            glowRenderer.color = baseGlowColor * Mathf.Lerp(0.35f, 0.7f, pulse);
        }

        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position + Vector3.up * 0.4f);

        // 3. Tự động hút về phía người chơi khi trong tầm nam châm
        if (dist <= magnetRadius && airborneTimer >= minAirborneDuration)
        {
            currentFlySpeed = Mathf.Min(currentFlySpeed + Time.deltaTime * 24f, 18f);
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position + Vector3.up * 0.4f,
                currentFlySpeed * Time.deltaTime);

            if (dist <= pickupRadius)
            {
                TriggerPickup();
            }
        }
    }

    private void TriggerPickup()
    {
        if (isCollected) return;
        isCollected = true;

        Vector3 spawnPopPos = player != null ? player.position + Vector3.up * 1.1f : transform.position + Vector3.up * 0.5f;

        if (itemId == "shards")
        {
            EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out _, out _, out _, out _, out float shardBonusPct, out _);
            int finalShards = quantity;
            if (shardBonusPct > 0f)
            {
                finalShards = Mathf.Max(1, Mathf.RoundToInt(quantity * (1f + shardBonusPct)));
            }

            if (player != null)
            {
                var inv = player.GetComponent<PlayerInventory>();
                if (inv != null) inv.AddShards(finalShards);
                else AccountDatabase.AddAethelShards(finalShards);
            }
            else
            {
                AccountDatabase.AddAethelShards(finalShards);
            }

            SoundManager.EnsureInstance()?.PlayShardPickup();
            DamagePopup.CreateText(spawnPopPos, $"+{finalShards:N0} Shards", new Color(0f, 0.85f, 1f));
        }
        else if (itemId == "potion")
        {
            if (player != null)
            {
                var inv = player.GetComponent<PlayerInventory>();
                if (inv != null) inv.AddPotions(quantity);
            }
            SoundManager.EnsureInstance()?.PlayGemSocket();
            DamagePopup.CreateText(spawnPopPos, $"+{quantity} Potion", new Color(1f, 0.35f, 0.45f));
        }
        else
        {
            var itemDef = InventoryItemDatabase.GetItem(itemId);
            string nameStr = !string.IsNullOrEmpty(customName) ? customName : (itemDef != null ? (LocalizationManager.IsVietnamese ? itemDef.nameVi : itemDef.nameEn) : itemId);
            Color col = GetRarityColor(rarityName);

            AccountDatabase.AddItemToActiveUser(itemId, quantity, "Material", "Rơi từ Rương / Chiến trận");
            SoundManager.EnsureInstance()?.PlayGemSocket();
            DamagePopup.CreateText(spawnPopPos, $"+{quantity} {nameStr}!", col);
        }

        if (InGameInventoryUI.Instance != null && InGameInventoryUI.Instance.IsOpen())
        {
            InGameInventoryUI.Instance.RefreshModal();
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Bắn tung 1 vật phẩm bay vút lên cao theo đường vòng cung Parabol thực tế
    /// </summary>
    public static WorldItemPickup SpawnArc(Vector3 originPos, string itemId, int count = 1, Sprite customIcon = null, string rarityStr = "Common", Vector2? launchDir = null, string customName = "")
    {
        GameObject obj = new GameObject($"DropItem_{itemId}");
        obj.transform.position = originPos;

        // 1. Shadow bên dưới sàn đất
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(obj.transform, false);
        shadowObj.transform.localPosition = Vector3.zero;
        var sSr = shadowObj.AddComponent<SpriteRenderer>();
        sSr.sprite = GetShadowSprite();
        sSr.sortingOrder = 4990;
        sSr.color = new Color(0f, 0f, 0f, 0.5f);

        // 2. Visual container bay lơ lửng trên cao
        GameObject visualObj = new GameObject("Visual");
        visualObj.transform.SetParent(obj.transform, false);
        visualObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);

        var sr = visualObj.AddComponent<SpriteRenderer>();
        Sprite iconSprite = customIcon;
        if (iconSprite == null)
        {
            if (itemId == "shards")
            {
                iconSprite = Resources.Load<Sprite>("Items/aethel_shard");
#if UNITY_EDITOR
                if (iconSprite == null) iconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/aethel_shard.png");
#endif
            }
            else if (itemId == "potion")
            {
                iconSprite = Resources.Load<Sprite>("Items/health_potion");
#if UNITY_EDITOR
                if (iconSprite == null) iconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/health_potion.png");
#endif
            }
            else
            {
                var def = InventoryItemDatabase.GetItem(itemId);
                if (def != null)
                {
                    iconSprite = def.Icon;
                    rarityStr = def.rarity.ToString();
                }
            }
        }
        sr.sprite = iconSprite;
        sr.sortingOrder = 5100;
        sr.color = Color.white;

        // 3. Quầng sáng Glow theo độ hiếm
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(visualObj.transform, false);
        glowObj.transform.localPosition = Vector3.zero;
        var glowSr = glowObj.AddComponent<SpriteRenderer>();
        glowSr.sprite = iconSprite;
        Color rColor = GetRarityColor(rarityStr);
        glowSr.color = rColor * 0.45f;
        glowSr.sortingOrder = 5090;
        glowObj.transform.localScale = Vector3.one * 1.35f;

        var pickup = obj.AddComponent<WorldItemPickup>();
        pickup.itemId = itemId;
        pickup.quantity = count;
        pickup.rarityName = rarityStr;
        pickup.customName = customName;
        pickup.visualTransform = visualObj.transform;
        pickup.shadowTransform = shadowObj.transform;
        pickup.spriteRenderer = sr;
        pickup.glowRenderer = glowSr;
        pickup.shadowRenderer = sSr;
        pickup.baseGlowColor = rColor;
        pickup.groundPos = originPos;
        pickup.currentHeight = 0.35f;

        // Tính vector lực phóng
        Vector2 dir = launchDir.HasValue ? launchDir.Value : Random.insideUnitCircle.normalized;
        if (dir.magnitude < 0.1f) dir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

        pickup.horizontalVelocity = new Vector3(dir.x, dir.y * 0.75f, 0f) * Random.Range(3.2f, 5.5f);
        pickup.heightVelocity = Random.Range(6.5f, 9.8f);

        return pickup;
    }

    public static WorldItemPickup Spawn(Vector3 pos, string itemId, int count = 1)
    {
        return SpawnArc(pos, itemId, count);
    }

    public static void RollDrop(Vector3 pos, bool isBoss = false, float dropChanceBonus = 0f)
    {
        string[] commonOres = { "ore_stone", "ore_small_rocks", "ore_coal", "ore_iron", "ore_copper" };
        string[] rareGems = { "gem_amber", "gem_topaz", "gem_sapphire", "gem_jade", "ore_gold_node", "ore_cyan_tin" };
        string[] epicGems = { "gem_amethyst", "gem_bloodstone", "ore_forge_stone", "chest_ancient_relic" };

        if (isBoss)
        {
            int dropCount = Random.Range(2, 4);
            for (int i = 0; i < dropCount; i++)
            {
                string dropItem = (i == 0 && Random.value < 0.65f)
                    ? epicGems[Random.Range(0, epicGems.Length)]
                    : ((Random.value < 0.35f) ? epicGems[Random.Range(0, epicGems.Length)] : rareGems[Random.Range(0, rareGems.Length)]);
                
                SpawnArc(pos, dropItem, 1);
            }
            return;
        }

        float roll = Random.value;
        if (roll < Mathf.Clamp01(0.14f + dropChanceBonus))
        {
            string dropItem;
            float rarityRoll = Random.value;
            if (rarityRoll < 0.06f) dropItem = epicGems[Random.Range(0, epicGems.Length)];
            else if (rarityRoll < 0.30f) dropItem = rareGems[Random.Range(0, rareGems.Length)];
            else dropItem = commonOres[Random.Range(0, commonOres.Length)];

            if (!string.IsNullOrEmpty(dropItem))
            {
                SpawnArc(pos, dropItem, 1);
            }
        }
    }

    public static Color GetRarityColor(string rarity)
    {
        switch (rarity)
        {
            case "Currency": return new Color(0f, 0.85f, 1f);
            case "Legendary": return new Color(1f, 0.75f, 0.15f);
            case "Epic": return new Color(0.75f, 0.35f, 0.95f);
            case "Rare": return new Color(0.25f, 0.65f, 1f);
            default: return new Color(0.85f, 0.9f, 0.95f);
        }
    }

    public static Color GetRarityColor(InventoryItemDatabase.ItemRarity rarity)
    {
        switch (rarity)
        {
            case InventoryItemDatabase.ItemRarity.Rare: return new Color(0.2f, 0.75f, 1f);
            case InventoryItemDatabase.ItemRarity.Epic: return new Color(0.75f, 0.3f, 1f);
            case InventoryItemDatabase.ItemRarity.Legendary: return new Color(1f, 0.85f, 0.2f);
            case InventoryItemDatabase.ItemRarity.Mythic: return new Color(1f, 0.25f, 0.3f);
            default: return new Color(0.85f, 0.9f, 0.95f);
        }
    }
}

