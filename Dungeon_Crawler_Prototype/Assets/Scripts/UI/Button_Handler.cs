using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Button_Handler : MonoBehaviour
{
    public string NextLevel;
    public void OnStart()
    {
        SceneManager.LoadScene(NextLevel);
    }
    public void OnQuit()
    {
        Application.Quit();
    }
}
