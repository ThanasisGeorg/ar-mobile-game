using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameMode
{
  Classic,
  AR
}

// Holds the state of the user study across scenes: which participant is playing,
// which version of the game is loaded and which sessions are already completed.
// Progress is kept in PlayerPrefs so it survives app restarts between sessions.
public static class StudySession
{
  public const string MainMenuScene = "MainMenu";
  public const string ClassicScene = "Game_Classic";
  public const string ARScene = "Game_AR";

  public static GameMode CurrentMode = GameMode.Classic;

  public static int ParticipantId
  {
    get => Mathf.Max(1, PlayerPrefs.GetInt("ux.participant", 1));
    set
    {
      PlayerPrefs.SetInt("ux.participant", Mathf.Max(1, value));
      PlayerPrefs.Save();
    }
  }

  public static bool QuestionnairesEnabled
  {
    get => PlayerPrefs.GetInt("ux.questionnaires", 1) == 1;
    set
    {
      PlayerPrefs.SetInt("ux.questionnaires", value ? 1 : 0);
      PlayerPrefs.Save();
    }
  }

  public static string SceneFor(GameMode mode)
  {
    return mode == GameMode.AR ? ARScene : ClassicScene;
  }

  public static GameMode? ModeForScene(string sceneName)
  {
    if (sceneName == ClassicScene) return GameMode.Classic;
    if (sceneName == ARScene) return GameMode.AR;
    return null;
  }

  public static bool HasCompleted(int participant, GameMode mode)
  {
    return PlayerPrefs.GetInt($"ux.p{participant}.{mode}", 0) == 1;
  }

  public static void MarkCompleted(int participant, GameMode mode)
  {
    PlayerPrefs.SetInt($"ux.p{participant}.{mode}", 1);
    PlayerPrefs.Save();
  }

  public static int CompletedCount(int participant)
  {
    int count = 0;
    if (HasCompleted(participant, GameMode.Classic)) count++;
    if (HasCompleted(participant, GameMode.AR)) count++;
    return count;
  }

  public static bool FinalQuestionnaireDone(int participant)
  {
    return PlayerPrefs.GetInt($"ux.p{participant}.final", 0) == 1;
  }

  public static void MarkFinalQuestionnaireDone(int participant)
  {
    PlayerPrefs.SetInt($"ux.p{participant}.final", 1);
    PlayerPrefs.Save();
  }

  // Counterbalancing (within-subjects design): odd participants start with the
  // classic version, even participants start with the AR version.
  public static GameMode FirstModeFor(int participant)
  {
    return participant % 2 == 1 ? GameMode.Classic : GameMode.AR;
  }

  public static GameMode? NextModeFor(int participant)
  {
    GameMode first = FirstModeFor(participant);
    GameMode second = first == GameMode.Classic ? GameMode.AR : GameMode.Classic;

    if (!HasCompleted(participant, first)) return first;
    if (!HasCompleted(participant, second)) return second;
    return null;
  }

  public static void ResetParticipant(int participant)
  {
    PlayerPrefs.DeleteKey($"ux.p{participant}.{GameMode.Classic}");
    PlayerPrefs.DeleteKey($"ux.p{participant}.{GameMode.AR}");
    PlayerPrefs.DeleteKey($"ux.p{participant}.final");
    PlayerPrefs.Save();
  }

  public static void Play(GameMode mode)
  {
    CurrentMode = mode;
    Time.timeScale = 1f;
    SceneManager.LoadScene(SceneFor(mode));
  }

  public static void ReturnToMenu()
  {
    Time.timeScale = 1f;
    SceneManager.LoadScene(MainMenuScene);
  }
}
