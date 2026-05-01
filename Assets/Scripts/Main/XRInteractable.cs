using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine;

public class XRInteractable : MonoBehaviour
{
    [SerializeField] bool isGrabbable = false;
    [SerializeField] InputAction selectAction;
    [SerializeField] Transform attachPoint;
    [SerializeField] UnityEvent scriptActions;
    private bool isgrabbed = false;

    void Start()
    {
        if (attachPoint == null)
        {
            attachPoint = this.transform;
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {

        if (isGrabbable)
        {
            selectAction.Enable();
            selectAction.performed += Grab;
        } else
        {
            Touch();
        }

        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (isGrabbable)
            {
                selectAction.Disable();
                selectAction.performed -= Grab;
            }
        }
    }

    private void Grab(InputAction.CallbackContext context) {
        transform.SetParent(attachPoint);
        scriptActions.Invoke();
    }
    private void Touch() {
        scriptActions.Invoke();
    }
}
