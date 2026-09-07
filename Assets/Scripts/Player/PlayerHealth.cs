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

        // Rung man hinh nhe
        var cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.12f, 0.15f);

        // Chop do
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

    private IEnumerator FlashRedRoutine()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
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
