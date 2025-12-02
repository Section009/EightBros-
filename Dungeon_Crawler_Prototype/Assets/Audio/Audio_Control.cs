using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Audio_Control : MonoBehaviour
{
    public AudioMixer mixer;
    public Slider masterSlider;
    public Slider sfxSlider;
    public Slider musicSlider;
    public bool Simple = false;
    // Start is called before the first frame update
    void Start()
    {
        // do we have saved volume player prefs?
        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            print("Accessed");
            // set the mixer volume levels based on the saved player prefs
            mixer.SetFloat("MasterVolume", PlayerPrefs.GetFloat("MasterVolume"));
            mixer.SetFloat("SFXVolume", PlayerPrefs.GetFloat("SFXVolume"));
            mixer.SetFloat("MusicVolume", PlayerPrefs.GetFloat("MusicVolume"));
            SetSliders();
        }
        // otherwise just set the sliders
        else
        {
            SetSliders();
        }
    }

    void SetSliders()
    {
        if (Simple == false)
        {
            masterSlider.value = PlayerPrefs.GetFloat("MasterVolume");
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume");
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume");
        }
    }

    public void UpdateMasterVolume()
    {
        mixer.SetFloat("MasterVolume", masterSlider.value);
        PlayerPrefs.SetFloat("MasterVolume", masterSlider.value);
    }
    // called when we update the sfx slider
    public void UpdateSFXVolume()
    {
        mixer.SetFloat("SFXVolume", sfxSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);
    }
    // called when we update the music slider
    public void UpdateMusicVolume()
    {
        mixer.SetFloat("MusicVolume", musicSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
    }
}
