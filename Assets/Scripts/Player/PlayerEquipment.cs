using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemManager itemManager;
    [SerializeField] private WeaponUI weaponUI;
    [SerializeField] private WeaponSoundManager weaponSoundManager;

    private void OnEnable()
    {
        if (itemManager != null)
        {
            itemManager.SelectedItemChanged += HandleSelectedItemChanged;
        }
    }

    private void Start()
    {
        RefreshEquipment();
    }

    private void OnDisable()
    {
        if (itemManager != null)
        {
            itemManager.SelectedItemChanged -= HandleSelectedItemChanged;
        }
    }

    private void HandleSelectedItemChanged(ItemSO selectedItem)
    {
        if (selectedItem is WeaponSO weapon)
        {
            EquipWeapon(weapon);
            return;
        }

        UnequipWeapon();
    }

    private void EquipWeapon(WeaponSO weapon)
    {
        weaponUI?.SetWeaponSprite(weapon.IdleSprite);
        weaponSoundManager?.SetAttackSounds(weapon.AttackSounds);
    }

    private void UnequipWeapon()
    {
        weaponUI?.SetWeaponSprite(null);
        weaponSoundManager?.ClearAttackSounds();
    }

    private void RefreshEquipment()
    {
        if (itemManager == null)
        {
            return;
        }

        HandleSelectedItemChanged(itemManager.SelectedItem);
    }
}