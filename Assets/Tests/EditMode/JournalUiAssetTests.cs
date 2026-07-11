using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class JournalUiAssetTests
{
    static readonly string[] Required =
    {
        "heritage_card", "heritage_card_selected", "heritage_card_locked",
        "coding_row", "coding_row_selected", "title_ribbon", "oracle_banner",
        "player_message_frame", "oracle_message_frame", "input_frame", "send_seal",
        "nav_prev_normal", "nav_prev_disabled", "nav_next_normal", "nav_next_disabled",
        "landmark_tutorial_jaro", "landmark_iloilo_molo", "landmark_oton",
        "landmark_tigbauan", "landmark_miagao", "landmark_san_joaquin",
        "coding_icon_commands", "coding_icon_autopilot"
    };

    [Test]
    public void JournalKit_HasCrispTransparentSpriteImports()
    {
        foreach (string name in Required)
        {
            string path = $"Assets/UI/Sprites/LugarithmUi/Journal/Parts/{name}.png";
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(path), path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer, path);
            Assert.AreEqual(FilterMode.Point, importer.filterMode, path);
            Assert.IsFalse(importer.mipmapEnabled, path);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, path);
            Assert.AreEqual(1f, importer.spritePixelsPerUnit, path);
        }
    }
}
