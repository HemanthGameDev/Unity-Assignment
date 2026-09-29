using System;
using UnityEngine;

/// <summary>
/// Pooled, target-tracking fireball. Damage is applied exactly once,
/// when the fireball reaches the target.
/// </summary>
public class DragonFireballProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float hitRadius = 0.6f;
    [Tooltip("Height above the target's pivot the fireball aims at (chest height).")]
    [SerializeField] private float aimHeight = 1f;
    [SerializeField] private bool faceMovementDirection = true;
    [SerializeField] private float maxLifetime = 5f;

    [Header("Visuals (auto-collected if empty)")]
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private TrailRenderer[] trails;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    private DragonImpactVfxPool impactVfxPool;
    private Action<DragonFireballProjectile> releaseAction;

    private DragonHealth owner;
    private DragonHealth target;
    private float damage;
    private bool isFlying;
    private bool hasImpacted;
    private bool inPool = true;
    private float expireTime;
    private bool cached;

    /// <summary>Called once by the pool when this object is created.</summary>
    public void Setup(DragonImpactVfxPool impactPool, Action<DragonFireballProjectile> release)
    {
        impactVfxPool = impactPool;
        releaseAction = release;
    }

    private void CacheComponents()
    {
        if (cached) return;
        cached = true;
        if (particleSystems == null || particleSystems.Length == 0)
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        if (trails == null || trails.Length == 0)
            trails = GetComponentsInChildren<TrailRenderer>(true);
    }

    /// <summary>Resets ALL state and starts the flight. Called by the pool each time it is reused.</summary>
    public void Launch(DragonHealth newOwner, DragonHealth newTarget, float newDamage, Vector3 startPosition)
    {
        CacheComponents();
        inPool = false;

        if (newTarget == null || newTarget == newOwner)
        {
            if (debugLogs) Debug.LogWarning("[Fireball] Invalid target, recycling.", this);
            Recycle();
            return;
        }

        owner = newOwner;
        target = newTarget;
        damage = newDamage;
        hasImpacted = false;
        expireTime = Time.time + maxLifetime;

        transform.position = startPosition;
        Vector3 dir = GetAimPoint() - startPosition;
        transform.rotation = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : Quaternion.identity;

        gameObject.SetActive(true);

        for (int i = 0; i < trails.Length; i++)
            if (trails[i] != null) trails[i].Clear();
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null) continue;
            particleSystems[i].Clear(true);
            particleSystems[i].Play(true);
        }

        isFlying = true;

        if (debugLogs) Debug.Log($"[Fireball] Launched at {target.name}, damage {damage}", this);
    }

    private Vector3 GetAimPoint()
    {
        return target.transform.position + Vector3.up * aimHeight;
    }

    private void Update()
    {
        if (!isFlying) return;

        if (Time.time >= expireTime)
        {
            if (debugLogs) Debug.Log("[Fireball] Expired.", this);
            Recycle();
            return;
        }

        // Target destroyed or already dead: fizzle at the current position, no damage.
        if (target == null || target.IsDead)
        {
            Impact(false);
            return;
        }

        Vector3 aim = GetAimPoint();
        Vector3 oldPos = transform.position;
        Vector3 newPos = Vector3.MoveTowards(oldPos, aim, speed * Time.deltaTime);
        transform.position = newPos;

        if (faceMovementDirection)
        {
            Vector3 dir = newPos - oldPos;
            if (dir.sqrMagnitude > 0.000001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        if ((aim - newPos).sqrMagnitude <= hitRadius * hitRadius)
            Impact(true);
    }

    private void Impact(bool applyDamage)
    {
        if (hasImpacted) return;
        hasImpacted = true;
        isFlying = false;

        float groundY = transform.position.y - aimHeight;
        if (target != null) groundY = target.transform.position.y;

        if (applyDamage && target != null && !target.IsDead)
        {
            if (debugLogs) Debug.Log($"[Fireball] Hit {target.name} for {damage}", this);
            target.TakeDamage(damage);
        }

        if (impactVfxPool != null)
            impactVfxPool.SpawnImpact(transform.position, groundY);

        Recycle();
    }

    private void Recycle()
    {
        if (inPool) return;
        inPool = true;
        isFlying = false;

        CacheComponents();
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null) continue;
            particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        owner = null;
        target = null;

        gameObject.SetActive(false);
        releaseAction?.Invoke(this);
    }

    private void OnDisable()
    {
        isFlying = false;
    }
}