using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EncounterTrigger : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public Transform[] enemySpawnPoints;

    [Header("Door Settings")]
    public GameObject doorPrefab;
    public Transform doorSpawnPoint;

    [Header("Optional Settings")]
    public string enemyTag = "Enemy";   // tag applied to spawned enemies
    public bool triggerOnce = true;     // prevents re-triggering

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered && triggerOnce) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        StartEncounter();
    }

    void StartEncounter()
    {
        // --- 1. Spawn enemies first ---
        foreach (Transform point in enemySpawnPoints)
        {
            if (enemyPrefab)
            {
                GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);
                enemy.tag = enemyTag;
            }
        }

        // --- 2. Spawn the door next ---
        GameObject newDoor = null;
        if (doorPrefab && doorSpawnPoint)
        {
            newDoor = Instantiate(doorPrefab, doorSpawnPoint.position, doorSpawnPoint.rotation);
        }

        // --- 3. Make the door disappear when enemies are gone ---
        if (newDoor)
        {
            var disappearScript = newDoor.AddComponent<EnemyWaveClear>();
            disappearScript.enemyTag = enemyTag;
        }

        Debug.Log("Encounter started! Enemies spawned, door closed.");
    }
}
