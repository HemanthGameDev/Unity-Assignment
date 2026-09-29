using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DragonPlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Animator animator;
    [SerializeField] private DragonPlayerCombatController combat;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float stoppingDistance = 0.15f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float deceleration = 5f;

    [Header("Attack Rotation")]
    [Tooltip("Degrees per second while facing the AI Dragon during an attack.")]
    [SerializeField] private float attackRotationSpeed = 360f;
    [Tooltip("If true, the player continues to the last clicked point after an attack ends.")]
    [SerializeField] private bool resumeMovementAfterAttack = true;

    [Header("Raycast")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayDistance = 1000f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private InputSystem_Actions input;
    private Rigidbody rb;

    private Vector3 targetPosition;
    private bool hasTarget;
    private float currentMoveSpeed;
    private bool wasAttacking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        input = new InputSystem_Actions();

        if (mainCamera == null) mainCamera = Camera.main;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (combat == null) combat = GetComponent<DragonPlayerCombatController>();
    }

    private void OnEnable()
    {
        input.Player.MoveTo.performed += OnMoveClicked;
        input.Enable();
    }

    private void OnDisable()
    {
        input.Player.MoveTo.performed -= OnMoveClicked;
        input.Disable();
    }

    private void OnMoveClicked(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, groundLayer))
        {
            targetPosition = hit.point;
            hasTarget = true;
        }
    }

    private void FixedUpdate()
    {
        bool attacking = combat != null && combat.IsAttackPlaying;

        // Attack just started: stop the movement immediately.
        if (attacking && !wasAttacking && !resumeMovementAfterAttack)
            hasTarget = false;
        wasAttacking = attacking;

        if (attacking)
        {
            HandleAttackState();
            return;
        }

        HandleMovement();
    }

    // ATTACK: no movement, rotate toward the AI Dragon.
    private void HandleAttackState()
    {
        currentMoveSpeed = 0f;
        SetHorizontalVelocity(Vector3.zero);
        animator.SetFloat(SpeedHash, 0f);

        Transform target = combat.AttackTarget;
        if (target == null)
            return;

        Vector3 direction = target.position - rb.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        rb.MoveRotation(Quaternion.RotateTowards(
            rb.rotation,
            targetRotation,
            attackRotationSpeed * Time.fixedDeltaTime
        ));
    }

    // NORMAL: click-to-move with acceleration / deceleration.
    private void HandleMovement()
    {
        if (!hasTarget)
        {
            StopMovement();
            return;
        }

        Vector3 direction = targetPosition - rb.position;
        direction.y = 0f;

        if (direction.magnitude <= stoppingDistance)
        {
            hasTarget = false;
            StopMovement();
            return;
        }

        Vector3 moveDirection = direction.normalized;

        currentMoveSpeed = Mathf.MoveTowards(
            currentMoveSpeed,
            moveSpeed,
            acceleration * Time.fixedDeltaTime
        );

        SetHorizontalVelocity(moveDirection * currentMoveSpeed);
        animator.SetFloat(SpeedHash, currentMoveSpeed);

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

        rb.MoveRotation(Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        ));
    }

    private void StopMovement()
    {
        currentMoveSpeed = Mathf.MoveTowards(
            currentMoveSpeed,
            0f,
            deceleration * Time.fixedDeltaTime
        );

        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.sqrMagnitude > 0.001f)
            horizontalVelocity = horizontalVelocity.normalized * currentMoveSpeed;
        else
            horizontalVelocity = Vector3.zero;

        SetHorizontalVelocity(horizontalVelocity);
        animator.SetFloat(SpeedHash, currentMoveSpeed);
    }

    private void SetHorizontalVelocity(Vector3 velocity)
    {
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
    }
}