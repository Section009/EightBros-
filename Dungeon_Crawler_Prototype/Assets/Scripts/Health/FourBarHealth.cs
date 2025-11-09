using UnityEngine;
using UnityEngine.Events;
using System.Reflection;

[DisallowMultipleComponent]
public class FourHitHealth : MonoBehaviour
{
    [Header("Segments")]
    public int maxSegments = 4;

    private int currentSegments = 4;

    [Header("Hit Settings")]
    public float hitCooldown = 0.1f;
    private float _cooldown;

    [Header("UI Binding")]
    public MonoBehaviour healthBar;   // 只要有 Set(int,int) 即可
    private MethodInfo _setMethod;    // 缓存反射方法，避免每帧反射

    [Header("Events")]
    public UnityEvent onDie;
    public UnityEvent<int, int> onSegmentsChanged; // (current, max)

    void Awake()
    {

        AutoBindHealthBarIfNeeded();


        if (currentSegments < 0 || currentSegments > maxSegments)
            currentSegments = Mathf.Clamp(currentSegments, 0, maxSegments);
        if (currentSegments <= 0) currentSegments = maxSegments;


        RefreshUI();
        onSegmentsChanged?.Invoke(currentSegments, maxSegments);
    }

    void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;
    }

    public void RegisterHit()
    {
        if (_cooldown > 0f || currentSegments <= 0) return;

        currentSegments = Mathf.Max(0, currentSegments - 1);
        RefreshUI();
        onSegmentsChanged?.Invoke(currentSegments, maxSegments);

        _cooldown = hitCooldown;

        if (currentSegments == 0)
        {
            onDie?.Invoke();
            Destroy(gameObject);
        }
    }

    public void HealOne()
    {
        if (currentSegments <= 0) return;
        currentSegments = Mathf.Min(maxSegments, currentSegments + 1);
        RefreshUI();
        onSegmentsChanged?.Invoke(currentSegments, maxSegments);
    }

    public (int current, int max) GetState() => (currentSegments, maxSegments);

    private void AutoBindHealthBarIfNeeded()
    {
        if (healthBar != null)
        {
            _setMethod = GetSetMethod(healthBar);
            if (_setMethod != null) return; 
        }

        var selfCandidates = GetComponents<MonoBehaviour>();
        foreach (var mb in selfCandidates)
        {
            var m = GetSetMethod(mb);
            if (m != null)
            {
                healthBar = mb;
                _setMethod = m;
                return;
            }
        }

        var childCandidates = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in childCandidates)
        {
            var m = GetSetMethod(mb);
            if (m != null)
            {
                healthBar = mb;
                _setMethod = m;
                return;
            }
        }

    }


    private MethodInfo GetSetMethod(MonoBehaviour mb)
    {
        if (mb == null) return null;
        return mb.GetType().GetMethod("Set", BindingFlags.Instance | BindingFlags.Public, null,
                                      new System.Type[] { typeof(int), typeof(int) }, null);
    }


    private void RefreshUI()
    {
        if (healthBar == null || _setMethod == null) return;

        const int maxValue = 100;
        int value = Mathf.RoundToInt((currentSegments / (float)maxSegments) * maxValue);
        _setMethod.Invoke(healthBar, new object[] { value, maxValue });
    }
}
