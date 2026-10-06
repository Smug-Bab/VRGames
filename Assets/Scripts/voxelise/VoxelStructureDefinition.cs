using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct VoxelStructureNode
{
    public Vector3Int relativePosition;
    public VoxelBlockDefinition blockDefinition;
}

[CreateAssetMenu(fileName = "New Voxel Structure Definition", menuName = "Modular Engine/Structure Definition")]
public class VoxelStructureDefinition : ScriptableObject
{
    [Header("Structure Layout")]
    public List<VoxelStructureNode> nodes = new List<VoxelStructureNode>();

    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    public float spawnChance = 0.02f; // Probability per surface voxel
}