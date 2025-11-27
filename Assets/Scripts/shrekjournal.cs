using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

public class shrekjournal : MonoBehaviour
{
    public InputActionReference controller;
    public Animator anim;

    private void OnEnable()
    {
        controller.action.started -= close;
        controller.action.started += open;
    }

    private void OnDisable()
    {
        controller.action.started -= open;
        controller.action.started += close;
        this.transform.localPosition = new Vector3((float)-0.25, 0, (float)0.25);
        this.transform.localEulerAngles = new Vector3(0, 0, 270);
    }

    void open(InputAction.CallbackContext context)
    {
        anim.Play("shrekjournalopen");
    }
    void close(InputAction.CallbackContext context)
    {
        anim.Play("shrekjournalclose");
    }
}
