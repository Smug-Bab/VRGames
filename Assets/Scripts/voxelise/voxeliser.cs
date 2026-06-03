using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Infinite terrain generator using tiny voxels with procedural generation
/// </summary>
public class Voxeliser : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float voxelSize = 0.1f; // Size of individual voxels
    [SerializeField] private int chunkSize = 16; // Number of voxels per chunk dimension
    [SerializeField] private int renderDistance = 3; // How many chunks to load around player
    [SerializeField] private Material voxelMaterial;

    [Header("Terrain Generation")]
    [SerializeField] private float heightScale = 20f;
    [SerializeField] private float heightOffset = 0f;
    [SerializeField] private float noiseScale = 0.05f;
    [SerializeField] private int octaves = 4;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private float lacunarity = 2f;

    [Header("Performance")]
    [SerializeField] private bool useMultithreading = true;
    [SerializeField] private int maxChunksPerFrame = 2;

    private Dictionary<Vector3Int, VoxelChunk> activeChunks = new Dictionary<Vector3Int, VoxelChunk>();
    private Queue<Vector3Int> chunksToGenerate = new Queue<Vector3Int>();
    private HashSet<Vector3Int> visibleChunks = new HashSet<Vector3Int>();
    private Transform playerTransformRef;
    private Vector3Int lastPlayerChunk = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue); // Force initial update

    private void Start()
    {
        if (playerTransform != null)
            playerTransformRef = playerTransform;
        else
            playerTransformRef = Camera.main?.transform;
        
        if (playerTransformRef == null)
            playerTransformRef = transform;

        Debug.Log($"[Voxeliser] Started. Player at {playerTransformRef.position}");
        Debug.Log($"[Voxeliser] Voxel size: {voxelSize}, Chunk size: {chunkSize}, Render distance: {renderDistance}");
        InitializeTerrainGenerator();
    }

    private void InitializeTerrainGenerator()
    {
        if (voxelMaterial == null)
        {
            voxelMaterial = new Material(Shader.Find("Standard"));
        }
    }

    private void Update()
    {
        UpdateVisibleChunks();
        ProcessChunkQueue();
    }

    /// <summary>
    /// Updates which chunks should be visible based on player position
    /// </summary>
    private void UpdateVisibleChunks()
    {
        Vector3Int playerChunk = GetChunkCoordinate(playerTransformRef.position);

        if (playerChunk == lastPlayerChunk)
            return;

        Debug.Log($"[Voxeliser] Player moved to chunk {playerChunk} from {lastPlayerChunk}");
        lastPlayerChunk = playerChunk;
        visibleChunks.Clear();

        // Queue chunks within render distance
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector3Int chunkCoord = playerChunk + new Vector3Int(x, y, z);
                    visibleChunks.Add(chunkCoord);

                    if (!activeChunks.ContainsKey(chunkCoord))
                    {
                        chunksToGenerate.Enqueue(chunkCoord);
                    }
                }
            }
        }

        Debug.Log($"[Voxeliser] Queued {chunksToGenerate.Count} chunks to generate");

        // Unload distant chunks
        List<Vector3Int> chunksToRemove = new List<Vector3Int>();
        foreach (var chunkCoord in activeChunks.Keys)
        {
            if (!visibleChunks.Contains(chunkCoord))
            {
                chunksToRemove.Add(chunkCoord);
            }
        }

        foreach (var chunkCoord in chunksToRemove)
        {
            DestroyChunk(chunkCoord);
        }
    }

    /// <summary>
    /// Processes queued chunks for generation
    /// </summary>
    private void ProcessChunkQueue()
    {
        int chunksProcessed = 0;
        while (chunksToGenerate.Count > 0 && chunksProcessed < maxChunksPerFrame)
        {
            Vector3Int chunkCoord = chunksToGenerate.Dequeue();
            GenerateChunk(chunkCoord);
            chunksProcessed++;
        }
    }

    /// <summary>
    /// Generates a single voxel chunk with surface-only voxels and cliff shells
    /// </summary>
    private void GenerateChunk(Vector3Int chunkCoord)
    {
        VoxelChunk chunk = new VoxelChunk(chunkCoord, chunkSize, voxelSize);
        int voxelCount = 0;
        Vector3 chunkOrigin = chunk.GetChunkWorldPosition();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                Vector3 worldXZ = new Vector3(chunkOrigin.x + x * voxelSize, 0f, chunkOrigin.z + z * voxelSize);
                float height = GetTerrainHeight(worldXZ.x, worldXZ.z);
                int worldTopY = Mathf.FloorToInt(height / voxelSize);
                int localTopY = worldTopY - chunkCoord.y * chunkSize;

                if (localTopY >= 0 && localTopY < chunkSize)
                {
                    Vector3 worldPos = chunk.GetVoxelWorldPosition(x, localTopY, z);
                    byte voxelType = GetVoxelType(worldPos, height);
                    if (chunk.SetVoxel(x, localTopY, z, voxelType))
                        voxelCount++;
                }

                // Add surface cliff voxels where neighbor columns are lower
                Vector3[] neighborOffsets = new Vector3[]
                {
                    new Vector3(voxelSize, 0f, 0f),
                    new Vector3(-voxelSize, 0f, 0f),
                    new Vector3(0f, 0f, voxelSize),
                    new Vector3(0f, 0f, -voxelSize)
                };

                for (int n = 0; n < neighborOffsets.Length; n++)
                {
                    Vector3 neighborXZ = worldXZ + neighborOffsets[n];
                    float neighborHeight = GetTerrainHeight(neighborXZ.x, neighborXZ.z);
                    int neighborTopY = Mathf.FloorToInt(neighborHeight / voxelSize);
                    int fillStartY = neighborTopY + 1;
                    int fillEndY = worldTopY;

                    for (int worldY = fillStartY; worldY <= fillEndY; worldY++)
                    {
                        int localY = worldY - chunkCoord.y * chunkSize;
                        if (localY >= 0 && localY < chunkSize)
                        {
                            Vector3 worldPos = chunk.GetVoxelWorldPosition(x, localY, z);
                            byte voxelType = GetVoxelType(worldPos, height);
                            if (chunk.SetVoxel(x, localY, z, voxelType))
                                voxelCount++;
                        }
                    }
                }
            }
        }

        Debug.Log($"[Voxeliser] Generated chunk {chunkCoord} with {voxelCount} surface voxels");

        if (voxelCount == 0)
        {
            activeChunks[chunkCoord] = chunk;
            return;
        }

        GameObject chunkObject = CreateChunkMesh(chunk);
        chunk.GameObject = chunkObject;
        activeChunks[chunkCoord] = chunk;
    }

    /// <summary>
    /// Gets terrain height at a specific 2D position using Perlin noise
    /// </summary>
    private float GetTerrainHeight(float x, float z)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float height = 0f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = x * noiseScale * frequency;
            float sampleZ = z * noiseScale * frequency;

            height += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;
            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        float finalHeight = (height / maxValue) * heightScale + heightOffset;
        return finalHeight > 0 ? finalHeight : 5f; // Guarantee minimum height
    }

    /// <summary>
    /// Determines voxel type based on position
    /// </summary>
    private byte GetVoxelType(Vector3 position, float terrainHeight)
    {
        float distanceFromSurface = terrainHeight - position.y;

        if (distanceFromSurface > 5f)
            return 1; // Stone
        else if (distanceFromSurface > 1f)
            return 2; // Dirt
        else
            return 3; // Grass (top layer)
    }

    /// <summary>
    /// Creates a mesh GameObject for a chunk
    /// </summary>
    private GameObject CreateChunkMesh(VoxelChunk chunk)
    {
        GameObject chunkObject = new GameObject($"Chunk_{chunk.Coordinate}");
        chunkObject.transform.position = chunk.GetChunkWorldPosition();

        MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = chunkObject.AddComponent<MeshCollider>();

        meshRenderer.material = voxelMaterial;

        // Generate mesh from voxel data
        Mesh mesh = GenerateMesh(chunk);
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;

        Debug.Log($"[Voxeliser] Created mesh with {mesh.vertices.Length} vertices at {chunkObject.transform.position}");
        return chunkObject;
    }

    /// <summary>
    /// Generates a mesh from voxel data using greedy meshing
    /// </summary>
    private Mesh GenerateMesh(VoxelChunk chunk)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();
        List<Vector2> uvs = new List<Vector2>();

        // Simple cube meshing - one quad per visible face
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    byte voxel = chunk.GetVoxel(x, y, z);
                    if (voxel == 0) continue;

                    Vector3 voxelPos = new Vector3(x, y, z) * voxelSize;
                    Color voxelColor = GetVoxelColor(voxel);

                    // Check each face and add if exposed
                    AddFaceIfExposed(chunk, x, y, z, vertices, triangles, colors, uvs, voxelPos, voxelColor, 0, 1, 0); // Top
                    AddFaceIfExposed(chunk, x, y, z, vertices, triangles, colors, uvs, voxelPos, voxelColor, 0, -1, 0); // Bottom
                    AddFaceIfExposed(chunk, x, y, z, vertices, triangles, colors, uvs, voxelPos, voxelColor, 1, 0, 0); // Right
                    AddFaceIfExposed(chunk, x, y, z, vertices, triangles, colors, uvs, voxelPos, voxelColor, -1, 0, 0); // Left
                    AddFaceIfExposed(chunk, x, y, z, vertices, triangles, colors, uvs, voxelPos, voxelColor, 0, 0, 1); // Front
                    AddFaceIfExposed(chunk, x, y, z, vertices, triangles, colors, uvs, voxelPos, voxelColor, 0, 0, -1); // Back
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        return mesh;
    }

    /// <summary>
    /// Adds a face quad if the adjacent voxel is empty
    /// </summary>
    private void AddFaceIfExposed(VoxelChunk chunk, int x, int y, int z, List<Vector3> vertices, List<int> triangles, List<Color> colors, List<Vector2> uvs, Vector3 voxelPos, Color color, int dx, int dy, int dz)
    {
        int nx = x + dx;
        int ny = y + dy;
        int nz = z + dz;

        if (!IsVoxelSolid(chunk, nx, ny, nz))
        {
            AddQuad(vertices, triangles, colors, uvs, voxelPos, color, dx, dy, dz);
        }
    }

    /// <summary>
    /// Checks if a voxel position contains a solid voxel
    /// </summary>
    private bool IsVoxelSolid(VoxelChunk chunk, int x, int y, int z)
    {
        if (x < 0 || x >= chunkSize || y < 0 || y >= chunkSize || z < 0 || z >= chunkSize)
            return false;

        return chunk.GetVoxel(x, y, z) != 0;
    }

    /// <summary>
    /// Adds a quad face to the mesh with correct winding order and explicit normals
    /// </summary>
    private void AddQuad(List<Vector3> vertices, List<int> triangles, List<Color> colors, List<Vector2> uvs, Vector3 voxelPos, Color color, int dx, int dy, int dz)
    {
        int startIndex = vertices.Count;
        float s = voxelSize;

        if (dx > 0) // Right face (+X)
        {
            vertices.Add(voxelPos + new Vector3(s, 0, 0));
            vertices.Add(voxelPos + new Vector3(s, s, 0));
            vertices.Add(voxelPos + new Vector3(s, s, s));
            vertices.Add(voxelPos + new Vector3(s, 0, s));
        }
        else if (dx < 0) // Left face (-X)
        {
            vertices.Add(voxelPos + new Vector3(0, 0, s));
            vertices.Add(voxelPos + new Vector3(0, s, s));
            vertices.Add(voxelPos + new Vector3(0, s, 0));
            vertices.Add(voxelPos + new Vector3(0, 0, 0));
        }
        else if (dy > 0) // Top face (+Y)
        {
            vertices.Add(voxelPos + new Vector3(0, s, 0));
            vertices.Add(voxelPos + new Vector3(0, s, s));
            vertices.Add(voxelPos + new Vector3(s, s, s));
            vertices.Add(voxelPos + new Vector3(s, s, 0));
        }
        else if (dy < 0) // Bottom face (-Y)
        {
            vertices.Add(voxelPos + new Vector3(0, 0, s));
            vertices.Add(voxelPos + new Vector3(0, 0, 0));
            vertices.Add(voxelPos + new Vector3(s, 0, 0));
            vertices.Add(voxelPos + new Vector3(s, 0, s));
        }
        else if (dz > 0) // Front face (+Z)
        {
            vertices.Add(voxelPos + new Vector3(0, 0, s));
            vertices.Add(voxelPos + new Vector3(s, 0, s));
            vertices.Add(voxelPos + new Vector3(s, s, s));
            vertices.Add(voxelPos + new Vector3(0, s, s));
        }
        else if (dz < 0) // Back face (-Z)
        {
            vertices.Add(voxelPos + new Vector3(s, 0, 0));
            vertices.Add(voxelPos + new Vector3(0, 0, 0));
            vertices.Add(voxelPos + new Vector3(0, s, 0));
            vertices.Add(voxelPos + new Vector3(s, s, 0));
        }

        triangles.Add(startIndex);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 3);

        for (int i = 0; i < 4; i++)
        {
            colors.Add(color);
            uvs.Add(new Vector2(i % 2, i / 2));
        }
    }

    /// <summary>
    /// Gets color for voxel type
    /// </summary>
    private Color GetVoxelColor(byte voxelType)
    {
        return voxelType switch
        {
            1 => new Color(0.5f, 0.5f, 0.5f), // Stone - gray
            2 => new Color(0.6f, 0.4f, 0.2f), // Dirt - brown
            3 => new Color(0.2f, 0.8f, 0.2f), // Grass - green
            _ => Color.white
        };
    }

    /// <summary>
    /// Gets chunk coordinate from world position
    /// </summary>
    private Vector3Int GetChunkCoordinate(Vector3 worldPos)
    {
        float chunkWorldSize = chunkSize * voxelSize;
        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x / chunkWorldSize),
            Mathf.FloorToInt(worldPos.y / chunkWorldSize),
            Mathf.FloorToInt(worldPos.z / chunkWorldSize)
        );
    }

    /// <summary>
    /// Destroys a chunk and removes it from active chunks
    /// </summary>
    private void DestroyChunk(Vector3Int chunkCoord)
    {
        if (activeChunks.TryGetValue(chunkCoord, out VoxelChunk chunk))
        {
            if (chunk.GameObject != null)
                Destroy(chunk.GameObject);

            activeChunks.Remove(chunkCoord);
        }
    }

    private void OnDestroy()
    {
        foreach (var chunk in activeChunks.Values)
        {
            if (chunk.GameObject != null)
                Destroy(chunk.GameObject);
        }
        activeChunks.Clear();
    }
}

/// <summary>
/// Represents a single voxel chunk
/// </summary>
public class VoxelChunk
{
    public Vector3Int Coordinate { get; private set; }
    public GameObject GameObject { get; set; }

    private byte[] voxelData;
    private int size;
    private float voxelSize;

    public VoxelChunk(Vector3Int coordinate, int chunkSize, float voxelSize)
    {
        Coordinate = coordinate;
        size = chunkSize;
        this.voxelSize = voxelSize;
        voxelData = new byte[chunkSize * chunkSize * chunkSize];
    }

    public bool SetVoxel(int x, int y, int z, byte value)
    {
        if (x < 0 || x >= size || y < 0 || y >= size || z < 0 || z >= size)
            return false;

        int index = x + y * size + z * size * size;
        if (voxelData[index] != 0)
            return false;

        voxelData[index] = value;
        return true;
    }

    public byte GetVoxel(int x, int y, int z)
    {
        if (x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size)
            return voxelData[x + y * size + z * size * size];
        return 0;
    }

    public Vector3 GetVoxelWorldPosition(int x, int y, int z)
    {
        return GetChunkWorldPosition() + new Vector3(x, y, z) * voxelSize;
    }

    public Vector3 GetChunkWorldPosition()
    {
        return new Vector3(Coordinate.x, Coordinate.y, Coordinate.z) * (size * voxelSize);
    }
}
