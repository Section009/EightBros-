using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Slider slider;
    public Vector3 worldOffset = new Vector3(0, 2.0f, 0); // 头顶高度
    private Transform target;
    private Camera cam;

    void Start()
    {
        target = transform.parent; // 把血条作为目标对象的子物体
        cam = Camera.main;
    }

    public void Set(int hp, int max)
    {
        if (!slider) return;
        slider.maxValue = max;
        slider.value = hp;
    }

    void LateUpdate()
    {
        if (!target || !cam) return;
        // 让血条跟随 & 朝向镜头
        transform.position = target.position + worldOffset;
        transform.forward = cam.transform.forward;
    }
}
