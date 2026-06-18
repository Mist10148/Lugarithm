using System;
using UnityEngine;

/// <summary>
/// A complete color palette for the code editor. Kept serializable so it can be
/// authored as an asset later; for now the library keeps a static set.
/// </summary>
[Serializable]
public class CodeTheme
{
    public int    id;
    public string name;
    public int    cost;

    [Header("Syntax")]
    public Color keywordColor;
    public Color actionColor;
    public Color queryColor;
    public Color commentColor;
    public Color textColor;
    public Color backgroundColor;

    [Header("Syntax (VS Code Dark+ extras)")]
    public Color stringColor;
    public Color numberColor;
    public Color constantColor;
    public Color variableColor;

    [Header("Status")]
    public Color errorColor;
    public Color okColor;

    [Header("Execution")]
    public Color execBarColor;
    public Color heatColdColor;
    public Color heatHotColor;

    /// <summary>Default Dark+ — matches the original hard-coded editor colors.</summary>
    public static CodeTheme DarkPlus => new CodeTheme
    {
        id = 0,
        name = "Dark+",
        cost = 0,
        keywordColor   = new Color(0.772f, 0.525f, 0.753f), // #C586C0
        actionColor    = new Color(0.863f, 0.863f, 0.667f), // #DCDCAA
        queryColor     = new Color(0.306f, 0.788f, 0.690f), // #4EC9B0
        commentColor   = new Color(0.416f, 0.600f, 0.333f), // #6A9955
        textColor      = new Color(0.933f, 0.933f, 0.878f),
        backgroundColor= new Color(0.070f, 0.080f, 0.110f),
        stringColor    = new Color(0.808f, 0.569f, 0.471f), // #CE9178
        numberColor    = new Color(0.710f, 0.808f, 0.659f), // #B5CEA8
        constantColor  = new Color(0.337f, 0.612f, 0.839f), // #569CD6
        variableColor  = new Color(0.612f, 0.863f, 0.996f), // #9CDCFE
        errorColor     = new Color(0.878f, 0.424f, 0.459f), // #E06C75
        okColor        = new Color(0.416f, 0.600f, 0.333f), // #6A9955
        execBarColor   = new Color(0.180f, 0.360f, 0.580f, 0.45f),
        heatColdColor  = new Color(0.416f, 0.600f, 0.333f),
        heatHotColor   = new Color(1.000f, 0.200f, 0.200f),
    };

    /// <summary>Neon — high-contrast cyberpunk accents.</summary>
    public static CodeTheme Neon => new CodeTheme
    {
        id = 1,
        name = "Neon",
        cost = 150,
        keywordColor   = new Color(1.000f, 0.200f, 0.760f),
        actionColor    = new Color(0.000f, 1.000f, 0.800f),
        queryColor     = new Color(0.600f, 0.800f, 1.000f),
        commentColor   = new Color(0.400f, 0.400f, 0.500f),
        textColor      = new Color(0.950f, 0.950f, 1.000f),
        backgroundColor= new Color(0.020f, 0.020f, 0.050f),
        stringColor    = new Color(1.000f, 0.850f, 0.300f),
        numberColor    = new Color(0.400f, 1.000f, 0.700f),
        constantColor  = new Color(0.800f, 0.500f, 1.000f),
        variableColor  = new Color(0.600f, 0.900f, 1.000f),
        errorColor     = new Color(1.000f, 0.200f, 0.200f),
        okColor        = new Color(0.000f, 1.000f, 0.400f),
        execBarColor   = new Color(0.000f, 0.700f, 1.000f, 0.50f),
        heatColdColor  = new Color(0.000f, 1.000f, 0.400f),
        heatHotColor   = new Color(1.000f, 0.000f, 0.200f),
    };

    /// <summary>Retro Amber — phosphor-terminal warmth.</summary>
    public static CodeTheme Amber => new CodeTheme
    {
        id = 2,
        name = "Amber",
        cost = 150,
        keywordColor   = new Color(1.000f, 0.700f, 0.000f),
        actionColor    = new Color(1.000f, 0.900f, 0.400f),
        queryColor     = new Color(1.000f, 0.550f, 0.000f),
        commentColor   = new Color(0.600f, 0.450f, 0.150f),
        textColor      = new Color(1.000f, 0.800f, 0.300f),
        backgroundColor= new Color(0.080f, 0.050f, 0.000f),
        stringColor    = new Color(1.000f, 0.780f, 0.350f),
        numberColor    = new Color(1.000f, 0.870f, 0.500f),
        constantColor  = new Color(1.000f, 0.600f, 0.100f),
        variableColor  = new Color(1.000f, 0.820f, 0.450f),
        errorColor     = new Color(1.000f, 0.300f, 0.000f),
        okColor        = new Color(1.000f, 0.700f, 0.000f),
        execBarColor   = new Color(1.000f, 0.600f, 0.000f, 0.35f),
        heatColdColor  = new Color(1.000f, 0.700f, 0.000f),
        heatHotColor   = new Color(1.000f, 0.200f, 0.000f),
    };

    /// <summary>Solarized — calmer, lower-contrast palette.</summary>
    public static CodeTheme Solarized => new CodeTheme
    {
        id = 3,
        name = "Solarized",
        cost = 200,
        keywordColor   = new Color(0.700f, 0.300f, 0.500f),
        actionColor    = new Color(0.200f, 0.500f, 0.600f),
        queryColor     = new Color(0.400f, 0.600f, 0.200f),
        commentColor   = new Color(0.500f, 0.580f, 0.590f),
        textColor      = new Color(0.300f, 0.300f, 0.290f),
        backgroundColor= new Color(0.990f, 0.960f, 0.890f),
        stringColor    = new Color(0.160f, 0.470f, 0.470f),
        numberColor    = new Color(0.380f, 0.420f, 0.130f),
        constantColor  = new Color(0.150f, 0.400f, 0.700f),
        variableColor  = new Color(0.200f, 0.350f, 0.450f),
        errorColor     = new Color(0.800f, 0.200f, 0.150f),
        okColor        = new Color(0.400f, 0.600f, 0.200f),
        execBarColor   = new Color(0.900f, 0.800f, 0.500f, 0.55f),
        heatColdColor  = new Color(0.400f, 0.600f, 0.200f),
        heatHotColor   = new Color(0.900f, 0.200f, 0.150f),
    };

    /// <summary>Mono — minimal grayscale.</summary>
    public static CodeTheme Mono => new CodeTheme
    {
        id = 4,
        name = "Mono",
        cost = 100,
        keywordColor   = new Color(0.800f, 0.800f, 0.800f),
        actionColor    = new Color(0.700f, 0.700f, 0.700f),
        queryColor     = new Color(0.600f, 0.600f, 0.600f),
        commentColor   = new Color(0.450f, 0.450f, 0.450f),
        textColor      = new Color(0.900f, 0.900f, 0.900f),
        backgroundColor= new Color(0.080f, 0.080f, 0.080f),
        stringColor    = new Color(0.750f, 0.750f, 0.750f),
        numberColor    = new Color(0.650f, 0.650f, 0.650f),
        constantColor  = new Color(0.850f, 0.850f, 0.850f),
        variableColor  = new Color(0.880f, 0.880f, 0.880f),
        errorColor     = new Color(0.900f, 0.300f, 0.300f),
        okColor        = new Color(0.500f, 0.900f, 0.500f),
        execBarColor   = new Color(0.500f, 0.500f, 0.500f, 0.40f),
        heatColdColor  = new Color(0.500f, 0.900f, 0.500f),
        heatHotColor   = new Color(1.000f, 0.200f, 0.200f),
    };
}
