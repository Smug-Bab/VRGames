using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class shrekwater : MonoBehaviour
{
    public GameObject failbox;
    public Material failmat;
    public AudioSource audio;
    public AudioClip[] clips;
    private void OnTriggerEnter(Collider other)
    {
        if (this.CompareTag("shrek") )
            {

        }
    }

    IEnumerator Kill()
    {
        yield return new WaitForSeconds(8);
    }
}
