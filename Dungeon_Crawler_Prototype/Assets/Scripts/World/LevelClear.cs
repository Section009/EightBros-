using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelClear : MonoBehaviour
{
    
    public int[] sceneIndexes;

    public bool loadOnce = true;

    private bool hasLoaded = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasLoaded && loadOnce) return;

        if (other.CompareTag("Player"))
        {
            if (sceneIndexes.Length == 0)
            {
                Debug.LogWarning("No scene indexes assigned to RandomSceneLoader.");
                return;
            }

            int randomIndex = sceneIndexes[Random.Range(0, sceneIndexes.Length)];
            Debug.Log("Loading random scene index: " + randomIndex);
            SceneManager.LoadScene(randomIndex);

            hasLoaded = true;
        }
    }
}
