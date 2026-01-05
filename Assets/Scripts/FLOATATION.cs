using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FLOATATION : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] GameObject[] portals;
    private void FixedUpdate()
    {
        transform.position = Vector3.Lerp(new Vector3(0, transform.position.y, 0), new Vector3(0, player.transform.position.y, 0), Time.deltaTime / 6);
        foreach(GameObject BigP in portals)
        {
            if (BigP.transform.childCount < 2)
            {
                break;

            }

            var kart = BigP.transform.GetChild(1);
            var SmallP = BigP.transform.GetChild(0);
            kart.forward = new Vector3(0, 1, 0);

            kart.transform.localPosition = Vector3.Lerp(Vector3.zero, kart.transform.localPosition, Time.deltaTime / 4);
            kart.transform.LookAt(player.transform.position);
        }
    }
}
