using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacement : MonoBehaviour
{
  public GameObject towerPrefab;
  public GameObject ghostPrefab;
  public Camera cam;

  public int towerCost = 20;
  public float gridSize = 1f;

  private GameObject ghostInstance;

  void Start()
  {
    ghostInstance = Instantiate(ghostPrefab);
  }

  void Update()
  {
    UpdateGhost();

    if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
    {
      PlaceTower();
    }
  }

  void UpdateGhost()
  {
    if (Touchscreen.current == null) return;

    Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();

    Ray ray = cam.ScreenPointToRay(touchPos);

    if (Physics.Raycast(ray, out RaycastHit hit))
    {
      if (hit.collider.CompareTag("Ground"))
      {
        Vector3 pos = SnapToGrid(hit.point);
        ghostInstance.transform.position = pos;
      }
    }
  }

  void PlaceTower()
  {
    if (!GameEconomy.Instance.SpendCoins(towerCost))
      return;

    Vector3 pos = ghostInstance.transform.position;

    Instantiate(towerPrefab, pos, Quaternion.identity);
  }

  Vector3 SnapToGrid(Vector3 position)
  {
    float x = Mathf.Round(position.x / gridSize) * gridSize;
    float z = Mathf.Round(position.z / gridSize) * gridSize;

    return new Vector3(x, 0f, z);
  }
}