using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public enum BattleResult
{
    None,
    PlayerWins,
    AIWins,
    Draw
}

/// <summary>
/// Central match-state controller.
/// Listens to DragonHealth.Died, decides the result once, stops gameplay scripts,
/// plays death animation(s), waits for them to finish, then raises BattleEnded.
/// UI is presented by UIManager, not here.
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("Dragons")]
    [SerializeField] private DragonHealth playerHealth;
    [SerializeField] private DragonHealth aiHealth;

    [Header("Gameplay Scripts To Stop On Battle End")]
    [SerializeField] private DragonPlayerController playerController;
    [SerializeField] private DragonPlayerCombatController playerCombatController;
    [SerializeField] private DragonAIController aiController;

    [Header("Death Animation")]
    [SerializeField] private DragonDeathController playerDeathController;
    [SerializeField] private DragonDeathController aiDeathController;
    [Tooltip("SAFETY TIMEOUT ONLY. Used if a death Animation Event is missing.")]
    [SerializeField] private float deathAnimationFallbackDuration = 4f;

    [Header("Winner Settle")]
    [Tooltip("Animator float set to 0 on the winner so it doesn't walk in place.")]
    [SerializeField] private string speedParameterName = "Speed";

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    /// <summary>
    /// Raised exactly once per battle, AFTER the death animation(s) finished
    /// (or the safety timeout elapsed). Presentation can now show the result.
    /// </summary>
    public event Action<BattleResult> BattleEnded;

    /// <summary>True as soon as the result is decided and gameplay is stopped.</summary>
    public bool IsBattleOver => battleEnded;
    public BattleResult Result { get; private set; } = BattleResult.None;

    private bool configValid;
    private bool subscribed;
    private bool battleEnded;
    private bool resolutionPending;
    private bool isRestarting;

    private bool playerDied;
    private bool aiDied;

    // Death wait state
    private bool playerDeathDone;
    private bool aiDeathDone;
    private bool deathSubscribed;

    private int speedHash;

    private void Awake()
    {
        battleEnded = false;
        resolutionPending = false;
        isRestarting = false;
        playerDied = false;
        aiDied = false;
        Result = BattleResult.None;
        speedHash = Animator.StringToHash(speedParameterName);

        configValid = ValidateReferences();
    }

    private void OnEnable()
    {
        if (!configValid || subscribed)
            return;

        playerHealth.Died += OnDragonDied;
        aiHealth.Died += OnDragonDied;
        subscribed = true;
    }

    private void Start()
    {
        if (configValid && !battleEnded && (playerHealth.IsDead || aiHealth.IsDead))
            BeginResolution();
    }

    private void OnDisable()
    {
        if (subscribed)
        {
            if (playerHealth != null) playerHealth.Died -= OnDragonDied;
            if (aiHealth != null) aiHealth.Died -= OnDragonDied;
            subscribed = false;
        }

        UnsubscribeDeathControllers();
        resolutionPending = false;
    }

    // ------------------------------------------------------------------
    // Death handling
    // ------------------------------------------------------------------

    private void OnDragonDied(DragonHealth deadDragon)
    {
        if (battleEnded)
            return;

        if (deadDragon == playerHealth) playerDied = true;
        else if (deadDragon == aiHealth) aiDied = true;
        else return;

        BeginResolution();
    }

    private void BeginResolution()
    {
        if (battleEnded || resolutionPending)
            return;

        resolutionPending = true;
        StartCoroutine(ResolveNextFrame());
    }

    /// <summary>Waits one frame so a second death in the same frame is counted (Draw).</summary>
    private IEnumerator ResolveNextFrame()
    {
        yield return null;

        resolutionPending = false;

        if (battleEnded)
            yield break;

        bool playerIsDead = playerDied || playerHealth.IsDead;
        bool aiIsDead = aiDied || aiHealth.IsDead;

        if (playerIsDead && aiIsDead)
            EndBattle(BattleResult.Draw);
        else if (playerIsDead)
            EndBattle(BattleResult.AIWins);
        else if (aiIsDead)
            EndBattle(BattleResult.PlayerWins);
    }

    private void EndBattle(BattleResult result)
    {
        if (battleEnded)
            return;

        battleEnded = true; // single-shot from here on
        Result = result;

        StopCombat();
        StopMotion();
        SettleWinner(result);
        LogResult(result);

        StartCoroutine(DeathSequence(result));
    }

    /// <summary>
    /// Plays death on the dead dragon(s), waits for the Animation Event(s),
    /// then raises BattleEnded. Fallback timeout is a safety net only.
    /// </summary>
    private IEnumerator DeathSequence(BattleResult result)
    {
        bool playerNeedsDeath = result == BattleResult.AIWins || result == BattleResult.Draw;
        bool aiNeedsDeath = result == BattleResult.PlayerWins || result == BattleResult.Draw;

        playerDeathDone = !playerNeedsDeath;
        aiDeathDone = !aiNeedsDeath;

        // Subscribe BEFORE playing so no event can be missed.
        if (playerNeedsDeath) PrepareDeath(playerDeathController, true);
        if (aiNeedsDeath) PrepareDeath(aiDeathController, false);
        deathSubscribed = true;

        if (playerNeedsDeath && playerDeathController != null) playerDeathController.PlayDeath();
        if (aiNeedsDeath && aiDeathController != null) aiDeathController.PlayDeath();

        float timer = 0f;
        while (!(playerDeathDone && aiDeathDone) && timer < deathAnimationFallbackDuration)
        {
            timer += Time.unscaledDeltaTime; // unscaled: timeScale can't freeze the safety net
            yield return null;
        }

        if (!(playerDeathDone && aiDeathDone))
        {
            Debug.LogWarning("[BattleManager] Death animation did not finish in time. " +
                             "Is the 'DeathAnimationFinishedEvent' Animation Event missing? Using fallback timeout.", this);
        }

        UnsubscribeDeathControllers();

        if (debugLog)
            Debug.Log("[BattleManager] Battle ended.", this);

        BattleEnded?.Invoke(result);
    }

    private void PrepareDeath(DragonDeathController controller, bool isPlayer)
    {
        if (controller == null)
        {
            Debug.LogWarning($"[BattleManager] {(isPlayer ? "Player" : "AI")} Death Controller not assigned. Skipping its death animation.", this);
            if (isPlayer) playerDeathDone = true; else aiDeathDone = true;
            return;
        }

        if (isPlayer)
        {
            if (controller.HasFinished) playerDeathDone = true;
            else controller.DeathAnimationFinished += OnPlayerDeathFinished;
        }
        else
        {
            if (controller.HasFinished) aiDeathDone = true;
            else controller.DeathAnimationFinished += OnAIDeathFinished;
        }
    }

    private void OnPlayerDeathFinished() { playerDeathDone = true; }
    private void OnAIDeathFinished() { aiDeathDone = true; }

    private void UnsubscribeDeathControllers()
    {
        if (!deathSubscribed)
            return;

        if (playerDeathController != null) playerDeathController.DeathAnimationFinished -= OnPlayerDeathFinished;
        if (aiDeathController != null) aiDeathController.DeathAnimationFinished -= OnAIDeathFinished;
        deathSubscribed = false;
    }

    // ------------------------------------------------------------------
    // Stop combat
    // ------------------------------------------------------------------

    private void StopCombat()
    {
        // Only these three. Health, damage dealer, hit feedback, fly launcher
        // and pools stay enabled. Animators stay enabled for the death animation.
        if (playerController != null) playerController.enabled = false;
        if (playerCombatController != null) playerCombatController.enabled = false;
        if (aiController != null) aiController.enabled = false;
    }

    /// <summary>Disabling scripts does not stop an agent or body that is already moving.</summary>
    private void StopMotion()
    {
        if (aiController != null)
        {
            NavMeshAgent agent = aiController.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }

        if (playerController != null)
        {
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    /// <summary>Winner stays alive and visible; just stop the walk cycle.</summary>
    private void SettleWinner(BattleResult result)
    {
        if (result == BattleResult.PlayerWins) SetAnimatorSpeedZero(playerDeathController);
        else if (result == BattleResult.AIWins) SetAnimatorSpeedZero(aiDeathController);
    }

    private void SetAnimatorSpeedZero(DragonDeathController controller)
    {
        if (controller == null || controller.Animator == null)
            return;

        controller.Animator.SetFloat(speedHash, 0f);
    }

    // ------------------------------------------------------------------
    // Logging
    // ------------------------------------------------------------------

    private void LogResult(BattleResult result)
    {
        if (!debugLog)
            return;

        switch (result)
        {
            case BattleResult.PlayerWins: Debug.Log("[BattleManager] Player wins.", this); break;
            case BattleResult.AIWins: Debug.Log("[BattleManager] AI wins.", this); break;
            case BattleResult.Draw: Debug.Log("[BattleManager] Draw.", this); break;
        }
    }

    // ------------------------------------------------------------------
    // Restart
    // ------------------------------------------------------------------

    public void RestartBattle()
    {
        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();

        if (debugLog)
            Debug.Log("[BattleManager] Restarting battle: " + activeScene.name, this);

        SceneManager.LoadScene(activeScene.name);
    }

    // ------------------------------------------------------------------
    // Validation
    // ------------------------------------------------------------------

    private bool ValidateReferences()
    {
        bool valid = true;

        if (playerHealth == null)
        {
            Debug.LogError("[BattleManager] Player Health is not assigned. Battle logic is disabled.", this);
            valid = false;
        }

        if (aiHealth == null)
        {
            Debug.LogError("[BattleManager] AI Health is not assigned. Battle logic is disabled.", this);
            valid = false;
        }

        if (valid && playerHealth == aiHealth)
        {
            Debug.LogError("[BattleManager] Player Health and AI Health are the same component. Battle logic is disabled.", this);
            valid = false;
        }

        // Non-fatal: missing gameplay refs only produce warnings.
        if (playerController == null || playerCombatController == null || aiController == null)
            Debug.LogWarning("[BattleManager] One or more gameplay script references are missing; those will not be stopped.", this);

        return valid;
    }

#if UNITY_EDITOR
    [ContextMenu("DEBUG/Kill AI")]
    private void DebugKillAI()
    {
        if (aiHealth != null) aiHealth.TakeDamage(aiHealth.CurrentHealth);
    }

    [ContextMenu("DEBUG/Kill Player")]
    private void DebugKillPlayer()
    {
        if (playerHealth != null) playerHealth.TakeDamage(playerHealth.CurrentHealth);
    }

    [ContextMenu("DEBUG/Kill Both (same frame)")]
    private void DebugKillBoth()
    {
        if (playerHealth != null) playerHealth.TakeDamage(playerHealth.CurrentHealth);
        if (aiHealth != null) aiHealth.TakeDamage(aiHealth.CurrentHealth);
    }
#endif
}