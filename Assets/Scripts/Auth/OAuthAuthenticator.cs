using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class OAuthAuthenticator : MonoBehaviour
{
    public static OAuthAuthenticator Instance { get; private set; }

    public static OAuthAuthenticator EnsureInstance()
    {
        if (Instance != null) return Instance;
        var existing = FindAnyObjectByType<OAuthAuthenticator>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }
        GameObject go = new GameObject("OAuthAuthenticator");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<OAuthAuthenticator>();
        return Instance;
    }

    [System.Serializable]
    public class UserProfile
    {
        public string id;
        public string name;
        public string email;
        public string pictureUrl;
        public string provider; // "Google" or "Facebook"
    }

    [Header("Google OAuth 2.0 Client ID (Google Cloud Console)")]
    public string googleClientId = "";
    public string googleClientSecret = "";

    public static string LastGoogleUserName { get; set; } = "";
    public static string LastGooglePhotoUrl { get; set; } = "";
    public static Sprite CachedGoogleAvatarSprite { get; private set; } = null;

    public static Sprite GetSavedAvatarSprite()
    {
        if (CachedGoogleAvatarSprite != null) return CachedGoogleAvatarSprite;

        try
        {
            string path = Path.Combine(Application.persistentDataPath, "google_avatar.png");
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    CachedGoogleAvatarSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    return CachedGoogleAvatarSprite;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OAuthAuthenticator] Lỗi tải avatar đã lưu: {ex.Message}");
        }
        return null;
    }

    public static void SaveAndDownloadAvatar(string photoUrl, Action<Sprite> onComplete = null)
    {
        if (string.IsNullOrEmpty(photoUrl)) return;
        LastGooglePhotoUrl = photoUrl;

        RunOnMainThread(() =>
        {
            try
            {
                PlayerPrefs.SetString("OAuth_LastGooglePhotoUrl", photoUrl);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[OAuthAuthenticator] Save avatar pref warning: " + ex.Message);
            }

            var instance = EnsureInstance();
            if (instance != null)
            {
                instance.StartCoroutine(instance.DownloadAvatarRoutine(photoUrl, onComplete));
            }
        });
    }

    private IEnumerator DownloadAvatarRoutine(string photoUrl, Action<Sprite> onComplete)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(photoUrl))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                    if (tex != null)
                    {
                        byte[] pngBytes = tex.EncodeToPNG();
                        string path = Path.Combine(Application.persistentDataPath, "google_avatar.png");
                        File.WriteAllBytes(path, pngBytes);

                        CachedGoogleAvatarSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        Debug.Log($"<color=green>[OAuthAuthenticator]</color> Đã lưu và nạp Avatar Google thành công!");
                        onComplete?.Invoke(CachedGoogleAvatarSprite);

                        if (MainMenuManager.Instance != null)
                        {
                            MainMenuManager.Instance.RefreshTopLeftPlayerProfileBadge();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[OAuthAuthenticator] Lỗi lưu avatar: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[OAuthAuthenticator] Không thể tải avatar Google từ URL: {uwr.error}");
            }
        }
    }

    private const int REDIRECT_PORT = 52140;
    private static readonly string REDIRECT_URI = $"http://localhost:{REDIRECT_PORT}/callback/";

    private HttpListener httpListener;
    private bool isListening = false;
    private string currentCodeVerifier = "";

    private Action<UserProfile> onAuthSuccessCallback;
    private Action<string> onAuthFailedCallback;
    private Action<string> onStatusUpdateCallback;

    private static readonly Queue<Action> mainThreadActions = new Queue<Action>();

    private void Awake()
    {
        Application.runInBackground = true;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSavedOAuthAppIds();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void LoadSavedOAuthAppIds()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // 1. Thử nạp từ file cấu hình ngoài trên PC (không chạy trên Android vì Application.dataPath nằm trong APK)
        try
        {
            string cfgPath = Path.Combine(Application.dataPath, "..", "oauth_config.json");
            if (File.Exists(cfgPath))
            {
                string json = File.ReadAllText(cfgPath);
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonUtility.FromJson<OAuthConfigContainer>(json);
                    if (data != null)
                    {
                        if (!string.IsNullOrEmpty(data.googleClientId)) googleClientId = data.googleClientId;
                        if (!string.IsNullOrEmpty(data.googleClientSecret)) googleClientSecret = data.googleClientSecret;
                    }
                }
            }
        }
        catch { }
#endif

        // 2. Thử nạp từ PlayerPrefs
        string savedId = PlayerPrefs.GetString("OAuth_GoogleClientId", "").Trim();
        if (!string.IsNullOrEmpty(savedId))
        {
            googleClientId = savedId;
        }
        else if (!string.IsNullOrEmpty(googleClientId))
        {
            PlayerPrefs.SetString("OAuth_GoogleClientId", googleClientId);
        }

        string savedSecret = PlayerPrefs.GetString("OAuth_GoogleClientSecret", "").Trim();
        if (!string.IsNullOrEmpty(savedSecret))
        {
            googleClientSecret = savedSecret;
        }
        else if (!string.IsNullOrEmpty(googleClientSecret))
        {
            PlayerPrefs.SetString("OAuth_GoogleClientSecret", googleClientSecret);
        }
        PlayerPrefs.Save();
    }

    [System.Serializable]
    private class OAuthConfigContainer
    {
        public string googleClientId;
        public string googleClientSecret;
    }

    public bool HasLiveBrowserOAuthConfigured(string provider = "Google")
    {
        LoadSavedOAuthAppIds();
        return !string.IsNullOrEmpty(googleClientId) &&
               googleClientId.Contains(".apps.googleusercontent.com");
    }

    private void Update()
    {
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions.Dequeue()?.Invoke();
            }
        }
    }

    public static void RunOnMainThread(Action action)
    {
        if (action == null) return;
        // Không gọi EnsureInstance() ở đây vì hàm này được gọi từ Android UI Thread / Background Thread
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    /// <summary>
    /// Bắt đầu quy trình đăng nhập Google OAuth 2.0 chính thức (mở accounts.google.com)
    /// Sử dụng giao thức RFC 7636 PKCE (Proof Key for Code Exchange) chuẩn Google
    /// </summary>
    public void StartGoogleLogin(Action<UserProfile> onSuccess, Action<string> onFailed, Action<string> onStatus)
    {
        onAuthSuccessCallback = onSuccess;
        onAuthFailedCallback = onFailed;
        onStatusUpdateCallback = onStatus;

        LoadSavedOAuthAppIds();
        if (string.IsNullOrEmpty(googleClientId))
        {
            onFailed?.Invoke("Chưa cấu hình Google Client ID. Vui lòng kiểm tra oauth_config.json hoặc Cài đặt.");
            return;
        }

        onStatusUpdateCallback?.Invoke("Đang mở Google OAuth 2.0 (accounts.google.com)...");

        try
        {
            currentCodeVerifier = GenerateCodeVerifier();
            string codeChallenge = GenerateCodeChallenge(currentCodeVerifier);

            StartLocalListener("Google");
            string state = Guid.NewGuid().ToString("N");
            string googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth" +
                                   $"?client_id={UnityWebRequest.EscapeURL(googleClientId)}" +
                                   $"&redirect_uri={UnityWebRequest.EscapeURL(REDIRECT_URI)}" +
                                   $"&response_type=code" +
                                   $"&scope={UnityWebRequest.EscapeURL("openid profile email")}" +
                                   $"&code_challenge={UnityWebRequest.EscapeURL(codeChallenge)}" +
                                   $"&code_challenge_method=S256" +
                                   $"&state={state}" +
                                   $"&access_type=offline" +
                                   $"&prompt=select_account";

            Debug.Log($"<color=cyan>[OAuth Google]</color> Mở URL Google Live: {googleAuthUrl}");
            Application.OpenURL(googleAuthUrl);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OAuth Google] Lỗi khởi động: {ex.Message}");
            onAuthFailedCallback?.Invoke("Lỗi khởi động xác thực: " + ex.Message);
            StopLocalListener();
        }
    }

    public void StartSocialLogin(string provider, Action<UserProfile> onSuccess, Action<string> onFailed, Action<string> onStatus)
    {
        StartGoogleLogin(onSuccess, onFailed, onStatus);
    }

    private static string GenerateCodeVerifier()
    {
        byte[] randomBytes = new byte[32];
        using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
        {
            rng.GetBytes(randomBytes);
        }
        return Base64UrlEncode(randomBytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            byte[] challengeBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
            return Base64UrlEncode(challengeBytes);
        }
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private void StartLocalListener(string provider)
    {
        StopLocalListener();

        try
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{REDIRECT_PORT}/callback/");
            httpListener.Start();
            isListening = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OAuth] Không thể khởi động cổng {REDIRECT_PORT}: {ex.Message}");
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                while (isListening && httpListener != null && httpListener.IsListening)
                {
                    var context = await httpListener.GetContextAsync();
                    var request = context.Request;
                    var response = context.Response;

                    string query = request.Url.Query;
                    var queryParams = ParseQueryString(query);

                    string code = queryParams.ContainsKey("code") ? queryParams["code"] : null;
                    string error = queryParams.ContainsKey("error") ? queryParams["error"] : null;
                    string errorDesc = queryParams.ContainsKey("error_description") ? queryParams["error_description"] : null;

                    if (!string.IsNullOrEmpty(error))
                    {
                        string responseHtml = GetBrowserResponseHtml(false, "", "", errorDesc ?? error);
                        byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
                        response.ContentLength64 = buffer.Length;
                        response.ContentType = "text/html; charset=utf-8";
                        using (var output = response.OutputStream)
                        {
                            await output.WriteAsync(buffer, 0, buffer.Length);
                        }

                        RunOnMainThread(() =>
                        {
                            onAuthFailedCallback?.Invoke($"Đăng nhập Google bị hủy hoặc lỗi: {errorDesc ?? error}");
                        });
                        break;
                    }

                    if (!string.IsNullOrEmpty(code))
                    {
                        // Trao đổi mã code với Google Token endpoint
                        UserProfile profile = await ExchangeGoogleCodeForProfileAsync(code, currentCodeVerifier);
                        if (profile == null || string.IsNullOrEmpty(profile.email))
                        {
                            string errHtml = GetBrowserResponseHtml(false, "", "", "Không thể giải mã hồ sơ Google. Vui lòng thử lại!");
                            byte[] errBuf = Encoding.UTF8.GetBytes(errHtml);
                            response.ContentLength64 = errBuf.Length;
                            response.ContentType = "text/html; charset=utf-8";
                            using (var output = response.OutputStream)
                            {
                                await output.WriteAsync(errBuf, 0, errBuf.Length);
                            }

                            RunOnMainThread(() =>
                            {
                                onAuthFailedCallback?.Invoke("Không thể xác thực danh tính Google! Vui lòng thử lại.");
                            });
                            break;
                        }

                        string responseHtml = GetBrowserResponseHtml(true, profile.name, profile.email, null);
                        byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
                        response.ContentLength64 = buffer.Length;
                        response.ContentType = "text/html; charset=utf-8";
                        using (var output = response.OutputStream)
                        {
                            await output.WriteAsync(buffer, 0, buffer.Length);
                        }

                        RunOnMainThread(() =>
                        {
                            LastGoogleUserName = profile.name;
                            if (!string.IsNullOrEmpty(profile.pictureUrl))
                            {
                                SaveAndDownloadAvatar(profile.pictureUrl);
                            }
                            onStatusUpdateCallback?.Invoke($"Đăng nhập Google thành công: {profile.name} ({profile.email})");
                            onAuthSuccessCallback?.Invoke(profile);
                        });
                        break;
                    }
                }
            }
            catch (HttpListenerException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                RunOnMainThread(() =>
                {
                    onAuthFailedCallback?.Invoke("Lỗi kết nối OAuth: " + ex.Message);
                });
            }
            finally
            {
                StopLocalListener();
            }
        });
    }

    private async Task<UserProfile> ExchangeGoogleCodeForProfileAsync(string code, string codeVerifier)
    {
        try
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                var pairs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", googleClientId),
                    new KeyValuePair<string, string>("redirect_uri", REDIRECT_URI),
                    new KeyValuePair<string, string>("grant_type", "authorization_code")
                };
                if (!string.IsNullOrEmpty(codeVerifier))
                {
                    pairs.Add(new KeyValuePair<string, string>("code_verifier", codeVerifier));
                }
                if (!string.IsNullOrEmpty(googleClientSecret))
                {
                    pairs.Add(new KeyValuePair<string, string>("client_secret", googleClientSecret));
                }

                var content = new System.Net.Http.FormUrlEncodedContent(pairs);
                var tokenResp = await client.PostAsync("https://oauth2.googleapis.com/token", content);
                string tokenJson = await tokenResp.Content.ReadAsStringAsync();
                Debug.Log($"<color=cyan>[OAuth Google Response {(int)tokenResp.StatusCode}]</color> {tokenJson}");

                string idToken = ExtractJsonField(tokenJson, "id_token");
                string accessToken = ExtractJsonField(tokenJson, "access_token");

                if (!string.IsNullOrEmpty(idToken))
                {
                    var p = DecodeGoogleIdToken(idToken);
                    if (p != null) return p;
                }

                if (!string.IsNullOrEmpty(accessToken))
                {
                    var userReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
                    userReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    var userResp = await client.SendAsync(userReq);
                    string userJson = await userResp.Content.ReadAsStringAsync();
                    Debug.Log($"<color=cyan>[OAuth Google UserInfo]</color> {userJson}");

                    return new UserProfile
                    {
                        id = ExtractJsonField(userJson, "sub"),
                        name = ExtractJsonField(userJson, "name"),
                        email = ExtractJsonField(userJson, "email"),
                        pictureUrl = ExtractJsonField(userJson, "picture"),
                        provider = "Google"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OAuth Google] Lỗi trao đổi Token: {ex.Message}");
        }
        return null;
    }

    private static UserProfile DecodeGoogleIdToken(string idToken)
    {
        try
        {
            string[] parts = idToken.Split('.');
            if (parts.Length < 2) return null;
            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            while (payload.Length % 4 != 0) payload += "=";
            byte[] bytes = Convert.FromBase64String(payload);
            string json = Encoding.UTF8.GetString(bytes);
            Debug.Log($"<color=cyan>[OAuth Google ID Token Payload]</color> {json}");

            string email = ExtractJsonField(json, "email");
            string name = ExtractJsonField(json, "name");
            string sub = ExtractJsonField(json, "sub");
            string pic = ExtractJsonField(json, "picture");

            if (!string.IsNullOrEmpty(email) || !string.IsNullOrEmpty(sub))
            {
                return new UserProfile
                {
                    id = sub,
                    name = !string.IsNullOrEmpty(name) ? name : "Google Fighter",
                    email = email,
                    pictureUrl = pic,
                    provider = "Google"
                };
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OAuth Google] Lỗi giải mã ID Token: {ex.Message}");
        }
        return null;
    }

    private static string ExtractJsonField(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return "";
        try
        {
            // Bắt chuẩn JSON có hoặc không có dấu cách: "key"\s*:\s*"value"
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

        // Fallback linh hoạt cho khoảng trắng giữa key và value
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

    private Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return result;

        if (query.StartsWith("?")) query = query.Substring(1);
        string[] pairs = query.Split('&');
        foreach (string pair in pairs)
        {
            if (string.IsNullOrEmpty(pair)) continue;
            string[] kv = pair.Split('=');
            string key = UnityWebRequest.UnEscapeURL(kv[0]);
            string val = kv.Length > 1 ? UnityWebRequest.UnEscapeURL(kv[1]) : "";
            result[key] = val;
        }
        return result;
    }

    public void StopLocalListener()
    {
        isListening = false;
        if (httpListener != null)
        {
            try
            {
                httpListener.Stop();
                httpListener.Close();
            }
            catch { }
            httpListener = null;
        }
    }

    private void OnDestroy()
    {
        StopLocalListener();
    }

    private string GetBrowserResponseHtml(bool isSuccess, string name, string email, string errorMessage = null)
    {
        string title = isSuccess ? "Đăng Nhập Google Thành Công!" : "Xác Thực Không Thành Công";
        string color = isSuccess ? "#2ecc71" : "#e74c3c";
        string sub = isSuccess 
            ? $"Xin chào <b>{name}</b> ({email})!<br>Tài khoản Google của bạn đã được kết nối với <b>VAELSTRIDER: RUINBOUND</b>.<br><br><span style='color: #00d2d3; font-weight: bold;'>Bạn có thể đóng tab này và quay lại trò chơi để trải nghiệm ngay!</span>"
            : $"Đăng nhập bằng Google đã bị hủy hoặc gặp sự cố: {errorMessage}<br>Bạn có thể đóng tab này và thử lại trong game.";

        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title} - Vaelstrider</title>
    <style>
        body {{
            background: radial-gradient(circle at center, #141926 0%, #080b12 100%);
            color: #ffffff;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            display: flex;
            align-items: center;
            justify-content: center;
            height: 100vh;
            margin: 0;
            text-align: center;
        }}
        .card {{
            background: rgba(22, 28, 42, 0.95);
            border: 1px solid rgba(255, 215, 0, 0.3);
            box-shadow: 0 10px 40px rgba(0,0,0,0.8);
            border-radius: 16px;
            padding: 40px 50px;
            max-width: 520px;
        }}
        h1 {{
            color: {color};
            font-size: 28px;
            margin-bottom: 12px;
        }}
        h2 {{
            color: #ffd700;
            font-size: 20px;
            letter-spacing: 2px;
            margin-top: 0;
            margin-bottom: 24px;
        }}
        p {{
            color: #c0c6d8;
            font-size: 16px;
            line-height: 1.6;
        }}
    </style>
</head>
<body>
    <div class=""card"">
        <h2>️ VAELSTRIDER: RUINBOUND ️</h2>
        <h1>{title}</h1>
        <p>{sub}</p>
        <p style=""font-size: 13px; color: #7f8fa6; margin-top: 20px;"">Hệ thống xác thực Google OAuth 2.0 PKCE</p>
    </div>
</body>
</html>";
    }
}
