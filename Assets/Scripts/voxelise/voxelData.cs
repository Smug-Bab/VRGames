using System;
using UnityEngine;
using System.IO;
using System.Runtime.CompilerServices;

public class VoxelData
{
    public readonly uint[] Voxels; 
    public readonly Vector3Int ChunkCoord;
    public readonly int ChunkSize;
    private readonly int SizeSq; 
    public bool isModified { get; set; } = false;

    public VoxelData(Vector3Int chunkCoord, int chunkSize, int chunkHeight, byte seed, float noiseScale, float heightMultiplier, bool enableCaves, float caveNoiseScale, float caveThreshold)
    {
        ChunkCoord = chunkCoord;
        ChunkSize = chunkSize;
        SizeSq = chunkSize * chunkSize;
        
        int totalVoxels = chunkSize * chunkSize * chunkSize;
        Voxels = new uint[totalVoxels >> 5];
        
        GenerateVoxelData(chunkHeight, seed, noiseScale, heightMultiplier, enableCaves, caveNoiseScale, caveThreshold);
    }

    public VoxelData(Vector3Int chunkCoord, int chunkSize, byte[] compressedData)
    {
        ChunkCoord = chunkCoord;
        ChunkSize = chunkSize;
        SizeSq = chunkSize * chunkSize;
        
        int totalVoxels = chunkSize * chunkSize * chunkSize;
        Voxels = new uint[totalVoxels >> 5];

        int bitIndex = 0;
        int totalBits = totalVoxels;
        int byteOffset = 0;

        while (byteOffset + 2 < compressedData.Length && bitIndex < totalBits)
        {
            ushort runLength = BitConverter.ToUInt16(compressedData, byteOffset);
            bool isSolid = compressedData[byteOffset + 2] != 0;
            byteOffset += 3;

            for (int i = 0; i < runLength && bitIndex < totalBits; i++)
            {
                if (isSolid)
                {
                    Voxels[bitIndex >> 5] |= (1U << (bitIndex & 31));
                }
                bitIndex++;
            }
        }
        this.isModified = true; 
    }

    private void GenerateVoxelData(int chunkHeight, byte seed, float noiseScale, float heightMultiplier, bool enableCaves, float caveNoiseScale, float caveThreshold)
    {
        int worldOffsetX = ChunkCoord.x * ChunkSize;
        int worldOffsetY = ChunkCoord.y * ChunkSize;
        int worldOffsetZ = ChunkCoord.z * ChunkSize;
        int maxSurfaceHeight = Mathf.Max(1, ThreadSafeRoundToInt(chunkHeight * heightMultiplier));

        int localSize = ChunkSize;
        int localSizeSq = SizeSq;
        uint[] localVoxels = Voxels;

        for (int y = 0; y < localSize; y++)
        {
            int yOffset = y * localSizeSq;
            int worldY = worldOffsetY + y;

            for (int z = 0; z < localSize; z++)
            {
                int zOffset = yOffset + (z * localSize);
                int worldZ = worldOffsetZ + z;

                for (int x = 0; x < localSize; x++)
                {
                    int worldX = worldOffsetX + x;

                    float noiseValue = ThreadSafeNoise.Noise2D((worldX + seed) * noiseScale, (worldZ + seed) * noiseScale);
                    int groundSurfaceY = chunkHeight + ThreadSafeRoundToInt(noiseValue * maxSurfaceHeight);

                    bool isSolid = worldY < groundSurfaceY;

                    if (isSolid && enableCaves)
                    {
                        if (Calculate3DNoise(worldX, worldY, worldZ, seed, caveNoiseScale) < caveThreshold)
                        {
                            isSolid = false;
                        }
                    }

                    if (isSolid)
                    {
                        int flatIndex = zOffset + x;
                        localVoxels[flatIndex >> 5] |= (1U << (flatIndex & 31));
                    }
                }
            }
        }
    }

    public void SetVoxel(int x, int y, int z, bool isSolid)
    {
        if (x >= 0 && x < ChunkSize && y >= 0 && y < ChunkSize && z >= 0 && z < ChunkSize)
        {
            int flatIndex = (y * SizeSq) + (z * ChunkSize) + x;
            int arrayIndex = flatIndex >> 5;
            int bitOffset = flatIndex & 31;

            bool currentStatus = (Voxels[arrayIndex] & (1U << bitOffset)) != 0;

            if (currentStatus != isSolid)
            {
                if (isSolid)
                {
                    Voxels[arrayIndex] |= (1U << bitOffset); 
                }
                else
                {
                    Voxels[arrayIndex] &= ~(1U << bitOffset); 
                }
                isModified = true; 
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVoxelSolidLocal(int x, int y, int z)
    {
        if (x < 0 || x >= ChunkSize || y < 0 || y >= ChunkSize || z < 0 || z >= ChunkSize)
            return false;

        int flatIndex = (y * SizeSq) + (z * ChunkSize) + x;
        return (Voxels[flatIndex >> 5] & (1U << (flatIndex & 31))) != 0;
    }

    public byte[] ExportCompressedBytes()
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                int totalBits = ChunkSize * ChunkSize * ChunkSize;
                bool currentType = (Voxels[0] & 1U) != 0;
                ushort runLength = 0;
                uint[] localVoxels = Voxels;

                for (int i = 0; i < totalBits; i++)
                {
                    bool type = (localVoxels[i >> 5] & (1U << (i & 31))) != 0;

                    if (type == currentType && runLength < ushort.MaxValue)
                    {
                        runLength++;
                    }
                    else
                    {
                        writer.Write(runLength);
                        writer.Write(currentType);
                        currentType = type;
                        runLength = 1;
                    }
                }
                writer.Write(runLength);
                writer.Write(currentType);
            }
            return ms.ToArray();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Calculate3DNoise(float x, float y, float z, byte seed, float scale)
    {
        float coordX = (x + seed) * scale;
        float coordY = (y + seed) * scale;
        float coordZ = (z + seed) * scale;

        return (ThreadSafeNoise.Noise2D(coordX, coordY) +
                ThreadSafeNoise.Noise2D(coordY, coordZ) +
                ThreadSafeNoise.Noise2D(coordX, coordZ) +
                ThreadSafeNoise.Noise2D(coordY, coordX) +
                ThreadSafeNoise.Noise2D(coordZ, coordY) +
                ThreadSafeNoise.Noise2D(coordZ, coordX)) * 0.16666667f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ThreadSafeRoundToInt(float value)
    {
        return (int)Math.Floor(value + 0.5f);
    }

    public static class ThreadSafeNoise
    {
        private static readonly int[] p = new int[512] {
            151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
            190, 6,148,247,120,234,75,0,26,56,62,94,252,219,203,117,35,11,32,57,177,33,88,237,149,56,87,174,20,125,136,171,168, 68,
            175,74,165,71,134,139,48,27,166,77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,102,143,54,
            65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,135,130,116,188,159,86,164,100,109,198,173,186, 3,64,
            52,217,226,250,124,123,5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,223,183,170,213,119,
            248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,
            218,246,97,228,251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,49,192,214, 31,181,199,106,
            157,184, 84,204,176,115,121,50,45,127, 4,150,254,138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
            151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
            190, 6,148,247,120,234,75,0,26,56,62,94,252,219,203,117,35,11,32,57,177,33,88,237,149,56,87,174,20,125,136,171,168, 68,
            175,74,165,71,134,139,48,27,166,77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,102,143,54,
            65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,135,130,116,188,159,86,164,100,109,198,173,186, 3,64,
            52,217,226,250,124,123,5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,223,183,170,213,119,
            248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,
            218,246,97,228,251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,49,192,214, 31,181,199,106,
            157,184, 84,204,176,115,121,50,45,127, 4,150,254,138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
        };

        public static float Noise2D(float x, float y)
        {
            int fastFloorX = x >= 0 ? (int)x : (int)x - 1;
            int fastFloorY = y >= 0 ? (int)y : (int)y - 1;

            int X = fastFloorX & 255;
            int Y = fastFloorY & 255;

            x -= fastFloorX;
            y -= fastFloorY;

            float u = x * x * x * (x * (x * 6f - 15f) + 10f);
            float v = y * y * y * (y * (y * 6f - 15f) + 10f);

            int A = p[X] + Y;
            int B = p[X + 1] + Y;

            float gradAA = Grad(p[A], x, y);
            float gradBA = Grad(p[B], x - 1, y);
            float gradAB = Grad(p[A + 1], x, y - 1);
            float gradBB = Grad(p[B + 1], x - 1, y - 1);

            float lerpX1 = gradAA + u * (gradBA - gradAA);
            float lerpX2 = gradAB + u * (gradBB - gradAB);

            return (lerpX1 + v * (lerpX2 - lerpX1)) * 0.5f + 0.5f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Grad(int hash, float x, float y)
        {
            int h = hash & 7;
            float u = h < 4 ? x : y;
            float v = h < 4 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}