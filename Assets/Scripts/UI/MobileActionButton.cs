using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType { Attack, Dash, Skill }
    [Header("Loại nút")]
    [SerializeField] private ButtonType buttonType = ButtonType.Attack;

    [Header("Gán trực tiếp Player vào đây (hoặc để tự tìm)")]
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerController playerController;

    [Header("Hiển thị Cooldown (Dành cho Skill)")]
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private Text legacyCooldownText;

    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
        EnsureTargets();
        EnsureRoundedSprites();

        if (cooldownOverlay == null)
        {
            Transform cdT = transform.Find("CooldownOverlay");
            if (cdT != null) cooldownOverlay = cdT.GetComponent<Image>();
        }

        if (cooldownText == null)
        {
            Transform txtT = transform.Find("CooldownText");
            if (txtT != null)
            {
                cooldownText = txtT.GetComponent<TextMeshProUGUI>();
                if (legacyCooldownText == null) legacyCooldownText = txtT.GetComponent<Text>();
            }
        }

        // Đảm bảo GameObject luôn Active, chỉ dùng component.enabled để ẩn/hiện
        if (cooldownOverlay != null)
        {
            if (!cooldownOverlay.gameObject.activeSelf)
                cooldownOverlay.gameObject.SetActive(true);
            cooldownOverlay.enabled = false;
        }

        if (cooldownText != null)
        {
            if (!cooldownText.gameObject.activeSelf)
                cooldownText.gameObject.SetActive(true);
            cooldownText.enabled = false;
        }

        if (legacyCooldownText != null)
        {
            if (!legacyCooldownText.gameObject.activeSelf)
                legacyCooldownText.gameObject.SetActive(true);
            legacyCooldownText.enabled = false;
        }
    }

    void Start()
    {
        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null && !graphic.raycastTarget)
        {
            graphic.raycastTarget = true;
        }
    }

    void Update()
    {
        if (buttonType == ButtonType.Skill)
        {
            EnsureTargets();
        EnsureRoundedSprites();
            if (playerCombat != null)
            {
                float norm = playerCombat.GetSkillCooldownNormalized();
                float rem = playerCombat.GetSkillCooldownRemaining();

                bool onCooldown = (rem > 0.05f);

                if (cooldownOverlay != null)
                {
                    cooldownOverlay.fillAmount = norm;
                    cooldownOverlay.enabled = onCooldown;
                }

                if (onCooldown)
                {
                    string timeStr = (rem >= 1f) ? Mathf.CeilToInt(rem).ToString() : rem.ToString("F1");
                    
                    if (cooldownText != null)
                    {
                        cooldownText.text = timeStr;
                        cooldownText.enabled = true;
                    }

                    if (legacyCooldownText != null)
                    {
                        legacyCooldownText.text = timeStr;
                        legacyCooldownText.enabled = true;
                    }
                }
                else
                {
                    if (cooldownText != null)
                    {
                        cooldownText.enabled = false;
                    }

                    if (legacyCooldownText != null)
                    {
                        legacyCooldownText.enabled = false;
                    }
                }
            }
        }
    }

    private void EnsureRoundedSprites()
    {
        Image btnImg = GetComponent<Image>();
        if (btnImg != null)
        {
            btnImg.color = Color.white; // Loại bỏ màu xám m_Color 0.6f làm xỉn nút

            Sprite roundSprite = null;
#if UNITY_EDITOR
            if (buttonType == ButtonType.Attack)
            {
                roundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/atk.png");
            }
            else if (buttonType == ButtonType.Dash)
            {
                roundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/dash.png");
            }
            else if (buttonType == ButtonType.Skill)
            {
                roundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/ulti.png");
            }
#endif
            if (roundSprite != null)
            {
                btnImg.sprite = roundSprite;
            }
        }

        Transform iconT = transform.Find("SkillIcon");
        if (iconT != null)
        {
            Image iconImg = iconT.GetComponent<Image>();
            if (iconImg != null)
            {
                iconImg.color = Color.white;
#if UNITY_EDITOR
                Sprite ultiSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/ulti.png");
                if (ultiSprite != null) iconImg.sprite = ultiSprite;
#endif
            }
        }
    }

    private void EnsureTargets()
    {
        if (playerCombat == null) playerCombat = FindAnyObjectByType<PlayerCombat>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
    }

    // Nhấn xuống lập tức kích hoạt hành động ngay tức khắc (chuẩn game hành động mobile)
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = originalScale * 0.85f;
        ExecuteAction();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }

    private void ExecuteAction()
    {
        EnsureTargets();
        EnsureRoundedSprites();

        if (buttonType == ButtonType.Attack)
        {
            if (playerCombat != null)
            {
                playerCombat.PerformAttack();
            }
        }
        else if (buttonType == ButtonType.Dash)
        {
            if (playerController != null)
            {
                playerController.PerformDash();
            }
        }
        else if (buttonType == ButtonType.Skill)
        {
            if (playerCombat != null)
            {
                if (playerCombat.IsSkillReady())
                {
                    playerCombat.PerformSpecialSkill();
                }
                else
                {
                    StartCoroutine(ShakeButtonRoutine());
                }
            }
        }
    }

    private System.Collections.IEnumerator ShakeButtonRoutine()
    {
        Vector3 origPos = transform.localPosition;
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float x = origPos.x + Random.Range(-4f, 4f);
            transform.localPosition = new Vector3(x, origPos.y, origPos.z);
            yield return null;
        }
        transform.localPosition = origPos;
    }

    public void SetCooldownComponents(Image overlay, TextMeshProUGUI text)
    {
        cooldownOverlay = overlay;
        cooldownText = text;
        if (cooldownOverlay != null)
        {
            if (!cooldownOverlay.gameObject.activeSelf) cooldownOverlay.gameObject.SetActive(true);
            cooldownOverlay.enabled = false;
        }
        if (cooldownText != null)
        {
            if (!cooldownText.gameObject.activeSelf) cooldownText.gameObject.SetActive(true);
            cooldownText.enabled = false;
        }
    }
}
