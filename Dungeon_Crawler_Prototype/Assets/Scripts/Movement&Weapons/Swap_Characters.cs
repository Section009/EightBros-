using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swap_Characters : MonoBehaviour
{
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
    // Start is called before the first frame update
    void Start()
    {
        
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
            else if (swap_available == false)
            {
                swap_timer += Time.deltaTime;
                if (swap_timer >= swap_timer_max)
                {
                    swap_available = true;
                    swap_timer = 0;
                }
            }

            if ((Input.GetKeyDown("e")) && (swap_available))
            {
                float dist = Vector3.Distance(transform.position, SwapToPos.transform.position);
                if (dist <= swap_dist)
                {
                    default_y = transform.position.y;
                    this.gameObject.GetComponent<Player_Movement>().Locked = true;
                    swap_active = true;
                    swap_available = false;
                    Camera.GetComponent<Camera_Follow>().active = false;
                }
            }
        }
        
    }

    void Swap_Active()
    {
        GetComponent<Rigidbody>().velocity = new Vector3(0f, swap_duration_speed, 0f);
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
            Destroy(this.gameObject);
        }
    }

    void Swap_In()
    {
        GetComponent<Rigidbody>().velocity = new Vector3(0f, -1f * swap_duration_speed, 0f);
        swap_duration_timer += Time.deltaTime;
        if (swap_duration_timer >= swap_duration_timer_max)
        {
            swap_duration_timer = 0f;
            this.gameObject.GetComponent<Player_Movement>().Locked = false;
            Camera.GetComponent<Camera_Follow>().active = true;
            swap_in = false;
        }
    }
}
