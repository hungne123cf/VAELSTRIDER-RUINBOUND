using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameAutoBootstrap : MonoBehaviour
{
    private static bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitializeGameComponents()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        BootstrapAll();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("<color=cyan>[VAELSTRIDER BOOTSTRAP]</color> Phát hiện Scene Loaded / Respawn! Tái khởi tạo toàn bộ hệ thống...");
        BootstrapAll();
    }

    public static void BootstrapAll()
    {
        Time.timeScale = 1.0f; // Dam bao thoi gian luon chay binh thuong khi vao map
        AccountDatabase.EnsureActiveSessionLoaded();
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "LoginScene")
        {
            SoundManager.EnsureInstance();
            LoginManager.EnsureInstance();
            Debug.Log("<color=cyan>[BOOTSTRAP]</color> Dang o LoginScene. Khoi tao Sound va Login UI.");
            return;
        }
        if (currentScene == "MainMenuScene")
        {
            SoundManager.EnsureInstance();
            MainMenuManager.EnsureInstance();
            Debug.Log("<color=cyan>[BOOTSTRAP]</color> Dang o MainMenuScene. Khoi tao Sound va MainMenu UI.");
            return;
        }
        if (currentScene == "Map2_ValleyRuin")
        {
            LevelUpChoiceUI.ResetUI();
            // Dang o Map 2: Giu nguyen tien trinh ca tran (so luot doi lai Rerolls va Linh hon tich luy tu Map 1)

            // Dang o Map 2: Giu nguyen dia hinh thung lung Map 2, khoi tao UI, Upgrade va Ancient Shrine Tru Da Phat Sang
            MatchTimerUI.EnsureUIExists();
            MinimapHUD.EnsureUIExists();
            FullscreenMapUI.EnsureUIExists();
            PotionQuickSlotUI.EnsureUIExists();
            GameOverUI.EnsureUIExists();
            EnsureSpecialSkillButton();
            SettingsUI.EnsureSettingsButtonExists();
            CharacterStatsUI.EnsureUIExists();
            InGameInventoryUI.EnsureUIExists();
            PlayerAvatarUI.EnsureUIExists();
            PlayerEnergyUI.EnsureUIExists();
            PlayerUpgradeManager.EnsureInstance();
            ChestLootPopupUI.EnsureUIExists();
            PortalConfirmationPopupUI.EnsureUIExists();
            RemoveAncientShrine();
            PlayerLevelSystem.EnsureInstance();
            AugmentManager.EnsureInstance();
            PlayerExpBarUI.EnsureUIExists();
            AugmentHUDBar.EnsureExists();
            GuardianShieldHUDTracker.EnsureTrackerExists();
            LostShardsGrave.EnsureSpawnGrave();
            ApplySelectedCharacterToPlayer();
            EnsureBreakables();
            MapSpiritSparksManager.EnsureInstance();
            CorruptedAtmosphereManager.EnsureAtmosphere();
            MagmaCalamityHazardManager.EnsureManager();
            AcidDownpourHazardManager.EnsureManager();
            FrostfallBlizzardHazardManager.EnsureManager();
            Debug.Log("<color=green>[BOOTSTRAP]</color> Đang ở Map 2 (Valley Ruin)! Đã khởi tạo Hệ thống Level Farm & Lõi TFT cùng hiệu ứng nguyên tố.");
            return;
        }

        if (currentScene.Contains("Map3") || currentScene.Contains("Ancient"))
        {
            LevelUpChoiceUI.ResetUI();
            // Dang o Map 3: Giu nguyen tien trinh ca tran (so luot doi lai Rerolls va Linh hon tich luy tu Map 1, 2)

            // Đang ở Map 3: Giữ nguyên 100% bản đồ bạn tự vẽ, chỉ nạp UI điều khiển và nâng cấp
            MatchTimerUI.EnsureUIExists();
            MinimapHUD.EnsureUIExists();
            FullscreenMapUI.EnsureUIExists();
            PotionQuickSlotUI.EnsureUIExists();
            GameOverUI.EnsureUIExists();
            EnsureSpecialSkillButton();
            SettingsUI.EnsureSettingsButtonExists();
            CharacterStatsUI.EnsureUIExists();
            InGameInventoryUI.EnsureUIExists();
            PlayerAvatarUI.EnsureUIExists();
            PlayerEnergyUI.EnsureUIExists();
            PlayerUpgradeManager.EnsureInstance();
            ChestLootPopupUI.EnsureUIExists();
            PortalConfirmationPopupUI.EnsureUIExists();
            RemoveAncientShrine();
            PlayerLevelSystem.EnsureInstance();
            AugmentManager.EnsureInstance();
            PlayerExpBarUI.EnsureUIExists();
            AugmentHUDBar.EnsureExists();
            GuardianShieldHUDTracker.EnsureTrackerExists();
            LostShardsGrave.EnsureSpawnGrave();
            ApplySelectedCharacterToPlayer();
            EnsureBreakables();
            CorruptedAtmosphereManager.EnsureAtmosphere();
            MagmaCalamityHazardManager.EnsureManager();
            AcidDownpourHazardManager.EnsureManager();
            FrostfallBlizzardHazardManager.EnsureManager();
            // Map 3 hoan toan do Map3WaveSpawner quan ly (Goblin, Skeleton, Slime, Skeleton King Boss)
            Debug.Log("<color=green>[BOOTSTRAP]</color> Đang ở Map 3 (Forgotten Memories)! Tải toàn diện hệ thống Map 3.");
            return;
        }

        // 0. Kiến tạo Bản Đồ Đa Phòng chỉ riêng cho Map 1 (Map1_Thestart)
        if (currentScene == "SampleScene" || currentScene == "Map1_Thestart")
        {
            if (!CorruptedCycleManager.IsCorruptedActive)
            {
                // Đầu mỗi trận mới thường: xóa sạch số linh hồn tạm tích từ trận cũ
                PlayerInventory.ResetRunShards();
                LevelUpChoiceUI.ResetUI();
                LevelUpChoiceUI.ResetMatchRerolls();
            }

            DungeonMapBuilder.BuildDungeon();
        }

        // 1. Khoi tao nut Binh Mau, Timer, Minimap & Fullscreen Map tren Canvas
        MatchTimerUI.EnsureUIExists();
        MinimapHUD.EnsureUIExists();
        FullscreenMapUI.EnsureUIExists();
        PotionQuickSlotUI.EnsureUIExists();
        GameOverUI.EnsureUIExists();

        // 2. Khoi tao nut Tuyet Ky Ruin Whirlwind tren Canvas
        EnsureSpecialSkillButton();

        // 2b. Khoi tao nut Settings, Character Stats va In-Game Bag tren Canvas
        SettingsUI.EnsureSettingsButtonExists();
        CharacterStatsUI.EnsureUIExists();
        InGameInventoryUI.EnsureUIExists();
        PlayerAvatarUI.EnsureUIExists();
        PlayerEnergyUI.EnsureUIExists();
        ChestLootPopupUI.EnsureUIExists();
        PortalConfirmationPopupUI.EnsureUIExists();

        // 3. Khoi tao He thong 3 dot quai + Boss Titan Wheel (WaveSpawner)
        EnsureWaveSpawner();

        // 4. Khoi tao Banner thong bao Wave tren UI (WaveUI)
        EnsureWaveUI();

        // 5. Khoi tao Ruong Kho Bau trong phong (TreasureChest)
        EnsureTreasureChest();

        // 6. Gỡ bỏ Trụ Nâng Cấp cũ, thay bằng Hệ thống Level & Lõi TFT
        RemoveAncientShrine();
        PlayerLevelSystem.EnsureInstance();
        AugmentManager.EnsureInstance();
        PlayerExpBarUI.EnsureUIExists();
        AugmentHUDBar.EnsureExists();
        GuardianShieldHUDTracker.EnsureTrackerExists();

        // 7. He thong Anh Sang URP 2D Dark Fantasy
        EnsureDungeonLighting();

        // 8. Dam bao Player Upgrade Manager va UI
        PlayerUpgradeManager.EnsureInstance();
        MapSpiritSparksManager.EnsureInstance();

        // 9. Co che Dark Souls: Kiem tra va tai sinh Vung Hon (Lost Shards Grave) tai vi tri tu tran cu
        LostShardsGrave.EnsureSpawnGrave();

        // 10. Ap dung Nhan Vat da chon (Vaelstrider / Maid) va Tuyet Ky Quet Tan Ke Yeu
        ApplySelectedCharacterToPlayer();
        CorruptedAtmosphereManager.EnsureAtmosphere();
        MagmaCalamityHazardManager.EnsureManager();
        AcidDownpourHazardManager.EnsureManager();
        FrostfallBlizzardHazardManager.EnsureManager();

        Debug.Log("<color=cyan>[VAELSTRIDER BOOTSTRAP]</color> Tất cả hệ thống (Bàn thờ, Ánh sáng 2D, Rương, 3 Wave quái, Boss, Bình máu, Tuyệt kỹ Whirlwind, UI) đã sẵn sàng 100%!");
    }

    public static void SetGameplayHUDVisible(bool visible)
    {
        string curScene = SceneManager.GetActiveScene().name;
        if (curScene == "MainMenuScene" || curScene == "LoginScene") return;

        // 1. Virtual Joysticks
        var joysticks = Object.FindObjectsByType<VirtualJoystick>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var joy in joysticks)
        {
            if (joy != null && joy.gameObject != null) joy.gameObject.SetActive(visible);
        }
        var joyBg = GameObject.Find("JoystickBackground");
        if (joyBg != null) joyBg.SetActive(visible);

        // 1.1 All Mobile Action Buttons directly
        var actionButtons = Object.FindObjectsByType<MobileActionButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in actionButtons)
        {
            if (b != null && b.gameObject != null) b.gameObject.SetActive(visible);
        }

        if (!visible && SkillCancelZoneUI.Instance != null)
        {
            SkillCancelZoneUI.Instance.Hide();
        }

        // 2. All Minimap components & roots
        var minimaps = Object.FindObjectsByType<MinimapHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in minimaps)
        {
            if (m != null && m.gameObject != null) m.gameObject.SetActive(MinimapHUD.EnableMinimapHUD && visible);
        }

        // 3. Match Timer
        var timers = Object.FindObjectsByType<MatchTimerUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in timers)
        {
            if (t != null && t.gameObject != null) t.gameObject.SetActive(visible);
        }

        // 4. Potion & Energy
        if (PotionQuickSlotUI.Instance != null && PotionQuickSlotUI.Instance.gameObject != null)
            PotionQuickSlotUI.Instance.gameObject.SetActive(PotionQuickSlotUI.EnablePotionQuickSlot && visible);

        if (PlayerEnergyUI.Instance != null && PlayerEnergyUI.Instance.gameObject != null)
            PlayerEnergyUI.Instance.gameObject.SetActive(visible);

        if (PlayerExpBarUI.Instance != null && PlayerExpBarUI.Instance.gameObject != null)
            PlayerExpBarUI.Instance.SetVisible(visible);

        if (AugmentHUDBar.Instance != null && AugmentHUDBar.Instance.gameObject != null)
            AugmentHUDBar.Instance.gameObject.SetActive(visible);

        // 5. Hide / Show all specific HUD objects across ALL Canvases
        string[] hudNames = new string[]
        {
            "Btn_Attack", "Btn_Dash", "Btn_Special", "Btn_Potion",
            "Btn_InGameBag", "Btn_CharacterStats", "Btn_SettingsGear", "Btn_InGameSettings", "Btn_InGameInfo",
            "PlayerAvatarHUD", "PlayerHealthBar", "PlayerHealthUI", "PlayerExpBarHUD", "ExpBarHUD", "ShardsCounter", "WaveBanner", "MatchTimerPanel", "RuinRadarCompass",
            "Minimap_Compass_HUD", "RadarDisc", "CompassNeedle", "RuinPortalDistanceText"
        };

        var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c == null) continue;
            string cName = c.gameObject.name;
            if (cName.Contains("InGameInventoryModal") || cName.Contains("SettingsModal") || cName.Contains("CharacterStatsPanel") || cName.Contains("ShrineUpgrade") || cName.Contains("FullscreenMapModal") || cName.Contains("LevelUpRoot") || cName.Contains("LevelUpChoiceUI"))
                continue;

            HideChildrenByName(c.transform, hudNames, visible);
        }
    }

    private static void HideChildrenByName(Transform parent, string[] targetNames, bool visible)
    {
        if (parent == null) return;
        for (int i = 0; i < targetNames.Length; i++)
        {
            if (parent.name == targetNames[i])
            {
                bool targetActive = visible;
                if (parent.name == "Btn_Potion" && !PotionQuickSlotUI.EnablePotionQuickSlot) targetActive = false;
                else if (parent.name == "Btn_InGameBag" && !InGameInventoryUI.EnableInGameInventory) targetActive = false;
                else if ((parent.name == "Minimap_Compass_HUD" || parent.name == "RadarDisc" || parent.name == "CompassNeedle" || parent.name == "RuinPortalDistanceText") && !MinimapHUD.EnableMinimapHUD) targetActive = false;

                parent.gameObject.SetActive(targetActive);
                return;
            }
        }
        for (int i = 0; i < parent.childCount; i++)
        {
            HideChildrenByName(parent.GetChild(i), targetNames, visible);
        }
    }

    public static Canvas GetMainGameplayCanvas()
    {
        var cObj = GameObject.Find("Canvas");
        if (cObj != null)
        {
            var c = cObj.GetComponent<Canvas>();
            if (c != null && c.isRootCanvas && c.enabled) return c;
        }

        var all = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c != null && c.isRootCanvas && c.enabled && !c.name.Contains("UltiCinematic") && !c.name.Contains("GameOver") && !c.name.Contains("ShrineUpgrade"))
            {
                return c;
            }
        }
        return all.Length > 0 ? all[0] : null;
    }

    public static GameObject EnsureSpecialSkillButton()
    {
        Canvas canvas = GetMainGameplayCanvas();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("Btn_Special");
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject btnObj = new GameObject("Btn_Special");
        btnObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-350, 290);
        rt.sizeDelta = new Vector2(100, 100);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = Color.white;
        var bgCircle = Resources.Load<Sprite>("Combat/circle_fill");
#if UNITY_EDITOR
        if (bgCircle == null) bgCircle = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/circle_fill.png");
#endif
        if (bgCircle != null) bg.sprite = bgCircle;

        // Icon bão kiếm xoay
        GameObject iconObj = new GameObject("SkillIcon");
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(16, 16);
        iconRt.offsetMax = new Vector2(-16, -16);
        Image iconImg = iconObj.AddComponent<Image>();
        var iconSprite = Resources.Load<Sprite>("Combat/skill_whirlwind_icon");
#if UNITY_EDITOR
        if (iconSprite == null) iconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Combat/skill_whirlwind_icon.png");
#endif
        if (iconSprite != null) iconImg.sprite = iconSprite;
        iconImg.raycastTarget = false;

        // Lớp phủ Cooldown xoay tròn (Radial 360)
        GameObject cdObj = new GameObject("CooldownOverlay");
        cdObj.transform.SetParent(btnObj.transform, false);
        RectTransform cdRt = cdObj.AddComponent<RectTransform>();
        cdRt.anchorMin = Vector2.zero;
        cdRt.anchorMax = Vector2.one;
        cdRt.offsetMin = Vector2.zero;
        cdRt.offsetMax = Vector2.zero;
        Image cdImg = cdObj.AddComponent<Image>();
        cdImg.color = new Color(0.04f, 0.05f, 0.08f, 0.78f);
        cdImg.type = Image.Type.Filled;
        cdImg.fillMethod = Image.FillMethod.Radial360;
        cdImg.fillOrigin = (int)Image.Origin360.Top;
        cdImg.fillClockwise = false;
        cdImg.raycastTarget = false;
        cdImg.enabled = false;
        var cdCircle = Resources.Load<Sprite>("Combat/circle_fill");
#if UNITY_EDITOR
        if (cdCircle == null) cdCircle = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/UI_button/circle_fill.png");
#endif
        if (cdCircle != null) cdImg.sprite = cdCircle;

        // Số đếm Cooldown
        GameObject txtObj = new GameObject("CooldownText");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "6";
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 0.88f, 0.25f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.enabled = false;

        // Nút bấm cảm ứng điện thoại (không hiển thị phím PC)

        var actionBtn = btnObj.AddComponent<MobileActionButton>();
        var btnTypeField = typeof(MobileActionButton).GetField("buttonType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (btnTypeField != null) btnTypeField.SetValue(actionBtn, MobileActionButton.ButtonType.Skill);

        actionBtn.SetCooldownComponents(cdImg, tmp);

        Debug.Log("<color=green>[BOOTSTRAP]</color> Đã tạo nút Tuyệt kỹ Ruin Whirlwind (Btn_Special) tại tọa độ {-350, 290}!");
        return btnObj;
    }

    public static WaveSpawner EnsureWaveSpawner()
    {
        var spawner = Object.FindAnyObjectByType<WaveSpawner>();
        if (spawner != null) return spawner;

        GameObject spawnerObj = new GameObject("WaveSpawner");
        spawnerObj.transform.position = Vector3.zero;
        spawner = spawnerObj.AddComponent<WaveSpawner>();
        Debug.Log("<color=green>[BOOTSTRAP]</color> Da tao WaveSpawner: 3 dot quai + Boss Titan Wheel!");
        return spawner;
    }

    public static WaveUI EnsureWaveUI()
    {
        var waveUI = Object.FindAnyObjectByType<WaveUI>();
        if (waveUI != null) return waveUI;

        Canvas canvas = GetMainGameplayCanvas();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("WaveBanner");
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
            waveUI = existing.GetComponent<WaveUI>();
            if (waveUI == null) waveUI = existing.gameObject.AddComponent<WaveUI>();
            return waveUI;
        }

        GameObject bannerObj = new GameObject("WaveBanner");
        bannerObj.transform.SetParent(canvas.transform, false);
        bannerObj.SetActive(false);

        RectTransform rt = bannerObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -152f);
        rt.sizeDelta = new Vector2(600f, 60f);

        CanvasGroup cg = bannerObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        GameObject textObj = new GameObject("WaveText");
        textObj.transform.SetParent(bannerObj.transform, false);
        RectTransform txtRt = textObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        txtRt.anchoredPosition = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "WAVE 1 / 3";
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 0.85f, 0.3f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        waveUI = bannerObj.AddComponent<WaveUI>();
        Debug.Log("<color=green>[BOOTSTRAP]</color> Da tao WaveUI tren Canvas!");
        return waveUI;
    }

    public static TreasureChest EnsureTreasureChest()
    {
        var chest = Object.FindAnyObjectByType<TreasureChest>();
        if (chest != null) return chest;

        Vector3 spawnPos = new Vector3(2.5f, 1.2f, 0f);
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            spawnPos = player.transform.position + new Vector3(2.8f, 1.2f, 0f);
        }

        chest = TreasureChest.SpawnChest(spawnPos);
        Debug.Log("<color=green>[BOOTSTRAP]</color> Da dat Ruong Kho Bau tai: " + spawnPos);
        return chest;
    }

    public static void RemoveAncientShrine()
    {
        var shrines = Object.FindObjectsByType<AncientShrine>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in shrines)
        {
            if (s != null && s.gameObject != null)
            {
                Object.Destroy(s.gameObject);
            }
        }
        var shrineUIs = Object.FindObjectsByType<ShrineUpgradeUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ui in shrineUIs)
        {
            if (ui != null && ui.gameObject != null)
            {
                Object.Destroy(ui.gameObject);
            }
        }
    }

    public static AncientShrine EnsureAncientShrine(Vector3? customPos = null)
    {
        RemoveAncientShrine();
        return null;
    }

    public static DungeonLightingManager EnsureDungeonLighting()
    {
        return DungeonLightingManager.EnsureInstance();
    }

    public static void CleanUpGameplayUI()
    {
        string[] uiNames = new string[] {
            "Btn_Special", "PotionQuickSlot", "WaveBanner", "PlayerHealthBar",
            "LostShardsGrave", "TreasureChest_Crypt", "DungeonBarriers", "DungeonTorches",
            "UltiCinematicCanvas", "UltiCinematicUI", "LoginManager_Auto", "Login_DarkBackdrop",
            "GameOverPanel", "ShrineUpgradePanel", "CharacterStatsPanel", "SettingsPanel",
            "Btn_SettingsGear", "Btn_CharacterStats", "ChestLootPopupModal"
        };
        foreach (var name in uiNames)
        {
            var obj = GameObject.Find(name);
            if (obj != null) Object.Destroy(obj);
        }
    }

    public static void ApplySelectedCharacterToPlayer()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        CharacterManager.EnsureInstance();
        string selectedChar = CharacterManager.Instance.GetSelectedCharacterId();
        CharacterData charData = CharacterManager.Instance.GetSelectedCharacter();

        var playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null && charData != null)
        {
            playerHealth.SetBaseMaxHealth(charData.baseHealth);
        }

        if (charData != null)
        {
            player.SetBaseSpeed(charData.baseSpeed);
        }

        EnsurePlayerColliderAndCombat(player, selectedChar);
        if (selectedChar == CharacterManager.CHAR_TITAN_BERSERKER)
        {
            player.transform.localScale = Vector3.one;
            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
                sr.color = Color.white;
                sr.material = new Material(Shader.Find("Sprites/Default"));
#if UNITY_EDITOR
                var idleObjs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player4/Hammer_Idle.png");
                if (idleObjs != null)
                {
                    foreach (var o in idleObjs)
                    {
                        if (o is Sprite s)
                        {
                            sr.sprite = s;
                            break;
                        }
                    }
                }
#endif
                if (sr.sprite == null)
                {
                    var fallbacks = MainMenuManager.LoadSpritesFromPath("Assets/ART/Player4/Hammer_Idle.png", 64, 64);
                    if (fallbacks != null && fallbacks.Length > 0) sr.sprite = fallbacks[0];
                }
            }

            var anim = player.GetComponent<Animator>();
            if (anim != null) anim.enabled = true;
#if UNITY_EDITOR
            var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Player4/Player4.controller");
            if (controller != null && anim != null)
            {
                anim.runtimeAnimatorController = controller;
                anim.Rebind();
                anim.Update(0f);
            }
#endif
            var sweep = player.GetComponent<SweepTheWeakSkill>();
            if (sweep != null) Object.Destroy(sweep);

            var ronin = player.GetComponent<GaleRoninSkill>();
            if (ronin != null) Object.Destroy(ronin);

            var azure = player.GetComponent<AzureBladeSkill>();
            if (azure != null) Object.Destroy(azure);

            var mage = player.GetComponent<CelestialMageSkill>();
            if (mage != null) Object.Destroy(mage);

            var titan = player.GetComponent<TitanBerserkerSkill>();
            if (titan == null)
            {
                titan = player.gameObject.AddComponent<TitanBerserkerSkill>();
            }
#if UNITY_EDITOR
            var attackObjs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player4/Hammer_Attack.png");
            if (attackObjs != null)
            {
                foreach (var o in attackObjs)
                {
                    if (o is Sprite s && s.name == "Hammer_Attack_0_0")
                    {
                        titan.chargePoseSprite = s;
                        break;
                    }
                }
            }
#endif

            var pc = player.GetComponent<PlayerCombat>();
            if (pc != null)
            {
                pc.AutoAlignCombat();
            }

            var btnSpecial = GameObject.Find("Btn_Special");
            if (btnSpecial != null)
            {
                var txt = btnSpecial.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = LocalizationManager.T("CHẤN ĐỊA", "CATACLYSM");

                var btn = btnSpecial.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        var tb = player.GetComponent<TitanBerserkerSkill>();
                        if (tb != null) tb.TriggerSpecialSkill();
                    });
                }
            }

            Debug.Log("<color=cyan>[CHARACTER]</color> Đã áp dụng nhân vật CUỒNG CHIẾN BINH (TITAN BERSERKER) với Tuyệt kỹ Cuồng Nộ Chấn Địa!");
        }
        else if (selectedChar == CharacterManager.CHAR_AZURE_BLADE)
        {
            player.transform.localScale = Vector3.one;
            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.material = new Material(Shader.Find("Sprites/Default"));
#if UNITY_EDITOR
                var idleObjs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player3/Sword_Idle.png");
                if (idleObjs != null)
                {
                    foreach (var o in idleObjs)
                    {
                        if (o is Sprite s)
                        {
                            sr.sprite = s;
                            break;
                        }
                    }
                }
#endif
            }

            var anim = player.GetComponent<Animator>();
#if UNITY_EDITOR
            var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Player3/Player3.controller");
            if (controller != null && anim != null)
            {
                anim.runtimeAnimatorController = controller;
                anim.Rebind();
                anim.Update(0f);
            }
#endif
            var titan = player.GetComponent<TitanBerserkerSkill>();
            if (titan != null) Object.Destroy(titan);

            var sweep = player.GetComponent<SweepTheWeakSkill>();
            if (sweep != null) Object.Destroy(sweep);

            var ronin = player.GetComponent<GaleRoninSkill>();
            if (ronin != null) Object.Destroy(ronin);

            var mage = player.GetComponent<CelestialMageSkill>();
            if (mage != null) Object.Destroy(mage);

            var azure = player.GetComponent<AzureBladeSkill>();
            if (azure == null)
            {
                azure = player.gameObject.AddComponent<AzureBladeSkill>();
            }
            if (azure != null)
            {
                azure.SetBaseWaveDamage(24);
            }

            var btnSpecial = GameObject.Find("Btn_Special");
            if (btnSpecial != null)
            {
                var txt = btnSpecial.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "SWORD WAVE";

                var btn = btnSpecial.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        var az = player.GetComponent<AzureBladeSkill>();
                        if (az != null) az.TriggerSpecialSkill();
                    });
                }
            }

            Debug.Log("<color=cyan>[CHARACTER]</color> Đã áp dụng nhân vật LAM KIẾM SĨ (AZURE BLADE) với Tuyệt kỹ Bão Sóng Kiếm!");
        }
        else if (selectedChar == CharacterManager.CHAR_GALE_RONIN)
        {
            player.transform.localScale = Vector3.one;
            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.material = new Material(Shader.Find("Sprites/Default"));
#if UNITY_EDITOR
                var idleObjs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Player2/_down idle.png");
                if (idleObjs != null)
                {
                    foreach (var o in idleObjs)
                    {
                        if (o is Sprite s)
                        {
                            sr.sprite = s;
                            break;
                        }
                    }
                }
#endif
            }

            var anim = player.GetComponent<Animator>();
#if UNITY_EDITOR
            var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Player2/Player2.controller");
            if (controller != null && anim != null)
            {
                anim.runtimeAnimatorController = controller;
                anim.Rebind();
                anim.Update(0f);
            }
#endif
            var titan = player.GetComponent<TitanBerserkerSkill>();
            if (titan != null) Object.Destroy(titan);

            var azure = player.GetComponent<AzureBladeSkill>();
            if (azure != null) Object.Destroy(azure);

            var sweep = player.GetComponent<SweepTheWeakSkill>();
            if (sweep != null) Object.Destroy(sweep);

            var mage = player.GetComponent<CelestialMageSkill>();
            if (mage != null) Object.Destroy(mage);

            var ronin = player.GetComponent<GaleRoninSkill>();
            if (ronin == null)
            {
                ronin = player.gameObject.AddComponent<GaleRoninSkill>();
            }
            if (ronin != null)
            {
                ronin.SetBaseSlashDamage(28);
            }

            var btnSpecial = GameObject.Find("Btn_Special");
            if (btnSpecial != null)
            {
                var txt = btnSpecial.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "SHADOW STRIKE";

                var btn = btnSpecial.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        var r = player.GetComponent<GaleRoninSkill>();
                        if (r != null) r.TriggerSpecialSkill();
                    });
                }
            }

            Debug.Log("<color=green>[CHARACTER]</color> Đã áp dụng nhân vật PHONG LÔI LÃNG KHÁCH (GALE RONIN) với Tuyệt kỹ Ám Ảnh Trảm (Shadow Strike)!");
        }
        else if (selectedChar == CharacterManager.CHAR_DUNE_ASSASSIN)
        {
            player.transform.localScale = Vector3.one;
            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.material = new Material(Shader.Find("Sprites/Default"));
#if UNITY_EDITOR
                var idleSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Player5/PNG/idle/idle_1.png");
                if (idleSpr != null) sr.sprite = idleSpr;
#endif
            }

            var titan = player.GetComponent<TitanBerserkerSkill>();
            if (titan != null) Object.Destroy(titan);

            var sweep = player.GetComponent<SweepTheWeakSkill>();
            if (sweep != null) Object.Destroy(sweep);

            var ronin = player.GetComponent<GaleRoninSkill>();
            if (ronin != null) Object.Destroy(ronin);

            var azure = player.GetComponent<AzureBladeSkill>();
            if (azure != null) Object.Destroy(azure);

            var dune = player.GetComponent<DuneAssassinSkill>();
            if (dune == null)
            {
                dune = player.gameObject.AddComponent<DuneAssassinSkill>();
            }

            var btnSpecial = GameObject.Find("Btn_Special");
            if (btnSpecial != null)
            {
                var txt = btnSpecial.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "SANDSTORM X";

                var btn = btnSpecial.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        var d = player.GetComponent<DuneAssassinSkill>();
                        if (d != null) d.TriggerSpecialSkill();
                    });
                }
            }

            var mage = player.GetComponent<CelestialMageSkill>();
            if (mage != null) Object.Destroy(mage);

            Debug.Log("<color=yellow>[CHARACTER]</color> Đã áp dụng nhân vật SA MẠC THÍCH KHÁCH (DUNE ASSASSIN) với Tuyệt kỹ Cuồng Bão Sa Ảnh!");
        }
        else if (selectedChar == CharacterManager.CHAR_CELESTIAL_MAGE)
        {
            player.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.material = new Material(Shader.Find("Sprites/Default"));
            }

            var anim = player.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;

            var titan = player.GetComponent<TitanBerserkerSkill>();
            if (titan != null) Object.Destroy(titan);

            var sweep = player.GetComponent<SweepTheWeakSkill>();
            if (sweep != null) Object.Destroy(sweep);

            var ronin = player.GetComponent<GaleRoninSkill>();
            if (ronin != null) Object.Destroy(ronin);

            var azure = player.GetComponent<AzureBladeSkill>();
            if (azure != null) Object.Destroy(azure);

            var dune = player.GetComponent<DuneAssassinSkill>();
            if (dune != null) Object.Destroy(dune);

            var mage = player.GetComponent<CelestialMageSkill>();
            if (mage == null)
            {
                mage = player.gameObject.AddComponent<CelestialMageSkill>();
            }
            if (mage != null)
            {
                mage.baseOrbDamage = 26;
            }

            var btnSpecial = GameObject.Find("Btn_Special");
            if (btnSpecial != null)
            {
                var txt = btnSpecial.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "METEOR APOCALYPSE";

                var btn = btnSpecial.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        var cm = player.GetComponent<CelestialMageSkill>();
                        if (cm != null) cm.TriggerSpecialSkill();
                    });
                }
            }

            Debug.Log("<color=magenta>[CHARACTER]</color> Đã áp dụng nhân vật PHÁP SƯ TINH TÚ (CELESTIAL MAGE) với Tuyệt kỹ Thiên Thạch Ma Trận!");
        }
        else
        {
            player.transform.localScale = Vector3.one;
            var anim = player.GetComponent<Animator>();
#if UNITY_EDITOR
            var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Player/idle_down_0.controller");
            if (controller != null && anim != null)
            {
                anim.runtimeAnimatorController = controller;
            }
#endif
            var titan = player.GetComponent<TitanBerserkerSkill>();
            if (titan != null) Object.Destroy(titan);

            var sweep = player.GetComponent<SweepTheWeakSkill>();
            if (sweep != null) Object.Destroy(sweep);

            var ronin = player.GetComponent<GaleRoninSkill>();
            if (ronin != null) Object.Destroy(ronin);

            var azure = player.GetComponent<AzureBladeSkill>();
            if (azure != null) Object.Destroy(azure);

            var dune = player.GetComponent<DuneAssassinSkill>();
            if (dune != null) Object.Destroy(dune);

            var mage = player.GetComponent<CelestialMageSkill>();
            if (mage != null) Object.Destroy(mage);

            var pCombat = player.GetComponent<PlayerCombat>();
            if (pCombat != null)
            {
                pCombat.SetBaseAttackDamage(10, 10);
            }

            var btnSpecial = GameObject.Find("Btn_Special");
            if (btnSpecial != null)
            {
                var txt = btnSpecial.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "WHIRL WIND";
            }
        }

        // Kích hoạt hiệu ứng hào quang hạt pixel hắc ám khi chọn Player 4 (Titan Berserker)
        PlayerPixelAura.EnsureAuraForPlayer(player.gameObject, selectedChar == CharacterManager.CHAR_TITAN_BERSERKER);
    }
    public static void EnsurePlayerColliderAndCombat(PlayerController player, string selectedChar = "")
    {
        if (player == null) return;

        if (string.IsNullOrEmpty(selectedChar))
        {
            CharacterManager.EnsureInstance();
            selectedChar = CharacterManager.Instance.GetSelectedCharacterId();
        }

        var box = player.GetComponent<BoxCollider2D>();
        var cap = player.GetComponent<CapsuleCollider2D>();
        var rb = player.GetComponent<Rigidbody2D>();
        var sr = player.GetComponent<SpriteRenderer>();

        PhysicsMaterial2D slipMat = new PhysicsMaterial2D("PlayerSlipperyMat");
        slipMat.friction = 0f;
        slipMat.bounciness = 0f;

        if (sr != null)
        {
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 5000;
        }

        if (rb != null)
        {
            rb.sharedMaterial = slipMat;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (player.GetComponent<PlayerEnergySystem>() == null)
        {
            player.gameObject.AddComponent<PlayerEnergySystem>();
        }

        if (player.GetComponent<PlayerInventory>() == null)
        {
            player.gameObject.AddComponent<PlayerInventory>();
        }

        PlayerEquipmentVisual.EnsureExists(player.gameObject);

        var tPoint = player.transform.Find("AttackPoint");

        // Align Collider with rounded CapsuleCollider2D to prevent corner sticking
        if (cap == null && box != null)
        {
            Vector2 oldOffset = box.offset;
            Vector2 oldSize = box.size;
            UnityEngine.Object.Destroy(box);
            cap = player.gameObject.AddComponent<CapsuleCollider2D>();
            cap.offset = oldOffset;
            cap.size = oldSize;
        }
        else if (cap == null && box == null)
        {
            cap = player.gameObject.AddComponent<CapsuleCollider2D>();
        }

        if (cap != null) cap.sharedMaterial = slipMat;

        if (selectedChar == CharacterManager.CHAR_TITAN_BERSERKER)
        {
            // Titan Berserker (Minotaur): chiều cao ~1.4m, tâm ngực Y = 0.65f, collider bao trọn thân
            if (cap != null)
            {
                cap.offset = new Vector2(0f, 0.65f);
                cap.size = new Vector2(0.90f, 1.30f);
            }
            if (tPoint != null)
            {
                tPoint.localPosition = new Vector3(0f, 0.65f, 0f);
            }
        }
        else if (selectedChar == CharacterManager.CHAR_AZURE_BLADE)
        {
            // Azure Blade: 64x64 pixel art, pivot (0.5, 0.5)
            if (cap != null)
            {
                cap.offset = new Vector2(0f, 0f);
                cap.size = new Vector2(0.50f, 0.80f);
            }
            if (tPoint != null)
            {
                tPoint.localPosition = new Vector3(0f, 0.1f, 0f);
            }
        }
        else if (selectedChar == CharacterManager.CHAR_DUNE_ASSASSIN)
        {
            // Dune Assassin (Player 5): 36x39 sprite, pivot (0.5, 0.5), centered perfectly on body
            if (cap != null)
            {
                cap.offset = new Vector2(0f, 0f);
                cap.size = new Vector2(0.48f, 0.72f);
            }
            if (tPoint != null)
            {
                tPoint.localPosition = new Vector3(0f, 0.05f, 0f);
            }
        }
        else if (selectedChar == CharacterManager.CHAR_CELESTIAL_MAGE)
        {
            // Celestial Mage (Player 6): 64x64 floating sprite, centered
            if (cap != null)
            {
                cap.offset = new Vector2(0f, 0f);
                cap.size = new Vector2(0.48f, 0.75f);
            }
            if (tPoint != null)
            {
                tPoint.localPosition = new Vector3(0f, 0.1f, 0f);
            }
        }
        else if (selectedChar == CharacterManager.CHAR_GALE_RONIN || selectedChar == CharacterManager.CHAR_MAID)
        {
            // Maid / Ronin: pivot (0.5, 0.5), chan o y=-0.45, dau o y=+0.45
            if (cap != null)
            {
                cap.offset = new Vector2(0f, -0.05f);
                cap.size = new Vector2(0.50f, 0.75f);
            }
            if (tPoint != null)
            {
                tPoint.localPosition = new Vector3(0f, 0.05f, 0f);
            }
        }
        else
        {
            // Adventurer (Player 1): sprite 96x80, pivot (0.5, 0), chan o y=0.72, dau o y=1.94
            if (cap != null)
            {
                cap.offset = new Vector2(0f, 1.25f);
                cap.size = new Vector2(0.50f, 0.95f);
            }
            if (tPoint != null)
            {
                tPoint.localPosition = new Vector3(0f, 1.25f, 0f);
            }
        }

        HeroCostumeManager.ApplyCostumeVisual(player.gameObject, selectedChar);
    }

    public static void EnsureBreakables()
    {
        if (GameObject.Find("DungeonBreakables") != null) return;

        GameObject root = new GameObject("DungeonBreakables");
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene.Contains("Map2"))
        {
            // Cụm bình và hoa cỏ rải rác quanh thung lũng Map 2
            BreakableProp.Create(new Vector3(-4f, 1f, 0), BreakableProp.PropType.Pot, 0, root.transform);
            BreakableProp.Create(new Vector3(-2f, -2f, 0), BreakableProp.PropType.Pot, 1, root.transform);
            BreakableProp.Create(new Vector3(4f, 2f, 0), BreakableProp.PropType.Pot, 2, root.transform);
            BreakableProp.Create(new Vector3(6f, -3f, 0), BreakableProp.PropType.Pot, 3, root.transform);
            BreakableProp.Create(new Vector3(0f, 4f, 0), BreakableProp.PropType.Pot, 0, root.transform);

            BreakableProp.Create(new Vector3(-3f, 3f, 0), BreakableProp.PropType.Foliage, 0, root.transform);
            BreakableProp.Create(new Vector3(3f, -2f, 0), BreakableProp.PropType.Foliage, 1, root.transform);
            BreakableProp.Create(new Vector3(5f, 4f, 0), BreakableProp.PropType.Foliage, 2, root.transform);
            BreakableProp.Create(new Vector3(-5f, -4f, 0), BreakableProp.PropType.Foliage, 3, root.transform);
        }
        else
        {
            // Map 1 (SampleScene)
            BreakableProp.Create(new Vector3(-6f, 3f, 0), BreakableProp.PropType.Pot, 0, root.transform);
            BreakableProp.Create(new Vector3(-6f, -3f, 0), BreakableProp.PropType.Pot, 1, root.transform);
            BreakableProp.Create(new Vector3(-13f, -4f, 0), BreakableProp.PropType.Foliage, 0, root.transform);
            BreakableProp.Create(new Vector3(-4f, -4f, 0), BreakableProp.PropType.Foliage, 1, root.transform);

            // Corridor 1 & Room 2
            BreakableProp.Create(new Vector3(2f, 0.8f, 0), BreakableProp.PropType.Pot, 2, root.transform);
            BreakableProp.Create(new Vector3(5f, -0.6f, 0), BreakableProp.PropType.Pot, 3, root.transform);
            BreakableProp.Create(new Vector3(10f, 6f, 0), BreakableProp.PropType.Pot, 0, root.transform);
            BreakableProp.Create(new Vector3(26f, 6f, 0), BreakableProp.PropType.Pot, 1, root.transform);
            BreakableProp.Create(new Vector3(10f, -4f, 0), BreakableProp.PropType.Pot, 2, root.transform);
            BreakableProp.Create(new Vector3(26f, -4f, 0), BreakableProp.PropType.Pot, 3, root.transform);
            BreakableProp.Create(new Vector3(12f, 2f, 0), BreakableProp.PropType.Foliage, 2, root.transform);
            BreakableProp.Create(new Vector3(24f, 2f, 0), BreakableProp.PropType.Foliage, 3, root.transform);
        }

        Debug.Log("<color=cyan>[BREAKABLES]</color> Đã khởi tạo các bình gốm và bụi hoa cỏ chém vỡ được!");
    }

}

