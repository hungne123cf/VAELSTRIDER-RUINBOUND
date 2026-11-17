using System;
using UnityEngine;

/// <summary>
/// Bridge C# giao tiếp 2 chiều với Android Native Google Account Picker (AccountManager & Google Play Services)
/// Nhận Email, Display Name và URL Avatar của tài khoản Google.
/// </summary>
public static class AndroidGoogleSignInBridge
{
    [System.Serializable]
    public class GoogleAccountProfile
    {
        public string email = "";
        public string displayName = "";
        public string photoUrl = "";
    }

    public static bool IsSupported
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }

    public static void LaunchAccountPicker(Action<GoogleAccountProfile> onSuccess, Action<string> onCanceled, Action<string> onError)
    {
        // Khởi tạo sẵn OAuthAuthenticator trên Unity Main Thread trước khi sang Android UI Thread
        // để đảm bảo hàng đợi RunOnMainThread luôn hoạt động và không bao giờ gọi FindAnyObjectByType từ thread phụ.
        OAuthAuthenticator.EnsureInstance();

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (currentActivity != null)
                    {
                        var proxy = new GooglePickerListenerProxy(onSuccess, onCanceled, onError);
                        using (var pickerClass = new AndroidJavaClass("com.HungNe.VaelstriderRuinbound.GoogleAccountPickerFragment"))
                        {
                            pickerClass.CallStatic("setListener", proxy);
                            pickerClass.CallStatic("showPicker", currentActivity);
                            return;
                        }
                    }
                }
            }
            onError?.Invoke("Không tìm thấy Android UnityPlayer.currentActivity!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AndroidGoogleSignInBridge] Lỗi gọi native picker: {ex.Message}");
            onError?.Invoke(ex.Message);
        }
#else
        onError?.Invoke("Native Google Account Picker chỉ hỗ trợ trên thiết bị Android thật.");
#endif
    }

    public static void LaunchAccountPicker(Action<string> onSuccessEmail, Action<string> onCanceled, Action<string> onError)
    {
        LaunchAccountPicker(
            onSuccess: (profile) => onSuccessEmail?.Invoke(profile != null ? profile.email : ""),
            onCanceled: onCanceled,
            onError: onError
        );
    }

#if UNITY_ANDROID
    private class GooglePickerListenerProxy : AndroidJavaProxy
    {
        private readonly Action<GoogleAccountProfile> _onSuccessProfile;
        private readonly Action<string> _onCanceled;
        private readonly Action<string> _onError;

        public GooglePickerListenerProxy(Action<GoogleAccountProfile> onSuccessProfile, Action<string> onCanceled, Action<string> onError)
            : base("com.HungNe.VaelstriderRuinbound.GoogleAccountPickerFragment$GooglePickerListener")
        {
            _onSuccessProfile = onSuccessProfile;
            _onCanceled = onCanceled;
            _onError = onError;
        }

        private static void DispatchToMain(Action action)
        {
            if (action == null) return;
            // Chỉ đẩy vào hàng đợi thread-safe, tuyệt đối không gọi EnsureInstance() từ Android UI Thread!
            OAuthAuthenticator.RunOnMainThread(action);
        }

        public void onSuccess(string email, string displayName, string photoUrl)
        {
            DispatchToMain(() =>
            {
                var profile = new GoogleAccountProfile
                {
                    email = email != null ? email.Trim() : "",
                    displayName = displayName != null ? displayName.Trim() : "",
                    photoUrl = photoUrl != null ? photoUrl.Trim() : ""
                };
                _onSuccessProfile?.Invoke(profile);
            });
        }

        // Backward compatibility overload
        public void onSuccess(string email)
        {
            onSuccess(email, "", "");
        }

        public void onCanceled(string reason)
        {
            DispatchToMain(() => _onCanceled?.Invoke(reason));
        }

        public void onError(string error)
        {
            DispatchToMain(() => _onError?.Invoke(error));
        }
    }
#endif
}
