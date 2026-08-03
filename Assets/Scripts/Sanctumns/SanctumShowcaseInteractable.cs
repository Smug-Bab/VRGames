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

    [Header("Events")]
    [Tooltip("Add functions here that should fire immediately when this specific object is interacted with.")]
    public UnityEvent OnInteractionTriggered;

    /// <summary>
    /// This is the main entry point called by your system architecture when selected.
    /// </summary>
    public void Interact()
    {
        OnInteractionTriggered?.Invoke();
    }

    // --- Public Utility Functions to Hook Up into UnityEvents ---

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
