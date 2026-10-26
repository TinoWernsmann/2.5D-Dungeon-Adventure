using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup "Erfolg freigeschaltet" oben in der Bildschirmmitte (mit Ein-/Ausblenden und optionalem Sound).
/// Wird beim Spielstart automatisch erzeugt und bleibt über alle Szenen erhalten –
/// keine Einrichtung in den Szenen nötig.
/// </summary>
public class AchievementToast : MonoBehaviour
{
    private const float DisplaySeconds = 5f;
    private const float FadeSeconds = 0.35f;

    private static readonly Color HeaderColor = new(1f, 0.82f, 0.3f, 1f);
    private static readonly Color BorderColor = new(1f, 0.82f, 0.3f, 0.9f);

    private readonly Queue<AchievementData> pending = new();

    private CanvasGroup canvasGroup;
    private TMP_Text titleLabel;
    private AudioSource audioSource;
    private bool isShowing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        GameObject toastObject = new("AchievementToast");
        toastObject.AddComponent<AchievementToast>();
        DontDestroyOnLoad(toastObject);
    }

    private void Awake()
    {
        BuildUi();
        AchievementManager.Unlocked += Enqueue;
    }

    private void OnDestroy()
    {
        AchievementManager.Unlocked -= Enqueue;
    }

    private void Enqueue(AchievementData achievement)
    {
        pending.Enqueue(achievement);

        if (!isShowing)
        {
            StartCoroutine(ShowPending());
        }
    }

    private IEnumerator ShowPending()
    {
        isShowing = true;

        while (pending.Count > 0)
        {
            AchievementData achievement = pending.Dequeue();
            titleLabel.text = achievement.Title;

            AudioClip sound = AchievementManager.Registry != null ? AchievementManager.Registry.PopupSound : null;

            if (sound != null)
            {
                audioSource.PlayOneShot(sound);
            }

            // Echtzeit: läuft auch im Pause-Menü (timeScale 0) weiter.
            yield return Fade(0f, 1f);
            yield return new WaitForSecondsRealtime(DisplaySeconds);
            yield return Fade(1f, 0f);
        }

        isShowing = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float elapsed = 0f; elapsed < FadeSeconds; elapsed += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / FadeSeconds);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // über allen Menüs und dem HUD

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.ignoreListenerPause = true;

        // Goldener Rahmen mit dunklem Inhalt, oben mittig – gut sichtbar, verdeckt aber nicht die Spielmitte.
        GameObject border = CreateImage("Border", transform, BorderColor);
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = borderRect.anchorMax = borderRect.pivot = new Vector2(0.5f, 1f);
        borderRect.anchoredPosition = new Vector2(0f, -40f);
        borderRect.sizeDelta = new Vector2(820f, 150f);

        canvasGroup = border.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject window = CreateImage("Window", border.transform, new Color(0f, 0f, 0f, 0.92f));
        Stretch(window.GetComponent<RectTransform>(), 4f);

        TMP_Text header = CreateLabel("Header", window.transform, "Erfolg freigeschaltet!", 26, HeaderColor);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.55f);
        headerRect.anchorMax = Vector2.one;
        headerRect.offsetMin = new Vector2(20f, 0f);
        headerRect.offsetMax = new Vector2(-20f, -14f);

        titleLabel = CreateLabel("Title", window.transform, string.Empty, 32, Color.white);
        RectTransform titleRect = titleLabel.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = new Vector2(1f, 0.55f);
        titleRect.offsetMin = new Vector2(20f, 14f);
        titleRect.offsetMax = new Vector2(-20f, 0f);
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return imageObject;
    }

    private static TMP_Text CreateLabel(string name, Transform parent, string text, float fontSize, Color color)
    {
        GameObject labelObject = new(name, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14;
        label.fontSizeMax = fontSize;

        AchievementRegistry registry = AchievementManager.Registry;

        if (registry != null && registry.PopupFont != null)
        {
            label.font = registry.PopupFont;
        }

        return label;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
