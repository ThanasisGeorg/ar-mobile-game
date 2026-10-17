using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

// Collects objective UX metrics during one play session (one participant, one
// game version) and writes them, together with the questionnaire answers, to CSV.
public class SessionTelemetry : MonoBehaviour
{
  public static SessionTelemetry Instance;

  public GameMode mode;
  public int participantId;
  public string sessionId;
  public int orderIndex;

  struct LoggedEvent
  {
    public float time;
    public string type;
    public string detail;
  }

  private readonly List<LoggedEvent> events = new List<LoggedEvent>();
  private readonly List<float> frameTimes = new List<float>(40000);
  private readonly List<float> placementTimes = new List<float>();

  private DateTime startedAt;
  private float sceneStart;
  private float gameStartTime = -1f;
  private float finishTime = -1f;

  private float playTime;
  private float pauseTime;
  private float systemPauseTime;

  private int tapsTotal, tapsUI, tapsWorld;
  private int towersPlaced, failNoCoins, failInvalid, failOccupied, failMissed;
  private float firstTowerTime = -1f;
  private int kills, leaks, userPauses;
  private string outcome = "incomplete";

  // AR specific
  private float firstPlaneTime = -1f;
  private float boardPlacedTime = -1f;
  private int boardPlacements;
  private int trackingLosses;
  private float trackingLossTime;
  private bool trackingLost;
  private Transform physicalCamera;
  private float worldUnitsPerMeter = 1f;
  private Vector3 lastCameraPos;
  private Quaternion lastCameraRot;
  private float devicePath;
  private float deviceRotation;
  private float boardWidthMeters;

  private float batteryStart;
  private float batteryEnd = -1f;

  private bool sessionWritten;
  private bool gameplayFinalized;

  private GameManager gm;
  private GameEconomy economy;
  private WaveManager waves;
  private TowerPlacement placement;
  private ClassicCameraController classicCamera;

  float Now => Time.realtimeSinceStartup - sceneStart;

  public bool IsWritten => sessionWritten;

  void Awake()
  {
    Instance = this;
    sceneStart = Time.realtimeSinceStartup;
    startedAt = DateTime.Now;
    batteryStart = SystemInfo.batteryLevel;
  }

  public void Init(GameMode gameMode, GameManager manager, GameEconomy eco, WaveManager waveManager,
      TowerPlacement towerPlacement, ClassicCameraController cameraController)
  {
    mode = gameMode;
    participantId = StudySession.ParticipantId;
    orderIndex = StudySession.CompletedCount(participantId) + 1;
    sessionId = $"P{participantId:D3}_{mode}_{startedAt:yyyyMMdd_HHmmss}";

    gm = manager;
    economy = eco;
    waves = waveManager;
    placement = towerPlacement;
    classicCamera = cameraController;

    gm.StateChanged += OnStateChanged;
    gm.BaseDamaged += OnBaseDamaged;
    gm.SystemPauseChanged += OnSystemPauseChanged;
    waves.WaveStarted += OnWaveStarted;
    placement.PlacementAttempted += OnPlacement;
    EnemyHealth.Killed += OnEnemyKilled;
    EnemyHealth.Leaked += OnEnemyLeaked;

    Log("session_start", $"mode={mode};order={orderIndex};device={SystemInfo.deviceModel}");
  }

  void OnDestroy()
  {
    EnemyHealth.Killed -= OnEnemyKilled;
    EnemyHealth.Leaked -= OnEnemyLeaked;

    if (gm != null)
    {
      gm.StateChanged -= OnStateChanged;
      gm.BaseDamaged -= OnBaseDamaged;
      gm.SystemPauseChanged -= OnSystemPauseChanged;
    }

    if (waves != null) waves.WaveStarted -= OnWaveStarted;
    if (placement != null) placement.PlacementAttempted -= OnPlacement;

    // Leaving the scene without finishing (e.g. via the menu) still keeps the data.
    if (!sessionWritten && gameStartTime >= 0f)
    {
      if (!gameplayFinalized)
        FinalizeGameplay("aborted");

      WriteSession(null);
    }

    if (Instance == this)
      Instance = null;
  }

  public void Log(string type, string detail = "")
  {
    events.Add(new LoggedEvent { time = Now, type = type, detail = detail });
  }

  // ---- AR hooks -------------------------------------------------------------

  public void SetPhysicalCamera(Transform cameraTransform, float unitsPerMeter, float boardMeters)
  {
    physicalCamera = cameraTransform;
    worldUnitsPerMeter = Mathf.Max(0.0001f, unitsPerMeter);
    boardWidthMeters = boardMeters;
    lastCameraPos = cameraTransform.position;
    lastCameraRot = cameraTransform.rotation;
  }

  public void MarkFirstPlane()
  {
    if (firstPlaneTime >= 0f) return;
    firstPlaneTime = Now;
    Log("ar_first_plane");
  }

  public void MarkBoardPlaced()
  {
    boardPlacements++;
    boardPlacedTime = Now;
    Log("ar_board_placed", "count=" + boardPlacements);
  }

  public void MarkTracking(bool tracking, string reason)
  {
    if (!tracking && !trackingLost)
    {
      trackingLost = true;
      if (gameStartTime >= 0f && !gameplayFinalized) trackingLosses++;
      Log("ar_tracking_lost", reason);
    }
    else if (tracking && trackingLost)
    {
      trackingLost = false;
      Log("ar_tracking_restored");
    }
  }

  // ---- Runtime --------------------------------------------------------------

  void Update()
  {
    if (gm == null || gameplayFinalized)
      return;

    float dt = Time.unscaledDeltaTime;

    if (PointerInput.TryGetTap(out Vector2 _, out bool overUI))
    {
      tapsTotal++;
      if (overUI) tapsUI++; else tapsWorld++;
    }

    if (gm.State == GameState.Playing)
    {
      if (gm.IsSystemPaused)
      {
        systemPauseTime += dt;
      }
      else
      {
        playTime += dt;
        frameTimes.Add(dt);
      }

      if (trackingLost)
        trackingLossTime += dt;
    }
    else if (gm.State == GameState.Paused)
    {
      pauseTime += dt;
    }

    if (physicalCamera != null)
    {
      if (gm.State == GameState.Playing)
      {
        devicePath += Vector3.Distance(physicalCamera.position, lastCameraPos) / worldUnitsPerMeter;
        deviceRotation += Quaternion.Angle(physicalCamera.rotation, lastCameraRot);
      }

      lastCameraPos = physicalCamera.position;
      lastCameraRot = physicalCamera.rotation;
    }
  }

  void OnApplicationPause(bool paused)
  {
    if (gm == null || gameplayFinalized)
      return;

    Log(paused ? "app_background" : "app_foreground");

    if (paused)
      gm.Pause();
  }

  void OnStateChanged(GameState state)
  {
    Log("state", state.ToString());

    switch (state)
    {
      case GameState.Playing:
        if (gameStartTime < 0f)
          gameStartTime = Now;
        break;

      case GameState.Paused:
        userPauses++;
        break;

      case GameState.Victory:
        FinalizeGameplay("victory");
        break;

      case GameState.Defeat:
        FinalizeGameplay("defeat");
        break;

      case GameState.Aborted:
        FinalizeGameplay("aborted");
        break;
    }
  }

  void OnSystemPauseChanged(bool paused)
  {
    Log(paused ? "system_pause" : "system_resume", gm.SystemPauseReason ?? "");
  }

  void OnBaseDamaged(int hp) => Log("base_damaged", "hp=" + hp);
  void OnWaveStarted(int wave) => Log("wave_start", "wave=" + wave);

  void OnEnemyKilled(EnemyHealth enemy)
  {
    kills++;
  }

  void OnEnemyLeaked(EnemyHealth enemy)
  {
    leaks++;
    Log("enemy_leaked");
  }

  void OnPlacement(PlacementResult result, Vector3 position)
  {
    string where = $"x={position.x.ToString(CultureInfo.InvariantCulture)};z={position.z.ToString(CultureInfo.InvariantCulture)}";
    Log("place_" + result.ToString().ToLowerInvariant(), where);

    switch (result)
    {
      case PlacementResult.Placed:
        towersPlaced++;
        placementTimes.Add(Now);
        if (firstTowerTime < 0f && gameStartTime >= 0f)
          firstTowerTime = Now - gameStartTime;
        break;
      case PlacementResult.NotEnoughCoins: failNoCoins++; break;
      case PlacementResult.InvalidCell: failInvalid++; break;
      case PlacementResult.Occupied: failOccupied++; break;
      case PlacementResult.MissedBoard: failMissed++; break;
    }
  }

  void FinalizeGameplay(string result)
  {
    if (gameplayFinalized)
      return;

    gameplayFinalized = true;
    outcome = result;
    finishTime = Now;
    batteryEnd = SystemInfo.batteryLevel;
    Log("session_end", outcome);

    WriteEvents();
    DataStore.WriteText("session_" + sessionId + ".csv", MetricsAsText(null));
  }

  // ---- Output ---------------------------------------------------------------

  // Called after the questionnaire (answers may be null when it is skipped).
  public void WriteSession(Dictionary<string, int> answers)
  {
    if (sessionWritten)
      return;

    if (!gameplayFinalized)
      FinalizeGameplay("aborted");

    sessionWritten = true;

    List<KeyValuePair<string, string>> metrics = BuildMetrics(answers);
    DataStore.AppendRow(DataStore.SessionsFile, metrics.Select(m => m.Key).ToList(), metrics.Select(m => m.Value).ToList());
    DataStore.WriteText("session_" + sessionId + ".csv", MetricsAsText(answers));

    if (answers != null && answers.Count > 0)
    {
      var header = new[] { "participant_id", "session_id", "mode", "order_index", "instrument", "item_id", "value" };

      foreach (QuestionItem item in Questionnaires.PostSession())
      {
        if (!answers.TryGetValue(item.Id, out int value))
          continue;

        DataStore.AppendRow(DataStore.AnswersFile, header, new[]
        {
          participantId.ToString(), sessionId, mode.ToString(), orderIndex.ToString(),
          item.Instrument, item.Id, value.ToString()
        });
      }
    }

    // A session counts for counterbalancing once it was played and either finished or evaluated.
    if (gameStartTime >= 0f && (outcome != "aborted" || (answers != null && answers.Count > 0)))
      StudySession.MarkCompleted(participantId, mode);
  }

  void WriteEvents()
  {
    var header = new[] { "participant_id", "session_id", "mode", "time_s", "event", "detail" };

    foreach (LoggedEvent e in events)
    {
      DataStore.AppendRow(DataStore.EventsFile, header, new[]
      {
        participantId.ToString(), sessionId, mode.ToString(), DataStore.F(e.time), e.type, e.detail
      });
    }
  }

  string MetricsAsText(Dictionary<string, int> answers)
  {
    StringBuilder sb = new StringBuilder();
    sb.AppendLine("metric,value");

    foreach (var pair in BuildMetrics(answers))
      sb.AppendLine(pair.Key + "," + pair.Value);

    return sb.ToString();
  }

  public string Summary()
  {
    return
        $"Αποτέλεσμα: {OutcomeLabel(outcome)}\n" +
        $"Κύματα: {(waves != null ? waves.CurrentWave : 0)}/{(waves != null ? waves.TotalWaves : 0)}   " +
        $"Ζωή βάσης: {(gm != null ? gm.playerHealth : 0)}/{(gm != null ? gm.MaxHealth : 0)}\n" +
        $"Εχθροί που νικήθηκαν: {kills}   Πύργοι: {towersPlaced}\n" +
        $"Χρόνος παιχνιδιού: {FormatTime(playTime)}";
  }

  public static string OutcomeLabel(string result)
  {
    switch (result)
    {
      case "victory": return "Νίκη";
      case "defeat": return "Ήττα";
      case "aborted": return "Διακοπή";
      default: return "-";
    }
  }

  static string FormatTime(float seconds)
  {
    int total = Mathf.RoundToInt(seconds);
    return $"{total / 60}:{total % 60:D2}";
  }

  List<KeyValuePair<string, string>> BuildMetrics(Dictionary<string, int> answers)
  {
    var m = new List<KeyValuePair<string, string>>();
    void Add(string key, string value) => m.Add(new KeyValuePair<string, string>(key, value ?? ""));
    string Opt(float value) => value >= 0f ? DataStore.F(value) : "";

    float end = finishTime >= 0f ? finishTime : Now;
    int attempts = towersPlaced + failNoCoins + failInvalid + failOccupied + failMissed;
    float minutes = playTime / 60f;

    Add("participant_id", participantId.ToString());
    Add("session_id", sessionId);
    Add("mode", mode.ToString());
    Add("order_index", orderIndex.ToString());
    Add("started_at", DataStore.Timestamp(startedAt));
    Add("device_model", SystemInfo.deviceModel);
    Add("os", SystemInfo.operatingSystem);
    Add("app_version", Application.version);
    Add("outcome", outcome);

    Add("setup_time_s", Opt(gameStartTime));
    Add("play_time_s", DataStore.F(playTime));
    Add("pause_time_s", DataStore.F(pauseTime));
    Add("system_pause_time_s", DataStore.F(systemPauseTime));
    Add("total_time_s", DataStore.F(end));

    Add("waves_reached", waves != null ? waves.CurrentWave.ToString() : "");
    Add("total_waves", waves != null ? waves.TotalWaves.ToString() : "");
    Add("base_hp_left", gm != null ? gm.playerHealth.ToString() : "");
    Add("base_hp_max", gm != null ? gm.MaxHealth.ToString() : "");
    Add("enemies_killed", kills.ToString());
    Add("enemies_leaked", leaks.ToString());
    Add("coins_earned", economy != null ? economy.TotalEarned.ToString() : "");
    Add("coins_spent", economy != null ? economy.TotalSpent.ToString() : "");

    Add("towers_placed", towersPlaced.ToString());
    Add("placement_attempts", attempts.ToString());
    Add("placement_fail_no_coins", failNoCoins.ToString());
    Add("placement_fail_invalid", failInvalid.ToString());
    Add("placement_fail_occupied", failOccupied.ToString());
    Add("placement_miss_board", failMissed.ToString());
    // Spatial errors only (invalid / occupied / outside the board); lack of coins is a strategy issue.
    Add("placement_error_rate", attempts > 0 ? DataStore.F((float)(failInvalid + failOccupied + failMissed) / attempts) : "");
    Add("time_to_first_tower_s", Opt(firstTowerTime));
    Add("mean_placement_interval_s", placementTimes.Count > 1
        ? DataStore.F((placementTimes[placementTimes.Count - 1] - placementTimes[0]) / (placementTimes.Count - 1))
        : "");

    Add("taps_total", tapsTotal.ToString());
    Add("taps_ui", tapsUI.ToString());
    Add("taps_world", tapsWorld.ToString());
    Add("taps_per_min", minutes > 0.01f ? DataStore.F(tapsTotal / minutes) : "");
    Add("user_pauses", userPauses.ToString());
    Add("camera_pan_gestures", classicCamera != null ? classicCamera.PanGestures.ToString() : "");
    Add("camera_zoom_gestures", classicCamera != null ? classicCamera.ZoomGestures.ToString() : "");

    bool ar = mode == GameMode.AR;
    Add("ar_time_to_first_plane_s", ar ? Opt(firstPlaneTime) : "");
    Add("ar_board_placements", ar ? boardPlacements.ToString() : "");
    Add("ar_board_width_m", ar ? DataStore.F(boardWidthMeters) : "");
    Add("ar_tracking_losses", ar ? trackingLosses.ToString() : "");
    Add("ar_tracking_loss_time_s", ar ? DataStore.F(trackingLossTime) : "");
    Add("ar_device_path_m", ar ? DataStore.F(devicePath) : "");
    Add("ar_device_rotation_deg", ar ? DataStore.F(deviceRotation) : "");

    AddFrameStats(Add);

    Add("battery_start", batteryStart >= 0f ? DataStore.F(batteryStart) : "");
    Add("battery_end", batteryEnd >= 0f ? DataStore.F(batteryEnd) : "");

    Dictionary<string, int> a = answers ?? new Dictionary<string, int>();
    Add("questionnaire_completed", answers != null && answers.Count > 0 ? "1" : "0");
    Add("sus_score", DataStore.F(Questionnaires.ScoreSUS(a)));

    foreach (var component in Questionnaires.ScoreIGEQ(a))
      Add("igeq_" + component.Key, DataStore.F(component.Value));

    foreach (QuestionItem item in Questionnaires.Custom)
      Add(item.Id.ToLowerInvariant(), a.TryGetValue(item.Id, out int v) ? v.ToString() : "");

    return m;
  }

  void AddFrameStats(Action<string, string> add)
  {
    if (frameTimes.Count < 10)
    {
      add("fps_avg", "");
      add("fps_1pct_low", "");
      add("frame_time_ms_avg", "");
      return;
    }

    float total = 0f;
    foreach (float t in frameTimes) total += t;

    float average = total / frameTimes.Count;
    List<float> sorted = frameTimes.OrderByDescending(t => t).ToList();
    int worst = Mathf.Max(1, sorted.Count / 100);
    float worstAverage = sorted.Take(worst).Average();

    add("fps_avg", DataStore.F(1f / average));
    add("fps_1pct_low", DataStore.F(1f / worstAverage));
    add("frame_time_ms_avg", DataStore.F(average * 1000f));
  }
}
