using UnityEngine;

public class XRPlayerHeight : MonoBehaviour
{
    [SerializeField] CapsuleCollider capsuleCollider;
    [SerializeField] Transform CameraTransform;

    void FixedUpdate()
    {
        capsuleCollider.height = CameraTransform.transform.localPosition.y;
        capsuleCollider.center = new Vector3(capsuleCollider.center.x, capsuleCollider.height / 2, capsuleCollider.center.z);
    }
}
