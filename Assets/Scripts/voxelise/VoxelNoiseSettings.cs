using UnityEngine;

[CreateAssetMenu(fileName = "VoxelNoiseSettings", menuName = "Modular Engine/Noise Settings")]
public class VoxelNoiseSettings : ScriptableObject
{
    [Header("Terrain Surface Elevation")]
    public float baseHeight = 32f;
    public float heightAmplitude = 24f;
    public float surfaceFrequency = 0.01f;
    public int octaves = 3;
    public float persistence = 0.5f;
    public float lacunarity = 2.0f;

    [Header("Cave Carver 3D Noise")]
    public float caveFrequency = 0.03f;
    [Range(0f, 1f)]
    public float caveThreshold = 0.55f; // Values above this threshold carve out air
    public float minCaveHeight = 4f;
    public float maxCaveHeight = 128f;

    public float Evaluate2DSurface(float x, float z, int seed)
    {
        float total = 0f;
        float frequency = surfaceFrequency;
        float amplitude = heightAmplitude;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x + seed) * frequency;
            float sampleZ = (z + seed) * frequency;
            
            total += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;
            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return baseHeight + (total / maxValue) * heightAmplitude;
    }
}