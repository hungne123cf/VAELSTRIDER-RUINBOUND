using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DungeonRoomManager : MonoBehaviour
{
    public static DungeonRoomManager Instance { get; private set; }

    [Header("Rooms")]
    public List<DungeonRoom> rooms = new List<DungeonRoom>();
    public DungeonRoom currentRoom;

    private WaveSpawner waveSpawner;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        waveSpawner = FindAnyObjectByType<WaveSpawner>();
        RoomWaypointUI.EnsureUIExists();

        // Start player at Room 1 Sanctuary
        if (rooms.Count > 0)
        {
            currentRoom = rooms[0];
            AnnounceRoom(rooms[0]);
        }
    }

    public void OnPlayerEnterRoom(DungeonRoom room)
    {
        currentRoom = room;
        Debug.Log($"<color=cyan>[ROOM]</color> Người chơi bước vào {room.roomTitle}!");

        AnnounceRoom(room);

        switch (room.roomType)
        {
            case DungeonRoom.RoomType.Sanctuary:
                // Safe room, no combat
                break;

            case DungeonRoom.RoomType.SunkenHall:
                // Sảnh 2: Điều phối Đợt 1 đến Đợt 4 / 10
                StartCoroutine(RunRoom2WavesRoutine(room));
                break;

            case DungeonRoom.RoomType.CursedCrypt:
                // Hầm Mộ 3: Điều phối Đợt 5 đến Đợt 9 / 10
                StartCoroutine(RunRoom3WavesRoutine(room));
                break;

            case DungeonRoom.RoomType.TitanSanctum:
                // Đại Điện 4: Đợt 10 / 10 - Đại Chiến Boss Titan Wheel!
                StartCoroutine(RunRoom4BossRoutine(room));
                break;
        }
    }

    private IEnumerator LockBarrierWhenSafeRoutine(RoomBarrier barrier, DungeonRoom room)
    {
        if (barrier == null) yield break;

        yield return new WaitForSeconds(0.4f);
        barrier.Lock();
    }

    private void AnnounceRoom(DungeonRoom room)
    {
        var waveUI = FindAnyObjectByType<WaveUI>();
        if (waveUI != null && room != null)
        {
            waveUI.ShowBanner(room.GetLocalizedTitle(), room.GetLocalizedSubtitle());
        }
    }

    private List<Vector3> GenerateRoomSpawns(DungeonRoom room, int count)
    {
        List<Vector3> list = new List<Vector3>();
        if (room == null) return list;

        Vector3 c = room.roomCenter;
        float rx = Mathf.Max(2.5f, (room.roomBounds.width * 0.5f) - 3.0f);
        float ry = Mathf.Max(2.5f, (room.roomBounds.height * 0.5f) - 3.0f);

        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * 360f * Mathf.Deg2Rad;
            float r = Random.Range(2.0f, Mathf.Min(rx, ry));
            Vector3 offset = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * (r * 0.75f), 0f);
            list.Add(c + offset);
        }
        return list;
    }

    private IEnumerator RunRoom2WavesRoutine(DungeonRoom room)
    {
        yield return StartCoroutine(LockBarrierWhenSafeRoutine(room.entranceBarrier, room));
        if (room.exitBarrier != null) room.exitBarrier.Lock();

        if (waveSpawner == null) waveSpawner = FindAnyObjectByType<WaveSpawner>();
        var waveUI = FindAnyObjectByType<WaveUI>();

        // Phòng 2: Điều phối 4 Đợt quái (Đợt 1 đến Đợt 4 / 10)
        for (int w = 1; w <= 4; w++)
        {
            int count = 2 + w; // Đợt 1: 3, Đợt 2: 4, Đợt 3: 5, Đợt 4: 6
            List<Vector3> spawns = GenerateRoomSpawns(room, count);

            string wavePrefix = LocalizationManager.Get("HUD_WAVE");
            string title = $"{wavePrefix} {w} / 10";
            if (w == 1) title += ": " + LocalizationManager.T("VỆ BINH TUẦN TRA", "PATROL GUARDS");
            else if (w == 2) title += ": " + LocalizationManager.T("TIẾP VIỆN ĐỘI BÁNH XE", "WHEEL REINFORCEMENTS");
            else if (w == 3) title += ": " + LocalizationManager.T("BẦY BÁNH XE THỨC TỈNH", "AWAKENED WHEEL HORDE");
            else if (w == 4) title += ": " + LocalizationManager.T("ĐỢT TẬP KÍCH CUỐI SẢNH 2", "FINAL SUNKEN HALL ATTACK");

            bool waveCleared = false;
            waveSpawner.SpawnRoomWave(w, 10, title, spawns, false, () => waveCleared = true);

            while (!waveCleared) yield return null;

            if (w < 4)
            {
                if (waveUI != null)
                {
                    waveUI.ShowBanner(
                        LocalizationManager.T($"HOÀN THÀNH ĐỢT {w} / 10!", $"WAVE {w} / 10 CLEARED!"),
                        LocalizationManager.T("+8 Mảnh Linh Hồn. Chuẩn bị đợt tiếp theo...", "+8 Shards. Prepare for next wave..."),
                        2.5f
                    );
                }
                AethelShard.SpawnShards(room.roomCenter, 8);
                yield return new WaitForSeconds(2.8f);
            }
        }

        OnRoomCombatFinished(room);
    }

    private IEnumerator RunRoom3WavesRoutine(DungeonRoom room)
    {
        yield return StartCoroutine(LockBarrierWhenSafeRoutine(room.entranceBarrier, room));
        if (room.exitBarrier != null) room.exitBarrier.Lock();

        if (waveSpawner == null) waveSpawner = FindAnyObjectByType<WaveSpawner>();
        var waveUI = FindAnyObjectByType<WaveUI>();

        // Phòng 3: Điều phối 5 Đợt quái (Đợt 5 đến Đợt 9 / 10)
        for (int w = 5; w <= 9; w++)
        {
            int count = Mathf.Clamp(4 + (w - 4), 5, 8); // Đợt 5: 5, Đợt 6: 6, Đợt 7: 7, Đợt 8: 8, Đợt 9: 8
            List<Vector3> spawns = GenerateRoomSpawns(room, count);

            string wavePrefix = LocalizationManager.Get("HUD_WAVE");
            string title = $"{wavePrefix} {w} / 10";
            if (w == 5) title += ": " + LocalizationManager.T("HẦM MỘ BỊ NGUYỀN RỦA", "CURSED CRYPT");
            else if (w == 6) title += ": " + LocalizationManager.T("BẦY QUÁI VẬT CUỒNG NỘ", "ENRAGED MONSTERS");
            else if (w == 7) title += ": " + LocalizationManager.T("BINH ĐOÀN ĐÁ ĐEN", "DARK STONE LEGION");
            else if (w == 8) title += ": " + LocalizationManager.T("TẬP KÍCH BÓNG TỐI", "SHADOW AMBUSH");
            else if (w == 9) title += ": " + LocalizationManager.T("ĐỢT QUÁI CUỐI TRƯỚC ĐẠI ĐIỆN!", "FINAL SWARM BEFORE SANCTUM!");

            bool waveCleared = false;
            waveSpawner.SpawnRoomWave(w, 10, title, spawns, false, () => waveCleared = true);

            while (!waveCleared) yield return null;

            if (w < 9)
            {
                if (waveUI != null)
                {
                    waveUI.ShowBanner(
                        LocalizationManager.T($"HOÀN THÀNH ĐỢT {w} / 10!", $"WAVE {w} / 10 CLEARED!"),
                        LocalizationManager.T("+10 Mảnh Linh Hồn. Chuẩn bị đợt tiếp theo...", "+10 Shards. Prepare for next wave..."),
                        2.5f
                    );
                }
                AethelShard.SpawnShards(room.roomCenter, 10);
                yield return new WaitForSeconds(2.8f);
            }
        }

        OnRoomCombatFinished(room);
    }

    private IEnumerator RunRoom4BossRoutine(DungeonRoom room)
    {
        yield return StartCoroutine(LockBarrierWhenSafeRoutine(room.entranceBarrier, room));
        if (room.exitBarrier != null) room.exitBarrier.Lock();

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.4f, 0.5f);

        yield return new WaitForSeconds(1.0f);

        if (waveSpawner == null) waveSpawner = FindAnyObjectByType<WaveSpawner>();
        string wavePrefix = LocalizationManager.Get("HUD_WAVE");
        string title = LocalizationManager.T($"CẢNH BÁO: TRÙM TITAN WHEEL! ({wavePrefix} 10 / 10)", $"BOSS INCOMING: TITAN WHEEL! ({wavePrefix} 10 / 10)");

        List<Vector3> spawns = new List<Vector3> { room.roomCenter };
        waveSpawner.SpawnRoomWave(10, 10, title, spawns, true, () =>
        {
            OnRoomCombatFinished(room);
        });
    }

    public void OnBossDefeated(DungeonRoom room = null)
    {
        if (room == null)
        {
            room = rooms.Find(r => r != null && r.roomType == DungeonRoom.RoomType.TitanSanctum) ?? currentRoom;
        }
        if (room == null || room.isCleared) return;
        OnRoomCombatFinished(room);
    }

    private void OnRoomCombatFinished(DungeonRoom room)
    {
        if (room == null || room.isCleared) return;

        // BẢO VỆ CHẶT CHẼ: Kiểm tra thực sự xem còn quái vật gốc sống nào trong scene không (bỏ qua phân thân)
        var allBots = FindObjectsByType<BotWheelAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var b in allBots)
        {
            if (b != null && !b.isClone && !b.IsDead)
            {
                Debug.LogWarning($"<color=red>[ROOM GUARD]</color> Quái {b.name} vẫn còn sống! Không được phép mở cổng phòng {room.roomTitle}.");
                return;
            }
        }

        var allBloods = FindObjectsByType<BloodMonsterAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var bm in allBloods)
        {
            if (bm != null && !bm.IsDead)
            {
                Debug.LogWarning($"<color=red>[ROOM GUARD]</color> Quái {bm.name} vẫn còn sống! Không được phép mở cổng phòng {room.roomTitle}.");
                return;
            }
        }

        var doppel = FindAnyObjectByType<ShadowDoppelgangerBoss>();
        if (doppel != null && !doppel.IsDead)
        {
            Debug.LogWarning("[ROOM GUARD] Bóng Ma Bản Ngã vẫn còn sống! Không được phép mở cổng.");
            return;
        }

        Debug.Log($"<color=yellow>[ROOM CLEARED]</color> {room.roomTitle} đã được quét sạch!");
        room.OnRoomCleared();

        var waveUI = FindAnyObjectByType<WaveUI>();
        if (room.roomType == DungeonRoom.RoomType.TitanSanctum)
        {
            PlayerPrefs.SetInt("Map1_Completed", 1);
            PlayerPrefs.Save();

            if (waveUI != null)
            {
                waveUI.ShowBanner(
                    LocalizationManager.T("KHU VỰC ĐÃ ĐƯỢC QUÉT SẠCH!", "REALM PURIFIED!"),
                    LocalizationManager.T("Cổng cổ tích đã mở ở phía trên!", "The Ruin Portal has opened ahead!")
                );
            }
            // Spawn Victory Portal cách vị trí trung tâm 2.5m về phía trên và có trễ 1.2s để tránh đè lên người chơi
            StartCoroutine(SpawnBossPortalDelayed(room.roomCenter + new Vector3(0f, 2.5f, 0f), 1.2f));
        }
        else
        {
            if (waveUI != null)
            {
                waveUI.ShowBanner(
                    LocalizationManager.T("KHU VỰC ĐÃ DỌN SẠCH!", "ROOM CLEARED!"),
                    LocalizationManager.T("Cổng phong ấn đã mở. Hãy tiến lên phía trước!", "The sealed barrier has opened. Move forward!")
                );
            }
        }
    }

    public Vector3 GetNextObjectiveTarget()
    {
        if (currentRoom == null || rooms.Count == 0) return Vector3.zero;

        int nextIdx = currentRoom.roomIndex; // 1-based, so nextIdx is the index of the next room
        if (currentRoom.isCleared && nextIdx < rooms.Count)
        {
            return rooms[nextIdx].roomCenter;
        }

        return currentRoom.roomCenter;
    }
    private IEnumerator SpawnBossPortalDelayed(Vector3 pos, float delay)
    {
        yield return new WaitForSeconds(delay);
        var portal = RuinPortal.SpawnPortal(pos);
        if (portal != null)
        {
            Debug.Log($"<color=green>[PORTAL SPAWNED]</color> Cổng chiến thắng đã xuất hiện rực rỡ tại {pos}!");
        }
        else
        {
            Debug.LogError("<color=red>[PORTAL ERROR]</color> Không thể spawn RuinPortal!");
        }

        // Xuất hiện Thương Nhân Tàn Tích (Wandering Merchant) ngay cạnh Cổng Không Gian
        Vector3 merchantPos = pos + new Vector3(-3.2f, -0.4f, 0f);
        var merchant = WanderingMerchant.SpawnMerchant(merchantPos);
        if (merchant != null)
        {
            Debug.Log($"<color=yellow>[MERCHANT SPAWNED]</color> Thương nhân cổ đại đã xuất hiện tại {merchantPos}!");
        }
    }
}