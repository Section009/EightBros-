using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Projectile : MonoBehaviour
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
    //Charge Shift
    public float c_shift_speed;
    public float c_shift_timer_max;
    private float c_shift_timer;
    private bool c_shifting;
    //Skill
    public float skill_cooldown;
    public float skill_timer;
    private bool skill_available;
    private bool skill_active;
    //Mage Skill
    public GameObject missile;
    public float skill_duration;
    public float bullet_cooldown;
    private float bullet_timer;
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
    }

    // Update is called once per frame
    void Update()
    {
        //Charge
        if (Input.GetButton("Fire1"))
        {
            print("Charge");
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
        //Standard Fire
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
            else
            {
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

    private void Skill_Start()
    {
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
            GameObject go = Instantiate(Ammo_Type, transform.position, Quaternion.identity);
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
}
