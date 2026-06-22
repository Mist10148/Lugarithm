using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports the copied Sprout Lands UI PNGs as point-filtered sprites and
/// slices the sheets into fixed grids so scene builders can reference named
/// sub-sprites directly.
/// </summary>
public sealed class SproutLandsUiPostprocessor : AssetPostprocessor
{
    const string Root = "Assets/UI/Sprites/SproutLands/Sheets/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.spritePixelsPerUnit = 32f;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        string file = Path.GetFileName(assetPath);
        switch (file)
        {
            case "UI Big Play Button.png":
                SetGrid(importer, 96, 32, 2, 2, "BigPlay");
                break;
            case "Setting menu.png":
                SetGrid(importer, 128, 144, 2, 1, "SettingsMenu");
                break;
            case "Small Square Buttons.png":
                SetGrid(importer, 32, 32, 1, 4, "SmallSquare");
                break;
            case "Square Buttons 19x26.png":
                SetGrid(importer, 32, 32, 4, 3, "Square19");
                break;
            case "Square Buttons 26x19.png":
                SetGrid(importer, 32, 32, 3, 4, "Square26x19");
                break;
            case "Square Buttons 26x26.png":
                SetGrid(importer, 32, 32, 3, 6, "Square26");
                break;
            case "UI Settings Buttons.png":
                SetGrid(importer, 32, 24, 4, 10, "UISettings");
                break;
            case "All Icons.png":
                SetGrid(importer, 16, 16, 18, 3, "Icons");
                break;
            case "dialog box character finished talking click to continue indicator - spritesheet .png":
                SetGrid(importer, 16, 16, 7, 1, "DialogContinue");
                break;
            default:
                importer.spriteImportMode = SpriteImportMode.Single;
                break;
        }
    }

    static void SetGrid(TextureImporter importer, int cellWidth, int cellHeight, int columns, int rows, string prefix)
    {
        importer.spriteImportMode = SpriteImportMode.Multiple;

        var sprites = new SpriteMetaData[columns * rows];
        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                sprites[index++] = new SpriteMetaData
                {
                    name = $"{prefix}_{row}_{column}",
                    rect = new Rect(column * cellWidth, (rows - 1 - row) * cellHeight, cellWidth, cellHeight),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                };
            }
        }

        importer.spritesheet = sprites;
    }
}
