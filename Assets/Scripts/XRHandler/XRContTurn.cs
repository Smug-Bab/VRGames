using UnityEngine;
using UnityEngine.InputSystem;

public class XRContTurn : MonoBehaviour
{
    public enum TurningStyle { Snap, Smooth }

    [Header("Input")]
    [SerializeField] private InputActionProperty turnAction;

    [Header("Style Settings")]
    [SerializeField] private TurningStyle turningStyle = TurningStyle.Smooth;

    [Header("Smooth Turn Settings")]
    [SerializeField] private float smoothTurnSpeed = 100f; // Degrees per second

    [Header("Snap Turn Settings")]
    [SerializeField] private float snapAngle = 45f;
    [SerializeField] [Range(0.1f, 0.9f)] private float snapThreshold = 0.5f;

    private Vector2 turnInput;
    private bool snapReady = true;

    private void OnEnable()
    {
        if (turnAction.action == null) return;

        turnAction.action.performed += OnTurnPerformed;
        turnAction.action.canceled += OnTurnCanceled;

        turnAction.action.Enable();
    }

    private void OnDisable()
    {
        if (turnAction.action == null) return;

        turnAction.action.performed -= OnTurnPerformed;
        turnAction.action.canceled -= OnTurnCanceled;

        turnAction.action.Disable();
    }

    private void OnTurnPerformed(InputAction.CallbackContext context)
    {
        turnInput = context.ReadValue<Vector2>();

        // If set to snap, try triggering it immediately upon stick movement
        if (turningStyle == TurningStyle.Snap)
        {
            ExecuteSnapTurn();
        }
    }

    private void OnTurnCanceled(InputAction.CallbackContext context)
    {
        turnInput = Vector2.zero;
        snapReady = true; // Reset gating for snap turning
    }

    private void FixedUpdate()
    {
        // Smooth turning needs to run continuously inside the physics loop
        if (turningStyle == TurningStyle.Smooth)
        {
            ExecuteSmoothTurn();
        }
    }

    private void ExecuteSmoothTurn()
    {
        // Apply deadzone check to prevent stick drift from rotating the player
        if (Mathf.Abs(turnInput.x) > 0.1f)
        {
            // Calculate continuous frame-rate independent rotation
            float rotationAmount = turnInput.x * smoothTurnSpeed * Time.fixedDeltaTime;
            transform.Rotate(Vector3.up, rotationAmount);
        }
    }

    private void ExecuteSnapTurn()
    {
        if (!snapReady) return;

        // Snap Right
        if (turnInput.x > snapThreshold)
        {
            transform.Rotate(Vector3.up, snapAngle);
            snapReady = false;
        }
        // Snap Left
        else if (turnInput.x < -snapThreshold)
        {
            transform.Rotate(Vector3.up, -snapAngle);
            snapReady = false;
        }
    }
}
