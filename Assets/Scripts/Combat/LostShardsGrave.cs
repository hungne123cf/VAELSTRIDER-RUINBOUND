using UnityEngine;
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Co che Dark Souls / Hollow Knight: Vung Hon / Mo Da giu lai so Shards nguoi choi da danh roi khi chet.
/// Nguoi choi phai vuot qua quai de quay lai nhat lai; neu chet lan 2 truoc khi nhat, so tien cu se bien mat vinh vien!
/// </summary>
public class LostShardsGrave : MonoBehaviour
{
    [Header("Grave Data")]
    [SerializeField] private int lostShardsAmount = 0;

    private Transform playerTransform;
    private TextMeshPro labelTmp;
    private Light2D graveLight;
    private bool isCollected = false;
    private Vector3 initialPos;

    public static void EnsureSpawnGrave()
    {
        int hasLost = PlayerPrefs.GetInt("Has_Lost_Shards", 0);
        int amount = PlayerPrefs.GetInt("Lost_Shards_Amount", 0);
        string deathScene = PlayerPrefs.GetString("Lost_Shards_Scene", "");
        string currentScene = SceneManager.GetActiveScene().name;

        if (hasLost == 1 && amount > 0 && deathScene == currentScene)
        {
            // Kiem tra neu da co grave trong scene chua
            if (Object.FindAnyObjectByType<LostShardsGrave>() != null) return;

            float x = PlayerPrefs.GetFloat("Lost_Shards_X", 0f);
            float y = PlayerPrefs.GetFloat("Lost_Shards_Y", 0f);

            GameObject graveObj = new GameObject("LostShards_SoulGrave");
            graveObj.transform.position = new Vector3(x, y, 0f);
            var comp = graveObj.AddComponent<LostShardsGrave>();
            comp.lostShardsAmount = amount;

            Debug.Log($"<color=cyan>[DARK SOULS]</color> Vung Hon ({amount} Aethel Shards) dang doi ban tai ({x:F1}, {y:F1})!");
        }
    }

    void Awake()
    {
        initialPos = transform.position;
    }

    void Start()
    {
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;

        BuildGraveVisuals();
    }

    void Update()
    {
        if (isCollected) return;

        // 1. Floating bobbing animation
        float bobbing = Mathf.Sin(Time.time * 3f) * 0.12f;
        transform.position = initialPos + new Vector3(0f, bobbing, 0f);

        // 2. Light pulsing
        if (graveLight != null)
        {
            graveLight.intensity = 1.8f + Mathf.Sin(Time.time * 4f) * 0.6f;
        }

        // 3. Distance check to player for collection
        if (playerTransform == null)
        {
            var pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null) playerTransform = pc.transform;
            if (playerTransform == null) return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= 1.4f)
        {
            Collect();
        }
    }

    private void BuildGraveVisuals()
    {
        // 1. Shard Crystal Sprite
        Sprite shardSprite = Resources.Load<Sprite>("Items/aethel_shard");
#if UNITY_EDITOR
        if (shardSprite == null)
        {
            shardSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/aethel_shard.png");
        }
#endif
        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = shardSprite;
        sr.color = new Color(0.4f, 0.95f, 1f, 1f); // Bright ethereal cyan
        sr.sortingLayerName = "Entities";
        sr.sortingOrder = 50;
        transform.localScale = new Vector3(2.2f, 2.2f, 1f);

        // 2. Point Light 2D (beacon glow)
        graveLight = gameObject.AddComponent<Light2D>();
        graveLight.lightType = Light2D.LightType.Point;
        graveLight.color = new Color(0.3f, 0.95f, 1f, 1f);
        graveLight.intensity = 2.2f;
        graveLight.pointLightInnerRadius = 0.5f;
        graveLight.pointLightOuterRadius = 4.2f;

        // 3. Floating Text Banner
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 1.25f, 0f);

        labelTmp = labelObj.AddComponent<TextMeshPro>();
        labelTmp.text = $"<color=#52F0FF>✦ VUNG HON ✦</color>\n<color=#FFE066>+{lostShardsAmount} Shards</color>\n<size=70%>(Cham de thu hoi)</size>";
        labelTmp.fontSize = 3.2f;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.sortingLayerID = SortingLayer.NameToID("UI");
        labelTmp.sortingOrder = 100;

        // 4. Subtle Spirit Embers
        GameObject psObj = new GameObject("SpiritEmbers");
        psObj.transform.SetParent(transform, false);
        var ps = psObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1.4f;
        main.startSpeed = 0.8f;
        main.startSize = 0.10f;
        main.startColor = new Color(0.4f, 0.95f, 1f, 0.85f);
        main.maxParticles = 25;
        var em = ps.emission;
        em.rateOverTime = 8f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.4f;
    }

    private void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        var inv = playerTransform.GetComponent<PlayerInventory>();
        if (inv != null)
        {
            inv.AddShards(lostShardsAmount);
        }

        // Xoa trang thai tien danh roi
        PlayerPrefs.SetInt("Has_Lost_Shards", 0);
        PlayerPrefs.SetInt("Lost_Shards_Amount", 0);
        PlayerPrefs.Save();

        // Am thanh & phan hoi
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayShardPickup();
        }

        Debug.Log($"<color=cyan>[DARK SOULS RECLAIMED]</color> Chuc mung! Ban da thu hoi thanh cong toan bo {lostShardsAmount} Aethel Shards!");

        Destroy(gameObject);
    }
}
