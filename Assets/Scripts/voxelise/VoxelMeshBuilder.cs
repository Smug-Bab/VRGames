using UnityEngine;
using System.Collections.Generic;

public class VoxelMeshBuilder
{
    private int chunkSize;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Color32> colors = new List<Color32>();
    private int vertexIndex = 0;

    public VoxelMeshBuilder(int size)
    {
        this.chunkSize = size;
    }

    public Mesh GenerateMesh(Vector3Int chunkCoord, ushort[] chunkData, VoxelRegistry registry, VoxelWorldManager worldManager)
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        colors.Clear();
        vertexIndex = 0;

        int worldOriginX = chunkCoord.x << 4;
        int worldOriginY = chunkCoord.y << 4;
        int worldOriginZ = chunkCoord.z << 4;

        ushort airID = registry.GetBlockID(registry.GetBlock(0));

        for (int x = 0; x < chunkSize; x++)
        {
            int globalX = worldOriginX + x;
            for (int y = 0; y < chunkSize; y++)
            {
                int globalY = worldOriginY + y;
                for (int z = 0; z < chunkSize; z++)
                {
                    // High performance bitwise shift address flattening mapping
                    int index = x | (y << 4) | (z << 8);
                    ushort blockID = chunkData[index];

                    if (blockID == airID || blockID == 0) continue;

                    VoxelBlockDefinition currentBlock = registry.GetBlock(blockID);
                    if (currentBlock == null) continue;

                    Color32 blockColor = currentBlock.blockColor;
                    Vector3 blockPos = new Vector3(x, y, z);
                    int globalZ = worldOriginZ + z;

                    // Up face
                    if (CheckFaceVisible(x, y + 1, z, globalX, globalY + 1, globalZ, chunkData, registry, worldManager, airID))
                        BuildFace(blockPos, Vector3.up, Vector3.forward, Vector3.right, blockColor);

                    // Down face
                    if (CheckFaceVisible(x, y - 1, z, globalX, globalY - 1, globalZ, chunkData, registry, worldManager, airID))
                        BuildFace(blockPos, Vector3.down, Vector3.back, Vector3.right, blockColor);

                    // Front face
                    if (CheckFaceVisible(x, y, z + 1, globalX, globalY, globalZ + 1, chunkData, registry, worldManager, airID))
                        BuildFace(blockPos, Vector3.forward, Vector3.up, Vector3.left, blockColor);

                    // Back face
                    if (CheckFaceVisible(x, y, z - 1, globalX, globalY, globalZ - 1, chunkData, registry, worldManager, airID))
                        BuildFace(blockPos, Vector3.back, Vector3.up, Vector3.right, blockColor);

                    // Right face
                    if (CheckFaceVisible(x + 1, y, z, globalX + 1, globalY, globalZ, chunkData, registry, worldManager, airID))
                        BuildFace(blockPos, Vector3.right, Vector3.up, Vector3.forward, blockColor);

                    // Left face
                    if (CheckFaceVisible(x - 1, y, z, globalX - 1, globalY, globalZ, chunkData, registry, worldManager, airID))
                        BuildFace(blockPos, Vector3.left, Vector3.up, Vector3.back, blockColor);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.RecalculateNormals();

        return mesh;
    }

    private bool CheckFaceVisible(int localX, int localY, int localZ, int globalX, int globalY, int globalZ, ushort[] chunkData, VoxelRegistry registry, VoxelWorldManager worldManager, ushort airID)
    {
        ushort neighborID;

        // Perform simple boundary check instead of costly division methods
        if (localX < 0 || localX >= chunkSize || localY < 0 || localY >= chunkSize || localZ < 0 || localZ >= chunkSize)
        {
            neighborID = worldManager.GetBlockAtGlobal(globalX, globalY, globalZ);
        }
        else
        {
            neighborID = chunkData[localX | (localY << 4) | (localZ << 8)];
        }

        if (neighborID == 0 || neighborID == airID) return true;

        VoxelBlockDefinition neighborBlock = registry.GetBlock(neighborID);
        return neighborBlock != null && neighborBlock.isTransparent;
    }

    private void BuildFace(Vector3 blockPos, Vector3 faceNormal, Vector3 upDir, Vector3 rightDir, Color32 faceColor)
    {
        Vector3 halfOffset = (faceNormal + upDir + rightDir) * 0.5f;
        Vector3 v0 = blockPos + halfOffset;
        Vector3 v1 = blockPos + halfOffset - rightDir;
        Vector3 v2 = blockPos + halfOffset - rightDir - upDir;
        Vector3 v3 = blockPos + halfOffset - upDir;

        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v0);
        vertices.Add(v1);

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(0, 1));

        colors.Add(faceColor);
        colors.Add(faceColor);
        colors.Add(faceColor);
        colors.Add(faceColor);

        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 0);
        triangles.Add(vertexIndex + 3);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 0);

        vertexIndex += 4;
    }
}
