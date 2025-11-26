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
                Skill_Timer.color = Color.green;
            }
            else
            {
                Skill_Timer.text = "Skill Time: " + (pcm.Melee_Skill_Cooldown_Max - pcm.Melee_Skill_Cooldown_timer).ToString("F2");
                Skill_Timer.color = Color.yellow;
            }
            if (pcm.Melee_Dash_Available)
            {
                Dash_Timer.text = "Dash Available!(Space)";
                Dash_Timer.color = Color.green;
            }
            else
            {
                Dash_Timer.text = "Dash Time: " + (pcm.Melee_Dash_Cooldown_Max - pcm.Melee_Dash_Cooldown_timer).ToString("F2");
                Dash_Timer.color = Color.yellow;
            }
            if (pcm.Melee_Ultimate_Available)
            {
                Ultimate_Timer.text = "Ultimate Available!(Center Click)";
                Ultimate_Timer.color = Color.green;
            }
            else
            {
                Ultimate_Timer.text = "Bounty_Points: " + pcm.Melee_Ultimate_Cur_Points.ToString("F2") + "/" + pcm.Melee_Ultimate_Max_Points.ToString("F2"); ;//+ (pcm.Melee_Ultimate_Cooldown_Max - pcm.Melee_Ultimate_Cooldown_timer).ToString("F2");
                Ultimate_Timer.color = Color.yellow;
            }
        }
        
        else
        {
            if (pcm.Firework_Skill_Available)
            {
                Skill_Timer.text = "Skill Available!(Right Click)";
                Skill_Timer.color = Color.green;
            }
            else
            {
                Skill_Timer.text = "Skill Time: " + (pcm.Firework_Skill_Cooldown_Max - pcm.Firework_Skill_Cooldown_timer).ToString("F2");
                Skill_Timer.color = Color.yellow;
            }
            if (pcm.Firework_Dash_Available)
            {
                Dash_Timer.text = "Dash Available!(Space)";
                Dash_Timer.color = Color.green;
            }
            else
            {
                Dash_Timer.text = "Dash Time: " + (pcm.Firework_Dash_Cooldown_Max - pcm.Firework_Dash_Cooldown_timer).ToString("F2");
                Dash_Timer.color = Color.yellow;
            }
            if (pcm.Firework_Ultimate_Available)
            {
                Ultimate_Timer.text = "Ultimate Available!(Center Click)";
                Ultimate_Timer.color = Color.green;
            }
            else
            {
                Ultimate_Timer.text = "Ultimate Time: " + (pcm.Firework_Ultimate_Cooldown_Max - pcm.Firework_Ultimate_Cooldown_timer).ToString("F2");
                Ultimate_Timer.color = Color.yellow;
            }
        }

        if (pcm.Swap_Available)
        {
            Swap_Timer.color = Color.green;
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
            Swap_Timer.color = Color.yellow;
        }
    }
}
