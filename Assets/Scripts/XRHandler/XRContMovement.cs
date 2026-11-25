using UnityEngine;
using UnityEngine.InputSystem;

public class XRLocomotion : MonoBehaviour
{
    [SerializeField] private Transform headsetCamera;
    [SerializeField] private Rigidbody rigid;

    [SerializeField] private InputActionProperty moveAction;

    [SerializeField] private float moveSpeed = 3.0f;

    private Vector2 movementInput;

    private void OnEnable()
    {
        // Subscribe to the Input Action events
        moveAction.action.performed += OnMovePerformed;
        moveAction.action.canceled += OnMoveCanceled;

        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks when disabled/destroyed
        moveAction.action.performed -= OnMovePerformed;
        moveAction.action.canceled -= OnMoveCanceled;

        moveAction.action.Disable();
    }

    // Called automatically only when the joystick changes position
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    // Called automatically only when the joystick snaps back to zero
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        movementInput = Vector2.zero;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {

        if (movementInput.sqrMagnitude > 0.01f)
        {
            // Flatten directions based on headset look orientation
            Vector3 forward = headsetCamera.forward;
            Vector3 right = headsetCamera.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * movementInput.y) + (right * movementInput.x);

            // Apply velocity while retaining existing gravity/vertical forces
            Vector3 targetVelocity = moveDirection * moveSpeed;
            targetVelocity.y = rigid.linearVelocity.y;

            rigid.linearVelocity = targetVelocity;
        }
        else
        {
            // Instantly halt horizontal slide when stick is untouched
            rigid.linearVelocity = new Vector3(0f, rigid.linearVelocity.y, 0f);
        }
    }
}
