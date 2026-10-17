using System.Collections.Generic;
using UnityEngine;

// Creates coloured materials at runtime by cloning the project's URP materials
// (taken from the prefabs in Resources), so the right shaders are always included in builds.
public static class GameMaterials
{
  static Material solidTemplate;
  static Material transparentTemplate;
  static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

  public static Material Solid(Color color)
  {
    return Get("solid", color, SolidTemplate);
  }

  public static Material Transparent(Color color)
  {
    return Get("transparent", color, TransparentTemplate);
  }

  public static void SetColor(Material material, Color color)
  {
    if (material.HasProperty("_BaseColor"))
      material.SetColor("_BaseColor", color);

    if (material.HasProperty("_Color"))
      material.SetColor("_Color", color);
  }

  static Material Get(string kind, Color color, Material template)
  {
    string key = kind + ColorUtility.ToHtmlStringRGBA(color);

    if (cache.TryGetValue(key, out Material cached) && cached != null)
      return cached;

    Material material = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
    material.name = key;
    SetColor(material, color);
    cache[key] = material;
    return material;
  }

  static Material SolidTemplate
  {
    get
    {
      if (solidTemplate == null)
        solidTemplate = MaterialFromPrefab("Prefabs/Tower");
      return solidTemplate;
    }
  }

  static Material TransparentTemplate
  {
    get
    {
      if (transparentTemplate == null)
        transparentTemplate = MaterialFromPrefab("Prefabs/Tower_Ghost");
      return transparentTemplate;
    }
  }

  static Material MaterialFromPrefab(string path)
  {
    GameObject prefab = Resources.Load<GameObject>(path);

    if (prefab == null)
      return null;

    Renderer renderer = prefab.GetComponentInChildren<Renderer>();
    return renderer != null ? renderer.sharedMaterial : null;
  }
}
