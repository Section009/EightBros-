using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Dialogue_Master : MonoBehaviour
{
    public string[] dialogue;
    public Text Display_Text;
    private int cur_num = 0;
    public string NextLevel;
    // Start is called before the first frame update
    void Start()
    {
        Display_Text.text = dialogue[0];
    }

    // Update is called once per frame
    public void Advance()
    {
        cur_num++;
        if (cur_num >= dialogue.Length)
        {
            SceneManager.LoadScene(NextLevel);
        }
        else
        {
            Display_Text.text = dialogue[cur_num];
        }
    }
}
