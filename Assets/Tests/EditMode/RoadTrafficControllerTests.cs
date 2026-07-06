using NUnit.Framework;
using UnityEngine;

public class RoadTrafficControllerTests
{
    [Test]
    public void ForceSpawn_RespectsModerateActiveVehicleCap()
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

            Assert.IsTrue(traffic.ForceSpawnForTests(0f));
            Assert.AreEqual(2, traffic.ActiveVehicleCount);

            Assert.IsTrue(traffic.ForceSpawnForTests(0f));
            Assert.AreEqual(3, traffic.ActiveVehicleCount);

            Assert.IsFalse(traffic.ForceSpawnForTests(0f));
            Assert.AreEqual(3, traffic.ActiveVehicleCount);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void SpawnedCars_GetRandomSpeedsWithinSaneRange()
    {
        GameObject root = new GameObject("TrafficSpeedRoot");
        GameObject target = new GameObject("Target");
        try
        {
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);

            Assert.IsTrue(traffic.ForceSpawnAtForTests(12f, 1f));
            Assert.IsTrue(traffic.ForceSpawnAtForTests(24f, -1f));
            Assert.IsTrue(traffic.ForceSpawnAtForTests(36f, 1f));

            float first = traffic.VehicleCruiseSpeedForTests(0);
            bool anyDifferent = false;
            for (int i = 0; i < traffic.ActiveVehicleCount; i++)
            {
                float speed = traffic.VehicleCruiseSpeedForTests(i);
                Assert.GreaterOrEqual(speed, 2.2f);
                Assert.LessOrEqual(speed, 3.3f);
                anyDifferent |= !Mathf.Approximately(first, speed);
            }
            Assert.IsTrue(anyDifferent, "traffic cars should not all share one fixed speed");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void CarBehindStoppedTarget_QueuesInsteadOfPassing()
    {
        GameObject root = new GameObject("TrafficFollowRoot");
        GameObject target = new GameObject("Target");
        try
        {
            target.transform.position = new Vector3(30f, 0f, 0f);
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);
            Assert.IsTrue(traffic.ForceSpawnAtForTests(20f, 1f, 3f));

            traffic.Tick(5f);

            float queuedAlong = traffic.VehicleAlongForTests(0);
            Assert.LessOrEqual(queuedAlong, 30f - traffic.FollowDistanceForTests + 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void QueuedCar_ResumesWhenTargetMovesAway()
    {
        GameObject root = new GameObject("TrafficResumeRoot");
        GameObject target = new GameObject("Target");
        try
        {
            target.transform.position = new Vector3(30f, 0f, 0f);
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);
            Assert.IsTrue(traffic.ForceSpawnAtForTests(20f, 1f, 3f));
            traffic.Tick(5f);
            float queuedAlong = traffic.VehicleAlongForTests(0);

            target.transform.position = new Vector3(50f, 0f, 0f);
            traffic.Tick(1f);

            Assert.Greater(traffic.VehicleAlongForTests(0), queuedAlong);
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
