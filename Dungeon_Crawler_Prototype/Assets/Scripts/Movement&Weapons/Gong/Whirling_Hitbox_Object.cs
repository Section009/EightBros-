using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Whirling_Hitbox_Object : MonoBehaviour
{
    public float timer_max;
    private float timer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > timer_max) {
            Destroy(this.gameObject);
        }
    }
}
