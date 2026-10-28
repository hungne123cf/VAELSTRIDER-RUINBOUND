using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;

/// <summary>
/// VAELSTRIDER: RUINBOUND - Acid Downpour Hazard Manager (Chu Kỳ 4: Mưa Axit Ăn Mòn)
/// Cơ chế môi trường: Mưa axit ăn mòn & Quy tắc Khát Máu (Bloodrush Combat):
/// - Bầu trời đổ mưa axit màu lục huỳnh quang bốc khói, rút máu người chơi liên tục (-2.5% Máu mỗi 1.6s).
/// - Người chơi BẮT BUỘC phải chém trúng quái liên tục để tích tầng [TRUNG HÒA AXIT].
/// - Mỗi đòn trúng quái cho 1.8s miễn nhiễm axit (cộng dồn tối đa 5s) và hồi phục 1% Máu!
/// - Triệt tiêu lối chơi trốn chạy câu giờ, kích thích chặt chém dồn dập tột độ!
/// </summary>
public class AcidDownpourHazardManager : MonoBehaviour
{
    private static AcidDownpourHazardManager instance;
    public static AcidDownpourHazardManager Instance => instance;

    [Header("State")]
    private float neutralizationTimer = 3.5f; // Khởi đầu cho 3.5s an toàn
    private float maxNeutralizationTime = 5.0f;
    private float corrosionDamageInterval = 1.6f;
    private float corrosionTimer = 0f;

    private PlayerHealth playerHealth;
    private PlayerController playerController;

    // HUD Indicator
    private GameObject hudObj;
    private TextMeshProUGUI hudText;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        if (CorruptedCycleManager.CurrentRealm != CalamityRealm.AcidDownpour)
        {
            Destroy(gameObject);
            return;
        }

        FindPlayer();
        ApplyAcidLighting();
        CreateAcidHUD();

        PlayerCombat.OnPlayerHitEnemy += HandlePlayerHitEnemy;
    }

    void OnDestroy()
    {
        PlayerCombat.OnPlayerHitEnemy -= HandlePlayerHitEnemy;
        if (hudObj != null) Destroy(hudObj);
    }

    public static void EnsureManager()
    {
        if (CorruptedCycleManager.CurrentRealm != CalamityRealm.AcidDownpour) return;

        if (FindAnyObjectByType<AcidDownpourHazardManager>() == null)
        {
            GameObject obj = new GameObject("AcidDownpourHazardManager");
            obj.AddComponent<AcidDownpourHazardManager>();
        }
    }

    private void FindPlayer()
    {
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            playerController = pc;
            playerHealth = pc.GetComponent<PlayerHealth>();
        }
    }

    private void ApplyAcidLighting()
    {
        var lights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l != null && l.lightType == Light2D.LightType.Global)
            {
                l.color = new Color(0.12f, 0.28f, 0.10f, 1f); // Toxic Emerald Green
                l.intensity = 0.85f;
            }
        }
    }

    void Update()
    {
        if (CorruptedCycleManager.CurrentRealm != CalamityRealm.AcidDownpour) return;

        if (playerHealth == null)
        {
            FindPlayer();
            if (playerHealth == null) return;
        }

        // Đếm ngược thời gian Kháng Axit
        if (neutralizationTimer > 0f)
        {
            neutralizationTimer -= Time.deltaTime;
            if (neutralizationTimer < 0f) neutralizationTimer = 0f;
        }
        else
        {
            // Bị nhiễm axit! Rút máu theo chu kỳ
            corrosionTimer -= Time.deltaTime;
            if (corrosionTimer <= 0f)
            {
                corrosionTimer = corrosionDamageInterval;
                ApplyAcidCorrosionDamage();
            }
        }

        UpdateHUD();
    }

    private void HandlePlayerHitEnemy(GameObject enemy, int damage)
    {
        if (CorruptedCycleManager.CurrentRealm != CalamityRealm.AcidDownpour) return;

        // Thưởng khi chém trúng quái: Tăng thêm 1.8s Kháng Axit
        neutralizationTimer = Mathf.Min(maxNeutralizationTime, neutralizationTimer + 1.8f);

        // Hồi phục 1% Máu tối đa
        if (playerHealth != null && playerHealth.MaxHealth > 0)
        {
            int heal = Mathf.Max(1, Mathf.RoundToInt(playerHealth.MaxHealth * 0.01f));
            playerHealth.Heal(heal);
        }
    }

    private void ApplyAcidCorrosionDamage()
    {
        if (playerHealth == null || playerHealth.CurrentHealth <= 0) return;

        int dmg = Mathf.Max(2, Mathf.RoundToInt(playerHealth.MaxHealth * 0.025f * CorruptedCycleManager.DamageMultiplier));
        playerHealth.TakeDamage(dmg);

        DamagePopup.CreateText(
            playerHealth.transform.position + Vector3.up * 0.9f,
            $"<color=#22C55E>-{dmg}</color>",
            new Color(0.35f, 0.95f, 0.35f)
        );

        SoundManager.EnsureInstance()?.PlayPlayerHurt();
    }

    private void CreateAcidHUD()
    {
        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        hudObj = new GameObject("AcidStatusHUD", typeof(RectTransform), typeof(CanvasRenderer));
        hudObj.layer = 5;
        hudObj.transform.SetParent(canvas.transform, false);

        var rt = hudObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 65f);
        rt.sizeDelta = new Vector2(360f, 30f);

        hudText = hudObj.AddComponent<TextMeshProUGUI>();
        hudText.fontSize = 13.5f;
        hudText.fontStyle = FontStyles.Bold;
        hudText.alignment = TextAlignmentOptions.Center;
        SoulslikeHUDStyler.SetSafeOutline(hudText, 0.35f, Color.black);
    }

    private void UpdateHUD()
    {
        if (hudText == null) return;

        if (neutralizationTimer > 0f)
        {
            hudText.text = $"<color=#22C55E>[KHÁNG AXIT] CÒN LẠI: {neutralizationTimer:0.0}s</color>";
        }
        else
        {
            float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
            Color warnColor = Color.Lerp(new Color(1f, 0.25f, 0.25f), new Color(0.2f, 0.95f, 0.2f), pulse);
            hudText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(warnColor)}>[NHIỄM ĐỘC AXIT] HÃY TẤN CÔNG ĐỂ SỐNG SÓT!</color>";
        }
    }
}
