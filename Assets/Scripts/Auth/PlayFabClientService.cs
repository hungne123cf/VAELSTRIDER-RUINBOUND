using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Dịch vụ kết nối trực tiếp tới Máy chủ Microsoft PlayFab (Azure Game Backend) chuẩn Thương Mại.
/// - Sử dụng chuẩn HTTPS REST API chính thức của PlayFab: https://{TitleId}.playfabapi.com/Client/...
/// - Không phụ thuộc thư viện .jar/.aar ngoài nên tương thích 100% với cả PC và Android APK (Zero Gradle Crash).
/// - Hỗ trợ:
///   1. Đăng ký & Đăng nhập Tài khoản Chay / Email (RegisterPlayFabUser, LoginWithPlayFab, LoginWithEmailAddress)
///   2. Đăng nhập Chơi Ngay theo mã phần cứng (LoginWithCustomID / Device ID) & Liên kết tài khoản (AddUsernamePassword)
///   3. Đăng nhập Google OAuth 2.0 (LoginWithGoogleAccount), Facebook OAuth 2.0 (LoginWithFacebook), Zalo OAuth v4
///   4. Đồng bộ Cloud Save 2 chiều toàn bộ Linh Hồn, 7 Tướng, Trang Bị, Kho Đồ (UpdateUserData / GetUserData)
///   5. Bảng Xếp Hạng Đấu Trường PvP Toàn Cầu thời gian thực (UpdatePlayerStatistics / GetLeaderboard)
/// </summary>
public class PlayFabClientService : MonoBehaviour
{
    public static PlayFabClientService Instance { get; private set; }

    public const string DEFAULT_TITLE_ID = "1BE56D";
    public const string PREF_PLAYFAB_TITLE_ID = "PlayFab_Title_ID";
    public const string PREF_GOOGLE_CLIENT_ID = "OAuth_Google_Client_ID";
    public const string PREF_FACEBOOK_APP_ID = "OAuth_Facebook_App_ID";
    public const string PREF_ZALO_APP_ID = "OAuth_Zalo_App_ID";
    public const string USER_DATA_SAVE_KEY = "VaelstriderAccountJson";
    public const string STAT_PVP_RATING = "PvP_Rating";
    public const string STAT_PVP_WINS = "PvP_Wins";

    [Header("Microsoft PlayFab Configuration")]
    [Tooltip("Mã Title ID trên trang developer.playfab.com (Của bạn: 1BE56D)")]
    public string defaultTitleId = DEFAULT_TITLE_ID;

    public string PlayFabId { get; private set; } = "";
    public string SessionTicket { get; private set; } = "";
    public string LoggedInProvider { get; private set; } = "Offline";
    public bool IsLoggedIn => !string.IsNullOrEmpty(SessionTicket) && !string.IsNullOrEmpty(PlayFabId);

    // Quản lý độc quyền phiên online (Chống 1 tài khoản đăng nhập trên 2 thiết bị cùng lúc)
    public const string KEY_ACTIVE_DEVICE = "ActiveOnlineDeviceId";
    public const string KEY_ACTIVE_HEARTBEAT = "ActiveOnlineHeartbeat";
    public const string KEY_ACTIVE_SESSION = "ActiveOnlineSessionId";

    private Coroutine _heartbeatCoroutine = null;
    private string _currentSessionId = "";
    private Coroutine _pendingUploadCoroutine = null;
    private float _lastUploadTime = -999f;
    private const float CLOUD_SYNC_MIN_INTERVAL = 2.0f;

    public static bool IsOfflineMode
    {
        get => PlayerPrefs.GetInt("IsOfflineMode", 0) == 1;
        set => PlayerPrefs.SetInt("IsOfflineMode", value ? 1 : 0);
    }

    public event Action<string> OnAuthStatusChanged;
    public event Action<List<LeaderboardEntry>> OnLeaderboardLoaded;

    [Serializable]
    public class LeaderboardEntry
    {
        public int Position;
        public string PlayFabId;
        public string DisplayName;
        public int StatValue;
    }

    // Các lớp tuần tự hóa JSON cho PlayFab REST API
    [Serializable]
    private class RegisterPlayFabRequest
    {
        public string TitleId;
        public string Username;
        public string Email;
        public string Password;
        public bool RequireBothUsernameAndEmail;
    }

    [Serializable]
    private class LoginUsernameRequest
    {
        public string TitleId;
        public string Username;
        public string Password;
    }

    [Serializable]
    private class LoginEmailRequest
    {
        public string TitleId;
        public string Email;
        public string Password;
    }

    [Serializable]
    private class LoginCustomIdRequest
    {
        public string TitleId;
        public string CustomId;
        public bool CreateAccount;
    }

    [Serializable]
    private class LoginGoogleRequest
    {
        public string TitleId;
        public string AccessToken;
        public bool CreateAccount;
    }

    [Serializable]
    private class LoginFacebookRequest
    {
        public string TitleId;
        public string AccessToken;
        public bool CreateAccount;
    }

    [Serializable]
    private class AddUsernamePasswordRequest
    {
        public string Username;
        public string Email;
        public string Password;
    }

    [Serializable]
    private class RecoveryEmailRequest
    {
        public string TitleId;
        public string Email;
    }

    [Serializable]
    private class UpdateDisplayNameRequest
    {
        public string DisplayName;
    }

    [Serializable]
    private class GetLeaderboardRequest
    {
        public string StatisticName;
        public int StartPosition;
        public int MaxResultsCount;
    }

    public static PlayFabClientService EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = FindAnyObjectByType<PlayFabClientService>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }
        GameObject go = new GameObject("PlayFabClientService");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<PlayFabClientService>();
        return Instance;
    }

    private void Awake()
    {
        Application.runInBackground = true;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public string GetActiveTitleId()
    {
        string saved = PlayerPrefs.GetString(PREF_PLAYFAB_TITLE_ID, "").Trim();
        if (!string.IsNullOrEmpty(saved)) return saved.ToUpperInvariant();
        if (!string.IsNullOrEmpty(defaultTitleId)) return defaultTitleId.Trim().ToUpperInvariant();
        return DEFAULT_TITLE_ID;
    }

    public void SetTitleId(string titleId)
    {
        string clean = (titleId ?? "").Trim().ToUpperInvariant();
        PlayerPrefs.SetString(PREF_PLAYFAB_TITLE_ID, clean);
        PlayerPrefs.Save();
    }

    public bool HasConfiguredTitleId()
    {
        return !string.IsNullOrEmpty(GetActiveTitleId());
    }

    private string BuildEndpoint(string path)
    {
        string titleId = GetActiveTitleId();
        return $"https://{titleId.ToLowerInvariant()}.playfabapi.com{path}";
    }

    private void EmitStatus(string msg)
    {
        Debug.Log($"<color=#55DDFF>[PLAYFAB]</color> {msg}");
        OnAuthStatusChanged?.Invoke(msg);
    }

    // =========================================================================
    // 1. ĐĂNG KÝ TÀI KHOẢN CHAY / EMAIL TRÊN MICROSOFT PLAYFAB
    // =========================================================================
    public void RegisterAccount(string username, string email, string password, Action<bool, string> callback)
    {
        if (!ValidateTitleIdOrWarn(callback)) return;
        StartCoroutine(RegisterAccountRoutine(username, email, password, callback));
    }

    private IEnumerator RegisterAccountRoutine(string username, string email, string password, Action<bool, string> callback)
    {
        string cleanUser = (username ?? "").Trim();
        string cleanEmail = (email ?? "").Trim();
        string cleanPass = (password ?? "").Trim();

        if (cleanUser.Length < 3)
        {
            callback?.Invoke(false, LocalizationManager.T("Tên tài khoản PlayFab phải từ 3 ký tự trở lên!", "Username must be at least 3 characters!"));
            yield break;
        }
        if (cleanPass.Length < 6)
        {
            callback?.Invoke(false, LocalizationManager.T("Mật khẩu chuẩn thương mại PlayFab phải từ 6 ký tự trở lên!", "PlayFab password must be at least 6 characters!"));
            yield break;
        }

        EmitStatus(LocalizationManager.T("ĐANG ĐĂNG KÝ TÀI KHOẢN LÊN MÁY CHỦ MICROSOFT PLAYFAB...", "REGISTERING ON MICROSOFT PLAYFAB..."));

        var reqBody = new RegisterPlayFabRequest
        {
            TitleId = GetActiveTitleId(),
            Username = cleanUser,
            Email = string.IsNullOrEmpty(cleanEmail) ? $"{cleanUser.ToLowerInvariant()}@vaelstrider.com" : cleanEmail,
            Password = cleanPass,
            RequireBothUsernameAndEmail = false
        };

        yield return SendPlayFabPost("/Client/RegisterPlayFabUser", JsonUtility.ToJson(reqBody), useAuthHeader: false,
            onSuccess: (json) =>
            {
                PlayFabId = ExtractJsonString(json, "PlayFabId");
                SessionTicket = ExtractJsonString(json, "SessionTicket");
                LoggedInProvider = "PlayFab Account";

                // Đồng bộ tài khoản local & đẩy lên PlayFab Cloud
                AccountDatabase.Register(cleanUser, cleanPass, out _);
                AccountDatabase.Login(cleanUser, cleanPass, out var localAcc, out _);
                if (localAcc != null)
                {
                    UploadAccountDataToPlayFab(localAcc);
                    SubmitPvPScoreToLeaderboard(localAcc.pvpRating, localAcc.pvpWins);
                }

                string msg = LocalizationManager.T(
                    $"ĐĂNG KÝ PLAYFAB THÀNH CÔNG! ID: {PlayFabId}",
                    $"PLAYFAB REGISTERED! ID: {PlayFabId}");
                EmitStatus(msg);
                callback?.Invoke(true, msg);
            },
            onError: (err) =>
            {
                EmitStatus(err);
                callback?.Invoke(false, err);
            });
    }

    // =========================================================================
    // 2. ĐĂNG NHẬP TÀI KHOẢN CHAY / EMAIL TRÊN MICROSOFT PLAYFAB
    // =========================================================================
    public void LoginWithUsernameOrEmail(string userOrEmail, string password, Action<bool, string> callback)
    {
        if (!ValidateTitleIdOrWarn(callback)) return;
        StartCoroutine(LoginUsernameOrEmailRoutine(userOrEmail, password, callback));
    }

    private IEnumerator LoginUsernameOrEmailRoutine(string userOrEmail, string password, Action<bool, string> callback)
    {
        string cleanInput = (userOrEmail ?? "").Trim();
        string cleanPass = (password ?? "").Trim();

        if (string.IsNullOrEmpty(cleanInput) || string.IsNullOrEmpty(cleanPass))
        {
            callback?.Invoke(false, LocalizationManager.T("Vui lòng nhập đầy đủ Tài khoản và Mật khẩu!", "Please enter Username/Email and Password!"));
            yield break;
        }

        EmitStatus(LocalizationManager.T("ĐANG XÁC THỰC TÀI KHOẢN VỚI MICROSOFT PLAYFAB...", "AUTHENTICATING WITH MICROSOFT PLAYFAB..."));

        bool isEmail = cleanInput.Contains("@");
        string path = isEmail ? "/Client/LoginWithEmailAddress" : "/Client/LoginWithPlayFab";
        string bodyJson = isEmail
            ? JsonUtility.ToJson(new LoginEmailRequest { TitleId = GetActiveTitleId(), Email = cleanInput, Password = cleanPass })
            : JsonUtility.ToJson(new LoginUsernameRequest { TitleId = GetActiveTitleId(), Username = cleanInput, Password = cleanPass });

        yield return SendPlayFabPost(path, bodyJson, useAuthHeader: false,
            onSuccess: (json) =>
            {
                PlayFabId = ExtractJsonString(json, "PlayFabId");
                SessionTicket = ExtractJsonString(json, "SessionTicket");
                LoggedInProvider = isEmail ? "Email" : "PlayFab Account";

                // Tải dữ liệu Cloud từ PlayFab về máy hiện tại
                FetchAndApplyCloudAccountData(cleanInput, cleanPass, cleanInput, callback);
            },
            onError: (err) =>
            {
                EmitStatus(err);
                callback?.Invoke(false, err);
            });
    }

    // =========================================================================
    // 3. CHƠI NGAY (GUEST DEVICE ID) & LIÊN KẾT TÀI KHOẢN CHÍNH THỨC
    // =========================================================================
    public void LoginWithDeviceGuest(Action<bool, string> callback)
    {
        if (!ValidateTitleIdOrWarn(callback)) return;
        StartCoroutine(LoginDeviceGuestRoutine(callback));
    }

    private IEnumerator LoginDeviceGuestRoutine(Action<bool, string> callback)
    {
        EmitStatus(LocalizationManager.T("ĐANG ĐĂNG NHẬP TÀI KHOẢN THIẾT BỊ (GUEST CLOUD)...", "LOGGING IN WITH DEVICE ID..."));

        string deviceId = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(deviceId) || deviceId == SystemInfo.unsupportedIdentifier)
        {
            deviceId = PlayerPrefs.GetString("Fallback_Device_GUID", "");
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString("Fallback_Device_GUID", deviceId);
                PlayerPrefs.Save();
            }
        }

        string hash10 = ComputeAlphanumericHash(deviceId, 10);
        string guestUsername = "gv" + hash10;
        string guestPass = "VaelG#" + hash10;

        yield return LoginOrAutoRegisterPlayFabAccount(guestUsername, $"{guestUsername}@vaelstrider.com", guestPass, "Guest_" + hash10.Substring(0, 5).ToUpperInvariant(), "Guest Cloud", callback);
    }

    private static string ComputeAlphanumericHash(string input, int length)
    {
        string raw = AccountDatabase.HashPassword(input ?? "device").ToLowerInvariant();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < raw.Length && sb.Length < length; i++)
        {
            char c = raw[i];
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                sb.Append(c);
        }
        while (sb.Length < length) sb.Append('0');
        return sb.ToString();
    }

    private IEnumerator LoginOrAutoRegisterPlayFabAccount(string username, string email, string password, string displayName, string providerLabel, Action<bool, string> callback)
    {
        var loginReq = new LoginUsernameRequest
        {
            TitleId = GetActiveTitleId(),
            Username = username,
            Password = password
        };

        bool loginSucceeded = false;
        yield return SendPlayFabPost("/Client/LoginWithPlayFab", JsonUtility.ToJson(loginReq), useAuthHeader: false,
            onSuccess: (json) =>
            {
                loginSucceeded = true;
                PlayFabId = ExtractJsonString(json, "PlayFabId");
                SessionTicket = ExtractJsonString(json, "SessionTicket");
                LoggedInProvider = providerLabel;
                FetchAndApplyCloudAccountData(username, password, displayName, callback);
            },
            onError: (_) => { loginSucceeded = false; });

        if (loginSucceeded) yield break;

        // Nếu tài khoản chưa tồn tại trên PlayFab 1BE56D, tự động khởi tạo bằng RegisterPlayFabUser
        // KHÔNG gửi DisplayName khi đăng ký vì DisplayName trên PlayFab yêu cầu Unique. Game có Bước 3 riêng để đặt tên!
        var regReq = new RegisterPlayFabRequest
        {
            TitleId = GetActiveTitleId(),
            Username = username,
            Email = email,
            Password = password,
            RequireBothUsernameAndEmail = false
        };

        yield return SendPlayFabPost("/Client/RegisterPlayFabUser", JsonUtility.ToJson(regReq), useAuthHeader: false,
            onSuccess: (json) =>
            {
                PlayFabId = ExtractJsonString(json, "PlayFabId");
                SessionTicket = ExtractJsonString(json, "SessionTicket");
                LoggedInProvider = providerLabel;
                FetchAndApplyCloudAccountData(username, password, displayName, callback);
            },
            onError: (err) =>
            {
                EmitStatus(err);
                callback?.Invoke(false, err);
            });
    }

    /// <summary>
    /// Liên kết tài khoản Guest (Chơi Ngay) lên Tài khoản Chính Thức (Username + Email + Password) không mất tiến trình!
    /// </summary>
    public void LinkGuestToUsernamePassword(string username, string email, string password, Action<bool, string> callback)
    {
        if (!IsLoggedIn)
        {
            callback?.Invoke(false, LocalizationManager.T("Bạn cần đăng nhập Guest trước khi liên kết!", "Please login first before linking!"));
            return;
        }
        StartCoroutine(LinkGuestRoutine(username, email, password, callback));
    }

    private IEnumerator LinkGuestRoutine(string username, string email, string password, Action<bool, string> callback)
    {
        var req = new AddUsernamePasswordRequest
        {
            Username = username.Trim(),
            Email = email.Trim(),
            Password = password.Trim()
        };

        yield return SendPlayFabPost("/Client/AddUsernamePassword", JsonUtility.ToJson(req), useAuthHeader: true,
            onSuccess: (_) =>
            {
                LoggedInProvider = "Linked PlayFab Account";
                UpdatePlayFabDisplayName(username.Trim());
                string msg = LocalizationManager.T("ĐÃ LIÊN KẾT TÀI KHOẢN THÀNH CÔNG!", "ACCOUNT LINKED SUCCESSFULLY!");
                EmitStatus(msg);
                callback?.Invoke(true, msg);
            },
            onError: (err) =>
            {
                EmitStatus(err);
                callback?.Invoke(false, err);
            });
    }

    // =========================================================================
    // 4. ĐĂNG NHẬP GOOGLE, FACEBOOK & ZALO (OAUTH 2.0 THẬT + TOKEN EXCHANGE)
    // =========================================================================
    public void LoginWithGoogleToken(string googleAccessToken, Action<bool, string> callback)
    {
        if (!ValidateTitleIdOrWarn(callback)) return;
        if (string.IsNullOrWhiteSpace(googleAccessToken))
        {
            // Mở cổng Google OAuth 2.0 trên trình duyệt nếu đã cấu hình Google Client ID
            string clientId = PlayerPrefs.GetString(PREF_GOOGLE_CLIENT_ID, "").Trim();
            if (!string.IsNullOrEmpty(clientId))
            {
                string redirectUri = UnityWebRequest.EscapeURL("http://localhost:54321/oauth");
                string scope = UnityWebRequest.EscapeURL("openid email profile");
                string url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirectUri}&response_type=token&scope={scope}";
                Application.OpenURL(url);
                EmitStatus(LocalizationManager.T("ĐÃ MỞ TRANG XÁC THỰC GOOGLE OAUTH 2.0 TRÊN TRÌNH DUYỆT!", "OPENED GOOGLE OAUTH 2.0 IN BROWSER!"));
            }
            else
            {
                callback?.Invoke(false, LocalizationManager.T("Vui lòng nhập Google Access Token hoặc cấu hình Google Client ID ở Tab Cấu Hình!", "Please enter Google Access Token or configure Google Client ID!"));
            }
            return;
        }

        var req = new LoginGoogleRequest
        {
            TitleId = GetActiveTitleId(),
            AccessToken = googleAccessToken.Trim(),
            CreateAccount = true
        };

        StartCoroutine(SendPlayFabPost("/Client/LoginWithGoogleAccount", JsonUtility.ToJson(req), useAuthHeader: false,
            onSuccess: (json) =>
            {
                PlayFabId = ExtractJsonString(json, "PlayFabId");
                SessionTicket = ExtractJsonString(json, "SessionTicket");
                LoggedInProvider = "Google";
                FetchAndApplyCloudAccountData("GG_" + PlayFabId, "google_oauth", "GooglePlayer", callback);
            },
            onError: (err) =>
            {
                EmitStatus(err);
                callback?.Invoke(false, err);
            }));
    }

    public void LoginWithFacebookToken(string fbAccessToken, Action<bool, string> callback)
    {
        if (!ValidateTitleIdOrWarn(callback)) return;
        if (string.IsNullOrWhiteSpace(fbAccessToken))
        {
            string appId = PlayerPrefs.GetString(PREF_FACEBOOK_APP_ID, "").Trim();
            if (!string.IsNullOrEmpty(appId))
            {
                string redirectUri = UnityWebRequest.EscapeURL("https://www.facebook.com/connect/login_success.html");
                string url = $"https://www.facebook.com/v19.0/dialog/oauth?client_id={appId}&redirect_uri={redirectUri}&response_type=token&scope=public_profile,email";
                Application.OpenURL(url);
                EmitStatus(LocalizationManager.T("ĐÃ MỞ TRANG ĐĂNG NHẬP FACEBOOK OAUTH TRÊN TRÌNH DUYỆT!", "OPENED FACEBOOK OAUTH IN BROWSER!"));
            }
            else
            {
                callback?.Invoke(false, LocalizationManager.T("Vui lòng nhập Facebook Access Token hoặc cấu hình Facebook App ID ở Tab Cấu Hình!", "Please enter Facebook Access Token or configure Facebook App ID!"));
            }
            return;
        }

        var req = new LoginFacebookRequest
        {
            TitleId = GetActiveTitleId(),
            AccessToken = fbAccessToken.Trim(),
            CreateAccount = true
        };

        StartCoroutine(SendPlayFabPost("/Client/LoginWithFacebook", JsonUtility.ToJson(req), useAuthHeader: false,
            onSuccess: (json) =>
            {
                PlayFabId = ExtractJsonString(json, "PlayFabId");
                SessionTicket = ExtractJsonString(json, "SessionTicket");
                LoggedInProvider = "Facebook";
                FetchAndApplyCloudAccountData("FB_" + PlayFabId, "fb_oauth", "FacebookPlayer", callback);
            },
            onError: (err) =>
            {
                EmitStatus(err);
                callback?.Invoke(false, err);
            }));
    }

    /// <summary>
    /// Xác thực Zalo OAuth v4 thật qua Graph API (https://graph.zalo.me/v2.0/me) và ánh xạ vào PlayFab CustomID.
    /// </summary>
    public void LoginWithZaloAccessToken(string zaloAccessToken, Action<bool, string> callback)
    {
        if (!ValidateTitleIdOrWarn(callback)) return;
        if (string.IsNullOrWhiteSpace(zaloAccessToken))
        {
            string zaloAppId = PlayerPrefs.GetString(PREF_ZALO_APP_ID, "").Trim();
            if (!string.IsNullOrEmpty(zaloAppId))
            {
                string redirectUri = UnityWebRequest.EscapeURL("https://oauth.zaloapp.com/");
                string url = $"https://oauth.zaloapp.com/v4/permission?app_id={zaloAppId}&redirect_uri={redirectUri}&state=vaelstrider";
                Application.OpenURL(url);
                EmitStatus(LocalizationManager.T("ĐÃ MỞ TRANG ĐĂNG NHẬP ZALO OAUTH V4 TRÊN TRÌNH DUYỆT!", "OPENED ZALO OAUTH V4 IN BROWSER!"));
            }
            else
            {
                callback?.Invoke(false, LocalizationManager.T("Vui lòng nhập Zalo Access Token hoặc cấu hình Zalo App ID ở Tab Cấu Hình!", "Please enter Zalo Access Token or configure Zalo App ID!"));
            }
            return;
        }

        StartCoroutine(VerifyZaloGraphAndLoginPlayFabRoutine(zaloAccessToken.Trim(), callback));
    }

    private IEnumerator VerifyZaloGraphAndLoginPlayFabRoutine(string zaloToken, Action<bool, string> callback)
    {
        EmitStatus(LocalizationManager.T("ĐANG XÁC THỰC ZALO GRAPH API (graph.zalo.me)...", "VERIFYING ZALO GRAPH API..."));
        using (UnityWebRequest req = UnityWebRequest.Get("https://graph.zalo.me/v2.0/me?fields=id,name"))
        {
            req.SetRequestHeader("access_token", zaloToken);
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                callback?.Invoke(false, LocalizationManager.T($"Lỗi kết nối Zalo Graph API: {req.error}", $"Zalo Graph API error: {req.error}"));
                yield break;
            }

            string body = req.downloadHandler.text;
            string zaloId = ExtractJsonString(body, "id");
            string zaloName = ExtractJsonString(body, "name");
            if (string.IsNullOrEmpty(zaloId))
            {
                callback?.Invoke(false, LocalizationManager.T("Zalo Access Token không hợp lệ hoặc đã hết hạn!", "Invalid or expired Zalo Access Token!"));
                yield break;
            }

            string hash10 = ComputeAlphanumericHash("zalo_" + zaloId, 10);
            string zlUser = "zl" + hash10;
            string zlPass = "VaelZ#" + hash10;
            string disp = !string.IsNullOrEmpty(zaloName) ? zaloName : ("Zalo_" + hash10.Substring(0, 5));

            yield return LoginOrAutoRegisterPlayFabAccount(zlUser, $"{zlUser}@vaelstrider.com", zlPass, disp, "Zalo", callback);
        }
    }

    // =========================================================================
    // 5. QUÊN MẬT KHẨU (GỬI EMAIL KHÔI PHỤC THẬT TỪ MICROSOFT PLAYFAB)
    // =========================================================================
    public void SendPasswordRecoveryEmail(string email, Action<bool, string> callback)
    {
        if (!ValidateTitleIdOrWarn(callback)) return;
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        {
            callback?.Invoke(false, LocalizationManager.T("Vui lòng nhập địa chỉ Email hợp lệ để nhận link đặt lại mật khẩu!", "Please enter a valid Email address!"));
            return;
        }

        var req = new RecoveryEmailRequest
        {
            TitleId = GetActiveTitleId(),
            Email = email.Trim()
        };

        StartCoroutine(SendPlayFabPost("/Client/SendAccountRecoveryEmail", JsonUtility.ToJson(req), useAuthHeader: false,
            onSuccess: (_) =>
            {
                string msg = LocalizationManager.T(
                    $"ĐÃ GỬI EMAIL KHÔI PHỤC MẬT KHẨU TỚI {email.Trim()}!",
                    $"RECOVERY EMAIL SENT TO {email.Trim()}!");
                EmitStatus(msg);
                callback?.Invoke(true, msg);
            },
            onError: (err) =>
            {
                EmitStatus(err);
                callback?.Invoke(false, err);
            }));
    }

    // =========================================================================
    // 6. ĐỒNG BỘ DỮ LIỆU ĐÁM MÂY 2 CHIỀU (USER DATA CLOUD SAVE) & QUẢN LÝ PHIÊN ONLINE ĐỘC QUYỀN
    // =========================================================================
    public void UploadAccountDataToPlayFab(AccountDatabase.AccountData acc, bool forceImmediate = false)
    {
        if (!IsLoggedIn || acc == null) return;

        if (forceImmediate)
        {
            if (_pendingUploadCoroutine != null)
            {
                StopCoroutine(_pendingUploadCoroutine);
                _pendingUploadCoroutine = null;
            }
            DoUploadAccountData(acc);
            return;
        }

        // Debounce: Nếu vừa upload cách đây chưa đầy 2s, hoãn lại 2s rồi mới đẩy để tránh lỗi 429
        if (Time.unscaledTime - _lastUploadTime < CLOUD_SYNC_MIN_INTERVAL)
        {
            if (_pendingUploadCoroutine != null) StopCoroutine(_pendingUploadCoroutine);
            _pendingUploadCoroutine = StartCoroutine(DebouncedUploadRoutine(acc, CLOUD_SYNC_MIN_INTERVAL));
            return;
        }

        DoUploadAccountData(acc);
    }

    private IEnumerator DebouncedUploadRoutine(AccountDatabase.AccountData acc, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        _pendingUploadCoroutine = null;
        DoUploadAccountData(acc);
    }

    private void DoUploadAccountData(AccountDatabase.AccountData acc)
    {
        if (!IsLoggedIn || acc == null) return;
        _lastUploadTime = Time.unscaledTime;
        string rawJson = JsonUtility.ToJson(acc);
        string escapedJson = EscapeJsonString(rawJson);
        string payload = $"{{\"Data\":{{\"{USER_DATA_SAVE_KEY}\":\"{escapedJson}\"}},\"Permission\":\"Public\"}}";

        StartCoroutine(SendPlayFabPost("/Client/UpdateUserData", payload, useAuthHeader: true,
            onSuccess: (_) =>
            {
                Debug.Log($"<color=#55FF88>[PLAYFAB CLOUD]</color> Đã đồng bộ hồ sơ '{acc.username}' ({acc.displayName} • Linh hồn: {acc.aethelShards:N0}) lên Microsoft PlayFab!");
            },
            onError: (err) =>
            {
                Debug.LogWarning($"[PLAYFAB CLOUD] Chưa thể đồng bộ UserData: {err}");
            }));
    }

    private void ClaimActiveDeviceSession(string myDevId, string sessionId)
    {
        if (!IsLoggedIn) return;
        long nowTicks = DateTime.UtcNow.Ticks;
        string payload = $"{{\"Data\":{{\"{KEY_ACTIVE_DEVICE}\":\"{myDevId}\",\"{KEY_ACTIVE_HEARTBEAT}\":\"{nowTicks}\",\"{KEY_ACTIVE_SESSION}\":\"{sessionId}\"}},\"Permission\":\"Public\"}}";
        StartCoroutine(SendPlayFabPost("/Client/UpdateUserData", payload, useAuthHeader: true, null, null));

        if (_heartbeatCoroutine != null) StopCoroutine(_heartbeatCoroutine);
        _heartbeatCoroutine = StartCoroutine(OnlineHeartbeatRoutine(myDevId, sessionId));
    }

    private IEnumerator OnlineHeartbeatRoutine(string myDevId, string sessionId)
    {
        var wait = new WaitForSecondsRealtime(15f);
        while (IsLoggedIn && _currentSessionId == sessionId)
        {
            yield return wait;
            if (!IsLoggedIn || _currentSessionId != sessionId) yield break;

            // 1. Kiểm tra phiên trên server: nếu thiết bị khác đã chiếm phiên -> đá thiết bị này ra
            bool kicked = false;
            string checkPayload = $"{{\"PlayFabId\":\"{PlayFabId}\",\"Keys\":[\"{KEY_ACTIVE_SESSION}\"]}}";
            yield return SendPlayFabPost("/Client/GetUserData", checkPayload, useAuthHeader: true,
                onSuccess: (json) =>
                {
                    string serverSession = ExtractNestedUserDataValue(json, KEY_ACTIVE_SESSION);
                    if (!string.IsNullOrEmpty(serverSession) && serverSession != sessionId) kicked = true;
                }, null);

            if (!IsLoggedIn || _currentSessionId != sessionId) yield break;
            if (kicked)
            {
                HandleSessionTakenOver();
                yield break;
            }

            // 2. Vẫn là chủ phiên -> cập nhật heartbeat
            long nowTicks = DateTime.UtcNow.Ticks;
            string payload = $"{{\"Data\":{{\"{KEY_ACTIVE_HEARTBEAT}\":\"{nowTicks}\"}},\"Permission\":\"Public\"}}";
            yield return SendPlayFabPost("/Client/UpdateUserData", payload, useAuthHeader: true, null, null);
        }
    }

    /// <summary>Bắn ra khi tài khoản bị đăng nhập trên thiết bị khác và thiết bị này bị đá.</summary>
    public event Action OnSessionKicked;

    private void HandleSessionTakenOver()
    {
        Debug.LogWarning("[PLAYFAB] Tài khoản đã đăng nhập trên thiết bị khác -> đăng xuất thiết bị này.");

        // Rời phòng Co-op / PvP nếu đang ở trong
        try { if (PvPNetworkManager.Instance != null) PvPNetworkManager.Instance.ShutdownRunnerOnly(); }
        catch (Exception ex) { Debug.LogWarning("[PLAYFAB] Shutdown runner warning: " + ex.Message); }

        // Đăng xuất cục bộ, KHÔNG xóa phiên trên server (phiên đó là của thiết bị mới)
        LogoutSession(clearServerSession: false);
        EmitStatus(LocalizationManager.T(
            "TÀI KHOẢN ĐÃ ĐĂNG NHẬP TRÊN THIẾT BỊ KHÁC. BẠN ĐÃ BỊ ĐĂNG XUẤT!",
            "YOUR ACCOUNT WAS LOGGED IN ON ANOTHER DEVICE. YOU HAVE BEEN LOGGED OUT!"));
        PlayerPrefs.SetInt("VR_SessionKickedNotice", 1);
        PlayerPrefs.Save();

        OnSessionKicked?.Invoke();

        // Quay về màn hình đăng nhập
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "LoginScene")
            UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
    }

    private void FetchAndApplyCloudAccountData(string fallbackUsername, string fallbackPass, string suggestedDisplayName, Action<bool, string> callback)
    {
        string payload = $"{{\"Keys\":[\"{USER_DATA_SAVE_KEY}\",\"{KEY_ACTIVE_DEVICE}\",\"{KEY_ACTIVE_HEARTBEAT}\",\"{KEY_ACTIVE_SESSION}\"]}}";
        StartCoroutine(SendPlayFabPost("/Client/GetUserData", payload, useAuthHeader: true,
            onSuccess: (json) =>
            {
                // 1. KIỂM TRA & CHIẾM PHIÊN HOẠT ĐỘNG CHO THIẾT BỊ HIỆN TẠI
                // Lưu ý: Khi người chơi đăng nhập trên điện thoại mới (hoặc vừa đóng game trên PC),
                // luôn ưu tiên cấp quyền đăng nhập cho thiết bị hiện tại thay vì khóa tài khoản 40s.
                string myDevId = SystemInfo.deviceUniqueIdentifier;
                if (string.IsNullOrEmpty(myDevId)) myDevId = "Device_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);

                // 2. CHIẾM PHIÊN HOẠT ĐỘNG VÀ BẬT VÒNG LẶP HEARTBEAT CHO THIẾT BỊ HIỆN TẠI
                _currentSessionId = System.Guid.NewGuid().ToString("N");
                ClaimActiveDeviceSession(myDevId, _currentSessionId);

                // 3. TẢI VÀ NẠP DỮ LIỆU CLOUD CHUẨN XÁC
                string embeddedAccJson = ExtractNestedUserDataValue(json, USER_DATA_SAVE_KEY);
                if (!string.IsNullOrEmpty(embeddedAccJson))
                {
                    try
                    {
                        var cloudAcc = JsonUtility.FromJson<AccountDatabase.AccountData>(embeddedAccJson);
                        if (cloudAcc != null && !string.IsNullOrEmpty(cloudAcc.username))
                        {
                            string localSavedName = PlayerPrefs.GetString("Player_Name", "").Trim();
                            bool hasValidCloudName = !string.IsNullOrEmpty(cloudAcc.displayName) &&
                                                    cloudAcc.displayName != "Player" &&
                                                    cloudAcc.displayName != "Wanderer" &&
                                                    cloudAcc.displayName != "Chiến Binh" &&
                                                    !cloudAcc.displayName.StartsWith("Guest_");

                            if (hasValidCloudName)
                            {
                                PlayerPrefs.SetString("Player_Name", cloudAcc.displayName);
                                PlayerPrefs.SetInt("HasSetNickname_" + cloudAcc.username, 1);
                            }
                            else if (!string.IsNullOrEmpty(suggestedDisplayName) &&
                                     suggestedDisplayName != "Player" &&
                                     suggestedDisplayName != "Wanderer" &&
                                     suggestedDisplayName != "Chiến Binh")
                            {
                                cloudAcc.displayName = suggestedDisplayName;
                                PlayerPrefs.SetString("Player_Name", suggestedDisplayName);
                                PlayerPrefs.SetInt("HasSetNickname_" + cloudAcc.username, 1);
                            }
                            else if (!string.IsNullOrEmpty(localSavedName) &&
                                     localSavedName != "Player" &&
                                     localSavedName != "Wanderer" &&
                                     localSavedName != "Chiến Binh")
                            {
                                cloudAcc.displayName = localSavedName;
                                PlayerPrefs.SetInt("HasSetNickname_" + cloudAcc.username, 1);
                            }

                            // Lưu vào database và áp dụng đè sạch vào Game Session (không lưu prefs cũ của máy này đè lên cloudAcc!)
                            AccountDatabase.SaveOrUpdateCloudAccount(cloudAcc);
                            PlayerPrefs.Save();

                            // Cập nhật lại giao diện MainMenu nếu đang mở
                            MainMenuManager.Instance?.RefreshTopLeftPlayerProfileBadge();
                            MainMenuManager.Instance?.RefreshCurrencies();

                            string msg = LocalizationManager.T(
                                $"ĐĂNG NHẬP & ĐỒNG BỘ PLAYFAB THÀNH CÔNG! ({cloudAcc.displayName} • {cloudAcc.aethelShards:N0} LINH HỒN)",
                                $"PLAYFAB SYNC COMPLETE! ({cloudAcc.displayName} • {cloudAcc.aethelShards:N0} SOULS)");
                            EmitStatus(msg);
                            callback?.Invoke(true, msg);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[PLAYFAB] Lỗi giải mã hồ sơ Cloud: {ex.Message}");
                    }
                }

                // Nếu tài khoản PlayFab mới chưa có bản lưu, khởi tạo bản lưu từ máy hiện tại và đẩy lên PlayFab
                AccountDatabase.Register(fallbackUsername, fallbackPass, out _);
                AccountDatabase.Login(fallbackUsername, fallbackPass, out var localAcc, out _);
                if (localAcc == null) localAcc = AccountDatabase.GetActiveAccount();
                if (localAcc != null)
                {
                    localAcc.username = fallbackUsername;
                    string nameToUse = !string.IsNullOrEmpty(suggestedDisplayName) && suggestedDisplayName != "Player" ? suggestedDisplayName : fallbackUsername;
                    localAcc.displayName = nameToUse;
                    PlayerPrefs.SetString("Player_Name", nameToUse);
                    PlayerPrefs.Save();

                    AccountDatabase.SaveOrUpdateCloudAccount(localAcc);
                    UploadAccountDataToPlayFab(localAcc, forceImmediate: true);
                    SubmitPvPScoreToLeaderboard(localAcc.pvpRating, localAcc.pvpWins);
                }

                string newMsg = LocalizationManager.T(
                    $"ĐĂNG NHẬP PLAYFAB THÀNH CÔNG ({fallbackUsername})!",
                    $"LOGGED IN TO PLAYFAB ({fallbackUsername})!");
                EmitStatus(newMsg);
                callback?.Invoke(true, newMsg);
            },
            onError: (err) =>
            {
                callback?.Invoke(false, err);
            }));
    }

    public void UpdatePlayFabDisplayName(string displayName, Action<bool, string> callback = null)
    {
        if (!IsLoggedIn || string.IsNullOrWhiteSpace(displayName))
        {
            callback?.Invoke(false, "Not logged in");
            return;
        }
        string clean = displayName.Trim();
        if (clean.Length < 3) clean = clean + "_VN";
        if (clean.Length > 25) clean = clean.Substring(0, 25);

        var req = new UpdateDisplayNameRequest { DisplayName = clean };
        StartCoroutine(SendPlayFabPost("/Client/UpdateUserTitleDisplayName", JsonUtility.ToJson(req), useAuthHeader: true,
            onSuccess: (_) =>
            {
                callback?.Invoke(true, clean);
            },
            onError: (err) =>
            {
                // Nếu PlayFab báo "The display name entered is not available" (trùng tên với người chơi khác trên toàn Server Title):
                // Tự động gắn hậu tố số ngẫu nhiên để lưu hợp lệ lên Cloud PlayFab mà không làm treo người chơi
                if (err.IndexOf("not available", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    err.IndexOf("NameNotAvailable", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string uniqueFallback = clean;
                    if (uniqueFallback.Length > 19) uniqueFallback = uniqueFallback.Substring(0, 19);
                    uniqueFallback += "_" + UnityEngine.Random.Range(100, 999);
                    var retryReq = new UpdateDisplayNameRequest { DisplayName = uniqueFallback };
                    StartCoroutine(SendPlayFabPost("/Client/UpdateUserTitleDisplayName", JsonUtility.ToJson(retryReq), useAuthHeader: true,
                        onSuccess: (_) => callback?.Invoke(true, uniqueFallback),
                        onError: (_) => callback?.Invoke(true, clean)));
                    return;
                }
                callback?.Invoke(false, err);
            }));
    }

    /// <summary>
    /// Kiểm tra xem tài khoản hiện tại đã đặt Tên Hiển Thị Trong Game (Ingame Nickname) chuẩn Liên Quân chưa
    /// </summary>
    public bool HasConfirmedIngameNickname()
    {
        var acc = AccountDatabase.GetActiveAccount();
        string key = "HasSetNickname_" + (acc != null ? acc.username : PlayFabId);
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    /// <summary>
    /// Đặt hoặc Đổi Tên Hiển Thị Trong Game (Ingame Nickname) và đồng bộ lên PlayFab Cloud + BXH
    /// </summary>
    public void SetIngameDisplayName(string newNickname, Action<bool, string> callback)
    {
        string clean = (newNickname ?? "").Trim();
        if (clean.Length < 2)
        {
            callback?.Invoke(false, LocalizationManager.T("Tên hiển thị trong game phải từ 2 ký tự trở lên!", "Display name must be at least 2 characters!"));
            return;
        }
        if (clean.Length > 20) clean = clean.Substring(0, 20);

        var acc = AccountDatabase.GetActiveAccount();
        string currentUsername = (acc != null) ? acc.username : "";
        if (AccountDatabase.IsDisplayNameTaken(clean, currentUsername))
        {
            string err = LocalizationManager.T("TÊN NGƯỜI CHƠI ĐÃ ĐƯỢC SỬ DỤNG! VUI LÒNG CHỌN TÊN KHÁC.", "DISPLAY NAME ALREADY TAKEN! PLEASE CHOOSE ANOTHER.");
            EmitStatus(err);
            callback?.Invoke(false, err);
            return;
        }

        if (acc == null)
        {
            string fallbackUser = !string.IsNullOrEmpty(PlayFabId) ? PlayFabId : "Player";
            AccountDatabase.Register(fallbackUser, "offline", out _);
            acc = AccountDatabase.GetActiveAccount();
        }

        if (acc != null)
        {
            acc.displayName = clean;
            AccountDatabase.SaveOrUpdateCloudAccount(acc);
            PlayerPrefs.SetInt("HasSetNickname_" + acc.username, 1);
        }
        PlayerPrefs.SetInt("HasSetNickname_" + clean, 1);
        if (!string.IsNullOrEmpty(PlayFabId))
        {
            PlayerPrefs.SetInt("HasSetNickname_" + PlayFabId, 1);
        }
        PlayerPrefs.SetString("Player_Name", clean);
        PlayerPrefs.Save();

        if (IsLoggedIn)
        {
            UpdatePlayFabDisplayName(clean);
            if (acc != null)
            {
                UploadAccountDataToPlayFab(acc);
                SubmitPvPScoreToLeaderboard(acc.pvpRating, acc.pvpWins);
            }
        }

        string msg = LocalizationManager.T($"ĐÃ CẬP NHẬT TÊN HIỂN THỊ: {clean}", $"UPDATED DISPLAY NAME: {clean}");
        EmitStatus(msg);
        callback?.Invoke(true, clean);
    }

    /// <summary>
    /// Đăng nhập 1 chạm Google / Facebook / Zalo chuẩn Liên Quân Mobile:
    /// Mở trình duyệt chọn tài khoản qua OAuthAuthenticator (không cần dán Token), tự động đồng bộ vào PlayFab 1BE56D.
    /// </summary>
    public void LoginWithOneClickSocialOAuth(string provider, Action<bool, string> callback)
    {
        // Lưu ý: Không tự động bypass bằng OAuth_LastGoogleEmail để người dùng luôn được quyền 
        // chọn tài khoản Google mong muốn (hoặc đổi tài khoản khác) qua cửa sổ OAuth / Native Picker.

        EmitStatus(LocalizationManager.T(
            $"Đang liên kết tới {provider} để chọn tài khoản...",
            $"Connecting to {provider} to select account..."));

        OAuthAuthenticator.EnsureInstance().StartSocialLogin(provider,
            onSuccess: (profile) =>
            {
                string emailOrId = !string.IsNullOrEmpty(profile.email) ? profile.email : profile.id;
                string suggestedName = !string.IsNullOrEmpty(profile.name) ? profile.name : (provider + "Gamer");
                if (!string.IsNullOrEmpty(profile.email))
                {
                    PlayerPrefs.SetString("OAuth_LastGoogleEmail", profile.email);
                    PlayerPrefs.Save();
                }
                LoginWithDirectSocialAccount(provider, emailOrId, suggestedName, (ok, m) =>
                {
                    if (ok && provider == "Google") SyncGoogleAvatar(profile.pictureUrl);
                    callback?.Invoke(ok, m);
                });
            },
            onFailed: (err) =>
            {
                string msg = LocalizationManager.T($"Xác thực {provider} thất bại: {err}", $"{provider} login failed: {err}");
                EmitStatus(msg);
                callback?.Invoke(false, msg);
            },
            onStatus: (statusMsg) =>
            {
                EmitStatus(statusMsg);
            });
    }

    public void LoginWithDirectSocialAccount(string provider, string socialAccountKey, string suggestedDisplayName, Action<bool, string> callback)
    {
        string prefix = provider == "Google" ? "gg" : (provider == "Zalo" ? "zl" : "fb");
        string hash10 = ComputeAlphanumericHash(prefix + "_" + socialAccountKey.ToLowerInvariant(), 10);
        string pfUsername = prefix + hash10;
        string pfPassword = "Vael#" + hash10;
        string pfEmail = $"{pfUsername}@vaelstrider.com";
        string disp = !string.IsNullOrWhiteSpace(suggestedDisplayName) ? suggestedDisplayName.Trim() : (provider + "_" + hash10.Substring(0, 4));

        StartCoroutine(LoginOrAutoRegisterPlayFabAccount(pfUsername, pfEmail, pfPassword, disp, provider, callback));
    }

    public void LogoutSession() => LogoutSession(true);

    public void LogoutSession(bool clearServerSession)
    {
        if (_heartbeatCoroutine != null)
        {
            StopCoroutine(_heartbeatCoroutine);
            _heartbeatCoroutine = null;
        }
        if (_pendingUploadCoroutine != null)
        {
            StopCoroutine(_pendingUploadCoroutine);
            _pendingUploadCoroutine = null;
        }

        if (IsLoggedIn && clearServerSession)
        {
            // Xóa phiên hoạt động online trên PlayFab để thiết bị khác có thể đăng nhập ngay tức khắc
            string payload = $"{{\"Data\":{{\"{KEY_ACTIVE_HEARTBEAT}\":\"0\",\"{KEY_ACTIVE_DEVICE}\":\"\"}},\"Permission\":\"Public\"}}";
            StartCoroutine(SendPlayFabPost("/Client/UpdateUserData", payload, useAuthHeader: true, null, null));
        }

        _currentSessionId = "";
        PlayFabId = "";
        SessionTicket = "";
        LoggedInProvider = "None";
        IsOfflineMode = true;
        EmitStatus(LocalizationManager.T("ĐÃ ĐĂNG XUẤT TÀI KHOẢN.", "LOGGED OUT."));
    }

    private void OnApplicationQuit()
    {
        if (IsLoggedIn)
        {
            var acc = AccountDatabase.GetActiveAccount();
            if (acc != null)
            {
                DoUploadAccountData(acc);
            }
        }
    }

    // =========================================================================
    // 7. BẢNG XẾP HẠNG ĐẤU TRƯỜNG PVP TOÀN SERVER (PLAYFAB LEADERBOARDS)
    // =========================================================================
    private const string GLOBAL_BOARD_USER = "vaelglobalboard";
    private const string GLOBAL_BOARD_PASS = "VaelBoard#2026";
    private const string GLOBAL_BOARD_DATA_KEY = "GlobalPvPLeaderboardV1";

    private string _cachedBoardTicket = "";
    private DateTime _cachedBoardTicketTime = DateTime.MinValue;
    private bool _isFetchingBoardTicket = false;

    private IEnumerator EnsureGlobalBoardTicketRoutine(Action<string> onTicketReady)
    {
        if (!string.IsNullOrEmpty(_cachedBoardTicket) && (DateTime.UtcNow - _cachedBoardTicketTime).TotalHours < 12)
        {
            onTicketReady?.Invoke(_cachedBoardTicket);
            yield break;
        }

        while (_isFetchingBoardTicket)
        {
            yield return new WaitForSeconds(0.1f);
            if (!string.IsNullOrEmpty(_cachedBoardTicket) && (DateTime.UtcNow - _cachedBoardTicketTime).TotalHours < 12)
            {
                onTicketReady?.Invoke(_cachedBoardTicket);
                yield break;
            }
        }

        _isFetchingBoardTicket = true;
        var loginReq = new LoginUsernameRequest
        {
            TitleId = GetActiveTitleId(),
            Username = GLOBAL_BOARD_USER,
            Password = GLOBAL_BOARD_PASS
        };

        yield return SendPlayFabPostWithCustomTicket("/Client/LoginWithPlayFab", JsonUtility.ToJson(loginReq), null,
            onSuccess: (json) =>
            {
                _cachedBoardTicket = ExtractJsonString(json, "SessionTicket");
                _cachedBoardTicketTime = DateTime.UtcNow;
                _isFetchingBoardTicket = false;
                onTicketReady?.Invoke(_cachedBoardTicket);
            },
            onError: (err) =>
            {
                _isFetchingBoardTicket = false;
                Debug.LogWarning("[PlayFabClientService] EnsureGlobalBoardTicket warning: " + err);
                onTicketReady?.Invoke(null);
            });
    }

    public void InvalidateBoardTicket()
    {
        _cachedBoardTicket = "";
        _cachedBoardTicketTime = DateTime.MinValue;
    }

    public void SubmitPvPScoreToLeaderboard(int pvpRating, int pvpWins)
    {
        if (!IsLoggedIn) return;
        if (pvpRating <= 0) pvpRating = 1000;

        // 1. Gửi lên PlayFab Statistics API (nếu đã bật Allow Client Post Statistics)
        string payload = $"{{\"Statistics\":[{{\"StatisticName\":\"{STAT_PVP_RATING}\",\"Value\":{pvpRating}}},{{\"StatisticName\":\"{STAT_PVP_WINS}\",\"Value\":{pvpWins}}}]}}";
        StartCoroutine(SendPlayFabPost("/Client/UpdatePlayerStatistics", payload, useAuthHeader: true, null, null));

        // 2. Đồng thời ghi trực tiếp vào Sổ Cái Bảng Xếp Hạng Toàn Server trên PlayFab 1BE56D (Hoạt động 100% tức thì)
        StartCoroutine(SyncToPlayFabGlobalBoardRegistryRoutine(pvpRating, pvpWins));
    }

    private IEnumerator SyncToPlayFabGlobalBoardRegistryRoutine(int pvpRating, int pvpWins)
    {
        var acc = AccountDatabase.GetActiveAccount();
        string myName = (acc != null && !string.IsNullOrEmpty(acc.displayName)) ? acc.displayName : "Hiệp Sĩ";
        myName = myName.Replace("|", "").Replace(";", "").Replace("\"", "");
        string myPfId = string.IsNullOrEmpty(PlayFabId) ? "LOCAL" : PlayFabId;

        string boardTicket = "";
        yield return EnsureGlobalBoardTicketRoutine((t) => boardTicket = t);
        if (string.IsNullOrEmpty(boardTicket)) yield break;

        string currentBoardRaw = "";
        string getPayload = $"{{\"Keys\":[\"{GLOBAL_BOARD_DATA_KEY}\"]}}";
        yield return SendPlayFabPostWithCustomTicket("/Client/GetUserData", getPayload, boardTicket,
            onSuccess: (json) => { currentBoardRaw = ExtractNestedUserDataValue(json, GLOBAL_BOARD_DATA_KEY); },
            onError: (err) => { if (err.Contains("401") || err.Contains("403")) InvalidateBoardTicket(); });

        var dict = new Dictionary<string, LeaderboardEntry>();
        if (!string.IsNullOrEmpty(currentBoardRaw))
        {
            string[] rows = currentBoardRaw.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var r in rows)
            {
                string[] p = r.Split('|');
                if (p.Length >= 3 && int.TryParse(p[2], out int score))
                {
                    dict[p[0]] = new LeaderboardEntry { PlayFabId = p[0], DisplayName = p[1], StatValue = score };
                }
            }
        }

        dict[myPfId] = new LeaderboardEntry
        {
            PlayFabId = myPfId,
            DisplayName = $"{myName} ({pvpWins}W)",
            StatValue = pvpRating
        };

        var sorted = new List<LeaderboardEntry>(dict.Values);
        sorted.Sort((a, b) => b.StatValue.CompareTo(a.StatValue));
        if (sorted.Count > 30) sorted.RemoveRange(30, sorted.Count - 30);

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].Position = i + 1;
            sb.Append($"{sorted[i].PlayFabId}|{sorted[i].DisplayName}|{sorted[i].StatValue};");
        }

        string escaped = EscapeJsonString(sb.ToString());
        string updatePayload = $"{{\"Data\":{{\"{GLOBAL_BOARD_DATA_KEY}\":\"{escaped}\"}},\"Permission\":\"Public\"}}";
        yield return SendPlayFabPostWithCustomTicket("/Client/UpdateUserData", updatePayload, boardTicket, null, null);
    }

    public void FetchGlobalPvPLeaderboard(int maxResults = 15)
    {
        if (!HasConfiguredTitleId())
        {
            ValidateTitleIdOrWarn(null);
            return;
        }
        StartCoroutine(FetchGlobalBoardRoutine(maxResults));
    }

    private IEnumerator FetchGlobalBoardRoutine(int maxResults)
    {
        EmitStatus(LocalizationManager.T("ĐANG TẢI BẢNG XẾP HẠNG ĐẤU TRƯỜNG TỪ PLAYFAB (1BE56D)...", "LOADING GLOBAL PVP LEADERBOARD FROM PLAYFAB (1BE56D)..."));

        string boardTicket = "";
        yield return EnsureGlobalBoardTicketRoutine((t) => boardTicket = t);
        if (string.IsNullOrEmpty(boardTicket)) yield break;

        var list = new List<LeaderboardEntry>();
        if (!string.IsNullOrEmpty(boardTicket))
        {
            string raw = "";
            string getPayload = $"{{\"Keys\":[\"{GLOBAL_BOARD_DATA_KEY}\"]}}";
            yield return SendPlayFabPostWithCustomTicket("/Client/GetUserData", getPayload, boardTicket,
                onSuccess: (json) => { raw = ExtractNestedUserDataValue(json, GLOBAL_BOARD_DATA_KEY); },
                onError: (err) => { if (err.Contains("401") || err.Contains("403")) InvalidateBoardTicket(); });

            if (!string.IsNullOrEmpty(raw))
            {
                string[] rows = raw.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < rows.Length && list.Count < maxResults; i++)
                {
                    string[] p = rows[i].Split('|');
                    if (p.Length >= 3 && int.TryParse(p[2], out int score))
                    {
                        list.Add(new LeaderboardEntry
                        {
                            Position = list.Count + 1,
                            PlayFabId = p[0],
                            DisplayName = p[1],
                            StatValue = score
                        });
                    }
                }
            }
        }

        if (list.Count == 0)
        {
            var acc = AccountDatabase.GetActiveAccount();
            int myRating = acc != null && acc.pvpRating > 0 ? acc.pvpRating : 1000;
            string myName = acc != null && !string.IsNullOrEmpty(acc.displayName) ? acc.displayName :
                            acc != null && !string.IsNullOrEmpty(acc.username) ? acc.username : "Chiến Binh";
            list.Add(new LeaderboardEntry
            {
                Position = 1,
                PlayFabId = string.IsNullOrEmpty(PlayFabId) ? "1BE56D" : PlayFabId,
                DisplayName = myName,
                StatValue = myRating
            });
        }

        OnLeaderboardLoaded?.Invoke(list);
        EmitStatus(LocalizationManager.T($"ĐÃ TẢI {list.Count} CAO THỦ TRÊN BẢNG XẾP HẠNG PLAYFAB (1BE56D)!", $"LOADED {list.Count} FIGHTERS FROM PLAYFAB LEADERBOARD (1BE56D)!"));
    }

    // =========================================================================
    // ĐỒNG BỘ AVATAR GOOGLE QUA PLAYFAB PROFILE (AvatarUrl)
    // =========================================================================
    /// <summary>Đẩy URL avatar lên PlayFab (khi thiết bị lấy được ảnh Google, vd PC).</summary>
    public void UploadAvatarUrl(string avatarUrl)
    {
        if (!IsLoggedIn || string.IsNullOrEmpty(avatarUrl)) return;
        string payload = $"{{\"ImageUrl\":\"{EscapeJsonString(avatarUrl.Trim())}\"}}";
        StartCoroutine(SendPlayFabPost("/Client/UpdateAvatarUrl", payload, useAuthHeader: true,
            onSuccess: (_) => Debug.Log("[PLAYFAB] Đã đồng bộ AvatarUrl lên PlayFab."),
            onError: (err) => Debug.LogWarning("[PLAYFAB] UpdateAvatarUrl lỗi: " + err)));
    }

    /// <summary>Lấy URL avatar đã lưu trên PlayFab (khi thiết bị không lấy được ảnh Google, vd Android AccountManager).</summary>
    public void FetchAvatarUrl(Action<string> onResult)
    {
        if (!IsLoggedIn) { onResult?.Invoke(""); return; }
        string payload = $"{{\"PlayFabId\":\"{PlayFabId}\",\"ProfileConstraints\":{{\"ShowAvatarUrl\":true}}}}";
        StartCoroutine(SendPlayFabPost("/Client/GetPlayerProfile", payload, useAuthHeader: true,
            onSuccess: (json) => onResult?.Invoke(ExtractJsonString(json, "AvatarUrl") ?? ""),
            onError: (err) => { Debug.LogWarning("[PLAYFAB] GetPlayerProfile lỗi: " + err); onResult?.Invoke(""); }));
    }

    /// <summary>Có ảnh -> đẩy lên PlayFab. Không có ảnh -> tải ảnh đã lưu từ PlayFab về.</summary>
    public void SyncGoogleAvatar(string localPhotoUrl, Action<Sprite> onAvatarReady = null)
    {
        if (!string.IsNullOrEmpty(localPhotoUrl))
        {
            UploadAvatarUrl(localPhotoUrl);
            return;
        }
        FetchAvatarUrl((url) =>
        {
            if (!string.IsNullOrEmpty(url))
                OAuthAuthenticator.SaveAndDownloadAvatar(url, onAvatarReady);
        });
    }

    // =========================================================================
    // HỆ THỐNG LỜI MỜI PHÒNG CO-OP CLOUD & CROSS-DEVICE
    // =========================================================================
    private const string GLOBAL_INVITES_DATA_KEY = "GlobalCoopRoomInvites_v1";

    public void SendRoomInviteCloud(string targetFriend, string senderName, string roomPin)
    {
        if (string.IsNullOrEmpty(targetFriend) || string.IsNullOrEmpty(roomPin)) return;
        // 1. Lưu Local Shared (hỗ trợ test cùng thiết bị hoặc cùng mạng)
        string cleanTarget = targetFriend.Trim().ToUpperInvariant();
        string localKey = "VR_LOCAL_ROOM_INVITE_" + cleanTarget;
        PlayerPrefs.SetString(localKey, $"{senderName}|{roomPin}|{DateTime.UtcNow.Ticks}");
        PlayerPrefs.Save();

        // 2. Gửi lên Cloud Title Data PlayFab
        StartCoroutine(SendRoomInviteCloudRoutine(targetFriend.Trim(), senderName, roomPin));
    }

    private IEnumerator SendRoomInviteCloudRoutine(string targetFriend, string senderName, string roomPin)
    {
        string boardTicket = "";
        yield return EnsureGlobalBoardTicketRoutine((t) => boardTicket = t);
        if (string.IsNullOrEmpty(boardTicket)) yield break;

        string currentRaw = "";
        string getPayload = $"{{\"Keys\":[\"{GLOBAL_INVITES_DATA_KEY}\"]}}";
        yield return SendPlayFabPostWithCustomTicket("/Client/GetUserData", getPayload, boardTicket,
            onSuccess: (json) => { currentRaw = ExtractNestedUserDataValue(json, GLOBAL_INVITES_DATA_KEY); },
            onError: (err) => { if (err.Contains("401") || err.Contains("403")) InvalidateBoardTicket(); });

        long nowTicks = DateTime.UtcNow.Ticks;
        long expireTicks = TimeSpan.FromSeconds(120).Ticks; // Giữ lời mời trong 120s

        List<string> validEntries = new List<string>();
        if (!string.IsNullOrEmpty(currentRaw))
        {
            string[] items = currentRaw.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                string[] p = item.Split('|');
                if (p.Length >= 4 && long.TryParse(p[3], out long t))
                {
                    if (nowTicks - t < expireTicks)
                    {
                        validEntries.Add(item);
                    }
                }
            }
        }

        validEntries.Add($"{targetFriend}|{senderName}|{roomPin}|{nowTicks}");
        if (validEntries.Count > 30) validEntries.RemoveAt(0);

        string updateData = string.Join(";", validEntries);
        string escaped = EscapeJsonString(updateData);
        string updatePayload = $"{{\"Data\":{{\"{GLOBAL_INVITES_DATA_KEY}\":\"{escaped}\"}},\"Permission\":\"Public\"}}";
        yield return SendPlayFabPostWithCustomTicket("/Client/UpdateUserData", updatePayload, boardTicket, null, null);
    }

    public void CheckAndConsumeIncomingRoomInvites(string myDisplayName, Action<string, string> onInviteFound)
    {
        if (string.IsNullOrEmpty(myDisplayName)) return;

        // 1. Kiểm tra Local Storage trước (kiểm tra cả displayName và username)
        string cleanName = myDisplayName.Trim().ToUpperInvariant();
        string localKey = "VR_LOCAL_ROOM_INVITE_" + cleanName;
        string localVal = PlayerPrefs.GetString(localKey, "");

        var acc = AccountDatabase.GetActiveAccount();
        if (string.IsNullOrEmpty(localVal) && acc != null && !string.IsNullOrEmpty(acc.username))
        {
            string userKey = "VR_LOCAL_ROOM_INVITE_" + acc.username.Trim().ToUpperInvariant();
            localVal = PlayerPrefs.GetString(userKey, "");
            if (!string.IsNullOrEmpty(localVal))
            {
                PlayerPrefs.DeleteKey(userKey);
            }
        }

        if (!string.IsNullOrEmpty(localVal))
        {
            PlayerPrefs.DeleteKey(localKey);
            PlayerPrefs.Save();
            string[] parts = localVal.Split('|');
            if (parts.Length >= 2)
            {
                onInviteFound?.Invoke(parts[0], parts[1]);
                return;
            }
        }

        // 2. Kiểm tra Cloud Storage
        StartCoroutine(CheckIncomingRoomInvitesRoutine(myDisplayName.Trim(), onInviteFound));
    }

    private IEnumerator CheckIncomingRoomInvitesRoutine(string myDisplayName, Action<string, string> onInviteFound)
    {
        string boardTicket = "";
        yield return EnsureGlobalBoardTicketRoutine((t) => boardTicket = t);
        if (string.IsNullOrEmpty(boardTicket)) yield break;

        string currentRaw = "";
        string getPayload = $"{{\"Keys\":[\"{GLOBAL_INVITES_DATA_KEY}\"]}}";
        yield return SendPlayFabPostWithCustomTicket("/Client/GetUserData", getPayload, boardTicket,
            onSuccess: (json) => { currentRaw = ExtractNestedUserDataValue(json, GLOBAL_INVITES_DATA_KEY); },
            onError: (err) => { if (err.Contains("401") || err.Contains("403")) InvalidateBoardTicket(); });

        if (string.IsNullOrEmpty(currentRaw)) yield break;

        long nowTicks = DateTime.UtcNow.Ticks;
        long expireTicks = TimeSpan.FromSeconds(120).Ticks;

        string[] items = currentRaw.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> remaining = new List<string>();
        string foundSender = null;
        string foundPin = null;

        var activeAcc = AccountDatabase.GetActiveAccount();
        string myUsername = activeAcc != null ? (activeAcc.username ?? "") : "";

        foreach (var item in items)
        {
            string[] p = item.Split('|');
            if (p.Length >= 4 && long.TryParse(p[3], out long t))
            {
                if (nowTicks - t < expireTicks)
                {
                    bool isForMe = string.Equals(p[0], myDisplayName, StringComparison.OrdinalIgnoreCase) ||
                                   (!string.IsNullOrEmpty(myUsername) && string.Equals(p[0], myUsername, StringComparison.OrdinalIgnoreCase));

                    if (foundSender == null && isForMe)
                    {
                        foundSender = p[1];
                        foundPin = p[2];
                        continue;
                    }
                    remaining.Add(item);
                }
            }
        }

        if (foundSender != null)
        {
            string updateData = string.Join(";", remaining);
            string escaped = EscapeJsonString(updateData);
            string updatePayload = $"{{\"Data\":{{\"{GLOBAL_INVITES_DATA_KEY}\":\"{escaped}\"}},\"Permission\":\"Public\"}}";
            StartCoroutine(SendPlayFabPostWithCustomTicket("/Client/UpdateUserData", updatePayload, boardTicket, null, null));

            onInviteFound?.Invoke(foundSender, foundPin);
        }
    }

    private IEnumerator SendPlayFabPostWithCustomTicket(string path, string jsonBody, string customTicket, Action<string> onSuccess, Action<string> onError)
    {
        string url = BuildEndpoint(path);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

        int maxRetries = 2;
        int currentAttempt = 0;

        while (currentAttempt <= maxRetries)
        {
            currentAttempt++;
            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.disposeUploadHandlerOnDispose = true;
                req.disposeDownloadHandlerOnDispose = true;
                req.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(customTicket))
                {
                    req.SetRequestHeader("X-Authorization", customTicket);
                }
                req.timeout = 10;

                yield return req.SendWebRequest();

                string responseText = req.downloadHandler != null ? req.downloadHandler.text : "";
                if (req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300)
                {
                    onSuccess?.Invoke(responseText);
                    yield break;
                }
                else
                {
                    bool isTlsOrSocketError = req.result == UnityWebRequest.Result.ConnectionError ||
                                              (!string.IsNullOrEmpty(req.error) && (req.error.Contains("Curl error 55") || req.error.Contains("unitytls") || req.error.Contains("Connection reset") || req.error.Contains("Broken pipe")));

                    if (isTlsOrSocketError && currentAttempt <= maxRetries)
                    {
                        yield return new WaitForSecondsRealtime(0.5f);
                        continue;
                    }

                    string err = isTlsOrSocketError
                        ? LocalizationManager.T("Kết nối mạng không ổn định hoặc ngắt kết nối TLS.", "Unstable network or TLS connection reset.")
                        : req.error;
                    onError?.Invoke(err);
                    yield break;
                }
            }
        }
    }

    // =========================================================================
    // CORE HTTP TRANSPORT & JSON PARSER NHẸ CHUẨN ZERO-DEPENDENCY
    // =========================================================================
    private bool ValidateTitleIdOrWarn(Action<bool, string> callback)
    {
        if (HasConfiguredTitleId()) return true;
        string warn = LocalizationManager.T(
            "CHƯA CÓ PLAYFAB TITLE ID! Hãy nhập Mã Title ID (5 ký tự từ developer.playfab.com) ở Tab [4. CẤU HÌNH API]!",
            "MISSING PLAYFAB TITLE ID! Please enter your 5-char Title ID in [4. API CONFIG] tab!");
        EmitStatus(warn);
        callback?.Invoke(false, warn);
        return false;
    }

    private IEnumerator SendPlayFabPost(string path, string jsonBody, bool useAuthHeader, Action<string> onSuccess, Action<string> onError)
    {
        string url = BuildEndpoint(path);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

        int maxRetries = 2;
        int currentAttempt = 0;

        while (currentAttempt <= maxRetries)
        {
            currentAttempt++;
            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.disposeUploadHandlerOnDispose = true;
                req.disposeDownloadHandlerOnDispose = true;
                req.SetRequestHeader("Content-Type", "application/json");
                if (useAuthHeader && !string.IsNullOrEmpty(SessionTicket))
                {
                    req.SetRequestHeader("X-Authorization", SessionTicket);
                }
                req.timeout = 10;

                yield return req.SendWebRequest();

                string responseText = req.downloadHandler != null ? req.downloadHandler.text : "";
                if (req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300)
                {
                    onSuccess?.Invoke(responseText);
                    yield break;
                }
                else
                {
                    bool isTlsOrSocketError = req.result == UnityWebRequest.Result.ConnectionError ||
                                              (!string.IsNullOrEmpty(req.error) && (req.error.Contains("Curl error 55") || req.error.Contains("unitytls") || req.error.Contains("Connection reset") || req.error.Contains("Broken pipe")));

                    if (isTlsOrSocketError && currentAttempt <= maxRetries)
                    {
                        yield return new WaitForSecondsRealtime(0.5f);
                        continue;
                    }

                    string errMsg = ExtractJsonString(responseText, "errorMessage");
                    if (string.IsNullOrEmpty(errMsg))
                    {
                        if (req.responseCode == 403 || req.responseCode == 429)
                        {
                            errMsg = LocalizationManager.T("Máy chủ PlayFab tạm thời giới hạn tần suất truy cập. Vui lòng thử lại sau vài giây!", "PlayFab API rate limit reached. Please retry in a few seconds!");
                        }
                        else if (req.responseCode == 401)
                        {
                            errMsg = LocalizationManager.T("Tên tài khoản hoặc mật khẩu không chính xác!", "Invalid username or password!");
                        }
                        else if (isTlsOrSocketError)
                        {
                            errMsg = LocalizationManager.T("Mất kết nối mạng hoặc phiên bảo mật TLS bị ngắt. Đang tiếp tục ở chế độ ngoại tuyến.", "Network interrupted or TLS reset. Continuing in offline mode.");
                        }
                        else
                        {
                            errMsg = req.error;
                        }
                    }
                    onError?.Invoke(LocalizationManager.T($"Lỗi PlayFab: {errMsg}", $"PlayFab Error: {errMsg}"));
                    yield break;
                }
            }
        }
    }

    private static string EscapeJsonString(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        return raw.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }

    private static string ExtractJsonString(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return "";
        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                json,
                $"\"{System.Text.RegularExpressions.Regex.Escape(key)}\"\\s*:\\s*\"((?:\\\\\"|[^\"])*)\"",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
            }
        }
        catch (Exception) { }

        int keyPos = json.IndexOf($"\"{key}\"", StringComparison.OrdinalIgnoreCase);
        if (keyPos < 0) return "";
        int colonPos = json.IndexOf(':', keyPos + key.Length + 2);
        if (colonPos < 0) return "";
        int firstQuote = json.IndexOf('"', colonPos + 1);
        if (firstQuote < 0) return "";
        int endQuote = firstQuote + 1;
        while (endQuote < json.Length)
        {
            if (json[endQuote] == '"' && json[endQuote - 1] != '\\') break;
            endQuote++;
        }
        if (endQuote <= firstQuote) return "";
        return json.Substring(firstQuote + 1, endQuote - firstQuote - 1).Replace("\\\"", "\"").Replace("\\\\", "\\");
    }

    private static string ExtractNestedUserDataValue(string json, string dataKey)
    {
        if (string.IsNullOrEmpty(json)) return "";
        int keyPos = json.IndexOf($"\"{dataKey}\"", StringComparison.OrdinalIgnoreCase);
        if (keyPos < 0) return "";
        string sub = json.Substring(keyPos);
        return ExtractJsonString(sub, "Value");
    }

    private static List<LeaderboardEntry> ParseLeaderboardEntries(string json)
    {
        var result = new List<LeaderboardEntry>();
        if (string.IsNullOrEmpty(json)) return result;

        int lbIdx = json.IndexOf("\"Leaderboard\":[", StringComparison.OrdinalIgnoreCase);
        if (lbIdx < 0) return result;

        string sub = json.Substring(lbIdx);
        string[] items = sub.Split(new string[] { "{\"PlayFabId\":" }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < items.Length; i++)
        {
            string chunk = "{\"PlayFabId\":" + items[i];
            string pfId = ExtractJsonString(chunk, "PlayFabId");
            string disp = ExtractJsonString(chunk, "DisplayName");
            int pos = ExtractJsonInt(chunk, "Position", i - 1);
            int val = ExtractJsonInt(chunk, "StatValue", 1000);
            result.Add(new LeaderboardEntry
            {
                Position = pos + 1,
                PlayFabId = pfId,
                DisplayName = string.IsNullOrEmpty(disp) ? ("Fighter_" + pfId) : disp,
                StatValue = val
            });
        }
        return result;
    }

    private static int ExtractJsonInt(string json, string key, int defaultVal)
    {
        string pattern = $"\"{key}\":";
        int start = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return defaultVal;
        start += pattern.Length;
        int end = start;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
        {
            end++;
        }
        if (end > start && int.TryParse(json.Substring(start, end - start), out int v))
            return v;
        return defaultVal;
    }
}
