using UnityEngine;
using System.Collections.Generic;

public class MazeGenerator : MonoBehaviour
{
    [Header("Prefab Lists By Exit Count")]
    public List<MazeModule> oneExitModules;
    public List<MazeModule> twoExitModules;
    public List<MazeModule> threeExitModules;
    public List<MazeModule> fourExitModules;

    [Header("Generation Settings")]
    public MazeModule startModule;
    public int maxModules = 50;

    private List<OpenExit> openExits = new();
    private List<MazeModule> placedModules = new();

    
    
    
    public void Generate()
    {
        if (startModule == null)
        {
            
            return;
        }

        
        foreach (var m in placedModules)
            if (m != null) Destroy(m.gameObject);

        placedModules.Clear();
        openExits.Clear();

        
        MazeModule start = Instantiate(startModule, Vector3.zero, Quaternion.identity);
        placedModules.Add(start);

        foreach (var e in start.exits)
            openExits.Add(new OpenExit(start, e));

        
        while (placedModules.Count < maxModules && openExits.Count > 0)
        {
            OpenExit open = openExits[0];
            openExits.RemoveAt(0);

            TryPlaceModule(open);
        }
    }

    
    
    
    void TryPlaceModule(OpenExit open)
    {
        List<MazeModule> list = GetListByExitCount(Random.Range(1, 5));
        if (list == null || list.Count == 0) return;

        MazeModule prefab = list[Random.Range(0, list.Count)];
        MazeModule newModule = Instantiate(prefab);

        Transform newExit = newModule.exits[Random.Range(0, newModule.exits.Length)];

        AlignExits(open.exit, newExit, newModule);

        if (IsIntersecting(newModule))
        {
            Destroy(newModule.gameObject);
            return;
        }

        placedModules.Add(newModule);

        foreach (var e in newModule.exits)
            if (e != newExit)
                openExits.Add(new OpenExit(newModule, e));
    }

    
    
    
    void AlignExits(Transform targetExit, Transform newExit, MazeModule module)
    {
        Quaternion rot = Quaternion.FromToRotation(newExit.forward, -targetExit.forward);
        module.transform.rotation = rot * module.transform.rotation;

        Vector3 offset = targetExit.position - newExit.position;
        module.transform.position += offset;
    }

    
    
    
    bool IsIntersecting(MazeModule module)
    {
        Collider[] cols = module.GetComponentsInChildren<Collider>();

        foreach (var col in cols)
        {
            Collider[] hits = Physics.OverlapBox(
                col.bounds.center,
                col.bounds.extents * 0.95f,
                col.transform.rotation
            );

            foreach (var hit in hits)
            {
                if (!hit.transform.IsChildOf(module.transform))
                    return true;
            }
        }

        return false;
    }

    
    
    
    List<MazeModule> GetListByExitCount(int count)
    {
        return count switch
        {
            1 => oneExitModules,
            2 => twoExitModules,
            3 => threeExitModules,
            4 => fourExitModules,
            _ => null
        };
    }

    
    
    
    [System.Serializable]
    public class MazeModule : MonoBehaviour
    {
        public Transform[] exits;
    }

    public class OpenExit
    {
        public MazeModule module;
        public Transform exit;

        public OpenExit(MazeModule m, Transform e)
        {
            module = m;
            exit = e;
        }
    }
}
