using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Audio_Obj_Controller : MonoBehaviour
{
    public AudioSource[] Sources;
    private AudioClip[] Clips;
    public float[] Starts;
    private bool[] played;
    private float audio_timer;
    private float audio_timer_max;
    public GameObject nextClip;
    // Start is called before the first frame update
    void Start()
    {
        played = new bool[Sources.Length];
        for (int i = 0; i < Sources.Length; i++)
        {
            float temp = Starts[i] + Sources[i].clip.length;
            if (temp > audio_timer_max)
            {
                audio_timer_max = temp;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        audio_timer += Time.deltaTime;
        for (int i = 0; i < Sources.Length; i++)
        {
            if ((played[i] == false)&&(audio_timer >= Starts[i]))
            {
                Sources[i].Play();
                played[i] = true;
            }
        }
        if (audio_timer >= audio_timer_max)
        {
            if (nextClip != null)
            {
                Instantiate(nextClip, transform.position, Quaternion.identity);

            }
            Destroy(this.gameObject);
        }
    }
}
