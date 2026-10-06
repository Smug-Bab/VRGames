using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SanctumShowcaseInteractable : MonoBehaviour
{
    [Header("Interaction Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animationStateName = "PlaySelection";

    [Header("Interaction Scene")]
    [SerializeField] private string sceneToLoad;


    public void PlayShowcaseAnimation()
    {
        if (animator != null && !string.IsNullOrEmpty(animationStateName))
        {
            animator.Play(animationStateName);
        }
    }

    public void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
