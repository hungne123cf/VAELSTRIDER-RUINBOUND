using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EmailOtpAuthService : MonoBehaviour
{
    public static EmailOtpAuthService Instance { get; private set; }

    [Header("SMTP Configuration (Tùy chọn cấu hình riêng)")]
    public string smtpHost = "smtp.gmail.com";
    public int smtpPort = 587;
    public string senderEmail = "";
    public string senderAppPassword = "";

    [Header("OTP Runtime State")]
    public string currentTargetEmail = "";
    public string currentGeneratedOtp = "";
    public DateTime otpExpireTime;

    private readonly Queue<Action> mainThreadQueue = new Queue<Action>();

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
        lock (mainThreadQueue)
        {
            while (mainThreadQueue.Count > 0)
            {
                mainThreadQueue.Dequeue()?.Invoke();
            }
        }
    }

    public void RunOnMainThread(Action action)
    {
        lock (mainThreadQueue)
        {
            mainThreadQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Gửi mã xác thực OTP 6 số đến địa chỉ email mục tiêu
    /// </summary>
    public void SendOtpCode(string targetEmail, Action onSuccess, Action<string> onFailed)
    {
        if (string.IsNullOrWhiteSpace(targetEmail) || !targetEmail.Contains("@") || !targetEmail.Contains("."))
        {
            onFailed?.Invoke("Địa chỉ Email không hợp lệ. Vui lòng kiểm tra lại!");
            return;
        }

        currentTargetEmail = targetEmail.Trim();
        currentGeneratedOtp = UnityEngine.Random.Range(100000, 999999).ToString();
        otpExpireTime = DateTime.Now.AddMinutes(5);

        Debug.Log($"<color=yellow><b>[MÃ OTP CỦA BẠN]</b></color> Mã xác thực 6 số cho <b>{currentTargetEmail}</b> là: <color=cyan><b>{currentGeneratedOtp}</b></color>");

        StartCoroutine(SendEmailHttpRoutine(currentTargetEmail, currentGeneratedOtp, onSuccess, onFailed));
    }

    private IEnumerator SendEmailHttpRoutine(string toEmail, string otpCode, Action onSuccess, Action<string> onFailed)
    {
        // 1. Nếu có cấu hình SMTP riêng với App Password
        if (!string.IsNullOrEmpty(senderEmail) && !string.IsNullOrEmpty(senderAppPassword))
        {
            Task.Run(() =>
            {
                try
                {
                    SendEmailViaSmtp(toEmail, otpCode);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SMTP Exception] {ex.Message}");
                }
            });
        }

        // 2. Gửi thư qua Cloud Dispatcher API (FormSubmit / Web3Forms) để thư bay thẳng vào hộp thư Gmail
        WWWForm form = new WWWForm();
        form.AddField("_subject", $"⚔️ [VAELSTRIDER] Mã xác thực OTP của bạn: {otpCode}");
        form.AddField("_captcha", "false");
        form.AddField("_template", "box");
        form.AddField("Tro_Choi", "Vaelstrider: Ruinbound");
        form.AddField("Ma_Xac_Thuc_OTP", otpCode);
        form.AddField("Thoi_Han_Hieu_Luc", "5 Phút");
        form.AddField("Loi_Nhan", $"Xin chào! Mã OTP để xác thực đăng nhập tài khoản game của bạn là: {otpCode}. Vui lòng nhập mã này vào trò chơi.");

        string postUrl = $"https://formsubmit.co/ajax/{toEmail}";

        using (UnityWebRequest req = UnityWebRequest.Post(postUrl, form))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=green>[EMAIL SENT]</color> Đã gửi email chứa mã OTP {otpCode} tới {toEmail} thành công!");
            }
            else
            {
                Debug.LogWarning($"[HTTP Email Notice] {req.error} - Mã OTP nội bộ đã sẵn sàng: {otpCode}");
            }
        }

        onSuccess?.Invoke();
    }

    /// <summary>
    /// Kiểm tra mã OTP người chơi nhập vào
    /// </summary>
    public bool VerifyOtp(string enteredOtp, out string errorMessage)
    {
        errorMessage = "";

        if (string.IsNullOrEmpty(currentGeneratedOtp))
        {
            errorMessage = "Chưa có mã OTP nào được gửi. Vui lòng bấm gửi mã!";
            return false;
        }

        if (DateTime.Now > otpExpireTime)
        {
            errorMessage = "Mã OTP đã hết hạn (5 phút). Vui lòng gửi lại mã mới!";
            return false;
        }

        if (enteredOtp.Trim() != currentGeneratedOtp)
        {
            errorMessage = $"Mã OTP không chính xác (Mã đúng gồm 6 chữ số). Vui lòng kiểm tra lại!";
            return false;
        }

        return true;
    }

    private void SendEmailViaSmtp(string toEmail, string otpCode)
    {
        using (MailMessage mail = new MailMessage())
        {
            mail.From = new MailAddress(senderEmail, "Vaelstrider: Ruinbound");
            mail.To.Add(toEmail);
            mail.Subject = $"⚔️ [VAELSTRIDER] Mã xác thực tài khoản của bạn: {otpCode}";
            mail.Body = $"Mã xác thực OTP tài khoản của bạn là: {otpCode}\nMã có hiệu lực trong 5 phút.";
            mail.IsBodyHtml = false;

            using (SmtpClient smtp = new SmtpClient(smtpHost, smtpPort))
            {
                smtp.Credentials = new NetworkCredential(senderEmail, senderAppPassword);
                smtp.EnableSsl = true;
                smtp.Timeout = 8000;
                smtp.Send(mail);
            }
        }
    }
}
