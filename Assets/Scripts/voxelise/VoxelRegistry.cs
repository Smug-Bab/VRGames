using UnityEngine;
using System.Collections.Generic;

// Safely pull in Editor namespaces at the absolute top of the file
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

[CreateAssetMenu(fileName = "New Voxel Registry", menuName = "Voxel Engine/Voxel Registry")]
public class VoxelRegistry : ScriptableObject
{
    [Header("Automatic Tracking Configurations")]
    [Tooltip("The path relative to the project Assets folder where your blocks reside.")]
    public string blocksSearchFolderPath = "Scripts/voxelise/world/blocks";

    [Header("Registered Biomes")]
    public List<VoxelBiomeDefinition> registeredBiomes;

    [Header("Baked Internal Database (Serialized & Persistent in Build)")]
    [Tooltip("This list auto-populates via the editor scan button and is stored permanently in the asset data for standalone builds.")]
    [SerializeField] private List<VoxelBlockDefinition> autoDiscoveredBlocks = new List<VoxelBlockDefinition>();

    private Dictionary<ushort, VoxelBlockDefinition> idToBlock = new Dictionary<ushort, VoxelBlockDefinition>();
    private Dictionary<VoxelBlockDefinition, ushort> blockToId = new Dictionary<VoxelBlockDefinition, ushort>();

    /// <summary>
    /// Populates the list using Unity's AssetDatabase API.
    /// This function is wrapped entirely in conditional compilation so it is ignored during builds.
    /// </summary>
    public void ScanFolderForBlockDefinitions()
    {
        #if UNITY_EDITOR
        autoDiscoveredBlocks.Clear();

        // Format path string safely to guarantee AssetDatabase matches target folder
        string fullPath = "Assets/" + blocksSearchFolderPath.Trim('/', ' ');

        // Find all unique file asset GUIDs matching the VoxelBlockDefinition profile
        string[] assetGuids = AssetDatabase.FindAssets("t:VoxelBlockDefinition", new[] { fullPath });

        HashSet<VoxelBlockDefinition> uniquelyFound = new HashSet<VoxelBlockDefinition>();

        for (int i = 0; i < assetGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
            VoxelBlockDefinition blockAsset = AssetDatabase.LoadAssetAtPath<VoxelBlockDefinition>(assetPath);

            if (blockAsset != null)
            {
                uniquelyFound.Add(blockAsset);
            }
        }

        autoDiscoveredBlocks.AddRange(uniquelyFound);

        // Mark the asset as dirty so Unity saves the newly discovered blocks to your hard drive
        EditorUtility.SetDirty(this);

        Debug.Log($"[Registry Scan] Success! Permanently baked {autoDiscoveredBlocks.Count} block assets from '{fullPath}' into registry asset data.");
        #else
        Debug.LogWarning("Folder asset scanning can only be performed inside the Unity Editor environment.");
        #endif
    }

    /// <summary>
    /// This runs on game start. Because autoDiscoveredBlocks was serialized in the editor,
    /// it contains your blocks—even in a compiled standalone game build!
    /// </summary>
    public void Initialize()
    {
        idToBlock.Clear();
        blockToId.Clear();

        HashSet<VoxelBlockDefinition> compiledBlocksList = new HashSet<VoxelBlockDefinition>();

        // 1. Gather all blocks pre-discovered and saved into the internal array data
        for (int i = 0; i < autoDiscoveredBlocks.Count; i++)
        {
            if (autoDiscoveredBlocks[i] != null)
            {
                compiledBlocksList.Add(autoDiscoveredBlocks[i]);
            }
        }

        // 2. Fallback backup check: scrape active biomes to capture items placed elsewhere
        if (registeredBiomes != null)
        {
            for (int i = 0; i < registeredBiomes.Count; i++)
            {
                VoxelBiomeDefinition biome = registeredBiomes[i];
                if (biome == null) continue;

                if (biome.terrainLayers != null)
                {
                    for (int l = 0; l < biome.terrainLayers.Count; l++)
                    {
                        if (biome.terrainLayers[l].block != null)
                            compiledBlocksList.Add(biome.terrainLayers[l].block);
                    }
                }

                if (biome.caveSettings != null)
                {
                    if (biome.caveSettings.baseStoneBlock != null)
                        compiledBlocksList.Add(biome.caveSettings.baseStoneBlock);

                    if (biome.caveSettings.oreSpawns != null)
                    {
                        for (int o = 0; o < biome.caveSettings.oreSpawns.Count; o++)
                        {
                            if (biome.caveSettings.oreSpawns[o].block != null)
                                compiledBlocksList.Add(biome.caveSettings.oreSpawns[o].block);
                        }
                    }
                }
            }
        }

        // 3. Air block configuration safety assignment
        bool airBlockExists = false;
        VoxelBlockDefinition airInstance = null;

        foreach (VoxelBlockDefinition block in compiledBlocksList)
        {
            if (block != null && block.name.ToLower() == "air")
            {
                airBlockExists = true;
                airInstance = block;
                break;
            }
        }

        if (!airBlockExists)
        {
            airInstance = CreateInstance<VoxelBlockDefinition>();
            airInstance.name = "Air";
            airInstance.isTransparent = true;
            airInstance.blockColor = new Color32(0, 0, 0, 0);
            compiledBlocksList.Add(airInstance);
        }

        // 4. Register blocks and generate unique deterministic ushort hashes
        foreach (VoxelBlockDefinition block in compiledBlocksList)
        {
            if (block == null) continue;

            ushort assignedID;

            if (block.name.ToLower() == "air")
            {
                assignedID = 0;
            }
            else
            {
                unchecked
                {
                    int hash = block.name.GetHashCode();
                    uint unsignedHash = (uint)hash;
                    assignedID = (ushort)((unsignedHash % 65534) + 1);
                }
            }

            if (idToBlock.ContainsKey(assignedID))
            {
                Debug.LogError($"[Registry ID Collision] '{block.name}' shares ID hash {assignedID} with '{idToBlock[assignedID].name}'. Choose a different asset name string.");
                continue;
            }

            idToBlock[assignedID] = block;
            blockToId[block] = assignedID;
        }
    }

    public VoxelBlockDefinition GetBlock(ushort id)
    {
        if (idToBlock.TryGetValue(id, out var b)) return b;
        return idToBlock.ContainsKey(0) ? idToBlock[0] : null;
    }

    public ushort GetBlockID(VoxelBlockDefinition block)
    {
        if (block != null && blockToId.TryGetValue(block, out var id)) return id;
        return 0;
    }
}


// ===================================================================================
// UNITY EDITOR EXTENSIONS (Wrapped securely inside conditional compiling for builds)
// ===================================================================================
#if UNITY_EDITOR
[CustomEditor(typeof(VoxelRegistry))]
public class VoxelRegistryInspectorDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VoxelRegistry myScript = (VoxelRegistry)target;

        GUILayout.Space(15);
        GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
        if (GUILayout.Button("🔄 Run Auto-Populate Scan", GUILayout.Height(32)))
        {
            myScript.ScanFolderForBlockDefinitions();
            AssetDatabase.SaveAssets();
        }
        GUI.backgroundColor = Color.white;
    }
}

/// <summary>
/// Hook that intercepts Unity's compiler loop right when you click "Build".
/// </summary>
public class VoxelRegistryBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("[Build Pipeline] Intercepting build to automate block registry collection...");

        string[] registryGuids = AssetDatabase.FindAssets("t:VoxelRegistry");
        if (registryGuids == null || registryGuids.Length == 0) return;

        for (int i = 0; i < registryGuids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(registryGuids[i]);
            VoxelRegistry registry = AssetDatabase.LoadAssetAtPath<VoxelRegistry>(assetPath);

            if (registry != null)
            {
                Debug.Log($"[Build Pipeline] Automatically running block scan on registry found at: {assetPath}");
                registry.ScanFolderForBlockDefinitions();
            }
        }

        // Save the modifications to your registry file asset prior to build execution wrapping up
        AssetDatabase.SaveAssets();
    }
}
#endif
