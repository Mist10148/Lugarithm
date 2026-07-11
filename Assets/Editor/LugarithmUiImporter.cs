using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Enforces crisp, reusable nine-slice imports for the generated UI kit.</summary>
public sealed class LugarithmUiImporter : AssetPostprocessor
{
    const string Root = "Assets/UI/Sprites/LugarithmUi/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        string name = Path.GetFileNameWithoutExtension(assetPath);
        if (assetPath.Contains("/Settings/") || assetPath.Contains("/TutorialHud/") ||
            assetPath.Contains("/TutorialMinigames/") || assetPath.Contains("/JeepneyHud/") ||
            assetPath.Contains("/Journal/"))
        {
            importer.spritePixelsPerUnit = 1f;
            importer.spriteBorder = Vector4.zero;
            if (assetPath.Contains("/Journal/Parts/") &&
                (name.Contains("card") || name.Contains("row") || name.Contains("ribbon") ||
                 name.Contains("frame") || name.Contains("input")))
                importer.spriteBorder = new Vector4(18, 18, 18, 18);
            return;
        }
        if (assetPath.Contains("/Icons/"))
        {
            importer.spriteBorder = Vector4.zero;
            return;
        }
        int border = name.Contains("window") || name.Contains("panel") || name.Contains("dialogue") ? 28
                   : name.Contains("button") || name.Contains("input") || name.Contains("frame") ? 18
                   : name.Contains("tab") || name.Contains("segmented") ? 14 : 8;
        importer.spriteBorder = new Vector4(border, border, border, border);
    }
}
