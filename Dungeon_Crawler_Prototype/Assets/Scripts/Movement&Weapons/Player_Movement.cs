using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

//using UnityEngine.InputSystem;

public class Player_Movement : MonoBehaviour
{
    public GameObject Model;
    public bool Paused;
    public float Speed;
    [SerializeField] float Dash_Time;
    [SerializeField] float Dash_Speed;
    private float Dash_Timer;
    public bool Dashing;
    public bool Locked;
    private bool moving;
    public Rigidbody rb;
    public bool slowed;
    public float slow_reduction;
    public string RunName;
    public string IdleName;
    public string IdleAnimName;
    public float IdleAnimLength;
    private bool Idling = false;
    private float IdleAnimation_Timer = 0;
    public float IdleCooldown;
    public string HitName;
    public float HitAnimLength;
    private bool hit = false;
    private float hit_timer = 0;
    private Health health_hub;

    private Animator animator;
    private bool stunned = false;

    // Start is called before the first frame update
    void Start()
    {
        animator = Model.GetComponent<Animator>();
        if (animator == null)
        {
            UnityEngine.Debug.LogError("Animator Failed");
        }
        rb = GetComponent<Rigidbody>();
        health_hub = GetComponent<Health>();
    }
    
    // Update is called once per frame
    void FixedUpdate()
    {
        if (Paused == false)
        {
            
            float horiInput = Input.GetAxisRaw("Horizontal");
            float vertInput = Input.GetAxisRaw("Vertical");

            Vector3 vec = new Vector3(horiInput, 0f, vertInput);

            if (Locked == false && stunned == false)
            {
                transform.LookAt(transform.position + vec, Vector3.up);
                if (vec.magnitude != 0)
                {
                    moving = true;
                    rb.velocity = transform.forward * Speed;
                    if (slowed)
                    {
                        rb.velocity /= slow_reduction;
                    }
                }

                else
                {
                    moving = false;
                    rb.velocity = new Vector3(0f, 0f, 0f);
                }
            }
            else
            {
                Idling = false;
                IdleAnimation_Timer = 0f;
            }
            if (hit)
            {
                Idling = false;
                IdleAnimation_Timer = 0f;
                animator.Play(HitName);
                hit_timer += Time.deltaTime;
                if (hit_timer >= HitAnimLength)
                {
                    hit_timer = 0f;
                    hit = false;
                }
            }
            else if (Locked == false && stunned == false)
            {
                if (moving)
                {
                    animator.Play(RunName);
                    Idling = false;
                    IdleAnimation_Timer = 0f;
                }
                else
                {
                    if (Idling)
                    {
                        animator.Play(IdleAnimName);
                        IdleAnimation_Timer += Time.deltaTime;
                        if (IdleAnimation_Timer >= IdleAnimLength)
                        {
                            IdleAnimation_Timer = 0f;
                            Idling = false;
                        }
                    }
                    else
                    {
                        animator.Play(IdleName);
                        IdleAnimation_Timer += Time.deltaTime;
                        if (IdleAnimation_Timer >= IdleCooldown)
                        {
                            IdleAnimation_Timer = 0f;
                            Idling = true;
                        }
                    }
                }
            }
        }
        
    }

    void Update()
    {
        if (Input.GetKeyDown("q"))
        {
            Damage_Player(5.0f);
        }

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
    public void Game_Over()
    {
        GameObject Head_UI = GameObject.FindWithTag("UI_Handler");
        Head_UI.GetComponent<UI_Master>().Death_Screen_Activate();
    }

    public void Damage_Player(float damage)
    {
        hit = true;
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
