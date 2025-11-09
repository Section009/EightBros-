using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Cooldown_Master : MonoBehaviour
{
    public GameObject Player;
    [Header("Firework Cooldowns")]
    public bool Firework_Skill_Available;
    public float Firework_Skill_Cooldown_Max;
    public float Firework_Skill_Cooldown_timer;
    public bool Firework_Dash_Available;
    public float Firework_Dash_Cooldown_Max;
    public float Firework_Dash_Cooldown_timer;
    public bool Firework_Ultimate_Available;
    public float Firework_Ultimate_Cooldown_Max;
    public float Firework_Ultimate_Cooldown_timer;
    [Header("Melee Cooldowns")]
    public bool Melee_Skill_Available;
    public float Melee_Skill_Cooldown_Max;
    public float Melee_Skill_Cooldown_timer;
    public bool Melee_Dash_Available;
    public float Melee_Dash_Cooldown_Max;
    public float Melee_Dash_Cooldown_timer;
    public bool Melee_Ultimate_Available;
    public float Melee_Ultimate_Cooldown_Max;
    public float Melee_Ultimate_Cooldown_timer;
    [Header("Swap Cooldown")]
    public bool Swap_Available;
    public float Swap_Cooldown_Max;
    public float Swap_Cooldown_timer;

    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
    }
    // Update cooldowns of all weapon abilities
    void Update()
    {
        //Firework cooldowns
        if (Firework_Skill_Available == false)
        {
            Firework_Skill_Cooldown_timer += Time.deltaTime;
            if (Firework_Skill_Cooldown_timer >= Firework_Skill_Cooldown_Max)
            {
                Firework_Skill_Available = true;
            }
        }
        if (Firework_Dash_Available == false)
        {
            Firework_Dash_Cooldown_timer += Time.deltaTime;
            if (Firework_Dash_Cooldown_timer >= Firework_Dash_Cooldown_Max)
            {
                Firework_Dash_Available = true;
            }
        }
        if (Firework_Ultimate_Available == false)
        {
            Firework_Ultimate_Cooldown_timer += Time.deltaTime;
            if (Firework_Ultimate_Cooldown_timer >= Firework_Ultimate_Cooldown_Max)
            {
                Firework_Ultimate_Available = true;
            }
        }

        //Melee cooldowns
        if (Melee_Skill_Available == false)
        {
            Melee_Skill_Cooldown_timer += Time.deltaTime;
            if (Melee_Skill_Cooldown_timer >= Melee_Skill_Cooldown_Max)
            {
                Melee_Skill_Available = true;
            }
        }
        if (Melee_Dash_Available == false)
        {
            Melee_Dash_Cooldown_timer += Time.deltaTime;
            if (Melee_Dash_Cooldown_timer >= Melee_Dash_Cooldown_Max)
            {
                Melee_Dash_Available = true;
            }
        }
        if (Melee_Ultimate_Available == false)
        {
            Melee_Ultimate_Cooldown_timer += Time.deltaTime;
            if (Melee_Ultimate_Cooldown_timer >= Melee_Ultimate_Cooldown_Max)
            {
                Firework_Ultimate_Available = true;
            }
        }
    }
}