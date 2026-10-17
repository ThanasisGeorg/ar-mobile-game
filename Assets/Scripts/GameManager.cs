using System;
using UnityEngine;

public enum GameState
{
  WaitingToStart,
  Playing,
  Paused,
  Victory,
  Defeat,
  Aborted
}

public class GameManager : MonoBehaviour
{
  public static GameManager Instance;

  public int playerHealth = 10;
  public GameMode mode = GameMode.Classic;

  public GameState State { get; private set; } = GameState.WaitingToStart;
  public int MaxHealth { get; private set; }

  // A "system" pause is requested by the game itself (e.g. AR tracking lost),
  // independently of the pause button pressed by the player.
  public bool IsSystemPaused { get; private set; }
  public string SystemPauseReason { get; private set; }

  public bool IsPlaying => State == GameState.Playing && !IsSystemPaused;
  public bool IsFinished => State == GameState.Victory || State == GameState.Defeat || State == GameState.Aborted;

  public event Action<GameState> StateChanged;
  public event Action<int> BaseDamaged;
  public event Action<bool> SystemPauseChanged;

  private void Awake()
  {
    Instance = this;
    MaxHealth = playerHealth;
    Time.timeScale = 1f;
  }

  private void OnDestroy()
  {
    if (Instance == this)
    {
      Instance = null;
      Time.timeScale = 1f;
    }
  }

  public void StartGame()
  {
    if (State != GameState.WaitingToStart)
      return;

    SetState(GameState.Playing);

    if (WaveManager.Instance != null)
    {
      WaveManager.Instance.Begin();
    }
  }

  public void Pause()
  {
    if (State == GameState.Playing)
      SetState(GameState.Paused);
  }

  public void Resume()
  {
    if (State == GameState.Paused)
      SetState(GameState.Playing);
  }

  public void SetSystemPause(bool paused, string reason = null)
  {
    if (IsSystemPaused == paused)
      return;

    IsSystemPaused = paused;
    SystemPauseReason = paused ? reason : null;
    ApplyTimeScale();
    SystemPauseChanged?.Invoke(paused);
  }

  public void DamageBase(int damage)
  {
    if (State != GameState.Playing)
      return;

    playerHealth = Mathf.Max(0, playerHealth - damage);

    Debug.Log("Base HP: " + playerHealth);
    BaseDamaged?.Invoke(playerHealth);

    if (playerHealth <= 0)
    {
      GameOver();
    }
  }

  public void Victory()
  {
    if (State != GameState.Playing)
      return;

    Debug.Log("VICTORY");
    SetState(GameState.Victory);
  }

  public void AbortSession()
  {
    if (IsFinished)
      return;

    SetState(GameState.Aborted);
  }

  void GameOver()
  {
    Debug.Log("GAME OVER");
    SetState(GameState.Defeat);
  }

  void SetState(GameState newState)
  {
    State = newState;
    ApplyTimeScale();
    StateChanged?.Invoke(newState);
  }

  void ApplyTimeScale()
  {
    bool running = State == GameState.WaitingToStart || (State == GameState.Playing && !IsSystemPaused);
    Time.timeScale = running ? 1f : 0f;
  }
}
