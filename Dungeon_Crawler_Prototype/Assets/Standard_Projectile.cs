using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Standard_Projectile : MonoBehaviour
{
    public float speed;
    public float life_time;
    public float damage;
    private float life_timer;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        life_timer += Time.deltaTime;
        transform.position += transform.forward * speed * Time.deltaTime;
        if (life_timer >= life_time)
        {
            Destroy(this.gameObject);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        print("col");
        if (col.gameObject.CompareTag("Enemy"))
        {
            Destroy(this.gameObject);
            print("Kill");
        }
    }
}
