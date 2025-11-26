using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shieldcode : MonoBehaviour
{
    public float Bounty_Gained;
    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Wall"))
        {
            transform.parent.GetComponent<Melee_Combat_Base>().End_Dash();
        }
    }

    public void Gong_Effect()
    {
        GameObject player = GameObject.FindWithTag("Player");
        player.GetComponent<Melee_Combat_Base>().Bounty_Points += Bounty_Gained;
    }
}
