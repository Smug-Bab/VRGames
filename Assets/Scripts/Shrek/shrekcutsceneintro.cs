using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class shrekcutsceneintro : MonoBehaviour
{
    [SerializeField] private GameObject[] puppets;
    [SerializeField] private AudioClip[] lines;
    private void Start()
    {
        StartCoroutine(Cutscene());
    }
    IEnumerator Cutscene()
    {
            puppets[0].GetComponent<NavMeshAgent>().destination = puppets[1].transform.position;
        yield return new WaitForSeconds(6);
        this.gameObject.SetActive(false);
    }
}
