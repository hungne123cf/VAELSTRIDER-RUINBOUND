using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public static class GameSetupTools
{
    [MenuItem("Vaelstrider/Spawn Treasure Chest in Room")]
    [MenuItem("GameObject/Vaelstrider/Treasure Chest", false, 10)]
    public static void SpawnTreasureChest()
    {
        GameObject chest = new GameObject("TreasureChest");

        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            chest.transform.position = player.transform.position + new Vector3(2f, 1f, 0);
        }
        else
        {
            chest.transform.position = Vector3.zero;
        }

        var sr = chest.AddComponent<SpriteRenderer>();
        var closedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/chest_closed.png");
        sr.sprite = closedSprite;
        sr.sortingOrder = 5000;

        var col = chest.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.85f, 0.7f);
        col.offset = new Vector2(0f, 0.35f);
        col.isTrigger = true;

        var chestComp = chest.AddComponent<TreasureChest>();

        Selection.activeGameObject = chest;
        Undo.RegisterCreatedObjectUndo(chest, "Spawn Treasure Chest");

        Debug.Log("<color=green>[VAELSTRIDER]</color> Đã tạo Rương Kho Báu tại vị trí: " + chest.transform.position);
    }

    [MenuItem("Vaelstrider/Setup Potion Quick Slot on UI")]
    [MenuItem("GameObject/Vaelstrider/Potion Quick Slot UI", false, 11)]
    public static void SetupPotionQuickSlot()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Không tìm thấy Canvas trong Scene!");
            return;
        }

        Transform existing = canvas.transform.Find("Btn_Potion");
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("<color=yellow>[VAELSTRIDER]</color> Nút Btn_Potion đã có sẵn trên Canvas!");
            return;
        }

        GameObject btnObj = new GameObject("Btn_Potion");
        btnObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-220, 360);
        rect.sizeDelta = new Vector2(75, 75);

        Image bgImage = btnObj.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.12f, 0.18f, 0.88f);

        Button button = btnObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        button.colors = colors;

        GameObject iconObj = new GameObject("PotionIcon");
        iconObj.transform.SetParent(btnObj.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0, 3);
        iconRect.sizeDelta = new Vector2(50, 50);

        Image iconImage = iconObj.AddComponent<Image>();
        Sprite potionSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/health_potion.png");
        if (potionSprite != null) iconImage.sprite = potionSprite;
        iconImage.raycastTarget = false;

        GameObject badgeObj = new GameObject("AmountText");
        badgeObj.transform.SetParent(btnObj.transform, false);

        RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1, 0);
        badgeRect.anchorMax = new Vector2(1, 0);
        badgeRect.pivot = new Vector2(1, 0);
        badgeRect.anchoredPosition = new Vector2(-4, 4);
        badgeRect.sizeDelta = new Vector2(40, 24);

        TextMeshProUGUI tmp = badgeObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "x0";
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.raycastTarget = false;

        btnObj.AddComponent<PotionQuickSlotUI>();

        Selection.activeGameObject = btnObj;
        Undo.RegisterCreatedObjectUndo(btnObj, "Create Potion Quick Slot");

        Debug.Log("<color=green>[VAELSTRIDER]</color> Đã tạo thành công Nút Bấm Bình Máu (Btn_Potion) trên Canvas!");
    }

    [MenuItem("Vaelstrider/Setup Victory Portal & UI")]
    public static void SetupVictoryPortalAndUI()
    {
        // 1. Tao RuinPortal trong Scene neu chua co
        RuinPortal existingPortal = Object.FindAnyObjectByType<RuinPortal>(FindObjectsInactive.Include);
        if (existingPortal == null)
        {
            GameObject portalObj = new GameObject("RuinPortal");
            
            // Dat o vi tri WaveSpawner hoac giua phong
            var spawner = Object.FindAnyObjectByType<WaveSpawner>();
            if (spawner != null)
            {
                portalObj.transform.position = spawner.transform.position;
            }
            else
            {
                portalObj.transform.position = Vector3.zero;
            }

            var sr = portalObj.AddComponent<SpriteRenderer>();
            var portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/ruin_portal.png");
            sr.sprite = portalSprite;
            sr.sortingOrder = 4900;

            var col = portalObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.85f;

            portalObj.AddComponent<RuinPortal>();
            Undo.RegisterCreatedObjectUndo(portalObj, "Create Ruin Portal");
            Debug.Log("<color=green>[VAELSTRIDER]</color> Đã tạo Cổng Dịch Chuyển (RuinPortal) trong phòng!");
        }

        // 2. Tao VictoryUI tren Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Không tìm thấy Canvas!");
            return;
        }

        Transform existingPanel = canvas.transform.Find("VictoryPanel");
        if (existingPanel != null)
        {
            Selection.activeGameObject = existingPanel.gameObject;
            Debug.Log("<color=yellow>[VAELSTRIDER]</color> VictoryPanel đã có sẵn!");
            return;
        }

        // VictoryPanel Root (Overlay den mo toan man hinh)
        GameObject panelObj = new GameObject("VictoryPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.03f, 0.08f, 0.92f);

        CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        VictoryUI victoryComp = panelObj.AddComponent<VictoryUI>();

        // Card Khung chua noi dung
        GameObject cardObj = new GameObject("CardWindow");
        cardObj.transform.SetParent(panelObj.transform, false);

        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(540, 420);

        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.11f, 0.09f, 0.16f, 0.98f);

        // Header Title
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(cardObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 150);
        titleRect.sizeDelta = new Vector2(500, 50);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "REALM PURIFIED!";
        titleTmp.fontSize = 36;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(1f, 0.85f, 0.2f);
        titleTmp.alignment = TextAlignmentOptions.Center;

        // Subtitle
        GameObject subObj = new GameObject("SubtitleText");
        subObj.transform.SetParent(cardObj.transform, false);
        RectTransform subRect = subObj.AddComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, 115);
        subRect.sizeDelta = new Vector2(480, 30);
        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = "The ancient ruins have been cleansed of corruption.";
        subTmp.fontSize = 15;
        subTmp.color = new Color(0.75f, 0.75f, 0.85f);
        subTmp.alignment = TextAlignmentOptions.Center;

        // Stats Container
        GameObject statsContainer = new GameObject("StatsContainer");
        statsContainer.transform.SetParent(cardObj.transform, false);
        RectTransform statsRect = statsContainer.AddComponent<RectTransform>();
        statsRect.anchoredPosition = new Vector2(0, 20);
        statsRect.sizeDelta = new Vector2(440, 140);

        // Row 1: Time
        var timeTmp = CreateStatRow(statsContainer.transform, "TIME ELAPSED", "00:00", 45);
        // Row 2: Enemies
        var enemiesTmp = CreateStatRow(statsContainer.transform, "ENEMIES SLAIN", "0 Slain", 15);
        // Row 3: Shards
        var shardsTmp = CreateStatRow(statsContainer.transform, "AETHEL SHARDS", "0 Shards", -15);
        // Row 4: Rank
        var rankTmp = CreateStatRow(statsContainer.transform, "CLEAR RANK", "S RANK", -45, new Color(1f, 0.85f, 0.2f));

        // Buttons Container
        GameObject btnContainer = new GameObject("ButtonsContainer");
        btnContainer.transform.SetParent(cardObj.transform, false);
        RectTransform btnRect = btnContainer.AddComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -145);
        btnRect.sizeDelta = new Vector2(460, 60);

        Button playAgainBtn = CreateButton(btnContainer.transform, "Btn_PlayAgain", "PLAY AGAIN", new Vector2(-115, 0), new Color(0.25f, 0.35f, 0.7f));
        Button nextRealmBtn = CreateButton(btnContainer.transform, "Btn_NextRealm", "NEXT REALM", new Vector2(115, 0), new Color(0.85f, 0.45f, 0.15f));

        // Serialized Fields Binding
        SerializedObject so = new SerializedObject(victoryComp);
        so.FindProperty("canvasGroup").objectReferenceValue = cg;
        so.FindProperty("timeText").objectReferenceValue = timeTmp;
        so.FindProperty("enemiesText").objectReferenceValue = enemiesTmp;
        so.FindProperty("shardsText").objectReferenceValue = shardsTmp;
        so.FindProperty("rankText").objectReferenceValue = rankTmp;
        so.FindProperty("playAgainButton").objectReferenceValue = playAgainBtn;
        so.FindProperty("nextRealmButton").objectReferenceValue = nextRealmBtn;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = panelObj;
        Undo.RegisterCreatedObjectUndo(panelObj, "Create Victory Panel");
        Debug.Log("<color=green>[VAELSTRIDER]</color> Đã tạo thành công Giao Diện Chiến Thắng (VictoryPanel)!");
    }

    private static TextMeshProUGUI CreateStatRow(Transform parent, string label, string defaultValue, float posY, Color? valColor = null)
    {
        GameObject row = new GameObject("Row_" + label);
        row.transform.SetParent(parent, false);
        RectTransform rect = row.AddComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, posY);
        rect.sizeDelta = new Vector2(420, 26);

        // Label Left
        GameObject lblObj = new GameObject("Label");
        lblObj.transform.SetParent(row.transform, false);
        RectTransform lblRect = lblObj.AddComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0, 0.5f);
        lblRect.anchorMax = new Vector2(0, 0.5f);
        lblRect.pivot = new Vector2(0, 0.5f);
        lblRect.anchoredPosition = Vector2.zero;
        lblRect.sizeDelta = new Vector2(200, 26);
        var lblTmp = lblObj.AddComponent<TextMeshProUGUI>();
        lblTmp.text = label;
        lblTmp.fontSize = 16;
        lblTmp.color = new Color(0.65f, 0.65f, 0.75f);
        lblTmp.alignment = TextAlignmentOptions.Left;

        // Value Right
        GameObject valObj = new GameObject("Value");
        valObj.transform.SetParent(row.transform, false);
        RectTransform valRect = valObj.AddComponent<RectTransform>();
        valRect.anchorMin = new Vector2(1, 0.5f);
        valRect.anchorMax = new Vector2(1, 0.5f);
        valRect.pivot = new Vector2(1, 0.5f);
        valRect.anchoredPosition = Vector2.zero;
        valRect.sizeDelta = new Vector2(200, 26);
        var valTmp = valObj.AddComponent<TextMeshProUGUI>();
        valTmp.text = defaultValue;
        valTmp.fontSize = 18;
        valTmp.fontStyle = FontStyles.Bold;
        valTmp.color = valColor.HasValue ? valColor.Value : Color.white;
        valTmp.alignment = TextAlignmentOptions.Right;

        return valTmp;
    }

    private static Button CreateButton(Transform parent, string name, string text, Vector2 pos, Color bgColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(200, 50);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }
}
