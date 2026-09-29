using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(100)]
public class DragonKnockbackReceiver : MonoBehaviour
{
    [Header("Tail Knockback")]
    [SerializeField] private float tailKnockbackDistance = 1.5f;
    [SerializeField] private float knockbackDuration = 0.18f;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private NavMeshAgent agent;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool useAgent;
    private bool isKnockedBack;

    private Vector3 knockbackDirection;
    private float knockbackDistance;
    private float knockbackDurationValue;
    private float elapsed;

    private Vector3 playerStartPosition;
    private Vector3 playerTargetPosition;

    public bool IsKnockedBack => isKnockedBack;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent == null)
            agent = GetComponentInParent<NavMeshAgent>();

        useAgent = agent != null;

        if (debugLog)
        {
            Debug.Log(
                $"{name}: Knockback initialized. " +
                $"Mode = {(useAgent ? "NavMeshAgent" : "Rigidbody")}.",
                this
            );
        }
    }

    public void ApplyTailKnockback(Vector3 attackerPosition)
    {
        Vector3 direction = transform.position - attackerPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = -transform.forward;
            direction.y = 0f;
        }

        direction.Normalize();

        if (useAgent)
        {
            if (!agent.enabled || !agent.isOnNavMesh)
            {
                if (debugLog)
                    Debug.LogWarning(
                        $"{name}: Cannot knock back because NavMeshAgent is not active/on NavMesh.",
                        this
                    );

                return;
            }

            StartAgentKnockback(direction);
        }
        else
        {
            if (rb == null)
            {
                if (debugLog)
                    Debug.LogWarning(
                        $"{name}: Cannot knock back because no Rigidbody was found.",
                        this
                    );

                return;
            }

            StartPlayerKnockback(direction);
        }
    }

    // =========================================================
    // AI
    // =========================================================

    private void StartAgentKnockback(Vector3 direction)
    {
        knockbackDirection = direction;
        knockbackDistance = tailKnockbackDistance;
        knockbackDurationValue = knockbackDuration;
        elapsed = 0f;
        isKnockedBack = true;

        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        if (debugLog)
        {
            Debug.Log(
                $"{name}: AI knockback START. Direction={direction}, Distance={knockbackDistance}",
                this
            );
        }
    }

    private void Update()
    {
        if (!isKnockedBack || !useAgent)
            return;

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            StopKnockback();
            return;
        }

        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / knockbackDurationValue);

        // Smooth ease-out.
        float eased = 1f - Mathf.Pow(1f - t, 3f);

        // Calculate total desired displacement.
        Vector3 desiredOffset =
            knockbackDirection * (knockbackDistance * eased);

        // Move only the amount not already moved.
        Vector3 currentPosition = agent.transform.position;

        Vector3 desiredPosition =
            currentPosition;

        if (t <= 1f)
        {
            // Use small frame-by-frame offset based on the remaining
            // total displacement.
            Vector3 targetPosition =
                agent.transform.position +
                knockbackDirection *
                (knockbackDistance * (1f - t));

            Vector3 offset =
                knockbackDirection *
                (knockbackDistance * Time.deltaTime / knockbackDurationValue);

            agent.Move(offset);
        }

        agent.isStopped = true;

        if (t >= 1f)
        {
            StopKnockback();

            if (debugLog)
                Debug.Log($"{name}: AI knockback END.", this);
        }
    }

    // =========================================================
    // PLAYER
    // =========================================================

    private void StartPlayerKnockback(Vector3 direction)
    {
        knockbackDirection = direction;
        knockbackDistance = tailKnockbackDistance;
        knockbackDurationValue = knockbackDuration;
        elapsed = 0f;
        isKnockedBack = true;

        playerStartPosition = rb.position;
        playerTargetPosition =
            playerStartPosition +
            direction * knockbackDistance;

        if (debugLog)
        {
            Debug.Log(
                $"{name}: PLAYER knockback START. Direction={direction}, Distance={knockbackDistance}",
                this
            );
        }
    }

    private void FixedUpdate()
    {
        if (!isKnockedBack || useAgent || rb == null)
            return;

        elapsed += Time.fixedDeltaTime;

        float t = Mathf.Clamp01(
            elapsed / knockbackDurationValue
        );

        float eased = 1f - Mathf.Pow(1f - t, 3f);

        Vector3 desiredPosition = Vector3.Lerp(
            playerStartPosition,
            playerTargetPosition,
            eased
        );

        // Preserve Y / gravity.
        desiredPosition.y = rb.position.y;

        rb.MovePosition(desiredPosition);

        if (t >= 1f)
        {
            StopKnockback();

            if (debugLog)
                Debug.Log($"{name}: Player knockback END.", this);
        }
    }

    // =========================================================
    // STOP
    // =========================================================

    private void StopKnockback()
    {
        isKnockedBack = false;

        if (agent != null && agent.enabled)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = false;
        }
    }
}