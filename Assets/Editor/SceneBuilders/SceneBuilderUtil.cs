using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared plumbing for the scene builders: cameras, lights, event systems,
/// placeholder sprite loading, and SerializedObject-based wiring of private
/// [SerializeField] fields (throws on unknown fields so renames break the
/// build loudly instead of leaving silent null refs).
/// </summary>
public static class SceneBuilderUtil
{
    public const string ScenesDir = "Assets/Scenes";

    // -------------------------------------------------------------------------
    // Scene lifecycle

    public static Scene NewScene()
    {
        return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    public static void SaveScene(Scene scene, string sceneName)
    {
        Directory.CreateDirectory(ScenesDir);
        string path = $"{ScenesDir}/{sceneName}.unity";
        if (!EditorSceneManager.SaveScene(scene, path))
            throw new Exception($"Failed to save scene '{path}'");
        Debug.Log($"[Lugarithm] Built scene {path}");
    }

    // -------------------------------------------------------------------------
    // Common scene objects

    /// <summary>Orthographic URP camera tagged MainCamera, at z = −10.</summary>
    public static Camera CreateCamera2D(string name, Color background, float orthoSize,
                                        Rect? viewport = null)
    {
        var go = new GameObject(name);
        go.tag = "MainCamera";

        var cam = go.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = orthoSize;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = background;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        if (viewport.HasValue)
            cam.rect = viewport.Value;

        go.AddComponent<AudioListener>();
        cam.GetUniversalAdditionalCameraData(); // force-add the URP camera data component

        return cam;
    }

    /// <summary>Global 2D light so URP-lit sprites render at full brightness.</summary>
    public static Light2D CreateGlobalLight2D()
    {
        var go = new GameObject("Global Light 2D");
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
        return light;
    }

    /// <summary>
    /// EventSystem using the Input System UI module (the project runs with
    /// activeInputHandler = Input System only; the legacy StandaloneInputModule
    /// would throw). Binds the project-wide actions asset when present.
    /// </summary>
    public static GameObject CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        var module = go.AddComponent<InputSystemUIInputModule>();

        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        if (actions != null)
        {
            module.actionsAsset = actions;
            module.point       = ActionRef(actions, "UI/Point",       module.point);
            module.leftClick   = ActionRef(actions, "UI/Click",       module.leftClick);
            module.scrollWheel = ActionRef(actions, "UI/ScrollWheel", module.scrollWheel);
            module.move        = ActionRef(actions, "UI/Navigate",    module.move);
            module.submit      = ActionRef(actions, "UI/Submit",      module.submit);
            module.cancel      = ActionRef(actions, "UI/Cancel",      module.cancel);
        }
        // else: the module falls back to the package's DefaultInputActions.

        return go;
    }

    static InputActionReference ActionRef(InputActionAsset asset, string path,
                                          InputActionReference fallback)
    {
        InputAction action = asset.FindAction(path);
        return action != null ? InputActionReference.Create(action) : fallback;
    }

    // -------------------------------------------------------------------------
    // Placeholder sprites

    public static Sprite LoadPlaceholder(string name)
    {
        string path = $"Assets/Resources/Placeholders/{name}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new Exception($"Placeholder sprite '{path}' not found — run Lugarithm/Generate Placeholder Art first.");
        return sprite;
    }

    // -------------------------------------------------------------------------
    // Serialized field wiring

    /// <summary>
    /// Sets a (possibly private) serialized field on a component. Supports
    /// Object references, bool, int, float, string, Color, and Vector2.
    /// Throws when the field doesn't exist.
    /// </summary>
    public static void Wire(Component target, string fieldName, object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
            throw new Exception($"{target.GetType().Name} has no serialized field '{fieldName}'");

        switch (value)
        {
            case UnityEngine.Object obj: prop.objectReferenceValue = obj; break;
            case bool b:                 prop.boolValue   = b;   break;
            case int i:                  prop.intValue    = i;   break;
            case float f:                prop.floatValue  = f;   break;
            case string s:               prop.stringValue = s;   break;
            case Color c:                prop.colorValue  = c;   break;
            case Vector2 v:              prop.vector2Value = v;  break;
            case null:                   prop.objectReferenceValue = null; break;
            default:
                throw new Exception($"Wire: unsupported value type {value.GetType().Name} for '{fieldName}'");
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Fills a serialized array field with object references.</summary>
    public static void WireArray(Component target, string fieldName, UnityEngine.Object[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
            throw new Exception($"{target.GetType().Name} has no serialized field '{fieldName}'");

        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Fills a serialized string-array field.</summary>
    public static void WireStringArray(Component target, string fieldName, string[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
            throw new Exception($"{target.GetType().Name} has no serialized field '{fieldName}'");

        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).stringValue = values[i];

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
