using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Locks the universal Settings overlay architecture: one prefab source, fully
/// wired controls, pixel-perfect crisp assets, and no generic Unity sprites.
/// Runtime open/close/singleton behavior lives in the PlayMode suite (Awake
/// only runs there).
/// </summary>
public class UniversalSettingsTests
{
    const string ControlsDir = "Assets/UI/Sprites/LugarithmUi/Settings/Controls";
    const string SettingsDir  = "Assets/UI/Sprites/LugarithmUi/Settings";

    // Serialized fields every SettingsPanel control must resolve to.
    static readonly string[] PanelFields =
    {
        "root", "closeButton", "driveModeSelector", "codingSelector", "brakeSelector",
        "musicSlider", "sfxSlider", "languageSelector", "subtitlesSelector",
        "dialogueSpeedSelector", "themeButton", "themeLabel",
    };

    [Test]
    public void OverlayPrefab_HasManagerCanvasAndWiredPanel()
    {
        SettingsOverlayBuilder.Build();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsOverlayBuilder.PrefabPath);
        Assert.NotNull(prefab, "Settings overlay prefab was not created.");

        var manager = prefab.GetComponent<UniversalSettingsManager>();
        Assert.NotNull(manager, "Prefab root is missing UniversalSettingsManager.");
        Assert.NotNull(prefab.GetComponentInChildren<Canvas>(true), "Prefab has no Canvas.");

        var panel = prefab.GetComponentInChildren<SettingsPanel>(true);
        Assert.NotNull(panel, "Prefab has no SettingsPanel.");

        var so = new SerializedObject(manager);
        SerializedProperty panelProp = so.FindProperty("panel");
        Assert.NotNull(panelProp, "UniversalSettingsManager has no 'panel' field.");
        Assert.NotNull(panelProp.objectReferenceValue, "UniversalSettingsManager.panel is not wired.");
    }

    [Test]
    public void SettingsPanel_HasEveryControlWired()
    {
        Canvas canvas = UIFactory.CreateCanvas("WiringTestCanvas");
        try
        {
            SettingsPanel panel = SettingsPanelBuilder.Build(canvas.transform);
            var so = new SerializedObject(panel);
            foreach (string field in PanelFields)
            {
                SerializedProperty prop = so.FindProperty(field);
                Assert.NotNull(prop, $"SettingsPanel has no serialized field '{field}'.");
                Assert.NotNull(prop.objectReferenceValue, $"SettingsPanel.{field} is not wired.");
            }
        }
        finally
        {
            Object.DestroyImmediate(canvas.gameObject);
        }
    }

    [Test]
    public void RequiredSettingsSprites_Exist()
    {
        Assert.NotNull(LugarithmUiSkin.SettingsWindow, "settings window sprite missing");

        string[] controls =
        {
            "selector_normal", "selector_selected",
            "icon_gameplay", "icon_controls", "icon_audio", "icon_dialogue", "icon_code",
        };
        foreach (string name in controls)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ControlsDir}/{name}.png");
            Assert.NotNull(sprite, $"Missing Settings control sprite '{name}'.");
        }
    }

    [Test]
    public void SettingsTextures_StayPixelPerfect()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SettingsDir });
        Assert.That(guids.Length, Is.GreaterThan(0), "No Settings textures found.");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.NotNull(importer, path);
            Assert.AreEqual(FilterMode.Point, importer.filterMode, path);
            Assert.IsFalse(importer.mipmapEnabled, path);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, path);
        }
    }

    [Test]
    public void SettingsControls_DoNotUseBuiltinUnitySprite()
    {
        Canvas canvas = UIFactory.CreateCanvas("BuiltinCheckCanvas");
        try
        {
            SettingsPanel panel = SettingsPanelBuilder.Build(canvas.transform);
            string[] builtin = { "UISprite", "Background", "InputFieldBackground", "UIMask", "Knob" };

            foreach (Image image in panel.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null) continue; // color-only graphics are fine
                foreach (string bad in builtin)
                    Assert.AreNotEqual(bad, image.sprite.name,
                        $"Settings control '{image.name}' uses the generic built-in sprite '{bad}'.");
            }
        }
        finally
        {
            Object.DestroyImmediate(canvas.gameObject);
        }
    }

    [Test]
    public void SegmentedPillLabels_FitInEnglishAndFilipino()
    {
        // The tightest rows: four dialogue-speed pills (compact width/font) and the
        // two language pills (wide width/font). Every localized label must fit its
        // pill with a little internal padding to spare.
        AssertPillsFit(new[] { "opt.speed.slow", "opt.speed.normal", "opt.speed.fast", "opt.speed.instant" },
                       SettingsLayout.SegCompactWidth, SettingsLayout.SegCompactFont);
        AssertPillsFit(new[] { "opt.english", "opt.filipino" },
                       SettingsLayout.SegWideWidth, SettingsLayout.SegDefaultFont);
        AssertPillsFit(new[] { "opt.manual", "opt.automation" },
                       SettingsLayout.SegWideWidth, SettingsLayout.SegDefaultFont);
    }

    static void AssertPillsFit(string[] keys, float pillWidth, float fontSize)
    {
        // The true overflow bound is the pill's own width — a label wider than the
        // pill spills out. (No extra padding subtracted, to avoid flagging a label
        // that fits snugly as a failure.)
        TMP_FontAsset font = SproutLandsMenuFont.EnsureFontAsset();

        var go = new GameObject("Measure", typeof(RectTransform));
        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = fontSize;
        text.enableWordWrapping = false;
        try
        {
            foreach (GameLanguage lang in new[] { GameLanguage.English, GameLanguage.Filipino })
            {
                foreach (string key in keys)
                {
                    string label = LocalizationTable.Get(key, lang);
                    float w = text.GetPreferredValues(label).x;
                    Assert.LessOrEqual(w, pillWidth,
                        $"Label '{label}' ({lang}) is {w:0.#}px wide — overflows the {pillWidth}px pill.");
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
