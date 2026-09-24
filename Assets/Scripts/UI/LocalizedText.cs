using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tự động cập nhật text theo ngôn ngữ (Tiếng Việt / English) mỗi khi người chơi đổi ngôn ngữ trong Cài Đặt.
/// Áp dụng cho mọi TextMeshProUGUI và Text trong game.
/// </summary>
[DisallowMultipleComponent]
public class LocalizedText : MonoBehaviour
{
    [Header("Localization Key (Ưu tiên tra từ điển LocalizationManager)")]
    [SerializeField] private string localizationKey;

    [Header("Hoặc cấu hình trực tiếp 2 ngôn ngữ tại đây")]
    [TextArea(1, 4)]
    [SerializeField] private string vietnameseText;
    [TextArea(1, 4)]
    [SerializeField] private string englishText;

    private TextMeshProUGUI tmpText;
    private Text legacyText;

    public string Key
    {
        get => localizationKey;
        set { localizationKey = value; Refresh(); }
    }

    public void SetTexts(string vi, string en)
    {
        vietnameseText = vi;
        englishText = en;
        Refresh();
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        CacheComponents();
        LocalizationManager.OnLanguageChanged -= Refresh;
        LocalizationManager.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= Refresh;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= Refresh;
    }

    private void CacheComponents()
    {
        if (tmpText == null) tmpText = GetComponent<TextMeshProUGUI>();
        if (legacyText == null && tmpText == null) legacyText = GetComponent<Text>();
    }

    public void Refresh()
    {
        CacheComponents();

        string textToDisplay = "";

        if (!string.IsNullOrEmpty(localizationKey))
        {
            textToDisplay = LocalizationManager.Get(localizationKey);
        }
        else if (!string.IsNullOrEmpty(vietnameseText) || !string.IsNullOrEmpty(englishText))
        {
            textToDisplay = LocalizationManager.T(vietnameseText, englishText);
        }

        if (string.IsNullOrEmpty(textToDisplay)) return;

        if (tmpText != null)
        {
            tmpText.text = textToDisplay;
        }
        else if (legacyText != null)
        {
            legacyText.text = textToDisplay;
        }
    }
}
