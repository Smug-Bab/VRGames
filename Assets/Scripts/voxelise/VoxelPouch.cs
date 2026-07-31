using System.Collections.Generic;
using UnityEngine;

public class VoxelPouch : MonoBehaviour
{
    // Simple structural data representation of a stored slot
    [System.Serializable]
    public class VoxelSlot
    {
        public ushort blockID;
        public VoxelBlockDefinition blockDefinition;
        public Material blockMaterial;
        public int count;

        public VoxelSlot(ushort id, VoxelBlockDefinition def, Material mat)
        {
            blockID = id;
            blockDefinition = def;
            blockMaterial = mat;
            count = 1;
        }
    }

    [Header("Inventory Settings")]
    public int maxUniqueTypes = 8;

    // Exposed list to view slots inside the Inspector
    public List<VoxelSlot> storedVoxels = new List<VoxelSlot>();

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object dropped into the trigger is a dynamic voxel
        // (Ensure DynamicVoxel script exists and has public getters for these properties)
        DynamicVoxel dynamicVoxel = other.GetComponent<DynamicVoxel>();
        if (dynamicVoxel == null) return;

        ushort id = dynamicVoxel.BlockID;

        // 1. Check if we already have this block type stored
        for (int i = 0; i < storedVoxels.Count; i++)
        {
            if (storedVoxels[i].blockID == id)
            {
                storedVoxels[i].count++;

                // FIX: Fallback to .name if your VoxelBlockDefinition doesn't have an explicit blockName variable
                string blockName = storedVoxels[i].blockDefinition != null ? storedVoxels[i].blockDefinition.name : "Unknown Block";
                Debug.Log($"Added to existing type. Total {blockName}: {storedVoxels[i].count}");

                Destroy(other.gameObject); // Consume the physical block
                return;
            }
        }

        // 2. If it's a new type, verify if we have open slots remaining
        if (storedVoxels.Count < maxUniqueTypes)
        {
            VoxelSlot newSlot = new VoxelSlot(id, dynamicVoxel.BlockDefinition, dynamicVoxel.BlockMaterial);
            storedVoxels.Add(newSlot);

            // FIX: Fallback to .name here as well
            string blockName = dynamicVoxel.BlockDefinition != null ? dynamicVoxel.BlockDefinition.name : "Unknown Block";
            Debug.Log($"Added NEW type ({blockName}) to pouch. Total unique types: {storedVoxels.Count}/{maxUniqueTypes}");

            Destroy(other.gameObject); // Consume the physical block
        }
        else
        {
            Debug.LogWarning($"Pouch inventory is full! Cannot store more than {maxUniqueTypes} unique types of blocks.");
        }
    }
}
