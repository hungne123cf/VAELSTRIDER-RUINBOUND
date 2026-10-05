using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CloudAccountService : MonoBehaviour
{
    public static CloudAccountService Instance { get; private set; }

    [Header("Cloud Server Configuration")]
    [Tooltip("Địa chỉ máy chủ Cloud REST Database lưu trữ tài khoản trực tuyến")]
    public string cloudDatabaseUrl = "https://vaelstrider-ruinbound-default-rtdb.asia-southeast1.firebasedatabase.app";

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

    /// <summary>
    /// Đăng ký tài khoản mới lên Máy chủ Đám mây (Cloud Server)
    /// </summary>
    public void RegisterAccountCloud(AccountDatabase.AccountData account, Action<bool, string> callback)
    {
        StartCoroutine(RegisterCloudRoutine(account, callback));
    }

    private IEnumerator RegisterCloudRoutine(AccountDatabase.AccountData account, Action<bool, string> callback)
    {
        string endpoint = $"{cloudDatabaseUrl.TrimEnd('/')}/accounts/{account.username.ToLower()}.json";
        
        // 1. Kiểm tra xem tài khoản đã có trên Cloud chưa
        using (UnityWebRequest checkReq = UnityWebRequest.Get(endpoint))
        {
            checkReq.timeout = 8;
            yield return checkReq.SendWebRequest();

            if (checkReq.result == UnityWebRequest.Result.Success)
            {
                string resText = checkReq.downloadHandler.text;
                if (!string.IsNullOrEmpty(resText) && resText != "null")
                {
                    callback?.Invoke(false, $"Tài khoản '{account.username}' đã tồn tại trên Máy chủ Đám mây!");
                    yield break;
                }
            }
        }

        // 2. Đẩy dữ liệu tài khoản mới lên Cloud
        string jsonData = JsonUtility.ToJson(account);
        using (UnityWebRequest putReq = UnityWebRequest.Put(endpoint, jsonData))
        {
            putReq.SetRequestHeader("Content-Type", "application/json");
            putReq.timeout = 8;
            yield return putReq.SendWebRequest();

            if (putReq.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=green>[CLOUD DB]</color> Đã lưu tài khoản mới '{account.username}' lên Máy chủ Đám mây thành công!");
                callback?.Invoke(true, "Đăng ký đám mây thành công!");
            }
            else
            {
                Debug.LogWarning($"[CLOUD DB Notice] Không thể kết nối Cloud ({putReq.error}), lưu trữ cục bộ.");
                callback?.Invoke(true, "Đã tạo tài khoản!");
            }
        }
    }

    /// <summary>
    /// Đăng nhập và Tải dữ liệu tài khoản từ Máy chủ Đám mây về máy hiện tại
    /// </summary>
    public void LoginAndFetchCloud(string username, string passwordHash, Action<bool, AccountDatabase.AccountData, string> callback)
    {
        StartCoroutine(LoginCloudRoutine(username, passwordHash, callback));
    }

    private IEnumerator LoginCloudRoutine(string username, string passwordHash, Action<bool, AccountDatabase.AccountData, string> callback)
    {
        string endpoint = $"{cloudDatabaseUrl.TrimEnd('/')}/accounts/{username.Trim().ToLower()}.json";

        using (UnityWebRequest req = UnityWebRequest.Get(endpoint))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string resText = req.downloadHandler.text;
                if (!string.IsNullOrEmpty(resText) && resText != "null")
                {
                    try
                    {
                        var cloudAcc = JsonUtility.FromJson<AccountDatabase.AccountData>(resText);
                        if (cloudAcc != null && cloudAcc.passwordHash == passwordHash)
                        {
                            Debug.Log($"<color=green>[CLOUD DB]</color> Đã đồng bộ thành công tiến trình từ Cloud cho '{username}' (Linh hồn: {cloudAcc.aethelShards}, Máu: {cloudAcc.healthPotions})!");
                            callback?.Invoke(true, cloudAcc, "Đăng nhập Đám mây thành công!");
                            yield break;
                        }
                        else
                        {
                            callback?.Invoke(false, null, "Mật khẩu không chính xác!");
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[CLOUD DB] Lỗi phân tích dữ liệu: {ex.Message}");
                    }
                }
            }
            
            // Nếu không tìm thấy trên Cloud hoặc mất mạng, thử nạp từ Local Database
            callback?.Invoke(false, null, "Không tìm thấy tài khoản trên Cloud hoặc chưa kết nối mạng!");
        }
    }

    /// <summary>
    /// Đồng bộ tiến trình chơi (Linh hồn, Bình máu, Trang bị) lên Máy chủ Đám mây trong thời gian thực
    /// </summary>
    public void SyncProgressToCloud(string username, long shards, int potions, string armor, string weapon)
    {
        if (string.IsNullOrEmpty(username)) return;
        StartCoroutine(SyncProgressRoutine(username.Trim().ToLower(), shards, potions, armor, weapon));
    }

    private IEnumerator SyncProgressRoutine(string cleanUser, long shards, int potions, string armor, string weapon)
    {
        string endpoint = $"{cloudDatabaseUrl.TrimEnd('/')}/accounts/{cleanUser}.json";

        string patchJson = $"{{\"aethelShards\":{shards},\"healthPotions\":{potions},\"equippedArmor\":\"{armor}\",\"equippedWeapon\":\"{weapon}\",\"lastLoginDate\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(patchJson);

        using (UnityWebRequest req = new UnityWebRequest(endpoint, "PATCH"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 6;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=cyan>[CLOUD SYNC]</color> Đã đồng bộ lên Đám mây cho '{cleanUser}': {shards} Shards, {potions} Potions, Armor: {armor}");
            }
        }
    }
}
