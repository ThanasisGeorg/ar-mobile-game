using UnityEngine;

public class Target : MonoBehaviour
{
  // Enemies normally detect the base themselves (EnemyMovement); this trigger is a
  // fallback for enemies that physically enter the base collider.
  private void OnTriggerEnter(Collider other)
  {
    if (other.CompareTag("Enemy"))
    {
      Debug.Log("Enemy reached the base!");

      EnemyHealth health = other.GetComponentInParent<EnemyHealth>();

      if (health != null)
      {
        health.ReachBase();
      }
      else
      {
        GameManager.Instance.DamageBase(1);
        Destroy(other.gameObject);
      }
    }
  }
}
