using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Schwarzblende über dem ganzen Bild. Erzeugt ihr eigenes Overlay-Canvas zur Laufzeit,
/// in der Szene muss keine UI gebaut werden. Liegt knapp unter dem Ladebildschirm.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private const int SortingOrder = 999;

    [Tooltip("Bild beim Start komplett schwarz (z.B. für Aufwach-Szenen).")]
    [SerializeField] private bool startBlack;

    private Image overlay;

    public float Alpha => overlay.color.a;

    private void Awake()
    {
        overlay = CreateOverlay();
        SetAlpha(startBlack ? 1f : 0f);
    }

    public void SetAlpha(float alpha)
    {
        Color color = overlay.color;
        color.a = Mathf.Clamp01(alpha);
        overlay.color = color;

        overlay.enabled = color.a > 0f;
    }

    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = Alpha;

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private Image CreateOverlay()
    {
        GameObject canvasObject = new("ScreenFaderCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        GameObject imageObject = new("Overlay");
        imageObject.transform.SetParent(canvasObject.transform, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return image;
    }
}
