using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
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
    private bool isPointerDown = false;
    private float pointerDownTime = 0f;
    private Vector2 pointerDownPos = Vector2.zero;
    private Vector2 currentDragAimDir = Vector2.zero;

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

        if (buttonType == ButtonType.Attack)
        {
            var azureBlade = (playerCombat != null) ? playerCombat.GetComponent<AzureBladeSkill>() : null;
            if (azureBlade != null)
            {
                float norm = azureBlade.AttackCooldownRatio;
                float rem = azureBlade.AttackCooldownRemaining;
                UpdateCooldownDisplay(norm, rem);

                if (isPointerDown)
                {
                    float holdDur = Time.time - pointerDownTime;
                    if (holdDur >= 0.10f)
                    {
                        Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) ? currentDragAimDir : azureBlade.GetFacingDirection();
                        azureBlade.UpdateAim(aimDir, holdDur);
                    }
                }
            }
            else
            {
                if (cooldownOverlay != null && cooldownOverlay.enabled) cooldownOverlay.enabled = false;
                if (cooldownText != null && cooldownText.enabled) cooldownText.enabled = false;
                if (legacyCooldownText != null && legacyCooldownText.enabled) legacyCooldownText.enabled = false;
            }
        }
        else if (buttonType == ButtonType.Skill)
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
        bool supportsCooldown = (buttonType == ButtonType.Skill || buttonType == ButtonType.Dash);
        if (buttonType == ButtonType.Attack && playerCombat != null && playerCombat.GetComponent<AzureBladeSkill>() != null)
        {
            supportsCooldown = true;
        }
        if (!supportsCooldown) return;

        Sprite circleSprite = Resources.Load<Sprite>("Combat/circle_fill");
#if UNITY_EDITOR
        if (circleSprite == null)
        {
            circleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/circle_fill.png");
        }
#endif

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

                if (circleSprite != null)
                {
                    img.sprite = circleSprite;
                }
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
            if (circleSprite != null && cooldownOverlay.sprite != circleSprite)
            {
                cooldownOverlay.sprite = circleSprite;
            }
        }

        if (cooldownOverlay != null)
        {
            cooldownOverlay.transform.SetAsLastSibling();
        }
        if (cooldownText != null)
        {
            if (!cooldownText.gameObject.activeSelf)
                cooldownText.gameObject.SetActive(true);
            cooldownText.transform.SetAsLastSibling();
        }

        if (legacyCooldownText != null)
        {
            if (!legacyCooldownText.gameObject.activeSelf)
                legacyCooldownText.gameObject.SetActive(true);
            legacyCooldownText.transform.SetAsLastSibling();
        }
    }

    private void EnsureRoundedSprites()
    {
        // 1. Khung viền (Frame Background)
        Sprite frameSprite = null;
#if UNITY_EDITOR
        frameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/potion_slot_circle.png");
        if (frameSprite == null) frameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/circle_fill.png");
#endif
        if (frameSprite == null) frameSprite = Resources.Load<Sprite>("UI/potion_slot_circle");

        Image btnImg = GetComponent<Image>();
        if (btnImg != null && frameSprite != null)
        {
            if (btnImg.sprite != frameSprite)
            {
                btnImg.sprite = frameSprite;
                btnImg.color = new Color(0.9f, 0.95f, 1f, 0.95f);
            }
        }

        // 2. Icon vẽ tay ở giữa (Attack / Dash / Ulti)
        Sprite actionSprite = null;
#if UNITY_EDITOR
        if (buttonType == ButtonType.Attack)
        {
            actionSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Attack.png");
        }
        else if (buttonType == ButtonType.Dash)
        {
            actionSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Dash.png");
        }
        else if (buttonType == ButtonType.Skill)
        {
            actionSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Ulti.png");
        }
#endif

        Transform actionIconT = transform.Find("ActionIcon");
        Image actionIconImg = null;
        if (actionIconT == null)
        {
            GameObject iconGo = new GameObject("ActionIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(transform, false);
            iconGo.transform.SetSiblingIndex(0);

            RectTransform rt = iconGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.18f, 0.18f);
            rt.anchorMax = new Vector2(0.82f, 0.82f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            actionIconImg = iconGo.GetComponent<Image>();
            actionIconImg.raycastTarget = false;
            actionIconImg.preserveAspect = true;
        }
        else
        {
            actionIconImg = actionIconT.GetComponent<Image>();
        }

        if (actionIconImg != null && actionSprite != null)
        {
            actionIconImg.sprite = actionSprite;
            actionIconImg.color = Color.white;
            actionIconImg.preserveAspect = true;
            if (!actionIconImg.gameObject.activeSelf) actionIconImg.gameObject.SetActive(true);
        }

        // 3. Tắt icon lốc xoáy cũ
        Transform iconT = transform.Find("SkillIcon");
        if (iconT != null)
        {
            iconT.gameObject.SetActive(false);
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

        if (buttonType == ButtonType.Attack)
        {
            var azureBlade = (playerCombat != null) ? playerCombat.GetComponent<AzureBladeSkill>() : null;
            if (azureBlade != null && !azureBlade.IsAttackReady)
            {
                StartCoroutine(ShakeButtonRoutine());
                return;
            }

            isPointerDown = true;
            pointerDownTime = Time.time;
            pointerDownPos = eventData.position;
            currentDragAimDir = Vector2.zero;

            if (azureBlade != null)
            {
                azureBlade.StartAim(azureBlade.GetFacingDirection());
            }
        }
        else
        {
            ExecuteAction();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (buttonType == ButtonType.Attack && isPointerDown)
        {
            Vector2 delta = eventData.position - pointerDownPos;
            if (delta.sqrMagnitude > 200f) // Kéo ngắm vượt qua vùng chết
            {
                currentDragAimDir = delta.normalized;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = originalScale;

        if (buttonType == ButtonType.Attack && isPointerDown)
        {
            EnsureTargets();
            float holdDur = Time.time - pointerDownTime;
            var azureBlade = (playerCombat != null) ? playerCombat.GetComponent<AzureBladeSkill>() : null;

            if (azureBlade != null && (holdDur >= 0.10f || currentDragAimDir.sqrMagnitude > 0.01f))
            {
                int bonusAtk = (playerCombat != null) ? playerCombat.BonusAttackDamage : 0;
                azureBlade.ReleaseAimAndFire(bonusAtk);
            }
            else if (playerCombat != null)
            {
                playerCombat.PerformAttack();
            }

            isPointerDown = false;
            currentDragAimDir = Vector2.zero;
        }
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
            if (cooldownOverlay.sprite == null)
            {
                Sprite circle = Resources.Load<Sprite>("Combat/circle_fill");
#if UNITY_EDITOR
                if (circle == null) circle = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/circle_fill.png");
#endif
                if (circle != null) cooldownOverlay.sprite = circle;
            }
        }
        if (cooldownText != null)
        {
            if (!cooldownText.gameObject.activeSelf) cooldownText.gameObject.SetActive(true);
            cooldownText.enabled = false;
        }
    }
}
