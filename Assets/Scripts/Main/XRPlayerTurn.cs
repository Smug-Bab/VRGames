using UnityEngine.InputSystem;
using UnityEngine;

public class XRPlayerTurn : MonoBehaviour
{
    [SerializeField] InputAction SecondaryJoy;
    [SerializeField] Rigidbody PlayerBody;
    [SerializeField] float mult = 10f;
    [SerializeField] ForceMode forceMode = ForceMode.Force;
    void OnEnable()
    {
        SecondaryJoy.Enable();
    }

    void OnDisable()
    {
        SecondaryJoy.Disable();
    }

    void FixedUpdate()
    {
        Vector2 input = SecondaryJoy.ReadValue<Vector2>();
        Vector3 movement = new Vector3(0, input.x, 0);
        PlayerBody.AddTorque(movement * mult * Time.deltaTime, forceMode);
    }
}
