using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Standard_Explosion : MonoBehaviour
{
    public float life_timer_max;
    public float widen_speed;
    public int damage;
    private float timer;
    public float knockback_time;
    public float knockback_speed;
    public float Bounty_Gained;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        transform.localScale += (new Vector3(1f, 1f, 1f)) * widen_speed * Time.deltaTime;
        if (timer > life_timer_max)
        {
            Destroy(this.gameObject);
        }
    }

    public void Gong_Effect()
    {
        GameObject cooldown = GameObject.FindWithTag("Cooldown_Tracker");
        cooldown.GetComponent<Player_Cooldown_Master>().Melee_Ultimate_Cur_Points += Bounty_Gained;
    }
}
