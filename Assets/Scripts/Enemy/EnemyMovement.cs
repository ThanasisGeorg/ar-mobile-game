using System;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
  public Transform target;
  public float speed = 2f;
  public float arriveDistance = 0.05f;

  // Path the enemy follows (set by the WaveManager). If empty, the enemy walks straight to the target.
  [NonSerialized] public Vector3[] waypoints;

  private int waypointIndex;
  private bool arrived;

  // Total distance walked; towers use it to aim at the enemy closest to the base.
  public float DistanceTravelled { get; private set; }

  void Update()
  {
    if (arrived) return;

    Vector3 destination;
    bool isFinal;

    if (waypoints != null && waypoints.Length > 0)
    {
      destination = waypoints[waypointIndex];
      isFinal = waypointIndex == waypoints.Length - 1;
    }
    else if (target != null)
    {
      destination = target.position;
      isFinal = true;
    }
    else
    {
      return;
    }

    // Enemies walk on the board plane and keep their own height.
    destination.y = transform.position.y;

    Vector3 previous = transform.position;

    transform.position = Vector3.MoveTowards(
        transform.position,
        destination,
        speed * Time.deltaTime
    );

    Vector3 step = transform.position - previous;
    DistanceTravelled += step.magnitude;

    if (step.sqrMagnitude > 0.000001f)
    {
      transform.rotation = Quaternion.LookRotation(step.normalized, Vector3.up);
    }

    if (Vector3.Distance(transform.position, destination) > arriveDistance)
      return;

    if (isFinal)
    {
      ReachBase();
    }
    else
    {
      waypointIndex++;
    }
  }

  void ReachBase()
  {
    arrived = true;

    EnemyHealth health = GetComponent<EnemyHealth>();

    if (health != null)
    {
      health.ReachBase();
    }
    else
    {
      if (GameManager.Instance != null)
        GameManager.Instance.DamageBase(1);

      Destroy(gameObject);
    }
  }
}
