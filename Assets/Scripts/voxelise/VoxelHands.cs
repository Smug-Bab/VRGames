using UnityEngine;
using UnityEngine.InputSystem;

public class VoxelHands : MonoBehaviour
{
    [Header("Dependencies")]
    public VoxelWorldManager worldManager;
    public VoxelRegistry voxelRegistry;

    [Header("Input Actions")]
    public InputAction leftHandGrabAction;
    public InputAction rightHandGrabAction;

    [Header("Hand Tracking Transforms")]
    public Transform leftHandTransform;
    public Transform rightHandTransform;

    [Header("Public Trigger Colliders")]
    public Collider leftHandCollider;
    public Collider rightHandCollider;
    public LayerMask voxelLayer;

    [Header("Pull Settings")]
    public float pullThreshold = 0.4f;

    private ushort airID;
    private bool isLeftHandHolding = false;
    private bool isRightHandHolding = false;

    // Core states
    private GameObject carriedBlock = null;
    private Rigidbody carriedRb = null;

    // Pull mechanic states
    private bool isTargetingBlock = false;
    private Vector3 targetedBlockWorldPos;
    private Vector3 targetedBlockGridPos;
    private ushort targetedBlockID;
    private Material sourceMaterial;

    private readonly Collider[] overlapResults = new Collider[8];

    private void Start()
    {
        if (voxelRegistry != null)
        {
            airID = voxelRegistry.GetBlockID(voxelRegistry.GetBlock(0));
        }
        else
        {
            Debug.LogError("VoxelRegistry is missing on VoxelHands!");
        }
    }

    private void OnEnable()
    {
        leftHandGrabAction.Enable();
        rightHandGrabAction.Enable();

        leftHandGrabAction.started += OnLeftGrabStarted;
        rightHandGrabAction.started += OnRightGrabStarted;
        leftHandGrabAction.canceled += OnLeftGrabCanceled;
        rightHandGrabAction.canceled += OnRightGrabCanceled;
    }

    private void OnDisable()
    {
        leftHandGrabAction.started -= OnLeftGrabStarted;
        rightHandGrabAction.started -= OnRightGrabStarted;
        leftHandGrabAction.canceled -= OnLeftGrabCanceled;
        rightHandGrabAction.canceled -= OnRightGrabCanceled;

        leftHandGrabAction.Disable();
        rightHandGrabAction.Disable();
    }

    private void FixedUpdate()
    {
        if (leftHandTransform == null || rightHandTransform == null) return;

        Vector3 midPoint = (leftHandTransform.position + rightHandTransform.position) * 0.5f;

        // State 1: Pulling a block
        if (isTargetingBlock && carriedBlock == null)
        {
            float currentPullDistance = Vector3.Distance(midPoint, targetedBlockWorldPos);

            if (currentPullDistance >= pullThreshold)
            {
                worldManager.SetBlockAtGlobal((int)targetedBlockGridPos.x, (int)targetedBlockGridPos.y, (int)targetedBlockGridPos.z, airID);

                VoxelBlockDefinition blockDef = voxelRegistry.GetBlock(targetedBlockID);
                SpawnDynamicMimicBlock(targetedBlockWorldPos, targetedBlockID, blockDef, sourceMaterial);

                isTargetingBlock = false;
            }
        }
        // State 2: Carrying the block
        else if (carriedBlock != null && carriedRb != null)
        {
            Vector3 targetPos = midPoint;
            Vector3 moveDirection = targetPos - carriedRb.position;
            float distance = moveDirection.magnitude;

            float speedMultiplier = Mathf.Lerp(5f, 25f, distance);
            carriedRb.linearVelocity = moveDirection.normalized * (distance * speedMultiplier);
        }
    }

    private void OnHandPress(bool isLeft, bool isPressed)
    {
        if (isLeft) isLeftHandHolding = isPressed;
        else isRightHandHolding = isPressed;

        if (isLeftHandHolding && isRightHandHolding && carriedBlock == null && !isTargetingBlock)
        {
            TryLockTargetBlock();
        }
    }

    private void OnHandRelease(bool isLeft)
    {
        if (isLeft) isLeftHandHolding = false;
        else isRightHandHolding = false;

        if (!isLeftHandHolding || !isRightHandHolding)
        {
            isTargetingBlock = false;
            DropCarriedBlock();
        }
    }

    private void TryLockTargetBlock()
    {
        if (worldManager == null || leftHandTransform == null || rightHandTransform == null) return;
        if (leftHandCollider == null || rightHandCollider == null) return;

        Vector3 midPoint = (leftHandTransform.position + rightHandTransform.position) * 0.5f;

        Bounds combinedBounds = leftHandCollider.bounds;
        combinedBounds.Encapsulate(rightHandCollider.bounds);

        int hitCount = Physics.OverlapBoxNonAlloc(
            combinedBounds.center,
            combinedBounds.extents,
            overlapResults,
            leftHandCollider.transform.rotation,
            voxelLayer
        );

        if (hitCount == 0) return;

        Collider closestCollider = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            float dist = Vector3.Distance(midPoint, overlapResults[i].transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestCollider = overlapResults[i];
            }
        }

        if (closestCollider == null) return;

        Vector3 touchPoint = closestCollider.ClosestPoint(midPoint);
        Vector3 inwardDirection = (closestCollider.bounds.center - touchPoint).normalized;
        Vector3 blockPosition = touchPoint + (inwardDirection * 0.1f);

        int globalX = Mathf.FloorToInt(blockPosition.x);
        int globalY = Mathf.FloorToInt(blockPosition.y);
        int globalZ = Mathf.FloorToInt(blockPosition.z);

        ushort blockID = worldManager.GetBlockAtGlobal(globalX, globalY, globalZ);
        if (blockID == 0 || blockID == airID) return;

        MeshRenderer targetChunkRenderer = closestCollider.GetComponent<MeshRenderer>();
        if (targetChunkRenderer == null) return;

        isTargetingBlock = true;
        targetedBlockGridPos = new Vector3(globalX, globalY, globalZ);
        targetedBlockWorldPos = new Vector3(globalX + 0.5f, globalY + 0.5f, globalZ + 0.5f);
        targetedBlockID = blockID;
        sourceMaterial = targetChunkRenderer.sharedMaterial;
    }

    private void SpawnDynamicMimicBlock(Vector3 spawnWorldPos, ushort blockID, VoxelBlockDefinition blockDef, Material worldMat)
    {
        carriedBlock = new GameObject("Dynamic_Voxel_Mimic");
        carriedBlock.transform.position = spawnWorldPos;

        // 75% smaller means it keeps 25% of its scale while held
        carriedBlock.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

        MeshFilter filter = carriedBlock.AddComponent<MeshFilter>();
        Mesh generatedMesh = GenerateSingleCubeMesh(blockDef.blockColor);
        filter.sharedMesh = generatedMesh;

        MeshRenderer renderer = carriedBlock.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = worldMat;

        MeshCollider collider = carriedBlock.AddComponent<MeshCollider>();
        collider.sharedMesh = filter.sharedMesh;
        collider.convex = true;

        carriedRb = carriedBlock.AddComponent<Rigidbody>();
        carriedRb.mass = 1.0f;
        carriedRb.useGravity = true;
        carriedRb.linearDamping = 1f;
        carriedRb.angularDamping = 1f;

        DynamicVoxel voxelData = carriedBlock.AddComponent<DynamicVoxel>();
        voxelData.Initialize(blockID, blockDef, generatedMesh, worldMat);
    }

    private Mesh GenerateSingleCubeMesh(Color32 blockColor)
    {
        Mesh mesh = new Mesh { name = "IsolatedCubeMesh" };
        Vector3[] vertices = new Vector3[24];
        Vector2[] uvs = new Vector2[24];
        Color32[] colors = new Color32[24];
        Vector3[] normals = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
        int vCount = 0;

        for (int i = 0; i < 6; i++)
        {
            Vector3 normal = normals[i];
            Vector3 side1 = new Vector3(normal.y, normal.z, normal.x);
            Vector3 side2 = Vector3.Cross(normal, side1);

            vertices[vCount + 0] = (normal - side1 - side2) * 0.5f;
            vertices[vCount + 1] = (normal - side1 + side2) * 0.5f;
            vertices[vCount + 2] = (normal + side1 + side2) * 0.5f;
            vertices[vCount + 3] = (normal + side1 - side2) * 0.5f;

            uvs[vCount + 0] = new Vector2(0, 0);
            uvs[vCount + 1] = new Vector2(0, 1);
            uvs[vCount + 2] = new Vector2(1, 1);
            uvs[vCount + 3] = new Vector2(1, 0);

            for (int c = 0; c < 4; c++) colors[vCount + c] = blockColor;
            vCount += 4;
        }

        int[] triangles = new int[36];
        int tIndex = 0;
        for (int i = 0; i < 6; i++)
        {
            int v = i * 4;
            triangles[tIndex++] = v; triangles[tIndex++] = v + 1; triangles[tIndex++] = v + 2;
            triangles[tIndex++] = v; triangles[tIndex++] = v + 2; triangles[tIndex++] = v + 3;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors32 = colors;
        mesh.RecalculateNormals();
        return mesh;
    }

    private void DropCarriedBlock()
    {
        if (carriedBlock == null) return;

        // Restore original size upon release
        carriedBlock.transform.localScale = Vector3.one;

        if (carriedRb != null)
        {
            carriedRb.AddForce(transform.forward * 1.5f, ForceMode.VelocityChange);
        }

        // Leave it in the world; clean up handled if destroyed, otherwise it can be captured by the pouch
        carriedBlock = null;
        carriedRb = null;
    }

    private void OnLeftGrabStarted(InputAction.CallbackContext ctx) => OnHandPress(true, true);
    private void OnRightGrabStarted(InputAction.CallbackContext ctx) => OnHandPress(false, true);
    private void OnLeftGrabCanceled(InputAction.CallbackContext ctx) => OnHandRelease(true);
    private void OnRightGrabCanceled(InputAction.CallbackContext ctx) => OnHandRelease(false);
}
