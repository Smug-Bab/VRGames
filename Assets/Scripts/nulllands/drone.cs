using UnityEngine;
using UnityEngine.InputSystem;

public class drone : MonoBehaviour
{
    [Header("Movement")]
    public float maxForwardSpeed = 5f; // units per second
    public float turnSpeed = 90f;      // degrees per second for full input

    [Header("Audio")]
    [SerializeField] private AudioSource engineAudio;
    [SerializeField] private float minEngineVolume = 0.05f;
    [SerializeField] private float maxEngineVolume = 1f;
    [SerializeField] private float engineVolumeSmooth = 5f; // higher = snappier

    [Header("Input (Input System)")]
    public InputAction forwardAction; // positive = forward, negative = backward
    public InputAction rotateAction;  // positive = rotate right, negative = rotate left

    void OnEnable()
    {
        forwardAction?.Enable();
        rotateAction?.Enable();
    }

    void OnDisable()
    {
        forwardAction?.Disable();
        rotateAction?.Disable();
    }

    void Update()
    {
        float forward = ReadAxis(forwardAction, Axis.Forward);
        float rotate = ReadAxis(rotateAction, Axis.Rotate);

        // Kinematic movement: forward moves forward, left/right rotate the player
        transform.position += transform.forward * forward * maxForwardSpeed * Time.deltaTime;
        transform.Rotate(0f, rotate * turnSpeed * Time.deltaTime, 0f, Space.Self);
    }

    // Simpler: update audio volume in FixedUpdate based on input magnitude
    void FixedUpdate()
    {
        if (engineAudio == null) return;
        float forward = ReadAxis(forwardAction, Axis.Forward);
        float speedRatio = Mathf.Clamp01(Mathf.Abs(forward)); // 0..1 relative input
        engineAudio.volume = Mathf.Lerp(minEngineVolume, maxEngineVolume, speedRatio);
        if (!engineAudio.isPlaying) engineAudio.Play();
    }

    enum Axis { Forward, Rotate }

    float ReadAxis(InputAction action, Axis axis)
    {
        if (action != null)
        {
            try
            {
                return action.ReadValue<float>();
            }
            catch
            {
                // fall through to keyboard fallback
            }
        }

        // Keyboard fallback: forward/back = W/S or Up/Down, rotate = A/D
        var kb = Keyboard.current;
        if (kb == null) return 0f;

        if (axis == Axis.Forward)
        {
            bool fwd = kb.wKey.isPressed || kb.upArrowKey.isPressed;
            bool back = kb.sKey.isPressed || kb.downArrowKey.isPressed;
            if (fwd && !back) return 1f;
            if (back && !fwd) return -1f;
            return 0f;
        }
        else // Rotate
        {
            bool left = kb.aKey.isPressed;
            bool right = kb.dKey.isPressed;
            if (left && !right) return -1f;
            if (right && !left) return 1f;
            return 0f;
        }
    }
}
