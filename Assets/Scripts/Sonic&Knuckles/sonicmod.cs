using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SonicMod : MonoBehaviour
{
    [Header("Sonic Physics Settings")]
    [Tooltip("The downward force keeping the player glued to the loop surface.")]
    public float wallRunStickyForce = 50f;

    [Tooltip("How far forward and down the bubble reaches from your origin.")]
    public float wallClingCheckDistance = 2.5f;

    [Tooltip("The size of the predictive bubble.")]
    public float sweepRadius = 1.0f;

    [Tooltip("Select the exact Layer your loops, ramps, and slopes are assigned to.")]
    public LayerMask wallMask;

    [Header("Slope & View Reference")]
    [SerializeField] private Transform lookDirectionAnchor;
    [Tooltip("Angles below this are treated as regular flat ground. Set to 1-5 to avoid getting stuck on floors.")]
    public float minWallAngle = 1f;

    [Header("Movement Drivers")]
    [Tooltip("How much force is applied along the wall when pushing the joystick.")]
    public float wallRunAcceleration = 50f;
    [Tooltip("Multiplies the simulated downhill gravity pull. Increase this if you feel too floaty on slopes.")]
    public float gravityMultiplier = 2.5f;
    [Tooltip("How fast the capsule matches the wall alignment.")]
    public float wallAlignmentSpeed = 10f;
    [Tooltip("How fast the capsule straightens up when leaving a slope.")]
    public float uprightReturnSpeed = 12f;

    [Header("Debug Visuals")]
    public Color debugColor = Color.white;
    [Range(0f, 1f)] public float gizmoAlpha = 0.4f;

    [Header("Live Status (Read Only)")]
    public bool isOverridingMovement = false;
    public bool physicsHitDetected = false;
    public float currentSurfaceAngle = 0f;

    private Rigidbody rb;
    private bool isInputActive = false;
    private Vector3 actualSweepDirection = Vector3.forward;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (lookDirectionAnchor == null && Camera.main != null)
        {
            lookDirectionAnchor = Camera.main.transform;
        }

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void ManualFixedUpdate(XRPlayerMovement.MovementState state)
    {
        isInputActive = Mathf.Abs(state.horizontalInput) > 0.1f || Mathf.Abs(state.verticalInput) > 0.1f;

        if (!isInputActive)
        {
            isOverridingMovement = false;
            physicsHitDetected = false;
            currentSurfaceAngle = 0f;
            debugColor = Color.gray;

            // FIX: Smooth back upright if joystick is released while tilted on a surface exit
            ResetToUpright();
            return;
        }

        RaycastHit hit;
        Vector3 castOrigin = transform.position + transform.up * (sweepRadius + 0.5f);

        Vector3 bodyForward = transform.forward;
        float currentSpeed = rb.linearVelocity.magnitude;
        Vector3 sweepDir = currentSpeed > 0.5f ? rb.linearVelocity.normalized : bodyForward;

        actualSweepDirection = (sweepDir * 0.85f - transform.up * 0.5f).normalized;

        bool validSurfaceFound = false;
        Vector3 wallNormal = Vector3.zero;

        if (Physics.SphereCast(castOrigin, sweepRadius, actualSweepDirection, out hit, wallClingCheckDistance, wallMask))
        {
            if (hit.collider.transform != transform && !hit.collider.transform.IsChildOf(transform))
            {
                physicsHitDetected = true;
                currentSurfaceAngle = Vector3.Angle(hit.normal, Vector3.up);

                if (currentSurfaceAngle >= minWallAngle)
                {
                    wallNormal = hit.normal;
                    validSurfaceFound = true;
                }
                else
                {
                    debugColor = Color.yellow;
                }
            }
        }
        else
        {
            physicsHitDetected = false;
            currentSurfaceAngle = 0f;
            debugColor = Color.red;
        }

        if (validSurfaceFound)
        {
            isOverridingMovement = true;
            debugColor = Color.green;

            if (rb.useGravity)
            {
                rb.AddForce(-Physics.gravity, ForceMode.Acceleration);
            }

            rb.AddForce(-wallNormal * wallRunStickyForce, ForceMode.Acceleration);

            Vector3 gravitySlopeForce = Vector3.ProjectOnPlane(Physics.gravity, wallNormal);
            rb.AddForce(gravitySlopeForce * gravityMultiplier, ForceMode.Acceleration);

            Vector3 slopeForwardMove = Vector3.ProjectOnPlane(transform.forward, wallNormal).normalized;
            Vector3 slopeRightMove = Vector3.ProjectOnPlane(transform.right, wallNormal).normalized;

            Vector3 wallMoveDirection = (slopeForwardMove * state.verticalInput) + (slopeRightMove * state.horizontalInput);

            if (wallMoveDirection.magnitude > 0.01f)
            {
                float inputMagnitude = Mathf.Clamp01(new Vector2(state.horizontalInput, state.verticalInput).magnitude);
                rb.AddForce(wallMoveDirection.normalized * wallRunAcceleration * inputMagnitude, ForceMode.Acceleration);
            }

            Quaternion targetSurfaceRotation = Quaternion.FromToRotation(transform.up, wallNormal) * rb.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetSurfaceRotation, Time.fixedDeltaTime * wallAlignmentSpeed));
        }
        else
        {
            isOverridingMovement = false;
            // FIX: Instantly trigger structural realignment back to flat horizons whether grounded or not
            ResetToUpright();
        }
    }

    private void ResetToUpright()
    {
        // Smoothly interpolates transform.up back to the world vertical baseline (Vector3.up)
        Quaternion uprightRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, uprightRotation, Time.fixedDeltaTime * uprightReturnSpeed));
    }

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position + transform.up * (sweepRadius + 0.5f);
        Vector3 targetSphereCenter = origin + (actualSweepDirection * wallClingCheckDistance);

        Gizmos.color = new Color(debugColor.r, debugColor.g, debugColor.b, gizmoAlpha);
        Gizmos.DrawLine(origin, targetSphereCenter);
        Gizmos.DrawWireSphere(targetSphereCenter, sweepRadius);
    }
}
