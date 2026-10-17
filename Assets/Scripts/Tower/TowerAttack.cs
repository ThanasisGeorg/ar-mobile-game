using UnityEngine;

public class TowerAttack : MonoBehaviour
{
  public float range = 5f;
  public float fireRate = 1.5f;

  public GameObject bulletPrefab;
  public Transform firePoint;

  private Transform currentTarget;
  private float timer;

  void Update()
  {
    FindTarget();

    // The tower is "loaded" while idle so it fires as soon as an enemy comes in range.
    timer = Mathf.Min(timer + Time.deltaTime, fireRate);

    if (currentTarget == null)
      return;

    if (timer >= fireRate)
    {
      Shoot();
      timer = 0f;
    }
  }

  void FindTarget()
  {
    // Aim at the enemy that walked furthest (closest to the base) within range.
    float bestProgress = -1f;
    EnemyHealth bestEnemy = null;

    foreach (EnemyHealth enemy in WaveManager.ActiveEnemies)
    {
      if (enemy == null)
        continue;

      Vector3 offset = enemy.transform.position - transform.position;
      offset.y = 0f;

      if (offset.magnitude > range)
        continue;

      EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
      float progress = movement != null ? movement.DistanceTravelled : 0f;

      if (progress > bestProgress)
      {
        bestProgress = progress;
        bestEnemy = enemy;
      }
    }

    currentTarget = bestEnemy != null ? bestEnemy.transform : null;
  }

  void Shoot()
  {
    Transform origin = firePoint != null ? firePoint : transform;
    GameObject bullet = Instantiate(bulletPrefab, origin.position, Quaternion.identity);
    bullet.GetComponent<Bullet>().SetTarget(currentTarget);
  }
}
