using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayCooldowns : MonoBehaviour
{
    public Text Skill_Timer, Dash_Timer, Ultimate_Timer, Swap_Timer;
    public Slider Skill_Slider, Dash_Slider, Ultimate_Slider;
    public RawImage Skill_Obj1, Skill_Obj2;
    public RawImage Dash_Obj1, Dash_Obj2;
    public RawImage Ult_Obj1, Ult_Obj2;
    public RawImage Skill_Slider_Color, Dash_Slider_Color, Ultimate_Slider_Color;
    public Texture2D Gong_Skill, Fire_Skill, Gong_Dash, Fire_Dash, Gong_Ult, Fire_Ult;
    public bool testing_mode = false;
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
            Skill_Obj1.texture = Gong_Skill;
            Skill_Obj2.texture = Gong_Skill;
            Dash_Obj1.texture = Gong_Dash;
            Dash_Obj2.texture = Gong_Dash;
            Ult_Obj1.texture = Gong_Ult;
            Ult_Obj2.texture = Gong_Ult;
            if (pcm.Melee_Skill_Available)
            {
                Skill_Slider_Color.color = Color.yellow;
                if (testing_mode)
                {
                    Skill_Timer.text = "Skill Available!(Right Click)";
                    Skill_Timer.color = Color.green;
                }
            }
            else
            {
                Skill_Slider.value = pcm.Melee_Skill_Cooldown_timer;
                Skill_Slider.maxValue = pcm.Melee_Skill_Cooldown_Max;
                Skill_Slider_Color.color = Color.green;
                if (testing_mode)
                {
                    Skill_Timer.text = "Skill Time: " + (pcm.Melee_Skill_Cooldown_Max - pcm.Melee_Skill_Cooldown_timer).ToString("F2");
                    Skill_Timer.color = Color.green;
                }
            }
            if (pcm.Melee_Dash_Available)
            {
                Dash_Slider_Color.color = Color.yellow;
                if (testing_mode)
                {
                    Dash_Timer.text = "Dash Available!(Space)";
                    Dash_Timer.color = Color.yellow;
                }
            }
            else
            {
                Dash_Slider.value = pcm.Melee_Dash_Cooldown_timer;
                Dash_Slider.maxValue = pcm.Melee_Dash_Cooldown_Max;
                Dash_Slider_Color.color = Color.green;
                if (testing_mode)
                {
                    Dash_Timer.text = "Dash Time: " + (pcm.Melee_Dash_Cooldown_Max - pcm.Melee_Dash_Cooldown_timer).ToString("F2");
                    Dash_Timer.color = Color.yellow;
                }
            }
            if (pcm.Melee_Ultimate_Available)
            {
                Ultimate_Slider_Color.color = Color.yellow;
                if (testing_mode)
                {
                    Ultimate_Timer.text = "Ultimate Available!(Center Click)";
                    Ultimate_Timer.color = Color.green;
                }
            }
            else
            {
                Ultimate_Slider.value = pcm.Melee_Ultimate_Cur_Points;
                Ultimate_Slider.maxValue = pcm.Melee_Ultimate_Max_Points;
                Ultimate_Slider_Color.color = Color.green;
                if (testing_mode)
                {
                    Ultimate_Timer.text = "Bounty_Points: " + pcm.Melee_Ultimate_Cur_Points.ToString("F2") + "/" + pcm.Melee_Ultimate_Max_Points.ToString("F2"); ;//+ (pcm.Melee_Ultimate_Cooldown_Max - pcm.Melee_Ultimate_Cooldown_timer).ToString("F2");
                    Ultimate_Timer.color = Color.yellow;
                }                
            }
        }
        
        else
        {
            Skill_Obj1.texture = Fire_Skill;
            Skill_Obj2.texture = Fire_Skill;
            Dash_Obj1.texture = Fire_Dash;
            Dash_Obj2.texture = Fire_Dash;
            Ult_Obj1.texture = Fire_Ult;
            Ult_Obj2.texture = Fire_Ult;
            if (pcm.Firework_Skill_Available)
            {
                Skill_Slider_Color.color = Color.yellow;
                if (testing_mode)
                {
                    Skill_Timer.text = "Skill Available!(Right Click)";
                    Skill_Timer.color = Color.green;
                }
            }
            else
            {
                Skill_Slider.value = pcm.Firework_Skill_Cooldown_timer;
                Skill_Slider.maxValue = pcm.Firework_Skill_Cooldown_Max;
                Skill_Slider_Color.color = Color.red;
                if (testing_mode)
                {
                    Skill_Timer.text = "Skill Time: " + (pcm.Firework_Skill_Cooldown_Max - pcm.Firework_Skill_Cooldown_timer).ToString("F2");
                    Skill_Timer.color = Color.yellow;
                }
            }
            if (pcm.Firework_Dash_Available)
            {
                Dash_Slider_Color.color = Color.yellow;
                if (testing_mode)
                {
                    Dash_Timer.text = "Dash Available!(Space)";
                    Dash_Timer.color = Color.green;
                }
            }
            else
            {
                Dash_Slider.value = pcm.Firework_Dash_Cooldown_timer;
                Dash_Slider.maxValue = pcm.Firework_Dash_Cooldown_Max;
                Dash_Slider_Color.color = Color.red;
                if (testing_mode)
                {
                    Dash_Timer.text = "Dash Time: " + (pcm.Firework_Dash_Cooldown_Max - pcm.Firework_Dash_Cooldown_timer).ToString("F2");
                    Dash_Timer.color = Color.yellow;
                }                
            }
            if (pcm.Firework_Ultimate_Available)
            {
                Ultimate_Slider_Color.color = Color.yellow;
                if (testing_mode)
                {
                    Ultimate_Timer.text = "Ultimate Available!(Center Click)";
                    Ultimate_Timer.color = Color.green;
                }                
            }
            else
            {
                Ultimate_Slider.value = pcm.Firework_Ultimate_Cur_Points;
                Ultimate_Slider.maxValue = pcm.Firework_Ultimate_Max_Points;
                Ultimate_Slider_Color.color = Color.red;
                if (testing_mode)
                {
                    Ultimate_Timer.text = "Bounty_Points: " + pcm.Firework_Ultimate_Cur_Points.ToString("F2") + "/" + pcm.Firework_Ultimate_Max_Points.ToString("F2"); ;//+ (pcm.Melee_Ultimate_Cooldown_Max - pcm.Melee_Ultimate_Cooldown_timer).ToString("F2");
                    Ultimate_Timer.color = Color.yellow;
                }                
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
