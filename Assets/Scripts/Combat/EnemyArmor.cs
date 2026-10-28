// ========================================================
// VAELSTRIDER: RUINBOUND - Enemy & Boss Armor Defense System
// Giáp Vật Lý (Physical Armor) & Giáp Phép / Kháng Phép (Magic Resistance)
// Cho phép Player tận dụng chỉ số Xuyên Giáp & Xuyên Kháng Phép
// ========================================================
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyArmor : MonoBehaviour
{
    [Header("Defense Attributes (Chỉ Số Phòng Ngự)")]
    [Tooltip("Giáp Vật Lý cơ bản: Giảm sát thương Vật Lý (Physical Damage)")]
    [SerializeField] private int basePhysicalArmor = 30;

    [Tooltip("Giáp Phép / Kháng Phép cơ bản: Giảm sát thương Phép (Magic Damage)")]
    [SerializeField] private int baseMagicArmor = 20;

    // Các biến điều chỉnh tạm thời (Buff / Debuff / Phá Giáp)
    private int flatArmorReduction = 0;
    private int flatMagicArmorReduction = 0;
    private float percentArmorReduction = 0f;
    private float percentMagicArmorReduction = 0f;

    public int BasePhysicalArmor => basePhysicalArmor;
    public int BaseMagicArmor => baseMagicArmor;

    public int CurrentPhysicalArmor
    {
        get
        {
            float shredded = basePhysicalArmor * (1f - Mathf.Clamp01(percentArmorReduction));
            return Mathf.Max(0, Mathf.RoundToInt(shredded - flatArmorReduction));
        }
    }

    public int CurrentMagicArmor
    {
        get
        {
            float shredded = baseMagicArmor * (1f - Mathf.Clamp01(percentMagicArmorReduction));
            return Mathf.Max(0, Mathf.RoundToInt(shredded - flatMagicArmorReduction));
        }
    }

    void Awake()
    {
        AutoConfigureStats(gameObject);
    }

    /// <summary>
    /// Tính toán sát thương thực tế sau khi giảm trừ qua Giáp / Kháng Phép và Xuyên Giáp / Xuyên Kháng Phép của người tấn công.
    /// Sát thương chuẩn (True Damage) bỏ qua 100% Giáp và Kháng Phép.
    /// </summary>
    public int MitigateDamage(int incomingRawDamage, DamageType damageType, GameObject attacker = null)
    {
        if (incomingRawDamage <= 0) return 0;

        // 1. Sát thương Chuẩn (True Damage) bỏ qua hoàn toàn Giáp & Kháng Phép
        if (damageType == DamageType.TrueDamage)
        {
            return incomingRawDamage;
        }

        // 2. Lấy chỉ số Xuyên Giáp / Xuyên Kháng Phép từ người tấn công (PlayerCombat)
        int flatPen = 0;
        float pctPen = 0f;

        if (attacker != null)
        {
            var combat = attacker.GetComponent<PlayerCombat>() ?? attacker.GetComponentInParent<PlayerCombat>();
            if (combat != null)
            {
                if (damageType == DamageType.Physical)
                {
                    flatPen = combat.TotalPhysicalArmorPenFlat;
                    pctPen = combat.TotalPhysicalArmorPenPercent;
                }
                else if (damageType == DamageType.Magic)
                {
                    flatPen = combat.TotalMagicPenFlat;
                    pctPen = combat.TotalMagicPenPercent;
                }
            }
        }

        float rawArmor = (damageType == DamageType.Physical) ? CurrentPhysicalArmor : CurrentMagicArmor;

        // Giáp hiệu dụng sau khi trừ Xuyên Giáp % và Xuyên Giáp Cố Định của Player
        // Công thức chuẩn: EffectiveArmor = (Armor * (1 - Penetration%)) - FlatPenetration
        float effectiveArmor = Mathf.Max(0f, (rawArmor * (1f - Mathf.Clamp01(pctPen))) - flatPen);

        // Công thức giảm thương RPG chuẩn: DamageMultiplier = 100 / (100 + EffectiveArmor)
        // 50 Giáp  -> Giảm 33.3% sát thương
        // 100 Giáp -> Giảm 50% sát thương
        // 150 Giáp -> Giảm 60% sát thương
        // 200 Giáp -> Giảm 66.7% sát thương
        float damageMultiplier = 100f / (100f + effectiveArmor);

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(incomingRawDamage * damageMultiplier));
        return finalDamage;
    }

    /// <summary>
    /// Đảm bảo một GameObject Enemy có component EnemyArmor. Nếu chưa có, tự động gắn và cấu hình chỉ số theo loại quái/Boss.
    /// </summary>
    public static EnemyArmor EnsureArmor(GameObject target)
    {
        if (target == null) return null;
        var armor = target.GetComponent<EnemyArmor>() ?? target.GetComponentInParent<EnemyArmor>();
        if (armor == null)
        {
            armor = target.AddComponent<EnemyArmor>();
            armor.AutoConfigureStats(target);
        }
        return armor;
    }

    /// <summary>
    /// Thiết lập thủ công chỉ số Giáp (dùng khi setup Prefab / Boss Phase)
    /// </summary>
    public void SetArmorValues(int physArmor, int magArmor)
    {
        basePhysicalArmor = Mathf.Max(0, physArmor);
        baseMagicArmor = Mathf.Max(0, magArmor);
    }

    /// <summary>
    /// Áp dụng hiệu ứng Phá Giáp tạm thời (Armor Shredding)
    /// </summary>
    public void ApplyShred(int flatReduction, float pctReduction, bool isMagic = false)
    {
        if (isMagic)
        {
            flatMagicArmorReduction = Mathf.Max(flatMagicArmorReduction, flatReduction);
            percentMagicArmorReduction = Mathf.Clamp01(percentMagicArmorReduction + pctReduction);
        }
        else
        {
            flatArmorReduction = Mathf.Max(flatArmorReduction, flatReduction);
            percentArmorReduction = Mathf.Clamp01(percentArmorReduction + pctReduction);
        }
    }

    public void ClearShred()
    {
        flatArmorReduction = 0;
        flatMagicArmorReduction = 0;
        percentArmorReduction = 0f;
        percentMagicArmorReduction = 0f;
    }

    public void AutoConfigureStats(GameObject target)
    {
        // 1. Boss Khổng Lồ Map 2: Valley Golem Boss (Đá thạch cổ xưa - Giáp vật lý cực dày)
        if (target.GetComponent<ValleyGolemBossAI>() != null)
        {
            basePhysicalArmor = 180;
            baseMagicArmor = 75;
        }
        // 2. Boss Tối Cao Map 1: Skeleton King Boss (Chúa Tể Xương - Cân bằng song giáp)
        else if (target.GetComponent<SkeletonKingBossAI>() != null)
        {
            basePhysicalArmor = 150;
            baseMagicArmor = 125;
        }
        // 3. Quái Vật Máu / Boss Huyết Tộc Map 3: Blood Monster
        else if (target.GetComponent<BloodMonsterAI>() != null)
        {
            var blood = target.GetComponent<BloodMonsterAI>();
            bool isBoss = blood != null && blood.IsBoss;
            basePhysicalArmor = isBoss ? 130 : 60;
            baseMagicArmor = isBoss ? 95 : 40;
        }
        // 4. Cỗ Máy Tử Thần / Boss BotWheel Map 2
        else if (target.GetComponent<BotWheelAI>() != null)
        {
            var bot = target.GetComponent<BotWheelAI>();
            bool isBoss = bot != null && bot.IsBoss;
            basePhysicalArmor = isBoss ? 140 : 55;
            baseMagicArmor = isBoss ? 105 : 45;
        }
        // 5. Lính Xương (Skeleton AI): Khung xương giáp cứng
        else if (target.GetComponent<SkeletonAI>() != null)
        {
            basePhysicalArmor = 45;
            baseMagicArmor = 20;
        }
        // 6. Quái Thú Thung Lũng (Valley Beast AI): Da dày thịt béo
        else if (target.GetComponent<ValleyBeastAI>() != null)
        {
            basePhysicalArmor = 60;
            baseMagicArmor = 30;
        }
        // 7. Bọ Khạc Độc (Valley Spitter AI): Giáp mềm, kháng phép khá
        else if (target.GetComponent<ValleySpitterAI>() != null)
        {
            basePhysicalArmor = 25;
            baseMagicArmor = 40;
        }
        // 8. Quái Nhầy Slime (Slime AI): Cơ thể lỏng, giáp thấp
        else if (target.GetComponent<SlimeAI>() != null)
        {
            basePhysicalArmor = 18;
            baseMagicArmor = 12;
        }
        // 9. Yêu Tinh Goblin (Goblin AI): Giáp nhẹ
        else if (target.GetComponent<GoblinAI>() != null)
        {
            basePhysicalArmor = 30;
            baseMagicArmor = 20;
        }
        // 10. Nhện Con (Mini Spider Minion): Mau lẹ nhưng mỏng manh
        else if (target.GetComponent<MiniSpiderMinion>() != null)
        {
            basePhysicalArmor = 15;
            baseMagicArmor = 12;
        }
        // 11. Bù Nhìn Tập Đánh (Dummy Enemy)
        else if (target.GetComponent<DummyEnemy>() != null)
        {
            basePhysicalArmor = 50;
            baseMagicArmor = 50;
        }
        // 12. Quái thường mặc định
        else
        {
            basePhysicalArmor = 30;
            baseMagicArmor = 20;
        }
    }
}
