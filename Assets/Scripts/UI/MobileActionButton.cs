using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType { Attack, Dash, Skill }
    [Header("Loai nut")]
    [SerializeField] private ButtonType buttonType = ButtonType.Attack;

    [Header("Gan truc tiep Player vao day (hoac de tu tim)")]
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerController playerController;

    [Header("Hien thi Cooldown (Danh cho Skill va Dash)")]
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private Text legacyCooldownText;

    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
        EnsureTargets();
        EnsureRoundedSprites();
        EnsureCooldownVisuals();
    }

    void Start()
    {
        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null && !graphic.raycastTarget)
        {
            graphic.raycastTarget = true;
        }
        EnsureCooldownVisuals();
    }

    void Update()
    {
        EnsureTargets();
        EnsureRoundedSprites();
        EnsureCooldownVisuals();

        if (buttonType == ButtonType.Skill)
        {
            if (playerCombat != null)
            {
                float norm = playerCombat.GetSkillCooldownNormalized();
                float rem = playerCombat.GetSkillCooldownRemaining();
                UpdateCooldownDisplay(norm, rem);
            }
        }
        else if (buttonType == ButtonType.Dash)
        {
            if (playerController != null)
            {
                float norm = playerController.GetDashCooldownNormalized();
                float rem = playerController.GetDashCooldownRemaining();
                UpdateCooldownDisplay(norm, rem);
            }
        }
    }

    private void UpdateCooldownDisplay(float norm, float rem)
    {
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

    private void EnsureCooldownVisuals()
    {
        if (buttonType != ButtonType.Skill && buttonType != ButtonType.Dash) return;

        if (cooldownOverlay == null)
        {
            Transform cdT = transform.Find("CooldownOverlay");
            if (cdT != null)
            {
                cooldownOverlay = cdT.GetComponent<Image>();
            }
            else
            {
                GameObject overlayGo = new GameObject("CooldownOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                overlayGo.transform.SetParent(transform, false);
                RectTransform rt = overlayGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image img = overlayGo.GetComponent<Image>();
                img.raycastTarget = false;
                img.color = new Color(0.04f, 0.05f, 0.08f, 0.78f);
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillClockwise = false;
                img.fillOrigin = (int)Image.Origin360.Top;

#if UNITY_EDITOR
                Sprite circleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/circle_fill.png");
                if (circleSprite != null)
                {
                    img.sprite = circleSprite;
                }
#endif
                cooldownOverlay = img;
            }
        }

        if (cooldownText == null)
        {
            Transform txtT = transform.Find("CooldownText");
            if (txtT != null)
            {
                cooldownText = txtT.GetComponent<TextMeshProUGUI>();
                if (legacyCooldownText == null) legacyCooldownText = txtT.GetComponent<Text>();
            }
            else
            {
                GameObject txtGo = new GameObject("CooldownText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(transform, false);
                RectTransform txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;

                TextMeshProUGUI tmp = txtGo.GetComponent<TextMeshProUGUI>();
                tmp.raycastTarget = false;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 22;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(1f, 0.88f, 0.25f, 1f);

                TextMeshProUGUI[] tmps = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
                foreach (var t in tmps)
                {
                    if (t != null && t.font != null)
                    {
                        tmp.font = t.font;
                        break;
                    }
                }

                cooldownText = tmp;
            }
        }

        if (cooldownOverlay != null)
        {
            if (!cooldownOverlay.gameObject.activeSelf)
                cooldownOverlay.gameObject.SetActive(true);
            if (cooldownOverlay.fillMethod != Image.FillMethod.Radial360)
            {
                cooldownOverlay.type = Image.Type.Filled;
                cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
                cooldownOverlay.fillClockwise = false;
                cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
            }
        }

        if (cooldownText != null)
        {
            if (!cooldownText.gameObject.activeSelf)
                cooldownText.gameObject.SetActive(true);
        }

        if (legacyCooldownText != null)
        {
            if (!legacyCooldownText.gameObject.activeSelf)
                legacyCooldownText.gameObject.SetActive(true);
        }
    }

    private void EnsureRoundedSprites()
    {
        Image btnImg = GetComponent<Image>();
        if (btnImg != null)
        {
            btnImg.color = Color.white;

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

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = originalScale * 0.85f;
        EnsureTargets();
        if (playerCombat != null)
        {
            playerCombat.BlockAttackThisFrame();
        }
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
                if (playerController.IsDashReady())
                {
                    playerController.PerformDash();
                }
                else
                {
                    StartCoroutine(ShakeButtonRoutine());
                }
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
            elapsed += Time.unscaledDeltaTime;
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
