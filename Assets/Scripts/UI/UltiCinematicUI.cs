using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI Điện Ảnh Cho Tuyệt Kỹ Chiêu Cuối (Phím F).
/// Hiển thị 2 vệt đen Letterbox, Banner Cắt Cảnh Ánh Mắt Anime Rực Lửa, và Câu Thoại Ngầu Lòi.
/// </summary>
public class UltiCinematicUI : MonoBehaviour
{
    public static UltiCinematicUI Instance { get; private set; }

    private CanvasGroup canvasGroup;
    private GameObject letterboxTop;
    private GameObject letterboxBottom;
    private GameObject bannerRoot;
    private Image eyeCutInImage;
    private TextMeshProUGUI speakerTMP;
    private TextMeshProUGUI quoteTMP;

    private static Sprite vaelEyesSprite;
    private static Sprite maidEyesSprite;

    private readonly string[] vaelQuotesVN = new string[]
    {
        "Bão Lực Tàn Tích... Biến mất vào hư không!",
        "Lưỡi kiếm này... sẽ định đoạt số phận của các ngươi!",
        "Sức mạnh cổ xưa thức tỉnh... Không ai có thể trốn thoát!",
        "Trước ngọn lửa tàn tích, mọi bóng tối đều phải tiêu tan!",
        "Ngươi đã bước vào tử địa của ta!"
    };

    private readonly string[] vaelQuotesEN = new string[]
    {
        "Ruin Tempest... Disappear into the void!",
        "This blade... shall seal your fate!",
        "Ancient wrath awakens... None shall escape!",
        "Before the ruin flame, all shadows perish!",
        "You have entered my domain of death!"
    };

    private int quoteIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadAssets();
        BuildUI();
    }

    private void LoadAssets()
    {
#if UNITY_EDITOR
        if (vaelEyesSprite == null)
        {
            vaelEyesSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/VFX/anime_eyes_vaelstrider.png");
        }
        if (maidEyesSprite == null)
        {
            maidEyesSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/VFX/anime_eyes_maid.png");
        }
#endif
    }

    private void BuildUI()
    {
        RectTransform rootRT = GetComponent<RectTransform>();
        if (rootRT == null) rootRT = gameObject.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.sizeDelta = Vector2.zero;
        rootRT.anchoredPosition = Vector2.zero;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // 1. Top Letterbox Bar
        letterboxTop = new GameObject("Letterbox_Top", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        letterboxTop.transform.SetParent(transform, false);
        RectTransform topRT = letterboxTop.GetComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0f, 1f);
        topRT.anchorMax = new Vector2(1f, 1f);
        topRT.pivot = new Vector2(0.5f, 1f);
        topRT.sizeDelta = new Vector2(0f, 130f);
        topRT.anchoredPosition = Vector2.zero;
        Image topImg = letterboxTop.GetComponent<Image>();
        topImg.color = new Color(0.01f, 0.01f, 0.02f, 0.97f);
        topImg.raycastTarget = false;

        // Viền sáng cạnh dưới Top Bar
        GameObject topEdge = new GameObject("TopEdgeGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topEdge.transform.SetParent(letterboxTop.transform, false);
        RectTransform topEdgeRT = topEdge.GetComponent<RectTransform>();
        topEdgeRT.anchorMin = new Vector2(0f, 0f);
        topEdgeRT.anchorMax = new Vector2(1f, 0f);
        topEdgeRT.pivot = new Vector2(0.5f, 0f);
        topEdgeRT.sizeDelta = new Vector2(0f, 3f);
        topEdgeRT.anchoredPosition = Vector2.zero;
        Image topEdgeImg = topEdge.GetComponent<Image>();
        topEdgeImg.color = new Color(1f, 0.4f, 0.1f, 0.85f);
        topEdgeImg.raycastTarget = false;

        // 2. Bottom Letterbox Bar
        letterboxBottom = new GameObject("Letterbox_Bottom", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        letterboxBottom.transform.SetParent(transform, false);
        RectTransform botRT = letterboxBottom.GetComponent<RectTransform>();
        botRT.anchorMin = new Vector2(0f, 0f);
        botRT.anchorMax = new Vector2(1f, 0f);
        botRT.pivot = new Vector2(0.5f, 0f);
        botRT.sizeDelta = new Vector2(0f, 130f);
        botRT.anchoredPosition = Vector2.zero;
        Image botImg = letterboxBottom.GetComponent<Image>();
        botImg.color = new Color(0.01f, 0.01f, 0.02f, 0.97f);
        botImg.raycastTarget = false;

        // Viền sáng cạnh trên Bottom Bar
        GameObject botEdge = new GameObject("BotEdgeGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        botEdge.transform.SetParent(letterboxBottom.transform, false);
        RectTransform botEdgeRT = botEdge.GetComponent<RectTransform>();
        botEdgeRT.anchorMin = new Vector2(0f, 1f);
        botEdgeRT.anchorMax = new Vector2(1f, 1f);
        botEdgeRT.pivot = new Vector2(0.5f, 1f);
        botEdgeRT.sizeDelta = new Vector2(0f, 3f);
        botEdgeRT.anchoredPosition = Vector2.zero;
        Image botEdgeImg = botEdge.GetComponent<Image>();
        botEdgeImg.color = new Color(1f, 0.4f, 0.1f, 0.85f);
        botEdgeImg.raycastTarget = false;

        // 3. Banner Root: Đặt chính diện phía dưới màn hình
        bannerRoot = new GameObject("Banner_Root", typeof(RectTransform));
        bannerRoot.transform.SetParent(transform, false);
        RectTransform bannerRT = bannerRoot.GetComponent<RectTransform>();
        bannerRT.anchorMin = new Vector2(0.5f, 0.25f);
        bannerRT.anchorMax = new Vector2(0.5f, 0.25f);
        bannerRT.pivot = new Vector2(0.5f, 0.5f);
        bannerRT.sizeDelta = new Vector2(980f, 210f);
        bannerRT.anchoredPosition = Vector2.zero;

        // 3.1 Dải Ánh Mắt Anime Rực Lửa (Eye Cut-In Strip)
        GameObject eyeStripObj = new GameObject("EyeCutInStrip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        eyeStripObj.transform.SetParent(bannerRoot.transform, false);
        RectTransform eyeRT = eyeStripObj.GetComponent<RectTransform>();
        eyeRT.anchorMin = new Vector2(0.5f, 1f);
        eyeRT.anchorMax = new Vector2(0.5f, 1f);
        eyeRT.pivot = new Vector2(0.5f, 1f);
        eyeRT.sizeDelta = new Vector2(850f, 110f);
        eyeRT.anchoredPosition = new Vector2(0f, 0f);
        eyeCutInImage = eyeStripObj.GetComponent<Image>();
        eyeCutInImage.sprite = vaelEyesSprite;
        eyeCutInImage.color = Color.white;
        eyeCutInImage.raycastTarget = false;

        // 3.2 Khung Hộp Thoại (Quote Box)
        GameObject quoteBox = new GameObject("QuoteBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        quoteBox.transform.SetParent(bannerRoot.transform, false);
        RectTransform qbRT = quoteBox.GetComponent<RectTransform>();
        qbRT.anchorMin = new Vector2(0.5f, 0f);
        qbRT.anchorMax = new Vector2(0.5f, 0f);
        qbRT.pivot = new Vector2(0.5f, 0f);
        qbRT.sizeDelta = new Vector2(940f, 95f);
        qbRT.anchoredPosition = new Vector2(0f, 0f);
        Image qbImg = quoteBox.GetComponent<Image>();
        qbImg.color = new Color(0.05f, 0.04f, 0.10f, 0.95f);
        qbImg.raycastTarget = false;

        Outline boxOutline = quoteBox.GetComponent<Outline>();
        boxOutline.effectColor = new Color(1f, 0.75f, 0.2f, 0.85f);
        boxOutline.effectDistance = new Vector2(2.5f, -2.5f);

        // Load Font SDF
        TMP_FontAsset sdfFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
        if (sdfFont == null)
        {
            sdfFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }
#endif
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 3.3 Tên Nhân Vật (Speaker Text)
        GameObject speakerObj = new GameObject("SpeakerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        speakerObj.transform.SetParent(quoteBox.transform, false);
        RectTransform spkRT = speakerObj.GetComponent<RectTransform>();
        spkRT.anchorMin = new Vector2(0f, 1f);
        spkRT.anchorMax = new Vector2(1f, 1f);
        spkRT.pivot = new Vector2(0.5f, 1f);
        spkRT.sizeDelta = new Vector2(-40f, 32f);
        spkRT.anchoredPosition = new Vector2(0f, -8f);

        speakerTMP = speakerObj.GetComponent<TextMeshProUGUI>();
        if (sdfFont != null) speakerTMP.font = sdfFont;
        speakerTMP.text = "✦ VAELSTRIDER ✦";
        speakerTMP.fontSize = 20;
        speakerTMP.fontStyle = FontStyles.Bold;
        speakerTMP.alignment = TextAlignmentOptions.Center;
        speakerTMP.color = new Color(1f, 0.85f, 0.25f, 1f);
        speakerTMP.raycastTarget = false;

        // 3.4 Câu Thoại Ngầu Lòi (Quote Text)
        GameObject quoteObj = new GameObject("QuoteText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        quoteObj.transform.SetParent(quoteBox.transform, false);
        RectTransform qRT = quoteObj.GetComponent<RectTransform>();
        qRT.anchorMin = new Vector2(0f, 0f);
        qRT.anchorMax = new Vector2(1f, 1f);
        qRT.pivot = new Vector2(0.5f, 0.5f);
        qRT.sizeDelta = new Vector2(-40f, -38f);
        qRT.anchoredPosition = new Vector2(0f, -12f);

        quoteTMP = quoteObj.GetComponent<TextMeshProUGUI>();
        if (sdfFont != null) quoteTMP.font = sdfFont;
        quoteTMP.text = "“Bão Lực Tàn Tích... Biến mất vào hư không!”";
        quoteTMP.fontSize = 25;
        quoteTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;
        quoteTMP.alignment = TextAlignmentOptions.Center;
        quoteTMP.color = new Color(1f, 0.98f, 0.9f, 1f);
        quoteTMP.raycastTarget = false;
    }

    public void ShowCutscene(float durationRealtime, bool isMaid = false)
    {
        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;
        string speaker = isMaid 
            ? (isVi ? "MAID CHIẾN BINH" : "BATTLE MAID")
            : (isVi ? "CHIẾN BINH TÀN TÍCH" : "RUIN STRIDER");

        string[] quotes = isVi ? vaelQuotesVN : vaelQuotesEN;
        string quote = quotes[quoteIndex % quotes.Length];
        quoteIndex++;

        ShowCutscene(durationRealtime, speaker, quote, isMaid);
    }

    public void ShowCutscene(float durationRealtime, string speaker, string quote, bool isMaid = false)
    {
        LoadAssets();
        StopAllCoroutines();
        StartCoroutine(CutsceneRoutine(durationRealtime, speaker, quote, isMaid));
    }

    private IEnumerator CutsceneRoutine(float durationRealtime, string speaker, string quote, bool isMaid)
    {
        string speakerFormatted = "✦ " + (string.IsNullOrEmpty(speaker) ? "VAELSTRIDER" : speaker.ToUpper()) + " ✦";
        string quoteFormatted = "“" + quote + "”";

        if (speakerTMP != null) speakerTMP.text = speakerFormatted;

        if (quoteTMP != null) quoteTMP.text = quoteFormatted;

        // Gán hình ảnh ánh mắt tương ứng
        if (eyeCutInImage != null)
        {
            eyeCutInImage.sprite = isMaid ? maidEyesSprite : vaelEyesSprite;
            eyeCutInImage.gameObject.SetActive(eyeCutInImage.sprite != null);
        }

        // Phát giọng nói thoại ngầu
        SoundManager.EnsureInstance()?.PlayUltiVoice();

        // ==========================================
        // GIAI ĐOẠN 1: BẬT LÊN VỚI HOẠT ẢNH BẬT NẨY (Pop-in)
        // ==========================================
        float fadeInTime = 0.12f;
        float elapsed = 0f;

        while (elapsed < fadeInTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInTime);

            if (canvasGroup != null) canvasGroup.alpha = t;
            if (bannerRoot != null)
            {
                // Bật từ 1.25 phóng to thu nhỏ về 1.0 (anime impact punch)
                bannerRoot.transform.localScale = Vector3.Lerp(Vector3.one * 1.25f, Vector3.one, t);
            }

            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (bannerRoot != null) bannerRoot.transform.localScale = Vector3.one;

        // ==========================================
        // GIAI ĐOẠN 2: GIỮ NGUYÊN ĐỂ NGƯỜI CHƠI ĐỌC THOẠI & XEM ÁNH MẮT
        // ==========================================
        float holdTime = Mathf.Max(0.15f, durationRealtime - fadeInTime - 0.18f);
        elapsed = 0f;

        while (elapsed < holdTime)
        {
            elapsed += Time.unscaledDeltaTime;
            // Nhịp thở nhẹ của banner
            if (bannerRoot != null)
            {
                float pulse = 1f + 0.02f * Mathf.Sin(Time.unscaledTime * 12f);
                bannerRoot.transform.localScale = Vector3.one * pulse;
            }
            yield return null;
        }

        // ==========================================
        // GIAI ĐOẠN 3: FADE OUT ÊM ÁI
        // ==========================================
        float fadeOutTime = 0.18f;
        elapsed = 0f;

        while (elapsed < fadeOutTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutTime);

            if (canvasGroup != null) canvasGroup.alpha = 1f - t;
            if (bannerRoot != null)
            {
                bannerRoot.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.92f, t);
            }

            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Đảm bảo một Canvas Overlay chuyên dụng riêng biệt cho UltiCinematicUI để luôn luôn hiển thị trên cùng (SortingOrder = 99999).
    /// </summary>
    public static UltiCinematicUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        GameObject canvasObj = GameObject.Find("UltiCinematicCanvas");
        Canvas canvas = null;

        if (canvasObj == null)
        {
            canvasObj = new GameObject("UltiCinematicCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99999;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            // Object.DontDestroyOnLoad(canvasObj); -- removed to prevent persistent canvas blocking UI on reset
        }
        else
        {
            canvas = canvasObj.GetComponent<Canvas>();
        }

        Transform existing = canvasObj.transform.Find("UltiCinematicUI");
        if (existing != null)
        {
            Instance = existing.GetComponent<UltiCinematicUI>();
            if (Instance != null) return Instance;
        }

        GameObject go = new GameObject("UltiCinematicUI");
        go.transform.SetParent(canvasObj.transform, false);
        Instance = go.AddComponent<UltiCinematicUI>();
        return Instance;
    }
}
