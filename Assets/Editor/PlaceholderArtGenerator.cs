using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates flat-color placeholder sprite PNGs into Assets/Resources/Placeholders.
/// Regeneration overwrites pixel bytes only — the .meta GUIDs persist, so real
/// art later just replaces the PNG files and every reference survives.
/// Runtime-spawned objects load these via Resources.Load("Placeholders/...").
/// </summary>
public static class PlaceholderArtGenerator
{
    const string Dir = "Assets/Resources/Placeholders";

    // Palette
    static readonly Color JeepneyRed   = new Color(0.85f, 0.25f, 0.20f);
    static readonly Color JeepneyRoof  = new Color(0.95f, 0.90f, 0.80f);
    static readonly Color WindowDark   = new Color(0.16f, 0.22f, 0.28f);
    static readonly Color RoadGray     = new Color(0.26f, 0.26f, 0.30f);
    static readonly Color RoadDash     = new Color(0.85f, 0.80f, 0.55f);
    static readonly Color GrassGreen   = new Color(0.32f, 0.52f, 0.27f);
    static readonly Color CoinGold     = new Color(0.95f, 0.78f, 0.25f);
    static readonly Color CoinEdge     = new Color(0.70f, 0.52f, 0.12f);
    static readonly Color BillGreen    = new Color(0.72f, 0.85f, 0.66f);
    static readonly Color BillEdge     = new Color(0.35f, 0.50f, 0.32f);
    static readonly Color SignRed      = new Color(0.80f, 0.15f, 0.15f);
    static readonly Color PoleGray     = new Color(0.55f, 0.55f, 0.58f);
    static readonly Color DialDark     = new Color(0.10f, 0.11f, 0.14f);
    static readonly Color TickWhite    = new Color(0.92f, 0.92f, 0.88f);

    // -------------------------------------------------------------------------

    public static void GenerateAll()
    {
        Directory.CreateDirectory(Dir);

        // Generic shapes (tinted at runtime)
        Make("white_box", 8, 8, 64, (x, y, w, h) => Color.white);
        Make("circle", 24, 24, 64, (x, y, w, h) => InEllipse(x, y, w, h) ? Color.white : Color.clear);
        Make("diamond", 64, 32, 64, (x, y, w, h) => InDiamond(x, y, w, h) ? Color.white : Color.clear);
        Make("triangle", 16, 16, 64, (x, y, w, h) => InUpTriangle(x, y, w, h) ? Color.white : Color.clear);

        // Manual-mode world
        Make("jeepney_top", 32, 64, 32, JeepneyTop);
        Make("peep", 16, 24, 32, Peep);
        Make("road_tile", 64, 64, 64, RoadTile);
        Make("grass_tile", 64, 64, 64, GrassTile);
        Make("stop_sign", 24, 48, 32, StopSign);

        // Automation-mode world
        Make("iso_jeepney", 48, 32, 64, IsoJeepney);

        // HUD bits
        Make("dial", 96, 96, 64, Dial);
        Make("needle", 6, 48, 64, (x, y, w, h) => Color.white, pivot: new Vector2(0.5f, 0.08f));
        Make("coin", 24, 24, 64, Coin);
        Make("bill", 36, 22, 64, Bill);

        AssetDatabase.Refresh();
        Debug.Log("[Lugarithm] Placeholder art generated in " + Dir);
    }

    // -------------------------------------------------------------------------
    // Painters

    static Color JeepneyTop(int x, int y, int w, int h)
    {
        // Long body with a lighter roof stripe and a windshield near the top.
        bool border = x == 0 || y == 0 || x == w - 1 || y == h - 1;
        if (border) return WindowDark;
        if (y > h - 9 && y < h - 3 && x > 3 && x < w - 4) return WindowDark;  // windshield
        if (x > 7 && x < w - 8 && y > 6 && y < h - 12) return JeepneyRoof;    // roof stripe
        return JeepneyRed;
    }

    static Color Peep(int x, int y, int w, int h)
    {
        // Head (top circle) + body (rounded column). White so runtime can tint.
        float cx = w * 0.5f, headCy = h * 0.78f, headR = w * 0.30f;
        float dx = x - cx, dy = y - headCy;
        if (dx * dx + dy * dy <= headR * headR) return Color.white;
        if (y < h * 0.62f && y > 1 && x > w * 0.22f && x < w * 0.78f) return Color.white;
        return Color.clear;
    }

    static Color RoadTile(int x, int y, int w, int h)
    {
        // Asphalt with a center dashed line.
        bool dash = x >= w / 2 - 2 && x <= w / 2 + 1 && (y / 8) % 2 == 0;
        return dash ? RoadDash : RoadGray;
    }

    static Color GrassTile(int x, int y, int w, int h)
    {
        // Deterministic speckle so tiles don't look perfectly flat.
        bool speck = ((x * 7 + y * 13) % 31) == 0;
        return speck ? GrassGreen * 0.85f + Color.black * 0.15f : GrassGreen;
    }

    static Color StopSign(int x, int y, int w, int h)
    {
        // Sign board on top of a pole.
        if (y > h * 0.55f)
        {
            bool border = x < 2 || x > w - 3 || y > h - 3 || y < h * 0.55f + 2;
            return border ? Color.white : SignRed;
        }
        if (x >= w / 2 - 1 && x <= w / 2 + 1) return PoleGray;
        return Color.clear;
    }

    static Color IsoJeepney(int x, int y, int w, int h)
    {
        // Rounded blob with a roof highlight — the facing arrow is a child sprite.
        float nx = (x - w * 0.5f) / (w * 0.46f);
        float ny = (y - h * 0.5f) / (h * 0.42f);
        float d = nx * nx + ny * ny;
        if (d > 1f) return Color.clear;
        if (d < 0.45f) return JeepneyRoof;
        return JeepneyRed;
    }

    static Color Dial(int x, int y, int w, int h)
    {
        // Upper-half gauge with tick marks.
        float cx = w * 0.5f, cy = h * 0.18f;
        float dx = x - cx, dy = y - cy;
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        float maxR = w * 0.48f;

        if (dy < 0 || r > maxR) return Color.clear;
        if (r > maxR - 3f) return TickWhite;

        // Ticks every 30° on the upper semicircle.
        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        for (int a = 0; a <= 180; a += 30)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(angle, a)) < 2.5f && r > maxR * 0.78f)
                return TickWhite;
        }

        return DialDark;
    }

    static Color Coin(int x, int y, int w, int h)
    {
        if (!InEllipse(x, y, w, h)) return Color.clear;
        float cx = w * 0.5f, cy = h * 0.5f;
        float dx = (x - cx) / (w * 0.5f), dy = (y - cy) / (h * 0.5f);
        return dx * dx + dy * dy > 0.62f ? CoinEdge : CoinGold;
    }

    static Color Bill(int x, int y, int w, int h)
    {
        bool border = x < 2 || y < 2 || x > w - 3 || y > h - 3;
        return border ? BillEdge : BillGreen;
    }

    // -------------------------------------------------------------------------
    // Shape helpers

    static bool InEllipse(int x, int y, int w, int h)
    {
        float dx = (x - w * 0.5f + 0.5f) / (w * 0.5f);
        float dy = (y - h * 0.5f + 0.5f) / (h * 0.5f);
        return dx * dx + dy * dy <= 1f;
    }

    static bool InDiamond(int x, int y, int w, int h)
    {
        float dx = Mathf.Abs(x - w * 0.5f + 0.5f) / (w * 0.5f);
        float dy = Mathf.Abs(y - h * 0.5f + 0.5f) / (h * 0.5f);
        return dx + dy <= 1f;
    }

    static bool InUpTriangle(int x, int y, int w, int h)
    {
        // Apex at top-center, base at the bottom.
        float t = (float)y / (h - 1);                       // 0 bottom → 1 top
        float halfWidth = (1f - t) * (w * 0.5f);
        return Mathf.Abs(x - w * 0.5f + 0.5f) <= halfWidth;
    }

    // -------------------------------------------------------------------------
    // PNG writing + import settings

    static void Make(string name, int width, int height, int ppu,
                     Func<int, int, int, int, Color> painter, Vector2? pivot = null)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = painter(x, y, width, height);

        tex.SetPixels(pixels);
        tex.Apply();

        string path = $"{Dir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode          = FilterMode.Point;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled       = false;

        // FullRect mesh so SpriteRenderer Tiled draw mode works (ground, bars).
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        if (pivot.HasValue)
        {
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot     = pivot.Value;
        }
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }
}
