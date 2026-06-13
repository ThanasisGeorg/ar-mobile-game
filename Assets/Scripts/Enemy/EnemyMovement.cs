using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
  public Transform target;
  public float speed = 2f;

  void Update()
  {
    if (target == null) return;

    transform.position = Vector3.MoveTowards(
        transform.position,
        target.position,
        speed * Time.deltaTime
    );
  }
}