using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Zeigt das ausgewählte Item in der Hand und rüstet Waffen-Sounds aus.
/// Jedes Item kann gehalten werden; nur Waffen (<see cref="WeaponSO"/>) können angreifen.
/// Munitionswaffen (Armbrust) zeigen die geladene Munition – oder sind leer.
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

    private PlayerCombat playerCombat;

    /// <summary>Waffe in der Hand: ausgewählte Waffe, Fäuste bei leerer Hand, null bei Nicht-Waffen (Schlüssel, Trank …).</summary>
    public WeaponSO CurrentWeapon =>
        itemManager == null || itemManager.SelectedItem == null
            ? fists
            : itemManager.SelectedWeapon;

    private void Awake()
    {
        playerCombat = GetComponent<PlayerCombat>();

        if (playerCombat == null)
        {
            playerCombat = FindAnyObjectByType<PlayerCombat>();
        }
    }

    private void OnEnable()
    {
        if (itemManager != null)
        {
            itemManager.SelectedItemChanged += HandleSelectedItemChanged;
            itemManager.InventoryChanged += RefreshHeldSprite;
        }

        if (playerCombat != null)
        {
            playerCombat.SelectedAmmoChanged += RefreshHeldSprite;
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
            itemManager.InventoryChanged -= RefreshHeldSprite;
        }

        if (playerCombat != null)
        {
            playerCombat.SelectedAmmoChanged -= RefreshHeldSprite;
        }
    }

    private void HandleSelectedItemChanged(ItemSO selectedItem)
    {
        ItemSO held = selectedItem != null ? selectedItem : fists;

        heldItemUI?.ShowHeldItem(GetHeldSprite(held));

        if (held is WeaponSO weapon)
        {
            weaponSoundManager?.SetAttackSounds(weapon.AttackSounds);
            return;
        }

        weaponSoundManager?.ClearAttackSounds();
    }

    /// <summary>Munition geschossen, aufgehoben, gewechselt: Bild anpassen, ohne die Schuss-Animation abzubrechen.</summary>
    private void RefreshHeldSprite()
    {
        if (itemManager == null)
        {
            return;
        }

        ItemSO held = itemManager.SelectedItem != null ? itemManager.SelectedItem : fists;
        heldItemUI?.UpdateHeldSprite(GetHeldSprite(held));
    }

    private Sprite GetHeldSprite(ItemSO held)
    {
        if (held is WeaponSO weapon && playerCombat != null)
        {
            return weapon.GetHeldSprite(playerCombat.SelectedAmmo);
        }

        return held != null ? held.HeldSprite : null;
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
