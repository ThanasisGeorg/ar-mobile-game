using UnityEngine;

public class Target : MonoBehaviour
{
  private void OnTriggerEnter(Collider other)
  {
    if (other.CompareTag("Enemy"))
    {
      GameManager.Instance.DamageBase(1);

      Debug.Log("Enemy reached the base!");

      Destroy(other.gameObject);
    }
  }
}