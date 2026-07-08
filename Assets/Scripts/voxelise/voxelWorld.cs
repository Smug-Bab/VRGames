using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    [System.Serializable]
    public struct ChunkMaterialConfig
    {
        public string name;
        public int minWorldY;
        public int maxWorldY;
        public Material material;
    }

    [Header("Player Tracking")]
    public Transform playerTransform;
    public bool usePlayerTransform = true;
    public int viewDistanceChunks = 5;

    [Header("Camera Culling")]
    public Camera cullingCamera;
    public bool useFrustumCulling = true;

    [Header("Chunk Settings")]
    public byte chunkSize = 16;
    public byte chunkHeight = 32;
    public float voxelSize = 1f; 
    public byte seed = 0;
    public float noiseScale = 0.1f;
    public float heightMultiplier = 0.5f;

    [Header("Cave / Carver Settings")]
    public bool enableCaves = true;
    public float caveNoiseScale = 0.15f;
    [Range(0.1f, 0.6f)] public float caveThreshold = 0.35f;

    [Header("Rendering")]
    public Material defaultMaterial;
    public List<ChunkMaterialConfig> heightMaterials = new List<ChunkMaterialConfig>();
    public bool useCollider = true;
    public bool generateOnStart = true;

    [Header("Optimization & Culling")]
    public float maxColliderDistance = 48f; 
    private float maxColliderDistanceSq; 
    public int maxMeshBuildsPerFrame = 4; 
    public float optimizationInterval = 0.1f; 

    private readonly Dictionary<Vector3Int, VoxelData> worldData = new Dictionary<Vector3Int, VoxelData>();
    private readonly Dictionary<Vector3Int, VoxelRender> activeRenderers = new Dictionary<Vector3Int, VoxelRender>();
    private readonly HashSet<Vector3Int> chunksLoadingAsync = new HashSet<Vector3Int>();
    
    private readonly ConcurrentQueue<VoxelRender> chunkPool = new ConcurrentQueue<VoxelRender>();
    private readonly ConcurrentQueue<MeshData> meshDataPool = new ConcurrentQueue<MeshData>();
    private const int INITIAL_POOL_SIZE = 128; 

    private struct MainThreadQueueItem
    {
        public Vector3Int chunkCoord;
        public MeshData meshData;
        public long generationTicket; 
        public int bakedMeshID; 
    }

    private readonly ConcurrentQueue<MainThreadQueueItem> mainThreadMeshQueue = new ConcurrentQueue<MainThreadQueueItem>();
    private readonly Dictionary<Vector3Int, long> activeTickets = new Dictionary<Vector3Int, long>();
    private long ticketCounter = 0;

    private Vector3Int currentChunkCenter3D = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
    private Transform chunkParent;
    private bool isUpdating = false;

    private Plane[] cameraPlanes = new Plane[6];
    private readonly List<Vector3Int> chunksToRemoveCache = new List<Vector3Int>();
    private readonly HashSet<Vector3Int> requiredChunksCache = new HashSet<Vector3Int>();
    
    private string cachedSaveDir;
    private float optimizationTimer = 0f;
    private readonly object dataLock = new object();

    void Awake()
    {
        if (usePlayerTransform && playerTransform == null)
            playerTransform = Camera.main?.transform;

        if (cullingCamera == null)
            cullingCamera = Camera.main;

        chunkParent = new GameObject("VoxelChunks").transform;
        chunkParent.SetParent(transform, false);

        maxColliderDistanceSq = maxColliderDistance * maxColliderDistance;
        cachedSaveDir = GetSaveDirectory();

        PreWarmChunkPool();
    }

    private void PreWarmChunkPool()
    {
        for (int i = 0; i < INITIAL_POOL_SIZE; i++)
        {
            GameObject go = new GameObject("Pooled_Chunk_Instance");
            go.transform.SetParent(chunkParent, false);
            
            VoxelRender r = go.AddComponent<VoxelRender>();
            r.SetupComponentsPool(); 
            go.SetActive(false);
            
            chunkPool.Enqueue(r);
            meshDataPool.Enqueue(new MeshData());
        }
    }

    void Start()
    {
        if (seed == 0) seed = (byte)UnityEngine.Random.Range(1, byte.MaxValue);
        if (generateOnStart) UpdateChunks(true);
    }

    void Update()
    {
        ProcessMeshQueue();

        Vector3 globalPlayerPos = usePlayerTransform && playerTransform != null ? playerTransform.position : Vector3.zero;
        
        optimizationTimer += Time.deltaTime;
        if (optimizationTimer >= optimizationInterval)
        {
            optimizationTimer = 0f;
            OptimizeActiveChunks(globalPlayerPos);
        }

        if (isUpdating) return;

        Vector3 localSourcePos = globalPlayerPos - transform.position;
        float totalChunkSizeWorld = chunkSize * voxelSize;
        
        Vector3Int newCenter3D = new Vector3Int(
            Mathf.FloorToInt(localSourcePos.x / totalChunkSizeWorld),
            Mathf.FloorToInt(localSourcePos.y / totalChunkSizeWorld),
            Mathf.FloorToInt(localSourcePos.z / totalChunkSizeWorld));

        if (newCenter3D != currentChunkCenter3D) 
        {
            UpdateChunks(false);
        }
    }

    private void UpdateChunks(bool force)
    {
        isUpdating = true;

        Vector3 globalPlayerPos = usePlayerTransform && playerTransform != null ? playerTransform.position : Vector3.zero;
        Vector3 localSourcePos = globalPlayerPos - transform.position;
        float totalChunkSizeWorld = chunkSize * voxelSize;
        
        Vector3Int newCenter3D = new Vector3Int(
            Mathf.FloorToInt(localSourcePos.x / totalChunkSizeWorld),
            Mathf.FloorToInt(localSourcePos.y / totalChunkSizeWorld),
            Mathf.FloorToInt(localSourcePos.z / totalChunkSizeWorld));

        if (!force && newCenter3D == currentChunkCenter3D)
        {
            isUpdating = false;
            return;
        }

        currentChunkCenter3D = newCenter3D;
        requiredChunksCache.Clear();

        int maxSurfaceHeight = Mathf.Max(1, Mathf.RoundToInt(chunkHeight * heightMultiplier));
        int absoluteMaxWorldY = chunkHeight + maxSurfaceHeight;
        int maxChunkY = Mathf.CeilToInt((float)absoluteMaxWorldY / chunkSize);
        int viewDistSq = viewDistanceChunks * viewDistanceChunks;

        for (int x = -viewDistanceChunks; x <= viewDistanceChunks; x++)
        {
            for (int z = -viewDistanceChunks; z <= viewDistanceChunks; z++)
            {
                if (x * x + z * z <= viewDistSq)
                {
                    for (int targetChunkY = 0; targetChunkY <= maxChunkY; targetChunkY++)
                    {
                        requiredChunksCache.Add(new Vector3Int(currentChunkCenter3D.x + x, targetChunkY, currentChunkCenter3D.z + z));
                    }
                }
            }
        }

        chunksToRemoveCache.Clear();
        lock (dataLock)
        {
            foreach (var pair in activeRenderers)
            {
                if (!requiredChunksCache.Contains(pair.Key)) chunksToRemoveCache.Add(pair.Key);
            }

            foreach (var chunkCoord in chunksToRemoveCache)
            {
                activeTickets.Remove(chunkCoord);

                if (activeRenderers.TryGetValue(chunkCoord, out VoxelRender r))
                {
                    activeRenderers.Remove(chunkCoord);
                    r.ClearAndDisable();
                    chunkPool.Enqueue(r);
                }

                if (worldData.TryGetValue(chunkCoord, out VoxelData data))
                {
                    if (data.isModified)
                    {
                        string filePath = Path.Combine(cachedSaveDir, $"chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}.dat");
                        byte[] exportBytes = data.ExportCompressedBytes();
                        Task.Run(() => SafeFileWrite(filePath, exportBytes));
                    }
                    worldData.Remove(chunkCoord);
                }
            }
        }

        foreach (var chunkCoord in requiredChunksCache)
        {
            lock (dataLock)
            {
                if (activeRenderers.ContainsKey(chunkCoord) || chunksLoadingAsync.Contains(chunkCoord))
                    continue;

                chunksLoadingAsync.Add(chunkCoord);
            }
            LoadChunkDataAsync(chunkCoord);
        }

        isUpdating = false;
    }

    private async void LoadChunkDataAsync(Vector3Int chunkCoord)
    {
        int size = chunkSize;
        int height = chunkHeight;
        byte s = seed;
        float nScale = noiseScale;
        float hMult = heightMultiplier;
        bool caves = enableCaves;
        float cScale = caveNoiseScale;
        float cThresh = caveThreshold;

        string filePath = Path.Combine(cachedSaveDir, $"chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}.dat");

        VoxelData vData = await Task.Run(() => 
        {
            byte[] savedBytes = null;
            if (File.Exists(filePath))
            {
                try { savedBytes = File.ReadAllBytes(filePath); } catch {}
            }

            if (savedBytes != null) return new VoxelData(chunkCoord, size, savedBytes);
            return new VoxelData(chunkCoord, size, height, s, nScale, hMult, caves, cScale, cThresh);
        });

        lock (dataLock)
        {
            if (!worldData.ContainsKey(chunkCoord))
            {
                worldData.Add(chunkCoord, vData);
            }
            else
            {
                vData = worldData[chunkCoord]; 
            }
        }

        BuildChunkMeshAsync(chunkCoord, vData);
    }

    private async void BuildChunkMeshAsync(Vector3Int chunkCoord, VoxelData vData)
    {
        float vSize = voxelSize;
        byte s = seed;
        float nScale = noiseScale;
        float hMult = heightMultiplier;
        bool caves = enableCaves;
        float cScale = caveNoiseScale;
        float cThresh = caveThreshold;
        int height = chunkHeight;

        long currentTicket;
        lock (dataLock)
        {
            ticketCounter++;
            currentTicket = ticketCounter;
            activeTickets[chunkCoord] = currentTicket;
        }

        if (!meshDataPool.TryDequeue(out MeshData pooledContainer))
        {
            pooledContainer = new MeshData();
        }

        VoxelData nRight, nLeft, nUp, nDown, nForward, nBack;
        lock (dataLock)
        {
            worldData.TryGetValue(chunkCoord + new Vector3Int(1, 0, 0), out nRight);
            worldData.TryGetValue(chunkCoord + new Vector3Int(-1, 0, 0), out nLeft);
            worldData.TryGetValue(chunkCoord + new Vector3Int(0, 1, 0), out nUp);
            worldData.TryGetValue(chunkCoord + new Vector3Int(0, -1, 0), out nDown);
            worldData.TryGetValue(chunkCoord + new Vector3Int(0, 0, 1), out nForward);
            worldData.TryGetValue(chunkCoord + new Vector3Int(0, 0, -1), out nBack);
        }

        MainThreadQueueItem queueItem = await Task.Run(() => 
        {
            MeshData mData = VoxelGenerator.GenerateMeshData(
                vData, s, nScale, hMult, caves, cScale, cThresh, height, vSize, pooledContainer,
                nRight, nLeft, nUp, nDown, nForward, nBack
            );
            
            int targetID = 0;
            if (mData.vertices.Count > 0 && useCollider)
            {
                Physics.BakeMesh(mData.voxelData.ChunkCoord.GetHashCode(), false);
            }

            return new MainThreadQueueItem { chunkCoord = chunkCoord, meshData = mData, generationTicket = currentTicket, bakedMeshID = targetID };
        });

        bool dynamicLoadConfirmed = false;
        lock (dataLock)
        {
            if (chunksLoadingAsync.Contains(chunkCoord))
            {
                chunksLoadingAsync.Remove(chunkCoord);
                dynamicLoadConfirmed = true;
            }
        }

        if (dynamicLoadConfirmed)
        {
            mainThreadMeshQueue.Enqueue(queueItem);

            Vector3Int[] directions = {
                new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
                new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
            };

            for (int i = 0; i < directions.Length; i++)
            {
                Vector3Int neighborCoord = chunkCoord + directions[i];
                VoxelData neighborData = null;
                bool containsRenderer = false;

                lock (dataLock)
                {
                    containsRenderer = activeRenderers.ContainsKey(neighborCoord);
                    if (containsRenderer) worldData.TryGetValue(neighborCoord, out neighborData);
                }

                if (containsRenderer && neighborData != null)
                {
                    ExecuteNeighborRemesh(neighborCoord, neighborData);
                }
            }
        }
        else
        {
            pooledContainer.Clear();
            meshDataPool.Enqueue(pooledContainer);
        }
    }

    private void ExecuteNeighborRemesh(Vector3Int neighborCoord, VoxelData neighborData)
    {
        MeshData uniqueContainer = new MeshData();

        long neighborTicket;
        lock (dataLock)
        {
            ticketCounter++;
            neighborTicket = ticketCounter;
            activeTickets[neighborCoord] = neighborTicket;
        }

        VoxelData nRight, nLeft, nUp, nDown, nForward, nBack;
        lock (dataLock)
        {
            worldData.TryGetValue(neighborCoord + new Vector3Int(1, 0, 0), out nRight);
            worldData.TryGetValue(neighborCoord + new Vector3Int(-1, 0, 0), out nLeft);
            worldData.TryGetValue(neighborCoord + new Vector3Int(0, 1, 0), out nUp);
            worldData.TryGetValue(neighborCoord + new Vector3Int(0, -1, 0), out nDown);
            worldData.TryGetValue(neighborCoord + new Vector3Int(0, 0, 1), out nForward); 
            worldData.TryGetValue(neighborCoord + new Vector3Int(0, 0, -1), out nBack);
        }

        Task.Run(() => 
        {
            MeshData updatedMesh = VoxelGenerator.GenerateMeshData(
                neighborData, seed, noiseScale, heightMultiplier, enableCaves, caveNoiseScale, caveThreshold, chunkHeight, voxelSize, uniqueContainer,
                nRight, nLeft, nUp, nDown, nForward, nBack
            );
            
            if (updatedMesh.vertices.Count > 0 && useCollider)
            {
                Physics.BakeMesh(updatedMesh.voxelData.ChunkCoord.GetHashCode(), false);
            }

            mainThreadMeshQueue.Enqueue(new MainThreadQueueItem { chunkCoord = neighborCoord, meshData = updatedMesh, generationTicket = neighborTicket, bakedMeshID = 0 });
        });
    }

    private void ProcessMeshQueue()
    {
        int buildsThisFrame = 0;
        int maxDistSq = viewDistanceChunks * viewDistanceChunks;

        while (mainThreadMeshQueue.Count > 0 && buildsThisFrame < maxMeshBuildsPerFrame)
        {
            if (mainThreadMeshQueue.TryDequeue(out MainThreadQueueItem item))
            {
                Vector3Int chunkCoord = item.chunkCoord;
                MeshData meshData = item.meshData;

                lock (dataLock)
                {
                    if (!activeTickets.TryGetValue(chunkCoord, out long dynamicTicket) || dynamicTicket != item.generationTicket)
                    {
                        meshData.Clear();
                        meshDataPool.Enqueue(meshData);
                        continue;
                    }
                }

                int distSq = (chunkCoord.x - currentChunkCenter3D.x) * (chunkCoord.x - currentChunkCenter3D.x) + 
                             (chunkCoord.y - currentChunkCenter3D.y) * (chunkCoord.y - currentChunkCenter3D.y) +
                             (chunkCoord.z - currentChunkCenter3D.z) * (chunkCoord.z - currentChunkCenter3D.z);

                if (distSq <= maxDistSq)
                {
                    VoxelRender renderer;
                    lock (dataLock)
                    {
                        if (activeRenderers.TryGetValue(chunkCoord, out VoxelRender existingRenderer))
                        {
                            existingRenderer.ClearAndDisable();
                            chunkPool.Enqueue(existingRenderer);
                            activeRenderers.Remove(chunkCoord);
                        }

                        renderer = GetOrCreatePooledRenderInstance();
                        if (renderer == null)
                        {
                            meshData.Clear();
                            meshDataPool.Enqueue(meshData);
                            continue;
                        }
                        renderer.gameObject.name = $"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}";
                        activeRenderers.Add(chunkCoord, renderer);
                    }

                    int unscaledWorldY = chunkCoord.y * chunkSize; 
                    renderer.Initialize(meshData, this, voxelSize, unscaledWorldY);
                    
                    // FIXED: Instantly engage visual state and collider mapping on assignment frame
                    renderer.SetOptimizationState(true, true);

                    buildsThisFrame++;
                }
                
                meshData.Clear();
                meshDataPool.Enqueue(meshData);
            }
        }
    }

    private VoxelRender GetOrCreatePooledRenderInstance()
    {
        if (chunkPool.TryDequeue(out VoxelRender r))
        {
            if (r != null && r.gameObject != null)
            {
                r.gameObject.SetActive(true);
                return r;
            }
        }

        GameObject go = new GameObject("Dynamic_Chunk_Extension");
        go.transform.SetParent(chunkParent, false);
        VoxelRender ren = go.AddComponent<VoxelRender>();
        ren.SetupComponentsPool();
        return ren;
    }

    private void OptimizeActiveChunks(Vector3 globalPlayerPos)
    {
        if (cullingCamera == null) return;

        Vector3 localSourcePos = globalPlayerPos - transform.position;
        float totalChunkSizeWorld = chunkSize * voxelSize;

        if (useFrustumCulling)
        {
            GeometryUtility.CalculateFrustumPlanes(cullingCamera, cameraPlanes);
        }

        Vector3 playerBlockPos = localSourcePos / voxelSize;

        lock (dataLock)
        {
            foreach (var pair in activeRenderers)
            {
                Vector3Int coord = pair.Key;
                VoxelRender renderer = pair.Value;

                if (renderer == null || renderer.gameObject == null) continue;

                bool isInsideCameraFrustum = true;
                if (useFrustumCulling)
                {
                    Vector3 chunkMin = new Vector3(coord.x, coord.y, coord.z) * totalChunkSizeWorld + transform.position;
                    Vector3 chunkSizeVector = new Vector3(totalChunkSizeWorld, totalChunkSizeWorld, totalChunkSizeWorld);
                    Bounds chunkBounds = new Bounds(chunkMin + chunkSizeVector * 0.5f, chunkSizeVector);
                    isInsideCameraFrustum = GeometryUtility.TestPlanesAABB(cameraPlanes, chunkBounds);
                }

                bool shouldEnableCollider = false;
                if (isInsideCameraFrustum && useCollider)
                {
                    float dx = (coord.x * chunkSize) - playerBlockPos.x;
                    float dy = (coord.y * chunkSize) - playerBlockPos.y;
                    float dz = (coord.z * chunkSize) - playerBlockPos.z;
                    float blockDistSq = (dx * dx) + (dy * dy) + (dz * dz);
                    
                    shouldEnableCollider = blockDistSq <= maxColliderDistanceSq;
                }

                renderer.SetOptimizationState(isInsideCameraFrustum, shouldEnableCollider);
            }
        }
    }

    public bool IsTerrainReadyAtPosition(Vector3 worldPos)
    {
        float totalChunkSizeWorld = chunkSize * voxelSize;
        Vector3Int chunkCoord = new Vector3Int(
            Mathf.FloorToInt(worldPos.x / totalChunkSizeWorld),
            Mathf.FloorToInt(worldPos.y / totalChunkSizeWorld),
            Mathf.FloorToInt(worldPos.z / totalChunkSizeWorld)
        );

        lock(dataLock)
        {
            if (activeRenderers.TryGetValue(chunkCoord, out VoxelRender renderer))
            {
                return renderer.IsColliderReady;
            }
        }
        return false;
    }

    public void SetVoxelAtWorldPosition(Vector3 worldPos, bool isSolid)
    {
        float totalChunkSizeWorld = chunkSize * voxelSize;

        Vector3Int chunkCoord = new Vector3Int(
            Mathf.FloorToInt(worldPos.x / totalChunkSizeWorld),
            Mathf.FloorToInt(worldPos.y / totalChunkSizeWorld),
            Mathf.FloorToInt(worldPos.z / totalChunkSizeWorld)
        );

        lock (dataLock)
        {
            if (worldData.TryGetValue(chunkCoord, out VoxelData data))
            {
                int localX = Mathf.FloorToInt((worldPos.x - (chunkCoord.x * totalChunkSizeWorld)) / voxelSize);
                int localY = Mathf.FloorToInt((worldPos.y - (chunkCoord.y * totalChunkSizeWorld)) / voxelSize);
                int localZ = Mathf.FloorToInt((worldPos.z - (chunkCoord.z * totalChunkSizeWorld)) / voxelSize);

                data.SetVoxel(localX, localY, localZ, isSolid);

                MeshData uniqueContainer = new MeshData();

                ticketCounter++;
                long currentTicket = ticketCounter;
                activeTickets[chunkCoord] = currentTicket;

                worldData.TryGetValue(chunkCoord + new Vector3Int(1, 0, 0), out VoxelData nRight);
                worldData.TryGetValue(chunkCoord + new Vector3Int(-1, 0, 0), out nLeft);
                worldData.TryGetValue(chunkCoord + new Vector3Int(0, 1, 0), out nUp);
                worldData.TryGetValue(chunkCoord + new Vector3Int(0, -1, 0), out nDown);
                worldData.TryGetValue(chunkCoord + new Vector3Int(0, 0, 1), out nForward);
                worldData.TryGetValue(chunkCoord + new Vector3Int(0, 0, -1), out nBack);

                Task.Run(() => {
                    MeshData updatedMesh = VoxelGenerator.GenerateMeshData(
                        data, seed, noiseScale, heightMultiplier, enableCaves, caveNoiseScale, caveThreshold, chunkHeight, voxelSize, uniqueContainer,
                        nRight, nLeft, nUp, nDown, nForward, nBack
                    );

                    mainThreadMeshQueue.Enqueue(new MainThreadQueueItem { chunkCoord = chunkCoord, meshData = updatedMesh, generationTicket = currentTicket, bakedMeshID = 0 });
                });
            }
        }
    }

    private string GetSaveDirectory()
    {
        string path;
#if UNITY_EDITOR
        path = Path.Combine(Application.dataPath, "Scenes", "voxelise", "WorldCache");
#else
        string buildDirectory = AppDomain.CurrentDomain.BaseDirectory;
        path = Path.Combine(buildDirectory, "voxelise");
#endif
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    public Material GetMaterialForPosition(int worldY)
    {
        int count = heightMaterials.Count;
        for (int i = 0; i < count; i++)
        {
            var config = heightMaterials[i];
            if (worldY >= config.minWorldY && worldY <= config.maxWorldY)
            {
                if (config.material != null) return config.material;
            }
        }
        return defaultMaterial;
    }

    private void SafeFileWrite(string path, byte[] bytes)
    {
        try { File.WriteAllBytes(path, bytes); } catch (Exception e) { Debug.LogError($"Voxel Save Failure: {e.Message}"); }
    }

    public void SaveAllModifiedChunksBeforeSceneChange()
    {
        lock (dataLock)
        {
            foreach (var pair in worldData)
            {
                if (pair.Value != null && pair.Value.isModified)
                {
                    string filePath = Path.Combine(cachedSaveDir, $"chunk_{pair.Key.x}_{pair.Key.y}_{pair.Key.z}.dat");
                    try 
                    { 
                        File.WriteAllBytes(filePath, pair.Value.ExportCompressedBytes()); 
                        pair.Value.isModified = false; 
                    } 
                    catch (Exception e) 
                    {
                        Debug.LogError($"Failed to save chunk during scene change: {e.Message}");
                    }
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        lock (dataLock)
        {
            foreach (var pair in worldData)
            {
                if (pair.Value != null && pair.Value.isModified)
                {
                    string filePath = Path.Combine(cachedSaveDir, $"chunk_{pair.Key.x}_{pair.Key.y}_{pair.Key.z}.dat");
                    try { File.WriteAllBytes(filePath, pair.Value.ExportCompressedBytes()); } catch {}
                }
            }
        }
        
        while (chunkPool.TryDequeue(out var r))
        {
            if (r != null) r.DestroyPoolInstance();
        }
    }

    void OnValidate()
    {
        chunkSize = (byte)Mathf.Max(1, chunkSize);
        chunkHeight = (byte)Mathf.Max(1, chunkHeight);
        voxelSize = Mathf.Max(0.01f, voxelSize);
        viewDistanceChunks = Mathf.Max(0, viewDistanceChunks);
        noiseScale = Mathf.Max(0.0001f, noiseScale);
        heightMultiplier = Mathf.Clamp(heightMultiplier, 0.01f, 4f);
        caveNoiseScale = Mathf.Max(0.0001f, caveNoiseScale);
        maxColliderDistance = Mathf.Max(0f, maxColliderDistance);
        maxColliderDistanceSq = maxColliderDistance * maxColliderDistance;
    }
}