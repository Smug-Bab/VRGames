using UnityEngine;

public class sonicring : MonoBehaviour
{
    [SerializeField] int ringValue = 1;
    [SerializeField] Material ringShader;
    [SerializeField] AudioSource ringSound;
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] GameObject lod0;

    void Start()
    {
        Material instance = new Material(ringShader);
        instance.SetFloat("_ringpower", ringValue);

        meshRenderer.material = instance;
        lod0.GetComponent<MeshRenderer>().material = instance;
    }
    void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                ringSound.PlayOneShot(ringSound.clip);
                Destroy(gameObject);
            }
        }
}