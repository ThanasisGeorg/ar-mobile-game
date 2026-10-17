using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
  public int maxHealth = 100;
  public int reward = 10;
  public int baseDamage = 1;

  public static event Action<EnemyHealth> Killed;
  public static event Action<EnemyHealth> Leaked;

  private int currentHealth;
  private bool isDead;
  private bool initialized;
  private EnemyHealthBar healthBar;

  public float HealthFraction => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

  void OnEnable()
  {
    WaveManager.ActiveEnemies.Add(this);
  }

  void OnDisable()
  {
    WaveManager.ActiveEnemies.Remove(this);
  }

  void Start()
  {
    if (!initialized)
      currentHealth = maxHealth;

    healthBar = EnemyHealthBar.Create(transform);
    healthBar.SetFraction(HealthFraction);
  }

  // Called by the WaveManager right after spawning to scale health with the wave number.
  public void Init(int health)
  {
    maxHealth = Mathf.Max(1, health);
    currentHealth = maxHealth;
    initialized = true;
  }

  public void TakeDamage(int damage)
  {
    if (isDead)
      return;

    currentHealth -= damage;

    currentHealth = Mathf.Max(currentHealth, 0);

    if (healthBar != null)
      healthBar.SetFraction(HealthFraction);

    if (currentHealth <= 0)
    {
      isDead = true;
      Die();
    }
  }

  public void ReachBase()
  {
    if (isDead)
      return;

    isDead = true;

    if (GameManager.Instance != null)
      GameManager.Instance.DamageBase(baseDamage);

    Leaked?.Invoke(this);

    Destroy(gameObject);
  }

  void Die()
  {
    Debug.Log("ENEMY DIED");

    if (GameEconomy.Instance != null)
      GameEconomy.Instance.AddCoins(reward);

    Killed?.Invoke(this);

    Destroy(gameObject);
  }
}
