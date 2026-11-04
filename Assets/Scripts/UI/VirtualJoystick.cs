using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI References")]
    [SerializeField] private RectTransform containerRect;
    [SerializeField] private RectTransform handleRect;
    [SerializeField] private float handleRange = 80f;
    [SerializeField] private Vector2 handleSize = new Vector2(90f, 90f);

    [Header("Player Target")]
    [SerializeField] private PlayerController playerController;

    private Vector2 inputVector = Vector2.zero;
    private Vector3 baseScale = Vector3.zero;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (containerRect == null) containerRect = GetComponent<RectTransform>();
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
            if (baseScale == Vector3.zero) baseScale = Vector3.one;
        }
        EnsureRoundedSprites();
        ApplyCustomPreferences();
    }

    void Start()
    {
        ApplyCustomPreferences();
    }

    public void ApplyCustomPreferences()
    {
        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
            if (baseScale == Vector3.zero) baseScale = Vector3.one;
        }

        float scaleMod = PlayerPrefs.GetFloat("Game_Controls_ButtonScale", 1.40f);
        if (scaleMod < 1.35f) scaleMod = 1.40f;
        float opacity = PlayerPrefs.GetFloat("Game_Controls_Opacity", 1.0f);
        if (opacity < 0.25f) opacity = 1.0f; // Bảo vệ an toàn: tránh vô tình bị mờ tịt

        transform.localScale = baseScale * scaleMod;

        if (PlayerPrefs.HasKey("Game_Controls_Joy_PosX"))
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                float posX = PlayerPrefs.GetFloat("Game_Controls_Joy_PosX", 250f);
                float posY = PlayerPrefs.GetFloat("Game_Controls_Joy_PosY", 250f);
                rt.anchoredPosition = new Vector2(posX, posY);
            }
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = opacity;
    }

    public static void ApplyToAllJoysticks()
    {
        var joysticks = Object.FindObjectsByType<VirtualJoystick>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var j in joysticks)
        {
            if (j != null) j.ApplyCustomPreferences();
        }
    }

    private static Sprite _proceduralBaseSprite;
    private static Sprite _proceduralHandleSprite;

    private static Sprite GetProceduralCircle(int size, Color ringColor, Color fillColor)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = (size - 2) * 0.5f;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    if (dist >= radius - 3.5f) tex.SetPixel(x, y, ringColor);
                    else tex.SetPixel(x, y, fillColor);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite LoadSpriteSafe(string resPath, string editorPath)
    {
        Sprite s = Resources.Load<Sprite>(resPath);
        if (s != null) return s;
        var all = Resources.LoadAll<Sprite>(resPath);
        if (all != null && all.Length > 0) return all[0];
        string noFolder = System.IO.Path.GetFileNameWithoutExtension(resPath);
        s = Resources.Load<Sprite>(noFolder);
        if (s != null) return s;
#if UNITY_EDITOR
        s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(editorPath);
        if (s != null) return s;
#endif
        return null;
    }

    private void EnsureRoundedSprites()
    {
        // 1. Nạp đúng 100% Cần điều khiển Pixel Art màu xanh (Joystick Base.png) bạn đã vẽ
        Image bgImg = GetComponent<Image>();
        if (bgImg != null)
        {
            Sprite baseSprite = LoadSpriteSafe("UI/Joystick Base", "Assets/Sprites/UI/Joystick Base.png");
            if (baseSprite == null) baseSprite = LoadSpriteSafe("UI/joystick_base", "Assets/Sprites/UI/joystick_base.png");

            if (baseSprite == null && bgImg.sprite == null)
            {
                if (_proceduralBaseSprite == null)
                {
                    _proceduralBaseSprite = GetProceduralCircle(128, new Color(0.04f, 0.75f, 0.81f, 0.85f), new Color(0.12f, 0.16f, 0.24f, 0.55f));
                }
                baseSprite = _proceduralBaseSprite;
            }

            if (baseSprite != null && bgImg.sprite != baseSprite)
            {
                bgImg.sprite = baseSprite;
                bgImg.color = Color.white;
            }
            bgImg.enabled = true;
        }

        // 2. Nạp đúng Núm điều khiển Pixel Art màu xanh (Joystick Handle.png) bạn đã vẽ
        if (handleRect != null)
        {
            if (handleSize == new Vector2(170f, 170f) || handleSize == Vector2.zero)
            {
                handleSize = new Vector2(90f, 90f);
            }
            handleRect.sizeDelta = handleSize;
            Image handleImg = handleRect.GetComponent<Image>();
            if (handleImg != null)
            {
                Sprite handleSprite = LoadSpriteSafe("UI/Joystick Handle", "Assets/Sprites/UI/Joystick Handle.png");
                if (handleSprite == null) handleSprite = LoadSpriteSafe("UI/joystick_handle", "Assets/Sprites/UI/joystick_handle.png");

                if (handleSprite == null && handleImg.sprite == null)
                {
                    if (_proceduralHandleSprite == null)
                    {
                        _proceduralHandleSprite = GetProceduralCircle(64, new Color(0.04f, 0.75f, 0.81f, 0.95f), new Color(0.04f, 0.75f, 0.81f, 0.85f));
                    }
                    handleSprite = _proceduralHandleSprite;
                }

                if (handleSprite != null && handleImg.sprite != handleSprite)
                {
                    handleImg.sprite = handleSprite;
                    handleImg.color = Color.white;
                }
                handleImg.enabled = true;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            containerRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out position
        );

        position = Vector2.ClampMagnitude(position, handleRange);
        if (handleRect != null)
        {
            handleRect.anchoredPosition = position;
        }

        inputVector = position / handleRange;
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.SetMobileInput(inputVector);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        if (handleRect != null)
        {
            handleRect.anchoredPosition = Vector2.zero;
        }

        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.SetMobileInput(Vector2.zero);
        }
    }
}
