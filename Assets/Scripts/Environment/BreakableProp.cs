using UnityEngine;

/// <summary>
/// Vat the co the chem be/chem vo trong game (Pots, Jars, Foliage, Flowers).
/// Ke thua IDamageable de tuong thich 100% voi cac don danh thuong, Dash va Tuyet ky cua Nguoi choi.
/// - Binh gom (Pot): Chem vo hien thi de vo Blender 3D (ancient_pot_broken), roi 10-25 Linh Hon + Binh Mau.
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
    [SerializeField] private int minShards = 10;
    [SerializeField] private int maxShards = 25;

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

        Vector3 spawnPos = transform.position;

        if (propType == PropType.Pot)
        {
            // 1. Chuyen sprite sang de gom vo (ancient_pot_broken tu Blender) nam sat mat dat
            Sprite brokenSprite = Resources.Load<Sprite>("Props/AncientPot/ancient_pot_broken");
            if (brokenSprite != null && sr != null)
            {
                sr.sprite = brokenSprite;
                sr.sortingOrder = 40; // Dat duoi chan nguoi choi va quai
                transform.position = spawnPos + new Vector3(0, -0.15f, 0);
            }
            else if (sr != null)
            {
                sr.enabled = false;
            }

            // 2. Hieu ung manh vo gom bay tung toe + am thanh + rung man hinh
            BreakableDebrisFX.SpawnPotShatter(spawnPos);
            SoundManager.EnsureInstance()?.PlayHitImpact();
            var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (cam != null) cam.Shake(0.12f, 0.2f);

            // 3. Tinh ti le roi vat pham:
            // Ti le roi Binh Mau Do (35%)
            if (Random.value < potionDropChance)
            {
                HealthPotion.Spawn(spawnPos + new Vector3(Random.Range(-0.3f, 0.3f), 0.2f, 0), 1);
            }

            // Luon roi 10 - 25 Linh Hon (Aethel Shard) cuc ky thoa man khi chem vo
            int count = Random.Range(minShards, maxShards + 1);
            AethelShard.SpawnShards(spawnPos, count);

            // 20% ti le roi them nguyen lieu / soi linh
            if (Random.value < 0.20f)
            {
                WorldItemPickup.RollDrop(spawnPos, false, -0.05f);
            }

            // Tu dong don dep manh de vo sau 15 giay de khong lam nang ram
            Destroy(gameObject, 15f);
        }
        else if (propType == PropType.Foliage)
        {
            if (sr != null) sr.enabled = false;
            // Hieu ung la rung / canh hoa bay tung toe (100% khong roi vat pham)
            BreakableDebrisFX.SpawnFoliageCut(spawnPos);
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            Destroy(gameObject, 0.05f);
        }
    }

    public static BreakableProp Create(Vector3 pos, PropType type, int variant = 0, Transform parent = null)
    {
        GameObject go = new GameObject(type == PropType.Pot ? $"Breakable_Pot_{variant}" : $"Breakable_Foliage_{variant}");
        go.transform.position = pos;
        if (parent != null) go.transform.SetParent(parent);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4900;

        if (type == PropType.Pot)
        {
            // Uu tien toi thuong: Load Sprite Blender 3D render sac net tu Resources
            Sprite potSprite = Resources.Load<Sprite>("Props/AncientPot/ancient_pot_intact");
            if (potSprite != null)
            {
                sr.sprite = potSprite;
                go.transform.localScale = Vector3.one * 0.75f;
            }
            else
            {
                // Fallback cac sprite pixel du phong
                string[] potSprites = new string[]
                {
                    "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_83_16x17.png",
                    "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_84_16x16.png",
                    "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_01_12x16.png",
                    "Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_72_12x14.png"
                };
                string spritePath = potSprites[variant % potSprites.Length];
                string fileName = System.IO.Path.GetFileNameWithoutExtension(spritePath);
                sr.sprite = Resources.Load<Sprite>("Props/" + fileName);
#if UNITY_EDITOR
                if (sr.sprite == null) sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
#endif
            }
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
            string spritePath = foliageSprites[variant % foliageSprites.Length];
            string fileName = System.IO.Path.GetFileNameWithoutExtension(spritePath);
            sr.sprite = Resources.Load<Sprite>("Props/" + fileName);
#if UNITY_EDITOR
            if (sr.sprite == null) sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
#endif
        }

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);
        col.isTrigger = (type == PropType.Foliage);

        var prop = go.AddComponent<BreakableProp>();
        prop.Setup(type);

        return prop;
    }
}
