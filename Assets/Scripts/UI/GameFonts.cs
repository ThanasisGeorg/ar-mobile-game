using TMPro;
using UnityEngine;

// Provides a TextMeshPro font that contains Greek glyphs. TMP's default font only has
// Latin characters baked in and relies on an LFS-stored .ttf for anything else, so
// Greek text showed up as boxes when that file was missing. DejaVu Sans is shipped in
// Resources/Fonts as a regular git file and turned into a dynamic font asset at runtime.
public static class GameFonts
{
  static TMP_FontAsset uiFont;
  static bool attempted;

  public static TMP_FontAsset UI
  {
    get
    {
      if (!attempted)
      {
        attempted = true;
        uiFont = CreateUIFont();
      }

      return uiFont;
    }
  }

  static TMP_FontAsset CreateUIFont()
  {
    Font source = Resources.Load<Font>("Fonts/DejaVuSans");

    if (source == null)
    {
      Debug.LogWarning("GameFonts: Resources/Fonts/DejaVuSans.ttf not found, Greek text may not render.");
      return null;
    }

    TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source);

    if (asset == null)
    {
      Debug.LogWarning("GameFonts: could not create a TMP font asset from DejaVuSans.");
      return null;
    }

    asset.name = "DejaVuSans SDF (runtime)";
    return asset;
  }
}
