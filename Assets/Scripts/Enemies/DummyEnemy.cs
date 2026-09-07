using UnityEngine;

public class DummyEnemy : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale;

    void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        originalScale = transform.localScale;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"<color=yellow>[DUMMY STATUS]</color> {gameObject.name} mat {damage} mau! Mau con lai: {currentHealth}/{maxHealth}");

        // Hieu ung chop chop va rung nhe khi trung don
        StopAllCoroutines();
        StartCoroutine(HitFeedbackRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator HitFeedbackRoutine()
    {
        if (spriteRenderer != null) spriteRenderer.color = Color.red;
        transform.localScale = originalScale * 1.25f; // Phong to nhe tao cam giac trung don

        yield return new WaitForSeconds(0.12f);

        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        transform.localScale = originalScale;
    }

    private void Die()
    {
        Debug.Log($"<color=red>[DUMMY DIED]</color> {gameObject.name} da bi tieu diet hoan toan!");
        Destroy(gameObject);
    }
}
