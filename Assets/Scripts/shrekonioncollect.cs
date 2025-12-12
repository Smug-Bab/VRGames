using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shrekonioncollect : MonoBehaviour
{
    [SerializeField] AudioSource audio;
    [SerializeField] shrekjournal journal;
    [SerializeField] LayerMask layer;
    private void Start()
    {
        RaycastHit hit;
        audio = this.GetComponent<AudioSource>();
        if (Physics.Raycast(transform.position, -transform.up, out hit, 100))
        {
            this.transform.position = hit.point;
        }
    }
    public void Collect()
    {
        journal.onioncount += 1;
        audio.Play();
        Destroy(this);
    }
}
