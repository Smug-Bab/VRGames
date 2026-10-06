using UnityEngine;

public static class VoxelCaveCarver
{
    // Fast pseudo-3D noise calculated from 2D Perlin slices to keep CPU overhead low on Quest 3
    public static bool IsCave(int globalX, int globalY, int globalZ, VoxelNoiseSettings noiseSettings, int seed)
    {
        if (globalY < noiseSettings.minCaveHeight || globalY > noiseSettings.maxCaveHeight)
        {
            return false;
        }

        float scale = noiseSettings.caveFrequency;
        
        // Sample overlapping 2D noise planes to emulate a 3D cave network
        float xy = Mathf.PerlinNoise((globalX + seed) * scale, (globalY + seed) * scale);
        float xz = Mathf.PerlinNoise((globalX + seed * 2) * scale, (globalZ + seed * 2) * scale);
        float yz = Mathf.PerlinNoise((globalY + seed * 3) * scale, (globalZ + seed * 3) * scale);

        float density = (xy + xz + yz) / 3.0f;

        return density > noiseSettings.caveThreshold;
    }
}