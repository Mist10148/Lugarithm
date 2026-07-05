using NUnit.Framework;
using UnityEngine;

public class RoadTrafficControllerTests
{
    [Test]
    public void ForceSpawn_RespectsMaxOneActiveVehicle()
    {
        GameObject root = new GameObject("TrafficTestRoot");
        GameObject target = new GameObject("Target");
        try
        {
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);

            Assert.IsTrue(traffic.ForceSpawnForTests(0f));
            Assert.AreEqual(1, traffic.ActiveVehicleCount);

            Assert.IsFalse(traffic.ForceSpawnForTests(0f));
            Assert.AreEqual(1, traffic.ActiveVehicleCount);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void ForceSpawn_SkipsSlotsTooCloseToStops()
    {
        GameObject root = new GameObject("TrafficStopClearanceRoot");
        GameObject target = new GameObject("Target");
        try
        {
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform,
                new Vector2(18f, 0f), new Vector2(19f, 0f), new Vector2(21f, 0f),
                new Vector2(23f, 0f), new Vector2(25f, 0f), new Vector2(28f, 0f),
                new Vector2(30f, 0f), new Vector2(32f, 0f));
            traffic.InitManual(route, root.transform, target.transform, null);

            Assert.IsFalse(traffic.ForceSpawnForTests(0f));
            Assert.AreEqual(0, traffic.ActiveVehicleCount);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
        }
    }

    static RouteContext RouteWithStops(Transform parent, params Vector2[] stops)
    {
        var route = new RouteContext
        {
            Waypoints = new[] { Vector2.zero, new Vector2(80f, 0f) },
            TotalLength = 80f,
            Zones = new StopZone[stops.Length],
        };

        for (int i = 0; i < stops.Length; i++)
        {
            var go = new GameObject("Stop_" + i);
            go.transform.SetParent(parent, false);
            go.transform.position = stops[i];
            route.Zones[i] = go.AddComponent<StopZone>();
        }
        return route;
    }
}
