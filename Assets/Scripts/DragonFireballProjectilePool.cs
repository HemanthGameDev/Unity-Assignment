using System;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Pool of fireballs. One instance in the scene is shared by both dragons.
/// </summary>
public class DragonFireballProjectilePool : MonoBehaviour
{
    [SerializeField] private DragonFireballProjectile projectilePrefab;
    [SerializeField] private DragonImpactVfxPool impactVfxPool;
    [SerializeField] private int initialSize = 4;
    [SerializeField] private int maxSize = 16;

    private ObjectPool<DragonFireballProjectile> pool;
    private Action<DragonFireballProjectile> releaseCallback;

    private void Awake()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("[DragonFireballProjectilePool] Projectile prefab not assigned.", this);
            enabled = false;
            return;
        }

        maxSize = Mathf.Max(1, maxSize);
        initialSize = Mathf.Clamp(initialSize, 0, maxSize);

        pool = new ObjectPool<DragonFireballProjectile>(
            createFunc: CreateProjectile,
            actionOnGet: null,
            actionOnRelease: p => p.gameObject.SetActive(false),
            actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
            collectionCheck: true,
            defaultCapacity: initialSize,
            maxSize: maxSize);

        releaseCallback = pool.Release; // cached once: no per-shot delegate allocation

        // Prewarm once at startup.
        DragonFireballProjectile[] temp = new DragonFireballProjectile[initialSize];
        for (int i = 0; i < initialSize; i++) temp[i] = pool.Get();
        for (int i = 0; i < initialSize; i++) pool.Release(temp[i]);
    }

    private DragonFireballProjectile CreateProjectile()
    {
        DragonFireballProjectile p = Instantiate(projectilePrefab, transform);
        p.gameObject.SetActive(false);
        p.Setup(impactVfxPool, releaseCallback);
        return p;
    }

    public DragonFireballProjectile Launch(DragonHealth owner, DragonHealth target, float damage, Vector3 position)
    {
        if (pool == null) return null;
        DragonFireballProjectile p = pool.Get();
        p.Launch(owner, target, damage, position);
        return p;
    }
}