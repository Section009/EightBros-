using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Audio_Obj_Controller : MonoBehaviour
{
    private AudioSource m_AudioSource;
    private AudioClip m_AudioClip;
    private float audio_timer;
    public GameObject nextClip;
    // Start is called before the first frame update
    void Start()
    {
        m_AudioSource = GetComponent<AudioSource>();
        m_AudioClip = m_AudioSource.clip;
        audio_timer = m_AudioClip.length;
    }

    // Update is called once per frame
    void Update()
    {
        audio_timer -= Time.deltaTime;
        if (audio_timer < 0)
        {
            if (nextClip != null)
            {
                Instantiate(nextClip, transform.position, Quaternion.identity);

            }
            Destroy(this.gameObject);
        }
    }
}
