using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates a monospace TMP font asset (for the "The Farmer Was Replaced"
/// code editor) by copying a system monospace TTF into the project and building
/// a dynamic SDF font asset at Assets/Resources/Fonts/CodeMono.asset. Wholly
/// defensive — any failure is logged and swallowed so the scene build never
/// breaks; the editors simply fall back to the default font.
/// </summary>
public static class MonospaceFontGenerator
{
    const string FontDir   = "Assets/Resources/Fonts";
    const string TtfPath   = FontDir + "/CodeMonoSource.ttf";
    const string AssetPath = FontDir + "/CodeMono.asset";

    static readonly string[] SystemCandidates =
    {
        @"C:\Windows\Fonts\consola.ttf",
        @"C:\Windows\Fonts\cour.ttf",
        @"C:\Windows\Fonts\lucon.ttf",
        "/System/Library/Fonts/Menlo.ttc",
        "/Library/Fonts/Courier New.ttf",
    };

    [MenuItem("Lugarithm/Generate Monospace Font")]
    public static void Generate()
    {
        try
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath) != null)
                return; // already generated

            Directory.CreateDirectory(FontDir);

            if (!File.Exists(TtfPath))
            {
                string src = SystemCandidates.FirstOrDefault(File.Exists);
                if (src == null)
                {
                    Debug.LogWarning("[Lugarithm] No system monospace font found — code editor uses the default font.");
                    return;
                }
                File.Copy(src, TtfPath, true);
                AssetDatabase.ImportAsset(TtfPath, ImportAssetOptions.ForceUpdate);
            }

            var ttf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (ttf == null)
            {
                Debug.LogWarning("[Lugarithm] Monospace TTF import failed — using the default font.");
                return;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(ttf);
            if (fontAsset == null)
            {
                Debug.LogWarning("[Lugarithm] TMP_FontAsset.CreateFontAsset returned null.");
                return;
            }
            fontAsset.name = "CodeMono";

            AssetDatabase.CreateAsset(fontAsset, AssetPath);

            // Persist the atlas texture + material as sub-assets so the font is self-contained.
            if (fontAsset.atlasTexture != null)
            {
                fontAsset.atlasTexture.name = "CodeMono Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }
            if (fontAsset.material != null)
            {
                fontAsset.material.name = "CodeMono Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Lugarithm] Monospace code font generated at " + AssetPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Lugarithm] Monospace font generation skipped: " + e.Message);
        }
    }
}
