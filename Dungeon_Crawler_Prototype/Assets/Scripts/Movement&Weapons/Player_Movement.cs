using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.InputSystem;

public class Player_Movement : MonoBehaviour
{
    public bool Paused;
    public float Speed;
    [SerializeField] float Dash_Time;
    [SerializeField] float Dash_Speed;
    private float Dash_Timer;
    public bool Dashing;
    public bool Locked;
    public Rigidbody rb;
    public bool slowed;
    public float slow_reduction;
    private Health health_hub;


    private bool stunned = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        health_hub = GetComponent<Health>();
    }
    
    // Update is called once per frame
    void FixedUpdate()
    {
        if (Paused == false)
        {
            float horiInput = Input.GetAxis("Horizontal");
            float vertInput = Input.GetAxis("Vertical");

            Vector3 vec = new Vector3(horiInput, 0f, vertInput);

            if (Locked == false && stunned == false)
            {
                transform.LookAt(transform.position + vec, Vector3.up);
                if (vec.magnitude != 0)
                {
                    //transform.position += transform.forward * Speed * Time.deltaTime;
                    rb.velocity = transform.forward * Speed;
                    if (slowed)
                    {
                        rb.velocity /= slow_reduction;
                    }
                }

                else
                {
                    rb.velocity = new Vector3(0f, 0f, 0f);
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown("t"))
        {
            Paused = !Paused;
            if (Paused)
            {
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
            }
            GameObject Head_UI = GameObject.FindWithTag("UI_Handler");
            Head_UI.GetComponent<UI_Master>().Set_Paused(Paused);
        }
    }

    public void Unpause()
    {
        Paused = false;
        Time.timeScale = 1f;
    }

    public void Damage_Player(float damage)
    {
        health_hub.TakeDamage(Mathf.RoundToInt(damage));
    }

    public void Stun_Player(float stunTime)
    {
        StartCoroutine(DoStun(stunTime));
    }
    IEnumerator DoStun(float stunTime)
    {
        
        stunned = true;
        rb.velocity *= 0;
        yield return new WaitForSeconds(stunTime);
        stunned = false;
    }
}
