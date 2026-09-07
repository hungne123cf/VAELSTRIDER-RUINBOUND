using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Player Stats")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"<color=orange>[PLAYER HURT]</color> Ban bi quai danh trung! Mat {damage} HP. Con lai: {currentHealth}/{maxHealth}");

        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.12f, 0.15f);

        if (spriteRenderer != null)
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
        spriteRenderer.color = originalColor;
    }

    private IEnumerator FlashColorRoutine(Color color)
    {
        spriteRenderer.color = color;
        yield return new WaitForSeconds(0.18f);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("<color=red>[PLAYER DIED]</color> Ban da bi quai ha guc!");
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
}
