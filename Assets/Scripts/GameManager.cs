using UnityEngine;

public class GameManager : MonoBehaviour
{
  public static GameManager Instance;

  public int playerHealth = 10;

  private void Awake()
  {
    Instance = this;
  }

  public void DamageBase(int damage)
  {
    playerHealth -= damage;

    Debug.Log("Base HP: " + playerHealth);

    if (playerHealth <= 0)
    {
      GameOver();
    }
  }

  void GameOver()
  {
    Debug.Log("GAME OVER");
    Time.timeScale = 0f;
  }
}