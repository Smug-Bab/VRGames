using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class ChunkMesher
{
    static readonly Vector3Int[] Dir = {
        new Vector3Int(1,0,0), new Vector3Int(-1,0,0), new Vector3Int(0,1,0),
        new Vector3Int(0,-1,0), new Vector3Int(0,0,1), new Vector3Int(0,0,-1) };

        static readonly Vector3[][] Face = {
            new[]{ new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(1,0,1) },
            new[]{ new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0), new Vector3(0,0,0) },
            new[]{ new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0) },
            new[]{ new Vector3(0,0,1), new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1) },
            new[]{ new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1), new Vector3(0,0,1) },
            new[]{ new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,0,0) } };

            // Reused buffers (main thread only)
            static readonly List<Vector3> V = new List<Vector3>(8192);
            static readonly List<int> T = new List<int>(12288);
            static readonly List<Color> C = new List<Color>(8192);
            static readonly ushort[] Cells = new ushort[(Chunk.S + 2) * (Chunk.S + 2) * (Chunk.S + 2)];

            // step = 1, 2 or 4. Returns null if the chunk has no visible faces.
            public static Mesh Build(Chunk c, System.Func<int, int, int, ushort> world, int step = 1)
            {
                int n = Chunk.S / step;
                int m = n + 2;
                var cells = Cells;
                Vector3Int o = c.Coord * Chunk.S;
                var ids = c.Ids;
                int S = Chunk.S;

                for (int x = -1; x <= n; x++)
                    for (int y = -1; y <= n; y++)
                        for (int z = -1; z <= n; z++)
                        {
                            ushort id = 0;
                            for (int dy = step - 1; dy >= 0 && id == 0; dy--)
                                for (int dx = 0; dx < step && id == 0; dx++)
                                    for (int dz = 0; dz < step && id == 0; dz++)
                                    {
                                        int lx = x * step + dx, ly = y * step + dy, lz = z * step + dz;
                                        id = (lx >= 0 && ly >= 0 && lz >= 0 && lx < S && ly < S && lz < S)
                                        ? ids[(lx * S + ly) * S + lz]
                                        : world(o.x + lx, o.y + ly, o.z + lz);
                                    }
                                    cells[((x + 1) * m + (y + 1)) * m + (z + 1)] = id;
                        }

                        var v = V; var t = T; var col = C;
                        v.Clear(); t.Clear(); col.Clear();

                        for (int x = 0; x < n; x++)
                            for (int y = 0; y < n; y++)
                                for (int z = 0; z < n; z++)
                                {
                                    ushort id = cells[((x + 1) * m + (y + 1)) * m + (z + 1)];
                                    if (id == 0) continue;
                                    Color tint = MaterialRegistry.Get(id).MeshTint;

                                    for (int f = 0; f < 6; f++)
                                    {
                                        var d = Dir[f];
                                        if (cells[((x + d.x + 1) * m + (y + d.y + 1)) * m + (z + d.z + 1)] != 0) continue;
                                        int b = v.Count;
                                        var face = Face[f];
                                        for (int k = 0; k < 4; k++)
                                        {
                                            var p = face[k];
                                            v.Add(new Vector3((x + p.x) * step, (y + p.y) * step, (z + p.z) * step));
                                            col.Add(tint);
                                        }
                                        t.Add(b); t.Add(b + 1); t.Add(b + 2);
                                        t.Add(b); t.Add(b + 2); t.Add(b + 3);
                                    }
                                }

                                if (v.Count == 0) return null;

                                var mesh = new Mesh { indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(v);
                mesh.SetColors(col);
                mesh.SetTriangles(t, 0, false);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
}
