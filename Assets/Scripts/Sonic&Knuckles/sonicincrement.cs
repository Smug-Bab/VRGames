using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class SonicIncrement : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] XRPlayerMovement mover;
    [SerializeField] Rigidbody rb;
    [SerializeField] Collider col;
    [SerializeField] AudioClip[] clips;
    [SerializeField] float inc = 1f;
    [SerializeField] float ld = 4f;
    [SerializeField] float dist = 1f;
    [SerializeField] AudioSource audio;
    [SerializeField] ParticleSystem ps;
    [SerializeField] Transform pt;

    ParticleSystem.MainModule pm;
    Vector2 joy;
    RaycastHit hit;

    void Start() => pm = ps.main;

    void FixedUpdate()
    {
        joy = mover.moveAction.ReadValue<Vector2>();
        StartCoroutine(SpeedSlopeManager(joy));
    }
    IEnumerator SpeedSlopeManager(Vector2 joy)
    {
        if (Physics.Raycast(player.transform.position, player.transform.TransformDirection(Vector3.down), out hit, dist) && joy != Vector2.zero)
        {
            player.transform.position = hit.point + hit.normal * col.bounds.extents.y;
            player.transform.up = Vector3.Lerp(player.transform.up, hit.normal, 8f * Time.fixedDeltaTime);
            rb.useGravity = false;
            rb.linearDamping = ld;
            mover.moveSpeed += inc;
        }
        else
        {
            player.transform.up = Vector3.Lerp(player.transform.up, Vector3.up, 20f * Time.fixedDeltaTime);
            rb.useGravity = true;
            rb.linearDamping = 0f;
            mover.moveSpeed = (int)(rb.linearVelocity.magnitude * 10f);
            ps.Stop();
        }
        yield return new WaitForSeconds(0.1f);
    }
    IEnumerator FootstepManager()
    {
        switch (hit.collider.tag)
            {
                case "mats/dirty":
                    audio.PlayOneShot(clips[0]);
                    pm.startSpeed = rb.linearVelocity.magnitude * 0.5f;
                    pt.eulerAngles = new Vector3(0f, joy.x * joy.y * 100f, 0f);
                    ps.Play();
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
        yield return null;
    }
}