using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orbiting_Object : MonoBehaviour
{
    public GameObject target;
    public GameObject Punch_Model;
    public float speed;
    public float lifetime;
    private float life_timer;
    public int damage;
    public float knockback_time;
    public float knockback_speed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(target.transform.position, new Vector3(0, 1, 0), speed * Time.deltaTime);
        life_timer += Time.deltaTime;
        if (life_timer >= lifetime)
        {
            Destroy(this.gameObject);
        }
    }
    void OnTriggerEnter(Collider col)
    {
        Dummy d = col.gameObject.GetComponent<Dummy>();
        if (d != null)
        {
            print("knocked");
            d.KnockBack(target.transform, knockback_time, knockback_speed);
        }
        if (col.gameObject.CompareTag("Enemy"))
        {
            print("Kill");
        }
    }

    public void Flip()
    {
        //Punch_Model.transform.localPosition = new Vector3(0f, 0f, -4f);
        //Punch_Model.transform.Rotate(180f, 180f, 0f);
    }
}
