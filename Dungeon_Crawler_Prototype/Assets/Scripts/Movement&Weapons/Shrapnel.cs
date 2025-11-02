using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shrapnel : MonoBehaviour
{
    public float speed;
    public bool stopped;
    public bool Activated = false;
    public GameObject Explosion;
    public GameObject Explosion_SFX;
    // Start is called before the first frame update
    void Start()
    {
        transform.rotation = Random.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (stopped == false)
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
        else if (Activated)
        {
            GetComponent<Rigidbody>().useGravity = true;
            //transform.position += new Vector3(0f, -1f, 0f) * speed * Time.deltaTime;
        }
    }
    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Floor"))
        {
            Instantiate(Explosion, transform.position, Quaternion.identity);
            Instantiate(Explosion_SFX, transform.position, Quaternion.identity);
            Destroy(this.gameObject);
        }
    }
}
