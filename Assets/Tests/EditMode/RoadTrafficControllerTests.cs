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

            Assert.IsTrue(traffic.ForceSpawnForTests(0f));
            Assert.AreEqual(4, traffic.ActiveVehicleCount);

            Assert.IsTrue(traffic.ForceSpawnForTests(0f));
            Assert.AreEqual(5, traffic.ActiveVehicleCount);

            Assert.IsFalse(traffic.ForceSpawnForTests(0f));
            Assert.AreEqual(5, traffic.ActiveVehicleCount);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void Tick_TopsUpToMinimumVisibleTraffic()
    {
        GameObject root = new GameObject("TrafficTopUpRoot");
        GameObject target = new GameObject("Target");
        try
        {
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);

            traffic.Tick(0.1f);

            Assert.GreaterOrEqual(traffic.ActiveVehicleCount, traffic.MinActiveVehiclesForTests);
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
    public void Cornering_EasesLaneTowardCenter_AndRotatesThroughTurn()
    {
        GameObject root = new GameObject("TrafficCornerRoot");
        GameObject target = new GameObject("Target");
        try
        {
            target.transform.position = Vector3.zero;
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = LRoute();
            traffic.InitManual(route, root.transform, target.transform, null);
            Assert.IsTrue(traffic.ForceSpawnAtForTests(37f, 1f, 3f));

            float beforeSide = Mathf.Abs(traffic.VehicleSideOffsetForTests(0));
            traffic.Tick(0.5f);

            Assert.Less(Mathf.Abs(traffic.VehicleSideOffsetForTests(0)), beforeSide,
                "traffic cars should tuck toward the centerline near corners like the manual jeepney");

            traffic.Clear();
            Assert.IsTrue(traffic.ForceSpawnAtForTests(39.8f, 1f, 3f));
            float beforeAngle = traffic.VehicleRotationForTests(0);
            traffic.Tick(0.1f);
            float afterAngle = traffic.VehicleRotationForTests(0);

            Assert.Greater(afterAngle, beforeAngle,
                "rotation should begin turning toward the new segment instead of staying locked to the old heading");
            Assert.Less(afterAngle, -5f,
                "rotation should ease through the turn instead of snapping all the way to the new heading in one tick");
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
    public void OncomingCar_MovesTowardAndPastTarget()
    {
        GameObject root = new GameObject("TrafficOncomingRoot");
        GameObject target = new GameObject("Target");
        try
        {
            target.transform.position = new Vector3(20f, 0f, 0f);
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);
            Assert.IsTrue(traffic.ForceSpawnAtForTests(40f, 1f, 4f, direction: -1));

            Assert.AreEqual(-1, traffic.VehicleDirectionForTests(0));
            float before = traffic.VehicleAlongForTests(0);
            traffic.Tick(1f);

            Assert.Less(traffic.VehicleAlongForTests(0), before,
                "oncoming traffic should travel opposite the route direction so it visibly passes the jeepney");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void SameDirectionCar_StillMovesForward()
    {
        GameObject root = new GameObject("TrafficSameDirectionRoot");
        GameObject target = new GameObject("Target");
        try
        {
            target.transform.position = Vector3.zero;
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);
            Assert.IsTrue(traffic.ForceSpawnAtForTests(20f, -1f, 3f, direction: 1));

            Assert.AreEqual(1, traffic.VehicleDirectionForTests(0));
            float before = traffic.VehicleAlongForTests(0);
            traffic.Tick(1f);

            Assert.Greater(traffic.VehicleAlongForTests(0), before);
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

    static RouteContext LRoute()
    {
        return new RouteContext
        {
            Waypoints = new[]
            {
                Vector2.zero,
                new Vector2(40f, 0f),
                new Vector2(40f, 40f),
            },
            TotalLength = 80f,
            Zones = new StopZone[0],
        };
    }
}
