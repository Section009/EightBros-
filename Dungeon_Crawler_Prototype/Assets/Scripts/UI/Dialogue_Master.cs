using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Dialogue_Master : MonoBehaviour
{
    public string[] dialogue;
    public string[] speaker;
    public Text Display_Name;
    public Text Display_Text;
    private int cur_num = 0;
    public string NextLevel;
    public UnityEvent[] Dialogue_Events;
    // Start is called before the first frame update
    void Start()
    {
        Display_Text.text = dialogue[0];
        Display_Name.text = speaker[0];
        Dialogue_Events[0]?.Invoke();
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
            Display_Name.text = speaker[cur_num];
            Dialogue_Events[cur_num]?.Invoke();
        }
    }
}
