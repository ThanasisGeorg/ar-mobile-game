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

    if (currentTarget == null)
      return;

    timer += Time.deltaTime;

    if (timer >= fireRate)
    {
      Shoot();
      timer = 0f;
    }
  }

  void FindTarget()
  {
    GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

    float shortestDistance = Mathf.Infinity;
    GameObject nearestEnemy = null;

    foreach (GameObject enemy in enemies)
    {
      float distance = Vector3.Distance(transform.position, enemy.transform.position);

      if (distance < shortestDistance && distance <= range)
      {
        shortestDistance = distance;
        nearestEnemy = enemy;
      }
    }

    if (nearestEnemy != null)
    {
      currentTarget = nearestEnemy.transform;
    }
    else
    {
      currentTarget = null;
    }
  }

  void Shoot()
  {
    GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
    bullet.GetComponent<Bullet>().SetTarget(currentTarget);
  }
}