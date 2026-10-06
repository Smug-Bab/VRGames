using System.Collections.Generic;
using UnityEngine;

public class VoxelMeshBuilder
{
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<int> triangles = new List<int>();
    private readonly List<Color32> colors = new List<Color32>();
    private readonly int chunkSize;

    public VoxelMeshBuilder(int size)
    {
        chunkSize = size;
    }

    public Mesh GenerateMesh(Vector3Int chunkCoord, ushort[] voxelData, VoxelRegistry registry, VoxelWorldManager worldManager)
    {
        vertices.Clear();
        triangles.Clear();
        colors.Clear();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    // Utilizing standard flattened array index
                    ushort blockID = voxelData[x | (y << 4) | (z << 8)];
                    if (blockID == 0) continue;

                    VoxelBlockDefinition blockDef = registry.GetBlock(blockID);
                    if (blockDef == null) continue;

                    int globalX = (chunkCoord.x * chunkSize) + x;
                    int globalY = (chunkCoord.y * chunkSize) + y;
                    int globalZ = (chunkCoord.z * chunkSize) + z;

                    // Fix B: Apply noise-driven procedural composition shifting
                    Color32 blockColor = GetDynamicVoxelColor(blockDef.CalculatedBlockColor, globalX, globalY, globalZ, worldManager.worldSeed);
                    Vector3 blockPos = new Vector3(x, y, z);

                    if (IsAir(voxelData, x, y, z - 1, chunkCoord, worldManager))
                        BuildFace(blockPos, FaceDirection.Back, blockColor);

                    if (IsAir(voxelData, x, y, z + 1, chunkCoord, worldManager))
                        BuildFace(blockPos, FaceDirection.Front, blockColor);

                    if (IsAir(voxelData, x - 1, y, z, chunkCoord, worldManager))
                        BuildFace(blockPos, FaceDirection.Left, blockColor);

                    if (IsAir(voxelData, x + 1, y, z, chunkCoord, worldManager))
                        BuildFace(blockPos, FaceDirection.Right, blockColor);

                    if (IsAir(voxelData, x, y - 1, z, chunkCoord, worldManager))
                        BuildFace(blockPos, FaceDirection.Bottom, blockColor);

                    if (IsAir(voxelData, x, y + 1, z, chunkCoord, worldManager))
                        BuildFace(blockPos, FaceDirection.Top, blockColor);
                }
            }
        }

        Mesh mesh = new Mesh();
        if (vertices.Count > 0)
        {
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        return mesh;
    }

    // Fix B: Perturb the elemental formula's color based on 3D world position
    private Color32 GetDynamicVoxelColor(Color32 baseColor, int globalX, int globalY, int globalZ, int seed)
    {
        float scale = 0.03f;

        // Simplex/Perlin noise shifting to simulate composition variances
        float hueNoise = (Mathf.PerlinNoise((globalX + seed) * scale, (globalZ + seed) * scale) - 0.5f) * 0.08f;
        float valNoise = (Mathf.PerlinNoise((globalX - seed) * scale, (globalY + seed) * scale) - 0.5f) * 0.15f;

        Color.RGBToHSV(baseColor, out float h, out float s, out float v);

        h += hueNoise;
        if (h > 1f) h -= 1f;
        if (h < 0f) h += 1f;

        v = Mathf.Clamp01(v + valNoise);
        s = Mathf.Clamp01(s + (valNoise * 0.4f));

        return Color.HSVToRGB(h, s, v);
    }

    private bool IsAir(ushort[] localData, int x, int y, int z, Vector3Int chunkCoord, VoxelWorldManager worldManager)
    {
        if (x >= 0 && x < chunkSize && y >= 0 && y < chunkSize && z >= 0 && z < chunkSize)
        {
            return localData[x | (y << 4) | (z << 8)] == 0;
        }

        int globalX = (chunkCoord.x * chunkSize) + x;
        int globalY = (chunkCoord.y * chunkSize) + y;
        int globalZ = (chunkCoord.z * chunkSize) + z;

        ushort neighborBlock = worldManager.GetBlockAtGlobal(globalX, globalY, globalZ, out bool isLoaded);

        if (!isLoaded) return false;

        return neighborBlock == 0;
    }

    private enum FaceDirection { Back, Front, Left, Right, Bottom, Top }

    private void BuildFace(Vector3 pos, FaceDirection direction, Color32 color)
    {
        int v0Count = vertices.Count;

        Vector3 v000 = pos + new Vector3(0, 0, 0);
        Vector3 v100 = pos + new Vector3(1, 0, 0);
        Vector3 v110 = pos + new Vector3(1, 1, 0);
        Vector3 v010 = pos + new Vector3(0, 1, 0);
        Vector3 v001 = pos + new Vector3(0, 0, 1);
        Vector3 v101 = pos + new Vector3(1, 0, 1);
        Vector3 v111 = pos + new Vector3(1, 1, 1);
        Vector3 v011 = pos + new Vector3(0, 1, 1);

        switch (direction)
        {
            case FaceDirection.Back: // -Z
                vertices.Add(v000); vertices.Add(v010); vertices.Add(v110); vertices.Add(v100);
                break;
            case FaceDirection.Front: // +Z
                vertices.Add(v101); vertices.Add(v111); vertices.Add(v011); vertices.Add(v001);
                break;
            case FaceDirection.Left: // -X
                vertices.Add(v001); vertices.Add(v011); vertices.Add(v010); vertices.Add(v000);
                break;
            case FaceDirection.Right: // +X
                vertices.Add(v100); vertices.Add(v110); vertices.Add(v111); vertices.Add(v101);
                break;
            case FaceDirection.Bottom: // -Y
                vertices.Add(v001); vertices.Add(v000); vertices.Add(v100); vertices.Add(v101);
                break;
            case FaceDirection.Top: // +Y
                vertices.Add(v010); vertices.Add(v011); vertices.Add(v111); vertices.Add(v110);
                break;
        }

        for (int i = 0; i < 4; i++)
        {
            colors.Add(color);
        }

        triangles.Add(v0Count);
        triangles.Add(v0Count + 1);
        triangles.Add(v0Count + 2);
        triangles.Add(v0Count);
        triangles.Add(v0Count + 2);
        triangles.Add(v0Count + 3);
    }
}
