using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Standard_Projectile : MonoBehaviour
{
    public float speed;
    public float life_time;
    public float damage;
    private float life_timer;
    public float knockback_time;
    public float knockback_speed;
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
        Dummy d = col.gameObject.GetComponent<Dummy>();
        if (d != null)
        {

        }
        if (col.gameObject.CompareTag("Enemy"))
        {
            Destroy(this.gameObject);
            print("Kill");
        }
    }
}
