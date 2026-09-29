using System.Collections;
using UnityEngine;

public class WeaponBase : ItemBase
{
    private int _attackDamage;
    private float _attackRange;
    private Sprite _idleSprite;
    private Sprite _attackSprite;
    private Sprite _currentWeaponStance;

    private const float ATTACK_SPEED = 1.0f;

    public Sprite CurrentSprite => _currentWeaponStance;
    public int AttackDamage => _attackDamage;
    public float AttackRange => _attackRange;

    public void EquipWeapon(WeaponSO weaponData)
    {
        if (weaponData == null) return;
        _attackDamage = weaponData.WeaponDamage;
        _attackRange = weaponData.WeaponRange;
        _idleSprite = weaponData.IdleSprite;
        _attackSprite = weaponData.AttackSprite;
        _currentWeaponStance = _idleSprite;
    }

    public void Attack()
    {
        StartCoroutine(AnimateWeapon());
    }

    private IEnumerator AnimateWeapon()
    {
        _currentWeaponStance = _attackSprite;

        yield return new WaitForSeconds(ATTACK_SPEED);

        _currentWeaponStance = _idleSprite;
    }
}
