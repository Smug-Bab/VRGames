using UnityEngine;

public class JDPartData : MonoBehaviour
{
    public ParticleSystem JDPartSys;

    void Awake()
    {
        JDPartSys = GetComponent<ParticleSystem>();
    }
}
