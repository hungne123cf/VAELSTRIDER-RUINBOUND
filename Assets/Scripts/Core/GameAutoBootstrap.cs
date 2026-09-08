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
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "Map2_ValleyRuin")
        {
            // Dang o Map 2: Giu nguyen dia hinh thung lung Map 2, chi khoi tao UI va Upgrade
            PotionQuickSlotUI.EnsureUIExists();
            EnsureSpecialSkillButton();
            PlayerUpgradeManager.EnsureInstance();
            ShrineUpgradeUI.EnsureUIExists();
            Debug.Log("<color=green>[BOOTSTRAP]</color> Dang o Map 2 (Valley Ruin)! Giu nguyen toan bo dia hinh Map 2.");
            return;
        }

        // 0. Kien tao Ban Do Da Phong 4 phong (Multi-Room Dungeon) cho Map 1
        DungeonMapBuilder.BuildDungeon();

        // 1. Khoi tao nut Binh Mau (Potion Quick Slot) tren Canvas
        PotionQuickSlotUI.EnsureUIExists();

        // 2. Khoi tao nut Tuyet Ky Ruin Whirlwind tren Canvas
        EnsureSpecialSkillButton();

        // 3. Khoi tao He thong 3 dot quai + Boss Titan Wheel (WaveSpawner)
        EnsureWaveSpawner();

        // 4. Khoi tao Banner thong bao Wave tren UI (WaveUI)
        EnsureWaveUI();

        // 5. Khoi tao Ruong Kho Bau trong phong (TreasureChest)
        EnsureTreasureChest();

        // 6. He thong Ban Tho Nang Cap Co Dai (Ancient Shrine)
        EnsureAncientShrine();

        // 7. He thong Anh Sang URP 2D Dark Fantasy
        EnsureDungeonLighting();

        // 8. Dam bao Player Upgrade Manager va UI
        PlayerUpgradeManager.EnsureInstance();
        ShrineUpgradeUI.EnsureUIExists();

        Debug.Log("<color=cyan>[VAELSTRIDER BOOTSTRAP]</color> Tất cả hệ thống (Bàn thờ, Ánh sáng 2D, Rương, 3 Wave quái, Boss, Bình máu, Tuyệt kỹ Whirlwind, UI) đã sẵn sàng 100%!");
    }

    public static GameObject EnsureSpecialSkillButton()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
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
        bg.color = new Color(0.12f, 0.18f, 0.28f, 0.92f);

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

        // Nhãn phím PC (F)
        GameObject hintObj = new GameObject("KeyHint");
        hintObj.transform.SetParent(btnObj.transform, false);
        RectTransform hintRt = hintObj.AddComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(1, 0);
        hintRt.anchorMax = new Vector2(1, 0);
        hintRt.pivot = new Vector2(1, 0);
        hintRt.anchoredPosition = new Vector2(-6, 6);
        hintRt.sizeDelta = new Vector2(24, 20);
        var hintTmp = hintObj.AddComponent<TextMeshProUGUI>();
        hintTmp.text = "[F]";
        hintTmp.fontSize = 12;
        hintTmp.fontStyle = FontStyles.Bold;
        hintTmp.color = new Color(0.55f, 0.8f, 1f, 0.85f);
        hintTmp.alignment = TextAlignmentOptions.BottomRight;
        hintTmp.raycastTarget = false;

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

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("WaveBanner");
        if (existing != null)
        {
            waveUI = existing.GetComponent<WaveUI>();
            if (waveUI == null) waveUI = existing.gameObject.AddComponent<WaveUI>();
            return waveUI;
        }

        GameObject bannerObj = new GameObject("WaveBanner");
        bannerObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = bannerObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -80f);
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

    public static AncientShrine EnsureAncientShrine()
    {
        var shrine = Object.FindAnyObjectByType<AncientShrine>();
        if (shrine != null) return shrine;

        Vector3 spawnPos = new Vector3(-3.2f, 1.6f, 0f);
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            spawnPos = player.transform.position + new Vector3(-3.2f, 1.6f, 0f);
        }

        shrine = AncientShrine.SpawnShrine(spawnPos);
        PlayerUpgradeManager.EnsureInstance();
        ShrineUpgradeUI.EnsureUIExists();

        Debug.Log("<color=green>[BOOTSTRAP]</color> Da tao Ancient Shrine va UI Nang Cap!");
        return shrine;
    }

    public static DungeonLightingManager EnsureDungeonLighting()
    {
        return DungeonLightingManager.EnsureInstance();
    }
}
