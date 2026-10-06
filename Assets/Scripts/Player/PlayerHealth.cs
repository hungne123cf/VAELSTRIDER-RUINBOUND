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

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDead = false;
    public bool isInvulnerable = false;

    public static Vector3 LastDeathPosition { get; private set; }
    public static string LastDeathScene { get; private set; }

    void Awake()
    {
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
        int upHp = upgradeMgr != null ? upgradeMgr.GetHealthBonus() : 0;
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

        int oldMax = maxHealth;
        maxHealth = baseMaxHealth + upHp + armorHp + weaponHp + sockHp;

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

    void Update()
    {
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

        // Giảm sát thương theo Giáp của trang bị đang mặc + Khảm ngọc (tối đa giảm 65% để người chơi luôn nhận sát thương thực tế)
        ArmorShopUI.GetEquippedArmorBonus(out int _, out float armorDef, out float _);
        EquipmentSocketManager.GetActiveEquippedSocketBonus(out _, out _, out int sockDef, out _, out _, out _, out _);
        float totalDef = Mathf.Min(65f, armorDef + sockDef);
        if (totalDef > 0)
        {
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - totalDef / 100f)));
        }

        // 1. Kiểm tra và hấp thụ sát thương bằng Lớp Giáp Ảo Hộ Thân nếu đang kích hoạt
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

        // Kích hoạt Hurt animation trên Animator
        var anim = GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.SetTrigger("HurtTrigger");
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
        if (combat != null) combat.enabled = false;

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

        if (curHero.Contains("azure") || curHero.Contains("player3") || GetComponent<AzureBladeSkill>() != null)
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
                if (list.Count > 0) return list.OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).Take(5).ToArray();
            }
#endif
        }

        return Resources.LoadAll<Sprite>("Player/player_death");
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
            float frameTime = 0.12f;
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

        // Giữ lại khung hình nằm gục trên sàn một khoảng thời gian rõ ràng để người chơi thấy hoạt ảnh
        yield return new WaitForSecondsRealtime(0.75f);

        // 4. Hien thi man hinh YOU DIED
        Debug.Log("<color=red>[GAMEOVER]</color> Goi GameOverUI.Show()...");
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
