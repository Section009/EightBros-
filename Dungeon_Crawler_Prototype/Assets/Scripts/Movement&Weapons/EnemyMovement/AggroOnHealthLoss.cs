using UnityEngine;

[DisallowMultipleComponent]
public class AggroOnHealthLoss : MonoBehaviour
{
    [Header("Links")]
    public EnemyLatchAI latchAI;          // 可为空，Start里自动取

    [Header("Debounce")]
    [Tooltip("两次激怒之间的最短间隔（秒）")]
    public float minInterval = 0.05f;

    // 支持这两种生命脚本
    private Health hp;                    // 数值 HP
    private FourHitHealth four;           // 四次命中式

    // 上次观测值
    private int lastHp = int.MinValue;
    private int lastFour = int.MinValue;

    private float lastTriggerTime = -999f;

    void Start()
    {
        if (!latchAI) latchAI = GetComponent<EnemyLatchAI>();

        hp   = GetComponent<Health>();
        four = GetComponent<FourHitHealth>();

        if (hp)   lastHp   = hp.currentHealth;
        if (four) lastFour = four.GetState().current;
    }

    void Update()
    {
        bool lost = false;

        if (hp)
        {
            if (lastHp == int.MinValue) lastHp = hp.currentHealth;
            else if (hp.currentHealth < lastHp) { lost = true; lastHp = hp.currentHealth; }
            else if (hp.currentHealth > lastHp) { lastHp = hp.currentHealth; }
        }

        if (four)
        {
            var cur = four.GetState().current;
            if (lastFour == int.MinValue) lastFour = cur;
            else if (cur < lastFour) { lost = true; lastFour = cur; }
            else if (cur > lastFour) { lastFour = cur; }
        }

        if (lost && Time.time - lastTriggerTime >= minInterval)
        {
            lastTriggerTime = Time.time;
            if (latchAI) latchAI.NotifyAggro();
            // Debug.Log($"[AggroOnHealthLoss] {name} took damage -> aggro");
        }
    }
}
