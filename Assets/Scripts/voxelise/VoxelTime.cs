using UnityEngine;

public class VoxelTimeOfDayController : MonoBehaviour
{
    [Header("Movement Tracking")]
    [Tooltip("The GameObject this controller should follow (e.g., the Player)")]
    public Transform targetToFollow;
    [Tooltip("Keep checked if you want this object to stay glued to the target's position")]
    public bool followTarget = true;

    [Header("Materials & Speed")]
    public Material voxelMaterial;
    public Material skyMaterial; // The material using the Vertex Color node
    public float rotationSpeed = 0.01f;
    [Tooltip("Controls how quickly the sun transitions between day and night cycles")]
    public float transitionSharpness = 0.5f;

    [Header("Color Settings")]
    [Tooltip("Left (0.0) = Day Blue, Middle (0.5) = Sunset Orange, Right (1.0) = Night Black")]
    public Gradient skyAndFogGradient;

    internal float currentBrightness;

    private void Update()
    {
        // --- 0. Position Following Logic ---
        if (followTarget && targetToFollow != null)
        {
            transform.position = targetToFollow.position;
        }

        // --- 1. Get our raw wave with a 90-degree phase shift (+ PI / 2) ---
        // This forces Mathf.Sin to evaluate to 1.0 at Time = 0, starting the game at Peak Day!
        float timeStep = Time.time * (rotationSpeed * 0.05f);
        float rawValue = Mathf.Sin(timeStep + (Mathf.PI * 0.5f));

        // --- 2. Shape the wave to flatten out/linger at the extremes (-1 and 1) ---
        float shapedValue = Mathf.Clamp(rawValue / transitionSharpness, -1f, 1f);

        // --- 3. Map directly to Gradient Timeline ---
        // shapedValue of  1.0 (Day)    -> evaluation point 0.0 (Blue)
        // shapedValue of  0.0 (Sunset) -> evaluation point 0.5 (Orange)
        // shapedValue of -1.0 (Night)  -> evaluation point 1.0 (Black)
        float gradientEvaluationPoint = (1f - shapedValue) * 0.5f;

        // --- 4. Tie block brightness to the day/night state ---
        float blockBrightness = Mathf.Clamp01(1f - gradientEvaluationPoint);
        currentBrightness = blockBrightness;

        // --- 5. Update the material properties ---
        if (voxelMaterial != null)
        {
            voxelMaterial.SetFloat("_BlockBrightness", blockBrightness);

            Vector3 allSidesEqual = new Vector3(blockBrightness, blockBrightness, blockBrightness);
            voxelMaterial.SetVector("_CustomLightDir", allSidesEqual);
        }

        // --- 6. Sample the gradient correctly ---
        Color currentSkyColor = Color.white;
        if (skyAndFogGradient != null)
        {
            currentSkyColor = skyAndFogGradient.Evaluate(gradientEvaluationPoint);
        }
        else
        {
            currentSkyColor = Color.Lerp(Color.cyan, Color.black, gradientEvaluationPoint);
        }

        // --- 7. Apply sampled color ---
        if (skyMaterial != null)
        {
            skyMaterial.SetColor("_SkyColor", currentSkyColor);
        }
        RenderSettings.fogColor = currentSkyColor;
    }
}

// --- Inspector Read-Only Display Logic ---
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(VoxelTimeOfDayController))]
public class VoxelTimeOfDayControllerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        VoxelTimeOfDayController script = (VoxelTimeOfDayController)target;

        UnityEditor.EditorGUI.BeginDisabledGroup(true);
        UnityEditor.EditorGUILayout.FloatField("Current Light Level (Read Only)", script.currentBrightness);
        UnityEditor.EditorGUI.EndDisabledGroup();

        if (Application.isPlaying) Repaint();
    }
}
#endif
