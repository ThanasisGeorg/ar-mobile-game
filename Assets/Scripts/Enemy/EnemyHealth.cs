using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
  public int maxHealth = 100;

  private int currentHealth;
  private bool isDead;

  void Start()
  {
    currentHealth = maxHealth;
  }

  public void TakeDamage(int damage)
  {
    if (isDead)
      return;

    currentHealth -= damage;

    currentHealth = Mathf.Max(currentHealth, 0);

    if (currentHealth <= 0)
    {
      isDead = true;
      Die();
    }
  }

  void Die()
  {
    Debug.Log("ENEMY DIED");

    GameEconomy.Instance.AddCoins(10);

    Destroy(transform.root.gameObject);
  }
}