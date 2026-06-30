using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Project-local accessors for the imported Sprout Lands UI art sheets.
/// The asset postprocessor slices the copied PNGs into named sprite sub-assets;
/// this helper resolves those slices by name for editor-time scene building.
/// </summary>
public static class SproutLandsUiLibrary
{
    const string Root = "Assets/UI/Sprites/SproutLands/Sheets";
    const string CustomIconRoot = "Assets/UI/Sprites/SproutLands/CustomIcons";

    public static Sprite MenuCardBlank => Get("Setting menu.png", "SettingsMenu_0_1");
    public static Sprite MenuCardTitle  => Get("Setting menu.png", "SettingsMenu_0_0");

    public static Sprite BigPlayBlank   => Get("UI Big Play Button.png", "BigPlay_0_0");
    public static Sprite BigPlayDark    => Get("UI Big Play Button.png", "BigPlay_0_1");
    public static Sprite BigPlayAction   => Get("UI Big Play Button.png", "BigPlay_1_0");
    public static Sprite BigPlayActionDark => Get("UI Big Play Button.png", "BigPlay_1_1");

    public static Sprite MainMenuBackground => Get("MainMenuCoastBackground.png");
    public static Sprite MainMenuTitle => Get("LugarithmTitle.png");
    public static Sprite MainMenuButton => Get("MainMenuButton.png");
    public static Sprite MainMenuButtonDisabled => Get("MainMenuButtonDisabled.png");
    public static Sprite DialogBoxSmall => Get("dialog box small.png");

    public static Sprite SquareButton    => Get("Square Buttons 26x26.png", "Square26_0_0");

    public static Sprite SmallSquareButton => Get("Small Square Buttons.png", "SmallSquare_0_0");

    public static Sprite SettingsSheet => Get("UI Settings Buttons.png", "UISettings_0_0");

    public static Sprite MenuIconSettings => GetCustomIcon("SettingsGear.png");
    public static Sprite MenuIconBook     => GetCustomIcon("JournalBook.png");
    public static Sprite MenuIconQuit      => GetCustomIcon("QuitExit.png");
    public static Sprite MenuIconJeep      => GetCustomIcon("JeepMenuIcon.png");
    public static Sprite MenuIconRoute     => GetCustomIcon("RouteMenuIcon.png");

    static Sprite Get(string fileName, string spriteName)
    {
        string path = $"{Root}/{fileName}";
        var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                                   .OfType<Sprite>()
                                   .ToArray();
        if (sprites.Length > 0)
        {
            var sprite = sprites.FirstOrDefault(s => string.Equals(s.name, spriteName, StringComparison.Ordinal));
            if (sprite != null)
                return sprite;

            throw new Exception($"Unable to load sprite '{spriteName}' from '{path}'. Available sprite slices were not matched by name.");
        }

        var singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (singleSprite != null)
            return singleSprite;

        throw new Exception($"Unable to load sprite '{spriteName}' from '{path}'. Make sure the Sprout Lands UI assets were copied and imported.");
    }

    static Sprite Get(string fileName)
    {
        string path = $"{Root}/{fileName}";
        var singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (singleSprite != null)
            return singleSprite;

        var sprite = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                                  .OfType<Sprite>()
                                  .FirstOrDefault();
        if (sprite != null)
            return sprite;

        throw new Exception($"Unable to load sprite asset '{path}'. Make sure the Sprout Lands UI assets were copied and imported.");
    }

    static Sprite GetCustomIcon(string fileName)
    {
        string path = $"{CustomIconRoot}/{fileName}";
        var singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (singleSprite != null)
            return singleSprite;

        var sprite = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                                  .OfType<Sprite>()
                                  .FirstOrDefault();
        if (sprite != null)
            return sprite;

        throw new Exception($"Unable to load custom icon sprite asset '{path}'. Make sure the fallback icons were imported.");
    }
}
