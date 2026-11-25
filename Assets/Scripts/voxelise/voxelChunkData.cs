using UnityEngine;

public class Chunk
{
    public const int S = 16;
    public readonly Vector3Int Coord;
    public readonly ushort[] Ids = new ushort[S * S * S];
    public readonly float[] Temp = new float[S * S * S];
    public readonly float[] Charge = new float[S * S * S];   // optional electricity
    public bool Dirty = true;

    public Chunk(Vector3Int c) { Coord = c; }
    public static int Idx(int x, int y, int z) => (x * S + y) * S + z;

    const float FlowPerFace = 0.15f;   // 6 faces * 0.15 = 0.9, so it stays stable
    const float AirCoolPerFace = 0.02f;
    const float Ambient = 288f;

    static readonly float[] scratchT = new float[S * S * S];
    static readonly float[] scratchC = new float[S * S * S];
    static readonly int[] DX = { 1, -1, 0, 0, 0, 0 };
    static readonly int[] DY = { 0, 0, 1, -1, 0, 0 };
    static readonly int[] DZ = { 0, 0, 0, 0, 1, -1 };

    // world = lookup for ids across chunk borders (used only to detect air)
    public void Step(float dt, bool electricity, System.Func<int, int, int, ushort> world)
    {
        System.Array.Copy(Temp, scratchT, Temp.Length);
        if (electricity) System.Array.Copy(Charge, scratchC, Charge.Length);
        int ox = Coord.x * S, oy = Coord.y * S, oz = Coord.z * S;

        for (int x = 0; x < S; x++)
            for (int y = 0; y < S; y++)
                for (int z = 0; z < S; z++)
                {
                    int i = Idx(x, y, z);
                    var a = MaterialRegistry.Get(Ids[i]);
                    if (a == null) continue;

                    float dT = 0, dC = 0; int airFaces = 0;
                    for (int f = 0; f < 6; f++)
                    {
                        int nx = x + DX[f], ny = y + DY[f], nz = z + DZ[f];
                        ushort nid;
                        bool inside = nx >= 0 && ny >= 0 && nz >= 0 && nx < S && ny < S && nz < S;
                        nid = inside ? Ids[Idx(nx, ny, nz)] : world(ox + nx, oy + ny, oz + nz);

                        var b = MaterialRegistry.Get(nid);
                        if (b == null) { airFaces++; continue; }
                        if (!inside) continue;                      // no cross-chunk flow
                        int j = Idx(nx, ny, nz);
                        float k = Mathf.Min(a.Conductivity, b.Conductivity) * FlowPerFace;
                        dT += (Temp[j] - Temp[i]) * k;
                        if (electricity) dC += (Charge[j] - Charge[i]) * k;
                    }

                    float t = Temp[i] + dT;
                    t -= (t - Ambient) * AirCoolPerFace * airFaces;       // air cooling
                    t += a.HeatPerSec * dt / a.Mass;                      // radioactive heating
                    scratchT[i] = t;
                    if (electricity) scratchC[i] = Charge[i] + dC;

                    // failure / decomposition
                    if (t > a.BreakTemp || (a.DecayPerSec > 0 && Random.value < a.DecayPerSec * dt))
                    { Ids[i] = 0; Dirty = true; }
                }

                System.Array.Copy(scratchT, Temp, Temp.Length);
                if (electricity) System.Array.Copy(scratchC, Charge, Charge.Length);
    }
}
