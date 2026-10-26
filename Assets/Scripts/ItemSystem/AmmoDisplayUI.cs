using TMPro;
using UnityEngine;

/// <summary>
/// Zeigt neben dem Fadenkreuz die aktive Munition („Push Bolts ×3“), solange eine Waffe mit Munition
/// in der Hand ist. Mit R wechselt der Spieler die Sorte (siehe <see cref="PlayerCombat.SwitchAmmo"/>).
/// </summary>
public class AmmoDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text ammoText;

    [Tooltip("Leer lassen = wird automatisch gesucht.")]
    [SerializeField] private PlayerCombat playerCombat;

    [Tooltip("Leer lassen = wird automatisch gesucht.")]
    [SerializeField] private ItemManager itemManager;

    private void Awake()
    {
        if (playerCombat == null) playerCombat = FindAnyObjectByType<PlayerCombat>();
        if (itemManager == null) itemManager = FindAnyObjectByType<ItemManager>();
    }

    private void OnEnable()
    {
        if (playerCombat != null) playerCombat.SelectedAmmoChanged += Refresh;

        if (itemManager != null)
        {
            itemManager.InventoryChanged += Refresh;
            itemManager.SelectedItemChanged += HandleSelectedItemChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (playerCombat != null) playerCombat.SelectedAmmoChanged -= Refresh;

        if (itemManager != null)
        {
            itemManager.InventoryChanged -= Refresh;
            itemManager.SelectedItemChanged -= HandleSelectedItemChanged;
        }
    }

    private void HandleSelectedItemChanged(ItemSO item)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (ammoText == null)
        {
            return;
        }

        AmmoSO ammo = playerCombat != null ? playerCombat.SelectedAmmo : null;

        ammoText.gameObject.SetActive(ammo != null);

        if (ammo != null)
        {
            ammoText.text = $"{ammo.ItemName} ×{itemManager.GetItemAmount(ammo)}";
        }
    }
}
