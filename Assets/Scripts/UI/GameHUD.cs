using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// In-game interface shared by both versions: status bar, instructions, start/pause,
// end-of-game summary and the post-session questionnaire.
public class GameHUD : MonoBehaviour
{
  private GameManager gm;
  private GameEconomy economy;
  private WaveManager waves;
  private TowerPlacement placement;
  private SessionTelemetry telemetry;
  private GameMode mode;

  private RectTransform safeArea;
  private TextMeshProUGUI coinsText;
  private TextMeshProUGUI waveText;
  private TextMeshProUGUI hpText;
  private TextMeshProUGUI countdownText;
  private TextMeshProUGUI toastText;
  private TextMeshProUGUI instructionText;
  private TextMeshProUGUI debugText;
  private Image instructionPanel;
  private Button startButton;
  private Button pauseButton;
  private GameObject pausePanel;
  private GameObject systemPausePanel;
  private TextMeshProUGUI systemPauseText;
  private GameObject endPanel;
  private TextMeshProUGUI endTitle;
  private TextMeshProUGUI endSummary;

  private bool startAvailable = true;
  private float toastUntil;
  private float hpFlashUntil;

  public static GameHUD Create(GameMode mode, GameManager gm, GameEconomy economy, WaveManager waves,
      TowerPlacement placement, SessionTelemetry telemetry)
  {
    Canvas canvas = UIFactory.CreateCanvas("Game HUD", 10);
    GameHUD hud = canvas.gameObject.AddComponent<GameHUD>();
    hud.mode = mode;
    hud.gm = gm;
    hud.economy = economy;
    hud.waves = waves;
    hud.placement = placement;
    hud.telemetry = telemetry;
    hud.Build(canvas.transform);
    return hud;
  }

  void Build(Transform canvas)
  {
    safeArea = UIFactory.SafeArea(canvas);

    // Status bar
    Image bar = UIFactory.Panel(safeArea, "Top Bar", UIFactory.BarColor, false);
    bar.rectTransform.anchorMin = new Vector2(0f, 1f);
    bar.rectTransform.anchorMax = new Vector2(1f, 1f);
    bar.rectTransform.pivot = new Vector2(0.5f, 1f);
    bar.rectTransform.sizeDelta = new Vector2(0f, 100f);
    bar.rectTransform.anchoredPosition = Vector2.zero;

    coinsText = StatusText(bar.transform, new Vector2(0.03f, 0f), new Vector2(0.28f, 1f), UIFactory.AccentColor);
    waveText = StatusText(bar.transform, new Vector2(0.3f, 0f), new Vector2(0.55f, 1f), UIFactory.TextColor);
    hpText = StatusText(bar.transform, new Vector2(0.57f, 0f), new Vector2(0.82f, 1f), UIFactory.TextColor);

    pauseButton = UIFactory.Button(bar.transform, "Παύση", UIFactory.NeutralColor, OnPauseButton, 34f);
    RectTransform pauseRect = (RectTransform)pauseButton.transform;
    pauseRect.anchorMin = new Vector2(0.86f, 0.12f);
    pauseRect.anchorMax = new Vector2(0.99f, 0.88f);
    pauseRect.offsetMin = pauseRect.offsetMax = Vector2.zero;

    countdownText = UIFactory.Text(safeArea, "", 40f, UIFactory.TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
    UIFactory.Place(countdownText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(900f, 60f));

    toastText = UIFactory.Text(safeArea, "", 44f, UIFactory.AccentColor, TextAlignmentOptions.Center, FontStyles.Bold);
    UIFactory.Place(toastText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(1400f, 70f));
    toastText.outlineWidth = 0.2f;
    toastText.outlineColor = Color.black;

    // Instructions and start button
    instructionPanel = UIFactory.Panel(safeArea, "Instructions", UIFactory.BarColor, false);
    UIFactory.Place(instructionPanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1500f, 150f));
    instructionText = UIFactory.Text(instructionPanel.transform, "", 36f, UIFactory.TextColor);
    UIFactory.Stretch(instructionText.rectTransform, 30f, 30f, 10f, 10f);
    instructionText.enableAutoSizing = true;
    instructionText.fontSizeMin = 24f;
    instructionText.fontSizeMax = 36f;

    startButton = UIFactory.Button(safeArea, "Έναρξη", UIFactory.SuccessColor, OnStartPressed, 52f);
    UIFactory.Place((RectTransform)startButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 200f), new Vector2(420f, 120f));

    BuildPausePanel();
    BuildSystemPausePanel();
    BuildEndPanel();

    if (mode == GameMode.Classic)
    {
      SetInstruction("Πατήστε σε ένα ελεύθερο σημείο του χορταριού για να χτίσετε πύργο (" + placement.towerCost +
          " νομίσματα). Σύρετε για μετακίνηση της κάμερας, τσιμπήστε για ζουμ. Πατήστε «Έναρξη» όταν είστε έτοιμοι.");
    }

    gm.StateChanged += OnStateChanged;
    gm.SystemPauseChanged += OnSystemPauseChanged;
    gm.BaseDamaged += OnBaseDamaged;
    waves.WaveStarted += OnWaveStarted;
    placement.PlacementAttempted += OnPlacement;

    RefreshPanels();
  }

  void OnDestroy()
  {
    if (gm != null)
    {
      gm.StateChanged -= OnStateChanged;
      gm.SystemPauseChanged -= OnSystemPauseChanged;
      gm.BaseDamaged -= OnBaseDamaged;
    }

    if (waves != null) waves.WaveStarted -= OnWaveStarted;
    if (placement != null) placement.PlacementAttempted -= OnPlacement;
  }

  TextMeshProUGUI StatusText(Transform parent, Vector2 min, Vector2 max, Color color)
  {
    TextMeshProUGUI text = UIFactory.Text(parent, "", 40f, color, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
    text.rectTransform.anchorMin = min;
    text.rectTransform.anchorMax = max;
    text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
    return text;
  }

  GameObject Overlay(string name, out RectTransform box, Vector2 boxSize)
  {
    Image dim = UIFactory.Panel(safeArea.parent, name, UIFactory.DimColor);
    UIFactory.Stretch(dim.rectTransform);

    Image panel = UIFactory.Panel(dim.transform, "Box", UIFactory.PanelColor);
    UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, boxSize);
    UIFactory.VerticalLayout(panel.gameObject, 24f, new RectOffset(50, 50, 40, 40), TextAnchor.MiddleCenter);

    box = panel.rectTransform;
    dim.gameObject.SetActive(false);
    return dim.gameObject;
  }

  void BuildPausePanel()
  {
    pausePanel = Overlay("Pause", out RectTransform box, new Vector2(800f, 520f));

    TextMeshProUGUI title = UIFactory.Text(box, "Παύση", 64f, UIFactory.TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
    UIFactory.Size(title, 100f);

    UIFactory.Size(UIFactory.Button(box, "Συνέχεια", UIFactory.SuccessColor, () => gm.Resume()), 110f);
    UIFactory.Size(UIFactory.Button(box, "Τερματισμός συνεδρίας", UIFactory.DangerColor, () => gm.AbortSession()), 110f);
  }

  void BuildSystemPausePanel()
  {
    systemPausePanel = Overlay("System Pause", out RectTransform box, new Vector2(1100f, 320f));
    systemPauseText = UIFactory.Text(box, "", 44f, UIFactory.TextColor);
    UIFactory.Size(systemPauseText, 220f);

    // A system pause must not block the camera view entirely in AR.
    systemPausePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
  }

  void BuildEndPanel()
  {
    endPanel = Overlay("End", out RectTransform box, new Vector2(1100f, 760f));

    endTitle = UIFactory.Text(box, "", 80f, UIFactory.TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
    UIFactory.Size(endTitle, 120f);

    endSummary = UIFactory.Text(box, "", 38f, UIFactory.MutedTextColor);
    UIFactory.Size(endSummary, 300f);

    string label = StudySession.QuestionnairesEnabled ? "Συνέχεια στο ερωτηματολόγιο" : "Επιστροφή στο μενού";
    UIFactory.Size(UIFactory.Button(box, label, UIFactory.PrimaryColor, OnEndContinue, 44f), 120f);
  }

  // ---- Public API used by the game setup (e.g. AR placement flow) -----------

  public void SetInstruction(string text)
  {
    instructionText.text = text;
    RefreshPanels();
  }

  // Small diagnostic line (used by the AR version before the game starts).
  public void SetDebugText(string text)
  {
    if (debugText == null)
    {
      debugText = UIFactory.Text(safeArea, "", 22f, new Color(1f, 1f, 0.6f), TextAlignmentOptions.TopLeft);
      UIFactory.Place(debugText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -230f), new Vector2(1800f, 80f));
      debugText.outlineWidth = 0.2f;
      debugText.outlineColor = Color.black;
    }

    debugText.text = text;
  }

  public void SetStartAvailable(bool available)
  {
    startAvailable = available;
    RefreshPanels();
  }

  public void ShowToast(string message, float seconds = 2f)
  {
    toastText.text = message;
    toastUntil = Time.unscaledTime + seconds;
  }

  // ---- Updates --------------------------------------------------------------

  void Update()
  {
    coinsText.text = "Νομίσματα: " + economy.coins;
    waveText.text = "Κύμα: " + waves.CurrentWave + "/" + waves.TotalWaves;
    hpText.text = "Βάση: " + gm.playerHealth + "/" + gm.MaxHealth;
    hpText.color = Time.unscaledTime < hpFlashUntil ? UIFactory.DangerColor : UIFactory.TextColor;

    if (gm.State == GameState.Playing && waves.NextWaveIn > 0f)
      countdownText.text = "Κύμα " + waves.UpcomingWave + " σε " + Mathf.CeilToInt(waves.NextWaveIn) + " δευτ.";
    else
      countdownText.text = "";

    float toastAlpha = Mathf.Clamp01((toastUntil - Time.unscaledTime) * 2f);
    toastText.alpha = toastAlpha;

    // Android back button / Escape toggles pause.
    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
    {
      if (gm.State == GameState.Playing) gm.Pause();
      else if (gm.State == GameState.Paused) gm.Resume();
    }
  }

  void RefreshPanels()
  {
    GameState state = gm.State;

    startButton.gameObject.SetActive(state == GameState.WaitingToStart && startAvailable);
    instructionPanel.gameObject.SetActive(state == GameState.WaitingToStart && !string.IsNullOrEmpty(instructionText.text));
    // Before the game starts the same button leads back to the menu.
    pauseButton.interactable = state == GameState.Playing || state == GameState.WaitingToStart;
    UIFactory.SetButtonLabel(pauseButton, state == GameState.WaitingToStart ? "Μενού" : "Παύση");
    pausePanel.SetActive(state == GameState.Paused);
    systemPausePanel.SetActive(state == GameState.Playing && gm.IsSystemPaused);
    endPanel.SetActive(gm.IsFinished && !questionnaireOpen && !thanksOpen);
  }

  void OnPauseButton()
  {
    if (gm.State == GameState.WaitingToStart)
      StudySession.ReturnToMenu();
    else
      gm.Pause();
  }

  void OnStartPressed()
  {
    gm.StartGame();
  }

  void OnStateChanged(GameState state)
  {
    if (gm.IsFinished)
    {
      endTitle.text = state == GameState.Victory ? "Νίκη!" : state == GameState.Defeat ? "Η βάση έπεσε" : "Τέλος συνεδρίας";
      endTitle.color = state == GameState.Victory ? UIFactory.SuccessColor : state == GameState.Defeat ? UIFactory.DangerColor : UIFactory.TextColor;
      endSummary.text = telemetry != null ? telemetry.Summary() : "";
    }

    RefreshPanels();
  }

  void OnSystemPauseChanged(bool paused)
  {
    systemPauseText.text = gm.SystemPauseReason ?? "";
    RefreshPanels();
  }

  void OnBaseDamaged(int hp)
  {
    hpFlashUntil = Time.unscaledTime + 0.6f;
  }

  void OnWaveStarted(int wave)
  {
    ShowToast(wave == waves.TotalWaves ? "Τελευταίο κύμα!" : "Κύμα " + wave, 1.5f);
  }

  void OnPlacement(PlacementResult result, Vector3 position)
  {
    switch (result)
    {
      case PlacementResult.NotEnoughCoins:
        ShowToast("Δεν έχετε αρκετά νομίσματα (" + placement.towerCost + ")");
        break;
      case PlacementResult.InvalidCell:
        ShowToast("Δεν μπορείτε να χτίσετε εδώ");
        break;
      case PlacementResult.Occupied:
        ShowToast("Υπάρχει ήδη πύργος εδώ");
        break;
    }
  }

  // ---- End of session -------------------------------------------------------

  private bool questionnaireOpen;
  private bool thanksOpen;

  void OnEndContinue()
  {
    if (!StudySession.QuestionnairesEnabled)
    {
      if (telemetry != null) telemetry.WriteSession(null);
      StudySession.ReturnToMenu();
      return;
    }

    questionnaireOpen = true;
    RefreshPanels();

    string version = mode == GameMode.AR ? "Επαυξημένης Πραγματικότητας (AR)" : "κλασική";
    string instruction = "Απαντήστε με βάση την εμπειρία σας στην " + version + " έκδοση που μόλις παίξατε.";

    QuestionnaireUI.Show(safeArea.parent, instruction, Questionnaires.PostSession(), answers =>
    {
      questionnaireOpen = false;
      if (telemetry != null) telemetry.WriteSession(answers);
      ShowThanks();
    });
  }

  void ShowThanks()
  {
    thanksOpen = true;
    RefreshPanels();

    GameObject panel = Overlay("Thanks", out RectTransform box, new Vector2(1100f, 520f));
    panel.SetActive(true);

    TextMeshProUGUI title = UIFactory.Text(box, "Ευχαριστούμε!", 72f, UIFactory.SuccessColor, TextAlignmentOptions.Center, FontStyles.Bold);
    UIFactory.Size(title, 110f);

    int participant = StudySession.ParticipantId;
    GameMode? next = StudySession.NextModeFor(participant);
    string message = next.HasValue
        ? "Οι απαντήσεις αποθηκεύτηκαν. Επόμενη: " + (next.Value == GameMode.AR ? "έκδοση AR" : "κλασική έκδοση") + "."
        : "Ολοκληρώσατε και τις δύο εκδόσεις. Συμπληρώστε το τελικό ερωτηματολόγιο από το μενού.";

    TextMeshProUGUI text = UIFactory.Text(box, message, 38f, UIFactory.MutedTextColor);
    UIFactory.Size(text, 140f);

    UIFactory.Size(UIFactory.Button(box, "Επιστροφή στο μενού", UIFactory.PrimaryColor, StudySession.ReturnToMenu, 44f), 120f);
  }
}
