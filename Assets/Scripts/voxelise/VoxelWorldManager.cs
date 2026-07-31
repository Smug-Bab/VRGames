using UnityEngine;
using System.Collections.Generic;

public class VoxelWorldManager : MonoBehaviour
{
    [Header("References")]
    public VoxelRegistry registry;
    public Transform playerTransform;
    public Material worldMaterial;

    [Header("Settings")]
    public const int chunkSize = 16;
    public int viewDistance = 4;

    [Header("Seed Settings")]
    public bool randomizeSeedOnStart = false; // Changed default to false
    public int worldSeed = 0; // Default set to 0

    [Header("Performance Ticking")]
    public int tickRadius = 1;
    public int randomTicksPerChunk = 30;

    private Dictionary<Vector3Int, GameObject> activeChunks = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, ushort[]> chunkDataRegistry = new Dictionary<Vector3Int, ushort[]>();
    private Vector3Int lastPlayerChunkCoord = new Vector3Int(-999, -999, -999);
    private float tickTimer = 0f;

    private Dictionary<Vector3Int, ushort> globalStructureBuffer = new Dictionary<Vector3Int, ushort>();
    private HashSet<Vector2Int> evaluatedStructureColumns = new HashSet<Vector2Int>();

    private ushort grassID;
    private ushort dirtID;
    private ushort stoneID;
    private bool isIDsCached = false;

    private void Awake()
    {
        if (randomizeSeedOnStart)
        {
            worldSeed = Random.Range(int.MinValue, int.MaxValue);
            Debug.Log($"[World Manager] Infinite Seed: {worldSeed}");
        }
        else
        {
            worldSeed = 0; // Explicitly ensure seed is 0 if randomization is off
            Debug.Log($"[World Manager] Fixed Seed: {worldSeed}");
        }

        if (registry != null)
        {
            registry.Initialize();
            CacheBlockIDs();
        }
    }

    private void CacheBlockIDs()
    {
        if (isIDsCached || registry == null) return;

        if (registry.registeredBiomes != null && registry.registeredBiomes.Count > 0)
        {
            VoxelBiomeDefinition biome = registry.registeredBiomes[0];

            if (biome.terrainLayers != null && biome.terrainLayers.Count > 0)
            {
                if (biome.terrainLayers.Count > 0) grassID = registry.GetBlockID(biome.terrainLayers[0].block);
                if (biome.terrainLayers.Count > 1) dirtID = registry.GetBlockID(biome.terrainLayers[1].block);
            }

            if (biome.caveSettings != null && biome.caveSettings.baseStoneBlock != null)
            {
                stoneID = registry.GetBlockID(biome.caveSettings.baseStoneBlock);
            }
        }

        isIDsCached = true;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        Vector3 localPlayerPos = playerTransform.position - transform.position;

        int currentChunkX = Mathf.FloorToInt(localPlayerPos.x / chunkSize);
        int currentChunkY = Mathf.FloorToInt(localPlayerPos.y / chunkSize);
        int currentChunkZ = Mathf.FloorToInt(localPlayerPos.z / chunkSize);
        Vector3Int playerChunkCoord = new Vector3Int(currentChunkX, currentChunkY, currentChunkZ);

        if (playerChunkCoord != lastPlayerChunkCoord)
        {
            UpdateVisibleChunks(playerChunkCoord);
            lastPlayerChunkCoord = playerChunkCoord;
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= 1.0f)
        {
            RunBlockTicks(playerChunkCoord);
            tickTimer = 0f;
        }
    }

    private void RunBlockTicks(Vector3Int playerCoord)
    {
        if (registry == null) return;

        for (int cx = -tickRadius; cx <= tickRadius; cx++)
        {
            for (int cy = -tickRadius; cy <= tickRadius; cy++)
            {
                for (int cz = -tickRadius; cz <= tickRadius; cz++)
                {
                    Vector3Int targetChunkCoord = playerCoord + new Vector3Int(cx, cy, cz);
                    if (!chunkDataRegistry.TryGetValue(targetChunkCoord, out ushort[] data)) continue;

                    for (int i = 0; i < randomTicksPerChunk; i++)
                    {
                        int rx = Random.Range(0, chunkSize);
                        int ry = Random.Range(0, chunkSize);
                        int rz = Random.Range(0, chunkSize);

                        int index = rx | (ry << 4) | (rz << 8);
                        ushort blockID = data[index];

                        if (blockID == 0) continue;

                        VoxelBlockDefinition block = registry.GetBlock(blockID);
                        if (block != null && block.isTickable)
                        {
                            Vector3Int globalPos = new Vector3Int(
                                targetChunkCoord.x * chunkSize + rx,
                                targetChunkCoord.y * chunkSize + ry,
                                targetChunkCoord.z * chunkSize + rz
                            );
                            block.OnTickEvent?.Invoke(globalPos);
                        }
                    }
                }
            }
        }
    }

    private void UpdateVisibleChunks(Vector3Int playerCoord)
    {
        if (!isIDsCached) CacheBlockIDs();
        HashSet<Vector3Int> visibleMeshCoords = new HashSet<Vector3Int>();

        int structureGenerationBuffer = viewDistance + 4;
        for (int x = -structureGenerationBuffer; x <= structureGenerationBuffer; x++)
        {
            for (int z = -structureGenerationBuffer; z <= structureGenerationBuffer; z++)
            {
                Vector2Int columnCoord = new Vector2Int(playerCoord.x + x, playerCoord.z + z);
                PreStageStructuresForChunkColumn(columnCoord);
            }
        }

        for (int x = -viewDistance - 1; x <= viewDistance + 1; x++)
        {
            for (int y = -viewDistance - 1; y <= viewDistance + 1; y++)
            {
                for (int z = -viewDistance - 1; z <= viewDistance + 1; z++)
                {
                    Vector3Int targetCoord = playerCoord + new Vector3Int(x, y, z);

                    if (!chunkDataRegistry.ContainsKey(targetCoord))
                    {
                        chunkDataRegistry[targetCoord] = GenerateChunkData(targetCoord);
                    }

                    if (Mathf.Abs(x) <= viewDistance && Mathf.Abs(y) <= viewDistance && Mathf.Abs(z) <= viewDistance)
                    {
                        visibleMeshCoords.Add(targetCoord);
                    }
                }
            }
        }

        foreach (Vector3Int coord in visibleMeshCoords)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                BuildChunkMeshInstance(coord);
            }
        }

        List<Vector3Int> toRemove = new List<Vector3Int>();
        foreach (var chunk in activeChunks)
        {
            if (!visibleMeshCoords.Contains(chunk.Key))
            {
                Destroy(chunk.Value);
                toRemove.Add(chunk.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            Vector3Int coord = toRemove[i];
            activeChunks.Remove(coord);
            chunkDataRegistry.Remove(coord);
        }
    }

    private void BuildChunkMeshInstance(Vector3Int coord)
    {
        GameObject chunkObject = new GameObject($"Chunk_{coord.x}_{coord.y}_{coord.z}");
        chunkObject.transform.parent = this.transform;
        chunkObject.transform.localPosition = new Vector3(coord.x * chunkSize, coord.y * chunkSize, coord.z * chunkSize);

        MeshFilter filter = chunkObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = chunkObject.AddComponent<MeshRenderer>();
        MeshCollider collider = chunkObject.AddComponent<MeshCollider>();

        renderer.sharedMaterial = worldMaterial;

        ushort[] data = chunkDataRegistry[coord];

        VoxelMeshBuilder meshBuilder = new VoxelMeshBuilder(chunkSize);
        Mesh mesh = meshBuilder.GenerateMesh(coord, data, registry, this);

        filter.sharedMesh = mesh;
        collider.sharedMesh = mesh;

        activeChunks.Add(coord, chunkObject);
    }

    private void PreStageStructuresForChunkColumn(Vector2Int columnXZ)
    {
        if (evaluatedStructureColumns.Contains(columnXZ)) return;
        evaluatedStructureColumns.Add(columnXZ);

        if (registry == null || registry.registeredBiomes.Count == 0) return;
        VoxelBiomeDefinition biome = registry.registeredBiomes[0];

        if (biome.allowedStructures == null || biome.allowedStructures.Count == 0) return;

        Random.State oldState = Random.state;
        int seedHash = (columnXZ.x.GetHashCode() ^ (columnXZ.y.GetHashCode() << 2)) + worldSeed;
        Random.InitState(seedHash);

        float seedOffsetX = (worldSeed % 1000) * 100f;
        float seedOffsetZ = (worldSeed / 1000 % 1000) * 100f;

        for (int i = 0; i < biome.allowedStructures.Count; i++)
        {
            var spawnSetting = biome.allowedStructures[i];
            if (spawnSetting.structureBlocks == null || spawnSetting.structureBlocks.Count == 0) continue;

            if (Random.value < spawnSetting.spawnChance)
            {
                int marginX = Mathf.Clamp(chunkSize - spawnSetting.structureWidth, 0, chunkSize - 1);
                int marginZ = Mathf.Clamp(chunkSize - spawnSetting.structureLength, 0, chunkSize - 1);

                int localX = (marginX > 0) ? Random.Range(0, marginX) : 0;
                int localZ = (marginZ > 0) ? Random.Range(0, marginZ) : 0;

                int globalX = (columnXZ.x * chunkSize) + localX;
                int globalZ = (columnXZ.y * chunkSize) + localZ;

                float sampleX = (globalX * biome.frequency) + seedOffsetX;
                float sampleZ = (globalZ * biome.frequency) + seedOffsetZ;
                int surfaceHeight = Mathf.FloorToInt(biome.baseHeight + (Mathf.PerlinNoise(sampleX, sampleZ) * biome.amplitude));

                for (int b = 0; b < spawnSetting.structureBlocks.Count; b++)
                {
                    var item = spawnSetting.structureBlocks[b];
                    if (item.blockType == null) continue;

                    Vector3Int structureBlockGlobalPos = new Vector3Int(
                        globalX + item.relativePosition.x,
                        (surfaceHeight + 1) + item.relativePosition.y,
                                                                        globalZ + item.relativePosition.z
                    );

                    ushort assignedID = registry.GetBlockID(item.blockType);

                    if (!globalStructureBuffer.ContainsKey(structureBlockGlobalPos))
                    {
                        globalStructureBuffer[structureBlockGlobalPos] = assignedID;
                    }
                }
            }
        }
        Random.state = oldState;
    }

    private ushort[] GenerateChunkData(Vector3Int chunkXYZ)
    {
        ushort[] chunkData = new ushort[4096];
        if (registry == null || registry.registeredBiomes.Count == 0) return chunkData;

        VoxelBiomeDefinition biome = registry.registeredBiomes[0];

        float seedOffsetX = (worldSeed % 1000) * 100f;
        float seedOffsetZ = (worldSeed / 1000 % 1000) * 100f;

        int originX = chunkXYZ.x * chunkSize;
        int originY = chunkXYZ.y * chunkSize;
        int originZ = chunkXYZ.z * chunkSize;

        Vector3Int lookupPos = Vector3Int.zero;

        for (int x = 0; x < chunkSize; x++)
        {
            int globalX = originX + x;
            lookupPos.x = globalX;

            for (int z = 0; z < chunkSize; z++)
            {
                int globalZ = originZ + z;
                lookupPos.z = globalZ;

                float sampleX = (globalX * biome.frequency) + seedOffsetX;
                float sampleZ = (globalZ * biome.frequency) + seedOffsetZ;
                int surfaceHeight = Mathf.FloorToInt(biome.baseHeight + (Mathf.PerlinNoise(sampleX, sampleZ) * biome.amplitude));

                for (int y = 0; y < chunkSize; y++)
                {
                    int globalY = originY + y;
                    lookupPos.y = globalY;

                    ushort targetBlock = 0;

                    if (globalStructureBuffer.TryGetValue(lookupPos, out ushort structureBlockID))
                    {
                        targetBlock = structureBlockID;
                    }
                    else if (globalY <= surfaceHeight)
                    {
                        targetBlock = biome.GetBlockAtHeight(globalY, surfaceHeight, registry);

                        if (targetBlock == 0)
                        {
                            if (globalY == surfaceHeight) targetBlock = grassID;
                            else if (globalY > surfaceHeight - 4) targetBlock = dirtID;
                            else targetBlock = stoneID;
                        }
                    }

                    int index = x | (y << 4) | (z << 8);
                    chunkData[index] = targetBlock;
                }
            }
        }
        return chunkData;
    }

    public ushort GetBlockAtGlobal(int globalX, int globalY, int globalZ)
    {
        int chunkX = globalX >> 4;
        int chunkY = globalY >> 4;
        int chunkZ = globalZ >> 4;
        Vector3Int chunkCoord = new Vector3Int(chunkX, chunkY, chunkZ);

        if (!chunkDataRegistry.TryGetValue(chunkCoord, out ushort[] data))
        {
            return 0;
        }

        int localX = globalX & 15;
        int localY = globalY & 15;
        int localZ = globalZ & 15;

        return data[localX | (localY << 4) | (localZ << 8)];
    }

    /// <summary>
    /// Modifies a block at absolute world coordinates and automatically updates the chunk's visual mesh.
    /// </summary>
    public void SetBlockAtGlobal(int globalX, int globalY, int globalZ, ushort newBlockID)
    {
        // 1. Bitwise shift to identify the chunk coordinate
        int chunkX = globalX >> 4;
        int chunkY = globalY >> 4;
        int chunkZ = globalZ >> 4;
        Vector3Int chunkCoord = new Vector3Int(chunkX, chunkY, chunkZ);

        // 2. Make sure the chunk actually exists in our data grid
        if (chunkDataRegistry.TryGetValue(chunkCoord, out ushort[] data))
        {
            // 3. Bitwise AND mask (x & 15) to pull the local inside-chunk index safely
            int localX = globalX & 15;
            int localY = globalY & 15;
            int localZ = globalZ & 15;

            int flattenedIndex = localX | (localY << 4) | (localZ << 8);
            data[flattenedIndex] = newBlockID;

            // 4. Force immediate graphic mesh update if the chunk is actively rendered
            if (activeChunks.TryGetValue(chunkCoord, out GameObject chunkObject))
            {
                MeshFilter filter = chunkObject.GetComponent<MeshFilter>();
                MeshCollider collider = chunkObject.GetComponent<MeshCollider>();

                VoxelMeshBuilder meshBuilder = new VoxelMeshBuilder(chunkSize);
                Mesh updatedMesh = meshBuilder.GenerateMesh(chunkCoord, data, registry, this);

                if (filter != null) filter.sharedMesh = updatedMesh;
                if (collider != null) collider.sharedMesh = updatedMesh;
            }
        }
    }
}
