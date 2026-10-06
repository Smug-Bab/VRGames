using UnityEngine;

public static class VoxelStructureGenerator
{
    public static void PlaceStructure(
        VoxelChunk chunk, 
        Vector3Int localOrigin, 
        VoxelStructureDefinition structure, 
        VoxelRegistry registry)
    {
        if (structure == null || structure.nodes == null) return;

        foreach (var node in structure.nodes)
        {
            Vector3Int targetPos = localOrigin + node.relativePosition;
            ushort blockID = registry.GetID(node.blockDefinition);

            // Ensure the node fits inside the local chunk boundaries
            if (targetPos.x >= 0 && targetPos.x < VoxelChunk.ChunkSize &&
                targetPos.y >= 0 && targetPos.y < VoxelChunk.ChunkSize &&
                targetPos.z >= 0 && targetPos.z < VoxelChunk.ChunkSize)
            {
                chunk.SetBlockLocal(targetPos.x, targetPos.y, targetPos.z, blockID);
            }
        }
    }
}