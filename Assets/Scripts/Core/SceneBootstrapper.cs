using UnityEngine;
using UnityEngine.SceneManagement;

// Sets up every scene from code when it is loaded, so the project does not depend
// on hand-wired scene objects: MainMenu gets the menu UI, Game_Classic and
// Game_AR get the same level and managers plus their version-specific camera.
public static class SceneBootstrapper
{
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  static void ResetStatics()
  {
    WaveManager.ActiveEnemies.Clear();
  }

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Initialize()
  {
    Application.targetFrameRate = 60;
    Screen.sleepTimeout = SleepTimeout.NeverSleep;

    // Landscape gives the most room for the board in both versions.
    Screen.autorotateToPortrait = false;
    Screen.autorotateToPortraitUpsideDown = false;
    Screen.autorotateToLandscapeLeft = true;
    Screen.autorotateToLandscapeRight = true;
    Screen.orientation = ScreenOrientation.AutoRotation;

    SceneManager.sceneLoaded -= OnSceneLoaded;
    SceneManager.sceneLoaded += OnSceneLoaded;
  }

  static void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
  {
    if (scene.name == StudySession.MainMenuScene)
    {
      if (Object.FindFirstObjectByType<MainMenuUI>() == null)
        MainMenuUI.Create();
      return;
    }

    GameMode? mode = StudySession.ModeForScene(scene.name);

    if (mode.HasValue && Object.FindFirstObjectByType<GameBootstrap>() == null)
    {
      GameObject go = new GameObject("Game Bootstrap");
      go.AddComponent<GameBootstrap>().Setup(mode.Value, scene);
    }
  }
}
