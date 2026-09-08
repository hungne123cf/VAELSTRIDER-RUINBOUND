using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class DungeonMapBuilder : MonoBehaviour
{
    [ContextMenu("Build Full Multi-Room Dungeon")]
    public static void BuildDungeon()
    {
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
            Debug.LogError("[MAP BUILDER] Không tìm thấy Tilemap Ground hoặc Walls trong Scene!");
            return;
        }

        // 2. Load Tile Assets từ Palettes
        TileBase floorTile = null;
        TileBase wallTile = null;

#if UNITY_EDITOR
        floorTile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Palettes/TX Tileset Stone Ground_24.asset");
        wallTile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Palettes/TX Tileset Stone Ground_43.asset");
#endif

        if (floorTile == null) floorTile = groundTm.GetTile(new Vector3Int(0, 0, 0));
        if (wallTile == null) wallTile = wallsTm.GetTile(new Vector3Int(-14, 0, 0));

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

        // Tính toán vành tường kín (Wall Perimeter)
        HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>();
        foreach (var pos in floorSet)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
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

        // 5. Tạo Cửa Phong Ấn (RoomBarriers)
        Transform barriersRoot = GetOrCreateRoot("DungeonBarriers");
        ClearChildren(barriersRoot);

        // Barrier 1: Cửa vào Sảnh 2 (x = 8, y = 0, cao 3 tiles)
        var b1 = RoomBarrier.Create(new Vector3(8f, 0f, 0f), true, 3.2f, "Barrier_Room2");
        b1.transform.SetParent(barriersRoot);

        // Barrier 2: Cửa vào Hầm 3 (x = 19, y = 24, rộng 3 tiles)
        var b2 = RoomBarrier.Create(new Vector3(19f, 23.5f, 0f), false, 3.2f, "Barrier_Room3");
        b2.transform.SetParent(barriersRoot);

        // Barrier 3: Cửa vào Đại Điện Boss 4 (x = 42, y = 34, cao 3 tiles)
        var b3 = RoomBarrier.Create(new Vector3(41.5f, 34f, 0f), true, 3.2f, "Barrier_Room4_Boss");
        b3.transform.SetParent(barriersRoot);

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
        SetupDungeonRooms(b1, b2, b3);

        // 8. Đặt lại vị trí Người chơi, Bàn Thờ Cổ & Rương
        PositionGameEntities();

        Debug.Log("<color=green>[MAP BUILDER]</color> Hoàn tất kiến tạo Bản Đồ Đa Phòng: 4 Phòng, 3 Hành Lang, 18 Đuốc, 3 Cửa Phong Ấn!");
    }

    private static void SetupDungeonRooms(RoomBarrier b1, RoomBarrier b2, RoomBarrier b3)
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
        r1.roomTitle = "✦ SANCTUARY OF AWAKENING ✦";
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
        r2.roomTitle = "✦ THE SUNKEN HALL ✦";
        r2.roomSubtitle = "Tiêu diệt toán Vệ Binh Tuần Tra (Wave 1)";
        r2.roomBounds = new Rect(8, -8, 22, 20);
        r2.roomCenter = new Vector3(19, 2, 0);
        r2.enemySpawns = new List<Vector3>() { new Vector3(14, 4, 0), new Vector3(24, 0, 0) };
        r2.entranceBarrier = b1;
        mgr.rooms.Add(r2);

        // Room 3: Cursed Crypt
        var r3Obj = new GameObject("Room_3_CursedCrypt");
        r3Obj.transform.SetParent(roomsRoot);
        var r3 = r3Obj.AddComponent<DungeonRoom>();
        r3.roomIndex = 3;
        r3.roomType = DungeonRoom.RoomType.CursedCrypt;
        r3.roomTitle = "✦ THE CURSED CRYPT ✦";
        r3.roomSubtitle = "Thanh tẩy Hầm Mộ Bị Nguyền Rủa (Wave 2)";
        r3.roomBounds = new Rect(8, 24, 22, 20);
        r3.roomCenter = new Vector3(19, 34, 0);
        r3.enemySpawns = new List<Vector3>() { new Vector3(13, 38, 0), new Vector3(25, 38, 0), new Vector3(13, 30, 0), new Vector3(25, 30, 0) };
        r3.entranceBarrier = b2;
        mgr.rooms.Add(r3);

        // Room 4: Titan Sanctum (Boss)
        var r4Obj = new GameObject("Room_4_TitanSanctum");
        r4Obj.transform.SetParent(roomsRoot);
        var r4 = r4Obj.AddComponent<DungeonRoom>();
        r4.roomIndex = 4;
        r4.roomType = DungeonRoom.RoomType.TitanSanctum;
        r4.roomTitle = "✦ SANCTUM OF THE TITAN ✦";
        r4.roomSubtitle = "ĐẠI CHIẾN BOSS: TITAN WHEEL";
        r4.roomBounds = new Rect(42, 20, 28, 28);
        r4.roomCenter = new Vector3(56, 34, 0);
        r4.enemySpawns = new List<Vector3>() { new Vector3(56, 34, 0) };
        r4.entranceBarrier = b3;
        mgr.rooms.Add(r4);
    }

    private static void PositionGameEntities()
    {
        // 1. Đặt người chơi tại Phòng 1
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.transform.position = new Vector3(-9f, 0f, 0f);
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
}
