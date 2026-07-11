using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class VehicleVisualAssetTests
{
    [TestCase("Assets/Resources/Vehicles/player_jeepney_sheet.png", 25)]
    [TestCase("Assets/Resources/Vehicles/jeepney_smoke_sheet.png", 18)]
    [TestCase("Assets/Resources/Vehicles/filipino_traffic_sheet.png", 8)]
    public void VehicleSheet_IsCrispAndFullySliced(string path, int minimumSprites)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Assert.NotNull(importer, path);
        Assert.AreEqual(FilterMode.Point, importer.filterMode, path);
        Assert.IsFalse(importer.mipmapEnabled, path);
        Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, path);
        Assert.That(AssetDatabase.LoadAllAssetsAtPath(path).Length - 1,
                    Is.GreaterThanOrEqualTo(minimumSprites), path);
    }

    [Test]
    public void VehicleAnimator_DoesNotAddPhysicsOrColliders()
    {
        var go = new GameObject("Vehicle", typeof(SpriteRenderer), typeof(VehicleSpriteAnimator));
        try
        {
            Assert.IsNull(go.GetComponent<Rigidbody2D>());
            Assert.IsNull(go.GetComponent<Collider2D>());
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PlayerJeepneySheet_ExposesRequiredFrameGroups()
    {
        // The runtime load path the animator uses. A renamed/missing sheet or
        // frame group should fail here with a clear message, not a null-ref at play.
        var all = Resources.LoadAll<Sprite>("Vehicles/player_jeepney_sheet");
        Assert.IsNotEmpty(all, "player_jeepney_sheet missing from Resources/Vehicles");
        foreach (var prefix in new[] { "idle_", "drive_" })
            Assert.IsTrue(System.Array.Exists(all, s => s.name.StartsWith(prefix)),
                $"player jeepney sheet has no '{prefix}' frames");
    }

    [Test]
    public void TrafficSheet_ProvidesEveryEnabledDesign()
    {
        var variants = Resources.LoadAll<Sprite>("Vehicles/filipino_traffic_sheet");
        Assert.GreaterOrEqual(variants.Length, 8,
            "filipino_traffic_sheet must slice into the 8 traffic designs the spawner cycles through");
    }
}
