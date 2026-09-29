using System;
using UnityEngine;

/// <summary>
/// Put on the root of any pooled VFX prefab. Resets particles on Play(),
/// and returns itself to its pool after a lifetime (no coroutines).
/// </summary>
public class DragonPooledVfx : MonoBehaviour
{
    [Tooltip("Leave empty to auto-collect all ParticleSystems in children.")]
    [SerializeField] private ParticleSystem[] particleSystems;

    private Action<DragonPooledVfx> releaseAction;
    private Vector3 baseScale = Vector3.one;
    private float releaseTime;
    private bool isActive;
    private bool cached;

    public void SetReleaseAction(Action<DragonPooledVfx> action)
    {
        releaseAction = action;
    }

    private void CacheComponents()
    {
        if (cached) return;
        cached = true;
        baseScale = transform.localScale;
        if (particleSystems == null || particleSystems.Length == 0)
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void Play(Vector3 position, Quaternion rotation, float scaleMultiplier, float lifetime)
    {
        CacheComponents();

        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = baseScale * scaleMultiplier;
        gameObject.SetActive(true);

        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null) continue;
            particleSystems[i].Clear(true);
            particleSystems[i].Play(true);
        }

        releaseTime = Time.time + lifetime;
        isActive = true;
    }

    private void Update()
    {
        if (isActive && Time.time >= releaseTime)
            Release();
    }

    public void Release()
    {
        if (!isActive) return;
        isActive = false;

        CacheComponents();
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null) continue;
            particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        gameObject.SetActive(false);
        releaseAction?.Invoke(this);
    }

    private void OnDisable()
    {
        isActive = false;
    }
}