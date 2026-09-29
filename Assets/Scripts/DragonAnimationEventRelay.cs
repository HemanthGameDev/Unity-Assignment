using UnityEngine;

/// <summary>
/// Lives on the Animator object. Thin bridge only: no gameplay logic.
/// </summary>
public class DragonAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private DragonAttackVfxController attackVfx;
    [SerializeField] private DragonDamageDealer damageDealer;
    [SerializeField] private DragonFlyLauncher flyLauncher;

    private void Awake()
    {
        // Auto-find on the dragon root if not assigned (cached once).
        if (attackVfx == null) attackVfx = GetComponentInParent<DragonAttackVfxController>();
        if (damageDealer == null) damageDealer = GetComponentInParent<DragonDamageDealer>();
        if (flyLauncher == null) flyLauncher = GetComponentInParent<DragonFlyLauncher>();
    }

    // Fire animation events
    public void PlayFireVFX() { if (attackVfx != null) attackVfx.PlayFireVFX(); }
    public void FireHit() { if (damageDealer != null) damageDealer.HitFire(); }

    // Tail animation event
    public void TailHit() { if (damageDealer != null) damageDealer.HitTail(); }

    // Fly animation event (unchanged behavior)
    public void LaunchFlyProjectile() { if (flyLauncher != null) flyLauncher.LaunchFireball(); }
}