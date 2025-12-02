using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveClear : MonoBehaviour
{

    [Tooltip("Tag used to identify enemies in the scene.")]
    public string enemyTag = "Enemy";

    [Tooltip("How often (in seconds) to check for enemies.")]
    public float checkInterval = 0.5f;

    private float timer;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            CheckEnemies();
        }
    }

    void CheckEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        if (enemies.Length == 0)
        {
            //Help with dynamic music
            GameObject Music = GameObject.FindWithTag("Music_Master");
            if (Music != null) Music.GetComponent<Music_Master>().Exit_Battle();

            Debug.Log("All enemies destroyed! Wall disappearing...");
            gameObject.SetActive(false); // disable the wall
        }
    }
}
