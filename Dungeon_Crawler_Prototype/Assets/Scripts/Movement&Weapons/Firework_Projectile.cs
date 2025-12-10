using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Firework_Projectile : MonoBehaviour
{
    public float speed;
    public float life_time;
    public int damage;
    private float life_timer;
    public float Bounty_Gained;
    public GameObject Explosion_Auto;
    public GameObject Explosion_Enemy_Hit;
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
            Instantiate(Explosion_Auto, transform.position, Quaternion.identity);
            Instantiate(Explosion_SFX, transform.position, Quaternion.identity);
            Destroy(this.gameObject);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        print("col");
        if (col.gameObject.CompareTag("Enemy"))
        {
            Destroy(this.gameObject);
            print("Kill");
        }
        Instantiate(Explosion_Enemy_Hit, transform.position, Quaternion.identity);
        Instantiate(Explosion_SFX, transform.position, Quaternion.identity);
        Destroy(this.gameObject);
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Enemy"))
        {
            GameObject cooldown = GameObject.FindWithTag("Cooldown_Tracker");
            cooldown.GetComponent<Player_Cooldown_Master>().Firework_Ultimate_Cur_Points += Bounty_Gained;
            print("Add");
            Instantiate(Explosion_Enemy_Hit, transform.position, Quaternion.identity);
            Instantiate(Explosion_SFX, transform.position, Quaternion.identity);
            Destroy(this.gameObject);
            print("Kill");
        }
        else if (col.gameObject.CompareTag("Wall"))
        {
            Instantiate(Explosion_Auto, transform.position, Quaternion.identity);
            Instantiate(Explosion_SFX, transform.position, Quaternion.identity);
            Destroy(this.gameObject);
        }
    }
    public void Firework_Effect()
    {
        GameObject cooldown = GameObject.FindWithTag("Cooldown_Tracker");
        cooldown.GetComponent<Player_Cooldown_Master>().Firework_Ultimate_Cur_Points += Bounty_Gained;
        print("Add");
    }
}
