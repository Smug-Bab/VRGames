using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class shrekai : MonoBehaviour
{
    [SerializeField] Animator anim;
    [SerializeField] NavMeshAgent nav;
    private void FixedUpdate()
    {
        anim.speed = nav.velocity.magnitude / 6;
        anim.SetFloat("ARMS", nav.velocity.magnitude / 12);
        anim.SetFloat("LEGS", nav.velocity.magnitude / 12);
    }
}
