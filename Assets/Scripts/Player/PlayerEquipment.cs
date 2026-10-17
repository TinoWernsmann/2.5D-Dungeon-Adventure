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

    [Tooltip("Waffe, wenn kein Item ausgewählt ist (Taste 1).")]
    [SerializeField] private WeaponSO fists;

    /// <summary>Waffe in der Hand: ausgewählte Waffe, Fäuste bei leerer Hand, null bei Nicht-Waffen (Schlüssel, Trank …).</summary>
    public WeaponSO CurrentWeapon =>
        itemManager == null || itemManager.SelectedItem == null
            ? fists
            : itemManager.SelectedWeapon;

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
        ItemSO held = selectedItem != null ? selectedItem : fists;

        heldItemUI?.ShowHeldItem(held != null ? held.HeldSprite : null);

        if (held is WeaponSO weapon)
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