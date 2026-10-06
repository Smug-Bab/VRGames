using UnityEngine;
using UnityEngine.InputSystem;

public class VoxelPickup : MonoBehaviour
{
    [Header("Hands")]
    public Transform leftHandTransform;
    public Transform rightHandTransform;

    [Header("Input Actions")]
    [Tooltip("Left-hand pick button. Press to grab, release to drop.")]
    public InputAction leftPickAction;
    [Tooltip("Right-hand pick button. Press to grab, release to drop.")]
    public InputAction rightPickAction;

    [Header("References")]
    public VoxelWorldManager worldManager;
    public VoxelRegistry registry;
    public float maxPickDistance = 0.35f;

    private GameObject heldObject;
    private Rigidbody heldRb;
    private Transform activeHand;
    private Vector3 previousHandPosition;

    private void OnEnable()
    {
        if (leftPickAction != null)
        {
            leftPickAction.performed += OnLeftPickPerformed;
            leftPickAction.canceled += OnLeftPickCanceled;
            leftPickAction.Enable();
        }

        if (rightPickAction != null)
        {
            rightPickAction.performed += OnRightPickPerformed;
            rightPickAction.canceled += OnRightPickCanceled;
            rightPickAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (leftPickAction != null)
        {
            leftPickAction.performed -= OnLeftPickPerformed;
            leftPickAction.canceled -= OnLeftPickCanceled;
            leftPickAction.Disable();
        }

        if (rightPickAction != null)
        {
            rightPickAction.performed -= OnRightPickPerformed;
            rightPickAction.canceled -= OnRightPickCanceled;
            rightPickAction.Disable();
        }
    }

    private void OnLeftPickPerformed(InputAction.CallbackContext ctx)
    {
        TryPick(leftHandTransform);
    }

    private void OnLeftPickCanceled(InputAction.CallbackContext ctx)
    {
        ReleaseHeldObject(leftHandTransform);
    }

    private void OnRightPickPerformed(InputAction.CallbackContext ctx)
    {
        TryPick(rightHandTransform);
    }

    private void OnRightPickCanceled(InputAction.CallbackContext ctx)
    {
        ReleaseHeldObject(rightHandTransform);
    }

    private void TryPick(Transform hand)
    {
        if (hand == null || heldObject != null || worldManager == null || registry == null)
            return;

        Ray ray = new Ray(hand.position, hand.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxPickDistance * 2f))
            return;

        Vector3 pickPoint = hit.point - hit.normal * 0.5f;
        int gx = Mathf.FloorToInt(pickPoint.x);
        int gy = Mathf.FloorToInt(pickPoint.y);
        int gz = Mathf.FloorToInt(pickPoint.z);

        ushort blockID = worldManager.GetBlockAtGlobal(gx, gy, gz, out bool isLoaded);
        if (!isLoaded || blockID == 0)
            return;

        VoxelBlockDefinition blockDef = registry.GetBlock(blockID);
        GameObject voxelGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        voxelGo.transform.position = new Vector3(gx + 0.5f, gy + 0.5f, gz + 0.5f);
        voxelGo.transform.localScale = Vector3.one;
        voxelGo.transform.SetParent(hand);

        heldRb = voxelGo.GetComponent<Rigidbody>();
        if (heldRb == null)
        {
            heldRb = voxelGo.AddComponent<Rigidbody>();
            heldRb.mass = 1f;
            heldRb.useGravity = false;
            heldRb.isKinematic = true;
        }
        else
        {
            heldRb.useGravity = false;
            heldRb.isKinematic = true;
        }

        var renderer = voxelGo.GetComponent<Renderer>();
        if (renderer != null && blockDef != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = blockDef.CalculatedBlockColor;
            renderer.material = mat;
        }

        heldObject = voxelGo;
        activeHand = hand;
        previousHandPosition = hand.position;

        worldManager.SetBlockAtGlobal(gx, gy, gz, 0);
    }

    private void ReleaseHeldObject(Transform hand)
    {
        if (heldObject == null || heldRb == null || hand == null || hand != activeHand)
            return;

        heldObject.transform.SetParent(null);
        heldRb.isKinematic = false;
        heldRb.useGravity = true;

        Vector3 handVelocity = (hand.position - previousHandPosition) / Time.fixedDeltaTime;
        heldRb.linearVelocity = handVelocity;

        heldObject = null;
        heldRb = null;
        activeHand = null;
    }
}
