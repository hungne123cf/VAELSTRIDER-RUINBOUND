using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Player Stats")]
    [SerializeField] private int maxHealth = 100;
    private Sprite[] deathSprites;
    private int currentHealth;
    private int baseMaxHealth = 100;
    private int bonusMaxHealth = 0;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public void SetBaseMaxHealth(int newBase)
    {
        baseMaxHealth = newBase;
        RefreshSocketHealthBonus();
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        PlayerPrefs.SetInt("Saved_CurrentHealth", currentHealth);
        PlayerPrefs.Save();
    }


    public void ApplyUpgradeBonus(int bonusHp)
    {
        int oldBonus = bonusMaxHealth;
        bonusMaxHealth = bonusHp;
        RefreshSocketHealthBonus();
    }

    // ─── Augment HP bonus ────────────────────────────────────────────────────
    private int augmentBonusHp = 0;

    public void ApplyAugmentBonus(int bonusHp)
    {
        augmentBonusHp = bonusHp;
        RefreshSocketHealthBonus();
    }

    /// <summary>Hồi HP trực tiếp (không vượt quá maxHealth). Dùng cho SoulHarvest / Regen.</summary>
    public void HealDirect(int amount)
    {
        if (isDead || amount <= 0) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        PlayerPrefs.SetInt("Saved_CurrentHealth", currentHealth);
    }


    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDead = false;
    public bool IsDead => isDead || currentHealth <= 0;
    public bool isInvulnerable = false;

    public static PlayerHealth Instance { get; private set; }
    public static bool IsPlayerDead => Instance != null && Instance.IsDead;

    public static Vector3 LastDeathPosition { get; private set; }
    public static string LastDeathScene { get; private set; }

    void Awake()
    {
        Instance = this;
        baseMaxHealth = maxHealth;
        RefreshSocketHealthBonus();

        int savedHp = PlayerPrefs.GetInt("Saved_CurrentHealth", maxHealth);
        if (savedHp <= 0 || savedHp > maxHealth) savedHp = maxHealth;
        currentHealth = Mathf.Clamp(savedHp, 1, maxHealth);

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        // Auto-load death sprites from Resources/Player/player_death if not set
        if (deathSprites == null || deathSprites.Length == 0)
        {
            deathSprites = Resources.LoadAll<Sprite>("Player/player_death");
        }
    }

    public void RefreshSocketHealthBonus()
    {
        EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out int sockHp, out _, out _, out _, out _, out socketHpRegen);
        var upgradeMgr = PlayerUpgradeManager.EnsureInstance();
        string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        int upHp = upgradeMgr != null ? upgradeMgr.GetHealthBonus(curHero) : 0;
        ArmorShopUI.GetEquippedArmorBonus(out int armorHp, out float _, out float _);

        int weaponHp = 0;
        string equippedWpnId = PlayerPrefs.GetString("Equipped_Weapon_Id", "");
        if (!string.IsNullOrEmpty(equippedWpnId))
        {
            var def = InventoryItemDatabase.GetItem(equippedWpnId);
            if (def != null && def.hpBonus > 0)
            {
                weaponHp = def.hpBonus;
            }
        }

        HeroCostumeManager.GetEquippedCostumeBonus(CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "", out int costumeHp, out _, out _, out _);

        int oldMax = maxHealth;
        maxHealth = baseMaxHealth + upHp + armorHp + weaponHp + sockHp + costumeHp + augmentBonusHp;

        if (currentHealth > maxHealth || currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
        else if (maxHealth > oldMax && oldMax > 0)
        {
            currentHealth += (maxHealth - oldMax);
        }

        currentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);
        PlayerPrefs.SetInt("Saved_CurrentHealth", currentHealth);
        PlayerPrefs.Save();
    }

    private float socketHpRegen = 0f;
    private float regenTimer = 0f;

    [Header("Virtual Health / Máu Ảo (+50% HP Tuyệt Kỹ)")]
    private int currentVirtualHealth = 0;
    private int maxVirtualHealth = 0;
    private float virtualHealthDuration = 15f;
    private float virtualHealthTimer = 0f;
    private bool isVirtualHealthActive = false;

    public void AddVirtualHealth(int amount, float duration = 15f)
    {
        if (amount <= 0 || isDead) return;
        int maxCap = Mathf.RoundToInt(maxHealth * 0.85f);
        if (isVirtualHealthActive && currentVirtualHealth > 0)
        {
            currentVirtualHealth = Mathf.Min(maxCap, currentVirtualHealth + amount);
            maxVirtualHealth = Mathf.Max(maxVirtualHealth, currentVirtualHealth);
        }
        else
        {
            currentVirtualHealth = Mathf.Min(maxCap, amount);
            maxVirtualHealth = currentVirtualHealth;
        }
        virtualHealthDuration = duration;
        virtualHealthTimer = duration;
        isVirtualHealthActive = true;

        SoundManager.EnsureInstance()?.PlayHitImpact();
    }

    public int ConsumeHealthPercent(float percent)
    {
        if (isDead || percent <= 0f) return 0;
        int cost = Mathf.Max(1, Mathf.RoundToInt(maxHealth * percent));
        return ConsumeHealth(cost);
    }

    public int ConsumeHealth(int amount)
    {
        if (isDead || amount <= 0) return 0;

        int remainingCost = amount;
        int virtualDeducted = 0;

        // 1. ƯU TIÊN TRỪ VÀO LỚP MÁU ẢO (VIRTUAL HEALTH) TRƯỚC
        if (isVirtualHealthActive && currentVirtualHealth > 0)
        {
            if (currentVirtualHealth >= remainingCost)
            {
                currentVirtualHealth -= remainingCost;
                virtualDeducted = remainingCost;
                remainingCost = 0;
                if (currentVirtualHealth <= 0)
                {
                    isVirtualHealthActive = false;
                }
            }
            else
            {
                virtualDeducted = currentVirtualHealth;
                remainingCost -= currentVirtualHealth;
                currentVirtualHealth = 0;
                isVirtualHealthActive = false;
            }

            DamagePopup.CreateText(transform.position + Vector3.up * 0.6f, $"-{virtualDeducted}", new Color(1f, 0.80f, 0.25f));
        }

        // 2. NẾU MÁU ẢO KHÔNG ĐỦ THÌ MỚI TRỪ VÀO MÁU GỐC (GIỮ LẠI TỐI THIỂU 1 HP)
        int realHpDeducted = 0;
        if (remainingCost > 0)
        {
            int spendable = Mathf.Max(0, currentHealth - 1);
            realHpDeducted = Mathf.Min(remainingCost, spendable);
            if (realHpDeducted > 0)
            {
                currentHealth -= realHpDeducted;
                PlayerPrefs.SetInt("Saved_CurrentHealth", currentHealth);
                PlayerPrefs.Save();
                DamagePopup.CreateText(transform.position + Vector3.up * 0.75f, $"-{realHpDeducted}", new Color(1f, 0.25f, 0.25f));
                if (spriteRenderer != null)
                {
                    StartCoroutine(FlashColorRoutine(new Color(1f, 0.2f, 0.2f)));
                }
            }
        }

        return virtualDeducted + realHpDeducted;
    }

    public int GetVirtualHealth() => currentVirtualHealth;
    public int GetMaxVirtualHealth() => maxVirtualHealth;
    public float GetVirtualHealthTimer() => virtualHealthTimer;
    public float GetVirtualHealthDuration() => virtualHealthDuration;
    public bool IsVirtualHealthActive() => isVirtualHealthActive && currentVirtualHealth > 0;
    public bool IsVirtualHealthExpiringSoon() => isVirtualHealthActive && virtualHealthTimer <= 3.5f;

    void Update()
    {
        // 1. Đếm thời gian duy trì Lớp Máu Ảo
        if (isVirtualHealthActive)
        {
            virtualHealthTimer -= Time.deltaTime;
            if (virtualHealthTimer <= 0f)
            {
                isVirtualHealthActive = false;
                currentVirtualHealth = 0;
                maxVirtualHealth = 0;
            }
        }

        // 2. Hồi phục HP theo ngọc khảm
        if (socketHpRegen > 0f && !isDead && currentHealth < maxHealth)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= 1.0f)
            {
                regenTimer = 0f;
                Heal(Mathf.RoundToInt(socketHpRegen));
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isInvulnerable) return;

        // Kiểm tra Thánh Thuẫn Phản Đòn khi Dash của Hiệp Sĩ Vaelstrider
        var playerCombat = GetComponent<PlayerCombat>();
        if (playerCombat != null && playerCombat.IsRadiantRiposteDashing)
        {
            playerCombat.OnRadiantRiposteTriggered();
            return; // Đỡ đòn hoàn hảo và miễn nhiễm sát thương!
        }

        // Kiểm tra Thế thủ Đỡ đòn (Parry Stance) của Hiệp sĩ Vaelstrider (Giảm 20% sát thương nhận vào)
        if (playerCombat != null && playerCombat.IsParrying)
        {
            playerCombat.OnParryIncomingHit();
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * 0.80f)); // Chặn 20% sát thương nhận vào (nhận 80%)
        }

        // Kiểm tra Thế thủ Gồng Địa Chấn của Titan Berserker
        var titan = GetComponent<TitanBerserkerSkill>();
        if (titan != null && titan.IsChargingEarthshaker)
        {
            return; // Bất tử trong suốt thời gian gồng búa!
        }

        // Kiểm tra Khiên Hộ Thể Tinh Tú của Celestial Mage (Giảm 60% sát thương)
        var mage = GetComponent<CelestialMageSkill>();
        if (mage != null && mage.IsChargingBarrier)
        {
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - mage.barrierDamageReduction)));
        }

        // Giảm sát thương theo Giáp của trang bị đang mặc + Khảm ngọc + Trang phục (tối đa giảm 65% để người chơi luôn nhận sát thương thực tế)
        ArmorShopUI.GetEquippedArmorBonus(out int _, out float armorDef, out float _);
        EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out _, out int sockDef, out _, out _, out _, out _);
        HeroCostumeManager.GetEquippedCostumeBonus(CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "", out _, out _, out float costumeDef, out _);
        float totalDef = Mathf.Min(65f, armorDef + sockDef + costumeDef);
        if (totalDef > 0)
        {
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - totalDef / 100f)));
        }

        // 1. Kiểm tra và trừ vào Lớp Máu Ảo trước (Virtual Health Barrier)
        if (isVirtualHealthActive && currentVirtualHealth > 0)
        {
            if (currentVirtualHealth >= damage)
            {
                currentVirtualHealth -= damage;
                if (currentVirtualHealth <= 0)
                {
                    isVirtualHealthActive = false;
                }
                return; // Đã hấp thụ toàn bộ đòn đánh bằng máu ảo!
            }
            else
            {
                int absorbed = currentVirtualHealth;
                damage -= absorbed;
                currentVirtualHealth = 0;
                isVirtualHealthActive = false;
            }
        }

        // 2. Kiểm tra và hấp thụ sát thương bằng Lớp Giáp Hộ Thân (Energy System Shield) nếu có
        var energySys = GetComponent<PlayerEnergySystem>() ?? PlayerEnergySystem.Instance;
        if (energySys != null && energySys.IsShieldActive())
        {
            damage = energySys.AbsorbDamage(damage);
            if (damage <= 0)
            {
                // Giáp hộ thân đã chặn toàn bộ đòn đánh!
                return;
            }
        }

        // 2b. Kiểm tra Thế Thủ Ảo Ảnh của Sa Mạc Thích Khách (Player 5)
        var duneSkill = GetComponent<DuneAssassinSkill>();
        if (duneSkill != null && duneSkill.IsDefending)
        {
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - duneSkill.defendDamageReduction)));
            duneSkill.OnDefendHitTrigger(damage);
        }

        // KHÔNG CỘNG NỘI NĂNG KHI BỊ ĐÁNH (Người chơi phải tự đánh trúng quái mới tăng nội năng)

        currentHealth = Mathf.Max(0, currentHealth - damage);
        PlayerPrefs.SetInt("Saved_CurrentHealth", currentHealth);
        PlayerPrefs.Save();
        SoundManager.EnsureInstance()?.PlayPlayerHurt();

        // Hiển thị số sát thương màu đỏ nảy lên trên đầu Player khi dính đòn
        DamagePopup.Create(transform.position + Vector3.up * 0.6f, damage, isCritical: false, isPlayer: true);
        Debug.Log($"<color=orange>[PLAYER HURT]</color> Bạn bị quái đánh trúng! Mất {damage} HP. Còn lại: {currentHealth}/{maxHealth}");

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.12f, 0.15f);

        // Kích hoạt Hurt animation trên Animator (Có Super Armor khi đang Xoay búa hoặc Tuyệt kỹ)
        var anim = GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            var titanComp = GetComponent<TitanBerserkerSkill>();
            var pCombat = GetComponent<PlayerCombat>();
            bool isSuperArmor = (titanComp != null && (titanComp.IsSpinning || titanComp.IsExecutingCataclysm)) || (pCombat != null && pCombat.IsParrying);
            if (!isSuperArmor)
            {
                anim.SetTrigger("HurtTrigger");
                anim.Play("Hurt", 0, 0f);
            }
        }

        var mageSkill = GetComponent<CelestialMageSkill>();
        if (mageSkill != null && mageSkill.enabled)
        {
            mageSkill.OnTakeDamageHurt();
        }

        if (spriteRenderer != null && currentHealth > 0)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRedRoutine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public bool Heal(int amount)
    {
        if (isDead || currentHealth >= maxHealth) return false;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        PlayerPrefs.SetInt("Saved_CurrentHealth", currentHealth);
        PlayerPrefs.Save();
        Debug.Log($"<color=green>[PLAYER HEAL]</color> Hoi {amount} HP! Mau hien tai: {currentHealth}/{maxHealth}");

        if (spriteRenderer != null)
        {
            StartCoroutine(FlashColorRoutine(new Color(0.3f, 1f, 0.4f)));
        }

        DamagePopup.CreateText(transform.position, $"+{amount} HP", new Color(0.2f, 1f, 0.35f));
        return true;
    }

    public bool IsFullHealth() => currentHealth >= maxHealth;

    private IEnumerator FlashRedRoutine()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (!isDead && spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    private IEnumerator FlashColorRoutine(Color color)
    {
        spriteRenderer.color = color;
        yield return new WaitForSeconds(0.18f);
        if (!isDead && spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead || isInvulnerable) return;
        isDead = true;
        currentHealth = 0;
        LastDeathPosition = transform.position;
        LastDeathScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt("Saved_CurrentHealth", maxHealth);
        PlayerPrefs.Save();
        Debug.Log("<color=red>[PLAYER DIED]</color> Nhan vat guc nga! Phat animation chet...");

        StopAllCoroutines();

        // 1. Vo hieu hoa dieu khien & tan cong
        var pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        var combat = GetComponent<PlayerCombat>();
        if (combat != null)
        {
            combat.CancelParryStance();
            combat.enabled = false;
        }

        var titan = GetComponent<TitanBerserkerSkill>();
        if (titan != null)
        {
            titan.CancelEarthshakerCharge();
            titan.StopAllCoroutines();
        }

        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null)
        {
            ronin.CancelIaidoCharge();
            ronin.StopAllCoroutines();
        }

        var azure = GetComponent<AzureBladeSkill>();
        if (azure != null)
        {
            azure.CancelAim();
            azure.StopAllCoroutines();
        }

        if (SkillAimIndicator.Instance != null) SkillAimIndicator.Instance.Hide();
        var rangedAim = GetComponent<RangedAimIndicator>();
        if (rangedAim != null) rangedAim.Hide();
        if (RangedAimIndicator.Instance != null) RangedAimIndicator.Instance.Hide();
        if (SkillCancelZoneUI.Instance != null) SkillCancelZoneUI.Instance.Hide();

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Vo hieu hoa Collider de khong con va cham nua
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 2. Rung manh camera
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.35f, 0.45f);

        // 3. Giu dung goc nhan vat (khong xoay 90 do) & phat animation chet
        transform.rotation = Quaternion.identity;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        // Tat Animator de coroutine truc tiep dieu khien tung frame chet ma khong bi ghi de
        var anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = false;
        }

        StartCoroutine(PlayKneelingDeathRoutine());
    }

    private Sprite[] GetDeathSpritesForCurrentCharacter(out bool flipX)
    {
        flipX = false;
        string curHero = CharacterManager.EnsureInstance()?.GetSelectedCharacterId() ?? "";
        curHero = curHero.ToLowerInvariant();

        if (curHero.Contains("titan") || curHero.Contains("player4") || GetComponent<TitanBerserkerSkill>() != null)
        {
            var anim = GetComponent<Animator>();
            float moveX = anim != null ? anim.GetFloat("MoveX") : 0f;
            float moveY = anim != null ? anim.GetFloat("MoveY") : -1f;

#if UNITY_EDITOR
            var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player4/Hammer_Death.png");
            if (sprites != null)
            {
                var list = new List<Sprite>();
                foreach (var o in sprites) if (o is Sprite s) list.Add(s);
                if (list.Count > 0)
                {
                    return list.OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToArray();
                }
            }
#endif
            var res = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player4/Hammer_Death.png", 64, 64);
            if (res != null && res.Length > 0) return res;
        }
        else if (curHero.Contains("azure") || curHero.Contains("player3") || GetComponent<AzureBladeSkill>() != null)
        {
            var anim = GetComponent<Animator>();
            float moveX = anim != null ? anim.GetFloat("MoveX") : 0f;
            float moveY = anim != null ? anim.GetFloat("MoveY") : -1f;

#if UNITY_EDITOR
            var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player3/Sword_Death.png");
            if (sprites != null)
            {
                var list = new List<Sprite>();
                foreach (var o in sprites) if (o is Sprite s) list.Add(s);
                if (list.Count >= 28)
                {
                    // Row 0: Down (0..6), Row 1: Left (7..13), Row 2: Up-Left (14..20), Row 3: Up (21..27)
                    var ordered = list.OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToList();
                    if (moveY > 0.35f)
                    {
                        return ordered.Skip(3 * 7).Take(7).ToArray(); // Up
                    }
                    else if (moveX > 0.25f)
                    {
                        flipX = true;
                        return ordered.Skip(1 * 7).Take(7).ToArray(); // Right
                    }
                    else if (moveX < -0.25f)
                    {
                        flipX = false;
                        return ordered.Skip(1 * 7).Take(7).ToArray(); // Left
                    }
                    else
                    {
                        return ordered.Skip(0 * 7).Take(7).ToArray(); // Down
                    }
                }
                else if (list.Count > 0)
                {
                    return list.OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).Take(7).ToArray();
                }
            }
#endif
            var res = Resources.LoadAll<Sprite>("Player3/Sword_Death");
            if (res != null && res.Length > 0) return res;
        }
        else if (curHero.Contains("ronin") || curHero.Contains("gale") || GetComponent<GaleRoninSkill>() != null)
        {
#if UNITY_EDITOR
            var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player2/_pick up.png");
            if (sprites != null)
            {
                var list = new List<Sprite>();
                foreach (var o in sprites) if (o is Sprite s) list.Add(s);
                if (list.Count > 0)
                {
                    return list.OrderBy(s => s.rect.x).ToArray();
                }
            }
#endif
            var res = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player2/_pick up.png", 64, 64);
            if (res != null && res.Length > 0) return res;
        }
        else if (curHero.Contains("dune") || curHero.Contains("player5") || GetComponent<DuneAssassinSkill>() != null)
        {
#if UNITY_EDITOR
            var duneDeathDir = "Assets/ART/Player5/PNG/death";
            if (System.IO.Directory.Exists(duneDeathDir))
            {
                var frameFiles = System.IO.Directory.GetFiles(duneDeathDir, "death_*.png");
                System.Array.Sort(frameFiles, (a, b) => {
                    string fA = System.IO.Path.GetFileNameWithoutExtension(a);
                    string fB = System.IO.Path.GetFileNameWithoutExtension(b);
                    int nA = int.TryParse(fA.Replace("death_", ""), out int vA) ? vA : 0;
                    int nB = int.TryParse(fB.Replace("death_", ""), out int vB) ? vB : 0;
                    return nA.CompareTo(nB);
                });

                if (frameFiles.Length > 0)
                {
                    var frameList = new List<Sprite>();
                    foreach (var path in frameFiles)
                    {
                        var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace("\\", "/"));
                        if (s != null) frameList.Add(s);
                    }
                    if (frameList.Count > 0) return frameList.ToArray();
                }
            }
#endif
        }
        else if (curHero.Contains("mage") || curHero.Contains("player6") || GetComponent<CelestialMageSkill>() != null)
        {
            var mageDeathFrames = CelestialMageSkill.GetDeathFrames();
            if (mageDeathFrames != null && mageDeathFrames.Length > 0) return mageDeathFrames;
        }

        // Default: Player 1 (Vaelstrider)
#if UNITY_EDITOR
        var deathDir = "Assets/ART/Player/DEATH";
        if (System.IO.Directory.Exists(deathDir))
        {
            var frameFiles = System.IO.Directory.GetFiles(deathDir, "idle_down_*.png")
                .OrderBy(f => f)
                .ToArray();
            if (frameFiles.Length > 0)
            {
                var frameList = new List<Sprite>();
                foreach (var path in frameFiles)
                {
                    var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace("\\", "/"));
                    if (s != null) frameList.Add(s);
                }
                if (frameList.Count > 0) return frameList.ToArray();
            }
        }

        var p1Sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player/DEATH/player_death.png");
        if (p1Sprites != null)
        {
            var list = new List<Sprite>();
            foreach (var o in p1Sprites) if (o is Sprite s) list.Add(s);
            if (list.Count > 0)
            {
                return list.OrderBy(s => s.rect.x).ToArray();
            }
        }
#endif
        var p1Res = Resources.LoadAll<Sprite>("Player/player_death");
        if (p1Res != null && p1Res.Length > 0)
        {
            return p1Res.OrderBy(s => s.rect.x).ToArray();
        }

        return null;
    }

    private IEnumerator PlayKneelingDeathRoutine()
    {
        bool flipX = false;
        deathSprites = GetDeathSpritesForCurrentCharacter(out flipX);

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = flipX;
        }

        if (deathSprites != null && deathSprites.Length > 0)
        {
            // Tốc độ chuyển frame: 0.18s / frame để người chơi nhìn rõ từng giai đoạn gục ngã
            float frameTime = 0.18f;
            for (int i = 0; i < deathSprites.Length; i++)
            {
                if (spriteRenderer != null && deathSprites[i] != null)
                {
                    spriteRenderer.sprite = deathSprites[i];
                }
                yield return new WaitForSecondsRealtime(frameTime);
            }
        }
        else
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f, 0.85f);
            }
        }

        // Giữ lại khung hình nằm gục hoàn toàn trên sàn 1 giây để người chơi cảm nhận trọn vẹn kết cục
        yield return new WaitForSecondsRealtime(1.0f);

        // 4. Hien thi man hinh YOU DIED sau khi da hoan tat toan bo animation
        Debug.Log("<color=red>[GAMEOVER]</color> Hoat anh chet hoan tat. Goi GameOverUI.Show()...");
        GameOverUI.Show();
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    public void Revive()
    {
        isDead = false;
        currentHealth = maxHealth;
        PlayerPrefs.SetInt("Saved_CurrentHealth", currentHealth);
        PlayerPrefs.Save();

        // 1. Bat lai dieu khien & tan cong
        var pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = true;

        var combat = GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = true;

        var ronin = GetComponent<GaleRoninSkill>();
        if (ronin != null) ronin.enabled = true;

        var azure = GetComponent<AzureBladeSkill>();
        if (azure != null) azure.enabled = true;

        var titan = GetComponent<TitanBerserkerSkill>();
        if (titan != null) titan.enabled = true;

        var dune = GetComponent<DuneAssassinSkill>();
        if (dune != null) dune.enabled = true;

        GameAutoBootstrap.ApplySelectedCharacterToPlayer();

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        var anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = true;

        transform.rotation = Quaternion.identity;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.flipX = false;
        }

        // 2. Tao song xung kich hat vang quai xung quanh ra xa de tranh bi hoi dong
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 3.5f);
        foreach (var hit in hitColliders)
        {
            if (hit.gameObject != gameObject && hit.attachedRigidbody != null)
            {
                Vector2 pushDir = (hit.transform.position - transform.position).normalized;
                if (pushDir == Vector2.zero) pushDir = Vector2.up;
                hit.attachedRigidbody.AddForce(pushDir * 12f, ForceMode2D.Impulse);
            }
        }

        // 3. Bat khien bat tu tam thoi trong 2.5 giay
        StopAllCoroutines();
        StartCoroutine(ReviveInvulnerabilityRoutine());

        // 4. Hieu ung am thanh & Khoi phuc HUD dieu khien
        GameAutoBootstrap.SetGameplayHUDVisible(true);
        var shardsUI = FindAnyObjectByType<ShardsUI>();
        if (shardsUI != null)
        {
            shardsUI.RefreshDisplay();
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayShardPickup();
        }

        Debug.Log("<color=green>[PLAYER REVIVED]</color> Chien binh da duoc hoi sinh thanh cong voi 100% HP, toan bo HUD duoc khoi phuc va co khien bao ve tam thoi!");
    }

    private IEnumerator ReviveInvulnerabilityRoutine()
    {
        isInvulnerable = true;
        float duration = 2.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 1f, 1f, (Mathf.PingPong(elapsed * 8f, 1f) > 0.4f) ? 0.35f : 1f);
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        isInvulnerable = false;
    }
}
