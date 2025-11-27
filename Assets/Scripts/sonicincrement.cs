using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine;

public class sonicmovement : MonoBehaviour
{
    [SerializeField] DynamicMoveProvider mover;
    [SerializeField] CharacterController chara;
    [SerializeField] AudioSource audio;
    [SerializeField] AudioClip[] clips;
    [SerializeField] float frequency;
    public float maxspeed;
    public ParticleSystem partsys;
    public ParticleSystem.MainModule partsysmain;
    public Transform partsysshape;
    private float startspeed;
    private float timerstep;

    private void Start()
    {
        var startspeed = mover.moveSpeed;
        partsys = GetComponent<ParticleSystem>();
        partsysmain = partsys.main;
    }
    private void FixedUpdate()
    {
        mover.inAirControlModifier = mover.moveSpeed * (float)0.02;
        if (!(chara.velocity.magnitude < 0) && !Vector2.Equals(mover.leftHandMoveInput.ReadValue(), Vector2.zero) && mover.moveSpeed < maxspeed)
        {
            mover.moveSpeed += (float)0.08;
            Footsteps();

        }
        else if (!Vector2.Equals(mover.leftHandMoveInput.ReadValue(), Vector2.zero))
        {
            mover.moveSpeed -= (float)0.08;
        }
        else
        {
            mover.moveSpeed = startspeed;
            partsys.Stop();
            timerstep = (float)0.02;
        }

    }
    private void Footsteps()
    {
        if ((chara.velocity.magnitude < 0.5)) return;
        if (Vector2.Equals(mover.leftHandMoveInput.ReadValue(), Vector2.zero)) return;
        var vel = chara.velocity.magnitude;
        timerstep -= Time.fixedDeltaTime;
        if (timerstep < 0)
        {
            if (Physics.Raycast(chara.transform.position + new Vector3(0, (float)0.2, 0), Vector3.down, out RaycastHit hit, 1))
            {
                switch (hit.collider.tag)
                {
                    case "Ground/wet":
                        audio.PlayOneShot(clips[0]);
                        partsysmain.startSpeed = vel / 2;
                        partsysshape.eulerAngles = new Vector3(0, (mover.leftHandMoveInput.ReadValue().x * mover.leftHandMoveInput.ReadValue().y) * 100, 0);
                        partsys.Play();
                        break;
                    case "Ground/wood":
                        audio.PlayOneShot(clips[1]);
                        break;
                    case "Ground/metal":
                        audio.PlayOneShot(clips[2]);
                        break;
                    default:
                        audio.PlayOneShot(clips[0]);
                        break;
                }
            }
            if (vel < 1)
            {
                timerstep = frequency / (vel * 5);
            }
            else
            {
                timerstep = frequency / vel;
            }
        }
    }
}
