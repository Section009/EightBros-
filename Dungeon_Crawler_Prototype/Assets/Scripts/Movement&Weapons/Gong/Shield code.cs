using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shieldcode : MonoBehaviour
{
    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Wall"))
        {
            transform.parent.GetComponent<Melee_Combat_Base>().End_Dash();
        }
    }
}
