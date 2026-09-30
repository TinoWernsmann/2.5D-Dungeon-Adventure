using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUI : MonoBehaviour
{
    [SerializeField] private Image _equipImage;

    private bool _animIsRunnning;

    private const float ATTACK_SPEED = 0.5f;

    private void Awake()
    {
        UpdateEquipWeaponVisibilty();
    }

    private void Start()
    {
        _animIsRunnning = false;
    }

    public void UpdateEquipSprite(Sprite sprite)
    {
        _equipImage.sprite = sprite;
        UpdateEquipWeaponVisibilty();
    }

    private void UpdateEquipWeaponVisibilty()
    {
        _equipImage.gameObject.SetActive(_equipImage.sprite != null);
    }

    public void Attack(WeaponSO weapon, WeaponSoundManager sound)
    {
        if (_animIsRunnning) return;
        StartCoroutine(AnimateWeapon(weapon, sound));
    }

    private IEnumerator AnimateWeapon(WeaponSO weapon, WeaponSoundManager sound)
    {
        _animIsRunnning = true;
        UpdateEquipSprite(weapon.AttackSprite);
        sound.PlayRandomAttackSound();

        yield return new WaitForSeconds(ATTACK_SPEED);

        UpdateEquipSprite(weapon.IdleSprite);
        _animIsRunnning = false;
    }
}
