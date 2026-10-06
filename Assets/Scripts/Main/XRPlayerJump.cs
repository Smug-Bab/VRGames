using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class XRPlayerJump : MonoBehaviour
{
    public Rigidbody rb;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 7f;

    [Header("Ground Check")]
    [SerializeField] private float rayDistance = 1.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Input Setup")]
    [SerializeField] private InputAction jumpAction;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Default");
    }

    private void OnEnable()
    {
        jumpAction.Enable();
        jumpAction.started += OnJumpStarted;
    }

    private void OnDisable()
    {
        jumpAction.started -= OnJumpStarted;
        jumpAction.Disable();
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        // FIX: Cast the ray relative to the player's current local orientation (-transform.up)
        // This allows jumping off walls/slopes smoothly while remaining decoupled.
        bool hit = Physics.Raycast(transform.position, -transform.up, rayDistance, groundLayer);
        if (hit)
        {
            // Apply upward force relative to the player's local orientation vector
            rb.linearVelocity = rb.linearVelocity + (transform.up * jumpForce);
        }
    }
}
