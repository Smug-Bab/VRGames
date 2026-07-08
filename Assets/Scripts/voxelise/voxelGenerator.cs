using UnityEngine;
using System.Collections.Generic;

public static class VoxelGenerator
{
    private static readonly Vector3Int[] FaceDirections = new Vector3Int[]
    {
        new Vector3Int(1, 0, 0),   
        new Vector3Int(-1, 0, 0),  
        new Vector3Int(0, 1, 0),   
        new Vector3Int(0, -1, 0),  
        new Vector3Int(0, 0, 1),   
        new Vector3Int(0, 0, -1)   
    };

    private static readonly Vector3[][] FaceVertices = new Vector3[][]
    {
        new Vector3[] { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1) },
        new Vector3[] { new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(0, 0, 0) },
        new Vector3[] { new Vector3(0, 1, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0) },
        new Vector3[] { new Vector3(0, 0, 1), new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1) },
        new Vector3[] { new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1), new Vector3(0, 0, 1) },
        new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0) }
    };

    private static readonly Vector3[] FaceNormals = new Vector3[]
    {
        Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
    };

    private static readonly Vector2[] FaceUVs = new Vector2[]
    {
        new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)
    };

    public static MeshData GenerateMeshData(VoxelData data, byte seed, float noiseScale, float heightMultiplier, 
                                            bool enableCaves, float caveNoiseScale, float caveThreshold, 
                                            int chunkHeight, float voxelSize, MeshData container,
                                            VoxelData neighborsRight = null, VoxelData neighborsLeft = null,
                                            VoxelData neighborsUp = null, VoxelData neighborsDown = null,
                                            VoxelData neighborsForward = null, VoxelData neighborsBack = null)
    {
        if (container == null) container = new MeshData();
        else container.Clear();

        container.voxelData = data;
        int size = data.ChunkSize;
        int sizeSq = size * size;
        uint[] voxels = data.Voxels; 

        int worldOffsetX = data.ChunkCoord.x * size;
        int worldOffsetY = data.ChunkCoord.y * size;
        int worldOffsetZ = data.ChunkCoord.z * size;

        int estimatedCapacity = sizeSq * 2; 
        if (container.vertices.Capacity < estimatedCapacity)
        {
            container.vertices.Capacity = estimatedCapacity;
            container.triangles.Capacity = estimatedCapacity * 2;
            container.normals.Capacity = estimatedCapacity;
            container.uvs.Capacity = estimatedCapacity;
        }

        Vector3Int[] faceDirs = FaceDirections;
        Vector3[][] faceVertsTable = FaceVertices;
        Vector3[] faceNormals = FaceNormals;
        Vector2[] faceUVs = FaceUVs;

        for (int y = 0; y < size; y++)
        {
            int yOffset = y * sizeSq;
            float scaledY = y * voxelSize;
            int worldY = worldOffsetY + y;

            for (int z = 0; z < size; z++)
            {
                int zOffset = yOffset + (z * size);
                float scaledZ = z * voxelSize;
                int worldZ = worldOffsetZ + z;

                for (int x = 0; x < size; x++)
                {
                    int currentVoxelIndex = zOffset + x;

                    bool isCurrentSolid = (voxels[currentVoxelIndex >> 5] & (1U << (currentVoxelIndex & 31))) != 0;
                    if (!isCurrentSolid) continue;

                    float scaledX = x * voxelSize;
                    int worldX = worldOffsetX + x;

                    for (int i = 0; i < 6; i++)
                    {
                        Vector3Int dir = faceDirs[i];
                        int nx = x + dir.x;
                        int ny = y + dir.y;
                        int nz = z + dir.z;

                        bool isFaceExposed = false;

                        if (nx < 0 || nx >= size || ny < 0 || ny >= size || nz < 0 || nz >= size)
                        {
                            bool neighborIsSolid = false;
                            bool hasNeighborChunk = false;

                            if (i == 0 && neighborsRight != null) { neighborIsSolid = neighborsRight.IsVoxelSolidLocal(0, y, z); hasNeighborChunk = true; }
                            else if (i == 1 && neighborsLeft != null) { neighborIsSolid = neighborsLeft.IsVoxelSolidLocal(size - 1, y, z); hasNeighborChunk = true; }
                            else if (i == 2 && neighborsUp != null) { neighborIsSolid = neighborsUp.IsVoxelSolidLocal(x, 0, z); hasNeighborChunk = true; }
                            else if (i == 3 && neighborsDown != null) { neighborIsSolid = neighborsDown.IsVoxelSolidLocal(x, size - 1, z); hasNeighborChunk = true; }
                            else if (i == 4 && neighborsForward != null) { neighborIsSolid = neighborsForward.IsVoxelSolidLocal(x, y, 0); hasNeighborChunk = true; }
                            else if (i == 5 && neighborsBack != null) { neighborIsSolid = neighborsBack.IsVoxelSolidLocal(x, y, size - 1); hasNeighborChunk = true; }

                            if (hasNeighborChunk)
                            {
                                if (!neighborIsSolid) isFaceExposed = true;
                            }
                            else
                            {
                                int targetWorldY = worldY + dir.y;
                                
                                // FIX: If the neighbor chunk data isn't loaded/cached yet, assume it is completely 
                                // solid underground (below chunkHeight boundary). This prevents rendering random vertical walls 
                                // during asynchronous world generation frames.
                                if (targetWorldY < chunkHeight)
                                {
                                    neighborIsSolid = true; 
                                }
                                else
                                {
                                    neighborIsSolid = false; 
                                }

                                if (!neighborIsSolid) isFaceExposed = true;
                            }
                        }
                        else
                        {
                            int neighborIndex = (ny * sizeSq) + (nz * size) + nx;
                            bool isNeighborSolid = (voxels[neighborIndex >> 5] & (1U << (neighborIndex & 31))) != 0;
                            if (!isNeighborSolid)
                            {
                                isFaceExposed = true;
                            }
                        }

                        if (isFaceExposed)
                        {
                            int vertexCount = container.vertices.Count;
                            Vector3[] currentFaceVerts = faceVertsTable[i];
                            Vector3 norm = faceNormals[i];

                            for (int v = 0; v < 4; v++)
                            {
                                Vector3 rawOffset = currentFaceVerts[v];
                                container.vertices.Add(new Vector3(
                                    scaledX + (rawOffset.x * voxelSize),
                                    scaledY + (rawOffset.y * voxelSize),
                                    scaledZ + (rawOffset.z * voxelSize)
                                ));
                                container.normals.Add(norm);
                                container.uvs.Add(faceUVs[v]);
                            }

                            container.triangles.Add(vertexCount);
                            container.triangles.Add(vertexCount + 1);
                            container.triangles.Add(vertexCount + 2);
                            
                            container.triangles.Add(vertexCount);
                            container.triangles.Add(vertexCount + 2);
                            container.triangles.Add(vertexCount + 3);
                        }
                    }
                }
            }
        }

        return container;
    }
}

public class MeshData
{
    public VoxelData voxelData;
    public readonly List<Vector3> vertices;
    public readonly List<int> triangles;
    public readonly List<Vector3> normals;
    public readonly List<Vector2> uvs;

    public MeshData()
    {
        vertices = new List<Vector3>(4096);
        triangles = new List<int>(6144);
        normals = new List<Vector3>(4096);
        uvs = new List<Vector2>(4096);
    }

    public void Clear()
    {
        voxelData = null;
        vertices.Clear();
        triangles.Clear();
        normals.Clear();
        uvs.Clear();
    }
}