using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine;

public class shrekjournal : MonoBehaviour
{
    public int onioncount = 0;
    [SerializeField] InputActionReference controller;
    [SerializeField] Animator anim;
    [SerializeField] TMP_Text counter;

    private void OnEnable()
    {

        controller.action.started -= close;
        controller.action.started += open;
        counter.text = onioncount.ToString();
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
