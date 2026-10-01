using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image weaponImage;

    private Coroutine attackAnimation;

    private void Awake()
    {
        if (weaponImage == null)
        {
            weaponImage = GetComponent<Image>();
        }

        HideWeapon();
    }

    public void SetWeaponSprite(Sprite sprite)
    {
        if (weaponImage == null)
        {
            return;
        }

        weaponImage.sprite = sprite;
        weaponImage.enabled = sprite != null;
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

        if (attackAnimation != null)
        {
            StopCoroutine(attackAnimation);
        }

        attackAnimation = StartCoroutine(
            PlayAttackAnimationRoutine(
                attackSprite,
                idleSprite,
                duration
            )
        );
    }

    private IEnumerator PlayAttackAnimationRoutine(
        Sprite attackSprite,
        Sprite idleSprite,
        float duration)
    {
        SetWeaponSprite(attackSprite);

        yield return new WaitForSeconds(duration);

        SetWeaponSprite(idleSprite);

        attackAnimation = null;
    }

    private void HideWeapon()
    {
        SetWeaponSprite(null);
    }
}