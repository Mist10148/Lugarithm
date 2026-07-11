using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LugarithmUiSkinTests
{
    [Test]
    public void BlueprintSkin_ProvidesEveryRequiredComponent()
    {
        Sprite[] sprites =
        {
            LugarithmUiSkin.WindowOuter, LugarithmUiSkin.PanelInner,
            LugarithmUiSkin.TitleRibbon, LugarithmUiSkin.CompactCard,
            LugarithmUiSkin.ButtonNormal, LugarithmUiSkin.ButtonPrimary,
            LugarithmUiSkin.ButtonDisabled, LugarithmUiSkin.DangerFrame,
            LugarithmUiSkin.Tab, LugarithmUiSkin.Segmented,
            LugarithmUiSkin.InputFrame, LugarithmUiSkin.DialogueFrame,
            LugarithmUiSkin.SliderTrack, LugarithmUiSkin.SliderKnob,
            LugarithmUiSkin.CheckboxOff, LugarithmUiSkin.CheckboxOn,
            LugarithmUiSkin.ScrollbarTrack, LugarithmUiSkin.ScrollbarHandle,
            LugarithmUiSkin.PortraitFrame,
        };
        foreach (Sprite sprite in sprites) Assert.NotNull(sprite);
    }

    [Test]
    public void JeepneyMinigameSkin_ProvidesReferenceShells()
    {
        Sprite[] sprites =
        {
            LugarithmUiSkin.MinigameStraightRoad,
            LugarithmUiSkin.MinigameResults,
            LugarithmUiSkin.JeepneyEditorShell,
        };
        foreach (Sprite sprite in sprites) Assert.NotNull(sprite);
    }

    [Test]
    public void JeepneyMinigameTextures_StayPixelPerfect()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D",
            new[] { "Assets/UI/Sprites/LugarithmUi/TutorialMinigames" });
        Assert.That(guids.Length, Is.GreaterThanOrEqualTo(7));
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
    public void BlueprintComponents_UsePointFilteringAndNineSliceBorders()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D",
            new[] { "Assets/UI/Sprites/LugarithmUi/Components" });
        Assert.That(guids.Length, Is.GreaterThanOrEqualTo(19));
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.NotNull(importer, path);
            Assert.AreEqual(FilterMode.Point, importer.filterMode, path);
            Assert.IsFalse(importer.mipmapEnabled, path);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, path);
            Assert.That(importer.spriteBorder.sqrMagnitude, Is.GreaterThan(0f), path);
        }
    }

    [Test]
    public void FactoryButton_UsesBlueprintSpritesWithoutBakedText()
    {
        var root = new GameObject("Root", typeof(RectTransform));
        try
        {
            Button button = UIFactory.CreateButton(root.transform, "RunButton", "RUN", new Vector2(200f, 60f));
            Assert.AreEqual(LugarithmUiSkin.ButtonNormal, button.image.sprite);
            Assert.AreEqual(Selectable.Transition.SpriteSwap, button.transition);
            Assert.AreEqual("RUN", button.GetComponentInChildren<TMP_Text>().text);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SettingsBlueprint_PreservesLocalizedControlsAndReferenceGeometry()
    {
        Canvas canvas = UIFactory.CreateCanvas("TestCanvas");
        try
        {
            SettingsPanel panel = SettingsPanelBuilder.Build(canvas.transform);
            Assert.NotNull(panel);
            RectTransform window = canvas.transform.Find("Settings/SettingsOverlay/Window") as RectTransform;
            Assert.NotNull(window);
            Assert.AreEqual(new Vector2(700f, 665f), window.sizeDelta);
            Assert.NotNull(window.Find("TitlePlate"));
            Assert.NotNull(window.Find("gameplayIconFrame"));
            Assert.NotNull(window.Find("CloseButton"));
        }
        finally
        {
            Object.DestroyImmediate(canvas.gameObject);
        }
    }
}
