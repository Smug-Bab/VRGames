using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VoxelBiomeDefinition", menuName = "Modular Engine/Biome Definition")]
public class VoxelBiomeDefinition : ScriptableObject
{
    [Header("Climate Criteria")]
    [Range(0f, 1f)] public float targetTemperature = 0.5f;
    [Range(0f, 1f)] public float targetHumidity = 0.5f;

    [Header("Terrain & Height Noise")]
    public VoxelNoiseSettings noiseSettings;

    [Header("Block Layer Assignment")]
    public VoxelBlockDefinition topBlock;
    public VoxelBlockDefinition fillerBlock;
    public VoxelBlockDefinition stoneBlock;
    public int topLayerDepth = 4;

    [Header("Structure Generation")]
    public List<VoxelStructureDefinition> structures = new List<VoxelStructureDefinition>();

    public ushort GetBlockForHeight(int currentY, int surfaceHeight, int globalX, int globalZ, int seed, VoxelRegistry registry)
    {
        if (noiseSettings != null && VoxelCaveCarver.IsCave(globalX, currentY, globalZ, noiseSettings, seed))
        {
            return 0; // Air
        }

        if (currentY > surfaceHeight)
        {
            return 0; // Air above ground
        }
        else if (currentY == surfaceHeight)
        {
            return registry.GetID(topBlock);
        }
        else if (currentY > surfaceHeight - topLayerDepth)
        {
            return registry.GetID(fillerBlock);
        }

        return registry.GetID(stoneBlock);
    }
}
