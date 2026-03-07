using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class JDPartData : MonoBehaviour
{
    [SerializeField] ParticleSystem JDPartSys;
    public ParticleSystem.MainModule JDPartSysMain;
    public ParticleSystem.ShapeModule JDPartSysShape;
    public ParticleSystem.VelocityOverLifetimeModule JDPartSysVel;
    public ParticleSystem.NoiseModule JDPartSysNoise;

    void Start()
    {
        JDPartSys = GetComponent<ParticleSystem>();
        JDPartSysMain = JDPartSys.main;
        JDPartSysVel = JDPartSys.velocityOverLifetime;
        JDPartSysNoise = JDPartSys.noise;
        JDPartSysShape = JDPartSys.shape;
    }
}
