using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Runtime behavior of the universal Settings overlay: the persistent-singleton
/// guard, and open/close/toggle through the shipped Resources prefab. Scene-load
/// and live-localization checks are covered by the manual/visual validation pass
/// (they depend on regenerated scenes being present in the build settings).
/// </summary>
public class UniversalSettingsPlayModeTests
{
    [TearDown]
    public void TearDown()
    {
        // Destroy any persistent instance so the static singleton doesn't leak
        // between tests.
        if (UniversalSettingsManager.Instance != null)
            Object.DestroyImmediate(UniversalSettingsManager.Instance.gameObject);
    }

    [UnityTest]
    public IEnumerator DuplicateManager_DestroysItselfAndKeepsFirst()
    {
        var first = new GameObject("SettingsFirst").AddComponent<UniversalSettingsManager>();
        var secondGo = new GameObject("SettingsSecond");
        secondGo.AddComponent<UniversalSettingsManager>();

        yield return null; // let the guard's Destroy() process

        Assert.AreSame(first, UniversalSettingsManager.Instance,
            "The first manager should remain the singleton instance.");
        Assert.IsTrue(secondGo == null,
            "The duplicate manager's GameObject should have destroyed itself.");
    }

    [Test]
    public void PrefabInstance_OpensClosesAndTogglesThroughSingleton()
    {
        var prefab = Resources.Load<GameObject>("UI/SettingsOverlay");
        if (prefab == null)
        {
            Assert.Ignore("Settings overlay prefab not present — run Lugarithm/Build All Scenes first.");
            return;
        }

        Object.Instantiate(prefab); // Awake sets Instance
        UniversalSettingsManager manager = UniversalSettingsManager.Instance;
        Assert.NotNull(manager, "Instantiating the prefab should populate the singleton.");

        Assert.IsFalse(manager.IsOpen, "Overlay should start closed.");
        Assert.IsFalse(UniversalSettingsManager.IsAnyOpen);

        manager.Open();
        Assert.IsTrue(manager.IsOpen, "Open() should show the overlay.");
        Assert.IsTrue(UniversalSettingsManager.IsAnyOpen, "IsAnyOpen should reflect the open modal.");

        manager.Close();
        Assert.IsFalse(manager.IsOpen, "Close() should hide the overlay.");
        Assert.IsFalse(UniversalSettingsManager.IsAnyOpen);

        manager.Toggle();
        Assert.IsTrue(manager.IsOpen, "Toggle() should open a closed overlay.");
        manager.Toggle();
        Assert.IsFalse(manager.IsOpen, "Toggle() should close an open overlay.");
    }

    [Test]
    public void Ensure_ReturnsSingletonAndDoesNotDuplicate()
    {
        var prefab = Resources.Load<GameObject>("UI/SettingsOverlay");
        if (prefab == null)
        {
            Assert.Ignore("Settings overlay prefab not present — run Lugarithm/Build All Scenes first.");
            return;
        }

        UniversalSettingsManager a = UniversalSettingsManager.Ensure();
        UniversalSettingsManager b = UniversalSettingsManager.Ensure();
        Assert.NotNull(a);
        Assert.AreSame(a, b, "Ensure() must return the same instance, not create duplicates.");
        Assert.AreSame(a, UniversalSettingsManager.Instance);
    }
}
