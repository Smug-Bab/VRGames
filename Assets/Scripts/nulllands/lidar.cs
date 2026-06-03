using UnityEngine;
using UnityEngine.InputSystem;

public class lidar : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private InputAction fireAction;
    [SerializeField] private int raycastCount = 32;
    [SerializeField] private float raycastSpreadScale = 1f;
    
    [Header("Mark Settings")]
    [SerializeField] private Color markColor = Color.red;
    [SerializeField] private float markSize = 0.1f;
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Cooldown")]
    [SerializeField] private float cooldownSeconds = 5f;
    private float lastFireTime = -Mathf.Infinity;

    private Camera mainCamera;
    private float raycastDistance;
    // Combined marks
    private GameObject marksParent;
    private Mesh combinedMesh;
    private Material markMaterial;
    private System.Collections.Generic.List<Vector3> verts;
    private System.Collections.Generic.List<int> tris;
    private System.Collections.Generic.List<Vector2> uvs;
    private System.Collections.Generic.List<Color> colors;

    void OnEnable()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        fireAction.Enable();
        fireAction.performed += ShootRandomRaycasts;
        EnsureMarksParent();
    }

    void OnDisable()
    {
        fireAction.performed -= ShootRandomRaycasts;
        fireAction.Disable();
    }

    void ShootRandomRaycasts(InputAction.CallbackContext context)
    {
        if (Time.time < lastFireTime + cooldownSeconds)
        {
            return;
        }

        lastFireTime = Time.time;

        // play a one-shot sound when shooting begins
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(sfxSource.clip);
        }
        // Update raycast distance from camera's far clip plane
        if (mainCamera != null)
        {
            raycastDistance = mainCamera.farClipPlane;
        }

        float raycastSpread = mainCamera.fieldOfView * raycastSpreadScale;

        for (int i = 0; i < raycastCount; i++)
        {
            // Generate uniformly distributed direction on a sphere
            float theta = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float phi = Random.Range(0f, raycastSpread) * Mathf.Deg2Rad;
            
            float x = Mathf.Sin(phi) * Mathf.Cos(theta);
            float y = Mathf.Sin(phi) * Mathf.Sin(theta);
            float z = Mathf.Cos(phi);
            
            Vector3 randomDirection = new Vector3(x, y, z);
            randomDirection = Quaternion.LookRotation(transform.forward) * randomDirection;
            
            // Cast ray
            if (Physics.Raycast(transform.position, randomDirection, out RaycastHit hit, raycastDistance))
            {
                MarkHitPoint(hit.point, hit.normal);
            }
        }
    }

    void MarkHitPoint(Vector3 hitPoint, Vector3 hitNormal)
    {
        // Add quad to combined mesh so all marks are one GameObject
        if (combinedMesh == null) EnsureMarksParent();
        AddQuad(hitPoint + hitNormal * 0.001f, hitNormal, markSize);
    }

    void EnsureMarksParent()
    {
        if (marksParent != null)
        {
            if (marksParent.transform.parent != null) marksParent.transform.SetParent(null);
            return;
        }
        marksParent = new GameObject("RaycastMarks");
        // place marks container at world root so marks remain in world space
        marksParent.transform.SetParent(null);
        marksParent.transform.position = Vector3.zero;
        marksParent.transform.rotation = Quaternion.identity;
        marksParent.transform.localScale = Vector3.one;

        combinedMesh = new Mesh();
        combinedMesh.name = "CombinedRaycastMarks";

        var mf = marksParent.AddComponent<MeshFilter>();
        mf.sharedMesh = combinedMesh;

        var mr = marksParent.AddComponent<MeshRenderer>();
        markMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        markMaterial.color = markColor;
        markMaterial.EnableKeyword("_VERTEX_COLOR");
        mr.sharedMaterial = markMaterial;

        verts = new System.Collections.Generic.List<Vector3>();
        tris = new System.Collections.Generic.List<int>();
        uvs = new System.Collections.Generic.List<Vector2>();
        colors = new System.Collections.Generic.List<Color>();
    }

    void AddQuad(Vector3 center, Vector3 normal, float size)
    {
        // orientation: quad +Z faces opposite the surface normal
        Quaternion rot = Quaternion.LookRotation(-normal);
        Vector3 up = rot * Vector3.up;
        Vector3 right = rot * Vector3.right;
        float half = size * 0.5f;

        Vector3 v0 = center + (-right + up) * half; // top-left
        Vector3 v1 = center + (right + up) * half;  // top-right
        Vector3 v2 = center + (right - up) * half;  // bottom-right
        Vector3 v3 = center + (-right - up) * half; // bottom-left

        int i = verts.Count;
        verts.Add(v0);
        verts.Add(v1);
        verts.Add(v2);
        verts.Add(v3);

        tris.Add(i + 0);
        tris.Add(i + 1);
        tris.Add(i + 2);
        tris.Add(i + 2);
        tris.Add(i + 3);
        tris.Add(i + 0);

        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(0, 0));

        colors.Add(markColor);
        colors.Add(markColor);
        colors.Add(markColor);
        colors.Add(markColor);

        combinedMesh.Clear();
        combinedMesh.SetVertices(verts);
        combinedMesh.SetTriangles(tris, 0);
        combinedMesh.SetUVs(0, uvs);
        combinedMesh.SetColors(colors);
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();
        combinedMesh.UploadMeshData(false);
    }
}
