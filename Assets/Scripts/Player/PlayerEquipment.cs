using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Zeigt das ausgewählte Item in der Hand und rüstet Waffen-Sounds aus.
/// Jedes Item kann gehalten werden; nur Waffen (<see cref="WeaponSO"/>) können angreifen.
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemManager itemManager;
    [FormerlySerializedAs("weaponUI")]
    [SerializeField] private HeldItemUI heldItemUI;
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
        heldItemUI?.ShowHeldItem(selectedItem != null ? selectedItem.HeldSprite : null);

        if (selectedItem is WeaponSO weapon)
        {
            weaponSoundManager?.SetAttackSounds(weapon.AttackSounds);
            return;
        }

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