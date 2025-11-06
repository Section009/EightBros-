using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swap_Characters : MonoBehaviour
{
    public GameObject Camera;
    public GameObject SwapToPos;
    public GameObject New_Player;
    public GameObject Place_Holder;
    public float swap_timer_max;
    public float swap_timer;
    public bool swap_available;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (swap_available == false)
        {
            swap_timer += Time.deltaTime;
            if (swap_timer >= swap_timer_max)
            {
                swap_available = true;
                swap_timer = 0;
            }
        }
        
        if ((Input.GetKeyDown("e")) &&(swap_available))
        {
            GameObject go = Instantiate(New_Player, SwapToPos.transform.position, SwapToPos.transform.rotation);
            Destroy(SwapToPos);
            Camera.GetComponent<Camera_Follow>().follow = go;
            GameObject temp = Instantiate(Place_Holder, transform.position, transform.rotation);
            go.GetComponent<Swap_Characters>().SwapToPos = temp;
            go.GetComponent<Swap_Characters>().Camera = Camera;
            print("Swap");
            Destroy(this.gameObject);
        }
    }
}
