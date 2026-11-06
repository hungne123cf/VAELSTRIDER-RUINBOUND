using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Compact Multi-Path Map Expansion Builder (Map 2 & Map 3)
/// 
/// ĐẶC TÍNH THIẾT KẾ GỌN GÀNG & MƯỢT MÀ:
/// 1. Bỏ hoàn toàn Nhánh Tây và chữ nổi chỉ đường rườm rà.
/// 2. Thu gọn bản đồ chỉ gồm Khu Khởi Đầu + 2 Nhánh Ngắn Gọn (1 Phòng Quái + 1 Phòng Đại Trùm mỗi nhánh),
///    hành lang rộng 8m thông thoáng, đi vài bước là vào trận ngay.
/// 3. Dọn sạch các chướng ngại vật (đá, gốc cây, cây giữa đường) và tắt toàn bộ Collider vô hình quá bự
///    của cây cổ thụ / cây ven đường để người chơi di chuyển mượt mà 100% không bị kẹt vô hình.
/// 4. Bỏ hoàn toàn Boss Cây (ValleyGolemBossAI) & Quái Đá tĩnh (ValleySpitterAI), khôi phục đầy đủ
///    Đại Trùm gốc Map 2 (Gor'Mok - Huyết Thú Cổ Thụ) và Đại Trùm gốc Map 3 (Phong Ấn Trứng Kén & Chúa Tể Nhện Cổ Đại),
///    cùng Đại Trùm Bóng Ma Bản Ngã (ShadowDoppelgangerBoss) ở nhánh còn lại.
/// </summary>
public class MultiPathMapExpansionBuilder : MonoBehaviour
{
    public enum RouteDensity
    {
        SwarmManyMonsters,     // Nhánh Đông Quái (2 đợt quái đông)
        EliteFewMonsters       // Nhánh Ít Quái Tinh Anh (1 đợt quái tinh nhuệ, đi nhanh)
    }

    public enum RouteBossType
    {
        GorMokBloodLord,       // Boss gốc Map 2: Đại Trùm Gor'Mok (BloodMonsterAI Boss 3 Phase)
        AncientSpiderKing,     // Boss gốc Map 3: Phong Ấn Trứng Kén -> Chúa Tể Nhện Cổ Đại (SkeletonKingBossAI)
        ShadowDoppelganger     // Đại Trùm Bóng Ma Bản Ngã (ShadowDoppelgangerBoss)
    }

    private static MultiPathMapExpansionBuilder instance;
    public static MultiPathMapExpansionBuilder Instance => instance;

    private readonly List<RectInt> walkableRects = new List<RectInt>();
    public IReadOnlyList<RectInt> WalkableRects => walkableRects;

    private readonly HashSet<GameObject> activeRoomMonsters = new HashSet<GameObject>();
    private bool isSpawningRoomWave = false;

    public static void EnsureExpandedMap()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool isMap2 = sceneName.Contains("Map2") || sceneName.Contains("Valley");
        bool isMap3 = sceneName.Contains("Map3") || sceneName.Contains("Forgotten");
        if (!isMap2 && !isMap3) return;

        if (FindAnyObjectByType<MultiPathMapExpansionBuilder>() != null) return;

        GameObject go = new GameObject("MultiPathMapExpansionBuilder");
        var builder = go.AddComponent<MultiPathMapExpansionBuilder>();
        builder.BuildCurrentSceneExpansion(isMap2);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void OnEnable()
    {
        BloodMonsterAI.OnEnemyDied += OnBloodMonsterDied;
        ValleyBeastAI.OnEnemyDied += OnValleyBeastDied;
        BotWheelAI.OnEnemyDied += OnBotWheelDied;
        GoblinAI.OnEnemyDied += OnGenericEnemyDied;
        SkeletonAI.OnEnemyDied += OnGenericEnemyDied;
        SlimeAI.OnEnemyDied += OnGenericEnemyDied;
        SkeletonKingBossAI.OnBossDied += OnSkeletonKingDied;
    }

    void OnDisable()
    {
        BloodMonsterAI.OnEnemyDied -= OnBloodMonsterDied;
        ValleyBeastAI.OnEnemyDied -= OnValleyBeastDied;
        BotWheelAI.OnEnemyDied -= OnBotWheelDied;
        GoblinAI.OnEnemyDied -= OnGenericEnemyDied;
        SkeletonAI.OnEnemyDied -= OnGenericEnemyDied;
        SlimeAI.OnEnemyDied -= OnGenericEnemyDied;
        SkeletonKingBossAI.OnBossDied -= OnSkeletonKingDied;
    }

    private void OnBloodMonsterDied(BloodMonsterAI e) => NotifyMonsterSlain(e != null ? e.gameObject : null);
    private void OnValleyBeastDied(ValleyBeastAI e) => NotifyMonsterSlain(e != null ? e.gameObject : null);
    private void OnBotWheelDied(BotWheelAI e) => NotifyMonsterSlain(e != null ? e.gameObject : null);
    private void OnSkeletonKingDied(SkeletonKingBossAI e) => NotifyMonsterSlain(e != null ? e.gameObject : null);
    private void OnGenericEnemyDied(object e)
    {
        if (e is Component comp && comp != null)
            NotifyMonsterSlain(comp.gameObject);
    }

    private void NotifyMonsterSlain(GameObject obj)
    {
        if (obj != null)
        {
            activeRoomMonsters.Remove(obj);
        }
        activeRoomMonsters.RemoveWhere(item => item == null);
    }

    private void BuildCurrentSceneExpansion(bool isMap2)
    {
        walkableRects.Clear();

        if (isMap2)
        {
            BuildMap2CompactValley();
        }
        else
        {
            BuildMap3CompactIslands();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 1. MAP 2: THUNG LŨNG TÀN TÍCH GỌN GÀNG (NHÁNH NAM & NHÁNH ĐÔNG)
    // ═══════════════════════════════════════════════════════════════════════════
    private void BuildMap2CompactValley()
    {
        Tilemap groundTm = null;
        Tilemap wallsTm = null;
        var tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (var tm in tilemaps)
        {
            if (tm == null) continue;
            string n = tm.gameObject.name.ToLower();
            if (n.Contains("ground") || n.Contains("floor")) groundTm = tm;
            else if (n.Contains("wall")) wallsTm = tm;
        }

        if (groundTm == null || wallsTm == null) return;

        TileBase grassTile = groundTm.GetTile(new Vector3Int(-12, 0, 0)) ?? groundTm.GetTile(new Vector3Int(0, 0, 0));
        TileBase dirtTile  = groundTm.GetTile(new Vector3Int(0, -3, 0)) ?? grassTile;
        TileBase wallTile  = wallsTm.GetTile(new Vector3Int(-20, 0, 0)) ?? wallsTm.GetTile(new Vector3Int(20, 0, 0)) ?? wallsTm.GetTile(new Vector3Int(0, -14, 0));

        // Dọn dẹp triệt để các chướng ngại vật chắn đường và tắt toàn bộ Collider vô hình quá bự ở Map 2
        CleanUpObstaclesAndOversizedCollidersMap2();

        // Khu Trung Tâm (Hub): x [-19..19], y [-13..13] (38m x 26m)
        AddWalkableRect(-19, -13, 38, 26);

        // --- LỐI ĐI 1: NHÁNH NAM (-Y) — ĐÔNG QUÁI -> ĐẠI TRÙM GỐC MAP 2: GOR'MOK ---
        AddWalkableRect(-4, -20, 8, 7);    // Hành lang Nam 1 rộng 8m, dài 7m (x: -4..4, y: -20..-13)
        AddWalkableRect(-12, -36, 24, 16); // Phòng Quái Nam (x: -12..12, y: -36..-20)
        AddWalkableRect(-4, -42, 8, 6);    // Hành lang Nam 2 rộng 8m, dài 6m (x: -4..4, y: -42..-36)
        AddWalkableRect(-14, -64, 28, 22); // Phòng Đại Trùm Gor'Mok (x: -14..14, y: -64..-42)

        // --- LỐI ĐI 2: NHÁNH ĐÔNG (+X) — ÍT QUÁI TINH ANH -> ĐẠI TRÙM BÓNG MA & GOR'MOK ---
        AddWalkableRect(19, -4, 7, 8);     // Hành lang Đông 1 rộng 8m, dài 7m (x: 19..26, y: -4..4)
        AddWalkableRect(26, -10, 18, 20);  // Phòng Quái Đông (x: 26..44, y: -10..10)
        AddWalkableRect(44, -4, 6, 8);     // Hành lang Đông 2 rộng 8m, dài 6m (x: 44..50, y: -4..4)
        AddWalkableRect(50, -12, 24, 24);  // Phòng Đại Trùm Đông (x: 50..74, y: -12..12)

        HashSet<Vector2Int> floorCells = new HashSet<Vector2Int>();
        foreach (var r in walkableRects)
        {
            for (int x = r.xMin; x < r.xMax; x++)
            {
                for (int y = r.yMin; y < r.yMax; y++)
                {
                    floorCells.Add(new Vector2Int(x, y));
                }
            }
        }

        // Đặt gạch sàn và xóa tường cũ đè lên lối đi
        foreach (var cell in floorCells)
        {
            Vector3Int pos = new Vector3Int(cell.x, cell.y, 0);
            if (!groundTm.HasTile(pos) && grassTile != null)
            {
                groundTm.SetTile(pos, grassTile);
            }
            if (wallsTm.HasTile(pos))
            {
                wallsTm.SetTile(pos, null);
            }
        }

        // Vẽ tâm đất nện cho 4 phòng mới
        Vector2[] clearingCenters = new Vector2[]
        {
            new Vector2(0f, -28f), new Vector2(0f, -53f),
            new Vector2(35f, 0f),  new Vector2(62f, 0f)
        };
        if (dirtTile != null)
        {
            foreach (var c in clearingCenters)
            {
                int cx = Mathf.RoundToInt(c.x);
                int cy = Mathf.RoundToInt(c.y);
                for (int dx = -5; dx <= 5; dx++)
                {
                    for (int dy = -4; dy <= 4; dy++)
                    {
                        if ((dx * dx) / 25f + (dy * dy) / 16f <= 1.0f)
                        {
                            groundTm.SetTile(new Vector3Int(cx + dx, cy + dy, 0), dirtTile);
                        }
                    }
                }
            }
        }

        // Xây viền tường đá kín xung quanh toàn bộ chu vi bản đồ
        if (wallTile != null)
        {
            HashSet<Vector2Int> wallCells = new HashSet<Vector2Int>();
            foreach (var cell in floorCells)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        Vector2Int nb = new Vector2Int(cell.x + dx, cell.y + dy);
                        if (!floorCells.Contains(nb))
                        {
                            wallCells.Add(nb);
                        }
                    }
                }
            }

            foreach (var wc in wallCells)
            {
                wallsTm.SetTile(new Vector3Int(wc.x, wc.y, 0), wallTile);
            }
        }

        groundTm.RefreshAllTiles();
        wallsTm.RefreshAllTiles();

        var wallCol = wallsTm.GetComponent<TilemapCollider2D>();
        if (wallCol != null)
        {
            wallCol.enabled = false;
            wallCol.enabled = true;
        }

        SetupMap2RoomsAndBarriers();
    }

    private void CleanUpObstaclesAndOversizedCollidersMap2()
    {
        var envRoot = GameObject.Find("Map2_Environment");
        if (envRoot != null)
        {
            for (int i = envRoot.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = envRoot.transform.GetChild(i);
                if (child == null) continue;
                string n = child.name;
                Vector3 p = child.position;

                // 1. Tắt EdgeCollider2D khổng lồ trên Giant_Ancient_Tree để không bao giờ cản đường vô hình
                if (n.Contains("Giant_Ancient_Tree"))
                {
                    var cols = child.GetComponentsInChildren<Collider2D>(true);
                    foreach (var c in cols) c.enabled = false;
                    continue;
                }

                // 2. Xóa toàn bộ cây cối, tảng đá, trụ đá nằm chắn giữa sân hoặc chắn lối đi Đông / Nam
                if (n.Contains("Tree_Mid") || n.Contains("Tree_Bottom") ||
                    n.Contains("Boulder_") || n.Contains("Rock_") ||
                    n.Contains("Glowing_Rune_Pillar") || n.Contains("AncientShrine"))
                {
                    Destroy(child.gameObject);
                    continue;
                }

                // 3. Xóa các cây viền (Forest_Border_Tree) nằm gần cửa Nam (x: -6..6, y < -6) hoặc cửa Đông (x > 10, y: -6..6)
                if (n.Contains("Forest_Border_Tree"))
                {
                    if ((p.x >= -6.5f && p.x <= 6.5f && p.y <= -6.0f) ||
                        (p.x >= 10.0f && p.y >= -6.5f && p.y <= 6.5f))
                    {
                        Destroy(child.gameObject);
                        continue;
                    }
                }

                // 4. Tắt toàn bộ Collider2D trên các cây trang trí ở rìa còn lại để không bao giờ bị vướng tán cây vô hình
                var childCols = child.GetComponentsInChildren<Collider2D>(true);
                foreach (var c in childCols)
                {
                    c.enabled = false;
                }
            }
        }
    }

    private void SetupMap2RoomsAndBarriers()
    {
        Transform barriersRoot = new GameObject("Map2_MultiPathBarriers").transform;
        barriersRoot.SetParent(transform, false);

        Transform roomsRoot = new GameObject("DungeonRooms").transform;
        roomsRoot.SetParent(transform, false);

        var mgr = roomsRoot.gameObject.AddComponent<DungeonRoomManager>();
        mgr.rooms.Clear();

        // Phòng 1: Thánh Địa Trung Tâm
        var hubRoom = CreateCustomRoom(
            roomsRoot, 1, DungeonRoom.RoomType.Sanctuary,
            new Rect(-19f, -13f, 38f, 26f),
            "THUNG LŨNG TÀN TÍCH", "VALLEY RUIN SANCTUARY",
            "Đi xuống Nhánh Nam (Đông Quái -> Trùm Gor'Mok) hoặc sang Nhánh Đông (Ít Quái -> Bóng Ma)!",
            "Head South (Swarm -> Boss Gor'Mok) or East (Fast Path -> Shadow Boss)!",
            null, null, null, null
        );
        hubRoom.isEntered = true;
        hubRoom.isCleared = true;
        mgr.rooms.Add(hubRoom);
        mgr.currentRoom = hubRoom;

        // --- LỐI ĐI 1: NHÁNH NAM (-Y) — ĐÔNG QUÁI -> ĐẠI TRÙM GỐC MAP 2: GOR'MOK ---
        var bS1_in  = CreateBarrier(barriersRoot, new Vector3(0f, -20f, 0f), false, 8.2f, "Barrier_S1_In");
        var bS1_out = CreateBarrier(barriersRoot, new Vector3(0f, -36f, 0f), false, 8.2f, "Barrier_S1_Out");
        var bS2_in  = CreateBarrier(barriersRoot, new Vector3(0f, -42f, 0f), false, 8.2f, "Barrier_S2_Boss");

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 2, DungeonRoom.RoomType.SunkenHall,
            new Rect(-12f, -36f, 24f, 16f),
            "SẢNH THUNG LŨNG NAM (ĐÔNG QUÁI)", "SOUTH VALLEY HALL (SWARM)",
            "Tiêu diệt 2 đợt bầy Huyết Thú & Hoàng Thú để mở cửa Huyết Điện!",
            "Purge 2 waves of Blood Monsters & Valley Beasts!",
            bS1_in, bS1_out, bS2_in,
            r => StartCoroutine(RunMap2CombatRoomRoutine(r, RouteDensity.SwarmManyMonsters))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 3, DungeonRoom.RoomType.TitanSanctum,
            new Rect(-14f, -64f, 28f, 22f),
            "HUYẾT ĐIỆN ĐẠI TRÙM GOR'MOK", "SANCTUM OF OVERLORD GOR'MOK",
            "ĐẠI CHIẾN BOSS MAP 2: GOR'MOK - HUYẾT THÚ CỔ THỤ!",
            "MAP 2 BOSS BATTLE: GOR'MOK THE ANCIENT BLOOD BEAST!",
            bS2_in, null, null,
            r => StartCoroutine(RunBossRoomRoutine(r, RouteBossType.GorMokBloodLord, isMap3: false))
        ));

        // --- LỐI ĐI 2: NHÁNH ĐÔNG (+X) — ÍT QUÁI TINH ANH -> ĐẠI TRÙM BÓNG MA BẢN NGÃ ---
        var bE1_in  = CreateBarrier(barriersRoot, new Vector3(26f, 0f, 0f), true, 8.2f, "Barrier_E1_In");
        var bE1_out = CreateBarrier(barriersRoot, new Vector3(44f, 0f, 0f), true, 8.2f, "Barrier_E1_Out");
        var bE2_in  = CreateBarrier(barriersRoot, new Vector3(50f, 0f, 0f), true, 8.2f, "Barrier_E2_Boss");

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 4, DungeonRoom.RoomType.CursedCrypt,
            new Rect(26f, -10f, 18f, 20f),
            "HÀNH LANG ĐÔNG (ÍT QUÁI - ĐƯỜNG NHANH)", "EAST HALL (LOW MONSTERS - FAST PATH)",
            "Hạ gục toán hộ vệ tinh nhuệ để tiến thẳng vào Điện Thờ!",
            "Defeat the elite guard squad to reach the Boss Sanctum!",
            bE1_in, bE1_out, bE2_in,
            r => StartCoroutine(RunMap2CombatRoomRoutine(r, RouteDensity.EliteFewMonsters))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 5, DungeonRoom.RoomType.TitanSanctum,
            new Rect(50f, -12f, 24f, 24f),
            "ĐIỆN THỜ BÓNG MA & HUYẾT THÚ", "SHADOW & BLOOD SANCTUM",
            "ĐẠI CHIẾN TRÙM NHÁNH ĐÔNG: BÓNG MA BẢN NGÃ!",
            "EAST ROUTE BOSS: SHADOW DOPPELGANGER!",
            bE2_in, null, null,
            r => StartCoroutine(RunBossRoomRoutine(r, RouteBossType.ShadowDoppelganger, isMap3: false))
        ));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 2. MAP 3: KÝ ỨC LÃNG QUÊN GỌN GÀNG (NHÁNH BẮC & NHÁNH ĐÔNG)
    // ═══════════════════════════════════════════════════════════════════════════
    private void BuildMap3CompactIslands()
    {
        var baseFloorObj = GameObject.Find("ArenaFloor_Mossy");
        Sprite mossFloorSprite = null;
        Material floorMat = null;
        if (baseFloorObj != null)
        {
            var sr = baseFloorObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                mossFloorSprite = sr.sprite;
                floorMat = sr.sharedMaterial;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(32f, 22f);
            }
        }

        // Gỡ bỏ tường kín 4 phía cũ của đảo trung tâm
        var oldWalls = GameObject.Find("ArenaWalls");
        if (oldWalls != null) Destroy(oldWalls);

        // Dọn dẹp toàn bộ đá, gốc cây chắn đường và tắt Collider vô hình trên đảo Map 3
        CleanUpObstaclesAndOversizedCollidersMap3();

        Transform map3ExpRoot = new GameObject("Map3_MultiPathIslands").transform;
        map3ExpRoot.SetParent(transform, false);

        // Đảo Trung Tâm (Hub): x [-16..16), y [-11..11)
        AddWalkableRect(-16, -11, 32, 22);

        // --- LỐI ĐI 1: NHÁNH BẮC (+Y) — ĐÔNG QUÁI -> BOSS GỐC MAP 3: CHÚA TỂ NHỆN CỔ ĐẠI ---
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_N1", -4, 11, 8, 7);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Island_N1", -13, 18, 26, 16);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_N2", -4, 34, 8, 6);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "BossIsland_North", -15, 40, 30, 22);

        // --- LỐI ĐI 2: NHÁNH ĐÔNG (+X) — ÍT QUÁI TINH ANH -> ĐẠI TRÙM BÓNG MA BẢN NGÃ ---
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_E1", 16, -4, 7, 8);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Island_E1", 23, -10, 18, 20);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_E2", 41, -4, 6, 8);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "BossIsland_East", 47, -12, 24, 24);

        BuildPerimeterWallsFromWalkableRects(map3ExpRoot);
        SetupMap3RoomsAndBarriers();
    }

    private void CreateIslandFloor(Transform parent, Sprite sprite, Material mat, string name, int xMin, int yMin, int width, int height)
    {
        AddWalkableRect(xMin, yMin, width, height);

        GameObject floorObj = new GameObject($"ArenaFloor_{name}");
        floorObj.transform.SetParent(parent, false);
        floorObj.transform.position = new Vector3(xMin + width * 0.5f, yMin + height * 0.5f, 0f);

        var sr = floorObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (mat != null) sr.sharedMaterial = mat;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(width, height);
        sr.sortingOrder = -500;
        sr.color = Color.white;
    }

    private void CleanUpObstaclesAndOversizedCollidersMap3()
    {
        var envRoot = GameObject.Find("Map3_ForgottenMemories_Environment");
        var decoRoot = GameObject.Find("NatureDecorations");

        Transform[] roots = new Transform[]
        {
            envRoot != null ? envRoot.transform : null,
            decoRoot != null ? decoRoot.transform : null
        };

        foreach (var r in roots)
        {
            if (r == null) continue;
            var allChildren = r.GetComponentsInChildren<Transform>(true);
            foreach (var ch in allChildren)
            {
                if (ch == null || ch == r) continue;
                string n = ch.name;

                // Xóa toàn bộ gốc cây, tảng đá và cây chắn cửa Bắc / Đông
                if (n.Contains("Stump_") || n.Contains("RockLarge_") || n.Contains("RockSmall_") ||
                    n.Contains("PineTree_North") || n.Contains("TealTree_East"))
                {
                    Destroy(ch.gameObject);
                    continue;
                }

                // Tắt toàn bộ Collider2D trên các cây trang trí ở góc đảo để không bao giờ gây kẹt đường
                var cols = ch.GetComponents<Collider2D>();
                foreach (var c in cols)
                {
                    c.enabled = false;
                }
            }
        }
    }

    private void BuildPerimeterWallsFromWalkableRects(Transform parent)
    {
        Transform wallGroup = new GameObject("Map3_PerimeterWalls").transform;
        wallGroup.SetParent(parent, false);

        HashSet<Vector2Int> landCells = new HashSet<Vector2Int>();
        foreach (var r in walkableRects)
        {
            for (int x = r.xMin; x < r.xMax; x++)
            {
                for (int y = r.yMin; y < r.yMax; y++)
                {
                    landCells.Add(new Vector2Int(x, y));
                }
            }
        }

        Dictionary<int, List<int>> northByY = new Dictionary<int, List<int>>();
        Dictionary<int, List<int>> southByY = new Dictionary<int, List<int>>();
        Dictionary<int, List<int>> eastByX  = new Dictionary<int, List<int>>();
        Dictionary<int, List<int>> westByX  = new Dictionary<int, List<int>>();

        foreach (var c in landCells)
        {
            if (!landCells.Contains(new Vector2Int(c.x, c.y + 1)))
            {
                if (!northByY.TryGetValue(c.y + 1, out var list)) northByY[c.y + 1] = list = new List<int>();
                list.Add(c.x);
            }
            if (!landCells.Contains(new Vector2Int(c.x, c.y - 1)))
            {
                if (!southByY.TryGetValue(c.y, out var list)) southByY[c.y] = list = new List<int>();
                list.Add(c.x);
            }
            if (!landCells.Contains(new Vector2Int(c.x + 1, c.y)))
            {
                if (!eastByX.TryGetValue(c.x + 1, out var list)) eastByX[c.x + 1] = list = new List<int>();
                list.Add(c.y);
            }
            if (!landCells.Contains(new Vector2Int(c.x - 1, c.y)))
            {
                if (!westByX.TryGetValue(c.x, out var list)) westByX[c.x] = list = new List<int>();
                list.Add(c.y);
            }
        }

        float thickness = 1.6f;
        CreateHorizontalWalls(wallGroup, northByY, +thickness * 0.5f, thickness);
        CreateHorizontalWalls(wallGroup, southByY, -thickness * 0.5f, thickness);
        CreateVerticalWalls(wallGroup, eastByX, +thickness * 0.5f, thickness);
        CreateVerticalWalls(wallGroup, westByX, -thickness * 0.5f, thickness);
    }

    private void CreateHorizontalWalls(Transform parent, Dictionary<int, List<int>> dict, float yOffset, float thickness)
    {
        foreach (var kvp in dict)
        {
            int y = kvp.Key;
            List<int> xs = kvp.Value;
            xs.Sort();
            int start = xs[0], prev = xs[0];
            for (int i = 1; i < xs.Count; i++)
            {
                if (xs[i] == prev + 1) prev = xs[i];
                else
                {
                    SpawnWallBox(parent, new Vector2((start + prev + 1) * 0.5f, y + yOffset), new Vector2((prev + 1 - start), thickness));
                    start = prev = xs[i];
                }
            }
            SpawnWallBox(parent, new Vector2((start + prev + 1) * 0.5f, y + yOffset), new Vector2((prev + 1 - start), thickness));
        }
    }

    private void CreateVerticalWalls(Transform parent, Dictionary<int, List<int>> dict, float xOffset, float thickness)
    {
        foreach (var kvp in dict)
        {
            int x = kvp.Key;
            List<int> ys = kvp.Value;
            ys.Sort();
            int start = ys[0], prev = ys[0];
            for (int i = 1; i < ys.Count; i++)
            {
                if (ys[i] == prev + 1) prev = ys[i];
                else
                {
                    SpawnWallBox(parent, new Vector2(x + xOffset, (start + prev + 1) * 0.5f), new Vector2(thickness, (prev + 1 - start)));
                    start = prev = ys[i];
                }
            }
            SpawnWallBox(parent, new Vector2(x + xOffset, (start + prev + 1) * 0.5f), new Vector2(thickness, (prev + 1 - start)));
        }
    }

    private void SpawnWallBox(Transform parent, Vector2 center, Vector2 size)
    {
        GameObject w = new GameObject("IslandBoundaryWall");
        w.transform.SetParent(parent, false);
        w.transform.position = new Vector3(center.x, center.y, 0f);
        var box = w.AddComponent<BoxCollider2D>();
        box.size = size;
    }

    private void SetupMap3RoomsAndBarriers()
    {
        Transform barriersRoot = new GameObject("Map3_MultiPathBarriers").transform;
        barriersRoot.SetParent(transform, false);

        Transform roomsRoot = new GameObject("DungeonRooms").transform;
        roomsRoot.SetParent(transform, false);

        var mgr = roomsRoot.gameObject.AddComponent<DungeonRoomManager>();
        mgr.rooms.Clear();

        // Phòng 1: Đảo Cổ Trung Tâm
        var hubRoom = CreateCustomRoom(
            roomsRoot, 1, DungeonRoom.RoomType.Sanctuary,
            new Rect(-16f, -11f, 32f, 22f),
            "ĐẢO CỔ KÝ ỨC", "FORGOTTEN ISLAND SANCTUARY",
            "Đi lên Cầu Bắc (Đông Quái -> Chúa Tể Nhện) hoặc sang Cầu Đông (Ít Quái -> Bóng Ma)!",
            "Head North (Swarm -> Spider Lord) or East (Fast Path -> Shadow Boss)!",
            null, null, null, null
        );
        hubRoom.isEntered = true;
        hubRoom.isCleared = true;
        mgr.rooms.Add(hubRoom);
        mgr.currentRoom = hubRoom;

        // --- LỐI ĐI 1: NHÁNH BẮC (+Y) — ĐÔNG QUÁI -> ĐẠI TRÙM GỐC MAP 3: CHÚA TỂ NHỆN CỔ ĐẠI ---
        var bN1_in  = CreateBarrier(barriersRoot, new Vector3(0f, 18f, 0f), false, 8.2f, "Barrier_N1_In");
        var bN1_out = CreateBarrier(barriersRoot, new Vector3(0f, 34f, 0f), false, 8.2f, "Barrier_N1_Out");
        var bN2_in  = CreateBarrier(barriersRoot, new Vector3(0f, 40f, 0f), false, 8.2f, "Barrier_N2_Boss");

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 2, DungeonRoom.RoomType.SunkenHall,
            new Rect(-13f, 18f, 26f, 16f),
            "ĐẢO TIỀN ĐỒN BẮC (ĐÔNG QUÁI)", "NORTH OUTPOST ISLE (SWARM)",
            "Càn quét 2 đợt bầy Yêu Tinh, Quái Nhớt và Bộ Xương Cổ Đại!",
            "Purge 2 waves of Goblins, Slimes, and Ancient Skeletons!",
            bN1_in, bN1_out, bN2_in,
            r => StartCoroutine(RunMap3CombatRoomRoutine(r, RouteDensity.SwarmManyMonsters))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 3, DungeonRoom.RoomType.TitanSanctum,
            new Rect(-15f, 40f, 30f, 22f),
            "THÁNH ĐẢO CHÚA TỂ NHỆN CỔ ĐẠI", "SANCTUM OF THE ANCIENT SPIDER LORD",
            "PHÁ VỠ PHONG ẤN TRỨNG KÉN ĐỂ TRIỆU HỒI CHÚA TỂ NHỆN CỔ ĐẠI!",
            "SHATTER THE COCOON SEAL TO AWAKEN THE ANCIENT SPIDER LORD!",
            bN2_in, null, null,
            r => StartCoroutine(RunBossRoomRoutine(r, RouteBossType.AncientSpiderKing, isMap3: true))
        ));

        // --- LỐI ĐI 2: NHÁNH ĐÔNG (+X) — ÍT QUÁI TINH ANH -> ĐẠI TRÙM BÓNG MA BẢN NGÃ ---
        var bE1_in  = CreateBarrier(barriersRoot, new Vector3(23f, 0f, 0f), true, 8.2f, "Barrier_M3_E1_In");
        var bE1_out = CreateBarrier(barriersRoot, new Vector3(41f, 0f, 0f), true, 8.2f, "Barrier_M3_E1_Out");
        var bE2_in  = CreateBarrier(barriersRoot, new Vector3(47f, 0f, 0f), true, 8.2f, "Barrier_M3_E2_Boss");

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 4, DungeonRoom.RoomType.CursedCrypt,
            new Rect(23f, -10f, 18f, 20f),
            "ĐẢO ĐÔNG (ÍT QUÁI - ĐƯỜNG NHANH)", "EAST ISLE (LOW MONSTERS - FAST PATH)",
            "Hạ gục nhanh kỵ sĩ xương tinh nhuệ để tiến vào Đảo Trùm!",
            "Swiftly slay the elite bone knights to enter the Boss Isle!",
            bE1_in, bE1_out, bE2_in,
            r => StartCoroutine(RunMap3CombatRoomRoutine(r, RouteDensity.EliteFewMonsters))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 5, DungeonRoom.RoomType.TitanSanctum,
            new Rect(47f, -12f, 24f, 24f),
            "ĐẢO BÓNG MA BẢN NGÃ", "SHADOW DOPPELGANGER ISLE",
            "ĐẠI CHIẾN TRÙM NHÁNH ĐÔNG: BÓNG MA BẢN NGÃ TỐI THƯỢNG!",
            "EAST ROUTE BOSS: SUPREME SHADOW DOPPELGANGER!",
            bE2_in, null, null,
            r => StartCoroutine(RunBossRoomRoutine(r, RouteBossType.ShadowDoppelganger, isMap3: true))
        ));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 3. ĐIỀU PHỐI ĐỢT QUÁI & ĐẠI TRÙM GỐC (KHÔNG DÙNG BOSS CÂY / QUÁI ĐÁ)
    // ═══════════════════════════════════════════════════════════════════════════
    private IEnumerator RunMap2CombatRoomRoutine(DungeonRoom room, RouteDensity density)
    {
        yield return new WaitForSeconds(0.3f);
        if (room.entranceBarrier != null) room.entranceBarrier.Lock();
        if (room.exitBarrier != null) room.exitBarrier.Lock();

        var waveUI = FindAnyObjectByType<WaveUI>();
        int totalWaves = (density == RouteDensity.EliteFewMonsters) ? 1 : 2;

        for (int w = 1; w <= totalWaves; w++)
        {
            activeRoomMonsters.Clear();
            isSpawningRoomWave = true;

            int bloodCount = (density == RouteDensity.SwarmManyMonsters) ? (3 + w) : 2;
            int beastCount = (density == RouteDensity.SwarmManyMonsters) ? (1 + w) : 1;
            int totalCount = bloodCount + beastCount;

            if (waveUI != null)
            {
                waveUI.ShowBanner(
                    room.GetLocalizedTitle(),
                    LocalizationManager.T($"Đợt {w} / {totalWaves} — Tiêu diệt {totalCount} quái vật!", $"Wave {w} / {totalWaves} — Slay {totalCount} monsters!"),
                    2.0f
                );
            }

            int spawnIdx = 0;
            for (int i = 0; i < bloodCount; i++)
            {
                Vector3 pos = GetSafeRoomSpawnPos(room, spawnIdx++, totalCount);
                var bm = BloodMonsterAI.Spawn(pos, isBoss: false, index: i + 1);
                if (bm != null) activeRoomMonsters.Add(bm.gameObject);
                yield return new WaitForSeconds(0.15f);
            }
            for (int i = 0; i < beastCount; i++)
            {
                Vector3 pos = GetSafeRoomSpawnPos(room, spawnIdx++, totalCount);
                var vb = ValleyBeastAI.Spawn(pos, i + 1);
                if (vb != null) activeRoomMonsters.Add(vb.gameObject);
                yield return new WaitForSeconds(0.15f);
            }

            isSpawningRoomWave = false;
            yield return StartCoroutine(WaitUntilActiveMonstersCleared());

            if (w < totalWaves)
            {
                AethelShard.SpawnShards(room.roomCenter, 12);
                yield return new WaitForSeconds(1.4f);
            }
        }

        CompleteCombatRoom(room, density);
    }

    private IEnumerator RunMap3CombatRoomRoutine(DungeonRoom room, RouteDensity density)
    {
        yield return new WaitForSeconds(0.3f);
        if (room.entranceBarrier != null) room.entranceBarrier.Lock();
        if (room.exitBarrier != null) room.exitBarrier.Lock();

        var waveUI = FindAnyObjectByType<WaveUI>();
        int totalWaves = (density == RouteDensity.EliteFewMonsters) ? 1 : 2;

        for (int w = 1; w <= totalWaves; w++)
        {
            activeRoomMonsters.Clear();
            isSpawningRoomWave = true;

            int goblinCount = (density == RouteDensity.SwarmManyMonsters) ? (3 + w) : 1;
            int skelCount   = (density == RouteDensity.SwarmManyMonsters) ? (2 + w) : 2;
            int slimeCount  = (density == RouteDensity.SwarmManyMonsters) ? 2 : 0;
            int totalCount  = goblinCount + skelCount + slimeCount;

            if (waveUI != null)
            {
                waveUI.ShowBanner(
                    room.GetLocalizedTitle(),
                    LocalizationManager.T($"Đợt {w} / {totalWaves} — Tiêu diệt {totalCount} quái vật!", $"Wave {w} / {totalWaves} — Slay {totalCount} monsters!"),
                    2.0f
                );
            }

            int spawnIdx = 0;
            for (int i = 0; i < skelCount; i++)
            {
                Vector3 pos = GetSafeRoomSpawnPos(room, spawnIdx++, totalCount);
                GameObject s = Map3WaveSpawner.SpawnSkeletonStatic(pos);
                if (s != null) activeRoomMonsters.Add(s);
                yield return new WaitForSeconds(0.15f);
            }
            for (int i = 0; i < goblinCount; i++)
            {
                Vector3 pos = GetSafeRoomSpawnPos(room, spawnIdx++, totalCount);
                GameObject g = Map3WaveSpawner.SpawnGoblinStatic(pos);
                if (g != null) activeRoomMonsters.Add(g);
                yield return new WaitForSeconds(0.15f);
            }
            for (int i = 0; i < slimeCount; i++)
            {
                Vector3 pos = GetSafeRoomSpawnPos(room, spawnIdx++, totalCount);
                GameObject sl = Map3WaveSpawner.SpawnSlimeStatic(pos);
                if (sl != null) activeRoomMonsters.Add(sl);
                yield return new WaitForSeconds(0.15f);
            }

            isSpawningRoomWave = false;
            yield return StartCoroutine(WaitUntilActiveMonstersCleared());

            if (w < totalWaves)
            {
                AethelShard.SpawnShards(room.roomCenter, 12);
                yield return new WaitForSeconds(1.4f);
            }
        }

        CompleteCombatRoom(room, density);
    }

    private IEnumerator RunBossRoomRoutine(DungeonRoom room, RouteBossType bossType, bool isMap3)
    {
        yield return new WaitForSeconds(0.3f);
        if (room.entranceBarrier != null) room.entranceBarrier.Lock();
        if (room.exitBarrier != null) room.exitBarrier.Lock();

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.45f, 0.55f);

        yield return new WaitForSeconds(0.75f);

        activeRoomMonsters.Clear();
        isSpawningRoomWave = true;

        Vector3 bossSpawnPos = room.roomCenter;
        bool bossDefeated = false;

        if (bossType == RouteBossType.GorMokBloodLord)
        {
            var boss = BloodMonsterAI.Spawn(bossSpawnPos, isBoss: true, index: 1);
            if (boss != null)
            {
                activeRoomMonsters.Add(boss.gameObject);
                BossIntroCutsceneUI.PlayGorMok(boss.transform);
            }
            isSpawningRoomWave = false;
            yield return StartCoroutine(WaitUntilActiveMonstersCleared());
            bossDefeated = true;
        }
        else if (bossType == RouteBossType.AncientSpiderKing)
        {
            // Giữ nguyên cơ chế gốc của Map 3: Sinh Phong Ấn Trứng Kén (BoneEggCocoon) trước, đập vỡ kén sẽ triệu hồi Chúa Tể Nhện!
            bool eggHatched = false;
            Vector3 hatchPos = bossSpawnPos;
            System.Action<Vector3> onHatched = (pos) =>
            {
                eggHatched = true;
                hatchPos = pos;
            };

            BoneEggCocoon.OnEggHatched += onHatched;
            GameObject cocoon = Map3WaveSpawner.SpawnBoneEggCocoonStatic(bossSpawnPos);

            while (!eggHatched && cocoon != null)
            {
                yield return new WaitForSeconds(0.2f);
            }
            BoneEggCocoon.OnEggHatched -= onHatched;

            DamagePopup.CreateText(hatchPos + Vector3.up * 2.2f, LocalizationManager.T("CHÚA TỂ NHỆN THỨC TỈNH!", "SPIDER LORD AWAKENS!"), Color.red);
            var skBoss = Map3WaveSpawner.SpawnSkeletonKingStatic(hatchPos);
            if (skBoss != null) activeRoomMonsters.Add(skBoss.gameObject);

            isSpawningRoomWave = false;
            yield return StartCoroutine(WaitUntilActiveMonstersCleared());
            bossDefeated = true;
        }
        else if (bossType == RouteBossType.ShadowDoppelganger)
        {
            var doppel = ShadowDoppelgangerBoss.Spawn(bossSpawnPos, () =>
            {
                bossDefeated = true;
            });
            if (doppel != null) activeRoomMonsters.Add(doppel.gameObject);
            isSpawningRoomWave = false;
            while (!bossDefeated && doppel != null && !doppel.IsDead)
            {
                yield return new WaitForSeconds(0.25f);
            }
            bossDefeated = true;
        }

        if (bossDefeated)
        {
            room.OnRoomCleared();
            AethelShard.SpawnShards(room.roomCenter, 30);
            HealthPotion.Spawn(room.roomCenter + new Vector3(-1.2f, -0.5f, 0f), 1);
            HealthPotion.Spawn(room.roomCenter + new Vector3(1.2f, -0.5f, 0f), 1);
            WorldItemPickup.RollDrop(room.roomCenter, true);

            var waveUI = FindAnyObjectByType<WaveUI>();
            if (waveUI != null)
            {
                waveUI.ShowBanner(
                    LocalizationManager.T("ĐẠI TRÙM ĐÃ BỊ TIÊU DIỆT!", "ANCIENT BOSS DEFEATED!"),
                    LocalizationManager.T("Cổng Không Gian và Thương Nhân đã xuất hiện!", "The Ruin Portal and Merchant have appeared!"),
                    3.5f
                );
            }

            if (bossType != RouteBossType.AncientSpiderKing)
            {
                yield return new WaitForSeconds(1.2f);
                Vector3 portalPos = room.roomCenter + new Vector3(0f, 2.0f, 0f);
                RuinPortal.SpawnPortal(portalPos);
                if (isMap3)
                {
                    CorruptedVoidPortal.Spawn(room.roomCenter + new Vector3(3.0f, 2.0f, 0f));
                }
                WanderingMerchant.SpawnMerchant(room.roomCenter + new Vector3(-3.0f, 1.6f, 0f));
            }
        }
    }

    private IEnumerator WaitUntilActiveMonstersCleared()
    {
        while (isSpawningRoomWave || activeRoomMonsters.Count > 0)
        {
            activeRoomMonsters.RemoveWhere(item => item == null);
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void CompleteCombatRoom(DungeonRoom room, RouteDensity density)
    {
        if (room == null || room.isCleared) return;
        room.OnRoomCleared();

        int rewardShards = density == RouteDensity.SwarmManyMonsters ? 18 : 12;
        AethelShard.SpawnShards(room.roomCenter, rewardShards);
        HealthPotion.Spawn(room.roomCenter + new Vector3(0f, -0.5f, 0f), 1);
        WorldItemPickup.RollDrop(room.roomCenter + new Vector3(0.6f, 0f, 0f), false, 0.35f);

        var waveUI = FindAnyObjectByType<WaveUI>();
        if (waveUI != null)
        {
            waveUI.ShowBanner(
                LocalizationManager.T("KHU VỰC ĐÃ DỌN SẠCH!", "CHAMBER CLEARED!"),
                LocalizationManager.T($"Cổng phong ấn đã mở! (+{rewardShards} Mảnh Linh Hồn)", $"Barrier unlocked! (+{rewardShards} Shards)"),
                2.2f
            );
        }
    }

    private void AddWalkableRect(int xMin, int yMin, int width, int height)
    {
        walkableRects.Add(new RectInt(xMin, yMin, width, height));
    }

    private Vector3 GetSafeRoomSpawnPos(DungeonRoom room, int index, int total)
    {
        Vector3 c = room.roomCenter;
        float rx = Mathf.Max(2.2f, room.roomBounds.width * 0.5f - 3.0f);
        float ry = Mathf.Max(2.2f, room.roomBounds.height * 0.5f - 3.0f);

        float angle = (index / (float)Mathf.Max(1, total)) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
        float distRatio = Random.Range(0.42f, 0.78f);
        return c + new Vector3(Mathf.Cos(angle) * rx * distRatio, Mathf.Sin(angle) * ry * distRatio, 0f);
    }

    private RoomBarrier CreateBarrier(Transform parent, Vector3 pos, bool vertical, float length, string name)
    {
        var b = RoomBarrier.Create(pos, vertical, length, name);
        b.transform.SetParent(parent, true);
        return b;
    }

    private DungeonRoom CreateCustomRoom(
        Transform parent, int index, DungeonRoom.RoomType type, Rect bounds,
        string titleVN, string titleEN, string subVN, string subEN,
        RoomBarrier entrance, RoomBarrier exit, RoomBarrier nextEntrance,
        System.Action<DungeonRoom> onEnter)
    {
        GameObject rObj = new GameObject($"Room_{index}_{titleEN}");
        rObj.transform.SetParent(parent, false);
        rObj.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0f);

        var r = rObj.AddComponent<DungeonRoom>();
        r.roomIndex = index;
        r.roomType = type;
        r.roomTitle = titleVN;
        r.roomSubtitle = subVN;
        r.customTitleVN = titleVN;
        r.customTitleEN = titleEN;
        r.customSubtitleVN = subVN;
        r.customSubtitleEN = subEN;
        r.roomBounds = bounds;
        r.roomCenter = new Vector3(bounds.center.x, bounds.center.y, 0f);
        r.entranceBarrier = entrance;
        r.exitBarrier = exit;
        r.nextEntranceBarrier = nextEntrance;
        r.customEnterHandler = onEnter;
        return r;
    }
}
