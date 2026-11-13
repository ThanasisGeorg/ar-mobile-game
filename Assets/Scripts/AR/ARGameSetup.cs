using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Drives the AR-specific flow on top of Unity's "AR Session" and "XR Origin (Mobile AR)"
// objects in Game_AR (added with Tools > AR Tower Defense > Add AR Rig To Game_AR Scene):
// scan for a horizontal surface -> tap to place the battlefield -> play.
//
// The battlefield keeps exactly the same size in world units as in the classic
// version (so gameplay code and balance are identical); it is shown at table-top
// size by scaling the XR Origin, the standard AR Foundation technique for
// "scaling content" without touching the content itself.
public class ARGameSetup : MonoBehaviour
{
  public float boardWidthMeters = 0.9f;

  public Camera ARCamera { get; private set; }

  private XROrigin origin;
  private ARSession session;
  private ARCameraManager cameraManager;
  private ARRaycastManager raycastManager;
  private ARPlaneManager planeManager;
  private LevelLayout level;
  private GameManager gm;
  private GameHUD hud;
  private SessionTelemetry telemetry;
  private Transform reticle;

  private bool supported = true;
  private bool hasPlane;
  private bool boardPlaced;
  private bool gameStarted;

  // Diagnostics (shown on screen until the game starts, to troubleshoot a black camera image)
  private int cameraFrames;
  private string permissionStatus = "-";
  private float diagnosticsTimer;

  static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

  float WorldUnitsPerMeter => level.BoardWidth / boardWidthMeters;

  // Returns null when Game_AR does not contain the AR rig.
  public static ARGameSetup Create(LevelLayout level)
  {
    XROrigin xrOrigin = FindFirstObjectByType<XROrigin>();
    ARSession arSession = FindFirstObjectByType<ARSession>();

    if (xrOrigin == null || xrOrigin.Camera == null || arSession == null)
    {
      Debug.LogError("Game_AR has no AR Session / XR Origin. Run Tools > AR Tower Defense > Add AR Rig To Game_AR Scene.");
      return null;
    }

    Camera cam = xrOrigin.Camera;
    cam.tag = "MainCamera";
    cam.nearClipPlane = 0.1f;
    cam.farClipPlane = 2000f;
    xrOrigin.CameraYOffset = 0f;

    GameObject originGO = xrOrigin.gameObject;
    ARRaycastManager raycaster = originGO.GetComponent<ARRaycastManager>();
    if (raycaster == null) raycaster = originGO.AddComponent<ARRaycastManager>();
    ARPlaneManager planes = originGO.GetComponent<ARPlaneManager>();
    if (planes == null) planes = originGO.AddComponent<ARPlaneManager>();
    planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;

    ARGameSetup setup = originGO.AddComponent<ARGameSetup>();
    setup.origin = xrOrigin;
    setup.raycastManager = raycaster;
    setup.planeManager = planes;
    setup.level = level;
    setup.ARCamera = cam;
    setup.session = arSession;
    setup.cameraManager = cam.GetComponent<ARCameraManager>();

    originGO.transform.localScale = Vector3.one * setup.WorldUnitsPerMeter;
    setup.reticle = CreateReticle(setup.WorldUnitsPerMeter);
    level.gameObject.SetActive(false);

    return setup;
  }

  public void Init(GameManager manager, GameHUD gameHud, SessionTelemetry sessionTelemetry)
  {
    gm = manager;
    hud = gameHud;
    telemetry = sessionTelemetry;

    gm.StateChanged += OnStateChanged;
    planeManager.trackablesChanged.AddListener(OnPlanesChanged);
    ARSession.stateChanged += OnSessionStateChanged;

    telemetry.SetPhysicalCamera(ARCamera.transform, WorldUnitsPerMeter, boardWidthMeters);

    hud.SetStartAvailable(false);
    hud.SetInstruction("Στρέψτε την κάμερα προς ένα τραπέζι ή το πάτωμα και κινήστε αργά το κινητό, ώστε να εντοπιστεί μια επίπεδη επιφάνεια.");
  }

  IEnumerator Start()
  {
    if (cameraManager != null)
      cameraManager.frameReceived += OnCameraFrame;

    yield return RequestCameraPermission();

    if (permissionStatus == "denied")
    {
      supported = false;
      telemetry?.Log("ar_camera_permission_denied");
      hud?.SetInstruction("Δεν δόθηκε άδεια χρήσης της κάμερας. Επιτρέψτε την από τις Ρυθμίσεις → Εφαρμογές → " +
          Application.productName + " → Άδειες και ξανανοίξτε την έκδοση AR.");
      yield break;
    }

    if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
      yield return ARSession.CheckAvailability();

    if (ARSession.state == ARSessionState.Unsupported)
    {
      supported = false;
      telemetry?.Log("ar_unsupported");
      hud?.SetInstruction("Η συσκευή δεν υποστηρίζει Επαυξημένη Πραγματικότητα (Google Play Services for AR). " +
          "Πατήστε «Μενού» για επιστροφή.");
    }
  }

  IEnumerator RequestCameraPermission()
  {
#if UNITY_ANDROID && !UNITY_EDITOR
    if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
    {
      permissionStatus = "granted";
      yield break;
    }

    permissionStatus = "requested";
    var callbacks = new UnityEngine.Android.PermissionCallbacks();
    callbacks.PermissionGranted += _ => permissionStatus = "granted";
    callbacks.PermissionDenied += _ => permissionStatus = "denied";
    callbacks.PermissionDeniedAndDontAskAgain += _ => permissionStatus = "denied";
    UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera, callbacks);

    while (permissionStatus == "requested")
      yield return null;
#else
    permissionStatus = "n/a";
    yield break;
#endif
  }

  void OnCameraFrame(ARCameraFrameEventArgs args)
  {
    cameraFrames++;
  }

  void LateUpdate()
  {
    if (hud == null || gameStarted)
      return;

    diagnosticsTimer -= Time.unscaledDeltaTime;

    if (diagnosticsTimer > 0f)
      return;

    diagnosticsTimer = 0.5f;
    hud.SetDebugText(Diagnostics());
  }

  string Diagnostics()
  {
    var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
    string feature = "?";

    try
    {
      FieldInfo field = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);

      if (pipeline != null && field?.GetValue(pipeline) is ScriptableRendererData[] renderers && renderers.Length > 0 && renderers[0] != null)
        feature = renderers[0].rendererFeatures.Any(f => f != null && f.GetType().Name == "ARBackgroundRendererFeature") ? "yes" : "NO";
    }
    catch (Exception)
    {
    }

    ARCameraBackground background = ARCamera.GetComponent<ARCameraBackground>();

    return "AR session: " + ARSession.state +
        "  |  tracking: " + ARSession.notTrackingReason +
        "  |  camera permission: " + permissionStatus +
        "\ncamera frames: " + cameraFrames +
        "  |  background: " + (background != null && background.enabled ? "on" : "off") +
        "  |  AR renderer feature: " + feature +
        "  |  pipeline: " + (pipeline != null ? pipeline.name : "none") +
        "  |  graphics: " + SystemInfo.graphicsDeviceType +
        "\nXR loader: " + LoaderName() +
        "  |  camera subsystem: " + (cameraManager == null || cameraManager.subsystem == null ? "none" : cameraManager.subsystem.running ? "running" : "stopped") +
        "  |  session subsystem: " + (session.subsystem == null ? "none" : session.subsystem.running ? "running" : "stopped");
  }

  static string LoaderName()
  {
    var settings = UnityEngine.XR.Management.XRGeneralSettings.Instance;

    if (settings == null || settings.Manager == null)
      return "no XR settings";

    return settings.Manager.activeLoader != null ? settings.Manager.activeLoader.name : "NOT initialized";
  }

  void OnDestroy()
  {
    ARSession.stateChanged -= OnSessionStateChanged;

    if (cameraManager != null)
      cameraManager.frameReceived -= OnCameraFrame;

    if (gm != null)
      gm.StateChanged -= OnStateChanged;

    if (planeManager != null)
      planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
  }

  void Update()
  {
    if (!supported || gm == null || gameStarted)
      return;

    // Preview where the board would go (centre of the screen) until it is placed once.
    if (!boardPlaced)
    {
      Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
      bool hitPlane = raycastManager.Raycast(center, hits, TrackableType.PlaneWithinPolygon);
      reticle.gameObject.SetActive(hitPlane);

      if (hitPlane)
        reticle.SetPositionAndRotation(hits[0].pose.position + Vector3.up * 0.01f, Quaternion.identity);
    }

    if (gm.State != GameState.WaitingToStart)
      return;

    if (PointerInput.TryGetTap(out Vector2 tap, out bool overUI) && !overUI)
    {
      if (raycastManager.Raycast(tap, hits, TrackableType.PlaneWithinPolygon))
      {
        PlaceBoard(hits[0].pose);
      }
      else
      {
        hud.ShowToast(hasPlane ? "Πατήστε πάνω σε μια εντοπισμένη επιφάνεια" : "Δεν έχει εντοπιστεί ακόμη επιφάνεια");
      }
    }
  }

  void PlaceBoard(Pose hitPose)
  {
    Transform space = origin.TrackablesParent != null ? origin.TrackablesParent : origin.transform;

    // Hit point and camera direction in session (real-world) space.
    Vector3 sessionPoint = space.InverseTransformPoint(hitPose.position);
    Vector3 cameraForward = space.InverseTransformDirection(ARCamera.transform.forward);
    cameraForward.y = 0f;

    if (cameraForward.sqrMagnitude < 0.0001f)
      cameraForward = Vector3.forward;

    // Rotate the world so the board faces the player (spawn on the left, base on the right).
    float yaw = Mathf.Atan2(cameraForward.x, cameraForward.z) * Mathf.Rad2Deg;
    Quaternion rotation = Quaternion.Euler(0f, -yaw, 0f);
    float scale = WorldUnitsPerMeter;

    // Move the origin so that the tapped point coincides with the board centre.
    Vector3 boardCenter = level.transform.position;
    origin.transform.localScale = Vector3.one * scale;
    origin.transform.rotation = rotation;
    origin.transform.position = boardCenter - rotation * (sessionPoint * scale);

    level.gameObject.SetActive(true);
    reticle.gameObject.SetActive(false);

    if (!boardPlaced)
    {
      boardPlaced = true;
      hud.SetStartAvailable(true);
      hud.SetInstruction("Πατήστε σε άλλο σημείο για να μετακινήσετε το πεδίο. Πατήστε «Έναρξη» όταν είστε έτοιμοι. " +
          "Στο παιχνίδι πατήστε στο χορτάρι για να χτίσετε πύργους και κινηθείτε γύρω από το πεδίο για καλύτερη θέα.");
    }

    telemetry.MarkBoardPlaced();
  }

  void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> changes)
  {
    foreach (ARPlane plane in changes.added)
    {
      AddPlaneVisual(plane);
      plane.gameObject.SetActive(!gameStarted);
    }

    if (!hasPlane && changes.added.Count > 0)
    {
      hasPlane = true;
      telemetry.MarkFirstPlane();

      if (!boardPlaced)
        hud.SetInstruction("Βρέθηκε επιφάνεια! Πατήστε πάνω της για να τοποθετήσετε το πεδίο της μάχης.");
    }
  }

  static void AddPlaneVisual(ARPlane plane)
  {
    if (plane.GetComponent<MeshRenderer>() != null)
      return;

    plane.gameObject.AddComponent<MeshFilter>();
    MeshRenderer renderer = plane.gameObject.AddComponent<MeshRenderer>();
    renderer.sharedMaterial = GameMaterials.Transparent(new Color(1f, 1f, 1f, 0.2f));
    renderer.shadowCastingMode = ShadowCastingMode.Off;
    plane.gameObject.AddComponent<ARPlaneMeshVisualizer>();
  }

  void OnStateChanged(GameState state)
  {
    if (state != GameState.Playing || gameStarted)
      return;

    // Once the game starts, the board stays fixed: stop plane detection and hide plane visuals.
    gameStarted = true;
    hud.SetDebugText("");
    reticle.gameObject.SetActive(false);
    planeManager.SetTrackablesActive(false);
    planeManager.enabled = false;
  }

  void OnSessionStateChanged(ARSessionStateChangedEventArgs args)
  {
    bool tracking = args.state == ARSessionState.SessionTracking;
    telemetry?.MarkTracking(tracking, ARSession.notTrackingReason.ToString());

    if (gm == null || !gameStarted)
      return;

    // Pause the game while the device has lost tracking: the board would otherwise drift.
    gm.SetSystemPause(!tracking && gm.State == GameState.Playing,
        "Χάθηκε η παρακολούθηση του χώρου.\nΣτρέψτε την κάμερα προς το πεδίο και κινήστε αργά το κινητό.");
  }

  static Transform CreateReticle(float unitsPerMeter)
  {
    GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    reticle.name = "Placement Reticle";
    Destroy(reticle.GetComponent<Collider>());

    float size = 0.12f * unitsPerMeter;
    reticle.transform.localScale = new Vector3(size, 0.002f * unitsPerMeter, size);
    reticle.GetComponent<Renderer>().sharedMaterial = GameMaterials.Transparent(new Color(0.3f, 0.8f, 1f, 0.6f));
    reticle.SetActive(false);
    return reticle.transform;
  }
}
