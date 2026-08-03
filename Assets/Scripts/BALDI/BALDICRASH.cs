using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class BALDICRASH : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform playerTransform; // Drag your Player GameObject here
    public Camera playerCamera;       // Drag your Player Camera GameObject here

    [Header("Movement Settings")]
    public float speed = 5f; // Adjust the chase speed
    
    private bool hasBeenSeen = false;
    private Renderer myRenderer;

    void Start()
    {
        myRenderer = GetComponent<Renderer>();

        // Fallback for Camera if you forget to drag it in
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Fallback for Player if you forget to drag it in
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    void Update()
    {
        // If it hasn't been seen yet, check if the camera is looking at it
        if (!hasBeenSeen)
        {
            CheckIfVisible();
        }

        // Once triggered, chase!
        if (hasBeenSeen && playerTransform != null)
        {
            ChasePlayer();
        }
    }

    void CheckIfVisible()
    {
        if (playerCamera == null) return;

        // Method 1: Check Unity's built-in renderer visibility
        if (myRenderer != null && myRenderer.isVisible)
        {
            // Simple Raycast line-of-sight check to ensure a wall isn't blocking it
            RaycastHit hit;
            Vector3 directionToObj = transform.position - playerCamera.transform.position;
            
            if (Physics.Raycast(playerCamera.transform.position, directionToObj, out hit))
            {
                if (hit.transform == this.transform)
                {
                    hasBeenSeen = true;
                    Debug.Log("BALDI SEES YOU!");
                }
            }
        }
    }

    void ChasePlayer()
    {
        // Look at the player (locks the Y axis so it doesn't tilt)
        Vector3 targetPosition = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        transform.LookAt(targetPosition);

        // Move towards the player using transform logic
        transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (playerTransform != null && other.transform == playerTransform)
        {
            SceneManager.LoadScene("main");
        }
    }
}