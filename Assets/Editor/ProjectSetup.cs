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

  // Adds Unity's own AR Session and XR Origin (Mobile AR) to the AR scene. The game uses them
  // instead of building the AR rig at runtime.
  [MenuItem("Tools/AR Tower Defense/Add AR Rig To Game_AR Scene")]
  public static void AddARRigToScene()
  {
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
      return;

    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game_AR.unity");

    if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARSession>() == null &&
        !EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session"))
    {
      Debug.LogError("[Setup] Could not create 'AR Session'. Add it manually: GameObject > XR > AR Session.");
      return;
    }

    if (UnityEngine.Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>() == null &&
        !EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)"))
    {
      Debug.LogError("[Setup] Could not create 'XR Origin (Mobile AR)'. Add it manually: GameObject > XR > XR Origin (Mobile AR).");
      return;
    }

    // The scene's original camera is replaced by the XR Origin's camera.
    foreach (GameObject root in scene.GetRootGameObjects())
    {
      if (root.GetComponent<Camera>() != null && root.GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>() == null)
        UnityEngine.Object.DestroyImmediate(root);
    }

    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    Debug.Log("[Setup] Game_AR now contains AR Session and XR Origin (Mobile AR).");
  }

  // Diagnostic: an AR scene with only Unity's AR Session + XR Origin and a cube, no game code.
  // It becomes the only build scene; "Restore Game Build Scenes" puts the game scenes back.
  [MenuItem("Tools/AR Tower Defense/Diagnostics/Create AR Test Scene (only scene in build)")]
  public static void CreateARTestScene()
  {
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
      return;

    var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
        UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);

    GameObject light = new GameObject("Directional Light");
    light.AddComponent<Light>().type = LightType.Directional;
    light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

    if (!EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session") ||
        !EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)"))
    {
      Debug.LogError("[Setup] Could not create the AR rig. Add GameObject > XR > AR Session and XR Origin (Mobile AR) manually.");
      return;
    }

    // A 10 cm cube half a metre in front of the starting position.
    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    cube.name = "Test Cube";
    cube.transform.position = new Vector3(0f, 0f, 0.5f);
    cube.transform.localScale = Vector3.one * 0.1f;

    const string path = "Assets/Scenes/ARTest.unity";
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
    EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
    Debug.Log("[Setup] ARTest scene created and set as the ONLY build scene. Build And Run, then use " +
        "Tools > AR Tower Defense > Diagnostics > Restore Game Build Scenes.");
  }

  [MenuItem("Tools/AR Tower Defense/Diagnostics/Restore Game Build Scenes")]
  public static void RestoreBuildScenes()
  {
    EnsureBuildScenes();
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
