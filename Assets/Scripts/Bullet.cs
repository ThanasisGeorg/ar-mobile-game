using UnityEngine;

public class Bullet : MonoBehaviour
{
  public float speed = 10f;
  public int damage = 25;
  public float hitDistance = 0.2f;

  private Transform target;
  private bool hasHit;

  public void SetTarget(Transform enemy)
  {
    target = enemy;
  }

  void Update()
  {
    if (hasHit)
      return;

    if (target == null)
    {
      Destroy(gameObject);
      return;
    }

    Vector3 direction = target.position - transform.position;
    float distanceThisFrame = speed * Time.deltaTime;

    if (direction.magnitude <= Mathf.Max(distanceThisFrame, hitDistance))
    {
      HitTarget(target.GetComponentInParent<EnemyHealth>());
      return;
    }

    transform.Translate(direction.normalized * distanceThisFrame, Space.World);
  }

  void OnTriggerEnter(Collider other)
  {
    if (hasHit)
      return;

    EnemyHealth health = other.GetComponentInParent<EnemyHealth>();

    if (health != null)
    {
      HitTarget(health);
    }
  }

  void OnCollisionEnter(Collision collision)
  {
    if (hasHit)
      return;

    EnemyHealth health = collision.collider.GetComponentInParent<EnemyHealth>();

    if (health != null)
    {
      HitTarget(health);
    }
  }

  void HitTarget(EnemyHealth health)
  {
    if (hasHit)
      return;

    hasHit = true;

    if (health != null)
    {
      health.TakeDamage(damage);
    }
    else
    {
      Debug.LogWarning("EnemyHealth component not found on target.");
    }

    Destroy(gameObject);
  }
}