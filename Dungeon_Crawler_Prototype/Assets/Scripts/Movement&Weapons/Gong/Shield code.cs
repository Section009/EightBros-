using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shieldcode : MonoBehaviour
{
    void OnCollisionStay(Collision col)
    {
        print("asdfa");
        if (col.gameObject.CompareTag("Wall"))
        {
            transform.parent.GetComponent<Melee_Combat_Base>().End_Dash();
        }
    }
}
