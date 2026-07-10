using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SceneTemplateLibraryTests
{
    static readonly string[] TemplateSprites =
    {
        "town_vertical_0", "town_horizontal_0", "town_turn_s_e_0",
        "town_turn_s_w_1", "town_turn_e_n_0", "town_turn_w_n_1",
    };

    static SceneChunkPlan FindPlan(Vector2 headingIn, Vector2 forward, string sprite)
    {
        for (int seed = 0; seed < 500; seed++)
        {
            SceneChunkPlan plan = SceneTemplateLibrary.PlanChunk(
                Vector2.zero, headingIn, forward, new List<ScenePlacement>(),
                forceStraight: false, seed: seed, order: 1);
            if (plan != null && plan.placement.sprite == sprite)
                return plan;
        }
        Assert.Fail($"No deterministic seed selected template '{sprite}'.");
        return null;
    }

    [TestCase("town_vertical_0", 0f, 1f)]
    [TestCase("town_horizontal_0", 1f, 0f)]
    public void StraightTemplates_StayOnOneCardinalAxis(string sprite, float x, float y)
    {
        Vector2 heading = new Vector2(x, y);
        SceneChunkPlan plan = SceneTemplateLibrary.PlanChunk(
            Vector2.zero, heading, heading, new List<ScenePlacement>(),
            forceStraight: true, seed: 17, order: 0);

        Assert.NotNull(plan);
        Assert.AreEqual(sprite, plan.placement.sprite);
        Assert.That(Vector2.Dot(plan.exit - Vector2.zero, heading), Is.GreaterThan(0f));
        Assert.That(Mathf.Abs(Vector2.Dot(plan.exit, new Vector2(-heading.y, heading.x))),
            Is.LessThan(0.001f));
    }

    [TestCase("town_turn_s_e_0", 0f, 1f, 0f, 1f)]
    [TestCase("town_turn_s_w_1", 0f, 1f, 0f, 1f)]
    [TestCase("town_turn_e_n_0", -1f, 0f, 0f, 1f)]
    [TestCase("town_turn_w_n_1", 1f, 0f, 0f, 1f)]
    public void TurnTemplates_UseCardinalGraphAndExplicitCurvedDrivePath(
        string sprite, float inX, float inY, float forwardX, float forwardY)
    {
        SceneChunkPlan plan = FindPlan(new Vector2(inX, inY),
                                       new Vector2(forwardX, forwardY), sprite);

        Assert.Contains(true, plan.corners);
        Assert.That(plan.drivePath.Count, Is.GreaterThan(4),
            "turns must use the generated painted-road samples, not one inferred corner");
        Vector2 previous = Vector2.zero;
        foreach (Vector2 vertex in plan.vertices)
        {
            Vector2 delta = vertex - previous;
            Assert.IsTrue(Mathf.Abs(delta.x) < 0.001f || Mathf.Abs(delta.y) < 0.001f,
                $"Automation graph edge {previous}->{vertex} must remain cardinal");
            previous = vertex;
        }
        Assert.That(Vector2.Distance(plan.drivePath[plan.drivePath.Count - 1], plan.exit),
            Is.LessThan(0.001f));
    }

    [Test]
    public void Planning_IsDeterministic()
    {
        var existing = new List<ScenePlacement>();
        SceneChunkPlan first = SceneTemplateLibrary.PlanChunk(
            Vector2.zero, Vector2.up, Vector2.up, existing, false, 81, 1);
        SceneChunkPlan again = SceneTemplateLibrary.PlanChunk(
            Vector2.zero, Vector2.up, Vector2.up, existing, false, 81, 1);

        Assert.AreEqual(first.placement.sprite, again.placement.sprite);
        Assert.That(Vector2.Distance(first.exit, again.exit), Is.LessThan(0.001f));

    }

    [Test]
    public void ConsecutiveChunks_ShareTheSameSeamPoint()
    {
        var existing = new List<ScenePlacement>();
        SceneChunkPlan first = SceneTemplateLibrary.PlanChunk(
            Vector2.zero, Vector2.up, Vector2.up, existing, true, 3, 0);
        existing.Add(first.placement);
        SceneChunkPlan second = SceneTemplateLibrary.PlanChunk(
            first.exit, first.exitHeading, Vector2.up, existing, false, 9, 1);

        Assert.NotNull(second);
        Vector2 secondPathStart = second.extraSegments.Count > 0
            ? second.extraSegments[0].a
            : first.exit;
        Assert.That(Vector2.Distance(first.exit, secondPathStart), Is.LessThan(0.001f));
    }

    [Test]
    public void EveryTemplate_CoversWideDrivingViewport()
    {
        const float widestSupportedAspect = 2.1f;
        float viewportWidth = SceneTemplateLibrary.RecommendedCameraOrthoSize * 2f
                              * widestSupportedAspect;
        foreach (string sprite in TemplateSprites)
        {
            Vector2 size = SceneTemplateLibrary.SizeOf(sprite);
            Assert.That(size.x, Is.GreaterThan(viewportWidth),
                $"{sprite} must cover the camera without exposing backing ground");
            Assert.That(size.y, Is.GreaterThan(
                SceneTemplateLibrary.RecommendedCameraOrthoSize * 2f),
                $"{sprite} must cover the camera vertically");
        }
    }
}
