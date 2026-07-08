using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class XRPlayerTurn : MonoBehaviour
{
    public Rigidbody rb;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private InputAction rotationAction;

    private float rotationInput;
    private void OnEnable()
    {
        rotationAction.Enable();
        rotationAction.performed += ctx => rotationInput = ctx.ReadValue<Vector2>().x;
        rotationAction.canceled += ctx => rotationInput = 0f;
    }

    private void OnDisable()
    {
        rotationAction.Disable();
    }

    private void FixedUpdate()
    {
        float turnAngle = rotationInput * rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAngle, 0f));
    }
}