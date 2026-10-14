using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image weaponImage;

    private Coroutine attackAnimation;

    // Ruhebild der AKTUELL ausgerüsteten Waffe. Eine laufende Angriffsanimation kehrt
    // am Ende hierhin zurück – nicht zur Waffe, mit der angegriffen wurde.
    private Sprite currentIdleSprite;

    private void Awake()
    {
        if (weaponImage == null)
        {
            weaponImage = GetComponent<Image>();
        }

        ShowWeapon(null);
    }

    /// <summary>
    /// Zeigt eine (neu ausgerüstete) Waffe an. Bricht eine laufende Angriffsanimation ab,
    /// damit z.B. nach einem schnellen Wechsel nicht das Bild der vorherigen Waffe erscheint.
    /// </summary>
    public void ShowWeapon(Sprite idleSprite)
    {
        StopAttackAnimation();

        currentIdleSprite = idleSprite;
        SetWeaponSprite(idleSprite);
    }

    public void PlayAttackAnimation(
        Sprite attackSprite,
        Sprite idleSprite,
        float duration)
    {
        if (weaponImage == null)
        {
            return;
        }

        StopAttackAnimation();

        currentIdleSprite = idleSprite;

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
        SetWeaponSprite(attackSprite);

        yield return new WaitForSeconds(duration);

        SetWeaponSprite(currentIdleSprite);

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

    private void SetWeaponSprite(Sprite sprite)
    {
        if (weaponImage == null)
        {
            return;
        }

        weaponImage.sprite = sprite;
        weaponImage.enabled = sprite != null;
    }
}
