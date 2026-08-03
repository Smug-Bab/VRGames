using UnityEngine;

public class VoxelPlayerFogController : MonoBehaviour
{
    [Header("Camera Reference")]
    public Camera mainCamera;            

    [Header("Fog Thresholds (Absolute Units)")]
    [Tooltip("The direct distance from the camera (in blocks/meters) where the fog begins fading in.")]
    public int fogStartDistance = 20;

    [Tooltip("The direct distance from the camera (in blocks/meters) where the fog becomes 100% solid.")]
    public int fogEndDistance = 150;

    private void Start()
    {
        // If unassigned, automatically find the camera attached to the player or scene main
        if (mainCamera == null)
        {
            mainCamera = GetComponentInChildren<Camera>();
            if (mainCamera == null) mainCamera = Camera.main;
        }

        // Configure the global environment settings for rendering linear voxel fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
    }

    private void Update()
    {
        // Apply your integer distances directly to Unity's global render settings
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = fogEndDistance;
    }
}