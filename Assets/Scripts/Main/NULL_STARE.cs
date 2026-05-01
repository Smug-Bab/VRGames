using UnityEngine;

public class NULL_STARE : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    void Update()
    {
        transform.LookAt(_player.transform);
    }
}
