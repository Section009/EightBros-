using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Follow : MonoBehaviour
{
    public GameObject follow;
    public Vector3 offset = new Vector3(0f, 10f, -8.5f);
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
