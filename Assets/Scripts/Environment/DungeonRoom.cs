using UnityEngine;
using System.Collections.Generic;

public class DungeonRoom : MonoBehaviour
{
    public enum RoomType
    {
        Sanctuary,      // Room 1: Safe start, Shrine & Chest
        SunkenHall,     // Room 2: Wave 1
        CursedCrypt,    // Room 3: Wave 2 & Reward Chest
        TitanSanctum    // Room 4: Boss Titan Wheel
    }

    [Header("Room Info")]
    public int roomIndex = 1;
    public RoomType roomType;
    public string roomTitle = "ROOM";
    public string roomSubtitle = "OBJECTIVE";
    public Rect roomBounds;
    public Vector3 roomCenter;

    [Header("Spawn Points & Barriers")]
    public List<Vector3> enemySpawns = new List<Vector3>();
    public RoomBarrier entranceBarrier;
    public RoomBarrier exitBarrier;
    public GameObject rewardObject;

    [Header("State")]
    public bool isEntered = false;
    public bool isCleared = false;

    void Update()
    {
        if (isEntered || isCleared) return;

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            Vector3 pos = player.transform.position;
            if (roomBounds.Contains(new Vector2(pos.x, pos.y)))
            {
                isEntered = true;
                DungeonRoomManager.Instance?.OnPlayerEnterRoom(this);
            }
        }
    }

    public void OnRoomCleared()
    {
        isCleared = true;
        if (entranceBarrier != null) entranceBarrier.Unlock();
        if (exitBarrier != null) exitBarrier.Unlock();
        if (rewardObject != null) rewardObject.SetActive(true);
    }
}
