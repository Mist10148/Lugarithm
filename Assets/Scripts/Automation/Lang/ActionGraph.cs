using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// One node in a generated action graph. The graph is a <i>flat</i> list of opcodes
/// (control flow expressed with explicit block markers like <c>if</c>/<c>endif</c>)
/// rather than a nested tree — Unity's JsonUtility can't deserialize recursive types,
/// and a flat list keeps the model's structured output simple to validate. The
/// <see cref="ActionGraphCompiler"/> rebuilds indentation from the markers.
/// </summary>
[Serializable]
public sealed class ActionGraphNode
{
    public string op;         // action | call | assign | comment | def | enddef | if | elif | else | endif | while | endwhile
    public string name;       // action/call/def: command or function name; assign: variable name
    public string arg;        // action: repeat/arg text; assign: right-hand expression
    public string condition;  // if/elif/while: a boolean expression (validated by the parser)
    public string comment;    // optional trailing comment on this line
}

/// <summary>The model's structured reply when generating code in Agent mode.</summary>
[Serializable]
public sealed class ActionGraphResponse
{
    public string message;            // friendly one/two-sentence explanation
    public ActionGraphNode[] nodes;   // the program as a flat opcode list
}

/// <summary>
/// Compiles a flat <see cref="ActionGraphResponse"/> into Python-style source the
/// existing parser/interpreter already accept, reconstructing indentation from the
/// block markers and emitting <c>#</c> comments. The result is then validated with
/// <see cref="GeneratedProgramPolicy"/> against the level's unlocked vocabulary, so a
/// malformed or out-of-scope graph is caught before it ever reaches the editor.
/// </summary>
public static class ActionGraphCompiler
{
    // JSON schema handed to Gemini as responseJsonSchema for Agent mode.
    public const string ResponseSchema =
        "{\"type\":\"object\",\"properties\":{" +
        "\"message\":{\"type\":\"string\"}," +
        "\"nodes\":{\"type\":\"array\",\"items\":{\"type\":\"object\",\"properties\":{" +
        "\"op\":{\"type\":\"string\",\"enum\":[\"action\",\"call\",\"assign\",\"comment\",\"def\",\"enddef\",\"if\",\"elif\",\"else\",\"endif\",\"while\",\"endwhile\"]}," +
        "\"name\":{\"type\":\"string\"},\"arg\":{\"type\":\"string\"}," +
        "\"condition\":{\"type\":\"string\"},\"comment\":{\"type\":\"string\"}}," +
        "\"required\":[\"op\"],\"additionalProperties\":false}}}," +
        "\"required\":[\"message\",\"nodes\"],\"additionalProperties\":false}";

    public static bool TryParse(string json, out ActionGraphResponse response)
    {
        response = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try { response = UnityEngine.JsonUtility.FromJson<ActionGraphResponse>(json); }
        catch { return false; }
        return response != null && response.nodes != null;
    }

    /// <summary>Compiles the graph to source. Returns false (with a reason) when the
    /// block markers don't balance or an opcode is malformed.</summary>
    public static bool TryCompile(ActionGraphResponse graph, out string source, out string error)
    {
        source = null;
        error = null;
        if (graph?.nodes == null) { error = "empty action graph"; return false; }

        var sb = new StringBuilder();
        int indent = 0;
        void Line(string text) => sb.Append(new string(' ', indent * 4)).Append(text).Append('\n');
        string Trail(string c) => string.IsNullOrWhiteSpace(c) ? "" : "  # " + c.Trim();

        foreach (ActionGraphNode node in graph.nodes)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.op)) { error = "node with no op"; return false; }
            switch (node.op)
            {
                case "comment":
                    Line("# " + (node.comment ?? node.name ?? "").Trim());
                    break;

                case "action":
                    if (string.IsNullOrWhiteSpace(node.name)) { error = "action with no name"; return false; }
                    Line($"{node.name.Trim()}({(node.arg ?? "").Trim()}){Trail(node.comment)}");
                    break;

                case "call":
                    if (string.IsNullOrWhiteSpace(node.name)) { error = "function call with no name"; return false; }
                    Line($"{node.name.Trim()}(){Trail(node.comment)}");
                    break;

                case "def":
                    if (string.IsNullOrWhiteSpace(node.name)) { error = "def with no name"; return false; }
                    Line($"def {node.name.Trim()}():{Trail(node.comment)}");
                    indent++;
                    break;

                case "assign":
                    if (string.IsNullOrWhiteSpace(node.name) || string.IsNullOrWhiteSpace(node.arg))
                    { error = "assign needs a name and value"; return false; }
                    Line($"{node.name.Trim()} = {node.arg.Trim()}{Trail(node.comment)}");
                    break;

                case "if":
                    Line($"if {Cond(node)}:{Trail(node.comment)}");
                    indent++;
                    break;

                case "while":
                    Line($"while {Cond(node)}:{Trail(node.comment)}");
                    indent++;
                    break;

                case "elif":
                    if (indent == 0) { error = "elif outside an if"; return false; }
                    indent--;
                    Line($"elif {Cond(node)}:{Trail(node.comment)}");
                    indent++;
                    break;

                case "else":
                    if (indent == 0) { error = "else outside an if"; return false; }
                    indent--;
                    Line($"else:{Trail(node.comment)}");
                    indent++;
                    break;

                case "endif":
                case "endwhile":
                case "enddef":
                    if (indent == 0) { error = $"{node.op} without a matching block"; return false; }
                    indent--;
                    break;

                default:
                    error = $"unknown op '{node.op}'";
                    return false;
            }
        }

        if (indent != 0) { error = "unclosed if/while block"; return false; }

        source = sb.ToString().TrimEnd();
        if (source.Length == 0) { error = "the program is empty"; return false; }
        return true;
    }

    static string Cond(ActionGraphNode node)
    {
        string c = (node.condition ?? "").Trim();
        return c.Length == 0 ? "True" : c;
    }
}
