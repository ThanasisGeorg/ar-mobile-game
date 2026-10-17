using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shows a list of questionnaire items one at a time (large touch targets,
// progress indicator, back button) and returns the answers as item id -> value.
public class QuestionnaireUI : MonoBehaviour
{
  private List<QuestionItem> items;
  private readonly Dictionary<string, int> answers = new Dictionary<string, int>();
  private Action<Dictionary<string, int>> onComplete;
  private int index;

  private TextMeshProUGUI progressText;
  private TextMeshProUGUI instructionText;
  private TextMeshProUGUI questionText;
  private Image progressFill;
  private RectTransform optionsRow;
  private Button backButton;
  private readonly List<Button> optionButtons = new List<Button>();

  public static QuestionnaireUI Show(Transform parent, string instruction, List<QuestionItem> questions,
      Action<Dictionary<string, int>> completed)
  {
    Image background = UIFactory.Panel(parent, "Questionnaire", new Color(0.05f, 0.06f, 0.09f, 0.98f));
    UIFactory.Stretch(background.rectTransform);

    QuestionnaireUI ui = background.gameObject.AddComponent<QuestionnaireUI>();
    ui.items = questions;
    ui.onComplete = completed;
    ui.Build(instruction);
    ui.ShowItem(0);
    return ui;
  }

  void Build(string instruction)
  {
    Transform root = transform;

    progressText = UIFactory.Text(root, "", 34f, UIFactory.MutedTextColor);
    UIFactory.Place(progressText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1500f, 50f));

    Image track = UIFactory.Panel(root, "Progress", new Color(1f, 1f, 1f, 0.12f), false);
    UIFactory.Place(track.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1500f, 14f));

    progressFill = UIFactory.Panel(track.transform, "Fill", UIFactory.AccentColor, false);
    progressFill.rectTransform.anchorMin = Vector2.zero;
    progressFill.rectTransform.anchorMax = new Vector2(0f, 1f);
    progressFill.rectTransform.offsetMin = Vector2.zero;
    progressFill.rectTransform.offsetMax = Vector2.zero;

    instructionText = UIFactory.Text(root, instruction, 32f, UIFactory.MutedTextColor);
    UIFactory.Place(instructionText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1600f, 110f));

    questionText = UIFactory.Text(root, "", 54f, UIFactory.TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
    UIFactory.Place(questionText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1600f, 260f));
    questionText.enableAutoSizing = true;
    questionText.fontSizeMin = 32f;
    questionText.fontSizeMax = 54f;

    optionsRow = UIFactory.Rect("Options", root);
    UIFactory.Place(optionsRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(1600f, 190f));
    UIFactory.HorizontalLayout(optionsRow.gameObject, 24f);

    backButton = UIFactory.Button(root, "Πίσω", UIFactory.NeutralColor, Back, 36f);
    UIFactory.Place((RectTransform)backButton.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 50f), new Vector2(260f, 100f));
  }

  void ShowItem(int i)
  {
    index = i;
    QuestionItem item = items[i];

    progressText.text = $"Ερώτηση {i + 1} από {items.Count}";
    progressFill.rectTransform.anchorMax = new Vector2((float)i / items.Count, 1f);
    questionText.text = item.Text;
    backButton.interactable = i > 0;

    foreach (Button b in optionButtons)
      Destroy(b.gameObject);

    optionButtons.Clear();
    answers.TryGetValue(item.Id, out int previous);
    bool hasPrevious = answers.ContainsKey(item.Id);

    for (int o = 0; o < item.Options.Length; o++)
    {
      int value = item.Values[o];
      Color color = hasPrevious && previous == value ? UIFactory.AccentColor : UIFactory.PrimaryColor;
      Button button = UIFactory.Button(optionsRow, item.Options[o], color, () => Answer(value), 38f);
      optionButtons.Add(button);
    }
  }

  void Answer(int value)
  {
    answers[items[index].Id] = value;

    if (index + 1 < items.Count)
    {
      ShowItem(index + 1);
      return;
    }

    Action<Dictionary<string, int>> callback = onComplete;
    onComplete = null;
    Destroy(gameObject);
    callback?.Invoke(new Dictionary<string, int>(answers));
  }

  void Back()
  {
    if (index > 0)
      ShowItem(index - 1);
  }
}
