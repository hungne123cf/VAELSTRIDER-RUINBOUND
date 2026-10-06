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

    private const float HOLD_AIM_THRESHOLD = 0.45f;

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
                    if (holdDur >= HOLD_AIM_THRESHOLD || currentDragAimDir.sqrMagnitude > 0.01f)
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

                if (isPointerDown)
                {
                    float holdDur = Time.time - pointerDownTime;
                    if (holdDur >= HOLD_AIM_THRESHOLD || currentDragAimDir.sqrMagnitude > 0.01f)
                    {
                        Vector2 playerPos = (playerCombat != null) ? (Vector2)playerCombat.transform.position : Vector2.zero;
                        Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) ? currentDragAimDir : ((playerController != null) ? playerController.GetFacingDirection() : Vector2.down);
                        
                        var ronin = (playerCombat != null) ? playerCombat.GetComponent<GaleRoninSkill>() : null;
                        if (ronin != null && ronin.enabled)
                        {
                            SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimArc(playerPos, aimDir, 1.10f, 180f, new Color(0.2f, 0.9f, 0.7f, 0.85f));
                        }
                        else if (playerCombat != null)
                        {
                            SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimArc(playerPos, aimDir, 1.05f, 120f, new Color(1f, 0.85f, 0.2f, 0.85f));
                        }
                    }
                }
            }
        }
        else if (buttonType == ButtonType.Skill)
        {
            if (playerCombat != null)
            {
                float norm = playerCombat.GetSkillCooldownNormalized();
                float rem = playerCombat.GetSkillCooldownRemaining();
                UpdateCooldownDisplay(norm, rem);

                if (isPointerDown && playerCombat.IsSkillReady())
                {
                    float holdDur = Time.time - pointerDownTime;
                    if (holdDur >= HOLD_AIM_THRESHOLD || currentDragAimDir.sqrMagnitude > 0.01f)
                    {
                        Vector2 playerPos = (Vector2)playerCombat.transform.position;
                        Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) ? currentDragAimDir : ((playerController != null) ? playerController.GetFacingDirection() : Vector2.down);

                        var ronin = playerCombat.GetComponent<GaleRoninSkill>();
                        if (ronin != null && ronin.enabled)
                        {
                            ronin.FindBestTarget(3.5f, out IDamageable targetDmg);
                            Transform targetT = (targetDmg != null) ? (targetDmg as Component)?.transform : null;
                            SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowTargetLockOn(
                                playerPos, 
                                3.5f, 
                                targetT, 
                                new Color(0.2f, 0.85f, 0.75f, 0.45f), 
                                (targetT != null) ? new Color(1f, 0.25f, 0.25f, 0.95f) : new Color(0.6f, 0.6f, 0.6f, 0.4f)
                            );
                        }
                        else
                        {
                            var azure = playerCombat.GetComponent<AzureBladeSkill>();
                            if (azure != null && azure.enabled)
                            {
                                SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimFan(playerPos, aimDir, 60f, 5, 6.5f, new Color(0.15f, 0.75f, 1f, 0.9f));
                            }
                            else
                            {
                                SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimCircle(playerPos, 2.4f, new Color(1f, 0.75f, 0.2f, 0.85f));
                            }
                        }
                    }
                }
            }
        }
        else if (buttonType == ButtonType.Dash)
        {
            if (playerController != null)
            {
                float norm = playerController.GetDashCooldownNormalized();
                float rem = playerController.GetDashCooldownRemaining();
                UpdateCooldownDisplay(norm, rem);

                if (isPointerDown && playerController.IsDashReady())
                {
                    float holdDur = Time.time - pointerDownTime;
                    if (holdDur >= HOLD_AIM_THRESHOLD || currentDragAimDir.sqrMagnitude > 0.01f)
                    {
                        Vector2 playerPos = (Vector2)playerController.transform.position;
                        Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) ? currentDragAimDir : playerController.GetFacingDirection();
                        SkillAimIndicator.EnsureExists(playerController.gameObject)?.ShowAimLine(playerPos, aimDir, 3.8f, new Color(0.2f, 0.85f, 1f, 0.85f));
                    }
                }
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

        // Kiểm tra điều kiện hồi chiêu trước khi cho phép giữ/bấm
        if (buttonType == ButtonType.Attack)
        {
            var azureBlade = (playerCombat != null) ? playerCombat.GetComponent<AzureBladeSkill>() : null;
            if (azureBlade != null && !azureBlade.IsAttackReady)
            {
                StartCoroutine(ShakeButtonRoutine());
                return;
            }
        }
        else if (buttonType == ButtonType.Dash)
        {
            if (playerController != null && !playerController.IsDashReady())
            {
                StartCoroutine(ShakeButtonRoutine());
                return;
            }
        }
        else if (buttonType == ButtonType.Skill)
        {
            if (playerCombat != null && !playerCombat.IsSkillReady())
            {
                StartCoroutine(ShakeButtonRoutine());
                return;
            }
        }

        isPointerDown = true;
        pointerDownTime = Time.time;
        pointerDownPos = eventData.position;
        currentDragAimDir = Vector2.zero;

        if (buttonType == ButtonType.Attack)
        {
            var azureBlade = (playerCombat != null) ? playerCombat.GetComponent<AzureBladeSkill>() : null;
            if (azureBlade != null)
            {
                azureBlade.StartAim(azureBlade.GetFacingDirection());
            }
        }

        SkillAimIndicator.EnsureExists((playerCombat != null) ? playerCombat.gameObject : playerController?.gameObject);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPointerDown)
        {
            Vector2 delta = eventData.position - pointerDownPos;
            if (delta.sqrMagnitude > 150f) // Kéo ngắm vượt qua vùng chết
            {
                currentDragAimDir = delta.normalized;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = originalScale;

        if (!isPointerDown) return;
        isPointerDown = false;

        if (SkillAimIndicator.Instance != null)
        {
            SkillAimIndicator.Instance.Hide();
        }

        EnsureTargets();
        float holdDur = Time.time - pointerDownTime;
        Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) 
            ? currentDragAimDir 
            : ((playerController != null) ? playerController.GetFacingDirection() : Vector2.down);

        if (buttonType == ButtonType.Attack)
        {
            var azureBlade = (playerCombat != null) ? playerCombat.GetComponent<AzureBladeSkill>() : null;
            if (azureBlade != null && (holdDur >= HOLD_AIM_THRESHOLD || currentDragAimDir.sqrMagnitude > 0.01f))
            {
                int bonusAtk = (playerCombat != null) ? playerCombat.BonusAttackDamage : 0;
                azureBlade.ReleaseAimAndFire(bonusAtk);
            }
            else if (playerCombat != null)
            {
                if (currentDragAimDir.sqrMagnitude > 0.01f)
                {
                    playerCombat.PerformAttackInDirection(currentDragAimDir);
                }
                else
                {
                    playerCombat.PerformAttack();
                }
            }
        }
        else if (buttonType == ButtonType.Dash)
        {
            if (playerController != null)
            {
                if (playerController.IsDashReady())
                {
                    if (currentDragAimDir.sqrMagnitude > 0.01f)
                    {
                        playerController.PerformDashInDirection(currentDragAimDir);
                    }
                    else
                    {
                        playerController.PerformDash();
                    }
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
                    if (currentDragAimDir.sqrMagnitude > 0.01f && playerController != null)
                    {
                        playerController.SetFacingDirection(currentDragAimDir);
                    }

                    // Đối với Gale Ronin: Kiểm tra bắt buộc phải có mục tiêu trong tầm 10.5m
                    var ronin = playerCombat.GetComponent<GaleRoninSkill>();
                    if (ronin != null && ronin.enabled)
                    {
                        var target = ronin.FindBestTarget(10.5f, out _);
                        if (target == null)
                        {
                            StartCoroutine(ShakeButtonRoutine());
                            DamagePopup.CreateText(
                                playerCombat.transform.position + Vector3.up * 1.2f, 
                                LocalizationManager.T("Không có mục tiêu trong tầm!", "No target in range!"), 
                                new Color(1f, 0.35f, 0.35f)
                            );
                            currentDragAimDir = Vector2.zero;
                            return;
                        }
                    }

                    bool success = playerCombat.PerformSpecialSkill();
                    if (!success)
                    {
                        StartCoroutine(ShakeButtonRoutine());
                    }
                }
                else
                {
                    StartCoroutine(ShakeButtonRoutine());
                }
            }
        }

        currentDragAimDir = Vector2.zero;
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
