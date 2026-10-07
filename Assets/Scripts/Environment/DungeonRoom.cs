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
            if (IsPlayerDeepInside(pos))
            {
                isEntered = true;
                DungeonRoomManager.Instance?.OnPlayerEnterRoom(this);
            }
        }
    }

    public bool IsPlayerDeepInside(Vector3 playerPos)
    {
        if (!roomBounds.Contains(new Vector2(playerPos.x, playerPos.y))) return false;

        // Nếu có cửa vào, người chơi phải bước sâu vào trong phòng cách cửa ít nhất 2.0m
        if (entranceBarrier != null)
        {
            Vector3 barrierPos = entranceBarrier.transform.position;
            float dist = Vector2.Distance(playerPos, barrierPos);
            if (dist < 2.0f)
            {
                return false;
            }
        }

        return true;
    }

    public void OnRoomCleared()
    {
        isCleared = true;
        if (entranceBarrier != null) entranceBarrier.Unlock();
        if (exitBarrier != null) exitBarrier.Unlock();
        if (rewardObject != null) rewardObject.SetActive(true);
    }

    public string GetLocalizedTitle()
    {
        switch (roomType)
        {
            case RoomType.Sanctuary: return LocalizationManager.T("PHÒNG 1: THÁNH ĐỊA KHỞI ĐẦU", "ROOM 1: THE ANCIENT SANCTUARY");
            case RoomType.SunkenHall: return LocalizationManager.T("PHÒNG 2: ĐẠI SẢNH CHÌM SÂU", "ROOM 2: SUNKEN HALL OF TRIAL");
            case RoomType.CursedCrypt: return LocalizationManager.T("PHÒNG 3: HẦM MỘ NGUYỀN RỦA", "ROOM 3: CURSED CRYPT OF BONES");
            case RoomType.TitanSanctum: return LocalizationManager.T("PHÒNG 4: THÁNH ĐIỆN TITAN WHEEL", "ROOM 4: SANCTUM OF TITAN WHEEL");
            default: return LocalizationManager.T($"PHÒNG {roomIndex}", $"ROOM {roomIndex}");
        }
    }

    public string GetLocalizedSubtitle()
    {
        switch (roomType)
        {
            case RoomType.Sanctuary: return LocalizationManager.T("Khu vực an toàn. Hãy mở rương và cầu nguyện tại Đài Thần Bí.", "Safe zone. Open chest and pray at the Ancient Shrine.");
            case RoomType.SunkenHall: return LocalizationManager.T("Tiêu diệt toàn bộ bầy quái vật bánh xe thức tỉnh!", "Purge all waking wheel monsters!");
            case RoomType.CursedCrypt: return LocalizationManager.T("Bẫy phong ấn kích hoạt! Tiêu diệt bầy quái hung tợn.", "Sealed ambush! Slay the enraged swarm.");
            case RoomType.TitanSanctum: return LocalizationManager.T("ĐẠI CHIẾN BOSS: CHÚA TỂ BÁNH XE KHỔNG LỒ TITAN!", "BOSS BATTLE: THE ANCIENT TITAN WHEEL!");
            default: return "";
        }
    }
}
