using System.Collections;
using UnityEngine;

public class DragonHitFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DragonHealth health;

    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;

    [Header("Debug")]
    [SerializeField] private bool showDamageLog = false;

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;

    private readonly int baseColorID = Shader.PropertyToID("_BaseColor");
    private readonly int colorID = Shader.PropertyToID("_Color");

    private Coroutine flashCoroutine;
    private float previousHealth;
    private bool hasPreviousHealth;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<DragonHealth>();

        renderers = GetComponentsInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (health == null)
            return;

        hasPreviousHealth = false;
        health.HealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (health == null)
            return;

        health.HealthChanged -= OnHealthChanged;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        RestoreColors();
    }

    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        if (!hasPreviousHealth)
        {
            previousHealth = currentHealth;
            hasPreviousHealth = true;
            return;
        }

        float damageTaken = previousHealth - currentHealth;

        if (damageTaken > 0f)
        {
            if (showDamageLog)
            {
                Debug.Log(
                    $"{gameObject.name} took {damageTaken} damage. " +
                    $"HP: {currentHealth}/{maxHealth}",
                    this
                );
            }

            PlayHitFlash();
        }

        previousHealth = currentHealth;
    }

    private void PlayHitFlash()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        SetFlashColor(flashColor);

        yield return new WaitForSeconds(flashDuration);

        RestoreColors();

        flashCoroutine = null;
    }

    private void SetFlashColor(Color color)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(propertyBlock);

            if (propertyBlock.HasProperty(baseColorID))
                propertyBlock.SetColor(baseColorID, color);
            else if (propertyBlock.HasProperty(colorID))
                propertyBlock.SetColor(colorID, color);

            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void RestoreColors()
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.SetPropertyBlock(null);
        }
    }
}