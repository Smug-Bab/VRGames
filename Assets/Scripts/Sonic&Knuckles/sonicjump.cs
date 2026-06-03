using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class sonicjump : MonoBehaviour
{
    [SerializeField] InputAction jumpAction;
    [SerializeField] SonicIncrement smover;
    [SerializeField] Rigidbody rigid;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] AudioSource jumpSound;
    private void OnEnable()
    {
        jumpAction.performed += OnJumpPerformed;
        jumpAction.Enable();
    }
    private void OnDisable()
    {
        jumpAction.performed -= OnJumpPerformed;
        jumpAction.Disable();
        smover.enabled = true;
    }
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        StartCoroutine(Jump());
    }
    IEnumerator Jump()
    {
        if (rigid.linearDamping == 0f)
        {
        rigid.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        jumpSound.PlayOneShot(jumpSound.clip);   
        yield return new WaitForSeconds(1f);   
        }
    }
}
