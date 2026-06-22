using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Project-owned TMP font asset for the Sprout Lands-inspired main menu.
/// The source TTF is copied into the repo and converted to a persistent
/// TMP_FontAsset so editor-built scenes can serialize a stable reference.
/// </summary>
public static class SproutLandsMenuFont
{
    const string FontDir = "Assets/UI/Fonts";
    const string TtfPath = FontDir + "/SproutLandsPixelFontSource.ttf";
    const string AssetPath = FontDir + "/SproutLandsPixelFont.asset";

    [InitializeOnLoadMethod]
    static void BootstrapOnLoad()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath) != null)
            return;

        if (File.Exists(TtfPath))
        {
            EnsureFontAsset();
            EditorApplication.delayCall += BuildPipelineEntry.BuildMainMenu;
        }
    }

    [MenuItem("Lugarithm/Generate Sprout Lands Menu Font")]
    public static void Generate()
    {
        EnsureFontAsset();
    }

    public static TMP_FontAsset EnsureFontAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
        if (existing != null)
        {
            Debug.Log("[Lugarithm] Reusing existing Sprout Lands menu font at " + AssetPath);
            return existing;
        }

        Directory.CreateDirectory(FontDir);

        if (!File.Exists(TtfPath))
        {
            Debug.LogWarning($"[Lugarithm] Missing main-menu font source at '{TtfPath}'. Falling back to the default TMP font.");
            return null;
        }

        AssetDatabase.ImportAsset(TtfPath, ImportAssetOptions.ForceUpdate);

        var ttf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (ttf == null)
        {
            Debug.LogWarning($"[Lugarithm] Failed to import '{TtfPath}' as a Font asset.");
            return null;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(ttf);
        if (fontAsset == null)
        {
            Debug.LogWarning("[Lugarithm] TMP_FontAsset.CreateFontAsset returned null for the Sprout Lands menu font.");
            return null;
        }

        fontAsset.name = "SproutLandsPixelFont";

        Debug.Log("[Lugarithm] Creating Sprout Lands menu font asset at " + AssetPath);
        AssetDatabase.CreateAsset(fontAsset, AssetPath);

        if (fontAsset.atlasTexture != null)
        {
            fontAsset.atlasTexture.name = "SproutLandsPixelFont Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "SproutLandsPixelFont Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Lugarithm] Created Sprout Lands menu font at " + AssetPath);

        return fontAsset;
    }
}
