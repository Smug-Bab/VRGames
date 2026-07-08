using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(XRGrabInteractable))]
public class ShrekOnionCollect : MonoBehaviour
{
    [SerializeField] private shrekjournal journal;
    [SerializeField] private AudioClip collectSound;
    
    private AudioSource audioSource;
    private XRGrabInteractable grabInteractable;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        
        grabInteractable.selectEntered.AddListener(OnOnionGrabbed);

        
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 100))
        {
            transform.position = hit.point;
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnOnionGrabbed);
        }
    }

    private void OnOnionGrabbed(SelectEnterEventArgs args)
    {
        if (journal != null)
        {
            journal.onioncount += 1;
        }
        if (audioSource != null && collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        Destroy(gameObject);
    }
}