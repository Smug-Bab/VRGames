using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainWatch : MonoBehaviour
{
    // Changed from InputActionReference to a direct InputAction
    [SerializeField] private InputAction button;

    private void OnEnable()
    {
        // Direct InputActions must be explicitly enabled
        button.Enable();
        button.started += Exit;
    }

    private void OnDisable()
    {
        // Clean up components and disable the action
        button.started -= Exit;
        button.Disable();
    }

    // Capitalized method name to match standard C# conventions
    private void Exit(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene("main");
    }
}