using UnityEngine;
using System.Collections.Generic;

public class SwordWaveProjectile : MonoBehaviour
{
    private Vector2 moveDirection = Vector2.down;
    private float speed = 10f;
    private int damage = 12;
    private float lifetime = 1.2f;
    private float maxDistance = 9f;
    private Vector3 startPosition;
    private int maxPierceCount = 5;
    private int currentPierceCount = 0;
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private AzureBladeSkill ownerSkill;
    private bool isUltiWave = false;
    private bool isChargedWave = false;
    private float spawnAge = 0f;

    private static Sprite[] twinSwordFrames = null;
    private float animationTimer = 0f;
    private int currentFrame = 0;
    private float animationFps = 15f;

    public static SwordWaveProjectile Spawn(Vector3 origin, Vector2 direction, int waveDamage, float waveSpeed = 11f, float scaleMultiplier = 1.0f, bool isUlti = false, bool isCharged = false, int pierceLimit = 4, AzureBladeSkill ownerSkill = null)
    {
        string name = isUlti ? "UltiSwordWave" : (isCharged ? "ChargedSwordWave" : "SwordWave");
        GameObject waveObj = new GameObject(name);
        waveObj.transform.position = origin;

        var proj = waveObj.AddComponent<SwordWaveProjectile>();
        proj.Setup(direction, waveDamage, waveSpeed, scaleMultiplier, isUlti, isCharged, pierceLimit, ownerSkill);
        return proj;
    }

    private static void LoadTwinSwordFrames()
    {
        if (twinSwordFrames != null && twinSwordFrames.Length == 8 && twinSwordFrames[0] != null) return;

        List<Sprite> frames = new List<Sprite>();
#if UNITY_EDITOR
        // 1. Ưu tiên bộ animation mới nhất trong twin1: Assets/ART/Player3/Twin_sword/twin1/twin_sword_1_0001.png -> 0008.png
        for (int i = 1; i <= 8; i++)
        {
            string path = $"Assets/ART/Player3/Twin_sword/twin1/twin_sword_1_{i:D4}.png";
            var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (loaded == null)
            {
                var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in allAssets)
                {
                    if (a is Sprite s) { loaded = s; break; }
                }
            }
            if (loaded != null) frames.Add(loaded);
        }

        // 2. Fallback sang đường dẫn cũ nếu thiếu file
        if (frames.Count < 8)
        {
            frames.Clear();
            for (int i = 1; i <= 8; i++)
            {
                string path = $"Assets/ART/Player3/Twin_sword/twin_sword_{i:D4}.png";
                var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (loaded == null)
                {
                    var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var a in allAssets)
                    {
                        if (a is Sprite s) { loaded = s; break; }
                    }
                }
                if (loaded != null) frames.Add(loaded);
            }
        }
#endif
        if (frames.Count == 0)
        {
            for (int i = 1; i <= 8; i++)
            {
                var s = Resources.Load<Sprite>($"Player3/Twin_sword/twin1/twin_sword_1_{i:D4}") ?? Resources.Load<Sprite>($"Player3/Twin_sword/twin_sword_{i:D4}");
                if (s != null) frames.Add(s);
            }
        }

        if (frames.Count > 0)
        {
            twinSwordFrames = frames.ToArray();
        }
    }

    public void Setup(Vector2 direction, int waveDamage, float waveSpeed, float scaleMultiplier, bool isUlti, bool isCharged = false, int pierceLimit = 4, AzureBladeSkill ownerSkill = null)
    {
        this.ownerSkill = ownerSkill;
        this.isUltiWave = isUlti;
        this.isChargedWave = isCharged;
        this.spawnAge = 0f;
        moveDirection = direction.normalized;
        if (moveDirection.sqrMagnitude < 0.001f) moveDirection = Vector2.down;

        damage = waveDamage;
        speed = waveSpeed;
        startPosition = transform.position;
        lifetime = isUlti ? 1.8f : (isCharged ? 1.5f : 1.2f);
        maxDistance = isUlti ? 12f : (isCharged ? 11f : 9f);
        maxPierceCount = isUlti ? 15 : pierceLimit;

        // Visual Setup
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Default";
        spriteRenderer.sortingOrder = 5500;

        LoadTwinSwordFrames();
        if (twinSwordFrames != null && twinSwordFrames.Length > 0)
        {
            spriteRenderer.sprite = twinSwordFrames[0];
        }
        else
        {
            spriteRenderer.sprite = GenerateWaveSprite(isUlti, isCharged);
        }

        spriteRenderer.color = isUlti ? new Color(0.3f, 0.95f, 1f, 1f) : (isCharged ? new Color(0.9f, 1f, 1f, 1f) : Color.white);

        // Rotation towards direction (Sprite Twin_sword có hướng vẽ gốc sang Phải nên góc chuẩn là angle)
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        float baseScale = isUlti ? 1.1f : 1.0f;
        transform.localScale = Vector3.one * baseScale * scaleMultiplier;

        // Collider
        boxCollider = gameObject.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(0.95f, 0.45f);

        // Rigidbody2D required for 100% reliable trigger detection in 2D physics
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Destroy(gameObject, lifetime);
    }

    private Sprite GenerateWaveSprite(bool isUlti, bool isCharged = false)
    {
        int texW = (isUlti || isCharged) ? 48 : 32;
        int texH = (isUlti || isCharged) ? 24 : 16;
        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color transparent = new Color(0, 0, 0, 0);
        Color coreColor = isCharged ? new Color(1f, 1f, 1f, 1f) : (isUlti ? new Color(0.9f, 1f, 1f, 1f) : new Color(0.85f, 0.95f, 1f, 1f));
        Color outerColor = isCharged ? new Color(0.1f, 0.9f, 1f, 1f) : (isUlti ? new Color(0.1f, 0.7f, 1f, 0.9f) : new Color(0.2f, 0.6f, 0.95f, 0.85f));

        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                float normX = (float)x / (texW - 1) * 2f - 1f;
                float normY = (float)y / (texH - 1);
                float arch = 1f - (normX * normX);
                float distToCurve = Mathf.Abs(normY - arch * 0.75f);

                if (distToCurve < 0.28f)
                {
                    if (distToCurve < 0.12f)
                        tex.SetPixel(x, y, coreColor);
                    else
                        tex.SetPixel(x, y, outerColor);
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), 32f);
    }

    void Update()
    {
        spawnAge += Time.deltaTime;
        Vector3 nextPos = transform.position + (Vector3)(moveDirection * speed * Time.deltaTime);

        // Frame-by-frame active overlap sweep để không bao giờ bị lọt qua quái nhỏ (nhện con, v.v.)
        Collider2D[] sweptHits = Physics2D.OverlapBoxAll((transform.position + nextPos) * 0.5f, new Vector2(1.2f, 0.8f), transform.eulerAngles.z);
        if (sweptHits != null && sweptHits.Length > 0)
        {
            for (int i = 0; i < sweptHits.Length; i++)
            {
                if (sweptHits[i] != null)
                {
                    HandleHit(sweptHits[i].gameObject);
                }
            }
        }

        transform.position = nextPos;

        // Frame-by-frame animation of hand-drawn Twin_sword sprites
        if (twinSwordFrames != null && twinSwordFrames.Length > 0 && spriteRenderer != null)
        {
            animationTimer += Time.deltaTime;
            currentFrame = (int)(animationTimer * animationFps) % twinSwordFrames.Length;
            spriteRenderer.sprite = twinSwordFrames[currentFrame];
        }

        // Max distance check
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.gameObject != null)
        {
            HandleHit(collision.gameObject);
        }
    }

    private HashSet<IDamageable> hitDamageables = new HashSet<IDamageable>();
    private HashSet<int> hitInstanceIds = new HashSet<int>();

    private void HandleHit(GameObject hitObj)
    {
        if (hitObj == null) return;

        // Bỏ qua Player
        if (hitObj.CompareTag("Player") || hitObj.GetComponent<PlayerController>() != null || hitObj.GetComponentInParent<PlayerController>() != null) return;

        // Kiểm tra đối tượng có thể nhận sát thương (Quái vật, Rương, Cỏ cây, Bình gốm, Boss)
        var damageable = hitObj.GetComponent<IDamageable>() ?? hitObj.GetComponentInParent<IDamageable>();
        if (damageable != null && !(damageable is PlayerHealth))
        {
            int instanceId = (damageable as Component != null) ? (damageable as Component).gameObject.GetInstanceID() : hitObj.GetInstanceID();
            if (!hitDamageables.Contains(damageable) && !hitInstanceIds.Contains(instanceId))
            {
                hitDamageables.Add(damageable);
                hitInstanceIds.Add(instanceId);

                damageable.TakeDamage(damage);

                // Giảm 50% thời gian hồi chiêu và hồi phục Năng Lượng / Nội Năng khi chém trúng quái vật thực sự
                bool isProp = (hitObj.GetComponent<BreakableProp>() != null || hitObj.GetComponentInParent<BreakableProp>() != null || hitObj.GetComponent<TreasureChest>() != null || hitObj.GetComponentInParent<TreasureChest>() != null);
                if (!isProp)
                {
                    if (ownerSkill != null)
                    {
                        ownerSkill.OnWaveHitEnemy(isChargedWave, isUltiWave);
                    }
                    else
                    {
                        var fallbackSkill = FindAnyObjectByType<AzureBladeSkill>();
                        if (fallbackSkill != null) fallbackSkill.OnWaveHitEnemy(isChargedWave, isUltiWave);
                    }
                }

                SoundManager.EnsureInstance()?.PlayHitImpact();
                DamagePopup.Create(hitObj.transform.position, damage, isCritical: (damage > 20));

                currentPierceCount++;
                if (currentPierceCount >= maxPierceCount)
                {
                    Destroy(gameObject);
                }
            }
        }
        else
        {
            // Không hủy kiếm khí trong 0.12s đầu tiên sau khi spawn (tránh dính tường sau lưng khi đứng sát mép dưới)
            if (spawnAge < 0.12f) return;

            string objName = hitObj.name.ToLower();
            int wallLayer = LayerMask.NameToLayer("Wall");
            int obsLayer = LayerMask.NameToLayer("Obstacle");

            bool isTilemapWall = hitObj.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null ||
                                 hitObj.GetComponent<CompositeCollider2D>() != null;

            bool isBarrier = hitObj.GetComponent<RoomBarrier>() != null || hitObj.GetComponentInParent<RoomBarrier>() != null;

            bool isSolidWall = (wallLayer != -1 && hitObj.layer == wallLayer) ||
                               (obsLayer != -1 && hitObj.layer == obsLayer) ||
                               isTilemapWall ||
                               isBarrier ||
                               objName.Contains("dungeon_wall") ||
                               objName.Contains("dungeonwall") ||
                               (objName.Contains("wall") && !objName.Contains("shadow") && !objName.Contains("ground"));

            var col = hitObj.GetComponent<Collider2D>();
            if (isSolidWall && col != null && !col.isTrigger)
            {
                // Kiểm tra nếu tường ở phía sau hoặc ngang hướng bay thì bỏ qua
                Vector2 dirToObj = ((Vector2)hitObj.transform.position - (Vector2)transform.position);
                if (dirToObj.sqrMagnitude > 0.04f && Vector2.Dot(moveDirection, dirToObj.normalized) < -0.2f)
                {
                    return;
                }

                Destroy(gameObject);
            }
        }
    }
}
