using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BellClap : MonoBehaviour
{
    private Player_Movement pm;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            pm = other.GetComponent<Player_Movement>();
            pm.Damage_Player(5f);
            pm.Stun_Player(2f);
            Destroy(gameObject);
        }
    }
}
