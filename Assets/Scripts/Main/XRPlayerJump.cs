using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class XRPlayerJump : MonoBehaviour
{
    public Rigidbody rb;
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 7f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayDistance = 1.1f; // Adjust based on your object's pivot height

    [Header("Input Setup")]
    [SerializeField] private InputAction jumpAction;

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
        bool hit = Physics.Raycast(transform.position, Vector3.down, rayDistance, groundLayer);
        if (hit)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }
    }
}