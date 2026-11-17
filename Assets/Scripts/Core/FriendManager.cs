using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class FriendEntry
{
    public string friendId;
    public string originalName;
    public string nickname;
    public int level;
    public string heroId;
    public bool isOnline;
    public string currentRoomPin;

    public FriendEntry() { }

    public FriendEntry(string id, string origName, string nick, int lvl, string hero, bool online, string pin = "")
    {
        friendId = id;
        originalName = origName;
        nickname = nick;
        level = lvl;
        heroId = hero;
        isOnline = online;
        currentRoomPin = pin;
    }

    /// <summary>
    /// Hiển thị tên theo định dạng chuẩn: TênGốc (TênGhiNhớ) nếu có nickname
    /// </summary>
    public string GetFormattedDisplayName()
    {
        if (!string.IsNullOrEmpty(nickname))
        {
            return $"{originalName} ({nickname})";
        }
        return originalName;
    }
}

[System.Serializable]
public class FriendRequestEntry
{
    public string requestId;
    public string senderName;
    public string senderHeroId;
    public int senderLevel;
    public string timestamp;

    public FriendRequestEntry() { }

    public FriendRequestEntry(string id, string sender, string hero, int lvl)
    {
        requestId = id;
        senderName = sender;
        senderHeroId = hero;
        senderLevel = lvl;
        timestamp = DateTime.Now.ToString("HH:mm");
    }
}

[System.Serializable]
public class RoomInviteEntry
{
    public string senderName;
    public string roomPin;
    public string timestamp;

    public RoomInviteEntry() { }

    public RoomInviteEntry(string sender, string pin)
    {
        senderName = sender;
        roomPin = pin;
        timestamp = DateTime.Now.ToString("HH:mm");
    }
}

[System.Serializable]
public class FriendRequestListContainer
{
    public List<FriendRequestEntry> requests = new List<FriendRequestEntry>();
}

[System.Serializable]
public class FriendListContainer
{
    public List<FriendEntry> friends = new List<FriendEntry>();
}

/// <summary>
/// Quản lý Hệ thống Bạn Bè: Thêm bạn, Xóa bạn, Đặt tên ghi nhớ (), Lưu trữ dữ liệu.
/// </summary>
public class FriendManager : MonoBehaviour
{
    public static FriendManager Instance { get; private set; }

    public static string GetCurrentAccountKey()
    {
        var acc = AccountDatabase.GetActiveAccount();
        if (acc != null && !string.IsNullOrEmpty(acc.username))
        {
            return acc.username.Trim().ToLowerInvariant();
        }
        string activeUser = PlayerPrefs.GetString(AccountDatabase.PREF_CURRENT_USER, "");
        if (!string.IsNullOrEmpty(activeUser)) return activeUser.Trim().ToLowerInvariant();
        string name = PlayerPrefs.GetString("Player_Name", "default");
        return string.IsNullOrEmpty(name) ? "default" : name.Trim().ToLowerInvariant();
    }

    private string GetFriendsPrefKey() => $"VR_FRIENDS_{GetCurrentAccountKey()}_JSON";
    private string GetRequestsPrefKey() => $"VR_FRIEND_REQ_{GetCurrentAccountKey()}_JSON";

    private readonly List<FriendEntry> _friendList = new List<FriendEntry>();
    private readonly List<FriendRequestEntry> _pendingRequests = new List<FriendRequestEntry>();
    private readonly List<RoomInviteEntry> _pendingRoomInvites = new List<RoomInviteEntry>();

    public event Action OnFriendsUpdated;
    public event Action OnFriendRequestsUpdated;
    public event Action OnRoomInvitesUpdated;
    public event Action<string, string> OnRoomInviteReceived; // senderName, roomPin

    public void ReloadForCurrentAccount()
    {
        LoadFriends();
        OnFriendsUpdated?.Invoke();
        OnFriendRequestsUpdated?.Invoke();
    }

    public static FriendManager EnsureInstance()
    {
        if (Instance == null)
        {
            var existing = FindAnyObjectByType<FriendManager>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            var go = new GameObject("FriendManager");
            Instance = go.AddComponent<FriendManager>();
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
            }
            Instance.LoadFriends();
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            LoadFriends();
        }
        else if (Instance != this)
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
    }

    public List<FriendEntry> GetFriends()
    {
        return new List<FriendEntry>(_friendList);
    }

    public FriendEntry GetFriend(string friendId)
    {
        return _friendList.Find(f => f.friendId == friendId);
    }

    public bool AddFriend(string originalName, string nickname = "", string heroId = "")
    {
        if (string.IsNullOrEmpty(originalName)) return false;

        string cleanName = originalName.Trim();
        // Kiểm tra xem đã kết bạn chưa
        var existing = _friendList.Find(f => f.originalName.Equals(cleanName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            if (!string.IsNullOrEmpty(nickname))
            {
                existing.nickname = nickname.Trim();
                SaveFriends();
                OnFriendsUpdated?.Invoke();
            }
            return false;
        }

        string newId = "FR-" + UnityEngine.Random.Range(1000, 9999).ToString();
        string chosenHero = string.IsNullOrEmpty(heroId) ? CharacterManager.CHAR_VAELSTRIDER : heroId;
        int lvl = 1;

        // Nếu người chơi có tài khoản trong hệ thống, lấy cấp độ và nhân vật thật
        var realAcc = AccountDatabase.FindAccountByName(cleanName);
        if (realAcc != null)
        {
            cleanName = !string.IsNullOrEmpty(realAcc.displayName) ? realAcc.displayName : realAcc.username;
            string norm = CharacterManager.NormalizeHeroId(chosenHero);
            lvl = AccountDatabase.GetHeroLevel(norm);
        }

        var newFriend = new FriendEntry(newId, cleanName, nickname.Trim(), lvl, chosenHero, false, "");
        _friendList.Insert(0, newFriend);

        SaveFriends();
        OnFriendsUpdated?.Invoke();
        return true;
    }

    public bool RemoveFriend(string friendId)
    {
        int index = _friendList.FindIndex(f => f.friendId == friendId);
        if (index >= 0)
        {
            _friendList.RemoveAt(index);
            SaveFriends();
            OnFriendsUpdated?.Invoke();
            return true;
        }
        return false;
    }

    public bool EditNickname(string friendId, string newNickname)
    {
        var friend = _friendList.Find(f => f.friendId == friendId);
        if (friend != null)
        {
            friend.nickname = (newNickname ?? "").Trim();
            SaveFriends();
            OnFriendsUpdated?.Invoke();
            return true;
        }
        return false;
    }

    public List<FriendRequestEntry> GetPendingRequests()
    {
        return new List<FriendRequestEntry>(_pendingRequests);
    }

    public int GetPendingRequestCount() => _pendingRequests.Count;

    public bool ReceiveFriendRequest(string senderName, string heroId, int level)
    {
        if (string.IsNullOrEmpty(senderName)) return false;
        string cleanSender = senderName.Trim();

        // Nếu đã là bạn bè rồi thì không thêm lời mời
        if (_friendList.Exists(f => f.originalName.Equals(cleanSender, StringComparison.OrdinalIgnoreCase)))
            return false;

        // Nếu đã có lời mời chờ từ người này rồi thì không thêm trùng
        if (_pendingRequests.Exists(r => r.senderName.Equals(cleanSender, StringComparison.OrdinalIgnoreCase)))
            return false;

        string reqId = "REQ-" + UnityEngine.Random.Range(1000, 9999).ToString();
        var req = new FriendRequestEntry(reqId, cleanSender, string.IsNullOrEmpty(heroId) ? CharacterManager.CHAR_VAELSTRIDER : heroId, level > 0 ? level : 1);
        _pendingRequests.Insert(0, req);
        SaveFriendRequests();
        OnFriendRequestsUpdated?.Invoke();
        return true;
    }

    public bool IsFriend(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        string clean = name.Trim();
        return _friendList.Exists(f => f.originalName.Equals(clean, StringComparison.OrdinalIgnoreCase));
    }

    public void SendFriendRequest(string targetPlayerName)
    {
        if (string.IsNullOrEmpty(targetPlayerName)) return;
        if (PvPNetworkManager.Instance != null)
        {
            PvPNetworkManager.Instance.SendFriendRequestPacket(targetPlayerName);
        }
    }

    public bool AcceptFriendRequest(string requestIdOrSenderName)
    {
        var req = _pendingRequests.Find(r => r.requestId == requestIdOrSenderName || r.senderName.Equals(requestIdOrSenderName, StringComparison.OrdinalIgnoreCase));
        if (req != null)
        {
            AddFriend(req.senderName, "", req.senderHeroId);
            _pendingRequests.Remove(req);
            SaveFriendRequests();
            OnFriendRequestsUpdated?.Invoke();

            if (PvPNetworkManager.Instance != null)
            {
                PvPNetworkManager.Instance.SendFriendAcceptPacket(req.senderName);
            }
            return true;
        }
        return false;
    }

    public bool DeclineFriendRequest(string requestIdOrSenderName)
    {
        int idx = _pendingRequests.FindIndex(r => r.requestId == requestIdOrSenderName || r.senderName.Equals(requestIdOrSenderName, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            _pendingRequests.RemoveAt(idx);
            SaveFriendRequests();
            OnFriendRequestsUpdated?.Invoke();
            return true;
        }
        return false;
    }

    public List<RoomInviteEntry> GetPendingRoomInvites()
    {
        return new List<RoomInviteEntry>(_pendingRoomInvites);
    }

    public int GetPendingRoomInviteCount() => _pendingRoomInvites.Count;

    public void AddPendingRoomInvite(string senderName, string roomPin)
    {
        if (string.IsNullOrEmpty(senderName) || string.IsNullOrEmpty(roomPin)) return;
        string cleanSender = senderName.Trim();
        string cleanPin = roomPin.Trim();

        // Tránh trùng lặp nếu người đó vừa gửi mã phòng này
        _pendingRoomInvites.RemoveAll(i => i.senderName.Equals(cleanSender, StringComparison.OrdinalIgnoreCase) && i.roomPin == cleanPin);
        _pendingRoomInvites.Insert(0, new RoomInviteEntry(cleanSender, cleanPin));

        // Giữ tối đa 10 lời mời gần nhất
        if (_pendingRoomInvites.Count > 10)
        {
            _pendingRoomInvites.RemoveRange(10, _pendingRoomInvites.Count - 10);
        }

        OnRoomInvitesUpdated?.Invoke();
        OnRoomInviteReceived?.Invoke(cleanSender, cleanPin);
    }

    public void ClearPendingRoomInvites()
    {
        _pendingRoomInvites.Clear();
        OnRoomInvitesUpdated?.Invoke();
    }

    public void RemoveRoomInvite(string roomPin)
    {
        int count = _pendingRoomInvites.RemoveAll(i => i.roomPin == roomPin);
        if (count > 0) OnRoomInvitesUpdated?.Invoke();
    }

    public void ReceiveRoomInvite(string senderName, string roomPin)
    {
        AddPendingRoomInvite(senderName, roomPin);
    }

    /// <summary>
    /// Gửi lời mời vào phòng Co-op: Hỗ trợ cả Cloud Relay (PlayFab cho 2 máy khác nhau qua mạng)
    /// và Fusion reliable data socket nếu bạn bè đang ở trong game.
    /// </summary>
    public void SendRoomInvite(string friendName, string roomPin)
    {
        if (string.IsNullOrEmpty(friendName) || string.IsNullOrEmpty(roomPin)) return;

        var acc = AccountDatabase.GetActiveAccount();
        string myName = acc != null
            ? (!string.IsNullOrEmpty(acc.displayName) ? acc.displayName : acc.username)
            : PlayerPrefs.GetString("SAVED_LOGIN_USERNAME", "Hiệp Sĩ");

        // 1. Gửi qua PlayFab Cloud Relay & Local Shared Loopback
        if (PlayFabClientService.Instance != null)
        {
            PlayFabClientService.Instance.SendRoomInviteCloud(friendName, myName, roomPin);
        }

        // 2. Gửi qua socket Fusion nếu bạn bè đang ở trong phiên
        if (PvPNetworkManager.Instance != null)
        {
            PvPNetworkManager.Instance.SendRoomInvitePacket(friendName, roomPin);
        }
    }

    public void SaveFriendRequests()
    {
        try
        {
            var container = new FriendRequestListContainer { requests = _pendingRequests };
            string json = JsonUtility.ToJson(container);
            PlayerPrefs.SetString(GetRequestsPrefKey(), json);
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[FriendManager] Save requests error: " + ex.Message);
        }
    }

    public void LoadFriendRequests()
    {
        _pendingRequests.Clear();
        string key = GetRequestsPrefKey();
        string json = PlayerPrefs.GetString(key, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var container = JsonUtility.FromJson<FriendRequestListContainer>(json);
                if (container != null && container.requests != null)
                {
                    _pendingRequests.AddRange(container.requests);
                }
            }
            catch (Exception) { }
        }
    }

    public void SaveFriends()
    {
        try
        {
            var container = new FriendListContainer { friends = _friendList };
            string json = JsonUtility.ToJson(container);
            PlayerPrefs.SetString(GetFriendsPrefKey(), json);
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[FriendManager] Save error: " + ex.Message);
        }
    }

    public void LoadFriends()
    {
        _friendList.Clear();
        string key = GetFriendsPrefKey();
        string json = PlayerPrefs.GetString(key, "");

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var container = JsonUtility.FromJson<FriendListContainer>(json);
                if (container != null && container.friends != null)
                {
                    foreach (var f in container.friends)
                    {
                        // Lọc bỏ dữ liệu mock thử nghiệm trước đây nếu có
                        if (f != null && f.friendId != "FR-2041" && f.friendId != "FR-5982" && f.friendId != "FR-8814")
                        {
                            _friendList.Add(f);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        LoadFriendRequests();
    }
}
