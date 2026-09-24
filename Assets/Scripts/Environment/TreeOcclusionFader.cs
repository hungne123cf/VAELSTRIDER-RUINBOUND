using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TreeOcclusionFader:
/// - Khi Player dung sau cay: chi co LA CAY (canopy) mo ao (alpha ~20%),
///   con THAN CAY van giu nguyen 100% do dac.
/// - Su dung Shader Custom/TreeCanopyFade va MaterialPropertyBlock.
/// - KHONG can thiep hoac ghi de bat ky thong so Collider nao cua ban.
/// - TUYET DOI KHONG ap dung cho: Goc cay (stump), Tang da (rock), Bui cay (bush), Nen (floor).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class TreeOcclusionFader : MonoBehaviour
{
    [Header("Occlusion Canopy Settings")]
    [Tooltip("Ty le UV chieu cao phan than cay giu nguyen dac (0.0 = sat chan, 1.0 = dinh cay)")]
    [Range(0.1f, 0.6f)]
    [SerializeField] private float trunkCutoffUV = 0.35f;

    [Tooltip("Do min cua vung giao thoa giua than cay va tan la")]
    [Range(0.01f, 0.15f)]
    [SerializeField] private float blendSoftness = 0.05f;

    [Tooltip("Do trong suot cua LA CAY khi Player dung sau (0 = vo hinh, 1 = ro net)")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float canopyAlpha = 0.20f;

    [Tooltip("Toc do mo/hien cua tan la")]
    [SerializeField] private float fadeSpeed = 5.5f;

    [Tooltip("Do rong bao phu them 2 ben cua tan la")]
    [SerializeField] private float lateralPadding = 0.35f;

    private SpriteRenderer sr;
    private MaterialPropertyBlock mpb;
    private float currentFadeBlend = 0.0f;
    public bool isPlayerBehind = false;

    private static Transform cachedPlayer;
    private static Material sharedFadeMat;
    private float checkTimer = 0f;

    private static readonly int PropTrunkCutoff = Shader.PropertyToID("_TrunkCutoffUV");
    private static readonly int PropBlendSoftness = Shader.PropertyToID("_BlendSoftness");
    private static readonly int PropCanopyAlpha = Shader.PropertyToID("_CanopyAlpha");
    private static readonly int PropFadeBlend = Shader.PropertyToID("_FadeBlend");

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        string n = gameObject.name.ToLower();
        string spName = (sr != null && sr.sprite != null) ? sr.sprite.name.ToLower() : "";

        // BLACKLIST: Stump, Rock, Bush, Floor, Ground, Wall, Shrine, Badge
        bool isBlacklisted = n.Contains("stump") || n.Contains("goc_cay") ||
                             n.Contains("rock") || n.Contains("bush") ||
                             n.Contains("floor") || n.Contains("ground") ||
                             n.Contains("wall") || n.Contains("shrine") ||
                             n.Contains("pillar") || n.Contains("torch") ||
                             n.Contains("open") || n.Contains("badge") ||
                             spName.Contains("stump") || spName.Contains("rock") ||
                             spName.Contains("bush") || spName.Contains("floor");

        if (isBlacklisted)
        {
            Destroy(this);
            return;
        }

        // Tu dong tinh trunkCutoffUV dua theo loai cay
        AutoConfigureTreeCutoff(n, spName);

        // Dam bao su dung Material TreeCanopyFade
        EnsureFadeMaterial();

        mpb = new MaterialPropertyBlock();
        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(PropTrunkCutoff, trunkCutoffUV);
        mpb.SetFloat(PropBlendSoftness, blendSoftness);
        mpb.SetFloat(PropCanopyAlpha, canopyAlpha);
        mpb.SetFloat(PropFadeBlend, 0.0f);
        sr.SetPropertyBlock(mpb);
    }

    private void AutoConfigureTreeCutoff(string goName, string spriteName)
    {
        if (goName.Contains("pine") || spriteName.Contains("pine"))
        {
            trunkCutoffUV = 0.28f;
        }
        else if (goName.Contains("willow") || spriteName.Contains("willow"))
        {
            trunkCutoffUV = 0.36f;
        }
        else if (goName.Contains("giant") || goName.Contains("obj_07") || spriteName.Contains("obj_07"))
        {
            trunkCutoffUV = 0.38f;
        }
        else
        {
            trunkCutoffUV = 0.34f;
        }
    }

    private void EnsureFadeMaterial()
    {
        if (sr == null) return;

        if (sr.sharedMaterial == null || sr.sharedMaterial.shader.name != "Custom/TreeCanopyFade")
        {
            if (sharedFadeMat == null)
            {
                Shader shader = Shader.Find("Custom/TreeCanopyFade");
                if (shader != null)
                {
                    sharedFadeMat = new Material(shader) { name = "M_TreeCanopyFade_Runtime" };
                }
            }

            if (sharedFadeMat != null)
            {
                sr.sharedMaterial = sharedFadeMat;
            }
        }
    }

    void Start()
    {
        EnsurePlayerRef();
    }

    private void EnsurePlayerRef()
    {
        if (cachedPlayer == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) cachedPlayer = p.transform;
            else
            {
                var pc = Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) cachedPlayer = pc.transform;
            }
        }
    }

    void Update()
    {
        if (sr == null) return;

        checkTimer += Time.deltaTime;
        if (checkTimer >= 0.03f)
        {
            checkTimer = 0f;
            CheckPlayerOcclusion();
        }

        float targetBlend = isPlayerBehind ? 1.0f : 0.0f;
        if (!Mathf.Approximately(currentFadeBlend, targetBlend))
        {
            currentFadeBlend = Mathf.MoveTowards(currentFadeBlend, targetBlend, Time.deltaTime * fadeSpeed);
            if (mpb == null) mpb = new MaterialPropertyBlock();
            sr.GetPropertyBlock(mpb);
            mpb.SetFloat(PropTrunkCutoff, trunkCutoffUV);
            mpb.SetFloat(PropBlendSoftness, blendSoftness);
            mpb.SetFloat(PropCanopyAlpha, canopyAlpha);
            mpb.SetFloat(PropFadeBlend, currentFadeBlend);
            sr.SetPropertyBlock(mpb);
        }
    }

    private void CheckPlayerOcclusion()
    {
        if (cachedPlayer == null)
        {
            EnsurePlayerRef();
            if (cachedPlayer == null) { isPlayerBehind = false; return; }
        }

        if (sr == null || sr.sprite == null) { isPlayerBehind = false; return; }

        Bounds b = sr.bounds;
        Vector3 playerPos = cachedPlayer.position;

        float leftX = b.min.x - lateralPadding;
        float rightX = b.max.x + lateralPadding;

        float trunkHeight = b.size.y * trunkCutoffUV;
        float canopyBottomY = b.min.y + trunkHeight * 0.75f;
        float canopyTopY = b.max.y + 0.35f;

        bool xInRange = playerPos.x >= leftX && playerPos.x <= rightX;
        bool yInRange = playerPos.y >= canopyBottomY && playerPos.y <= canopyTopY;

        isPlayerBehind = xInRange && yInRange;
    }

    public void OnDrawGizmosSelected()
    {
        if (sr != null)
        {
            Bounds b = sr.bounds;
            float trunkHeight = b.size.y * trunkCutoffUV;
            float canopyBottomY = b.min.y + trunkHeight * 0.75f;
            float canopyTopY = b.max.y + 0.35f;

            Gizmos.color = isPlayerBehind ? Color.green : new Color(0.2f, 0.8f, 1f, 0.8f);
            float centerX = (b.min.x + b.max.x) * 0.5f;
            float centerY = (canopyBottomY + canopyTopY) * 0.5f;
            Vector3 center = new Vector3(centerX, centerY, 0f);
            Vector3 size = new Vector3(b.size.x + lateralPadding * 2f, canopyTopY - canopyBottomY, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}

/// <summary>
/// Tu dong quet va gan TreeOcclusionFader cho toan bo cay tan la cao trong Scene.
/// </summary>
public static class TreeFaderSceneAutoSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        ScanAndAttachTreeFaders();
        SceneManager.sceneLoaded += (scene, mode) => ScanAndAttachTreeFaders();
    }

    public static void ScanAndAttachTreeFaders()
    {
        var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int added = 0;
        int removed = 0;

        foreach (var r in allRenderers)
        {
            if (r == null || r.gameObject == null) continue;

            string goName = r.gameObject.name.ToLower();
            string spriteName = (r.sprite != null) ? r.sprite.name.ToLower() : "";

            bool isBlacklisted = goName.Contains("stump") || goName.Contains("goc_cay") ||
                                 goName.Contains("rock") || goName.Contains("bush") ||
                                 goName.Contains("floor") || goName.Contains("ground") ||
                                 goName.Contains("wall") || goName.Contains("arena") ||
                                 goName.Contains("shrine") || goName.Contains("pillar") ||
                                 goName.Contains("torch") || goName.Contains("light") ||
                                 goName.Contains("open") || goName.Contains("badge") ||
                                 goName.Contains("deco_") ||
                                 spriteName.Contains("stump") || spriteName.Contains("rock") ||
                                 spriteName.Contains("bush") || spriteName.Contains("floor") ||
                                 spriteName.Contains("fm_floor") || spriteName.Contains("fm_rock") ||
                                 spriteName.Contains("fm_bush");

            if (isBlacklisted)
            {
                var fader = r.GetComponent<TreeOcclusionFader>();
                if (fader != null)
                {
                    Object.Destroy(fader);
                    removed++;
                }
                continue;
            }

            bool isTree = goName.Contains("tree") || goName.Contains("willow") || goName.Contains("pine") ||
                          goName.Contains("teal") || goName.Contains("autumn") ||
                          spriteName.Contains("fm_tree_willow") || spriteName.Contains("fm_tree_pine") ||
                          spriteName.Contains("fm_tree_teal") || spriteName.Contains("fm_tree_red") ||
                          spriteName.Contains("fm_tree_autumn") || spriteName.Contains("fm_tree_ancient") ||
                          spriteName.Contains("tree") || spriteName.Contains("willow") ||
                          spriteName.Contains("pine") || spriteName.Contains("obj_07") ||
                          spriteName.Contains("obj_08") || spriteName.Contains("obj_10");

            if (isTree || r.GetComponent<TreeWindSway>() != null)
            {
                if (r.GetComponent<TreeOcclusionFader>() == null)
                {
                    r.gameObject.AddComponent<TreeOcclusionFader>();
                    added++;
                }
            }
        }
    }
}