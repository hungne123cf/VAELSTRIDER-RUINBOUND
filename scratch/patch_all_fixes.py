import os

def patch_augment_manager():
    path = "Assets/Scripts/Player/AugmentManager.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    # 1. Patch DemonicPact in ApplyAugment to call PlayerHealth.ApplySacrificeHealth
    old_pact = """            case AugmentDatabase.AugmentType.DemonicPact:
                hasDemonicPact = true;
                augBonusAtk += 45;
                augBonusSpeed += 0.20f;
                augBonusLifesteal += 0.15f;
                augBonusSpellVamp += 0.15f;
                // Hy sinh 15% Máu tối đa
                int sacrificeHp = PlayerHealth.Instance != null ? Mathf.RoundToInt(PlayerHealth.Instance.MaxHealth * 0.15f) : 60;
                augBonusHp -= sacrificeHp;
                FlushStatsToCombat();
                FlushStatsToController();
                FlushStatsToHealth();
                break;"""

    new_pact = """            case AugmentDatabase.AugmentType.DemonicPact:
                hasDemonicPact = true;
                augBonusAtk += 45;
                augBonusSpeed += 0.20f;
                augBonusLifesteal += 0.15f;
                augBonusSpellVamp += 0.15f;
                FlushStatsToCombat();
                FlushStatsToController();
                if (PlayerHealth.Instance != null)
                {
                    PlayerHealth.Instance.ApplySacrificeHealth(0.15f, "Khế Ước Quỷ Dữ");
                }
                else
                {
                    augBonusHp -= 60;
                    FlushStatsToHealth();
                }
                break;"""

    if old_pact in content:
        content = content.replace(old_pact, new_pact, 1)
        print("AugmentManager.cs (DemonicPact) patched!")
    else:
        print("AugmentManager.cs DemonicPact target NOT found!")

    # 2. Add the 8 new augment cases before Timed buffs
    old_timed = """            // ── Timed buffs (Cộng dồn thời gian nếu nhận lại cùng Lõi) ────────────"""

    new_cases = """            // ─── 8 Lõi Nâng Cấp Mới & Cursed Double-Edged ───
            case AugmentDatabase.AugmentType.PercentCurrentHpExecute:
                // Trảm Sát Trảm Kích: Đòn đánh gây thêm 50% HP hiện tại của mục tiêu
                break;

            case AugmentDatabase.AugmentType.GoldenSanctuary:
                // Kim Thân Bất Hoại: Mỗi 10s nhận hiệu ứng miễn nhiễm khống chế
                FindAnyObjectByType<PlayerController>()?.EnableGoldenSanctuary();
                break;

            case AugmentDatabase.AugmentType.CursedMight:
                // Khế Ước Cuồng Nộ: Nhận +100 Sát Thương Đòn Đánh, nhưng Quái & Boss tăng thêm +50% Máu
                augBonusAtk += 100;
                FlushStatsToCombat();
                EnemyHealthHelper.MultiplyHealthOfAllActiveEnemies(1.50f);
                Debug.Log("<color=purple>[CURSED MIGHT]</color> Nhận +100 ATK! Toàn bộ Quái & Boss tăng +50% HP!");
                break;

            case AugmentDatabase.AugmentType.IroncladPact:
                // Minh Ước Hắc Thiết: Nhận +50 Giáp, nhưng Quái & Lính nhận thêm +5% Miễn Thương
                hasIroncladPact = true;
                augBonusDef += 50;
                FlushStatsToCombat();
                Debug.Log("<color=cyan>[IRONCLAD PACT]</color> Nhận +50 Giáp! Quái & Lính nhận thêm +5% Miễn Thương!");
                break;

            case AugmentDatabase.AugmentType.ChronoStasis:
                // Đầm Lầy Thời Không: Quái bị làm chậm 35%, bản thân giảm 50% tốc chạy
                augBonusSpeed -= 0.50f;
                FlushStatsToController();
                EnemyChronoSlowEffect.ApplyToAllEnemies();
                Debug.Log("<color=teal>[CHRONO STASIS]</color> Quái bị làm chậm 35%! Tốc chạy bản thân giảm 50%!");
                break;

            case AugmentDatabase.AugmentType.BloodTitheBlade:
                // Ma Kiếm Huyết Tế: Nhận +40 ATK, +40% Hút Máu, nhưng mất 1% HP mỗi giây
                hasBloodTitheBlade = true;
                bloodTitheTimer = 0f;
                augBonusAtk += 40;
                augBonusLifesteal += 0.40f;
                augBonusSpellVamp += 0.40f;
                FlushStatsToCombat();
                Debug.Log("<color=red>[BLOOD TITHE BLADE]</color> Nhận +40 ATK, +40% Hút Máu! Ma kiếm bắt đầu rút 1% HP/giây!");
                break;

            case AugmentDatabase.AugmentType.GlassCannonTitan:
                // Bản Thể Thủy Tinh: +75% Crit, +80% Sát thương Crit, nhưng nhận thêm +30% Sát thương
                augBonusCrit += 0.75f;
                augBonusCritDmg += 0.80f;
                FlushStatsToCombat();
                Debug.Log("<color=yellow>[GLASS CANNON]</color> Nhận +75% Crit & +80% Crit DMG! Nhận thêm +30% sát thương từ địch!");
                break;

            case AugmentDatabase.AugmentType.PhantomRush:
                // Bóng Ma Cuồng Nộ: Nhận +50% CDR, nhưng giảm 25% Máu tối đa
                augBonusCDR += 0.50f;
                FlushStatsToCombat();
                if (PlayerHealth.Instance != null)
                {
                    PlayerHealth.Instance.ApplySacrificeHealth(0.25f, "Bóng Ma Cuồng Nộ");
                }
                else
                {
                    augBonusHp -= 80;
                    FlushStatsToHealth();
                }
                Debug.Log("<color=purple>[PHANTOM RUSH]</color> Nhận +50% CDR! Đã hy sinh 25% Máu tối đa!");
                break;

            // ── Timed buffs (Cộng dồn thời gian nếu nhận lại cùng Lõi) ────────────"""

    if old_timed in content:
        content = content.replace(old_timed, new_cases, 1)
        print("AugmentManager.cs (New Augment cases) patched!")
    else:
        print("AugmentManager.cs Timed buffs target NOT found!")

    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

def patch_enemy_health_helper():
    path = "Assets/Scripts/Combat/EnemyHealthHelper.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    target = """    public static void MultiplyHealthOfAllActiveEnemies(float mult)
    {
        var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in all)
        {
            if (mb is IEnemyHealth eh && mb != null)
            {
                eh.MultiplyHealth(mult);
            }
        }
    }
}"""

    replacement = """    public static void MultiplyHealthOfAllActiveEnemies(float mult)
    {
        var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in all)
        {
            if (mb is IEnemyHealth eh && mb != null)
            {
                eh.MultiplyHealth(mult);
            }
        }
    }

    /// <summary>
    /// Kích hoạt khi quái mới xuất hiện trong trận đấu (Áp dụng bùa hại CursedMight nếu người chơi đã chọn)
    /// </summary>
    public static void OnEnemySpawned(GameObject enemy)
    {
        if (enemy == null) return;
        if (AugmentManager.Instance != null && AugmentManager.Instance.HasAugment(AugmentDatabase.AugmentType.CursedMight))
        {
            var eh = enemy.GetComponent<IEnemyHealth>() ?? enemy.GetComponentInParent<IEnemyHealth>();
            if (eh != null)
            {
                eh.MultiplyHealth(1.50f);
            }
        }
    }
}"""

    if target in content:
        content = content.replace(target, replacement, 1)
        with open(path, "w", encoding="utf-8") as f:
            f.write(content)
        print("EnemyHealthHelper.cs patched!")
    else:
        print("EnemyHealthHelper.cs target NOT found!")

def patch_wave_spawners():
    path1 = "Assets/Scripts/Combat/WaveSpawner.cs"
    with open(path1, "r", encoding="utf-8") as f:
        content1 = f.read()

    t1 = """                GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                newEnemy.SetActive(true);
                currentWaveEnemies.Add(newEnemy);

                IgnorePlayerCollision(newEnemy);"""

    r1 = """                GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                newEnemy.SetActive(true);
                currentWaveEnemies.Add(newEnemy);

                IgnorePlayerCollision(newEnemy);
                EnemyHealthHelper.OnEnemySpawned(newEnemy);"""

    t2 = """                    GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                    newEnemy.SetActive(true);
                    currentWaveEnemies.Add(newEnemy);

                    IgnorePlayerCollision(newEnemy);"""

    r2 = """                    GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                    newEnemy.SetActive(true);
                    currentWaveEnemies.Add(newEnemy);

                    IgnorePlayerCollision(newEnemy);
                    EnemyHealthHelper.OnEnemySpawned(newEnemy);"""

    if t1 in content1:
        content1 = content1.replace(t1, r1, 1)
    if t2 in content1:
        content1 = content1.replace(t2, r2, 1)

    with open(path1, "w", encoding="utf-8") as f:
        f.write(content1)
    print("WaveSpawner.cs patched!")

    path2 = "Assets/Scripts/Combat/Map3WaveSpawner.cs"
    with open(path2, "r", encoding="utf-8") as f:
        content2 = f.read()

    # Add OnEnemySpawned to SpawnGoblin and SpawnSkeleton
    tg = """        g.AddComponent<AutoDepthSort>();
        g.AddComponent<GoblinAI>();
    }"""
    rg = """        g.AddComponent<AutoDepthSort>();
        g.AddComponent<GoblinAI>();
        EnemyHealthHelper.OnEnemySpawned(g);
    }"""
    if tg in content2:
        content2 = content2.replace(tg, rg, 1)

    ts = """        s.AddComponent<AutoDepthSort>();
        s.AddComponent<SkeletonAI>();
    }"""
    rs = """        s.AddComponent<AutoDepthSort>();
        s.AddComponent<SkeletonAI>();
        EnemyHealthHelper.OnEnemySpawned(s);
    }"""
    if ts in content2:
        content2 = content2.replace(ts, rs, 1)

    with open(path2, "w", encoding="utf-8") as f:
        f.write(content2)
    print("Map3WaveSpawner.cs patched!")

def patch_player_controller():
    path = "Assets/Scripts/Player/PlayerController.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    target = """    void Start()
    {
        GameAutoBootstrap.ApplySelectedCharacterToPlayer();
    }"""

    replacement = """    void Start()
    {
        GameAutoBootstrap.ApplySelectedCharacterToPlayer();
        if (AugmentManager.Instance != null && AugmentManager.Instance.HasAugment(AugmentDatabase.AugmentType.GoldenSanctuary))
        {
            EnableGoldenSanctuary();
        }
    }"""

    if target in content:
        content = content.replace(target, replacement, 1)
        with open(path, "w", encoding="utf-8") as f:
            f.write(content)
        print("PlayerController.cs patched!")
    else:
        print("PlayerController.cs target NOT found!")

def patch_game_over_ui():
    path = "Assets/Scripts/UI/GameOverUI.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    t1 = """        if (subtitleText != null) 
        {
            subtitleText.text = isVi ? "Sinh mệnh đã cạn. Mọi kinh nghiệm và lõi ấn chỉ lưu giữ trong chuyến đi này." : "Your journey ends here. Battle levels and augments only exist within this run.";
            var sRt = subtitleText.rectTransform;
            if (sRt != null)
            {
                sRt.sizeDelta = new Vector2(620f, 32f);
                sRt.anchoredPosition = new Vector2(0f, 110f);
            }
        }"""

    r1 = """        if (subtitleText != null) 
        {
            subtitleText.text = isVi ? "Sinh mệnh đã cạn. Mọi kinh nghiệm và lõi ấn chỉ lưu giữ trong chuyến đi này." : "Your journey ends here. Battle levels and augments only exist within this run.";
            var sRt = subtitleText.rectTransform;
            if (sRt != null)
            {
                sRt.sizeDelta = new Vector2(780f, 44f);
                sRt.anchoredPosition = new Vector2(0f, 136f);
            }
        }"""

    if t1 in content:
        content = content.replace(t1, r1, 1)

    t2 = """        // Title Bottom Symmetric Divider (No overlapping flanking wings)
        CreateOrnamentDivider(boxObj.transform, "TitleDivider", new Vector2(0, 205), new Vector2(740, 12), new Color(1f, 0.9f, 0.65f, 0.85f));

        // ─── Subtitle ──────────────────────────────────────────────────────────
        GameObject subObj = new GameObject("Subtitle", typeof(RectTransform));
        subObj.layer = 5;
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, 165);
        subRect.sizeDelta = new Vector2(800, 38);
        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = "Sinh mệnh đã cạn. Mọi kinh nghiệm và lõi ấn chỉ lưu giữ trong chuyến đi này.";
        subTmp.fontSize = 19f;
        subTmp.color = new Color(0.75f, 0.78f, 0.86f);
        subTmp.alignment = TextAlignmentOptions.Center;
        Instance.subtitleText = subTmp;

        // ─── Run Summary Indicator ─────────────────────────────────────────────
        GameObject statusObj = new GameObject("RespawnStatus", typeof(RectTransform));
        statusObj.layer = 5;
        statusObj.transform.SetParent(boxObj.transform, false);
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchoredPosition = new Vector2(0, 98);
        statusRect.sizeDelta = new Vector2(800, 48);
        var statusTmp = statusObj.AddComponent<TextMeshProUGUI>();
        statusTmp.fontSize = 24f;
        statusTmp.fontStyle = FontStyles.Bold;
        statusTmp.alignment = TextAlignmentOptions.Center;
        Instance.respawnStatusText = statusTmp;

        // Section Divider before buttons
        CreateOrnamentDivider(boxObj.transform, "SectionDivider", new Vector2(0, 55), new Vector2(740, 10), ColorDividerLine);

        // ─── 1. Button: START NEW RUN (LEVEL 1) ────────────────────────────────
        Instance.restartButton = CreateFramedButton(boxObj.transform, "Btn_RestartMap", "HỒI SINH & CHƠI LẠI RUN MỚI", new Vector2(0, -32), new Vector2(720, 92), ColorBtnRestartBg, ColorBtnFrame, 28, out Instance.restartBtnText);

        // ─── 2. Button: RETURN TO SANCTUARY ────────────────────────────────────
        Instance.mainMenuButton = CreateFramedButton(boxObj.transform, "Btn_MainMenu", "VỀ SẢNH CHÍNH", new Vector2(0, -145), new Vector2(720, 92), ColorBtnMainMenuBg, ColorBtnFrame, 28, out Instance.mainMenuBtnText);"""

    r2 = """        // Title Bottom Symmetric Divider
        CreateOrnamentDivider(boxObj.transform, "TitleDivider", new Vector2(0, 172), new Vector2(740, 10), new Color(1f, 0.9f, 0.65f, 0.85f));

        // ─── Subtitle ──────────────────────────────────────────────────────────
        GameObject subObj = new GameObject("Subtitle", typeof(RectTransform));
        subObj.layer = 5;
        subObj.transform.SetParent(boxObj.transform, false);
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, 136);
        subRect.sizeDelta = new Vector2(780, 44);
        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = "Sinh mệnh đã cạn. Mọi kinh nghiệm và lõi ấn chỉ lưu giữ trong chuyến đi này.";
        subTmp.fontSize = 18f;
        subTmp.color = new Color(0.75f, 0.78f, 0.86f);
        subTmp.alignment = TextAlignmentOptions.Center;
        Instance.subtitleText = subTmp;

        // ─── Run Summary Indicator ─────────────────────────────────────────────
        GameObject statusObj = new GameObject("RespawnStatus", typeof(RectTransform));
        statusObj.layer = 5;
        statusObj.transform.SetParent(boxObj.transform, false);
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchoredPosition = new Vector2(0, 78);
        statusRect.sizeDelta = new Vector2(780, 40);
        var statusTmp = statusObj.AddComponent<TextMeshProUGUI>();
        statusTmp.fontSize = 24f;
        statusTmp.fontStyle = FontStyles.Bold;
        statusTmp.alignment = TextAlignmentOptions.Center;
        Instance.respawnStatusText = statusTmp;

        // Section Divider before buttons
        CreateOrnamentDivider(boxObj.transform, "SectionDivider", new Vector2(0, 42), new Vector2(740, 10), ColorDividerLine);

        // ─── 1. Button: START NEW RUN (LEVEL 1) ────────────────────────────────
        Instance.restartButton = CreateFramedButton(boxObj.transform, "Btn_RestartMap", "HỒI SINH & CHƠI LẠI RUN MỚI", new Vector2(0, -32), new Vector2(720, 84), ColorBtnRestartBg, ColorBtnFrame, 28, out Instance.restartBtnText);

        // ─── 2. Button: RETURN TO SANCTUARY ────────────────────────────────────
        Instance.mainMenuButton = CreateFramedButton(boxObj.transform, "Btn_MainMenu", "VỀ SẢNH CHÍNH", new Vector2(0, -145), new Vector2(720, 84), ColorBtnMainMenuBg, ColorBtnFrame, 28, out Instance.mainMenuBtnText);"""

    if t2 in content:
        content = content.replace(t2, r2, 1)

    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
    print("GameOverUI.cs patched!")

def patch_settings_ui():
    path = "Assets/Scripts/UI/SettingsUI.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    target = """        // Showcase Container Card
        controlsCard = new GameObject("ControlsCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        controlsCard.transform.SetParent(pageControls.transform, false);
        RectTransform ccRt = controlsCard.GetComponent<RectTransform>();
        ccRt.anchorMin = new Vector2(0f, 1f);
        ccRt.anchorMax = new Vector2(0f, 1f);
        ccRt.pivot = new Vector2(0f, 1f);
        ccRt.anchoredPosition = new Vector2(25f, -76f);
        ccRt.sizeDelta = new Vector2(670f, 235f);

        Image ccImg = controlsCard.GetComponent<Image>();
        ccImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        ccImg.type = Image.Type.Sliced;
        ccImg.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
        WhiteIconLoader.ApplyGothicFrame(controlsCard, new Color(0.35f, 0.55f, 0.75f, 0.55f), 0.45f);

        // Title inside Card
        controlsCardTitle = CreateLabel(controlsCard.transform, "CardTitle", "<b>TUY CH?NH NUT B?M TR?C TI?P TRONG TR?N D?U</b>", new Vector2(22f, -16f), new Vector2(626f, 26f));
        controlsCardTitle.fontSize = 16.5f;
        controlsCardTitle.color = new Color(1f, 0.88f, 0.45f);

        // Description inside Card
        controlsCardDesc = CreateLabel(controlsCard.transform, "CardDesc", "Ch?m v ko t? do C?n di chuy?n (Joystick), Nt Dnh, Lu?t & Tuy?t k? tr?c ti?p trn mn hnh tr?n d?u. B?n c th? phng to, thu nh? v ch?nh d? m? cc nt ngay t?i d d? v?a v?n nh?t v?i tay c?m c?a mnh.\\n\\nV? tr v kch thu?c m?i s? du?c luu t? d?ng v p d?ng vinh vi?n cho t?t c? cc tr?n d?u!", new Vector2(22f, -46f), new Vector2(626f, 85f));
        controlsCardDesc.fontSize = 13.5f;
        controlsCardDesc.color = new Color(0.82f, 0.88f, 0.95f, 0.9f);

        // 2 Big Action Buttons inside Card
        customizeLayoutBtn = CreateStyledButton(controlsCard.transform, "Btn_CustomizeLayout", new Vector2(22f, -148f), new Vector2(370f, 54f), new Color(0.10f, 0.38f, 0.42f), out customizeLayoutBtnText);
        customizeLayoutBtnText.fontSize = 15.5f;

        resetControlsDefaultBtn = CreateStyledButton(controlsCard.transform, "Btn_ResetControlsDefault", new Vector2(405f, -148f), new Vector2(243f, 54f), new Color(0.26f, 0.14f, 0.18f), out resetControlsDefaultBtnText);
        resetControlsDefaultBtnText.fontSize = 15f;

        // Bottom Tip below Card
        controlsCardTip = CreateLabel(pageControls.transform, "CardTip", "<i><color=#88A0C0>* Tr?n d?u s? t? d?ng t?m d?ng an ton trong lc b?n can ch?nh v? tr v kch thu?c cc nt.</color></i>", new Vector2(25f, -330f), new Vector2(670f, 24f));
        controlsCardTip.fontSize = 13.5f;"""

    # We do a more flexible replacement based on lines 417-452
    idx1 = content.find("controlsCard = new GameObject(\"ControlsCard\"")
    idx2 = content.find("pageGameData = CreatePage(contentPanel.transform", idx1)

    if idx1 != -1 and idx2 != -1:
        replacement = """controlsCard = new GameObject("ControlsCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        controlsCard.transform.SetParent(pageControls.transform, false);
        RectTransform ccRt = controlsCard.GetComponent<RectTransform>();
        ccRt.anchorMin = new Vector2(0f, 1f);
        ccRt.anchorMax = new Vector2(0f, 1f);
        ccRt.pivot = new Vector2(0f, 1f);
        ccRt.anchoredPosition = new Vector2(25f, -76f);
        ccRt.sizeDelta = new Vector2(670f, 275f);

        Image ccImg = controlsCard.GetComponent<Image>();
        ccImg.sprite = SoulslikeHUDStyler.GetDarkFantasyPanelBg();
        ccImg.type = Image.Type.Sliced;
        ccImg.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
        WhiteIconLoader.ApplyGothicFrame(controlsCard, new Color(0.35f, 0.55f, 0.75f, 0.55f), 0.45f);

        // Title inside Card
        controlsCardTitle = CreateLabel(controlsCard.transform, "CardTitle", "<b>TÙY CHỈNH NÚT BẤM TRỰC TIẾP TRONG TRẬN ĐẤU</b>", new Vector2(22f, -16f), new Vector2(626f, 26f));
        controlsCardTitle.fontSize = 16.5f;
        controlsCardTitle.color = new Color(1f, 0.88f, 0.45f);

        // Description inside Card
        controlsCardDesc = CreateLabel(controlsCard.transform, "CardDesc", "Chạm và kéo tự do Cần di chuyển (Joystick), Nút Đánh, Lướt & Tuyệt kỹ trực tiếp trên màn hình trận đấu.\\n\\nVị trí và kích thước mới sẽ được lưu tự động và áp dụng vĩnh viễn cho tất cả các trận đấu!", new Vector2(22f, -44f), new Vector2(626f, 75f));
        controlsCardDesc.fontSize = 13.5f;
        controlsCardDesc.color = new Color(0.82f, 0.88f, 0.95f, 0.9f);

        // 2 Big Action Buttons inside Card (Xếp dọc toàn hàng ngang 626px cho chữ to rõ, không bị cụt ba chấm)
        customizeLayoutBtn = CreateStyledButton(controlsCard.transform, "Btn_CustomizeLayout", new Vector2(22f, -130f), new Vector2(626f, 56f), new Color(0.10f, 0.38f, 0.42f), out customizeLayoutBtnText);
        customizeLayoutBtnText.fontSize = 16.5f;

        resetControlsDefaultBtn = CreateStyledButton(controlsCard.transform, "Btn_ResetControlsDefault", new Vector2(22f, -198f), new Vector2(626f, 52f), new Color(0.26f, 0.14f, 0.18f), out resetControlsDefaultBtnText);
        resetControlsDefaultBtnText.fontSize = 16f;

        // Bottom Tip below Card
        controlsCardTip = CreateLabel(pageControls.transform, "CardTip", "<i><color=#88A0C0>* Trận đấu sẽ tự động tạm dừng an toàn trong lúc bạn căn chỉnh vị trí và kích thước các nút.</color></i>", new Vector2(25f, -365f), new Vector2(670f, 24f));
        controlsCardTip.fontSize = 13.5f;

        // --- PAGE 3: GAME DATA & SYSTEM ---
        """
        content = content[:idx1] + replacement + content[idx2:]
        with open(path, "w", encoding="utf-8") as f:
            f.write(content)
        print("SettingsUI.cs patched!")
    else:
        print("SettingsUI.cs indices NOT found!")

def patch_augment_hud_bar():
    path = "Assets/Scripts/UI/AugmentHUDBar.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    # Increase card height and medallion size in PopulateOverviewContent
    t_card = """            var le = cardObj.GetComponent<LayoutElement>();
            le.preferredHeight = 72f;
            le.minHeight = 72f;"""
    r_card = """            var le = cardObj.GetComponent<LayoutElement>();
            le.preferredHeight = 84f;
            le.minHeight = 84f;"""
    if t_card in content:
        content = content.replace(t_card, r_card, 1)

    t_medal = """            medalRt.anchoredPosition = new Vector2(12f, 0f);
            medalRt.sizeDelta = new Vector2(46f, 46f);"""
    r_medal = """            medalRt.anchoredPosition = new Vector2(12f, 0f);
            medalRt.sizeDelta = new Vector2(52f, 52f);"""
    if t_medal in content:
        content = content.replace(t_medal, r_medal, 1)

    t_text = """            var textTmp = textObj.GetComponent<TextMeshProUGUI>();
            string rankStr = rank > 1 ? $" <color=#FBBF24>Lv.{rank}</color>" : "";
            textTmp.text = $"<size=15><b><color=#{tierHex}>{tierName} - {typeName}</color>  <color=#FFFFFF>{title}</color>{rankStr}</b></size>\\n<size=12><color=#CBD5E1>{desc}</color></size>";"""

    r_text = """            var textTmp = textObj.GetComponent<TextMeshProUGUI>();
            string rankStr = rank > 1 ? $" <color=#FBBF24>Lv.{rank}</color>" : "";
            textTmp.text = $"<size=16.5><b><color=#{tierHex}>{tierName} - {typeName}</color>  <color=#FFFFFF>{title}</color>{rankStr}</b></size>\\n<size=13.5><color=#CBD5E1>{desc}</color></size>";"""
    if t_text in content:
        content = content.replace(t_text, r_text, 1)

    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
    print("AugmentHUDBar.cs patched!")

def patch_level_up_choice_ui():
    path = "Assets/Scripts/UI/LevelUpChoiceUI.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    t_font = """        descTMP.fontSizeMin = 11f;
        descTMP.fontSizeMax = 15.5f;"""
    r_font = """        descTMP.fontSizeMin = 13f;
        descTMP.fontSizeMax = 17f;"""
    if t_font in content:
        content = content.replace(t_font, r_font, 1)
        with open(path, "w", encoding="utf-8") as f:
            f.write(content)
        print("LevelUpChoiceUI.cs patched!")
    else:
        print("LevelUpChoiceUI.cs target NOT found!")

if __name__ == "__main__":
    patch_augment_manager()
    patch_enemy_health_helper()
    patch_wave_spawners()
    patch_player_controller()
    patch_game_over_ui()
    patch_settings_ui()
    patch_augment_hud_bar()
    patch_level_up_choice_ui()

