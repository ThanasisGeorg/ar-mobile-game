using UnityEngine;

// Camera for the classic (non-AR) version: an angled view of the board with
// one-finger drag to pan and pinch (or mouse wheel) to zoom - the usual
// interaction of mobile strategy games.
[RequireComponent(typeof(Camera))]
public class ClassicCameraController : MonoBehaviour
{
  public float pitch = 55f;
  public float yaw = 0f;
  public float minDistance = 8f;
  public float maxDistance = 40f;
  public float zoomSpeed = 1.5f;

  public Vector3 focus;
  public float distance = 22f;
  public Bounds bounds = new Bounds(Vector3.zero, new Vector3(24f, 1f, 14f));

  public int PanGestures { get; private set; }
  public int ZoomGestures { get; private set; }

  private Camera cam;
  private bool wasPanning;
  private bool wasZooming;

  void Awake()
  {
    cam = GetComponent<Camera>();
  }

  // Positions the camera so that the whole board is visible.
  public void Frame(Bounds board)
  {
    bounds = board;
    focus = new Vector3(board.center.x, board.center.y, board.center.z - board.size.z * 0.05f);

    if (cam == null)
      cam = GetComponent<Camera>();

    float verticalFov = cam.fieldOfView * Mathf.Deg2Rad;
    float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * cam.aspect);

    float fitWidth = board.size.x * 0.5f / Mathf.Tan(horizontalFov * 0.5f);
    float fitDepth = board.size.z * 0.5f / Mathf.Tan(verticalFov * 0.5f) / Mathf.Sin(pitch * Mathf.Deg2Rad);

    distance = Mathf.Clamp(Mathf.Max(fitWidth, fitDepth) * 1.1f, minDistance, maxDistance);
    maxDistance = Mathf.Max(maxDistance, distance * 1.2f);
    Apply();
  }

  void LateUpdate()
  {
    GameManager gm = GameManager.Instance;

    if (gm != null && gm.State == GameState.Paused)
      return;

    Vector2 drag = PointerInput.DragDelta;
    float pinch = PointerInput.PinchDelta;
    float scroll = PointerInput.ScrollDelta;

    bool panning = drag.sqrMagnitude > 0.01f;
    bool zooming = Mathf.Abs(pinch) > 0.01f || Mathf.Abs(scroll) > 0.01f;

    if (panning)
    {
      float worldPerPixel = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
      Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
      Vector3 right = yawRotation * Vector3.right;
      Vector3 forward = yawRotation * Vector3.forward;

      focus -= (right * drag.x + forward * drag.y / Mathf.Sin(pitch * Mathf.Deg2Rad)) * worldPerPixel;
    }

    if (zooming)
    {
      float factor = 1f - pinch / Screen.height * zoomSpeed * 2f - scroll * 0.1f * zoomSpeed;
      distance = Mathf.Clamp(distance * Mathf.Clamp(factor, 0.5f, 1.5f), minDistance, maxDistance);
    }

    if (panning && !wasPanning) PanGestures++;
    if (zooming && !wasZooming) ZoomGestures++;

    wasPanning = panning;
    wasZooming = zooming;

    focus.x = Mathf.Clamp(focus.x, bounds.min.x, bounds.max.x);
    focus.z = Mathf.Clamp(focus.z, bounds.min.z, bounds.max.z);

    Apply();
  }

  void Apply()
  {
    Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
    transform.rotation = rotation;
    transform.position = focus - rotation * Vector3.forward * distance;
  }
}
