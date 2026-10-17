using System.Collections.Generic;
using System.Linq;

public class QuestionItem
{
  public string Instrument;   // SUS, iGEQ, CUSTOM, FINAL, DEMO
  public string Id;           // e.g. SUS01, iGEQ05
  public string Text;         // Greek text shown to the participant
  public string TextEn;       // Original English wording (for the thesis / reporting)
  public string[] Options;    // Answer labels
  public int[] Values;        // Stored numeric values (same length as Options)
}

// Instruments used in the study. Greek wording is an adaptation of the original
// English items, which are kept in TextEn for reference.
public static class Questionnaires
{
  static readonly string[] AgreementLabels =
  {
    "Διαφωνώ\nαπόλυτα", "Διαφωνώ", "Ουδέτερο", "Συμφωνώ", "Συμφωνώ\nαπόλυτα"
  };

  static readonly int[] AgreementValues = { 1, 2, 3, 4, 5 };

  static readonly string[] IntensityLabels =
  {
    "Καθόλου", "Λίγο", "Μέτρια", "Αρκετά", "Πάρα\nπολύ"
  };

  static readonly int[] IntensityValues = { 0, 1, 2, 3, 4 };

  // System Usability Scale (Brooke, 1996). 5-point agreement scale, 1..5.
  public static readonly QuestionItem[] SUS =
  {
    Sus("SUS01", "Νομίζω ότι θα ήθελα να παίζω αυτό το παιχνίδι συχνά.", "I think that I would like to use this system frequently."),
    Sus("SUS02", "Βρήκα το παιχνίδι άσκοπα πολύπλοκο.", "I found the system unnecessarily complex."),
    Sus("SUS03", "Θεώρησα ότι το παιχνίδι ήταν εύκολο στη χρήση.", "I thought the system was easy to use."),
    Sus("SUS04", "Νομίζω ότι θα χρειαζόμουν βοήθεια από κάποιον έμπειρο για να μπορέσω να το παίξω.", "I think that I would need the support of a technical person to be able to use this system."),
    Sus("SUS05", "Βρήκα ότι οι διάφορες λειτουργίες του παιχνιδιού ήταν καλά ενσωματωμένες.", "I found the various functions in this system were well integrated."),
    Sus("SUS06", "Θεώρησα ότι υπήρχε υπερβολική ασυνέπεια στο παιχνίδι.", "I thought there was too much inconsistency in this system."),
    Sus("SUS07", "Φαντάζομαι ότι οι περισσότεροι άνθρωποι θα μάθαιναν να παίζουν αυτό το παιχνίδι πολύ γρήγορα.", "I would imagine that most people would learn to use this system very quickly."),
    Sus("SUS08", "Βρήκα το παιχνίδι πολύ δύσχρηστο.", "I found the system very cumbersome to use."),
    Sus("SUS09", "Ένιωσα πολύ σίγουρος/η παίζοντας το παιχνίδι.", "I felt very confident using the system."),
    Sus("SUS10", "Χρειάστηκε να μάθω πολλά πράγματα πριν μπορέσω να ξεκινήσω με το παιχνίδι.", "I needed to learn a lot of things before I could get going with this system."),
  };

  // In-game Game Experience Questionnaire (iGEQ, IJsselsteijn, de Kort & Poels, 2013). 0..4.
  public static readonly QuestionItem[] IGEQ =
  {
    Geq("iGEQ01", "Με ενδιέφερε η «ιστορία» του παιχνιδιού.", "I was interested in the game's story."),
    Geq("iGEQ02", "Ένιωσα επιτυχημένος/η.", "I felt successful."),
    Geq("iGEQ03", "Βαρέθηκα.", "I felt bored."),
    Geq("iGEQ04", "Το βρήκα εντυπωσιακό.", "I found it impressive."),
    Geq("iGEQ05", "Ξέχασα τα πάντα γύρω μου.", "I forgot everything around me."),
    Geq("iGEQ06", "Ένιωσα απογοήτευση.", "I felt frustrated."),
    Geq("iGEQ07", "Το βρήκα κουραστικό.", "I found it tiresome."),
    Geq("iGEQ08", "Ένιωσα εκνευρισμένος/η.", "I felt irritable."),
    Geq("iGEQ09", "Ένιωσα ικανός/ή.", "I felt skilful."),
    Geq("iGEQ10", "Απορροφήθηκα πλήρως από το παιχνίδι.", "I felt completely absorbed."),
    Geq("iGEQ11", "Ένιωσα ικανοποιημένος/η.", "I felt content."),
    Geq("iGEQ12", "Ένιωσα ότι το παιχνίδι μού έθετε πρόκληση.", "I felt challenged."),
    Geq("iGEQ13", "Χρειάστηκε να καταβάλω μεγάλη προσπάθεια.", "I had to put a lot of effort into it."),
    Geq("iGEQ14", "Ένιωσα ωραία.", "I felt good."),
  };

  // Study-specific items about aspects where AR and classic play are expected to differ.
  public static readonly QuestionItem[] Custom =
  {
    Item("CUSTOM", "C01_fatigue", "Ένιωσα σωματική κούραση (π.χ. στα χέρια, στους ώμους ή στον αυχένα).", "I felt physical fatigue (arms, shoulders, neck).", IntensityLabels, IntensityValues),
    Item("CUSTOM", "C02_discomfort", "Ένιωσα ζάλη, ναυτία ή οπτική ενόχληση.", "I felt dizziness, nausea or visual discomfort.", IntensityLabels, IntensityValues),
    Item("CUSTOM", "C03_presence", "Ένιωσα ότι το πεδίο της μάχης ήταν μέρος του χώρου γύρω μου.", "I felt the battlefield was part of the space around me.", IntensityLabels, IntensityValues),
    Item("CUSTOM", "C04_precision", "Ήταν εύκολο να τοποθετήσω τους πύργους ακριβώς εκεί που ήθελα.", "It was easy to place the towers exactly where I wanted.", IntensityLabels, IntensityValues),
    Item("CUSTOM", "C05_overview", "Μπορούσα εύκολα να έχω εικόνα όλου του πεδίου της μάχης.", "I could easily keep an overview of the whole battlefield.", IntensityLabels, IntensityValues),
  };

  static readonly string[] ModeChoice = { "Κλασική", "AR", "Καμία\nδιαφορά" };
  static readonly int[] ModeChoiceValues = { 0, 1, 2 };

  // Asked once per participant after both sessions (comparison + demographics).
  public static readonly QuestionItem[] Final =
  {
    Item("FINAL", "F01_preference", "Ποια έκδοση προτιμήσατε συνολικά;", "Which version did you prefer overall?", ModeChoice, ModeChoiceValues),
    Item("FINAL", "F02_fun", "Ποια έκδοση ήταν πιο διασκεδαστική;", "Which version was more fun?", ModeChoice, ModeChoiceValues),
    Item("FINAL", "F03_ease", "Ποια έκδοση ήταν πιο εύκολη στον χειρισμό;", "Which version was easier to control?", ModeChoice, ModeChoiceValues),
    Item("FINAL", "F04_immersion", "Ποια έκδοση σας έκανε να νιώσετε πιο «μέσα» στο παιχνίδι;", "Which version was more immersive?", ModeChoice, ModeChoiceValues),
    Item("FINAL", "F05_strategy", "Σε ποια έκδοση σχεδιάσατε καλύτερα τη στρατηγική σας;", "In which version could you plan your strategy better?", ModeChoice, ModeChoiceValues),
    Item("FINAL", "F06_again", "Ποια έκδοση θα παίζατε ξανά στον ελεύθερο χρόνο σας;", "Which version would you play again in your free time?", ModeChoice, ModeChoiceValues),
    Item("DEMO", "D01_age", "Ηλικιακή ομάδα", "Age group",
        new[] { "18-24", "25-34", "35-44", "45+" }, new[] { 0, 1, 2, 3 }),
    Item("DEMO", "D02_gender", "Φύλο", "Gender",
        new[] { "Άνδρας", "Γυναίκα", "Άλλο /\nΔεν απαντώ" }, new[] { 0, 1, 2 }),
    Item("DEMO", "D03_gaming", "Πόσο συχνά παίζετε παιχνίδια στο κινητό;", "How often do you play mobile games?",
        new[] { "Ποτέ", "Μηνιαία", "Εβδομαδιαία", "Καθημερινά" }, new[] { 0, 1, 2, 3 }),
    Item("DEMO", "D04_ar", "Πόση εμπειρία έχετε με εφαρμογές ή παιχνίδια AR;", "How much experience do you have with AR apps/games?",
        new[] { "Καμία", "Λίγη", "Αρκετή", "Πολλή" }, new[] { 0, 1, 2, 3 }),
    Item("DEMO", "D05_td", "Έχετε παίξει ξανά παιχνίδια Tower Defense;", "Have you played tower defense games before?",
        new[] { "Ποτέ", "Λίγες φορές", "Συχνά" }, new[] { 0, 1, 2 }),
  };

  public static List<QuestionItem> PostSession()
  {
    return SUS.Concat(IGEQ).Concat(Custom).ToList();
  }

  // ---- Scoring -----------------------------------------------------------

  // SUS score 0..100: odd items contribute (x - 1), even items (5 - x), sum * 2.5.
  public static float? ScoreSUS(IDictionary<string, int> answers)
  {
    float sum = 0f;

    for (int i = 0; i < SUS.Length; i++)
    {
      if (!answers.TryGetValue(SUS[i].Id, out int value))
        return null;

      sum += i % 2 == 0 ? value - 1 : 5 - value;
    }

    return sum * 2.5f;
  }

  // iGEQ components (mean of two items each, 0..4).
  public static readonly Dictionary<string, string[]> IGEQComponents = new Dictionary<string, string[]>
  {
    { "competence", new[] { "iGEQ02", "iGEQ09" } },
    { "immersion", new[] { "iGEQ01", "iGEQ04" } },
    { "flow", new[] { "iGEQ05", "iGEQ10" } },
    { "tension", new[] { "iGEQ06", "iGEQ08" } },
    { "challenge", new[] { "iGEQ12", "iGEQ13" } },
    { "negative_affect", new[] { "iGEQ03", "iGEQ07" } },
    { "positive_affect", new[] { "iGEQ11", "iGEQ14" } },
  };

  public static Dictionary<string, float?> ScoreIGEQ(IDictionary<string, int> answers)
  {
    var scores = new Dictionary<string, float?>();

    foreach (var component in IGEQComponents)
    {
      float sum = 0f;
      bool complete = true;

      foreach (string id in component.Value)
      {
        if (answers.TryGetValue(id, out int value))
          sum += value;
        else
          complete = false;
      }

      scores[component.Key] = complete ? sum / component.Value.Length : (float?)null;
    }

    return scores;
  }

  static QuestionItem Sus(string id, string text, string en)
  {
    return Item("SUS", id, text, en, AgreementLabels, AgreementValues);
  }

  static QuestionItem Geq(string id, string text, string en)
  {
    return Item("iGEQ", id, text, en, IntensityLabels, IntensityValues);
  }

  static QuestionItem Item(string instrument, string id, string text, string en, string[] options, int[] values)
  {
    return new QuestionItem
    {
      Instrument = instrument,
      Id = id,
      Text = text,
      TextEn = en,
      Options = options,
      Values = values
    };
  }
}
