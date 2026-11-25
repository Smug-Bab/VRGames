using UnityEngine;

[CreateAssetMenu(menuName = "Voxel/Voxel Def")]
public class VoxelDef : ScriptableObject
{
    public string formula = "SiO2";
    public string[] tags;                       // optional, used by biome modifiers to target groups

    public ushort BaseId => MaterialRegistry.Id(formula);

    [ContextMenu("Print variables")]
    void Print() => Debug.Log(new Compound(formula).Describe());

    public bool Matches(string key)
    {
        if (string.IsNullOrEmpty(key)) return true;
        if (name == key) return true;
        return tags != null && System.Array.IndexOf(tags, key) >= 0;
    }
}
