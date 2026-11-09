using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Hệ thống Minimap Radar (Bản đồ thu nhỏ) & La Bàn Dẫn Đường (Objective Compass).
/// Thiết kế chuẩn Dark Fantasy Action RPG: Radar hình tròn chống răng cưa, các điểm chấm (Blip) bo tròn phát sáng,
/// hoàn toàn không bị đè lên các nút góc trên màn hình.
/// </summary>
public class MinimapHUD : MonoBehaviour
{
    public static MinimapHUD Instance { get; private set; }

    /// <summary>
    /// Cho phép bật/tắt toàn bộ hệ thống Minimap Radar trên màn hình.
    /// Đặt true để hiển thị chuẩn Dark Fantasy Action RPG.
    /// </summary>
    public static bool EnableMinimapHUD = true;

    // --- DỮ LIỆU ĐỊA HÌNH CHIA SẺ VỚI FULLSCREEN MAP ---
    public static Sprite SharedTerrainSprite { get; private set; }
    public static Vector2 SharedTerrainOriginWorld { get; private set; }
    public static Vector2 SharedTerrainSizeWorld { get; private set; }

    [Header("Minimap Settings")]
    [SerializeField] private float radarRadius = 35f; // Bán kính quét thực tế trong game (35m)
    [SerializeField] private float mapUiRadius = 56f;  // Bán kính hiển thị trên Minimap UI (pixel)

    [Header("UI References")]
    private RectTransform radarPanelRt;
    private Image playerIconImg;
    private RectTransform playerIconRt;
    private RectTransform compassNeedleRt;
    private Image compassNeedleImg;
    private TextMeshProUGUI compassDistanceText;
    private TextMeshProUGUI realmTitleText;
    private RectTransform blipContainer;
    private RectTransform terrainMapRt;
    private Image terrainMapImg;
    private Vector2 terrainOriginWorld;
    private float terrainPixelsPerUnit = 1.6f;

    [Header("Target Tracking")]
    private Transform playerTransform;

    // Procedural Sprites
    private static Sprite circleDiscSprite;
    private static Sprite roundDotSprite;
    private static Sprite arrowSprite;
    private static Sprite diamondSprite;

    // Blip pool for performance
    private List<Image> activeBlips = new List<Image>();
    private Queue<Image> blipPool = new Queue<Image>();

    private float updateTimer = 0f;

    void Awake()
    {
        if (!EnableMinimapHUD)
        {
            gameObject.SetActive(false);
            return;
        }

        Instance = this;
        EnsureProceduralSprites();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        FindPlayer();
        UpdateRealmTitle();
        BuildTerrainMap();
    }

    private void UpdateRealmTitle()
    {
        if (realmTitleText == null) return;
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Map1_Thestart")
        {
            realmTitleText.text = LocalizationManager.T("<color=#FFD700>VỰC THẲM KHỞI ĐẦU</color>", "<color=#FFD700>ABYSSAL SANCTUARY</color>");
        }
        else if (sceneName == "Map2_ValleyRuin")
        {
            realmTitleText.text = LocalizationManager.T("<color=#FFD700>THUNG LŨNG TÀN TÍCH</color>", "<color=#FFD700>VALLEY OF RUINS</color>");
        }
        else if (sceneName.Contains("Map3") || sceneName.Contains("Ancient"))
        {
            realmTitleText.text = LocalizationManager.T("<color=#FFD700>VÙNG ĐẤT LÃNG QUÊN</color>", "<color=#FFD700>FORGOTTEN REALM</color>");
        }
        else
        {
            realmTitleText.text = LocalizationManager.T("<color=#FFD700>TÀN TÍCH RUINBOUND</color>", "<color=#FFD700>RUIN REALM</color>");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Tab))
        {
            FullscreenMapUI.EnsureUIExists()?.ToggleMap();
        }

        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // Cập nhật hướng xoay của icon Player trên Minimap
        float playerFacing = playerTransform.localScale.x > 0 ? 1f : -1f;
        if (playerIconRt != null)
        {
            // Lật mặt avatar theo hướng di chuyển của nhân vật (trái / phải)
            playerIconRt.localScale = new Vector3(playerFacing, 1f, 1f);
        }

        // Cập nhật vị trí địa hình di chuyển mượt mà dưới tâm la bàn
        if (terrainMapRt != null)
        {
            Vector2 p = (Vector2)playerTransform.position;
            Vector2 offsetFromOrigin = p - terrainOriginWorld;
            terrainMapRt.anchoredPosition = -offsetFromOrigin * terrainPixelsPerUnit;
        }

        // Cập nhật La Bàn Dẫn Đường (Compass Needle)
        UpdateObjectiveCompass();

        // Cập nhật các chấm Blip trên Radar định kỳ mỗi 0.04s (25 FPS) để siêu mượt & nhẹ máy
        updateTimer += Time.deltaTime;
        if (updateTimer >= 0.04f)
        {
            updateTimer = 0f;
            UpdateRadarBlips();
        }
    }

    private void BuildTerrainMap()
    {
        var tilemaps = Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsSortMode.None);
        if (tilemaps == null || tilemaps.Length == 0) return;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        HashSet<Vector2Int> groundCells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> wallCells = new HashSet<Vector2Int>();

        foreach (var tm in tilemaps)
        {
            if (tm == null) continue;
            string n = tm.gameObject.name.ToLower();
            bool isGround = n.Contains("ground") || n.Contains("floor");
            bool isWall = n.Contains("wall");
            if (!isGround && !isWall) continue;

            BoundsInt b = tm.cellBounds;
            for (int x = b.xMin; x < b.xMax; x++)
            {
                for (int y = b.yMin; y < b.yMax; y++)
                {
                    if (tm.HasTile(new Vector3Int(x, y, 0)))
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;

                        Vector2Int c = new Vector2Int(x, y);
                        if (isWall) wallCells.Add(c);
                        else groundCells.Add(c);
                    }
                }
            }
        }

        if (groundCells.Count == 0 && wallCells.Count == 0) return;

        int pad = 3;
        minX -= pad; maxX += pad;
        minY -= pad; maxY += pad;
        int widthCells = Mathf.Max(1, maxX - minX + 1);
        int heightCells = Mathf.Max(1, maxY - minY + 1);

        int ppu = 4; // 4 pixels per tile cell
        int texW = Mathf.Clamp(widthCells * ppu, 64, 512);
        int texH = Mathf.Clamp(heightCells * ppu, 64, 512);

        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;

        Color waterColor = new Color(0.04f, 0.08f, 0.15f, 0.85f);
        Color groundColor = new Color(0.18f, 0.25f, 0.35f, 0.95f);
        Color seamColor = new Color(0.24f, 0.32f, 0.44f, 0.95f);
        Color wallColor = new Color(0.42f, 0.32f, 0.18f, 1f);

        for (int py = 0; py < texH; py++)
        {
            int cy = minY + (py / ppu);
            for (int px = 0; px < texW; px++)
            {
                int cx = minX + (px / ppu);
                Vector2Int cell = new Vector2Int(cx, cy);

                if (wallCells.Contains(cell))
                {
                    tex.SetPixel(px, py, wallColor);
                }
                else if (groundCells.Contains(cell))
                {
                    bool isSeam = (px % ppu == 0 || py % ppu == 0);
                    tex.SetPixel(px, py, isSeam ? seamColor : groundColor);
                }
                else
                {
                    tex.SetPixel(px, py, waterColor);
                }
            }
        }
        tex.Apply();

        terrainOriginWorld = new Vector2(minX, minY);
        terrainPixelsPerUnit = (mapUiRadius / radarRadius);

        Sprite terrSprite = Sprite.Create(tex, new Rect(0, 0, texW, texH), Vector2.zero);

        // Lưu dữ liệu chia sẻ với FullscreenMapUI
        SharedTerrainSprite = terrSprite;
        SharedTerrainOriginWorld = terrainOriginWorld;
        SharedTerrainSizeWorld = new Vector2(widthCells, heightCells);

        if (terrainMapImg != null)
        {
            terrainMapImg.sprite = terrSprite;
            terrainMapImg.color = Color.white;
        }
        if (terrainMapRt != null)
        {
            terrainMapRt.sizeDelta = new Vector2(widthCells, heightCells) * terrainPixelsPerUnit;
        }
    }

    public static Sprite GetOrBuildTerrainSprite(out Vector2 origin, out Vector2 size)
    {
        if (SharedTerrainSprite != null)
        {
            origin = SharedTerrainOriginWorld;
            size = SharedTerrainSizeWorld;
            return SharedTerrainSprite;
        }

        if (Instance != null)
        {
            Instance.BuildTerrainMap();
            origin = SharedTerrainOriginWorld;
            size = SharedTerrainSizeWorld;
            return SharedTerrainSprite;
        }

        // Quét độc lập nếu Instance chưa có
        origin = Vector2.zero;
        size = Vector2.zero;
        return null;
    }

    public static Sprite GetPlayerAvatarSprite()
    {
        string heroId = PlayerPrefs.GetString("SelectedHero", "vael");
        if (string.IsNullOrEmpty(heroId)) heroId = "vael";
        heroId = heroId.ToLower();

        // 1. Thử load avatar từ UI/Avatars
        Sprite av = Resources.Load<Sprite>($"UI/Avatars/Avatar_{heroId}");
        if (av == null) av = Resources.Load<Sprite>($"UI/Avatars/avatar_{heroId}");
        if (av == null)
        {
            if (heroId.Contains("titan")) av = Resources.Load<Sprite>("UI/Avatars/Avatar_TitanBerserker") ?? Resources.Load<Sprite>("UI/Avatars/avatar_titan");
            else if (heroId.Contains("azure")) av = Resources.Load<Sprite>("UI/Avatars/Avatar_AzureBlade") ?? Resources.Load<Sprite>("UI/Avatars/avatar_azure");
            else if (heroId.Contains("ronin")) av = Resources.Load<Sprite>("UI/Avatars/Avatar_GaleRonin") ?? Resources.Load<Sprite>("UI/Avatars/avatar_ronin");
            else if (heroId.Contains("dune")) av = Resources.Load<Sprite>("UI/Avatars/Avatar_DuneAssassin") ?? Resources.Load<Sprite>("UI/Avatars/avatar_dune");
            else if (heroId.Contains("mage")) av = Resources.Load<Sprite>("UI/Avatars/Avatar_CelestialMage") ?? Resources.Load<Sprite>("UI/Avatars/avatar_mage");
            else av = Resources.Load<Sprite>("UI/Avatars/Avatar_Vaelstrider") ?? Resources.Load<Sprite>("UI/Avatars/avatar_vael");
        }
        if (av != null) return av;

        // 2. Fallback: Lấy SpriteRenderer từ Player trong scene
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            var sr = pc.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null) return sr.sprite;
        }

        return roundDotSprite;
    }

    private void FindPlayer()
    {
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;
    }

    public static List<GameObject> GetAllEnemies()
    {
        HashSet<GameObject> set = new HashSet<GameObject>();
        try
        {
            var tagged = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var go in tagged)
            {
                if (go != null && go.activeInHierarchy) set.Add(go);
            }
        }
        catch { }

        // Quét toàn bộ AI components để không bao giờ bị sót quái
        var wheels = Object.FindObjectsByType<BotWheelAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var w in wheels) if (w != null && w.gameObject.activeInHierarchy) set.Add(w.gameObject);

        var golems = Object.FindObjectsByType<ValleyGolemBossAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var g in golems) if (g != null && g.gameObject.activeInHierarchy) set.Add(g.gameObject);

        var kings = Object.FindObjectsByType<SkeletonKingBossAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var k in kings) if (k != null && k.gameObject.activeInHierarchy) set.Add(k.gameObject);

        var bloods = Object.FindObjectsByType<BloodMonsterAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var b in bloods) if (b != null && b.gameObject.activeInHierarchy) set.Add(b.gameObject);

        var slimes = Object.FindObjectsByType<SlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var s in slimes) if (s != null && s.gameObject.activeInHierarchy) set.Add(s.gameObject);

        var skeletons = Object.FindObjectsByType<SkeletonAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var sk in skeletons) if (sk != null && sk.gameObject.activeInHierarchy) set.Add(sk.gameObject);

        var goblins = Object.FindObjectsByType<GoblinAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var gb in goblins) if (gb != null && gb.gameObject.activeInHierarchy) set.Add(gb.gameObject);

        var beasts = Object.FindObjectsByType<ValleyBeastAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var vb in beasts) if (vb != null && vb.gameObject.activeInHierarchy) set.Add(vb.gameObject);

        var spitters = Object.FindObjectsByType<ValleySpitterAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var vs in spitters) if (vs != null && vs.gameObject.activeInHierarchy) set.Add(vs.gameObject);

        var cocoons = Object.FindObjectsByType<BoneEggCocoon>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var c in cocoons) if (c != null && c.gameObject.activeInHierarchy) set.Add(c.gameObject);

        var eggs = Object.FindObjectsByType<BoneEggObstacle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var e in eggs) if (e != null && e.gameObject.activeInHierarchy) set.Add(e.gameObject);

        return new List<GameObject>(set);
    }

    private void UpdateObjectiveCompass()
    {
        Transform priorityTarget = null;
        string targetLabel = "";

        // Ưu tiên 1: Boss đang còn sống
        var bossKing = Object.FindAnyObjectByType<SkeletonKingBossAI>();
        if (bossKing != null && bossKing.gameObject.activeInHierarchy)
        {
            priorityTarget = bossKing.transform;
            targetLabel = LocalizationManager.T("CHÚA TỂ NHỆN", "SPIDER LORD");
        }
        if (priorityTarget == null)
        {
            var bossGolem = Object.FindAnyObjectByType<ValleyGolemBossAI>();
            if (bossGolem != null && bossGolem.gameObject.activeInHierarchy)
            {
                priorityTarget = bossGolem.transform;
                targetLabel = LocalizationManager.T("CỰ THẠCH HỘ VỆ", "VALLEY GOLEM");
            }
        }
        if (priorityTarget == null)
        {
            var bossBlood = Object.FindAnyObjectByType<BloodMonsterAI>();
            if (bossBlood != null && bossBlood.gameObject.activeInHierarchy && bossBlood.isBoss)
            {
                priorityTarget = bossBlood.transform;
                targetLabel = LocalizationManager.T("HUYẾT QUÁI CỔ", "BLOOD FIEND");
            }
        }
        if (priorityTarget == null)
        {
            var bossWheel = Object.FindAnyObjectByType<BotWheelAI>();
            if (bossWheel != null && bossWheel.gameObject.activeInHierarchy && (bossWheel.name.Contains("Boss") || bossWheel.name.Contains("Titan")))
            {
                priorityTarget = bossWheel.transform;
                targetLabel = LocalizationManager.T("CỰ BÁNH XE TITAN", "TITAN WHEEL");
            }
        }

        // Ưu tiên 2: Cổng Dịch Chuyển (Portal)
        if (priorityTarget == null)
        {
            var portal = Object.FindAnyObjectByType<RuinPortal>();
            if (portal != null && portal.gameObject.activeInHierarchy)
            {
                priorityTarget = portal.transform;
                targetLabel = LocalizationManager.T("CỔNG TÀN TÍCH", "RUIN PORTAL");
            }
        }

        // Ưu tiên 3: Trụ Nâng Cấp (Ancient Shrine)
        if (priorityTarget == null)
        {
            var shrine = Object.FindAnyObjectByType<AncientShrine>();
            if (shrine != null && shrine.gameObject.activeInHierarchy)
            {
                priorityTarget = shrine.transform;
                targetLabel = LocalizationManager.T("ĐÀI THẦN BÍ", "SHRINE");
            }
        }

        // Ưu tiên 4: Kẻ thù gần nhất
        if (priorityTarget == null)
        {
            var enemies = GetAllEnemies();
            float nearestDist = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null || !e.activeInHierarchy) continue;
                float d = Vector2.Distance(playerTransform.position, e.transform.position);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    priorityTarget = e.transform;
                    targetLabel = LocalizationManager.T("KẺ THÙ", "ENEMY");
                }
            }
        }

        // Cập nhật hướng mũi kim La Bàn
        if (priorityTarget != null)
        {
            Vector2 dir = (priorityTarget.position - playerTransform.position);
            float dist = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            if (compassNeedleRt != null)
            {
                compassNeedleRt.gameObject.SetActive(true);
                compassNeedleRt.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                compassNeedleRt.anchoredPosition = dir.normalized * (mapUiRadius + 2f);
            }

            if (compassDistanceText != null)
            {
                compassDistanceText.gameObject.SetActive(true);
                compassDistanceText.text = $"<color=#FFD700>{targetLabel}</color> <b>{(int)dist}m</b>";
            }
        }
        else
        {
            if (compassNeedleRt != null) compassNeedleRt.gameObject.SetActive(false);
            if (compassDistanceText != null)
            {
                compassDistanceText.text = $"<color=#2ED573>{LocalizationManager.T("AN TOÀN", "SECURE")}</color>";
            }
        }
    }

    private void UpdateRadarBlips()
    {
        for (int i = 0; i < activeBlips.Count; i++)
        {
            activeBlips[i].gameObject.SetActive(false);
            blipPool.Enqueue(activeBlips[i]);
        }
        activeBlips.Clear();

        Vector3 playerPos = playerTransform.position;

        // 1. Quét Kẻ Thù Toàn Diện (Red Circle / Diamond Blips)
        var enemyObjects = GetAllEnemies();
        foreach (var enemy in enemyObjects)
        {
            if (enemy == null || !enemy.activeInHierarchy) continue;
            Vector2 offset = (enemy.transform.position - playerPos);
            float dist = offset.magnitude;
            if (dist > radarRadius) continue;

            bool isBoss = enemy.name.Contains("Boss") || enemy.name.Contains("King") || enemy.name.Contains("Golem") || enemy.name.Contains("Cocoon") || enemy.name.Contains("Titan");
            Color blipColor = isBoss ? new Color(1f, 0.15f, 0.25f, 1f) : new Color(1f, 0.30f, 0.30f, 0.95f);
            Vector2 blipSize = isBoss ? new Vector2(15f, 15f) : new Vector2(9f, 9f);
            Sprite blipSprite = isBoss ? diamondSprite : roundDotSprite;

            Vector2 uiPos = (offset / radarRadius) * mapUiRadius;
            SpawnBlip(uiPos, blipColor, blipSize, blipSprite);
        }

        // 2. Quét Cổng Dịch Chuyển (Cyan Diamond Blip)
        var portals = Object.FindObjectsByType<RuinPortal>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var p in portals)
        {
            if (p == null) continue;
            Vector2 offset = (p.transform.position - playerPos);
            if (offset.magnitude > radarRadius) continue;

            Vector2 uiPos = (offset / radarRadius) * mapUiRadius;
            SpawnBlip(uiPos, new Color(0.1f, 0.9f, 1f, 1f), new Vector2(12f, 12f), diamondSprite);
        }

        // 3. Quét Trụ Nâng Cấp (Golden Star/Diamond Blip)
        var shrines = Object.FindObjectsByType<AncientShrine>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var s in shrines)
        {
            if (s == null) continue;
            Vector2 offset = (s.transform.position - playerPos);
            if (offset.magnitude > radarRadius) continue;

            Vector2 uiPos = (offset / radarRadius) * mapUiRadius;
            SpawnBlip(uiPos, new Color(1f, 0.85f, 0.2f, 1f), new Vector2(12f, 12f), diamondSprite);
        }

        // 4. Quét Hòm Báu (Yellow Rounded Dot Blip)
        var chests = Object.FindObjectsByType<TreasureChest>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var ch in chests)
        {
            if (ch == null) continue;
            Vector2 offset = (ch.transform.position - playerPos);
            if (offset.magnitude > radarRadius) continue;

            Vector2 uiPos = (offset / radarRadius) * mapUiRadius;
            SpawnBlip(uiPos, new Color(1f, 0.7f, 0.1f, 1f), new Vector2(10f, 10f), roundDotSprite);
        }
    }

    private void SpawnBlip(Vector2 localPos, Color color, Vector2 size, Sprite spr)
    {
        // Giới hạn các blip nằm trong hình tròn radar
        if (localPos.magnitude > mapUiRadius)
        {
            localPos = localPos.normalized * mapUiRadius;
        }

        Image blipImg;
        if (blipPool.Count > 0)
        {
            blipImg = blipPool.Dequeue();
            blipImg.gameObject.SetActive(true);
        }
        else
        {
            GameObject bObj = new GameObject("Blip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bObj.layer = 5;
            bObj.transform.SetParent(blipContainer, false);
            blipImg = bObj.GetComponent<Image>();
        }

        RectTransform rt = blipImg.GetComponent<RectTransform>();
        rt.anchoredPosition = localPos;
        rt.sizeDelta = size;
        blipImg.sprite = spr;
        blipImg.color = color;

        activeBlips.Add(blipImg);
    }

    private static void EnsureProceduralSprites()
    {
        if (circleDiscSprite != null) return;

        // 1. Gilded Runic Compass Disc (256x256) - Dark Fantasy Astrolabe với viền vàng Rune và kính hắc diện thạch
        int size = 256;
        Texture2D texDisc = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texDisc.wrapMode = TextureWrapMode.Clamp;
        texDisc.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float rOuter = 125f;
        float rBevel = 114f;
        float rInner = 108f;
        float rRing1 = 70f;
        float rRing2 = 36f;

        Color goldRim = new Color(0.92f, 0.78f, 0.35f, 1.0f);
        Color goldHighlight = new Color(1.0f, 0.94f, 0.65f, 1.0f);
        Color darkGold = new Color(0.48f, 0.36f, 0.12f, 1.0f);
        Color obsidianGlass = new Color(0.04f, 0.06f, 0.10f, 0.90f);
        Color gridLineColor = new Color(0.18f, 0.32f, 0.48f, 0.35f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > rOuter)
                {
                    texDisc.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Viền ngoài chống răng cưa
                float edgeAlpha = Mathf.Clamp01((rOuter - dist) * 2.5f);

                // 1. Viền vàng kim loại Bevel chạm khắc hoa văn cổ đại
                if (dist >= rInner)
                {
                    float angleDeg = Mathf.Atan2(y - center.y, x - center.x) * Mathf.Rad2Deg;
                    if (angleDeg < 0) angleDeg += 360f;

                    // 4 Vạch hướng chính: Bắc (90 độ), Đông (0 độ), Nam (270 độ), Tây (180 độ)
                    bool isCardinal = (Mathf.Abs(angleDeg - 90f) < 2.0f ||
                                       Mathf.Abs(angleDeg - 0f) < 2.0f || Mathf.Abs(angleDeg - 360f) < 2.0f ||
                                       Mathf.Abs(angleDeg - 270f) < 2.0f ||
                                       Mathf.Abs(angleDeg - 180f) < 2.0f);

                    // Điểm Bắc (N) có nốt ngọc Rune vàng rực rỡ
                    bool isNorthMarker = (Mathf.Abs(angleDeg - 90f) < 3.5f && dist >= rBevel);

                    if (isNorthMarker)
                    {
                        texDisc.SetPixel(x, y, new Color(1f, 0.95f, 0.4f, edgeAlpha));
                    }
                    else if (isCardinal)
                    {
                        texDisc.SetPixel(x, y, new Color(1f, 0.88f, 0.3f, edgeAlpha));
                    }
                    else if (dist >= rBevel)
                    {
                        // Mặt ngoài bevel sáng ánh vàng
                        texDisc.SetPixel(x, y, Color.Lerp(darkGold, goldHighlight, (dist - rBevel) / (rOuter - rBevel)) * new Color(1, 1, 1, edgeAlpha));
                    }
                    else
                    {
                        // Rãnh khắc sâu sẫm màu
                        texDisc.SetPixel(x, y, Color.Lerp(darkGold, goldRim, (dist - rInner) / (rBevel - rInner)) * new Color(1, 1, 1, edgeAlpha));
                    }
                }
                // 2. Mặt kính obsidian bên trong cùng các vòng tròn trắc địa ma pháp
                else
                {
                    // Vòng trắc địa ngoại vi
                    if (Mathf.Abs(dist - rRing1) < 0.9f || Mathf.Abs(dist - rRing2) < 0.9f)
                    {
                        texDisc.SetPixel(x, y, gridLineColor);
                    }
                    // Chữ thập tâm la bàn mờ nhạt
                    else if ((Mathf.Abs(x - center.x) < 0.8f || Mathf.Abs(y - center.y) < 0.8f) && dist > 10f)
                    {
                        texDisc.SetPixel(x, y, new Color(0.22f, 0.38f, 0.55f, 0.22f));
                    }
                    else
                    {
                        // Kính hắc diện mờ sẫm Dark Fantasy
                        float centerVignette = Mathf.Clamp01(dist / rInner);
                        Color c = Color.Lerp(new Color(0.02f, 0.04f, 0.07f, 0.88f), obsidianGlass, centerVignette);
                        texDisc.SetPixel(x, y, c);
                    }
                }
            }
        }
        texDisc.Apply();
        circleDiscSprite = Sprite.Create(texDisc, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));

        // 2. Round Dot Sprite (32x32)
        Texture2D texDot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Vector2 cDot = new Vector2(15.5f, 15.5f);
        float rDot = 14f;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), cDot);
                if (d > rDot)
                {
                    texDot.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float a = Mathf.Clamp01((rDot - d) * 2.5f);
                    texDot.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
        }
        texDot.Apply();
        roundDotSprite = Sprite.Create(texDot, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // 3. Arrow Sprite (32x32)
        Texture2D texArrow = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                // Mũi tên nhọn hướng LÊN TRÊN (Pointing UP)
                float nx = (x - 15.5f) / 15.5f;
                float ny = y / 31f;
                if (ny >= Mathf.Abs(nx) * 0.85f && y >= 4)
                {
                    texArrow.SetPixel(x, y, Color.white);
                }
                else
                {
                    texArrow.SetPixel(x, y, Color.clear);
                }
            }
        }
        texArrow.Apply();
        arrowSprite = Sprite.Create(texArrow, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // 4. Diamond Sprite (32x32)
        Texture2D texDia = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = Mathf.Abs(x - 15.5f) / 14f;
                float dy = Mathf.Abs(y - 15.5f) / 14f;
                if (dx + dy <= 1f)
                {
                    texDia.SetPixel(x, y, Color.white);
                }
                else
                {
                    texDia.SetPixel(x, y, Color.clear);
                }
            }
        }
        texDia.Apply();
        diamondSprite = Sprite.Create(texDia, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }

    public static MinimapHUD EnsureUIExists()
    {
        if (!EnableMinimapHUD)
        {
            if (Instance != null && Instance.gameObject != null)
            {
                Instance.gameObject.SetActive(false);
            }
            var allMinimaps = Object.FindObjectsByType<MinimapHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var m in allMinimaps)
            {
                if (m != null && m.gameObject != null) m.gameObject.SetActive(false);
            }
            var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c == null) continue;
                Transform ex = c.transform.Find("Minimap_Compass_HUD");
                if (ex != null) ex.gameObject.SetActive(false);
            }
            return null;
        }

        EnsureProceduralSprites();

        if (Instance != null)
        {
            Instance.gameObject.SetActive(true);
            return Instance;
        }

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("Minimap_Compass_HUD");
        if (existing != null)
        {
            Instance = existing.GetComponent<MinimapHUD>();
            if (Instance != null)
            {
                Instance.gameObject.SetActive(true);
                return Instance;
            }
        }

        // 1. Root Container (Nằm ngay dưới nút Cài Đặt Settings Gear tại góc trên bên phải)
        GameObject rootObj = new GameObject("Minimap_Compass_HUD", typeof(RectTransform), typeof(CanvasRenderer));
        rootObj.layer = 5;
        rootObj.transform.SetParent(canvas.transform, false);

        RectTransform rootRt = rootObj.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(1f, 1f);
        rootRt.anchoredPosition = new Vector2(-15f, -95f); // Cách lề phải 15px, dưới nút gear 95px
        rootRt.sizeDelta = new Vector2(190f, 210f);

        // 1.1 Badge Tên Vùng Đất (Realm Title Header - Nằm cách xa phía trên vòng tròn la bàn)
        GameObject realmObj = new GameObject("RealmTitleBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        realmObj.layer = 5;
        realmObj.transform.SetParent(rootObj.transform, false);
        RectTransform realmRt = realmObj.GetComponent<RectTransform>();
        realmRt.anchoredPosition = new Vector2(0f, 24f);
        realmRt.sizeDelta = new Vector2(180f, 24f);
        Image realmBg = realmObj.GetComponent<Image>();
        realmBg.color = new Color(0.04f, 0.06f, 0.10f, 0.88f);
        var realmOutline = realmObj.AddComponent<Outline>();
        realmOutline.effectColor = new Color(0.85f, 0.70f, 0.25f, 0.75f);
        realmOutline.effectDistance = new Vector2(1f, -1f);

        GameObject realmTxtObj = new GameObject("RealmText", typeof(RectTransform), typeof(CanvasRenderer));
        realmTxtObj.layer = 5;
        realmTxtObj.transform.SetParent(realmObj.transform, false);
        RectTransform realmTxtRt = realmTxtObj.GetComponent<RectTransform>();
        realmTxtRt.anchorMin = Vector2.zero;
        realmTxtRt.anchorMax = Vector2.one;
        realmTxtRt.offsetMin = new Vector2(8f, 0f);
        realmTxtRt.offsetMax = new Vector2(-8f, 0f);
        var realmTmp = realmTxtObj.AddComponent<TextMeshProUGUI>();
        realmTmp.text = "<color=#FFD700>THUNG LŨNG TÀN TÍCH</color>";
        realmTmp.fontSize = 12.5f;
        realmTmp.fontStyle = FontStyles.Bold;
        realmTmp.alignment = TextAlignmentOptions.Center;
        realmTmp.enableWordWrapping = false;

        // 2. Radar Background Disc (120x120 px viền vàng Rune, hỗ trợ Mask cắt địa hình và click mở Bản đồ Lớn)
        GameObject radarBg = new GameObject("RadarDisc", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(Button));
        radarBg.layer = 5;
        radarBg.transform.SetParent(rootObj.transform, false);

        RectTransform rRt = radarBg.GetComponent<RectTransform>();
        rRt.anchoredPosition = new Vector2(0f, -48f);
        rRt.sizeDelta = new Vector2(120f, 120f);

        Image rImg = radarBg.GetComponent<Image>();
        rImg.sprite = circleDiscSprite;
        rImg.color = Color.white;

        Mask rMask = radarBg.GetComponent<Mask>();
        rMask.showMaskGraphic = true;

        Button rBtn = radarBg.GetComponent<Button>();
        rBtn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = rBtn.colors;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        rBtn.colors = colors;
        rBtn.onClick.AddListener(() =>
        {
            FullscreenMapUI.EnsureUIExists()?.ToggleMap();
        });

        // 2.1 Layer Chiếu Địa Hình Thực Tế (Terrain Map Projection Layer)
        GameObject terrObj = new GameObject("TerrainMapProjection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        terrObj.layer = 5;
        terrObj.transform.SetParent(radarBg.transform, false);
        terrObj.transform.SetAsFirstSibling();
        RectTransform tRt = terrObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 0.5f);
        tRt.anchorMax = new Vector2(0.5f, 0.5f);
        tRt.pivot = new Vector2(0.5f, 0.5f);
        tRt.anchoredPosition = Vector2.zero;
        tRt.sizeDelta = new Vector2(120f, 120f);
        Image tImg = terrObj.GetComponent<Image>();
        tImg.raycastTarget = false;

        // 2.2 Container các chấm Blip
        GameObject bContainer = new GameObject("BlipContainer", typeof(RectTransform));
        bContainer.layer = 5;
        bContainer.transform.SetParent(radarBg.transform, false);
        RectTransform bRt = bContainer.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.sizeDelta = Vector2.zero;

        // 2.3 Player Center Icon: AVATAR CHÂN DUNG PLAYER THAY CHO TAM GIÁC XANH
        GameObject pIconObj = new GameObject("PlayerIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pIconObj.layer = 5;
        pIconObj.transform.SetParent(radarBg.transform, false);
        RectTransform pRt = pIconObj.GetComponent<RectTransform>();
        pRt.sizeDelta = new Vector2(24f, 24f);
        pRt.anchoredPosition = Vector2.zero;

        var pImg = pIconObj.GetComponent<Image>();
        pImg.sprite = GetPlayerAvatarSprite();
        pImg.color = Color.white;
        pImg.preserveAspect = true;
        pImg.raycastTarget = false;

        // Viền vàng kim hào quang bao quanh Avatar Player
        var pOutline = pIconObj.AddComponent<Outline>();
        pOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.95f);
        pOutline.effectDistance = new Vector2(1.5f, 1.5f);

        // 2.4 La Bàn Dẫn Đường: Needle Pointer chạy quanh viền hình tròn
        GameObject needleObj = new GameObject("CompassNeedle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        needleObj.layer = 5;
        needleObj.transform.SetParent(radarBg.transform, false);
        RectTransform nRt = needleObj.GetComponent<RectTransform>();
        nRt.sizeDelta = new Vector2(20f, 20f);
        nRt.anchoredPosition = Vector2.zero;

        var nImg = needleObj.GetComponent<Image>();
        nImg.sprite = arrowSprite;
        nImg.color = new Color(1f, 0.85f, 0.25f);
        nImg.raycastTarget = false;

        // 3. Label Khoảng Cách Mục Tiêu Dưới Radar (Bấm vào cũng mở bản đồ)
        GameObject distObj = new GameObject("DistanceBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        distObj.layer = 5;
        distObj.transform.SetParent(rootObj.transform, false);
        RectTransform dRt = distObj.GetComponent<RectTransform>();
        dRt.anchoredPosition = new Vector2(0f, -125f);
        dRt.sizeDelta = new Vector2(170f, 24f);

        Image dImg = distObj.GetComponent<Image>();
        dImg.color = new Color(0.06f, 0.08f, 0.14f, 0.92f);

        var dOutline = distObj.AddComponent<Outline>();
        dOutline.effectColor = new Color(0.85f, 0.70f, 0.25f, 0.65f);
        dOutline.effectDistance = new Vector2(1f, -1f);

        Button dBtn = distObj.GetComponent<Button>();
        dBtn.onClick.AddListener(() =>
        {
            FullscreenMapUI.EnsureUIExists()?.ToggleMap();
        });

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.layer = 5;
        txtObj.transform.SetParent(distObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var dTmp = txtObj.AddComponent<TextMeshProUGUI>();
        dTmp.text = "<b>--</b>";
        dTmp.fontSize = 11.5f;
        dTmp.color = Color.white;
        dTmp.alignment = TextAlignmentOptions.Center;

        // Khởi tạo Component
        var comp = rootObj.AddComponent<MinimapHUD>();
        comp.radarPanelRt = rRt;
        comp.playerIconRt = pRt;
        comp.playerIconImg = pImg;
        comp.compassNeedleRt = nRt;
        comp.compassNeedleImg = nImg;
        comp.compassDistanceText = dTmp;
        comp.realmTitleText = realmTmp;
        comp.blipContainer = bRt;
        comp.terrainMapRt = tRt;
        comp.terrainMapImg = tImg;

        Instance = comp;
        return comp;
    }
}
