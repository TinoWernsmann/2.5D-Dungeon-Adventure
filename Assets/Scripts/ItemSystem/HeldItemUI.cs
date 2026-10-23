using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Zeigt das Item in der Hand des Players (Waffe, Schlüssel, Trank …)
/// und spielt bei Waffen die Angriffsanimation.
/// </summary>
public class HeldItemUI : MonoBehaviour
{
    [Header("References")]
    [FormerlySerializedAs("weaponImage")]
    [SerializeField] private Image heldItemImage;

    private Coroutine attackAnimation;

    // Bild des AKTUELL gehaltenen Items. Eine laufende Angriffsanimation kehrt
    // am Ende hierhin zurück – nicht zur Waffe, mit der angegriffen wurde.
    private Sprite currentHeldSprite;

    private void Awake()
    {
        if (heldItemImage == null)
        {
            heldItemImage = GetComponent<Image>();
        }

        ShowHeldItem(null);
    }

    /// <summary>
    /// Zeigt das (neu ausgewählte) Item in der Hand. Bricht eine laufende Angriffsanimation ab,
    /// damit z.B. nach einem schnellen Wechsel nicht das Bild der vorherigen Waffe erscheint.
    /// </summary>
    public void ShowHeldItem(Sprite heldSprite)
    {
        StopAttackAnimation();

        currentHeldSprite = heldSprite;
        SetSprite(heldSprite);
    }

    /// <summary>
    /// Tauscht das Bild des gehaltenen Items (z.B. Armbrust leer/geladen), ohne eine laufende
    /// Angriffsanimation abzubrechen – sie endet dann mit dem neuen Bild.
    /// </summary>
    public void UpdateHeldSprite(Sprite heldSprite)
    {
        currentHeldSprite = heldSprite;

        if (attackAnimation == null)
        {
            SetSprite(heldSprite);
        }
    }

    public void PlayAttackAnimation(
        Sprite attackSprite,
        Sprite heldSprite,
        float duration)
    {
        if (heldItemImage == null)
        {
            return;
        }

        StopAttackAnimation();

        currentHeldSprite = heldSprite;

        attackAnimation = StartCoroutine(
            PlayAttackAnimationRoutine(
                attackSprite,
                duration
            )
        );
    }

    private IEnumerator PlayAttackAnimationRoutine(
        Sprite attackSprite,
        float duration)
    {
        SetSprite(attackSprite);

        yield return new WaitForSeconds(duration);

        SetSprite(currentHeldSprite);

        attackAnimation = null;
    }

    private void StopAttackAnimation()
    {
        if (attackAnimation == null)
        {
            return;
        }

        StopCoroutine(attackAnimation);
        attackAnimation = null;
    }

    private void SetSprite(Sprite sprite)
    {
        if (heldItemImage == null)
        {
            return;
        }

        heldItemImage.sprite = sprite;
        heldItemImage.enabled = sprite != null;
    }
}
