using UnityEngine;

public class TerrainGen
{
    const int Region = 32;                      // structures must fit within this
    readonly BiomeDef[] biomes;
    readonly StructureDef[] structures;

    public TerrainGen(BiomeDef[] biomes, StructureDef[] structures)
    { this.biomes = biomes; this.structures = structures; }

    float Noise(float x, float z, float s, float o) => Mathf.PerlinNoise(x * s + o, z * s + o);

    public Climate ClimateAt(int wx, int wz) => new Climate
    {
        Temperature = Noise(wx, wz, 0.004f, 200f),
        Humidity    = Noise(wx, wz, 0.004f, 700f),
        Elevation   = Mathf.Clamp01(RawHeight(wx, wz) / 48f)
    };

    float RawHeight(int wx, int wz) =>
    Noise(wx, wz, 0.01f, 1000f) * 40f + Noise(wx, wz, 0.05f, 500f) * 8f;

    public BiomeDef BiomeAt(Climate c)
    {
        BiomeDef best = null; float bd = float.MaxValue;
        foreach (var b in biomes) { float d = b.Distance(c.Temperature, c.Humidity); if (d < bd) { bd = d; best = b; } }
        return best;
    }

    public int Height(int wx, int wz) => (int)(RawHeight(wx, wz) * BiomeAt(ClimateAt(wx, wz)).heightScale);

    public void Fill(Chunk c)
    {
        int ox = c.Coord.x * Chunk.S, oy = c.Coord.y * Chunk.S, oz = c.Coord.z * Chunk.S;

        for (int x = 0; x < Chunk.S; x++)
            for (int z = 0; z < Chunk.S; z++)
            {
                int wx = ox + x, wz = oz + z;
                var clim = ClimateAt(wx, wz);
                var biome = BiomeAt(clim);
                int h = (int)(RawHeight(wx, wz) * biome.heightScale);

                for (int y = 0; y < Chunk.S; y++)
                {
                    int wy = oy + y, depth = h - wy, i = Chunk.Idx(x, y, z);
                    ushort id = 0;
                    if (depth >= 0)
                    {
                        var def = biome.LayerAt(depth);
                        if (biome.ores != null)
                            foreach (var o in biome.ores)
                                if (depth >= o.minDepth && Hash(wx, wy, wz) % 1000 < (uint)o.perThousand) { def = o.voxel; break; }
                                id = biome.Resolve(def, clim);
                    }
                    c.Ids[i] = id;
                    c.Temp[i] = 288f + Mathf.Max(0, depth) * 0.5f;
                }
            }

            PlaceStructures(c, ox, oy, oz);
    }

    void PlaceStructures(Chunk c, int ox, int oy, int oz)
    {
        if (structures == null) return;
        int r0x = Mathf.FloorToInt((ox - Region) / (float)Region), r1x = Mathf.FloorToInt((ox + Chunk.S) / (float)Region);
        int r0z = Mathf.FloorToInt((oz - Region) / (float)Region), r1z = Mathf.FloorToInt((oz + Chunk.S) / (float)Region);

        foreach (var s in structures)
            for (int rx = r0x; rx <= r1x; rx++)
                for (int rz = r0z; rz <= r1z; rz++)
                {
                    uint h = Hash(rx, s.seed, rz);
                    if ((h % 1000) / 1000f >= s.chancePerRegion) continue;

                    int px = rx * Region + (int)((h >> 10) % Region);
                    int pz = rz * Region + (int)((h >> 18) % Region);
                    var clim = ClimateAt(px, pz);
                    var biome = BiomeAt(clim);
                    if (!s.AllowedIn(biome)) continue;
                    int py = (int)(RawHeight(px, pz) * biome.heightScale) + s.yOffset;

                    foreach (var b in s.blocks)
                    {
                        int wx = px + b.offset.x, wy = py + b.offset.y, wz = pz + b.offset.z;
                        int lx = wx - ox, ly = wy - oy, lz = wz - oz;
                        if (lx < 0 || ly < 0 || lz < 0 || lx >= Chunk.S || ly >= Chunk.S || lz >= Chunk.S) continue;
                        // Structures get biome composition too (using climate at the origin).
                        c.Ids[Chunk.Idx(lx, ly, lz)] = biome.Resolve(b.voxel, clim);
                    }
                }
    }

    static uint Hash(int x, int y, int z)
    {
        unchecked { uint h = (uint)(x * 374761393 + y * 668265263 + z * 2147483647);
            h = (h ^ (h >> 13)) * 1274126177; return h ^ (h >> 16); }
    }
}
