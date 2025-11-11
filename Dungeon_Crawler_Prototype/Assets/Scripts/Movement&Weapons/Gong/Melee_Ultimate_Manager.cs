using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Melee_Ultimate_Manager : MonoBehaviour
{
    public float TimeToSpawn;
    private float timer;
    public int NumToSpawn;
    private int spawned;
    public GameObject Wave;

    void Start()
    {
        timer = TimeToSpawn;
    }
    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= TimeToSpawn)
        {
            Instantiate(Wave, transform.position, transform.rotation);
            timer = 0f;
            spawned++;
        }
        if (spawned >= NumToSpawn) {
            Destroy(this.gameObject);
        }
    }
}
