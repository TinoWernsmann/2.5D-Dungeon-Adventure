using UnityEngine;

[CreateAssetMenu(
    fileName = "New Weapon",
    menuName = "New Item/Weapon"
)]
public class WeaponSO : ItemSO
{
    [Header("Combat")]
    [Min(0)]
    public int WeaponDamage;

    [Min(0f)]
    public float WeaponRange;

    [Min(0.01f)]
    public float AttackDuration = 0.5f;

    [Header("Visuals")]
    public Sprite IdleSprite;
    public Sprite AttackSprite;

    [Header("Audio")]
    public AudioClip[] AttackSounds;
}