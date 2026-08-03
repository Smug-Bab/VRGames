using UnityEngine;

public class XRPlayerHeight : MonoBehaviour
{
    [SerializeField] private CapsuleCollider capsuleCollider;
    [SerializeField] private Transform cameraTransform;

    [Header("Height Settings")]
    [Tooltip("Prevents the collider from flattening to 0 if the headset is placed on the floor.")]
    [SerializeField] private float minHeight = 0.5f;

    void FixedUpdate()
    {
        if (capsuleCollider == null || cameraTransform == null) return;

        // 1. Get the local Y position of the camera and clamp it to a minimum height
        float targetHeight = cameraTransform.localPosition.y;
        capsuleCollider.height = Mathf.Max(targetHeight, minHeight);

        // 2. Adjust the center of the capsule so its base remains on the floor (local Y = 0)
        Vector3 newCenter = capsuleCollider.center;
        newCenter.y = capsuleCollider.height / 2f;

        capsuleCollider.center = newCenter;
    }
}
