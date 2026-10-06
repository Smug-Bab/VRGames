using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelChunk : MonoBehaviour
{
    public const int ChunkSize = 16;
    public Vector3Int ChunkCoord { get; private set; }
    public float AmbientTemperature { get; private set; }

    private ushort[] voxelData;
    private NativeArray<ushort> nativeVoxelData;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private bool isNativeSynced = false;

    public void Initialize(Vector3Int coord, float ambientTemp)
    {
        ChunkCoord = coord;
        AmbientTemperature = ambientTemp;
        voxelData = new ushort[ChunkSize * ChunkSize * ChunkSize];

        nativeVoxelData = new NativeArray<ushort>(voxelData.Length, Allocator.Persistent);
        isNativeSynced = false;

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetBlockLocal(int x, int y, int z, ushort blockID)
    {
        if (x >= 0 && x < ChunkSize && y >= 0 && y < ChunkSize && z >= 0 && z < ChunkSize)
        {
            voxelData[x | (y << 4) | (z << 8)] = blockID;
            isNativeSynced = false;
        }
    }

    public ushort GetBlockLocal(int x, int y, int z)
    {
        if (x >= 0 && x < ChunkSize && y >= 0 && y < ChunkSize && z >= 0 && z < ChunkSize)
        {
            return voxelData[x | (y << 4) | (z << 8)];
        }
        return 0;
    }

    public void SyncVoxelDataToNative()
    {
        if (!isNativeSynced && nativeVoxelData.IsCreated)
        {
            nativeVoxelData.CopyFrom(voxelData);
            isNativeSynced = true;
        }
    }

    public JobHandle ScheduleThermalJob(float deltaTime, float ambientTemp)
    {
        SyncVoxelDataToNative();

        VoxelThermalJob thermalJob = new VoxelThermalJob
        {
            voxelData = nativeVoxelData,
            deltaTime = deltaTime,
            ambientTemperature = ambientTemp
        };

        return thermalJob.Schedule(voxelData.Length, 64);
    }

    public void RebuildMesh(VoxelRegistry registry, VoxelWorldManager worldManager)
    {
        SyncVoxelDataToNative();

        VoxelMeshBuilder meshBuilder = new VoxelMeshBuilder(ChunkSize);
        Mesh mesh = meshBuilder.GenerateMesh(ChunkCoord, voxelData, registry, worldManager);

        meshFilter.sharedMesh = mesh;
    }

    private void OnDestroy()
    {
        if (nativeVoxelData.IsCreated)
        {
            nativeVoxelData.Dispose();
        }
    }
}

public struct VoxelThermalJob : IJobParallelFor
{
    public NativeArray<ushort> voxelData;
    public float deltaTime;
    public float ambientTemperature;

    public void Execute(int index)
    {
        ushort currentID = voxelData[index];
        if (currentID == 0) return;

        // Thermal simulation logic goes here safely within job constraints
    }
}
