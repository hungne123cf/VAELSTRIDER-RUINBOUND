using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AncientShrine : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactRadius = 2.6f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0, 2.2f, 0);

    private Transform playerTransform;
    private GameObject worldPromptObj;
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
        CreateWorldPrompt();
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactRadius;

        if (inRange != isPlayerInRange)
        {
            isPlayerInRange = inRange;
            if (worldPromptObj != null) worldPromptObj.SetActive(inRange && !isMenuOpen);
            UpdateMobileInteractButton(inRange && !isMenuOpen);
        }

        // PC Key [E] to interact
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            ToggleShrineMenu();
        }
    }

    private void FindPlayer()
    {
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;
    }

    private void CreateWorldPrompt()
    {
        worldPromptObj = new GameObject("ShrinePrompt");
        worldPromptObj.transform.SetParent(transform);
        worldPromptObj.transform.localPosition = promptOffset;

        var textMesh = worldPromptObj.AddComponent<TextMeshPro>();
        textMesh.text = "✦ [E] BÀN THỜ CỔ ĐẠI ✦";
        textMesh.fontSize = 4.2f;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = new Color(0.35f, 0.95f, 1f, 1f);
        textMesh.sortingLayerID = SortingLayer.NameToID("UI");
        textMesh.sortingOrder = 100;

        worldPromptObj.SetActive(false);
    }

    public void ToggleShrineMenu()
    {
        var ui = ShrineUpgradeUI.EnsureUIExists();
        if (ui == null) return;

        isMenuOpen = !ui.IsOpen();
        if (isMenuOpen)
        {
            ui.Open();
        }
        else
        {
            ui.Close();
        }

        if (worldPromptObj != null) worldPromptObj.SetActive(isPlayerInRange && !isMenuOpen);
        UpdateMobileInteractButton(isPlayerInRange && !isMenuOpen);
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

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find("Btn_Interact");
        if (existing != null)
        {
            mobileInteractBtn = existing.gameObject;
            return mobileInteractBtn;
        }

        mobileInteractBtn = new GameObject("Btn_Interact");
        mobileInteractBtn.transform.SetParent(canvas.transform, false);

        RectTransform rt = mobileInteractBtn.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-470, 180);
        rt.sizeDelta = new Vector2(90, 90);

        Image bg = mobileInteractBtn.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.28f, 0.35f, 0.92f);

        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(mobileInteractBtn.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "🏛️\nCẦU\nNGUYỆN";
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.3f, 0.95f, 1f, 1f);
        tmp.raycastTarget = false;

        Button btn = mobileInteractBtn.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            var shrine = FindAnyObjectByType<AncientShrine>();
            if (shrine != null) shrine.OnMobileInteractClicked();
        });

        mobileInteractBtn.SetActive(false);
        return mobileInteractBtn;
    }

    public static AncientShrine SpawnShrine(Vector3 worldPos)
    {
        var existing = FindAnyObjectByType<AncientShrine>();
        if (existing != null) return existing;

        GameObject shrineObj = new GameObject("AncientShrine");
        shrineObj.transform.position = worldPos;
        shrineObj.transform.localScale = new Vector3(1.75f, 1.75f, 1f); // Uy nghi, bề thế

        SpriteRenderer sr = shrineObj.AddComponent<SpriteRenderer>();
        Sprite altarSprite = null;
#if UNITY_EDITOR
        var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Map/Texture/TX Struct.png");
        foreach (var obj in sprites)
        {
            if (obj is Sprite s && s.name == "TX Struct_24") { altarSprite = s; break; }
        }
#endif
        if (altarSprite != null) sr.sprite = altarSprite;
        sr.sortingLayerName = "Entities";
        sr.sortingOrder = 0;

        // Auto add depth sort
        shrineObj.AddComponent<AutoDepthSort>();

        // Collider trigger
        CircleCollider2D col = shrineObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 2.2f;

        var shrine = shrineObj.AddComponent<AncientShrine>();

        // Attach mystic Light2D with wide beacon radius so it's always prominent
        var light = shrineObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = new Color(0.25f, 0.85f, 1f, 1f);
        light.intensity = 2.0f;
        light.pointLightInnerRadius = 0.8f;
        light.pointLightOuterRadius = 5.2f;

        // Add gentle pulse script
        shrineObj.AddComponent<TorchFlicker>().SetMysticPulse();

        Debug.Log("<color=cyan>[ANCIENT SHRINE]</color> Đã tạo Bàn Thờ Cổ Đại tại: " + worldPos);
        return shrine;
    }
}
