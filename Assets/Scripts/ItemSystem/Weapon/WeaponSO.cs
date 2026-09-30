using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "New Item/Weapon")]
public class WeaponSO : ItemSO
{
    public int WeaponDamage;
    public float WeaponRange;
    public AudioClip[] AttackSounds;
    public Sprite IdleSprite;
    public Sprite AttackSprite;
}
