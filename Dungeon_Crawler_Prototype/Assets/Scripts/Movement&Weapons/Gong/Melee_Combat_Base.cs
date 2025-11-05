using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Melee_Combat_Base : MonoBehaviour
{
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
    public float charge_move_speed;
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
    public float bullet_cooldown;
    private float bullet_timer;
    //Ultimate
    public float ultimate_cooldown;
    public float ultimate_timer;
    public bool ultimate_available;
    private bool ultimate_active;
    public GameObject Ultimate;
    //Dash
    [SerializeField] float Dash_Time;
    [SerializeField] float Dash_Speed;
    private float Dash_Timer;
    public bool Dashing;
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
    }
    void FixedUpdate()
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
            rb.velocity = transform.forward * Dash_Speed;
            Dash_Timer += Time.deltaTime;
            if (Dash_Timer >= Dash_Time)
            {
                Dash_Timer = 0;
                Dashing = false;
                pm.Locked = false;
                GetComponent<MeshRenderer>().enabled = true;
                Instantiate(Explosion, transform.position, Quaternion.identity);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Dash
        if ((Input.GetButtonDown("Jump")) && (Dashing == false))
        {
            print("Dash");
            Dashing = true;
            pm.Locked = true;
            GetComponent<MeshRenderer>().enabled = false;
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
        if (ultimate_available)
        {
            if (ultimate_active)
            {
                Ultimate_Active();
            }
        }
        else
        {
            ultimate_timer += Time.deltaTime;
            if (ultimate_timer >= ultimate_cooldown)
            {
                ultimate_timer = 0f;
                ultimate_available = true;
            }
        }
        //Skill Prep
        if (skill_available)
        {
            if (skill_active)
            {
                Skill_Active();
            }
        }
        else
        {
            skill_timer += Time.deltaTime;
            if (skill_timer >= skill_cooldown)
            {
                skill_timer = 0f;
                skill_available = true;
            }
        }
        if ((Input.GetButton("Fire3") && (ultimate_available) && (pm.Locked == false)))
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
        if ((Input.GetButtonUp("Fire2")) && (skill_available) && (pm.Locked == false))
        {
            print("Fire");
            Skill_Start();
            pm.Locked = true;

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
    private void Basic_Shot_Start()
    {
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
            GameObject go = Instantiate(Ammo_Type, pos, Quaternion.identity);
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
        skill_timer = 0f;
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
        skill_timer += Time.deltaTime;
        if (skill_timer >= skill_duration)
        {
            skill_available = false;
            skill_active = false;
            skill_timer = 0f;
            bullet_timer = 0f;
            pm.Locked = false;
        }
    }

    private void Ultimate_Start()
    {
        ultimate_timer = 0f;
        ultimate_available = false;
        Instantiate(Ultimate, transform.position, Quaternion.identity);
    }

    private void Ultimate_Active()
    {

    }
}
