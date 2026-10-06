using UnityEngine;

[CreateAssetMenu(fileName = "New Voxel Element", menuName = "Modular Engine/Voxel Element")]
public class VoxelElement : ScriptableObject
{
    public string symbol;
    public string elementName;
    public Color elementColor = Color.white;
    public float atomicMass = 1.0f;
}
