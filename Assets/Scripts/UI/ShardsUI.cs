using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShardsUI : MonoBehaviour
{
    [Header("UI Component (Ho tro ca Text truyen thong va TextMeshPro)")]
    [SerializeField] private Text legacyText;
    [SerializeField] private TextMeshProUGUI tmpText;

    private PlayerInventory inventory;
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
        FindTextComponents();
    }

    void Start()
    {
        FindTextComponents();

        inventory = FindAnyObjectByType<PlayerInventory>();
        if (inventory != null)
        {
            inventory.OnShardsChanged += UpdateShardsDisplay;
            UpdateShardsDisplay(inventory.GetShards());
        }
    }

    private void FindTextComponents()
    {
        if (legacyText == null) legacyText = GetComponentInChildren<Text>();
        if (tmpText == null) tmpText = GetComponentInChildren<TextMeshProUGUI>();
    }

    void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnShardsChanged -= UpdateShardsDisplay;
        }
    }

    private void UpdateShardsDisplay(int amount)
    {
        FindTextComponents();

        string str = amount.ToString();

        if (legacyText != null)
        {
            legacyText.text = str;
        }

        if (tmpText != null)
        {
            tmpText.text = str;
        }

        Debug.Log($"<color=green>[UI UPDATED]</color> So tien Aethel Shards tren man hinh: {str}");

        StopAllCoroutines();
        StartCoroutine(PunchScaleRoutine());
    }

    private System.Collections.IEnumerator PunchScaleRoutine()
    {
        transform.localScale = originalScale * 1.35f;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t / 0.2f);
            yield return null;
        }
        transform.localScale = originalScale;
    }
}
