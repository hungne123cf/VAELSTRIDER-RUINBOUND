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
    public string customTitleVN = "";
    public string customTitleEN = "";
    public string customSubtitleVN = "";
    public string customSubtitleEN = "";
    public System.Action<DungeonRoom> customEnterHandler;
    public Rect roomBounds;
    public Vector3 roomCenter;

    [Header("Spawn Points & Barriers")]
    public List<Vector3> enemySpawns = new List<Vector3>();
    public RoomBarrier entranceBarrier;
    public RoomBarrier exitBarrier;
    public RoomBarrier nextEntranceBarrier;
    public GameObject rewardObject;

    [Header("State")]
    public bool isEntered = false;
    public bool isCleared = false;

    private static PlayerController s_cachedPlayer;
    private static Rigidbody2D s_cachedPlayerRb;

    private static PlayerController GetCachedPlayer()
    {
        if (s_cachedPlayer == null)
        {
            s_cachedPlayer = FindAnyObjectByType<PlayerController>();
            s_cachedPlayerRb = s_cachedPlayer != null ? s_cachedPlayer.GetComponent<Rigidbody2D>() : null;
        }
        return s_cachedPlayer;
    }

    void Update()
    {
        if (isEntered || isCleared) return;

        var player = GetCachedPlayer();
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

    void LateUpdate()
    {
        // Cứu hộ khẩn cấp: Chỉ kích hoạt nếu người chơi bị kẹt/lướt xuyên tường ra HẲN BÊN NGOÀI căn phòng khi đang chiến đấu.
        // Bên trong phòng, để cho hệ thống vật lý Collider (Walls & RoomBarriers) tự nhiên cản lại,
        // tuyệt đối KHÔNG ép clamp thu nhỏ biên khiến người chơi không thể đi sát hoặc chạm vào tường!
        if (isEntered && !isCleared && roomType != RoomType.Sanctuary)
        {
            var player = GetCachedPlayer();
            if (player != null)
            {
                Vector3 p = player.transform.position;
                // Chỉ kéo về khi thực sự vượt ra ngoài phạm vi tường của phòng (ngoài lề > 1.2m)
                float outerMargin = 1.2f;
                bool isOutside = p.x < (roomBounds.xMin - outerMargin) ||
                                 p.x > (roomBounds.xMax + outerMargin) ||
                                 p.y < (roomBounds.yMin - outerMargin) ||
                                 p.y > (roomBounds.yMax + outerMargin);
                if (isOutside)
                {
                    float insideX = Mathf.Clamp(p.x, roomBounds.xMin + 0.5f, roomBounds.xMax - 0.5f);
                    float insideY = Mathf.Clamp(p.y, roomBounds.yMin + 0.5f, roomBounds.yMax - 0.5f);
                    Vector3 newPos = new Vector3(insideX, insideY, 0f);
                    player.transform.position = newPos;
                    if (s_cachedPlayerRb == null) s_cachedPlayerRb = player.GetComponent<Rigidbody2D>();
                    if (s_cachedPlayerRb != null)
                    {
                        s_cachedPlayerRb.position = new Vector2(insideX, insideY);
                        s_cachedPlayerRb.linearVelocity = Vector2.zero;
                    }
                }
            }
        }
    }

    public bool IsPlayerDeepInside(Vector3 playerPos)
    {
        if (!roomBounds.Contains(new Vector2(playerPos.x, playerPos.y))) return false;

        // Nếu có cửa vào, người chơi phải bước sâu vào trong phòng cách cửa ít nhất 2.2m
        if (entranceBarrier != null)
        {
            Vector3 barrierPos = entranceBarrier.transform.position;
            float dist = Vector2.Distance(playerPos, barrierPos);
            if (dist < 2.2f)
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
        if (nextEntranceBarrier != null) nextEntranceBarrier.Unlock();
        if (rewardObject != null) rewardObject.SetActive(true);
    }

    public string GetLocalizedTitle()
    {
        if (!string.IsNullOrEmpty(customTitleVN))
        {
            return LocalizationManager.T(customTitleVN, string.IsNullOrEmpty(customTitleEN) ? customTitleVN : customTitleEN);
        }
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
        if (!string.IsNullOrEmpty(customSubtitleVN))
        {
            return LocalizationManager.T(customSubtitleVN, string.IsNullOrEmpty(customSubtitleEN) ? customSubtitleVN : customSubtitleEN);
        }
        switch (roomType)
        {
            case RoomType.Sanctuary: return LocalizationManager.T("Khu vực an toàn. Hãy mở rương và chuẩn bị sức mạnh.", "Safe zone. Open chest and prepare for battle.");
            case RoomType.SunkenHall: return LocalizationManager.T("Vượt qua 4 đợt quái tuần tra thức tỉnh (Đợt 1 - 4 / 10)!", "Clear 4 patrol waves of waking monsters (Wave 1 - 4 / 10)!");
            case RoomType.CursedCrypt: return LocalizationManager.T("Bẫy phong ấn kích hoạt! Tiêu diệt 5 đợt quái hầm mộ (Đợt 5 - 9 / 10).", "Sealed ambush! Slay 5 crypt waves (Wave 5 - 9 / 10).");
            case RoomType.TitanSanctum: return LocalizationManager.T("ĐẠI CHIẾN BOSS ĐỢT 10: CHÚA TỂ BÁNH XE KHỔNG LỒ TITAN!", "FINAL BOSS WAVE 10: THE ANCIENT TITAN WHEEL!");
            default: return "";
        }
    }
}
