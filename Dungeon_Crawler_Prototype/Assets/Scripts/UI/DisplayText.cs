using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayText : MonoBehaviour
{
    public Text textField;
    // Start is called before the first frame update
    public GameObject player;

    public Weapon_Projectile weaponProjectile; 
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (textField != null && weaponProjectile != null)
        {
            // Example: Display combo count and cooldown
            textField.text =
                "Combo: " + weaponProjectile.combo_count + " Skill Timer: " + weaponProjectile.skill_timer.ToString("F2");
        }
    }
}
