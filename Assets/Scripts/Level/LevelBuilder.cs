using UnityEngine;

// Builds the battlefield procedurally. Using the same code for both versions
// guarantees an identical level (size, path, difficulty), which is a requirement
// for a fair comparison of the classic and AR user experience.
public static class LevelBuilder
{
  public const float BoardHalfWidth = 12f;
  public const float BoardHalfDepth = 7f;
  public const float EnemyHeight = 1f;

  // Enemy route on the board (x, z), from the spawn portal to the player's base.
  static readonly Vector2[] Path =
  {
    new Vector2(-12f, 4f),
    new Vector2(-6f, 4f),
    new Vector2(-6f, -3f),
    new Vector2(0f, -3f),
    new Vector2(0f, 3f),
    new Vector2(6f, 3f),
    new Vector2(6f, -2f),
    new Vector2(11f, -2f),
  };

  static readonly Vector2Int[] Trees =
  {
    new Vector2Int(-10, -5),
    new Vector2Int(-9, 1),
    new Vector2Int(-2, 6),
    new Vector2Int(3, -6),
    new Vector2Int(9, 5),
    new Vector2Int(10, -6),
  };

  static readonly Color GrassColor = new Color(0.34f, 0.62f, 0.3f);
  static readonly Color FrameColor = new Color(0.36f, 0.25f, 0.16f);
  static readonly Color PathColor = new Color(0.83f, 0.7f, 0.47f);
  static readonly Color PortalColor = new Color(0.75f, 0.15f, 0.2f);
  static readonly Color BaseColor = new Color(0.2f, 0.4f, 0.85f);
  static readonly Color RoofColor = new Color(0.9f, 0.8f, 0.25f);
  static readonly Color TrunkColor = new Color(0.45f, 0.3f, 0.18f);
  static readonly Color LeavesColor = new Color(0.16f, 0.45f, 0.2f);

  public static LevelLayout Build()
  {
    GameObject root = new GameObject("Level");
    LevelLayout layout = root.AddComponent<LevelLayout>();
    layout.halfSize = new Vector2(BoardHalfWidth, BoardHalfDepth);
    layout.boardTop = 0f;

    // Board (top surface at y = 0). Its collider is used for tower placement raycasts.
    GameObject ground = Box(root.transform, "Ground", new Vector3(0f, -0.1f, 0f),
        new Vector3(BoardHalfWidth * 2f, 0.2f, BoardHalfDepth * 2f), GrassColor, keepCollider: true);
    ground.tag = "Ground";
    layout.ground = ground.GetComponent<Collider>();

    Box(root.transform, "Frame", new Vector3(0f, -0.2f, 0f),
        new Vector3(BoardHalfWidth * 2f + 0.8f, 0.3f, BoardHalfDepth * 2f + 0.8f), FrameColor);

    // Path
    Vector3[] waypoints = new Vector3[Path.Length];

    for (int i = 0; i < Path.Length; i++)
      waypoints[i] = new Vector3(Path[i].x, EnemyHeight, Path[i].y);

    layout.waypoints = waypoints;

    for (int i = 1; i < Path.Length; i++)
    {
      Vector3 a = new Vector3(Path[i - 1].x, 0f, Path[i - 1].y);
      Vector3 b = new Vector3(Path[i].x, 0f, Path[i].y);
      Vector3 mid = (a + b) * 0.5f;
      Vector3 size = new Vector3(Mathf.Abs(b.x - a.x) + 1f, 0.02f, Mathf.Abs(b.z - a.z) + 1f);
      mid.y = 0.011f + i * 0.001f;
      Box(root.transform, "Path " + i, mid, size, PathColor);
    }

    layout.BlockPath(0.75f);

    // Spawn portal
    Vector3 spawnPos = waypoints[0];
    GameObject spawn = new GameObject("EnemySpawner");
    spawn.transform.SetParent(root.transform, false);
    spawn.transform.position = spawnPos;
    layout.spawnPoint = spawn.transform;

    GameObject portal = Primitive(PrimitiveType.Cylinder, root.transform, "Portal",
        new Vector3(spawnPos.x + 0.5f, 0.05f, spawnPos.z), new Vector3(1.6f, 0.05f, 1.6f), PortalColor);
    portal.transform.SetParent(spawn.transform, true);
    layout.BlockArea(new Vector3(spawnPos.x + 0.5f, 0f, spawnPos.z), 1.2f);

    // Player's base
    Vector3 basePos = waypoints[waypoints.Length - 1];
    GameObject target = new GameObject("Target");
    target.transform.SetParent(root.transform, false);
    target.transform.position = basePos;
    layout.baseTarget = target.transform;

    Box(target.transform, "Keep", new Vector3(basePos.x, 0.8f, basePos.z), new Vector3(1.6f, 1.6f, 1.6f), BaseColor);
    Primitive(PrimitiveType.Cylinder, target.transform, "Roof",
        new Vector3(basePos.x, 1.8f, basePos.z), new Vector3(1.2f, 0.4f, 1.2f), RoofColor);
    layout.BlockArea(new Vector3(basePos.x, 0f, basePos.z), 1.5f);

    // Decoration (also blocks building, the same for every player)
    foreach (Vector2Int cell in Trees)
    {
      Vector3 p = layout.CellToWorld(cell);
      Primitive(PrimitiveType.Cylinder, root.transform, "Trunk",
          p + new Vector3(0f, 0.4f, 0f), new Vector3(0.25f, 0.4f, 0.25f), TrunkColor);
      Primitive(PrimitiveType.Sphere, root.transform, "Leaves",
          p + new Vector3(0f, 1.1f, 0f), new Vector3(0.9f, 0.9f, 0.9f), LeavesColor);
      layout.Block(cell);
    }

    return layout;
  }

  static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Color color, bool keepCollider = false)
  {
    return Primitive(PrimitiveType.Cube, parent, name, position, scale, color, keepCollider);
  }

  static GameObject Primitive(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 scale, Color color, bool keepCollider = false)
  {
    GameObject go = GameObject.CreatePrimitive(type);
    go.name = name;
    go.transform.SetParent(parent, false);
    go.transform.position = position;
    go.transform.localScale = scale;

    if (!keepCollider)
      Object.Destroy(go.GetComponent<Collider>());

    go.GetComponent<Renderer>().sharedMaterial = GameMaterials.Solid(color);
    return go;
  }
}
