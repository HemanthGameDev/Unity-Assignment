using System;
using UnityEngine;

/// <summary>
/// Plays a dragon's death animation and reports when it has finished.
/// Put this on the SAME GameObject as the Animator (it receives the Animation Event).
/// </summary>
public class DragonDeathController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTriggerName = "Die";

    /// <summary>Raised once, by the Animation Event at the end of the death clip.</summary>
    public event Action DeathAnimationFinished;

    public Animator Animator => animator;
    public bool HasPlayed { get; private set; }
    public bool HasFinished { get; private set; }

    private int deathTriggerHash;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        deathTriggerHash = Animator.StringToHash(deathTriggerName);

        if (animator == null)
            Debug.LogError("[DragonDeathController] No Animator assigned or found.", this);
    }

    public void PlayDeath()
    {
        if (HasPlayed || animator == null)
            return;

        HasPlayed = true;
        animator.SetTrigger(deathTriggerHash);
    }

    /// <summary>Animation Event: add at the END of the death clip. No parameters.</summary>
    public void DeathAnimationFinishedEvent()
    {
        if (HasFinished)
            return;

        HasFinished = true;
        DeathAnimationFinished?.Invoke();
    }
}