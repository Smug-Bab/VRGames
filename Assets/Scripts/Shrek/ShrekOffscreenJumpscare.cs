using UnityEngine;
using System.Collections;

public class ShrekOffscreenJumpscare : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform boneToRotate;
    public SkinnedMeshRenderer targetSkinnedMeshRenderer;

    [Tooltip("Fine-tune the rotation axes directly relative to Shrek's body.")]
    public Vector3 localRotationOffset = new Vector3(180f, 25f, 0f);

    [Tooltip("How many seconds the head stays tracking before snapping back to normal.")]
    public float resetDelaySeconds = 1.5f;

    private Camera mainCam;
    private bool wasVisibleLastFrame;
    private bool isJumpscareActive;
    private Quaternion originalLocalRotation;

    void Start()
    {
        if (!cameraTransform) cameraTransform = Camera.main.transform;
        mainCam = cameraTransform.GetComponent<Camera>();
        originalLocalRotation = boneToRotate.localRotation;
    }

    void Update()
    {
        if (!mainCam || !targetSkinnedMeshRenderer || isJumpscareActive) return;

        Vector3 vp = mainCam.WorldToViewportPoint(targetSkinnedMeshRenderer.bounds.center);
        bool isVisible = (vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f && vp.z > 0f);

        if (wasVisibleLastFrame && !isVisible)
        {
            isJumpscareActive = true;
            cameraTransform.rotation = Quaternion.LookRotation((cameraTransform.position - targetSkinnedMeshRenderer.bounds.center).normalized);
        }
        wasVisibleLastFrame = isVisible;
    }

    void LateUpdate()
    {
        if (!isJumpscareActive || !boneToRotate) return;

        Transform parent = boneToRotate.parent != null ? boneToRotate.parent : transform;
        Vector3 localTargetDir = parent.InverseTransformDirection(cameraTransform.position - boneToRotate.position).normalized;

        if (localTargetDir != Vector3.zero)
        {
            Quaternion localLook = Quaternion.LookRotation(localTargetDir, Vector3.up);
            boneToRotate.localRotation = localLook * Quaternion.Euler(localRotationOffset);
        }

        if (wasVisibleLastFrame)
        {
            StartCoroutine(ResetHead());
        }
    }

    private IEnumerator ResetHead()
    {
        // Waits for your variable time amount set in the inspector
        yield return new WaitForSeconds(resetDelaySeconds);

        boneToRotate.localRotation = originalLocalRotation;
        isJumpscareActive = false;
    }
}
