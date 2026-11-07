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
    [SerializeField] private float baseMaxEnergy = 100f;
    [SerializeField] private float bonusMaxEnergy = 0f;
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float currentEnergy = 100f;
    [SerializeField] private float baseEnergyRegenRate = 3.5f; // Hồi 3.5 NL/s cơ bản
    [SerializeField] private float bonusEnergyRegenRate = 0f;

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

    public void SetBonusEnergyRegen(float bonus)
    {
        bonusEnergyRegenRate = bonus;
    }

    public float GetEnergyRegenRate() => baseEnergyRegenRate + bonusEnergyRegenRate;

    public void RefreshSocketAndEquipmentEnergyBonus()
    {
        // 1. Chỉ số từ ngọc khảm (Gem Sockets)
        EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out _, out _, out _, out _, out _, out _, out float sockMaxEnergy, out float sockEnergyRegen);

        // 2. Chỉ số từ trang bị (Equipped Armor)
        ArmorShopUI.GetEquippedArmorBonus(out _, out _, out _, out float armorMaxEnergy, out float armorEnergyRegen);

        // 3. Chỉ số từ vũ khí (Equipped Weapon)
        float weaponMaxEnergy = 0f;
        float weaponEnergyRegen = 0f;
        string equippedWpnId = PlayerPrefs.GetString("Equipped_Weapon_Id", "");
        if (!string.IsNullOrEmpty(equippedWpnId))
        {
            var def = InventoryItemDatabase.GetItem(equippedWpnId);
            if (def != null)
            {
                weaponMaxEnergy = def.manaBonus;
                weaponEnergyRegen = def.manaRegenBonus;
            }
        }

        bonusMaxEnergy = sockMaxEnergy + armorMaxEnergy + weaponMaxEnergy;
        bonusEnergyRegenRate = sockEnergyRegen + armorEnergyRegen + weaponEnergyRegen;

        float oldMax = maxEnergy;
        maxEnergy = baseMaxEnergy + bonusMaxEnergy;

        if (maxEnergy > oldMax && oldMax > 0)
        {
            currentEnergy += (maxEnergy - oldMax);
        }
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
    }

    void Awake()
    {
        Instance = this;
        baseMaxEnergy = 100f;
        RefreshSocketAndEquipmentEnergyBonus();
        currentEnergy = maxEnergy;
        currentInnerEnergy = 0f;
    }

    void Start()
    {
        RefreshSocketAndEquipmentEnergyBonus();
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
        // 1. Tu dong hoi phuc Nang Luong theo thoi gian (hoi cham de kiem soat spam chieu)
        if (currentEnergy < maxEnergy)
        {
            float rate = GetEnergyRegenRate();
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + rate * Time.deltaTime);
        }

        // 2. Giáp Hộ Thân Nội Năng màu vàng duy trì liên tục cho đến khi bị quái đánh vỡ (chuẩn phong cách ARPG / MOBA)
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
    /// Rút bớt năng lượng (dùng khi dính hiệu ứng Khóa Hồn hoặc chiêu thức đối thủ)
    /// </summary>
    public void ConsumeEnergy(float amount)
    {
        currentEnergy = Mathf.Max(0f, currentEnergy - amount);
    }

    /// <summary>
    /// <summary>
    /// Tích tụ Nội Năng trong giao tranh.
    /// - Với Player 6: Đầy 100% kích hoạt trạng thái Bùng Nổ Thiên Quang (Hồi chiêu cuối 1s liên tục, không có giáp vàng).
    /// - Với các nhân vật khác: Đầy 100% tự động bung Giáp Hộ Thân màu vàng.
    /// </summary>
    public bool IsPlayer1()
    {
        string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        bool hasOtherHeroSkill = (GetComponent<GaleRoninSkill>() != null || GetComponent<AzureBladeSkill>() != null ||
                                  GetComponent<TitanBerserkerSkill>() != null || GetComponent<DuneAssassinSkill>() != null ||
                                  GetComponent<CelestialMageSkill>() != null);
        return (curHero == CharacterManager.CHAR_VAELSTRIDER || string.IsNullOrEmpty(curHero)) && !hasOtherHeroSkill;
    }

    public void AddInnerEnergy(float amount)
    {
        // Riêng Player 1 (Vaelstrider): Đã có Giáp Ảo 3-hit màu trắng và Máu Ảo từ Lõi, BỎ HOÀN TOÀN giáp nội năng vàng
        if (IsPlayer1()) return;

        // Riêng Player 6 (Celestial Mage): Không bật khiên vàng, dùng để kích hoạt trạng thái Hồi Siêu Tốc Chiêu Cuối (Overdrive)
        var mage = GetComponent<CelestialMageSkill>() 
            ?? GetComponentInChildren<CelestialMageSkill>() 
            ?? GetComponentInParent<CelestialMageSkill>() 
            ?? FindAnyObjectByType<CelestialMageSkill>();

        if (mage != null && mage.enabled)
        {
            if (mage.IsOverdriveActive) return; // Đang trong lúc Overdrive thì không cộng thêm
            currentInnerEnergy = Mathf.Clamp(currentInnerEnergy + amount, 0f, maxInnerEnergy);
            Debug.Log($"<color=magenta>[INNER ENERGY (PLAYER 6)]</color> +{amount} -> {currentInnerEnergy}/{maxInnerEnergy}");
            if (currentInnerEnergy >= maxInnerEnergy)
            {
                mage.ActivateCelestialOverdrive();
            }
            return;
        }

        if (isShieldActive) return; // Khi dang co giap thi khong can tich them

        currentInnerEnergy = Mathf.Clamp(currentInnerEnergy + amount, 0f, maxInnerEnergy);
        Debug.Log($"<color=yellow>[INNER ENERGY]</color> +{amount} -> {currentInnerEnergy}/{maxInnerEnergy}");

        // Kiem tra neu day noi nang -> Kich hoat giap ao
        if (currentInnerEnergy >= maxInnerEnergy)
        {
            ActivateShield();
        }
    }

    /// <summary>
    /// Đặt trực tiếp giá trị Nội Năng (dùng khi rút dần thanh nội năng trong chế độ Overdrive)
    /// </summary>
    public void SetInnerEnergy(float val)
    {
        currentInnerEnergy = Mathf.Clamp(val, 0f, maxInnerEnergy);
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

        SoundManager.EnsureInstance()?.PlayHitImpact();

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.15f, 0.2f);

        Debug.Log("<color=yellow>[INNER ENERGY]</color> Noi Nang da day! Kich hoat Lop Giap Ao Ho Than 80 HP!");
    }

    /// <summary>
    /// Hấp thụ sát thương khi người chơi bị quái đánh trúng.
    /// Ưu tiên trừ vào Thanh Giáp Nội Năng màu vàng trước tiên theo yêu cầu.
    /// Trả về lượng sát thương dư thừa (nếu có) xuyên qua vào giáp ảo/máu.
    /// </summary>
    public int AbsorbDamage(int incomingDamage)
    {
        // Riêng Player 1 và Player 6 không dùng cơ chế giáp vàng
        if (IsPlayer1()) return incomingDamage;
        var mage = GetComponent<CelestialMageSkill>();
        if (mage != null && mage.enabled) return incomingDamage;

        if (incomingDamage <= 0) return incomingDamage;

        // Âm thanh & hiệu ứng giáp chắn đòn
        if (shieldVisual != null)
        {
            shieldVisual.OnHit();
        }

        // 1. Nếu đang có Khiên Hộ Thân Siêu Cấp (đầy nội năng 100%)
        if (isShieldActive && currentShieldHealth > 0)
        {
            if (currentShieldHealth >= incomingDamage)
            {
                currentShieldHealth -= incomingDamage;
                currentInnerEnergy = Mathf.Clamp(currentShieldHealth, 0f, maxInnerEnergy);
                if (currentShieldHealth <= 0)
                {
                    BreakShield(expired: false);
                }
                return 0; // Hấp thụ hoàn toàn sát thương!
            }
            else
            {
                int remainingDamage = incomingDamage - currentShieldHealth;
                currentShieldHealth = 0;
                BreakShield(expired: false);
                return remainingDamage; // Phần sát thương còn lại trừ tiếp vào các lớp sau
            }
        }

        // 2. Nếu thanh nội năng màu vàng đang có năng lượng (> 0): Hoạt động như lớp giáp năng lượng bảo hộ
        if (currentInnerEnergy > 0f)
        {
            int energyAvailable = Mathf.RoundToInt(currentInnerEnergy);
            if (energyAvailable >= incomingDamage)
            {
                currentInnerEnergy = Mathf.Max(0f, currentInnerEnergy - incomingDamage);
                return 0; // Hấp thụ trọn vẹn vào nội năng!
            }
            else
            {
                int remDmg = incomingDamage - energyAvailable;
                currentInnerEnergy = 0f;
                return remDmg;
            }
        }

        return incomingDamage;
    }

    /// <summary>
    /// Huy lop giap khi bi danh vo hoac het thoi gian ton tai.
    /// </summary>
    public void BreakShield(bool expired = false)
    {
        if (!isShieldActive) return;

        isShieldActive = false;
        currentShieldHealth = 0;
        currentInnerEnergy = 0f;

        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null && ronin.IsChargingIaido)
        {
            ronin.CancelIaidoCharge();
        } // Reset ve 0 de tiep tuc tich luy cho lan sau

        if (shieldVisual != null)
        {
            shieldVisual.ShowShield(false);
            shieldVisual.PlayBreakVFX();
        }

        if (!expired)
        {
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

    public bool IsShieldActive() => !IsPlayer1() && (isShieldActive || currentInnerEnergy > 0f);
    public int GetShieldHealth() => currentShieldHealth;
    public int GetMaxShieldHealth() => maxShieldHealth;
    public float GetShieldHealthNormalized() => maxShieldHealth > 0 ? Mathf.Clamp01((float)currentShieldHealth / maxShieldHealth) : 0f;
}
