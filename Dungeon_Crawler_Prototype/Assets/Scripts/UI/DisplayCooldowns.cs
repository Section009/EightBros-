using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayCooldowns : MonoBehaviour
{
    public Text Skill_Timer, Dash_Timer, Ultimate_Timer, Swap_Timer;
    // Start is called before the first frame update
    public Player_Cooldown_Master pcm;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (pcm.Melee_Open)
        {
            if (pcm.Melee_Skill_Available)
            {
                Skill_Timer.text = "Skill Available!(Right Click)";
            }
            else
            {
                Skill_Timer.text = "Skill Time: " + (pcm.Melee_Skill_Cooldown_Max - pcm.Melee_Skill_Cooldown_timer).ToString("F2");
            }
            if (pcm.Melee_Dash_Available)
            {
                Dash_Timer.text = "Dash Available!(Space)";
            }
            else
            {
                Dash_Timer.text = "Dash Time: " + (pcm.Melee_Dash_Cooldown_Max - pcm.Melee_Dash_Cooldown_timer).ToString("F2");
            }
            if (pcm.Melee_Ultimate_Available)
            {
                Ultimate_Timer.text = "Ultimate Available!(Center Click)";
            }
            else
            {
                Ultimate_Timer.text = "Ultimate Time: " + (pcm.Melee_Ultimate_Cooldown_Max - pcm.Melee_Ultimate_Cooldown_timer).ToString("F2");
            }
        }
        
        else
        {
            if (pcm.Firework_Skill_Available)
            {
                Skill_Timer.text = "Skill Available!(Right Click)";
            }
            else
            {
                Skill_Timer.text = "Skill Time: " + (pcm.Firework_Skill_Cooldown_Max - pcm.Firework_Skill_Cooldown_timer).ToString("F2");
            }
            if (pcm.Firework_Dash_Available)
            {
                Dash_Timer.text = "Dash Available!(Space)";
            }
            else
            {
                Dash_Timer.text = "Dash Time: " + (pcm.Firework_Dash_Cooldown_Max - pcm.Firework_Dash_Cooldown_timer).ToString("F2");
            }
            if (pcm.Firework_Ultimate_Available)
            {
                Ultimate_Timer.text = "Ultimate Available!(Center Click)";
            }
            else
            {
                Ultimate_Timer.text = "Ultimate Time: " + (pcm.Firework_Ultimate_Cooldown_Max - pcm.Firework_Ultimate_Cooldown_timer).ToString("F2");
            }
        }

        if (pcm.Swap_Available)
        {
            if (pcm.Melee_Open)
            {
                Swap_Timer.text = "Swap to Fireworks with E!";
            }
            else
            {
                Swap_Timer.text = "Swap to Melee with E!";
            }
        }
        else
        {
            Swap_Timer.text = "Time to Next Swap: " + (pcm.Swap_Cooldown_Max - pcm.Swap_Cooldown_timer).ToString("F2");
        }
    }
}
