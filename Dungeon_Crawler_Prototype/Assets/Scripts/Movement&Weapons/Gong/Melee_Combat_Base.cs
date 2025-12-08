using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Melee_Combat_Base : MonoBehaviour
{
    public GameObject Model;
    public Player_Cooldown_Master pcm;
    private Player_Movement pm;
    private Rigidbody rb;
    public GameObject Ammo_Type;
    public int combo_count;
    public float[] cooldowns;
    private float timer;
    public float reset_cooldown = 0.5f;
    public string BasicAttackAnimName;
    private bool firing;
    //Basic Shift
    public float shift_speed;
    public float shift_timer_max;
    private float shift_timer;
    private bool shifting;
    //Charge Attack
    private bool c_firing;
    public GameObject Charged_Ammo_Type;
    public string ChargeAttackAnimName;
    public float charge_timer_max;
    private float charge_timer;
    private bool charged;
    public float charge_cooldown;
    public float charge_move_speed;
    //Charge Shift
    public float c_shift_speed;
    public float c_shift_timer_max;
    private float c_shift_timer;
    private bool c_shifting;
    //Skill
    public string SkillAnimName;
    public float skill_cooldown;
    public float skill_timer;
    public bool skill_available;
    private bool skill_active;
    //Gong Skill
    public float jump_height;
    public float jump_time_max;
    private float jump_dist;
    public GameObject Landing_Explosion;
    public float skill_duration;
    private float skill_duration_timer;
    //Ultimate
    public string UltimateAnimName;
    public float Bounty_Points;
    public float ultimate_cooldown;
    public float ultimate_timer;
    public bool ultimate_available;
    private bool ultimate_active;
    public GameObject Ultimate;
    //Dash
    [SerializeField] float Dash_Time;
    [SerializeField] float Dash_Speed;
    private float Dash_Timer;
    public string DashAnimName;
    public bool Dashing;
    public bool Dash_Available;
    public float Dash_StartUp;
    private float Dash_StartUp_Timer;
    public bool Dash_On;
    //Melee Dash
    public float dash_cooldown;
    public float dash_cooldown_timer;
    public bool teleport;
    public GameObject Shield;
    private GameObject cur_Shield;
    public float dash_winddown_max;
    private float dash_winddown_timer;
    private bool dash_winddown;
    private Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = Model.GetComponent<Animator>();
        if (animator == null)
        {
            UnityEngine.Debug.LogError("Animator Failed");
        }
        pm = GetComponent<Player_Movement>();
        rb = GetComponent<Rigidbody>();
        pcm = GameObject.FindGameObjectWithTag("Cooldown_Tracker").GetComponent<Player_Cooldown_Master>();
    }
    void FixedUpdate()
    {
        if (pm.Paused == false)
        {
            if (shifting)
            {
                rb.velocity = transform.forward * shift_speed;
                shift_timer += Time.deltaTime;
                if (shift_timer >= shift_timer_max)
                {
                    shift_timer = 0f;
                    shifting = false;
                }
            }
            if (c_shifting)
            {
                rb.velocity = transform.forward * c_shift_speed;
                c_shift_timer += Time.deltaTime;
                if (c_shift_timer >= c_shift_timer_max)
                {
                    c_shift_timer = 0f;
                    c_shifting = false;
                }
            }
            if (Dashing)
            {
                if (Dash_On)
                {
                    Dash_Active();
                }
                else
                {
                    Dash_WindUp();
                }
                Dash_Timer += Time.deltaTime;
                if (Dash_Timer >= Dash_Time)
                {
                    End_Dash();
                }
            }
            if (dash_winddown)
            {
                Dash_Winddown();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (pm.Paused == false)
        {
            //Dash
            if ((Input.GetButtonDown("Jump")) && (Dashing == false) && (pcm.Melee_Dash_Available) && (pm.Locked == false))
            {
                Dash_Start();
                Dashing = true;
                pm.Locked = true;
            }
            //Charge
            if (Input.GetButton("Fire1"))
            {
                print("Charge");
                pm.slowed = true;
                if (charged == false)
                {
                    charge_timer += Time.deltaTime;
                    if (charge_timer >= charge_timer_max)
                    {
                        charge_timer = 0f;
                        charged = true;
                    }
                }
            }
            else
            {
                pm.slowed = false;
            }
            //Ultimate Prep
            if (pcm.Melee_Ultimate_Available)
            {
                if (ultimate_active)
                {
                    Ultimate_Active();
                }
            }
            //Skill Prep
            if (pcm.Melee_Skill_Available)
            {
                if (skill_active)
                {
                    Skill_Active();
                }
            }
            if ((Input.GetButton("Fire3") && (pcm.Melee_Ultimate_Available) && (pm.Locked == false)))
            {
                Ultimate_Start();
            }
            if (Input.GetButton("Fire2"))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Vector3 dir = new Vector3();
                if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                {
                    dir = hit.point;// - transform.position;
                }

            }
            //Skill Fire
            if ((Input.GetButtonUp("Fire2")) && (pcm.Melee_Skill_Available) && (pm.Locked == false))
            {
                print("Fire");
                pm.Locked = true;
                Skill_Start();


            }
            //Charged_Whirlwind
            if ((Input.GetButtonUp("Fire1")) && (firing == false) && (c_firing == false) && (pm.Locked == false))
            {
                Charged_Shot_Start();
            }
            //Standard Punch
            if ((Input.GetButtonDown("Fire1")) && (firing == false) && (c_firing == false) && (pm.Locked == false))
            {
                Basic_Shot_Start();
            }
            if (c_firing == true)
            {
                Charged_Shot_Active();
                timer += Time.deltaTime;
                if (timer >= charge_cooldown)
                {
                    pm.Locked = false;
                    c_firing = false;
                    timer = 0f;
                }
            }
            //basic cooldown
            if (firing == true)
            {
                Basic_Shot_Active();
                timer += Time.deltaTime;
                if (timer >= cooldowns[combo_count])
                {
                    combo_count++;
                    pm.Locked = false;
                    firing = false;
                    timer = 0f;
                    if (combo_count == cooldowns.Length)
                    {
                        combo_count = 0;
                    }
                }
            }
            //end combo
            if ((combo_count > 0) && (firing == false))
            {
                timer += Time.deltaTime;
                if (timer >= reset_cooldown)
                {
                    combo_count = 0;
                    timer = 0f;

                }
            }
        }
    }
    private void Basic_Shot_Start()
    {
        animator.Play(BasicAttackAnimName);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 dir = new Vector3();
        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
        {
            dir = hit.point;// - transform.position;
        }
        Vector3 pos = transform.position - (2 * transform.right);
        if (combo_count % 2 == 0)
        {
            pos += 4 * transform.right;
        }
        GameObject go = Instantiate(Ammo_Type, pos, new Quaternion(0f, 0f, 0f, 1));
        go.transform.LookAt(transform.position);
        //go.transform.localRotation = Quaternion.identity;// = new Quaternion(0f, 0f, 0f, 1);
        go.transform.parent = transform;
        go.GetComponent<Orbiting_Object>().target = this.gameObject;
        if (combo_count%2 == 0)
        {
            go.GetComponent<Orbiting_Object>().speed *= -1.0f;
            
        }
        Vector3 target = new Vector3(dir.x, transform.position.y, dir.z);
        transform.LookAt(target, Vector3.up);
        shifting = true;
        firing = true;
        pm.Locked = true;
        timer = 0f;
        charge_timer = 0f;
    }

    private void Basic_Shot_Active()
    {

    }

    private void Charged_Shot_Start()
    {
        animator.Play(ChargeAttackAnimName);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 dir = new Vector3();
        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
        {
            dir = hit.point;// - transform.position;
        }
        if (charged)
        {
            GameObject go = Instantiate(Charged_Ammo_Type, transform.position, Quaternion.identity);
            go.transform.parent = transform;
            Vector3 target = new Vector3(dir.x, transform.position.y, dir.z);
            transform.LookAt(target, Vector3.up);
            c_shifting = true;
            print("Charge_Shot");
            c_firing = true;
            pm.Locked = true;
            timer = 0f;
            charge_timer = 0f;
            charged = false;
        }
    }
    
    private void Charged_Shot_Active()
    {
        float horiInput = Input.GetAxis("Horizontal");
        float vertInput = Input.GetAxis("Vertical");

        Vector3 vec = new Vector3(horiInput, 0f, vertInput);

        transform.LookAt(transform.position + vec, Vector3.up);
        if (vec.magnitude != 0)
        {
            rb.velocity = transform.forward * charge_move_speed;
        }
        else
        {
            rb.velocity = new Vector3(0f, 0f, 0f);
        }
    }

    private void Dash_Start()
    {
        animator.Play(DashAnimName);
    }
    
    private void Dash_WindUp()
    {
        Dash_StartUp_Timer += Time.deltaTime;
        if (Dash_StartUp_Timer > Dash_StartUp)
        {
            Vector3 pos = transform.position + transform.forward;
            cur_Shield = Instantiate(Shield, pos, transform.rotation);
            cur_Shield.transform.parent = transform;
            Dash_StartUp_Timer = 0f;
            Dash_On = true;
        }
    }

    private void Dash_Active()
    {
        float horiInput = Input.GetAxis("Horizontal");
        float vertInput = Input.GetAxis("Vertical");

        Vector3 vec = new Vector3(horiInput, 0f, vertInput);
        if (vec.magnitude != 0)
        {
            transform.LookAt(transform.position + vec, Vector3.up);
        }
        rb.velocity = transform.forward * Dash_Speed;
    }
    public void End_Dash()
    {
        Destroy(cur_Shield);
        dash_winddown = true;
        Dash_Timer = 0;
        Dash_On = false;
        Dash_Available = false;
        Dashing = false;
        dash_cooldown_timer = 0f;
        pcm.Melee_Dash_Available = false;
        pcm.Melee_Dash_Cooldown_timer = 0f;
        //pm.rb.velocity = new Vector3(0f, 0f, 0f);
    }

    private void Dash_Winddown()
    {
        print("winding");
        dash_winddown_timer += Time.deltaTime;
        if (dash_winddown_timer >= dash_winddown_max)
        {
            dash_winddown_timer = 0f;
            dash_winddown = false;
            pm.Locked = false;
        }
    }

    private void Skill_Start()
    {
        animator.Play(SkillAnimName);
        pm.rb.velocity = new Vector3(0f, 0f, 0f);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 dir = new Vector3();
        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
        {
            dir = hit.point;// - transform.position;
        }
        Vector3 target_pos = new Vector3(dir.x, transform.position.y, dir.z);
        jump_dist = Vector3.Distance(target_pos, transform.position);
        LayerMask lm = LayerMask.GetMask("Wall");
        transform.LookAt(target_pos, Vector3.up);
        RaycastHit rayhit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out rayhit, jump_dist))
        {
            print("ILLEGAL");
            pm.Locked = false;
        }
        else
        {
            skill_active = true;

            float vy = -((-9.81f) * jump_time_max / 2);
            float vx = jump_dist / jump_time_max;

            float normUp = vy / Mathf.Sqrt(Mathf.Pow(vy, 2) + Mathf.Pow(vx, 2));
            float normForward = vx / Mathf.Sqrt(Mathf.Pow(vy, 2) + Mathf.Pow(vx, 2));
            Vector3 forwardVec = transform.forward * vx;
            pm.rb.AddForce((new Vector3(0f, vy, 0f) + forwardVec), ForceMode.VelocityChange);
        }
    }

    private void Skill_Active()
    {
        
        
        skill_duration_timer += Time.deltaTime;
        if (skill_duration_timer >= jump_time_max)
        {
            Instantiate(Landing_Explosion, transform.position, Quaternion.identity);
            skill_available = false;
            skill_active = false;
            skill_duration_timer = 0f;
            pm.Locked = false;
            skill_timer = 0f;
            pcm.Melee_Skill_Available = false;
            pcm.Melee_Skill_Cooldown_timer = 0f;
        }
    }

    private void Ultimate_Start()
    {
        animator.Play(UltimateAnimName);
        ultimate_timer = 0f;
        ultimate_available = false;
        pcm.Melee_Ultimate_Available = false;
        pcm.Melee_Ultimate_Cur_Points = 0f;
        Instantiate(Ultimate, transform.position, Quaternion.identity);
    }

    private void Ultimate_Active()
    {

    }
}
