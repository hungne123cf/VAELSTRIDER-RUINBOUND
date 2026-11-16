using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shared presentation rules for VAELSTRIDER: RUINBOUND.
/// Keeps uGUI at a stable 16:9 reference, gives interactive controls crisp
/// cyan/ember feedback, and does so without replacing each screen's authored art.
/// </summary>
public static class RuinboundVisualLanguage
{
    public static readonly Color Ember = new Color(1.00f, 0.48f, 0.16f, 1f);
    public static readonly Color Cyan = new Color(0.22f, 0.90f, 1.00f, 1f);
    public static readonly Color Obsidian = new Color(0.035f, 0.055f, 0.090f, 1f);

    public const float ReferenceWidth = 1920f;
    public const float ReferenceHeight = 1080f;

    public static void ApplyToAllCanvases()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas != null && canvas.isRootCanvas)
                ApplyToCanvas(canvas);
        }
    }

    public static void ApplyToCanvas(Canvas canvas)
    {
        if (canvas == null) return;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            // A single reference grid prevents stretched UI across 16:9 through 20:9.
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
        }

        var buttons = canvas.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            if (button == null || button.GetComponent<RuinboundButtonFeedback>() != null) continue;
            button.gameObject.AddComponent<RuinboundButtonFeedback>();
        }
    }
}

/// <summary>
/// Deliberately uses a two-reference-pixel nudge and hard colour changes rather
/// than smooth scaling/blur, preserving the pixel-art feel on mouse and touch.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class RuinboundButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Button button;
    private Graphic graphic;
    private RectTransform rect;
    private Vector2 restingPosition;
    private Color restingColor;
    private bool pointerInside;
    private bool pointerDown;

    private void Awake()
    {
        button = GetComponent<Button>();
        graphic = button.targetGraphic;
        rect = transform as RectTransform;
        if (rect != null) restingPosition = rect.anchoredPosition;
        if (graphic != null) restingColor = graphic.color;
    }

    private void OnEnable()
    {
        if (rect != null) restingPosition = rect.anchoredPosition;
        if (graphic != null) restingColor = graphic.color;
        ApplyState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        ApplyState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        pointerDown = false;
        ApplyState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDown = true;
        ApplyState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerDown = false;
        ApplyState();
    }

    private void ApplyState()
    {
        if (button != null && !button.interactable)
        {
            Restore();
            return;
        }

        if (rect != null)
            rect.anchoredPosition = restingPosition + (pointerDown ? new Vector2(0f, -2f) : pointerInside ? new Vector2(0f, 2f) : Vector2.zero);

        if (graphic == null) return;
        if (pointerDown)
            graphic.color = Color.Lerp(restingColor, RuinboundVisualLanguage.Ember, 0.52f);
        else if (pointerInside)
            graphic.color = Color.Lerp(restingColor, RuinboundVisualLanguage.Cyan, 0.32f);
        else
            graphic.color = restingColor;
    }

    private void Restore()
    {
        if (rect != null) rect.anchoredPosition = restingPosition;
        if (graphic != null) graphic.color = restingColor;
    }

    private void OnDisable() => Restore();
}
