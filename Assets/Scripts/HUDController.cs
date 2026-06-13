using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
  public TMP_Text coins;
  public TMP_Text wave;
  public TMP_Text hp;

  public GameEconomy economy;
  public GameManager gameManager;
  public WaveManager waveManager;

  void Update()
  {
    coins.text = "Coins: " + economy.coins;
    hp.text = "Base HP: " + gameManager.playerHealth;
    wave.text = "Wave: " + waveManager.CurrentWave;
  }
}