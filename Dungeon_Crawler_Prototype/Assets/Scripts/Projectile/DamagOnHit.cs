using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class DamageOnHit : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 10;

    public bool destroyOnHit = false;

    [Header("Damage Over Time (DoT)")]
    public bool damageOverTime = false;

    public float secsPerTick = 0.2f;

    [Header("Events")]
    public UnityEvent onHit;

    [Header("Filters")]
    public string[] validTags = new string[] { "Enemy" };

    [Header("Anti-Multi-Hit")]
    public float hitCooldownSeconds = 0.05f;

    private readonly Dictionary<int, float> _nextAllowedTime = new Dictionary<int, float>(); // key: targetID -> next time
    private readonly Dictionary<int, float> _tickTimer = new Dictionary<int, float>();       // key: targetID -> accumulated secs

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void OnTriggerEnter(Collider other)
    {
        TryHitNow(other);
        int key = TargetKey(other);
        if (key != 0) _tickTimer[key] = 0f;
    }

    void OnTriggerStay(Collider other)
    {
        if (!damageOverTime) return;

        int key = TargetKey(other);
        if (key == 0) return;


        if (!_tickTimer.ContainsKey(key)) _tickTimer[key] = 0f;
        _tickTimer[key] += Time.deltaTime;

        if (_tickTimer[key] >= Mathf.Max(0.01f, secsPerTick))
        {
            _tickTimer[key] = 0f;
            TryHitNow(other);
        }
    }

    private void TryHitNow(Collider other)
    {
        if (!IsTagAllowed(other)) return;

        var four = other.GetComponentInParent<FourHitHealth>();
        if (four != null)
        {
            if (!HitCooldownPassed(four)) return;
            four.RegisterHit();
            PostHit();
            return;
        }

        var hp = other.GetComponentInParent<Health>();
        if (hp != null)
        {
            if (!HitCooldownPassed(hp)) return;
            onHit?.Invoke();
            if (damage > 0) hp.TakeDamage(damage);
            PostHit();
            return;
        }

    }

    private void PostHit()
    {
        if (destroyOnHit && !damageOverTime)
        {
            Destroy(gameObject);
        }
    }


    private bool HitCooldownPassed(Component target)
    {
        int id = target.GetInstanceID();
        float now = Time.time;
        if (_nextAllowedTime.TryGetValue(id, out float nextT) && now < nextT)
            return false;

        _nextAllowedTime[id] = now + Mathf.Max(0f, hitCooldownSeconds);
        return true;
    }


    private int TargetKey(Collider other)
    {
        var four = other.GetComponentInParent<FourHitHealth>();
        if (four) return four.GetInstanceID();

        var hp = other.GetComponentInParent<Health>();
        if (hp) return hp.GetInstanceID();

        return other.transform.root ? other.transform.root.GetInstanceID() : 0;
    }


    private bool IsTagAllowed(Collider other)
    {
        if (validTags == null || validTags.Length == 0) return true;

        string t1 = other.tag;
        string t2 = other.transform.root ? other.transform.root.tag : null;

        for (int i = 0; i < validTags.Length; i++)
        {
            string vt = validTags[i];
            if (t1 == vt || t2 == vt) return true;
        }
        return false;
    }
}
