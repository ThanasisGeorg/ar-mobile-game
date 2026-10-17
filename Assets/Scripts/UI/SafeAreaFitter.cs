using UnityEngine;

// Keeps a full-screen UI container inside the device safe area (notches, rounded corners).
public class SafeAreaFitter : MonoBehaviour
{
  private Rect applied;
  private RectTransform rt;

  void Awake()
  {
    rt = (RectTransform)transform;
    Apply();
  }

  void Update()
  {
    if (Screen.safeArea != applied)
      Apply();
  }

  void Apply()
  {
    Rect safe = Screen.safeArea;
    applied = safe;

    if (Screen.width <= 0 || Screen.height <= 0)
      return;

    rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
    rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
    rt.offsetMin = Vector2.zero;
    rt.offsetMax = Vector2.zero;
  }
}
