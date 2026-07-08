using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelRender : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh chunkMesh;
    private bool hasAssignedMeshToCollider = false;
    private UnityEngine.Rendering.IndexFormat currentFormat = UnityEngine.Rendering.IndexFormat.UInt16;

    public void SetupComponentsPool()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
        
        if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();
        
        if (chunkMesh == null)
        {
            chunkMesh = new Mesh();
            chunkMesh.MarkDynamic(); 
            meshFilter.sharedMesh = chunkMesh;
            currentFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        }
    }

    public int GetSharedMeshID()
    {
        return chunkMesh != null ? chunkMesh.GetInstanceID() : 0;
    }

    public void Initialize(MeshData meshData, VoxelWorld worldManager, float voxelSize, int unscaledWorldY)
    {
        if (meshRenderer == null) SetupComponentsPool();

        float worldChunkScale = meshData.voxelData.ChunkSize * voxelSize;
        transform.localPosition = new Vector3(
            meshData.voxelData.ChunkCoord.x * worldChunkScale, 
            meshData.voxelData.ChunkCoord.y * worldChunkScale, 
            meshData.voxelData.ChunkCoord.z * worldChunkScale
        );

        meshRenderer.sharedMaterial = worldManager.GetMaterialForPosition(unscaledWorldY);
        hasAssignedMeshToCollider = false;
        
        ApplyMesh(meshData, voxelSize);
    }

    public void SetOptimizationState(bool shouldBeVisible, bool enableCollider)
    {
        if (gameObject.activeSelf != shouldBeVisible)
        {
            gameObject.SetActive(shouldBeVisible);
        }

        if (meshCollider != null)
        {
            if (meshCollider.enabled != enableCollider)
            {
                meshCollider.enabled = enableCollider;
            }

            if (enableCollider && !hasAssignedMeshToCollider && chunkMesh != null && chunkMesh.vertexCount > 0)
            {
                if (meshCollider.sharedMesh != chunkMesh)
                {
                    meshCollider.sharedMesh = chunkMesh;
                }
                hasAssignedMeshToCollider = true;
            }
        }
    }

    private void ApplyMesh(MeshData meshData, float voxelSize)
    {
        hasAssignedMeshToCollider = false;

        if (meshData.vertices.Count == 0)
        {
            chunkMesh.Clear(keepVertexLayout: false);
            if (meshCollider != null && meshCollider.sharedMesh != null) 
            {
                meshCollider.sharedMesh = null;
            }
            return;
        }

        UnityEngine.Rendering.IndexFormat targetFormat = meshData.vertices.Count > 65535 ? 
            UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

        if (targetFormat != currentFormat)
        {
            chunkMesh.Clear(keepVertexLayout: false);
            chunkMesh.indexFormat = targetFormat;
            currentFormat = targetFormat;
        }
        else
        {
            chunkMesh.Clear(keepVertexLayout: true);
        }
            
        chunkMesh.SetVertices(meshData.vertices);
        chunkMesh.SetTriangles(meshData.triangles, 0, calculateBounds: false); 
        chunkMesh.SetNormals(meshData.normals);
        chunkMesh.SetUVs(0, meshData.uvs);
        
        float size = meshData.voxelData.ChunkSize * voxelSize;
        chunkMesh.bounds = new Bounds(new Vector3(size * 0.5f, size * 0.5f, size * 0.5f), new Vector3(size, size, size));

        if (meshCollider != null && meshCollider.enabled)
        {
            if (meshCollider.sharedMesh != chunkMesh)
            {
                meshCollider.sharedMesh = chunkMesh;
            }
            hasAssignedMeshToCollider = true;
        }
    }

    public void ClearAndDisable()
    {
        if (chunkMesh != null) chunkMesh.Clear(keepVertexLayout: false);
        if (meshCollider != null && meshCollider.sharedMesh != null) meshCollider.sharedMesh = null;
        hasAssignedMeshToCollider = false;
        gameObject.SetActive(false);
    }

    public void DestroyPoolInstance()
    {
        if (chunkMesh != null) Destroy(chunkMesh);
        Destroy(gameObject);
    }
}