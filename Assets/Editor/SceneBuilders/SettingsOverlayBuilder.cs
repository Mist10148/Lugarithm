using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the single universal Settings overlay as a prefab at
/// <c>Assets/Resources/UI/SettingsOverlay.prefab</c>. This prefab is the one
/// authoritative Settings presentation for the whole game — Bootstrap
/// instantiates it (persistent, DontDestroyOnLoad via
/// <see cref="UniversalSettingsManager"/>) and any scene played directly in the
/// editor lazily loads the same prefab through
/// <see cref="UniversalSettingsManager.Ensure"/>. Because there is exactly one
/// build output, every entry point renders an identical panel.
///
/// The pixel font is forced on via <see cref="UIFactory.FontOverride"/> for the
/// duration of the build (and re-asserted after), so the overlay's font no
/// longer depends on which scene happened to be constructing it — the historic
/// cause of the "font differs/disappears by scene" bug.
/// </summary>
public static class SettingsOverlayBuilder
{
    public const string PrefabPath = "Assets/Resources/UI/SettingsOverlay.prefab";

    // Above the Almanac overlay (200), below the scene-transition fade (1000).
    const int CanvasSortOrder = 300;

    public static GameObject Build()
    {
        TMP_FontAsset previousFont = UIFactory.FontOverride;
        UIFactory.FontOverride = SproutLandsMenuFont.EnsureFontAsset();
        try
        {
            var root = new GameObject("UniversalSettings");
            var manager = root.AddComponent<UniversalSettingsManager>();

            Canvas canvas = UIFactory.CreateCanvas("SettingsCanvas", CanvasSortOrder);
            canvas.transform.SetParent(root.transform, false);

            SettingsPanel panel = SettingsPanelBuilder.Build(canvas.transform);
            SceneBuilderUtil.Wire(manager, "panel", panel);

            // Re-assert the pixel font on every label (font only — no color or
            // sprite changes, so the selector/window art is untouched).
            ForcePixelFont(root.transform);

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            Debug.Log("[Lugarithm] Built Settings overlay prefab at " + PrefabPath);
            return prefab;
        }
        finally
        {
            UIFactory.FontOverride = previousFont;
        }
    }

    static void ForcePixelFont(Transform root)
    {
        TMP_FontAsset pixelFont = SproutLandsMenuFont.EnsureFontAsset();
        if (pixelFont == null) return;

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            text.font = pixelFont;
    }
}
