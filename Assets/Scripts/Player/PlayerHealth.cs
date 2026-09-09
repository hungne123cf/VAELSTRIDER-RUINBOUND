using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Player Stats")]
    [SerializeField] private int maxHealth = 100;
    private Sprite[] deathSprites;
    private int currentHealth;
    private int baseMaxHealth = 100;
    private int bonusMaxHealth = 0;

    public void ApplyUpgradeBonus(int bonusHp)
    {
        int oldBonus = bonusMaxHealth;
        bonusMaxHealth = bonusHp;
        maxHealth = baseMaxHealth + bonusMaxHealth;
        int diff = bonusMaxHealth - oldBonus;
        if (diff > 0)
        {
            currentHealth = Mathf.Min(currentHealth + diff, maxHealth);
            PlayerPrefs.SetInt("Saved_CurrentHealth", currentHealth);
            PlayerPrefs.Save();
        }
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
        int savedHp = PlayerPrefs.GetInt("Saved_CurrentHealth", maxHealth);
        if (savedHp <= 0) savedHp = maxHealth;
        currentHealth = Mathf.Min(savedHp, maxHealth);
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        // Auto-load death sprites from Resources/Player/player_death if not set
        if (deathSprites == null || deathSprites.Length == 0)
        {
            deathSprites = Resources.LoadAll<Sprite>("Player/player_death");
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damage;
        PlayerPrefs.SetInt("Saved_CurrentHealth", Mathf.Max(0, currentHealth));
        PlayerPrefs.Save();
        SoundManager.EnsureInstance()?.PlayPlayerHurt();
        Debug.Log($"<color=orange>[PLAYER HURT]</color> Ban bi quai danh trung! Mat {damage} HP. Con lai: {currentHealth}/{maxHealth}");

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.12f, 0.15f);

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
        Debug.Log("<color=red>[PLAYER DIED]</color> Nhan vat guc nga! Phat animation quy xuong chet...");

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

        // Vo hieu hoa Animator de tranh lap lai animation Run / Idle
        var anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        // Vo hieu hoa Collider de khong con va cham nua
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 2. Rung manh camera
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.35f, 0.45f);

        // 3. Giu dung goc nhan vat (khong xoay 90 do) & phat animation quy xuong
        transform.rotation = Quaternion.identity;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        StartCoroutine(PlayKneelingDeathRoutine());
    }

    private IEnumerator PlayKneelingDeathRoutine()
    {
        // Load fallback neu chua co
        if (deathSprites == null || deathSprites.Length == 0)
        {
            deathSprites = Resources.LoadAll<Sprite>("Player/player_death");
        }

        if (deathSprites != null && deathSprites.Length > 0)
        {
            float frameTime = 0.10f;
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
            yield return new WaitForSecondsRealtime(0.4f);
        }

        // Nghi 0.2s truoc khi bung man hinh GameOver
        yield return new WaitForSecondsRealtime(0.2f);

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

        // 4. Hieu ung am thanh
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayShardPickup();
        }

        Debug.Log("<color=green>[PLAYER REVIVED]</color> Chien binh da duoc hoi sinh thanh cong voi 100% HP va khien bao ve tam thoi!");
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
