using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tile types for the top-down level tilemap. Each character in a level map
/// string corresponds to one tile. See <see cref="OverworldMapData"/>.
/// </summary>
public enum TileType
{
    Grass,      // G — open walkable grass
    Path,       // P — dirt path (walkable)
    Wall,       // W — solid wall/building (blocks movement)
    Water,      // ~ — water (blocks movement, cosmetic)
}

/// <summary>
/// Special entity types placed on top of the tilemap. These are not tiles
/// themselves but markers read from the map string that spawn GameObjects
/// (NPCs, interaction zones, the player start, etc.).
/// </summary>
public enum EntityType
{
    None,       // . — nothing
    PlayerStart,// S — player spawn point
    Npc,        // N — NPC character
    JeepStop,   // J — jeep boarding zone (launches jeep minigame)
    Exit,       // E — level exit / completion trigger
    PuzzleStation, // Q — non-coding puzzle minigame station (maze, flow, block-fill…)
    CodeChallenge, // C — the town's main coding challenge (gates moving on)
    Artifact,   // Runtime-only secret collectible unlocked after every objective
}

/// <summary>
/// A single parsed entity from a level map string. Carries the grid position,
/// type, and an optional display name (for NPCs, stops, etc.).
/// </summary>
[Serializable]
public class MapEntity
{
    public EntityType type;
    public int gridX;
    public int gridY;
    public string displayName;

    /// <summary>For NPC entities: the conversation id resolved against
    /// <see cref="TownNpcDialogueLibrary"/>. Assigned by the map author in
    /// row-major spawn order (see <see cref="OverworldMapLibrary"/>).</summary>
    public string npcId;

    /// <summary>For minigame stations (puzzle / code challenge): the definition id
    /// resolved against <see cref="TownMinigameLibrary"/>. Assigned in row-major
    /// spawn order by station kind (see <see cref="OverworldMapLibrary"/>).</summary>
    public string minigameId;

    /// <summary>World-space position derived from grid coords.</summary>
    public Vector2 WorldPosition(float cellSize)
    {
        return new Vector2(gridX * cellSize, gridY * cellSize);
    }
}

/// <summary>
/// Full map data for one top-down level: a 2D grid of <see cref="TileType"/>
/// tiles plus a list of <see cref="MapEntity"/> overlays. Maps are authored
/// as string arrays (one string per row, one char per cell) and parsed into
/// this structured form for runtime use.
/// </summary>
[Serializable]
public class OverworldMapData
{
    /// <summary>Map width (columns) — derived from the first row.</summary>
    public int width;

    /// <summary>Map height (rows).</summary>
    public int height;

    /// <summary>
    /// 2D tile grid, indexed [y, x]. All entries are valid <see cref="TileType"/>
    /// values; entities have already been extracted.
    /// </summary>
    public TileType[,] tiles;

    /// <summary>Entities extracted from the map string.</summary>
    public List<MapEntity> entities = new List<MapEntity>();

    // -------------------------------------------------------------------------

    /// <summary>
    /// Parses a string-array map into structured tile + entity data.
    /// Tile chars: G (grass), P (path), W (wall), ~ (water).
    /// Entity chars: S (player start), N (NPC), J (jeep stop), E (exit),
    /// Q (puzzle station), C (code challenge), . (nothing).
    /// </summary>
    public static OverworldMapData Parse(string[] rows)
    {
        int h = rows.Length;
        int w = 0;
        for (int i = 0; i < h; i++)
            if (rows[i].Length > w) w = rows[i].Length;

        var data = new OverworldMapData { width = w, height = h };
        data.tiles = new TileType[h, w];
        data.entities = new List<MapEntity>();

        for (int y = 0; y < h; y++)
        {
            string row = rows[y];

            for (int x = 0; x < w; x++)
            {
                char ch = x < row.Length ? row[x] : 'G'; // pad with grass

                // Check entity chars first
                EntityType ent = EntityFromChar(ch);
                if (ent != EntityType.None)
                {
                    data.entities.Add(new MapEntity
                    {
                        type = ent,
                        gridX = x,
                        gridY = y,
                        displayName = ent.ToString(),
                    });
                    data.tiles[y, x] = TileType.Path; // entities sit on walkable ground
                }
                else
                {
                    data.tiles[y, x] = TileFromChar(ch);
                }
            }
        }

        return data;
    }

    /// <summary>Gets the tile at grid position (x, y). Out of bounds → Grass.</summary>
    public TileType GetTile(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return TileType.Grass;
        return tiles[y, x];
    }

    /// <summary>True when the tile blocks player movement (walls, water).</summary>
    public bool IsSolid(int x, int y)
    {
        TileType t = GetTile(x, y);
        return t == TileType.Wall || t == TileType.Water;
    }

    // -------------------------------------------------------------------------

    static TileType TileFromChar(char ch)
    {
        switch (ch)
        {
            case 'P': return TileType.Path;
            case 'W': return TileType.Wall;
            case '~': return TileType.Water;
            default:  return TileType.Grass; // G or anything else
        }
    }

    static EntityType EntityFromChar(char ch)
    {
        switch (ch)
        {
            case 'S': return EntityType.PlayerStart;
            case 'N': return EntityType.Npc;
            case 'J': return EntityType.JeepStop;
            case 'E': return EntityType.Exit;
            case 'Q': return EntityType.PuzzleStation;
            case 'C': return EntityType.CodeChallenge;
            default:  return EntityType.None;
        }
    }
}

/// <summary>
/// Static library of authored overworld maps. Follows the same pattern as
/// <see cref="LevelLibrary"/> — maps are code-defined and can later be lifted
/// into ScriptableObjects or asset files.
/// </summary>
public static class OverworldMapLibrary
{
    /// <summary>
    /// Returns the authored overworld map for a level index. All content levels
    /// have a walk-around town with 1–4 NPCs, a jeep stop (boards the drive), and
    /// an exit. Unknown indices fall back to the tutorial map.
    ///
    /// Legend:
    ///   G = grass,  P = path,  W = wall,  ~ = water
    ///   S = player start, N = NPC, J = jeep stop, E = exit
    ///   Q = puzzle station (non-coding minigame), C = code challenge
    /// NPC tiles are tagged with their conversation id + name in row-major spawn
    /// order (top-to-bottom, left-to-right) via <see cref="Build"/>. Minigame
    /// stations (Q/C) are bound to <see cref="TownMinigameLibrary"/> defs by the
    /// level controller in the same row-major order, per station kind.
    /// </summary>
    public static OverworldMapData ForLevel(int levelIndex)
    {
        switch (levelIndex)
        {
            case 1:  return MoloMap();
            case 2:  return OtonMap();
            case 3:  return TigbauanMap();
            case 4:  return MiagaoMap();
            case 5:  return SanJoaquinMap();
            default: return TutorialMap();
        }
    }

    // -------------------------------------------------------------------------

    public static OverworldMapData TutorialMap()
    {
        return Build(new[]
        {
            "WWWWWWWWWWWWWWWWWWWWWWWW",
            "WWWWWWWWWWPPPPPWWWWWWWWW",
            "WWWWWWWWWWPPPPPWWWWWWWWW",
            "WWWWWWWWWWPPPPPWWWWWWWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWPPPPQPPPPPPPQPPPPWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWPPPPNPPPPPPPNPPPPWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWWWWPPPPPPPPPPPPWWWWW",
            "WWWWWWWPPPPPPPPPPPPWWWWW",
            "WWWWPPQPPPPPPPPPPPQPPWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWWWWWWWPPPPPWWWWWWWWW",
            "WWWWWWWWWWPPPPPWWWWWWWWW",
            "WWWWWWWWWWPPPPPWWWWWWWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWPPPPPPPPEPPPPPPPPWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWWWWWWWPPPPPWWWWWWWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWPPPPPPPPNPPPPPPPPWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWPPPPPPPPPPPPPPCPPWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWWWWPPPPPPPPPPPPWWWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWPPCPPPPPPPPPPPPPPWWW",
            "WWWWPPPPPPPPJPPPPPPPPWWW",
            "WWWWPPPPPPPPPPPPPPPPPWWW",
            "WWWWPPPPPPPPSPPPPPPPPWWW",
            "WWWWWWWWWWWWWWWWWWWWWWWW",
        },
            ("il_vendor",  "Manang Rosa"),
            ("il_student", "Toto"),
            ("il_tindera", "Aling Bising"));
    }

    static OverworldMapData MoloMap()
    {
        return Build(new[]
        {
            "GGGGGGGGGGGGGGGG",
            "GWWWGGGGGGGWWWGG",
            "GGNGGGGEGGGGNGGG",
            "GGGQGGGPGGGGQGGG",
            "GPPPPPPPPPPPPPPG",
            "GGGGGGGPGGGCGGGG",
            "GGQGGGGNGGGQGGGG",
            "GGGGGGGPGGCGGGGG",
            "GGGGGGGPGGGGGGGG",
            "GGGGGGGJGGGGGGGG",
            "GGGGGGGGGGGGGGGG",
            "GGGGGGGSGGGGGGGG",
        },
            ("molo_cook",      "Manang Pacing"),
            ("molo_sacristan", "Mang Ambo"),
            ("molo_kid",       "Inday"));
    }

    static OverworldMapData OtonMap()
    {
        return Build(new[]
        {
            "GGGGGGGGGGGGGGGG",
            "G~~~~~~~~~~~~~~GG",
            "GGNGGGGGGGGNGGGG",
            "GGGQGGGGGGGGQGGG",
            "GGGGGGGEGGGGGGGG",
            "GPPPPPPPPPPPPPGG",
            "GGQGGGGPGGGCGQGG",
            "GGGGGGGJGGGGGGGG",
            "GGGGGGGCGGGGGGGG",
            "GGGGGGGSGGGGGGGG",
        },
            ("oton_fisher", "Mang Dado"),
            ("oton_guide",  "Ate Let"));
    }

    static OverworldMapData TigbauanMap()
    {
        return Build(new[]
        {
            "GGGGGGGGGGGGGGGG",
            "GWWWGGGGGGGGGGGG",
            "GGNGGGGEGGGGGGGG",
            "GGGQGGGPGGGGQGGG",
            "GPPPPPPPPPPPPGGG",
            "GGGCGGGQGNGQGGGG",
            "GGGGGGGGGPCGGGGG",
            "GGGGGGGGGJGGGGGG",
            "GGGGGGGGGGGGGGGG",
            "GGGGGGGSGGGGGGGG",
        },
            ("tig_weaver",  "Nanay Pilar"),
            ("tig_teacher", "Mr. Tan"));
    }

    static OverworldMapData MiagaoMap()
    {
        return Build(new[]
        {
            "GGGGGGGGGGGGGGGG",
            "GGGGGWWWWWGGGGGG",
            "GGNGGGGEGGGGNGGG",
            "GGGQGGGPGGGGQGGG",
            "GPPPPPPPPPPPPPGG",
            "GGGGGGGPGGGCGGGG",
            "GGQGGGGJGGGGQGGG",
            "GGGGGGGCGGGGGGGG",
            "GGGGGGGSGGGGGGGG",
        },
            ("miag_guide",  "Kuya Boy"),
            ("miag_weaver", "Lola Ines"));
    }

    static OverworldMapData SanJoaquinMap()
    {
        return Build(new[]
        {
            "GGGGGGGGGGGGGGGG",
            "GGNGGGGEGGGGNGGG",
            "GGGQGGGPGGGGQGGG",
            "GPPPPPPPPPPPPPGG",
            "GGGGGGGPGGGCGGGG",
            "GGQGGGGJGGGGQGGG",
            "GGGGGGGSGGCGGGGG",
            "G~~~~~~~~~~~~~~GG",
            "G~~~~~~~~~~~~~~GG",
        },
            ("sj_keeper", "Manong Edring"),
            ("sj_fisher", "Aling Cora"));
    }

    // -------------------------------------------------------------------------

    /// <summary>Parses a map and tags its NPC entities (in row-major spawn order)
    /// with the given conversation id + display name pairs.</summary>
    static OverworldMapData Build(string[] rows, params (string id, string name)[] npcs)
    {
        OverworldMapData data = OverworldMapData.Parse(rows);
        int i = 0;
        foreach (MapEntity e in data.entities)
        {
            if (e.type != EntityType.Npc) continue;
            if (i < npcs.Length)
            {
                e.npcId = npcs[i].id;
                e.displayName = npcs[i].name;
            }
            i++;
        }
        return data;
    }
}
