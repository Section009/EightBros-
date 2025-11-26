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
        GameObject cooldown = GameObject.FindWithTag("Cooldown_Tracker");
        cooldown.GetComponent<Player_Cooldown_Master>().Melee_Ultimate_Cur_Points += Bounty_Gained;
    }
}
