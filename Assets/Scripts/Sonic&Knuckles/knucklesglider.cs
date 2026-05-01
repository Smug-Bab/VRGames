using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class knucklesglider : MonoBehaviour
{
    [SerializeField] private InputAction grip;
    [SerializeField] private MonoBehaviour[] scripts;
    [SerializeField] private Rigidbody rigid;
    [SerializeField] private int palm = 0;
    [SerializeField] private XRPlayerMovement PlayerSpeed;
    [SerializeField] private float glideForce = 1f;

    private void Awake()
    {
            grip.performed += OnGripPressed;
            grip.Enable();
            grip.canceled += OnGripReleased;
    }

    private void OnDestroy()
    {
            grip.performed -= OnGripPressed;
            grip.Disable();
            grip.canceled -= OnGripReleased;
    }

    private void OnGripPressed(InputAction.CallbackContext context)
    {
        ++palm;
        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = !script.enabled;
        }
        if (palm >= 2)
        {
            rigid.useGravity = false;
            StartCoroutine(Glide());
        }
    }
        private void OnGripReleased(InputAction.CallbackContext context)
    {
        --palm;
        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = !script.enabled;
        }
        if (palm < 1)
        {
            rigid.useGravity = true;
            StopCoroutine(Glide());
        }
    }
    private IEnumerator Glide()
    {
        while (palm >= 2)
        {
            glideForce += PlayerSpeed.mult + 1f;
            rigid.AddForce(transform.forward * glideForce);
            yield return null;
        }
    }
}
