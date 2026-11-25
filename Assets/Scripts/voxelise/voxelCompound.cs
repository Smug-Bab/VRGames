using System.Collections.Generic;
using UnityEngine;

public sealed class Compound
{
    public const float MassScale = 1f;
    public const float TempScale = 3000f;
    public const float ToughnessScale = 1000f;
    public const float RadHeat = 50f;
    public const float DecayScale = 0.01f;

    public readonly string Formula;
    public readonly Dictionary<string, int> Atoms;
    public readonly float Mass;
    public readonly float Bond;
    public readonly float Conductivity;
    public readonly float Radioactivity;
    public readonly float BreakTemp;
    public readonly float Toughness;
    public readonly float HeatPerSec;
    public readonly float DecayPerSec;
    public readonly Color Tint;       // sRGB, for UI/inspector
    public readonly Color MeshTint;   // colour-space corrected, for vertex colours

    public Compound(string formula)
    {
        Formula = formula;
        var atoms = FormulaParser.Parse(formula);
        Atoms = atoms;

        double mass = 0; float bond = 0, cond = 0, rad = 0; int n = 0;
        foreach (var kv in atoms)
        {
            var e = PeriodicTable.Get(kv.Key);
            mass += e.Mass * kv.Value;
            bond += e.Bond * kv.Value;
            cond += e.Conductivity * kv.Value;
            rad  += e.Radioactivity * kv.Value;
            n += kv.Value;
        }
        Mass = (float)mass * MassScale;
        Bond = bond / n; Conductivity = cond / n; Radioactivity = rad / n;
        BreakTemp = Bond * TempScale;
        Toughness = Bond * ToughnessScale;
        HeatPerSec = Radioactivity * RadHeat;
        DecayPerSec = Radioactivity * DecayScale;

        // Stable FNV-1a (string.GetHashCode is not stable across runtimes/builds)
        uint hh = 2166136261;
        foreach (char ch in formula) hh = (hh ^ ch) * 16777619;
        var rng = new System.Random((int)hh);
        Tint = Color.HSVToRGB((float)rng.NextDouble(), 0.3f + 0.3f * (float)rng.NextDouble(),
                              0.5f + 0.4f * (float)rng.NextDouble());
        MeshTint = QualitySettings.activeColorSpace == ColorSpace.Linear ? Tint.linear : Tint;
    }

    public string Describe()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Formula        {Formula}");
        sb.AppendLine($"Atoms          {string.Join(" ", System.Linq.Enumerable.Select(Atoms, kv => kv.Key + "×" + kv.Value))}");
        sb.AppendLine($"Mass           {Mass:F2} kg  (heat capacity)");
        sb.AppendLine($"Bond           {Bond:F3}");
        sb.AppendLine($"Conductivity   {Conductivity:F3}");
        sb.AppendLine($"Radioactivity  {Radioactivity:F3}");
        sb.AppendLine($"Break temp     {BreakTemp:F0} K  ({BreakTemp - 273.15f:F0} °C)");
        sb.AppendLine($"Toughness      {Toughness:F0} J");
        sb.AppendLine($"Self-heating   {HeatPerSec:F2} /s");
        sb.AppendLine($"Decay chance   {DecayPerSec:F4} /s");
        sb.AppendLine($"Tint           #{ColorUtility.ToHtmlStringRGB(Tint)}");
        return sb.ToString();
    }
}

public static class MaterialRegistry
{
    static readonly List<Compound> list = new List<Compound> { null }; // 0 = air
    static readonly Dictionary<string, ushort> ids = new Dictionary<string, ushort>();

    public static ushort Id(string formula)
    {
        if (ids.TryGetValue(formula, out var id)) return id;
        list.Add(new Compound(formula));
        id = (ushort)(list.Count - 1);
        ids[formula] = id;
        return id;
    }
    public static Compound Get(ushort id) => list[id];
}
