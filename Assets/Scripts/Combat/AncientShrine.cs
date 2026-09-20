using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AncientShrine : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactRadius = 3.5f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0, 2.5f, 0);

    private Transform playerTransform;
    private GameObject worldPromptObj;
    private GameObject openBadgeObj;
    private RectTransform openBadgeRt;
    private Outline openBadgeOutline;
    private bool isPlayerInRange = false;
    private bool isMenuOpen = false;

    // Mobile interact button reference
    private static GameObject mobileInteractBtn;

    void Awake()
    {
        mobileInteractBtn = null;
    }

    void OnDestroy()
    {
        mobileInteractBtn = null;
    }

    void Start()
    {
        FindPlayer();
        EnsurePillarVisuals();
        CreateOpenBadge();
        CreateWorldPrompt();
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // Tinh khoang cach toi tam cua tru da (tru da cao khoang 2.5m nen tam o y + 0.8m)
        Vector2 shrineCenter = (Vector2)transform.position + new Vector2(0f, 0.8f);
        float dist = Vector2.Distance(shrineCenter, playerTransform.position);
        float effectiveRadius = Mathf.Max(interactRadius, 4.2f);
        bool inRange = dist <= effectiveRadius;
        isPlayerInRange = inRange;

        // Kiem tra truc tiep tu ShrineUpgradeUI xem Menu co dang mo hay khong
        bool menuOpen = ShrineUpgradeUI.Instance != null && ShrineUpgradeUI.Instance.IsOpen();
        bool shouldShow = inRange && !menuOpen;

        // Luon cap nhat trang thai hien thi cua Open Badge va World Prompt moi khung hinh
        if (openBadgeObj != null && openBadgeObj.activeSelf != shouldShow)
        {
            openBadgeObj.SetActive(shouldShow);
        }
        if (worldPromptObj != null && worldPromptObj.activeSelf != shouldShow)
        {
            worldPromptObj.SetActive(shouldShow);
        }
        UpdateMobileInteractButton(shouldShow);

        // Hieu ung bay bong, nhap nhay vien sang cho badge "OPEN"
        if (openBadgeObj != null && openBadgeObj.activeSelf && openBadgeRt != null)
        {
            float bobbing = Mathf.Sin(Time.time * 3.2f) * 0.12f;
            openBadgeObj.transform.localPosition = new Vector3(0f, promptOffset.y + bobbing, 0f);

            float targetScale = 0.015f;
            openBadgeRt.localScale = Vector3.Lerp(openBadgeRt.localScale, Vector3.one * targetScale, Time.deltaTime * 10f);

            if (openBadgeOutline != null)
            {
                float alpha = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
                Color baseCol = new Color(0.35f, 0.95f, 1f, 1f);
                openBadgeOutline.effectColor = new Color(baseCol.r, baseCol.g, baseCol.b, Mathf.Lerp(0.5f, 1f, alpha));
            }
        }

        // Phim [E] de tuong tac tren PC
        if (inRange && Input.GetKeyDown(KeyCode.E))
        {
            ToggleShrineMenu();
        }
    }

    private void FindPlayer()
    {
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;
    }

    /// <summary>
    /// Tao huy hieu nút bấm "OPEN" cuc ky de bam tren man hinh Mobile & chuot PC
    /// </summary>
    private void CreateOpenBadge()
    {
        if (openBadgeObj != null) return;

        openBadgeObj = new GameObject("Shrine_OpenBadge");
        openBadgeObj.transform.SetParent(transform, false);
        openBadgeObj.transform.localPosition = promptOffset;

        // World space Canvas
        Canvas badgeCanvas = openBadgeObj.AddComponent<Canvas>();
        badgeCanvas.renderMode = RenderMode.WorldSpace;
        badgeCanvas.sortingLayerName = "UI";
        badgeCanvas.worldCamera = Camera.main;
        badgeCanvas.sortingOrder = 500;

        var cs = openBadgeObj.AddComponent<CanvasScaler>();
        cs.dynamicPixelsPerUnit = 100;
        openBadgeObj.AddComponent<GraphicRaycaster>();

        openBadgeRt = openBadgeObj.GetComponent<RectTransform>();
        openBadgeRt.sizeDelta = new Vector2(160f, 60f);
        openBadgeRt.localScale = Vector3.one * 0.012f;

        // Background
        Image bgImg = openBadgeObj.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.12f, 0.20f, 0.94f);

        openBadgeOutline = openBadgeObj.AddComponent<Outline>();
        openBadgeOutline.effectColor = new Color(0.35f, 0.95f, 1f, 0.95f);
        openBadgeOutline.effectDistance = new Vector2(3f, -3f);

        // Text "OPEN"
        GameObject txtObj = new GameObject("Text_Open");
        txtObj.transform.SetParent(openBadgeObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "OPEN";
        tmp.fontSize = 32;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.92f, 0.35f); // Rich radiant gold
        tmp.raycastTarget = false;

        // UI Button component
        Button btn = openBadgeObj.AddComponent<Button>();
        btn.onClick.AddListener(OnBadgeClicked);

        // BoxCollider2D de bat ca su kien Touch / Click truc tiep trong the gioi 2D
        BoxCollider2D col = openBadgeObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(160f, 60f);
        var clicker = openBadgeObj.AddComponent<ShrineBadgeClicker>();
        clicker.shrine = this;

        openBadgeObj.SetActive(false);
    }

    private void CreateWorldPrompt()
    {
        worldPromptObj = new GameObject("ShrinePrompt");
        worldPromptObj.transform.SetParent(transform);
        worldPromptObj.transform.localPosition = promptOffset + new Vector3(0f, 0.7f, 0f);

        var textMesh = worldPromptObj.AddComponent<TextMeshPro>();
        textMesh.text = "NANG CAP CO DAI";
        textMesh.fontSize = 3.6f;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = new Color(0.35f, 0.95f, 1f, 1f);
        textMesh.sortingLayerID = SortingLayer.NameToID("UI");
        textMesh.sortingOrder = 100;

        worldPromptObj.SetActive(false);
    }

    public void OnBadgeClicked()
    {
        ToggleShrineMenu();
    }

    public void ToggleShrineMenu()
    {
        var ui = ShrineUpgradeUI.EnsureUIExists();
        if (ui == null) return;

        if (ui.IsOpen())
        {
            ui.Close();
        }
        else
        {
            ui.Open();
        }

        bool menuOpen = ui.IsOpen();
        bool shouldShow = isPlayerInRange && !menuOpen;
        if (openBadgeObj != null) openBadgeObj.SetActive(shouldShow);
        if (worldPromptObj != null) worldPromptObj.SetActive(shouldShow);
        UpdateMobileInteractButton(shouldShow);
    }

    public void OnMobileInteractClicked()
    {
        if (isPlayerInRange)
        {
            ToggleShrineMenu();
        }
    }

    private void UpdateMobileInteractButton(bool show)
    {
        EnsureMobileInteractButton();
        if (mobileInteractBtn != null)
        {
            mobileInteractBtn.SetActive(show);
        }
    }

    public static GameObject EnsureMobileInteractButton()
    {
        if (mobileInteractBtn != null) return mobileInteractBtn;

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("Btn_Interact");
        if (existing != null)
        {
            mobileInteractBtn = existing.gameObject;
            return mobileInteractBtn;
        }

        mobileInteractBtn = new GameObject("Btn_Interact", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        mobileInteractBtn.layer = 5;
        mobileInteractBtn.transform.SetParent(canvas.transform, false);

        RectTransform rt = mobileInteractBtn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-470, 180);
        rt.sizeDelta = new Vector2(90, 90);

        Image bg = mobileInteractBtn.GetComponent<Image>();
        bg.color = new Color(0.12f, 0.28f, 0.35f, 0.92f);

        GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        txtObj.layer = 5;
        txtObj.transform.SetParent(mobileInteractBtn.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "NANG\nCAP";
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.3f, 0.95f, 1f, 1f);
        tmp.raycastTarget = false;

        Button btn = mobileInteractBtn.GetComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() =>
        {
            var shrine = FindAnyObjectByType<AncientShrine>();
            if (shrine != null) shrine.OnMobileInteractClicked();
        });

        mobileInteractBtn.SetActive(false);
        return mobileInteractBtn;
    }

    public void EnsurePillarVisuals()
    {
        Sprite baseSprite = LoadPillarSprite();
        Sprite glowSprite = LoadGlowSprite();

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (sr.sprite == null && baseSprite != null) sr.sprite = baseSprite;
        sr.sortingLayerName = "Entities";
        sr.sortingOrder = 3;

        // Dam bao co GlowingRunePillar voi khe nut phat sang va am thanh khong gian
        GlowingRunePillar.Create(gameObject, glowSprite, new Color(0.45f, 0.95f, 1f, 1f));

        // Chan de va cham vat ly
        var baseCol = GetComponent<BoxCollider2D>();
        if (baseCol == null) baseCol = gameObject.AddComponent<BoxCollider2D>();
        baseCol.size = new Vector2(0.9f, 0.9f);
        baseCol.offset = new Vector2(0f, -0.6f);

        // Vung tuong tac trigger
        var trigCol = GetComponent<CircleCollider2D>();
        if (trigCol == null) trigCol = gameObject.AddComponent<CircleCollider2D>();
        trigCol.isTrigger = true;
        trigCol.radius = 2.8f;
    }

    public static AncientShrine SpawnShrine(Vector3 worldPos)
    {
        var existing = FindAnyObjectByType<AncientShrine>();
        if (existing != null)
        {
            existing.transform.position = worldPos;
            existing.EnsurePillarVisuals();
            return existing;
        }

        GameObject shrineObj = new GameObject("AncientShrine_Pillar");
        shrineObj.transform.position = worldPos;
        shrineObj.transform.localScale = new Vector3(1.6f, 1.6f, 1f);

        var shrine = shrineObj.AddComponent<AncientShrine>();
        shrine.EnsurePillarVisuals();

        Debug.Log("<color=cyan>[ANCIENT SHRINE]</color> Kien tao Tru Da Phat Sang (Be Da Nang Cap) tai: " + worldPos);
        return shrine;
    }

    public static Sprite LoadPillarSprite()
    {
        Sprite s = null;
#if UNITY_EDITOR
        s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_52_14x38.png");
#endif
        if (s == null) s = Resources.Load<Sprite>("Props/obj_52_14x38");
        return s;
    }

    public static Sprite LoadGlowSprite()
    {
        Sprite s = null;
#if UNITY_EDITOR
        s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Map/Valley Ruin Asset Pack V0.3/Objects/Extracted/obj_52_crack_glow.png");
#endif
        if (s == null) s = Resources.Load<Sprite>("Props/obj_52_crack_glow");
        return s;
    }
}

public class ShrineBadgeClicker : MonoBehaviour
{
    public AncientShrine shrine;

    void OnMouseDown()
    {
        if (shrine != null)
        {
            shrine.OnBadgeClicked();
        }
    }
}
