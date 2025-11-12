using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swap_Characters : MonoBehaviour
{
    public Player_Cooldown_Master pcm;
    private Player_Movement pm;
    public GameObject Camera;
    public GameObject SwapToPos;
    public GameObject New_Player;
    public GameObject Place_Holder;
    public float swap_timer_max;
    public float swap_timer;
    private float swap_duration_timer;
    public float swap_duration_timer_max;
    public float swap_duration_speed;
    private float default_y;
    public bool swap_available;
    private bool swap_active;
    private bool swap_in = false;
    public float swap_dist = 30f;
    public bool first_swap = true;
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
                if (first_swap)
                {
                    Vector3 newpos = transform.position + transform.forward;
                    SwapToPos = Instantiate(Place_Holder, newpos, transform.rotation);
                    default_y = transform.position.y;
                    pm.Locked = true;
                    swap_active = true;
                    swap_available = false;
                    Camera.GetComponent<Camera_Follow>().active = false;
                }
                else
                {
                    float dist = Vector3.Distance(transform.position, SwapToPos.transform.position);
                    if (dist <= swap_dist)
                    {
                        default_y = transform.position.y;
                        pm.Locked = true;
                        swap_active = true;
                        swap_available = false;
                        Camera.GetComponent<Camera_Follow>().active = false;
                    }
                }
            }
        }
        
    }

    void Swap_Active()
    {
        //GetComponent<Rigidbody>().velocity = new Vector3(0f, swap_duration_speed, 0f);
        swap_duration_timer += Time.deltaTime;
        if (swap_duration_timer >= swap_duration_timer_max)
        {
            Vector3 newSpawnPos = new Vector3(SwapToPos.transform.position.x, transform.position.y, SwapToPos.transform.position.z);
            GameObject go = Instantiate(New_Player, newSpawnPos, SwapToPos.transform.rotation);
            Destroy(SwapToPos);
            Camera.GetComponent<Camera_Follow>().follow = go;
            Vector3 newpos = new Vector3(transform.position.x, default_y, transform.position.z);
            GameObject temp = Instantiate(Place_Holder, newpos, transform.rotation);
            go.GetComponent<Swap_Characters>().SwapToPos = temp;
            go.GetComponent<Swap_Characters>().Camera = Camera;
            Camera.transform.position = SwapToPos.transform.position + Camera.GetComponent<Camera_Follow>().offset;
            go.GetComponent<Swap_Characters>().swap_in = true;
            go.GetComponent<Player_Movement>().Locked = true;
            go.GetComponent<Health>().currentHealth = GetComponent<Health>().currentHealth;
            Destroy(this.gameObject);
        }
    }

    void Swap_In()
    {

        //GetComponent<Rigidbody>().velocity = new Vector3(0f, -1f * swap_duration_speed, 0f);
        swap_duration_timer += Time.deltaTime;
        if (swap_duration_timer >= swap_duration_timer_max)
        {
            swap_duration_timer = 0f;
            pcm.Swap_Available = false;
            pcm.Swap_Cooldown_timer = 0f;
            this.gameObject.GetComponent<Player_Movement>().Locked = false;
            Camera.GetComponent<Camera_Follow>().active = true;
            swap_in = false;
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
    /*
    void OnTriggerEnter(Collider col)
    {
        if ((swap_in)&&(col.gameObject.CompareTag("Floor")))
        {
            pcm.Swap_Available = false;
            pcm.Swap_Cooldown_timer = 0f;
            this.gameObject.GetComponent<Player_Movement>().Locked = false;
            Camera.GetComponent<Camera_Follow>().active = true;
            swap_in = false;
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
    */
}
