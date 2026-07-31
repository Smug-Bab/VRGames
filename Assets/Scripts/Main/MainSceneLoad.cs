using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // This will show up in the Unity Inspector
    [Header("Scene Settings")]
    public string sceneToLoad;

    // Call this method from a UI Button onClick event or another script
    public void LoadTargetScene()
    {
        // Safety check to ensure the string isn't empty
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Scene name is empty! Please assign a scene name in the Inspector.");
        }
    }
}
