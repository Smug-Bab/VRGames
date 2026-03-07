using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;

public class sanctumnJDAI : MonoBehaviour
{
    //Initialise
    public float fogDefDistance;
    [SerializeField] GameObject JD;
    [SerializeField] NavMeshAgent nav;
    [SerializeField] Animator anim;
    [SerializeField] SphereCollider rad;
    [SerializeField] GameObject InfectPartSys;
    [SerializeField] JDPartData PartData;
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip[] clips;
    [SerializeField] GameObject player;
    // Init
    private void FixedUpdate() 
    {
        nav.SetDestination(player.transform.position);
        JD.transform.eulerAngles = new Vector3(JD.transform.eulerAngles.x, JD.transform.eulerAngles.y, nav.speed * 3);
        JD.transform.GetChild(0).LookAt(player.transform);
    }

    //Particle Control
    void OnTriggerEnter(Collider other)
    {
        JD.SetActive(true);
        InfectPartSys.SetActive(true);
        source.Play();
        player.GetComponent<AudioSource>().Stop();
    }
    void OnTriggerExit(Collider other)
    {
        JD.SetActive(false);
        nav.speed = 0.5f;
        anim.speed = 0.2f;
        InfectPartSys.SetActive(false);
        RenderSettings.fogEndDistance = fogDefDistance;
        source.Stop();
        player.GetComponent<AudioSource>().Play();
    }



    void OnTriggerStay(Collider other)
    {
        // Fog
        InfectPartSys.transform.position = other.transform.position;
        RenderSettings.fogEndDistance = Vector3.Distance (other.transform.position, this.transform.position);

        // Particles
        PartData.JDPartSysMain.startLifetime = (int)Vector3.Distance (-other.transform.position, this.transform.position) / 64;
        PartData.JDPartSysShape.radius = (int)Vector3.Distance (other.transform.position, this.transform.position) * 2;
        PartData.JDPartSysVel.orbitalX = Vector3.Distance (-other.transform.position, this.transform.position)/ 256;
        PartData.JDPartSysVel.orbitalY = Vector3.Distance (-other.transform.position, this.transform.position)/ 64;
        PartData.JDPartSysNoise.strength = (int)Vector3.Distance (-other.transform.position, this.transform.position)/ 64;

        //Music
        int dist = Mathf.FloorToInt(Vector3.Distance (other.transform.position, this.transform.position));
        switch (dist)
        {
        case 100:
        nav.speed = 1f;
        anim.speed = 0.4f;
        source.Stop();
        source.clip = clips[0];
        source.Play();
            break;
        case 75:
        nav.speed = 2f;
        anim.speed = 0.7f;
        source.Stop();
        source.clip = clips[1];
        source.Play();
            break;
        case 45:
        nav.speed = 3f;
        anim.speed = 0.9f;
        source.Stop();
        source.clip = clips[2];
        source.Play();
            break;
        case 20:
        nav.speed = 4f;
        anim.speed = 1.2f;
        source.Stop();
        source.clip = clips[3];
        source.Play();
            break;
        default:
            break;
        }
    }
}
