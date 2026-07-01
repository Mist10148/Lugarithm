using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EditMode tests verifying that the code-based minigames surface their hint
/// button after the player struggles.
/// </summary>
public class MinigameHintRevealTests
{
    // -------------------------------------------------------------------------
    // CodeOrderMinigame
    // -------------------------------------------------------------------------

    [Test]
    public void CodeOrderMinigame_RevealsHint_AfterTwoWrongRuns()
    {
        CodeOrderMinigame game = SetupCodeOrderMinigame(out Button hintButton);

        // First wrong run.
        InvokeMethod(game, "OnRun");
        Assert.IsFalse(hintButton.gameObject.activeSelf, "hint should stay hidden after one mistake");

        // Second wrong run.
        InvokeMethod(game, "OnRun");
        Assert.IsTrue(hintButton.gameObject.activeSelf, "hint should appear after two mistakes");

        UnityEngine.Object.DestroyImmediate(game.gameObject);
    }

    CodeOrderMinigame SetupCodeOrderMinigame(out Button hintButton)
    {
        var rootGo = new GameObject("CodeOrderRoot");
        var game = rootGo.AddComponent<CodeOrderMinigame>();

        hintButton = CreateButton(rootGo.transform, "HintButton");
        var hintLabel = CreateText(rootGo.transform, "HintLabel");
        var runButton = CreateButton(rootGo.transform, "RunButton");
        var quitButton = CreateButton(rootGo.transform, "QuitButton");

        int maxLines = CodeOrderMinigame.MaxLines;
        var cardLabels = new TMP_Text[maxLines];
        var cardBackgrounds = new Image[maxLines];
        var upButtons = new Button[maxLines];
        var downButtons = new Button[maxLines];

        for (int i = 0; i < maxLines; i++)
        {
            var row = new GameObject($"Card_{i}").AddComponent<RectTransform>();
            row.SetParent(rootGo.transform, false);
            cardLabels[i] = CreateText(row, "Label");
            cardBackgrounds[i] = row.gameObject.AddComponent<Image>();
            upButtons[i] = CreateButton(row, "Up");
            downButtons[i] = CreateButton(row, "Down");
        }

        SetField(game, "root", rootGo);
        SetField(game, "hintButton", hintButton);
        SetField(game, "hintLabel", hintLabel);
        SetField(game, "runButton", runButton);
        SetField(game, "quitButton", quitButton);
        SetField(game, "cardLabels", cardLabels);
        SetField(game, "cardBackgrounds", cardBackgrounds);
        SetField(game, "upButtons", upButtons);
        SetField(game, "downButtons", downButtons);

        var def = new MinigameStationDef
        {
            id = "tut_code",
            title = "Tutorial Code",
            concept = "sequencing",
            type = MinigameStationType.Coding,
            kind = MinigamePuzzleKind.Coding,
        };

        InvokeMethod(game, "Begin", def, (Action)null, (Action)null);

        // Sanity: the puzzle loaded.
        Assert.IsTrue(rootGo.activeSelf);
        return game;
    }

    // -------------------------------------------------------------------------
    // MazeRepairMinigame
    // -------------------------------------------------------------------------

    [Test]
    public void MazeRepairMinigame_RevealsHint_AfterTwoTimeouts()
    {
        MazeRepairMinigame game = SetupMazeRepairMinigame(out Button hintButton);

        InvokeMethod(game, "Finish", true);   // timed out once
        Assert.IsFalse(hintButton.gameObject.activeSelf, "hint should stay hidden after one timeout");

        // Finish() sets _active false; re-arm so the next timeout counts.
        SetField(game, "_active", true);
        InvokeMethod(game, "Finish", true);   // timed out twice
        Assert.IsTrue(hintButton.gameObject.activeSelf, "hint should appear after two timeouts");

        UnityEngine.Object.DestroyImmediate(game.gameObject);
    }

    [Test]
    public void MazeRepairMinigame_RevealsHint_AfterThreeRunAttempts()
    {
        MazeRepairMinigame game = SetupMazeRepairMinigame(out Button hintButton);

        IncrementRunAttempts(game);
        Assert.IsFalse(hintButton.gameObject.activeSelf, "hint should stay hidden after one run attempt");

        IncrementRunAttempts(game);
        Assert.IsFalse(hintButton.gameObject.activeSelf, "hint should stay hidden after two run attempts");

        IncrementRunAttempts(game);
        Assert.IsTrue(hintButton.gameObject.activeSelf, "hint should appear after three run attempts");

        UnityEngine.Object.DestroyImmediate(game.gameObject);
    }

    MazeRepairMinigame SetupMazeRepairMinigame(out Button hintButton)
    {
        var rootGo = new GameObject("MazeRepairRoot");
        var game = rootGo.AddComponent<MazeRepairMinigame>();

        hintButton = CreateButton(rootGo.transform, "HintButton");
        var hintLabel = CreateText(rootGo.transform, "HintLabel");

        SetField(game, "root", rootGo);
        SetField(game, "hintButton", hintButton);
        SetField(game, "hintLabel", hintLabel);

        InvokeMethod(game, "Show", BreakdownFault.Engine, 12345, (Action<MinigameResult>)null);
        Assert.IsTrue(rootGo.activeSelf);
        return game;
    }

    void IncrementRunAttempts(MazeRepairMinigame game)
    {
        int runs = (int)GetField(game, "_runAttempts");
        SetField(game, "_runAttempts", runs + 1);
        InvokeMethod(game, "RevealHintAfterStruggle");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    static Button CreateButton(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        go.SetActive(false);   // minigames start with the hint button hidden
        return btn;
    }

    static TMP_Text CreateText(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<TextMeshProUGUI>();
    }

    static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(field, $"{target.GetType().Name} missing field {name}");
        field.SetValue(target, value);
    }

    static object GetField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(field, $"{target.GetType().Name} missing field {name}");
        return field.GetValue(target);
    }

    static void InvokeMethod(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(method, $"{target.GetType().Name} missing method {methodName}");
        method.Invoke(target, args);
    }
}
