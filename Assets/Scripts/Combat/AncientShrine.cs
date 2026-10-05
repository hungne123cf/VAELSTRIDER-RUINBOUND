using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AncientShrine : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactRadius = 3.5f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0, 2.2f, 0);

    private Transform playerTransform;
    private GameObject arrowIndicatorObj;
    private TextMeshPro arrowTextMesh;
    private Color arrowColor = new Color(0.35f, 0.95f, 1f, 1f);

    private bool isPlayerInRange = false;

    void Awake()
    {
    }

    void OnDestroy()
    {
    }

    void Start()
    {
        FindPlayer();
        EnsurePillarVisuals();
        CreateArrowIndicator();

        Canvas canvas = GameAutoBootstrap.GetMainGameplayCanvas();
        if (canvas != null)
        {
            Transform oldBtn = canvas.transform.Find("Btn_Interact");
            if (oldBtn != null) Destroy(oldBtn.gameObject);
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // Tinh khoang cach toi tam cua tru da (tru da cao khoang 2.5m nen tam o y + 0.8m)
        Vector2 shrineCenter = (Vector2)transform.position + new Vector2(0f, 0.4f);
        float dist = Vector2.Distance(shrineCenter, playerTransform.position);
        float effectiveRadius = Mathf.Max(interactRadius, 1.8f);
        bool inRange = dist <= effectiveRadius;
        isPlayerInRange = inRange;

        // Kiem tra truc tiep tu ShrineUpgradeUI va cac UI khac xem co dang mo hay khong
        bool menuOpen = ShrineUpgradeUI.Instance != null && ShrineUpgradeUI.Instance.IsOpen();
        bool otherUIOpen = IsAnyOtherUIOpen();
        bool shouldShow = inRange && !menuOpen && !otherUIOpen;

        // Cap nhat trang thai hien thi mui ten
        if (arrowIndicatorObj != null && arrowIndicatorObj.activeSelf != shouldShow)
        {
            arrowIndicatorObj.SetActive(shouldShow);
        }

        // Hieu ung mui ten bay bong, nhap nhay nhe nhang tren dau tru da
        if (arrowIndicatorObj != null && arrowIndicatorObj.activeSelf)
        {
            float bobbing = Mathf.Sin(Time.time * 4.5f) * 0.12f;
            arrowIndicatorObj.transform.localPosition = promptOffset + new Vector3(0f, bobbing, 0f);

            if (arrowTextMesh != null)
            {
                float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
                arrowTextMesh.color = Color.Lerp(new Color(0.3f, 0.9f, 1f, 0.85f), new Color(1f, 0.95f, 0.4f, 1f), pulse);
            }
        }

        // Phim [E] de tuong tac tren PC (chi khi khong co UI nao khac dang mo)
        if (inRange && !otherUIOpen && Input.GetKeyDown(KeyCode.E))
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
    /// Tao mui ten phat sang bay bong chi vao tru da, khong can chu OPEN / NANG CAP
    /// </summary>
    private void CreateArrowIndicator()
    {
        if (arrowIndicatorObj != null) return;

        arrowIndicatorObj = new GameObject("Shrine_ArrowIndicator");
        arrowIndicatorObj.transform.SetParent(transform, false);
        arrowIndicatorObj.transform.localPosition = promptOffset;

        arrowTextMesh = arrowIndicatorObj.AddComponent<TextMeshPro>();
        arrowTextMesh.text = "▼";
        arrowTextMesh.fontSize = 5.2f;
        arrowTextMesh.fontStyle = FontStyles.Bold;
        arrowTextMesh.alignment = TextAlignmentOptions.Center;
        arrowTextMesh.color = arrowColor;
        arrowTextMesh.sortingLayerID = SortingLayer.NameToID("UI");
        arrowTextMesh.sortingOrder = 500;

        arrowIndicatorObj.SetActive(false);
    }

    public void OnBadgeClicked()
    {
        if (playerTransform == null) FindPlayer();
        float dist = playerTransform != null ? Vector2.Distance((Vector2)transform.position + new Vector2(0f, 0.4f), playerTransform.position) : 99f;
        if (dist <= 3.2f)
        {
            ToggleShrineMenu();
        }
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
        if (arrowIndicatorObj != null) arrowIndicatorObj.SetActive(shouldShow);
    }

    public void OnMobileInteractClicked()
    {
        if (isPlayerInRange)
        {
            ToggleShrineMenu();
        }
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

        // Xoa cac CircleCollider2D cu khong mong muon
        var circleCols = GetComponents<CircleCollider2D>();
        foreach (var c in circleCols)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }

        // 1. Chan de va cham vat ly (Tru da cung ko the di qua - chi o phan de duoi)
        var boxCols = GetComponents<BoxCollider2D>();
        BoxCollider2D baseCol = null;
        BoxCollider2D trigCol = null;

        if (boxCols.Length > 0) baseCol = boxCols[0];
        if (boxCols.Length > 1) trigCol = boxCols[1];

        if (baseCol == null) baseCol = gameObject.AddComponent<BoxCollider2D>();
        baseCol.isTrigger = false;
        baseCol.size = new Vector2(0.85f, 0.7f);
        baseCol.offset = new Vector2(0f, -0.65f);

        // 2. Vung tuong tac trigger & ho tro click/touch vua khit 100% voi Sprite tru da
        if (trigCol == null) trigCol = gameObject.AddComponent<BoxCollider2D>();
        trigCol.isTrigger = true;
        trigCol.size = new Vector2(0.9f, 2.3f);
        trigCol.offset = new Vector2(0f, 0.2f);

        var clicker = GetComponent<ShrineBadgeClicker>();
        if (clicker == null) clicker = gameObject.AddComponent<ShrineBadgeClicker>();
        clicker.shrine = this;
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

    public static bool IsAnyOtherUIOpen()
    {
        if (SettingsUI.Instance != null && SettingsUI.Instance.IsOpen()) return true;
        if (InGameInventoryUI.Instance != null && InGameInventoryUI.Instance.IsOpen()) return true;
        if (CharacterStatsUI.Instance != null && CharacterStatsUI.Instance.IsOpen()) return true;
        if (ArmorShopUI.Instance != null && ArmorShopUI.Instance.IsOpen()) return true;
        if (GameOverUI.Instance != null && GameOverUI.Instance.IsShown()) return true;
        if (VictoryUI.Instance != null && VictoryUI.Instance.IsShown()) return true;
        return false;
    }

    public static bool IsPointerOverUI()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;

        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return true;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                    return true;
            }
        }

        return false;
    }
}

public class ShrineBadgeClicker : MonoBehaviour
{
    public AncientShrine shrine;

    void OnMouseDown()
    {
        // 1. TUYET DOI CHAN neu con tro / ngon tay dang bam vao bat ky UI nao (Settings, Inventory, Sliders, v.v.)
        if (AncientShrine.IsPointerOverUI())
        {
            return;
        }

        // 2. Chan neu co bat ky Menu / UI nao khac dang mo tren man hinh
        if (AncientShrine.IsAnyOtherUIOpen())
        {
            return;
        }

        if (shrine != null)
        {
            shrine.OnBadgeClicked();
        }
    }
}
