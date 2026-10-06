using UnityEngine;

public class sonicspring : MonoBehaviour
{
    [SerializeField] int springValue = 0;
    [SerializeField] Material ringShader;
    [SerializeField] AudioSource springSound;
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] GameObject lod0;

    void Start()
    {
        Material instance = new Material(ringShader);
        instance.SetFloat("_springvalue", (int)springValue/100);

        meshRenderer.material = instance;
        lod0.GetComponent<MeshRenderer>().material = instance;
    }
    void OnTriggerEnter(Collider other)
        {
                springSound.PlayOneShot(springSound.clip);
                other.GetComponent<Rigidbody>().AddForce(transform.forward * springValue, ForceMode.Impulse);
        }
}
