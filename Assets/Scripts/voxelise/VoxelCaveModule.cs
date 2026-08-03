using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Cave Module", menuName = "Voxel Engine/Modules/Cave Module")]
public class VoxelCaveModule : ScriptableObject
{
    [Header("Base Subterranean Stone")]
    [Tooltip("The default rock that fills the deep underground underneath the biome layers.")]
    public VoxelBlockDefinition baseStoneBlock;

    [Header("Ore Generation")]
    public List<VoxelOreSpawnSettings> oreSpawns = new List<VoxelOreSpawnSettings>();

    [Header("Cave Noise Settings")]
    public float caveFrequency = 0.02f;
    [Range(0f, 1f)] public float caveThreshold = 0.6f;

    /// <summary>
    /// Evaluates 3D perlin noise to determine if a block position should be hollowed out into a cave.
    /// </summary>
    public bool IsCaveAt(int x, int y, int z, float seed)
    {
        // Simple 3D Perlin Noise approximation for carving tunnels
        float ab = Mathf.PerlinNoise((x + seed) * caveFrequency, (y + seed) * caveFrequency);
        float bc = Mathf.PerlinNoise((y + seed) * caveFrequency, (z + seed) * caveFrequency);
        float ac = Mathf.PerlinNoise((x + seed) * caveFrequency, (z + seed) * caveFrequency);

        float abc = (ab + bc + ac) / 3f;
        return abc > caveThreshold;
    }

    /// <summary>
    /// Determines if a specific location should replace the base stone with an ore block.
    /// </summary>
    public VoxelBlockDefinition RequestOreSpawn(int y)
    {
        foreach (var ore in oreSpawns)
        {
            if (ore.block == null) continue;

            if (y >= ore.minHeight && y <= ore.maxHeight)
            {
                if (Random.value < ore.spawnChance)
                {
                    return ore.block;
                }
            }
        }
        return baseStoneBlock;
    }
}

[System.Serializable]
public struct VoxelOreSpawnSettings
{
    public VoxelBlockDefinition block;
    [Range(0f, 1f)] public float spawnChance;
    public int minHeight;
    public int maxHeight;
}
