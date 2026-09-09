#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public static class Map3CustomMapPlacer
{
    private const string SPRITESHEET_PATH = "Assets/ART/Blood Monster_A with shadows/Blood Monster_A.png";
    private const string ANIM_CONTROLLER_PATH = "Assets/Animation/BloodMonster/BloodMonster.controller";

    [MenuItem("Vaelstrider/Map 3/Setup Gameplay Elements on Custom Map", false, 1)]
    public static void PlaceMap3GameplayElements()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("Map3") && !activeScene.name.Contains("Ancient"))
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Xác nhận Scene",
                $"Bạn đang ở Scene '{activeScene.name}'. Bạn có muốn mở Scene 'Map3_AncientRuins' để thiết lập không?",
                "Mở Map 3", "Hủy");
            if (proceed)
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Map3_AncientRuins.unity");
            }
            else
            {
                return;
            }
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Map 3 Gameplay Elements");

        // 1. Root Container cho các thực thể Gameplay Map 3
        GameObject gameplayRoot = GameObject.Find("Map3_Gameplay_Elements");
        if (gameplayRoot == null)
        {
            gameplayRoot = new GameObject("Map3_Gameplay_Elements");
            Undo.RegisterCreatedObjectUndo(gameplayRoot, "Create Gameplay Root");
        }

        // 2. Định vị Player ở khu vực an toàn (Sanctuary)
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            var pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null) player = pc.gameObject;
        }
        if (player != null)
        {
            Undo.RecordObject(player.transform, "Move Player");
            player.transform.position = new Vector3(-3.5f, -6.0f, 0f);
            Debug.Log("<color=green>[MAP 3]</color> Đã định vị Người chơi tại vùng an toàn: (-3.5, -6.0)");
        }

        // 3. Trụ Nâng Cấp Cổ Đại (Ancient Upgrade Shrine)
        Vector3 shrinePos = new Vector3(-1.5f, -5.0f, 0f);
        var shrine = Object.FindAnyObjectByType<AncientShrine>();
        if (shrine != null)
        {
            Undo.RecordObject(shrine.transform, "Reposition Shrine");
            shrine.transform.position = shrinePos;
            shrine.EnsurePillarVisuals();
            shrine.transform.SetParent(gameplayRoot.transform);
        }
        else
        {
            shrine = AncientShrine.SpawnShrine(shrinePos);
            if (shrine != null)
            {
                shrine.transform.SetParent(gameplayRoot.transform);
                Undo.RegisterCreatedObjectUndo(shrine.gameObject, "Spawn Shrine");
            }
        }
        Debug.Log("<color=cyan>[MAP 3]</color> Đã thiết lập Trụ Nâng Cấp (Ancient Shrine) tại: " + shrinePos);

        // 4. Rương Kho Báu Khởi Đầu (Treasure Chest)
        Vector3 chestPos = new Vector3(-5.0f, -5.0f, 0f);
        var chest = Object.FindAnyObjectByType<TreasureChest>();
        if (chest != null)
        {
            Undo.RecordObject(chest.transform, "Move Chest");
            chest.transform.position = chestPos;
            chest.transform.SetParent(gameplayRoot.transform);
        }
        else
        {
            // Kiểm tra tên Map2_GoldenTreasureChest cũ
            var oldChest = GameObject.Find("Map2_GoldenTreasureChest");
            if (oldChest != null)
            {
                Undo.RecordObject(oldChest.transform, "Move Chest");
                oldChest.transform.position = chestPos;
                oldChest.transform.SetParent(gameplayRoot.transform);
            }
        }

        // 5. Cầu thang & Góc nhìn trên cao (Camera Elevation Trigger)
        Vector3 stairsPos = new Vector3(4.0f, -2.5f, 0f);
        var elevationObj = GameObject.Find("Camera_Elevation_Trigger");
        if (elevationObj == null)
        {
            elevationObj = new GameObject("Camera_Elevation_Trigger");
            Undo.RegisterCreatedObjectUndo(elevationObj, "Create Elevation Trigger");
        }
        elevationObj.transform.SetParent(gameplayRoot.transform);
        elevationObj.transform.position = stairsPos;
        var elevCol = elevationObj.GetComponent<BoxCollider2D>();
        if (elevCol == null) elevCol = elevationObj.AddComponent<BoxCollider2D>();
        elevCol.isTrigger = true;
        elevCol.size = new Vector2(4.5f, 4.0f);
        if (elevationObj.GetComponent<CameraElevationTrigger>() == null)
        {
            elevationObj.AddComponent<CameraElevationTrigger>();
        }

        // 6. Cổng Ngăn Chặn Lối Vào (Room Barrier)
        Vector3 barrierPos = new Vector3(1.8f, -0.5f, 0f);
        RoomBarrier entranceBarrier = null;
        var oldBarrier = GameObject.Find("Barrier_Room2");
        if (oldBarrier != null)
        {
            Undo.RecordObject(oldBarrier, "Update Barrier");
            oldBarrier.name = "Arena_Entrance_Barrier";
            oldBarrier.transform.position = barrierPos;
            oldBarrier.transform.SetParent(gameplayRoot.transform);
            entranceBarrier = oldBarrier.GetComponent<RoomBarrier>();
            if (entranceBarrier != null) entranceBarrier.Setup(true, 3.2f);
        }
        else
        {
            var existing = GameObject.Find("Arena_Entrance_Barrier");
            if (existing != null)
            {
                entranceBarrier = existing.GetComponent<RoomBarrier>();
                existing.transform.position = barrierPos;
            }
            else
            {
                entranceBarrier = RoomBarrier.Create(barrierPos, true, 3.2f, "Arena_Entrance_Barrier");
                Undo.RegisterCreatedObjectUndo(entranceBarrier.gameObject, "Create Barrier");
            }
            if (entranceBarrier != null) entranceBarrier.transform.SetParent(gameplayRoot.transform);
        }

        // 7. Xóa bỏ DummyEnemy cũ nếu có
        var dummy = GameObject.Find("DummyEnemy");
        if (dummy != null)
        {
            Undo.DestroyObjectImmediate(dummy);
        }

        // Tải Sprite và Animator cho Quái Blood Monster
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(SPRITESHEET_PATH);
        Sprite bloodMonsterSprite = null;
        foreach (var a in allAssets)
        {
            if (a is Sprite s)
            {
                bloodMonsterSprite = s;
                break;
            }
        }
        var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIM_CONTROLLER_PATH);

        // 8. Tạo 3 Quái Minion Blood Monster canh gác trên đài
        Vector3[] minionPositions = new Vector3[]
        {
            new Vector3(9.0f, 1.0f, 0f),
            new Vector3(14.0f, -2.0f, 0f),
            new Vector3(12.0f, 7.0f, 0f)
        };

        // Dọn dẹp quái minion cũ thuộc gameplayRoot
        var oldMinions = Object.FindObjectsByType<BloodMonsterAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<BloodMonsterAI> minionList = new List<BloodMonsterAI>();
        BloodMonsterAI bossAI = null;

        foreach (var bm in oldMinions)
        {
            if (bm != null && bm.gameObject != null)
            {
                if (bm.isBoss || bm.gameObject.name.Contains("Boss"))
                {
                    bossAI = bm;
                }
                else
                {
                    Undo.DestroyObjectImmediate(bm.gameObject);
                }
            }
        }

        for (int i = 0; i < minionPositions.Length; i++)
        {
            GameObject minionObj = new GameObject($"BloodMonster_Minion_{i + 1}");
            minionObj.transform.SetParent(gameplayRoot.transform);
            minionObj.transform.position = minionPositions[i];
            minionObj.transform.localScale = Vector3.one * 1.05f;

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) minionObj.layer = enemyLayer;
            minionObj.tag = "Enemy";

            var sr = minionObj.AddComponent<SpriteRenderer>();
            sr.sprite = bloodMonsterSprite;
            sr.sortingOrder = 3000;

            var anim = minionObj.AddComponent<Animator>();
            anim.runtimeAnimatorController = animController;

            var cap = minionObj.AddComponent<CapsuleCollider2D>();
            cap.size = new Vector2(1.0f, 1.0f);
            cap.direction = CapsuleDirection2D.Vertical;
            cap.isTrigger = false;

            var rb = minionObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var ai = minionObj.AddComponent<BloodMonsterAI>();
            ai.isBoss = false;

            minionList.Add(ai);
            Undo.RegisterCreatedObjectUndo(minionObj, "Create Minion");
        }

        // 9. Tạo Boss ✦ GOR'MOK • HUYẾT THÚ CỔ TỰ ✦
        Vector3 bossPos = new Vector3(18.0f, 6.0f, 0f);
        if (bossAI == null)
        {
            GameObject bossObj = new GameObject("Boss_GorMok_BloodMonster");
            bossObj.transform.SetParent(gameplayRoot.transform);
            bossObj.transform.position = bossPos;
            bossObj.transform.localScale = Vector3.one * 1.85f;

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) bossObj.layer = enemyLayer;
            bossObj.tag = "Enemy";

            var sr = bossObj.AddComponent<SpriteRenderer>();
            sr.sprite = bloodMonsterSprite;
            sr.sortingOrder = 3000;

            var anim = bossObj.AddComponent<Animator>();
            anim.runtimeAnimatorController = animController;

            var cap = bossObj.AddComponent<CapsuleCollider2D>();
            cap.size = new Vector2(1.5f, 1.4f);
            cap.direction = CapsuleDirection2D.Vertical;
            cap.isTrigger = false;

            var rb = bossObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            bossAI = bossObj.AddComponent<BloodMonsterAI>();
            bossAI.SetupBoss(1.85f, 2);

            Undo.RegisterCreatedObjectUndo(bossObj, "Create Boss GorMok");
        }
        else
        {
            Undo.RecordObject(bossAI.transform, "Move Boss");
            bossAI.transform.position = bossPos;
            bossAI.transform.localScale = Vector3.one * 1.85f;
            bossAI.transform.SetParent(gameplayRoot.transform);
            bossAI.SetupBoss(1.85f, 2);
        }
        Debug.Log("<color=red>[MAP 3]</color> Đã thiết lập Boss Gor'Mok tại trung tâm giác đấu trường: " + bossPos);

        // 10. Cổng Cổ Tích Kết Thúc (Ruin Portal)
        Vector3 portalPos = new Vector3(24.0f, 10.0f, 0f);
        var portal = Object.FindAnyObjectByType<RuinPortal>();
        if (portal != null)
        {
            Undo.RecordObject(portal.transform, "Move Portal");
            portal.transform.position = portalPos;
            portal.transform.SetParent(gameplayRoot.transform);
            HeavenlyPortalFX.Attach(portal.gameObject);
        }
        else
        {
            GameObject portalObj = new GameObject("RuinPortal");
            portalObj.transform.position = portalPos;
            portalObj.transform.SetParent(gameplayRoot.transform);
            var sr = portalObj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/ruin_portal.png");
            portalObj.AddComponent<CircleCollider2D>().isTrigger = true;
            portal = portalObj.AddComponent<RuinPortal>();
            HeavenlyPortalFX.Attach(portalObj);
            Undo.RegisterCreatedObjectUndo(portalObj, "Create Portal");
        }

        // 11. Map3ArenaManager & Vùng Trigger Trận Đấu
        var managerObj = GameObject.Find("Map3_Arena_Manager");
        if (managerObj == null)
        {
            managerObj = new GameObject("Map3_Arena_Manager");
            Undo.RegisterCreatedObjectUndo(managerObj, "Create Arena Manager");
        }
        managerObj.transform.SetParent(gameplayRoot.transform);
        managerObj.transform.position = new Vector3(5.5f, -0.5f, 0f);

        var managerCol = managerObj.GetComponent<BoxCollider2D>();
        if (managerCol == null) managerCol = managerObj.AddComponent<BoxCollider2D>();
        managerCol.isTrigger = true;
        managerCol.size = new Vector2(4.5f, 6.0f);

        var arenaMgr = managerObj.GetComponent<Map3ArenaManager>();
        if (arenaMgr == null) arenaMgr = managerObj.AddComponent<Map3ArenaManager>();
        arenaMgr.SetupReferences(entranceBarrier, bossAI, minionList, portal, managerCol);

        // 12. Tắt WaveSpawner để tránh xung đột
        var waveSpawner = Object.FindAnyObjectByType<WaveSpawner>();
        if (waveSpawner != null)
        {
            Undo.RecordObject(waveSpawner.gameObject, "Disable WaveSpawner");
            waveSpawner.gameObject.SetActive(false);
        }

        // 13. Khởi tạo sẵn UI Nâng Cấp và UI Boss
        ShrineUpgradeUI.EnsureUIExists();
        BossHealthBarUI.EnsureInstance();

        Undo.CollapseUndoOperations(undoGroup);
        var currentScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        EditorUtility.DisplayDialog(
            "Cài đặt Map 3 Thành Công!",
            "Đã gắn hoàn chỉnh các thành phần Gameplay vào bản đồ của bạn:\n\n" +
            "1. Trụ Nâng Cấp (Ancient Shrine) tại khu vực xuất phát (-1.5, -5.0).\n" +
            "2. Rương Kho Báu tại (-5.0, -5.0).\n" +
            "3. Cổng Phong Ấn (Barrier) chặn lối vào/ra tại (1.8, -0.5).\n" +
            "4. 3 Quái Huyết Thú Minion canh gác đài cao.\n" +
            "5. Đại Trùm Gor'Mok (3 Phase) tại tâm giác đấu trường (18.0, 6.0).\n" +
            "6. Cổng Cổ Tích Kết Thúc (Ruin Portal) ẩn phía sau (24.0, 10.0).\n" +
            "7. Quản lý Đấu Trường (Map3ArenaManager) tự động khóa cổng khi bạn bước vào và mở cổng + portal khi hạ Boss!",
            "Tuyệt vời!");
    }
}
#endif
