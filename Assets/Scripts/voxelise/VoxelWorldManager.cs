using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[RequireComponent(typeof(VoxelRegistry))]
public class VoxelWorldManager : MonoBehaviour
{
    [Header("World Setup")]
    public Transform playerTransform;
    public Material defaultChunkMaterial;

    [Tooltip("If true, a random seed will be generated every time the game starts.")]
    public bool useRandomSeed = true;
    public int customSeed = 1337;

    [HideInInspector]
    public int worldSeed;

    [Header("World Boundaries")]
    public int worldLowerLimit = 0;
    public int worldUpperLimit = 128;

    [Header("Biome Climate Noise Scaling")]
    public float temperatureScale = 0.002f;
    public float humidityScale = 0.002f;

    [Header("Planetary Climate Settings")]
    public float basePlanetaryTemperature = 288.15f;
    public float biomeTemperatureVariance = 40.0f;

    [Header("Streaming Settings")]
    public int renderDistance = 4;

    [Header("Thermal Simulation")]
    public int thermalTickInterval = 4;
    private int frameCounter = 0;

    private VoxelRegistry registry;
    private readonly Dictionary<Vector3Int, VoxelChunk> activeChunks = new Dictionary<Vector3Int, VoxelChunk>();
    private Vector3Int lastPlayerChunkCoord = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);

    private void Awake()
    {
        worldSeed = useRandomSeed ? Random.Range(-100000, 100000) : customSeed;

        registry = GetComponent<VoxelRegistry>();
        registry.Initialize();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        Vector3Int currentChunkCoord = GetChunkCoordFromPosition(playerTransform.position);
        if (currentChunkCoord != lastPlayerChunkCoord)
        {
            lastPlayerChunkCoord = currentChunkCoord;
            UpdateWorldChunks(currentChunkCoord);
        }

        frameCounter++;
        if (frameCounter >= thermalTickInterval)
        {
            frameCounter = 0;
            UpdateThermalSimulation();
        }
    }

    private void UpdateWorldChunks(Vector3Int playerCoord)
    {
        List<Vector3Int> chunksToRemove = new List<Vector3Int>();

        foreach (var kvp in activeChunks)
        {
            if ((float)(kvp.Key - playerCoord).magnitude > renderDistance + 1)
            {
                chunksToRemove.Add(kvp.Key);
            }
        }

        foreach (var coord in chunksToRemove)
        {
            Destroy(activeChunks[coord].gameObject);
            activeChunks.Remove(coord);
        }

        List<VoxelChunk> newlyGeneratedChunks = new List<VoxelChunk>();
        List<VoxelChunk> chunksNeedingMeshUpdate = new List<VoxelChunk>();

        // PASS 1: Data Generation Only.
        // We calculate all voxel positions before ANY mesh is allowed to render.
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector3Int coord = new Vector3Int(playerCoord.x + x, playerCoord.y + y, playerCoord.z + z);

                    int chunkWorldYMin = coord.y * VoxelChunk.ChunkSize;
                    int chunkWorldYMax = chunkWorldYMin + VoxelChunk.ChunkSize;

                    if (chunkWorldYMax < worldLowerLimit || chunkWorldYMin > worldUpperLimit)
                        continue;

                    if (!activeChunks.ContainsKey(coord))
                    {
                        VoxelChunk newChunk = CreateChunkDataOnly(coord);
                        newlyGeneratedChunks.Add(newChunk);
                        chunksNeedingMeshUpdate.Add(newChunk);
                    }
                }
            }
        }

        // Flag neighbors of new chunks to update their meshes so borders stitch seamlessly
        foreach (var chunk in newlyGeneratedChunks)
        {
            Vector3Int pos = chunk.ChunkCoord;
            CheckAndFlagNeighbor(pos + Vector3Int.right, chunksNeedingMeshUpdate);
            CheckAndFlagNeighbor(pos + Vector3Int.left, chunksNeedingMeshUpdate);
            CheckAndFlagNeighbor(pos + Vector3Int.up, chunksNeedingMeshUpdate);
            CheckAndFlagNeighbor(pos + Vector3Int.down, chunksNeedingMeshUpdate);
            CheckAndFlagNeighbor(pos + Vector3Int.forward, chunksNeedingMeshUpdate);
            CheckAndFlagNeighbor(pos + Vector3Int.back, chunksNeedingMeshUpdate);
        }

        // PASS 2: Mesh Generation.
        // Now that all data is physically present in the arrays, build the geometry.
        foreach (var chunk in chunksNeedingMeshUpdate)
        {
            chunk.RebuildMesh(registry, this);
        }
    }

    private void CheckAndFlagNeighbor(Vector3Int coord, List<VoxelChunk> updateList)
    {
        if (activeChunks.TryGetValue(coord, out VoxelChunk chunk))
        {
            if (!updateList.Contains(chunk))
            {
                updateList.Add(chunk);
            }
        }
    }

    private VoxelChunk CreateChunkDataOnly(Vector3Int coord)
    {
        GameObject chunkObj = new GameObject($"Chunk_{coord.x}_{coord.y}_{coord.z}");
        chunkObj.transform.SetParent(transform);
        chunkObj.transform.position = new Vector3(
            coord.x * VoxelChunk.ChunkSize,
            coord.y * VoxelChunk.ChunkSize,
            coord.z * VoxelChunk.ChunkSize
        );

        VoxelChunk chunk = chunkObj.AddComponent<VoxelChunk>();
        float chunkAmbientTemp = CalculateChunkAmbientTemperature(coord);
        chunk.Initialize(coord, chunkAmbientTemp);

        if (defaultChunkMaterial != null)
        {
            chunkObj.GetComponent<MeshRenderer>().material = defaultChunkMaterial;
        }

        GenerateTerrainData(chunk);
        chunk.SyncVoxelDataToNative();
        activeChunks.Add(coord, chunk);

        return chunk;
    }

    private float CalculateChunkAmbientTemperature(Vector3Int coord)
    {
        int centerGlobalX = (coord.x * VoxelChunk.ChunkSize) + (VoxelChunk.ChunkSize / 2);
        int centerGlobalZ = (coord.z * VoxelChunk.ChunkSize) + (VoxelChunk.ChunkSize / 2);

        float tempNoise = Mathf.PerlinNoise((centerGlobalX + worldSeed) * temperatureScale, (centerGlobalZ + worldSeed) * temperatureScale);
        VoxelBiomeDefinition biome = registry.GetBiomeForLocation(tempNoise, 0.5f);

        float biomeScalar = biome != null ? biome.targetTemperature : 0.5f;
        return basePlanetaryTemperature + ((biomeScalar - 0.5f) * 2.0f * biomeTemperatureVariance);
    }

    private void GenerateTerrainData(VoxelChunk chunk)
    {
        int startX = chunk.ChunkCoord.x * VoxelChunk.ChunkSize;
        int startY = chunk.ChunkCoord.y * VoxelChunk.ChunkSize;
        int startZ = chunk.ChunkCoord.z * VoxelChunk.ChunkSize;

        for (int x = 0; x < VoxelChunk.ChunkSize; x++)
        {
            for (int z = 0; z < VoxelChunk.ChunkSize; z++)
            {
                int globalX = startX + x;
                int globalZ = startZ + z;

                float temp = Mathf.PerlinNoise((globalX + worldSeed) * temperatureScale, (globalZ + worldSeed) * temperatureScale);
                float hum = Mathf.PerlinNoise((globalX - worldSeed) * humidityScale, (globalZ - worldSeed) * humidityScale);

                VoxelBiomeDefinition biome = registry.GetBiomeForLocation(temp, hum);

                int surfaceHeight = (biome != null && biome.noiseSettings != null)
                ? Mathf.FloorToInt(biome.noiseSettings.Evaluate2DSurface(globalX, globalZ, worldSeed))
                : 16;

                for (int y = 0; y < VoxelChunk.ChunkSize; y++)
                {
                    int globalY = startY + y;

                    if (globalY < worldLowerLimit || globalY > worldUpperLimit)
                    {
                        chunk.SetBlockLocal(x, y, z, 0);
                        continue;
                    }

                    ushort blockID = biome != null
                    ? biome.GetBlockForHeight(globalY, surfaceHeight, globalX, globalZ, worldSeed, registry)
                    : (ushort)0;

                    if (chunk.GetBlockLocal(x, y, z) == 0)
                    {
                        chunk.SetBlockLocal(x, y, z, blockID);
                    }

                    if (globalY == surfaceHeight && blockID != 0 && biome != null && biome.structures != null)
                    {
                        TrySpawnStructures(chunk, x, y, z, globalX, globalZ, biome);
                    }
                }
            }
        }
    }

    private void TrySpawnStructures(VoxelChunk chunk, int localX, int localY, int localZ, int globalX, int globalZ, VoxelBiomeDefinition biome)
    {
        foreach (var structDef in biome.structures)
        {
            if (structDef == null) continue;

            float hash = Mathf.Abs((globalX * 73856093 ^ globalZ * 19349663 ^ worldSeed) % 10000) / 10000f;
            if (hash < structDef.spawnChance)
            {
                int globalY = (chunk.ChunkCoord.y * VoxelChunk.ChunkSize) + localY + 1;
                if (globalY <= worldUpperLimit)
                {
                    VoxelStructureGenerator.PlaceStructure(
                        chunk,
                        new Vector3Int(localX, localY + 1, localZ),
                                                           structDef,
                                                           registry
                    );
                }
                break;
            }
        }
    }

    private void UpdateThermalSimulation()
    {
        if (activeChunks.Count == 0) return;

        float deltaTime = Time.deltaTime * thermalTickInterval;
        NativeList<JobHandle> jobHandles = new NativeList<JobHandle>(activeChunks.Count, Allocator.Temp);

        foreach (var chunk in activeChunks.Values)
        {
            JobHandle handle = chunk.ScheduleThermalJob(deltaTime, chunk.AmbientTemperature);
            jobHandles.Add(handle);
        }

        JobHandle combinedHandle = JobHandle.CombineDependencies(jobHandles);
        combinedHandle.Complete();

        jobHandles.Dispose();
    }

    public ushort GetBlockAtGlobal(int globalX, int globalY, int globalZ)
    {
        return GetBlockAtGlobal(globalX, globalY, globalZ, out _);
    }

    // Sets a block at global coordinates. Returns true if set on a loaded chunk.
    public bool SetBlockAtGlobal(int globalX, int globalY, int globalZ, ushort blockID, bool rebuild = true)
    {
        if (globalY < worldLowerLimit || globalY > worldUpperLimit)
            return false;

        Vector3Int coord = GetChunkCoordFromGlobal(globalX, globalY, globalZ);

        if (activeChunks.TryGetValue(coord, out VoxelChunk chunk))
        {
            int localX = globalX - (coord.x * VoxelChunk.ChunkSize);
            int localY = globalY - (coord.y * VoxelChunk.ChunkSize);
            int localZ = globalZ - (coord.z * VoxelChunk.ChunkSize);

            if (localX >= 0 && localX < VoxelChunk.ChunkSize && localY >= 0 && localY < VoxelChunk.ChunkSize && localZ >= 0 && localZ < VoxelChunk.ChunkSize)
            {
                chunk.SetBlockLocal(localX, localY, localZ, blockID);

                if (rebuild)
                {
                    chunk.RebuildMesh(registry, this);

                    // Also rebuild simple neighbors to keep seams consistent
                    Vector3Int[] neighbors = new Vector3Int[] { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down, Vector3Int.forward, Vector3Int.back };
                    foreach (var n in neighbors)
                    {
                        Vector3Int nc = coord + n;
                        if (activeChunks.TryGetValue(nc, out VoxelChunk neigh))
                        {
                            neigh.RebuildMesh(registry, this);
                        }
                    }
                }

                return true;
            }
        }

        return false;
    }

    public ushort GetBlockAtGlobal(int globalX, int globalY, int globalZ, out bool isLoaded)
    {
        isLoaded = true;

        if (globalY < worldLowerLimit || globalY > worldUpperLimit)
            return 0;

        Vector3Int coord = GetChunkCoordFromGlobal(globalX, globalY, globalZ);

        if (activeChunks.TryGetValue(coord, out VoxelChunk chunk))
        {
            int localX = globalX - (coord.x * VoxelChunk.ChunkSize);
            int localY = globalY - (coord.y * VoxelChunk.ChunkSize);
            int localZ = globalZ - (coord.z * VoxelChunk.ChunkSize);
            return chunk.GetBlockLocal(localX, localY, localZ);
        }

        isLoaded = false;
        return 0;
    }

    public Vector3Int GetChunkCoordFromPosition(Vector3 pos)
    {
        // Defensive check for invalid input
        if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z))
        {
            return Vector3Int.zero;
        }
        return GetChunkCoordFromGlobal(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
    }

    private Vector3Int GetChunkCoordFromGlobal(int globalX, int globalY, int globalZ)
    {
        return new Vector3Int(
            FloorToChunk(globalX),
                              FloorToChunk(globalY),
                              FloorToChunk(globalZ)
        );
    }

    private int FloorToChunk(int value)
    {
        return value >= 0 ? value / VoxelChunk.ChunkSize : (value - VoxelChunk.ChunkSize + 1) / VoxelChunk.ChunkSize;
    }
}
