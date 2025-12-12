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
        StartCoroutine(DoorOpen());
    }

    IEnumerator DoorOpen()
    {
        float timeCount = 0;
        for (int i = 0; i < 5; i++)
        {
            timeCount += Time.deltaTime;
            puppets[2].transform.rotation = Quaternion.Lerp(puppets[2].transform.rotation, puppets[2].transform.rotation * Quaternion.Euler(0, 110, 0), timeCount * 2f);
            yield return new WaitForSeconds(0.1f);
        }
        puppets[2].GetComponent<AudioSource>().PlayOneShot(lines[0]);
        puppets[1].GetComponent<Rigidbody>().isKinematic = false;
        puppets[1].GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0, 2000, 2000));
    }
}