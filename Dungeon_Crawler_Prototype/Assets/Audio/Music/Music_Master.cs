using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Music_Master : MonoBehaviour
{
    public AudioClip Idle_Track, Battle_Track;
    AudioSource audioSource;
    bool fighting = false;
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void Enter_Battle()
    {
        if (fighting == false)
        {
            audioSource.clip = Battle_Track;
            audioSource.Play();
            fighting = true;
        }
    }

    public void Exit_Battle()
    {
        if (fighting == true) 
        {
            audioSource.clip = Idle_Track;
            audioSource.Play();
            fighting = false;
        }
    }
}
