using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Projectile : MonoBehaviour
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
    public string[] BasicAttackAnims;
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
    public string Charge_Walk_Anim;
    public float charge_timer_max;
    private float charge_timer;
    public bool charged;
    public float charge_cooldown;
    private Vector3 Charge_Target;
    private bool charge_active;
    public float Charge_StartUp;
    private float Charge_StartUp_Timer;
    public bool Charge_On;
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
    public float Skill_StartUp;
    private float Skill_StartUp_Timer;
    public bool Skill_On;
    //Mage Skill
    public GameObject missile;
    public GameObject big_missile;
    public float skill_duration;
    private float skill_duration_timer;
    public float bullet_cooldown;
    private float bullet_timer;
    //Ultimate
    public string UltimateAnimName;
    public float ultimate_cooldown;
    public float ultimate_timer;
    public bool ultimate_available;
    private bool ultimate_active;
    public float ultimate_duration = 8.0f;
    private float ultimate_duration_timer;
    public GameObject Ultimate;
    //Dash
    [SerializeField] float Dash_Time;
    [SerializeField] float Dash_Speed;
    private float Dash_Timer;
    public string DashAnimName;
    public bool Dashing;
    public bool Dash_Available;
    //Mage Dash
    public float dash_cooldown;
    public float dash_cooldown_timer;
    public bool teleport;
    public GameObject Explosion;
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
                //transform.position += transform.forward * shift_speed * Time.deltaTime;
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
                //transform.position += transform.forward * shift_speed * Time.deltaTime;
                c_shift_timer += Time.deltaTime;
                if (c_shift_timer >= c_shift_timer_max)
                {
                    c_shift_timer = 0f;
                    c_shifting = false;
                }
            }
            if (Dashing)
            {
                Dash_Active();
                Dash_Timer += Time.deltaTime;
                if (Dash_Timer >= Dash_Time)
                {
                    End_Dash();
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (pm.Paused == false)
        {
            //Dash
            if ((Input.GetButtonDown("Jump")) && (Dashing == false) && (pcm.Firework_Dash_Available) && (pm.Locked == false))
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
            if (pcm.Firework_Ultimate_Available)
            {
                if (ultimate_active)
                {
                    Ultimate_Active();
                }
            }
            //Skill Prep
            if (pcm.Firework_Skill_Available)
            {
                if (skill_active)
                {
                    if (Skill_On)
                    {
                        Skill_Active();
                    }
                    else
                    {
                        Skill_WindUp();
                    }
                }
            }
            if ((Input.GetButton("Fire3") && (pcm.Firework_Ultimate_Available) && (pm.Locked == false) && (ultimate_active == false)))
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

                Debug.DrawLine(transform.position, dir);
            }
            //Skill Fire
            if ((Input.GetButtonUp("Fire2")) && (pcm.Firework_Skill_Available) && (pm.Locked == false))
            {
                print("Fire");
                Skill_Start();
                pm.Locked = true;

            }
            //Charge Fire
            if ((Input.GetButtonUp("Fire1")) && (firing == false) && (c_firing == false) && (pm.Locked == false))
            {
                Charged_Shot_Start();
                /*
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
                    Vector3 target = new Vector3(dir.x, transform.position.y, dir.z);
                    transform.LookAt(target, Vector3.up);
                    go.transform.LookAt(target, Vector3.up);
                    c_shifting = true;
                    print("Charge_Shot");
                    c_firing = true;
                    pm.Locked = true;
                    timer = 0f;
                    charge_timer = 0f;
                    charged = false;
                }
                */
            }
            //Standard Fire
            if ((Input.GetButtonDown("Fire1")) && (firing == false) && (c_firing == false) && (pm.Locked == false))
            {
                //animator.Play(BasicAttackAnimName);
                animator.Play(BasicAttackAnims[combo_count]);
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Vector3 dir = new Vector3();
                if (Physics.Raycast(ray, out RaycastHit hit, 30f))
                {
                    dir = hit.point;// - transform.position;
                }
                GameObject go = Instantiate(Ammo_Type, transform.position, Quaternion.identity);
                Vector3 target = new Vector3(dir.x, transform.position.y, dir.z);
                transform.LookAt(target, Vector3.up);
                shifting = true;
                go.transform.LookAt(target, Vector3.up);
                firing = true;
                pm.Locked = true;
                timer = 0f;
                charge_timer = 0f;

            }
            if (c_firing == true)
            {
                if (Charge_On)
                {
                    Charged_Shot_Active();
                    
                }
                else
                {
                    Charged_Shot_Windup();
                }
                
            }
            //basic cooldown
            if (firing == true)
            {
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
    private void Charged_Shot_Start()
    {
        /*
        animator.Play(ChargeAttackAnimName);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 dir = new Vector3();
        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
        {
            dir = hit.point;// - transform.position;
        }
        if (charged)
        {
            Charge_Target = new Vector3(dir.x, transform.position.y, dir.z);
            transform.LookAt(target, Vector3.up);
        }
        */
            
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
                Vector3 target = new Vector3(dir.x, transform.position.y, dir.z);
                transform.LookAt(target, Vector3.up);
                go.transform.LookAt(target, Vector3.up);
                c_shifting = true;
                print("Charge_Shot");
                c_firing = true;
                pm.Locked = true;
                timer = 0f;
                charge_timer = 0f;
                charged = false;
            }
            
        }

    private void Charged_Shot_Windup()
    {
        Charge_StartUp_Timer += Time.deltaTime;
        if (Charge_StartUp_Timer > Charge_StartUp)
        {
            GameObject go = Instantiate(Charged_Ammo_Type, transform.position, Quaternion.identity);
            go.transform.LookAt(Charge_Target, Vector3.up);
            c_shifting = true;
            print("Charge_Shot");
            c_firing = true;
            pm.Locked = true;
            timer = 0f;
            charge_timer = 0f;
            charged = false;
            Charge_On = true;
            Charge_StartUp_Timer = 0f;
            /*
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 dir = new Vector3();
            if (Physics.Raycast(ray, out RaycastHit hit, 30f))
            {
                dir = hit.point;// - transform.position;
            }
            Charge_On = true;
            GameObject go = Instantiate(Charged_Ammo_Type, transform.position, Quaternion.identity);
            go.transform.parent = transform;
            Vector3 target = new Vector3(dir.x, transform.position.y, dir.z);
            transform.LookAt(target, Vector3.up);
            c_shifting = true;
            print("Charge_Shot");
            */
        }
    }

    private void Charged_Shot_Active()
    {
        timer += Time.deltaTime;
        if (timer >= charge_cooldown)
        {
            pm.Locked = false;
            c_firing = false;
            timer = 0f;
            Charge_On = false;
        }
    }

    private void Skill_Start()
    {
        animator.Play(SkillAnimName);
        pm.rb.velocity = new Vector3(0f, 0f, 0f);
        /*
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 dir = new Vector3();
        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
        {
            dir = hit.point;// - transform.position;
        }
        Vector3 target = new Vector3(dir.x, transform.position.y, dir.z);
        GameObject go = Instantiate(big_missile, transform.position, Quaternion.identity);
        GameObject go2 = Instantiate(big_missile, transform.position, Quaternion.identity);
        GameObject go3 = Instantiate(big_missile, transform.position, Quaternion.identity);
        GameObject go4 = Instantiate(big_missile, transform.position, Quaternion.identity);
        go.transform.LookAt(target, Vector3.up);
        go2.transform.LookAt(target, Vector3.up);
        go3.transform.LookAt(target, Vector3.up);
        go4.transform.LookAt(target, Vector3.up);
        go.transform.Rotate(Vector3.right * -120.0f);
        go2.transform.Rotate(Vector3.right * -60.0f);
        go3.transform.Rotate(Vector3.right * 60.0f);
        go4.transform.Rotate(Vector3.right * 120.0f);
        */
        skill_active = true;
    }

    private void Skill_WindUp()
    {
        Skill_StartUp_Timer += Time.deltaTime;
        if (Skill_StartUp_Timer > Skill_StartUp)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 dir = new Vector3();
            if (Physics.Raycast(ray, out RaycastHit hit, 30f))
            {
                dir = hit.point;// - transform.position;
            }
            Vector3 target = new Vector3(dir.x, transform.position.y, dir.z);
            GameObject go = Instantiate(big_missile, transform.position, Quaternion.identity);
            GameObject go2 = Instantiate(big_missile, transform.position, Quaternion.identity);
            GameObject go3 = Instantiate(big_missile, transform.position, Quaternion.identity);
            GameObject go4 = Instantiate(big_missile, transform.position, Quaternion.identity);
            go.transform.LookAt(target, Vector3.up);
            go2.transform.LookAt(target, Vector3.up);
            go3.transform.LookAt(target, Vector3.up);
            go4.transform.LookAt(target, Vector3.up);
            go.transform.Rotate(Vector3.right * -120.0f);
            go2.transform.Rotate(Vector3.right * -60.0f);
            go3.transform.Rotate(Vector3.right * 60.0f);
            go4.transform.Rotate(Vector3.right * 120.0f);
            Skill_StartUp_Timer = 0f;
            Skill_On = true;
        }
    }

    private void Skill_Active()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 dir = new Vector3();
        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
        {
            dir = hit.point;// - transform.position;
        }
        Vector3 target = new Vector3(dir.x, transform.position.y, dir.z);
        transform.LookAt(target, Vector3.up);
        bullet_timer += Time.deltaTime;
        if (bullet_timer >= bullet_cooldown)
        {
            GameObject go = Instantiate(missile, transform.position, Quaternion.identity);
            go.transform.LookAt(target, Vector3.up);
            bullet_timer = 0f;
        }
        skill_duration_timer += Time.deltaTime;
        if (skill_duration_timer >= skill_duration)
        {
            skill_available = false;
            skill_active = false;
            skill_duration_timer = 0f;
            pcm.Firework_Skill_Cooldown_timer = 0f;
            pcm.Firework_Skill_Available = false;
            bullet_timer = 0f;
            pm.Locked = false;
            skill_timer = 0f;
            Skill_On = false;
        }
    }

    private void Dash_Start()
    {
        animator.Play(DashAnimName);
        Set_Player_Visible(false);
    }

    private void Dash_Active()
    {
        rb.velocity = transform.forward * Dash_Speed;
    }
    public void End_Dash()
    {
        Dash_Timer = 0;
        Dash_Available = false;
        dash_cooldown_timer = 0f;
        pcm.Firework_Dash_Cooldown_timer = 0f;
        pcm.Firework_Dash_Available = false;
        Dashing = false;
        pm.Locked = false;
        Set_Player_Visible(true);
        Instantiate(Explosion, transform.position, Quaternion.identity);
    }

    private void Ultimate_Start()
    {
        /*
        ultimate_timer = 0f;
        ultimate_available = false;
        pcm.Firework_Ultimate_Cooldown_timer = 0f;
        pcm.Firework_Ultimate_Available = false;
        */
        animator.Play(UltimateAnimName);
        pm.Speed *= 2;
        ultimate_active = true;
        Instantiate(Ultimate, transform.position, Quaternion.identity);
    }

    private void Ultimate_Active()
    {
        ultimate_duration_timer += Time.deltaTime;
        if (ultimate_duration_timer >= ultimate_duration)
        {
            pm.Speed /= 2;
            ultimate_duration_timer = 0f;
            ultimate_timer = 0f;
            ultimate_available = false;
            pcm.Firework_Ultimate_Available = false;
            pcm.Firework_Ultimate_Cur_Points = 0f;
        }
    }

    private void Set_Player_Visible(bool visible)
    {
        Transform model = transform.GetChild(0);
        Set_Obj_Invisible(model, visible);
    }

    private void Set_Obj_Invisible(Transform comp, bool visible)
    {
        print("Rose");
        foreach (Transform child in comp)
        {
            if (child.childCount > 0)
            {
                Set_Obj_Invisible(child, visible);
            }

            SkinnedMeshRenderer MR = child.gameObject.GetComponent<SkinnedMeshRenderer>();
            if (MR != null)
            {
                MR.enabled = visible;
            }
        }
    }
}
