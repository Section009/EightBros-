using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Follow : MonoBehaviour
{
    public GameObject follow;
    public Vector3 offset;

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = follow.transform.position + offset;
    }
}
