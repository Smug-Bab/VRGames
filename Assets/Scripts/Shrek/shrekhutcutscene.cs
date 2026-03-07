using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder.Shapes;

public class shrekhutcutscene : MonoBehaviour
{
    [SerializeField] SphereCollider trigger;
    [SerializeField] private GameObject[] puppets;
    [SerializeField] private AudioClip[] lines;

    private void OnTriggerEnter(Collider other)
    {
        trigger.enabled = false;
        StartCoroutine(CutsceneHut());
    }

    IEnumerator CutsceneHut()
    {
        puppets[0].GetComponent<AudioSource>().PlayOneShot(lines[0]);
        yield return new WaitForSeconds(6f);
        puppets[0].GetComponent<AudioSource>().PlayOneShot(lines[1]);
        float timeCount = 0;
        for (int i = 0; i < 5; i++)
        {
            timeCount += Time.deltaTime;
            puppets[2].transform.rotation = Quaternion.Lerp(puppets[2].transform.rotation, puppets[2].transform.rotation * Quaternion.Euler(0, 110, 0), timeCount * 2f);
            yield return new WaitForSeconds(0.1f);
        }
        puppets[2].GetComponent<AudioSource>().PlayOneShot(lines[2]);
        puppets[0].GetComponent<AudioSource>().spatialBlend = 0.8f;
        yield return new WaitForSeconds(2f);
        puppets[1].GetComponent<Rigidbody>().isKinematic = false;
        puppets[1].GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0, -3000, 2499.75f));
        yield return new WaitForSeconds(5f);
        puppets[0].GetComponent<NavMeshAgent>().SetDestination(puppets[1].transform.position);
        puppets[1].GetComponent<Light>().enabled = false;
        puppets[4].SetActive(true);
        puppets[4].transform.GetChild(3).parent = null;
        puppets[1].GetComponent<Rigidbody>().isKinematic = true;
        yield return new WaitUntil(() => (puppets[0].GetComponent<NavMeshAgent>().velocity.magnitude < 0.01));
        puppets[0].GetComponent<Animator>().Play("shrekcrush", 0);

        puppets[3].transform.position = puppets[1].transform.position;
        puppets[1].transform.GetChild(0).GetChild(0).GetChild(0).transform.localScale = Vector3.zero;
        puppets[3].SetActive(true);
    }
}