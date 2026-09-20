using UnityEngine;

/// <summary>
/// Quan ly He thong Nang Luong (Energy/Stamina) & Noi Nang (Inner Energy/Qi).
/// - Nang luong dung de thi trien ky nang (Luot, Tuyet ky).
/// - Noi nang tich tu khi chien dau, khi day 100% se kich hoat Lop Giap Ao Ho Than (Protective Energy Shield).
/// </summary>
public class PlayerEnergySystem : MonoBehaviour
{
    public static PlayerEnergySystem Instance { get; private set; }

    [Header("Nang Luong (Energy)")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float currentEnergy = 100f;
    [SerializeField] private float energyRegenRate = 15f; // Hoi 15 NL moi giay

    [Header("Noi Nang (Inner Energy / Qi)")]
    [SerializeField] private float maxInnerEnergy = 100f;
    [SerializeField] private float currentInnerEnergy = 0f;

    [Header("Lop Giap Ao Ho Than (Protective Shield)")]
    [SerializeField] private int maxShieldHealth = 80;
    [SerializeField] private int currentShieldHealth = 0;
    [SerializeField] private float shieldDuration = 20f; // Ton tai toi da 20s
    private float shieldTimer = 0f;
    private bool isShieldActive = false;

    private PlayerShieldVisual shieldVisual;

    void Awake()
    {
        Instance = this;
        currentEnergy = maxEnergy;
        currentInnerEnergy = 0f;
    }

    void Start()
    {
        shieldVisual = GetComponentInChildren<PlayerShieldVisual>();
        if (shieldVisual == null)
        {
            GameObject vfxObj = new GameObject("Player_Shield_Visual");
            vfxObj.transform.SetParent(transform, false);
            shieldVisual = vfxObj.AddComponent<PlayerShieldVisual>();
        }
    }

    void Update()
    {
        // 1. Tu dong hoi phuc Nang Luong theo thoi gian
        if (currentEnergy < maxEnergy)
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRegenRate * Time.deltaTime);
        }

        // 2. Dem thoi gian duy tri Giap Ho Than
        if (isShieldActive)
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer <= 0f)
            {
                BreakShield(expired: true);
            }
        }
    }

    /// <summary>
    /// Kiem tra va tieu hao Nang Luong khi thi trien chieu thuc.
    /// </summary>
    public bool TryConsumeEnergy(float amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            return true;
        }

        // Thong bao tren man hinh khi khong du nang luong
        DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, "KHONG DU NANG LUONG!", new Color(0.3f, 0.85f, 1f));
        SoundManager.EnsureInstance()?.PlayMenuHover();
        return false;
    }

    /// <summary>
    /// Hoi lai mot luong nang luong truc tiep (khi chem trung dich).
    /// </summary>
    public void RestoreEnergy(float amount)
    {
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
    }

    /// <summary>
    /// Tich tu Noi Nang trong giao tranh. Khi day 100%, tu dong bung Giap Ho Than.
    /// </summary>
    public void AddInnerEnergy(float amount)
    {
        if (isShieldActive) return; // Khi dang co giap thi khong can tich them

        currentInnerEnergy = Mathf.Clamp(currentInnerEnergy + amount, 0f, maxInnerEnergy);

        // Kiem tra neu day noi nang -> Kich hoat giap ao
        if (currentInnerEnergy >= maxInnerEnergy)
        {
            ActivateShield();
        }
    }

    /// <summary>
    /// Kich hoat Lop Giap Ao Ho Than bao ve nguoi choi.
    /// </summary>
    public void ActivateShield()
    {
        isShieldActive = true;
        currentShieldHealth = maxShieldHealth;
        shieldTimer = shieldDuration;

        if (shieldVisual != null)
        {
            shieldVisual.ShowShield(true);
        }

        // Hieu ung thong bao & am thanh hoanh trang
        DamagePopup.CreateText(transform.position + Vector3.up * 1.8f, "GIAP HO THAN KICH HOAT!", new Color(1f, 0.85f, 0.25f));
        SoundManager.EnsureInstance()?.PlayHitImpact();

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.15f, 0.2f);

        Debug.Log("<color=yellow>[INNER ENERGY]</color> Noi Nang da day! Kich hoat Lop Giap Ao Ho Than 80 HP!");
    }

    /// <summary>
    /// Hap thu sat thuong khi nguoi choi bi quai danh trung.
    /// Tra ve luong sat thuong du thua (neu co) xuyen qua vao mau goc.
    /// </summary>
    public int AbsorbDamage(int incomingDamage)
    {
        if (!isShieldActive || incomingDamage <= 0) return incomingDamage;

        // Am thanh & hieu ung giap chan don
        if (shieldVisual != null)
        {
            shieldVisual.OnHit();
        }

        if (currentShieldHealth >= incomingDamage)
        {
            currentShieldHealth -= incomingDamage;
            DamagePopup.CreateText(transform.position + Vector3.up * 1.3f, $"[GIAP HO THAN] -{incomingDamage}", new Color(1f, 0.88f, 0.35f));

            if (currentShieldHealth <= 0)
            {
                BreakShield(expired: false);
            }
            return 0; // Hap thu hoan toan sat thuong!
        }
        else
        {
            int remainingDamage = incomingDamage - currentShieldHealth;
            DamagePopup.CreateText(transform.position + Vector3.up * 1.3f, $"[GIAP VO] -{currentShieldHealth}", new Color(1f, 0.45f, 0.35f));
            currentShieldHealth = 0;
            BreakShield(expired: false);
            return remainingDamage; // Phan sat thuong con lai tru vao mau
        }
    }

    /// <summary>
    /// Huy lop giap khi bi danh vo hoac het thoi gian ton tai.
    /// </summary>
    public void BreakShield(bool expired = false)
    {
        if (!isShieldActive) return;

        isShieldActive = false;
        currentShieldHealth = 0;
        currentInnerEnergy = 0f; // Reset ve 0 de tiep tuc tich luy cho lan sau

        if (shieldVisual != null)
        {
            shieldVisual.ShowShield(false);
            shieldVisual.PlayBreakVFX();
        }

        if (expired)
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.4f, "GIAP HO THAN HET GIO", new Color(0.7f, 0.75f, 0.85f));
        }
        else
        {
            DamagePopup.CreateText(transform.position + Vector3.up * 1.4f, "GIAP HO THAN VO TAN!", new Color(1f, 0.35f, 0.4f));
            SoundManager.EnsureInstance()?.PlayHitImpact();
        }

        Debug.Log("<color=orange>[INNER ENERGY]</color> Lop Giap Ao Ho Than da ket thuc. Reset Noi Nang ve 0.");
    }

    // Getters cho UI
    public float GetEnergy() => currentEnergy;
    public float GetMaxEnergy() => maxEnergy;
    public float GetEnergyNormalized() => maxEnergy > 0 ? Mathf.Clamp01(currentEnergy / maxEnergy) : 0f;

    public float GetInnerEnergy() => currentInnerEnergy;
    public float GetMaxInnerEnergy() => maxInnerEnergy;
    public float GetInnerEnergyNormalized() => maxInnerEnergy > 0 ? Mathf.Clamp01(currentInnerEnergy / maxInnerEnergy) : 0f;

    public bool IsShieldActive() => isShieldActive;
    public int GetShieldHealth() => currentShieldHealth;
    public int GetMaxShieldHealth() => maxShieldHealth;
    public float GetShieldHealthNormalized() => maxShieldHealth > 0 ? Mathf.Clamp01((float)currentShieldHealth / maxShieldHealth) : 0f;
}
