using UnityEditor;
using UnityEngine;

/// <summary>
/// Enforces the driving-background import contract: every texture under
/// Resources/Driving/Background imports as a Point-filtered, compressed,
/// mipmap-free FullRect sprite at 24 PPU, matching the scale
/// SceneChunkVisualBuilder places the whole-scene chunks at. Compression
/// matters: uncompressed the ~1250 px templates are ~6 MB each in memory and
/// their first-render upload is a visible mid-drive hitch.
/// A one-time startup sweep re-imports any texture that slipped in with default
/// settings (e.g. generated while the editor was already open).
/// </summary>
public class DrivingBackgroundImporter : AssetPostprocessor
{
    const string Root = "Assets/Resources/Driving/Background";
    const float  PPU  = 24f;
    const int    MaxSize = 4096;   // whole-scene chunks run up to ~2150 px wide
    const TextureImporterCompression Compression = TextureImporterCompression.Compressed;

    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').StartsWith(Root)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PPU;
        importer.filterMode          = FilterMode.Point;
        importer.mipmapEnabled       = false;
        importer.textureCompression  = Compression;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize      = MaxSize;

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

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                if (!Mathf.Approximately(importer.spritePixelsPerUnit, PPU) ||
                    importer.filterMode != FilterMode.Point ||
                    importer.mipmapEnabled ||
                    importer.textureType != TextureImporterType.Sprite ||
                    importer.textureCompression != Compression ||
                    importer.maxTextureSize != MaxSize ||
                    settings.spriteMeshType != SpriteMeshType.FullRect)
                {
                    importer.SaveAndReimport();   // routes through OnPreprocessTexture
                }
            }
        };
    }
}
