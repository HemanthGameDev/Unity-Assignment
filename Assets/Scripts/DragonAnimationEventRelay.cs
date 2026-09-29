using UnityEngine;

/// <summary>
/// Lives on the Animator object. Thin bridge only: no gameplay logic.
/// </summary>
public class DragonAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private DragonAttackVfxController attackVfx;
    [SerializeField] private DragonDamageDealer damageDealer;
    [SerializeField] private DragonFlyLauncher flyLauncher;
    [SerializeField] private DragonSFXController sfx;   // NEW

    private void Awake()
    {
        if (attackVfx == null) attackVfx = GetComponentInParent<DragonAttackVfxController>();
        if (damageDealer == null) damageDealer = GetComponentInParent<DragonDamageDealer>();
        if (flyLauncher == null) flyLauncher = GetComponentInParent<DragonFlyLauncher>();
        if (sfx == null) sfx = GetComponentInParent<DragonSFXController>();   // NEW
    }

    // Fire animation events
    public void PlayFireVFX() { if (attackVfx != null) attackVfx.PlayFireVFX(); }
    public void FireHit() { if (damageDealer != null) damageDealer.HitFire(); }

    // Tail animation event
    public void TailHit() { if (damageDealer != null) damageDealer.HitTail(); }

    // Fly animation event (unchanged behavior)
    public void LaunchFlyProjectile() { if (flyLauncher != null) flyLauncher.LaunchFireball(); }

    // NEW — SFX animation events
    public void PlayFireSFX() { if (sfx != null) sfx.PlayFire(); }
    public void PlayTailAttackSFX() { if (sfx != null) sfx.PlayTailAttack(); }
    public void PlayFlySFX() { if (sfx != null) sfx.PlayFly(); }
    public void PlayFireballSFX() { if (sfx != null) sfx.PlayFireball(); }
}