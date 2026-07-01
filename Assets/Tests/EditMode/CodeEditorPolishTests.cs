using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public void CodeEditor_LongLineStillCountsAsOneLogicalLine()
    {
        var editorGo = new GameObject("Editor");
        var inputGo = new GameObject("Input");
        try
        {
            var editor = editorGo.AddComponent<CodeEditorController>();
            var input = inputGo.AddComponent<TMP_InputField>();
            editor.input = input;

            string longComment =
                "# Split the ride into helper functions, then call drive(): drive(), handlePassengers(), handleFares()";
            editor.SetSource(longComment + "\npickUp()");

            Assert.AreEqual(2, editor.LogicalLineCount);

            string lineNumbers = editor.BuildLineNumberText(editor.LogicalLineCount);
            int visibleRows = lineNumbers.Split('\n').Count(row => !string.IsNullOrWhiteSpace(row));
            Assert.AreEqual(2, visibleRows);
            StringAssert.Contains(">1</color>", lineNumbers);
            StringAssert.Contains(">2</color>", lineNumbers);
            Assert.IsFalse(lineNumbers.Contains(">3</color>"));
        }
        finally
        {
            Object.DestroyImmediate(editorGo);
            Object.DestroyImmediate(inputGo);
        }
    }

    [Test]
    public void CodeEditor_ForcesNoWrapMetricsForInputHighlightAndGutter()
    {
        var editorGo = new GameObject("Editor");
        var inputGo = new GameObject("Input");
        var inputTextGo = new GameObject("InputText");
        var highlightGo = new GameObject("Highlight");
        var lineNumbersGo = new GameObject("LineNumbers");
        try
        {
            inputTextGo.transform.SetParent(inputGo.transform, false);
            highlightGo.transform.SetParent(editorGo.transform, false);
            lineNumbersGo.transform.SetParent(editorGo.transform, false);

            var editor = editorGo.AddComponent<CodeEditorController>();
            var input = inputGo.AddComponent<TMP_InputField>();
            var inputText = inputTextGo.AddComponent<TextMeshProUGUI>();
            var highlight = highlightGo.AddComponent<TextMeshProUGUI>();
            var lineNumbers = lineNumbersGo.AddComponent<TextMeshProUGUI>();

            input.textComponent = inputText;
            editor.input = input;
            SetPrivate(editor, "highlight", highlight);
            SetPrivate(editor, "lineNumbers", lineNumbers);

            inputText.textWrappingMode = TextWrappingModes.Normal;
            highlight.textWrappingMode = TextWrappingModes.Normal;
            lineNumbers.textWrappingMode = TextWrappingModes.Normal;

            InvokePrivate(editor, "SyncHighlightToInput");

            Assert.AreEqual(TextWrappingModes.NoWrap, inputText.textWrappingMode);
            Assert.AreEqual(TextWrappingModes.NoWrap, highlight.textWrappingMode);
            Assert.AreEqual(TextWrappingModes.NoWrap, lineNumbers.textWrappingMode);
            Assert.AreEqual(TextOverflowModes.Overflow, inputText.overflowMode);
            Assert.AreEqual(TextOverflowModes.Overflow, highlight.overflowMode);
            Assert.AreEqual(TextOverflowModes.Overflow, lineNumbers.overflowMode);
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

    [Test]
    public void CodeEditor_AutoClose_InsertsPairAndPositionsCaret()
    {
        var editorGo = new GameObject("Editor");
        var inputGo = new GameObject("Input");
        try
        {
            var editor = editorGo.AddComponent<CodeEditorController>();
            var input = CreateInput(inputGo);
            editor.input = input;

            input.text = "print";
            input.stringPosition = 5;
            InvokePrivate(editor, "OnValidateInput", "print", 5, '(');

            Assert.AreEqual("print()", input.text);
            Assert.AreEqual(6, input.stringPosition);
        }
        finally
        {
            Object.DestroyImmediate(editorGo);
            Object.DestroyImmediate(inputGo);
        }
    }

    [Test]
    public void CodeEditor_AutoClose_TypingFullPrintHelloWorldKeepsParentheses()
    {
        var editorGo = new GameObject("Editor");
        var inputGo = new GameObject("Input");
        try
        {
            var editor = editorGo.AddComponent<CodeEditorController>();
            var input = CreateInput(inputGo);
            editor.input = input;

            // Type print(helloworld) one character at a time through OnValidateInput.
            input.text = "";
            TypeChar(editor, input, 'p');
            TypeChar(editor, input, 'r');
            TypeChar(editor, input, 'i');
            TypeChar(editor, input, 'n');
            TypeChar(editor, input, 't');
            TypeChar(editor, input, '(');
            TypeChar(editor, input, 'h');
            TypeChar(editor, input, 'e');
            TypeChar(editor, input, 'l');
            TypeChar(editor, input, 'l');
            TypeChar(editor, input, 'o');
            TypeChar(editor, input, 'w');
            TypeChar(editor, input, 'o');
            TypeChar(editor, input, 'r');
            TypeChar(editor, input, 'l');
            TypeChar(editor, input, 'd');
            TypeChar(editor, input, ')');

            Assert.AreEqual("print(helloworld)", input.text);
        }
        finally
        {
            Object.DestroyImmediate(editorGo);
            Object.DestroyImmediate(inputGo);
        }
    }

    [Test]
    public void CodeEditor_AutoClose_OverTypeSkipsExistingCloser()
    {
        var editorGo = new GameObject("Editor");
        var inputGo = new GameObject("Input");
        try
        {
            var editor = editorGo.AddComponent<CodeEditorController>();
            var input = CreateInput(inputGo);
            editor.input = input;

            input.text = "print()";
            input.stringPosition = 6;
            InvokePrivate(editor, "OnValidateInput", "print()", 6, ')');

            Assert.AreEqual("print()", input.text);
            Assert.AreEqual(7, input.stringPosition);
        }
        finally
        {
            Object.DestroyImmediate(editorGo);
            Object.DestroyImmediate(inputGo);
        }
    }

    TMP_InputField CreateInput(GameObject inputGo)
    {
        var input = inputGo.AddComponent<TMP_InputField>();
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(inputGo.transform, false);
        input.textComponent = textGo.AddComponent<TextMeshProUGUI>();
        return input;
    }

    void TypeChar(CodeEditorController editor, TMP_InputField input, char c)
    {
        int pos = input.stringPosition;
        string before = input.text;
        char validated = (char)InvokePrivate(editor, "OnValidateInput", before, pos, c);
        if (validated != '\0')
        {
            input.text = before.Insert(pos, validated.ToString());
            input.stringPosition = pos + 1;
        }
    }

    [Test]
    public void ConsoleRows_UseReadableFixedHeights()
    {
        var consoleGo = new GameObject("Console", typeof(RectTransform));
        var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var templateGo = new GameObject("LineTemplate", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        try
        {
            scrollGo.transform.SetParent(consoleGo.transform, false);
            contentGo.transform.SetParent(scrollGo.transform, false);
            templateGo.transform.SetParent(consoleGo.transform, false);

            var content = (RectTransform)contentGo.transform;
            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var template = templateGo.GetComponent<TextMeshProUGUI>();
            template.fontSize = 16f;
            templateGo.SetActive(false);

            var console = consoleGo.AddComponent<ConsoleController>();
            SetPrivate(console, "scrollRect", scrollGo.GetComponent<ScrollRect>());
            SetPrivate(console, "content", content);
            SetPrivate(console, "lineTemplate", template);

            console.Info("first terminal line");
            console.Info("second terminal line");

            Assert.AreEqual(2, content.childCount);
            for (int i = 0; i < content.childCount; i++)
            {
                var row = content.GetChild(i);
                var rowText = row.GetComponent<TMP_Text>();
                var rowLayout = row.GetComponent<LayoutElement>();
                var rowRect = (RectTransform)row;

                Assert.NotNull(rowText);
                Assert.NotNull(rowLayout);
                Assert.AreEqual(TextWrappingModes.NoWrap, rowText.textWrappingMode);
                Assert.AreEqual(TextOverflowModes.Masking, rowText.overflowMode);
                Assert.GreaterOrEqual(rowLayout.preferredHeight, 24f);
                Assert.AreEqual(0f, rowRect.anchorMin.x);
                Assert.AreEqual(1f, rowRect.anchorMax.x);
            }
        }
        finally
        {
            Object.DestroyImmediate(consoleGo);
        }
    }

    static void SetPrivate(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, fieldName);
        field.SetValue(target, value);
    }

    static object InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, methodName);
        return method.Invoke(target, args);
    }
}
