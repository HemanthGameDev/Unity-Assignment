using System;
using UnityEngine;

public class DragonDamageDealer : MonoBehaviour
{
    [Serializable]
    public class AttackSettings
    {
        public float damage = 20f;

        [Tooltip("Max flat (XZ) distance to the target, centre to centre.")]
        public float range = 5f;

        [Tooltip("Total cone angle in front of the dragon. 360 = all around.")]
        [Range(1f, 360f)] public float angle = 90f;
    }

    [Header("References")]
    [SerializeField] private DragonHealth owner;
    [SerializeField] private DragonHealth target;

    [Header("Attacks")]
    [SerializeField]
    private AttackSettings fire =
        new AttackSettings { damage = 20f, range = 8f, angle = 60f };

    [SerializeField]
    private AttackSettings tail =
        new AttackSettings { damage = 30f, range = 3.5f, angle = 360f };

    [SerializeField]
    private AttackSettings fly =
        new AttackSettings { damage = 40f, range = 4f, angle = 360f };

    [Header("Safety")]
    [Tooltip("Ignores a repeat of the same attack's hit inside this window (guards against double-fired events).")]
    [SerializeField] private float minRepeatInterval = 0.3f;

    [Header("Debug")]
    [Tooltip("Logs why each Animation Event hit landed or was rejected. Turn off when stable.")]
    [SerializeField] private bool debugLog = true;

    private readonly float[] lastHitTime = { -999f, -999f, -999f };
    private static readonly string[] attackNames = { "Fire", "Tail", "Fly" };

    // ---- Read-only access (used by DragonFlyLauncher) ----

    public DragonHealth Owner => owner;
    public DragonHealth Target => target;

    /// <summary>
    /// The single authoritative Fly damage value.
    /// Applied by the fireball on impact.
    /// </summary>
    public float FlyDamage => fly.damage;

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<DragonHealth>();
    }

    private void Start()
    {
        if (owner == null)
        {
            Debug.LogError(
                $"{name}: DragonDamageDealer has no owner DragonHealth.",
                this
            );
        }

        if (target == null)
        {
            Debug.LogError(
                $"{name}: DragonDamageDealer has no target DragonHealth assigned.",
                this
            );
        }
    }

    // =========================================================
    // ANIMATION EVENT ENTRY POINTS
    // =========================================================

    public void HitFire()
    {
        TryHit(fire, 0);
    }

    public void HitTail()
    {
        TryHit(tail, 1);
    }

    // IMPORTANT:
    // Fly damage is handled by DragonFireballProjectile on impact.
    // Do NOT put HitFly on the Fly animation.
    public void HitFly()
    {
        TryHit(fly, 2);
    }

    // =========================================================
    // HIT LOGIC
    // =========================================================

    private void TryHit(AttackSettings attack, int index)
    {
        string label = $"{owner?.name ?? name} {attackNames[index]}";

        // -----------------------------------------------------
        // BASIC VALIDATION
        // -----------------------------------------------------

        if (owner == null || target == null)
        {
            Log($"{label}: rejected - owner or target missing.");
            return;
        }

        if (owner.IsDead || target.IsDead)
        {
            Log($"{label}: rejected - owner or target is dead.");
            return;
        }

        // -----------------------------------------------------
        // REPEAT HIT PROTECTION
        // -----------------------------------------------------

        if (Time.time - lastHitTime[index] < minRepeatInterval)
        {
            Log(
                $"{label}: rejected - repeat inside {minRepeatInterval}s."
            );
            return;
        }

        // -----------------------------------------------------
        // DISTANCE CHECK
        // -----------------------------------------------------

        Vector3 toTarget =
            target.transform.position -
            owner.transform.position;

        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        if (distance > attack.range)
        {
            Log(
                $"{label}: MISS - distance {distance:F2} > " +
                $"range {attack.range:F2}."
            );

            return;
        }

        // -----------------------------------------------------
        // ANGLE CHECK
        // -----------------------------------------------------

        if (attack.angle < 360f &&
            toTarget.sqrMagnitude > 0.0001f)
        {
            Vector3 forward = owner.transform.forward;
            forward.y = 0f;

            float angleToTarget =
                Vector3.Angle(forward, toTarget);

            if (angleToTarget > attack.angle * 0.5f)
            {
                Log(
                    $"{label}: MISS - angle {angleToTarget:F1} > " +
                    $"{attack.angle * 0.5f:F1}."
                );

                return;
            }
        }

        // -----------------------------------------------------
        // SUCCESSFUL HIT
        // -----------------------------------------------------

        lastHitTime[index] = Time.time;

        // Apply damage first.
        target.TakeDamage(attack.damage);

        // -----------------------------------------------------
        // TAIL KNOCKBACK
        // -----------------------------------------------------
        // Only Tail (index 1) gets knockback.
        // This code is reached ONLY after all hit checks passed.

        if (index == 1 && !target.IsDead)
        {
            DragonKnockbackReceiver knockbackReceiver =
                target.GetComponent<DragonKnockbackReceiver>();

            if (knockbackReceiver == null)
            {
                // Fallback in case the receiver is on a parent object.
                knockbackReceiver =
                    target.GetComponentInParent<DragonKnockbackReceiver>();
            }

            if (knockbackReceiver != null)
            {
                knockbackReceiver.ApplyTailKnockback(
                    owner.transform.position
                );

                Log(
                    $"{label}: Tail knockback applied to {target.name}."
                );
            }
            else
            {
                Log(
                    $"{label}: HIT, but target {target.name} " +
                    $"has no DragonKnockbackReceiver."
                );
            }
        }

        // -----------------------------------------------------
        // SUCCESS LOG
        // -----------------------------------------------------

        Log(
            $"{label}: HIT for {attack.damage} " +
            $"(distance {distance:F2}). " +
            $"{target.name} HP " +
            $"{target.CurrentHealth}/{target.MaxHealth}"
        );
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void Log(string message)
    {
        if (debugLog)
            Debug.Log(message, this);
    }

    // =========================================================
    // CONTEXT TESTS
    // =========================================================

    [ContextMenu("Test Hit Fire")]
    private void TestFire() => HitFire();

    [ContextMenu("Test Hit Tail")]
    private void TestTail() => HitTail();

    [ContextMenu("Test Hit Fly")]
    private void TestFly() => HitFly();
}