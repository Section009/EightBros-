using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Homing : MonoBehaviour
{
    public string homing_tag;
    public float turning_speed;
    private GameObject[] targets;
    
    void Start()
    {
        targets = GameObject.FindGameObjectsWithTag(homing_tag);
    }

    // Update is called once per frame
    void Update()
    {
        float dist = float.PositiveInfinity;
        GameObject nearest = null;
        foreach(GameObject t in targets)
        {
            float d = (t.transform.position - transform.position).sqrMagnitude;
            if (d < dist)
            {
                nearest = t;
                dist = d;
            }
        }
        Vector3 dir = nearest.transform.position - transform.position;
        Quaternion toRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, Time.deltaTime * turning_speed);
        //transform.LookAt(nearest.transform.position, Vector3.up);
    }
}
