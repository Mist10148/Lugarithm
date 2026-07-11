using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TopDownAgentViewTests
{
    [Test]
    public void RebindSpacePreservingPose_DoesNotSnapRotateOrResetCruise()
    {
        var go = new GameObject("TopDownAgentViewTest");
        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(go.transform, false);
        var body = bodyGo.AddComponent<SpriteRenderer>();

        try
        {
            var view = go.AddComponent<TopDownAgentView>();
            view.body = body;

            var initialSpace = new FakeGridSpace(Vector3.zero, 10);
            view.Init(initialSpace, new Vector2Int(1, 2), facing: 1);

            Vector3 preservedPosition = new Vector3(12.5f, -3.25f, 0f);
            Quaternion preservedRotation = Quaternion.Euler(0f, 0f, 37f);
            go.transform.position = preservedPosition;
            body.transform.localRotation = preservedRotation;

            SetPrivateFloat(view, "_cruiseSpeed", 3.25f);

            SetPrivateFloat(view, "_laneScalar", -1.35f);
            SetPrivateFloat(view, "_laneScalarTarget", -1.35f);
            SetPrivateFloat(view, "_laneScalarVel", 0.4f);

            var shiftedSpace = new FakeGridSpace(new Vector3(100f, 200f, 0f), 500);
            view.RebindSpacePreservingPose(shiftedSpace, null, new Vector2Int(6, 7));

            Assert.AreEqual(preservedPosition, go.transform.position);
            Assert.AreEqual(preservedRotation.eulerAngles.z, body.transform.localRotation.eulerAngles.z, 0.001f);
            Assert.AreEqual(3.25f, view.CurrentSpeed, 0.001f);
            Assert.AreEqual(shiftedSpace.SortOrder(new Vector2Int(6, 7)) + 1, body.sortingOrder);

            // Streaming rebinds re-pin by world pose: the lane channel must carry
            // across untouched or the offset visibly pops at batch boundaries.
            Assert.AreEqual(-1.35f, GetPrivateFloat(view, "_laneScalar"), 0.001f);
            Assert.AreEqual(-1.35f, GetPrivateFloat(view, "_laneScalarTarget"), 0.001f);
            Assert.AreEqual(0.4f, GetPrivateFloat(view, "_laneScalarVel"), 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void LaneVisualOffset_DefaultsToPlaceholderMetrics()
    {
        var go = new GameObject("TopDownAgentViewLaneDefault");
        try
        {
            var view = go.AddComponent<TopDownAgentView>();
            Assert.AreEqual(RoadMetrics.PlaceholderLaneOffset, view.LaneVisualOffset, 0.001f,
                "bare views (tests, authored scenes) must keep the placeholder lane spacing");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ApplySoftTrafficContact_BleedsCruiseSpeed()
    {
        var go = new GameObject("TopDownAgentViewContact");
        try
        {
            var view = go.AddComponent<TopDownAgentView>();
            SetPrivateFloat(view, "_cruiseSpeed", 4f);

            view.ApplySoftTrafficContact(new Vector2(0.5f, 0f));

            Assert.AreEqual(4f * 0.55f, view.CurrentSpeed, 0.001f,
                "a traffic contact must bleed cruise speed like the manual jeepney bump");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SnapTo_ClearsTrafficGateAndContactState()
    {
        var go = new GameObject("TopDownAgentViewSnap");
        try
        {
            var view = go.AddComponent<TopDownAgentView>();
            view.Init(new FakeGridSpace(Vector3.zero, 10), new Vector2Int(1, 2), facing: 1);
            view.SetTrafficFollowGate(1.5f);

            view.SnapTo(new Vector2Int(4, 4), 0);

            FieldInfo gate = typeof(TopDownAgentView).GetField("_followGateRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(gate);
            Assert.IsTrue(float.IsPositiveInfinity((float)gate.GetValue(view)),
                "a teleport must release the traffic follow gate — it belongs to the old position");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SnapTo_SetsLaneOffsetInstantly()
    {
        var go = new GameObject("TopDownAgentViewSnapLane");
        try
        {
            var view = go.AddComponent<TopDownAgentView>();
            view.LaneVisualOffset = 1.35f;
            view.LaneCardinalSource = () => 2;   // lane toward South
            var space = new FakeGridSpace(Vector3.zero, 10);

            view.Init(space, new Vector2Int(3, 3), facing: 1);   // heading East

            Vector3 expected = space.CellToWorld(new Vector2Int(3, 3))
                             + new Vector3(0f, -1.35f, 0f);      // South lane offset
            Assert.AreEqual(expected.x, go.transform.position.x, 0.001f);
            Assert.AreEqual(expected.y, go.transform.position.y, 0.001f);
            Assert.AreEqual(0f, GetPrivateFloat(view, "_laneScalarVel"), 0.001f,
                "teleports must land settled — no residual lateral velocity");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void LaneSign_ResolvesLaneSide_ForAllHeadings()
    {
        Vector2 n = Vector2.up, e = Vector2.right, s = Vector2.down, w = Vector2.left;

        // Right-hand home lanes (lane cardinal = right of travel) → -1.
        Assert.AreEqual(-1f, TopDownAgentView.LaneSign(e, n));   // heading N, lane E
        Assert.AreEqual(-1f, TopDownAgentView.LaneSign(s, e));   // heading E, lane S
        Assert.AreEqual(-1f, TopDownAgentView.LaneSign(w, s));   // heading S, lane W
        Assert.AreEqual(-1f, TopDownAgentView.LaneSign(n, w));   // heading W, lane N

        // Passing lanes (left of travel) → +1.
        Assert.AreEqual(1f, TopDownAgentView.LaneSign(w, n));
        Assert.AreEqual(1f, TopDownAgentView.LaneSign(n, e));
        Assert.AreEqual(1f, TopDownAgentView.LaneSign(e, s));
        Assert.AreEqual(1f, TopDownAgentView.LaneSign(s, w));

        // Chamfer diagonals (±0.707 dot) still resolve the correct side.
        Vector2 ne = new Vector2(1f, 1f).normalized;
        Assert.AreEqual(-1f, TopDownAgentView.LaneSign(s, ne));
        Assert.AreEqual(1f, TopDownAgentView.LaneSign(n, ne));
    }

    static float GetPrivateFloat(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, fieldName);
        return (float)field.GetValue(target);
    }

    static void SetPrivateFloat(object target, string fieldName, float value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, fieldName);
        field.SetValue(target, value);
    }

    class FakeGridSpace : IGridSpace
    {
        readonly Vector3 _origin;
        readonly int _sortBase;

        public FakeGridSpace(Vector3 origin, int sortBase)
        {
            _origin = origin;
            _sortBase = sortBase;
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return _origin + new Vector3(cell.x, cell.y, 0f);
        }

        public int SortOrder(Vector2Int cell)
        {
            return _sortBase + cell.y * 100 + cell.x;
        }

        public Vector2 FacingDirection(int facing)
        {
            switch (((facing % 4) + 4) % 4)
            {
                case 0: return Vector2.up;
                case 1: return Vector2.right;
                case 2: return Vector2.down;
                default: return Vector2.left;
            }
        }
    }
}
