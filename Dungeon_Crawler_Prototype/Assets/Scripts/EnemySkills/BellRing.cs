using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BellRing : MonoBehaviour
{
    private Player_Movement pm;
    private BellAI ba;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            pm = other.GetComponent<Player_Movement>();
            ba = transform.parent.GetComponent<BellAI>();

            if (ba.ringing) pm.Damage_Player(10f);
            else pm.Damage_Player(5f);

            Destroy(gameObject);
        }
    }
}
