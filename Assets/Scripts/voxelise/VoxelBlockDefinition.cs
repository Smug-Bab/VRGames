using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "New Voxel Block", menuName = "Voxel Engine/Block Definition")]
public class VoxelBlockDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("The unique in-game name for this block. Use this instead of the filename.")]
    public string blockName = "Unknown Block";

    [Header("Rendering")]
    [Tooltip("The color tint applied to this block's base texture.")]
    public Color blockColor = Color.white;
    public bool isTransparent;

    [Header("Behavior")]
    public bool isTickable;
    public float tickRate = 1f;

    [Header("Custom Logic Trigger")]
    public UnityEvent<Vector3Int> OnTickEvent;

    public virtual void OnBlockTicked(Vector3Int globalPos)
    {
        OnTickEvent?.Invoke(globalPos);
    }
}
