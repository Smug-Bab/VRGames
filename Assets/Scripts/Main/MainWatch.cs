using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EXIT : MonoBehaviour
{
    [SerializeField] InputActionReference button;

    private void OnEnable()
    {

        button.action.started += exit;
    }

    private void OnDisable()
    {
        button.action.started -= exit;
    }

    void exit(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene("main");
    }
}
