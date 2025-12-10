using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy_Funglimate : MonoBehaviour
{
    // ---------- Forms ----------
    public enum FormType { StunShooter, HeavyShooter, Summoner, SlowShooter }

    [Header("Form / Skin")]
    [Tooltip("Default form picked at spawn.")]
    public FormType defaultForm = FormType.StunShooter;

    [Tooltip("If true, the enemy can switch form during runtime.")]
    public bool allowFormSwitch = false;

    [Tooltip("If switching, choose next form in order (true) or randomly (false).")]
    public bool switchInOrder = true;

    [Tooltip("Min/Max seconds between allowed form switches.")]
    public Vector2 formSwitchIntervalRange = new Vector2(6f, 10f);

    [Tooltip("Assign 4 skins; the script enables only the active one.")]
    public GameObject skin_StunShooter;
    public GameObject skin_HeavyShooter;
    public GameObject skin_Summoner;
    public GameObject skin_SlowShooter;

    [Tooltip("Event fired after a form change (for VFX/SFX hooks).")]
    public UnityEvent<FormType> onFormChanged;

    // ---------- Target & Vision ----------
    [Header("Target & Vision")]
    public string playerTag = "Player";
    public float halfFovDeg = 35f;
    public float visionDistance = 20f;
    public bool requireLineOfSight = true;
    public LayerMask losBlockMask = ~0; // blockers for LOS
    public Transform visionOrigin;      // optional, defaults to self
    public Transform visionForward;     // optional, defaults to self

    [Tooltip("Lose aggro if farther than this and not angry / no memory (0 = never).")]
    public float disengageDistance = 0f;

    [Tooltip("After first attack pause, keep engaging even if FOV is lost.")]
    public bool ignoreVisionAfterFirstPause = true;

    [Tooltip("Keep chasing this long after losing FOV (prevents single hop then idle).")]
    public float visionMemoryTime = 1.0f;
    float visionMemoryTimer;

    // ---------- Hop Motion ----------
    [Header("Hop Motion (approach but stay far for ranged)")]
    public float hopDistance = 4.0f;
    public float hopDuration = 0.45f;
    public float arcHeight = 1.2f;
    public float hopCooldown = 0.2f;

    [Tooltip("Stop approaching around this distance for ranged attacks.")]
    public float stopDistance = 8.0f;

    public float turnSpeed = 720f;

    // ---------- Pause / Attack Window ----------
    [Header("Pause / Attack Window")]
    public Vector2 pauseTimeRange = new Vector2(0.6f, 1.0f);
    public Vector2 betweenAttackDelayRange = new Vector2(0.25f, 0.6f);

    public UnityEvent onPauseBegin;
    public UnityEvent onPauseEnd;

    // ---------- Damage Aggro ----------
    [Header("Damage Aggro")]
    public bool enableDamageAggro = true;
    public float damageAggroTimeout = 6f;
    float aggroTimer;

    // ---------- Projectiles / Ranged ----------
    [Header("Ranged Prefabs (optional; leave empty to use built-in simple projectile)")]
    public GameObject prefab_StunBullet;
    public GameObject prefab_HeavyBullet;
    public GameObject prefab_SlowBullet;

    [Header("Ranged Common")]
    public Transform shootMuzzle;
    public float projectileSpeed = 16f;
    public float projectileLife = 6f;
    public float fireSpreadDeg = 2f;

    [Header("Stun Shooter")]
    public int stunBulletDamage = 5;
    public float stunDuration = 0.8f;

    [Header("Heavy Shooter")]
    public int heavyBulletDamage = 20;

    [Header("Slow Shooter")]
    public int slowBulletDamage = 10;
    [Tooltip("Reserved for future slow API; currently not used.")]
    public float slowMultiplier = 0.5f;
    [Tooltip("Reserved for future slow API; currently not used.")]
    public float slowDuration = 1.5f;

    // ---------- Summoner ----------
    [Header("Summoner")]
    public GameObject summonPrefab; // if null, a minimal JumpMeleeMinion will be auto-added
    public int maxActiveSummons = 3;
    public Vector2 summonIntervalRange = new Vector2(3.0f, 5.0f);
    public float summonRadius = 2.5f;

    // ------------------ Animation_Components ------------------
    [Header("Animation Components")]
    public GameObject Model;
    private Animator animator;
    public string IdleName;
    public string MoveName;
    public string AutumnName;
    public string SpringName;
    public string SummerName;
    public string WinterName;

    // ---------- Internals ----------
    Transform player;
    NavMeshAgent agent;
    bool ignoreVisionPhase;
    bool isHopping;
    bool isPausing;
    bool firstPauseDone;
    float nextAllowFormSwitchTime;

    Health hp;          int lastHP = -1;
    FourHitHealth four; int lastSeg = -1;

    FormType currentForm;
    int formIndexForCycle = 0;
    readonly List<GameObject> _activeSummons = new List<GameObject>();

    enum State { Idle, HopChase, Pause }
    State state;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.autoBraking = false;
        agent.speed = 5.0f;
        agent.stoppingDistance = Mathf.Max(0f, stopDistance - 0.25f);

        if (losBlockMask.value == 0)
        {
            // if user didn't set, exclude self by default
            losBlockMask = ~0;
            losBlockMask &= ~(1 << gameObject.layer);
        }
    }

    void Start()
    {
        animator = Model.GetComponent<Animator>();
        if (animator == null)
        {
            UnityEngine.Debug.LogError("Animator Failed");
        }

        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;

        hp   = GetComponent<Health>();
        four = GetComponent<FourHitHealth>();
        if (hp)   lastHP  = hp.currentHealth;
        if (four) lastSeg = four.GetState().current;

        SetForm(defaultForm);
        if (allowFormSwitch)
            nextAllowFormSwitchTime = Time.time + Random.Range(formSwitchIntervalRange.x, formSwitchIntervalRange.y);

        state = State.Idle;
        StartCoroutine(FSM());
    }

    void OnDestroy()
    {
        // clean summon list
        for (int i = _activeSummons.Count - 1; i >= 0; i--)
        {
            if (_activeSummons[i] == null) continue;
            var hook = _activeSummons[i].GetComponent<SimpleLifeHook>();
            if (hook != null) hook.onDestroyed -= OnSummonedDestroyed;
        }
        _activeSummons.Clear();
    }

    // ================= FSM =================
    IEnumerator FSM()
    {
        while (true)
        {
            if (!player) { yield return null; continue; }

            // aggro from damage
            if (enableDamageAggro)
            {
                if (WasDamagedThisFrame()) aggroTimer = damageAggroTimeout;
                if (aggroTimer > 0f) aggroTimer -= Time.deltaTime;
            }

            bool inVision = IsPlayerInVision();
            float dist = Vector3.Distance(transform.position, player.position);

            if (inVision) visionMemoryTimer = visionMemoryTime;
            else if (visionMemoryTimer > 0f) visionMemoryTimer -= Time.deltaTime;

            bool angry = aggroTimer > 0f;

            if (disengageDistance > 0f && dist > disengageDistance
                && !angry && visionMemoryTimer <= 0f && !ignoreVisionPhase)
            {
                ignoreVisionPhase = false;
                firstPauseDone = false;
                state = State.Idle;
            }

            switch (state)
            {
                case State.Idle:
                    agent.isStopped = true;
                    isHopping = false; isPausing = false;
                    if (inVision || angry || visionMemoryTimer > 0f)
                    {
                        agent.isStopped = false;
                        state = State.HopChase;
                    }
                    break;

                case State.HopChase:
                {
                    bool canChase = ignoreVisionPhase ? true : (inVision || angry || visionMemoryTimer > 0f);
                    if (!canChase)
                    {
                        agent.isStopped = true;
                        state = State.Idle;
                        break;
                    }

                    if (dist <= stopDistance + 0.05f && !isHopping)
                    {
                        state = State.Pause;
                        break;
                    }

                    if (!isHopping)
                    {
                        yield return StartCoroutine(DoOneHop());
                        yield return new WaitForSeconds(hopCooldown);
                    }
                    break;
                }

                case State.Pause:
                    if (!isPausing)
                    {
                        yield return StartCoroutine(DoPauseAndAttackOnce());

                        if (ignoreVisionAfterFirstPause && !firstPauseDone)
                        {
                            ignoreVisionPhase = true;
                            firstPauseDone = true;
                        }
                        state = State.HopChase;
                    }
                    break;
            }

            yield return null;
        }
    }

    // ================= Vision / Hop =================
    bool IsPlayerInVision()
    {
        if (!player) return false;

        Transform o = visionOrigin ? visionOrigin : transform;
        Transform f = visionForward ? visionForward : transform;

        Vector3 to = player.position - o.position;
        float d = to.magnitude;
        if (d > visionDistance) return false;

        Vector3 fwd = f.forward;
        to.y = 0f; fwd.y = 0f;
        if (to.sqrMagnitude < 1e-6f || fwd.sqrMagnitude < 1e-6f) return false;

        float cos = Vector3.Dot(to.normalized, fwd.normalized);
        float limit = Mathf.Cos(halfFovDeg * Mathf.Deg2Rad);
        if (cos < limit) return false;

        if (!requireLineOfSight) return true;

        Vector3 origin = o.position + Vector3.up * 1.4f;
        Vector3 target = player.position + Vector3.up * 1.0f;
        Vector3 dir = (target - origin);
        float maxD = dir.magnitude;
        if (maxD <= 1e-4f) return true;
        dir /= maxD;

        RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxD, losBlockMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider.transform == transform || h.collider.transform.IsChildOf(transform))
                continue;

            if (h.collider.CompareTag(playerTag) || (player && h.collider.transform.IsChildOf(player)))
                return true;

            return false;
        }
        return true;
    }

    IEnumerator DoOneHop()
    {
        isHopping = true;

        Vector3 origin = transform.position;
        Vector3 land = ChooseHopLanding(origin);

        Vector3 planarDir = land - origin; planarDir.y = 0f;
        if (planarDir.sqrMagnitude > 0.0001f)
        {
            Quaternion want = Quaternion.LookRotation(planarDir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
        }

        bool prevStopped = agent.isStopped;
        bool prevUpdPos  = agent.updatePosition;
        bool prevUpdRot  = agent.updateRotation;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, hopDuration);
            float u = Mathf.Clamp01(t);

            Vector3 pos = Vector3.Lerp(origin, land, u);
            float h = 4f * arcHeight * u * (1f - u);
            pos.y = Mathf.Lerp(origin.y, land.y, u) + h;

            transform.position = pos;

            if (planarDir.sqrMagnitude > 0.0001f)
            {
                Quaternion want = Quaternion.LookRotation(planarDir.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
            }

            yield return null;
        }

        agent.updatePosition = prevUpdPos;
        agent.updateRotation = prevUpdRot;
        agent.Warp(transform.position);
        agent.isStopped = prevStopped;

        isHopping = false;
    }

    Vector3 ChooseHopLanding(Vector3 origin)
    {
        if (!player) return origin;

        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(player.position, path);

        Vector3 target = player.position;
        if (path.status == NavMeshPathStatus.PathComplete && path.corners != null && path.corners.Length >= 2)
            target = FirstUsableCorner(path.corners, origin, 0.1f, player.position);

        Vector3 to = target - origin; to.y = 0f;
        float planarDist = to.magnitude;

        float desired = Mathf.Min(hopDistance, Mathf.Max(0f, planarDist - stopDistance));
        Vector3 landPlanar = (planarDist < 0.0001f) ? origin : origin + to.normalized * desired;

        Vector3 land = landPlanar;
        if (NavMesh.SamplePosition(landPlanar, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            land = hit.position;

        // clamp not closer than stopDistance
        Vector3 toPlayer = player.position - land; toPlayer.y = 0f;
        float d = toPlayer.magnitude;
        if (d < stopDistance * 0.9f && toPlayer.sqrMagnitude > 0.0001f)
            land = player.position - toPlayer.normalized * stopDistance;

        return land;
    }

    Vector3 FirstUsableCorner(Vector3[] corners, Vector3 origin, float minGap, Vector3 fallback)
    {
        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 c = corners[i];
            Vector3 oc = new Vector3(c.x, origin.y, c.z);
            if ((oc - origin).magnitude > minGap)
                return c;
        }
        return fallback;
    }

    // ================= Pause & Attack =================
    IEnumerator DoPauseAndAttackOnce()
    {
        isPausing = true;

        bool prevStopped = agent.isStopped;
        agent.isStopped = true;
        agent.ResetPath();

        float pause = Random.Range(pauseTimeRange.x, pauseTimeRange.y);
        onPauseBegin?.Invoke();

        // Face target and perform attack immediately at pause start
        FacePlayerInstant();
        DoAttackByForm();

        // Hold pause window (face the player)
        float t = 0f;
        while (t < pause)
        {
            t += Time.deltaTime;
            FacePlayerSmooth();
            yield return null;
        }

        onPauseEnd?.Invoke();

        // Small delay between attacks (breathing room)
        float gap = Random.Range(betweenAttackDelayRange.x, betweenAttackDelayRange.y);
        float g = 0f;
        while (g < gap) { g += Time.deltaTime; yield return null; }

        agent.isStopped = prevStopped;
        isPausing = false;

        // Optional: form switching
        if (allowFormSwitch && Time.time >= nextAllowFormSwitchTime)
        {
            SwitchFormOnce();
            nextAllowFormSwitchTime = Time.time + Random.Range(formSwitchIntervalRange.x, formSwitchIntervalRange.y);
        }
    }

    void DoAttackByForm()
    {
        switch (currentForm)
        {
            case FormType.StunShooter:
                animator.Play(SpringName);
                FireOneProjectile(prefab_StunBullet, stunBulletDamage, doStun: true);
                break;

            case FormType.HeavyShooter:
                animator.Play(SummerName);
                FireOneProjectile(prefab_HeavyBullet, heavyBulletDamage, doStun: false);
                break;

            case FormType.Summoner:
                animator.Play(AutumnName);
                TrySummonOne();
                break;

            case FormType.SlowShooter:
                animator.Play(WinterName);
                // No slow API yet; damage only for now.
                FireOneProjectile(prefab_SlowBullet, slowBulletDamage, doStun: false);
                break;
        }
    }

    void FireOneProjectile(GameObject prefab, int damage, bool doStun)
    {
        if (!player) return;

        Transform muzzle = shootMuzzle ? shootMuzzle : transform;
        Vector3 spawnPos = muzzle.position;
        Vector3 dir = (player.position + Vector3.up * 1.0f) - spawnPos;
        if (dir.sqrMagnitude < 1e-6f) dir = transform.forward;
        dir.Normalize();

        // small spread
        float yaw = Random.Range(-fireSpreadDeg, fireSpreadDeg);
        float pitch = Random.Range(-fireSpreadDeg, fireSpreadDeg);
        dir = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right) * dir;

        GameObject go = prefab
            ? Instantiate(prefab, spawnPos, Quaternion.LookRotation(dir, Vector3.up))
            : CreateBuiltinProjectile(spawnPos, dir);

        var proj = go.GetComponent<SimpleStatusProjectile>();
        if (proj == null) proj = go.AddComponent<SimpleStatusProjectile>();

        proj.Init(playerTag, damage, projectileSpeed, projectileLife, doStun);
    }

    GameObject CreateBuiltinProjectile(Vector3 pos, Vector3 dir)
    {
        var go = new GameObject("(auto) SimpleProjectile");
        go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir, Vector3.up));
        var col = go.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = 0.2f;
        var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true;
        go.AddComponent<SimpleStatusProjectile>();
        return go;
    }

    void TrySummonOne()
    {
        // prune dead
        for (int i = _activeSummons.Count - 1; i >= 0; i--)
            if (_activeSummons[i] == null) _activeSummons.RemoveAt(i);

        if (_activeSummons.Count >= maxActiveSummons) return;

        Vector3 rnd = Random.insideUnitCircle * summonRadius;
        Vector3 pos = transform.position + new Vector3(rnd.x, 0f, rnd.y);
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            pos = hit.position;

        GameObject s = summonPrefab ? Instantiate(summonPrefab, pos, Quaternion.identity)
                                    : CreateBuiltinMinion(pos);

        _activeSummons.Add(s);
        var hook = s.GetComponent<SimpleLifeHook>(); if (!hook) hook = s.AddComponent<SimpleLifeHook>();
        hook.onDestroyed += OnSummonedDestroyed;

        // ensure basic minion logic exists
        if (!s.GetComponent<NavMeshAgent>()) s.AddComponent<NavMeshAgent>();
        if (!s.GetComponent<JumpMeleeMinion>()) s.AddComponent<JumpMeleeMinion>();

        var jm = s.GetComponent<JumpMeleeMinion>();
        jm.playerTag = playerTag;
        jm.stopDistance = 1.5f;
    }

    GameObject CreateBuiltinMinion(Vector3 pos)
    {
        var go = new GameObject("(auto) JumpMeleeMinion");
        go.transform.position = pos;
        go.AddComponent<NavMeshAgent>();
        go.AddComponent<JumpMeleeMinion>();
        return go;
    }

    void OnSummonedDestroyed(GameObject who) => _activeSummons.Remove(who);

    void FacePlayerInstant()
    {
        if (!player) return;
        Vector3 to = player.position - transform.position; to.y = 0f;
        if (to.sqrMagnitude < 1e-6f) return;
        transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
    }

    void FacePlayerSmooth()
    {
        if (!player) return;
        Vector3 to = player.position - transform.position; to.y = 0f;
        if (to.sqrMagnitude < 1e-6f) return;
        Quaternion want = Quaternion.LookRotation(to.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
    }

    // ---------- Form control ----------
    void SetForm(FormType f)
    {
        currentForm = f;
        ApplySkins(f);
        onFormChanged?.Invoke(f);
    }

    void SwitchFormOnce()
    {
        FormType next = currentForm;
        if (switchInOrder)
        {
            formIndexForCycle = (formIndexForCycle + 1) % 4;
            next = (FormType)formIndexForCycle;
        }
        else
        {
            next = (FormType)Random.Range(0, 4);
        }
        SetForm(next);
    }

    void ApplySkins(FormType f)
    {
        if (skin_StunShooter)  skin_StunShooter.SetActive(f == FormType.StunShooter);
        if (skin_HeavyShooter) skin_HeavyShooter.SetActive(f == FormType.HeavyShooter);
        if (skin_Summoner)     skin_Summoner.SetActive(f == FormType.Summoner);
        if (skin_SlowShooter)  skin_SlowShooter.SetActive(f == FormType.SlowShooter);
    }

    // ---------- Damage wake ----------
    public void NotifyDamaged()
    {
        if (!enableDamageAggro) return;
        aggroTimer = damageAggroTimeout;
        if (state == State.Idle) state = State.HopChase;
    }

    bool WasDamagedThisFrame()
    {
        bool hit = false;
        if (hp != null)
        {
            if (lastHP < 0) lastHP = hp.currentHealth;
            if (hp.currentHealth < lastHP) hit = true;
            lastHP = hp.currentHealth;
        }
        if (four != null)
        {
            int cur = four.GetState().current;
            if (lastSeg < 0) lastSeg = cur;
            if (cur < lastSeg) hit = true;
            lastSeg = cur;
        }
        return hit;
    }

    // ---------- Gizmos ----------
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, visionDistance);

        Transform f = visionForward ? visionForward : transform;
        Vector3 fwd = f.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude > 1e-6f)
        {
            float a = halfFovDeg;
            Quaternion L = Quaternion.AngleAxis(-a, Vector3.up);
            Quaternion R = Quaternion.AngleAxis(a, Vector3.up);
            Vector3 l = L * fwd.normalized * visionDistance;
            Vector3 r = R * fwd.normalized * visionDistance;
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.6f);
            Gizmos.DrawLine(transform.position, transform.position + l);
            Gizmos.DrawLine(transform.position, transform.position + r);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}

// =====================================================================
//  Built-in simple projectile:
//  - straight flight
//  - on hit: deal damage
//  - optional stun: calls Player_Movement.Stun_Player(stunSeconds)
// =====================================================================
public class SimpleStatusProjectile : MonoBehaviour
{
    string targetTag;
    int damage;
    float speed;
    float life;
    bool doStun;
    float stunSeconds;
    Vector3 dir;

    public void Init(string targetTag, int damage, float speed, float life, bool doStun)
    {
        this.targetTag = targetTag;
        this.damage = damage;
        this.speed = Mathf.Max(0f, speed);
        this.life = Mathf.Max(0.01f, life);
        this.doStun = doStun;
        this.stunSeconds = doStun ? Mathf.Max(0.05f, 0.8f) : 0f; // default if doStun=true; tweak if needed

        dir = transform.forward;
        StartCoroutine(Co_Life());
    }

    IEnumerator Co_Life()
    {
        float t = 0f;
        while (t < life)
        {
            transform.position += dir * speed * Time.deltaTime;
            t += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other) return;
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) return;

        GameObject tgt = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

        var hp = tgt.GetComponentInParent<Health>();
        if (hp) hp.TakeDamage(Mathf.Max(1, damage));
        else
        {
            var four = tgt.GetComponentInParent<FourHitHealth>();
            if (four) four.RegisterHit();
        }

        if (doStun && stunSeconds > 0f)
        {
            var pm = tgt.GetComponentInParent<Player_Movement>();
            if (pm) pm.Stun_Player(stunSeconds); // disables movement input during stun. :contentReference[oaicite:1]{index=1}
        }

        Destroy(gameObject);
    }
}

// =====================================================================
//  Minimal jump-melee minion used by Summoner form
// =====================================================================
[RequireComponent(typeof(NavMeshAgent))]
public class JumpMeleeMinion : MonoBehaviour
{
    public string playerTag = "Player";
    public float hopDistance = 3.5f;
    public float hopDuration = 0.35f;
    public float arcHeight = 0.9f;
    public float hopCooldown = 0.15f;
    public float stopDistance = 1.5f;
    public int contactDamage = 8;

    Transform player;
    NavMeshAgent agent;
    bool isHopping;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) player = p.transform;

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.autoBraking = false;
        agent.stoppingDistance = stopDistance;
        agent.speed = 5f;
    }

    void Update()
    {
        if (!player) return;
        float dist = Vector3.Distance(transform.position, player.position);
        if (isHopping) return;

        if (dist > stopDistance + 0.05f)
            StartCoroutine(DoOneHop());
    }

    IEnumerator DoOneHop()
    {
        isHopping = true;
        Vector3 origin = transform.position;
        Vector3 target = player ? player.position : origin;

        Vector3 to = target - origin; to.y = 0f;
        float planar = to.magnitude;
        float desired = Mathf.Min(hopDistance, Mathf.Max(0f, planar - stopDistance));
        Vector3 landPlanar = (planar < 0.001f) ? origin : origin + to.normalized * desired;

        Vector3 land = landPlanar;
        if (NavMesh.SamplePosition(landPlanar, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            land = hit.position;

        bool prevStopped = agent.isStopped; agent.isStopped = true;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, hopDuration);
            float u = Mathf.Clamp01(t);
            Vector3 pos = Vector3.Lerp(origin, land, u);
            float h = 4f * arcHeight * u * (1f - u);
            pos.y = Mathf.Lerp(origin.y, land.y, u) + h;
            transform.position = pos;
            yield return null;
        }

        agent.Warp(transform.position);
        agent.isStopped = prevStopped;
        isHopping = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other) return;
        if (!other.CompareTag(playerTag)) return;

        var hp = other.GetComponentInParent<Health>();
        if (hp) hp.TakeDamage(Mathf.Max(1, contactDamage));
        else
        {
            var four = other.GetComponentInParent<FourHitHealth>();
            if (four) four.RegisterHit();
        }
    }
}

// =====================================================================
//  Simple destroy callback for summoned units
// =====================================================================
public class SimpleLifeHook : MonoBehaviour
{
    public System.Action<GameObject> onDestroyed;
    void OnDestroy() { onDestroyed?.Invoke(gameObject); }
}
