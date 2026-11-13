using UnityEngine;
using UnityEngine.SceneManagement;

// Assembles a game scene. Everything that affects gameplay (level, enemies,
// towers, economy, UI) is shared; only the camera / interaction space differs:
// Classic = touch-controlled virtual camera, AR = the phone camera itself.
public class GameBootstrap : MonoBehaviour
{
  public GameMode Mode { get; private set; }
  public LevelLayout Level { get; private set; }

  public void Setup(GameMode mode, Scene scene)
  {
    Mode = mode;
    StudySession.CurrentMode = mode;

    DisableLegacyObjects(scene);
    Level = LevelBuilder.Build();

    GameManager gm = Ensure<GameManager>("GameManager");
    gm.mode = mode;

    GameEconomy economy = FindFirstObjectByType<GameEconomy>();
    if (economy == null) economy = gm.gameObject.AddComponent<GameEconomy>();

    WaveManager waves = Ensure<WaveManager>("WaveManager");
    if (waves.enemyPrefab == null) waves.enemyPrefab = Resources.Load<GameObject>("Prefabs/Enemy");
    waves.spawnPoint = Level.spawnPoint;
    waves.target = Level.baseTarget;
    waves.waypoints = Level.waypoints;

    TowerPlacement placement = Ensure<TowerPlacement>("TowerManager");
    if (placement.towerPrefab == null) placement.towerPrefab = Resources.Load<GameObject>("Prefabs/Tower");
    if (placement.ghostPrefab == null) placement.ghostPrefab = Resources.Load<GameObject>("Prefabs/Tower_Ghost");
    placement.level = Level;
    placement.gridSize = Level.gridSize;

    ClassicCameraController classicCamera = null;
    ARGameSetup ar = null;

    if (mode == GameMode.Classic)
    {
      Camera cam = Camera.main;

      if (cam == null)
      {
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        cam = camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
      }

      classicCamera = cam.GetComponent<ClassicCameraController>();
      if (classicCamera == null) classicCamera = cam.gameObject.AddComponent<ClassicCameraController>();
      classicCamera.Frame(Level.BoardBounds);
      placement.cam = cam;
    }
    else
    {
      // The AR rig brings its own camera; other scene cameras would compete for "MainCamera".
      foreach (Camera existing in FindObjectsByType<Camera>(FindObjectsSortMode.None))
      {
        if (existing.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() == null)
          existing.gameObject.SetActive(false);
      }

      ar = ARGameSetup.Create(Level);
      placement.cam = ar != null ? ar.ARCamera : null;
    }

    SessionTelemetry telemetry = new GameObject("Session Telemetry").AddComponent<SessionTelemetry>();
    telemetry.Init(mode, gm, economy, waves, placement, classicCamera);

    GameHUD hud = GameHUD.Create(mode, gm, economy, waves, placement, telemetry);

    if (ar != null)
    {
      ar.Init(gm, hud, telemetry);
    }
    else if (mode == GameMode.AR)
    {
      Level.gameObject.SetActive(false);
      hud.SetStartAvailable(false);
      hud.SetInstruction("Η σκηνή AR δεν έχει ρυθμιστεί: στο Unity εκτελέστε Tools → AR Tower Defense → " +
          "Add AR Rig To Game_AR Scene και ξαναφτιάξτε το build. Πατήστε «Μενού» για επιστροφή.");
    }
  }

  static T Ensure<T>(string name) where T : Component
  {
    T existing = FindFirstObjectByType<T>();
    return existing != null ? existing : new GameObject(name).AddComponent<T>();
  }

  // The first prototype of Game_Classic had a hand-made level and HUD; they are replaced by
  // the shared, code-built versions so both game modes are identical.
  static void DisableLegacyObjects(Scene scene)
  {
    foreach (GameObject root in scene.GetRootGameObjects())
    {
      if (root.name == "Ground" || root.name == "Target" || root.name == "EnemySpawner")
        root.SetActive(false);
    }

    foreach (HUDController legacyHud in FindObjectsByType<HUDController>(FindObjectsSortMode.None))
      legacyHud.gameObject.SetActive(false);
  }
}
