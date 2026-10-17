using System;
using UnityEngine;

public class GameEconomy : MonoBehaviour
{
  public static GameEconomy Instance;

  public int coins = 100;

  public int TotalEarned { get; private set; }
  public int TotalSpent { get; private set; }

  public event Action<int> CoinsChanged;

  private void Awake()
  {
    Instance = this;
  }

  private void OnDestroy()
  {
    if (Instance == this)
      Instance = null;
  }

  public void AddCoins(int amount)
  {
    coins += amount;
    TotalEarned += amount;
    Debug.Log("Coins: " + coins);
    CoinsChanged?.Invoke(coins);
  }

  public bool CanAfford(int amount)
  {
    return coins >= amount;
  }

  public bool SpendCoins(int amount)
  {
    if (coins >= amount)
    {
      coins -= amount;
      TotalSpent += amount;
      Debug.Log("Spent: " + amount + " | Coins left: " + coins);
      CoinsChanged?.Invoke(coins);
      return true;
    }

    Debug.Log("Not enough coins!");
    return false;
  }
}
