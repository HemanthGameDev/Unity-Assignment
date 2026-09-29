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

    [Header("Debug (temporary)")]
    [Tooltip("Logs attack requests, rejections, acceptances, cooldown ready and attack duration. Turn off when done.")]
    [SerializeField] private bool debugCooldowns = false;

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

    // Debug-only state
    private bool fireWasOnCooldown;
    private bool tailWasOnCooldown;
    private bool flyWasOnCooldown;
    private string currentAttackName;
    private float attackStartTime;

    // Public API (for UI and for DragonPlayerController)
    public float FireCooldownRemaining => fireTimer;
    public float TailCooldownRemaining => tailTimer;
    public float FlyCooldownRemaining => flyTimer;

    public float FireCooldownNormalized => fireCooldown <= 0f ? 0f : Mathf.Clamp01(fireTimer / fireCooldown);
    public float TailCooldownNormalized => tailCooldown <= 0f ? 0f : Mathf.Clamp01(tailTimer / tailCooldown);
    public float FlyCooldownNormalized => flyCooldown <= 0f ? 0f : Mathf.Clamp01(flyTimer / flyCooldown);
    public bool IsAttackPlaying => attackPlaying;
    public Transform AttackTarget => aiDragon;

    private void Awake()
    {
        input = new InputSystem_Actions();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (debugCooldowns)
            Debug.Log($"[Cooldown] Inspector values: Fire={fireCooldown:F2} Tail={tailCooldown:F2} Fly={flyCooldown:F2}", this);
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

        if (debugCooldowns)
        {
            CheckReady("Fire", fireTimer, ref fireWasOnCooldown);
            CheckReady("Tail", tailTimer, ref tailWasOnCooldown);
            CheckReady("Fly", flyTimer, ref flyWasOnCooldown);
        }
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        if (!CanAttack("Fire", fireTimer)) return;
        fireTimer = fireCooldown;
        if (debugCooldowns) Debug.Log($"[Cooldown] Fire cooldown set = {fireCooldown:F2}s", this);
        StartAttack(FireHash, "Fire");
    }

    private void OnTail(InputAction.CallbackContext context)
    {
        if (!CanAttack("Tail", tailTimer)) return;
        tailTimer = tailCooldown;
        if (debugCooldowns) Debug.Log($"[Cooldown] Tail cooldown set = {tailCooldown:F2}s", this);
        StartAttack(TailHash, "Tail");
    }

    private void OnFly(InputAction.CallbackContext context)
    {
        if (!CanAttack("Fly", flyTimer)) return;
        flyTimer = flyCooldown;
        if (debugCooldowns) Debug.Log($"[Cooldown] Fly cooldown set = {flyCooldown:F2}s", this);
        StartAttack(FlyHash, "Fly");
    }

    private bool CanAttack(string abilityName, float cooldownRemaining)
    {
        if (debugCooldowns)
            Debug.Log($"[Cooldown] {abilityName} requested (t={Time.time:F2})", this);

        if (attackPlaying) // never interrupt another attack
        {
            if (debugCooldowns)
                Debug.Log($"[Cooldown] {abilityName} rejected: attack already playing ({currentAttackName})", this);
            return false;
        }

        if (cooldownRemaining > 0f) // this ability is on cooldown
        {
            if (debugCooldowns)
                Debug.Log($"[Cooldown] {abilityName} rejected: cooldown remaining = {cooldownRemaining:F2}s", this);
            return false;
        }

        if (debugCooldowns)
            Debug.Log($"[Cooldown] {abilityName} accepted", this);

        return true;
    }

    private void StartAttack(int triggerHash, string abilityName)
    {
        // Locked immediately, in the same frame, so a second key press is ignored.
        attackPlaying = true;
        attackEntered = false;
        enterTimer = enterTimeout;

        currentAttackName = abilityName;
        attackStartTime = Time.time;

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
        if (debugCooldowns)
            Debug.Log($"[Cooldown] {currentAttackName} attack finished after {Time.time - attackStartTime:F2}s", this);

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

    private void CheckReady(string abilityName, float timer, ref bool wasOnCooldown)
    {
        if (timer > 0f)
        {
            wasOnCooldown = true;
        }
        else if (wasOnCooldown)
        {
            wasOnCooldown = false;
            Debug.Log($"[Cooldown] {abilityName} became ready (t={Time.time:F2})", this);
        }
    }
}