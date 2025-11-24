using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Master : MonoBehaviour
{
    public GameObject Standard_UI, Pause_Menu, Game_Over_Menu;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Set_Paused(bool paused)
    {
        Standard_UI.SetActive(!paused);
        Pause_Menu.SetActive(paused);
    }
    
    public void Unpause()
    {
        GameObject player = GameObject.FindWithTag("Player");
        player.GetComponent<Player_Movement>().Unpause();
        Set_Paused(false);
    }
    public void Death_Screen_Activate()
    {
        Standard_UI.SetActive(false);
        Game_Over_Menu.SetActive(true);
    }
}
