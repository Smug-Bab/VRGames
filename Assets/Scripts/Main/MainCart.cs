using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MainCart : MonoBehaviour
{
    [Header("Line Setup")]
    private Transform objectA;
    public Transform objectB;

    [Header("Line Thickness Settings")]
    [Tooltip("The thickness of the line when the objects are touching (Distance = 0).")]
    public float maxThickness = 0.5f;
    [Tooltip("The thickness of the line when the distance reaches or exceeds the threshold.")]
    public float minThickness = 0.05f;

    [Header("Threshold Settings")]
    [Tooltip("The scene will load when the distance between A and B is GREATER than or EQUAL to this number while grabbed.")]
    public float distanceThreshold = 5.0f;

    [Header("Scene Management")]
    public string sceneToLoad;

    private LineRenderer lineRenderer;
    private XRInteractable interactable;
    private bool sceneIsLoading = false;

    void Start()
    {
        objectA = this.transform;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        // Get the interaction script attached to this cart
        interactable = GetComponent<XRInteractable>();
    }

    void Update()
    {
        if (objectA == null || objectB == null) return;

        // 1. Calculate the current distance
        float currentDistance = Vector3.Distance(objectA.position, objectB.position);

        // 2. Dynamically adjust line thickness based on distance
        AdjustLineThickness(currentDistance);

        // Always update the line renderer positions so they can see the tension
        lineRenderer.SetPosition(0, objectA.position);
        lineRenderer.SetPosition(1, objectB.position);

        // CRITICAL CHECK: Only calculate distance logic if the player is actively grabbing it
        if (interactable != null && interactable.IsGrabbed)
        {
            // If they pull it far enough, BOOM—load the scene
            if (currentDistance >= distanceThreshold && !sceneIsLoading)
            {
                LoadTargetScene();
            }
        }
    }

    private void AdjustLineThickness(float distance)
    {
        // Normalize the distance value into a 0 to 1 range based on your threshold
        float t = Mathf.Clamp01(distance / distanceThreshold);

        // Interpolate between max thickness (at 0 distance) and min thickness (at threshold distance)
        // Using (1 - t) because we want it to get THINNER as distance increases
        lineRenderer.widthMultiplier = Mathf.Lerp(maxThickness, minThickness, t);
    }

    private void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            sceneIsLoading = true;
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("Distance threshold met while pulling, but 'Scene To Load' is empty!", this);
        }
    }
}
