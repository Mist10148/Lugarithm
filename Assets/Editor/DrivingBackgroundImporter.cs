using UnityEditor;
using UnityEngine;

/// <summary>
/// Enforces the driving-background import contract: every texture under
/// Resources/Driving/Background imports as a Point-filtered, uncompressed,
/// mipmap-free FullRect sprite at 24 PPU, matching the scale
/// SceneChunkVisualBuilder places the whole-scene chunks at.
/// A one-time startup sweep re-imports any texture that slipped in with default
/// settings (e.g. generated while the editor was already open).
/// </summary>
public class DrivingBackgroundImporter : AssetPostprocessor
{
    const string Root = "Assets/Resources/Driving/Background";
    const float  PPU  = 24f;

    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').StartsWith(Root)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode          = FilterMode.Point;
        importer.mipmapEnabled       = false;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize      = 4096;   // whole-scene chunks run up to ~2150 px wide

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }

    [InitializeOnLoadMethod]
    static void ReimportStaleTextures()
    {
        EditorApplication.delayCall += () =>
        {
            if (!AssetDatabase.IsValidFolder(Root)) return;
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, PPU) ||
                    importer.filterMode != FilterMode.Point ||
                    importer.mipmapEnabled ||
                    importer.textureType != TextureImporterType.Sprite)
                {
                    importer.SaveAndReimport();   // routes through OnPreprocessTexture
                }
            }
        };
    }
}
