using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class ElementData
{
    public string symbol;
    public string name;
    public float atomicMass;
    public string defaultColor;
    public float visualWeight = 1f;

    public Color GetColor()
    {
        if (ColorUtility.TryParseHtmlString(defaultColor, out Color color))
            return color;
        return Color.white;
    }
}

[Serializable]
public class ElementListWrapper
{
    public List<ElementData> elements;
}

public static class VoxelElementRegistry
{
    private static readonly Dictionary<string, ElementData> elementsBySymbol = new Dictionary<string, ElementData>(StringComparer.OrdinalIgnoreCase);
    private static bool isInitialized = false;

    // Automatically initializes in the Editor when Unity loads or re-compiles
    #if UNITY_EDITOR
    [InitializeOnLoadMethod]
    #endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        elementsBySymbol.Clear();

        TextAsset jsonAsset = null;

        #if UNITY_EDITOR
        // Find ElementsDatabase.json anywhere in your project folder
        string[] guids = AssetDatabase.FindAssets("ElementsDatabase t:TextAsset");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }
        #else
        // Fallback for runtime builds (requires file in a Resources folder)
        jsonAsset = Resources.Load<TextAsset>("ElementsDatabase");
        #endif

        if (jsonAsset == null)
        {
            Debug.LogWarning("[VoxelElementRegistry] Could not find 'ElementsDatabase.json' in project assets.");
            return;
        }

        try
        {
            string wrappedJson = "{\"elements\":" + jsonAsset.text + "}";
            ElementListWrapper wrapper = JsonUtility.FromJson<ElementListWrapper>(wrappedJson);

            if (wrapper != null && wrapper.elements != null)
            {
                foreach (var elem in wrapper.elements)
                {
                    if (!string.IsNullOrWhiteSpace(elem.symbol))
                    {
                        elementsBySymbol[elem.symbol] = elem;
                    }
                }
                isInitialized = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoxelElementRegistry] Failed to parse JSON database: {e.Message}");
        }
    }

    #if UNITY_EDITOR
    [MenuItem("Tools/Voxelise/Reload Element Database")]
    public static void ReloadElementDatabase()
    {
        Debug.Log("[VoxelElementRegistry] Manually reloading ElementsDatabase.json...");
        isInitialized = false;
        Initialize();
        Debug.Log($"[VoxelElementRegistry] Database reloaded successfully! {elementsBySymbol.Count} elements loaded.");
    }
    #endif

    public static ElementData Get(string symbol)
    {
        if (!isInitialized) Initialize();
        elementsBySymbol.TryGetValue(symbol, out ElementData elem);
        return elem;
    }

    public static bool ContainsElement(string symbol)
    {
        if (!isInitialized) Initialize();
        return elementsBySymbol.ContainsKey(symbol);
    }
}
