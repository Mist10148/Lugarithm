using System;
using System.Collections.Generic;

/// <summary>A user-defined function value (top-level <c>def</c> only).</summary>
public class FunctionValue
{
    public string Name;
    public List<string> Params = new List<string>();
    public List<StmtNode> Body = new List<StmtNode>();
}

/// <summary>
/// Tagged runtime value for the Para language. Designed to grow into lists,
/// dicts, tuples and functions in later tiers; Phase 1 uses the scalar kinds.
/// </summary>
public enum ValueKind
{
    None,
    Int,
    Float,
    Bool,
    Str,
    List,
    Dict,
    Tuple,
    Func,
}

/// <summary>
/// A small immutable value. Reference types live in <see cref="Obj"/>.
/// Equality is value-based for scalars and reference-equality for collections.
/// </summary>
public readonly struct Value : IEquatable<Value>
{
    public readonly ValueKind Kind;
    public readonly long      I;
    public readonly double    F;
    public readonly bool      B;
    public readonly string    S;
    public readonly object    Obj;

    public static readonly Value NoneVal = new Value(ValueKind.None);

    public static Value Int(long v)           => new Value(ValueKind.Int,   i: v);
    public static Value Float(double v)       => new Value(ValueKind.Float, f: v);
    public static Value Bool(bool v)          => new Value(ValueKind.Bool,  b: v);
    public static Value Str(string v)         => new Value(ValueKind.Str,   s: v);
    public static Value None                  => NoneVal;
    public static Value List(List<Value> v)   => new Value(ValueKind.List, obj: v);
    public static Value Dict(Dictionary<Value, Value> v) => new Value(ValueKind.Dict, obj: v);
    public static Value Tuple(Value[] v)      => new Value(ValueKind.Tuple, obj: v);
    public static Value Func(FunctionValue v) => new Value(ValueKind.Func, obj: v);

    public FunctionValue AsFunc() => Obj as FunctionValue;

    Value(ValueKind kind, long i = 0, double f = 0, bool b = false, string s = null, object obj = null)
    {
        Kind = kind;
        I    = i;
        F    = f;
        B    = b;
        S    = s;
        Obj  = obj;
    }

    /// <summary>Python-style truthiness: 0, 0.0, empty string and None are false.</summary>
    public bool IsTruthy()
    {
        switch (Kind)
        {
            case ValueKind.None:  return false;
            case ValueKind.Int:   return I != 0;
            case ValueKind.Float: return F != 0.0;
            case ValueKind.Bool:  return B;
            case ValueKind.Str:   return !string.IsNullOrEmpty(S);
            default:              return Obj != null;
        }
    }

    /// <summary>Beginner-friendly type name for error messages.</summary>
    public string TypeName
    {
        get
        {
            switch (Kind)
            {
                case ValueKind.Int:
                case ValueKind.Float: return "number";
                case ValueKind.Bool:  return "true-or-false";
                case ValueKind.Str:   return "text";
                case ValueKind.None:  return "nothing";
                case ValueKind.List:  return "list";
                case ValueKind.Dict:  return "dictionary";
                case ValueKind.Tuple: return "pair";
                case ValueKind.Func:  return "function";
                default:              return "value";
            }
        }
    }

    /// <summary>Display rendering for print() and the console.</summary>
    public string Display()
    {
        switch (Kind)
        {
            case ValueKind.None:  return "None";
            case ValueKind.Int:   return I.ToString();
            case ValueKind.Float: return F.ToString(System.Globalization.CultureInfo.InvariantCulture);
            case ValueKind.Bool:  return B ? "True" : "False";
            case ValueKind.Str:   return S ?? "";
            case ValueKind.List:  return Obj is List<Value> list ? $"[{string.Join(", ", list.ConvertAll(v => v.Display()))}]" : "[]";
            case ValueKind.Dict:
            {
                if (!(Obj is Dictionary<Value, Value> dict)) return "{}";
                var pairs = new List<string>();
                foreach (var kv in dict)
                    pairs.Add($"{kv.Key.Display()}: {kv.Value.Display()}");
                return "{" + string.Join(", ", pairs) + "}";
            }
            case ValueKind.Tuple: return Obj is Value[] arr ? $"({string.Join(", ", Array.ConvertAll(arr, v => v.Display()))})" : "()";
            case ValueKind.Func:  return "<function>";
            default:              return "";
        }
    }

    // -------------------------------------------------------------------------
    // Equality

    public bool Equals(Value other)
    {
        if (Kind != other.Kind) return false;
        switch (Kind)
        {
            case ValueKind.None:  return true;
            case ValueKind.Int:   return I == other.I;
            case ValueKind.Float: return Math.Abs(F - other.F) < double.Epsilon * 100;
            case ValueKind.Bool:  return B == other.B;
            case ValueKind.Str:   return S == other.S;
            default:              return ReferenceEquals(Obj, other.Obj);
        }
    }

    public override bool Equals(object obj) => obj is Value v && Equals(v);
    public override int GetHashCode()
    {
        switch (Kind)
        {
            case ValueKind.None:  return 0;
            case ValueKind.Int:   return I.GetHashCode();
            case ValueKind.Float: return F.GetHashCode();
            case ValueKind.Bool:  return B.GetHashCode();
            case ValueKind.Str:   return S?.GetHashCode() ?? 0;
            default:              return Obj?.GetHashCode() ?? 0;
        }
    }

    public static bool operator ==(Value a, Value b) => a.Equals(b);
    public static bool operator !=(Value a, Value b) => !a.Equals(b);

    // -------------------------------------------------------------------------
    // Convenience accessors with friendly runtime-error helpers.

    public long AsInt()
    {
        if (Kind == ValueKind.Int) return I;
        if (Kind == ValueKind.Float) return (long)F;
        throw new InvalidOperationException($"expected a number, got {TypeName}");
    }

    public double AsFloat()
    {
        if (Kind == ValueKind.Int) return I;
        if (Kind == ValueKind.Float) return F;
        throw new InvalidOperationException($"expected a number, got {TypeName}");
    }

    public string AsStr()
    {
        if (Kind == ValueKind.Str) return S;
        throw new InvalidOperationException($"expected text, got {TypeName}");
    }
}
