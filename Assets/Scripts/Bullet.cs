using UnityEngine;

public class Bullet : MonoBehaviour
{
  public float speed = 10f;
  public int damage = 25;

  private Transform target;

  public void SetTarget(Transform enemy)
  {
    target = enemy;
  }

  void Update()
  {
    if (target == null)
    {
      Destroy(gameObject);
      return;
    }

    transform.position = Vector3.MoveTowards(
        transform.position,
        target.position,
        speed * Time.deltaTime
    );

    if (Vector3.Distance(transform.position, target.position) < 0.1f)
    {
      HitTarget();
    }
  }

  void HitTarget()
  {
    EnemyHealth health = target.GetComponent<EnemyHealth>();

    if (health != null)
    {
      health.TakeDamage(damage);
    }

    Destroy(gameObject);
  }
}