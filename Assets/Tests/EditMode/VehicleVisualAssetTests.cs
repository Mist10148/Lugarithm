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
}
