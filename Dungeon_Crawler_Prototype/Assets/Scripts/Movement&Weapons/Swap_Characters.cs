using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swap_Characters : MonoBehaviour
{
    public Player_Cooldown_Master pcm;
    private Player_Movement pm;
    public GameObject Camera;
    public GameObject New_Player;
    public GameObject Smoke_Prefab;
    public GameObject Smoke;
    public float swap_timer_max;
    public float swap_timer;
    private float swap_duration_timer;
    public float swap_duration_timer_max;
    public bool swap_available;
    private bool swap_active;
    private bool swap_in = false;
    // Start is called before the first frame update
    void Start()
    {
        pm = GetComponent<Player_Movement>();
        pcm = GameObject.FindGameObjectWithTag("Cooldown_Tracker").GetComponent<Player_Cooldown_Master>();
    }

    // Update is called once per frame
    void Update()
    {
        if (swap_in)
        {
            Swap_In();
        }
        else
        {
            if (swap_active)
            {
                Swap_Active();
            }

            if ((Input.GetKeyDown("e")) && (pcm.Swap_Available) && (pm.Locked == false))
            {
                GetComponent<Rigidbody>().velocity = new Vector3(0f, 0f, 0f);
                Smoke = Instantiate(Smoke_Prefab, transform.position + new Vector3(0f, -0.5f, 0f), transform.rotation);
                //Smoke.transform.parent = transform;
                pm.Locked = true;
                swap_active = true;
                //Camera.GetComponent<Camera_Follow>().active = false;
            }
        }
        
    }

    void Swap_Active()
    {
        Swap_Movement();
        swap_duration_timer += Time.deltaTime;
        if (swap_duration_timer >= swap_duration_timer_max)
        {
            GameObject go = Instantiate(New_Player, transform.position, transform.rotation);
            pcm.Melee_Open = !pcm.Melee_Open;
            Camera.GetComponent<Camera_Follow>().follow = go;
            go.GetComponent<Swap_Characters>().Camera = Camera;
            go.GetComponent<Swap_Characters>().swap_in = true;
            go.GetComponent<Player_Movement>().Locked = true;
            go.GetComponent<Swap_Characters>().Smoke = Smoke;
            go.GetComponent<Health>().currentHealth = GetComponent<Health>().currentHealth;
            Destroy(this.gameObject);
        }
    }

    void Swap_In()
    {
        Swap_Movement();
        swap_duration_timer += Time.deltaTime;
        if (swap_duration_timer >= swap_duration_timer_max)
        {
            swap_duration_timer = 0f;
            pcm.Swap_Available = false;
            pcm.Swap_Cooldown_timer = 0f;
            pm.Locked = false;
            Camera.GetComponent<Camera_Follow>().active = true;
            swap_in = false;
            Destroy(Smoke);
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                if (enemy.GetComponent<EnemyAI>() != null)
                {
                    enemy.GetComponent<EnemyAI>().Assign_Player();
                }
                else if (enemy.GetComponent<BellAI>() != null)
                {
                    enemy.GetComponent<BellAI>().Assign_Player();
                }
            }
        }
    }
    
    void Swap_Movement()
    {
        float horiInput = Input.GetAxis("Horizontal");
        float vertInput = Input.GetAxis("Vertical");

        Vector3 vec = new Vector3(horiInput, 0f, vertInput);

        transform.LookAt(transform.position + vec, Vector3.up);
        if (vec.magnitude != 0)
        {
            pm.rb.velocity = transform.forward * pm.Speed;
        }
        else
        {
            pm.rb.velocity = new Vector3(0f, 0f, 0f);
        }
    }
}
