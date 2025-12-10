using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Music_Master : MonoBehaviour
{
    public AudioClip Idle_Track, Battle_Track;
    public AudioSource source1;
    public AudioSource source2;
    public float Duration = 1f;


    AudioSource curSource;
    AudioSource nextSource;
    bool fighting = false;
    // Start is called before the first frame update
    void Start()
    {
        curSource = source1;
        nextSource = source2;
        nextSource.volume = 0f;
    }

    public void Enter_Battle()
    {
        if (fighting == false)
        {
            StopAllCoroutines();
            StartCoroutine(Fade_In());
            fighting = true;
        }
    }

    public void Exit_Battle()
    {
        if (fighting == true) 
        {
            StopAllCoroutines();
            StartCoroutine(Fade_In());
            fighting = false;
        }
    }

    IEnumerator Fade_In()
    {
        nextSource.Play();

        float timer = 0f;
        while (timer < Duration)
        {
            timer += Time.deltaTime;
            float normalTime = timer / Duration;

            curSource.volume = Mathf.Lerp(1f, 0f, normalTime);
            nextSource.volume = Mathf.Lerp(0f, 1f, normalTime);

            yield return null;
        }

        curSource.volume = 0f;
        nextSource.volume = 1f;

        curSource.Stop();
        AudioSource tmp = curSource;
        curSource = nextSource;
        nextSource = tmp;
    }
}
