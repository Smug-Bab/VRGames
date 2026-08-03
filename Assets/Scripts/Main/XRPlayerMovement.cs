using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class XRPlayerMovement : MonoBehaviour
{
    [System.Serializable]
    public struct MovementState
    {
        public bool isGrounded;
        public float horizontalInput;
        public float verticalInput;
        public float turnInput;
        public Vector3 localVelocity;
    }

    [Header("Exposed State")]
    public MovementState currentMovementState;

    [Header("Debug Live Vector Views")]
    [Tooltip("Read-only view of the Rigidbody's current absolute world velocity.")]
    public Vector3 debugWorldVelocity;
    [Tooltip("Read-only view of the current horizontal speed magnitude (MPH/Units scale).")]
    public float currentSpeedMagnitude;

    [Header("Sub-Movement Overrides")]
    [Tooltip("Drop any movement modules (SonicMod, KnucklesGlider, SonicDash, etc.) into this list.")]
    [SerializeField] private List<MonoBehaviour> subMovementScripts = new List<MonoBehaviour>();

    [Header("Movement Settings")]
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float maxSpeed = 25f;
    [SerializeField] private float friction = 5f;
    [SerializeField] private float turnSpeed = 100f;

    [Header("Dynamic Acceleration Settings")]
    [SerializeField] private float maxAccelerationMultiplier = 2.0f;
    [SerializeField] private float timeToMaxAcceleration = 3.0f;

    [Header("Direction Orientation Anchor")]
    [SerializeField] private Transform directionAnchor;

    [Header("Ground/Air Settings")]
    [Range(0f, 1f)] [SerializeField] private float airControlMultiplier = 0.2f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    [Header("Input Configuration")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction turnAction;

    private Rigidbody rb;
    private float inputTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (directionAnchor == null && Camera.main != null)
        {
            directionAnchor = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        turnAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        turnAction.Disable();
    }

    private void Update()
    {
        Vector2 inputVector = moveAction.ReadValue<Vector2>();
        currentMovementState.horizontalInput = inputVector.x;
        currentMovementState.verticalInput = inputVector.y;

        Vector2 turnVector = turnAction.ReadValue<Vector2>();
        currentMovementState.turnInput = turnVector.x;
    }

    private void FixedUpdate()
    {
        // Update debug trackers in the inspector window
        if (rb != null)
        {
            debugWorldVelocity = rb.linearVelocity;
            currentSpeedMagnitude = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        }

        if (groundCheck != null)
        {
            float checkRadius = !rb.useGravity ? groundDistance * 1.5f : groundDistance;
            currentMovementState.isGrounded = Physics.CheckSphere(groundCheck.position, checkRadius, groundMask);
        }
        else
        {
            currentMovementState.isGrounded = true;
        }

        if (Mathf.Abs(currentMovementState.turnInput) > 0.01f)
        {
            float rotationAmount = currentMovementState.turnInput * turnSpeed * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.AngleAxis(rotationAmount, transform.up);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }

        bool overrideActive = false;

        foreach (MonoBehaviour script in subMovementScripts)
        {
            if (script == null || !script.enabled) continue;

            MethodInfo method = script.GetType().GetMethod("ManualFixedUpdate");

            if (method != null)
            {
                method.Invoke(script, new object[] { currentMovementState });

                FieldInfo overrideField = script.GetType().GetField("isOverridingMovement");
                if (overrideField != null)
                {
                    bool isModuleOverriding = (bool)overrideField.GetValue(script);
                    if (isModuleOverriding)
                    {
                        overrideActive = true;
                    }
                }
            }
        }

        if (!overrideActive)
        {
            MoveLocallyWithForces();
        }
    }

    private void MoveLocallyWithForces()
    {
        currentMovementState.localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

        float currentFriction = currentMovementState.isGrounded || !rb.useGravity ? friction : friction * airControlMultiplier;

        bool isInputtingDirection = (Mathf.Abs(currentMovementState.horizontalInput) > 0.01f || Mathf.Abs(currentMovementState.verticalInput) > 0.01f);
        if (isInputtingDirection)
        {
            inputTimer += Time.fixedDeltaTime;
            inputTimer = Mathf.Min(inputTimer, timeToMaxAcceleration);
        }
        else
        {
            inputTimer = 0f;
        }

        float progressPct = inputTimer / timeToMaxAcceleration;
        float dynamicMultiplier = Mathf.Lerp(1.0f, maxAccelerationMultiplier, progressPct);
        float baseAcc = currentMovementState.isGrounded ? acceleration : acceleration * airControlMultiplier;
        float finalAcceleration = baseAcc * dynamicMultiplier;

        Vector3 forceDirection = Vector3.zero;
        Vector3 movementForward = transform.forward;
        Vector3 movementRight = transform.right;

        if (directionAnchor != null)
        {
            movementForward = Vector3.ProjectOnPlane(directionAnchor.forward, transform.up).normalized;
            movementRight = Vector3.ProjectOnPlane(directionAnchor.right, transform.up).normalized;
        }

        if (Mathf.Abs(currentMovementState.horizontalInput) > 0.01f)
        {
            forceDirection += movementRight * currentMovementState.horizontalInput;
        }
        if (Mathf.Abs(currentMovementState.verticalInput) > 0.01f)
        {
            forceDirection += movementForward * currentMovementState.verticalInput;
        }

        if (forceDirection.magnitude > 0.01f)
        {
            float inputMagnitude = Mathf.Clamp01(new Vector2(currentMovementState.horizontalInput, currentMovementState.verticalInput).magnitude);
            rb.AddForce(forceDirection.normalized * finalAcceleration * inputMagnitude, ForceMode.Acceleration);
        }
        else if (currentMovementState.isGrounded || !rb.useGravity)
        {
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizontalVel.magnitude > 0.01f)
            {
                rb.AddForce(-horizontalVel * currentFriction, ForceMode.Acceleration);
            }
        }

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            Vector3 clampedHorizontal = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(clampedHorizontal.x, rb.linearVelocity.y, clampedHorizontal.z);
        }
    }
}
