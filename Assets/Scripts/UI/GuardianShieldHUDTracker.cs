using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Guardian Shield HUD Cooldown Tracker (Bộ Theo Dõi Hồi Chiêu Thánh Khiên)
/// Giải pháp tối ưu hiển thị thời gian hồi chiêu của Lõi [Thánh Khiên Hộ Mệnh] (Divine Aegis - 45s CD):
/// - Tự động hiển thị ngay cạnh Thanh Máu (Player Health Bar) khi người chơi sở hữu Lõi Thánh Khiên.
/// - Khi SẴN SÀNG (Ready): Biểu tượng Khiên Hoàng Kim rực sáng óng ánh, kèm nhãn [KHIÊN: SẴN SÀNG].
/// - Khi ĐANG HỒI CHIÊU (Cooldown): Icon tối mờ, vòng xoay quét Radial 360 độ nạp dần,
///   kèm số giây đếm lùi TO RÕ NÉT (45s, 30s, 15s, 4.2s...).
/// - Khi VỪA HỒI PHỤC (Recharged): Hiệu ứng nổ bừng ánh sáng vàng kim + âm thanh báo hiệu.
/// </summary>
public class GuardianShieldHUDTracker : MonoBehaviour
{
    private static GuardianShieldHUDTracker instance;
    public static GuardianShieldHUDTracker Instance => instance;

    [Header("UI Components")]
    private GameObject rootContainer;
    private Image shieldIconImg;
    private Image cooldownRadialImg;
    private Image borderRingImg;
    private TextMeshProUGUI cooldownText;
    private TextMeshProUGUI statusTagText;

    private bool wasOnCooldown = false;
    private float pulseTimer = 0f;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        EnsureUIBuilt();
    }

    void Update()
    {
        if (AugmentManager.Instance == null)
        {
            if (rootContainer != null && rootContainer.activeSelf) rootContainer.SetActive(false);
            return;
        }

        bool hasAegis = AugmentManager.Instance.HasDivineAegis;
        if (!hasAegis)
        {
            if (rootContainer != null && rootContainer.activeSelf) rootContainer.SetActive(false);
            return;
        }

        if (rootContainer != null && !rootContainer.activeSelf)
        {
            rootContainer.SetActive(true);
        }

        float remaining = AugmentManager.Instance.DivineAegisCooldownRemaining;
        float maxCd = AugmentManager.Instance.DivineAegisMaxCooldown;

        if (remaining > 0f)
        {
            wasOnCooldown = true;

            // Đang hồi chiêu (Cooldown Active)
            if (cooldownRadialImg != null)
            {
                cooldownRadialImg.gameObject.SetActive(true);
                cooldownRadialImg.fillAmount = Mathf.Clamp01(remaining / maxCd);
            }

            if (shieldIconImg != null)
            {
                shieldIconImg.color = new Color(0.40f, 0.40f, 0.45f, 0.55f); // Làm tối khi đang hồi
            }

            if (borderRingImg != null)
            {
                borderRingImg.color = new Color(0.55f, 0.45f, 0.25f, 0.75f);
            }

            if (cooldownText != null)
            {
                string secStr = remaining >= 10f ? $"{Mathf.CeilToInt(remaining)}s" : $"{remaining:0.#}s";
                cooldownText.text = secStr;
                cooldownText.color = new Color(1f, 0.85f, 0.35f);
            }

            if (statusTagText != null)
            {
                statusTagText.text = $"<color=#FF9944>HỒI {Mathf.CeilToInt(remaining)}s</color>";
            }
        }
        else
        {
            // Sẵn sàng kích hoạt (READY)
            if (wasOnCooldown)
            {
                wasOnCooldown = false;
                TriggerRechargeFlash();
            }

            if (cooldownRadialImg != null && cooldownRadialImg.gameObject.activeSelf)
            {
                cooldownRadialImg.gameObject.SetActive(false);
            }

            // Hiệu ứng thở nhẹ ánh vàng khi sẵn sàng
            pulseTimer += Time.deltaTime * 3.5f;
            float glow = 0.85f + Mathf.Sin(pulseTimer) * 0.15f;

            if (shieldIconImg != null)
            {
                shieldIconImg.color = new Color(1f, 0.88f * glow, 0.40f * glow, 1f);
            }

            if (borderRingImg != null)
            {
                borderRingImg.color = new Color(1f, 0.82f, 0.25f, glow);
            }

            if (cooldownText != null)
            {
                cooldownText.text = "";
            }

            if (statusTagText != null)
            {
                statusTagText.text = "<color=#52F0FF><b>SẴN SÀNG</b></color>";
            }
        }
    }

    private void TriggerRechargeFlash()
    {
        if (rootContainer != null)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        if (rootContainer == null) yield break;
        Transform t = rootContainer.transform;
        Vector3 baseScale = Vector3.one;

        float elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.unscaledDeltaTime;
            float factor = elapsed / 0.25f;
            t.localScale = Vector3.Lerp(Vector3.one * 1.25f, baseScale, Mathf.SmoothStep(0f, 1f, factor));
            yield return null;
        }
        t.localScale = baseScale;
    }

    public static GuardianShieldHUDTracker EnsureTrackerExists()
    {
        if (instance != null && instance.gameObject != null) return instance;

        Canvas targetCanvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (targetCanvas == null) targetCanvas = Object.FindAnyObjectByType<Canvas>();
        if (targetCanvas == null) return null;

        Transform existing = targetCanvas.transform.Find("GuardianShieldHUDTracker");
        if (existing != null)
        {
            instance = existing.GetComponent<GuardianShieldHUDTracker>();
            if (instance != null) return instance;
        }

        GameObject trackerObj = new GameObject("GuardianShieldHUDTracker", typeof(RectTransform));
        trackerObj.transform.SetParent(targetCanvas.transform, false);

        instance = trackerObj.AddComponent<GuardianShieldHUDTracker>();
        instance.EnsureUIBuilt();

        return instance;
    }

    private void EnsureUIBuilt()
    {
        if (rootContainer != null) return;

        Canvas targetCanvas = GetComponentInParent<Canvas>();
        if (targetCanvas == null) targetCanvas = GameAutoBootstrap.GetMainGameplayCanvas();

        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);

        // Đặt ở vị trí tuyệt đẹp: Ngay cạnh mép phải của thanh Máu (PlayerHealthBar nằm ở x=244, width=360 -> mép phải là 424f)
        // Hoặc ngay phía dưới thanh EXP: x=435, y=-26
        rt.anchoredPosition = new Vector2(434f, -24f);
        rt.sizeDelta = new Vector2(110f, 38f);

        rootContainer = gameObject;

        // 1. Nền tối bán nguyệt
        GameObject bgObj = new GameObject("BgPlate", typeof(RectTransform), typeof(Image), typeof(Outline));
        bgObj.transform.SetParent(transform, false);
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

        Outline bgOutline = bgObj.GetComponent<Outline>();
        bgOutline.effectColor = new Color(0.95f, 0.78f, 0.35f, 0.85f);
        bgOutline.effectDistance = new Vector2(1f, -1f);

        // 2. Icon Chiếc Khiên Thánh bên trái (Size 28x28)
        GameObject shieldBox = new GameObject("ShieldBox", typeof(RectTransform));
        shieldBox.transform.SetParent(transform, false);
        var sRt = shieldBox.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0.5f);
        sRt.anchorMax = new Vector2(0f, 0.5f);
        sRt.pivot = new Vector2(0f, 0.5f);
        sRt.anchoredPosition = new Vector2(4f, 0f);
        sRt.sizeDelta = new Vector2(28f, 28f);

        // Viền vòng Bezel
        GameObject borderObj = new GameObject("BorderRing", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(shieldBox.transform, false);
        var bRt = borderObj.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.offsetMin = bRt.offsetMax = Vector2.zero;

        borderRingImg = borderObj.GetComponent<Image>();
        borderRingImg.sprite = AugmentHUDBar.Instance != null ? AugmentHUDBar.GetRunicRing() : null;
        borderRingImg.color = new Color(1f, 0.82f, 0.25f, 1f);

        // Hình chiếc khiên
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(shieldBox.transform, false);
        var iRt = iconObj.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0.15f, 0.15f);
        iRt.anchorMax = new Vector2(0.85f, 0.85f);
        iRt.offsetMin = iRt.offsetMax = Vector2.zero;

        shieldIconImg = iconObj.GetComponent<Image>();
        shieldIconImg.sprite = AugmentIconHelper.GetAugmentSprite(AugmentDatabase.AugmentType.DivineAegis);
        shieldIconImg.preserveAspect = true;
        shieldIconImg.color = new Color(1f, 0.88f, 0.40f, 1f);

        // Vòng quét tròn Cooldown (Radial 360 Fill)
        GameObject cdMaskObj = new GameObject("CooldownRadial", typeof(RectTransform), typeof(Image));
        cdMaskObj.transform.SetParent(shieldBox.transform, false);
        var cdRt = cdMaskObj.GetComponent<RectTransform>();
        cdRt.anchorMin = Vector2.zero;
        cdRt.anchorMax = Vector2.one;
        cdRt.offsetMin = cdRt.offsetMax = Vector2.zero;

        cooldownRadialImg = cdMaskObj.GetComponent<Image>();
        cooldownRadialImg.sprite = AugmentHUDBar.Instance != null ? AugmentHUDBar.GetObsidianCore() : null;
        cooldownRadialImg.type = Image.Type.Filled;
        cooldownRadialImg.fillMethod = Image.FillMethod.Radial360;
        cooldownRadialImg.fillOrigin = (int)Image.Origin360.Top;
        cooldownRadialImg.fillClockwise = false;
        cooldownRadialImg.color = new Color(0f, 0f, 0f, 0.75f);
        cdMaskObj.SetActive(false);

        // 3. Số giây đếm lùi to rõ ràng đè lên Icon khi hồi chiêu
        GameObject cdTxtObj = new GameObject("CdNumText", typeof(RectTransform), typeof(TextMeshProUGUI));
        cdTxtObj.transform.SetParent(shieldBox.transform, false);
        var cntRt = cdTxtObj.GetComponent<RectTransform>();
        cntRt.anchorMin = Vector2.zero;
        cntRt.anchorMax = Vector2.one;
        cntRt.offsetMin = cntRt.offsetMax = Vector2.zero;

        cooldownText = cdTxtObj.GetComponent<TextMeshProUGUI>();
        cooldownText.fontSize = 11f;
        cooldownText.fontStyle = FontStyles.Bold;
        cooldownText.alignment = TextAlignmentOptions.Center;
        cooldownText.color = new Color(1f, 0.90f, 0.35f);
        SoulslikeHUDStyler.SetSafeOutline(cooldownText, 0.45f, Color.black);

        // 4. Nhãn trạng thái bên phải icon: [SẴN SÀNG] hoặc [HỒI 38s]
        GameObject tagObj = new GameObject("StatusTag", typeof(RectTransform), typeof(TextMeshProUGUI));
        tagObj.transform.SetParent(transform, false);
        var tRt = tagObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.32f, 0f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.offsetMin = new Vector2(0f, 2f);
        tRt.offsetMax = new Vector2(-4f, -2f);

        statusTagText = tagObj.GetComponent<TextMeshProUGUI>();
        statusTagText.fontSize = 10f;
        statusTagText.fontStyle = FontStyles.Bold;
        statusTagText.alignment = TextAlignmentOptions.Left;
        statusTagText.color = new Color(0.35f, 0.90f, 1f);
        SoulslikeHUDStyler.SetSafeOutline(statusTagText, 0.35f, Color.black);

        rootContainer.SetActive(false); // Ẩn khi chưa nhặt Lõi
    }
}
