using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TutorialTreeOccluderPlayModeTests
{
    [UnityTest]
    public IEnumerator PlayerBehindTree_FadesThenRestoresAfterExit()
    {
        GameObject tree = CreateTree(out SpriteRenderer renderer);
        GameObject player = CreatePlayer(new Vector2(0f, 1f));

        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForSecondsRealtime(0.16f);

        Assert.That(renderer.color.a, Is.EqualTo(0.42f).Within(0.03f));
        Assert.AreEqual(6, renderer.sortingOrder);

        player.transform.position = new Vector2(4f, 1f);
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForSecondsRealtime(0.2f);

        Assert.That(renderer.color.a, Is.EqualTo(1f).Within(0.01f));
        Assert.AreEqual(6, renderer.sortingOrder);

        Object.Destroy(tree);
        Object.Destroy(player);
    }

    [UnityTest]
    public IEnumerator PlayerInFrontOfTree_RemainsOpaqueAndRendersInFront()
    {
        GameObject tree = CreateTree(out SpriteRenderer renderer);
        GameObject player = CreatePlayer(new Vector2(0f, -0.5f));

        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.That(renderer.color.a, Is.EqualTo(1f).Within(0.01f));
        Assert.AreEqual(4, renderer.sortingOrder);

        Object.Destroy(tree);
        Object.Destroy(player);
    }

    static GameObject CreateTree(out SpriteRenderer renderer)
    {
        var tree = new GameObject("Tree");
        renderer = tree.AddComponent<SpriteRenderer>();

        var trigger = tree.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(3f, 4f);

        var occluder = tree.AddComponent<TutorialTreeOccluder>();
        occluder.Configure(renderer);
        return tree;
    }

    static GameObject CreatePlayer(Vector2 position)
    {
        var player = new GameObject("Player");
        player.transform.position = position;

        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;

        player.AddComponent<BoxCollider2D>();
        player.AddComponent<TopDownPlayerController>();
        return player;
    }
}
