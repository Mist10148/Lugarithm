using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Imports generated vehicle sheets without optional package dependencies.</summary>
public sealed class VehicleSpriteImporter : AssetPostprocessor
{
    const string Root = "Assets/Resources/Vehicles/";
    const int SheetWidth = 1536;
    const int SheetHeight = 1024;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root) || assetPath.Contains("_chroma")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = assetPath.Contains("traffic") ? 192f
                                    : assetPath.Contains("smoke") ? 128f : 96f;
#pragma warning disable 0618
        importer.spritesheet = assetPath.Contains("player_jeepney") ? PlayerSlices()
                           : assetPath.Contains("filipino_traffic") ? GridSlices(4, 2, "traffic")
                           : GridSlices(6, 3, "smoke");
#pragma warning restore 0618
    }

    static SpriteMetaData[] GridSlices(int columns, int rows, string prefix)
    {
        var result = new List<SpriteMetaData>();
        float w = SheetWidth / (float)columns;
        float h = SheetHeight / (float)rows;
        for (int row = 0; row < rows; row++)
        for (int col = 0; col < columns; col++)
            result.Add(Make($"{prefix}_{row:D2}_{col:D2}",
                new Rect(Mathf.Round(col * w), Mathf.Round(SheetHeight - (row + 1) * h),
                         Mathf.Round(w), Mathf.Round(h))));
        return result.ToArray();
    }

    static SpriteMetaData[] PlayerSlices()
    {
        var result = new List<SpriteMetaData>();
        AddRow(result, "idle", 100, new[] { 225, 425, 625 }, 180, 180);
        AddRow(result, "drive", 290, new[] { 225, 425, 625, 810, 1000, 1185 }, 180, 180);
        AddRow(result, "accelerate", 500, new[] { 225, 425, 625, 810, 1000, 1185 }, 180, 180);
        AddRow(result, "brake", 690, new[] { 225, 425, 625, 810, 1000, 1185 }, 180, 180);
        AddRow(result, "turn_left", 885, new[] { 225, 390, 550, 680 }, 190, 210);
        AddRow(result, "turn_right", 885, new[] { 850, 1000, 1150, 1275 }, 190, 210);
        return result.ToArray();
    }

    static void AddRow(List<SpriteMetaData> result, string prefix, int centerYFromTop,
                       int[] centersX, int width, int height)
    {
        for (int i = 0; i < centersX.Length; i++)
        {
            float x = Mathf.Clamp(centersX[i] - width * 0.5f, 0f, SheetWidth - width);
            float yTop = Mathf.Clamp(centerYFromTop - height * 0.5f, 0f, SheetHeight - height);
            result.Add(Make($"{prefix}_{i:D2}",
                new Rect(x, SheetHeight - yTop - height, width, height)));
        }
    }

    static SpriteMetaData Make(string name, Rect rect)
    {
        return new SpriteMetaData
        {
            name = name,
            rect = rect,
            pivot = new Vector2(0.5f, 0.5f),
            alignment = (int)SpriteAlignment.Center,
        };
    }
}
