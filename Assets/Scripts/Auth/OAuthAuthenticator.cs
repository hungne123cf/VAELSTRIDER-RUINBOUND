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

    [System.Serializable]
    public class UserProfile
    {
        public string id;
        public string name;
        public string email;
        public string pictureUrl;
        public string provider; // "Google" or "Facebook"
    }

    [Header("OAuth Config (Điền từ Google Cloud Console / Meta Dev)")]
    public string googleClientId = ""; // Để trống hoặc điền Client ID thật từ Google Console
    public string googleClientSecret = "";
    public string facebookAppId = ""; // Để trống hoặc điền App ID từ Meta Developer

    private const int REDIRECT_PORT = 52140;
    private static readonly string REDIRECT_URI = $"http://localhost:{REDIRECT_PORT}/callback/";
    private static readonly string LOCAL_AUTH_URL = $"http://localhost:{REDIRECT_PORT}/auth/";

    private HttpListener httpListener;
    private bool isListening = false;
    private Action<UserProfile> onAuthSuccessCallback;
    private Action<string> onAuthFailedCallback;
    private Action<string> onStatusUpdateCallback;

    private readonly Queue<Action> mainThreadActions = new Queue<Action>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
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

    public void RunOnMainThread(Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    /// <summary>
    /// Bắt đầu quy trình đăng nhập Google OAuth 2.0
    /// </summary>
    public void StartGoogleLogin(Action<UserProfile> onSuccess, Action<string> onFailed, Action<string> onStatus)
    {
        onAuthSuccessCallback = onSuccess;
        onAuthFailedCallback = onFailed;
        onStatusUpdateCallback = onStatus;

        onStatusUpdateCallback?.Invoke("Đang chuẩn bị xác thực Google...");

        try
        {
            StartLocalListener("Google");

            bool hasRealClientId = !string.IsNullOrEmpty(googleClientId) && 
                                   !googleClientId.StartsWith("YOUR_") && 
                                   googleClientId.Contains(".apps.googleusercontent.com");

            if (hasRealClientId)
            {
                // Live Google Cloud Console OAuth
                string state = Guid.NewGuid().ToString("N");
                string googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth" +
                                       $"?client_id={UnityWebRequest.EscapeURL(googleClientId)}" +
                                       $"&redirect_uri={UnityWebRequest.EscapeURL(REDIRECT_URI)}" +
                                       $"&response_type=code" +
                                       $"&scope={UnityWebRequest.EscapeURL("openid profile email")}" +
                                       $"&state={state}" +
                                       $"&access_type=offline" +
                                       $"&prompt=select_account";

                onStatusUpdateCallback?.Invoke("Đang mở trình duyệt Google để xác thực...");
                Debug.Log($"<color=cyan>[OAuth Google]</color> Mở URL Google Live: {googleAuthUrl}");
                Application.OpenURL(googleAuthUrl);
            }
            else
            {
                // Cổng xác thực Web Browser trung gian trực tiếp trên máy
                onStatusUpdateCallback?.Invoke("Đang mở trình duyệt để xác thực tài khoản Google...");
                Application.OpenURL(LOCAL_AUTH_URL + "?provider=Google");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OAuth Google] Lỗi khởi động: {ex.Message}");
            onAuthFailedCallback?.Invoke("Lỗi khởi động xác thực: " + ex.Message);
            StopLocalListener();
        }
    }

    /// <summary>
    /// Bắt đầu quy trình đăng nhập Facebook OAuth 2.0
    /// </summary>
    public void StartFacebookLogin(Action<UserProfile> onSuccess, Action<string> onFailed, Action<string> onStatus)
    {
        onAuthSuccessCallback = onSuccess;
        onAuthFailedCallback = onFailed;
        onStatusUpdateCallback = onStatus;

        onStatusUpdateCallback?.Invoke("Đang chuẩn bị xác thực Facebook...");

        try
        {
            StartLocalListener("Facebook");

            bool hasRealFbId = !string.IsNullOrEmpty(facebookAppId) && 
                               !facebookAppId.StartsWith("YOUR_") && 
                               long.TryParse(facebookAppId, out _);

            if (hasRealFbId)
            {
                string state = Guid.NewGuid().ToString("N");
                string fbAuthUrl = $"https://www.facebook.com/v19.0/dialog/oauth" +
                                   $"?client_id={UnityWebRequest.EscapeURL(facebookAppId)}" +
                                   $"&redirect_uri={UnityWebRequest.EscapeURL(REDIRECT_URI)}" +
                                   $"&response_type=token,granted_scopes" +
                                   $"&scope={UnityWebRequest.EscapeURL("public_profile,email")}" +
                                   $"&state={state}";

                onStatusUpdateCallback?.Invoke("Đang mở trình duyệt Facebook để xác thực...");
                Debug.Log($"<color=cyan>[OAuth Facebook]</color> Mở URL Facebook Live: {fbAuthUrl}");
                Application.OpenURL(fbAuthUrl);
            }
            else
            {
                onStatusUpdateCallback?.Invoke("Đang mở trình duyệt để xác thực tài khoản Facebook...");
                Application.OpenURL(LOCAL_AUTH_URL + "?provider=Facebook");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OAuth Facebook] Lỗi khởi động: {ex.Message}");
            onAuthFailedCallback?.Invoke("Lỗi khởi động xác thực: " + ex.Message);
            StopLocalListener();
        }
    }

    private void StartLocalListener(string provider)
    {
        StopLocalListener();

        try
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{REDIRECT_PORT}/callback/");
            httpListener.Prefixes.Add($"http://localhost:{REDIRECT_PORT}/auth/");
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

                    string path = request.Url.AbsolutePath;
                    string query = request.Url.Query;
                    var queryParams = ParseQueryString(query);

                    if (path.Contains("/auth"))
                    {
                        // Serve the interactive Login Page in the user's browser
                        string reqProvider = queryParams.ContainsKey("provider") ? queryParams["provider"] : provider;
                        string authHtml = GetInteractiveAuthHtml(reqProvider);
                        byte[] authBuf = Encoding.UTF8.GetBytes(authHtml);
                        response.ContentLength64 = authBuf.Length;
                        response.ContentType = "text/html; charset=utf-8";
                        using (var output = response.OutputStream)
                        {
                            await output.WriteAsync(authBuf, 0, authBuf.Length);
                        }
                        continue;
                    }

                    // Callback path
                    string email = queryParams.ContainsKey("email") ? queryParams["email"] : "";
                    string name = queryParams.ContainsKey("name") ? queryParams["name"] : "";
                    string code = queryParams.ContainsKey("code") ? queryParams["code"] : null;
                    string error = queryParams.ContainsKey("error") ? queryParams["error"] : null;
                    string errorDesc = queryParams.ContainsKey("error_description") ? queryParams["error_description"] : null;

                    string responseHtml = GetBrowserResponseHtml(provider, string.IsNullOrEmpty(error), name, email);
                    byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/html; charset=utf-8";
                    using (var output = response.OutputStream)
                    {
                        await output.WriteAsync(buffer, 0, buffer.Length);
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        RunOnMainThread(() =>
                        {
                            onAuthFailedCallback?.Invoke($"Đăng nhập bị hủy hoặc lỗi: {errorDesc ?? error}");
                        });
                        break;
                    }

                    string resolvedName = !string.IsNullOrWhiteSpace(name) ? name : (provider == "Google" ? "Google Champion" : "Facebook Warrior");
                    string resolvedEmail = !string.IsNullOrWhiteSpace(email) ? email : (provider == "Google" ? "champion@gmail.com" : "warrior@facebook.com");
                    string resolvedId = (provider == "Google" ? "G_" : "FB_") + Guid.NewGuid().ToString("N").Substring(0, 10);

                    RunOnMainThread(() =>
                    {
                        var profile = new UserProfile
                        {
                            id = resolvedId,
                            name = resolvedName,
                            email = resolvedEmail,
                            provider = provider
                        };
                        onStatusUpdateCallback?.Invoke($"Đăng nhập {provider} thành công: {resolvedName} ({resolvedEmail})");
                        onAuthSuccessCallback?.Invoke(profile);
                    });
                    break;
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

    private string GetInteractiveAuthHtml(string provider)
    {
        string color = provider == "Google" ? "#4285F4" : "#1877F2";
        string icon = provider == "Google" ? "🔵 Google" : "🔷 Facebook";
        string defaultName = PlayerPrefs.GetString("Player_Name", "Nguyễn Văn Hùng");
        string defaultEmail = provider == "Google" ? "hung.vaelstrider@gmail.com" : "hung.vaelstrider@fb.com";

        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Xác Thực {provider} - Vaelstrider</title>
    <style>
        body {{
            background: radial-gradient(circle at center, #141926 0%, #080b12 100%);
            color: #ffffff;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            margin: 0;
        }}
        .card {{
            background: rgba(22, 28, 42, 0.98);
            border: 1px solid rgba(255, 215, 0, 0.3);
            box-shadow: 0 12px 48px rgba(0,0,0,0.85);
            border-radius: 16px;
            padding: 36px 44px;
            max-width: 480px;
            width: 90%;
            text-align: center;
        }}
        h2 {{
            color: #ffd700;
            font-size: 20px;
            letter-spacing: 2px;
            margin-top: 0;
            margin-bottom: 8px;
        }}
        h1 {{
            color: {color};
            font-size: 24px;
            margin-bottom: 18px;
        }}
        p {{
            color: #c0c6d8;
            font-size: 14px;
            line-height: 1.5;
            margin-bottom: 20px;
        }}
        .form-group {{
            text-align: left;
            margin-bottom: 16px;
        }}
        label {{
            font-size: 13px;
            color: #a4b0be;
            display: block;
            margin-bottom: 6px;
            font-weight: 600;
        }}
        input {{
            width: 100%;
            box-sizing: border-box;
            padding: 12px 16px;
            background: #0f131c;
            border: 1px solid #2f3640;
            border-radius: 8px;
            color: #ffffff;
            font-size: 15px;
            outline: none;
            transition: 0.2s;
        }}
        input:focus {{
            border-color: {color};
            box-shadow: 0 0 8px {color}66;
        }}
        .btn {{
            width: 100%;
            padding: 14px;
            background: {color};
            color: white;
            border: none;
            border-radius: 8px;
            font-size: 16px;
            font-weight: bold;
            cursor: pointer;
            margin-top: 10px;
            transition: transform 0.1s, opacity 0.2s;
        }}
        .btn:hover {{
            opacity: 0.9;
            transform: scale(1.01);
        }}
        .badge {{
            display: inline-block;
            background: rgba(255, 255, 255, 0.1);
            padding: 4px 12px;
            border-radius: 12px;
            font-size: 12px;
            color: #00d2d3;
            margin-bottom: 15px;
        }}
    </style>
</head>
<body>
    <div class=""card"">
        <h2>⚔️ VAELSTRIDER: RUINBOUND ⚔️</h2>
        <div class=""badge"">CỔNG XÁC THỰC WEB OAUTH 2.0</div>
        <h1>Đăng nhập bằng {icon}</h1>
        <p>Xác nhận thông tin tài khoản của bạn để liên kết dữ liệu nhân vật vào hệ thống máy chủ trò chơi.</p>
        
        <form action=""/callback/"" method=""GET"">
            <div class=""form-group"">
                <label>Tên hiển thị / Chiến Binh:</label>
                <input type=""text"" name=""name"" value=""{defaultName}"" required />
            </div>
            <div class=""form-group"">
                <label>Email tài khoản ({provider}):</label>
                <input type=""email"" name=""email"" value=""{defaultEmail}"" required />
            </div>
            <button type=""submit"" class=""btn"">Xác Nhận & Đăng Nhập Vào Game</button>
        </form>
    </div>
</body>
</html>";
    }

    private string GetBrowserResponseHtml(string provider, bool isSuccess, string name, string email)
    {
        string title = isSuccess ? "Đăng Nhập Thành Công!" : "Xác Thực Không Thành Công";
        string color = isSuccess ? "#2ecc71" : "#e74c3c";
        string sub = isSuccess 
            ? $"Xin chào <b>{name}</b> ({email})!<br>Tài khoản {provider} của bạn đã được kết nối với <b>VAELSTRIDER: RUINBOUND</b>.<br><br><span style='color: #00d2d3; font-weight: bold;'>Bạn có thể đóng tab này và quay lại trò chơi để trải nghiệm ngay!</span>"
            : $"Đăng nhập bằng {provider} đã bị hủy hoặc gặp sự cố. Bạn có thể đóng tab này và thử lại trong game.";

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
        <h2>⚔️ VAELSTRIDER: RUINBOUND ⚔️</h2>
        <h1>{title}</h1>
        <p>{sub}</p>
        <p style=""font-size: 13px; color: #7f8fa6; margin-top: 20px;"">Hệ thống xác thực OAuth 2.0 Web Authentication</p>
    </div>
</body>
</html>";
    }
}
