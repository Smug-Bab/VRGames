using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public Transform player;
    public Material voxelMaterial;
    public int viewRadius = 8;
    public int simRadius = 2;
    public int chunksPerFrame = 1;
    public int simChunksPerTick = 4;
    public float simTick = 1f;
    public bool electricity = true;

    [Header("LOD (distance in chunks)")]
    public int lod0Radius = 2;
    public int lod1Radius = 4;

    [Header("World definitions")]
    public BiomeDef[] biomes;
    public StructureDef[] structures;

    const int MinCY = -2, MaxCY = 3;
    readonly Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    readonly Dictionary<Vector3Int, GameObject> objects = new Dictionary<Vector3Int, GameObject>();
    readonly Dictionary<Vector3Int, int> meshLod = new Dictionary<Vector3Int, int>();
    readonly List<Chunk> simList = new List<Chunk>();
    readonly List<Vector3Int> remove = new List<Vector3Int>();
    Vector3Int playerChunk;
    int simCursor;
    float timer;
    TerrainGen gen;

    void Start()
    {
        gen = new TerrainGen(biomes, structures);
        if (voxelMaterial != null && voxelMaterial.HasProperty("_Cull"))
            voxelMaterial.SetInt("_Cull", 2);   // back-face culling
    }

    public ushort GetVoxel(int wx, int wy, int wz)
    {
        var cc = new Vector3Int(Mathf.FloorToInt(wx / (float)Chunk.S),
                                Mathf.FloorToInt(wy / (float)Chunk.S),
                                Mathf.FloorToInt(wz / (float)Chunk.S));
        if (!chunks.TryGetValue(cc, out var ch)) return 0;
        return ch.Ids[Chunk.Idx(wx - cc.x * Chunk.S, wy - cc.y * Chunk.S, wz - cc.z * Chunk.S)];
    }

    int DesiredLod(Vector3Int cc)
    {
        int d = Mathf.Max(Mathf.Abs(cc.x - playerChunk.x), Mathf.Abs(cc.z - playerChunk.z));
        return d <= lod0Radius ? 0 : d <= lod1Radius ? 1 : 2;
    }

    void Update()
    {
        if (gen == null || player == null) return;

        var p = new Vector3Int(Mathf.FloorToInt(player.position.x / Chunk.S), 0,
                               Mathf.FloorToInt(player.position.z / Chunk.S));
        playerChunk = p;

        int made = 0;
        for (int r = 0; r <= viewRadius && made < chunksPerFrame; r++)
            for (int dx = -r; dx <= r && made < chunksPerFrame; dx++)
                for (int dz = -r; dz <= r && made < chunksPerFrame; dz++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                    for (int cy = MinCY; cy <= MaxCY && made < chunksPerFrame; cy++)
                    {
                        var cc = new Vector3Int(p.x + dx, cy, p.z + dz);
                        if (chunks.ContainsKey(cc)) continue;
                        var newChunk = new Chunk(cc);
                        gen.Fill(newChunk);
                        chunks[cc] = newChunk;
                        made++;
                    }
                }

                remove.Clear();
                foreach (var k in chunks.Keys)
                    if (Mathf.Abs(k.x - p.x) > viewRadius + 1 || Mathf.Abs(k.z - p.z) > viewRadius + 1)
                        remove.Add(k);

        foreach (var k in remove)
        {
            if (objects.TryGetValue(k, out var go))
            {
                var mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) Destroy(mf.sharedMesh);
                Destroy(go);
                objects.Remove(k);
            }
            chunks.Remove(k);
            meshLod.Remove(k);
        }

        // Find one dirty chunk (LOD change flags it); nearest-first isn't needed, one per frame
        Chunk dirty = null;
        foreach (var kv in chunks)
        {
            var ch = kv.Value;
            if (!ch.Dirty && (!meshLod.TryGetValue(kv.Key, out int have) || have != DesiredLod(kv.Key)))
                ch.Dirty = true;
            if (ch.Dirty && dirty == null) dirty = ch;
        }
        if (dirty != null) Remesh(dirty);

        timer += Time.deltaTime;
        if (timer >= simTick)
        {
            simList.Clear();
            foreach (var kv in chunks)
                if (Mathf.Abs(kv.Key.x - p.x) <= simRadius && Mathf.Abs(kv.Key.z - p.z) <= simRadius)
                    simList.Add(kv.Value);

            for (int n = 0; n < simChunksPerTick && simList.Count > 0; n++)
            {
                simCursor = (simCursor + 1) % simList.Count;
                simList[simCursor].Step(timer * simList.Count / Mathf.Max(1, simChunksPerTick), electricity, GetVoxel);
            }
            timer = 0;
        }
    }

    void Remesh(Chunk ch)
    {
        ch.Dirty = false;
        int lod = DesiredLod(ch.Coord);
        meshLod[ch.Coord] = lod;

        Mesh mesh = ChunkMesher.Build(ch, GetVoxel, 1 << lod);

        if (!objects.TryGetValue(ch.Coord, out var go))
        {
            if (mesh == null) return;   // empty chunk: no GameObject
            go = new GameObject($"Chunk {ch.Coord}");
            go.transform.SetParent(transform);
            go.transform.position = (Vector3)(ch.Coord * Chunk.S);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>().sharedMaterial = voxelMaterial;
            go.AddComponent<MeshCollider>();
            objects[ch.Coord] = go;
        }

        var mfi = go.GetComponent<MeshFilter>();
        var old = mfi.sharedMesh;
        mfi.sharedMesh = mesh;

        var mc = go.GetComponent<MeshCollider>();
        bool col = lod == 0 && mesh != null;
        mc.sharedMesh = col ? mesh : null;
        mc.enabled = col;
        go.SetActive(mesh != null);

        if (old != null) Destroy(old);
    }
}
