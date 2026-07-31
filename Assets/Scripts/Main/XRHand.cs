using UnityEngine;

// Specify a concrete collider type like SphereCollider so Unity knows exactly what to create
[RequireComponent(typeof(SphereCollider))]
public class XRHand : MonoBehaviour
{
    public enum HandType { Left, Right }
    public HandType Handedness;

    private void Awake()
    {
        ConfigureTrigger();
    }

    private void Reset()
    {
        ConfigureTrigger();
    }

    private void ConfigureTrigger()
    {
        // This will now safely find the SphereCollider (or any existing collider)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
}
