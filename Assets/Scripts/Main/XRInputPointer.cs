using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class XRInputPointer : MonoBehaviour
{
    [Header("Input System")]
    [Tooltip("The InputAction used to trigger the pointer selection.")]
    [SerializeField] private InputAction selectAction;

    [Header("Pointer Settings")]
    [SerializeField] private float maxPointerDistance = 50f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Events")]
    [Tooltip("Fires automatically when the input action is triggered. Passes the GameObject that was hit.")]
    public UnityEvent<GameObject> OnObjectSelected;

    private LineRenderer pointerLine;

    void Awake()
    {
        // Automatically cache the required LineRenderer component
        pointerLine = GetComponent<LineRenderer>();
    }

    void OnEnable()
    {
        selectAction.Enable();
        selectAction.performed += ExecuteRaycastSelection;
    }

    void OnDisable()
    {
        selectAction.performed -= ExecuteRaycastSelection;
        selectAction.Disable();
    }

    void Update()
    {
        RenderPointerBeam();
    }

    private void RenderPointerBeam()
    {
        pointerLine.SetPosition(0, transform.position);
        Ray ray = new Ray(transform.position, transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, maxPointerDistance, targetLayers))
        {
            pointerLine.SetPosition(1, hit.point);
        }
        else
        {
            pointerLine.SetPosition(1, transform.position + (transform.forward * maxPointerDistance));
        }
    }

    private void ExecuteRaycastSelection(InputAction.CallbackContext context)
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxPointerDistance, targetLayers))
        {
            // Broadcast the hit object to any listener components or routers
            OnObjectSelected?.Invoke(hit.collider.gameObject);
        }
    }
}