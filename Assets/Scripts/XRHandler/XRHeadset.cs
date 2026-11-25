using UnityEngine;

public class XRHeadset : MonoBehaviour
{
    [SerializeField] public Transform headsetCamera;
    [SerializeField] public CapsuleCollider capsule;
    [SerializeField] public Rigidbody rigid;

    private void FixedUpdate()
    {
        // Height adjustment
        capsule.height = Mathf.Clamp(headsetCamera.localPosition.y, 0.5f, 2.5f);
    }
}
