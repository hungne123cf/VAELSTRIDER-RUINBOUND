using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Multi-Path Map Expansion Builder (Map 2 & Map 3)
/// 
/// Nâng cấp Map 2 (Thung Lũng Tàn Tích) và Map 3 (Ký Ức Lãng Quên) thành Bản Đồ Đa Nhánh (3 Lối Đi Dài):
/// - Mỗi nhánh gồm 3 Hành Lang / Cầu Đá + 2 Phòng Quái Chiến Đấu + 1 Đấu Trường Đại Trùm riêng biệt.
/// - Mỗi lối đi có mật độ quái khác nhau rõ rệt:
///   1. Nhánh Đông Quái (Swarm Route): Rất nhiều quái, rơi nhiều Linh Hồn & EXP -> Dẫn tới Đại Trùm 1.
///   2. Nhánh Quái Vừa Phải (Balanced Route): Số lượng quái cân bằng -> Dẫn tới Đại Trùm 2.
///   3. Nhánh Ít Quái Tinh Anh (Low-Monster Fast Route): Ít quái nhưng tinh nhuệ, tiến nhanh -> Dẫn tới Đại Trùm 3.
/// - Tích hợp đầy đủ với DungeonRoomManager, FullscreenMapUI, MinimapHUD và SurroundingWaterOceanSystem.
/// </summary>
public class MultiPathMapExpansionBuilder : MonoBehaviour
{
    public enum RouteDensity
    {
        SwarmManyMonsters,     // Đông quái (2 đợt lớn mỗi phòng)
        BalancedMonsters,      // Quái vừa phải (2 đợt vừa mỗi phòng)
        EliteFewMonsters       // Ít quái tinh anh (1 đợt ít quái mỗi phòng)
    }

    public enum RouteBossType
    {
        GorMokBloodLord,       // Đại Trùm Gor'Mok - Huyết Thú Cổ Thụ (BloodMonsterAI Boss)
        AncientValleyGolem,    // Golem Cổ Đại - Cự Thạch Hộ Vệ (ValleyGolemBossAI)
        ShadowDoppelganger,    // Bóng Ma Bản Ngã (ShadowDoppelgangerBoss)
        AncientSpiderKing      // Chúa Tể Nhện Cổ Đại (SkeletonKingBossAI)
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
        ValleySpitterAI.OnEnemyDied += OnValleySpitterDied;
        ValleyGolemBossAI.OnEnemyDied += OnValleyGolemDied;
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
        ValleySpitterAI.OnEnemyDied -= OnValleySpitterDied;
        ValleyGolemBossAI.OnEnemyDied -= OnValleyGolemDied;
        BotWheelAI.OnEnemyDied -= OnBotWheelDied;
        GoblinAI.OnEnemyDied -= OnGenericEnemyDied;
        SkeletonAI.OnEnemyDied -= OnGenericEnemyDied;
        SlimeAI.OnEnemyDied -= OnGenericEnemyDied;
        SkeletonKingBossAI.OnBossDied -= OnSkeletonKingDied;
    }

    private void OnBloodMonsterDied(BloodMonsterAI e) => NotifyMonsterSlain(e != null ? e.gameObject : null);
    private void OnValleyBeastDied(ValleyBeastAI e) => NotifyMonsterSlain(e != null ? e.gameObject : null);
    private void OnValleySpitterDied(ValleySpitterAI e) => NotifyMonsterSlain(e != null ? e.gameObject : null);
    private void OnValleyGolemDied(ValleyGolemBossAI e) => NotifyMonsterSlain(e != null ? e.gameObject : null);
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
            BuildMap2MultiPathValley();
        }
        else
        {
            BuildMap3MultiPathIslands();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 1. MAP 2: THUNG LŨNG TÀN TÍCH ĐA NHÁNH (EAST, SOUTH, WEST ROUTES)
    // ═══════════════════════════════════════════════════════════════════════════
    private void BuildMap2MultiPathValley()
    {
        // Tìm Tilemap Ground & Walls của Map 2
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

        if (groundTm == null || wallsTm == null)
        {
            Debug.LogWarning("[MULTI-PATH MAP 2] Không tìm thấy Tilemap Ground hoặc Walls!");
            return;
        }

        // Lấy mẫu gạch Cỏ, Đất và Tường Đá trực tiếp từ Tilemap hiện có (Zero-Bug Android)
        TileBase grassTile = groundTm.GetTile(new Vector3Int(-12, 0, 0)) ?? groundTm.GetTile(new Vector3Int(0, 0, 0));
        TileBase dirtTile  = groundTm.GetTile(new Vector3Int(0, -3, 0)) ?? grassTile;
        TileBase wallTile  = wallsTm.GetTile(new Vector3Int(-20, 0, 0)) ?? wallsTm.GetTile(new Vector3Int(20, 0, 0)) ?? wallsTm.GetTile(new Vector3Int(0, -14, 0));

        // Giải phóng lối đi tại 3 cửa Đông, Nam, Tây của Thánh Địa Trung Tâm
        ClearBlockingHubPropsMap2();

        // Định nghĩa toàn bộ các hình chữ nhật mặt đất (Walkable Floor Rectangles)
        // Central Sanctuary Hub: x [-19..19], y [-13..13]
        AddWalkableRect(-19, -13, 39, 27);

        // --- NHÁNH ĐÔNG (+X): ĐÔNG QUÁI -> BOSS GOR'MOK ---
        AddWalkableRect(20, -3, 14, 6);    // Hành lang Đông 1 (x: 20..33, y: -3..2)
        AddWalkableRect(34, -10, 21, 21);  // Phòng Đông 1 (x: 34..54, y: -10..10)
        AddWalkableRect(55, -3, 13, 6);    // Hành lang Đông 2 (x: 55..67, y: -3..2)
        AddWalkableRect(68, -10, 21, 21);  // Phòng Đông 2 (x: 68..88, y: -10..10)
        AddWalkableRect(89, -3, 13, 6);    // Hành lang Đông 3 (x: 89..101, y: -3..2)
        AddWalkableRect(102, -13, 27, 27); // Đấu Trường Boss Đông (x: 102..128, y: -13..13)

        // --- NHÁNH NAM (-Y): QUÁI VỪA PHẢI -> BOSS GOLEM CỔ ĐẠI ---
        AddWalkableRect(-3, -27, 6, 14);   // Hành lang Nam 1 (x: -3..2, y: -27..-14)
        AddWalkableRect(-12, -48, 25, 21); // Phòng Nam 1 (x: -12..12, y: -48..-28)
        AddWalkableRect(-3, -61, 6, 13);   // Hành lang Nam 2 (x: -3..2, y: -61..-49)
        AddWalkableRect(-12, -82, 25, 21); // Phòng Nam 2 (x: -12..12, y: -82..-62)
        AddWalkableRect(-3, -95, 6, 13);   // Hành lang Nam 3 (x: -3..2, y: -95..-83)
        AddWalkableRect(-15, -122, 31, 27);// Đấu Trường Boss Nam (x: -15..15, y: -122..-96)

        // --- NHÁNH TÂY (-X): ÍT QUÁI TINH ANH -> BOSS BÓNG MA BẢN NGÃ ---
        AddWalkableRect(-33, -3, 14, 6);   // Hành lang Tây 1 (x: -33..-20, y: -3..2)
        AddWalkableRect(-54, -10, 21, 21); // Phòng Tây 1 (x: -54..-34, y: -10..10)
        AddWalkableRect(-67, -3, 13, 6);   // Hành lang Tây 2 (x: -67..-55, y: -3..2)
        AddWalkableRect(-88, -10, 21, 21); // Phòng Tây 2 (x: -88..-68, y: -10..10)
        AddWalkableRect(-101, -3, 13, 6);  // Hành lang Tây 3 (x: -101..-89, y: -3..2)
        AddWalkableRect(-128, -13, 27, 27);// Đấu Trường Boss Tây (x: -128..-102, y: -13..13)

        // Tập hợp toàn bộ ô đất đi được
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

        // Vẽ tâm đấu trường đất nện (Dirt Clearing) cho các phòng chiến đấu và phòng Boss mới
        Vector2[] clearingCenters = new Vector2[]
        {
            new Vector2(44f, 0f), new Vector2(78f, 0f), new Vector2(115f, 0f),
            new Vector2(0f, -38f), new Vector2(0f, -72f), new Vector2(0f, -109f),
            new Vector2(-44f, 0f), new Vector2(-78f, 0f), new Vector2(-115f, 0f)
        };
        if (dirtTile != null)
        {
            foreach (var c in clearingCenters)
            {
                int cx = Mathf.RoundToInt(c.x);
                int cy = Mathf.RoundToInt(c.y);
                for (int dx = -6; dx <= 6; dx++)
                {
                    for (int dy = -5; dy <= 5; dy++)
                    {
                        if ((dx * dx) / 36f + (dy * dy) / 25f <= 1.0f)
                        {
                            groundTm.SetTile(new Vector3Int(cx + dx, cy + dy, 0), dirtTile);
                        }
                    }
                }
            }
        }

        // Xây viền tường đá kín xung quanh toàn bộ chu vi bản đồ mới (trừ phần phía Bắc cây cổ thụ y >= 14 của Hub)
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

        // Khởi tạo hệ thống Phòng, Cửa Phong Ấn, Bảng Chỉ Đường và Trang Trí cho Map 2
        SetupMap2RoomsAndBarriers();
        DecorateMap2ExpansionChambers();
    }

    private void ClearBlockingHubPropsMap2()
    {
        var envRoot = GameObject.Find("Map2_Environment");
        if (envRoot == null) return;

        for (int i = envRoot.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = envRoot.transform.GetChild(i);
            if (child == null) continue;
            Vector3 p = child.position;

            // Di dời Trụ Đá Nam để mở Cổng Nam (x: -3..2, y: -14..-9)
            if (child.name.Contains("Glowing_Rune_Pillar_South"))
            {
                child.position = new Vector3(-6.2f, -11.5f, 0f);
                continue;
            }

            // Di dời các cây/đá nằm chắn ngay trước Cổng Đông (x > 11, y: -4.5..3.5) hoặc Cổng Tây (x < -11, y: -4.5..3.5)
            if (p.y >= -4.8f && p.y <= 3.8f)
            {
                if (p.x >= 11.5f)
                {
                    child.position = new Vector3(p.x, 6.2f, p.z);
                }
                else if (p.x <= -11.5f)
                {
                    child.position = new Vector3(p.x, 6.2f, p.z);
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

        // Phòng 0: Thánh Địa Trung Tâm (Sanctuary)
        var hubRoom = CreateCustomRoom(
            roomsRoot, 1, DungeonRoom.RoomType.Sanctuary,
            new Rect(-19f, -13f, 38f, 26f),
            "THÁNH ĐỊA THUNG LŨNG (TRUNG TÂM)", "VALLEY SANCTUARY (CENTRAL HUB)",
            "Hãy chọn 1 trong 3 Lối Đi (Đông / Nam / Tây) dẫn tới 3 Đại Trùm khác nhau!",
            "Choose 1 of 3 Branching Paths (East / South / West) leading to 3 different Bosses!",
            null, null, null, null
        );
        hubRoom.isEntered = true;
        hubRoom.isCleared = true;
        mgr.rooms.Add(hubRoom);
        mgr.currentRoom = hubRoom;

        // --- NHÁNH ĐÔNG: ĐÔNG QUÁI -> BOSS GOR'MOK ---
        var bE1_in  = CreateBarrier(barriersRoot, new Vector3(34f, -0.5f, 0f), true, 6.2f, "Barrier_E1_In");
        var bE1_out = CreateBarrier(barriersRoot, new Vector3(54f, -0.5f, 0f), true, 6.2f, "Barrier_E1_Out");
        var bE2_in  = CreateBarrier(barriersRoot, new Vector3(68f, -0.5f, 0f), true, 6.2f, "Barrier_E2_In");
        var bE2_out = CreateBarrier(barriersRoot, new Vector3(88f, -0.5f, 0f), true, 6.2f, "Barrier_E2_Out");
        var bE3_in  = CreateBarrier(barriersRoot, new Vector3(102f, -0.5f, 0f), true, 6.2f, "Barrier_E3_Boss");

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 2, DungeonRoom.RoomType.SunkenHall,
            new Rect(34f, -10f, 20f, 20f),
            "NHÁNH ĐÔNG 1: ĐẠI LỘ HUYẾT THÚ", "EAST PATH 1: BLOOD MONSTER AVENUE",
            "Lối đi Đông Quái! Quét sạch 2 đợt bầy Huyết Thú đông đảo!",
            "High-density Swarm Path! Purge 2 massive Blood Monster waves!",
            bE1_in, bE1_out, bE2_in,
            r => StartCoroutine(RunMap2CombatRoomRoutine(r, RouteDensity.SwarmManyMonsters, 1))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 3, DungeonRoom.RoomType.CursedCrypt,
            new Rect(68f, -10f, 20f, 20f),
            "NHÁNH ĐÔNG 2: HUYẾT TRÌ SÔI SỤC", "EAST PATH 2: BOILING BLOOD BASIN",
            "Bầy quái thung lũng hung tợn bảo vệ cửa vào Huyết Điện!",
            "Frenzied valley monsters guarding the Blood Sanctum!",
            bE2_in, bE2_out, bE3_in,
            r => StartCoroutine(RunMap2CombatRoomRoutine(r, RouteDensity.SwarmManyMonsters, 2))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 4, DungeonRoom.RoomType.TitanSanctum,
            new Rect(102f, -13f, 26f, 26f),
            "ĐẤU TRƯỜNG ĐÔNG: ĐẠI TRÙM GOR'MOK", "EAST SANCTUM: OVERLORD GOR'MOK",
            "ĐẠI CHIẾN TRÙM NHÁNH ĐÔNG: GOR'MOK - HUYẾT THÚ CỔ THỤ!",
            "EAST ROUTE FINAL BOSS: GOR'MOK THE ANCIENT BLOOD BEAST!",
            bE3_in, null, null,
            r => StartCoroutine(RunBossRoomRoutine(r, RouteBossType.GorMokBloodLord, isMap3: false))
        ));

        // --- NHÁNH NAM: QUÁI VỪA PHẢI -> BOSS GOLEM CỔ ĐẠI ---
        var bS1_in  = CreateBarrier(barriersRoot, new Vector3(-0.5f, -28f, 0f), false, 6.2f, "Barrier_S1_In");
        var bS1_out = CreateBarrier(barriersRoot, new Vector3(-0.5f, -48f, 0f), false, 6.2f, "Barrier_S1_Out");
        var bS2_in  = CreateBarrier(barriersRoot, new Vector3(-0.5f, -62f, 0f), false, 6.2f, "Barrier_S2_In");
        var bS2_out = CreateBarrier(barriersRoot, new Vector3(-0.5f, -82f, 0f), false, 6.2f, "Barrier_S2_Out");
        var bS3_in  = CreateBarrier(barriersRoot, new Vector3(-0.5f, -96f, 0f), false, 6.2f, "Barrier_S3_Boss");

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 5, DungeonRoom.RoomType.SunkenHall,
            new Rect(-12f, -48f, 24f, 20f),
            "NHÁNH NAM 1: SẢNH ĐÁ RÊU PHONG", "SOUTH PATH 1: MOSSY STONE HALL",
            "Lối đi Cân Bằng! Tiêu diệt đội tuần tra thung lũng!",
            "Balanced Path! Defeat the valley patrol squad!",
            bS1_in, bS1_out, bS2_in,
            r => StartCoroutine(RunMap2CombatRoomRoutine(r, RouteDensity.BalancedMonsters, 1))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 6, DungeonRoom.RoomType.CursedCrypt,
            new Rect(-12f, -82f, 24f, 20f),
            "NHÁNH NAM 2: HẺM NÚI ĐÁ VỠ", "SOUTH PATH 2: SHATTERED CANYON",
            "Vượt qua chốt chặn cuối trước Thánh Điện Cự Thạch!",
            "Break through the final garrison before the Golem Sanctum!",
            bS2_in, bS2_out, bS3_in,
            r => StartCoroutine(RunMap2CombatRoomRoutine(r, RouteDensity.BalancedMonsters, 2))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 7, DungeonRoom.RoomType.TitanSanctum,
            new Rect(-15f, -122f, 30f, 26f),
            "ĐẤU TRƯỜNG NAM: CỰ THẠCH GOLEM", "SOUTH SANCTUM: ANCIENT VALLEY GOLEM",
            "ĐẠI CHIẾN TRÙM NHÁNH NAM: CỰ THẠCH HỘ VỆ THUNG LŨNG!",
            "SOUTH ROUTE FINAL BOSS: ANCIENT STONE GOLEM!",
            bS3_in, null, null,
            r => StartCoroutine(RunBossRoomRoutine(r, RouteBossType.AncientValleyGolem, isMap3: false))
        ));

        // --- NHÁNH TÂY: ÍT QUÁI TINH ANH -> BOSS BÓNG MA BẢN NGÃ ---
        var bW1_in  = CreateBarrier(barriersRoot, new Vector3(-34f, -0.5f, 0f), true, 6.2f, "Barrier_W1_In");
        var bW1_out = CreateBarrier(barriersRoot, new Vector3(-54f, -0.5f, 0f), true, 6.2f, "Barrier_W1_Out");
        var bW2_in  = CreateBarrier(barriersRoot, new Vector3(-68f, -0.5f, 0f), true, 6.2f, "Barrier_W2_In");
        var bW2_out = CreateBarrier(barriersRoot, new Vector3(-88f, -0.5f, 0f), true, 6.2f, "Barrier_W2_Out");
        var bW3_in  = CreateBarrier(barriersRoot, new Vector3(-102f, -0.5f, 0f), true, 6.2f, "Barrier_W3_Boss");

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 8, DungeonRoom.RoomType.SunkenHall,
            new Rect(-54f, -10f, 20f, 20f),
            "NHÁNH TÂY 1: VƯỜN CỔ TỊNH KHÔNG", "WEST PATH 1: SILENT ANCIENT GROVE",
            "Lối đi Ít Quái Tinh Anh! Hạ gục nhanh hộ vệ tinh nhuệ!",
            "Low-Monster Elite Path! Swiftly defeat the elite guardians!",
            bW1_in, bW1_out, bW2_in,
            r => StartCoroutine(RunMap2CombatRoomRoutine(r, RouteDensity.EliteFewMonsters, 1))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 9, DungeonRoom.RoomType.CursedCrypt,
            new Rect(-88f, -10f, 20f, 20f),
            "NHÁNH TÂY 2: HÀNH LANG PHẢN CHIẾU", "WEST PATH 2: MIRRORED CORRIDOR",
            "Bóng tối ngưng tụ trước Điện Thờ Bản Ngã!",
            "Shadows condense before the Mirror Sanctum!",
            bW2_in, bW2_out, bW3_in,
            r => StartCoroutine(RunMap2CombatRoomRoutine(r, RouteDensity.EliteFewMonsters, 2))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 10, DungeonRoom.RoomType.TitanSanctum,
            new Rect(-128f, -13f, 26f, 26f),
            "ĐẤU TRƯỜNG TÂY: BÓNG MA BẢN NGÃ", "WEST SANCTUM: SHADOW DOPPELGANGER",
            "ĐẠI CHIẾN TRÙM NHÁNH TÂY: BÓNG MA BẢN NGÃ HƯ KHÔNG!",
            "WEST ROUTE FINAL BOSS: VOID SHADOW DOPPELGANGER!",
            bW3_in, null, null,
            r => StartCoroutine(RunBossRoomRoutine(r, RouteBossType.ShadowDoppelganger, isMap3: false))
        ));

        // Đặt 3 Bảng Chỉ Đường Phát Sáng ngay tại 3 cửa ngõ của Thánh Địa Map 2
        CreateRouteSignpost(
            new Vector3(17.2f, 2.8f, 0f),
            LocalizationManager.T(
                "<b><color=#FF6B6B>➔ NHÁNH ĐÔNG: ĐẠI LỘ HUYẾT THÚ</color></b>\n<size=78%><color=#FFD166>[ĐÔNG QUÁI - NHIỀU LINH HỒN]</color>\nBOSS: ĐẠI TRÙM GOR'MOK</size>",
                "<b><color=#FF6B6B>➔ EAST PATH: BLOOD AVENUE</color></b>\n<size=78%><color=#FFD166>[MANY MONSTERS - HIGH LOOT]</color>\nBOSS: OVERLORD GOR'MOK</size>"
            ),
            new Color(1f, 0.35f, 0.2f)
        );

        CreateRouteSignpost(
            new Vector3(0f, -10.8f, 0f),
            LocalizationManager.T(
                "<b><color=#4ECDC4>⬇ NHÁNH NAM: HẺM NÚI CỰ THẠCH</color></b>\n<size=78%><color=#A8E6CF>[QUÁI VỪA PHẢI - CÂN BẰNG]</color>\nBOSS: CỰ THẠCH GOLEM CỔ ĐẠI</size>",
                "<b><color=#4ECDC4>⬇ SOUTH PATH: STONE CANYON</color></b>\n<size=78%><color=#A8E6CF>[BALANCED MONSTERS]</color>\nBOSS: ANCIENT VALLEY GOLEM</size>"
            ),
            new Color(0.25f, 0.9f, 0.85f)
        );

        CreateRouteSignpost(
            new Vector3(-17.2f, 2.8f, 0f),
            LocalizationManager.T(
                "<b><color=#C77DFF>⬅ NHÁNH TÂY: ĐƯỜNG TẮT TỊNH KHÔNG</color></b>\n<size=78%><color=#E0AAFF>[ÍT QUÁI TINH ANH - ĐƯỜNG NHANH]</color>\nBOSS: BÓNG MA BẢN NGÃ</size>",
                "<b><color=#C77DFF>⬅ WEST PATH: SILENT SHORTCUT</color></b>\n<size=78%><color=#E0AAFF>[FEW ELITE MONSTERS - FAST]</color>\nBOSS: SHADOW DOPPELGANGER</size>"
            ),
            new Color(0.78f, 0.45f, 1f)
        );
    }

    private void DecorateMap2ExpansionChambers()
    {
        var envRoot = GameObject.Find("Map2_Environment");
        if (envRoot == null) return;

        Transform treeTemplate = envRoot.transform.Find("Forest_Border_Tree") ?? envRoot.transform.Find("Tree_MidLeft");
        Transform boulderTemplate = envRoot.transform.Find("Boulder_MidLeft");
        Transform pillarTemplate = envRoot.transform.Find("Glowing_Rune_Pillar_West");

        Transform decoRoot = new GameObject("Map2_ExpansionDecorations").transform;
        decoRoot.SetParent(transform, false);

        Vector2[] roomCenters = new Vector2[]
        {
            new Vector2(44f, 0f), new Vector2(78f, 0f), new Vector2(115f, 0f),
            new Vector2(0f, -38f), new Vector2(0f, -72f), new Vector2(0f, -109f),
            new Vector2(-44f, 0f), new Vector2(-78f, 0f), new Vector2(-115f, 0f)
        };

        for (int i = 0; i < roomCenters.Length; i++)
        {
            Vector3 c = new Vector3(roomCenters[i].x, roomCenters[i].y, 0f);
            bool isBossRoom = (i % 3 == 2);
            float rx = isBossRoom ? 9.5f : 7.0f;
            float ry = isBossRoom ? 9.0f : 6.8f;

            // Đặt 4 cây cổ thụ ở 4 góc phòng để tạo cảnh quan rừng thung lũng sống động (không chắn đường đi ở giữa)
            if (treeTemplate != null)
            {
                CloneDecor(treeTemplate, decoRoot, c + new Vector3(-rx, ry, 0f));
                CloneDecor(treeTemplate, decoRoot, c + new Vector3(rx, ry, 0f));
                CloneDecor(treeTemplate, decoRoot, c + new Vector3(-rx, -ry, 0f));
                CloneDecor(treeTemplate, decoRoot, c + new Vector3(rx, -ry, 0f));
            }

            if (boulderTemplate != null)
            {
                CloneDecor(boulderTemplate, decoRoot, c + new Vector3(-rx + 1.5f, -ry + 1.2f, 0f));
                CloneDecor(boulderTemplate, decoRoot, c + new Vector3(rx - 1.5f, ry - 1.2f, 0f));
            }

            if (pillarTemplate != null && isBossRoom)
            {
                CloneDecor(pillarTemplate, decoRoot, c + new Vector3(-6.5f, 6.5f, 0f));
                CloneDecor(pillarTemplate, decoRoot, c + new Vector3(6.5f, 6.5f, 0f));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 2. MAP 3: KÝ ỨC LÃNG QUÊN ĐA NHÁNH (NORTH, EAST, WEST ISLAND ROUTES)
    // ═══════════════════════════════════════════════════════════════════════════
    private void BuildMap3MultiPathIslands()
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

        // Gỡ bỏ tường kín 4 phía cũ của đảo trung tâm để xây hệ thống tường viền mở 3 cầu đá
        var oldWalls = GameObject.Find("ArenaWalls");
        if (oldWalls != null) Destroy(oldWalls);

        // Dịch chuyển nhẹ 2 cây ở mép Đông/Tây đảo trung tâm để không chắn lối lên Cầu Đông & Cầu Tây
        ClearBlockingHubPropsMap3();

        Transform map3ExpRoot = new GameObject("Map3_MultiPathIslands").transform;
        map3ExpRoot.SetParent(transform, false);

        // Đảo Trung Tâm (Hub): x [-16..16), y [-11..11)
        AddWalkableRect(-16, -11, 32, 22);

        // --- NHÁNH BẮC (+Y): ĐÔNG QUÁI -> BOSS CHÚA TỂ NHỆN SKELETON KING ---
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_N1", -3, 11, 6, 12);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Island_N1", -14, 23, 28, 18);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_N2", -3, 41, 6, 12);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Island_N2", -14, 53, 28, 18);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_N3", -3, 71, 6, 12);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "BossIsland_North", -17, 83, 34, 24);

        // --- NHÁNH ĐÔNG (+X): QUÁI VỪA PHẢI -> BOSS BÓNG MA BẢN NGÃ ---
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_E1", 16, -3, 12, 6);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Island_E1", 28, -11, 20, 22);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_E2", 48, -3, 12, 6);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Island_E2", 60, -11, 20, 22);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_E3", 80, -3, 12, 6);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "BossIsland_East", 92, -14, 26, 28);

        // --- NHÁNH TÂY (-X): ÍT QUÁI TINH ANH -> BOSS CỰ THẦN HƯ KHÔNG ---
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_W1", -28, -3, 12, 6);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Island_W1", -48, -11, 20, 22);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_W2", -60, -3, 12, 6);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Island_W2", -80, -11, 20, 22);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "Bridge_W3", -92, -3, 12, 6);
        CreateIslandFloor(map3ExpRoot, mossFloorSprite, floorMat, "BossIsland_West", -118, -14, 26, 28);

        // Tự động sinh toàn bộ tường bao quanh các đảo và cầu đá (mở thông hoàn toàn các đầu cầu)
        BuildPerimeterWallsFromWalkableRects(map3ExpRoot);

        // Thiết lập hệ thống Phòng, Cửa Phong Ấn, Bảng Chỉ Đường và Cây Cổ Thụ cho Map 3
        SetupMap3RoomsAndBarriers();
        DecorateMap3ExpansionIslands();
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

    private void ClearBlockingHubPropsMap3()
    {
        var deco = GameObject.Find("NatureDecorations");
        if (deco == null) return;

        for (int i = 0; i < deco.transform.childCount; i++)
        {
            Transform ch = deco.transform.GetChild(i);
            if (ch == null) continue;
            if (ch.name.Contains("RedTree_West"))
            {
                ch.position = new Vector3(-13.5f, 4.8f, 0f);
            }
            else if (ch.name.Contains("TealTree_East"))
            {
                ch.position = new Vector3(13.5f, 4.8f, 0f);
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

        // Phòng 0: Đảo Cổ Trung Tâm (Sanctuary Hub)
        var hubRoom = CreateCustomRoom(
            roomsRoot, 1, DungeonRoom.RoomType.Sanctuary,
            new Rect(-16f, -11f, 32f, 22f),
            "ĐẢO CỔ KÝ ỨC (TRUNG TÂM)", "FORGOTTEN ISLAND (CENTRAL HUB)",
            "Hãy chọn 1 trong 3 Cầu Đá (Bắc / Đông / Tây) dẫn tới 3 Đại Trùm khác nhau!",
            "Choose 1 of 3 Stone Bridges (North / East / West) leading to 3 different Bosses!",
            null, null, null, null
        );
        hubRoom.isEntered = true;
        hubRoom.isCleared = true;
        mgr.rooms.Add(hubRoom);
        mgr.currentRoom = hubRoom;

        // --- NHÁNH BẮC (+Y): ĐÔNG QUÁI -> BOSS CHÚA TỂ NHỆN SKELETON KING ---
        var bN1_in  = CreateBarrier(barriersRoot, new Vector3(0f, 23f, 0f), false, 6.2f, "Barrier_N1_In");
        var bN1_out = CreateBarrier(barriersRoot, new Vector3(0f, 41f, 0f), false, 6.2f, "Barrier_N1_Out");
        var bN2_in  = CreateBarrier(barriersRoot, new Vector3(0f, 53f, 0f), false, 6.2f, "Barrier_N2_In");
        var bN2_out = CreateBarrier(barriersRoot, new Vector3(0f, 71f, 0f), false, 6.2f, "Barrier_N2_Out");
        var bN3_in  = CreateBarrier(barriersRoot, new Vector3(0f, 83f, 0f), false, 6.2f, "Barrier_N3_Boss");

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 2, DungeonRoom.RoomType.SunkenHall,
            new Rect(-14f, 23f, 28f, 18f),
            "NHÁNH BẮC 1: ĐẢO YÊU TINH & QUÁI NHỚT", "NORTH PATH 1: GOBLIN & SLIME ISLE",
            "Lối đi Đông Quái! Càn quét 2 đợt bầy Yêu Tinh và Quái Nhớt áp đảo!",
            "High-Density Swarm Path! Purge 2 massive Goblin & Slime waves!",
            bN1_in, bN1_out, bN2_in,
            r => StartCoroutine(RunMap3CombatRoomRoutine(r, RouteDensity.SwarmManyMonsters, 1))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 3, DungeonRoom.RoomType.CursedCrypt,
            new Rect(-14f, 53f, 28f, 18f),
            "NHÁNH BẮC 2: QUẦN ĐẢO XƯƠNG CỔ ĐẠI", "NORTH PATH 2: ANCIENT BONE ARCHIPELAGO",
            "Binh đoàn Bộ Xương & Yêu Tinh bảo vệ Phong Ấn Trứng Nhện!",
            "Skeleton legion & Goblins guarding the Spider Cocoon Seal!",
            bN2_in, bN2_out, bN3_in,
            r => StartCoroutine(RunMap3CombatRoomRoutine(r, RouteDensity.SwarmManyMonsters, 2))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 4, DungeonRoom.RoomType.TitanSanctum,
            new Rect(-17f, 83f, 34f, 24f),
            "ĐẤU TRƯỜNG BẮC: CHÚA TỂ NHỆN CỔ ĐẠI", "NORTH SANCTUM: ANCIENT SPIDER LORD",
            "ĐẠI CHIẾN TRÙM NHÁNH BẮC: CHÚA TỂ NHỆN CỔ ĐẠI SKELETON KING!",
            "NORTH ROUTE FINAL BOSS: ANCIENT SPIDER LORD SKELETON KING!",
            bN3_in, null, null,
            r => StartCoroutine(RunBossRoomRoutine(r, RouteBossType.AncientSpiderKing, isMap3: true))
        ));

        // --- NHÁNH ĐÔNG (+X): QUÁI VỪA PHẢI -> BOSS BÓNG MA BẢN NGÃ ---
        var bE1_in  = CreateBarrier(barriersRoot, new Vector3(28f, 0f, 0f), true, 6.2f, "Barrier_M3_E1_In");
        var bE1_out = CreateBarrier(barriersRoot, new Vector3(48f, 0f, 0f), true, 6.2f, "Barrier_M3_E1_Out");
        var bE2_in  = CreateBarrier(barriersRoot, new Vector3(60f, 0f, 0f), true, 6.2f, "Barrier_M3_E2_In");
        var bE2_out = CreateBarrier(barriersRoot, new Vector3(80f, 0f, 0f), true, 6.2f, "Barrier_M3_E2_Out");
        var bE3_in  = CreateBarrier(barriersRoot, new Vector3(92f, 0f, 0f), true, 6.2f, "Barrier_M3_E3_Boss");

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 5, DungeonRoom.RoomType.SunkenHall,
            new Rect(28f, -11f, 20f, 22f),
            "NHÁNH ĐÔNG 1: ĐẢO SƯƠNG MÙ ĐÔNG", "EAST PATH 1: MISTY EAST ISLE",
            "Lối đi Cân Bằng! Vượt qua đội tuần tra Bộ Xương và Yêu Tinh!",
            "Balanced Path! Defeat the Skeleton & Goblin patrol!",
            bE1_in, bE1_out, bE2_in,
            r => StartCoroutine(RunMap3CombatRoomRoutine(r, RouteDensity.BalancedMonsters, 1))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 6, DungeonRoom.RoomType.CursedCrypt,
            new Rect(60f, -11f, 20f, 22f),
            "NHÁNH ĐÔNG 2: ĐẢO HỒI ỨC TAN VỠ", "EAST PATH 2: SHATTERED MEMORY ISLE",
            "Dọn sạch chốt chặn cuối trước Điện Thờ Bản Ngã!",
            "Clear the final guard post before the Mirror Sanctum!",
            bE2_in, bE2_out, bE3_in,
            r => StartCoroutine(RunMap3CombatRoomRoutine(r, RouteDensity.BalancedMonsters, 2))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 7, DungeonRoom.RoomType.TitanSanctum,
            new Rect(92f, -14f, 26f, 28f),
            "ĐẤU TRƯỜNG ĐÔNG: BÓNG MA BẢN NGÃ", "EAST SANCTUM: SHADOW DOPPELGANGER",
            "ĐẠI CHIẾN TRÙM NHÁNH ĐÔNG: BÓNG MA BẢN NGÃ TỐI THƯỢNG!",
            "EAST ROUTE FINAL BOSS: SUPREME SHADOW DOPPELGANGER!",
            bE3_in, null, null,
            r => StartCoroutine(RunBossRoomRoutine(r, RouteBossType.ShadowDoppelganger, isMap3: true))
        ));

        // --- NHÁNH TÂY (-X): ÍT QUÁI TINH ANH -> BOSS CỰ THẦN HƯ KHÔNG ---
        var bW1_in  = CreateBarrier(barriersRoot, new Vector3(-28f, 0f, 0f), true, 6.2f, "Barrier_M3_W1_In");
        var bW1_out = CreateBarrier(barriersRoot, new Vector3(-48f, 0f, 0f), true, 6.2f, "Barrier_M3_W1_Out");
        var bW2_in  = CreateBarrier(barriersRoot, new Vector3(-60f, 0f, 0f), true, 6.2f, "Barrier_M3_W2_In");
        var bW2_out = CreateBarrier(barriersRoot, new Vector3(-80f, 0f, 0f), true, 6.2f, "Barrier_M3_W2_Out");
        var bW3_in  = CreateBarrier(barriersRoot, new Vector3(-92f, 0f, 0f), true, 6.2f, "Barrier_M3_W3_Boss");

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 8, DungeonRoom.RoomType.SunkenHall,
            new Rect(-48f, -11f, 20f, 22f),
            "NHÁNH TÂY 1: ĐẢO TỊNH MẶC TÂY", "WEST PATH 1: SILENT WEST ISLE",
            "Lối đi Ít Quái Tinh Anh! Hạ gục nhanh kỵ sĩ xương tinh nhuệ!",
            "Low-Monster Elite Path! Swiftly slay the elite bone knights!",
            bW1_in, bW1_out, bW2_in,
            r => StartCoroutine(RunMap3CombatRoomRoutine(r, RouteDensity.EliteFewMonsters, 1))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 9, DungeonRoom.RoomType.CursedCrypt,
            new Rect(-80f, -11f, 20f, 22f),
            "NHÁNH TÂY 2: ĐẢO HỘ VỆ CỔ ĐẠI", "WEST PATH 2: ANCIENT GUARDIAN ISLE",
            "Vượt qua hộ vệ cuối cùng trước Vực Thẳm Cự Thần!",
            "Overcome the final elites before the Abyssal Golem Sanctum!",
            bW2_in, bW2_out, bW3_in,
            r => StartCoroutine(RunMap3CombatRoomRoutine(r, RouteDensity.EliteFewMonsters, 2))
        ));

        mgr.rooms.Add(CreateCustomRoom(
            roomsRoot, 10, DungeonRoom.RoomType.TitanSanctum,
            new Rect(-118f, -14f, 26f, 28f),
            "ĐẤU TRƯỜNG TÂY: CỰ THẦN HƯ KHÔNG", "WEST SANCTUM: ABYSSAL STONE TITAN",
            "ĐẠI CHIẾN TRÙM NHÁNH TÂY: CỰ THẦN ĐÁ CỔ ĐẠI THỨC TỈNH!",
            "WEST ROUTE FINAL BOSS: AWAKENED ABYSSAL STONE TITAN!",
            bW3_in, null, null,
            r => StartCoroutine(RunBossRoomRoutine(r, RouteBossType.AncientValleyGolem, isMap3: true))
        ));

        // Đặt 3 Bảng Chỉ Đường Phát Sáng ngay trước 3 Cầu Đá ở Đảo Trung Tâm Map 3
        CreateRouteSignpost(
            new Vector3(0f, 9.2f, 0f),
            LocalizationManager.T(
                "<b><color=#FF6B6B>⬆ NHÁNH BẮC: QUẦN ĐẢO NHỆN CỔ</color></b>\n<size=78%><color=#FFD166>[ĐÔNG QUÁI - NHIỀU LINH HỒN]</color>\nBOSS: CHÚA TỂ NHỆN CỔ ĐẠI</size>",
                "<b><color=#FF6B6B>⬆ NORTH PATH: SPIDER ARCHIPELAGO</color></b>\n<size=78%><color=#FFD166>[MANY MONSTERS - HIGH LOOT]</color>\nBOSS: ANCIENT SPIDER LORD</size>"
            ),
            new Color(1f, 0.35f, 0.2f)
        );

        CreateRouteSignpost(
            new Vector3(13.2f, 2.2f, 0f),
            LocalizationManager.T(
                "<b><color=#4ECDC4>➔ NHÁNH ĐÔNG: HÀNH LANG BẢN NGÃ</color></b>\n<size=78%><color=#A8E6CF>[QUÁI VỪA PHẢI - CÂN BẰNG]</color>\nBOSS: BÓNG MA BẢN NGÃ</size>",
                "<b><color=#4ECDC4>➔ EAST PATH: MIRROR ARCHIPELAGO</color></b>\n<size=78%><color=#A8E6CF>[BALANCED MONSTERS]</color>\nBOSS: SHADOW DOPPELGANGER</size>"
            ),
            new Color(0.25f, 0.9f, 0.85f)
        );

        CreateRouteSignpost(
            new Vector3(-13.2f, 2.2f, 0f),
            LocalizationManager.T(
                "<b><color=#C77DFF>⬅ NHÁNH TÂY: VỰC THẲM CỰ THẦN</color></b>\n<size=78%><color=#E0AAFF>[ÍT QUÁI TINH ANH - ĐƯỜNG NHANH]</color>\nBOSS: CỰ THẦN HƯ KHÔNG</size>",
                "<b><color=#C77DFF>⬅ WEST PATH: ABYSSAL TITAN ISLES</color></b>\n<size=78%><color=#E0AAFF>[FEW ELITE MONSTERS - FAST]</color>\nBOSS: ABYSSAL STONE TITAN</size>"
            ),
            new Color(0.78f, 0.45f, 1f)
        );
    }

    private void DecorateMap3ExpansionIslands()
    {
        var deco = GameObject.Find("NatureDecorations");
        if (deco == null) return;

        Transform willowTpl = deco.transform.Find("Willow_NW");
        Transform redTreeTpl = deco.transform.Find("RedTree_West");
        Transform tealTreeTpl = deco.transform.Find("TealTree_East");
        Transform rockTpl = deco.transform.Find("RockLarge_W");

        Transform expDeco = new GameObject("Map3_ExpansionDecorations").transform;
        expDeco.SetParent(transform, false);

        Vector2[] islandCenters = new Vector2[]
        {
            new Vector2(0f, 32f), new Vector2(0f, 62f), new Vector2(0f, 95f),
            new Vector2(38f, 0f), new Vector2(70f, 0f), new Vector2(105f, 0f),
            new Vector2(-38f, 0f), new Vector2(-70f, 0f), new Vector2(-105f, 0f)
        };

        for (int i = 0; i < islandCenters.Length; i++)
        {
            Vector3 c = new Vector3(islandCenters[i].x, islandCenters[i].y, 0f);
            bool isBoss = (i % 3 == 2);
            float rx = isBoss ? 10.5f : 7.2f;
            float ry = isBoss ? 8.5f : 6.5f;

            if (willowTpl != null && isBoss)
            {
                CloneDecor(willowTpl, expDeco, c + new Vector3(-rx, ry, 0f));
                CloneDecor(willowTpl, expDeco, c + new Vector3(rx, ry, 0f));
            }
            if (redTreeTpl != null)
            {
                CloneDecor(redTreeTpl, expDeco, c + new Vector3(-rx, -ry, 0f));
            }
            if (tealTreeTpl != null)
            {
                CloneDecor(tealTreeTpl, expDeco, c + new Vector3(rx, -ry, 0f));
            }
            if (rockTpl != null && !isBoss)
            {
                CloneDecor(rockTpl, expDeco, c + new Vector3(-rx + 1.5f, ry - 1.0f, 0f));
                CloneDecor(rockTpl, expDeco, c + new Vector3(rx - 1.5f, ry - 1.0f, 0f));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 3. ĐIỀU PHỐI ĐỢT QUÁI & ĐẠI TRÙM THEO TỪNG PHÒNG (MAP 2 & MAP 3)
    // ═══════════════════════════════════════════════════════════════════════════
    private IEnumerator RunMap2CombatRoomRoutine(DungeonRoom room, RouteDensity density, int stageIndex)
    {
        yield return new WaitForSeconds(0.35f);
        if (room.entranceBarrier != null) room.entranceBarrier.Lock();
        if (room.exitBarrier != null) room.exitBarrier.Lock();

        var waveUI = FindAnyObjectByType<WaveUI>();
        int totalWaves = (density == RouteDensity.EliteFewMonsters) ? 1 : 2;

        for (int w = 1; w <= totalWaves; w++)
        {
            activeRoomMonsters.Clear();
            isSpawningRoomWave = true;

            int bloodCount = 0, spitterCount = 0, beastCount = 0;
            float eliteHpMult = 1.0f;

            if (density == RouteDensity.SwarmManyMonsters)
            {
                bloodCount   = (stageIndex == 1) ? (3 + w) : (4 + w);
                spitterCount = 2;
                beastCount   = (w == 2) ? 2 : 1;
            }
            else if (density == RouteDensity.BalancedMonsters)
            {
                bloodCount   = 2 + (w - 1);
                spitterCount = 1 + (stageIndex - 1);
                beastCount   = 1 + (w - 1);
            }
            else // EliteFewMonsters
            {
                bloodCount   = 1;
                spitterCount = (stageIndex == 2) ? 1 : 0;
                beastCount   = 2;
                eliteHpMult  = 1.35f;
            }

            int totalCount = bloodCount + spitterCount + beastCount;
            if (waveUI != null)
            {
                waveUI.ShowBanner(
                    room.GetLocalizedTitle(),
                    LocalizationManager.T($"Đợt {w} / {totalWaves} — Tiêu diệt {totalCount} quái vật!", $"Wave {w} / {totalWaves} — Slay {totalCount} monsters!"),
                    2.2f
                );
            }

            int spawnIdx = 0;
            for (int i = 0; i < bloodCount; i++)
            {
                Vector3 pos = GetSafeRoomSpawnPos(room, spawnIdx++, totalCount);
                var bm = BloodMonsterAI.Spawn(pos, isBoss: false, index: i + 1);
                if (bm != null)
                {
                    if (eliteHpMult > 1.05f) bm.MultiplyHealth(eliteHpMult);
                    activeRoomMonsters.Add(bm.gameObject);
                }
                yield return new WaitForSeconds(0.18f);
            }
            for (int i = 0; i < spitterCount; i++)
            {
                Vector3 pos = GetSafeRoomSpawnPos(room, spawnIdx++, totalCount);
                var sp = ValleySpitterAI.Spawn(pos, i + 1);
                if (sp != null)
                {
                    if (eliteHpMult > 1.05f) sp.MultiplyHealth(eliteHpMult);
                    activeRoomMonsters.Add(sp.gameObject);
                }
                yield return new WaitForSeconds(0.18f);
            }
            for (int i = 0; i < beastCount; i++)
            {
                Vector3 pos = GetSafeRoomSpawnPos(room, spawnIdx++, totalCount);
                var vb = ValleyBeastAI.Spawn(pos, i + 1);
                if (vb != null)
                {
                    if (eliteHpMult > 1.05f) vb.MultiplyHealth(eliteHpMult);
                    activeRoomMonsters.Add(vb.gameObject);
                }
                yield return new WaitForSeconds(0.18f);
            }

            isSpawningRoomWave = false;
            yield return StartCoroutine(WaitUntilActiveMonstersCleared());

            if (w < totalWaves)
            {
                AethelShard.SpawnShards(room.roomCenter, density == RouteDensity.SwarmManyMonsters ? 12 : 8);
                yield return new WaitForSeconds(1.8f);
            }
        }

        CompleteCombatRoom(room, density);
    }

    private IEnumerator RunMap3CombatRoomRoutine(DungeonRoom room, RouteDensity density, int stageIndex)
    {
        yield return new WaitForSeconds(0.35f);
        if (room.entranceBarrier != null) room.entranceBarrier.Lock();
        if (room.exitBarrier != null) room.exitBarrier.Lock();

        var waveUI = FindAnyObjectByType<WaveUI>();
        int totalWaves = (density == RouteDensity.EliteFewMonsters) ? 1 : 2;

        for (int w = 1; w <= totalWaves; w++)
        {
            activeRoomMonsters.Clear();
            isSpawningRoomWave = true;

            int goblinCount = 0, skelCount = 0, slimeCount = 0, bloodCount = 0;

            if (density == RouteDensity.SwarmManyMonsters)
            {
                goblinCount = 4 + w;
                skelCount   = (stageIndex == 1) ? (2 + w) : (4 + w);
                slimeCount  = 3;
            }
            else if (density == RouteDensity.BalancedMonsters)
            {
                goblinCount = 2 + (w - 1);
                skelCount   = 2 + stageIndex;
                slimeCount  = 1;
            }
            else // EliteFewMonsters
            {
                goblinCount = 0;
                skelCount   = 2;
                bloodCount  = 1 + (stageIndex - 1);
            }

            int totalCount = goblinCount + skelCount + slimeCount + bloodCount;
            if (waveUI != null)
            {
                waveUI.ShowBanner(
                    room.GetLocalizedTitle(),
                    LocalizationManager.T($"Đợt {w} / {totalWaves} — Tiêu diệt {totalCount} quái vật!", $"Wave {w} / {totalWaves} — Slay {totalCount} monsters!"),
                    2.2f
                );
            }

            int spawnIdx = 0;
            for (int i = 0; i < skelCount; i++)
            {
                Vector3 pos = GetSafeRoomSpawnPos(room, spawnIdx++, totalCount);
                GameObject s = Map3WaveSpawner.SpawnSkeletonStatic(pos);
                if (s != null) activeRoomMonsters.Add(s);
                yield return new WaitForSeconds(0.16f);
            }
            for (int i = 0; i < goblinCount; i++)
            {
                Vector3 pos = GetSafeRoomSpawnPos(room, spawnIdx++, totalCount);
                GameObject g = Map3WaveSpawner.SpawnGoblinStatic(pos);
                if (g != null) activeRoomMonsters.Add(g);
                yield return new WaitForSeconds(0.16f);
            }
            for (int i = 0; i < slimeCount; i++)
            {
                Vector3 pos = GetSafeRoomSpawnPos(room, spawnIdx++, totalCount);
                GameObject sl = Map3WaveSpawner.SpawnSlimeStatic(pos);
                if (sl != null) activeRoomMonsters.Add(sl);
                yield return new WaitForSeconds(0.16f);
            }
            for (int i = 0; i < bloodCount; i++)
            {
                Vector3 pos = GetSafeRoomSpawnPos(room, spawnIdx++, totalCount);
                var bm = BloodMonsterAI.Spawn(pos, isBoss: false, index: i + 1);
                if (bm != null) activeRoomMonsters.Add(bm.gameObject);
                yield return new WaitForSeconds(0.16f);
            }

            isSpawningRoomWave = false;
            yield return StartCoroutine(WaitUntilActiveMonstersCleared());

            if (w < totalWaves)
            {
                AethelShard.SpawnShards(room.roomCenter, density == RouteDensity.SwarmManyMonsters ? 14 : 9);
                yield return new WaitForSeconds(1.8f);
            }
        }

        CompleteCombatRoom(room, density);
    }

    private IEnumerator RunBossRoomRoutine(DungeonRoom room, RouteBossType bossType, bool isMap3)
    {
        yield return new WaitForSeconds(0.35f);
        if (room.entranceBarrier != null) room.entranceBarrier.Lock();
        if (room.exitBarrier != null) room.exitBarrier.Lock();

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.45f, 0.55f);

        yield return new WaitForSeconds(0.85f);

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
        else if (bossType == RouteBossType.AncientValleyGolem)
        {
            GameObject golemObj = new GameObject("Boss_AncientValleyGolem");
            golemObj.transform.position = bossSpawnPos;
            var golem = golemObj.AddComponent<ValleyGolemBossAI>();
            EnemyHealthHelper.OnEnemySpawned(golemObj);
            activeRoomMonsters.Add(golemObj);
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
        else if (bossType == RouteBossType.AncientSpiderKing)
        {
            // Triệu hồi Chúa Tể Nhện Cổ Đại trực tiếp tại tâm đấu trường Bắc
            var skBoss = Map3WaveSpawner.SpawnSkeletonKingStatic(bossSpawnPos);
            if (skBoss != null) activeRoomMonsters.Add(skBoss.gameObject);
            isSpawningRoomWave = false;
            yield return StartCoroutine(WaitUntilActiveMonstersCleared());
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

            // Nếu không phải SkeletonKingBossAI (vì SkeletonKingBossAI tự sinh cổng khi chết), sinh Cổng Chiến Thắng & Thương Nhân tại phòng Boss này
            if (bossType != RouteBossType.AncientSpiderKing)
            {
                yield return new WaitForSeconds(1.2f);
                Vector3 portalPos = room.roomCenter + new Vector3(0f, 2.2f, 0f);
                RuinPortal.SpawnPortal(portalPos);
                if (isMap3)
                {
                    CorruptedVoidPortal.Spawn(room.roomCenter + new Vector3(3.0f, 2.2f, 0f));
                }
                WanderingMerchant.SpawnMerchant(room.roomCenter + new Vector3(-3.2f, 1.8f, 0f));
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

        int rewardShards = density == RouteDensity.SwarmManyMonsters ? 18 :
                           density == RouteDensity.BalancedMonsters ? 12 : 10;
        AethelShard.SpawnShards(room.roomCenter, rewardShards);
        HealthPotion.Spawn(room.roomCenter + new Vector3(0f, -0.5f, 0f), 1);
        WorldItemPickup.RollDrop(room.roomCenter + new Vector3(0.6f, 0f, 0f), false, 0.35f);

        var waveUI = FindAnyObjectByType<WaveUI>();
        if (waveUI != null)
        {
            waveUI.ShowBanner(
                LocalizationManager.T("KHU VỰC ĐÃ DỌN SẠCH!", "CHAMBER CLEARED!"),
                LocalizationManager.T($"Cổng phong ấn đã mở! (+{rewardShards} Mảnh Linh Hồn)", $"Barrier unlocked! (+{rewardShards} Shards)"),
                2.5f
            );
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 4. TIỆN ÍCH KHỞI TẠO PHÒNG, CỔNG PHONG ẤN & BẢNG CHỈ ĐƯỜNG
    // ═══════════════════════════════════════════════════════════════════════════
    private void AddWalkableRect(int xMin, int yMin, int width, int height)
    {
        walkableRects.Add(new RectInt(xMin, yMin, width, height));
    }

    private Vector3 GetSafeRoomSpawnPos(DungeonRoom room, int index, int total)
    {
        Vector3 c = room.roomCenter;
        float rx = Mathf.Max(2.5f, room.roomBounds.width * 0.5f - 3.2f);
        float ry = Mathf.Max(2.5f, room.roomBounds.height * 0.5f - 3.2f);

        float angle = (index / (float)Mathf.Max(1, total)) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
        float distRatio = Random.Range(0.45f, 0.85f);
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

    private void CreateRouteSignpost(Vector3 position, string text, Color glowColor)
    {
        GameObject signObj = new GameObject("RouteSignpost_Beacon");
        signObj.transform.SetParent(transform, false);
        signObj.transform.position = position;

        var sr = signObj.AddComponent<SpriteRenderer>();
        sr.sprite = MagmaCalamityHazardManager.GetSoftCircleSprite();
        sr.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.42f);
        sr.sortingOrder = 5;
        signObj.transform.localScale = new Vector3(1.8f, 0.9f, 1f);

        var lt = signObj.AddComponent<Light2D>();
        if (lt != null)
        {
            lt.lightType = Light2D.LightType.Point;
            lt.color = glowColor;
            lt.intensity = 1.4f;
            lt.pointLightInnerRadius = 0.5f;
            lt.pointLightOuterRadius = 3.5f;
        }

        GameObject txtObj = new GameObject("SignpostText");
        txtObj.transform.SetParent(signObj.transform, false);
        txtObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        txtObj.transform.localScale = new Vector3(1f / 1.8f, 1f / 0.9f, 1f);

        var tmp = txtObj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 2.6f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineWidth = 0.22f;
        tmp.outlineColor = new Color32(0, 0, 0, 235);
        tmp.sortingOrder = 4900;

        var rt = txtObj.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(8.5f, 2.5f);
    }

    private void CloneDecor(Transform template, Transform parent, Vector3 pos)
    {
        if (template == null) return;
        GameObject clone = Instantiate(template.gameObject, pos, Quaternion.identity, parent);
        clone.name = template.name + "_Clone";
    }
}
