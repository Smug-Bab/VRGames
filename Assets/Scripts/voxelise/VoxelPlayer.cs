using UnityEngine;
using System.Collections;

public class VoxelPlayer : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the world manager to check block data before moving")]
    public VoxelWorldManager worldManager;

    [Header("Voxel World Settings")]
    [Tooltip("The width/length of a single chunk (usually 16)")]
    public int chunkSize = 16;

    [Tooltip("The maximum height the raycast will start from to look for the surface")]
    public float maxSurfaceHeight = 256f;

    [Tooltip("Layer mask for your voxel/ground objects so raycasts only hit the world")]
    public LayerMask groundLayer;

    [Header("Debug Info")]
    [SerializeField] private Vector2Int currentChunkCoord;

    private void Start()
    {
        // Start the routine that waits for physical colliders to bake
        StartCoroutine(WaitForWorldAndSpawn());
    }

    private void Update()
    {
        // Keep track of which chunk the player is currently standing in
        currentChunkCoord.x = Mathf.FloorToInt(transform.position.x / chunkSize);
        currentChunkCoord.y = Mathf.FloorToInt(transform.position.z / chunkSize);
    }

    /// <summary>
    /// Coroutine that pauses until VoxelWorldManager explicitly populates block data at the origin column.
    /// </summary>
    private IEnumerator WaitForWorldAndSpawn()
    {
        if (worldManager == null)
        {
            Debug.LogError("[VoxelPlayer] VoxelWorldManager reference is missing!");
            yield break;
        }

        // Wait a couple of initial frames for the manager's Awake loop to initialize registries
        yield return null;
        yield return null;

        bool groundDataReady = false;
        int retries = 0;

        // Loop and wait for the manager's natural Update sequence to generate block data at (0, y, 0)
        while (!groundDataReady && retries < 30)
        {
            // Check along a likely ground height (e.g., Y coordinate 4) to see if it's no longer air (0)
            if (worldManager.GetBlockAtGlobal(0, 4, 0) != 0)
            {
                // Data is generated! Now wait 2 frames for Unity's Mesh and Collider baking to catch up
                yield return new WaitForSeconds(0.1f);
                yield return new WaitForFixedUpdate();
                groundDataReady = true;
                break;
            }

            retries++;
            yield return null; // Wait for the next frame
        }

        // Safe to execute raycast placement at starting origin (0,0)
        SnapToSurfaceWithRaycast(0f, 0f);
    }

    /// <summary>
    /// Teleports the player exactly 1 chunk away in a random direction and handles the raycast snap.
    /// </summary>
    public void TeleportToNearbyChunkSurface()
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // East
            new Vector2Int(-1, 0),  // West
            new Vector2Int(0, 1),   // North
            new Vector2Int(0, -1)   // South
        };
        Vector2Int chosenDirection = directions[Random.Range(0, directions.Length)];

        float targetX = transform.position.x + (chosenDirection.x * chunkSize);
        float targetZ = transform.position.z + (chosenDirection.y * chunkSize);

        SnapToSurfaceWithRaycast(targetX, targetZ);
    }

    /// <summary>
    /// Fires a physics raycast downward from the sky ceiling and positions the player 5 units above the hit point.
    /// </summary>
    private void SnapToSurfaceWithRaycast(float targetX, float targetZ)
    {
        Vector3 rayStart = new Vector3(targetX, maxSurfaceHeight, targetZ);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, maxSurfaceHeight + 50f, groundLayer))
        {
            Vector3 targetPosition = new Vector3(targetX, hit.point.y + 5.0f, targetZ);
            transform.position = targetPosition;
            Debug.Log($"[VoxelPlayer] Success! Raycast hit ground at Y: {hit.point.y}. Spawned at: {targetPosition}");
        }
        else
        {
            // Hard dynamic backup: if the physics layer missed, fall back to raw data heights so you NEVER drop below 0!
            int dataSurfaceY = 64;
            for (int y = (int)maxSurfaceHeight; y >= 0; y--)
            {
                if (worldManager.GetBlockAtGlobal((int)targetX, y, (int)targetZ) != 0)
                {
                    dataSurfaceY = y;
                    break;
                }
            }

            Vector3 fallbackPosition = new Vector3(targetX, dataSurfaceY + 5.0f, targetZ);
            transform.position = fallbackPosition;
            Debug.LogWarning($"[VoxelPlayer] Raycast missed colliders, but data scanning saved you! Placed 5 units above raw data height at Y: {fallbackPosition.y}");
        }
    }
}
