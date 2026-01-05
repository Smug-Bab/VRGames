using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class shrekonioncollect : MonoBehaviour
{
    [SerializeField] AudioSource audio;
    [SerializeField] shrekjournal journal;
    private void Start()
    {
        RaycastHit hit;
        audio = this.GetComponent<AudioSource>();
        if (Physics.Raycast(transform.position, -transform.up, out hit, 100))
        {
            this.transform.position = hit.point;
        }
    }
    private void OnTransformParentChanged()
    {
        journal.onioncount += 1;
        audio.Play();
        Destroy(this.gameObject);
    }
}
