using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup "Erfolg freigeschaltet" oben rechts. Wird beim Spielstart automatisch erzeugt
/// und bleibt über alle Szenen erhalten – keine Einrichtung in den Szenen nötig.
/// </summary>
public class AchievementToast : MonoBehaviour
{
    private const float DisplaySeconds = 3.5f;

    private readonly Queue<AchievementData> pending = new();

    private GameObject window;
    private TMP_Text label;
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

            label.text = $"Erfolg freigeschaltet!\n<size=80%>{achievement.Title}</size>";
            window.SetActive(true);

            // Echtzeit: läuft auch im Pause-Menü (timeScale 0) weiter.
            yield return new WaitForSecondsRealtime(DisplaySeconds);

            window.SetActive(false);
        }

        isShowing = false;
    }

    private void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // über allen Menüs

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        window = new GameObject("Window", typeof(RectTransform), typeof(Image));
        window.transform.SetParent(transform, false);
        window.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
        window.GetComponent<Image>().raycastTarget = false;

        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = windowRect.anchorMax = windowRect.pivot = new Vector2(1f, 1f);
        windowRect.anchoredPosition = new Vector2(-30f, -30f);
        windowRect.sizeDelta = new Vector2(560f, 130f);

        GameObject labelObject = new("Label", typeof(RectTransform));
        labelObject.transform.SetParent(window.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 15f);
        labelRect.offsetMax = new Vector2(-20f, -15f);

        label = labelObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = 24;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        AchievementRegistry registry = AchievementManager.Registry;

        if (registry != null && registry.PopupFont != null)
        {
            label.font = registry.PopupFont;
        }

        window.SetActive(false);
    }
}
