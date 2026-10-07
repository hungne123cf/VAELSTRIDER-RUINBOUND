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
        cachedHeroActionIcons.Clear();
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
            var titan = (playerCombat != null) ? playerCombat.GetComponent<TitanBerserkerSkill>() : null;
            var azureBlade = (playerCombat != null) ? playerCombat.GetComponent<AzureBladeSkill>() : null;
            if (titan != null)
            {
                float norm = titan.AttackCooldownRatio;
                float rem = titan.AttackCooldownRemaining;
                UpdateCooldownDisplay(norm, rem);

                if (isPointerDown)
                {
                    float holdDur = Time.time - pointerDownTime;
                    if (holdDur >= HOLD_AIM_THRESHOLD || currentDragAimDir.sqrMagnitude > 0.01f)
                    {
                        Vector2 playerPos = (Vector2)playerCombat.transform.position;
                        Vector2 aimDir = (currentDragAimDir.sqrMagnitude > 0.01f) ? currentDragAimDir : ((playerController != null) ? playerController.GetFacingDirection() : Vector2.down);
                        SkillAimIndicator.EnsureExists(playerCombat.gameObject)?.ShowAimArc(playerPos, aimDir, 1.35f, 140f, new Color(1f, 0.55f, 0.1f, 0.9f));
                        SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
                    }
                }
            }
            else if (azureBlade != null)
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
                        SkillCancelZoneUI.EnsureExists(GetComponentInParent<Canvas>())?.Show();
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

                        if (titan != null && titan.enabled)
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

        // 1. Khung viền đổi màu phát sáng theo hệ nguyên tố của tướng
        Sprite frameSprite = null;
#if UNITY_EDITOR
        frameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/potion_slot_circle.png");
        if (frameSprite == null) frameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/circle_fill.png");
#endif
        if (frameSprite == null) frameSprite = Resources.Load<Sprite>("UI/potion_slot_circle");

        Image btnImg = GetComponent<Image>();
        if (btnImg != null && frameSprite != null)
        {
            btnImg.sprite = frameSprite;
            if (isAzure)
            {
                btnImg.color = new Color(0.35f, 0.88f, 1f, 1f); // Lam Băng Tinh Thể
            }
            else if (isRonin)
            {
                btnImg.color = new Color(0.28f, 1f, 0.75f, 1f); // Phong Lôi Ngọc Bích
            }
            else
            {
                btnImg.color = new Color(1f, 0.88f, 0.40f, 1f); // Hoàng Kim Cổ Tự
            }
        }

        // Cập nhật màu chữ Cooldown theo tướng
        if (cooldownText != null)
        {
            if (isAzure) cooldownText.color = new Color(0.6f, 0.95f, 1f, 1f);
            else if (isRonin) cooldownText.color = new Color(0.4f, 1f, 0.8f, 1f);
            else cooldownText.color = new Color(1f, 0.9f, 0.35f, 1f);
        }

        // 2. Icon chuyên biệt độc quyền cho từng nhân vật
        Sprite actionSprite = GetHeroActionIcon(curHero, buttonType);

        Transform actionIconT = transform.Find("ActionIcon");
        Image actionIconImg = null;
        if (actionIconT == null)
        {
            GameObject iconGo = new GameObject("ActionIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(transform, false);
            iconGo.transform.SetSiblingIndex(0);

            RectTransform rt = iconGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.10f, 0.10f);
            rt.anchorMax = new Vector2(0.90f, 0.90f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            actionIconImg = iconGo.GetComponent<Image>();
            actionIconImg.raycastTarget = false;
            actionIconImg.preserveAspect = true;
        }
        else
        {
            actionIconImg = actionIconT.GetComponent<Image>();
            RectTransform rt = actionIconT.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.10f, 0.10f);
                rt.anchorMax = new Vector2(0.90f, 0.90f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
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

    private static Sprite GetHeroActionIcon(string heroId, ButtonType btnType)
    {
        string key = $"{heroId}_{btnType}";
        if (cachedHeroActionIcons.TryGetValue(key, out Sprite cached) && cached != null)
        {
            return cached;
        }

        bool isTitan = (heroId.Contains("titan") || heroId.Contains("player4"));
        bool isRonin = (heroId.Contains("ronin") || heroId.Contains("gale") || heroId.Contains("maid"));
        bool isAzure = (heroId.Contains("azure") || heroId.Contains("player3"));

        Sprite result = null;

        if (isTitan)
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
            if (btnType == ButtonType.Attack)
                result = GenerateIcon_RuinSword();
            else if (btnType == ButtonType.Dash)
                result = GenerateIcon_RuinDash();
            else
                result = GenerateIcon_RuinWhirlwind();
        }

        if (result != null)
        {
            cachedHeroActionIcons[key] = result;
        }
        return result;
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
            var azureBlade = (playerCombat != null) ? playerCombat.GetComponent<AzureBladeSkill>() : null;
            if (azureBlade != null)
            {
                azureBlade.CancelAim();
            }

            Vector3 playerPos = (playerCombat != null) ? playerCombat.transform.position : ((playerController != null) ? playerController.transform.position : transform.position);
            DamagePopup.CreateText(playerPos + Vector3.up * 1.5f, LocalizationManager.T("ĐÃ HỦY CHIÊU", "CANCELLED"), new Color(1f, 0.35f, 0.35f));
            SoundManager.EnsureInstance()?.PlayMenuHover();
            currentDragAimDir = Vector2.zero;
            return;
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
