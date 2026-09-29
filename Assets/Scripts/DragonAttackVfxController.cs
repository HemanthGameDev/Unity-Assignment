using UnityEngine;

/// <summary>
/// Owns ONE reusable Fire ParticleSystem for this dragon. Visual only, never deals damage.
/// </summary>
public class DragonAttackVfxController : MonoBehaviour
{
    [SerializeField] private ParticleSystem fireVfx;

    private void Awake()
    {
        if (fireVfx == null)
        {
            Debug.LogWarning($"{name}: Fire VFX ParticleSystem is not assigned.", this);
            return;
        }

        // Make sure nothing is showing at start.
        fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void PlayFireVFX()
    {
        if (fireVfx == null) return;

        // Reset so a rapid second Fire restarts cleanly.
        fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        fireVfx.Play(true);
    }

    public void StopFireVFX()
    {
        if (fireVfx == null) return;
        fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}