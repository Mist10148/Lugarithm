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
        GameObject rosa = GameObject.Find("NPC_8_27");
        GameObject toto = GameObject.Find("NPC_16_27");
        GameObject bising = GameObject.Find("NPC_12_11");
        Assert.IsNotNull(player);
        Assert.IsNotNull(rosa);
        Assert.IsNotNull(toto);
        Assert.IsNotNull(bising);

        GameObject jeepStop = GameObject.Find("JeepStop_12_3");
        Assert.IsNotNull(jeepStop);
        Assert.That(Vector2.Distance(player.transform.position, jeepStop.transform.position),
            Is.LessThan(0.01f), "Player must begin at the jeep boarding trigger.");

        GameObject background = GameObject.Find("HeritagePlazaBackground");
        Assert.IsNotNull(background);
        Assert.IsNull(background.GetComponent<PolygonCollider2D>(),
            "The full plaza image must not act as a filled physics collider.");
        Assert.IsNotNull(GameObject.Find("MapBoundaries"));

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
        DisableOtherPlayers(player);

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
    public IEnumerator CompletingEveryObjective_SpawnsCollectibleArtifactAndUpdatesTracker()
    {
        SceneManager.LoadScene("TopDownLevel");
        yield return null;
        yield return null;

        TopDownLevelController controller =
            UnityEngine.Object.FindAnyObjectByType<TopDownLevelController>();
        Assert.IsNotNull(controller);

        FieldInfo stationDefsField = typeof(TopDownLevelController).GetField(
            "_stationDefs", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo markSolved = typeof(TopDownLevelController).GetMethod(
            "MarkStationSolved", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(stationDefsField);
        Assert.IsNotNull(markSolved);

        var stationDefs = stationDefsField.GetValue(controller) as System.Collections.IDictionary;
        Assert.IsNotNull(stationDefs);
        Assert.AreEqual(6, stationDefs.Count);
        foreach (System.Collections.DictionaryEntry entry in stationDefs)
            markSolved.Invoke(controller, new[] { entry.Key, entry.Value });

        var solvedField = typeof(TopDownLevelController).GetField(
            "_solvedStations", BindingFlags.Instance | BindingFlags.NonPublic);
        var mainField = typeof(TopDownLevelController).GetField(
            "_mainQuestId", BindingFlags.Instance | BindingFlags.NonPublic);
        var sideCountField = typeof(TopDownLevelController).GetField(
            "_sideObjectiveCount", BindingFlags.Instance | BindingFlags.NonPublic);
        object solved = solvedField?.GetValue(controller);
        Assert.IsNotNull(solved);
        Assert.AreEqual(6, solved.GetType().GetProperty("Count")?.GetValue(solved),
            "Every tutorial station must contribute a unique solved id.");
        Assert.AreEqual("tut_coding_maze", mainField?.GetValue(controller));
        Assert.AreEqual(5, sideCountField?.GetValue(controller));

        yield return null;

        InteractionTrigger artifact = null;
        foreach (InteractionTrigger trigger in
                 UnityEngine.Object.FindObjectsByType<InteractionTrigger>(FindObjectsInactive.Include))
        {
            if (trigger.EntityType == EntityType.Artifact)
            {
                artifact = trigger;
                break;
            }
        }
        Assert.IsNotNull(artifact);
        Assert.IsTrue(artifact.gameObject.activeSelf);

        FieldInfo statusRootField = typeof(TopDownLevelController).GetField(
            "artifactStatusRoot", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo statusMarkField = typeof(TopDownLevelController).GetField(
            "artifactStatusMark", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo statusCheckField = typeof(TopDownLevelController).GetField(
            "artifactStatusCheck", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(statusRootField);
        Assert.IsNotNull(statusMarkField);
        Assert.IsNotNull(statusCheckField);

        GameObject statusRoot = statusRootField.GetValue(controller) as GameObject;
        object statusMark = statusMarkField.GetValue(controller);
        GameObject statusCheck = statusCheckField.GetValue(controller) as GameObject;
        Assert.IsNotNull(statusRoot);
        Assert.IsNotNull(statusMark);
        Assert.IsNotNull(statusCheck);
        Assert.IsTrue(statusRoot.activeInHierarchy);
        Assert.AreEqual("X", ReadText(statusMark));
        Assert.IsFalse(statusCheck.activeSelf);

        MethodInfo collectArtifact = typeof(TopDownLevelController).GetMethod(
            "HandleArtifactInteraction", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(collectArtifact);
        collectArtifact.Invoke(controller, new object[] { artifact });

        Assert.IsFalse(artifact.gameObject.activeSelf);
        Assert.IsFalse((statusMark as Component).gameObject.activeSelf);
        Assert.IsTrue(statusCheck.activeSelf);

        // Keep the fixture's standalone tree tests isolated from the runtime map
        // boundary colliders created by TopDownLevelController.
        Scene cleanupScene = SceneManager.CreateScene("ArtifactTestCleanup");
        SceneManager.SetActiveScene(cleanupScene);
        yield return SceneManager.UnloadSceneAsync("TopDownLevel");
    }

    static string ReadText(object label)
    {
        PropertyInfo textProperty = label?.GetType().GetProperty("text");
        Assert.IsNotNull(textProperty);
        return textProperty.GetValue(label) as string;
    }

    [UnityTest]
    public IEnumerator PlayerInFrontOfTree_RemainsOpaqueAndRendersInFront()
    {
        GameObject tree = CreateTree(out SpriteRenderer renderer);
        GameObject player = CreatePlayer(new Vector2(0f, -0.5f));
        DisableOtherPlayers(player);

        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForSecondsRealtime(0.2f);

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

    static void DisableOtherPlayers(GameObject fixturePlayer)
    {
        foreach (TopDownPlayerController controller in
                 UnityEngine.Object.FindObjectsByType<TopDownPlayerController>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (controller.gameObject != fixturePlayer)
                controller.gameObject.SetActive(false);
        }
    }
}
