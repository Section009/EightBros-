using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Cooldown_Master : MonoBehaviour
{
    public GameObject Player;
    [Header("Firework Cooldowns")]
    public float Firework_Skill_Cooldown_Max;
    public float Firework_Skill_Cooldown_timer;
    public float Firework_Dash_Cooldown_Max;
    public float Firework_Dash_Cooldown_timer;
    public float Firework_Ultimate_Cooldown_Max;
    public float Firework_Ultimate_Cooldown_timer;
    [Header("Melee Cooldowns")]
    public float Melee_Skill_Cooldown_Max;
    public float Melee_Skill_Cooldown_timer;
    public float Melee_Dash_Cooldown_Max;
    public float Melee_Dash_Cooldown_timer;
    public float Melee_Ultimate_Cooldown_Max;
    public float Melee_Ultimate_Cooldown_timer;
    [Header("Swap Cooldown")]
    public float Swap_Cooldown_Max;
    public float Swap_Cooldown_timer;

    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}