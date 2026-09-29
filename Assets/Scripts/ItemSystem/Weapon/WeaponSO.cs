using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "New Item/Weapon")]
public class WeaponSO : ItemSO
{
    public int WeaponDamage;
    public float WeaponRange;
    public AudioClip AttackSound;
    public Sprite IdleSprite;
    public Sprite AttackSprite;
}
