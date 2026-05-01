using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine;

public class sonicmovement : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] XRPlayerMovement mover;
    [SerializeField] Rigidbody rigid;
    [SerializeField] Collider coll;
    [SerializeField] AudioSource audio;
    [SerializeField] AudioClip[] clips;
    [SerializeField] float frequency;
    public float incre;
    public float dist;
    public float maxspeed;
    public ParticleSystem partsys;
    public ParticleSystem.MainModule partsysmain;
    public Transform partsysshape;
    private float startspeed;
    private float timerstep;
    private void Start()
    {
        var startspeed = mover.mult;
        partsysmain = partsys.main;
    }
    private void FixedUpdate()
    {
        RaycastHit hit;
        if (!Vector2.Equals(mover.PrimaryJoy.ReadValue<Vector2>(), Vector2.zero))
        {
            if (Physics.Raycast(player.transform.position, player.transform.TransformDirection(Vector3.down), out hit, dist))
            {
                player.transform.position = hit.point + (hit.normal * coll.bounds.extents.y);
                player.transform.up = Vector3.Lerp(player.transform.up, hit.normal, 8f * Time.fixedDeltaTime);
                rigid.useGravity = false;
            } else
            {
                player.transform.up = Vector3.Lerp(player.transform.up, Vector3.up, 4f * Time.fixedDeltaTime);
                rigid.useGravity = true;
            }
            mover.mult += incre;
            Footsteps();

        }
        else
        {
            player.transform.up = Vector3.Lerp(player.transform.up, Vector3.up, 20f * Time.fixedDeltaTime);
            rigid.useGravity = true;
            mover.mult = (int)(rigid.linearVelocity.magnitude * 100);
            partsys.Stop();
            timerstep = (float)0.02;
        }
    }
    private void Footsteps()
    {
        if ((rigid.linearVelocity.magnitude < 0.5) || Vector2.Equals(mover.PrimaryJoy.ReadValue<Vector2>(), Vector2.zero)) return;
        var vel = rigid.linearVelocity.magnitude;
        timerstep -= Time.fixedDeltaTime;
        if (timerstep < 0)
        {
            if (Physics.Raycast(rigid.transform.position + new Vector3(0, (float)0.2, 0), Vector3.down, out RaycastHit hit, 1))
            {
                switch (hit.collider.tag)
                {
                    case "mats/dirty":
                        audio.PlayOneShot(clips[0]);
                        partsysmain.startSpeed = vel / 2;
                        partsysshape.eulerAngles = new Vector3(0, mover.PrimaryJoy.ReadValue<Vector2>().x * mover.PrimaryJoy.ReadValue<Vector2>().y * 100, 0);
                        partsys.Play();
                        break;
                    case "mats/hard":
                        audio.PlayOneShot(clips[1]);
                        break;
                    case "mats/metallic":
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
