using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine;

public class XRInteractable : MonoBehaviour
{
    [SerializeField] bool isGrabbable = false;
    [SerializeField] InputAction selectAction;
    [SerializeField] UnityEvent scriptActions;

    public bool IsGrabbed { get; private set; } = false;

    private Rigidbody rb;
    private Transform activeHandTransform;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        // SMART CHECK: See if the object entering has our Hand component
        XRHand hand = other.GetComponent<XRHand>();

        if (hand != null)
        {
            // Now you know exactly which hand it is!
            Debug.Log($"Hand detected: {hand.Handedness}");

            // Store this specific hand's transform to attach to later
            activeHandTransform = other.transform;

            if (isGrabbable)
            {
                // Prevent event stacking bugs
                selectAction.performed -= Grab;
                selectAction.canceled -= Release;

                selectAction.Enable();
                selectAction.performed += Grab;
                selectAction.canceled += Release;
            }
            else
            {
                Touch();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Ensure the object leaving is the hand we are currently tracking
        if (activeHandTransform != null && other.transform == activeHandTransform)
        {
            if (isGrabbable && !IsGrabbed)
            {
                CleanUpInput();
                activeHandTransform = null;
            }
        }
    }

    private void Grab(InputAction.CallbackContext context)
    {
        if (activeHandTransform == null) return;

        IsGrabbed = true;

        // Smart Physics: Make it kinematic so it follows the hand perfectly
        // without disabling the Rigidbody component entirely
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Parent it to the specific hand that triggered the collision
        transform.SetParent(activeHandTransform);

        scriptActions.Invoke();
    }

    private void Release(InputAction.CallbackContext context)
    {
        if (!IsGrabbed) return;

        IsGrabbed = false;
        transform.SetParent(null);

        // Return physics control back to the physics engine
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        CleanUpInput();
        activeHandTransform = null;
    }

    private void Touch() {
        scriptActions.Invoke();
    }

    private void CleanUpInput()
    {
        selectAction.performed -= Grab;
        selectAction.canceled -= Release;
        selectAction.Disable();
    }

    private void OnDisable()
    {
        CleanUpInput();
    }
}
