using UnityEngine;

/// <summary>
/// Universal damage-to-aggro bridge for ALL enemies.
/// Attach this to every enemy. It watches Health/FourHitHealth and,
/// when a decrease is detected, calls the enemy's NotifyDamaged/NotifyAggro entry.
/// </summary>
[DisallowMultipleComponent]
public class AggroOnHealthLoss : MonoBehaviour
{
    [Header("Debounce")]
    [Tooltip("Minimum interval between two aggro triggers (sec).")]
    public float minInterval = 0.05f;

    // Health backends
    private Health hp;                    // numeric HP
    private FourHitHealth four;           // 4-hit segmented HP

    // last observed
    private int lastHp = int.MinValue;
    private int lastFour = int.MinValue;

    // cached enemy scripts (any may be null)
    private EnemyAI melee1;
    private Enemy_Funglimate funglimate;
    private EnemyAssassinPouncerAI assassin;

    private float lastTriggerTime = -999f;

    void Awake()
    {
        hp   = GetComponent<Health>();
        four = GetComponent<FourHitHealth>();

        melee1     = GetComponent<EnemyAI>();
        funglimate = GetComponent<Enemy_Funglimate>();
        assassin   = GetComponent<EnemyAssassinPouncerAI>();
    }

    void Start()
    {
        if (hp)   lastHp   = hp.currentHealth;
        if (four) lastFour = four.GetState().current;
    }

    void Update()
    {
        bool tookDamage = false;

        if (hp)
        {
            if (lastHp == int.MinValue) lastHp = hp.currentHealth;
            else if (hp.currentHealth < lastHp) { tookDamage = true; lastHp = hp.currentHealth; }
            else if (hp.currentHealth > lastHp) { lastHp = hp.currentHealth; }
        }

        if (four)
        {
            int cur = four.GetState().current;
            if (lastFour == int.MinValue) lastFour = cur;
            else if (cur < lastFour) { tookDamage = true; lastFour = cur; }
            else if (cur > lastFour) { lastFour = cur; }
        }

        if (tookDamage && Time.time - lastTriggerTime >= minInterval)
        {
            lastTriggerTime = Time.time;

            // fan-out call; only the present component will respond
            if (melee1 != null) melee1.NotifyDamaged();
            if (funglimate != null) funglimate.NotifyDamaged();
            if (assassin != null) assassin.NotifyDamaged();
            // Debug.Log($"[AggroOnHealthLoss] {name} took damage -> aggro");
        }
    }
}
