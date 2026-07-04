using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TutorialTreeOccluderPlayModeTests
{
    [UnityTest]
    public IEnumerator TutorialNpcInteraction_ShowsPromptAndOpensDialogue()
    {
        SceneManager.LoadScene("TopDownLevel");
        yield return null;
        yield return null;

        TopDownPlayerController player =
            UnityEngine.Object.FindAnyObjectByType<TopDownPlayerController>();
        GameObject rosa = GameObject.Find("NPC_8_8");
        GameObject toto = GameObject.Find("NPC_16_8");
        GameObject bising = GameObject.Find("NPC_12_24");
        Assert.IsNotNull(player);
        Assert.IsNotNull(rosa);
        Assert.IsNotNull(toto);
        Assert.IsNotNull(bising);

        AssertAnimator(player.GetComponentInChildren<Animator>(), "Townspeople_13_NPC_Animator");
        AssertAnimator(rosa.GetComponentInChildren<Animator>(), "Townspeople_5_NPC_Animator");
        AssertAnimator(toto.GetComponentInChildren<Animator>(), "Townspeople_3_NPC_Animator");
        AssertAnimator(bising.GetComponentInChildren<Animator>(), "Townspeople_15_NPC_Animator");

        TutorialTreeOccluder[] trees =
            UnityEngine.Object.FindObjectsByType<TutorialTreeOccluder>(
                FindObjectsInactive.Include);
        Assert.AreEqual(0, trees.Length);
        foreach (TutorialTreeOccluder tree in trees)
        {
            BoxCollider2D canopyTrigger = tree.GetComponent<BoxCollider2D>();
            Assert.IsNotNull(canopyTrigger);
            Assert.IsTrue(canopyTrigger.isTrigger, $"{tree.name} must remain non-blocking.");
        }

        player.transform.position = toto.transform.position;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.IsNotNull(
            GameObject.Find("PromptBg"),
            "Entering Toto's trigger must show the existing interaction prompt.");

        InteractionTrigger trigger = toto.GetComponent<InteractionTrigger>();
        FieldInfo interactionField = typeof(InteractionTrigger).GetField(
            "OnInteracted",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var interaction = interactionField?.GetValue(trigger) as Action<InteractionTrigger>;
        Assert.IsNotNull(interaction, "The NPC interaction event must remain wired.");

        interaction.Invoke(trigger);
        yield return null;

        DialogueController dialogue =
            UnityEngine.Object.FindAnyObjectByType<DialogueController>(FindObjectsInactive.Include);
        FieldInfo rootField = typeof(DialogueController).GetField(
            "root",
            BindingFlags.Instance | BindingFlags.NonPublic);
        GameObject dialogueRoot = rootField?.GetValue(dialogue) as GameObject;

        Assert.IsTrue(player.InputLocked);
        Assert.IsNotNull(dialogueRoot);
        Assert.IsTrue(dialogueRoot.activeInHierarchy);
    }

    static void AssertAnimator(Animator animator, string expectedControllerName)
    {
        Assert.IsNotNull(animator);
        Assert.IsNotNull(animator.runtimeAnimatorController);
        Assert.AreEqual(expectedControllerName, animator.runtimeAnimatorController.name);
    }

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

        UnityEngine.Object.Destroy(tree);
        UnityEngine.Object.Destroy(player);
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

        UnityEngine.Object.Destroy(tree);
        UnityEngine.Object.Destroy(player);
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
