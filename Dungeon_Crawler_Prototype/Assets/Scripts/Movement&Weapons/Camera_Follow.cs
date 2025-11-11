using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Follow : MonoBehaviour
{
    public GameObject follow;
    public Vector3 offset;
    public bool active = true;

    // Update is called once per frame
    void FixedUpdate()
    {
        if (active)
        {
            transform.position = follow.transform.position + offset;
        }
    }
}
