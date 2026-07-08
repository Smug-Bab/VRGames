using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class sanctumnJDAI : MonoBehaviour
{
[Header("References")]
[SerializeField] GameObject JD;
[SerializeField] GameObject player;
[SerializeField] NavMeshAgent nav;
[SerializeField] float navmaxspeed = 8f;
[SerializeField] Animator anim;
[SerializeField] SphereCollider rad;
[SerializeField] GameObject InfectPartSys;
[SerializeField] JDPartData PartData;
[SerializeField] AudioSource source;
[SerializeField] AudioClip[] clips;
private float fogDefDistance;
private float fogLerpSpeed = 5f;
private float currentFogEnd;

void Start()
{
fogDefDistance = RenderSettings.fogEndDistance;
currentFogEnd = RenderSettings.fogEndDistance;
}
void Update()
{
if (player != null)
{nav.SetDestination(player.transform.position);}
}

void OnTriggerEnter(Collider other)
{
if (other.gameObject != player)
return;
JD.SetActive(true);
InfectPartSys.SetActive(true);
player.GetComponent<AudioSource>().Stop();
	}

	void OnTriggerStay(Collider other)
	{
if (other.gameObject != player)
return;

float dist = Vector3.Distance(other.transform.position, transform.position);
anim.SetFloat("speed", nav.velocity.magnitude/navmaxspeed);
anim.speed = nav.velocity.magnitude/4f;


InfectPartSys.transform.position = other.transform.position;
currentFogEnd = dist;
RenderSettings.fogEndDistance = Mathf.Lerp(
RenderSettings.fogEndDistance,
currentFogEnd,
Time.deltaTime * fogLerpSpeed);


ParticleSystem ps = PartData.JDPartSys;

var main = ps.main;
main.startLifetime = dist;

var shape = ps.shape;
shape.radius = dist * 2f;

var vel = ps.velocityOverLifetime;
vel.orbitalX = dist;
vel.orbitalY = dist;

var noise = ps.noise;
noise.strength = dist;


if (dist > rad.radius/1.3f)
{
nav.speed = navmaxspeed/6;
PlayClip(0);
}
else if (dist > rad.radius/2.3f)
{
nav.speed = navmaxspeed/4;
PlayClip(1);
}
else if (dist > rad.radius/3.3f)
{
nav.speed = navmaxspeed/2;
PlayClip(2);
}
else if (dist > rad.radius/4.3f)
{
nav.speed = navmaxspeed;
PlayClip(3);
}
	
}

void OnTriggerExit(Collider other)
{
if (other.gameObject != player)
return;

JD.SetActive(false);
InfectPartSys.SetActive(false);
nav.speed = 0.5f;
currentFogEnd = fogDefDistance;

source.Stop();
player.GetComponent<AudioSource>().Play();
}


void PlayClip(int index)
{
if (source.clip != clips[index])
{
source.Stop();
source.clip = clips[index];
source.Play();
}
}

}