using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BellClap : MonoBehaviour
{
    private Player_Movement pm;
    public float damage = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            pm = other.GetComponent<Player_Movement>();
            pm.Damage_Player(damage);
            pm.Stun_Player(2f);
        }
        Destroy(gameObject);
    }
}
