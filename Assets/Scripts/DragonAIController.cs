using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DragonAIController : MonoBehaviour
{
    private enum AIState
    {
        Idle,
        Chase,
        Fire,
        Tail
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;

    [Header("Ranges")]
    [SerializeField] private float chaseRange = 30f;
    [SerializeField] private float fireRange = 10f;
    [SerializeField] private float tailRange = 3f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float angularSpeed = 360f;

    [Header("Attack Cooldowns")]
    [SerializeField] private float fireCooldown = 3f;
    [SerializeField] private float tailCooldown = 2f;

    [Header("Animation")]
    [SerializeField] private float animationDampTime = 0.1f;

    private NavMeshAgent agent;

    private AIState currentState;

    private float fireTimer;
    private float tailTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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

        ChangeState(AIState.Idle);
    }

    private void Update()
    {
        if (player == null)
            return;

        UpdateCooldowns();
        UpdateState();
        UpdateAnimation();
    }

    private void UpdateCooldowns()
    {
        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;

        if (tailTimer > 0f)
            tailTimer -= Time.deltaTime;
    }

    private void UpdateState()
    {
        float distance = GetDistanceToPlayer();

        switch (currentState)
        {
            case AIState.Idle:
                HandleIdle(distance);
                break;

            case AIState.Chase:
                HandleChase(distance);
                break;

            case AIState.Fire:
                HandleFire(distance);
                break;

            case AIState.Tail:
                HandleTail(distance);
                break;
        }
    }

    private void HandleIdle(float distance)
    {
        agent.isStopped = true;
        agent.ResetPath();

        FacePlayer();

        if (distance <= chaseRange)
        {
            ChangeState(AIState.Chase);
        }
    }

    private void HandleChase(float distance)
    {
        // Player completely outside detection range.
        if (distance > chaseRange)
        {
            ChangeState(AIState.Idle);
            return;
        }

        // Very close -> Tail.
        if (distance <= tailRange)
        {
            ChangeState(AIState.Tail);
            return;
        }

        // Fire range.
        if (distance <= fireRange)
        {
            ChangeState(AIState.Fire);
            return;
        }

        // Keep following the player continuously.
        agent.isStopped = false;
        agent.SetDestination(player.position);

        FacePlayer();
    }

    private void HandleFire(float distance)
    {
        // Player came close enough for Tail.
        if (distance <= tailRange)
        {
            ChangeState(AIState.Tail);
            return;
        }

        // Player moved outside Fire range.
        if (distance > fireRange)
        {
            ChangeState(AIState.Chase);
            return;
        }

        // Stay in place while firing.
        agent.isStopped = true;
        agent.ResetPath();

        FacePlayer();

        // Fire again when cooldown is complete
        // and the previous attack animation has finished.
        if (fireTimer <= 0f && !IsAttackAnimationPlaying())
        {
            animator.ResetTrigger("Tail");
            animator.SetTrigger("Fire");

            fireTimer = fireCooldown;
        }
    }

    private void HandleTail(float distance)
    {
        // Player moved outside Tail range
        // but is still within Fire range.
        if (distance > tailRange && distance <= fireRange)
        {
            ChangeState(AIState.Fire);
            return;
        }

        // Player moved outside Fire range.
        if (distance > fireRange)
        {
            ChangeState(AIState.Chase);
            return;
        }

        // Stay in place while attacking.
        agent.isStopped = true;
        agent.ResetPath();

        FacePlayer();

        // Tail again after cooldown.
        if (tailTimer <= 0f && !IsAttackAnimationPlaying())
        {
            animator.ResetTrigger("Fire");
            animator.SetTrigger("Tail");

            tailTimer = tailCooldown;
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        switch (currentState)
        {
            case AIState.Idle:

                agent.isStopped = true;
                agent.ResetPath();

                animator.ResetTrigger("Fire");
                animator.ResetTrigger("Tail");

                ReturnToLocomotion();

                break;

            case AIState.Chase:

                agent.isStopped = false;

                animator.ResetTrigger("Fire");
                animator.ResetTrigger("Tail");

                ReturnToLocomotion();

                break;

            case AIState.Fire:

                agent.isStopped = true;
                agent.ResetPath();

                animator.ResetTrigger("Tail");
                animator.SetTrigger("Fire");

                fireTimer = fireCooldown;

                break;

            case AIState.Tail:

                agent.isStopped = true;
                agent.ResetPath();

                animator.ResetTrigger("Fire");
                animator.SetTrigger("Tail");

                tailTimer = tailCooldown;

                break;
        }
    }

    private void ReturnToLocomotion()
    {
        // Immediately leave Fire/Tail animation.
        animator.CrossFade("Locomotion", 0.05f);
    }

    private bool IsAttackAnimationPlaying()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        return stateInfo.IsTag("Attack");
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