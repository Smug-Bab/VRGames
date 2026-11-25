// FormulaParser.cs
using System;
using System.Collections.Generic;

public static class FormulaParser
{
    public static Dictionary<string, int> Parse(string formula)
    {
        int i = 0;
        var result = ParseGroup(formula, ref i, false);
        if (i != formula.Length) throw new FormatException($"Unexpected ')' in {formula}");
        if (result.Count == 0) throw new FormatException("Empty formula");
        return result;
    }
    public static string ToFormula(Dictionary<string, int> atoms)
    {
        var keys = new List<string>(atoms.Keys);
        keys.Sort(System.StringComparer.Ordinal);       // canonical order => same ID for same composition
        var sb = new System.Text.StringBuilder();
        foreach (var k in keys)
        {
            int n = atoms[k];
            if (n <= 0) continue;
            sb.Append(k);
            if (n > 1) sb.Append(n);
        }
        return sb.ToString();
    }
    static Dictionary<string, int> ParseGroup(string f, ref int i, bool nested)
    {
        var r = new Dictionary<string, int>();
        while (i < f.Length)
        {
            char c = f[i];
            if (c == '(')
            {
                i++;
                var inner = ParseGroup(f, ref i, true);
                if (i >= f.Length || f[i] != ')') throw new FormatException("Missing ')'");
                i++;
                int mult = ReadInt(f, ref i);
                foreach (var kv in inner) Add(r, kv.Key, kv.Value * mult);
            }
            else if (c == ')')
            {
                if (!nested) throw new FormatException("Unbalanced ')'");
                return r;
            }
            else if (char.IsUpper(c))
            {
                string sym = c.ToString(); i++;
                if (i < f.Length && char.IsLower(f[i])) sym += f[i++];
                PeriodicTable.Get(sym); // validates the element exists
                Add(r, sym, ReadInt(f, ref i));
            }
            else throw new FormatException($"Bad character '{c}' in {f}");
        }
        if (nested) throw new FormatException("Missing ')'");
        return r;
    }

    static int ReadInt(string f, ref int i)
    {
        int start = i;
        while (i < f.Length && char.IsDigit(f[i])) i++;
        return i == start ? 1 : int.Parse(f.Substring(start, i - start));
    }

    static void Add(Dictionary<string, int> d, string k, int n)
    { d.TryGetValue(k, out int cur); d[k] = cur + n; }
}
