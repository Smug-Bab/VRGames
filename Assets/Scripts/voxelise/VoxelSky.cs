using UnityEngine;

public class VoxelSky : MonoBehaviour
{

    [Tooltip("Gradient describing sky color through the day. 0 = start, 1 = end.")]
    public Gradient skyGradient = new Gradient();

    [Tooltip("Length of a full day cycle in seconds. If <= 0, time is static.")]
    public float dayLengthSeconds = 120f;

    [SerializeField] private float elapsed = 0f;

    [Tooltip("Automatically advance time")] 
    public bool autoAdvance = true;

    [Tooltip("If true, the cycle loops after reaching the end.")]
    public bool loop = true;

    private Material SkyMaterial;

    private void OnEnable()
    {
        ResolveSkyMaterial();
        UpdateSkyColor();
    }

    private void Update()
    {
        if (Application.isPlaying && autoAdvance && dayLengthSeconds > 0f)
        {
            elapsed += Time.deltaTime;

            if (loop)
                elapsed %= dayLengthSeconds;
            else
                elapsed = Mathf.Min(elapsed, dayLengthSeconds);

            UpdateSkyColor();
        }
        else
        {
            // In edit mode or when not auto-advancing, make sure manual edits show immediately
            UpdateSkyColor();
        }
    }

    private void ResolveSkyMaterial()
    {
        Renderer parentRenderer = transform.parent != null ? transform.parent.GetComponent<Renderer>() : null;
        Renderer ownRenderer = GetComponent<Renderer>();

        // Avoid calling .material in edit mode to prevent Unity from creating an instantiated material
        // (which leaks instances into the scene). Use sharedMaterial when not playing; use instance
        // material when playing so runtime changes don't edit the asset.
        if (Application.isPlaying)
        {
            if (parentRenderer != null)
                SkyMaterial = parentRenderer.material;
            else if (ownRenderer != null)
                SkyMaterial = ownRenderer.material;
            else
                SkyMaterial = null;
        }
        else
        {
            if (parentRenderer != null)
                SkyMaterial = parentRenderer.sharedMaterial;
            else if (ownRenderer != null)
                SkyMaterial = ownRenderer.sharedMaterial;
            else
                SkyMaterial = null;
        }
    }

    private float GetTimeOfDay()
    {
        if (dayLengthSeconds <= 0f)
            return 0f;

        return Mathf.Clamp01(elapsed / dayLengthSeconds);
    }

    private void UpdateSkyColor()
    {
        if (SkyMaterial == null)
        {
            // Try resolving again in case the parent/renderer was assigned later
            ResolveSkyMaterial();
            if (SkyMaterial == null)
                return;
        }

        float timeOfDay = GetTimeOfDay();

        Color c;
        // Guard against an empty/default Gradient which evaluates to black
        if (skyGradient == null || skyGradient.colorKeys == null || skyGradient.colorKeys.Length == 0)
        {
            c = Color.white;
        }
        else
        {
            c = skyGradient.Evaluate(Mathf.Clamp01(timeOfDay));
        }

        if (c == Color.black)
        {
            Debug.LogWarning($"VoxelSky: evaluated color is black at time={timeOfDay}. Material=" + (SkyMaterial != null ? SkyMaterial.name : "null"));
        }

        // Try common property names to be compatible with different shaders
        if (SkyMaterial.HasProperty("_Color"))
            SkyMaterial.SetColor("_Color", c);
        if (SkyMaterial.HasProperty("_BaseColor"))
            SkyMaterial.SetColor("_BaseColor", c);
        if (SkyMaterial.HasProperty("_TintColor"))
            SkyMaterial.SetColor("_TintColor", c);

        // Also update main color as a fallback
        SkyMaterial.color = c;
    }

    private void OnValidate()
    {
        elapsed = Mathf.Clamp(elapsed, 0f, Mathf.Max(dayLengthSeconds, 0.0001f));
        UpdateSkyColor();
    }
}
