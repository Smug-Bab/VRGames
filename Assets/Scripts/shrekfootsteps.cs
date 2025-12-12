using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine;
using System;

public class ShrekFootstep : MonoBehaviour
{
    [SerializeField] DynamicMoveProvider mover;
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
        joy.action.Enable();
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
                        case "Ground/wet":
                            audio.PlayOneShot(clips[0]);
                            break;
                        case "Ground/wood":
                            audio.PlayOneShot(clips[1]);
                            break;
                        case "Ground/metal":
                            audio.PlayOneShot(clips[2]);
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
