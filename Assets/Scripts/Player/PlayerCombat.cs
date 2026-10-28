using UnityEngine.EventSystems;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    public Transform AttackPoint => attackPoint;
    [SerializeField] private float attackRadius = 1.35f;
    [SerializeField] private float attackOffsetDistance = 1.3f;
    [SerializeField] private Vector2 bodyCenterOffset = new Vector2(0f, 0.1f);
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private int attack1Damage = 5;
    [SerializeField] private int attack2Damage = 5;
    [SerializeField] private float comboResetTime = 0.9f;

    [Header("Special Skill - Ruin Whirlwind")]
    [SerializeField] private float skillCooldown = 30.0f;
    [SerializeField] private float skillRadius = 2.8f;
    [SerializeField] private float skillKnockbackForce = 11.5f;

    private Animator animator;
    private PlayerController playerController;
    private PlayerHealth playerHealth;

    public static event System.Action<GameObject, int> OnPlayerHitEnemy;

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    private float skillCooldownTimer = 0f;
    private int lastAttackFrame = -1;
    private int lastSpecialSkillFrame = -1;
    private bool isUltiActive = false;
    private int bonusAttackDamage = 0;
    private float ultiCooldownReduction = 0f;
    private int bonusUltiDamage = 0;
    private float lifestealPercent = 0f;
    private float physLifestealPercent = 0f;
    private float spellVampPercent = 0f;

    public float PhysLifestealPercent => (physLifestealPercent > 0f ? physLifestealPercent : lifestealPercent) + augBonusLifesteal;
    public float SpellVampPercent => spellVampPercent + augBonusSpellVamp;
    public float LifestealPercent => lifestealPercent + augBonusLifesteal;

    private int upgradeBonusAtk = 0;
    private int weaponBonusAtk = 0;
    private int socketBonusAtk = 0;

    private int bonusPhysAtk = 0;
    private int bonusMagicAtk = 0;
    private int bonusTrueAtk = 0;

    private int weaponPhysAtk = 0;
    private int weaponMagicAtk = 0;
    private int weaponTrueAtk = 0;

    private int socketPhysAtk = 0;
    private int socketMagicAtk = 0;
    private int socketTrueAtk = 0;

    private int weaponArmorPenFlat = 0;
    private float weaponArmorPenPct = 0f;
    private int weaponMagicPenFlat = 0;
    private float weaponMagicPenPct = 0f;

    private int socketArmorPenFlat = 0;
    private float socketArmorPenPct = 0f;
    private int socketMagicPenFlat = 0;
    private float socketMagicPenPct = 0f;

    private int augBonusArmorPenFlat = 0;
    private float augBonusArmorPenPct = 0f;
    private int augBonusMagicPenFlat = 0;
    private float augBonusMagicPenPct = 0f;

    public int TotalPhysicalArmorPenFlat
    {
        get
        {
            int innate = 0;
            string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
            if (curHero == CharacterManager.CHAR_TITAN_BERSERKER || GetComponent<TitanBerserkerSkill>() != null) innate += 20;
            else if (curHero == CharacterManager.CHAR_GALE_RONIN || GetComponent<GaleRoninSkill>() != null) innate += 15;
            return weaponArmorPenFlat + socketArmorPenFlat + augBonusArmorPenFlat + innate;
        }
    }

    public float TotalPhysicalArmorPenPercent
    {
        get
        {
            float innate = 0f;
            string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
            if (curHero == CharacterManager.CHAR_DUNE_ASSASSIN || GetComponent<DuneAssassinSkill>() != null) innate += 0.15f;
            return Mathf.Clamp01(weaponArmorPenPct + socketArmorPenPct + augBonusArmorPenPct + innate);
        }
    }

    public int TotalMagicPenFlat
    {
        get
        {
            int innate = 0;
            string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
            if (curHero == CharacterManager.CHAR_CELESTIAL_MAGE || GetComponent<CelestialMageSkill>() != null) innate += 15;
            else if (curHero == CharacterManager.CHAR_AZURE_BLADE || GetComponent<AzureBladeSkill>() != null) innate += 10;
            return weaponMagicPenFlat + socketMagicPenFlat + augBonusMagicPenFlat + innate;
        }
    }

    public float TotalMagicPenPercent
    {
        get
        {
            float innate = 0f;
            string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
            if (curHero == CharacterManager.CHAR_CELESTIAL_MAGE || GetComponent<CelestialMageSkill>() != null) innate += 0.10f;
            return Mathf.Clamp01(weaponMagicPenPct + socketMagicPenPct + augBonusMagicPenPct + innate);
        }
    }

    [Header("Critical Strike Settings")]
    [SerializeField] private float baseCritChance = 0.20f;   // 20% Tỉ lệ chí mạng cơ bản
    [SerializeField] private float critMultiplier = 1.75f;   // 175% Sát thương khi nổ chí mạng
    private float bonusCritChance = 0f;

    public float BaseCritChance => baseCritChance;
    public float BonusCritChance => bonusCritChance;
    public float TotalCritChance => Mathf.Clamp(baseCritChance + bonusCritChance + augBonusCrit, 0.05f, 0.95f);
    public float CritMultiplier => critMultiplier + augBonusCritDmg;

    public bool RollCritical(float extraChance, out float multiplier)
    {
        float totalChance = Mathf.Clamp(TotalCritChance + extraChance, 0.05f, 0.95f);
        bool isCrit = UnityEngine.Random.value < totalChance;
        multiplier = isCrit ? CritMultiplier : 1.0f;
        return isCrit;
    }

    public bool RollCritical(out float multiplier)
    {
        return RollCritical(0f, out multiplier);
    }

    public int BonusPhysicalDamage => bonusPhysAtk;
    public int BonusMagicDamage => bonusMagicAtk;
    public int BonusTrueDamage => bonusTrueAtk;

    public void ApplyUpgradeBonus(int bonusAtk, float ultiCdReduction, int bonusUltiDmg)
    {
        upgradeBonusAtk = bonusAtk;
        bonusUltiDamage = bonusUltiDmg;
        RecalculateCombatStats();
    }

    // ─── Augment Bonuses (from Level-Up system) ────────────────────────────────
    private int   augBonusAtk      = 0;
    private float augBonusCrit     = 0f;
    private float augBonusCritDmg  = 0f;
    private float augBonusLifesteal= 0f;
    private float augBonusSpellVamp= 0f;
    private float augBonusCDR      = 0f;
    private float comboResetMult   = 1f;

    public void ApplyAugmentBonus(int bonusAtk, float bonusCrit, float bonusCritDmg,
                                   float bonusLifesteal, float bonusSpellVamp, float bonusCDR,
                                   int bonusArmPen = 0, float bonusArmPenPct = 0f, int bonusMagPen = 0, float bonusMagPenPct = 0f)
    {
        augBonusAtk          = bonusAtk;
        augBonusCrit         = bonusCrit;
        augBonusCritDmg      = bonusCritDmg;
        augBonusLifesteal    = bonusLifesteal;
        augBonusSpellVamp    = bonusSpellVamp;
        augBonusCDR          = bonusCDR;
        augBonusArmorPenFlat = bonusArmPen;
        augBonusArmorPenPct  = bonusArmPenPct;
        augBonusMagicPenFlat = bonusMagPen;
        augBonusMagicPenPct  = bonusMagPenPct;
        RecalculateCombatStats();
    }

    public int GetBaseAttack() => attack1Damage;

    public void SetComboResetMultiplier(float mult)
    {
        comboResetMult = Mathf.Clamp(mult, 0.2f, 1f);
    }

    public void ReduceSkillCooldownImmediate(float fraction)
    {
        skillCooldownTimer = Mathf.Max(0f, skillCooldownTimer * (1f - fraction));
    }

    public float AugBonusCrit     => augBonusCrit;
    public float AugBonusCritDmg  => augBonusCritDmg;
    public float AugBonusLifesteal => augBonusLifesteal;
    public float AugBonusSpellVamp => augBonusSpellVamp;

    // Hashes
    private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
    private static readonly int ComboStepHash = Animator.StringToHash("ComboStep");

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();

        // Bat buoc ap dung chi so can ban Hardcore chuan (Vaelstrider = 18 + 28, Cooldown = 18s)
        attack1Damage = 18;
        attack2Damage = 28;
        skillCooldown = 18.0f;

        AutoAlignCombat();
        if (enemyLayers.value == 0)
        {
            enemyLayers = ~0; // Chem tat ca cac layer co IDamageable
        }

        if (attackPoint == null)
        {
            Transform existing = transform.Find("AttackPoint");
            if (existing != null)
            {
                attackPoint = existing;
            }
            else
            {
                GameObject point = new GameObject("AttackPoint");
                point.transform.parent = transform;
                attackPoint = point.transform;
            }
        }
        UpdateAttackPointPosition();

        RefreshEquipmentBonuses();
    }

    void Start()
    {
        RefreshEquipmentBonuses();
    }

    public void RefreshEquipmentBonuses()
    {
        var upgradeMgr = PlayerUpgradeManager.EnsureInstance();
        string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        if (upgradeMgr != null)
        {
            upgradeBonusAtk = upgradeMgr.GetAttackBonus(curHero);
            bonusUltiDamage = upgradeMgr.GetUltiDamageBonus(curHero);
        }

        string equippedWpnId = PlayerPrefs.GetString("Equipped_Weapon_Id", "");
        weaponBonusAtk = 0;
        weaponPhysAtk = 0;
        weaponMagicAtk = 0;
        weaponTrueAtk = 0;
        weaponArmorPenFlat = 0;
        weaponArmorPenPct = 0f;
        weaponMagicPenFlat = 0;
        weaponMagicPenPct = 0f;
        bonusCritChance = 0f;
        if (!string.IsNullOrEmpty(equippedWpnId))
        {
            var def = InventoryItemDatabase.GetItem(equippedWpnId);
            if (def != null)
            {
                weaponBonusAtk = def.atkBonus;
                weaponPhysAtk = (def.physAtkBonus > 0 || def.magicAtkBonus > 0 || def.trueAtkBonus > 0) ? def.physAtkBonus : def.atkBonus;
                weaponMagicAtk = def.magicAtkBonus;
                weaponTrueAtk = def.trueAtkBonus;
                weaponArmorPenFlat = def.armorPenBonus;
                weaponArmorPenPct = def.armorPenPctBonus;
                weaponMagicPenFlat = def.magicPenBonus;
                weaponMagicPenPct = def.magicPenPctBonus;

                if (equippedWpnId == "wpn_katana")
                {
                    bonusCritChance = 0.15f; // +15% Tỉ lệ bạo kích từ Hắc Dạ Katana
                }
            }
        }

        EquipmentSocketManager.GetActiveEquippedSocketBonus(
            out socketBonusAtk, out socketPhysAtk, out socketMagicAtk, out socketTrueAtk,
            out _, out _,
            out lifestealPercent, out _, out _, out _,
            out _, out _,
            out physLifestealPercent, out spellVampPercent, out float socketCrit,
            out socketArmorPenFlat, out socketMagicPenFlat, out socketArmorPenPct, out socketMagicPenPct);
        bonusCritChance += socketCrit;
        RecalculateCombatStats();
    }

    public void RecalculateCombatStats()
    {
        HeroCostumeManager.GetEquippedCostumeBonus(CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "", out _, out int costumeAtk, out _, out _);
        string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        bool isAzure = (curHero == CharacterManager.CHAR_AZURE_BLADE || GetComponent<AzureBladeSkill>() != null);

        int azureSpeedConvertedAP = 0;
        var azureBlade = GetComponent<AzureBladeSkill>();
        if (azureBlade != null)
        {
            azureBlade.RefreshCooldownStats();
            if (isAzure)
            {
                azureSpeedConvertedAP = azureBlade.ConvertedMagicAttack;
            }
        }

        bonusPhysAtk = upgradeBonusAtk + weaponPhysAtk + socketPhysAtk + costumeAtk + augBonusAtk;
        bonusMagicAtk = weaponMagicAtk + socketMagicAtk + azureSpeedConvertedAP;
        bonusTrueAtk = weaponTrueAtk + socketTrueAtk;
        bonusAttackDamage = bonusPhysAtk + bonusMagicAtk + bonusTrueAtk;

        EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out _, out _, out lifestealPercent, out float cdReduc, out _, out _);
        float upUltiCd = 0f;
        var upgradeMgr = PlayerUpgradeManager.EnsureInstance();
        if (upgradeMgr != null)
        {
            upUltiCd = upgradeMgr.GetUltiCooldownReduction(curHero);
        }
        float baseUlti = (curHero == CharacterManager.CHAR_GALE_RONIN || GetComponent<GaleRoninSkill>() != null) ? 15.0f :
                         (curHero == CharacterManager.CHAR_AZURE_BLADE || GetComponent<AzureBladeSkill>() != null) ? 18.0f : 24.0f;

        ultiCooldownReduction = upUltiCd + (baseUlti * (cdReduc + augBonusCDR));
        skillCooldown = Mathf.Max(3.0f, baseUlti - ultiCooldownReduction);

        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null)
        {
            ronin.RefreshCooldownStats();
        }

        var dune = GetComponent<DuneAssassinSkill>();
        if (dune != null)
        {
            dune.RefreshCooldownStats();
        }

        var mage = GetComponent<CelestialMageSkill>();
        if (mage != null)
        {
            mage.RefreshCooldownStats();
        }
    }

    public void PerformAttackInDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f && playerController != null)
        {
            playerController.SetFacingDirection(direction.normalized);
        }
        PerformAttack();
    }

    public int BonusAttackDamage => bonusAttackDamage;
    public int Attack1Damage => attack1Damage;
    public int Attack2Damage => attack2Damage;
    public int TotalAttackDamage => attack1Damage + bonusAttackDamage;
    public Vector2 BodyCenterOffset => bodyCenterOffset;
    public float SkillCooldown => skillCooldown;

    public void SetBaseAttackDamage(int hit1, int hit2)
    {
        attack1Damage = hit1;
        attack2Damage = hit2;
    }

    public bool IsAttackHolding => isAttackHolding;
    private bool isAttackHolding = false;
    private float attackHoldDuration = 0f;

    public void CancelAttackHold()
    {
        isAttackHolding = false;
        attackHoldDuration = 0f;
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            if (isAttackHolding)
            {
                isAttackHolding = false;
                attackHoldDuration = 0f;
            }
            if (isParrying)
            {
                CancelParryStance();
            }
            return;
        }

        if (skillCooldownTimer > 0f)
        {
            skillCooldownTimer -= Time.deltaTime;
        }

        if (parryCooldownTimer > 0f)
        {
            parryCooldownTimer -= Time.deltaTime;
        }

        if (Time.time - lastAttackTime > comboResetTime)
        {
            comboStep = 0;
            if (animator != null) animator.SetInteger(ComboStepHash, 0);
        }

        // Tuyet ky Ruin Whirlwind (Phim F tren PC)
        if (Input.GetKeyDown(KeyCode.F))
        {
            PerformSpecialSkill();
        }

        // Danh thuong & Tu luc ngam ban (Chuot trai hoac phim J)
        if (Time.frameCount != lastSpecialSkillFrame && !isUltiActive)
        {
            var titan = GetComponent<TitanBerserkerSkill>();
            var azureBlade = GetComponent<AzureBladeSkill>();
            var roninSkill = GetComponent<GaleRoninSkill>();
            var duneSkill = GetComponent<DuneAssassinSkill>();
            var mageSkill = GetComponent<CelestialMageSkill>();

            if (Input.GetKeyDown(KeyCode.J) || (Input.GetMouseButtonDown(0) && !IsPointerOverUI()))
            {
                bool canAttack = (titan == null || titan.IsAttackReady) &&
                                 (azureBlade == null || azureBlade.IsAttackReady) &&
                                 (roninSkill == null || roninSkill.IsAttackReady) &&
                                 (duneSkill == null || duneSkill.IsAttackReady) &&
                                 (mageSkill == null || mageSkill.IsAttackReady);

                if (canAttack)
                {
                    isAttackHolding = true;
                    attackHoldDuration = 0f;
                    Vector2 initialAim = GetAimDirectionFromInput();

                    if (azureBlade != null && azureBlade.enabled)
                    {
                        azureBlade.StartAim(initialAim);
                    }
                    else if (roninSkill != null && roninSkill.enabled)
                    {
                        if (roninSkill.CanUseIaidoCharge)
                        {
                            roninSkill.StartIaidoCharge(initialAim);
                        }
                    }
                    else if (mageSkill != null && mageSkill.enabled)
                    {
                        mageSkill.StartBarrierCharge(initialAim);
                    }
                }
            }

            if (isAttackHolding)
            {
                attackHoldDuration += Time.deltaTime;
                Vector2 aimDir = GetAimDirectionFromInput();

                if (mageSkill != null && mageSkill.enabled)
                {
                    mageSkill.UpdateBarrierCharge(aimDir, attackHoldDuration);
                }
                else if (duneSkill != null && duneSkill.enabled)
                {
                    if (attackHoldDuration >= 0.15f)
                    {
                        if (!duneSkill.IsAimingTornado && duneSkill.IsAttackReady)
                        {
                            duneSkill.StartTornadoAim(aimDir);
                        }
                        duneSkill.UpdateTornadoAim(aimDir, attackHoldDuration);
                    }
                }
                else if (titan != null && titan.enabled)
                {
                    if (titan.CanUseEarthshakerCharge && attackHoldDuration >= 0.15f)
                    {
                        if (!titan.IsChargingEarthshaker && titan.IsAttackReady)
                        {
                            titan.StartEarthshakerCharge(aimDir);
                        }
                        titan.UpdateEarthshakerCharge(aimDir, attackHoldDuration);
                    }
                    else if (!titan.CanUseEarthshakerCharge && attackHoldDuration >= 0.15f)
                    {
                        if (playerController != null) playerController.SetFacingDirection(aimDir);
                        Vector2 playerPos = (Vector2)transform.position;
                        SkillAimIndicator.EnsureExists(gameObject)?.ShowAimArc(playerPos, aimDir, attackRadius, 120f, new Color(1f, 0.85f, 0.2f, 0.85f));
                    }
                }
                else if (azureBlade != null && azureBlade.enabled && attackHoldDuration >= 0.10f)
                {
                    azureBlade.UpdateAim(aimDir, attackHoldDuration);
                }
                else if (roninSkill != null && roninSkill.enabled)
                {
                    if (roninSkill.CanUseIaidoCharge)
                    {
                        if (!roninSkill.IsChargingIaido && roninSkill.IsAttackReady)
                        {
                            roninSkill.StartIaidoCharge(aimDir);
                        }
                        roninSkill.UpdateIaidoCharge(aimDir, attackHoldDuration);
                    }
                    else if (attackHoldDuration >= 0.15f)
                    {
                        if (playerController != null) playerController.SetFacingDirection(aimDir);
                        Vector2 playerPos = (Vector2)transform.position;
                        SkillAimIndicator.EnsureExists(gameObject)?.ShowAimArc(playerPos, aimDir, roninSkill.SlashRadius, 180f, new Color(0.15f, 0.95f, 0.85f, 0.85f));
                    }
                }
                else if (titan == null && azureBlade == null && roninSkill == null && duneSkill == null && mageSkill == null)
                {
                    // Chỉ riêng Player 1 (Vaelstrider): Thế thủ kiếm thuẫn & Phóng đại sóng kiếm hoàng kim
                    if (CanUseParryCharge && attackHoldDuration >= 0.15f)
                    {
                        if (!isParrying)
                        {
                            StartParryStance(aimDir);
                        }
                        UpdateParryStance(aimDir, attackHoldDuration);
                    }
                    else if (!CanUseParryCharge && attackHoldDuration >= 0.15f)
                    {
                        if (playerController != null) playerController.SetFacingDirection(aimDir);
                        Vector2 playerPos = (Vector2)transform.position;
                        SkillAimIndicator.EnsureExists(gameObject)?.ShowAimArc(playerPos, aimDir, attackRadius, 120f, new Color(1f, 0.85f, 0.2f, 0.85f));
                    }
                }
            }

            if (isAttackHolding && (Input.GetKeyUp(KeyCode.J) || Input.GetMouseButtonUp(0)))
            {
                if (SkillAimIndicator.Instance != null) SkillAimIndicator.Instance.Hide();
                Vector2 aimDir = GetAimDirectionFromInput();
                float cachedHoldDur = attackHoldDuration;

                // Xoá trạng thái gồng TRƯỚC để IsMovementBlocked() không chặn di chuyển trong hàm Release
                isAttackHolding = false;
                attackHoldDuration = 0f;

                if (mageSkill != null && mageSkill.enabled)
                {
                    if (mageSkill.IsChargingBarrier && cachedHoldDur >= 0.18f)
                    {
                        mageSkill.ReleaseBarrierCharge(aimDir, bonusAttackDamage);
                    }
                    else
                    {
                        if (mageSkill.IsChargingBarrier) mageSkill.ReleaseBarrierCharge(aimDir, 0);
                        if (aimDir.sqrMagnitude > 0.01f && playerController != null)
                        {
                            playerController.SetFacingDirection(aimDir);
                        }
                        PerformAttack();
                    }
                }
                else if (duneSkill != null && duneSkill.enabled)
                {
                    if (duneSkill.IsAimingTornado || duneSkill.IsDefending)
                    {
                        duneSkill.ReleaseTornadoAim(aimDir, bonusAttackDamage);
                    }
                    else
                    {
                        if (aimDir.sqrMagnitude > 0.01f && playerController != null)
                        {
                            playerController.SetFacingDirection(aimDir);
                        }
                        PerformAttack();
                    }
                }
                else if (titan != null && titan.enabled)
                {
                    if (titan.CanUseEarthshakerCharge && titan.IsChargingEarthshaker && cachedHoldDur >= 0.22f)
                    {
                        titan.ReleaseEarthshakerCharge(aimDir, bonusAttackDamage);
                    }
                    else
                    {
                        if (titan.IsChargingEarthshaker)
                        {
                            titan.CancelEarthshakerCharge();
                        }
                        if (aimDir.sqrMagnitude > 0.01f && playerController != null)
                        {
                            playerController.SetFacingDirection(aimDir);
                        }
                        PerformAttack();
                    }
                }
                else if (azureBlade != null && azureBlade.enabled && cachedHoldDur >= 0.10f)
                {
                    azureBlade.ReleaseAimAndFire(bonusAttackDamage);
                }
                else if (roninSkill != null && roninSkill.enabled)
                {
                    if (roninSkill.CanUseIaidoCharge && roninSkill.IsChargingIaido)
                    {
                        roninSkill.ReleaseIaidoCharge(aimDir, bonusAttackDamage);
                    }
                    else
                    {
                        if (roninSkill.IsChargingIaido)
                        {
                            roninSkill.CancelIaidoCharge();
                        }
                        if (aimDir.sqrMagnitude > 0.01f && playerController != null)
                        {
                            playerController.SetFacingDirection(aimDir);
                        }
                        PerformAttack();
                    }
                }
                else if (titan == null && azureBlade == null && roninSkill == null && duneSkill == null && mageSkill == null)
                {
                    // Chỉ riêng Player 1 (Vaelstrider)
                    if (CanUseParryCharge && isParrying && (cachedHoldDur >= 0.22f || wasParryTriggered))
                    {
                        ReleaseParryStance(aimDir, bonusAttackDamage);
                    }
                    else
                    {
                        if (isParrying)
                        {
                            CancelParryStance();
                        }
                        if (aimDir.sqrMagnitude > 0.01f && playerController != null)
                        {
                            playerController.SetFacingDirection(aimDir);
                        }
                        PerformAttack();
                    }
                }

                if (playerController != null) playerController.RefreshMoveInput();
            }
        }

        UpdateAttackPointPosition();
    }

    public Vector2 GetAimDirectionFromInput()
    {
        if (Camera.main != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 diff = (Vector2)mouseWorld - (Vector2)transform.position;
            if (diff.sqrMagnitude > 0.04f) return diff.normalized;
        }
        if (playerController != null)
        {
            Vector2 f = playerController.GetFacingDirection();
            if (f.sqrMagnitude > 0.01f) return f.normalized;
        }
        return Vector2.down;
    }


    public void PerformAttack()
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (isUltiActive) return;
        if (playerController != null && (playerController.IsDashing() || playerController.IsStunned)) return;

        if (Time.frameCount == lastAttackFrame) return;
        lastAttackFrame = Time.frameCount;
        CancelAttackHold();

        string selectedHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";

        // Kich hoat Phong Loi Lang Khach cho Nhan vat 2 (Gale Ronin)
        if (selectedHero == CharacterManager.CHAR_GALE_RONIN || GetComponent<GaleRoninSkill>() != null)
        {
            var ronin = GetComponent<GaleRoninSkill>();
            if (ronin == null) ronin = gameObject.AddComponent<GaleRoninSkill>();
            ronin.enabled = true;
            if (!ronin.IsAttackReady) return; // Toc do danh cham co dinh, khong bi spam nhanh

            lastAttackTime = Time.time;
            comboStep++;
            if (comboStep > 2) comboStep = 1;

            if (animator != null)
            {
                animator.SetInteger(ComboStepHash, comboStep);
                animator.SetTrigger(AttackTriggerHash);

                string attackState = (comboStep == 1) ? "Attack1" : "Attack2";
                animator.Play(attackState, 0, 0f);
            }

            Vector2 facing = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
            int curAtk = ((comboStep == 1) ? attack1Damage : attack2Damage) + bonusAttackDamage;
            ronin.PerformAttack(facing, curAtk);
            return;
        }

        // Kich hoat Dai Chuy cho Nhan vat 4 (Titan Berserker)
        if (selectedHero == CharacterManager.CHAR_TITAN_BERSERKER || GetComponent<TitanBerserkerSkill>() != null)
        {
            var titan = GetComponent<TitanBerserkerSkill>();
            if (titan == null) titan = gameObject.AddComponent<TitanBerserkerSkill>();
            titan.enabled = true;
            if (!titan.IsAttackReady) return; // Không cho spam ngắt nhịp hoạt ảnh vung đại chùy

            lastAttackTime = Time.time;
            Vector2 facing = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
            int curAtk = attack1Damage + bonusAttackDamage;
            titan.OnPerformAttack(facing, curAtk);
            return;
        }

        // Kich hoat Song Kiem cho Nhan vat 3 (Azure Blade)
        if (selectedHero == CharacterManager.CHAR_AZURE_BLADE || GetComponent<AzureBladeSkill>() != null)
        {
            var azureBlade = GetComponent<AzureBladeSkill>();
            if (azureBlade == null) azureBlade = gameObject.AddComponent<AzureBladeSkill>();
            azureBlade.enabled = true;
            if (!azureBlade.IsAttackReady) return;

            lastAttackTime = Time.time;
            comboStep++;
            if (comboStep > 2) comboStep = 1;
            SoundManager.EnsureInstance()?.PlaySwordSlash(comboStep);

            if (animator != null)
            {
                animator.SetInteger(ComboStepHash, comboStep);
                animator.SetTrigger(AttackTriggerHash);
                string attackState = (comboStep == 1) ? "Attack1" : "Attack2";
                animator.Play(attackState, 0, 0f);
            }

            Vector2 facing = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
            int curAtk = ((comboStep == 1) ? attack1Damage : attack2Damage) + bonusAttackDamage;
            azureBlade.OnPerformAttack(facing, curAtk);
            return;
        }

        // Kich hoat Sa Mac Thich Khach cho Nhan vat 5 (Dune Assassin)
        if (selectedHero == CharacterManager.CHAR_DUNE_ASSASSIN || GetComponent<DuneAssassinSkill>() != null)
        {
            var dune = GetComponent<DuneAssassinSkill>();
            if (dune == null) dune = gameObject.AddComponent<DuneAssassinSkill>();
            dune.enabled = true;
            if (!dune.IsAttackReady) return;

            Vector2 facing = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
            int curAtk = attack1Damage + bonusAttackDamage;
            dune.PerformAttack(facing, curAtk);
            return;
        }

        // Kich hoat Phap Su Tinh Tu cho Nhan vat 6 (Celestial Mage)
        if (selectedHero == CharacterManager.CHAR_CELESTIAL_MAGE || GetComponent<CelestialMageSkill>() != null)
        {
            var mage = GetComponent<CelestialMageSkill>();
            if (mage == null) mage = gameObject.AddComponent<CelestialMageSkill>();
            mage.enabled = true;
            if (!mage.IsAttackReady) return;

            Vector2 facing = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
            int curAtk = attack1Damage + bonusAttackDamage;
            mage.PerformAttack(facing, curAtk);
            return;
        }

        // Đòn đánh mặc định (Vaelstrider)
        if (Time.time - lastAttackTime < 0.22f) return; // Min attack interval to prevent animation glitching
        lastAttackTime = Time.time;
        comboStep++;
        if (comboStep > 2) comboStep = 1;
        SoundManager.EnsureInstance()?.PlaySwordSlash(comboStep);

        if (animator != null)
        {
            animator.SetInteger(ComboStepHash, comboStep);
            animator.SetTrigger(AttackTriggerHash);

            string attackState = (comboStep == 1) ? "Attack1" : "Attack2";
            animator.Play(attackState, 0, 0f);
        }

        // Đồng bộ thời điểm gây sát thương chính xác với khung hình vung kiếm (Impact Frame)
        // Đòn 1 chém chéo vung tới đích sau ~0.10s, Đòn 2 bổ nặng kết chuỗi vung tới đích sau ~0.14s
        float hitDelay = (comboStep == 1) ? 0.10f : 0.14f;
        if (attackHitRoutine != null) StopCoroutine(attackHitRoutine);
        attackHitRoutine = StartCoroutine(DelayedHitDamageRoutine(comboStep, hitDelay));
    }

    private Coroutine attackHitRoutine = null;

    private IEnumerator DelayedHitDamageRoutine(int step, float delay)
    {
        if (delay > 0.005f)
        {
            yield return new WaitForSeconds(delay);
        }
        ExecuteHitDamage(step);
        attackHitRoutine = null;
    }

    private void ExecuteHitDamage(int step = -1)
    {
        int curStep = (step > 0) ? step : comboStep;
        Vector3 origin = (attackPoint != null) ? attackPoint.position : transform.position;
        int baseDmg = (curStep == 1) ? attack1Damage : attack2Damage;
        int damage = baseDmg + bonusAttackDamage;

        // Quet toan bo muc tieu trong tam chem (Bao gom ca Quai vat va Vat the chem be BreakableProp)
        Collider2D[] allHits = Physics2D.OverlapCircleAll(origin, attackRadius);
        var damagedSet = new System.Collections.Generic.HashSet<IDamageable>();
        int enemyCount = 0;

        foreach (var col in allHits)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is PlayerHealth) && !damagedSet.Contains(damageable))
            {
                damagedSet.Add(damageable);

                // Neu la Vat the chem be BreakableProp hoac TreasureChest
                bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
                if (isProp)
                {
                    damageable.TakeDamage(damage);
                    continue;
                }

                // La Quai Vat: Tinh toan Ti Le No Chi Mang (Base 20%, Don 2 Finisher +18% Crit Chance)
                enemyCount++;
                bool isCrit = RollCritical(curStep == 2 ? 0.18f : 0f, out float critMult);
                if (AugmentManager.Instance != null && AugmentManager.Instance.ConsumeShadowAmbush())
                {
                    isCrit = true;
                    critMult = Mathf.Max(critMult, 2.2f);
                }
                SoundManager.EnsureInstance()?.PlayHitImpact(isCrit, curStep == 2);
                int baseAndPhys = baseDmg + bonusPhysAtk;
                int finalPhys = Mathf.RoundToInt(baseAndPhys * critMult);
                int finalMagic = (bonusMagicAtk > 0) ? Mathf.RoundToInt(bonusMagicAtk * critMult) : 0;
                int finalTrue = (bonusTrueAtk > 0) ? Mathf.RoundToInt(bonusTrueAtk * critMult) : 0;

                int actualPhys = (finalPhys > 0) ? AugmentManager.ProcessOutgoingDamage(gameObject, col.gameObject, finalPhys, DamageType.Physical) : 0;
                int actualMagic = (finalMagic > 0) ? AugmentManager.ProcessOutgoingDamage(gameObject, col.gameObject, finalMagic, DamageType.Magic) : 0;
                int actualTrue = (finalTrue > 0) ? AugmentManager.ProcessOutgoingDamage(gameObject, col.gameObject, finalTrue, DamageType.TrueDamage) : 0;
                int actualHitDmg = actualPhys + actualMagic + actualTrue;
                damageable.TakeDamage(actualHitDmg);
                OnPlayerHitEnemy?.Invoke(col.gameObject, actualHitDmg);

                if (actualPhys > 0)
                {
                    DamagePopup.Create(col.transform.position, actualPhys, DamageType.Physical, isCritical: isCrit);
                }
                if (actualMagic > 0)
                {
                    DamagePopup.Create(col.transform.position + new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), 0.25f, 0f), actualMagic, DamageType.Magic, isCritical: isCrit);
                }
                if (actualTrue > 0)
                {
                    DamagePopup.Create(col.transform.position + new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), 0.5f, 0f), actualTrue, DamageType.TrueDamage, isCritical: isCrit);
                }

                var augMgr = AugmentManager.EnsureInstance();
                AugmentManager.TriggerOnHitAugments(gameObject, col.gameObject, actualHitDmg, damageable);

                // Hoi nang luong va tich luy Noi Nang CHỈ KHI chem trung quai vat
                var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
                if (energySys != null)
                {
                    float extraEnergy = (augMgr != null && augMgr.HasSurgingPulse()) ? 6f : 0f;
                    energySys.RestoreEnergy(8f + extraEnergy);
                    energySys.AddInnerEnergy(15f);
                }

                // Hồi phục Máu từ Hút Máu Vật Lý & Hút Máu Phép (Huyết Thần Tế Lễ chuyển hóa Khiên Máu Ảo khi đầy bình)
                if (playerHealth != null)
                {
                    int physHeal = (PhysLifestealPercent > 0f) ? Mathf.RoundToInt(actualPhys * PhysLifestealPercent) : 0;
                    int magicHeal = (SpellVampPercent > 0f) ? Mathf.RoundToInt(actualMagic * SpellVampPercent) : 0;
                    int totalHeal = physHeal + magicHeal;
                    if (GrievousWoundsDebuff.HasDebuff(gameObject))
                    {
                        totalHeal = Mathf.RoundToInt(totalHeal * GrievousWoundsDebuff.GetHealingMultiplier(gameObject));
                    }
                    if (totalHeal > 0)
                    {
                        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth && augMgr != null && augMgr.HasBloodGodTribute())
                        {
                            playerHealth.AddVirtualHealth(totalHeal, 35f);
                        }
                        else
                        {
                            playerHealth.Heal(Mathf.Max(1, totalHeal));
                        }
                    }
                }
            }
        }

        Debug.Log($"<color=cyan>[ATTACK]</color> Vung kiếm (Combo {comboStep})! Đánh trúng: {damagedSet.Count} mục tiêu ({enemyCount} quái vật).");
    }

    public bool PerformSpecialSkill()
    {
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (isUltiActive || (playerController != null && playerController.IsStunned)) return false;

        string selectedHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";

        // Kích hoạt Tuyệt kỹ Gale Ronin (Player 2)
        if (selectedHero == CharacterManager.CHAR_GALE_RONIN || GetComponent<GaleRoninSkill>() != null)
        {
            var ronin = GetComponent<GaleRoninSkill>();
            if (ronin == null) ronin = gameObject.AddComponent<GaleRoninSkill>();
            ronin.enabled = true;
            return ronin.TriggerSpecialSkill();
        }

        // Kích hoạt Tuyệt kỹ Azure Blade (Player 3)
        if (selectedHero == CharacterManager.CHAR_AZURE_BLADE || GetComponent<AzureBladeSkill>() != null)
        {
            var azureBlade = GetComponent<AzureBladeSkill>();
            if (azureBlade == null) azureBlade = gameObject.AddComponent<AzureBladeSkill>();
            azureBlade.enabled = true;
            return azureBlade.TriggerSpecialSkill();
        }

        // Kích hoạt Tuyệt kỹ Titan Berserker (Player 4)
        if (selectedHero == CharacterManager.CHAR_TITAN_BERSERKER || GetComponent<TitanBerserkerSkill>() != null)
        {
            var titan = GetComponent<TitanBerserkerSkill>();
            if (titan == null) titan = gameObject.AddComponent<TitanBerserkerSkill>();
            titan.enabled = true;
            return titan.TriggerSpecialSkill();
        }

        // Kích hoạt Tuyệt kỹ Dune Assassin (Player 5)
        if (selectedHero == CharacterManager.CHAR_DUNE_ASSASSIN || GetComponent<DuneAssassinSkill>() != null)
        {
            var dune = GetComponent<DuneAssassinSkill>();
            if (dune == null) dune = gameObject.AddComponent<DuneAssassinSkill>();
            dune.enabled = true;
            return dune.TriggerSpecialSkill();
        }

        // Kích hoạt Tuyệt kỹ Celestial Mage (Player 6)
        if (selectedHero == CharacterManager.CHAR_CELESTIAL_MAGE || GetComponent<CelestialMageSkill>() != null)
        {
            var mage = GetComponent<CelestialMageSkill>();
            if (mage == null) mage = gameObject.AddComponent<CelestialMageSkill>();
            mage.enabled = true;
            return mage.TriggerSpecialSkill();
        }

        // Kích hoạt Tuyệt kỹ Sweep The Weak nếu là Maid
        var sweepSkill = GetComponent<SweepTheWeakSkill>();
        if (sweepSkill != null && sweepSkill.enabled)
        {
            var energySysMaid = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
            if (energySysMaid != null && !energySysMaid.TryConsumeEnergy(50f)) return false;
            StartCoroutine(sweepSkill.ExecuteSkillRoutine());
            return true;
        }

        // Vaelstrider (Player 1)
        if (skillCooldownTimer > 0f) return false;

        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null && !energySys.TryConsumeEnergy(50f))
        {
            return false; // Khong du nang luong de thi trien Tuyet ky
        }

        float effectiveCooldown = Mathf.Max(5.0f, skillCooldown - ultiCooldownReduction);
        skillCooldownTimer = effectiveCooldown;
        lastSpecialSkillFrame = Time.frameCount;

        StartCoroutine(CinematicSpecialSkillRoutine());
        return true;
    }

    private IEnumerator CinematicSpecialSkillRoutine()
    {
        isUltiActive = true;
        if (playerHealth != null) playerHealth.isInvulnerable = true;
        if (playerController != null) playerController.SetCanMove(false);

        var cam = FindAnyObjectByType<CameraFollow>();

        try
        {
            // ====================================================
            // GIAI ĐOẠN 1: CUTSCENE ZOOM-IN & NGƯNG ĐỌNG THỜI GIAN
            // ====================================================
            Time.timeScale = 0.05f; // Ngưng đọng thời gian (Hit-stop / Cinematic slow-mo)

            // Camera zoom cận cảnh vào người chơi
            if (cam != null) cam.SetCinematicZoom(2.8f);

            // Phát âm thanh thức tỉnh sức mạnh thần bí
            SoundManager.EnsureInstance()?.PlayPortalSpawn();

            // Bật Hiệu Ứng Ánh Mắt Rực Lửa (Eye Flare VFX) trên mặt nhân vật
            var eyeFlare = GetComponent<EyeFlareFX>();
            if (eyeFlare == null) eyeFlare = gameObject.AddComponent<EyeFlareFX>();
            eyeFlare.TriggerEyeFlare(1.0f, isMaid: false);

            // Bật Cutscene UI: 2 vệt đen điện ảnh + Dải ánh mắt anime + Câu thoại ngầu lòi
            var cutsceneUI = UltiCinematicUI.EnsureInstance();
            if (cutsceneUI != null) cutsceneUI.ShowCutscene(1.0f, isMaid: false);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ULTI] Error in cinematic phase: " + ex);
        }

        // Chờ 1.0 giây thời gian thực (Realtime)
        yield return new WaitForSecondsRealtime(1.0f);

        try
        {
            // ====================================================
            // GIAI ĐOẠN 2: THỜI GIAN TRỞ LẠI & BÙNG NỔ BÃO LỐC
            // ====================================================
            Time.timeScale = 1.0f; // Luôn đảm bảo khôi phục thời gian bình thường!
            if (cam != null)
            {
                cam.ResetZoom();
                cam.Shake(0.25f, 0.35f);
            }

            Vector3 center = GetBodyCenterWorld();

            // ĐỢT 1 (t = 0.0s): Chém mở màn & Hút quái vào tâm bão
            if (animator != null) animator.Play("Player_Ulti", 0, 0f);
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            ExecuteCleaveWave(center, damage: 30 + Mathf.RoundToInt(bonusAttackDamage * 0.5f), isFinal: false, pullInward: true);
            yield return new WaitForSecondsRealtime(0.28f);

            // ĐỢT 2 (t = 0.28s): Bão kiếm cuộn xoay liên kích 2 & Gom quái chặt hơn
            if (animator != null) animator.Play("Player_Ulti", 0, 0.15f);
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            if (cam != null) cam.Shake(0.15f, 0.2f);
            ExecuteCleaveWave(center, damage: 45 + Mathf.RoundToInt(bonusAttackDamage * 0.6f), isFinal: false, pullInward: true);
            yield return new WaitForSecondsRealtime(0.28f);

            // ĐỢT 3 (t = 0.56s): Bão kiếm cuộn xoay liên kích 3
            if (animator != null) animator.Play("Player_Ulti", 0, 0.3f);
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            ExecuteCleaveWave(center, damage: 60 + Mathf.RoundToInt(bonusAttackDamage * 0.8f), isFinal: false, pullInward: false);
            yield return new WaitForSecondsRealtime(0.28f);

            // ĐỢT 4 - ĐÒN KẾT LIỄU CỰC ĐẠI (t = 0.84s): NỔ TUNG HẤT VĂNG TOÀN BỘ QUÁI
            if (animator != null) animator.Play("Player_Ulti", 0, 0.45f);
            SoundManager.EnsureInstance()?.PlaySwordSlash();
            SoundManager.EnsureInstance()?.PlayHitImpact();
            if (cam != null) cam.Shake(0.4f, 0.5f);
            ExecuteCleaveWave(center, damage: 120 + bonusUltiDamage + bonusAttackDamage * 2, isFinal: true, pullInward: false);
            yield return new WaitForSecondsRealtime(0.30f);
        }
        finally
        {
            // ====================================================
            // KẾT THÚC TUYỆT KỸ - BẢO ĐẢM 100% LUÔN PHỤC HỒI DI CHUYỂN
            // ====================================================
            Time.timeScale = 1.0f;
            if (cam != null) cam.ResetZoom();
            if (playerHealth != null) playerHealth.isInvulnerable = false;
            if (playerController != null) playerController.SetCanMove(true);
            isUltiActive = false;
            if (animator != null) animator.Play("Idle", 0, 0f);

            Debug.Log("<color=cyan>[SPECIAL SKILL]</color> Hoàn tất chuỗi Bão Lốc Tàn Tích!");
        }
    }

    private void ExecuteCleaveWave(Vector3 center, int damage, bool isFinal, bool pullInward)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(center, skillRadius, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy == null || enemy.gameObject == gameObject || enemy.transform.IsChildOf(transform)) continue;

            var damageable = enemy.GetComponent<IDamageable>() ?? enemy.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is PlayerHealth))
            {
                bool isCrit = RollCritical(isFinal ? 0.25f : 0.05f, out float critMult);
                int rawDamage = Mathf.RoundToInt(damage * critMult);
                int finalDamage = AugmentManager.ProcessOutgoingDamage(gameObject, enemy.gameObject, rawDamage);
                damageable.TakeDamage(finalDamage);
                OnPlayerHitEnemy?.Invoke(enemy.gameObject, finalDamage);
                SoundManager.EnsureInstance()?.PlayHitImpact();
                DamagePopup.Create(enemy.transform.position, finalDamage, DamageType.Physical, isCritical: isCrit);
                if (bonusTrueAtk > 0)
                {
                    int finalTrue = Mathf.RoundToInt(bonusTrueAtk * critMult);
                    DamagePopup.Create(enemy.transform.position + Vector3.up * 0.4f, finalTrue, DamageType.TrueDamage, isCritical: isCrit);
                }

                if (isFinal)
                {
                    AugmentManager.TriggerOnHitAugments(gameObject, enemy.gameObject, finalDamage, damageable);
                }

                bool isProp = (enemy.GetComponent<BreakableProp>() != null || enemy.GetComponentInParent<BreakableProp>() != null || enemy.GetComponent<TreasureChest>() != null || enemy.GetComponentInParent<TreasureChest>() != null);
                if (!isProp)
                {
                    // Tích lũy Nội Năng khi Tuyệt kỹ đánh trúng quái
                    var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
                    if (energySys != null)
                    {
                        energySys.AddInnerEnergy(isFinal ? 12f : 5f);
                    }

                    if (lifestealPercent > 0f && playerHealth != null)
                    {
                        int healAmount = Mathf.Max(1, Mathf.RoundToInt(damage * lifestealPercent));
                        playerHealth.Heal(healAmount);
                    }
                }
            }

            var enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                Vector2 dir = ((Vector2)enemy.transform.position - (Vector2)center).normalized;
                if (dir == Vector2.zero) dir = Vector2.up;

                if (pullInward)
                {
                    // Hút nhẹ vào tâm bão để đợt chém sau trúng trọn vẹn
                    enemyRb.linearVelocity = -dir * 3.5f;
                }
                else if (isFinal)
                {
                    // Đòn cuối hất văng cực mạnh ra xa
                    enemyRb.linearVelocity = dir * skillKnockbackForce;
                }
                else
                {
                    // Đẩy nhẹ giữ khoảng cách trong bão
                    enemyRb.linearVelocity = dir * 2.5f;
                }
            }
        }
    }

    public float GetSkillCooldownNormalized()
    {
        var titan = GetComponent<TitanBerserkerSkill>();
        if (titan != null && titan.enabled) return titan.SpecialCooldownRatio;

        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null && ronin.enabled) return ronin.SpecialCooldownRatio;

        var azure = GetComponent<AzureBladeSkill>();
        if (azure != null && azure.enabled) return azure.SpecialCooldownRatio;

        var dune = GetComponent<DuneAssassinSkill>();
        if (dune != null && dune.enabled) return dune.SpecialCooldownRatio;

        var mage = GetComponent<CelestialMageSkill>();
        if (mage != null && mage.enabled) return mage.SpecialCooldownRatio;

        return (skillCooldown > 0f) ? Mathf.Clamp01(skillCooldownTimer / skillCooldown) : 0f;
    }

    public float GetSkillCooldownRemaining()
    {
        var titan = GetComponent<TitanBerserkerSkill>();
        if (titan != null && titan.enabled) return titan.SpecialCooldownRemaining;

        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null && ronin.enabled) return ronin.SpecialCooldownRemaining;

        var azure = GetComponent<AzureBladeSkill>();
        if (azure != null && azure.enabled) return azure.SpecialCooldownRemaining;

        var dune = GetComponent<DuneAssassinSkill>();
        if (dune != null && dune.enabled) return dune.SpecialCooldownRemaining;

        var mage = GetComponent<CelestialMageSkill>();
        if (mage != null && mage.enabled) return mage.SpecialCooldownRemaining;

        return Mathf.Max(0f, skillCooldownTimer);
    }

    public bool IsSkillReady()
    {
        var titan = GetComponent<TitanBerserkerSkill>();
        if (titan != null && titan.enabled) return titan.IsSpecialReady;

        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null && ronin.enabled) return ronin.IsSpecialReady;

        var azure = GetComponent<AzureBladeSkill>();
        if (azure != null && azure.enabled) return azure.IsSpecialReady;

        var dune = GetComponent<DuneAssassinSkill>();
        if (dune != null && dune.enabled) return dune.IsSpecialReady;

        var mage = GetComponent<CelestialMageSkill>();
        if (mage != null && mage.enabled) return mage.IsSpecialReady;

        return skillCooldownTimer <= 0f && !isUltiActive;
    }

    
    public void BlockAttackThisFrame()
    {
        lastSpecialSkillFrame = Time.frameCount;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (EventSystem.current.IsPointerOverGameObject()) return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return true;
            }
        }

        return false;
    }

    public Vector3 GetBodyCenterWorld()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            return sr.bounds.center;
        }

        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            return col.bounds.center;
        }

        string curChar = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        if (curChar == CharacterManager.CHAR_TITAN_BERSERKER || GetComponent<TitanBerserkerSkill>() != null)
        {
            return transform.position + new Vector3(0f, 0.65f, 0f);
        }
        if (curChar == CharacterManager.CHAR_AZURE_BLADE || GetComponent<AzureBladeSkill>() != null)
        {
            return transform.position + new Vector3(0f, 0.05f, 0f);
        }
        if (curChar == CharacterManager.CHAR_GALE_RONIN || curChar == CharacterManager.CHAR_MAID || GetComponent<GaleRoninSkill>() != null)
        {
            return transform.position + new Vector3(0f, 0.05f, 0f);
        }
        if (curChar == CharacterManager.CHAR_DUNE_ASSASSIN || GetComponent<DuneAssassinSkill>() != null)
        {
            return transform.position + new Vector3(0f, 0.05f, 0f);
        }

        return transform.position + new Vector3(bodyCenterOffset.x, bodyCenterOffset.y, 0f);
    }

    private void UpdateAttackPointPosition()
    {
        if (attackPoint != null && playerController != null)
        {
            Vector2 dir = playerController.GetFacingDirection();
            Vector3 center = GetBodyCenterWorld();
            attackPoint.position = center + new Vector3(dir.x * attackOffsetDistance, dir.y * attackOffsetDistance, 0f);
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 center = GetBodyCenterWorld();
        Gizmos.color = Color.red;
        if (attackPoint != null)
        {
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
        else
        {
            Vector3 origin = center + Vector3.down * attackOffsetDistance;
            Gizmos.DrawWireSphere(origin, attackRadius);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, skillRadius);
    }

    public void AutoAlignCombat()
    {
        string curChar = CharacterManager.EnsureInstance().GetSelectedCharacterId();
        bool isTitan = (curChar == CharacterManager.CHAR_TITAN_BERSERKER || GetComponent<TitanBerserkerSkill>() != null);
        bool isVaelstrider = (curChar == CharacterManager.CHAR_VAELSTRIDER);
        bool isRonin = (curChar == CharacterManager.CHAR_GALE_RONIN || GetComponent<GaleRoninSkill>() != null);
        bool isAzure = (curChar == CharacterManager.CHAR_AZURE_BLADE || GetComponent<AzureBladeSkill>() != null);
        bool isDune = (curChar == CharacterManager.CHAR_DUNE_ASSASSIN || GetComponent<DuneAssassinSkill>() != null);

        if (isTitan)
        {
            bodyCenterOffset = new Vector2(0f, 0.65f);
            attackOffsetDistance = 0.85f;
            attackRadius = 1.35f;
            skillRadius = 5.5f;
        }
        else if (isDune)
        {
            bodyCenterOffset = new Vector2(0f, 0.05f);
            attackOffsetDistance = 0.50f;
            attackRadius = 0.90f;
            skillRadius = 3.5f;
        }
        else if (isRonin)
        {
            bodyCenterOffset = new Vector2(0f, 0.05f);
            attackOffsetDistance = 0.55f;
            attackRadius = 0.85f;
            skillRadius = 1.2f;
        }
        else if (isAzure)
        {
            bodyCenterOffset = new Vector2(0f, 0.05f);
            attackOffsetDistance = 0.45f;
            attackRadius = 0.60f;
            skillRadius = 2.0f;
        }
        else if (isVaelstrider)
        {
            bodyCenterOffset = new Vector2(0f, 0.15f);
            attackOffsetDistance = 0.50f;
            attackRadius = 1.35f;
            skillRadius = 2.6f;
        }
        else
        {
            bodyCenterOffset = new Vector2(0f, 0.15f);
            attackOffsetDistance = 0.50f;
            attackRadius = 1.35f;
            skillRadius = 2.0f;
        }

        if (attackPoint != null)
        {
            UpdateAttackPointPosition();
        }
    }

    private void EnsureAttackPoint()
    {
        if (attackPoint == null)
        {
            Transform existing = transform.Find("AttackPoint");
            if (existing != null)
            {
                attackPoint = existing;
            }
            else if (Application.isPlaying)
            {
                GameObject point = new GameObject("AttackPoint");
                point.transform.SetParent(transform, false);
                attackPoint = point.transform;
            }
        }

        if (attackPoint != null)
        {
            UpdateAttackPointPosition();
        }
    }

    // =========================================================================
    // HỆ THỐNG THẾ THỦ KIẾM THUẪN & PHÓNG ĐẠI SÓNG KIẾM HOÀNG KIM (VAELSTRIDER CHARGE)
    // =========================================================================
    [Header("--- THẾ THỦ KIẾM THUẪN & SÓNG KIẾM HOÀNG KIM (VAELSTRIDER CHARGE) ---")]
    private bool isParrying = false;
    private bool wasParryTriggered = false;
    private float parryHoldDuration = 0f;
    private GameObject parryAuraObj = null;
    private static Sprite cachedDivineWaveSprite = null;
    private const float MAX_PARRY_CHARGE_TIME = 0.65f;
    private const float MIN_PARRY_CHARGE_TIME = 0.22f;

    [SerializeField] private float minParryCooldown = 5.0f; // Cooldown 5s khi gồng ngắn
    [SerializeField] private float maxParryCooldown = 10.0f; // Cooldown 10s khi gồng max công lực
    private float parryCooldownTimer = 0f;
    private float lastParryCooldownMax = 5.0f;

    public bool IsParrying => isParrying;
    public bool WasParryTriggered => wasParryTriggered;
    public bool CanUseParryCharge => parryCooldownTimer <= 0f;
    public float ParryCooldownRemaining => Mathf.Max(0f, parryCooldownTimer);
    public float ParryCooldownRatio => (lastParryCooldownMax > 0f) ? Mathf.Clamp01(parryCooldownTimer / lastParryCooldownMax) : 0f;

    public void StartParryStance(Vector2 initialAim)
    {
        if (playerHealth != null && playerHealth.IsDead) return;
        if (isUltiActive || !CanUseParryCharge) return;

        // Chỉ riêng Hiệp Sĩ Vaelstrider (Player 1) mới có thế thủ Parry Stance
        if (GetComponent<TitanBerserkerSkill>() != null ||
            GetComponent<AzureBladeSkill>() != null ||
            GetComponent<GaleRoninSkill>() != null ||
            GetComponent<DuneAssassinSkill>() != null ||
            GetComponent<CelestialMageSkill>() != null)
        {
            return;
        }

        isParrying = true;
        wasParryTriggered = false;
        parryHoldDuration = 0f;

        if (playerController != null)
        {
            playerController.SetCanMove(false);
            if (initialAim.sqrMagnitude > 0.01f)
            {
                playerController.SetFacingDirection(initialAim.normalized);
            }
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }

        CreateParryAura();
        SoundManager.EnsureInstance()?.PlaySwordSlash();
    }

    public void UpdateParryStance(Vector2 aimDir, float holdDuration)
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            CancelParryStance();
            return;
        }

        if (!isParrying)
        {
            if (!CanUseParryCharge)
            {
                PerformAttack();
                return;
            }
            StartParryStance(aimDir);
        }

        if (!isParrying) return;
        parryHoldDuration = holdDuration;

        if (aimDir.sqrMagnitude > 0.01f && playerController != null)
        {
            playerController.SetFacingDirection(aimDir.normalized);
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }

        float chargeSpan = MAX_PARRY_CHARGE_TIME - MIN_PARRY_CHARGE_TIME;
        float ratio = Mathf.Clamp01((parryHoldDuration - MIN_PARRY_CHARGE_TIME) / (chargeSpan > 0.01f ? chargeSpan : 0.43f));

        // Tiêu hao năng lượng liên tục khi gồng thủ thế (gồng càng lâu tốc độ trừ năng lượng càng tăng)
        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null)
        {
            float drainRate = Mathf.Lerp(12f, 32f, ratio);
            float drainAmount = drainRate * Time.deltaTime;
            bool hasEnergy = energySys.TryConsumeEnergy(drainAmount);
            if (!hasEnergy || energySys.GetEnergy() <= 0.5f)
            {
                // Hết năng lượng khi đang gồng -> Tự động vung đại kiếm phóng sóng kiếm ngay lập tức!
                ReleaseParryStance(aimDir, bonusAttackDamage);
                return;
            }
        }

        // Hiển thị đường ngắm định hướng bắn sóng kiếm hoàng kim xa 4.5m -> 7.5m
        if (parryHoldDuration >= MIN_PARRY_CHARGE_TIME)
        {
            Color aimColor = wasParryTriggered 
                ? new Color(1f, 0.95f, 0.35f, 0.95f) 
                : Color.Lerp(new Color(1f, 0.85f, 0.2f, 0.85f), new Color(1f, 0.98f, 0.6f, 0.95f), ratio);
            float lineLength = Mathf.Lerp(4.5f, 7.5f, ratio);
            float lineWidth = Mathf.Lerp(0.18f, 0.30f, ratio);
            Vector3 aimCenter = GetBodyCenterWorld();
            SkillAimIndicator.EnsureExists(gameObject)?.ShowAimLine((Vector2)aimCenter, aimDir, lineLength, aimColor, lineWidth, 0.45f);
        }
        else
        {
            SkillAimIndicator.Instance?.Hide();
        }

        UpdateParryAura(aimDir, holdDuration);
    }

    public void OnParryIncomingHit()
    {
        if (!isParrying) return;
        wasParryTriggered = true;

        SoundManager.EnsureInstance()?.PlayHitImpact();

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.28f, 0.25f);

        if (parryAuraObj != null)
        {
            var light = parryAuraObj.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
            if (light != null) light.intensity = 4.8f;
        }

        // Hồi 25 Năng Lượng & 35 Nội Năng khi Đỡ đòn phản công thành công
        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null)
        {
            energySys.RestoreEnergy(25f);
            energySys.AddInnerEnergy(35f);
        }
    }

    public void CancelParryStance()
    {
        if (!isParrying) return;
        CancelAttackHold();
        isParrying = false;
        wasParryTriggered = false;
        parryHoldDuration = 0f;
        if (playerController != null) playerController.SetCanMove(true);
        DestroyParryAura();
        SkillAimIndicator.Instance?.Hide();
    }

    public void ReleaseParryStance(Vector2 aimDir, int bonusAtk)
    {
        CancelAttackHold();
        if (playerHealth != null && playerHealth.IsDead)
        {
            CancelParryStance();
            return;
        }

        if (SkillAimIndicator.Instance != null) SkillAimIndicator.Instance.Hide();
        DestroyParryAura();

        bool triggered = wasParryTriggered;
        float holdTime = parryHoldDuration;
        bool wasParryingState = isParrying;
        isParrying = false;
        wasParryTriggered = false;
        parryHoldDuration = 0f;

        if (playerController != null) playerController.SetCanMove(true);

        if (!wasParryingState || (holdTime < MIN_PARRY_CHARGE_TIME && !triggered))
        {
            if (aimDir.sqrMagnitude > 0.01f && playerController != null)
            {
                playerController.SetFacingDirection(aimDir.normalized);
            }
            PerformAttack();
            return;
        }

        // Đặt Cooldown 5s - 10s cho kỹ năng gồng
        float ratio = Mathf.Clamp01(holdTime / MAX_PARRY_CHARGE_TIME);
        lastParryCooldownMax = Mathf.Lerp(minParryCooldown, maxParryCooldown, ratio);
        parryCooldownTimer = lastParryCooldownMax;

        ExecuteDivineRiposte(aimDir, triggered, bonusAtk, ratio);
    }

    private void ExecuteDivineRiposte(Vector2 facingDir, bool isParryCrit, int bonusAtk, float chargeRatio = 1f)
    {
        if (facingDir.sqrMagnitude < 0.01f) facingDir = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
        facingDir = facingDir.normalized;

        if (playerController != null)
        {
            playerController.SetFacingDirection(facingDir);
            playerController.SetCanMove(true);
        }

        if (animator != null)
        {
            animator.SetTrigger(AttackTriggerHash);
            animator.Play("Attack2", 0, 0f);
        }

        SoundManager.EnsureInstance()?.PlaySwordSlash();
        SoundManager.EnsureInstance()?.PlayHitImpact();

        float shake = isParryCrit ? 0.40f : Mathf.Lerp(0.20f, 0.32f, chargeRatio);
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(shake, 0.32f);

        float dmgMult = isParryCrit ? 3.2f : Mathf.Lerp(1.75f, 2.65f, chargeRatio);
        int totalDmg = Mathf.RoundToInt((attack1Damage + bonusAtk) * dmgMult);
        float slashRange = isParryCrit ? 2.8f : Mathf.Lerp(2.0f, 2.6f, chargeRatio);

        Vector2 bodyCenter = (Vector2)GetBodyCenterWorld();
        Vector2 origin = bodyCenter + facingDir * 0.6f;

        // 1. Phóng ra Luồng Sóng Kiếm Hoàng Kim Cực Đại (Divine Sword Wave)
        SpawnDivineRiposteWave(origin, facingDir, isParryCrit, totalDmg, chargeRatio);

        // 2. Chém cận chiến diện rộng
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, slashRange, enemyLayers);
        HashSet<GameObject> damagedRoots = new HashSet<GameObject>();
        int hitCount = 0;

        foreach (var col in hits)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is PlayerHealth))
            {
                GameObject root = (damageable as Component)?.gameObject ?? col.gameObject;
                if (damagedRoots.Contains(root)) continue;
                damagedRoots.Add(root);

                int actualDmg = AugmentManager.ProcessOutgoingDamage(gameObject, col.gameObject, totalDmg, DamageType.Physical);
                damageable.TakeDamage(actualDmg);
                DamagePopup.Create(col.transform.position, actualDmg, DamageType.Physical, isCritical: isParryCrit || chargeRatio >= 0.8f);
                if (bonusTrueAtk > 0)
                {
                    DamagePopup.Create(col.transform.position + Vector3.up * 0.4f, bonusTrueAtk, DamageType.TrueDamage, isCritical: isParryCrit);
                }

                var rb = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 push = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
                    rb.AddForce(push * (isParryCrit ? 14f : 8f), ForceMode2D.Impulse);
                }

                bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
                if (!isProp)
                {
                    hitCount++;
                    ReduceUltiCooldownPercent(0.50f); // Giảm 50% hồi chiêu Ulti khi đánh trúng quái/Boss

                    // Hồi phục Máu từ Ngọc Huyết Thạch / Lifesteal khi nổ phản đòn
                    if (lifestealPercent > 0f && playerHealth != null)
                    {
                        int healAmount = Mathf.Max(1, Mathf.RoundToInt(totalDmg * lifestealPercent));
                        playerHealth.Heal(healAmount);
                    }
                }
            }
        }

        if (isParryCrit && skillCooldownTimer > 0f)
        {
            skillCooldownTimer = Mathf.Max(0f, skillCooldownTimer - 3.0f);
        }
    }

    /// <summary>
    /// Giảm phần trăm thời gian hồi Tuyệt kỹ Ulti khi đánh trúng quái vật / Boss
    /// </summary>
    public void ReduceUltiCooldownPercent(float percent = 0.50f)
    {
        if (skillCooldownTimer > 0f)
        {
            float reduction = skillCooldownTimer * percent;
            skillCooldownTimer = Mathf.Max(0f, skillCooldownTimer - reduction);
            Debug.Log($"<color=yellow>[HOLY SURGE]</color> Đòn đánh trúng Quái/Boss! Giảm {percent * 100f:F0}% hồi chiêu Tuyệt kỹ. Cooldown còn lại: {skillCooldownTimer:F1}s");
        }
    }

    private void SpawnDivineRiposteWave(Vector2 origin, Vector2 dir, bool isCrit, int waveDmg, float chargeRatio)
    {
        StartCoroutine(DivineRiposteWaveRoutine(origin, dir, isCrit, waveDmg, chargeRatio));
    }

    private static Sprite[] holyVfxFrames = null;

    private static Sprite[] GetHolyVfxFrames()
    {
        if (holyVfxFrames != null && holyVfxFrames.Length > 0) return holyVfxFrames;

        List<Sprite> list = new List<Sprite>();
        for (int i = 1; i <= 16; i++)
        {
            string fileName = $"Holy VFX 02 {i}";
            Sprite chosen = null;

            float maxArea = -1f;

#if UNITY_EDITOR
            string path = $"Assets/ART/Player1/Separated Frames/{fileName}.png";
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Sprite spr)
                {
                    float area = spr.rect.width * spr.rect.height;
                    if (area > maxArea)
                    {
                        maxArea = area;
                        chosen = spr;
                    }
                }
            }
#endif

            if (chosen == null)
            {
                var resSprites = Resources.LoadAll<Sprite>($"VFX/Player1_HolyWave/{fileName}");
                if (resSprites != null && resSprites.Length > 0)
                {
                    float resMaxArea = -1f;
                    foreach (var spr in resSprites)
                    {
                        if (spr == null) continue;
                        float area = spr.rect.width * spr.rect.height;
                        if (area > resMaxArea)
                        {
                            resMaxArea = area;
                            chosen = spr;
                        }
                    }
                }
            }

            if (chosen == null)
            {
                chosen = LoadRawPngSprite($"Assets/ART/Player1/Separated Frames/{fileName}.png");
            }

            if (chosen != null)
            {
                list.Add(chosen);
            }
        }

        if (list.Count > 0) holyVfxFrames = list.ToArray();
        return holyVfxFrames;
    }

    private static Sprite LoadRawPngSprite(string relativePath)
    {
        try
        {
            string fullPath = System.IO.Path.Combine(Application.dataPath, "..", relativePath);
            if (System.IO.File.Exists(fullPath))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                if (tex.LoadImage(bytes))
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
                }
            }
        }
        catch { }
        return null;
    }

    private IEnumerator DivineRiposteWaveRoutine(Vector2 origin, Vector2 dir, bool isCrit, int waveDmg, float chargeRatio)
    {
        Sprite[] frames = GetHolyVfxFrames();

        GameObject root = new GameObject("Vael_DivineHolySurge");
        root.transform.position = origin;

        float maxDist = isCrit ? 8.5f : Mathf.Lerp(5.5f, 8.0f, chargeRatio);
        float stepDist = 1.25f; // Giãn cách rộng hơn giữa các cột
        int pillarCount = Mathf.Clamp(Mathf.RoundToInt(maxDist / stepDist), 4, 8);

        // Ánh sáng hoàng kim tổng thể của đợt bộc phá thánh quang
        var surgeLight = root.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        surgeLight.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        surgeLight.color = new Color(1f, 0.92f, 0.45f, 1f);
        surgeLight.intensity = isCrit ? 3.5f : Mathf.Lerp(2.2f, 3.0f, chargeRatio);
        surgeLight.pointLightInnerRadius = 0.8f;
        surgeLight.pointLightOuterRadius = maxDist * 0.9f;

        HashSet<GameObject> hitObjects = new HashSet<GameObject>();
        bool[] hasReducedUlti = new bool[] { false };

        // Kích hoạt chuỗi các cột thánh quang bộc phá nhấp nhô trào dâng tuần tự SIÊU CHẬM để quan sát rõ từng nhịp
        float propagationDelay = 0.20f; // 200ms giữa mỗi cột
        for (int i = 0; i < pillarCount; i++)
        {
            float d = 0.6f + (i * stepDist);
            Vector3 pillarPos = (Vector3)origin + (Vector3)(dir * d);
            float scale = isCrit ? 1.85f : Mathf.Lerp(1.35f, 1.70f, chargeRatio); // Thu nhỏ kích thước vừa vặn
            float delay = i * propagationDelay;

            StartCoroutine(HolyPillarRoutine(root.transform, pillarPos, scale, delay, frames, waveDmg, isCrit, chargeRatio, hitObjects, dir, hasReducedUlti));
        }

        // Đợi toàn bộ chuỗi thánh quang chạy xong trọn vẹn rồi dọn dẹp
        float totalTime = (pillarCount * propagationDelay) + 2.8f;
        float elapsed = 0f;
        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;
            float life = elapsed / totalTime;
            if (surgeLight != null)
            {
                surgeLight.intensity = Mathf.Lerp(isCrit ? 3.5f : 2.5f, 0f, life);
            }
            yield return null;
        }

        Destroy(root);
    }

    private IEnumerator HolyPillarRoutine(Transform rootTransform, Vector3 pos, float scale, float delay, Sprite[] frames, int waveDmg, bool isCrit, float chargeRatio, HashSet<GameObject> hitObjects, Vector2 dir, bool[] hasReducedUlti = null)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        GameObject pillar = new GameObject("HolyPillar_Eruption");
        if (rootTransform != null) pillar.transform.SetParent(rootTransform, true);
        pillar.transform.position = pos;
        pillar.transform.localScale = Vector3.one * scale;

        var sr = pillar.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 5000 + Mathf.RoundToInt(-pos.y * 100) + 20;

        var pLight = pillar.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        pLight.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        pLight.color = new Color(1f, 0.95f, 0.5f, 1f);
        pLight.intensity = isCrit ? 3.2f : 2.2f;
        pLight.pointLightInnerRadius = 0.3f;
        pLight.pointLightOuterRadius = 1.35f;

        // Quét sát thương ngay khi cột quang trào dâng
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.65f * scale, enemyLayers);
        foreach (var col in hits)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable != null && !(damageable is PlayerHealth))
            {
                GameObject hitRoot = (damageable as Component)?.gameObject ?? col.gameObject;
                if (!hitObjects.Contains(hitRoot))
                {
                    hitObjects.Add(hitRoot);
                    int actualWaveDmg = AugmentManager.ProcessOutgoingDamage(gameObject, col.gameObject, waveDmg, DamageType.Physical);
                    damageable.TakeDamage(actualWaveDmg);
                    DamagePopup.Create(col.transform.position, actualWaveDmg, DamageType.Physical, isCritical: isCrit || chargeRatio >= 0.8f);

                    var rb = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.AddForce(dir * (isCrit ? 15f : 10f), ForceMode2D.Impulse);
                    }

                    bool isProp = (col.GetComponent<BreakableProp>() != null || col.GetComponentInParent<BreakableProp>() != null || col.GetComponent<TreasureChest>() != null || col.GetComponentInParent<TreasureChest>() != null);
                    if (!isProp)
                    {
                        if (hasReducedUlti != null && !hasReducedUlti[0])
                        {
                            hasReducedUlti[0] = true;
                            ReduceUltiCooldownPercent(0.50f); // Giảm 50% hồi chiêu Ulti khi đánh trúng quái/Boss
                        }

                        // Hồi phục Máu từ Ngọc Huyết Thạch / Lifesteal khi cột thánh quang đánh trúng quái
                        if (lifestealPercent > 0f && playerHealth != null)
                        {
                            int healAmount = Mathf.Max(1, Mathf.RoundToInt(waveDmg * lifestealPercent));
                            playerHealth.Heal(healAmount);
                        }
                    }
                }
            }
        }

        // Chạy tuần tự 16 frame hoạt ảnh trào dâng siêu chậm (8 FPS, mỗi frame 0.125s) để quan sát từng chi tiết
        if (frames != null && frames.Length > 0)
        {
            float fps = 8f;
            float frameTime = 1f / fps;
            for (int f = 0; f < frames.Length; f++)
            {
                if (sr == null) break;
                sr.sprite = frames[f];
                yield return new WaitForSeconds(frameTime);
            }
        }
        else
        {
            if (cachedDivineWaveSprite != null || (cachedDivineWaveSprite = GenerateDivineWaveSprite()) != null)
            {
                sr.sprite = cachedDivineWaveSprite;
            }
            yield return new WaitForSeconds(1.2f);
        }

        // Fade out êm đềm
        float fade = 1f;
        while (fade > 0f)
        {
            fade -= Time.deltaTime * 2f;
            if (sr != null) sr.color = new Color(1f, 1f, 1f, Mathf.Max(0f, fade));
            if (pLight != null) pLight.intensity = Mathf.Max(0f, pLight.intensity * fade);
            yield return null;
        }

        Destroy(pillar);
    }

    private void CreateParryAura()
    {
        if (parryAuraObj != null) Destroy(parryAuraObj);

        // Hào quang năng lượng hoàng kim phát sáng ngay trên người Vaelstrider
        parryAuraObj = new GameObject("Vael_ParryAura");
        parryAuraObj.transform.SetParent(transform, false);
        Vector3 localCenter = GetBodyCenterWorld() - transform.position;
        parryAuraObj.transform.localPosition = localCenter;

        var light = parryAuraObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = new Color(1f, 0.90f, 0.35f, 1f);
        light.intensity = 2.6f;
        light.pointLightInnerRadius = 0.5f;
        light.pointLightOuterRadius = 2.4f;
    }

    private void UpdateParryAura(Vector2 aimDir, float holdDuration)
    {
        if (parryAuraObj == null) return;

        // Giữ hào quang tỏa sáng tập trung quanh thân thể nhân vật
        Vector3 localCenter = GetBodyCenterWorld() - transform.position;
        parryAuraObj.transform.localPosition = localCenter;

        // Hiệu ứng nhấp nháy phát quang hào quang khi tụ lực
        float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.18f;
        parryAuraObj.transform.localScale = Vector3.one * (1.1f * pulse);

        var light = parryAuraObj.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
        if (light != null)
        {
            light.intensity = wasParryTriggered ? 4.8f : (2.6f * pulse);
        }
    }

    private void DestroyParryAura()
    {
        if (parryAuraObj != null)
        {
            Destroy(parryAuraObj);
            parryAuraObj = null;
        }
    }

    private static Sprite GenerateDivineWaveSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color core = new Color(1f, 1f, 0.9f, 1f);
        Color gold = new Color(1f, 0.85f, 0.2f, 0.95f);
        Color flame = new Color(1f, 0.5f, 0.05f, 0.85f);

        Vector2 center = new Vector2(size * 0.5f, size * 0.15f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                float ang = Mathf.Atan2(y - center.y, x - center.x) * Mathf.Rad2Deg;

                if (ang >= 30f && ang <= 150f && dist >= 22f && dist <= 32f)
                {
                    float dRatio = Mathf.Abs(dist - 27f) / 5f;
                    tex.SetPixel(x, y, (dRatio < 0.3f) ? core : ((dRatio < 0.7f) ? gold : flame));
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    // =========================================================
    // THÁNH THUẪN PHẢN ĐÒN (RADIANT RIPOSTE DASH) CHO HIỆP SĨ VAELSTRIDER
    // =========================================================
    private bool isRadiantRiposteDashing = false;
    private bool wasRiposteTriggeredThisDash = false;
    private Coroutine radiantRiposteRoutine = null;
    private GameObject radiantRiposteAuraObj = null;

    public bool IsRadiantRiposteDashing => isRadiantRiposteDashing;

    public void TriggerRadiantRiposteDash(Vector2 dashDir)
    {
        if (radiantRiposteRoutine != null) StopCoroutine(radiantRiposteRoutine);
        radiantRiposteRoutine = StartCoroutine(RadiantRiposteDashRoutine(dashDir));
    }

    public void OnRadiantRiposteTriggered()
    {
        if (isRadiantRiposteDashing && !wasRiposteTriggeredThisDash)
        {
            Vector2 dir = (playerController != null) ? playerController.GetFacingDirection() : Vector2.right;
            ExecuteRadiantRiposteCounter(dir);
        }
    }

    private IEnumerator RadiantRiposteDashRoutine(Vector2 dashDir)
    {
        isRadiantRiposteDashing = true;
        wasRiposteTriggeredThisDash = false;

        var ph = GetComponent<PlayerHealth>();
        bool prevInvul = ph != null && ph.isInvulnerable;
        if (ph != null) ph.isInvulnerable = true;

        CreateRadiantRiposteAura();

        float elapsed = 0f;
        float dashDuration = (playerController != null) ? playerController.DashDuration : 0.35f;

        // Vòng lặp quét né đòn / phản kích trong suốt thời gian lướt
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;

            if (!wasRiposteTriggeredThisDash)
            {
                // Quét quái trong phạm vi lướt qua
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                int mask = (enemyLayer != -1) ? (1 << enemyLayer) : ~0;
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.45f, mask);

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].CompareTag("Enemy") || hits[i].GetComponent<IDamageable>() != null)
                    {
                        ExecuteRadiantRiposteCounter(dashDir);
                        break;
                    }
                }
            }

            yield return null;
        }

        DestroyRadiantRiposteAura();
        if (ph != null) ph.isInvulnerable = prevInvul;
        isRadiantRiposteDashing = false;
    }

    private void ExecuteRadiantRiposteCounter(Vector2 dir)
    {
        if (wasRiposteTriggeredThisDash) return;
        wasRiposteTriggeredThisDash = true;

        if (dir.sqrMagnitude < 0.01f) dir = (playerController != null) ? playerController.GetFacingDirection() : Vector2.right;
        dir = dir.normalized;

        // 1. Âm thanh & rung lắc màn hình
        SoundManager.EnsureInstance()?.PlaySwordSlash();
        SoundManager.EnsureInstance()?.PlayHitImpact();

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.35f, 0.28f);

        // 2. Hồi 50% Cooldown Dash & Hồi 15 Mana
        if (playerController != null)
        {
            playerController.ReduceDashCooldown(0.5f);
        }

        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null)
        {
            energySys.RestoreEnergy(15f);
        }

        // 3. Hiệu ứng bộc phá Thánh Kiếm Khí (Radiant Sword Waves)
        int counterDmg = Mathf.RoundToInt(attack1Damage * 1.6f);
        StartCoroutine(DivineRiposteWaveRoutine(transform.position, dir, true, counterDmg, 1.0f));

        // 4. Banner chữ chiến đấu nổi
        DamagePopup.CreateText(transform.position + Vector3.up * 1.5f, "THÁNH THUẪN PHẢN ĐÒN", new Color(1f, 0.85f, 0.2f, 1f));

        // 5. Hiệu ứng tia sáng bừng sáng
        if (radiantRiposteAuraObj != null)
        {
            var sr = radiantRiposteAuraObj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 1f, 0.7f, 1f);
        }
    }

    private void CreateRadiantRiposteAura()
    {
        DestroyRadiantRiposteAura();

        radiantRiposteAuraObj = new GameObject("Vael_RadiantRiposteAura");
        radiantRiposteAuraObj.transform.SetParent(transform, false);
        radiantRiposteAuraObj.transform.localPosition = new Vector3(0f, 0.8f, 0f);

        var sr = radiantRiposteAuraObj.AddComponent<SpriteRenderer>();
        sr.sprite = GenerateRadiantShieldSprite();
        sr.color = new Color(1f, 0.88f, 0.2f, 0.85f);
        sr.sortingOrder = 3050;
        radiantRiposteAuraObj.transform.localScale = Vector3.one * 1.4f;

        var light = radiantRiposteAuraObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = new Color(1f, 0.85f, 0.3f, 1f);
        light.intensity = 2.5f;
        light.pointLightInnerRadius = 0.5f;
        light.pointLightOuterRadius = 3.2f;
    }

    private void DestroyRadiantRiposteAura()
    {
        if (radiantRiposteAuraObj != null)
        {
            Destroy(radiantRiposteAuraObj);
            radiantRiposteAuraObj = null;
        }
    }

    private static Sprite GenerateRadiantShieldSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float radius = 28f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                if (d <= radius)
                {
                    float edge = (radius - d) / radius;
                    if (d >= radius - 5f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 0.85f, 0.95f)); // Viền sáng rực rỡ
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(1f, 0.82f, 0.15f, Mathf.Lerp(0.2f, 0.6f, edge)));
                    }
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        AutoAlignCombat();
    }
#endif
}
