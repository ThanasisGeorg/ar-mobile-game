using UnityEngine;

// Small world-space health bar above an enemy, built from primitives so it
// works without extra assets and looks the same in both game versions.
public class EnemyHealthBar : MonoBehaviour
{
  const float Width = 1f;
  const float Height = 0.14f;

  private Transform fill;
  private Renderer fillRenderer;

  public static EnemyHealthBar Create(Transform enemy)
  {
    GameObject root = new GameObject("HealthBar");
    root.transform.SetParent(enemy, false);
    root.transform.localPosition = new Vector3(0f, 1.45f, 0f);

    EnemyHealthBar bar = root.AddComponent<EnemyHealthBar>();

    Transform background = CreateQuad(root.transform, "Background", new Color(0.1f, 0.1f, 0.1f));
    background.localScale = new Vector3(Width + 0.06f, Height + 0.06f, 0.02f);

    bar.fill = CreateQuad(root.transform, "Fill", Color.green);
    bar.fill.localPosition = new Vector3(0f, 0f, -0.02f);
    bar.fillRenderer = bar.fill.GetComponent<Renderer>();

    return bar;
  }

  static Transform CreateQuad(Transform parent, string name, Color color)
  {
    GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
    quad.name = name;
    Destroy(quad.GetComponent<Collider>());
    quad.transform.SetParent(parent, false);
    quad.transform.localScale = new Vector3(Width, Height, 0.02f);

    Renderer r = quad.GetComponent<Renderer>();
    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    r.receiveShadows = false;
    r.material.color = color;

    return quad.transform;
  }

  public void SetFraction(float fraction)
  {
    fraction = Mathf.Clamp01(fraction);

    fill.localScale = new Vector3(Width * fraction, Height, 0.02f);
    fill.localPosition = new Vector3(-(Width - Width * fraction) * 0.5f, 0f, -0.02f);
    fillRenderer.material.color = Color.Lerp(Color.red, Color.green, fraction);
  }

  void LateUpdate()
  {
    Camera cam = Camera.main;

    if (cam == null)
      return;

    // Billboard: always face the player's camera, but stay upright.
    Vector3 toCamera = cam.transform.position - transform.position;
    toCamera.y = 0f;

    if (toCamera.sqrMagnitude > 0.0001f)
      transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
  }
}
