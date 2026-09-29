using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DragonAIController : MonoBehaviour
{
    // Decision state: what the AI WANTS to do based on distance.
    private enum AIState
    {
        Idle,
        Chase,
        Fire,
        Tail,
        Fly
    }

    private enum AttackType
    {
        Fire,
        Tail,
        Fly
    }

    // Attack lifecycle: what the attack animation is actually doing.
    private enum AttackPhase
    {
        None,       // no attack in progress
        Requested,  // trigger sent, Animator has not entered the attack yet
        Playing     // Animator is in (or blending into) an Attack-tagged state
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [Tooltip("Optional. Auto-found on the player if empty. Used to react when the player attacks.")]
    [SerializeField] private DragonPlayerCombatController playerCombat;

    [Header("Ranges")]
    [SerializeField] private float chaseRange = 30f;
    [Tooltip("Range at which the AI becomes hostile after the Player attacks.")]
    [SerializeField] private float alertRange = 60f;
    [Tooltip("Keep this at or below the DragonDamageDealer Fire range.")]
    [SerializeField] private float fireRange = 7f;
    [Tooltip("Keep this at or below the DragonDamageDealer Tail range.")]
    [SerializeField] private float tailRange = 3f;
    [Tooltip("Fly (fireball) is used beyond Fire range, up to this distance. Keep at or below Chase Range.")]
    [SerializeField] private float flyRange = 14f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float angularSpeed = 360f;

    [Header("Attack Cooldowns")]
    [SerializeField] private float fireCooldown = 3f;
    [SerializeField] private float tailCooldown = 2f;
    [SerializeField] private float flyCooldown = 5f;

    [Header("Attack Rules")]
    [Tooltip("AI only starts an attack when facing the Player within this angle. Keep below half the Fire cone angle.")]
    [SerializeField] private float attackFacingTolerance = 15f;

    [Header("Safety Timeouts")]
    [Tooltip("If the Animator never enters the attack state after the trigger, give up after this long.")]
    [SerializeField] private float attackStartTimeout = 1f;
    [Tooltip("If an attack state never ends, force-finish after this long.")]
    [SerializeField] private float attackMaxDuration = 8f;

    [Header("Animation")]
    [SerializeField] private float animationDampTime = 0.1f;

    private NavMeshAgent agent;

    private AIState currentState = AIState.Idle;
    private AttackPhase attackPhase = AttackPhase.None;
    private float attackPhaseTime;

    private float fireTimer;
    private float tailTimer;
    private float flyTimer;

    private bool alerted;

    // ------------------------------------------------------------------
    // Read-only cooldown API (for UI display only; UI never controls the AI)
    // ------------------------------------------------------------------

    public float FireCooldownRemaining => Mathf.Max(0f, fireTimer);
    public float TailCooldownRemaining => Mathf.Max(0f, tailTimer);
    public float FlyCooldownRemaining => Mathf.Max(0f, flyTimer);

    public float FireCooldownNormalized => fireCooldown <= 0f ? 0f : Mathf.Clamp01(fireTimer / fireCooldown);
    public float TailCooldownNormalized => tailCooldown <= 0f ? 0f : Mathf.Clamp01(tailTimer / tailCooldown);
    public float FlyCooldownNormalized => flyCooldown <= 0f ? 0f : Mathf.Clamp01(flyTimer / flyCooldown);

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (playerCombat == null && player != null)
        {
            playerCombat = player.GetComponentInParent<DragonPlayerCombatController>();

            if (playerCombat == null)
                playerCombat = player.GetComponentInChildren<DragonPlayerCombatController>();
        }
    }

    private void Start()
    {
        agent.speed = moveSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = angularSpeed;

        // We decide attack distances ourselves.
        agent.stoppingDistance = 0f;

        // We manually face the player.
        agent.updateRotation = false;

        StopAgent();
    }

    private void OnValidate()
    {
        // Fly band starts where Fire ends.
        if (flyRange < fireRange)
            flyRange = fireRange;
    }

    private void Update()
    {
        if (player == null)
            return;

        UpdateCooldowns();
        UpdateAttackTracking();

        float distance = GetDistanceToPlayer();

        UpdateAlert(distance);

        if (attackPhase != AttackPhase.None)
        {
            // An attack animation is in progress: never change state, never interrupt.
            StopAgent();
            FacePlayer();
        }
        else
        {
            // No attack in progress: always decide from the CURRENT distance.
            currentState = DecideState(distance);
            RunState();
        }

        UpdateAnimation();
    }

    // ------------------------------------------------------------------
    // Cooldowns (independent of state and of attack animation)
    // ------------------------------------------------------------------

    private void UpdateCooldowns()
    {
        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;

        if (tailTimer > 0f)
            tailTimer -= Time.deltaTime;

        if (flyTimer > 0f)
            flyTimer -= Time.deltaTime;
    }

    // ------------------------------------------------------------------
    // Alert: become hostile when the Player attacks
    // ------------------------------------------------------------------

    private void UpdateAlert(float distance)
    {
        if (alerted)
            return;

        if (playerCombat != null && playerCombat.IsAttackPlaying && distance <= alertRange)
            alerted = true;
    }

    // ------------------------------------------------------------------
    // Decision (distance only)
    // ------------------------------------------------------------------

    private AIState DecideState(float distance)
    {
        float detectionRange = alerted ? alertRange : chaseRange;

        if (distance > detectionRange)
        {
            alerted = false;
            return AIState.Idle;
        }

        if (distance <= tailRange)
            return AIState.Tail;

        if (distance <= fireRange)
            return AIState.Fire;

        if (distance <= flyRange)
            return AIState.Fly;

        return AIState.Chase;
    }

    private void RunState()
    {
        switch (currentState)
        {
            case AIState.Idle:
                StopAgent();
                FacePlayer();
                break;

            case AIState.Chase:
                ChasePlayer();
                break;

            case AIState.Fire:
                StopAgent();
                FacePlayer();

                if (fireTimer <= 0f && IsFacingPlayer())
                    StartAttack(AttackType.Fire);
                break;

            case AIState.Tail:
                StopAgent();
                FacePlayer();

                if (tailTimer <= 0f && IsFacingPlayer())
                    StartAttack(AttackType.Tail);
                break;

            case AIState.Fly:
                if (flyTimer <= 0f)
                {
                    // Ready: stop, aim, fire the fireball.
                    StopAgent();
                    FacePlayer();

                    if (IsFacingPlayer())
                        StartAttack(AttackType.Fly);
                }
                else
                {
                    // On cooldown: keep closing in so the AI never stalls at long range.
                    ChasePlayer();
                }
                break;
        }
    }

    private void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        FacePlayer();
    }

    // ------------------------------------------------------------------
    // Attack lifecycle: Requested -> Playing -> Finished
    // ------------------------------------------------------------------

    private void StartAttack(AttackType type)
    {
        // Clear stale triggers so an old unconsumed trigger can never fire later.
        ResetAttackTriggers();

        switch (type)
        {
            case AttackType.Fire:
                animator.SetTrigger("Fire");
                fireTimer = fireCooldown;
                break;

            case AttackType.Tail:
                animator.SetTrigger("Tail");
                tailTimer = tailCooldown;
                break;

            case AttackType.Fly:
                animator.SetTrigger("Fly");
                flyTimer = flyCooldown;
                break;
        }

        // Do not slide while attacking.
        agent.velocity = Vector3.zero;
        StopAgent();

        attackPhase = AttackPhase.Requested;
        attackPhaseTime = 0f;
    }

    private void ResetAttackTriggers()
    {
        animator.ResetTrigger("Fire");
        animator.ResetTrigger("Tail");
        animator.ResetTrigger("Fly");
    }

    private void UpdateAttackTracking()
    {
        if (attackPhase == AttackPhase.None)
            return;

        attackPhaseTime += Time.deltaTime;

        bool inAttack = IsAttackStateActive();

        if (attackPhase == AttackPhase.Requested)
        {
            if (inAttack)
            {
                attackPhase = AttackPhase.Playing;
                attackPhaseTime = 0f;
            }
            else if (attackPhaseTime > attackStartTimeout)
            {
                // Animator never entered the attack. Clean up so the AI cannot get stuck.
                Debug.LogWarning($"{name}: attack trigger was not consumed by the Animator. Check transitions/conditions.", this);
                ResetAttackTriggers();
                attackPhase = AttackPhase.None;
            }
        }
        else // Playing
        {
            if (!inAttack)
            {
                // Attack (including its exit transition) has finished.
                attackPhase = AttackPhase.None;
            }
            else if (attackPhaseTime > attackMaxDuration)
            {
                Debug.LogWarning($"{name}: attack state exceeded max duration; forcing finish.", this);
                attackPhase = AttackPhase.None;
            }
        }
    }

    // True if the Animator is in an Attack-tagged state, OR blending into one.
    // Checking the NEXT state is what makes this reliable during transitions.
    private bool IsAttackStateActive()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
            return true;

        if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsTag("Attack"))
            return true;

        return false;
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void StopAgent()
    {
        if (!agent.enabled)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private bool IsFacingPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return true;

        Vector3 forward = transform.forward;
        forward.y = 0f;

        return Vector3.Angle(forward, direction) <= attackFacingTolerance;
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            angularSpeed * Time.deltaTime
        );
    }

    private float GetDistanceToPlayer()
    {
        Vector3 direction = player.position - transform.position;

        direction.y = 0f;

        return direction.magnitude;
    }

    private void UpdateAnimation()
    {
        Vector3 velocity = agent.velocity;
        velocity.y = 0f;

        float speed = velocity.magnitude;

        animator.SetFloat(
            "Speed",
            speed,
            animationDampTime,
            Time.deltaTime
        );
    }
}