using UnityEngine;

public class DynamicVoxel : MonoBehaviour
{
    public ushort BlockID { get; private set; }
    public Color32 BlockColor { get; private set; }
    public VoxelBlockDefinition BlockDefinition { get; private set; }
    public Material BlockMaterial { get; private set; }

    private Mesh assignedMesh;

    public void Initialize(ushort blockID, VoxelBlockDefinition blockDef, Mesh mesh, Material mat)
    {
        BlockID = blockID;
        BlockDefinition = blockDef;
        BlockColor = blockDef != null ? blockDef.blockColor : Color.white;
        assignedMesh = mesh;
        BlockMaterial = mat;
    }

    private void OnDestroy()
    {
        if (assignedMesh != null)
        {
            Destroy(assignedMesh);
        }
    }
}
