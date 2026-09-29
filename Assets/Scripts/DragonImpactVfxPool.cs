using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Pools the impact effects: several flames spread around the impact point,
/// plus an optional burst at the impact point itself.
/// </summary>
public class DragonImpactVfxPool : MonoBehaviour
{
    [Header("Prefabs (each needs a DragonPooledVfx on its root)")]
    [SerializeField] private DragonPooledVfx flameVfxPrefab;
    [Tooltip("Optional. Played once at the exact impact point.")]
    [SerializeField] private DragonPooledVfx impactBurstPrefab;

    [Header("Pool Sizes")]
    [SerializeField] private int flameInitialSize = 12;
    [SerializeField] private int flameMaxSize = 30;
    [SerializeField] private int burstInitialSize = 4;
    [SerializeField] private int burstMaxSize = 10;

    [Header("Flame Layout")]
    [SerializeField] private int impactVfxCount = 4;
    [SerializeField] private float minImpactRadius = 0.8f;
    [SerializeField] private float maxImpactRadius = 1.6f;
    [Range(0f, 0.5f)]
    [SerializeField] private float angleJitterFraction = 0.3f;
    [SerializeField] private Vector2 scaleRange = new Vector2(0.9f, 1.2f);
    [SerializeField] private bool randomYRotation = true;

    [Header("Lifetime")]
    [SerializeField] private float impactVfxLifetime = 2.5f;
    [SerializeField] private float burstLifetime = 1.5f;

    [Header("Ground Placement")]
    [Tooltip("Optional. If set, flames are snapped to this layer. If empty, the target's feet height is used.")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundRayHeight = 3f;
    [SerializeField] private float groundOffset = 0.05f;

    private ObjectPool<DragonPooledVfx> flamePool;
    private ObjectPool<DragonPooledVfx> burstPool;

    private void Awake()
    {
        if (flameVfxPrefab != null)
            flamePool = BuildPool(flameVfxPrefab, flameInitialSize, flameMaxSize);
        else
            Debug.LogWarning("[DragonImpactVfxPool] Flame prefab not assigned.", this);

        if (impactBurstPrefab != null)
            burstPool = BuildPool(impactBurstPrefab, burstInitialSize, burstMaxSize);
    }

    private ObjectPool<DragonPooledVfx> BuildPool(DragonPooledVfx prefab, int initialSize, int maxSize)
    {
        maxSize = Mathf.Max(1, maxSize);
        initialSize = Mathf.Clamp(initialSize, 0, maxSize);

        ObjectPool<DragonPooledVfx> pool = null;
        pool = new ObjectPool<DragonPooledVfx>(
            createFunc: () =>
            {
                DragonPooledVfx vfx = Instantiate(prefab, transform);
                vfx.gameObject.SetActive(false);
                vfx.SetReleaseAction(pool.Release);
                return vfx;
            },
            actionOnGet: null,
            actionOnRelease: v => v.gameObject.SetActive(false),
            actionOnDestroy: v => { if (v != null) Destroy(v.gameObject); },
            collectionCheck: true,
            defaultCapacity: initialSize,
            maxSize: maxSize);

        // Prewarm once at startup so gameplay doesn't allocate.
        DragonPooledVfx[] temp = new DragonPooledVfx[initialSize];
        for (int i = 0; i < initialSize; i++) temp[i] = pool.Get();
        for (int i = 0; i < initialSize; i++) pool.Release(temp[i]);

        return pool;
    }

    /// <summary>
    /// impactPoint: where the fireball hit. groundY: height of the target's feet.
    /// </summary>
    public void SpawnImpact(Vector3 impactPoint, float groundY)
    {
        if (burstPool != null)
        {
            burstPool.Get().Play(impactPoint, Quaternion.identity, 1f, burstLifetime);
        }

        if (flamePool == null || impactVfxCount <= 0) return;

        float angleStep = 360f / impactVfxCount;
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < impactVfxCount; i++)
        {
            float jitter = Random.Range(-angleStep * angleJitterFraction, angleStep * angleJitterFraction);
            float angleRad = (startAngle + i * angleStep + jitter) * Mathf.Deg2Rad;
            float radius = Random.Range(minImpactRadius, maxImpactRadius);

            Vector3 pos = new Vector3(
                impactPoint.x + Mathf.Cos(angleRad) * radius,
                groundY,
                impactPoint.z + Mathf.Sin(angleRad) * radius);

            if (groundMask.value != 0)
            {
                Vector3 origin = new Vector3(pos.x, groundY + groundRayHeight, pos.z);
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                        groundRayHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    pos.y = hit.point.y;
                }
            }

            pos.y += groundOffset;

            Quaternion rot = randomYRotation
                ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                : Quaternion.identity;

            flamePool.Get().Play(pos, rot, Random.Range(scaleRange.x, scaleRange.y), impactVfxLifetime);
        }
    }

    private void OnValidate()
    {
        minImpactRadius = Mathf.Max(0f, minImpactRadius);
        maxImpactRadius = Mathf.Max(minImpactRadius, maxImpactRadius);
        impactVfxLifetime = Mathf.Max(0.1f, impactVfxLifetime);
        scaleRange.y = Mathf.Max(scaleRange.x, scaleRange.y);
    }
}