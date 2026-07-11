using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The always-visible "!" beacons above town-hub minigame stations: present and
/// bobbing on every unsolved station (main quest gold + larger), absent from
/// NPC/jeep/exit triggers, and hidden once a station is solved.
/// </summary>
public class StationBeaconPlayModeTests
{
    [UnityTest]
    public IEnumerator Beacons_ShowOnUnsolvedMinigameStations_AndHideOnSolve()
    {
        SceneManager.LoadScene("TopDownLevel");
        yield return null;
        yield return null;

        TopDownLevelController controller =
            UnityEngine.Object.FindAnyObjectByType<TopDownLevelController>();
        Assert.IsNotNull(controller);

        var stationDefs = ReadField<Dictionary<InteractionTrigger, MinigameStationDef>>(
            controller, "_stationDefs");
        var beacons = ReadField<Dictionary<InteractionTrigger, GameObject>>(
            controller, "_stationBeacons");
        Assert.AreEqual(6, stationDefs.Count, "the tutorial town has six minigame stations");
        Assert.AreEqual(stationDefs.Count, beacons.Count,
            "every minigame station must own a beacon");

        InteractionTrigger mainTrigger = null;
        foreach (var kv in stationDefs)
        {
            GameObject beacon = beacons[kv.Key];
            Assert.IsNotNull(beacon, $"{kv.Value.id} has no beacon");
            Assert.IsTrue(beacon.activeInHierarchy,
                $"{kv.Value.id}'s beacon must be visible before the station is solved");
            Assert.IsNotNull(beacon.GetComponent<StationBeacon>());

            if (kv.Value.isMainQuest) mainTrigger = kv.Key;
        }
        Assert.IsNotNull(mainTrigger, "one station must be the main quest");
        Assert.Greater(beacons[mainTrigger].transform.localScale.x, 2f,
            "the main quest beacon reads larger than side-objective beacons");

        // Non-station triggers (NPCs, jeep stop, exit) never get a beacon.
        var triggers = ReadField<List<InteractionTrigger>>(controller, "_triggers");
        foreach (InteractionTrigger trigger in triggers)
        {
            if (stationDefs.ContainsKey(trigger)) continue;
            Assert.IsNull(trigger.transform.Find("Beacon"),
                $"{trigger.name} is not a minigame station and must not carry a beacon");
        }

        // Solving a station retires its beacon.
        MethodInfo solve = typeof(TopDownLevelController).GetMethod("MarkStationSolved",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(solve);
        solve.Invoke(controller, new object[] { mainTrigger, stationDefs[mainTrigger] });

        Assert.IsFalse(beacons[mainTrigger].activeInHierarchy,
            "a solved station's beacon must disappear");

        yield return Cleanup();
    }

    static T ReadField<T>(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, name);
        return (T)field.GetValue(target);
    }

    static IEnumerator Cleanup()
    {
        Scene cleanup = SceneManager.CreateScene("BeaconTestCleanup_" + Guid.NewGuid());
        SceneManager.SetActiveScene(cleanup);
        yield return SceneManager.UnloadSceneAsync("TopDownLevel");
    }
}
