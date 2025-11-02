using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Firework_Projectile : MonoBehaviour
{
    public float speed;
    public float life_time;
    public int damage;
    private float life_timer;
    public GameObject Explosion;
    public GameObject Explosion_SFX;
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
            Instantiate(Explosion, transform.position, Quaternion.identity);
            Instantiate(Explosion_SFX, transform.position, Quaternion.identity);
            Destroy(this.gameObject);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Enemy"))
        {
            Destroy(this.gameObject);
            print("Kill");
        }
        Instantiate(Explosion, transform.position, Quaternion.identity);
        Instantiate(Explosion_SFX, transform.position, Quaternion.identity);
        Destroy(this.gameObject);
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
