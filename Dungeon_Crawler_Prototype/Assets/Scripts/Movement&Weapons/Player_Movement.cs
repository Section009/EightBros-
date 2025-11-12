using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.InputSystem;

public class Player_Movement : MonoBehaviour
{
    [SerializeField] float Speed;
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
    void Update()
    {
        /*
        if ((Input.GetButtonDown("Jump")) && (Dashing == false))
        {
            print("Dash");
            Dashing = true;
            Locked = true;
        }
        */
    }
    // Update is called once per frame
    void FixedUpdate()
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
        else
        {
            /*
            if (Dashing)
            {
                //transform.position += transform.forward * Dash_Speed * Time.deltaTime;
                rb.velocity = transform.forward * Dash_Speed;
                Dash_Timer += Time.deltaTime;
                if (Dash_Timer >= Dash_Time)
                {
                    Dash_Timer = 0;
                    Dashing = false;
                    Locked = false;
                }
            }
            */
        }
        
        
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
