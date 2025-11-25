using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Projectile : MonoBehaviour
{
    public Player_Cooldown_Master pcm;
    private Player_Movement pm;
    private Rigidbody rb;
    public GameObject Ammo_Type;
    public int combo_count;
    public float[] cooldowns;
    private float timer;
    public float reset_cooldown = 0.5f;
    private bool firing;
    //Basic Shift
    public float shift_speed;
    public float shift_timer_max;
    private float shift_timer;
    private bool shifting;
    //Charge Attack
    private bool c_firing;
    public GameObject Charged_Ammo_Type;
    public float charge_timer_max;
    private float charge_timer;
    private bool charged;
    public float charge_cooldown;
    //Charge Shift
    public float c_shift_speed;
    public float c_shift_timer_max;
    private float c_shift_timer;
    private bool c_shifting;
    //Skill
    public float skill_cooldown;
    public float skill_timer;
    public bool skill_available;
    private bool skill_active;
    //Mage Skill
    public GameObject missile;
    public GameObject big_missile;
    public float skill_duration;
    private float skill_duration_timer;
    public float bullet_cooldown;
    private float bullet_timer;
    //Ultimate
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
    public bool Dashing;
    public bool Dash_Available;
    //Mage Dash
    public float dash_cooldown;
    public float dash_cooldown_timer;
    public bool teleport;
    public GameObject Explosion;
    // Start is called before the first frame update
    void Start()
    {
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
                    Skill_Active();
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
            //Standard Fire
            if ((Input.GetButtonDown("Fire1")) && (firing == false) && (c_firing == false) && (pm.Locked == false))
            {
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

    private void Skill_Start()
    {
        pm.rb.velocity = new Vector3(0f, 0f, 0f);
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
        skill_active = true;
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
        }
    }

    private void Dash_Start()
    {
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
            pcm.Firework_Ultimate_Cooldown_timer = 0f;
        }
    }
    
    private void Set_Player_Visible(bool visible)
    {
        Transform model = transform.GetChild(0);
        foreach (Transform child in model)
        {
            MeshRenderer MR = child.gameObject.GetComponent<MeshRenderer>();
            if (MR != null)
            {
                MR.enabled = visible;
            }
        }
    }
}
