using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class KnucklesGlider : MonoBehaviour
{
    [Header("Input Actions (Triggers)")]
    [SerializeField] private InputAction leftTrigger;
    [SerializeField] private InputAction rightTrigger;

    [Header("Hand Anchors")]
    [SerializeField] private Transform leftHandAnchor;
    [SerializeField] private Transform rightHandAnchor;

    [Header("Gliding Settings")]
    [SerializeField] private float glideForwardForce = 25f;
    [SerializeField] private float glideDescentVelocity = -1.5f;
    [SerializeField] private float descentBrakingForce = 10f;

    [Header("Dynamic Glide Acceleration")]
    [SerializeField] private float maxGlideMultiplier = 2.5f;
    [SerializeField] private float timeToMaxGlideSpeed = 4.0f;

    // Read by XRPlayerMovement
    public bool isOverridingMovement = false;

    private Rigidbody rb;
    private bool isLeftHolding = false;
    private bool isRightHolding = false;
    private float glideTimer = 0f;

    public bool IsGliding { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        leftTrigger.performed += OnLeftTriggerPerformed;
        leftTrigger.canceled += OnLeftTriggerCanceled;
        rightTrigger.performed += OnRightTriggerPerformed;
        rightTrigger.canceled += OnRightTriggerCanceled;

        leftTrigger.Enable();
        rightTrigger.Enable();
    }

    private void OnDisable()
    {
        leftTrigger.performed -= OnLeftTriggerPerformed;
        leftTrigger.canceled -= OnLeftTriggerCanceled;
        rightTrigger.performed -= OnRightTriggerPerformed;
        rightTrigger.canceled -= OnRightTriggerCanceled;

        leftTrigger.Disable();
        rightTrigger.Disable();
        isOverridingMovement = false;
    }

    private void OnLeftTriggerPerformed(InputAction.CallbackContext ctx) { isLeftHolding = true; }
    private void OnLeftTriggerCanceled(InputAction.CallbackContext ctx) { isLeftHolding = false; }
    private void OnRightTriggerPerformed(InputAction.CallbackContext ctx) { isRightHolding = true; }
    private void OnRightTriggerCanceled(InputAction.CallbackContext ctx) { isRightHolding = false; }

    public void ManualFixedUpdate(XRPlayerMovement.MovementState state)
    {
        // --- HOLD TRIGGER INPUT OVERRIDE ---
        // Overrides whenever both actions are actively clamped down
        if (isLeftHolding && isRightHolding)
        {
            isOverridingMovement = true;

            // Gliding movement should only calculate velocity changes while airborne
            if (!state.isGrounded)
            {
                IsGliding = true;
            }
            else
            {
                IsGliding = false;
                glideTimer = 0f;
            }
        }
        else
        {
            isOverridingMovement = false;
            IsGliding = false;
            glideTimer = 0f;
            return;
        }

        if (!IsGliding) return;

        // Process Gliding Force Calculations
        glideTimer += Time.fixedDeltaTime;
        glideTimer = Mathf.Min(glideTimer, timeToMaxGlideSpeed);

        float progressPct = glideTimer / timeToMaxGlideSpeed;
        float currentGlideMultiplier = Mathf.Lerp(1.0f, maxGlideMultiplier, progressPct);
        float dynamicGlideForce = glideForwardForce * currentGlideMultiplier;

        if (state.localVelocity.y < glideDescentVelocity)
        {
            float brakeEffort = (glideDescentVelocity - state.localVelocity.y) * descentBrakingForce;
            rb.AddForce(transform.up * brakeEffort, ForceMode.Acceleration);
        }

        Vector3 combinedForward = transform.forward;
        if (leftHandAnchor != null && rightHandAnchor != null)
        {
            combinedForward = (leftHandAnchor.forward + rightHandAnchor.forward).normalized;
        }

        Vector3 projectGlideDirection = Vector3.ProjectOnPlane(combinedForward, transform.up).normalized;

        if (projectGlideDirection.magnitude > 0.01f)
        {
            rb.AddForce(projectGlideDirection * dynamicGlideForce, ForceMode.Acceleration);
        }
    }
}
