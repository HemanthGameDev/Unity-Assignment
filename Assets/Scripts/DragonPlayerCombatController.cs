using UnityEngine;
using UnityEngine.InputSystem;

public class DragonPlayerCombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Attack Target")]
    [SerializeField] private Transform aiDragon;

    [Header("Cooldowns (seconds)")]
    [SerializeField] private float fireCooldown = 3f;
    [SerializeField] private float tailCooldown = 2f;
    [SerializeField] private float flyCooldown = 5f;

    [Header("Safety")]
    [Tooltip("If the Animator never enters an Attack state within this time, the attack is cancelled.")]
    [SerializeField] private float enterTimeout = 0.5f;

    private const string AttackTag = "Attack";

    private static readonly int FireHash = Animator.StringToHash("Fire");
    private static readonly int TailHash = Animator.StringToHash("Tail");
    private static readonly int FlyHash = Animator.StringToHash("Fly");

    private InputSystem_Actions input;

    private float fireTimer;
    private float tailTimer;
    private float flyTimer;

    private bool attackPlaying;
    private bool attackEntered;   // Has the Animator actually reached an Attack state yet?
    private float enterTimer;

    // Public API (for UI and for DragonPlayerController)
    public float FireCooldownRemaining => fireTimer;
    public float TailCooldownRemaining => tailTimer;
    public float FlyCooldownRemaining => flyTimer;
    public bool IsAttackPlaying => attackPlaying;
    public Transform AttackTarget => aiDragon;

    private void Awake()
    {
        input = new InputSystem_Actions();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        input.Player.Fire.performed += OnFire;
        input.Player.Tail.performed += OnTail;
        input.Player.Fly.performed += OnFly;
        input.Enable();
    }

    private void OnDisable()
    {
        input.Player.Fire.performed -= OnFire;
        input.Player.Tail.performed -= OnTail;
        input.Player.Fly.performed -= OnFly;
        input.Disable();

        attackPlaying = false;
        attackEntered = false;
    }

    private void Update()
    {
        UpdateCooldowns();
        UpdateAttackStatus();
    }

    private void UpdateCooldowns()
    {
        fireTimer = Mathf.Max(0f, fireTimer - Time.deltaTime);
        tailTimer = Mathf.Max(0f, tailTimer - Time.deltaTime);
        flyTimer = Mathf.Max(0f, flyTimer - Time.deltaTime);
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        if (!CanAttack(fireTimer)) return;
        fireTimer = fireCooldown;
        StartAttack(FireHash);
    }

    private void OnTail(InputAction.CallbackContext context)
    {
        if (!CanAttack(tailTimer)) return;
        tailTimer = tailCooldown;
        StartAttack(TailHash);
    }

    private void OnFly(InputAction.CallbackContext context)
    {
        if (!CanAttack(flyTimer)) return;
        flyTimer = flyCooldown;
        StartAttack(FlyHash);
    }

    private bool CanAttack(float cooldownRemaining)
    {
        if (attackPlaying) return false;          // never interrupt another attack
        if (cooldownRemaining > 0f) return false; // this ability is on cooldown
        return true;
    }

    private void StartAttack(int triggerHash)
    {
        // Locked immediately, in the same frame, so a second key press is ignored.
        attackPlaying = true;
        attackEntered = false;
        enterTimer = enterTimeout;

        ResetAllTriggers();
        animator.SetTrigger(triggerHash);
    }

    private void UpdateAttackStatus()
    {
        if (!attackPlaying)
            return;

        bool inAttack = IsInAttackState();

        // Phase 1: wait until the Animator really enters an Attack state.
        if (!attackEntered)
        {
            if (inAttack)
            {
                attackEntered = true;
            }
            else
            {
                enterTimer -= Time.deltaTime;
                if (enterTimer <= 0f)
                {
                    Debug.LogWarning("Attack never entered an 'Attack' tagged state. Check Animator tags/transitions.", this);
                    FinishAttack();
                }
            }
            return;
        }

        // Phase 2: attack is over once the Animator has fully left the Attack state.
        if (!inAttack)
            FinishAttack();
    }

    // True while the current state is tagged Attack, or while we are blending INTO an Attack state.
    private bool IsInAttackState()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsTag(AttackTag))
            return true;

        if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsTag(AttackTag))
            return true;

        return false;
    }

    private void FinishAttack()
    {
        attackPlaying = false;
        attackEntered = false;
        ResetAllTriggers();
    }

    private void ResetAllTriggers()
    {
        animator.ResetTrigger(FireHash);
        animator.ResetTrigger(TailHash);
        animator.ResetTrigger(FlyHash);
    }
}