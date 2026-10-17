using System;
using UnityEngine;

public enum PlacementResult
{
  Placed,
  NotEnoughCoins,
  InvalidCell,
  Occupied,
  MissedBoard
}

public class TowerPlacement : MonoBehaviour
{
  public GameObject towerPrefab;
  public GameObject ghostPrefab;
  public Camera cam;

  public int towerCost = 20;
  public float gridSize = 1f;

  [Tooltip("Height of the tower pivot above the board (the tower cube is 4 units tall).")]
  public float towerHeightOffset = 2f;

  [NonSerialized] public LevelLayout level;

  public int TowersPlaced { get; private set; }

  public event Action<PlacementResult, Vector3> PlacementAttempted;

  private GameObject ghostInstance;
  private Renderer[] ghostRenderers;
  private LineRenderer rangeRing;
  private float towerRange = 5f;

  static readonly Color ValidColor = new Color(0.3f, 1f, 0.4f, 0.45f);
  static readonly Color InvalidColor = new Color(1f, 0.25f, 0.25f, 0.45f);

  void Start()
  {
    ghostInstance = Instantiate(ghostPrefab);
    ghostInstance.name = "Tower Ghost";

    // The ghost is only a preview: it must never block raycasts or collide.
    foreach (Collider c in ghostInstance.GetComponentsInChildren<Collider>())
      Destroy(c);

    ghostRenderers = ghostInstance.GetComponentsInChildren<Renderer>();

    foreach (Renderer r in ghostRenderers)
      r.material = GameMaterials.Transparent(ValidColor);

    TowerAttack attack = towerPrefab != null ? towerPrefab.GetComponent<TowerAttack>() : null;

    if (attack != null)
      towerRange = attack.range;

    rangeRing = CreateRangeRing(towerRange);
    SetGhostVisible(false);
  }

  void Update()
  {
    GameManager gm = GameManager.Instance;
    bool canBuild = gm != null && gm.IsPlaying && cam != null;

    if (!canBuild)
    {
      SetGhostVisible(false);
      return;
    }

    UpdateGhost();

    if (PointerInput.TryGetTap(out Vector2 tapPosition, out bool overUI) && !overUI)
    {
      PlaceTower(tapPosition);
    }
  }

  void UpdateGhost()
  {
    if (!PointerInput.TryGetPointer(out Vector2 pointer) || PointerInput.IsOverUI(pointer) ||
        !TryGetBoardPoint(pointer, out Vector3 point))
    {
      SetGhostVisible(false);
      return;
    }

    Vector3 pos = SnapToGrid(point);
    ghostInstance.transform.position = pos + Vector3.up * towerHeightOffset;
    rangeRing.transform.position = pos + Vector3.up * 0.05f;

    bool valid = IsBuildable(pos) && GameEconomy.Instance != null && GameEconomy.Instance.CanAfford(towerCost);
    Color color = valid ? ValidColor : InvalidColor;

    foreach (Renderer r in ghostRenderers)
      GameMaterials.SetColor(r.material, color);

    GameMaterials.SetColor(rangeRing.material, color);
    SetGhostVisible(true);
  }

  void PlaceTower(Vector2 screenPosition)
  {
    if (!TryGetBoardPoint(screenPosition, out Vector3 point))
    {
      Report(PlacementResult.MissedBoard, Vector3.zero);
      return;
    }

    Vector3 pos = SnapToGrid(point);

    if (level != null)
    {
      Vector2Int cell = level.WorldToCell(pos);

      if (!level.IsInside(cell) || level.IsBlocked(cell))
      {
        Report(PlacementResult.InvalidCell, pos);
        return;
      }

      if (level.IsOccupied(cell))
      {
        Report(PlacementResult.Occupied, pos);
        return;
      }
    }

    if (!GameEconomy.Instance.SpendCoins(towerCost))
    {
      Report(PlacementResult.NotEnoughCoins, pos);
      return;
    }

    Transform parent = level != null ? level.transform : null;
    Instantiate(towerPrefab, pos + Vector3.up * towerHeightOffset, Quaternion.identity, parent);

    if (level != null)
      level.Occupy(level.WorldToCell(pos));

    TowersPlaced++;
    Report(PlacementResult.Placed, pos);
  }

  void Report(PlacementResult result, Vector3 position)
  {
    PlacementAttempted?.Invoke(result, position);
  }

  bool IsBuildable(Vector3 pos)
  {
    return level == null || level.IsBuildable(level.WorldToCell(pos));
  }

  bool TryGetBoardPoint(Vector2 screenPosition, out Vector3 point)
  {
    Ray ray = cam.ScreenPointToRay(screenPosition);

    if (level != null && level.ground != null)
    {
      // Test only the board collider so enemies, towers or bullets never get in the way.
      if (level.ground.Raycast(ray, out RaycastHit boardHit, 10000f))
      {
        point = boardHit.point;
        return true;
      }

      point = Vector3.zero;
      return false;
    }

    if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag("Ground"))
    {
      point = hit.point;
      return true;
    }

    point = Vector3.zero;
    return false;
  }

  Vector3 SnapToGrid(Vector3 position)
  {
    float x = Mathf.Round(position.x / gridSize) * gridSize;
    float z = Mathf.Round(position.z / gridSize) * gridSize;
    float y = level != null ? level.boardTop : 0f;

    return new Vector3(x, y, z);
  }

  void SetGhostVisible(bool visible)
  {
    if (ghostInstance != null && ghostInstance.activeSelf != visible)
      ghostInstance.SetActive(visible);

    if (rangeRing != null && rangeRing.gameObject.activeSelf != visible)
      rangeRing.gameObject.SetActive(visible);
  }

  LineRenderer CreateRangeRing(float radius)
  {
    GameObject ring = new GameObject("Tower Range");
    LineRenderer line = ring.AddComponent<LineRenderer>();
    const int segments = 64;

    line.useWorldSpace = false;
    line.loop = true;
    line.positionCount = segments;
    line.widthMultiplier = 0.08f;
    line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    line.material = GameMaterials.Transparent(ValidColor);

    for (int i = 0; i < segments; i++)
    {
      float angle = i * Mathf.PI * 2f / segments;
      line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
    }

    return line;
  }
}
