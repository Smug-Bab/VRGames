using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class shrekai : MonoBehaviour
{
    [SerializeField] Animator anim;
    [SerializeField] NavMeshAgent nav;
    [SerializeField] AudioSource audio;
    [SerializeField] AudioClip[] clips;
    private float steptime = 0;
    private void FixedUpdate()
    {
        anim.speed = nav.velocity.magnitude / 16;
        anim.SetFloat("ARMS", nav.velocity.magnitude / 12);
        anim.SetFloat("LEGS", nav.velocity.magnitude / 12);
    }
    IEnumerator FootstepPush()
    {
        if (Physics.Raycast(nav.transform.position + new Vector3(0, (float)0.2, 0), Vector3.down, out RaycastHit hit, 1))
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
        yield return null;
    }
}
