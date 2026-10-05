using UnityEngine;

/// <summary>
/// Vat the co the chem be/chem vo trong game (Pots, Jars, Foliage, Flowers).
/// Ke thua IDamageable de tuong thich 100% voi cac don danh thuong, Dash va Tuyet ky cua Nguoi choi.
/// - Binh gom (Pot): Chem vo roi manh gom + ti le roi Binh Mau (35%) hoac Tinh The Xanh (50%).
/// - Hoa co (Foliage): Chem dut roi la/canh hoa bay tung toe + 0% roi vat pham.
/// </summary>
public class BreakableProp : MonoBehaviour, IDamageable
{
    public enum PropType
    {
        Pot,        // Binh / Chum / Vo gom (Co roi do: Mau + Shards)
        Foliage     // Hoa / Co / Bui cay (Khong roi do, chi co hieu ung la bay)
    }

    [Header("Prop Configuration")]
    [SerializeField] private PropType propType = PropType.Pot;
    [SerializeField] private int health = 1;

    [Header("Pot Drop Rates")]
    [Range(0f, 1f)] [SerializeField] private float potionDropChance = 0.35f;
    [Range(0f, 1f)] [SerializeField] private float shardDropChance = 0.50f;
    [SerializeField] private int minShards = 1;
    [SerializeField] private int maxShards = 3;

    private bool isBroken = false;
    private Collider2D col;
    private SpriteRenderer sr;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        if (col == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 0.8f);
            box.isTrigger = (propType == PropType.Foliage);
            col = box;
        }

        if (sr != null && sr.sortingOrder == 0)
        {
            sr.sortingOrder = 4900;
        }
    }

    public void Setup(PropType type)
    {
        propType = type;
        if (col is BoxCollider2D box)
        {
            box.isTrigger = (propType == PropType.Foliage);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isBroken) return;

        health -= damage;
        if (health <= 0)
        {
            Break();
        }
    }

    public void Break()
    {
        if (isBroken) return;
        isBroken = true;

        if (col != null) col.enabled = false;
        if (sr != null) sr.enabled = false;

        Vector3 spawnPos = transform.position;

        if (propType == PropType.Pot)
        {
            // 1. Hieu ung manh vo gom
            BreakableDebrisFX.SpawnPotShatter(spawnPos);
            SoundManager.EnsureInstance()?.PlayHitImpact();

            // 2. Tinh ti le roi vat pham
            float roll = Random.value;
            if (roll < potionDropChance)
            {
                // Roi Binh Mau Do
                HealthPotion.Spawn(spawnPos, 1);
            }
            else if (roll < potionDropChance + shardDropChance)
            {
                // Roi Tinh The Xanh Aethel Shard (1 - 3 vien)
                int count = Random.Range(minShards, maxShards + 1);
                AethelShard.SpawnShards(spawnPos, count);
            }
            else
            {
                // 15% con lai co the roi khoang thach / soi linh
                WorldItemPickup.RollDrop(spawnPos, false, -0.05f);
            }
        }
        else if (propType == PropType.Foliage)
        {
            // Hieu ung la rung / canh hoa bay tung toe (100% khong roi vat pham)
            BreakableDebrisFX.SpawnFoliageCut(spawnPos);
            SoundManager.EnsureInstance()?.PlaySwordSlash();
        }

        Destroy(gameObject, 0.05f);
    }

    public static BreakableProp Create(Vector3 pos, PropType type, int variant = 0, Transform parent = null)
    {
        GameObject go = new GameObject(type == PropType.Pot ? $"Breakable_Pot_{variant}" : $"Breakable_Foliage_{variant}");
        go.transform.position = pos;
        if (parent != null) go.transform.SetParent(parent);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4900;

        string spritePath = "";
        if (type == PropType.Pot)
        {
            // Chon cac sprite binh gom pixel co san trong pack
            string[] potSprites = new string[]
            {
                "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_83_16x17.png",
                "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_84_16x16.png",
                "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_01_12x16.png",
                "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_72_12x14.png"
            };
            spritePath = potSprites[variant % potSprites.Length];
        }
        else
        {
            // Chon cac sprite hoa co, bui cay
            string[] foliageSprites = new string[]
            {
                "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_11_14x18.png",
                "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_12_14x16.png",
                "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_15_13x15.png",
                "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_14_14x14.png"
            };
            spritePath = foliageSprites[variant % foliageSprites.Length];
        }

#if UNITY_EDITOR
        sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
#endif

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);
        col.isTrigger = (type == PropType.Foliage);

        var prop = go.AddComponent<BreakableProp>();
        prop.Setup(type);

        return prop;
    }
}
