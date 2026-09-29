using UnityEngine;

/// <summary>
/// Spawns a fireball from the pool. Owner, target and damage all come from the
/// dragon's DragonDamageDealer, so there is ONE authoritative Fly damage value.
/// Put this on the dragon root (or the same object as the relay).
/// </summary>
public class DragonFlyLauncher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DragonFireballProjectilePool projectilePool;
    [SerializeField] private DragonDamageDealer damageDealer;
    [Tooltip("Empty child placed at the dragon's mouth.")]
    [SerializeField] private Transform launchPoint;

    [Header("Safety")]
    [Tooltip("Ignores a second launch inside this window (guards against double-fired animation events).")]
    [SerializeField] private float minLaunchInterval = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private float lastLaunchTime = -999f;

    private void Awake()
    {
        if (damageDealer == null)
            damageDealer = GetComponentInParent<DragonDamageDealer>();
        if (damageDealer == null)
            damageDealer = GetComponentInChildren<DragonDamageDealer>();
    }

    private void Start()
    {
        if (projectilePool == null)
            Debug.LogError($"{name}: DragonFlyLauncher has no projectile pool assigned.", this);
        if (damageDealer == null)
            Debug.LogError($"{name}: DragonFlyLauncher has no DragonDamageDealer.", this);
        if (launchPoint == null)
            Debug.LogWarning($"{name}: No launch point assigned, using a fallback in front of the dragon.", this);
    }

    /// <summary>Called by DragonAnimationEventRelay at the release frame of the Fly animation.</summary>
    public void LaunchFireball()
    {
        if (projectilePool == null || damageDealer == null) return;

        DragonHealth owner = damageDealer.Owner;
        DragonHealth target = damageDealer.Target;

        if (owner == null || target == null)
        {
            Log("Launch rejected - owner or target missing.");
            return;
        }

        if (owner.IsDead || target.IsDead)
        {
            Log("Launch rejected - owner or target is dead.");
            return;
        }

        if (Time.time - lastLaunchTime < minLaunchInterval)
        {
            Log($"Launch rejected - repeat inside {minLaunchInterval}s.");
            return;
        }

        lastLaunchTime = Time.time;

        Vector3 startPos = launchPoint != null
            ? launchPoint.position
            : owner.transform.position + Vector3.up + owner.transform.forward;

        projectilePool.Launch(owner, target, damageDealer.FlyDamage, startPos);

        Log($"{owner.name} launched fireball at {target.name} (damage {damageDealer.FlyDamage}).");
    }

    private void Log(string message)
    {
        if (debugLog) Debug.Log(message, this);
    }

    // Test without animation events: right-click the component header in Play Mode.
    [ContextMenu("Test Launch Fireball")] private void TestLaunch() => LaunchFireball();
}