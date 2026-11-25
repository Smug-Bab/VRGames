using System;
using UnityEngine;

[Serializable] public struct StructBlock { public Vector3Int offset; public VoxelDef voxel; }  // voxel null = carve to air

[CreateAssetMenu(menuName = "Voxel/Structure Def")]
public class StructureDef : ScriptableObject
{
    public StructBlock[] blocks;
    public BiomeDef[] allowedBiomes;          // empty = any
    [Range(0, 1)] public float chancePerRegion = 0.3f;
    public int yOffset = 0;                   // relative to surface height at origin
    public int seed = 1;

    public bool AllowedIn(BiomeDef b)
    {
        if (allowedBiomes == null || allowedBiomes.Length == 0) return true;
        return Array.IndexOf(allowedBiomes, b) >= 0;
    }
}
