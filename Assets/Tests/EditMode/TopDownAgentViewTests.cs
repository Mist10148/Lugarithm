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

            var shiftedSpace = new FakeGridSpace(new Vector3(100f, 200f, 0f), 500);
            view.RebindSpacePreservingPose(shiftedSpace, null, new Vector2Int(6, 7));

            Assert.AreEqual(preservedPosition, go.transform.position);
            Assert.AreEqual(preservedRotation.eulerAngles.z, body.transform.localRotation.eulerAngles.z, 0.001f);
            Assert.AreEqual(3.25f, view.CurrentSpeed, 0.001f);
            Assert.AreEqual(shiftedSpace.SortOrder(new Vector2Int(6, 7)) + 1, body.sortingOrder);
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
