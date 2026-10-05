using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShardsUI : MonoBehaviour
{
    [Header("UI Component (Hỗ trợ cả Text truyền thống và TextMeshPro)")]
    [SerializeField] private Text legacyText;
    [SerializeField] private TextMeshProUGUI tmpText;

    private PlayerInventory inventory;
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
        FindTextComponents();
    }

    void OnEnable()
    {
        FindTextComponents();
        RefreshDisplay();
    }

    void Start()
    {
        FindTextComponents();
        AccountDatabase.EnsureActiveSessionLoaded();
        HookInventory();
        RefreshDisplay();
    }

    private void FindTextComponents()
    {
        if (legacyText == null) legacyText = GetComponentInChildren<Text>();
        if (tmpText == null) tmpText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void HookInventory()
    {
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<PlayerInventory>();
            if (inventory == null)
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null)
                {
                    inventory = pc.GetComponent<PlayerInventory>();
                    if (inventory == null) inventory = pc.gameObject.AddComponent<PlayerInventory>();
                }
            }
        }

        if (inventory != null)
        {
            inventory.OnShardsChanged -= UpdateShardsDisplay;
            inventory.OnShardsChanged += UpdateShardsDisplay;
        }
    }

    public void RefreshDisplay()
    {
        HookInventory();
        long amount = (inventory != null) ? inventory.GetShards() : AccountDatabase.GetAethelShards();
        SetDisplayText(amount);
    }

    void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnShardsChanged -= UpdateShardsDisplay;
        }
    }

    private void SetDisplayText(long amount)
    {
        string str = (amount >= 1_000_000_000) ? (amount / 1_000_000_000.0).ToString("0.##") + "B" :
                     (amount >= 1_000_000) ? (amount / 1_000_000.0).ToString("0.##") + "M" :
                     (amount >= 10_000) ? (amount / 1_000.0).ToString("0.#") + "K" :
                     amount.ToString("N0");

        if (legacyText != null)
        {
            legacyText.text = str;
            legacyText.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        if (tmpText != null)
        {
            tmpText.text = str;
            tmpText.enableWordWrapping = false;
            tmpText.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private void UpdateShardsDisplay(long amount)
    {
        FindTextComponents();
        SetDisplayText(amount);

        // Chỉ chạy hiệu ứng khi GameObject đang active, tránh crash coroutine
        if (gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(PunchScaleRoutine());
        }
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
