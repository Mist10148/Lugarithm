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

            int cap = traffic.VehicleCapForTests;
            for (int i = 1; i <= cap; i++)
            {
                Assert.IsTrue(traffic.ForceSpawnForTests(0f));
                Assert.AreEqual(i, traffic.ActiveVehicleCount);
            }

            Assert.IsFalse(traffic.ForceSpawnForTests(0f),
                "spawns past the active-vehicle cap must be refused");
            Assert.AreEqual(cap, traffic.ActiveVehicleCount);
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
            // Route runs along +x, so route-left is +y; a side=+1 car sits at y=+1.35.
            // The jeepney must occupy that lane to count as an obstacle.
            target.transform.position = new Vector3(30f, 1.35f, 0f);
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
            target.transform.position = new Vector3(30f, 1.35f, 0f);
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);
            Assert.IsTrue(traffic.ForceSpawnAtForTests(20f, 1f, 3f));
            traffic.Tick(5f);
            float queuedAlong = traffic.VehicleAlongForTests(0);

            target.transform.position = new Vector3(50f, 1.35f, 0f);
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

    [Test]
    public void DefaultSpawnSides_FollowRightHandDriving()
    {
        GameObject root = new GameObject("TrafficLaneRoot");
        GameObject target = new GameObject("Target");
        try
        {
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);

            // Default side (NaN) resolves from direction: forward keeps route-right
            // (negative left-offset), oncoming keeps route-left (positive).
            Assert.IsTrue(traffic.ForceSpawnAtForTests(30f, direction: 1));
            Assert.IsTrue(traffic.ForceSpawnAtForTests(50f, direction: -1));

            Assert.Less(traffic.VehicleSideOffsetForTests(0), 0f,
                "forward traffic should keep to the right side of the road");
            Assert.Greater(traffic.VehicleSideOffsetForTests(1), 0f,
                "oncoming traffic should keep to the left side of the road");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void OncomingCar_QueuesBehindSlowerOncomingCar()
    {
        GameObject root = new GameObject("TrafficOncomingQueueRoot");
        GameObject target = new GameObject("Target");
        try
        {
            target.transform.position = Vector3.zero;
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);

            // Oncoming cars travel toward lower along; the trailing car starts higher.
            Assert.IsTrue(traffic.ForceSpawnAtForTests(30f, 1f, 0.1f, direction: -1));
            Assert.IsTrue(traffic.ForceSpawnAtForTests(40f, 1f, 4f, direction: -1));

            for (int i = 0; i < 60; i++)
                traffic.Tick(0.1f);

            float gap = traffic.VehicleAlongForTests(1) - traffic.VehicleAlongForTests(0);
            Assert.GreaterOrEqual(gap, traffic.FollowDistanceForTests - 0.05f,
                "a faster oncoming car must queue behind a slower one instead of driving through it");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void ForwardCar_IgnoresJeepneyInAnotherLane()
    {
        GameObject root = new GameObject("TrafficOtherLaneRoot");
        GameObject target = new GameObject("Target");
        try
        {
            // Jeepney parked in the oncoming (route-left, y=+1.35) lane; the forward
            // car cruises the route-right lane and should pass without queuing.
            target.transform.position = new Vector3(30f, 1.35f, 0f);
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);
            Assert.IsTrue(traffic.ForceSpawnAtForTests(20f, -1f, 3f));

            traffic.Tick(5f);

            Assert.Greater(traffic.VehicleAlongForTests(0),
                30f - traffic.FollowDistanceForTests + 0.5f,
                "a forward car should not stop for a jeepney sitting in a different lane");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void OncomingCar_StopsForJeepneyInItsLane()
    {
        GameObject root = new GameObject("TrafficHeadOnRoot");
        GameObject target = new GameObject("Target");
        try
        {
            // Jeepney drives in the oncoming (route-left) lane; the oncoming car
            // must brake instead of driving through it.
            target.transform.position = new Vector3(20f, 1.35f, 0f);
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);
            Assert.IsTrue(traffic.ForceSpawnAtForTests(40f, 1f, 4f, direction: -1));

            for (int i = 0; i < 100; i++)
                traffic.Tick(0.1f);

            Assert.GreaterOrEqual(traffic.VehicleAlongForTests(0),
                20f + traffic.FollowDistanceForTests - 0.05f,
                "an oncoming car must stop short of a jeepney occupying its lane");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void ForwardCar_DrivesOffRouteEndAndDespawns_InsteadOfParking()
    {
        GameObject root = new GameObject("TrafficRouteEndRoot");
        GameObject target = new GameObject("Target");
        try
        {
            target.transform.position = new Vector3(70f, 0f, 0f);
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(40f, 0f));
            traffic.InitManual(route, root.transform, target.transform, null);
            Assert.IsTrue(traffic.ForceSpawnAtForTests(76f, cruiseSpeed: 3f));

            for (int i = 0; i < 15; i++)
                traffic.Tick(0.2f);

            for (int i = 0; i < traffic.ActiveVehicleCount; i++)
            {
                float along = traffic.VehicleAlongForTests(i);
                Assert.IsFalse(along >= 74f && along <= 81f,
                    "no car should be parked at the route frontier — it must drive off and despawn");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void OncomingCar_RunsPastRouteStartAndDespawns()
    {
        GameObject root = new GameObject("TrafficRouteStartRoot");
        GameObject target = new GameObject("Target");
        try
        {
            target.transform.position = new Vector3(30f, 0f, 0f);
            RoadTrafficController traffic = root.AddComponent<RoadTrafficController>();
            RouteContext route = RouteWithStops(root.transform, new Vector2(100f, 100f));
            traffic.InitManual(route, root.transform, target.transform, null);
            Assert.IsTrue(traffic.ForceSpawnAtForTests(6f, cruiseSpeed: 4f, direction: -1));

            for (int i = 0; i < 20; i++)
                traffic.Tick(0.1f);

            for (int i = 0; i < traffic.ActiveVehicleCount; i++)
            {
                Assert.IsFalse(traffic.VehicleDirectionForTests(i) < 0 &&
                               traffic.VehicleAlongForTests(i) < 10f,
                    "no oncoming car should be parked near the route start — it must exit and despawn");
            }
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
