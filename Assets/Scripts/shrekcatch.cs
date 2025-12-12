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
    public LayerMask mask;
    private void OnTriggerEnter(Collider other)
    {
        if (this.CompareTag(mask.ToString()))
            Kill();
            {

        }
    }

    IEnumerator Kill()
    {
        yield return new WaitForSeconds(8);
        SceneManager.LoadScene("shrekswamp");
    }
}
