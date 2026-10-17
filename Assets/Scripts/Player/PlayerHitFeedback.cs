using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zeigt dem Spieler deutlich, dass er getroffen wurde – und von wo:
/// roter Bildschirmrand, Richtungsanzeige zum Angreifer, kurzes Kamerawackeln, optional ein Sound.
/// Die Anzeige wird zur Laufzeit erzeugt, in der Szene muss keine UI gebaut werden.
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerHitFeedback : MonoBehaviour
{
    private const int SortingOrder = 900;
    private const float IndicatorDistance = 330f; // Abstand zur Bildmitte (Referenz 1920x1080)

    [Header("Screen Flash")]
    [SerializeField] private Color flashColor = new(0.85f, 0f, 0f, 0.55f);

    [Min(0.05f)]
    [SerializeField] private float flashDuration = 0.4f;

    [Header("Direction Indicator")]
    [SerializeField] private Color indicatorColor = new(1f, 0.1f, 0.1f, 0.9f);

    [Min(0.1f)]
    [SerializeField] private float indicatorDuration = 1.2f;

    [Header("Camera Shake")]
    [Min(0f)]
    [SerializeField] private float shakeDuration = 0.2f;

    [Min(0f)]
    [SerializeField] private float shakeStrength = 0.4f;

    [Header("Sound")]
    [SerializeField] private AudioClip[] hitSounds;

    [Range(0f, 1f)]
    [SerializeField] private float hitVolume = 0.8f;

    private Health health;
    private Image flash;
    private RectTransform indicatorPivot;
    private Image indicator;
    private Transform viewCamera;
    private CameraShake cameraShake;
    private AudioSource audioSource;

    private Transform lastAttacker;
    private Vector3 lastHitDirection;
    private float flashTimer;
    private float indicatorTimer;

    private void Awake()
    {
        health = GetComponent<Health>();
        CreateOverlay();
    }

    private void Start()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return;
        }

        viewCamera = mainCamera.transform;

        if (!mainCamera.TryGetComponent(out cameraShake))
        {
            cameraShake = mainCamera.gameObject.AddComponent<CameraShake>();
        }
    }

    private void OnEnable()
    {
        health.Damaged += HandleDamaged;
        health.DamagedBy += HandleDamagedBy;
    }

    private void OnDisable()
    {
        health.Damaged -= HandleDamaged;
        health.DamagedBy -= HandleDamagedBy;
    }

    private void HandleDamaged(int damage)
    {
        flashTimer = flashDuration;

        if (cameraShake != null)
        {
            StartCoroutine(cameraShake.Shake(shakeDuration, shakeStrength));
        }

        PlayHitSound();
    }

    private void HandleDamagedBy(Transform attacker)
    {
        lastAttacker = attacker;
        lastHitDirection = attacker.position - transform.position;
        indicatorTimer = indicatorDuration;
    }

    private void Update()
    {
        UpdateFlash();
        UpdateIndicator();
    }

    private void UpdateFlash()
    {
        flashTimer = Mathf.Max(0f, flashTimer - Time.deltaTime);
        SetAlpha(flash, flashColor, flashTimer / flashDuration);
    }

    private void UpdateIndicator()
    {
        indicatorTimer = Mathf.Max(0f, indicatorTimer - Time.deltaTime);
        SetAlpha(indicator, indicatorColor, indicatorTimer / indicatorDuration);

        if (indicatorTimer <= 0f || viewCamera == null)
        {
            return;
        }

        // Folgt dem Angreifer, solange er lebt – sonst zeigt sie auf die letzte bekannte Richtung.
        if (lastAttacker != null)
        {
            lastHitDirection = lastAttacker.position - transform.position;
        }

        Vector3 forward = Vector3.ProjectOnPlane(viewCamera.forward, Vector3.up);
        Vector3 toAttacker = Vector3.ProjectOnPlane(lastHitDirection, Vector3.up);

        float angle = Vector3.SignedAngle(forward, toAttacker, Vector3.up);
        indicatorPivot.localEulerAngles = new Vector3(0f, 0f, -angle);
    }

    private void PlayHitSound()
    {
        if (hitSounds == null || hitSounds.Length == 0)
        {
            return;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
        }

        audioSource.PlayOneShot(hitSounds[Random.Range(0, hitSounds.Length)], hitVolume);
    }

    private static void SetAlpha(Image image, Color color, float factor)
    {
        color.a *= Mathf.Clamp01(factor);
        image.color = color;
        image.enabled = color.a > 0.001f;
    }

    private void CreateOverlay()
    {
        GameObject canvasObject = new("HitFeedbackCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        flash = CreateImage("Flash", canvasObject.transform, CreateVignetteSprite());
        Stretch(flash.rectTransform);

        GameObject pivotObject = new("IndicatorPivot", typeof(RectTransform));
        pivotObject.transform.SetParent(canvasObject.transform, false);
        indicatorPivot = (RectTransform)pivotObject.transform;

        indicator = CreateImage("Indicator", indicatorPivot, CreateArcSprite());
        indicator.rectTransform.sizeDelta = new Vector2(260f, 60f);
        indicator.rectTransform.anchoredPosition = new Vector2(0f, IndicatorDistance);

        SetAlpha(flash, flashColor, 0f);
        SetAlpha(indicator, indicatorColor, 0f);
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject imageObject = new(name, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;

        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>Weißer Rand, innen durchsichtig – wird über die Image-Farbe rot eingefärbt.</summary>
    private static Sprite CreateVignetteSprite()
    {
        const int size = 128;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 fromCenter = new Vector2(x, y) / (size - 1) * 2f - Vector2.one;
                float edge = Mathf.Max(Mathf.Abs(fromCenter.x), Mathf.Abs(fromCenter.y));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, edge))));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>Weicher, nach außen gebogener Balken für die Richtungsanzeige.</summary>
    private static Sprite CreateArcSprite()
    {
        const int width = 128;
        const int height = 32;
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f) * 2f - 1f;
                float arcY = (1f - u * u) * 0.5f; // Bogen: Mitte höher als die Enden
                float v = y / (height - 1f);
                float thickness = Mathf.Abs(v - (0.25f + arcY)) * 4f;
                float alpha = Mathf.Clamp01(1f - thickness) * Mathf.Clamp01(1f - Mathf.Abs(u));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}
