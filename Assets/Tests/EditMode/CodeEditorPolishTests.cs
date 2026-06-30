using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class CodeEditorPolishTests
{
    [Test]
    public void CodeEditor_ComputesFoldRangesAndPreservesSource()
    {
        var editorGo = new GameObject("Editor");
        var inputGo = new GameObject("Input");
        try
        {
            var editor = editorGo.AddComponent<CodeEditorController>();
            var input = inputGo.AddComponent<TMP_InputField>();
            editor.input = input;

            const string source =
                "def drive():\n" +
                "    moveForward()\n" +
                "    if frontIsClear():\n" +
                "        moveForward()\n" +
                "moveForward()";

            editor.SetSource(source);
            Assert.AreEqual(2, editor.FoldRangeCount);

            editor.ToggleFold(1);

            Assert.AreEqual(source, editor.Source);
            Assert.AreEqual(1, editor.FoldedHeaderCount);
        }
        finally
        {
            Object.DestroyImmediate(editorGo);
            Object.DestroyImmediate(inputGo);
        }
    }

    [Test]
    public void CodeEditor_FoldedLineNumberTextUsesAsciiPlaceholder()
    {
        var editorGo = new GameObject("Editor");
        var inputGo = new GameObject("Input");
        try
        {
            var editor = editorGo.AddComponent<CodeEditorController>();
            var input = inputGo.AddComponent<TMP_InputField>();
            editor.input = input;

            editor.SetSource("if frontIsClear():\n    moveForward()");
            editor.ToggleFold(1);

            string lineNumbers = editor.BuildLineNumberText(2);
            StringAssert.Contains("...", lineNumbers);
            Assert.IsFalse(lineNumbers.Contains("â"));
        }
        finally
        {
            Object.DestroyImmediate(editorGo);
            Object.DestroyImmediate(inputGo);
        }
    }

    [Test]
    public void Autocomplete_FiltersAgentApiToUnlockedVocabulary()
    {
        var go = new GameObject("Autocomplete");
        try
        {
            var autocomplete = go.AddComponent<CodeAutocompleteController>();
            autocomplete.SetVocabulary(
                new[] { "moveForward" },
                new[] { "frontIsClear" },
                new[] { "seatsLeft" });

            List<string> moveSuggestions = autocomplete.BuildSuggestionTexts("mo");
            CollectionAssert.Contains(moveSuggestions, "moveForward");
            CollectionAssert.DoesNotContain(moveSuggestions, "moveLeft");

            List<string> driveSuggestions = autocomplete.BuildSuggestionTexts("dr");
            CollectionAssert.DoesNotContain(driveSuggestions, "driveToNextStop");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Autocomplete_IncludesVariablesAndUserFunctions()
    {
        var go = new GameObject("Autocomplete");
        try
        {
            var autocomplete = go.AddComponent<CodeAutocompleteController>();

            List<string> suggestions = autocomplete.BuildSuggestionTexts(
                "ha",
                new List<string> { "hasFare" },
                new List<string> { "handlePassengers" });

            CollectionAssert.Contains(suggestions, "hasFare");
            CollectionAssert.Contains(suggestions, "handlePassengers");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void AutomationSpeedPresets_IncludeReadableSlowMo()
    {
        CollectionAssert.AreEqual(
            new[] { 0.25f, 0.5f, 1f, 2f, 4f },
            AutomationDriveController.SpeedPresetValues.ToArray());
    }
}
