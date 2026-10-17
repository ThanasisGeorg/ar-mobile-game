using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Small helper to build the uGUI interface from code (identical in both game versions).
public static class UIFactory
{
  public static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.13f, 0.92f);
  public static readonly Color BarColor = new Color(0.07f, 0.09f, 0.13f, 0.75f);
  public static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
  public static readonly Color PrimaryColor = new Color(0.18f, 0.45f, 0.85f);
  public static readonly Color AccentColor = new Color(0.95f, 0.6f, 0.15f);
  public static readonly Color SuccessColor = new Color(0.2f, 0.62f, 0.35f);
  public static readonly Color DangerColor = new Color(0.78f, 0.25f, 0.25f);
  public static readonly Color NeutralColor = new Color(0.3f, 0.33f, 0.4f);
  public static readonly Color TextColor = new Color(0.96f, 0.96f, 0.98f);
  public static readonly Color MutedTextColor = new Color(0.75f, 0.78f, 0.85f);

  public static Canvas CreateCanvas(string name, int sortingOrder = 0)
  {
    EnsureEventSystem();

    GameObject go = new GameObject(name);
    Canvas canvas = go.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = sortingOrder;

    CanvasScaler scaler = go.AddComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920f, 1080f);
    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
    scaler.matchWidthOrHeight = 0.5f;

    go.AddComponent<GraphicRaycaster>();
    return canvas;
  }

  public static void EnsureEventSystem()
  {
    if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
      return;

    GameObject go = new GameObject("EventSystem");
    go.AddComponent<EventSystem>();
    go.AddComponent<InputSystemUIInputModule>();
  }

  // Full-screen container that respects the device safe area (notches, rounded corners).
  public static RectTransform SafeArea(Transform canvas)
  {
    RectTransform rt = Rect("SafeArea", canvas);
    Stretch(rt);
    rt.gameObject.AddComponent<SafeAreaFitter>();
    return rt;
  }

  public static RectTransform Rect(string name, Transform parent)
  {
    GameObject go = new GameObject(name, typeof(RectTransform));
    go.transform.SetParent(parent, false);
    return (RectTransform)go.transform;
  }

  public static RectTransform Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
  {
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.one;
    rt.pivot = new Vector2(0.5f, 0.5f);
    rt.offsetMin = new Vector2(left, bottom);
    rt.offsetMax = new Vector2(-right, -top);
    return rt;
  }

  public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
  {
    rt.anchorMin = anchor;
    rt.anchorMax = anchor;
    rt.pivot = pivot;
    rt.anchoredPosition = position;
    rt.sizeDelta = size;
    return rt;
  }

  public static Image Panel(Transform parent, string name, Color color, bool blocksInput = true)
  {
    RectTransform rt = Rect(name, parent);
    Image image = rt.gameObject.AddComponent<Image>();
    image.color = color;
    image.raycastTarget = blocksInput;
    return image;
  }

  public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color,
      TextAlignmentOptions alignment = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
  {
    RectTransform rt = Rect("Text", parent);
    TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
    tmp.text = text;
    tmp.fontSize = size;
    tmp.color = color;
    tmp.alignment = alignment;
    tmp.fontStyle = style;
    tmp.raycastTarget = false;
    return tmp;
  }

  public static Button Button(Transform parent, string label, Color color, Action onClick, float fontSize = 40f)
  {
    Image image = Panel(parent, "Button " + label.Replace("\n", " "), color);
    Button button = image.gameObject.AddComponent<Button>();
    button.targetGraphic = image;

    ColorBlock colors = button.colors;
    colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
    colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
    colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
    button.colors = colors;

    TextMeshProUGUI text = Text(image.transform, label, fontSize, TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
    Stretch(text.rectTransform, 12f, 12f, 6f, 6f);
    text.enableAutoSizing = true;
    text.fontSizeMin = fontSize * 0.5f;
    text.fontSizeMax = fontSize;

    if (onClick != null)
      button.onClick.AddListener(() => onClick());

    return button;
  }

  public static void SetButtonLabel(Button button, string label)
  {
    TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
    if (text != null) text.text = label;
  }

  public static void SetButtonColor(Button button, Color color)
  {
    if (button.targetGraphic != null)
      button.targetGraphic.color = color;
  }

  public static VerticalLayoutGroup VerticalLayout(GameObject go, float spacing, RectOffset padding = null,
      TextAnchor alignment = TextAnchor.UpperCenter)
  {
    VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
    layout.spacing = spacing;
    layout.padding = padding ?? new RectOffset(0, 0, 0, 0);
    layout.childAlignment = alignment;
    layout.childControlWidth = true;
    layout.childControlHeight = true;
    layout.childForceExpandWidth = true;
    layout.childForceExpandHeight = false;
    return layout;
  }

  public static HorizontalLayoutGroup HorizontalLayout(GameObject go, float spacing, RectOffset padding = null)
  {
    HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
    layout.spacing = spacing;
    layout.padding = padding ?? new RectOffset(0, 0, 0, 0);
    layout.childAlignment = TextAnchor.MiddleCenter;
    layout.childControlWidth = true;
    layout.childControlHeight = true;
    layout.childForceExpandWidth = true;
    layout.childForceExpandHeight = true;
    return layout;
  }

  public static LayoutElement Size(Component c, float preferredHeight, float preferredWidth = -1f, float flexibleWidth = -1f)
  {
    LayoutElement element = c.gameObject.GetComponent<LayoutElement>();
    if (element == null) element = c.gameObject.AddComponent<LayoutElement>();
    element.preferredHeight = preferredHeight;
    element.minHeight = preferredHeight;
    if (preferredWidth >= 0f) element.preferredWidth = preferredWidth;
    if (flexibleWidth >= 0f) element.flexibleWidth = flexibleWidth;
    return element;
  }
}
