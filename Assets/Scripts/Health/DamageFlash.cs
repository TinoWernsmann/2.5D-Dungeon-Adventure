using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class DamageFlash : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = Color.red;

    [Min(0.01f)]
    [SerializeField] private float flashDuration = 0.1f;

    private Color originalColor;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Damaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= HandleDamaged;
        }

        ResetColor();
    }

    private void HandleDamaged(int damage)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        spriteRenderer.color = originalColor;

        flashCoroutine = null;
    }

    private void ResetColor()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = originalColor;
    }
}