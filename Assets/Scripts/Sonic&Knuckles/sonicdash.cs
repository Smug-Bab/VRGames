using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class sonicdash : MonoBehaviour
{
    [SerializeField] InputAction dashAction;
    [SerializeField] XRPlayerMovement mover;
    [SerializeField] Rigidbody rigid;
    [SerializeField] AudioSource chargeSound;
    [SerializeField] AudioSource dashSound;

    [SerializeField] private float charge;

    private void OnEnable()
    {
            dashAction.performed += OnDashPerformed;
            dashAction.canceled += OnDashCanceled;
            dashAction.Enable();
    }

    private void OnDisable()
    {
            dashAction.performed -= OnDashPerformed;
            dashAction.canceled -= OnDashCanceled;
            dashAction.Disable();
            mover.enabled = true;
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        mover.enabled = false;
        StartCoroutine(ChargeDash());
        StopCoroutine(Dash());
    }

    private void OnDashCanceled(InputAction.CallbackContext context)
    {
        mover.enabled = true;
        StartCoroutine(Dash());
        StopCoroutine(ChargeDash());
    }
    IEnumerator ChargeDash()
    {
        while (rigid.useGravity == true)
        {
            charge += charge * Time.deltaTime;
            chargeSound.pitch += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator Dash()
    {
        chargeSound.pitch = 0.1f;
        mover.moveSpeed = charge;
        dashSound.PlayOneShot(dashSound.clip);
        yield return null;
    }
}
