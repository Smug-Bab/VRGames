using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;

public class knucklesmovement : MonoBehaviour
{
    [SerializeField] DynamicMoveProvider mover;
    [SerializeField] GravityProvider grav;
    [SerializeField] JumpProvider jumper;
    [SerializeField] CharacterController chara;
    [SerializeField] InputActionReference[] inputs;
    [SerializeField] GameObject[] gloves;
    [SerializeField] AudioSource audio;
    [SerializeField] AudioClip[] clips;
    [SerializeField] float frequency;
    public float maxspeed;
    private float startspeed;
    private float timerstep;

    void Start()
    {
        var startspeed = mover.moveSpeed;
    }
    private void OnEnable()
    {
        inputs[0].action.Enable();
        inputs[1].action.Enable();
    }
    private void OnDisable()
    {
        inputs[0].action.Disable();
        inputs[1].action.Disable();

    }

    
    private void FixedUpdate()
    {
        gloves[0].GetComponent<Animator>().SetFloat("Blend", inputs[0].action.ReadValue<float>());
        gloves[1].GetComponent<Animator>().SetFloat("Blend", inputs[1].action.ReadValue<float>());
        if (gloves[0].GetComponent<Animator>().GetFloat("Blend") >= 0.8 && gloves[1].GetComponent<Animator>().GetFloat("Blend") >= 0.8)
        {
            mover.leftHandMoveInput.manualValue = new Vector2(0, 0);
            grav.useGravity = false;
            StartCoroutine(Glide());
        } else
        {
            grav.useGravity = true;
            StopCoroutine(Glide());
        }

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
            timerstep = (float)0.02;
        }

    }


    void Footsteps()
    {
        if ((chara.velocity.magnitude < 0.5)) return;
        if (Vector2.Equals(mover.leftHandMoveInput.ReadValue(), Vector2.zero)) return;
        var vel = chara.velocity.magnitude;
        timerstep -= Time.fixedDeltaTime;
        if (timerstep < 0)
        {
            if (Physics.Raycast(chara.transform.position + new Vector3(0, (float)0.2, 0), Vector3.down, out RaycastHit hit, 1f))
            {
                switch (hit.collider.tag)
                {
                    case "Ground/wet":
                        audio.PlayOneShot(clips[0]);
                        break;
                    case "Ground/wood":
                        audio.PlayOneShot(clips[1]);
                        break;
                    case "Ground/metal":
                        audio.PlayOneShot(clips[2]);
                        break;
                    default:
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
    IEnumerator Glide()
    {
        if (!chara.isGrounded)
        {
            chara.transform.Translate(new Vector3(0f  , 0 , (gloves[0].transform.forward.z * gloves[1].transform.forward.z)) , Space.Self);
        }
        yield return null;
    }
}
