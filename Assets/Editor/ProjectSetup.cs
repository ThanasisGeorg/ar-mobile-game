using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One-click project configuration for the AR / Classic tower defense study.
// The idempotent parts (build scenes, AR background renderer feature) also run
// automatically once per editor session.
[InitializeOnLoad]
public static class ProjectSetup
{
  static readonly string[] Scenes =
  {
    "Assets/Scenes/MainMenu.unity",
    "Assets/Scenes/Game_Classic.unity",
    "Assets/Scenes/Game_AR.unity",
  };

  const string FeatureTypeName = "UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature, Unity.XR.ARFoundation";

  static ProjectSetup()
  {
    if (SessionState.GetBool("ARTD.SetupChecked", false))
      return;

    SessionState.SetBool("ARTD.SetupChecked", true);
    EditorApplication.delayCall += () =>
    {
      EnsureBuildScenes();
      EnsureARBackgroundFeature();
      WarnAboutAndroidGraphics();
    };
  }

  [MenuItem("Tools/AR Tower Defense/Setup Project")]
  public static void SetupAll()
  {
    EnsureBuildScenes();
    EnsureARBackgroundFeature();
    ConfigureAndroid();
    AssetDatabase.SaveAssets();
    Debug.Log("[Setup] Project configured for the AR / Classic tower defense study.");
  }

  [MenuItem("Tools/AR Tower Defense/Open Study Data Folder")]
  public static void OpenDataFolder()
  {
    EditorUtility.RevealInFinder(DataStore.Folder);
  }

  static void EnsureBuildScenes()
  {
    EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
    bool ok = current.Length == Scenes.Length &&
        current.Select(s => s.path).SequenceEqual(Scenes) &&
        current.All(s => s.enabled);

    if (ok)
      return;

    EditorBuildSettings.scenes = Scenes.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
    Debug.Log("[Setup] Build scenes set to MainMenu, Game_Classic, Game_AR.");
  }

  // URP only draws the AR camera image if the renderer has the AR Background Renderer Feature.
  static void EnsureARBackgroundFeature()
  {
    Type featureType = Type.GetType(FeatureTypeName);

    if (featureType == null)
    {
      Debug.LogWarning("[Setup] ARBackgroundRendererFeature type not found; add it manually to the URP renderer.");
      return;
    }

    foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
    {
      string path = AssetDatabase.GUIDToAssetPath(guid);

      if (!path.StartsWith("Assets/"))
        continue;

      var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);

      if (data == null || data.rendererFeatures.Any(f => f != null && f.GetType() == featureType))
        continue;

      var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(featureType);
      feature.name = "ARBackgroundRendererFeature";
      AssetDatabase.AddObjectToAsset(feature, data);
      AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);

      // Same steps as URP's renderer inspector "Add Renderer Feature" button.
      SerializedObject so = new SerializedObject(data);
      SerializedProperty features = so.FindProperty("m_RendererFeatures");
      SerializedProperty map = so.FindProperty("m_RendererFeatureMap");

      features.arraySize++;
      features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
      map.arraySize++;
      map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
      so.ApplyModifiedProperties();

      EditorUtility.SetDirty(data);
      AssetDatabase.SaveAssets();
      Debug.Log("[Setup] Added AR Background Renderer Feature to " + path);
    }
  }

  static void ConfigureAndroid()
  {
    // ARCore is reliably supported with OpenGL ES 3 on every ARCore device.
    PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
    PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

    // ARCore needs the classic Activity entry point; with GameActivity the camera may deliver no frames.
    PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;

    PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
    PlayerSettings.allowedAutorotateToPortrait = false;
    PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
    PlayerSettings.allowedAutorotateToLandscapeLeft = true;
    PlayerSettings.allowedAutorotateToLandscapeRight = true;

    if ((int)PlayerSettings.Android.minSdkVersion < 29)
      PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29;

    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
    PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

    Debug.Log("[Setup] Android: OpenGLES3, landscape, ARM64/IL2CPP, min SDK 29.");
  }

  static void WarnAboutAndroidGraphics()
  {
    GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);

    if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) || (apis.Length > 0 && apis[0] == GraphicsDeviceType.Vulkan))
      Debug.Log("[Setup] Tip: run Tools > AR Tower Defense > Setup Project to use OpenGLES3 for ARCore builds.");
  }
}
