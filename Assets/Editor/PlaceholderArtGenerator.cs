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

    // Isometric tilemap palette (Automation world)
    static readonly Color PathTan      = new Color(0.55f, 0.46f, 0.33f);
    static readonly Color PathTanDark  = new Color(0.46f, 0.38f, 0.27f);
    static readonly Color WaterBlue    = new Color(0.28f, 0.52f, 0.68f);
    static readonly Color WaterRipple  = new Color(0.44f, 0.67f, 0.81f);
    static readonly Color HedgeTop     = new Color(0.24f, 0.42f, 0.26f);
    static readonly Color HedgeTopHi   = new Color(0.32f, 0.53f, 0.33f);
    static readonly Color HedgeLeft    = new Color(0.17f, 0.31f, 0.19f);
    static readonly Color HedgeRight   = new Color(0.11f, 0.22f, 0.13f);
    static readonly Color StartBlue    = new Color(0.35f, 0.55f, 0.95f);
    static readonly Color DestGreen    = new Color(0.35f, 0.85f, 0.45f);
    static readonly Color StopAmber    = new Color(0.95f, 0.75f, 0.25f);

    // Top-down level palette
    static readonly Color TDGrass       = new Color(0.38f, 0.58f, 0.30f);
    static readonly Color TDGrassDark   = new Color(0.30f, 0.48f, 0.24f);
    static readonly Color TDPath        = new Color(0.72f, 0.62f, 0.45f);
    static readonly Color TDPathDark    = new Color(0.60f, 0.50f, 0.36f);
    static readonly Color TDWall       = new Color(0.45f, 0.38f, 0.32f);
    static readonly Color TDWallTop     = new Color(0.55f, 0.48f, 0.40f);
    static readonly Color TDWallCap     = new Color(0.62f, 0.55f, 0.46f);
    static readonly Color TDWater       = new Color(0.30f, 0.50f, 0.72f);
    static readonly Color TDWaterLight  = new Color(0.42f, 0.62f, 0.82f);
    static readonly Color TDPlayerBody  = new Color(0.25f, 0.55f, 0.85f);
    static readonly Color TDPlayerHead  = new Color(0.85f, 0.72f, 0.55f);
    static readonly Color TDPlayerHair = new Color(0.30f, 0.22f, 0.15f);
    static readonly Color TDNpcBody    = new Color(0.80f, 0.30f, 0.30f);
    static readonly Color TDNpcHead    = new Color(0.85f, 0.72f, 0.55f);
    static readonly Color TDNpcHair    = new Color(0.20f, 0.15f, 0.10f);
    static readonly Color TDJeepStop   = new Color(0.85f, 0.75f, 0.20f);
    static readonly Color TDJeepStopDark = new Color(0.60f, 0.50f, 0.15f);
    static readonly Color TDInteract    = new Color(1.0f, 0.90f, 0.20f);

    // Roadside building palette (Manual drive street — Filipino heritage mix)
    static readonly Color BldgOutline   = new Color(0.18f, 0.14f, 0.12f);
    static readonly Color RoofTerra     = new Color(0.74f, 0.36f, 0.28f);
    static readonly Color RoofTerraDk   = new Color(0.62f, 0.29f, 0.22f);
    static readonly Color RoofGalv      = new Color(0.60f, 0.62f, 0.64f);
    static readonly Color RoofGalvDk    = new Color(0.49f, 0.51f, 0.54f);
    static readonly Color WoodBrown     = new Color(0.55f, 0.40f, 0.26f);
    static readonly Color WoodDark      = new Color(0.40f, 0.28f, 0.18f);
    static readonly Color StoneGray     = new Color(0.64f, 0.62f, 0.57f);
    static readonly Color ThatchTan     = new Color(0.80f, 0.68f, 0.40f);
    static readonly Color ThatchDark    = new Color(0.65f, 0.53f, 0.30f);
    static readonly Color AwningRed     = new Color(0.82f, 0.30f, 0.28f);
    static readonly Color AwningOrange  = new Color(0.92f, 0.55f, 0.18f);
    static readonly Color AwningStripe  = new Color(0.95f, 0.92f, 0.86f);
    static readonly Color ChapelWhite   = new Color(0.92f, 0.90f, 0.84f);
    static readonly Color CapizPane     = new Color(0.86f, 0.86f, 0.70f);
    static readonly Color DoorBrown     = new Color(0.36f, 0.24f, 0.16f);
    static readonly Color CounterTan    = new Color(0.72f, 0.56f, 0.36f);

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

        // Isometric tilemap tiles (64×32 diamond footprint; wall is a raised block)
        Make("iso_ground_grass", 64, 32, 64, (x, y, w, h) => IsoGround(x, y, w, h, GrassGreen, 0));
        Make("iso_ground_path",  64, 32, 64, (x, y, w, h) => IsoGround(x, y, w, h, PathTan, 1));
        Make("iso_water",        64, 32, 64, (x, y, w, h) => IsoGround(x, y, w, h, WaterBlue, 2));
        Make("iso_wall",         64, 48, 64, IsoWall, pivot: new Vector2(0.5f, 32f / 48f));
        Make("iso_start",        64, 32, 64, (x, y, w, h) => IsoMarker(x, y, w, h, StartBlue));
        Make("iso_dest",         64, 32, 64, (x, y, w, h) => IsoMarker(x, y, w, h, DestGreen));
        Make("iso_stop",         64, 32, 64, (x, y, w, h) => IsoMarker(x, y, w, h, StopAmber));

        // HUD bits
        Make("dial", 96, 96, 64, Dial);
        Make("needle", 6, 48, 64, (x, y, w, h) => Color.white, pivot: new Vector2(0.5f, 0.08f));
        Make("coin", 24, 24, 64, Coin);
        Make("bill", 36, 22, 64, Bill);

        // Top-down level tiles
        Make("td_grass", 16, 16, 64, TDGrassPainter);
        Make("td_path",  16, 16, 64, TDPathPainter);
        Make("td_wall",  16, 16, 64, TDWallPainter);
        Make("td_water", 16, 16, 64, TDWaterPainter);
        Make("td_jeep_stop", 16, 16, 64, TDJeepStopPainter);
        Make("td_interaction", 16, 16, 64, (x, y, w, h) => TDInteractionPainter(x, y, w, h));

        // Top-down characters
        Make("td_player", 16, 24, 64, TDPlayerPainter, pivot: new Vector2(0.5f, 0.25f));
        Make("td_npc",    16, 24, 64, TDNpcPainter,    pivot: new Vector2(0.5f, 0.25f));

        // Roadside buildings (Manual drive street — heritage mix). Top-down roof with
        // a "front" strip along the bottom edge; the spawner rotates the front to the road.
        Make("bldg_bahay_bato", 80, 84, 16, BahayBato);
        Make("bldg_sari_sari",  64, 64, 16, SariSari);
        Make("bldg_carinderia", 64, 64, 16, Carinderia);
        Make("bldg_nipa",       56, 60, 16, NipaHut);
        Make("bldg_chapel",     64, 92, 16, Chapel);

        // Ambient street person (non-passenger) — white so the runtime tints it a muted color.
        Make("townsfolk", 16, 24, 32, Townsfolk, pivot: new Vector2(0.5f, 0.2f));

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

    // -------------------------------------------------------------------------
    // Isometric tiles

    /// <summary>A diamond-top ground tile (grass / path / water variants).</summary>
    static Color IsoGround(int x, int y, int w, int h, Color baseCol, int variant)
    {
        float cx = w * 0.5f, cy = h * 0.5f;
        float dx = Mathf.Abs(x - cx + 0.5f) / (w * 0.5f);
        float dy = Mathf.Abs(y - cy + 0.5f) / (h * 0.5f);
        float d = dx + dy;
        if (d > 1f) return Color.clear;

        Color c = baseCol;
        switch (variant)
        {
            case 0: if (((x * 7 + y * 13) % 31) == 0) c = baseCol * 0.82f;          break; // grass speckle
            case 1: if (((x * 5 + y * 11) % 23) == 0) c = PathTanDark;              break; // path pebbles
            case 2: if (((y / 2) % 3 == 0) && ((x + y) % 7 < 2) && d < 0.85f) c = WaterRipple; break; // ripples
        }
        if (d > 0.88f) c *= 0.72f;                                                          // edge rim

        return new Color(c.r, c.g, c.b, 1f);
    }

    /// <summary>A path tile with a centered round marker (start / dest / stop).</summary>
    static Color IsoMarker(int x, int y, int w, int h, Color marker)
    {
        Color ground = IsoGround(x, y, w, h, PathTan, 1);
        if (ground.a < 0.5f) return Color.clear;

        float cx = w * 0.5f, cy = h * 0.5f;
        float ddx = x - cx + 0.5f;
        float ddy = (y - cy + 0.5f) * 2f;            // un-squash the iso so the dot is round
        float rr = ddx * ddx + ddy * ddy;
        float r = w * 0.22f;

        if (rr <= r * r)                     return new Color(marker.r, marker.g, marker.b, 1f);
        if (rr <= (r * 1.3f) * (r * 1.3f))   return new Color(marker.r * 0.7f, marker.g * 0.7f, marker.b * 0.7f, 1f);
        return ground;
    }

    /// <summary>A raised hedge/wall block: diamond cap + two shaded side faces.</summary>
    static Color IsoWall(int x, int y, int w, int h)
    {
        float cx = w * 0.5f;
        const float topHalfH = 16f;
        float topHalfW = w * 0.5f;
        float topCy = h - topHalfH;                  // cap centered in the top 32px

        float ax = Mathf.Abs(x - cx + 0.5f) / topHalfW;       // 0 center … 1 edge
        float ay = Mathf.Abs(y - topCy + 0.5f) / topHalfH;

        // Diamond cap
        if (ax + ay <= 1f)
        {
            Color c = (y > topCy) ? HedgeTopHi : HedgeTop;
            if (((x * 7 + y * 5) % 17) == 0) c *= 0.86f;
            return new Color(c.r, c.g, c.b, 1f);
        }

        // Side faces hang from the cap's lower silhouette down to the base
        if (ax <= 1f)
        {
            float yEdge = topCy - topHalfH * (1f - ax);
            if (y < yEdge && y >= 2)
            {
                Color side = (x < cx) ? HedgeLeft : HedgeRight;
                if ((y % 4) == 0) side *= 0.9f;
                return new Color(side.r, side.g, side.b, 1f);
            }
        }

        return Color.clear;
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
    // Top-down level painters

    static Color TDGrassPainter(int x, int y, int w, int h)
    {
        // Green with deterministic speckle.
        bool speck = ((x * 7 + y * 13) % 31) == 0;
        bool edge  = x == 0 || y == 0 || x == w - 1 || y == h - 1;
        if (speck) return TDGrassDark;
        if (edge) return TDGrassDark * 0.95f;
        return TDGrass;
    }

    static Color TDPathPainter(int x, int y, int w, int h)
    {
        // Tan/dirt path with pebble texture.
        bool pebble = ((x * 5 + y * 11) % 23) == 0;
        bool edge   = x == 0 || y == 0 || x == w - 1 || y == h - 1;
        if (pebble) return TDPathDark;
        if (edge) return TDPathDark * 0.95f;
        return TDPath;
    }

    static Color TDWallPainter(int x, int y, int w, int h)
    {
        // Building wall: darker sides on bottom-left, lighter cap on top-right.
        bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
        // Highlight on top row and right column (fake 3D)
        if (y == 0 || x == w - 1) return TDWallCap;
        // Shadow on bottom row and left column
        if (y == h - 1 || x == 0) return TDWall * 0.7f;
        if (edge) return TDWallTop;
        // Brick pattern
        bool mortar = (y % 4 == 0) || (x % 6 == 0 && (y / 4) % 2 == 0) || (x % 6 == 3 && (y / 4) % 2 == 1);
        if (mortar) return TDWall * 0.65f;
        return TDWall;
    }

    static Color TDWaterPainter(int x, int y, int w, int h)
    {
        // Blue water with ripple.
        bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
        if (edge) return TDWater * 0.8f;
        // Horizontal wave lines
        bool wave = ((x + y) % 6 < 2) && (y % 4 == 0);
        if (wave) return TDWaterLight;
        return TDWater;
    }

    static Color TDJeepStopPainter(int x, int y, int w, int h)
    {
        // Amber/yellow marker on a dark base — indicates jeep boarding.
        float cx = w * 0.5f, cy = h * 0.5f;
        float dx = Mathf.Abs(x - cx + 0.5f) / (w * 0.5f);
        float dy = Mathf.Abs(y - cy + 0.5f) / (h * 0.5f);
        float d = dx + dy;
        bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
        if (edge) return TDJeepStopDark;
        // Center diamond marker
        if (d <= 0.45f) return TDJeepStop;
        if (d <= 0.6f) return TDJeepStopDark;
        return TDPath * 0.85f;
    }

    static Color TDInteractionPainter(int x, int y, int w, int h)
    {
        // Yellow exclamation mark on transparent background.
        float cx = w * 0.5f;
        if (x < 6 || x > 9 || y < 2 || y > 13) return Color.clear;
        // Exclamation mark body (vertical bar)
        if (x >= 6 && x <= 9 && y >= 3 && y <= 9) return TDInteract;
        // Exclamation dot
        if (x >= 6 && x <= 9 && y >= 11 && y <= 12) return TDInteract;
        return Color.clear;
    }

    static Color TDPlayerPainter(int x, int y, int w, int h)
    {
        // Top-down character: body (blue shirt), head, hair.
        float cx = w * 0.5f;
        bool border = x < 2 || x > w - 3 || y < 2 || y > h - 3;
        if (border) return Color.clear;

        // Head (top area, circular-ish)
        float headCx = cx, headCy = h * 0.78f, headR = w * 0.32f;
        float hdx = x - headCx, hdy = y - headCy;
        bool inHead = hdx * hdx + hdy * hdy <= headR * headR;

        if (inHead)
        {
            // Hair on top portion of head
            if (hdy > headR * 0.15f) return TDPlayerHair;
            return TDPlayerHead; // face
        }

        // Body
        if (y >= 4 && y < h * 0.62f && x >= 4 && x < w - 4) return TDPlayerBody;

        return Color.clear;
    }

    static Color TDNpcPainter(int x, int y, int w, int h)
    {
        // Same shape as player but red body, darker hair.
        float cx = w * 0.5f;
        bool border = x < 2 || x > w - 3 || y < 2 || y > h - 3;
        if (border) return Color.clear;

        float headCx = cx, headCy = h * 0.78f, headR = w * 0.32f;
        float hdx = x - headCx, hdy = y - headCy;
        bool inHead = hdx * hdx + hdy * hdy <= headR * headR;

        if (inHead)
        {
            if (hdy > headR * 0.15f) return TDNpcHair;
            return TDNpcHead;
        }

        if (y >= 4 && y < h * 0.62f && x >= 4 && x < w - 4) return TDNpcBody;

        return Color.clear;
    }

    // -------------------------------------------------------------------------
    // Roadside buildings + ambient people (Manual drive street)

    static bool BldgBorder(int x, int y, int w, int h)
        => x == 0 || y == 0 || x == w - 1 || y == h - 1;

    static Color BahayBato(int x, int y, int w, int h)
    {
        // Heritage stone-and-wood house: terracotta tiled roof + capiz-window gallery.
        if (BldgBorder(x, y, w, h)) return BldgOutline;
        float fy = (float)y / h;
        if (fy < 0.30f)                                  // wood gallery + capiz windows
        {
            if (y < 3) return WoodDark;                  // sill
            bool mullion = (x % 9) < 2;
            return mullion ? WoodBrown : CapizPane;
        }
        int ridge = (int)(h * 0.66f);
        if (Mathf.Abs(y - ridge) < 2) return WoodDark;   // roof ridge
        Color roof = (y < ridge) ? RoofTerra : RoofTerraDk;
        if ((y % 5) == 0) roof *= 0.92f;                 // tile rows
        return roof;
    }

    static Color SariSari(int x, int y, int w, int h)
    {
        // Sari-sari store: red candy-striped awning over a shaded opening, galvanized roof.
        if (BldgBorder(x, y, w, h)) return BldgOutline;
        float fy = (float)y / h;
        if (fy < 0.30f)
        {
            if (fy < 0.12f) return WindowDark;           // shaded storefront
            bool stripe = ((x / 5) % 2) == 0;
            return stripe ? AwningRed : AwningStripe;
        }
        bool rib = (x % 6) < 3;                           // corrugated roof
        return rib ? RoofGalv : RoofGalvDk;
    }

    static Color Carinderia(int x, int y, int w, int h)
    {
        // Carinderia (eatery): open counter + orange awning, galvanized roof.
        if (BldgBorder(x, y, w, h)) return BldgOutline;
        float fy = (float)y / h;
        if (fy < 0.34f)
        {
            if (fy < 0.10f) return WoodDark;             // base
            if (fy < 0.18f) return CounterTan;           // counter
            bool stripe = ((x / 5) % 2) == 0;
            return stripe ? AwningOrange : AwningStripe;
        }
        bool rib = (x % 6) < 3;
        return rib ? RoofGalv : RoofGalvDk;
    }

    static Color NipaHut(int x, int y, int w, int h)
    {
        // Bahay kubo: bamboo-slat wall + thatch roof.
        if (BldgBorder(x, y, w, h)) return BldgOutline;
        float fy = (float)y / h;
        if (fy < 0.32f)
        {
            bool slat = (x % 4) < 2;                      // bamboo slats
            return slat ? WoodBrown : WoodBrown * 0.82f;
        }
        Color t = (((x + y) % 6) < 2) ? ThatchDark : ThatchTan;   // thatch texture
        if (fy > 0.64f) t *= 0.95f;
        return t;
    }

    static Color Chapel(int x, int y, int w, int h)
    {
        // Small chapel: white facade + arched door, gray roof with a cross.
        if (BldgBorder(x, y, w, h)) return BldgOutline;
        float fy = (float)y / h;
        float cx = w * 0.5f;
        if (fy < 0.32f)
        {
            if (Mathf.Abs(x - cx + 0.5f) < w * 0.14f && fy < 0.24f) return DoorBrown;
            return ChapelWhite;
        }
        bool crossV = Mathf.Abs(x - cx + 0.5f) < 1.5f && fy > 0.72f;
        bool crossH = fy > 0.84f && fy < 0.90f && Mathf.Abs(x - cx + 0.5f) < 5f;
        if (crossV || crossH) return ChapelWhite;        // cross on the roof
        return (y < (int)(h * 0.66f)) ? StoneGray : StoneGray * 0.88f;
    }

    static Color Townsfolk(int x, int y, int w, int h)
    {
        // Standing person with a wide hat — white so the runtime tints it a muted color
        // (distinct from the saturated, boardable peeps clustered at the stop signs).
        float cx = w * 0.5f;
        float headCy = h * 0.72f, headR = w * 0.26f;
        float dx = x - cx + 0.5f, dy = y - headCy;
        if (Mathf.Abs(dy - headR * 0.7f) < 1.5f && Mathf.Abs(dx) < w * 0.44f) return Color.white; // hat brim
        if (dx * dx + dy * dy <= headR * headR) return Color.white;                                // head
        if (y > 1 && y < h * 0.56f && Mathf.Abs(dx) < w * 0.30f) return Color.white;               // body
        return Color.clear;
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
