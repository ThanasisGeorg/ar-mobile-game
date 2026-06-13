using UnityEngine;

public class GameEconomy : MonoBehaviour
{
  public static GameEconomy Instance;

  public int coins = 100;

  private void Awake()
  {
    Instance = this;
  }

  public void AddCoins(int amount)
  {
    coins += amount;
    Debug.Log("Coins: " + coins);
  }

  public bool SpendCoins(int amount)
  {
    if (coins >= amount)
    {
      coins -= amount;
      Debug.Log("Spent: " + amount + " | Coins left: " + coins);
      return true;
    }

    Debug.Log("Not enough coins!");
    return false;
  }
}