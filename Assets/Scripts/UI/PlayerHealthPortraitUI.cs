using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthPortraitUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private Image portraitImage;

    [Header("Portraits")]
    [SerializeField] private Sprite fullHealthPortrait;
    [SerializeField] private Sprite mediumHealthPortrait;
    [SerializeField] private Sprite lowHealthPortrait;
    [SerializeField] private Sprite hitPortrait;

    [Header("Thresholds")]
    [Range(0f, 1f)]
    [SerializeField] private float mediumHealthThreshold = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float lowHealthThreshold = 0.25f;
    [SerializeField] private float hitPortraitDuration = 1f;

    private Coroutine hitPortraitCoroutine;

    private void Awake()
    {
        if (portraitImage == null)
        {
            portraitImage = GetComponent<Image>();
        }

        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            playerHealth = player != null ? player.GetComponent<Health>() : null;
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += HandleHealthChanged;
        }
    }

    private void Start()
    {
        if (playerHealth != null)
        {
            SetHealthPortrait(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
        }

        if (hitPortraitCoroutine != null)
        {
            StopCoroutine(hitPortraitCoroutine);
            hitPortraitCoroutine = null;
        }
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        SetHealthPortrait(currentHealth, maxHealth);

        if (currentHealth < maxHealth)
        {
            if (hitPortraitCoroutine != null)
            {
                StopCoroutine(hitPortraitCoroutine);
            }

            hitPortraitCoroutine = StartCoroutine(ShowHitPortrait());
        }
    }

    private void SetHealthPortrait(int currentHealth, int maxHealth)
    {
        if (portraitImage == null || maxHealth <= 0)
        {
            return;
        }

        float healthRatio = (float)currentHealth / maxHealth;
        portraitImage.sprite = healthRatio <= lowHealthThreshold
            ? lowHealthPortrait
            : healthRatio <= mediumHealthThreshold
                ? mediumHealthPortrait
                : fullHealthPortrait;
    }

    private IEnumerator ShowHitPortrait()
    {
        portraitImage.sprite = hitPortrait;
        yield return new WaitForSeconds(hitPortraitDuration);

        SetHealthPortrait(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        hitPortraitCoroutine = null;
    }
}
