using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class DamageOnHit : MonoBehaviour   // 注意这里是 ":" 不是 "="
{
    [Header("Damage")]
    public int damage = 10;

    [Tooltip("destroyOnHit")]
    public bool destroyOnHit = false;

    [Tooltip("Deal damage over time")]
    public bool damageOverTime = false;

    [Tooltip("Time between each damage tick (Only used if damageOverTime is true)")]
    public float secsPerTick = 0.0f;
    private float timer;

    [Tooltip("tag")]
    public string[] validTags = new string[] { "Enemy" };

    void Reset()
    {
        // 建议子弹用触发器
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        // 如果用刚体推进，保持Kinematic避免物理反弹
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
        timer = 0.0f;
    }

    void OnTriggerStay(Collider other)
    {
        if (damageOverTime)
        {
            timer += Time.deltaTime;
            if (timer >= secsPerTick)
            {
                timer = 0.0f;
                TryDamage(other);
            }
        }
    }

    /*
    void OnCollisionEnter(Collision c)  { TryDamage(c.collider); }
    void OnCollisionStay(Collision c)   { TryDamage(c.collider); }
    */

    private void TryDamage(Collider other)
    {
        if (!TagAllowed(other.tag)) return;

        var h = other.GetComponent<Health>();
        if (h != null)
        {
            h.TakeDamage(damage);
            // Debug.Log($"[DamageOnHit] {name} hit {other.name}, -{damage}");
            if (destroyOnHit) Destroy(gameObject);
        }
    }

    private bool TagAllowed(string tag)
    {
        if (validTags == null || validTags.Length == 0) return true;
        for (int i = 0; i < validTags.Length; i++)
            if (tag == validTags[i]) return true;
        return false;
    }
}
