using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class pumpreset : MonoBehaviour
{
    private void OnTriggerExit(Collider other)
    {
        StartCoroutine(Transition());
    }
    public IEnumerator Transition()
    {
        for (int i = 0; i < 10; i++)
        {
            RenderSettings.fogDensity += (float)0.003;
            yield return new WaitForSeconds(0.05f);
        }
        SceneManager.LoadScene("knuckles");
    }
}
