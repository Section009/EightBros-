using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Follow : MonoBehaviour
{
    public GameObject follow;
    public Vector3 offset = new Vector3(0f, 10f, -8.5f);
    public bool active = true;
    public float shake_intensity = 1.0f;
    public bool camera_shaking = false;
    
    void Update()
    {
        /*
        if (Input.GetKeyDown("r"))
        {
            camera_shaking = true;
        }
        */
        if (camera_shaking)
        {
            StartCoroutine(CameraShakeRoutine());
        }
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (active)
        {
            transform.position = follow.transform.position + offset;
        }
    }
    IEnumerator CameraShakeRoutine()
    {
        transform.position = follow.transform.position + offset + (Random.insideUnitSphere * shake_intensity);
        yield return new WaitForSeconds(0.25f);
        transform.position = follow.transform.position + offset;
        camera_shaking = false;
    }
}
