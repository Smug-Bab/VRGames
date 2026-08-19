using System.Collections.Generic;
using UnityEngine;

public class VoxelRegistry : MonoBehaviour
{
    [Header("Biome Register")]
    public List<VoxelBiomeDefinition> registeredBiomes = new List<VoxelBiomeDefinition>();

    private readonly Dictionary<ushort, VoxelBlockDefinition> idToBlock = new Dictionary<ushort, VoxelBlockDefinition>();
    private readonly Dictionary<VoxelBlockDefinition, ushort> blockToID = new Dictionary<VoxelBlockDefinition, ushort>();

    public void Initialize()
    {
        idToBlock.Clear();
        blockToID.Clear();

        ushort currentID = 1; // ID 0 is strictly Air and is culled from storage/registration

        foreach (var biome in registeredBiomes)
        {
            if (biome == null) continue;

            // Register Biome Layer Blocks safely, ignoring null/empty entries
            RegisterBlock(biome.topBlock, ref currentID);
            RegisterBlock(biome.fillerBlock, ref currentID);
            RegisterBlock(biome.stoneBlock, ref currentID);

            // Register Structure Blocks safely
            if (biome.structures != null)
            {
                foreach (var structure in biome.structures)
                {
                    if (structure == null || structure.nodes == null) continue;
                    foreach (var node in structure.nodes)
                    {
                        RegisterBlock(node.blockDefinition, ref currentID);
                    }
                }
            }
        }
    }

    private void RegisterBlock(VoxelBlockDefinition block, ref ushort currentID)
    {
        // Guard clause to ensure empty or null definitions are completely culled
        if (block != null && !blockToID.ContainsKey(block))
        {
            idToBlock[currentID] = block;
            blockToID[block] = currentID;
            currentID++;
        }
    }

    public ushort GetID(VoxelBlockDefinition block) => (block != null && blockToID.TryGetValue(block, out ushort id)) ? id : (ushort)0;
    public VoxelBlockDefinition GetBlock(ushort id) => idToBlock.TryGetValue(id, out var block) ? block : null;

    public VoxelBiomeDefinition GetBiomeForLocation(float temperature, float humidity)
    {
        VoxelBiomeDefinition bestMatch = null;
        float closestDistance = float.MaxValue;

        foreach (var biome in registeredBiomes)
        {
            if (biome == null) continue;

            float tempDiff = biome.targetTemperature - temperature;
            float humDiff = biome.targetHumidity - humidity;
            float distance = (tempDiff * tempDiff) + (humDiff * humDiff);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestMatch = biome;
            }
        }

        return bestMatch ?? (registeredBiomes.Count > 0 ? registeredBiomes[0] : null);
    }
}
