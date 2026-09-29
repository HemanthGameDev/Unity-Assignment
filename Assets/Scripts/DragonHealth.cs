using System;
using UnityEngine;

public class DragonHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    [Header("On Death")]
    [Tooltip("Scripts/components to switch off when this dragon dies (movement, combat, AI, NavMeshAgent).")]
    [SerializeField] private Behaviour[] disableOnDeath;

    [Tooltip("Optional. Freezes the pose on death until a death animation exists.")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool freezeAnimatorOnDeath = true;

    /// <summary>(currentHealth, maxHealth) - raised whenever health changes.</summary>
    public event Action<float, float> HealthChanged;

    /// <summary>Raised once, when health reaches zero. Passes the dragon that died.</summary>
    public event Action<DragonHealth> Died;

    private float currentHealth;
    private bool initialized;

    public float MaxHealth => maxHealth;

    /// <summary>
    /// Safe to read at any time, even before this component's Awake has run
    /// (other scripts may read it in their own Awake/OnEnable).
    /// </summary>
    public float CurrentHealth
    {
        get
        {
            EnsureInitialized();
            return currentHealth;
        }
    }

    public float HealthNormalized => CurrentHealth / maxHealth;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        EnsureInitialized();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        currentHealth = maxHealth;
        initialized = true;
    }

    /// <summary>Returns true if damage was actually applied.</summary>
    public bool TakeDamage(float amount)
    {
        EnsureInitialized();

        if (IsDead || amount <= 0f)
            return false;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();

        return true;
    }

    private void Die()
    {
        IsDead = true;

        if (disableOnDeath != null)
        {
            foreach (Behaviour b in disableOnDeath)
            {
                if (b != null)
                    b.enabled = false;
            }
        }

        if (TryGetComponent(out Rigidbody rb) && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (freezeAnimatorOnDeath && animator != null)
            animator.enabled = false;

        Died?.Invoke(this);
    }
}