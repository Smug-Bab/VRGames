using System;
using System.Collections.Generic;
using UnityEngine;

public enum ClimateParam { Temperature, Humidity, Elevation }

public struct Climate { public float Temperature, Humidity, Elevation; // all 0..1
    public float Get(ClimateParam p) => p == ClimateParam.Temperature ? Temperature
    : p == ClimateParam.Humidity ? Humidity : Elevation; }

    [Serializable]
    public class CompositionMod
    {
        public string match;                 // voxel name or tag; empty = every voxel in this biome
        public string additive;              // formula to add per unit, e.g. "XY2"
        public ClimateParam param;
        public AnimationCurve unitsByParam = AnimationCurve.Linear(0, 0, 1, 0);  // param(0..1) -> units (may be negative)
    }

    [Serializable] public class Layer { public VoxelDef voxel; public int thickness = 4; }   // last layer extends forever
    [Serializable] public class Ore   { public VoxelDef voxel; public int minDepth = 6; public int perThousand = 8; }

    [CreateAssetMenu(menuName = "Voxel/Biome Def")]
    public class BiomeDef : ScriptableObject
    {
        [Header("Where it occurs (nearest centre wins)")]
        [Range(0, 1)] public float temperatureCenter = 0.5f;
        [Range(0, 1)] public float humidityCenter = 0.5f;

        [Header("Terrain")]
        public float heightScale = 1f;
        public Layer[] layers;
        public Ore[] ores;

        [Header("Composition modifiers")]
        public CompositionMod[] mods;

        [NonSerialized] readonly Dictionary<(VoxelDef, int), ushort> cache = new Dictionary<(VoxelDef, int), ushort>();
        const int Q = 16;   // climate quantisation: caps how many variants of one voxel can exist

        public float Distance(float t, float h) => (t - temperatureCenter) * (t - temperatureCenter) + (h - humidityCenter) * (h - humidityCenter);

        public ushort Resolve(VoxelDef v, Climate c)
        {
            if (v == null) return 0;
            int key = Mathf.RoundToInt(c.Temperature * (Q - 1)) * Q * Q
            + Mathf.RoundToInt(c.Humidity * (Q - 1)) * Q
            + Mathf.RoundToInt(c.Elevation * (Q - 1));
            if (cache.TryGetValue((v, key), out var id)) return id;

            var atoms = new Dictionary<string, int>(FormulaParser.Parse(v.formula));
            if (mods != null)
                foreach (var m in mods)
                {
                    if (!v.Matches(m.match) || string.IsNullOrEmpty(m.additive)) continue;
                    int units = Mathf.RoundToInt(m.unitsByParam.Evaluate(c.Get(m.param)));
                    if (units == 0) continue;
                    foreach (var kv in FormulaParser.Parse(m.additive))
                    { atoms.TryGetValue(kv.Key, out int cur); atoms[kv.Key] = cur + kv.Value * units; }
                }

                string f = FormulaParser.ToFormula(atoms);
            id = f.Length == 0 ? v.BaseId : MaterialRegistry.Id(f);   // fully removed => keep base
            cache[(v, key)] = id;
            return id;
        }

        public VoxelDef LayerAt(int depth)
        {
            int d = 0;
            for (int i = 0; i < layers.Length; i++)
            { d += layers[i].thickness; if (depth < d || i == layers.Length - 1) return layers[i].voxel; }
            return null;
        }
    }
