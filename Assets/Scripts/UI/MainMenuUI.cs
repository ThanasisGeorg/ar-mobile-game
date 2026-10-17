using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Main menu / study control panel: participant selection, counterbalanced
// order of the two versions, final comparison questionnaire and data location.
public class MainMenuUI : MonoBehaviour
{
  private RectTransform safeArea;
  private TextMeshProUGUI participantText;
  private TextMeshProUGUI statusText;
  private TextMeshProUGUI dataText;
  private Button classicButton;
  private Button arButton;
  private Button finalButton;
  private Button questionnaireToggle;
  private bool confirmReset;
  private Button resetButton;

  public static MainMenuUI Create()
  {
    Canvas canvas = UIFactory.CreateCanvas("Main Menu", 0);
    MainMenuUI menu = canvas.gameObject.AddComponent<MainMenuUI>();
    menu.Build(canvas.transform);
    return menu;
  }

  void Build(Transform canvas)
  {
    Image background = UIFactory.Panel(canvas, "Background", new Color(0.06f, 0.08f, 0.12f, 1f), false);
    UIFactory.Stretch(background.rectTransform);
    background.transform.SetAsFirstSibling();

    safeArea = UIFactory.SafeArea(canvas);

    // Left column: title and participant
    RectTransform left = UIFactory.Rect("Left", safeArea);
    left.anchorMin = new Vector2(0.04f, 0.06f);
    left.anchorMax = new Vector2(0.5f, 0.94f);
    left.offsetMin = left.offsetMax = Vector2.zero;
    UIFactory.VerticalLayout(left.gameObject, 18f, null, TextAnchor.UpperLeft);

    TextMeshProUGUI title = UIFactory.Text(left, "Tower Defense\nΚλασικό vs AR", 76f, UIFactory.TextColor, TextAlignmentOptions.Left, FontStyles.Bold);
    UIFactory.Size(title, 200f);

    TextMeshProUGUI subtitle = UIFactory.Text(left,
        "Μελέτη εμπειρίας χρήστη (UX) στο mobile gaming:\nσύγκριση Επαυξημένης Πραγματικότητας και κλασικού παιχνιδιού.",
        30f, UIFactory.MutedTextColor, TextAlignmentOptions.Left);
    UIFactory.Size(subtitle, 100f);

    TextMeshProUGUI participantLabel = UIFactory.Text(left, "Συμμετέχων", 32f, UIFactory.MutedTextColor, TextAlignmentOptions.Left);
    UIFactory.Size(participantLabel, 50f);

    RectTransform row = UIFactory.Rect("Participant", left);
    UIFactory.HorizontalLayout(row.gameObject, 16f);
    UIFactory.Size(row, 110f);

    UIFactory.Size(UIFactory.Button(row, "-", UIFactory.NeutralColor, () => ChangeParticipant(-1), 60f), 110f, 120f, 0f);
    participantText = UIFactory.Text(row, "", 64f, UIFactory.AccentColor, TextAlignmentOptions.Center, FontStyles.Bold);
    UIFactory.Size(participantText, 110f, 260f, 0f);
    UIFactory.Size(UIFactory.Button(row, "+", UIFactory.NeutralColor, () => ChangeParticipant(1), 60f), 110f, 120f, 0f);

    statusText = UIFactory.Text(left, "", 30f, UIFactory.TextColor, TextAlignmentOptions.TopLeft);
    UIFactory.Size(statusText, 170f);

    dataText = UIFactory.Text(left, "", 22f, UIFactory.MutedTextColor, TextAlignmentOptions.BottomLeft);
    UIFactory.Size(dataText, 110f);

    // Right column: actions
    RectTransform right = UIFactory.Rect("Right", safeArea);
    right.anchorMin = new Vector2(0.55f, 0.08f);
    right.anchorMax = new Vector2(0.96f, 0.92f);
    right.offsetMin = right.offsetMax = Vector2.zero;
    UIFactory.VerticalLayout(right.gameObject, 22f, null, TextAnchor.MiddleCenter);

    classicButton = UIFactory.Button(right, "Κλασική έκδοση", UIFactory.PrimaryColor, () => StudySession.Play(GameMode.Classic), 46f);
    UIFactory.Size(classicButton, 130f);

    arButton = UIFactory.Button(right, "Έκδοση AR", UIFactory.PrimaryColor, () => StudySession.Play(GameMode.AR), 46f);
    UIFactory.Size(arButton, 130f);

    finalButton = UIFactory.Button(right, "Τελικό ερωτηματολόγιο", UIFactory.NeutralColor, OpenFinalQuestionnaire, 40f);
    UIFactory.Size(finalButton, 110f);

    questionnaireToggle = UIFactory.Button(right, "", UIFactory.NeutralColor, ToggleQuestionnaires, 32f);
    UIFactory.Size(questionnaireToggle, 90f);

    resetButton = UIFactory.Button(right, "Επαναφορά συμμετέχοντα", UIFactory.NeutralColor, ResetParticipant, 32f);
    UIFactory.Size(resetButton, 90f);

    UIFactory.Size(UIFactory.Button(right, "Έξοδος", UIFactory.DangerColor, Application.Quit, 32f), 90f);

    Refresh();
  }

  void Update()
  {
    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && GetComponentInChildren<QuestionnaireUI>() == null)
      Application.Quit();
  }

  void ChangeParticipant(int delta)
  {
    StudySession.ParticipantId = StudySession.ParticipantId + delta;
    confirmReset = false;
    Refresh();
  }

  void ToggleQuestionnaires()
  {
    StudySession.QuestionnairesEnabled = !StudySession.QuestionnairesEnabled;
    Refresh();
  }

  void ResetParticipant()
  {
    if (!confirmReset)
    {
      confirmReset = true;
      Refresh();
      return;
    }

    confirmReset = false;
    StudySession.ResetParticipant(StudySession.ParticipantId);
    Refresh();
  }

  void Refresh()
  {
    int p = StudySession.ParticipantId;
    participantText.text = "P" + p.ToString("D3");

    GameMode first = StudySession.FirstModeFor(p);
    GameMode? next = StudySession.NextModeFor(p);
    bool classicDone = StudySession.HasCompleted(p, GameMode.Classic);
    bool arDone = StudySession.HasCompleted(p, GameMode.AR);
    bool finalDone = StudySession.FinalQuestionnaireDone(p);

    string order = first == GameMode.Classic ? "πρώτα Κλασική, μετά AR" : "πρώτα AR, μετά Κλασική";
    statusText.text =
        "Σειρά (αντιστάθμιση): <b>" + order + "</b>\n" +
        "Κλασική: " + (classicDone ? "<color=#5BD17A>ολοκληρώθηκε</color>" : "εκκρεμεί") + "\n" +
        "AR: " + (arDone ? "<color=#5BD17A>ολοκληρώθηκε</color>" : "εκκρεμεί") + "\n" +
        "Τελικό ερωτηματολόγιο: " + (finalDone ? "<color=#5BD17A>ολοκληρώθηκε</color>" : "εκκρεμεί");

    UIFactory.SetButtonLabel(classicButton, next == GameMode.Classic ? "Κλασική έκδοση  (επόμενη)" : "Κλασική έκδοση");
    UIFactory.SetButtonLabel(arButton, next == GameMode.AR ? "Έκδοση AR  (επόμενη)" : "Έκδοση AR");
    UIFactory.SetButtonColor(classicButton, next == GameMode.Classic ? UIFactory.SuccessColor : UIFactory.PrimaryColor);
    UIFactory.SetButtonColor(arButton, next == GameMode.AR ? UIFactory.SuccessColor : UIFactory.PrimaryColor);
    UIFactory.SetButtonColor(finalButton, classicDone && arDone && !finalDone ? UIFactory.SuccessColor : UIFactory.NeutralColor);

    UIFactory.SetButtonLabel(questionnaireToggle, "Ερωτηματολόγια μετά το παιχνίδι: " + (StudySession.QuestionnairesEnabled ? "ΝΑΙ" : "ΟΧΙ"));
    UIFactory.SetButtonLabel(resetButton, confirmReset ? "Πατήστε ξανά για επιβεβαίωση" : "Επαναφορά συμμετέχοντα");

    dataText.text = "Δεδομένα: " + DataStore.Folder + "\nΣυνεδρίες: " + DataStore.CountRows(DataStore.SessionsFile) +
        "   Τελικά ερωτηματολόγια: " + DataStore.CountRows(DataStore.FinalFile);
  }

  void OpenFinalQuestionnaire()
  {
    int participant = StudySession.ParticipantId;
    List<QuestionItem> items = Questionnaires.Final.ToList();

    QuestionnaireUI.Show(transform, "Συγκρίνετε τις δύο εκδόσεις που παίξατε.", items, answers =>
    {
      var header = new List<string> { "participant_id", "completed_at", "first_mode", "classic_done", "ar_done" };
      var values = new List<string>
      {
        participant.ToString(),
        DataStore.Timestamp(System.DateTime.Now),
        StudySession.FirstModeFor(participant).ToString(),
        StudySession.HasCompleted(participant, GameMode.Classic) ? "1" : "0",
        StudySession.HasCompleted(participant, GameMode.AR) ? "1" : "0",
      };

      foreach (QuestionItem item in items)
      {
        header.Add(item.Id.ToLowerInvariant());
        values.Add(answers.TryGetValue(item.Id, out int v) ? v.ToString() : "");
      }

      DataStore.AppendRow(DataStore.FinalFile, header, values);
      StudySession.MarkFinalQuestionnaireDone(participant);
      Refresh();
    });
  }
}
