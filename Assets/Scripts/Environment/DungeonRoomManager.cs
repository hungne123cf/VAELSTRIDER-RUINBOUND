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
                // Lock entrance barrier, start Wave 1
                if (room.entranceBarrier != null) room.entranceBarrier.Lock();
                StartCoroutine(TriggerCombatWaveRoutine(room, 1, 2, false));
                break;

            case DungeonRoom.RoomType.CursedCrypt:
                // Lock entrance barrier, start Wave 2
                if (room.entranceBarrier != null) room.entranceBarrier.Lock();
                StartCoroutine(TriggerCombatWaveRoutine(room, 2, 4, false));
                break;

            case DungeonRoom.RoomType.TitanSanctum:
                // Boss Room! Lock barrier, shake camera, start Boss Wave 3
                if (room.entranceBarrier != null) room.entranceBarrier.Lock();
                StartCoroutine(TriggerBossCombatRoutine(room));
                break;
        }
    }

    private void AnnounceRoom(DungeonRoom room)
    {
        var waveUI = FindAnyObjectByType<WaveUI>();
        if (waveUI != null)
        {
            waveUI.ShowBanner(room.roomTitle, room.roomSubtitle);
        }
    }

    private IEnumerator TriggerCombatWaveRoutine(DungeonRoom room, int wave, int enemyCount, bool isBoss)
    {
        yield return new WaitForSeconds(0.6f);

        if (waveSpawner == null) waveSpawner = FindAnyObjectByType<WaveSpawner>();
        if (waveSpawner != null)
        {
            waveSpawner.SpawnRoomEnemies(room.enemySpawns, isBoss, () =>
            {
                OnRoomCombatFinished(room);
            });
        }
    }

    private IEnumerator TriggerBossCombatRoutine(DungeonRoom room)
    {
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.35f, 0.45f);

        yield return new WaitForSeconds(1.0f);

        if (waveSpawner == null) waveSpawner = FindAnyObjectByType<WaveSpawner>();
        if (waveSpawner != null)
        {
            waveSpawner.SpawnRoomEnemies(room.enemySpawns, true, () =>
            {
                OnRoomCombatFinished(room);
            });
        }
    }

    private void OnRoomCombatFinished(DungeonRoom room)
    {
        Debug.Log($"<color=yellow>[ROOM CLEARED]</color> {room.roomTitle} đã được quét sạch!");
        room.OnRoomCleared();

        var waveUI = FindAnyObjectByType<WaveUI>();
        if (waveUI != null)
        {
            if (room.roomType == DungeonRoom.RoomType.TitanSanctum)
            {
                waveUI.ShowBanner("✦ REALM PURIFIED! ✦", "Stepping into the Ruin Portal...");
                // Spawn Victory Portal in Boss Room
                RuinPortal.SpawnPortal(room.roomCenter);
            }
            else
            {
                waveUI.ShowBanner("✦ KHU VỰC ĐÃ DỌN SẠCH! ✦", "Cổng phong ấn đã mở. Hãy tiến lên phía trước!");
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
}
