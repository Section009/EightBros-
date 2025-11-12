using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayDamage : MonoBehaviour
{
    public float speed;
    public float lifetime;
    private float lifetimer;
    private Text textField;
    private RectTransform RT;
    // Start is called before the first frame update
    void Start()
    {
        textField = GetComponent<Text>();
        textField.CrossFadeAlpha(0f, lifetime, false);
        RT = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        RT.anchoredPosition += new Vector2(0f, speed * Time.deltaTime);
        lifetimer += Time.deltaTime;
        if (lifetime <= lifetimer)
        {
            Destroy(this.gameObject);
        }
    }
}
