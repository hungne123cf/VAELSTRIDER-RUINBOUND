using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Thanh máu (Slider)")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider easeHpSlider; // Thanh vàng tụt chậm tạo hiệu ứng game xịn
    [SerializeField] private Text hpText;

    [Header("Tốc độ tụt mượt mà")]
    [SerializeField] private float easeSpeed = 0.05f;

    private PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (hpSlider == null) hpSlider = GetComponent<Slider>();

        if (playerHealth != null)
        {
            int max = playerHealth.GetMaxHealth();
            if (hpSlider != null)
            {
                hpSlider.maxValue = max;
                hpSlider.value = playerHealth.GetCurrentHealth();
            }
            if (easeHpSlider != null)
            {
                easeHpSlider.maxValue = max;
                easeHpSlider.value = playerHealth.GetCurrentHealth();
            }
            UpdateText(playerHealth.GetCurrentHealth(), max);
        }
    }

    void Update()
    {
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            return;
        }

        int currentHP = playerHealth.GetCurrentHealth();
        int maxHP = playerHealth.GetMaxHealth();

        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }

        // Thanh vang duoi theo thanh do tao do tre dep mat
        if (easeHpSlider != null)
        {
            if (easeHpSlider.value != hpSlider.value)
            {
                easeHpSlider.value = Mathf.Lerp(easeHpSlider.value, currentHP, easeSpeed);
            }
        }

        UpdateText(currentHP, maxHP);
    }

    private void UpdateText(int current, int max)
    {
        if (hpText != null)
        {
            hpText.text = $"{current} / {max}";
        }
    }
}
