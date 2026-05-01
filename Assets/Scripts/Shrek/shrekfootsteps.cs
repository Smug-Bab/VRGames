using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine;

public class ShrekFootstep : MonoBehaviour
{
    [SerializeField] MovementScript mover;
    [SerializeField] InputActionReference joy;
    [SerializeField] AudioSource audio;
    [SerializeField] CharacterController chara;
    [SerializeField] AudioClip[] clips;
    private void OnEnable()
    {
        joy.action.Enable();
        StartCoroutine(Footsteps());
    }
    private void OnDisable()
    {
        joy.action.Disable();
        StopCoroutine(Footsteps());

    }
    IEnumerator Footsteps()
    {
        do
        {
            var speed = joy.action.ReadValue<Vector2>();
            if (speed != Vector2.zero)
            {
                if (Physics.Raycast(chara.transform.position + new Vector3(0, (float)0.2, 0), Vector3.down, out RaycastHit hit, 1))
                {
                    switch (hit.collider.tag)
                    {
                        case "mats/dirty":
                            audio.PlayOneShot(clips[0]);
                            break;
                        case "mats/hard":
                            audio.PlayOneShot(clips[1]);
                            break;
                        default:
                            audio.PlayOneShot(clips[0]);
                            break;
                    }
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
        while (joy.action.enabled);
    }
}
