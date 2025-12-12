using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class shrekboatcutscene : MonoBehaviour
{
    [SerializeField] private GameObject[] puppets;
    [SerializeField] private AudioClip[] lines;
    public void PlayGame()
    {
        puppets[0].SetActive(false);
        StartCoroutine(Speak());
    }
    IEnumerator Speak()
    {
        yield return new WaitForSeconds(1);
        puppets[1].GetComponent<AudioSource>().PlayOneShot(lines[0]);
        yield return new WaitForSeconds(16);
        puppets[1].GetComponent<AudioSource>().PlayOneShot(lines[1]);
        yield return new WaitForSeconds(9);
        puppets[2].GetComponent<AudioSource>().PlayOneShot(lines[2]);
        yield return new WaitForSeconds(10);
        puppets[1].GetComponent<AudioSource>().PlayOneShot(lines[3]);
        yield return new WaitForSeconds(2);
        puppets[3].gameObject.GetComponent<AudioSource>().volume = (float)0.3;
        puppets[2].GetComponent<AudioSource>().PlayOneShot(lines[4]);
        yield return new WaitForSeconds(10);
        puppets[3].gameObject.GetComponent<AudioSource>().volume = (float)0.6;
        puppets[1].GetComponent<AudioSource>().PlayOneShot(lines[5]);
        yield return new WaitForSeconds(18);
        puppets[3].gameObject.GetComponent<AudioSource>().volume = 1;
        puppets[1].GetComponent<AudioSource>().PlayOneShot(lines[6]);
        puppets[4].GetComponent<Animator>().Play("shrekboatcutscene");
        yield return new WaitForSeconds(8);
        puppets[5].GetComponent<AudioSource>().Play();
        yield return new WaitForSeconds(8);
        puppets[1].GetComponent<AudioSource>().PlayOneShot(lines[7]);
        yield return new WaitForSeconds(4);
        puppets[1].GetComponent<AudioSource>().PlayOneShot(lines[8]);
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene("shrekisland");
    } 

    public void QuitGame()
    {
        SceneManager.LoadScene("main");
    }
}
