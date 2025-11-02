using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shrapnel_Master : MonoBehaviour
{
    public int Sharpnel_Pieces;
    public GameObject Shrapnel;
    private Shrapnel[] pieces;
    public float life_time;
    public float speed;
    public float life_timer;
    private bool stopped;
    private float lowest_y;
    private float highest_y;
    private float cur_y;
    public float y_timer_max;
    private float y_timer;
    // Start is called before the first frame update
    void Start()
    {
        pieces = new Shrapnel[Sharpnel_Pieces];
        for (int i = 0; i < Sharpnel_Pieces; i++)
        {
            GameObject go = Instantiate(Shrapnel, transform.position, Quaternion.identity);
            pieces[i] = go.GetComponent<Shrapnel>();
            pieces[i].speed = speed;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (stopped == false)
        {
            life_timer += Time.deltaTime;
            transform.position += transform.forward * speed * Time.deltaTime;
            if (life_timer >= life_time)
            {
                float lowest = Mathf.Infinity;
                float highest = Mathf.Infinity * -1f;
                foreach(Shrapnel piece in pieces)
                {
                    piece.stopped = true;
                    if (piece.transform.position.y < lowest)
                    {
                        lowest = piece.transform.position.y;
                    }
                    if (piece.transform.position.y > highest)
                    {
                        highest = piece.transform.position.y;
                    }
                }
                lowest_y = lowest;
                highest_y = highest;
                cur_y = lowest_y;
                stopped = true;
            }
        }
        else
        {
            y_timer += Time.deltaTime;
            if (y_timer <= y_timer_max)
            {
                cur_y = Mathf.Lerp(lowest_y, highest_y, y_timer / y_timer_max);
                foreach (Shrapnel piece in pieces)
                {
                    if ((piece.Activated == false)&&(piece.transform.position.y <= cur_y))
                    {
                        piece.Activated = true;
                    }
                }
            }
        }
    }
}
