using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SonicDash : MonoBehaviour
{
    [Header("Spindash Settings")]
    [SerializeField] private float chargeRate = 60f;
    [SerializeField] private float maxDashForce = 160f;
    [SerializeField] private float minDashForce = 25f;
    [SerializeField] private float decayRate = 40f;

    [Header("Input Configuration")]
    [SerializeField] private InputAction dashAction;

    // Read by XRPlayerMovement
    public bool isOverridingMovement = false;

    private Rigidbody rb;
    private float currentDashForce = 0f;
    private bool isCharging = false;
    private bool shouldLaunch = false;
    private bool isRolling = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        dashAction.Enable();
        dashAction.canceled += OnDashReleased;
    }

    private void OnDisable()
    {
        dashAction.Disable();
        dashAction.canceled -= OnDashReleased;
        isOverridingMovement = false;
    }

    private void Update()
    {
        isCharging = dashAction.IsPressed();

        if (isCharging)
        {
            currentDashForce += chargeRate * Time.deltaTime;
            currentDashForce = Mathf.Min(currentDashForce, maxDashForce);
        }
        else
        {
            currentDashForce = Mathf.MoveTowards(currentDashForce, 0f, decayRate * Time.deltaTime);
        }
    }

    private void OnDashReleased(InputAction.CallbackContext context)
    {
        if (currentDashForce >= minDashForce)
        {
            shouldLaunch = true;
        }
    }

    public void ManualFixedUpdate(XRPlayerMovement.MovementState state)
    {
        bool activelyHolding = dashAction != null && dashAction.IsPressed();

        // If rolling, check if we should cancel out because momentum stopped or we jumped
        if (isRolling)
        {
            float currentMomentum = rb.linearVelocity.magnitude;

            if (currentMomentum < 0.5f || !state.isGrounded)
            {
                isRolling = false;
            }
        }

        // --- EXPLICIT ROLLING STATE OVERRIDE ---
        if (activelyHolding || shouldLaunch || isRolling)
        {
            isOverridingMovement = true;
        }
        else
        {
            isOverridingMovement = false;
        }

        if (shouldLaunch)
        {
            shouldLaunch = false;

            if (state.isGrounded)
            {
                isRolling = true; // Enter rolling lock state
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(transform.forward * currentDashForce, ForceMode.VelocityChange);
            }

            currentDashForce = 0f;
        }
    }

    public float GetCurrentCharge() => currentDashForce;
}
