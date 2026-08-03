using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Voxel Biome", menuName = "Voxel Engine/Biome Definition")]
public class VoxelBiomeDefinition : ScriptableObject
{
    [Header("Terrain Shape")]
    public float baseHeight = 0f;
    public float frequency = 0.005f;
    public float amplitude = 64f;

    [Header("Dynamic Layer Stacking")]
    public List<VoxelBiomeLayer> terrainLayers = new List<VoxelBiomeLayer>();

    [Header("Modular Features")]
    public VoxelCaveModule caveSettings;

    [Header("Structures")]
    [Tooltip("List of native block structures that can randomly spawn on the terrain surface.")]
    public List<VoxelStructureSpawnSettings> allowedStructures;

    public virtual ushort GetBlockAtHeight(int globalY, int surfaceHeight, VoxelRegistry registry)
    {
        if (globalY > surfaceHeight) return 0;

        int depth = surfaceHeight - globalY;
        int currentDepthOffset = 0;

        for (int i = 0; i < terrainLayers.Count; i++)
        {
            var layer = terrainLayers[i];
            if (layer.block == null) continue;

            currentDepthOffset += layer.thickness;

            if (depth < currentDepthOffset)
            {
                return registry.GetBlockID(layer.block);
            }
        }

        if (caveSettings != null && caveSettings.baseStoneBlock != null)
        {
            return registry.GetBlockID(caveSettings.baseStoneBlock);
        }

        return 0;
    }
}

[System.Serializable]
public struct VoxelBiomeLayer
{
    public VoxelBlockDefinition block;
    public int thickness;
}

[System.Serializable]
public struct VoxelStructureSpawnSettings
{
    public string structureName;

    [Range(0f, 1f)] public float spawnChance;

    [Header("Bounding Box (Optimized Spacing Checks)")]
    public int structureWidth;  // X Size
    public int structureLength; // Z Size

    [Tooltip("The blocks that compose this object relative to its origin.")]
    public List<VoxelStructureBlockOffset> structureBlocks;
}

[System.Serializable]
public struct VoxelStructureBlockOffset
{
    public Vector3Int relativePosition;
    public VoxelBlockDefinition blockType;
}
