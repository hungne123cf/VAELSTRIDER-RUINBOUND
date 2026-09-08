using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance { get; private set; }

    [Header("Input")]
    [SerializeField] private TMP_InputField nameInput;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    [Header("Titles")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI playButtonText;
    [SerializeField] private TextMeshProUGUI quitButtonText;
    [SerializeField] private TextMeshProUGUI warriorNameLabel;

    public static void EnsureInstance()
    {
        if (Instance == null && Object.FindAnyObjectByType<LoginManager>() == null)
        {
            GameObject obj = new GameObject("LoginManager_Auto");
            obj.AddComponent<LoginManager>();
        }
    }

    void Awake()
    {
        Instance = this;
        GameAutoBootstrap.CleanUpGameplayUI();
        CleanObsoletePanels();
    }

    void Start()
    {
        GameAutoBootstrap.CleanUpGameplayUI();
        CleanObsoletePanels();

        // Neu chua co UI hoac nut play bi thieu, tao UI dong cuc dep
        if (playButton == null || nameInput == null)
        {
            BuildDynamicUI();
        }
        else
        {
            // Neu UI da co trong scene, dam bao sua triet de inputField de click va go ten 100%
            FixInputField(nameInput);
        }

        // 1. Load saved player name
        string savedName = PlayerPrefs.GetString("Player_Name", PlayerPrefs.GetString("Player_Username", "Vaelstrider"));
        if (string.IsNullOrWhiteSpace(savedName)) savedName = "Vaelstrider";

        if (nameInput != null)
        {
            nameInput.text = savedName;
            nameInput.onValueChanged.RemoveAllListeners();
            nameInput.onValueChanged.AddListener(OnNameChanged);
            nameInput.interactable = true;
            nameInput.ActivateInputField();
        }

        // 2. Setup button listeners
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        // 3. Localization setup
        LocalizationManager.OnLanguageChanged -= RefreshLocalization;
        LocalizationManager.OnLanguageChanged += RefreshLocalization;
        RefreshLocalization();

        // 4. Play ambient music
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySceneAmbience("LoginScene");
        }
    }

    void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= RefreshLocalization;
    }

    private void CleanObsoletePanels()
    {
        string[] obsolete = new string[] { "SettingsPanel", "GuidePanel", "Btn_SettingsGear", "Btn_Settings" };
        foreach (var name in obsolete)
        {
            var obj = GameObject.Find(name);
            if (obj != null) Destroy(obj);
        }
    }

    private void FixInputField(TMP_InputField input)
    {
        if (input == null) return;
        input.interactable = true;

        var inImg = input.GetComponent<Image>();
        if (inImg != null)
        {
            inImg.raycastTarget = true;
            input.targetGraphic = inImg;
        }

        if (input.textComponent != null)
        {
            input.textComponent.raycastTarget = false;
            if (input.textViewport == null)
            {
                input.textViewport = input.textComponent.rectTransform;
            }
        }

        // Dam bao EventSystem ton tai
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private void OnNameChanged(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) newName = "Vaelstrider";
        string trimmed = newName.Trim();
        PlayerPrefs.SetString("Player_Name", trimmed);
        PlayerPrefs.SetString("Player_Username", trimmed);
        PlayerPrefs.Save();
    }

    private void OnPlayClicked()
    {
        if (nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text))
        {
            string clean = nameInput.text.Trim();
            PlayerPrefs.SetString("Player_Name", clean);
            PlayerPrefs.SetString("Player_Username", clean);
            PlayerPrefs.Save();
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySwordSlash();

        Time.timeScale = 1f;
        Debug.Log("<color=green>[LOGIN]</color> Dang nhap thanh cong! Chuyen den Sanh Cho Chinh (MainMenuScene).");
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OnQuitClicked()
    {
        Debug.Log("<color=yellow>[GAME]</color> Thoat tro choi...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshLocalization()
    {
        bool isVi = LocalizationManager.CurrentLanguage == LocalizationManager.Language.Vietnamese;

        if (titleText != null) titleText.text = "VAELSTRIDER";
        if (subtitleText != null) subtitleText.text = isVi ? "- RUINBOUND: KE DU HANH CO TICH -" : "- RUINBOUND: WANDERER OF RUINS -";
        if (playButtonText != null) playButtonText.text = isVi ? "BAT DAU HANH TRINH" : "ENTER THE REALM";
        if (quitButtonText != null) quitButtonText.text = isVi ? "THOAT GAME" : "QUIT GAME";
        if (warriorNameLabel != null) warriorNameLabel.text = isVi ? "TEN CHIEN BINH:" : "WARRIOR NAME:";
    }

    private void BuildDynamicUI()
    {
        // 1. Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cObj = new GameObject("LoginCanvas");
            canvas = cObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = cObj.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight = 0.5f;
            cObj.AddComponent<GraphicRaycaster>();
        }

        // Clean any old duplicate LoginCard/Header
        Transform oldCard = canvas.transform.Find("Login_Card");
        if (oldCard != null) DestroyImmediate(oldCard.gameObject);
        Transform oldHeader = canvas.transform.Find("Login_Header");
        if (oldHeader != null) DestroyImmediate(oldHeader.gameObject);

        // EventSystem
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. Fullscreen dark backdrop
        Transform existingBg = canvas.transform.Find("Login_DarkBackdrop");
        if (existingBg == null)
        {
            GameObject bgObj = new GameObject("Login_DarkBackdrop");
            bgObj.transform.SetParent(canvas.transform, false);
            bgObj.transform.SetAsFirstSibling();
            var bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);
        }

        // 3. Header Title (Gon gang, cach tam hop ly)
        GameObject headerObj = new GameObject("Login_Header");
        headerObj.transform.SetParent(canvas.transform, false);
        var hRt = headerObj.AddComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0.5f, 0.5f);
        hRt.anchorMax = new Vector2(0.5f, 0.5f);
        hRt.pivot = new Vector2(0.5f, 0.5f);
        hRt.anchoredPosition = new Vector2(0f, 230f);
        hRt.sizeDelta = new Vector2(900f, 130f);

        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(headerObj.transform, false);
        var tRt = tObj.AddComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 25f);
        tRt.sizeDelta = new Vector2(850f, 80f);
        titleText = tObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "VAELSTRIDER";
        titleText.fontSize = 68;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.25f);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;

        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(headerObj.transform, false);
        var subRt = subObj.AddComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0f, -32f);
        subRt.sizeDelta = new Vector2(850f, 35f);
        subtitleText = subObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "- RUINBOUND: KE DU HANH CO TICH -";
        subtitleText.fontSize = 19;
        subtitleText.fontStyle = FontStyles.Bold;
        subtitleText.color = new Color(0.85f, 0.40f, 0.40f);
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.raycastTarget = false;

        // 4. Central Login Card (Chinh giua man hinh, khong the bi khuat)
        GameObject cardObj = new GameObject("Login_Card");
        cardObj.transform.SetParent(canvas.transform, false);
        var cardRt = cardObj.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = new Vector2(0f, -40f);
        cardRt.sizeDelta = new Vector2(620f, 360f);

        var cardImg = cardObj.AddComponent<Image>();
        cardImg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);
        var outline = cardObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.42f, 0.55f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        // 4A. Label "TEN CHIEN BINH:"
        GameObject lblObj = new GameObject("NameLabel");
        lblObj.transform.SetParent(cardObj.transform, false);
        var lRt = lblObj.AddComponent<RectTransform>();
        lRt.anchoredPosition = new Vector2(0f, 115f);
        lRt.sizeDelta = new Vector2(460f, 30f);
        warriorNameLabel = lblObj.AddComponent<TextMeshProUGUI>();
        warriorNameLabel.text = "TEN CHIEN BINH:";
        warriorNameLabel.fontSize = 17;
        warriorNameLabel.fontStyle = FontStyles.Bold;
        warriorNameLabel.color = new Color(0.85f, 0.90f, 0.98f);
        warriorNameLabel.alignment = TextAlignmentOptions.Center;
        warriorNameLabel.raycastTarget = false;

        // 4B. Name Input Field (Rong rai 460x54, click focus nhap de dang)
        GameObject inObj = new GameObject("InputField");
        inObj.transform.SetParent(cardObj.transform, false);
        var inRt = inObj.AddComponent<RectTransform>();
        inRt.anchoredPosition = new Vector2(0f, 65f);
        inRt.sizeDelta = new Vector2(460f, 54f);

        var inImg = inObj.AddComponent<Image>();
        inImg.color = new Color(0.14f, 0.16f, 0.23f);
        inImg.raycastTarget = true; // Bat buoc true de bat click chuot

        var inOutline = inObj.AddComponent<Outline>();
        inOutline.effectColor = new Color(0.4f, 0.55f, 0.75f, 0.8f);
        inOutline.effectDistance = new Vector2(1.5f, -1.5f);

        nameInput = inObj.AddComponent<TMP_InputField>();
        nameInput.targetGraphic = inImg;

        // Text area / viewport
        GameObject inTxtObj = new GameObject("Text");
        inTxtObj.transform.SetParent(inObj.transform, false);
        var inTxtRt = inTxtObj.AddComponent<RectTransform>();
        inTxtRt.anchorMin = Vector2.zero;
        inTxtRt.anchorMax = Vector2.one;
        inTxtRt.offsetMin = new Vector2(16f, 0f);
        inTxtRt.offsetMax = new Vector2(-16f, 0f);

        var inTmp = inTxtObj.AddComponent<TextMeshProUGUI>();
        inTmp.text = "Vaelstrider";
        inTmp.fontSize = 20;
        inTmp.fontStyle = FontStyles.Bold;
        inTmp.color = Color.white;
        inTmp.alignment = TextAlignmentOptions.Center;
        inTmp.raycastTarget = false; // Bat buoc false de khong chan raycast cua InputField

        nameInput.textViewport = inTxtRt;
        nameInput.textComponent = inTmp;

        // Placeholder
        GameObject phObj = new GameObject("Placeholder");
        phObj.transform.SetParent(inObj.transform, false);
        var phRt = phObj.AddComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(16f, 0f);
        phRt.offsetMax = new Vector2(-16f, 0f);

        var phTmp = phObj.AddComponent<TextMeshProUGUI>();
        phTmp.text = "Nhap ten chien binh...";
        phTmp.fontSize = 20;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.6f, 0.65f, 0.75f, 0.5f);
        phTmp.alignment = TextAlignmentOptions.Center;
        phTmp.raycastTarget = false;
        nameInput.placeholder = phTmp;

        nameInput.interactable = true;

        // 4C. Play Button (BAT DAU HANH TRINH)
        playButton = CreateButton(cardObj.transform, "Btn_Play", "BAT DAU HANH TRINH", new Vector2(0f, -25f), new Vector2(460f, 60f), new Color(0.85f, 0.50f, 0.15f), 22, out playButtonText);

        // 4D. Quit Button (THOAT GAME)
        quitButton = CreateButton(cardObj.transform, "Btn_Quit", "THOAT GAME", new Vector2(0f, -100f), new Vector2(460f, 48f), new Color(0.55f, 0.20f, 0.20f), 17, out quitButtonText);
    }

    private Button CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color bgColor, float fontSize, out TextMeshProUGUI btnTxt)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = obj.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;
        var btn = obj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(obj.transform, false);
        var tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;
        btnTxt = txtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.text = text;
        btnTxt.fontSize = fontSize;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.raycastTarget = false;
        return btn;
    }
}
