using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dummy : MonoBehaviour
{
    public int Health;
    private bool knocked_back;
    private float knocked_time;
    private float knocked_speed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (knocked_back)
        {
            transform.position += transform.forward * -1.0f * knocked_speed * Time.deltaTime;
            knocked_time -= Time.deltaTime;
            if (knocked_time < 0)
            {
                knocked_time = 0;
                knocked_back = false;
            }
        }
        if (Health <= 0)
        {
            Destroy(this.gameObject);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (GetComponent<BellAI>() == null && col.gameObject.CompareTag("Explosion"))
        {
            Standard_Explosion se = col.gameObject.GetComponent<Standard_Explosion>();
            Health -= se.damage;
            KnockBack(col.gameObject.transform, se.knockback_time, se.knockback_speed);
            /*
            knocked_back = true;
            knocked_time = se.knockback_time;
            knocked_speed = se.knockback_speed;
            Vector3 facePos = new Vector3(col.gameObject.transform.position.x, transform.position.y, col.gameObject.transform.position.z);
            transform.LookAt(facePos, Vector3.up);
            */
        }
    }
    
    public void KnockBack(Transform obj, float t_knocked_time, float t_knocked_speed)
    {
        knocked_back = true;
        knocked_time = t_knocked_time;
        knocked_speed = t_knocked_speed;
        Vector3 facePos = new Vector3(obj.position.x, transform.position.y, obj.position.z);
        transform.LookAt(facePos, Vector3.up);
    }
}
