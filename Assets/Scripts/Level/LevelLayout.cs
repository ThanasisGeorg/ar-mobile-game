using System.Collections.Generic;
using UnityEngine;

// Runtime description of the battlefield: board size, enemy path and which grid
// cells can hold a tower. Both game versions use exactly the same layout.
public class LevelLayout : MonoBehaviour
{
  public float gridSize = 1f;
  public Vector2 halfSize = new Vector2(12f, 7f);
  public float boardTop = 0f;

  public Collider ground;
  public Transform spawnPoint;
  public Transform baseTarget;
  public Vector3[] waypoints;

  private readonly HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
  private readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

  public Bounds BoardBounds => new Bounds(
      new Vector3(transform.position.x, boardTop, transform.position.z),
      new Vector3(halfSize.x * 2f, 0.2f, halfSize.y * 2f));

  public float BoardWidth => halfSize.x * 2f;

  public float PathLength
  {
    get
    {
      float length = 0f;
      for (int i = 1; i < waypoints.Length; i++)
        length += Vector3.Distance(waypoints[i - 1], waypoints[i]);
      return length;
    }
  }

  public Vector2Int WorldToCell(Vector3 world)
  {
    return new Vector2Int(
        Mathf.RoundToInt(world.x / gridSize),
        Mathf.RoundToInt(world.z / gridSize));
  }

  public Vector3 CellToWorld(Vector2Int cell)
  {
    return new Vector3(cell.x * gridSize, boardTop, cell.y * gridSize);
  }

  public bool IsInside(Vector2Int cell)
  {
    float half = gridSize * 0.5f;
    Vector3 p = CellToWorld(cell);
    return Mathf.Abs(p.x) + half <= halfSize.x + 0.001f && Mathf.Abs(p.z) + half <= halfSize.y + 0.001f;
  }

  public bool IsBlocked(Vector2Int cell) => blocked.Contains(cell);
  public bool IsOccupied(Vector2Int cell) => occupied.Contains(cell);

  public bool IsBuildable(Vector2Int cell)
  {
    return IsInside(cell) && !IsBlocked(cell) && !IsOccupied(cell);
  }

  public void Block(Vector2Int cell) => blocked.Add(cell);
  public void Occupy(Vector2Int cell) => occupied.Add(cell);

  // Blocks every cell whose centre is closer than `radius` to the enemy path.
  public void BlockPath(float radius)
  {
    int maxX = Mathf.CeilToInt(halfSize.x / gridSize);
    int maxZ = Mathf.CeilToInt(halfSize.y / gridSize);

    for (int x = -maxX; x <= maxX; x++)
    {
      for (int z = -maxZ; z <= maxZ; z++)
      {
        Vector2Int cell = new Vector2Int(x, z);
        Vector3 p = CellToWorld(cell);

        for (int i = 1; i < waypoints.Length; i++)
        {
          if (DistanceToSegment(p, waypoints[i - 1], waypoints[i]) < radius)
          {
            blocked.Add(cell);
            break;
          }
        }
      }
    }
  }

  public void BlockArea(Vector3 center, float radius)
  {
    int r = Mathf.CeilToInt(radius / gridSize);
    Vector2Int c = WorldToCell(center);

    for (int x = -r; x <= r; x++)
    {
      for (int z = -r; z <= r; z++)
      {
        Vector2Int cell = new Vector2Int(c.x + x, c.y + z);
        Vector3 p = CellToWorld(cell);
        p.y = center.y = boardTop;

        if (Vector3.Distance(p, center) <= radius)
          blocked.Add(cell);
      }
    }
  }

  static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
  {
    a.y = b.y = p.y = 0f;
    Vector3 ab = b - a;
    float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
    return Vector3.Distance(p, a + ab * t);
  }
}
