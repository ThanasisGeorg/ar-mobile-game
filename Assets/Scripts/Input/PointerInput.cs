using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Unified touch + mouse gestures, shared by both game versions so that the
// interaction model (tap to build, drag/pinch for the camera) is identical.
// State is computed lazily once per frame, so any number of scripts can read it.
public static class PointerInput
{
  // A press that does not move is a tap; long presses are allowed so players can aim with the ghost preview.
  const float MaxTapDuration = 2f;

  static int lastFrame = -1;

  static bool pressed;
  static bool dragging;
  static bool multiTouch;
  static bool pressStartedOverUI;
  static Vector2 pressStart;
  static float pressStartTime;

  static bool tapThisFrame;
  static Vector2 tapPosition;
  static Vector2 dragDelta;
  static float pinchDelta;
  static float scrollDelta;
  static bool hasPointer;
  static Vector2 pointerPosition;

  static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

  public static bool IsTouchDevice => Touchscreen.current != null;

  public static bool IsDragging { get { Refresh(); return dragging || multiTouch; } }

  // Screen-space movement of a one-finger drag (or right/middle mouse drag) this frame.
  public static Vector2 DragDelta { get { Refresh(); return dragDelta; } }

  // Change in distance between two fingers this frame, in pixels (positive = zoom in).
  public static float PinchDelta { get { Refresh(); return pinchDelta; } }

  public static float ScrollDelta { get { Refresh(); return scrollDelta; } }

  public static bool TryGetTap(out Vector2 position, out bool overUI)
  {
    Refresh();
    position = tapPosition;
    overUI = tapThisFrame && IsOverUI(tapPosition);
    return tapThisFrame;
  }

  // Current pointer position: mouse cursor, or the finger while it touches the screen.
  public static bool TryGetPointer(out Vector2 position)
  {
    Refresh();
    position = pointerPosition;
    return hasPointer;
  }

  public static bool IsOverUI(Vector2 screenPosition)
  {
    EventSystem eventSystem = EventSystem.current;

    if (eventSystem == null)
      return false;

    PointerEventData data = new PointerEventData(eventSystem) { position = screenPosition };
    uiHits.Clear();
    eventSystem.RaycastAll(data, uiHits);
    return uiHits.Count > 0;
  }

  static float DragThreshold => Mathf.Max(12f, (Screen.dpi > 0 ? Screen.dpi : 160f) * 0.08f);

  static void Refresh()
  {
    if (lastFrame == Time.frameCount)
      return;

    lastFrame = Time.frameCount;
    tapThisFrame = false;
    dragDelta = Vector2.zero;
    pinchDelta = 0f;
    scrollDelta = 0f;
    hasPointer = false;

    if (!EnhancedTouchSupport.enabled)
      EnhancedTouchSupport.Enable();

    var touches = ETouch.activeTouches;

    if (touches.Count > 0)
    {
      HandleTouches(touches);
      return;
    }

    Mouse mouse = Mouse.current;

    if (mouse != null)
    {
      HandleMouse(mouse);
    }
    else
    {
      pressed = false;
      dragging = false;
      multiTouch = false;
    }
  }

  static void HandleTouches(UnityEngine.InputSystem.Utilities.ReadOnlyArray<ETouch> touches)
  {
    if (touches.Count >= 2)
    {
      ETouch a = touches[0];
      ETouch b = touches[1];

      if (!pressed)
        BeginPress(a.screenPosition);

      multiTouch = true;

      float current = Vector2.Distance(a.screenPosition, b.screenPosition);
      float previous = Vector2.Distance(a.screenPosition - a.delta, b.screenPosition - b.delta);
      pinchDelta = current - previous;

      if (!pressStartedOverUI)
        dragDelta = (a.delta + b.delta) * 0.5f;

      return;
    }

    ETouch touch = touches[0];
    Vector2 position = touch.screenPosition;

    hasPointer = true;
    pointerPosition = position;

    switch (touch.phase)
    {
      case TouchPhase.Began:
        BeginPress(position);
        break;

      case TouchPhase.Moved:
      case TouchPhase.Stationary:
        if (!pressed)
          BeginPress(position);
        UpdatePress(position, touch.delta);
        break;

      case TouchPhase.Ended:
        if (!pressed)
          BeginPress(position);
        UpdatePress(position, touch.delta);
        EndPress(position);
        break;

      case TouchPhase.Canceled:
        pressed = false;
        dragging = false;
        multiTouch = false;
        break;
    }
  }

  static void HandleMouse(Mouse mouse)
  {
    Vector2 position = mouse.position.ReadValue();
    Vector2 delta = mouse.delta.ReadValue();

    hasPointer = true;
    pointerPosition = position;

    // Depending on platform/settings, scroll is reported as ~120 or ~1 per notch; normalise to notches.
    float scroll = mouse.scroll.ReadValue().y;
    scrollDelta = Mathf.Abs(scroll) > 10f ? scroll / 120f : scroll;

    if (mouse.leftButton.wasPressedThisFrame)
    {
      BeginPress(position);
    }
    else if (mouse.leftButton.isPressed && pressed)
    {
      UpdatePress(position, delta);
    }

    if (mouse.leftButton.wasReleasedThisFrame && pressed)
    {
      EndPress(position);
    }

    if ((mouse.rightButton.isPressed || mouse.middleButton.isPressed) && !IsOverUI(position))
    {
      dragDelta += delta;
    }
  }

  static void BeginPress(Vector2 position)
  {
    pressed = true;
    dragging = false;
    multiTouch = false;
    pressStart = position;
    pressStartTime = Time.unscaledTime;
    pressStartedOverUI = IsOverUI(position);
  }

  static void UpdatePress(Vector2 position, Vector2 delta)
  {
    if (!dragging && (position - pressStart).magnitude > DragThreshold)
      dragging = true;

    if (dragging && !multiTouch && !pressStartedOverUI)
      dragDelta += delta;
  }

  static void EndPress(Vector2 position)
  {
    if (!dragging && !multiTouch && Time.unscaledTime - pressStartTime <= MaxTapDuration)
    {
      tapThisFrame = true;
      tapPosition = position;
    }

    pressed = false;
    dragging = false;
    multiTouch = false;
  }
}
