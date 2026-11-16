using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class DungeonMapBuilder : MonoBehaviour
{
    [ContextMenu("Build Full Multi-Room Dungeon")]
    public static void BuildDungeon()
    {
        if (PvPMatchManager.IsPvPModeActive)
        {
            BuildPvPColosseumArena();
            return;
        }

        Debug.Log("<color=cyan>[MAP BUILDER]</color> Bắt đầu kiến thiết Bản Đồ Đa Phòng (Realm 1: Sunken Sepulcher)...");

        // 1. Tìm Tilemaps Ground & Walls
        Tilemap groundTm = null;
        Tilemap wallsTm = null;

        var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (var tm in tilemaps)
        {
            if (tm.gameObject.name.ToLower().Contains("ground") || tm.gameObject.name.ToLower().Contains("floor"))
            {
                groundTm = tm;
            }
            else if (tm.gameObject.name.ToLower().Contains("wall"))
            {
                wallsTm = tm;
            }
        }

        if (groundTm == null || wallsTm == null)
        {
            Debug.LogWarning("[MAP BUILDER] Bỏ qua vì không tìm thấy Tilemap Ground hoặc Walls trong Scene hiện tại.");
            return;
        }

        // 2. Kiểm tra xem Tilemap đã có sẵn gạch trong Scene hay chưa
        int existingGroundCount = groundTm.GetUsedTilesCount();
        if (existingGroundCount > 0)
        {
            Debug.Log($"<color=cyan>[MAP BUILDER]</color> Bản đồ đã có sẵn {existingGroundCount} ô gạch trong Scene. Giữ nguyên 100% không xóa!");
        }
        else
        {
            // 2b. Chỉ tái tạo gạch khi Scene hoàn toàn chưa có gạch
            TileBase floorTile = null;
            TileBase wallTile = null;

#if UNITY_EDITOR
            floorTile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Palettes/TX Tileset Stone Ground_24.asset");
            wallTile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Palettes/TX Tileset Stone Ground_43.asset");
#endif

            if (floorTile == null) floorTile = groundTm.GetTile(new Vector3Int(0, 0, 0));
            if (wallTile == null) wallTile = wallsTm.GetTile(new Vector3Int(-14, 0, 0));

            if (floorTile != null && wallTile != null)
            {
                // 3. Clear toàn bộ tiles cũ
                groundTm.ClearAllTiles();
                wallsTm.ClearAllTiles();

                // 4. Tính toán toàn bộ ô sàn (Floor) và ô tường (Wall)
                HashSet<Vector2Int> floorSet = new HashSet<Vector2Int>();

                void AddRect(int x1, int y1, int x2, int y2)
                {
                    for (int x = x1; x <= x2; x++)
                    {
                        for (int y = y1; y <= y2; y++)
                        {
                            floorSet.Add(new Vector2Int(x, y));
                        }
                    }
                }

                // Phòng 1: Sanctuary
                AddRect(-16, -6, -2, 6);
                // Hành lang 1: Corridor 1 (Đông)
                AddRect(-1, -1, 7, 1);
                // Phòng 2: Sunken Hall
                AddRect(8, -8, 30, 12);
                // Hành lang 2: Corridor 2 (Bắc)
                AddRect(18, 13, 20, 23);
                // Phòng 3: Cursed Crypt
                AddRect(8, 24, 30, 44);
                // Hành lang 3: Corridor 3 (Đông)
                AddRect(31, 33, 41, 35);
                // Phòng 4: Sanctum of the Titan (Boss Room)
                AddRect(42, 20, 70, 48);

                // Đặt toàn bộ gạch sàn (Ground)
                foreach (var pos in floorSet)
                {
                    groundTm.SetTile(new Vector3Int(pos.x, pos.y, 0), floorTile);
                }

                // Tính toán vành tường kín dày 2-3 ô (Wall Perimeter) ngăn hoàn toàn việc đi xuyên tường
                HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>();
                foreach (var pos in floorSet)
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            Vector2Int neighbor = new Vector2Int(pos.x + dx, pos.y + dy);
                            if (!floorSet.Contains(neighbor))
                            {
                                wallSet.Add(neighbor);
                            }
                        }
                    }
                }

                // Đặt toàn bộ gạch tường (Walls)
                foreach (var pos in wallSet)
                {
                    wallsTm.SetTile(new Vector3Int(pos.x, pos.y, 0), wallTile);
                }

                groundTm.RefreshAllTiles();
                wallsTm.RefreshAllTiles();
            }
            else
            {
                Debug.LogWarning("[MAP BUILDER] floorTile hoặc wallTile null, an toàn bảo toàn Scene không bị xóa trắng!");
            }
        }

        // Cấu hình Sorting Layer và Collider vững chắc cho Walls & Ground
        var wallRend = wallsTm.GetComponent<TilemapRenderer>();
        if (wallRend != null)
        {
            wallRend.sortingLayerName = "Default";
            wallRend.sortingOrder = 2;
        }

        var groundRend = groundTm.GetComponent<TilemapRenderer>();
        if (groundRend != null)
        {
            groundRend.sortingLayerName = "Default";
            groundRend.sortingOrder = -10;
        }

        // Đảm bảo TilemapCollider2D tạo box collider đặc cho từng ô gạch tường, không dùng outline rỗng
        var wallCol = wallsTm.GetComponent<TilemapCollider2D>();
        if (wallCol == null) wallCol = wallsTm.gameObject.AddComponent<TilemapCollider2D>();
        wallCol.compositeOperation = Collider2D.CompositeOperation.None;

        var compCol = wallsTm.GetComponent<CompositeCollider2D>();
        if (compCol != null)
        {
            Object.DestroyImmediate(compCol);
        }

        // 5. Tạo Cửa Phong Ấn (RoomBarriers)
        Transform barriersRoot = GetOrCreateRoot("DungeonBarriers");
        ClearChildren(barriersRoot);

        // Barrier 1: Cửa vào Sảnh 2 (x = 8, y = 0.5, cao 3 tiles: [-1.0, 2.0])
        var b1 = RoomBarrier.Create(new Vector3(8f, 0.5f, 0f), true, 3.2f, "Barrier_Room2_Entrance");
        b1.transform.SetParent(barriersRoot);

        // Barrier 2 (Exit Room 2): Cửa ra Sảnh 2 vào Hành lang 2 (x = 19.5, y = 13, rộng 3 tiles)
        var b2_exit = RoomBarrier.Create(new Vector3(19.5f, 13f, 0f), false, 3.2f, "Barrier_Room2_Exit");
        b2_exit.transform.SetParent(barriersRoot);
        b2_exit.Lock();

        // Barrier 3 (Entrance Room 3): Cửa vào Hầm 3 (x = 19.5, y = 24, rộng 3 tiles)
        var b3_entrance = RoomBarrier.Create(new Vector3(19.5f, 24f, 0f), false, 3.2f, "Barrier_Room3_Entrance");
        b3_entrance.transform.SetParent(barriersRoot);
        b3_entrance.Lock();

        // Barrier 4 (Exit Room 3): Cửa ra Hầm 3 vào Hành lang 3 (x = 31, y = 34.5, cao 3 tiles)
        var b3_exit = RoomBarrier.Create(new Vector3(31f, 34.5f, 0f), true, 3.2f, "Barrier_Room3_Exit");
        b3_exit.transform.SetParent(barriersRoot);
        b3_exit.Lock();

        // Barrier 5 (Entrance Room 4): Cửa vào Đại Điện Boss 4 (x = 42, y = 34.5, cao 3 tiles)
        var b4_entrance = RoomBarrier.Create(new Vector3(42f, 34.5f, 0f), true, 3.2f, "Barrier_Room4_Boss");
        b4_entrance.transform.SetParent(barriersRoot);
        b4_entrance.Lock();

        // 6. Đặt Đuốc Tường (Torches with Light2D)
        Transform torchesRoot = GetOrCreateRoot("DungeonTorches");
        ClearChildren(torchesRoot);

        List<Vector3> torchPositions = new List<Vector3>()
        {
            // Room 1
            new Vector3(-14, 6, 0), new Vector3(-4, 6, 0),
            // Corridor 1
            new Vector3(1, 2, 0), new Vector3(5, 2, 0),
            // Room 2
            new Vector3(10, 12, 0), new Vector3(28, 12, 0), new Vector3(10, -8, 0), new Vector3(28, -8, 0),
            // Corridor 2
            new Vector3(17, 16, 0), new Vector3(21, 20, 0),
            // Room 3
            new Vector3(10, 44, 0), new Vector3(28, 44, 0), new Vector3(10, 24, 0), new Vector3(28, 24, 0),
            // Corridor 3
            new Vector3(34, 36, 0), new Vector3(38, 36, 0),
            // Room 4 (Boss)
            new Vector3(45, 47, 0), new Vector3(67, 47, 0), new Vector3(45, 21, 0), new Vector3(67, 21, 0)
        };

        foreach (var pos in torchPositions)
        {
            SpawnTorch(pos, torchesRoot);
        }

        // 7. Tạo & Cấu hình DungeonRoomManager
        SetupDungeonRooms(b1, b2_exit, b3_entrance, b3_exit, b4_entrance);

        // 8. Đặt lại vị trí Người chơi, Bàn Thờ Cổ & Rương
        PositionGameEntities();

        Debug.Log("<color=green>[MAP BUILDER]</color> Hoàn tất kiến tạo Bản Đồ Đa Phòng: 4 Phòng, 3 Hành Lang, 18 Đuốc, 5 Cửa Phong Ấn!");
    }

    private static void SetupDungeonRooms(RoomBarrier b1, RoomBarrier b2_exit, RoomBarrier b3_entrance, RoomBarrier b3_exit, RoomBarrier b4_entrance)
    {
        Transform roomsRoot = GetOrCreateRoot("DungeonRooms");
        ClearChildren(roomsRoot);

        var mgrObj = roomsRoot.gameObject;
        var mgr = mgrObj.GetComponent<DungeonRoomManager>();
        if (mgr == null) mgr = mgrObj.AddComponent<DungeonRoomManager>();
        mgr.rooms.Clear();

        // Room 1: Sanctuary
        var r1Obj = new GameObject("Room_1_Sanctuary");
        r1Obj.transform.SetParent(roomsRoot);
        var r1 = r1Obj.AddComponent<DungeonRoom>();
        r1.roomIndex = 1;
        r1.roomType = DungeonRoom.RoomType.Sanctuary;
        r1.roomTitle = "SANCTUARY OF AWAKENING";
        r1.roomSubtitle = "Đền Thờ Khởi Đầu - Vùng An Toàn";
        r1.roomBounds = new Rect(-16, -6, 14, 12);
        r1.roomCenter = new Vector3(-9, 0, 0);
        r1.isCleared = true;
        r1.isEntered = true;
        mgr.rooms.Add(r1);

        // Room 2: Sunken Hall
        var r2Obj = new GameObject("Room_2_SunkenHall");
        r2Obj.transform.SetParent(roomsRoot);
        var r2 = r2Obj.AddComponent<DungeonRoom>();
        r2.roomIndex = 2;
        r2.roomType = DungeonRoom.RoomType.SunkenHall;
        r2.roomTitle = "THE SUNKEN HALL";
        r2.roomSubtitle = "Tiêu diệt toán Vệ Binh Tuần Tra (Wave 1)";
        r2.roomBounds = new Rect(8, -8, 22, 20);
        r2.roomCenter = new Vector3(19, 2, 0);
        r2.enemySpawns = new List<Vector3>() { new Vector3(14, 4, 0), new Vector3(24, 0, 0) };
        r2.entranceBarrier = b1;
        r2.exitBarrier = b2_exit;
        r2.nextEntranceBarrier = b3_entrance;
        mgr.rooms.Add(r2);

        // Room 3: Cursed Crypt
        var r3Obj = new GameObject("Room_3_CursedCrypt");
        r3Obj.transform.SetParent(roomsRoot);
        var r3 = r3Obj.AddComponent<DungeonRoom>();
        r3.roomIndex = 3;
        r3.roomType = DungeonRoom.RoomType.CursedCrypt;
        r3.roomTitle = "THE CURSED CRYPT";
        r3.roomSubtitle = "Thanh tẩy Hầm Mộ Bị Nguyền Rủa (Wave 2)";
        r3.roomBounds = new Rect(8, 24, 22, 20);
        r3.roomCenter = new Vector3(19, 34, 0);
        r3.enemySpawns = new List<Vector3>() { new Vector3(13, 38, 0), new Vector3(25, 38, 0), new Vector3(13, 30, 0), new Vector3(25, 30, 0) };
        r3.entranceBarrier = b3_entrance;
        r3.exitBarrier = b3_exit;
        r3.nextEntranceBarrier = b4_entrance;
        mgr.rooms.Add(r3);

        // Room 4: Titan Sanctum (Boss)
        var r4Obj = new GameObject("Room_4_TitanSanctum");
        r4Obj.transform.SetParent(roomsRoot);
        var r4 = r4Obj.AddComponent<DungeonRoom>();
        r4.roomIndex = 4;
        r4.roomType = DungeonRoom.RoomType.TitanSanctum;
        r4.roomTitle = "SANCTUM OF THE TITAN";
        r4.roomSubtitle = "ĐẠI CHIẾN BOSS: TITAN WHEEL";
        r4.roomBounds = new Rect(42, 20, 28, 28);
        r4.roomCenter = new Vector3(56, 34, 0);
        r4.enemySpawns = new List<Vector3>() { new Vector3(56, 34, 0) };
        r4.entranceBarrier = b4_entrance;
        mgr.rooms.Add(r4);
    }

    private static void PositionGameEntities()
    {
        // 1. Đặt người chơi tại Phòng 1 (Căn chỉnh chân nhân vật chính xác tâm sàn xuất phát)
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
            float footOffset = PvPMatchManager.GetHeroFootOffsetY(curHero);
            player.transform.position = new Vector3(-9f, -footOffset, 0f);
        }

        // 2. Bàn Thờ Cổ tại hốc phía Tây Phòng 1
        var shrine = Object.FindAnyObjectByType<AncientShrine>();
        if (shrine != null)
        {
            shrine.transform.position = new Vector3(-13f, 2f, 0f);
        }

        // 3. Rương Khởi Đầu tại góc Đông Bắc Phòng 1
        var chest = Object.FindAnyObjectByType<TreasureChest>();
        if (chest != null)
        {
            chest.transform.position = new Vector3(-5f, 2f, 0f);
        }

        // 4. Rương Thưởng 2 tại Hầm Mộ Phòng 3
        var chests = Object.FindObjectsByType<TreasureChest>(FindObjectsSortMode.None);
        if (chests.Length < 2)
        {
            var chest2 = TreasureChest.SpawnChest(new Vector3(12f, 41f, 0f));
            if (chest2 != null) chest2.gameObject.name = "TreasureChest_Crypt";
        }
    }

    private static void SpawnTorch(Vector3 pos, Transform parent)
    {
        GameObject torch = new GameObject("Torch");
        torch.transform.position = pos;
        torch.transform.SetParent(parent);

        var light = torch.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.72f, 0.35f, 1f); // Warm flame
        light.intensity = 1.35f;
        light.pointLightInnerRadius = 0.6f;
        light.pointLightOuterRadius = 4.2f;

        torch.AddComponent<TorchFlicker>();
    }

    private static Transform GetOrCreateRoot(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return existing.transform;
        return new GameObject(name).transform;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    // =========================================================================
    // KIẾN TẠO BẢN ĐỒ VÕ ĐÀI ĐỐI KHÁNG ĐỘC LẬP (PVP COLOSSEUM ARENA MAP)
    // =========================================================================
    private static Sprite _cachedColosseumCrestSprite;
    private static Sprite _cachedPedestalRingSprite;

    public static void BuildPvPColosseumArena()
    {
        Debug.Log("<color=orange>[PVP ARENA BUILDER]</color> Khởi tạo Bản Đồ Đấu Trường Cổ Đại (Grand Colosseum Arena) độc lập cho chế độ PvP 1v1...");

        // 1. Dọn sạch toàn bộ các cụm phòng, cửa chắn, rương, bàn thờ của chế độ đi ải cũ
        string[] pveRoots = new string[] { "DungeonBarriers", "DungeonRooms", "DungeonTorches", "DungeonBreakables", "PvP_Arena_Border" };
        foreach (var rName in pveRoots)
        {
            var obj = GameObject.Find(rName);
            if (obj != null) Object.DestroyImmediate(obj);
        }

        var roomMgrs = Object.FindObjectsByType<DungeonRoomManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var rm in roomMgrs) if (rm != null) Object.DestroyImmediate(rm.gameObject);

        var spawners = Object.FindObjectsByType<WaveSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sp in spawners) if (sp != null) sp.gameObject.SetActive(false);

        var chests = Object.FindObjectsByType<TreasureChest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in chests) if (c != null) Object.DestroyImmediate(c.gameObject);

        var shrines = Object.FindObjectsByType<AncientShrine>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in shrines) if (s != null) Object.DestroyImmediate(s.gameObject);

        // 2. Tìm Tilemap Ground & Walls để xây dựng Võ Đài Đá Mới Hoàn Toàn
        Tilemap groundTm = null;
        Tilemap wallsTm = null;

        var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (var tm in tilemaps)
        {
            string n = tm.gameObject.name.ToLower();
            if (n.Contains("ground") || n.Contains("floor")) groundTm = tm;
            else if (n.Contains("wall")) wallsTm = tm;
        }

        if (groundTm != null && wallsTm != null)
        {
            TileBase floorTile = ExtractAnyTile(groundTm, new Vector3Int(-9, 0, 0));
            TileBase wallTile = ExtractAnyTile(wallsTm, new Vector3Int(-14, 7, 0));

#if UNITY_EDITOR
            if (floorTile == null)
                floorTile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Palettes/TX Tileset Stone Ground_24.asset");
            if (wallTile == null)
                wallTile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Palettes/TX Tileset Stone Ground_43.asset");
#endif

            if (floorTile != null && wallTile != null)
            {
                groundTm.ClearAllTiles();
                wallsTm.ClearAllTiles();

                HashSet<Vector2Int> arenaFloor = new HashSet<Vector2Int>();

                // Thiết kế Võ Đài Bát Giác Cổ Đại đối xứng hoàn hảo:
                // Rộng từ x = -11 đến +11 (23 ô), Cao từ y = -6 đến +6 (13 ô), vát 4 góc hoàng gia (|x| + |y| <= 15)
                for (int x = -11; x <= 11; x++)
                {
                    for (int y = -6; y <= 6; y++)
                    {
                        if (Mathf.Abs(x) + Mathf.Abs(y) <= 15)
                        {
                            arenaFloor.Add(new Vector2Int(x, y));
                        }
                    }
                }

                foreach (var p in arenaFloor)
                {
                    groundTm.SetTile(new Vector3Int(p.x, p.y, 0), floorTile);
                }

                // Tường đá kiên cố dày 3 lớp bao quanh toàn bộ Võ Đài Bát Giác
                HashSet<Vector2Int> arenaWalls = new HashSet<Vector2Int>();
                foreach (var p in arenaFloor)
                {
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        for (int dy = -3; dy <= 3; dy++)
                        {
                            Vector2Int nb = new Vector2Int(p.x + dx, p.y + dy);
                            if (!arenaFloor.Contains(nb))
                            {
                                arenaWalls.Add(nb);
                            }
                        }
                    }
                }

                foreach (var w in arenaWalls)
                {
                    wallsTm.SetTile(new Vector3Int(w.x, w.y, 0), wallTile);
                }

                groundTm.RefreshAllTiles();
                wallsTm.RefreshAllTiles();

                var wallCol = wallsTm.GetComponent<TilemapCollider2D>();
                if (wallCol == null) wallCol = wallsTm.gameObject.AddComponent<TilemapCollider2D>();
                wallCol.compositeOperation = Collider2D.CompositeOperation.None;
                wallCol.ProcessTilemapChanges();
            }
        }

        // 3. Kiến tạo Kiến Trúc Đấu Trường Hoàng Gia (Ấn Quyết Đấu Trung Tâm, Đài Xuất Chiến Đông-Tây, 8 Trụ Lửa Lôi Đài)
        Transform archRoot = GetOrCreateRoot("PvP_Colosseum_Architecture");
        ClearChildren(archRoot);

        Shader sprShader = Shader.Find("Sprites/Default");

        // 3a. Ấn Quyết Đấu Hoàng Kim ở chính giữa Võ Đài (0, 0)
        GameObject crestObj = new GameObject("Center_Colosseum_Crest");
        crestObj.transform.SetParent(archRoot, false);
        crestObj.transform.position = new Vector3(0f, 0.2f, 0f);
        crestObj.transform.localScale = new Vector3(4.2f, 4.2f, 1f);
        var crestSr = crestObj.AddComponent<SpriteRenderer>();
        if (sprShader != null) crestSr.material = new Material(sprShader);
        crestSr.sprite = GetColosseumCrestSprite();
        crestSr.color = new Color(0.35f, 0.32f, 0.25f, 0.14f); // Khắc chìm âm sàn tinh tế, không làm rối mắt người chơi
        crestSr.sortingOrder = -8;

        // 3b. Đài Xuất Chiến Tây (Người Chơi 1 - Lam Ngọc) & Đông (Đối Thủ - Huyết Tinh)
        CreateChampionPedestal(archRoot, "West_Champion_Pedestal", new Vector3(-3.8f, 0.2f, 0f), new Color(0.22f, 0.75f, 0.85f, 0.18f), sprShader);
        CreateChampionPedestal(archRoot, "East_Champion_Pedestal", new Vector3(3.8f, 0.2f, 0f), new Color(0.85f, 0.30f, 0.25f, 0.18f), sprShader);

        // 3c. Viền Khảm Vàng Bát Giác chạy dọc mép trong sàn đấu
        GameObject inlayObj = new GameObject("Colosseum_GoldInlay_Border");
        inlayObj.transform.SetParent(archRoot, false);
        LineRenderer lr = inlayObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        if (sprShader != null) lr.material = new Material(sprShader);
        Color inlayCol = new Color(0.75f, 0.60f, 0.25f, 0.25f);
        lr.startColor = inlayCol;
        lr.endColor = inlayCol;
        lr.sortingOrder = -7;

        Vector3[] octagonPts = new Vector3[]
        {
            new Vector3(-9.0f,  5.6f, 0f),
            new Vector3( 9.0f,  5.6f, 0f),
            new Vector3(11.0f,  3.6f, 0f),
            new Vector3(11.0f, -3.2f, 0f),
            new Vector3( 9.0f, -5.2f, 0f),
            new Vector3(-9.0f, -5.2f, 0f),
            new Vector3(-11.0f, -3.2f, 0f),
            new Vector3(-11.0f,  3.6f, 0f)
        };
        lr.positionCount = octagonPts.Length;
        lr.SetPositions(octagonPts);

        // 3d. 8 Đài Lửa Cổ Đại (Colosseum Flaming Braziers) thắp sáng toàn bộ Võ Đài
        Vector3[] brazierPositions = new Vector3[]
        {
            new Vector3(-8.2f,  5.3f, 0f),
            new Vector3( 0.0f,  5.5f, 0f),
            new Vector3( 8.2f,  5.3f, 0f),
            new Vector3(-8.2f, -4.9f, 0f),
            new Vector3( 0.0f, -5.1f, 0f),
            new Vector3( 8.2f, -4.9f, 0f),
            new Vector3(-10.5f, 0.2f, 0f),
            new Vector3( 10.5f, 0.2f, 0f)
        };
        foreach (var bPos in brazierPositions)
        {
            SpawnColosseumBrazier(bPos, archRoot, sprShader);
        }
    }

    private static TileBase ExtractAnyTile(Tilemap tm, Vector3Int preferredPos)
    {
        if (tm == null) return null;
        TileBase t = tm.GetTile(preferredPos);
        if (t != null) return t;

        BoundsInt bounds = tm.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            t = tm.GetTile(pos);
            if (t != null) return t;
        }
        return null;
    }

    private static void CreateChampionPedestal(Transform parent, string name, Vector3 pos, Color glowColor, Shader sh)
    {
        GameObject ped = new GameObject(name);
        ped.transform.SetParent(parent, false);
        ped.transform.position = pos;
        ped.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
        var sr = ped.AddComponent<SpriteRenderer>();
        if (sh != null) sr.material = new Material(sh);
        sr.sprite = GetPedestalRingSprite();
        sr.color = glowColor;
        sr.sortingOrder = -7;

        var light = ped.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(glowColor.r, glowColor.g, glowColor.b, 1f);
        light.intensity = 1.15f;
        light.pointLightInnerRadius = 0.5f;
        light.pointLightOuterRadius = 3.5f;
    }

    private static void SpawnColosseumBrazier(Vector3 pos, Transform parent, Shader sh)
    {
        GameObject brazier = new GameObject("Colosseum_Brazier");
        brazier.transform.SetParent(parent, false);
        brazier.transform.position = pos;

        var sr = brazier.AddComponent<SpriteRenderer>();
        if (sh != null) sr.material = new Material(sh);
        sr.sprite = GetPedestalRingSprite();
        sr.color = new Color(1f, 0.55f, 0.15f, 0.85f);
        brazier.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
        sr.sortingOrder = -5;

        var light = brazier.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.74f, 0.36f, 1f);
        light.intensity = 1.45f;
        light.pointLightInnerRadius = 0.8f;
        light.pointLightOuterRadius = 5.2f;

        brazier.AddComponent<TorchFlicker>();
    }

    private static Sprite GetColosseumCrestSprite()
    {
        if (_cachedColosseumCrestSprite != null) return _cachedColosseumCrestSprite;
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        float c = (size - 1) * 0.5f;
        Color[] cols = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 0f;

                // Vòng cổ ngữ ngoài & trong
                if ((r >= 56f && r <= 60f) || (r >= 44f && r <= 47f) || (r >= 20f && r <= 23f))
                    a = 1f;
                // Chữ thập kiếm quyết đấu
                else if (r <= 58f && (Mathf.Abs(dx) <= 1.6f || Mathf.Abs(dy) <= 1.6f || Mathf.Abs(Mathf.Abs(dx) - Mathf.Abs(dy)) <= 1.5f))
                    a = 0.65f;

                cols[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        _cachedColosseumCrestSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        return _cachedColosseumCrestSprite;
    }

    private static Sprite GetPedestalRingSprite()
    {
        if (_cachedPedestalRingSprite != null) return _cachedPedestalRingSprite;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        float c = (size - 1) * 0.5f;
        Color[] cols = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float r = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a = 0f;
                if ((r >= 26f && r <= 30f) || (r >= 18f && r <= 20f)) a = 1f;
                else if (r < 18f) a = 0.22f;
                cols[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        _cachedPedestalRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        return _cachedPedestalRingSprite;
    }
}
