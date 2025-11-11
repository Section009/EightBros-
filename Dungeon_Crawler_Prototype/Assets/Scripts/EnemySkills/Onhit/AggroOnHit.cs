using UnityEngine;

[DisallowMultipleComponent]
public class AggroOnProjectileHit : MonoBehaviour
{
    public string projectileTag = "Projectile";

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(projectileTag)) return;
        var ai = GetComponent<EnemyLatchAI>();
        if (ai) ai.NotifyAggro();
    }

    void OnCollisionEnter(Collision c)
    {
        if (!c.collider.CompareTag(projectileTag)) return;
        var ai = GetComponent<EnemyLatchAI>();
        if (ai) ai.NotifyAggro();
    }
}
