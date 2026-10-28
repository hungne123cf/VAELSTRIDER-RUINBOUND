using System.Collections.Generic;
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
    private PlayerHealth playerHealth;

    [Header("Hien thi Cooldown (Danh cho Skill va Dash)")]
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private Text legacyCooldownText;
    // === Dash Charge Pip UI (3 dots, modern style) ===
    private Image[] chargePips;          // 3 pip images
    private float[] pipPulseTimer;       // per-pip pulse animation time
    private int lastKnownCharges = -1;

    // === Stun Lock Visuals ===
    private GameObject lockOverlayObj;
    private Image lockIconImg;

    private Vector3 originalScale;
    private Vector3 baseScale = Vector3.zero;
    private CanvasGroup canvasGroup;
    private bool isPointerDown = false;
    private float pointerDownTime = 0f;
    private Vector2 pointerDownPos = Vector2.zero;
    private Vector2 currentDragAimDir = Vector2.zero;

    void Awake()
    {
        cachedHeroActionIcons.Clear();
        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
            if (baseScale == Vector3.zero) baseScale = Vector3.one;
        }
        originalScale = baseScale;
        EnsureTargets();
        EnsureRoundedSprites();
        EnsureCooldownVisuals();
        EnsureLockVisuals();
        ApplyCustomPreferences();
    }

    void Start()
    {
        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null && !graphic.raycastTarget)
        {
            graphic.raycastTarget = true;
        }
        EnsureCooldownVisuals();
        EnsureLockVisuals();
        ApplyCustomPreferences();
    }

    public void ApplyCustomPreferences()
    {
        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
            if (baseScale == Vector3.zero) baseScale = Vector3.one;
        }

        float scaleMod = PlayerPrefs.GetFloat("Game_Controls_ButtonScale", 1.0f);
        float opacity = PlayerPrefs.GetFloat("Game_Controls_Opacity", 1.0f);

        originalScale = baseScale * scaleMod;
        transform.localScale = isPointerDown ? (originalScale * 0.85f) : originalScale;

        string posXKey = null;
        string posYKey = null;
        float defaultX = 0f;
        float defaultY = 0f;

        if (buttonType == ButtonType.Attack || gameObject.name == "Btn_Attack")
        {
            posXKey = "Game_Controls_Atk_PosX";
            posYKey = "Game_Controls_Atk_PosY";
            defaultX = -220f;
            defaultY = 220f;
        }
        else if (buttonType == ButtonType.Dash || gameObject.name == "Btn_Dash")
        {
            posXKey = "Game_Controls_Dash_PosX";
            posYKey = "Game_Controls_Dash_PosY";
            defaultX = -380f;
            defaultY = 160f;
        }
        else if (buttonType == ButtonType.Skill || gameObject.name == "Btn_Special" || gameObject.name == "Btn_Skill")
        {
            posXKey = "Game_Controls_Skill_PosX";
            posYKey = "Game_Controls_Skill_PosY";
            defaultX = -350f;
            defaultY = 290f;
        }

        if (posXKey != null && PlayerPrefs.HasKey(posXKey))
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                float px = PlayerPrefs.GetFloat(posXKey, defaultX);
                float py = PlayerPrefs.GetFloat(posYKey, defaultY);
                rt.anchoredPosition = new Vector2(px, py);
            }
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = opacity;
    }

    public static void ApplyToAllButtons()
    {
        var buttons = Object.FindObjectsByType<MobileActionButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in buttons)
        {
            if (b != null) b.ApplyCustomPreferences();
        }
    }

    private const float HOLD_AIM_THRESHOLD = 0.45f;

    void Update()
    {
        EnsureTargets();
        if (playerHealth != null && playerHealth.IsDead)
        {
            isPointerDown = false;
            return;
        }

        EnsureRoundedSprites();
        EnsureCooldownVisuals();
        EnsureLockVisuals();

        bool isPlayerStunned = (playerController != null && playerController.IsStunned);
        if (lockOverlayObj != null)
        {
            if (lockOverlayObj.activeSelf != isPlayerStunned)
            {
                lockOverlayObj.SetActive(isPlayerStunned);
            }
            if (isPlayerStunned && lockIconImg != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
                lockIconImg.transform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }

        if (buttonType == ButtonType.Attack)
        {
            var titan = (playerCombat != null) ? playerCombat.GetComponent<TitanBerserkerSkill>() : null;
            var azureBlade = (playerCombat != null) ? playerCombat.GetComponent<AzureBladeSkill>() : null;
            var ronin = (playerCombat != null) ? playerCombat.GetComponent<GaleRoninSkill>() : null;
            var dune = (playerCombat != null) ? playerCombat.GetComponent<DuneAssassinSkill>() : null;
            var mage = (playerCombat != null) ? playerCombat.GetComponent<CelestialMageSkill>() : null;

            if (mage != null && mage.enabled)
            {
                float norm = mage.AttackCooldownRatio;
                float rem = mage.AttackCooldownRemaining;
                UpdateCooldownDisplay(norm, rem);

                if (isPointerDown)
                {
                    float holdDur = Time.time - pointerDownTime;
                    Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) ? currentDragAimDir : ((playerController != null) ? playerController.GetFacingDirection() : Vector2.down);
                    if (holdDur >= 0.15f || currentDragAimDir.sqrMagnitude > 0.01f)
                    {
                        if (playerController != null) playerController.SetFacingDirection(aimDir);
                        if (!mage.IsChargingBarrier && mage.IsAttackReady)
                        {
                            mage.StartBarrierCharge(aimDir);
                        }
                        mage.UpdateBarrierCharge(aimDir, holdDur);

                        Vector2 playerPos = (Vector2)playerCombat.transform.position;
                        float novaRadius = 2.6f + Mathf.Clamp01(holdDur / mage.maxChargeDuration) * 1.6f;
                        SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimCircle(playerPos, novaRadius, new Color(0.85f, 0.35f, 1f, 0.85f));
                        SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
                    }
                }
            }
            else if (dune != null && dune.enabled)
            {
                if (isPointerDown)
                {
                    float holdDur = Time.time - pointerDownTime;
                    Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) ? currentDragAimDir : ((playerController != null) ? playerController.GetFacingDirection() : Vector2.down);
                    if (holdDur >= 0.15f || currentDragAimDir.sqrMagnitude > 0.01f)
                    {
                        if (!dune.IsAimingTornado && dune.IsAttackReady)
                        {
                            dune.StartTornadoAim(aimDir);
                        }
                        dune.UpdateTornadoAim(aimDir, holdDur);
                        SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
                    }
                }
            }
            else if (titan != null && titan.enabled)
            {
                float norm = (titan.ChargeCooldownRemaining > 0.05f) ? titan.ChargeCooldownRatio : 0f;
                float rem = titan.ChargeCooldownRemaining;
                UpdateCooldownDisplay(norm, rem);

                if (isPointerDown)
                {
                    float holdDur = Time.time - pointerDownTime;
                    Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) ? currentDragAimDir : ((playerController != null) ? playerController.GetFacingDirection() : Vector2.down);
                    
                    if (titan.CanUseEarthshakerCharge && (holdDur >= 0.15f || currentDragAimDir.sqrMagnitude > 0.01f))
                    {
                        if (!titan.IsChargingEarthshaker && titan.IsAttackReady)
                        {
                            titan.StartEarthshakerCharge(aimDir);
                        }
                        titan.UpdateEarthshakerCharge(aimDir, holdDur);
                        if (holdDur >= HOLD_AIM_THRESHOLD || currentDragAimDir.sqrMagnitude > 0.01f)
                        {
                            SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
                        }
                    }
                    else if (!titan.CanUseEarthshakerCharge)
                    {
                        if (holdDur >= HOLD_AIM_THRESHOLD || currentDragAimDir.sqrMagnitude > 0.01f)
                        {
                            Vector2 playerPos = (Vector2)playerCombat.transform.position;
                            if (playerController != null) playerController.SetFacingDirection(aimDir);
                            SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimArc(playerPos, aimDir, 1.4f, 120f, new Color(1f, 0.85f, 0.2f, 0.85f));
                            SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
                        }
                    }
                }
            }
            else if (azureBlade != null && azureBlade.enabled)
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
                        SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
                    }
                }
            }
            else if (ronin != null && ronin.enabled)
            {
                float norm = ronin.AttackCooldownRatio;
                float rem = ronin.AttackCooldownRemaining;
                UpdateCooldownDisplay(norm, rem);

                if (isPointerDown && ronin.IsAttackReady)
                {
                    float holdDur = Time.time - pointerDownTime;
                    Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) ? currentDragAimDir : ((playerController != null) ? playerController.GetFacingDirection() : Vector2.down);

                    if (ronin.CanUseIaidoCharge)
                    {
                        ronin.UpdateIaidoCharge(aimDir, holdDur);
                        SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
                    }
                    else
                    {
                        if (holdDur >= HOLD_AIM_THRESHOLD || currentDragAimDir.sqrMagnitude > 0.01f)
                        {
                            Vector2 playerPos = (Vector2)playerCombat.transform.position;
                            SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimArc(playerPos, aimDir, ronin.SlashRadius, 180f, new Color(0.15f, 0.95f, 0.85f, 0.85f));
                            SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
                        }
                    }
                }
            }
            else
            {
                // Player 1: Vaelstrider (Thế Thủ Kiếm Thuẫn & Sóng Kiếm Hoàng Kim)
                if (playerCombat != null)
                {
                    float norm = (playerCombat.ParryCooldownRemaining > 0.05f) ? playerCombat.ParryCooldownRatio : 0f;
                    float rem = playerCombat.ParryCooldownRemaining;
                    UpdateCooldownDisplay(norm, rem);

                    if (isPointerDown)
                    {
                        float holdDur = Time.time - pointerDownTime;
                        Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) ? currentDragAimDir : ((playerController != null) ? playerController.GetFacingDirection() : Vector2.down);

                        if (playerCombat.CanUseParryCharge && (holdDur >= 0.15f || currentDragAimDir.sqrMagnitude > 0.01f))
                        {
                            if (!playerCombat.IsParrying)
                            {
                                playerCombat.StartParryStance(aimDir);
                            }
                            playerCombat.UpdateParryStance(aimDir, holdDur);
                            if (holdDur >= HOLD_AIM_THRESHOLD || currentDragAimDir.sqrMagnitude > 0.01f)
                            {
                                SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
                            }
                        }
                        else if (!playerCombat.CanUseParryCharge)
                        {
                            if (holdDur >= HOLD_AIM_THRESHOLD || currentDragAimDir.sqrMagnitude > 0.01f)
                            {
                                Vector2 playerPos = (Vector2)playerCombat.transform.position;
                                if (playerController != null) playerController.SetFacingDirection(aimDir);
                                SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimArc(playerPos, aimDir, 1.35f, 120f, new Color(1f, 0.85f, 0.2f, 0.85f));
                                SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
                            }
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

                        var titan = playerCombat.GetComponent<TitanBerserkerSkill>();
                        var ronin = playerCombat.GetComponent<GaleRoninSkill>();
                        var azure = playerCombat.GetComponent<AzureBladeSkill>();
                        var dune = playerCombat.GetComponent<DuneAssassinSkill>();
                        var mage = playerCombat.GetComponent<CelestialMageSkill>();

                        if (mage != null && mage.enabled)
                        {
                            SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimCircle(playerPos, 5.0f, new Color(0.85f, 0.35f, 1f, 0.85f));
                        }
                        else if (dune != null && dune.enabled)
                        {
                            SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimCircle(playerPos, dune.ultimateRadius, new Color(1f, 0.80f, 0.20f, 0.85f));
                        }
                        else if (titan != null && titan.enabled)
                        {
                            SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimCircle(playerPos, titan.ultimateRadius, new Color(1f, 0.45f, 0.1f, 0.85f));
                        }
                        else if (ronin != null && ronin.enabled)
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
                        else if (azure != null && azure.enabled)
                        {
                            SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimFan(playerPos, aimDir, 60f, 5, 6.5f, new Color(0.15f, 0.75f, 1f, 0.9f));
                        }
                        else
                        {
                            SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimCircle(playerPos, 2.4f, new Color(1f, 0.75f, 0.2f, 0.85f));
                        }
                        SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
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

                if (playerController.GetMaxDashCharges() > 1)
                {
                    EnsureChargePips(playerController.GetMaxDashCharges());
                    UpdateChargePips(playerController.GetCurrentDashCharges(),
                                     playerController.GetDashChargeRechargeProgress());
                }
                else
                {
                    HideChargePips();
                }

                if (isPointerDown && playerController.IsDashReady())
                {
                    float holdDur = Time.time - pointerDownTime;
                    if (holdDur >= HOLD_AIM_THRESHOLD || currentDragAimDir.sqrMagnitude > 0.01f)
                    {
                        Vector2 playerPos = (Vector2)playerController.transform.position;
                        Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) ? currentDragAimDir : playerController.GetFacingDirection();
                        SkillAimIndicator.EnsureExists(playerController.gameObject)?.ShowAimLine(playerPos, aimDir, 3.8f, new Color(0.2f, 0.85f, 1f, 0.85f));
                        SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
                    }
                }
            }
        }
    }

    private void UpdateCooldownDisplay(float norm, float rem)
    {
        bool onCooldown = (rem > 0.05f);

        if (cooldownOverlay == null && onCooldown)
        {
            EnsureCooldownVisuals();
        }

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
        bool supportsCooldown = (buttonType == ButtonType.Skill || buttonType == ButtonType.Dash || buttonType == ButtonType.Attack);
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

    private void EnsureLockVisuals()
    {
        if (lockOverlayObj == null)
        {
            Transform existing = transform.Find("StunLockOverlay");
            if (existing != null)
            {
                lockOverlayObj = existing.gameObject;
                lockIconImg = existing.Find("LockIcon")?.GetComponent<Image>();
            }
            else
            {
                lockOverlayObj = new GameObject("StunLockOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                lockOverlayObj.transform.SetParent(transform, false);
                RectTransform rt = lockOverlayObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image bg = lockOverlayObj.GetComponent<Image>();
                bg.raycastTarget = false;
                bg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);
                Sprite circleSprite = Resources.Load<Sprite>("Combat/circle_fill");
#if UNITY_EDITOR
                if (circleSprite == null) circleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/circle_fill.png");
#endif
                if (circleSprite != null) bg.sprite = circleSprite;

                GameObject iconGo = new GameObject("LockIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconGo.transform.SetParent(lockOverlayObj.transform, false);
                RectTransform iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.sizeDelta = new Vector2(36f, 36f);
                iconRt.anchoredPosition = Vector2.zero;

                lockIconImg = iconGo.GetComponent<Image>();
                lockIconImg.raycastTarget = false;
                lockIconImg.sprite = WhiteIconLoader.GetSprite("Locker-Locked") ?? WhiteIconLoader.GetSprite("lock");
                lockIconImg.color = new Color(1f, 0.85f, 0.25f, 0.95f); // Gold lock

                lockOverlayObj.SetActive(false);
            }
        }

        if (lockOverlayObj != null)
        {
            lockOverlayObj.transform.SetAsLastSibling();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Pip / Dot charge display (Genshin / Valorant style)
    // ─────────────────────────────────────────────────────────────
    private static readonly Color PIP_LIT   = new Color(0.30f, 0.95f, 1.00f, 1.00f); // cyan glow
    private static readonly Color PIP_DIM   = new Color(0.15f, 0.25f, 0.30f, 0.75f); // dark empty
    private static readonly Color PIP_PULSE = new Color(0.55f, 1.00f, 0.70f, 1.00f); // green flash on refill

    private void EnsureChargePips(int maxCharges)
    {
        if (chargePips != null && chargePips.Length == maxCharges) return;

        // Destroy old pip container if any
        Transform oldCon = transform.Find("ChargePipContainer");
        if (oldCon != null) Destroy(oldCon.gameObject);

        chargePips      = new Image[maxCharges];
        pipPulseTimer   = new float[maxCharges];
        lastKnownCharges = -1;

        // Container: row of pips, anchored bottom-center of the button
        GameObject con = new GameObject("ChargePipContainer", typeof(RectTransform));
        con.transform.SetParent(transform, false);
        RectTransform conRt = con.GetComponent<RectTransform>();
        conRt.anchorMin  = new Vector2(0f, -0.32f);
        conRt.anchorMax  = new Vector2(1f, -0.05f);
        conRt.offsetMin  = Vector2.zero;
        conRt.offsetMax  = Vector2.zero;

        float spacing  = 1f / maxCharges;
        float pipW     = Mathf.Min(0.22f, spacing * 0.68f);

        for (int i = 0; i < maxCharges; i++)
        {
            GameObject pipGo = new GameObject("Pip" + i,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pipGo.transform.SetParent(con.transform, false);

            float centerX = spacing * i + spacing * 0.5f;
            RectTransform rt = pipGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(centerX - pipW * 0.5f, 0.05f);
            rt.anchorMax = new Vector2(centerX + pipW * 0.5f, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = pipGo.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = PIP_LIT;

            // Try to load a circle sprite (same as cooldown)
            Sprite circ = Resources.Load<Sprite>("Combat/circle_fill");
#if UNITY_EDITOR
            if (circ == null)
                circ = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/circle_fill.png");
#endif
            if (circ != null) img.sprite = circ;

            chargePips[i] = img;
        }
        con.transform.SetAsLastSibling();
    }

    private void UpdateChargePips(int currentCharges, float rechargeProgress)
    {
        if (chargePips == null) return;

        int maxCharges = chargePips.Length;
        bool chargesJustRestored = (currentCharges > lastKnownCharges && lastKnownCharges >= 0);

        for (int i = 0; i < maxCharges; i++)
        {
            if (chargePips[i] == null) continue;

            bool pipLit = (i < currentCharges);

            // Pulse animation: the next-to-recharge pip (first dim one) flashes while recharging
            bool isRechargingPip = (!pipLit && i == currentCharges && currentCharges < maxCharges);

            if (chargesJustRestored && i == currentCharges - 1)
            {
                // Flash the newly restored pip
                pipPulseTimer[i] = 0.45f;
            }

            if (pipPulseTimer[i] > 0f)
            {
                pipPulseTimer[i] -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(pipPulseTimer[i] / 0.45f);
                chargePips[i].color = Color.Lerp(PIP_PULSE, PIP_LIT, t);
                float s = Mathf.Lerp(1.35f, 1f, t);
                chargePips[i].transform.localScale = new Vector3(s, s, 1f);
            }
            else if (pipLit)
            {
                // Lit pip: solid cyan, gentle breathe
                float breathe = 0.05f * Mathf.Sin(Time.time * 3.5f + i * 1.1f);
                Color c = PIP_LIT;
                c.r = Mathf.Clamp01(c.r + breathe);
                c.g = Mathf.Clamp01(c.g + breathe);
                c.b = Mathf.Clamp01(c.b + breathe);
                chargePips[i].color = c;
                chargePips[i].transform.localScale = Vector3.one;
            }
            else if (isRechargingPip)
            {
                // Recharging pip: pulse dim->brighter based on rechargeProgress
                float pulse = Mathf.Lerp(0f, 0.8f, rechargeProgress);
                float flicker = 0.12f * Mathf.Abs(Mathf.Sin(Time.time * 6f));
                Color c = Color.Lerp(PIP_DIM, PIP_LIT, pulse + flicker);
                chargePips[i].color = c;
                float s = 0.75f + 0.25f * rechargeProgress;
                chargePips[i].transform.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                // Empty pip: dark
                chargePips[i].color = PIP_DIM;
                chargePips[i].transform.localScale = Vector3.one * 0.75f;
            }
        }

        lastKnownCharges = currentCharges;
    }

    private void HideChargePips()
    {
        if (chargePips == null) return;
        foreach (var p in chargePips)
            if (p != null) p.gameObject.SetActive(false);
    }

    private static Dictionary<string, Sprite> cachedHeroActionIcons = new Dictionary<string, Sprite>();
    private string lastThemedHero = "";

    private void EnsureRoundedSprites()
    {
        string curHero = "vael";
        var cm = CharacterManager.EnsureInstance();
        if (cm != null)
        {
            curHero = cm.GetSelectedCharacterId().ToLower();
        }

        bool isRonin = (curHero.Contains("ronin") || curHero.Contains("gale") || curHero.Contains("maid"));
        bool isAzure = (curHero.Contains("azure") || curHero.Contains("player3"));
        bool isDune = (curHero.Contains("dune") || curHero.Contains("player5"));
        bool isMage = (curHero.Contains("mage") || curHero.Contains("celestial") || curHero.Contains("player6"));

        // Dọn dẹp ObsidianCoreBg cũ nếu có để tránh che khuất Icon
        Transform oldCore = transform.Find("ObsidianCoreBg");
        if (oldCore != null)
        {
            Destroy(oldCore.gameObject);
        }

        // 1. Nền đĩa đá Obsidian đen huyền ở gốc nút
        Image btnImg = GetComponent<Image>();
        Sprite coreSprite = SoulslikeHUDStyler.GetActionButtonCoreSprite();
        if (btnImg != null && coreSprite != null)
        {
            btnImg.sprite = coreSprite;
            btnImg.color = Color.white;
        }

        // 2. Icon chuyên biệt cho từng nhân vật (Hiển thị ngay trên nền Obsidian)
        Sprite actionSprite = GetHeroActionIcon(curHero, buttonType);
        if (actionSprite == null)
        {
            if (buttonType == ButtonType.Attack) actionSprite = WhiteIconLoader.GetSprite("Sword");
            else if (buttonType == ButtonType.Dash) actionSprite = WhiteIconLoader.GetSprite("Dash") ?? WhiteIconLoader.GetSprite("Footsteps");
            else actionSprite = WhiteIconLoader.GetSprite("Star") ?? WhiteIconLoader.GetSprite("Flame");
        }

        Transform actionIconT = transform.Find("ActionIcon");
        Image actionIconImg = null;
        if (actionIconT == null)
        {
            GameObject iconGo = new GameObject("ActionIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(transform, false);

            RectTransform rt = iconGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.18f, 0.18f);
            rt.anchorMax = new Vector2(0.82f, 0.82f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            actionIconImg = iconGo.GetComponent<Image>();
            actionIconImg.raycastTarget = false;
            actionIconImg.preserveAspect = true;
            actionIconT = iconGo.transform;
        }
        else
        {
            actionIconImg = actionIconT.GetComponent<Image>();
            RectTransform rt = actionIconT.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.18f, 0.18f);
                rt.anchorMax = new Vector2(0.82f, 0.82f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        if (actionIconImg != null)
        {
            actionIconImg.sprite = actionSprite;
            actionIconImg.color = Color.white;
            actionIconImg.preserveAspect = true;
            if (!actionIconImg.gameObject.activeSelf) actionIconImg.gameObject.SetActive(true);
        }

        // Đảm bảo ActionIcon nằm ngay trên nền (Sibling index 0)
        actionIconT.SetSiblingIndex(0);

        // 3. Khung viền đồng cổ rèn đúc Soulslike Runic Ring (Nằm phía trên viền ngoài icon)
        Transform ringT = transform.Find("RunicRingFrame");
        Image ringImg = null;
        if (ringT == null)
        {
            GameObject ringGo = new GameObject("RunicRingFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ringGo.transform.SetParent(transform, false);

            RectTransform rRt = ringGo.GetComponent<RectTransform>();
            rRt.anchorMin = Vector2.zero;
            rRt.anchorMax = Vector2.one;
            rRt.offsetMin = Vector2.zero;
            rRt.offsetMax = Vector2.zero;

            ringImg = ringGo.GetComponent<Image>();
            ringImg.sprite = SoulslikeHUDStyler.GetActionButtonRingSprite();
            ringImg.color = Color.white;
            ringImg.raycastTarget = false;
            ringT = ringGo.transform;
        }
        else
        {
            ringImg = ringT.GetComponent<Image>();
            if (ringImg != null)
            {
                ringImg.sprite = SoulslikeHUDStyler.GetActionButtonRingSprite();
                ringImg.color = Color.white;
            }
        }

        // Đảm bảo Khung viền Runic Ring nằm ngay sau ActionIcon (Sibling index 1)
        ringT.SetSiblingIndex(1);

        // Đảm bảo CooldownOverlay nằm trên cùng của Icon và Ring
        if (cooldownOverlay != null)
        {
            cooldownOverlay.transform.SetAsLastSibling();
        }

        // Đảm bảo CooldownText hiển thị rõ ràng trên CooldownOverlay
        if (cooldownText != null)
        {
            cooldownText.transform.SetAsLastSibling();
            if (isMage) cooldownText.color = new Color(0.9f, 0.5f, 1f, 1f);
            else if (isAzure) cooldownText.color = new Color(0.6f, 0.95f, 1f, 1f);
            else if (isRonin) cooldownText.color = new Color(0.4f, 1f, 0.8f, 1f);
            else if (isDune) cooldownText.color = new Color(1f, 0.85f, 0.35f, 1f);
            else cooldownText.color = new Color(1f, 0.9f, 0.35f, 1f);
        }

        // Tắt icon lốc xoáy cũ nếu có
        Transform iconT = transform.Find("SkillIcon");
        if (iconT != null)
        {
            iconT.gameObject.SetActive(false);
        }
    }

    public static Sprite GetHeroActionIcon(string heroId, ButtonType btnType)
    {
        string rawHero = heroId ?? "";
        string lowerHero = rawHero.ToLowerInvariant();
        string key = $"{lowerHero}_{btnType}";
        if (cachedHeroActionIcons.TryGetValue(key, out Sprite cached) && cached != null)
        {
            return cached;
        }

        bool isTitan = (lowerHero.Contains("titan") || lowerHero.Contains("player4"));
        bool isRonin = (lowerHero.Contains("ronin") || lowerHero.Contains("gale") || lowerHero.Contains("maid") || lowerHero.Contains("player2"));
        bool isAzure = (lowerHero.Contains("azure") || lowerHero.Contains("player3"));
        bool isDune = (lowerHero.Contains("dune") || lowerHero.Contains("player5") || lowerHero.Contains("assassin"));
        bool isMage = (lowerHero.Contains("mage") || lowerHero.Contains("player6") || lowerHero.Contains("celestial"));

        Sprite result = null;

        if (isMage)
        {
            // Player 6: Celestial Mage (Cầu Ma Pháp, Tốc Biến Hư Không, Mưa Thiên Thạch)
            result = LoadPlayer6CustomIcon(btnType);
            if (result == null)
            {
                if (btnType == ButtonType.Attack)
                    result = GenerateIcon_ArcaneOrbs();
                else if (btnType == ButtonType.Dash)
                    result = GenerateIcon_ArcaneBlink();
                else
                    result = GenerateIcon_MeteorApocalypse();
            }
        }
        else if (isDune)
        {
            // Player 5: Dune Assassin (Song Đoản Đao Sa Mạc, Tốc Lướt Ảo Ảnh, Cuồng Bão Sa Ảnh X-Rend)
            result = LoadPlayer5CustomIcon(btnType);
            if (result == null)
            {
                if (btnType == ButtonType.Attack)
                    result = GenerateIcon_DuneDaggers();
                else if (btnType == ButtonType.Dash)
                    result = GenerateIcon_SandDash();
                else
                    result = GenerateIcon_SandstormXRend();
            }
        }
        else if (isTitan)
        {
            // Player 4: Titan Berserker (War Hammer, Juggernaut Rush, Cataclysm Volcanic Slam)
            result = LoadPlayer4CustomIcon(btnType);
            if (result == null)
            {
                if (btnType == ButtonType.Attack)
                    result = GenerateIcon_RuinSword();
                else if (btnType == ButtonType.Dash)
                    result = GenerateIcon_RuinDash();
                else
                    result = GenerateIcon_RuinWhirlwind();
            }
        }
        else if (isAzure)
        {
            // Player 3: Azure Blade (Độc quyền bộ icon vẽ tay: Lam Nguyệt Sóng Kiếm, Băng Trượt Tốc Lướt, Nan Quạt 5 Tia Kiếm Khí)
            result = LoadPlayer3CustomIcon(btnType);
            if (result == null)
            {
                if (btnType == ButtonType.Attack)
                    result = GenerateIcon_TwinBlades();
                else if (btnType == ButtonType.Dash)
                    result = GenerateIcon_FrostDash();
                else
                    result = GenerateIcon_CrescentWaves();
            }
        }
        else if (isRonin)
        {
            // Player 2: Gale Ronin (Độc quyền bộ icon vẽ tay: Trảm Nguyệt Phong Ma, Tốc Lướt Phong Thiểm, Phân Thân Phong Ma Tuyệt Kỹ)
            result = LoadPlayer2CustomIcon(btnType);
            if (result == null)
            {
                if (btnType == ButtonType.Attack)
                    result = GenerateIcon_KatanaSlash();
                else if (btnType == ButtonType.Dash)
                    result = GenerateIcon_GaleDash();
                else
                    result = GenerateIcon_StormCleave();
            }
        }
        else
        {
            // Player 1: Vaelstrider (Đại Kiếm Hoàng Kim)
            result = LoadPlayer1CustomIcon(btnType);
            if (result == null)
            {
                if (btnType == ButtonType.Attack)
                    result = GenerateIcon_RuinSword();
                else if (btnType == ButtonType.Dash)
                    result = GenerateIcon_RuinDash();
                else
                    result = GenerateIcon_RuinWhirlwind();
            }
        }

        if (result != null)
        {
            cachedHeroActionIcons[key] = result;
        }
        return result;
    }

    private static Sprite LoadPlayer1CustomIcon(ButtonType btnType)
    {
        string fileName = (btnType == ButtonType.Attack) ? "icon_attack" : ((btnType == ButtonType.Dash) ? "icon_dash" : "icon_ulti");
        Sprite loaded = null;
#if UNITY_EDITOR
        string path = $"Assets/ART/Player/UI/{fileName}.png";
        loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (loaded == null)
        {
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in all)
            {
                if (a is Sprite s) { loaded = s; break; }
            }
        }
        if (loaded == null)
        {
            path = $"Assets/ART/Player1/UI/{fileName}.png";
            loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (loaded == null)
            {
                var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in all)
                {
                    if (a is Sprite s) { loaded = s; break; }
                }
            }
        }
#endif
        if (loaded == null)
        {
            loaded = Resources.Load<Sprite>($"Player/UI/{fileName}");
        }
        if (loaded == null)
        {
            loaded = Resources.Load<Sprite>($"Player1/UI/{fileName}");
        }
        return loaded;
    }

    private static Sprite LoadPlayer4CustomIcon(ButtonType btnType)
    {
        string fileName = (btnType == ButtonType.Attack) ? "icon_attack" : ((btnType == ButtonType.Dash) ? "icon_dash" : "icon_ulti");
        Sprite loaded = null;
#if UNITY_EDITOR
        string path = $"Assets/ART/Player4/UI/{fileName}.png";
        loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (loaded == null)
        {
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in all)
            {
                if (a is Sprite s) { loaded = s; break; }
            }
        }
#endif
        if (loaded == null)
        {
            loaded = Resources.Load<Sprite>($"Player4/UI/{fileName}");
        }
        return loaded;
    }

    private static Sprite LoadPlayer2CustomIcon(ButtonType btnType)
    {
        string fileName = (btnType == ButtonType.Attack) ? "icon_attack" : ((btnType == ButtonType.Dash) ? "icon_dash" : "icon_ulti");
        Sprite loaded = null;
#if UNITY_EDITOR
        string path = $"Assets/ART/Player2/UI/{fileName}.png";
        loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (loaded == null)
        {
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in all)
            {
                if (a is Sprite s) { loaded = s; break; }
            }
        }
#endif
        if (loaded == null)
        {
            loaded = Resources.Load<Sprite>($"Player2/UI/{fileName}");
        }
        return loaded;
    }

    private static Sprite LoadPlayer3CustomIcon(ButtonType btnType)
    {
        string fileName = (btnType == ButtonType.Attack) ? "icon_attack" : ((btnType == ButtonType.Dash) ? "icon_dash" : "icon_ulti");
        Sprite loaded = null;
#if UNITY_EDITOR
        string path = $"Assets/ART/Player3/UI/{fileName}.png";
        loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (loaded == null)
        {
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in all)
            {
                if (a is Sprite s) { loaded = s; break; }
            }
        }
#endif
        if (loaded == null)
        {
            loaded = Resources.Load<Sprite>($"Player3/UI/{fileName}");
        }
        return loaded;
    }

    private static Sprite LoadPlayer5CustomIcon(ButtonType btnType)
    {
        string fileName = (btnType == ButtonType.Attack) ? "icon_attack" : ((btnType == ButtonType.Dash) ? "icon_dash" : "icon_ulti");
        Sprite loaded = null;
#if UNITY_EDITOR
        string path = $"Assets/ART/Player5/UI/{fileName}.png";
        loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (loaded == null)
        {
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in all)
            {
                if (a is Sprite s) { loaded = s; break; }
            }
        }
#endif
        if (loaded == null)
        {
            loaded = Resources.Load<Sprite>($"Player5/UI/{fileName}");
        }
        return loaded;
    }

    private static Sprite LoadPlayer6CustomIcon(ButtonType btnType)
    {
        if (btnType == ButtonType.Attack)
        {
#if UNITY_EDITOR
            var attackIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Player6/VFX/NovaBurst/frame0001.png");
            if (attackIcon != null) return attackIcon;
#endif
            var direct = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player6/VFX/NovaBurst/frame0001.png", 64, 64);
            if (direct != null && direct.Length > 0) return direct[0];
        }

        string fileName = (btnType == ButtonType.Attack) ? "icon_attack" : ((btnType == ButtonType.Dash) ? "icon_dash" : "icon_ulti");
        Sprite loaded = null;
#if UNITY_EDITOR
        string path = $"Assets/ART/Player6/UI/{fileName}.png";
        loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (loaded == null)
        {
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in all)
            {
                if (a is Sprite s) { loaded = s; break; }
            }
        }
#endif
        if (loaded == null)
        {
            loaded = Resources.Load<Sprite>($"Player6/UI/{fileName}");
        }
        return loaded;
    }

    // =========================================================================
    // HỆ THỐNG VẼ ICON PIXEL-VECTOR CHUẨN ĐỘT PHÁ CHO TỪNG TƯỚNG
    // =========================================================================

    private static Sprite GenerateIcon_RuinSword()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color gold = new Color(1f, 0.85f, 0.25f, 1f);
        Color blade = new Color(1f, 0.98f, 0.85f, 1f);
        Color hilt = new Color(0.8f, 0.55f, 0.15f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Kiếm lớn đặt chéo từ dưới trái lên trên phải
                float distDiag = Mathf.Abs(x - y) / 1.414f;
                float progress = (x + y) / (2f * size);

                if (distDiag <= 4f && progress >= 0.35f && progress <= 0.88f)
                {
                    tex.SetPixel(x, y, distDiag <= 1.5f ? blade : gold);
                }
                else if (progress >= 0.28f && progress <= 0.35f && distDiag <= 10f)
                {
                    tex.SetPixel(x, y, hilt); // Chuôi kiếm
                }
                else if (progress >= 0.12f && progress < 0.28f && distDiag <= 2.5f)
                {
                    tex.SetPixel(x, y, hilt); // Cán kiếm
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_RuinDash()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color gold = new Color(1f, 0.85f, 0.25f, 0.95f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 3 vệt lướt tốc độ hình mũi tên kép hoàng kim
                float dy1 = Mathf.Abs(y - size * 0.5f);
                float dy2 = Mathf.Abs(y - size * 0.32f);
                float dy3 = Mathf.Abs(y - size * 0.68f);

                bool line1 = (dy1 <= 2.5f && x >= 14 && x <= 46);
                bool line2 = (dy2 <= 2.0f && x >= 22 && x <= 50);
                bool line3 = (dy3 <= 2.0f && x >= 22 && x <= 50);
                bool arrow = (x >= 42 && x <= 54 && Mathf.Abs(y - size * 0.5f) <= (54 - x) * 1.5f);

                if (line1 || line2 || line3 || arrow)
                    tex.SetPixel(x, y, gold);
                else
                    tex.SetPixel(x, y, clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_RuinWhirlwind()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color gold = new Color(1f, 0.88f, 0.3f, 1f);
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float d = Vector2.Distance(pos, center);
                float angle = Mathf.Atan2(y - center.y, x - center.x);

                // 3 cánh lốc xoáy 360 độ
                float spiral = Mathf.Sin(angle * 3f + d * 0.28f);
                if (d >= 6f && d <= 26f && spiral > 0.45f)
                {
                    float alpha = Mathf.Clamp01((spiral - 0.45f) * 3f);
                    tex.SetPixel(x, y, new Color(gold.r, gold.g, gold.b, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_KatanaSlash()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color emerald = new Color(0.15f, 1f, 0.75f, 1f);
        Color white = new Color(0.9f, 1f, 0.95f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Vòng chém bán nguyệt Iaido cong vút
                float normX = (x - size * 0.5f) / (size * 0.45f);
                float normY = (y - size * 0.5f) / (size * 0.45f);
                float distCurve = Mathf.Abs(normY - (normX * normX * 0.6f - 0.3f));

                if (distCurve < 0.18f && x >= 10 && x <= 54 && y >= 12 && y <= 52)
                {
                    tex.SetPixel(x, y, distCurve < 0.08f ? white : emerald);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_GaleDash()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color emerald = new Color(0.2f, 1f, 0.75f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 3 nhát chém lướt gió song song sắc lẹm
                float d1 = Mathf.Abs((x - y * 0.6f) - 12f);
                float d2 = Mathf.Abs((x - y * 0.6f) - 22f);
                float d3 = Mathf.Abs((x - y * 0.6f) - 32f);

                if ((d1 <= 2.2f && y >= 14 && y <= 44) ||
                    (d2 <= 2.8f && y >= 10 && y <= 54) ||
                    (d3 <= 2.2f && y >= 20 && y <= 50))
                {
                    tex.SetPixel(x, y, emerald);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_StormCleave()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color mint = new Color(0.3f, 1f, 0.85f, 1f);
        Color core = new Color(0.95f, 1f, 0.98f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Nhát chém chữ X bão phong trảm rực lửa sấm sét
                float d1 = Mathf.Abs(x - y) / 1.414f;
                float d2 = Mathf.Abs(x + y - size) / 1.414f;

                if ((d1 <= 3.5f || d2 <= 3.5f) && x >= 12 && x <= 52 && y >= 12 && y <= 52)
                {
                    bool isCenter = (d1 <= 1.4f || d2 <= 1.4f);
                    tex.SetPixel(x, y, isCenter ? core : mint);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_TwinBlades()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color cyan = new Color(0.25f, 0.85f, 1f, 1f);
        Color ice = new Color(0.9f, 0.98f, 1f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Song kiếm bắt chéo hình chữ X sắc bén tinh xảo
                float d1 = Mathf.Abs(x - y * 0.9f - 4f) / 1.414f;
                float d2 = Mathf.Abs(x + y * 0.9f - size - 4f) / 1.414f;

                if ((d1 <= 3.2f || d2 <= 3.2f) && x >= 14 && x <= 50 && y >= 10 && y <= 54)
                {
                    bool isCore = (d1 <= 1.2f || d2 <= 1.2f);
                    tex.SetPixel(x, y, isCore ? ice : cyan);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_FrostDash()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color cyan = new Color(0.3f, 0.9f, 1f, 1f);
        Color white = new Color(1f, 1f, 1f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Đôi cánh lướt băng lam 2 bên
                float dx = Mathf.Abs(x - size * 0.5f);
                float dy = y - size * 0.5f;

                if (dx >= 4f && dx <= 26f && dy >= -dx * 0.6f && dy <= dx * 0.6f)
                {
                    float edge = Mathf.Abs(dy) / (dx * 0.6f);
                    tex.SetPixel(x, y, edge > 0.7f ? white : cyan);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_CrescentWaves()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color cyan = new Color(0.2f, 0.85f, 1f, 1f);
        Color core = new Color(0.9f, 1f, 1f, 1f);
        Vector2 center = new Vector2(size / 2f, size * 0.25f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 3 tầng sóng kiếm hình vòng cung tỏa lên trên
                Vector2 pos = new Vector2(x, y);
                float d = Vector2.Distance(pos, center);
                float angle = Mathf.Atan2(y - center.y, x - center.x) * Mathf.Rad2Deg;

                bool inAngle = (angle >= 40f && angle <= 140f);
                bool wave1 = inAngle && Mathf.Abs(d - 16f) <= 2.2f;
                bool wave2 = inAngle && Mathf.Abs(d - 27f) <= 2.5f;
                bool wave3 = inAngle && Mathf.Abs(d - 38f) <= 2.8f;

                if (wave1 || wave2 || wave3)
                {
                    tex.SetPixel(x, y, (wave1 || wave2) ? core : cyan);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_DuneDaggers()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color gold = new Color(1f, 0.82f, 0.25f, 1f);
        Color amber = new Color(0.92f, 0.55f, 0.12f, 1f);
        Color core = new Color(1f, 0.96f, 0.75f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Song đoản đao cong lưỡi liềm bắt chéo hình chữ X
                float d1 = Mathf.Abs((x - y) * 0.707f);
                float d2 = Mathf.Abs((x + y - size) * 0.707f);

                bool blade1 = (d1 <= 2.5f) && (x >= 12 && x <= 52 && y >= 12 && y <= 52);
                bool blade2 = (d2 <= 2.5f) && (x >= 12 && x <= 52 && y >= 12 && y <= 52);
                bool curveGlow = (Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) - 18f) <= 1.8f);

                if (blade1 || blade2)
                {
                    bool isCore = (d1 <= 1.2f || d2 <= 1.2f);
                    tex.SetPixel(x, y, isCore ? core : gold);
                }
                else if (curveGlow)
                {
                    tex.SetPixel(x, y, amber);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_SandDash()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color amber = new Color(0.98f, 0.70f, 0.22f, 1f);
        Color white = new Color(1f, 0.95f, 0.7f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Mũi tên lướt tốc biến với các vệt cát ảo ảnh song song
                float dy = Mathf.Abs(y - size * 0.5f);
                float arrowX = size * 0.75f - dy * 1.3f;
                bool arrowHead = (x >= arrowX - 3.5f && x <= arrowX && dy <= 16f);
                bool trail1 = (y >= 26 && y <= 29 && x >= 10 && x <= 38);
                bool trail2 = (y >= 35 && y <= 38 && x >= 16 && x <= 42);
                bool trail3 = (y >= 18 && y <= 21 && x >= 20 && x <= 34);

                if (arrowHead)
                {
                    tex.SetPixel(x, y, white);
                }
                else if (trail1 || trail2 || trail3)
                {
                    tex.SetPixel(x, y, amber);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_SandstormXRend()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color gold = new Color(1f, 0.85f, 0.30f, 1f);
        Color amber = new Color(0.95f, 0.50f, 0.10f, 1f);
        Color core = new Color(1f, 1f, 0.85f, 1f);
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                float angle = Mathf.Atan2(y - center.y, x - center.x);

                // Lốc xoáy 4 cánh chữ X xoay tròn
                float spiral = Mathf.Sin(angle * 4f + dist * 0.45f);
                bool isVortexArm = (dist >= 5f && dist <= 26f && spiral > 0.55f);
                bool isEye = (dist <= 4.5f);

                if (isEye)
                {
                    tex.SetPixel(x, y, core);
                }
                else if (isVortexArm)
                {
                    tex.SetPixel(x, y, (spiral > 0.85f) ? gold : amber);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_ArcaneOrbs()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color orbCore = new Color(0.95f, 0.98f, 1f, 1f);
        Color orbPlasma = new Color(0.88f, 0.35f, 1f, 1f);
        Color outerGlow = new Color(0.55f, 0.12f, 0.85f, 0.95f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float dx = Mathf.Abs(x - center.x);
                float dy = Mathf.Abs(y - center.y);

                // 4-pointed radiant star spikes
                bool isStarRay = (dx <= 2f && dy <= 22f) || (dy <= 2f && dx <= 22f);

                if (d <= 6f)
                {
                    tex.SetPixel(x, y, orbCore);
                }
                else if (d <= 15f)
                {
                    float t = (d - 6f) / 9f;
                    tex.SetPixel(x, y, Color.Lerp(orbCore, orbPlasma, t));
                }
                else if (isStarRay)
                {
                    float t = Mathf.Max(dx, dy) / 22f;
                    tex.SetPixel(x, y, Color.Lerp(orbCore, orbPlasma, t));
                }
                else if (d <= 22f)
                {
                    float t = (d - 15f) / 7f;
                    float a = Mathf.Clamp01(1f - t);
                    tex.SetPixel(x, y, new Color(outerGlow.r, outerGlow.g, outerGlow.b, a));
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_ArcaneBlink()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color flashCore = new Color(1f, 1f, 1f, 1f);
        Color flashViolet = new Color(0.88f, 0.4f, 1f, 1f);
        Color flashDeep = new Color(0.45f, 0.1f, 0.75f, 1f);
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center.x);
                float dy = Mathf.Abs(y - center.y);
                float starDist = Mathf.Min(dx, dy) + Mathf.Max(dx, dy) * 0.3f;
                float diag = Mathf.Abs(dx - dy);

                if (dx <= 2f && dy <= 22f || dy <= 2f && dx <= 22f)
                {
                    tex.SetPixel(x, y, (dx <= 1f && dy <= 12f || dy <= 1f && dx <= 12f) ? flashCore : flashViolet);
                }
                else if (diag <= 2.5f && (dx + dy) <= 24f)
                {
                    tex.SetPixel(x, y, flashDeep);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateIcon_MeteorApocalypse()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color meteorCore = new Color(1f, 0.95f, 0.85f, 1f);
        Color flamePink = new Color(1f, 0.4f, 0.7f, 1f);
        Color flamePurple = new Color(0.6f, 0.15f, 0.9f, 1f);

        Vector2 meteorCenter = new Vector2(size * 0.35f, size * 0.35f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distToHead = Vector2.Distance(pos, meteorCenter);
                float tailProgress = ((x - meteorCenter.x) + (y - meteorCenter.y)) / (1.414f * size);
                float distDiag = Mathf.Abs((x - meteorCenter.x) - (y - meteorCenter.y)) / 1.414f;

                if (distToHead <= 6f)
                {
                    tex.SetPixel(x, y, distToHead <= 3f ? meteorCore : flamePink);
                }
                else if (tailProgress > 0f && tailProgress <= 0.55f && distDiag <= (8f * (1f - tailProgress * 1.4f)))
                {
                    tex.SetPixel(x, y, distDiag <= 2.5f ? flamePink : flamePurple);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void EnsureTargets()
    {
        if (playerCombat == null) playerCombat = FindAnyObjectByType<PlayerCombat>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (playerHealth == null && playerCombat != null) playerHealth = playerCombat.GetComponent<PlayerHealth>();
        if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        EnsureTargets();
        if (playerHealth != null && playerHealth.IsDead) return;

        if (playerController != null && playerController.IsStunned)
        {
            StartCoroutine(ShakeButtonRoutine());
            return;
        }

        transform.localScale = originalScale * 0.85f;
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
            var titanSkill = (playerCombat != null) ? playerCombat.GetComponent<TitanBerserkerSkill>() : null;
            var azureBlade = (playerCombat != null) ? playerCombat.GetComponent<AzureBladeSkill>() : null;
            var roninSkill = (playerCombat != null) ? playerCombat.GetComponent<GaleRoninSkill>() : null;

            if (titanSkill != null && titanSkill.enabled)
            {
                // KHÔNG gọi StartEarthshakerCharge ngay lập tức khi vừa chạm (frame 0) để không ngắt hoạt ảnh combo đánh thường!
                // Gồng tụ lực sẽ được kích hoạt trong Update() khi người chơi thực sự giữ nút (holdDur >= 0.15s)
            }
            else if (azureBlade != null && azureBlade.enabled)
            {
                azureBlade.StartAim(azureBlade.GetFacingDirection());
            }
            else if (roninSkill != null && roninSkill.enabled && roninSkill.IsAttackReady && roninSkill.CanUseIaidoCharge)
            {
                Vector2 facing = (playerController != null) ? playerController.GetFacingDirection() : Vector2.down;
                roninSkill.StartIaidoCharge(facing);
            }
        }

        SkillAimIndicator.EnsureExists((playerCombat != null) ? playerCombat.gameObject : playerController?.gameObject);
    }

    public void OnDrag(PointerEventData eventData)
    {
        EnsureTargets();
        if (playerHealth != null && playerHealth.IsDead)
        {
            isPointerDown = false;
            return;
        }

        if (playerController != null && playerController.IsStunned)
        {
            isPointerDown = false;
            return;
        }

        if (isPointerDown)
        {
            Vector2 delta = eventData.position - pointerDownPos;
            if (delta.sqrMagnitude > 150f) // Kéo ngắm vượt qua vùng chết
            {
                currentDragAimDir = delta.normalized;
                Canvas parentC = GetComponentInParent<Canvas>();
                SkillCancelZoneUI.EnsureExists(parentC)?.Show();
            }

            if (SkillCancelZoneUI.Instance != null && SkillCancelZoneUI.Instance.gameObject.activeInHierarchy)
            {
                SkillCancelZoneUI.Instance.UpdatePointerPosition(eventData.position);
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = originalScale;

        if (!isPointerDown) return;
        isPointerDown = false;

        EnsureTargets();
        if (playerHealth != null && playerHealth.IsDead) return;
        if (playerController != null && playerController.IsStunned) return;

        bool isCancelled = false;
        if (SkillCancelZoneUI.Instance != null && SkillCancelZoneUI.Instance.gameObject.activeInHierarchy)
        {
            isCancelled = SkillCancelZoneUI.Instance.IsHovered || SkillCancelZoneUI.Instance.CheckIfInside(eventData.position);
            SkillCancelZoneUI.Instance.Hide();
        }

        if (SkillAimIndicator.Instance != null)
        {
            SkillAimIndicator.Instance.Hide();
        }

        // Hủy chiêu chuẩn Liên Quân Mobile khi kéo vào vùng chữ X
        if (isCancelled)
        {
            var titanSkill = (playerCombat != null) ? playerCombat.GetComponent<TitanBerserkerSkill>() : null;
            if (titanSkill != null && titanSkill.IsChargingEarthshaker)
            {
                titanSkill.CancelEarthshakerCharge();
            }
            var azureBlade = (playerCombat != null) ? playerCombat.GetComponent<AzureBladeSkill>() : null;
            if (azureBlade != null)
            {
                azureBlade.CancelAim();
            }
            var roninSkill = (playerCombat != null) ? playerCombat.GetComponent<GaleRoninSkill>() : null;
            if (roninSkill != null)
            {
                roninSkill.CancelIaidoCharge();
            }
            var duneSkill = (playerCombat != null) ? playerCombat.GetComponent<DuneAssassinSkill>() : null;
            if (duneSkill != null && (duneSkill.IsDefending || duneSkill.IsAimingTornado))
            {
                duneSkill.CancelTornadoAim();
            }
            if (playerCombat != null && playerCombat.IsParrying)
            {
                playerCombat.CancelParryStance();
            }
            var mageSkillCancel = (playerCombat != null) ? playerCombat.GetComponent<CelestialMageSkill>() : null;
            if (mageSkillCancel != null && mageSkillCancel.IsChargingBarrier)
            {
                mageSkillCancel.ReleaseBarrierCharge(Vector2.zero, 0);
            }

            if (playerCombat != null) playerCombat.CancelAttackHold();
            SoundManager.EnsureInstance()?.PlayMenuHover();
            currentDragAimDir = Vector2.zero;
            return;
        }

        EnsureTargets();
        if (playerCombat != null) playerCombat.CancelAttackHold();
        float holdDur = Time.time - pointerDownTime;
        Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) 
            ? currentDragAimDir 
            : ((playerController != null) ? playerController.GetFacingDirection() : Vector2.down);

        if (buttonType == ButtonType.Attack)
        {
            var titanSkill = (playerCombat != null) ? playerCombat.GetComponent<TitanBerserkerSkill>() : null;
            var azureBlade = (playerCombat != null) ? playerCombat.GetComponent<AzureBladeSkill>() : null;
            var roninSkill = (playerCombat != null) ? playerCombat.GetComponent<GaleRoninSkill>() : null;
            var duneSkill = (playerCombat != null) ? playerCombat.GetComponent<DuneAssassinSkill>() : null;
            var mageSkill = (playerCombat != null) ? playerCombat.GetComponent<CelestialMageSkill>() : null;
            int bonusAtk = (playerCombat != null) ? playerCombat.BonusAttackDamage : 0;

            if (mageSkill != null && mageSkill.enabled)
            {
                if (mageSkill.IsChargingBarrier)
                {
                    mageSkill.ReleaseBarrierCharge(aimDir, bonusAtk);
                }
                else
                {
                    if (currentDragAimDir.sqrMagnitude > 0.01f && playerController != null)
                    {
                        playerController.SetFacingDirection(currentDragAimDir);
                    }
                    playerCombat.PerformAttack();
                }
            }
            else if (duneSkill != null && duneSkill.enabled)
            {
                if (duneSkill.IsAimingTornado || duneSkill.IsDefending)
                {
                    duneSkill.ReleaseTornadoAim(aimDir, bonusAtk);
                }
                else
                {
                    if (currentDragAimDir.sqrMagnitude > 0.01f && playerController != null)
                    {
                        playerController.SetFacingDirection(currentDragAimDir);
                    }
                    playerCombat.PerformAttack();
                }
            }
            else if (titanSkill != null && titanSkill.enabled)
            {
                if (titanSkill.IsChargingEarthshaker)
                {
                    titanSkill.ReleaseEarthshakerCharge(aimDir, bonusAtk);
                }
                else
                {
                    if (currentDragAimDir.sqrMagnitude > 0.01f && playerController != null)
                    {
                        playerController.SetFacingDirection(currentDragAimDir);
                    }
                    playerCombat.PerformAttack();
                }
            }
            else if (azureBlade != null && azureBlade.enabled && (azureBlade.IsAiming || holdDur >= 0.15f || currentDragAimDir.sqrMagnitude > 0.01f))
            {
                azureBlade.ReleaseAimAndFire(bonusAtk);
            }
            else if (roninSkill != null && roninSkill.enabled)
            {
                if (roninSkill.CanUseIaidoCharge && roninSkill.IsChargingIaido)
                {
                    roninSkill.ReleaseIaidoCharge(aimDir, bonusAtk);
                }
                else
                {
                    if (currentDragAimDir.sqrMagnitude > 0.01f && playerController != null)
                    {
                        playerController.SetFacingDirection(currentDragAimDir);
                    }
                    playerCombat.PerformAttack();
                }
            }
            else if (playerCombat != null)
            {
                if (playerCombat.IsParrying)
                {
                    playerCombat.ReleaseParryStance(aimDir, bonusAtk);
                }
                else
                {
                    if (currentDragAimDir.sqrMagnitude > 0.01f && playerController != null)
                    {
                        playerController.SetFacingDirection(currentDragAimDir);
                    }
                    playerCombat.PerformAttack();
                }
            }

            if (playerController != null) playerController.RefreshMoveInput();
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
        if (playerHealth != null && playerHealth.IsDead) return;
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
